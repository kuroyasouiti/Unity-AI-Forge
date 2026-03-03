using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Utilities
{
    /// <summary>
    /// File-based persistence for UI states, replacing EditorPrefs which has a 32KB per-key limit.
    /// States are stored as JSON files under Library/UnityAIForge/UIStates/.
    /// </summary>
    internal static class UIStatePersistence
    {
        private const string StateDir = "Library/UnityAIForge/UIStates";
        private const string GroupDir = "Library/UnityAIForge/UIStateGroups";
        private const string ActiveDir = "Library/UnityAIForge/UIStateActive";

        // Legacy EditorPrefs key prefixes for migration
        private const string LegacyStatePrefix = "UIState_";
        private const string LegacyRegistryPrefix = "UIStateRegistry_";
        private const string LegacyGroupPrefix = "UIStateGroup_";
        private const string LegacyActivePrefix = "UIStateActive_";

        #region Path Encoding

        /// <summary>
        /// Encodes a root path to a safe filename component.
        /// Uses Uri.EscapeDataString to avoid collisions (/ → %2F, _ stays as _).
        /// </summary>
        private static string EncodePath(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath)) return "root";
            return Uri.EscapeDataString(rootPath);
        }

        private static string GetStateFilePath(string rootPath, string stateName)
        {
            return Path.Combine(StateDir, $"{EncodePath(rootPath)}_{Uri.EscapeDataString(stateName)}.json");
        }

        private static string GetGroupFilePath(string rootPath, string groupName)
        {
            return Path.Combine(GroupDir, $"{EncodePath(rootPath)}_{Uri.EscapeDataString(groupName)}.json");
        }

        private static string GetActiveFilePath(string rootPath)
        {
            return Path.Combine(ActiveDir, $"{EncodePath(rootPath)}.txt");
        }

        #endregion

        #region State CRUD

        public static void SaveState(string rootPath, string stateName, string json)
        {
            MigrateIfNeeded(rootPath, stateName);
            var filePath = GetStateFilePath(rootPath, stateName);
            EnsureDirectory(filePath);
            File.WriteAllText(filePath, json);
        }

        public static bool HasState(string rootPath, string stateName)
        {
            MigrateIfNeeded(rootPath, stateName);
            return File.Exists(GetStateFilePath(rootPath, stateName));
        }

        public static string LoadState(string rootPath, string stateName)
        {
            MigrateIfNeeded(rootPath, stateName);
            var filePath = GetStateFilePath(rootPath, stateName);
            if (!File.Exists(filePath)) return null;
            return File.ReadAllText(filePath);
        }

        public static void DeleteState(string rootPath, string stateName)
        {
            var filePath = GetStateFilePath(rootPath, stateName);
            if (File.Exists(filePath)) File.Delete(filePath);

            // Also clean up legacy EditorPrefs if present
            var legacyKey = LegacyStatePrefix + LegacyEncodePath(rootPath) + "_" + stateName;
            if (EditorPrefs.HasKey(legacyKey)) EditorPrefs.DeleteKey(legacyKey);
        }

        /// <summary>
        /// Lists all state names for a given rootPath by scanning the state directory.
        /// </summary>
        public static List<string> ListStateNames(string rootPath)
        {
            MigrateAllForRoot(rootPath);
            var result = new List<string>();
            if (!Directory.Exists(StateDir)) return result;

            var prefix = EncodePath(rootPath) + "_";
            foreach (var file in Directory.GetFiles(StateDir, "*.json"))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.StartsWith(prefix))
                {
                    var encodedName = fileName.Substring(prefix.Length);
                    try
                    {
                        result.Add(Uri.UnescapeDataString(encodedName));
                    }
                    catch
                    {
                        result.Add(encodedName);
                    }
                }
            }

            return result;
        }

        #endregion

        #region State Group

        public static void SaveGroup(string rootPath, string groupName, string json)
        {
            var filePath = GetGroupFilePath(rootPath, groupName);
            EnsureDirectory(filePath);
            File.WriteAllText(filePath, json);
        }

        public static string LoadGroup(string rootPath, string groupName)
        {
            var filePath = GetGroupFilePath(rootPath, groupName);
            if (!File.Exists(filePath)) return null;
            return File.ReadAllText(filePath);
        }

        #endregion

        #region Active State

        public static void SetActiveState(string rootPath, string stateName)
        {
            var filePath = GetActiveFilePath(rootPath);
            EnsureDirectory(filePath);
            File.WriteAllText(filePath, stateName);

            // Clean up legacy
            var legacyKey = LegacyActivePrefix + LegacyEncodePath(rootPath);
            if (EditorPrefs.HasKey(legacyKey)) EditorPrefs.DeleteKey(legacyKey);
        }

        public static string GetActiveState(string rootPath)
        {
            // Check file first
            var filePath = GetActiveFilePath(rootPath);
            if (File.Exists(filePath)) return File.ReadAllText(filePath).Trim();

            // Migrate from legacy
            var legacyKey = LegacyActivePrefix + LegacyEncodePath(rootPath);
            if (EditorPrefs.HasKey(legacyKey))
            {
                var value = EditorPrefs.GetString(legacyKey, "");
                if (!string.IsNullOrEmpty(value))
                {
                    SetActiveState(rootPath, value);
                    EditorPrefs.DeleteKey(legacyKey);
                }
                return value;
            }

            return "";
        }

        #endregion

        #region Migration

        /// <summary>
        /// Legacy path encoding that used Replace("/", "_"), kept only for migration.
        /// </summary>
        private static string LegacyEncodePath(string rootPath)
        {
            return string.IsNullOrEmpty(rootPath) ? "root" : rootPath.Replace("/", "_");
        }

        /// <summary>
        /// Migrates a single state from EditorPrefs to file if the legacy key exists.
        /// </summary>
        private static void MigrateIfNeeded(string rootPath, string stateName)
        {
            var legacyKey = LegacyStatePrefix + LegacyEncodePath(rootPath) + "_" + stateName;
            if (!EditorPrefs.HasKey(legacyKey)) return;

            var filePath = GetStateFilePath(rootPath, stateName);
            if (File.Exists(filePath))
            {
                // File already exists, just remove legacy
                EditorPrefs.DeleteKey(legacyKey);
                return;
            }

            var json = EditorPrefs.GetString(legacyKey);
            if (!string.IsNullOrEmpty(json))
            {
                EnsureDirectory(filePath);
                File.WriteAllText(filePath, json);
            }

            EditorPrefs.DeleteKey(legacyKey);
        }

        /// <summary>
        /// Migrates all states for a rootPath from the legacy EditorPrefs registry.
        /// </summary>
        private static void MigrateAllForRoot(string rootPath)
        {
            var legacyRegistryKey = LegacyRegistryPrefix +
                (string.IsNullOrEmpty(rootPath) ? "all" : rootPath.Replace("/", "_"));

            if (!EditorPrefs.HasKey(legacyRegistryKey)) return;

            var registry = EditorPrefs.GetString(legacyRegistryKey, "");
            if (!string.IsNullOrEmpty(registry))
            {
                var stateNames = registry.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var stateName in stateNames)
                {
                    MigrateIfNeeded(rootPath, stateName);
                }
            }

            EditorPrefs.DeleteKey(legacyRegistryKey);
        }

        #endregion

        #region Helpers

        private static void EnsureDirectory(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        #endregion
    }
}
