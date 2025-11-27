using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor
{
    internal static partial class McpCommandProcessor
    {
        #region Asset Management

        /// <summary>
        /// Handles asset management operations (create, update, updateImporter, delete, rename, duplicate, inspect).
        /// Note: C# script files (.cs) cannot be created/updated through this tool - use code editor tools instead.
        /// Supports JSON, XML, TXT, and other text-based asset files.
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation' and asset-specific settings.</param>
        /// <returns>Result dictionary with asset information.</returns>
        private static object HandleAssetManage(Dictionary<string, object> payload)
        {
            var operation = EnsureValue(GetString(payload, "operation"), "operation");

            // Check if compilation is in progress and wait if necessary (except for read-only operations)
            Dictionary<string, object> compilationWaitInfo = null;
            if (operation != "inspect" && operation != "findMultiple" && operation != "inspectMultiple")
            {
                compilationWaitInfo = EnsureNoCompilationInProgress("assetManage", maxWaitSeconds: 30f);
            }

            var result = operation switch
            {
                "create" => CreateTextAsset(payload),
                "update" => UpdateTextAsset(payload),
                "updateImporter" => UpdateAssetImporter(payload),
                "delete" => DeleteAsset(payload),
                "rename" => RenameAsset(payload),
                "duplicate" => DuplicateAsset(payload),
                "inspect" => InspectAsset(payload),
                "findMultiple" => FindMultipleAssets(payload),
                "deleteMultiple" => DeleteMultipleAssets(payload),
                "inspectMultiple" => InspectMultipleAssets(payload),
                _ => throw new InvalidOperationException($"Unknown assetManage operation: {operation}"),
            };

            // Add compilation wait info if we waited
            if (compilationWaitInfo != null && result is Dictionary<string, object> resultDict)
            {
                resultDict["compilationWait"] = compilationWaitInfo;
            }

            return result;
        }

        private static object CreateTextAsset(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "assetPath"), "assetPath");
            var content = EnsureValue(GetString(payload, "content"), "content");

            // Reject C# script files - these should be created using code editor tools
            if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Cannot create .cs files using assetManage. Use designPatternGenerate, scriptTemplateGenerate, or code editor's file creation tools instead.");
            }

            // Ensure parent directory exists
            EnsureDirectoryExists(path);

            // Check if file already exists
            if (File.Exists(path))
            {
                throw new InvalidOperationException($"Asset already exists: {path}. Use 'update' operation to modify existing assets.");
            }

            // Write content to file
            File.WriteAllText(path, content, System.Text.Encoding.UTF8);

            // Import the asset
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh();

            return new Dictionary<string, object>
            {
                ["created"] = path,
                ["guid"] = AssetDatabase.AssetPathToGUID(path),
                ["size"] = new FileInfo(path).Length,
            };
        }

        private static object UpdateTextAsset(Dictionary<string, object> payload)
        {
            var path = ResolveAssetPathFromPayload(payload);
            var content = EnsureValue(GetString(payload, "content"), "content");

            // Reject C# script files - these should be edited using code editor tools
            if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Cannot update .cs files using assetManage. Use code editor's file editing tools (search_replace, write, etc.) instead.");
            }

            // Check if file exists
            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"Asset not found: {path}. Use 'create' operation for new assets.");
            }

            // Write content to file
            File.WriteAllText(path, content, System.Text.Encoding.UTF8);

            // Reimport the asset
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh();

            return new Dictionary<string, object>
            {
                ["updated"] = path,
                ["guid"] = AssetDatabase.AssetPathToGUID(path),
                ["size"] = new FileInfo(path).Length,
            };
        }

        private static object UpdateAssetImporter(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "assetPath"), "assetPath");
            var assetTypeName = EnsureValue(GetString(payload, "assetType"), "assetType");

            EnsureDirectoryExists(path);

            var assetType = ResolveType(assetTypeName);

            // Check if it's a ScriptableObject
            if (assetType.IsSubclassOf(typeof(ScriptableObject)))
            {
                var instance = ScriptableObject.CreateInstance(assetType);

                // Apply property changes if provided
                if (payload.TryGetValue("propertyChanges", out var propertyObj) && propertyObj is Dictionary<string, object> propertyChanges)
                {
                    foreach (var kvp in propertyChanges)
                    {
                        ApplyProperty(instance, kvp.Key, kvp.Value);
                    }
                }

                AssetDatabase.CreateAsset(instance, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return DescribeAsset(path);
            }
            else
            {
                throw new InvalidOperationException($"Asset type {assetTypeName} is not a ScriptableObject. Only ScriptableObject creation is supported. Use Edit/Write tools for script files.");
            }
        }

        private static object UpdateAsset(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "assetPath"), "assetPath");

            // Load the asset
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Asset not found or cannot be loaded: {path}");
            }

            // Apply property changes
            if (payload.TryGetValue("propertyChanges", out var propertyObj) && propertyObj is Dictionary<string, object> propertyChanges)
            {
                foreach (var kvp in propertyChanges)
                {
                    ApplyProperty(asset, kvp.Key, kvp.Value);
                }
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return DescribeAsset(path);
        }

        private static object DeleteAsset(Dictionary<string, object> payload)
        {
            var path = ResolveAssetPathFromPayload(payload);
            if (!AssetDatabase.DeleteAsset(path))
            {
                throw new InvalidOperationException($"Failed to delete asset: {path}");
            }

            AssetDatabase.Refresh();
            return new Dictionary<string, object>
            {
                ["deleted"] = path,
            };
        }

        private static object RenameAsset(Dictionary<string, object> payload)
        {
            var path = ResolveAssetPathFromPayload(payload);
            var destination = EnsureValue(GetString(payload, "destinationPath"), "destinationPath");
            var result = AssetDatabase.MoveAsset(path, destination);
            if (!string.IsNullOrEmpty(result))
            {
                throw new InvalidOperationException(result);
            }

            AssetDatabase.Refresh();
            return DescribeAsset(destination);
        }

        private static object DuplicateAsset(Dictionary<string, object> payload)
        {
            var path = ResolveAssetPathFromPayload(payload);
            var destination = EnsureValue(GetString(payload, "destinationPath"), "destinationPath");
            EnsureDirectoryExists(destination);
            if (!AssetDatabase.CopyAsset(path, destination))
            {
                throw new InvalidOperationException($"Failed to duplicate asset {path}");
            }

            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
            return DescribeAsset(destination);
        }

        private static object InspectAsset(Dictionary<string, object> payload)
        {
            var path = ResolveAssetPathFromPayload(payload);
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                throw new InvalidOperationException($"Asset not found: {path}");
            }

            var includeProperties = GetBool(payload, "includeProperties");
            var guid = AssetDatabase.AssetPathToGUID(path);
            var mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(path);
            var assetObj = AssetDatabase.LoadMainAssetAtPath(path);

            var result = new Dictionary<string, object>
            {
                ["path"] = path,
                ["guid"] = guid,
                ["type"] = mainAssetType?.FullName,
                ["exists"] = assetObj != null,
            };

            if (assetObj != null && includeProperties)
            {
                var properties = new Dictionary<string, object>();
                var assetType = assetObj.GetType();

                // Get all public properties
                var propertyInfos = assetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in propertyInfos)
                {
                    if (!prop.CanRead)
                    {
                        continue;
                    }

                    try
                    {
                        var value = prop.GetValue(assetObj);
                        properties[prop.Name] = SerializeValue(value);
                    }
                    catch (Exception ex)
                    {
                        properties[prop.Name] = $"<Error: {ex.Message}>";
                    }
                }

                // Get all public fields
                var fieldInfos = assetType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fieldInfos)
                {
                    try
                    {
                        var value = field.GetValue(assetObj);
                        properties[field.Name] = SerializeValue(value);
                    }
                    catch (Exception ex)
                    {
                        properties[field.Name] = $"<Error: {ex.Message}>";
                    }
                }

                result["properties"] = properties;
            }

            // Include AssetImporter properties if requested
            if (includeProperties)
            {
                var importer = AssetImporter.GetAtPath(path);
                if (importer != null)
                {
                    var importerProperties = new Dictionary<string, object>();
                    var importerType = importer.GetType();

                    // Get all public properties
                    var propertyInfos = importerType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in propertyInfos)
                    {
                        if (!prop.CanRead)
                        {
                            continue;
                        }

                        try
                        {
                            var value = prop.GetValue(importer);
                            importerProperties[prop.Name] = SerializeValue(value);
                        }
                        catch (Exception ex)
                        {
                            importerProperties[prop.Name] = $"<Error: {ex.Message}>";
                        }
                    }

                    result["importerType"] = importerType.FullName;
                    result["importerProperties"] = importerProperties;
                }
            }

            return result;
        }

        private static object FindMultipleAssets(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");

            var assetPaths = McpWildcardUtility.ResolveAssetPaths(pattern, useRegex);

            var results = new List<Dictionary<string, object>>();
            foreach (var path in assetPaths)
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                var mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(path);

                results.Add(new Dictionary<string, object>
                {
                    ["path"] = path,
                    ["guid"] = guid,
                    ["type"] = mainAssetType?.FullName,
                });
            }

            return new Dictionary<string, object>
            {
                ["pattern"] = pattern,
                ["count"] = results.Count,
                ["assets"] = results,
            };
        }

        private static object DeleteMultipleAssets(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");

            var assetPaths = McpWildcardUtility.ResolveAssetPaths(pattern, useRegex);
            var deletedPaths = new List<string>();

            foreach (var path in assetPaths)
            {
                if (AssetDatabase.DeleteAsset(path))
                {
                    deletedPaths.Add(path);
                }
            }

            AssetDatabase.Refresh();

            return new Dictionary<string, object>
            {
                ["pattern"] = pattern,
                ["count"] = deletedPaths.Count,
                ["deleted"] = deletedPaths,
            };
        }

        private static object InspectMultipleAssets(Dictionary<string, object> payload)
        {
            var pattern = EnsureValue(GetString(payload, "pattern"), "pattern");
            var useRegex = GetBool(payload, "useRegex");
            var includeProperties = GetBool(payload, "includeProperties");

            var assetPaths = McpWildcardUtility.ResolveAssetPaths(pattern, useRegex);

            var results = new List<Dictionary<string, object>>();
            foreach (var path in assetPaths)
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                var mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                var assetObj = AssetDatabase.LoadMainAssetAtPath(path);

                var assetData = new Dictionary<string, object>
                {
                    ["path"] = path,
                    ["guid"] = guid,
                    ["type"] = mainAssetType?.FullName,
                    ["exists"] = assetObj != null,
                };

                if (includeProperties && assetObj != null)
                {
                    var properties = new Dictionary<string, object>();
                    var assetType = assetObj.GetType();

                    // Get all public properties
                    var propertyInfos = assetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in propertyInfos)
                    {
                        if (!prop.CanRead)
                        {
                            continue;
                        }

                        try
                        {
                            var value = prop.GetValue(assetObj);
                            properties[prop.Name] = SerializeValue(value);
                        }
                        catch (Exception ex)
                        {
                            properties[prop.Name] = $"<Error: {ex.Message}>";
                        }
                    }

                    // Get all public fields
                    var fieldInfos = assetType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var field in fieldInfos)
                    {
                        try
                        {
                            var value = field.GetValue(assetObj);
                            properties[field.Name] = SerializeValue(value);
                        }
                        catch (Exception ex)
                        {
                            properties[field.Name] = $"<Error: {ex.Message}>";
                        }
                    }

                    assetData["properties"] = properties;
                }

                results.Add(assetData);
            }

            return new Dictionary<string, object>
            {
                ["pattern"] = pattern,
                ["count"] = results.Count,
                ["assets"] = results,
            };
        }

        #endregion
    }
}

