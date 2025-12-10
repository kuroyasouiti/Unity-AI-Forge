using System;
using MCP.Editor.Interfaces;
using UnityEngine;

namespace MCP.Editor.Base.ValueConverters
{
    /// <summary>
    /// プリミティブ型（int, float, bool, string等）の変換を担当するコンバーター。
    /// </summary>
    public class PrimitiveValueConverter : IValueConverter
    {
        public int Priority => 100;

        public bool CanConvert(Type targetType)
        {
            return targetType == typeof(float)
                || targetType == typeof(int)
                || targetType == typeof(bool)
                || targetType == typeof(string)
                || targetType == typeof(double)
                || targetType == typeof(long)
                || targetType == typeof(short)
                || targetType == typeof(byte);
        }

        public object Convert(object value, Type targetType)
        {
            if (value == null)
                return targetType == typeof(string) ? null : Activator.CreateInstance(targetType);

            if (targetType.IsInstanceOfType(value))
                return value;

            if (targetType == typeof(float))
            {
                if (value is double d) return (float)d;
                if (value is long l) return (float)l;
                if (value is int i) return (float)i;
                return System.Convert.ToSingle(value);
            }

            if (targetType == typeof(int))
            {
                if (value is long l) return (int)l;
                if (value is double d) return (int)d;
                return System.Convert.ToInt32(value);
            }

            if (targetType == typeof(bool))
            {
                if (value is string s) return bool.Parse(s);
                return System.Convert.ToBoolean(value);
            }

            if (targetType == typeof(string))
            {
                return value?.ToString();
            }

            if (targetType == typeof(double))
            {
                return System.Convert.ToDouble(value);
            }

            if (targetType == typeof(long))
            {
                return System.Convert.ToInt64(value);
            }

            if (targetType == typeof(short))
            {
                return System.Convert.ToInt16(value);
            }

            if (targetType == typeof(byte))
            {
                return System.Convert.ToByte(value);
            }

            throw new InvalidOperationException($"Cannot convert to {targetType.Name}");
        }
    }
}
