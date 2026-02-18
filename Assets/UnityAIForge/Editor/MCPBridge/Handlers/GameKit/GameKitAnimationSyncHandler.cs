using System;
using System.Collections.Generic;
using System.Reflection;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Animation Sync handler: create and manage animation synchronization systems.
    /// Uses code generation to produce standalone AnimationSync scripts with zero package dependency.
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

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

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

            // Check if already has an AnimationSync component (by checking for syncId field)
            var existing = CodeGenHelper.FindComponentByField(targetGo, "syncId", null);
            if (existing != null)
            {
                throw new InvalidOperationException(
                    $"GameObject '{targetPath}' already has an AnimationSync component.");
            }

            var syncId = GetString(payload, "syncId") ?? $"AnimSync_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var autoFindAnimator = GetBool(payload, "autoFindAnimator", true);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(syncId, "AnimationSync");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "SYNC_ID", syncId },
                { "AUTO_FIND_ANIMATOR", autoFindAnimator }
            };

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "AnimationSync", syncId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate AnimationSync script.");
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["syncId"] = syncId;
            result["path"] = BuildGameObjectPath(targetGo);

            return result;
        }

        #endregion

        #region Update

        private object UpdateAnimationSync(Dictionary<string, object> payload)
        {
            var component = ResolveAnimSyncComponent(payload);

            Undo.RecordObject(component, "Update AnimationSync");
            var so = new SerializedObject(component);

            // Update animator reference
            var animatorPath = GetString(payload, "animatorPath");
            if (!string.IsNullOrEmpty(animatorPath))
            {
                var animatorGo = ResolveGameObject(animatorPath);
                if (animatorGo != null)
                {
                    var animator = animatorGo.GetComponent<Animator>();
                    var animatorProp = so.FindProperty("animator");
                    if (animatorProp != null)
                    {
                        animatorProp.objectReferenceValue = animator;
                    }
                }
            }

            if (payload.TryGetValue("autoFindAnimator", out var autoFindObj))
            {
                var prop = so.FindProperty("autoFindAnimator");
                if (prop != null)
                {
                    prop.boolValue = Convert.ToBoolean(autoFindObj);
                }
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var syncId = so.FindProperty("syncId").stringValue;

            return CreateSuccessResponse(
                ("syncId", syncId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectAnimationSync(Dictionary<string, object> payload)
        {
            var component = ResolveAnimSyncComponent(payload);
            var so = new SerializedObject(component);

            // Read sync rules array
            var syncRulesList = new List<Dictionary<string, object>>();
            var syncRulesProp = so.FindProperty("syncRules");
            if (syncRulesProp != null)
            {
                for (int i = 0; i < syncRulesProp.arraySize; i++)
                {
                    var element = syncRulesProp.GetArrayElementAtIndex(i);
                    var paramTypeProp = element.FindPropertyRelative("parameterType");
                    var srcTypeProp = element.FindPropertyRelative("sourceType");

                    syncRulesList.Add(new Dictionary<string, object>
                    {
                        { "parameterName", element.FindPropertyRelative("parameterName").stringValue },
                        { "parameterType", paramTypeProp.enumValueIndex < paramTypeProp.enumDisplayNames.Length
                            ? paramTypeProp.enumDisplayNames[paramTypeProp.enumValueIndex] : "Float" },
                        { "sourceType", srcTypeProp.enumValueIndex < srcTypeProp.enumDisplayNames.Length
                            ? srcTypeProp.enumDisplayNames[srcTypeProp.enumValueIndex] : "Rigidbody3D" },
                        { "sourceProperty", element.FindPropertyRelative("sourceProperty").stringValue },
                        { "healthId", element.FindPropertyRelative("healthId").stringValue },
                        { "multiplier", element.FindPropertyRelative("multiplier").floatValue },
                        { "boolThreshold", element.FindPropertyRelative("boolThreshold").floatValue }
                    });
                }
            }

            // Read trigger rules array
            var triggerRulesList = new List<Dictionary<string, object>>();
            var triggerRulesProp = so.FindProperty("triggerRules");
            if (triggerRulesProp != null)
            {
                for (int i = 0; i < triggerRulesProp.arraySize; i++)
                {
                    var element = triggerRulesProp.GetArrayElementAtIndex(i);
                    var eventSrcProp = element.FindPropertyRelative("eventSource");
                    var healthEvtProp = element.FindPropertyRelative("healthEvent");

                    triggerRulesList.Add(new Dictionary<string, object>
                    {
                        { "triggerName", element.FindPropertyRelative("triggerName").stringValue },
                        { "eventSource", eventSrcProp.enumValueIndex < eventSrcProp.enumDisplayNames.Length
                            ? eventSrcProp.enumDisplayNames[eventSrcProp.enumValueIndex] : "Manual" },
                        { "inputAction", element.FindPropertyRelative("inputAction").stringValue },
                        { "healthId", element.FindPropertyRelative("healthId").stringValue },
                        { "healthEvent", healthEvtProp.enumValueIndex < healthEvtProp.enumDisplayNames.Length
                            ? healthEvtProp.enumDisplayNames[healthEvtProp.enumValueIndex] : "OnDamaged" }
                    });
                }
            }

            // Check animator reference
            var animatorProp = so.FindProperty("animator");
            var hasAnimator = animatorProp != null && animatorProp.objectReferenceValue != null;
            string animatorPath = null;
            if (hasAnimator)
            {
                var animatorComp = animatorProp.objectReferenceValue as Animator;
                if (animatorComp != null)
                {
                    animatorPath = BuildGameObjectPath(animatorComp.gameObject);
                }
            }

            return CreateSuccessResponse(
                ("syncId", so.FindProperty("syncId").stringValue),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("hasAnimator", hasAnimator),
                ("animatorPath", animatorPath),
                ("syncRulesCount", syncRulesProp != null ? syncRulesProp.arraySize : 0),
                ("syncRules", syncRulesList),
                ("triggerRulesCount", triggerRulesProp != null ? triggerRulesProp.arraySize : 0),
                ("triggerRules", triggerRulesList)
            );
        }

        #endregion

        #region Delete

        private object DeleteAnimationSync(Dictionary<string, object> payload)
        {
            var component = ResolveAnimSyncComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var syncId = new SerializedObject(component).FindProperty("syncId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component);

            // Clean up the generated script from tracker
            ScriptGenerator.Delete(syncId);

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
            var component = ResolveAnimSyncComponent(payload);

            if (!payload.TryGetValue("rule", out var ruleObj) || !(ruleObj is Dictionary<string, object> ruleDict))
            {
                throw new InvalidOperationException("'rule' parameter is required for addSyncRule.");
            }

            var so = new SerializedObject(component);
            var syncRulesProp = so.FindProperty("syncRules");

            if (syncRulesProp == null)
            {
                throw new InvalidOperationException("syncRules property not found on component.");
            }

            // Add a new element to the array
            syncRulesProp.InsertArrayElementAtIndex(syncRulesProp.arraySize);
            var newElement = syncRulesProp.GetArrayElementAtIndex(syncRulesProp.arraySize - 1);

            // Set fields on the new element
            var parameterName = ruleDict.TryGetValue("parameter", out var param) ? param?.ToString() : "";
            newElement.FindPropertyRelative("parameterName").stringValue = parameterName;
            newElement.FindPropertyRelative("sourceProperty").stringValue =
                ruleDict.TryGetValue("sourceProperty", out var prop) ? prop?.ToString() : "";

            if (ruleDict.TryGetValue("parameterType", out var paramType))
            {
                SetEnumByName(newElement.FindPropertyRelative("parameterType"),
                    ParseParameterType(paramType?.ToString()));
            }

            if (ruleDict.TryGetValue("sourceType", out var srcType))
            {
                SetEnumByName(newElement.FindPropertyRelative("sourceType"),
                    ParseSourceType(srcType?.ToString()));
            }

            if (ruleDict.TryGetValue("healthId", out var healthId))
            {
                newElement.FindPropertyRelative("healthId").stringValue = healthId?.ToString() ?? "";
            }

            if (ruleDict.TryGetValue("multiplier", out var mult))
            {
                newElement.FindPropertyRelative("multiplier").floatValue = Convert.ToSingle(mult);
            }
            else
            {
                newElement.FindPropertyRelative("multiplier").floatValue = 1f;
            }

            if (ruleDict.TryGetValue("boolThreshold", out var threshold))
            {
                newElement.FindPropertyRelative("boolThreshold").floatValue = Convert.ToSingle(threshold);
            }
            else
            {
                newElement.FindPropertyRelative("boolThreshold").floatValue = 0.01f;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(component);
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("syncId", new SerializedObject(component).FindProperty("syncId").stringValue),
                ("parameterName", parameterName),
                ("added", true)
            );
        }

        private object RemoveSyncRule(Dictionary<string, object> payload)
        {
            var component = ResolveAnimSyncComponent(payload);
            var parameterName = GetString(payload, "parameterName");

            if (string.IsNullOrEmpty(parameterName))
            {
                throw new InvalidOperationException("'parameterName' is required for removeSyncRule.");
            }

            var so = new SerializedObject(component);
            var syncRulesProp = so.FindProperty("syncRules");
            bool removed = false;

            if (syncRulesProp != null)
            {
                for (int i = syncRulesProp.arraySize - 1; i >= 0; i--)
                {
                    var element = syncRulesProp.GetArrayElementAtIndex(i);
                    if (element.FindPropertyRelative("parameterName").stringValue == parameterName)
                    {
                        syncRulesProp.DeleteArrayElementAtIndex(i);
                        removed = true;
                    }
                }
                so.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(component);
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("syncId", new SerializedObject(component).FindProperty("syncId").stringValue),
                ("parameterName", parameterName),
                ("removed", removed)
            );
        }

        #endregion

        #region Trigger Rules

        private object AddTriggerRule(Dictionary<string, object> payload)
        {
            var component = ResolveAnimSyncComponent(payload);

            if (!payload.TryGetValue("trigger", out var triggerObj) || !(triggerObj is Dictionary<string, object> triggerDict))
            {
                throw new InvalidOperationException("'trigger' parameter is required for addTriggerRule.");
            }

            var so = new SerializedObject(component);
            var triggerRulesProp = so.FindProperty("triggerRules");

            if (triggerRulesProp == null)
            {
                throw new InvalidOperationException("triggerRules property not found on component.");
            }

            // Add a new element to the array
            triggerRulesProp.InsertArrayElementAtIndex(triggerRulesProp.arraySize);
            var newElement = triggerRulesProp.GetArrayElementAtIndex(triggerRulesProp.arraySize - 1);

            // Set fields on the new element
            var triggerName = triggerDict.TryGetValue("triggerName", out var name) ? name?.ToString() : "";
            newElement.FindPropertyRelative("triggerName").stringValue = triggerName;

            if (triggerDict.TryGetValue("eventSource", out var srcObj))
            {
                SetEnumByName(newElement.FindPropertyRelative("eventSource"),
                    ParseTriggerEventSource(srcObj?.ToString()));
            }

            if (triggerDict.TryGetValue("inputAction", out var action))
            {
                newElement.FindPropertyRelative("inputAction").stringValue = action?.ToString() ?? "";
            }

            if (triggerDict.TryGetValue("healthId", out var healthId))
            {
                newElement.FindPropertyRelative("healthId").stringValue = healthId?.ToString() ?? "";
            }

            if (triggerDict.TryGetValue("healthEvent", out var eventObj) || triggerDict.TryGetValue("event", out eventObj))
            {
                SetEnumByName(newElement.FindPropertyRelative("healthEvent"),
                    ParseHealthEventType(eventObj?.ToString()));
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(component);
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("syncId", new SerializedObject(component).FindProperty("syncId").stringValue),
                ("triggerName", triggerName),
                ("added", true)
            );
        }

        private object RemoveTriggerRule(Dictionary<string, object> payload)
        {
            var component = ResolveAnimSyncComponent(payload);
            var triggerName = GetString(payload, "triggerName");

            if (string.IsNullOrEmpty(triggerName))
            {
                throw new InvalidOperationException("'triggerName' is required for removeTriggerRule.");
            }

            var so = new SerializedObject(component);
            var triggerRulesProp = so.FindProperty("triggerRules");
            bool removed = false;

            if (triggerRulesProp != null)
            {
                for (int i = triggerRulesProp.arraySize - 1; i >= 0; i--)
                {
                    var element = triggerRulesProp.GetArrayElementAtIndex(i);
                    if (element.FindPropertyRelative("triggerName").stringValue == triggerName)
                    {
                        triggerRulesProp.DeleteArrayElementAtIndex(i);
                        removed = true;
                    }
                }
                so.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(component);
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("syncId", new SerializedObject(component).FindProperty("syncId").stringValue),
                ("triggerName", triggerName),
                ("removed", removed)
            );
        }

        #endregion

        #region Runtime Operations

        private object FireTrigger(Dictionary<string, object> payload)
        {
            var component = ResolveAnimSyncComponent(payload);
            var triggerName = GetString(payload, "triggerName");

            if (string.IsNullOrEmpty(triggerName))
            {
                throw new InvalidOperationException("'triggerName' is required for fireTrigger.");
            }

            var syncId = new SerializedObject(component).FindProperty("syncId").stringValue;

            // Note: This only works in play mode
            if (!Application.isPlaying)
            {
                return CreateSuccessResponse(
                    ("syncId", syncId),
                    ("triggerName", triggerName),
                    ("note", "Trigger will be fired in play mode only.")
                );
            }

            // Use reflection to call FireTrigger on the generated component
            var method = component.GetType().GetMethod("FireTrigger",
                BindingFlags.Public | BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(component, new object[] { triggerName });
            }

            return CreateSuccessResponse(
                ("syncId", syncId),
                ("triggerName", triggerName),
                ("fired", true)
            );
        }

        private object SetParameter(Dictionary<string, object> payload)
        {
            var component = ResolveAnimSyncComponent(payload);
            var parameterName = GetString(payload, "parameterName");
            var parameterType = GetString(payload, "parameterType") ?? "float";

            if (string.IsNullOrEmpty(parameterName))
            {
                throw new InvalidOperationException("'parameterName' is required for setParameter.");
            }

            var syncId = new SerializedObject(component).FindProperty("syncId").stringValue;

            // Note: This only works in play mode
            if (!Application.isPlaying)
            {
                return CreateSuccessResponse(
                    ("syncId", syncId),
                    ("parameterName", parameterName),
                    ("note", "Parameter will be set in play mode only.")
                );
            }

            var compType = component.GetType();

            switch (parameterType.ToLowerInvariant())
            {
                case "float":
                    var floatValue = GetFloat(payload, "value", 0f);
                    var setParamMethod = compType.GetMethod("SetParameter",
                        BindingFlags.Public | BindingFlags.Instance,
                        null, new[] { typeof(string), typeof(float) }, null);
                    setParamMethod?.Invoke(component, new object[] { parameterName, floatValue });
                    break;

                case "bool":
                    var boolValue = GetBool(payload, "value", false);
                    var setBoolMethod = compType.GetMethod("SetParameterBool",
                        BindingFlags.Public | BindingFlags.Instance);
                    setBoolMethod?.Invoke(component, new object[] { parameterName, boolValue });
                    break;

                case "int":
                    var intValue = GetInt(payload, "value", 0);
                    var setIntMethod = compType.GetMethod("SetParameterInt",
                        BindingFlags.Public | BindingFlags.Instance);
                    setIntMethod?.Invoke(component, new object[] { parameterName, intValue });
                    break;
            }

            return CreateSuccessResponse(
                ("syncId", syncId),
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

            var component = CodeGenHelper.FindComponentInSceneByField("syncId", syncId);
            if (component == null)
            {
                return CreateSuccessResponse(("found", false), ("syncId", syncId));
            }

            var so = new SerializedObject(component);
            var animatorProp = so.FindProperty("animator");
            var syncRulesProp = so.FindProperty("syncRules");
            var triggerRulesProp = so.FindProperty("triggerRules");

            return CreateSuccessResponse(
                ("found", true),
                ("syncId", so.FindProperty("syncId").stringValue),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("hasAnimator", animatorProp != null && animatorProp.objectReferenceValue != null),
                ("syncRulesCount", syncRulesProp != null ? syncRulesProp.arraySize : 0),
                ("triggerRulesCount", triggerRulesProp != null ? triggerRulesProp.arraySize : 0)
            );
        }

        #endregion

        #region Helpers

        private Component ResolveAnimSyncComponent(Dictionary<string, object> payload)
        {
            // Try by syncId first
            var syncId = GetString(payload, "syncId");
            if (!string.IsNullOrEmpty(syncId))
            {
                var syncById = CodeGenHelper.FindComponentInSceneByField("syncId", syncId);
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
                    var syncByPath = CodeGenHelper.FindComponentByField(targetGo, "syncId", null);
                    if (syncByPath != null)
                    {
                        return syncByPath;
                    }
                    throw new InvalidOperationException($"No AnimationSync component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either 'syncId' or 'targetPath' is required.");
        }

        private void SetEnumByName(SerializedProperty prop, string enumName)
        {
            if (prop == null || string.IsNullOrEmpty(enumName)) return;

            var names = prop.enumDisplayNames;
            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(names[i], enumName, StringComparison.OrdinalIgnoreCase))
                {
                    prop.enumValueIndex = i;
                    return;
                }
            }
        }

        private string ParseParameterType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "float" => "Float",
                "int" or "integer" => "Int",
                "bool" or "boolean" => "Bool",
                _ => "Float"
            };
        }

        private string ParseSourceType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "rigidbody3d" or "rigidbody" => "Rigidbody3D",
                "rigidbody2d" => "Rigidbody2D",
                "transform" => "Transform",
                "health" => "Health",
                "custom" => "Custom",
                _ => "Rigidbody3D"
            };
        }

        private string ParseTriggerEventSource(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "health" => "Health",
                "input" => "Input",
                "manual" => "Manual",
                _ => "Manual"
            };
        }

        private string ParseHealthEventType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "ondamaged" or "damaged" => "OnDamaged",
                "onhealed" or "healed" => "OnHealed",
                "ondeath" or "death" => "OnDeath",
                "onrespawn" or "respawn" => "OnRespawn",
                "oninvincibilitystart" or "invincibilitystart" => "OnInvincibilityStart",
                "oninvincibilityend" or "invincibilityend" => "OnInvincibilityEnd",
                _ => "OnDamaged"
            };
        }

        #endregion
    }
}
