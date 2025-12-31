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
    /// GameKitAIHandler unit tests (Phase 1).
    /// Tests AI behavior creation and management (Patrol/Chase/Flee).
    /// </summary>
    [TestFixture]
    public class GameKitAIHandlerTests
    {
        private GameKitAIHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitAIHandler();
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
        public void Category_ShouldReturnGamekitAI()
        {
            Assert.AreEqual("gamekitAI", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("create", operations);
            Assert.Contains("update", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("setState", operations);
            Assert.Contains("setTarget", operations);
            Assert.Contains("addPatrolPoint", operations);
            Assert.Contains("clearPatrolPoints", operations);
        }

        #endregion

        #region Create Operation Tests

        [Test]
        public void Execute_Create_ShouldAddAIComponent()
        {
            var go = CreateTestGameObject("TestAI");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestAI",
                ["aiId"] = "enemy_ai"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var ai = go.GetComponent<GameKitAIBehavior>();
            Assert.IsNotNull(ai);
            Assert.AreEqual("enemy_ai", ai.AIId);
        }

        [Test]
        public void Execute_Create_WithBehavior_ShouldSetBehavior()
        {
            var go = CreateTestGameObject("TestAIPatrol");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestAIPatrol",
                ["behaviorType"] = "Patrol",
                ["moveSpeed"] = 5f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var ai = go.GetComponent<GameKitAIBehavior>();
            Assert.AreEqual(GameKitAIBehavior.AIBehaviorType.Patrol, ai.BehaviorType);
            Assert.AreEqual(5f, ai.MoveSpeed, 0.01f);
        }

        [Test]
        public void Execute_Create_ChaseMode_ShouldSetChaseSettings()
        {
            var go = CreateTestGameObject("TestAIChase");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestAIChase",
                ["behaviorType"] = "Chase",
                ["detectionRadius"] = 15f,
                ["moveSpeed"] = 8f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var ai = go.GetComponent<GameKitAIBehavior>();
            Assert.AreEqual(GameKitAIBehavior.AIBehaviorType.Chase, ai.BehaviorType);
        }

        #endregion

        #region SetState Operation Tests

        [Test]
        public void Execute_SetState_ShouldChangeState()
        {
            var go = CreateTestGameObject("TestAISetState");
            var ai = go.AddComponent<GameKitAIBehavior>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "setState",
                ["targetPath"] = "TestAISetState",
                ["state"] = "Flee"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion

        #region SetTarget Operation Tests

        [Test]
        public void Execute_SetTarget_ShouldSetTargetTransform()
        {
            var go = CreateTestGameObject("TestAITarget");
            var ai = go.AddComponent<GameKitAIBehavior>();
            var target = CreateTestGameObject("TargetObject");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "setTarget",
                ["targetPath"] = "TestAITarget",
                ["chaseTargetPath"] = "TargetObject"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(target.transform, ai.ChaseTarget);
        }

        #endregion

        #region AddPatrolPoint Operation Tests

        [Test]
        public void Execute_AddPatrolPoint_ShouldAddPoint()
        {
            var go = CreateTestGameObject("TestAIPatrolPoint");
            var ai = go.AddComponent<GameKitAIBehavior>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "addPatrolPoint",
                ["targetPath"] = "TestAIPatrolPoint",
                ["position"] = new Dictionary<string, object>
                {
                    ["x"] = 10f,
                    ["y"] = 0f,
                    ["z"] = 5f
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnAIInfo()
        {
            var go = CreateTestGameObject("TestAIInspect");
            var ai = go.AddComponent<GameKitAIBehavior>();
            // Use reflection to set private fields
            typeof(GameKitAIBehavior).GetField("aiId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ai, "inspect_ai");
            typeof(GameKitAIBehavior).GetField("behaviorType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ai, GameKitAIBehavior.AIBehaviorType.Patrol);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["targetPath"] = "TestAIInspect"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            var aiInfo = result["ai"] as Dictionary<string, object>;
            Assert.IsNotNull(aiInfo);
            Assert.AreEqual("inspect_ai", aiInfo["aiId"]);
            Assert.AreEqual("Patrol", aiInfo["behaviorType"]);
        }

        #endregion

        #region Delete Operation Tests

        [Test]
        public void Execute_Delete_ShouldRemoveAIComponent()
        {
            var go = CreateTestGameObject("TestAIDelete");
            go.AddComponent<GameKitAIBehavior>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["targetPath"] = "TestAIDelete"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(go.GetComponent<GameKitAIBehavior>());
        }

        #endregion
    }
}
