using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using UnityEditor;
using UnityEngine;

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
            "getState"
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
    }
}
