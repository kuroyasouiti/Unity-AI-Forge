using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Resource Manager: Machinations-inspired resource flow system.
    /// Supports resource pools, automatic flows, converters, and triggers.
    /// Can use Machinations Asset for structured economic system management.
    /// Automatically added by GameKitManager when ManagerType.ResourcePool is selected.
    /// </summary>
    [AddComponentMenu("")]
    public class GameKitResourceManager : MonoBehaviour
    {
        [Header("Machinations Asset (Optional)")]
        [Tooltip("Use a Machinations diagram asset for structured resource management")]
        [SerializeField] private GameKitMachinationsAsset machinationsAsset;
        
        [Header("Resource Pool")]
        [SerializeField] private List<ResourceEntry> resources = new List<ResourceEntry>();
        
        [Header("Resource Flows")]
        [Tooltip("Automatic resource generation/consumption over time")]
        [SerializeField] private List<ResourceFlow> flows = new List<ResourceFlow>();
        
        [Header("Resource Converters")]
        [Tooltip("Convert one resource to another")]
        [SerializeField] private List<ResourceConverter> converters = new List<ResourceConverter>();
        
        [Header("Resource Triggers")]
        [Tooltip("Trigger events when resources reach thresholds")]
        [SerializeField] private List<ResourceTrigger> triggers = new List<ResourceTrigger>();
        
        [Header("Events")]
        [Tooltip("Invoked when any resource changes (resourceName, newAmount)")]
        public ResourceChangedEvent OnResourceChanged = new ResourceChangedEvent();
        
        [Tooltip("Invoked when resource reaches threshold (triggerName, resourceName, amount)")]
        public ResourceTriggerEvent OnResourceTriggered = new ResourceTriggerEvent();

        private Dictionary<string, float> lastTriggerValues = new Dictionary<string, float>();
        
        public GameKitMachinationsAsset MachinationsAsset => machinationsAsset;

        private void Start()
        {
            // Initialize from machinations asset if available
            if (machinationsAsset != null)
            {
                ApplyMachinationsAsset(machinationsAsset, true);
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            
            // Process automatic flows
            foreach (var flow in flows)
            {
                if (flow.enabled)
                {
                    ProcessFlow(flow, deltaTime);
                }
            }
            
            // Check triggers
            CheckTriggers();
        }

        public void SetResource(string resourceName, float amount)
        {
            var resource = GetOrCreateResource(resourceName);
            // Store previous value for trigger detection
            lastTriggerValues[resourceName] = resource.amount;
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
                // Store previous value for trigger detection
                lastTriggerValues[resourceName] = resource.amount;
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
            // Store previous value for trigger detection
            lastTriggerValues[resourceName] = resource.amount;
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
                // Clear existing configuration
                resources.Clear();
                flows.Clear();
                converters.Clear();
                triggers.Clear();
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
                lastTriggerValues[pool.resourceName] = pool.initialAmount;
            }

            // Apply flows
            foreach (var flowDef in asset.Flows)
            {
                var existing = flows.Find(f => f.resourceName == flowDef.resourceName && f.isSource == flowDef.isSource);
                if (existing != null)
                {
                    existing.ratePerSecond = flowDef.ratePerSecond;
                    existing.enabled = flowDef.enabledByDefault;
                }
                else
                {
                    flows.Add(new ResourceFlow
                    {
                        resourceName = flowDef.resourceName,
                        ratePerSecond = flowDef.ratePerSecond,
                        isSource = flowDef.isSource,
                        enabled = flowDef.enabledByDefault
                    });
                }
            }

            // Apply converters
            foreach (var converterDef in asset.Converters)
            {
                var existing = converters.Find(c => c.fromResource == converterDef.fromResource && c.toResource == converterDef.toResource);
                if (existing != null)
                {
                    existing.conversionRate = converterDef.conversionRate;
                    existing.inputCost = converterDef.inputCost;
                    existing.enabled = converterDef.enabledByDefault;
                }
                else
                {
                    converters.Add(new ResourceConverter
                    {
                        fromResource = converterDef.fromResource,
                        toResource = converterDef.toResource,
                        conversionRate = converterDef.conversionRate,
                        inputCost = converterDef.inputCost,
                        enabled = converterDef.enabledByDefault
                    });
                }
            }

            // Apply triggers
            foreach (var triggerDef in asset.Triggers)
            {
                var existing = triggers.Find(t => t.triggerName == triggerDef.triggerName);
                if (existing != null)
                {
                    existing.resourceName = triggerDef.resourceName;
                    existing.thresholdType = triggerDef.thresholdType;
                    existing.thresholdValue = triggerDef.thresholdValue;
                    existing.enabled = triggerDef.enabledByDefault;
                }
                else
                {
                    triggers.Add(new ResourceTrigger
                    {
                        triggerName = triggerDef.triggerName,
                        resourceName = triggerDef.resourceName,
                        thresholdType = triggerDef.thresholdType,
                        thresholdValue = triggerDef.thresholdValue,
                        enabled = triggerDef.enabledByDefault
                    });
                }
            }

            Debug.Log($"[GameKitResourceManager] Applied machinations asset: {asset.DiagramId}");
        }

        /// <summary>
        /// Export current configuration to a machinations asset.
        /// </summary>
        public GameKitMachinationsAsset ExportToAsset(string diagramId = "ExportedDiagram")
        {
            var asset = ScriptableObject.CreateInstance<GameKitMachinationsAsset>();
            
#if UNITY_EDITOR
            // Add pools
            foreach (var resource in resources)
            {
                asset.AddPool(resource.name, resource.amount, resource.minValue, resource.maxValue);
            }

            // Add flows
            foreach (var flow in flows)
            {
                asset.AddFlow($"flow_{flow.resourceName}", flow.resourceName, flow.ratePerSecond, flow.isSource);
            }

            // Add converters
            foreach (var converter in converters)
            {
                asset.AddConverter($"converter_{converter.fromResource}_to_{converter.toResource}", 
                    converter.fromResource, converter.toResource, converter.conversionRate, converter.inputCost);
            }

            // Add triggers
            foreach (var trigger in triggers)
            {
                asset.AddTrigger(trigger.triggerName, trigger.resourceName, trigger.thresholdType, trigger.thresholdValue);
            }
#endif

            return asset;
        }

        #endregion

        #region Machinations-Inspired Features

        /// <summary>
        /// Add or update a resource flow (automatic generation/consumption).
        /// </summary>
        public void AddFlow(string resourceName, float ratePerSecond, bool isSource = true)
        {
            var existingFlow = flows.Find(f => f.resourceName == resourceName && f.isSource == isSource);
            if (existingFlow != null)
            {
                existingFlow.ratePerSecond = ratePerSecond;
            }
            else
            {
                flows.Add(new ResourceFlow
                {
                    resourceName = resourceName,
                    ratePerSecond = ratePerSecond,
                    isSource = isSource,
                    enabled = true
                });
            }
        }

        /// <summary>
        /// Add a resource converter (convert one resource to another).
        /// </summary>
        public void AddConverter(string fromResource, string toResource, float conversionRate, float inputCost = 1f)
        {
            converters.Add(new ResourceConverter
            {
                fromResource = fromResource,
                toResource = toResource,
                conversionRate = conversionRate,
                inputCost = inputCost,
                enabled = true
            });
        }

        /// <summary>
        /// Convert resources manually.
        /// </summary>
        public bool Convert(string converterFrom, string converterTo, float amount = 1f)
        {
            var converter = converters.Find(c => c.fromResource == converterFrom && c.toResource == converterTo);
            if (converter == null || !converter.enabled)
                return false;

            float totalCost = converter.inputCost * amount;
            if (GetResource(converterFrom) >= totalCost)
            {
                ConsumeResource(converterFrom, totalCost);
                AddResource(converterTo, converter.conversionRate * amount);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add a resource trigger (invoke event when threshold is crossed).
        /// </summary>
        public void AddTrigger(string triggerName, string resourceName, ThresholdType thresholdType, float thresholdValue)
        {
            triggers.Add(new ResourceTrigger
            {
                triggerName = triggerName,
                resourceName = resourceName,
                thresholdType = thresholdType,
                thresholdValue = thresholdValue,
                enabled = true
            });
        }

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

        /// <summary>
        /// Enable or disable a flow.
        /// </summary>
        public void SetFlowEnabled(string resourceName, bool enabled)
        {
            var flow = flows.Find(f => f.resourceName == resourceName);
            if (flow != null)
            {
                flow.enabled = enabled;
            }
        }

        /// <summary>
        /// Enable or disable a converter.
        /// </summary>
        public void SetConverterEnabled(string fromResource, string toResource, bool enabled)
        {
            var converter = converters.Find(c => c.fromResource == fromResource && c.toResource == toResource);
            if (converter != null)
            {
                converter.enabled = enabled;
            }
        }

        private void ProcessFlow(ResourceFlow flow, float deltaTime)
        {
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
                if (resource != null && resource.amount > resource.minValue)
                {
                    ConsumeResource(flow.resourceName, flowAmount);
                }
            }
        }

        private void CheckTriggers()
        {
            foreach (var trigger in triggers)
            {
                if (!trigger.enabled)
                    continue;

                float currentValue = GetResource(trigger.resourceName);
                float lastValue = lastTriggerValues.ContainsKey(trigger.resourceName) 
                    ? lastTriggerValues[trigger.resourceName] 
                    : currentValue;

                bool shouldTrigger = false;

                switch (trigger.thresholdType)
                {
                    case ThresholdType.Above:
                        shouldTrigger = lastValue <= trigger.thresholdValue && currentValue > trigger.thresholdValue;
                        break;
                    case ThresholdType.Below:
                        shouldTrigger = lastValue >= trigger.thresholdValue && currentValue < trigger.thresholdValue;
                        break;
                    case ThresholdType.Equal:
                        shouldTrigger = Mathf.Approximately(currentValue, trigger.thresholdValue);
                        break;
                    case ThresholdType.NotEqual:
                        shouldTrigger = !Mathf.Approximately(currentValue, trigger.thresholdValue);
                        break;
                }

                if (shouldTrigger)
                {
                    OnResourceTriggered?.Invoke(trigger.triggerName, trigger.resourceName, currentValue);
                }

                lastTriggerValues[trigger.resourceName] = currentValue;
            }
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
        public class ResourceFlow
        {
            [Tooltip("Resource to flow")]
            public string resourceName;
            
            [Tooltip("Flow rate per second (positive = generate, negative = consume)")]
            public float ratePerSecond;
            
            [Tooltip("Is this a source (generator) or drain (consumer)?")]
            public bool isSource = true;
            
            [Tooltip("Enable/disable this flow")]
            public bool enabled = true;
        }

        [Serializable]
        public class ResourceConverter
        {
            [Tooltip("Input resource")]
            public string fromResource;
            
            [Tooltip("Output resource")]
            public string toResource;
            
            [Tooltip("Conversion rate (1 input → N output)")]
            public float conversionRate = 1f;
            
            [Tooltip("Input cost per conversion")]
            public float inputCost = 1f;
            
            [Tooltip("Enable/disable this converter")]
            public bool enabled = true;
        }

        [Serializable]
        public class ResourceTrigger
        {
            [Tooltip("Trigger identifier")]
            public string triggerName;
            
            [Tooltip("Resource to monitor")]
            public string resourceName;
            
            [Tooltip("Threshold type")]
            public ThresholdType thresholdType;
            
            [Tooltip("Threshold value")]
            public float thresholdValue;
            
            [Tooltip("Enable/disable this trigger")]
            public bool enabled = true;
        }

        public enum ThresholdType
        {
            Above,      // Trigger when crossing above threshold
            Below,      // Trigger when crossing below threshold
            Equal,      // Trigger when equal to threshold
            NotEqual    // Trigger when not equal to threshold
        }

        [Serializable]
        public class ResourceChangedEvent : UnityEngine.Events.UnityEvent<string, float> { }
        
        [Serializable]
        public class ResourceTriggerEvent : UnityEngine.Events.UnityEvent<string, string, float> { }
    }
}

