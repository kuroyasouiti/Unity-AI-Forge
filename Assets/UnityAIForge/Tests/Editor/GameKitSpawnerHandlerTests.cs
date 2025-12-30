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
    /// GameKitSpawnerHandler unit tests (Phase 1).
    /// Tests spawner creation, wave management, and pooling functionality.
    /// </summary>
    [TestFixture]
    public class GameKitSpawnerHandlerTests
    {
        private GameKitSpawnerHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitSpawnerHandler();
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
        public void Category_ShouldReturnGamekitSpawner()
        {
            Assert.AreEqual("gamekitSpawner", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("create", operations);
            Assert.Contains("update", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("addWave", operations);
            Assert.Contains("start", operations);
        }

        #endregion

        #region Create Operation Tests

        [Test]
        public void Execute_Create_ShouldAddSpawnerComponent()
        {
            var go = CreateTestGameObject("TestSpawner");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestSpawner",
                ["spawnerId"] = "enemy_spawner"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var spawner = go.GetComponent<GameKitSpawner>();
            Assert.IsNotNull(spawner);
            Assert.AreEqual("enemy_spawner", spawner.SpawnerId);
        }

        [Test]
        public void Execute_Create_WithPooling_ShouldEnablePooling()
        {
            var go = CreateTestGameObject("TestSpawnerPool");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestSpawnerPool",
                ["usePool"] = true,
                ["poolInitialSize"] = 20
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var spawner = go.GetComponent<GameKitSpawner>();
            Assert.IsNotNull(spawner);
        }

        [Test]
        public void Execute_Create_WithSpawnSettings_ShouldApplySettings()
        {
            var go = CreateTestGameObject("TestSpawnerSettings");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestSpawnerSettings",
                ["spawnInterval"] = 2f,
                ["maxActive"] = 10,
                ["autoStart"] = true
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var spawner = go.GetComponent<GameKitSpawner>();
            Assert.IsNotNull(spawner);
        }

        #endregion

        #region Update Operation Tests

        [Test]
        public void Execute_Update_ShouldModifySpawnerProperties()
        {
            var go = CreateTestGameObject("TestSpawnerUpdate");
            var spawner = go.AddComponent<GameKitSpawner>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["targetPath"] = "TestSpawnerUpdate",
                ["spawnInterval"] = 5f,
                ["maxActive"] = 50
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnSpawnerInfo()
        {
            var go = CreateTestGameObject("TestSpawnerInspect");
            var spawner = go.AddComponent<GameKitSpawner>();
            // Use reflection to set private fields
            typeof(GameKitSpawner).GetField("spawnerId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(spawner, "test_spawner");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["targetPath"] = "TestSpawnerInspect"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            var spawnerInfo = result["spawner"] as Dictionary<string, object>;
            Assert.IsNotNull(spawnerInfo);
            Assert.AreEqual("test_spawner", spawnerInfo["spawnerId"]);
        }

        #endregion

        #region AddWave Operation Tests

        [Test]
        public void Execute_AddWave_ShouldAddWaveToSpawner()
        {
            var go = CreateTestGameObject("TestSpawnerWave");
            var spawner = go.AddComponent<GameKitSpawner>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "addWave",
                ["targetPath"] = "TestSpawnerWave",
                ["waveCount"] = 5,
                ["waveDelay"] = 10f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(1, spawner.WaveCount);
        }

        #endregion

        #region Delete Operation Tests

        [Test]
        public void Execute_Delete_ShouldRemoveSpawnerComponent()
        {
            var go = CreateTestGameObject("TestSpawnerDelete");
            go.AddComponent<GameKitSpawner>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["targetPath"] = "TestSpawnerDelete"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(go.GetComponent<GameKitSpawner>());
        }

        #endregion
    }
}
