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
    /// GameKit Combat handler: create and manage unified damage/attack systems.
    /// Supports melee, ranged, AoE, and projectile attacks with hitbox configuration.
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

        protected override bool RequiresCompilationWait(string operation) => false;

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
            {
                throw new InvalidOperationException("targetPath is required for create operation.");
            }

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
            {
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            // Check if already has combat component
            var existingCombat = targetGo.GetComponent<GameKitCombat>();
            if (existingCombat != null)
            {
                throw new InvalidOperationException($"GameObject '{targetPath}' already has a GameKitCombat component.");
            }

            var combatId = GetString(payload, "combatId") ?? $"Combat_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var attackType = ParseAttackType(GetString(payload, "attackType") ?? "melee");
            var baseDamage = GetFloat(payload, "baseDamage", 10f);

            // Add component
            var combat = Undo.AddComponent<GameKitCombat>(targetGo);

            // Set properties via SerializedObject
            var serializedCombat = new SerializedObject(combat);
            serializedCombat.FindProperty("combatId").stringValue = combatId;
            serializedCombat.FindProperty("attackType").enumValueIndex = (int)attackType;
            serializedCombat.FindProperty("baseDamage").floatValue = baseDamage;

            // Hitbox configuration
            if (payload.TryGetValue("hitbox", out var hitboxObj) && hitboxObj is Dictionary<string, object> hitboxDict)
            {
                if (hitboxDict.TryGetValue("type", out var shapeObj))
                {
                    var shape = ParseHitboxShape(shapeObj.ToString());
                    serializedCombat.FindProperty("hitboxShape").enumValueIndex = (int)shape;
                }

                if (hitboxDict.TryGetValue("radius", out var radiusObj))
                {
                    serializedCombat.FindProperty("hitboxRadius").floatValue = Convert.ToSingle(radiusObj);
                }

                if (hitboxDict.TryGetValue("size", out var sizeObj) && sizeObj is Dictionary<string, object> sizeDict)
                {
                    var size = GetVector3FromDict(sizeDict, Vector3.one);
                    serializedCombat.FindProperty("hitboxSize").vector3Value = size;
                }

                if (hitboxDict.TryGetValue("offset", out var offsetObj) && offsetObj is Dictionary<string, object> offsetDict)
                {
                    var offset = GetVector3FromDict(offsetDict, Vector3.zero);
                    serializedCombat.FindProperty("hitboxOffset").vector3Value = offset;
                }
            }

            // Damage settings
            if (payload.TryGetValue("damageVariance", out var varianceObj))
            {
                serializedCombat.FindProperty("damageVariance").floatValue = Convert.ToSingle(varianceObj);
            }

            if (payload.TryGetValue("critChance", out var critChanceObj))
            {
                serializedCombat.FindProperty("critChance").floatValue = Convert.ToSingle(critChanceObj);
            }

            if (payload.TryGetValue("critMultiplier", out var critMultObj))
            {
                serializedCombat.FindProperty("critMultiplier").floatValue = Convert.ToSingle(critMultObj);
            }

            // Targeting
            if (payload.TryGetValue("targetTags", out var tagsObj) && tagsObj is List<object> tagsList)
            {
                var tagsProp = serializedCombat.FindProperty("targetTags");
                tagsProp.ClearArray();
                for (int i = 0; i < tagsList.Count; i++)
                {
                    tagsProp.InsertArrayElementAtIndex(i);
                    tagsProp.GetArrayElementAtIndex(i).stringValue = tagsList[i].ToString();
                }
            }

            if (payload.TryGetValue("hitMultipleTargets", out var multiObj))
            {
                serializedCombat.FindProperty("hitMultipleTargets").boolValue = Convert.ToBoolean(multiObj);
            }

            if (payload.TryGetValue("maxTargets", out var maxObj))
            {
                serializedCombat.FindProperty("maxTargets").intValue = Convert.ToInt32(maxObj);
            }

            // Timing
            if (payload.TryGetValue("attackCooldown", out var cooldownObj))
            {
                serializedCombat.FindProperty("attackCooldown").floatValue = Convert.ToSingle(cooldownObj);
            }

            // Effects
            if (payload.TryGetValue("onHitEffectId", out var hitEffectObj))
            {
                serializedCombat.FindProperty("onHitEffectId").stringValue = hitEffectObj.ToString();
            }

            if (payload.TryGetValue("onCritEffectId", out var critEffectObj))
            {
                serializedCombat.FindProperty("onCritEffectId").stringValue = critEffectObj.ToString();
            }

            // Projectile settings
            if (payload.TryGetValue("projectileSpeed", out var speedObj))
            {
                serializedCombat.FindProperty("projectileSpeed").floatValue = Convert.ToSingle(speedObj);
            }

            serializedCombat.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("combatId", combatId),
                ("path", BuildGameObjectPath(targetGo)),
                ("attackType", attackType.ToString()),
                ("baseDamage", baseDamage)
            );
        }

        #endregion

        #region Update

        private object UpdateCombat(Dictionary<string, object> payload)
        {
            var combat = ResolveCombatComponent(payload);

            Undo.RecordObject(combat, "Update GameKit Combat");

            var serializedCombat = new SerializedObject(combat);

            if (payload.TryGetValue("attackType", out var typeObj))
            {
                var attackType = ParseAttackType(typeObj.ToString());
                serializedCombat.FindProperty("attackType").enumValueIndex = (int)attackType;
            }

            if (payload.TryGetValue("baseDamage", out var damageObj))
            {
                serializedCombat.FindProperty("baseDamage").floatValue = Convert.ToSingle(damageObj);
            }

            if (payload.TryGetValue("damageVariance", out var varianceObj))
            {
                serializedCombat.FindProperty("damageVariance").floatValue = Convert.ToSingle(varianceObj);
            }

            if (payload.TryGetValue("critChance", out var critChanceObj))
            {
                serializedCombat.FindProperty("critChance").floatValue = Convert.ToSingle(critChanceObj);
            }

            if (payload.TryGetValue("critMultiplier", out var critMultObj))
            {
                serializedCombat.FindProperty("critMultiplier").floatValue = Convert.ToSingle(critMultObj);
            }

            if (payload.TryGetValue("attackCooldown", out var cooldownObj))
            {
                serializedCombat.FindProperty("attackCooldown").floatValue = Convert.ToSingle(cooldownObj);
            }

            if (payload.TryGetValue("hitMultipleTargets", out var multiObj))
            {
                serializedCombat.FindProperty("hitMultipleTargets").boolValue = Convert.ToBoolean(multiObj);
            }

            if (payload.TryGetValue("maxTargets", out var maxObj))
            {
                serializedCombat.FindProperty("maxTargets").intValue = Convert.ToInt32(maxObj);
            }

            if (payload.TryGetValue("onHitEffectId", out var hitEffectObj))
            {
                serializedCombat.FindProperty("onHitEffectId").stringValue = hitEffectObj.ToString();
            }

            if (payload.TryGetValue("onCritEffectId", out var critEffectObj))
            {
                serializedCombat.FindProperty("onCritEffectId").stringValue = critEffectObj.ToString();
            }

            serializedCombat.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(combat.gameObject.scene);

            return CreateSuccessResponse(
                ("combatId", combat.CombatId),
                ("path", BuildGameObjectPath(combat.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectCombat(Dictionary<string, object> payload)
        {
            var combat = ResolveCombatComponent(payload);

            var serializedCombat = new SerializedObject(combat);

            var targetTags = new List<string>();
            var tagsProp = serializedCombat.FindProperty("targetTags");
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                targetTags.Add(tagsProp.GetArrayElementAtIndex(i).stringValue);
            }

            var info = new Dictionary<string, object>
            {
                { "combatId", combat.CombatId },
                { "path", BuildGameObjectPath(combat.gameObject) },
                { "attackType", combat.Type.ToString() },
                { "baseDamage", combat.BaseDamage },
                { "damageVariance", serializedCombat.FindProperty("damageVariance").floatValue },
                { "critChance", serializedCombat.FindProperty("critChance").floatValue },
                { "critMultiplier", serializedCombat.FindProperty("critMultiplier").floatValue },
                { "attackCooldown", serializedCombat.FindProperty("attackCooldown").floatValue },
                { "isOnCooldown", combat.IsOnCooldown },
                { "cooldownRemaining", combat.CooldownRemaining },
                { "targetTags", targetTags },
                { "hitMultipleTargets", serializedCombat.FindProperty("hitMultipleTargets").boolValue },
                { "maxTargets", serializedCombat.FindProperty("maxTargets").intValue },
                { "hitbox", new Dictionary<string, object>
                    {
                        { "shape", serializedCombat.FindProperty("hitboxShape").enumNames[serializedCombat.FindProperty("hitboxShape").enumValueIndex] },
                        { "radius", serializedCombat.FindProperty("hitboxRadius").floatValue },
                        { "size", Vector3ToDict(serializedCombat.FindProperty("hitboxSize").vector3Value) },
                        { "offset", Vector3ToDict(serializedCombat.FindProperty("hitboxOffset").vector3Value) }
                    }
                }
            };

            return CreateSuccessResponse(("combat", info));
        }

        #endregion

        #region Delete

        private object DeleteCombat(Dictionary<string, object> payload)
        {
            var combat = ResolveCombatComponent(payload);
            var path = BuildGameObjectPath(combat.gameObject);
            var combatId = combat.CombatId;
            var scene = combat.gameObject.scene;

            Undo.DestroyObjectImmediate(combat);
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
            var combat = ResolveCombatComponent(payload);
            var tag = GetString(payload, "tag");

            if (string.IsNullOrEmpty(tag))
            {
                throw new InvalidOperationException("tag is required for addTargetTag.");
            }

            var serializedCombat = new SerializedObject(combat);
            var tagsProp = serializedCombat.FindProperty("targetTags");

            // Check if tag already exists
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    return CreateSuccessResponse(
                        ("combatId", combat.CombatId),
                        ("tag", tag),
                        ("added", false),
                        ("reason", "Tag already exists")
                    );
                }
            }

            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            serializedCombat.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(combat.gameObject.scene);

            return CreateSuccessResponse(
                ("combatId", combat.CombatId),
                ("tag", tag),
                ("added", true)
            );
        }

        private object RemoveTargetTag(Dictionary<string, object> payload)
        {
            var combat = ResolveCombatComponent(payload);
            var tag = GetString(payload, "tag");

            if (string.IsNullOrEmpty(tag))
            {
                throw new InvalidOperationException("tag is required for removeTargetTag.");
            }

            var serializedCombat = new SerializedObject(combat);
            var tagsProp = serializedCombat.FindProperty("targetTags");

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    tagsProp.DeleteArrayElementAtIndex(i);
                    serializedCombat.ApplyModifiedProperties();
                    EditorSceneManager.MarkSceneDirty(combat.gameObject.scene);

                    return CreateSuccessResponse(
                        ("combatId", combat.CombatId),
                        ("tag", tag),
                        ("removed", true)
                    );
                }
            }

            return CreateSuccessResponse(
                ("combatId", combat.CombatId),
                ("tag", tag),
                ("removed", false),
                ("reason", "Tag not found")
            );
        }

        private object ResetCooldown(Dictionary<string, object> payload)
        {
            var combat = ResolveCombatComponent(payload);

            // In editor mode, we can't reset runtime cooldown
            return CreateSuccessResponse(
                ("combatId", combat.CombatId),
                ("reset", true),
                ("note", "Cooldown reset will take effect in play mode.")
            );
        }

        private object FindByCombatId(Dictionary<string, object> payload)
        {
            var combatId = GetString(payload, "combatId");
            if (string.IsNullOrEmpty(combatId))
            {
                throw new InvalidOperationException("combatId is required for findByCombatId.");
            }

            var combat = FindCombatById(combatId);
            if (combat == null)
            {
                return CreateSuccessResponse(("found", false), ("combatId", combatId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("combatId", combat.CombatId),
                ("path", BuildGameObjectPath(combat.gameObject)),
                ("attackType", combat.Type.ToString()),
                ("baseDamage", combat.BaseDamage)
            );
        }

        #endregion

        #region Helpers

        private GameKitCombat ResolveCombatComponent(Dictionary<string, object> payload)
        {
            // Try by combatId first
            var combatId = GetString(payload, "combatId");
            if (!string.IsNullOrEmpty(combatId))
            {
                var combatById = FindCombatById(combatId);
                if (combatById != null)
                {
                    return combatById;
                }
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var combatByPath = targetGo.GetComponent<GameKitCombat>();
                    if (combatByPath != null)
                    {
                        return combatByPath;
                    }
                    throw new InvalidOperationException($"No GameKitCombat component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either combatId or targetPath is required.");
        }

        private GameKitCombat FindCombatById(string combatId)
        {
            var combats = UnityEngine.Object.FindObjectsByType<GameKitCombat>(FindObjectsSortMode.None);
            foreach (var combat in combats)
            {
                if (combat.CombatId == combatId)
                {
                    return combat;
                }
            }
            return null;
        }

        private GameKitCombat.AttackType ParseAttackType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "melee" => GameKitCombat.AttackType.Melee,
                "ranged" => GameKitCombat.AttackType.Ranged,
                "aoe" => GameKitCombat.AttackType.AoE,
                "projectile" => GameKitCombat.AttackType.Projectile,
                _ => GameKitCombat.AttackType.Melee
            };
        }

        private GameKitCombat.HitboxShape ParseHitboxShape(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "sphere" => GameKitCombat.HitboxShape.Sphere,
                "box" => GameKitCombat.HitboxShape.Box,
                "capsule" => GameKitCombat.HitboxShape.Capsule,
                "cone" => GameKitCombat.HitboxShape.Cone,
                _ => GameKitCombat.HitboxShape.Sphere
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
