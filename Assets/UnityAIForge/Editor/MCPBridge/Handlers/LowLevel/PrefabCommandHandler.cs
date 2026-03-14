using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCP.Editor.Base;
using MCP.Editor.Interfaces;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Prefab management command handler.
    /// Handles prefab creation, updating, inspection, instantiation, unpacking, and override management.
    /// </summary>
    public class PrefabCommandHandler : BaseCommandHandler
    {
        #region ICommandHandler Implementation
        
        public override string Category => "prefabManage";
        
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "create",
            "update",
            "inspect",
            "instantiate",
            "unpack",
            "applyOverrides",
            "revertOverrides",
            "editAsset",
            "editMultiple"
        };

        private readonly IPropertyApplier _propertyApplier;

        public PrefabCommandHandler()
        {
            _propertyApplier = new ComponentPropertyApplier();
        }
        
        #endregion
        
        #region BaseCommandHandler Overrides
        
        protected override bool RequiresCompilationWait(string operation)
        {
            // Only inspect is read-only
            return operation != "inspect";
        }
        
        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreatePrefab(payload),
                "update" => UpdatePrefab(payload),
                "inspect" => InspectPrefab(payload),
                "instantiate" => InstantiatePrefab(payload),
                "unpack" => UnpackPrefab(payload),
                "applyOverrides" => ApplyPrefabOverrides(payload),
                "revertOverrides" => RevertPrefabOverrides(payload),
                "editAsset" => EditPrefabAsset(payload),
                "editMultiple" => EditMultiplePrefabs(payload),
                _ => throw new InvalidOperationException($"Unknown prefab operation: {operation}")
            };
        }
        
        #endregion
        
        #region Prefab Operations
        
        /// <summary>
        /// Creates a prefab from a GameObject in the scene.
        /// </summary>
        private object CreatePrefab(Dictionary<string, object> payload)
        {
            var gameObjectPath = GetString(payload, "gameObjectPath");
            var prefabPath = GetString(payload, "prefabPath");
            
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required");
            }
            
            if (string.IsNullOrEmpty(prefabPath))
            {
                throw new InvalidOperationException("prefabPath is required");
            }
            
            var gameObject = ResolveGameObject(gameObjectPath);
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(prefabPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Failed to create prefab at {prefabPath}");
            }
            
            AssetDatabase.Refresh();
            
            return CreateSuccessResponse(
                ("prefabPath", prefabPath),
                ("guid", AssetDatabase.AssetPathToGUID(prefabPath)),
                ("sourceGameObject", gameObjectPath)
            );
        }
        
        /// <summary>
        /// Updates a prefab asset from a prefab instance in the scene.
        /// </summary>
        private object UpdatePrefab(Dictionary<string, object> payload)
        {
            var gameObjectPath = GetString(payload, "gameObjectPath");
            var prefabPath = GetString(payload, "prefabPath");
            
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required");
            }
            
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
            
            return CreateSuccessResponse(
                ("prefabPath", actualPrefabPath),
                ("guid", AssetDatabase.AssetPathToGUID(actualPrefabPath)),
                ("updatedFrom", gameObjectPath)
            );
        }
        
        /// <summary>
        /// Inspects a prefab asset.
        /// </summary>
        private object InspectPrefab(Dictionary<string, object> payload)
        {
            var prefabPath = GetString(payload, "prefabPath");
            
            if (string.IsNullOrEmpty(prefabPath))
            {
                throw new InvalidOperationException("prefabPath is required");
            }
            
            if (!File.Exists(prefabPath))
            {
                throw new InvalidOperationException($"Prefab not found: {prefabPath}");
            }
            
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Failed to load prefab: {prefabPath}");
            }
            
            var info = new Dictionary<string, object>
            {
                ["success"] = true,
                ["prefabPath"] = prefabPath,
                ["guid"] = AssetDatabase.AssetPathToGUID(prefabPath),
                ["name"] = prefab.name,
                ["childCount"] = prefab.transform.childCount
            };
            
            // List components
            var components = new List<Dictionary<string, object>>();
            foreach (var comp in prefab.GetComponents<Component>())
            {
                if (comp != null)
                {
                    components.Add(new Dictionary<string, object>
                    {
                        ["type"] = comp.GetType().FullName,
                        ["name"] = comp.GetType().Name
                    });
                }
            }
            info["components"] = components;
            
            // List direct children
            var children = new List<string>();
            for (int i = 0; i < prefab.transform.childCount; i++)
            {
                children.Add(prefab.transform.GetChild(i).name);
            }
            info["children"] = children;
            
            return info;
        }
        
        /// <summary>
        /// Instantiates a prefab in the scene.
        /// </summary>
        private object InstantiatePrefab(Dictionary<string, object> payload)
        {
            var prefabPath = GetString(payload, "prefabPath");
            var parentPath = GetString(payload, "parentPath");
            
            if (string.IsNullOrEmpty(prefabPath))
            {
                throw new InvalidOperationException("prefabPath is required");
            }
            
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Prefab not found: {prefabPath}");
            }
            
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null)
            {
                throw new InvalidOperationException($"Failed to instantiate prefab: {prefabPath}");
            }
            
            // Set parent if specified
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = ResolveGameObject(parentPath);
                instance.transform.SetParent(parent.transform, false);
            }
            
            Undo.RegisterCreatedObjectUndo(instance, $"Instantiate Prefab: {prefab.name}");
            
            return CreateSuccessResponse(
                ("prefabPath", prefabPath),
                ("instancePath", BuildGameObjectPath(instance)),
                ("name", instance.name),
                ("instanceID", instance.GetInstanceID())
            );
        }
        
        /// <summary>
        /// Unpacks a prefab instance.
        /// </summary>
        private object UnpackPrefab(Dictionary<string, object> payload)
        {
            var gameObjectPath = GetString(payload, "gameObjectPath");
            var unpackMode = GetString(payload, "unpackMode", "Completely");
            
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required");
            }
            
            var gameObject = ResolveGameObject(gameObjectPath);
            
            if (!PrefabUtility.IsPartOfPrefabInstance(gameObject))
            {
                throw new InvalidOperationException($"GameObject at {gameObjectPath} is not a prefab instance");
            }
            
            var mode = unpackMode == "Completely" 
                ? PrefabUnpackMode.Completely 
                : PrefabUnpackMode.OutermostRoot;
            
            Undo.RegisterFullObjectHierarchyUndo(gameObject, $"Unpack Prefab: {gameObject.name}");
            PrefabUtility.UnpackPrefabInstance(gameObject, mode, InteractionMode.UserAction);
            
            return CreateSuccessResponse(
                ("gameObjectPath", gameObjectPath),
                ("unpackMode", unpackMode),
                ("message", "Prefab unpacked successfully")
            );
        }
        
        /// <summary>
        /// Applies prefab instance overrides to the prefab asset.
        /// </summary>
        private object ApplyPrefabOverrides(Dictionary<string, object> payload)
        {
            var gameObjectPath = GetString(payload, "gameObjectPath");
            
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required");
            }
            
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
            
            // Apply all overrides
            PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.UserAction);
            AssetDatabase.Refresh();
            
            return CreateSuccessResponse(
                ("gameObjectPath", gameObjectPath),
                ("prefabPath", prefabPath),
                ("message", "Prefab overrides applied successfully")
            );
        }
        
        /// <summary>
        /// Reverts prefab instance overrides.
        /// </summary>
        private object RevertPrefabOverrides(Dictionary<string, object> payload)
        {
            var gameObjectPath = GetString(payload, "gameObjectPath");
            
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required");
            }
            
            var gameObject = ResolveGameObject(gameObjectPath);
            
            if (!PrefabUtility.IsPartOfPrefabInstance(gameObject))
            {
                throw new InvalidOperationException($"GameObject at {gameObjectPath} is not a prefab instance");
            }
            
            var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
            
            // Revert all overrides
            PrefabUtility.RevertPrefabInstance(prefabRoot, InteractionMode.UserAction);
            
            return CreateSuccessResponse(
                ("gameObjectPath", gameObjectPath),
                ("message", "Prefab overrides reverted successfully")
            );
        }
        
        /// <summary>
        /// Edits a prefab asset directly without instantiating it in the scene.
        /// Supports: tag, layer, componentChanges (add/update components), removeComponents.
        /// </summary>
        private object EditPrefabAsset(Dictionary<string, object> payload)
        {
            var prefabPath = GetString(payload, "prefabPath");
            if (string.IsNullOrEmpty(prefabPath))
                throw new InvalidOperationException("prefabPath is required for editAsset.");
            EnsureSafeAssetPath(prefabPath, "prefabPath");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Prefab not found: {prefabPath}");

            var updated = new List<string>();

            // Use PrefabUtility.LoadPrefabContents / SaveAsPrefabAsset for direct editing
            var contentsRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contentsRoot == null)
                throw new InvalidOperationException($"Failed to load prefab contents: {prefabPath}");

            bool saveSucceeded = false;
            try
            {
                // Set tag
                if (payload.ContainsKey("tag"))
                {
                    contentsRoot.tag = GetString(payload, "tag");
                    updated.Add("tag");
                }

                // Set layer (by name or number)
                if (payload.ContainsKey("layer"))
                {
                    var layerValue = payload["layer"];
                    int layer;
                    if (layerValue is string layerName)
                    {
                        layer = LayerMask.NameToLayer(layerName);
                        if (layer < 0)
                            throw new InvalidOperationException($"Layer not found: {layerName}");
                    }
                    else
                    {
                        layer = GetInt(payload, "layer");
                    }
                    contentsRoot.layer = layer;
                    updated.Add("layer");
                }

                // Apply component property changes
                if (payload.TryGetValue("componentChanges", out var changesObj) && changesObj is List<object> changesList)
                {
                    foreach (var item in changesList)
                    {
                        if (item is not Dictionary<string, object> change) continue;
                        var componentType = change.ContainsKey("componentType") ? change["componentType"]?.ToString() : null;
                        if (string.IsNullOrEmpty(componentType)) continue;

                        var type = ResolveType(componentType);
                        var component = contentsRoot.GetComponent(type);

                        // Add component if not present
                        if (component == null)
                        {
                            component = contentsRoot.AddComponent(type);
                            updated.Add($"added:{componentType}");
                        }

                        if (change.TryGetValue("propertyChanges", out var propsObj) && propsObj is Dictionary<string, object> props && props.Count > 0)
                        {
                            _propertyApplier.ApplyProperties(component, props);
                            updated.Add($"updated:{componentType}");
                        }
                    }
                }

                // Remove components
                if (payload.TryGetValue("removeComponents", out var removeObj) && removeObj is List<object> removeList)
                {
                    foreach (var item in removeList)
                    {
                        var typeName = item?.ToString();
                        if (string.IsNullOrEmpty(typeName)) continue;
                        var type = ResolveType(typeName);
                        var component = contentsRoot.GetComponent(type);
                        if (component != null)
                        {
                            UnityEngine.Object.DestroyImmediate(component);
                            updated.Add($"removed:{typeName}");
                        }
                    }
                }

                bool success;
                PrefabUtility.SaveAsPrefabAsset(contentsRoot, prefabPath, out success);
                saveSucceeded = success;
                if (!success)
                    throw new InvalidOperationException($"Failed to save prefab: {prefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contentsRoot);
            }

            AssetDatabase.Refresh();

            return CreateSuccessResponse(
                ("prefabPath", prefabPath),
                ("updated", updated),
                ("message", $"Prefab edited directly: {string.Join(", ", updated)}")
            );
        }

        /// <summary>
        /// Edits multiple prefab assets with the same changes.
        /// Accepts prefabPaths array + shared tag/layer/componentChanges.
        /// </summary>
        private object EditMultiplePrefabs(Dictionary<string, object> payload)
        {
            var prefabPaths = GetStringList(payload, "prefabPaths");
            if (prefabPaths == null || prefabPaths.Count == 0)
                throw new InvalidOperationException("prefabPaths array is required for editMultiple.");

            var results = new List<Dictionary<string, object>>();
            int successCount = 0;
            int errorCount = 0;

            foreach (var path in prefabPaths)
            {
                try
                {
                    // Build per-prefab payload with same shared properties
                    var perPrefabPayload = new Dictionary<string, object>(payload)
                    {
                        ["prefabPath"] = path
                    };
                    perPrefabPayload.Remove("prefabPaths");

                    var result = EditPrefabAsset(perPrefabPayload) as Dictionary<string, object>;
                    results.Add(new Dictionary<string, object>
                    {
                        ["prefabPath"] = path,
                        ["success"] = true,
                        ["updated"] = result?.ContainsKey("updated") == true ? result["updated"] : new List<string>()
                    });
                    successCount++;
                }
                catch (Exception ex)
                {
                    results.Add(new Dictionary<string, object>
                    {
                        ["prefabPath"] = path,
                        ["success"] = false,
                        ["error"] = ex.Message
                    });
                    errorCount++;
                }
            }

            return CreateSuccessResponse(
                ("results", results),
                ("totalCount", prefabPaths.Count),
                ("successCount", successCount),
                ("errorCount", errorCount)
            );
        }

        #endregion

        #region Helper Methods

        // GetGameObjectPath is inherited from BaseCommandHandler as BuildGameObjectPath

        #endregion
    }
}

