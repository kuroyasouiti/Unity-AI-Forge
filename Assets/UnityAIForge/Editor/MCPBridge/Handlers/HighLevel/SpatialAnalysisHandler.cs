using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using MCP.Editor.Utilities.GraphAnalysis;
using UnityEngine;

namespace MCP.Editor.Handlers.HighLevel
{
    /// <summary>
    /// Handler for analyzing 3D spatial layout using the rule of thirds (3×3×3 grid).
    /// Detects Collider/Rigidbody distribution and layout bias in the scene.
    /// </summary>
    public class SpatialAnalysisHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "analyzeLayout",
            "inspectCell"
        };

        public override string Category => "spatialAnalysis";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "analyzeLayout" => AnalyzeLayout(payload),
                "inspectCell" => InspectCell(payload),
                _ => throw new InvalidOperationException($"Unsupported spatial analysis operation: {operation}"),
            };
        }

        #region Operations

        private object AnalyzeLayout(Dictionary<string, object> payload)
        {
            var analyzer = new SpatialAnalyzer();
            var result = analyzer.AnalyzeLayout(
                rootPath: GetString(payload, "rootPath"),
                targetTag: GetString(payload, "targetTag"),
                targetLayer: GetString(payload, "targetLayer"),
                includeKinematic: GetBool(payload, "includeKinematic", true),
                include2D: GetBool(payload, "include2D", false),
                includeTriggers: GetBool(payload, "includeTriggers", true),
                detectionMode: GetString(payload, "detectionMode") ?? "collider",
                customMin: GetOptionalVector3(payload, "customMin"),
                customMax: GetOptionalVector3(payload, "customMax")
            );

            var dict = result.ToDictionary();
            dict["success"] = true;
            dict["operation"] = "analyzeLayout";
            return dict;
        }

        private object InspectCell(Dictionary<string, object> payload)
        {
            int cellX = GetInt(payload, "cellX", -1);
            int cellY = GetInt(payload, "cellY", -1);
            int cellZ = GetInt(payload, "cellZ", -1);

            if (cellX < 0 || cellX > 2 || cellY < 0 || cellY > 2 || cellZ < 0 || cellZ > 2)
            {
                throw new ArgumentException(
                    $"cellX, cellY, cellZ are required and must be 0-2. Got ({cellX}, {cellY}, {cellZ}).");
            }

            var analyzer = new SpatialAnalyzer();
            var cell = analyzer.InspectCell(
                cellX, cellY, cellZ,
                rootPath: GetString(payload, "rootPath"),
                targetTag: GetString(payload, "targetTag"),
                targetLayer: GetString(payload, "targetLayer"),
                includeKinematic: GetBool(payload, "includeKinematic", true),
                include2D: GetBool(payload, "include2D", false),
                includeTriggers: GetBool(payload, "includeTriggers", true),
                detectionMode: GetString(payload, "detectionMode") ?? "collider",
                customMin: GetOptionalVector3(payload, "customMin"),
                customMax: GetOptionalVector3(payload, "customMax")
            );

            var dict = cell.ToDictionary();
            dict["success"] = true;
            dict["operation"] = "inspectCell";
            return dict;
        }

        #endregion

        #region Helpers

        private Vector3? GetOptionalVector3(Dictionary<string, object> payload, string key)
        {
            if (payload == null || !payload.ContainsKey(key)) return null;

            var value = payload[key];
            if (value is Dictionary<string, object> dict)
            {
                float x = dict.ContainsKey("x") ? Convert.ToSingle(dict["x"]) : 0f;
                float y = dict.ContainsKey("y") ? Convert.ToSingle(dict["y"]) : 0f;
                float z = dict.ContainsKey("z") ? Convert.ToSingle(dict["z"]) : 0f;
                return new Vector3(x, y, z);
            }

            return null;
        }

        #endregion
    }
}
