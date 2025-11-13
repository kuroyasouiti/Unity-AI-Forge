using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace MCP.Editor
{
    internal readonly struct CommandResult
    {
        public CommandResult(bool success, int exitCode, string output, string error)
        {
            Success = success;
            ExitCode = exitCode;
            Output = output;
            Error = error;
        }

        public bool Success { get; }
        public int ExitCode { get; }
        public string Output { get; }
        public string Error { get; }
    }

    internal static class ProcessHelper
    {
        public static void RunShellCommandAsync(string command, string workingDirectory, Action<CommandResult> callback)
        {
            Task.Run(() =>
            {
                var result = RunShellCommand(command, workingDirectory);
                EditorApplication.delayCall += () => callback?.Invoke(result);
            });
        }

        private static CommandResult RunShellCommand(string command, string workingDirectory)
        {
            try
            {
                var effectiveWorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory)
                    ? Directory.GetCurrentDirectory()
                    : workingDirectory;
                var startInfo = CreateProcessStartInfo(command, effectiveWorkingDirectory);
                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return new CommandResult(false, -1, string.Empty, "Failed to start process.");
                }

                var output = new StringBuilder();
                var error = new StringBuilder();

                process.OutputDataReceived += (_, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        output.AppendLine(args.Data);
                    }
                };

                process.ErrorDataReceived += (_, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        error.AppendLine(args.Data);
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                return new CommandResult(process.ExitCode == 0, process.ExitCode, output.ToString(), error.ToString());
            }
            catch (Exception ex)
            {
                return new CommandResult(false, -1, string.Empty, ex.Message);
            }
        }

        public static bool TryGetExecutablePath(string executableName, out string fullPath)
        {
            fullPath = string.Empty;
            if (string.IsNullOrWhiteSpace(executableName))
            {
                return false;
            }

            var probeNames = BuildProbeNames(executableName);
            var comparer = GetPathComparer();
            var visited = new HashSet<string>(comparer);

            foreach (var directory in GetSearchDirectories())
            {
                if (string.IsNullOrWhiteSpace(directory))
                {
                    continue;
                }

                string normalizedDirectory;
                try
                {
                    normalizedDirectory = Path.GetFullPath(directory);
                }
                catch
                {
                    continue;
                }

                if (!visited.Add(normalizedDirectory))
                {
                    continue;
                }

                foreach (var probe in probeNames)
                {
                    string candidate;
                    try
                    {
                        candidate = Path.Combine(normalizedDirectory, probe);
                    }
                    catch
                    {
                        continue;
                    }

                    if (File.Exists(candidate))
                    {
                        fullPath = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        private static ProcessStartInfo CreateProcessStartInfo(string command, string workingDirectory)
        {
#if UNITY_EDITOR_WIN
            return new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
#else
            var escapedCommand = command.Replace("\"", "\\\"");
            return new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-lc \"{escapedCommand}\"",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
#endif
        }

        private static IReadOnlyList<string> BuildProbeNames(string executableName)
        {
#if UNITY_EDITOR_WIN
            if (Path.HasExtension(executableName))
            {
                return new[] { executableName };
            }

            return new[]
            {
                $"{executableName}.exe",
                $"{executableName}.cmd",
                $"{executableName}.bat",
                $"{executableName}.ps1"
            };
#else
            return new[] { executableName };
#endif
        }

        private static IEnumerable<string> GetSearchDirectories()
        {
            var pathVariable = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathVariable))
            {
                var segments = pathVariable.Split(Path.PathSeparator);
                foreach (var segment in segments)
                {
                    var trimmed = segment?.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        yield return trimmed;
                    }
                }
            }

#if UNITY_EDITOR_WIN
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(userProfile))
            {
                yield return Path.Combine(userProfile, ".local", "bin");
            }

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrEmpty(localAppData))
            {
                yield return Path.Combine(localAppData, "Programs", "uv");
                yield return Path.Combine(localAppData, "Microsoft", "WinGet", "Packages", "astral-sh.uv_Microsoft.Winget.Source_8wekyb3d8bbwe");
            }
#else
            var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            if (!string.IsNullOrEmpty(home))
            {
                yield return Path.Combine(home, ".local", "bin");
            }
#endif
        }

        private static StringComparer GetPathComparer()
        {
#if UNITY_EDITOR_WIN
            return StringComparer.OrdinalIgnoreCase;
#else
            return StringComparer.Ordinal;
#endif
        }
    }
}
