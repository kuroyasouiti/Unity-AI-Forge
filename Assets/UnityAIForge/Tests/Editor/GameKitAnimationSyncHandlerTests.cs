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
    /// GameKitAnimationSyncHandler unit tests (Phase 3).
    /// Tests animation synchronization with game state.
    /// </summary>
    [TestFixture]
    public class GameKitAnimationSyncHandlerTests
    {
        private GameKitAnimationSyncHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitAnimationSyncHandler();
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
        public void Category_ShouldReturnGamekitAnimationSync()
        {
            Assert.AreEqual("gamekitAnimationSync", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("create", operations);
            Assert.Contains("update", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("addSyncRule", operations);
            Assert.Contains("removeSyncRule", operations);
            Assert.Contains("addTriggerRule", operations);
            Assert.Contains("removeTriggerRule", operations);
        }

        #endregion

        #region Create Operation Tests

        [Test]
        public void Execute_Create_ShouldAddAnimationSyncComponent()
        {
            var go = CreateTestGameObject("TestAnimSync");
            go.AddComponent<Animator>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestAnimSync",
                ["syncId"] = "player_anim_sync"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var sync = go.GetComponent<GameKitAnimationSync>();
            Assert.IsNotNull(sync);
            Assert.AreEqual("player_anim_sync", sync.SyncId);
        }

        [Test]
        public void Execute_Create_WithSyncRules_ShouldAddRules()
        {
            var go = CreateTestGameObject("TestAnimSyncRules");
            go.AddComponent<Animator>();
            go.AddComponent<Rigidbody>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestAnimSyncRules",
                ["syncRules"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["parameterName"] = "Speed",
                        ["sourceType"] = "Rigidbody",
                        ["sourceProperty"] = "velocity.magnitude"
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var sync = go.GetComponent<GameKitAnimationSync>();
            Assert.AreEqual(1, sync.SyncRules.Count);
        }

        #endregion

        #region AddSyncRule Operation Tests

        [Test]
        public void Execute_AddSyncRule_ShouldAddRuleToComponent()
        {
            var go = CreateTestGameObject("TestAnimSyncAddRule");
            go.AddComponent<Animator>();
            var sync = go.AddComponent<GameKitAnimationSync>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "addSyncRule",
                ["targetPath"] = "TestAnimSyncAddRule",
                ["parameterName"] = "IsGrounded",
                ["sourceType"] = "Transform",
                ["sourceProperty"] = "position.y"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(1, sync.SyncRules.Count);
        }

        #endregion

        #region AddTriggerRule Operation Tests

        [Test]
        public void Execute_AddTriggerRule_ShouldAddTriggerRule()
        {
            var go = CreateTestGameObject("TestAnimSyncTrigger");
            go.AddComponent<Animator>();
            var sync = go.AddComponent<GameKitAnimationSync>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "addTriggerRule",
                ["targetPath"] = "TestAnimSyncTrigger",
                ["eventName"] = "OnDamaged",
                ["triggerParameter"] = "TakeDamage"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(1, sync.TriggerRules.Count);
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnAnimSyncInfo()
        {
            var go = CreateTestGameObject("TestAnimSyncInspect");
            go.AddComponent<Animator>();
            var sync = go.AddComponent<GameKitAnimationSync>();
            // SyncId is read-only, set via Initialize
            typeof(GameKitAnimationSync).GetField("syncId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(sync, "inspect_sync");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["targetPath"] = "TestAnimSyncInspect"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("inspect_sync", result["syncId"]);
            Assert.IsTrue(result.ContainsKey("syncRules"));
            Assert.IsTrue(result.ContainsKey("triggerRules"));
        }

        #endregion

        #region Delete Operation Tests

        [Test]
        public void Execute_Delete_ShouldRemoveAnimSyncComponent()
        {
            var go = CreateTestGameObject("TestAnimSyncDelete");
            go.AddComponent<Animator>();
            go.AddComponent<GameKitAnimationSync>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["targetPath"] = "TestAnimSyncDelete"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(go.GetComponent<GameKitAnimationSync>());
        }

        #endregion
    }
}
