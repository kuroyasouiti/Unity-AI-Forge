using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
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
            "create", "update", "inspect", "delete",
            "setRange", "refresh", "findByBindingId"
        };

        public override string Category => "gamekitUIBinding";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateBinding(payload),
                "update" => UpdateBinding(payload),
                "inspect" => InspectBinding(payload),
                "delete" => DeleteBinding(payload),
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

        #endregion

        #region Update

        private object UpdateBinding(Dictionary<string, object> payload)
        {
            var component = ResolveBindingComponent(payload);

            Undo.RecordObject(component, "Update UIBinding");

            var so = new SerializedObject(component);

            if (payload.TryGetValue("sourceType", out var typeObj))
            {
                var sourceType = ParseSourceType(typeObj.ToString());
                var prop = so.FindProperty("sourceType");
                var names = prop.enumDisplayNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], sourceType, StringComparison.OrdinalIgnoreCase))
                    { prop.enumValueIndex = i; break; }
                }
            }

            if (payload.TryGetValue("sourceId", out var srcIdObj))
                so.FindProperty("sourceId").stringValue = srcIdObj.ToString();

            if (payload.TryGetValue("format", out var formatObj))
            {
                var format = ParseValueFormat(formatObj.ToString());
                var prop = so.FindProperty("format");
                var names = prop.enumDisplayNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], format, StringComparison.OrdinalIgnoreCase))
                    { prop.enumValueIndex = i; break; }
                }
            }

            if (payload.TryGetValue("elementName", out var elemObj))
                so.FindProperty("elementName").stringValue = elemObj.ToString();

            if (payload.TryGetValue("targetProperty", out var propObj))
                so.FindProperty("targetProperty").stringValue = propObj.ToString();

            if (payload.TryGetValue("formatString", out var formatStrObj))
                so.FindProperty("formatString").stringValue = formatStrObj.ToString();

            if (payload.TryGetValue("minValue", out var minObj))
                so.FindProperty("minValue").floatValue = Convert.ToSingle(minObj);

            if (payload.TryGetValue("maxValue", out var maxObj))
                so.FindProperty("maxValue").floatValue = Convert.ToSingle(maxObj);

            if (payload.TryGetValue("updateInterval", out var intervalObj))
                so.FindProperty("updateInterval").floatValue = Convert.ToSingle(intervalObj);

            if (payload.TryGetValue("smoothTransition", out var smoothObj))
                so.FindProperty("smoothTransition").boolValue = Convert.ToBoolean(smoothObj);

            if (payload.TryGetValue("smoothSpeed", out var speedObj))
                so.FindProperty("smoothSpeed").floatValue = Convert.ToSingle(speedObj);

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("bindingId", so.FindProperty("bindingId").stringValue),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
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

        #region Delete

        private object DeleteBinding(Dictionary<string, object> payload)
        {
            var bindingId = GetString(payload, "bindingId");

            try
            {
                var component = ResolveBindingComponent(payload);
                var path = BuildGameObjectPath(component.gameObject);
                bindingId = new SerializedObject(component).FindProperty("bindingId").stringValue;
                var scene = component.gameObject.scene;

                Undo.DestroyObjectImmediate(component);
                ScriptGenerator.Delete(bindingId);
                EditorSceneManager.MarkSceneDirty(scene);

                return CreateSuccessResponse(
                    ("bindingId", bindingId),
                    ("path", path),
                    ("deleted", true)
                );
            }
            catch (InvalidOperationException) when (!string.IsNullOrEmpty(bindingId))
            {
                // Parent GO already destroyed — clean up the orphaned script via tracker
                ScriptGenerator.Delete(bindingId);

                return CreateSuccessResponse(
                    ("bindingId", bindingId),
                    ("deleted", true),
                    ("note", "Component not found in scene; orphaned script cleaned up.")
                );
            }
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
            return CreateSuccessResponse(
                ("found", true),
                ("bindingId", bindingId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("sourceType", so.FindProperty("sourceType").enumDisplayNames[so.FindProperty("sourceType").enumValueIndex]),
                ("sourceId", so.FindProperty("sourceId").stringValue)
            );
        }

        #endregion

        #region Helpers

        private Component ResolveBindingComponent(Dictionary<string, object> payload)
        {
            var bindingId = GetString(payload, "bindingId");
            if (!string.IsNullOrEmpty(bindingId))
            {
                var byId = CodeGenHelper.FindComponentInSceneByField("bindingId", bindingId);
                if (byId != null) return byId;
            }

            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var byPath = CodeGenHelper.FindComponentByField(targetGo, "bindingId", null);
                    if (byPath != null) return byPath;
                    throw new InvalidOperationException($"No UIBinding component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either bindingId or targetPath is required.");
        }

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
