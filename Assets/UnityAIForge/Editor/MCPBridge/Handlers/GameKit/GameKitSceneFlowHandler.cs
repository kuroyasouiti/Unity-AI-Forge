using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using MCP.Editor.Base;
using UnityAIForge.GameKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit SceneFlow handler: manage scene transitions and flow.
    /// Now uses prefab-based approach with automatic loading via InitializeOnLoad and RuntimeInitializeOnLoadMethod.
    /// </summary>
    public class GameKitSceneFlowHandler : BaseCommandHandler
    {
        private const string RESOURCES_PATH = "Assets/Resources/GameKitSceneFlows";
        private const string PREFAB_EXTENSION = ".prefab";
        
        private static readonly string[] Operations = { 
            "create", "inspect", "delete", "transition",
            "addScene", "removeScene", "updateScene",
            "addTransition", "removeTransition",
            "addSharedScene", "removeSharedScene"
        };

        public override string Category => "gamekitSceneFlow";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateSceneFlow(payload),
                "inspect" => InspectSceneFlow(payload),
                "delete" => DeleteSceneFlow(payload),
                "transition" => TriggerTransition(payload),
                "addScene" => AddScene(payload),
                "removeScene" => RemoveScene(payload),
                "updateScene" => UpdateScene(payload),
                "addTransition" => AddTransition(payload),
                "removeTransition" => RemoveTransition(payload),
                "addSharedScene" => AddSharedScene(payload),
                "removeSharedScene" => RemoveSharedScene(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit SceneFlow operation: {operation}"),
            };
        }

        #region Create SceneFlow

        private object CreateSceneFlow(Dictionary<string, object> payload)
        {
            var flowId = GetString(payload, "flowId") ?? $"SceneFlow_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // 1. Ensure Resources/GameKitSceneFlows directory exists
            EnsureResourcesDirectory();
            
            // 2. Check if prefab already exists
            var prefabPath = GetPrefabPath(flowId);
            if (File.Exists(prefabPath))
            {
                throw new InvalidOperationException($"SceneFlow prefab '{flowId}' already exists at {prefabPath}");
            }
            
            // 3. Create temporary GameObject
            var flowGo = new GameObject(flowId);
            var sceneFlow = flowGo.AddComponent<GameKitSceneFlow>();
            sceneFlow.Initialize(flowId);

            // 4. Add scenes
            if (payload.TryGetValue("scenes", out var scenesObj) && scenesObj is List<object> scenesList)
            {
                foreach (var sceneObj in scenesList)
                {
                    if (sceneObj is Dictionary<string, object> sceneDict)
                    {
                        var name = GetStringFromDict(sceneDict, "name");
                        var scenePath = GetStringFromDict(sceneDict, "scenePath");
                        var loadModeStr = GetStringFromDict(sceneDict, "loadMode") ?? "additive";
                        var loadMode = loadModeStr.ToLowerInvariant() == "single" 
                            ? GameKitSceneFlow.SceneLoadMode.Single 
                            : GameKitSceneFlow.SceneLoadMode.Additive;

                        var sharedScenePaths = new List<string>();
                        if (sceneDict.TryGetValue("sharedScenePaths", out var sharedScenesObj) && sharedScenesObj is List<object> scenePathsList)
                        {
                            sharedScenePaths = scenePathsList.Select(p => p.ToString()).ToList();
                        }

                        sceneFlow.AddScene(name, scenePath, loadMode, sharedScenePaths.ToArray());
                    }
                }
            }

            // 5. Add transitions
            if (payload.TryGetValue("transitions", out var transitionsObj) && transitionsObj is List<object> transitionsList)
            {
                foreach (var transitionObj in transitionsList)
                {
                    if (transitionObj is Dictionary<string, object> transitionDict)
                    {
                        var fromScene = GetStringFromDict(transitionDict, "fromScene");
                        var trigger = GetStringFromDict(transitionDict, "trigger");
                        var toScene = GetStringFromDict(transitionDict, "toScene");
                        
                        if (string.IsNullOrEmpty(fromScene))
                        {
                            Debug.LogWarning("[GameKitSceneFlowHandler] Transition missing 'fromScene'. Transitions must be added to a specific scene.");
                            continue;
                        }
                        
                        sceneFlow.AddTransition(fromScene, trigger, toScene);
                    }
                }
            }

            // 6. Save as prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(flowGo, prefabPath);
            
            // 7. Destroy temporary GameObject
            UnityEngine.Object.DestroyImmediate(flowGo);
            
            Debug.Log($"[GameKitSceneFlow] Created prefab: {prefabPath}");
            
            return CreateSuccessResponse(
                ("flowId", flowId), 
                ("prefabPath", prefabPath),
                ("message", "SceneFlow prefab created successfully"),
                ("autoLoad", "Prefab will be auto-loaded in Play Mode and at runtime")
            );
        }

        #endregion

        #region Add Scene

        private object AddScene(Dictionary<string, object> payload)
        {
            var flowId = GetString(payload, "flowId");
            if (string.IsNullOrEmpty(flowId))
            {
                throw new InvalidOperationException("flowId is required for addScene.");
            }

            var sceneName = GetString(payload, "sceneName");
            var scenePath = GetString(payload, "scenePath");
            if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(scenePath))
            {
                throw new InvalidOperationException("sceneName and scenePath are required for addScene.");
            }

            var loadModeStr = GetString(payload, "loadMode") ?? "additive";
            var loadMode = loadModeStr.ToLowerInvariant() == "single" 
                ? GameKitSceneFlow.SceneLoadMode.Single 
                : GameKitSceneFlow.SceneLoadMode.Additive;

            var sharedScenePaths = new List<string>();
            if (payload.TryGetValue("sharedScenePaths", out var sharedScenesObj) && sharedScenesObj is List<object> scenePathsList)
            {
                sharedScenePaths = scenePathsList.Select(p => p.ToString()).ToList();
            }

            // Edit prefab
            EditPrefab(flowId, (sceneFlow) =>
            {
                sceneFlow.AddScene(sceneName, scenePath, loadMode, sharedScenePaths.ToArray());
            });

            return CreateSuccessResponse(
                ("flowId", flowId), 
                ("sceneName", sceneName), 
                ("scenePath", scenePath),
                ("message", "Scene added to prefab successfully")
            );
        }

        #endregion

        #region Remove Scene

        private object RemoveScene(Dictionary<string, object> payload)
        {
            var flowId = GetString(payload, "flowId");
            if (string.IsNullOrEmpty(flowId))
            {
                throw new InvalidOperationException("flowId is required for removeScene.");
            }

            var sceneName = GetString(payload, "sceneName");
            if (string.IsNullOrEmpty(sceneName))
            {
                throw new InvalidOperationException("sceneName is required for removeScene.");
            }

            bool removed = false;
            EditPrefab(flowId, (sceneFlow) =>
            {
                removed = sceneFlow.RemoveScene(sceneName);
            });

            return CreateSuccessResponse(
                ("flowId", flowId), 
                ("sceneName", sceneName), 
                ("removed", removed),
                ("message", "Scene removed from prefab successfully")
            );
        }

        #endregion

        #region Update Scene

        private object UpdateScene(Dictionary<string, object> payload)
        {
            var flowId = GetString(payload, "flowId");
            if (string.IsNullOrEmpty(flowId))
            {
                throw new InvalidOperationException("flowId is required for updateScene.");
            }

            var sceneName = GetString(payload, "sceneName");
            if (string.IsNullOrEmpty(sceneName))
            {
                throw new InvalidOperationException("sceneName is required for updateScene.");
            }

            var scenePath = GetString(payload, "scenePath");
            GameKitSceneFlow.SceneLoadMode? loadMode = null;
            if (payload.TryGetValue("loadMode", out var loadModeObj) && loadModeObj != null)
            {
                var loadModeStr = loadModeObj.ToString();
                loadMode = loadModeStr.ToLowerInvariant() == "single" 
                    ? GameKitSceneFlow.SceneLoadMode.Single 
                    : GameKitSceneFlow.SceneLoadMode.Additive;
            }

            string[] sharedScenePaths = null;
            if (payload.TryGetValue("sharedScenePaths", out var sharedScenesObj) && sharedScenesObj is List<object> scenePathsList)
            {
                sharedScenePaths = scenePathsList.Select(p => p.ToString()).ToArray();
            }

            bool updated = false;
            EditPrefab(flowId, (sceneFlow) =>
            {
                updated = sceneFlow.UpdateScene(sceneName, scenePath, loadMode, sharedScenePaths);
            });

            return CreateSuccessResponse(
                ("flowId", flowId), 
                ("sceneName", sceneName), 
                ("updated", updated),
                ("message", "Scene updated in prefab successfully")
            );
        }

        #endregion

        #region Add Transition

        private object AddTransition(Dictionary<string, object> payload)
        {
            var flowId = GetString(payload, "flowId");
            if (string.IsNullOrEmpty(flowId))
            {
                throw new InvalidOperationException("flowId is required for addTransition.");
            }

            var fromScene = GetString(payload, "fromScene");
            var trigger = GetString(payload, "trigger");
            var toScene = GetString(payload, "toScene");

            if (string.IsNullOrEmpty(fromScene) || string.IsNullOrEmpty(trigger) || string.IsNullOrEmpty(toScene))
            {
                throw new InvalidOperationException("fromScene, trigger, and toScene are required for addTransition.");
            }

            EditPrefab(flowId, (sceneFlow) =>
            {
                sceneFlow.AddTransition(fromScene, trigger, toScene);
            });

            return CreateSuccessResponse(
                ("flowId", flowId), 
                ("fromScene", fromScene), 
                ("trigger", trigger), 
                ("toScene", toScene),
                ("message", "Transition added to prefab successfully")
            );
        }

        #endregion

        #region Remove Transition

        private object RemoveTransition(Dictionary<string, object> payload)
        {
            var flowId = GetString(payload, "flowId");
            if (string.IsNullOrEmpty(flowId))
            {
                throw new InvalidOperationException("flowId is required for removeTransition.");
            }

            var fromScene = GetString(payload, "fromScene");
            var trigger = GetString(payload, "trigger");

            if (string.IsNullOrEmpty(fromScene) || string.IsNullOrEmpty(trigger))
            {
                throw new InvalidOperationException("fromScene and trigger are required for removeTransition.");
            }

            bool removed = false;
            EditPrefab(flowId, (sceneFlow) =>
            {
                removed = sceneFlow.RemoveTransition(fromScene, trigger);
            });

            return CreateSuccessResponse(
                ("flowId", flowId), 
                ("fromScene", fromScene), 
                ("trigger", trigger), 
                ("removed", removed),
                ("message", "Transition removed from prefab successfully")
            );
        }

        #endregion

        #region Add Shared Scene

        private object AddSharedScene(Dictionary<string, object> payload)
        {
            var flowId = GetString(payload, "flowId");
            if (string.IsNullOrEmpty(flowId))
            {
                throw new InvalidOperationException("flowId is required for addSharedScene.");
            }

            var sceneName = GetString(payload, "sceneName");
            var sharedScenePath = GetString(payload, "sharedScenePath");

            if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(sharedScenePath))
            {
                throw new InvalidOperationException("sceneName and sharedScenePath are required for addSharedScene.");
            }

            EditPrefab(flowId, (sceneFlow) =>
            {
                sceneFlow.AddSharedScenesToScene(sceneName, new[] { sharedScenePath });
            });

            return CreateSuccessResponse(
                ("flowId", flowId), 
                ("sceneName", sceneName), 
                ("sharedScenePath", sharedScenePath),
                ("message", "Shared scene added to prefab successfully")
            );
        }

        #endregion

        #region Remove Shared Scene

        private object RemoveSharedScene(Dictionary<string, object> payload)
        {
            var flowId = GetString(payload, "flowId");
            if (string.IsNullOrEmpty(flowId))
            {
                throw new InvalidOperationException("flowId is required for removeSharedScene.");
            }

            var sceneName = GetString(payload, "sceneName");
            var sharedScenePath = GetString(payload, "sharedScenePath");

            if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(sharedScenePath))
            {
                throw new InvalidOperationException("sceneName and sharedScenePath are required for removeSharedScene.");
            }

            bool removed = false;
            EditPrefab(flowId, (sceneFlow) =>
            {
                removed = sceneFlow.RemoveSharedSceneFromScene(sceneName, sharedScenePath);
            });

            return CreateSuccessResponse(
                ("flowId", flowId), 
                ("sceneName", sceneName), 
                ("sharedScenePath", sharedScenePath), 
                ("removed", removed),
                ("message", "Shared scene removed from prefab successfully")
            );
        }

        #endregion

        #region Inspect SceneFlow

        private object InspectSceneFlow(Dictionary<string, object> payload)
        {
            var flowId = GetString(payload, "flowId");
            if (string.IsNullOrEmpty(flowId))
            {
                throw new InvalidOperationException("flowId is required for inspect.");
            }

            var prefabPath = GetPrefabPath(flowId);
            if (!File.Exists(prefabPath))
            {
                return CreateSuccessResponse(
                    ("found", false), 
                    ("flowId", flowId),
                    ("message", $"SceneFlow prefab '{flowId}' not found")
                );
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var sceneFlow = prefab.GetComponent<GameKitSceneFlow>();
            if (sceneFlow == null)
            {
                return CreateSuccessResponse(
                    ("found", false), 
                    ("flowId", flowId),
                    ("message", "Prefab found but GameKitSceneFlow component not found")
                );
            }

            var info = new Dictionary<string, object>
            {
                { "found", true },
                { "flowId", sceneFlow.FlowId },
                { "prefabPath", prefabPath },
                { "currentScene", sceneFlow.CurrentScene },
                { "autoLoad", "Loaded automatically in Play Mode and at runtime" }
            };

            return CreateSuccessResponse(("sceneFlow", info));
        }

        #endregion

        #region Delete SceneFlow

        private object DeleteSceneFlow(Dictionary<string, object> payload)
        {
            var flowId = GetString(payload, "flowId");
            if (string.IsNullOrEmpty(flowId))
            {
                throw new InvalidOperationException("flowId is required for delete.");
            }

            var prefabPath = GetPrefabPath(flowId);
            if (!File.Exists(prefabPath))
            {
                throw new InvalidOperationException($"SceneFlow prefab '{flowId}' not found at {prefabPath}");
            }

            // Delete prefab file
            AssetDatabase.DeleteAsset(prefabPath);
            AssetDatabase.Refresh();
            
            Debug.Log($"[GameKitSceneFlow] Deleted prefab: {prefabPath}");

            return CreateSuccessResponse(
                ("flowId", flowId), 
                ("deleted", true),
                ("message", "SceneFlow prefab deleted successfully")
            );
        }

        #endregion

        #region Trigger Transition

        private object TriggerTransition(Dictionary<string, object> payload)
        {
            var triggerName = GetString(payload, "triggerName");
            if (string.IsNullOrEmpty(triggerName))
            {
                throw new InvalidOperationException("triggerName is required for transition.");
            }

            // This operation is meant for runtime, but we can log it in editor
            Debug.Log($"[GameKitSceneFlow] Transition trigger '{triggerName}' would be executed at runtime.");

            return CreateSuccessResponse(("triggerName", triggerName), ("note", "Transition will execute at runtime"));
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Ensures that Resources/GameKitSceneFlows directory exists.
        /// </summary>
        private void EnsureResourcesDirectory()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            if (!AssetDatabase.IsValidFolder(RESOURCES_PATH))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "GameKitSceneFlows");
            }
        }

        /// <summary>
        /// Gets the prefab path for a given flowId.
        /// </summary>
        private string GetPrefabPath(string flowId)
        {
            return $"{RESOURCES_PATH}/{flowId}{PREFAB_EXTENSION}";
        }

        /// <summary>
        /// Edits a SceneFlow prefab with the given action.
        /// </summary>
        private void EditPrefab(string flowId, System.Action<GameKitSceneFlow> editAction)
        {
            var prefabPath = GetPrefabPath(flowId);
            if (!File.Exists(prefabPath))
            {
                throw new InvalidOperationException($"SceneFlow prefab '{flowId}' not found at {prefabPath}");
            }

            // Edit prefab using PrefabUtility.EditPrefabContentsScope
            using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                var sceneFlow = scope.prefabContentsRoot.GetComponent<GameKitSceneFlow>();
                if (sceneFlow == null)
                {
                    throw new InvalidOperationException($"GameKitSceneFlow component not found on prefab '{flowId}'");
                }

                editAction(sceneFlow);
                
                // Changes are automatically saved when scope is disposed
                EditorUtility.SetDirty(sceneFlow);
            }

            Debug.Log($"[GameKitSceneFlow] Edited prefab: {prefabPath}");
        }

        private string GetStringFromDict(Dictionary<string, object> dict, string key)
        {
            return dict.TryGetValue(key, out var value) ? value?.ToString() : null;
        }

        #endregion
    }
}

