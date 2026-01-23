using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// Trigger zone that executes actions when entities enter, stay in, or exit the zone.
    /// Supports various zone types like checkpoints, damage zones, heal zones, and teleporters.
    /// </summary>
    public class GameKitTriggerZone : MonoBehaviour
    {
        #region Enums

        public enum ZoneType
        {
            Generic,
            Checkpoint,
            DamageZone,
            HealZone,
            Teleport,
            SpeedBoost,
            SlowDown,
            KillZone,
            SafeZone,
            Trigger
        }

        public enum TriggerMode
        {
            Once,
            OncePerEntity,
            Repeat,
            WhileInside
        }

        #endregion

        #region Serialized Fields

        [Header("Identification")]
        [SerializeField] private string zoneId;
        [SerializeField] private ZoneType zoneType = ZoneType.Generic;

        [Header("Trigger Settings")]
        [SerializeField] private TriggerMode triggerMode = TriggerMode.Repeat;
        [SerializeField] private string requiredTag = "Player";
        [SerializeField] private LayerMask triggerLayers = -1;
        [SerializeField] private bool requireTriggerCollider = true;

        [Header("Zone Effects")]
        [SerializeField] private float effectValue = 10f;
        [SerializeField] private float effectInterval = 1f;
        [SerializeField] private float speedMultiplier = 1.5f;

        [Header("Teleport Settings")]
        [SerializeField] private Transform teleportDestination;
        [SerializeField] private bool preserveVelocity = false;
        [SerializeField] private bool preserveRotation = true;

        [Header("Checkpoint Settings")]
        [SerializeField] private bool isActiveCheckpoint = false;
        [SerializeField] private Vector3 respawnOffset = Vector3.up;

        [Header("Visual Feedback")]
        [SerializeField] private bool changeColorOnEnter = false;
        [SerializeField] private Color activeColor = Color.green;
        [SerializeField] private Color inactiveColor = Color.gray;
        [SerializeField] private GameObject enterEffect;
        [SerializeField] private GameObject exitEffect;
        [SerializeField] private GameObject stayEffect;

        [Header("Audio")]
        [SerializeField] private AudioClip enterSound;
        [SerializeField] private AudioClip exitSound;

        [Header("Events")]
        public UnityEvent<GameObject> OnZoneEnter;
        public UnityEvent<GameObject> OnZoneExit;
        public UnityEvent<GameObject> OnZoneStay;
        public UnityEvent<GameObject> OnZoneTriggered;
        public UnityEvent OnCheckpointActivated;

        #endregion

        #region Private Fields

        private bool _hasTriggered;
        private HashSet<GameObject> _triggeredEntities = new HashSet<GameObject>();
        private Dictionary<GameObject, float> _stayTimers = new Dictionary<GameObject, float>();
        private List<GameObject> _entitiesInZone = new List<GameObject>();
        private Renderer _renderer;
        private Color _originalColor;
        private GameObject _activeStayEffect;

        #endregion

        #region Properties

        public string ZoneId
        {
            get => zoneId;
            set => zoneId = value;
        }

        public ZoneType Type
        {
            get => zoneType;
            set => zoneType = value;
        }

        public TriggerMode Mode
        {
            get => triggerMode;
            set => triggerMode = value;
        }

        public float EffectValue
        {
            get => effectValue;
            set => effectValue = value;
        }

        public Transform TeleportDestination
        {
            get => teleportDestination;
            set => teleportDestination = value;
        }

        public bool IsActiveCheckpoint => isActiveCheckpoint;
        public int EntitiesInZoneCount => _entitiesInZone.Count;
        public bool HasTriggered => _hasTriggered;

        public Vector3 RespawnPosition => transform.position + respawnOffset;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                _originalColor = _renderer.material.color;
            }

            if (string.IsNullOrEmpty(zoneId))
            {
                zoneId = $"zone_{gameObject.name}_{GetInstanceID()}";
            }
        }

        private void Update()
        {
            if (triggerMode != TriggerMode.WhileInside || _entitiesInZone.Count == 0) return;

            // Process effects for entities staying in zone
            var keysToUpdate = new List<GameObject>(_stayTimers.Keys);
            foreach (var entity in keysToUpdate)
            {
                if (entity == null)
                {
                    _stayTimers.Remove(entity);
                    _entitiesInZone.Remove(entity);
                    continue;
                }

                _stayTimers[entity] += Time.deltaTime;
                if (_stayTimers[entity] >= effectInterval)
                {
                    _stayTimers[entity] = 0f;
                    ApplyZoneEffect(entity);
                    OnZoneStay?.Invoke(entity);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (requireTriggerCollider)
            {
                HandleEnter(other.gameObject);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (requireTriggerCollider)
            {
                HandleExit(other.gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (requireTriggerCollider)
            {
                HandleEnter(other.gameObject);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (requireTriggerCollider)
            {
                HandleExit(other.gameObject);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!requireTriggerCollider)
            {
                HandleEnter(collision.gameObject);
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (!requireTriggerCollider)
            {
                HandleExit(collision.gameObject);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!requireTriggerCollider)
            {
                HandleEnter(collision.gameObject);
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (!requireTriggerCollider)
            {
                HandleExit(collision.gameObject);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Resets the trigger state.
        /// </summary>
        public void ResetTrigger()
        {
            _hasTriggered = false;
            _triggeredEntities.Clear();
        }

        /// <summary>
        /// Activates this zone as the current checkpoint.
        /// </summary>
        public void ActivateCheckpoint()
        {
            // Deactivate other checkpoints
            var checkpoints = FindObjectsByType<GameKitTriggerZone>(FindObjectsSortMode.None);
            foreach (var cp in checkpoints)
            {
                if (cp.zoneType == ZoneType.Checkpoint && cp != this)
                {
                    cp.DeactivateCheckpoint();
                }
            }

            isActiveCheckpoint = true;
            UpdateVisuals(true);
            OnCheckpointActivated?.Invoke();
        }

        /// <summary>
        /// Deactivates this checkpoint.
        /// </summary>
        public void DeactivateCheckpoint()
        {
            isActiveCheckpoint = false;
            UpdateVisuals(false);
        }

        /// <summary>
        /// Teleports an entity to the destination.
        /// </summary>
        public void TeleportEntity(GameObject entity)
        {
            if (teleportDestination == null) return;

            var rb = entity.GetComponent<Rigidbody>();
            var rb2d = entity.GetComponent<Rigidbody2D>();
            Vector3 oldVelocity = Vector3.zero;

            if (rb != null)
            {
                oldVelocity = rb.linearVelocity;
                rb.linearVelocity = Vector3.zero;
            }
            if (rb2d != null)
            {
                oldVelocity = rb2d.linearVelocity;
                rb2d.linearVelocity = Vector2.zero;
            }

            entity.transform.position = teleportDestination.position;

            if (!preserveRotation)
            {
                entity.transform.rotation = teleportDestination.rotation;
            }

            if (preserveVelocity)
            {
                if (rb != null) rb.linearVelocity = oldVelocity;
                if (rb2d != null) rb2d.linearVelocity = oldVelocity;
            }
        }

        /// <summary>
        /// Forces the zone to trigger for a specific entity.
        /// </summary>
        public void ForceTrigger(GameObject entity)
        {
            ApplyZoneEffect(entity);
            OnZoneTriggered?.Invoke(entity);
        }

        /// <summary>
        /// Gets all entities currently in the zone.
        /// </summary>
        public List<GameObject> GetEntitiesInZone()
        {
            return new List<GameObject>(_entitiesInZone);
        }

        #endregion

        #region Private Methods

        private void HandleEnter(GameObject entity)
        {
            if (!CanTrigger(entity)) return;

            _entitiesInZone.Add(entity);
            _stayTimers[entity] = 0f;

            // Check trigger mode
            bool shouldTrigger = true;
            switch (triggerMode)
            {
                case TriggerMode.Once:
                    if (_hasTriggered) shouldTrigger = false;
                    else _hasTriggered = true;
                    break;

                case TriggerMode.OncePerEntity:
                    if (_triggeredEntities.Contains(entity)) shouldTrigger = false;
                    else _triggeredEntities.Add(entity);
                    break;

                case TriggerMode.WhileInside:
                    // Apply initial effect, then continue in Update
                    break;
            }

            if (shouldTrigger)
            {
                ApplyZoneEffect(entity);
                OnZoneTriggered?.Invoke(entity);
            }

            OnZoneEnter?.Invoke(entity);

            // Effects and sounds
            if (enterSound != null)
            {
                AudioSource.PlayClipAtPoint(enterSound, transform.position);
            }

            if (enterEffect != null)
            {
                var effect = Instantiate(enterEffect, entity.transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }

            if (stayEffect != null && _activeStayEffect == null)
            {
                _activeStayEffect = Instantiate(stayEffect, transform);
            }

            if (changeColorOnEnter)
            {
                UpdateVisuals(true);
            }
        }

        private void HandleExit(GameObject entity)
        {
            _entitiesInZone.Remove(entity);
            _stayTimers.Remove(entity);

            OnZoneExit?.Invoke(entity);

            // Handle speed zones
            if (zoneType == ZoneType.SpeedBoost || zoneType == ZoneType.SlowDown)
            {
                ResetEntitySpeed(entity);
            }

            // Effects and sounds
            if (exitSound != null)
            {
                AudioSource.PlayClipAtPoint(exitSound, transform.position);
            }

            if (exitEffect != null)
            {
                var effect = Instantiate(exitEffect, entity.transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }

            if (_entitiesInZone.Count == 0)
            {
                if (_activeStayEffect != null)
                {
                    Destroy(_activeStayEffect);
                    _activeStayEffect = null;
                }

                if (changeColorOnEnter)
                {
                    UpdateVisuals(false);
                }
            }
        }

        private bool CanTrigger(GameObject entity)
        {
            // Check tag
            if (!string.IsNullOrEmpty(requiredTag) && !entity.CompareTag(requiredTag))
            {
                return false;
            }

            // Check layer
            if (triggerLayers != -1 && (triggerLayers.value & (1 << entity.layer)) == 0)
            {
                return false;
            }

            return true;
        }

        private void ApplyZoneEffect(GameObject entity)
        {
            switch (zoneType)
            {
                case ZoneType.DamageZone:
                    ApplyDamage(entity);
                    break;

                case ZoneType.HealZone:
                    ApplyHeal(entity);
                    break;

                case ZoneType.Teleport:
                    TeleportEntity(entity);
                    break;

                case ZoneType.SpeedBoost:
                case ZoneType.SlowDown:
                    ApplySpeedModifier(entity);
                    break;

                case ZoneType.KillZone:
                    KillEntity(entity);
                    break;

                case ZoneType.Checkpoint:
                    ActivateCheckpoint();
                    break;

                case ZoneType.SafeZone:
                    // Safe zone just marks entity as safe, no direct effect
                    break;

                case ZoneType.Generic:
                case ZoneType.Trigger:
                    // No automatic effect, use events
                    break;
            }
        }

        private void ApplyDamage(GameObject entity)
        {
            var health = entity.GetComponent<GameKitHealth>();
            if (health != null)
            {
                health.TakeDamage(effectValue);
            }
        }

        private void ApplyHeal(GameObject entity)
        {
            var health = entity.GetComponent<GameKitHealth>();
            if (health != null)
            {
                health.Heal(effectValue);
            }
        }

        private void ApplySpeedModifier(GameObject entity)
        {
            float modifier = zoneType == ZoneType.SpeedBoost ? speedMultiplier : (1f / speedMultiplier);

            // Try to apply to various movement components
            var actor = entity.GetComponent<GameKitActor>();
            if (actor != null)
            {
                // Store original speed and apply modifier
                // This would need to be implemented in GameKitActor
            }

            var rb = entity.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity *= modifier;
            }

            var rb2d = entity.GetComponent<Rigidbody2D>();
            if (rb2d != null)
            {
                rb2d.linearVelocity *= modifier;
            }
        }

        private void ResetEntitySpeed(GameObject entity)
        {
            // Reset speed modifier when exiting zone
            // Implementation depends on how speed was stored
        }

        private void KillEntity(GameObject entity)
        {
            var health = entity.GetComponent<GameKitHealth>();
            if (health != null)
            {
                health.Kill();
            }
            else
            {
                Destroy(entity);
            }
        }

        private void UpdateVisuals(bool active)
        {
            if (_renderer != null && changeColorOnEnter)
            {
                _renderer.material.color = active ? activeColor : inactiveColor;
            }
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Finds a trigger zone by its ID.
        /// </summary>
        public static GameKitTriggerZone FindById(string id)
        {
            var zones = FindObjectsByType<GameKitTriggerZone>(FindObjectsSortMode.None);
            foreach (var zone in zones)
            {
                if (zone.ZoneId == id)
                {
                    return zone;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the currently active checkpoint.
        /// </summary>
        public static GameKitTriggerZone GetActiveCheckpoint()
        {
            var zones = FindObjectsByType<GameKitTriggerZone>(FindObjectsSortMode.None);
            foreach (var zone in zones)
            {
                if (zone.zoneType == ZoneType.Checkpoint && zone.isActiveCheckpoint)
                {
                    return zone;
                }
            }
            return null;
        }

        #endregion

        #region Editor Visualization

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Color gizmoColor = zoneType switch
            {
                ZoneType.DamageZone => Color.red,
                ZoneType.HealZone => Color.green,
                ZoneType.Checkpoint => isActiveCheckpoint ? Color.yellow : Color.gray,
                ZoneType.Teleport => Color.blue,
                ZoneType.SpeedBoost => Color.cyan,
                ZoneType.SlowDown => new Color(1f, 0.5f, 0f),
                ZoneType.KillZone => Color.black,
                ZoneType.SafeZone => Color.white,
                _ => Color.magenta
            };

            gizmoColor.a = 0.3f;
            Gizmos.color = gizmoColor;

            // Draw zone bounds
            var collider = GetComponent<Collider>();
            var collider2D = GetComponent<Collider2D>();

            if (collider is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (collider is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
            }
            else if (collider2D is BoxCollider2D box2D)
            {
                Gizmos.DrawCube(transform.position + (Vector3)box2D.offset, box2D.size);
            }
            else if (collider2D is CircleCollider2D circle2D)
            {
                Gizmos.DrawSphere(transform.position + (Vector3)circle2D.offset, circle2D.radius);
            }

            // Draw teleport destination connection
            if (zoneType == ZoneType.Teleport && teleportDestination != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, teleportDestination.position);
                Gizmos.DrawWireSphere(teleportDestination.position, 0.5f);
            }

            // Draw respawn point for checkpoints
            if (zoneType == ZoneType.Checkpoint)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(RespawnPosition, 0.3f);
            }
        }
#endif

        #endregion
    }
}
