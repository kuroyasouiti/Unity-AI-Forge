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

    [InitializeOnLoad]
    internal static class McpBridgeService
    {
        private const string BridgePath = "/bridge";
        private const int MaxHandshakeHeaderSize = 16 * 1024;
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(10);
        private const string WasConnectedBeforeCompileKey = "McpBridge_WasConnectedBeforeCompile";

        private static readonly ConcurrentQueue<string> IncomingMessages = new();
        private static readonly Queue<Action> MainThreadActions = new();
        private static readonly object SendLock = new();

        private static TcpListener _listener;
        private static CancellationTokenSource _listenerCts;
        private static CancellationTokenSource _receiveCts;
        private static TcpClient _client;
        private static WebSocket _socket;
        private static DateTime _lastHeartbeatSent = DateTime.MinValue;
        private static DateTime _lastContextSent = DateTime.MinValue;
        private static bool _contextDirty = true;
        private static string _sessionId = Guid.NewGuid().ToString();
        private static McpConnectionState _state = McpConnectionState.Disconnected;
        private static bool _isCompilingOrReloading = false;

        public static event Action<McpConnectionState> StateChanged;

        public static McpConnectionState State => _state;

        public static bool IsConnected => _socket != null && _socket.State == WebSocketState.Open;

        public static string SessionId => _sessionId;

        static McpBridgeService()
        {
            EditorApplication.update += OnEditorUpdate;
            Selection.selectionChanged += MarkContextDirty;
            EditorApplication.hierarchyChanged += MarkContextDirty;
            EditorApplication.projectChanged += MarkContextDirty;

            // コンパイル開始時に接続状態を保存
            CompilationPipeline.compilationStarted += OnCompilationStarted;

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
                    Debug.Log("MCP Bridge: Reconnecting after compilation...");
                    EditorPrefs.DeleteKey(WasConnectedBeforeCompileKey);
                    Connect();
                }
                else if (McpBridgeSettings.Instance.AutoConnectOnLoad)
                {
                    Connect();
                }
            });
        }

        public static void Connect()
        {
            if (_listener != null)
            {
                return;
            }

            _sessionId = Guid.NewGuid().ToString();
            StartListener();
        }

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

            _state = McpConnectionState.Disconnected;
            StateChanged?.Invoke(_state);
        }

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

            lock (MainThreadActions)
            {
                MainThreadActions.Enqueue(() =>
                {
                    _state = McpConnectionState.Connected;
                    StateChanged?.Invoke(_state);
                    Send(McpBridgeMessages.CreateHelloPayload(_sessionId, McpBridgeSettings.Instance.BridgeToken));
                    MarkContextDirty();
                    PushContext();
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
                });
            }
        }

        private static void OnEditorUpdate()
        {
            ProcessMainThreadActions();
            ProcessIncomingMessages();
            MaybeSendHeartbeat();
            MaybePushContext();
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
                var payload = MiniJson.Deserialize(json);
                if (McpIncomingCommand.TryParse(payload, out var command))
                {
                    lock (MainThreadActions)
                    {
                        MainThreadActions.Enqueue(() => ExecuteCommand(command));
                    }
                }
            }
        }

        private static void ExecuteCommand(McpIncomingCommand command)
        {
            try
            {
                var result = McpCommandProcessor.Execute(command);
                Send(McpBridgeMessages.CreateCommandResult(command.CommandId, true, result));
                MarkContextDirty();
            }
            catch (Exception ex)
            {
                Debug.LogError($"MCP command failed ({command.ToolName}): {ex.Message}\n{ex}");
                Send(McpBridgeMessages.CreateCommandResult(command.CommandId, false, null, ex.Message));
            }
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
            }
        }

    }
}
