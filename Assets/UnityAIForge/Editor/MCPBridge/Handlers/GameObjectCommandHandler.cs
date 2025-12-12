using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;
using MCP.Editor.Interfaces;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// GameObject management command handler.
    /// Handles GameObject creation, deletion, moving, renaming, updating, duplication, and inspection.
    /// Also supports batch operations with pattern matching.
    /// </summary>
    public class GameObjectCommandHandler : BaseCommandHandler
    {
        #region Fields

        private readonly IPropertyApplier _propertyApplier;

        #endregion

        #region Constructor

        public GameObjectCommandHandler()
        {
            _propertyApplier = new ComponentPropertyApplier();
        }

        #endregion

        #region ICommandHandler Implementation

        public override string Category => "gameObject";
        
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "create",
            "delete",
            "move",
            "rename",
            "update",
            "duplicate",
            "inspect",
            "findMultiple",
            "deleteMultiple",
            "inspectMultiple"
        };
        
        #endregion
        
        #region BaseCommandHandler Overrides
        
        protected override bool RequiresCompilationWait(string operation)
        {
            // Read-only operations
            var readOnlyOps = new[] { "inspect", "findMultiple", "inspectMultiple" };
            return !readOnlyOps.Contains(operation);
        }
        
        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
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
                _ => throw new InvalidOperationException($"Unknown GameObject operation: {operation}")
            };
        }
        
        #endregion
        
        #region GameObject Operations
        
        /// <summary>
        /// Creates a new GameObject.
        /// </summary>
        private object CreateGameObject(Dictionary<string, object> payload)
        {
            var name = GetString(payload, "name", "New GameObject");
            var parentPath = GetString(payload, "parentPath");
            var template = GetString(payload, "template");

            GameObject instance;
            GameObject parent = null;

            // Resolve parent if specified
            if (!string.IsNullOrEmpty(parentPath))
            {
                parent = ResolveGameObject(parentPath);
            }

            // Create from template (primitive or prefab) or create new
            if (!string.IsNullOrEmpty(template))
            {
                // Check if template is a primitive type
                var primitiveType = GetPrimitiveType(template);
                if (primitiveType.HasValue)
                {
                    instance = GameObject.CreatePrimitive(primitiveType.Value);
                    instance.name = name;
                }
                else
                {
                    // Try to load as prefab
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(template);
                    if (prefab == null)
                    {
                        throw new InvalidOperationException($"Prefab not found: {template}. Supported primitives: Cube, Sphere, Capsule, Cylinder, Plane, Quad");
                    }

                    instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                }
            }
            else
            {
                instance = new GameObject(name);
            }

            // Set parent
            if (parent != null)
            {
                instance.transform.SetParent(parent.transform, false);
            }

            // Register undo
            Undo.RegisterCreatedObjectUndo(instance, $"Create GameObject: {instance.name}");

            // Add components if specified
            var addedComponents = new List<Dictionary<string, object>>();
            if (payload.ContainsKey("components"))
            {
                var components = payload["components"] as List<object>;
                if (components != null)
                {
                    foreach (var componentObj in components)
                    {
                        var componentDef = componentObj as Dictionary<string, object>;
                        if (componentDef == null) continue;

                        var componentTypeName = componentDef.ContainsKey("type") ? componentDef["type"]?.ToString() : null;
                        if (string.IsNullOrEmpty(componentTypeName)) continue;

                        try
                        {
                            var componentType = ResolveType(componentTypeName);
                            if (!typeof(Component).IsAssignableFrom(componentType))
                            {
                                Debug.LogWarning($"Type {componentTypeName} is not a Component, skipping");
                                continue;
                            }

                            var component = Undo.AddComponent(instance, componentType);

                            var componentResult = new Dictionary<string, object>
                            {
                                ["type"] = componentType.FullName,
                                ["instanceID"] = component.GetInstanceID()
                            };

                            // Apply property changes if provided
                            if (componentDef.ContainsKey("properties"))
                            {
                                var properties = componentDef["properties"] as Dictionary<string, object>;
                                if (properties != null && properties.Count > 0)
                                {
                                    var applyResult = _propertyApplier.ApplyProperties(component, properties);
                                    componentResult["updatedProperties"] = applyResult.Updated;
                                }
                            }

                            addedComponents.Add(componentResult);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Failed to add component {componentTypeName}: {ex.Message}");
                            addedComponents.Add(new Dictionary<string, object>
                            {
                                ["type"] = componentTypeName,
                                ["error"] = ex.Message
                            });
                        }
                    }
                }
            }

            var response = new Dictionary<string, object>
            {
                ["success"] = true,
                ["gameObjectPath"] = GetGameObjectPath(instance),
                ["name"] = instance.name,
                ["instanceID"] = instance.GetInstanceID()
            };

            if (addedComponents.Count > 0)
            {
                response["components"] = addedComponents;
            }

            return response;
        }
        
        /// <summary>
        /// Deletes a GameObject.
        /// </summary>
        private object DeleteGameObject(Dictionary<string, object> payload)
        {
            var gameObject = ResolveGameObjectFromPayload(payload);
            var path = GetGameObjectPath(gameObject);
            
            Undo.DestroyObjectImmediate(gameObject);
            
            return CreateSuccessResponse(
                ("gameObjectPath", path),
                ("message", "GameObject deleted successfully")
            );
        }
        
        /// <summary>
        /// Moves a GameObject to a new parent.
        /// </summary>
        private object MoveGameObject(Dictionary<string, object> payload)
        {
            var gameObject = ResolveGameObjectFromPayload(payload);
            var parentPath = GetString(payload, "parentPath");
            
            GameObject newParent = null;
            if (!string.IsNullOrEmpty(parentPath))
            {
                newParent = ResolveGameObject(parentPath);
            }
            
            Undo.SetTransformParent(gameObject.transform, newParent?.transform, $"Move GameObject: {gameObject.name}");
            
            return CreateSuccessResponse(
                ("gameObjectPath", GetGameObjectPath(gameObject)),
                ("parentPath", newParent != null ? GetGameObjectPath(newParent) : "root")
            );
        }
        
        /// <summary>
        /// Renames a GameObject.
        /// </summary>
        private object RenameGameObject(Dictionary<string, object> payload)
        {
            var gameObject = ResolveGameObjectFromPayload(payload);
            var name = GetString(payload, "name");
            
            if (string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException("name parameter is required for rename operation");
            }
            
            var oldName = gameObject.name;
            Undo.RecordObject(gameObject, $"Rename GameObject: {oldName} -> {name}");
            gameObject.name = name;
            
            return CreateSuccessResponse(
                ("gameObjectPath", GetGameObjectPath(gameObject)),
                ("oldName", oldName),
                ("newName", name)
            );
        }
        
        /// <summary>
        /// Updates GameObject properties (tag, layer, active state, static flag).
        /// </summary>
        private object UpdateGameObject(Dictionary<string, object> payload)
        {
            var gameObject = ResolveGameObjectFromPayload(payload);
            var updated = new List<string>();
            
            Undo.RecordObject(gameObject, $"Update GameObject: {gameObject.name}");
            
            // Update tag
            if (payload.ContainsKey("tag"))
            {
                var tag = GetString(payload, "tag");
                gameObject.tag = tag;
                updated.Add("tag");
            }
            
            // Update layer
            if (payload.ContainsKey("layer"))
            {
                var layerValue = payload["layer"];
                int layer;
                
                if (layerValue is string layerName)
                {
                    layer = LayerMask.NameToLayer(layerName);
                    if (layer < 0)
                    {
                        throw new InvalidOperationException($"Layer not found: {layerName}");
                    }
                }
                else
                {
                    layer = GetInt(payload, "layer");
                }
                
                gameObject.layer = layer;
                updated.Add("layer");
            }
            
            // Update active state
            if (payload.ContainsKey("active"))
            {
                var active = GetBool(payload, "active");
                gameObject.SetActive(active);
                updated.Add("active");
            }
            
            // Update static flag
            if (payload.ContainsKey("static"))
            {
                var isStatic = GetBool(payload, "static");
                gameObject.isStatic = isStatic;
                updated.Add("static");
            }
            
            return CreateSuccessResponse(
                ("gameObjectPath", GetGameObjectPath(gameObject)),
                ("updated", updated)
            );
        }
        
        /// <summary>
        /// Duplicates a GameObject.
        /// </summary>
        private object DuplicateGameObject(Dictionary<string, object> payload)
        {
            var gameObject = ResolveGameObjectFromPayload(payload);
            var duplicate = GameObject.Instantiate(gameObject, gameObject.transform.parent);
            
            Undo.RegisterCreatedObjectUndo(duplicate, $"Duplicate GameObject: {gameObject.name}");
            
            return CreateSuccessResponse(
                ("originalPath", GetGameObjectPath(gameObject)),
                ("duplicatePath", GetGameObjectPath(duplicate)),
                ("name", duplicate.name),
                ("instanceID", duplicate.GetInstanceID())
            );
        }
        
        /// <summary>
        /// Inspects a GameObject and returns detailed information.
        /// </summary>
        private object InspectGameObject(Dictionary<string, object> payload)
        {
            var gameObject = ResolveGameObjectFromPayload(payload);
            
            var info = new Dictionary<string, object>
            {
                ["success"] = true,
                ["name"] = gameObject.name,
                ["path"] = GetGameObjectPath(gameObject),
                ["instanceID"] = gameObject.GetInstanceID(),
                ["tag"] = gameObject.tag,
                ["layer"] = LayerMask.LayerToName(gameObject.layer),
                ["active"] = gameObject.activeSelf,
                ["activeInHierarchy"] = gameObject.activeInHierarchy,
                ["static"] = gameObject.isStatic,
                ["childCount"] = gameObject.transform.childCount
            };
            
            // Transform information
            info["transform"] = new Dictionary<string, object>
            {
                ["position"] = VectorToDict(gameObject.transform.position),
                ["localPosition"] = VectorToDict(gameObject.transform.localPosition),
                ["rotation"] = VectorToDict(gameObject.transform.eulerAngles),
                ["localRotation"] = VectorToDict(gameObject.transform.localEulerAngles),
                ["localScale"] = VectorToDict(gameObject.transform.localScale)
            };
            
            // Components
            var components = gameObject.GetComponents<Component>();
            info["components"] = components
                .Where(c => c != null)
                .Select(c => new Dictionary<string, object>
                {
                    ["type"] = c.GetType().FullName,
                    ["name"] = c.GetType().Name
                })
                .ToList();
            
            return info;
        }
        
        #endregion
        
        #region Batch Operations
        
        /// <summary>
        /// Finds multiple GameObjects matching a pattern.
        /// </summary>
        private object FindMultipleGameObjects(Dictionary<string, object> payload)
        {
            var pattern = GetString(payload, "pattern");
            var useRegex = GetBool(payload, "useRegex", false);
            var maxResults = GetInt(payload, "maxResults", 1000);
            var includeComponents = GetBool(payload, "includeComponents", false);
            
            if (string.IsNullOrEmpty(pattern))
            {
                throw new InvalidOperationException("pattern parameter is required");
            }
            
            var gameObjects = FindGameObjectsByPattern(pattern, useRegex, maxResults).ToList();
            
            var results = gameObjects.Select(go =>
            {
                var info = new Dictionary<string, object>
                {
                    ["name"] = go.name,
                    ["path"] = GetGameObjectPath(go),
                    ["instanceID"] = go.GetInstanceID(),
                    ["active"] = go.activeSelf
                };
                
                if (includeComponents)
                {
                    var components = go.GetComponents<Component>();
                    info["components"] = components
                        .Where(c => c != null)
                        .Select(c => c.GetType().Name)
                        .ToList();
                }
                
                return info;
            }).ToList();
            
            return CreateSuccessResponse(
                ("results", results),
                ("count", results.Count),
                ("pattern", pattern)
            );
        }
        
        /// <summary>
        /// Deletes multiple GameObjects matching a pattern.
        /// </summary>
        private object DeleteMultipleGameObjects(Dictionary<string, object> payload)
        {
            var pattern = GetString(payload, "pattern");
            var useRegex = GetBool(payload, "useRegex", false);
            var maxResults = GetInt(payload, "maxResults", 1000);
            
            if (string.IsNullOrEmpty(pattern))
            {
                throw new InvalidOperationException("pattern parameter is required");
            }
            
            var gameObjects = FindGameObjectsByPattern(pattern, useRegex, maxResults).ToList();
            var deleted = new List<string>();
            
            foreach (var go in gameObjects)
            {
                var path = GetGameObjectPath(go);
                Undo.DestroyObjectImmediate(go);
                deleted.Add(path);
            }
            
            return CreateSuccessResponse(
                ("deleted", deleted),
                ("count", deleted.Count),
                ("pattern", pattern)
            );
        }
        
        /// <summary>
        /// Inspects multiple GameObjects matching a pattern.
        /// </summary>
        private object InspectMultipleGameObjects(Dictionary<string, object> payload)
        {
            var pattern = GetString(payload, "pattern");
            var useRegex = GetBool(payload, "useRegex", false);
            var maxResults = GetInt(payload, "maxResults", 1000);
            var includeComponents = GetBool(payload, "includeComponents", false);
            
            if (string.IsNullOrEmpty(pattern))
            {
                throw new InvalidOperationException("pattern parameter is required");
            }
            
            var gameObjects = FindGameObjectsByPattern(pattern, useRegex, maxResults).ToList();
            
            var results = gameObjects.Select(go =>
            {
                var info = new Dictionary<string, object>
                {
                    ["name"] = go.name,
                    ["path"] = GetGameObjectPath(go),
                    ["instanceID"] = go.GetInstanceID(),
                    ["tag"] = go.tag,
                    ["layer"] = LayerMask.LayerToName(go.layer),
                    ["active"] = go.activeSelf
                };
                
                if (includeComponents)
                {
                    var components = go.GetComponents<Component>();
                    info["components"] = components
                        .Where(c => c != null)
                        .Select(c => new Dictionary<string, object>
                        {
                            ["type"] = c.GetType().FullName,
                            ["name"] = c.GetType().Name
                        })
                        .ToList();
                }
                
                return info;
            }).ToList();
            
            return CreateSuccessResponse(
                ("results", results),
                ("count", results.Count),
                ("pattern", pattern)
            );
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Gets the hierarchical path of a GameObject.
        /// </summary>
        private string GetGameObjectPath(GameObject go)
        {
            if (go == null)
            {
                return null;
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
        /// Converts a Vector3 to a dictionary.
        /// </summary>
        private Dictionary<string, object> VectorToDict(Vector3 vector)
        {
            return new Dictionary<string, object>
            {
                ["x"] = vector.x,
                ["y"] = vector.y,
                ["z"] = vector.z
            };
        }

        /// <summary>
        /// Gets the PrimitiveType from a template name (case-insensitive).
        /// Returns null if the template is not a recognized primitive.
        /// </summary>
        private PrimitiveType? GetPrimitiveType(string template)
        {
            if (string.IsNullOrEmpty(template))
                return null;

            switch (template.ToLowerInvariant())
            {
                case "cube":
                    return PrimitiveType.Cube;
                case "sphere":
                    return PrimitiveType.Sphere;
                case "capsule":
                    return PrimitiveType.Capsule;
                case "cylinder":
                    return PrimitiveType.Cylinder;
                case "plane":
                    return PrimitiveType.Plane;
                case "quad":
                    return PrimitiveType.Quad;
                default:
                    return null;
            }
        }

        #endregion
    }
}

