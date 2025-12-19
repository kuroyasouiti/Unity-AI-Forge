using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using MCP.Editor.ServerManager;

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
        private bool _showLogSection = false;
        private bool _showProjectRegistrationSection = true;

        // AI Tool Registration State
        private Dictionary<AITool, (bool cliAvailable, bool registered)> _aiToolStatus = new();

        // Per-tool scope-specific registration status (for tools that support scopes)
        private Dictionary<(AITool tool, McpCliRegistry.RegistrationScope scope), bool> _toolScopeStatus = new();

        // Project Registration State (CLI-based)
        private bool _showConfigPreview = false;
        private McpCliRegistry.RegistrationScope _registrationScope = McpCliRegistry.RegistrationScope.User;

        // Tab-based registration UI
        private int _selectedClientTab = 0;
        private int _selectedScopeTab = 0;
        private static readonly AITool[] SupportedClients = { AITool.ClaudeCode, AITool.CodexCli, AITool.GeminiCli };
        private static readonly string[] ClientTabNames = { "Claude Code", "Codex CLI", "Gemini CLI" };
        private static readonly string[] ScopeTabNames = { "User", "Local", "Project" };
        private static readonly string[] ScopeDescriptions = {
            "User: Available in all projects - Token stored safely",
            "Local: This machine only - Token stored safely",
            "Project: Shared with team - Token NOT included (security)"
        };

        // GUI Styles (lazy initialized)
        private static GUIStyle _statusConnectedStyle;
        private static GUIStyle _statusDisconnectedStyle;
        private static GUIStyle _statusConnectingStyle;

        private static GUIStyle StatusConnectedStyle => _statusConnectedStyle ??= new GUIStyle(EditorStyles.helpBox)
        {
            normal = { textColor = new Color(0.2f, 0.7f, 0.2f) },
            fontStyle = FontStyle.Bold
        };

        private static GUIStyle StatusDisconnectedStyle => _statusDisconnectedStyle ??= new GUIStyle(EditorStyles.helpBox)
        {
            normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
        };

        private static GUIStyle StatusConnectingStyle => _statusConnectingStyle ??= new GUIStyle(EditorStyles.helpBox)
        {
            normal = { textColor = new Color(0.9f, 0.7f, 0.2f) },
            fontStyle = FontStyle.Italic
        };

        [MenuItem("Unity-AI-Forge/MCP Assistant")]
        public static void ShowWindow()
        {
            var window = GetWindow<McpBridgeWindow>("MCP Assistant");
            window.minSize = new Vector2(420f, 600f);
        }

        private void OnEnable()
        {
            McpBridgeService.StateChanged += OnStateChanged;
            McpBridgeService.ClientInfoReceived += OnClientInfoReceived;
            UpdateStatus(McpBridgeService.State);

            // Initialize Server Manager
            Application.logMessageReceived += OnLogMessageReceived;
            RefreshAllStatus();
        }

        private void RefreshAllStatus()
        {
            RefreshProjectRegistrationStatus();
        }

        private void OnDisable()
        {
            McpBridgeService.StateChanged -= OnStateChanged;
            McpBridgeService.ClientInfoReceived -= OnClientInfoReceived;
            Application.logMessageReceived -= OnLogMessageReceived;
        }

        private void OnStateChanged(McpConnectionState state)
        {
            UpdateStatus(state);
            Repaint();
        }

        private void OnClientInfoReceived(ClientInfo clientInfo)
        {
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

            // Draw connection status bar at the top
            DrawConnectionStatusBar();

            using (var scroll = new EditorGUILayout.ScrollViewScope(_mainScroll))
            {
                _mainScroll = scroll.scrollPosition;

                // Bridge Listener Section
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

                DrawSectionSeparator();

                // Project Registration Section (JSON-based)
                _showProjectRegistrationSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showProjectRegistrationSection, "Project Registration");
                if (_showProjectRegistrationSection)
                {
                    DrawProjectRegistrationSection();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                DrawSectionSeparator();

                // Command Output Section
                _showLogSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showLogSection, "Command Output");
                if (_showLogSection)
                {
                    DrawCommandLog();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        private void DrawConnectionStatusBar()
        {
            var state = McpBridgeService.State;
            var style = state switch
            {
                McpConnectionState.Connected => StatusConnectedStyle,
                McpConnectionState.Connecting => StatusConnectingStyle,
                _ => StatusDisconnectedStyle
            };

            var icon = state switch
            {
                McpConnectionState.Connected => "\u25cf",  // ●
                McpConnectionState.Connecting => "\u25cb", // ○
                _ => "\u25cb"                              // ○
            };

            var statusText = state switch
            {
                McpConnectionState.Connected => $"{icon} Connected - Session: {PreviewSessionId()}",
                McpConnectionState.Connecting => $"{icon} Listening for connections...",
                _ => $"{icon} Bridge Stopped"
            };

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(statusText, style);

                if (GUILayout.Button("Refresh All", GUILayout.Width(80)))
                {
                    RefreshAllStatus();
                    AppendLog("All status refreshed");
                }
            }
            GUILayout.Space(2f);
        }

        private static void DrawSectionSeparator()
        {
            GUILayout.Space(6f);
        }

        private void DrawBridgeSettings(McpBridgeSettings settings)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                var host = EditorGUILayout.TextField("Listen Host", settings.ServerHost);
                var port = EditorGUILayout.IntField("Listen Port", settings.ServerPort);
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

            // Advanced operations
            GUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Clear Python Cache", GUILayout.Height(20)))
                {
                    McpServerManager.ClearPythonCache();
                    AppendLog("Python cache cleared");
                }
            }
        }

        private void DrawStatusSection(McpBridgeSettings settings)
        {
            EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);

            EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);
            using (var scroll = new EditorGUILayout.ScrollViewScope(_diagnosticsScroll, GUILayout.Height(180f)))
            {
                _diagnosticsScroll = scroll.scrollPosition;
                EditorGUILayout.LabelField($"State     : {McpBridgeService.State}");
                EditorGUILayout.LabelField($"Session ID: {PreviewSessionId()}");
                EditorGUILayout.LabelField($"Bridge URL: {settings.BridgeWebSocketUrl}");
                EditorGUILayout.LabelField($"Endpoint  : {settings.McpServerUrl}");

                var clientInfo = McpBridgeService.ConnectedClientInfo;
                if (clientInfo != null)
                {
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.LabelField("Connected Client:", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"  Client  : {clientInfo.ClientName}");
                    EditorGUILayout.LabelField($"  Server  : {clientInfo.ServerName} v{clientInfo.ServerVersion}");
                    EditorGUILayout.LabelField($"  Python  : {clientInfo.PythonVersion}");
                    EditorGUILayout.LabelField($"  Platform: {clientInfo.Platform}");
                }
            }
        }


        private void DrawCommandLog()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Clear", GUILayout.Width(60)))
                    {
                        _logBuilder.Clear();
                    }
                }

                using (var scroll = new EditorGUILayout.ScrollViewScope(_logScroll, GUILayout.Height(150f)))
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

            // Verify pyproject.toml exists in working directory
            if (!File.Exists(Path.Combine(workingDirectory, "pyproject.toml")))
            {
                AppendLog("pyproject.toml not found. Ensure the MCP server project is present before executing commands.");
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

        private static string PreviewSessionId()
        {
            var session = McpBridgeService.SessionId;
            if (string.IsNullOrEmpty(session))
            {
                return "n/a";
            }

            return session.Length <= 8 ? session : session.Substring(0, 8);
        }

        private void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            // Filter Server Manager logs
            if (message.Contains("[McpServerManager]") ||
                message.Contains("[McpServerInstaller]") ||
                message.Contains("[McpConfigManager]") ||
                message.Contains("[McpToolRegistry]") ||
                message.Contains("[McpCliRegistry]"))
            {
                // Extract clean message
                var cleanMessage = message;
                if (message.Contains("]"))
                {
                    var index = message.IndexOf("]");
                    cleanMessage = message.Substring(index + 1).Trim();
                }

                AppendLog(cleanMessage);
            }

            // Project Registry logs
            if (message.Contains("[McpProjectRegistry]"))
            {
                var cleanMessage = message;
                if (message.Contains("]"))
                {
                    var index = message.IndexOf("]");
                    cleanMessage = message.Substring(index + 1).Trim();
                }

                AppendLog(cleanMessage);
            }
        }

        #region Project Registration (CLI)

        private void RefreshProjectRegistrationStatus()
        {
            _aiToolStatus.Clear();
            _toolScopeStatus.Clear();

            // サポート対象のAIツールのみチェック
            var serverName = McpProjectRegistry.GetProjectServerName();

            foreach (var tool in SupportedClients)
            {
                var supportsScope = McpCliRegistry.SupportsScope(tool);

                if (supportsScope)
                {
                    // スコープをサポートするツールの場合、各スコープごとにチェック（JSON直接登録）
                    foreach (McpCliRegistry.RegistrationScope scope in Enum.GetValues(typeof(McpCliRegistry.RegistrationScope)))
                    {
                        var registered = McpCliRegistry.IsServerRegistered(tool, serverName, scope);
                        // ツールとスコープのペアで状態を保存
                        _toolScopeStatus[(tool, scope)] = registered;
                    }

                    // 現在選択中のスコープの登録状態を使用
                    var currentScopeRegistered = _toolScopeStatus.TryGetValue((tool, _registrationScope), out var reg) && reg;
                    _aiToolStatus[tool] = (true, currentScopeRegistered);
                }
                else
                {
                    // スコープ非対応ツールは従来通り
                    var cliAvailable = McpCliRegistry.IsCliAvailable(tool);
                    var registered = cliAvailable && McpCliRegistry.IsServerRegistered(tool, serverName);
                    _aiToolStatus[tool] = (cliAvailable, registered);
                }
            }
        }

        private void DrawProjectRegistrationSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // Project info (collapsible)
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var settings = McpBridgeSettings.Instance;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Server:", GUILayout.Width(50));
                        EditorGUILayout.LabelField(McpProjectRegistry.GetProjectServerName(), EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField($"Port: {settings.ServerPort}", GUILayout.Width(80));
                    }
                }

                GUILayout.Space(5f);

                // AI Client Tabs
                EditorGUILayout.LabelField("Select AI Client", EditorStyles.boldLabel);
                var newClientTab = GUILayout.Toolbar(_selectedClientTab, ClientTabNames);
                if (newClientTab != _selectedClientTab)
                {
                    _selectedClientTab = newClientTab;
                    RefreshProjectRegistrationStatus();
                }

                var selectedClient = SupportedClients[_selectedClientTab];

                GUILayout.Space(5f);

                // Scope Tabs (for tools that support scope)
                if (McpCliRegistry.SupportsScope(selectedClient))
                {
                    DrawScopeTabs(selectedClient);
                }

                GUILayout.Space(5f);

                // Draw client-specific registration panel
                DrawClientRegistrationPanel(selectedClient);

                GUILayout.Space(5f);

                // Config preview (collapsed by default)
                _showConfigPreview = EditorGUILayout.Foldout(_showConfigPreview, "JSON Config Preview");
                if (_showConfigPreview)
                {
                    DrawJsonConfigPreviewForClient(selectedClient);
                }

                GUILayout.Space(5f);

                // Refresh button
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Refresh Status", GUILayout.Width(120)))
                    {
                        RefreshProjectRegistrationStatus();
                        AppendLog("Project registration status refreshed");
                    }
                }
            }
        }

        private void DrawScopeTabs(AITool client)
        {
            EditorGUILayout.LabelField("Registration Scope", EditorStyles.boldLabel);

            // スコープタブにステータスアイコンを追加
            var scopeTabLabels = new string[ScopeTabNames.Length];
            for (int i = 0; i < ScopeTabNames.Length; i++)
            {
                var scope = (McpCliRegistry.RegistrationScope)i;
                var isRegistered = _toolScopeStatus.TryGetValue((client, scope), out var reg) && reg;
                var icon = isRegistered ? "\u2713 " : "";
                scopeTabLabels[i] = $"{icon}{ScopeTabNames[i]}";
            }

            var newScopeTab = GUILayout.Toolbar(_selectedScopeTab, scopeTabLabels);
            if (newScopeTab != _selectedScopeTab)
            {
                _selectedScopeTab = newScopeTab;
                _registrationScope = (McpCliRegistry.RegistrationScope)_selectedScopeTab;
                Repaint(); // UIを即座に更新
            }

            // Scope description
            EditorGUILayout.HelpBox(ScopeDescriptions[_selectedScopeTab], MessageType.None);

            // Project scope warning and environment variable guide
            if (_registrationScope == McpCliRegistry.RegistrationScope.Project)
            {
                EditorGUILayout.HelpBox(
                    "Project scope stores config in a shared settings file which may be committed to git.\n" +
                    "Token is NOT included to prevent accidental exposure.\n\n" +
                    "You must set MCP_BRIDGE_TOKEN as a system environment variable.",
                    MessageType.Warning
                );

                DrawEnvironmentVariableGuide();
            }
        }

        private void DrawEnvironmentVariableGuide()
        {
            var settings = McpBridgeSettings.Instance;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Environment Variable Setup", EditorStyles.boldLabel);

                var isWindows = Application.platform == RuntimePlatform.WindowsEditor;

                if (isWindows)
                {
                    EditorGUILayout.LabelField("Windows (PowerShell - permanent):", EditorStyles.miniLabel);
                    var psCommand = $"[Environment]::SetEnvironmentVariable('MCP_BRIDGE_TOKEN', '{settings.BridgeToken}', 'User')";
                    GUI.enabled = false;
                    EditorGUILayout.TextField(psCommand);
                    GUI.enabled = true;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Copy", GUILayout.Width(60)))
                        {
                            GUIUtility.systemCopyBuffer = psCommand;
                            AppendLog("PowerShell command copied");
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("macOS/Linux (add to ~/.bashrc or ~/.zshrc):", EditorStyles.miniLabel);
                    var shellCommand = $"export MCP_BRIDGE_TOKEN=\"{settings.BridgeToken}\"";
                    GUI.enabled = false;
                    EditorGUILayout.TextField(shellCommand);
                    GUI.enabled = true;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Copy", GUILayout.Width(60)))
                        {
                            GUIUtility.systemCopyBuffer = shellCommand;
                            AppendLog("Shell command copied");
                        }
                    }
                }

                EditorGUILayout.HelpBox("Restart your terminal/IDE after setting the environment variable.", MessageType.Info);
            }
        }

        private void DrawClientRegistrationPanel(AITool client)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var clientName = McpConfigManager.GetToolDisplayName(client);

                // Get registration status for this client
                bool registered;

                if (McpCliRegistry.SupportsScope(client))
                {
                    // スコープ対応ツールの場合、現在選択中のスコープの登録状態を使用
                    registered = _toolScopeStatus.TryGetValue((client, _registrationScope), out var reg) && reg;
                }
                else
                {
                    registered = _aiToolStatus.TryGetValue(client, out var s) && s.registered;
                }

                var jsonSupported = McpCliRegistry.SupportsScope(client); // JSON登録サポート対象

                // Status display
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Config Path:", GUILayout.Width(100));
                    var configPath = McpCliRegistry.GetScopedConfigPath(client, _registrationScope);
                    EditorGUILayout.LabelField(configPath ?? "N/A", EditorStyles.miniLabel);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Status:", GUILayout.Width(100));
                    string statusText;
                    GUIStyle statusStyle;
                    if (!jsonSupported)
                    {
                        statusText = "Not Supported";
                        statusStyle = EditorStyles.miniLabel;
                    }
                    else
                    {
                        statusText = registered ? "\u2713 Registered" : "\u2717 Not Registered";
                        statusStyle = registered ? EditorStyles.boldLabel : EditorStyles.label;
                    }
                    EditorGUILayout.LabelField(statusText, statusStyle);
                }

                GUILayout.Space(10f);

                // Action buttons
                if (!jsonSupported)
                {
                    EditorGUILayout.HelpBox(
                        $"{clientName} does not support JSON registration yet.\n" +
                        "Please configure manually.",
                        MessageType.Warning
                    );
                }
                else
                {
                    // Registration/Unregistration buttons
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUI.enabled = !_commandRunning && !registered;
                        if (GUILayout.Button("Register", GUILayout.Height(30)))
                        {
                            RegisterProjectToToolViaCli(client);
                        }

                        GUI.enabled = !_commandRunning && registered;
                        if (GUILayout.Button("Unregister", GUILayout.Height(30)))
                        {
                            UnregisterProjectFromToolViaCli(client);
                        }

                        GUI.enabled = true;
                    }

                    if (McpCliRegistry.SupportsScope(client))
                    {
                        GUILayout.Space(5f);
                        EditorGUILayout.HelpBox(
                            "Note: Registration scope affects where the configuration is stored.\n" +
                            "User scope is recommended for personal use.",
                            MessageType.Info
                        );
                    }
                }
            }
        }

        private void DrawJsonConfigPreviewForClient(AITool client)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var settings = McpBridgeSettings.Instance;
                var serverName = McpProjectRegistry.GetProjectServerName();
                var clientName = McpConfigManager.GetToolDisplayName(client);

                EditorGUILayout.LabelField($"JSON Config for {clientName}:", EditorStyles.boldLabel);

                var configPreview = GenerateJsonConfigPreview(client, serverName, settings);

                GUI.enabled = false;
                EditorGUILayout.TextArea(configPreview, GUILayout.Height(120));
                GUI.enabled = true;

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Copy to Clipboard", GUILayout.Width(130)))
                    {
                        GUIUtility.systemCopyBuffer = configPreview;
                        AppendLog("JSON config copied to clipboard");
                    }
                }
            }
        }

        private string GenerateJsonConfigPreview(AITool client, string serverName, McpBridgeSettings settings)
        {
            var options = new McpCliRegistry.ProjectRegistrationOptions
            {
                ServerName = serverName,
                ServerPath = McpServerManager.SourcePath,
                BridgeToken = settings.BridgeToken,
                BridgeHost = settings.ServerHost,
                BridgePort = settings.ServerPort,
                ProjectPath = McpProjectRegistry.GetProjectPath(),
                Scope = _registrationScope
            };

            var isWindows = Application.platform == RuntimePlatform.WindowsEditor;
            var args = new Newtonsoft.Json.Linq.JArray();

            if (isWindows)
            {
                args.Add("/c");
                args.Add("uv");
            }

            args.Add("--directory");
            args.Add(options.ServerPath);
            args.Add("run");
            args.Add("unity-ai-forge");
            args.Add("--bridge-port");
            args.Add(options.BridgePort.ToString());

            var entry = new Newtonsoft.Json.Linq.JObject
            {
                ["command"] = isWindows ? "cmd" : "uv",
                ["args"] = args
            };

            // Project スコープ以外はトークンを含める
            if (_registrationScope != McpCliRegistry.RegistrationScope.Project && !string.IsNullOrEmpty(options.BridgeToken))
            {
                entry["env"] = new Newtonsoft.Json.Linq.JObject
                {
                    ["MCP_BRIDGE_TOKEN"] = options.BridgeToken
                };
            }

            // スコープに応じたプレビュー形式
            Newtonsoft.Json.Linq.JObject preview;
            switch (_registrationScope)
            {
                case McpCliRegistry.RegistrationScope.Local:
                    preview = new Newtonsoft.Json.Linq.JObject
                    {
                        ["projects"] = new Newtonsoft.Json.Linq.JObject
                        {
                            [options.ProjectPath] = new Newtonsoft.Json.Linq.JObject
                            {
                                ["mcpServers"] = new Newtonsoft.Json.Linq.JObject
                                {
                                    [serverName] = entry
                                }
                            }
                        }
                    };
                    break;
                default:
                    preview = new Newtonsoft.Json.Linq.JObject
                    {
                        ["mcpServers"] = new Newtonsoft.Json.Linq.JObject
                        {
                            [serverName] = entry
                        }
                    };
                    break;
            }

            return preview.ToString(Newtonsoft.Json.Formatting.Indented);
        }

        private string GenerateCliCommand(AITool client, string serverName, string serverPath, McpBridgeSettings settings)
        {
            var isWindows = Application.platform == RuntimePlatform.WindowsEditor;
            var uvCommand = isWindows ? "cmd /c uv" : "uv";
            var scope = ScopeTabNames[_selectedScopeTab].ToLower();
            var isProjectScope = _registrationScope == McpCliRegistry.RegistrationScope.Project;

            // スコープ対応CLIコマンドの共通フォーマット
            // --env オプションはサーバー名の後に配置（CLIの仕様）
            string GenerateScopedCliCommand(string cliCommand)
            {
                if (isProjectScope)
                {
                    return $"# Token NOT included (Project scope - use system environment variable)\n" +
                           $"{cliCommand} mcp add --scope {scope} {serverName} " +
                           $"-- {uvCommand} --directory \"{serverPath}\" run unity-ai-forge --bridge-port {settings.ServerPort}";
                }
                return $"{cliCommand} mcp add --scope {scope} {serverName} --env MCP_BRIDGE_TOKEN={settings.BridgeTokenMasked} " +
                       $"-- {uvCommand} --directory \"{serverPath}\" run unity-ai-forge --bridge-port {settings.ServerPort}";
            }

            return client switch
            {
                AITool.ClaudeCode => GenerateScopedCliCommand("claude"),
                AITool.CodexCli => GenerateScopedCliCommand("codex"),
                AITool.GeminiCli => GenerateScopedCliCommand("gemini"),
                _ => $"# {McpConfigManager.GetToolDisplayName(client)}: Configuration not available"
            };
        }

        private void RegisterProjectToToolViaCli(AITool tool)
        {
            try
            {
                _commandRunning = true;
                var toolName = McpConfigManager.GetToolDisplayName(tool);
                AppendLog($"Registering project to {toolName} via JSON...");

                var settings = McpBridgeSettings.Instance;
                var options = new McpCliRegistry.ProjectRegistrationOptions
                {
                    ServerName = McpProjectRegistry.GetProjectServerName(),
                    ServerPath = McpServerManager.SourcePath,
                    BridgeToken = settings.BridgeToken,
                    BridgeHost = settings.ServerHost,
                    BridgePort = settings.ServerPort,
                    ProjectPath = McpProjectRegistry.GetProjectPath(),
                    Scope = _registrationScope
                };

                // JSON直接書き込みを使用（CLIではなく）
                var result = McpCliRegistry.RegisterProjectViaJson(tool, options);

                RefreshProjectRegistrationStatus();

                if (result.Success)
                {
                    AppendLog($"Successfully registered to {toolName}");
                    if (!string.IsNullOrEmpty(result.Output))
                    {
                        AppendLog($"Output: {result.Output}");
                    }

                    var configPath = McpCliRegistry.GetScopedConfigPath(tool, _registrationScope);
                    EditorUtility.DisplayDialog("Success",
                        $"Successfully registered to {toolName}!\n\n" +
                        $"Server: {options.ServerName}\n" +
                        $"Port: {options.BridgePort}\n" +
                        $"Config: {configPath}\n\n" +
                        "Please restart the AI tool for changes to take effect.",
                        "OK");
                }
                else
                {
                    AppendLog($"Failed to register to {toolName}: {result.Error}");
                    EditorUtility.DisplayDialog("Registration Failed",
                        $"Failed to register to {toolName}.\n\n{result.Error}",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Failed to register project: {ex.Message}");
                EditorUtility.DisplayDialog("Error",
                    $"Failed to register:\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _commandRunning = false;
                Repaint();
            }
        }

        private void UnregisterProjectFromToolViaCli(AITool tool)
        {
            try
            {
                _commandRunning = true;
                var toolName = McpConfigManager.GetToolDisplayName(tool);
                var serverName = McpProjectRegistry.GetProjectServerName();
                AppendLog($"Unregistering project from {toolName} via JSON (scope: {_registrationScope})...");

                // JSON直接編集を使用（CLIではなく）
                var projectPath = McpProjectRegistry.GetProjectPath();
                var result = McpCliRegistry.UnregisterProjectViaJson(tool, serverName, _registrationScope, projectPath);

                RefreshProjectRegistrationStatus();

                if (result.Success)
                {
                    AppendLog($"Successfully unregistered from {toolName}");
                    if (!string.IsNullOrEmpty(result.Output))
                    {
                        AppendLog($"Output: {result.Output}");
                    }

                    EditorUtility.DisplayDialog("Success",
                        $"Successfully unregistered from {toolName}!\n\n" +
                        "Please restart the AI tool for changes to take effect.",
                        "OK");
                }
                else
                {
                    AppendLog($"Failed to unregister from {toolName}: {result.Error}");
                    EditorUtility.DisplayDialog("Unregistration Failed",
                        $"Failed to unregister from {toolName}.\n\n{result.Error}",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Failed to unregister project: {ex.Message}");
                EditorUtility.DisplayDialog("Error",
                    $"Failed to unregister:\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _commandRunning = false;
                Repaint();
            }
        }

        #endregion
    }
}


