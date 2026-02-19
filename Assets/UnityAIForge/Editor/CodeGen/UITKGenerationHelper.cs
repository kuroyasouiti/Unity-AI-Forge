using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MCP.Editor.CodeGen
{
    /// <summary>
    /// Utility methods for UI Toolkit asset generation (UXML/USS).
    /// Handles writing UI definition files, creating UIDocument GameObjects,
    /// and cleaning up generated UI assets.
    /// </summary>
    public static class UITKGenerationHelper
    {
        public const string DefaultUIOutputDir = "Assets/UI/Generated";
        private const string DefaultPanelSettingsPath = "Assets/UI/DefaultPanelSettings.asset";

        /// <summary>
        /// Writes a UXML file to disk and imports it into the AssetDatabase.
        /// </summary>
        /// <param name="outputDir">Directory to write to.</param>
        /// <param name="fileName">File name without extension.</param>
        /// <param name="uxmlContent">UXML content string.</param>
        /// <returns>Asset path of the written file (e.g. "Assets/UI/Generated/MyPanel.uxml").</returns>
        public static string WriteUXML(string outputDir, string fileName, string uxmlContent)
        {
            EnsureDirectoryExists(outputDir);
            var path = Path.Combine(outputDir, fileName + ".uxml").Replace("\\", "/");
            File.WriteAllText(path, uxmlContent, System.Text.Encoding.UTF8);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return path;
        }

        /// <summary>
        /// Writes a USS file to disk and imports it into the AssetDatabase.
        /// </summary>
        /// <param name="outputDir">Directory to write to.</param>
        /// <param name="fileName">File name without extension.</param>
        /// <param name="ussContent">USS content string.</param>
        /// <returns>Asset path of the written file (e.g. "Assets/UI/Generated/MyPanel.uss").</returns>
        public static string WriteUSS(string outputDir, string fileName, string ussContent)
        {
            EnsureDirectoryExists(outputDir);
            var path = Path.Combine(outputDir, fileName + ".uss").Replace("\\", "/");
            File.WriteAllText(path, ussContent, System.Text.Encoding.UTF8);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return path;
        }

        /// <summary>
        /// Creates a new GameObject with a UIDocument component configured with the given UXML/USS.
        /// </summary>
        /// <param name="name">GameObject name.</param>
        /// <param name="parent">Parent transform (can be null for root).</param>
        /// <param name="uxmlPath">Asset path to the UXML file.</param>
        /// <param name="ussPath">Asset path to the USS file (optional, can be null if USS is referenced from UXML).</param>
        /// <returns>The created GameObject.</returns>
        public static GameObject CreateUIDocumentGameObject(string name, Transform parent, string uxmlPath, string ussPath)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");

            if (parent != null)
                go.transform.SetParent(parent, false);

            ConfigureUIDocument(go, uxmlPath, ussPath);
            return go;
        }

        /// <summary>
        /// Ensures the GameObject has a UIDocument component configured with the given UXML/USS.
        /// If a UIDocument already exists, updates its assets.
        /// </summary>
        /// <param name="go">Target GameObject.</param>
        /// <param name="uxmlPath">Asset path to the UXML file.</param>
        /// <param name="ussPath">Asset path to the USS file (optional).</param>
        public static void EnsureUIDocument(GameObject go, string uxmlPath, string ussPath)
        {
            ConfigureUIDocument(go, uxmlPath, ussPath);
        }

        /// <summary>
        /// Deletes UXML and USS files associated with a component ID.
        /// Reads paths from the tracker's stored variables (UXML_PATH, USS_PATH keys).
        /// </summary>
        /// <param name="componentId">The component ID whose UI assets should be deleted.</param>
        public static void DeleteUIAssets(string componentId)
        {
            var tracker = GeneratedScriptTracker.Instance;
            var entry = tracker.FindByComponentId(componentId);
            if (entry == null) return;

            var vars = ScriptGenerator.DeserializeVariables(entry.variablesJson);

            if (vars.TryGetValue("UXML_PATH", out var uxmlObj) && uxmlObj is string uxmlPath)
            {
                if (File.Exists(uxmlPath))
                    AssetDatabase.DeleteAsset(uxmlPath);
            }

            if (vars.TryGetValue("USS_PATH", out var ussObj) && ussObj is string ussPath)
            {
                if (File.Exists(ussPath))
                    AssetDatabase.DeleteAsset(ussPath);
            }
        }

        private static void ConfigureUIDocument(GameObject go, string uxmlPath, string ussPath)
        {
            var uiDoc = go.GetComponent<UIDocument>();
            if (uiDoc == null)
                uiDoc = Undo.AddComponent<UIDocument>(go);
            else
                Undo.RecordObject(uiDoc, "Configure UIDocument");

            // Ensure PanelSettings is assigned
            if (uiDoc.panelSettings == null)
                uiDoc.panelSettings = GetOrCreateDefaultPanelSettings();

            if (!string.IsNullOrEmpty(uxmlPath))
            {
                var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
                if (visualTree != null)
                    uiDoc.visualTreeAsset = visualTree;
            }
        }

        /// <summary>
        /// Gets or creates a default PanelSettings asset for UIDocuments.
        /// </summary>
        public static PanelSettings GetOrCreateDefaultPanelSettings()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(DefaultPanelSettingsPath);
            if (existing != null)
                return existing;

            // Create default PanelSettings
            var dir = Path.GetDirectoryName(DefaultPanelSettingsPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;

            AssetDatabase.CreateAsset(panelSettings, DefaultPanelSettingsPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[UITKGenerationHelper] Created default PanelSettings at {DefaultPanelSettingsPath}");
            return panelSettings;
        }

        private static void EnsureDirectoryExists(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }
        }
    }
}
