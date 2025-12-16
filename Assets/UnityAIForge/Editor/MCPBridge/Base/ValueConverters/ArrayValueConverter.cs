using System;
using System.Collections;
using System.Collections.Generic;
using MCP.Editor.Interfaces;
using UnityEngine;

namespace MCP.Editor.Base.ValueConverters
{
    /// <summary>
    /// 配列およびList型の変換を担当するコンバーター。
    /// 各要素はValueConverterManagerを通じて適切な型に変換されます。
    /// </summary>
    public class ArrayValueConverter : IValueConverter
    {
        public int Priority => 250;

        public bool CanConvert(Type targetType)
        {
            if (targetType == null)
                return false;

            // 配列型
            if (targetType.IsArray)
                return true;

            // List<T>型
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
                return true;

            return false;
        }

        public object Convert(object value, Type targetType)
        {
            if (value == null)
            {
                return CreateEmptyCollection(targetType);
            }

            // 既に正しい型の場合はそのまま返す
            if (targetType.IsInstanceOfType(value))
                return value;

            // 入力値をIListとして扱う
            IList sourceList = value as IList;
            if (sourceList == null)
            {
                // 単一の値を1要素の配列として扱う
                sourceList = new object[] { value };
            }

            // 要素の型を取得
            Type elementType = GetElementType(targetType);
            if (elementType == null)
            {
                Debug.LogWarning($"Cannot determine element type for {targetType.Name}");
                return CreateEmptyCollection(targetType);
            }

            // 配列の場合
            if (targetType.IsArray)
            {
                return ConvertToArray(sourceList, elementType);
            }

            // List<T>の場合
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return ConvertToList(sourceList, elementType, targetType);
            }

            return CreateEmptyCollection(targetType);
        }

        /// <summary>
        /// 配列に変換します。
        /// </summary>
        private Array ConvertToArray(IList sourceList, Type elementType)
        {
            Array result = Array.CreateInstance(elementType, sourceList.Count);

            for (int i = 0; i < sourceList.Count; i++)
            {
                var convertedElement = ConvertElement(sourceList[i], elementType);
                result.SetValue(convertedElement, i);
            }

            return result;
        }

        /// <summary>
        /// List<T>に変換します。
        /// </summary>
        private IList ConvertToList(IList sourceList, Type elementType, Type listType)
        {
            IList result = (IList)Activator.CreateInstance(listType);

            for (int i = 0; i < sourceList.Count; i++)
            {
                var convertedElement = ConvertElement(sourceList[i], elementType);
                result.Add(convertedElement);
            }

            return result;
        }

        /// <summary>
        /// 単一の要素を変換します。
        /// </summary>
        private object ConvertElement(object element, Type elementType)
        {
            if (element == null)
            {
                return GetDefaultValue(elementType);
            }

            if (elementType.IsInstanceOfType(element))
            {
                return element;
            }

            // ValueConverterManagerを使用して変換
            // 注意: 循環参照を避けるため、配列以外のコンバーターのみを使用
            return ValueConverterManager.Instance.Convert(element, elementType);
        }

        /// <summary>
        /// コレクションの要素型を取得します。
        /// </summary>
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

        /// <summary>
        /// 空のコレクションを作成します。
        /// </summary>
        private object CreateEmptyCollection(Type targetType)
        {
            if (targetType.IsArray)
            {
                Type elementType = targetType.GetElementType();
                return Array.CreateInstance(elementType, 0);
            }

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return Activator.CreateInstance(targetType);
            }

            return null;
        }

        /// <summary>
        /// 型のデフォルト値を取得します。
        /// </summary>
        private object GetDefaultValue(Type type)
        {
            if (type == null || !type.IsValueType)
                return null;

            return Activator.CreateInstance(type);
        }
    }
}
