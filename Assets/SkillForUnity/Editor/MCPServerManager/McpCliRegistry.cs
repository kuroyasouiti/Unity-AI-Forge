using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MCP.Editor.ServerManager
{
    /// <summary>
    /// 各AIツールのCLIを使用してMCPサーバーを登録するクラス。
    /// </summary>
    public static class McpCliRegistry
    {
        /// <summary>
        /// CLIコマンドの結果
        /// </summary>
        public class CliResult
        {
            public bool Success { get; set; }
            public string Output { get; set; }
            public string Error { get; set; }
            public int ExitCode { get; set; }
        }
        
        /// <summary>
        /// Cursor CLIを使用して登録
        /// </summary>
        public static CliResult RegisterToCursor(string serverPath)
        {
            var command = "cursor";
            var args = $"mcp add skill-for-unity --directory \"{serverPath}\"";
            return ExecuteCliCommand(command, args, "Cursor");
        }
        
        /// <summary>
        /// Cursor CLIを使用して解除
        /// </summary>
        public static CliResult UnregisterFromCursor()
        {
            var command = "cursor";
            var args = "mcp remove skill-for-unity";
            return ExecuteCliCommand(command, args, "Cursor");
        }
        
        /// <summary>
        /// Claude Code CLIを使用して登録
        /// </summary>
        public static CliResult RegisterToClaudeCode(string serverPath)
        {
            var command = "claude-code";
            var args = $"mcp add skill-for-unity --directory \"{serverPath}\"";
            return ExecuteCliCommand(command, args, "Claude Code");
        }
        
        /// <summary>
        /// Claude Code CLIを使用して解除
        /// </summary>
        public static CliResult UnregisterFromClaudeCode()
        {
            var command = "claude-code";
            var args = "mcp remove skill-for-unity";
            return ExecuteCliCommand(command, args, "Claude Code");
        }
        
        /// <summary>
        /// Cline CLIを使用して登録
        /// </summary>
        public static CliResult RegisterToCline(string serverPath)
        {
            var command = "cline";
            var args = $"mcp add skill-for-unity --directory \"{serverPath}\"";
            return ExecuteCliCommand(command, args, "Cline");
        }
        
        /// <summary>
        /// Cline CLIを使用して解除
        /// </summary>
        public static CliResult UnregisterFromCline()
        {
            var command = "cline";
            var args = "mcp remove skill-for-unity";
            return ExecuteCliCommand(command, args, "Cline");
        }
        
        /// <summary>
        /// Windsurf CLIを使用して登録
        /// </summary>
        public static CliResult RegisterToWindsurf(string serverPath)
        {
            var command = "windsurf";
            var args = $"mcp add skill-for-unity --directory \"{serverPath}\"";
            return ExecuteCliCommand(command, args, "Windsurf");
        }
        
        /// <summary>
        /// Windsurf CLIを使用して解除
        /// </summary>
        public static CliResult UnregisterFromWindsurf()
        {
            var command = "windsurf";
            var args = "mcp remove skill-for-unity";
            return ExecuteCliCommand(command, args, "Windsurf");
        }
        
        /// <summary>
        /// CLIが利用可能かチェック
        /// </summary>
        public static bool IsCliAvailable(string command)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit(5000); // 5秒タイムアウト
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
        
        #region Helper Methods
        
        private static CliResult ExecuteCliCommand(string command, string args, string toolName)
        {
            try
            {
                Debug.Log($"[McpCliRegistry] Executing: {command} {args}");
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = args,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                
                process.WaitForExit();
                
                var result = new CliResult
                {
                    Success = process.ExitCode == 0,
                    Output = output,
                    Error = error,
                    ExitCode = process.ExitCode
                };
                
                if (result.Success)
                {
                    Debug.Log($"[McpCliRegistry] {toolName} CLI command succeeded");
                    if (!string.IsNullOrEmpty(output))
                    {
                        Debug.Log($"[McpCliRegistry] Output: {output}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[McpCliRegistry] {toolName} CLI command failed with exit code {process.ExitCode}");
                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogWarning($"[McpCliRegistry] Error: {error}");
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpCliRegistry] Failed to execute {toolName} CLI: {ex.Message}");
                return new CliResult
                {
                    Success = false,
                    Error = ex.Message,
                    ExitCode = -1
                };
            }
        }
        
        #endregion
    }
}

