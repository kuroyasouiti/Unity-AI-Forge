using System;
using System.Collections.Generic;
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
        
        [Header("Resource Pool")]
        [SerializeField] private List<ResourceEntry> resources = new List<ResourceEntry>();
        
        [Header("Events")]
        [Tooltip("Invoked when any resource changes (resourceName, newAmount)")]
        public ResourceChangedEvent OnResourceChanged = new ResourceChangedEvent();
        
        public GameKitMachinationsAsset MachinationsAsset => machinationsAsset;

        private void Start()
        {
            // Initialize from machinations asset if available
            if (machinationsAsset != null)
            {
                ApplyMachinationsAsset(machinationsAsset, true);
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

        #region Machinations Asset Management

        /// <summary>
        /// Apply a machinations asset to this resource manager.
        /// Only applies resource pools (initial values and constraints).
        /// Other features (flows, converters, triggers) should be implemented externally.
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

            // Apply resource pools only
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
            }

            Debug.Log($"[GameKitResourceManager] Applied resource pools from machinations asset: {asset.DiagramId}");
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

        [Serializable]
        public class ResourceChangedEvent : UnityEngine.Events.UnityEvent<string, float> { }
    }
}

