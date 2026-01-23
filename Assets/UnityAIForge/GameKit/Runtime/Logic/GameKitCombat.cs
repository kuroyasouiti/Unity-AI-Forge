using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Combat: Unified damage calculation and attack system.
    /// Handles melee, ranged, AoE, and projectile attacks with hitboxes.
    /// Integrates with GameKitHealth for damage application.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/Logic/Combat")]
    public class GameKitCombat : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string combatId;

        [Header("Attack Configuration")]
        [SerializeField] private AttackType attackType = AttackType.Melee;
        [SerializeField] private float baseDamage = 10f;
        [SerializeField] private float damageVariance = 0f;
        [SerializeField] private float critChance = 0f;
        [SerializeField] private float critMultiplier = 2f;

        [Header("Hitbox Settings")]
        [SerializeField] private HitboxShape hitboxShape = HitboxShape.Sphere;
        [SerializeField] private Vector3 hitboxSize = Vector3.one;
        [SerializeField] private Vector3 hitboxOffset = Vector3.zero;
        [SerializeField] private float hitboxRadius = 1f;

        [Header("Targeting")]
        [SerializeField] private List<string> targetTags = new List<string> { "Enemy" };
        [SerializeField] private LayerMask targetLayers = -1;
        [SerializeField] private bool hitMultipleTargets = true;
        [SerializeField] private int maxTargets = 10;

        [Header("Timing")]
        [SerializeField] private float attackCooldown = 0.5f;
        [SerializeField] private float hitDuration = 0.1f;

        [Header("Effects Integration")]
        [SerializeField] private string onHitEffectId;
        [SerializeField] private string onCritEffectId;
        [SerializeField] private string onMissEffectId;

        [Header("Projectile Settings (for Ranged/Projectile types)")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 20f;
        [SerializeField] private Transform firePoint;

        [Header("Events")]
        public UnityEvent<AttackResult> OnAttackPerformed = new UnityEvent<AttackResult>();
        public UnityEvent<HitResult> OnHitDealt = new UnityEvent<HitResult>();
        public UnityEvent OnAttackMissed = new UnityEvent();
        public UnityEvent OnCooldownComplete = new UnityEvent();

        // Registry
        private static readonly Dictionary<string, GameKitCombat> _registry = new Dictionary<string, GameKitCombat>();

        // State
        private float _cooldownTimer;
        private bool _isOnCooldown;
        private Collider[] _hitColliders3D = new Collider[20];
        private Collider2D[] _hitColliders2D = new Collider2D[20];

        public string CombatId => combatId;
        public AttackType Type => attackType;
        public float BaseDamage => baseDamage;
        public bool IsOnCooldown => _isOnCooldown;
        public float CooldownRemaining => _cooldownTimer;
        public float CooldownPercent => attackCooldown > 0 ? _cooldownTimer / attackCooldown : 0f;

        /// <summary>
        /// Attack types supported by the combat system.
        /// </summary>
        public enum AttackType
        {
            Melee,      // Close-range hitbox attack
            Ranged,     // Instant raycast attack
            AoE,        // Area of effect damage
            Projectile  // Spawns projectile prefab
        }

        /// <summary>
        /// Hitbox shapes for attack detection.
        /// </summary>
        public enum HitboxShape
        {
            Sphere,
            Box,
            Capsule,
            Cone
        }

        /// <summary>
        /// Result of an attack action.
        /// </summary>
        [Serializable]
        public class AttackResult
        {
            public string combatId;
            public AttackType attackType;
            public int targetsHit;
            public float totalDamage;
            public bool hasCrit;
            public Vector3 attackOrigin;
        }

        /// <summary>
        /// Result of a single hit.
        /// </summary>
        [Serializable]
        public class HitResult
        {
            public string combatId;
            public GameObject target;
            public float damage;
            public bool isCrit;
            public Vector3 hitPoint;
            public GameKitHealth targetHealth;
        }

        /// <summary>
        /// Find combat component by ID.
        /// </summary>
        public static GameKitCombat FindById(string id)
        {
            return _registry.TryGetValue(id, out var combat) ? combat : null;
        }

        private void Awake()
        {
            EnsureEventsInitialized();
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(combatId))
            {
                _registry[combatId] = this;
            }
        }

        private void OnDisable()
        {
            if (!string.IsNullOrEmpty(combatId))
            {
                _registry.Remove(combatId);
            }
        }

        private void Update()
        {
            if (_isOnCooldown)
            {
                _cooldownTimer -= Time.deltaTime;
                if (_cooldownTimer <= 0)
                {
                    _isOnCooldown = false;
                    _cooldownTimer = 0;
                    OnCooldownComplete?.Invoke();
                }
            }
        }

        private void EnsureEventsInitialized()
        {
            OnAttackPerformed ??= new UnityEvent<AttackResult>();
            OnHitDealt ??= new UnityEvent<HitResult>();
            OnAttackMissed ??= new UnityEvent();
            OnCooldownComplete ??= new UnityEvent();
        }

        /// <summary>
        /// Initialize the combat component with specified values.
        /// </summary>
        public void Initialize(string id, AttackType type, float damage, HitboxShape shape, float radius)
        {
            combatId = id;
            attackType = type;
            baseDamage = damage;
            hitboxShape = shape;
            hitboxRadius = radius;

            EnsureEventsInitialized();
        }

        /// <summary>
        /// Perform an attack from the current position and direction.
        /// </summary>
        public AttackResult PerformAttack()
        {
            return PerformAttack(transform.position + hitboxOffset, transform.forward);
        }

        /// <summary>
        /// Perform an attack from a specific position and direction.
        /// </summary>
        public AttackResult PerformAttack(Vector3 origin, Vector3 direction)
        {
            if (_isOnCooldown) return null;

            var result = new AttackResult
            {
                combatId = combatId,
                attackType = attackType,
                attackOrigin = origin
            };

            switch (attackType)
            {
                case AttackType.Melee:
                case AttackType.AoE:
                    PerformAreaAttack(origin, result);
                    break;

                case AttackType.Ranged:
                    PerformRaycastAttack(origin, direction, result);
                    break;

                case AttackType.Projectile:
                    PerformProjectileAttack(origin, direction, result);
                    break;
            }

            // Start cooldown
            _isOnCooldown = true;
            _cooldownTimer = attackCooldown;

            OnAttackPerformed?.Invoke(result);

            if (result.targetsHit == 0)
            {
                OnAttackMissed?.Invoke();
            }

            return result;
        }

        private void PerformAreaAttack(Vector3 origin, AttackResult result)
        {
            List<GameObject> targets = new List<GameObject>();

            // Detect targets based on hitbox shape
            switch (hitboxShape)
            {
                case HitboxShape.Sphere:
                    DetectTargetsSphere(origin, targets);
                    break;

                case HitboxShape.Box:
                    DetectTargetsBox(origin, targets);
                    break;

                case HitboxShape.Capsule:
                    DetectTargetsCapsule(origin, targets);
                    break;
            }

            // Apply damage to targets
            foreach (var target in targets)
            {
                if (!hitMultipleTargets && result.targetsHit > 0) break;
                if (result.targetsHit >= maxTargets) break;

                ApplyDamageToTarget(target, origin, result);
            }
        }

        private void DetectTargetsSphere(Vector3 origin, List<GameObject> targets)
        {
            // Try 3D first
            int count3D = Physics.OverlapSphereNonAlloc(origin, hitboxRadius, _hitColliders3D, targetLayers);
            for (int i = 0; i < count3D; i++)
            {
                var go = _hitColliders3D[i].gameObject;
                if (IsValidTarget(go))
                {
                    targets.Add(go);
                }
            }

            // Try 2D
            int count2D = Physics2D.OverlapCircleNonAlloc(origin, hitboxRadius, _hitColliders2D, targetLayers);
            for (int i = 0; i < count2D; i++)
            {
                var go = _hitColliders2D[i].gameObject;
                if (IsValidTarget(go) && !targets.Contains(go))
                {
                    targets.Add(go);
                }
            }
        }

        private void DetectTargetsBox(Vector3 origin, List<GameObject> targets)
        {
            // 3D box detection
            int count3D = Physics.OverlapBoxNonAlloc(origin, hitboxSize / 2f, _hitColliders3D, transform.rotation, targetLayers);
            for (int i = 0; i < count3D; i++)
            {
                var go = _hitColliders3D[i].gameObject;
                if (IsValidTarget(go))
                {
                    targets.Add(go);
                }
            }

            // 2D box detection
            int count2D = Physics2D.OverlapBoxNonAlloc(origin, new Vector2(hitboxSize.x, hitboxSize.y), 0f, _hitColliders2D, targetLayers);
            for (int i = 0; i < count2D; i++)
            {
                var go = _hitColliders2D[i].gameObject;
                if (IsValidTarget(go) && !targets.Contains(go))
                {
                    targets.Add(go);
                }
            }
        }

        private void DetectTargetsCapsule(Vector3 origin, List<GameObject> targets)
        {
            Vector3 point1 = origin - transform.up * (hitboxSize.y / 2f - hitboxRadius);
            Vector3 point2 = origin + transform.up * (hitboxSize.y / 2f - hitboxRadius);

            int count = Physics.OverlapCapsuleNonAlloc(point1, point2, hitboxRadius, _hitColliders3D, targetLayers);
            for (int i = 0; i < count; i++)
            {
                var go = _hitColliders3D[i].gameObject;
                if (IsValidTarget(go))
                {
                    targets.Add(go);
                }
            }
        }

        private void PerformRaycastAttack(Vector3 origin, Vector3 direction, AttackResult result)
        {
            // 3D raycast
            if (Physics.Raycast(origin, direction, out RaycastHit hit3D, hitboxRadius, targetLayers))
            {
                if (IsValidTarget(hit3D.collider.gameObject))
                {
                    ApplyDamageToTarget(hit3D.collider.gameObject, hit3D.point, result);
                    return;
                }
            }

            // 2D raycast
            RaycastHit2D hit2D = Physics2D.Raycast(origin, direction, hitboxRadius, targetLayers);
            if (hit2D.collider != null && IsValidTarget(hit2D.collider.gameObject))
            {
                ApplyDamageToTarget(hit2D.collider.gameObject, hit2D.point, result);
            }
        }

        private void PerformProjectileAttack(Vector3 origin, Vector3 direction, AttackResult result)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning($"[GameKitCombat] No projectile prefab assigned for {combatId}");
                return;
            }

            Vector3 spawnPos = firePoint != null ? firePoint.position : origin;
            Quaternion spawnRot = Quaternion.LookRotation(direction);

            var projectile = Instantiate(projectilePrefab, spawnPos, spawnRot);

            // Configure projectile if it has GameKitProjectile
            var gkProjectile = projectile.GetComponent<GameKitProjectile>();
            if (gkProjectile != null)
            {
                gkProjectile.Damage = CalculateDamage(out bool isCrit);
            }

            // Configure Rigidbody velocity
            var rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = direction.normalized * projectileSpeed;
            }

            var rb2D = projectile.GetComponent<Rigidbody2D>();
            if (rb2D != null)
            {
                rb2D.linearVelocity = direction.normalized * projectileSpeed;
            }

            result.targetsHit = 1; // Projectile counts as successful attack
        }

        private bool IsValidTarget(GameObject target)
        {
            if (target == gameObject) return false;

            if (targetTags.Count > 0)
            {
                bool hasTag = false;
                foreach (var tag in targetTags)
                {
                    if (target.CompareTag(tag))
                    {
                        hasTag = true;
                        break;
                    }
                }
                if (!hasTag) return false;
            }

            return true;
        }

        private void ApplyDamageToTarget(GameObject target, Vector3 hitPoint, AttackResult attackResult)
        {
            float damage = CalculateDamage(out bool isCrit);

            var hitResult = new HitResult
            {
                combatId = combatId,
                target = target,
                damage = damage,
                isCrit = isCrit,
                hitPoint = hitPoint
            };

            // Apply to GameKitHealth if present
            var health = target.GetComponent<GameKitHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
                hitResult.targetHealth = health;
            }

            // Update attack result
            attackResult.targetsHit++;
            attackResult.totalDamage += damage;
            if (isCrit) attackResult.hasCrit = true;

            OnHitDealt?.Invoke(hitResult);

            // Trigger effects
            if (isCrit && !string.IsNullOrEmpty(onCritEffectId))
            {
                GameKitEffectManager.Instance?.PlayEffect(onCritEffectId, hitPoint);
            }
            else if (!string.IsNullOrEmpty(onHitEffectId))
            {
                GameKitEffectManager.Instance?.PlayEffect(onHitEffectId, hitPoint);
            }
        }

        private float CalculateDamage(out bool isCrit)
        {
            float damage = baseDamage;

            // Apply variance
            if (damageVariance > 0)
            {
                damage += UnityEngine.Random.Range(-damageVariance, damageVariance);
            }

            // Check for crit
            isCrit = UnityEngine.Random.value < critChance;
            if (isCrit)
            {
                damage *= critMultiplier;
            }

            return Mathf.Max(0, damage);
        }

        /// <summary>
        /// Set the base damage value.
        /// </summary>
        public void SetBaseDamage(float damage)
        {
            baseDamage = damage;
        }

        /// <summary>
        /// Set the attack cooldown duration.
        /// </summary>
        public void SetCooldown(float cooldown)
        {
            attackCooldown = cooldown;
        }

        /// <summary>
        /// Add a target tag to the filter list.
        /// </summary>
        public void AddTargetTag(string tag)
        {
            if (!targetTags.Contains(tag))
            {
                targetTags.Add(tag);
            }
        }

        /// <summary>
        /// Remove a target tag from the filter list.
        /// </summary>
        public void RemoveTargetTag(string tag)
        {
            targetTags.Remove(tag);
        }

        /// <summary>
        /// Reset the cooldown, allowing immediate attack.
        /// </summary>
        public void ResetCooldown()
        {
            _isOnCooldown = false;
            _cooldownTimer = 0;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position + hitboxOffset;
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

            switch (hitboxShape)
            {
                case HitboxShape.Sphere:
                    Gizmos.DrawWireSphere(origin, hitboxRadius);
                    break;

                case HitboxShape.Box:
                    Gizmos.matrix = Matrix4x4.TRS(origin, transform.rotation, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, hitboxSize);
                    Gizmos.matrix = Matrix4x4.identity;
                    break;

                case HitboxShape.Capsule:
                    Gizmos.DrawWireSphere(origin - transform.up * (hitboxSize.y / 2f - hitboxRadius), hitboxRadius);
                    Gizmos.DrawWireSphere(origin + transform.up * (hitboxSize.y / 2f - hitboxRadius), hitboxRadius);
                    break;
            }
        }
#endif
    }
}
