using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Status Effect Asset: ScriptableObject that defines status effects (buffs/debuffs).
    /// Supports damage over time, stat modifiers, and visual effects.
    /// </summary>
    [CreateAssetMenu(fileName = "StatusEffect", menuName = "GameKit/StatusEffect")]
    public class GameKitStatusEffectAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string effectId;
        [SerializeField] private string displayName;
        [TextArea(2, 5)]
        [SerializeField] private string description;

        [Header("Appearance")]
        [SerializeField] private Sprite icon;
        [SerializeField] private Color tintColor = Color.white;

        [Header("Type")]
        [SerializeField] private EffectType type = EffectType.Buff;
        [SerializeField] private EffectCategory category = EffectCategory.Generic;

        [Header("Duration")]
        [SerializeField] private float duration = 10f;
        [SerializeField] private bool isPermanent = false;

        [Header("Stacking")]
        [SerializeField] private bool stackable = false;
        [SerializeField] private int maxStacks = 1;
        [SerializeField] private StackBehavior stackBehavior = StackBehavior.RefreshDuration;

        [Header("Tick")]
        [SerializeField] private float tickInterval = 1f;
        [SerializeField] private bool tickOnApply = false;

        [Header("Effects")]
        [SerializeField] private List<EffectModifier> modifiers = new List<EffectModifier>();

        [Header("Visual Effects")]
        [SerializeField] private string particleEffectId;
        [SerializeField] private string onApplyEffectId;
        [SerializeField] private string onRemoveEffectId;
        [SerializeField] private string onTickEffectId;

        [Header("Audio")]
        [SerializeField] private string onApplySoundPath;
        [SerializeField] private string onRemoveSoundPath;
        [SerializeField] private string onTickSoundPath;

        [Header("Immunity")]
        [SerializeField] private List<string> immuneToEffects = new List<string>();
        [SerializeField] private List<string> removesEffects = new List<string>();

        // Properties
        public string EffectId => effectId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public Color TintColor => tintColor;
        public EffectType Type => type;
        public EffectCategory Category => category;
        public float Duration => duration;
        public bool IsPermanent => isPermanent;
        public bool Stackable => stackable;
        public int MaxStacks => maxStacks;
        public StackBehavior StackingBehavior => stackBehavior;
        public float TickInterval => tickInterval;
        public bool TickOnApply => tickOnApply;
        public IReadOnlyList<EffectModifier> Modifiers => modifiers.AsReadOnly();
        public string ParticleEffectId => particleEffectId;
        public string OnApplyEffectId => onApplyEffectId;
        public string OnRemoveEffectId => onRemoveEffectId;
        public string OnTickEffectId => onTickEffectId;
        public IReadOnlyList<string> ImmuneToEffects => immuneToEffects.AsReadOnly();
        public IReadOnlyList<string> RemovesEffects => removesEffects.AsReadOnly();

        /// <summary>
        /// Initialize the effect asset.
        /// </summary>
        public void Initialize(string id, string name, EffectType effectType)
        {
            effectId = id;
            displayName = name;
            type = effectType;
        }

        /// <summary>
        /// Add a modifier.
        /// </summary>
        public void AddModifier(EffectModifier modifier)
        {
            if (modifier != null)
            {
                modifiers.Add(modifier);
            }
        }

        /// <summary>
        /// Remove a modifier.
        /// </summary>
        public bool RemoveModifier(string modifierId)
        {
            return modifiers.RemoveAll(m => m.modifierId == modifierId) > 0;
        }

        /// <summary>
        /// Clear all modifiers.
        /// </summary>
        public void ClearModifiers()
        {
            modifiers.Clear();
        }

        #region Serializable Types

        [Serializable]
        public class EffectModifier
        {
            public string modifierId;
            public ModifierType type;

            [Header("Target")]
            public string targetHealthId;
            public string targetStat;

            [Header("Values")]
            public float value;
            public ModifierOperation operation = ModifierOperation.Add;
            public bool scaleWithStacks = false;

            [Header("DoT/HoT")]
            public float damagePerTick;
            public float healPerTick;
            public DamageType damageType = DamageType.Physical;
        }

        public enum EffectType
        {
            Buff,
            Debuff,
            Neutral
        }

        public enum EffectCategory
        {
            Generic,
            Poison,
            Burn,
            Freeze,
            Stun,
            Slow,
            Haste,
            Shield,
            Regeneration,
            Invincibility,
            Weakness,
            Strength,
            Custom
        }

        public enum StackBehavior
        {
            RefreshDuration,
            AddDuration,
            Independent,
            IncreaseStacks
        }

        public enum ModifierType
        {
            StatModifier,
            DamageOverTime,
            HealOverTime,
            Stun,
            Silence,
            Invincible,
            Custom
        }

        public enum ModifierOperation
        {
            Add,
            Subtract,
            Multiply,
            Divide,
            Set,
            PercentAdd,
            PercentMultiply
        }

        public enum DamageType
        {
            Physical,
            Magic,
            Fire,
            Ice,
            Lightning,
            Poison,
            True
        }

        #endregion
    }
}
