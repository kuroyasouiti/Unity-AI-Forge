using System;
using System.Collections.Generic;
using System.IO;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Machinations handler: create and manage Machinations diagram assets.
    /// Uses code generation to produce standalone ScriptableObject scripts with zero package dependency.
    /// </summary>
    public class GameKitMachinationsHandler : BaseCommandHandler
    {
        private static readonly string[] Operations = { "create", "update", "inspect", "delete", "apply", "export" };

        public override string Category => "gamekitMachinations";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

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

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(diagramId, "Machinations");
            var outputDir = GetString(payload, "outputPath");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "DIAGRAM_ID", diagramId }
            };

            // Generate the ScriptableObject script (null target since this is not a MonoBehaviour)
            var result = ScriptGenerator.Generate(null, "MachinationsData", className, diagramId, variables, outputDir);
            if (!result.Success)
            {
                throw new InvalidOperationException(result.ErrorMessage ?? "Failed to generate MachinationsData script.");
            }

            var response = new Dictionary<string, object>
            {
                ["success"] = true,
                ["scriptPath"] = result.ScriptPath,
                ["className"] = result.ClassName,
                ["componentId"] = diagramId,
                ["diagramId"] = diagramId,
                ["assetPath"] = assetPath
            };

            // Try to create the asset if the type is already compiled
            var generatedType = ScriptGenerator.ResolveGeneratedType(className);
            if (generatedType != null)
            {
                var asset = CreateAssetInstance(generatedType, assetPath, diagramId, description, payload);
                response["assetCreated"] = true;
                response["compilationRequired"] = false;
            }
            else
            {
                // Script needs compilation first; store parameters for deferred creation
                response["assetCreated"] = false;
                response["compilationRequired"] = true;
                response["note"] = "ScriptableObject script generated. After compilation, " +
                    "create the asset via the Unity CreateAssetMenu or re-run this operation.";
            }

            return response;
        }

        /// <summary>
        /// Creates a ScriptableObject asset instance from a generated type and populates its fields.
        /// </summary>
        private ScriptableObject CreateAssetInstance(
            Type soType, string assetPath, string diagramId, string description,
            Dictionary<string, object> payload)
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var asset = ScriptableObject.CreateInstance(soType);
            var so = new SerializedObject(asset);

            // Set identity fields
            so.FindProperty("diagramId").stringValue = diagramId;
            if (!string.IsNullOrEmpty(description))
            {
                so.FindProperty("description").stringValue = description;
            }

            so.ApplyModifiedProperties();

            // Add pools (support both "pools" and "initialResources" keys for MCP compatibility)
            var poolsKey = payload.ContainsKey("initialResources") ? "initialResources" : "pools";
            if (payload.TryGetValue(poolsKey, out var poolsObj) && poolsObj is List<object> poolsList)
            {
                AddPools(so, poolsList);
            }

            // Add flows
            if (payload.TryGetValue("flows", out var flowsObj) && flowsObj is List<object> flowsList)
            {
                AddFlows(so, flowsList);
            }

            // Add converters
            if (payload.TryGetValue("converters", out var convertersObj) && convertersObj is List<object> convertersList)
            {
                AddConverters(so, convertersList);
            }

            // Add triggers
            if (payload.TryGetValue("triggers", out var triggersObj) && triggersObj is List<object> triggersList)
            {
                AddTriggers(so, triggersList);
            }

            so.ApplyModifiedProperties();

            // Save asset
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return asset;
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

            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"Machinations asset not found at: {assetPath}");
            }

            Undo.RecordObject(asset, "Update Machinations Asset");
            var so = new SerializedObject(asset);

            // Update description if provided
            if (payload.TryGetValue("description", out var descObj))
            {
                so.FindProperty("description").stringValue = descObj.ToString();
            }

            // Add pools
            if (payload.TryGetValue("addPools", out var addPoolsObj) && addPoolsObj is List<object> addPoolsList)
            {
                AddPools(so, addPoolsList);
            }

            // Remove pools
            if (payload.TryGetValue("removePools", out var removePoolsObj) && removePoolsObj is List<object> removePoolsList)
            {
                var poolsProp = so.FindProperty("pools");
                foreach (var poolName in removePoolsList)
                {
                    RemoveListItemByField(poolsProp, "name", poolName.ToString());
                }
            }

            // Add flows
            if (payload.TryGetValue("addFlows", out var addFlowsObj) && addFlowsObj is List<object> addFlowsList)
            {
                AddFlows(so, addFlowsList);
            }

            // Add converters
            if (payload.TryGetValue("addConverters", out var addConvertersObj) && addConvertersObj is List<object> addConvertersList)
            {
                AddConverters(so, addConvertersList);
            }

            // Add triggers
            if (payload.TryGetValue("addTriggers", out var addTriggersObj) && addTriggersObj is List<object> addTriggersList)
            {
                AddTriggers(so, addTriggersList);
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            var diagramId = new SerializedObject(asset).FindProperty("diagramId").stringValue;

            return CreateSuccessResponse(("diagramId", diagramId), ("assetPath", assetPath));
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

            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null)
            {
                return CreateSuccessResponse(("found", false), ("assetPath", assetPath));
            }

            var so = new SerializedObject(asset);

            // Build pools info
            var poolsInfo = new List<Dictionary<string, object>>();
            var poolsProp = so.FindProperty("pools");
            if (poolsProp != null)
            {
                for (int i = 0; i < poolsProp.arraySize; i++)
                {
                    var poolElem = poolsProp.GetArrayElementAtIndex(i);
                    poolsInfo.Add(new Dictionary<string, object>
                    {
                        { "resourceName", poolElem.FindPropertyRelative("name").stringValue },
                        { "initialAmount", poolElem.FindPropertyRelative("initialAmount").floatValue },
                        { "minValue", poolElem.FindPropertyRelative("minValue").floatValue },
                        { "maxValue", poolElem.FindPropertyRelative("maxValue").floatValue }
                    });
                }
            }

            // Build flows info
            var flowsInfo = new List<Dictionary<string, object>>();
            var flowsProp = so.FindProperty("flows");
            if (flowsProp != null)
            {
                for (int i = 0; i < flowsProp.arraySize; i++)
                {
                    var flowElem = flowsProp.GetArrayElementAtIndex(i);
                    flowsInfo.Add(new Dictionary<string, object>
                    {
                        { "flowId", flowElem.FindPropertyRelative("flowId").stringValue },
                        { "resourceName", flowElem.FindPropertyRelative("resourceName").stringValue },
                        { "ratePerSecond", flowElem.FindPropertyRelative("ratePerSecond").floatValue },
                        { "isSource", flowElem.FindPropertyRelative("isSource").boolValue },
                        { "enabledByDefault", flowElem.FindPropertyRelative("enabledByDefault").boolValue }
                    });
                }
            }

            // Build converters info
            var convertersInfo = new List<Dictionary<string, object>>();
            var convertersProp = so.FindProperty("converters");
            if (convertersProp != null)
            {
                for (int i = 0; i < convertersProp.arraySize; i++)
                {
                    var convElem = convertersProp.GetArrayElementAtIndex(i);
                    convertersInfo.Add(new Dictionary<string, object>
                    {
                        { "converterId", convElem.FindPropertyRelative("converterId").stringValue },
                        { "fromResource", convElem.FindPropertyRelative("fromResource").stringValue },
                        { "toResource", convElem.FindPropertyRelative("toResource").stringValue },
                        { "conversionRate", convElem.FindPropertyRelative("conversionRate").floatValue },
                        { "inputCost", convElem.FindPropertyRelative("inputCost").floatValue },
                        { "enabledByDefault", convElem.FindPropertyRelative("enabledByDefault").boolValue }
                    });
                }
            }

            // Build triggers info
            var triggersInfo = new List<Dictionary<string, object>>();
            var triggersProp = so.FindProperty("triggers");
            if (triggersProp != null)
            {
                for (int i = 0; i < triggersProp.arraySize; i++)
                {
                    var trigElem = triggersProp.GetArrayElementAtIndex(i);
                    var thresholdProp = trigElem.FindPropertyRelative("thresholdType");
                    var thresholdStr = thresholdProp.enumValueIndex < thresholdProp.enumDisplayNames.Length
                        ? thresholdProp.enumDisplayNames[thresholdProp.enumValueIndex]
                        : "Above";

                    triggersInfo.Add(new Dictionary<string, object>
                    {
                        { "triggerName", trigElem.FindPropertyRelative("triggerName").stringValue },
                        { "resourceName", trigElem.FindPropertyRelative("resourceName").stringValue },
                        { "thresholdType", thresholdStr },
                        { "thresholdValue", trigElem.FindPropertyRelative("thresholdValue").floatValue },
                        { "enabled", trigElem.FindPropertyRelative("enabled").boolValue }
                    });
                }
            }

            // Build summary
            var diagramId = so.FindProperty("diagramId").stringValue;
            var descriptionProp = so.FindProperty("description");
            var descriptionValue = descriptionProp != null ? descriptionProp.stringValue : "";
            var summary = $"{diagramId}: {poolsInfo.Count} pools, {flowsInfo.Count} flows, " +
                          $"{convertersInfo.Count} converters, {triggersInfo.Count} triggers";

            var info = new Dictionary<string, object>
            {
                { "found", true },
                { "diagramId", diagramId },
                { "description", descriptionValue },
                { "assetPath", assetPath },
                { "pools", poolsInfo },
                { "flows", flowsInfo },
                { "converters", convertersInfo },
                { "triggers", triggersInfo },
                { "summary", summary }
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

            // Try to get diagramId before deletion to clean up generated script
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset != null)
            {
                var so = new SerializedObject(asset);
                var diagramIdProp = so.FindProperty("diagramId");
                if (diagramIdProp != null && !string.IsNullOrEmpty(diagramIdProp.stringValue))
                {
                    ScriptGenerator.Delete(diagramIdProp.stringValue);
                }
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

            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"Machinations asset not found at: {assetPath}");
            }

            // Find manager by managerId field
            var managerComponent = CodeGenHelper.FindComponentInSceneByField("managerId", managerId);
            if (managerComponent == null)
            {
                throw new InvalidOperationException($"Manager with ID '{managerId}' not found.");
            }

            // Find resource manager component on the same GameObject (by checking for a component
            // with a serialized field that references machinations/resource data)
            var resourceManager = CodeGenHelper.FindComponentByField(
                managerComponent.gameObject, "machinationsAsset", null);
            if (resourceManager == null)
            {
                throw new InvalidOperationException(
                    $"Manager '{managerId}' does not have a ResourceManager component with a machinationsAsset field.");
            }

            // Apply asset reference
            Undo.RecordObject(resourceManager, "Apply Machinations Asset");
            var rmSo = new SerializedObject(resourceManager);

            // Set the asset reference
            var assetProp = rmSo.FindProperty("machinationsAsset");
            if (assetProp != null)
            {
                assetProp.objectReferenceValue = asset;
            }

            // If resetExisting, clear current runtime resource values
            if (resetExisting)
            {
                var currentResourcesProp = rmSo.FindProperty("currentResources");
                if (currentResourcesProp != null && currentResourcesProp.isArray)
                {
                    currentResourcesProp.ClearArray();
                }
            }

            rmSo.ApplyModifiedProperties();
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

            // Find manager by managerId field
            var managerComponent = CodeGenHelper.FindComponentInSceneByField("managerId", managerId);
            if (managerComponent == null)
            {
                throw new InvalidOperationException($"Manager with ID '{managerId}' not found.");
            }

            // Find resource manager component on the same GameObject
            var resourceManager = CodeGenHelper.FindComponentByField(
                managerComponent.gameObject, "machinationsAsset", null);
            if (resourceManager == null)
            {
                throw new InvalidOperationException(
                    $"Manager '{managerId}' does not have a ResourceManager component with a machinationsAsset field.");
            }

            // Read the existing machinations asset reference to determine the generated SO type
            var rmSo = new SerializedObject(resourceManager);
            var existingAssetProp = rmSo.FindProperty("machinationsAsset");
            ScriptableObject existingAsset = existingAssetProp?.objectReferenceValue as ScriptableObject;

            // Determine the SO type to create
            Type soType = existingAsset != null ? existingAsset.GetType() : null;

            if (soType == null)
            {
                // Try to find any generated MachinationsData type via the tracker
                var className = ScriptGenerator.ToPascalCase(diagramId, "Machinations");
                soType = ScriptGenerator.ResolveGeneratedType(className);
            }

            if (soType == null)
            {
                throw new InvalidOperationException(
                    "Cannot determine MachinationsData type for export. " +
                    "Ensure a MachinationsData script has been generated first.");
            }

            // Create a new asset of the determined type
            var newAsset = ScriptableObject.CreateInstance(soType);
            var newSo = new SerializedObject(newAsset);

            // Set diagram ID and description
            newSo.FindProperty("diagramId").stringValue = diagramId;

            // Copy pools, flows, converters, triggers from existing asset if available
            if (existingAsset != null)
            {
                var srcSo = new SerializedObject(existingAsset);
                CopySerializedArray(srcSo, newSo, "pools");
                CopySerializedArray(srcSo, newSo, "flows");
                CopySerializedArray(srcSo, newSo, "converters");
                CopySerializedArray(srcSo, newSo, "triggers");
            }

            newSo.ApplyModifiedProperties();

            // Ensure directory exists
            var directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Save asset
            AssetDatabase.CreateAsset(newAsset, assetPath);
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

        private string ParseThresholdType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "above" => "Above",
                "below" => "Below",
                "equal" => "Equal",
                "notequal" => "NotEqual",
                _ => "Above"
            };
        }

        /// <summary>
        /// Adds pools to a serialized MachinationsData asset.
        /// </summary>
        private void AddPools(SerializedObject so, List<object> poolsList)
        {
            var poolsProp = so.FindProperty("pools");
            if (poolsProp == null) return;

            foreach (var poolObj in poolsList)
            {
                if (poolObj is Dictionary<string, object> poolDict)
                {
                    var resourceName = GetString(poolDict, "name") ?? GetString(poolDict, "resourceName");
                    var initialAmount = GetFloat(poolDict, "initialAmount", 0f);
                    var minValue = GetFloat(poolDict, "minValue", 0f);
                    var maxValue = GetFloat(poolDict, "maxValue", 100f);

                    if (!string.IsNullOrEmpty(resourceName))
                    {
                        var index = poolsProp.arraySize;
                        poolsProp.InsertArrayElementAtIndex(index);
                        var elem = poolsProp.GetArrayElementAtIndex(index);
                        elem.FindPropertyRelative("name").stringValue = resourceName;
                        elem.FindPropertyRelative("initialAmount").floatValue = initialAmount;
                        elem.FindPropertyRelative("minValue").floatValue = minValue;
                        elem.FindPropertyRelative("maxValue").floatValue = maxValue;
                    }
                }
            }
        }

        /// <summary>
        /// Adds flows to a serialized MachinationsData asset.
        /// </summary>
        private void AddFlows(SerializedObject so, List<object> flowsList)
        {
            var flowsProp = so.FindProperty("flows");
            if (flowsProp == null) return;

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
                        var index = flowsProp.arraySize;
                        flowsProp.InsertArrayElementAtIndex(index);
                        var elem = flowsProp.GetArrayElementAtIndex(index);
                        elem.FindPropertyRelative("flowId").stringValue = flowId;
                        elem.FindPropertyRelative("resourceName").stringValue = resourceName;
                        elem.FindPropertyRelative("ratePerSecond").floatValue = ratePerSecond;
                        elem.FindPropertyRelative("isSource").boolValue = isSource;
                        elem.FindPropertyRelative("enabledByDefault").boolValue = true;
                    }
                }
            }
        }

        /// <summary>
        /// Adds converters to a serialized MachinationsData asset.
        /// </summary>
        private void AddConverters(SerializedObject so, List<object> convertersList)
        {
            var convertersProp = so.FindProperty("converters");
            if (convertersProp == null) return;

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
                        var index = convertersProp.arraySize;
                        convertersProp.InsertArrayElementAtIndex(index);
                        var elem = convertersProp.GetArrayElementAtIndex(index);
                        elem.FindPropertyRelative("converterId").stringValue = converterId;
                        elem.FindPropertyRelative("fromResource").stringValue = fromResource;
                        elem.FindPropertyRelative("toResource").stringValue = toResource;
                        elem.FindPropertyRelative("conversionRate").floatValue = conversionRate;
                        elem.FindPropertyRelative("inputCost").floatValue = inputCost;
                        elem.FindPropertyRelative("enabledByDefault").boolValue = true;
                    }
                }
            }
        }

        /// <summary>
        /// Adds triggers to a serialized MachinationsData asset.
        /// </summary>
        private void AddTriggers(SerializedObject so, List<object> triggersList)
        {
            var triggersProp = so.FindProperty("triggers");
            if (triggersProp == null) return;

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
                        var index = triggersProp.arraySize;
                        triggersProp.InsertArrayElementAtIndex(index);
                        var elem = triggersProp.GetArrayElementAtIndex(index);
                        elem.FindPropertyRelative("triggerName").stringValue = triggerName;
                        elem.FindPropertyRelative("resourceName").stringValue = resourceName;

                        // Set enum by matching display name
                        var thresholdProp = elem.FindPropertyRelative("thresholdType");
                        var names = thresholdProp.enumDisplayNames;
                        for (int i = 0; i < names.Length; i++)
                        {
                            if (string.Equals(names[i], thresholdType, StringComparison.OrdinalIgnoreCase))
                            {
                                thresholdProp.enumValueIndex = i;
                                break;
                            }
                        }

                        elem.FindPropertyRelative("thresholdValue").floatValue = thresholdValue;
                        elem.FindPropertyRelative("enabled").boolValue = true;
                    }
                }
            }
        }

        /// <summary>
        /// Removes a list item by matching a string field value.
        /// </summary>
        private void RemoveListItemByField(SerializedProperty arrayProp, string fieldName, string fieldValue)
        {
            if (arrayProp == null || !arrayProp.isArray) return;

            for (int i = arrayProp.arraySize - 1; i >= 0; i--)
            {
                var elem = arrayProp.GetArrayElementAtIndex(i);
                var field = elem.FindPropertyRelative(fieldName);
                if (field != null && field.stringValue == fieldValue)
                {
                    arrayProp.DeleteArrayElementAtIndex(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Copies a serialized array property from one SerializedObject to another.
        /// Both objects must have the same property layout for the given array.
        /// </summary>
        private void CopySerializedArray(SerializedObject src, SerializedObject dst, string propertyName)
        {
            var srcProp = src.FindProperty(propertyName);
            var dstProp = dst.FindProperty(propertyName);
            if (srcProp == null || dstProp == null || !srcProp.isArray || !dstProp.isArray) return;

            dstProp.ClearArray();
            for (int i = 0; i < srcProp.arraySize; i++)
            {
                dstProp.InsertArrayElementAtIndex(i);
                CopySerializedElement(srcProp.GetArrayElementAtIndex(i), dstProp.GetArrayElementAtIndex(i));
            }
        }

        /// <summary>
        /// Copies serialized property values from one element to another (shallow copy).
        /// </summary>
        private void CopySerializedElement(SerializedProperty src, SerializedProperty dst)
        {
            var iter = src.Copy();
            var dstIter = dst.Copy();
            var endProp = src.GetEndProperty();

            if (iter.NextVisible(true) && dstIter.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(iter, endProp)) break;

                    var dstChild = dst.FindPropertyRelative(iter.name);
                    if (dstChild == null) continue;

                    switch (iter.propertyType)
                    {
                        case SerializedPropertyType.Integer:
                            dstChild.intValue = iter.intValue;
                            break;
                        case SerializedPropertyType.Boolean:
                            dstChild.boolValue = iter.boolValue;
                            break;
                        case SerializedPropertyType.Float:
                            dstChild.floatValue = iter.floatValue;
                            break;
                        case SerializedPropertyType.String:
                            dstChild.stringValue = iter.stringValue;
                            break;
                        case SerializedPropertyType.Enum:
                            dstChild.enumValueIndex = iter.enumValueIndex;
                            break;
                        case SerializedPropertyType.Color:
                            dstChild.colorValue = iter.colorValue;
                            break;
                    }
                } while (iter.NextVisible(false));
            }
        }

        // Note: Using 'new' to explicitly hide inherited methods for local use
        private new float GetFloat(Dictionary<string, object> dict, string key, float defaultValue = 0f)
        {
            if (dict.TryGetValue(key, out var value))
            {
                return Convert.ToSingle(value);
            }
            return defaultValue;
        }

        private new bool GetBool(Dictionary<string, object> dict, string key, bool defaultValue = false)
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
