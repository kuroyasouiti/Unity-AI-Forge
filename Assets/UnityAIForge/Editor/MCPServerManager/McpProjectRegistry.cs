using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.ServerManager
{
    /// <summary>
    /// 現在のUnityプロジェクトをMCPクライアントの設定に登録するクラス。
    /// プロジェクト固有のトークンとブリッジ設定を含めて登録します。
    /// </summary>
    public static class McpProjectRegistry
    {
        /// <summary>
        /// プロジェクト登録の結果
        /// </summary>
        public class RegistrationResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public string ConfigPath { get; set; }
            public string ServerName { get; set; }
        }

        /// <summary>
        /// プロジェクト固有のサーバー名を生成
        /// プロジェクト名をベースにした一意の名前を返す
        /// </summary>
        public static string GetProjectServerName()
        {
            var projectName = GetProjectName();
            // プロジェクト名をサーバー名として使用（スペースをハイフンに置換、小文字化）
            var normalized = projectName.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("_", "-");

            // 特殊文字を除去
            var sanitized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[^a-z0-9\-]", "");

            // 空の場合はデフォルト名を使用
            if (string.IsNullOrEmpty(sanitized))
            {
                sanitized = "unity-project";
            }

            return sanitized;
        }

        /// <summary>
        /// 現在のプロジェクト名を取得
        /// </summary>
        public static string GetProjectName()
        {
            var projectPath = GetProjectPath();
            return Path.GetFileName(projectPath);
        }

        /// <summary>
        /// 現在のプロジェクトパスを取得
        /// </summary>
        public static string GetProjectPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        /// <summary>
        /// 現在のプロジェクトをAIツールに登録
        /// </summary>
        /// <param name="tool">登録先のAIツール</param>
        /// <param name="useProjectName">プロジェクト名をサーバー名に使用するか（falseの場合はデフォルト名を使用）</param>
        /// <returns>登録結果</returns>
        public static RegistrationResult RegisterProject(AITool tool, bool useProjectName = true)
        {
            try
            {
                Debug.Log($"[McpProjectRegistry] Registering project to {tool}...");

                // サーバーがインストールされているか確認
                if (!McpServerManager.IsInstalled())
                {
                    return new RegistrationResult
                    {
                        Success = false,
                        Message = "MCP server is not installed. Please install it first."
                    };
                }

                var serverName = useProjectName ? GetProjectServerName() : McpServerManager.ServerName;
                var configPath = McpConfigManager.GetConfigPath(tool);

                // 既に登録されているかチェック
                if (IsProjectRegistered(tool, serverName))
                {
                    return new RegistrationResult
                    {
                        Success = false,
                        Message = $"Project '{serverName}' is already registered to {McpConfigManager.GetToolDisplayName(tool)}",
                        ConfigPath = configPath,
                        ServerName = serverName
                    };
                }

                // バックアップ
                if (McpConfigManager.ConfigExists(tool))
                {
                    McpConfigManager.BackupConfig(tool);
                }

                // 設定読み込み
                var config = McpConfigManager.LoadConfig(tool);

                JObject mcpServers;

                // Claude Codeは特殊な構造を使用
                if (McpConfigManager.UsesProjectBasedConfig(tool))
                {
                    var projectPath = GetProjectPath();
                    mcpServers = McpConfigManager.GetOrCreateClaudeCodeMcpServers(config, projectPath);
                }
                else
                {
                    // 他のツールは標準的な構造
                    if (!config.ContainsKey("mcpServers"))
                    {
                        config["mcpServers"] = new JObject();
                    }
                    mcpServers = config["mcpServers"] as JObject;
                }

                // プロジェクト固有のエントリを追加
                mcpServers[serverName] = CreateProjectServerEntry(tool);

                // 保存
                McpConfigManager.SaveConfig(tool, config);

                Debug.Log($"[McpProjectRegistry] Successfully registered project '{serverName}' to {tool}");

                return new RegistrationResult
                {
                    Success = true,
                    Message = $"Successfully registered project to {McpConfigManager.GetToolDisplayName(tool)}",
                    ConfigPath = configPath,
                    ServerName = serverName
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpProjectRegistry] Failed to register project to {tool}: {ex.Message}");
                return new RegistrationResult
                {
                    Success = false,
                    Message = $"Failed to register: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 現在のプロジェクトをAIツールから解除
        /// </summary>
        public static RegistrationResult UnregisterProject(AITool tool, bool useProjectName = true)
        {
            try
            {
                Debug.Log($"[McpProjectRegistry] Unregistering project from {tool}...");

                var serverName = useProjectName ? GetProjectServerName() : McpServerManager.ServerName;
                var configPath = McpConfigManager.GetConfigPath(tool);

                // 登録されていない場合
                if (!IsProjectRegistered(tool, serverName))
                {
                    return new RegistrationResult
                    {
                        Success = false,
                        Message = $"Project '{serverName}' is not registered to {McpConfigManager.GetToolDisplayName(tool)}",
                        ConfigPath = configPath,
                        ServerName = serverName
                    };
                }

                // バックアップ
                McpConfigManager.BackupConfig(tool);

                // 設定読み込み
                var config = McpConfigManager.LoadConfig(tool);

                // Claude Codeは特殊な構造を使用
                if (McpConfigManager.UsesProjectBasedConfig(tool))
                {
                    var projectPath = GetProjectPath();
                    McpConfigManager.RemoveServerFromClaudeCode(config, projectPath, serverName);
                }
                else
                {
                    // 他のツールは標準的な構造
                    if (config.ContainsKey("mcpServers"))
                    {
                        var mcpServers = config["mcpServers"] as JObject;
                        if (mcpServers.ContainsKey(serverName))
                        {
                            mcpServers.Remove(serverName);
                        }
                    }
                }

                // 保存
                McpConfigManager.SaveConfig(tool, config);

                Debug.Log($"[McpProjectRegistry] Successfully unregistered project '{serverName}' from {tool}");

                return new RegistrationResult
                {
                    Success = true,
                    Message = $"Successfully unregistered project from {McpConfigManager.GetToolDisplayName(tool)}",
                    ConfigPath = configPath,
                    ServerName = serverName
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpProjectRegistry] Failed to unregister project from {tool}: {ex.Message}");
                return new RegistrationResult
                {
                    Success = false,
                    Message = $"Failed to unregister: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// プロジェクトが登録されているかチェック
        /// </summary>
        public static bool IsProjectRegistered(AITool tool, string serverName = null)
        {
            try
            {
                if (!McpConfigManager.ConfigExists(tool))
                {
                    return false;
                }

                var name = serverName ?? GetProjectServerName();
                var config = McpConfigManager.LoadConfig(tool);

                // Claude Codeは特殊な構造を使用
                if (McpConfigManager.UsesProjectBasedConfig(tool))
                {
                    var projectPath = GetProjectPath();
                    return McpConfigManager.IsServerRegisteredInClaudeCode(config, projectPath, name);
                }

                // 他のツールは標準的な構造
                if (!config.ContainsKey("mcpServers"))
                {
                    return false;
                }

                var mcpServers = config["mcpServers"] as JObject;
                return mcpServers != null && mcpServers.ContainsKey(name);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpProjectRegistry] Failed to check registration status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// すべてのAIツールへの登録状態を取得
        /// </summary>
        public static Dictionary<AITool, bool> GetProjectRegistrationStatus()
        {
            var status = new Dictionary<AITool, bool>();
            var serverName = GetProjectServerName();

            foreach (AITool tool in Enum.GetValues(typeof(AITool)))
            {
                status[tool] = IsProjectRegistered(tool, serverName);
            }

            return status;
        }

        /// <summary>
        /// プロジェクト固有のMCPサーバーエントリを作成
        /// CLI引数でプロジェクト固有の設定を渡す
        /// </summary>
        /// <param name="tool">対象のAIツール（Claude Codeの場合は特殊処理）</param>
        private static JObject CreateProjectServerEntry(AITool tool = AITool.ClaudeDesktop)
        {
            var settings = McpBridgeSettings.Instance;
            var installPath = McpServerManager.UserInstallPath;
            var projectPath = GetProjectPath();
            var bridgeToken = settings.BridgeToken;
            var bridgeHost = settings.ServerHost;
            var bridgePort = settings.ServerPort;

            // 基本的なargsを構築
            var args = new JArray
            {
                "--directory",
                installPath,
                "run",
                McpServerManager.ServerName
            };

            // ブリッジトークンを追加
            if (!string.IsNullOrEmpty(bridgeToken))
            {
                args.Add("--bridge-token");
                args.Add(bridgeToken);
            }

            // ブリッジホストを追加（デフォルト以外の場合）
            if (!string.IsNullOrEmpty(bridgeHost) && bridgeHost != "127.0.0.1")
            {
                args.Add("--bridge-host");
                args.Add(bridgeHost);
            }

            // ブリッジポートを追加
            args.Add("--bridge-port");
            args.Add(bridgePort.ToString());

            var entry = new JObject
            {
                ["command"] = "uv",
                ["args"] = args
            };

            // Claude Code以外のツールには環境変数でプロジェクトパスを追加
            // Claude Codeはprojectsセクションのパスで識別するため不要
            if (!McpConfigManager.UsesProjectBasedConfig(tool))
            {
                entry["env"] = new JObject
                {
                    ["UNITY_PROJECT_ROOT"] = projectPath
                };
            }

            return entry;
        }

        /// <summary>
        /// 生成されるJSON設定のプレビューを取得
        /// 選択したツールによって表示形式が変わる
        /// </summary>
        /// <param name="tool">プレビュー対象のAIツール</param>
        public static string GetConfigPreview(AITool tool = AITool.ClaudeDesktop)
        {
            var serverName = GetProjectServerName();
            var entry = CreateProjectServerEntry(tool);

            JObject preview;

            if (McpConfigManager.UsesProjectBasedConfig(tool))
            {
                // Claude Codeのプレビュー形式
                var projectPath = GetProjectPath().Replace("/", "\\");
                preview = new JObject
                {
                    ["projects"] = new JObject
                    {
                        [projectPath] = new JObject
                        {
                            ["mcpServers"] = new JObject
                            {
                                [serverName] = entry
                            }
                        }
                    }
                };
            }
            else
            {
                // 標準的なプレビュー形式
                preview = new JObject
                {
                    ["mcpServers"] = new JObject
                    {
                        [serverName] = entry
                    }
                };
            }

            return preview.ToString(Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>
        /// コピー用のJSON設定を取得
        /// </summary>
        /// <param name="tool">対象のAIツール</param>
        public static string GetConfigForClipboard(AITool tool = AITool.ClaudeDesktop)
        {
            var serverName = GetProjectServerName();
            var entry = CreateProjectServerEntry(tool);

            JObject config;

            if (McpConfigManager.UsesProjectBasedConfig(tool))
            {
                // Claude Code形式
                var projectPath = GetProjectPath().Replace("/", "\\");
                config = new JObject
                {
                    ["projects"] = new JObject
                    {
                        [projectPath] = new JObject
                        {
                            ["mcpServers"] = new JObject
                            {
                                [serverName] = entry
                            }
                        }
                    }
                };
            }
            else
            {
                // mcpServersセクション全体を返す（既存の設定にマージしやすいように）
                config = new JObject
                {
                    [serverName] = entry
                };
            }

            return config.ToString(Newtonsoft.Json.Formatting.Indented);
        }
    }
}
