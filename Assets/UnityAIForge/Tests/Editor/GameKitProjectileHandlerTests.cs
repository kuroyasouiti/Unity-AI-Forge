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
    /// GameKitProjectileHandler unit tests (Phase 2).
    /// Tests projectile creation with homing functionality.
    /// </summary>
    [TestFixture]
    public class GameKitProjectileHandlerTests
    {
        private GameKitProjectileHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitProjectileHandler();
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
        public void Category_ShouldReturnGamekitProjectile()
        {
            Assert.AreEqual("gamekitProjectile", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("create", operations);
            Assert.Contains("update", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("launch", operations);
            Assert.Contains("setHomingTarget", operations);
        }

        #endregion

        #region Create Operation Tests

        [Test]
        public void Execute_Create_ShouldAddProjectileComponent()
        {
            var go = CreateTestGameObject("TestProjectile");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestProjectile",
                ["projectileId"] = "bullet_001",
                ["speed"] = 20f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var projectile = go.GetComponent<GameKitProjectile>();
            Assert.IsNotNull(projectile);
            Assert.AreEqual("bullet_001", projectile.ProjectileId);
        }

        [Test]
        public void Execute_Create_WithHoming_ShouldEnableHomingBehavior()
        {
            var go = CreateTestGameObject("TestProjectileHoming");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestProjectileHoming",
                ["speed"] = 15f,
                ["isHoming"] = true,
                ["homingStrength"] = 5f,
                ["homingRange"] = 10f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var projectile = go.GetComponent<GameKitProjectile>();
            Assert.IsNotNull(projectile);
        }

        [Test]
        public void Execute_Create_WithDamage_ShouldSetDamageSettings()
        {
            var go = CreateTestGameObject("TestProjectileDamage");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestProjectileDamage",
                ["speed"] = 25f,
                ["damage"] = 50f,
                ["lifetime"] = 5f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var projectile = go.GetComponent<GameKitProjectile>();
            Assert.IsNotNull(projectile);
        }

        #endregion

        #region SetTarget Operation Tests

        [Test]
        public void Execute_SetTarget_ShouldSetHomingTarget()
        {
            var go = CreateTestGameObject("TestProjectileTarget");
            var projectile = go.AddComponent<GameKitProjectile>();
            var target = CreateTestGameObject("Enemy");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "setHomingTarget",
                ["targetPath"] = "TestProjectileTarget",
                ["homingTargetPath"] = "Enemy"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnProjectileInfo()
        {
            var go = CreateTestGameObject("TestProjectileInspect");
            var projectile = go.AddComponent<GameKitProjectile>();
            projectile.ProjectileId = "missile_001";

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["targetPath"] = "TestProjectileInspect"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            var projectileInfo = result["projectile"] as Dictionary<string, object>;
            Assert.IsNotNull(projectileInfo);
            Assert.AreEqual("missile_001", projectileInfo["projectileId"]);
        }

        #endregion

        #region Delete Operation Tests

        [Test]
        public void Execute_Delete_ShouldRemoveProjectileComponent()
        {
            var go = CreateTestGameObject("TestProjectileDelete");
            go.AddComponent<GameKitProjectile>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["targetPath"] = "TestProjectileDelete"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(go.GetComponent<GameKitProjectile>());
        }

        #endregion
    }
}
