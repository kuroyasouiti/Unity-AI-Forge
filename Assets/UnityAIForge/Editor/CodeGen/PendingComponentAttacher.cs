using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.CodeGen
{
    /// <summary>
    /// Automatically attaches generated components to their target GameObjects
    /// after Unity recompiles. Uses [InitializeOnLoad] to run after domain reload.
    /// </summary>
    [InitializeOnLoad]
    internal static class PendingComponentAttacher
    {
        static PendingComponentAttacher()
        {
            // Delay one frame to ensure all assemblies are fully loaded
            EditorApplication.delayCall += ProcessPendingAttachments;
        }

        private static void ProcessPendingAttachments()
        {
            var tracker = GeneratedScriptTracker.Instance;
            if (tracker == null) return;

            // Materialize to avoid collection-modified exception
            var pending = tracker.FindPendingAttach().ToList();
            foreach (var entry in pending)
            {
                try
                {
                    AttachComponent(entry, tracker);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PendingComponentAttacher] Failed to attach '{entry.className}' to '{entry.gameObjectPath}': {ex.Message}");
                }
            }
        }

        private static void AttachComponent(GeneratedScriptTracker.Entry entry, GeneratedScriptTracker tracker)
        {
            // Resolve the generated type
            var type = ScriptGenerator.ResolveGeneratedType(entry.className);
            if (type == null)
            {
                // Type not compiled yet — leave pending for next reload
                return;
            }

            // Find the target GameObject by stored path
            var go = FindGameObjectByPath(entry.gameObjectPath);
            if (go == null)
            {
                // GO was deleted or path changed — clear pending flag
                entry.pendingAttach = false;
                tracker.Register(entry);
                return;
            }

            // Check if component already exists
            if (go.GetComponent(type) != null)
            {
                entry.pendingAttach = false;
                tracker.Register(entry);
                return;
            }

            // Add the component
            var component = Undo.AddComponent(go, type);
            if (component != null)
            {
                Debug.Log($"[PendingComponentAttacher] Auto-attached '{entry.className}' to '{entry.gameObjectPath}'");
                EditorUtility.SetDirty(go);
            }

            // Clear the pending flag
            entry.pendingAttach = false;
            tracker.Register(entry);
        }

        private static GameObject FindGameObjectByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            // Try direct find by path from scene root
            var go = GameObject.Find(path);
            if (go != null)
                return go;

            // Try just the name (last segment) — handles root objects
            var lastSlash = path.LastIndexOf('/');
            if (lastSlash >= 0)
            {
                var name = path.Substring(lastSlash + 1);
                go = GameObject.Find(name);
            }

            return go;
        }
    }
}
