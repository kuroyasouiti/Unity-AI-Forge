using System;
using System.Collections.Generic;
using MCP.Editor.Interfaces;
using UnityEngine;

namespace MCP.Editor.Base.ValueConverters
{
    /// <summary>
    /// Unity構造体（Vector2, Vector3, Color, Quaternion等）の変換を担当するコンバーター。
    /// </summary>
    public class UnityStructValueConverter : IValueConverter
    {
        public int Priority => 200;

        public bool CanConvert(Type targetType)
        {
            return targetType == typeof(Vector2)
                || targetType == typeof(Vector3)
                || targetType == typeof(Vector4)
                || targetType == typeof(Color)
                || targetType == typeof(Color32)
                || targetType == typeof(Quaternion)
                || targetType == typeof(Rect)
                || targetType == typeof(Bounds);
        }

        public object Convert(object value, Type targetType)
        {
            if (value == null)
                return Activator.CreateInstance(targetType);

            if (targetType.IsInstanceOfType(value))
                return value;

            if (!(value is Dictionary<string, object> dict))
            {
                throw new InvalidOperationException(
                    $"Cannot convert {value.GetType().Name} to {targetType.Name}. Expected a dictionary.");
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
    }
}
