using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base.ValueConverters;
using MCP.Editor.Interfaces;
using UnityEngine;

namespace MCP.Editor.Base
{
    /// <summary>
    /// 複数のIValueConverterを管理し、適切なコンバーターを選択して値変換を行うマネージャー。
    /// Compositeパターンを使用して、複数の変換戦略を統合します。
    /// </summary>
    public class ValueConverterManager
    {
        private static ValueConverterManager _instance;
        private readonly List<IValueConverter> _converters;

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

        /// <summary>
        /// デフォルトのコンバーターを登録してマネージャーを初期化します。
        /// </summary>
        private ValueConverterManager()
        {
            _converters = new List<IValueConverter>
            {
                new UnityObjectReferenceConverter(),  // Priority: 300
                new ArrayValueConverter(),             // Priority: 250
                new UnityStructValueConverter(),       // Priority: 200
                new EnumValueConverter(),              // Priority: 150
                new PrimitiveValueConverter()          // Priority: 100
            };

            // 優先度の高い順にソート
            _converters.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        /// <summary>
        /// カスタムコンバーターを登録します。
        /// </summary>
        /// <param name="converter">追加するコンバーター</param>
        public void RegisterConverter(IValueConverter converter)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            _converters.Add(converter);
            _converters.Sort((a, b) => b.Priority.CompareTo(a.Priority));
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

            // 適切なコンバーターを探して変換
            foreach (var converter in _converters)
            {
                if (converter.CanConvert(targetType))
                {
                    try
                    {
                        return converter.Convert(value, targetType);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Converter {converter.GetType().Name} failed: {ex.Message}");
                        // 次のコンバーターを試す
                    }
                }
            }

            // フォールバック: サポートされていない型
            return HandleUnsupportedType(value, targetType);
        }

        /// <summary>
        /// 値を指定された型に変換を試みます。失敗した場合はdefault値を返します。
        /// </summary>
        /// <param name="value">変換元の値</param>
        /// <param name="targetType">変換先の型</param>
        /// <param name="result">変換結果</param>
        /// <returns>変換に成功した場合はtrue</returns>
        public bool TryConvert(object value, Type targetType, out object result)
        {
            try
            {
                result = Convert(value, targetType);
                // 変換が成功したかどうかは、結果がnullでないかで判定
                // ただし元の値がnullの場合はnullが正当な結果
                if (value == null)
                {
                    return true;
                }
                // nullが返された場合は変換失敗（値型の場合も含む）
                // Convert()がnullを返すのは変換に失敗した場合のみ
                if (result == null)
                {
                    return false;
                }
                return true;
            }
            catch
            {
                result = GetDefaultValue(targetType);
                return false;
            }
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

        /// <summary>
        /// サポートされていない型の変換を処理します。
        /// </summary>
        private object HandleUnsupportedType(object value, Type targetType)
        {
            // Unity固有の構造体は変換をスキップ
            if (targetType.IsValueType && !targetType.IsPrimitive && !targetType.IsEnum)
            {
                Debug.LogWarning($"Cannot convert value to unsupported Unity struct type: {targetType.Name}");
                return null;
            }

            // 最後の手段として Convert.ChangeType を試す
            try
            {
                return System.Convert.ChangeType(value, targetType);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to convert value to {targetType.Name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// テスト用: インスタンスをリセットします。
        /// </summary>
        internal static void ResetInstance()
        {
            _instance = null;
        }
    }
}
