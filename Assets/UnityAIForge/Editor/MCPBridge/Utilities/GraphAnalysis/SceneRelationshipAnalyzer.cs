using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Utilities.GraphAnalysis
{
    /// <summary>
    /// Analyzes relationships between scenes in the Unity project.
    /// </summary>
    public class SceneRelationshipAnalyzer
    {
        private readonly Dictionary<string, SceneNode> _nodeCache = new Dictionary<string, SceneNode>();

        /// <summary>
        /// Analyze all scene relationships in the project.
        /// </summary>
        public SceneRelationshipResult AnalyzeAll(bool includeScriptReferences = true, bool includeSceneFlow = true)
        {
            var result = new SceneRelationshipResult();

            // Get all scenes in the project
            var sceneGuids = AssetDatabase.FindAssets("t:Scene");
            var allScenes = sceneGuids
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .ToList();

            // Add all scenes as nodes
            foreach (var scenePath in allScenes)
            {
                var node = CreateNodeForScene(scenePath);
                result.AddNode(node);
            }

            // Get build settings info
            var buildScenes = EditorBuildSettings.scenes;
            var buildOrder = new List<string>();
            var unregisteredScenes = new List<string>();

            foreach (var buildScene in buildScenes)
            {
                if (buildScene.enabled)
                {
                    buildOrder.Add(buildScene.path);
                }
            }
            result.BuildOrder = buildOrder;

            // Find unregistered scenes
            foreach (var scenePath in allScenes)
            {
                if (!buildScenes.Any(bs => bs.path == scenePath))
                {
                    unregisteredScenes.Add(scenePath);
                }
            }
            result.UnregisteredScenes = unregisteredScenes;

            // Analyze script references to scenes
            if (includeScriptReferences)
            {
                AnalyzeScriptReferences(allScenes, result);
            }

            // Analyze GameKitSceneFlow if available
            if (includeSceneFlow)
            {
                AnalyzeSceneFlowAssets(result);
            }

            return result;
        }

        /// <summary>
        /// Analyze transitions from a specific scene.
        /// </summary>
        public SceneRelationshipResult AnalyzeScene(string scenePath, bool includeScriptReferences = true, bool includeSceneFlow = true)
        {
            var result = new SceneRelationshipResult();

            if (!File.Exists(scenePath))
            {
                throw new InvalidOperationException($"Scene not found: {scenePath}");
            }

            // Add the source scene as a node
            var sourceNode = CreateNodeForScene(scenePath);
            result.AddNode(sourceNode);

            // Analyze full project and filter
            var fullResult = AnalyzeAll(includeScriptReferences, includeSceneFlow);

            // Get edges originating from this scene
            foreach (var edge in fullResult.Edges)
            {
                if (edge.Source == scenePath)
                {
                    result.AddEdge(edge);

                    // Add target node
                    var targetNode = fullResult.GetNode(edge.Target);
                    if (targetNode != null)
                    {
                        result.AddNode(targetNode);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Find all scenes that can transition to the specified scene.
        /// </summary>
        public SceneRelationshipResult FindTransitionsTo(string scenePath)
        {
            var result = new SceneRelationshipResult();

            if (!File.Exists(scenePath))
            {
                throw new InvalidOperationException($"Scene not found: {scenePath}");
            }

            // Add target scene as a node
            var targetNode = CreateNodeForScene(scenePath);
            result.AddNode(targetNode);

            // Analyze full project
            var fullResult = AnalyzeAll(true, true);

            // Get edges targeting this scene
            foreach (var edge in fullResult.Edges)
            {
                if (edge.Target == scenePath)
                {
                    result.AddEdge(edge);

                    // Add source node
                    var sourceNode = fullResult.GetNode(edge.Source);
                    if (sourceNode != null)
                    {
                        result.AddNode(sourceNode);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Find all scenes that the specified scene can transition to.
        /// </summary>
        public SceneRelationshipResult FindTransitionsFrom(string scenePath)
        {
            // Same as AnalyzeScene
            return AnalyzeScene(scenePath, true, true);
        }

        /// <summary>
        /// Validate build settings and report issues.
        /// </summary>
        public SceneRelationshipResult ValidateBuildSettings()
        {
            var result = new SceneRelationshipResult();

            // Get all scene transitions
            var fullResult = AnalyzeAll(true, true);

            // Check each edge's target scene is in build settings
            var buildScenePaths = EditorBuildSettings.scenes.Select(s => s.path).ToHashSet();
            var issues = new List<Dictionary<string, object>>();

            foreach (var edge in fullResult.Edges)
            {
                var targetPath = edge.Target;
                if (!buildScenePaths.Contains(targetPath))
                {
                    var issue = new Dictionary<string, object>
                    {
                        ["type"] = "missing_from_build",
                        ["scene"] = targetPath,
                        ["referencedFrom"] = edge.Source,
                        ["relation"] = edge.Relation
                    };
                    issues.Add(issue);
                }
            }

            // Check for disabled scenes that are referenced
            var disabledScenes = EditorBuildSettings.scenes
                .Where(s => !s.enabled)
                .Select(s => s.path)
                .ToHashSet();

            foreach (var edge in fullResult.Edges)
            {
                if (disabledScenes.Contains(edge.Target))
                {
                    var issue = new Dictionary<string, object>
                    {
                        ["type"] = "disabled_in_build",
                        ["scene"] = edge.Target,
                        ["referencedFrom"] = edge.Source,
                        ["relation"] = edge.Relation
                    };
                    issues.Add(issue);
                }
            }

            result.Metadata["issues"] = issues;
            result.Metadata["issueCount"] = issues.Count;
            result.Metadata["isValid"] = issues.Count == 0;

            // Copy build order
            result.BuildOrder = fullResult.BuildOrder;
            result.UnregisteredScenes = fullResult.UnregisteredScenes;

            // Add nodes for scenes with issues
            foreach (var node in fullResult.Nodes)
            {
                result.AddNode(node);
            }

            return result;
        }

        #region Private Methods

        private void AnalyzeScriptReferences(List<string> allScenes, SceneRelationshipResult result)
        {
            // Create a lookup for scene names to paths
            var sceneNameToPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in allScenes)
            {
                var name = Path.GetFileNameWithoutExtension(path);
                if (!sceneNameToPath.ContainsKey(name))
                {
                    sceneNameToPath[name] = path;
                }
            }

            // Find all C# scripts
            var scriptGuids = AssetDatabase.FindAssets("t:Script", new[] { "Assets" });

            foreach (var guid in scriptGuids)
            {
                var scriptPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!scriptPath.EndsWith(".cs")) continue;

                try
                {
                    var content = File.ReadAllText(scriptPath);

                    // Find SceneManager.LoadScene calls
                    var loadScenePatterns = new[]
                    {
                        // SceneManager.LoadScene("SceneName")
                        @"SceneManager\.LoadScene\s*\(\s*""([^""]+)""",
                        // SceneManager.LoadScene(""SceneName"", LoadSceneMode.Single)
                        @"SceneManager\.LoadScene\s*\(\s*""([^""]+)""\s*,\s*LoadSceneMode\.(\w+)",
                        // SceneManager.LoadSceneAsync("SceneName")
                        @"SceneManager\.LoadSceneAsync\s*\(\s*""([^""]+)""",
                        // Application.LoadLevel (legacy)
                        @"Application\.LoadLevel\s*\(\s*""([^""]+)"""
                    };

                    foreach (var pattern in loadScenePatterns)
                    {
                        var matches = Regex.Matches(content, pattern);
                        foreach (Match match in matches)
                        {
                            var targetSceneName = match.Groups[1].Value;
                            var loadMode = match.Groups.Count > 2 ? match.Groups[2].Value : "Single";

                            // Find the source scene (if this script is in a scene context)
                            // We'll associate with any scene that might contain this script
                            var sourceScenes = FindScenesContainingScript(scriptPath, allScenes);

                            // Resolve target scene path
                            string targetPath = null;
                            if (targetSceneName.Contains("/") || targetSceneName.EndsWith(".unity"))
                            {
                                // It's a path
                                targetPath = targetSceneName.EndsWith(".unity") ? targetSceneName : targetSceneName + ".unity";
                                if (!targetPath.StartsWith("Assets/"))
                                {
                                    targetPath = "Assets/" + targetPath;
                                }
                            }
                            else if (sceneNameToPath.TryGetValue(targetSceneName, out var resolvedPath))
                            {
                                targetPath = resolvedPath;
                            }

                            if (targetPath != null && allScenes.Contains(targetPath))
                            {
                                var lineNumber = GetLineNumber(content, match.Index);

                                foreach (var sourceScene in sourceScenes)
                                {
                                    var edge = new SceneTransitionEdge(sourceScene, targetPath, "scene_load")
                                    {
                                        LoadType = loadMode.ToLower(),
                                        CallerScript = scriptPath,
                                        CallerLine = lineNumber
                                    };
                                    result.AddEdge(edge);
                                }

                                // If no source scenes found, add edge from script location
                                if (!sourceScenes.Any())
                                {
                                    // Create a generic edge indicating script-based transition
                                    var edge = new SceneTransitionEdge("(script)", targetPath, "scene_load")
                                    {
                                        LoadType = loadMode.ToLower(),
                                        CallerScript = scriptPath,
                                        CallerLine = lineNumber
                                    };
                                    // Don't add this edge as it doesn't have a proper source
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Skip files that can't be read
                }
            }
        }

        private void AnalyzeSceneFlowAssets(SceneRelationshipResult result)
        {
            // Find SceneFlow components in the current scene (generated via code generation)
            var allMonoBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(UnityEngine.FindObjectsSortMode.None);

            foreach (var mb in allMonoBehaviours)
            {
                if (mb == null) continue;

                // Check if this MonoBehaviour has a "flowId" and "scenes" field (SceneFlow pattern)
                var mbType = mb.GetType();
                var flowIdField = mbType.GetField("flowId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var scenesField = mbType.GetField("scenes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (flowIdField == null || scenesField == null) continue;

                try
                {
                    var scenes = scenesField.GetValue(mb) as System.Collections.IList;
                    if (scenes == null) continue;

                    // Build scene name to path mapping from flow
                    var sceneNameToPath = new Dictionary<string, string>();
                    foreach (var sceneObj in scenes)
                    {
                        var sceneDataType = sceneObj.GetType();
                        var nameField = sceneDataType.GetField("sceneName");
                        var pathField = sceneDataType.GetField("scenePath");

                        if (nameField != null && pathField != null)
                        {
                            var name = nameField.GetValue(sceneObj) as string;
                            var path = pathField.GetValue(sceneObj) as string;
                            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(path))
                            {
                                sceneNameToPath[name] = path;
                            }
                        }

                        // Get transitions from each scene entry
                        var transitionsField = sceneDataType.GetField("transitions");
                        if (transitionsField != null)
                        {
                            var transitions = transitionsField.GetValue(sceneObj) as System.Collections.IList;
                            if (transitions != null)
                            {
                                foreach (var transObj in transitions)
                                {
                                    var transType = transObj.GetType();
                                    var toField = transType.GetField("toScene");
                                    var triggerField = transType.GetField("trigger");

                                    if (nameField != null && toField != null)
                                    {
                                        var fromName = nameField.GetValue(sceneObj) as string;
                                        var toName = toField.GetValue(transObj) as string;
                                        var trigger = triggerField?.GetValue(transObj) as string;

                                        if (!string.IsNullOrEmpty(fromName) && !string.IsNullOrEmpty(toName) &&
                                            sceneNameToPath.TryGetValue(fromName, out var fromPath) &&
                                            sceneNameToPath.TryGetValue(toName, out var toPath))
                                        {
                                            var edge = new SceneTransitionEdge(fromPath, toPath, "sceneflow_transition")
                                            {
                                                Trigger = trigger
                                            };
                                            result.AddEdge(edge);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Reflection errors, skip this component
                }
            }
        }

        private List<string> FindScenesContainingScript(string scriptPath, List<string> allScenes)
        {
            var containingScenes = new List<string>();

            // Get the script asset
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            if (script == null) return containingScenes;

            var scriptClass = script.GetClass();
            if (scriptClass == null) return containingScenes;

            // Check if it's a MonoBehaviour
            if (!typeof(MonoBehaviour).IsAssignableFrom(scriptClass))
            {
                return containingScenes;
            }

            // For now, return empty - full implementation would need to parse scene files
            // This is a performance-heavy operation that could be expanded later
            return containingScenes;
        }

        private SceneNode CreateNodeForScene(string scenePath)
        {
            if (_nodeCache.TryGetValue(scenePath, out var cached))
            {
                return cached;
            }

            var node = new SceneNode(scenePath);

            // Check build settings
            var buildScenes = EditorBuildSettings.scenes;
            for (int i = 0; i < buildScenes.Length; i++)
            {
                if (buildScenes[i].path == scenePath)
                {
                    node.InBuildSettings = true;
                    node.BuildIndex = buildScenes[i].enabled ? i : -1;
                    break;
                }
            }

            // Check addressables (if available)
            // This would require Addressables package to be installed
            // For now, we'll skip this check

            _nodeCache[scenePath] = node;
            return node;
        }

        private int GetLineNumber(string content, int charIndex)
        {
            var lineNumber = 1;
            for (int i = 0; i < charIndex && i < content.Length; i++)
            {
                if (content[i] == '\n')
                {
                    lineNumber++;
                }
            }
            return lineNumber;
        }

        private Type FindType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(typeName);
                    if (type != null) return type;
                }
                catch { }
            }
            return null;
        }

        #endregion
    }
}
