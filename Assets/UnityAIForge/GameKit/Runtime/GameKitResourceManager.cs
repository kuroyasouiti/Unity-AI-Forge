using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Resource Manager: Simple resource storage and event management.
    /// Stores resource amounts and fires events when resources change.
    /// Can use Machinations Asset to initialize resource pools.
    /// Automatically added by GameKitManager when ManagerType.ResourcePool is selected.
    /// 
    /// Note: Complex logic (flows, converters, triggers) should be implemented externally
    /// or via GameKitMachinationsAsset with a separate controller component.
    /// </summary>
    [AddComponentMenu("")]
    public class GameKitResourceManager : MonoBehaviour
    {
        [Header("Machinations Asset (Optional)")]
        [Tooltip("Use a Machinations diagram asset to initialize resource pools")]
        [SerializeField] private GameKitMachinationsAsset machinationsAsset;
        
        [Header("Diagram Execution")]
        [Tooltip("Automatically process flows from machinations asset")]
        [SerializeField] private bool autoProcessFlows = false;
        
        [Tooltip("Automatically check triggers from machinations asset")]
        [SerializeField] private bool autoCheckTriggers = false;
        
        [Header("Resource Pool")]
        [SerializeField] private List<ResourceEntry> resources = new List<ResourceEntry>();
        
        [Header("Events")]
        [Tooltip("Invoked when any resource changes (resourceName, newAmount)")]
        public ResourceChangedEvent OnResourceChanged = new ResourceChangedEvent();
        
        [Tooltip("Invoked when a trigger from machinations asset is fired")]
        public TriggerFiredEvent OnTriggerFired = new TriggerFiredEvent();
        
        public GameKitMachinationsAsset MachinationsAsset => machinationsAsset;
        
        private Dictionary<string, float> lastTriggerValues = new Dictionary<string, float>();
        private Dictionary<string, bool> flowStates = new Dictionary<string, bool>();

        private void Start()
        {
            // Initialize from machinations asset if available
            if (machinationsAsset != null)
            {
                ApplyMachinationsAsset(machinationsAsset, true);
                InitializeFlowStates();
            }
        }

        private void Update()
        {
            if (machinationsAsset == null)
                return;

            float deltaTime = Time.deltaTime;

            // Process flows if enabled
            if (autoProcessFlows)
            {
                ProcessDiagramFlows(deltaTime);
            }

            // Check triggers if enabled
            if (autoCheckTriggers)
            {
                CheckDiagramTriggers();
            }
        }

        private void InitializeFlowStates()
        {
            if (machinationsAsset == null)
                return;

            flowStates.Clear();
            foreach (var flow in machinationsAsset.Flows)
            {
                flowStates[flow.flowId] = flow.enabledByDefault;
            }
        }

        public void SetResource(string resourceName, float amount)
        {
            var resource = GetOrCreateResource(resourceName);
            resource.amount = Mathf.Clamp(amount, resource.minValue, resource.maxValue);
            OnResourceChanged?.Invoke(resourceName, resource.amount);
        }

        public float GetResource(string resourceName, float defaultValue = 0f)
        {
            var resource = resources.Find(r => r.name == resourceName);
            return resource != null ? resource.amount : defaultValue;
        }

        public bool ConsumeResource(string resourceName, float amount)
        {
            var resource = resources.Find(r => r.name == resourceName);
            if (resource != null && resource.amount >= amount)
            {
                float newAmount = resource.amount - amount;
                resource.amount = Mathf.Max(newAmount, resource.minValue);
                OnResourceChanged?.Invoke(resourceName, resource.amount);
                return true;
            }
            return false;
        }

        public void AddResource(string resourceName, float amount)
        {
            var resource = GetOrCreateResource(resourceName);
            resource.amount = Mathf.Clamp(resource.amount + amount, resource.minValue, resource.maxValue);
            OnResourceChanged?.Invoke(resourceName, resource.amount);
        }

        private ResourceEntry GetOrCreateResource(string resourceName)
        {
            var resource = resources.Find(r => r.name == resourceName);
            if (resource == null)
            {
                resource = new ResourceEntry 
                { 
                    name = resourceName, 
                    amount = 0, 
                    minValue = float.MinValue,
                    maxValue = float.MaxValue
                };
                resources.Add(resource);
            }
            return resource;
        }

        public Dictionary<string, float> GetAllResources()
        {
            var result = new Dictionary<string, float>();
            foreach (var resource in resources)
            {
                result[resource.name] = resource.amount;
            }
            return result;
        }

        public bool HasResource(string resourceName, float minAmount)
        {
            return GetResource(resourceName) >= minAmount;
        }

        public void ClearAllResources()
        {
            resources.Clear();
        }

        #region Machinations Diagram Execution

        /// <summary>
        /// Process all flows from the machinations asset.
        /// Call this manually or enable autoProcessFlows for automatic execution.
        /// </summary>
        public void ProcessDiagramFlows(float deltaTime)
        {
            if (machinationsAsset == null)
                return;

            foreach (var flow in machinationsAsset.Flows)
            {
                // Check if flow is enabled
                if (!flowStates.TryGetValue(flow.flowId, out bool enabled) || !enabled)
                    continue;

                float flowAmount = flow.ratePerSecond * deltaTime;

                if (flow.isSource)
                {
                    // Source: generate resource
                    AddResource(flow.resourceName, flowAmount);
                }
                else
                {
                    // Drain: consume resource
                    var resource = resources.Find(r => r.name == flow.resourceName);
                    if (resource != null)
                    {
                        float newAmount = resource.amount - flowAmount;
                        resource.amount = Mathf.Max(newAmount, resource.minValue);
                        OnResourceChanged?.Invoke(flow.resourceName, resource.amount);
                    }
                }
            }
        }

        /// <summary>
        /// Check all triggers from the machinations asset.
        /// Call this manually or enable autoCheckTriggers for automatic execution.
        /// </summary>
        public void CheckDiagramTriggers()
        {
            if (machinationsAsset == null)
                return;

            foreach (var trigger in machinationsAsset.Triggers)
            {
                if (!trigger.enabledByDefault)
                    continue;

                float currentValue = GetResource(trigger.resourceName);
                float lastValue = lastTriggerValues.ContainsKey(trigger.resourceName)
                    ? lastTriggerValues[trigger.resourceName]
                    : currentValue;

                bool shouldTrigger = false;

                switch (trigger.thresholdType)
                {
                    case GameKitMachinationsAsset.ThresholdType.Above:
                        shouldTrigger = lastValue <= trigger.thresholdValue && currentValue > trigger.thresholdValue;
                        break;
                    case GameKitMachinationsAsset.ThresholdType.Below:
                        shouldTrigger = lastValue >= trigger.thresholdValue && currentValue < trigger.thresholdValue;
                        break;
                    case GameKitMachinationsAsset.ThresholdType.Equal:
                        shouldTrigger = Mathf.Approximately(currentValue, trigger.thresholdValue);
                        break;
                    case GameKitMachinationsAsset.ThresholdType.NotEqual:
                        shouldTrigger = !Mathf.Approximately(currentValue, trigger.thresholdValue);
                        break;
                }

                if (shouldTrigger)
                {
                    OnTriggerFired?.Invoke(trigger.triggerName, trigger.resourceName, currentValue);
                }

                lastTriggerValues[trigger.resourceName] = currentValue;
            }
        }

        /// <summary>
        /// Execute a specific converter from the machinations asset.
        /// </summary>
        public bool ExecuteConverter(string converterId, float amount = 1f)
        {
            if (machinationsAsset == null)
            {
                Debug.LogWarning("[GameKitResourceManager] No machinations asset assigned");
                return false;
            }

            var converter = machinationsAsset.GetConverter(converterId);
            if (converter == null)
            {
                Debug.LogWarning($"[GameKitResourceManager] Converter '{converterId}' not found in asset");
                return false;
            }

            if (!converter.enabledByDefault)
            {
                Debug.LogWarning($"[GameKitResourceManager] Converter '{converterId}' is disabled");
                return false;
            }

            float totalCost = converter.inputCost * amount;
            if (GetResource(converter.fromResource) >= totalCost)
            {
                ConsumeResource(converter.fromResource, totalCost);
                AddResource(converter.toResource, converter.conversionRate * amount);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Enable or disable a specific flow by ID.
        /// </summary>
        public void SetFlowEnabled(string flowId, bool enabled)
        {
            if (flowStates.ContainsKey(flowId))
            {
                flowStates[flowId] = enabled;
            }
            else
            {
                Debug.LogWarning($"[GameKitResourceManager] Flow '{flowId}' not found");
            }
        }

        /// <summary>
        /// Check if a flow is currently enabled.
        /// </summary>
        public bool IsFlowEnabled(string flowId)
        {
            return flowStates.TryGetValue(flowId, out bool enabled) && enabled;
        }

        /// <summary>
        /// Get all flow states.
        /// </summary>
        public Dictionary<string, bool> GetFlowStates()
        {
            return new Dictionary<string, bool>(flowStates);
        }

        #endregion

        #region Machinations Asset Management

        /// <summary>
        /// Apply a machinations asset to this resource manager.
        /// Initializes resource pools and prepares flows/converters/triggers.
        /// </summary>
        public void ApplyMachinationsAsset(GameKitMachinationsAsset asset, bool resetExisting = false)
        {
            if (asset == null)
            {
                Debug.LogWarning("[GameKitResourceManager] Cannot apply null machinations asset");
                return;
            }

            // Validate asset
            if (!asset.Validate(out string errorMessage))
            {
                Debug.LogError($"[GameKitResourceManager] Invalid machinations asset: {errorMessage}");
                return;
            }

            machinationsAsset = asset;

            if (resetExisting)
            {
                resources.Clear();
            }

            // Apply resource pools
            foreach (var pool in asset.Pools)
            {
                var existing = resources.Find(r => r.name == pool.resourceName);
                if (existing != null)
                {
                    // Update existing
                    existing.amount = pool.initialAmount;
                    existing.minValue = pool.minValue;
                    existing.maxValue = pool.maxValue;
                }
                else
                {
                    // Create new
                    resources.Add(new ResourceEntry
                    {
                        name = pool.resourceName,
                        amount = pool.initialAmount,
                        minValue = pool.minValue,
                        maxValue = pool.maxValue
                    });
                }
                
                // Initialize trigger tracking
                lastTriggerValues[pool.resourceName] = pool.initialAmount;
            }

            // Initialize flow states
            InitializeFlowStates();

            Debug.Log($"[GameKitResourceManager] Applied machinations asset: {asset.DiagramId} " +
                      $"({asset.Pools.Count} pools, {asset.Flows.Count} flows, " +
                      $"{asset.Converters.Count} converters, {asset.Triggers.Count} triggers)");
        }

        /// <summary>
        /// Export current resource pools to a machinations asset.
        /// </summary>
        public GameKitMachinationsAsset ExportToAsset(string diagramId = "ExportedDiagram")
        {
            var asset = ScriptableObject.CreateInstance<GameKitMachinationsAsset>();
            
#if UNITY_EDITOR
            // Add pools only
            foreach (var resource in resources)
            {
                asset.AddPool(resource.name, resource.amount, resource.minValue, resource.maxValue);
            }
#endif

            return asset;
        }

        #endregion

        #region Resource Constraints

        /// <summary>
        /// Set resource constraints (min/max values).
        /// </summary>
        public void SetResourceConstraints(string resourceName, float minValue, float maxValue)
        {
            var resource = GetOrCreateResource(resourceName);
            resource.minValue = minValue;
            resource.maxValue = maxValue;
            resource.amount = Mathf.Clamp(resource.amount, minValue, maxValue);
        }

        #endregion

        #region State Persistence

        /// <summary>
        /// Export current resource state to a serializable object.
        /// </summary>
        public ResourceState ExportState(string managerId = null)
        {
            var state = new ResourceState
            {
                managerId = managerId ?? $"ResourceManager_{GetInstanceID()}",
                timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            // Export resources
            foreach (var resource in resources)
            {
                state.resources.Add(new ResourceState.ResourceSnapshot
                {
                    name = resource.name,
                    amount = resource.amount,
                    minValue = resource.minValue,
                    maxValue = resource.maxValue
                });
            }

            // Export flow states
            foreach (var kvp in flowStates)
            {
                state.flowStates.Add(new ResourceState.FlowStateSnapshot
                {
                    flowId = kvp.Key,
                    enabled = kvp.Value
                });
            }

            // Export machinations asset reference
#if UNITY_EDITOR
            if (machinationsAsset != null)
            {
                state.machinationsAssetPath = UnityEditor.AssetDatabase.GetAssetPath(machinationsAsset);
            }
#endif

            return state;
        }

        /// <summary>
        /// Import resource state from a serializable object.
        /// </summary>
        public void ImportState(ResourceState state, bool resetExisting = true)
        {
            if (state == null)
            {
                Debug.LogWarning("[GameKitResourceManager] Cannot import null state");
                return;
            }

            if (resetExisting)
            {
                resources.Clear();
                flowStates.Clear();
            }

            // Import resources
            foreach (var snapshot in state.resources)
            {
                var existing = resources.Find(r => r.name == snapshot.name);
                if (existing != null)
                {
                    existing.amount = snapshot.amount;
                    existing.minValue = snapshot.minValue;
                    existing.maxValue = snapshot.maxValue;
                }
                else
                {
                    resources.Add(new ResourceEntry
                    {
                        name = snapshot.name,
                        amount = snapshot.amount,
                        minValue = snapshot.minValue,
                        maxValue = snapshot.maxValue
                    });
                }
            }

            // Import flow states
            foreach (var flowSnapshot in state.flowStates)
            {
                flowStates[flowSnapshot.flowId] = flowSnapshot.enabled;
            }

            // Load machinations asset reference if available
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(state.machinationsAssetPath))
            {
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameKitMachinationsAsset>(state.machinationsAssetPath);
                if (asset != null)
                {
                    machinationsAsset = asset;
                }
            }
#endif

            Debug.Log($"[GameKitResourceManager] Imported state: {state.resources.Count} resources, {state.flowStates.Count} flow states");
        }

        /// <summary>
        /// Save resource state to JSON file.
        /// </summary>
        public void SaveStateToFile(string filePath, string managerId = null)
        {
            try
            {
                var state = ExportState(managerId);
                string json = JsonUtility.ToJson(state, true);
                
                // Ensure directory exists
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(filePath, json);
                Debug.Log($"[GameKitResourceManager] State saved to: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameKitResourceManager] Failed to save state: {e.Message}");
            }
        }

        /// <summary>
        /// Load resource state from JSON file.
        /// </summary>
        public bool LoadStateFromFile(string filePath, bool resetExisting = true)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[GameKitResourceManager] State file not found: {filePath}");
                    return false;
                }

                string json = File.ReadAllText(filePath);
                var state = JsonUtility.FromJson<ResourceState>(json);
                
                ImportState(state, resetExisting);
                Debug.Log($"[GameKitResourceManager] State loaded from: {filePath}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameKitResourceManager] Failed to load state: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Save resource state to PlayerPrefs.
        /// </summary>
        public void SaveStateToPlayerPrefs(string key = "GameKitResourceState")
        {
            try
            {
                var state = ExportState();
                string json = JsonUtility.ToJson(state);
                PlayerPrefs.SetString(key, json);
                PlayerPrefs.Save();
                Debug.Log($"[GameKitResourceManager] State saved to PlayerPrefs: {key}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameKitResourceManager] Failed to save state to PlayerPrefs: {e.Message}");
            }
        }

        /// <summary>
        /// Load resource state from PlayerPrefs.
        /// </summary>
        public bool LoadStateFromPlayerPrefs(string key = "GameKitResourceState", bool resetExisting = true)
        {
            try
            {
                if (!PlayerPrefs.HasKey(key))
                {
                    Debug.LogWarning($"[GameKitResourceManager] PlayerPrefs key not found: {key}");
                    return false;
                }

                string json = PlayerPrefs.GetString(key);
                var state = JsonUtility.FromJson<ResourceState>(json);
                
                ImportState(state, resetExisting);
                Debug.Log($"[GameKitResourceManager] State loaded from PlayerPrefs: {key}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameKitResourceManager] Failed to load state from PlayerPrefs: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clear saved state from PlayerPrefs.
        /// </summary>
        public void ClearStateFromPlayerPrefs(string key = "GameKitResourceState")
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Debug.Log($"[GameKitResourceManager] Cleared state from PlayerPrefs: {key}");
        }

        /// <summary>
        /// Get the default save file path for this manager.
        /// </summary>
        public static string GetDefaultSavePath(string managerId = "default")
        {
            return Path.Combine(Application.persistentDataPath, $"ResourceState_{managerId}.json");
        }

        #endregion

        [Serializable]
        public class ResourceEntry
        {
            [Tooltip("Resource identifier (e.g., 'health', 'gold', 'mana')")]
            public string name;
            
            [Tooltip("Current amount")]
            public float amount;
            
            [Tooltip("Minimum value (default: -Infinity)")]
            public float minValue = float.MinValue;
            
            [Tooltip("Maximum value (default: +Infinity)")]
            public float maxValue = float.MaxValue;
        }

        /// <summary>
        /// Serializable resource state for save/load functionality.
        /// </summary>
        [Serializable]
        public class ResourceState
        {
            public string managerId;
            public long timestamp;
            public List<ResourceSnapshot> resources = new List<ResourceSnapshot>();
            public List<FlowStateSnapshot> flowStates = new List<FlowStateSnapshot>();
            public string machinationsAssetPath;

            [Serializable]
            public class ResourceSnapshot
            {
                public string name;
                public float amount;
                public float minValue;
                public float maxValue;
            }

            [Serializable]
            public class FlowStateSnapshot
            {
                public string flowId;
                public bool enabled;
            }
        }

        [Serializable]
        public class ResourceChangedEvent : UnityEngine.Events.UnityEvent<string, float> { }
        
        [Serializable]
        public class TriggerFiredEvent : UnityEngine.Events.UnityEvent<string, string, float> { }
    }
}

