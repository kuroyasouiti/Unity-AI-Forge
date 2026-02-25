using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;
using MCP.Editor.Utilities.GraphAnalysis;

namespace MCP.Editor.Handlers.HighLevel
{
    /// <summary>
    /// Handler for analyzing asset dependencies of Unity scenes.
    /// Uses AssetDatabase.GetDependencies() to discover which assets
    /// (materials, textures, prefabs, scripts, audio, etc.) a scene depends on.
    /// </summary>
    public class SceneDependencyHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "analyzeScene",
            "findAssetUsage",
            "findSharedAssets",
            "findUnusedAssets"
        };

        public override string Category => "sceneDependency";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "analyzeScene" => AnalyzeScene(payload),
                "findAssetUsage" => FindAssetUsage(payload),
                "findSharedAssets" => FindSharedAssets(payload),
                "findUnusedAssets" => FindUnusedAssets(payload),
                _ => throw new InvalidOperationException($"Unsupported scene dependency operation: {operation}"),
            };
        }

        #region Operations

        /// <summary>
        /// Analyze all asset dependencies of a scene, categorized by type.
        /// </summary>
        private object AnalyzeScene(Dictionary<string, object> payload)
        {
            var scenePath = GetString(payload, "scenePath");
            if (string.IsNullOrEmpty(scenePath))
            {
                throw new ArgumentException("scenePath is required for analyzeScene operation");
            }

            var includeIndirect = GetBool(payload, "includeIndirect", true);
            var typeFilter = GetString(payload, "typeFilter");

            var analyzer = new SceneDependencyAnalyzer();
            var result = analyzer.AnalyzeScene(scenePath, includeIndirect, typeFilter);
            result["success"] = true;
            return result;
        }

        /// <summary>
        /// Find all scenes that depend on a specific asset.
        /// </summary>
        private object FindAssetUsage(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new ArgumentException("assetPath is required for findAssetUsage operation");
            }

            var searchPath = GetString(payload, "searchPath");

            var analyzer = new SceneDependencyAnalyzer();
            var result = analyzer.FindAssetUsage(assetPath, searchPath);
            result["success"] = true;
            return result;
        }

        /// <summary>
        /// Find assets shared across multiple scenes.
        /// </summary>
        private object FindSharedAssets(Dictionary<string, object> payload)
        {
            var minSharedCount = GetInt(payload, "minSharedCount", 2);
            var typeFilter = GetString(payload, "typeFilter");
            var scenePathsList = GetStringList(payload, "scenePaths");
            var scenePaths = scenePathsList?.ToArray();

            var analyzer = new SceneDependencyAnalyzer();
            var result = analyzer.FindSharedAssets(scenePaths, minSharedCount, typeFilter);
            result["success"] = true;
            return result;
        }

        /// <summary>
        /// Find assets not referenced by any scene.
        /// </summary>
        private object FindUnusedAssets(Dictionary<string, object> payload)
        {
            var searchPath = GetString(payload, "searchPath", "Assets");
            var typeFilter = GetString(payload, "typeFilter");

            var analyzer = new SceneDependencyAnalyzer();
            var result = analyzer.FindUnusedAssets(searchPath, typeFilter);
            result["success"] = true;
            return result;
        }

        #endregion
    }
}
