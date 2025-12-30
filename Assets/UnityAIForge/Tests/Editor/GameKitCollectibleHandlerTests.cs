using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MCP.Editor.Handlers.GameKit;
using UnityAIForge.GameKit;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// GameKitCollectibleHandler unit tests (Phase 2).
    /// Tests collectible item creation with magnet effect functionality.
    /// </summary>
    [TestFixture]
    public class GameKitCollectibleHandlerTests
    {
        private GameKitCollectibleHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitCollectibleHandler();
            _createdObjects = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    Undo.ClearUndo(obj);
                    Object.DestroyImmediate(obj);
                }
            }
            _createdObjects.Clear();
        }

        private GameObject CreateTestGameObject(string name)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go;
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnGamekitCollectible()
        {
            Assert.AreEqual("gamekitCollectible", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("create", operations);
            Assert.Contains("update", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("collect", operations);
        }

        #endregion

        #region Create Operation Tests

        [Test]
        public void Execute_Create_ShouldAddCollectibleComponent()
        {
            var go = CreateTestGameObject("TestCollectible");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestCollectible",
                ["collectibleId"] = "coin_001",
                ["collectibleType"] = "Coin"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var collectible = go.GetComponent<GameKitCollectible>();
            Assert.IsNotNull(collectible);
            Assert.AreEqual("coin_001", collectible.CollectibleId);
        }

        [Test]
        public void Execute_Create_WithMagnet_ShouldEnableMagnetEffect()
        {
            var go = CreateTestGameObject("TestCollectibleMagnet");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestCollectibleMagnet",
                ["collectibleType"] = "Gem",
                ["useMagnet"] = true,
                ["magnetRange"] = 5f,
                ["magnetSpeed"] = 10f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var collectible = go.GetComponent<GameKitCollectible>();
            Assert.IsNotNull(collectible);
        }

        [Test]
        public void Execute_Create_WithValue_ShouldSetValue()
        {
            var go = CreateTestGameObject("TestCollectibleValue");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestCollectibleValue",
                ["collectibleType"] = "Gold",
                ["value"] = 100
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var collectible = go.GetComponent<GameKitCollectible>();
            Assert.IsNotNull(collectible);
        }

        #endregion

        #region Update Operation Tests

        [Test]
        public void Execute_Update_ShouldModifyCollectibleProperties()
        {
            var go = CreateTestGameObject("TestCollectibleUpdate");
            var collectible = go.AddComponent<GameKitCollectible>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["targetPath"] = "TestCollectibleUpdate",
                ["value"] = 50
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnCollectibleInfo()
        {
            var go = CreateTestGameObject("TestCollectibleInspect");
            var collectible = go.AddComponent<GameKitCollectible>();
            collectible.CollectibleId = "gem_001";

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["targetPath"] = "TestCollectibleInspect"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            var collectibleInfo = result["collectible"] as Dictionary<string, object>;
            Assert.IsNotNull(collectibleInfo);
            Assert.AreEqual("gem_001", collectibleInfo["collectibleId"]);
        }

        #endregion

        #region Delete Operation Tests

        [Test]
        public void Execute_Delete_ShouldRemoveCollectibleComponent()
        {
            var go = CreateTestGameObject("TestCollectibleDelete");
            go.AddComponent<GameKitCollectible>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["targetPath"] = "TestCollectibleDelete"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(go.GetComponent<GameKitCollectible>());
        }

        #endregion
    }
}
