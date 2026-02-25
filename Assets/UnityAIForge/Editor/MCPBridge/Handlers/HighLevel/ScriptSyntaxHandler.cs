using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using MCP.Editor.Utilities.GraphAnalysis;

namespace MCP.Editor.Handlers.HighLevel
{
    /// <summary>
    /// Handler for C# source code syntax analysis.
    /// Provides source-level structure parsing with line numbers,
    /// cross-file reference search, unused code detection, and code metrics.
    /// </summary>
    public class ScriptSyntaxHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "analyzeScript",
            "findReferences",
            "findUnusedCode",
            "analyzeMetrics"
        };

        public override string Category => "scriptSyntax";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "analyzeScript" => AnalyzeScript(payload),
                "findReferences" => FindReferences(payload),
                "findUnusedCode" => FindUnusedCode(payload),
                "analyzeMetrics" => AnalyzeMetrics(payload),
                _ => throw new InvalidOperationException($"Unsupported script syntax operation: {operation}"),
            };
        }

        #region Operations

        /// <summary>
        /// Parse a C# script file and return its structure (types, methods, fields, properties, etc.)
        /// with line numbers.
        /// </summary>
        private object AnalyzeScript(Dictionary<string, object> payload)
        {
            var scriptPath = GetString(payload, "scriptPath");
            if (string.IsNullOrEmpty(scriptPath))
                throw new ArgumentException("scriptPath is required for analyzeScript operation");

            var analyzer = new ScriptSyntaxAnalyzer();
            var result = analyzer.AnalyzeScript(scriptPath);
            result["success"] = true;
            return result;
        }

        /// <summary>
        /// Find all references to a symbol across the project.
        /// </summary>
        private object FindReferences(Dictionary<string, object> payload)
        {
            var symbolName = GetString(payload, "symbolName");
            if (string.IsNullOrEmpty(symbolName))
                throw new ArgumentException("symbolName is required for findReferences operation");

            var symbolType = GetString(payload, "symbolType");
            var searchPath = GetString(payload, "searchPath");

            var analyzer = new ScriptSyntaxAnalyzer();
            var result = analyzer.FindReferences(symbolName, symbolType, searchPath);
            result["success"] = true;
            return result;
        }

        /// <summary>
        /// Find potentially unused methods and fields.
        /// </summary>
        private object FindUnusedCode(Dictionary<string, object> payload)
        {
            var scriptPath = GetString(payload, "scriptPath");
            var searchPath = GetString(payload, "searchPath");
            var targetType = GetString(payload, "targetType");

            var analyzer = new ScriptSyntaxAnalyzer();
            var result = analyzer.FindUnusedCode(scriptPath, searchPath, targetType);
            result["success"] = true;
            return result;
        }

        /// <summary>
        /// Compute code metrics for one or more scripts.
        /// </summary>
        private object AnalyzeMetrics(Dictionary<string, object> payload)
        {
            var scriptPath = GetString(payload, "scriptPath");
            var searchPath = GetString(payload, "searchPath");

            var analyzer = new ScriptSyntaxAnalyzer();
            var result = analyzer.AnalyzeMetrics(scriptPath, searchPath);
            result["success"] = true;
            return result;
        }

        #endregion
    }
}
