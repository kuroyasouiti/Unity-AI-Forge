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
    /// GameKitHealthHandler unit tests (Phase 1).
    /// Tests health system creation, damage, healing, and respawn functionality.
    /// </summary>
    [TestFixture]
    public class GameKitHealthHandlerTests
    {
        private GameKitHealthHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitHealthHandler();
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
        public void Category_ShouldReturnGamekitHealth()
        {
            Assert.AreEqual("gamekitHealth", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("create", operations);
            Assert.Contains("update", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("applyDamage", operations);
            Assert.Contains("heal", operations);
            Assert.Contains("kill", operations);
            Assert.Contains("respawn", operations);
            Assert.Contains("setInvincible", operations);
            Assert.Contains("findByHealthId", operations);
        }

        #endregion

        #region Create Operation Tests

        [Test]
        public void Execute_Create_ShouldAddHealthComponent()
        {
            var go = CreateTestGameObject("TestHealth");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestHealth",
                ["maxHealth"] = 100f,
                ["currentHealth"] = 100f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var health = go.GetComponent<GameKitHealth>();
            Assert.IsNotNull(health);
            Assert.AreEqual(100f, health.MaxHealth, 0.01f);
        }

        [Test]
        public void Execute_Create_WithHealthId_ShouldSetHealthId()
        {
            var go = CreateTestGameObject("TestHealthId");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestHealthId",
                ["healthId"] = "player_health",
                ["maxHealth"] = 150f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var health = go.GetComponent<GameKitHealth>();
            Assert.AreEqual("player_health", health.HealthId);
        }

        #endregion

        #region Update Operation Tests

        [Test]
        public void Execute_Update_ShouldModifyHealthProperties()
        {
            var go = CreateTestGameObject("TestHealthUpdate");
            var health = go.AddComponent<GameKitHealth>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["targetPath"] = "TestHealthUpdate",
                ["maxHealth"] = 200f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(200f, health.MaxHealth, 0.01f);
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnHealthInfo()
        {
            var go = CreateTestGameObject("TestHealthInspect");
            var health = go.AddComponent<GameKitHealth>();
            // Use reflection to set private fields
            typeof(GameKitHealth).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(health, 100f);
            typeof(GameKitHealth).GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(health, 75f);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["targetPath"] = "TestHealthInspect"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            var healthInfo = result["health"] as Dictionary<string, object>;
            Assert.IsNotNull(healthInfo);
            Assert.AreEqual(100f, (float)healthInfo["maxHealth"], 0.01f);
            Assert.AreEqual(75f, (float)healthInfo["currentHealth"], 0.01f);
        }

        #endregion

        #region ApplyDamage Operation Tests

        [Test]
        public void Execute_ApplyDamage_ShouldReduceHealth()
        {
            var go = CreateTestGameObject("TestHealthDamage");
            var health = go.AddComponent<GameKitHealth>();
            // Use reflection to set private fields
            typeof(GameKitHealth).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(health, 100f);
            typeof(GameKitHealth).GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(health, 100f);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "applyDamage",
                ["targetPath"] = "TestHealthDamage",
                ["amount"] = 30f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(70f, health.CurrentHealth, 0.01f);
        }

        #endregion

        #region Heal Operation Tests

        [Test]
        public void Execute_Heal_ShouldIncreaseHealth()
        {
            var go = CreateTestGameObject("TestHealthHeal");
            var health = go.AddComponent<GameKitHealth>();
            // Use reflection to set private fields
            typeof(GameKitHealth).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(health, 100f);
            typeof(GameKitHealth).GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(health, 50f);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "heal",
                ["targetPath"] = "TestHealthHeal",
                ["amount"] = 25f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(75f, health.CurrentHealth, 0.01f);
        }

        #endregion

        #region Kill Operation Tests

        [Test]
        public void Execute_Kill_ShouldSetHealthToZero()
        {
            var go = CreateTestGameObject("TestHealthKill");
            var health = go.AddComponent<GameKitHealth>();
            // Use reflection to set private fields
            typeof(GameKitHealth).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(health, 100f);
            typeof(GameKitHealth).GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(health, 100f);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "kill",
                ["targetPath"] = "TestHealthKill"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(0f, health.CurrentHealth, 0.01f);
        }

        #endregion

        #region SetInvincible Operation Tests

        [Test]
        public void Execute_SetInvincible_ShouldSetInvincibleState()
        {
            var go = CreateTestGameObject("TestHealthInvincible");
            var health = go.AddComponent<GameKitHealth>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "setInvincible",
                ["targetPath"] = "TestHealthInvincible",
                ["invincible"] = true
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(health.IsInvincible);
        }

        #endregion

        #region Delete Operation Tests

        [Test]
        public void Execute_Delete_ShouldRemoveHealthComponent()
        {
            var go = CreateTestGameObject("TestHealthDelete");
            go.AddComponent<GameKitHealth>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["targetPath"] = "TestHealthDelete"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(go.GetComponent<GameKitHealth>());
        }

        #endregion
    }
}
