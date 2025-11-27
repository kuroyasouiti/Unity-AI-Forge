using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace MCP.Editor
{
    /// <summary>
    /// Scene management operations for McpCommandProcessor.
    /// Handles scene creation, loading, saving, deletion, and build settings management.
    /// </summary>
    internal static partial class McpCommandProcessor
    {
        #region Scene Management

        /// <summary>
        /// Handles scene management operations (create, load, save, delete, duplicate).
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation' type and scene path.</param>
        /// <returns>Result dictionary with operation-specific data.</returns>
        /// <exception cref="InvalidOperationException">Thrown when operation is invalid or missing.</exception>
        private static object HandleSceneManage(Dictionary<string, object> payload)
        {
            var operation = GetString(payload, "operation");
            if (string.IsNullOrEmpty(operation))
            {
                throw new InvalidOperationException("operation is required");
            }

            // Check if compilation is in progress and wait if necessary (except for read-only operations)
            Dictionary<string, object> compilationWaitInfo = null;
            if (operation != "listBuildSettings" && operation != "inspect")
            {
                compilationWaitInfo = EnsureNoCompilationInProgress("sceneManage", maxWaitSeconds: 30f);
            }

            object result;
            switch (operation)
            {
                case "create":
                    result = CreateScene(payload);
                    break;
                case "load":
                    result = LoadScene(payload);
                    break;
                case "save":
                    result = SaveScenes(payload);
                    break;
                case "delete":
                    result = DeleteScene(payload);
                    break;
                case "duplicate":
                    result = DuplicateScene(payload);
                    break;
                case "inspect":
                    result = InspectScene(payload);
                    break;
                case "listBuildSettings":
                    result = ListBuildSettings(payload);
                    break;
                case "addToBuildSettings":
                    result = AddToBuildSettings(payload);
                    break;
                case "removeFromBuildSettings":
                    result = RemoveFromBuildSettings(payload);
                    break;
                case "reorderBuildSettings":
                    result = ReorderBuildSettings(payload);
                    break;
                case "setBuildSettingsEnabled":
                    result = SetBuildSettingsEnabled(payload);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown sceneManage operation: {operation}");
            }

            // Add compilation wait info if we waited
            if (compilationWaitInfo != null && result is Dictionary<string, object> resultDict)
            {
                resultDict["compilationWait"] = compilationWaitInfo;
            }

            return result;
        }

        /// <summary>
        /// Creates a new scene.
        /// </summary>
        /// <param name="payload">Parameters including optional scenePath and additive flag.</param>
        /// <returns>Dictionary containing scene path, name, and dirty state.</returns>
        private static object CreateScene(Dictionary<string, object> payload)
        {
            var additive = GetBool(payload, "additive");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, additive ? NewSceneMode.Additive : NewSceneMode.Single);

            var scenePath = GetString(payload, "scenePath");
            if (!string.IsNullOrEmpty(scenePath))
            {
                EnsureDirectoryExists(scenePath);
                EditorSceneManager.SaveScene(scene, scenePath);
                AssetDatabase.Refresh();
            }

            return new Dictionary<string, object>
            {
                ["path"] = scene.path,
                ["name"] = scene.name,
                ["isDirty"] = scene.isDirty,
            };
        }

        /// <summary>
        /// Loads an existing scene.
        /// </summary>
        /// <param name="payload">Parameters including scenePath and optional additive flag.</param>
        /// <returns>Dictionary containing scene path and loaded state.</returns>
        private static object LoadScene(Dictionary<string, object> payload)
        {
            var scenePath = EnsureValue(GetString(payload, "scenePath"), "scenePath");
            var additive = GetBool(payload, "additive");
            var openMode = additive ? OpenSceneMode.Additive : OpenSceneMode.Single;
            var scene = EditorSceneManager.OpenScene(scenePath, openMode);

            return new Dictionary<string, object>
            {
                ["path"] = scene.path,
                ["isLoaded"] = scene.isLoaded,
            };
        }

        /// <summary>
        /// Saves one or more scenes.
        /// </summary>
        /// <param name="payload">Parameters including optional scenePath and includeOpenScenes flag.</param>
        /// <returns>Dictionary containing list of saved scene paths.</returns>
        private static object SaveScenes(Dictionary<string, object> payload)
        {
            var includeOpen = GetBool(payload, "includeOpenScenes");
            var scenePath = GetString(payload, "scenePath");
            var savedScenes = new List<object>();

            if (includeOpen)
            {
                var count = EditorSceneManager.sceneCount;
                for (var i = 0; i < count; i++)
                {
                    var scene = EditorSceneManager.GetSceneAt(i);
                    if (!scene.IsValid())
                    {
                        continue;
                    }

                    EditorSceneManager.SaveScene(scene);
                    savedScenes.Add(scene.path);
                }
            }
            else if (!string.IsNullOrEmpty(scenePath))
            {
                var scene = SceneManager.GetSceneByPath(scenePath);
                if (!scene.IsValid())
                {
                    throw new InvalidOperationException($"Scene not loaded: {scenePath}");
                }

                EditorSceneManager.SaveScene(scene, scenePath);
                savedScenes.Add(scenePath);
            }
            else
            {
                var activeScene = SceneManager.GetActiveScene();
                EditorSceneManager.SaveScene(activeScene);
                savedScenes.Add(activeScene.path);
            }

            AssetDatabase.Refresh();

            return new Dictionary<string, object>
            {
                ["savedScenes"] = savedScenes,
            };
        }

        /// <summary>
        /// Deletes a scene asset.
        /// </summary>
        /// <param name="payload">Parameters including scenePath.</param>
        /// <returns>Dictionary containing the deleted scene path.</returns>
        private static object DeleteScene(Dictionary<string, object> payload)
        {
            var scenePath = EnsureValue(GetString(payload, "scenePath"), "scenePath");
            if (!AssetDatabase.DeleteAsset(scenePath))
            {
                throw new InvalidOperationException($"Failed to delete scene: {scenePath}");
            }

            AssetDatabase.Refresh();
            return new Dictionary<string, object>
            {
                ["deleted"] = scenePath,
            };
        }

        /// <summary>
        /// Duplicates a scene asset.
        /// </summary>
        /// <param name="payload">Parameters including scenePath and optional newSceneName.</param>
        /// <returns>Dictionary containing source and destination paths.</returns>
        private static object DuplicateScene(Dictionary<string, object> payload)
        {
            var scenePath = EnsureValue(GetString(payload, "scenePath"), "scenePath");
            var newName = GetString(payload, "newSceneName");
            if (string.IsNullOrEmpty(newName))
            {
                newName = Path.GetFileNameWithoutExtension(scenePath) + " Copy";
            }

            var destination = Path.Combine(Path.GetDirectoryName(scenePath) ?? "", newName + ".unity");
            EnsureDirectoryExists(destination);

            if (!AssetDatabase.CopyAsset(scenePath, destination))
            {
                throw new InvalidOperationException($"Failed to duplicate scene {scenePath}");
            }

            AssetDatabase.Refresh();
            return new Dictionary<string, object>
            {
                ["source"] = scenePath,
                ["destination"] = destination,
            };
        }

        /// <summary>
        /// Inspects a scene by delegating to context inspection.
        /// </summary>
        /// <param name="payload">Inspection parameters.</param>
        /// <returns>Scene inspection data.</returns>
        private static object InspectScene(Dictionary<string, object> payload)
        {
            // Delegate to existing HandleContextInspect logic
            return HandleContextInspect(payload);
        }

        #endregion

        #region Scene Build Settings

        /// <summary>
        /// Lists all scenes in build settings.
        /// </summary>
        /// <param name="payload">Operation parameters (unused).</param>
        /// <returns>Dictionary containing list of scenes with their paths, enabled state, GUID, and index.</returns>
        private static object ListBuildSettings(Dictionary<string, object> payload)
        {
            var scenes = EditorBuildSettings.scenes;
            var sceneList = new List<object>();

            for (int i = 0; i < scenes.Length; i++)
            {
                var scene = scenes[i];
                sceneList.Add(new Dictionary<string, object>
                {
                    ["path"] = scene.path,
                    ["enabled"] = scene.enabled,
                    ["guid"] = scene.guid.ToString(),
                    ["index"] = i
                });
            }

            return new Dictionary<string, object>
            {
                ["scenes"] = sceneList,
                ["count"] = scenes.Length
            };
        }

        /// <summary>
        /// Adds a scene to build settings.
        /// </summary>
        /// <param name="payload">Parameters including scenePath, optional enabled flag, and index.</param>
        /// <returns>Dictionary containing scene path, enabled state, index, and total scene count.</returns>
        private static object AddToBuildSettings(Dictionary<string, object> payload)
        {
            var scenePath = EnsureValue(GetString(payload, "scenePath"), "scenePath");
            var enabled = GetBool(payload, "enabled", true);
            var index = GetInt(payload, "index", -1);

            // Verify scene exists
            if (!File.Exists(scenePath))
            {
                throw new InvalidOperationException($"Scene not found: {scenePath}");
            }

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            // Check if scene already exists
            var existingIndex = scenes.FindIndex(s => s.path == scenePath);
            if (existingIndex >= 0)
            {
                throw new InvalidOperationException($"Scene already in build settings at index {existingIndex}");
            }

            var newScene = new EditorBuildSettingsScene(scenePath, enabled);

            if (index >= 0 && index <= scenes.Count)
            {
                scenes.Insert(index, newScene);
            }
            else
            {
                scenes.Add(newScene);
                index = scenes.Count - 1;
            }

            EditorBuildSettings.scenes = scenes.ToArray();

            return new Dictionary<string, object>
            {
                ["path"] = scenePath,
                ["enabled"] = enabled,
                ["index"] = index,
                ["totalScenes"] = scenes.Count
            };
        }

        /// <summary>
        /// Removes a scene from build settings by path or index.
        /// </summary>
        /// <param name="payload">Parameters including either scenePath or index.</param>
        /// <returns>Dictionary containing removed scene path, index, and total scene count.</returns>
        private static object RemoveFromBuildSettings(Dictionary<string, object> payload)
        {
            var scenePath = GetString(payload, "scenePath");
            var index = GetInt(payload, "index", -1);

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            if (!string.IsNullOrEmpty(scenePath))
            {
                // Remove by path
                var removed = scenes.RemoveAll(s => s.path == scenePath);
                if (removed == 0)
                {
                    throw new InvalidOperationException($"Scene not found in build settings: {scenePath}");
                }

                EditorBuildSettings.scenes = scenes.ToArray();

                return new Dictionary<string, object>
                {
                    ["removed"] = scenePath,
                    ["count"] = removed,
                    ["totalScenes"] = scenes.Count
                };
            }
            else if (index >= 0 && index < scenes.Count)
            {
                // Remove by index
                var removedScene = scenes[index];
                scenes.RemoveAt(index);
                EditorBuildSettings.scenes = scenes.ToArray();

                return new Dictionary<string, object>
                {
                    ["removed"] = removedScene.path,
                    ["index"] = index,
                    ["totalScenes"] = scenes.Count
                };
            }
            else
            {
                throw new InvalidOperationException("Either scenePath or valid index must be provided");
            }
        }

        /// <summary>
        /// Reorders a scene in build settings.
        /// </summary>
        /// <param name="payload">Parameters including toIndex and either scenePath or fromIndex.</param>
        /// <returns>Dictionary containing scene path, fromIndex, toIndex, and total scene count.</returns>
        private static object ReorderBuildSettings(Dictionary<string, object> payload)
        {
            var scenePath = GetString(payload, "scenePath");
            var fromIndex = GetInt(payload, "fromIndex", -1);
            var toIndex = GetInt(payload, "toIndex", -1);

            if (toIndex < 0)
            {
                throw new InvalidOperationException("toIndex is required and must be >= 0");
            }

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            int sourceIndex = fromIndex;

            if (!string.IsNullOrEmpty(scenePath))
            {
                // Find by path
                sourceIndex = scenes.FindIndex(s => s.path == scenePath);
                if (sourceIndex < 0)
                {
                    throw new InvalidOperationException($"Scene not found in build settings: {scenePath}");
                }
            }
            else if (fromIndex >= 0 && fromIndex < scenes.Count)
            {
                sourceIndex = fromIndex;
            }
            else
            {
                throw new InvalidOperationException("Either scenePath or valid fromIndex must be provided");
            }

            if (toIndex < 0 || toIndex >= scenes.Count)
            {
                throw new InvalidOperationException($"Invalid toIndex: {toIndex} (must be 0-{scenes.Count - 1})");
            }

            if (sourceIndex == toIndex)
            {
                return new Dictionary<string, object>
                {
                    ["message"] = "Scene already at target position",
                    ["path"] = scenes[sourceIndex].path,
                    ["index"] = toIndex
                };
            }

            var scene = scenes[sourceIndex];
            scenes.RemoveAt(sourceIndex);
            scenes.Insert(toIndex, scene);

            EditorBuildSettings.scenes = scenes.ToArray();

            return new Dictionary<string, object>
            {
                ["path"] = scene.path,
                ["fromIndex"] = sourceIndex,
                ["toIndex"] = toIndex,
                ["totalScenes"] = scenes.Count
            };
        }

        /// <summary>
        /// Enables or disables a scene in build settings.
        /// </summary>
        /// <param name="payload">Parameters including enabled flag and either scenePath or index.</param>
        /// <returns>Dictionary containing scene path, enabled state, and index.</returns>
        private static object SetBuildSettingsEnabled(Dictionary<string, object> payload)
        {
            var scenePath = GetString(payload, "scenePath");
            var index = GetInt(payload, "index", -1);
            var enabled = GetBool(payload, "enabled", true);

            var scenes = EditorBuildSettings.scenes;
            int targetIndex = -1;

            if (!string.IsNullOrEmpty(scenePath))
            {
                // Find by path
                for (int i = 0; i < scenes.Length; i++)
                {
                    if (scenes[i].path == scenePath)
                    {
                        targetIndex = i;
                        break;
                    }
                }

                if (targetIndex < 0)
                {
                    throw new InvalidOperationException($"Scene not found in build settings: {scenePath}");
                }
            }
            else if (index >= 0 && index < scenes.Length)
            {
                targetIndex = index;
            }
            else
            {
                throw new InvalidOperationException("Either scenePath or valid index must be provided");
            }

            scenes[targetIndex].enabled = enabled;
            EditorBuildSettings.scenes = scenes;

            return new Dictionary<string, object>
            {
                ["path"] = scenes[targetIndex].path,
                ["enabled"] = enabled,
                ["index"] = targetIndex
            };
        }

        #endregion
    }
}

