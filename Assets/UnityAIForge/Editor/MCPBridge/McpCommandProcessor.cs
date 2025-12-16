using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.UI;
using MCP.Editor.Base;

namespace MCP.Editor
{
    /// <summary>
    /// Processes MCP tool commands and executes corresponding Unity Editor operations.
    /// Supports management operations for scenes, GameObjects, components, and assets.
    /// 
    /// Architecture:
    /// - Phase 3+ handlers: Use new CommandHandlerFactory system for improved testability
    /// - Legacy handlers: Continue to use partial class methods for backward compatibility
    /// </summary>
    internal static partial class McpCommandProcessor
    {
        /// <summary>
        /// Executes an MCP command and returns the result.
        /// Tries to use new handler system first, falls back to legacy partial class methods.
        /// </summary>
        /// <param name="command">The command to execute containing tool name and payload.</param>
        /// <returns>Result dictionary with operation-specific data.</returns>
        /// <exception cref="InvalidOperationException">Thrown when tool name is not supported.</exception>
        public static object Execute(McpIncomingCommand command)
        {
            // Try new handler system first (Phase 3+ handlers)
            if (CommandHandlerFactory.TryGetHandler(command.ToolName, out var handler))
            {
                try
                {
                    return handler.Execute(command.Payload);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Handler '{command.ToolName}' threw exception: {ex.Message}");
                    throw;
                }
            }
            
            // Fall back to legacy partial class methods
            return ExecuteLegacy(command);
        }
        
        /// <summary>
        /// Executes legacy partial class methods for backward compatibility.
        /// All handlers have been migrated to the new CommandHandlerFactory system.
        /// </summary>
        private static object ExecuteLegacy(McpIncomingCommand command)
        {
            // All handlers have been migrated to new handler system
            throw new InvalidOperationException($"Unsupported tool name: {command.ToolName}. Handler not registered in CommandHandlerFactory.");
        }
        
        /// <summary>
        /// Gets handler execution mode for diagnostics.
        /// </summary>
        public static string GetHandlerMode(string toolName)
        {
            if (CommandHandlerFactory.IsRegistered(toolName))
            {
                return "NewHandler";
            }
            return "Legacy";
        }

        /// <summary>
        /// Gets compilation result including errors, warnings, and console logs.
        /// </summary>
        /// <returns>Dictionary containing compilation result data.</returns>
        public static Dictionary<string, object> GetCompilationResult()
        {
            var errorMessages = new List<string>();
            var warningMessages = new List<string>();
            var consoleEntries = new List<Dictionary<string, object>>();

            // Parse console logs for errors and warnings
            var logEntries = GetConsoleLogEntries(limit: 200);

            foreach (var entry in logEntries)
            {
                if (!entry.ContainsKey("type") || !entry.ContainsKey("message"))
                {
                    continue;
                }

                var message = entry["message"].ToString();
                var entryType = entry["type"].ToString();

                consoleEntries.Add(new Dictionary<string, object>
                {
                    ["type"] = entryType,
                    ["message"] = message,
                });

                if (entryType == "Error")
                {
                    errorMessages.Add(message);
                }
                else if (entryType == "Warning")
                {
                    warningMessages.Add(message);
                }
            }

            return new Dictionary<string, object>
            {
                ["success"] = errorMessages.Count == 0,
                ["errorCount"] = errorMessages.Count,
                ["warningCount"] = warningMessages.Count,
                ["errors"] = errorMessages,
                ["warnings"] = warningMessages,
                ["consoleLogs"] = consoleEntries,
            };
        }

        private static List<Dictionary<string, object>> GetConsoleLogEntries(int limit = 100)
        {
            var logEntries = new List<Dictionary<string, object>>();

            try
            {
                // Use reflection to access Unity's internal LogEntries
                var logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor");
                if (logEntriesType == null)
                {
                    return logEntries;
                }

                var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
                var startGettingEntriesMethod = logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public);
                var getEntryInternalMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
                var endGettingEntriesMethod = logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public);

                if (getCountMethod == null || startGettingEntriesMethod == null ||
                    getEntryInternalMethod == null || endGettingEntriesMethod == null)
                {
                    return logEntries;
                }

                var count = (int)getCountMethod.Invoke(null, null);
                startGettingEntriesMethod.Invoke(null, null);

                var logEntryType = Type.GetType("UnityEditor.LogEntry, UnityEditor");
                if (logEntryType == null)
                {
                    endGettingEntriesMethod.Invoke(null, null);
                    return logEntries;
                }

                for (int i = 0; i < Math.Min(count, limit); i++)
                {
                    var logEntry = Activator.CreateInstance(logEntryType);
                    var parameters = new object[] { i, logEntry };
                    var success = (bool)getEntryInternalMethod.Invoke(null, parameters);

                    if (success)
                    {
                        var messageField = logEntryType.GetField("message");
                        var modeField = logEntryType.GetField("mode");

                        if (messageField != null && modeField != null)
                        {
                            var message = messageField.GetValue(logEntry)?.ToString() ?? "";
                            var mode = (int)modeField.GetValue(logEntry);

                            var entryType = mode switch
                            {
                                0 => "Log",
                                1 => "Warning",
                                2 => "Error",
                                _ => "Unknown"
                            };

                            logEntries.Add(new Dictionary<string, object>
                            {
                                ["message"] = message,
                                ["type"] = entryType,
                            });
                        }
                    }
                }

                endGettingEntriesMethod.Invoke(null, null);
            }
            catch (Exception)
            {
                // Silently fail if we can't access log entries
            }

            return logEntries;
        }

    }
}

