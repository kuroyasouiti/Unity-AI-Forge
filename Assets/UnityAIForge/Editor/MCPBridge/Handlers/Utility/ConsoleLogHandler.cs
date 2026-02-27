using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using MCP.Editor.Base;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Console Log Handler: Retrieves compilation errors, runtime errors, and Debug.Log messages.
    /// Essential for LLMs to debug and fix issues autonomously.
    /// </summary>
    public class ConsoleLogHandler : BaseCommandHandler
    {
        // Unity internal LogEntries class for accessing console logs
        private static readonly Type LogEntriesType;
        private static readonly MethodInfo GetCountMethod;
        private static readonly MethodInfo GetCountsByTypeMethod;
        private static readonly MethodInfo GetEntryMethod;
        private static readonly MethodInfo ClearMethod;
        private static readonly MethodInfo StartGettingEntriesMethod;
        private static readonly MethodInfo EndGettingEntriesMethod;

        // Snapshot state for diff operations
        private static List<Dictionary<string, object>> _snapshotLogs;
        private static int _snapshotTotalCount;

        static ConsoleLogHandler()
        {
            // Get Unity internal LogEntries class
            LogEntriesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntries");

            if (LogEntriesType != null)
            {
                GetCountMethod = LogEntriesType.GetMethod("GetCount", BindingFlags.Public | BindingFlags.Static);
                GetCountsByTypeMethod = LogEntriesType.GetMethod("GetCountsByType", BindingFlags.Public | BindingFlags.Static);
                ClearMethod = LogEntriesType.GetMethod("Clear", BindingFlags.Public | BindingFlags.Static);
                StartGettingEntriesMethod = LogEntriesType.GetMethod("StartGettingEntries", BindingFlags.Public | BindingFlags.Static);
                EndGettingEntriesMethod = LogEntriesType.GetMethod("EndGettingEntries", BindingFlags.Public | BindingFlags.Static);

                // GetEntryInternal for older Unity versions, or GetEntryAtIndex for newer
                GetEntryMethod = LogEntriesType.GetMethod("GetEntryInternal", BindingFlags.Public | BindingFlags.Static);
                if (GetEntryMethod == null)
                {
                    GetEntryMethod = LogEntriesType.GetMethod("GetEntryAtIndex", BindingFlags.Public | BindingFlags.Static);
                }
            }
        }

        public override IEnumerable<string> SupportedOperations => new[]
        {
            "getRecent",
            "getErrors",
            "getWarnings",
            "getLogs",
            "clear",
            "getCompilationErrors",
            "getSummary",
            "snapshot",
            "diff",
            "filter"
        };

        public override string Category => "consoleLog";
        public override string Version => "1.0.0";

        protected override bool RequiresCompilationWait(string operation)
        {
            // Console log operations don't require compilation wait
            return false;
        }

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "getRecent" => HandleGetRecent(payload),
                "getErrors" => HandleGetErrors(payload),
                "getWarnings" => HandleGetWarnings(payload),
                "getLogs" => HandleGetLogs(payload),
                "clear" => HandleClear(payload),
                "getCompilationErrors" => HandleGetCompilationErrors(payload),
                "getSummary" => HandleGetSummary(payload),
                "snapshot" => HandleSnapshot(payload),
                "diff" => HandleDiff(payload),
                "filter" => HandleFilter(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }

        /// <summary>
        /// Get recent N console logs.
        /// </summary>
        private object HandleGetRecent(Dictionary<string, object> payload)
        {
            int count = GetInt(payload, "count", 50);
            var logs = GetConsoleLogs(count, null);

            return CreateSuccessResponse(
                ("logs", logs),
                ("count", logs.Count),
                ("summary", GetLogSummary())
            );
        }

        /// <summary>
        /// Get errors only.
        /// </summary>
        private object HandleGetErrors(Dictionary<string, object> payload)
        {
            int count = GetInt(payload, "count", 100);
            var logs = GetConsoleLogs(count, LogType.Error);

            return CreateSuccessResponse(
                ("logs", logs),
                ("count", logs.Count),
                ("logType", "error")
            );
        }

        /// <summary>
        /// Get warnings only.
        /// </summary>
        private object HandleGetWarnings(Dictionary<string, object> payload)
        {
            int count = GetInt(payload, "count", 100);
            var logs = GetConsoleLogs(count, LogType.Warning);

            return CreateSuccessResponse(
                ("logs", logs),
                ("count", logs.Count),
                ("logType", "warning")
            );
        }

        /// <summary>
        /// Get normal logs only (Debug.Log).
        /// </summary>
        private object HandleGetLogs(Dictionary<string, object> payload)
        {
            int count = GetInt(payload, "count", 100);
            var logs = GetConsoleLogs(count, LogType.Log);

            return CreateSuccessResponse(
                ("logs", logs),
                ("count", logs.Count),
                ("logType", "log")
            );
        }

        /// <summary>
        /// Clear console.
        /// </summary>
        private object HandleClear(Dictionary<string, object> payload)
        {
            if (ClearMethod != null)
            {
                ClearMethod.Invoke(null, null);
                return CreateSuccessResponse(
                    ("message", "Console cleared")
                );
            }

            return new Dictionary<string, object>
            {
                ["success"] = false,
                ["error"] = "Unable to clear console - internal API not available"
            };
        }

        /// <summary>
        /// Get detailed compilation errors.
        /// </summary>
        private object HandleGetCompilationErrors(Dictionary<string, object> payload)
        {
            var errors = new List<Dictionary<string, object>>();

            // Get errors from console logs (includes compilation errors)
            var consoleLogs = GetConsoleLogs(200, LogType.Error);
            foreach (var log in consoleLogs)
            {
                var message = log.ContainsKey("message") ? log["message"]?.ToString() : "";
                // Check if it's a compilation error (contains CS error codes)
                if (message.Contains("error CS") || message.Contains("error UnityEngine") ||
                    message.Contains("Assets/") && message.Contains(".cs"))
                {
                    log["type"] = "compilationError";
                    errors.Add(log);
                }
            }

            return CreateSuccessResponse(
                ("errors", errors),
                ("count", errors.Count),
                ("isCompiling", EditorApplication.isCompiling)
            );
        }

        /// <summary>
        /// Get console log summary.
        /// </summary>
        private object HandleGetSummary(Dictionary<string, object> payload)
        {
            return CreateSuccessResponse(
                ("summary", GetLogSummary()),
                ("isCompiling", EditorApplication.isCompiling),
                ("isPlaying", EditorApplication.isPlaying)
            );
        }

        /// <summary>
        /// Take a snapshot of current console logs for later diff comparison.
        /// </summary>
        private object HandleSnapshot(Dictionary<string, object> payload)
        {
            var severities = GetSeverityList(payload);
            var logs = GetConsoleLogs(int.MaxValue, null);

            if (severities != null && severities.Count > 0)
            {
                logs = FilterBySeverity(logs, severities);
            }

            _snapshotLogs = logs;
            _snapshotTotalCount = logs.Count;

            return CreateSuccessResponse(
                ("snapshotCount", _snapshotTotalCount),
                ("message", $"Snapshot taken with {_snapshotTotalCount} log entries")
            );
        }

        /// <summary>
        /// Compare current console logs against the last snapshot, returning only new entries.
        /// </summary>
        private object HandleDiff(Dictionary<string, object> payload)
        {
            if (_snapshotLogs == null)
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "No snapshot taken. Call 'snapshot' operation first."
                };
            }

            int limit = GetInt(payload, "limit", 100);
            var severities = GetSeverityList(payload);
            var keyword = GetString(payload, "keyword");

            // Get all current logs
            var currentLogs = GetConsoleLogs(int.MaxValue, null);

            // Find the max index from the snapshot to detect new entries
            int snapshotMaxIndex = -1;
            foreach (var log in _snapshotLogs)
            {
                if (log.ContainsKey("index") && log["index"] is int idx && idx > snapshotMaxIndex)
                {
                    snapshotMaxIndex = idx;
                }
            }

            // Collect only new entries (index > snapshotMaxIndex)
            var newLogs = new List<Dictionary<string, object>>();
            foreach (var log in currentLogs)
            {
                if (log.ContainsKey("index") && log["index"] is int idx && idx > snapshotMaxIndex)
                {
                    newLogs.Add(log);
                }
            }

            // Apply severity filter
            if (severities != null && severities.Count > 0)
            {
                newLogs = FilterBySeverity(newLogs, severities);
            }

            // Apply keyword filter
            if (!string.IsNullOrEmpty(keyword))
            {
                newLogs = FilterByKeyword(newLogs, keyword);
            }

            // Apply limit
            if (newLogs.Count > limit)
            {
                newLogs = newLogs.GetRange(0, limit);
            }

            return CreateSuccessResponse(
                ("logs", newLogs),
                ("count", newLogs.Count),
                ("snapshotCount", _snapshotTotalCount),
                ("message", $"{newLogs.Count} new log entries since snapshot")
            );
        }

        /// <summary>
        /// Filter console logs by severity and/or keyword regex.
        /// </summary>
        private object HandleFilter(Dictionary<string, object> payload)
        {
            int limit = GetInt(payload, "limit", 100);
            var severities = GetSeverityList(payload);
            var keyword = GetString(payload, "keyword");

            var logs = GetConsoleLogs(int.MaxValue, null);

            // Apply severity filter
            if (severities != null && severities.Count > 0)
            {
                logs = FilterBySeverity(logs, severities);
            }

            // Apply keyword filter
            if (!string.IsNullOrEmpty(keyword))
            {
                logs = FilterByKeyword(logs, keyword);
            }

            // Apply limit
            if (logs.Count > limit)
            {
                logs = logs.GetRange(0, limit);
            }

            return CreateSuccessResponse(
                ("logs", logs),
                ("count", logs.Count)
            );
        }

        /// <summary>
        /// Extract severity array from payload.
        /// </summary>
        private List<string> GetSeverityList(Dictionary<string, object> payload)
        {
            if (!payload.ContainsKey("severity")) return null;

            var result = new List<string>();
            if (payload["severity"] is IList<object> list)
            {
                foreach (var item in list)
                {
                    if (item != null)
                        result.Add(item.ToString().ToLower());
                }
            }
            else if (payload["severity"] is string s)
            {
                result.Add(s.ToLower());
            }
            return result.Count > 0 ? result : null;
        }

        /// <summary>
        /// Filter log entries by severity types.
        /// </summary>
        private List<Dictionary<string, object>> FilterBySeverity(
            List<Dictionary<string, object>> logs, List<string> severities)
        {
            var filtered = new List<Dictionary<string, object>>();
            foreach (var log in logs)
            {
                if (log.ContainsKey("type") && severities.Contains(log["type"].ToString().ToLower()))
                {
                    filtered.Add(log);
                }
            }
            return filtered;
        }

        /// <summary>
        /// Filter log entries by keyword regex pattern.
        /// </summary>
        private List<Dictionary<string, object>> FilterByKeyword(
            List<Dictionary<string, object>> logs, string keyword)
        {
            var filtered = new List<Dictionary<string, object>>();
            Regex regex;
            try
            {
                regex = new Regex(keyword, RegexOptions.IgnoreCase);
            }
            catch (ArgumentException)
            {
                // Invalid regex, fallback to literal contains
                foreach (var log in logs)
                {
                    var message = log.ContainsKey("message") ? log["message"]?.ToString() ?? "" : "";
                    if (message.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        filtered.Add(log);
                    }
                }
                return filtered;
            }

            foreach (var log in logs)
            {
                var message = log.ContainsKey("message") ? log["message"]?.ToString() ?? "" : "";
                if (regex.IsMatch(message))
                {
                    filtered.Add(log);
                }
            }
            return filtered;
        }

        /// <summary>
        /// Get console logs from Unity's internal LogEntries.
        /// </summary>
        private List<Dictionary<string, object>> GetConsoleLogs(int maxCount, LogType? filterType)
        {
            var logs = new List<Dictionary<string, object>>();

            if (LogEntriesType == null || GetCountMethod == null)
            {
                // Fallback: use Application.logMessageReceived would require persistent storage
                return logs;
            }

            try
            {
                // Start getting entries
                StartGettingEntriesMethod?.Invoke(null, null);

                int totalCount = (int)GetCountMethod.Invoke(null, null);
                // When filtering by type, scan all entries to find matching logs
                // When not filtering, limit to recent entries (maxCount * 3) for performance
                int startIndex = filterType.HasValue ? 0 : Math.Max(0, totalCount - maxCount * 3);

                // Use LogEntry structure via reflection
                var logEntryType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntry");
                if (logEntryType == null)
                {
                    return logs;
                }

                var logEntry = Activator.CreateInstance(logEntryType);
                var messageField = logEntryType.GetField("message", BindingFlags.Public | BindingFlags.Instance);
                var modeField = logEntryType.GetField("mode", BindingFlags.Public | BindingFlags.Instance);
                var fileField = logEntryType.GetField("file", BindingFlags.Public | BindingFlags.Instance);
                var lineField = logEntryType.GetField("line", BindingFlags.Public | BindingFlags.Instance);
                var conditionField = logEntryType.GetField("condition", BindingFlags.Public | BindingFlags.Instance);

                for (int i = totalCount - 1; i >= startIndex && logs.Count < maxCount; i--)
                {
                    bool success = false;

                    if (GetEntryMethod != null)
                    {
                        var result = GetEntryMethod.Invoke(null, new object[] { i, logEntry });
                        success = result is bool boolResult ? boolResult : true;
                    }

                    if (success)
                    {
                        int mode = 0;
                        if (modeField != null)
                        {
                            mode = (int)modeField.GetValue(logEntry);
                        }

                        string message = "";
                        if (conditionField != null)
                        {
                            message = conditionField.GetValue(logEntry)?.ToString() ?? "";
                        }
                        else if (messageField != null)
                        {
                            message = messageField.GetValue(logEntry)?.ToString() ?? "";
                        }

                        // Get log type from mode flags, with message-based fallback for compilation logs
                        LogType logType = GetLogTypeFromMode(mode, message);

                        // Filter by type if specified
                        if (filterType.HasValue && logType != filterType.Value)
                        {
                            continue;
                        }

                        string file = fileField?.GetValue(logEntry)?.ToString() ?? "";
                        int line = lineField != null ? (int)lineField.GetValue(logEntry) : 0;

                        var logDict = new Dictionary<string, object>
                        {
                            ["type"] = logType.ToString().ToLower(),
                            ["message"] = message,
                            ["index"] = i
                        };

                        if (!string.IsNullOrEmpty(file))
                        {
                            logDict["file"] = file;
                            logDict["line"] = line;
                        }

                        // Extract stack trace if present
                        var parts = message.Split(new[] { '\n' }, 2);
                        if (parts.Length > 1)
                        {
                            logDict["message"] = parts[0].Trim();
                            logDict["stackTrace"] = parts[1].Trim();
                        }

                        logs.Add(logDict);
                    }
                }

                // End getting entries
                EndGettingEntriesMethod?.Invoke(null, null);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ConsoleLogHandler: Error reading logs - {ex.Message}");
            }

            return logs;
        }

        /// <summary>
        /// Convert Unity internal mode to LogType, with message-based fallback for compilation logs.
        /// </summary>
        private LogType GetLogTypeFromMode(int mode, string message = null)
        {
            // Unity mode flags:
            // 1 = Error, 2 = Warning, 4 = Log, 8 = Exception
            // These can be combined, so we check in priority order
            if ((mode & 1) != 0 || (mode & 8) != 0 || (mode & 256) != 0)
            {
                return LogType.Error;
            }
            if ((mode & 2) != 0 || (mode & 512) != 0)
            {
                return LogType.Warning;
            }

            // Fallback: Check message content for compilation warnings/errors
            // Compilation logs may not have proper mode flags set
            if (!string.IsNullOrEmpty(message))
            {
                // Check for compilation error patterns (e.g., "error CS0001:", "error UnityEngine")
                if (message.Contains(": error CS") || message.Contains(": error UnityEngine") ||
                    message.Contains("): error "))
                {
                    return LogType.Error;
                }

                // Check for compilation warning patterns (e.g., "warning CS0108:")
                if (message.Contains(": warning CS") || message.Contains(": warning UnityEngine") ||
                    message.Contains("): warning "))
                {
                    return LogType.Warning;
                }
            }

            return LogType.Log;
        }

        /// <summary>
        /// Get summary of log counts by type.
        /// </summary>
        private Dictionary<string, object> GetLogSummary()
        {
            var summary = new Dictionary<string, object>
            {
                ["errors"] = 0,
                ["warnings"] = 0,
                ["logs"] = 0,
                ["total"] = 0
            };

            if (GetCountsByTypeMethod != null)
            {
                try
                {
                    var parameters = new object[] { 0, 0, 0 };
                    GetCountsByTypeMethod.Invoke(null, parameters);

                    summary["errors"] = (int)parameters[0];
                    summary["warnings"] = (int)parameters[1];
                    summary["logs"] = (int)parameters[2];
                    summary["total"] = (int)parameters[0] + (int)parameters[1] + (int)parameters[2];
                }
                catch
                {
                    // Fallback to GetCount
                    if (GetCountMethod != null)
                    {
                        summary["total"] = (int)GetCountMethod.Invoke(null, null);
                    }
                }
            }

            return summary;
        }
    }
}
