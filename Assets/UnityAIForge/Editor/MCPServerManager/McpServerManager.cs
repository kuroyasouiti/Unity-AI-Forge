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
        public const string ServerName = "unity-ai-forge";
        
        // バージョン取得のフォールバック値
        private const string FallbackVersion = "2.0.0";
        
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
                return Path.Combine(userProfile, "Unity-AI-Forge");
            }
        }
        
        /// <summary>
        /// Unityプロジェクト内のMCPサーバーソースパス
        /// </summary>
        public static string SourcePath
        {
            get
            {
                // パッケージとしてインストールされているか確認
                var packagePath = GetPackagePath();
                if (!string.IsNullOrEmpty(packagePath))
                {
                    return Path.Combine(packagePath, "MCPServer");
                }
                
                // ローカルパッケージの場合
                return Path.Combine(Application.dataPath, "UnityAIForge", "MCPServer");
            }
        }
        
        /// <summary>
        /// パッケージのルートパスを取得（パッケージとしてインストールされている場合）
        /// </summary>
        private static string GetPackagePath()
        {
            try
            {
                // このスクリプトファイル自体のパスからパッケージルートを推測
                var guids = UnityEditor.AssetDatabase.FindAssets("McpServerManager t:Script");
                if (guids.Length > 0)
                {
                    var scriptPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    
                    // パッケージパスの場合（Packages/com.unityaiforge/...）
                    if (scriptPath.StartsWith("Packages/"))
                    {
                        var parts = scriptPath.Split('/');
                        if (parts.Length >= 2)
                        {
                            var packageName = parts[1]; // com.unityaiforge
                            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(scriptPath);
                            if (packageInfo != null)
                            {
                                return packageInfo.resolvedPath;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[McpServerManager] Failed to get package path: {ex.Message}");
            }
            
            return null;
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
                Version = GetServerVersion(),
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
        /// サーバーのバージョンを取得
        /// インストール先またはソースからpyproject.tomlを読み取る
        /// </summary>
        public static string GetServerVersion()
        {
            // 1. インストール先のpyproject.tomlから取得
            if (IsInstalled())
            {
                var installedVersion = ReadVersionFromPyProject(UserInstallPath);
                if (!string.IsNullOrEmpty(installedVersion))
                {
                    return installedVersion;
                }
            }
            
            // 2. ソースのpyproject.tomlから取得
            if (Directory.Exists(SourcePath))
            {
                var sourceVersion = ReadVersionFromPyProject(SourcePath);
                if (!string.IsNullOrEmpty(sourceVersion))
                {
                    return sourceVersion;
                }
            }
            
            // 3. フォールバック
            return FallbackVersion;
        }
        
        /// <summary>
        /// pyproject.tomlからバージョンを読み取る
        /// </summary>
        private static string ReadVersionFromPyProject(string directoryPath)
        {
            try
            {
                var pyprojectPath = Path.Combine(directoryPath, "pyproject.toml");
                if (!File.Exists(pyprojectPath))
                {
                    return null;
                }
                
                var lines = File.ReadAllLines(pyprojectPath);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    // version = "2.0.0" の形式を探す
                    if (trimmed.StartsWith("version") && trimmed.Contains("="))
                    {
                        var parts = trimmed.Split('=');
                        if (parts.Length >= 2)
                        {
                            var versionPart = parts[1].Trim();
                            // クォートを削除
                            versionPart = versionPart.Trim('"', '\'', ' ');
                            if (!string.IsNullOrEmpty(versionPart))
                            {
                                return versionPart;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[McpServerManager] Failed to read version from {directoryPath}: {ex.Message}");
            }
            
            return null;
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

                // 古いバックアップを削除（1つだけ保持）
                CleanupOldBackups();

                // バックアップ作成
                var backupPath = UserInstallPath + ".backup";
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
        /// 古いバックアップを削除（1つだけ保持するため、既存のバックアップを削除）
        /// </summary>
        private static void CleanupOldBackups()
        {
            try
            {
                var parentDir = Path.GetDirectoryName(UserInstallPath);
                if (string.IsNullOrEmpty(parentDir) || !Directory.Exists(parentDir))
                {
                    return;
                }

                var installDirName = Path.GetFileName(UserInstallPath);
                var backupPattern = installDirName + ".backup";

                // バックアップディレクトリを探す（.backup と .backup.timestamp の両方に対応）
                var directories = Directory.GetDirectories(parentDir);
                foreach (var dir in directories)
                {
                    var dirName = Path.GetFileName(dir);
                    if (dirName != null && dirName.StartsWith(backupPattern))
                    {
                        try
                        {
                            Directory.Delete(dir, true);
                            Debug.Log($"[McpServerManager] Deleted old backup: {dir}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[McpServerManager] Failed to delete old backup {dir}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[McpServerManager] Failed to cleanup old backups: {ex.Message}");
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

                // パッケージ更新後はPythonキャッシュをクリア
                ClearPythonCache();

                Debug.Log("[McpServerManager] Reinstallation completed successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpServerManager] Reinstallation failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Pythonキャッシュをクリア（__pycache__ディレクトリと.pycファイルを削除）
        /// パッケージ更新後に呼び出すことで、古いバイトコードキャッシュの問題を防止
        /// </summary>
        public static void ClearPythonCache()
        {
            var paths = new[] { SourcePath, UserInstallPath };
            var totalCleared = 0;

            foreach (var basePath in paths)
            {
                if (string.IsNullOrEmpty(basePath) || !Directory.Exists(basePath))
                {
                    continue;
                }

                try
                {
                    // __pycache__ ディレクトリを削除
                    var pycacheDirs = Directory.GetDirectories(basePath, "__pycache__", SearchOption.AllDirectories);
                    foreach (var dir in pycacheDirs)
                    {
                        try
                        {
                            Directory.Delete(dir, true);
                            totalCleared++;
                            Debug.Log($"[McpServerManager] Deleted cache directory: {dir}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[McpServerManager] Failed to delete {dir}: {ex.Message}");
                        }
                    }

                    // .pyc ファイルを削除（__pycache__外にある場合）
                    var pycFiles = Directory.GetFiles(basePath, "*.pyc", SearchOption.AllDirectories);
                    foreach (var file in pycFiles)
                    {
                        try
                        {
                            File.Delete(file);
                            totalCleared++;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[McpServerManager] Failed to delete {file}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[McpServerManager] Failed to clear cache in {basePath}: {ex.Message}");
                }
            }

            if (totalCleared > 0)
            {
                Debug.Log($"[McpServerManager] Cleared {totalCleared} Python cache item(s)");
            }
            else
            {
                Debug.Log("[McpServerManager] No Python cache to clear");
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

