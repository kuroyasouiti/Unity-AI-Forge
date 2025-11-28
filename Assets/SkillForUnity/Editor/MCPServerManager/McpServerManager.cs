using System;
using System.IO;
using UnityEngine;

namespace MCP.Editor.ServerManager
{
    /// <summary>
    /// MCPサーバーの管理を行うメインクラス。
    /// インストール、アンインストール、ステータス確認などを提供します。
    /// </summary>
    public static class McpServerManager
    {
        // MCPサーバーの名前
        public const string ServerName = "skill-for-unity";
        
        // バージョン
        public const string Version = "1.7.1";
        
        /// <summary>
        /// ユーザーホームディレクトリのインストール先パス
        /// McpBridgeSettingsで設定可能
        /// </summary>
        public static string UserInstallPath
        {
            get
            {
                // Try to get from settings (if available)
                try
                {
                    // Use reflection to get McpBridgeSettings without direct reference
                    var settingsType = System.Type.GetType("MCP.Editor.McpBridgeSettings, Assembly-CSharp-Editor");
                    if (settingsType != null)
                    {
                        var instanceProp = settingsType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (instanceProp != null)
                        {
                            var settings = instanceProp.GetValue(null);
                            if (settings != null)
                            {
                                var pathProp = settingsType.GetProperty("ServerInstallPath");
                                if (pathProp != null)
                                {
                                    var path = pathProp.GetValue(settings) as string;
                                    if (!string.IsNullOrEmpty(path))
                                    {
                                        return path;
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback to default if settings not available
                }
                
                // Default path
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(userProfile, "SkillForUnity");
            }
        }
        
        /// <summary>
        /// Unityプロジェクト内のMCPサーバーソースパス
        /// </summary>
        public static string SourcePath
        {
            get
            {
                return Path.Combine(Application.dataPath, "SkillForUnity", "MCPServer");
            }
        }
        
        /// <summary>
        /// サーバーがインストールされているかチェック
        /// </summary>
        public static bool IsInstalled()
        {
            return Directory.Exists(UserInstallPath) && 
                   File.Exists(Path.Combine(UserInstallPath, "pyproject.toml"));
        }
        
        /// <summary>
        /// サーバーのステータスを取得
        /// </summary>
        public static ServerStatus GetStatus()
        {
            var status = new ServerStatus
            {
                IsInstalled = IsInstalled(),
                InstallPath = UserInstallPath,
                SourcePath = SourcePath,
                Version = Version,
            };
            
            if (status.IsInstalled)
            {
                // Pythonのチェック
                status.PythonAvailable = CheckPythonAvailable();
                status.UvAvailable = CheckUvAvailable();
            }
            
            return status;
        }
        
        /// <summary>
        /// サーバーをインストール
        /// </summary>
        public static void Install()
        {
            try
            {
                Debug.Log("[McpServerManager] Starting installation...");
                
                // 検証
                if (!Directory.Exists(SourcePath))
                {
                    throw new Exception($"Source directory not found: {SourcePath}");
                }
                
                if (IsInstalled())
                {
                    Debug.LogWarning("[McpServerManager] Server is already installed. Use Reinstall to update.");
                    return;
                }
                
                // インストール実行
                McpServerInstaller.CopyServerFiles(SourcePath, UserInstallPath);
                
                // Python環境セットアップ
                McpServerInstaller.SetupPythonEnvironment(UserInstallPath);
                
                // 検証
                if (!IsInstalled())
                {
                    throw new Exception("Installation verification failed");
                }
                
                Debug.Log("[McpServerManager] Installation completed successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpServerManager] Installation failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// サーバーをアンインストール
        /// </summary>
        public static void Uninstall()
        {
            try
            {
                Debug.Log("[McpServerManager] Starting uninstallation...");
                
                if (!IsInstalled())
                {
                    Debug.LogWarning("[McpServerManager] Server is not installed.");
                    return;
                }
                
                // バックアップ作成（オプション）
                var backupPath = UserInstallPath + ".backup." + DateTime.Now.ToString("yyyyMMddHHmmss");
                Directory.Move(UserInstallPath, backupPath);
                
                Debug.Log($"[McpServerManager] Server files moved to backup: {backupPath}");
                Debug.Log("[McpServerManager] Uninstallation completed successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpServerManager] Uninstallation failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// サーバーを再インストール
        /// </summary>
        public static void Reinstall()
        {
            try
            {
                Debug.Log("[McpServerManager] Starting reinstallation...");
                
                if (IsInstalled())
                {
                    Uninstall();
                }
                
                Install();
                
                Debug.Log("[McpServerManager] Reinstallation completed successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpServerManager] Reinstallation failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// インストールフォルダを開く
        /// </summary>
        public static void OpenInstallFolder()
        {
            if (!IsInstalled())
            {
                Debug.LogWarning("[McpServerManager] Server is not installed.");
                return;
            }
            
            System.Diagnostics.Process.Start("explorer.exe", UserInstallPath);
        }
        
        /// <summary>
        /// ソースフォルダを開く
        /// </summary>
        public static void OpenSourceFolder()
        {
            if (!Directory.Exists(SourcePath))
            {
                Debug.LogWarning($"[McpServerManager] Source folder not found: {SourcePath}");
                return;
            }
            
            System.Diagnostics.Process.Start("explorer.exe", SourcePath);
        }
        
        #region Helper Methods
        
        private static bool CheckPythonAvailable()
        {
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
        
        private static bool CheckUvAvailable()
        {
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "uv",
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// サーバーのステータス情報
    /// </summary>
    public class ServerStatus
    {
        public bool IsInstalled { get; set; }
        public string InstallPath { get; set; }
        public string SourcePath { get; set; }
        public string Version { get; set; }
        public bool PythonAvailable { get; set; }
        public bool UvAvailable { get; set; }
        
        public override string ToString()
        {
            return $"Installed: {IsInstalled}, Python: {PythonAvailable}, UV: {UvAvailable}";
        }
    }
}

