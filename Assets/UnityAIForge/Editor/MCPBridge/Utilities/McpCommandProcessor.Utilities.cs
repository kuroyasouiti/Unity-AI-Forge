using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor
{
    internal static partial class McpCommandProcessor
    {
        #region Context Inspection

        /// <summary>
        /// Handles context inspection to retrieve scene hierarchy and statistics.
        /// </summary>
        /// <param name="payload">Operation parameters including hierarchy and component inclusion flags.</param>
        /// <returns>Result dictionary with scene context, hierarchy, and statistics.</returns>
        private static object HandleContextInspect(Dictionary<string, object> payload)
        {
            try
            {
                var includeHierarchy = GetBool(payload, "includeHierarchy", true);
                var includeComponents = GetBool(payload, "includeComponents", false);
                var filter = GetString(payload, "filter");

                Debug.Log($"[contextInspect] Inspecting scene context");

                var result = new Dictionary<string, object>
                {
                    ["sceneName"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                    ["scenePath"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                };

                if (includeHierarchy)
                {
                    var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                    var hierarchy = new List<object>();

                    foreach (var root in rootObjects)
                    {
                        if (!string.IsNullOrEmpty(filter))
                        {
                            if (!McpWildcardUtility.IsMatch(root.name, filter, false))
                            {
                                continue;
                            }
                        }

                        hierarchy.Add(BuildHierarchyInfo(root, includeComponents, filter));
                    }

                    result["hierarchy"] = hierarchy;
                    result["rootObjectCount"] = rootObjects.Length;
                }

                // Add scene statistics
                var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                result["totalGameObjects"] = allObjects.Length;

                var cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                result["cameraCount"] = cameras.Length;

                var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
                result["lightCount"] = lights.Length;

                var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                result["canvasCount"] = canvases.Length;

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[contextInspect] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Builds hierarchy information for a GameObject including components and children.
        /// </summary>
        /// <param name="go">The GameObject to inspect.</param>
        /// <param name="includeComponents">Whether to include component information.</param>
        /// <param name="filter">Optional filter pattern for child names.</param>
        /// <returns>Dictionary containing GameObject hierarchy information.</returns>
        private static Dictionary<string, object> BuildHierarchyInfo(GameObject go, bool includeComponents, string filter)
        {
            var info = new Dictionary<string, object>
            {
                ["name"] = go.name,
                ["path"] = GetHierarchyPath(go),
                ["active"] = go.activeSelf,
                ["childCount"] = go.transform.childCount,
            };

            if (includeComponents)
            {
                var components = new List<string>();
                foreach (var comp in go.GetComponents<Component>())
                {
                    if (comp != null)
                    {
                        components.Add(comp.GetType().Name);
                    }
                }
                info["components"] = components;
            }

            // Always include direct child names (one level only)
            if (go.transform.childCount > 0)
            {
                var childNames = new List<string>();
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    var child = go.transform.GetChild(i).gameObject;

                    if (!string.IsNullOrEmpty(filter))
                    {
                        if (!McpWildcardUtility.IsMatch(child.name, filter, false))
                        {
                            continue;
                        }
                    }

                    childNames.Add(child.name);
                }
                if (childNames.Count > 0)
                {
                    info["childNames"] = childNames;
                }
            }

            return info;
        }

        #endregion

        #region Compilation Management

        /// <summary>
        /// Detects if compilation has started after script operations.
        /// Waits for a short period and checks if EditorApplication.isCompiling becomes true.
        /// </summary>
        /// <param name="wasCompilingBefore">Whether compilation was already running before operations.</param>
        /// <param name="maxWaitSeconds">Maximum time to wait for compilation to start.</param>
        /// <returns>True if compilation started, false otherwise.</returns>
        private static bool DetectCompilationStart(bool wasCompilingBefore, float maxWaitSeconds = 1.5f)
        {
            // If already compiling, compilation was triggered
            if (wasCompilingBefore)
            {
                return true;
            }

            // Wait and check if compilation starts
            var startTime = EditorApplication.timeSinceStartup;
            var checkInterval = 0.1f; // Check every 100ms

            while ((EditorApplication.timeSinceStartup - startTime) < maxWaitSeconds)
            {
                if (EditorApplication.isCompiling)
                {
                    Debug.Log("MCP Bridge: Compilation detected after script operations");
                    return true;
                }

                // Small delay before next check
                System.Threading.Thread.Sleep((int)(checkInterval * 1000));
            }

            Debug.Log("MCP Bridge: No compilation detected after script operations");
            return false;
        }

        /// <summary>
        /// Waits for ongoing compilation to complete.
        /// Returns immediately if not compiling.
        /// </summary>
        /// <param name="maxWaitSeconds">Maximum time to wait for compilation to complete.</param>
        /// <returns>True if compilation completed successfully, false if timeout or still compiling.</returns>
        private static bool WaitForCompilationComplete(float maxWaitSeconds = 30f)
        {
            if (!EditorApplication.isCompiling)
            {
                return true; // Not compiling, return immediately
            }

            Debug.Log($"MCP Bridge: Waiting for ongoing compilation to complete (max {maxWaitSeconds}s)...");
            var startTime = EditorApplication.timeSinceStartup;
            var checkInterval = 0.2f; // Check every 200ms

            while ((EditorApplication.timeSinceStartup - startTime) < maxWaitSeconds)
            {
                if (!EditorApplication.isCompiling)
                {
                    var elapsedSeconds = EditorApplication.timeSinceStartup - startTime;
                    Debug.Log($"MCP Bridge: Compilation completed after {elapsedSeconds:F1}s");
                    return true;
                }

                // Small delay before next check
                System.Threading.Thread.Sleep((int)(checkInterval * 1000));
            }

            Debug.LogWarning($"MCP Bridge: Compilation did not complete within {maxWaitSeconds}s");
            return false;
        }

        /// <summary>
        /// Ensures no compilation is in progress before executing a tool operation.
        /// If compilation is in progress, waits for it to complete.
        /// </summary>
        /// <param name="toolName">Name of the tool being executed (for logging).</param>
        /// <param name="maxWaitSeconds">Maximum time to wait for compilation to complete.</param>
        /// <returns>Dictionary with status information if waiting occurred, null if no wait was needed.</returns>
        private static Dictionary<string, object> EnsureNoCompilationInProgress(string toolName, float maxWaitSeconds = 30f)
        {
            if (!EditorApplication.isCompiling)
            {
                return null; // No compilation in progress
            }

            Debug.Log($"MCP Bridge: Tool '{toolName}' called during compilation, waiting for completion...");
            var startTime = EditorApplication.timeSinceStartup;
            var completed = WaitForCompilationComplete(maxWaitSeconds);
            var elapsedSeconds = EditorApplication.timeSinceStartup - startTime;

            return new Dictionary<string, object>
            {
                ["waitedForCompilation"] = true,
                ["compilationCompleted"] = completed,
                ["waitTimeSeconds"] = (float)Math.Round(elapsedSeconds, 2),
                ["message"] = completed
                    ? $"Waited {elapsedSeconds:F1}s for ongoing compilation to complete"
                    : $"Compilation did not complete within {maxWaitSeconds}s"
            };
        }

        #endregion
    }
}

