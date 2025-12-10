using System;
using System.Collections.Generic;
using System.Reflection;
using MCP.Editor.Interfaces;
using UnityEngine;

namespace MCP.Editor.Base
{
    /// <summary>
    /// コンポーネントにプロパティを適用するクラス。
    /// Template Methodパターンを使用して、プロパティ適用の共通ロジックを提供します。
    /// </summary>
    public class ComponentPropertyApplier : IPropertyApplier
    {
        private readonly ValueConverterManager _converterManager;

        /// <summary>
        /// デフォルトのValueConverterManagerを使用してインスタンスを作成します。
        /// </summary>
        public ComponentPropertyApplier() : this(ValueConverterManager.Instance)
        {
        }

        /// <summary>
        /// カスタムのValueConverterManagerを使用してインスタンスを作成します。
        /// </summary>
        /// <param name="converterManager">使用するValueConverterManager</param>
        public ComponentPropertyApplier(ValueConverterManager converterManager)
        {
            _converterManager = converterManager ?? throw new ArgumentNullException(nameof(converterManager));
        }

        /// <inheritdoc/>
        public PropertyApplyResult ApplyProperties(Component component, Dictionary<string, object> propertyChanges)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (propertyChanges == null)
                throw new ArgumentNullException(nameof(propertyChanges));

            var result = new PropertyApplyResult();
            var type = component.GetType();

            foreach (var kvp in propertyChanges)
            {
                var propertyName = kvp.Key;
                var value = kvp.Value;

                try
                {
                    if (TryApplyProperty(component, type, propertyName, value))
                    {
                        result.Updated.Add(propertyName);
                    }
                    else if (TryApplyField(component, type, propertyName, value))
                    {
                        result.Updated.Add(propertyName);
                    }
                    else
                    {
                        result.Failed.Add(propertyName);
                        result.Errors[propertyName] = $"Property or field '{propertyName}' not found on {type.Name}";
                        Debug.LogWarning($"Property or field '{propertyName}' not found on {type.Name}");
                    }
                }
                catch (Exception ex)
                {
                    result.Failed.Add(propertyName);
                    result.Errors[propertyName] = ex.Message;
                    Debug.LogWarning($"Failed to set property '{propertyName}' on {type.Name}: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// プロパティへの値適用を試みます。
        /// </summary>
        private bool TryApplyProperty(Component component, Type type, string propertyName, object value)
        {
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (property == null || !property.CanWrite)
                return false;

            var convertedValue = _converterManager.Convert(value, property.PropertyType);
            property.SetValue(component, convertedValue);
            return true;
        }

        /// <summary>
        /// フィールドへの値適用を試みます。
        /// </summary>
        private bool TryApplyField(Component component, Type type, string propertyName, object value)
        {
            var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (field == null)
                return false;

            // プライベートフィールドは [SerializeField] 属性が必要
            if (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null)
            {
                Debug.LogWarning($"Private field '{propertyName}' on {type.Name} is not marked with [SerializeField]");
                return false;
            }

            var convertedValue = _converterManager.Convert(value, field.FieldType);
            field.SetValue(component, convertedValue);
            return true;
        }
    }
}
