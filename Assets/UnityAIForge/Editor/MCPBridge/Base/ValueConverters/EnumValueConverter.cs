using System;
using System.Collections.Generic;
using MCP.Editor.Interfaces;

namespace MCP.Editor.Base.ValueConverters
{
    /// <summary>
    /// 列挙型の変換を担当するコンバーター。
    /// </summary>
    public class EnumValueConverter : IValueConverter
    {
        public int Priority => 150;

        public bool CanConvert(Type targetType)
        {
            return targetType.IsEnum;
        }

        public object Convert(object value, Type targetType)
        {
            if (value == null)
                return Activator.CreateInstance(targetType);

            if (targetType.IsInstanceOfType(value))
                return value;

            // Handle string values
            if (value is string enumStr)
            {
                return Enum.Parse(targetType, enumStr, ignoreCase: true);
            }

            // Handle numeric types directly
            if (value is int || value is long || value is short || value is byte ||
                value is uint || value is ulong || value is ushort || value is sbyte ||
                value is float || value is double || value is decimal)
            {
                return Enum.ToObject(targetType, System.Convert.ToInt32(value));
            }

            // Handle dictionary representation (e.g., from JSON {"value": "EnumName"} or {"value": 0})
            if (value is IDictionary<string, object> dict)
            {
                if (dict.TryGetValue("value", out var innerValue))
                {
                    return Convert(innerValue, targetType);
                }
                // Try common key names
                if (dict.TryGetValue("name", out innerValue))
                {
                    return Convert(innerValue, targetType);
                }
            }

            // Try to parse from ToString() as last resort
            string stringValue = value.ToString();
            if (!string.IsNullOrEmpty(stringValue))
            {
                // Try to parse as enum name
                if (Enum.IsDefined(targetType, stringValue))
                {
                    return Enum.Parse(targetType, stringValue, ignoreCase: true);
                }

                // Try case-insensitive match
                foreach (var name in Enum.GetNames(targetType))
                {
                    if (string.Equals(name, stringValue, StringComparison.OrdinalIgnoreCase))
                    {
                        return Enum.Parse(targetType, name);
                    }
                }

                // Try to parse as integer
                if (int.TryParse(stringValue, out int intValue))
                {
                    return Enum.ToObject(targetType, intValue);
                }
            }

            throw new InvalidOperationException(
                $"Cannot convert value of type {value.GetType().Name} to enum {targetType.Name}. " +
                $"Value: {value}");
        }
    }
}
