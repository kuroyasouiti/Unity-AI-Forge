using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor
{
    internal static partial class McpCommandProcessor
    {
        #region GameObject Management

        /// <summary>
        /// Handles GameObject management operations (create, delete, move, rename, duplicate).
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation' type and GameObject path.</param>
        /// <returns>Result dictionary with operation-specific data.</returns>
        /// <exception cref="InvalidOperationException">Thrown when operation is invalid or missing.</exception>
        private static object HandleGameObjectManage(Dictionary<string, object> payload)
        {
            var operation = EnsureValue(GetString(payload, "operation"), "operation");

            // Check if compilation is in progress and wait if necessary (except for read-only operations)
            Dictionary<string, object> compilationWaitInfo = null;
            if (operation != "inspect" && operation != "findMultiple" && operation != "inspectMultiple")
            {
                compilationWaitInfo = EnsureNoCompilationInProgress("gameObjectManage", maxWaitSeconds: 30f);
            }

            var result = operation switch
            {
                "create" => CreateGameObject(payload),
                "delete" => DeleteGameObject(payload),
                "move" => MoveGameObject(payload),
                "rename" => RenameGameObject(payload),
                "update" => UpdateGameObject(payload),
                "duplicate" => DuplicateGameObject(payload),
                "inspect" => InspectGameObject(payload),
                "findMultiple" => FindMultipleGameObjects(payload),
                "deleteMultiple" => DeleteMultipleGameObjects(payload),
                "inspectMultiple" => InspectMultipleGameObjects(payload),
                _ => throw new InvalidOperationException($"Unknown gameObjectManage operation: {operation}"),
            };

            // Add compilation wait info if we waited
            if (compilationWaitInfo != null && result is Dictionary<string, object> resultDict)
            {
                resultDict["compilationWait"] = compilationWaitInfo;
            }

            return result;
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

            // Use Undo-aware destruction for proper editor integration
            Undo.DestroyObjectImmediate(target);

            return new Dictionary<string, object>
            {
                ["deleted"] = path,
                ["success"] = true,
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

        private static object UpdateGameObject(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var target = ResolveGameObject(path);

            // Update tag if provided
            if (payload.TryGetValue("tag", out var tagObj) && tagObj != null)
            {
                var tag = tagObj.ToString();
                if (!string.IsNullOrEmpty(tag))
                {
                    target.tag = tag;
                }
            }

            // Update layer if provided (accepts both layer name and layer index)
            if (payload.TryGetValue("layer", out var layerObj) && layerObj != null)
            {
                if (layerObj is int layerIndex)
                {
                    target.layer = layerIndex;
                }
                else if (layerObj is long layerIndexLong)
                {
                    target.layer = (int)layerIndexLong;
                }
                else if (layerObj is double layerIndexDouble)
                {
                    target.layer = (int)layerIndexDouble;
                }
                else if (layerObj is string layerName && !string.IsNullOrEmpty(layerName))
                {
                    var layer = LayerMask.NameToLayer(layerName);
                    if (layer == -1)
                    {
                        throw new InvalidOperationException($"Layer not found: {layerName}");
                    }
                    target.layer = layer;
                }
            }

            // Update active state if provided
            if (payload.TryGetValue("active", out var activeObj) && activeObj != null)
            {
                if (activeObj is bool active)
                {
                    target.SetActive(active);
                }
            }

            // Update static flag if provided
            if (payload.TryGetValue("static", out var staticObj) && staticObj != null)
            {
                if (staticObj is bool isStatic)
                {
                    target.isStatic = isStatic;
                }
            }

            // Return updated GameObject info
            return new Dictionary<string, object>
            {
                ["path"] = GetHierarchyPath(target),
                ["name"] = target.name,
                ["tag"] = target.tag,
                ["layer"] = target.layer,
                ["layerName"] = LayerMask.LayerToName(target.layer),
                ["active"] = target.activeSelf,
                ["activeInHierarchy"] = target.activeInHierarchy,
                ["static"] = target.isStatic,
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

            return result;
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

        #endregion
    }
}

