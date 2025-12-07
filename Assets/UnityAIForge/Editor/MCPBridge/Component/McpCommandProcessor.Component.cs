using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor
{
    internal static partial class McpCommandProcessor
    {
        #region Component Management

        /// <summary>
        /// Handles component management operations (add, remove, update, inspect).
        /// Uses reflection to set component properties from the payload.
        /// Monitors compilation status and returns whether compilation was triggered.
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation', 'gameObjectPath', 'componentType', and optional 'propertyChanges'.</param>
        /// <returns>Result dictionary with component type, GameObject path, and compilation status.</returns>
        /// <exception cref="InvalidOperationException">Thrown when GameObject or component type is not found.</exception>
        private static object HandleComponentManage(Dictionary<string, object> payload)
        {
            var operation = EnsureValue(GetString(payload, "operation"), "operation");

            // Check if compilation is in progress and wait if necessary (only for non-inspect operations)
            Dictionary<string, object> compilationWaitInfo = null;
            if (operation != "inspect" && operation != "inspectMultiple")
            {
                compilationWaitInfo = EnsureNoCompilationInProgress("componentManage", maxWaitSeconds: 30f);
            }

            // Record compilation state before operation
            var wasCompiling = EditorApplication.isCompiling;

            // Execute the operation
            var result = operation switch
            {
                "add" => AddComponent(payload),
                "remove" => RemoveComponent(payload),
                "update" => UpdateComponent(payload),
                "inspect" => InspectComponent(payload),
                "addMultiple" => AddMultipleComponents(payload),
                "removeMultiple" => RemoveMultipleComponents(payload),
                "updateMultiple" => UpdateMultipleComponents(payload),
                "inspectMultiple" => InspectMultipleComponents(payload),
                _ => throw new InvalidOperationException($"Unknown componentManage operation: {operation}"),
            };

            // Detect if compilation was triggered (only for operations that modify state)
            var compilationTriggered = false;
            if (operation != "inspect" && operation != "inspectMultiple")
            {
                compilationTriggered = DetectCompilationStart(wasCompiling, maxWaitSeconds: 1.0f);
            }

            // Add compilation status to result
            if (result is Dictionary<string, object> resultDict)
            {
                resultDict["compilationTriggered"] = compilationTriggered;
                if (compilationWaitInfo != null)
                {
                    resultDict["compilationWait"] = compilationWaitInfo;
                }
                return resultDict;
            }

            // If result is not a dictionary, wrap it
            var wrappedResult = new Dictionary<string, object>
            {
                ["result"] = result,
                ["compilationTriggered"] = compilationTriggered
            };
            if (compilationWaitInfo != null)
            {
                wrappedResult["compilationWait"] = compilationWaitInfo;
            }
            return wrappedResult;
        }

        private static object AddComponent(Dictionary<string, object> payload)
        {
            var go = ResolveGameObjectFromPayload(payload);
            var type = ResolveType(EnsureValue(GetString(payload, "componentType"), "componentType"));

            var component = go.GetComponent(type);
            if (component == null)
            {
                component = go.AddComponent(type);
                if (component == null)
                {
                    throw new InvalidOperationException($"Failed to add component {type.FullName} to {go.name}");
                }
            }

            EditorUtility.SetDirty(go);
            return DescribeComponent(component);
        }

        private static object RemoveComponent(Dictionary<string, object> payload)
        {
            var go = ResolveGameObjectFromPayload(payload);
            var type = ResolveType(EnsureValue(GetString(payload, "componentType"), "componentType"));
            var component = go.GetComponent(type);
            if (component == null)
            {
                throw new InvalidOperationException($"Component {type.FullName} not found on {go.name}");
            }

            UnityEngine.Object.DestroyImmediate(component, true);
            return new Dictionary<string, object>
            {
                ["removed"] = type.FullName,
            };
        }

        private static object UpdateComponent(Dictionary<string, object> payload)
        {
            var go = ResolveGameObjectFromPayload(payload);
            var type = ResolveType(EnsureValue(GetString(payload, "componentType"), "componentType"));
            var component = go.GetComponent(type);
            if (component == null)
            {
                throw new InvalidOperationException($"Component {type.FullName} not found on {go.name}");
            }

            var updatedProperties = new List<string>();
            var failedProperties = new Dictionary<string, string>();

            if (payload.TryGetValue("propertyChanges", out var propertyObj) && propertyObj is Dictionary<string, object> propertyChanges)
            {
                foreach (var kvp in propertyChanges)
                {
                    try
                    {
                        ApplyProperty(component, kvp.Key, kvp.Value);
                        updatedProperties.Add(kvp.Key);
                    }
                    catch (Exception ex)
                    {
                        failedProperties[kvp.Key] = ex.Message;
                        Debug.LogWarning($"[MCP] Failed to update property '{kvp.Key}': {ex.Message}");
                    }
                }
            }

            EditorUtility.SetDirty(component);

            var result = DescribeComponent(component);
            result["success"] = true;
            result["updatedProperties"] = updatedProperties;
            
            if (failedProperties.Count > 0)
            {
                result["failedProperties"] = failedProperties;
                result["partialSuccess"] = true;
            }
            
            return result;
        }

        private static object InspectComponent(Dictionary<string, object> payload)
        {
            var go = ResolveGameObjectFromPayload(payload);
            var type = ResolveType(EnsureValue(GetString(payload, "componentType"), "componentType"));
            var component = go.GetComponent(type);
            if (component == null)
            {
                throw new InvalidOperationException($"Component {type.FullName} not found on {go.name}");
            }

            var result = new Dictionary<string, object>
            {
                ["gameObject"] = GetHierarchyPath(go),
                ["type"] = component.GetType().FullName,
            };

            // Check if properties should be included (default: true)
            var includeProperties = true;
            if (payload.TryGetValue("includeProperties", out var includePropObj))
            {
                includeProperties = Convert.ToBoolean(includePropObj);
            }

            // Only include properties if requested
            if (includeProperties)
            {
                var properties = new Dictionary<string, object>();
                var componentType = component.GetType();

                // Get property filter if specified
                HashSet<string> propertyFilter = null;
                if (payload.TryGetValue("propertyFilter", out var filterObj) && filterObj is List<object> filterList)
                {
                    propertyFilter = new HashSet<string>(filterList.Select(f => f.ToString()));
                }

                // Properties that cause memory leaks in edit mode
                var dangerousProperties = new HashSet<string>
                {
                    "mesh",      // Use sharedMesh instead
                    "material",  // Use sharedMaterial instead
                    "materials", // Use sharedMaterials instead
                };

                // Get all public properties
                var propertyInfos = componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in propertyInfos)
                {
                    if (!prop.CanRead)
                    {
                        continue;
                    }

                    // Skip if property filter is specified and this property doesn't match
                    if (propertyFilter != null && !propertyFilter.Contains(prop.Name))
                    {
                        continue;
                    }

                    // Skip dangerous properties that cause memory leaks
                    if (dangerousProperties.Contains(prop.Name))
                    {
                        properties[prop.Name] = $"<Skipped:{prop.Name}:UseSharedVersion>";
                        continue;
                    }

                    try
                    {
                        var value = prop.GetValue(component);
                        properties[prop.Name] = SerializeValue(value);
                    }
                    catch (Exception ex)
                    {
                        properties[prop.Name] = $"<Error: {ex.Message}>";
                    }
                }

                // Get all public fields and private fields with [SerializeField] attribute
                // When propertyFilter is specified, skip internal fields (starting with m_ or _)
                var fieldInfos = componentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null);
                foreach (var field in fieldInfos)
                {
                    // If propertyFilter is specified, skip internal fields and only include filtered fields
                    if (propertyFilter != null)
                    {
                        // Skip internal fields (Unity convention: m_ or _ prefix)
                        if (field.Name.StartsWith("m_") || field.Name.StartsWith("_"))
                        {
                            continue;
                        }
                        
                        // Skip if this field doesn't match the filter
                        if (!propertyFilter.Contains(field.Name))
                        {
                            continue;
                        }
                    }

                    try
                    {
                        var value = field.GetValue(component);
                        properties[field.Name] = SerializeValue(value);
                    }
                    catch (Exception ex)
                    {
                        properties[field.Name] = $"<Error: {ex.Message}>";
                    }
                }

                result["properties"] = properties;
            }

            return result;
        }

        private static object AddMultipleComponents(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");
            var componentTypeName = EnsureValue(GetString(payload, "componentType"), "componentType");
            var type = ResolveType(componentTypeName);
            var maxResults = GetInt(payload, "maxResults", defaultValue: 1000);
            var stopOnError = GetBool(payload, "stopOnError");

            var gameObjects = McpWildcardUtility.ResolveGameObjects(pattern, useRegex);
            var totalCount = gameObjects.Count;

            // Limit results to prevent timeout
            if (gameObjects.Count > maxResults)
            {
                gameObjects = gameObjects.Take(maxResults).ToList();
            }

            var results = new List<Dictionary<string, object>>();
            var errors = new List<Dictionary<string, object>>();

            // Get property changes if specified
            var propertyChanges = payload.ContainsKey("propertyChanges") && payload["propertyChanges"] is Dictionary<string, object> changes
                ? changes
                : new Dictionary<string, object>();

            foreach (var go in gameObjects)
            {
                try
                {
                    // Check if component already exists
                    var existingComponent = go.GetComponent(type);
                    if (existingComponent != null)
                    {
                        if (stopOnError)
                        {
                            throw new InvalidOperationException($"Component {type.FullName} already exists on {GetHierarchyPath(go)}");
                        }

                        errors.Add(new Dictionary<string, object>
                        {
                            ["gameObject"] = GetHierarchyPath(go),
                            ["error"] = $"Component {type.FullName} already exists",
                        });
                        continue;
                    }

                    var component = go.AddComponent(type);
                    if (component == null)
                    {
                        throw new InvalidOperationException($"Failed to add component {type.FullName} to {GetHierarchyPath(go)}");
                    }

                    var appliedProperties = new List<string>();
                    var failedProperties = new Dictionary<string, string>();

                    // Apply initial property changes if specified
                    foreach (var kvp in propertyChanges)
                    {
                        try
                        {
                            ApplyProperty(component, kvp.Key, kvp.Value);
                            appliedProperties.Add(kvp.Key);
                        }
                        catch (Exception ex)
                        {
                            failedProperties[kvp.Key] = ex.Message;
                            Debug.LogWarning($"[MCP] Failed to apply initial property '{kvp.Key}' on {GetHierarchyPath(go)}: {ex.Message}");
                        }
                    }

                    EditorUtility.SetDirty(go);

                    var resultItem = new Dictionary<string, object>
                    {
                        ["gameObject"] = GetHierarchyPath(go),
                        ["type"] = type.FullName,
                        ["success"] = true,
                    };

                    if (appliedProperties.Count > 0)
                    {
                        resultItem["appliedProperties"] = appliedProperties;
                    }

                    if (failedProperties.Count > 0)
                    {
                        resultItem["failedProperties"] = failedProperties;
                        resultItem["partialSuccess"] = true;
                    }

                    results.Add(resultItem);
                }
                catch (Exception ex)
                {
                    errors.Add(new Dictionary<string, object>
                    {
                        ["gameObject"] = GetHierarchyPath(go),
                        ["error"] = ex.Message,
                    });

                    if (stopOnError)
                    {
                        break;
                    }
                }
            }

            return new Dictionary<string, object>
            {
                ["pattern"] = pattern,
                ["componentType"] = type.FullName,
                ["totalCount"] = totalCount,
                ["successCount"] = results.Count,
                ["errorCount"] = errors.Count,
                ["truncated"] = totalCount > maxResults,
                ["results"] = results,
                ["errors"] = errors,
            };
        }

        private static object RemoveMultipleComponents(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");
            var componentTypeName = EnsureValue(GetString(payload, "componentType"), "componentType");
            var type = ResolveType(componentTypeName);
            var maxResults = GetInt(payload, "maxResults", defaultValue: 1000);
            var stopOnError = GetBool(payload, "stopOnError");

            var gameObjects = McpWildcardUtility.ResolveGameObjects(pattern, useRegex);
            var totalCount = gameObjects.Count;

            // Limit results to prevent timeout
            if (gameObjects.Count > maxResults)
            {
                gameObjects = gameObjects.Take(maxResults).ToList();
            }

            var results = new List<Dictionary<string, object>>();
            var errors = new List<Dictionary<string, object>>();

            foreach (var go in gameObjects)
            {
                try
                {
                    var component = go.GetComponent(type);
                    if (component != null)
                    {
                        UnityEngine.Object.DestroyImmediate(component);
                        results.Add(new Dictionary<string, object>
                        {
                            ["gameObject"] = GetHierarchyPath(go),
                            ["type"] = type.FullName,
                            ["removed"] = true,
                        });
                    }
                    else
                    {
                        // Component not found - add to errors if stopOnError is true
                        if (stopOnError)
                        {
                            throw new InvalidOperationException($"Component {type.FullName} not found on {GetHierarchyPath(go)}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new Dictionary<string, object>
                    {
                        ["gameObject"] = GetHierarchyPath(go),
                        ["error"] = ex.Message,
                    });

                    if (stopOnError)
                    {
                        break;
                    }
                }
            }

            return new Dictionary<string, object>
            {
                ["pattern"] = pattern,
                ["componentType"] = type.FullName,
                ["totalCount"] = totalCount,
                ["successCount"] = results.Count,
                ["errorCount"] = errors.Count,
                ["truncated"] = totalCount > maxResults,
                ["results"] = results,
                ["errors"] = errors,
            };
        }

        private static object UpdateMultipleComponents(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");
            var componentTypeName = EnsureValue(GetString(payload, "componentType"), "componentType");
            var type = ResolveType(componentTypeName);
            var maxResults = GetInt(payload, "maxResults", defaultValue: 1000);
            var stopOnError = GetBool(payload, "stopOnError");
            var propertyChanges = payload.ContainsKey("propertyChanges") && payload["propertyChanges"] is Dictionary<string, object> changes
                ? changes
                : new Dictionary<string, object>();

            var gameObjects = McpWildcardUtility.ResolveGameObjects(pattern, useRegex);
            var totalCount = gameObjects.Count;

            // Limit results to prevent timeout
            if (gameObjects.Count > maxResults)
            {
                gameObjects = gameObjects.Take(maxResults).ToList();
            }

            var results = new List<Dictionary<string, object>>();
            var errors = new List<Dictionary<string, object>>();

            foreach (var go in gameObjects)
            {
                try
                {
                    var component = go.GetComponent(type);
                    if (component != null)
                    {
                        var updatedProperties = new List<string>();
                        var failedProperties = new Dictionary<string, string>();

                        // Apply each property change using existing ApplyProperty method
                        foreach (var kvp in propertyChanges)
                        {
                            try
                            {
                                ApplyProperty(component, kvp.Key, kvp.Value);
                                updatedProperties.Add(kvp.Key);
                            }
                            catch (Exception ex)
                            {
                                failedProperties[kvp.Key] = ex.Message;
                                Debug.LogWarning($"[MCP] Failed to update property '{kvp.Key}' on {GetHierarchyPath(go)}: {ex.Message}");
                            }
                        }

                        EditorUtility.SetDirty(component);

                        var resultItem = new Dictionary<string, object>
                        {
                            ["gameObject"] = GetHierarchyPath(go),
                            ["type"] = type.FullName,
                            ["updated"] = true,
                            ["updatedProperties"] = updatedProperties,
                        };

                        if (failedProperties.Count > 0)
                        {
                            resultItem["failedProperties"] = failedProperties;
                            resultItem["partialSuccess"] = true;
                        }

                        results.Add(resultItem);
                    }
                    else
                    {
                        // Component not found
                        if (stopOnError)
                        {
                            throw new InvalidOperationException($"Component {type.FullName} not found on {GetHierarchyPath(go)}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new Dictionary<string, object>
                    {
                        ["gameObject"] = GetHierarchyPath(go),
                        ["error"] = ex.Message,
                    });

                    if (stopOnError)
                    {
                        break;
                    }
                }
            }

            return new Dictionary<string, object>
            {
                ["pattern"] = pattern,
                ["componentType"] = type.FullName,
                ["totalCount"] = totalCount,
                ["successCount"] = results.Count,
                ["errorCount"] = errors.Count,
                ["truncated"] = totalCount > maxResults,
                ["results"] = results,
                ["errors"] = errors,
            };
        }

        private static object InspectMultipleComponents(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");
            var componentTypeName = EnsureValue(GetString(payload, "componentType"), "componentType");
            var type = ResolveType(componentTypeName);
            var maxResults = GetInt(payload, "maxResults", defaultValue: 1000);

            // Check if properties should be included (default: true)
            var includeProperties = true;
            if (payload.TryGetValue("includeProperties", out var includePropObj))
            {
                includeProperties = Convert.ToBoolean(includePropObj);
            }

            // Get property filter if specified
            HashSet<string> propertyFilter = null;
            if (payload.TryGetValue("propertyFilter", out var filterObj) && filterObj is List<object> filterList)
            {
                propertyFilter = new HashSet<string>(filterList.Select(f => f.ToString()));
            }

            var gameObjects = McpWildcardUtility.ResolveGameObjects(pattern, useRegex);
            var totalCount = gameObjects.Count;

            // Limit results to prevent timeout
            if (gameObjects.Count > maxResults)
            {
                gameObjects = gameObjects.Take(maxResults).ToList();
            }

            var results = new List<Dictionary<string, object>>();

            // Properties that cause memory leaks in edit mode
            var dangerousProperties = new HashSet<string>
            {
                "mesh",      // Use sharedMesh instead
                "material",  // Use sharedMaterial instead
                "materials", // Use sharedMaterials instead
            };

            foreach (var go in gameObjects)
            {
                var component = go.GetComponent(type);
                if (component != null)
                {
                    var resultData = new Dictionary<string, object>
                    {
                        ["gameObject"] = GetHierarchyPath(go),
                        ["type"] = component.GetType().FullName,
                    };

                    // Only include properties if requested
                    if (includeProperties)
                    {
                        var properties = new Dictionary<string, object>();
                        var componentType = component.GetType();

                        // Get all public properties
                        var propertyInfos = componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var prop in propertyInfos)
                        {
                            if (!prop.CanRead)
                            {
                                continue;
                            }

                            // Skip if property filter is specified and this property doesn't match
                            if (propertyFilter != null && !propertyFilter.Contains(prop.Name))
                            {
                                continue;
                            }

                            // Skip dangerous properties that cause memory leaks
                            if (dangerousProperties.Contains(prop.Name))
                            {
                                properties[prop.Name] = $"<Skipped:{prop.Name}:UseSharedVersion>";
                                continue;
                            }

                            try
                            {
                                var value = prop.GetValue(component);
                                properties[prop.Name] = SerializeValue(value);
                            }
                            catch (Exception ex)
                            {
                                properties[prop.Name] = $"<Error: {ex.Message}>";
                            }
                        }

                        // Get all public fields and private fields with [SerializeField] attribute
                        // When propertyFilter is specified, skip internal fields (starting with m_ or _)
                        var fieldInfos = componentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null);
                        foreach (var field in fieldInfos)
                        {
                            // If propertyFilter is specified, skip internal fields and only include filtered fields
                            if (propertyFilter != null)
                            {
                                // Skip internal fields (Unity convention: m_ or _ prefix)
                                if (field.Name.StartsWith("m_") || field.Name.StartsWith("_"))
                                {
                                    continue;
                                }
                                
                                // Skip if this field doesn't match the filter
                                if (!propertyFilter.Contains(field.Name))
                                {
                                    continue;
                                }
                            }

                            try
                            {
                                var value = field.GetValue(component);
                                properties[field.Name] = SerializeValue(value);
                            }
                            catch (Exception ex)
                            {
                                properties[field.Name] = $"<Error: {ex.Message}>";
                            }
                        }

                        resultData["properties"] = properties;
                    }

                    results.Add(resultData);
                }
            }

            return new Dictionary<string, object>
            {
                ["pattern"] = pattern,
                ["componentType"] = type.FullName,
                ["totalCount"] = totalCount,
                ["returnedCount"] = results.Count,
                ["truncated"] = totalCount > maxResults,
                ["results"] = results,
            };
        }

        #endregion
    }
}

