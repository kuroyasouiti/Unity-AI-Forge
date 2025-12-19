using System;
using System.Collections.Generic;
using System.Reflection;
using MCP.Editor.Interfaces;
using UnityEngine;

namespace MCP.Editor.Base.ValueConverters
{
    /// <summary>
    /// [Serializable]属性を持つユーザー定義構造体の変換を担当するコンバーター。
    /// Unity組み込み構造体（Vector3, Color等）はUnityStructValueConverterが処理するため、
    /// このコンバーターは優先度を低く設定しています。
    /// </summary>
    public class SerializableStructValueConverter : IValueConverter
    {
        // UnityStructValueConverter（200）より低い優先度に設定
        public int Priority => 150;

        // UnityのビルトインTypes（UnityStructValueConverterで処理される）
        private static readonly HashSet<Type> UnityBuiltInTypes = new HashSet<Type>
        {
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Color),
            typeof(Color32),
            typeof(Quaternion),
            typeof(Rect),
            typeof(Bounds),
            typeof(Vector2Int),
            typeof(Vector3Int),
            typeof(RectInt),
            typeof(BoundsInt)
        };

        public bool CanConvert(Type targetType)
        {
            if (targetType == null)
                return false;

            // Unity組み込み型はUnityStructValueConverterに任せる
            if (UnityBuiltInTypes.Contains(targetType))
                return false;

            // 値型（struct）かつ[Serializable]属性を持つ場合に処理
            if (targetType.IsValueType && !targetType.IsPrimitive && !targetType.IsEnum)
            {
                return targetType.IsDefined(typeof(SerializableAttribute), false);
            }

            return false;
        }

        public object Convert(object value, Type targetType)
        {
            if (value == null)
            {
                return Activator.CreateInstance(targetType);
            }

            if (targetType.IsInstanceOfType(value))
            {
                return value;
            }

            // 辞書からの変換
            if (value is Dictionary<string, object> dict)
            {
                return ConvertFromDictionary(dict, targetType);
            }

            // IEnumerableなDictionary（Newtonsoftなどから来る場合）
            if (value is IEnumerable<KeyValuePair<string, object>> kvpEnumerable)
            {
                var dictCopy = new Dictionary<string, object>();
                foreach (var kvp in kvpEnumerable)
                {
                    dictCopy[kvp.Key] = kvp.Value;
                }
                return ConvertFromDictionary(dictCopy, targetType);
            }

            throw new InvalidOperationException(
                $"Cannot convert {value.GetType().Name} to {targetType.Name}. Expected a dictionary.");
        }

        /// <summary>
        /// 辞書から構造体インスタンスを作成します。
        /// </summary>
        private object ConvertFromDictionary(Dictionary<string, object> dict, Type targetType)
        {
            // 構造体のインスタンスを作成（ボックス化）
            object instance = Activator.CreateInstance(targetType);

            // パブリックフィールドを取得
            var fields = targetType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                // 辞書に対応するキーがあるか確認
                if (dict.TryGetValue(field.Name, out var fieldValue))
                {
                    try
                    {
                        // フィールドの型に変換
                        var convertedValue = ConvertFieldValue(fieldValue, field.FieldType);
                        field.SetValue(instance, convertedValue);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to set field '{field.Name}' on {targetType.Name}: {ex.Message}");
                    }
                }
            }

            return instance;
        }

        /// <summary>
        /// フィールド値を適切な型に変換します。
        /// </summary>
        private object ConvertFieldValue(object value, Type fieldType)
        {
            if (value == null)
            {
                return fieldType.IsValueType ? Activator.CreateInstance(fieldType) : null;
            }

            if (fieldType.IsInstanceOfType(value))
            {
                return value;
            }

            // ValueConverterManagerを使用して変換
            return ValueConverterManager.Instance.Convert(value, fieldType);
        }
    }
}
