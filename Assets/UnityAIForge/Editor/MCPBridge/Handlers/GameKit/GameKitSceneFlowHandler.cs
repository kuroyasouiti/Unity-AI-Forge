using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit SceneFlow handler: manage scene transitions and flow.
    /// Uses code generation to produce standalone SceneFlow scripts with zero package dependency.
    /// </summary>
    public class GameKitSceneFlowHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "inspect", "delete", "transition",
            "addScene", "removeScene", "updateScene",
            "addTransition", "removeTransition",
            "addSharedScene", "removeSharedScene"
        };

        public override string Category => "gamekitSceneFlow";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

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
            var targetPath = GetString(payload, "targetPath");

            // Create or resolve target GameObject
            GameObject targetGo;
            if (!string.IsNullOrEmpty(targetPath))
            {
                targetGo = ResolveGameObject(targetPath);
                if (targetGo == null)
                    throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }
            else
            {
                targetGo = new GameObject(flowId);
                Undo.RegisterCreatedObjectUndo(targetGo, "Create GameKit SceneFlow");
            }

            // Check if already has a scene flow component
            var existingFlow = CodeGenHelper.FindComponentByField(targetGo, "flowId", null);
            if (existingFlow != null)
            {
                throw new InvalidOperationException(
                    $"GameObject '{BuildGameObjectPath(targetGo)}' already has a SceneFlow component.");
            }

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(flowId, "SceneFlow");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "FLOW_ID", flowId }
            };

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "SceneFlow", flowId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate SceneFlow script.");
            }

            // If component was added immediately, configure scenes and transitions
            if (result.TryGetValue("componentAdded", out var added) && (bool)added)
            {
                var component = CodeGenHelper.FindComponentByField(targetGo, "flowId", flowId);
                if (component != null)
                {
                    var so = new SerializedObject(component);

                    // Add scenes
                    if (payload.TryGetValue("scenes", out var scenesObj) && scenesObj is List<object> scenesList)
                    {
                        var scenesProp = so.FindProperty("scenes");
                        if (scenesProp != null)
                        {
                            foreach (var sceneObj in scenesList)
                            {
                                if (sceneObj is Dictionary<string, object> sceneDict)
                                {
                                    AddSceneToArray(scenesProp, sceneDict);
                                }
                            }
                        }
                    }

                    // Add transitions
                    if (payload.TryGetValue("transitions", out var transitionsObj) && transitionsObj is List<object> transitionsList)
                    {
                        var scenesProp = so.FindProperty("scenes");
                        if (scenesProp != null)
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

                                    AddTransitionToSceneArray(scenesProp, fromScene, trigger, toScene);
                                }
                            }
                        }
                    }

                    so.ApplyModifiedProperties();
                }
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["flowId"] = flowId;
            result["path"] = BuildGameObjectPath(targetGo);

            return result;
        }

        #endregion

        #region Inspect SceneFlow

        private object InspectSceneFlow(Dictionary<string, object> payload)
        {
            var component = ResolveSceneFlowComponent(payload);
            var so = new SerializedObject(component);

            var flowIdValue = so.FindProperty("flowId").stringValue;
            var currentScene = so.FindProperty("currentSceneName").stringValue;

            // Read scenes
            var sceneNames = new List<string>();
            var scenesProp = so.FindProperty("scenes");
            if (scenesProp != null)
            {
                for (int i = 0; i < scenesProp.arraySize; i++)
                {
                    var entry = scenesProp.GetArrayElementAtIndex(i);
                    var name = entry.FindPropertyRelative("sceneName").stringValue;
                    if (!string.IsNullOrEmpty(name))
                        sceneNames.Add(name);
                }
            }

            // Read transitions per scene
            var transitionsList = new List<Dictionary<string, string>>();
            if (scenesProp != null)
            {
                for (int i = 0; i < scenesProp.arraySize; i++)
                {
                    var entry = scenesProp.GetArrayElementAtIndex(i);
                    var sceneName = entry.FindPropertyRelative("sceneName").stringValue;
                    var transitions = entry.FindPropertyRelative("transitions");
                    if (transitions != null)
                    {
                        for (int j = 0; j < transitions.arraySize; j++)
                        {
                            var t = transitions.GetArrayElementAtIndex(j);
                            transitionsList.Add(new Dictionary<string, string>
                            {
                                { "fromScene", sceneName },
                                { "trigger", t.FindPropertyRelative("trigger").stringValue },
                                { "toScene", t.FindPropertyRelative("toScene").stringValue }
                            });
                        }
                    }
                }
            }

            var info = new Dictionary<string, object>
            {
                { "found", true },
                { "flowId", flowIdValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "currentScene", currentScene },
                { "sceneCount", sceneNames.Count },
                { "scenes", sceneNames },
                { "transitions", transitionsList }
            };

            return CreateSuccessResponse(("sceneFlow", info));
        }

        #endregion

        #region Delete SceneFlow

        private object DeleteSceneFlow(Dictionary<string, object> payload)
        {
            var component = ResolveSceneFlowComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var flowId = new SerializedObject(component).FindProperty("flowId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component);

            // Clean up generated script from tracker
            ScriptGenerator.Delete(flowId);

            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("flowId", flowId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Add Scene

        private object AddScene(Dictionary<string, object> payload)
        {
            var component = ResolveSceneFlowComponent(payload);

            var sceneName = GetString(payload, "sceneName");
            var scenePath = GetString(payload, "scenePath");
            if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(scenePath))
            {
                throw new InvalidOperationException("sceneName and scenePath are required for addScene.");
            }

            Undo.RecordObject(component, "Add Scene to SceneFlow");

            var so = new SerializedObject(component);
            var scenesProp = so.FindProperty("scenes");
            if (scenesProp == null)
                throw new InvalidOperationException("scenes property not found on SceneFlow component.");

            var sceneDict = new Dictionary<string, object>
            {
                { "name", sceneName },
                { "scenePath", scenePath }
            };

            if (payload.TryGetValue("loadMode", out var loadModeObj))
                sceneDict["loadMode"] = loadModeObj.ToString();

            if (payload.TryGetValue("sharedScenePaths", out var sharedObj) && sharedObj is List<object> sharedList)
                sceneDict["sharedScenePaths"] = sharedList;

            AddSceneToArray(scenesProp, sceneDict);
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var flowId = new SerializedObject(component).FindProperty("flowId").stringValue;

            return CreateSuccessResponse(
                ("flowId", flowId),
                ("sceneName", sceneName),
                ("scenePath", scenePath),
                ("message", "Scene added successfully")
            );
        }

        #endregion

        #region Remove Scene

        private object RemoveScene(Dictionary<string, object> payload)
        {
            var component = ResolveSceneFlowComponent(payload);

            var sceneName = GetString(payload, "sceneName");
            if (string.IsNullOrEmpty(sceneName))
            {
                throw new InvalidOperationException("sceneName is required for removeScene.");
            }

            Undo.RecordObject(component, "Remove Scene from SceneFlow");

            var so = new SerializedObject(component);
            var scenesProp = so.FindProperty("scenes");
            bool removed = false;

            if (scenesProp != null)
            {
                for (int i = scenesProp.arraySize - 1; i >= 0; i--)
                {
                    var entry = scenesProp.GetArrayElementAtIndex(i);
                    if (entry.FindPropertyRelative("sceneName").stringValue == sceneName)
                    {
                        scenesProp.DeleteArrayElementAtIndex(i);
                        removed = true;
                    }
                }
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var flowId = new SerializedObject(component).FindProperty("flowId").stringValue;

            return CreateSuccessResponse(
                ("flowId", flowId),
                ("sceneName", sceneName),
                ("removed", removed),
                ("message", "Scene removed successfully")
            );
        }

        #endregion

        #region Update Scene

        private object UpdateScene(Dictionary<string, object> payload)
        {
            var component = ResolveSceneFlowComponent(payload);

            var sceneName = GetString(payload, "sceneName");
            if (string.IsNullOrEmpty(sceneName))
            {
                throw new InvalidOperationException("sceneName is required for updateScene.");
            }

            Undo.RecordObject(component, "Update Scene in SceneFlow");

            var so = new SerializedObject(component);
            var scenesProp = so.FindProperty("scenes");
            bool updated = false;

            if (scenesProp != null)
            {
                for (int i = 0; i < scenesProp.arraySize; i++)
                {
                    var entry = scenesProp.GetArrayElementAtIndex(i);
                    if (entry.FindPropertyRelative("sceneName").stringValue == sceneName)
                    {
                        // Update scenePath if provided
                        var scenePath = GetString(payload, "scenePath");
                        if (!string.IsNullOrEmpty(scenePath))
                        {
                            entry.FindPropertyRelative("scenePath").stringValue = scenePath;
                        }

                        // Update loadMode if provided
                        if (payload.TryGetValue("loadMode", out var loadModeObj) && loadModeObj != null)
                        {
                            var loadModeStr = ParseLoadMode(loadModeObj.ToString());
                            var loadModeProp = entry.FindPropertyRelative("loadMode");
                            var names = loadModeProp.enumDisplayNames;
                            for (int j = 0; j < names.Length; j++)
                            {
                                if (string.Equals(names[j], loadModeStr, StringComparison.OrdinalIgnoreCase))
                                {
                                    loadModeProp.enumValueIndex = j;
                                    break;
                                }
                            }
                        }

                        // Update sharedScenePaths if provided
                        if (payload.TryGetValue("sharedScenePaths", out var sharedObj) && sharedObj is List<object> sharedList)
                        {
                            var sharedProp = entry.FindPropertyRelative("sharedScenePaths");
                            if (sharedProp != null)
                            {
                                sharedProp.ClearArray();
                                for (int j = 0; j < sharedList.Count; j++)
                                {
                                    sharedProp.InsertArrayElementAtIndex(j);
                                    sharedProp.GetArrayElementAtIndex(j).stringValue = sharedList[j].ToString();
                                }
                            }
                        }

                        updated = true;
                        break;
                    }
                }
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var flowId = new SerializedObject(component).FindProperty("flowId").stringValue;

            return CreateSuccessResponse(
                ("flowId", flowId),
                ("sceneName", sceneName),
                ("updated", updated),
                ("message", "Scene updated successfully")
            );
        }

        #endregion

        #region Add Transition

        private object AddTransition(Dictionary<string, object> payload)
        {
            var component = ResolveSceneFlowComponent(payload);

            var fromScene = GetString(payload, "fromScene");
            var trigger = GetString(payload, "trigger");
            var toScene = GetString(payload, "toScene");

            if (string.IsNullOrEmpty(fromScene) || string.IsNullOrEmpty(trigger) || string.IsNullOrEmpty(toScene))
            {
                throw new InvalidOperationException("fromScene, trigger, and toScene are required for addTransition.");
            }

            Undo.RecordObject(component, "Add Transition to SceneFlow");

            var so = new SerializedObject(component);
            var scenesProp = so.FindProperty("scenes");
            if (scenesProp == null)
                throw new InvalidOperationException("scenes property not found on SceneFlow component.");

            AddTransitionToSceneArray(scenesProp, fromScene, trigger, toScene);
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var flowId = new SerializedObject(component).FindProperty("flowId").stringValue;

            return CreateSuccessResponse(
                ("flowId", flowId),
                ("fromScene", fromScene),
                ("trigger", trigger),
                ("toScene", toScene),
                ("message", "Transition added successfully")
            );
        }

        #endregion

        #region Remove Transition

        private object RemoveTransition(Dictionary<string, object> payload)
        {
            var component = ResolveSceneFlowComponent(payload);

            var fromScene = GetString(payload, "fromScene");
            var trigger = GetString(payload, "trigger");

            if (string.IsNullOrEmpty(fromScene) || string.IsNullOrEmpty(trigger))
            {
                throw new InvalidOperationException("fromScene and trigger are required for removeTransition.");
            }

            Undo.RecordObject(component, "Remove Transition from SceneFlow");

            var so = new SerializedObject(component);
            var scenesProp = so.FindProperty("scenes");
            bool removed = false;

            if (scenesProp != null)
            {
                for (int i = 0; i < scenesProp.arraySize; i++)
                {
                    var entry = scenesProp.GetArrayElementAtIndex(i);
                    if (entry.FindPropertyRelative("sceneName").stringValue == fromScene)
                    {
                        var transitions = entry.FindPropertyRelative("transitions");
                        if (transitions != null)
                        {
                            for (int j = transitions.arraySize - 1; j >= 0; j--)
                            {
                                var t = transitions.GetArrayElementAtIndex(j);
                                if (t.FindPropertyRelative("trigger").stringValue == trigger)
                                {
                                    transitions.DeleteArrayElementAtIndex(j);
                                    removed = true;
                                }
                            }
                        }
                        break;
                    }
                }
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var flowId = new SerializedObject(component).FindProperty("flowId").stringValue;

            return CreateSuccessResponse(
                ("flowId", flowId),
                ("fromScene", fromScene),
                ("trigger", trigger),
                ("removed", removed),
                ("message", "Transition removed successfully")
            );
        }

        #endregion

        #region Add Shared Scene

        private object AddSharedScene(Dictionary<string, object> payload)
        {
            var component = ResolveSceneFlowComponent(payload);

            var sceneName = GetString(payload, "sceneName");
            var sharedScenePath = GetString(payload, "sharedScenePath");

            if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(sharedScenePath))
            {
                throw new InvalidOperationException("sceneName and sharedScenePath are required for addSharedScene.");
            }

            Undo.RecordObject(component, "Add Shared Scene to SceneFlow");

            var so = new SerializedObject(component);
            var scenesProp = so.FindProperty("scenes");
            bool added = false;

            if (scenesProp != null)
            {
                for (int i = 0; i < scenesProp.arraySize; i++)
                {
                    var entry = scenesProp.GetArrayElementAtIndex(i);
                    if (entry.FindPropertyRelative("sceneName").stringValue == sceneName)
                    {
                        var sharedProp = entry.FindPropertyRelative("sharedScenePaths");
                        if (sharedProp != null)
                        {
                            // Check if already exists
                            bool exists = false;
                            for (int j = 0; j < sharedProp.arraySize; j++)
                            {
                                if (sharedProp.GetArrayElementAtIndex(j).stringValue == sharedScenePath)
                                {
                                    exists = true;
                                    break;
                                }
                            }

                            if (!exists)
                            {
                                sharedProp.InsertArrayElementAtIndex(sharedProp.arraySize);
                                sharedProp.GetArrayElementAtIndex(sharedProp.arraySize - 1).stringValue = sharedScenePath;
                                added = true;
                            }
                        }
                        break;
                    }
                }
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var flowId = new SerializedObject(component).FindProperty("flowId").stringValue;

            return CreateSuccessResponse(
                ("flowId", flowId),
                ("sceneName", sceneName),
                ("sharedScenePath", sharedScenePath),
                ("added", added),
                ("message", "Shared scene added successfully")
            );
        }

        #endregion

        #region Remove Shared Scene

        private object RemoveSharedScene(Dictionary<string, object> payload)
        {
            var component = ResolveSceneFlowComponent(payload);

            var sceneName = GetString(payload, "sceneName");
            var sharedScenePath = GetString(payload, "sharedScenePath");

            if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(sharedScenePath))
            {
                throw new InvalidOperationException("sceneName and sharedScenePath are required for removeSharedScene.");
            }

            Undo.RecordObject(component, "Remove Shared Scene from SceneFlow");

            var so = new SerializedObject(component);
            var scenesProp = so.FindProperty("scenes");
            bool removed = false;

            if (scenesProp != null)
            {
                for (int i = 0; i < scenesProp.arraySize; i++)
                {
                    var entry = scenesProp.GetArrayElementAtIndex(i);
                    if (entry.FindPropertyRelative("sceneName").stringValue == sceneName)
                    {
                        var sharedProp = entry.FindPropertyRelative("sharedScenePaths");
                        if (sharedProp != null)
                        {
                            for (int j = sharedProp.arraySize - 1; j >= 0; j--)
                            {
                                if (sharedProp.GetArrayElementAtIndex(j).stringValue == sharedScenePath)
                                {
                                    sharedProp.DeleteArrayElementAtIndex(j);
                                    removed = true;
                                }
                            }
                        }
                        break;
                    }
                }
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var flowId = new SerializedObject(component).FindProperty("flowId").stringValue;

            return CreateSuccessResponse(
                ("flowId", flowId),
                ("sceneName", sceneName),
                ("sharedScenePath", sharedScenePath),
                ("removed", removed),
                ("message", "Shared scene removed successfully")
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

            // This operation is meant for runtime; log in editor
            Debug.Log($"[GameKitSceneFlow] Transition trigger '{triggerName}' would be executed at runtime.");

            return CreateSuccessResponse(
                ("triggerName", triggerName),
                ("note", "Transition will execute at runtime")
            );
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Resolves a SceneFlow component by flowId or targetPath.
        /// Returns Component instead of a specific GameKit type since the type is generated at runtime.
        /// </summary>
        private Component ResolveSceneFlowComponent(Dictionary<string, object> payload)
        {
            // Try by flowId first
            var flowId = GetString(payload, "flowId");
            if (!string.IsNullOrEmpty(flowId))
            {
                var flowById = CodeGenHelper.FindComponentInSceneByField("flowId", flowId);
                if (flowById != null)
                    return flowById;
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var flowByPath = CodeGenHelper.FindComponentByField(targetGo, "flowId", null);
                    if (flowByPath != null)
                        return flowByPath;

                    throw new InvalidOperationException($"No SceneFlow component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either flowId or targetPath is required.");
        }

        /// <summary>
        /// Adds a scene entry to the scenes SerializedProperty array.
        /// </summary>
        private void AddSceneToArray(SerializedProperty scenesProp, Dictionary<string, object> sceneDict)
        {
            var name = GetStringFromDict(sceneDict, "name");
            var scenePath = GetStringFromDict(sceneDict, "scenePath");
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(scenePath))
                return;

            // Check for duplicates
            for (int i = 0; i < scenesProp.arraySize; i++)
            {
                if (scenesProp.GetArrayElementAtIndex(i).FindPropertyRelative("sceneName").stringValue == name)
                    return;
            }

            var idx = scenesProp.arraySize;
            scenesProp.InsertArrayElementAtIndex(idx);
            var entry = scenesProp.GetArrayElementAtIndex(idx);

            entry.FindPropertyRelative("sceneName").stringValue = name;
            entry.FindPropertyRelative("scenePath").stringValue = scenePath;

            // Set load mode
            var loadModeStr = GetStringFromDict(sceneDict, "loadMode") ?? "single";
            var loadModeValue = ParseLoadMode(loadModeStr);
            var loadModeProp = entry.FindPropertyRelative("loadMode");
            if (loadModeProp != null)
            {
                var names = loadModeProp.enumDisplayNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], loadModeValue, StringComparison.OrdinalIgnoreCase))
                    {
                        loadModeProp.enumValueIndex = i;
                        break;
                    }
                }
            }

            // Set shared scene paths
            if (sceneDict.TryGetValue("sharedScenePaths", out var sharedObj) && sharedObj is List<object> sharedList)
            {
                var sharedProp = entry.FindPropertyRelative("sharedScenePaths");
                if (sharedProp != null)
                {
                    sharedProp.ClearArray();
                    for (int i = 0; i < sharedList.Count; i++)
                    {
                        sharedProp.InsertArrayElementAtIndex(i);
                        sharedProp.GetArrayElementAtIndex(i).stringValue = sharedList[i].ToString();
                    }
                }
            }

            // Clear transitions array for new entry
            var transitionsProp = entry.FindPropertyRelative("transitions");
            if (transitionsProp != null)
            {
                transitionsProp.ClearArray();
            }
        }

        /// <summary>
        /// Adds a transition to a scene entry in the scenes SerializedProperty array.
        /// </summary>
        private void AddTransitionToSceneArray(SerializedProperty scenesProp, string fromScene, string trigger, string toScene)
        {
            for (int i = 0; i < scenesProp.arraySize; i++)
            {
                var entry = scenesProp.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("sceneName").stringValue == fromScene)
                {
                    var transitions = entry.FindPropertyRelative("transitions");
                    if (transitions == null)
                        return;

                    // Check for duplicate trigger
                    for (int j = 0; j < transitions.arraySize; j++)
                    {
                        if (transitions.GetArrayElementAtIndex(j).FindPropertyRelative("trigger").stringValue == trigger)
                            return;
                    }

                    var idx = transitions.arraySize;
                    transitions.InsertArrayElementAtIndex(idx);
                    var t = transitions.GetArrayElementAtIndex(idx);
                    t.FindPropertyRelative("trigger").stringValue = trigger;
                    t.FindPropertyRelative("toScene").stringValue = toScene;
                    return;
                }
            }
        }

        /// <summary>
        /// Parses a load mode string to its PascalCase enum name.
        /// </summary>
        private string ParseLoadMode(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "single" => "Single",
                "additive" => "Additive",
                _ => "Single"
            };
        }

        private string GetStringFromDict(Dictionary<string, object> dict, string key)
        {
            return dict.TryGetValue(key, out var value) ? value?.ToString() : null;
        }

        #endregion
    }
}
