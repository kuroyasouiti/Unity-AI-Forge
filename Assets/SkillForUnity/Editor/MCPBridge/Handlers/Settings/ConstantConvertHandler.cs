using System;
using System.Collections.Generic;
using MCP.Editor.Base;

namespace MCP.Editor.Handlers.Settings
{
    /// <summary>
    /// 定数変換のコマンドハンドラー。
    /// Enum、Color、Layer変換をサポート。
    /// </summary>
    public class ConstantConvertHandler : BaseCommandHandler
    {
        public override string Category => "constantConvert";
        
        public override IEnumerable<string> SupportedOperations => new[]
        {
            // Enum conversions
            "enumToValue",
            "valueToEnum",
            "listEnums",
            "listCommonEnums",
            // Color conversions
            "colorToRGBA",
            "rgbaToColor",
            "listColors",
            // Layer conversions
            "layerToIndex",
            "indexToLayer",
            "listLayers",
        };
        
        public ConstantConvertHandler() : base()
        {
        }
        
        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                // Enum conversions
                "enumToValue" => ConvertEnumToValue(payload),
                "valueToEnum" => ConvertValueToEnum(payload),
                "listEnums" => ListEnumValues(payload),
                "listCommonEnums" => ListCommonEnumTypes(),
                // Color conversions
                "colorToRGBA" => ConvertColorToRGBA(payload),
                "rgbaToColor" => ConvertRGBAToColor(payload),
                "listColors" => ListConstantColors(),
                // Layer conversions
                "layerToIndex" => ConvertLayerToIndex(payload),
                "indexToLayer" => ConvertIndexToLayer(payload),
                "listLayers" => ListConstantLayers(),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }
        
        protected override bool RequiresCompilationWait(string operation)
        {
            // Constant conversions don't require compilation wait
            return false;
        }
        
        #region Enum Conversions
        
        private object ConvertEnumToValue(Dictionary<string, object> payload)
        {
            var enumTypeName = GetString(payload, "enumType");
            if (string.IsNullOrEmpty(enumTypeName))
            {
                throw new InvalidOperationException("enumType is required");
            }
            
            var enumValueName = GetString(payload, "enumValue");
            if (string.IsNullOrEmpty(enumValueName))
            {
                throw new InvalidOperationException("enumValue is required");
            }
            
            var numericValue = McpConstantConverter.EnumNameToValue(enumTypeName, enumValueName);
            
            return new Dictionary<string, object>
            {
                ["enumType"] = enumTypeName,
                ["enumValue"] = enumValueName,
                ["numericValue"] = numericValue,
                ["success"] = true
            };
        }
        
        private object ConvertValueToEnum(Dictionary<string, object> payload)
        {
            var enumTypeName = GetString(payload, "enumType");
            if (string.IsNullOrEmpty(enumTypeName))
            {
                throw new InvalidOperationException("enumType is required");
            }
            
            var numericValue = GetInt(payload, "numericValue", 0);
            
            var enumValueName = McpConstantConverter.EnumValueToName(enumTypeName, numericValue);
            
            return new Dictionary<string, object>
            {
                ["enumType"] = enumTypeName,
                ["numericValue"] = numericValue,
                ["enumValue"] = enumValueName,
                ["success"] = true
            };
        }
        
        private object ListEnumValues(Dictionary<string, object> payload)
        {
            var enumTypeName = GetString(payload, "enumType");
            if (string.IsNullOrEmpty(enumTypeName))
            {
                throw new InvalidOperationException("enumType is required");
            }
            
            var enumValues = McpConstantConverter.ListEnumValues(enumTypeName);
            
            return new Dictionary<string, object>
            {
                ["enumType"] = enumTypeName,
                ["values"] = enumValues,
                ["count"] = enumValues.Count,
                ["success"] = true
            };
        }
        
        private object ListCommonEnumTypes()
        {
            var commonEnums = McpConstantConverter.ListCommonUnityEnums();
            var totalCount = 0;
            foreach (var category in commonEnums.Values)
            {
                totalCount += category.Count;
            }
            
            return new Dictionary<string, object>
            {
                ["categories"] = commonEnums,
                ["totalCount"] = totalCount,
                ["success"] = true
            };
        }
        
        #endregion
        
        #region Color Conversions
        
        private object ConvertColorToRGBA(Dictionary<string, object> payload)
        {
            var colorName = GetString(payload, "colorName");
            if (string.IsNullOrEmpty(colorName))
            {
                throw new InvalidOperationException("colorName is required");
            }
            
            var rgba = McpConstantConverter.ColorNameToRGBA(colorName);
            
            return new Dictionary<string, object>
            {
                ["colorName"] = colorName,
                ["rgba"] = rgba,
                ["r"] = rgba["r"],
                ["g"] = rgba["g"],
                ["b"] = rgba["b"],
                ["a"] = rgba["a"],
                ["success"] = true
            };
        }
        
        private object ConvertRGBAToColor(Dictionary<string, object> payload)
        {
            var r = GetFloat(payload, "r", 0f);
            var g = GetFloat(payload, "g", 0f);
            var b = GetFloat(payload, "b", 0f);
            var a = GetFloat(payload, "a", 1f);
            
            var colorName = McpConstantConverter.RGBAToColorName(r, g, b, a);
            
            return new Dictionary<string, object>
            {
                ["r"] = r,
                ["g"] = g,
                ["b"] = b,
                ["a"] = a,
                ["colorName"] = colorName ?? "unknown",
                ["matched"] = colorName != null,
                ["success"] = true
            };
        }
        
        private object ListConstantColors()
        {
            var colorNames = McpConstantConverter.ListColorNames();
            
            return new Dictionary<string, object>
            {
                ["colors"] = colorNames,
                ["count"] = colorNames.Count,
                ["success"] = true
            };
        }
        
        #endregion
        
        #region Layer Conversions
        
        private object ConvertLayerToIndex(Dictionary<string, object> payload)
        {
            var layerName = GetString(payload, "layerName");
            if (string.IsNullOrEmpty(layerName))
            {
                throw new InvalidOperationException("layerName is required");
            }
            
            var layerIndex = McpConstantConverter.LayerNameToIndex(layerName);
            
            return new Dictionary<string, object>
            {
                ["layerName"] = layerName,
                ["layerIndex"] = layerIndex,
                ["success"] = true
            };
        }
        
        private object ConvertIndexToLayer(Dictionary<string, object> payload)
        {
            var layerIndex = GetInt(payload, "layerIndex", 0);
            
            var layerName = McpConstantConverter.LayerIndexToName(layerIndex);
            
            return new Dictionary<string, object>
            {
                ["layerIndex"] = layerIndex,
                ["layerName"] = layerName,
                ["success"] = true
            };
        }
        
        private object ListConstantLayers()
        {
            var layers = McpConstantConverter.ListLayers();
            
            return new Dictionary<string, object>
            {
                ["layers"] = layers,
                ["count"] = layers.Count,
                ["success"] = true
            };
        }
        
        #endregion
    }
}

