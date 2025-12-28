using System;
using UnityEngine;
using UnityEngine.Events;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// Projectile component for bullets, missiles, and other launched objects.
    /// Supports physics-based and transform-based movement with damage and effects.
    /// </summary>
    public class GameKitProjectile : MonoBehaviour
    {
        #region Enums

        public enum MovementType
        {
            Transform,
            Rigidbody,
            Rigidbody2D
        }

        public enum DestroyBehavior
        {
            OnHit,
            OnLifetimeEnd,
            OnBounceLimit,
            Manual
        }

        #endregion

        #region Serialized Fields

        [Header("Identification")]
        [SerializeField] private string projectileId;

        [Header("Movement")]
        [SerializeField] private MovementType movementType = MovementType.Transform;
        [SerializeField] private float speed = 10f;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private bool useGravity = false;
        [SerializeField] private float gravityScale = 1f;

        [Header("Damage")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private bool damageOnHit = true;
        [SerializeField] private string targetTag = "";
        [SerializeField] private LayerMask targetLayers = -1;

        [Header("Bouncing")]
        [SerializeField] private bool canBounce = false;
        [SerializeField] private int maxBounces = 3;
        [SerializeField] private float bounciness = 0.8f;

        [Header("Homing")]
        [SerializeField] private bool isHoming = false;
        [SerializeField] private Transform homingTarget;
        [SerializeField] private float homingStrength = 5f;
        [SerializeField] private float maxHomingAngle = 45f;

        [Header("Piercing")]
        [SerializeField] private bool canPierce = false;
        [SerializeField] private int maxPierceCount = 3;
        [SerializeField] private float pierceDamageReduction = 0.2f;

        [Header("Effects")]
        [SerializeField] private GameObject hitEffect;
        [SerializeField] private GameObject trailEffect;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip launchSound;
        [SerializeField] private float effectDuration = 2f;

        [Header("Events")]
        public UnityEvent<GameKitProjectile, GameObject> OnHit;
        public UnityEvent<GameKitProjectile> OnDestroyed;
        public UnityEvent<GameKitProjectile, Vector3> OnBounce;

        #endregion

        #region Private Fields

        private Vector3 _direction;
        private Vector3 _velocity;
        private float _elapsedTime;
        private int _bounceCount;
        private int _pierceCount;
        private float _currentDamage;
        private Rigidbody _rigidbody;
        private Rigidbody2D _rigidbody2D;
        private bool _isLaunched;
        private GameObject _owner;

        #endregion

        #region Properties

        public string ProjectileId
        {
            get => projectileId;
            set => projectileId = value;
        }

        public float Speed
        {
            get => speed;
            set => speed = value;
        }

        public float Damage
        {
            get => damage;
            set => damage = value;
        }

        public float CurrentDamage => _currentDamage;

        public float Lifetime
        {
            get => lifetime;
            set => lifetime = value;
        }

        public bool IsHoming
        {
            get => isHoming;
            set => isHoming = value;
        }

        public Transform HomingTarget
        {
            get => homingTarget;
            set => homingTarget = value;
        }

        public bool CanBounce
        {
            get => canBounce;
            set => canBounce = value;
        }

        public bool CanPierce
        {
            get => canPierce;
            set => canPierce = value;
        }

        public GameObject Owner => _owner;
        public bool IsLaunched => _isLaunched;
        public Vector3 Direction => _direction;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody2D = GetComponent<Rigidbody2D>();

            if (string.IsNullOrEmpty(projectileId))
            {
                projectileId = $"projectile_{gameObject.name}_{GetInstanceID()}";
            }
        }

        private void Start()
        {
            if (trailEffect != null)
            {
                Instantiate(trailEffect, transform);
            }
        }

        private void Update()
        {
            if (!_isLaunched) return;

            _elapsedTime += Time.deltaTime;

            if (_elapsedTime >= lifetime)
            {
                DestroyProjectile();
                return;
            }

            if (movementType == MovementType.Transform)
            {
                UpdateTransformMovement();
            }
        }

        private void FixedUpdate()
        {
            if (!_isLaunched) return;

            if (isHoming && homingTarget != null)
            {
                UpdateHoming();
            }

            if (movementType == MovementType.Rigidbody && _rigidbody != null)
            {
                UpdateRigidbodyMovement();
            }
            else if (movementType == MovementType.Rigidbody2D && _rigidbody2D != null)
            {
                UpdateRigidbody2DMovement();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleHit(other.gameObject, other.ClosestPoint(transform.position));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleHit(other.gameObject, other.ClosestPoint(transform.position));
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (canBounce && _bounceCount < maxBounces)
            {
                HandleBounce(collision.contacts[0].normal);
            }
            else
            {
                HandleHit(collision.gameObject, collision.contacts[0].point);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (canBounce && _bounceCount < maxBounces)
            {
                HandleBounce(collision.contacts[0].normal);
            }
            else
            {
                HandleHit(collision.gameObject, collision.contacts[0].point);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Launches the projectile in the specified direction.
        /// </summary>
        public void Launch(Vector3 direction, GameObject owner = null)
        {
            _direction = direction.normalized;
            _owner = owner;
            _isLaunched = true;
            _currentDamage = damage;
            _elapsedTime = 0f;
            _bounceCount = 0;
            _pierceCount = 0;

            transform.forward = _direction;

            if (movementType == MovementType.Rigidbody && _rigidbody != null)
            {
                _rigidbody.useGravity = useGravity;
                _rigidbody.linearVelocity = _direction * speed;
            }
            else if (movementType == MovementType.Rigidbody2D && _rigidbody2D != null)
            {
                _rigidbody2D.gravityScale = useGravity ? gravityScale : 0f;
                _rigidbody2D.linearVelocity = _direction * speed;
            }

            if (launchSound != null)
            {
                AudioSource.PlayClipAtPoint(launchSound, transform.position);
            }
        }

        /// <summary>
        /// Launches the projectile toward a target position.
        /// </summary>
        public void LaunchAt(Vector3 targetPosition, GameObject owner = null)
        {
            var direction = (targetPosition - transform.position).normalized;
            Launch(direction, owner);
        }

        /// <summary>
        /// Launches the projectile toward a target transform.
        /// </summary>
        public void LaunchAt(Transform target, GameObject owner = null)
        {
            if (target == null) return;
            LaunchAt(target.position, owner);
        }

        /// <summary>
        /// Sets the homing target.
        /// </summary>
        public void SetHomingTarget(Transform target)
        {
            homingTarget = target;
            isHoming = target != null;
        }

        /// <summary>
        /// Destroys the projectile with effects.
        /// </summary>
        public void DestroyProjectile()
        {
            OnDestroyed?.Invoke(this);

            if (hitEffect != null)
            {
                var effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
                Destroy(effect, effectDuration);
            }

            Destroy(gameObject);
        }

        #endregion

        #region Private Methods

        private void UpdateTransformMovement()
        {
            if (useGravity)
            {
                _velocity += Physics.gravity * gravityScale * Time.deltaTime;
                _direction = (_direction * speed + _velocity).normalized;
            }

            transform.position += _direction * speed * Time.deltaTime;

            if (_direction != Vector3.zero)
            {
                transform.forward = _direction;
            }
        }

        private void UpdateRigidbodyMovement()
        {
            if (isHoming && homingTarget != null)
            {
                _rigidbody.linearVelocity = _direction * speed;
            }
        }

        private void UpdateRigidbody2DMovement()
        {
            if (isHoming && homingTarget != null)
            {
                _rigidbody2D.linearVelocity = _direction * speed;
            }
        }

        private void UpdateHoming()
        {
            if (homingTarget == null) return;

            var targetDir = (homingTarget.position - transform.position).normalized;
            var angle = Vector3.Angle(_direction, targetDir);

            if (angle <= maxHomingAngle)
            {
                _direction = Vector3.Slerp(_direction, targetDir, homingStrength * Time.fixedDeltaTime);
                _direction.Normalize();
                transform.forward = _direction;
            }
        }

        private void HandleHit(GameObject hitObject, Vector3 hitPoint)
        {
            // Check if we should ignore this object
            if (hitObject == _owner) return;

            // Check tag
            if (!string.IsNullOrEmpty(targetTag) && !hitObject.CompareTag(targetTag))
            {
                return;
            }

            // Check layer
            if (targetLayers != -1 && (targetLayers.value & (1 << hitObject.layer)) == 0)
            {
                return;
            }

            // Apply damage
            if (damageOnHit)
            {
                var health = hitObject.GetComponent<GameKitHealth>();
                if (health != null)
                {
                    health.TakeDamage(_currentDamage);
                }
            }

            // Play effects
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, hitPoint);
            }

            if (hitEffect != null)
            {
                var effect = Instantiate(hitEffect, hitPoint, Quaternion.identity);
                Destroy(effect, effectDuration);
            }

            // Invoke event
            OnHit?.Invoke(this, hitObject);

            // Handle piercing
            if (canPierce && _pierceCount < maxPierceCount)
            {
                _pierceCount++;
                _currentDamage *= (1f - pierceDamageReduction);
                return;
            }

            // Destroy projectile
            OnDestroyed?.Invoke(this);
            Destroy(gameObject);
        }

        private void HandleBounce(Vector3 normal)
        {
            _bounceCount++;
            _direction = Vector3.Reflect(_direction, normal);

            if (movementType == MovementType.Rigidbody && _rigidbody != null)
            {
                _rigidbody.linearVelocity = _direction * speed * bounciness;
            }
            else if (movementType == MovementType.Rigidbody2D && _rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = _direction * speed * bounciness;
            }

            speed *= bounciness;
            transform.forward = _direction;

            OnBounce?.Invoke(this, normal);
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Finds a projectile by its ID.
        /// </summary>
        public static GameKitProjectile FindById(string id)
        {
            var projectiles = FindObjectsByType<GameKitProjectile>(FindObjectsSortMode.None);
            foreach (var projectile in projectiles)
            {
                if (projectile.ProjectileId == id)
                {
                    return projectile;
                }
            }
            return null;
        }

        /// <summary>
        /// Creates and launches a projectile.
        /// </summary>
        public static GameKitProjectile Spawn(GameObject prefab, Vector3 position, Vector3 direction, GameObject owner = null)
        {
            var instance = Instantiate(prefab, position, Quaternion.LookRotation(direction));
            var projectile = instance.GetComponent<GameKitProjectile>();
            if (projectile != null)
            {
                projectile.Launch(direction, owner);
            }
            return projectile;
        }

        #endregion
    }
}
