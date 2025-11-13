using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor
{
    /// <summary>
    /// Manages persistence of pending commands that need to be executed after compilation.
    /// Uses a JSON file to survive Unity Editor compilation and assembly reload.
    /// </summary>
    internal static class McpPendingCommandStorage
    {
        private const string StorageFileName = "Library/McpPendingCommands.json";

        [Serializable]
        private class PendingCommandData
        {
            public string commandId;
            public string toolName;
            public string payloadJson;
            public long enqueueTimestamp;
        }

        [Serializable]
        private class PendingCommandList
        {
            public List<PendingCommandData> commands = new List<PendingCommandData>();
        }

        /// <summary>
        /// Saves a command to be executed after compilation completes.
        /// </summary>
        /// <param name="command">The command to save</param>
        public static void SavePendingCommand(McpIncomingCommand command)
        {
            try
            {
                var commandList = LoadCommandList();

                var commandData = new PendingCommandData
                {
                    commandId = command.CommandId,
                    toolName = command.ToolName,
                    payloadJson = MiniJson.Serialize(command.Payload),
                    enqueueTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                commandList.commands.Add(commandData);
                SaveCommandList(commandList);

                Debug.Log($"MCP: Saved pending command {command.CommandId} ({command.ToolName}) to execute after compilation");
            }
            catch (Exception ex)
            {
                Debug.LogError($"MCP: Failed to save pending command: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves and removes all pending commands from storage.
        /// </summary>
        /// <returns>List of pending commands, or empty list if none exist</returns>
        public static List<McpIncomingCommand> GetAndClearPendingCommands()
        {
            var result = new List<McpIncomingCommand>();

            try
            {
                var commandList = LoadCommandList();

                foreach (var data in commandList.commands)
                {
                    try
                    {
                        var payloadObj = MiniJson.Deserialize(data.payloadJson);
                        var payload = payloadObj as Dictionary<string, object> ?? new Dictionary<string, object>();
                        var command = new McpIncomingCommand(data.commandId, data.toolName, payload);
                        result.Add(command);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"MCP: Failed to deserialize pending command {data.commandId}: {ex.Message}");
                    }
                }

                // Clear the storage file after reading
                if (result.Count > 0)
                {
                    ClearStorage();
                    Debug.Log($"MCP: Retrieved {result.Count} pending command(s) for execution");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"MCP: Failed to retrieve pending commands: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Checks if there are any pending commands in storage.
        /// </summary>
        public static bool HasPendingCommands()
        {
            try
            {
                if (!File.Exists(StorageFileName))
                {
                    return false;
                }

                var commandList = LoadCommandList();
                return commandList.commands.Count > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Clears all pending commands from storage.
        /// </summary>
        public static void ClearStorage()
        {
            try
            {
                if (File.Exists(StorageFileName))
                {
                    File.Delete(StorageFileName);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"MCP: Failed to clear pending command storage: {ex.Message}");
            }
        }

        private static PendingCommandList LoadCommandList()
        {
            if (!File.Exists(StorageFileName))
            {
                return new PendingCommandList();
            }

            var json = File.ReadAllText(StorageFileName);
            return JsonUtility.FromJson<PendingCommandList>(json) ?? new PendingCommandList();
        }

        private static void SaveCommandList(PendingCommandList commandList)
        {
            var directory = Path.GetDirectoryName(StorageFileName);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonUtility.ToJson(commandList, prettyPrint: true);
            File.WriteAllText(StorageFileName, json);
        }
    }
}
