using System;
using System.Collections.Generic;
using MCP.Editor.Interfaces;
using UnityEngine;

namespace MCP.Editor.Base.ValueConverters
{
    /// <summary>
    /// Unity構造体（Vector2, Vector3, Color, Quaternion等）の変換を担当するコンバーター。
    /// 文字列定数（"red", "zero", "up"等）もサポートします。
    /// </summary>
    public class UnityStructValueConverter : IValueConverter
    {
        public int Priority => 200;

        // Color定数マッピング
        private static readonly Dictionary<string, Color> ColorConstants = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
        {
            { "red", Color.red },
            { "green", Color.green },
            { "blue", Color.blue },
            { "white", Color.white },
            { "black", Color.black },
            { "yellow", Color.yellow },
            { "cyan", Color.cyan },
            { "magenta", Color.magenta },
            { "gray", Color.gray },
            { "grey", Color.grey },
            { "clear", Color.clear }
        };

        // Vector3定数マッピング
        private static readonly Dictionary<string, Vector3> Vector3Constants = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase)
        {
            { "zero", Vector3.zero },
            { "one", Vector3.one },
            { "up", Vector3.up },
            { "down", Vector3.down },
            { "left", Vector3.left },
            { "right", Vector3.right },
            { "forward", Vector3.forward },
            { "back", Vector3.back },
            { "positiveInfinity", Vector3.positiveInfinity },
            { "negativeInfinity", Vector3.negativeInfinity }
        };

        // Vector2定数マッピング
        private static readonly Dictionary<string, Vector2> Vector2Constants = new Dictionary<string, Vector2>(StringComparer.OrdinalIgnoreCase)
        {
            { "zero", Vector2.zero },
            { "one", Vector2.one },
            { "up", Vector2.up },
            { "down", Vector2.down },
            { "left", Vector2.left },
            { "right", Vector2.right },
            { "positiveInfinity", Vector2.positiveInfinity },
            { "negativeInfinity", Vector2.negativeInfinity }
        };

        // Vector4定数マッピング
        private static readonly Dictionary<string, Vector4> Vector4Constants = new Dictionary<string, Vector4>(StringComparer.OrdinalIgnoreCase)
        {
            { "zero", Vector4.zero },
            { "one", Vector4.one },
            { "positiveInfinity", Vector4.positiveInfinity },
            { "negativeInfinity", Vector4.negativeInfinity }
        };

        // Quaternion定数マッピング
        private static readonly Dictionary<string, Quaternion> QuaternionConstants = new Dictionary<string, Quaternion>(StringComparer.OrdinalIgnoreCase)
        {
            { "identity", Quaternion.identity }
        };

