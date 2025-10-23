using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
                "scriptOutline" => HandleScriptOutline(command.Payload),
                "prefabManage" => HandlePrefabManage(command.Payload),
                "projectSettingsManage" => HandleProjectSettingsManage(command.Payload),
                "renderPipelineManage" => HandleRenderPipelineManage(command.Payload),
                "inputSystemManage" => HandleInputSystemManage(command.Payload),
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
                _ => throw new InvalidOperationException($"Unknown componentManage operation: {operation}"),
            };
        }

        private static object AddComponent(Dictionary<string, object> payload)
        {
            var go = ResolveGameObject(EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath"));
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
            var go = ResolveGameObject(EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath"));
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
            var go = ResolveGameObject(EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath"));
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
            var go = ResolveGameObject(EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath"));
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
                _ => throw new InvalidOperationException($"Unknown assetManage operation: {operation}"),
            };
        }

        private static object CreateAsset(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "assetPath"), "assetPath");
            var contents = GetString(payload, "contents") ?? string.Empty;
            EnsureDirectoryExists(path);
            File.WriteAllText(path, contents, Encoding.UTF8);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return DescribeAsset(path);
        }

        private static object UpdateAsset(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "assetPath"), "assetPath");
            var contents = GetString(payload, "contents");
            var overwrite = GetBool(payload, "overwrite", true);

            if (!File.Exists(path) && !overwrite)
            {
                throw new InvalidOperationException($"Asset does not exist: {path}");
            }

            EnsureDirectoryExists(path);
            File.WriteAllText(path, contents ?? string.Empty, Encoding.UTF8);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return DescribeAsset(path);
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

        private static object HandleScriptOutline(Dictionary<string, object> payload)
        {
            var guid = GetString(payload, "guid");
            var assetPath = GetString(payload, "assetPath");

            if (!string.IsNullOrEmpty(guid))
            {
                assetPath = AssetDatabase.GUIDToAssetPath(guid);
            }

            assetPath = EnsureValue(assetPath, "assetPath");
            var fullPath = Path.GetFullPath(assetPath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("Script file not found", fullPath);
            }

            var source = File.ReadAllText(fullPath);
            var outline = AnalyzeScriptOutline(source);
            var syntaxOk = CheckBraceBalance(source);

            return new Dictionary<string, object>
            {
                ["assetPath"] = assetPath,
                ["syntaxOk"] = syntaxOk,
                ["outline"] = outline,
            };
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
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null && property.CanWrite)
            {
                var converted = ConvertValue(rawValue, property.PropertyType);
                property.SetValue(component, converted);
                return;
            }

            var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                var converted = ConvertValue(rawValue, field.FieldType);
                field.SetValue(component, converted);
                return;
            }

            throw new InvalidOperationException($"Property or field '{propertyName}' not found on {type.FullName}");
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
        /// Resolves a GameObject reference by hierarchy path.
        /// </summary>
        private static UnityEngine.Object ResolveGameObjectReference(Dictionary<string, object> refDict, Type targetType)
        {
            var path = GetString(refDict, "path");
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("GameObject reference requires 'path' parameter");
            }

            var gameObject = ResolveGameObject(path);

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
                    throw new InvalidOperationException($"Component {targetType.FullName} not found on GameObject: {path}");
                }
                return component;
            }

            return gameObject;
        }

        /// <summary>
        /// Resolves a Component reference by GameObject path and component type.
        /// </summary>
        private static UnityEngine.Object ResolveComponentReference(Dictionary<string, object> refDict, Type targetType)
        {
            var path = GetString(refDict, "path");
            var typeName = GetString(refDict, "type");

            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("Component reference requires 'path' parameter");
            }

            var gameObject = ResolveGameObject(path);

            // Determine which component type to get
            Type componentType = targetType;
            if (!string.IsNullOrEmpty(typeName))
            {
                componentType = ResolveType(typeName);
            }

            var component = gameObject.GetComponent(componentType);
            if (component == null)
            {
                throw new InvalidOperationException($"Component {componentType.FullName} not found on GameObject: {path}");
            }

            return component;
        }

        /// <summary>
        /// Resolves an Asset reference by asset path or GUID.
        /// </summary>
        private static UnityEngine.Object ResolveAssetReference(Dictionary<string, object> refDict, Type targetType)
        {
            var path = GetString(refDict, "path");
            var guid = GetString(refDict, "guid");

            // Resolve path from GUID if provided
            if (!string.IsNullOrEmpty(guid))
            {
                path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    throw new InvalidOperationException($"Asset not found with GUID: {guid}");
                }
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("Asset reference requires 'path' or 'guid' parameter");
            }

            // Load the asset
            var asset = AssetDatabase.LoadAssetAtPath(path, targetType);
            if (asset == null)
            {
                // Try loading as generic Object if specific type fails
                asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset != null && !targetType.IsInstanceOfType(asset))
                {
                    throw new InvalidOperationException($"Asset at {path} is type {asset.GetType().FullName}, expected {targetType.FullName}");
                }
            }

            if (asset == null)
            {
                throw new InvalidOperationException($"Asset not found or incompatible type at path: {path}");
            }

            return asset;
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
                "displayresolutiondialog" => PlayerSettings.displayResolutionDialog.ToString(),
                "defaultisfullscreen" => PlayerSettings.defaultIsFullScreen,
                "defaultisnativeresolution" => PlayerSettings.defaultIsNativeResolution,
                "allowfullscreenswitch" => PlayerSettings.allowFullscreenSwitch,
                "capturesinglescreen" => PlayerSettings.captureSingleScreen,
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
                case "defaultisfullscreen":
                    PlayerSettings.defaultIsFullScreen = Convert.ToBoolean(value);
                    break;
                case "defaultisnativeresolution":
                    PlayerSettings.defaultIsNativeResolution = Convert.ToBoolean(value);
                    break;
                case "allowfullscreenswitch":
                    PlayerSettings.allowFullscreenSwitch = Convert.ToBoolean(value);
                    break;
                case "capturesinglescreen":
                    PlayerSettings.captureSingleScreen = Convert.ToBoolean(value);
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
                "autosimulation" => Physics.autoSimulation,
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
                case "autosimulation":
                    Physics.autoSimulation = Convert.ToBoolean(value);
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
                "etctexturecompressorbehavior" => EditorSettings.etcTextureCompressorBehavior.ToString(),
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
                        "scriptOutline" => HandleScriptOutline(operationPayload),
                        "prefabManage" => HandlePrefabManage(operationPayload),
                        "projectSettingsManage" => HandleProjectSettingsManage(operationPayload),
                        "renderPipelineManage" => HandleRenderPipelineManage(operationPayload),
                        "inputSystemManage" => HandleInputSystemManage(operationPayload),
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
    }
}
