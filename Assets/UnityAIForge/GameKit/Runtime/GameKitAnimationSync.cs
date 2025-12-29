using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Animation Sync: declarative animation synchronization with game state.
    /// Synchronizes Animator parameters with Rigidbody, Transform, and custom properties.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/Animation Sync")]
    public class GameKitAnimationSync : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string syncId;

        [Header("Animator Reference")]
        [SerializeField] private Animator animator;
        [SerializeField] private bool autoFindAnimator = true;

        [Header("Sync Rules")]
        [SerializeField] private List<AnimSyncRule> syncRules = new List<AnimSyncRule>();

        [Header("Trigger Rules")]
        [SerializeField] private List<AnimTriggerRule> triggerRules = new List<AnimTriggerRule>();

        [Header("Events")]
        [Tooltip("Invoked when a sync rule updates a parameter")]
        public UnityEvent<string, float> OnParameterSynced = new UnityEvent<string, float>();

        [Tooltip("Invoked when a trigger is fired")]
        public UnityEvent<string> OnTriggerFired = new UnityEvent<string>();

        // Cached references
        private Rigidbody rb;
        private Rigidbody2D rb2D;
        private Dictionary<string, GameKitHealth> healthComponents = new Dictionary<string, GameKitHealth>();

        // Properties
        public string SyncId => syncId;
        public Animator AnimatorReference => animator;
        public IReadOnlyList<AnimSyncRule> SyncRules => syncRules.AsReadOnly();
        public IReadOnlyList<AnimTriggerRule> TriggerRules => triggerRules.AsReadOnly();

        private void Awake()
        {
            EnsureEventsInitialized();

            if (autoFindAnimator && animator == null)
            {
                animator = GetComponent<Animator>();
            }

            CacheComponents();
        }

        private void OnEnable()
        {
            SubscribeToHealthEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromHealthEvents();
        }

        private void Update()
        {
            if (animator == null) return;

            ProcessSyncRules();
        }

        /// <summary>
        /// Initialize the animation sync component.
        /// </summary>
        public void Initialize(string id, Animator targetAnimator = null)
        {
            syncId = id;
            if (targetAnimator != null)
            {
                animator = targetAnimator;
            }
            else if (autoFindAnimator && animator == null)
            {
                animator = GetComponent<Animator>();
            }

            CacheComponents();
            EnsureEventsInitialized();
        }

        /// <summary>
        /// Add a sync rule to synchronize an animator parameter with a source.
        /// </summary>
        public void AddSyncRule(AnimSyncRule rule)
        {
            if (rule == null) return;
            syncRules.Add(rule);
        }

        /// <summary>
        /// Remove a sync rule by parameter name.
        /// </summary>
        public bool RemoveSyncRule(string parameterName)
        {
            return syncRules.RemoveAll(r => r.parameterName == parameterName) > 0;
        }

        /// <summary>
        /// Add a trigger rule to fire animator triggers based on events.
        /// </summary>
        public void AddTriggerRule(AnimTriggerRule rule)
        {
            if (rule == null) return;
            triggerRules.Add(rule);

            // Subscribe to health events if needed
            if (rule.eventSource == TriggerEventSource.Health && !string.IsNullOrEmpty(rule.healthId))
            {
                SubscribeToHealthComponent(rule.healthId);
            }
        }

        /// <summary>
        /// Remove a trigger rule by trigger name.
        /// </summary>
        public bool RemoveTriggerRule(string triggerName)
        {
            return triggerRules.RemoveAll(r => r.triggerName == triggerName) > 0;
        }

        /// <summary>
        /// Manually fire a trigger by name.
        /// </summary>
        public void FireTrigger(string triggerName)
        {
            if (animator == null || string.IsNullOrEmpty(triggerName)) return;

            animator.SetTrigger(triggerName);
            OnTriggerFired?.Invoke(triggerName);
        }

        /// <summary>
        /// Set an animator parameter directly.
        /// </summary>
        public void SetParameter(string parameterName, float value)
        {
            if (animator == null || string.IsNullOrEmpty(parameterName)) return;
            animator.SetFloat(parameterName, value);
        }

        /// <summary>
        /// Set an animator bool parameter directly.
        /// </summary>
        public void SetParameterBool(string parameterName, bool value)
        {
            if (animator == null || string.IsNullOrEmpty(parameterName)) return;
            animator.SetBool(parameterName, value);
        }

        /// <summary>
        /// Set an animator integer parameter directly.
        /// </summary>
        public void SetParameterInt(string parameterName, int value)
        {
            if (animator == null || string.IsNullOrEmpty(parameterName)) return;
            animator.SetInteger(parameterName, value);
        }

        private void CacheComponents()
        {
            rb = GetComponent<Rigidbody>();
            rb2D = GetComponent<Rigidbody2D>();
        }

        private void EnsureEventsInitialized()
        {
            OnParameterSynced ??= new UnityEvent<string, float>();
            OnTriggerFired ??= new UnityEvent<string>();
        }

        private void ProcessSyncRules()
        {
            foreach (var rule in syncRules)
            {
                if (rule == null || string.IsNullOrEmpty(rule.parameterName)) continue;

                float value = GetSourceValue(rule);
                value *= rule.multiplier;

                // Apply value based on parameter type
                switch (rule.parameterType)
                {
                    case AnimParameterType.Float:
                        animator.SetFloat(rule.parameterName, value);
                        OnParameterSynced?.Invoke(rule.parameterName, value);
                        break;

                    case AnimParameterType.Int:
                        animator.SetInteger(rule.parameterName, Mathf.RoundToInt(value));
                        OnParameterSynced?.Invoke(rule.parameterName, value);
                        break;

                    case AnimParameterType.Bool:
                        bool boolValue = value > rule.boolThreshold;
                        animator.SetBool(rule.parameterName, boolValue);
                        OnParameterSynced?.Invoke(rule.parameterName, boolValue ? 1f : 0f);
                        break;
                }
            }
        }

        private float GetSourceValue(AnimSyncRule rule)
        {
            switch (rule.sourceType)
            {
                case SyncSourceType.Rigidbody3D:
                    return GetRigidbody3DValue(rule.sourceProperty);

                case SyncSourceType.Rigidbody2D:
                    return GetRigidbody2DValue(rule.sourceProperty);

                case SyncSourceType.Transform:
                    return GetTransformValue(rule.sourceProperty);

                case SyncSourceType.Health:
                    return GetHealthValue(rule.healthId, rule.sourceProperty);

                case SyncSourceType.Custom:
                    // For custom, user can set value manually via SetParameter
                    return 0f;

                default:
                    return 0f;
            }
        }

        private float GetRigidbody3DValue(string property)
        {
            if (rb == null) return 0f;

            return property?.ToLowerInvariant() switch
            {
                "velocity.magnitude" or "speed" => rb.linearVelocity.magnitude,
                "velocity.x" => rb.linearVelocity.x,
                "velocity.y" => rb.linearVelocity.y,
                "velocity.z" => rb.linearVelocity.z,
                "angularvelocity.magnitude" => rb.angularVelocity.magnitude,
                "iskinematic" => rb.isKinematic ? 1f : 0f,
                _ => 0f
            };
        }

        private float GetRigidbody2DValue(string property)
        {
            if (rb2D == null) return 0f;

            return property?.ToLowerInvariant() switch
            {
                "velocity.magnitude" or "speed" => rb2D.linearVelocity.magnitude,
                "velocity.x" => rb2D.linearVelocity.x,
                "velocity.y" => rb2D.linearVelocity.y,
                "angularvelocity" => rb2D.angularVelocity,
                "iskinematic" => rb2D.isKinematic ? 1f : 0f,
                _ => 0f
            };
        }

        private float GetTransformValue(string property)
        {
            return property?.ToLowerInvariant() switch
            {
                "position.x" => transform.position.x,
                "position.y" => transform.position.y,
                "position.z" => transform.position.z,
                "localposition.x" => transform.localPosition.x,
                "localposition.y" => transform.localPosition.y,
                "localposition.z" => transform.localPosition.z,
                "rotation.x" => transform.eulerAngles.x,
                "rotation.y" => transform.eulerAngles.y,
                "rotation.z" => transform.eulerAngles.z,
                "localscale.x" => transform.localScale.x,
                "localscale.y" => transform.localScale.y,
                "localscale.z" => transform.localScale.z,
                _ => 0f
            };
        }

        private float GetHealthValue(string healthId, string property)
        {
            if (string.IsNullOrEmpty(healthId)) return 0f;

            if (!healthComponents.TryGetValue(healthId, out var health))
            {
                health = FindHealthById(healthId);
                if (health != null)
                {
                    healthComponents[healthId] = health;
                }
            }

            if (health == null) return 0f;

            return property?.ToLowerInvariant() switch
            {
                "currenthealth" or "current" => health.CurrentHealth,
                "maxhealth" or "max" => health.MaxHealth,
                "healthpercent" or "percent" => health.HealthPercent,
                "isalive" => health.IsAlive ? 1f : 0f,
                "isdead" => health.IsDead ? 1f : 0f,
                "isinvincible" => health.IsInvincible ? 1f : 0f,
                _ => 0f
            };
        }

        private GameKitHealth FindHealthById(string healthId)
        {
            var healths = FindObjectsByType<GameKitHealth>(FindObjectsSortMode.None);
            foreach (var health in healths)
            {
                if (health.HealthId == healthId)
                {
                    return health;
                }
            }
            return null;
        }

        private void SubscribeToHealthEvents()
        {
            foreach (var rule in triggerRules)
            {
                if (rule.eventSource == TriggerEventSource.Health && !string.IsNullOrEmpty(rule.healthId))
                {
                    SubscribeToHealthComponent(rule.healthId);
                }
            }
        }

        private void SubscribeToHealthComponent(string healthId)
        {
            if (healthComponents.ContainsKey(healthId)) return;

            var health = FindHealthById(healthId);
            if (health == null) return;

            healthComponents[healthId] = health;

            // Subscribe to events based on trigger rules
            foreach (var rule in triggerRules)
            {
                if (rule.healthId != healthId) continue;

                switch (rule.healthEvent)
                {
                    case HealthEventType.OnDamaged:
                        health.OnDamaged.AddListener(_ => FireTrigger(rule.triggerName));
                        break;
                    case HealthEventType.OnHealed:
                        health.OnHealed.AddListener(_ => FireTrigger(rule.triggerName));
                        break;
                    case HealthEventType.OnDeath:
                        health.OnDeath.AddListener(() => FireTrigger(rule.triggerName));
                        break;
                    case HealthEventType.OnRespawn:
                        health.OnRespawn.AddListener(() => FireTrigger(rule.triggerName));
                        break;
                    case HealthEventType.OnInvincibilityStart:
                        health.OnInvincibilityStart.AddListener(() => FireTrigger(rule.triggerName));
                        break;
                    case HealthEventType.OnInvincibilityEnd:
                        health.OnInvincibilityEnd.AddListener(() => FireTrigger(rule.triggerName));
                        break;
                }
            }
        }

        private void UnsubscribeFromHealthEvents()
        {
            // Note: UnityEvents don't easily support individual listener removal
            // In a production system, you'd want to track listeners more carefully
            healthComponents.Clear();
        }

        #region Serializable Types

        [Serializable]
        public class AnimSyncRule
        {
            [Tooltip("Animator parameter name to sync")]
            public string parameterName;

            [Tooltip("Type of animator parameter")]
            public AnimParameterType parameterType = AnimParameterType.Float;

            [Tooltip("Source type for the value")]
            public SyncSourceType sourceType;

            [Tooltip("Property to read from source (e.g., 'velocity.magnitude', 'position.y')")]
            public string sourceProperty;

            [Tooltip("Health ID when using Health source type")]
            public string healthId;

            [Tooltip("Multiplier applied to the source value")]
            public float multiplier = 1f;

            [Tooltip("Threshold for bool parameter (value > threshold = true)")]
            public float boolThreshold = 0.01f;
        }

        [Serializable]
        public class AnimTriggerRule
        {
            [Tooltip("Animator trigger name to fire")]
            public string triggerName;

            [Tooltip("Event source that fires this trigger")]
            public TriggerEventSource eventSource;

            [Tooltip("Input action name when using Input source")]
            public string inputAction;

            [Tooltip("Health ID when using Health source")]
            public string healthId;

            [Tooltip("Health event type when using Health source")]
            public HealthEventType healthEvent;
        }

        public enum AnimParameterType
        {
            Float,
            Int,
            Bool
        }

        public enum SyncSourceType
        {
            Rigidbody3D,
            Rigidbody2D,
            Transform,
            Health,
            Custom
        }

        public enum TriggerEventSource
        {
            Health,
            Input,
            Manual
        }

        public enum HealthEventType
        {
            OnDamaged,
            OnHealed,
            OnDeath,
            OnRespawn,
            OnInvincibilityStart,
            OnInvincibilityEnd
        }

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (autoFindAnimator && animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }
#endif
    }
}
