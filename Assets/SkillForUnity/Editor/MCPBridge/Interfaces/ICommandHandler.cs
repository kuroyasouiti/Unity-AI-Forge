using System.Collections.Generic;

namespace MCP.Editor.Interfaces
{
    /// <summary>
    /// コマンドハンドラーの基本インターフェース。
    /// 各カテゴリのコマンド（Scene, GameObject, Component等）はこのインターフェースを実装します。
    /// </summary>
    public interface ICommandHandler
    {
        /// <summary>
        /// コマンドを実行します。
        /// </summary>
        /// <param name="payload">コマンドパラメータ（operation, その他のパラメータを含む）</param>
        /// <returns>実行結果（通常は Dictionary&lt;string, object&gt; 形式）</returns>
        /// <exception cref="System.InvalidOperationException">操作が不正または実行できない場合</exception>
        object Execute(Dictionary<string, object> payload);
        
        /// <summary>
        /// このハンドラーが対応している操作のリスト。
        /// 例: ["create", "delete", "update", "inspect"]
        /// </summary>
        IEnumerable<string> SupportedOperations { get; }
        
        /// <summary>
        /// ハンドラーのカテゴリ名。
        /// 例: "scene", "gameObject", "component"
        /// </summary>
        string Category { get; }
        
        /// <summary>
        /// ハンドラーのバージョン。
        /// セマンティックバージョニング形式（例: "1.0.0"）
        /// </summary>
        string Version { get; }
    }
}

