using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;
using MCP.Editor.Utilities.GraphAnalysis;

namespace MCP.Editor.Handlers.HighLevel
{
    /// <summary>
    /// Handler for cataloging and inspecting types in the Unity project.
    /// Provides type enumeration and detailed type inspection.
    /// </summary>
    public class ClassCatalogHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "listTypes",
            "inspectType"
        };

        public override string Category => "classCatalog";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "listTypes" => ListTypes(payload),
                "inspectType" => InspectType(payload),
                _ => throw new InvalidOperationException($"Unsupported class catalog operation: {operation}"),
            };
        }

        #region Operations

        private object ListTypes(Dictionary<string, object> payload)
        {
            var searchPath = GetString(payload, "searchPath");
            var typeKind = GetString(payload, "typeKind");
            var namespaceName = GetString(payload, "namespace");
            var baseClass = GetString(payload, "baseClass");
            var namePattern = GetString(payload, "namePattern");
            var maxResults = GetIntOrDefault(payload, "maxResults", 100);

            var analyzer = new TypeCatalogAnalyzer();
            var entries = analyzer.ListTypes(searchPath, typeKind, namespaceName, baseClass, namePattern, maxResults);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["count"] = entries.Count,
                ["types"] = entries.Select(e => e.ToDictionary()).ToList()
            };
        }

        private object InspectType(Dictionary<string, object> payload)
        {
            var className = GetString(payload, "className");
            if (string.IsNullOrEmpty(className))
            {
                throw new ArgumentException("className is required");
            }

            var includeFields = GetBoolOrDefault(payload, "includeFields", true);
            var includeMethods = GetBoolOrDefault(payload, "includeMethods", false);
            var includeProperties = GetBoolOrDefault(payload, "includeProperties", false);

            var analyzer = new TypeCatalogAnalyzer();
            var result = analyzer.InspectType(className, includeFields, includeMethods, includeProperties);

            var dict = result.ToDictionary();
            dict["success"] = true;
            return dict;
        }

        #endregion

        #region Helpers

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
