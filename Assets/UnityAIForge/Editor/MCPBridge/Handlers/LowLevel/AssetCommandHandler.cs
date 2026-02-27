using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCP.Editor.Base;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Asset management command handler.
    /// Handles asset creation, updating, deletion, renaming, duplication, and inspection.
    /// Supports all asset types including C# scripts (.cs) with automatic compilation wait.
    /// </summary>
    public class AssetCommandHandler : BaseCommandHandler
    {
        #region ICommandHandler Implementation
        
        public override string Category => "assetManage";
        
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "create",
            "update",
            "updateImporter",
            "delete",
            "rename",
            "duplicate",
            "inspect",
            "findMultiple",
            "deleteMultiple",
            "inspectMultiple"
        };
        
        #endregion
        
        #region BaseCommandHandler Overrides
        
        protected override bool RequiresCompilationWait(string operation)
        {
            // Compilation wait is disabled to prevent Unity Editor from freezing.
            // Client-side should handle compilation completion detection if needed.
            return false;
        }
        
        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateAsset(payload),
                "update" => UpdateAsset(payload),
                "updateImporter" => UpdateAssetImporter(payload),
                "delete" => DeleteAsset(payload),
                "rename" => RenameAsset(payload),
                "duplicate" => DuplicateAsset(payload),
                "inspect" => InspectAsset(payload),
                "findMultiple" => FindMultipleAssets(payload),
                "deleteMultiple" => DeleteMultipleAssets(payload),
                "inspectMultiple" => InspectMultipleAssets(payload),
                _ => throw new InvalidOperationException($"Unknown asset operation: {operation}")
            };
        }
        
        #endregion
        
        #region Asset Operations
        
        /// <summary>
        /// Creates a new text asset.
        /// </summary>
        private object CreateAsset(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            var content = GetString(payload, "content");
            
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("assetPath is required");
            }
            
            if (!ValidateAssetPath(assetPath))
            {
                throw new InvalidOperationException($"Invalid asset path: {assetPath}");
            }
            
            if (File.Exists(assetPath))
            {
                throw new InvalidOperationException($"Asset already exists: {assetPath}");
            }
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Write content
            File.WriteAllText(assetPath, content ?? string.Empty);
            AssetDatabase.ImportAsset(assetPath);
            AssetDatabase.Refresh(); // Trigger compilation if needed
            
            return CreateSuccessResponse(
                ("assetPath", assetPath),
                ("guid", AssetDatabase.AssetPathToGUID(assetPath)),
                ("message", "Asset created successfully")
            );
        }
        
        /// <summary>
        /// Updates an existing text asset.
        /// </summary>
        private object UpdateAsset(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            var content = GetString(payload, "content");
            
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("assetPath is required");
            }
            
            if (!File.Exists(assetPath))
            {
                throw new InvalidOperationException($"Asset does not exist: {assetPath}");
            }
            
            File.WriteAllText(assetPath, content ?? string.Empty);
            AssetDatabase.ImportAsset(assetPath);
            AssetDatabase.Refresh(); // Trigger compilation if needed
            
            return CreateSuccessResponse(
                ("assetPath", assetPath),
                ("message", "Asset updated successfully")
            );
        }
        
        /// <summary>
        /// Updates asset importer settings.
        /// </summary>
        private object UpdateAssetImporter(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("assetPath is required");
            }
            
            var importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null)
            {
                throw new InvalidOperationException($"Asset importer not found: {assetPath}");
            }
            
            var propertyChanges = payload["propertyChanges"] as Dictionary<string, object>;
            if (propertyChanges == null)
            {
                throw new InvalidOperationException("propertyChanges parameter is required");
            }
            
            var updated = new List<string>();
            
            foreach (var kvp in propertyChanges)
            {
                var propertyName = kvp.Key;
                var value = kvp.Value;
                
                var property = importer.GetType().GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(importer, value);
                    updated.Add(propertyName);
                }
            }
            
            importer.SaveAndReimport();
            
            return CreateSuccessResponse(
                ("assetPath", assetPath),
                ("updated", updated)
            );
        }
        
        /// <summary>
        /// Deletes an asset.
        /// </summary>
        private object DeleteAsset(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("assetPath is required");
            }
            
            if (!File.Exists(assetPath))
            {
                throw new InvalidOperationException($"Asset does not exist: {assetPath}");
            }
            
            if (!AssetDatabase.DeleteAsset(assetPath))
            {
                throw new InvalidOperationException($"Failed to delete asset: {assetPath}");
            }
            
            return CreateSuccessResponse(
                ("assetPath", assetPath),
                ("message", "Asset deleted successfully")
            );
        }
        
        /// <summary>
        /// Renames an asset.
        /// </summary>
        private object RenameAsset(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            var newName = GetString(payload, "destinationPath");
            
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("assetPath is required");
            }
            
            if (string.IsNullOrEmpty(newName))
            {
                throw new InvalidOperationException("destinationPath is required");
            }
            
            var errorMessage = AssetDatabase.RenameAsset(assetPath, Path.GetFileNameWithoutExtension(newName));
            if (!string.IsNullOrEmpty(errorMessage))
            {
                throw new InvalidOperationException($"Failed to rename asset: {errorMessage}");
            }
            
            return CreateSuccessResponse(
                ("oldPath", assetPath),
                ("newPath", newName)
            );
        }
        
        /// <summary>
        /// Duplicates an asset.
        /// </summary>
        private object DuplicateAsset(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            var destinationPath = GetString(payload, "destinationPath");
            
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("assetPath is required");
            }
            
            if (string.IsNullOrEmpty(destinationPath))
            {
                throw new InvalidOperationException("destinationPath is required");
            }
            
            if (!AssetDatabase.CopyAsset(assetPath, destinationPath))
            {
                throw new InvalidOperationException(
                    $"Failed to duplicate asset from {assetPath} to {destinationPath}"
                );
            }
            
            return CreateSuccessResponse(
                ("sourcePath", assetPath),
                ("destinationPath", destinationPath)
            );
        }
        
        /// <summary>
        /// Inspects an asset.
        /// </summary>
        private object InspectAsset(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("assetPath is required");
            }
            
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"Asset not found: {assetPath}");
            }
            
            var includeProperties = GetBool(payload, "includeProperties", false);
            
            var info = new Dictionary<string, object>
            {
                ["success"] = true,
                ["assetPath"] = assetPath,
                ["guid"] = AssetDatabase.AssetPathToGUID(assetPath),
                ["type"] = asset.GetType().FullName,
                ["name"] = asset.name
            };
            
            if (includeProperties)
            {
                var importer = AssetImporter.GetAtPath(assetPath);
                if (importer != null)
                {
                    info["importerType"] = importer.GetType().FullName;
                }
            }
            
            return info;
        }
        
        /// <summary>
        /// Finds multiple assets matching a pattern.
        /// </summary>
        private object FindMultipleAssets(Dictionary<string, object> payload)
        {
            var pattern = GetString(payload, "pattern");
            var maxResults = GetInt(payload, "maxResults", 1000);
            
            if (string.IsNullOrEmpty(pattern))
            {
                throw new InvalidOperationException("pattern is required");
            }
            
            var guids = AssetDatabase.FindAssets("", new[] { "Assets" });
            var results = new List<Dictionary<string, object>>();
            
            foreach (var guid in guids.Take(maxResults))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                
                // Simple pattern matching
                if (MatchesPattern(path, pattern))
                {
                    results.Add(new Dictionary<string, object>
                    {
                        ["assetPath"] = path,
                        ["guid"] = guid
                    });
                }
                
                if (results.Count >= maxResults)
                    break;
            }
            
            return CreateSuccessResponse(
                ("results", results),
                ("count", results.Count)
            );
        }
        
        /// <summary>
        /// Deletes multiple assets matching a pattern.
        /// </summary>
        private object DeleteMultipleAssets(Dictionary<string, object> payload)
        {
            var pattern = GetString(payload, "pattern");
            var maxResults = GetInt(payload, "maxResults", 1000);
            
            if (string.IsNullOrEmpty(pattern))
            {
                throw new InvalidOperationException("pattern is required");
            }
            
            var guids = AssetDatabase.FindAssets("", new[] { "Assets" });
            var deleted = new List<string>();
            
            foreach (var guid in guids.Take(maxResults))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                
                if (MatchesPattern(path, pattern))
                {
                    if (AssetDatabase.DeleteAsset(path))
                    {
                        deleted.Add(path);
                    }
                }
                
                if (deleted.Count >= maxResults)
                    break;
            }
            
            return CreateSuccessResponse(
                ("deleted", deleted),
                ("count", deleted.Count)
            );
        }
        
        /// <summary>
        /// Inspects multiple assets matching a pattern.
        /// </summary>
        private object InspectMultipleAssets(Dictionary<string, object> payload)
        {
            var pattern = GetString(payload, "pattern");
            var maxResults = GetInt(payload, "maxResults", 1000);
            var includeProperties = GetBool(payload, "includeProperties", false);
            
            if (string.IsNullOrEmpty(pattern))
            {
                throw new InvalidOperationException("pattern is required");
            }
            
            var guids = AssetDatabase.FindAssets("", new[] { "Assets" });
            var results = new List<Dictionary<string, object>>();
            
            foreach (var guid in guids.Take(maxResults))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                
                if (MatchesPattern(path, pattern))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    if (asset != null)
                    {
                        results.Add(new Dictionary<string, object>
                        {
                            ["assetPath"] = path,
                            ["guid"] = guid,
                            ["type"] = asset.GetType().FullName
                        });
                    }
                }
                
                if (results.Count >= maxResults)
                    break;
            }
            
            return CreateSuccessResponse(
                ("results", results),
                ("count", results.Count)
            );
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Simple pattern matching with wildcards.
        /// </summary>
        private bool MatchesPattern(string text, string pattern)
        {
            var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";
            
            return System.Text.RegularExpressions.Regex.IsMatch(text, regexPattern);
        }
        
        #endregion
    }
}

