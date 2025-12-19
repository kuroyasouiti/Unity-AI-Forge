using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor
{
    [FilePath("ProjectSettings/McpBridgeSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class McpBridgeSettings : ScriptableSingleton<McpBridgeSettings>
    {
        private const string TokenFileName = ".mcp_bridge_token";

        private static bool _hideFlagsInitialized;

        [SerializeField] private string serverHost = "127.0.0.1";
        [SerializeField] private int serverPort = 7070;
        [SerializeField] private bool autoConnectOnLoad = true;
        [SerializeField] private float contextPushIntervalSeconds = 5f;
        [SerializeField] private string serverInstallPath = string.Empty;

        // Cached token loaded from file (thread-safe access via lock)
        private string _cachedToken;
        private readonly object _tokenLock = new object();

        static McpBridgeSettings()
        {
            // Ensure hideFlags is set on the main thread
            EditorApplication.delayCall += EnsureHideFlagsOnMainThread;
        }

        public static McpBridgeSettings Instance
        {
            get
            {
                var instance = ScriptableSingleton<McpBridgeSettings>.instance;
                // hideFlags is applied on the main thread via EnsureHideFlagsOnMainThread
                return instance;
            }
        }

        private static void EnsureHideFlagsOnMainThread()
        {
            if (_hideFlagsInitialized)
            {
                return;
            }

            var inst = ScriptableSingleton<McpBridgeSettings>.instance;
            inst.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            _hideFlagsInitialized = true;
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
                // Use McpServerManager's default path
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(userProfile, "Unity-AI-Forge");
            }
        }

        /// <summary>
        /// Gets or sets the bridge authentication token.
        /// Priority: 1) Environment variable MCP_BRIDGE_TOKEN, 2) Token file (.mcp_bridge_token).
        /// Use environment variables in CI/CD or shared environments to avoid committing secrets.
        /// </summary>
        public string BridgeToken
        {
            get
            {
                // Check environment variable first
                var envToken = Environment.GetEnvironmentVariable("MCP_BRIDGE_TOKEN");
                if (!string.IsNullOrWhiteSpace(envToken))
                {
                    return envToken.Trim();
                }

                // Load from file
                return LoadTokenFromFile();
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                lock (_tokenLock)
                {
                    var trimmed = value.Trim();
                    var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                    SaveTokenToFile(trimmed, projectRoot);
                }
            }
        }

        /// <summary>
        /// Generates a new unique token and saves it.
        /// </summary>
        /// <returns>The newly generated token.</returns>
        public string GenerateToken()
        {
            lock (_tokenLock)
            {
                var token = GenerateSecureToken();
                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                SaveTokenToFile(token, projectRoot);
                return token;
            }
        }

        /// <summary>
        /// Checks if a given token is valid.
        /// Uses constant-time comparison to prevent timing attacks.
        /// </summary>
        public bool IsValidToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var validToken = BridgeToken;
            if (string.IsNullOrEmpty(validToken))
            {
                return false;
            }

            return ConstantTimeEquals(token.Trim(), validToken);
        }

        /// <summary>
        /// Checks if a token is configured.
        /// </summary>
        public bool HasToken => !string.IsNullOrEmpty(BridgeToken);

        /// <summary>
        /// Gets the stored token value for display purposes.
        /// Returns masked version to avoid accidental exposure in logs/UI.
        /// </summary>
        public string BridgeTokenMasked
        {
            get
            {
                var token = BridgeToken;
                return string.IsNullOrEmpty(token) ? string.Empty : MaskToken(token);
            }
        }

        private static string MaskToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return string.Empty;
            }

            if (token.Length <= 8)
            {
                return new string('*', token.Length);
            }

            return token.Substring(0, 4) + new string('*', token.Length - 8) + token.Substring(token.Length - 4);
        }

        /// <summary>
        /// Checks if token is loaded from environment variable.
        /// </summary>
        public bool IsTokenFromEnvironment => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MCP_BRIDGE_TOKEN"));

        /// <summary>
        /// Generates a cryptographically secure token.
        /// </summary>
        private static string GenerateSecureToken()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "");
        }

        /// <summary>
        /// Constant-time string comparison to prevent timing attacks.
        /// </summary>
        private static bool ConstantTimeEquals(string a, string b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            var diff = (uint)a.Length ^ (uint)b.Length;
            var minLength = Math.Min(a.Length, b.Length);

            for (var i = 0; i < minLength; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }

            return diff == 0;
        }

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

        /// <summary>
        /// Loads the token from file.
        /// </summary>
        private string LoadTokenFromFile()
        {
            lock (_tokenLock)
            {
                // 空文字列もキャッシュミスとして扱う
                if (!string.IsNullOrEmpty(_cachedToken))
                {
                    return _cachedToken;
                }

                // キャッシュをクリア（空文字列が残っている場合）
                _cachedToken = null;

                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

                try
                {
                    var tokenPath = Path.Combine(projectRoot, TokenFileName);
                    if (File.Exists(tokenPath))
                    {
                        var content = File.ReadAllText(tokenPath).Trim();
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            _cachedToken = content;
                            return _cachedToken;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[McpBridgeSettings] Failed to load token from file: {ex.Message}");
                }

                // Auto-create if no token found
                try
                {
                    var token = GenerateSecureToken();
                    SaveTokenToFile(token, projectRoot);
                    _cachedToken = token;
                    Debug.Log($"[McpBridgeSettings] Auto-generated new bridge token: {MaskToken(token)}");
                    return _cachedToken;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[McpBridgeSettings] Failed to auto-generate token: {ex.Message}");
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Saves the token to file.
        /// </summary>
        private void SaveTokenToFile(string token, string projectRoot)
        {
            try
            {
                var tokenPath = Path.Combine(projectRoot, TokenFileName);
                File.WriteAllText(tokenPath, token);
                _cachedToken = token;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[McpBridgeSettings] Failed to save token: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Clears the cached token, forcing a reload from file on next access.
        /// </summary>
        public void InvalidateTokenCache()
        {
            lock (_tokenLock)
            {
                _cachedToken = null;
            }
        }

        /// <summary>
        /// Gets the path to the token file.
        /// </summary>
        public static string GetTokenFilePath()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, TokenFileName);
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

            // Return path as-is without appending any folder name
            return trimmed.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
