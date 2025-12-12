using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor
{
    [FilePath("ProjectSettings/McpBridgeSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class McpBridgeSettings : ScriptableSingleton<McpBridgeSettings>
    {
        private const string TokenFileName = ".mcp_bridge_tokens.json";
        private const string LegacyTokenFileName = ".mcp_bridge_token";
        private const string TokenFileVersion = "1.0";

        private static bool _hideFlagsInitialized;

        [SerializeField] private string serverHost = "127.0.0.1";
        [SerializeField] private int serverPort = 7070;
        [SerializeField] private string bridgeToken = string.Empty; // Legacy: kept for backward compatibility
        [SerializeField] private bool autoConnectOnLoad = true;
        [SerializeField] private float contextPushIntervalSeconds = 5f;
        [SerializeField] private string serverInstallPath = string.Empty;

        // Cached tokens loaded from file (thread-safe access via lock)
        private List<string> _cachedTokens;
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
        /// Gets the list of valid bridge authentication tokens.
        /// Priority: 1) Environment variable MCP_BRIDGE_TOKEN, 2) Token file (.mcp_bridge_tokens.json), 3) Legacy file (.mcp_bridge_token).
        /// Use environment variables in CI/CD or shared environments to avoid committing secrets.
        /// </summary>
        public IReadOnlyList<string> BridgeTokens
        {
            get
            {
                // Check environment variable first
                var envToken = Environment.GetEnvironmentVariable("MCP_BRIDGE_TOKEN");
                if (!string.IsNullOrWhiteSpace(envToken))
                {
                    return new[] { envToken.Trim() };
                }

                // Load from file
                return LoadTokensFromFile().AsReadOnly();
            }
        }

        /// <summary>
        /// Gets or sets the primary bridge authentication token.
        /// For backward compatibility, returns the first token from the list.
        /// Setting this value will add or update the first token in the list.
        /// </summary>
        public string BridgeToken
        {
            get
            {
                var tokens = BridgeTokens;
                return tokens.Count > 0 ? tokens[0] : string.Empty;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                lock (_tokenLock)
                {
                    var tokens = LoadTokensFromFileUnlocked();
                    var trimmed = value.Trim();
                    if (tokens.Count > 0)
                    {
                        tokens[0] = trimmed;
                    }
                    else
                    {
                        tokens.Add(trimmed);
                    }
                    var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                    SaveTokensToFileInternal(tokens, projectRoot);
                }
            }
        }

        /// <summary>
        /// Adds a new token to the token list.
        /// </summary>
        /// <returns>True if token was added, false if it already exists.</returns>
        public bool AddToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            lock (_tokenLock)
            {
                var tokens = LoadTokensFromFileUnlocked();
                var trimmed = token.Trim();
                if (tokens.Contains(trimmed))
                {
                    return false;
                }

                tokens.Add(trimmed);
                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                SaveTokensToFileInternal(tokens, projectRoot);
                return true;
            }
        }

        /// <summary>
        /// Removes a token from the token list.
        /// </summary>
        /// <returns>True if token was removed, false if it was not found.</returns>
        public bool RemoveToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            lock (_tokenLock)
            {
                var tokens = LoadTokensFromFileUnlocked();
                var trimmed = token.Trim();
                if (!tokens.Remove(trimmed))
                {
                    return false;
                }

                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                SaveTokensToFileInternal(tokens, projectRoot);
                return true;
            }
        }

        /// <summary>
        /// Generates a new unique token and adds it to the list.
        /// </summary>
        /// <returns>The newly generated token.</returns>
        public string GenerateAndAddToken()
        {
            lock (_tokenLock)
            {
                var token = Guid.NewGuid().ToString("N");
                var tokens = LoadTokensFromFileUnlocked();
                tokens.Add(token);
                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                SaveTokensToFileInternal(tokens, projectRoot);
                return token;
            }
        }

        /// <summary>
        /// Gets the number of configured tokens.
        /// </summary>
        public int TokenCount => BridgeTokens.Count;

        /// <summary>
        /// Checks if a given token is valid (exists in the token list).
        /// </summary>
        public bool IsValidToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var tokens = BridgeTokens;
            return tokens.Contains(token.Trim());
        }

        /// <summary>
        /// Gets the stored tokens for display purposes.
        /// Returns masked versions to avoid accidental exposure in logs/UI.
        /// </summary>
        public IReadOnlyList<string> BridgeTokensMasked
        {
            get
            {
                return BridgeTokens.Select(MaskToken).ToList().AsReadOnly();
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

        private List<string> LoadTokensFromFile()
        {
            lock (_tokenLock)
            {
                return LoadTokensFromFileUnlocked();
            }
        }

        /// <summary>
        /// Internal method for loading tokens without acquiring lock.
        /// Caller must hold _tokenLock before calling this method.
        /// </summary>
        private List<string> LoadTokensFromFileUnlocked()
        {
            if (_cachedTokens != null)
            {
                return new List<string>(_cachedTokens);
            }

            var tokens = new List<string>();
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            try
            {
                // Try new JSON format first
                var jsonPath = Path.Combine(projectRoot, TokenFileName);
                if (File.Exists(jsonPath))
                {
                    var json = File.ReadAllText(jsonPath);
                    var data = MiniJson.Deserialize(json) as Dictionary<string, object>;
                    if (data != null && data.TryGetValue("tokens", out var tokensObj) && tokensObj is List<object> tokenList)
                    {
                        foreach (var t in tokenList)
                        {
                            var tokenStr = t?.ToString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(tokenStr) && !tokens.Contains(tokenStr))
                            {
                                tokens.Add(tokenStr);
                            }
                        }
                    }

                    if (tokens.Count > 0)
                    {
                        _cachedTokens = new List<string>(tokens);
                        return new List<string>(tokens);
                    }
                }

                // Fallback: Try legacy single-token file
                var legacyPath = Path.Combine(projectRoot, LegacyTokenFileName);
                if (File.Exists(legacyPath))
                {
                    var content = File.ReadAllText(legacyPath).Trim();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        tokens.Add(content);
                        // Migrate to new format
                        SaveTokensToFileInternal(tokens, projectRoot);
                        _cachedTokens = new List<string>(tokens);
                        return new List<string>(tokens);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[McpBridgeSettings] Failed to load tokens from file: {ex.Message}");
            }

            // Auto-create if no tokens found
            if (tokens.Count == 0)
            {
                try
                {
                    var token = Guid.NewGuid().ToString("N");
                    tokens.Add(token);
                    SaveTokensToFileInternal(tokens, projectRoot);
                    _cachedTokens = new List<string>(tokens);
                    Debug.Log($"[McpBridgeSettings] Auto-generated new bridge token: {MaskToken(token)}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[McpBridgeSettings] Failed to auto-generate token: {ex.Message}");
                    // Return empty list - ValidateToken will reject connection
                }
            }

            return new List<string>(tokens);
        }

        private void SaveTokensToFile(List<string> tokens)
        {
            lock (_tokenLock)
            {
                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                SaveTokensToFileInternal(tokens, projectRoot);
            }
        }

        /// <summary>
        /// Internal method for saving tokens without acquiring lock.
        /// Called from LoadTokensFromFile which already holds the lock.
        /// </summary>
        private void SaveTokensToFileInternal(List<string> tokens, string projectRoot)
        {
            try
            {
                var jsonPath = Path.Combine(projectRoot, TokenFileName);

                var data = new Dictionary<string, object>
                {
                    ["tokens"] = tokens,
                    ["version"] = TokenFileVersion
                };

                var json = MiniJson.Serialize(data);
                File.WriteAllText(jsonPath, json);

                // Update cache
                _cachedTokens = new List<string>(tokens);

                // Also update legacy file for backward compatibility with older MCP servers
                var legacyPath = Path.Combine(projectRoot, LegacyTokenFileName);
                if (tokens.Count > 0)
                {
                    File.WriteAllText(legacyPath, tokens[0]);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[McpBridgeSettings] Failed to save tokens: {ex.Message}");
                throw; // Re-throw to allow caller to handle
            }
        }

        /// <summary>
        /// Clears the cached tokens, forcing a reload from file on next access.
        /// </summary>
        public void InvalidateTokenCache()
        {
            lock (_tokenLock)
            {
                _cachedTokens = null;
            }
        }

        /// <summary>
        /// Gets the path to the tokens file.
        /// </summary>
        public static string GetTokensFilePath()
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
