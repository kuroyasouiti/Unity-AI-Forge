using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace MCP.Editor
{
    /// <summary>
    /// Processes MCP tool commands and executes corresponding Unity Editor operations.
    /// Supports management operations for scenes, GameObjects, components, and assets.
    /// </summary>
    internal static class McpCommandProcessor
    {
        /// <summary>
        /// Executes an MCP command and returns the result.
        /// </summary>
        /// <param name="command">The command to execute containing tool name and payload.</param>
        /// <returns>Result dictionary with operation-specific data.</returns>
        /// <exception cref="InvalidOperationException">Thrown when tool name is not supported.</exception>
        public static object Execute(McpIncomingCommand command)
        {
            return command.ToolName switch
            {
                "pingUnityEditor" => HandlePing(),
                "sceneManage" => HandleSceneManage(command.Payload),
                "gameObjectManage" => HandleGameObjectManage(command.Payload),
                "componentManage" => HandleComponentManage(command.Payload),
                "assetManage" => HandleAssetManage(command.Payload),
                "uguiRectAdjust" => HandleUguiRectAdjust(command.Payload),
                "uguiAnchorManage" => HandleUguiAnchorManage(command.Payload),
                "uguiManage" => HandleUguiManage(command.Payload),
                "uguiCreateFromTemplate" => HandleUguiCreateFromTemplate(command.Payload),
                "uguiLayoutManage" => HandleUguiLayoutManage(command.Payload),
                "uguiDetectOverlaps" => HandleUguiDetectOverlaps(command.Payload),
                "hierarchyBuilder" => HandleHierarchyBuilder(command.Payload),
                "sceneQuickSetup" => HandleSceneQuickSetup(command.Payload),
                "gameObjectCreateFromTemplate" => HandleGameObjectCreateFromTemplate(command.Payload),
                "contextInspect" => HandleContextInspect(command.Payload),
                "tagLayerManage" => HandleTagLayerManage(command.Payload),
                "prefabManage" => HandlePrefabManage(command.Payload),
                "projectSettingsManage" => HandleProjectSettingsManage(command.Payload),
                "renderPipelineManage" => HandleRenderPipelineManage(command.Payload),
                "constantConvert" => HandleConstantConvert(command.Payload),
                "batchExecute" => HandleBatchExecute(command.Payload),
                _ => throw new InvalidOperationException($"Unsupported tool name: {command.ToolName}"),
            };
        }

        /// <summary>
        /// Handles ping requests to verify Unity Editor connectivity.
        /// </summary>
        /// <returns>Dictionary containing Unity version, project name, and current timestamp.</returns>
        private static object HandlePing()
        {
            return new Dictionary<string, object>
            {
                ["editor"] = Application.unityVersion,
                ["project"] = Application.productName,
                ["time"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            };
        }

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

            switch (operation)
            {
                case "create":
                    return CreateScene(payload);
                case "load":
                    return LoadScene(payload);
                case "save":
                    return SaveScenes(payload);
                case "delete":
                    return DeleteScene(payload);
                case "duplicate":
                    return DuplicateScene(payload);
                case "listBuildSettings":
                    return ListBuildSettings(payload);
                case "addToBuildSettings":
                    return AddToBuildSettings(payload);
                case "removeFromBuildSettings":
                    return RemoveFromBuildSettings(payload);
                case "reorderBuildSettings":
                    return ReorderBuildSettings(payload);
                case "setBuildSettingsEnabled":
                    return SetBuildSettingsEnabled(payload);
                default:
                    throw new InvalidOperationException($"Unknown sceneManage operation: {operation}");
            }
        }

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

        /// <summary>
        /// Handles GameObject management operations (create, delete, move, rename, duplicate).
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation' type and GameObject path.</param>
        /// <returns>Result dictionary with GameObject hierarchy path and instance ID.</returns>
        /// <exception cref="InvalidOperationException">Thrown when operation or required parameters are invalid.</exception>
        private static object HandleGameObjectManage(Dictionary<string, object> payload)
        {
            var operation = EnsureValue(GetString(payload, "operation"), "operation");
            return operation switch
            {
                "create" => CreateGameObject(payload),
                "delete" => DeleteGameObject(payload),
                "move" => MoveGameObject(payload),
                "rename" => RenameGameObject(payload),
                "duplicate" => DuplicateGameObject(payload),
                "inspect" => InspectGameObject(payload),
                "findMultiple" => FindMultipleGameObjects(payload),
                "deleteMultiple" => DeleteMultipleGameObjects(payload),
                "inspectMultiple" => InspectMultipleGameObjects(payload),
                _ => throw new InvalidOperationException($"Unknown gameObjectManage operation: {operation}"),
            };
        }

        private static object CreateGameObject(Dictionary<string, object> payload)
        {
            var parentPath = GetString(payload, "parentPath");
            var templatePath = GetString(payload, "template");
            GameObject parent = null;
            if (!string.IsNullOrEmpty(parentPath))
            {
                parent = ResolveGameObject(parentPath);
            }

            GameObject instance;
            if (!string.IsNullOrEmpty(templatePath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Prefab not found: {templatePath}");
                }

                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            }
            else
            {
                instance = new GameObject(GetString(payload, "name") ?? "New GameObject");
            }

            if (parent != null)
            {
                instance.transform.SetParent(parent.transform);
            }

            Selection.activeGameObject = instance;

            return new Dictionary<string, object>
            {
                ["path"] = GetHierarchyPath(instance),
                ["id"] = instance.GetInstanceID(),
            };
        }

        private static object DeleteGameObject(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var target = ResolveGameObject(path);
            UnityEngine.Object.DestroyImmediate(target);
            return new Dictionary<string, object>
            {
                ["deleted"] = path,
            };
        }

        private static object MoveGameObject(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var target = ResolveGameObject(path);
            var parentPath = GetString(payload, "parentPath");

            if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = ResolveGameObject(parentPath);
                target.transform.SetParent(parent.transform);
            }
            else
            {
                target.transform.SetParent(null);
            }

            return new Dictionary<string, object>
            {
                ["path"] = GetHierarchyPath(target),
            };
        }

        private static object RenameGameObject(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var newName = EnsureValue(GetString(payload, "name"), "name");
            var target = ResolveGameObject(path);
            target.name = newName;
            return new Dictionary<string, object>
            {
                ["path"] = GetHierarchyPath(target),
                ["name"] = target.name,
            };
        }

        private static object DuplicateGameObject(Dictionary<string, object> payload)
        {
            var sourcePath = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var source = ResolveGameObject(sourcePath);

            var parentPath = GetString(payload, "parentPath");
            GameObject parent = null;
            if (!string.IsNullOrEmpty(parentPath))
            {
                parent = ResolveGameObject(parentPath);
            }

            // Instantiate copy and keep world transform by default.
            var duplicate = UnityEngine.Object.Instantiate(source);

            if (parent != null)
            {
                duplicate.transform.SetParent(parent.transform, worldPositionStays: true);
            }
            else
            {
                duplicate.transform.SetParent(source.transform.parent, worldPositionStays: true);
            }

            var explicitName = GetString(payload, "name");
            if (!string.IsNullOrEmpty(explicitName))
            {
                duplicate.name = explicitName;
            }
            else
            {
                var parentTransform = duplicate.transform.parent;
                duplicate.name = GameObjectUtility.GetUniqueNameForSibling(parentTransform, source.name);
            }

            if (duplicate.transform.parent == source.transform.parent)
            {
                var newIndex = source.transform.GetSiblingIndex() + 1;
                duplicate.transform.SetSiblingIndex(newIndex);
            }

            Selection.activeGameObject = duplicate;

            return new Dictionary<string, object>
            {
                ["path"] = GetHierarchyPath(duplicate),
                ["id"] = duplicate.GetInstanceID(),
            };
        }

        private static object InspectGameObject(Dictionary<string, object> payload)
        {
            var go = ResolveGameObject(EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath"));

            // Check if children should be included (default: true)
            var includeChildren = true;
            if (payload.TryGetValue("includeChildren", out var includeChildrenObj))
            {
                includeChildren = Convert.ToBoolean(includeChildrenObj);
            }

            // Get max depth for child hierarchy (default: 1 = direct children only)
            var maxDepth = GetInt(payload, "maxDepth", defaultValue: 1);

            // Get component type names (not full component details - use componentManage for that)
            var components = go.GetComponents<Component>();
            var componentTypeNames = components
                .Where(c => c != null)
                .Select(c => c.GetType().FullName)
                .ToList();

            var result = new Dictionary<string, object>
            {
                ["gameObjectPath"] = GetHierarchyPath(go),
                ["name"] = go.name,
                ["active"] = go.activeSelf,
                ["activeInHierarchy"] = go.activeInHierarchy,
                ["tag"] = go.tag,
                ["layer"] = go.layer,
                ["layerName"] = LayerMask.LayerToName(go.layer),
                ["static"] = go.isStatic,
                ["componentTypes"] = componentTypeNames,
                ["componentCount"] = componentTypeNames.Count,
                ["childCount"] = go.transform.childCount,
            };

            // Include children if requested
            if (includeChildren && go.transform.childCount > 0)
            {
                var children = CollectChildren(go.transform, maxDepth, currentDepth: 0);
                result["children"] = children;
            }

            return result;
        }

        private static List<Dictionary<string, object>> CollectChildren(Transform parent, int maxDepth, int currentDepth)
        {
            var children = new List<Dictionary<string, object>>();

            if (currentDepth >= maxDepth)
            {
                return children;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var childData = new Dictionary<string, object>
                {
                    ["name"] = child.name,
                    ["path"] = GetHierarchyPath(child.gameObject),
                    ["active"] = child.gameObject.activeSelf,
                    ["tag"] = child.tag,
                    ["layer"] = child.gameObject.layer,
                    ["layerName"] = LayerMask.LayerToName(child.gameObject.layer),
                    ["childCount"] = child.childCount,
                };

                // Recursively collect grandchildren if within depth limit
                if (child.childCount > 0 && currentDepth + 1 < maxDepth)
                {
                    childData["children"] = CollectChildren(child, maxDepth, currentDepth + 1);
                }

                children.Add(childData);
            }

            return children;
        }

        private static object FindMultipleGameObjects(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");
            var maxResults = GetInt(payload, "maxResults", defaultValue: 1000); // Default max 1000 objects

            var gameObjects = McpWildcardUtility.ResolveGameObjects(pattern, useRegex);
            var totalCount = gameObjects.Count;

            // Limit results to prevent timeout
            if (gameObjects.Count > maxResults)
            {
                gameObjects = gameObjects.Take(maxResults).ToList();
            }

            var results = new List<Dictionary<string, object>>();
            foreach (var go in gameObjects)
            {
                results.Add(new Dictionary<string, object>
                {
                    ["path"] = GetHierarchyPath(go),
                    ["name"] = go.name,
                    ["id"] = go.GetInstanceID(),
                    ["active"] = go.activeSelf,
                    ["tag"] = go.tag,
                    ["layer"] = LayerMask.LayerToName(go.layer),
                });
            }

            return new Dictionary<string, object>
            {
                ["pattern"] = pattern,
                ["totalCount"] = totalCount,
                ["returnedCount"] = results.Count,
                ["truncated"] = totalCount > maxResults,
                ["gameObjects"] = results,
            };
        }

        private static object DeleteMultipleGameObjects(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");
            var maxResults = GetInt(payload, "maxResults", defaultValue: 1000); // Default max 1000 objects

            var gameObjects = McpWildcardUtility.ResolveGameObjects(pattern, useRegex);
            var totalCount = gameObjects.Count;

            // Limit results to prevent timeout
            if (gameObjects.Count > maxResults)
            {
                gameObjects = gameObjects.Take(maxResults).ToList();
            }

            var deletedPaths = new List<string>();

            foreach (var go in gameObjects)
            {
                var path = GetHierarchyPath(go);
                UnityEngine.Object.DestroyImmediate(go);
                deletedPaths.Add(path);
            }

            return new Dictionary<string, object>
            {
                ["pattern"] = pattern,
                ["totalCount"] = totalCount,
                ["deletedCount"] = deletedPaths.Count,
                ["truncated"] = totalCount > maxResults,
                ["deleted"] = deletedPaths,
            };
        }

        private static object InspectMultipleGameObjects(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");
            var includeComponents = GetBool(payload, "includeComponents");
            var maxResults = GetInt(payload, "maxResults", defaultValue: 1000); // Default max 1000 objects

            var gameObjects = McpWildcardUtility.ResolveGameObjects(pattern, useRegex);
            var totalCount = gameObjects.Count;

            // Limit results to prevent timeout
            if (gameObjects.Count > maxResults)
            {
                gameObjects = gameObjects.Take(maxResults).ToList();
            }

            var results = new List<Dictionary<string, object>>();
            foreach (var go in gameObjects)
            {
                var goData = new Dictionary<string, object>
                {
                    ["path"] = GetHierarchyPath(go),
                    ["name"] = go.name,
                    ["id"] = go.GetInstanceID(),
                    ["active"] = go.activeSelf,
                    ["tag"] = go.tag,
                    ["layer"] = LayerMask.LayerToName(go.layer),
                };

                if (includeComponents)
                {
                    var components = go.GetComponents<Component>();
                    var componentsList = new List<string>();
                    foreach (var component in components)
                    {
                        if (component != null)
                        {
                            componentsList.Add(component.GetType().FullName);
                        }
                    }
                    goData["components"] = componentsList;
                    goData["componentCount"] = componentsList.Count;
                }

                results.Add(goData);
            }

            return new Dictionary<string, object>
            {
                ["pattern"] = pattern,
                ["totalCount"] = totalCount,
                ["returnedCount"] = results.Count,
                ["truncated"] = totalCount > maxResults,
                ["gameObjects"] = results,
            };
        }

        /// <summary>
        /// Handles component management operations (add, remove, update, inspect).
        /// Uses reflection to set component properties from the payload.
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation', 'gameObjectPath', 'componentType', and optional 'propertyChanges'.</param>
        /// <returns>Result dictionary with component type and GameObject path.</returns>
        /// <exception cref="InvalidOperationException">Thrown when GameObject or component type is not found.</exception>
        private static object HandleComponentManage(Dictionary<string, object> payload)
        {
            var operation = EnsureValue(GetString(payload, "operation"), "operation");
            return operation switch
            {
                "add" => AddComponent(payload),
                "remove" => RemoveComponent(payload),
                "update" => UpdateComponent(payload),
                "inspect" => InspectComponent(payload),
                "addMultiple" => AddMultipleComponents(payload),
                "removeMultiple" => RemoveMultipleComponents(payload),
                "updateMultiple" => UpdateMultipleComponents(payload),
                "inspectMultiple" => InspectMultipleComponents(payload),
                _ => throw new InvalidOperationException($"Unknown componentManage operation: {operation}"),
            };
        }

        private static object AddComponent(Dictionary<string, object> payload)
        {
            var go = ResolveGameObjectFromPayload(payload);
            var type = ResolveType(EnsureValue(GetString(payload, "componentType"), "componentType"));

            var component = go.GetComponent(type);
            if (component == null)
            {
                component = go.AddComponent(type);
                if (component == null)
                {
                    throw new InvalidOperationException($"Failed to add component {type.FullName} to {go.name}");
                }
            }

            EditorUtility.SetDirty(go);
            return DescribeComponent(component);
        }

        private static object RemoveComponent(Dictionary<string, object> payload)
        {
            var go = ResolveGameObjectFromPayload(payload);
            var type = ResolveType(EnsureValue(GetString(payload, "componentType"), "componentType"));
            var component = go.GetComponent(type);
            if (component == null)
            {
                throw new InvalidOperationException($"Component {type.FullName} not found on {go.name}");
            }

            UnityEngine.Object.DestroyImmediate(component, true);
            return new Dictionary<string, object>
            {
                ["removed"] = type.FullName,
            };
        }

        private static object UpdateComponent(Dictionary<string, object> payload)
        {
            var go = ResolveGameObjectFromPayload(payload);
            var type = ResolveType(EnsureValue(GetString(payload, "componentType"), "componentType"));
            var component = go.GetComponent(type);
            if (component == null)
            {
                throw new InvalidOperationException($"Component {type.FullName} not found on {go.name}");
            }

            if (payload.TryGetValue("propertyChanges", out var propertyObj) && propertyObj is Dictionary<string, object> propertyChanges)
            {
                foreach (var kvp in propertyChanges)
                {
                    ApplyProperty(component, kvp.Key, kvp.Value);
                }
            }

            EditorUtility.SetDirty(component);
            return DescribeComponent(component);
        }

        private static object InspectComponent(Dictionary<string, object> payload)
        {
            var go = ResolveGameObjectFromPayload(payload);
            var type = ResolveType(EnsureValue(GetString(payload, "componentType"), "componentType"));
            var component = go.GetComponent(type);
            if (component == null)
            {
                throw new InvalidOperationException($"Component {type.FullName} not found on {go.name}");
            }

            var result = new Dictionary<string, object>
            {
                ["gameObject"] = GetHierarchyPath(go),
                ["type"] = component.GetType().FullName,
            };

            // Check if properties should be included (default: true)
            var includeProperties = true;
            if (payload.TryGetValue("includeProperties", out var includePropObj))
            {
                includeProperties = Convert.ToBoolean(includePropObj);
            }

            // Only include properties if requested
            if (includeProperties)
            {
                var properties = new Dictionary<string, object>();
                var componentType = component.GetType();

                // Get property filter if specified
                HashSet<string> propertyFilter = null;
                if (payload.TryGetValue("propertyFilter", out var filterObj) && filterObj is List<object> filterList)
                {
                    propertyFilter = new HashSet<string>(filterList.Select(f => f.ToString()));
                }

                // Properties that cause memory leaks in edit mode
                var dangerousProperties = new HashSet<string>
                {
                    "mesh",      // Use sharedMesh instead
                    "material",  // Use sharedMaterial instead
                    "materials", // Use sharedMaterials instead
                };

                // Get all public properties
                var propertyInfos = componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in propertyInfos)
                {
                    if (!prop.CanRead)
                    {
                        continue;
                    }

                    // Skip if property filter is specified and this property doesn't match
                    if (propertyFilter != null && !propertyFilter.Contains(prop.Name))
                    {
                        continue;
                    }

                    // Skip dangerous properties that cause memory leaks
                    if (dangerousProperties.Contains(prop.Name))
                    {
                        properties[prop.Name] = $"<Skipped:{prop.Name}:UseSharedVersion>";
                        continue;
                    }

                    try
                    {
                        var value = prop.GetValue(component);
                        properties[prop.Name] = SerializeValue(value);
                    }
                    catch (Exception ex)
                    {
                        properties[prop.Name] = $"<Error: {ex.Message}>";
                    }
                }

                // Get all public fields
                var fieldInfos = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fieldInfos)
                {
                    // Skip if property filter is specified and this field doesn't match
                    if (propertyFilter != null && !propertyFilter.Contains(field.Name))
                    {
                        continue;
                    }

                    try
                    {
                        var value = field.GetValue(component);
                        properties[field.Name] = SerializeValue(value);
                    }
                    catch (Exception ex)
                    {
                        properties[field.Name] = $"<Error: {ex.Message}>";
                    }
                }

                result["properties"] = properties;
            }

            return result;
        }

        private static object AddMultipleComponents(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");
            var componentTypeName = EnsureValue(GetString(payload, "componentType"), "componentType");
            var type = ResolveType(componentTypeName);
            var maxResults = GetInt(payload, "maxResults", defaultValue: 1000);
            var stopOnError = GetBool(payload, "stopOnError");

            var gameObjects = McpWildcardUtility.ResolveGameObjects(pattern, useRegex);
            var totalCount = gameObjects.Count;

            // Limit results to prevent timeout
            if (gameObjects.Count > maxResults)
            {
                gameObjects = gameObjects.Take(maxResults).ToList();
            }

            var results = new List<Dictionary<string, object>>();
            var errors = new List<Dictionary<string, object>>();

            // Get property changes if specified
            var propertyChanges = payload.ContainsKey("propertyChanges") && payload["propertyChanges"] is Dictionary<string, object> changes
                ? changes
                : new Dictionary<string, object>();

            foreach (var go in gameObjects)
            {
                try
                {
                    // Check if component already exists
                    var existingComponent = go.GetComponent(type);
                    if (existingComponent != null)
                    {
                        if (stopOnError)
                        {
                            throw new InvalidOperationException($"Component {type.FullName} already exists on {GetHierarchyPath(go)}");
                        }

                        errors.Add(new Dictionary<string, object>
                        {
                            ["gameObject"] = GetHierarchyPath(go),
                            ["error"] = $"Component {type.FullName} already exists",
                        });
                        continue;
                    }

                    var component = go.AddComponent(type);
                    if (component == null)
                    {
                        throw new InvalidOperationException($"Failed to add component {type.FullName} to {GetHierarchyPath(go)}");
                    }

                    // Apply initial property changes if specified
                    foreach (var kvp in propertyChanges)
                    {
                        ApplyProperty(component, kvp.Key, kvp.Value);
                    }

                    EditorUtility.SetDirty(go);

                    results.Add(new Dictionary<string, object>
                    {
                        ["gameObject"] = GetHierarchyPath(go),
                        ["type"] = type.FullName,
                        ["success"] = true,
                    });
                }
                catch (Exception ex)
                {
                    errors.Add(new Dictionary<string, object>
                    {
                        ["gameObject"] = GetHierarchyPath(go),
                        ["error"] = ex.Message,
                    });

                    if (stopOnError)
                    {
                        break;
                    }
                }
            }

            return new Dictionary<string, object>
            {
                ["pattern"] = pattern,
                ["componentType"] = type.FullName,
                ["totalCount"] = totalCount,
                ["successCount"] = results.Count,
                ["errorCount"] = errors.Count,
                ["truncated"] = totalCount > maxResults,
                ["results"] = results,
                ["errors"] = errors,
            };
        }

        private static object RemoveMultipleComponents(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");
            var componentTypeName = EnsureValue(GetString(payload, "componentType"), "componentType");
            var type = ResolveType(componentTypeName);
            var maxResults = GetInt(payload, "maxResults", defaultValue: 1000);
            var stopOnError = GetBool(payload, "stopOnError");

            var gameObjects = McpWildcardUtility.ResolveGameObjects(pattern, useRegex);
            var totalCount = gameObjects.Count;

            // Limit results to prevent timeout
            if (gameObjects.Count > maxResults)
            {
                gameObjects = gameObjects.Take(maxResults).ToList();
            }

            var results = new List<Dictionary<string, object>>();
            var errors = new List<Dictionary<string, object>>();

            foreach (var go in gameObjects)
            {
                try
                {
                    var component = go.GetComponent(type);
                    if (component != null)
                    {
                        UnityEngine.Object.DestroyImmediate(component);
                        results.Add(new Dictionary<string, object>
                        {
                            ["gameObject"] = GetHierarchyPath(go),
                            ["type"] = type.FullName,
                            ["removed"] = true,
                        });
                    }
                    else
                    {
                        // Component not found - add to errors if stopOnError is true
                        if (stopOnError)
                        {
                            throw new InvalidOperationException($"Component {type.FullName} not found on {GetHierarchyPath(go)}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new Dictionary<string, object>
                    {
                        ["gameObject"] = GetHierarchyPath(go),
                        ["error"] = ex.Message,
                    });

                    if (stopOnError)
                    {
                        break;
                    }
                }
            }

            return new Dictionary<string, object>
            {
                ["pattern"] = pattern,
                ["componentType"] = type.FullName,
                ["totalCount"] = totalCount,
                ["successCount"] = results.Count,
                ["errorCount"] = errors.Count,
                ["truncated"] = totalCount > maxResults,
                ["results"] = results,
                ["errors"] = errors,
            };
        }

        private static object UpdateMultipleComponents(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");
            var componentTypeName = EnsureValue(GetString(payload, "componentType"), "componentType");
            var type = ResolveType(componentTypeName);
            var maxResults = GetInt(payload, "maxResults", defaultValue: 1000);
            var stopOnError = GetBool(payload, "stopOnError");
            var propertyChanges = payload.ContainsKey("propertyChanges") && payload["propertyChanges"] is Dictionary<string, object> changes
                ? changes
                : new Dictionary<string, object>();

            var gameObjects = McpWildcardUtility.ResolveGameObjects(pattern, useRegex);
            var totalCount = gameObjects.Count;

            // Limit results to prevent timeout
            if (gameObjects.Count > maxResults)
            {
                gameObjects = gameObjects.Take(maxResults).ToList();
            }

            var results = new List<Dictionary<string, object>>();
            var errors = new List<Dictionary<string, object>>();

            foreach (var go in gameObjects)
            {
                try
                {
                    var component = go.GetComponent(type);
                    if (component != null)
                    {
                        // Apply each property change using existing ApplyProperty method
                        foreach (var kvp in propertyChanges)
                        {
                            ApplyProperty(component, kvp.Key, kvp.Value);
                        }

                        EditorUtility.SetDirty(component);

                        results.Add(new Dictionary<string, object>
                        {
                            ["gameObject"] = GetHierarchyPath(go),
                            ["type"] = type.FullName,
                            ["updated"] = true,
                        });
                    }
                    else
                    {
                        // Component not found
                        if (stopOnError)
                        {
                            throw new InvalidOperationException($"Component {type.FullName} not found on {GetHierarchyPath(go)}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new Dictionary<string, object>
                    {
                        ["gameObject"] = GetHierarchyPath(go),
                        ["error"] = ex.Message,
                    });

                    if (stopOnError)
                    {
                        break;
                    }
                }
            }

            return new Dictionary<string, object>
            {
                ["pattern"] = pattern,
                ["componentType"] = type.FullName,
                ["totalCount"] = totalCount,
                ["successCount"] = results.Count,
                ["errorCount"] = errors.Count,
                ["truncated"] = totalCount > maxResults,
                ["results"] = results,
                ["errors"] = errors,
            };
        }

        private static object InspectMultipleComponents(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");
            var componentTypeName = EnsureValue(GetString(payload, "componentType"), "componentType");
            var type = ResolveType(componentTypeName);
            var maxResults = GetInt(payload, "maxResults", defaultValue: 1000);

            // Check if properties should be included (default: true)
            var includeProperties = true;
            if (payload.TryGetValue("includeProperties", out var includePropObj))
            {
                includeProperties = Convert.ToBoolean(includePropObj);
            }

            // Get property filter if specified
            HashSet<string> propertyFilter = null;
            if (payload.TryGetValue("propertyFilter", out var filterObj) && filterObj is List<object> filterList)
            {
                propertyFilter = new HashSet<string>(filterList.Select(f => f.ToString()));
            }

            var gameObjects = McpWildcardUtility.ResolveGameObjects(pattern, useRegex);
            var totalCount = gameObjects.Count;

            // Limit results to prevent timeout
            if (gameObjects.Count > maxResults)
            {
                gameObjects = gameObjects.Take(maxResults).ToList();
            }

            var results = new List<Dictionary<string, object>>();

            // Properties that cause memory leaks in edit mode
            var dangerousProperties = new HashSet<string>
            {
                "mesh",      // Use sharedMesh instead
                "material",  // Use sharedMaterial instead
                "materials", // Use sharedMaterials instead
            };

            foreach (var go in gameObjects)
            {
                var component = go.GetComponent(type);
                if (component != null)
                {
                    var resultData = new Dictionary<string, object>
                    {
                        ["gameObject"] = GetHierarchyPath(go),
                        ["type"] = component.GetType().FullName,
                    };

                    // Only include properties if requested
                    if (includeProperties)
                    {
                        var properties = new Dictionary<string, object>();
                        var componentType = component.GetType();

                        // Get all public properties
                        var propertyInfos = componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var prop in propertyInfos)
                        {
                            if (!prop.CanRead)
                            {
                                continue;
                            }

                            // Skip if property filter is specified and this property doesn't match
                            if (propertyFilter != null && !propertyFilter.Contains(prop.Name))
                            {
                                continue;
                            }

                            // Skip dangerous properties that cause memory leaks
                            if (dangerousProperties.Contains(prop.Name))
                            {
                                properties[prop.Name] = $"<Skipped:{prop.Name}:UseSharedVersion>";
                                continue;
                            }

                            try
                            {
                                var value = prop.GetValue(component);
                                properties[prop.Name] = SerializeValue(value);
                            }
                            catch (Exception ex)
                            {
                                properties[prop.Name] = $"<Error: {ex.Message}>";
                            }
                        }

                        // Get all public fields
                        var fieldInfos = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var field in fieldInfos)
                        {
                            // Skip if property filter is specified and this field doesn't match
                            if (propertyFilter != null && !propertyFilter.Contains(field.Name))
                            {
                                continue;
                            }

                            try
                            {
                                var value = field.GetValue(component);
                                properties[field.Name] = SerializeValue(value);
                            }
                            catch (Exception ex)
                            {
                                properties[field.Name] = $"<Error: {ex.Message}>";
                            }
                        }

                        resultData["properties"] = properties;
                    }

                    results.Add(resultData);
                }
            }

            return new Dictionary<string, object>
            {
                ["pattern"] = pattern,
                ["componentType"] = type.FullName,
                ["totalCount"] = totalCount,
                ["returnedCount"] = results.Count,
                ["truncated"] = totalCount > maxResults,
                ["results"] = results,
            };
        }

        /// <summary>
        /// Handles asset management operations (updateImporter, delete, rename, duplicate, inspect).
        /// Note: This does NOT handle file content creation/modification - use Claude Code's file tools for that.
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation' and asset-specific settings.</param>
        /// <returns>Result dictionary with asset information.</returns>
        private static object HandleAssetManage(Dictionary<string, object> payload)
        {
            var operation = EnsureValue(GetString(payload, "operation"), "operation");
            return operation switch
            {
                "updateImporter" => UpdateAssetImporter(payload),
                "delete" => DeleteAsset(payload),
                "rename" => RenameAsset(payload),
                "duplicate" => DuplicateAsset(payload),
                "inspect" => InspectAsset(payload),
                "findMultiple" => FindMultipleAssets(payload),
                "deleteMultiple" => DeleteMultipleAssets(payload),
                "inspectMultiple" => InspectMultipleAssets(payload),
                _ => throw new InvalidOperationException($"Unknown assetManage operation: {operation}"),
            };
        }

        private static object UpdateAssetImporter(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "assetPath"), "assetPath");
            var assetTypeName = EnsureValue(GetString(payload, "assetType"), "assetType");

            EnsureDirectoryExists(path);

            var assetType = ResolveType(assetTypeName);

            // Check if it's a ScriptableObject
            if (assetType.IsSubclassOf(typeof(ScriptableObject)))
            {
                var instance = ScriptableObject.CreateInstance(assetType);

                // Apply property changes if provided
                if (payload.TryGetValue("propertyChanges", out var propertyObj) && propertyObj is Dictionary<string, object> propertyChanges)
                {
                    foreach (var kvp in propertyChanges)
                    {
                        ApplyProperty(instance, kvp.Key, kvp.Value);
                    }
                }

                AssetDatabase.CreateAsset(instance, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return DescribeAsset(path);
            }
            else
            {
                throw new InvalidOperationException($"Asset type {assetTypeName} is not a ScriptableObject. Only ScriptableObject creation is supported. Use Edit/Write tools for script files.");
            }
        }

        private static object UpdateAsset(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "assetPath"), "assetPath");

            // Load the asset
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Asset not found or cannot be loaded: {path}");
            }

            // Apply property changes
            if (payload.TryGetValue("propertyChanges", out var propertyObj) && propertyObj is Dictionary<string, object> propertyChanges)
            {
                foreach (var kvp in propertyChanges)
                {
                    ApplyProperty(asset, kvp.Key, kvp.Value);
                }
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return DescribeAsset(path);
        }

        private static object DeleteAsset(Dictionary<string, object> payload)
        {
            var path = ResolveAssetPathFromPayload(payload);
            if (!AssetDatabase.DeleteAsset(path))
            {
                throw new InvalidOperationException($"Failed to delete asset: {path}");
            }

            AssetDatabase.Refresh();
            return new Dictionary<string, object>
            {
                ["deleted"] = path,
            };
        }

        private static object RenameAsset(Dictionary<string, object> payload)
        {
            var path = ResolveAssetPathFromPayload(payload);
            var destination = EnsureValue(GetString(payload, "destinationPath"), "destinationPath");
            var result = AssetDatabase.MoveAsset(path, destination);
            if (!string.IsNullOrEmpty(result))
            {
                throw new InvalidOperationException(result);
            }

            AssetDatabase.Refresh();
            return DescribeAsset(destination);
        }

        private static object DuplicateAsset(Dictionary<string, object> payload)
        {
            var path = ResolveAssetPathFromPayload(payload);
            var destination = EnsureValue(GetString(payload, "destinationPath"), "destinationPath");
            EnsureDirectoryExists(destination);
            if (!AssetDatabase.CopyAsset(path, destination))
            {
                throw new InvalidOperationException($"Failed to duplicate asset {path}");
            }

            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
            return DescribeAsset(destination);
        }

        private static object InspectAsset(Dictionary<string, object> payload)
        {
            var path = ResolveAssetPathFromPayload(payload);
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                throw new InvalidOperationException($"Asset not found: {path}");
            }

            var includeProperties = GetBool(payload, "includeProperties");
            var guid = AssetDatabase.AssetPathToGUID(path);
            var mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(path);
            var assetObj = AssetDatabase.LoadMainAssetAtPath(path);

            var result = new Dictionary<string, object>
            {
                ["path"] = path,
                ["guid"] = guid,
                ["type"] = mainAssetType?.FullName,
                ["exists"] = assetObj != null,
            };

            if (assetObj != null && includeProperties)
            {
                var properties = new Dictionary<string, object>();
                var assetType = assetObj.GetType();

                // Get all public properties
                var propertyInfos = assetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in propertyInfos)
                {
                    if (!prop.CanRead)
                    {
                        continue;
                    }

                    try
                    {
                        var value = prop.GetValue(assetObj);
                        properties[prop.Name] = SerializeValue(value);
                    }
                    catch (Exception ex)
                    {
                        properties[prop.Name] = $"<Error: {ex.Message}>";
                    }
                }

                // Get all public fields
                var fieldInfos = assetType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fieldInfos)
                {
                    try
                    {
                        var value = field.GetValue(assetObj);
                        properties[field.Name] = SerializeValue(value);
                    }
                    catch (Exception ex)
                    {
                        properties[field.Name] = $"<Error: {ex.Message}>";
                    }
                }

                result["properties"] = properties;
            }

            // Include AssetImporter properties if requested
            if (includeProperties)
            {
                var importer = AssetImporter.GetAtPath(path);
                if (importer != null)
                {
                    var importerProperties = new Dictionary<string, object>();
                    var importerType = importer.GetType();

                    // Get all public properties
                    var propertyInfos = importerType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in propertyInfos)
                    {
                        if (!prop.CanRead)
                        {
                            continue;
                        }

                        try
                        {
                            var value = prop.GetValue(importer);
                            importerProperties[prop.Name] = SerializeValue(value);
                        }
                        catch (Exception ex)
                        {
                            importerProperties[prop.Name] = $"<Error: {ex.Message}>";
                        }
                    }

                    result["importerType"] = importerType.FullName;
                    result["importerProperties"] = importerProperties;
                }
            }

            return result;
        }

        private static object FindMultipleAssets(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");

            var assetPaths = McpWildcardUtility.ResolveAssetPaths(pattern, useRegex);

            var results = new List<Dictionary<string, object>>();
            foreach (var path in assetPaths)
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                var mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(path);

                results.Add(new Dictionary<string, object>
                {
                    ["path"] = path,
                    ["guid"] = guid,
                    ["type"] = mainAssetType?.FullName,
                });
            }

            return new Dictionary<string, object>
            {
                ["pattern"] = pattern,
                ["count"] = results.Count,
                ["assets"] = results,
            };
        }

        private static object DeleteMultipleAssets(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");

            var assetPaths = McpWildcardUtility.ResolveAssetPaths(pattern, useRegex);
            var deletedPaths = new List<string>();

            foreach (var path in assetPaths)
            {
                if (AssetDatabase.DeleteAsset(path))
                {
                    deletedPaths.Add(path);
                }
            }

            AssetDatabase.Refresh();

            return new Dictionary<string, object>
            {
                ["pattern"] = pattern,
                ["count"] = deletedPaths.Count,
                ["deleted"] = deletedPaths,
            };
        }

        private static object InspectMultipleAssets(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");
            var includeProperties = GetBool(payload, "includeProperties");

            var assetPaths = McpWildcardUtility.ResolveAssetPaths(pattern, useRegex);

            var results = new List<Dictionary<string, object>>();
            foreach (var path in assetPaths)
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                var mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                var assetObj = AssetDatabase.LoadMainAssetAtPath(path);

                var assetData = new Dictionary<string, object>
                {
                    ["path"] = path,
                    ["guid"] = guid,
                    ["type"] = mainAssetType?.FullName,
                    ["exists"] = assetObj != null,
                };

                if (includeProperties && assetObj != null)
                {
                    var properties = new Dictionary<string, object>();
                    var assetType = assetObj.GetType();

                    // Get all public properties
                    var propertyInfos = assetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in propertyInfos)
                    {
                        if (!prop.CanRead)
                        {
                            continue;
                        }

                        try
                        {
                            var value = prop.GetValue(assetObj);
                            properties[prop.Name] = SerializeValue(value);
                        }
                        catch (Exception ex)
                        {
                            properties[prop.Name] = $"<Error: {ex.Message}>";
                        }
                    }

                    // Get all public fields
                    var fieldInfos = assetType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var field in fieldInfos)
                    {
                        try
                        {
                            var value = field.GetValue(assetObj);
                            properties[field.Name] = SerializeValue(value);
                        }
                        catch (Exception ex)
                        {
                            properties[field.Name] = $"<Error: {ex.Message}>";
                        }
                    }

                    assetData["properties"] = properties;
                }

                results.Add(assetData);
            }

            return new Dictionary<string, object>
            {
                ["pattern"] = pattern,
                ["count"] = results.Count,
                ["assets"] = results,
            };
        }

        private static object HandleUguiRectAdjust(Dictionary<string, object> payload)
        {
            try
            {
                var path = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
                Debug.Log($"[uguiRectAdjust] Processing: {path}");

                var target = ResolveGameObject(path);
                Debug.Log($"[uguiRectAdjust] GameObject resolved: {target.name}");

                var rectTransform = target.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    throw new InvalidOperationException("Target does not contain a RectTransform");
                }
                Debug.Log($"[uguiRectAdjust] RectTransform found");

                var canvas = rectTransform.GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    throw new InvalidOperationException("Target is not under a Canvas");
                }
                Debug.Log($"[uguiRectAdjust] Canvas found: {canvas.name}");

                var worldCorners = new Vector3[4];
                rectTransform.GetWorldCorners(worldCorners);
                Debug.Log($"[uguiRectAdjust] Got world corners");

                var width = Vector3.Distance(worldCorners[3], worldCorners[0]);
                var height = Vector3.Distance(worldCorners[1], worldCorners[0]);
                var scaleFactor = canvas.scaleFactor == 0f ? 1f : canvas.scaleFactor;
                var pixelWidth = width / scaleFactor;
                var pixelHeight = height / scaleFactor;
                Debug.Log($"[uguiRectAdjust] Calculated dimensions: {pixelWidth}x{pixelHeight}, scaleFactor: {scaleFactor}");

                var beforeAnchoredPosition = rectTransform.anchoredPosition;
                var beforeSizeDelta = rectTransform.sizeDelta;

                var before = new Dictionary<string, object>
                {
                    ["anchoredPosition"] = new Dictionary<string, object>
                    {
                        ["x"] = beforeAnchoredPosition.x,
                        ["y"] = beforeAnchoredPosition.y,
                    },
                    ["sizeDelta"] = new Dictionary<string, object>
                    {
                        ["x"] = beforeSizeDelta.x,
                        ["y"] = beforeSizeDelta.y,
                    },
                };

                rectTransform.sizeDelta = new Vector2(pixelWidth, pixelHeight);
                var afterAnchoredPosition = rectTransform.anchoredPosition;
                var afterSizeDelta = rectTransform.sizeDelta;

                EditorUtility.SetDirty(rectTransform);
                Debug.Log($"[uguiRectAdjust] Completed successfully");

                return new Dictionary<string, object>
                {
                    ["before"] = before,
                    ["after"] = new Dictionary<string, object>
                    {
                        ["anchoredPosition"] = new Dictionary<string, object>
                        {
                            ["x"] = afterAnchoredPosition.x,
                            ["y"] = afterAnchoredPosition.y,
                        },
                        ["sizeDelta"] = new Dictionary<string, object>
                        {
                            ["x"] = afterSizeDelta.x,
                            ["y"] = afterSizeDelta.y,
                        },
                    },
                    ["scaleFactor"] = scaleFactor,
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[uguiRectAdjust] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Handles RectTransform anchor manipulation operations.
        /// Supports setting anchors, converting between anchor-based and absolute positions,
        /// and adjusting positioning based on anchor changes.
        /// </summary>
        /// <param name="payload">Operation parameters including gameObjectPath and anchor settings.</param>
        /// <returns>Result dictionary with before/after anchor and position data.</returns>
        private static object HandleUguiAnchorManage(Dictionary<string, object> payload)
        {
            try
            {
                var path = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
                Debug.Log($"[uguiAnchorManage] Processing: {path}");

                var target = ResolveGameObject(path);
                var rectTransform = target.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    throw new InvalidOperationException("Target does not contain a RectTransform");
                }

                var canvas = rectTransform.GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    throw new InvalidOperationException("Target is not under a Canvas");
                }

                // Capture before state
                var beforeState = CaptureRectTransformState(rectTransform);

                // Get operation type
                var operation = GetString(payload, "operation");
                if (string.IsNullOrEmpty(operation))
                {
                    throw new InvalidOperationException("operation is required");
                }

                switch (operation)
                {
                    case "setAnchor":
                        SetAnchor(rectTransform, payload);
                        break;
                    case "setAnchorPreset":
                        SetAnchorPreset(rectTransform, payload);
                        break;
                    case "convertToAnchored":
                        ConvertToAnchoredPosition(rectTransform, payload);
                        break;
                    case "convertToAbsolute":
                        ConvertToAbsolutePosition(rectTransform, payload);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown uguiAnchorManage operation: {operation}");
                }

                EditorUtility.SetDirty(rectTransform);
                Debug.Log($"[uguiAnchorManage] Completed successfully");

                // Capture after state
                var afterState = CaptureRectTransformState(rectTransform);

                return new Dictionary<string, object>
                {
                    ["before"] = beforeState,
                    ["after"] = afterState,
                    ["operation"] = operation,
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[uguiAnchorManage] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Captures the current state of a RectTransform including anchors, positions, and size.
        /// </summary>
        private static Dictionary<string, object> CaptureRectTransformState(RectTransform rectTransform)
        {
            return new Dictionary<string, object>
            {
                ["anchorMin"] = new Dictionary<string, object>
                {
                    ["x"] = rectTransform.anchorMin.x,
                    ["y"] = rectTransform.anchorMin.y,
                },
                ["anchorMax"] = new Dictionary<string, object>
                {
                    ["x"] = rectTransform.anchorMax.x,
                    ["y"] = rectTransform.anchorMax.y,
                },
                ["anchoredPosition"] = new Dictionary<string, object>
                {
                    ["x"] = rectTransform.anchoredPosition.x,
                    ["y"] = rectTransform.anchoredPosition.y,
                },
                ["sizeDelta"] = new Dictionary<string, object>
                {
                    ["x"] = rectTransform.sizeDelta.x,
                    ["y"] = rectTransform.sizeDelta.y,
                },
                ["pivot"] = new Dictionary<string, object>
                {
                    ["x"] = rectTransform.pivot.x,
                    ["y"] = rectTransform.pivot.y,
                },
                ["offsetMin"] = new Dictionary<string, object>
                {
                    ["x"] = rectTransform.offsetMin.x,
                    ["y"] = rectTransform.offsetMin.y,
                },
                ["offsetMax"] = new Dictionary<string, object>
                {
                    ["x"] = rectTransform.offsetMax.x,
                    ["y"] = rectTransform.offsetMax.y,
                },
            };
        }

        /// <summary>
        /// Sets custom anchor values while preserving the visual position.
        /// </summary>
        private static void SetAnchor(RectTransform rectTransform, Dictionary<string, object> payload)
        {
            var preservePosition = GetBool(payload, "preservePosition", true);

            // Store current position in parent space if we need to preserve it
            Vector2 oldPos = Vector2.zero;
            if (preservePosition)
            {
                oldPos = rectTransform.anchoredPosition;
            }

            // Get anchor values
            var anchorMinX = GetFloat(payload, "anchorMinX");
            var anchorMinY = GetFloat(payload, "anchorMinY");
            var anchorMaxX = GetFloat(payload, "anchorMaxX");
            var anchorMaxY = GetFloat(payload, "anchorMaxY");

            if (anchorMinX.HasValue && anchorMinY.HasValue)
            {
                rectTransform.anchorMin = new Vector2(anchorMinX.Value, anchorMinY.Value);
            }
            if (anchorMaxX.HasValue && anchorMaxY.HasValue)
            {
                rectTransform.anchorMax = new Vector2(anchorMaxX.Value, anchorMaxY.Value);
            }

            // Restore position if needed
            if (preservePosition)
            {
                rectTransform.anchoredPosition = oldPos;
            }
        }

        /// <summary>
        /// Sets anchor using common presets (e.g., top-left, center, stretch).
        /// </summary>
        private static void SetAnchorPreset(RectTransform rectTransform, Dictionary<string, object> payload)
        {
            var preset = GetString(payload, "preset");
            var preservePosition = GetBool(payload, "preservePosition", true);

            if (string.IsNullOrEmpty(preset))
            {
                throw new InvalidOperationException("preset is required");
            }

            // Store current corners if we need to preserve position
            Vector3[] corners = new Vector3[4];
            if (preservePosition)
            {
                rectTransform.GetWorldCorners(corners);
            }

            // Set anchor based on preset
            switch (preset.ToLower())
            {
                case "top-left":
                    rectTransform.anchorMin = new Vector2(0, 1);
                    rectTransform.anchorMax = new Vector2(0, 1);
                    break;
                case "top-center":
                    rectTransform.anchorMin = new Vector2(0.5f, 1);
                    rectTransform.anchorMax = new Vector2(0.5f, 1);
                    break;
                case "top-right":
                    rectTransform.anchorMin = new Vector2(1, 1);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    break;
                case "middle-left":
                    rectTransform.anchorMin = new Vector2(0, 0.5f);
                    rectTransform.anchorMax = new Vector2(0, 0.5f);
                    break;
                case "middle-center":
                case "center":
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    break;
                case "middle-right":
                    rectTransform.anchorMin = new Vector2(1, 0.5f);
                    rectTransform.anchorMax = new Vector2(1, 0.5f);
                    break;
                case "bottom-left":
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(0, 0);
                    break;
                case "bottom-center":
                    rectTransform.anchorMin = new Vector2(0.5f, 0);
                    rectTransform.anchorMax = new Vector2(0.5f, 0);
                    break;
                case "bottom-right":
                    rectTransform.anchorMin = new Vector2(1, 0);
                    rectTransform.anchorMax = new Vector2(1, 0);
                    break;
                case "stretch-horizontal":
                    rectTransform.anchorMin = new Vector2(0, 0.5f);
                    rectTransform.anchorMax = new Vector2(1, 0.5f);
                    break;
                case "stretch-vertical":
                    rectTransform.anchorMin = new Vector2(0.5f, 0);
                    rectTransform.anchorMax = new Vector2(0.5f, 1);
                    break;
                case "stretch-all":
                case "stretch":
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    break;
                case "stretch-top":
                    rectTransform.anchorMin = new Vector2(0, 1);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    break;
                case "stretch-middle":
                    rectTransform.anchorMin = new Vector2(0, 0.5f);
                    rectTransform.anchorMax = new Vector2(1, 0.5f);
                    break;
                case "stretch-bottom":
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(1, 0);
                    break;
                case "stretch-left":
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(0, 1);
                    break;
                case "stretch-center-vertical":
                    rectTransform.anchorMin = new Vector2(0.5f, 0);
                    rectTransform.anchorMax = new Vector2(0.5f, 1);
                    break;
                case "stretch-right":
                    rectTransform.anchorMin = new Vector2(1, 0);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown anchor preset: {preset}");
            }

            // Restore position if needed by adjusting offsetMin and offsetMax
            if (preservePosition)
            {
                Vector3[] newCorners = new Vector3[4];
                rectTransform.GetWorldCorners(newCorners);

                // Calculate the difference and adjust
                var parentRect = rectTransform.parent.GetComponent<RectTransform>();
                if (parentRect != null)
                {
                    // Convert world corners to local and adjust offsets
                    Vector2 localCorner0 = parentRect.InverseTransformPoint(corners[0]);
                    Vector2 localCorner2 = parentRect.InverseTransformPoint(corners[2]);

                    Vector2 parentSize = parentRect.rect.size;
                    Vector2 anchorMin = rectTransform.anchorMin;
                    Vector2 anchorMax = rectTransform.anchorMax;

                    Vector2 offsetMin = localCorner0 - new Vector2(anchorMin.x * parentSize.x, anchorMin.y * parentSize.y);
                    Vector2 offsetMax = localCorner2 - new Vector2(anchorMax.x * parentSize.x, anchorMax.y * parentSize.y);

                    rectTransform.offsetMin = offsetMin;
                    rectTransform.offsetMax = offsetMax;
                }
            }
        }

        /// <summary>
        /// Converts absolute position values to anchored position based on current anchors.
        /// </summary>
        private static void ConvertToAnchoredPosition(RectTransform rectTransform, Dictionary<string, object> payload)
        {
            var absoluteX = GetFloat(payload, "absoluteX");
            var absoluteY = GetFloat(payload, "absoluteY");

            var parentRect = rectTransform.parent.GetComponent<RectTransform>();
            if (parentRect == null)
            {
                throw new InvalidOperationException("Parent does not have a RectTransform");
            }

            var parentSize = parentRect.rect.size;
            var anchorMin = rectTransform.anchorMin;
            var anchorMax = rectTransform.anchorMax;
            var pivot = rectTransform.pivot;

            // Calculate anchor center in parent space
            var anchorCenter = new Vector2(
                (anchorMin.x + anchorMax.x) * 0.5f * parentSize.x,
                (anchorMin.y + anchorMax.y) * 0.5f * parentSize.y
            );

            // Calculate pivot offset
            var size = rectTransform.rect.size;
            var pivotOffset = new Vector2(
                (pivot.x - 0.5f) * size.x,
                (pivot.y - 0.5f) * size.y
            );

            // Convert absolute to anchored
            if (absoluteX.HasValue)
            {
                rectTransform.anchoredPosition = new Vector2(
                    absoluteX.Value - anchorCenter.x + pivotOffset.x,
                    rectTransform.anchoredPosition.y
                );
            }
            if (absoluteY.HasValue)
            {
                rectTransform.anchoredPosition = new Vector2(
                    rectTransform.anchoredPosition.x,
                    absoluteY.Value - anchorCenter.y + pivotOffset.y
                );
            }
        }

        /// <summary>
        /// Converts anchored position to absolute position in parent space.
        /// This is a read-only operation that returns the absolute position.
        /// </summary>
        private static void ConvertToAbsolutePosition(RectTransform _1, Dictionary<string, object> _2)
        {
            // This operation doesn't modify the transform, it just calculates values
            // The result will be returned in the "after" state which includes calculated absolute positions

            // Note: The actual absolute position calculation is implicit in Unity's RectTransform system
            // We just need to ensure the state is captured correctly
        }

        /// <summary>
        /// Unified UGUI management handler that consolidates all UGUI operations.
        /// Supports operations: rectAdjust, setAnchor, setAnchorPreset, convertToAnchored,
        /// convertToAbsolute, inspect, updateRect.
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation' type and target GameObject.</param>
        /// <returns>Result dictionary with operation-specific data.</returns>
        private static object HandleUguiManage(Dictionary<string, object> payload)
        {
            try
            {
                var operation = GetString(payload, "operation");
                if (string.IsNullOrEmpty(operation))
                {
                    throw new InvalidOperationException("operation is required");
                }

                var path = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
                Debug.Log($"[uguiManage] Processing operation '{operation}' on: {path}");

                var target = ResolveGameObject(path);
                var rectTransform = target.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    throw new InvalidOperationException("Target does not contain a RectTransform");
                }

                var canvas = rectTransform.GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    throw new InvalidOperationException("Target is not under a Canvas");
                }

                // Capture before state
                var beforeState = CaptureRectTransformState(rectTransform);

                object result;
                switch (operation)
                {
                    case "rectAdjust":
                        result = ExecuteRectAdjust(rectTransform, canvas, payload, beforeState);
                        break;
                    case "setAnchor":
                        SetAnchor(rectTransform, payload);
                        result = CreateStandardResult(beforeState, rectTransform, operation);
                        break;
                    case "setAnchorPreset":
                        SetAnchorPreset(rectTransform, payload);
                        result = CreateStandardResult(beforeState, rectTransform, operation);
                        break;
                    case "convertToAnchored":
                        ConvertToAnchoredPosition(rectTransform, payload);
                        result = CreateStandardResult(beforeState, rectTransform, operation);
                        break;
                    case "convertToAbsolute":
                        ConvertToAbsolutePosition(rectTransform, payload);
                        result = CreateStandardResult(beforeState, rectTransform, operation);
                        break;
                    case "inspect":
                        result = ExecuteInspect(rectTransform, canvas);
                        break;
                    case "updateRect":
                        ExecuteUpdateRect(rectTransform, payload);
                        result = CreateStandardResult(beforeState, rectTransform, operation);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown uguiManage operation: {operation}");
                }

                EditorUtility.SetDirty(rectTransform);
                Debug.Log($"[uguiManage] Completed successfully");

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[uguiManage] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Executes RectTransform size adjustment based on world corners.
        /// </summary>
        private static object ExecuteRectAdjust(RectTransform rectTransform, Canvas canvas,
            Dictionary<string, object> payload, Dictionary<string, object> beforeState)
        {
            var worldCorners = new Vector3[4];
            rectTransform.GetWorldCorners(worldCorners);

            var width = Vector3.Distance(worldCorners[3], worldCorners[0]);
            var height = Vector3.Distance(worldCorners[1], worldCorners[0]);
            var scaleFactor = canvas.scaleFactor == 0f ? 1f : canvas.scaleFactor;
            var pixelWidth = width / scaleFactor;
            var pixelHeight = height / scaleFactor;

            rectTransform.sizeDelta = new Vector2(pixelWidth, pixelHeight);

            return new Dictionary<string, object>
            {
                ["before"] = beforeState,
                ["after"] = CaptureRectTransformState(rectTransform),
                ["operation"] = "rectAdjust",
                ["scaleFactor"] = scaleFactor,
            };
        }

        /// <summary>
        /// Inspects current RectTransform state with detailed information.
        /// </summary>
        private static object ExecuteInspect(RectTransform rectTransform, Canvas canvas)
        {
            var state = CaptureRectTransformState(rectTransform);
            state["canvasName"] = canvas.name;
            state["scaleFactor"] = canvas.scaleFactor;

            // Add calculated world corners
            var worldCorners = new Vector3[4];
            rectTransform.GetWorldCorners(worldCorners);
            state["worldCorners"] = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["x"] = worldCorners[0].x, ["y"] = worldCorners[0].y, ["z"] = worldCorners[0].z },
                new Dictionary<string, object> { ["x"] = worldCorners[1].x, ["y"] = worldCorners[1].y, ["z"] = worldCorners[1].z },
                new Dictionary<string, object> { ["x"] = worldCorners[2].x, ["y"] = worldCorners[2].y, ["z"] = worldCorners[2].z },
                new Dictionary<string, object> { ["x"] = worldCorners[3].x, ["y"] = worldCorners[3].y, ["z"] = worldCorners[3].z },
            };

            // Add rect dimensions
            state["rectWidth"] = rectTransform.rect.width;
            state["rectHeight"] = rectTransform.rect.height;

            return new Dictionary<string, object>
            {
                ["state"] = state,
                ["operation"] = "inspect",
            };
        }

        /// <summary>
        /// Updates RectTransform properties from payload.
        /// </summary>
        private static void ExecuteUpdateRect(RectTransform rectTransform, Dictionary<string, object> payload)
        {
            // Update anchoredPosition - supports both dictionary format and individual fields
            if (payload.TryGetValue("anchoredPosition", out var anchoredPosObj) && anchoredPosObj is Dictionary<string, object> anchoredPosDict)
            {
                // Dictionary format: {"anchoredPosition": {"x": 100, "y": 200}}
                var pos = rectTransform.anchoredPosition;
                rectTransform.anchoredPosition = new Vector2(
                    GetFloat(anchoredPosDict, "x") ?? pos.x,
                    GetFloat(anchoredPosDict, "y") ?? pos.y
                );
            }
            else
            {
                // Individual fields format: {"anchoredPositionX": 100, "anchoredPositionY": 200}
                var anchoredPositionX = GetFloat(payload, "anchoredPositionX");
                var anchoredPositionY = GetFloat(payload, "anchoredPositionY");
                if (anchoredPositionX.HasValue || anchoredPositionY.HasValue)
                {
                    var pos = rectTransform.anchoredPosition;
                    rectTransform.anchoredPosition = new Vector2(
                        anchoredPositionX ?? pos.x,
                        anchoredPositionY ?? pos.y
                    );
                }
            }

            // Update sizeDelta - supports both dictionary format and individual fields
            if (payload.TryGetValue("sizeDelta", out var sizeDeltaObj) && sizeDeltaObj is Dictionary<string, object> sizeDeltaDict)
            {
                // Dictionary format: {"sizeDelta": {"x": 300, "y": 400}}
                var size = rectTransform.sizeDelta;
                rectTransform.sizeDelta = new Vector2(
                    GetFloat(sizeDeltaDict, "x") ?? size.x,
                    GetFloat(sizeDeltaDict, "y") ?? size.y
                );
            }
            else
            {
                // Individual fields format: {"sizeDeltaX": 300, "sizeDeltaY": 400}
                var sizeDeltaX = GetFloat(payload, "sizeDeltaX");
                var sizeDeltaY = GetFloat(payload, "sizeDeltaY");
                if (sizeDeltaX.HasValue || sizeDeltaY.HasValue)
                {
                    var size = rectTransform.sizeDelta;
                    rectTransform.sizeDelta = new Vector2(
                        sizeDeltaX ?? size.x,
                        sizeDeltaY ?? size.y
                    );
                }
            }

            // Update pivot - supports both dictionary format and individual fields
            if (payload.TryGetValue("pivot", out var pivotObj) && pivotObj is Dictionary<string, object> pivotDict)
            {
                // Dictionary format: {"pivot": {"x": 0.5, "y": 0.5}}
                var pivot = rectTransform.pivot;
                rectTransform.pivot = new Vector2(
                    GetFloat(pivotDict, "x") ?? pivot.x,
                    GetFloat(pivotDict, "y") ?? pivot.y
                );
            }
            else
            {
                // Individual fields format: {"pivotX": 0.5, "pivotY": 0.5}
                var pivotX = GetFloat(payload, "pivotX");
                var pivotY = GetFloat(payload, "pivotY");
                if (pivotX.HasValue || pivotY.HasValue)
                {
                    var pivot = rectTransform.pivot;
                    rectTransform.pivot = new Vector2(
                        pivotX ?? pivot.x,
                        pivotY ?? pivot.y
                    );
                }
            }

            // Update offsetMin - supports both dictionary format and individual fields
            if (payload.TryGetValue("offsetMin", out var offsetMinObj) && offsetMinObj is Dictionary<string, object> offsetMinDict)
            {
                // Dictionary format: {"offsetMin": {"x": 10, "y": 10}}
                var offset = rectTransform.offsetMin;
                rectTransform.offsetMin = new Vector2(
                    GetFloat(offsetMinDict, "x") ?? offset.x,
                    GetFloat(offsetMinDict, "y") ?? offset.y
                );
            }
            else
            {
                // Individual fields format: {"offsetMinX": 10, "offsetMinY": 10}
                var offsetMinX = GetFloat(payload, "offsetMinX");
                var offsetMinY = GetFloat(payload, "offsetMinY");
                if (offsetMinX.HasValue || offsetMinY.HasValue)
                {
                    var offset = rectTransform.offsetMin;
                    rectTransform.offsetMin = new Vector2(
                        offsetMinX ?? offset.x,
                        offsetMinY ?? offset.y
                    );
                }
            }

            // Update offsetMax - supports both dictionary format and individual fields
            if (payload.TryGetValue("offsetMax", out var offsetMaxObj) && offsetMaxObj is Dictionary<string, object> offsetMaxDict)
            {
                // Dictionary format: {"offsetMax": {"x": -10, "y": -10}}
                var offset = rectTransform.offsetMax;
                rectTransform.offsetMax = new Vector2(
                    GetFloat(offsetMaxDict, "x") ?? offset.x,
                    GetFloat(offsetMaxDict, "y") ?? offset.y
                );
            }
            else
            {
                // Individual fields format: {"offsetMaxX": -10, "offsetMaxY": -10}
                var offsetMaxX = GetFloat(payload, "offsetMaxX");
                var offsetMaxY = GetFloat(payload, "offsetMaxY");
                if (offsetMaxX.HasValue || offsetMaxY.HasValue)
                {
                    var offset = rectTransform.offsetMax;
                    rectTransform.offsetMax = new Vector2(
                        offsetMaxX ?? offset.x,
                        offsetMaxY ?? offset.y
                    );
                }
            }

            // Update anchorMin - supports both dictionary format and individual fields
            if (payload.TryGetValue("anchorMin", out var anchorMinObj) && anchorMinObj is Dictionary<string, object> anchorMinDict)
            {
                // Dictionary format: {"anchorMin": {"x": 0, "y": 0}}
                var anchor = rectTransform.anchorMin;
                rectTransform.anchorMin = new Vector2(
                    GetFloat(anchorMinDict, "x") ?? anchor.x,
                    GetFloat(anchorMinDict, "y") ?? anchor.y
                );
            }

            // Update anchorMax - supports both dictionary format and individual fields
            if (payload.TryGetValue("anchorMax", out var anchorMaxObj) && anchorMaxObj is Dictionary<string, object> anchorMaxDict)
            {
                // Dictionary format: {"anchorMax": {"x": 1, "y": 1}}
                var anchor = rectTransform.anchorMax;
                rectTransform.anchorMax = new Vector2(
                    GetFloat(anchorMaxDict, "x") ?? anchor.x,
                    GetFloat(anchorMaxDict, "y") ?? anchor.y
                );
            }
        }

        /// <summary>
        /// Creates a standard result with before/after state and operation name.
        /// </summary>
        private static Dictionary<string, object> CreateStandardResult(
            Dictionary<string, object> beforeState, RectTransform rectTransform, string operation)
        {
            return new Dictionary<string, object>
            {
                ["before"] = beforeState,
                ["after"] = CaptureRectTransformState(rectTransform),
                ["operation"] = operation,
            };
        }

        /// <summary>
        /// Creates UI elements from templates (Button, Text, Image, Panel, ScrollView, InputField, Slider, Toggle, Dropdown).
        /// Each template automatically includes necessary components and sensible defaults.
        /// </summary>
        /// <param name="payload">Template parameters including 'template' type, name, parentPath, size, position, etc.</param>
        /// <returns>Result dictionary with created GameObject information.</returns>
        private static object HandleUguiCreateFromTemplate(Dictionary<string, object> payload)
        {
            try
            {
                var template = GetString(payload, "template");
                if (string.IsNullOrEmpty(template))
                {
                    throw new InvalidOperationException("template is required");
                }

                Debug.Log($"[uguiCreateFromTemplate] Creating template: {template}");

                // Get parent path or find first Canvas
                var parentPath = GetString(payload, "parentPath");
                GameObject parent = null;
                if (!string.IsNullOrEmpty(parentPath))
                {
                    parent = ResolveGameObject(parentPath);
                }
                else
                {
                    // Find first Canvas in the scene
                    var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
                    if (canvas != null)
                    {
                        parent = canvas.gameObject;
                    }
                    else
                    {
                        throw new InvalidOperationException("No Canvas found in scene. Please specify parentPath or create a Canvas first.");
                    }
                }

                // Verify parent is under a Canvas
                if (parent.GetComponentInParent<Canvas>() == null)
                {
                    throw new InvalidOperationException("Parent must be under a Canvas");
                }

                // Get name or use template as default
                var name = GetString(payload, "name");
                if (string.IsNullOrEmpty(name))
                {
                    name = template;
                }

                // Create the GameObject based on template
                GameObject go = null;
                switch (template)
                {
                    case "Button":
                        go = CreateButtonTemplate(name, parent, payload);
                        break;
                    case "Text":
                        go = CreateTextTemplate(name, parent, payload);
                        break;
                    case "Image":
                        go = CreateImageTemplate(name, parent, payload);
                        break;
                    case "RawImage":
                        go = CreateRawImageTemplate(name, parent, payload);
                        break;
                    case "Panel":
                        go = CreatePanelTemplate(name, parent, payload);
                        break;
                    case "ScrollView":
                        go = CreateScrollViewTemplate(name, parent, payload);
                        break;
                    case "InputField":
                        go = CreateInputFieldTemplate(name, parent, payload);
                        break;
                    case "Slider":
                        go = CreateSliderTemplate(name, parent, payload);
                        break;
                    case "Toggle":
                        go = CreateToggleTemplate(name, parent, payload);
                        break;
                    case "Dropdown":
                        go = CreateDropdownTemplate(name, parent, payload);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown template: {template}");
                }

                Undo.RegisterCreatedObjectUndo(go, $"Create {template}");
                Selection.activeGameObject = go;

                Debug.Log($"[uguiCreateFromTemplate] Created {template}: {GetHierarchyPath(go)}");

                return new Dictionary<string, object>
                {
                    ["template"] = template,
                    ["gameObjectPath"] = GetHierarchyPath(go),
                    ["name"] = go.name,
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[uguiCreateFromTemplate] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Creates a text component (UI.Text or TextMeshPro) based on the useTextMeshPro flag.
        /// </summary>
        private static Component CreateTextComponent(GameObject textGo, Dictionary<string, object> payload, string defaultText, int defaultFontSize)
        {
            var useTextMeshPro = GetBool(payload, "useTextMeshPro", false);
            var textContent = GetString(payload, "text") ?? defaultText;
            var fontSize = GetInt(payload, "fontSize", defaultFontSize);

            if (useTextMeshPro)
            {
                // Try to use TextMeshPro (TMPro)
                var tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
                if (tmpType == null)
                {
                    Debug.LogWarning("[CreateTextComponent] TextMeshPro package not found. Falling back to UI.Text. Install TextMeshPro package to use TMP components.");
                    useTextMeshPro = false;
                }
                else
                {
                    var tmpComponent = textGo.AddComponent(tmpType);

                    // Set text property
                    var textProp = tmpType.GetProperty("text");
                    if (textProp != null)
                    {
                        textProp.SetValue(tmpComponent, textContent);
                    }

                    // Set fontSize property
                    var fontSizeProp = tmpType.GetProperty("fontSize");
                    if (fontSizeProp != null)
                    {
                        fontSizeProp.SetValue(tmpComponent, (float)fontSize);
                    }

                    // Set alignment property
                    var alignmentProp = tmpType.GetProperty("alignment");
                    if (alignmentProp != null)
                    {
                        // TextAlignmentOptions.Center = 514
                        var alignmentType = Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
                        if (alignmentType != null)
                        {
                            alignmentProp.SetValue(tmpComponent, Enum.ToObject(alignmentType, 514));
                        }
                    }

                    // Set color property
                    var colorProp = tmpType.GetProperty("color");
                    if (colorProp != null)
                    {
                        colorProp.SetValue(tmpComponent, Color.black);
                    }

                    return tmpComponent;
                }
            }

            // Use standard UI.Text
            var text = textGo.AddComponent<UnityEngine.UI.Text>();
            text.text = textContent;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            return text;
        }

        private static GameObject CreateButtonTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(1f, 1f, 1f, 1f);

            var button = go.AddComponent<UnityEngine.UI.Button>();
            button.interactable = GetBool(payload, "interactable", true);

            // Create Text child
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            CreateTextComponent(textGo, payload, "Button", 14);

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 160, 30);

            return go;
        }

        private static GameObject CreateTextTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            CreateTextComponent(go, payload, "New Text", 14);

            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 160, 30);

            return go;
        }

        private static GameObject CreateImageTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.white;

            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 100, 100);

            return go;
        }

        private static GameObject CreateRawImageTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var rawImage = go.AddComponent<UnityEngine.UI.RawImage>();
            rawImage.color = Color.white;

            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 100, 100);

            return go;
        }

        private static GameObject CreatePanelTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(1f, 1f, 1f, 0.392f);

            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 200, 200);

            return go;
        }

        private static GameObject CreateScrollViewTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            // Create main ScrollView GameObject
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var scrollRect = go.AddComponent<UnityEngine.UI.ScrollRect>();
            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(1f, 1f, 1f, 1f);

            // Create Viewport
            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(go.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewport.AddComponent<UnityEngine.UI.Mask>();
            var viewportImage = viewport.AddComponent<UnityEngine.UI.Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);

            // Create Content
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 300);

            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 200, 200);

            return go;
        }

        private static GameObject CreateInputFieldTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.white;

            var inputField = go.AddComponent<UnityEngine.UI.InputField>();
            inputField.interactable = GetBool(payload, "interactable", true);

            // Create Text child
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<UnityEngine.UI.Text>();
            text.text = GetString(payload, "text") ?? "";
            text.fontSize = GetInt(payload, "fontSize", 14);
            text.color = Color.black;
            text.supportRichText = false;

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 6);
            textRect.offsetMax = new Vector2(-10, -7);

            // Create Placeholder child
            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
            placeholderGo.transform.SetParent(go.transform, false);
            var placeholder = placeholderGo.AddComponent<UnityEngine.UI.Text>();
            placeholder.text = "Enter text...";
            placeholder.fontSize = GetInt(payload, "fontSize", 14);
            placeholder.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            placeholder.fontStyle = FontStyle.Italic;

            var placeholderRect = placeholderGo.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(10, 6);
            placeholderRect.offsetMax = new Vector2(-10, -7);

            inputField.textComponent = text;
            inputField.placeholder = placeholder;

            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 160, 30);

            return go;
        }

        private static GameObject CreateSliderTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var slider = go.AddComponent<UnityEngine.UI.Slider>();
            slider.interactable = GetBool(payload, "interactable", true);

            // Create Background
            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(go.transform, false);
            var bgImage = bgGo.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.sizeDelta = new Vector2(0, 0);

            // Create Fill Area
            var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGo.transform.SetParent(go.transform, false);
            var fillAreaRect = fillAreaGo.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);

            // Create Fill
            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillImage = fillGo.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = new Color(0.5f, 0.8f, 1f, 1f);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.sizeDelta = new Vector2(10, 0);

            // Create Handle Slide Area
            var handleAreaGo = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleAreaGo.transform.SetParent(go.transform, false);
            var handleAreaRect = handleAreaGo.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = new Vector2(0, 0);
            handleAreaRect.anchorMax = new Vector2(1, 1);
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);

            // Create Handle
            var handleGo = new GameObject("Handle", typeof(RectTransform));
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            var handleImage = handleGo.AddComponent<UnityEngine.UI.Image>();
            handleImage.color = Color.white;
            var handleRect = handleGo.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;

            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 160, 20);

            return go;
        }

        private static GameObject CreateToggleTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var toggle = go.AddComponent<UnityEngine.UI.Toggle>();
            toggle.interactable = GetBool(payload, "interactable", true);

            // Create Background
            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(go.transform, false);
            var bgImage = bgGo.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = Color.white;
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.5f);
            bgRect.anchorMax = new Vector2(0, 0.5f);
            bgRect.anchoredPosition = new Vector2(10, 0);
            bgRect.sizeDelta = new Vector2(20, 20);

            // Create Checkmark
            var checkGo = new GameObject("Checkmark", typeof(RectTransform));
            checkGo.transform.SetParent(bgGo.transform, false);
            var checkImage = checkGo.AddComponent<UnityEngine.UI.Image>();
            checkImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);
            var checkRect = checkGo.GetComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.sizeDelta = Vector2.zero;

            // Create Label
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var label = labelGo.AddComponent<UnityEngine.UI.Text>();
            label.text = GetString(payload, "text") ?? "Toggle";
            label.fontSize = GetInt(payload, "fontSize", 14);
            label.color = Color.black;
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(23, 0);
            labelRect.offsetMax = new Vector2(0, 0);

            toggle.graphic = checkImage;
            toggle.targetGraphic = bgImage;

            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 160, 20);

            return go;
        }

        private static GameObject CreateDropdownTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.white;

            var dropdown = go.AddComponent<UnityEngine.UI.Dropdown>();
            dropdown.interactable = GetBool(payload, "interactable", true);

            // Create Label
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var label = labelGo.AddComponent<UnityEngine.UI.Text>();
            label.text = GetString(payload, "text") ?? "Option A";
            label.fontSize = GetInt(payload, "fontSize", 14);
            label.color = Color.black;
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10, 6);
            labelRect.offsetMax = new Vector2(-25, -7);

            // Create Arrow
            var arrowGo = new GameObject("Arrow", typeof(RectTransform));
            arrowGo.transform.SetParent(go.transform, false);
            var arrow = arrowGo.AddComponent<UnityEngine.UI.Text>();
            arrow.text = "▼";
            arrow.fontSize = GetInt(payload, "fontSize", 14);
            arrow.color = Color.black;
            arrow.alignment = TextAnchor.MiddleCenter;
            var arrowRect = arrowGo.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0);
            arrowRect.anchorMax = new Vector2(1, 1);
            arrowRect.sizeDelta = new Vector2(20, 0);
            arrowRect.anchoredPosition = new Vector2(-15, 0);

            // Create Template (simplified version)
            var templateGo = new GameObject("Template", typeof(RectTransform));
            templateGo.transform.SetParent(go.transform, false);
            var templateRect = templateGo.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = new Vector2(0, 2);
            templateRect.sizeDelta = new Vector2(0, 150);
            templateGo.SetActive(false);

            dropdown.captionText = label;
            dropdown.template = templateRect;

            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 160, 30);

            return go;
        }

        private static void ApplyCommonRectTransformSettings(RectTransform rectTransform, Dictionary<string, object> payload, float defaultWidth, float defaultHeight)
        {
            // Apply anchor preset
            var anchorPreset = GetString(payload, "anchorPreset") ?? "center";
            var presetPayload = new Dictionary<string, object>
            {
                ["preset"] = anchorPreset,
                ["preservePosition"] = false
            };
            SetAnchorPreset(rectTransform, presetPayload);

            // Apply size (with validation to prevent negative values)
            var width = GetFloat(payload, "width") ?? defaultWidth;
            var height = GetFloat(payload, "height") ?? defaultHeight;

            if (width < 0)
            {
                Debug.LogWarning($"[ApplyCommonRectTransformSettings] Width cannot be negative. Clamping {width} to 0.");
                width = 0;
            }

            if (height < 0)
            {
                Debug.LogWarning($"[ApplyCommonRectTransformSettings] Height cannot be negative. Clamping {height} to 0.");
                height = 0;
            }

            rectTransform.sizeDelta = new Vector2(width, height);

            // Apply position
            var posX = GetFloat(payload, "positionX") ?? 0f;
            var posY = GetFloat(payload, "positionY") ?? 0f;
            rectTransform.anchoredPosition = new Vector2(posX, posY);
        }

        /// <summary>
        /// Manages layout components (HorizontalLayoutGroup, VerticalLayoutGroup, GridLayoutGroup,
        /// ContentSizeFitter, LayoutElement, AspectRatioFitter) on UI GameObjects.
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation', 'gameObjectPath', 'layoutType', and layout-specific settings.</param>
        /// <returns>Result dictionary with operation-specific data.</returns>
        private static object HandleUguiLayoutManage(Dictionary<string, object> payload)
        {
            try
            {
                var operation = GetString(payload, "operation");
                if (string.IsNullOrEmpty(operation))
                {
                    throw new InvalidOperationException("operation is required");
                }

                var path = GetString(payload, "gameObjectPath");
                if (string.IsNullOrEmpty(path))
                {
                    throw new InvalidOperationException("gameObjectPath is required");
                }

                Debug.Log($"[uguiLayoutManage] Processing operation '{operation}' on: {path}");

                var go = ResolveGameObject(path);

                object result;
                switch (operation)
                {
                    case "add":
                        result = AddLayoutComponent(go, payload);
                        break;
                    case "update":
                        result = UpdateLayoutComponent(go, payload);
                        break;
                    case "remove":
                        result = RemoveLayoutComponent(go, payload);
                        break;
                    case "inspect":
                        result = InspectLayoutComponent(go, payload);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown uguiLayoutManage operation: {operation}");
                }

                EditorUtility.SetDirty(go);
                Debug.Log($"[uguiLayoutManage] Completed successfully");

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[uguiLayoutManage] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private static object AddLayoutComponent(GameObject go, Dictionary<string, object> payload)
        {
            var layoutType = GetString(payload, "layoutType");
            if (string.IsNullOrEmpty(layoutType))
            {
                throw new InvalidOperationException("layoutType is required for add operation");
            }

            Component component;
            switch (layoutType)
            {
                case "HorizontalLayoutGroup":
                    component = go.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                    ApplyLayoutGroupSettings(component, payload);
                    break;
                case "VerticalLayoutGroup":
                    component = go.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                    ApplyLayoutGroupSettings(component, payload);
                    break;
                case "GridLayoutGroup":
                    component = go.AddComponent<UnityEngine.UI.GridLayoutGroup>();
                    ApplyGridLayoutGroupSettings((UnityEngine.UI.GridLayoutGroup)component, payload);
                    break;
                case "ContentSizeFitter":
                    component = go.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                    ApplyContentSizeFitterSettings((UnityEngine.UI.ContentSizeFitter)component, payload);
                    break;
                case "LayoutElement":
                    component = go.AddComponent<UnityEngine.UI.LayoutElement>();
                    ApplyLayoutElementSettings((UnityEngine.UI.LayoutElement)component, payload);
                    break;
                case "AspectRatioFitter":
                    component = go.AddComponent<UnityEngine.UI.AspectRatioFitter>();
                    ApplyAspectRatioFitterSettings((UnityEngine.UI.AspectRatioFitter)component, payload);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown layoutType: {layoutType}");
            }

            return new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["layoutType"] = layoutType,
                ["gameObjectPath"] = GetHierarchyPath(go),
            };
        }

        private static object UpdateLayoutComponent(GameObject go, Dictionary<string, object> payload)
        {
            var layoutType = GetString(payload, "layoutType");
            if (string.IsNullOrEmpty(layoutType))
            {
                throw new InvalidOperationException("layoutType is required for update operation");
            }

            Component component = null;
            switch (layoutType)
            {
                case "HorizontalLayoutGroup":
                    component = go.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                    if (component != null) ApplyLayoutGroupSettings(component, payload);
                    break;
                case "VerticalLayoutGroup":
                    component = go.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                    if (component != null) ApplyLayoutGroupSettings(component, payload);
                    break;
                case "GridLayoutGroup":
                    component = go.GetComponent<UnityEngine.UI.GridLayoutGroup>();
                    if (component != null) ApplyGridLayoutGroupSettings((UnityEngine.UI.GridLayoutGroup)component, payload);
                    break;
                case "ContentSizeFitter":
                    component = go.GetComponent<UnityEngine.UI.ContentSizeFitter>();
                    if (component != null) ApplyContentSizeFitterSettings((UnityEngine.UI.ContentSizeFitter)component, payload);
                    break;
                case "LayoutElement":
                    component = go.GetComponent<UnityEngine.UI.LayoutElement>();
                    if (component != null) ApplyLayoutElementSettings((UnityEngine.UI.LayoutElement)component, payload);
                    break;
                case "AspectRatioFitter":
                    component = go.GetComponent<UnityEngine.UI.AspectRatioFitter>();
                    if (component != null) ApplyAspectRatioFitterSettings((UnityEngine.UI.AspectRatioFitter)component, payload);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown layoutType: {layoutType}");
            }

            if (component == null)
            {
                throw new InvalidOperationException($"Component {layoutType} not found on GameObject");
            }

            return new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["layoutType"] = layoutType,
                ["gameObjectPath"] = GetHierarchyPath(go),
            };
        }

        private static object RemoveLayoutComponent(GameObject go, Dictionary<string, object> payload)
        {
            var layoutType = GetString(payload, "layoutType");
            if (string.IsNullOrEmpty(layoutType))
            {
                throw new InvalidOperationException("layoutType is required for remove operation");
            }

            Component component = null;
            switch (layoutType)
            {
                case "HorizontalLayoutGroup":
                    component = go.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                    break;
                case "VerticalLayoutGroup":
                    component = go.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                    break;
                case "GridLayoutGroup":
                    component = go.GetComponent<UnityEngine.UI.GridLayoutGroup>();
                    break;
                case "ContentSizeFitter":
                    component = go.GetComponent<UnityEngine.UI.ContentSizeFitter>();
                    break;
                case "LayoutElement":
                    component = go.GetComponent<UnityEngine.UI.LayoutElement>();
                    break;
                case "AspectRatioFitter":
                    component = go.GetComponent<UnityEngine.UI.AspectRatioFitter>();
                    break;
                default:
                    throw new InvalidOperationException($"Unknown layoutType: {layoutType}");
            }

            if (component == null)
            {
                throw new InvalidOperationException($"Component {layoutType} not found on GameObject");
            }

            UnityEngine.Object.DestroyImmediate(component);

            return new Dictionary<string, object>
            {
                ["operation"] = "remove",
                ["layoutType"] = layoutType,
                ["gameObjectPath"] = GetHierarchyPath(go),
            };
        }

        private static object InspectLayoutComponent(GameObject go, Dictionary<string, object> payload)
        {
            var layoutType = GetString(payload, "layoutType");
            var result = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = GetHierarchyPath(go),
                ["layouts"] = new List<object>(),
            };

            var layouts = new List<object>();

            if (string.IsNullOrEmpty(layoutType))
            {
                // Inspect all layout components if layoutType not specified
                var hlg = go.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                if (hlg != null) layouts.Add(SerializeLayoutGroup(hlg, "HorizontalLayoutGroup"));

                var vlg = go.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                if (vlg != null) layouts.Add(SerializeLayoutGroup(vlg, "VerticalLayoutGroup"));

                var glg = go.GetComponent<UnityEngine.UI.GridLayoutGroup>();
                if (glg != null) layouts.Add(SerializeGridLayoutGroup(glg));

                var csf = go.GetComponent<UnityEngine.UI.ContentSizeFitter>();
                if (csf != null) layouts.Add(SerializeContentSizeFitter(csf));

                var le = go.GetComponent<UnityEngine.UI.LayoutElement>();
                if (le != null) layouts.Add(SerializeLayoutElement(le));

                var arf = go.GetComponent<UnityEngine.UI.AspectRatioFitter>();
                if (arf != null) layouts.Add(SerializeAspectRatioFitter(arf));
            }
            else
            {
                // Inspect specific layout type
                switch (layoutType)
                {
                    case "HorizontalLayoutGroup":
                        var hlg = go.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                        if (hlg != null) layouts.Add(SerializeLayoutGroup(hlg, "HorizontalLayoutGroup"));
                        break;
                    case "VerticalLayoutGroup":
                        var vlg = go.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                        if (vlg != null) layouts.Add(SerializeLayoutGroup(vlg, "VerticalLayoutGroup"));
                        break;
                    case "GridLayoutGroup":
                        var glg = go.GetComponent<UnityEngine.UI.GridLayoutGroup>();
                        if (glg != null) layouts.Add(SerializeGridLayoutGroup(glg));
                        break;
                    case "ContentSizeFitter":
                        var csf = go.GetComponent<UnityEngine.UI.ContentSizeFitter>();
                        if (csf != null) layouts.Add(SerializeContentSizeFitter(csf));
                        break;
                    case "LayoutElement":
                        var le = go.GetComponent<UnityEngine.UI.LayoutElement>();
                        if (le != null) layouts.Add(SerializeLayoutElement(le));
                        break;
                    case "AspectRatioFitter":
                        var arf = go.GetComponent<UnityEngine.UI.AspectRatioFitter>();
                        if (arf != null) layouts.Add(SerializeAspectRatioFitter(arf));
                        break;
                }
            }

            result["layouts"] = layouts;
            return result;
        }

        private static void ApplyLayoutGroupSettings(Component component, Dictionary<string, object> payload)
        {
            var layoutGroup = component as UnityEngine.UI.HorizontalOrVerticalLayoutGroup;
            if (layoutGroup == null) return;

            // Apply padding
            if (payload.ContainsKey("padding"))
            {
                var paddingDict = payload["padding"] as Dictionary<string, object>;
                if (paddingDict != null)
                {
                    layoutGroup.padding = new RectOffset(
                        GetInt(paddingDict, "left", layoutGroup.padding.left),
                        GetInt(paddingDict, "right", layoutGroup.padding.right),
                        GetInt(paddingDict, "top", layoutGroup.padding.top),
                        GetInt(paddingDict, "bottom", layoutGroup.padding.bottom)
                    );
                }
            }

            // Apply spacing
            var spacing = GetFloat(payload, "spacing");
            if (spacing.HasValue) layoutGroup.spacing = spacing.Value;

            // Apply childAlignment
            var childAlignment = GetString(payload, "childAlignment");
            if (!string.IsNullOrEmpty(childAlignment))
            {
                layoutGroup.childAlignment = (TextAnchor)System.Enum.Parse(typeof(TextAnchor), childAlignment);
            }

            // Apply child control settings
            if (payload.ContainsKey("childControlWidth"))
                layoutGroup.childControlWidth = GetBool(payload, "childControlWidth");
            if (payload.ContainsKey("childControlHeight"))
                layoutGroup.childControlHeight = GetBool(payload, "childControlHeight");
            if (payload.ContainsKey("childForceExpandWidth"))
                layoutGroup.childForceExpandWidth = GetBool(payload, "childForceExpandWidth");
            if (payload.ContainsKey("childForceExpandHeight"))
                layoutGroup.childForceExpandHeight = GetBool(payload, "childForceExpandHeight");
        }

        private static void ApplyGridLayoutGroupSettings(UnityEngine.UI.GridLayoutGroup grid, Dictionary<string, object> payload)
        {
            // Apply common layout group settings
            if (payload.ContainsKey("padding"))
            {
                var paddingDict = payload["padding"] as Dictionary<string, object>;
                if (paddingDict != null)
                {
                    grid.padding = new RectOffset(
                        GetInt(paddingDict, "left", grid.padding.left),
                        GetInt(paddingDict, "right", grid.padding.right),
                        GetInt(paddingDict, "top", grid.padding.top),
                        GetInt(paddingDict, "bottom", grid.padding.bottom)
                    );
                }
            }

            var childAlignment = GetString(payload, "childAlignment");
            if (!string.IsNullOrEmpty(childAlignment))
            {
                grid.childAlignment = (TextAnchor)System.Enum.Parse(typeof(TextAnchor), childAlignment);
            }

            // Apply grid-specific settings
            var cellSizeX = GetFloat(payload, "cellSizeX");
            var cellSizeY = GetFloat(payload, "cellSizeY");
            if (cellSizeX.HasValue || cellSizeY.HasValue)
            {
                grid.cellSize = new Vector2(
                    cellSizeX ?? grid.cellSize.x,
                    cellSizeY ?? grid.cellSize.y
                );
            }

            var spacingX = GetFloat(payload, "spacing");
            var spacingY = GetFloat(payload, "spacingY");
            if (spacingX.HasValue || spacingY.HasValue)
            {
                grid.spacing = new Vector2(
                    spacingX ?? grid.spacing.x,
                    spacingY ?? grid.spacing.y
                );
            }

            var constraint = GetString(payload, "constraint");
            if (!string.IsNullOrEmpty(constraint))
            {
                grid.constraint = (UnityEngine.UI.GridLayoutGroup.Constraint)System.Enum.Parse(typeof(UnityEngine.UI.GridLayoutGroup.Constraint), constraint);
            }

            var constraintCount = GetInt(payload, "constraintCount", -1);
            if (constraintCount >= 0)
            {
                grid.constraintCount = constraintCount;
            }

            var startCorner = GetString(payload, "startCorner");
            if (!string.IsNullOrEmpty(startCorner))
            {
                grid.startCorner = (UnityEngine.UI.GridLayoutGroup.Corner)System.Enum.Parse(typeof(UnityEngine.UI.GridLayoutGroup.Corner), startCorner);
            }

            var startAxis = GetString(payload, "startAxis");
            if (!string.IsNullOrEmpty(startAxis))
            {
                grid.startAxis = (UnityEngine.UI.GridLayoutGroup.Axis)System.Enum.Parse(typeof(UnityEngine.UI.GridLayoutGroup.Axis), startAxis);
            }
        }

        private static void ApplyContentSizeFitterSettings(UnityEngine.UI.ContentSizeFitter fitter, Dictionary<string, object> payload)
        {
            var horizontalFit = GetString(payload, "horizontalFit");
            if (!string.IsNullOrEmpty(horizontalFit))
            {
                fitter.horizontalFit = (UnityEngine.UI.ContentSizeFitter.FitMode)System.Enum.Parse(typeof(UnityEngine.UI.ContentSizeFitter.FitMode), horizontalFit);
            }

            var verticalFit = GetString(payload, "verticalFit");
            if (!string.IsNullOrEmpty(verticalFit))
            {
                fitter.verticalFit = (UnityEngine.UI.ContentSizeFitter.FitMode)System.Enum.Parse(typeof(UnityEngine.UI.ContentSizeFitter.FitMode), verticalFit);
            }
        }

        private static void ApplyLayoutElementSettings(UnityEngine.UI.LayoutElement element, Dictionary<string, object> payload)
        {
            var minWidth = GetFloat(payload, "minWidth");
            if (minWidth.HasValue) element.minWidth = minWidth.Value;

            var minHeight = GetFloat(payload, "minHeight");
            if (minHeight.HasValue) element.minHeight = minHeight.Value;

            var preferredWidth = GetFloat(payload, "preferredWidth");
            if (preferredWidth.HasValue) element.preferredWidth = preferredWidth.Value;

            var preferredHeight = GetFloat(payload, "preferredHeight");
            if (preferredHeight.HasValue) element.preferredHeight = preferredHeight.Value;

            var flexibleWidth = GetFloat(payload, "flexibleWidth");
            if (flexibleWidth.HasValue) element.flexibleWidth = flexibleWidth.Value;

            var flexibleHeight = GetFloat(payload, "flexibleHeight");
            if (flexibleHeight.HasValue) element.flexibleHeight = flexibleHeight.Value;

            if (payload.ContainsKey("ignoreLayout"))
                element.ignoreLayout = GetBool(payload, "ignoreLayout");
        }

        private static void ApplyAspectRatioFitterSettings(UnityEngine.UI.AspectRatioFitter fitter, Dictionary<string, object> payload)
        {
            var aspectMode = GetString(payload, "aspectMode");
            if (!string.IsNullOrEmpty(aspectMode))
            {
                fitter.aspectMode = (UnityEngine.UI.AspectRatioFitter.AspectMode)System.Enum.Parse(typeof(UnityEngine.UI.AspectRatioFitter.AspectMode), aspectMode);
            }

            var aspectRatio = GetFloat(payload, "aspectRatio");
            if (aspectRatio.HasValue) fitter.aspectRatio = aspectRatio.Value;
        }

        private static Dictionary<string, object> SerializeLayoutGroup(Component component, string typeName)
        {
            var layoutGroup = component as UnityEngine.UI.HorizontalOrVerticalLayoutGroup;
            return new Dictionary<string, object>
            {
                ["type"] = typeName,
                ["padding"] = new Dictionary<string, object>
                {
                    ["left"] = layoutGroup.padding.left,
                    ["right"] = layoutGroup.padding.right,
                    ["top"] = layoutGroup.padding.top,
                    ["bottom"] = layoutGroup.padding.bottom,
                },
                ["spacing"] = layoutGroup.spacing,
                ["childAlignment"] = layoutGroup.childAlignment.ToString(),
                ["childControlWidth"] = layoutGroup.childControlWidth,
                ["childControlHeight"] = layoutGroup.childControlHeight,
                ["childForceExpandWidth"] = layoutGroup.childForceExpandWidth,
                ["childForceExpandHeight"] = layoutGroup.childForceExpandHeight,
            };
        }

        private static Dictionary<string, object> SerializeGridLayoutGroup(UnityEngine.UI.GridLayoutGroup grid)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "GridLayoutGroup",
                ["padding"] = new Dictionary<string, object>
                {
                    ["left"] = grid.padding.left,
                    ["right"] = grid.padding.right,
                    ["top"] = grid.padding.top,
                    ["bottom"] = grid.padding.bottom,
                },
                ["cellSize"] = new Dictionary<string, object>
                {
                    ["x"] = grid.cellSize.x,
                    ["y"] = grid.cellSize.y,
                },
                ["spacing"] = new Dictionary<string, object>
                {
                    ["x"] = grid.spacing.x,
                    ["y"] = grid.spacing.y,
                },
                ["childAlignment"] = grid.childAlignment.ToString(),
                ["constraint"] = grid.constraint.ToString(),
                ["constraintCount"] = grid.constraintCount,
                ["startCorner"] = grid.startCorner.ToString(),
                ["startAxis"] = grid.startAxis.ToString(),
            };
        }

        private static Dictionary<string, object> SerializeContentSizeFitter(UnityEngine.UI.ContentSizeFitter fitter)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "ContentSizeFitter",
                ["horizontalFit"] = fitter.horizontalFit.ToString(),
                ["verticalFit"] = fitter.verticalFit.ToString(),
            };
        }

        private static Dictionary<string, object> SerializeLayoutElement(UnityEngine.UI.LayoutElement element)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "LayoutElement",
                ["minWidth"] = element.minWidth,
                ["minHeight"] = element.minHeight,
                ["preferredWidth"] = element.preferredWidth,
                ["preferredHeight"] = element.preferredHeight,
                ["flexibleWidth"] = element.flexibleWidth,
                ["flexibleHeight"] = element.flexibleHeight,
                ["ignoreLayout"] = element.ignoreLayout,
            };
        }

        private static Dictionary<string, object> SerializeAspectRatioFitter(UnityEngine.UI.AspectRatioFitter fitter)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "AspectRatioFitter",
                ["aspectMode"] = fitter.aspectMode.ToString(),
                ["aspectRatio"] = fitter.aspectRatio,
            };
        }

        /// <summary>
        /// Detects overlapping UI elements in the scene.
        /// Can check a specific GameObject for overlaps with others, or check all UI elements for overlaps with each other.
        /// </summary>
        /// <param name="payload">Detection parameters including 'gameObjectPath', 'checkAll', 'includeChildren', and 'threshold'.</param>
        /// <returns>Result dictionary with list of overlapping UI element pairs.</returns>
        private static object HandleUguiDetectOverlaps(Dictionary<string, object> payload)
        {
            try
            {
                var gameObjectPath = GetString(payload, "gameObjectPath");
                var checkAll = GetBool(payload, "checkAll", false);
                var includeChildren = GetBool(payload, "includeChildren", false);
                var threshold = GetFloat(payload, "threshold") ?? 0f;

                Debug.Log($"[uguiDetectOverlaps] Detecting overlaps - checkAll={checkAll}, includeChildren={includeChildren}, threshold={threshold}");

                var overlaps = new List<Dictionary<string, object>>();

                if (checkAll)
                {
                    // Check all UI elements for overlaps with each other
                    var allRects = UnityEngine.Object.FindObjectsOfType<RectTransform>();
                    var rectList = new List<RectTransform>();

                    foreach (var rect in allRects)
                    {
                        // Only include RectTransforms that are under a Canvas
                        if (rect.GetComponentInParent<Canvas>() != null)
                        {
                            rectList.Add(rect);
                        }
                    }

                    Debug.Log($"[uguiDetectOverlaps] Checking {rectList.Count} UI elements");

                    for (int i = 0; i < rectList.Count; i++)
                    {
                        for (int j = i + 1; j < rectList.Count; j++)
                        {
                            var overlap = DetectRectOverlap(rectList[i], rectList[j], threshold);
                            if (overlap != null)
                            {
                                overlaps.Add(overlap);
                            }
                        }
                    }
                }
                else
                {
                    // Check specific GameObject for overlaps
                    if (string.IsNullOrEmpty(gameObjectPath))
                    {
                        throw new InvalidOperationException("gameObjectPath is required when checkAll is false");
                    }

                    var targetGo = ResolveGameObject(gameObjectPath);
                    var targetRects = new List<RectTransform>();

                    if (includeChildren)
                    {
                        targetRects.AddRange(targetGo.GetComponentsInChildren<RectTransform>());
                    }
                    else
                    {
                        var rect = targetGo.GetComponent<RectTransform>();
                        if (rect != null)
                        {
                            targetRects.Add(rect);
                        }
                    }

                    // Get all other RectTransforms in the scene
                    var allRects = UnityEngine.Object.FindObjectsOfType<RectTransform>();
                    var otherRects = new List<RectTransform>();

                    foreach (var rect in allRects)
                    {
                        // Only include RectTransforms that are under a Canvas and not in targetRects
                        if (rect.GetComponentInParent<Canvas>() != null && !targetRects.Contains(rect))
                        {
                            otherRects.Add(rect);
                        }
                    }

                    Debug.Log($"[uguiDetectOverlaps] Checking {targetRects.Count} target elements against {otherRects.Count} other elements");

                    foreach (var targetRect in targetRects)
                    {
                        foreach (var otherRect in otherRects)
                        {
                            var overlap = DetectRectOverlap(targetRect, otherRect, threshold);
                            if (overlap != null)
                            {
                                overlaps.Add(overlap);
                            }
                        }
                    }
                }

                Debug.Log($"[uguiDetectOverlaps] Found {overlaps.Count} overlaps");

                return new Dictionary<string, object>
                {
                    ["overlaps"] = overlaps,
                    ["count"] = overlaps.Count,
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[uguiDetectOverlaps] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Detects if two RectTransforms overlap in world space.
        /// </summary>
        /// <param name="rect1">First RectTransform.</param>
        /// <param name="rect2">Second RectTransform.</param>
        /// <param name="threshold">Minimum overlap area to be considered overlapping.</param>
        /// <returns>Dictionary with overlap information, or null if no overlap.</returns>
        private static Dictionary<string, object> DetectRectOverlap(RectTransform rect1, RectTransform rect2, float threshold)
        {
            // Get world corners for both rectangles
            Vector3[] corners1 = new Vector3[4];
            Vector3[] corners2 = new Vector3[4];
            rect1.GetWorldCorners(corners1);
            rect2.GetWorldCorners(corners2);

            // Calculate bounds in 2D (using x and y coordinates)
            float rect1MinX = Mathf.Min(corners1[0].x, corners1[1].x, corners1[2].x, corners1[3].x);
            float rect1MaxX = Mathf.Max(corners1[0].x, corners1[1].x, corners1[2].x, corners1[3].x);
            float rect1MinY = Mathf.Min(corners1[0].y, corners1[1].y, corners1[2].y, corners1[3].y);
            float rect1MaxY = Mathf.Max(corners1[0].y, corners1[1].y, corners1[2].y, corners1[3].y);

            float rect2MinX = Mathf.Min(corners2[0].x, corners2[1].x, corners2[2].x, corners2[3].x);
            float rect2MaxX = Mathf.Max(corners2[0].x, corners2[1].x, corners2[2].x, corners2[3].x);
            float rect2MinY = Mathf.Min(corners2[0].y, corners2[1].y, corners2[2].y, corners2[3].y);
            float rect2MaxY = Mathf.Max(corners2[0].y, corners2[1].y, corners2[2].y, corners2[3].y);

            // Check for overlap
            bool overlapsX = rect1MinX < rect2MaxX && rect1MaxX > rect2MinX;
            bool overlapsY = rect1MinY < rect2MaxY && rect1MaxY > rect2MinY;

            if (overlapsX && overlapsY)
            {
                // Calculate overlap area
                float overlapWidth = Mathf.Min(rect1MaxX, rect2MaxX) - Mathf.Max(rect1MinX, rect2MinX);
                float overlapHeight = Mathf.Min(rect1MaxY, rect2MaxY) - Mathf.Max(rect1MinY, rect2MinY);
                float overlapArea = overlapWidth * overlapHeight;

                if (overlapArea >= threshold)
                {
                    return new Dictionary<string, object>
                    {
                        ["element1"] = GetHierarchyPath(rect1.gameObject),
                        ["element2"] = GetHierarchyPath(rect2.gameObject),
                        ["overlapArea"] = overlapArea,
                        ["overlapWidth"] = overlapWidth,
                        ["overlapHeight"] = overlapHeight,
                        ["element1Bounds"] = new Dictionary<string, object>
                        {
                            ["minX"] = rect1MinX,
                            ["maxX"] = rect1MaxX,
                            ["minY"] = rect1MinY,
                            ["maxY"] = rect1MaxY,
                            ["width"] = rect1MaxX - rect1MinX,
                            ["height"] = rect1MaxY - rect1MinY,
                        },
                        ["element2Bounds"] = new Dictionary<string, object>
                        {
                            ["minX"] = rect2MinX,
                            ["maxX"] = rect2MaxX,
                            ["minY"] = rect2MinY,
                            ["maxY"] = rect2MaxY,
                            ["width"] = rect2MaxX - rect2MinX,
                            ["height"] = rect2MaxY - rect2MinY,
                        },
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Builds GameObject hierarchies declaratively from a nested structure definition.
        /// Allows creating complex multi-level hierarchies with components in a single command.
        /// </summary>
        /// <param name="payload">Hierarchy definition including nested GameObjects, components, and properties.</param>
        /// <returns>Result dictionary with created GameObject paths.</returns>
        private static object HandleHierarchyBuilder(Dictionary<string, object> payload)
        {
            try
            {
                var hierarchyDict = payload["hierarchy"] as Dictionary<string, object>;
                if (hierarchyDict == null)
                {
                    throw new InvalidOperationException("hierarchy is required");
                }

                var parentPath = GetString(payload, "parentPath");
                GameObject parent = null;
                if (!string.IsNullOrEmpty(parentPath))
                {
                    parent = ResolveGameObject(parentPath);
                }

                Debug.Log($"[hierarchyBuilder] Building hierarchy with {hierarchyDict.Count} root objects");

                var createdPaths = new List<string>();
                foreach (var kvp in hierarchyDict)
                {
                    var goName = kvp.Key;
                    var goSpec = kvp.Value as Dictionary<string, object>;
                    if (goSpec != null)
                    {
                        var createdGo = BuildGameObjectFromSpec(goName, goSpec, parent);
                        createdPaths.Add(GetHierarchyPath(createdGo));
                    }
                }

                return new Dictionary<string, object>
                {
                    ["createdObjects"] = createdPaths,
                    ["count"] = createdPaths.Count,
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[hierarchyBuilder] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private static GameObject BuildGameObjectFromSpec(string name, Dictionary<string, object> spec, GameObject parent)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent.transform, false);
            }

            // Add components
            if (spec.ContainsKey("components"))
            {
                var components = spec["components"] as List<object>;
                if (components != null)
                {
                    foreach (var comp in components)
                    {
                        var componentType = comp as string;
                        if (!string.IsNullOrEmpty(componentType))
                        {
                            var type = ResolveType(componentType);
                            if (type != null)
                            {
                                go.AddComponent(type);
                            }
                        }
                    }
                }
            }

            // Set properties
            if (spec.ContainsKey("properties"))
            {
                var properties = spec["properties"] as Dictionary<string, object>;
                if (properties != null)
                {
                    // Apply transform properties
                    if (properties.ContainsKey("position"))
                    {
                        var posDict = properties["position"] as Dictionary<string, object>;
                        if (posDict != null)
                        {
                            go.transform.localPosition = new Vector3(
                                GetFloat(posDict, "x") ?? 0,
                                GetFloat(posDict, "y") ?? 0,
                                GetFloat(posDict, "z") ?? 0
                            );
                        }
                    }

                    if (properties.ContainsKey("rotation"))
                    {
                        var rotDict = properties["rotation"] as Dictionary<string, object>;
                        if (rotDict != null)
                        {
                            go.transform.localEulerAngles = new Vector3(
                                GetFloat(rotDict, "x") ?? 0,
                                GetFloat(rotDict, "y") ?? 0,
                                GetFloat(rotDict, "z") ?? 0
                            );
                        }
                    }

                    if (properties.ContainsKey("scale"))
                    {
                        var scaleDict = properties["scale"] as Dictionary<string, object>;
                        if (scaleDict != null)
                        {
                            go.transform.localScale = new Vector3(
                                GetFloat(scaleDict, "x") ?? 1,
                                GetFloat(scaleDict, "y") ?? 1,
                                GetFloat(scaleDict, "z") ?? 1
                            );
                        }
                    }

                    // Apply component properties
                    foreach (var component in go.GetComponents<Component>())
                    {
                        var componentTypeName = component.GetType().Name;
                        if (properties.ContainsKey(componentTypeName))
                        {
                            var compProps = properties[componentTypeName] as Dictionary<string, object>;
                            if (compProps != null)
                            {
                                foreach (var propKvp in compProps)
                                {
                                    ApplyProperty(component, propKvp.Key, propKvp.Value);
                                }
                            }
                        }
                    }
                }
            }

            // Build children recursively
            if (spec.ContainsKey("children"))
            {
                var children = spec["children"] as Dictionary<string, object>;
                if (children != null)
                {
                    foreach (var childKvp in children)
                    {
                        var childName = childKvp.Key;
                        var childSpec = childKvp.Value as Dictionary<string, object>;
                        if (childSpec != null)
                        {
                            BuildGameObjectFromSpec(childName, childSpec, go);
                        }
                    }
                }
            }

            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            return go;
        }

        /// <summary>
        /// Quickly sets up new scenes with common configurations (3D, 2D, UI, VR).
        /// Automatically creates necessary GameObjects like Camera, Lights, Canvas, EventSystem.
        /// </summary>
        /// <param name="payload">Setup type and optional camera/light settings.</param>
        /// <returns>Result dictionary with created objects.</returns>
        private static object HandleSceneQuickSetup(Dictionary<string, object> payload)
        {
            try
            {
                var setupType = GetString(payload, "setupType");
                if (string.IsNullOrEmpty(setupType))
                {
                    throw new InvalidOperationException("setupType is required");
                }

                Debug.Log($"[sceneQuickSetup] Setting up {setupType} scene");

                var createdObjects = new List<string>();

                switch (setupType)
                {
                    case "3D":
                        createdObjects.AddRange(Setup3DScene(payload));
                        break;
                    case "2D":
                        createdObjects.AddRange(Setup2DScene(payload));
                        break;
                    case "UI":
                        createdObjects.AddRange(SetupUIScene(payload));
                        break;
                    case "VR":
                        createdObjects.AddRange(SetupVRScene(payload));
                        break;
                    case "Empty":
                        // Do nothing for empty scene
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown setupType: {setupType}");
                }

                return new Dictionary<string, object>
                {
                    ["setupType"] = setupType,
                    ["createdObjects"] = createdObjects,
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[sceneQuickSetup] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private static List<string> Setup3DScene(Dictionary<string, object> payload)
        {
            var created = new List<string>();

            // Check if Main Camera already exists
            var existingCamera = Camera.main;
            if (existingCamera == null)
            {
                // Create Main Camera
                var camera = new GameObject("Main Camera");
                camera.AddComponent<Camera>();
                camera.tag = "MainCamera";

                var camPosDict = payload.ContainsKey("cameraPosition") ? payload["cameraPosition"] as Dictionary<string, object> : null;
                if (camPosDict != null)
                {
                    camera.transform.position = new Vector3(
                        GetFloat(camPosDict, "x") ?? 0,
                        GetFloat(camPosDict, "y") ?? 1,
                        GetFloat(camPosDict, "z") ?? -10
                    );
                }
                else
                {
                    camera.transform.position = new Vector3(0, 1, -10);
                }

                Undo.RegisterCreatedObjectUndo(camera, "Create Main Camera");
                created.Add("Main Camera");
            }

            // Check if Directional Light already exists
            var existingLights = UnityEngine.Object.FindObjectsOfType<Light>();
            var hasDirectionalLight = false;
            foreach (var existingLight in existingLights)
            {
                if (existingLight.type == LightType.Directional)
                {
                    hasDirectionalLight = true;
                    break;
                }
            }

            if (!hasDirectionalLight)
            {
                // Create Directional Light
                var light = new GameObject("Directional Light");
                var lightComp = light.AddComponent<Light>();
                lightComp.type = LightType.Directional;
                lightComp.intensity = GetFloat(payload, "lightIntensity") ?? 1f;
                light.transform.rotation = Quaternion.Euler(50, -30, 0);

                Undo.RegisterCreatedObjectUndo(light, "Create Directional Light");
                created.Add("Directional Light");
            }

            return created;
        }

        private static List<string> Setup2DScene(Dictionary<string, object> payload)
        {
            var created = new List<string>();

            // Check if Main Camera already exists
            var existingCamera = Camera.main;
            if (existingCamera == null)
            {
                // Create Main Camera for 2D
                var camera = new GameObject("Main Camera");
                var cam = camera.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 5;
                camera.tag = "MainCamera";
                camera.transform.position = new Vector3(0, 0, -10);

                Undo.RegisterCreatedObjectUndo(camera, "Create Main Camera");
                created.Add("Main Camera");
            }

            return created;
        }

        private static List<string> SetupUIScene(Dictionary<string, object> payload)
        {
            var created = new List<string>();

            // Check if Canvas already exists
            var existingCanvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (existingCanvas == null)
            {
                // Create Canvas
                var canvas = new GameObject("Canvas");
                var canvasComp = canvas.AddComponent<Canvas>();
                canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

                Undo.RegisterCreatedObjectUndo(canvas, "Create Canvas");
                created.Add("Canvas");
            }

            // Check if EventSystem already exists
            var includeEventSystem = GetBool(payload, "includeEventSystem", true);
            if (includeEventSystem)
            {
                var existingEventSystem = UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
                if (existingEventSystem == null)
                {
                    var eventSystem = new GameObject("EventSystem");
                    eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

                    Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
                    created.Add("EventSystem");
                }
            }

            return created;
        }

        private static List<string> SetupVRScene(Dictionary<string, object> payload)
        {
            var created = new List<string>();

            // Check if Main Camera already exists
            var existingCamera = Camera.main;
            if (existingCamera == null)
            {
                // Create XR Origin (simplified - would need XR packages in real implementation)
                var camera = new GameObject("Main Camera");
                camera.AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.transform.position = new Vector3(0, 1.6f, 0);

                Undo.RegisterCreatedObjectUndo(camera, "Create Main Camera");
                created.Add("Main Camera");
            }

            return created;
        }

        /// <summary>
        /// Creates GameObjects from predefined templates (primitives, lights, camera, etc.).
        /// Each template includes appropriate components and sensible defaults.
        /// </summary>
        /// <param name="payload">Template type, name, parent, transform properties.</param>
        /// <returns>Result dictionary with created GameObject information.</returns>
        private static object HandleGameObjectCreateFromTemplate(Dictionary<string, object> payload)
        {
            try
            {
                var template = GetString(payload, "template");
                if (string.IsNullOrEmpty(template))
                {
                    throw new InvalidOperationException("template is required");
                }

                Debug.Log($"[gameObjectCreateFromTemplate] Creating template: {template}");

                var name = GetString(payload, "name");
                if (string.IsNullOrEmpty(name))
                {
                    name = template.Replace("Light-", "");
                }

                GameObject parent = null;
                var parentPath = GetString(payload, "parentPath");
                if (!string.IsNullOrEmpty(parentPath))
                {
                    parent = ResolveGameObject(parentPath);
                }

                GameObject go = null;
                switch (template)
                {
                    case "Camera":
                        go = new GameObject(name);
                        go.AddComponent<Camera>();
                        go.tag = "MainCamera";
                        break;
                    case "Light-Directional":
                        go = new GameObject(name);
                        var dirLight = go.AddComponent<Light>();
                        dirLight.type = LightType.Directional;
                        go.transform.rotation = Quaternion.Euler(50, -30, 0);
                        break;
                    case "Light-Point":
                        go = new GameObject(name);
                        var pointLight = go.AddComponent<Light>();
                        pointLight.type = LightType.Point;
                        break;
                    case "Light-Spot":
                        go = new GameObject(name);
                        var spotLight = go.AddComponent<Light>();
                        spotLight.type = LightType.Spot;
                        break;
                    case "Cube":
                        go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        go.name = name;
                        break;
                    case "Sphere":
                        go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        go.name = name;
                        break;
                    case "Plane":
                        go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                        go.name = name;
                        break;
                    case "Cylinder":
                        go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        go.name = name;
                        break;
                    case "Capsule":
                        go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        go.name = name;
                        break;
                    case "Quad":
                        go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        go.name = name;
                        break;
                    case "Empty":
                        go = new GameObject(name);
                        break;
                    case "Player":
                        go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        go.name = name;
                        go.AddComponent<Rigidbody>();
                        var playerCollider = go.GetComponent<Collider>();
                        if (playerCollider != null)
                        {
                            playerCollider.material = new PhysicsMaterial("PlayerPhysics");
                        }
                        break;
                    case "Enemy":
                        go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        go.name = name;
                        go.AddComponent<Rigidbody>();
                        break;
                    case "Particle System":
                        go = new GameObject(name);
                        go.AddComponent<ParticleSystem>();
                        break;
                    case "Audio Source":
                        go = new GameObject(name);
                        go.AddComponent<AudioSource>();
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown template: {template}");
                }

                if (parent != null)
                {
                    go.transform.SetParent(parent.transform, false);
                }

                // Apply transform properties
                if (payload.ContainsKey("position"))
                {
                    var posDict = payload["position"] as Dictionary<string, object>;
                    if (posDict != null)
                    {
                        go.transform.position = new Vector3(
                            GetFloat(posDict, "x") ?? 0,
                            GetFloat(posDict, "y") ?? 0,
                            GetFloat(posDict, "z") ?? 0
                        );
                    }
                }

                if (payload.ContainsKey("rotation"))
                {
                    var rotDict = payload["rotation"] as Dictionary<string, object>;
                    if (rotDict != null)
                    {
                        go.transform.eulerAngles = new Vector3(
                            GetFloat(rotDict, "x") ?? 0,
                            GetFloat(rotDict, "y") ?? 0,
                            GetFloat(rotDict, "z") ?? 0
                        );
                    }
                }

                if (payload.ContainsKey("scale"))
                {
                    var scaleDict = payload["scale"] as Dictionary<string, object>;
                    if (scaleDict != null)
                    {
                        go.transform.localScale = new Vector3(
                            GetFloat(scaleDict, "x") ?? 1,
                            GetFloat(scaleDict, "y") ?? 1,
                            GetFloat(scaleDict, "z") ?? 1
                        );
                    }
                }

                Undo.RegisterCreatedObjectUndo(go, $"Create {template}");
                Selection.activeGameObject = go;

                return new Dictionary<string, object>
                {
                    ["template"] = template,
                    ["gameObjectPath"] = GetHierarchyPath(go),
                    ["name"] = go.name,
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[gameObjectCreateFromTemplate] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Inspects the current scene context including hierarchy, GameObjects, and components.
        /// Provides a comprehensive overview for Claude to understand the current state.
        /// </summary>
        /// <param name="payload">Options for what to include in the inspection.</param>
        /// <returns>Result dictionary with scene context information.</returns>
        private static object HandleContextInspect(Dictionary<string, object> payload)
        {
            try
            {
                var includeHierarchy = GetBool(payload, "includeHierarchy", true);
                var includeComponents = GetBool(payload, "includeComponents", false);
                var maxDepth = GetInt(payload, "maxDepth", -1);
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

                        hierarchy.Add(BuildHierarchyInfo(root, 0, maxDepth, includeComponents, filter));
                    }

                    result["hierarchy"] = hierarchy;
                    result["rootObjectCount"] = rootObjects.Length;
                }

                // Add scene statistics
                var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                result["totalGameObjects"] = allObjects.Length;

                var cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
                result["cameraCount"] = cameras.Length;

                var lights = UnityEngine.Object.FindObjectsOfType<Light>();
                result["lightCount"] = lights.Length;

                var canvases = UnityEngine.Object.FindObjectsOfType<Canvas>();
                result["canvasCount"] = canvases.Length;

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[contextInspect] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private static Dictionary<string, object> BuildHierarchyInfo(GameObject go, int currentDepth, int maxDepth, bool includeComponents, string filter)
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

            // Include children if within depth limit
            if (maxDepth < 0 || currentDepth < maxDepth)
            {
                if (go.transform.childCount > 0)
                {
                    var children = new List<object>();
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

                        children.Add(BuildHierarchyInfo(child, currentDepth + 1, maxDepth, includeComponents, filter));
                    }
                    if (children.Count > 0)
                    {
                        info["children"] = children;
                    }
                }
            }

            return info;
        }

        /// <summary>
        /// Handles tag and layer management operations.
        /// Supports setting/getting tags and layers on GameObjects,
        /// and adding/removing tags and layers from the project.
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation' type.</param>
        /// <returns>Result dictionary with operation-specific data.</returns>
        private static object HandleTagLayerManage(Dictionary<string, object> payload)
        {
            try
            {
                var operation = GetString(payload, "operation");
                if (string.IsNullOrEmpty(operation))
                {
                    throw new InvalidOperationException("operation is required");
                }

                Debug.Log($"[tagLayerManage] Processing operation: {operation}");

                switch (operation)
                {
                    case "setTag":
                        return SetTag(payload);
                    case "getTag":
                        return GetTag(payload);
                    case "setLayer":
                        return SetLayer(payload);
                    case "getLayer":
                        return GetLayer(payload);
                    case "setLayerRecursive":
                        return SetLayerRecursive(payload);
                    case "listTags":
                        return ListTags();
                    case "addTag":
                        return AddTag(payload);
                    case "removeTag":
                        return RemoveTag(payload);
                    case "listLayers":
                        return ListLayers();
                    case "addLayer":
                        return AddLayer(payload);
                    case "removeLayer":
                        return RemoveLayer(payload);
                    default:
                        throw new InvalidOperationException($"Unknown tagLayerManage operation: {operation}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[tagLayerManage] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Sets the tag of a GameObject.
        /// </summary>
        private static object SetTag(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var tag = EnsureValue(GetString(payload, "tag"), "tag");

            var target = ResolveGameObject(path);
            var oldTag = target.tag;

            // Verify tag exists
            try
            {
                target.tag = tag;
            }
            catch (UnityException ex)
            {
                throw new InvalidOperationException($"Tag '{tag}' does not exist in the project. Use addTag operation to create it first. {ex.Message}");
            }

            EditorUtility.SetDirty(target);

            return new Dictionary<string, object>
            {
                ["gameObjectPath"] = path,
                ["oldTag"] = oldTag,
                ["newTag"] = tag,
                ["operation"] = "setTag",
            };
        }

        /// <summary>
        /// Gets the tag of a GameObject.
        /// </summary>
        private static object GetTag(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var target = ResolveGameObject(path);

            return new Dictionary<string, object>
            {
                ["gameObjectPath"] = path,
                ["tag"] = target.tag,
                ["operation"] = "getTag",
            };
        }

        /// <summary>
        /// Sets the layer of a GameObject.
        /// </summary>
        private static object SetLayer(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var target = ResolveGameObject(path);

            int newLayer;
            if (payload.TryGetValue("layer", out var layerObj))
            {
                if (layerObj is string layerName)
                {
                    newLayer = LayerMask.NameToLayer(layerName);
                    if (newLayer == -1)
                    {
                        throw new InvalidOperationException($"Layer '{layerName}' does not exist in the project. Use addLayer operation to create it first.");
                    }
                }
                else if (layerObj is int layerIndex)
                {
                    newLayer = layerIndex;
                }
                else if (layerObj is double layerDouble)
                {
                    newLayer = (int)layerDouble;
                }
                else
                {
                    throw new InvalidOperationException("layer must be a string (layer name) or integer (layer index)");
                }
            }
            else
            {
                throw new InvalidOperationException("layer is required");
            }

            var oldLayer = target.layer;
            var oldLayerName = LayerMask.LayerToName(oldLayer);

            target.layer = newLayer;
            EditorUtility.SetDirty(target);

            return new Dictionary<string, object>
            {
                ["gameObjectPath"] = path,
                ["oldLayer"] = oldLayer,
                ["oldLayerName"] = oldLayerName,
                ["newLayer"] = newLayer,
                ["newLayerName"] = LayerMask.LayerToName(newLayer),
                ["operation"] = "setLayer",
            };
        }

        /// <summary>
        /// Gets the layer of a GameObject.
        /// </summary>
        private static object GetLayer(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var target = ResolveGameObject(path);

            return new Dictionary<string, object>
            {
                ["gameObjectPath"] = path,
                ["layer"] = target.layer,
                ["layerName"] = LayerMask.LayerToName(target.layer),
                ["operation"] = "getLayer",
            };
        }

        /// <summary>
        /// Sets the layer of a GameObject and all its children recursively.
        /// </summary>
        private static object SetLayerRecursive(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var target = ResolveGameObject(path);

            int newLayer;
            if (payload.TryGetValue("layer", out var layerObj))
            {
                if (layerObj is string layerName)
                {
                    newLayer = LayerMask.NameToLayer(layerName);
                    if (newLayer == -1)
                    {
                        throw new InvalidOperationException($"Layer '{layerName}' does not exist in the project. Use addLayer operation to create it first.");
                    }
                }
                else if (layerObj is int layerIndex)
                {
                    newLayer = layerIndex;
                }
                else if (layerObj is double layerDouble)
                {
                    newLayer = (int)layerDouble;
                }
                else
                {
                    throw new InvalidOperationException("layer must be a string (layer name) or integer (layer index)");
                }
            }
            else
            {
                throw new InvalidOperationException("layer is required");
            }

            var affectedCount = 0;
            SetLayerRecursiveInternal(target, newLayer, ref affectedCount);

            return new Dictionary<string, object>
            {
                ["gameObjectPath"] = path,
                ["newLayer"] = newLayer,
                ["newLayerName"] = LayerMask.LayerToName(newLayer),
                ["affectedCount"] = affectedCount,
                ["operation"] = "setLayerRecursive",
            };
        }

        /// <summary>
        /// Internal helper for recursive layer setting.
        /// </summary>
        private static void SetLayerRecursiveInternal(GameObject obj, int layer, ref int count)
        {
            obj.layer = layer;
            EditorUtility.SetDirty(obj);
            count++;

            foreach (Transform child in obj.transform)
            {
                SetLayerRecursiveInternal(child.gameObject, layer, ref count);
            }
        }

        /// <summary>
        /// Lists all tags in the project.
        /// </summary>
        private static object ListTags()
        {
            var tags = UnityEditorInternal.InternalEditorUtility.tags;

            return new Dictionary<string, object>
            {
                ["tags"] = new List<string>(tags),
                ["count"] = tags.Length,
                ["operation"] = "listTags",
            };
        }

        /// <summary>
        /// Adds a new tag to the project.
        /// </summary>
        private static object AddTag(Dictionary<string, object> payload)
        {
            var tag = EnsureValue(GetString(payload, "tag"), "tag");

            // Check if tag already exists
            var existingTags = UnityEditorInternal.InternalEditorUtility.tags;
            if (System.Array.IndexOf(existingTags, tag) != -1)
            {
                return new Dictionary<string, object>
                {
                    ["tag"] = tag,
                    ["added"] = false,
                    ["message"] = "Tag already exists",
                    ["operation"] = "addTag",
                };
            }

            // Add tag using SerializedObject
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProp = tagManager.FindProperty("tags");

            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            var newTagProp = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
            newTagProp.stringValue = tag;

            tagManager.ApplyModifiedProperties();

            return new Dictionary<string, object>
            {
                ["tag"] = tag,
                ["added"] = true,
                ["operation"] = "addTag",
            };
        }

        /// <summary>
        /// Removes a tag from the project.
        /// </summary>
        private static object RemoveTag(Dictionary<string, object> payload)
        {
            var tag = EnsureValue(GetString(payload, "tag"), "tag");

            // Find tag index
            var existingTags = UnityEditorInternal.InternalEditorUtility.tags;
            var tagIndex = System.Array.IndexOf(existingTags, tag);

            if (tagIndex == -1)
            {
                return new Dictionary<string, object>
                {
                    ["tag"] = tag,
                    ["removed"] = false,
                    ["message"] = "Tag does not exist",
                    ["operation"] = "removeTag",
                };
            }

            // Remove tag using SerializedObject
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProp = tagManager.FindProperty("tags");

            // Find the property index (it may not match array index due to built-in tags)
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    tagsProp.DeleteArrayElementAtIndex(i);
                    tagManager.ApplyModifiedProperties();

                    return new Dictionary<string, object>
                    {
                        ["tag"] = tag,
                        ["removed"] = true,
                        ["operation"] = "removeTag",
                    };
                }
            }

            return new Dictionary<string, object>
            {
                ["tag"] = tag,
                ["removed"] = false,
                ["message"] = "Failed to find tag in TagManager",
                ["operation"] = "removeTag",
            };
        }

        /// <summary>
        /// Lists all layers in the project.
        /// </summary>
        private static object ListLayers()
        {
            var layers = new List<Dictionary<string, object>>();

            for (int i = 0; i < 32; i++)
            {
                var layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    layers.Add(new Dictionary<string, object>
                    {
                        ["index"] = i,
                        ["name"] = layerName,
                    });
                }
            }

            return new Dictionary<string, object>
            {
                ["layers"] = layers,
                ["count"] = layers.Count,
                ["operation"] = "listLayers",
            };
        }

        /// <summary>
        /// Adds a new layer to the project.
        /// </summary>
        private static object AddLayer(Dictionary<string, object> payload)
        {
            var layer = EnsureValue(GetString(payload, "layer"), "layer");

            // Check if layer already exists
            for (int i = 0; i < 32; i++)
            {
                if (LayerMask.LayerToName(i) == layer)
                {
                    return new Dictionary<string, object>
                    {
                        ["layer"] = layer,
                        ["index"] = i,
                        ["added"] = false,
                        ["message"] = "Layer already exists",
                        ["operation"] = "addLayer",
                    };
                }
            }

            // Find first available layer slot (8-31, 0-7 are built-in)
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layersProp = tagManager.FindProperty("layers");

            int availableIndex = -1;
            for (int i = 8; i < 32; i++)
            {
                var layerProp = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layerProp.stringValue))
                {
                    availableIndex = i;
                    break;
                }
            }

            if (availableIndex == -1)
            {
                return new Dictionary<string, object>
                {
                    ["layer"] = layer,
                    ["added"] = false,
                    ["message"] = "No available layer slots (layers 8-31 are full)",
                    ["operation"] = "addLayer",
                };
            }

            var newLayerProp = layersProp.GetArrayElementAtIndex(availableIndex);
            newLayerProp.stringValue = layer;
            tagManager.ApplyModifiedProperties();

            return new Dictionary<string, object>
            {
                ["layer"] = layer,
                ["index"] = availableIndex,
                ["added"] = true,
                ["operation"] = "addLayer",
            };
        }

        /// <summary>
        /// Removes a layer from the project.
        /// </summary>
        private static object RemoveLayer(Dictionary<string, object> payload)
        {
            var layer = EnsureValue(GetString(payload, "layer"), "layer");

            // Find layer index
            int layerIndex = -1;
            for (int i = 0; i < 32; i++)
            {
                if (LayerMask.LayerToName(i) == layer)
                {
                    layerIndex = i;
                    break;
                }
            }

            if (layerIndex == -1)
            {
                return new Dictionary<string, object>
                {
                    ["layer"] = layer,
                    ["removed"] = false,
                    ["message"] = "Layer does not exist",
                    ["operation"] = "removeLayer",
                };
            }

            // Cannot remove built-in layers (0-7)
            if (layerIndex < 8)
            {
                return new Dictionary<string, object>
                {
                    ["layer"] = layer,
                    ["index"] = layerIndex,
                    ["removed"] = false,
                    ["message"] = "Cannot remove built-in layers (0-7)",
                    ["operation"] = "removeLayer",
                };
            }

            // Remove layer using SerializedObject
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layersProp = tagManager.FindProperty("layers");

            var layerProp = layersProp.GetArrayElementAtIndex(layerIndex);
            layerProp.stringValue = "";
            tagManager.ApplyModifiedProperties();

            return new Dictionary<string, object>
            {
                ["layer"] = layer,
                ["index"] = layerIndex,
                ["removed"] = true,
                ["operation"] = "removeLayer",
            };
        }

        /// <summary>
        /// Gets a float value from payload, handling both direct float and nested dictionary cases.
        /// </summary>
        private static float? GetFloat(Dictionary<string, object> payload, string key)
        {
            if (!payload.TryGetValue(key, out var value) || value == null)
            {
                return null;
            }

            if (value is double d)
            {
                return (float)d;
            }
            if (value is float f)
            {
                return f;
            }
            if (value is int i)
            {
                return i;
            }
            if (value is string s && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private static object HandlePrefabManage(Dictionary<string, object> payload)
        {
            var operation = EnsureValue(GetString(payload, "operation"), "operation");
            return operation switch
            {
                "create" => CreatePrefab(payload),
                "update" => UpdatePrefab(payload),
                "inspect" => InspectPrefab(payload),
                "instantiate" => InstantiatePrefab(payload),
                "unpack" => UnpackPrefab(payload),
                "applyOverrides" => ApplyPrefabOverrides(payload),
                "revertOverrides" => RevertPrefabOverrides(payload),
                _ => throw new InvalidOperationException($"Unknown prefabManage operation: {operation}"),
            };
        }

        private static object CreatePrefab(Dictionary<string, object> payload)
        {
            var gameObjectPath = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var prefabPath = EnsureValue(GetString(payload, "prefabPath"), "prefabPath");

            var gameObject = ResolveGameObject(gameObjectPath);
            EnsureDirectoryExists(prefabPath);

            var prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Failed to create prefab at {prefabPath}");
            }

            AssetDatabase.Refresh();
            return new Dictionary<string, object>
            {
                ["prefabPath"] = prefabPath,
                ["guid"] = AssetDatabase.AssetPathToGUID(prefabPath),
                ["sourceGameObject"] = gameObjectPath,
            };
        }

        private static object UpdatePrefab(Dictionary<string, object> payload)
        {
            var gameObjectPath = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var prefabPath = EnsureValue(GetString(payload, "prefabPath"), "prefabPath");

            var gameObject = ResolveGameObject(gameObjectPath);

            // Check if the GameObject is a prefab instance
            if (!PrefabUtility.IsPartOfPrefabInstance(gameObject))
            {
                throw new InvalidOperationException($"GameObject at {gameObjectPath} is not a prefab instance");
            }

            var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
            var correspondingPrefab = PrefabUtility.GetCorrespondingObjectFromSource(prefabRoot);

            if (correspondingPrefab == null)
            {
                throw new InvalidOperationException($"Could not find corresponding prefab for {gameObjectPath}");
            }

            var actualPrefabPath = AssetDatabase.GetAssetPath(correspondingPrefab);

            // Save the prefab instance modifications to the prefab asset
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, actualPrefabPath);
            AssetDatabase.Refresh();

            return new Dictionary<string, object>
            {
                ["prefabPath"] = actualPrefabPath,
                ["guid"] = AssetDatabase.AssetPathToGUID(actualPrefabPath),
                ["updatedFrom"] = gameObjectPath,
            };
        }

        private static object InspectPrefab(Dictionary<string, object> payload)
        {
            var prefabPath = EnsureValue(GetString(payload, "prefabPath"), "prefabPath");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Prefab not found at {prefabPath}");
            }

            var result = new Dictionary<string, object>
            {
                ["prefabPath"] = prefabPath,
                ["guid"] = AssetDatabase.AssetPathToGUID(prefabPath),
                ["name"] = prefab.name,
                ["isPrefabAsset"] = PrefabUtility.IsPartOfPrefabAsset(prefab),
            };

            // Get components on the root GameObject
            var components = new List<Dictionary<string, object>>();
            foreach (var component in prefab.GetComponents<Component>())
            {
                if (component == null) continue;
                components.Add(new Dictionary<string, object>
                {
                    ["type"] = component.GetType().FullName,
                    ["typeName"] = component.GetType().Name,
                });
            }
            result["components"] = components;

            // Count children
            result["childCount"] = prefab.transform.childCount;

            return result;
        }

        private static object InstantiatePrefab(Dictionary<string, object> payload)
        {
            var prefabPath = EnsureValue(GetString(payload, "prefabPath"), "prefabPath");
            var parentPath = GetString(payload, "parentPath");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Prefab not found at {prefabPath}");
            }

            GameObject parent = null;
            if (!string.IsNullOrEmpty(parentPath))
            {
                parent = ResolveGameObject(parentPath);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (parent != null)
            {
                instance.transform.SetParent(parent.transform, worldPositionStays: false);
            }

            Selection.activeGameObject = instance;

            return new Dictionary<string, object>
            {
                ["instancePath"] = GetHierarchyPath(instance),
                ["instanceId"] = instance.GetInstanceID(),
                ["prefabPath"] = prefabPath,
                ["isPrefabInstance"] = PrefabUtility.IsPartOfPrefabInstance(instance),
            };
        }

        private static object UnpackPrefab(Dictionary<string, object> payload)
        {
            var gameObjectPath = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var unpackModeStr = GetString(payload, "unpackMode") ?? "OutermostRoot";

            var gameObject = ResolveGameObject(gameObjectPath);

            if (!PrefabUtility.IsPartOfPrefabInstance(gameObject))
            {
                throw new InvalidOperationException($"GameObject at {gameObjectPath} is not a prefab instance");
            }

            var unpackMode = unpackModeStr switch
            {
                "Completely" => PrefabUnpackMode.Completely,
                "OutermostRoot" => PrefabUnpackMode.OutermostRoot,
                _ => PrefabUnpackMode.OutermostRoot,
            };

            PrefabUtility.UnpackPrefabInstance(gameObject, unpackMode, InteractionMode.AutomatedAction);

            return new Dictionary<string, object>
            {
                ["gameObjectPath"] = gameObjectPath,
                ["unpackMode"] = unpackModeStr,
                ["isPrefabInstance"] = PrefabUtility.IsPartOfPrefabInstance(gameObject),
            };
        }

        private static object ApplyPrefabOverrides(Dictionary<string, object> payload)
        {
            var gameObjectPath = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var gameObject = ResolveGameObject(gameObjectPath);

            if (!PrefabUtility.IsPartOfPrefabInstance(gameObject))
            {
                throw new InvalidOperationException($"GameObject at {gameObjectPath} is not a prefab instance");
            }

            var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
            var correspondingPrefab = PrefabUtility.GetCorrespondingObjectFromSource(prefabRoot);

            if (correspondingPrefab == null)
            {
                throw new InvalidOperationException($"Could not find corresponding prefab for {gameObjectPath}");
            }

            var prefabPath = AssetDatabase.GetAssetPath(correspondingPrefab);

            // Apply all overrides from the instance to the prefab
            PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.AutomatedAction);
            AssetDatabase.Refresh();

            return new Dictionary<string, object>
            {
                ["gameObjectPath"] = gameObjectPath,
                ["prefabPath"] = prefabPath,
                ["message"] = "Overrides applied to prefab",
            };
        }

        private static object RevertPrefabOverrides(Dictionary<string, object> payload)
        {
            var gameObjectPath = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var gameObject = ResolveGameObject(gameObjectPath);

            if (!PrefabUtility.IsPartOfPrefabInstance(gameObject))
            {
                throw new InvalidOperationException($"GameObject at {gameObjectPath} is not a prefab instance");
            }

            var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);

            // Revert all overrides on the instance to match the prefab
            PrefabUtility.RevertPrefabInstance(prefabRoot, InteractionMode.AutomatedAction);

            return new Dictionary<string, object>
            {
                ["gameObjectPath"] = gameObjectPath,
                ["message"] = "Overrides reverted to prefab state",
            };
        }

        /// <summary>
        /// Handles project settings management operations (read, write, list).
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation', 'category', 'property', and optional 'value'.</param>
        /// <returns>Result dictionary with operation-specific data.</returns>
        /// <exception cref="InvalidOperationException">Thrown when operation or parameters are invalid.</exception>
        private static object HandleProjectSettingsManage(Dictionary<string, object> payload)
        {
            var operation = EnsureValue(GetString(payload, "operation"), "operation");
            return operation switch
            {
                "read" => ReadProjectSettings(payload),
                "write" => WriteProjectSettings(payload),
                "list" => ListProjectSettings(payload),
                _ => throw new InvalidOperationException($"Unknown projectSettingsManage operation: {operation}"),
            };
        }

        private static object ReadProjectSettings(Dictionary<string, object> payload)
        {
            var category = EnsureValue(GetString(payload, "category"), "category");
            var property = GetString(payload, "property");

            var result = new Dictionary<string, object>
            {
                ["category"] = category,
            };

            switch (category.ToLower())
            {
                case "player":
                    result["settings"] = ReadPlayerSettings(property);
                    break;
                case "quality":
                    result["settings"] = ReadQualitySettings(property);
                    break;
                case "time":
                    result["settings"] = ReadTimeSettings(property);
                    break;
                case "physics":
                    result["settings"] = ReadPhysicsSettings(property);
                    break;
                case "audio":
                    result["settings"] = ReadAudioSettings(property);
                    break;
                case "editor":
                    result["settings"] = ReadEditorSettings(property);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown settings category: {category}");
            }

            return result;
        }

        private static object WriteProjectSettings(Dictionary<string, object> payload)
        {
            var category = EnsureValue(GetString(payload, "category"), "category");
            var property = EnsureValue(GetString(payload, "property"), "property");

            if (!payload.TryGetValue("value", out var value))
            {
                throw new InvalidOperationException("value is required for write operation");
            }

            switch (category.ToLower())
            {
                case "player":
                    WritePlayerSettings(property, value);
                    break;
                case "quality":
                    WriteQualitySettings(property, value);
                    break;
                case "time":
                    WriteTimeSettings(property, value);
                    break;
                case "physics":
                    WritePhysicsSettings(property, value);
                    break;
                case "audio":
                    WriteAudioSettings(property, value);
                    break;
                case "editor":
                    WriteEditorSettings(property, value);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown settings category: {category}");
            }

            return new Dictionary<string, object>
            {
                ["category"] = category,
                ["property"] = property,
                ["value"] = value,
                ["message"] = "Settings updated successfully",
            };
        }

        private static object ListProjectSettings(Dictionary<string, object> payload)
        {
            var category = GetString(payload, "category");

            if (string.IsNullOrEmpty(category))
            {
                // Return all available categories
                return new Dictionary<string, object>
                {
                    ["categories"] = new List<string>
                    {
                        "player",
                        "quality",
                        "time",
                        "physics",
                        "audio",
                        "editor",
                    },
                };
            }

            // Return available properties for the specified category
            var properties = category.ToLower() switch
            {
                "player" => new List<string>
                {
                    "companyName", "productName", "version", "bundleVersion",
                    "defaultScreenWidth", "defaultScreenHeight", "runInBackground",
                    "displayResolutionDialog", "defaultIsFullScreen", "defaultIsNativeResolution",
                    "allowFullscreenSwitch", "captureSingleScreen", "resizableWindow",
                },
                "quality" => new List<string>
                {
                    "names", "currentLevel", "pixelLightCount", "shadowDistance",
                    "shadowResolution", "shadowProjection", "shadowCascades", "vSyncCount",
                    "antiAliasing", "softParticles", "realtimeReflectionProbes",
                },
                "time" => new List<string>
                {
                    "fixedDeltaTime", "maximumDeltaTime", "timeScale", "maximumParticleDeltaTime",
                    "captureDeltaTime",
                },
                "physics" => new List<string>
                {
                    "gravity", "defaultSolverIterations", "defaultSolverVelocityIterations",
                    "bounceThreshold", "sleepThreshold", "defaultContactOffset",
                    "queriesHitTriggers", "queriesHitBackfaces", "autoSimulation",
                },
                "audio" => new List<string>
                {
                    "dspBufferSize", "sampleRate", "speakerMode", "numRealVoices",
                    "numVirtualVoices",
                },
                "editor" => new List<string>
                {
                    "serializationMode", "spritePackerMode", "etcTextureCompressorBehavior",
                    "lineEndingsForNewScripts", "defaultBehaviorMode", "prefabRegularEnvironment",
                },
                _ => throw new InvalidOperationException($"Unknown settings category: {category}"),
            };

            return new Dictionary<string, object>
            {
                ["category"] = category,
                ["properties"] = properties,
            };
        }

        // PlayerSettings read/write methods
        private static object ReadPlayerSettings(string property)
        {
            if (string.IsNullOrEmpty(property))
            {
                return new Dictionary<string, object>
                {
                    ["companyName"] = PlayerSettings.companyName,
                    ["productName"] = PlayerSettings.productName,
                    ["version"] = PlayerSettings.bundleVersion,
                    ["defaultScreenWidth"] = PlayerSettings.defaultScreenWidth,
                    ["defaultScreenHeight"] = PlayerSettings.defaultScreenHeight,
                    ["runInBackground"] = PlayerSettings.runInBackground,
                };
            }

            return property.ToLower() switch
            {
                "companyname" => PlayerSettings.companyName,
                "productname" => PlayerSettings.productName,
                "version" or "bundleversion" => PlayerSettings.bundleVersion,
                "defaultscreenwidth" => PlayerSettings.defaultScreenWidth,
                "defaultscreenheight" => PlayerSettings.defaultScreenHeight,
                "runinbackground" => PlayerSettings.runInBackground,
                "fullscreenmode" => PlayerSettings.fullScreenMode.ToString(),
                "defaultisnativeresolution" => PlayerSettings.defaultIsNativeResolution,
                "allowfullscreenswitch" => PlayerSettings.allowFullscreenSwitch,
                "resizablewindow" => PlayerSettings.resizableWindow,
                _ => throw new InvalidOperationException($"Unknown PlayerSettings property: {property}"),
            };
        }

        private static void WritePlayerSettings(string property, object value)
        {
            switch (property.ToLower())
            {
                case "companyname":
                    PlayerSettings.companyName = value.ToString();
                    break;
                case "productname":
                    PlayerSettings.productName = value.ToString();
                    break;
                case "version":
                case "bundleversion":
                    PlayerSettings.bundleVersion = value.ToString();
                    break;
                case "defaultscreenwidth":
                    PlayerSettings.defaultScreenWidth = Convert.ToInt32(value);
                    break;
                case "defaultscreenheight":
                    PlayerSettings.defaultScreenHeight = Convert.ToInt32(value);
                    break;
                case "runinbackground":
                    PlayerSettings.runInBackground = Convert.ToBoolean(value);
                    break;
                case "fullscreenmode":
                    if (Enum.TryParse<FullScreenMode>(value.ToString(), true, out var mode))
                    {
                        PlayerSettings.fullScreenMode = mode;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Invalid FullScreenMode value: {value}");
                    }
                    break;
                case "defaultisnativeresolution":
                    PlayerSettings.defaultIsNativeResolution = Convert.ToBoolean(value);
                    break;
                case "allowfullscreenswitch":
                    PlayerSettings.allowFullscreenSwitch = Convert.ToBoolean(value);
                    break;
                case "resizablewindow":
                    PlayerSettings.resizableWindow = Convert.ToBoolean(value);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown or readonly PlayerSettings property: {property}");
            }
        }

        // QualitySettings read/write methods
        private static object ReadQualitySettings(string property)
        {
            if (string.IsNullOrEmpty(property))
            {
                return new Dictionary<string, object>
                {
                    ["names"] = QualitySettings.names,
                    ["currentLevel"] = QualitySettings.GetQualityLevel(),
                    ["pixelLightCount"] = QualitySettings.pixelLightCount,
                    ["shadowDistance"] = QualitySettings.shadowDistance,
                    ["vSyncCount"] = QualitySettings.vSyncCount,
                    ["antiAliasing"] = QualitySettings.antiAliasing,
                };
            }

            return property.ToLower() switch
            {
                "names" => QualitySettings.names,
                "currentlevel" => QualitySettings.GetQualityLevel(),
                "pixellightcount" => QualitySettings.pixelLightCount,
                "shadowdistance" => QualitySettings.shadowDistance,
                "shadowresolution" => QualitySettings.shadowResolution.ToString(),
                "shadowprojection" => QualitySettings.shadowProjection.ToString(),
                "shadowcascades" => QualitySettings.shadowCascades,
                "vsynccount" => QualitySettings.vSyncCount,
                "antialiasing" => QualitySettings.antiAliasing,
                "softparticles" => QualitySettings.softParticles,
                "realtimereflectionprobes" => QualitySettings.realtimeReflectionProbes,
                _ => throw new InvalidOperationException($"Unknown QualitySettings property: {property}"),
            };
        }

        private static void WriteQualitySettings(string property, object value)
        {
            switch (property.ToLower())
            {
                case "currentlevel":
                    QualitySettings.SetQualityLevel(Convert.ToInt32(value));
                    break;
                case "pixellightcount":
                    QualitySettings.pixelLightCount = Convert.ToInt32(value);
                    break;
                case "shadowdistance":
                    QualitySettings.shadowDistance = Convert.ToSingle(value);
                    break;
                case "shadowcascades":
                    QualitySettings.shadowCascades = Convert.ToInt32(value);
                    break;
                case "vsynccount":
                    QualitySettings.vSyncCount = Convert.ToInt32(value);
                    break;
                case "antialiasing":
                    QualitySettings.antiAliasing = Convert.ToInt32(value);
                    break;
                case "softparticles":
                    QualitySettings.softParticles = Convert.ToBoolean(value);
                    break;
                case "realtimereflectionprobes":
                    QualitySettings.realtimeReflectionProbes = Convert.ToBoolean(value);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown or readonly QualitySettings property: {property}");
            }
        }

        // TimeSettings read/write methods
        private static object ReadTimeSettings(string property)
        {
            if (string.IsNullOrEmpty(property))
            {
                return new Dictionary<string, object>
                {
                    ["fixedDeltaTime"] = Time.fixedDeltaTime,
                    ["maximumDeltaTime"] = Time.maximumDeltaTime,
                    ["timeScale"] = Time.timeScale,
                    ["maximumParticleDeltaTime"] = Time.maximumParticleDeltaTime,
                };
            }

            return property.ToLower() switch
            {
                "fixeddeltatime" => Time.fixedDeltaTime,
                "maximumdeltatime" => Time.maximumDeltaTime,
                "timescale" => Time.timeScale,
                "maximumparticledeltatime" => Time.maximumParticleDeltaTime,
                "capturedeltatime" => Time.captureDeltaTime,
                _ => throw new InvalidOperationException($"Unknown TimeSettings property: {property}"),
            };
        }

        private static void WriteTimeSettings(string property, object value)
        {
            switch (property.ToLower())
            {
                case "fixeddeltatime":
                    Time.fixedDeltaTime = Convert.ToSingle(value);
                    break;
                case "maximumdeltatime":
                    Time.maximumDeltaTime = Convert.ToSingle(value);
                    break;
                case "timescale":
                    Time.timeScale = Convert.ToSingle(value);
                    break;
                case "maximumparticledeltatime":
                    Time.maximumParticleDeltaTime = Convert.ToSingle(value);
                    break;
                case "capturedeltatime":
                    Time.captureDeltaTime = Convert.ToSingle(value);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown TimeSettings property: {property}");
            }
        }

        // PhysicsSettings read/write methods
        private static object ReadPhysicsSettings(string property)
        {
            if (string.IsNullOrEmpty(property))
            {
                return new Dictionary<string, object>
                {
                    ["gravity"] = new Dictionary<string, object>
                    {
                        ["x"] = Physics.gravity.x,
                        ["y"] = Physics.gravity.y,
                        ["z"] = Physics.gravity.z,
                    },
                    ["defaultSolverIterations"] = Physics.defaultSolverIterations,
                    ["defaultSolverVelocityIterations"] = Physics.defaultSolverVelocityIterations,
                    ["bounceThreshold"] = Physics.bounceThreshold,
                    ["sleepThreshold"] = Physics.sleepThreshold,
                    ["queriesHitTriggers"] = Physics.queriesHitTriggers,
                };
            }

            return property.ToLower() switch
            {
                "gravity" => new Dictionary<string, object>
                {
                    ["x"] = Physics.gravity.x,
                    ["y"] = Physics.gravity.y,
                    ["z"] = Physics.gravity.z,
                },
                "defaultsolveriterations" => Physics.defaultSolverIterations,
                "defaultsolvervelocityiterations" => Physics.defaultSolverVelocityIterations,
                "bouncethreshold" => Physics.bounceThreshold,
                "sleepthreshold" => Physics.sleepThreshold,
                "defaultcontactoffset" => Physics.defaultContactOffset,
                "querieshittriggers" => Physics.queriesHitTriggers,
                "querieshitbackfaces" => Physics.queriesHitBackfaces,
                "simulationmode" => Physics.simulationMode.ToString(),
                _ => throw new InvalidOperationException($"Unknown PhysicsSettings property: {property}"),
            };
        }

        private static void WritePhysicsSettings(string property, object value)
        {
            switch (property.ToLower())
            {
                case "gravity":
                    if (value is Dictionary<string, object> gravityDict)
                    {
                        Physics.gravity = new Vector3(
                            Convert.ToSingle(gravityDict.GetValueOrDefault("x", 0f)),
                            Convert.ToSingle(gravityDict.GetValueOrDefault("y", -9.81f)),
                            Convert.ToSingle(gravityDict.GetValueOrDefault("z", 0f))
                        );
                    }
                    break;
                case "defaultsolveriterations":
                    Physics.defaultSolverIterations = Convert.ToInt32(value);
                    break;
                case "defaultsolvervelocityiterations":
                    Physics.defaultSolverVelocityIterations = Convert.ToInt32(value);
                    break;
                case "bouncethreshold":
                    Physics.bounceThreshold = Convert.ToSingle(value);
                    break;
                case "sleepthreshold":
                    Physics.sleepThreshold = Convert.ToSingle(value);
                    break;
                case "defaultcontactoffset":
                    Physics.defaultContactOffset = Convert.ToSingle(value);
                    break;
                case "querieshittriggers":
                    Physics.queriesHitTriggers = Convert.ToBoolean(value);
                    break;
                case "querieshitbackfaces":
                    Physics.queriesHitBackfaces = Convert.ToBoolean(value);
                    break;
                case "simulationmode":
                    if (Enum.TryParse<SimulationMode>(value.ToString(), true, out var simMode))
                    {
                        Physics.simulationMode = simMode;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Invalid SimulationMode value: {value}");
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unknown PhysicsSettings property: {property}");
            }
        }

        // AudioSettings read/write methods
        private static object ReadAudioSettings(string property)
        {
            if (string.IsNullOrEmpty(property))
            {
                var config = AudioSettings.GetConfiguration();
                return new Dictionary<string, object>
                {
                    ["dspBufferSize"] = config.dspBufferSize,
                    ["sampleRate"] = config.sampleRate,
                    ["speakerMode"] = config.speakerMode.ToString(),
                    ["numRealVoices"] = config.numRealVoices,
                    ["numVirtualVoices"] = config.numVirtualVoices,
                };
            }

            var audioConfig = AudioSettings.GetConfiguration();
            return property.ToLower() switch
            {
                "dspbuffersize" => audioConfig.dspBufferSize,
                "samplerate" => audioConfig.sampleRate,
                "speakermode" => audioConfig.speakerMode.ToString(),
                "numrealvoices" => audioConfig.numRealVoices,
                "numvirtualvoices" => audioConfig.numVirtualVoices,
                _ => throw new InvalidOperationException($"Unknown AudioSettings property: {property}"),
            };
        }

        private static void WriteAudioSettings(string property, object value)
        {
            var config = AudioSettings.GetConfiguration();
            var modified = false;

            switch (property.ToLower())
            {
                case "dspbuffersize":
                    config.dspBufferSize = Convert.ToInt32(value);
                    modified = true;
                    break;
                case "samplerate":
                    config.sampleRate = Convert.ToInt32(value);
                    modified = true;
                    break;
                case "speakermode":
                    if (Enum.TryParse<AudioSpeakerMode>(value.ToString(), out var mode))
                    {
                        config.speakerMode = mode;
                        modified = true;
                    }
                    break;
                case "numrealvoices":
                    config.numRealVoices = Convert.ToInt32(value);
                    modified = true;
                    break;
                case "numvirtualvoices":
                    config.numVirtualVoices = Convert.ToInt32(value);
                    modified = true;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown AudioSettings property: {property}");
            }

            if (modified)
            {
                AudioSettings.Reset(config);
            }
        }

        // EditorSettings read/write methods
        private static object ReadEditorSettings(string property)
        {
            if (string.IsNullOrEmpty(property))
            {
                return new Dictionary<string, object>
                {
                    ["serializationMode"] = EditorSettings.serializationMode.ToString(),
                    ["spritePackerMode"] = EditorSettings.spritePackerMode.ToString(),
                    ["lineEndingsForNewScripts"] = EditorSettings.lineEndingsForNewScripts.ToString(),
                    ["defaultBehaviorMode"] = EditorSettings.defaultBehaviorMode.ToString(),
                };
            }

            return property.ToLower() switch
            {
                "serializationmode" => EditorSettings.serializationMode.ToString(),
                "spritepackermode" => EditorSettings.spritePackerMode.ToString(),
                "lineendingsfornewscripts" => EditorSettings.lineEndingsForNewScripts.ToString(),
                "defaultbehaviormode" => EditorSettings.defaultBehaviorMode.ToString(),
                "prefabregularenvironment" => EditorSettings.prefabRegularEnvironment?.name,
                _ => throw new InvalidOperationException($"Unknown EditorSettings property: {property}"),
            };
        }

        private static void WriteEditorSettings(string property, object value)
        {
            switch (property.ToLower())
            {
                case "serializationmode":
                    if (Enum.TryParse<SerializationMode>(value.ToString(), out var serMode))
                    {
                        EditorSettings.serializationMode = serMode;
                    }
                    break;
                case "spritepackermode":
                    if (Enum.TryParse<SpritePackerMode>(value.ToString(), out var spriteMode))
                    {
                        EditorSettings.spritePackerMode = spriteMode;
                    }
                    break;
                case "lineendingsfornewscripts":
                    if (Enum.TryParse<LineEndingsMode>(value.ToString(), out var lineMode))
                    {
                        EditorSettings.lineEndingsForNewScripts = lineMode;
                    }
                    break;
                case "defaultbehaviormode":
                    if (Enum.TryParse<EditorBehaviorMode>(value.ToString(), out var behaviorMode))
                    {
                        EditorSettings.defaultBehaviorMode = behaviorMode;
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unknown or readonly EditorSettings property: {property}");
            }
        }

        /// <summary>
        /// Handles render pipeline management operations (inspect, setAsset, getSettings, updateSettings).
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation' and pipeline-specific settings.</param>
        /// <returns>Result dictionary with operation-specific data.</returns>
        private static object HandleRenderPipelineManage(Dictionary<string, object> payload)
        {
            var operation = EnsureValue(GetString(payload, "operation"), "operation");
            return operation switch
            {
                "inspect" => InspectRenderPipeline(),
                "setAsset" => SetRenderPipelineAsset(payload),
                "getSettings" => GetRenderPipelineSettings(payload),
                "updateSettings" => UpdateRenderPipelineSettings(payload),
                _ => throw new InvalidOperationException($"Unknown renderPipelineManage operation: {operation}"),
            };
        }

        private static object InspectRenderPipeline()
        {
            var currentPipeline = GraphicsSettings.currentRenderPipeline;
            var result = new Dictionary<string, object>
            {
                ["hasRenderPipeline"] = currentPipeline != null,
            };

            if (currentPipeline != null)
            {
                result["pipelineName"] = currentPipeline.name;
                result["pipelineType"] = currentPipeline.GetType().FullName;
                result["assetPath"] = AssetDatabase.GetAssetPath(currentPipeline);

                // Detect pipeline type
                var typeName = currentPipeline.GetType().Name;
                if (typeName.Contains("Universal") || typeName.Contains("URP"))
                {
                    result["pipelineKind"] = "URP";
                }
                else if (typeName.Contains("HDRenderPipeline") || typeName.Contains("HDRP"))
                {
                    result["pipelineKind"] = "HDRP";
                }
                else
                {
                    result["pipelineKind"] = "Custom";
                }
            }
            else
            {
                result["pipelineKind"] = "BuiltIn";
            }

            return result;
        }

        private static object SetRenderPipelineAsset(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");

            if (string.IsNullOrEmpty(assetPath))
            {
                // Clear render pipeline (set to built-in)
                GraphicsSettings.defaultRenderPipeline = null;
                return new Dictionary<string, object>
                {
                    ["message"] = "Render pipeline cleared (using Built-in)",
                    ["pipelineKind"] = "BuiltIn",
                };
            }

            var asset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"RenderPipelineAsset not found at path: {assetPath}");
            }

            GraphicsSettings.defaultRenderPipeline = asset;

            return new Dictionary<string, object>
            {
                ["message"] = "Render pipeline asset set successfully",
                ["assetPath"] = assetPath,
                ["pipelineName"] = asset.name,
                ["pipelineType"] = asset.GetType().FullName,
            };
        }

        private static object GetRenderPipelineSettings(Dictionary<string, object> payload)
        {
            var currentPipeline = GraphicsSettings.currentRenderPipeline;
            if (currentPipeline == null)
            {
                throw new InvalidOperationException("No render pipeline is currently active. Using Built-in renderer.");
            }

            var result = new Dictionary<string, object>
            {
                ["pipelineName"] = currentPipeline.name,
                ["pipelineType"] = currentPipeline.GetType().FullName,
            };

            // Use reflection to get common properties
            var pipelineType = currentPipeline.GetType();
            var properties = new Dictionary<string, object>();

            foreach (var prop in pipelineType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead) continue;

                try
                {
                    var value = prop.GetValue(currentPipeline);
                    properties[prop.Name] = SerializeValue(value);
                }
                catch (Exception ex)
                {
                    properties[prop.Name] = $"<Error: {ex.Message}>";
                }
            }

            result["properties"] = properties;
            return result;
        }

        private static object UpdateRenderPipelineSettings(Dictionary<string, object> payload)
        {
            var currentPipeline = GraphicsSettings.currentRenderPipeline;
            if (currentPipeline == null)
            {
                throw new InvalidOperationException("No render pipeline is currently active.");
            }

            if (!payload.TryGetValue("settings", out var settingsObj) || !(settingsObj is Dictionary<string, object> settings))
            {
                throw new InvalidOperationException("settings dictionary is required");
            }

            var pipelineType = currentPipeline.GetType();
            var updatedProperties = new List<string>();

            foreach (var kvp in settings)
            {
                var prop = pipelineType.GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.Instance);
                if (prop != null && prop.CanWrite)
                {
                    try
                    {
                        var converted = ConvertValue(kvp.Value, prop.PropertyType);
                        prop.SetValue(currentPipeline, converted);
                        updatedProperties.Add(kvp.Key);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failed to set property {kvp.Key}: {ex.Message}");
                    }
                }
            }

            EditorUtility.SetDirty(currentPipeline);
            AssetDatabase.SaveAssets();

            return new Dictionary<string, object>
            {
                ["message"] = "Render pipeline settings updated",
                ["updatedProperties"] = updatedProperties,
            };
        }

        private static int GetInt(Dictionary<string, object> payload, string key, int defaultValue)
        {
            if (!payload.TryGetValue(key, out var value) || value == null)
            {
                return defaultValue;
            }

            if (value is int intValue)
            {
                return intValue;
            }

            if (value is long longValue)
            {
                return (int)longValue;
            }

            if (value is double doubleValue)
            {
                return (int)doubleValue;
            }

            if (value is string strValue && int.TryParse(strValue, out var parsed))
            {
                return parsed;
            }

            return defaultValue;
        }

        private static float GetFloat(Dictionary<string, object> payload, string key, float defaultValue)
        {
            if (!payload.TryGetValue(key, out var value) || value == null)
            {
                return defaultValue;
            }

            if (value is float floatValue)
            {
                return floatValue;
            }

            if (value is double doubleValue)
            {
                return (float)doubleValue;
            }

            if (value is int intValue)
            {
                return (float)intValue;
            }

            if (value is long longValue)
            {
                return (float)longValue;
            }

            if (value is string strValue && float.TryParse(strValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return defaultValue;
        }

        private static List<object> GetList(Dictionary<string, object> payload, string key)
        {
            if (!payload.TryGetValue(key, out var value) || value == null)
            {
                return null;
            }

            return value as List<object>;
        }

        #region Project Compile

        /// <summary>
        /// Gets compilation result by checking for compilation errors.
        /// This is called after compilation completes to check if there were any errors.
        /// Uses enhanced console log parsing for more accurate error and warning detection.
        /// </summary>
        /// <returns>Dictionary with compilation result including success status and any errors.</returns>
        public static Dictionary<string, object> GetCompilationResult()
        {
            var errorMessages = new List<string>();
            var warningMessages = new List<string>();
            var assemblyInfo = new List<string>();

            // Get assembly information
            try
            {
                var assemblies = CompilationPipeline.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    assemblyInfo.Add($"{assembly.name} ({assembly.sourceFiles.Length} files)");
                }
            }
            catch
            {
                // Ignore if we can't get assembly info
            }

            // Parse console logs for errors and warnings
            // This is the most reliable way to get compilation messages after compilation completes
            var logEntries = GetConsoleLogEntries(limit: 200); // Increased from 100 to 200

            foreach (var entry in logEntries)
            {
                if (!entry.ContainsKey("type") || !entry.ContainsKey("message"))
                {
                    continue;
                }

                var message = entry["message"].ToString();
                var entryType = entry["type"].ToString();

                if (entryType == "Error")
                {
                    // Enhanced error detection patterns
                    if (message.Contains("error CS", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("CompilerError", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("Build failed", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("compilation error", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("error :", StringComparison.OrdinalIgnoreCase))
                    {
                        // Avoid duplicates
                        if (!errorMessages.Contains(message))
                        {
                            errorMessages.Add(message);
                        }
                    }
                }
                else if (entryType == "Warning")
                {
                    // Enhanced warning detection patterns
                    if (message.Contains("warning CS", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("CompilerWarning", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("warning :", StringComparison.OrdinalIgnoreCase))
                    {
                        // Avoid duplicates and limit to 20 warnings
                        if (!warningMessages.Contains(message) && warningMessages.Count < 20)
                        {
                            warningMessages.Add(message);
                        }
                    }
                }
            }

            var hasErrors = errorMessages.Count > 0;
            var result = new Dictionary<string, object>
            {
                ["success"] = !hasErrors,
                ["completed"] = true,
                ["timedOut"] = false,
                ["hasErrors"] = hasErrors,
                ["hasWarnings"] = warningMessages.Count > 0,
                ["errors"] = errorMessages,
                ["warnings"] = warningMessages,
                ["errorCount"] = errorMessages.Count,
                ["warningCount"] = warningMessages.Count,
                ["assemblies"] = assemblyInfo,
                ["message"] = hasErrors
                    ? $"Compilation completed with {errorMessages.Count} error(s)" +
                      (warningMessages.Count > 0 ? $" and {warningMessages.Count} warning(s)" : "")
                    : (warningMessages.Count > 0
                        ? $"Compilation completed successfully with {warningMessages.Count} warning(s)"
                        : "Compilation completed successfully"),
            };

            return result;
        }

        /// <summary>
        /// Gets console log entries for error checking.
        /// </summary>
        /// <param name="limit">Maximum number of log entries to retrieve (default: 100)</param>
        /// <returns>List of log entry dictionaries.</returns>
        private static List<Dictionary<string, object>> GetConsoleLogEntries(int limit = 100)
        {
            var logEntries = new List<Dictionary<string, object>>();

            try
            {
                // Use reflection to access Unity's internal LogEntries
                var logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor");
                if (logEntriesType == null)
                {
                    return logEntries;
                }

                var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
                var startGettingEntriesMethod = logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public);
                var getEntryInternalMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
                var endGettingEntriesMethod = logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public);

                if (getCountMethod == null || startGettingEntriesMethod == null ||
                    getEntryInternalMethod == null || endGettingEntriesMethod == null)
                {
                    return logEntries;
                }

                var count = (int)getCountMethod.Invoke(null, null);
                startGettingEntriesMethod.Invoke(null, null);

                var logEntryType = Type.GetType("UnityEditor.LogEntry, UnityEditor");
                if (logEntryType == null)
                {
                    endGettingEntriesMethod.Invoke(null, null);
                    return logEntries;
                }

                for (int i = 0; i < Math.Min(count, limit); i++) // Use configurable limit
                {
                    var logEntry = Activator.CreateInstance(logEntryType);
                    var parameters = new object[] { i, logEntry };
                    var success = (bool)getEntryInternalMethod.Invoke(null, parameters);

                    if (success)
                    {
                        var messageField = logEntryType.GetField("message");
                        var modeField = logEntryType.GetField("mode");

                        if (messageField != null && modeField != null)
                        {
                            var message = messageField.GetValue(logEntry)?.ToString() ?? "";
                            var mode = (int)modeField.GetValue(logEntry);

                            var entryType = mode switch
                            {
                                0 => "Log",
                                1 => "Warning",
                                2 => "Error",
                                _ => "Unknown"
                            };

                            logEntries.Add(new Dictionary<string, object>
                            {
                                ["message"] = message,
                                ["type"] = entryType,
                            });
                        }
                    }
                }

                endGettingEntriesMethod.Invoke(null, null);
            }
            catch (Exception)
            {
                // Silently fail if we can't access log entries
            }

            return logEntries;
        }

        #endregion

        #region Utility Helper Methods

        /// <summary>
        /// Gets a string value from a dictionary payload.
        /// </summary>
        /// <param name="payload">The payload dictionary.</param>
        /// <param name="key">The key to retrieve.</param>
        /// <returns>The string value, or null if the key doesn't exist or the value is null.</returns>
        private static string GetString(Dictionary<string, object> payload, string key)
        {
            if (payload == null || !payload.ContainsKey(key))
            {
                return null;
            }

            var value = payload[key];
            return value?.ToString();
        }

        /// <summary>
        /// Gets a boolean value from a dictionary payload.
        /// </summary>
        /// <param name="payload">The payload dictionary.</param>
        /// <param name="key">The key to retrieve.</param>
        /// <returns>The boolean value, or false if the key doesn't exist.</returns>
        private static bool GetBool(Dictionary<string, object> payload, string key)
        {
            if (payload == null || !payload.ContainsKey(key))
            {
                return false;
            }

            var value = payload[key];
            if (value is bool boolValue)
            {
                return boolValue;
            }

            if (value is string strValue)
            {
                return bool.TryParse(strValue, out var result) && result;
            }

            return false;
        }

        /// <summary>
        /// Resolves a GameObject by its hierarchy path.
        /// </summary>
        /// <param name="path">The hierarchy path (e.g., "Canvas/Panel/Button").</param>
        /// <returns>The resolved GameObject.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the GameObject is not found.</exception>
        private static GameObject ResolveGameObject(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("GameObject path is required");
            }

            var go = GameObject.Find(path);
            if (go == null)
            {
                throw new InvalidOperationException($"GameObject not found: {path}");
            }

            return go;
        }

        /// <summary>
        /// Gets the full hierarchy path of a GameObject.
        /// </summary>
        /// <param name="go">The GameObject.</param>
        /// <returns>The full hierarchy path (e.g., "Canvas/Panel/Button").</returns>
        private static string GetHierarchyPath(GameObject go)
        {
            if (go == null)
            {
                return string.Empty;
            }

            var path = go.name;
            var parent = go.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        /// <summary>
        /// Serializes a value for JSON output.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>A serialized representation suitable for JSON.</returns>
        private static object SerializeValue(object value)
        {
            return SerializeValue(value, 0, 3, 50);
        }

        /// <summary>
        /// Serializes a value for JSON output with depth and size limits.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="depth">Current recursion depth.</param>
        /// <param name="maxDepth">Maximum recursion depth (default: 3).</param>
        /// <param name="maxItems">Maximum items in collections (default: 50).</param>
        /// <returns>A serialized representation suitable for JSON.</returns>
        private static object SerializeValue(object value, int depth, int maxDepth, int maxItems)
        {
            if (value == null)
            {
                return null;
            }

            // Check depth limit
            if (depth >= maxDepth)
            {
                return $"<MaxDepthReached:{value.GetType().Name}>";
            }

            // Handle Unity Object references
            if (value is UnityEngine.Object unityObj)
            {
                if (unityObj == null) // Unity's null check
                {
                    return null;
                }

                var result = new Dictionary<string, object>
                {
                    ["type"] = value.GetType().FullName,
                    ["name"] = unityObj.name,
                };

                // Add asset path if it's an asset
                var assetPath = AssetDatabase.GetAssetPath(unityObj);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    result["assetPath"] = assetPath;
                    result["guid"] = AssetDatabase.AssetPathToGUID(assetPath);
                }

                // Add instance ID for scene objects
                if (unityObj is GameObject || unityObj is Component)
                {
                    result["instanceID"] = unityObj.GetInstanceID();
                }

                return result;
            }

            // Handle Vector2
            if (value is Vector2 v2)
            {
                return new Dictionary<string, object>
                {
                    ["x"] = v2.x,
                    ["y"] = v2.y,
                };
            }

            // Handle Vector3
            if (value is Vector3 v3)
            {
                return new Dictionary<string, object>
                {
                    ["x"] = v3.x,
                    ["y"] = v3.y,
                    ["z"] = v3.z,
                };
            }

            // Handle Vector4
            if (value is Vector4 v4)
            {
                return new Dictionary<string, object>
                {
                    ["x"] = v4.x,
                    ["y"] = v4.y,
                    ["z"] = v4.z,
                    ["w"] = v4.w,
                };
            }

            // Handle Color
            if (value is Color color)
            {
                return new Dictionary<string, object>
                {
                    ["r"] = color.r,
                    ["g"] = color.g,
                    ["b"] = color.b,
                    ["a"] = color.a,
                };
            }

            // Handle Rect
            if (value is Rect rect)
            {
                return new Dictionary<string, object>
                {
                    ["x"] = rect.x,
                    ["y"] = rect.y,
                    ["width"] = rect.width,
                    ["height"] = rect.height,
                };
            }

            // Handle enums
            if (value.GetType().IsEnum)
            {
                return value.ToString();
            }

            // Handle arrays and lists with size limit
            if (value is IEnumerable enumerable && !(value is string))
            {
                var list = new List<object>();
                int count = 0;
                foreach (var item in enumerable)
                {
                    if (count >= maxItems)
                    {
                        list.Add($"<TruncatedAt{maxItems}Items>");
                        break;
                    }
                    list.Add(SerializeValue(item, depth + 1, maxDepth, maxItems));
                    count++;
                }
                return list;
            }

            // Return primitives and strings as-is
            return value;
        }

        /// <summary>
        /// Resolves a type by its name, searching common Unity namespaces.
        /// </summary>
        /// <param name="typeName">The type name (e.g., "UnityEngine.UI.Button").</param>
        /// <returns>The resolved Type.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the type cannot be resolved.</exception>
        private static Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new InvalidOperationException("Type name is required");
            }

            // Try direct resolution first
            var type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            // Try with common Unity assemblies
            var assemblies = new[]
            {
                "UnityEngine",
                "UnityEngine.UI",
                "UnityEngine.CoreModule",
                "Assembly-CSharp",
            };

            foreach (var assembly in assemblies)
            {
                type = Type.GetType($"{typeName}, {assembly}");
                if (type != null)
                {
                    return type;
                }
            }

            // Search all loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            throw new InvalidOperationException($"Type not found: {typeName}");
        }

        /// <summary>
        /// Gets a boolean value from a dictionary payload with a default value.
        /// </summary>
        /// <param name="payload">The payload dictionary.</param>
        /// <param name="key">The key to retrieve.</param>
        /// <param name="defaultValue">The default value if the key doesn't exist.</param>
        /// <returns>The boolean value, or the default value if the key doesn't exist.</returns>
        private static bool GetBool(Dictionary<string, object> payload, string key, bool defaultValue)
        {
            if (payload == null || !payload.ContainsKey(key))
            {
                return defaultValue;
            }

            var value = payload[key];
            if (value is bool boolValue)
            {
                return boolValue;
            }

            if (value is string strValue)
            {
                return bool.TryParse(strValue, out var result) ? result : defaultValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// Resolves a GameObject from a payload dictionary, supporting both path and GlobalObjectId.
        /// </summary>
        /// <param name="payload">The payload containing gameObjectPath or gameObjectGlobalObjectId.</param>
        /// <returns>The resolved GameObject.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the GameObject cannot be resolved.</exception>
        private static GameObject ResolveGameObjectFromPayload(Dictionary<string, object> payload)
        {
            // Try GlobalObjectId first (more reliable)
            var globalObjectIdStr = GetString(payload, "gameObjectGlobalObjectId");
            if (!string.IsNullOrEmpty(globalObjectIdStr))
            {
                if (GlobalObjectId.TryParse(globalObjectIdStr, out var globalObjectId))
                {
                    var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId);
                    if (obj is GameObject go)
                    {
                        return go;
                    }
                    if (obj is Component comp)
                    {
                        return comp.gameObject;
                    }
                }
                throw new InvalidOperationException($"Could not resolve GameObject from GlobalObjectId: {globalObjectIdStr}");
            }

            // Fall back to gameObjectPath
            var path = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("Either gameObjectPath or gameObjectGlobalObjectId is required");
            }

            return ResolveGameObject(path);
        }

        /// <summary>
        /// Resolves an asset path from a payload dictionary, supporting both path and GUID.
        /// </summary>
        /// <param name="payload">The payload containing assetPath or assetGuid.</param>
        /// <returns>The resolved asset path.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the asset cannot be resolved.</exception>
        private static string ResolveAssetPathFromPayload(Dictionary<string, object> payload)
        {
            // Try GUID first (more reliable)
            var guid = GetString(payload, "assetGuid");
            if (!string.IsNullOrEmpty(guid))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    throw new InvalidOperationException($"Could not resolve asset from GUID: {guid}");
                }
                return path;
            }

            // Fall back to assetPath
            var assetPath = GetString(payload, "assetPath");
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("Either assetPath or assetGuid is required");
            }

            return assetPath;
        }

        /// <summary>
        /// Applies property changes to an asset's importer settings.
        /// </summary>
        /// <param name="assetPath">The asset path.</param>
        /// <param name="propertyChanges">Dictionary of property name/value pairs to apply.</param>
        private static void ApplyAssetImporterProperties(string assetPath, Dictionary<string, object> propertyChanges)
        {
            if (propertyChanges == null || propertyChanges.Count == 0)
            {
                return;
            }

            var importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not get AssetImporter for: {assetPath}");
            }

            foreach (var kvp in propertyChanges)
            {
                ApplyProperty(importer, kvp.Key, kvp.Value);
            }

            importer.SaveAndReimport();
        }

        /// <summary>
        /// Describes a component by serializing its public properties and fields.
        /// </summary>
        /// <param name="component">The component to describe.</param>
        /// <returns>A dictionary containing component information.</returns>
        private static Dictionary<string, object> DescribeComponent(Component component)
        {
            if (component == null)
            {
                return null;
            }

            var result = new Dictionary<string, object>
            {
                ["type"] = component.GetType().FullName,
                ["typeName"] = component.GetType().Name,
                ["gameObjectPath"] = GetHierarchyPath(component.gameObject),
                ["instanceID"] = component.GetInstanceID(),
            };

            // Add GlobalObjectId if available
            var globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(component.gameObject);
            result["globalObjectId"] = globalObjectId.ToString();

            var properties = new Dictionary<string, object>();
            var type = component.GetType();

            // Properties that cause memory leaks in edit mode (use shared versions instead)
            var dangerousProperties = new HashSet<string>
            {
                "mesh",      // Use sharedMesh instead
                "material",  // Use sharedMaterial instead
                "materials", // Use sharedMaterials instead
            };

            // Serialize public properties
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.CanRead && !prop.GetIndexParameters().Any())
                {
                    // Skip dangerous properties that cause memory leaks
                    if (dangerousProperties.Contains(prop.Name))
                    {
                        properties[prop.Name] = $"<Skipped:{prop.Name}:UseSharedVersion>";
                        continue;
                    }

                    try
                    {
                        var value = prop.GetValue(component);
                        properties[prop.Name] = SerializeValue(value);
                    }
                    catch
                    {
                        // Skip properties that can't be read
                    }
                }
            }

            result["properties"] = properties;
            return result;
        }

        /// <summary>
        /// Describes an asset by its path.
        /// </summary>
        /// <param name="assetPath">The asset path.</param>
        /// <returns>A dictionary containing asset information.</returns>
        private static Dictionary<string, object> DescribeAsset(string assetPath)
        {
            var result = new Dictionary<string, object>
            {
                ["path"] = assetPath,
                ["exists"] = File.Exists(assetPath),
                ["guid"] = AssetDatabase.AssetPathToGUID(assetPath),
            };

            if (File.Exists(assetPath))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (asset != null)
                {
                    result["name"] = asset.name;
                    result["type"] = asset.GetType().FullName;
                }

                var fileInfo = new FileInfo(assetPath);
                result["size"] = fileInfo.Length;
                result["modified"] = fileInfo.LastWriteTimeUtc.ToString("o");
            }

            return result;
        }

        /// <summary>
        /// Applies a property value to a component.
        /// </summary>
        /// <param name="component">The component to modify.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value to set.</param>
        private static void ApplyProperty(Component component, string propertyName, object value)
        {
            ApplyPropertyToObject(component, propertyName, value);
        }

        private static void ApplyProperty(UnityEngine.Object obj, string propertyName, object value)
        {
            ApplyPropertyToObject(obj, propertyName, value);
        }

        private static void ApplyPropertyToObject(UnityEngine.Object obj, string propertyName, object value)
        {
            var type = obj.GetType();

            // Try property first
            var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                try
                {
                    var converted = ConvertValue(value, prop.PropertyType);
                    prop.SetValue(obj, converted);
                    Undo.RecordObject(obj, $"Set {propertyName}");
                    EditorUtility.SetDirty(obj);
                    return;
                }
                catch (Exception ex)
                {
                    var valueStr = value?.ToString() ?? "null";
                    var valueType = value?.GetType().Name ?? "null";
                    throw new InvalidOperationException(
                        $"Failed to set property '{propertyName}' on {type.Name}. " +
                        $"Target type: {prop.PropertyType.Name}, " +
                        $"Provided value: '{valueStr}' ({valueType}). " +
                        $"Error: {ex.Message}");
                }
            }

            // Try field
            var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                try
                {
                    var converted = ConvertValue(value, field.FieldType);
                    field.SetValue(obj, converted);
                    Undo.RecordObject(obj, $"Set {propertyName}");
                    EditorUtility.SetDirty(obj);
                    return;
                }
                catch (Exception ex)
                {
                    var valueStr = value?.ToString() ?? "null";
                    var valueType = value?.GetType().Name ?? "null";
                    throw new InvalidOperationException(
                        $"Failed to set field '{propertyName}' on {type.Name}. " +
                        $"Target type: {field.FieldType.Name}, " +
                        $"Provided value: '{valueStr}' ({valueType}). " +
                        $"Error: {ex.Message}");
                }
            }

            // Property/field not found - provide helpful suggestions
            var allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .Select(p => p.Name)
                .ToList();
            var allFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Select(f => f.Name)
                .ToList();
            var allMembers = allProperties.Concat(allFields).OrderBy(n => n).ToList();

            var suggestions = "";
            if (allMembers.Count > 0)
            {
                // Find similar names (case-insensitive)
                var similarNames = allMembers
                    .Where(n => n.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (similarNames.Count > 0)
                {
                    suggestions = $" Did you mean: {string.Join(", ", similarNames)}?";
                }
                else
                {
                    suggestions = $" Available members: {string.Join(", ", allMembers.Take(10))}";
                    if (allMembers.Count > 10)
                    {
                        suggestions += $" (and {allMembers.Count - 10} more)";
                    }
                }
            }

            throw new InvalidOperationException($"Property or field '{propertyName}' not found on {type.FullName}.{suggestions}");
        }

        /// <summary>
        /// Converts a value to a target type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The target type.</param>
        /// <returns>The converted value.</returns>
        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null)
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }

            // Direct assignment if types match
            if (targetType.IsAssignableFrom(value.GetType()))
            {
                return value;
            }

            // Handle Vector2
            if (targetType == typeof(Vector2) && value is Dictionary<string, object> v2Dict)
            {
                return new Vector2(
                    GetFloat(v2Dict, "x") ?? 0f,
                    GetFloat(v2Dict, "y") ?? 0f
                );
            }

            // Handle Vector3
            if (targetType == typeof(Vector3) && value is Dictionary<string, object> v3Dict)
            {
                return new Vector3(
                    GetFloat(v3Dict, "x") ?? 0f,
                    GetFloat(v3Dict, "y") ?? 0f,
                    GetFloat(v3Dict, "z") ?? 0f
                );
            }

            // Handle Vector4
            if (targetType == typeof(Vector4) && value is Dictionary<string, object> v4Dict)
            {
                return new Vector4(
                    GetFloat(v4Dict, "x") ?? 0f,
                    GetFloat(v4Dict, "y") ?? 0f,
                    GetFloat(v4Dict, "z") ?? 0f,
                    GetFloat(v4Dict, "w") ?? 0f
                );
            }

            // Handle Color
            if (targetType == typeof(Color))
            {
                // Dictionary format: {r:1, g:0, b:0, a:1}
                if (value is Dictionary<string, object> colorDict)
                {
                    return new Color(
                        GetFloat(colorDict, "r") ?? 0f,
                        GetFloat(colorDict, "g") ?? 0f,
                        GetFloat(colorDict, "b") ?? 0f,
                        GetFloat(colorDict, "a") ?? 1f
                    );
                }

                // String color name: "red", "blue", etc.
                if (value is string colorName)
                {
                    try
                    {
                        var rgba = McpConstantConverter.ColorNameToRGBA(colorName);
                        return new Color(
                            (float)rgba["r"],
                            (float)rgba["g"],
                            (float)rgba["b"],
                            (float)rgba["a"]
                        );
                    }
                    catch (ArgumentException ex)
                    {
                        throw new InvalidOperationException($"Invalid color name '{colorName}': {ex.Message}. " +
                            $"Valid colors are: {string.Join(", ", McpConstantConverter.ListColorNames())}");
                    }
                }
            }

            // Handle Rect
            if (targetType == typeof(Rect) && value is Dictionary<string, object> rectDict)
            {
                return new Rect(
                    GetFloat(rectDict, "x") ?? 0f,
                    GetFloat(rectDict, "y") ?? 0f,
                    GetFloat(rectDict, "width") ?? 0f,
                    GetFloat(rectDict, "height") ?? 0f
                );
            }

            // Handle enums
            if (targetType.IsEnum)
            {
                if (value is string strValue)
                {
                    try
                    {
                        // Try exact match first
                        if (Enum.IsDefined(targetType, strValue))
                        {
                            return Enum.Parse(targetType, strValue, false);
                        }

                        // Try case-insensitive match
                        var names = Enum.GetNames(targetType);
                        var matchedName = names.FirstOrDefault(n =>
                            string.Equals(n, strValue, StringComparison.OrdinalIgnoreCase));

                        if (matchedName != null)
                        {
                            return Enum.Parse(targetType, matchedName, false);
                        }

                        // No match found
                        throw new InvalidOperationException(
                            $"Enum value '{strValue}' is not valid for type {targetType.Name}. " +
                            $"Valid values are: {string.Join(", ", names)}");
                    }
                    catch (ArgumentException ex)
                    {
                        var names = Enum.GetNames(targetType);
                        throw new InvalidOperationException(
                            $"Failed to parse enum value '{strValue}' for type {targetType.Name}: {ex.Message}. " +
                            $"Valid values are: {string.Join(", ", names)}");
                    }
                }

                if (value is int intValue)
                {
                    if (Enum.IsDefined(targetType, intValue))
                    {
                        return Enum.ToObject(targetType, intValue);
                    }
                    else
                    {
                        // Allow undefined values but log warning
                        Debug.LogWarning($"Enum value {intValue} is not defined in {targetType.Name}. " +
                            $"Valid values are: {string.Join(", ", Enum.GetValues(targetType).Cast<int>())}");
                        return Enum.ToObject(targetType, intValue);
                    }
                }

                // Try to convert other numeric types to int
                if (value is long || value is short || value is byte || value is sbyte ||
                    value is uint || value is ushort || value is ulong)
                {
                    try
                    {
                        var enumIntValue = Convert.ToInt32(value);
                        if (Enum.IsDefined(targetType, enumIntValue))
                        {
                            return Enum.ToObject(targetType, enumIntValue);
                        }
                        else
                        {
                            Debug.LogWarning($"Enum value {enumIntValue} is not defined in {targetType.Name}. " +
                                $"Valid values are: {string.Join(", ", Enum.GetValues(targetType).Cast<int>())}");
                            return Enum.ToObject(targetType, enumIntValue);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to convert {value} ({value.GetType().Name}) to enum {targetType.Name}: {ex.Message}");
                    }
                }
            }

            // Handle Unity Object references
            if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
            {
                if (value is Dictionary<string, object> refDict && refDict.ContainsKey("_ref"))
                {
                    var refType = refDict["_ref"].ToString();
                    if (refType == "asset")
                    {
                        // Try GUID first
                        if (refDict.ContainsKey("guid"))
                        {
                            var guid = refDict["guid"].ToString();
                            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                            if (!string.IsNullOrEmpty(assetPath))
                            {
                                return AssetDatabase.LoadAssetAtPath(assetPath, targetType);
                            }
                        }

                        // Fall back to path
                        if (refDict.ContainsKey("path"))
                        {
                            var path = refDict["path"].ToString();
                            return AssetDatabase.LoadAssetAtPath(path, targetType);
                        }
                    }
                }

                // Direct asset path
                if (value is string assetPathStr)
                {
                    // Handle built-in resources
                    if (assetPathStr.StartsWith("Library/unity default resources::"))
                    {
                        var resourceName = assetPathStr.Substring("Library/unity default resources::".Length);
                        var builtIn = AssetDatabase.GetBuiltinExtraResource(targetType, resourceName);
                        if (builtIn != null)
                        {
                            return builtIn;
                        }
                    }

                    // Regular asset
                    return AssetDatabase.LoadAssetAtPath(assetPathStr, targetType);
                }
            }

            // Primitive type conversions
            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch (Exception ex)
            {
                // Provide detailed error message with conversion context
                var valueStr = value?.ToString() ?? "null";
                var valueType = value?.GetType().Name ?? "null";

                throw new InvalidOperationException(
                    $"Cannot convert value '{valueStr}' of type {valueType} to {targetType.Name}. " +
                    $"Reason: {ex.Message}");
            }
        }

        /// <summary>
        /// Ensures a value is not null or empty, throwing an exception if it is.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="paramName">The parameter name for error messages.</param>
        /// <returns>The original value if it's valid.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the value is null or empty.</exception>
        private static string EnsureValue(string value, string paramName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException($"{paramName} is required and cannot be null or empty");
            }
            return value;
        }

        /// <summary>
        /// Ensures the directory for a given file path exists, creating it if necessary.
        /// </summary>
        /// <param name="filePath">The file path whose directory should exist.</param>
        private static void EnsureDirectoryExists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        #endregion

        #region Constant Conversion

        /// <summary>
        /// Handles constant conversion operations (enum, color, layer conversions).
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation' type.</param>
        /// <returns>Result dictionary with conversion data.</returns>
        /// <exception cref="InvalidOperationException">Thrown when operation is invalid or missing.</exception>
        private static object HandleConstantConvert(Dictionary<string, object> payload)
        {
            var operation = GetString(payload, "operation");
            if (string.IsNullOrEmpty(operation))
            {
                throw new InvalidOperationException("operation is required");
            }

            switch (operation)
            {
                case "enumToValue":
                    return ConvertEnumToValue(payload);
                case "valueToEnum":
                    return ConvertValueToEnum(payload);
                case "colorToRGBA":
                    return ConvertColorToRGBA(payload);
                case "rgbaToColor":
                    return ConvertRGBAToColor(payload);
                case "layerToIndex":
                    return ConvertLayerToIndex(payload);
                case "indexToLayer":
                    return ConvertIndexToLayer(payload);
                case "listEnums":
                    return ListEnumValues(payload);
                case "listColors":
                    return ListConstantColors();
                case "listLayers":
                    return ListConstantLayers();
                default:
                    throw new InvalidOperationException($"Unknown operation: {operation}");
            }
        }

        /// <summary>
        /// Converts enum name to numeric value.
        /// </summary>
        private static object ConvertEnumToValue(Dictionary<string, object> payload)
        {
            var enumTypeName = EnsureValue(GetString(payload, "enumType"), "enumType");
            var enumValueName = EnsureValue(GetString(payload, "enumValue"), "enumValue");

            var numericValue = McpConstantConverter.EnumNameToValue(enumTypeName, enumValueName);

            return new Dictionary<string, object>
            {
                ["enumType"] = enumTypeName,
                ["enumValue"] = enumValueName,
                ["numericValue"] = numericValue,
                ["success"] = true
            };
        }

        /// <summary>
        /// Converts numeric value to enum name.
        /// </summary>
        private static object ConvertValueToEnum(Dictionary<string, object> payload)
        {
            var enumTypeName = EnsureValue(GetString(payload, "enumType"), "enumType");
            var numericValue = GetInt(payload, "numericValue", 0);

            var enumValueName = McpConstantConverter.EnumValueToName(enumTypeName, numericValue);

            return new Dictionary<string, object>
            {
                ["enumType"] = enumTypeName,
                ["numericValue"] = numericValue,
                ["enumValue"] = enumValueName,
                ["success"] = true
            };
        }

        /// <summary>
        /// Converts Unity color name to RGBA values.
        /// </summary>
        private static object ConvertColorToRGBA(Dictionary<string, object> payload)
        {
            var colorName = EnsureValue(GetString(payload, "colorName"), "colorName");

            var rgba = McpConstantConverter.ColorNameToRGBA(colorName);

            return new Dictionary<string, object>
            {
                ["colorName"] = colorName,
                ["rgba"] = rgba,
                ["r"] = rgba["r"],
                ["g"] = rgba["g"],
                ["b"] = rgba["b"],
                ["a"] = rgba["a"],
                ["success"] = true
            };
        }

        /// <summary>
        /// Converts RGBA values to Unity color name (nearest match).
        /// </summary>
        private static object ConvertRGBAToColor(Dictionary<string, object> payload)
        {
            var r = GetFloat(payload, "r", 0f);
            var g = GetFloat(payload, "g", 0f);
            var b = GetFloat(payload, "b", 0f);
            var a = GetFloat(payload, "a", 1f);

            var colorName = McpConstantConverter.RGBAToColorName(r, g, b, a);

            return new Dictionary<string, object>
            {
                ["r"] = r,
                ["g"] = g,
                ["b"] = b,
                ["a"] = a,
                ["colorName"] = colorName ?? "unknown",
                ["matched"] = colorName != null,
                ["success"] = true
            };
        }

        /// <summary>
        /// Converts layer name to layer index.
        /// </summary>
        private static object ConvertLayerToIndex(Dictionary<string, object> payload)
        {
            var layerName = EnsureValue(GetString(payload, "layerName"), "layerName");

            var layerIndex = McpConstantConverter.LayerNameToIndex(layerName);

            return new Dictionary<string, object>
            {
                ["layerName"] = layerName,
                ["layerIndex"] = layerIndex,
                ["success"] = true
            };
        }

        /// <summary>
        /// Converts layer index to layer name.
        /// </summary>
        private static object ConvertIndexToLayer(Dictionary<string, object> payload)
        {
            var layerIndex = GetInt(payload, "layerIndex", 0);

            var layerName = McpConstantConverter.LayerIndexToName(layerIndex);

            return new Dictionary<string, object>
            {
                ["layerIndex"] = layerIndex,
                ["layerName"] = layerName,
                ["success"] = true
            };
        }

        /// <summary>
        /// Lists all values for a given enum type.
        /// </summary>
        private static object ListEnumValues(Dictionary<string, object> payload)
        {
            var enumTypeName = EnsureValue(GetString(payload, "enumType"), "enumType");

            var enumValues = McpConstantConverter.ListEnumValues(enumTypeName);

            return new Dictionary<string, object>
            {
                ["enumType"] = enumTypeName,
                ["values"] = enumValues,
                ["count"] = enumValues.Count,
                ["success"] = true
            };
        }

        /// <summary>
        /// Lists all Unity built-in color names.
        /// </summary>
        private static object ListConstantColors()
        {
            var colorNames = McpConstantConverter.ListColorNames();

            return new Dictionary<string, object>
            {
                ["colors"] = colorNames,
                ["count"] = colorNames.Count,
                ["success"] = true
            };
        }

        /// <summary>
        /// Lists all layer names and their indices (for constant conversion).
        /// </summary>
        private static object ListConstantLayers()
        {
            var layers = McpConstantConverter.ListLayers();

            return new Dictionary<string, object>
            {
                ["layers"] = layers,
                ["count"] = layers.Count,
                ["success"] = true
            };
        }

        #endregion

        #region Batch Execute

        /// <summary>
        /// Handles batch execution of multiple tool operations.
        /// Automatically detects script changes and triggers compilation.
        /// </summary>
        /// <param name="payload">
        /// Required keys:
        /// - operations: List of operations, each with:
        ///   - tool: Tool name (e.g., "assetManage", "gameObjectManage")
        ///   - payload: Tool-specific payload
        /// Optional keys:
        /// - stopOnError: If true, stops batch on first error (default: false)
        /// - awaitCompilation: If true, triggers compilation after script changes (default: true)
        /// </param>
        private static object HandleBatchExecute(Dictionary<string, object> payload)
        {
            var operationsList = GetList(payload, "operations");
            if (operationsList == null || operationsList.Count == 0)
            {
                throw new InvalidOperationException("operations array is required and must not be empty");
            }

            var stopOnError = GetBool(payload, "stopOnError", false);
            var results = new List<Dictionary<string, object>>();
            var hasErrors = false;
            var hasScriptChanges = false;

            foreach (var opObj in operationsList)
            {
                if (!(opObj is Dictionary<string, object> opPayload))
                {
                    results.Add(new Dictionary<string, object>
                    {
                        ["success"] = false,
                        ["error"] = "Invalid operation entry: must be a dictionary"
                    });
                    hasErrors = true;
                    if (stopOnError) break;
                    continue;
                }

                try
                {
                    var toolName = GetString(opPayload, "tool");
                    var toolPayload = opPayload.TryGetValue("payload", out var payloadObj) && payloadObj is Dictionary<string, object> dict
                        ? dict
                        : new Dictionary<string, object>();

                    if (string.IsNullOrEmpty(toolName))
                    {
                        throw new InvalidOperationException("tool name is required for each operation");
                    }

                    // Create a command for this operation
                    var command = new McpIncomingCommand("batch_" + Guid.NewGuid().ToString(), toolName, toolPayload);

                    // Execute the tool
                    var result = Execute(command);

                    // Check if this was a script-related operation
                    if (IsScriptRelatedTool(toolName, toolPayload))
                    {
                        hasScriptChanges = true;
                    }

                    results.Add(new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["tool"] = toolName,
                        ["result"] = result
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new Dictionary<string, object>
                    {
                        ["success"] = false,
                        ["tool"] = opPayload.TryGetValue("tool", out var t) ? t : "unknown",
                        ["error"] = ex.Message
                    });
                    hasErrors = true;
                    if (stopOnError) break;
                }
            }

            // If script changes were detected, trigger compilation
            var compilationTriggered = false;
            if (hasScriptChanges)
            {
                AssetDatabase.Refresh();
                CompilationPipeline.RequestScriptCompilation();
                compilationTriggered = true;
            }

            return new Dictionary<string, object>
            {
                ["success"] = !hasErrors,
                ["processedCount"] = results.Count,
                ["totalCount"] = operationsList.Count,
                ["results"] = results,
                ["compilationTriggered"] = compilationTriggered,
                ["message"] = hasErrors
                    ? $"Batch completed with errors. Processed {results.Count}/{operationsList.Count} operations."
                    : $"Batch completed successfully. Processed {results.Count} operations." +
                      (compilationTriggered ? " Compilation triggered." : "")
            };
        }

        /// <summary>
        /// Determines if a tool operation involves script changes that require compilation.
        /// </summary>
        private static bool IsScriptRelatedTool(string toolName, Dictionary<string, object> toolPayload)
        {
            // Note: assetManage no longer supports create/update operations (file operations must be done via Claude Code)
            // Only delete operations on .cs files trigger compilation
            if (toolName == "assetManage")
            {
                var operation = GetString(toolPayload, "operation");
                if (operation == "delete")
                {
                    var assetPath = GetString(toolPayload, "assetPath");
                    if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                // Check for multiple delete operations with .cs patterns
                if (operation == "deleteMultiple")
                {
                    var pattern = GetString(toolPayload, "pattern");
                    if (!string.IsNullOrEmpty(pattern) && pattern.Contains(".cs", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion
    }
}

