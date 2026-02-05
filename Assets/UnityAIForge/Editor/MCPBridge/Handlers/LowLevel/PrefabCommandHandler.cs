using System;
using System.Collections.Generic;
using System.IO;
using MCP.Editor.Base;
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
        
        public override string Category => "prefab";
        
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "create",
            "update",
            "inspect",
            "instantiate",
            "unpack",
            "applyOverrides",
            "revertOverrides"
        };
        
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
        
        #endregion
        
        #region Helper Methods

        // GetGameObjectPath is inherited from BaseCommandHandler as BuildGameObjectPath

        #endregion
    }
}

