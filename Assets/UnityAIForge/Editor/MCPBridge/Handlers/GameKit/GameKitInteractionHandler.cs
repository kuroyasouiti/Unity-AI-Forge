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
    /// GameKit Interaction handler: create and manage interaction triggers.
    /// Uses code generation to produce standalone scripts with zero package dependency.
    /// </summary>
    public class GameKitInteractionHandler : BaseCommandHandler
    {
        private static readonly string[] Operations = { "create", "update", "inspect", "delete" };

        public override string Category => "gamekitInteraction";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateInteraction(payload),
                "update" => UpdateInteraction(payload),
                "inspect" => InspectInteraction(payload),
                "delete" => DeleteInteraction(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit Interaction operation: {operation}"),
            };
        }

        #region Create Interaction

        private object CreateInteraction(Dictionary<string, object> payload)
        {
            var interactionId = GetString(payload, "interactionId") ?? $"Interaction_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var parentPath = GetString(payload, "parentPath");
            var triggerStr = GetString(payload, "triggerType") ?? "trigger";

            // Create GameObject
            var interactionGo = new GameObject(interactionId);
            Undo.RegisterCreatedObjectUndo(interactionGo, "Create GameKit Interaction");

            if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = ResolveGameObject(parentPath);
                interactionGo.transform.SetParent(parent.transform, false);
            }

            // Check if already has an interaction component (by checking for interactionId field)
            var existingInteraction = CodeGenHelper.FindComponentByField(interactionGo, "interactionId", null);
            if (existingInteraction != null)
            {
                throw new InvalidOperationException(
                    $"GameObject '{interactionId}' already has an Interaction component.");
            }

            var triggerType = ParseTriggerType(triggerStr);
            var proximityRadius = GetFloat(payload, "proximityRadius", 3f);
            var inputKey = GetString(payload, "inputKey") ?? "E";
            var allowRepeated = GetBool(payload, "allowRepeatedTrigger", true);
            var triggerCooldown = GetFloat(payload, "triggerCooldown", 0f);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(interactionId, "Interaction");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "INTERACTION_ID", interactionId },
                { "TRIGGER_TYPE", triggerType },
                { "PROXIMITY_RADIUS", proximityRadius },
                { "INPUT_KEY", inputKey },
                { "ALLOW_REPEATED", allowRepeated ? "true" : "false" },
                { "TRIGGER_COOLDOWN", triggerCooldown }
            };

            // Build properties to set after component is added
            var propertiesToSet = new Dictionary<string, object>();

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                interactionGo, "Interaction", interactionId, className, variables, outputDir,
                propertiesToSet.Count > 0 ? propertiesToSet : null);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate Interaction script.");
            }

            // Add collider for trigger/collision types
            if (triggerType == "Trigger" || triggerType == "Collision")
            {
                var shapeStr = GetString(payload, "triggerShape") ?? "box";
                var is2D = GetBool(payload, "is2D", false);
                AddCollider(interactionGo, shapeStr, triggerType == "Trigger", is2D, payload);
            }

            EditorSceneManager.MarkSceneDirty(interactionGo.scene);

            result["interactionId"] = interactionId;
            result["path"] = BuildGameObjectPath(interactionGo);
            result["triggerType"] = triggerType;

            return result;
        }

        private void AddCollider(GameObject go, string shape, bool isTrigger, bool is2D, Dictionary<string, object> payload)
        {
            if (is2D)
            {
                // 2D Colliders
                switch (shape.ToLowerInvariant())
                {
                    case "box":
                        var boxCol2D = Undo.AddComponent<BoxCollider2D>(go);
                        boxCol2D.isTrigger = isTrigger;
                        if (payload.TryGetValue("triggerSize", out var sizeObj2D) && sizeObj2D is Dictionary<string, object> sizeDict2D)
                        {
                            boxCol2D.size = GetVector2FromDict(sizeDict2D, Vector2.one);
                        }
                        break;

                    case "circle":
                    case "sphere":
                        var circleCol = Undo.AddComponent<CircleCollider2D>(go);
                        circleCol.isTrigger = isTrigger;
                        if (payload.TryGetValue("triggerSize", out var radiusObj2D) && radiusObj2D is Dictionary<string, object> radiusDict2D)
                        {
                            if (radiusDict2D.TryGetValue("x", out var xObj2D))
                            {
                                circleCol.radius = Convert.ToSingle(xObj2D);
                            }
                        }
                        break;

                    case "capsule":
                        var capsuleCol2D = Undo.AddComponent<CapsuleCollider2D>(go);
                        capsuleCol2D.isTrigger = isTrigger;
                        if (payload.TryGetValue("triggerSize", out var capsuleSizeObj) && capsuleSizeObj is Dictionary<string, object> capsuleSizeDict)
                        {
                            capsuleCol2D.size = GetVector2FromDict(capsuleSizeDict, new Vector2(1f, 2f));
                        }
                        break;

                    case "polygon":
                        var polyCol = Undo.AddComponent<PolygonCollider2D>(go);
                        polyCol.isTrigger = isTrigger;
                        break;

                    default:
                        // Default to box for 2D
                        var defaultBoxCol2D = Undo.AddComponent<BoxCollider2D>(go);
                        defaultBoxCol2D.isTrigger = isTrigger;
                        break;
                }
            }
            else
            {
                // 3D Colliders
                switch (shape.ToLowerInvariant())
                {
                    case "box":
                        var boxCol = Undo.AddComponent<BoxCollider>(go);
                        boxCol.isTrigger = isTrigger;
                        if (payload.TryGetValue("triggerSize", out var sizeObj) && sizeObj is Dictionary<string, object> sizeDict)
                        {
                            boxCol.size = GetVector3FromDict(sizeDict, Vector3.one);
                        }
                        break;

                    case "sphere":
                    case "circle":
                        var sphereCol = Undo.AddComponent<SphereCollider>(go);
                        sphereCol.isTrigger = isTrigger;
                        if (payload.TryGetValue("triggerSize", out var radiusObj) && radiusObj is Dictionary<string, object> radiusDict)
                        {
                            if (radiusDict.TryGetValue("x", out var xObj))
                            {
                                sphereCol.radius = Convert.ToSingle(xObj);
                            }
                        }
                        break;

                    case "capsule":
                        var capsuleCol = Undo.AddComponent<CapsuleCollider>(go);
                        capsuleCol.isTrigger = isTrigger;
                        break;

                    case "mesh":
                        var meshCol = Undo.AddComponent<MeshCollider>(go);
                        meshCol.convex = isTrigger; // Triggers require convex
                        break;

                    default:
                        // Default to box for 3D
                        var defaultBoxCol = Undo.AddComponent<BoxCollider>(go);
                        defaultBoxCol.isTrigger = isTrigger;
                        break;
                }
            }
        }

        #endregion

        #region Update Interaction

        private object UpdateInteraction(Dictionary<string, object> payload)
        {
            var component = ResolveInteractionComponent(payload);

            Undo.RecordObject(component, "Update GameKit Interaction");

            var so = new SerializedObject(component);

            if (payload.TryGetValue("triggerType", out var triggerObj))
            {
                var triggerType = ParseTriggerType(triggerObj.ToString());
                var triggerProp = so.FindProperty("triggerType");
                var names = triggerProp.enumDisplayNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], triggerType, StringComparison.OrdinalIgnoreCase))
                    {
                        triggerProp.enumValueIndex = i;
                        break;
                    }
                }
            }

            if (payload.TryGetValue("proximityRadius", out var proxObj))
                so.FindProperty("proximityRadius").floatValue = Convert.ToSingle(proxObj);

            if (payload.TryGetValue("inputKey", out var keyObj))
            {
                var inputKeyProp = so.FindProperty("inputKey");
                var keyStr = keyObj.ToString();
                var keyNames = inputKeyProp.enumDisplayNames;
                for (int i = 0; i < keyNames.Length; i++)
                {
                    if (string.Equals(keyNames[i], keyStr, StringComparison.OrdinalIgnoreCase))
                    {
                        inputKeyProp.enumValueIndex = i;
                        break;
                    }
                }
            }

            if (payload.TryGetValue("allowRepeatedTrigger", out var repeatObj))
                so.FindProperty("allowRepeatedTrigger").boolValue = Convert.ToBoolean(repeatObj);

            if (payload.TryGetValue("triggerCooldown", out var cooldownObj))
                so.FindProperty("triggerCooldown").floatValue = Convert.ToSingle(cooldownObj);

            if (payload.TryGetValue("logInteractions", out var logObj))
                so.FindProperty("logInteractions").boolValue = Convert.ToBoolean(logObj);

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var interactionId = new SerializedObject(component).FindProperty("interactionId").stringValue;

            return CreateSuccessResponse(
                ("interactionId", interactionId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect Interaction

        private object InspectInteraction(Dictionary<string, object> payload)
        {
            var interactionId = GetString(payload, "interactionId");
            if (string.IsNullOrEmpty(interactionId))
            {
                throw new InvalidOperationException("interactionId is required for inspect.");
            }

            var component = CodeGenHelper.FindComponentInSceneByField("interactionId", interactionId);
            if (component == null)
            {
                return CreateSuccessResponse(("found", false), ("interactionId", interactionId));
            }

            var so = new SerializedObject(component);

            var triggerProp = so.FindProperty("triggerType");
            var triggerType = triggerProp.enumValueIndex < triggerProp.enumDisplayNames.Length
                ? triggerProp.enumDisplayNames[triggerProp.enumValueIndex]
                : "Trigger";

            var info = new Dictionary<string, object>
            {
                { "found", true },
                { "interactionId", so.FindProperty("interactionId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "triggerType", triggerType },
                { "proximityRadius", so.FindProperty("proximityRadius").floatValue },
                { "allowRepeatedTrigger", so.FindProperty("allowRepeatedTrigger").boolValue },
                { "triggerCooldown", so.FindProperty("triggerCooldown").floatValue }
            };

            return CreateSuccessResponse(("interaction", info));
        }

        #endregion

        #region Delete Interaction

        private object DeleteInteraction(Dictionary<string, object> payload)
        {
            var interactionId = GetString(payload, "interactionId");
            if (string.IsNullOrEmpty(interactionId))
            {
                throw new InvalidOperationException("interactionId is required for delete.");
            }

            var component = CodeGenHelper.FindComponentInSceneByField("interactionId", interactionId);
            if (component == null)
            {
                throw new InvalidOperationException($"Interaction with ID '{interactionId}' not found.");
            }

            var path = BuildGameObjectPath(component.gameObject);
            var scene = component.gameObject.scene;
            Undo.DestroyObjectImmediate(component.gameObject);

            // Also clean up the generated script from tracker
            ScriptGenerator.Delete(interactionId);

            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("interactionId", interactionId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Helpers

        private Component ResolveInteractionComponent(Dictionary<string, object> payload)
        {
            // Try by interactionId first
            var interactionId = GetString(payload, "interactionId");
            if (!string.IsNullOrEmpty(interactionId))
            {
                var interactionById = CodeGenHelper.FindComponentInSceneByField("interactionId", interactionId);
                if (interactionById != null)
                {
                    return interactionById;
                }
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var interactionByPath = CodeGenHelper.FindComponentByField(targetGo, "interactionId", null);
                    if (interactionByPath != null)
                    {
                        return interactionByPath;
                    }

                    throw new InvalidOperationException($"No Interaction component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either interactionId or targetPath is required.");
        }

        private string ParseTriggerType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "collision" => "Collision",
                "trigger" => "Trigger",
                "raycast" => "Raycast",
                "proximity" => "Proximity",
                "input" => "Input",
                "tilemapcell" => "TilemapCell",
                "graphnode" => "GraphNode",
                "splineprogress" => "SplineProgress",
                _ => "Trigger"
            };
        }

        private string ParseActionType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "spawnprefab" => "SpawnPrefab",
                "destroyobject" => "DestroyObject",
                "playsound" => "PlaySound",
                "sendmessage" => "SendMessage",
                "changescene" => "ChangeScene",
                "triggeractoraction" => "TriggerActorAction",
                "updatemanagerresource" => "UpdateManagerResource",
                "triggersceneflow" => "TriggerSceneFlow",
                "teleporttotile" => "TeleportToTile",
                "movetographnode" => "MoveToGraphNode",
                "setsplineprogress" => "SetSplineProgress",
                _ => "SendMessage"
            };
        }

        private string ParseConditionType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "tag" => "Tag",
                "layer" => "Layer",
                "distance" => "Distance",
                "actorid" => "ActorId",
                "managerresource" => "ManagerResource",
                "custom" => "Custom",
                _ => "Tag"
            };
        }

        private string GetStringFromDict(Dictionary<string, object> dict, string key)
        {
            return dict.TryGetValue(key, out var value) ? value?.ToString() : null;
        }

        #endregion
    }
}
