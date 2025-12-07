using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor
{
    internal static class McpBridgeMessages
    {
        public static Dictionary<string, object> CreateHelloPayload(string sessionId)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "hello",
                ["sessionId"] = sessionId,
                ["unityVersion"] = Application.unityVersion,
                ["projectName"] = Application.productName,
            };
        }

        public static Dictionary<string, object> CreateHeartbeat()
        {
            return new Dictionary<string, object>
            {
                ["type"] = "heartbeat",
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            };
        }

        public static Dictionary<string, object> CreateContextUpdate(Dictionary<string, object> payload)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "context:update",
                ["payload"] = payload,
            };
        }

        public static Dictionary<string, object> CreateCommandResult(string commandId, bool ok, object result, string errorMessage = null)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "command:result",
                ["commandId"] = commandId,
                ["ok"] = ok,
                ["result"] = result,
                ["errorMessage"] = errorMessage,
            };
        }

        public static Dictionary<string, object> CreateCompilationComplete(Dictionary<string, object> compilationResult)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "compilation:complete",
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ["result"] = compilationResult,
            };
        }

        public static Dictionary<string, object> CreateBridgeRestarted(string reason)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "bridge:restarted",
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ["reason"] = reason,
                ["sessionId"] = McpBridgeService.SessionId,
            };
        }
    }

    internal sealed class McpIncomingCommand
    {
        public string CommandId { get; }
        public string ToolName { get; }
        public Dictionary<string, object> Payload { get; }

        public McpIncomingCommand(string commandId, string toolName, Dictionary<string, object> payload)
        {
            CommandId = commandId;
            ToolName = toolName;
            Payload = payload ?? new Dictionary<string, object>();
        }

        public static bool TryParse(object message, out McpIncomingCommand command)
        {
            command = null;
            if (message is not Dictionary<string, object> map)
            {
                return false;
            }

            if (!map.TryGetValue("type", out var typeObj) || !string.Equals(typeObj as string, "command:execute", StringComparison.Ordinal))
            {
                return false;
            }

            if (!map.TryGetValue("commandId", out var idObj) || idObj is not string commandId)
            {
                return false;
            }

            if (!map.TryGetValue("toolName", out var toolObj) || toolObj is not string toolName)
            {
                return false;
            }

            var payload = map.TryGetValue("payload", out var payloadObj) && payloadObj is Dictionary<string, object> dict
                ? dict
                : new Dictionary<string, object>();

            command = new McpIncomingCommand(commandId, toolName, payload);
            return true;
        }
    }
}
