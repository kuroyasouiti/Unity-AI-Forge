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
    /// GameKit Animation Sync handler: create and manage animation synchronization systems.
    /// Provides declarative animation sync without custom scripts.
    /// </summary>
    public class GameKitAnimationSyncHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "update", "inspect", "delete",
            "addSyncRule", "removeSyncRule",
            "addTriggerRule", "removeTriggerRule",
            "fireTrigger", "setParameter",
            "findBySyncId"
        };

        public override string Category => "gamekitAnimationSync";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateAnimationSync(payload),
                "update" => UpdateAnimationSync(payload),
                "inspect" => InspectAnimationSync(payload),
                "delete" => DeleteAnimationSync(payload),
                "addSyncRule" => AddSyncRule(payload),
                "removeSyncRule" => RemoveSyncRule(payload),
                "addTriggerRule" => AddTriggerRule(payload),
                "removeTriggerRule" => RemoveTriggerRule(payload),
                "fireTrigger" => FireTrigger(payload),
                "setParameter" => SetParameter(payload),
                "findBySyncId" => FindBySyncId(payload),
                _ => throw new InvalidOperationException($"Unsupported operation: {operation}")
            };
        }

        #region Create

        private object CreateAnimationSync(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            if (string.IsNullOrEmpty(targetPath))
            {
                throw new InvalidOperationException("targetPath is required for create operation.");
            }

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
            {
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            // Check if already has component
            var existing = targetGo.GetComponent<GameKitAnimationSync>();
            if (existing != null)
            {
                throw new InvalidOperationException($"GameObject '{targetPath}' already has a GameKitAnimationSync component.");
            }

            var syncId = GetString(payload, "syncId") ?? $"AnimSync_{Guid.NewGuid().ToString().Substring(0, 8)}";

            // Add component
            var animSync = Undo.AddComponent<GameKitAnimationSync>(targetGo);

            // Resolve animator if specified
            Animator animator = null;
            var animatorPath = GetString(payload, "animatorPath");
            if (!string.IsNullOrEmpty(animatorPath))
            {
                var animatorGo = ResolveGameObject(animatorPath);
                if (animatorGo != null)
                {
                    animator = animatorGo.GetComponent<Animator>();
                }
            }

            animSync.Initialize(syncId, animator);

            // Add sync rules if provided
            if (payload.TryGetValue("syncRules", out var rulesObj) && rulesObj is List<object> rulesList)
            {
                foreach (var ruleObj in rulesList)
                {
                    if (ruleObj is Dictionary<string, object> ruleDict)
                    {
                        var rule = CreateSyncRuleFromDict(ruleDict);
                        animSync.AddSyncRule(rule);
                    }
                }
            }

            // Add trigger rules if provided
            if (payload.TryGetValue("triggers", out var triggersObj) && triggersObj is List<object> triggersList)
            {
                foreach (var triggerObj in triggersList)
                {
                    if (triggerObj is Dictionary<string, object> triggerDict)
                    {
                        var trigger = CreateTriggerRuleFromDict(triggerDict);
                        animSync.AddTriggerRule(trigger);
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("syncId", syncId),
                ("path", BuildGameObjectPath(targetGo)),
                ("hasAnimator", animSync.AnimatorReference != null)
            );
        }

        #endregion

        #region Update

        private object UpdateAnimationSync(Dictionary<string, object> payload)
        {
            var animSync = ResolveAnimSyncComponent(payload);

            Undo.RecordObject(animSync, "Update GameKit AnimationSync");
            var serializedObj = new SerializedObject(animSync);

            // Update animator reference
            var animatorPath = GetString(payload, "animatorPath");
            if (!string.IsNullOrEmpty(animatorPath))
            {
                var animatorGo = ResolveGameObject(animatorPath);
                if (animatorGo != null)
                {
                    var animator = animatorGo.GetComponent<Animator>();
                    serializedObj.FindProperty("animator").objectReferenceValue = animator;
                }
            }

            if (payload.TryGetValue("autoFindAnimator", out var autoFindObj))
            {
                serializedObj.FindProperty("autoFindAnimator").boolValue = Convert.ToBoolean(autoFindObj);
            }

            serializedObj.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(animSync.gameObject.scene);

            return CreateSuccessResponse(
                ("syncId", animSync.SyncId),
                ("path", BuildGameObjectPath(animSync.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectAnimationSync(Dictionary<string, object> payload)
        {
            var animSync = ResolveAnimSyncComponent(payload);

            var syncRulesList = new List<Dictionary<string, object>>();
            foreach (var rule in animSync.SyncRules)
            {
                syncRulesList.Add(new Dictionary<string, object>
                {
                    { "parameterName", rule.parameterName },
                    { "parameterType", rule.parameterType.ToString() },
                    { "sourceType", rule.sourceType.ToString() },
                    { "sourceProperty", rule.sourceProperty },
                    { "healthId", rule.healthId },
                    { "multiplier", rule.multiplier },
                    { "boolThreshold", rule.boolThreshold }
                });
            }

            var triggerRulesList = new List<Dictionary<string, object>>();
            foreach (var trigger in animSync.TriggerRules)
            {
                triggerRulesList.Add(new Dictionary<string, object>
                {
                    { "triggerName", trigger.triggerName },
                    { "eventSource", trigger.eventSource.ToString() },
                    { "inputAction", trigger.inputAction },
                    { "healthId", trigger.healthId },
                    { "healthEvent", trigger.healthEvent.ToString() }
                });
            }

            return CreateSuccessResponse(
                ("syncId", animSync.SyncId),
                ("path", BuildGameObjectPath(animSync.gameObject)),
                ("hasAnimator", animSync.AnimatorReference != null),
                ("animatorPath", animSync.AnimatorReference != null ? BuildGameObjectPath(animSync.AnimatorReference.gameObject) : null),
                ("syncRulesCount", animSync.SyncRules.Count),
                ("syncRules", syncRulesList),
                ("triggerRulesCount", animSync.TriggerRules.Count),
                ("triggerRules", triggerRulesList)
            );
        }

        #endregion

        #region Delete

        private object DeleteAnimationSync(Dictionary<string, object> payload)
        {
            var animSync = ResolveAnimSyncComponent(payload);
            var path = BuildGameObjectPath(animSync.gameObject);
            var syncId = animSync.SyncId;
            var scene = animSync.gameObject.scene;

            Undo.DestroyObjectImmediate(animSync);
            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("syncId", syncId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Sync Rules

        private object AddSyncRule(Dictionary<string, object> payload)
        {
            var animSync = ResolveAnimSyncComponent(payload);

            if (!payload.TryGetValue("rule", out var ruleObj) || !(ruleObj is Dictionary<string, object> ruleDict))
            {
                throw new InvalidOperationException("'rule' parameter is required for addSyncRule.");
            }

            var rule = CreateSyncRuleFromDict(ruleDict);

            Undo.RecordObject(animSync, "Add Sync Rule");
            animSync.AddSyncRule(rule);
            EditorUtility.SetDirty(animSync);
            EditorSceneManager.MarkSceneDirty(animSync.gameObject.scene);

            return CreateSuccessResponse(
                ("syncId", animSync.SyncId),
                ("parameterName", rule.parameterName),
                ("added", true)
            );
        }

        private object RemoveSyncRule(Dictionary<string, object> payload)
        {
            var animSync = ResolveAnimSyncComponent(payload);
            var parameterName = GetString(payload, "parameterName");

            if (string.IsNullOrEmpty(parameterName))
            {
                throw new InvalidOperationException("'parameterName' is required for removeSyncRule.");
            }

            Undo.RecordObject(animSync, "Remove Sync Rule");
            var removed = animSync.RemoveSyncRule(parameterName);
            EditorUtility.SetDirty(animSync);
            EditorSceneManager.MarkSceneDirty(animSync.gameObject.scene);

            return CreateSuccessResponse(
                ("syncId", animSync.SyncId),
                ("parameterName", parameterName),
                ("removed", removed)
            );
        }

        #endregion

        #region Trigger Rules

        private object AddTriggerRule(Dictionary<string, object> payload)
        {
            var animSync = ResolveAnimSyncComponent(payload);

            if (!payload.TryGetValue("trigger", out var triggerObj) || !(triggerObj is Dictionary<string, object> triggerDict))
            {
                throw new InvalidOperationException("'trigger' parameter is required for addTriggerRule.");
            }

            var trigger = CreateTriggerRuleFromDict(triggerDict);

            Undo.RecordObject(animSync, "Add Trigger Rule");
            animSync.AddTriggerRule(trigger);
            EditorUtility.SetDirty(animSync);
            EditorSceneManager.MarkSceneDirty(animSync.gameObject.scene);

            return CreateSuccessResponse(
                ("syncId", animSync.SyncId),
                ("triggerName", trigger.triggerName),
                ("added", true)
            );
        }

        private object RemoveTriggerRule(Dictionary<string, object> payload)
        {
            var animSync = ResolveAnimSyncComponent(payload);
            var triggerName = GetString(payload, "triggerName");

            if (string.IsNullOrEmpty(triggerName))
            {
                throw new InvalidOperationException("'triggerName' is required for removeTriggerRule.");
            }

            Undo.RecordObject(animSync, "Remove Trigger Rule");
            var removed = animSync.RemoveTriggerRule(triggerName);
            EditorUtility.SetDirty(animSync);
            EditorSceneManager.MarkSceneDirty(animSync.gameObject.scene);

            return CreateSuccessResponse(
                ("syncId", animSync.SyncId),
                ("triggerName", triggerName),
                ("removed", removed)
            );
        }

        #endregion

        #region Runtime Operations

        private object FireTrigger(Dictionary<string, object> payload)
        {
            var animSync = ResolveAnimSyncComponent(payload);
            var triggerName = GetString(payload, "triggerName");

            if (string.IsNullOrEmpty(triggerName))
            {
                throw new InvalidOperationException("'triggerName' is required for fireTrigger.");
            }

            // Note: This only works in play mode
            if (!Application.isPlaying)
            {
                return CreateSuccessResponse(
                    ("syncId", animSync.SyncId),
                    ("triggerName", triggerName),
                    ("note", "Trigger will be fired in play mode only.")
                );
            }

            animSync.FireTrigger(triggerName);

            return CreateSuccessResponse(
                ("syncId", animSync.SyncId),
                ("triggerName", triggerName),
                ("fired", true)
            );
        }

        private object SetParameter(Dictionary<string, object> payload)
        {
            var animSync = ResolveAnimSyncComponent(payload);
            var parameterName = GetString(payload, "parameterName");
            var parameterType = GetString(payload, "parameterType") ?? "float";

            if (string.IsNullOrEmpty(parameterName))
            {
                throw new InvalidOperationException("'parameterName' is required for setParameter.");
            }

            // Note: This only works in play mode
            if (!Application.isPlaying)
            {
                return CreateSuccessResponse(
                    ("syncId", animSync.SyncId),
                    ("parameterName", parameterName),
                    ("note", "Parameter will be set in play mode only.")
                );
            }

            switch (parameterType.ToLowerInvariant())
            {
                case "float":
                    var floatValue = GetFloat(payload, "value", 0f);
                    animSync.SetParameter(parameterName, floatValue);
                    break;

                case "bool":
                    var boolValue = GetBool(payload, "value", false);
                    animSync.SetParameterBool(parameterName, boolValue);
                    break;

                case "int":
                    var intValue = GetInt(payload, "value", 0);
                    animSync.SetParameterInt(parameterName, intValue);
                    break;
            }

            return CreateSuccessResponse(
                ("syncId", animSync.SyncId),
                ("parameterName", parameterName),
                ("set", true)
            );
        }

        #endregion

        #region Find

        private object FindBySyncId(Dictionary<string, object> payload)
        {
            var syncId = GetString(payload, "syncId");
            if (string.IsNullOrEmpty(syncId))
            {
                throw new InvalidOperationException("'syncId' is required for findBySyncId.");
            }

            var animSync = FindAnimSyncById(syncId);
            if (animSync == null)
            {
                return CreateSuccessResponse(("found", false), ("syncId", syncId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("syncId", animSync.SyncId),
                ("path", BuildGameObjectPath(animSync.gameObject)),
                ("hasAnimator", animSync.AnimatorReference != null),
                ("syncRulesCount", animSync.SyncRules.Count),
                ("triggerRulesCount", animSync.TriggerRules.Count)
            );
        }

        #endregion

        #region Helpers

        private GameKitAnimationSync ResolveAnimSyncComponent(Dictionary<string, object> payload)
        {
            // Try by syncId first
            var syncId = GetString(payload, "syncId");
            if (!string.IsNullOrEmpty(syncId))
            {
                var syncById = FindAnimSyncById(syncId);
                if (syncById != null)
                {
                    return syncById;
                }
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var syncByPath = targetGo.GetComponent<GameKitAnimationSync>();
                    if (syncByPath != null)
                    {
                        return syncByPath;
                    }
                    throw new InvalidOperationException($"No GameKitAnimationSync component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either 'syncId' or 'targetPath' is required.");
        }

        private GameKitAnimationSync FindAnimSyncById(string syncId)
        {
            var syncs = UnityEngine.Object.FindObjectsByType<GameKitAnimationSync>(FindObjectsSortMode.None);
            foreach (var sync in syncs)
            {
                if (sync.SyncId == syncId)
                {
                    return sync;
                }
            }
            return null;
        }

        private GameKitAnimationSync.AnimSyncRule CreateSyncRuleFromDict(Dictionary<string, object> dict)
        {
            var rule = new GameKitAnimationSync.AnimSyncRule
            {
                parameterName = dict.TryGetValue("parameter", out var param) ? param?.ToString() : "",
                sourceProperty = dict.TryGetValue("sourceProperty", out var prop) ? prop?.ToString() : ""
            };

            if (dict.TryGetValue("parameterType", out var paramType))
            {
                rule.parameterType = ParseParameterType(paramType?.ToString());
            }

            if (dict.TryGetValue("sourceType", out var srcType))
            {
                rule.sourceType = ParseSourceType(srcType?.ToString());
            }

            if (dict.TryGetValue("healthId", out var healthId))
            {
                rule.healthId = healthId?.ToString();
            }

            if (dict.TryGetValue("multiplier", out var mult))
            {
                rule.multiplier = Convert.ToSingle(mult);
            }

            if (dict.TryGetValue("boolThreshold", out var threshold))
            {
                rule.boolThreshold = Convert.ToSingle(threshold);
            }

            return rule;
        }

        private GameKitAnimationSync.AnimTriggerRule CreateTriggerRuleFromDict(Dictionary<string, object> dict)
        {
            var rule = new GameKitAnimationSync.AnimTriggerRule
            {
                triggerName = dict.TryGetValue("triggerName", out var name) ? name?.ToString() : ""
            };

            if (dict.TryGetValue("eventSource", out var srcObj))
            {
                rule.eventSource = ParseTriggerEventSource(srcObj?.ToString());
            }

            if (dict.TryGetValue("inputAction", out var action))
            {
                rule.inputAction = action?.ToString();
            }

            if (dict.TryGetValue("healthId", out var healthId))
            {
                rule.healthId = healthId?.ToString();
            }

            if (dict.TryGetValue("healthEvent", out var eventObj) || dict.TryGetValue("event", out eventObj))
            {
                rule.healthEvent = ParseHealthEventType(eventObj?.ToString());
            }

            return rule;
        }

        private GameKitAnimationSync.AnimParameterType ParseParameterType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "float" => GameKitAnimationSync.AnimParameterType.Float,
                "int" or "integer" => GameKitAnimationSync.AnimParameterType.Int,
                "bool" or "boolean" => GameKitAnimationSync.AnimParameterType.Bool,
                _ => GameKitAnimationSync.AnimParameterType.Float
            };
        }

        private GameKitAnimationSync.SyncSourceType ParseSourceType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "rigidbody3d" or "rigidbody" => GameKitAnimationSync.SyncSourceType.Rigidbody3D,
                "rigidbody2d" => GameKitAnimationSync.SyncSourceType.Rigidbody2D,
                "transform" => GameKitAnimationSync.SyncSourceType.Transform,
                "health" => GameKitAnimationSync.SyncSourceType.Health,
                "custom" => GameKitAnimationSync.SyncSourceType.Custom,
                _ => GameKitAnimationSync.SyncSourceType.Rigidbody3D
            };
        }

        private GameKitAnimationSync.TriggerEventSource ParseTriggerEventSource(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "health" => GameKitAnimationSync.TriggerEventSource.Health,
                "input" => GameKitAnimationSync.TriggerEventSource.Input,
                "manual" => GameKitAnimationSync.TriggerEventSource.Manual,
                _ => GameKitAnimationSync.TriggerEventSource.Manual
            };
        }

        private GameKitAnimationSync.HealthEventType ParseHealthEventType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "ondamaged" or "damaged" => GameKitAnimationSync.HealthEventType.OnDamaged,
                "onhealed" or "healed" => GameKitAnimationSync.HealthEventType.OnHealed,
                "ondeath" or "death" => GameKitAnimationSync.HealthEventType.OnDeath,
                "onrespawn" or "respawn" => GameKitAnimationSync.HealthEventType.OnRespawn,
                "oninvincibilitystart" or "invincibilitystart" => GameKitAnimationSync.HealthEventType.OnInvincibilityStart,
                "oninvincibilityend" or "invincibilityend" => GameKitAnimationSync.HealthEventType.OnInvincibilityEnd,
                _ => GameKitAnimationSync.HealthEventType.OnDamaged
            };
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
