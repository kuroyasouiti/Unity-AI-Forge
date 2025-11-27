using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor
{
    internal static partial class McpCommandProcessor
    {
        #region Prefab Management

        private static object HandlePrefabManage(Dictionary<string, object> payload)
        {
            var operation = EnsureValue(GetString(payload, "operation"), "operation");

            // Check if compilation is in progress and wait if necessary (except for inspect operation)
            Dictionary<string, object> compilationWaitInfo = null;
            if (operation != "inspect")
            {
                compilationWaitInfo = EnsureNoCompilationInProgress("prefabManage", maxWaitSeconds: 30f);
            }

            var result = operation switch
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

            // Add compilation wait info to result if present
            if (compilationWaitInfo != null && result is Dictionary<string, object> resultDict)
            {
                resultDict["compilationWait"] = compilationWaitInfo;
            }

            return result;
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
        #endregion
    }
}
