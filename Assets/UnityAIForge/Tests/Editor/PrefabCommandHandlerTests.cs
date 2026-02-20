using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class PrefabCommandHandlerTests
    {
        private PrefabCommandHandler _handler;
        private GameObjectTracker _tracker;
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _handler = new PrefabCommandHandler();
            _tracker = new GameObjectTracker();
            _tempDir = TestUtilities.CreateTempAssetDirectory();
        }

        [TearDown]
        public void TearDown()
        {
            _tracker.Dispose();
            TestUtilities.CleanupTempAssetDirectory(_tempDir);
        }

        [Test]
        public void Category_ReturnsPrefab()
        {
            Assert.AreEqual("prefab", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("create", ops);
            Assert.Contains("update", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("instantiate", ops);
            Assert.Contains("unpack", ops);
        }

        [Test]
        public void Execute_NullPayload_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(null));
        }

        [Test]
        public void Execute_UnsupportedOperation_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(TestUtilities.CreatePayload("nonExistent")), "not supported");
        }

        [Test]
        public void Create_FromGameObject_ReturnsSuccess()
        {
            var go = _tracker.Create("PrefabSource");
            var prefabPath = $"{_tempDir}/TestPrefab.prefab";
            var result = _handler.Execute(TestUtilities.CreatePayload("create",
                ("gameObjectPath", "PrefabSource"),
                ("prefabPath", prefabPath)));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void Inspect_CreatedPrefab_ReturnsDetails()
        {
            var go = _tracker.Create("InspectPrefab");
            var prefabPath = $"{_tempDir}/InspectPrefab.prefab";
            _handler.Execute(TestUtilities.CreatePayload("create",
                ("gameObjectPath", "InspectPrefab"),
                ("prefabPath", prefabPath)));

            var result = _handler.Execute(TestUtilities.CreatePayload("inspect",
                ("prefabPath", prefabPath)));
            TestUtilities.AssertSuccess(result);
        }
    }
}
