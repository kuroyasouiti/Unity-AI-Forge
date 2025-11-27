using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Interfaces;

namespace MCP.Editor.Base
{
    /// <summary>
    /// 標準的なペイロードバリデーター実装。
    /// 共通的なバリデーションルールを提供します。
    /// </summary>
    public class StandardPayloadValidator : IPayloadValidator
    {
        private readonly Dictionary<string, OperationSchema> _operationSchemas;
        
        public StandardPayloadValidator()
        {
            _operationSchemas = new Dictionary<string, OperationSchema>();
        }
        
        /// <summary>
        /// 操作のスキーマを登録します。
        /// </summary>
        public void RegisterOperation(string operation, OperationSchema schema)
        {
            if (string.IsNullOrEmpty(operation))
            {
                throw new ArgumentNullException(nameof(operation));
            }
            
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }
            
            _operationSchemas[operation] = schema;
        }
        
        /// <inheritdoc/>
        public ValidationResult Validate(Dictionary<string, object> payload, string operation)
        {
            var result = new ValidationResult
            {
                NormalizedPayload = new Dictionary<string, object>(payload)
            };
            
            // 1. ペイロードがnullでないことを確認
            if (payload == null)
            {
                result.AddError("Payload cannot be null");
                return result;
            }
            
            // 2. 操作がスキーマに登録されているか確認
            if (!_operationSchemas.TryGetValue(operation, out var schema))
            {
                // スキーマが登録されていない場合は基本的なバリデーションのみ
                return ValidationResult.Success(result.NormalizedPayload);
            }
            
            // 3. 必須パラメータの存在チェック
            foreach (var requiredParam in schema.RequiredParameters)
            {
                if (!payload.ContainsKey(requiredParam))
                {
                    result.AddError($"Required parameter '{requiredParam}' is missing");
                }
                else if (payload[requiredParam] == null)
                {
                    result.AddError($"Required parameter '{requiredParam}' cannot be null");
                }
            }
            
            // 4. 各パラメータの型チェックと正規化
            foreach (var kvp in schema.ParameterTypes)
            {
                var paramName = kvp.Key;
                var expectedType = kvp.Value;
                
                if (!payload.ContainsKey(paramName))
                {
                    // オプションパラメータの場合はデフォルト値を設定
                    if (schema.DefaultValues.TryGetValue(paramName, out var defaultValue))
                    {
                        result.NormalizedPayload[paramName] = defaultValue;
                    }
                    continue;
                }
                
                var value = payload[paramName];
                if (value == null)
                {
                    continue; // null値は既にチェック済み
                }
                
                // 型の検証と変換
                try
                {
                    var normalizedValue = NormalizeValue(value, expectedType, paramName);
                    result.NormalizedPayload[paramName] = normalizedValue;
                }
                catch (Exception ex)
                {
                    result.AddError($"Parameter '{paramName}' type error: {ex.Message}");
                }
            }
            
            // 5. カスタムバリデーションルール
            if (schema.CustomValidators != null)
            {
                foreach (var validator in schema.CustomValidators)
                {
                    try
                    {
                        validator(result.NormalizedPayload, result);
                    }
                    catch (Exception ex)
                    {
                        result.AddError($"Custom validation error: {ex.Message}");
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 値を指定された型に正規化します。
        /// </summary>
        private object NormalizeValue(object value, Type expectedType, string paramName)
        {
            if (value == null)
            {
                return null;
            }
            
            var actualType = value.GetType();
            
            // 既に正しい型の場合
            if (expectedType.IsAssignableFrom(actualType))
            {
                return value;
            }
            
            // 文字列への変換
            if (expectedType == typeof(string))
            {
                return value.ToString();
            }
            
            // bool への変換
            if (expectedType == typeof(bool))
            {
                if (value is bool boolValue)
                    return boolValue;
                if (bool.TryParse(value.ToString(), out var parsedBool))
                    return parsedBool;
                throw new InvalidCastException($"Cannot convert '{value}' to bool");
            }
            
            // int への変換
            if (expectedType == typeof(int))
            {
                if (value is int intValue)
                    return intValue;
                if (value is long longValue)
                    return (int)longValue;
                if (int.TryParse(value.ToString(), out var parsedInt))
                    return parsedInt;
                throw new InvalidCastException($"Cannot convert '{value}' to int");
            }
            
            // float への変換
            if (expectedType == typeof(float))
            {
                if (value is float floatValue)
                    return floatValue;
                if (value is double doubleValue)
                    return (float)doubleValue;
                if (float.TryParse(value.ToString(), out var parsedFloat))
                    return parsedFloat;
                throw new InvalidCastException($"Cannot convert '{value}' to float");
            }
            
            // Dictionary への変換
            if (expectedType == typeof(Dictionary<string, object>))
            {
                if (value is Dictionary<string, object> dict)
                    return dict;
                throw new InvalidCastException($"Cannot convert '{value}' to Dictionary<string, object>");
            }
            
            // List への変換
            if (expectedType == typeof(List<object>))
            {
                if (value is List<object> list)
                    return list;
                if (value is object[] array)
                    return new List<object>(array);
                throw new InvalidCastException($"Cannot convert '{value}' to List<object>");
            }
            
            throw new InvalidCastException($"Unsupported type conversion for parameter '{paramName}': {actualType} to {expectedType}");
        }
    }
    
    /// <summary>
    /// 操作のスキーマ定義。
    /// </summary>
    public class OperationSchema
    {
        /// <summary>
        /// 必須パラメータのリスト。
        /// </summary>
        public List<string> RequiredParameters { get; set; } = new List<string>();
        
        /// <summary>
        /// パラメータ名と期待される型のマッピング。
        /// </summary>
        public Dictionary<string, Type> ParameterTypes { get; set; } = new Dictionary<string, Type>();
        
        /// <summary>
        /// オプションパラメータのデフォルト値。
        /// </summary>
        public Dictionary<string, object> DefaultValues { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// カスタムバリデーション関数のリスト。
        /// </summary>
        public List<Action<Dictionary<string, object>, ValidationResult>> CustomValidators { get; set; }
        
        /// <summary>
        /// 操作の説明。
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// ビルダーを使用してスキーマを構築します。
        /// </summary>
        public static OperationSchemaBuilder Builder()
        {
            return new OperationSchemaBuilder();
        }
    }
    
    /// <summary>
    /// OperationSchemaのビルダークラス。
    /// </summary>
    public class OperationSchemaBuilder
    {
        private readonly OperationSchema _schema = new OperationSchema();
        
        public OperationSchemaBuilder WithDescription(string description)
        {
            _schema.Description = description;
            return this;
        }
        
        public OperationSchemaBuilder RequireParameter(string name, Type type = null)
        {
            _schema.RequiredParameters.Add(name);
            if (type != null)
            {
                _schema.ParameterTypes[name] = type;
            }
            return this;
        }
        
        public OperationSchemaBuilder OptionalParameter(string name, Type type, object defaultValue = null)
        {
            _schema.ParameterTypes[name] = type;
            if (defaultValue != null)
            {
                _schema.DefaultValues[name] = defaultValue;
            }
            return this;
        }
        
        public OperationSchemaBuilder AddCustomValidator(Action<Dictionary<string, object>, ValidationResult> validator)
        {
            if (_schema.CustomValidators == null)
            {
                _schema.CustomValidators = new List<Action<Dictionary<string, object>, ValidationResult>>();
            }
            _schema.CustomValidators.Add(validator);
            return this;
        }
        
        public OperationSchema Build()
        {
            return _schema;
        }
    }
}

