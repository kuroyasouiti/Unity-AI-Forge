using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace MCP.Editor
{
    internal enum McpConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
    }

    /// <summary>
    /// Information about the connected MCP client (Claude Desktop, Claude Code, etc.)
    /// </summary>
    internal sealed class ClientInfo
    {
        public string ClientName { get; set; } = "Unknown";
        public string ServerName { get; set; } = string.Empty;
        public string ServerVersion { get; set; } = string.Empty;
        public string PythonVersion { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
    }

    /// <summary>
    /// Main WebSocket bridge service that connects Unity Editor to MCP clients.
    /// Handles client connections, message routing, context updates, and heartbeat monitoring.
    /// Features automatic reconnection after disconnections and compilation/assembly reloads.
    /// </summary>
    [InitializeOnLoad]
    internal static class McpBridgeService
    {
        private const string BridgePath = "/bridge";
        private const int MaxHandshakeHeaderSize = 16 * 1024;
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromSeconds(30);
        private const string WasConnectedBeforeCompileKey = "McpBridge_WasConnectedBeforeCompile";
        private const string CompilationStartTimeKey = "McpBridge_CompilationStartTime";
        private const string PendingCompilationResultKey = "McpBridge_PendingCompilationResult";

        private static readonly ConcurrentQueue<string> IncomingMessages = new();
        private static readonly Queue<Action> MainThreadActions = new();
        private static readonly object SendLock = new();
        private static bool _isCompiling = false;
        private static DateTime _compilationStartTime;
        private static DateTime _lastCompilationProgressSent = DateTime.MinValue;

        private static TcpListener _listener;
        private static CancellationTokenSource _listenerCts;
        private static CancellationTokenSource _receiveCts;
        private static TcpClient _client;
        private static WebSocket _socket;
        private static DateTime _lastHeartbeatSent = DateTime.MinValue;
        private static DateTime _lastHeartbeatReceived = DateTime.MinValue;
        private static DateTime _lastContextSent = DateTime.MinValue;
        private static bool _contextDirty = true;
        private static string _sessionId = Guid.NewGuid().ToString();
        private static McpConnectionState _state = McpConnectionState.Disconnected;
        private static bool _isCompilingOrReloading = false;
        private static bool _shouldSendRestartedSignal = false;
        private static ClientInfo _clientInfo = null;

        public static event Action<McpConnectionState> StateChanged;
        public static event Action<ClientInfo> ClientInfoReceived;

        public static McpConnectionState State => _state;

        public static bool IsConnected => _socket != null && _socket.State == WebSocketState.Open;

        public static string SessionId => _sessionId;

        public static ClientInfo ConnectedClientInfo => _clientInfo;

        static McpBridgeService()
        {
            EditorApplication.update += OnEditorUpdate;
            Selection.selectionChanged += MarkContextDirty;
            EditorApplication.hierarchyChanged += MarkContextDirty;
            EditorApplication.projectChanged += MarkContextDirty;

            // コンパイル開始時に接続状態を保存
            CompilationPipeline.compilationStarted += OnCompilationStarted;

            // コンパイル完了時に保留コマンドを処理
            CompilationPipeline.compilationFinished += OnCompilationFinished;

            // アセンブリリロード前に接続状態を保存
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

            // アセンブリリロード後に再接続
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;

            DelayAction(() =>
            {
                // コンパイル前に接続していた場合は自動再接続
                bool wasConnectedBeforeCompile = EditorPrefs.GetBool(WasConnectedBeforeCompileKey, false);

                if (wasConnectedBeforeCompile)
                {
                    Debug.Log("MCP Bridge: Reconnecting after compilation/reload...");
                    EditorPrefs.DeleteKey(WasConnectedBeforeCompileKey);
                    _shouldSendRestartedSignal = true;
                    Connect();
                }
                else if (McpBridgeSettings.Instance.AutoConnectOnLoad)
                {
                    Connect();
                }
            });
        }

        /// <summary>
        /// Starts the WebSocket bridge listener on the configured host and port.
        /// Creates a new session and begins accepting client connections.
        /// </summary>
        public static void Connect()
        {
            if (_listener != null)
            {
                return;
            }

            _sessionId = Guid.NewGuid().ToString();
            StartListener();
        }

        /// <summary>
        /// Stops the bridge listener and closes any active client connections.
        /// Cleans up all resources and transitions to Disconnected state.
        /// </summary>
        public static void Disconnect()
        {
            _listenerCts?.Cancel();
            _listenerCts?.Dispose();
            _listenerCts = null;

            if (_listener != null)
            {
                try
                {
                    _listener.Stop();
                }
                catch (Exception)
                {
                    // ignored
                }
                _listener = null;
            }

            CloseSocket();

            _clientInfo = null;
            _state = McpConnectionState.Disconnected;
            StateChanged?.Invoke(_state);
        }

        /// <summary>
        /// Sends a message to the connected MCP client over WebSocket.
        /// Message is serialized to JSON and sent as a text frame.
        /// </summary>
        /// <param name="message">Dictionary containing message type and payload data.</param>
        public static void Send(Dictionary<string, object> message)
        {
            if (!IsConnected)
            {
                return;
            }

            var json = MiniJson.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            var segment = new ArraySegment<byte>(bytes);

            lock (SendLock)
            {
                if (!IsConnected)
                {
                    return;
                }

                try
                {
                    _ = _socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"MCP bridge send error: {ex.Message}");
                    HandleSocketClosed();
                }
            }
        }

        /// <summary>
        /// Forces an immediate context update to be sent to the connected client.
        /// Bypasses the normal context push interval timer.
        /// </summary>
        public static void RequestImmediateContextPush()
        {
            MarkContextDirty();
            PushContext();
        }

        private static void StartListener()
        {
            var settings = McpBridgeSettings.Instance;
            try
            {
                var ipAddress = ResolveListenerAddress(settings.ServerHost);
                _listener = new TcpListener(ipAddress, settings.ServerPort);
                _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listener.Start();
                _listenerCts = new CancellationTokenSource();
                _state = McpConnectionState.Connecting;
                StateChanged?.Invoke(_state);
                _ = Task.Run(() => AcceptLoopAsync(_listenerCts.Token));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start MCP bridge listener: {ex.Message}");
                Disconnect();
            }
        }

        private static IPAddress ResolveListenerAddress(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return IPAddress.Loopback;
            }

            if (host == "*" || host == "+")
            {
                return IPAddress.Any;
            }

            if (IPAddress.TryParse(host, out var ipAddress))
            {
                return ipAddress;
            }

            try
            {
                var addresses = Dns.GetHostAddresses(host);
                var ipv4 = addresses.FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork);
                if (ipv4 != null)
                {
                    return ipv4;
                }

                var ipv6 = addresses.FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetworkV6);
                if (ipv6 != null)
                {
                    return ipv6;
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return IPAddress.Loopback;
        }

        private static async Task AcceptLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient client = null;
                try
                {
                    client = await _listener.AcceptTcpClientAsync();
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (InvalidOperationException)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    continue;
                }
                catch (SocketException ex)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    Debug.LogError($"MCP bridge listener socket error: {ex.Message}");
                    continue;
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    Debug.LogError($"MCP bridge listener error: {ex.Message}");
                    continue;
                }

                if (client == null)
                {
                    continue;
                }

                _ = Task.Run(() => HandleClientAsync(client, token));
            }
        }

        private static async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            NetworkStream stream = null;
            HttpRequestData request = null;

            try
            {
                client.NoDelay = true;
                stream = client.GetStream();

                using var handshakeCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                handshakeCts.CancelAfter(TimeSpan.FromSeconds(10));

                request = await ReadHttpRequestAsync(stream, handshakeCts.Token);
                if (request == null)
                {
                    await SendHttpErrorAsync(stream, HttpStatusCode.BadRequest, "Malformed request.", handshakeCts.Token);
                    client.Dispose();
                    return;
                }

                if (!IsWebSocketHandshake(request, out var failureReason))
                {
                    LogRejectedRequest(request, failureReason ?? "Expected websocket upgrade.");
                    await SendHttpErrorAsync(stream, HttpStatusCode.BadRequest, "Expected websocket upgrade.", handshakeCts.Token);
                    client.Dispose();
                    return;
                }

                var normalizedPath = request.NormalizedPath;
                var expectedPath = BridgePath.TrimEnd('/');
                if (normalizedPath.Length == 0)
                {
                    normalizedPath = "/";
                }

                if (expectedPath.Length == 0)
                {
                    expectedPath = "/";
                }

                if (!string.Equals(normalizedPath, expectedPath, StringComparison.OrdinalIgnoreCase))
                {
                    LogRejectedRequest(request, $"Unexpected path (expected \"{BridgePath}\").");
                    await SendHttpErrorAsync(stream, HttpStatusCode.NotFound, "Not found.", handshakeCts.Token);
                    client.Dispose();
                    return;
                }

                var selectedSubProtocol = GetRequestedSubprotocol(request);
                await SendWebSocketHandshakeResponseAsync(stream, request, selectedSubProtocol, handshakeCts.Token);

                var webSocket = WebSocket.CreateFromStream(stream, isServer: true, subProtocol: selectedSubProtocol, TimeSpan.FromSeconds(30));
                RegisterSocket(webSocket, client);
                return;
            }
            catch (OperationCanceledException)
            {
                client.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to accept MCP bridge connection: {ex.Message}");
                try
                {
                    client.Dispose();
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        private static async Task<HttpRequestData> ReadHttpRequestAsync(NetworkStream stream, CancellationToken token)
        {
            var buffer = new List<byte>();
            var singleByte = new byte[1];

            while (buffer.Count < MaxHandshakeHeaderSize)
            {
                var bytesRead = await stream.ReadAsync(singleByte, 0, 1, token);
                if (bytesRead == 0)
                {
                    return null;
                }

                buffer.Add(singleByte[0]);
                var count = buffer.Count;
                if (count >= 4 &&
                    buffer[count - 4] == '\r' &&
                    buffer[count - 3] == '\n' &&
                    buffer[count - 2] == '\r' &&
                    buffer[count - 1] == '\n')
                {
                    break;
                }
            }

            if (buffer.Count >= MaxHandshakeHeaderSize)
            {
                return null;
            }

            var requestText = Encoding.ASCII.GetString(buffer.ToArray());
            var lines = requestText.Split(new[] { "\r\n" }, StringSplitOptions.None);
            if (lines.Length == 0 || string.IsNullOrWhiteSpace(lines[0]))
            {
                return null;
            }

            var requestLine = lines[0];
            var parts = requestLine.Split(' ');
            if (parts.Length < 3)
            {
                return null;
            }

            var method = parts[0];
            var rawPath = parts[1];
            var versionToken = parts[2];

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrEmpty(line))
                {
                    break;
                }

                var separatorIndex = line.IndexOf(':');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var headerName = line.Substring(0, separatorIndex).Trim();
                var headerValue = line.Substring(separatorIndex + 1).Trim();
                headers[headerName] = headerValue;
            }

            var protocolVersion = HttpVersion.Version10;
            if (versionToken.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
            {
                var versionString = versionToken.Substring(5);
                if (!Version.TryParse(versionString, out protocolVersion))
                {
                    protocolVersion = HttpVersion.Version10;
                }
            }

            var pathWithoutQuery = rawPath;
            var queryIndex = rawPath.IndexOf('?');
            if (queryIndex >= 0)
            {
                pathWithoutQuery = rawPath.Substring(0, queryIndex);
            }

            var normalizedPath = pathWithoutQuery.TrimEnd('/');
            if (normalizedPath.Length == 0)
            {
                normalizedPath = "/";
            }

            return new HttpRequestData
            {
                RequestLine = requestLine,
                Method = method,
                RawPath = rawPath,
                NormalizedPath = normalizedPath,
                ProtocolVersion = protocolVersion,
                Headers = headers,
            };
        }

        private static bool IsWebSocketHandshake(HttpRequestData request, out string reason)
        {
            if (request == null)
            {
                reason = "Missing request.";
                return false;
            }

            if (!string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                reason = "Expected GET method.";
                return false;
            }

            if (request.ProtocolVersion < HttpVersion.Version11)
            {
                reason = "HTTP/1.1 or higher required.";
                return false;
            }

            if (!request.Headers.TryGetValue("Upgrade", out var upgradeHeader) ||
                !string.Equals(upgradeHeader, "websocket", StringComparison.OrdinalIgnoreCase))
            {
                reason = "Missing Upgrade: websocket header.";
                return false;
            }

            if (!request.Headers.TryGetValue("Connection", out var connectionHeader) ||
                !connectionHeader
                    .Split(',')
                    .Any(value => string.Equals(value.Trim(), "upgrade", StringComparison.OrdinalIgnoreCase)))
            {
                reason = "Missing Connection: Upgrade header.";
                return false;
            }

            if (!request.Headers.TryGetValue("Sec-WebSocket-Key", out var keyHeader) ||
                string.IsNullOrWhiteSpace(keyHeader))
            {
                reason = "Missing Sec-WebSocket-Key header.";
                return false;
            }

            if (!request.Headers.TryGetValue("Sec-WebSocket-Version", out var versionHeader) ||
                string.IsNullOrWhiteSpace(versionHeader))
            {
                reason = "Missing Sec-WebSocket-Version header.";
                return false;
            }

            reason = null;

            return true;
        }

        private static string GetRequestedSubprotocol(HttpRequestData request)
        {
            if (request == null)
            {
                return null;
            }

            if (!request.Headers.TryGetValue("Sec-WebSocket-Protocol", out var protocolHeader) ||
                string.IsNullOrWhiteSpace(protocolHeader))
            {
                return null;
            }

            var protocols = protocolHeader
                .Split(',')
                .Select(value => value.Trim())
                .Where(value => !string.IsNullOrEmpty(value));

            return protocols.FirstOrDefault();
        }

        private static async Task SendWebSocketHandshakeResponseAsync(NetworkStream stream, HttpRequestData request, string subProtocol, CancellationToken token)
        {
            var key = request.Headers["Sec-WebSocket-Key"];
            var acceptValue = ComputeWebSocketAcceptKey(key);

            var responseBuilder = new StringBuilder();
            responseBuilder.Append("HTTP/1.1 101 Switching Protocols\r\n");
            responseBuilder.Append("Connection: Upgrade\r\n");
            responseBuilder.Append("Upgrade: websocket\r\n");
            responseBuilder.Append($"Sec-WebSocket-Accept: {acceptValue}\r\n");
            if (!string.IsNullOrEmpty(subProtocol))
            {
                responseBuilder.Append($"Sec-WebSocket-Protocol: {subProtocol}\r\n");
            }
            responseBuilder.Append("\r\n");

            var responseBytes = Encoding.ASCII.GetBytes(responseBuilder.ToString());
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length, token);
            await stream.FlushAsync(token);
        }

        private static string ComputeWebSocketAcceptKey(string secWebSocketKey)
        {
            var combined = secWebSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            var combinedBytes = Encoding.UTF8.GetBytes(combined);
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(combinedBytes);
            return Convert.ToBase64String(hash);
        }

        private static async Task SendHttpErrorAsync(NetworkStream stream, HttpStatusCode statusCode, string message, CancellationToken token)
        {
            var reasonPhrase = GetReasonPhrase(statusCode);
            var body = string.IsNullOrEmpty(message) ? reasonPhrase : message;
            var bodyBytes = Encoding.UTF8.GetBytes(body);

            var responseBuilder = new StringBuilder();
            responseBuilder.AppendFormat("HTTP/1.1 {0} {1}\r\n", (int)statusCode, reasonPhrase);
            responseBuilder.Append("Connection: close\r\n");
            responseBuilder.Append("Content-Type: text/plain; charset=utf-8\r\n");
            responseBuilder.AppendFormat("Content-Length: {0}\r\n", bodyBytes.Length);
            responseBuilder.Append("\r\n");

            var headerBytes = Encoding.ASCII.GetBytes(responseBuilder.ToString());
            await stream.WriteAsync(headerBytes, 0, headerBytes.Length, token);

            if (bodyBytes.Length > 0)
            {
            await stream.WriteAsync(bodyBytes, 0, bodyBytes.Length, token);
            }

            await stream.FlushAsync(token);
        }

        private static string GetReasonPhrase(HttpStatusCode statusCode)
        {
            switch (statusCode)
            {
                case HttpStatusCode.BadRequest:
                    return "Bad Request";
                case HttpStatusCode.NotFound:
                    return "Not Found";
                case HttpStatusCode.InternalServerError:
                    return "Internal Server Error";
                default:
                    return statusCode.ToString();
            }
        }

        private sealed class HttpRequestData
        {
            public string RequestLine { get; set; }
            public string Method { get; set; }
            public string RawPath { get; set; }
            public string NormalizedPath { get; set; }
            public Version ProtocolVersion { get; set; }
            public Dictionary<string, string> Headers { get; set; }
        }

        private static void LogRejectedRequest(HttpRequestData request, string reason)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"MCP bridge rejected connection: {reason}");

            if (!string.IsNullOrEmpty(request?.RequestLine))
            {
                builder.AppendLine(request.RequestLine);
            }

            if (request?.Headers != null)
            {
                foreach (var header in request.Headers)
                {
                    builder.AppendLine($"{header.Key}: {header.Value}");
                }
            }

            if (request?.ProtocolVersion != null)
            {
                builder.AppendLine($"ProtocolVersion: HTTP/{request.ProtocolVersion}");
            }

            Debug.LogWarning(builder.ToString());
        }

        private static void RegisterSocket(WebSocket socket, TcpClient client)
        {
            CloseSocket();
            _client = client;
            _socket = socket;
            _receiveCts = new CancellationTokenSource();
            _ = Task.Run(() => ReceiveLoopAsync(socket, _receiveCts.Token));

            // Reset heartbeat tracking on successful connection
            _lastHeartbeatReceived = DateTime.UtcNow;

            lock (MainThreadActions)
            {
                MainThreadActions.Enqueue(() =>
                {
                    _state = McpConnectionState.Connected;
                    StateChanged?.Invoke(_state);
                    Send(McpBridgeMessages.CreateHelloPayload(_sessionId, McpBridgeSettings.Instance.BridgeToken));

                    // Send restart signal if this is a reconnection after compilation/reload
                    if (_shouldSendRestartedSignal)
                    {
                        _shouldSendRestartedSignal = false;
                        Send(McpBridgeMessages.CreateBridgeRestarted("compilation_or_reload"));
                        Debug.Log("MCP Bridge: Sent bridge restart signal after compilation/reload.");
                    }

                    // Check for pending compilation result and send it
                    if (EditorPrefs.HasKey(PendingCompilationResultKey))
                    {
                        try
                        {
                            var resultJson = EditorPrefs.GetString(PendingCompilationResultKey, "");
                            if (!string.IsNullOrEmpty(resultJson))
                            {
                                var compilationResult = MiniJson.Deserialize(resultJson) as Dictionary<string, object>;
                                if (compilationResult != null)
                                {
                                    Send(McpBridgeMessages.CreateCompilationComplete(compilationResult));
                                    Debug.Log($"MCP Bridge: Sent pending compilation result on connection (success: {compilationResult["success"]})");

                                    // クリーンアップ
                                    EditorPrefs.DeleteKey(PendingCompilationResultKey);
                                    EditorPrefs.DeleteKey(CompilationStartTimeKey);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"MCP Bridge: Failed to send pending compilation result: {ex.Message}");
                        }
                    }

                    MarkContextDirty();
                    PushContext();
                    Debug.Log("MCP Bridge: Client connected successfully.");
                });
            }
        }

        private static async Task ReceiveLoopAsync(WebSocket socket, CancellationToken token)
        {
            var buffer = new byte[8192];
            while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                using var ms = new MemoryStream();
                try
                {
                    do
                    {
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "ack", CancellationToken.None);
                            HandleSocketClosed();
                            return;
                        }

                        ms.Write(buffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                {
                    Debug.Log("MCP bridge connection closed by remote endpoint.");
                    HandleSocketClosed();
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"MCP bridge receive error: {ex.Message}");
                    HandleSocketClosed();
                    return;
                }

                var payload = Encoding.UTF8.GetString(ms.ToArray());
                IncomingMessages.Enqueue(payload);
            }
        }

        private static void CloseSocket()
        {
            _receiveCts?.Cancel();
            _receiveCts?.Dispose();
            _receiveCts = null;

            if (_socket != null)
            {
                try
                {
                    if (_socket.State == WebSocketState.Open)
                    {
                        _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "shutdown", CancellationToken.None)
                            .GetAwaiter().GetResult();
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
                finally
                {
                    _socket.Dispose();
                    _socket = null;
                }
            }

            if (_client != null)
            {
                try
                {
                    _client.Dispose();
                }
                catch (Exception)
                {
                    // ignored
                }
                finally
                {
                    _client = null;
                }
            }
        }

        private static void HandleSocketClosed()
        {
            CloseSocket();
            lock (MainThreadActions)
            {
                MainThreadActions.Enqueue(() =>
                {
                    _contextDirty = true;
                    _state = _listener != null ? McpConnectionState.Connecting : McpConnectionState.Disconnected;
                    StateChanged?.Invoke(_state);

                    // Log disconnection if listener is still active
                    if (_listener != null && !_isCompilingOrReloading)
                    {
                        Debug.Log("MCP Bridge: Client disconnected. Ready for reconnection.");
                    }
                });
            }
        }

        private static void OnEditorUpdate()
        {
            ProcessMainThreadActions();
            ProcessIncomingMessages();
            MaybeSendHeartbeat();
            MaybeCheckHeartbeatTimeout();
            MaybePushContext();
            MaybeSendCompilationProgress();
        }

        /// <summary>
        /// Sends compilation progress updates every 5 seconds during compilation.
        /// </summary>
        private static void MaybeSendCompilationProgress()
        {
            if (!_isCompiling || !IsConnected)
            {
                return;
            }

            var timeSinceLastProgress = DateTime.UtcNow - _lastCompilationProgressSent;
            if (timeSinceLastProgress < TimeSpan.FromSeconds(5))
            {
                return;
            }

            _lastCompilationProgressSent = DateTime.UtcNow;
            SendCompilationProgressMessage();
        }

        private static void ProcessMainThreadActions()
        {
            lock (MainThreadActions)
            {
                while (MainThreadActions.Count > 0)
                {
                    var action = MainThreadActions.Dequeue();
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
        }

        private static void ProcessIncomingMessages()
        {
            while (IncomingMessages.TryDequeue(out var json))
            {
                // Update last heartbeat received timestamp on any incoming message
                _lastHeartbeatReceived = DateTime.UtcNow;

                var payload = MiniJson.Deserialize(json);

                // Handle server:info message
                if (payload is Dictionary<string, object> dict &&
                    dict.TryGetValue("type", out var typeObj) &&
                    typeObj as string == "server:info")
                {
                    HandleServerInfoMessage(dict);
                    continue;
                }

                // Handle command messages
                if (McpIncomingCommand.TryParse(payload, out var command))
                {
                    lock (MainThreadActions)
                    {
                        MainThreadActions.Enqueue(() => ExecuteCommand(command));
                    }
                }
            }
        }

        private static void HandleServerInfoMessage(Dictionary<string, object> message)
        {
            if (!message.TryGetValue("clientInfo", out var clientInfoObj) ||
                clientInfoObj is not Dictionary<string, object> clientInfoDict)
            {
                return;
            }

            _clientInfo = new ClientInfo
            {
                ClientName = clientInfoDict.TryGetValue("clientName", out var cn) ? cn as string ?? "Unknown" : "Unknown",
                ServerName = clientInfoDict.TryGetValue("serverName", out var sn) ? sn as string ?? "" : "",
                ServerVersion = clientInfoDict.TryGetValue("serverVersion", out var sv) ? sv as string ?? "" : "",
                PythonVersion = clientInfoDict.TryGetValue("pythonVersion", out var pv) ? pv as string ?? "" : "",
                Platform = clientInfoDict.TryGetValue("platform", out var pl) ? pl as string ?? "" : "",
            };

            Debug.Log($"MCP Bridge: Received client info - {_clientInfo.ClientName} " +
                      $"(server={_clientInfo.ServerName} v{_clientInfo.ServerVersion}, " +
                      $"python={_clientInfo.PythonVersion}, platform={_clientInfo.Platform})");

            ClientInfoReceived?.Invoke(_clientInfo);
        }

        private static void ExecuteCommand(McpIncomingCommand command)
        {
            try
            {
                // Check if this command will trigger compilation
                bool willTriggerCompilation = IsCompilationTriggeringCommand(command);

                // If compilation will be triggered, save the command for later execution
                if (willTriggerCompilation && !_isCompiling)
                {
                    McpPendingCommandStorage.SavePendingCommand(command);
                    Debug.Log($"MCP Bridge: Command {command.CommandId} ({command.ToolName}) will trigger compilation, saved for post-compile execution");
                }

                // Execute the command
                var result = McpCommandProcessor.Execute(command);

                // Send result immediately if not compiling
                // If compiling started, the result will be sent after compilation completes
                if (!willTriggerCompilation || !EditorApplication.isCompiling)
                {
                    Send(McpBridgeMessages.CreateCommandResult(command.CommandId, true, result));
                }

                MarkContextDirty();
            }
            catch (Exception ex)
            {
                Debug.LogError($"MCP command failed ({command.ToolName}): {ex.Message}\n{ex}");
                Send(McpBridgeMessages.CreateCommandResult(command.CommandId, false, null, ex.Message));
            }
        }

        /// <summary>
        /// Determines if a command will trigger Unity compilation.
        /// </summary>
        private static bool IsCompilationTriggeringCommand(McpIncomingCommand command)
        {
            // Currently, only projectCompile with requestScriptCompilation=true triggers compilation
            if (command.ToolName == "projectCompile")
            {
                if (command.Payload != null &&
                    command.Payload.TryGetValue("requestScriptCompilation", out var value))
                {
                    if (value is bool boolValue)
                    {
                        return boolValue;
                    }
                }
                // Default to true if not specified
                return true;
            }

            return false;
        }

        private static void MaybeSendHeartbeat()
        {
            if (!IsConnected)
            {
                return;
            }

            if (DateTime.UtcNow - _lastHeartbeatSent < HeartbeatInterval)
            {
                return;
            }

            _lastHeartbeatSent = DateTime.UtcNow;
            Send(McpBridgeMessages.CreateHeartbeat());
        }

        private static void MaybeCheckHeartbeatTimeout()
        {
            if (!IsConnected)
            {
                return;
            }

            // Initialize lastHeartbeatReceived if this is the first check
            if (_lastHeartbeatReceived == DateTime.MinValue)
            {
                _lastHeartbeatReceived = DateTime.UtcNow;
                return;
            }

            // Check if we haven't received any message (including heartbeats) within timeout period
            if (DateTime.UtcNow - _lastHeartbeatReceived > HeartbeatTimeout)
            {
                Debug.LogWarning("MCP Bridge: Heartbeat timeout detected. Connection appears dead.");
                HandleSocketClosed();
            }
        }

        private static void MaybePushContext()
        {
            if (!IsConnected || !_contextDirty)
            {
                return;
            }

            var interval = TimeSpan.FromSeconds(McpBridgeSettings.Instance.ContextPushIntervalSeconds);
            if (DateTime.UtcNow - _lastContextSent < interval)
            {
                return;
            }

            PushContext();
        }

        private static void PushContext()
        {
            if (!IsConnected)
            {
                _contextDirty = true;
                return;
            }

            _contextDirty = false;
            _lastContextSent = DateTime.UtcNow;
            var payload = McpContextCollector.BuildContextPayload();
            Send(McpBridgeMessages.CreateContextUpdate(payload));
        }

        private static void MarkContextDirty()
        {
            _contextDirty = true;
        }

        private static void DelayAction(Action action)
        {
            void Wrapper()
            {
                EditorApplication.delayCall -= Wrapper;
                action?.Invoke();
            }

            EditorApplication.delayCall += Wrapper;
        }

        private static void OnCompilationStarted(object obj)
        {
            // コンパイル開始時に接続状態を保存
            if (_listener != null || IsConnected)
            {
                _isCompilingOrReloading = true;
                EditorPrefs.SetBool(WasConnectedBeforeCompileKey, true);
                Debug.Log("MCP Bridge: Saving connection state before compilation...");

                // Send compilation started notification to clients
                lock (MainThreadActions)
                {
                    MainThreadActions.Enqueue(SendCompilationStartedMessage);
                }
            }

            _isCompiling = true;
            _compilationStartTime = DateTime.UtcNow;

            // コンパイル開始時刻を保存（アセンブリリロード後も経過時間を計算できるように）
            EditorPrefs.SetString(CompilationStartTimeKey, _compilationStartTime.Ticks.ToString());
        }

        private static void OnCompilationFinished(object obj)
        {
            _isCompiling = false;

            // コンパイル結果を保存（アセンブリリロード後に送信するため）
            try
            {
                var compilationResult = McpCommandProcessor.GetCompilationResult();

                // 経過時間を計算
                var startTimeTicks = EditorPrefs.GetString(CompilationStartTimeKey, "");
                if (!string.IsNullOrEmpty(startTimeTicks) && long.TryParse(startTimeTicks, out var ticks))
                {
                    var startTime = new DateTime(ticks);
                    var elapsedSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
                    compilationResult["elapsedSeconds"] = (int)elapsedSeconds;
                }

                // コンパイル結果をJSON形式で保存
                EditorPrefs.SetString(PendingCompilationResultKey, MiniJson.Serialize(compilationResult));
                Debug.Log($"MCP Bridge: Compilation finished, result saved for post-reload transmission");
            }
            catch (Exception ex)
            {
                Debug.LogError($"MCP Bridge: Failed to save compilation result: {ex.Message}");
            }

            // コンパイル結果を取得して送信
            lock (MainThreadActions)
            {
                MainThreadActions.Enqueue(SendCompilationCompleteMessage);
                MainThreadActions.Enqueue(ExecutePendingCommands);
            }
        }

        /// <summary>
        /// Sends compilation started message to connected clients.
        /// Called from OnCompilationStarted via the main thread queue.
        /// </summary>
        private static void SendCompilationStartedMessage()
        {
            try
            {
                var message = new Dictionary<string, object>
                {
                    ["type"] = "compilation:started",
                    ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                };
                Send(message);
                Debug.Log("MCP Bridge: Sent compilation started message");
            }
            catch (Exception ex)
            {
                Debug.LogError($"MCP Bridge: Failed to send compilation started message: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends compilation progress message to connected clients during compilation.
        /// Called from OnEditorUpdate periodically while compiling.
        /// </summary>
        private static void SendCompilationProgressMessage()
        {
            if (!_isCompiling || !IsConnected)
            {
                return;
            }

            try
            {
                var elapsedSeconds = (DateTime.UtcNow - _compilationStartTime).TotalSeconds;
                var message = new Dictionary<string, object>
                {
                    ["type"] = "compilation:progress",
                    ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    ["elapsedSeconds"] = (int)elapsedSeconds,
                    ["status"] = "compiling",
                };
                Send(message);
            }
            catch (Exception ex)
            {
                // Don't spam error logs for progress messages
                Debug.LogWarning($"MCP Bridge: Failed to send compilation progress: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends compilation complete message to connected clients.
        /// Called from OnCompilationFinished via the main thread queue.
        /// </summary>
        private static void SendCompilationCompleteMessage()
        {
            try
            {
                var elapsedSeconds = (DateTime.UtcNow - _compilationStartTime).TotalSeconds;
                var compilationResult = McpCommandProcessor.GetCompilationResult();
                compilationResult["elapsedSeconds"] = (int)elapsedSeconds;
                Send(McpBridgeMessages.CreateCompilationComplete(compilationResult));
                Debug.Log($"MCP Bridge: Sent compilation complete message (success: {compilationResult["success"]}, elapsed: {elapsedSeconds:F1}s)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"MCP Bridge: Failed to send compilation complete message: {ex.Message}\n{ex}");
            }
        }

        /// <summary>
        /// Executes commands that were saved before compilation started.
        /// Called from OnCompilationFinished via the main thread queue.
        /// </summary>
        private static void ExecutePendingCommands()
        {
            if (!McpPendingCommandStorage.HasPendingCommands())
            {
                return;
            }

            try
            {
                var pendingCommands = McpPendingCommandStorage.GetAndClearPendingCommands();

                if (pendingCommands.Count == 0)
                {
                    return;
                }

                Debug.Log($"MCP Bridge: Executing {pendingCommands.Count} pending command(s) after compilation");

                foreach (var command in pendingCommands)
                {
                    try
                    {
                        // Get compilation result for this command
                        var compilationResult = McpCommandProcessor.GetCompilationResult();

                        // Send the compilation result as the command result
                        Send(McpBridgeMessages.CreateCommandResult(command.CommandId, true, compilationResult));

                        Debug.Log($"MCP Bridge: Sent result for pending command {command.CommandId} ({command.ToolName})");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"MCP Bridge: Failed to send result for pending command {command.CommandId}: {ex.Message}");
                        Send(McpBridgeMessages.CreateCommandResult(command.CommandId, false, null, ex.Message));
                    }
                }

                MarkContextDirty();
            }
            catch (Exception ex)
            {
                Debug.LogError($"MCP Bridge: Failed to execute pending commands: {ex.Message}\n{ex}");
            }
        }

        private static void OnBeforeAssemblyReload()
        {
            // アセンブリリロード前に接続状態を保存
            if (_listener != null || IsConnected)
            {
                _isCompilingOrReloading = true;
                EditorPrefs.SetBool(WasConnectedBeforeCompileKey, true);
                Debug.Log("MCP Bridge: Saving connection state before assembly reload...");
            }
        }

        private static void OnAfterAssemblyReload()
        {
            // この関数は新しいドメインで呼ばれるので、static constructorで処理済み
            // 念のため明示的にログを残す
            if (EditorPrefs.GetBool(WasConnectedBeforeCompileKey, false))
            {
                Debug.Log("MCP Bridge: Assembly reloaded, reconnection will be initiated...");
                // Note: コンパイル結果の送信はRegisterSocket()内で処理される
            }
        }

    }
}
