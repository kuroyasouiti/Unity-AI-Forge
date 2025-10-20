using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor
{
    internal sealed class McpBridgeWindow : EditorWindow
    {
        private string _statusMessage = string.Empty;
        private Vector2 _diagnosticsScroll;
        private Vector2 _logScroll;
        private Vector2 _mainScroll;
        private readonly StringBuilder _logBuilder = new StringBuilder();
        private bool _commandRunning;
        private Action<CommandResult> _pendingCommandContinuation;
        private bool _showBridgeSection = true;
        private bool _showServerSection = true;
        private bool _showLogSection = false;
        private bool _showQuickRegister = true;

        private readonly struct CliRegistration
        {
            public CliRegistration(string displayName, string cliName, string registerArgs, string unregisterArgs)
            {
                DisplayName = displayName;
                CliName = cliName;
                RegisterArgs = registerArgs;
                UnregisterArgs = unregisterArgs;
            }

            public string DisplayName { get; }
            public string CliName { get; }
            public string RegisterArgs { get; }
            public string UnregisterArgs { get; }
        }

        [MenuItem("Tools/MCP Assistant")]
        public static void ShowWindow()
        {
            var window = GetWindow<McpBridgeWindow>("MCP Assistant");
            window.minSize = new Vector2(420f, 400f);
        }

        private void OnEnable()
        {
            McpBridgeService.StateChanged += OnStateChanged;
            UpdateStatus(McpBridgeService.State);
        }

        private void OnDisable()
        {
            McpBridgeService.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(McpConnectionState state)
        {
            UpdateStatus(state);
            Repaint();
        }

        private void UpdateStatus(McpConnectionState state)
        {
            _statusMessage = state switch
            {
                McpConnectionState.Connected => $"Client connected (session {PreviewSessionId()})",
                McpConnectionState.Connecting => "Listening for MCP server connections...",
                _ => "Bridge stopped",
            };
        }

        private void OnGUI()
        {
            var settings = McpBridgeSettings.Instance;

            using (var scroll = new EditorGUILayout.ScrollViewScope(_mainScroll))
            {
                _mainScroll = scroll.scrollPosition;

                _showBridgeSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showBridgeSection, "Bridge Listener");
                if (_showBridgeSection)
                {
                    DrawBridgeSettings(settings);
                    GUILayout.Space(4f);
                    DrawBridgeControls();
                    GUILayout.Space(4f);
                    DrawStatusSection(settings);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                GUILayout.Space(4f);

                _showServerSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showServerSection, "Server Management");
                if (_showServerSection)
                {
                    DrawServerManagement(settings);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                GUILayout.Space(4f);

                _showQuickRegister = EditorGUILayout.BeginFoldoutHeaderGroup(_showQuickRegister, "Client Registration");
                if (_showQuickRegister)
                {
                    DrawQuickRegistration(settings);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                GUILayout.Space(4f);

                _showLogSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showLogSection, "Command Output");
                if (_showLogSection)
                {
                    DrawCommandLog();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        private void DrawBridgeSettings(McpBridgeSettings settings)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                var host = EditorGUILayout.TextField("Listen Host", settings.ServerHost);
                var port = EditorGUILayout.IntField("Listen Port", settings.ServerPort);

                // Token field with environment variable indicator
                if (settings.IsTokenFromEnvironment)
                {
                    EditorGUILayout.LabelField("Bridge Token", "*** (from MCP_BRIDGE_TOKEN env var) ***");
                    EditorGUILayout.HelpBox("Token is loaded from environment variable MCP_BRIDGE_TOKEN. Stored value is ignored.", MessageType.Info);
                }
                else
                {
                    var token = EditorGUILayout.TextField("Bridge Token", settings.BridgeToken);
                    if (EditorGUI.EndChangeCheck() && token != settings.BridgeToken)
                    {
                        settings.BridgeToken = token;
                    }
                    EditorGUI.BeginChangeCheck();

                    if (!string.IsNullOrEmpty(settings.BridgeTokenMasked))
                    {
                        EditorGUILayout.LabelField("Token Preview", settings.BridgeTokenMasked);
                    }
                }

                var interval = EditorGUILayout.FloatField("Context Interval (s)", settings.ContextPushIntervalSeconds);
                var autoStart = EditorGUILayout.Toggle("Auto Start on Load", settings.AutoConnectOnLoad);
                if (EditorGUI.EndChangeCheck())
                {
                    settings.ServerHost = host;
                    settings.ServerPort = port;
                    settings.ContextPushIntervalSeconds = interval;
                    settings.AutoConnectOnLoad = autoStart;
                }

                EditorGUILayout.LabelField("WebSocket URL", settings.BridgeWebSocketUrl);
                EditorGUILayout.LabelField("MCP Endpoint", settings.McpServerUrl);
            }
        }

        private void DrawBridgeControls()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
            GUI.enabled = McpBridgeService.State == McpConnectionState.Disconnected && !_commandRunning;
                if (GUILayout.Button("Start Bridge"))
                {
                    McpBridgeService.Connect();
                }

                GUI.enabled = McpBridgeService.State != McpConnectionState.Disconnected;
                if (GUILayout.Button("Stop Bridge"))
                {
                    McpBridgeService.Disconnect();
                }

                GUI.enabled = true;
            }

            GUI.enabled = McpBridgeService.IsConnected;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Push Context"))
                {
                    McpBridgeService.RequestImmediateContextPush();
                }

                if (GUILayout.Button("Ping"))
                {
                    McpBridgeService.Send(McpBridgeMessages.CreateHeartbeat());
                }
            }
            GUI.enabled = true;
        }

        private void DrawStatusSection(McpBridgeSettings settings)
        {
            EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);

            EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);
            using (var scroll = new EditorGUILayout.ScrollViewScope(_diagnosticsScroll, GUILayout.Height(140f)))
            {
                _diagnosticsScroll = scroll.scrollPosition;
                EditorGUILayout.LabelField($"State     : {McpBridgeService.State}");
                EditorGUILayout.LabelField($"Session ID: {PreviewSessionId()}");
                EditorGUILayout.LabelField($"Bridge URL: {settings.BridgeWebSocketUrl}");
                EditorGUILayout.LabelField($"Endpoint  : {settings.McpServerUrl}");
            }
        }

        private void DrawServerManagement(McpBridgeSettings settings)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                var path = EditorGUILayout.TextField("Install Path", settings.ServerInstallPath);
                if (EditorGUI.EndChangeCheck())
                {
                    settings.ServerInstallPath = path;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Browse...", GUILayout.Width(110f)))
                    {
                        var selected = EditorUtility.OpenFolderPanel("Select MCP Server Directory", settings.ServerInstallPath, string.Empty);
                        if (!string.IsNullOrEmpty(selected))
                        {
                            settings.ServerInstallPath = selected;
                        }
                    }

                    if (GUILayout.Button("Use Default", GUILayout.Width(110f)))
                    {
                        settings.UseDefaultServerInstallPath();
                    }
                }

                EditorGUILayout.SelectableLabel(settings.ServerInstallPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));

                if (!Directory.Exists(settings.ServerInstallPath))
                {
                    EditorGUILayout.HelpBox("Server install directory does not exist. Install the template or choose a valid path.", MessageType.Warning);
                }
                else if (!ServerInstallerUtility.HasPyProject(settings.ServerInstallPath))
                {
                    EditorGUILayout.HelpBox("pyproject.toml not found. Install the template or ensure the Python server project exists at this path.", MessageType.Warning);
                }
            }

            GUI.enabled = !_commandRunning;
            if (GUILayout.Button("Setup Server Template"))
            {
                SetupServer(settings);
            }

            if (GUILayout.Button("Uninstall Server"))
            {
                UninstallServer(settings);
            }

            if (GUILayout.Button("Verify Server (python -m compileall)"))
            {
                RunServerCommand("python -m compileall mcp_server", "Verify server", settings.ServerInstallPath);
            }
            GUI.enabled = true;
        }

        private void DrawQuickRegistration(McpBridgeSettings settings)
        {
            EditorGUILayout.HelpBox("Register or remove the MCP server from external clients using their CLI tools.", MessageType.Info);
            GUI.enabled = !_commandRunning;

            if (!ServerInstallerUtility.HasPyProject(settings.ServerInstallPath))
            {
                EditorGUILayout.HelpBox("Python server project not detected. Run \"Setup Server\" before registering clients.", MessageType.Warning);
                GUI.enabled = true;
                return;
            }

            var hasUv = ProcessHelper.TryGetExecutablePath("uv", out var uvPath);
            if (!hasUv)
            {
                EditorGUILayout.HelpBox("uv executable not found on PATH. Commands will call \"uv\" and may fail if it is not installed.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.SelectableLabel($"uv: {uvPath}", EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }

            var uvInvocation = hasUv ? uvPath : "uv";
            var quotedUvInvocation = QuoteForCli(uvInvocation);

            var installDirectory = settings.ServerInstallPath;
            var normalizedInstallDirectory = string.IsNullOrWhiteSpace(installDirectory)
                ? string.Empty
                : installDirectory;

            var directoryOption = string.IsNullOrEmpty(normalizedInstallDirectory)
                ? string.Empty
                : $" --directory \"{normalizedInstallDirectory}\"";

            var serverCommand = $"{quotedUvInvocation} run {directoryOption} main.py";

            var entries = new[]
            {
                new CliRegistration("Claude Code", "claude", $"mcp add unity-mcp -- {serverCommand}", "mcp remove unity-mcp"),
                new CliRegistration("Codex CLI", "codex", $"mcp add unity-mcp {serverCommand}", "mcp remove unity-mcp"),
                new CliRegistration("Gemini CLI", "gemini", $"mcp add unity-mcp {serverCommand}", "mcp remove unity-mcp"),
                new CliRegistration("Cursor CLI", "cursor", $"mcp add unity-mcp {serverCommand}", "mcp remove unity-mcp"),
            };

            foreach (var entry in entries)
            {
                DrawCliRow(entry);
            }

            GUI.enabled = true;
        }

        private void DrawCliRow(CliRegistration entry)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(entry.DisplayName, GUILayout.Width(160f));

                if (GUILayout.Button("Register", GUILayout.Width(90f)))
                {
                    RunRegisterCommand(entry);
                }

                if (GUILayout.Button("Remove", GUILayout.Width(90f)))
                {
                    RunCliCommand(($"{entry.CliName} {entry.UnregisterArgs}").Trim(), $"{entry.DisplayName} remove");
                }
            }
        }

        private void DrawCommandLog()
        {
            EditorGUILayout.LabelField("Command Output", EditorStyles.boldLabel);
            using (var scroll = new EditorGUILayout.ScrollViewScope(_logScroll, GUILayout.Height(170f)))
            {
                _logScroll = scroll.scrollPosition;
                GUI.enabled = false;
                EditorGUILayout.TextArea(_logBuilder.ToString(), GUILayout.ExpandHeight(true));
                GUI.enabled = true;
            }

            if (_commandRunning)
            {
                EditorGUILayout.HelpBox("Command currently running...", MessageType.None);
            }
        }

        private void SetupServer(McpBridgeSettings settings)
        {
            if (_commandRunning)
            {
                AppendLog("Another command is currently running. Please wait...");
                return;
            }

            var path = settings.ServerInstallPath;
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    AppendLog($"Created install directory: {path}");
                }
            }
            catch (Exception ex)
            {
                var message = $"Failed to create install directory: {ex.Message}";
                AppendLog(message);
                Debug.LogError(message);
                return;
            }

            var needsTemplate = !ServerInstallerUtility.HasPyProject(path);
            if (needsTemplate)
            {
                AppendLog("Installing server template...");
                var success = ServerInstallerUtility.InstallTemplate(path, out var message);
                AppendLog(message);
                if (!success)
                {
                    Debug.LogWarning(message);
                    return;
                }

                Debug.Log(message);
            }
            else
            {
                AppendLog("Server template detected. Skipping template installation.");
            }

            AppendLog("Server setup complete. Manage Python dependencies manually if required.");
        }

        private void UninstallServer(McpBridgeSettings settings)
        {
            if (_commandRunning)
            {
                AppendLog("Another command is currently running. Please wait...");
                return;
            }

            var path = settings.ServerInstallPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                AppendLog("Install path is empty. Nothing to uninstall.");
                return;
            }

            if (!Directory.Exists(path))
            {
                AppendLog($"Install directory not found: {path}");
                return;
            }

            var looksLikeServer = ServerInstallerUtility.HasPyProject(path);
            var title = "Uninstall MCP Server";
            var prompt = looksLikeServer
                ? $"This will delete the MCP server installation at:\n{path}\nContinue?"
                : $"The install directory does not contain a pyproject.toml.\nDelete the directory anyway?\n{path}";
            if (!EditorUtility.DisplayDialog(title, prompt, "Uninstall", "Cancel"))
            {
                return;
            }

            var success = ServerInstallerUtility.TryUninstall(path, out var uninstallMessage, force: !looksLikeServer);
            AppendLog(uninstallMessage);
            if (success)
            {
                Debug.Log(uninstallMessage);
            }
            else
            {
                Debug.LogWarning(uninstallMessage);
            }
        }

        private void RunServerCommand(string command, string description, string workingDirectory)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                AppendLog($"Command is empty for {description}.");
                return;
            }

            if (!Directory.Exists(workingDirectory))
            {
                AppendLog($"Server path not found: {workingDirectory}");
                return;
            }

            if (!ServerInstallerUtility.HasPyProject(workingDirectory))
            {
                AppendLog("pyproject.toml not found. Run \"Setup Server\" to install the template before executing commands.");
                return;
            }

            if (_commandRunning)
            {
                AppendLog("Another command is currently running. Please wait...");
                return;
            }

            _commandRunning = true;
            AppendLog($"> ({workingDirectory}) {command}");

            ProcessHelper.RunShellCommandAsync(command, workingDirectory, result => HandleCommandResult(result, description));
        }

        private void AppendLog(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (_logBuilder.Length > 0)
            {
                _logBuilder.AppendLine();
            }

            _logBuilder.Append(text);
            Repaint();
        }

        private void RunCliCommand(string command, string description, Action<CommandResult> continuation = null)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                AppendLog($"Command is empty for {description}.");
                return;
            }

            if (_commandRunning)
            {
                AppendLog("Another command is currently running. Please wait...");
                return;
            }

            _commandRunning = true;
            _pendingCommandContinuation = continuation;
            AppendLog($"> {command}");

            var workingDirectory = ResolveProjectDirectory();

            ProcessHelper.RunShellCommandAsync(command, workingDirectory, result => HandleCommandResult(result, description));
        }

        private void HandleCommandResult(CommandResult result, string description)
        {
            _commandRunning = false;
            var continuation = _pendingCommandContinuation;
            _pendingCommandContinuation = null;

            if (!string.IsNullOrWhiteSpace(result.Output))
            {
                AppendLog(result.Output.TrimEnd());
            }

            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                AppendLog(result.Error.TrimEnd());
            }

            AppendLog($"Exit code: {result.ExitCode}");

            var errorText = result.Error?.Trim();
            var alreadyRegistered = !string.IsNullOrEmpty(errorText)
                                    && errorText.IndexOf("already exists in local config", StringComparison.OrdinalIgnoreCase) >= 0;
            var isRegisterCommand = description.EndsWith("register", StringComparison.OrdinalIgnoreCase);

            if (!result.Success)
            {
                if (alreadyRegistered && isRegisterCommand)
                {
                    const string friendlyMessage = "Server already registered. Use Remove before registering again.";
                    AppendLog(friendlyMessage);
                    Debug.LogWarning($"Command completed with warning ({description}): {friendlyMessage}");
                }
                else
                {
                    Debug.LogError($"Command failed ({description}): {result.Error}");
                }
            }
            else
            {
                Debug.Log($"Command succeeded ({description}).");
            }

            Repaint();

            continuation?.Invoke(result);
        }

        private static string QuoteForCli(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            return $"\"{value.Replace("\"", "\\\"")}\"";
        }

        private void RunRegisterCommand(CliRegistration entry)
        {
            var registerCommand = ($"{entry.CliName} {entry.RegisterArgs}").Trim();
            RunCliCommand(registerCommand, $"{entry.DisplayName} register", _ =>
            {
                var listCommand = ($"{entry.CliName} mcp list").Trim();
                RunCliCommand(listCommand, $"{entry.DisplayName} list");
            });
        }

        private static string ResolveProjectDirectory()
        {
            try
            {
                var assetsPath = Application.dataPath;
                if (!string.IsNullOrEmpty(assetsPath))
                {
                    var projectPath = Path.GetDirectoryName(assetsPath);
                    if (!string.IsNullOrEmpty(projectPath) && Directory.Exists(projectPath))
                    {
                        return projectPath;
                    }
                }
            }
            catch (Exception)
            {
                // fall through to other strategies
            }

            var current = Directory.GetCurrentDirectory();
            if (!string.IsNullOrEmpty(current) && Directory.Exists(current))
            {
                return current;
            }

            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(profile) && Directory.Exists(profile))
            {
                return profile;
            }

            return string.Empty;
        }

        private void HandleRegistrationResult(string clientName, bool success, string message)
        {
            AppendLog($"[{clientName}] {message}");
            if (success)
            {
                Debug.Log($"[{clientName}] {message}");
            }
            else
            {
                Debug.LogWarning($"[{clientName}] {message}");
            }
        }

        private static string PreviewSessionId()
        {
            var session = McpBridgeService.SessionId;
            if (string.IsNullOrEmpty(session))
            {
                return "n/a";
            }

            return session.Length <= 8 ? session : session.Substring(0, 8);
        }
    }
}