        // LayerMask定数マッピング
        private static readonly Dictionary<string, int> LayerMaskConstants = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "nothing", 0 },
            { "everything", ~0 },
            { "default", 1 << 0 },
            { "transparentfx", 1 << 1 },
            { "ignoreraycast", 1 << 2 },
            { "water", 1 << 4 },
            { "ui", 1 << 5 }
        };

        public bool CanConvert(Type targetType)
        {
            return targetType == typeof(Vector2)
                || targetType == typeof(Vector3)
                || targetType == typeof(Vector4)
                || targetType == typeof(Color)
                || targetType == typeof(Color32)
                || targetType == typeof(Quaternion)
                || targetType == typeof(Rect)
                || targetType == typeof(Bounds)
                || targetType == typeof(LayerMask);
        }

        public object Convert(object value, Type targetType)
        {
            if (value == null)
                return Activator.CreateInstance(targetType);

            if (targetType.IsInstanceOfType(value))
                return value;

            // LayerMaskの場合は整数値から直接変換可能
            if (targetType == typeof(LayerMask))
            {
                if (value is int intVal)
                    return (LayerMask)intVal;
                if (value is long longVal)
                    return (LayerMask)(int)longVal;
                if (value is float floatVal)
                    return (LayerMask)(int)floatVal;
                if (value is double doubleVal)
                    return (LayerMask)(int)doubleVal;
            }

            // 文字列定数のサポート
            if (value is string stringValue)
            {
                // LayerMaskの場合、カンマ区切りのレイヤー名リストをサポート
                if (targetType == typeof(LayerMask))
                {
                    // カンマ区切りで複数レイヤーを指定できる
                    var layerNames = stringValue.Split(',');
                    int mask = 0;
                    foreach (var layerName in layerNames)
                    {
                        mask |= ConvertLayerNameToMask(layerName);
                    }
                    return (LayerMask)mask;
                }

                var result = TryConvertFromConstant(stringValue, targetType);
                if (result != null)
                    return result;

                throw new InvalidOperationException(
                    $"Unknown constant '{stringValue}' for type {targetType.Name}.");
            }

            if (!(value is Dictionary<string, object> dict))
            {
                throw new InvalidOperationException(
                    $"Cannot convert {value.GetType().Name} to {targetType.Name}. Expected a dictionary or constant name.");
            }

            if (targetType == typeof(Vector3))
            {
                return new Vector3(
                    GetFloat(dict, "x"),
                    GetFloat(dict, "y"),
                    GetFloat(dict, "z")
                );
            }

            if (targetType == typeof(Vector2))
            {
                return new Vector2(
                    GetFloat(dict, "x"),
                    GetFloat(dict, "y")
                );
            }

            if (targetType == typeof(Vector4))
            {
                return new Vector4(
                    GetFloat(dict, "x"),
                    GetFloat(dict, "y"),
                    GetFloat(dict, "z"),
                    GetFloat(dict, "w")
                );
            }

            if (targetType == typeof(Color))
            {
                return new Color(
                    GetFloat(dict, "r", 1f),
                    GetFloat(dict, "g", 1f),
                    GetFloat(dict, "b", 1f),
                    GetFloat(dict, "a", 1f)
                );
            }

            if (targetType == typeof(Color32))
            {
                return new Color32(
                    GetByte(dict, "r", 255),
                    GetByte(dict, "g", 255),
                    GetByte(dict, "b", 255),
                    GetByte(dict, "a", 255)
                );
            }

            if (targetType == typeof(Quaternion))
            {
                return new Quaternion(
                    GetFloat(dict, "x"),
                    GetFloat(dict, "y"),
                    GetFloat(dict, "z"),
                    GetFloat(dict, "w", 1f)
                );
            }

            if (targetType == typeof(Rect))
            {
                return new Rect(
                    GetFloat(dict, "x"),
                    GetFloat(dict, "y"),
                    GetFloat(dict, "width"),
                    GetFloat(dict, "height")
                );
            }

            if (targetType == typeof(Bounds))
            {
                var center = Vector3.zero;
                var size = Vector3.zero;

                if (dict.TryGetValue("center", out var centerObj) && centerObj is Dictionary<string, object> centerDict)
                {
                    center = new Vector3(
                        GetFloat(centerDict, "x"),
                        GetFloat(centerDict, "y"),
                        GetFloat(centerDict, "z")
                    );
                }

                if (dict.TryGetValue("size", out var sizeObj) && sizeObj is Dictionary<string, object> sizeDict)
                {
                    size = new Vector3(
                        GetFloat(sizeDict, "x"),
                        GetFloat(sizeDict, "y"),
                        GetFloat(sizeDict, "z")
                    );
                }

                return new Bounds(center, size);
            }

            if (targetType == typeof(LayerMask))
            {
                // Dictionary with "value" key
                if (dict.TryGetValue("value", out var maskValue))
                {
                    return (LayerMask)GetInt(dict, "value");
                }
                // Dictionary with "layers" key (array of layer names)
                if (dict.TryGetValue("layers", out var layersObj) && layersObj is IList<object> layersList)
                {
                    int mask = 0;
                    foreach (var layer in layersList)
                    {
                        mask |= ConvertLayerNameToMask(layer.ToString());
                    }
                    return (LayerMask)mask;
                }
                throw new InvalidOperationException("LayerMask dictionary must have 'value' or 'layers' key.");
            }

            throw new InvalidOperationException($"Cannot convert to {targetType.Name}");
        }

        private static float GetFloat(Dictionary<string, object> dict, string key, float defaultValue = 0f)
        {
            if (!dict.TryGetValue(key, out var value))
                return defaultValue;

            if (value is float f) return f;
            if (value is double d) return (float)d;
            if (value is int i) return i;
            if (value is long l) return l;

            return System.Convert.ToSingle(value);
        }

        private static byte GetByte(Dictionary<string, object> dict, string key, byte defaultValue = 0)
        {
            if (!dict.TryGetValue(key, out var value))
                return defaultValue;

            if (value is byte b) return b;
            if (value is int i) return (byte)i;
            if (value is long l) return (byte)l;

            return System.Convert.ToByte(value);
        }

        private static int GetInt(Dictionary<string, object> dict, string key, int defaultValue = 0)
        {
            if (!dict.TryGetValue(key, out var value))
                return defaultValue;

            if (value is int i) return i;
            if (value is long l) return (int)l;
            if (value is float f) return (int)f;
            if (value is double d) return (int)d;

            return System.Convert.ToInt32(value);
        }

        /// <summary>
        /// レイヤー名をLayerMaskのビットマスク値に変換します。
        /// </summary>
        private static int ConvertLayerNameToMask(string layerName)
        {
            if (string.IsNullOrEmpty(layerName))
                return 0;

            // 定数名チェック
            if (LayerMaskConstants.TryGetValue(layerName.Trim(), out var constantMask))
                return constantMask;

            // Unityのレイヤー名からマスクを取得
            int layerIndex = LayerMask.NameToLayer(layerName.Trim());
            if (layerIndex >= 0)
                return 1 << layerIndex;

            // 数値として解析を試みる
            if (int.TryParse(layerName.Trim(), out var numericValue))
                return numericValue;

            Debug.LogWarning($"Unknown layer name: {layerName}");
            return 0;
        }

        /// <summary>
        /// 文字列定数からUnity構造体への変換を試みます。
        /// </summary>
        /// <param name="constantName">定数名（例: "red", "zero", "up"）</param>
        /// <param name="targetType">変換先の型</param>
        /// <returns>変換に成功した場合は変換後の値、失敗した場合はnull</returns>
        private object TryConvertFromConstant(string constantName, Type targetType)
        {
            if (string.IsNullOrEmpty(constantName))
                return null;

            if (targetType == typeof(Color))
            {
                if (ColorConstants.TryGetValue(constantName, out var color))
                    return color;
            }
            else if (targetType == typeof(Color32))
            {
                // Color32はColorから変換
                if (ColorConstants.TryGetValue(constantName, out var color))
                    return (Color32)color;
            }
            else if (targetType == typeof(Vector3))
            {
                if (Vector3Constants.TryGetValue(constantName, out var vec3))
                    return vec3;
            }
            else if (targetType == typeof(Vector2))
            {
                if (Vector2Constants.TryGetValue(constantName, out var vec2))
                    return vec2;
            }
            else if (targetType == typeof(Vector4))
            {
                if (Vector4Constants.TryGetValue(constantName, out var vec4))
                    return vec4;
            }
            else if (targetType == typeof(Quaternion))
            {
                if (QuaternionConstants.TryGetValue(constantName, out var quat))
                    return quat;
            }

            return null;
        }

        /// <summary>
        /// 指定された型でサポートされている定数名の一覧を取得します。
        /// </summary>
        /// <param name="targetType">対象の型</param>
        /// <returns>サポートされている定数名の配列</returns>
        public static string[] GetSupportedConstants(Type targetType)
        {
            if (targetType == typeof(Color) || targetType == typeof(Color32))
            {
                return new List<string>(ColorConstants.Keys).ToArray();
            }
            else if (targetType == typeof(Vector3))
            {
                return new List<string>(Vector3Constants.Keys).ToArray();
            }
            else if (targetType == typeof(Vector2))
            {
                return new List<string>(Vector2Constants.Keys).ToArray();
            }
            else if (targetType == typeof(Vector4))
            {
                return new List<string>(Vector4Constants.Keys).ToArray();
            }
            else if (targetType == typeof(Quaternion))
            {
                return new List<string>(QuaternionConstants.Keys).ToArray();
            }
            else if (targetType == typeof(LayerMask))
            {
                return new List<string>(LayerMaskConstants.Keys).ToArray();
            }

            return Array.Empty<string>();
        }
    }
}
