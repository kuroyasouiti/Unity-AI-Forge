using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// Shared test utilities for MCP Editor tests.
    /// </summary>
    public static class TestUtilities
    {
        /// <summary>
        /// Creates a payload dictionary with the given operation.
        /// </summary>
        public static Dictionary<string, object> CreatePayload(string operation, params (string key, object value)[] extras)
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = operation
            };
            foreach (var (key, value) in extras)
            {
                payload[key] = value;
            }
            return payload;
        }

        /// <summary>
        /// Creates a new GameObject and registers it for cleanup.
        /// </summary>
        public static GameObject CreateGameObject(string name = "TestObject")
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create test object");
            return go;
        }

        /// <summary>
        /// Creates a hierarchy of GameObjects: parent/child/grandchild.
        /// Returns the root.
        /// </summary>
        public static GameObject CreateHierarchy(string parentName, params string[] childNames)
        {
            var parent = CreateGameObject(parentName);
            foreach (var childName in childNames)
            {
                var child = CreateGameObject(childName);
                child.transform.SetParent(parent.transform);
            }
            return parent;
        }

        /// <summary>
        /// Gets the full hierarchy path for a GameObject.
        /// </summary>
        public static string GetHierarchyPath(GameObject go)
        {
            if (go == null) return string.Empty;
            var path = go.name;
            var parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        /// <summary>
        /// Asserts that a result dictionary indicates success.
        /// </summary>
        public static void AssertSuccess(object result)
        {
            Assert.IsNotNull(result, "Result should not be null");
            var dict = result as Dictionary<string, object>;
            Assert.IsNotNull(dict, "Result should be a Dictionary<string, object>");
            Assert.IsTrue(dict.ContainsKey("success"), "Result should contain 'success' key");
            Assert.IsTrue((bool)dict["success"], $"Expected success but got error: {(dict.ContainsKey("error") ? dict["error"] : "unknown")}");
        }

        /// <summary>
        /// Asserts that a result dictionary indicates failure.
        /// </summary>
        public static void AssertError(object result, string expectedMessageContains = null)
        {
            Assert.IsNotNull(result, "Result should not be null");
            var dict = result as Dictionary<string, object>;
            Assert.IsNotNull(dict, "Result should be a Dictionary<string, object>");
            Assert.IsTrue(dict.ContainsKey("success"), "Result should contain 'success' key");
            Assert.IsFalse((bool)dict["success"], "Expected failure but got success");
            if (expectedMessageContains != null)
            {
                Assert.IsTrue(dict.ContainsKey("error"), "Result should contain 'error' key");
                StringAssert.Contains(expectedMessageContains, dict["error"].ToString());
            }
        }

        /// <summary>
        /// Gets a value from a result dictionary.
        /// </summary>
        public static T GetResultValue<T>(object result, string key)
        {
            var dict = result as Dictionary<string, object>;
            Assert.IsNotNull(dict, "Result should be a Dictionary<string, object>");
            Assert.IsTrue(dict.ContainsKey(key), $"Result should contain '{key}' key");
            return (T)dict[key];
        }

        /// <summary>
        /// Creates a temporary directory under Assets for test assets.
        /// Returns the path relative to Assets (e.g., "Assets/_TestTemp_xxx").
        /// </summary>
        public static string CreateTempAssetDirectory()
        {
            var dirName = $"_TestTemp_{Guid.NewGuid():N}";
            var fullPath = Path.Combine(Application.dataPath, dirName);
            Directory.CreateDirectory(fullPath);
            AssetDatabase.Refresh();
            return $"Assets/{dirName}";
        }

        /// <summary>
        /// Cleans up a temporary asset directory.
        /// </summary>
        public static void CleanupTempAssetDirectory(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return;
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }
    }

    /// <summary>
    /// Tracks GameObjects created during a test for cleanup in TearDown.
    /// </summary>
    public class GameObjectTracker : IDisposable
    {
        private readonly List<GameObject> _tracked = new List<GameObject>();

        public GameObject Track(GameObject go)
        {
            if (go != null)
                _tracked.Add(go);
            return go;
        }

        public GameObject Create(string name = "TestObject")
        {
            var go = new GameObject(name);
            _tracked.Add(go);
            return go;
        }

        public void Dispose()
        {
            foreach (var go in _tracked)
            {
                if (go != null)
                    UnityEngine.Object.DestroyImmediate(go);
            }
            _tracked.Clear();
        }
    }
}
