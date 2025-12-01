using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;
using UnityAIForge.GameKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit SceneFlow handler: manage scene transitions and flow.
    /// </summary>
    public class GameKitSceneFlowHandler : BaseCommandHandler
    {
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
            var managerScenePath = GetString(payload, "managerScenePath");

            // Create SceneFlow GameObject
            var flowGo = new GameObject(flowId);
            Undo.RegisterCreatedObjectUndo(flowGo, "Create SceneFlow");

            var sceneFlow = Undo.AddComponent<GameKitSceneFlow>(flowGo);
            sceneFlow.Initialize(flowId);

            // Add scenes
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
                        // Support legacy "sharedGroups" for backward compatibility (treated as scene paths)
                        else if (sceneDict.TryGetValue("sharedGroups", out var sharedGroupsObj) && sharedGroupsObj is List<object> groupsList)
                        {
                            sharedScenePaths = groupsList.Select(g => g.ToString()).ToList();
                        }

                        sceneFlow.AddScene(name, scenePath, loadMode, sharedScenePaths.ToArray());
                    }
                }
            }

            // Add transitions (now scene-centric: each scene defines its own transitions)
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

            // Legacy support: Add shared scene groups (deprecated - use sharedScenePaths in scene definition instead)
            if (payload.TryGetValue("sharedSceneGroups", out var sharedGroupsDict) && sharedGroupsDict is Dictionary<string, object> groupsDict)
            {
                Debug.LogWarning("[GameKitSceneFlowHandler] 'sharedSceneGroups' is deprecated. Use 'sharedScenePaths' in scene definitions instead.");
                // For backward compatibility, we could add these to scenes, but it's better to just warn
            }

            EditorSceneManager.MarkSceneDirty(flowGo.scene);
            return CreateSuccessResponse(("flowId", flowId), ("path", BuildGameObjectPath(flowGo)));
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

            var sceneFlow = FindSceneFlowById(flowId);
            if (sceneFlow == null)
            {
                throw new InvalidOperationException($"SceneFlow with ID '{flowId}' not found.");
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

            Undo.RecordObject(sceneFlow, "Add Scene");
            sceneFlow.AddScene(sceneName, scenePath, loadMode, sharedScenePaths.ToArray());
            EditorSceneManager.MarkSceneDirty(sceneFlow.gameObject.scene);

            return CreateSuccessResponse(("flowId", flowId), ("sceneName", sceneName), ("scenePath", scenePath));
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

            var sceneFlow = FindSceneFlowById(flowId);
            if (sceneFlow == null)
            {
                throw new InvalidOperationException($"SceneFlow with ID '{flowId}' not found.");
            }

            var sceneName = GetString(payload, "sceneName");
            if (string.IsNullOrEmpty(sceneName))
            {
                throw new InvalidOperationException("sceneName is required for removeScene.");
            }

            Undo.RecordObject(sceneFlow, "Remove Scene");
            bool removed = sceneFlow.RemoveScene(sceneName);
            EditorSceneManager.MarkSceneDirty(sceneFlow.gameObject.scene);

            return CreateSuccessResponse(("flowId", flowId), ("sceneName", sceneName), ("removed", removed));
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

            var sceneFlow = FindSceneFlowById(flowId);
            if (sceneFlow == null)
            {
                throw new InvalidOperationException($"SceneFlow with ID '{flowId}' not found.");
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

            Undo.RecordObject(sceneFlow, "Update Scene");
            bool updated = sceneFlow.UpdateScene(sceneName, scenePath, loadMode, sharedScenePaths);
            EditorSceneManager.MarkSceneDirty(sceneFlow.gameObject.scene);

            return CreateSuccessResponse(("flowId", flowId), ("sceneName", sceneName), ("updated", updated));
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

            var sceneFlow = FindSceneFlowById(flowId);
            if (sceneFlow == null)
            {
                throw new InvalidOperationException($"SceneFlow with ID '{flowId}' not found.");
            }

            var fromScene = GetString(payload, "fromScene");
            var trigger = GetString(payload, "trigger");
            var toScene = GetString(payload, "toScene");

            if (string.IsNullOrEmpty(fromScene) || string.IsNullOrEmpty(trigger) || string.IsNullOrEmpty(toScene))
            {
                throw new InvalidOperationException("fromScene, trigger, and toScene are required for addTransition.");
            }

            Undo.RecordObject(sceneFlow, "Add Transition");
            sceneFlow.AddTransition(fromScene, trigger, toScene);
            EditorSceneManager.MarkSceneDirty(sceneFlow.gameObject.scene);

            return CreateSuccessResponse(("flowId", flowId), ("fromScene", fromScene), ("trigger", trigger), ("toScene", toScene));
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

            var sceneFlow = FindSceneFlowById(flowId);
            if (sceneFlow == null)
            {
                throw new InvalidOperationException($"SceneFlow with ID '{flowId}' not found.");
            }

            var fromScene = GetString(payload, "fromScene");
            var trigger = GetString(payload, "trigger");

            if (string.IsNullOrEmpty(fromScene) || string.IsNullOrEmpty(trigger))
            {
                throw new InvalidOperationException("fromScene and trigger are required for removeTransition.");
            }

            Undo.RecordObject(sceneFlow, "Remove Transition");
            bool removed = sceneFlow.RemoveTransition(fromScene, trigger);
            EditorSceneManager.MarkSceneDirty(sceneFlow.gameObject.scene);

            return CreateSuccessResponse(("flowId", flowId), ("fromScene", fromScene), ("trigger", trigger), ("removed", removed));
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

            var sceneFlow = FindSceneFlowById(flowId);
            if (sceneFlow == null)
            {
                throw new InvalidOperationException($"SceneFlow with ID '{flowId}' not found.");
            }

            var sceneName = GetString(payload, "sceneName");
            var sharedScenePath = GetString(payload, "sharedScenePath");

            if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(sharedScenePath))
            {
                throw new InvalidOperationException("sceneName and sharedScenePath are required for addSharedScene.");
            }

            Undo.RecordObject(sceneFlow, "Add Shared Scene");
            sceneFlow.AddSharedScenesToScene(sceneName, new[] { sharedScenePath });
            EditorSceneManager.MarkSceneDirty(sceneFlow.gameObject.scene);

            return CreateSuccessResponse(("flowId", flowId), ("sceneName", sceneName), ("sharedScenePath", sharedScenePath));
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

            var sceneFlow = FindSceneFlowById(flowId);
            if (sceneFlow == null)
            {
                throw new InvalidOperationException($"SceneFlow with ID '{flowId}' not found.");
            }

            var sceneName = GetString(payload, "sceneName");
            var sharedScenePath = GetString(payload, "sharedScenePath");

            if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(sharedScenePath))
            {
                throw new InvalidOperationException("sceneName and sharedScenePath are required for removeSharedScene.");
            }

            Undo.RecordObject(sceneFlow, "Remove Shared Scene");
            bool removed = sceneFlow.RemoveSharedSceneFromScene(sceneName, sharedScenePath);
            EditorSceneManager.MarkSceneDirty(sceneFlow.gameObject.scene);

            return CreateSuccessResponse(("flowId", flowId), ("sceneName", sceneName), ("sharedScenePath", sharedScenePath), ("removed", removed));
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

            var sceneFlow = FindSceneFlowById(flowId);
            if (sceneFlow == null)
            {
                return CreateSuccessResponse(("found", false), ("flowId", flowId));
            }

            var info = new Dictionary<string, object>
            {
                { "found", true },
                { "flowId", sceneFlow.FlowId },
                { "path", BuildGameObjectPath(sceneFlow.gameObject) },
                { "currentScene", sceneFlow.CurrentScene }
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

            var sceneFlow = FindSceneFlowById(flowId);
            if (sceneFlow == null)
            {
                throw new InvalidOperationException($"SceneFlow with ID '{flowId}' not found.");
            }

            var scene = sceneFlow.gameObject.scene;
            Undo.DestroyObjectImmediate(sceneFlow.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(("flowId", flowId), ("deleted", true));
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

        private GameKitSceneFlow FindSceneFlowById(string flowId)
        {
            var sceneFlows = UnityEngine.Object.FindObjectsByType<GameKitSceneFlow>(FindObjectsSortMode.None);
            foreach (var sceneFlow in sceneFlows)
            {
                if (sceneFlow.FlowId == flowId)
                {
                    return sceneFlow;
                }
            }
            return null;
        }

        private string GetStringFromDict(Dictionary<string, object> dict, string key)
        {
            return dict.TryGetValue(key, out var value) ? value?.ToString() : null;
        }

        private string BuildGameObjectPath(GameObject go)
        {
            var path = go.name;
            var current = go.transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        #endregion
    }
}

