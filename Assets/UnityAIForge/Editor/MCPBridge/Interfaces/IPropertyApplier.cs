using System.Collections.Generic;
using UnityEngine;

namespace MCP.Editor.Interfaces
{
    /// <summary>
    /// コンポーネントへのプロパティ適用結果。
    /// </summary>
    public class PropertyApplyResult
    {
        /// <summary>
        /// 正常に更新されたプロパティ名のリスト。
        /// </summary>
        public List<string> Updated { get; } = new List<string>();

        /// <summary>
        /// 更新に失敗したプロパティ名のリスト。
        /// </summary>
        public List<string> Failed { get; } = new List<string>();

        /// <summary>
        /// エラーメッセージのディクショナリ（プロパティ名 → エラーメッセージ）。
        /// </summary>
        public Dictionary<string, string> Errors { get; } = new Dictionary<string, string>();

        /// <summary>
        /// すべてのプロパティが正常に更新されたかどうか。
        /// </summary>
        public bool AllSucceeded => Failed.Count == 0;
    }

    /// <summary>
    /// コンポーネントにプロパティを適用するインターフェース。
    /// </summary>
    public interface IPropertyApplier
    {
        /// <summary>
        /// コンポーネントにプロパティ変更を適用します。
        /// </summary>
        /// <param name="component">対象コンポーネント</param>
        /// <param name="propertyChanges">プロパティ名と値のディクショナリ</param>
        /// <returns>適用結果</returns>
        PropertyApplyResult ApplyProperties(Component component, Dictionary<string, object> propertyChanges);
    }
}
