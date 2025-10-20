using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor
{
    internal static class ServerInstallerUtility
    {
        private static readonly string[] IgnoredDirectories =
        {
            "node_modules",
            "dist",
            ".git",
            ".venv",
            "__pycache__"
        };

        public static string TemplatePath => Path.Combine("..", "..", "Runtime", "MCPServer");
        private static string PyProjectSourcePath => Path.Combine("..", "..", "Runtime", "pyproject.toml");

        public static bool InstallTemplate(string destinationPath, out string message)
        {
            if (!Directory.Exists(TemplatePath))
            {
                message = $"Template server not found: {TemplatePath}";
                return false;
            }

            try
            {
                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }

                CopyDirectory(new DirectoryInfo(TemplatePath), new DirectoryInfo(destinationPath));
                EnsureEnvFile(destinationPath);
                EnsurePyProject(destinationPath);
                message = $"Template copied to {destinationPath}";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Failed to install template: {ex.Message}";
                return false;
            }
        }

        public static bool HasPyProject(string path)
        {
            var pyProject = Path.Combine(path, "pyproject.toml");
            return File.Exists(pyProject);
        }

        public static bool TryUninstall(string path, out string message, bool force = false)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                message = "Install path is empty.";
                return false;
            }

            string normalized;
            try
            {
                normalized = Path.GetFullPath(path.Trim());
            }
            catch (Exception ex)
            {
                message = $"Invalid install path: {ex.Message}";
                return false;
            }

            if (!Directory.Exists(normalized))
            {
                message = $"Install directory not found: {normalized}";
                return false;
            }

            if (!force && !HasPyProject(normalized))
            {
                message = "pyproject.toml not found. Refusing to uninstall to avoid deleting unrelated files.";
                return false;
            }

            var root = Path.GetPathRoot(normalized);
            if (!string.IsNullOrEmpty(root))
            {
                var trimmedPath = normalized.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var trimmedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (string.Equals(trimmedPath, trimmedRoot, StringComparison.OrdinalIgnoreCase))
                {
                    message = $"Refusing to delete root directory: {normalized}";
                    return false;
                }
            }

            try
            {
                FileUtil.DeleteFileOrDirectory(normalized);
                message = $"Removed server install directory: {normalized}";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Failed to remove install directory: {ex.Message}";
                return false;
            }
        }

        private static void CopyDirectory(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (var dir in source.GetDirectories())
            {
                if (Array.Exists(IgnoredDirectories, name => string.Equals(name, dir.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var targetSubDir = target.CreateSubdirectory(dir.Name);
                CopyDirectory(dir, targetSubDir);
            }

            foreach (var file in source.GetFiles())
            {
                if (string.Equals(file.Extension, ".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var targetFilePath = Path.Combine(target.FullName, file.Name);
                file.CopyTo(targetFilePath, overwrite: true);
            }
        }

        private static void EnsureEnvFile(string rootPath)
        {
            var examplePath = Path.Combine(rootPath, ".env.example");
            if (!File.Exists(examplePath))
            {
                return;
            }

            var envPath = Path.Combine(rootPath, ".env");
            if (File.Exists(envPath))
            {
                return;
            }

            File.Copy(examplePath, envPath);
        }

        private static void EnsurePyProject(string rootPath)
        {
            if (!File.Exists(PyProjectSourcePath))
            {
                return;
            }

            var destination = Path.Combine(rootPath, "pyproject.toml");
            if (File.Exists(destination))
            {
                return;
            }

            File.Copy(PyProjectSourcePath, destination);
        }
    }
}
