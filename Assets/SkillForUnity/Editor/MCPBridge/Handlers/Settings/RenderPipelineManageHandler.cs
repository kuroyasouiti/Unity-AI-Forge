using System;
using System.Collections.Generic;
using System.Reflection;
using MCP.Editor.Base;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace MCP.Editor.Handlers.Settings
{
    /// <summary>
    /// レンダーパイプライン管理のコマンドハンドラー。
    /// パイプラインの検査、設定、取得、更新をサポート。
    /// </summary>
    public class RenderPipelineManageHandler : BaseCommandHandler
    {
        public override string Category => "renderPipelineManage";
        
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "inspect",
            "setAsset",
            "getSettings",
            "updateSettings",
        };
        
        public RenderPipelineManageHandler() : base()
        {
        }
        
        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "inspect" => InspectRenderPipeline(),
                "setAsset" => SetRenderPipelineAsset(payload),
                "getSettings" => GetRenderPipelineSettings(payload),
                "updateSettings" => UpdateRenderPipelineSettings(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }
        
        protected override bool RequiresCompilationWait(string operation)
        {
            // Read-only operations don't require compilation wait
            return operation != "inspect" && operation != "getSettings";
        }
        
        #region Operations
        
        private object InspectRenderPipeline()
        {
            var currentPipeline = GraphicsSettings.currentRenderPipeline;
            var result = new Dictionary<string, object>
            {
                ["hasRenderPipeline"] = currentPipeline != null,
            };
            
            if (currentPipeline != null)
            {
                result["pipelineName"] = currentPipeline.name;
                result["pipelineType"] = currentPipeline.GetType().FullName;
                result["assetPath"] = AssetDatabase.GetAssetPath(currentPipeline);
                
                // Detect pipeline type
                var typeName = currentPipeline.GetType().Name;
                if (typeName.Contains("Universal") || typeName.Contains("URP"))
                {
                    result["pipelineKind"] = "URP";
                }
                else if (typeName.Contains("HDRenderPipeline") || typeName.Contains("HDRP"))
                {
                    result["pipelineKind"] = "HDRP";
                }
                else
                {
                    result["pipelineKind"] = "Custom";
                }
            }
            else
            {
                result["pipelineKind"] = "BuiltIn";
            }
            
            return result;
        }
        
        private object SetRenderPipelineAsset(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            
            if (string.IsNullOrEmpty(assetPath))
            {
                // Clear render pipeline (set to built-in)
                GraphicsSettings.defaultRenderPipeline = null;
                return new Dictionary<string, object>
                {
                    ["message"] = "Render pipeline cleared (using Built-in)",
                    ["pipelineKind"] = "BuiltIn",
                };
            }
            
            var asset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"Failed to load RenderPipelineAsset from path: {assetPath}");
            }
            
            GraphicsSettings.defaultRenderPipeline = asset;
            
            var typeName = asset.GetType().Name;
            var pipelineKind = "Custom";
            if (typeName.Contains("Universal") || typeName.Contains("URP"))
            {
                pipelineKind = "URP";
            }
            else if (typeName.Contains("HDRenderPipeline") || typeName.Contains("HDRP"))
            {
                pipelineKind = "HDRP";
            }
            
            return new Dictionary<string, object>
            {
                ["message"] = "Render pipeline asset set successfully",
                ["pipelineKind"] = pipelineKind,
                ["assetPath"] = assetPath,
            };
        }
        
        private object GetRenderPipelineSettings(Dictionary<string, object> payload)
        {
            var currentPipeline = GraphicsSettings.currentRenderPipeline;
            if (currentPipeline == null)
            {
                throw new InvalidOperationException("No render pipeline is currently active.");
            }
            
            var result = new Dictionary<string, object>
            {
                ["pipelineName"] = currentPipeline.name,
                ["pipelineType"] = currentPipeline.GetType().FullName,
            };
            
            // Get all public properties
            var properties = new Dictionary<string, object>();
            var pipelineType = currentPipeline.GetType();
            
            foreach (var prop in pipelineType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.CanRead)
                {
                    try
                    {
                        var value = prop.GetValue(currentPipeline);
                        properties[prop.Name] = value?.ToString() ?? "null";
                    }
                    catch
                    {
                        // Skip properties that throw exceptions when accessed
                    }
                }
            }
            
            result["properties"] = properties;
            return result;
        }
        
        private object UpdateRenderPipelineSettings(Dictionary<string, object> payload)
        {
            var currentPipeline = GraphicsSettings.currentRenderPipeline;
            if (currentPipeline == null)
            {
                throw new InvalidOperationException("No render pipeline is currently active.");
            }
            
            if (!payload.TryGetValue("settings", out var settingsObj) || !(settingsObj is Dictionary<string, object> settings))
            {
                throw new InvalidOperationException("settings dictionary is required");
            }
            
            var pipelineType = currentPipeline.GetType();
            var updatedProperties = new List<string>();
            
            foreach (var kvp in settings)
            {
                var prop = pipelineType.GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.Instance);
                if (prop != null && prop.CanWrite)
                {
                    try
                    {
                        var converted = ConvertValue(kvp.Value, prop.PropertyType);
                        prop.SetValue(currentPipeline, converted);
                        updatedProperties.Add(kvp.Key);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failed to set property {kvp.Key}: {ex.Message}");
                    }
                }
            }
            
            EditorUtility.SetDirty(currentPipeline);
            AssetDatabase.SaveAssets();
            
            return new Dictionary<string, object>
            {
                ["message"] = "Render pipeline settings updated",
                ["updatedProperties"] = updatedProperties,
            };
        }
        
        #endregion
        
        #region Helper Methods
        
        private object ConvertValue(object value, Type targetType)
        {
            if (value == null) return null;
            if (targetType.IsAssignableFrom(value.GetType())) return value;
            
            // Handle nullable types
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                targetType = Nullable.GetUnderlyingType(targetType);
            }
            
            // Handle enums
            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, value.ToString(), true);
            }
            
            // Handle common conversions
            return Convert.ChangeType(value, targetType);
        }
        
        #endregion
    }
}

