using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MCP.Editor.Base;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Component management command handler.
    /// Handles adding, removing, updating, and inspecting components on GameObjects.
    /// Supports batch operations with pattern matching.
    /// </summary>
    public class ComponentCommandHandler : BaseCommandHandler
    {
        #region ICommandHandler Implementation
        
        public override string Category => "component";
        
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "add",
            "remove",
            "update",
            "inspect",
            "addMultiple",
            "removeMultiple",
            "updateMultiple",
            "inspectMultiple"
        };
        
        #endregion
        
        #region BaseCommandHandler Overrides
        
        protected override bool RequiresCompilationWait(string operation)
        {
            var readOnlyOps = new[] { "inspect", "inspectMultiple" };
            return !readOnlyOps.Contains(operation);
        }
        
        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "add" => AddComponent(payload),
                "remove" => RemoveComponent(payload),
                "update" => UpdateComponent(payload),
                "inspect" => InspectComponent(payload),
                "addMultiple" => AddMultipleComponents(payload),
                "removeMultiple" => RemoveMultipleComponents(payload),
                "updateMultiple" => UpdateMultipleComponents(payload),
                "inspectMultiple" => InspectMultipleComponents(payload),
                _ => throw new InvalidOperationException($"Unknown component operation: {operation}")
            };
        }
        
        #endregion
        
        #region Component Operations
        
        /// <summary>
        /// Adds a component to a GameObject.
        /// </summary>
        private object AddComponent(Dictionary<string, object> payload)
        {
            var gameObject = ResolveGameObjectFromPayload(payload);
            var componentType = GetString(payload, "componentType");
            
            if (string.IsNullOrEmpty(componentType))
            {
                throw new InvalidOperationException("componentType parameter is required");
            }
            
            var type = ResolveType(componentType);
            
            if (!typeof(Component).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"Type {componentType} is not a Component");
            }
            
            var component = Undo.AddComponent(gameObject, type);
            
            // Apply property changes if provided
            if (payload.ContainsKey("propertyChanges"))
            {
                var propertyChanges = payload["propertyChanges"] as Dictionary<string, object>;
                if (propertyChanges != null)
                {
                    ApplyPropertyChanges(component, propertyChanges);
                }
            }
            
            return CreateSuccessResponse(
                ("gameObjectPath", GetGameObjectPath(gameObject)),
                ("componentType", type.FullName),
                ("instanceID", component.GetInstanceID())
            );
        }
        
        /// <summary>
        /// Removes a component from a GameObject.
        /// </summary>
        private object RemoveComponent(Dictionary<string, object> payload)
        {
            var gameObject = ResolveGameObjectFromPayload(payload);
            var componentType = GetString(payload, "componentType");
            
            if (string.IsNullOrEmpty(componentType))
            {
                throw new InvalidOperationException("componentType parameter is required");
            }
            
            var type = ResolveType(componentType);
            var component = gameObject.GetComponent(type);
            
            if (component == null)
            {
                throw new InvalidOperationException(
                    $"Component {componentType} not found on GameObject {GetGameObjectPath(gameObject)}"
                );
            }
            
            Undo.DestroyObjectImmediate(component);
            
            return CreateSuccessResponse(
                ("gameObjectPath", GetGameObjectPath(gameObject)),
                ("componentType", type.FullName),
                ("message", "Component removed successfully")
            );
        }
        
        /// <summary>
        /// Updates component properties.
        /// </summary>
        private object UpdateComponent(Dictionary<string, object> payload)
        {
            var gameObject = ResolveGameObjectFromPayload(payload);
            var componentType = GetString(payload, "componentType");
            
            if (string.IsNullOrEmpty(componentType))
            {
                throw new InvalidOperationException("componentType parameter is required");
            }
            
            var type = ResolveType(componentType);
            var component = gameObject.GetComponent(type);
            
            if (component == null)
            {
                throw new InvalidOperationException(
                    $"Component {componentType} not found on GameObject {GetGameObjectPath(gameObject)}"
                );
            }
            
            var propertyChanges = payload["propertyChanges"] as Dictionary<string, object>;
            if (propertyChanges == null)
            {
                throw new InvalidOperationException("propertyChanges parameter is required");
            }
            
            Undo.RecordObject(component, $"Update {componentType}");
            var updated = ApplyPropertyChanges(component, propertyChanges);
            
            return CreateSuccessResponse(
                ("gameObjectPath", GetGameObjectPath(gameObject)),
                ("componentType", type.FullName),
                ("updated", updated)
            );
        }
        
        /// <summary>
        /// Inspects a component and returns its properties.
        /// </summary>
        private object InspectComponent(Dictionary<string, object> payload)
        {
            var gameObject = ResolveGameObjectFromPayload(payload);
            var componentType = GetString(payload, "componentType");
            
            if (string.IsNullOrEmpty(componentType))
            {
                throw new InvalidOperationException("componentType parameter is required");
            }
            
            var type = ResolveType(componentType);
            var component = gameObject.GetComponent(type);
            
            if (component == null)
            {
                throw new InvalidOperationException(
                    $"Component {componentType} not found on GameObject {GetGameObjectPath(gameObject)}"
                );
            }
            
            var includeProperties = GetBool(payload, "includeProperties", true);
            var propertyFilter = ParsePropertyFilter(payload);
            
            var info = new Dictionary<string, object>
            {
                ["success"] = true,
                ["gameObjectPath"] = GetGameObjectPath(gameObject),
                ["componentType"] = type.FullName,
                ["instanceID"] = component.GetInstanceID()
            };
            
            if (includeProperties)
            {
                info["properties"] = SerializeComponentProperties(component, propertyFilter);
            }
            
            return info;
        }
        
        #endregion
        
        #region Batch Operations
        
        /// <summary>
        /// Adds a component to multiple GameObjects matching a pattern.
        /// </summary>
        private object AddMultipleComponents(Dictionary<string, object> payload)
        {
            var pattern = GetString(payload, "pattern");
            var componentType = GetString(payload, "componentType");
            var useRegex = GetBool(payload, "useRegex", false);
            var maxResults = GetInt(payload, "maxResults", 1000);
            var stopOnError = GetBool(payload, "stopOnError", false);
            
            // Get optional propertyChanges to apply after adding
            var propertyChanges = payload.ContainsKey("propertyChanges") 
                ? payload["propertyChanges"] as Dictionary<string, object> 
                : null;
            
            if (string.IsNullOrEmpty(pattern))
            {
                throw new InvalidOperationException("pattern parameter is required");
            }
            
            if (string.IsNullOrEmpty(componentType))
            {
                throw new InvalidOperationException("componentType parameter is required");
            }
            
            var type = ResolveType(componentType);
            var gameObjects = FindGameObjectsByPattern(pattern, useRegex, maxResults).ToList();
            
            var results = new List<Dictionary<string, object>>();
            int successCount = 0;
            int failureCount = 0;
            
            foreach (var go in gameObjects)
            {
                try
                {
                    var component = Undo.AddComponent(go, type);
                    
                    // Apply property changes if provided
                    List<string> updatedProperties = null;
                    if (propertyChanges != null && propertyChanges.Count > 0)
                    {
                        updatedProperties = ApplyPropertyChanges(component, propertyChanges);
                    }
                    
                    successCount++;
                    
                    var resultEntry = new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["gameObjectPath"] = GetGameObjectPath(go),
                        ["instanceID"] = component.GetInstanceID()
                    };
                    
                    if (updatedProperties != null)
                    {
                        resultEntry["updatedProperties"] = updatedProperties;
                    }
                    
                    results.Add(resultEntry);
                }
                catch (Exception ex)
                {
                    failureCount++;
                    
                    results.Add(new Dictionary<string, object>
                    {
                        ["success"] = false,
                        ["gameObjectPath"] = GetGameObjectPath(go),
                        ["error"] = ex.Message
                    });
                    
                    if (stopOnError)
                    {
                        throw;
                    }
                }
            }
            
            return CreateSuccessResponse(
                ("results", results),
                ("processed", gameObjects.Count),
                ("succeeded", successCount),
                ("failed", failureCount)
            );
        }
        
        /// <summary>
        /// Removes a component from multiple GameObjects matching a pattern.
        /// </summary>
        private object RemoveMultipleComponents(Dictionary<string, object> payload)
        {
            var pattern = GetString(payload, "pattern");
            var componentType = GetString(payload, "componentType");
            var useRegex = GetBool(payload, "useRegex", false);
            var maxResults = GetInt(payload, "maxResults", 1000);
            var stopOnError = GetBool(payload, "stopOnError", false);
            
            if (string.IsNullOrEmpty(pattern))
            {
                throw new InvalidOperationException("pattern parameter is required");
            }
            
            if (string.IsNullOrEmpty(componentType))
            {
                throw new InvalidOperationException("componentType parameter is required");
            }
            
            var type = ResolveType(componentType);
            var gameObjects = FindGameObjectsByPattern(pattern, useRegex, maxResults).ToList();
            
            var results = new List<Dictionary<string, object>>();
            int successCount = 0;
            int failureCount = 0;
            
            foreach (var go in gameObjects)
            {
                try
                {
                    var component = go.GetComponent(type);
                    if (component == null)
                    {
                        throw new InvalidOperationException($"Component not found");
                    }
                    
                    Undo.DestroyObjectImmediate(component);
                    successCount++;
                    
                    results.Add(new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["gameObjectPath"] = GetGameObjectPath(go)
                    });
                }
                catch (Exception ex)
                {
                    failureCount++;
                    
                    results.Add(new Dictionary<string, object>
                    {
                        ["success"] = false,
                        ["gameObjectPath"] = GetGameObjectPath(go),
                        ["error"] = ex.Message
                    });
                    
                    if (stopOnError)
                    {
                        throw;
                    }
                }
            }
            
            return CreateSuccessResponse(
                ("results", results),
                ("processed", gameObjects.Count),
                ("succeeded", successCount),
                ("failed", failureCount)
            );
        }
        
        /// <summary>
        /// Updates a component on multiple GameObjects matching a pattern.
        /// </summary>
        private object UpdateMultipleComponents(Dictionary<string, object> payload)
        {
            var pattern = GetString(payload, "pattern");
            var componentType = GetString(payload, "componentType");
            var propertyChanges = payload["propertyChanges"] as Dictionary<string, object>;
            var useRegex = GetBool(payload, "useRegex", false);
            var maxResults = GetInt(payload, "maxResults", 1000);
            var stopOnError = GetBool(payload, "stopOnError", false);
            
            if (string.IsNullOrEmpty(pattern))
            {
                throw new InvalidOperationException("pattern parameter is required");
            }
            
            if (propertyChanges == null)
            {
                throw new InvalidOperationException("propertyChanges parameter is required");
            }
            
            var type = ResolveType(componentType);
            var gameObjects = FindGameObjectsByPattern(pattern, useRegex, maxResults).ToList();
            
            var results = new List<Dictionary<string, object>>();
            int successCount = 0;
            int failureCount = 0;
            
            foreach (var go in gameObjects)
            {
                try
                {
                    var component = go.GetComponent(type);
                    if (component == null)
                    {
                        throw new InvalidOperationException($"Component not found");
                    }
                    
                    Undo.RecordObject(component, $"Update {componentType}");
                    var updated = ApplyPropertyChanges(component, propertyChanges);
                    successCount++;
                    
                    results.Add(new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["gameObjectPath"] = GetGameObjectPath(go),
                        ["updated"] = updated
                    });
                }
                catch (Exception ex)
                {
                    failureCount++;
                    
                    results.Add(new Dictionary<string, object>
                    {
                        ["success"] = false,
                        ["gameObjectPath"] = GetGameObjectPath(go),
                        ["error"] = ex.Message
                    });
                    
                    if (stopOnError)
                    {
                        throw;
                    }
                }
            }
            
            return CreateSuccessResponse(
                ("results", results),
                ("processed", gameObjects.Count),
                ("succeeded", successCount),
                ("failed", failureCount)
            );
        }
        
        /// <summary>
        /// Inspects a component on multiple GameObjects matching a pattern.
        /// </summary>
        private object InspectMultipleComponents(Dictionary<string, object> payload)
        {
            var pattern = GetString(payload, "pattern");
            var componentType = GetString(payload, "componentType");
            var useRegex = GetBool(payload, "useRegex", false);
            var maxResults = GetInt(payload, "maxResults", 1000);
            var includeProperties = GetBool(payload, "includeProperties", true);
            var propertyFilter = ParsePropertyFilter(payload);
            
            if (string.IsNullOrEmpty(pattern))
            {
                throw new InvalidOperationException("pattern parameter is required");
            }
            
            var type = ResolveType(componentType);
            var gameObjects = FindGameObjectsByPattern(pattern, useRegex, maxResults).ToList();
            
            var results = new List<Dictionary<string, object>>();
            
            foreach (var go in gameObjects)
            {
                var component = go.GetComponent(type);
                if (component != null)
                {
                    var info = new Dictionary<string, object>
                    {
                        ["gameObjectPath"] = GetGameObjectPath(go),
                        ["instanceID"] = component.GetInstanceID()
                    };
                    
                    if (includeProperties)
                    {
                        info["properties"] = SerializeComponentProperties(component, propertyFilter);
                    }
                    
                    results.Add(info);
                }
            }
            
            return CreateSuccessResponse(
                ("results", results),
                ("count", results.Count)
            );
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Gets the hierarchical path of a GameObject.
        /// </summary>
        private string GetGameObjectPath(GameObject go)
        {
            if (go == null)
            {
                return null;
            }
            
            var path = go.name;
            var parent = go.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
        
        /// <summary>
        /// Applies property changes to a component.
        /// </summary>
        private List<string> ApplyPropertyChanges(Component component, Dictionary<string, object> propertyChanges)
        {
            var updated = new List<string>();
            var failed = new List<string>();
            var type = component.GetType();
            
            foreach (var kvp in propertyChanges)
            {
                var propertyName = kvp.Key;
                var value = kvp.Value;
                
                // Try property first
                var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (property != null && property.CanWrite)
                {
                    var convertedValue = ConvertValue(value, property.PropertyType);
                    property.SetValue(component, convertedValue);
                    updated.Add(propertyName);
                    continue;
                }
                
                // Try field (including private fields with [SerializeField] attribute)
                var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    // For private fields, require SerializeField attribute
                    if (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null)
                    {
                        Debug.LogWarning($"Private field '{propertyName}' on {type.Name} is not marked with [SerializeField]");
                        failed.Add(propertyName);
                        continue;
                    }
                    
                    var convertedValue = ConvertValue(value, field.FieldType);
                    field.SetValue(component, convertedValue);
                    updated.Add(propertyName);
                    continue;
                }
                
                failed.Add(propertyName);
                Debug.LogWarning($"Property or field '{propertyName}' not found on {type.Name}");
            }
            
            return updated;
        }
        
        /// <summary>
        /// Serializes component properties to a dictionary.
        /// </summary>
        private Dictionary<string, object> SerializeComponentProperties(
            Component component, 
            List<string> propertyFilter)
        {
            var properties = new Dictionary<string, object>();
            var type = component.GetType();
            
            // Serialize public properties
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead)
                    continue;
                
                if (propertyFilter != null && !propertyFilter.Contains(prop.Name))
                    continue;
                
                try
                {
                    var value = prop.GetValue(component);
                    properties[prop.Name] = SerializeValue(value);
                }
                catch
                {
                    // Skip properties that throw on access
                }
            }
            
            // Serialize public fields and private fields with [SerializeField] attribute
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null);
            
            foreach (var field in fields)
            {
                if (propertyFilter != null && !propertyFilter.Contains(field.Name))
                    continue;
                
                try
                {
                    var value = field.GetValue(component);
                    properties[field.Name] = SerializeValue(value);
                }
                catch
                {
                    // Skip fields that throw on access
                }
            }
            
            return properties;
        }
        
        /// <summary>
        /// Serializes a value to a JSON-compatible type.
        /// </summary>
        private object SerializeValue(object value)
        {
            if (value == null)
                return null;
            
            if (value is Vector3 v3)
                return new { x = v3.x, y = v3.y, z = v3.z };
            
            if (value is Vector2 v2)
                return new { x = v2.x, y = v2.y };
            
            if (value is Color color)
                return new { r = color.r, g = color.g, b = color.b, a = color.a };
            
            if (value is Quaternion quat)
                return new { x = quat.x, y = quat.y, z = quat.z, w = quat.w };
            
            return value.ToString();
        }
        
        /// <summary>
        /// Converts a value to the target type.
        /// </summary>
        private object ConvertValue(object value, Type targetType)
        {
            if (value == null)
                return null;
            
            if (targetType.IsInstanceOfType(value))
                return value;
            
            // Handle explicit reference formats:
            // Format 1: { "$type": "reference", "$path": "path/to/object" }
            // Format 2: { "$ref": "path/to/object" }
            if (value is Dictionary<string, object> refDict)
            {
                // Format 1: { "$type": "reference", "$path": "..." }
                if (refDict.TryGetValue("$type", out var typeValue) && 
                    typeValue?.ToString() == "reference")
                {
                    if (refDict.TryGetValue("$path", out var pathValue) && pathValue != null)
                    {
                        return ResolveUnityObjectFromPath(pathValue.ToString(), targetType);
                    }
                    return null;
                }
                
                // Format 2: { "$ref": "..." }
                if (refDict.TryGetValue("$ref", out var refValue) && refValue != null)
                {
                    return ResolveUnityObjectFromPath(refValue.ToString(), targetType);
                }
            }
            
            // Handle Unity Object references from string path
            if (value is string stringValue && typeof(UnityEngine.Object).IsAssignableFrom(targetType))
            {
                return ResolveUnityObjectFromPath(stringValue, targetType);
            }
            
            // Handle basic types
            if (targetType == typeof(float))
            {
                if (value is double d) return (float)d;
                if (value is long l) return (float)l;
                if (value is int i) return (float)i;
                return Convert.ToSingle(value);
            }
            
            if (targetType == typeof(int))
            {
                if (value is long l) return (int)l;
                if (value is double d) return (int)d;
                return Convert.ToInt32(value);
            }
            
            if (targetType == typeof(bool))
            {
                if (value is string s) return bool.Parse(s);
                return Convert.ToBoolean(value);
            }
            
            // Handle Unity types from Dictionary
            if (value is Dictionary<string, object> dict)
            {
                if (targetType == typeof(Vector3))
                {
                    return new Vector3(
                        Convert.ToSingle(dict.GetValueOrDefault("x", 0f)),
                        Convert.ToSingle(dict.GetValueOrDefault("y", 0f)),
                        Convert.ToSingle(dict.GetValueOrDefault("z", 0f))
                    );
                }
                
                if (targetType == typeof(Vector2))
                {
                    return new Vector2(
                        Convert.ToSingle(dict.GetValueOrDefault("x", 0f)),
                        Convert.ToSingle(dict.GetValueOrDefault("y", 0f))
                    );
                }
                
                if (targetType == typeof(Vector4))
                {
                    return new Vector4(
                        Convert.ToSingle(dict.GetValueOrDefault("x", 0f)),
                        Convert.ToSingle(dict.GetValueOrDefault("y", 0f)),
                        Convert.ToSingle(dict.GetValueOrDefault("z", 0f)),
                        Convert.ToSingle(dict.GetValueOrDefault("w", 0f))
                    );
                }
                
                if (targetType == typeof(Color))
                {
                    return new Color(
                        Convert.ToSingle(dict.GetValueOrDefault("r", 1f)),
                        Convert.ToSingle(dict.GetValueOrDefault("g", 1f)),
                        Convert.ToSingle(dict.GetValueOrDefault("b", 1f)),
                        Convert.ToSingle(dict.GetValueOrDefault("a", 1f))
                    );
                }
                
                if (targetType == typeof(Color32))
                {
                    return new Color32(
                        Convert.ToByte(dict.GetValueOrDefault("r", (byte)255)),
                        Convert.ToByte(dict.GetValueOrDefault("g", (byte)255)),
                        Convert.ToByte(dict.GetValueOrDefault("b", (byte)255)),
                        Convert.ToByte(dict.GetValueOrDefault("a", (byte)255))
                    );
                }
                
                if (targetType == typeof(Quaternion))
                {
                    return new Quaternion(
                        Convert.ToSingle(dict.GetValueOrDefault("x", 0f)),
                        Convert.ToSingle(dict.GetValueOrDefault("y", 0f)),
                        Convert.ToSingle(dict.GetValueOrDefault("z", 0f)),
                        Convert.ToSingle(dict.GetValueOrDefault("w", 1f))
                    );
                }
                
                if (targetType == typeof(Rect))
                {
                    return new Rect(
                        Convert.ToSingle(dict.GetValueOrDefault("x", 0f)),
                        Convert.ToSingle(dict.GetValueOrDefault("y", 0f)),
                        Convert.ToSingle(dict.GetValueOrDefault("width", 0f)),
                        Convert.ToSingle(dict.GetValueOrDefault("height", 0f))
                    );
                }
                
                if (targetType == typeof(Bounds))
                {
                    var center = Vector3.zero;
                    var size = Vector3.zero;
                    
                    if (dict.ContainsKey("center") && dict["center"] is Dictionary<string, object> centerDict)
                    {
                        center = new Vector3(
                            Convert.ToSingle(centerDict.GetValueOrDefault("x", 0f)),
                            Convert.ToSingle(centerDict.GetValueOrDefault("y", 0f)),
                            Convert.ToSingle(centerDict.GetValueOrDefault("z", 0f))
                        );
                    }
                    
                    if (dict.ContainsKey("size") && dict["size"] is Dictionary<string, object> sizeDict)
                    {
                        size = new Vector3(
                            Convert.ToSingle(sizeDict.GetValueOrDefault("x", 0f)),
                            Convert.ToSingle(sizeDict.GetValueOrDefault("y", 0f)),
                            Convert.ToSingle(sizeDict.GetValueOrDefault("z", 0f))
                        );
                    }
                    
                    return new Bounds(center, size);
                }
            }
            
            // Handle enum types
            if (targetType.IsEnum)
            {
                if (value is string enumStr)
                    return Enum.Parse(targetType, enumStr, true);
                return Enum.ToObject(targetType, Convert.ToInt32(value));
            }
            
            return Convert.ChangeType(value, targetType);
        }
        
        /// <summary>
        /// Resolves a Unity Object from a string path.
        /// Handles both GameObject references and Component references.
        /// </summary>
        /// <param name="path">The GameObject path (e.g., "Canvas/Panel/Button")</param>
        /// <param name="targetType">The expected type (GameObject, Component subclass, etc.)</param>
        /// <returns>The resolved Unity Object, or null if not found</returns>
        private UnityEngine.Object ResolveUnityObjectFromPath(string path, Type targetType)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            
            // Try to find the GameObject by path
            GameObject targetObject = GameObject.Find(path);
            
            // If not found by full path, try finding by name in hierarchy
            if (targetObject == null)
            {
                // Try searching in all root objects
                var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
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
            
            // If target type is GameObject, return it directly
            if (targetType == typeof(GameObject))
            {
                return targetObject;
            }
            
            // If target type is Transform, return the transform
            if (targetType == typeof(Transform) || targetType == typeof(RectTransform))
            {
                if (targetType == typeof(RectTransform))
                    return targetObject.GetComponent<RectTransform>() ?? (UnityEngine.Object)targetObject.transform;
                return targetObject.transform;
            }
            
            // If target type is a Component, get it from the GameObject
            if (typeof(Component).IsAssignableFrom(targetType))
            {
                var component = targetObject.GetComponent(targetType);
                if (component == null)
                {
                    Debug.LogWarning($"Component {targetType.Name} not found on GameObject at path: {path}");
                }
                return component;
            }
            
            // For other Unity Object types (like ScriptableObject, Material, etc.),
            // try to load from asset path if it looks like an asset path
            if (path.StartsWith("Assets/") && path.Contains("."))
            {
                return AssetDatabase.LoadAssetAtPath(path, targetType);
            }
            
            return null;
        }
        
        /// <summary>
        /// Finds a child GameObject by path relative to a parent transform.
        /// </summary>
        private GameObject FindChildByPath(Transform parent, string path)
        {
            if (parent == null || string.IsNullOrEmpty(path))
                return null;
            
            // Check if this object matches
            string parentPath = GetTransformPath(parent);
            if (parentPath == path || parent.name == path)
                return parent.gameObject;
            
            // Check if path starts with parent name
            if (path.StartsWith(parent.name + "/"))
            {
                string remainingPath = path.Substring(parent.name.Length + 1);
                return FindChildRecursive(parent, remainingPath);
            }
            
            // Search children recursively
            foreach (Transform child in parent)
            {
                var result = FindChildByPath(child, path);
                if (result != null)
                    return result;
            }
            
            return null;
        }
        
        /// <summary>
        /// Finds a child by relative path.
        /// </summary>
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
        
        /// <summary>
        /// Gets the full hierarchy path of a Transform.
        /// </summary>
        private string GetTransformPath(Transform transform)
        {
            if (transform == null)
                return null;
            
            var path = transform.name;
            var parent = transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
        
        /// <summary>
        /// Parses the propertyFilter from payload, handling various input formats.
        /// </summary>
        private List<string> ParsePropertyFilter(Dictionary<string, object> payload)
        {
            if (!payload.ContainsKey("propertyFilter"))
                return null;
            
            var filterValue = payload["propertyFilter"];
            
            // Handle null
            if (filterValue == null)
                return null;
            
            // Handle List<object> (common from JSON deserialization)
            if (filterValue is List<object> listObj)
                return listObj.Select(o => o?.ToString()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            
            // Handle object[] array
            if (filterValue is object[] objArray)
                return objArray.Select(o => o?.ToString()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            
            // Handle string[] array
            if (filterValue is string[] strArray)
                return strArray.Where(s => !string.IsNullOrEmpty(s)).ToList();
            
            // Handle IEnumerable<string>
            if (filterValue is IEnumerable<string> strEnum)
                return strEnum.Where(s => !string.IsNullOrEmpty(s)).ToList();
            
            // Handle comma-separated string
            if (filterValue is string str && !string.IsNullOrEmpty(str))
                return str.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(s => s.Trim())
                         .Where(s => !string.IsNullOrEmpty(s))
                         .ToList();
            
            // Handle generic IEnumerable
            if (filterValue is System.Collections.IEnumerable enumerable)
            {
                var result = new List<string>();
                foreach (var item in enumerable)
                {
                    var itemStr = item?.ToString();
                    if (!string.IsNullOrEmpty(itemStr))
                        result.Add(itemStr);
                }
                return result.Count > 0 ? result : null;
            }
            
            return null;
        }
        
        #endregion
    }
}

