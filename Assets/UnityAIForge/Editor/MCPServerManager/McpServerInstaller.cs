using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MCP.Editor.ServerManager
{
    /// <summary>
    /// MCPサーバーのインストール処理を担当するクラス。
    /// ファイルコピーとPython環境のセットアップを行います。
    /// </summary>
    public static class McpServerInstaller
    {
        /// <summary>
        /// サーバーファイルをソースから宛先にコピー
        /// Assets/UnityAIForge/MCPServerから直接コピー
        /// </summary>
        public static void CopyServerFiles(string sourcePath, string destPath)
        {
            try
            {
                Debug.Log($"[McpServerInstaller] Copying server files...");
                Debug.Log($"[McpServerInstaller] Source: {sourcePath}");
                Debug.Log($"[McpServerInstaller] Destination: {destPath}");
                
                if (!Directory.Exists(sourcePath))
                {
                    throw new DirectoryNotFoundException($"Source directory not found: {sourcePath}");
                }
                
                // 宛先ディレクトリが既に存在する場合は削除
                if (Directory.Exists(destPath))
                {
                    Debug.Log($"[McpServerInstaller] Removing existing installation...");
                    Directory.Delete(destPath, true);
                }
                
                // 宛先ディレクトリを作成
                Directory.CreateDirectory(destPath);
                
                // ファイルとディレクトリをコピー
                CopyDirectory(sourcePath, destPath);

                Debug.Log($"[McpServerInstaller] Successfully copied server files to: {destPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpServerInstaller] Failed to copy server files: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Python環境をセットアップ（uvを使用）
        /// </summary>
        public static void SetupPythonEnvironment(string installPath)
        {
            try
            {
                Debug.Log("[McpServerInstaller] Setting up Python environment with uv...");
                
                // uvが利用可能かチェック
                if (!IsUvAvailable())
                {
                    Debug.LogWarning("[McpServerInstaller] 'uv' is not available. Please install it first.");
                    Debug.LogWarning("[McpServerInstaller] Visit: https://github.com/astral-sh/uv");
                    Debug.LogWarning("[McpServerInstaller] You can skip this step if you'll set up manually.");
                    return;
                }
                
                // uv syncを実行
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "uv",
                        Arguments = "sync",
                        WorkingDirectory = installPath,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                Debug.Log("[McpServerInstaller] Running 'uv sync'...");
                process.Start();
                
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                
                process.WaitForExit();
                
                if (!string.IsNullOrEmpty(output))
                {
                    Debug.Log($"[McpServerInstaller] UV Output:\n{output}");
                }
                
                if (process.ExitCode != 0)
                {
                    Debug.LogError($"[McpServerInstaller] UV failed with exit code {process.ExitCode}");
                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogError($"[McpServerInstaller] UV Error:\n{error}");
                    }
                    throw new Exception($"UV sync failed with exit code {process.ExitCode}");
                }
                
                Debug.Log("[McpServerInstaller] Python environment setup completed successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpServerInstaller] Failed to setup Python environment: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// インストールの検証
        /// </summary>
        public static bool ValidateInstallation(string installPath)
        {
            try
            {
                Debug.Log("[McpServerInstaller] Validating installation...");
                
                // 必須ファイルの存在確認
                var requiredFiles = new[]
                {
                    "pyproject.toml",
                    "src/main.py",
                    "src/server/create_mcp_server.py",
                };
                
                foreach (var file in requiredFiles)
                {
                    var filePath = Path.Combine(installPath, file);
                    if (!File.Exists(filePath))
                    {
                        Debug.LogError($"[McpServerInstaller] Required file not found: {file}");
                        return false;
                    }
                }
                
                Debug.Log("[McpServerInstaller] Installation validation passed!");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpServerInstaller] Validation failed: {ex.Message}");
                return false;
            }
        }
        
        #region Helper Methods
        
        private static void CopyDirectory(string sourceDir, string destDir)
        {
            // ディレクトリ情報を取得
            var dir = new DirectoryInfo(sourceDir);
            
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");
            }
            
            // 宛先ディレクトリを作成
            Directory.CreateDirectory(destDir);
            
            // ファイルをコピー
            foreach (var file in dir.GetFiles())
            {
                // 除外するファイル
                if (ShouldExcludeFile(file.Name))
                {
                    continue;
                }
                
                var targetPath = Path.Combine(destDir, file.Name);
                file.CopyTo(targetPath, true);
            }
            
            // サブディレクトリを再帰的にコピー
            foreach (var subDir in dir.GetDirectories())
            {
                // 除外するディレクトリ
                if (ShouldExcludeDirectory(subDir.Name))
                {
                    continue;
                }
                
                var targetPath = Path.Combine(destDir, subDir.Name);
                CopyDirectory(subDir.FullName, targetPath);
            }
        }
        
        private static bool ShouldExcludeFile(string fileName)
        {
            var excludeExtensions = new[] { ".pyc", ".pyo", ".pyd", ".meta" };
            var excludeFiles = new[] { ".DS_Store", "Thumbs.db" };
            
            foreach (var ext in excludeExtensions)
            {
                if (fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            
            foreach (var file in excludeFiles)
            {
                if (fileName.Equals(file, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private static bool ShouldExcludeDirectory(string dirName)
        {
            var excludeDirs = new[] { "__pycache__", ".venv", ".pytest_cache", ".git", "node_modules" };
            
            foreach (var dir in excludeDirs)
            {
                if (dirName.Equals(dir, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private static bool IsUvAvailable()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
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
}

