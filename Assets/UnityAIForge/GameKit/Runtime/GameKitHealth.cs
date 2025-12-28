using System;
using UnityEngine;
using UnityEngine.Events;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Health component: manages health, damage, healing, and death behavior.
    /// Provides a complete health system without requiring custom scripts.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/Health")]
    public class GameKitHealth : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string healthId;

        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;

        [Header("Damage Settings")]
        [SerializeField] private float invincibilityDuration = 0.5f;
        [SerializeField] private bool canTakeDamage = true;

        [Header("Death Settings")]
        [SerializeField] private DeathBehavior onDeath = DeathBehavior.Destroy;
        [SerializeField] private Vector3 respawnPosition;
        [SerializeField] private float respawnDelay = 1f;
        [SerializeField] private bool resetHealthOnRespawn = true;

        [Header("Events")]
        [Tooltip("Invoked when health changes (currentHealth, maxHealth)")]
        public UnityEvent<float, float> OnHealthChanged = new UnityEvent<float, float>();

        [Tooltip("Invoked when damage is taken (damageAmount)")]
        public UnityEvent<float> OnDamaged = new UnityEvent<float>();

        [Tooltip("Invoked when healed (healAmount)")]
        public UnityEvent<float> OnHealed = new UnityEvent<float>();

        [Tooltip("Invoked when health reaches zero")]
        public UnityEvent OnDeath = new UnityEvent();

        [Tooltip("Invoked after respawn")]
        public UnityEvent OnRespawn = new UnityEvent();

        [Tooltip("Invoked when invincibility starts")]
        public UnityEvent OnInvincibilityStart = new UnityEvent();

        [Tooltip("Invoked when invincibility ends")]
        public UnityEvent OnInvincibilityEnd = new UnityEvent();

        // State
        private bool isInvincible = false;
        private float invincibilityTimer = 0f;
        private bool isDead = false;

        // Properties
        public string HealthId => healthId;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercent => maxHealth > 0 ? currentHealth / maxHealth : 0f;
        public bool IsAlive => !isDead && currentHealth > 0;
        public bool IsDead => isDead;
        public bool IsInvincible => isInvincible;
        public bool CanTakeDamage => canTakeDamage && !isInvincible && !isDead;
        public DeathBehavior DeathBehaviorType => onDeath;
        public Vector3 RespawnPosition => respawnPosition;

        private void Awake()
        {
            EnsureEventsInitialized();
        }

        private void Update()
        {
            // Handle invincibility timer
            if (isInvincible)
            {
                invincibilityTimer -= Time.deltaTime;
                if (invincibilityTimer <= 0)
                {
                    isInvincible = false;
                    OnInvincibilityEnd?.Invoke();
                }
            }
        }

        /// <summary>
        /// Initialize the health component with specified values.
        /// </summary>
        public void Initialize(string id, float max, float current, DeathBehavior deathBehavior = DeathBehavior.Destroy)
        {
            healthId = id;
            maxHealth = max;
            currentHealth = Mathf.Clamp(current, 0, max);
            onDeath = deathBehavior;
            isDead = false;
            isInvincible = false;

            EnsureEventsInitialized();
        }

        private void EnsureEventsInitialized()
        {
            OnHealthChanged ??= new UnityEvent<float, float>();
            OnDamaged ??= new UnityEvent<float>();
            OnHealed ??= new UnityEvent<float>();
            OnDeath ??= new UnityEvent();
            OnRespawn ??= new UnityEvent();
            OnInvincibilityStart ??= new UnityEvent();
            OnInvincibilityEnd ??= new UnityEvent();
        }

        /// <summary>
        /// Apply damage to this health component.
        /// </summary>
        /// <param name="amount">Amount of damage to apply</param>
        /// <returns>Actual damage dealt (may be 0 if invincible)</returns>
        public float TakeDamage(float amount)
        {
            if (!CanTakeDamage || amount <= 0)
                return 0f;

            float previousHealth = currentHealth;
            currentHealth = Mathf.Max(0, currentHealth - amount);
            float actualDamage = previousHealth - currentHealth;

            if (actualDamage > 0)
            {
                OnDamaged?.Invoke(actualDamage);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);

                // Start invincibility
                if (invincibilityDuration > 0 && currentHealth > 0)
                {
                    StartInvincibility(invincibilityDuration);
                }

                // Check for death
                if (currentHealth <= 0 && !isDead)
                {
                    Die();
                }
            }

            return actualDamage;
        }

        /// <summary>
        /// Apply healing to this health component.
        /// </summary>
        /// <param name="amount">Amount to heal</param>
        /// <returns>Actual amount healed</returns>
        public float Heal(float amount)
        {
            if (isDead || amount <= 0)
                return 0f;

            float previousHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            float actualHeal = currentHealth - previousHealth;

            if (actualHeal > 0)
            {
                OnHealed?.Invoke(actualHeal);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }

            return actualHeal;
        }

        /// <summary>
        /// Set health to a specific value.
        /// </summary>
        public void SetHealth(float value)
        {
            float previousHealth = currentHealth;
            currentHealth = Mathf.Clamp(value, 0, maxHealth);

            if (!Mathf.Approximately(previousHealth, currentHealth))
            {
                OnHealthChanged?.Invoke(currentHealth, maxHealth);

                if (currentHealth <= 0 && !isDead)
                {
                    Die();
                }
            }
        }

        /// <summary>
        /// Set max health. Optionally adjust current health proportionally.
        /// </summary>
        public void SetMaxHealth(float value, bool adjustCurrentHealth = false)
        {
            float ratio = maxHealth > 0 ? currentHealth / maxHealth : 1f;
            maxHealth = Mathf.Max(1, value);

            if (adjustCurrentHealth)
            {
                currentHealth = maxHealth * ratio;
            }
            else
            {
                currentHealth = Mathf.Min(currentHealth, maxHealth);
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Instantly kill this entity.
        /// </summary>
        public void Kill()
        {
            if (isDead)
                return;

            currentHealth = 0;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            Die();
        }

        /// <summary>
        /// Start invincibility for a specified duration.
        /// </summary>
        public void StartInvincibility(float duration)
        {
            if (duration <= 0)
                return;

            isInvincible = true;
            invincibilityTimer = duration;
            OnInvincibilityStart?.Invoke();
        }

        /// <summary>
        /// End invincibility immediately.
        /// </summary>
        public void EndInvincibility()
        {
            if (isInvincible)
            {
                isInvincible = false;
                invincibilityTimer = 0;
                OnInvincibilityEnd?.Invoke();
            }
        }

        /// <summary>
        /// Respawn the entity at the respawn position.
        /// </summary>
        public void Respawn()
        {
            isDead = false;
            isInvincible = false;

            if (resetHealthOnRespawn)
            {
                currentHealth = maxHealth;
            }

            transform.position = respawnPosition;
            gameObject.SetActive(true);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnRespawn?.Invoke();
        }

        /// <summary>
        /// Set the respawn position.
        /// </summary>
        public void SetRespawnPosition(Vector3 position)
        {
            respawnPosition = position;
        }

        private void Die()
        {
            isDead = true;
            OnDeath?.Invoke();

            switch (onDeath)
            {
                case DeathBehavior.Destroy:
                    Destroy(gameObject);
                    break;

                case DeathBehavior.Disable:
                    gameObject.SetActive(false);
                    break;

                case DeathBehavior.Respawn:
                    StartCoroutine(RespawnAfterDelay());
                    break;

                case DeathBehavior.Event:
                    // Just fire the event, let external systems handle it
                    break;
            }
        }

        private System.Collections.IEnumerator RespawnAfterDelay()
        {
            if (respawnDelay > 0)
            {
                yield return new WaitForSeconds(respawnDelay);
            }
            Respawn();
        }

        /// <summary>
        /// Death behavior options.
        /// </summary>
        public enum DeathBehavior
        {
            /// <summary>Destroy the GameObject on death.</summary>
            Destroy,
            /// <summary>Disable the GameObject on death.</summary>
            Disable,
            /// <summary>Respawn at the respawn position after delay.</summary>
            Respawn,
            /// <summary>Only fire the OnDeath event, no automatic behavior.</summary>
            Event
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw respawn position
            if (onDeath == DeathBehavior.Respawn)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(respawnPosition, 0.5f);
                Gizmos.DrawLine(transform.position, respawnPosition);
            }
        }
#endif
    }
}
