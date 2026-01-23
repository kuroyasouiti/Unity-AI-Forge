using System;
using UnityEngine;
using UnityEngine.Events;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// Collectible item that can be picked up by players or other entities.
    /// Supports various item types with customizable collection behavior.
    /// </summary>
    public class GameKitCollectible : MonoBehaviour
    {
        #region Enums

        public enum CollectibleType
        {
            Coin,
            Health,
            Mana,
            PowerUp,
            Key,
            Ammo,
            Experience,
            Custom
        }

        public enum CollectionBehavior
        {
            Destroy,
            Disable,
            Respawn
        }

        #endregion

        #region Serialized Fields

        [Header("Identification")]
        [SerializeField] private string collectibleId;
        [SerializeField] private CollectibleType collectibleType = CollectibleType.Coin;
        [SerializeField] private string customTypeName;

        [Header("Value")]
        [SerializeField] private float value = 1f;
        [SerializeField] private int intValue = 1;

        [Header("Collection Settings")]
        [SerializeField] private CollectionBehavior collectionBehavior = CollectionBehavior.Destroy;
        [SerializeField] private float respawnDelay = 5f;
        [SerializeField] private bool collectable = true;

        [Header("Collection Conditions")]
        [SerializeField] private string requiredTag = "Player";
        [SerializeField] private LayerMask collectableLayers = -1;
        [SerializeField] private bool requireTriggerCollider = true;

        [Header("Effects")]
        [SerializeField] private GameObject collectEffect;
        [SerializeField] private AudioClip collectSound;
        [SerializeField] private float effectDuration = 2f;

        [Header("Animation")]
        [SerializeField] private bool enableFloatAnimation = false;
        [SerializeField] private float floatAmplitude = 0.25f;
        [SerializeField] private float floatFrequency = 1f;
        [SerializeField] private bool enableRotation = false;
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        [Header("Events")]
        public UnityEvent<GameKitCollectible, GameObject> OnCollected;
        public UnityEvent OnRespawned;

        #endregion

        #region Private Fields

        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private float _floatTime;
        private bool _isCollected;
        private Collider _collider;
        private Collider2D _collider2D;
        private Renderer _renderer;

        #endregion

        #region Properties

        public string CollectibleId
        {
            get => collectibleId;
            set => collectibleId = value;
        }

        public CollectibleType Type
        {
            get => collectibleType;
            set => collectibleType = value;
        }

        public string CustomTypeName
        {
            get => customTypeName;
            set => customTypeName = value;
        }

        public float Value
        {
            get => value;
            set => this.value = value;
        }

        public int IntValue
        {
            get => intValue;
            set => intValue = value;
        }

        public CollectionBehavior Behavior
        {
            get => collectionBehavior;
            set => collectionBehavior = value;
        }

        public float RespawnDelay
        {
            get => respawnDelay;
            set => respawnDelay = value;
        }

        public bool IsCollectable
        {
            get => collectable;
            set => collectable = value;
        }

        public bool IsCollected => _isCollected;

        public string RequiredTag
        {
            get => requiredTag;
            set => requiredTag = value;
        }

        public LayerMask CollectableLayers
        {
            get => collectableLayers;
            set => collectableLayers = value;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _startPosition = transform.position;
            _startRotation = transform.rotation;
            _collider = GetComponent<Collider>();
            _collider2D = GetComponent<Collider2D>();
            _renderer = GetComponent<Renderer>();

            if (string.IsNullOrEmpty(collectibleId))
            {
                collectibleId = $"collectible_{gameObject.name}_{GetInstanceID()}";
            }
        }

        private void Update()
        {
            if (_isCollected) return;

            if (enableFloatAnimation)
            {
                _floatTime += Time.deltaTime * floatFrequency;
                var offset = Mathf.Sin(_floatTime * Mathf.PI * 2f) * floatAmplitude;
                transform.position = _startPosition + Vector3.up * offset;
            }

            if (enableRotation)
            {
                transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (requireTriggerCollider)
            {
                TryCollect(other.gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (requireTriggerCollider)
            {
                TryCollect(other.gameObject);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!requireTriggerCollider)
            {
                TryCollect(collision.gameObject);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!requireTriggerCollider)
            {
                TryCollect(collision.gameObject);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Attempts to collect this item by the specified collector.
        /// </summary>
        public bool TryCollect(GameObject collector)
        {
            if (!collectable || _isCollected) return false;

            // Check tag requirement
            if (!string.IsNullOrEmpty(requiredTag) && !collector.CompareTag(requiredTag))
            {
                return false;
            }

            // Check layer requirement
            if (collectableLayers != -1 && (collectableLayers.value & (1 << collector.layer)) == 0)
            {
                return false;
            }

            Collect(collector);
            return true;
        }

        /// <summary>
        /// Forces collection without checking conditions.
        /// </summary>
        public void Collect(GameObject collector)
        {
            if (_isCollected) return;

            _isCollected = true;

            // Spawn effect
            if (collectEffect != null)
            {
                var effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
                Destroy(effect, effectDuration);
            }

            // Play sound
            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }

            // Apply value to collector if it has compatible components
            ApplyValueToCollector(collector);

            // Invoke event
            OnCollected?.Invoke(this, collector);

            // Handle collection behavior
            switch (collectionBehavior)
            {
                case CollectionBehavior.Destroy:
                    Destroy(gameObject);
                    break;

                case CollectionBehavior.Disable:
                    SetVisible(false);
                    break;

                case CollectionBehavior.Respawn:
                    SetVisible(false);
                    Invoke(nameof(Respawn), respawnDelay);
                    break;
            }
        }

        /// <summary>
        /// Respawns the collectible at its original position.
        /// </summary>
        public void Respawn()
        {
            _isCollected = false;
            transform.position = _startPosition;
            transform.rotation = _startRotation;
            _floatTime = 0f;
            SetVisible(true);
            OnRespawned?.Invoke();
        }

        /// <summary>
        /// Resets the collectible without triggering respawn event.
        /// </summary>
        public void Reset()
        {
            CancelInvoke(nameof(Respawn));
            _isCollected = false;
            transform.position = _startPosition;
            transform.rotation = _startRotation;
            _floatTime = 0f;
            SetVisible(true);
        }

        #endregion

        #region Private Methods

        private void SetVisible(bool visible)
        {
            if (_renderer != null) _renderer.enabled = visible;
            if (_collider != null) _collider.enabled = visible;
            if (_collider2D != null) _collider2D.enabled = visible;

            // Also disable/enable child renderers
            foreach (var childRenderer in GetComponentsInChildren<Renderer>())
            {
                childRenderer.enabled = visible;
            }
        }

        private void ApplyValueToCollector(GameObject collector)
        {
            // Try to apply to GameKitHealth for health collectibles
            if (collectibleType == CollectibleType.Health)
            {
                var health = collector.GetComponent<GameKitHealth>();
                if (health != null)
                {
                    health.Heal(value);
                }
            }

            // Try to apply to GameKitResourceManager
            var resourceManager = collector.GetComponent<GameKitResourceManager>();
            if (resourceManager != null)
            {
                string resourceName = collectibleType switch
                {
                    CollectibleType.Coin => "gold",
                    CollectibleType.Health => "health",
                    CollectibleType.Mana => "mana",
                    CollectibleType.Ammo => "ammo",
                    CollectibleType.Experience => "experience",
                    CollectibleType.Custom => customTypeName.ToLower(),
                    _ => collectibleType.ToString().ToLower()
                };

                resourceManager.AddResource(resourceName, value);
            }
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Finds a collectible by its ID.
        /// </summary>
        public static GameKitCollectible FindById(string id)
        {
            var collectibles = FindObjectsByType<GameKitCollectible>(FindObjectsSortMode.None);
            foreach (var collectible in collectibles)
            {
                if (collectible.CollectibleId == id)
                {
                    return collectible;
                }
            }
            return null;
        }

        #endregion
    }
}
