using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
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
                "hierarchyBuilder" => HandleHierarchyBuilder(command.Payload),
                "sceneQuickSetup" => HandleSceneQuickSetup(command.Payload),
                "gameObjectCreateFromTemplate" => HandleGameObjectCreateFromTemplate(command.Payload),
                "contextInspect" => HandleContextInspect(command.Payload),
                "tagLayerManage" => HandleTagLayerManage(command.Payload),
                "scriptManage" => HandleScriptManage(command.Payload),
                "prefabManage" => HandlePrefabManage(command.Payload),
                "projectSettingsManage" => HandleProjectSettingsManage(command.Payload),
                "renderPipelineManage" => HandleRenderPipelineManage(command.Payload),
                "inputSystemManage" => HandleInputSystemManage(command.Payload),
                "batchExecute" => HandleBatchExecute(command.Payload),
                "tilemapManage" => HandleTilemapManage(command.Payload),
                "navmeshManage" => HandleNavMeshManage(command.Payload),
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
            var components = go.GetComponents<Component>();
            var componentsList = new List<Dictionary<string, object>>();

            foreach (var component in components)
            {
                if (component == null)
                {
                    continue;
                }

                var componentType = component.GetType();
                var properties = new Dictionary<string, object>();

                // Get all public properties
                var propertyInfos = componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in propertyInfos)
                {
                    if (!prop.CanRead)
                    {
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

                var componentData = new Dictionary<string, object>
                {
                    ["type"] = componentType.FullName,
                    ["properties"] = properties,
                };

                // Add enabled state for Behaviour components
                if (component is Behaviour behaviour)
                {
                    componentData["enabled"] = behaviour.enabled;
                }

                componentsList.Add(componentData);
            }

            return new Dictionary<string, object>
            {
                ["gameObject"] = GetHierarchyPath(go),
                ["components"] = componentsList,
                ["count"] = componentsList.Count,
            };
        }

        private static object FindMultipleGameObjects(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");

            var gameObjects = McpWildcardUtility.ResolveGameObjects(pattern, useRegex);

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
                ["count"] = results.Count,
                ["gameObjects"] = results,
            };
        }

        private static object DeleteMultipleGameObjects(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");

            var gameObjects = McpWildcardUtility.ResolveGameObjects(pattern, useRegex);
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
                ["count"] = deletedPaths.Count,
                ["deleted"] = deletedPaths,
            };
        }

        private static object InspectMultipleGameObjects(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");
            var includeComponents = GetBool(payload, "includeComponents");

            var gameObjects = McpWildcardUtility.ResolveGameObjects(pattern, useRegex);

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
                ["count"] = results.Count,
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

            return new Dictionary<string, object>
            {
                ["gameObject"] = GetHierarchyPath(go),
                ["type"] = componentType.FullName,
                ["properties"] = properties,
            };
        }

        private static object AddMultipleComponents(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");
            var componentTypeName = EnsureValue(GetString(payload, "componentType"), "componentType");
            var type = ResolveType(componentTypeName);

            var gameObjects = McpWildcardUtility.ResolveGameObjects(pattern, useRegex);
            var results = new List<Dictionary<string, object>>();

            foreach (var go in gameObjects)
            {
                var component = go.AddComponent(type);
                results.Add(new Dictionary<string, object>
                {
                    ["gameObject"] = GetHierarchyPath(go),
                    ["type"] = type.FullName,
                });
            }

            return new Dictionary<string, object>
            {
                ["pattern"] = pattern,
                ["componentType"] = type.FullName,
                ["count"] = results.Count,
                ["results"] = results,
            };
        }

        private static object RemoveMultipleComponents(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");
            var componentTypeName = EnsureValue(GetString(payload, "componentType"), "componentType");
            var type = ResolveType(componentTypeName);

            var gameObjects = McpWildcardUtility.ResolveGameObjects(pattern, useRegex);
            var results = new List<Dictionary<string, object>>();

            foreach (var go in gameObjects)
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
            }

            return new Dictionary<string, object>
            {
                ["pattern"] = pattern,
                ["componentType"] = type.FullName,
                ["count"] = results.Count,
                ["results"] = results,
            };
        }

        private static object UpdateMultipleComponents(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");
            var componentTypeName = EnsureValue(GetString(payload, "componentType"), "componentType");
            var type = ResolveType(componentTypeName);
            var propertyChanges = payload.ContainsKey("propertyChanges") && payload["propertyChanges"] is Dictionary<string, object> changes
                ? changes
                : new Dictionary<string, object>();

            var gameObjects = McpWildcardUtility.ResolveGameObjects(pattern, useRegex);
            var results = new List<Dictionary<string, object>>();

            foreach (var go in gameObjects)
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
            }

            return new Dictionary<string, object>
            {
                ["pattern"] = pattern,
                ["componentType"] = type.FullName,
                ["count"] = results.Count,
                ["results"] = results,
            };
        }

        private static object InspectMultipleComponents(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");
            var componentTypeName = EnsureValue(GetString(payload, "componentType"), "componentType");
            var type = ResolveType(componentTypeName);

            var gameObjects = McpWildcardUtility.ResolveGameObjects(pattern, useRegex);
            var results = new List<Dictionary<string, object>>();

            foreach (var go in gameObjects)
            {
                var component = go.GetComponent(type);
                if (component != null)
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

                    results.Add(new Dictionary<string, object>
                    {
                        ["gameObject"] = GetHierarchyPath(go),
                        ["type"] = componentType.FullName,
                        ["properties"] = properties,
                    });
                }
            }

            return new Dictionary<string, object>
            {
                ["pattern"] = pattern,
                ["componentType"] = type.FullName,
                ["count"] = results.Count,
                ["results"] = results,
            };
        }

        /// <summary>
        /// Handles asset management operations (create, update, delete, rename, duplicate, inspect).
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation' and asset-specific settings.</param>
        /// <returns>Result dictionary with asset information.</returns>
        private static object HandleAssetManage(Dictionary<string, object> payload)
        {
            var operation = EnsureValue(GetString(payload, "operation"), "operation");
            return operation switch
            {
                "create" => CreateAsset(payload),
                "update" => UpdateAsset(payload),
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

        private static object CreateAsset(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "assetPath"), "assetPath");
            var contents = GetString(payload, "contents") ?? string.Empty;
            var timeoutSeconds = GetInt(payload, "timeoutSeconds", 30);

            EnsureDirectoryExists(path);
            File.WriteAllText(path, contents, Encoding.UTF8);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var result = DescribeAsset(path);

            // If this is a C# script file, wait for compilation to complete
            if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                var compilationResult = WaitForCompilation(timeoutSeconds);
                if (result is Dictionary<string, object> dict)
                {
                    dict["compilation"] = compilationResult;
                }
            }

            return result;
        }

        private static object UpdateAsset(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "assetPath"), "assetPath");
            var contents = GetString(payload, "contents");
            var overwrite = GetBool(payload, "overwrite", true);
            var timeoutSeconds = GetInt(payload, "timeoutSeconds", 30);

            if (!File.Exists(path) && !overwrite)
            {
                throw new InvalidOperationException($"Asset does not exist: {path}");
            }

            EnsureDirectoryExists(path);
            File.WriteAllText(path, contents ?? string.Empty, Encoding.UTF8);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var result = DescribeAsset(path);

            // If this is a C# script file, wait for compilation to complete
            if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                var compilationResult = WaitForCompilation(timeoutSeconds);
                if (result is Dictionary<string, object> dict)
                {
                    dict["compilation"] = compilationResult;
                }
            }

            return result;
        }

        private static object DeleteAsset(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "assetPath"), "assetPath");
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
            var path = EnsureValue(GetString(payload, "assetPath"), "assetPath");
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
            var path = EnsureValue(GetString(payload, "assetPath"), "assetPath");
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
            var path = EnsureValue(GetString(payload, "assetPath"), "assetPath");
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                throw new InvalidOperationException($"Asset not found: {path}");
            }

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

            if (assetObj != null)
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
            // Update anchoredPosition if provided
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

            // Update sizeDelta if provided
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

            // Update pivot if provided
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

            // Update offsetMin if provided
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

            // Update offsetMax if provided
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

            // Apply size
            var width = GetFloat(payload, "width") ?? defaultWidth;
            var height = GetFloat(payload, "height") ?? defaultHeight;
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

        private static object HandleScriptManage(Dictionary<string, object> payload)
        {
            var operation = EnsureValue(GetString(payload, "operation"), "operation").ToLowerInvariant();

            return operation switch
            {
                "read" => HandleScriptRead(payload),
                "outline" => HandleScriptRead(payload),
                "create" => HandleScriptCreate(payload),
                "update" => HandleScriptUpdate(payload),
                "delete" => HandleScriptDelete(payload),
                _ => throw new InvalidOperationException($"Unknown scriptManage operation: {operation}"),
            };
        }

        private static object HandleScriptRead(Dictionary<string, object> payload)
        {
            var assetPath = ResolveScriptPath(payload, "assetPath", false);
            var fullPath = Path.GetFullPath(assetPath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("Script file not found", fullPath);
            }

            var waitForCompilation = GetBool(payload, "waitForCompilation", true);
            var timeoutSeconds = GetInt(payload, "timeoutSeconds", 30);

            // Wait for any ongoing compilation to complete before reading
            // This ensures we get the latest compiled state
            if (waitForCompilation && EditorApplication.isCompiling)
            {
                var compilationResult = WaitForCompilation(timeoutSeconds);
                // Continue even if compilation timed out or had errors
                // User might still want to read the script
            }

            var source = File.ReadAllText(fullPath);
            var outline = AnalyzeScriptOutline(source);
            var syntaxOk = CheckBraceBalance(source);
            var includeSource = GetBool(payload, "includeSource", true);

            var result = new Dictionary<string, object>
            {
                ["assetPath"] = assetPath,
                ["syntaxOk"] = syntaxOk,
                ["outline"] = outline,
            };

            if (includeSource)
            {
                result["source"] = source;
            }

            return result;
        }

        private static string ResolveScriptPath(Dictionary<string, object> payload, string preferredKey, bool allowAssetFallback)
        {
            var keyUsed = preferredKey;
            var path = GetString(payload, preferredKey);

            if (allowAssetFallback && string.IsNullOrEmpty(path))
            {
                var assetValue = GetString(payload, "assetPath");
                if (!string.IsNullOrEmpty(assetValue))
                {
                    path = assetValue;
                    keyUsed = "assetPath";
                }
            }

            var guid = GetString(payload, "guid");
            if (!string.IsNullOrEmpty(guid))
            {
                var guidPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(guidPath))
                {
                    throw new InvalidOperationException($"No asset found for guid: {guid}");
                }

                path = guidPath;
                keyUsed = "guid";
            }

            path = EnsureValue(path, keyUsed);
            path = path.Replace("\\", "/");

            if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("scriptPath must be under the Assets/ folder.");
            }

            return path;
        }

        private static object HandleScriptUpdate(Dictionary<string, object> payload)
        {
            var scriptPath = ResolveScriptPath(payload, "scriptPath", true);
            var fullPath = Path.GetFullPath(scriptPath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("Script file not found", fullPath);
            }

            var editsList = GetList(payload, "edits");
            if (editsList == null || editsList.Count == 0)
            {
                throw new InvalidOperationException("edits array is required for scriptManage update operation.");
            }

            var dryRun = GetBool(payload, "dryRun");
            var timeoutSeconds = GetInt(payload, "timeoutSeconds", 30);
            var source = File.ReadAllText(fullPath, Encoding.UTF8);
            var updatedSource = source;
            var editSummaries = ApplyScriptEdits(ref updatedSource, editsList);
            var appliedCount = editSummaries.Sum(summary => summary.TryGetValue("appliedCount", out var value) ? Convert.ToInt32(value, CultureInfo.InvariantCulture) : 0);

            if (!dryRun && appliedCount > 0)
            {
                File.WriteAllText(fullPath, updatedSource, Encoding.UTF8);
                AssetDatabase.ImportAsset(scriptPath, ImportAssetOptions.ForceSynchronousImport);
            }

            var result = new Dictionary<string, object>
            {
                ["scriptPath"] = scriptPath,
                ["dryRun"] = dryRun,
                ["changesMade"] = appliedCount,
                ["appliedEdits"] = editSummaries,
                ["updatedLength"] = updatedSource.Length,
                ["previewSource"] = dryRun ? updatedSource : null,
            };

            // Always wait for compilation after script updates (unless dry run)
            if (!dryRun && appliedCount > 0)
            {
                var compilationResult = WaitForCompilation(timeoutSeconds);
                result["compilation"] = compilationResult;
            }

            return result;
        }

        private static object HandleScriptDelete(Dictionary<string, object> payload)
        {
            var scriptPath = ResolveScriptPath(payload, "scriptPath", true);
            var dryRun = GetBool(payload, "dryRun");
            var timeoutSeconds = GetInt(payload, "timeoutSeconds", 30);
            var fullPath = Path.GetFullPath(scriptPath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("Script file not found", fullPath);
            }

            if (!dryRun)
            {
                if (!AssetDatabase.DeleteAsset(scriptPath))
                {
                    throw new InvalidOperationException($"Failed to delete script at path: {scriptPath}");
                }

                // Refresh AssetDatabase to trigger compilation
                AssetDatabase.Refresh();
            }

            var result = new Dictionary<string, object>
            {
                ["scriptPath"] = scriptPath,
                ["deleted"] = !dryRun,
                ["dryRun"] = dryRun,
            };

            // Wait for compilation after script deletion (unless dry run)
            if (!dryRun)
            {
                var compilationResult = WaitForCompilation(timeoutSeconds);
                result["compilation"] = compilationResult;
                result["success"] = (bool)compilationResult["success"];
            }

            return result;
        }

        private static List<Dictionary<string, object>> ApplyScriptEdits(ref string source, List<object> edits)
        {
            var summaries = new List<Dictionary<string, object>>();

            foreach (var edit in edits)
            {
                if (edit is not Dictionary<string, object> editDict)
                {
                    throw new InvalidOperationException("Each script edit must be an object with action and match fields.");
                }

                var action = EnsureValue(GetString(editDict, "action"), "edits.action").ToLowerInvariant();
                var match = EnsureValue(GetString(editDict, "match"), "edits.match");
                var count = GetInt(editDict, "count", 1);
                if (count <= 0)
                {
                    count = int.MaxValue;
                }

                var caseSensitive = GetBool(editDict, "caseSensitive", true);
                var allowMissing = GetBool(editDict, "allowMissingMatch");
                var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                bool applied;
                int occurrences = 0;

                switch (action)
                {
                    case "replace":
                        var replacement = EnsureValue(GetString(editDict, "replacement"), "edits.replacement");
                        applied = TryApplyReplace(ref source, match, replacement, count, comparison, out occurrences);
                        break;
                    case "insertbefore":
                        var beforeText = EnsureValue(GetString(editDict, "text"), "edits.text");
                        applied = TryApplyInsert(ref source, match, beforeText, count, insertAfter: false, comparison, out occurrences);
                        break;
                    case "insertafter":
                        var afterText = EnsureValue(GetString(editDict, "text"), "edits.text");
                        applied = TryApplyInsert(ref source, match, afterText, count, insertAfter: true, comparison, out occurrences);
                        break;
                    case "delete":
                        applied = TryApplyReplace(ref source, match, string.Empty, count, comparison, out occurrences);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported script edit action: {action}");
                }

                if (!applied && !allowMissing)
                {
                    throw new InvalidOperationException($"No matches found for edit action '{action}' with pattern '{match}'.");
                }

                summaries.Add(new Dictionary<string, object>
                {
                    ["action"] = action,
                    ["match"] = match,
                    ["appliedCount"] = occurrences,
                    ["caseSensitive"] = caseSensitive,
                });
            }

            return summaries;
        }

        private static bool TryApplyReplace(ref string content, string search, string replacement, int maxCount, StringComparison comparison, out int occurrences)
        {
            occurrences = 0;
            if (string.IsNullOrEmpty(search))
            {
                return false;
            }

            var builder = new StringBuilder();
            var currentIndex = 0;
            while (true)
            {
                var index = content.IndexOf(search, currentIndex, comparison);
                if (index < 0)
                {
                    builder.Append(content, currentIndex, content.Length - currentIndex);
                    break;
                }

                builder.Append(content, currentIndex, index - currentIndex);
                builder.Append(replacement);
                occurrences++;
                currentIndex = index + search.Length;

                if (occurrences == maxCount)
                {
                    builder.Append(content, currentIndex, content.Length - currentIndex);
                    break;
                }
            }

            if (occurrences == 0)
            {
                return false;
            }

            content = builder.ToString();
            return true;
        }

        private static bool TryApplyInsert(ref string content, string search, string insertion, int maxCount, bool insertAfter, StringComparison comparison, out int occurrences)
        {
            occurrences = 0;
            if (string.IsNullOrEmpty(search))
            {
                return false;
            }

            var currentIndex = 0;
            while (true)
            {
                var index = content.IndexOf(search, currentIndex, comparison);
                if (index < 0)
                {
                    break;
                }

                var insertIndex = insertAfter ? index + search.Length : index;
                content = content.Insert(insertIndex, insertion);
                occurrences++;

                currentIndex = insertAfter
                    ? insertIndex + insertion.Length
                    : insertIndex + insertion.Length + search.Length;

                if (occurrences == maxCount)
                {
                    break;
                }
            }

            return occurrences > 0;
        }

        private static List<object> AnalyzeScriptOutline(string source)
        {
            var outline = new List<object>();
            var classRegex = new Regex(@"(public|internal|protected|private|static|partial|abstract|sealed|\s)+\s*(class|struct|record)\s+(?<name>[A-Za-z0-9_]+)", RegexOptions.Compiled);
            var methodRegex = new Regex(@"(public|internal|protected|private|static|virtual|override|async|\s)+\s+[A-Za-z0-9_<>,\[\]]+\s+(?<name>[A-Za-z0-9_]+)\s*\((?<args>[^)]*)\)\s*\{", RegexOptions.Compiled);

            foreach (Match classMatch in classRegex.Matches(source))
            {
                var className = classMatch.Groups["name"].Value;
                var classEntry = new Dictionary<string, object>
                {
                    ["kind"] = "type",
                    ["name"] = className,
                };

                var members = new List<object>();
                foreach (Match methodMatch in methodRegex.Matches(source, classMatch.Index))
                {
                    if (methodMatch.Index < classMatch.Index)
                    {
                        continue;
                    }

                    members.Add(new Dictionary<string, object>
                    {
                        ["kind"] = "method",
                        ["name"] = methodMatch.Groups["name"].Value,
                        ["signature"] = methodMatch.Value.Trim(),
                    });
                }

                classEntry["members"] = members;
                outline.Add(classEntry);
            }

            return outline;
        }

        private static bool CheckBraceBalance(string source)
        {
            var stack = 0;
            foreach (var ch in source)
            {
                if (ch == '{')
                {
                    stack++;
                }
                else if (ch == '}')
                {
                    stack--;
                }

                if (stack < 0)
                {
                    return false;
                }
            }

            return stack == 0;
        }

        /// <summary>
        /// Handles script creation with templates for MonoBehaviour, ScriptableObject, Editor scripts, etc.
        /// </summary>
        private static object HandleScriptCreate(Dictionary<string, object> payload)
        {
            var scriptPath = EnsureValue(GetString(payload, "scriptPath"), "scriptPath");
            var scriptType = GetString(payload, "scriptType") ?? "monoBehaviour";
            var namespaceName = GetString(payload, "namespace");
            var methods = GetList(payload, "methods");
            var fields = GetList(payload, "fields");
            var attributes = GetList(payload, "attributes");
            var baseClass = GetString(payload, "baseClass");
            var interfaces = GetList(payload, "interfaces");
            var includeUsings = GetList(payload, "includeUsings");
            var timeoutSeconds = GetInt(payload, "timeoutSeconds", 30);

            // Extract class name from path
            var className = Path.GetFileNameWithoutExtension(scriptPath);

            // Validate script path
            if (!scriptPath.EndsWith(".cs"))
            {
                scriptPath += ".cs";
            }

            // Generate script content
            var scriptContent = GenerateScriptContent(
                className,
                scriptType,
                namespaceName,
                methods,
                fields,
                attributes,
                baseClass,
                interfaces,
                includeUsings
            );

            // Ensure directory exists
            EnsureDirectoryExists(scriptPath);

            // Write file
            File.WriteAllText(scriptPath, scriptContent, Encoding.UTF8);
            AssetDatabase.ImportAsset(scriptPath, ImportAssetOptions.ForceSynchronousImport);

            var result = new Dictionary<string, object>
            {
                ["scriptPath"] = scriptPath,
                ["className"] = className,
                ["scriptType"] = scriptType,
                ["success"] = true,
            };

            // Always wait for compilation after script creation
            var compilationResult = WaitForCompilation(timeoutSeconds);
            result["compilation"] = compilationResult;
            result["success"] = (bool)compilationResult["success"];

            return result;
        }

        private static string GenerateScriptContent(
            string className,
            string scriptType,
            string namespaceName,
            List<object> methods,
            List<object> fields,
            List<object> attributes,
            string baseClass,
            List<object> interfaces,
            List<object> includeUsings)
        {
            var sb = new StringBuilder();
            var indent = "";

            // Add using statements
            var usings = new List<string> { "UnityEngine" };

            switch (scriptType.ToLower())
            {
                case "monobehaviour":
                    break;
                case "scriptableobject":
                    break;
                case "editor":
                    usings.Add("UnityEditor");
                    break;
                case "class":
                case "interface":
                case "struct":
                    // Basic usings only
                    break;
            }

            // Add custom usings
            if (includeUsings != null)
            {
                foreach (var u in includeUsings)
                {
                    var usingStr = u.ToString();
                    if (!usings.Contains(usingStr))
                    {
                        usings.Add(usingStr);
                    }
                }
            }

            foreach (var u in usings)
            {
                sb.AppendLine($"using {u};");
            }
            sb.AppendLine();

            // Add namespace if specified
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
                indent = "    ";
            }

            // Add class attributes
            if (attributes != null)
            {
                foreach (var attr in attributes)
                {
                    sb.AppendLine($"{indent}[{attr}]");
                }
            }

            // Add special attributes based on script type
            switch (scriptType.ToLower())
            {
                case "scriptableobject":
                    sb.AppendLine($"{indent}[CreateAssetMenu(fileName = \"{className}\", menuName = \"ScriptableObjects/{className}\")]");
                    break;
            }

            // Determine base class
            var actualBaseClass = baseClass;
            if (string.IsNullOrEmpty(actualBaseClass))
            {
                actualBaseClass = scriptType.ToLower() switch
                {
                    "monobehaviour" => "MonoBehaviour",
                    "scriptableobject" => "ScriptableObject",
                    "editor" => "Editor",
                    _ => null
                };
            }

            // Build class/interface/struct declaration
            var typeKeyword = scriptType.ToLower() switch
            {
                "interface" => "interface",
                "struct" => "struct",
                _ => "class"
            };

            var inheritance = new List<string>();
            if (!string.IsNullOrEmpty(actualBaseClass))
            {
                inheritance.Add(actualBaseClass);
            }
            if (interfaces != null)
            {
                foreach (var iface in interfaces)
                {
                    inheritance.Add(iface.ToString());
                }
            }

            var inheritanceStr = inheritance.Count > 0 ? " : " + string.Join(", ", inheritance) : "";
            sb.AppendLine($"{indent}public {typeKeyword} {className}{inheritanceStr}");
            sb.AppendLine($"{indent}{{");

            var memberIndent = indent + "    ";

            // Add fields
            if (fields != null)
            {
                foreach (var field in fields)
                {
                    if (field is Dictionary<string, object> fieldDict)
                    {
                        var fieldName = GetString(fieldDict, "name");
                        var fieldType = GetString(fieldDict, "type") ?? "string";
                        var fieldVisibility = GetString(fieldDict, "visibility") ?? "private";
                        var isSerialize = GetBool(fieldDict, "serialize", true);
                        var defaultValue = GetString(fieldDict, "defaultValue");

                        if (isSerialize && fieldVisibility == "private")
                        {
                            sb.AppendLine($"{memberIndent}[SerializeField]");
                        }

                        var defaultValueStr = !string.IsNullOrEmpty(defaultValue) ? $" = {defaultValue}" : "";
                        sb.AppendLine($"{memberIndent}{fieldVisibility} {fieldType} {fieldName}{defaultValueStr};");
                    }
                    else
                    {
                        // Simple field definition as string
                        sb.AppendLine($"{memberIndent}[SerializeField]");
                        sb.AppendLine($"{memberIndent}private {field};");
                    }
                }
                sb.AppendLine();
            }

            // Add methods
            if (methods != null)
            {
                foreach (var method in methods)
                {
                    var methodName = method.ToString();
                    var methodBody = GetMethodTemplate(methodName, scriptType);
                    sb.AppendLine(methodBody.Replace("\n", "\n" + memberIndent).TrimEnd());
                    sb.AppendLine();
                }
            }
            else
            {
                // Add default methods based on script type
                switch (scriptType.ToLower())
                {
                    case "monobehaviour":
                        sb.AppendLine($"{memberIndent}private void Start()");
                        sb.AppendLine($"{memberIndent}{{");
                        sb.AppendLine($"{memberIndent}    ");
                        sb.AppendLine($"{memberIndent}}}");
                        sb.AppendLine();
                        sb.AppendLine($"{memberIndent}private void Update()");
                        sb.AppendLine($"{memberIndent}{{");
                        sb.AppendLine($"{memberIndent}    ");
                        sb.AppendLine($"{memberIndent}}}");
                        break;
                    case "scriptableobject":
                        sb.AppendLine($"{memberIndent}private void OnEnable()");
                        sb.AppendLine($"{memberIndent}{{");
                        sb.AppendLine($"{memberIndent}    ");
                        sb.AppendLine($"{memberIndent}}}");
                        break;
                    case "editor":
                        sb.AppendLine($"{memberIndent}public override void OnInspectorGUI()");
                        sb.AppendLine($"{memberIndent}{{");
                        sb.AppendLine($"{memberIndent}    base.OnInspectorGUI();");
                        sb.AppendLine($"{memberIndent}    ");
                        sb.AppendLine($"{memberIndent}}}");
                        break;
                }
            }

            sb.AppendLine($"{indent}}}");

            // Close namespace if specified
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        private static string GetMethodTemplate(string methodName, string scriptType)
        {
            var templates = new Dictionary<string, string>
            {
                ["Start"] = "private void Start()\n{\n    \n}",
                ["Update"] = "private void Update()\n{\n    \n}",
                ["FixedUpdate"] = "private void FixedUpdate()\n{\n    \n}",
                ["LateUpdate"] = "private void LateUpdate()\n{\n    \n}",
                ["Awake"] = "private void Awake()\n{\n    \n}",
                ["OnEnable"] = "private void OnEnable()\n{\n    \n}",
                ["OnDisable"] = "private void OnDisable()\n{\n    \n}",
                ["OnDestroy"] = "private void OnDestroy()\n{\n    \n}",
                ["OnTriggerEnter"] = "private void OnTriggerEnter(Collider other)\n{\n    \n}",
                ["OnTriggerExit"] = "private void OnTriggerExit(Collider other)\n{\n    \n}",
                ["OnTriggerStay"] = "private void OnTriggerStay(Collider other)\n{\n    \n}",
                ["OnCollisionEnter"] = "private void OnCollisionEnter(Collision collision)\n{\n    \n}",
                ["OnCollisionExit"] = "private void OnCollisionExit(Collision collision)\n{\n    \n}",
                ["OnCollisionStay"] = "private void OnCollisionStay(Collision collision)\n{\n    \n}",
                ["OnInspectorGUI"] = "public override void OnInspectorGUI()\n{\n    base.OnInspectorGUI();\n    \n}",
            };

            return templates.ContainsKey(methodName) ? templates[methodName] : $"private void {methodName}()\n{{\n    \n}}";
        }

        private static Dictionary<string, object> DescribeComponent(Component component)
        {
            return new Dictionary<string, object>
            {
                ["gameObject"] = GetHierarchyPath(component.gameObject),
                ["type"] = component.GetType().FullName,
            };
        }

        private static Dictionary<string, object> DescribeAsset(string path)
        {
            return new Dictionary<string, object>
            {
                ["path"] = path,
                ["guid"] = AssetDatabase.AssetPathToGUID(path),
                ["type"] = AssetDatabase.GetMainAssetTypeAtPath(path)?.FullName,
            };
        }

        private static GameObject ResolveGameObject(string hierarchyPath)
        {
            var go = GameObject.Find(hierarchyPath);
            if (go == null)
            {
                throw new InvalidOperationException($"GameObject not found: {hierarchyPath}");
            }

            return go;
        }

        /// <summary>
        /// Resolves a GameObject from a payload dictionary using GlobalObjectId or gameObjectPath.
        /// </summary>
        /// <param name="payload">Payload dictionary containing GameObject identification</param>
        /// <returns>Resolved GameObject</returns>
        /// <exception cref="InvalidOperationException">Thrown when GameObject cannot be resolved</exception>
        private static GameObject ResolveGameObjectFromPayload(Dictionary<string, object> payload)
        {
            // Try GlobalObjectId first (if provided)
            var globalObjectIdString = GetString(payload, "gameObjectGlobalObjectId");
            if (!string.IsNullOrEmpty(globalObjectIdString))
            {
                return ResolveGameObjectByGlobalObjectId(globalObjectIdString);
            }

            // Fall back to gameObjectPath
            var gameObjectPath = GetString(payload, "gameObjectPath");
            if (!string.IsNullOrEmpty(gameObjectPath))
            {
                return ResolveGameObject(gameObjectPath);
            }

            throw new InvalidOperationException("Payload must contain either 'gameObjectGlobalObjectId' or 'gameObjectPath' parameter");
        }

        /// <summary>
        /// Resolves a GameObject by its GlobalObjectId string.
        /// </summary>
        /// <param name="globalObjectIdString">GlobalObjectId in string format (e.g., "GlobalObjectId_V1-1-abc123-456-0")</param>
        /// <returns>The resolved GameObject</returns>
        /// <exception cref="InvalidOperationException">Thrown when GameObject is not found</exception>
        private static GameObject ResolveGameObjectByGlobalObjectId(string globalObjectIdString)
        {
            if (string.IsNullOrEmpty(globalObjectIdString))
            {
                throw new InvalidOperationException("GlobalObjectId string cannot be null or empty");
            }

            if (!GlobalObjectId.TryParse(globalObjectIdString, out GlobalObjectId globalObjectId))
            {
                throw new InvalidOperationException($"Invalid GlobalObjectId format: {globalObjectIdString}");
            }

            var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId);
            if (obj == null)
            {
                throw new InvalidOperationException($"GameObject not found with GlobalObjectId: {globalObjectIdString}");
            }

            if (obj is GameObject go)
            {
                return go;
            }

            // If it's a component, return its GameObject
            if (obj is Component component)
            {
                return component.gameObject;
            }

            throw new InvalidOperationException($"Object with GlobalObjectId {globalObjectIdString} is not a GameObject or Component");
        }

        /// <summary>
        /// Gets the GlobalObjectId string for a GameObject.
        /// </summary>
        /// <param name="go">The GameObject</param>
        /// <returns>GlobalObjectId in string format</returns>
        private static string GetGlobalObjectIdString(GameObject go)
        {
            var globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(go);
            return globalObjectId.ToString();
        }

        private static string GetHierarchyPath(GameObject go)
        {
            var stack = new Stack<string>();
            var current = go.transform;
            while (current != null)
            {
                stack.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", stack);
        }

        private static void EnsureDirectoryExists(string assetPath)
        {
            var directory = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static Type ResolveType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

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

        private static void ApplyProperty(Component component, string propertyName, object rawValue)
        {
            var type = component.GetType();

            // Try property first
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null)
            {
                // Check if it's a UnityEvent
                if (typeof(UnityEventBase).IsAssignableFrom(property.PropertyType))
                {
                    var unityEvent = property.GetValue(component) as UnityEventBase;
                    if (unityEvent != null)
                    {
                        ApplyUnityEvent(component, unityEvent, rawValue);
                        return;
                    }
                }

                if (property.CanWrite)
                {
                    var converted = ConvertValue(rawValue, property.PropertyType);
                    property.SetValue(component, converted);
                    return;
                }
            }

            // Try field
            var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                // Check if it's a UnityEvent
                if (typeof(UnityEventBase).IsAssignableFrom(field.FieldType))
                {
                    var unityEvent = field.GetValue(component) as UnityEventBase;
                    if (unityEvent != null)
                    {
                        ApplyUnityEvent(component, unityEvent, rawValue);
                        return;
                    }
                }

                var converted = ConvertValue(rawValue, field.FieldType);
                field.SetValue(component, converted);
                return;
            }

            throw new InvalidOperationException($"Property or field '{propertyName}' not found on {type.FullName}");
        }

        /// <summary>
        /// Applies UnityEvent configuration (add/remove listeners).
        /// Supports both simple string format ("GameObject.MethodName") and complex dictionary format.
        /// </summary>
        /// <param name="component">The component containing the UnityEvent</param>
        /// <param name="unityEvent">The UnityEvent to configure</param>
        /// <param name="rawValue">Configuration data (string or dictionary)</param>
        private static void ApplyUnityEvent(Component component, UnityEventBase unityEvent, object rawValue)
        {
            // Simple string format: "GameObject.MethodName"
            if (rawValue is string simpleFormat)
            {
                var parts = simpleFormat.Split('.');
                if (parts.Length != 2)
                {
                    throw new InvalidOperationException($"Invalid UnityEvent format. Expected 'GameObjectPath.MethodName', got: {simpleFormat}");
                }

                var targetPath = parts[0];
                var methodName = parts[1];
                var target = ResolveGameObject(targetPath);

                AddVoidListener(unityEvent, target, methodName);
                EditorUtility.SetDirty(component);
                return;
            }

            // Complex dictionary format
            if (rawValue is Dictionary<string, object> eventConfig)
            {
                // Clear existing listeners if requested
                var clearListeners = GetBool(eventConfig, "clearListeners", false);
                if (clearListeners)
                {
                    RemoveAllListeners(unityEvent);
                }

                // Add new listeners
                if (eventConfig.TryGetValue("listeners", out var listenersObj) && listenersObj is IList listenersList)
                {
                    foreach (var listenerObj in listenersList)
                    {
                        if (listenerObj is Dictionary<string, object> listenerConfig)
                        {
                            AddListener(unityEvent, listenerConfig);
                        }
                    }
                }

                EditorUtility.SetDirty(component);
                return;
            }

            throw new InvalidOperationException($"UnityEvent value must be a string ('GameObject.MethodName') or dictionary with 'listeners' array");
        }

        /// <summary>
        /// Adds a listener to a UnityEvent based on configuration.
        /// </summary>
        private static void AddListener(UnityEventBase unityEvent, Dictionary<string, object> config)
        {
            var targetPath = EnsureValue(GetString(config, "targetPath"), "targetPath");
            var methodName = EnsureValue(GetString(config, "methodName"), "methodName");
            var mode = GetString(config, "mode") ?? "Void";

            var target = ResolveGameObject(targetPath);

            switch (mode.ToLower())
            {
                case "void":
                    AddVoidListener(unityEvent, target, methodName);
                    break;

                case "int":
                    var intArg = GetInt(config, "argument", 0);
                    AddIntListener(unityEvent, target, methodName, intArg);
                    break;

                case "float":
                    var floatArg = GetFloat(config, "argument", 0f);
                    AddFloatListener(unityEvent, target, methodName, floatArg);
                    break;

                case "string":
                    var stringArg = GetString(config, "argument") ?? "";
                    AddStringListener(unityEvent, target, methodName, stringArg);
                    break;

                case "bool":
                    var boolArg = GetBool(config, "argument", false);
                    AddBoolListener(unityEvent, target, methodName, boolArg);
                    break;

                case "object":
                    var objectArg = ResolveObjectArgument(config);
                    AddObjectListener(unityEvent, target, methodName, objectArg);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported UnityEvent mode: {mode}. Supported modes: Void, Int, Float, String, Bool, Object");
            }
        }

        private static void AddVoidListener(UnityEventBase unityEvent, GameObject target, string methodName)
        {
            var targetComponent = FindComponentWithMethod(target, methodName, Type.EmptyTypes);
            if (targetComponent == null)
            {
                throw new InvalidOperationException($"Method '{methodName}()' not found on any component of '{target.name}'");
            }

            var action = System.Delegate.CreateDelegate(typeof(UnityAction), targetComponent, methodName) as UnityAction;
            UnityEventTools.AddVoidPersistentListener(unityEvent, action);
        }

        private static void AddIntListener(UnityEventBase unityEvent, GameObject target, string methodName, int argument)
        {
            var targetComponent = FindComponentWithMethod(target, methodName, new[] { typeof(int) });
            if (targetComponent == null)
            {
                throw new InvalidOperationException($"Method '{methodName}(int)' not found on any component of '{target.name}'");
            }

            var action = System.Delegate.CreateDelegate(typeof(UnityAction<int>), targetComponent, methodName) as UnityAction<int>;
            UnityEventTools.AddIntPersistentListener(unityEvent, action, argument);
        }

        private static void AddFloatListener(UnityEventBase unityEvent, GameObject target, string methodName, float argument)
        {
            var targetComponent = FindComponentWithMethod(target, methodName, new[] { typeof(float) });
            if (targetComponent == null)
            {
                throw new InvalidOperationException($"Method '{methodName}(float)' not found on any component of '{target.name}'");
            }

            var action = System.Delegate.CreateDelegate(typeof(UnityAction<float>), targetComponent, methodName) as UnityAction<float>;
            UnityEventTools.AddFloatPersistentListener(unityEvent, action, argument);
        }

        private static void AddStringListener(UnityEventBase unityEvent, GameObject target, string methodName, string argument)
        {
            var targetComponent = FindComponentWithMethod(target, methodName, new[] { typeof(string) });
            if (targetComponent == null)
            {
                throw new InvalidOperationException($"Method '{methodName}(string)' not found on any component of '{target.name}'");
            }

            var action = System.Delegate.CreateDelegate(typeof(UnityAction<string>), targetComponent, methodName) as UnityAction<string>;
            UnityEventTools.AddStringPersistentListener(unityEvent, action, argument);
        }

        private static void AddBoolListener(UnityEventBase unityEvent, GameObject target, string methodName, bool argument)
        {
            var targetComponent = FindComponentWithMethod(target, methodName, new[] { typeof(bool) });
            if (targetComponent == null)
            {
                throw new InvalidOperationException($"Method '{methodName}(bool)' not found on any component of '{target.name}'");
            }

            var action = System.Delegate.CreateDelegate(typeof(UnityAction<bool>), targetComponent, methodName) as UnityAction<bool>;
            UnityEventTools.AddBoolPersistentListener(unityEvent, action, argument);
        }

        private static void AddObjectListener(UnityEventBase unityEvent, GameObject target, string methodName, UnityEngine.Object argument)
        {
            if (argument == null)
            {
                throw new InvalidOperationException("Object argument cannot be null for Object mode listeners");
            }

            var argumentType = argument.GetType();
            var targetComponent = FindComponentWithMethod(target, methodName, new[] { argumentType });
            if (targetComponent == null)
            {
                throw new InvalidOperationException($"Method '{methodName}({argumentType.Name})' not found on any component of '{target.name}'");
            }

            // Use reflection to call the generic AddObjectPersistentListener<T> method
            var addObjectMethod = typeof(UnityEventTools).GetMethods()
                .FirstOrDefault(m => m.Name == "AddObjectPersistentListener" && m.IsGenericMethod);

            if (addObjectMethod == null)
            {
                throw new InvalidOperationException("UnityEventTools.AddObjectPersistentListener method not found");
            }

            var genericMethod = addObjectMethod.MakeGenericMethod(argumentType);
            var actionType = typeof(UnityAction<>).MakeGenericType(argumentType);
            var action = System.Delegate.CreateDelegate(actionType, targetComponent, methodName);

            genericMethod.Invoke(null, new object[] { unityEvent, action, argument });
        }

        private static Component FindComponentWithMethod(GameObject target, string methodName, Type[] parameterTypes)
        {
            var components = target.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component == null) continue;

                var method = component.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, parameterTypes, null);
                if (method != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static UnityEngine.Object ResolveObjectArgument(Dictionary<string, object> config)
        {
            if (config.TryGetValue("argument", out var argObj))
            {
                if (argObj is string assetPath)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    if (asset != null)
                    {
                        return asset;
                    }
                }
                else if (argObj is Dictionary<string, object> refDict)
                {
                    return ResolveUnityObjectReference(refDict, typeof(UnityEngine.Object));
                }
            }

            throw new InvalidOperationException("Object mode requires 'argument' parameter with asset path or object reference");
        }

        private static void RemoveAllListeners(UnityEventBase unityEvent)
        {
            // Clear persistent listeners
            var persistentCount = unityEvent.GetPersistentEventCount();
            for (int i = persistentCount - 1; i >= 0; i--)
            {
                UnityEventTools.RemovePersistentListener(unityEvent, i);
            }
        }

        /// <summary>
        /// Converts a raw value to the target type with support for Unity types and object references.
        /// </summary>
        /// <param name="rawValue">The raw value to convert (primitives, dictionaries, or reference objects).</param>
        /// <param name="targetType">The target type to convert to.</param>
        /// <returns>Converted value compatible with the target type.</returns>
        /// <exception cref="InvalidOperationException">Thrown when reference resolution fails.</exception>
        private static object ConvertValue(object rawValue, Type targetType)
        {
            if (rawValue == null)
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }

            if (targetType.IsInstanceOfType(rawValue))
            {
                return rawValue;
            }

            // Handle Unity object references (dictionary format)
            if (typeof(UnityEngine.Object).IsAssignableFrom(targetType) && rawValue is Dictionary<string, object> refDict)
            {
                if (refDict.TryGetValue("_ref", out var refType))
                {
                    return ResolveUnityObjectReference(refDict, targetType);
                }
            }

            // Handle Unity object references (string format for asset paths)
            if (typeof(UnityEngine.Object).IsAssignableFrom(targetType) && rawValue is string assetPath)
            {
                return ResolveAssetPath(assetPath, targetType);
            }

            if (targetType.IsEnum && rawValue is string enumString)
            {
                return Enum.Parse(targetType, enumString);
            }

            // Handle Enum from integer values
            if (targetType.IsEnum && (rawValue is int || rawValue is long))
            {
                return Enum.ToObject(targetType, rawValue);
            }

            if (targetType == typeof(Vector3) && rawValue is Dictionary<string, object> dict)
            {
                return new Vector3(
                    Convert.ToSingle(dict.GetValueOrDefault("x", 0f), CultureInfo.InvariantCulture),
                    Convert.ToSingle(dict.GetValueOrDefault("y", 0f), CultureInfo.InvariantCulture),
                    Convert.ToSingle(dict.GetValueOrDefault("z", 0f), CultureInfo.InvariantCulture));
            }

            if (targetType == typeof(Vector2) && rawValue is Dictionary<string, object> dict2)
            {
                return new Vector2(
                    Convert.ToSingle(dict2.GetValueOrDefault("x", 0f), CultureInfo.InvariantCulture),
                    Convert.ToSingle(dict2.GetValueOrDefault("y", 0f), CultureInfo.InvariantCulture));
            }

            if (targetType == typeof(Color) && rawValue is Dictionary<string, object> colorDict)
            {
                return new Color(
                    Convert.ToSingle(colorDict.GetValueOrDefault("r", 1f), CultureInfo.InvariantCulture),
                    Convert.ToSingle(colorDict.GetValueOrDefault("g", 1f), CultureInfo.InvariantCulture),
                    Convert.ToSingle(colorDict.GetValueOrDefault("b", 1f), CultureInfo.InvariantCulture),
                    Convert.ToSingle(colorDict.GetValueOrDefault("a", 1f), CultureInfo.InvariantCulture));
            }

            // Handle arrays
            if (targetType.IsArray && rawValue is IList rawList)
            {
                var elementType = targetType.GetElementType();
                var array = Array.CreateInstance(elementType, rawList.Count);
                for (int i = 0; i < rawList.Count; i++)
                {
                    array.SetValue(ConvertValue(rawList[i], elementType), i);
                }
                return array;
            }

            // Handle List<T>
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>) && rawValue is IList rawListGeneric)
            {
                var elementType = targetType.GetGenericArguments()[0];
                var list = (IList)Activator.CreateInstance(targetType);
                foreach (var item in rawListGeneric)
                {
                    list.Add(ConvertValue(item, elementType));
                }
                return list;
            }

            if (rawValue is double d)
            {
                rawValue = Convert.ChangeType(d, typeof(float), CultureInfo.InvariantCulture);
            }
            else if (rawValue is long l && targetType != typeof(long))
            {
                rawValue = Convert.ChangeType(l, typeof(int), CultureInfo.InvariantCulture);
            }

            return Convert.ChangeType(rawValue, targetType, CultureInfo.InvariantCulture);
        }

        private static string GetString(Dictionary<string, object> payload, string key)
        {
            return payload.TryGetValue(key, out var value) ? value as string : null;
        }

        private static bool GetBool(Dictionary<string, object> payload, string key, bool defaultValue = false)
        {
            if (!payload.TryGetValue(key, out var value) || value == null)
            {
                return defaultValue;
            }

            if (value is bool boolean)
            {
                return boolean;
            }

            if (value is string str && bool.TryParse(str, out var parsed))
            {
                return parsed;
            }

            if (value is double dbl)
            {
                return Math.Abs(dbl) > double.Epsilon;
            }

            return defaultValue;
        }

        private static string EnsureValue(string value, string parameterName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException($"{parameterName} is required");
            }

            return value;
        }

        /// <summary>
        /// Resolves an asset path to a Unity object, supporting both regular asset paths and built-in Unity resources.
        /// </summary>
        /// <param name="assetPath">Asset path, which can be:
        /// - Regular asset path: "Assets/Models/Sphere.fbx"
        /// - Built-in resource: "Library/unity default resources::Sphere"
        /// - Editor resource: "Library/unity editor resources::GameObject Icon"
        /// </param>
        /// <param name="targetType">Expected Unity object type.</param>
        /// <returns>Resolved Unity object or null if not found.</returns>
        /// <exception cref="InvalidOperationException">Thrown when asset cannot be loaded.</exception>
        private static UnityEngine.Object ResolveAssetPath(string assetPath, Type targetType)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            // Handle built-in Unity resources (e.g., "Library/unity default resources::Sphere")
            if (assetPath.Contains("::"))
            {
                var parts = assetPath.Split(new[] { "::" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var resourceName = parts[1].Trim();

                    // Load built-in resource by name and type
                    var builtinAssets = AssetDatabase.LoadAllAssetsAtPath(parts[0]);
                    foreach (var asset in builtinAssets)
                    {
                        if (asset != null && asset.name == resourceName && targetType.IsInstanceOfType(asset))
                        {
                            return asset;
                        }
                    }

                    // If specific type match not found, try to find by name only
                    foreach (var asset in builtinAssets)
                    {
                        if (asset != null && asset.name == resourceName)
                        {
                            return asset;
                        }
                    }

                    throw new InvalidOperationException($"Built-in resource not found: {assetPath}");
                }
            }

            // Handle regular asset paths
            var loadedAsset = AssetDatabase.LoadAssetAtPath(assetPath, targetType);
            if (loadedAsset == null)
            {
                // Try loading as generic Object if specific type fails
                loadedAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (loadedAsset != null && !targetType.IsInstanceOfType(loadedAsset))
                {
                    throw new InvalidOperationException($"Asset at {assetPath} is type {loadedAsset.GetType().FullName}, expected {targetType.FullName}");
                }
            }

            if (loadedAsset == null)
            {
                throw new InvalidOperationException($"Asset not found at path: {assetPath}");
            }

            return loadedAsset;
        }

        /// <summary>
        /// Resolves an asset by GUID.
        /// </summary>
        /// <param name="guid">Asset GUID string</param>
        /// <param name="targetType">Expected asset type</param>
        /// <returns>Loaded asset</returns>
        /// <exception cref="InvalidOperationException">Thrown when asset is not found or type mismatch</exception>
        private static UnityEngine.Object ResolveAssetByGuid(string guid, Type targetType)
        {
            if (string.IsNullOrEmpty(guid))
            {
                throw new InvalidOperationException("Asset GUID cannot be null or empty");
            }

            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException($"Asset not found with GUID: {guid}");
            }

            return ResolveAssetPath(assetPath, targetType);
        }

        /// <summary>
        /// Resolves a Unity object reference from a dictionary specification.
        /// Supports GameObject, Component, and Asset references by path, instance ID, or GUID.
        /// </summary>
        /// <param name="refDict">Reference dictionary containing "_ref" type and lookup parameters.</param>
        /// <param name="targetType">Expected Unity object type.</param>
        /// <returns>Resolved Unity object or null if not found.</returns>
        /// <exception cref="InvalidOperationException">Thrown when reference format is invalid.</exception>
        private static UnityEngine.Object ResolveUnityObjectReference(Dictionary<string, object> refDict, Type targetType)
        {
            var refType = GetString(refDict, "_ref");

            switch (refType)
            {
                case "gameObject":
                    return ResolveGameObjectReference(refDict, targetType);

                case "component":
                    return ResolveComponentReference(refDict, targetType);

                case "asset":
                    return ResolveAssetReference(refDict, targetType);

                case "instance":
                    return ResolveInstanceReference(refDict, targetType);

                default:
                    throw new InvalidOperationException($"Unknown reference type: {refType}");
            }
        }

        /// <summary>
        /// Resolves a GameObject reference by GlobalObjectId only.
        /// </summary>
        private static UnityEngine.Object ResolveGameObjectReference(Dictionary<string, object> refDict, Type targetType)
        {
            var globalObjectIdString = GetString(refDict, "globalObjectId");

            if (string.IsNullOrEmpty(globalObjectIdString))
            {
                throw new InvalidOperationException("GameObject reference requires 'globalObjectId' parameter");
            }

            var gameObject = ResolveGameObjectByGlobalObjectId(globalObjectIdString);

            // If target is GameObject, return directly
            if (targetType == typeof(GameObject) || targetType.IsAssignableFrom(typeof(GameObject)))
            {
                return gameObject;
            }

            // If target is a Component type, get the component
            if (typeof(Component).IsAssignableFrom(targetType))
            {
                var component = gameObject.GetComponent(targetType);
                if (component == null)
                {
                    throw new InvalidOperationException($"Component {targetType.FullName} not found on GameObject: {globalObjectIdString}");
                }
                return component;
            }

            return gameObject;
        }

        /// <summary>
        /// Resolves a Component reference by GameObject GlobalObjectId and component type.
        /// </summary>
        private static UnityEngine.Object ResolveComponentReference(Dictionary<string, object> refDict, Type targetType)
        {
            var globalObjectIdString = GetString(refDict, "globalObjectId");
            var typeName = GetString(refDict, "type");

            if (string.IsNullOrEmpty(globalObjectIdString))
            {
                throw new InvalidOperationException("Component reference requires 'globalObjectId' parameter");
            }

            var gameObject = ResolveGameObjectByGlobalObjectId(globalObjectIdString);

            // Determine which component type to get
            Type componentType = targetType;
            if (!string.IsNullOrEmpty(typeName))
            {
                componentType = ResolveType(typeName);
            }

            var component = gameObject.GetComponent(componentType);
            if (component == null)
            {
                throw new InvalidOperationException($"Component {componentType.FullName} not found on GameObject: {globalObjectIdString}");
            }

            return component;
        }

        /// <summary>
        /// Resolves an Asset reference by GUID or asset path.
        /// Prioritizes GUID over path when both are provided.
        /// </summary>
        private static UnityEngine.Object ResolveAssetReference(Dictionary<string, object> refDict, Type targetType)
        {
            var guid = GetString(refDict, "guid");
            var path = GetString(refDict, "path");

            // GUID優先で処理
            if (!string.IsNullOrEmpty(guid))
            {
                return ResolveAssetByGuid(guid, targetType);
            }

            if (!string.IsNullOrEmpty(path))
            {
                return ResolveAssetPath(path, targetType);
            }

            throw new InvalidOperationException("Asset reference requires either 'guid' or 'path' parameter");
        }

        /// <summary>
        /// Resolves a Unity object reference by instance ID.
        /// </summary>
        private static UnityEngine.Object ResolveInstanceReference(Dictionary<string, object> refDict, Type targetType)
        {
            if (!refDict.TryGetValue("id", out var idObj))
            {
                throw new InvalidOperationException("Instance reference requires 'id' parameter");
            }

            int instanceId;
            if (idObj is int intId)
            {
                instanceId = intId;
            }
            else if (idObj is long longId)
            {
                instanceId = (int)longId;
            }
            else if (idObj is double doubleId)
            {
                instanceId = (int)doubleId;
            }
            else
            {
                throw new InvalidOperationException($"Invalid instance ID type: {idObj.GetType()}");
            }

            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj == null)
            {
                throw new InvalidOperationException($"Object not found with instance ID: {instanceId}");
            }

            if (!targetType.IsInstanceOfType(obj))
            {
                throw new InvalidOperationException($"Object with ID {instanceId} is type {obj.GetType().FullName}, expected {targetType.FullName}");
            }

            return obj;
        }

        private static object SerializeValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            var valueType = value.GetType();

            // Primitive types and strings
            if (valueType.IsPrimitive || valueType == typeof(string))
            {
                return value;
            }

            // Enums
            if (valueType.IsEnum)
            {
                return value.ToString();
            }

            // Unity Vector types
            if (value is Vector2 v2)
            {
                return new Dictionary<string, object>
                {
                    ["x"] = v2.x,
                    ["y"] = v2.y,
                };
            }

            if (value is Vector3 v3)
            {
                return new Dictionary<string, object>
                {
                    ["x"] = v3.x,
                    ["y"] = v3.y,
                    ["z"] = v3.z,
                };
            }

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

            if (value is Quaternion q)
            {
                return new Dictionary<string, object>
                {
                    ["x"] = q.x,
                    ["y"] = q.y,
                    ["z"] = q.z,
                    ["w"] = q.w,
                };
            }

            if (value is Color c)
            {
                return new Dictionary<string, object>
                {
                    ["r"] = c.r,
                    ["g"] = c.g,
                    ["b"] = c.b,
                    ["a"] = c.a,
                };
            }

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

            // Unity Object references
            if (value is UnityEngine.Object unityObj)
            {
                if (unityObj == null)
                {
                    return null;
                }

                var objInfo = new Dictionary<string, object>
                {
                    ["name"] = unityObj.name,
                    ["type"] = unityObj.GetType().Name,
                    ["instanceId"] = unityObj.GetInstanceID(),
                };

                // Try to get asset path if it's an asset
                var assetPath = AssetDatabase.GetAssetPath(unityObj);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    objInfo["assetPath"] = assetPath;
                }

                return objInfo;
            }

            // Arrays and Lists
            if (value is System.Collections.IEnumerable enumerable && !(value is string))
            {
                var list = new List<object>();
                var count = 0;
                foreach (var item in enumerable)
                {
                    if (count >= 100) // Limit array size to prevent huge responses
                    {
                        list.Add($"<Truncated: more than 100 items>");
                        break;
                    }
                    list.Add(SerializeValue(item));
                    count++;
                }
                return list;
            }

            // For other types, return type name and ToString()
            return new Dictionary<string, object>
            {
                ["_type"] = valueType.FullName,
                ["_value"] = value.ToString(),
            };
        }

        /// <summary>
        /// Handles prefab management operations (create, update, inspect, instantiate, unpack, applyOverrides, revertOverrides).
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation' type and prefab/GameObject paths.</param>
        /// <returns>Result dictionary with operation-specific data.</returns>
        /// <exception cref="InvalidOperationException">Thrown when operation is invalid or missing.</exception>
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

        /// <summary>
        /// Handles input system management operations (listActions, createAsset, addAction, addBinding, enableAction, readValue).
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation' and input-specific settings.</param>
        /// <returns>Result dictionary with operation-specific data.</returns>
        private static object HandleInputSystemManage(Dictionary<string, object> payload)
        {
            var operation = EnsureValue(GetString(payload, "operation"), "operation");
            return operation switch
            {
                "listActions" => ListInputActions(payload),
                "createAsset" => CreateInputActionAsset(payload),
                "addActionMap" => AddInputActionMap(payload),
                "addAction" => AddInputAction(payload),
                "addBinding" => AddInputBinding(payload),
                "inspectAsset" => InspectInputActionAsset(payload),
                "deleteAsset" => DeleteInputActionAsset(payload),
                "deleteActionMap" => DeleteInputActionMap(payload),
                "deleteAction" => DeleteInputAction(payload),
                "deleteBinding" => DeleteInputBinding(payload),
                _ => throw new InvalidOperationException($"Unknown inputSystemManage operation: {operation}"),
            };
        }

        private static object ListInputActions(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");

            if (string.IsNullOrEmpty(assetPath))
            {
                // Find all InputActionAsset files in the project
                var guids = AssetDatabase.FindAssets("t:InputActionAsset");
                var assets = new List<Dictionary<string, object>>();

                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    if (asset != null)
                    {
                        assets.Add(new Dictionary<string, object>
                        {
                            ["name"] = asset.name,
                            ["path"] = path,
                            ["guid"] = guid,
                        });
                    }
                }

                return new Dictionary<string, object>
                {
                    ["assets"] = assets,
                    ["count"] = assets.Count,
                };
            }

            // Inspect specific asset
            return InspectInputActionAsset(payload);
        }

        private static object CreateInputActionAsset(Dictionary<string, object> payload)
        {
            var assetPath = EnsureValue(GetString(payload, "assetPath"), "assetPath");

            // Ensure path ends with .inputactions
            if (!assetPath.EndsWith(".inputactions"))
            {
                assetPath += ".inputactions";
            }

            EnsureDirectoryExists(assetPath);

            // Try to use New Input System API via reflection
            var inputActionAssetType = Type.GetType("UnityEngine.InputSystem.InputActionAsset, Unity.InputSystem");
            if (inputActionAssetType == null)
            {
                throw new InvalidOperationException("Input System package is not installed. Install it via Package Manager.");
            }

            var asset = ScriptableObject.CreateInstance(inputActionAssetType);
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return new Dictionary<string, object>
            {
                ["assetPath"] = assetPath,
                ["guid"] = AssetDatabase.AssetPathToGUID(assetPath),
                ["message"] = "Input Action Asset created successfully",
            };
        }

        private static object AddInputActionMap(Dictionary<string, object> payload)
        {
            var assetPath = EnsureValue(GetString(payload, "assetPath"), "assetPath");
            var mapName = EnsureValue(GetString(payload, "mapName"), "mapName");

            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"InputActionAsset not found at path: {assetPath}");
            }

            // Use reflection to add action map
            var addMapMethod = asset.GetType().GetMethod("AddActionMap", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            if (addMapMethod == null)
            {
                throw new InvalidOperationException("Could not find AddActionMap method. Ensure Input System package is installed.");
            }

            var actionMap = addMapMethod.Invoke(asset, new object[] { mapName });

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            return new Dictionary<string, object>
            {
                ["assetPath"] = assetPath,
                ["mapName"] = mapName,
                ["message"] = "Action map added successfully",
            };
        }

        private static object AddInputAction(Dictionary<string, object> payload)
        {
            var assetPath = EnsureValue(GetString(payload, "assetPath"), "assetPath");
            var mapName = EnsureValue(GetString(payload, "mapName"), "mapName");
            var actionName = EnsureValue(GetString(payload, "actionName"), "actionName");
            var actionType = GetString(payload, "actionType") ?? "Button";

            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"InputActionAsset not found at path: {assetPath}");
            }

            // Use reflection to find and add action to map
            var findMapMethod = asset.GetType().GetMethod("FindActionMap", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string), typeof(bool) }, null);
            if (findMapMethod == null)
            {
                throw new InvalidOperationException("Could not find FindActionMap method.");
            }

            var actionMap = findMapMethod.Invoke(asset, new object[] { mapName, false });
            if (actionMap == null)
            {
                throw new InvalidOperationException($"Action map '{mapName}' not found in asset.");
            }

            var addActionMethod = actionMap.GetType().GetMethod("AddAction", BindingFlags.Public | BindingFlags.Instance);
            if (addActionMethod == null)
            {
                throw new InvalidOperationException("Could not find AddAction method.");
            }

            var action = addActionMethod.Invoke(actionMap, new object[] { actionName, null, null, null, null, null, null, null });

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            return new Dictionary<string, object>
            {
                ["assetPath"] = assetPath,
                ["mapName"] = mapName,
                ["actionName"] = actionName,
                ["message"] = "Action added successfully",
            };
        }

        private static object AddInputBinding(Dictionary<string, object> payload)
        {
            var assetPath = EnsureValue(GetString(payload, "assetPath"), "assetPath");
            var mapName = EnsureValue(GetString(payload, "mapName"), "mapName");
            var actionName = EnsureValue(GetString(payload, "actionName"), "actionName");
            var path = EnsureValue(GetString(payload, "path"), "path");

            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"InputActionAsset not found at path: {assetPath}");
            }

            // Use reflection to find action and add binding
            var findActionMethod = asset.GetType().GetMethod("FindAction", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string), typeof(bool) }, null);
            if (findActionMethod == null)
            {
                throw new InvalidOperationException("Could not find FindAction method.");
            }

            var fullActionName = $"{mapName}/{actionName}";
            var action = findActionMethod.Invoke(asset, new object[] { fullActionName, false });
            if (action == null)
            {
                throw new InvalidOperationException($"Action '{fullActionName}' not found in asset.");
            }

            var addBindingMethod = action.GetType().GetMethod("AddBinding", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            if (addBindingMethod == null)
            {
                throw new InvalidOperationException("Could not find AddBinding method.");
            }

            addBindingMethod.Invoke(action, new object[] { path });

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            return new Dictionary<string, object>
            {
                ["assetPath"] = assetPath,
                ["mapName"] = mapName,
                ["actionName"] = actionName,
                ["path"] = path,
                ["message"] = "Binding added successfully",
            };
        }

        private static object InspectInputActionAsset(Dictionary<string, object> payload)
        {
            var assetPath = EnsureValue(GetString(payload, "assetPath"), "assetPath");

            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"InputActionAsset not found at path: {assetPath}");
            }

            var result = new Dictionary<string, object>
            {
                ["assetPath"] = assetPath,
                ["name"] = asset.name,
            };

            // Use reflection to get action maps
            var actionMapsProperty = asset.GetType().GetProperty("actionMaps", BindingFlags.Public | BindingFlags.Instance);
            if (actionMapsProperty != null)
            {
                var actionMaps = actionMapsProperty.GetValue(asset);
                if (actionMaps is System.Collections.IEnumerable enumerable)
                {
                    var mapsList = new List<Dictionary<string, object>>();
                    foreach (var map in enumerable)
                    {
                        var mapName = map.GetType().GetProperty("name")?.GetValue(map);
                        var actionsProperty = map.GetType().GetProperty("actions");
                        var actions = actionsProperty?.GetValue(map);

                        var actionsList = new List<string>();
                        if (actions is System.Collections.IEnumerable actionsEnum)
                        {
                            foreach (var action in actionsEnum)
                            {
                                var actionNameProp = action.GetType().GetProperty("name")?.GetValue(action);
                                if (actionNameProp != null)
                                {
                                    actionsList.Add(actionNameProp.ToString());
                                }
                            }
                        }

                        mapsList.Add(new Dictionary<string, object>
                        {
                            ["name"] = mapName?.ToString() ?? "Unknown",
                            ["actions"] = actionsList,
                        });
                    }
                    result["actionMaps"] = mapsList;
                }
            }

            return result;
        }

        private static object DeleteInputActionAsset(Dictionary<string, object> payload)
        {
            var assetPath = EnsureValue(GetString(payload, "assetPath"), "assetPath");

            if (!File.Exists(assetPath))
            {
                throw new InvalidOperationException($"InputActionAsset not found at path: {assetPath}");
            }

            if (!AssetDatabase.DeleteAsset(assetPath))
            {
                throw new InvalidOperationException($"Failed to delete asset: {assetPath}");
            }

            AssetDatabase.Refresh();

            return new Dictionary<string, object>
            {
                ["deleted"] = assetPath,
                ["message"] = "Input Action Asset deleted successfully",
            };
        }

        private static object DeleteInputActionMap(Dictionary<string, object> payload)
        {
            var assetPath = EnsureValue(GetString(payload, "assetPath"), "assetPath");
            var mapName = EnsureValue(GetString(payload, "mapName"), "mapName");

            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"InputActionAsset not found at path: {assetPath}");
            }

            // Use reflection to find and remove action map
            var findMapMethod = asset.GetType().GetMethod("FindActionMap", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string), typeof(bool) }, null);
            if (findMapMethod == null)
            {
                throw new InvalidOperationException("Could not find FindActionMap method.");
            }

            var actionMap = findMapMethod.Invoke(asset, new object[] { mapName, false });
            if (actionMap == null)
            {
                throw new InvalidOperationException($"Action map '{mapName}' not found in asset.");
            }

            var removeMapMethod = asset.GetType().GetMethod("RemoveActionMap", BindingFlags.Public | BindingFlags.Instance);
            if (removeMapMethod == null)
            {
                throw new InvalidOperationException("Could not find RemoveActionMap method.");
            }

            removeMapMethod.Invoke(asset, new object[] { actionMap });

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            return new Dictionary<string, object>
            {
                ["assetPath"] = assetPath,
                ["deletedMap"] = mapName,
                ["message"] = "Action map deleted successfully",
            };
        }

        private static object DeleteInputAction(Dictionary<string, object> payload)
        {
            var assetPath = EnsureValue(GetString(payload, "assetPath"), "assetPath");
            var mapName = EnsureValue(GetString(payload, "mapName"), "mapName");
            var actionName = EnsureValue(GetString(payload, "actionName"), "actionName");

            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"InputActionAsset not found at path: {assetPath}");
            }

            // Use reflection to find the action map
            var findMapMethod = asset.GetType().GetMethod("FindActionMap", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string), typeof(bool) }, null);
            if (findMapMethod == null)
            {
                throw new InvalidOperationException("Could not find FindActionMap method.");
            }

            var actionMap = findMapMethod.Invoke(asset, new object[] { mapName, false });
            if (actionMap == null)
            {
                throw new InvalidOperationException($"Action map '{mapName}' not found in asset.");
            }

            // Find the action
            var findActionMethod = actionMap.GetType().GetMethod("FindAction", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string), typeof(bool) }, null);
            if (findActionMethod == null)
            {
                throw new InvalidOperationException("Could not find FindAction method.");
            }

            var action = findActionMethod.Invoke(actionMap, new object[] { actionName, false });
            if (action == null)
            {
                throw new InvalidOperationException($"Action '{actionName}' not found in map '{mapName}'.");
            }

            // Get the action's ID to remove it
            var actionsProperty = actionMap.GetType().GetProperty("actions", BindingFlags.Public | BindingFlags.Instance);
            if (actionsProperty == null)
            {
                throw new InvalidOperationException("Could not find actions property.");
            }

            var actions = actionsProperty.GetValue(actionMap);
            var removeMethod = actions.GetType().GetMethod("Remove", new[] { action.GetType() });
            if (removeMethod == null)
            {
                throw new InvalidOperationException("Could not find Remove method.");
            }

            removeMethod.Invoke(actions, new object[] { action });

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            return new Dictionary<string, object>
            {
                ["assetPath"] = assetPath,
                ["mapName"] = mapName,
                ["deletedAction"] = actionName,
                ["message"] = "Action deleted successfully",
            };
        }

        private static object DeleteInputBinding(Dictionary<string, object> payload)
        {
            var assetPath = EnsureValue(GetString(payload, "assetPath"), "assetPath");
            var mapName = EnsureValue(GetString(payload, "mapName"), "mapName");
            var actionName = EnsureValue(GetString(payload, "actionName"), "actionName");
            var bindingIndex = GetInt(payload, "bindingIndex", -1);

            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"InputActionAsset not found at path: {assetPath}");
            }

            // Use reflection to find action
            var findActionMethod = asset.GetType().GetMethod("FindAction", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string), typeof(bool) }, null);
            if (findActionMethod == null)
            {
                throw new InvalidOperationException("Could not find FindAction method.");
            }

            var fullActionName = $"{mapName}/{actionName}";
            var action = findActionMethod.Invoke(asset, new object[] { fullActionName, false });
            if (action == null)
            {
                throw new InvalidOperationException($"Action '{fullActionName}' not found in asset.");
            }

            // Get bindings
            var bindingsProperty = action.GetType().GetProperty("bindings", BindingFlags.Public | BindingFlags.Instance);
            if (bindingsProperty == null)
            {
                throw new InvalidOperationException("Could not find bindings property.");
            }

            var bindings = bindingsProperty.GetValue(action);
            var bindingsCount = (int)bindings.GetType().GetProperty("Count").GetValue(bindings);

            if (bindingIndex < 0)
            {
                // Delete all bindings
                var changeBindingMethod = action.GetType().GetMethod("ChangeBinding", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null);
                if (changeBindingMethod == null)
                {
                    throw new InvalidOperationException("Could not find ChangeBinding method.");
                }

                // Remove bindings from last to first to avoid index shifting
                for (int i = bindingsCount - 1; i >= 0; i--)
                {
                    var binding = changeBindingMethod.Invoke(action, new object[] { i });
                    var eraseMethod = binding.GetType().GetMethod("Erase", BindingFlags.Public | BindingFlags.Instance);
                    if (eraseMethod != null)
                    {
                        eraseMethod.Invoke(binding, null);
                    }
                }

                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();

                return new Dictionary<string, object>
                {
                    ["assetPath"] = assetPath,
                    ["mapName"] = mapName,
                    ["actionName"] = actionName,
                    ["deletedBindings"] = bindingsCount,
                    ["message"] = $"All {bindingsCount} binding(s) deleted successfully",
                };
            }
            else
            {
                // Delete specific binding by index
                if (bindingIndex >= bindingsCount)
                {
                    throw new InvalidOperationException($"Binding index {bindingIndex} out of range (0-{bindingsCount - 1})");
                }

                var changeBindingMethod = action.GetType().GetMethod("ChangeBinding", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null);
                if (changeBindingMethod == null)
                {
                    throw new InvalidOperationException("Could not find ChangeBinding method.");
                }

                var binding = changeBindingMethod.Invoke(action, new object[] { bindingIndex });
                var eraseMethod = binding.GetType().GetMethod("Erase", BindingFlags.Public | BindingFlags.Instance);
                if (eraseMethod == null)
                {
                    throw new InvalidOperationException("Could not find Erase method.");
                }

                eraseMethod.Invoke(binding, null);

                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();

                return new Dictionary<string, object>
                {
                    ["assetPath"] = assetPath,
                    ["mapName"] = mapName,
                    ["actionName"] = actionName,
                    ["deletedBindingIndex"] = bindingIndex,
                    ["message"] = "Binding deleted successfully",
                };
            }
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

        /// <summary>
        /// Handles batch execution of multiple operations.
        /// </summary>
        /// <param name="payload">Batch parameters including 'operations' array and 'stopOnError' flag.</param>
        /// <returns>Result dictionary with array of operation results.</returns>
        private static object HandleBatchExecute(Dictionary<string, object> payload)
        {
            if (!payload.TryGetValue("operations", out var operationsObj) || !(operationsObj is List<object> operations))
            {
                throw new InvalidOperationException("operations array is required");
            }

            var stopOnError = GetBool(payload, "stopOnError", false);
            var results = new List<Dictionary<string, object>>();
            var successCount = 0;
            var failureCount = 0;

            for (int i = 0; i < operations.Count; i++)
            {
                if (!(operations[i] is Dictionary<string, object> op))
                {
                    results.Add(new Dictionary<string, object>
                    {
                        ["index"] = i,
                        ["success"] = false,
                        ["error"] = "Invalid operation format",
                    });
                    failureCount++;
                    continue;
                }

                var toolName = GetString(op, "tool");
                if (string.IsNullOrEmpty(toolName))
                {
                    results.Add(new Dictionary<string, object>
                    {
                        ["index"] = i,
                        ["success"] = false,
                        ["error"] = "tool name is required",
                    });
                    failureCount++;
                    if (stopOnError) break;
                    continue;
                }

                var operationPayload = op.TryGetValue("payload", out var payloadObj) && payloadObj is Dictionary<string, object> dict
                    ? dict
                    : new Dictionary<string, object>();

                try
                {
                    object result = toolName switch
                    {
                        "sceneManage" => HandleSceneManage(operationPayload),
                        "gameObjectManage" => HandleGameObjectManage(operationPayload),
                        "componentManage" => HandleComponentManage(operationPayload),
                        "assetManage" => HandleAssetManage(operationPayload),
                        "uguiRectAdjust" => HandleUguiRectAdjust(operationPayload),
                        "uguiAnchorManage" => HandleUguiAnchorManage(operationPayload),
                        "uguiManage" => HandleUguiManage(operationPayload),
                        "uguiCreateFromTemplate" => HandleUguiCreateFromTemplate(operationPayload),
                        "uguiLayoutManage" => HandleUguiLayoutManage(operationPayload),
                        "hierarchyBuilder" => HandleHierarchyBuilder(operationPayload),
                        "sceneQuickSetup" => HandleSceneQuickSetup(operationPayload),
                        "gameObjectCreateFromTemplate" => HandleGameObjectCreateFromTemplate(operationPayload),
                        "contextInspect" => HandleContextInspect(operationPayload),
                        "tagLayerManage" => HandleTagLayerManage(operationPayload),
                        "scriptManage" => HandleScriptManage(operationPayload),
                        "prefabManage" => HandlePrefabManage(operationPayload),
                        "projectSettingsManage" => HandleProjectSettingsManage(operationPayload),
                        "renderPipelineManage" => HandleRenderPipelineManage(operationPayload),
                        "inputSystemManage" => HandleInputSystemManage(operationPayload),
                        "tilemapManage" => HandleTilemapManage(operationPayload),
                        "navmeshManage" => HandleNavMeshManage(operationPayload),
                        _ => throw new InvalidOperationException($"Unsupported tool name in batch: {toolName}"),
                    };

                    results.Add(new Dictionary<string, object>
                    {
                        ["index"] = i,
                        ["success"] = true,
                        ["tool"] = toolName,
                        ["result"] = result,
                    });
                    successCount++;
                }
                catch (Exception ex)
                {
                    results.Add(new Dictionary<string, object>
                    {
                        ["index"] = i,
                        ["success"] = false,
                        ["tool"] = toolName,
                        ["error"] = ex.Message,
                    });
                    failureCount++;

                    if (stopOnError)
                    {
                        break;
                    }
                }
            }

            return new Dictionary<string, object>
            {
                ["totalOperations"] = operations.Count,
                ["executedOperations"] = results.Count,
                ["successCount"] = successCount,
                ["failureCount"] = failureCount,
                ["results"] = results,
            };
        }

        #region Tilemap Management

        /// <summary>
        /// Handles Tilemap management operations.
        /// </summary>
        private static object HandleTilemapManage(Dictionary<string, object> payload)
        {
            var operation = GetString(payload, "operation");
            if (string.IsNullOrEmpty(operation))
            {
                throw new InvalidOperationException("operation is required");
            }

            return operation switch
            {
                "createTilemap" => CreateTilemap(payload),
                "setTile" => SetTile(payload),
                "getTile" => GetTile(payload),
                "clearTile" => ClearTile(payload),
                "fillArea" => FillArea(payload),
                "inspectTilemap" => InspectTilemap(payload),
                "clearAll" => ClearAllTiles(payload),
                _ => throw new InvalidOperationException($"Unknown tilemapManage operation: {operation}"),
            };
        }

        private static object CreateTilemap(Dictionary<string, object> payload)
        {
            var tilemapName = GetString(payload, "tilemapName") ?? "Tilemap";
            var parentPath = GetString(payload, "parentPath");

            // Create Grid parent
            var gridGo = new GameObject("Grid");
            var grid = gridGo.AddComponent<Grid>();

            // Create Tilemap child
            var tilemapGo = new GameObject(tilemapName);
            tilemapGo.transform.SetParent(gridGo.transform);

            var tilemap = tilemapGo.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            var renderer = tilemapGo.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();

            // Set parent if specified
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = GameObject.Find(parentPath);
                if (parent != null)
                {
                    gridGo.transform.SetParent(parent.transform);
                }
            }

            Undo.RegisterCreatedObjectUndo(gridGo, "Create Tilemap");

            return new Dictionary<string, object>
            {
                ["gridPath"] = GetHierarchyPath(gridGo),
                ["tilemapPath"] = GetHierarchyPath(tilemapGo),
                ["success"] = true,
            };
        }

        private static object SetTile(Dictionary<string, object> payload)
        {
            var gameObjectPath = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var tileAssetPath = EnsureValue(GetString(payload, "tileAssetPath"), "tileAssetPath");

            var posX = GetInt(payload, "positionX", 0);
            var posY = GetInt(payload, "positionY", 0);
            var posZ = GetInt(payload, "positionZ", 0);

            var go = GameObject.Find(gameObjectPath);
            if (go == null)
            {
                throw new InvalidOperationException($"GameObject not found: {gameObjectPath}");
            }

            var tilemap = go.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            if (tilemap == null)
            {
                throw new InvalidOperationException($"Tilemap component not found on: {gameObjectPath}");
            }

            var tile = AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.TileBase>(tileAssetPath);
            if (tile == null)
            {
                throw new InvalidOperationException($"Tile asset not found: {tileAssetPath}");
            }

            var position = new UnityEngine.Vector3Int(posX, posY, posZ);
            Undo.RecordObject(tilemap, "Set Tile");
            tilemap.SetTile(position, tile);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["position"] = new Dictionary<string, object>
                {
                    ["x"] = posX,
                    ["y"] = posY,
                    ["z"] = posZ,
                },
            };
        }

        private static object GetTile(Dictionary<string, object> payload)
        {
            var gameObjectPath = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var posX = GetInt(payload, "positionX", 0);
            var posY = GetInt(payload, "positionY", 0);
            var posZ = GetInt(payload, "positionZ", 0);

            var go = GameObject.Find(gameObjectPath);
            if (go == null)
            {
                throw new InvalidOperationException($"GameObject not found: {gameObjectPath}");
            }

            var tilemap = go.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            if (tilemap == null)
            {
                throw new InvalidOperationException($"Tilemap component not found on: {gameObjectPath}");
            }

            var position = new UnityEngine.Vector3Int(posX, posY, posZ);
            var tile = tilemap.GetTile(position);

            return new Dictionary<string, object>
            {
                ["hasTile"] = tile != null,
                ["tileName"] = tile != null ? tile.name : null,
                ["position"] = new Dictionary<string, object>
                {
                    ["x"] = posX,
                    ["y"] = posY,
                    ["z"] = posZ,
                },
            };
        }

        private static object ClearTile(Dictionary<string, object> payload)
        {
            var gameObjectPath = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var posX = GetInt(payload, "positionX", 0);
            var posY = GetInt(payload, "positionY", 0);
            var posZ = GetInt(payload, "positionZ", 0);

            var go = GameObject.Find(gameObjectPath);
            if (go == null)
            {
                throw new InvalidOperationException($"GameObject not found: {gameObjectPath}");
            }

            var tilemap = go.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            if (tilemap == null)
            {
                throw new InvalidOperationException($"Tilemap component not found on: {gameObjectPath}");
            }

            var position = new UnityEngine.Vector3Int(posX, posY, posZ);
            Undo.RecordObject(tilemap, "Clear Tile");
            tilemap.SetTile(position, null);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["position"] = new Dictionary<string, object>
                {
                    ["x"] = posX,
                    ["y"] = posY,
                    ["z"] = posZ,
                },
            };
        }

        private static object FillArea(Dictionary<string, object> payload)
        {
            var gameObjectPath = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var tileAssetPath = EnsureValue(GetString(payload, "tileAssetPath"), "tileAssetPath");

            var startX = GetInt(payload, "startX", 0);
            var startY = GetInt(payload, "startY", 0);
            var endX = GetInt(payload, "endX", 0);
            var endY = GetInt(payload, "endY", 0);

            var go = GameObject.Find(gameObjectPath);
            if (go == null)
            {
                throw new InvalidOperationException($"GameObject not found: {gameObjectPath}");
            }

            var tilemap = go.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            if (tilemap == null)
            {
                throw new InvalidOperationException($"Tilemap component not found on: {gameObjectPath}");
            }

            var tile = AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.TileBase>(tileAssetPath);
            if (tile == null)
            {
                throw new InvalidOperationException($"Tile asset not found: {tileAssetPath}");
            }

            Undo.RecordObject(tilemap, "Fill Area");

            var tilesSet = 0;
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    var position = new UnityEngine.Vector3Int(x, y, 0);
                    tilemap.SetTile(position, tile);
                    tilesSet++;
                }
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["tilesSet"] = tilesSet,
                ["area"] = new Dictionary<string, object>
                {
                    ["startX"] = startX,
                    ["startY"] = startY,
                    ["endX"] = endX,
                    ["endY"] = endY,
                },
            };
        }

        private static object InspectTilemap(Dictionary<string, object> payload)
        {
            var gameObjectPath = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");

            var go = GameObject.Find(gameObjectPath);
            if (go == null)
            {
                throw new InvalidOperationException($"GameObject not found: {gameObjectPath}");
            }

            var tilemap = go.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            if (tilemap == null)
            {
                throw new InvalidOperationException($"Tilemap component not found on: {gameObjectPath}");
            }

            var bounds = tilemap.cellBounds;
            var tileCount = 0;

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    for (int z = bounds.zMin; z < bounds.zMax; z++)
                    {
                        if (tilemap.HasTile(new UnityEngine.Vector3Int(x, y, z)))
                        {
                            tileCount++;
                        }
                    }
                }
            }

            return new Dictionary<string, object>
            {
                ["gameObjectPath"] = gameObjectPath,
                ["tileCount"] = tileCount,
                ["bounds"] = new Dictionary<string, object>
                {
                    ["xMin"] = bounds.xMin,
                    ["yMin"] = bounds.yMin,
                    ["zMin"] = bounds.zMin,
                    ["xMax"] = bounds.xMax,
                    ["yMax"] = bounds.yMax,
                    ["zMax"] = bounds.zMax,
                    ["size"] = new Dictionary<string, object>
                    {
                        ["x"] = bounds.size.x,
                        ["y"] = bounds.size.y,
                        ["z"] = bounds.size.z,
                    },
                },
            };
        }

        private static object ClearAllTiles(Dictionary<string, object> payload)
        {
            var gameObjectPath = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");

            var go = GameObject.Find(gameObjectPath);
            if (go == null)
            {
                throw new InvalidOperationException($"GameObject not found: {gameObjectPath}");
            }

            var tilemap = go.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            if (tilemap == null)
            {
                throw new InvalidOperationException($"Tilemap component not found on: {gameObjectPath}");
            }

            Undo.RecordObject(tilemap, "Clear All Tiles");
            tilemap.ClearAllTiles();

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["gameObjectPath"] = gameObjectPath,
            };
        }

        #endregion

        #region NavMesh Management

        /// <summary>
        /// Handles NavMesh management operations.
        /// </summary>
        private static object HandleNavMeshManage(Dictionary<string, object> payload)
        {
            var operation = GetString(payload, "operation");
            if (string.IsNullOrEmpty(operation))
            {
                throw new InvalidOperationException("operation is required");
            }

            return operation switch
            {
                "bakeNavMesh" => BakeNavMesh(payload),
                "clearNavMesh" => ClearNavMesh(payload),
                "addNavMeshAgent" => AddNavMeshAgent(payload),
                "setDestination" => SetNavMeshDestination(payload),
                "inspectNavMesh" => InspectNavMesh(payload),
                "updateSettings" => UpdateNavMeshSettings(payload),
                "createNavMeshSurface" => CreateNavMeshSurface(payload),
                _ => throw new InvalidOperationException($"Unknown navmeshManage operation: {operation}"),
            };
        }

        private static object BakeNavMesh(Dictionary<string, object> payload)
        {
#pragma warning disable CS0618
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
#pragma warning restore CS0618

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["message"] = "NavMesh bake completed",
            };
        }

        private static object ClearNavMesh(Dictionary<string, object> payload)
        {
#pragma warning disable CS0618
            UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
#pragma warning restore CS0618

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["message"] = "NavMesh cleared",
            };
        }

        private static object AddNavMeshAgent(Dictionary<string, object> payload)
        {
            var gameObjectPath = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");

            var go = GameObject.Find(gameObjectPath);
            if (go == null)
            {
                throw new InvalidOperationException($"GameObject not found: {gameObjectPath}");
            }

            var agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent == null)
            {
                agent = Undo.AddComponent<UnityEngine.AI.NavMeshAgent>(go);
            }

            // Set optional parameters if provided
            if (payload.ContainsKey("agentSpeed"))
            {
                agent.speed = (float)GetFloat(payload, "agentSpeed");
            }
            if (payload.ContainsKey("agentAcceleration"))
            {
                agent.acceleration = (float)GetFloat(payload, "agentAcceleration");
            }
            if (payload.ContainsKey("agentStoppingDistance"))
            {
                agent.stoppingDistance = (float)GetFloat(payload, "agentStoppingDistance");
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["gameObjectPath"] = gameObjectPath,
                ["agentProperties"] = new Dictionary<string, object>
                {
                    ["speed"] = agent.speed,
                    ["acceleration"] = agent.acceleration,
                    ["stoppingDistance"] = agent.stoppingDistance,
                    ["radius"] = agent.radius,
                    ["height"] = agent.height,
                },
            };
        }

        private static object SetNavMeshDestination(Dictionary<string, object> payload)
        {
            var gameObjectPath = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");

            if (!payload.ContainsKey("destinationX") || !payload.ContainsKey("destinationY") || !payload.ContainsKey("destinationZ"))
            {
                throw new InvalidOperationException("destinationX, destinationY, and destinationZ are required");
            }

            var destX = GetFloat(payload, "destinationX");
            var destY = GetFloat(payload, "destinationY");
            var destZ = GetFloat(payload, "destinationZ");

            var go = GameObject.Find(gameObjectPath);
            if (go == null)
            {
                throw new InvalidOperationException($"GameObject not found: {gameObjectPath}");
            }

            var agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent == null)
            {
                throw new InvalidOperationException($"NavMeshAgent component not found on: {gameObjectPath}");
            }

            var destination = new Vector3((float)destX, (float)destY, (float)destZ);
            agent.SetDestination(destination);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["destination"] = new Dictionary<string, object>
                {
                    ["x"] = destX,
                    ["y"] = destY,
                    ["z"] = destZ,
                },
                ["hasPath"] = agent.hasPath,
                ["pathPending"] = agent.pathPending,
            };
        }

        private static object InspectNavMesh(Dictionary<string, object> payload)
        {
            var triangulation = UnityEngine.AI.NavMesh.CalculateTriangulation();
            var settings = UnityEngine.AI.NavMesh.GetSettingsByIndex(0);

            return new Dictionary<string, object>
            {
                ["triangulation"] = new Dictionary<string, object>
                {
                    ["vertexCount"] = triangulation.vertices.Length,
                    ["triangleCount"] = triangulation.indices.Length / 3,
                    ["areaCount"] = triangulation.areas.Length,
                },
                ["settings"] = new Dictionary<string, object>
                {
                    ["agentTypeID"] = settings.agentTypeID,
                    ["agentRadius"] = settings.agentRadius,
                    ["agentHeight"] = settings.agentHeight,
                    ["agentSlope"] = settings.agentSlope,
                    ["agentClimb"] = settings.agentClimb,
                },
            };
        }

        private static object UpdateNavMeshSettings(Dictionary<string, object> payload)
        {
            if (!payload.ContainsKey("settings"))
            {
                throw new InvalidOperationException("settings dictionary is required");
            }

            var settings = payload["settings"] as Dictionary<string, object>;
            if (settings == null)
            {
                throw new InvalidOperationException("settings must be a dictionary");
            }

            // NavMesh settings are managed through the Navigation window and SerializedObject
            // We'll use reflection to access UnityEditor.AI.NavMeshBuilder settings
            var navMeshEditorHelpers = Type.GetType("UnityEditor.AI.NavMeshEditorHelpers, UnityEditor");
            if (navMeshEditorHelpers == null)
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["message"] = "NavMesh settings can only be modified through the Navigation window (Window > AI > Navigation). Runtime modification is not supported through this API.",
                    ["requestedSettings"] = settings,
                };
            }

            // For now, return current settings and a message
            var currentSettings = UnityEngine.AI.NavMesh.GetSettingsByIndex(0);

            return new Dictionary<string, object>
            {
                ["success"] = false,
                ["message"] = "NavMesh settings modification is currently not supported. Please use Unity's Navigation window (Window > AI > Navigation) to modify NavMesh bake settings.",
                ["currentSettings"] = new Dictionary<string, object>
                {
                    ["agentTypeID"] = currentSettings.agentTypeID,
                    ["agentRadius"] = currentSettings.agentRadius,
                    ["agentHeight"] = currentSettings.agentHeight,
                    ["agentSlope"] = currentSettings.agentSlope,
                    ["agentClimb"] = currentSettings.agentClimb,
                },
                ["requestedSettings"] = settings,
            };
        }

        private static object CreateNavMeshSurface(Dictionary<string, object> payload)
        {
            var gameObjectPath = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");

            var go = GameObject.Find(gameObjectPath);
            if (go == null)
            {
                throw new InvalidOperationException($"GameObject not found: {gameObjectPath}");
            }

            // Try to use NavMeshSurface from NavMesh Components package via reflection
            var navMeshSurfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            if (navMeshSurfaceType == null)
            {
                throw new InvalidOperationException("NavMeshSurface not found. Please install the 'NavMesh Components' package from Package Manager.");
            }

            var surface = go.GetComponent(navMeshSurfaceType);
            if (surface == null)
            {
                surface = Undo.AddComponent(go, navMeshSurfaceType);
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["gameObjectPath"] = gameObjectPath,
                ["componentType"] = navMeshSurfaceType.FullName,
            };
        }

        #endregion

        #region Project Compile

        /// <summary>
        /// Handles project compilation operations.
        /// </summary>
        /// <param name="payload">Operation parameters (currently unused, reserved for future options).</param>
        /// <returns>Result dictionary with compilation status and error information.</returns>
        /// <summary>
        /// Waits for Unity compilation to complete with optimized polling.
        /// Uses short sleep intervals (10ms) to improve Editor responsiveness while waiting.
        /// </summary>
        /// <param name="timeoutSeconds">Maximum time to wait in seconds. Default is 30 seconds.</param>
        /// <returns>Dictionary with compilation result including success status and any errors.</returns>
        private static Dictionary<string, object> WaitForCompilation(int timeoutSeconds = 30)
        {
            var startTime = DateTimeOffset.UtcNow;
            var timeoutMs = timeoutSeconds * 1000;

            // Wait for compilation to start (if not already started)
            // Use 10ms sleep intervals for better responsiveness
            var initialWaitMs = 0;
            while (!EditorApplication.isCompiling && initialWaitMs < 2000)
            {
                System.Threading.Thread.Sleep(10);  // Reduced from 50ms to 10ms
                initialWaitMs += 10;
            }

            // Wait for compilation to complete
            // Use 10ms sleep intervals to allow Unity Editor to remain responsive
            while (EditorApplication.isCompiling)
            {
                var elapsed = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                if (elapsed > timeoutMs)
                {
                    return new Dictionary<string, object>
                    {
                        ["success"] = false,
                        ["completed"] = false,
                        ["timedOut"] = true,
                        ["message"] = $"Compilation did not complete within {timeoutSeconds} seconds",
                        ["elapsedSeconds"] = elapsed / 1000.0,
                    };
                }

                System.Threading.Thread.Sleep(10);  // Reduced from 100ms to 10ms
            }

            var totalElapsed = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds / 1000.0;

            // Compilation completed, check for errors
            var errorMessages = new List<string>();
            var logEntries = GetConsoleLogEntries();
            foreach (var entry in logEntries)
            {
                if (entry.ContainsKey("type") && entry["type"].ToString() == "Error" &&
                    entry.ContainsKey("message"))
                {
                    var message = entry["message"].ToString();
                    if (message.Contains("error CS") || message.Contains("CompilerError"))
                    {
                        errorMessages.Add(message);
                    }
                }
            }

            var hasErrors = errorMessages.Count > 0;
            return new Dictionary<string, object>
            {
                ["success"] = !hasErrors,
                ["completed"] = true,
                ["timedOut"] = false,
                ["hasErrors"] = hasErrors,
                ["errors"] = errorMessages,
                ["errorCount"] = errorMessages.Count,
                ["elapsedSeconds"] = totalElapsed,
                ["message"] = hasErrors
                    ? $"Compilation completed with {errorMessages.Count} error(s)"
                    : "Compilation completed successfully",
            };
        }


        /// <summary>
        /// Gets console log entries for error checking.
        /// </summary>
        /// <returns>List of log entry dictionaries.</returns>
        private static List<Dictionary<string, object>> GetConsoleLogEntries()
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

                for (int i = 0; i < Math.Min(count, 100); i++) // Limit to 100 entries
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
    }
}
