using System;

namespace MCP.Editor.Interfaces
{
    /// <summary>
    /// 値の型変換を行うインターフェース。
    /// Strategy パターンにより、異なる型変換戦略を実装可能。
    /// </summary>
    public interface IValueConverter
    {
        /// <summary>
        /// 指定された型への変換をサポートするかどうかを判定します。
        /// </summary>
        /// <param name="targetType">変換先の型</param>
        /// <returns>サポートする場合はtrue</returns>
        bool CanConvert(Type targetType);

        /// <summary>
        /// 値を指定された型に変換します。
        /// </summary>
        /// <param name="value">変換元の値</param>
        /// <param name="targetType">変換先の型</param>
        /// <returns>変換後の値</returns>
        object Convert(object value, Type targetType);

        /// <summary>
        /// 変換の優先度。値が大きいほど優先的に使用されます。
        /// </summary>
        int Priority { get; }
    }
}
