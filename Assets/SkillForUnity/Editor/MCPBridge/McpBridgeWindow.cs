using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using MCP.Editor.ServerManager;
using System.Collections.Generic;

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
        private bool _showRegistrationSection = true;
        
        // Server Manager State
        private ServerStatus _serverStatus;
        private Dictionary<AITool, bool> _registrationStatus;
        private Dictionary<AITool, bool> _configFileStatus;
        private bool _serverManagerInitialized;

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
        [MenuItem("Skill for Unity/MCP Assistant")]
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
            RefreshServerManagerStatus();
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

                _showServerManagerSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showServerManagerSection, "MCP Server Manager");
                if (_showServerManagerSection)
                {
                    DrawServerManagerSection();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                GUILayout.Space(4f);

                _showRegistrationSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showRegistrationSection, "AI Tool Registration");
                if (_showRegistrationSection)
                {
                    DrawRegistrationSection();
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
        
        #region Server Manager Integration
        
        private void RefreshServerManagerStatus()
        {
            try
            {
                _serverStatus = McpServerManager.GetStatus();
                _registrationStatus = McpToolRegistry.GetRegistrationStatus();
                _configFileStatus = McpConfigManager.GetAllConfigStatus();
                _serverManagerInitialized = true;
            }
            catch (Exception ex)
            {
                AppendLog($"Failed to refresh server manager status: {ex.Message}");
                _serverManagerInitialized = false;
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
        
        private void DrawRegistrationSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_serverStatus == null || !_serverStatus.IsInstalled)
                {
                    EditorGUILayout.HelpBox("Please install the MCP server first.", MessageType.Info);
                    return;
                }
                
                EditorGUILayout.LabelField("AI Tool Registration", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Register the MCP server by updating configuration files.\n" +
                    "Backups are automatically created before any changes.",
                    MessageType.Info
                );
                
                GUILayout.Space(5f);
                
                // Individual tool registration
                foreach (AITool tool in Enum.GetValues(typeof(AITool)))
                {
                    DrawToolRegistrationRow(tool);
                }
                
                GUILayout.Space(5f);
                
                // Bulk actions
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = !_commandRunning;
                    
                    if (GUILayout.Button("Register All", GUILayout.Height(25)))
                    {
                        ExecuteRegistrationAction(() =>
                        {
                            McpToolRegistry.RegisterAll();
                            RefreshServerManagerStatus();
                            AppendLog("Registered to all available tools");
                        });
                    }
                    
                    if (GUILayout.Button("Unregister All", GUILayout.Height(25)))
                    {
                        if (EditorUtility.DisplayDialog("Confirm Unregister All",
                            "Are you sure you want to unregister from all AI tools?",
                            "Yes", "No"))
                        {
                            ExecuteRegistrationAction(() =>
                            {
                                McpToolRegistry.UnregisterAll();
                                RefreshServerManagerStatus();
                                AppendLog("Unregistered from all tools");
                            });
                        }
                    }
                    
                    if (GUILayout.Button("Refresh Status", GUILayout.Height(25)))
                    {
                        RefreshServerManagerStatus();
                        AppendLog("Registration status refreshed");
                    }
                    
                    GUI.enabled = true;
                }
            }
        }
        
        private void DrawToolRegistrationRow(AITool tool)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var isRegistered = _registrationStatus != null && _registrationStatus.ContainsKey(tool) && _registrationStatus[tool];
                    var configExists = _configFileStatus != null && _configFileStatus.ContainsKey(tool) && _configFileStatus[tool];
                    
                    // Status icons
                    var registrationIcon = isRegistered ? "✅" : "⭕";
                    var configIcon = configExists ? "📄" : "❌";
                    
                    var displayName = McpConfigManager.GetToolDisplayName(tool);
                    EditorGUILayout.LabelField($"{registrationIcon} {displayName}", EditorStyles.boldLabel, GUILayout.Width(150));
                    
                    if (!configExists)
                    {
                        EditorGUILayout.LabelField($"{configIcon} Config not found", EditorStyles.miniLabel, GUILayout.Width(120));
                    }
                    else if (isRegistered)
                    {
                        EditorGUILayout.LabelField($"{configIcon} Registered", EditorStyles.miniLabel, GUILayout.Width(120));
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"{configIcon} Not registered", EditorStyles.miniLabel, GUILayout.Width(120));
                    }
                    
                    GUILayout.FlexibleSpace();
                    
                    GUI.enabled = !_commandRunning;
                    
                    if (isRegistered)
                    {
                        if (GUILayout.Button("Unregister", GUILayout.Width(90)))
                        {
                            ExecuteToolAction(tool, "Unregister", () => McpToolRegistry.Unregister(tool));
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Register", GUILayout.Width(90)))
                        {
                            ExecuteToolAction(tool, "Register", () => McpToolRegistry.Register(tool));
                        }
                    }
                    
                    // Backup button
                    if (configExists && GUILayout.Button("📦", GUILayout.Width(30)))
                    {
                        ExecuteToolAction(tool, "Backup", () => McpConfigManager.BackupConfig(tool));
                    }
                    
                    // Open config file button
                    if (GUILayout.Button("📂", GUILayout.Width(30)))
                    {
                        OpenConfigFile(tool);
                    }
                    
                    GUI.enabled = true;
                }
                
                // Show config file path
                var configPath = McpConfigManager.GetConfigPath(tool);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Path:", GUILayout.Width(35));
                    EditorGUILayout.SelectableLabel(configPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                }
                
                GUILayout.Space(2f);
            }
        }
        
        private void OpenConfigFile(AITool tool)
        {
            try
            {
                var path = McpConfigManager.GetConfigPath(tool);
                var directory = Path.GetDirectoryName(path);
                
                if (File.Exists(path))
                {
                    // ファイルが存在する場合は開く
                    System.Diagnostics.Process.Start(path);
                    AppendLog($"Opened config file: {path}");
                }
                else if (Directory.Exists(directory))
                {
                    // ファイルはないがディレクトリがある場合はディレクトリを開く
                    System.Diagnostics.Process.Start(directory);
                    AppendLog($"Opened config directory: {directory}");
                }
                else
                {
                    // どちらもない場合
                    AppendLog($"Config file not found: {path}");
                    EditorUtility.DisplayDialog("Config Not Found", 
                        $"Configuration file not found:\n{path}\n\nPlease ensure {McpConfigManager.GetToolDisplayName(tool)} is installed and has been run at least once.", 
                        "OK");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Failed to open config: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to open config file:\n{ex.Message}", "OK");
            }
        }
        
        private void ExecuteToolAction(AITool tool, string action, Action actionFunc)
        {
            try
            {
                _commandRunning = true;
                var displayName = McpConfigManager.GetToolDisplayName(tool);
                AppendLog($"[{displayName}] Executing {action}...");
                
                actionFunc();
                
                RefreshServerManagerStatus();
                AppendLog($"[{displayName}] {action} successful!");
                Repaint();
            }
            catch (Exception ex)
            {
                var displayName = McpConfigManager.GetToolDisplayName(tool);
                AppendLog($"[{displayName}] {action} failed: {ex.Message}");
                EditorUtility.DisplayDialog($"{displayName} {action} Failed", 
                    $"Failed to {action.ToLower()} {displayName}.\n\n{ex.Message}", "OK");
            }
            finally
            {
                _commandRunning = false;
            }
        }
        
        private void ExecuteRegistrationAction(Action action)
        {
            try
            {
                _commandRunning = true;
                action();
                Repaint();
            }
            catch (Exception ex)
            {
                AppendLog($"Action failed: {ex.Message}");
                EditorUtility.DisplayDialog("Action Failed", ex.Message, "OK");
            }
            finally
            {
                _commandRunning = false;
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
                message.Contains("[McpToolRegistry]"))
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
        }
        
        #endregion
    }
}


