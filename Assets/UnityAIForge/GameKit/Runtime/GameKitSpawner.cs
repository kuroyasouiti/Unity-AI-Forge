using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Spawner component: manages spawning of prefabs with various modes.
    /// Supports interval spawning, wave-based spawning, and object pooling.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/Spawner")]
    public class GameKitSpawner : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string spawnerId;

        [Header("Prefab Settings")]
        [SerializeField] private GameObject prefab;
        [SerializeField] private Transform spawnParent;

        [Header("Spawn Mode")]
        [SerializeField] private SpawnMode spawnMode = SpawnMode.Interval;
        [SerializeField] private bool autoStart = false;

        [Header("Interval Settings")]
        [SerializeField] private float spawnInterval = 3f;
        [SerializeField] private float initialDelay = 0f;

        [Header("Limits")]
        [SerializeField] private int maxActive = 10;
        [SerializeField] private int maxTotal = -1; // -1 = unlimited

        [Header("Spawn Points")]
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
        [SerializeField] private SpawnPointMode spawnPointMode = SpawnPointMode.Sequential;

        [Header("Wave Settings")]
        [SerializeField] private List<WaveConfig> waves = new List<WaveConfig>();
        [SerializeField] private bool loopWaves = false;
        [SerializeField] private float delayBetweenWaves = 2f;

        [Header("Pooling")]
        [SerializeField] private bool usePool = true;
        [SerializeField] private int poolInitialSize = 5;

        [Header("Randomization")]
        [SerializeField] private Vector3 positionRandomness = Vector3.zero;
        [SerializeField] private Vector3 rotationRandomness = Vector3.zero;

        [Header("Events")]
        public UnityEvent<GameObject> OnSpawned = new UnityEvent<GameObject>();
        public UnityEvent<GameObject> OnDespawned = new UnityEvent<GameObject>();
        public UnityEvent<int> OnWaveStarted = new UnityEvent<int>();
        public UnityEvent<int> OnWaveCompleted = new UnityEvent<int>();
        public UnityEvent OnAllWavesCompleted = new UnityEvent();
        public UnityEvent OnMaxActiveReached = new UnityEvent();

        // State
        private bool isSpawning = false;
        private int totalSpawned = 0;
        private int currentWaveIndex = 0;
        private int currentSpawnPointIndex = 0;
        private List<GameObject> activeInstances = new List<GameObject>();
        private Queue<GameObject> pool = new Queue<GameObject>();
        private Coroutine spawnCoroutine;

        // Properties
        public string SpawnerId => spawnerId;
        public bool IsSpawning => isSpawning;
        public int ActiveCount => activeInstances.Count;
        public int TotalSpawned => totalSpawned;
        public int CurrentWaveIndex => currentWaveIndex;
        public int WaveCount => waves.Count;
        public SpawnMode Mode => spawnMode;

        private void Awake()
        {
            EnsureEventsInitialized();
        }

        private void Start()
        {
            if (usePool && prefab != null)
            {
                InitializePool();
            }

            if (autoStart)
            {
                StartSpawning();
            }
        }

        private void OnDestroy()
        {
            StopSpawning();
            ClearPool();
        }

        /// <summary>
        /// Initialize the spawner with specified settings.
        /// </summary>
        public void Initialize(string id, GameObject spawnPrefab, SpawnMode mode = SpawnMode.Interval)
        {
            spawnerId = id;
            prefab = spawnPrefab;
            spawnMode = mode;
            EnsureEventsInitialized();
        }

        private void EnsureEventsInitialized()
        {
            OnSpawned ??= new UnityEvent<GameObject>();
            OnDespawned ??= new UnityEvent<GameObject>();
            OnWaveStarted ??= new UnityEvent<int>();
            OnWaveCompleted ??= new UnityEvent<int>();
            OnAllWavesCompleted ??= new UnityEvent();
            OnMaxActiveReached ??= new UnityEvent();
        }

        #region Spawning Control

        /// <summary>
        /// Start spawning based on the configured mode.
        /// </summary>
        public void StartSpawning()
        {
            if (isSpawning || prefab == null)
                return;

            isSpawning = true;

            switch (spawnMode)
            {
                case SpawnMode.Interval:
                    spawnCoroutine = StartCoroutine(IntervalSpawnRoutine());
                    break;

                case SpawnMode.Wave:
                    spawnCoroutine = StartCoroutine(WaveSpawnRoutine());
                    break;

                case SpawnMode.Burst:
                    SpawnBurst(maxActive);
                    isSpawning = false;
                    break;
            }
        }

        /// <summary>
        /// Stop spawning.
        /// </summary>
        public void StopSpawning()
        {
            isSpawning = false;

            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
        }

        /// <summary>
        /// Start a specific wave by index.
        /// </summary>
        public void StartWave(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= waves.Count)
            {
                Debug.LogWarning($"[GameKitSpawner] Invalid wave index: {waveIndex}");
                return;
            }

            StopSpawning();
            currentWaveIndex = waveIndex;
            spawnCoroutine = StartCoroutine(SpawnWave(waves[waveIndex]));
        }

        /// <summary>
        /// Spawn a single instance immediately.
        /// </summary>
        public GameObject SpawnOne()
        {
            return SpawnOne(GetNextSpawnPosition(), GetSpawnRotation());
        }

        /// <summary>
        /// Spawn a single instance at a specific position.
        /// </summary>
        public GameObject SpawnOne(Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
                return null;

            if (maxActive > 0 && activeInstances.Count >= maxActive)
            {
                OnMaxActiveReached?.Invoke();
                return null;
            }

            if (maxTotal > 0 && totalSpawned >= maxTotal)
                return null;

            GameObject instance;

            if (usePool && pool.Count > 0)
            {
                instance = pool.Dequeue();
                instance.transform.position = position;
                instance.transform.rotation = rotation;
                instance.SetActive(true);
            }
            else
            {
                instance = Instantiate(prefab, position, rotation, spawnParent);
            }

            activeInstances.Add(instance);
            totalSpawned++;

            // Subscribe to destruction
            var despawnTracker = instance.GetComponent<DespawnTracker>();
            if (despawnTracker == null)
            {
                despawnTracker = instance.AddComponent<DespawnTracker>();
            }
            despawnTracker.Initialize(this);

            OnSpawned?.Invoke(instance);
            return instance;
        }

        /// <summary>
        /// Spawn multiple instances immediately.
        /// </summary>
        public List<GameObject> SpawnBurst(int count)
        {
            var spawned = new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                var instance = SpawnOne();
                if (instance != null)
                {
                    spawned.Add(instance);
                }
                else
                {
                    break; // Max reached
                }
            }
            return spawned;
        }

        /// <summary>
        /// Despawn an instance (return to pool or destroy).
        /// </summary>
        public void Despawn(GameObject instance)
        {
            if (instance == null)
                return;

            activeInstances.Remove(instance);
            OnDespawned?.Invoke(instance);

            if (usePool)
            {
                instance.SetActive(false);
                pool.Enqueue(instance);
            }
            else
            {
                Destroy(instance);
            }
        }

        /// <summary>
        /// Despawn all active instances.
        /// </summary>
        public void DespawnAll()
        {
            var toRemove = new List<GameObject>(activeInstances);
            foreach (var instance in toRemove)
            {
                if (instance != null)
                {
                    Despawn(instance);
                }
            }
            activeInstances.Clear();
        }

        /// <summary>
        /// Reset the spawner state.
        /// </summary>
        public void Reset()
        {
            StopSpawning();
            DespawnAll();
            totalSpawned = 0;
            currentWaveIndex = 0;
            currentSpawnPointIndex = 0;
        }

        #endregion

        #region Spawn Routines

        private IEnumerator IntervalSpawnRoutine()
        {
            if (initialDelay > 0)
            {
                yield return new WaitForSeconds(initialDelay);
            }

            while (isSpawning)
            {
                SpawnOne();
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private IEnumerator WaveSpawnRoutine()
        {
            while (isSpawning && currentWaveIndex < waves.Count)
            {
                yield return StartCoroutine(SpawnWave(waves[currentWaveIndex]));
                currentWaveIndex++;

                if (currentWaveIndex >= waves.Count)
                {
                    if (loopWaves)
                    {
                        currentWaveIndex = 0;
                    }
                    else
                    {
                        OnAllWavesCompleted?.Invoke();
                        isSpawning = false;
                    }
                }
                else if (delayBetweenWaves > 0)
                {
                    yield return new WaitForSeconds(delayBetweenWaves);
                }
            }
        }

        private IEnumerator SpawnWave(WaveConfig wave)
        {
            OnWaveStarted?.Invoke(currentWaveIndex);

            if (wave.delay > 0)
            {
                yield return new WaitForSeconds(wave.delay);
            }

            int spawned = 0;
            float interval = wave.spawnInterval > 0 ? wave.spawnInterval : 0.1f;

            while (spawned < wave.count && isSpawning)
            {
                var instance = SpawnOne();
                if (instance != null)
                {
                    spawned++;
                }
                else
                {
                    // Wait for space
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }

                if (spawned < wave.count)
                {
                    yield return new WaitForSeconds(interval);
                }
            }

            OnWaveCompleted?.Invoke(currentWaveIndex);
        }

        #endregion

        #region Spawn Position

        private Vector3 GetNextSpawnPosition()
        {
            Vector3 basePosition;

            if (spawnPoints.Count > 0)
            {
                var point = GetNextSpawnPoint();
                basePosition = point != null ? point.position : transform.position;
            }
            else
            {
                basePosition = transform.position;
            }

            // Add randomness
            if (positionRandomness != Vector3.zero)
            {
                basePosition += new Vector3(
                    UnityEngine.Random.Range(-positionRandomness.x, positionRandomness.x),
                    UnityEngine.Random.Range(-positionRandomness.y, positionRandomness.y),
                    UnityEngine.Random.Range(-positionRandomness.z, positionRandomness.z)
                );
            }

            return basePosition;
        }

        private Transform GetNextSpawnPoint()
        {
            if (spawnPoints.Count == 0)
                return null;

            Transform point;

            switch (spawnPointMode)
            {
                case SpawnPointMode.Sequential:
                    point = spawnPoints[currentSpawnPointIndex];
                    currentSpawnPointIndex = (currentSpawnPointIndex + 1) % spawnPoints.Count;
                    break;

                case SpawnPointMode.Random:
                    point = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
                    break;

                case SpawnPointMode.RandomNoRepeat:
                    if (spawnPoints.Count == 1)
                    {
                        point = spawnPoints[0];
                    }
                    else
                    {
                        int newIndex;
                        do
                        {
                            newIndex = UnityEngine.Random.Range(0, spawnPoints.Count);
                        } while (newIndex == currentSpawnPointIndex);
                        currentSpawnPointIndex = newIndex;
                        point = spawnPoints[newIndex];
                    }
                    break;

                default:
                    point = spawnPoints[0];
                    break;
            }

            return point;
        }

        private Quaternion GetSpawnRotation()
        {
            Quaternion baseRotation = prefab != null ? prefab.transform.rotation : Quaternion.identity;

            if (rotationRandomness != Vector3.zero)
            {
                baseRotation *= Quaternion.Euler(
                    UnityEngine.Random.Range(-rotationRandomness.x, rotationRandomness.x),
                    UnityEngine.Random.Range(-rotationRandomness.y, rotationRandomness.y),
                    UnityEngine.Random.Range(-rotationRandomness.z, rotationRandomness.z)
                );
            }

            return baseRotation;
        }

        #endregion

        #region Pooling

        private void InitializePool()
        {
            for (int i = 0; i < poolInitialSize; i++)
            {
                var instance = Instantiate(prefab, transform.position, Quaternion.identity, spawnParent);
                instance.SetActive(false);
                pool.Enqueue(instance);
            }
        }

        private void ClearPool()
        {
            while (pool.Count > 0)
            {
                var instance = pool.Dequeue();
                if (instance != null)
                {
                    Destroy(instance);
                }
            }
        }

        #endregion

        #region Public Setters

        public void SetPrefab(GameObject newPrefab)
        {
            prefab = newPrefab;
        }

        public void SetSpawnInterval(float interval)
        {
            spawnInterval = Mathf.Max(0.1f, interval);
        }

        public void SetMaxActive(int max)
        {
            maxActive = max;
        }

        public void AddSpawnPoint(Transform point)
        {
            if (point != null && !spawnPoints.Contains(point))
            {
                spawnPoints.Add(point);
            }
        }

        public void ClearSpawnPoints()
        {
            spawnPoints.Clear();
        }

        public void AddWave(WaveConfig wave)
        {
            waves.Add(wave);
        }

        public void ClearWaves()
        {
            waves.Clear();
        }

        #endregion

        /// <summary>
        /// Called by DespawnTracker when an instance is destroyed.
        /// </summary>
        internal void NotifyInstanceDestroyed(GameObject instance)
        {
            activeInstances.Remove(instance);
        }

        #region Enums and Classes

        public enum SpawnMode
        {
            /// <summary>Spawn at regular intervals.</summary>
            Interval,
            /// <summary>Spawn in waves with configurable counts and delays.</summary>
            Wave,
            /// <summary>Spawn all at once.</summary>
            Burst,
            /// <summary>Only spawn when triggered manually.</summary>
            Manual
        }

        public enum SpawnPointMode
        {
            /// <summary>Cycle through spawn points in order.</summary>
            Sequential,
            /// <summary>Pick a random spawn point each time.</summary>
            Random,
            /// <summary>Pick a random spawn point, avoiding repeats.</summary>
            RandomNoRepeat
        }

        [Serializable]
        public class WaveConfig
        {
            [Tooltip("Number of enemies to spawn in this wave")]
            public int count = 5;

            [Tooltip("Delay before this wave starts")]
            public float delay = 0f;

            [Tooltip("Time between spawns within this wave")]
            public float spawnInterval = 0.5f;
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw spawn points
            Gizmos.color = Color.cyan;
            foreach (var point in spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.5f);
                    Gizmos.DrawLine(transform.position, point.position);
                }
            }

            // Draw randomness area
            if (positionRandomness != Vector3.zero)
            {
                Gizmos.color = new Color(0, 1, 1, 0.3f);
                Gizmos.DrawWireCube(transform.position, positionRandomness * 2);
            }
        }
#endif
    }

    /// <summary>
    /// Internal component to track when spawned instances are destroyed.
    /// </summary>
    internal class DespawnTracker : MonoBehaviour
    {
        private GameKitSpawner spawner;

        public void Initialize(GameKitSpawner owner)
        {
            spawner = owner;
        }

        private void OnDestroy()
        {
            if (spawner != null)
            {
                spawner.NotifyInstanceDestroyed(gameObject);
            }
        }
    }
}
