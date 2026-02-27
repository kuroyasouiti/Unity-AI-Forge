using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MCP.Editor.Base;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// PlayMode Control Handler: Controls Unity Editor play mode for testing games.
    /// Enables LLMs to execute and test games autonomously.
    /// </summary>
    public class PlayModeControlHandler : BaseCommandHandler
    {
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "play",
            "pause",
            "unpause",
            "stop",
            "step",
            "getState",
            "captureState",
            "waitForScene"
        };

        public override string Category => "playModeControl";
        public override string Version => "1.0.0";

        protected override bool RequiresCompilationWait(string operation)
        {
            // PlayMode operations don't require compilation wait
            return false;
        }

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "play" => HandlePlay(payload),
                "pause" => HandlePause(payload),
                "unpause" => HandleUnpause(payload),
                "stop" => HandleStop(payload),
                "step" => HandleStep(payload),
                "getState" => HandleGetState(payload),
                "captureState" => HandleCaptureState(payload),
                "waitForScene" => HandleWaitForScene(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }

        /// <summary>
        /// Start play mode.
        /// </summary>
        private object HandlePlay(Dictionary<string, object> payload)
        {
            if (EditorApplication.isPlaying)
            {
                return CreateSuccessResponse(
                    ("message", "Already in play mode"),
                    ("isPlaying", true),
                    ("isPaused", EditorApplication.isPaused)
                );
            }

            // Check if compilation is in progress
            if (EditorApplication.isCompiling)
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "Cannot enter play mode while compiling. Wait for compilation to complete.",
                    ["isCompiling"] = true
                };
            }

            EditorApplication.isPlaying = true;

            return CreateSuccessResponse(
                ("message", "Play mode started"),
                ("isPlaying", true),
                ("isPaused", false)
            );
        }

        /// <summary>
        /// Pause play mode.
        /// </summary>
        private object HandlePause(Dictionary<string, object> payload)
        {
            if (!EditorApplication.isPlaying)
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "Not in play mode. Start play mode first.",
                    ["isPlaying"] = false
                };
            }

            if (EditorApplication.isPaused)
            {
                return CreateSuccessResponse(
                    ("message", "Already paused"),
                    ("isPlaying", true),
                    ("isPaused", true)
                );
            }

            EditorApplication.isPaused = true;

            return CreateSuccessResponse(
                ("message", "Play mode paused"),
                ("isPlaying", true),
                ("isPaused", true)
            );
        }

        /// <summary>
        /// Unpause (resume) play mode.
        /// </summary>
        private object HandleUnpause(Dictionary<string, object> payload)
        {
            if (!EditorApplication.isPlaying)
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "Not in play mode. Start play mode first.",
                    ["isPlaying"] = false
                };
            }

            if (!EditorApplication.isPaused)
            {
                return CreateSuccessResponse(
                    ("message", "Not paused"),
                    ("isPlaying", true),
                    ("isPaused", false)
                );
            }

            EditorApplication.isPaused = false;

            return CreateSuccessResponse(
                ("message", "Play mode resumed"),
                ("isPlaying", true),
                ("isPaused", false)
            );
        }

        /// <summary>
        /// Stop play mode.
        /// </summary>
        private object HandleStop(Dictionary<string, object> payload)
        {
            if (!EditorApplication.isPlaying)
            {
                return CreateSuccessResponse(
                    ("message", "Already stopped"),
                    ("isPlaying", false),
                    ("isPaused", false)
                );
            }

            EditorApplication.isPlaying = false;

            return CreateSuccessResponse(
                ("message", "Play mode stopped"),
                ("isPlaying", false),
                ("isPaused", false)
            );
        }

        /// <summary>
        /// Step one frame (while paused).
        /// </summary>
        private object HandleStep(Dictionary<string, object> payload)
        {
            if (!EditorApplication.isPlaying)
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "Not in play mode. Start play mode first.",
                    ["isPlaying"] = false
                };
            }

            if (!EditorApplication.isPaused)
            {
                // Auto-pause before stepping
                EditorApplication.isPaused = true;
            }

            EditorApplication.Step();

            return CreateSuccessResponse(
                ("message", "Stepped one frame"),
                ("isPlaying", true),
                ("isPaused", true)
            );
        }

        /// <summary>
        /// Get current play mode state.
        /// </summary>
        private object HandleGetState(Dictionary<string, object> payload)
        {
            string state;
            if (!EditorApplication.isPlaying)
            {
                state = "stopped";
            }
            else if (EditorApplication.isPaused)
            {
                state = "paused";
            }
            else
            {
                state = "playing";
            }

            return CreateSuccessResponse(
                ("state", state),
                ("isPlaying", EditorApplication.isPlaying),
                ("isPaused", EditorApplication.isPaused),
                ("isCompiling", EditorApplication.isCompiling),
                ("timeSinceStartup", Time.realtimeSinceStartup)
            );
        }

        /// <summary>
        /// Capture runtime state of specified GameObjects. Requires play mode.
        /// </summary>
        private object HandleCaptureState(Dictionary<string, object> payload)
        {
            if (!EditorApplication.isPlaying)
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "captureState requires play mode. Start play mode first.",
                    ["isPlaying"] = false
                };
            }

            var targetPaths = GetStringList(payload, "targets");
            bool includeConsole = GetBool(payload, "includeConsole", false);

            // Active scenes
            var activeScenes = new List<Dictionary<string, object>>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                activeScenes.Add(new Dictionary<string, object>
                {
                    ["name"] = scene.name,
                    ["path"] = scene.path,
                    ["isLoaded"] = scene.isLoaded
                });
            }

            // Target GameObjects state
            var targetsResult = new List<Dictionary<string, object>>();
            if (targetPaths != null)
            {
                foreach (var path in targetPaths)
                {
                    var go = GameObject.Find(path);
                    if (go == null)
                    {
                        targetsResult.Add(new Dictionary<string, object>
                        {
                            ["path"] = path,
                            ["found"] = false
                        });
                        continue;
                    }

                    var pos = go.transform.position;
                    var rot = go.transform.rotation.eulerAngles;
                    var componentNames = go.GetComponents<Component>()
                        .Where(c => c != null)
                        .Select(c => c.GetType().Name)
                        .ToList();

                    targetsResult.Add(new Dictionary<string, object>
                    {
                        ["path"] = path,
                        ["found"] = true,
                        ["activeInHierarchy"] = go.activeInHierarchy,
                        ["position"] = new Dictionary<string, object> { ["x"] = pos.x, ["y"] = pos.y, ["z"] = pos.z },
                        ["rotation"] = new Dictionary<string, object> { ["x"] = rot.x, ["y"] = rot.y, ["z"] = rot.z },
                        ["components"] = componentNames
                    });
                }
            }

            var result = new Dictionary<string, object>
            {
                ["success"] = true,
                ["state"] = "captured",
                ["isPlaying"] = true,
                ["isPaused"] = EditorApplication.isPaused,
                ["activeScenes"] = activeScenes,
                ["frameCount"] = Time.frameCount,
                ["time"] = Time.time,
                ["targets"] = targetsResult
            };

            // Console summary
            if (includeConsole)
            {
                result["console"] = GetConsoleSummary();
            }

            return result;
        }

        /// <summary>
        /// Check if a scene is loaded by name or path. AI client polls until loaded=true.
        /// </summary>
        private object HandleWaitForScene(Dictionary<string, object> payload)
        {
            var sceneName = GetString(payload, "sceneName");
            if (string.IsNullOrEmpty(sceneName))
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "sceneName is required for waitForScene operation."
                };
            }

            // Check all loaded scenes
            bool loaded = false;
            var activeScenes = new List<Dictionary<string, object>>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                activeScenes.Add(new Dictionary<string, object>
                {
                    ["name"] = scene.name,
                    ["path"] = scene.path,
                    ["isLoaded"] = scene.isLoaded
                });

                if (scene.isLoaded && (scene.name == sceneName || scene.path == sceneName))
                {
                    loaded = true;
                }
            }

            return CreateSuccessResponse(
                ("sceneName", sceneName),
                ("loaded", loaded),
                ("activeScenes", activeScenes),
                ("message", loaded ? $"Scene '{sceneName}' is loaded" : $"Scene '{sceneName}' is not yet loaded"),
                ("isPlaying", EditorApplication.isPlaying)
            );
        }

        /// <summary>
        /// Get console log summary (errors/warnings/logs counts) via reflection.
        /// </summary>
        private Dictionary<string, object> GetConsoleSummary()
        {
            var summary = new Dictionary<string, object>
            {
                ["errors"] = 0,
                ["warnings"] = 0,
                ["logs"] = 0
            };

            try
            {
                var logEntriesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntries");
                if (logEntriesType != null)
                {
                    var getCountsByType = logEntriesType.GetMethod("GetCountsByType",
                        BindingFlags.Public | BindingFlags.Static);
                    if (getCountsByType != null)
                    {
                        var parameters = new object[] { 0, 0, 0 };
                        getCountsByType.Invoke(null, parameters);
                        summary["errors"] = (int)parameters[0];
                        summary["warnings"] = (int)parameters[1];
                        summary["logs"] = (int)parameters[2];
                    }
                }
            }
            catch (Exception)
            {
                // Ignore reflection errors
            }

            return summary;
        }
    }
}
