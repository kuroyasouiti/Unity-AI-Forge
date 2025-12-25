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
        /// コンパイルが開始されたかどうかを確認します。
        /// ブロッキング待機は行いません。Python側で非同期メッセージを使用して待機します。
        /// </summary>
        /// <param name="wasCompilingBefore">操作前にコンパイル中だったかどうか。</param>
        /// <returns>コンパイル中の場合はtrue、そうでない場合はfalse。</returns>
        private static bool DetectCompilationStart(bool wasCompilingBefore)
        {
            // 既にコンパイル中だった場合、または現在コンパイル中の場合
            if (wasCompilingBefore || EditorApplication.isCompiling)
            {
                Debug.Log("MCP Bridge: Compilation detected - Python side will handle async waiting");
                return true;
            }

            // コンパイルが検出されなかった
            // NOTE: Unity の AssetDatabase.Refresh() 後、コンパイルは非同期で開始される
            // Python側で compilation:started メッセージを受信して状態を追跡する
            return false;
        }

        /// <summary>
        /// 現在のコンパイル状態を確認します。
        /// ブロッキング待機は行いません。
        /// </summary>
        /// <returns>コンパイル中でない場合はtrue、コンパイル中の場合はfalse。</returns>
        private static bool IsCompilationComplete()
        {
            return !EditorApplication.isCompiling;
        }

        /// <summary>
        /// コンパイルが進行中かどうかを確認します。
        /// ブロッキング待機は行いません。Python側で非同期メッセージを使用して待機します。
        /// </summary>
        /// <param name="toolName">実行中のツール名（ログ用）。</param>
        /// <returns>コンパイル中の場合は状態情報を含む辞書、そうでない場合はnull。</returns>
        private static Dictionary<string, object> CheckCompilationInProgress(string toolName)
        {
            if (!EditorApplication.isCompiling)
            {
                return null; // コンパイル中ではない
            }

            Debug.Log($"MCP Bridge: Tool '{toolName}' called during compilation - Python side will handle async waiting");
            return new Dictionary<string, object>
            {
                ["isCompiling"] = true,
                ["waitedForCompilation"] = false,
                ["compilationCompleted"] = false,
                ["message"] = "Compilation in progress - Python side will handle async waiting"
            };
        }

        // 後方互換性のためのエイリアス
        private static Dictionary<string, object> EnsureNoCompilationInProgress(string toolName, float maxWaitSeconds = 30f)
        {
            return CheckCompilationInProgress(toolName);
        }

        // 後方互換性のためのエイリアス
        private static bool WaitForCompilationComplete(float maxWaitSeconds = 30f)
        {
            return IsCompilationComplete();
        }

        #endregion
    }
}

