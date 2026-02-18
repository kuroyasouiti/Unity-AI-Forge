using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// Base class for GameKit handler tests using the code-generation pattern.
    /// Provides common setup/teardown, test object management, and shared assertions.
    /// </summary>
    public abstract class GameKitHandlerTestBase
    {
        protected const string TestOutputDir = "Assets/TestTemp/Generated";
        private List<GameObject> _createdObjects;
        private List<string> _generatedFiles;
        private List<string> _generatedAssets;

        [SetUp]
        public void BaseSetUp()
        {
            _createdObjects = new List<GameObject>();
            _generatedFiles = new List<string>();
            _generatedAssets = new List<string>();
            if (!Directory.Exists(TestOutputDir))
                Directory.CreateDirectory(TestOutputDir);
        }

        [TearDown]
        public void BaseTearDown()
        {
            foreach (var file in _generatedFiles)
            {
                if (File.Exists(file))
                    AssetDatabase.DeleteAsset(file);
            }

            foreach (var asset in _generatedAssets)
            {
                if (File.Exists(asset))
                    AssetDatabase.DeleteAsset(asset);
            }

            if (Directory.Exists(TestOutputDir))
                AssetDatabase.DeleteAsset("Assets/TestTemp");

            TestUtilities.CleanupGameObjects(_createdObjects);
            GeneratedScriptTracker.ResetInstance();
        }

        protected GameObject CreateTestGameObject(string name)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go;
        }

        protected void TrackGameObject(GameObject go)
        {
            if (go != null && !_createdObjects.Contains(go))
                _createdObjects.Add(go);
        }

        /// <summary>
        /// Tracks a handler-created GameObject by name for cleanup.
        /// Call after create operations that auto-create GameObjects.
        /// </summary>
        protected void TrackCreatedGameObject(string name)
        {
            var go = GameObject.Find(name);
            if (go != null) TrackGameObject(go);
        }

        protected void TrackScriptPath(Dictionary<string, object> result)
        {
            if (result?.TryGetValue("scriptPath", out var p) == true && p != null)
                _generatedFiles.Add(p.ToString());
        }

        protected void TrackAssetPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
                _generatedAssets.Add(path);
        }

        /// <summary>
        /// Executes a handler operation with the given arguments.
        /// Automatically tracks generated script paths for cleanup.
        /// </summary>
        protected Dictionary<string, object> Execute(
            BaseCommandHandler handler,
            string operation,
            params (string key, object value)[] args)
        {
            var payload = new Dictionary<string, object> { ["operation"] = operation };
            foreach (var (k, v) in args)
                payload[k] = v;
            var result = handler.Execute(payload) as Dictionary<string, object>;
            TrackScriptPath(result);
            return result;
        }

        #region Assertions

        protected static void AssertSuccess(Dictionary<string, object> result)
        {
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsTrue(result.ContainsKey("success"), "Result should contain 'success' key");
            Assert.IsTrue((bool)result["success"],
                $"Expected success but got error: {(result.TryGetValue("error", out var e) ? e : "unknown")}");
        }

        protected static void AssertError(Dictionary<string, object> result)
        {
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsTrue(result.ContainsKey("success"), "Result should contain 'success' key");
            Assert.IsFalse((bool)result["success"], "Expected failure but got success");
        }

        protected static void AssertHasField(Dictionary<string, object> result, string key)
        {
            Assert.IsTrue(result.ContainsKey(key), $"Result should contain '{key}'");
        }

        protected static void AssertOperationsContain(
            IEnumerable<string> supportedOperations, params string[] expected)
        {
            var ops = new List<string>(supportedOperations);
            foreach (var op in expected)
                Assert.IsTrue(ops.Contains(op), $"SupportedOperations should contain '{op}'");
        }

        protected void AssertScriptGenerated(Dictionary<string, object> result)
        {
            AssertHasField(result, "scriptPath");
            var path = result["scriptPath"].ToString();
            Assert.IsTrue(File.Exists(path), $"Script should exist at: {path}");

            var content = File.ReadAllText(path);
            StringAssert.DoesNotContain("UnityAIForge", content,
                "Generated script must not reference UnityAIForge");
            StringAssert.DoesNotContain("MCP.Editor", content,
                "Generated script must not reference MCP.Editor");
        }

        protected void AssertScriptContainsClass(Dictionary<string, object> result, string className)
        {
            var content = File.ReadAllText(result["scriptPath"].ToString());
            StringAssert.Contains($"class {className}", content,
                $"Generated script should contain class {className}");
        }

        #endregion
    }
}
