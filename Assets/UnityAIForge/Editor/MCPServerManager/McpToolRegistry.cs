using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MCP.Editor.ServerManager
{
    /// <summary>
    /// 各AIツールへのMCPサーバー登録・解除を管理するクラス。
    /// CLI対応ツールはCLIを使用し、非対応ツールはJSON設定ファイルを直接編集します。
    /// </summary>
    public static class McpToolRegistry
    {
        /// <summary>
        /// CLI対応のAIツール一覧
        /// </summary>
        private static readonly HashSet<AITool> CliSupportedTools = new()
        {
            AITool.Cursor,
            AITool.ClaudeCode,
            AITool.Cline,
            AITool.Windsurf
        };

        /// <summary>
        /// 指定されたAIツールがCLI登録をサポートしているかチェック
        /// </summary>
        public static bool IsCliSupported(AITool tool)
        {
            return CliSupportedTools.Contains(tool) && McpCliRegistry.IsCliAvailable(tool);
        }

        /// <summary>
        /// 指定されたAIツールにMCPサーバーを登録
        /// </summary>
        public static void Register(AITool tool)
        {
            try
            {
                Debug.Log($"[McpToolRegistry] Registering MCP server to {tool}...");

                // サーバーがインストールされているか確認
                if (!McpServerManager.IsInstalled())
                {
                    throw new Exception("MCP server is not installed. Please install it first.");
                }

                // 既に登録されているかチェック
                if (IsRegistered(tool))
                {
                    Debug.LogWarning($"[McpToolRegistry] MCP server is already registered to {tool}");
                    return;
                }

                // CLI対応ツールはCLIを使用
                if (IsCliSupported(tool))
                {
                    RegisterViaCli(tool);
                }
                else
                {
                    RegisterViaConfig(tool);
                }

                Debug.Log($"[McpToolRegistry] Successfully registered MCP server to {tool}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpToolRegistry] Failed to register to {tool}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// CLIを使用して登録
        /// </summary>
        private static void RegisterViaCli(AITool tool)
        {
            var serverPath = McpServerManager.UserInstallPath;
            var result = McpCliRegistry.Register(tool, serverPath);

            if (!result.Success)
            {
                throw new Exception($"CLI registration failed: {result.Error}");
            }
        }

        /// <summary>
        /// JSON設定ファイルを直接編集して登録
        /// </summary>
        private static void RegisterViaConfig(AITool tool)
        {
            // バックアップ
            if (McpConfigManager.ConfigExists(tool))
            {
                McpConfigManager.BackupConfig(tool);
            }

            // 設定読み込み
            var config = McpConfigManager.LoadConfig(tool);

            // mcpServersセクションを取得または作成
            if (!config.ContainsKey("mcpServers"))
            {
                config["mcpServers"] = new JObject();
            }

            var mcpServers = config["mcpServers"] as JObject;

            // Unity-AI-Forgeのエントリを追加
            mcpServers[McpServerManager.ServerName] = CreateServerEntry();

            // 保存
            McpConfigManager.SaveConfig(tool, config);
        }

        /// <summary>
        /// 指定されたAIツールからMCPサーバーを解除
        /// </summary>
        public static void Unregister(AITool tool)
        {
            try
            {
                Debug.Log($"[McpToolRegistry] Unregistering MCP server from {tool}...");

                // 登録されていない場合
                if (!IsRegistered(tool))
                {
                    Debug.LogWarning($"[McpToolRegistry] MCP server is not registered to {tool}");
                    return;
                }

                // CLI対応ツールはCLIを使用
                if (IsCliSupported(tool))
                {
                    UnregisterViaCli(tool);
                }
                else
                {
                    UnregisterViaConfig(tool);
                }

                Debug.Log($"[McpToolRegistry] Successfully unregistered MCP server from {tool}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpToolRegistry] Failed to unregister from {tool}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// CLIを使用して解除
        /// </summary>
        private static void UnregisterViaCli(AITool tool)
        {
            var result = McpCliRegistry.Unregister(tool);

            if (!result.Success)
            {
                throw new Exception($"CLI unregistration failed: {result.Error}");
            }
        }

        /// <summary>
        /// JSON設定ファイルを直接編集して解除
        /// </summary>
        private static void UnregisterViaConfig(AITool tool)
        {
            // バックアップ
            McpConfigManager.BackupConfig(tool);

            // 設定読み込み
            var config = McpConfigManager.LoadConfig(tool);

            if (config.ContainsKey("mcpServers"))
            {
                var mcpServers = config["mcpServers"] as JObject;

                // Unity-AI-Forgeのエントリを削除
                if (mcpServers.ContainsKey(McpServerManager.ServerName))
                {
                    mcpServers.Remove(McpServerManager.ServerName);
                }
            }

            // 保存
            McpConfigManager.SaveConfig(tool, config);
        }
        
        /// <summary>
        /// すべてのAIツールに登録
        /// </summary>
        public static void RegisterAll()
        {
            Debug.Log("[McpToolRegistry] Registering to all available AI tools...");
            
            var results = new Dictionary<AITool, bool>();
            
            foreach (AITool tool in Enum.GetValues(typeof(AITool)))
            {
                try
                {
                    Register(tool);
                    results[tool] = true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[McpToolRegistry] Failed to register to {tool}: {ex.Message}");
                    results[tool] = false;
                }
            }
            
            // 結果サマリー
            var successCount = 0;
            foreach (var result in results)
            {
                if (result.Value) successCount++;
            }
            
            Debug.Log($"[McpToolRegistry] Registration completed: {successCount}/{results.Count} successful");
        }
        
        /// <summary>
        /// すべてのAIツールから解除
        /// </summary>
        public static void UnregisterAll()
        {
            Debug.Log("[McpToolRegistry] Unregistering from all AI tools...");
            
            var results = new Dictionary<AITool, bool>();
            
            foreach (AITool tool in Enum.GetValues(typeof(AITool)))
            {
                try
                {
                    Unregister(tool);
                    results[tool] = true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[McpToolRegistry] Failed to unregister from {tool}: {ex.Message}");
                    results[tool] = false;
                }
            }
            
            // 結果サマリー
            var successCount = 0;
            foreach (var result in results)
            {
                if (result.Value) successCount++;
            }
            
            Debug.Log($"[McpToolRegistry] Unregistration completed: {successCount}/{results.Count} successful");
        }
        
        /// <summary>
        /// 指定されたAIツールに登録されているかチェック
        /// </summary>
        public static bool IsRegistered(AITool tool)
        {
            try
            {
                if (!McpConfigManager.ConfigExists(tool))
                {
                    return false;
                }
                
                var config = McpConfigManager.LoadConfig(tool);
                
                if (!config.ContainsKey("mcpServers"))
                {
                    return false;
                }
                
                var mcpServers = config["mcpServers"] as JObject;
                return mcpServers != null && mcpServers.ContainsKey(McpServerManager.ServerName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpToolRegistry] Failed to check registration status for {tool}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// すべてのAIツールの登録状態を取得
        /// </summary>
        public static Dictionary<AITool, bool> GetRegistrationStatus()
        {
            var status = new Dictionary<AITool, bool>();
            
            foreach (AITool tool in Enum.GetValues(typeof(AITool)))
            {
                status[tool] = IsRegistered(tool);
            }
            
            return status;
        }
        
        #region Helper Methods
        
        /// <summary>
        /// MCPサーバーエントリのJSONを作成
        /// </summary>
        private static JObject CreateServerEntry()
        {
            var installPath = McpServerManager.UserInstallPath;
            
            // Windowsパスのバックスラッシュをエスケープ
            var escapedPath = installPath.Replace("\\", "\\\\");
            
            return new JObject
            {
                ["command"] = "uv",
                ["args"] = new JArray
                {
                    "--directory",
                    installPath,
                    "run",
                    McpServerManager.ServerName
                }
            };
        }
        
        #endregion
    }
}

