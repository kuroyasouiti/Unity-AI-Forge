using System.Collections.Generic;

namespace MCP.Editor.Interfaces
{
    /// <summary>
    /// 個別の操作ハンドラーインターフェース。
    /// 各操作（create, delete, update等）に対応するハンドラーを定義します。
    /// </summary>
    public interface IOperationHandler
    {
        /// <summary>
        /// 操作名（例: "create", "delete", "update"）
        /// </summary>
        string OperationName { get; }
        
        /// <summary>
        /// 操作を実行します。
        /// </summary>
        /// <param name="payload">操作パラメータ</param>
        /// <returns>実行結果</returns>
        object Execute(Dictionary<string, object> payload);
        
        /// <summary>
        /// この操作が読み取り専用かどうか。
        /// 読み取り専用操作はコンパイル待機をスキップできます。
        /// </summary>
        bool IsReadOnly { get; }
        
        /// <summary>
        /// 必須パラメータのリスト。
        /// バリデーション時に使用されます。
        /// </summary>
        IEnumerable<string> RequiredParameters { get; }
        
        /// <summary>
        /// オプションパラメータのリスト。
        /// ドキュメント生成に使用されます。
        /// </summary>
        IEnumerable<string> OptionalParameters { get; }
    }
}

