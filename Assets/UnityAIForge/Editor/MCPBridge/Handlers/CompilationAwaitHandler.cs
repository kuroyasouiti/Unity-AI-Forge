using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// コンパイル状態確認のコマンドハンドラー。
    /// 現在のコンパイル状態を返します。
    ///
    /// NOTE: コンパイル待機はPython (MCP Server) 側で非同期メッセージ
    /// (compilation:complete, bridge:restarted) を使用して行われます。
    /// このハンドラーはブロッキング待機を行いません。
    /// </summary>
    public class CompilationAwaitHandler : BaseCommandHandler
    {
        public override string Category => "compilationAwait";

        public override IEnumerable<string> SupportedOperations => new[]
        {
            "await",
            "status",
        };

        public CompilationAwaitHandler() : base()
        {
        }

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "await" => GetCompilationStatus(payload),
                "status" => GetCompilationStatus(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }

        protected override bool RequiresCompilationWait(string operation)
        {
            // This handler itself manages compilation status
            return false;
        }

        #region Operations

        /// <summary>
        /// 現在のコンパイル状態を返します。
        /// ブロッキング待機は行いません。
        /// Python側で非同期メッセージを使用して待機を行います。
        /// </summary>
        private object GetCompilationStatus(Dictionary<string, object> payload)
        {
            var isCompiling = EditorApplication.isCompiling;

            if (!isCompiling)
            {
                // コンパイル中でない場合、最新のコンパイル結果を返す
                var compilationResult = GetCompilationResult();
                return new Dictionary<string, object>
                {
                    ["isCompiling"] = false,
                    ["wasCompiling"] = false,
                    ["compilationCompleted"] = true,
                    ["waitTimeSeconds"] = 0f,
                    ["success"] = true,
                    ["errorCount"] = compilationResult["errorCount"],
                    ["warningCount"] = compilationResult["warningCount"],
                    ["errors"] = compilationResult["errors"],
                    ["warnings"] = compilationResult["warnings"],
                    ["message"] = "No compilation in progress",
                };
            }

            // コンパイル中の場合、Python側で非同期待機するよう状態を返す
            Debug.Log("[CompilationAwait] Compilation in progress - Python side will wait for async completion message");
            return new Dictionary<string, object>
            {
                ["isCompiling"] = true,
                ["wasCompiling"] = true,
                ["compilationCompleted"] = false,
                ["waitTimeSeconds"] = 0f,
                ["success"] = false,
                ["errorCount"] = 0,
                ["message"] = "Compilation in progress - waiting for async completion",
            };
        }

        #endregion

        #region Helper Methods

        private Dictionary<string, object> GetCompilationResult()
        {
            var errorMessages = new List<string>();
            var warningMessages = new List<string>();
            var consoleEntries = new List<Dictionary<string, object>>();
            
            // Get assembly information
            try
            {
                var assemblies = CompilationPipeline.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    // Check for compilation errors (this is available after compilation completes)
                    // Note: Unity doesn't provide direct API to get compilation errors
                    // We rely on console log parsing below
                }
            }
            catch
            {
                // Ignore if we can't get assembly info
            }
            
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
        
        private List<Dictionary<string, object>> GetConsoleLogEntries(int limit = 100)
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
                
                var getCountMethod = logEntriesType.GetMethod("GetCount", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                var startGettingEntriesMethod = logEntriesType.GetMethod("StartGettingEntries", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                var getEntryInternalMethod = logEntriesType.GetMethod("GetEntryInternal", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                var endGettingEntriesMethod = logEntriesType.GetMethod("EndGettingEntries", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                
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
        
        #endregion
    }
}

