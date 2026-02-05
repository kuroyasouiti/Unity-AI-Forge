using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using MCP.Editor.Utilities.GraphAnalysis;

namespace MCP.Editor.Handlers.HighLevel
{
    /// <summary>
    /// Handler for analyzing class dependencies in the Unity project.
    /// Provides tools to understand script/component relationships.
    /// </summary>
    public class ClassDependencyGraphHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "analyzeClass",
            "analyzeAssembly",
            "analyzeNamespace",
            "findDependents",
            "findDependencies"
        };

        public override string Category => "classDependencyGraph";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "analyzeClass" => AnalyzeClass(payload),
                "analyzeAssembly" => AnalyzeAssembly(payload),
                "analyzeNamespace" => AnalyzeNamespace(payload),
                "findDependents" => FindDependents(payload),
                "findDependencies" => FindDependencies(payload),
                _ => throw new InvalidOperationException($"Unsupported class dependency graph operation: {operation}"),
            };
        }

        #region Operations

        /// <summary>
        /// Analyze a single class and its dependencies.
        /// </summary>
        private object AnalyzeClass(Dictionary<string, object> payload)
        {
            var target = GetString(payload, "target");
            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentException("target (class name) is required");
            }

            var depth = GetIntOrDefault(payload, "depth", 1);
            var includeUnityTypes = GetBoolOrDefault(payload, "includeUnityTypes", false);
            var format = GetString(payload, "format") ?? "json";

            var analyzer = new ClassDependencyAnalyzer();
            var result = analyzer.AnalyzeClass(target, depth, includeUnityTypes);

            return FormatResult(result, format);
        }

        /// <summary>
        /// Analyze all classes in an assembly.
        /// </summary>
        private object AnalyzeAssembly(Dictionary<string, object> payload)
        {
            var target = GetString(payload, "target");
            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentException("target (assembly name) is required");
            }

            var depth = GetIntOrDefault(payload, "depth", 1);
            var includeUnityTypes = GetBoolOrDefault(payload, "includeUnityTypes", false);
            var format = GetString(payload, "format") ?? "json";

            var analyzer = new ClassDependencyAnalyzer();
            var result = analyzer.AnalyzeAssembly(target, depth, includeUnityTypes);

            return FormatResult(result, format);
        }

        /// <summary>
        /// Analyze all classes in a namespace.
        /// </summary>
        private object AnalyzeNamespace(Dictionary<string, object> payload)
        {
            var target = GetString(payload, "target");
            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentException("target (namespace) is required");
            }

            var depth = GetIntOrDefault(payload, "depth", 1);
            var includeUnityTypes = GetBoolOrDefault(payload, "includeUnityTypes", false);
            var format = GetString(payload, "format") ?? "json";

            var analyzer = new ClassDependencyAnalyzer();
            var result = analyzer.AnalyzeNamespace(target, depth, includeUnityTypes);

            return FormatResult(result, format);
        }

        /// <summary>
        /// Find classes that depend on the specified class.
        /// </summary>
        private object FindDependents(Dictionary<string, object> payload)
        {
            var target = GetString(payload, "target");
            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentException("target (class name) is required");
            }

            var includeUnityTypes = GetBoolOrDefault(payload, "includeUnityTypes", false);
            var format = GetString(payload, "format") ?? "json";

            var analyzer = new ClassDependencyAnalyzer();
            var result = analyzer.FindDependents(target, includeUnityTypes);

            return FormatResult(result, format);
        }

        /// <summary>
        /// Find classes that the specified class depends on.
        /// </summary>
        private object FindDependencies(Dictionary<string, object> payload)
        {
            var target = GetString(payload, "target");
            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentException("target (class name) is required");
            }

            var depth = GetIntOrDefault(payload, "depth", 1);
            var includeUnityTypes = GetBoolOrDefault(payload, "includeUnityTypes", false);
            var format = GetString(payload, "format") ?? "json";

            var analyzer = new ClassDependencyAnalyzer();
            var result = analyzer.FindDependencies(target, depth, includeUnityTypes);

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
                        ["content"] = result.ToDot("ClassDependencies")
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

        private int GetIntOrDefault(Dictionary<string, object> payload, string key, int defaultValue)
        {
            if (payload.TryGetValue(key, out var value))
            {
                if (value is int i) return i;
                if (value is long l) return (int)l;
                if (value is double d) return (int)d;
                if (value is string s && int.TryParse(s, out var parsed)) return parsed;
            }
            return defaultValue;
        }

        #endregion
    }
}
