using System;
using System.Collections;
using System.Collections.Generic;
using MCP.Editor.Base.JsonConverters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Base
{
    /// <summary>
    /// Newtonsoft.Jsonを使用して値変換を行うマネージャー。
    /// Unity型、ユーザー定義型、プリミティブ型の変換をサポートします。
    /// </summary>
    public class ValueConverterManager
    {
        private static ValueConverterManager _instance;
        private readonly JsonSerializer _serializer;

        /// <summary>
        /// シングルトンインスタンスを取得します。
        /// </summary>
        public static ValueConverterManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ValueConverterManager();
                }
                return _instance;
            }
        }

        private ValueConverterManager()
        {
            _serializer = UnityJsonSettings.Serializer;
        }

        /// <summary>
        /// 値を指定された型に変換します。
        /// </summary>
        /// <param name="value">変換元の値</param>
        /// <param name="targetType">変換先の型</param>
        /// <returns>変換後の値</returns>
        public object Convert(object value, Type targetType)
        {
            if (value == null)
            {
                return GetDefaultValue(targetType);
            }

            if (targetType.IsInstanceOfType(value))
            {
                return value;
            }

            try
            {
                // UnityEngine.Object参照の特殊処理
                if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
                {
                    return ConvertToUnityObject(value, targetType);
                }

                // JTokenからの変換
                if (value is JToken jToken)
                {
                    return jToken.ToObject(targetType, _serializer);
                }

                // Dictionary<string, object>からの変換
                if (value is Dictionary<string, object> dict)
                {
                    return ConvertFromDictionary(dict, targetType);
                }

                // IList（配列/List）の変換
                if (value is IList list && (targetType.IsArray || IsGenericList(targetType)))
                {
                    return ConvertList(list, targetType);
                }

                // プリミティブ型の変換
                if (targetType.IsPrimitive || targetType == typeof(string) || targetType == typeof(decimal))
                {
                    return ConvertPrimitive(value, targetType);
                }

                // Enumの変換
                if (targetType.IsEnum)
                {
                    return ConvertToEnum(value, targetType);
                }

                // その他: JSON経由で変換
                var json = JsonConvert.SerializeObject(value, UnityJsonSettings.Settings);
                return JsonConvert.DeserializeObject(json, targetType, UnityJsonSettings.Settings);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ValueConverterManager: Failed to convert {value?.GetType().Name} to {targetType.Name}: {ex.Message}");
                return GetDefaultValue(targetType);
            }
        }

        /// <summary>
        /// 値を指定された型に変換を試みます。
        /// </summary>
        public bool TryConvert(object value, Type targetType, out object result)
        {
            try
            {
                result = Convert(value, targetType);
                return result != null || value == null;
            }
            catch
            {
                result = GetDefaultValue(targetType);
                return false;
            }
        }

        /// <summary>
        /// オブジェクトをシリアライズ可能な形式に変換します。
        /// </summary>
        public object Serialize(object value)
        {
            if (value == null)
                return null;

            try
            {
                // JTokenに変換してからDictionary/Listに変換
                var jToken = JToken.FromObject(value, _serializer);
                return ConvertJTokenToObject(jToken);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ValueConverterManager: Failed to serialize {value.GetType().Name}: {ex.Message}");
                return value?.ToString();
            }
        }

        #region Private Methods

        private object ConvertFromDictionary(Dictionary<string, object> dict, Type targetType)
        {
            // DictionaryをJObjectに変換
            var jObject = new JObject();
            foreach (var kvp in dict)
            {
                jObject[kvp.Key] = kvp.Value != null ? JToken.FromObject(kvp.Value, _serializer) : JValue.CreateNull();
            }
            return jObject.ToObject(targetType, _serializer);
        }

        private object ConvertList(IList sourceList, Type targetType)
        {
            Type elementType = GetElementType(targetType);
            if (elementType == null)
            {
                return CreateEmptyCollection(targetType);
            }

            if (targetType.IsArray)
            {
                var array = Array.CreateInstance(elementType, sourceList.Count);
                for (int i = 0; i < sourceList.Count; i++)
                {
                    array.SetValue(Convert(sourceList[i], elementType), i);
                }
                return array;
            }

            if (IsGenericList(targetType))
            {
                var list = (IList)Activator.CreateInstance(targetType);
                foreach (var item in sourceList)
                {
                    list.Add(Convert(item, elementType));
                }
                return list;
            }

            return CreateEmptyCollection(targetType);
        }

        private object ConvertPrimitive(object value, Type targetType)
        {
            if (value is JValue jValue)
            {
                value = jValue.Value;
            }

            if (targetType == typeof(string))
            {
                return value?.ToString();
            }

            return System.Convert.ChangeType(value, targetType);
        }

        private object ConvertToEnum(object value, Type targetType)
        {
            if (value is JValue jValue)
            {
                value = jValue.Value;
            }

            if (value is string enumStr)
            {
                return Enum.Parse(targetType, enumStr, ignoreCase: true);
            }

            if (value is int || value is long || value is short || value is byte)
            {
                return Enum.ToObject(targetType, System.Convert.ToInt32(value));
            }

            return Enum.Parse(targetType, value.ToString(), ignoreCase: true);
        }

        private object ConvertToUnityObject(object value, Type targetType)
        {
            string assetPath = null;
            string guid = null;

            if (value is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue("assetPath", out var pathObj))
                    assetPath = pathObj?.ToString();
                if (dict.TryGetValue("guid", out var guidObj))
                    guid = guidObj?.ToString();
            }
            else if (value is JObject jObj)
            {
                assetPath = jObj.Value<string>("assetPath");
                guid = jObj.Value<string>("guid");
            }
            else if (value is string str)
            {
                assetPath = str;
            }

            if (!string.IsNullOrEmpty(guid))
            {
                assetPath = AssetDatabase.GUIDToAssetPath(guid);
            }

            if (!string.IsNullOrEmpty(assetPath))
            {
                return AssetDatabase.LoadAssetAtPath(assetPath, targetType);
            }

            return null;
        }

        private object ConvertJTokenToObject(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in ((JObject)token).Properties())
                    {
                        dict[prop.Name] = ConvertJTokenToObject(prop.Value);
                    }
                    return dict;

                case JTokenType.Array:
                    var list = new List<object>();
                    foreach (var item in (JArray)token)
                    {
                        list.Add(ConvertJTokenToObject(item));
                    }
                    return list;

                case JTokenType.Integer:
                    return token.Value<long>();

                case JTokenType.Float:
                    return token.Value<double>();

                case JTokenType.String:
                    return token.Value<string>();

                case JTokenType.Boolean:
                    return token.Value<bool>();

                case JTokenType.Null:
                    return null;

                default:
                    return token.ToString();
            }
        }

        private Type GetElementType(Type collectionType)
        {
            if (collectionType.IsArray)
            {
                return collectionType.GetElementType();
            }

            if (collectionType.IsGenericType)
            {
                var genericArgs = collectionType.GetGenericArguments();
                if (genericArgs.Length > 0)
                {
                    return genericArgs[0];
                }
            }

            return null;
        }

        private bool IsGenericList(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }

        private object CreateEmptyCollection(Type targetType)
        {
            if (targetType.IsArray)
            {
                return Array.CreateInstance(targetType.GetElementType(), 0);
            }

            if (IsGenericList(targetType))
            {
                return Activator.CreateInstance(targetType);
            }

            return null;
        }

        private object GetDefaultValue(Type type)
        {
            if (type == null || !type.IsValueType)
                return null;

            return Activator.CreateInstance(type);
        }

        #endregion

        /// <summary>
        /// テスト用: インスタンスをリセットします。
        /// </summary>
        internal static void ResetInstance()
        {
            _instance = null;
            UnityJsonSettings.Reset();
        }
    }
}
