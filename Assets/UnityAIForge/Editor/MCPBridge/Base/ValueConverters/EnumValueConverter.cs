using System;
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

            if (value is string enumStr)
            {
                return Enum.Parse(targetType, enumStr, ignoreCase: true);
            }

            return Enum.ToObject(targetType, System.Convert.ToInt32(value));
        }
    }
}
