using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Interfaces;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Base
{
    /// <summary>
    /// コマンドハンドラーの基底クラス。
    /// 共通のロジックを実装し、派生クラスで個別の操作を実装します。
    /// </summary>
    public abstract class BaseCommandHandler : ICommandHandler
    {
        #region Response Key Constants

        /// <summary>Response key: success status.</summary>
        protected const string KeySuccess = "success";
        /// <summary>Response key: error message.</summary>
        protected const string KeyError = "error";
        /// <summary>Response key: category name.</summary>
        protected const string KeyCategory = "category";
        /// <summary>Response key: general message.</summary>
        protected const string KeyMessage = "message";
        /// <summary>Response key: GameObject path.</summary>
        protected const string KeyGameObjectPath = "gameObjectPath";
        /// <summary>Response key: asset path.</summary>
        protected const string KeyAssetPath = "assetPath";
        /// <summary>Response key: operation name.</summary>
        protected const string KeyOperation = "operation";

        #endregion

        #region Protected Fields
        
        /// <summary>
        /// ペイロードバリデーター。
        /// </summary>
        protected IPayloadValidator Validator { get; private set; }
        
        /// <summary>
        /// GameObjectリゾルバー。
        /// </summary>
        protected IGameObjectResolver GameObjectResolver { get; private set; }
        
        /// <summary>
        /// Assetリゾルバー。
        /// </summary>
        protected IAssetResolver AssetResolver { get; private set; }
        
        /// <summary>
        /// Typeリゾルバー。
        /// </summary>
        protected ITypeResolver TypeResolver { get; private set; }
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// デフォルトのリゾルバーを使用してコマンドハンドラーを初期化します。
        /// </summary>
        protected BaseCommandHandler()
        {
            Validator = new StandardPayloadValidator();
            GameObjectResolver = new GameObjectResolver();
            AssetResolver = new AssetResolver();
            TypeResolver = new TypeResolver();
        }
        
        /// <summary>
        /// カスタムリゾルバーを使用してコマンドハンドラーを初期化します。
        /// </summary>
        protected BaseCommandHandler(
            IPayloadValidator validator,
            IGameObjectResolver gameObjectResolver,
            IAssetResolver assetResolver,
            ITypeResolver typeResolver)
        {
            Validator = validator ?? new StandardPayloadValidator();
            GameObjectResolver = gameObjectResolver ?? new GameObjectResolver();
            AssetResolver = assetResolver ?? new AssetResolver();
            TypeResolver = typeResolver ?? new TypeResolver();
        }
        
        #endregion
        
        #region ICommandHandler Implementation
        
        /// <inheritdoc/>
        public abstract IEnumerable<string> SupportedOperations { get; }
        
        /// <inheritdoc/>
        public abstract string Category { get; }
        
        /// <inheritdoc/>
        public virtual string Version => "1.0.0";
        
        /// <inheritdoc/>
        public virtual object Execute(Dictionary<string, object> payload)
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
                
                // 4. 操作の実行
                var result = ExecuteOperation(operation, payload);
                
                // 5. 操作後のコンパイル待機（必要な場合）
                Dictionary<string, object> compilationWaitInfo = null;
                if (RequiresCompilationWait(operation))
                {
                    compilationWaitInfo = WaitForCompilationAfterOperation(operation);
                }
                
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
            
            // バリデーターが設定されている場合は使用
            if (Validator != null)
            {
                var operation = GetOperation(payload);
                var validationResult = Validator.Validate(payload, operation);
                
                if (!validationResult.IsValid)
                {
                    var errors = string.Join("; ", validationResult.Errors);
                    throw new InvalidOperationException($"Payload validation failed: {errors}");
                }
                
                // 正規化されたペイロードに置き換え
                if (validationResult.NormalizedPayload != null)
                {
                    foreach (var kvp in validationResult.NormalizedPayload)
                    {
                        payload[kvp.Key] = kvp.Value;
                    }
                }
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
        /// 操作実行後にコンパイルが開始された場合に待機します。
        /// ブリッジ再ロード時に待機を解除します。
        /// 派生クラスでオーバーライドして個別のロジックを実装できます。
        /// 
        /// NOTE: デフォルトでは無効化されています。
        /// Thread.Sleep() を使った同期待機は Unity Editor のメインスレッドをブロックし、
        /// UI のフリーズを引き起こすため、推奨されません。
        /// クライアント側で非同期的にコンパイル完了を検出することを推奨します。
        /// </summary>
        protected virtual Dictionary<string, object> WaitForCompilationAfterOperation(string operation)
        {
            // Compilation wait is disabled by default to prevent Unity Editor freezing.
            // If you need to wait for compilation, override this method in derived classes
            // and implement an async/callback-based approach instead of synchronous blocking.
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
        /// ペイロードから浮動小数点値を取得します。
        /// </summary>
        protected float GetFloat(Dictionary<string, object> payload, string key, float defaultValue = 0f)
        {
            if (payload == null || !payload.ContainsKey(key))
            {
                return defaultValue;
            }
            
            var value = payload[key];
            if (value is float floatValue)
            {
                return floatValue;
            }
            
            if (value is double doubleValue)
            {
                return (float)doubleValue;
            }
            
            if (value is int intValue)
            {
                return (float)intValue;
            }
            
            if (float.TryParse(value?.ToString(), out var parsedValue))
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
                [KeySuccess] = true
            };

            foreach (var (key, value) in additionalData)
            {
                response[key] = value;
            }

            return response;
        }

        /// <summary>
        /// Extracts a nested dictionary from payload.
        /// Returns null if key doesn't exist or value is not a dictionary.
        /// </summary>
        /// <param name="payload">The source payload</param>
        /// <param name="key">The key to look up</param>
        /// <returns>The nested dictionary or null</returns>
        protected Dictionary<string, object> GetDictFromPayload(Dictionary<string, object> payload, string key)
        {
            if (payload != null && payload.TryGetValue(key, out var value) && value is Dictionary<string, object> dict)
            {
                return dict;
            }
            return null;
        }

        /// <summary>
        /// Extracts a list from payload.
        /// Returns null if key doesn't exist or value is not a list.
        /// </summary>
        /// <param name="payload">The source payload</param>
        /// <param name="key">The key to look up</param>
        /// <returns>The list or null</returns>
        protected List<object> GetListFromPayload(Dictionary<string, object> payload, string key)
        {
            if (payload != null && payload.TryGetValue(key, out var value) && value is List<object> list)
            {
                return list;
            }
            return null;
        }

        /// <summary>
        /// Creates a failure response with a message and optional additional data.
        /// Use this for expected error conditions (e.g., "asset not found").
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        /// <param name="additionalData">Optional additional key-value pairs</param>
        /// <returns>A dictionary with success=false and error details</returns>
        protected Dictionary<string, object> CreateFailureResponse(string errorMessage, params (string key, object value)[] additionalData)
        {
            var response = new Dictionary<string, object>
            {
                [KeySuccess] = false,
                [KeyError] = errorMessage,
                [KeyCategory] = Category
            };

            foreach (var (key, value) in additionalData)
            {
                response[key] = value;
            }

            return response;
        }

        #endregion
        
        #region Resource Resolution Helper Methods
        
        /// <summary>
        /// GameObjectを階層パスから解決します。
        /// </summary>
        protected GameObject ResolveGameObject(string path)
        {
            if (GameObjectResolver == null)
            {
                throw new InvalidOperationException("GameObjectResolver is not initialized");
            }
            return GameObjectResolver.Resolve(path);
        }
        
        /// <summary>
        /// GameObjectを階層パスから解決します（失敗時はnull）。
        /// </summary>
        protected GameObject TryResolveGameObject(string path)
        {
            if (GameObjectResolver == null || string.IsNullOrEmpty(path))
            {
                return null;
            }
            return GameObjectResolver.TryResolve(path);
        }
        
        /// <summary>
        /// パターンマッチでGameObjectを検索します。
        /// </summary>
        protected IEnumerable<GameObject> FindGameObjectsByPattern(string pattern, bool useRegex = false, int maxResults = 1000)
        {
            if (GameObjectResolver == null)
            {
                return Enumerable.Empty<GameObject>();
            }
            return GameObjectResolver.FindByPattern(pattern, useRegex, maxResults);
        }
        
        /// <summary>
        /// Assetをパスまたは GUID から解決します。
        /// </summary>
        protected UnityEngine.Object ResolveAsset(string identifier)
        {
            if (AssetResolver == null)
            {
                throw new InvalidOperationException("AssetResolver is not initialized");
            }
            return AssetResolver.Resolve(identifier);
        }
        
        /// <summary>
        /// Assetをパスまたは GUID から解決します（失敗時はnull）。
        /// </summary>
        protected UnityEngine.Object TryResolveAsset(string identifier)
        {
            if (AssetResolver == null || string.IsNullOrEmpty(identifier))
            {
                return null;
            }
            return AssetResolver.TryResolve(identifier);
        }
        
        /// <summary>
        /// アセットパスを検証します。
        /// </summary>
        protected bool ValidateAssetPath(string path)
        {
            if (AssetResolver == null)
            {
                return false;
            }
            return AssetResolver.ValidatePath(path);
        }
        
        /// <summary>
        /// 型名から Type を解決します。
        /// </summary>
        protected Type ResolveType(string typeName)
        {
            if (TypeResolver == null)
            {
                throw new InvalidOperationException("TypeResolver is not initialized");
            }
            return TypeResolver.Resolve(typeName);
        }
        
        /// <summary>
        /// 型名から Type を解決します（失敗時はnull）。
        /// </summary>
        protected Type TryResolveType(string typeName)
        {
            if (TypeResolver == null || string.IsNullOrEmpty(typeName))
            {
                return null;
            }
            return TypeResolver.TryResolve(typeName);
        }
        
        /// <summary>
        /// 指定された基底型を継承する全ての型を検索します。
        /// </summary>
        protected IEnumerable<Type> FindDerivedTypes(Type baseType)
        {
            if (TypeResolver == null || baseType == null)
            {
                return Enumerable.Empty<Type>();
            }
            return TypeResolver.FindDerivedTypes(baseType);
        }
        
        /// <summary>
        /// ペイロードからGameObjectを解決します。
        /// gameObjectPath または gameObjectGlobalObjectId を使用します。
        /// </summary>
        protected GameObject ResolveGameObjectFromPayload(Dictionary<string, object> payload)
        {
            // GlobalObjectId が指定されている場合（優先）
            var globalId = GetString(payload, "gameObjectGlobalObjectId");
            if (!string.IsNullOrEmpty(globalId))
            {
                if (GlobalObjectId.TryParse(globalId, out var parsed))
                {
                    var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(parsed) as GameObject;
                    if (obj != null)
                    {
                        return obj;
                    }
                }
                else
                {
                    Debug.LogWarning($"Invalid GlobalObjectId format: {globalId}");
                }
            }
            
            // 階層パスから解決
            var path = GetString(payload, "gameObjectPath");
            if (!string.IsNullOrEmpty(path))
            {
                return ResolveGameObject(path);
            }
            
            throw new InvalidOperationException("Either 'gameObjectPath' or 'gameObjectGlobalObjectId' is required");
        }
        
        /// <summary>
        /// ペイロードからAssetを解決します。
        /// assetPath または assetGuid を使用します。
        /// </summary>
        protected UnityEngine.Object ResolveAssetFromPayload(Dictionary<string, object> payload)
        {
            // GUID が指定されている場合（優先）
            var guid = GetString(payload, "assetGuid");
            if (!string.IsNullOrEmpty(guid))
            {
                return ResolveAsset(guid);
            }
            
            // パスから解決
            var path = GetString(payload, "assetPath");
            if (!string.IsNullOrEmpty(path))
            {
                return ResolveAsset(path);
            }
            
            throw new InvalidOperationException("Either 'assetPath' or 'assetGuid' is required");
        }
        
        #endregion
    }
}

