using System.Collections.Generic;

namespace MCP.Editor.Interfaces
{
    /// <summary>
    /// ペイロードバリデーターインターフェース。
    /// コマンド実行前にペイロードの妥当性を検証します。
    /// </summary>
    public interface IPayloadValidator
    {
        /// <summary>
        /// ペイロードをバリデートします。
        /// </summary>
        /// <param name="payload">バリデート対象のペイロード</param>
        /// <param name="operation">操作名</param>
        /// <returns>バリデーション結果</returns>
        ValidationResult Validate(Dictionary<string, object> payload, string operation);
    }
    
    /// <summary>
    /// バリデーション結果を表すクラス。
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// バリデーションが成功したかどうか。
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// バリデーションエラーのリスト。
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
        
        /// <summary>
        /// 正規化されたペイロード。
        /// デフォルト値の適用や型変換を行った結果。
        /// </summary>
        public Dictionary<string, object> NormalizedPayload { get; set; }
        
        /// <summary>
        /// 成功したバリデーション結果を作成します。
        /// </summary>
        public static ValidationResult Success(Dictionary<string, object> normalizedPayload = null)
        {
            return new ValidationResult
            {
                IsValid = true,
                NormalizedPayload = normalizedPayload
            };
        }
        
        /// <summary>
        /// 失敗したバリデーション結果を作成します。
        /// </summary>
        public static ValidationResult Failure(params string[] errors)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<string>(errors)
            };
        }
        
        /// <summary>
        /// エラーを追加します。
        /// </summary>
        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }
    }
}

