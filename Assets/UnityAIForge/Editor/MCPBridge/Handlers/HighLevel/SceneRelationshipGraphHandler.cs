using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using MCP.Editor.Utilities.GraphAnalysis;

namespace MCP.Editor.Handlers.HighLevel
{
    /// <summary>
    /// Handler for analyzing relationships between Unity scenes.
    /// Provides tools to understand scene transitions and dependencies.
    /// </summary>
    public class SceneRelationshipGraphHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "analyzeAll",
            "analyzeScene",
            "findTransitionsTo",
            "findTransitionsFrom",
            "validateBuildSettings"
        };

        public override string Category => "sceneRelationshipGraph";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "analyzeAll" => AnalyzeAll(payload),
                "analyzeScene" => AnalyzeScene(payload),
                "findTransitionsTo" => FindTransitionsTo(payload),
                "findTransitionsFrom" => FindTransitionsFrom(payload),
                "validateBuildSettings" => ValidateBuildSettings(payload),
                _ => throw new InvalidOperationException($"Unsupported scene relationship graph operation: {operation}"),
            };
        }

        #region Operations

        /// <summary>
        /// Analyze all scene relationships in the project.
        /// </summary>
        private object AnalyzeAll(Dictionary<string, object> payload)
        {
            var includeScriptReferences = GetBoolOrDefault(payload, "includeScriptReferences", true);
            var includeSceneFlow = GetBoolOrDefault(payload, "includeSceneFlow", true);
            var format = GetString(payload, "format") ?? "json";

            var analyzer = new SceneRelationshipAnalyzer();
            var result = analyzer.AnalyzeAll(includeScriptReferences, includeSceneFlow);

            return FormatResult(result, format);
        }

        /// <summary>
        /// Analyze transitions from a specific scene.
        /// </summary>
        private object AnalyzeScene(Dictionary<string, object> payload)
        {
            var scenePath = GetString(payload, "scenePath");
            if (string.IsNullOrEmpty(scenePath))
            {
                throw new ArgumentException("scenePath is required");
            }

            var includeScriptReferences = GetBoolOrDefault(payload, "includeScriptReferences", true);
            var includeSceneFlow = GetBoolOrDefault(payload, "includeSceneFlow", true);
            var format = GetString(payload, "format") ?? "json";

            var analyzer = new SceneRelationshipAnalyzer();
            var result = analyzer.AnalyzeScene(scenePath, includeScriptReferences, includeSceneFlow);

            return FormatResult(result, format);
        }

        /// <summary>
        /// Find all scenes that can transition to the specified scene.
        /// </summary>
        private object FindTransitionsTo(Dictionary<string, object> payload)
        {
            var scenePath = GetString(payload, "scenePath");
            if (string.IsNullOrEmpty(scenePath))
            {
                throw new ArgumentException("scenePath is required");
            }

            var format = GetString(payload, "format") ?? "json";

            var analyzer = new SceneRelationshipAnalyzer();
            var result = analyzer.FindTransitionsTo(scenePath);

            return FormatResult(result, format);
        }

        /// <summary>
        /// Find all scenes that the specified scene can transition to.
        /// </summary>
        private object FindTransitionsFrom(Dictionary<string, object> payload)
        {
            var scenePath = GetString(payload, "scenePath");
            if (string.IsNullOrEmpty(scenePath))
            {
                throw new ArgumentException("scenePath is required");
            }

            var format = GetString(payload, "format") ?? "json";

            var analyzer = new SceneRelationshipAnalyzer();
            var result = analyzer.FindTransitionsFrom(scenePath);

            return FormatResult(result, format);
        }

        /// <summary>
        /// Validate build settings and report issues.
        /// </summary>
        private object ValidateBuildSettings(Dictionary<string, object> payload)
        {
            var format = GetString(payload, "format") ?? "json";

            var analyzer = new SceneRelationshipAnalyzer();
            var result = analyzer.ValidateBuildSettings();

            return FormatResult(result, format);
        }

        #endregion

        #region Helpers

        private object FormatResult(GraphResult result, string format)
        {
            switch (format.ToLower())
            {
                case "dot":
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["format"] = "dot",
                        ["content"] = result.ToDot("SceneRelationships")
                    };

                case "mermaid":
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["format"] = "mermaid",
                        ["content"] = result.ToMermaid()
                    };

                case "summary":
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["format"] = "summary",
                        ["content"] = result.ToSummary()
                    };

                case "json":
                default:
                    var dict = result.ToDictionary();
                    dict["success"] = true;
                    return dict;
            }
        }

        private bool GetBoolOrDefault(Dictionary<string, object> payload, string key, bool defaultValue)
        {
            if (payload.TryGetValue(key, out var value))
            {
                if (value is bool b) return b;
                if (value is string s && bool.TryParse(s, out var parsed)) return parsed;
            }
            return defaultValue;
        }

        #endregion
    }
}
