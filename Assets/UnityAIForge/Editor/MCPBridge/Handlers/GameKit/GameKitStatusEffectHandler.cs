using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCP.Editor.Base;
using UnityAIForge.GameKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Status Effect handler: create and manage status effect systems (buffs/debuffs).
    /// Provides declarative status effect creation without custom scripts.
    /// </summary>
    public class GameKitStatusEffectHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "defineEffect", "updateEffect", "inspectEffect", "deleteEffect",
            "addModifier", "updateModifier", "removeModifier", "clearModifiers",
            "create", "update", "inspect", "delete",
            "applyEffect", "removeEffect", "clearEffects",
            "getActiveEffects", "getStatModifier",
            "findByEffectId", "findByReceiverId", "listEffects"
        };

        public override string Category => "gamekitStatusEffect";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "defineEffect" => DefineEffect(payload),
                "updateEffect" => UpdateEffect(payload),
                "inspectEffect" => InspectEffect(payload),
                "deleteEffect" => DeleteEffect(payload),
                "addModifier" => AddModifier(payload),
                "updateModifier" => UpdateModifier(payload),
                "removeModifier" => RemoveModifier(payload),
                "clearModifiers" => ClearModifiers(payload),
                "create" => CreateReceiver(payload),
                "update" => UpdateReceiver(payload),
                "inspect" => InspectReceiver(payload),
                "delete" => DeleteReceiver(payload),
                "applyEffect" => ApplyEffect(payload),
                "removeEffect" => RemoveEffect(payload),
                "clearEffects" => ClearEffects(payload),
                "getActiveEffects" => GetActiveEffects(payload),
                "getStatModifier" => GetStatModifier(payload),
                "findByEffectId" => FindByEffectId(payload),
                "findByReceiverId" => FindByReceiverId(payload),
                "listEffects" => ListEffects(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit Status Effect operation: {operation}")
            };
        }

        #region Effect Asset CRUD

        private object DefineEffect(Dictionary<string, object> payload)
        {
            var effectId = GetString(payload, "effectId") ?? $"Effect_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var assetPath = GetString(payload, "assetPath") ?? $"Assets/StatusEffects/{effectId}.asset";

            // Ensure directory exists
            var directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Check if asset already exists
            if (AssetDatabase.LoadAssetAtPath<GameKitStatusEffectAsset>(assetPath) != null)
            {
                throw new InvalidOperationException($"Status effect asset already exists at: {assetPath}");
            }

            var displayName = GetString(payload, "displayName") ?? effectId;
            var effectType = payload.TryGetValue("type", out var typeObj)
                ? ParseEffectType(typeObj.ToString())
                : GameKitStatusEffectAsset.EffectType.Buff;

            // Create ScriptableObject
            var effect = ScriptableObject.CreateInstance<GameKitStatusEffectAsset>();
            effect.Initialize(effectId, displayName, effectType);

            // Set properties via SerializedObject
            var serializedEffect = new SerializedObject(effect);

            if (payload.TryGetValue("description", out var descObj))
            {
                serializedEffect.FindProperty("description").stringValue = descObj.ToString();
            }

            if (payload.TryGetValue("category", out var catObj))
            {
                var category = ParseEffectCategory(catObj.ToString());
                serializedEffect.FindProperty("category").enumValueIndex = (int)category;
            }

            if (payload.TryGetValue("duration", out var durObj))
            {
                serializedEffect.FindProperty("duration").floatValue = Convert.ToSingle(durObj);
            }

            if (payload.TryGetValue("isPermanent", out var permObj))
            {
                serializedEffect.FindProperty("isPermanent").boolValue = Convert.ToBoolean(permObj);
            }

            if (payload.TryGetValue("stackable", out var stackObj))
            {
                serializedEffect.FindProperty("stackable").boolValue = Convert.ToBoolean(stackObj);
            }

            if (payload.TryGetValue("maxStacks", out var maxStackObj))
            {
                serializedEffect.FindProperty("maxStacks").intValue = Convert.ToInt32(maxStackObj);
            }

            if (payload.TryGetValue("stackBehavior", out var stackBehaviorObj))
            {
                var behavior = ParseStackBehavior(stackBehaviorObj.ToString());
                serializedEffect.FindProperty("stackBehavior").enumValueIndex = (int)behavior;
            }

            if (payload.TryGetValue("tickInterval", out var tickObj))
            {
                serializedEffect.FindProperty("tickInterval").floatValue = Convert.ToSingle(tickObj);
            }

            if (payload.TryGetValue("tickOnApply", out var tickApplyObj))
            {
                serializedEffect.FindProperty("tickOnApply").boolValue = Convert.ToBoolean(tickApplyObj);
            }

            // Visual effects
            if (payload.TryGetValue("particleEffectId", out var particleObj))
            {
                serializedEffect.FindProperty("particleEffectId").stringValue = particleObj.ToString();
            }

            if (payload.TryGetValue("onApplyEffectId", out var applyEffectObj))
            {
                serializedEffect.FindProperty("onApplyEffectId").stringValue = applyEffectObj.ToString();
            }

            if (payload.TryGetValue("onRemoveEffectId", out var removeEffectObj))
            {
                serializedEffect.FindProperty("onRemoveEffectId").stringValue = removeEffectObj.ToString();
            }

            serializedEffect.ApplyModifiedPropertiesWithoutUndo();

            // Add modifiers if provided
            if (payload.TryGetValue("modifiers", out var modsObj) && modsObj is List<object> modsList)
            {
                foreach (var modObj in modsList)
                {
                    if (modObj is Dictionary<string, object> modDict)
                    {
                        var modifier = ParseModifier(modDict);
                        effect.AddModifier(modifier);
                    }
                }
            }

            // Save asset
            AssetDatabase.CreateAsset(effect, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("assetPath", assetPath),
                ("displayName", displayName),
                ("type", effectType.ToString()),
                ("modifierCount", effect.Modifiers.Count)
            );
        }

        private object UpdateEffect(Dictionary<string, object> payload)
        {
            var effect = ResolveEffectAsset(payload);
            var serializedEffect = new SerializedObject(effect);

            if (payload.TryGetValue("displayName", out var nameObj))
            {
                serializedEffect.FindProperty("displayName").stringValue = nameObj.ToString();
            }

            if (payload.TryGetValue("description", out var descObj))
            {
                serializedEffect.FindProperty("description").stringValue = descObj.ToString();
            }

            if (payload.TryGetValue("type", out var typeObj))
            {
                var type = ParseEffectType(typeObj.ToString());
                serializedEffect.FindProperty("type").enumValueIndex = (int)type;
            }

            if (payload.TryGetValue("category", out var catObj))
            {
                var category = ParseEffectCategory(catObj.ToString());
                serializedEffect.FindProperty("category").enumValueIndex = (int)category;
            }

            if (payload.TryGetValue("duration", out var durObj))
            {
                serializedEffect.FindProperty("duration").floatValue = Convert.ToSingle(durObj);
            }

            if (payload.TryGetValue("stackable", out var stackObj))
            {
                serializedEffect.FindProperty("stackable").boolValue = Convert.ToBoolean(stackObj);
            }

            if (payload.TryGetValue("maxStacks", out var maxStackObj))
            {
                serializedEffect.FindProperty("maxStacks").intValue = Convert.ToInt32(maxStackObj);
            }

            if (payload.TryGetValue("tickInterval", out var tickObj))
            {
                serializedEffect.FindProperty("tickInterval").floatValue = Convert.ToSingle(tickObj);
            }

            serializedEffect.ApplyModifiedProperties();
            EditorUtility.SetDirty(effect);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("effectId", effect.EffectId),
                ("updated", true)
            );
        }

        private object InspectEffect(Dictionary<string, object> payload)
        {
            var effect = ResolveEffectAsset(payload);

            var modifiers = new List<Dictionary<string, object>>();
            foreach (var mod in effect.Modifiers)
            {
                modifiers.Add(SerializeModifier(mod));
            }

            return CreateSuccessResponse(
                ("effectId", effect.EffectId),
                ("assetPath", AssetDatabase.GetAssetPath(effect)),
                ("displayName", effect.DisplayName),
                ("description", effect.Description),
                ("type", effect.Type.ToString()),
                ("category", effect.Category.ToString()),
                ("duration", effect.Duration),
                ("isPermanent", effect.IsPermanent),
                ("stackable", effect.Stackable),
                ("maxStacks", effect.MaxStacks),
                ("stackBehavior", effect.StackingBehavior.ToString()),
                ("tickInterval", effect.TickInterval),
                ("tickOnApply", effect.TickOnApply),
                ("modifierCount", effect.Modifiers.Count),
                ("modifiers", modifiers)
            );
        }

        private object DeleteEffect(Dictionary<string, object> payload)
        {
            var effect = ResolveEffectAsset(payload);
            var assetPath = AssetDatabase.GetAssetPath(effect);
            var effectId = effect.EffectId;

            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.Refresh();

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("assetPath", assetPath),
                ("deleted", true)
            );
        }

        #endregion

        #region Modifier Operations

        private object AddModifier(Dictionary<string, object> payload)
        {
            var effect = ResolveEffectAsset(payload);

            if (!payload.TryGetValue("modifier", out var modObj) || modObj is not Dictionary<string, object> modDict)
            {
                throw new InvalidOperationException("modifier data is required for addModifier operation.");
            }

            var modifier = ParseModifier(modDict);
            effect.AddModifier(modifier);

            EditorUtility.SetDirty(effect);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("effectId", effect.EffectId),
                ("modifierId", modifier.modifierId),
                ("modifierCount", effect.Modifiers.Count)
            );
        }

        private object UpdateModifier(Dictionary<string, object> payload)
        {
            var effect = ResolveEffectAsset(payload);
            var modifierId = GetString(payload, "modifierId");

            if (string.IsNullOrEmpty(modifierId))
            {
                throw new InvalidOperationException("modifierId is required for updateModifier operation.");
            }

            if (!payload.TryGetValue("modifier", out var modObj) || modObj is not Dictionary<string, object> modDict)
            {
                throw new InvalidOperationException("modifier data is required for updateModifier operation.");
            }

            // Remove old and add updated
            effect.RemoveModifier(modifierId);
            modDict["modifierId"] = modifierId;
            var updated = ParseModifier(modDict);
            effect.AddModifier(updated);

            EditorUtility.SetDirty(effect);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("effectId", effect.EffectId),
                ("modifierId", modifierId),
                ("updated", true)
            );
        }

        private object RemoveModifier(Dictionary<string, object> payload)
        {
            var effect = ResolveEffectAsset(payload);
            var modifierId = GetString(payload, "modifierId");

            if (string.IsNullOrEmpty(modifierId))
            {
                throw new InvalidOperationException("modifierId is required for removeModifier operation.");
            }

            var removed = effect.RemoveModifier(modifierId);
            if (!removed)
            {
                throw new InvalidOperationException($"Modifier '{modifierId}' not found in effect.");
            }

            EditorUtility.SetDirty(effect);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("effectId", effect.EffectId),
                ("modifierId", modifierId),
                ("removed", true),
                ("modifierCount", effect.Modifiers.Count)
            );
        }

        private object ClearModifiers(Dictionary<string, object> payload)
        {
            var effect = ResolveEffectAsset(payload);
            effect.ClearModifiers();

            EditorUtility.SetDirty(effect);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("effectId", effect.EffectId),
                ("cleared", true),
                ("modifierCount", 0)
            );
        }

        #endregion

        #region Receiver Operations

        private object CreateReceiver(Dictionary<string, object> payload)
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

            var existingReceiver = targetGo.GetComponent<GameKitStatusEffectReceiver>();
            if (existingReceiver != null)
            {
                throw new InvalidOperationException($"GameObject '{targetPath}' already has a GameKitStatusEffectReceiver component.");
            }

            var receiverId = GetString(payload, "receiverId") ?? $"Receiver_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var healthId = GetString(payload, "healthId");

            var receiver = Undo.AddComponent<GameKitStatusEffectReceiver>(targetGo);
            receiver.Initialize(receiverId, healthId);

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("receiverId", receiverId),
                ("path", BuildGameObjectPath(targetGo)),
                ("created", true)
            );
        }

        private object UpdateReceiver(Dictionary<string, object> payload)
        {
            var receiver = ResolveReceiver(payload);
            var serializedReceiver = new SerializedObject(receiver);

            if (payload.TryGetValue("healthId", out var healthIdObj))
            {
                serializedReceiver.FindProperty("healthId").stringValue = healthIdObj.ToString();
            }

            if (payload.TryGetValue("immuneCategories", out var immuneCatObj) && immuneCatObj is List<object> catList)
            {
                var prop = serializedReceiver.FindProperty("immuneCategories");
                prop.ClearArray();
                foreach (var cat in catList)
                {
                    prop.InsertArrayElementAtIndex(prop.arraySize);
                    prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = cat.ToString();
                }
            }

            if (payload.TryGetValue("immuneEffects", out var immuneEffObj) && immuneEffObj is List<object> effList)
            {
                var prop = serializedReceiver.FindProperty("immuneEffects");
                prop.ClearArray();
                foreach (var eff in effList)
                {
                    prop.InsertArrayElementAtIndex(prop.arraySize);
                    prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = eff.ToString();
                }
            }

            serializedReceiver.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(receiver.gameObject.scene);

            return CreateSuccessResponse(
                ("receiverId", receiver.ReceiverId),
                ("path", BuildGameObjectPath(receiver.gameObject)),
                ("updated", true)
            );
        }

        private object InspectReceiver(Dictionary<string, object> payload)
        {
            var receiver = ResolveReceiver(payload);

            var activeEffects = new List<Dictionary<string, object>>();
            foreach (var active in receiver.ActiveEffects)
            {
                activeEffects.Add(new Dictionary<string, object>
                {
                    { "effectId", active.EffectId },
                    { "stacks", active.Stacks },
                    { "remainingDuration", active.RemainingDuration },
                    { "isPermanent", active.IsPermanent }
                });
            }

            return CreateSuccessResponse(
                ("receiverId", receiver.ReceiverId),
                ("path", BuildGameObjectPath(receiver.gameObject)),
                ("activeEffectCount", receiver.ActiveEffects.Count),
                ("activeEffects", activeEffects),
                ("isStunned", receiver.IsStunned),
                ("isSilenced", receiver.IsSilenced),
                ("isInvincible", receiver.IsInvincible)
            );
        }

        private object DeleteReceiver(Dictionary<string, object> payload)
        {
            var receiver = ResolveReceiver(payload);
            var path = BuildGameObjectPath(receiver.gameObject);
            var receiverId = receiver.ReceiverId;
            var scene = receiver.gameObject.scene;

            Undo.DestroyObjectImmediate(receiver);
            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("receiverId", receiverId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Runtime Operations

        private object ApplyEffect(Dictionary<string, object> payload)
        {
            var effectId = GetString(payload, "effectId");
            if (string.IsNullOrEmpty(effectId))
            {
                throw new InvalidOperationException("effectId is required for applyEffect operation.");
            }

            // Register the effect for runtime use
            var effect = ResolveEffectAsset(payload);
            GameKitStatusEffectReceiver.RegisterEffect(effect);

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("registered", true),
                ("note", "Effect registered. Apply effect in play mode with GameKitStatusEffectReceiver.ApplyEffect().")
            );
        }

        private object RemoveEffect(Dictionary<string, object> payload)
        {
            var effectId = GetString(payload, "effectId");
            return CreateSuccessResponse(
                ("effectId", effectId),
                ("note", "Effect removal only works in play mode.")
            );
        }

        private object ClearEffects(Dictionary<string, object> payload)
        {
            return CreateSuccessResponse(
                ("note", "Effect clearing only works in play mode.")
            );
        }

        private object GetActiveEffects(Dictionary<string, object> payload)
        {
            var receiver = ResolveReceiver(payload);

            var effects = new List<Dictionary<string, object>>();
            foreach (var active in receiver.ActiveEffects)
            {
                effects.Add(new Dictionary<string, object>
                {
                    { "effectId", active.EffectId },
                    { "stacks", active.Stacks },
                    { "remainingDuration", active.RemainingDuration }
                });
            }

            return CreateSuccessResponse(
                ("receiverId", receiver.ReceiverId),
                ("activeEffects", effects),
                ("count", effects.Count)
            );
        }

        private object GetStatModifier(Dictionary<string, object> payload)
        {
            var receiver = ResolveReceiver(payload);
            var statName = GetString(payload, "statName");

            if (string.IsNullOrEmpty(statName))
            {
                throw new InvalidOperationException("statName is required for getStatModifier operation.");
            }

            var value = receiver.GetStatModifier(statName);

            return CreateSuccessResponse(
                ("receiverId", receiver.ReceiverId),
                ("statName", statName),
                ("modifier", value)
            );
        }

        #endregion

        #region Find Operations

        private object FindByEffectId(Dictionary<string, object> payload)
        {
            var effectId = GetString(payload, "effectId");
            if (string.IsNullOrEmpty(effectId))
            {
                throw new InvalidOperationException("effectId is required for findByEffectId.");
            }

            var guids = AssetDatabase.FindAssets("t:GameKitStatusEffectAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var effect = AssetDatabase.LoadAssetAtPath<GameKitStatusEffectAsset>(path);
                if (effect != null && effect.EffectId == effectId)
                {
                    return CreateSuccessResponse(
                        ("found", true),
                        ("effectId", effect.EffectId),
                        ("assetPath", path),
                        ("displayName", effect.DisplayName),
                        ("type", effect.Type.ToString())
                    );
                }
            }

            return CreateSuccessResponse(("found", false), ("effectId", effectId));
        }

        private object FindByReceiverId(Dictionary<string, object> payload)
        {
            var receiverId = GetString(payload, "receiverId");
            if (string.IsNullOrEmpty(receiverId))
            {
                throw new InvalidOperationException("receiverId is required for findByReceiverId.");
            }

            var receivers = UnityEngine.Object.FindObjectsByType<GameKitStatusEffectReceiver>(FindObjectsSortMode.None);
            foreach (var receiver in receivers)
            {
                if (receiver.ReceiverId == receiverId)
                {
                    return CreateSuccessResponse(
                        ("found", true),
                        ("receiverId", receiver.ReceiverId),
                        ("path", BuildGameObjectPath(receiver.gameObject)),
                        ("activeEffectCount", receiver.ActiveEffects.Count)
                    );
                }
            }

            return CreateSuccessResponse(("found", false), ("receiverId", receiverId));
        }

        private object ListEffects(Dictionary<string, object> payload)
        {
            var guids = AssetDatabase.FindAssets("t:GameKitStatusEffectAsset");

            var effects = new List<Dictionary<string, object>>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var effect = AssetDatabase.LoadAssetAtPath<GameKitStatusEffectAsset>(path);
                if (effect != null)
                {
                    effects.Add(new Dictionary<string, object>
                    {
                        { "effectId", effect.EffectId },
                        { "displayName", effect.DisplayName },
                        { "type", effect.Type.ToString() },
                        { "category", effect.Category.ToString() },
                        { "assetPath", path }
                    });
                }
            }

            return CreateSuccessResponse(
                ("count", effects.Count),
                ("effects", effects)
            );
        }

        #endregion

        #region Helpers

        private GameKitStatusEffectAsset ResolveEffectAsset(Dictionary<string, object> payload)
        {
            var effectId = GetString(payload, "effectId");
            var assetPath = GetString(payload, "assetPath");

            if (!string.IsNullOrEmpty(assetPath))
            {
                var effect = AssetDatabase.LoadAssetAtPath<GameKitStatusEffectAsset>(assetPath);
                if (effect != null) return effect;
            }

            if (!string.IsNullOrEmpty(effectId))
            {
                var guids = AssetDatabase.FindAssets("t:GameKitStatusEffectAsset");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var effect = AssetDatabase.LoadAssetAtPath<GameKitStatusEffectAsset>(path);
                    if (effect != null && effect.EffectId == effectId)
                    {
                        return effect;
                    }
                }
            }

            throw new InvalidOperationException("Either assetPath or effectId is required to resolve effect asset.");
        }

        private GameKitStatusEffectReceiver ResolveReceiver(Dictionary<string, object> payload)
        {
            var receiverId = GetString(payload, "receiverId");
            var targetPath = GetString(payload, "targetPath");

            if (!string.IsNullOrEmpty(receiverId))
            {
                var receivers = UnityEngine.Object.FindObjectsByType<GameKitStatusEffectReceiver>(FindObjectsSortMode.None);
                foreach (var receiver in receivers)
                {
                    if (receiver.ReceiverId == receiverId)
                    {
                        return receiver;
                    }
                }
            }

            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var receiver = targetGo.GetComponent<GameKitStatusEffectReceiver>();
                    if (receiver != null)
                    {
                        return receiver;
                    }
                    throw new InvalidOperationException($"No GameKitStatusEffectReceiver component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either receiverId or targetPath is required.");
        }

        private GameKitStatusEffectAsset.EffectModifier ParseModifier(Dictionary<string, object> dict)
        {
            return new GameKitStatusEffectAsset.EffectModifier
            {
                modifierId = dict.TryGetValue("modifierId", out var id) ? id?.ToString() : $"mod_{Guid.NewGuid().ToString().Substring(0, 8)}",
                type = dict.TryGetValue("type", out var type) ? ParseModifierType(type.ToString()) : GameKitStatusEffectAsset.ModifierType.StatModifier,
                targetHealthId = dict.TryGetValue("targetHealthId", out var healthId) ? healthId?.ToString() : "",
                targetStat = dict.TryGetValue("targetStat", out var stat) ? stat?.ToString() : "",
                value = dict.TryGetValue("value", out var val) ? Convert.ToSingle(val) : 0f,
                operation = dict.TryGetValue("operation", out var op) ? ParseModifierOperation(op.ToString()) : GameKitStatusEffectAsset.ModifierOperation.Add,
                scaleWithStacks = dict.TryGetValue("scaleWithStacks", out var scale) && Convert.ToBoolean(scale),
                damagePerTick = dict.TryGetValue("damagePerTick", out var dmg) ? Convert.ToSingle(dmg) : 0f,
                healPerTick = dict.TryGetValue("healPerTick", out var heal) ? Convert.ToSingle(heal) : 0f,
                damageType = dict.TryGetValue("damageType", out var dmgType) ? ParseDamageType(dmgType.ToString()) : GameKitStatusEffectAsset.DamageType.Physical
            };
        }

        private Dictionary<string, object> SerializeModifier(GameKitStatusEffectAsset.EffectModifier mod)
        {
            return new Dictionary<string, object>
            {
                { "modifierId", mod.modifierId },
                { "type", mod.type.ToString() },
                { "targetStat", mod.targetStat },
                { "value", mod.value },
                { "operation", mod.operation.ToString() },
                { "scaleWithStacks", mod.scaleWithStacks },
                { "damagePerTick", mod.damagePerTick },
                { "healPerTick", mod.healPerTick },
                { "damageType", mod.damageType.ToString() }
            };
        }

        private GameKitStatusEffectAsset.EffectType ParseEffectType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "buff" => GameKitStatusEffectAsset.EffectType.Buff,
                "debuff" => GameKitStatusEffectAsset.EffectType.Debuff,
                "neutral" => GameKitStatusEffectAsset.EffectType.Neutral,
                _ => GameKitStatusEffectAsset.EffectType.Buff
            };
        }

        private GameKitStatusEffectAsset.EffectCategory ParseEffectCategory(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "generic" => GameKitStatusEffectAsset.EffectCategory.Generic,
                "poison" => GameKitStatusEffectAsset.EffectCategory.Poison,
                "burn" => GameKitStatusEffectAsset.EffectCategory.Burn,
                "freeze" => GameKitStatusEffectAsset.EffectCategory.Freeze,
                "stun" => GameKitStatusEffectAsset.EffectCategory.Stun,
                "slow" => GameKitStatusEffectAsset.EffectCategory.Slow,
                "haste" => GameKitStatusEffectAsset.EffectCategory.Haste,
                "shield" => GameKitStatusEffectAsset.EffectCategory.Shield,
                "regeneration" => GameKitStatusEffectAsset.EffectCategory.Regeneration,
                "invincibility" => GameKitStatusEffectAsset.EffectCategory.Invincibility,
                "weakness" => GameKitStatusEffectAsset.EffectCategory.Weakness,
                "strength" => GameKitStatusEffectAsset.EffectCategory.Strength,
                "custom" => GameKitStatusEffectAsset.EffectCategory.Custom,
                _ => GameKitStatusEffectAsset.EffectCategory.Generic
            };
        }

        private GameKitStatusEffectAsset.StackBehavior ParseStackBehavior(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "refreshduration" or "refresh" => GameKitStatusEffectAsset.StackBehavior.RefreshDuration,
                "addduration" or "add" => GameKitStatusEffectAsset.StackBehavior.AddDuration,
                "independent" => GameKitStatusEffectAsset.StackBehavior.Independent,
                "increasestacks" or "increase" => GameKitStatusEffectAsset.StackBehavior.IncreaseStacks,
                _ => GameKitStatusEffectAsset.StackBehavior.RefreshDuration
            };
        }

        private GameKitStatusEffectAsset.ModifierType ParseModifierType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "statmodifier" or "stat" => GameKitStatusEffectAsset.ModifierType.StatModifier,
                "damageovertime" or "dot" => GameKitStatusEffectAsset.ModifierType.DamageOverTime,
                "healovertime" or "hot" => GameKitStatusEffectAsset.ModifierType.HealOverTime,
                "stun" => GameKitStatusEffectAsset.ModifierType.Stun,
                "silence" => GameKitStatusEffectAsset.ModifierType.Silence,
                "invincible" => GameKitStatusEffectAsset.ModifierType.Invincible,
                "custom" => GameKitStatusEffectAsset.ModifierType.Custom,
                _ => GameKitStatusEffectAsset.ModifierType.StatModifier
            };
        }

        private GameKitStatusEffectAsset.ModifierOperation ParseModifierOperation(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "add" => GameKitStatusEffectAsset.ModifierOperation.Add,
                "subtract" => GameKitStatusEffectAsset.ModifierOperation.Subtract,
                "multiply" => GameKitStatusEffectAsset.ModifierOperation.Multiply,
                "divide" => GameKitStatusEffectAsset.ModifierOperation.Divide,
                "set" => GameKitStatusEffectAsset.ModifierOperation.Set,
                "percentadd" or "percent" => GameKitStatusEffectAsset.ModifierOperation.PercentAdd,
                "percentmultiply" => GameKitStatusEffectAsset.ModifierOperation.PercentMultiply,
                _ => GameKitStatusEffectAsset.ModifierOperation.Add
            };
        }

        private GameKitStatusEffectAsset.DamageType ParseDamageType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "physical" => GameKitStatusEffectAsset.DamageType.Physical,
                "magic" => GameKitStatusEffectAsset.DamageType.Magic,
                "fire" => GameKitStatusEffectAsset.DamageType.Fire,
                "ice" => GameKitStatusEffectAsset.DamageType.Ice,
                "lightning" => GameKitStatusEffectAsset.DamageType.Lightning,
                "poison" => GameKitStatusEffectAsset.DamageType.Poison,
                "true" => GameKitStatusEffectAsset.DamageType.True,
                _ => GameKitStatusEffectAsset.DamageType.Physical
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
