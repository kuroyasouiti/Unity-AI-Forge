using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace MCP.Editor
{
    /// <summary>
    /// Manages port discovery files that enable Python MCP servers to find the
    /// correct bridge port when multiple Unity projects run simultaneously.
    /// Port files are stored in %TEMP%/unity-ai-forge/{project-hash}.port.
    /// </summary>
    internal static class PortDiscoveryFile
    {
        private const string DirectoryName = "unity-ai-forge";

        /// <summary>
        /// Returns the discovery directory path: %TEMP%/unity-ai-forge/
        /// </summary>
        public static string GetDiscoveryDirectory()
            => Path.Combine(Path.GetTempPath(), DirectoryName);

        /// <summary>
        /// Computes a deterministic hash for the project path.
        /// Normalization: backslash to forward slash, lowercase, strip trailing slash, SHA256 first 16 hex chars.
        /// This algorithm must be identical in C# and Python.
        /// </summary>
        public static string ComputeProjectHash(string projectPath)
        {
            var normalized = projectPath.Replace("\\", "/").ToLowerInvariant().TrimEnd('/');
            var bytes = Encoding.UTF8.GetBytes(normalized);
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(bytes);
            var sb = new StringBuilder(16);
            for (int i = 0; i < 8; i++)
                sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }

        /// <summary>
        /// Returns the full path of the port file for the current project.
        /// </summary>
        public static string GetPortFilePath()
            => Path.Combine(GetDiscoveryDirectory(),
                ComputeProjectHash(GetProjectRoot()) + ".port");

        /// <summary>
        /// Writes the port file atomically (write to .tmp then rename).
        /// JSON content: port, projectPath, pid, timestamp (ISO 8601 UTC).
        /// </summary>
        public static void WritePortFile(int port)
        {
            try
            {
                var dir = GetDiscoveryDirectory();
                Directory.CreateDirectory(dir);

                var filePath = GetPortFilePath();
                var tmpPath = filePath + ".tmp";
                var projectRoot = GetProjectRoot();
                var pid = Process.GetCurrentProcess().Id;
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                var json = $"{{\"port\":{port},\"projectPath\":\"{EscapeJsonString(projectRoot)}\",\"pid\":{pid},\"timestamp\":\"{timestamp}\"}}";

                File.WriteAllText(tmpPath, json, Encoding.UTF8);

                if (File.Exists(filePath))
                    File.Delete(filePath);
                File.Move(tmpPath, filePath);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"MCP Bridge: Failed to write port file: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes the port file for the current project.
        /// </summary>
        public static void DeletePortFile()
        {
            try
            {
                var filePath = GetPortFilePath();
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"MCP Bridge: Failed to delete port file: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads and validates a port file. Returns null if the file is missing,
        /// invalid, or references a dead process (stale file is deleted).
        /// </summary>
        public static int? ReadValidPort(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                var json = File.ReadAllText(filePath, Encoding.UTF8);
                var data = MiniJson.Deserialize(json) as System.Collections.Generic.Dictionary<string, object>;
                if (data == null)
                    return null;

                if (!data.TryGetValue("port", out var portObj))
                    return null;
                var port = Convert.ToInt32(portObj);

                if (data.TryGetValue("pid", out var pidObj))
                {
                    var pid = Convert.ToInt32(pidObj);
                    if (!IsProcessAlive(pid))
                    {
                        try { File.Delete(filePath); } catch { /* ignored */ }
                        return null;
                    }
                }

                return port;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool IsProcessAlive(int pid)
        {
            try
            {
                var process = Process.GetProcessById(pid);
                return !process.HasExited;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static string GetProjectRoot()
            => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static string EscapeJsonString(string value)
            => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
