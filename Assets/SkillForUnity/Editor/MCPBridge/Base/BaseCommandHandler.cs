using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Interfaces;

namespace MCP.Editor.Base
{
    /// <summary>
    /// コマンドハンドラーの基底クラス。
    /// 共通のロジックを実装し、派生クラスで個別の操作を実装します。
    /// </summary>
    public abstract class BaseCommandHandler : ICommandHandler
    {
        #region ICommandHandler Implementation
        
        /// <inheritdoc/>
        public abstract IEnumerable<string> SupportedOperations { get; }
        
        /// <inheritdoc/>
        public abstract string Category { get; }
        
        /// <inheritdoc/>
        public virtual string Version => "1.0.0";
        
        /// <inheritdoc/>
        public object Execute(Dictionary<string, object> payload)
        {
            try
            {
                // 1. ペイロードの検証
                ValidatePayload(payload);
                
                // 2. 操作の取得
                var operation = GetOperation(payload);
                
                // 3. 操作のサポート確認
                if (!SupportedOperations.Contains(operation))
                {
                    throw new InvalidOperationException(
                        $"Operation '{operation}' is not supported by {Category} handler. " +
                        $"Supported operations: {string.Join(", ", SupportedOperations)}"
                    );
                }
                
                // 4. コンパイル待機（必要な場合）
                Dictionary<string, object> compilationWaitInfo = null;
                if (RequiresCompilationWait(operation))
                {
                    compilationWaitInfo = WaitForCompilationIfNeeded(operation);
                }
                
                // 5. 操作の実行
                var result = ExecuteOperation(operation, payload);
                
                // 6. コンパイル待機情報の追加
                if (compilationWaitInfo != null && result is Dictionary<string, object> resultDict)
                {
                    resultDict["compilationWait"] = compilationWaitInfo;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex);
            }
        }
        
        #endregion
        
        #region Abstract Methods
        
        /// <summary>
        /// 指定された操作を実行します。
        /// 派生クラスで実装します。
        /// </summary>
        /// <param name="operation">操作名</param>
        /// <param name="payload">操作パラメータ</param>
        /// <returns>実行結果</returns>
        protected abstract object ExecuteOperation(string operation, Dictionary<string, object> payload);
        
        #endregion
        
        #region Virtual Methods
        
        /// <summary>
        /// ペイロードを検証します。
        /// 派生クラスでオーバーライドして追加の検証を実装できます。
        /// </summary>
        protected virtual void ValidatePayload(Dictionary<string, object> payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload), "Payload cannot be null");
            }
            
            if (!payload.ContainsKey("operation"))
            {
                throw new InvalidOperationException("'operation' parameter is required");
            }
        }
        
        /// <summary>
        /// 指定された操作がコンパイル待機を必要とするか判定します。
        /// 派生クラスでオーバーライドして個別のロジックを実装できます。
        /// </summary>
        protected virtual bool RequiresCompilationWait(string operation)
        {
            // デフォルトでは読み取り専用操作はコンパイル待機不要
            var readOnlyOperations = new[] { "inspect", "list", "find", "findMultiple", "inspectMultiple" };
            return !readOnlyOperations.Contains(operation);
        }
        
        /// <summary>
        /// コンパイルが必要な場合に待機します。
        /// 派生クラスでオーバーライドして個別のロジックを実装できます。
        /// </summary>
        protected virtual Dictionary<string, object> WaitForCompilationIfNeeded(string operation)
        {
            // 実装は McpCommandProcessor.Settings.cs の EnsureNoCompilationInProgress を使用
            // この基底クラスでは null を返す（実装例として）
            return null;
        }
        
        /// <summary>
        /// エラーレスポンスを作成します。
        /// </summary>
        protected virtual object CreateErrorResponse(Exception ex)
        {
            return new Dictionary<string, object>
            {
                ["success"] = false,
                ["error"] = ex.Message,
                ["errorType"] = ex.GetType().Name,
                ["category"] = Category
            };
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// ペイロードから操作名を取得します。
        /// </summary>
        protected string GetOperation(Dictionary<string, object> payload)
        {
            var operation = payload["operation"]?.ToString();
            if (string.IsNullOrEmpty(operation))
            {
                throw new InvalidOperationException("'operation' parameter cannot be null or empty");
            }
            return operation;
        }
        
        /// <summary>
        /// ペイロードから文字列値を取得します。
        /// </summary>
        protected string GetString(Dictionary<string, object> payload, string key, string defaultValue = null)
        {
            if (payload == null || !payload.ContainsKey(key))
            {
                return defaultValue;
            }
            return payload[key]?.ToString();
        }
        
        /// <summary>
        /// ペイロードからブール値を取得します。
        /// </summary>
        protected bool GetBool(Dictionary<string, object> payload, string key, bool defaultValue = false)
        {
            if (payload == null || !payload.ContainsKey(key))
            {
                return defaultValue;
            }
            
            var value = payload[key];
            if (value is bool boolValue)
            {
                return boolValue;
            }
            
            if (bool.TryParse(value?.ToString(), out var parsedValue))
            {
                return parsedValue;
            }
            
            return defaultValue;
        }
        
        /// <summary>
        /// ペイロードから整数値を取得します。
        /// </summary>
        protected int GetInt(Dictionary<string, object> payload, string key, int defaultValue = 0)
        {
            if (payload == null || !payload.ContainsKey(key))
            {
                return defaultValue;
            }
            
            var value = payload[key];
            if (value is int intValue)
            {
                return intValue;
            }
            
            if (value is long longValue)
            {
                return (int)longValue;
            }
            
            if (int.TryParse(value?.ToString(), out var parsedValue))
            {
                return parsedValue;
            }
            
            return defaultValue;
        }
        
        /// <summary>
        /// 成功レスポンスを作成します。
        /// </summary>
        protected Dictionary<string, object> CreateSuccessResponse(params (string key, object value)[] additionalData)
        {
            var response = new Dictionary<string, object>
            {
                ["success"] = true
            };
            
            foreach (var (key, value) in additionalData)
            {
                response[key] = value;
            }
            
            return response;
        }
        
        #endregion
    }
}

