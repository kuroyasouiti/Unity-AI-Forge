using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using MCP.Editor.Utilities.GraphAnalysis;

namespace MCP.Editor.Handlers.HighLevel
{
    /// <summary>
    /// Handler for analyzing object references within Unity scenes.
    /// Provides tools to understand how GameObjects and Components reference each other.
    /// </summary>
    public class SceneReferenceGraphHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "analyzeScene",
            "analyzeObject",
            "findReferencesTo",
            "findReferencesFrom",
            "findOrphans"
        };

        public override string Category => "sceneReferenceGraph";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "analyzeScene" => AnalyzeScene(payload),
                "analyzeObject" => AnalyzeObject(payload),
                "findReferencesTo" => FindReferencesTo(payload),
                "findReferencesFrom" => FindReferencesFrom(payload),
                "findOrphans" => FindOrphans(payload),
                _ => throw new InvalidOperationException($"Unsupported scene reference graph operation: {operation}"),
            };
        }

        #region Operations

        /// <summary>
        /// Analyze all object references in the current or specified scene.
        /// </summary>
        private object AnalyzeScene(Dictionary<string, object> payload)
        {
            var scenePath = GetString(payload, "scenePath");
            var includeHierarchy = GetBoolOrDefault(payload, "includeHierarchy", true);
            var includeEvents = GetBoolOrDefault(payload, "includeEvents", true);
            var format = GetString(payload, "format") ?? "json";

            var analyzer = new SceneReferenceAnalyzer();
            var result = analyzer.AnalyzeScene(scenePath, includeHierarchy, includeEvents);

            return FormatResult(result, format);
        }

        /// <summary>
        /// Analyze references for a specific GameObject.
        /// </summary>
        private object AnalyzeObject(Dictionary<string, object> payload)
        {
            var objectPath = GetString(payload, "objectPath");
            if (string.IsNullOrEmpty(objectPath))
            {
                throw new ArgumentException("objectPath is required");
            }

            var includeChildren = GetBoolOrDefault(payload, "includeChildren", true);
            var includeEvents = GetBoolOrDefault(payload, "includeEvents", true);
            var format = GetString(payload, "format") ?? "json";

            var analyzer = new SceneReferenceAnalyzer();
            var result = analyzer.AnalyzeObject(objectPath, includeChildren, includeEvents);

            return FormatResult(result, format);
        }

        /// <summary>
        /// Find all objects that reference the specified object.
        /// </summary>
        private object FindReferencesTo(Dictionary<string, object> payload)
        {
            var objectPath = GetString(payload, "objectPath");
            if (string.IsNullOrEmpty(objectPath))
            {
                throw new ArgumentException("objectPath is required");
            }

            var format = GetString(payload, "format") ?? "json";

            var analyzer = new SceneReferenceAnalyzer();
            var result = analyzer.FindReferencesTo(objectPath);

            return FormatResult(result, format);
        }

        /// <summary>
        /// Find all objects that the specified object references.
        /// </summary>
        private object FindReferencesFrom(Dictionary<string, object> payload)
        {
            var objectPath = GetString(payload, "objectPath");
            if (string.IsNullOrEmpty(objectPath))
            {
                throw new ArgumentException("objectPath is required");
            }

            var format = GetString(payload, "format") ?? "json";

            var analyzer = new SceneReferenceAnalyzer();
            var result = analyzer.FindReferencesFrom(objectPath);

            return FormatResult(result, format);
        }

        /// <summary>
        /// Find orphan objects (not referenced by anything).
        /// </summary>
        private object FindOrphans(Dictionary<string, object> payload)
        {
            var format = GetString(payload, "format") ?? "json";

            var analyzer = new SceneReferenceAnalyzer();
            var result = analyzer.FindOrphans();

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
                        ["content"] = result.ToDot("SceneReferences")
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
