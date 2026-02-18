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
    /// GameKit Timer handler: create and manage timers and cooldowns.
    /// Uses code generation to produce standalone Timer/Cooldown scripts with zero package dependency.
    /// </summary>
    public class GameKitTimerHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "createTimer", "updateTimer", "inspectTimer", "deleteTimer",
            "createCooldown", "updateCooldown", "inspectCooldown", "deleteCooldown",
            "createCooldownManager", "addCooldownToManager", "inspectCooldownManager",
            "findByTimerId", "findByCooldownId"
        };

        public override string Category => "gamekitTimer";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "createTimer" || operation == "createCooldown" || operation == "createCooldownManager";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "createTimer" => CreateTimer(payload),
                "updateTimer" => UpdateTimer(payload),
                "inspectTimer" => InspectTimer(payload),
                "deleteTimer" => DeleteTimer(payload),
                "createCooldown" => CreateCooldown(payload),
                "updateCooldown" => UpdateCooldown(payload),
                "inspectCooldown" => InspectCooldown(payload),
                "deleteCooldown" => DeleteCooldown(payload),
                "createCooldownManager" => CreateCooldownManager(payload),
                "addCooldownToManager" => AddCooldownToManager(payload),
                "inspectCooldownManager" => InspectCooldownManager(payload),
                "findByTimerId" => FindByTimerId(payload),
                "findByCooldownId" => FindByCooldownId(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit Timer operation: {operation}")
            };
        }

        #region Timer Operations

        private object CreateTimer(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            if (string.IsNullOrEmpty(targetPath))
                throw new InvalidOperationException("targetPath is required for createTimer.");

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");

            var timerId = GetString(payload, "timerId") ?? $"Timer_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var duration = GetFloat(payload, "duration", 5f);
            var loop = GetBool(payload, "loop", false);
            var unscaledTime = GetBool(payload, "unscaledTime", false);
            var autoStart = GetBool(payload, "autoStart", false);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(timerId, "Timer");

            var variables = new Dictionary<string, object>
            {
                { "TIMER_ID", timerId },
                { "DURATION", duration },
                { "LOOP", loop.ToString().ToLowerInvariant() },
                { "AUTO_START", autoStart.ToString().ToLowerInvariant() },
                { "UNSCALED_TIME", unscaledTime.ToString().ToLowerInvariant() }
            };

            var outputDir = GetString(payload, "outputPath");

            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "Timer", timerId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate Timer script.");
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["timerId"] = timerId;
            result["path"] = BuildGameObjectPath(targetGo);
            result["duration"] = duration;
            result["loop"] = loop;

            return result;
        }

        private object UpdateTimer(Dictionary<string, object> payload)
        {
            var component = ResolveTimerComponent(payload);

            Undo.RecordObject(component, "Update Timer");
            var so = new SerializedObject(component);

            if (payload.TryGetValue("duration", out var durObj))
                so.FindProperty("duration").floatValue = Convert.ToSingle(durObj);
            if (payload.TryGetValue("loop", out var loopObj))
                so.FindProperty("loop").boolValue = Convert.ToBoolean(loopObj);
            if (payload.TryGetValue("autoStart", out var autoObj))
                so.FindProperty("autoStart").boolValue = Convert.ToBoolean(autoObj);
            if (payload.TryGetValue("unscaledTime", out var unscaledObj))
                so.FindProperty("unscaledTime").boolValue = Convert.ToBoolean(unscaledObj);

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("timerId", so.FindProperty("timerId").stringValue),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        private object InspectTimer(Dictionary<string, object> payload)
        {
            var component = ResolveTimerComponent(payload);
            var so = new SerializedObject(component);

            var info = new Dictionary<string, object>
            {
                { "timerId", so.FindProperty("timerId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "duration", so.FindProperty("duration").floatValue },
                { "loop", so.FindProperty("loop").boolValue },
                { "autoStart", so.FindProperty("autoStart").boolValue },
                { "unscaledTime", so.FindProperty("unscaledTime").boolValue }
            };

            return CreateSuccessResponse(("timer", info));
        }

        private object DeleteTimer(Dictionary<string, object> payload)
        {
            var component = ResolveTimerComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var timerId = new SerializedObject(component).FindProperty("timerId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component);
            ScriptGenerator.Delete(timerId);
            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("timerId", timerId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Cooldown Operations

        private object CreateCooldown(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            if (string.IsNullOrEmpty(targetPath))
                throw new InvalidOperationException("targetPath is required for createCooldown.");

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");

            var cooldownId = GetString(payload, "cooldownId") ?? $"Cooldown_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var duration = GetFloat(payload, "cooldownDuration", 1f);
            var startReady = GetBool(payload, "startReady", true);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(cooldownId, "Cooldown");

            var variables = new Dictionary<string, object>
            {
                { "COOLDOWN_ID", cooldownId },
                { "COOLDOWN_DURATION", duration },
                { "START_READY", startReady.ToString().ToLowerInvariant() }
            };

            var outputDir = GetString(payload, "outputPath");

            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "Cooldown", cooldownId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate Cooldown script.");
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["cooldownId"] = cooldownId;
            result["path"] = BuildGameObjectPath(targetGo);
            result["cooldownDuration"] = duration;
            result["startReady"] = startReady;

            return result;
        }

        private object UpdateCooldown(Dictionary<string, object> payload)
        {
            var component = ResolveCooldownComponent(payload);

            Undo.RecordObject(component, "Update Cooldown");
            var so = new SerializedObject(component);

            if (payload.TryGetValue("cooldownDuration", out var durObj))
                so.FindProperty("cooldownDuration").floatValue = Convert.ToSingle(durObj);
            if (payload.TryGetValue("startReady", out var readyObj))
                so.FindProperty("startReady").boolValue = Convert.ToBoolean(readyObj);

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("cooldownId", so.FindProperty("cooldownId").stringValue),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        private object InspectCooldown(Dictionary<string, object> payload)
        {
            var component = ResolveCooldownComponent(payload);
            var so = new SerializedObject(component);

            var info = new Dictionary<string, object>
            {
                { "cooldownId", so.FindProperty("cooldownId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "cooldownDuration", so.FindProperty("cooldownDuration").floatValue },
                { "startReady", so.FindProperty("startReady").boolValue }
            };

            return CreateSuccessResponse(("cooldown", info));
        }

        private object DeleteCooldown(Dictionary<string, object> payload)
        {
            var component = ResolveCooldownComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var cooldownId = new SerializedObject(component).FindProperty("cooldownId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component);
            ScriptGenerator.Delete(cooldownId);
            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("cooldownId", cooldownId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Cooldown Manager Operations

        private object CreateCooldownManager(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            if (string.IsNullOrEmpty(targetPath))
                throw new InvalidOperationException("targetPath is required for createCooldownManager.");

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");

            var existingManager = CodeGenHelper.FindComponentByField(targetGo, "cooldowns", null);
            // Fallback: check for any CooldownManager-like component
            // Use a unique field name for CooldownManager templates

            var managerId = $"CDManager_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(managerId, "CooldownManager");

            var variables = new Dictionary<string, object>
            {
                { "MANAGER_ID", managerId }
            };

            var outputDir = GetString(payload, "outputPath");

            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "CooldownManager", managerId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate CooldownManager script.");
            }

            // Add initial cooldowns if provided and component was added
            if (result.TryGetValue("componentAdded", out var added) && (bool)added)
            {
                if (payload.TryGetValue("cooldowns", out var cooldownsObj) && cooldownsObj is List<object> cooldownsList)
                {
                    var component = CodeGenHelper.FindComponentByField(targetGo, "cooldownManagerId", managerId);
                    if (component != null)
                    {
                        var so = new SerializedObject(component);
                        var cooldownsProp = so.FindProperty("cooldowns");
                        if (cooldownsProp != null)
                        {
                            foreach (var cdObj in cooldownsList)
                            {
                                if (cdObj is Dictionary<string, object> cdDict)
                                {
                                    var id = cdDict.TryGetValue("id", out var idObj) ? idObj.ToString() : $"cd_{cooldownsProp.arraySize}";
                                    var duration = cdDict.TryGetValue("duration", out var durObj) ? Convert.ToSingle(durObj) : 1f;
                                    var startReady = cdDict.TryGetValue("startReady", out var readyObj) ? Convert.ToBoolean(readyObj) : true;

                                    cooldownsProp.InsertArrayElementAtIndex(cooldownsProp.arraySize);
                                    var cdProp = cooldownsProp.GetArrayElementAtIndex(cooldownsProp.arraySize - 1);
                                    cdProp.FindPropertyRelative("id").stringValue = id;
                                    cdProp.FindPropertyRelative("duration").floatValue = duration;
                                    cdProp.FindPropertyRelative("startReady").boolValue = startReady;
                                }
                            }
                            so.ApplyModifiedProperties();
                        }
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["path"] = BuildGameObjectPath(targetGo);
            result["created"] = true;

            return result;
        }

        private object AddCooldownToManager(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            if (string.IsNullOrEmpty(targetPath))
                throw new InvalidOperationException("targetPath is required.");

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");

            var component = CodeGenHelper.FindComponentByField(targetGo, "cooldownManagerId", null);
            if (component == null)
                throw new InvalidOperationException($"No CooldownManager found on '{targetPath}'.");

            var id = GetString(payload, "cooldownId") ?? $"cd_{DateTime.Now.Ticks}";
            var duration = GetFloat(payload, "cooldownDuration", 1f);
            var startReady = GetBool(payload, "startReady", true);

            var so = new SerializedObject(component);
            var cooldownsProp = so.FindProperty("cooldowns");

            cooldownsProp.InsertArrayElementAtIndex(cooldownsProp.arraySize);
            var cdProp = cooldownsProp.GetArrayElementAtIndex(cooldownsProp.arraySize - 1);
            cdProp.FindPropertyRelative("id").stringValue = id;
            cdProp.FindPropertyRelative("duration").floatValue = duration;
            cdProp.FindPropertyRelative("startReady").boolValue = startReady;

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("path", BuildGameObjectPath(targetGo)),
                ("cooldownId", id),
                ("cooldownDuration", duration),
                ("added", true)
            );
        }

        private object InspectCooldownManager(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            if (string.IsNullOrEmpty(targetPath))
                throw new InvalidOperationException("targetPath is required.");

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");

            var component = CodeGenHelper.FindComponentByField(targetGo, "cooldownManagerId", null);
            if (component == null)
                return CreateSuccessResponse(("found", false), ("path", targetPath));

            var so = new SerializedObject(component);
            var cooldownsProp = so.FindProperty("cooldowns");
            var cooldownsInfo = new List<Dictionary<string, object>>();

            if (cooldownsProp != null)
            {
                for (int i = 0; i < cooldownsProp.arraySize; i++)
                {
                    var cdProp = cooldownsProp.GetArrayElementAtIndex(i);
                    cooldownsInfo.Add(new Dictionary<string, object>
                    {
                        { "id", cdProp.FindPropertyRelative("id").stringValue },
                        { "duration", cdProp.FindPropertyRelative("duration").floatValue },
                        { "startReady", cdProp.FindPropertyRelative("startReady").boolValue }
                    });
                }
            }

            var info = new Dictionary<string, object>
            {
                { "path", BuildGameObjectPath(component.gameObject) },
                { "cooldownCount", cooldownsInfo.Count },
                { "cooldowns", cooldownsInfo }
            };

            return CreateSuccessResponse(("cooldownManager", info));
        }

        #endregion

        #region Find Operations

        private object FindByTimerId(Dictionary<string, object> payload)
        {
            var timerId = GetString(payload, "timerId");
            if (string.IsNullOrEmpty(timerId))
                throw new InvalidOperationException("timerId is required.");

            var component = CodeGenHelper.FindComponentInSceneByField("timerId", timerId);
            if (component == null)
                return CreateSuccessResponse(("found", false), ("timerId", timerId));

            var so = new SerializedObject(component);
            return CreateSuccessResponse(
                ("found", true),
                ("timerId", timerId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("duration", so.FindProperty("duration").floatValue)
            );
        }

        private object FindByCooldownId(Dictionary<string, object> payload)
        {
            var cooldownId = GetString(payload, "cooldownId");
            if (string.IsNullOrEmpty(cooldownId))
                throw new InvalidOperationException("cooldownId is required.");

            var component = CodeGenHelper.FindComponentInSceneByField("cooldownId", cooldownId);
            if (component == null)
                return CreateSuccessResponse(("found", false), ("cooldownId", cooldownId));

            var so = new SerializedObject(component);
            return CreateSuccessResponse(
                ("found", true),
                ("cooldownId", cooldownId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("cooldownDuration", so.FindProperty("cooldownDuration").floatValue)
            );
        }

        #endregion

        #region Helpers

        private Component ResolveTimerComponent(Dictionary<string, object> payload)
        {
            var timerId = GetString(payload, "timerId");
            if (!string.IsNullOrEmpty(timerId))
            {
                var timerById = CodeGenHelper.FindComponentInSceneByField("timerId", timerId);
                if (timerById != null)
                    return timerById;
            }

            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var timerByPath = CodeGenHelper.FindComponentByField(targetGo, "timerId", null);
                    if (timerByPath != null)
                        return timerByPath;
                    throw new InvalidOperationException($"No Timer component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either timerId or targetPath is required.");
        }

        private Component ResolveCooldownComponent(Dictionary<string, object> payload)
        {
            var cooldownId = GetString(payload, "cooldownId");
            if (!string.IsNullOrEmpty(cooldownId))
            {
                var cooldownById = CodeGenHelper.FindComponentInSceneByField("cooldownId", cooldownId);
                if (cooldownById != null)
                    return cooldownById;
            }

            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var cooldownByPath = CodeGenHelper.FindComponentByField(targetGo, "cooldownId", null);
                    if (cooldownByPath != null)
                        return cooldownByPath;
                    throw new InvalidOperationException($"No Cooldown component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either cooldownId or targetPath is required.");
        }

        #endregion
    }
}
