using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Status Effect Receiver: Component that receives and manages status effects.
    /// Handles buff/debuff application, stacking, duration, and stat modifications.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/Status Effect Receiver")]
    public class GameKitStatusEffectReceiver : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string receiverId;

        [Header("Links")]
        [SerializeField] private string healthId;
        [SerializeField] private GameKitHealth linkedHealth;

        [Header("Immunity")]
        [SerializeField] private List<string> immuneCategories = new List<string>();
        [SerializeField] private List<string> immuneEffects = new List<string>();

        [Header("Events")]
        public UnityEvent<string, int> OnEffectApplied = new UnityEvent<string, int>();
        public UnityEvent<string> OnEffectRemoved = new UnityEvent<string>();
        public UnityEvent<string> OnEffectExpired = new UnityEvent<string>();
        public UnityEvent<string, int> OnEffectStackChanged = new UnityEvent<string, int>();
        public UnityEvent<string, float> OnEffectTick = new UnityEvent<string, float>();

        // Active effects
        private List<ActiveEffect> activeEffects = new List<ActiveEffect>();

        // Stat modifiers applied by effects
        private Dictionary<string, float> statModifiers = new Dictionary<string, float>();

        // Registry
        private static readonly Dictionary<string, GameKitStatusEffectReceiver> _registry = new Dictionary<string, GameKitStatusEffectReceiver>();
        private static readonly Dictionary<string, GameKitStatusEffectAsset> _effectRegistry = new Dictionary<string, GameKitStatusEffectAsset>();

        // Properties
        public string ReceiverId => receiverId;
        public IReadOnlyList<ActiveEffect> ActiveEffects => activeEffects.AsReadOnly();
        public bool IsStunned => HasEffectOfType(GameKitStatusEffectAsset.ModifierType.Stun);
        public bool IsSilenced => HasEffectOfType(GameKitStatusEffectAsset.ModifierType.Silence);
        public bool IsInvincible => HasEffectOfType(GameKitStatusEffectAsset.ModifierType.Invincible);

        private void Awake()
        {
            EnsureEventsInitialized();

            // Try to find linked health
            if (linkedHealth == null && !string.IsNullOrEmpty(healthId))
            {
                linkedHealth = GameKitHealth.FindById(healthId);
            }
            if (linkedHealth == null)
            {
                linkedHealth = GetComponent<GameKitHealth>();
            }
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(receiverId))
            {
                _registry[receiverId] = this;
            }
        }

        private void OnDisable()
        {
            if (!string.IsNullOrEmpty(receiverId))
            {
                _registry.Remove(receiverId);
            }
        }

        private void Update()
        {
            UpdateEffects(Time.deltaTime);
        }

        private void EnsureEventsInitialized()
        {
            OnEffectApplied ??= new UnityEvent<string, int>();
            OnEffectRemoved ??= new UnityEvent<string>();
            OnEffectExpired ??= new UnityEvent<string>();
            OnEffectStackChanged ??= new UnityEvent<string, int>();
            OnEffectTick ??= new UnityEvent<string, float>();
        }

        /// <summary>
        /// Find receiver by ID.
        /// </summary>
        public static GameKitStatusEffectReceiver FindById(string id)
        {
            return _registry.TryGetValue(id, out var receiver) ? receiver : null;
        }

        /// <summary>
        /// Register an effect asset for runtime lookup.
        /// </summary>
        public static void RegisterEffect(GameKitStatusEffectAsset effect)
        {
            if (effect != null && !string.IsNullOrEmpty(effect.EffectId))
            {
                _effectRegistry[effect.EffectId] = effect;
            }
        }

        /// <summary>
        /// Find effect asset by ID.
        /// </summary>
        public static GameKitStatusEffectAsset FindEffectById(string effectId)
        {
            if (_effectRegistry.TryGetValue(effectId, out var effect))
                return effect;

            return Resources.Load<GameKitStatusEffectAsset>($"StatusEffects/{effectId}");
        }

        /// <summary>
        /// Initialize the receiver.
        /// </summary>
        public void Initialize(string id, string linkedHealthId = null)
        {
            receiverId = id;
            healthId = linkedHealthId;

            if (!string.IsNullOrEmpty(healthId))
            {
                linkedHealth = GameKitHealth.FindById(healthId);
            }

            if (!string.IsNullOrEmpty(receiverId))
            {
                _registry[receiverId] = this;
            }
        }

        #region Effect Management

        /// <summary>
        /// Apply an effect to this receiver.
        /// </summary>
        public bool ApplyEffect(string effectId, GameObject source = null)
        {
            var effectAsset = FindEffectById(effectId);
            if (effectAsset == null)
            {
                Debug.LogWarning($"Effect '{effectId}' not found.");
                return false;
            }

            return ApplyEffect(effectAsset, source);
        }

        /// <summary>
        /// Apply an effect to this receiver.
        /// </summary>
        public bool ApplyEffect(GameKitStatusEffectAsset effectAsset, GameObject source = null)
        {
            if (effectAsset == null) return false;

            // Check immunity
            if (IsImmuneToEffect(effectAsset))
            {
                return false;
            }

            // Remove effects that this effect removes
            foreach (var removeId in effectAsset.RemovesEffects)
            {
                RemoveEffect(removeId, true);
            }

            // Check for existing effect
            var existing = activeEffects.Find(e => e.EffectId == effectAsset.EffectId);

            if (existing != null)
            {
                // Handle stacking
                if (effectAsset.Stackable)
                {
                    switch (effectAsset.StackingBehavior)
                    {
                        case GameKitStatusEffectAsset.StackBehavior.RefreshDuration:
                            existing.RemainingDuration = effectAsset.Duration;
                            if (existing.Stacks < effectAsset.MaxStacks)
                            {
                                existing.Stacks++;
                                OnEffectStackChanged?.Invoke(effectAsset.EffectId, existing.Stacks);
                            }
                            break;

                        case GameKitStatusEffectAsset.StackBehavior.AddDuration:
                            existing.RemainingDuration += effectAsset.Duration;
                            if (existing.Stacks < effectAsset.MaxStacks)
                            {
                                existing.Stacks++;
                                OnEffectStackChanged?.Invoke(effectAsset.EffectId, existing.Stacks);
                            }
                            break;

                        case GameKitStatusEffectAsset.StackBehavior.IncreaseStacks:
                            if (existing.Stacks < effectAsset.MaxStacks)
                            {
                                existing.Stacks++;
                                existing.RemainingDuration = effectAsset.Duration;
                                OnEffectStackChanged?.Invoke(effectAsset.EffectId, existing.Stacks);
                            }
                            break;

                        case GameKitStatusEffectAsset.StackBehavior.Independent:
                            // Add as new effect
                            AddNewEffect(effectAsset, source);
                            break;
                    }
                }
                else
                {
                    // Just refresh duration
                    existing.RemainingDuration = effectAsset.Duration;
                }

                RecalculateStatModifiers();
                return true;
            }

            // Add new effect
            AddNewEffect(effectAsset, source);
            return true;
        }

        private void AddNewEffect(GameKitStatusEffectAsset effectAsset, GameObject source)
        {
            var active = new ActiveEffect
            {
                EffectId = effectAsset.EffectId,
                EffectAsset = effectAsset,
                Source = source,
                Stacks = 1,
                RemainingDuration = effectAsset.Duration,
                TickTimer = effectAsset.TickOnApply ? 0f : effectAsset.TickInterval,
                IsPermanent = effectAsset.IsPermanent
            };

            activeEffects.Add(active);
            RecalculateStatModifiers();

            // Play apply effect
            if (!string.IsNullOrEmpty(effectAsset.OnApplyEffectId) && GameKitEffectManager.Instance != null)
            {
                GameKitEffectManager.Instance.PlayEffect(effectAsset.OnApplyEffectId, transform.position);
            }

            // First tick if configured
            if (effectAsset.TickOnApply)
            {
                ProcessEffectTick(active);
            }

            OnEffectApplied?.Invoke(effectAsset.EffectId, active.Stacks);
        }

        /// <summary>
        /// Remove an effect from this receiver.
        /// </summary>
        public bool RemoveEffect(string effectId, bool allStacks = false)
        {
            var existing = activeEffects.Find(e => e.EffectId == effectId);
            if (existing == null) return false;

            if (allStacks || existing.Stacks <= 1)
            {
                // Remove completely
                activeEffects.Remove(existing);
                RecalculateStatModifiers();

                // Play remove effect
                if (!string.IsNullOrEmpty(existing.EffectAsset.OnRemoveEffectId) && GameKitEffectManager.Instance != null)
                {
                    GameKitEffectManager.Instance.PlayEffect(existing.EffectAsset.OnRemoveEffectId, transform.position);
                }

                OnEffectRemoved?.Invoke(effectId);
            }
            else
            {
                // Remove one stack
                existing.Stacks--;
                RecalculateStatModifiers();
                OnEffectStackChanged?.Invoke(effectId, existing.Stacks);
            }

            return true;
        }

        /// <summary>
        /// Remove all effects of a specific type.
        /// </summary>
        public int RemoveEffectsOfType(GameKitStatusEffectAsset.EffectType type)
        {
            var toRemove = activeEffects.Where(e => e.EffectAsset.Type == type).ToList();
            foreach (var effect in toRemove)
            {
                RemoveEffect(effect.EffectId, true);
            }
            return toRemove.Count;
        }

        /// <summary>
        /// Remove all effects of a specific category.
        /// </summary>
        public int RemoveEffectsOfCategory(GameKitStatusEffectAsset.EffectCategory category)
        {
            var toRemove = activeEffects.Where(e => e.EffectAsset.Category == category).ToList();
            foreach (var effect in toRemove)
            {
                RemoveEffect(effect.EffectId, true);
            }
            return toRemove.Count;
        }

        /// <summary>
        /// Clear all effects.
        /// </summary>
        public void ClearAllEffects()
        {
            var ids = activeEffects.Select(e => e.EffectId).ToList();
            activeEffects.Clear();
            RecalculateStatModifiers();

            foreach (var id in ids)
            {
                OnEffectRemoved?.Invoke(id);
            }
        }

        /// <summary>
        /// Check if has a specific effect.
        /// </summary>
        public bool HasEffect(string effectId)
        {
            return activeEffects.Any(e => e.EffectId == effectId);
        }

        /// <summary>
        /// Get stack count of an effect.
        /// </summary>
        public int GetStackCount(string effectId)
        {
            var effect = activeEffects.Find(e => e.EffectId == effectId);
            return effect?.Stacks ?? 0;
        }

        /// <summary>
        /// Get remaining duration of an effect.
        /// </summary>
        public float GetRemainingDuration(string effectId)
        {
            var effect = activeEffects.Find(e => e.EffectId == effectId);
            return effect?.RemainingDuration ?? 0f;
        }

        /// <summary>
        /// Get total stat modifier for a stat.
        /// </summary>
        public float GetStatModifier(string statName)
        {
            return statModifiers.TryGetValue(statName, out var value) ? value : 0f;
        }

        #endregion

        #region Internal Methods

        private void UpdateEffects(float deltaTime)
        {
            var expiredEffects = new List<ActiveEffect>();

            foreach (var effect in activeEffects)
            {
                // Update duration
                if (!effect.IsPermanent)
                {
                    effect.RemainingDuration -= deltaTime;
                    if (effect.RemainingDuration <= 0)
                    {
                        expiredEffects.Add(effect);
                        continue;
                    }
                }

                // Update tick timer
                effect.TickTimer -= deltaTime;
                if (effect.TickTimer <= 0)
                {
                    ProcessEffectTick(effect);
                    effect.TickTimer = effect.EffectAsset.TickInterval;
                }
            }

            // Remove expired effects
            foreach (var expired in expiredEffects)
            {
                activeEffects.Remove(expired);

                // Play remove effect
                if (!string.IsNullOrEmpty(expired.EffectAsset.OnRemoveEffectId) && GameKitEffectManager.Instance != null)
                {
                    GameKitEffectManager.Instance.PlayEffect(expired.EffectAsset.OnRemoveEffectId, transform.position);
                }

                OnEffectExpired?.Invoke(expired.EffectId);
            }

            if (expiredEffects.Count > 0)
            {
                RecalculateStatModifiers();
            }
        }

        private void ProcessEffectTick(ActiveEffect effect)
        {
            foreach (var modifier in effect.EffectAsset.Modifiers)
            {
                ProcessModifierTick(modifier, effect.Stacks);
            }

            // Play tick effect
            if (!string.IsNullOrEmpty(effect.EffectAsset.OnTickEffectId) && GameKitEffectManager.Instance != null)
            {
                GameKitEffectManager.Instance.PlayEffect(effect.EffectAsset.OnTickEffectId, transform.position);
            }

            OnEffectTick?.Invoke(effect.EffectId, effect.TickTimer);
        }

        private void ProcessModifierTick(GameKitStatusEffectAsset.EffectModifier modifier, int stacks)
        {
            float stackMultiplier = modifier.scaleWithStacks ? stacks : 1f;

            switch (modifier.type)
            {
                case GameKitStatusEffectAsset.ModifierType.DamageOverTime:
                    if (linkedHealth != null)
                    {
                        float damage = modifier.damagePerTick * stackMultiplier;
                        linkedHealth.TakeDamage(damage);
                    }
                    break;

                case GameKitStatusEffectAsset.ModifierType.HealOverTime:
                    if (linkedHealth != null)
                    {
                        float heal = modifier.healPerTick * stackMultiplier;
                        linkedHealth.Heal(heal);
                    }
                    break;
            }
        }

        private void RecalculateStatModifiers()
        {
            statModifiers.Clear();

            foreach (var effect in activeEffects)
            {
                foreach (var modifier in effect.EffectAsset.Modifiers)
                {
                    if (modifier.type == GameKitStatusEffectAsset.ModifierType.StatModifier)
                    {
                        float stackMultiplier = modifier.scaleWithStacks ? effect.Stacks : 1f;
                        float value = modifier.value * stackMultiplier;

                        string stat = modifier.targetStat;
                        if (string.IsNullOrEmpty(stat)) continue;

                        if (!statModifiers.ContainsKey(stat))
                        {
                            statModifiers[stat] = 0f;
                        }

                        switch (modifier.operation)
                        {
                            case GameKitStatusEffectAsset.ModifierOperation.Add:
                            case GameKitStatusEffectAsset.ModifierOperation.PercentAdd:
                                statModifiers[stat] += value;
                                break;
                            case GameKitStatusEffectAsset.ModifierOperation.Subtract:
                                statModifiers[stat] -= value;
                                break;
                            case GameKitStatusEffectAsset.ModifierOperation.Multiply:
                            case GameKitStatusEffectAsset.ModifierOperation.PercentMultiply:
                                statModifiers[stat] *= value;
                                break;
                        }
                    }
                }
            }
        }

        private bool IsImmuneToEffect(GameKitStatusEffectAsset effect)
        {
            // Check direct effect immunity
            if (immuneEffects.Contains(effect.EffectId))
                return true;

            // Check category immunity
            if (immuneCategories.Contains(effect.Category.ToString()))
                return true;

            // Check if any active effect grants immunity
            foreach (var active in activeEffects)
            {
                if (active.EffectAsset.ImmuneToEffects.Contains(effect.EffectId))
                    return true;
            }

            return false;
        }

        private bool HasEffectOfType(GameKitStatusEffectAsset.ModifierType modifierType)
        {
            foreach (var effect in activeEffects)
            {
                foreach (var modifier in effect.EffectAsset.Modifiers)
                {
                    if (modifier.type == modifierType)
                        return true;
                }
            }
            return false;
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Get save data for active effects.
        /// </summary>
        public StatusEffectSaveData GetSaveData()
        {
            return new StatusEffectSaveData
            {
                receiverId = receiverId,
                effects = activeEffects.Select(e => new EffectSaveData
                {
                    effectId = e.EffectId,
                    stacks = e.Stacks,
                    remainingDuration = e.RemainingDuration
                }).ToList()
            };
        }

        /// <summary>
        /// Load active effects from save data.
        /// </summary>
        public void LoadSaveData(StatusEffectSaveData data)
        {
            if (data == null) return;

            ClearAllEffects();

            foreach (var effectData in data.effects)
            {
                var asset = FindEffectById(effectData.effectId);
                if (asset != null)
                {
                    var active = new ActiveEffect
                    {
                        EffectId = effectData.effectId,
                        EffectAsset = asset,
                        Stacks = effectData.stacks,
                        RemainingDuration = effectData.remainingDuration,
                        TickTimer = asset.TickInterval,
                        IsPermanent = asset.IsPermanent
                    };
                    activeEffects.Add(active);
                }
            }

            RecalculateStatModifiers();
        }

        #endregion

        #region Types

        public class ActiveEffect
        {
            public string EffectId;
            public GameKitStatusEffectAsset EffectAsset;
            public GameObject Source;
            public int Stacks;
            public float RemainingDuration;
            public float TickTimer;
            public bool IsPermanent;
        }

        [Serializable]
        public class StatusEffectSaveData
        {
            public string receiverId;
            public List<EffectSaveData> effects = new List<EffectSaveData>();
        }

        [Serializable]
        public class EffectSaveData
        {
            public string effectId;
            public int stacks;
            public float remainingDuration;
        }

        #endregion
    }
}
