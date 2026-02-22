using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.ServerManager
{
    /// <summary>
    /// パッケージ更新時にMCPサーバーのソースパスが変更されたかを検出し、
    /// 登録済みのAIツール設定ファイル内のパスを更新するよう促すクラス。
    /// </summary>
    [InitializeOnLoad]
    internal static class McpServerPathUpdateChecker
    {
        static McpServerPathUpdateChecker()
        {
            EditorApplication.delayCall += CheckForPathUpdate;
        }

        private static void CheckForPathUpdate()
        {
            // Play mode 中はスキップ
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            try
            {
                var settings = McpBridgeSettings.Instance;
                var currentSourcePath = McpServerManager.SourcePath;
                var lastKnownPath = settings.LastKnownSourcePath;

                // 初回起動時: パスを記録するだけ
                if (string.IsNullOrEmpty(lastKnownPath))
                {
                    settings.LastKnownSourcePath = currentSourcePath;
                    return;
                }

                // パスが変わっていなければ何もしない
                if (ArePathsEqual(currentSourcePath, lastKnownPath))
                {
                    return;
                }

                Debug.Log(
                    $"[McpServerPathUpdateChecker] MCP server source path changed:\n" +
                    $"  Old: {lastKnownPath}\n" +
                    $"  New: {currentSourcePath}");

                // 設定ファイル内で使われているパスは、登録時の UserInstallPath。
                // デフォルト設定（カスタムパス未設定）の場合、旧 UserInstallPath == 旧 SourcePath。
                var oldRegisteredPath = lastKnownPath;
                var newRegisteredPath = McpServerManager.UserInstallPath;

                // 旧パスと新パスが同一ならば設定ファイルの更新は不要
                if (ArePathsEqual(oldRegisteredPath, newRegisteredPath))
                {
                    settings.LastKnownSourcePath = currentSourcePath;
                    return;
                }

                // 旧パスを含む設定ファイルを検索
                var configFiles = CollectAllConfigPaths();
                var affectedFiles = FindConfigsWithPath(configFiles, oldRegisteredPath);

                if (affectedFiles.Count == 0)
                {
                    Debug.Log("[McpServerPathUpdateChecker] No config files reference the old path. Skipping update prompt.");
                    settings.LastKnownSourcePath = currentSourcePath;
                    return;
                }

                // ダイアログで更新するか確認
                var shouldUpdate = EditorUtility.DisplayDialog(
                    "Unity-AI-Forge パッケージ更新検出",
                    $"MCPサーバーのパスが変更されました。\n\n" +
                    $"旧パス:\n{oldRegisteredPath}\n\n" +
                    $"新パス:\n{newRegisteredPath}\n\n" +
                    $"登録済みのAIツール設定を更新しますか？\n" +
                    $"（{affectedFiles.Count} 件の設定ファイルが該当）",
                    "更新する",
                    "スキップ");

                // パスの記録は結果に関わらず更新
                settings.LastKnownSourcePath = currentSourcePath;

                if (shouldUpdate)
                {
                    var updatedCount = UpdatePaths(affectedFiles, oldRegisteredPath, newRegisteredPath);

                    if (updatedCount > 0)
                    {
                        Debug.Log($"[McpServerPathUpdateChecker] Updated {updatedCount} config file(s)");

                        EditorUtility.DisplayDialog(
                            "Unity-AI-Forge",
                            $"{updatedCount} 件のAIツール設定ファイルのMCPサーバーパスを更新しました。",
                            "OK");
                    }
                    else
                    {
                        Debug.Log("[McpServerPathUpdateChecker] No config files were updated (path entries not found in JSON).");
                    }
                }
                else
                {
                    Debug.Log("[McpServerPathUpdateChecker] User skipped config update.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpServerPathUpdateChecker] Error checking for path update: {ex.Message}");
            }
        }

        /// <summary>
        /// すべてのAIツールの設定ファイルパスを収集（重複排除）
        /// </summary>
        private static HashSet<string> CollectAllConfigPaths()
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 各AIツールの標準設定パス
            foreach (AITool tool in Enum.GetValues(typeof(AITool)))
            {
                try
                {
                    var path = McpConfigManager.GetConfigPath(tool);
                    if (!string.IsNullOrEmpty(path))
                    {
                        paths.Add(path);
                    }
                }
                catch
                {
                    // ConfigPath 取得失敗はスキップ
                }

                // スコープ付き設定ファイル（Claude Code, Codex CLI, Gemini CLI）
                if (McpCliRegistry.SupportsScope(tool))
                {
                    var scopes = new[]
                    {
                        McpCliRegistry.RegistrationScope.User,
                        McpCliRegistry.RegistrationScope.Local,
                        McpCliRegistry.RegistrationScope.Project
                    };

                    foreach (var scope in scopes)
                    {
                        try
                        {
                            var path = McpCliRegistry.GetScopedConfigPath(tool, scope);
                            if (!string.IsNullOrEmpty(path))
                            {
                                paths.Add(path);
                            }
                        }
                        catch
                        {
                            // スコープ付きパス取得失敗はスキップ
                        }
                    }
                }
            }

            return paths;
        }

        /// <summary>
        /// 設定ファイル群から指定パスを含むものを検索
        /// </summary>
        private static List<string> FindConfigsWithPath(HashSet<string> configFiles, string targetPath)
        {
            var result = new List<string>();
            var normalizedTarget = NormalizePath(targetPath);

            foreach (var configPath in configFiles)
            {
                if (!File.Exists(configPath))
                {
                    continue;
                }

                try
                {
                    var content = File.ReadAllText(configPath);

                    // パス区切り文字を統一して簡易チェック
                    var normalizedContent = content.Replace("\\\\", "/").Replace("\\", "/");
                    if (normalizedContent.Contains(normalizedTarget, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(configPath);
                    }
                }
                catch
                {
                    // 読み込み失敗はスキップ
                }
            }

            return result;
        }

        /// <summary>
        /// 該当する設定ファイルのパスを更新
        /// </summary>
        private static int UpdatePaths(List<string> configFiles, string oldPath, string newPath)
        {
            var count = 0;

            foreach (var configPath in configFiles)
            {
                try
                {
                    if (UpdateConfigFile(configPath, oldPath, newPath))
                    {
                        count++;
                        Debug.Log($"[McpServerPathUpdateChecker] Updated: {configPath}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[McpServerPathUpdateChecker] Failed to update {configPath}: {ex.Message}");
                }
            }

            return count;
        }

        /// <summary>
        /// 単一の設定ファイル内の --directory 引数を更新
        /// </summary>
        private static bool UpdateConfigFile(string configPath, string oldPath, string newPath)
        {
            var content = File.ReadAllText(configPath);
            var config = JObject.Parse(content);

            var updated = false;

            // ルートレベルの mcpServers を更新
            updated |= UpdateMcpServersSection(config, oldPath, newPath);

            // Claude Code の projects 構造内の mcpServers を更新
            if (config.TryGetValue("projects", out var projectsToken) && projectsToken is JObject projects)
            {
                foreach (var project in projects.Properties())
                {
                    if (project.Value is JObject projectObj)
                    {
                        updated |= UpdateMcpServersSection(projectObj, oldPath, newPath);
                    }
                }
            }

            if (updated)
            {
                // 変更前にバックアップを作成
                var backupPath = configPath + ".backup." + DateTime.Now.ToString("yyyyMMddHHmmss");
                File.Copy(configPath, backupPath, true);

                File.WriteAllText(configPath, config.ToString(Formatting.Indented));
            }

            return updated;
        }

        /// <summary>
        /// mcpServers セクション内の各サーバーエントリの --directory 引数を更新
        /// </summary>
        private static bool UpdateMcpServersSection(JObject parent, string oldPath, string newPath)
        {
            if (!parent.TryGetValue("mcpServers", out var serversToken) || !(serversToken is JObject mcpServers))
            {
                return false;
            }

            var updated = false;

            foreach (var server in mcpServers.Properties())
            {
                if (!(server.Value is JObject serverEntry))
                {
                    continue;
                }

                if (!serverEntry.TryGetValue("args", out var argsToken) || !(argsToken is JArray args))
                {
                    continue;
                }

                for (var i = 0; i < args.Count - 1; i++)
                {
                    if (args[i].ToString() == "--directory" && ArePathsEqual(args[i + 1].ToString(), oldPath))
                    {
                        args[i + 1] = newPath;
                        updated = true;
                    }
                }
            }

            return updated;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            return path.Replace('\\', '/').TrimEnd('/');
        }

        private static bool ArePathsEqual(string path1, string path2)
        {
            var n1 = NormalizePath(path1);
            var n2 = NormalizePath(path2);
            return string.Equals(n1, n2, StringComparison.OrdinalIgnoreCase);
        }
    }
}
