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
    /// GameKit Health handler: create and manage health systems for game entities.
    /// Uses code generation to produce standalone Health scripts with zero package dependency.
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

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

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

            // Check if already has a health component (by checking for healthId field)
            var existingHealth = CodeGenHelper.FindComponentByField(targetGo, "healthId", null);
            if (existingHealth != null)
            {
                throw new InvalidOperationException(
                    $"GameObject '{targetPath}' already has a Health component.");
            }

            var healthId = GetString(payload, "healthId") ?? $"Health_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var maxHealth = GetFloat(payload, "maxHealth", 100f);
            var currentHealth = GetFloat(payload, "currentHealth", maxHealth);
            var deathBehavior = ParseDeathBehavior(GetString(payload, "onDeath") ?? "destroy");
            var invincibilityDuration = GetFloat(payload, "invincibilityDuration", 0f);
            var respawnDelay = GetFloat(payload, "respawnDelay", 1f);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(healthId, "Health");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "HEALTH_ID", healthId },
                { "MAX_HEALTH", maxHealth },
                { "CURRENT_HEALTH", currentHealth },
                { "INVINCIBILITY_DURATION", invincibilityDuration },
                { "DEATH_BEHAVIOR", deathBehavior },
                { "RESPAWN_DELAY", respawnDelay }
            };

            // Build properties to set after component is added
            var propertiesToSet = new Dictionary<string, object>();
            if (payload.TryGetValue("canTakeDamage", out var canDmgObj))
                propertiesToSet["canTakeDamage"] = Convert.ToBoolean(canDmgObj);
            if (payload.TryGetValue("respawnPosition", out var respawnObj) && respawnObj is Dictionary<string, object> respawnDict)
                propertiesToSet["respawnPosition"] = respawnDict;
            if (payload.TryGetValue("resetHealthOnRespawn", out var resetObj))
                propertiesToSet["resetHealthOnRespawn"] = Convert.ToBoolean(resetObj);

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "Health", healthId, className, variables, outputDir,
                propertiesToSet.Count > 0 ? propertiesToSet : null);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate Health script.");
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["healthId"] = healthId;
            result["path"] = BuildGameObjectPath(targetGo);
            result["maxHealth"] = maxHealth;
            result["currentHealth"] = currentHealth;
            result["onDeath"] = deathBehavior;

            return result;
        }

        #endregion

        #region Update

        private object UpdateHealth(Dictionary<string, object> payload)
        {
            var component = ResolveHealthComponent(payload);

            Undo.RecordObject(component, "Update Health");

            var so = new SerializedObject(component);

            if (payload.TryGetValue("maxHealth", out var maxObj))
                so.FindProperty("maxHealth").floatValue = Convert.ToSingle(maxObj);

            if (payload.TryGetValue("currentHealth", out var currObj))
                so.FindProperty("currentHealth").floatValue = Convert.ToSingle(currObj);

            if (payload.TryGetValue("invincibilityDuration", out var invObj))
                so.FindProperty("invincibilityDuration").floatValue = Convert.ToSingle(invObj);

            if (payload.TryGetValue("canTakeDamage", out var canDmgObj))
                so.FindProperty("canTakeDamage").boolValue = Convert.ToBoolean(canDmgObj);

            if (payload.TryGetValue("onDeath", out var deathObj))
            {
                var deathBehavior = ParseDeathBehavior(deathObj.ToString());
                var deathProp = so.FindProperty("onDeath");
                var names = deathProp.enumDisplayNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], deathBehavior, StringComparison.OrdinalIgnoreCase))
                    {
                        deathProp.enumValueIndex = i;
                        break;
                    }
                }
            }

            if (payload.TryGetValue("respawnPosition", out var respawnObj) && respawnObj is Dictionary<string, object> respawnDict)
            {
                var respawnPos = GetVector3FromDict(respawnDict, Vector3.zero);
                so.FindProperty("respawnPosition").vector3Value = respawnPos;
            }

            if (payload.TryGetValue("respawnDelay", out var delayObj))
                so.FindProperty("respawnDelay").floatValue = Convert.ToSingle(delayObj);

            if (payload.TryGetValue("resetHealthOnRespawn", out var resetObj))
                so.FindProperty("resetHealthOnRespawn").boolValue = Convert.ToBoolean(resetObj);

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var healthId = new SerializedObject(component).FindProperty("healthId").stringValue;

            return CreateSuccessResponse(
                ("healthId", healthId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectHealth(Dictionary<string, object> payload)
        {
            var component = ResolveHealthComponent(payload);
            var so = new SerializedObject(component);

            var maxHealth = so.FindProperty("maxHealth").floatValue;
            var currentHealth = so.FindProperty("currentHealth").floatValue;
            var respawnPos = so.FindProperty("respawnPosition").vector3Value;
            var isInvincible = so.FindProperty("isInvincible").boolValue;
            var canTakeDamage = so.FindProperty("canTakeDamage").boolValue;
            var onDeathProp = so.FindProperty("onDeath");
            var onDeath = onDeathProp.enumValueIndex < onDeathProp.enumDisplayNames.Length
                ? onDeathProp.enumDisplayNames[onDeathProp.enumValueIndex]
                : "Destroy";

            var info = new Dictionary<string, object>
            {
                { "healthId", so.FindProperty("healthId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "maxHealth", maxHealth },
                { "currentHealth", currentHealth },
                { "healthPercent", maxHealth > 0 ? currentHealth / maxHealth : 0f },
                { "isAlive", currentHealth > 0 },
                { "isDead", currentHealth <= 0 },
                { "isInvincible", isInvincible },
                { "canTakeDamage", canTakeDamage && !isInvincible },
                { "onDeath", onDeath },
                { "respawnPosition", new Dictionary<string, object>
                    {
                        { "x", respawnPos.x },
                        { "y", respawnPos.y },
                        { "z", respawnPos.z }
                    }
                }
            };

            return CreateSuccessResponse(("health", info));
        }

        #endregion

        #region Delete

        private object DeleteHealth(Dictionary<string, object> payload)
        {
            var component = ResolveHealthComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var healthId = new SerializedObject(component).FindProperty("healthId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component);

            // Also clean up the generated script from tracker
            ScriptGenerator.Delete(healthId);

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
            var component = ResolveHealthComponent(payload);
            var amount = GetFloat(payload, "amount", 0f);

            if (amount <= 0)
            {
                throw new InvalidOperationException("amount must be greater than 0 for applyDamage.");
            }

            var so = new SerializedObject(component);
            var currentProp = so.FindProperty("currentHealth");
            var previousHealth = currentProp.floatValue;
            var newHealth = Mathf.Max(0, previousHealth - amount);
            currentProp.floatValue = newHealth;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("healthId", so.FindProperty("healthId").stringValue),
                ("previousHealth", previousHealth),
                ("currentHealth", newHealth),
                ("damageDealt", previousHealth - newHealth),
                ("isDead", newHealth <= 0)
            );
        }

        private object Heal(Dictionary<string, object> payload)
        {
            var component = ResolveHealthComponent(payload);
            var amount = GetFloat(payload, "amount", 0f);

            if (amount <= 0)
            {
                throw new InvalidOperationException("amount must be greater than 0 for heal.");
            }

            var so = new SerializedObject(component);
            var currentProp = so.FindProperty("currentHealth");
            var maxProp = so.FindProperty("maxHealth");
            var previousHealth = currentProp.floatValue;
            var newHealth = Mathf.Min(maxProp.floatValue, previousHealth + amount);
            currentProp.floatValue = newHealth;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("healthId", so.FindProperty("healthId").stringValue),
                ("previousHealth", previousHealth),
                ("currentHealth", newHealth),
                ("amountHealed", newHealth - previousHealth)
            );
        }

        private object Kill(Dictionary<string, object> payload)
        {
            var component = ResolveHealthComponent(payload);

            var so = new SerializedObject(component);
            so.FindProperty("currentHealth").floatValue = 0;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("healthId", so.FindProperty("healthId").stringValue),
                ("killed", true),
                ("note", "In editor mode, death behavior will trigger in play mode.")
            );
        }

        private object Respawn(Dictionary<string, object> payload)
        {
            var component = ResolveHealthComponent(payload);

            var so = new SerializedObject(component);
            var maxHealth = so.FindProperty("maxHealth").floatValue;
            var respawnPos = so.FindProperty("respawnPosition").vector3Value;

            if (so.FindProperty("resetHealthOnRespawn").boolValue)
            {
                so.FindProperty("currentHealth").floatValue = maxHealth;
            }
            so.ApplyModifiedProperties();

            component.transform.position = respawnPos;
            component.gameObject.SetActive(true);

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("healthId", so.FindProperty("healthId").stringValue),
                ("respawned", true),
                ("position", new Dictionary<string, object>
                {
                    { "x", respawnPos.x },
                    { "y", respawnPos.y },
                    { "z", respawnPos.z }
                }),
                ("currentHealth", so.FindProperty("currentHealth").floatValue)
            );
        }

        private object SetInvincible(Dictionary<string, object> payload)
        {
            var component = ResolveHealthComponent(payload);
            var invincible = GetBool(payload, "invincible", true);
            var duration = GetFloat(payload, "duration", 0f);

            var so = new SerializedObject(component);
            so.FindProperty("isInvincible").boolValue = invincible;

            if (duration > 0)
            {
                so.FindProperty("invincibilityDuration").floatValue = duration;
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("healthId", so.FindProperty("healthId").stringValue),
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

            var component = CodeGenHelper.FindComponentInSceneByField("healthId", healthId);
            if (component == null)
            {
                return CreateSuccessResponse(("found", false), ("healthId", healthId));
            }

            var so = new SerializedObject(component);
            var maxHealth = so.FindProperty("maxHealth").floatValue;
            var currentHealth = so.FindProperty("currentHealth").floatValue;

            return CreateSuccessResponse(
                ("found", true),
                ("healthId", healthId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("currentHealth", currentHealth),
                ("maxHealth", maxHealth),
                ("isAlive", currentHealth > 0)
            );
        }

        #endregion

        #region Helpers

        private Component ResolveHealthComponent(Dictionary<string, object> payload)
        {
            // Try by healthId first
            var healthId = GetString(payload, "healthId");
            if (!string.IsNullOrEmpty(healthId))
            {
                var healthById = CodeGenHelper.FindComponentInSceneByField("healthId", healthId);
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
                    var healthByPath = CodeGenHelper.FindComponentByField(targetGo, "healthId", null);
                    if (healthByPath != null)
                    {
                        return healthByPath;
                    }

                    throw new InvalidOperationException($"No Health component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either healthId or targetPath is required.");
        }

        private string ParseDeathBehavior(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "destroy" => "Destroy",
                "disable" => "Disable",
                "respawn" => "Respawn",
                "event" => "Event",
                _ => "Destroy"
            };
        }

        #endregion
    }
}
