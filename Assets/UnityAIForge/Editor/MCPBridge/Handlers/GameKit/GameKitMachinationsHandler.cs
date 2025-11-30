using System;
using System.Collections.Generic;
using System.IO;
using MCP.Editor.Base;
using UnityAIForge.GameKit;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Machinations handler: create and manage Machinations diagram assets.
    /// Allows creating ScriptableObject assets for reusable resource flow systems.
    /// </summary>
    public class GameKitMachinationsHandler : BaseCommandHandler
    {
        private static readonly string[] Operations = { "create", "update", "inspect", "delete", "apply", "export" };

        public override string Category => "gamekitMachinations";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateMachinationsAsset(payload),
                "update" => UpdateMachinationsAsset(payload),
                "inspect" => InspectMachinationsAsset(payload),
                "delete" => DeleteMachinationsAsset(payload),
                "apply" => ApplyMachinationsToManager(payload),
                "export" => ExportMachinationsFromManager(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit Machinations operation: {operation}"),
            };
        }

        #region Create Asset

        private object CreateMachinationsAsset(Dictionary<string, object> payload)
        {
            var diagramId = GetString(payload, "diagramId") ?? $"Machinations_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var assetPath = GetString(payload, "assetPath") ?? $"Assets/{diagramId}.asset";
            var description = GetString(payload, "description") ?? "";

            // Ensure .asset extension
            if (!assetPath.EndsWith(".asset"))
            {
                assetPath += ".asset";
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create asset
            var asset = ScriptableObject.CreateInstance<GameKitMachinationsAsset>();
            
            // Set diagram ID using reflection (since it's private)
            var diagramIdField = typeof(GameKitMachinationsAsset).GetField("diagramId", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            diagramIdField?.SetValue(asset, diagramId);
            
            var descriptionField = typeof(GameKitMachinationsAsset).GetField("description",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            descriptionField?.SetValue(asset, description);

            // Add pools
            if (payload.TryGetValue("pools", out var poolsObj) && poolsObj is List<object> poolsList)
            {
                foreach (var poolObj in poolsList)
                {
                    if (poolObj is Dictionary<string, object> poolDict)
                    {
                        var resourceName = GetString(poolDict, "resourceName");
                        var initialAmount = GetFloat(poolDict, "initialAmount", 0f);
                        var minValue = GetFloat(poolDict, "minValue", 0f);
                        var maxValue = GetFloat(poolDict, "maxValue", 100f);
                        
                        if (!string.IsNullOrEmpty(resourceName))
                        {
                            asset.AddPool(resourceName, initialAmount, minValue, maxValue);
                        }
                    }
                }
            }

            // Add flows
            if (payload.TryGetValue("flows", out var flowsObj) && flowsObj is List<object> flowsList)
            {
                foreach (var flowObj in flowsList)
                {
                    if (flowObj is Dictionary<string, object> flowDict)
                    {
                        var flowId = GetString(flowDict, "flowId");
                        var resourceName = GetString(flowDict, "resourceName");
                        var ratePerSecond = GetFloat(flowDict, "ratePerSecond", 1f);
                        var isSource = GetBool(flowDict, "isSource", true);
                        
                        if (!string.IsNullOrEmpty(flowId) && !string.IsNullOrEmpty(resourceName))
                        {
                            asset.AddFlow(flowId, resourceName, ratePerSecond, isSource);
                        }
                    }
                }
            }

            // Add converters
            if (payload.TryGetValue("converters", out var convertersObj) && convertersObj is List<object> convertersList)
            {
                foreach (var converterObj in convertersList)
                {
                    if (converterObj is Dictionary<string, object> converterDict)
                    {
                        var converterId = GetString(converterDict, "converterId");
                        var fromResource = GetString(converterDict, "fromResource");
                        var toResource = GetString(converterDict, "toResource");
                        var conversionRate = GetFloat(converterDict, "conversionRate", 1f);
                        var inputCost = GetFloat(converterDict, "inputCost", 1f);
                        
                        if (!string.IsNullOrEmpty(converterId) && !string.IsNullOrEmpty(fromResource) && !string.IsNullOrEmpty(toResource))
                        {
                            asset.AddConverter(converterId, fromResource, toResource, conversionRate, inputCost);
                        }
                    }
                }
            }

            // Add triggers
            if (payload.TryGetValue("triggers", out var triggersObj) && triggersObj is List<object> triggersList)
            {
                foreach (var triggerObj in triggersList)
                {
                    if (triggerObj is Dictionary<string, object> triggerDict)
                    {
                        var triggerName = GetString(triggerDict, "triggerName");
                        var resourceName = GetString(triggerDict, "resourceName");
                        var thresholdTypeStr = GetString(triggerDict, "thresholdType", "Above");
                        var thresholdValue = GetFloat(triggerDict, "thresholdValue", 0f);
                        
                        var thresholdType = ParseThresholdType(thresholdTypeStr);
                        
                        if (!string.IsNullOrEmpty(triggerName) && !string.IsNullOrEmpty(resourceName))
                        {
                            asset.AddTrigger(triggerName, resourceName, thresholdType, thresholdValue);
                        }
                    }
                }
            }

            // Save asset
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return CreateSuccessResponse(("diagramId", diagramId), ("assetPath", assetPath));
        }

        #endregion

        #region Update Asset

        private object UpdateMachinationsAsset(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("assetPath is required for update.");
            }

            var asset = AssetDatabase.LoadAssetAtPath<GameKitMachinationsAsset>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"Machinations asset not found at: {assetPath}");
            }

            // Update pools
            if (payload.TryGetValue("addPools", out var addPoolsObj) && addPoolsObj is List<object> addPoolsList)
            {
                foreach (var poolObj in addPoolsList)
                {
                    if (poolObj is Dictionary<string, object> poolDict)
                    {
                        var resourceName = GetString(poolDict, "resourceName");
                        var initialAmount = GetFloat(poolDict, "initialAmount", 0f);
                        var minValue = GetFloat(poolDict, "minValue", 0f);
                        var maxValue = GetFloat(poolDict, "maxValue", 100f);
                        
                        if (!string.IsNullOrEmpty(resourceName))
                        {
                            asset.AddPool(resourceName, initialAmount, minValue, maxValue);
                        }
                    }
                }
            }

            // Remove pools
            if (payload.TryGetValue("removePools", out var removePoolsObj) && removePoolsObj is List<object> removePoolsList)
            {
                foreach (var poolName in removePoolsList)
                {
                    asset.RemovePool(poolName.ToString());
                }
            }

            // Add flows
            if (payload.TryGetValue("addFlows", out var addFlowsObj) && addFlowsObj is List<object> addFlowsList)
            {
                foreach (var flowObj in addFlowsList)
                {
                    if (flowObj is Dictionary<string, object> flowDict)
                    {
                        var flowId = GetString(flowDict, "flowId");
                        var resourceName = GetString(flowDict, "resourceName");
                        var ratePerSecond = GetFloat(flowDict, "ratePerSecond", 1f);
                        var isSource = GetBool(flowDict, "isSource", true);
                        
                        if (!string.IsNullOrEmpty(flowId) && !string.IsNullOrEmpty(resourceName))
                        {
                            asset.AddFlow(flowId, resourceName, ratePerSecond, isSource);
                        }
                    }
                }
            }

            // Add converters
            if (payload.TryGetValue("addConverters", out var addConvertersObj) && addConvertersObj is List<object> addConvertersList)
            {
                foreach (var converterObj in addConvertersList)
                {
                    if (converterObj is Dictionary<string, object> converterDict)
                    {
                        var converterId = GetString(converterDict, "converterId");
                        var fromResource = GetString(converterDict, "fromResource");
                        var toResource = GetString(converterDict, "toResource");
                        var conversionRate = GetFloat(converterDict, "conversionRate", 1f);
                        var inputCost = GetFloat(converterDict, "inputCost", 1f);
                        
                        if (!string.IsNullOrEmpty(converterId) && !string.IsNullOrEmpty(fromResource) && !string.IsNullOrEmpty(toResource))
                        {
                            asset.AddConverter(converterId, fromResource, toResource, conversionRate, inputCost);
                        }
                    }
                }
            }

            // Add triggers
            if (payload.TryGetValue("addTriggers", out var addTriggersObj) && addTriggersObj is List<object> addTriggersList)
            {
                foreach (var triggerObj in addTriggersList)
                {
                    if (triggerObj is Dictionary<string, object> triggerDict)
                    {
                        var triggerName = GetString(triggerDict, "triggerName");
                        var resourceName = GetString(triggerDict, "resourceName");
                        var thresholdTypeStr = GetString(triggerDict, "thresholdType", "Above");
                        var thresholdValue = GetFloat(triggerDict, "thresholdValue", 0f);
                        
                        var thresholdType = ParseThresholdType(thresholdTypeStr);
                        
                        if (!string.IsNullOrEmpty(triggerName) && !string.IsNullOrEmpty(resourceName))
                        {
                            asset.AddTrigger(triggerName, resourceName, thresholdType, thresholdValue);
                        }
                    }
                }
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(("diagramId", asset.DiagramId), ("assetPath", assetPath));
        }

        #endregion

        #region Inspect Asset

        private object InspectMachinationsAsset(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("assetPath is required for inspect.");
            }

            var asset = AssetDatabase.LoadAssetAtPath<GameKitMachinationsAsset>(assetPath);
            if (asset == null)
            {
                return CreateSuccessResponse(("found", false), ("assetPath", assetPath));
            }

            // Build pools info
            var poolsInfo = new List<Dictionary<string, object>>();
            foreach (var pool in asset.Pools)
            {
                poolsInfo.Add(new Dictionary<string, object>
                {
                    { "resourceName", pool.resourceName },
                    { "initialAmount", pool.initialAmount },
                    { "minValue", pool.minValue },
                    { "maxValue", pool.maxValue }
                });
            }

            // Build flows info
            var flowsInfo = new List<Dictionary<string, object>>();
            foreach (var flow in asset.Flows)
            {
                flowsInfo.Add(new Dictionary<string, object>
                {
                    { "flowId", flow.flowId },
                    { "resourceName", flow.resourceName },
                    { "ratePerSecond", flow.ratePerSecond },
                    { "isSource", flow.isSource },
                    { "enabledByDefault", flow.enabledByDefault }
                });
            }

            // Build converters info
            var convertersInfo = new List<Dictionary<string, object>>();
            foreach (var converter in asset.Converters)
            {
                convertersInfo.Add(new Dictionary<string, object>
                {
                    { "converterId", converter.converterId },
                    { "fromResource", converter.fromResource },
                    { "toResource", converter.toResource },
                    { "conversionRate", converter.conversionRate },
                    { "inputCost", converter.inputCost },
                    { "enabledByDefault", converter.enabledByDefault }
                });
            }

            // Build triggers info
            var triggersInfo = new List<Dictionary<string, object>>();
            foreach (var trigger in asset.Triggers)
            {
                triggersInfo.Add(new Dictionary<string, object>
                {
                    { "triggerName", trigger.triggerName },
                    { "resourceName", trigger.resourceName },
                    { "thresholdType", trigger.thresholdType.ToString() },
                    { "thresholdValue", trigger.thresholdValue },
                    { "enabledByDefault", trigger.enabledByDefault }
                });
            }

            var info = new Dictionary<string, object>
            {
                { "found", true },
                { "diagramId", asset.DiagramId },
                { "description", asset.Description },
                { "assetPath", assetPath },
                { "pools", poolsInfo },
                { "flows", flowsInfo },
                { "converters", convertersInfo },
                { "triggers", triggersInfo },
                { "summary", asset.GetSummary() }
            };

            return CreateSuccessResponse(("machinations", info));
        }

        #endregion

        #region Delete Asset

        private object DeleteMachinationsAsset(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("assetPath is required for delete.");
            }

            if (!AssetDatabase.DeleteAsset(assetPath))
            {
                throw new InvalidOperationException($"Failed to delete asset at: {assetPath}");
            }

            AssetDatabase.Refresh();

            return CreateSuccessResponse(("assetPath", assetPath), ("deleted", true));
        }

        #endregion

        #region Apply Asset

        private object ApplyMachinationsToManager(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            var managerId = GetString(payload, "managerId");
            var resetExisting = GetBool(payload, "resetExisting", false);

            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("assetPath is required for apply.");
            }

            if (string.IsNullOrEmpty(managerId))
            {
                throw new InvalidOperationException("managerId is required for apply.");
            }

            var asset = AssetDatabase.LoadAssetAtPath<GameKitMachinationsAsset>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"Machinations asset not found at: {assetPath}");
            }

            // Find manager
            var managers = UnityEngine.Object.FindObjectsByType<GameKitManager>(FindObjectsSortMode.None);
            GameKitManager targetManager = null;
            foreach (var manager in managers)
            {
                if (manager.ManagerId == managerId)
                {
                    targetManager = manager;
                    break;
                }
            }

            if (targetManager == null)
            {
                throw new InvalidOperationException($"Manager with ID '{managerId}' not found.");
            }

            // Get resource manager component
            var resourceManager = targetManager.GetComponent<GameKitResourceManager>();
            if (resourceManager == null)
            {
                throw new InvalidOperationException($"Manager '{managerId}' does not have a GameKitResourceManager component.");
            }

            // Apply asset
            Undo.RecordObject(resourceManager, "Apply Machinations Asset");
            resourceManager.ApplyMachinationsAsset(asset, resetExisting);
            EditorUtility.SetDirty(resourceManager);

            return CreateSuccessResponse(
                ("managerId", managerId), 
                ("assetPath", assetPath), 
                ("applied", true)
            );
        }

        #endregion

        #region Export Asset

        private object ExportMachinationsFromManager(Dictionary<string, object> payload)
        {
            var managerId = GetString(payload, "managerId");
            var assetPath = GetString(payload, "assetPath");
            var diagramId = GetString(payload, "diagramId") ?? $"Exported_{managerId}";

            if (string.IsNullOrEmpty(managerId))
            {
                throw new InvalidOperationException("managerId is required for export.");
            }

            if (string.IsNullOrEmpty(assetPath))
            {
                assetPath = $"Assets/{diagramId}.asset";
            }

            // Ensure .asset extension
            if (!assetPath.EndsWith(".asset"))
            {
                assetPath += ".asset";
            }

            // Find manager
            var managers = UnityEngine.Object.FindObjectsByType<GameKitManager>(FindObjectsSortMode.None);
            GameKitManager targetManager = null;
            foreach (var manager in managers)
            {
                if (manager.ManagerId == managerId)
                {
                    targetManager = manager;
                    break;
                }
            }

            if (targetManager == null)
            {
                throw new InvalidOperationException($"Manager with ID '{managerId}' not found.");
            }

            // Get resource manager component
            var resourceManager = targetManager.GetComponent<GameKitResourceManager>();
            if (resourceManager == null)
            {
                throw new InvalidOperationException($"Manager '{managerId}' does not have a GameKitResourceManager component.");
            }

            // Export to asset
            var asset = resourceManager.ExportToAsset(diagramId);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Save asset
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return CreateSuccessResponse(
                ("managerId", managerId),
                ("diagramId", diagramId),
                ("assetPath", assetPath),
                ("exported", true)
            );
        }

        #endregion

        #region Helpers

        private GameKitResourceManager.ThresholdType ParseThresholdType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "above" => GameKitResourceManager.ThresholdType.Above,
                "below" => GameKitResourceManager.ThresholdType.Below,
                "equal" => GameKitResourceManager.ThresholdType.Equal,
                "notequal" => GameKitResourceManager.ThresholdType.NotEqual,
                _ => GameKitResourceManager.ThresholdType.Above
            };
        }

        private float GetFloat(Dictionary<string, object> dict, string key, float defaultValue = 0f)
        {
            if (dict.TryGetValue(key, out var value))
            {
                return Convert.ToSingle(value);
            }
            return defaultValue;
        }

        private bool GetBool(Dictionary<string, object> dict, string key, bool defaultValue = false)
        {
            if (dict.TryGetValue(key, out var value))
            {
                if (value is bool boolValue)
                    return boolValue;
                if (value is string strValue)
                    return bool.Parse(strValue);
            }
            return defaultValue;
        }

        #endregion
    }
}

