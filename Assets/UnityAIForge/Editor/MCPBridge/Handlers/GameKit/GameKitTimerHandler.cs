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
    /// GameKit Timer handler: create and manage timers and cooldowns.
    /// Provides timer, cooldown, and cooldown manager functionality.
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

        protected override bool RequiresCompilationWait(string operation) => false;

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
            {
                throw new InvalidOperationException("targetPath is required for createTimer.");
            }

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
            {
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            var timerId = GetString(payload, "timerId") ?? $"Timer_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var duration = GetFloat(payload, "duration", 5f);
            var loop = GetBool(payload, "loop", false);
            var unscaledTime = GetBool(payload, "unscaledTime", false);

            var timer = Undo.AddComponent<GameKitTimer>(targetGo);
            timer.Initialize(timerId, duration, loop, unscaledTime);

            // Apply additional settings
            var serialized = new SerializedObject(timer);

            if (payload.TryGetValue("autoStart", out var autoObj))
            {
                serialized.FindProperty("autoStart").boolValue = Convert.ToBoolean(autoObj);
            }

            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("timerId", timerId),
                ("path", BuildGameObjectPath(targetGo)),
                ("duration", duration),
                ("loop", loop)
            );
        }

        private object UpdateTimer(Dictionary<string, object> payload)
        {
            var timer = ResolveTimerComponent(payload);

            Undo.RecordObject(timer, "Update GameKit Timer");
            var serialized = new SerializedObject(timer);

            if (payload.TryGetValue("duration", out var durObj))
            {
                serialized.FindProperty("duration").floatValue = Convert.ToSingle(durObj);
            }

            if (payload.TryGetValue("loop", out var loopObj))
            {
                serialized.FindProperty("loop").boolValue = Convert.ToBoolean(loopObj);
            }

            if (payload.TryGetValue("autoStart", out var autoObj))
            {
                serialized.FindProperty("autoStart").boolValue = Convert.ToBoolean(autoObj);
            }

            if (payload.TryGetValue("unscaledTime", out var unscaledObj))
            {
                serialized.FindProperty("unscaledTime").boolValue = Convert.ToBoolean(unscaledObj);
            }

            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(timer.gameObject.scene);

            return CreateSuccessResponse(
                ("timerId", timer.TimerId),
                ("path", BuildGameObjectPath(timer.gameObject)),
                ("updated", true)
            );
        }

        private object InspectTimer(Dictionary<string, object> payload)
        {
            var timer = ResolveTimerComponent(payload);

            var serialized = new SerializedObject(timer);

            var info = new Dictionary<string, object>
            {
                { "timerId", timer.TimerId },
                { "path", BuildGameObjectPath(timer.gameObject) },
                { "duration", timer.Duration },
                { "remainingTime", timer.RemainingTime },
                { "elapsedTime", timer.ElapsedTime },
                { "normalizedTime", timer.NormalizedTime },
                { "isRunning", timer.IsRunning },
                { "isPaused", timer.IsPaused },
                { "isComplete", timer.IsComplete },
                { "loop", serialized.FindProperty("loop").boolValue },
                { "autoStart", serialized.FindProperty("autoStart").boolValue },
                { "unscaledTime", serialized.FindProperty("unscaledTime").boolValue }
            };

            return CreateSuccessResponse(("timer", info));
        }

        private object DeleteTimer(Dictionary<string, object> payload)
        {
            var timer = ResolveTimerComponent(payload);
            var path = BuildGameObjectPath(timer.gameObject);
            var timerId = timer.TimerId;
            var scene = timer.gameObject.scene;

            Undo.DestroyObjectImmediate(timer);
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
            {
                throw new InvalidOperationException("targetPath is required for createCooldown.");
            }

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
            {
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            var cooldownId = GetString(payload, "cooldownId") ?? $"Cooldown_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var duration = GetFloat(payload, "cooldownDuration", 1f);
            var startReady = GetBool(payload, "startReady", true);

            var cooldown = Undo.AddComponent<GameKitCooldown>(targetGo);
            cooldown.Initialize(cooldownId, duration, startReady);

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("cooldownId", cooldownId),
                ("path", BuildGameObjectPath(targetGo)),
                ("cooldownDuration", duration),
                ("startReady", startReady)
            );
        }

        private object UpdateCooldown(Dictionary<string, object> payload)
        {
            var cooldown = ResolveCooldownComponent(payload);

            Undo.RecordObject(cooldown, "Update GameKit Cooldown");
            var serialized = new SerializedObject(cooldown);

            if (payload.TryGetValue("cooldownDuration", out var durObj))
            {
                serialized.FindProperty("cooldownDuration").floatValue = Convert.ToSingle(durObj);
            }

            if (payload.TryGetValue("startReady", out var readyObj))
            {
                serialized.FindProperty("startReady").boolValue = Convert.ToBoolean(readyObj);
            }

            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(cooldown.gameObject.scene);

            return CreateSuccessResponse(
                ("cooldownId", cooldown.CooldownId),
                ("path", BuildGameObjectPath(cooldown.gameObject)),
                ("updated", true)
            );
        }

        private object InspectCooldown(Dictionary<string, object> payload)
        {
            var cooldown = ResolveCooldownComponent(payload);

            var info = new Dictionary<string, object>
            {
                { "cooldownId", cooldown.CooldownId },
                { "path", BuildGameObjectPath(cooldown.gameObject) },
                { "cooldownDuration", cooldown.CooldownDuration },
                { "remainingCooldown", cooldown.RemainingCooldown },
                { "normalizedCooldown", cooldown.NormalizedCooldown },
                { "isReady", cooldown.IsReady },
                { "isOnCooldown", cooldown.IsOnCooldown }
            };

            return CreateSuccessResponse(("cooldown", info));
        }

        private object DeleteCooldown(Dictionary<string, object> payload)
        {
            var cooldown = ResolveCooldownComponent(payload);
            var path = BuildGameObjectPath(cooldown.gameObject);
            var cooldownId = cooldown.CooldownId;
            var scene = cooldown.gameObject.scene;

            Undo.DestroyObjectImmediate(cooldown);
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
            {
                throw new InvalidOperationException("targetPath is required for createCooldownManager.");
            }

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
            {
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            var existingManager = targetGo.GetComponent<GameKitCooldownManager>();
            if (existingManager != null)
            {
                throw new InvalidOperationException($"GameObject already has a GameKitCooldownManager component.");
            }

            var manager = Undo.AddComponent<GameKitCooldownManager>(targetGo);

            // Add initial cooldowns if provided
            if (payload.TryGetValue("cooldowns", out var cooldownsObj) && cooldownsObj is List<object> cooldownsList)
            {
                var serialized = new SerializedObject(manager);
                var cooldownsProp = serialized.FindProperty("cooldowns");

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

                serialized.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("path", BuildGameObjectPath(targetGo)),
                ("created", true)
            );
        }

        private object AddCooldownToManager(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            if (string.IsNullOrEmpty(targetPath))
            {
                throw new InvalidOperationException("targetPath is required.");
            }

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
            {
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            var manager = targetGo.GetComponent<GameKitCooldownManager>();
            if (manager == null)
            {
                throw new InvalidOperationException($"No GameKitCooldownManager found on '{targetPath}'.");
            }

            var id = GetString(payload, "cooldownId") ?? $"cd_{DateTime.Now.Ticks}";
            var duration = GetFloat(payload, "cooldownDuration", 1f);
            var startReady = GetBool(payload, "startReady", true);

            var serialized = new SerializedObject(manager);
            var cooldownsProp = serialized.FindProperty("cooldowns");

            cooldownsProp.InsertArrayElementAtIndex(cooldownsProp.arraySize);
            var cdProp = cooldownsProp.GetArrayElementAtIndex(cooldownsProp.arraySize - 1);
            cdProp.FindPropertyRelative("id").stringValue = id;
            cdProp.FindPropertyRelative("duration").floatValue = duration;
            cdProp.FindPropertyRelative("startReady").boolValue = startReady;

            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);

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
            {
                throw new InvalidOperationException("targetPath is required.");
            }

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
            {
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            var manager = targetGo.GetComponent<GameKitCooldownManager>();
            if (manager == null)
            {
                return CreateSuccessResponse(("found", false), ("path", targetPath));
            }

            var serialized = new SerializedObject(manager);
            var cooldownsProp = serialized.FindProperty("cooldowns");
            var cooldownsInfo = new List<Dictionary<string, object>>();

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

            var info = new Dictionary<string, object>
            {
                { "path", BuildGameObjectPath(manager.gameObject) },
                { "cooldownCount", cooldownsProp.arraySize },
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
            {
                throw new InvalidOperationException("timerId is required.");
            }

            var timer = FindTimerById(timerId);
            if (timer == null)
            {
                return CreateSuccessResponse(("found", false), ("timerId", timerId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("timerId", timer.TimerId),
                ("path", BuildGameObjectPath(timer.gameObject)),
                ("duration", timer.Duration),
                ("isRunning", timer.IsRunning)
            );
        }

        private object FindByCooldownId(Dictionary<string, object> payload)
        {
            var cooldownId = GetString(payload, "cooldownId");
            if (string.IsNullOrEmpty(cooldownId))
            {
                throw new InvalidOperationException("cooldownId is required.");
            }

            var cooldown = FindCooldownById(cooldownId);
            if (cooldown == null)
            {
                return CreateSuccessResponse(("found", false), ("cooldownId", cooldownId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("cooldownId", cooldown.CooldownId),
                ("path", BuildGameObjectPath(cooldown.gameObject)),
                ("cooldownDuration", cooldown.CooldownDuration),
                ("isReady", cooldown.IsReady)
            );
        }

        #endregion

        #region Helpers

        private GameKitTimer ResolveTimerComponent(Dictionary<string, object> payload)
        {
            var timerId = GetString(payload, "timerId");
            if (!string.IsNullOrEmpty(timerId))
            {
                var timerById = FindTimerById(timerId);
                if (timerById != null)
                {
                    return timerById;
                }
            }

            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var timerByPath = targetGo.GetComponent<GameKitTimer>();
                    if (timerByPath != null)
                    {
                        return timerByPath;
                    }
                    throw new InvalidOperationException($"No GameKitTimer component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either timerId or targetPath is required.");
        }

        private GameKitCooldown ResolveCooldownComponent(Dictionary<string, object> payload)
        {
            var cooldownId = GetString(payload, "cooldownId");
            if (!string.IsNullOrEmpty(cooldownId))
            {
                var cooldownById = FindCooldownById(cooldownId);
                if (cooldownById != null)
                {
                    return cooldownById;
                }
            }

            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var cooldownByPath = targetGo.GetComponent<GameKitCooldown>();
                    if (cooldownByPath != null)
                    {
                        return cooldownByPath;
                    }
                    throw new InvalidOperationException($"No GameKitCooldown component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either cooldownId or targetPath is required.");
        }

        private GameKitTimer FindTimerById(string timerId)
        {
            var timers = UnityEngine.Object.FindObjectsByType<GameKitTimer>(FindObjectsSortMode.None);
            foreach (var timer in timers)
            {
                if (timer.TimerId == timerId)
                {
                    return timer;
                }
            }
            return null;
        }

        private GameKitCooldown FindCooldownById(string cooldownId)
        {
            var cooldowns = UnityEngine.Object.FindObjectsByType<GameKitCooldown>(FindObjectsSortMode.None);
            foreach (var cooldown in cooldowns)
            {
                if (cooldown.CooldownId == cooldownId)
                {
                    return cooldown;
                }
            }
            return null;
        }

        #endregion
    }
}
