using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// ScriptableObject for defining Machinations-inspired resource flow systems as assets.
    /// Represents resource pools, flows, converters, and triggers that can be reused across projects.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMachinations", menuName = "UnityAIForge/GameKit/Machinations Diagram", order = 2)]
    public class GameKitMachinationsAsset : ScriptableObject
    {
        [Header("Diagram Definition")]
        [Tooltip("Unique identifier for this machinations diagram")]
        [SerializeField] private string diagramId;
        
        [Tooltip("Description of this economic system")]
        [TextArea(3, 5)]
        [SerializeField] private string description;
        
        [Header("Resource Pools")]
        [Tooltip("All resource pools in this system")]
        [SerializeField] private List<ResourcePool> pools = new List<ResourcePool>();
        
        [Header("Resource Flows")]
        [Tooltip("Automatic resource generation/consumption over time")]
        [SerializeField] private List<ResourceFlowDefinition> flows = new List<ResourceFlowDefinition>();
        
        [Header("Resource Converters")]
        [Tooltip("Convert one resource to another")]
        [SerializeField] private List<ResourceConverterDefinition> converters = new List<ResourceConverterDefinition>();
        
        [Header("Resource Triggers")]
        [Tooltip("Trigger events when resources reach thresholds")]
        [SerializeField] private List<ResourceTriggerDefinition> triggers = new List<ResourceTriggerDefinition>();

        public string DiagramId => diagramId;
        public string Description => description;
        public List<ResourcePool> Pools => pools;
        public List<ResourceFlowDefinition> Flows => flows;
        public List<ResourceConverterDefinition> Converters => converters;
        public List<ResourceTriggerDefinition> Triggers => triggers;

        /// <summary>
        /// Resource pool definition with constraints
        /// </summary>
        [Serializable]
        public class ResourcePool
        {
            [Tooltip("Resource name (e.g., 'health', 'gold', 'mana')")]
            public string resourceName;
            
            [Tooltip("Initial amount when system starts")]
            public float initialAmount = 0f;
            
            [Tooltip("Minimum value constraint")]
            public float minValue = 0f;
            
            [Tooltip("Maximum value constraint")]
            public float maxValue = 100f;
            
            [Tooltip("Display color for visualization")]
            public Color displayColor = Color.white;
            
            [Tooltip("Resource icon (optional)")]
            public Sprite icon;
            
            [Tooltip("Custom data as JSON")]
            [TextArea(2, 3)]
            public string customData;
        }

        /// <summary>
        /// Automatic resource flow (source or drain)
        /// </summary>
        [Serializable]
        public class ResourceFlowDefinition
        {
            [Tooltip("Flow identifier")]
            public string flowId;
            
            [Tooltip("Target resource")]
            public string resourceName;
            
            [Tooltip("Flow rate per second (positive = generate, negative = consume)")]
            public float ratePerSecond = 1f;
            
            [Tooltip("Is this a source (generator) or drain (consumer)?")]
            public bool isSource = true;
            
            [Tooltip("Enabled by default?")]
            public bool enabledByDefault = true;
            
            [Tooltip("Flow description")]
            public string description;
        }

        /// <summary>
        /// Resource converter definition
        /// </summary>
        [Serializable]
        public class ResourceConverterDefinition
        {
            [Tooltip("Converter identifier")]
            public string converterId;
            
            [Tooltip("Input resource")]
            public string fromResource;
            
            [Tooltip("Output resource")]
            public string toResource;
            
            [Tooltip("Conversion rate (1 input → N output)")]
            public float conversionRate = 1f;
            
            [Tooltip("Input cost per conversion")]
            public float inputCost = 1f;
            
            [Tooltip("Enabled by default?")]
            public bool enabledByDefault = true;
            
            [Tooltip("Converter description")]
            public string description;
        }

        /// <summary>
        /// Resource trigger definition
        /// </summary>
        [Serializable]
        public class ResourceTriggerDefinition
        {
            [Tooltip("Trigger identifier")]
            public string triggerName;
            
            [Tooltip("Resource to monitor")]
            public string resourceName;
            
            [Tooltip("Threshold type")]
            public GameKitResourceManager.ThresholdType thresholdType;
            
            [Tooltip("Threshold value")]
            public float thresholdValue;
            
            [Tooltip("Enabled by default?")]
            public bool enabledByDefault = true;
            
            [Tooltip("Trigger description")]
            public string description;
        }

        /// <summary>
        /// Get resource pool by name
        /// </summary>
        public ResourcePool GetPool(string resourceName)
        {
            return pools.Find(p => p.resourceName == resourceName);
        }

        /// <summary>
        /// Get flows for a specific resource
        /// </summary>
        public List<ResourceFlowDefinition> GetFlowsForResource(string resourceName)
        {
            return flows.FindAll(f => f.resourceName == resourceName);
        }

        /// <summary>
        /// Get converter by ID
        /// </summary>
        public ResourceConverterDefinition GetConverter(string converterId)
        {
            return converters.Find(c => c.converterId == converterId);
        }

        /// <summary>
        /// Get trigger by name
        /// </summary>
        public ResourceTriggerDefinition GetTrigger(string triggerName)
        {
            return triggers.Find(t => t.triggerName == triggerName);
        }

        /// <summary>
        /// Validate machinations diagram configuration
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            errorMessage = "";
            
            // Check for pools
            if (pools.Count == 0)
            {
                errorMessage = "No resource pools defined";
                return false;
            }
            
            // Check for duplicate pool names
            var poolNames = new HashSet<string>();
            foreach (var pool in pools)
            {
                if (string.IsNullOrEmpty(pool.resourceName))
                {
                    errorMessage = "Found pool with empty name";
                    return false;
                }
                
                if (!poolNames.Add(pool.resourceName))
                {
                    errorMessage = $"Duplicate pool name found: {pool.resourceName}";
                    return false;
                }
                
                if (pool.minValue > pool.maxValue)
                {
                    errorMessage = $"Pool '{pool.resourceName}': minValue ({pool.minValue}) > maxValue ({pool.maxValue})";
                    return false;
                }
            }
            
            // Validate flows reference existing pools
            foreach (var flow in flows)
            {
                if (!poolNames.Contains(flow.resourceName))
                {
                    errorMessage = $"Flow '{flow.flowId}' references non-existent pool: {flow.resourceName}";
                    return false;
                }
            }
            
            // Validate converters reference existing pools
            foreach (var converter in converters)
            {
                if (!poolNames.Contains(converter.fromResource))
                {
                    errorMessage = $"Converter '{converter.converterId}' references non-existent fromResource: {converter.fromResource}";
                    return false;
                }
                
                if (!poolNames.Contains(converter.toResource))
                {
                    errorMessage = $"Converter '{converter.converterId}' references non-existent toResource: {converter.toResource}";
                    return false;
                }
                
                if (converter.conversionRate <= 0)
                {
                    errorMessage = $"Converter '{converter.converterId}' has invalid conversion rate: {converter.conversionRate}";
                    return false;
                }
            }
            
            // Validate triggers reference existing pools
            foreach (var trigger in triggers)
            {
                if (!poolNames.Contains(trigger.resourceName))
                {
                    errorMessage = $"Trigger '{trigger.triggerName}' references non-existent pool: {trigger.resourceName}";
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// Get summary statistics
        /// </summary>
        public string GetSummary()
        {
            return $"Machinations Diagram '{diagramId}':\n" +
                   $"  Pools: {pools.Count}\n" +
                   $"  Flows: {flows.Count}\n" +
                   $"  Converters: {converters.Count}\n" +
                   $"  Triggers: {triggers.Count}";
        }

        private void OnValidate()
        {
            // Auto-generate ID if empty
            if (string.IsNullOrEmpty(diagramId))
            {
                diagramId = name;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Add a new resource pool (Editor only)
        /// </summary>
        public void AddPool(string resourceName, float initialAmount = 0f, float minValue = 0f, float maxValue = 100f)
        {
            if (!pools.Exists(p => p.resourceName == resourceName))
            {
                pools.Add(new ResourcePool
                {
                    resourceName = resourceName,
                    initialAmount = initialAmount,
                    minValue = minValue,
                    maxValue = maxValue,
                    displayColor = Color.white
                });
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        /// <summary>
        /// Add a new flow (Editor only)
        /// </summary>
        public void AddFlow(string flowId, string resourceName, float ratePerSecond, bool isSource = true)
        {
            flows.Add(new ResourceFlowDefinition
            {
                flowId = flowId,
                resourceName = resourceName,
                ratePerSecond = ratePerSecond,
                isSource = isSource,
                enabledByDefault = true
            });
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Add a new converter (Editor only)
        /// </summary>
        public void AddConverter(string converterId, string fromResource, string toResource, float conversionRate, float inputCost = 1f)
        {
            converters.Add(new ResourceConverterDefinition
            {
                converterId = converterId,
                fromResource = fromResource,
                toResource = toResource,
                conversionRate = conversionRate,
                inputCost = inputCost,
                enabledByDefault = true
            });
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Add a new trigger (Editor only)
        /// </summary>
        public void AddTrigger(string triggerName, string resourceName, GameKitResourceManager.ThresholdType thresholdType, float thresholdValue)
        {
            triggers.Add(new ResourceTriggerDefinition
            {
                triggerName = triggerName,
                resourceName = resourceName,
                thresholdType = thresholdType,
                thresholdValue = thresholdValue,
                enabledByDefault = true
            });
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Remove a pool and all related flows/converters/triggers (Editor only)
        /// </summary>
        public void RemovePool(string resourceName)
        {
            pools.RemoveAll(p => p.resourceName == resourceName);
            flows.RemoveAll(f => f.resourceName == resourceName);
            converters.RemoveAll(c => c.fromResource == resourceName || c.toResource == resourceName);
            triggers.RemoveAll(t => t.resourceName == resourceName);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}

