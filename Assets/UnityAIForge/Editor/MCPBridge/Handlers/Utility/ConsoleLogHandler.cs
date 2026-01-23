using System;
using System.Collections.Generic;
using System.Reflection;
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
            "getSummary"
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
