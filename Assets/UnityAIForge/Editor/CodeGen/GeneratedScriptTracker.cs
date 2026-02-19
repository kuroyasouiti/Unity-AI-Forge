using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.CodeGen
{
    /// <summary>
    /// Editor-only ScriptableObject that tracks metadata for generated scripts.
    /// Used by handlers to inspect, update, and delete generated components.
    /// Stored inside the package directory — does not ship with user builds.
    /// </summary>
    public class GeneratedScriptTracker : ScriptableObject
    {
        private const string AssetPath = "Assets/UnityAIForge/Editor/GeneratedScriptTracker.asset";

        [Serializable]
        public class Entry
        {
            /// <summary>Logical ID provided by the handler (e.g. "player_hp").</summary>
            public string componentId;

            /// <summary>Generated C# class name (e.g. "PlayerHpHealth").</summary>
            public string className;

            /// <summary>Output file path relative to project (e.g. "Assets/Scripts/Generated/PlayerHpHealth.cs").</summary>
            public string scriptPath;

            /// <summary>Template name used to generate this script (e.g. "Health").</summary>
            public string templateName;

            /// <summary>Hierarchy path of the target GameObject at creation time.</summary>
            public string gameObjectPath;

            /// <summary>Serialized variables used for generation (JSON). Enables regeneration.</summary>
            public string variablesJson;

            /// <summary>True if the component needs to be attached after next compilation.</summary>
            public bool pendingAttach;
        }

        [SerializeField]
        private List<Entry> entries = new List<Entry>();

        /// <summary>Read-only access to all tracked entries.</summary>
        public IReadOnlyList<Entry> Entries => entries;

        #region Singleton

        private static GeneratedScriptTracker _instance;

        /// <summary>
        /// Gets the singleton instance, creating the asset if it doesn't exist.
        /// </summary>
        public static GeneratedScriptTracker Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                // Try to load existing asset
                _instance = AssetDatabase.LoadAssetAtPath<GeneratedScriptTracker>(AssetPath);
                if (_instance != null)
                    return _instance;

                // Create new asset
                _instance = CreateInstance<GeneratedScriptTracker>();
                // Ensure directory exists
                var dir = System.IO.Path.GetDirectoryName(AssetPath);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                AssetDatabase.CreateAsset(_instance, AssetPath);
                AssetDatabase.SaveAssets();
                return _instance;
            }
        }

        /// <summary>
        /// Resets the cached singleton reference. Useful for tests.
        /// </summary>
        internal static void ResetInstance()
        {
            _instance = null;
        }

        #endregion

        #region Query

        /// <summary>Finds an entry by its logical component ID.</summary>
        public Entry FindByComponentId(string componentId)
        {
            if (string.IsNullOrEmpty(componentId))
                return null;
            return entries.FirstOrDefault(e => e.componentId == componentId);
        }

        /// <summary>Finds an entry by its generated class name.</summary>
        public Entry FindByClassName(string className)
        {
            if (string.IsNullOrEmpty(className))
                return null;
            return entries.FirstOrDefault(e => e.className == className);
        }

        /// <summary>Finds an entry by its output script path.</summary>
        public Entry FindByScriptPath(string scriptPath)
        {
            if (string.IsNullOrEmpty(scriptPath))
                return null;
            return entries.FirstOrDefault(e => e.scriptPath == scriptPath);
        }

        /// <summary>Finds all entries that used a given template.</summary>
        public IEnumerable<Entry> FindByTemplateName(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
                return Enumerable.Empty<Entry>();
            return entries.Where(e => e.templateName == templateName);
        }

        /// <summary>Finds all entries with pending component attachment.</summary>
        public IEnumerable<Entry> FindPendingAttach()
        {
            return entries.Where(e => e.pendingAttach);
        }

        /// <summary>Checks whether a class name is already registered.</summary>
        public bool HasClassName(string className)
        {
            return FindByClassName(className) != null;
        }

        #endregion

        #region Mutation

        /// <summary>Registers a new generated script entry.</summary>
        public void Register(Entry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            // Remove existing entry with same componentId if present
            var existing = FindByComponentId(entry.componentId);
            if (existing != null)
            {
                entries.Remove(existing);
            }

            entries.Add(entry);
            Save();
        }

        /// <summary>Removes an entry by its component ID.</summary>
        public bool Unregister(string componentId)
        {
            var entry = FindByComponentId(componentId);
            if (entry == null)
                return false;

            entries.Remove(entry);
            Save();
            return true;
        }

        /// <summary>Removes all entries (for testing).</summary>
        internal void Clear()
        {
            entries.Clear();
            Save();
        }

        private void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        #endregion
    }
}
