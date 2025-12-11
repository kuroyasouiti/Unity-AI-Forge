using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using UnityAIForge.GameKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Interaction handler: create and manage interaction triggers.
    /// </summary>
    public class GameKitInteractionHandler : BaseCommandHandler
    {
        private static readonly string[] Operations = { "create", "update", "inspect", "delete" };

        public override string Category => "gamekitInteraction";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

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

            // Add GameKitInteraction component
            var interaction = Undo.AddComponent<GameKitInteraction>(interactionGo);
            var trigger = ParseTriggerType(triggerStr);
            interaction.Initialize(interactionId, trigger);

            // Add collider for trigger/collision types
            if (trigger == GameKitInteraction.TriggerType.Trigger || trigger == GameKitInteraction.TriggerType.Collision)
            {
                var shapeStr = GetString(payload, "triggerShape") ?? "box";
                var is2D = GetBoolOrDefault(payload, "is2D", false);
                AddCollider(interactionGo, shapeStr, trigger == GameKitInteraction.TriggerType.Trigger, is2D, payload);
            }

            // Add actions
            if (payload.TryGetValue("actions", out var actionsObj) && actionsObj is List<object> actionsList)
            {
                foreach (var actionObj in actionsList)
                {
                    if (actionObj is Dictionary<string, object> actionDict)
                    {
                        var actionType = ParseActionType(GetStringFromDict(actionDict, "type"));
                        var target = GetStringFromDict(actionDict, "target");
                        var parameter = GetStringFromDict(actionDict, "parameter");
                        interaction.AddAction(actionType, target, parameter);
                    }
                }
            }

            // Add conditions
            if (payload.TryGetValue("conditions", out var conditionsObj) && conditionsObj is List<object> conditionsList)
            {
                foreach (var conditionObj in conditionsList)
                {
                    if (conditionObj is Dictionary<string, object> conditionDict)
                    {
                        var conditionType = ParseConditionType(GetStringFromDict(conditionDict, "type"));
                        var value = GetStringFromDict(conditionDict, "value");
                        interaction.AddCondition(conditionType, value);
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(interactionGo.scene);
            return CreateSuccessResponse(("interactionId", interactionId), ("path", BuildGameObjectPath(interactionGo)));
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
            var interactionId = GetString(payload, "interactionId");
            if (string.IsNullOrEmpty(interactionId))
            {
                throw new InvalidOperationException("interactionId is required for update.");
            }

            var interaction = FindInteractionById(interactionId);
            if (interaction == null)
            {
                throw new InvalidOperationException($"Interaction with ID '{interactionId}' not found.");
            }

            Undo.RecordObject(interaction, "Update GameKit Interaction");

            // Add new actions
            if (payload.TryGetValue("actions", out var actionsObj) && actionsObj is List<object> actionsList)
            {
                foreach (var actionObj in actionsList)
                {
                    if (actionObj is Dictionary<string, object> actionDict)
                    {
                        var actionType = ParseActionType(GetStringFromDict(actionDict, "type"));
                        var target = GetStringFromDict(actionDict, "target");
                        var parameter = GetStringFromDict(actionDict, "parameter");
                        interaction.AddAction(actionType, target, parameter);
                    }
                }
            }

            // Add new conditions
            if (payload.TryGetValue("conditions", out var conditionsObj) && conditionsObj is List<object> conditionsList)
            {
                foreach (var conditionObj in conditionsList)
                {
                    if (conditionObj is Dictionary<string, object> conditionDict)
                    {
                        var conditionType = ParseConditionType(GetStringFromDict(conditionDict, "type"));
                        var value = GetStringFromDict(conditionDict, "value");
                        interaction.AddCondition(conditionType, value);
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(interaction.gameObject.scene);
            return CreateSuccessResponse(("interactionId", interactionId), ("path", BuildGameObjectPath(interaction.gameObject)));
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

            var interaction = FindInteractionById(interactionId);
            if (interaction == null)
            {
                return CreateSuccessResponse(("found", false), ("interactionId", interactionId));
            }

            var info = new Dictionary<string, object>
            {
                { "found", true },
                { "interactionId", interaction.InteractionId },
                { "path", BuildGameObjectPath(interaction.gameObject) },
                { "triggerType", interaction.Trigger.ToString() }
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

            var interaction = FindInteractionById(interactionId);
            if (interaction == null)
            {
                throw new InvalidOperationException($"Interaction with ID '{interactionId}' not found.");
            }

            var scene = interaction.gameObject.scene;
            Undo.DestroyObjectImmediate(interaction.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(("interactionId", interactionId), ("deleted", true));
        }

        #endregion

        #region Helpers

        private GameKitInteraction FindInteractionById(string interactionId)
        {
            var interactions = UnityEngine.Object.FindObjectsByType<GameKitInteraction>(FindObjectsSortMode.None);
            foreach (var interaction in interactions)
            {
                if (interaction.InteractionId == interactionId)
                {
                    return interaction;
                }
            }
            return null;
        }

        private GameKitInteraction.TriggerType ParseTriggerType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "collision" => GameKitInteraction.TriggerType.Collision,
                "trigger" => GameKitInteraction.TriggerType.Trigger,
                "raycast" => GameKitInteraction.TriggerType.Raycast,
                "proximity" => GameKitInteraction.TriggerType.Proximity,
                "input" => GameKitInteraction.TriggerType.Input,
                _ => GameKitInteraction.TriggerType.Trigger
            };
        }

        private GameKitInteraction.ActionType ParseActionType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "spawnprefab" => GameKitInteraction.ActionType.SpawnPrefab,
                "destroyobject" => GameKitInteraction.ActionType.DestroyObject,
                "playsound" => GameKitInteraction.ActionType.PlaySound,
                "sendmessage" => GameKitInteraction.ActionType.SendMessage,
                "changescene" => GameKitInteraction.ActionType.ChangeScene,
                _ => GameKitInteraction.ActionType.SendMessage
            };
        }

        private GameKitInteraction.ConditionType ParseConditionType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "tag" => GameKitInteraction.ConditionType.Tag,
                "layer" => GameKitInteraction.ConditionType.Layer,
                "distance" => GameKitInteraction.ConditionType.Distance,
                "custom" => GameKitInteraction.ConditionType.Custom,
                _ => GameKitInteraction.ConditionType.Tag
            };
        }

        private string GetStringFromDict(Dictionary<string, object> dict, string key)
        {
            return dict.TryGetValue(key, out var value) ? value?.ToString() : null;
        }

        private Vector3 GetVector3FromDict(Dictionary<string, object> dict, Vector3 fallback)
        {
            float x = dict.TryGetValue("x", out var xObj) ? Convert.ToSingle(xObj) : fallback.x;
            float y = dict.TryGetValue("y", out var yObj) ? Convert.ToSingle(yObj) : fallback.y;
            float z = dict.TryGetValue("z", out var zObj) ? Convert.ToSingle(zObj) : fallback.z;
            return new Vector3(x, y, z);
        }

        private Vector2 GetVector2FromDict(Dictionary<string, object> dict, Vector2 fallback)
        {
            float x = dict.TryGetValue("x", out var xObj) ? Convert.ToSingle(xObj) : fallback.x;
            float y = dict.TryGetValue("y", out var yObj) ? Convert.ToSingle(yObj) : fallback.y;
            return new Vector2(x, y);
        }

        private bool GetBoolOrDefault(Dictionary<string, object> dict, string key, bool defaultValue)
        {
            if (dict.TryGetValue(key, out var value))
            {
                if (value is bool boolVal) return boolVal;
                if (value is string strVal && bool.TryParse(strVal, out var parsed)) return parsed;
            }
            return defaultValue;
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

