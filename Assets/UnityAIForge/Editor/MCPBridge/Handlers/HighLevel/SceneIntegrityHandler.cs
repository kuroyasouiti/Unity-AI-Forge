using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;
using MCP.Editor.Utilities.GraphAnalysis;

namespace MCP.Editor.Handlers.HighLevel
{
    /// <summary>
    /// Handler for scene integrity validation.
    /// Detects missing scripts, null references, broken events, and broken prefabs.
    /// </summary>
    public class SceneIntegrityHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "missingScripts",
            "nullReferences",
            "brokenEvents",
            "brokenPrefabs",
            "removeMissingScripts",
            "all"
        };

        public override string Category => "sceneIntegrity";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            var rootPath = GetString(payload, "rootPath");
            var analyzer = new SceneIntegrityAnalyzer();

            return operation switch
            {
                "missingScripts" => BuildResponse(operation, analyzer.FindMissingScripts(rootPath)),
                "nullReferences" => BuildResponse(operation, analyzer.FindNullReferences(rootPath)),
                "brokenEvents" => BuildResponse(operation, analyzer.FindBrokenEvents(rootPath)),
                "brokenPrefabs" => BuildResponse(operation, analyzer.FindBrokenPrefabs(rootPath)),
                "removeMissingScripts" => BuildRemoveResponse(analyzer, rootPath),
                "all" => BuildAllResponse(analyzer, rootPath),
                _ => throw new InvalidOperationException($"Unsupported scene integrity operation: {operation}"),
            };
        }

        private Dictionary<string, object> BuildResponse(string operation,
            List<SceneIntegrityAnalyzer.IntegrityIssue> issues)
        {
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["category"] = "sceneIntegrity",
                ["operation"] = operation,
                ["issues"] = issues.Select(i => i.ToDictionary()).ToList(),
                ["issueCount"] = issues.Count,
                ["isClean"] = issues.Count == 0
            };
        }

        private Dictionary<string, object> BuildRemoveResponse(SceneIntegrityAnalyzer analyzer, string rootPath)
        {
            var (removed, totalRemoved) = analyzer.RemoveMissingScripts(rootPath);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["category"] = "sceneIntegrity",
                ["operation"] = "removeMissingScripts",
                ["removed"] = removed.Select(i => i.ToDictionary()).ToList(),
                ["totalRemoved"] = totalRemoved,
                ["affectedGameObjects"] = removed.Count
            };
        }

        private Dictionary<string, object> BuildAllResponse(SceneIntegrityAnalyzer analyzer, string rootPath)
        {
            var (issues, summary) = analyzer.FindAllIssues(rootPath);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["category"] = "sceneIntegrity",
                ["operation"] = "all",
                ["issues"] = issues.Select(i => i.ToDictionary()).ToList(),
                ["issueCount"] = issues.Count,
                ["isClean"] = issues.Count == 0,
                ["summary"] = summary
            };
        }
    }
}
