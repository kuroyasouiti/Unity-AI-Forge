using System;
using System.Collections.Generic;
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
        /// 各AIツールのCLIコマンド情報
        /// </summary>
        private static readonly Dictionary<AITool, (string command, string displayName)> CliCommands = new()
        {
            { AITool.Cursor, ("cursor", "Cursor") },
            { AITool.ClaudeCode, ("claude", "Claude Code") },
            { AITool.Cline, ("cline", "Cline") },
            { AITool.Windsurf, ("windsurf", "Windsurf") }
        };

        /// <summary>
        /// 指定されたAIツールにMCPサーバーを登録
        /// </summary>
        public static CliResult Register(AITool tool, string serverPath)
        {
            if (!CliCommands.TryGetValue(tool, out var cliInfo))
            {
                return new CliResult
                {
                    Success = false,
                    Error = $"CLI not supported for {tool}",
                    ExitCode = -1
                };
            }

            var args = $"mcp add {McpServerManager.ServerName} --directory \"{serverPath}\"";
            return ExecuteCliCommand(cliInfo.command, args, cliInfo.displayName);
        }

        /// <summary>
        /// 指定されたAIツールからMCPサーバーを解除
        /// </summary>
        public static CliResult Unregister(AITool tool)
        {
            if (!CliCommands.TryGetValue(tool, out var cliInfo))
            {
                return new CliResult
                {
                    Success = false,
                    Error = $"CLI not supported for {tool}",
                    ExitCode = -1
                };
            }

            var args = $"mcp remove {McpServerManager.ServerName}";
            return ExecuteCliCommand(cliInfo.command, args, cliInfo.displayName);
        }

        /// <summary>
        /// 指定されたAIツールのCLIが利用可能かチェック
        /// </summary>
        public static bool IsCliAvailable(AITool tool)
        {
            if (!CliCommands.TryGetValue(tool, out var cliInfo))
            {
                return false;
            }
            return IsCliAvailable(cliInfo.command);
        }

        /// <summary>
        /// CLIコマンドが利用可能かチェック
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

