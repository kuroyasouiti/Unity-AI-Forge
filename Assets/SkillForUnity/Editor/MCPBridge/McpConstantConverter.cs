using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MCP.Editor
{
    /// <summary>
    /// Provides utilities for converting between Unity constants and their numeric values.
    /// Supports enum types, Unity built-in colors, and layer names.
    /// </summary>
    internal static class McpConstantConverter
    {
        // Cache for resolved types to improve performance
        private static readonly Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();
        
        /// <summary>
        /// Resolves a type by name, searching through all loaded assemblies.
        /// Supports both simple names (e.g., "UnityEngine.KeyCode") and assembly-qualified names.
        /// </summary>
        private static Type ResolveType(string typeName)
        {
            if (_typeCache.TryGetValue(typeName, out var cachedType))
            {
                return cachedType;
            }
            
            // Try simple Type.GetType first
            var type = Type.GetType(typeName);
            
            // If not found, search through all loaded assemblies
            if (type == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        break;
                    }
                }
            }
            
            // Cache the result (even if null) to avoid repeated searches
            if (type != null)
            {
                _typeCache[typeName] = type;
            }
            
            return type;
        }
        
        /// <summary>
        /// Converts an enum value name to its numeric value.
        /// </summary>
        /// <param name="enumTypeName">Fully qualified enum type name (e.g., "UnityEngine.KeyCode")</param>
        /// <param name="enumValueName">Enum value name (e.g., "Space")</param>
        /// <returns>Numeric value of the enum</returns>
        /// <exception cref="ArgumentException">Thrown when enum type or value is not found</exception>
        public static int EnumNameToValue(string enumTypeName, string enumValueName)
        {
            var enumType = ResolveType(enumTypeName);
            if (enumType == null || !enumType.IsEnum)
            {
                // Provide helpful error message with available assemblies
                var assemblies = string.Join(", ", AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.GetName().Name.Contains("Unity"))
                    .Select(a => a.GetName().Name)
                    .Take(5));
                throw new ArgumentException(
                    $"Enum type '{enumTypeName}' not found. " +
                    $"Available Unity assemblies: {assemblies}. " +
                    $"Use fully qualified name or check spelling."
                );
            }

            if (!Enum.IsDefined(enumType, enumValueName))
            {
                var availableValues = string.Join(", ", Enum.GetNames(enumType).Take(10));
                throw new ArgumentException(
                    $"Enum value '{enumValueName}' not found in type '{enumTypeName}'. " +
                    $"Available values (first 10): {availableValues}"
                );
            }

            return (int)Enum.Parse(enumType, enumValueName);
        }

        /// <summary>
        /// Converts a numeric value to its enum value name.
        /// </summary>
        /// <param name="enumTypeName">Fully qualified enum type name</param>
        /// <param name="numericValue">Numeric value</param>
        /// <returns>Enum value name</returns>
        /// <exception cref="ArgumentException">Thrown when enum type is not found</exception>
        public static string EnumValueToName(string enumTypeName, int numericValue)
        {
            var enumType = ResolveType(enumTypeName);
            if (enumType == null || !enumType.IsEnum)
            {
                throw new ArgumentException($"Enum type '{enumTypeName}' not found");
            }

            if (!Enum.IsDefined(enumType, numericValue))
            {
                // Return numeric value as string if not defined
                return numericValue.ToString();
            }

            return Enum.GetName(enumType, numericValue);
        }

        /// <summary>
        /// Converts Unity built-in color name to RGBA values.
        /// </summary>
        /// <param name="colorName">Color name (e.g., "red", "green", "blue")</param>
        /// <returns>Dictionary with r, g, b, a keys (values 0-1)</returns>
        /// <exception cref="ArgumentException">Thrown when color name is not recognized</exception>
        public static Dictionary<string, object> ColorNameToRGBA(string colorName)
        {
            Color color;

            switch (colorName.ToLower())
            {
                case "red": color = Color.red; break;
                case "green": color = Color.green; break;
                case "blue": color = Color.blue; break;
                case "white": color = Color.white; break;
                case "black": color = Color.black; break;
                case "yellow": color = Color.yellow; break;
                case "cyan": color = Color.cyan; break;
                case "magenta": color = Color.magenta; break;
                case "gray": case "grey": color = Color.gray; break;
                case "clear": color = Color.clear; break;
                default:
                    throw new ArgumentException($"Color name '{colorName}' not recognized");
            }

            return new Dictionary<string, object>
            {
                ["r"] = color.r,
                ["g"] = color.g,
                ["b"] = color.b,
                ["a"] = color.a
            };
        }

        /// <summary>
        /// Converts RGBA values to the nearest Unity built-in color name.
        /// </summary>
        /// <param name="r">Red component (0-1)</param>
        /// <param name="g">Green component (0-1)</param>
        /// <param name="b">Blue component (0-1)</param>
        /// <param name="a">Alpha component (0-1)</param>
        /// <returns>Nearest color name or null if no close match</returns>
        public static string RGBAToColorName(float r, float g, float b, float a)
        {
            var targetColor = new Color(r, g, b, a);

            var colorMap = new Dictionary<string, Color>
            {
                ["red"] = Color.red,
                ["green"] = Color.green,
                ["blue"] = Color.blue,
                ["white"] = Color.white,
                ["black"] = Color.black,
                ["yellow"] = Color.yellow,
                ["cyan"] = Color.cyan,
                ["magenta"] = Color.magenta,
                ["gray"] = Color.gray,
                ["clear"] = Color.clear
            };

            float minDistance = float.MaxValue;
            string nearestColor = null;
            const float threshold = 0.1f; // Tolerance for color matching

            foreach (var kvp in colorMap)
            {
                var distance = ColorDistance(targetColor, kvp.Value);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestColor = kvp.Key;
                }
            }

            return minDistance <= threshold ? nearestColor : null;
        }

        /// <summary>
        /// Converts layer name to layer index.
        /// </summary>
        /// <param name="layerName">Layer name</param>
        /// <returns>Layer index (0-31)</returns>
        /// <exception cref="ArgumentException">Thrown when layer name is not found</exception>
        public static int LayerNameToIndex(string layerName)
        {
            var layerIndex = LayerMask.NameToLayer(layerName);
            if (layerIndex == -1)
            {
                throw new ArgumentException($"Layer '{layerName}' not found");
            }
            return layerIndex;
        }

        /// <summary>
        /// Converts layer index to layer name.
        /// </summary>
        /// <param name="layerIndex">Layer index (0-31)</param>
        /// <returns>Layer name</returns>
        /// <exception cref="ArgumentException">Thrown when layer index is invalid</exception>
        public static string LayerIndexToName(int layerIndex)
        {
            if (layerIndex < 0 || layerIndex > 31)
            {
                throw new ArgumentException($"Layer index must be between 0 and 31, got {layerIndex}");
            }

            var layerName = LayerMask.LayerToName(layerIndex);
            if (string.IsNullOrEmpty(layerName))
            {
                throw new ArgumentException($"Layer at index {layerIndex} is not defined");
            }
            return layerName;
        }

        /// <summary>
        /// Lists all available enum values for a given enum type.
        /// </summary>
        /// <param name="enumTypeName">Fully qualified enum type name</param>
        /// <returns>Dictionary mapping enum names to their numeric values</returns>
        /// <exception cref="ArgumentException">Thrown when enum type is not found</exception>
        public static Dictionary<string, int> ListEnumValues(string enumTypeName)
        {
            var enumType = ResolveType(enumTypeName);
            if (enumType == null || !enumType.IsEnum)
            {
                throw new ArgumentException($"Enum type '{enumTypeName}' not found");
            }

            var result = new Dictionary<string, int>();
            foreach (var value in Enum.GetValues(enumType))
            {
                result[value.ToString()] = (int)value;
            }
            return result;
        }

        /// <summary>
        /// Lists all Unity built-in color names.
        /// </summary>
        /// <returns>List of color names</returns>
        public static List<string> ListColorNames()
        {
            return new List<string>
            {
                "red", "green", "blue", "white", "black",
                "yellow", "cyan", "magenta", "gray", "clear"
            };
        }

        /// <summary>
        /// Lists all layer names and their indices.
        /// </summary>
        /// <returns>Dictionary mapping layer names to their indices</returns>
        public static Dictionary<string, int> ListLayers()
        {
            var result = new Dictionary<string, int>();
            for (int i = 0; i < 32; i++)
            {
                var layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    result[layerName] = i;
                }
            }
            return result;
        }

        /// <summary>
        /// Calculates the Euclidean distance between two colors.
        /// </summary>
        private static float ColorDistance(Color c1, Color c2)
        {
            var dr = c1.r - c2.r;
            var dg = c1.g - c2.g;
            var db = c1.b - c2.b;
            var da = c1.a - c2.a;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db + da * da);
        }
        
        /// <summary>
        /// Lists commonly used Unity enum types for quick reference.
        /// </summary>
        /// <returns>Dictionary mapping category names to enum type names</returns>
        public static Dictionary<string, List<string>> ListCommonUnityEnums()
        {
            return new Dictionary<string, List<string>>
            {
                ["Input"] = new List<string>
                {
                    "UnityEngine.KeyCode",
                    "UnityEngine.TouchPhase",
                    "UnityEngine.IMECompositionMode"
                },
                ["Rendering"] = new List<string>
                {
                    "UnityEngine.RenderingPath",
                    "UnityEngine.LightType",
                    "UnityEngine.LightShadows",
                    "UnityEngine.ShadowQuality",
                    "UnityEngine.ColorSpace",
                    "UnityEngine.CameraClearFlags"
                },
                ["Physics"] = new List<string>
                {
                    "UnityEngine.CollisionDetectionMode",
                    "UnityEngine.RigidbodyConstraints",
                    "UnityEngine.ForceMode"
                },
                ["UI"] = new List<string>
                {
                    "UnityEngine.TextAnchor",
                    "UnityEngine.FontStyle",
                    "UnityEngine.UI.Image+Type",
                    "UnityEngine.UI.Image+FillMethod"
                },
                ["Audio"] = new List<string>
                {
                    "UnityEngine.AudioRolloffMode",
                    "UnityEngine.AudioVelocityUpdateMode"
                },
                ["Animation"] = new List<string>
                {
                    "UnityEngine.AnimatorCullingMode",
                    "UnityEngine.AnimatorUpdateMode",
                    "UnityEngine.WrapMode"
                },
                ["Scripting"] = new List<string>
                {
                    "UnityEngine.RuntimePlatform",
                    "UnityEngine.HideFlags",
                    "UnityEngine.Space"
                }
            };
        }
    }
}
