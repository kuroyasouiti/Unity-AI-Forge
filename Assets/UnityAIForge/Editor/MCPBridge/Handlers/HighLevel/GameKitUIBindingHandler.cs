using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.HighLevel
{
    /// <summary>
    /// GameKit UI Binding handler: create and manage declarative UI data bindings.
    /// Binds game state to UI Toolkit VisualElements via UIDocument queries.
    /// Uses code generation to produce standalone UIBinding scripts with zero package dependency.
    /// </summary>
    public class GameKitUIBindingHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "createMultiple", "inspect",
            "setRange", "refresh", "findByBindingId"
        };

        public override string Category => "gamekitUIBinding";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create" || operation == "createMultiple";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateBinding(payload),
                "createMultiple" => CreateMultipleBindings(payload),
                "inspect" => InspectBinding(payload),
                "setRange" => SetRange(payload),
                "refresh" => RefreshBinding(payload),
                "findByBindingId" => FindByBindingId(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit UI Binding operation: {operation}")
            };
        }

        #region Create

        private object CreateBinding(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            if (string.IsNullOrEmpty(targetPath))
                throw new InvalidOperationException("targetPath is required for create operation.");

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");

            var existingBinding = CodeGenHelper.FindComponentByField(targetGo, "bindingId", null);
            if (existingBinding != null)
                throw new InvalidOperationException($"GameObject '{targetPath}' already has a UIBinding component.");

            var bindingId = GetString(payload, "bindingId") ?? $"Binding_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var sourceType = ParseSourceType(GetString(payload, "sourceType") ?? "health");
            var sourceId = GetString(payload, "sourceId") ?? "";
            var format = ParseValueFormat(GetString(payload, "format") ?? "raw");
            var elementName = GetString(payload, "elementName") ?? "";
            var targetProperty = GetString(payload, "targetProperty") ?? "";
            var formatString = GetString(payload, "formatString") ?? "{0}";
            var minValue = GetFloat(payload, "minValue", 0f);
            var maxValue = GetFloat(payload, "maxValue", 100f);
            var updateInterval = GetFloat(payload, "updateInterval", 0.1f);
            var smoothTransition = GetBool(payload, "smoothTransition", false);
            var smoothSpeed = GetFloat(payload, "smoothSpeed", 5f);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(bindingId, "UIBinding");

            var variables = new Dictionary<string, object>
            {
                { "BINDING_ID", bindingId },
                { "SOURCE_TYPE", sourceType },
                { "SOURCE_ID", sourceId },
                { "ELEMENT_NAME", elementName },
                { "TARGET_PROPERTY", targetProperty },
                { "FORMAT", format },
                { "FORMAT_STRING", formatString },
                { "MIN_VALUE", minValue },
                { "MAX_VALUE", maxValue },
                { "UPDATE_INTERVAL", updateInterval },
                { "SMOOTH_TRANSITION", smoothTransition },
                { "SMOOTH_SPEED", smoothSpeed }
            };

            var outputDir = GetString(payload, "outputPath");

            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "UIBinding", bindingId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString() : "Failed to generate UIBinding script.");

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["bindingId"] = bindingId;
            result["path"] = BuildGameObjectPath(targetGo);
            result["sourceType"] = sourceType;
            result["sourceId"] = sourceId;
            result["format"] = format;
            result["elementName"] = elementName;

            return result;
        }

        private object CreateMultipleBindings(Dictionary<string, object> payload)
        {
            if (!payload.TryGetValue("bindings", out var bindingsObj) ||
                bindingsObj is not List<object> bindings || bindings.Count == 0)
                throw new InvalidOperationException("'bindings' array is required for createMultiple and must not be empty.");

            var results = new List<Dictionary<string, object>>();
            var errors = new List<string>();

            // Inherit shared targetPath if individual items don't specify it
            var sharedTargetPath = GetString(payload, "targetPath");

            foreach (var bindingObj in bindings)
            {
                if (bindingObj is not Dictionary<string, object> bindingPayload)
                {
                    errors.Add("Invalid binding format (expected object).");
                    continue;
                }

                if (!bindingPayload.ContainsKey("targetPath") && !string.IsNullOrEmpty(sharedTargetPath))
                    bindingPayload["targetPath"] = sharedTargetPath;

                try
                {
                    var result = CreateBinding(bindingPayload);
                    if (result is Dictionary<string, object> dict)
                        results.Add(dict);
                }
                catch (Exception ex)
                {
                    var bindingId = bindingPayload.TryGetValue("bindingId", out var id) ? id?.ToString() : "unknown";
                    errors.Add($"{bindingId}: {ex.Message}");
                }
            }

            return CreateSuccessResponse(
                ("created", results),
                ("createdCount", results.Count),
                ("errorCount", errors.Count),
                ("errors", errors),
                ("requiresCompilationWait", results.Count > 0)
            );
        }

        #endregion

        #region Inspect

        private object InspectBinding(Dictionary<string, object> payload)
        {
            var component = ResolveBindingComponent(payload);
            var so = new SerializedObject(component);

            var formatProp = so.FindProperty("format");
            var sourceTypeProp = so.FindProperty("sourceType");

            var info = new Dictionary<string, object>
            {
                { "bindingId", so.FindProperty("bindingId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "sourceType", sourceTypeProp.enumValueIndex < sourceTypeProp.enumDisplayNames.Length
                    ? sourceTypeProp.enumDisplayNames[sourceTypeProp.enumValueIndex] : "Health" },
                { "sourceId", so.FindProperty("sourceId").stringValue },
                { "elementName", so.FindProperty("elementName").stringValue },
                { "format", formatProp.enumValueIndex < formatProp.enumDisplayNames.Length
                    ? formatProp.enumDisplayNames[formatProp.enumValueIndex] : "Raw" },
                { "minValue", so.FindProperty("minValue").floatValue },
                { "maxValue", so.FindProperty("maxValue").floatValue },
                { "updateInterval", so.FindProperty("updateInterval").floatValue },
                { "smoothTransition", so.FindProperty("smoothTransition").boolValue }
            };

            return CreateSuccessResponse(("binding", info));
        }

        #endregion

        #region Operations

        private object SetRange(Dictionary<string, object> payload)
        {
            var component = ResolveBindingComponent(payload);
            var min = GetFloat(payload, "minValue", 0f);
            var max = GetFloat(payload, "maxValue", 100f);

            var so = new SerializedObject(component);
            so.FindProperty("minValue").floatValue = min;
            so.FindProperty("maxValue").floatValue = max;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("bindingId", new SerializedObject(component).FindProperty("bindingId").stringValue),
                ("minValue", min),
                ("maxValue", max)
            );
        }

        private object RefreshBinding(Dictionary<string, object> payload)
        {
            var component = ResolveBindingComponent(payload);
            var so = new SerializedObject(component);

            return CreateSuccessResponse(
                ("bindingId", so.FindProperty("bindingId").stringValue),
                ("refreshed", true),
                ("note", "Refresh will take effect in play mode.")
            );
        }

        private object FindByBindingId(Dictionary<string, object> payload)
        {
            var bindingId = GetString(payload, "bindingId");
            if (string.IsNullOrEmpty(bindingId))
                throw new InvalidOperationException("bindingId is required for findByBindingId.");

            var component = CodeGenHelper.FindComponentInSceneByField("bindingId", bindingId);
            if (component == null)
                return CreateSuccessResponse(("found", false), ("bindingId", bindingId));

            var so = new SerializedObject(component);
            var sourceTypeProp = so.FindProperty("sourceType");
            return CreateSuccessResponse(
                ("found", true),
                ("bindingId", bindingId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("sourceType", sourceTypeProp.enumValueIndex < sourceTypeProp.enumDisplayNames.Length
                    ? sourceTypeProp.enumDisplayNames[sourceTypeProp.enumValueIndex] : "Health"),
                ("sourceId", so.FindProperty("sourceId").stringValue)
            );
        }

        #endregion

        #region Helpers

        private Component ResolveBindingComponent(Dictionary<string, object> payload)
            => ResolveGeneratedComponent(payload, "bindingId", "bindingId", "UIBinding");

        private string ParseSourceType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "health" => "Health",
                "economy" => "Economy",
                "timer" => "Timer",
                "custom" => "Custom",
                _ => "Health"
            };
        }

        private string ParseValueFormat(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "raw" => "Raw",
                "percent" => "Percent",
                "formatted" => "Formatted",
                "ratio" => "Ratio",
                _ => "Raw"
            };
        }

        #endregion
    }
}
