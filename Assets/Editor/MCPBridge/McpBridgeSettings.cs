using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor
{
    [FilePath("ProjectSettings/McpBridgeSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class McpBridgeSettings : ScriptableSingleton<McpBridgeSettings>
    {
        [SerializeField] private string serverHost = "127.0.0.1";
        [SerializeField] private int serverPort = 7070;
        [SerializeField] private string bridgeToken = string.Empty;
        [SerializeField] private bool autoConnectOnLoad = true;
        [SerializeField] private float contextPushIntervalSeconds = 5f;
        [SerializeField] private string serverInstallPath = string.Empty;
        private const string ServerInstallFolderName = "MCPServer";
        private const string LegacyServerFolderName = "mcp-server";
        private static readonly string LegacyNestedSuffix = Path.Combine(LegacyServerFolderName, ServerInstallFolderName);

        public static McpBridgeSettings Instance
        {
            get
            {
                var instance = ScriptableSingleton<McpBridgeSettings>.instance;
                instance.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                return instance;
            }
        }

        public string ServerHost
        {
            get => serverHost;
            set
            {
                var normalized = NormalizeHost(value);
                if (serverHost == normalized)
                {
                    return;
                }

                serverHost = normalized;
                SaveSettings();
            }
        }

        public int ServerPort
        {
            get => serverPort;
            set
            {
                if (serverPort == value)
                {
                    return;
                }

                var clamped = Mathf.Max(1, value);
                if (serverPort == clamped)
                {
                    return;
                }

                serverPort = clamped;
                SaveSettings();
            }
        }

        public string ServerInstallPath
        {
            get
            {
                var path = string.IsNullOrEmpty(serverInstallPath) ? DefaultServerInstallPath : serverInstallPath;
                return NormalizeInstallPath(path);
            }
            set
            {
                var normalized = NormalizeInstallPath(value);
                if (serverInstallPath == normalized)
                {
                    return;
                }

                serverInstallPath = normalized;
                SaveSettings();
            }
        }

        public string DefaultServerInstallPath
        {
            get
            {
#if UNITY_EDITOR_WIN
                var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(basePath, "UnityMCP", ServerInstallFolderName);
#elif UNITY_EDITOR_OSX
                var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                return Path.Combine(home, "Library", "Application Support", "UnityMCP", ServerInstallFolderName);
#else
                var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                return Path.Combine(home, ".unitymcp", ServerInstallFolderName);
#endif
            }
        }

        /// <summary>
        /// Gets or sets the bridge authentication token.
        /// Priority: 1) Environment variable MCP_BRIDGE_TOKEN, 2) Stored value.
        /// Use environment variables in CI/CD or shared environments to avoid committing secrets.
        /// </summary>
        public string BridgeToken
        {
            get
            {
                // First check environment variable
                var envToken = Environment.GetEnvironmentVariable("MCP_BRIDGE_TOKEN");
                if (!string.IsNullOrWhiteSpace(envToken))
                {
                    return envToken;
                }

                // Fallback to stored value
                return bridgeToken;
            }
            set
            {
                if (bridgeToken == value)
                {
                    return;
                }

                bridgeToken = value;
                SaveSettings();
            }
        }

        /// <summary>
        /// Gets the stored (non-environment) token value for display purposes.
        /// Returns masked version to avoid accidental exposure in logs/UI.
        /// </summary>
        public string BridgeTokenMasked
        {
            get
            {
                if (string.IsNullOrEmpty(bridgeToken))
                {
                    return string.Empty;
                }

                if (bridgeToken.Length <= 8)
                {
                    return new string('*', bridgeToken.Length);
                }

                return bridgeToken.Substring(0, 4) + new string('*', bridgeToken.Length - 8) + bridgeToken.Substring(bridgeToken.Length - 4);
            }
        }

        /// <summary>
        /// Checks if token is loaded from environment variable.
        /// </summary>
        public bool IsTokenFromEnvironment => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MCP_BRIDGE_TOKEN"));

        public bool AutoConnectOnLoad
        {
            get => autoConnectOnLoad;
            set
            {
                if (autoConnectOnLoad == value)
                {
                    return;
                }

                autoConnectOnLoad = value;
                SaveSettings();
            }
        }

        public float ContextPushIntervalSeconds
        {
            get => Mathf.Max(1f, contextPushIntervalSeconds);
            set
            {
                var clamped = Mathf.Max(1f, value);
                if (Mathf.Approximately(contextPushIntervalSeconds, clamped))
                {
                    return;
                }

                contextPushIntervalSeconds = clamped;
                SaveSettings();
            }
        }

        public string ListenerPrefix => $"http://{NormalizeHostForUri(serverHost)}:{serverPort}/";

        public string BridgeWebSocketUrl => $"ws://{NormalizeHostForUri(serverHost)}:{serverPort}/bridge";

        public string McpServerUrl => $"http://{NormalizeHostForUri(serverHost)}:{serverPort}/mcp";

        public void UseDefaultServerInstallPath()
        {
            serverInstallPath = string.Empty;
            SaveSettings();
        }

        public void SaveSettings()
        {
            EditorUtility.SetDirty(this);
            Save(true);
            AssetDatabase.SaveAssets();
        }

        private static string NormalizeHost(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "127.0.0.1" : value.Trim();
        }

        private static string NormalizeHostForUri(string value)
        {
            var normalized = NormalizeHost(value);
            if (normalized.Contains(":") &&
                !(normalized.StartsWith("[", StringComparison.Ordinal) &&
                  normalized.EndsWith("]", StringComparison.Ordinal)))
            {
                return $"[{normalized}]";
            }

            return normalized;
        }

        private static string NormalizeInstallPath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var trimmed = value.Trim();

            try
            {
                trimmed = Path.GetFullPath(trimmed);
            }
            catch (Exception)
            {
                // keep trimmed fallback
            }

            return EnsureServerFolder(trimmed);
        }

        private static string EnsureServerFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            var normalized = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.IsNullOrEmpty(normalized))
            {
                return ServerInstallFolderName;
            }

            if (normalized.EndsWith(LegacyNestedSuffix, StringComparison.OrdinalIgnoreCase))
            {
                var legacyParent = Path.GetDirectoryName(normalized);
                var grandParent = string.IsNullOrEmpty(legacyParent) ? null : Path.GetDirectoryName(legacyParent);
                if (string.IsNullOrEmpty(grandParent))
                {
                    return ServerInstallFolderName;
                }

                return Path.Combine(grandParent, ServerInstallFolderName);
            }

            var folderName = Path.GetFileName(normalized);
            if (string.Equals(folderName, ServerInstallFolderName, StringComparison.OrdinalIgnoreCase))
            {
                return normalized;
            }

            if (string.Equals(folderName, LegacyServerFolderName, StringComparison.OrdinalIgnoreCase))
            {
                var parent = Path.GetDirectoryName(normalized);
                if (string.IsNullOrEmpty(parent))
                {
                    return ServerInstallFolderName;
                }

                return Path.Combine(parent, ServerInstallFolderName);
            }

            return Path.Combine(normalized, ServerInstallFolderName);
        }
    }
}
