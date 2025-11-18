using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MCP.Editor
{
    /// <summary>
    /// Collects Unity Editor context information for MCP clients.
    /// Gathers scene hierarchy, selection, asset index, and Git status.
    /// </summary>
    internal static class McpContextCollector
    {
        private const int MaxAssets = 200;
        private static readonly string[] AssetTypeFilters =
        {
            "Script",
            "Prefab",
            "Material",
            "Scene",
            "ScriptableObject",
            "Shader",
        };

        // Cached asset index to reduce performance impact
        private static List<object> _cachedAssetIndex;
        private static DateTime _lastAssetIndexUpdate = DateTime.MinValue;
        private static readonly TimeSpan AssetIndexCacheLifetime = TimeSpan.FromSeconds(30);
        private static Task<List<object>> _assetIndexTask;

        /// <summary>
        /// Builds a complete context payload containing all relevant project information.
        /// </summary>
        /// <returns>Dictionary with activeScene, hierarchy, selection, assets, and gitDiffSummary.</returns>
        public static Dictionary<string, object> BuildContextPayload()
        {
            return new Dictionary<string, object>
            {
                ["activeScene"] = BuildActiveSceneInfo(),
                ["hierarchy"] = BuildHierarchyTree(),
                ["selection"] = BuildSelectionInfo(),
                ["assets"] = GetAssetIndexCached(),
                ["gitDiffSummary"] = TryCaptureGitStatus(),
                ["updatedAt"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            };
        }

        /// <summary>
        /// Invalidates the asset index cache, forcing a refresh on next access.
        /// Call this when assets are added, removed, or modified.
        /// </summary>
        public static void InvalidateAssetIndexCache()
        {
            _cachedAssetIndex = null;
            _lastAssetIndexUpdate = DateTime.MinValue;
        }

        /// <summary>
        /// Gets the asset index, using cached data if available and fresh.
        /// Automatically refreshes cache if expired or starts async refresh.
        /// </summary>
        /// <returns>List of asset information dictionaries.</returns>
        private static List<object> GetAssetIndexCached()
        {
            var now = DateTime.UtcNow;
            var cacheAge = now - _lastAssetIndexUpdate;

            // Return cached data if still fresh
            if (_cachedAssetIndex != null && cacheAge < AssetIndexCacheLifetime)
            {
                return _cachedAssetIndex;
            }

            // If async task is running, return cached data (even if stale) or empty list
            if (_assetIndexTask != null && !_assetIndexTask.IsCompleted)
            {
                return _cachedAssetIndex ?? new List<object>();
            }

            // Start async refresh if needed
            if (_assetIndexTask == null || _assetIndexTask.IsCompleted)
            {
                _assetIndexTask = Task.Run(() => BuildAssetIndex());
            }

            // Check if task completed immediately (small projects)
            if (_assetIndexTask.IsCompleted)
            {
                _cachedAssetIndex = _assetIndexTask.Result;
                _lastAssetIndexUpdate = now;
                return _cachedAssetIndex;
            }

            // Return stale cache or empty list while async task runs
            return _cachedAssetIndex ?? new List<object>();
        }

        private static Dictionary<string, object> BuildActiveSceneInfo()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                return null;
            }

            return new Dictionary<string, object>
            {
                ["name"] = scene.name,
                ["path"] = scene.path,
            };
        }

        private static Dictionary<string, object> BuildHierarchyTree()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            var rootChildren = roots
                .Select(go => BuildHierarchyNode(go, 0))
                .Where(node => node != null)
                .ToList();

            return new Dictionary<string, object>
            {
                ["id"] = "scene-root",
                ["name"] = scene.name,
                ["type"] = "Scene",
                ["children"] = rootChildren,
            };
        }

        private static Dictionary<string, object> BuildHierarchyNode(GameObject go, int depth)
        {
            if (go == null)
            {
                return null;
            }

            var components = go.GetComponents<Component>()
                .Where(component => component != null)
                .Select(component => new Dictionary<string, object>
                {
                    ["type"] = component.GetType().FullName,
                    ["enabled"] = component is Behaviour behaviour ? behaviour.enabled : (bool?)null,
                })
                .ToList();

            // Only collect direct child names, not their full hierarchy
            var childNames = new List<string>();
            for (var i = 0; i < go.transform.childCount; i++)
            {
                var child = go.transform.GetChild(i);
                if (child != null && child.gameObject != null)
                {
                    childNames.Add(child.gameObject.name);
                }
            }

            return new Dictionary<string, object>
            {
                ["id"] = go.GetInstanceID().ToString(),
                ["name"] = go.name,
                ["type"] = ResolveHierarchyType(go),
                ["components"] = components,
                ["childCount"] = go.transform.childCount,
                ["childNames"] = childNames,
            };
        }

        private static string ResolveHierarchyType(GameObject go)
        {
            if (go.GetComponent<RectTransform>() != null)
            {
                return "UIElement";
            }

            if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
            {
                return "PrefabInstance";
            }

            return "GameObject";
        }

        private static List<object> BuildSelectionInfo()
        {
            return Selection.objects
                .Where(obj => obj != null)
                .Select(obj => new Dictionary<string, object>
                {
                    ["name"] = obj.name,
                    ["type"] = obj.GetType().FullName,
                    ["path"] = AssetDatabase.GetAssetPath(obj),
                    ["guid"] = string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj))
                        ? null
                        : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj)),
                })
                .Cast<object>()
                .ToList();
        }

        private static List<object> BuildAssetIndex()
        {
            var guids = new List<string>(MaxAssets);
            var seen = new HashSet<string>();

            foreach (var type in AssetTypeFilters)
            {
                foreach (var guid in AssetDatabase.FindAssets($"t:{type}"))
                {
                    if (!seen.Add(guid))
                    {
                        continue;
                    }

                    guids.Add(guid);

                    if (guids.Count >= MaxAssets)
                    {
                        break;
                    }
                }

                if (guids.Count >= MaxAssets)
                {
                    break;
                }
            }

            var results = new List<object>(guids.Count);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                results.Add(new Dictionary<string, object>
                {
                    ["guid"] = guid,
                    ["path"] = path,
                    ["type"] = type != null ? type.FullName : null,
                });
            }

            return results;
        }

        private static string TryCaptureGitStatus()
        {
            try
            {
                var projectRoot = Directory.GetCurrentDirectory();
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "status --short",
                    WorkingDirectory = projectRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    return null;
                }

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(1500);

                if (process.ExitCode != 0)
                {
                    return null;
                }

                var sb = new StringBuilder();
                foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    sb.AppendLine(line.Trim());
                }

                return sb.ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
