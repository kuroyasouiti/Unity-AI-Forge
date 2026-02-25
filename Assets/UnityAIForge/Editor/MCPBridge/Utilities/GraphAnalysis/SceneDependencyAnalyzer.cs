using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace MCP.Editor.Utilities.GraphAnalysis
{
    /// <summary>
    /// Analyzes asset dependencies of Unity scenes using AssetDatabase.GetDependencies().
    /// </summary>
    public class SceneDependencyAnalyzer
    {
        /// <summary>
        /// Known asset type categories keyed by file extension.
        /// </summary>
        private static readonly Dictionary<string, string> ExtensionToCategory = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Textures
            { ".png", "Texture" }, { ".jpg", "Texture" }, { ".jpeg", "Texture" },
            { ".tga", "Texture" }, { ".psd", "Texture" }, { ".exr", "Texture" },
            { ".hdr", "Texture" }, { ".bmp", "Texture" }, { ".gif", "Texture" },
            // Materials & Shaders
            { ".mat", "Material" }, { ".shader", "Shader" }, { ".shadergraph", "Shader" },
            { ".shadersubgraph", "Shader" }, { ".compute", "Shader" },
            // Models
            { ".fbx", "Model" }, { ".obj", "Model" }, { ".dae", "Model" },
            { ".blend", "Model" }, { ".3ds", "Model" }, { ".max", "Model" },
            // Audio
            { ".wav", "Audio" }, { ".mp3", "Audio" }, { ".ogg", "Audio" },
            { ".aiff", "Audio" }, { ".aif", "Audio" }, { ".flac", "Audio" },
            // Animation
            { ".anim", "AnimationClip" }, { ".controller", "AnimatorController" },
            { ".overrideController", "AnimatorOverride" }, { ".mask", "AvatarMask" },
            // Prefabs & Scenes
            { ".prefab", "Prefab" }, { ".unity", "Scene" },
            // Scripts
            { ".cs", "Script" },
            // Fonts
            { ".ttf", "Font" }, { ".otf", "Font" }, { ".fontsettings", "Font" },
            // ScriptableObject / Generic Asset
            { ".asset", "Asset" },
            // UI Toolkit
            { ".uxml", "UXML" }, { ".uss", "USS" },
            // Video
            { ".mp4", "Video" }, { ".webm", "Video" }, { ".mov", "Video" },
            // Misc
            { ".json", "Data" }, { ".xml", "Data" }, { ".txt", "Data" }, { ".csv", "Data" },
            { ".lighting", "LightingData" }, { ".renderTexture", "RenderTexture" },
            { ".cubemap", "Cubemap" }, { ".flare", "Flare" },
            { ".physicMaterial", "PhysicsMaterial" }, { ".physicsMaterial2D", "PhysicsMaterial2D" },
            { ".mixer", "AudioMixer" }, { ".spriteatlasv2", "SpriteAtlas" }, { ".spriteatlas", "SpriteAtlas" },
        };

        /// <summary>
        /// Classify an asset path into a category string.
        /// </summary>
        private static string ClassifyAsset(string assetPath)
        {
            var ext = Path.GetExtension(assetPath);
            if (string.IsNullOrEmpty(ext)) return "Other";
            return ExtensionToCategory.TryGetValue(ext, out var category) ? category : "Other";
        }

        /// <summary>
        /// Analyze all asset dependencies of a scene.
        /// </summary>
        /// <param name="scenePath">Path to the scene asset (e.g., "Assets/Scenes/Main.unity").</param>
        /// <param name="includeIndirect">If true, include transitive dependencies.</param>
        /// <param name="typeFilter">Optional category filter (e.g., "Material", "Texture").</param>
        /// <returns>Dictionary containing categorized dependency information.</returns>
        public Dictionary<string, object> AnalyzeScene(string scenePath, bool includeIndirect, string typeFilter)
        {
            ValidateScenePath(scenePath);

            var dependencies = AssetDatabase.GetDependencies(scenePath, includeIndirect);

            // Exclude the scene itself
            var filtered = dependencies.Where(d => d != scenePath);

            // Categorize
            var categorized = new Dictionary<string, List<string>>();
            foreach (var dep in filtered)
            {
                var category = ClassifyAsset(dep);

                if (!string.IsNullOrEmpty(typeFilter) &&
                    !string.Equals(category, typeFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!categorized.ContainsKey(category))
                {
                    categorized[category] = new List<string>();
                }
                categorized[category].Add(dep);
            }

            // Sort each category
            foreach (var kvp in categorized)
            {
                kvp.Value.Sort(StringComparer.OrdinalIgnoreCase);
            }

            // Build result
            var result = new Dictionary<string, object>
            {
                ["scenePath"] = scenePath,
                ["includeIndirect"] = includeIndirect,
                ["totalDependencies"] = categorized.Values.Sum(v => v.Count),
            };

            if (!string.IsNullOrEmpty(typeFilter))
            {
                result["typeFilter"] = typeFilter;
            }

            // Categories summary
            var summary = new Dictionary<string, object>();
            foreach (var kvp in categorized.OrderBy(k => k.Key))
            {
                summary[kvp.Key] = kvp.Value.Count;
            }
            result["categorySummary"] = summary;

            // Detailed assets by category
            var details = new Dictionary<string, object>();
            foreach (var kvp in categorized.OrderBy(k => k.Key))
            {
                details[kvp.Key] = kvp.Value;
            }
            result["dependencies"] = details;

            return result;
        }

        /// <summary>
        /// Find all scenes that depend on a specific asset.
        /// </summary>
        /// <param name="assetPath">Path to the asset to search for.</param>
        /// <param name="searchPath">Optional folder to limit scene search (e.g., "Assets/Scenes").</param>
        /// <returns>Dictionary with scenes that use the asset.</returns>
        public Dictionary<string, object> FindAssetUsage(string assetPath, string searchPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new ArgumentException("assetPath is required");
            }

            // Verify asset exists
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if (assetType == null)
            {
                throw new ArgumentException($"Asset not found: {assetPath}");
            }

            var scenePaths = FindAllScenes(searchPath);
            var usages = new List<Dictionary<string, object>>();

            foreach (var scenePath in scenePaths)
            {
                var deps = AssetDatabase.GetDependencies(scenePath, true);
                if (deps.Contains(assetPath))
                {
                    usages.Add(new Dictionary<string, object>
                    {
                        ["scenePath"] = scenePath,
                        ["sceneName"] = Path.GetFileNameWithoutExtension(scenePath),
                    });
                }
            }

            return new Dictionary<string, object>
            {
                ["assetPath"] = assetPath,
                ["assetType"] = ClassifyAsset(assetPath),
                ["totalScenes"] = usages.Count,
                ["scenes"] = usages,
            };
        }

        /// <summary>
        /// Find assets shared across multiple scenes.
        /// </summary>
        /// <param name="scenePaths">Scenes to compare. If null/empty, uses all project scenes.</param>
        /// <param name="minSharedCount">Minimum number of scenes an asset must be shared across (default: 2).</param>
        /// <param name="typeFilter">Optional category filter.</param>
        /// <returns>Dictionary with shared asset information.</returns>
        public Dictionary<string, object> FindSharedAssets(string[] scenePaths, int minSharedCount, string typeFilter)
        {
            if (minSharedCount < 2) minSharedCount = 2;

            // Get scenes to analyze
            if (scenePaths == null || scenePaths.Length == 0)
            {
                scenePaths = FindAllScenes(null);
            }

            // Collect dependencies for each scene
            var assetToScenes = new Dictionary<string, List<string>>();

            foreach (var scenePath in scenePaths)
            {
                ValidateScenePath(scenePath);
                var deps = AssetDatabase.GetDependencies(scenePath, true);
                foreach (var dep in deps)
                {
                    if (dep == scenePath) continue; // Skip scene itself

                    if (!string.IsNullOrEmpty(typeFilter))
                    {
                        var category = ClassifyAsset(dep);
                        if (!string.Equals(category, typeFilter, StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    if (!assetToScenes.ContainsKey(dep))
                    {
                        assetToScenes[dep] = new List<string>();
                    }
                    if (!assetToScenes[dep].Contains(scenePath))
                    {
                        assetToScenes[dep].Add(scenePath);
                    }
                }
            }

            // Filter by minSharedCount
            var sharedAssets = assetToScenes
                .Where(kvp => kvp.Value.Count >= minSharedCount)
                .OrderByDescending(kvp => kvp.Value.Count)
                .ThenBy(kvp => kvp.Key)
                .Select(kvp => new Dictionary<string, object>
                {
                    ["assetPath"] = kvp.Key,
                    ["assetType"] = ClassifyAsset(kvp.Key),
                    ["sharedByCount"] = kvp.Value.Count,
                    ["scenes"] = kvp.Value,
                })
                .ToList();

            return new Dictionary<string, object>
            {
                ["analyzedScenes"] = scenePaths.ToList(),
                ["minSharedCount"] = minSharedCount,
                ["totalSharedAssets"] = sharedAssets.Count,
                ["sharedAssets"] = sharedAssets,
            };
        }

        /// <summary>
        /// Find assets under a search path that are not referenced by any scene.
        /// </summary>
        /// <param name="searchPath">Folder to search for assets (e.g., "Assets/Art").</param>
        /// <param name="typeFilter">Optional category filter.</param>
        /// <returns>Dictionary with unused asset information.</returns>
        public Dictionary<string, object> FindUnusedAssets(string searchPath, string typeFilter)
        {
            if (string.IsNullOrEmpty(searchPath))
            {
                searchPath = "Assets";
            }

            // Collect all scene dependencies
            var allScenes = FindAllScenes(null);
            var allUsedAssets = new HashSet<string>();

            foreach (var scenePath in allScenes)
            {
                var deps = AssetDatabase.GetDependencies(scenePath, true);
                foreach (var dep in deps)
                {
                    allUsedAssets.Add(dep);
                }
            }

            // Find all assets under searchPath
            var guids = AssetDatabase.FindAssets("", new[] { searchPath });
            var unusedAssets = new List<Dictionary<string, object>>();

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // Skip folders, scenes, and meta files
                if (AssetDatabase.IsValidFolder(assetPath)) continue;
                if (assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)) continue;

                var category = ClassifyAsset(assetPath);

                if (!string.IsNullOrEmpty(typeFilter) &&
                    !string.Equals(category, typeFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!allUsedAssets.Contains(assetPath))
                {
                    unusedAssets.Add(new Dictionary<string, object>
                    {
                        ["assetPath"] = assetPath,
                        ["assetType"] = category,
                    });
                }
            }

            // Group by type for summary
            var categorySummary = unusedAssets
                .GroupBy(a => a["assetType"] as string)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => (object)g.Count());

            return new Dictionary<string, object>
            {
                ["searchPath"] = searchPath,
                ["totalScenesChecked"] = allScenes.Length,
                ["totalUnusedAssets"] = unusedAssets.Count,
                ["categorySummary"] = categorySummary,
                ["unusedAssets"] = unusedAssets.OrderBy(a => a["assetPath"] as string).ToList(),
            };
        }

        #region Helpers

        private static void ValidateScenePath(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                throw new ArgumentException("scenePath is required");
            }
            if (!scenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid scene path (must end with .unity): {scenePath}");
            }
            if (AssetDatabase.GetMainAssetTypeAtPath(scenePath) == null)
            {
                throw new ArgumentException($"Scene not found: {scenePath}");
            }
        }

        private static string[] FindAllScenes(string searchPath)
        {
            string[] searchFolders = string.IsNullOrEmpty(searchPath)
                ? new[] { "Assets" }
                : new[] { searchPath };

            var guids = AssetDatabase.FindAssets("t:Scene", searchFolders);
            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p)
                .ToArray();
        }

        #endregion
    }
}
