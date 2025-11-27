using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// コンパイル完了待機のコマンドハンドラー。
    /// 進行中のコンパイルを待機し、結果を返します。
    /// </summary>
    public class CompilationAwaitHandler : BaseCommandHandler
    {
        public override string Category => "compilationAwait";
        
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "await",
        };
        
        public CompilationAwaitHandler() : base()
        {
        }
        
        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "await" => AwaitCompilation(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }
        
        protected override bool RequiresCompilationWait(string operation)
        {
            // This handler itself manages compilation waiting
            return false;
        }
        
        #region Operations
        
        private object AwaitCompilation(Dictionary<string, object> payload)
        {
            var timeoutSeconds = GetInt(payload, "timeoutSeconds", 60);
            
            if (!EditorApplication.isCompiling)
            {
                // Not compiling, return immediately with success
                return new Dictionary<string, object>
                {
                    ["wasCompiling"] = false,
                    ["compilationCompleted"] = true,
                    ["waitTimeSeconds"] = 0f,
                    ["success"] = true,
                    ["errorCount"] = 0,
                    ["message"] = "No compilation in progress",
                };
            }
            
            Debug.Log($"[CompilationAwait] Waiting for compilation to complete (timeout: {timeoutSeconds}s)...");
            var startTime = EditorApplication.timeSinceStartup;
            var completed = WaitForCompilationToComplete(timeoutSeconds);
            var elapsedSeconds = EditorApplication.timeSinceStartup - startTime;
            
            var result = new Dictionary<string, object>
            {
                ["wasCompiling"] = true,
                ["compilationCompleted"] = completed,
                ["waitTimeSeconds"] = (float)Math.Round(elapsedSeconds, 2),
                ["success"] = completed,
            };
            
            if (!completed)
            {
                result["message"] = $"Compilation did not complete within {timeoutSeconds}s";
                result["errorCount"] = 0;
                return result;
            }
            
            // Get compilation result
            var compilationResult = GetCompilationResult();
            result["errorCount"] = compilationResult["errorCount"];
            result["warningCount"] = compilationResult["warningCount"];
            result["errors"] = compilationResult["errors"];
            result["warnings"] = compilationResult["warnings"];
            result["consoleLogs"] = compilationResult["consoleLogs"];
            
            var hasErrors = (int)compilationResult["errorCount"] > 0;
            result["success"] = !hasErrors;
            result["message"] = hasErrors
                ? $"Compilation completed with {compilationResult["errorCount"]} error(s)"
                : $"Compilation completed successfully after {elapsedSeconds:F1}s";
            
            return result;
        }
        
        #endregion
        
        #region Helper Methods
        
        private bool WaitForCompilationToComplete(float maxWaitSeconds)
        {
            if (!EditorApplication.isCompiling)
            {
                return true;
            }
            
            var startTime = EditorApplication.timeSinceStartup;
            var checkInterval = 0.2f; // Check every 200ms
            
            while ((EditorApplication.timeSinceStartup - startTime) < maxWaitSeconds)
            {
                if (!EditorApplication.isCompiling)
                {
                    return true;
                }
                
                System.Threading.Thread.Sleep((int)(checkInterval * 1000));
            }
            
            return false;
        }
        
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

