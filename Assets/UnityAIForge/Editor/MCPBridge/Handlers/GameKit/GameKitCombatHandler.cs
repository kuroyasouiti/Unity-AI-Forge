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
    /// GameKit Combat handler: create and manage unified damage/attack systems.
    /// Uses code generation to produce standalone Combat scripts with zero package dependency.
    /// </summary>
    public class GameKitCombatHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "update", "inspect", "delete",
            "addTargetTag", "removeTargetTag", "resetCooldown",
            "findByCombatId"
        };

        public override string Category => "gamekitCombat";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateCombat(payload),
                "update" => UpdateCombat(payload),
                "inspect" => InspectCombat(payload),
                "delete" => DeleteCombat(payload),
                "addTargetTag" => AddTargetTag(payload),
                "removeTargetTag" => RemoveTargetTag(payload),
                "resetCooldown" => ResetCooldown(payload),
                "findByCombatId" => FindByCombatId(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit Combat operation: {operation}")
            };
        }

        #region Create

        private object CreateCombat(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            if (string.IsNullOrEmpty(targetPath))
                throw new InvalidOperationException("targetPath is required for create operation.");

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");

            // Check if already has a combat component
            var existingCombat = CodeGenHelper.FindComponentByField(targetGo, "combatId", null);
            if (existingCombat != null)
                throw new InvalidOperationException($"GameObject '{targetPath}' already has a Combat component.");

            var combatId = GetString(payload, "combatId") ?? $"Combat_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var attackType = ParseAttackType(GetString(payload, "attackType") ?? "melee");
            var baseDamage = GetFloat(payload, "baseDamage", 10f);
            var damageVariance = GetFloat(payload, "damageVariance", 0f);
            var critChance = GetFloat(payload, "critChance", 0f);
            var critMultiplier = GetFloat(payload, "critMultiplier", 2f);
            var attackCooldown = GetFloat(payload, "attackCooldown", 0.5f);
            var hitMultiple = GetBool(payload, "hitMultipleTargets", false);
            var maxTargets = GetInt(payload, "maxTargets", 1);
            var projectileSpeed = GetFloat(payload, "projectileSpeed", 10f);
            var castDistance = GetFloat(payload, "castDistance", 50f);
            var areaRadius = GetFloat(payload, "areaRadius", 5f);

            // Hitbox configuration
            var hitboxShape = "Sphere";
            var hitboxRadius = 1.5f;
            var hitboxSize = "new Vector3(1f, 1f, 1f)";
            var hitboxOffset = "Vector3.zero";

            if (payload.TryGetValue("hitbox", out var hitboxObj) && hitboxObj is Dictionary<string, object> hitboxDict)
            {
                if (hitboxDict.TryGetValue("type", out var shapeObj))
                    hitboxShape = ParseHitboxShape(shapeObj.ToString());
                if (hitboxDict.TryGetValue("radius", out var radiusObj))
                    hitboxRadius = Convert.ToSingle(radiusObj);
                if (hitboxDict.TryGetValue("size", out var sizeObj) && sizeObj is Dictionary<string, object> sizeDict)
                {
                    var sv = GetVector3FromDict(sizeDict, Vector3.one);
                    hitboxSize = $"new Vector3({sv.x}f, {sv.y}f, {sv.z}f)";
                }
                if (hitboxDict.TryGetValue("offset", out var offsetObj) && offsetObj is Dictionary<string, object> offsetDict)
                {
                    var ov = GetVector3FromDict(offsetDict, Vector3.zero);
                    hitboxOffset = $"new Vector3({ov.x}f, {ov.y}f, {ov.z}f)";
                }
            }

            // Target tags
            var targetTagsStr = "";
            if (payload.TryGetValue("targetTags", out var tagsObj) && tagsObj is List<object> tagsList)
            {
                var quoted = new List<string>();
                foreach (var t in tagsList)
                    quoted.Add($"\"{t}\"");
                targetTagsStr = string.Join(", ", quoted);
            }

            // Effect IDs
            var onHitEffectId = GetString(payload, "onHitEffectId") ?? "";
            var onCritEffectId = GetString(payload, "onCritEffectId") ?? "";
            var projectilePrefabPath = GetString(payload, "projectilePrefabPath") ?? "";

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(combatId, "Combat");

            var variables = new Dictionary<string, object>
            {
                { "COMBAT_ID", combatId },
                { "ATTACK_TYPE", attackType },
                { "BASE_DAMAGE", baseDamage },
                { "DAMAGE_VARIANCE", damageVariance },
                { "CRIT_CHANCE", critChance },
                { "CRIT_MULTIPLIER", critMultiplier },
                { "ATTACK_COOLDOWN", attackCooldown },
                { "TARGET_TAGS", targetTagsStr },
                { "HIT_MULTIPLE", hitMultiple.ToString().ToLowerInvariant() },
                { "MAX_TARGETS", maxTargets },
                { "HITBOX_SHAPE", hitboxShape },
                { "HITBOX_RADIUS", hitboxRadius },
                { "HITBOX_SIZE", hitboxSize },
                { "HITBOX_OFFSET", hitboxOffset },
                { "CAST_DISTANCE", castDistance },
                { "AREA_RADIUS", areaRadius },
                { "PROJECTILE_SPEED", projectileSpeed },
                { "PROJECTILE_PREFAB_PATH", projectilePrefabPath },
                { "ON_HIT_EFFECT_ID", onHitEffectId },
                { "ON_CRIT_EFFECT_ID", onCritEffectId }
            };

            var outputDir = GetString(payload, "outputPath");

            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "Combat", combatId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate Combat script.");
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["combatId"] = combatId;
            result["path"] = BuildGameObjectPath(targetGo);
            result["attackType"] = attackType;
            result["baseDamage"] = baseDamage;

            return result;
        }

        #endregion

        #region Update

        private object UpdateCombat(Dictionary<string, object> payload)
        {
            var component = ResolveCombatComponent(payload);

            Undo.RecordObject(component, "Update Combat");

            var so = new SerializedObject(component);

            if (payload.TryGetValue("attackType", out var typeObj))
            {
                var attackTypeName = ParseAttackType(typeObj.ToString());
                var prop = so.FindProperty("attackType");
                var names = prop.enumDisplayNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], attackTypeName, StringComparison.OrdinalIgnoreCase))
                    {
                        prop.enumValueIndex = i;
                        break;
                    }
                }
            }

            if (payload.TryGetValue("baseDamage", out var damageObj))
                so.FindProperty("baseDamage").floatValue = Convert.ToSingle(damageObj);

            if (payload.TryGetValue("damageVariance", out var varianceObj))
                so.FindProperty("damageVariance").floatValue = Convert.ToSingle(varianceObj);

            if (payload.TryGetValue("critChance", out var critChanceObj))
                so.FindProperty("critChance").floatValue = Convert.ToSingle(critChanceObj);

            if (payload.TryGetValue("critMultiplier", out var critMultObj))
                so.FindProperty("critMultiplier").floatValue = Convert.ToSingle(critMultObj);

            if (payload.TryGetValue("attackCooldown", out var cooldownObj))
                so.FindProperty("attackCooldown").floatValue = Convert.ToSingle(cooldownObj);

            if (payload.TryGetValue("hitMultipleTargets", out var multiObj))
                so.FindProperty("hitMultipleTargets").boolValue = Convert.ToBoolean(multiObj);

            if (payload.TryGetValue("maxTargets", out var maxObj))
                so.FindProperty("maxTargets").intValue = Convert.ToInt32(maxObj);

            if (payload.TryGetValue("onHitEffectId", out var hitEffectObj))
                so.FindProperty("onHitEffectId").stringValue = hitEffectObj.ToString();

            if (payload.TryGetValue("onCritEffectId", out var critEffectObj))
                so.FindProperty("onCritEffectId").stringValue = critEffectObj.ToString();

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var combatId = new SerializedObject(component).FindProperty("combatId").stringValue;

            return CreateSuccessResponse(
                ("combatId", combatId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectCombat(Dictionary<string, object> payload)
        {
            var component = ResolveCombatComponent(payload);
            var so = new SerializedObject(component);

            var targetTags = new List<string>();
            var tagsProp = so.FindProperty("targetTags");
            if (tagsProp != null)
            {
                for (int i = 0; i < tagsProp.arraySize; i++)
                    targetTags.Add(tagsProp.GetArrayElementAtIndex(i).stringValue);
            }

            var attackTypeProp = so.FindProperty("attackType");
            var hitboxShapeProp = so.FindProperty("hitboxShape");

            var info = new Dictionary<string, object>
            {
                { "combatId", so.FindProperty("combatId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "attackType", attackTypeProp.enumValueIndex < attackTypeProp.enumDisplayNames.Length
                    ? attackTypeProp.enumDisplayNames[attackTypeProp.enumValueIndex] : "Melee" },
                { "baseDamage", so.FindProperty("baseDamage").floatValue },
                { "damageVariance", so.FindProperty("damageVariance").floatValue },
                { "critChance", so.FindProperty("critChance").floatValue },
                { "critMultiplier", so.FindProperty("critMultiplier").floatValue },
                { "attackCooldown", so.FindProperty("attackCooldown").floatValue },
                { "targetTags", targetTags },
                { "hitMultipleTargets", so.FindProperty("hitMultipleTargets").boolValue },
                { "maxTargets", so.FindProperty("maxTargets").intValue },
                { "hitbox", new Dictionary<string, object>
                    {
                        { "shape", hitboxShapeProp.enumValueIndex < hitboxShapeProp.enumDisplayNames.Length
                            ? hitboxShapeProp.enumDisplayNames[hitboxShapeProp.enumValueIndex] : "Sphere" },
                        { "radius", so.FindProperty("hitboxRadius").floatValue },
                        { "size", Vector3ToDict(so.FindProperty("hitboxSize").vector3Value) },
                        { "offset", Vector3ToDict(so.FindProperty("hitboxOffset").vector3Value) }
                    }
                }
            };

            return CreateSuccessResponse(("combat", info));
        }

        #endregion

        #region Delete

        private object DeleteCombat(Dictionary<string, object> payload)
        {
            var component = ResolveCombatComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var combatId = new SerializedObject(component).FindProperty("combatId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component);

            // Clean up generated script from tracker
            ScriptGenerator.Delete(combatId);

            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("combatId", combatId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Operations

        private object AddTargetTag(Dictionary<string, object> payload)
        {
            var component = ResolveCombatComponent(payload);
            var tag = GetString(payload, "tag");

            if (string.IsNullOrEmpty(tag))
                throw new InvalidOperationException("tag is required for addTargetTag.");

            var so = new SerializedObject(component);
            var tagsProp = so.FindProperty("targetTags");

            // Check if tag already exists
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    return CreateSuccessResponse(
                        ("combatId", so.FindProperty("combatId").stringValue),
                        ("tag", tag),
                        ("added", false),
                        ("reason", "Tag already exists")
                    );
                }
            }

            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("combatId", so.FindProperty("combatId").stringValue),
                ("tag", tag),
                ("added", true)
            );
        }

        private object RemoveTargetTag(Dictionary<string, object> payload)
        {
            var component = ResolveCombatComponent(payload);
            var tag = GetString(payload, "tag");

            if (string.IsNullOrEmpty(tag))
                throw new InvalidOperationException("tag is required for removeTargetTag.");

            var so = new SerializedObject(component);
            var tagsProp = so.FindProperty("targetTags");

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    tagsProp.DeleteArrayElementAtIndex(i);
                    so.ApplyModifiedProperties();
                    EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

                    return CreateSuccessResponse(
                        ("combatId", so.FindProperty("combatId").stringValue),
                        ("tag", tag),
                        ("removed", true)
                    );
                }
            }

            return CreateSuccessResponse(
                ("combatId", so.FindProperty("combatId").stringValue),
                ("tag", tag),
                ("removed", false),
                ("reason", "Tag not found")
            );
        }

        private object ResetCooldown(Dictionary<string, object> payload)
        {
            var component = ResolveCombatComponent(payload);
            var so = new SerializedObject(component);

            return CreateSuccessResponse(
                ("combatId", so.FindProperty("combatId").stringValue),
                ("reset", true),
                ("note", "Cooldown reset will take effect in play mode.")
            );
        }

        private object FindByCombatId(Dictionary<string, object> payload)
        {
            var combatId = GetString(payload, "combatId");
            if (string.IsNullOrEmpty(combatId))
                throw new InvalidOperationException("combatId is required for findByCombatId.");

            var component = CodeGenHelper.FindComponentInSceneByField("combatId", combatId);
            if (component == null)
                return CreateSuccessResponse(("found", false), ("combatId", combatId));

            var so = new SerializedObject(component);
            var attackTypeProp = so.FindProperty("attackType");

            return CreateSuccessResponse(
                ("found", true),
                ("combatId", combatId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("attackType", attackTypeProp.enumValueIndex < attackTypeProp.enumDisplayNames.Length
                    ? attackTypeProp.enumDisplayNames[attackTypeProp.enumValueIndex] : "Melee"),
                ("baseDamage", so.FindProperty("baseDamage").floatValue)
            );
        }

        #endregion

        #region Helpers

        private Component ResolveCombatComponent(Dictionary<string, object> payload)
        {
            // Try by combatId first
            var combatId = GetString(payload, "combatId");
            if (!string.IsNullOrEmpty(combatId))
            {
                var combatById = CodeGenHelper.FindComponentInSceneByField("combatId", combatId);
                if (combatById != null)
                    return combatById;
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var combatByPath = CodeGenHelper.FindComponentByField(targetGo, "combatId", null);
                    if (combatByPath != null)
                        return combatByPath;

                    throw new InvalidOperationException($"No Combat component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either combatId or targetPath is required.");
        }

        private string ParseAttackType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "melee" => "Melee",
                "ranged" => "Ranged",
                "aoe" => "AoE",
                "projectile" => "Projectile",
                _ => "Melee"
            };
        }

        private string ParseHitboxShape(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "sphere" => "Sphere",
                "box" => "Box",
                "capsule" => "Capsule",
                "cone" => "Cone",
                "circle" => "Circle",
                _ => "Sphere"
            };
        }

        private Dictionary<string, object> Vector3ToDict(Vector3 v)
        {
            return new Dictionary<string, object>
            {
                { "x", v.x },
                { "y", v.y },
                { "z", v.z }
            };
        }

        #endregion
    }
}
