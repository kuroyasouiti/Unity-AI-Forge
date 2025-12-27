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
            var (assetPath, sceneObjectPath) = ExtractPaths(value);

            // Try to load from asset path first
            if (!string.IsNullOrEmpty(assetPath))
            {
                var asset = LoadAssetAtPath(assetPath, targetType);
                if (asset != null)
                    return asset;

                // If asset not found and path doesn't look like an asset path,
                // treat it as a scene object path (for $ref format with scene objects)
                if (!assetPath.StartsWith("Assets/") && !assetPath.StartsWith("Packages/") && !assetPath.EndsWith(".asset") && !assetPath.EndsWith(".prefab"))
                {
                    sceneObjectPath = assetPath;
                }
            }

            // Try to find scene object
            if (!string.IsNullOrEmpty(sceneObjectPath))
            {
                return FindSceneObject(sceneObjectPath, targetType);
            }

            return null;
        }

        /// <summary>
        /// Extracts asset path and scene object path from value.
        /// Supported formats:
        /// - Asset reference: {"$ref": "Assets/Prefabs/Player.prefab"}
        /// - Scene object (string): "Player" or "Canvas/Panel/Button"
        /// - Scene object ($ref): {"$ref": "Canvas/Panel/Button"} (auto-detected if not an asset path)
        /// </summary>
        private (string assetPath, string sceneObjectPath) ExtractPaths(object value)
        {
            // String value: auto-detect based on "Assets/" prefix
            if (value is string str)
            {
                if (str.StartsWith("Assets/") || str.StartsWith("Packages/"))
                {
                    return (str, null); // Asset path
                }
                return (null, str); // Scene object path
            }

            string refPath = null;

            if (value is Dictionary<string, object> dict)
            {
                refPath = GetStringValue(dict, "$ref");
            }
            else if (value is JObject jObj)
            {
                refPath = jObj.Value<string>("$ref");
            }

            if (!string.IsNullOrEmpty(refPath))
            {
                // Check if it looks like an asset path
                if (refPath.StartsWith("Assets/") || refPath.StartsWith("Packages/"))
                {
                    return (refPath, null); // Definitely an asset path
                }
                // Could be either - return as assetPath first, ConvertToUnityObject will fallback to scene
                return (refPath, null);
            }

            return (null, null);
        }

        private string GetStringValue(Dictionary<string, object> dict, string key)
        {
            return dict.TryGetValue(key, out var val) ? val?.ToString() : null;
        }

        private UnityEngine.Object LoadAssetAtPath(string path, Type targetType)
        {
            var asset = AssetDatabase.LoadAssetAtPath(path, targetType);
            if (asset != null)
                return asset;

            var mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
            if (mainAsset != null && targetType.IsInstanceOfType(mainAsset))
                return mainAsset;

            Debug.LogWarning($"ValueConverterManager: Asset not found at path '{path}' for type {targetType.Name}");
            return null;
        }

        /// <summary>
        /// Finds a scene object by path, including inactive objects.
        /// Supports paths like "Player", "Canvas/Panel/Button", "/RootObject/Child".
        /// </summary>
        private UnityEngine.Object FindSceneObject(string path, Type targetType)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            // First try GameObject.Find (only finds active objects)
            var go = GameObject.Find(path);

            // If not found, search including inactive objects
            if (go == null)
            {
                go = FindGameObjectIncludingInactive(path);
            }

            if (go == null)
                return null;

            if (typeof(GameObject).IsAssignableFrom(targetType))
                return go;
            if (typeof(Component).IsAssignableFrom(targetType))
                return go.GetComponent(targetType);

            return null;
        }

        /// <summary>
        /// Finds a GameObject by path, including inactive objects.
        /// </summary>
        private GameObject FindGameObjectIncludingInactive(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            // Remove leading slash if present
            var searchPath = path.TrimStart('/');
            var parts = searchPath.Split('/');

            if (parts.Length == 0)
                return null;

            // Get all root GameObjects in all loaded scenes
            var rootObjects = new List<GameObject>();
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    rootObjects.AddRange(scene.GetRootGameObjects());
                }
            }

            // Find the root object (first part of path)
            GameObject current = null;
            foreach (var root in rootObjects)
            {
                if (root.name == parts[0])
                {
                    current = root;
                    break;
                }
            }

            if (current == null)
                return null;

            // Navigate through the path
            for (int i = 1; i < parts.Length; i++)
            {
                var child = FindChildByName(current.transform, parts[i]);
                if (child == null)
                    return null;
                current = child.gameObject;
            }

            return current;
        }

        /// <summary>
        /// Finds a direct child by name (works with inactive objects).
        /// </summary>
        private Transform FindChildByName(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name)
                    return child;
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
