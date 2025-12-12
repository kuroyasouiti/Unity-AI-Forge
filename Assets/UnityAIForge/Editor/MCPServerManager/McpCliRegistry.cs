using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MCP.Editor.ServerManager
{
    /// <summary>
    /// 各AIツールのCLIを使用してMCPサーバーを登録するクラス。
    /// プロジェクト固有のトークンとポートを含めた登録をサポート。
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
        /// 登録スコープ（Claude Code用）
        /// </summary>
        public enum RegistrationScope
        {
            /// <summary>ユーザー設定（すべてのプロジェクトで利用可能、~/.claude.json）</summary>
            User,
            /// <summary>ローカル設定（このマシンのみ、.claude.json）</summary>
            Local,
            /// <summary>プロジェクト設定（チーム共有、.claude/settings.json）</summary>
            Project
        }

        /// <summary>
        /// プロジェクト登録オプション
        /// </summary>
        public class ProjectRegistrationOptions
        {
            /// <summary>サーバー名（プロジェクト固有）</summary>
            public string ServerName { get; set; }

            /// <summary>MCPサーバーのインストールパス</summary>
            public string ServerPath { get; set; }

            /// <summary>ブリッジ認証トークン</summary>
            public string BridgeToken { get; set; }

            /// <summary>ブリッジホスト</summary>
            public string BridgeHost { get; set; } = "127.0.0.1";

            /// <summary>ブリッジポート</summary>
            public int BridgePort { get; set; } = 7070;

            /// <summary>Unityプロジェクトルートパス</summary>
            public string ProjectPath { get; set; }

            /// <summary>登録スコープ（Claude Code用）</summary>
            public RegistrationScope Scope { get; set; } = RegistrationScope.User;
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
        /// 指定されたAIツールにMCPサーバーを登録（基本）
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

            // MCP CLIをサポートしているかチェック（GUIアプリが起動するのを防ぐ）
            if (!SupportsMcpCli(tool))
            {
                return new CliResult
                {
                    Success = false,
                    Error = $"{cliInfo.displayName} does not support MCP CLI commands (may open GUI app)",
                    ExitCode = -1
                };
            }

            // Claude Code: claude mcp add --transport stdio <name> -- uv --directory <path> run unity-ai-forge
            var isWindows = Application.platform == RuntimePlatform.WindowsEditor;
            var uvCommand = "uv";
            var args = $"mcp add --transport stdio {McpServerManager.ServerName} -- {uvCommand} --directory \"{serverPath}\" run unity-ai-forge";
            return ExecuteCliCommand(cliInfo.command, args, cliInfo.displayName);
        }

        /// <summary>
        /// プロジェクト固有の設定でMCPサーバーを登録
        /// トークン、ポート、プロジェクトパスを含む
        /// </summary>
        public static CliResult RegisterProject(AITool tool, ProjectRegistrationOptions options)
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

            // MCP CLIをサポートしているかチェック（GUIアプリが起動するのを防ぐ）
            if (!SupportsMcpCli(tool))
            {
                return new CliResult
                {
                    Success = false,
                    Error = $"{cliInfo.displayName} does not support MCP CLI commands (may open GUI app)",
                    ExitCode = -1
                };
            }

            // サーバー実行引数を構築
            // 形式: unity-ai-forge --bridge-token {token} --bridge-port {port}
            var serverArgsBuilder = new StringBuilder();
            serverArgsBuilder.Append($"--bridge-port {options.BridgePort}");

            if (!string.IsNullOrEmpty(options.BridgeToken))
            {
                serverArgsBuilder.Append($" --bridge-token {options.BridgeToken}");
            }

            if (!string.IsNullOrEmpty(options.BridgeHost) && options.BridgeHost != "127.0.0.1")
            {
                serverArgsBuilder.Append($" --bridge-host {options.BridgeHost}");
            }

            // Claude Code CLI引数を構築
            // 形式: claude mcp add --transport stdio [--scope <scope>] <name> -- uv --directory <path> run unity-ai-forge <args>
            var isWindows = Application.platform == RuntimePlatform.WindowsEditor;
            var uvCommand = isWindows ? "cmd /c uv" : "uv";

            var argsBuilder = new StringBuilder();
            argsBuilder.Append("mcp add --transport stdio");

            // スコープオプション（Claude Code用）
            if (tool == AITool.ClaudeCode)
            {
                var scope = options.Scope switch
                {
                    RegistrationScope.Local => "local",
                    RegistrationScope.Project => "project",
                    _ => "user"
                };
                argsBuilder.Append($" --scope {scope}");
            }

            argsBuilder.Append($" {options.ServerName}");
            argsBuilder.Append($" -- {uvCommand} --directory \"{options.ServerPath}\" run unity-ai-forge {serverArgsBuilder}");

            // 環境変数でプロジェクトパスを渡す場合（一部ツールで有効）
            var envVars = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(options.ProjectPath))
            {
                envVars["UNITY_PROJECT_ROOT"] = options.ProjectPath;
            }

            return ExecuteCliCommand(cliInfo.command, argsBuilder.ToString(), cliInfo.displayName, envVars);
        }

        /// <summary>
        /// プロジェクト固有のサーバーを解除
        /// </summary>
        public static CliResult UnregisterProject(AITool tool, string serverName, RegistrationScope scope = RegistrationScope.User)
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

            // MCP CLIをサポートしているかチェック（GUIアプリが起動するのを防ぐ）
            if (!SupportsMcpCli(tool))
            {
                return new CliResult
                {
                    Success = false,
                    Error = $"{cliInfo.displayName} does not support MCP CLI commands (may open GUI app)",
                    ExitCode = -1
                };
            }

            // スコープオプション（Claude Code用）
            var args = $"mcp remove {serverName}";
            if (tool == AITool.ClaudeCode)
            {
                var scopeStr = scope switch
                {
                    RegistrationScope.Local => "local",
                    RegistrationScope.Project => "project",
                    _ => "user"
                };
                args = $"mcp remove --scope {scopeStr} {serverName}";
            }

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

            // MCP CLIをサポートしているかチェック（GUIアプリが起動するのを防ぐ）
            if (!SupportsMcpCli(tool))
            {
                return new CliResult
                {
                    Success = false,
                    Error = $"{cliInfo.displayName} does not support MCP CLI commands (may open GUI app)",
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
        /// 指定されたAIツールがMCP CLIコマンドをサポートしているかチェック
        /// CLIが存在しても、GUIアプリが起動する場合はfalseを返す
        /// </summary>
        public static bool IsMcpCliSupported(AITool tool)
        {
            return SupportsMcpCli(tool);
        }

        /// <summary>
        /// CLIコマンドが利用可能かチェック
        /// Windowsでは where コマンドで存在確認（アプリ起動を防ぐ）
        /// </summary>
        public static bool IsCliAvailable(string command)
        {
            try
            {
                var isWindows = Application.platform == RuntimePlatform.WindowsEditor;

                if (isWindows)
                {
                    // Windowsでは where コマンドで存在確認（アプリを起動しない）
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "where",
                            Arguments = command,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.WaitForExit(5000);
                    return process.ExitCode == 0;
                }
                else
                {
                    // macOS/Linux では which コマンド
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "which",
                            Arguments = command,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.WaitForExit(5000);
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 登録済みMCPサーバー一覧を取得
        /// </summary>
        public static CliResult ListServers(AITool tool)
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

            // MCP CLIをサポートしているかチェック（GUIアプリが起動するのを防ぐ）
            if (!SupportsMcpCli(tool))
            {
                return new CliResult
                {
                    Success = false,
                    Error = $"{cliInfo.displayName} does not support MCP CLI commands (may open GUI app)",
                    ExitCode = -1
                };
            }

            return ExecuteCliCommand(cliInfo.command, "mcp list", cliInfo.displayName);
        }

        /// <summary>
        /// 指定されたサーバーが登録済みかチェック
        /// CLIが利用不可な場合はfalseを返す
        /// </summary>
        public static bool IsServerRegistered(AITool tool, string serverName)
        {
            // CLIが利用可能かまずチェック
            if (!IsCliAvailable(tool))
            {
                return false;
            }

            // MCP CLIをサポートしているかチェック（GUIアプリが起動するのを防ぐ）
            if (!SupportsMcpCli(tool))
            {
                return false;
            }

            var result = ListServers(tool);
            if (!result.Success)
            {
                return false;
            }

            // 出力からサーバー名を検索
            return result.Output?.Contains(serverName, StringComparison.OrdinalIgnoreCase) ?? false;
        }

        /// <summary>
        /// ツールがMCP CLIをサポートしているかチェック
        /// 一部のツールはCLIコマンドが存在してもGUIアプリが起動するため、
        /// 実際にMCP CLIをサポートしているツールのみtrueを返す
        /// </summary>
        private static bool SupportsMcpCli(AITool tool)
        {
            // 現時点でMCP CLIをサポートしていることが確認されているツール
            // 他のツールはGUIが起動する可能性があるためfalseを返す
            return tool switch
            {
                AITool.ClaudeCode => true,  // claude mcp add/list/remove をサポート
                // AITool.Cursor => true,   // TODO: Cursor MCP CLIが安定したら有効化
                // AITool.Cline => true,    // TODO: Cline MCP CLIが確認できたら有効化
                // AITool.Windsurf => true, // TODO: Windsurf MCP CLIが確認できたら有効化
                _ => false
            };
        }

        #region Helper Methods

        private static CliResult ExecuteCliCommand(string command, string args, string toolName, Dictionary<string, string> envVars = null)
        {
            try
            {
                Debug.Log($"[McpCliRegistry] Executing: {command} {args}");

                // Windowsでは cmd /c 経由で実行（.cmd/.bat ファイル対応）
                var isWindows = Application.platform == RuntimePlatform.WindowsEditor;
                var fileName = isWindows ? "cmd.exe" : command;
                var arguments = isWindows ? $"/c {command} {args}" : args;

                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                // 環境変数を追加
                if (envVars != null)
                {
                    foreach (var kvp in envVars)
                    {
                        startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                        Debug.Log($"[McpCliRegistry] Environment: {kvp.Key}={kvp.Value}");
                    }
                }

                var process = new Process { StartInfo = startInfo };
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

