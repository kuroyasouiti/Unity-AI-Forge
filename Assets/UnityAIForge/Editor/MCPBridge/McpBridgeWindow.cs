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
        private bool _showCustomConfigSection = false;
        
        // Server Manager State
        private ServerStatus _serverStatus;
        private bool _serverManagerInitialized;
        
        // Custom Config State
        private string _customConfigPath = "";
        private bool _customConfigExists = false;
        private bool _customConfigHasServer = false;

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

                _showCustomConfigSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showCustomConfigSection, "Config File Manager");
                if (_showCustomConfigSection)
                {
                    DrawCustomConfigSection();
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
        
        #region Custom Config File
        
        private void DrawCustomConfigSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_serverStatus == null || !_serverStatus.IsInstalled)
                {
                    EditorGUILayout.HelpBox("Please install the MCP server first.", MessageType.Info);
                    return;
                }
                
                EditorGUILayout.LabelField("Configuration File Manager", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Specify a configuration file to add or remove MCP server settings.\n" +
                    "Supports all AI tools and custom configurations.",
                    MessageType.Info
                );
                
                GUILayout.Space(5f);
                
                // File path selection
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Config File Path", EditorStyles.boldLabel);
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        _customConfigPath = EditorGUILayout.TextField(_customConfigPath);
                        if (EditorGUI.EndChangeCheck())
                        {
                            CheckCustomConfigStatus();
                        }
                        
                        if (GUILayout.Button("Browse...", GUILayout.Width(80)))
                        {
                            var defaultPath = string.IsNullOrEmpty(_customConfigPath) 
                                ? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                                : Path.GetDirectoryName(_customConfigPath);
                                
                            var selectedPath = EditorUtility.OpenFilePanel(
                                "Select Config File", 
                                defaultPath, 
                                "json");
                                
                            if (!string.IsNullOrEmpty(selectedPath))
                            {
                                _customConfigPath = selectedPath;
                                CheckCustomConfigStatus();
                            }
                        }
                    }
                    
                    // Quick select buttons for common locations
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Cursor", GUILayout.Width(80)))
                        {
                            _customConfigPath = McpConfigManager.GetConfigPath(AITool.Cursor);
                            CheckCustomConfigStatus();
                        }
                        
                        if (GUILayout.Button("Claude Desktop", GUILayout.Width(110)))
                        {
                            _customConfigPath = McpConfigManager.GetConfigPath(AITool.ClaudeDesktop);
                            CheckCustomConfigStatus();
                        }
                        
                        if (GUILayout.Button("Cline", GUILayout.Width(80)))
                        {
                            _customConfigPath = McpConfigManager.GetConfigPath(AITool.Cline);
                            CheckCustomConfigStatus();
                        }
                        
                        if (GUILayout.Button("Windsurf", GUILayout.Width(80)))
                        {
                            _customConfigPath = McpConfigManager.GetConfigPath(AITool.Windsurf);
                            CheckCustomConfigStatus();
                        }
                    }
                }
                
                GUILayout.Space(5f);
                
                // Status display
                if (!string.IsNullOrEmpty(_customConfigPath))
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField("File Status", EditorStyles.boldLabel);
                        
                        var existsIcon = _customConfigExists ? "✅" : "❌";
                        EditorGUILayout.LabelField($"{existsIcon} File Exists", _customConfigExists ? "Yes" : "No");
                        
                        if (_customConfigExists)
                        {
                            var serverIcon = _customConfigHasServer ? "✅" : "⭕";
                            EditorGUILayout.LabelField($"{serverIcon} Server Entry", 
                                _customConfigHasServer ? "Registered" : "Not Registered");
                        }
                    }
                    
                    GUILayout.Space(5f);
                    
                    // Actions
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUI.enabled = !string.IsNullOrEmpty(_customConfigPath) && !_commandRunning;
                        
                        if (GUILayout.Button("➕ Add Server Entry", GUILayout.Height(30)))
                        {
                            AddServerToCustomConfig();
                        }
                        
                        GUI.enabled = _customConfigExists && _customConfigHasServer && !_commandRunning;
                        
                        if (GUILayout.Button("➖ Remove Server Entry", GUILayout.Height(30)))
                        {
                            RemoveServerFromCustomConfig();
                        }
                        
                        GUI.enabled = true;
                    }
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUI.enabled = _customConfigExists && !_commandRunning;
                        
                        if (GUILayout.Button("📦 Create Backup", GUILayout.Height(25)))
                        {
                            CreateCustomConfigBackup();
                        }
                        
                        if (GUILayout.Button("📂 Open File", GUILayout.Height(25)))
                        {
                            OpenCustomConfigFile();
                        }
                        
                        if (GUILayout.Button("🔄 Refresh Status", GUILayout.Height(25)))
                        {
                            CheckCustomConfigStatus();
                            AppendLog("Custom config status refreshed");
                        }
                        
                        GUI.enabled = true;
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Please specify a configuration file path.", MessageType.Warning);
                }
            }
        }
        
        private void CheckCustomConfigStatus()
        {
            try
            {
                if (string.IsNullOrEmpty(_customConfigPath))
                {
                    _customConfigExists = false;
                    _customConfigHasServer = false;
                    return;
                }
                
                _customConfigExists = File.Exists(_customConfigPath);
                
                if (_customConfigExists)
                {
                    try
                    {
                        var json = File.ReadAllText(_customConfigPath);
                        var config = Newtonsoft.Json.Linq.JObject.Parse(json);
                        
                        if (config.ContainsKey("mcpServers"))
                        {
                            var mcpServers = config["mcpServers"] as Newtonsoft.Json.Linq.JObject;
                            _customConfigHasServer = mcpServers != null && 
                                                     mcpServers.ContainsKey(McpServerManager.ServerName);
                        }
                        else
                        {
                            _customConfigHasServer = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"Failed to parse config file: {ex.Message}");
                        _customConfigHasServer = false;
                    }
                }
                else
                {
                    _customConfigHasServer = false;
                }
                
                Repaint();
            }
            catch (Exception ex)
            {
                AppendLog($"Failed to check custom config status: {ex.Message}");
                _customConfigExists = false;
                _customConfigHasServer = false;
            }
        }
        
        private void AddServerToCustomConfig()
        {
            try
            {
                _commandRunning = true;
                AppendLog($"Adding server entry to: {_customConfigPath}");
                
                // Create directory if needed
                var directory = Path.GetDirectoryName(_customConfigPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    AppendLog($"Created directory: {directory}");
                }
                
                // Load or create config
                Newtonsoft.Json.Linq.JObject config;
                if (File.Exists(_customConfigPath))
                {
                    // Backup first
                    var backupPath = _customConfigPath + ".backup." + DateTime.Now.ToString("yyyyMMddHHmmss");
                    File.Copy(_customConfigPath, backupPath, true);
                    AppendLog($"Backup created: {backupPath}");
                    
                    var json = File.ReadAllText(_customConfigPath);
                    config = Newtonsoft.Json.Linq.JObject.Parse(json);
                }
                else
                {
                    config = new Newtonsoft.Json.Linq.JObject();
                    AppendLog("Creating new config file");
                }
                
                // Add mcpServers section if needed
                if (!config.ContainsKey("mcpServers"))
                {
                    config["mcpServers"] = new Newtonsoft.Json.Linq.JObject();
                }
                
                var mcpServers = config["mcpServers"] as Newtonsoft.Json.Linq.JObject;
                
                // Check if already exists
                if (mcpServers.ContainsKey(McpServerManager.ServerName))
                {
                    AppendLog($"Server entry '{McpServerManager.ServerName}' already exists. Updating...");
                }
                
                // Add server entry
                var installPath = McpServerManager.UserInstallPath;
                mcpServers[McpServerManager.ServerName] = new Newtonsoft.Json.Linq.JObject
                {
                    ["command"] = "uv",
                    ["args"] = new Newtonsoft.Json.Linq.JArray
                    {
                        "--directory",
                        installPath,
                        "run",
                        McpServerManager.ServerName
                    }
                };
                
                // Save
                var formattedJson = config.ToString(Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(_customConfigPath, formattedJson);
                
                CheckCustomConfigStatus();
                AppendLog($"✅ Server entry added successfully to: {_customConfigPath}");
                EditorUtility.DisplayDialog("Success", 
                    "Server entry added successfully!\n\nPlease restart your AI tool for changes to take effect.", 
                    "OK");
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Failed to add server entry: {ex.Message}");
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to add server entry:\n\n{ex.Message}", 
                    "OK");
            }
            finally
            {
                _commandRunning = false;
            }
        }
        
        private void RemoveServerFromCustomConfig()
        {
            try
            {
                _commandRunning = true;
                AppendLog($"Removing server entry from: {_customConfigPath}");
                
                if (!File.Exists(_customConfigPath))
                {
                    AppendLog("Config file not found");
                    return;
                }
                
                // Backup first
                var backupPath = _customConfigPath + ".backup." + DateTime.Now.ToString("yyyyMMddHHmmss");
                File.Copy(_customConfigPath, backupPath, true);
                AppendLog($"Backup created: {backupPath}");
                
                // Load config
                var json = File.ReadAllText(_customConfigPath);
                var config = Newtonsoft.Json.Linq.JObject.Parse(json);
                
                if (config.ContainsKey("mcpServers"))
                {
                    var mcpServers = config["mcpServers"] as Newtonsoft.Json.Linq.JObject;
                    
                    if (mcpServers.ContainsKey(McpServerManager.ServerName))
                    {
                        mcpServers.Remove(McpServerManager.ServerName);
                        AppendLog($"Removed server entry: {McpServerManager.ServerName}");
                    }
                    else
                    {
                        AppendLog($"Server entry '{McpServerManager.ServerName}' not found");
                    }
                }
                
                // Save
                var formattedJson = config.ToString(Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(_customConfigPath, formattedJson);
                
                CheckCustomConfigStatus();
                AppendLog($"✅ Server entry removed successfully from: {_customConfigPath}");
                EditorUtility.DisplayDialog("Success", 
                    "Server entry removed successfully!\n\nPlease restart your AI tool for changes to take effect.", 
                    "OK");
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Failed to remove server entry: {ex.Message}");
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to remove server entry:\n\n{ex.Message}", 
                    "OK");
            }
            finally
            {
                _commandRunning = false;
            }
        }
        
        private void CreateCustomConfigBackup()
        {
            try
            {
                if (!File.Exists(_customConfigPath))
                {
                    AppendLog("Config file not found");
                    EditorUtility.DisplayDialog("Error", "Config file not found", "OK");
                    return;
                }
                
                var backupPath = _customConfigPath + ".backup." + DateTime.Now.ToString("yyyyMMddHHmmss");
                File.Copy(_customConfigPath, backupPath, true);
                
                AppendLog($"✅ Backup created: {backupPath}");
                EditorUtility.DisplayDialog("Backup Created", 
                    $"Backup saved to:\n{backupPath}", 
                    "OK");
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Failed to create backup: {ex.Message}");
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to create backup:\n\n{ex.Message}", 
                    "OK");
            }
        }
        
        private void OpenCustomConfigFile()
        {
            try
            {
                if (File.Exists(_customConfigPath))
                {
                    System.Diagnostics.Process.Start(_customConfigPath);
                    AppendLog($"Opened config file: {_customConfigPath}");
                }
                else
                {
                    var directory = Path.GetDirectoryName(_customConfigPath);
                    if (Directory.Exists(directory))
                    {
                        System.Diagnostics.Process.Start(directory);
                        AppendLog($"Opened directory: {directory}");
                    }
                    else
                    {
                        AppendLog("Config file and directory not found");
                        EditorUtility.DisplayDialog("Not Found", 
                            $"Config file not found:\n{_customConfigPath}", 
                            "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Failed to open config: {ex.Message}");
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to open config file:\n\n{ex.Message}", 
                    "OK");
            }
        }
        
        #endregion
    }
}


