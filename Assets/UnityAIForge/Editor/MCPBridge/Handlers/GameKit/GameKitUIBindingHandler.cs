using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using UnityAIForge.GameKit;
using UnityAIForge.GameKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit UI Binding handler: create and manage declarative UI data bindings.
    /// Supports binding health, economy, timer, and custom data sources to UI elements.
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

        protected override bool RequiresCompilationWait(string operation) => false;

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
            {
                throw new InvalidOperationException("targetPath is required for create operation.");
            }

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
            {
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            // Check if already has binding component
            var existingBinding = targetGo.GetComponent<GameKitUIBinding>();
            if (existingBinding != null)
            {
                throw new InvalidOperationException($"GameObject '{targetPath}' already has a GameKitUIBinding component.");
            }

            var bindingId = GetString(payload, "bindingId") ?? $"Binding_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var sourceType = ParseSourceType(GetString(payload, "sourceType") ?? "health");
            var sourceId = GetString(payload, "sourceId") ?? "";
            var format = ParseValueFormat(GetString(payload, "format") ?? "raw");

            // Add component
            var binding = Undo.AddComponent<GameKitUIBinding>(targetGo);

            // Set properties via SerializedObject
            var serializedBinding = new SerializedObject(binding);
            serializedBinding.FindProperty("bindingId").stringValue = bindingId;
            serializedBinding.FindProperty("sourceType").enumValueIndex = (int)sourceType;
            serializedBinding.FindProperty("sourceId").stringValue = sourceId;
            serializedBinding.FindProperty("format").enumValueIndex = (int)format;

            // Target configuration
            if (payload.TryGetValue("targetProperty", out var propObj))
            {
                serializedBinding.FindProperty("targetProperty").stringValue = propObj.ToString();
            }

            if (payload.TryGetValue("formatString", out var formatStrObj))
            {
                serializedBinding.FindProperty("formatString").stringValue = formatStrObj.ToString();
            }

            if (payload.TryGetValue("minValue", out var minObj))
            {
                serializedBinding.FindProperty("minValue").floatValue = Convert.ToSingle(minObj);
            }

            if (payload.TryGetValue("maxValue", out var maxObj))
            {
                serializedBinding.FindProperty("maxValue").floatValue = Convert.ToSingle(maxObj);
            }

            if (payload.TryGetValue("updateInterval", out var intervalObj))
            {
                serializedBinding.FindProperty("updateInterval").floatValue = Convert.ToSingle(intervalObj);
            }

            if (payload.TryGetValue("smoothTransition", out var smoothObj))
            {
                serializedBinding.FindProperty("smoothTransition").boolValue = Convert.ToBoolean(smoothObj);
            }

            if (payload.TryGetValue("smoothSpeed", out var speedObj))
            {
                serializedBinding.FindProperty("smoothSpeed").floatValue = Convert.ToSingle(speedObj);
            }

            // Set target object to self by default
            serializedBinding.FindProperty("targetObject").objectReferenceValue = targetGo;

            serializedBinding.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("bindingId", bindingId),
                ("path", BuildGameObjectPath(targetGo)),
                ("sourceType", sourceType.ToString()),
                ("sourceId", sourceId),
                ("format", format.ToString())
            );
        }

        #endregion

        #region Update

        private object UpdateBinding(Dictionary<string, object> payload)
        {
            var binding = ResolveBindingComponent(payload);

            Undo.RecordObject(binding, "Update GameKit UI Binding");

            var serializedBinding = new SerializedObject(binding);

            if (payload.TryGetValue("sourceType", out var typeObj))
            {
                var sourceType = ParseSourceType(typeObj.ToString());
                serializedBinding.FindProperty("sourceType").enumValueIndex = (int)sourceType;
            }

            if (payload.TryGetValue("sourceId", out var srcIdObj))
            {
                serializedBinding.FindProperty("sourceId").stringValue = srcIdObj.ToString();
            }

            if (payload.TryGetValue("format", out var formatObj))
            {
                var format = ParseValueFormat(formatObj.ToString());
                serializedBinding.FindProperty("format").enumValueIndex = (int)format;
            }

            if (payload.TryGetValue("targetProperty", out var propObj))
            {
                serializedBinding.FindProperty("targetProperty").stringValue = propObj.ToString();
            }

            if (payload.TryGetValue("formatString", out var formatStrObj))
            {
                serializedBinding.FindProperty("formatString").stringValue = formatStrObj.ToString();
            }

            if (payload.TryGetValue("minValue", out var minObj))
            {
                serializedBinding.FindProperty("minValue").floatValue = Convert.ToSingle(minObj);
            }

            if (payload.TryGetValue("maxValue", out var maxObj))
            {
                serializedBinding.FindProperty("maxValue").floatValue = Convert.ToSingle(maxObj);
            }

            if (payload.TryGetValue("updateInterval", out var intervalObj))
            {
                serializedBinding.FindProperty("updateInterval").floatValue = Convert.ToSingle(intervalObj);
            }

            if (payload.TryGetValue("smoothTransition", out var smoothObj))
            {
                serializedBinding.FindProperty("smoothTransition").boolValue = Convert.ToBoolean(smoothObj);
            }

            if (payload.TryGetValue("smoothSpeed", out var speedObj))
            {
                serializedBinding.FindProperty("smoothSpeed").floatValue = Convert.ToSingle(speedObj);
            }

            serializedBinding.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(binding.gameObject.scene);

            return CreateSuccessResponse(
                ("bindingId", binding.BindingId),
                ("path", BuildGameObjectPath(binding.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectBinding(Dictionary<string, object> payload)
        {
            var binding = ResolveBindingComponent(payload);

            var serializedBinding = new SerializedObject(binding);

            var info = new Dictionary<string, object>
            {
                { "bindingId", binding.BindingId },
                { "path", BuildGameObjectPath(binding.gameObject) },
                { "sourceType", binding.Source.ToString() },
                { "sourceId", binding.SourceId },
                { "currentValue", binding.CurrentValue },
                { "currentPercent", binding.CurrentPercent },
                { "format", serializedBinding.FindProperty("format").enumNames[serializedBinding.FindProperty("format").enumValueIndex] },
                { "minValue", serializedBinding.FindProperty("minValue").floatValue },
                { "maxValue", serializedBinding.FindProperty("maxValue").floatValue },
                { "updateInterval", serializedBinding.FindProperty("updateInterval").floatValue },
                { "smoothTransition", serializedBinding.FindProperty("smoothTransition").boolValue }
            };

            return CreateSuccessResponse(("binding", info));
        }

        #endregion

        #region Delete

        private object DeleteBinding(Dictionary<string, object> payload)
        {
            var binding = ResolveBindingComponent(payload);
            var path = BuildGameObjectPath(binding.gameObject);
            var bindingId = binding.BindingId;
            var scene = binding.gameObject.scene;

            Undo.DestroyObjectImmediate(binding);
            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("bindingId", bindingId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Operations

        private object SetRange(Dictionary<string, object> payload)
        {
            var binding = ResolveBindingComponent(payload);
            var min = GetFloat(payload, "minValue", 0f);
            var max = GetFloat(payload, "maxValue", 100f);

            var serializedBinding = new SerializedObject(binding);
            serializedBinding.FindProperty("minValue").floatValue = min;
            serializedBinding.FindProperty("maxValue").floatValue = max;
            serializedBinding.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(binding.gameObject.scene);

            return CreateSuccessResponse(
                ("bindingId", binding.BindingId),
                ("minValue", min),
                ("maxValue", max)
            );
        }

        private object RefreshBinding(Dictionary<string, object> payload)
        {
            var binding = ResolveBindingComponent(payload);

            // In editor mode, we can't call Refresh() directly
            // Just return current state
            return CreateSuccessResponse(
                ("bindingId", binding.BindingId),
                ("refreshed", true),
                ("note", "Refresh will take effect in play mode.")
            );
        }

        private object FindByBindingId(Dictionary<string, object> payload)
        {
            var bindingId = GetString(payload, "bindingId");
            if (string.IsNullOrEmpty(bindingId))
            {
                throw new InvalidOperationException("bindingId is required for findByBindingId.");
            }

            var binding = FindBindingById(bindingId);
            if (binding == null)
            {
                return CreateSuccessResponse(("found", false), ("bindingId", bindingId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("bindingId", binding.BindingId),
                ("path", BuildGameObjectPath(binding.gameObject)),
                ("sourceType", binding.Source.ToString()),
                ("sourceId", binding.SourceId)
            );
        }

        #endregion

        #region Helpers

        private GameKitUIBinding ResolveBindingComponent(Dictionary<string, object> payload)
        {
            // Try by bindingId first
            var bindingId = GetString(payload, "bindingId");
            if (!string.IsNullOrEmpty(bindingId))
            {
                var bindingById = FindBindingById(bindingId);
                if (bindingById != null)
                {
                    return bindingById;
                }
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var bindingByPath = targetGo.GetComponent<GameKitUIBinding>();
                    if (bindingByPath != null)
                    {
                        return bindingByPath;
                    }
                    throw new InvalidOperationException($"No GameKitUIBinding component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either bindingId or targetPath is required.");
        }

        private GameKitUIBinding FindBindingById(string bindingId)
        {
            var bindings = UnityEngine.Object.FindObjectsByType<GameKitUIBinding>(FindObjectsSortMode.None);
            foreach (var binding in bindings)
            {
                if (binding.BindingId == bindingId)
                {
                    return binding;
                }
            }
            return null;
        }

        private GameKitUIBinding.SourceType ParseSourceType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "health" => GameKitUIBinding.SourceType.Health,
                "economy" => GameKitUIBinding.SourceType.Economy,
                "timer" => GameKitUIBinding.SourceType.Timer,
                "custom" => GameKitUIBinding.SourceType.Custom,
                _ => GameKitUIBinding.SourceType.Health
            };
        }

        private GameKitUIBinding.ValueFormat ParseValueFormat(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "raw" => GameKitUIBinding.ValueFormat.Raw,
                "percent" => GameKitUIBinding.ValueFormat.Percent,
                "formatted" => GameKitUIBinding.ValueFormat.Formatted,
                "ratio" => GameKitUIBinding.ValueFormat.Ratio,
                _ => GameKitUIBinding.ValueFormat.Raw
            };
        }

        private string BuildGameObjectPath(GameObject go)
        {
            var path = go.name;
            var current = go.transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        private float GetFloat(Dictionary<string, object> payload, string key, float defaultValue)
        {
            if (payload.TryGetValue(key, out var value))
            {
                return Convert.ToSingle(value);
            }
            return defaultValue;
        }

        #endregion
    }
}
