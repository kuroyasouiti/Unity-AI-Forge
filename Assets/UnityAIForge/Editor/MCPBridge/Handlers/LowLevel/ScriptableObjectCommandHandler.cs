using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MCP.Editor.Base;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// ScriptableObject management command handler.
    /// Handles ScriptableObject creation, inspection, updating, deletion, duplication, listing, and searching.
    /// </summary>
    public class ScriptableObjectCommandHandler : BaseCommandHandler
    {
        #region ICommandHandler Implementation
        
        public override string Category => "scriptableObject";
        
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "create",
            "inspect",
            "update",
            "delete",
            "duplicate",
            "list",
            "findByType"
        };
        
        #endregion
        
        #region BaseCommandHandler Overrides
        
        protected override bool RequiresCompilationWait(string operation)
        {
            // Read-only operations
            var readOnlyOps = new[] { "inspect", "list", "findByType" };
            return !readOnlyOps.Contains(operation);
        }
        
        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateScriptableObject(payload),
                "inspect" => InspectScriptableObject(payload),
                "update" => UpdateScriptableObject(payload),
                "delete" => DeleteScriptableObject(payload),
                "duplicate" => DuplicateScriptableObject(payload),
                "list" => ListScriptableObjects(payload),
                "findByType" => FindScriptableObjectsByType(payload),
                _ => throw new InvalidOperationException($"Unknown scriptableObject operation: {operation}")
            };
        }
        
        #endregion
        
        #region ScriptableObject Operations
        
        /// <summary>
        /// Creates a new ScriptableObject asset.
        /// </summary>
        private object CreateScriptableObject(Dictionary<string, object> payload)
        {
            var typeName = GetString(payload, "typeName");
            var assetPath = GetString(payload, "assetPath");
            
            if (string.IsNullOrEmpty(typeName))
            {
                throw new InvalidOperationException("typeName is required");
            }
            
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("assetPath is required");
            }
            
            // Validate asset path
            if (!ValidateAssetPath(assetPath) || !assetPath.EndsWith(".asset"))
            {
                throw new InvalidOperationException($"Invalid asset path: {assetPath}. Must start with 'Assets/' and end with '.asset'");
            }
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Check if file already exists
            if (File.Exists(assetPath))
            {
                throw new InvalidOperationException($"Asset already exists: {assetPath}");
            }
            
            // Resolve and validate type
            var type = ResolveType(typeName);
            
            if (!typeof(ScriptableObject).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"Type {typeName} is not a ScriptableObject");
            }
            
            if (type.IsAbstract)
            {
                throw new InvalidOperationException($"Cannot create instance of abstract type {typeName}");
            }
            
            // Create instance
            var instance = ScriptableObject.CreateInstance(type);
            if (instance == null)
            {
                throw new InvalidOperationException($"Failed to create instance of {typeName}");
            }
            
            // Apply initial properties if provided
            var appliedProperties = new List<string>();
            var failedProperties = new List<string>();
            
            if (payload.TryGetValue("properties", out var propertiesObj) && propertiesObj is Dictionary<string, object> properties)
            {
                foreach (var kvp in properties)
                {
                    try
                    {
                        ApplyPropertyToScriptableObject(instance, kvp.Key, kvp.Value);
                        appliedProperties.Add(kvp.Key);
                    }
                    catch (Exception ex)
                    {
                        failedProperties.Add($"{kvp.Key}: {ex.Message}");
                    }
                }
            }
            
            // Create asset
            AssetDatabase.CreateAsset(instance, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            var result = CreateSuccessResponse(
                ("assetPath", assetPath),
                ("typeName", type.FullName),
                ("guid", AssetDatabase.AssetPathToGUID(assetPath))
            );
            
            if (appliedProperties.Count > 0)
            {
                ((Dictionary<string, object>)result)["appliedProperties"] = appliedProperties;
            }
            
            if (failedProperties.Count > 0)
            {
                ((Dictionary<string, object>)result)["failedProperties"] = failedProperties;
            }
            
            return result;
        }
        
        /// <summary>
        /// Inspects a ScriptableObject asset.
        /// </summary>
        private object InspectScriptableObject(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            var assetGuid = GetString(payload, "assetGuid");
            
            // Resolve path from GUID if provided
            if (!string.IsNullOrEmpty(assetGuid))
            {
                assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            }
            
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("Either assetPath or assetGuid is required");
            }
            
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"ScriptableObject not found: {assetPath}");
            }
            
            var includeProperties = GetBool(payload, "includeProperties", true);
            var propertyFilter = payload.ContainsKey("propertyFilter")
                ? (payload["propertyFilter"] as List<object>)?.Select(o => o.ToString()).ToList()
                : null;
            
            var info = new Dictionary<string, object>
            {
                ["success"] = true,
                ["assetPath"] = assetPath,
                ["guid"] = AssetDatabase.AssetPathToGUID(assetPath),
                ["typeName"] = asset.GetType().FullName,
                ["name"] = asset.name
            };
            
            if (includeProperties)
            {
                info["properties"] = SerializeScriptableObjectProperties(asset, propertyFilter);
            }
            
            return info;
        }
        
        /// <summary>
        /// Updates properties of a ScriptableObject.
        /// </summary>
        private object UpdateScriptableObject(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            var assetGuid = GetString(payload, "assetGuid");
            
            // Resolve path from GUID if provided
            if (!string.IsNullOrEmpty(assetGuid))
            {
                assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            }
            
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("Either assetPath or assetGuid is required");
            }
            
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"ScriptableObject not found: {assetPath}");
            }
            
            var properties = payload["properties"] as Dictionary<string, object>;
            if (properties == null)
            {
                throw new InvalidOperationException("properties parameter is required");
            }
            
            var appliedProperties = new List<string>();
            var failedProperties = new List<string>();
            
            Undo.RecordObject(asset, $"Update ScriptableObject: {asset.name}");
            
            foreach (var kvp in properties)
            {
                try
                {
                    ApplyPropertyToScriptableObject(asset, kvp.Key, kvp.Value);
                    appliedProperties.Add(kvp.Key);
                }
                catch (Exception ex)
                {
                    failedProperties.Add($"{kvp.Key}: {ex.Message}");
                }
            }
            
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            
            var result = CreateSuccessResponse(
                ("assetPath", assetPath),
                ("appliedProperties", appliedProperties)
            );
            
            if (failedProperties.Count > 0)
            {
                ((Dictionary<string, object>)result)["failedProperties"] = failedProperties;
            }
            
            return result;
        }
        
        /// <summary>
        /// Deletes a ScriptableObject asset.
        /// </summary>
        private object DeleteScriptableObject(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            var assetGuid = GetString(payload, "assetGuid");
            
            // Resolve path from GUID if provided
            if (!string.IsNullOrEmpty(assetGuid))
            {
                assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            }
            
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("Either assetPath or assetGuid is required");
            }
            
            if (!File.Exists(assetPath))
            {
                throw new InvalidOperationException($"Asset not found: {assetPath}");
            }
            
            if (!AssetDatabase.DeleteAsset(assetPath))
            {
                throw new InvalidOperationException($"Failed to delete asset: {assetPath}");
            }
            
            AssetDatabase.Refresh();
            
            return CreateSuccessResponse(
                ("assetPath", assetPath),
                ("message", "ScriptableObject deleted successfully")
            );
        }
        
        /// <summary>
        /// Duplicates a ScriptableObject asset.
        /// </summary>
        private object DuplicateScriptableObject(Dictionary<string, object> payload)
        {
            var sourceAssetPath = GetString(payload, "sourceAssetPath");
            var sourceAssetGuid = GetString(payload, "sourceAssetGuid");
            var destinationAssetPath = GetString(payload, "destinationAssetPath");
            
            // Resolve source path from GUID if provided
            if (!string.IsNullOrEmpty(sourceAssetGuid))
            {
                sourceAssetPath = AssetDatabase.GUIDToAssetPath(sourceAssetGuid);
            }
            
            if (string.IsNullOrEmpty(sourceAssetPath))
            {
                throw new InvalidOperationException("Either sourceAssetPath or sourceAssetGuid is required");
            }
            
            if (string.IsNullOrEmpty(destinationAssetPath))
            {
                throw new InvalidOperationException("destinationAssetPath is required");
            }
            
            if (!File.Exists(sourceAssetPath))
            {
                throw new InvalidOperationException($"Source asset not found: {sourceAssetPath}");
            }
            
            if (File.Exists(destinationAssetPath))
            {
                throw new InvalidOperationException($"Destination asset already exists: {destinationAssetPath}");
            }
            
            // Ensure destination directory exists
            var directory = Path.GetDirectoryName(destinationAssetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            if (!AssetDatabase.CopyAsset(sourceAssetPath, destinationAssetPath))
            {
                throw new InvalidOperationException($"Failed to duplicate asset from {sourceAssetPath} to {destinationAssetPath}");
            }
            
            AssetDatabase.Refresh();
            
            return CreateSuccessResponse(
                ("sourceAssetPath", sourceAssetPath),
                ("destinationAssetPath", destinationAssetPath),
                ("guid", AssetDatabase.AssetPathToGUID(destinationAssetPath))
            );
        }
        
        /// <summary>
        /// Lists all ScriptableObject assets in a folder.
        /// </summary>
        private object ListScriptableObjects(Dictionary<string, object> payload)
        {
            var searchPath = GetString(payload, "searchPath", "Assets");
            var typeName = GetString(payload, "typeName");
            var maxResults = GetInt(payload, "maxResults", 1000);
            var offset = GetInt(payload, "offset", 0);
            
            var guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { searchPath });
            var results = new List<Dictionary<string, object>>();
            
            Type typeFilter = null;
            if (!string.IsNullOrEmpty(typeName))
            {
                typeFilter = ResolveType(typeName);
            }
            
            foreach (var guid in guids.Skip(offset).Take(maxResults))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                
                if (asset != null)
                {
                    // Apply type filter if specified
                    if (typeFilter != null && !typeFilter.IsInstanceOfType(asset))
                    {
                        continue;
                    }
                    
                    results.Add(new Dictionary<string, object>
                    {
                        ["assetPath"] = path,
                        ["guid"] = guid,
                        ["typeName"] = asset.GetType().FullName,
                        ["name"] = asset.name
                    });
                }
            }
            
            return CreateSuccessResponse(
                ("results", results),
                ("count", results.Count),
                ("total", guids.Length)
            );
        }
        
        /// <summary>
        /// Finds ScriptableObjects by type (including derived types).
        /// </summary>
        private object FindScriptableObjectsByType(Dictionary<string, object> payload)
        {
            var typeName = GetString(payload, "typeName");
            var searchPath = GetString(payload, "searchPath", "Assets");
            var maxResults = GetInt(payload, "maxResults", 1000);
            var offset = GetInt(payload, "offset", 0);
            var includeProperties = GetBool(payload, "includeProperties", false);
            
            if (string.IsNullOrEmpty(typeName))
            {
                throw new InvalidOperationException("typeName is required");
            }
            
            var type = ResolveType(typeName);
            
            if (!typeof(ScriptableObject).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"Type {typeName} is not a ScriptableObject");
            }
            
            var guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { searchPath });
            var results = new List<Dictionary<string, object>>();
            
            foreach (var guid in guids.Skip(offset))
            {
                if (results.Count >= maxResults)
                {
                    break;
                }
                
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                
                if (asset != null && type.IsInstanceOfType(asset))
                {
                    var info = new Dictionary<string, object>
                    {
                        ["assetPath"] = path,
                        ["guid"] = guid,
                        ["typeName"] = asset.GetType().FullName,
                        ["name"] = asset.name
                    };
                    
                    if (includeProperties)
                    {
                        info["properties"] = SerializeScriptableObjectProperties(asset, null);
                    }
                    
                    results.Add(info);
                }
            }
            
            return CreateSuccessResponse(
                ("results", results),
                ("count", results.Count),
                ("typeName", type.FullName)
            );
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Applies a property value to a ScriptableObject.
        /// </summary>
        private void ApplyPropertyToScriptableObject(ScriptableObject obj, string propertyName, object value)
        {
            var type = obj.GetType();
            
            // Try property first
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.CanWrite)
            {
                var convertedValue = ConvertPropertyValue(value, property.PropertyType);
                property.SetValue(obj, convertedValue);
                return;
            }
            
            // Try field (including private fields with [SerializeField] attribute)
            var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                // For private fields, require SerializeField attribute
                if (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null)
                {
                    throw new InvalidOperationException($"Private field '{propertyName}' on type {type.Name} is not marked with [SerializeField]");
                }
                
                var convertedValue = ConvertPropertyValue(value, field.FieldType);
                field.SetValue(obj, convertedValue);
                return;
            }
            
            throw new InvalidOperationException($"Property or field '{propertyName}' not found on type {type.Name}");
        }
        
        /// <summary>
        /// Serializes ScriptableObject properties.
        /// </summary>
        private Dictionary<string, object> SerializeScriptableObjectProperties(ScriptableObject obj, List<string> propertyFilter)
        {
            var properties = new Dictionary<string, object>();
            var type = obj.GetType();
            
            // Serialize public properties
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead)
                    continue;
                
                if (propertyFilter != null && !propertyFilter.Contains(prop.Name))
                    continue;
                
                try
                {
                    var value = prop.GetValue(obj);
                    properties[prop.Name] = SerializePropertyValue(value);
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
                    var value = field.GetValue(obj);
                    properties[field.Name] = SerializePropertyValue(value);
                }
                catch
                {
                    // Skip fields that throw on access
                }
            }
            
            return properties;
        }
        
        /// <summary>
        /// Serializes a property value to a JSON-compatible type.
        /// Uses ValueConverterManager for consistent serialization.
        /// </summary>
        private object SerializePropertyValue(object value)
        {
            if (value == null)
                return null;

            // Handle Unity Object references specially (need asset path/guid)
            if (value is UnityEngine.Object unityObj)
            {
                if (unityObj == null)
                    return null;

                var assetPath = AssetDatabase.GetAssetPath(unityObj);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    return new Dictionary<string, object>
                    {
                        ["name"] = unityObj.name,
                        ["type"] = unityObj.GetType().Name,
                        ["assetPath"] = assetPath,
                        ["guid"] = AssetDatabase.AssetPathToGUID(assetPath)
                    };
                }
                return new Dictionary<string, object>
                {
                    ["name"] = unityObj.name,
                    ["type"] = unityObj.GetType().Name
                };
            }

            // Use ValueConverterManager for all other types
            return ValueConverterManager.Instance.Serialize(value);
        }

        /// <summary>
        /// Converts a value to the target type using ValueConverterManager.
        /// </summary>
        private object ConvertPropertyValue(object value, Type targetType)
        {
            return ValueConverterManager.Instance.Convert(value, targetType);
        }
        
        #endregion
    }
}

