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
    /// GameKit Health handler: create and manage health systems for game entities.
    /// Provides damage, healing, death, and respawn functionality without custom scripts.
    /// </summary>
    public class GameKitHealthHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "update", "inspect", "delete",
            "applyDamage", "heal", "kill", "respawn",
            "setInvincible", "findByHealthId"
        };

        public override string Category => "gamekitHealth";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateHealth(payload),
                "update" => UpdateHealth(payload),
                "inspect" => InspectHealth(payload),
                "delete" => DeleteHealth(payload),
                "applyDamage" => ApplyDamage(payload),
                "heal" => Heal(payload),
                "kill" => Kill(payload),
                "respawn" => Respawn(payload),
                "setInvincible" => SetInvincible(payload),
                "findByHealthId" => FindByHealthId(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit Health operation: {operation}")
            };
        }

        #region Create

        private object CreateHealth(Dictionary<string, object> payload)
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

            // Check if already has health component
            var existingHealth = targetGo.GetComponent<GameKitHealth>();
            if (existingHealth != null)
            {
                throw new InvalidOperationException($"GameObject '{targetPath}' already has a GameKitHealth component.");
            }

            var healthId = GetString(payload, "healthId") ?? $"Health_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var maxHealth = GetFloat(payload, "maxHealth", 100f);
            var currentHealth = GetFloat(payload, "currentHealth", maxHealth);
            var deathBehavior = ParseDeathBehavior(GetString(payload, "onDeath") ?? "destroy");

            // Add component
            var health = Undo.AddComponent<GameKitHealth>(targetGo);
            health.Initialize(healthId, maxHealth, currentHealth, deathBehavior);

            // Set additional properties
            ApplyHealthSettings(health, payload);

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("healthId", healthId),
                ("path", BuildGameObjectPath(targetGo)),
                ("maxHealth", maxHealth),
                ("currentHealth", currentHealth),
                ("onDeath", deathBehavior.ToString())
            );
        }

        #endregion

        #region Update

        private object UpdateHealth(Dictionary<string, object> payload)
        {
            var health = ResolveHealthComponent(payload);

            Undo.RecordObject(health, "Update GameKit Health");

            var serializedHealth = new SerializedObject(health);

            if (payload.TryGetValue("maxHealth", out var maxObj))
            {
                serializedHealth.FindProperty("maxHealth").floatValue = Convert.ToSingle(maxObj);
            }

            if (payload.TryGetValue("currentHealth", out var currObj))
            {
                serializedHealth.FindProperty("currentHealth").floatValue = Convert.ToSingle(currObj);
            }

            if (payload.TryGetValue("invincibilityDuration", out var invObj))
            {
                serializedHealth.FindProperty("invincibilityDuration").floatValue = Convert.ToSingle(invObj);
            }

            if (payload.TryGetValue("canTakeDamage", out var canDmgObj))
            {
                serializedHealth.FindProperty("canTakeDamage").boolValue = Convert.ToBoolean(canDmgObj);
            }

            if (payload.TryGetValue("onDeath", out var deathObj))
            {
                var deathBehavior = ParseDeathBehavior(deathObj.ToString());
                serializedHealth.FindProperty("onDeath").enumValueIndex = (int)deathBehavior;
            }

            if (payload.TryGetValue("respawnPosition", out var respawnObj) && respawnObj is Dictionary<string, object> respawnDict)
            {
                var respawnPos = GetVector3FromDict(respawnDict, Vector3.zero);
                serializedHealth.FindProperty("respawnPosition").vector3Value = respawnPos;
            }

            if (payload.TryGetValue("respawnDelay", out var delayObj))
            {
                serializedHealth.FindProperty("respawnDelay").floatValue = Convert.ToSingle(delayObj);
            }

            if (payload.TryGetValue("resetHealthOnRespawn", out var resetObj))
            {
                serializedHealth.FindProperty("resetHealthOnRespawn").boolValue = Convert.ToBoolean(resetObj);
            }

            serializedHealth.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(health.gameObject.scene);

            return CreateSuccessResponse(
                ("healthId", health.HealthId),
                ("path", BuildGameObjectPath(health.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectHealth(Dictionary<string, object> payload)
        {
            var health = ResolveHealthComponent(payload);

            var info = new Dictionary<string, object>
            {
                { "healthId", health.HealthId },
                { "path", BuildGameObjectPath(health.gameObject) },
                { "maxHealth", health.MaxHealth },
                { "currentHealth", health.CurrentHealth },
                { "healthPercent", health.HealthPercent },
                { "isAlive", health.IsAlive },
                { "isDead", health.IsDead },
                { "isInvincible", health.IsInvincible },
                { "canTakeDamage", health.CanTakeDamage },
                { "onDeath", health.DeathBehaviorType.ToString() },
                { "respawnPosition", new Dictionary<string, object>
                    {
                        { "x", health.RespawnPosition.x },
                        { "y", health.RespawnPosition.y },
                        { "z", health.RespawnPosition.z }
                    }
                }
            };

            return CreateSuccessResponse(("health", info));
        }

        #endregion

        #region Delete

        private object DeleteHealth(Dictionary<string, object> payload)
        {
            var health = ResolveHealthComponent(payload);
            var path = BuildGameObjectPath(health.gameObject);
            var healthId = health.HealthId;
            var scene = health.gameObject.scene;

            Undo.DestroyObjectImmediate(health);
            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("healthId", healthId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Runtime Operations

        private object ApplyDamage(Dictionary<string, object> payload)
        {
            var health = ResolveHealthComponent(payload);
            var amount = GetFloat(payload, "amount", 0f);

            if (amount <= 0)
            {
                throw new InvalidOperationException("amount must be greater than 0 for applyDamage.");
            }

            // Note: In editor mode, we directly modify the serialized property
            // Runtime damage with invincibility would require play mode
            var serializedHealth = new SerializedObject(health);
            var currentProp = serializedHealth.FindProperty("currentHealth");
            var previousHealth = currentProp.floatValue;
            var newHealth = Mathf.Max(0, previousHealth - amount);
            currentProp.floatValue = newHealth;
            serializedHealth.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(health.gameObject.scene);

            return CreateSuccessResponse(
                ("healthId", health.HealthId),
                ("previousHealth", previousHealth),
                ("currentHealth", newHealth),
                ("damageDealt", previousHealth - newHealth),
                ("isDead", newHealth <= 0)
            );
        }

        private object Heal(Dictionary<string, object> payload)
        {
            var health = ResolveHealthComponent(payload);
            var amount = GetFloat(payload, "amount", 0f);

            if (amount <= 0)
            {
                throw new InvalidOperationException("amount must be greater than 0 for heal.");
            }

            var serializedHealth = new SerializedObject(health);
            var currentProp = serializedHealth.FindProperty("currentHealth");
            var maxProp = serializedHealth.FindProperty("maxHealth");
            var previousHealth = currentProp.floatValue;
            var newHealth = Mathf.Min(maxProp.floatValue, previousHealth + amount);
            currentProp.floatValue = newHealth;
            serializedHealth.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(health.gameObject.scene);

            return CreateSuccessResponse(
                ("healthId", health.HealthId),
                ("previousHealth", previousHealth),
                ("currentHealth", newHealth),
                ("amountHealed", newHealth - previousHealth)
            );
        }

        private object Kill(Dictionary<string, object> payload)
        {
            var health = ResolveHealthComponent(payload);

            var serializedHealth = new SerializedObject(health);
            serializedHealth.FindProperty("currentHealth").floatValue = 0;
            serializedHealth.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(health.gameObject.scene);

            return CreateSuccessResponse(
                ("healthId", health.HealthId),
                ("killed", true),
                ("note", "In editor mode, death behavior will trigger in play mode.")
            );
        }

        private object Respawn(Dictionary<string, object> payload)
        {
            var health = ResolveHealthComponent(payload);

            var serializedHealth = new SerializedObject(health);
            var maxHealth = serializedHealth.FindProperty("maxHealth").floatValue;
            var respawnPos = serializedHealth.FindProperty("respawnPosition").vector3Value;

            // Reset health
            if (serializedHealth.FindProperty("resetHealthOnRespawn").boolValue)
            {
                serializedHealth.FindProperty("currentHealth").floatValue = maxHealth;
            }
            serializedHealth.ApplyModifiedProperties();

            // Move to respawn position
            health.transform.position = respawnPos;

            // Ensure active
            health.gameObject.SetActive(true);

            EditorSceneManager.MarkSceneDirty(health.gameObject.scene);

            return CreateSuccessResponse(
                ("healthId", health.HealthId),
                ("respawned", true),
                ("position", new Dictionary<string, object>
                {
                    { "x", respawnPos.x },
                    { "y", respawnPos.y },
                    { "z", respawnPos.z }
                }),
                ("currentHealth", serializedHealth.FindProperty("currentHealth").floatValue)
            );
        }

        private object SetInvincible(Dictionary<string, object> payload)
        {
            var health = ResolveHealthComponent(payload);
            var invincible = GetBool(payload, "invincible", true);
            var duration = GetFloat(payload, "duration", 0f);

            // Set isInvincible field directly
            var serializedHealth = new SerializedObject(health);
            serializedHealth.FindProperty("isInvincible").boolValue = invincible;

            if (duration > 0)
            {
                serializedHealth.FindProperty("invincibilityDuration").floatValue = duration;
            }

            serializedHealth.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(health.gameObject.scene);

            return CreateSuccessResponse(
                ("healthId", health.HealthId),
                ("isInvincible", invincible),
                ("note", invincible ? "Set to invincible" : "Set to vulnerable")
            );
        }

        #endregion

        #region Find

        private object FindByHealthId(Dictionary<string, object> payload)
        {
            var healthId = GetString(payload, "healthId");
            if (string.IsNullOrEmpty(healthId))
            {
                throw new InvalidOperationException("healthId is required for findByHealthId.");
            }

            var health = FindHealthById(healthId);
            if (health == null)
            {
                return CreateSuccessResponse(("found", false), ("healthId", healthId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("healthId", health.HealthId),
                ("path", BuildGameObjectPath(health.gameObject)),
                ("currentHealth", health.CurrentHealth),
                ("maxHealth", health.MaxHealth),
                ("isAlive", health.IsAlive)
            );
        }

        #endregion

        #region Helpers

        private GameKitHealth ResolveHealthComponent(Dictionary<string, object> payload)
        {
            // Try by healthId first
            var healthId = GetString(payload, "healthId");
            if (!string.IsNullOrEmpty(healthId))
            {
                var healthById = FindHealthById(healthId);
                if (healthById != null)
                {
                    return healthById;
                }
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var healthByPath = targetGo.GetComponent<GameKitHealth>();
                    if (healthByPath != null)
                    {
                        return healthByPath;
                    }
                    throw new InvalidOperationException($"No GameKitHealth component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either healthId or targetPath is required.");
        }

        private GameKitHealth FindHealthById(string healthId)
        {
            var healths = UnityEngine.Object.FindObjectsByType<GameKitHealth>(FindObjectsSortMode.None);
            foreach (var health in healths)
            {
                if (health.HealthId == healthId)
                {
                    return health;
                }
            }
            return null;
        }

        private void ApplyHealthSettings(GameKitHealth health, Dictionary<string, object> payload)
        {
            var serializedHealth = new SerializedObject(health);

            if (payload.TryGetValue("invincibilityDuration", out var invObj))
            {
                serializedHealth.FindProperty("invincibilityDuration").floatValue = Convert.ToSingle(invObj);
            }

            if (payload.TryGetValue("canTakeDamage", out var canDmgObj))
            {
                serializedHealth.FindProperty("canTakeDamage").boolValue = Convert.ToBoolean(canDmgObj);
            }

            if (payload.TryGetValue("respawnPosition", out var respawnObj) && respawnObj is Dictionary<string, object> respawnDict)
            {
                var respawnPos = GetVector3FromDict(respawnDict, Vector3.zero);
                serializedHealth.FindProperty("respawnPosition").vector3Value = respawnPos;
            }

            if (payload.TryGetValue("respawnDelay", out var delayObj))
            {
                serializedHealth.FindProperty("respawnDelay").floatValue = Convert.ToSingle(delayObj);
            }

            if (payload.TryGetValue("resetHealthOnRespawn", out var resetObj))
            {
                serializedHealth.FindProperty("resetHealthOnRespawn").boolValue = Convert.ToBoolean(resetObj);
            }

            serializedHealth.ApplyModifiedProperties();
        }

        private GameKitHealth.DeathBehavior ParseDeathBehavior(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "destroy" => GameKitHealth.DeathBehavior.Destroy,
                "disable" => GameKitHealth.DeathBehavior.Disable,
                "respawn" => GameKitHealth.DeathBehavior.Respawn,
                "event" => GameKitHealth.DeathBehavior.Event,
                _ => GameKitHealth.DeathBehavior.Destroy
            };
        }

        private Vector3 GetVector3FromDict(Dictionary<string, object> dict, Vector3 fallback)
        {
            float x = dict.TryGetValue("x", out var xObj) ? Convert.ToSingle(xObj) : fallback.x;
            float y = dict.TryGetValue("y", out var yObj) ? Convert.ToSingle(yObj) : fallback.y;
            float z = dict.TryGetValue("z", out var zObj) ? Convert.ToSingle(zObj) : fallback.z;
            return new Vector3(x, y, z);
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

        private float GetFloat(Dictionary<string, object> payload, string key, float defaultValue)
        {
            if (payload.TryGetValue(key, out var value))
            {
                return Convert.ToSingle(value);
            }
            return defaultValue;
        }

        private bool GetBool(Dictionary<string, object> payload, string key, bool defaultValue)
        {
            if (payload.TryGetValue(key, out var value))
            {
                return Convert.ToBoolean(value);
            }
            return defaultValue;
        }

        #endregion
    }
}
