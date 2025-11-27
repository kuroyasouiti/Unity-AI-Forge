using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MCP.Editor.ServerManager
{
    /// <summary>
    /// AIツールの種類
    /// </summary>
    public enum AITool
    {
        Cursor,
        ClaudeDesktop,
        Cline,
        Windsurf
    }
    
    /// <summary>
    /// 各AIツールの設定ファイルを管理するクラス。
    /// JSON設定の読み込み、保存、バックアップを行います。
    /// </summary>
    public static class McpConfigManager
    {
        private const string BackupExtension = ".backup";
        
        /// <summary>
        /// 指定されたAIツールの設定ファイルパスを取得
        /// </summary>
        public static string GetConfigPath(AITool tool)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            
            return tool switch
            {
                // Cursorは複数の可能性があるため、FindCursorConfigPath()で検索
                AITool.Cursor => FindCursorConfigPath(appData),
                    
                AITool.ClaudeDesktop => Path.Combine(appData, "Claude", "claude_desktop_config.json"),
                
                AITool.Cline => Path.Combine(appData, "Code", "User", "globalStorage",
                    "saoudrizwan.claude-dev", "settings", "cline_mcp_settings.json"),
                    
                AITool.Windsurf => Path.Combine(appData, "Windsurf", "User", "globalStorage",
                    "windsurf.windsurf", "settings", "mcp_settings.json"),
                    
                _ => throw new ArgumentException($"Unknown AI tool: {tool}")
            };
        }
        
        /// <summary>
        /// Cursorの設定ファイルパスを検索
        /// 複数の可能性を試す
        /// </summary>
        private static string FindCursorConfigPath(string appData)
        {
            // 可能性1: Cursor独自のMCP設定
            var path1 = Path.Combine(appData, "Cursor", "User", "globalStorage", "cursor", "mcp.json");
            if (File.Exists(path1)) return path1;
            
            // 可能性2: CursorのグローバルMCP設定
            var path2 = Path.Combine(appData, "Cursor", "User", "globalStorage", "cursor-mcp", "settings.json");
            if (File.Exists(path2)) return path2;
            
            // 可能性3: Cline統合（Roo Cline）
            var path3 = Path.Combine(appData, "Cursor", "User", "globalStorage", 
                "rooveterinaryinc.roo-cline", "settings", "cline_mcp_settings.json");
            if (File.Exists(path3)) return path3;
            
            // 可能性4: Cline統合（saoudrizwan）
            var path4 = Path.Combine(appData, "Cursor", "User", "globalStorage", 
                "saoudrizwan.claude-dev", "settings", "cline_mcp_settings.json");
            if (File.Exists(path4)) return path4;
            
            // 可能性5: Cursorのsettings.json（メイン設定ファイル）
            var path5 = Path.Combine(appData, "Cursor", "User", "settings.json");
            if (File.Exists(path5))
            {
                // settings.jsonがあれば、それを使用（mcpServersセクションがあるかチェック）
                try
                {
                    var content = File.ReadAllText(path5);
                    if (content.Contains("mcpServers"))
                    {
                        return path5;
                    }
                }
                catch
                {
                    // エラーは無視して次へ
                }
            }
            
            // デフォルト: Roo Clineのパスを返す（最も一般的）
            return path3;
        }
        
        /// <summary>
        /// 設定ファイルが存在するかチェック
        /// </summary>
        public static bool ConfigExists(AITool tool)
        {
            var path = GetConfigPath(tool);
            return File.Exists(path);
        }
        
        /// <summary>
        /// 設定ファイルを読み込み
        /// </summary>
        public static JObject LoadConfig(AITool tool)
        {
            try
            {
                var path = GetConfigPath(tool);
                
                Debug.Log($"[McpConfigManager] Loading config for {tool} from: {path}");
                
                if (!File.Exists(path))
                {
                    Debug.Log($"[McpConfigManager] Config file not found for {tool}, creating new one.");
                    return new JObject();
                }
                
                var json = File.ReadAllText(path);
                var config = JObject.Parse(json);
                
                Debug.Log($"[McpConfigManager] Config loaded successfully for {tool}");
                
                return config;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpConfigManager] Failed to load config for {tool}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 設定ファイルを保存
        /// </summary>
        public static void SaveConfig(AITool tool, JObject config)
        {
            try
            {
                var path = GetConfigPath(tool);
                
                // ディレクトリが存在しない場合は作成
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // JSONを整形して保存
                var json = config.ToString(Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(path, json);
                
                Debug.Log($"[McpConfigManager] Config saved for {tool}: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpConfigManager] Failed to save config for {tool}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 設定ファイルのバックアップを作成
        /// </summary>
        public static void BackupConfig(AITool tool)
        {
            try
            {
                var path = GetConfigPath(tool);
                
                if (!File.Exists(path))
                {
                    Debug.Log($"[McpConfigManager] No config file to backup for {tool}");
                    return;
                }
                
                var backupPath = path + BackupExtension + "." + DateTime.Now.ToString("yyyyMMddHHmmss");
                File.Copy(path, backupPath, true);
                
                Debug.Log($"[McpConfigManager] Config backed up for {tool}: {backupPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpConfigManager] Failed to backup config for {tool}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 最新のバックアップから設定を復元
        /// </summary>
        public static bool RestoreFromBackup(AITool tool)
        {
            try
            {
                var path = GetConfigPath(tool);
                var directory = Path.GetDirectoryName(path);
                var fileName = Path.GetFileName(path);
                
                if (!Directory.Exists(directory))
                {
                    Debug.LogWarning($"[McpConfigManager] Config directory not found for {tool}");
                    return false;
                }
                
                // バックアップファイルを検索
                var backupFiles = Directory.GetFiles(directory, fileName + BackupExtension + ".*");
                
                if (backupFiles.Length == 0)
                {
                    Debug.LogWarning($"[McpConfigManager] No backup files found for {tool}");
                    return false;
                }
                
                // 最新のバックアップを取得
                Array.Sort(backupFiles);
                var latestBackup = backupFiles[backupFiles.Length - 1];
                
                // 復元
                File.Copy(latestBackup, path, true);
                
                Debug.Log($"[McpConfigManager] Config restored for {tool} from: {latestBackup}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpConfigManager] Failed to restore config for {tool}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// すべてのAIツールの設定ファイル存在状況を取得
        /// </summary>
        public static Dictionary<AITool, bool> GetAllConfigStatus()
        {
            var status = new Dictionary<AITool, bool>();
            
            foreach (AITool tool in Enum.GetValues(typeof(AITool)))
            {
                status[tool] = ConfigExists(tool);
            }
            
            return status;
        }
        
        /// <summary>
        /// AIツールの表示名を取得
        /// </summary>
        public static string GetToolDisplayName(AITool tool)
        {
            return tool switch
            {
                AITool.Cursor => "Cursor",
                AITool.ClaudeDesktop => "Claude Desktop",
                AITool.Cline => "Cline (VS Code)",
                AITool.Windsurf => "Windsurf",
                _ => tool.ToString()
            };
        }
    }
}

