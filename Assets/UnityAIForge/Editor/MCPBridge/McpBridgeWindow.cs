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
        private bool _showServerManagerSection = true;
        private bool _showProjectRegistrationSection = true;

        // Server Manager State
        private ServerStatus _serverStatus;

        // AI Tool Registration State
        private Dictionary<AITool, (bool cliAvailable, bool registered)> _aiToolStatus = new();

        // Project Registration State (CLI-based)
        private bool _showConfigPreview = false;
        private McpCliRegistry.RegistrationScope _registrationScope = McpCliRegistry.RegistrationScope.User;

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
            RefreshServerManagerStatus();
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

                // MCP Server Manager Section
                _showServerManagerSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showServerManagerSection, "MCP Server Manager");
                if (_showServerManagerSection)
                {
                    DrawServerManagerSection();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                DrawSectionSeparator();

                // Project Registration Section (CLI-based)
                _showProjectRegistrationSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showProjectRegistrationSection, "Project Registration (CLI)");
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
        
        #region Server Manager Integration
        
        private void RefreshServerManagerStatus()
        {
            try
            {
                _serverStatus = McpServerManager.GetStatus();
            }
            catch (Exception ex)
            {
                AppendLog($"Failed to refresh server manager status: {ex.Message}");
                _serverStatus = null;
            }
        }
        
        private void DrawServerManagerSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // Server Status
                EditorGUILayout.LabelField("Server Status", EditorStyles.boldLabel);
                
                if (_serverStatus == null)
                {
                    EditorGUILayout.HelpBox("Server manager not initialized", MessageType.Warning);
                    if (GUILayout.Button("Refresh"))
                    {
                        RefreshServerManagerStatus();
                    }
                    return;
                }
                
                var statusIcon = _serverStatus.IsInstalled ? "✅" : "❌";
                EditorGUILayout.LabelField($"{statusIcon} Status", _serverStatus.IsInstalled ? "Installed" : "Not Installed");
                
                if (_serverStatus.IsInstalled)
                {
                    EditorGUILayout.LabelField("Install Path", _serverStatus.InstallPath, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField("Version", _serverStatus.Version);
                    EditorGUILayout.LabelField("Python", _serverStatus.PythonAvailable ? "✅ Available" : "❌ Not Found");
                    EditorGUILayout.LabelField("UV", _serverStatus.UvAvailable ? "✅ Available" : "❌ Not Found");
                }
                else
                {
                    EditorGUILayout.LabelField("Source Path", _serverStatus.SourcePath, EditorStyles.wordWrappedLabel);
                }
                
                GUILayout.Space(5f);
                
                // Install Path Settings
                var settings = McpBridgeSettings.Instance;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Install Path Settings", EditorStyles.boldLabel);
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Install To:", GUILayout.Width(80));
                        
                        EditorGUI.BeginChangeCheck();
                        var newPath = EditorGUILayout.TextField(settings.ServerInstallPath);
                        if (EditorGUI.EndChangeCheck())
                        {
                            settings.ServerInstallPath = newPath;
                            RefreshServerManagerStatus();
                        }
                    }
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Default", GUILayout.Width(70)))
                        {
                            settings.UseDefaultServerInstallPath();
                            RefreshServerManagerStatus();
                            AppendLog($"Reset to default path: {settings.ServerInstallPath}");
                        }
                        
                        if (GUILayout.Button("Browse...", GUILayout.Width(70)))
                        {
                            var selected = EditorUtility.OpenFolderPanel("Select Install Directory", 
                                Path.GetDirectoryName(settings.ServerInstallPath) ?? "", "");
                            if (!string.IsNullOrEmpty(selected))
                            {
                                settings.ServerInstallPath = Path.Combine(selected, "Unity-AI-Forge");
                                RefreshServerManagerStatus();
                                AppendLog($"Install path changed to: {settings.ServerInstallPath}");
                            }
                        }
                        
                        GUILayout.FlexibleSpace();
                        
                        EditorGUILayout.LabelField($"Default: {settings.DefaultServerInstallPath}", 
                            EditorStyles.miniLabel);
                    }
                }
                
                GUILayout.Space(5f);
                
                // Server Operations
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = !_serverStatus.IsInstalled && !_commandRunning;
                    if (GUILayout.Button("Install Server", GUILayout.Height(30)))
                    {
                        ExecuteServerManagerAction(() =>
                        {
                            McpServerManager.Install();
                            RefreshServerManagerStatus();
                            AppendLog("Server installed successfully!");
                        });
                    }
                    GUI.enabled = true;
                    
                    GUI.enabled = _serverStatus.IsInstalled && !_commandRunning;
                    if (GUILayout.Button("Uninstall Server", GUILayout.Height(30)))
                    {
                        if (EditorUtility.DisplayDialog("Confirm Uninstall",
                            "Are you sure you want to uninstall the MCP server?",
                            "Yes", "No"))
                        {
                            ExecuteServerManagerAction(() =>
                            {
                                McpServerManager.Uninstall();
                                RefreshServerManagerStatus();
                                AppendLog("Server uninstalled successfully!");
                            });
                        }
                    }
                    GUI.enabled = true;
                }
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = _serverStatus.IsInstalled && !_commandRunning;
                    if (GUILayout.Button("Reinstall Server", GUILayout.Height(30)))
                    {
                        ExecuteServerManagerAction(() =>
                        {
                            McpServerManager.Reinstall();
                            RefreshServerManagerStatus();
                            AppendLog("Server reinstalled successfully!");
                        });
                    }
                    GUI.enabled = true;
                    
                    if (GUILayout.Button("Refresh Status", GUILayout.Height(30)))
                    {
                        RefreshServerManagerStatus();
                        AppendLog("Status refreshed");
                    }
                }
                
                GUILayout.Space(5f);
                
                // Quick Actions
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Open Install Folder"))
                    {
                        McpServerManager.OpenInstallFolder();
                    }
                    
                    if (GUILayout.Button("Open Source Folder"))
                    {
                        McpServerManager.OpenSourceFolder();
                    }
                }
            }
        }
        
        private void ExecuteServerManagerAction(Action action)
        {
            try
            {
                action();
                Repaint();
            }
            catch (Exception ex)
            {
                AppendLog($"Error: {ex.Message}");
                EditorUtility.DisplayDialog("Error", ex.Message, "OK");
            }
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

        #endregion

        #region Project Registration (CLI)

        private void RefreshProjectRegistrationStatus()
        {
            _aiToolStatus.Clear();

            // CLI対応のAIツールのみチェック
            var cliTools = new[] { AITool.Cursor, AITool.ClaudeCode, AITool.Cline, AITool.Windsurf };
            var serverName = McpProjectRegistry.GetProjectServerName();

            foreach (var tool in cliTools)
            {
                var cliAvailable = McpCliRegistry.IsCliAvailable(tool);
                // CLIが利用可能な場合のみ登録状態をチェック
                var registered = cliAvailable && McpCliRegistry.IsServerRegistered(tool, serverName);
                _aiToolStatus[tool] = (cliAvailable, registered);
            }
        }

        private void DrawProjectRegistrationSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_serverStatus == null || !_serverStatus.IsInstalled)
                {
                    EditorGUILayout.HelpBox("Please install the MCP server first.", MessageType.Info);
                    return;
                }

                // Project info
                EditorGUILayout.LabelField("Project Info", EditorStyles.boldLabel);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Project Name", McpProjectRegistry.GetProjectName());
                    EditorGUILayout.LabelField("Server Name", McpProjectRegistry.GetProjectServerName());

                    var settings = McpBridgeSettings.Instance;
                    EditorGUILayout.LabelField("Bridge Port", settings.ServerPort.ToString());
                    EditorGUILayout.LabelField("Token", settings.BridgeTokenMasked);

                    GUILayout.Space(3f);

                    // Scope selector (for Claude Code)
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Scope", GUILayout.Width(80));
                        _registrationScope = (McpCliRegistry.RegistrationScope)EditorGUILayout.EnumPopup(_registrationScope);
                    }
                    EditorGUILayout.LabelField(
                        _registrationScope == McpCliRegistry.RegistrationScope.User
                            ? "User: Available in all projects"
                            : "Local: Only for this project",
                        EditorStyles.miniLabel);
                }

                GUILayout.Space(5f);

                EditorGUILayout.HelpBox(
                    "Register this project to AI tools via CLI.\n" +
                    "Each registration includes the bridge token and port.\n" +
                    "CLI: Command available, MCP: Supports MCP CLI commands (some tools open GUI instead)",
                    MessageType.Info
                );

                GUILayout.Space(5f);

                // Tool table header
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("AI Tool", EditorStyles.boldLabel, GUILayout.Width(120));
                    EditorGUILayout.LabelField("CLI", EditorStyles.boldLabel, GUILayout.Width(50));
                    EditorGUILayout.LabelField("MCP", EditorStyles.boldLabel, GUILayout.Width(50));
                    EditorGUILayout.LabelField("Status", EditorStyles.boldLabel, GUILayout.Width(100));
                    EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
                }

                DrawToolDivider();

                // Draw each AI tool row
                DrawProjectToolRow(AITool.Cursor);
                DrawProjectToolRow(AITool.ClaudeCode);
                DrawProjectToolRow(AITool.Cline);
                DrawProjectToolRow(AITool.Windsurf);

                GUILayout.Space(5f);

                // Batch operations
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = !_commandRunning;

                    if (GUILayout.Button("Register All", GUILayout.Height(25)))
                    {
                        RegisterProjectToAllViaCli();
                    }

                    if (GUILayout.Button("Unregister All", GUILayout.Height(25)))
                    {
                        UnregisterProjectFromAllViaCli();
                    }

                    if (GUILayout.Button("Refresh", GUILayout.Height(25)))
                    {
                        RefreshProjectRegistrationStatus();
                        AppendLog("Project registration status refreshed");
                    }

                    GUI.enabled = true;
                }

                GUILayout.Space(5f);

                // Config preview
                _showConfigPreview = EditorGUILayout.Foldout(_showConfigPreview, "Config Preview");
                if (_showConfigPreview)
                {
                    DrawCliCommandPreview();
                }
            }
        }

        private void DrawToolDivider()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        private void DrawProjectToolRow(AITool tool)
        {
            if (!_aiToolStatus.TryGetValue(tool, out var status))
            {
                status = (false, false);
            }

            var mcpSupported = McpCliRegistry.IsMcpCliSupported(tool);

            using (new EditorGUILayout.HorizontalScope())
            {
                // Tool name
                EditorGUILayout.LabelField(McpConfigManager.GetToolDisplayName(tool), GUILayout.Width(120));

                // CLI availability
                var cliIcon = status.cliAvailable ? "\u2713" : "\u2717";
                EditorGUILayout.LabelField(cliIcon, GUILayout.Width(50));

                // MCP CLI support (some CLIs open GUI app instead of running MCP commands)
                var mcpIcon = mcpSupported ? "\u2713" : "-";
                EditorGUILayout.LabelField(mcpIcon, GUILayout.Width(50));

                // Registration status
                string statusText;
                GUIStyle statusStyle;
                if (!mcpSupported)
                {
                    statusText = "Not Supported";
                    statusStyle = EditorStyles.miniLabel;
                }
                else
                {
                    statusText = status.registered ? "Registered" : "Not Registered";
                    statusStyle = status.registered ? EditorStyles.boldLabel : EditorStyles.label;
                }
                EditorGUILayout.LabelField(statusText, statusStyle, GUILayout.Width(100));

                // Actions - only enable for tools that support MCP CLI
                GUI.enabled = status.cliAvailable && mcpSupported && !_commandRunning;

                if (!status.registered)
                {
                    if (GUILayout.Button("Register", GUILayout.Width(80)))
                    {
                        RegisterProjectToToolViaCli(tool);
                    }
                }
                else
                {
                    if (GUILayout.Button("Unregister", GUILayout.Width(80)))
                    {
                        UnregisterProjectFromToolViaCli(tool);
                    }
                }

                GUI.enabled = true;
            }
        }

        private void DrawCliCommandPreview()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var settings = McpBridgeSettings.Instance;
                var serverName = McpProjectRegistry.GetProjectServerName();
                var serverPath = McpServerManager.UserInstallPath;

                EditorGUILayout.LabelField("CLI Command Preview:", EditorStyles.boldLabel);

                var command = $"<cli> mcp add {serverName} --directory \"{serverPath}\" -- --bridge-port {settings.ServerPort} --bridge-token {settings.BridgeTokenMasked}";

                GUI.enabled = false;
                EditorGUILayout.TextArea(command, GUILayout.Height(60));
                GUI.enabled = true;

                EditorGUILayout.HelpBox(
                    "Replace <cli> with: cursor, claude, cline, or windsurf",
                    MessageType.None
                );
            }
        }

        private void RegisterProjectToToolViaCli(AITool tool)
        {
            try
            {
                _commandRunning = true;
                var toolName = McpConfigManager.GetToolDisplayName(tool);
                AppendLog($"Registering project to {toolName} via CLI...");

                var settings = McpBridgeSettings.Instance;
                var options = new McpCliRegistry.ProjectRegistrationOptions
                {
                    ServerName = McpProjectRegistry.GetProjectServerName(),
                    ServerPath = McpServerManager.UserInstallPath,
                    BridgeToken = settings.BridgeToken,
                    BridgeHost = settings.ServerHost,
                    BridgePort = settings.ServerPort,
                    ProjectPath = McpProjectRegistry.GetProjectPath(),
                    Scope = _registrationScope
                };

                var result = McpCliRegistry.RegisterProject(tool, options);

                RefreshProjectRegistrationStatus();

                if (result.Success)
                {
                    AppendLog($"Successfully registered to {toolName}");
                    if (!string.IsNullOrEmpty(result.Output))
                    {
                        AppendLog($"Output: {result.Output}");
                    }

                    EditorUtility.DisplayDialog("Success",
                        $"Successfully registered to {toolName}!\n\n" +
                        $"Server: {options.ServerName}\n" +
                        $"Port: {options.BridgePort}\n\n" +
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
                AppendLog($"Unregistering project from {toolName} via CLI (scope: {_registrationScope})...");

                var result = McpCliRegistry.UnregisterProject(tool, serverName, _registrationScope);

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

        private void RegisterProjectToAllViaCli()
        {
            var successCount = 0;
            var failCount = 0;
            var settings = McpBridgeSettings.Instance;

            foreach (var kvp in _aiToolStatus)
            {
                var tool = kvp.Key;
                var (cliAvailable, registered) = kvp.Value;

                // Skip if CLI not available, already registered, or MCP CLI not supported
                if (!cliAvailable || registered || !McpCliRegistry.IsMcpCliSupported(tool))
                {
                    continue;
                }

                try
                {
                    var toolName = McpConfigManager.GetToolDisplayName(tool);
                    AppendLog($"Registering to {toolName}...");

                    var options = new McpCliRegistry.ProjectRegistrationOptions
                    {
                        ServerName = McpProjectRegistry.GetProjectServerName(),
                        ServerPath = McpServerManager.UserInstallPath,
                        BridgeToken = settings.BridgeToken,
                        BridgeHost = settings.ServerHost,
                        BridgePort = settings.ServerPort,
                        ProjectPath = McpProjectRegistry.GetProjectPath(),
                        Scope = _registrationScope
                    };

                    var result = McpCliRegistry.RegisterProject(tool, options);

                    if (result.Success)
                    {
                        successCount++;
                        AppendLog($"{toolName} registered");
                    }
                    else
                    {
                        failCount++;
                        AppendLog($"{toolName} failed: {result.Error}");
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    AppendLog($"{McpConfigManager.GetToolDisplayName(tool)} failed: {ex.Message}");
                }
            }

            RefreshProjectRegistrationStatus();
            AppendLog($"Batch registration completed: {successCount} succeeded, {failCount} failed");

            if (successCount > 0)
            {
                EditorUtility.DisplayDialog("Batch Registration Complete",
                    $"Registered to {successCount} AI tool(s).\n\n" +
                    "Please restart the AI tools for changes to take effect.",
                    "OK");
            }
        }

        private void UnregisterProjectFromAllViaCli()
        {
            var successCount = 0;
            var failCount = 0;
            var serverName = McpProjectRegistry.GetProjectServerName();

            foreach (var kvp in _aiToolStatus)
            {
                var tool = kvp.Key;
                var (cliAvailable, registered) = kvp.Value;

                // Skip if CLI not available, not registered, or MCP CLI not supported
                if (!cliAvailable || !registered || !McpCliRegistry.IsMcpCliSupported(tool))
                {
                    continue;
                }

                try
                {
                    var toolName = McpConfigManager.GetToolDisplayName(tool);
                    AppendLog($"Unregistering from {toolName} (scope: {_registrationScope})...");

                    var result = McpCliRegistry.UnregisterProject(tool, serverName, _registrationScope);

                    if (result.Success)
                    {
                        successCount++;
                        AppendLog($"{toolName} unregistered");
                    }
                    else
                    {
                        failCount++;
                        AppendLog($"{toolName} failed: {result.Error}");
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    AppendLog($"{McpConfigManager.GetToolDisplayName(tool)} failed: {ex.Message}");
                }
            }

            RefreshProjectRegistrationStatus();
            AppendLog($"Batch unregistration completed: {successCount} succeeded, {failCount} failed");

            if (successCount > 0)
            {
                EditorUtility.DisplayDialog("Batch Unregistration Complete",
                    $"Unregistered from {successCount} AI tool(s).\n\n" +
                    "Please restart the AI tools for changes to take effect.",
                    "OK");
            }
        }

        #endregion
    }
}


