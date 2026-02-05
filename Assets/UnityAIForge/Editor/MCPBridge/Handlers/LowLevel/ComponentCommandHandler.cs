using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MCP.Editor.Base;
using MCP.Editor.Interfaces;
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
        #region Fields

        private readonly IPropertyApplier _propertyApplier;
        private readonly ValueConverterManager _converterManager;

        #endregion

        #region Constructor

        public ComponentCommandHandler()
        {
            _converterManager = ValueConverterManager.Instance;
            _propertyApplier = new ComponentPropertyApplier(_converterManager);
        }

        #endregion

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
            List<string> updatedProperties = null;
            if (payload.ContainsKey("propertyChanges"))
            {
                var propertyChanges = payload["propertyChanges"] as Dictionary<string, object>;
                if (propertyChanges != null)
                {
                    var result = _propertyApplier.ApplyProperties(component, propertyChanges);
                    updatedProperties = result.Updated;
                }
            }

            var response = CreateSuccessResponse(
                ("gameObjectPath", BuildGameObjectPath(gameObject)),
                ("componentType", type.FullName),
                ("instanceID", component.GetInstanceID())
            );

            if (updatedProperties != null && updatedProperties.Count > 0)
            {
                response["updatedProperties"] = updatedProperties;
            }

            return response;
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
                    $"Component {componentType} not found on GameObject {BuildGameObjectPath(gameObject)}"
                );
            }
            
            Undo.DestroyObjectImmediate(component);
            
            return CreateSuccessResponse(
                ("gameObjectPath", BuildGameObjectPath(gameObject)),
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
                    $"Component {componentType} not found on GameObject {BuildGameObjectPath(gameObject)}"
                );
            }
            
            var propertyChanges = payload["propertyChanges"] as Dictionary<string, object>;
            if (propertyChanges == null)
            {
                throw new InvalidOperationException("propertyChanges parameter is required");
            }
            
            Undo.RecordObject(component, $"Update {componentType}");
            var result = _propertyApplier.ApplyProperties(component, propertyChanges);

            return CreateSuccessResponse(
                ("gameObjectPath", BuildGameObjectPath(gameObject)),
                ("componentType", type.FullName),
                ("updated", result.Updated)
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
                    $"Component {componentType} not found on GameObject {BuildGameObjectPath(gameObject)}"
                );
            }
            
            var includeProperties = GetBool(payload, "includeProperties", true);
            var propertyFilter = ParsePropertyFilter(payload);
            
            var info = new Dictionary<string, object>
            {
                ["success"] = true,
                ["gameObjectPath"] = BuildGameObjectPath(gameObject),
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
                        var applyResult = _propertyApplier.ApplyProperties(component, propertyChanges);
                        updatedProperties = applyResult.Updated;
                    }

                    successCount++;

                    var resultEntry = new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["gameObjectPath"] = BuildGameObjectPath(go),
                        ["instanceID"] = component.GetInstanceID()
                    };

                    if (updatedProperties != null && updatedProperties.Count > 0)
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
                        ["gameObjectPath"] = BuildGameObjectPath(go),
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
                        ["gameObjectPath"] = BuildGameObjectPath(go)
                    });
                }
                catch (Exception ex)
                {
                    failureCount++;
                    
                    results.Add(new Dictionary<string, object>
                    {
                        ["success"] = false,
                        ["gameObjectPath"] = BuildGameObjectPath(go),
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
                    var applyResult = _propertyApplier.ApplyProperties(component, propertyChanges);
                    successCount++;

                    results.Add(new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["gameObjectPath"] = BuildGameObjectPath(go),
                        ["updated"] = applyResult.Updated
                    });
                }
                catch (Exception ex)
                {
                    failureCount++;
                    
                    results.Add(new Dictionary<string, object>
                    {
                        ["success"] = false,
                        ["gameObjectPath"] = BuildGameObjectPath(go),
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
                        ["gameObjectPath"] = BuildGameObjectPath(go),
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

