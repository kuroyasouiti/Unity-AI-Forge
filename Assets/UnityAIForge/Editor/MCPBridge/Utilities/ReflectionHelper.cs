using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MCP.Editor.Utilities
{
    /// <summary>
    /// Safe reflection helper for GameKit handlers that operate on code-generated components.
    /// Provides null-checked method invocation, field/property access, and caching.
    /// </summary>
    internal static class ReflectionHelper
    {
        private static readonly Dictionary<(Type, string), MethodInfo> MethodCache = new();
        private static readonly Dictionary<(Type, string), PropertyInfo> PropertyCache = new();
        private static readonly Dictionary<(Type, string), FieldInfo> FieldCache = new();

        /// <summary>
        /// Invokes a method on a component safely. Returns default(T) and logs an error if the method is not found.
        /// </summary>
        public static T InvokeMethodSafe<T>(Component component, string methodName, params object[] args)
        {
            if (component == null)
                throw new InvalidOperationException("Component is null.");

            var method = GetCachedMethod(component.GetType(), methodName);
            if (method == null)
            {
                throw new InvalidOperationException(
                    $"Method '{methodName}' not found on component type '{component.GetType().Name}'. " +
                    "Ensure the generated script has been compiled (use unity_compilation_await).");
            }

            var result = method.Invoke(component, args);
            if (result is T typedResult) return typedResult;
            return default;
        }

        /// <summary>
        /// Invokes a void method on a component safely.
        /// </summary>
        public static void InvokeVoidMethodSafe(Component component, string methodName, params object[] args)
        {
            if (component == null)
                throw new InvalidOperationException("Component is null.");

            var method = GetCachedMethod(component.GetType(), methodName);
            if (method == null)
            {
                throw new InvalidOperationException(
                    $"Method '{methodName}' not found on component type '{component.GetType().Name}'. " +
                    "Ensure the generated script has been compiled (use unity_compilation_await).");
            }

            method.Invoke(component, args);
        }

        /// <summary>
        /// Tries to invoke a method. Returns false if the method doesn't exist.
        /// </summary>
        public static bool TryInvokeMethod(Component component, string methodName, out object result, params object[] args)
        {
            result = null;
            if (component == null) return false;

            var method = GetCachedMethod(component.GetType(), methodName);
            if (method == null) return false;

            result = method.Invoke(component, args);
            return true;
        }

        /// <summary>
        /// Gets a property value safely.
        /// </summary>
        public static T GetPropertySafe<T>(Component component, string propertyName)
        {
            if (component == null) return default;

            var prop = GetCachedProperty(component.GetType(), propertyName);
            if (prop == null) return default;

            var value = prop.GetValue(component);
            if (value is T typedValue) return typedValue;
            return default;
        }

        /// <summary>
        /// Gets a field value from an object safely.
        /// </summary>
        public static T GetFieldSafe<T>(object obj, string fieldName)
        {
            if (obj == null) return default;

            var field = GetCachedField(obj.GetType(), fieldName);
            if (field == null) return default;

            var value = field.GetValue(obj);
            if (value is T typedValue) return typedValue;
            return default;
        }

        /// <summary>
        /// Sets a field value on an object safely. Throws with a clear message on failure.
        /// </summary>
        public static void SetFieldSafe(object obj, string fieldName, object value)
        {
            if (obj == null)
                throw new InvalidOperationException("Cannot set field on null object.");

            var field = GetCachedField(obj.GetType(), fieldName);
            if (field == null)
            {
                throw new InvalidOperationException(
                    $"Field '{fieldName}' not found on type '{obj.GetType().Name}'.");
            }

            field.SetValue(obj, value);
        }

        /// <summary>
        /// Checks if a method exists on the component's type.
        /// </summary>
        public static bool HasMethod(Component component, string methodName)
        {
            if (component == null) return false;
            return GetCachedMethod(component.GetType(), methodName) != null;
        }

        /// <summary>
        /// Clears the reflection cache. Call after script recompilation if needed.
        /// </summary>
        public static void ClearCache()
        {
            MethodCache.Clear();
            PropertyCache.Clear();
            FieldCache.Clear();
        }

        #region Cache Helpers

        private static MethodInfo GetCachedMethod(Type type, string name)
        {
            var key = (type, name);
            if (MethodCache.TryGetValue(key, out var cached)) return cached;
            var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
            MethodCache[key] = method;
            return method;
        }

        private static PropertyInfo GetCachedProperty(Type type, string name)
        {
            var key = (type, name);
            if (PropertyCache.TryGetValue(key, out var cached)) return cached;
            var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            PropertyCache[key] = prop;
            return prop;
        }

        private static FieldInfo GetCachedField(Type type, string name)
        {
            var key = (type, name);
            if (FieldCache.TryGetValue(key, out var cached)) return cached;
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            FieldCache[key] = field;
            return field;
        }

        #endregion
    }
}
