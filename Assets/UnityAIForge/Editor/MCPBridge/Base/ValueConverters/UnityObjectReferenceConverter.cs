using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Interfaces;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MCP.Editor.Base.ValueConverters
{
    /// <summary>
    /// Unity Object参照（GameObject, Component, Asset等）の変換を担当するコンバーター。
    /// </summary>
    public class UnityObjectReferenceConverter : IValueConverter
    {
        public int Priority => 300;

        public bool CanConvert(Type targetType)
        {
            return typeof(UnityEngine.Object).IsAssignableFrom(targetType);
        }

        public object Convert(object value, Type targetType)
        {
            if (value == null)
                return null;

            if (targetType.IsInstanceOfType(value))
                return value;

            // Dictionary形式の参照解決
            if (value is Dictionary<string, object> refDict)
            {
                var path = ExtractPathFromDictionary(refDict);
                if (!string.IsNullOrEmpty(path))
                {
                    return ResolveUnityObjectFromPath(path, targetType);
                }

                Debug.LogWarning(
                    $"Cannot resolve Unity Object from dictionary. " +
                    $"Expected format: {{ \"$ref\": \"path\" }} or {{ \"_gameObjectPath\": \"path\" }}. " +
                    $"Got: {string.Join(", ", refDict.Keys)}");
                return null;
            }

            // 文字列パスからの参照解決
            if (value is string stringValue)
            {
                return ResolveUnityObjectFromPath(stringValue, targetType);
            }

            Debug.LogWarning($"Cannot convert {value.GetType().Name} to {targetType.Name}");
            return null;
        }

        /// <summary>
        /// Dictionaryからパスを抽出します。
        /// </summary>
        private string ExtractPathFromDictionary(Dictionary<string, object> dict)
        {
            // Format 1: { "$type": "reference", "$path": "..." }
            if (dict.TryGetValue("$type", out var typeValue) &&
                typeValue?.ToString() == "reference" &&
                dict.TryGetValue("$path", out var pathValue))
            {
                return pathValue?.ToString();
            }

            // Format 2: { "$ref": "..." }
            if (dict.TryGetValue("$ref", out var refValue))
            {
                return refValue?.ToString();
            }

            // Format 3: { "_gameObjectPath": "..." }
            if (dict.TryGetValue("_gameObjectPath", out var goPathValue))
            {
                return goPathValue?.ToString();
            }

            // Try common path keys
            string[] pathKeys = { "path", "gameObjectPath", "objectPath", "target", "reference" };
            foreach (var key in pathKeys)
            {
                if (dict.TryGetValue(key, out var val) && val is string pathStr)
                {
                    return pathStr;
                }
            }

            // If dictionary has only one string value, try that as path
            if (dict.Count == 1)
            {
                var singleValue = dict.Values.First();
                if (singleValue is string singlePathStr)
                {
                    return singlePathStr;
                }
            }

            return null;
        }

        /// <summary>
        /// パスからUnity Objectを解決します。
        /// </summary>
        private UnityEngine.Object ResolveUnityObjectFromPath(string path, Type targetType)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            // アセットパスの場合
            if (path.StartsWith("Assets/") && path.Contains("."))
            {
                return AssetDatabase.LoadAssetAtPath(path, targetType);
            }

            // GameObjectをシーン階層から検索
            GameObject targetObject = GameObject.Find(path);

            if (targetObject == null)
            {
                var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
                foreach (var root in rootObjects)
                {
                    targetObject = FindChildByPath(root.transform, path);
                    if (targetObject != null)
                        break;
                }
            }

            if (targetObject == null)
            {
                Debug.LogWarning($"GameObject not found at path: {path}");
                return null;
            }

            // GameObjectを返す場合
            if (targetType == typeof(GameObject))
            {
                return targetObject;
            }

            // Transformを返す場合
            if (targetType == typeof(Transform))
            {
                return targetObject.transform;
            }

            if (targetType == typeof(RectTransform))
            {
                return targetObject.GetComponent<RectTransform>() ?? (UnityEngine.Object)targetObject.transform;
            }

            // Componentを返す場合
            if (typeof(Component).IsAssignableFrom(targetType))
            {
                var component = targetObject.GetComponent(targetType);
                if (component == null)
                {
                    Debug.LogWarning($"Component {targetType.Name} not found on GameObject at path: {path}");
                }
                return component;
            }

            return null;
        }

        private GameObject FindChildByPath(Transform parent, string path)
        {
            if (parent == null || string.IsNullOrEmpty(path))
                return null;

            if (parent.name == path)
                return parent.gameObject;

            if (path.StartsWith(parent.name + "/"))
            {
                string remainingPath = path.Substring(parent.name.Length + 1);
                return FindChildRecursive(parent, remainingPath);
            }

            foreach (Transform child in parent)
            {
                var result = FindChildByPath(child, path);
                if (result != null)
                    return result;
            }

            return null;
        }

        private GameObject FindChildRecursive(Transform parent, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return parent.gameObject;

            int slashIndex = relativePath.IndexOf('/');
            string childName = slashIndex >= 0 ? relativePath.Substring(0, slashIndex) : relativePath;
            string remaining = slashIndex >= 0 ? relativePath.Substring(slashIndex + 1) : "";

            Transform child = parent.Find(childName);
            if (child == null)
                return null;

            if (string.IsNullOrEmpty(remaining))
                return child.gameObject;

            return FindChildRecursive(child, remaining);
        }
    }
}
