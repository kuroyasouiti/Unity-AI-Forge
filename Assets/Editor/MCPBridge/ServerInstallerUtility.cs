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

        private static string _templatePath;
        private static string _pyProjectSourcePath;

        public static string TemplatePath
        {
            get
            {
                if (_templatePath == null)
                {
                    _templatePath = FindTemplatePath();
                }
                return _templatePath;
            }
        }

        private static string PyProjectSourcePath
        {
            get
            {
                if (_pyProjectSourcePath == null)
                {
                    _pyProjectSourcePath = FindPyProjectPath();
                }
                return _pyProjectSourcePath;
            }
        }

        private static string FindTemplatePath()
        {
            var assetsPath = Application.dataPath;
            if (string.IsNullOrEmpty(assetsPath))
            {
                return null;
            }

            // Assets/Runtime/MCPServer を探す
            var candidates = new[]
            {
                Path.Combine(assetsPath, "Runtime", "MCPServer"),
                Path.Combine(assetsPath, "Plugins", "MCPServer"),
                Path.Combine(assetsPath, "MCPServer"),
            };

            foreach (var candidate in candidates)
            {
                if (Directory.Exists(candidate))
                {
                    var hasMainPy = File.Exists(Path.Combine(candidate, "main.py"));
                    var hasInitPy = File.Exists(Path.Combine(candidate, "__init__.py"));
                    if (hasMainPy || hasInitPy)
                    {
                        return candidate;
                    }
                }
            }

            // 見つからない場合は、Assets配下を再帰的に検索
            try
            {
                var foundPaths = Directory.GetDirectories(assetsPath, "MCPServer", SearchOption.AllDirectories);
                foreach (var foundPath in foundPaths)
                {
                    var hasMainPy = File.Exists(Path.Combine(foundPath, "main.py"));
                    var hasInitPy = File.Exists(Path.Combine(foundPath, "__init__.py"));
                    if (hasMainPy || hasInitPy)
                    {
                        return foundPath;
                    }
                }
            }
            catch (Exception)
            {
                // 検索中にエラーが発生した場合は無視
            }

            return null;
        }

        private static string FindPyProjectPath()
        {
            var templatePath = TemplatePath;
            if (string.IsNullOrEmpty(templatePath))
            {
                return null;
            }

            // MCPServerの親ディレクトリ(通常はRuntime)にあるpyproject.tomlを探す
            var parentDir = Directory.GetParent(templatePath)?.FullName;
            if (!string.IsNullOrEmpty(parentDir))
            {
                var pyProjectPath = Path.Combine(parentDir, "pyproject.toml");
                if (File.Exists(pyProjectPath))
                {
                    return pyProjectPath;
                }
            }

            // テンプレートディレクトリ自体にあるpyproject.tomlを探す
            var templatePyProject = Path.Combine(templatePath, "pyproject.toml");
            if (File.Exists(templatePyProject))
            {
                return templatePyProject;
            }

            return null;
        }

        public static void ResetCache()
        {
            _templatePath = null;
            _pyProjectSourcePath = null;
        }

        public static string GetTemplateSearchInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine("Template Search Information:");
            info.AppendLine($"Assets Path: {Application.dataPath}");
            info.AppendLine($"Found Template Path: {TemplatePath ?? "Not Found"}");
            info.AppendLine($"Found PyProject Path: {PyProjectSourcePath ?? "Not Found"}");

            if (!string.IsNullOrEmpty(TemplatePath))
            {
                info.AppendLine($"main.py exists: {File.Exists(Path.Combine(TemplatePath, "main.py"))}");
                info.AppendLine($"__init__.py exists: {File.Exists(Path.Combine(TemplatePath, "__init__.py"))}");
            }

            return info.ToString();
        }

        public static bool InstallTemplate(string destinationPath, out string message)
        {
            if (string.IsNullOrEmpty(TemplatePath))
            {
                message = "Template server not found. Please ensure the MCPServer directory exists in Assets/Runtime/MCPServer.";
                return false;
            }

            if (!Directory.Exists(TemplatePath))
            {
                message = $"Template server directory not found at: {TemplatePath}";
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
