using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit VFX: Visual effects wrapper for particle systems.
    /// Provides pooling, lifecycle management, and easy integration with game events.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/Presentation/VFX")]
    public class GameKitVFX : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string vfxId;

        [Header("Particle System")]
        [SerializeField] private ParticleSystem particleSystem;
        [SerializeField] private GameObject particlePrefab;

        [Header("Settings")]
        [SerializeField] private bool autoPlay = false;
        [SerializeField] private bool loop = false;
        [SerializeField] private bool usePooling = true;
        [SerializeField] private int poolSize = 5;
        [SerializeField] private bool attachToParent = false;

        [Header("Scale & Duration")]
        [SerializeField] private float durationMultiplier = 1f;
        [SerializeField] private float sizeMultiplier = 1f;
        [SerializeField] private float emissionMultiplier = 1f;

        [Header("Events")]
        public UnityEvent OnVFXStarted = new UnityEvent();
        public UnityEvent OnVFXStopped = new UnityEvent();

        // Registry
        private static readonly Dictionary<string, GameKitVFX> _registry = new Dictionary<string, GameKitVFX>();

        // Pooling
        private Queue<GameObject> _pool = new Queue<GameObject>();
        private List<GameObject> _activeInstances = new List<GameObject>();

        // State
        private bool _isPlaying;

        public string VFXId => vfxId;
        public bool IsPlaying => _isPlaying || (particleSystem != null && particleSystem.isPlaying);
        public ParticleSystem ParticleSystem => particleSystem;

        /// <summary>
        /// Find VFX by ID.
        /// </summary>
        public static GameKitVFX FindById(string id)
        {
            return _registry.TryGetValue(id, out var vfx) ? vfx : null;
        }

        /// <summary>
        /// Play VFX by ID at position (static helper).
        /// </summary>
        public static void Play(string id, Vector3 position)
        {
            var vfx = FindById(id);
            if (vfx != null)
            {
                vfx.PlayAt(position);
            }
        }

        private void Awake()
        {
            EnsureEventsInitialized();

            if (particleSystem == null)
            {
                particleSystem = GetComponent<ParticleSystem>();
            }

            if (usePooling && particlePrefab != null)
            {
                InitializePool();
            }
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(vfxId))
            {
                _registry[vfxId] = this;
            }

            if (autoPlay)
            {
                PlayVFX();
            }
        }

        private void OnDisable()
        {
            if (!string.IsNullOrEmpty(vfxId))
            {
                _registry.Remove(vfxId);
            }

            StopVFX();
        }

        private void EnsureEventsInitialized()
        {
            OnVFXStarted ??= new UnityEvent();
            OnVFXStopped ??= new UnityEvent();
        }

        private void InitializePool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                var instance = CreatePooledInstance();
                instance.SetActive(false);
                _pool.Enqueue(instance);
            }
        }

        private GameObject CreatePooledInstance()
        {
            var instance = Instantiate(particlePrefab, transform);
            instance.name = $"{particlePrefab.name}_Pooled_{_pool.Count}";
            return instance;
        }

        /// <summary>
        /// Initialize the VFX component.
        /// </summary>
        public void Initialize(string id, ParticleSystem ps)
        {
            vfxId = id;
            particleSystem = ps;

            if (!string.IsNullOrEmpty(vfxId))
            {
                _registry[vfxId] = this;
            }

            EnsureEventsInitialized();
        }

        /// <summary>
        /// Play the VFX at the current position.
        /// </summary>
        public void PlayVFX()
        {
            if (particleSystem != null)
            {
                ApplyMultipliers();
                particleSystem.Play();
                _isPlaying = true;
                OnVFXStarted?.Invoke();
            }
        }

        /// <summary>
        /// Play the VFX at a specific position.
        /// </summary>
        public void PlayAt(Vector3 position)
        {
            if (usePooling && particlePrefab != null)
            {
                PlayPooledAt(position, Quaternion.identity);
            }
            else if (particleSystem != null)
            {
                transform.position = position;
                PlayVFX();
            }
        }

        /// <summary>
        /// Play the VFX at a specific position and rotation.
        /// </summary>
        public void PlayAt(Vector3 position, Quaternion rotation)
        {
            if (usePooling && particlePrefab != null)
            {
                PlayPooledAt(position, rotation);
            }
            else if (particleSystem != null)
            {
                transform.position = position;
                transform.rotation = rotation;
                PlayVFX();
            }
        }

        /// <summary>
        /// Play the VFX attached to a transform.
        /// </summary>
        public void PlayAttached(Transform parent)
        {
            if (usePooling && particlePrefab != null)
            {
                var instance = GetPooledInstance();
                if (instance != null)
                {
                    instance.transform.SetParent(parent);
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.identity;
                    instance.SetActive(true);

                    var ps = instance.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        ps.Play();
                        StartCoroutine(ReturnToPoolAfterComplete(instance, ps));
                    }

                    _activeInstances.Add(instance);
                }
            }
            else if (particleSystem != null)
            {
                if (attachToParent)
                {
                    transform.SetParent(parent);
                    transform.localPosition = Vector3.zero;
                }
                else
                {
                    transform.position = parent.position;
                }
                PlayVFX();
            }
        }

        private void PlayPooledAt(Vector3 position, Quaternion rotation)
        {
            var instance = GetPooledInstance();
            if (instance != null)
            {
                instance.transform.SetParent(null);
                instance.transform.position = position;
                instance.transform.rotation = rotation;
                instance.SetActive(true);

                var ps = instance.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                    StartCoroutine(ReturnToPoolAfterComplete(instance, ps));
                }

                _activeInstances.Add(instance);
                OnVFXStarted?.Invoke();
            }
        }

        private GameObject GetPooledInstance()
        {
            if (_pool.Count > 0)
            {
                return _pool.Dequeue();
            }

            // Expand pool if needed
            if (particlePrefab != null)
            {
                return CreatePooledInstance();
            }

            return null;
        }

        private System.Collections.IEnumerator ReturnToPoolAfterComplete(GameObject instance, ParticleSystem ps)
        {
            // Wait for particle system to finish
            while (ps.isPlaying || ps.particleCount > 0)
            {
                yield return null;
            }

            ReturnToPool(instance);
        }

        private void ReturnToPool(GameObject instance)
        {
            instance.SetActive(false);
            instance.transform.SetParent(transform);
            _activeInstances.Remove(instance);
            _pool.Enqueue(instance);
        }

        /// <summary>
        /// Stop the VFX.
        /// </summary>
        public void StopVFX()
        {
            if (particleSystem != null)
            {
                particleSystem.Stop();
            }

            _isPlaying = false;
            OnVFXStopped?.Invoke();
        }

        /// <summary>
        /// Stop the VFX immediately and clear particles.
        /// </summary>
        public void StopImmediate()
        {
            if (particleSystem != null)
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            // Return all active pooled instances
            foreach (var instance in _activeInstances.ToArray())
            {
                var ps = instance.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                ReturnToPool(instance);
            }

            _isPlaying = false;
            OnVFXStopped?.Invoke();
        }

        /// <summary>
        /// Set the duration multiplier.
        /// </summary>
        public void SetDurationMultiplier(float multiplier)
        {
            durationMultiplier = multiplier;
        }

        /// <summary>
        /// Set the size multiplier.
        /// </summary>
        public void SetSizeMultiplier(float multiplier)
        {
            sizeMultiplier = multiplier;
        }

        /// <summary>
        /// Set the emission rate multiplier.
        /// </summary>
        public void SetEmissionMultiplier(float multiplier)
        {
            emissionMultiplier = multiplier;
        }

        private void ApplyMultipliers()
        {
            if (particleSystem == null) return;

            var main = particleSystem.main;

            // Apply size multiplier
            if (!Mathf.Approximately(sizeMultiplier, 1f))
            {
                main.startSizeMultiplier *= sizeMultiplier;
            }

            // Apply emission multiplier
            if (!Mathf.Approximately(emissionMultiplier, 1f))
            {
                var emission = particleSystem.emission;
                emission.rateOverTimeMultiplier *= emissionMultiplier;
            }

            // Apply duration multiplier
            if (!Mathf.Approximately(durationMultiplier, 1f))
            {
                main.simulationSpeed = 1f / durationMultiplier;
            }
        }

        /// <summary>
        /// Set the particle color.
        /// </summary>
        public void SetColor(Color color)
        {
            if (particleSystem != null)
            {
                var main = particleSystem.main;
                main.startColor = color;
            }
        }

        /// <summary>
        /// Set whether to loop the effect.
        /// </summary>
        public void SetLoop(bool shouldLoop)
        {
            loop = shouldLoop;
            if (particleSystem != null)
            {
                var main = particleSystem.main;
                main.loop = shouldLoop;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (particleSystem == null)
            {
                particleSystem = GetComponent<ParticleSystem>();
            }
        }
#endif
    }
}
