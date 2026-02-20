using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCP.Editor.Base;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// Base class for GameKit handler tests.
    /// Provides shared setup/teardown, execution helpers, and assertion methods
    /// for testing handlers that use code generation.
    /// </summary>
    public abstract class GameKitHandlerTestBase
    {
        protected GameObjectTracker Tracker { get; private set; }
        protected List<string> TrackedScriptPaths { get; private set; }
        protected string TempAssetDir { get; private set; }

        [SetUp]
        public virtual void SetUp()
        {
            Tracker = new GameObjectTracker();
            TrackedScriptPaths = new List<string>();
        }

        [TearDown]
        public virtual void TearDown()
        {
            Tracker?.Dispose();

            // Batch all asset deletions to avoid repeated .csproj sync (IOException sharing violation)
            bool needsBatch = (TrackedScriptPaths != null && TrackedScriptPaths.Count > 0)
                              || !string.IsNullOrEmpty(TempAssetDir);

            if (needsBatch)
                AssetDatabase.StartAssetEditing();

            try
            {
                // Clean up generated scripts
                foreach (var path in TrackedScriptPaths)
                {
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        AssetDatabase.DeleteAsset(path);
                    }
                }
                TrackedScriptPaths?.Clear();

                // Clean up temp asset dir
                if (!string.IsNullOrEmpty(TempAssetDir))
                {
                    TestUtilities.CleanupTempAssetDirectory(TempAssetDir);
                    TempAssetDir = null;
                }
            }
            finally
            {
                if (needsBatch)
                    AssetDatabase.StopAssetEditing();
            }
        }

        /// <summary>
        /// Executes a handler with the given operation and additional payload entries.
        /// </summary>
        protected object Execute(BaseCommandHandler handler, string operation, params (string key, object value)[] extras)
        {
            var payload = TestUtilities.CreatePayload(operation, extras);
            return handler.Execute(payload);
        }

        /// <summary>
        /// Tracks a GameObject for cleanup.
        /// </summary>
        protected GameObject TrackGameObject(GameObject go)
        {
            return Tracker.Track(go);
        }

        /// <summary>
        /// Tracks a script path for cleanup.
        /// </summary>
        protected void TrackScriptPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
                TrackedScriptPaths.Add(path);
        }

        /// <summary>
        /// Asserts that the result indicates success.
        /// </summary>
        protected void AssertSuccess(object result)
        {
            TestUtilities.AssertSuccess(result);
        }

        /// <summary>
        /// Asserts that the result indicates an error.
        /// </summary>
        protected void AssertError(object result, string expectedMessageContains = null)
        {
            TestUtilities.AssertError(result, expectedMessageContains);
        }

        /// <summary>
        /// Asserts that the result contains a scriptPath indicating code generation occurred.
        /// </summary>
        protected void AssertScriptGenerated(object result)
        {
            var dict = result as Dictionary<string, object>;
            Assert.IsNotNull(dict, "Result should be a Dictionary<string, object>");
            Assert.IsTrue(dict.ContainsKey("scriptPath"), "Result should contain 'scriptPath'");
            var scriptPath = dict["scriptPath"].ToString();
            Assert.IsFalse(string.IsNullOrEmpty(scriptPath), "scriptPath should not be empty");
            TrackScriptPath(scriptPath);
        }

        /// <summary>
        /// Asserts that the generated script contains the expected class name.
        /// </summary>
        protected void AssertScriptContainsClass(object result, string expectedClassName)
        {
            var dict = result as Dictionary<string, object>;
            Assert.IsNotNull(dict);
            Assert.IsTrue(dict.ContainsKey("className"), "Result should contain 'className'");
            Assert.AreEqual(expectedClassName, dict["className"].ToString());
        }

        /// <summary>
        /// Asserts that the handler's SupportedOperations contains the expected operation.
        /// </summary>
        protected void AssertOperationsContain(BaseCommandHandler handler, string operation)
        {
            Assert.IsTrue(
                handler.SupportedOperations.Contains(operation),
                $"SupportedOperations should contain '{operation}'. " +
                $"Available: {string.Join(", ", handler.SupportedOperations)}"
            );
        }

        /// <summary>
        /// Gets a value from a result dictionary.
        /// </summary>
        protected T GetResultValue<T>(object result, string key)
        {
            return TestUtilities.GetResultValue<T>(result, key);
        }
    }
}
