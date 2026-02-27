using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;
using MCP.Editor.Utilities.GraphAnalysis;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace MCP.Editor.Handlers.HighLevel
{
    /// <summary>
    /// Handler for scene integrity validation.
    /// Detects missing scripts, null references, broken events, and broken prefabs.
    /// </summary>
    public class SceneIntegrityHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "missingScripts",
            "nullReferences",
            "brokenEvents",
            "brokenPrefabs",
            "removeMissingScripts",
            "all",
            "typeCheck",
            "report",
            "checkPrefab"
        };

        public override string Category => "sceneIntegrity";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            var rootPath = GetString(payload, "rootPath");
            var analyzer = new SceneIntegrityAnalyzer();

            return operation switch
            {
                "missingScripts" => BuildResponse(operation, analyzer.FindMissingScripts(rootPath)),
                "nullReferences" => BuildResponse(operation, analyzer.FindNullReferences(rootPath)),
                "brokenEvents" => BuildResponse(operation, analyzer.FindBrokenEvents(rootPath)),
                "brokenPrefabs" => BuildResponse(operation, analyzer.FindBrokenPrefabs(rootPath)),
                "removeMissingScripts" => BuildRemoveResponse(analyzer, rootPath),
                "all" => BuildAllResponse(analyzer, rootPath),
                "typeCheck" => BuildResponse(operation, analyzer.FindTypeMismatches(rootPath)),
                "report" => HandleReport(payload),
                "checkPrefab" => HandleCheckPrefab(payload),
                _ => throw new InvalidOperationException($"Unsupported scene integrity operation: {operation}"),
            };
        }

        private Dictionary<string, object> BuildResponse(string operation,
            List<SceneIntegrityAnalyzer.IntegrityIssue> issues)
        {
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["category"] = "sceneIntegrity",
                ["operation"] = operation,
                ["issues"] = issues.Select(i => i.ToDictionary()).ToList(),
                ["issueCount"] = issues.Count,
                ["isClean"] = issues.Count == 0
            };
        }

        private Dictionary<string, object> BuildRemoveResponse(SceneIntegrityAnalyzer analyzer, string rootPath)
        {
            var (removed, totalRemoved) = analyzer.RemoveMissingScripts(rootPath);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["category"] = "sceneIntegrity",
                ["operation"] = "removeMissingScripts",
                ["removed"] = removed.Select(i => i.ToDictionary()).ToList(),
                ["totalRemoved"] = totalRemoved,
                ["affectedGameObjects"] = removed.Count
            };
        }

        private Dictionary<string, object> BuildAllResponse(SceneIntegrityAnalyzer analyzer, string rootPath)
        {
            var (issues, summary) = analyzer.FindAllIssues(rootPath);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["category"] = "sceneIntegrity",
                ["operation"] = "all",
                ["issues"] = issues.Select(i => i.ToDictionary()).ToList(),
                ["issueCount"] = issues.Count,
                ["isClean"] = issues.Count == 0,
                ["summary"] = summary
            };
        }

        private Dictionary<string, object> HandleReport(Dictionary<string, object> payload)
        {
            var scope = GetString(payload, "scope", "active_scene");
            var byScene = new List<Dictionary<string, object>>();
            var bySeverity = new Dictionary<string, int>
            {
                ["error"] = 0,
                ["warning"] = 0,
                ["info"] = 0
            };
            int totalIssues = 0;

            var scenePaths = new List<string>();

            switch (scope)
            {
                case "build_scenes":
                    foreach (var scene in EditorBuildSettings.scenes)
                    {
                        if (scene.enabled)
                            scenePaths.Add(scene.path);
                    }
                    break;
                case "all_scenes":
                    var guids = AssetDatabase.FindAssets("t:Scene");
                    foreach (var guid in guids)
                    {
                        scenePaths.Add(AssetDatabase.GUIDToAssetPath(guid));
                    }
                    break;
                default: // active_scene
                    break;
            }

            if (scope == "active_scene")
            {
                // Just analyze the active scene
                var analyzer = new SceneIntegrityAnalyzer();
                var (issues, summary) = analyzer.FindAllIssues();
                var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

                totalIssues = issues.Count;
                foreach (var issue in issues)
                {
                    var sev = issue.Severity?.ToLower() ?? "info";
                    if (bySeverity.ContainsKey(sev)) bySeverity[sev]++;
                }

                byScene.Add(new Dictionary<string, object>
                {
                    ["scenePath"] = activeScene.path,
                    ["sceneName"] = activeScene.name,
                    ["issueCount"] = issues.Count,
                    ["summary"] = summary
                });
            }
            else
            {
                // Guard: max 20 scenes
                if (scenePaths.Count > 20)
                {
                    return new Dictionary<string, object>
                    {
                        ["success"] = false,
                        ["error"] = $"Too many scenes ({scenePaths.Count}). Maximum is 20. Use a more specific scope."
                    };
                }

                var currentScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;

                foreach (var scenePath in scenePaths)
                {
                    try
                    {
                        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                        var analyzer = new SceneIntegrityAnalyzer();
                        var (issues, summary) = analyzer.FindAllIssues();

                        totalIssues += issues.Count;
                        foreach (var issue in issues)
                        {
                            var sev = issue.Severity?.ToLower() ?? "info";
                            if (bySeverity.ContainsKey(sev)) bySeverity[sev]++;
                        }

                        byScene.Add(new Dictionary<string, object>
                        {
                            ["scenePath"] = scenePath,
                            ["sceneName"] = scene.name,
                            ["issueCount"] = issues.Count,
                            ["summary"] = summary
                        });

                        // Close scene if it's not the original active scene
                        if (scenePath != currentScenePath)
                        {
                            EditorSceneManager.CloseScene(scene, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        byScene.Add(new Dictionary<string, object>
                        {
                            ["scenePath"] = scenePath,
                            ["error"] = ex.Message
                        });
                    }
                }
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["category"] = "sceneIntegrity",
                ["operation"] = "report",
                ["scope"] = scope,
                ["totalIssues"] = totalIssues,
                ["byScene"] = byScene,
                ["bySeverity"] = bySeverity
            };
        }

        private Dictionary<string, object> HandleCheckPrefab(Dictionary<string, object> payload)
        {
            var prefabPath = GetString(payload, "prefabPath");
            if (string.IsNullOrEmpty(prefabPath))
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "prefabPath is required for checkPrefab operation."
                };
            }

            if (!prefabPath.EndsWith(".prefab"))
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = $"Invalid prefab path: '{prefabPath}'. Must end with .prefab."
                };
            }

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(prefabPath);
            if (asset == null)
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = $"Prefab not found at path: '{prefabPath}'."
                };
            }

            var analyzer = new SceneIntegrityAnalyzer();
            var (issues, summary) = analyzer.CheckPrefabAsset(prefabPath);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["category"] = "sceneIntegrity",
                ["operation"] = "checkPrefab",
                ["prefabPath"] = prefabPath,
                ["issues"] = issues.Select(i => i.ToDictionary()).ToList(),
                ["issueCount"] = issues.Count,
                ["isClean"] = issues.Count == 0,
                ["summary"] = summary
            };
        }
    }
}
