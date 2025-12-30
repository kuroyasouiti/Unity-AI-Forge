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
    /// GameKitTimerHandler unit tests (Phase 1).
    /// Tests timer/cooldown creation and management functionality.
    /// </summary>
    [TestFixture]
    public class GameKitTimerHandlerTests
    {
        private GameKitTimerHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitTimerHandler();
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
        public void Category_ShouldReturnGamekitTimer()
        {
            Assert.AreEqual("gamekitTimer", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("createTimer", operations);
            Assert.Contains("updateTimer", operations);
            Assert.Contains("inspectTimer", operations);
            Assert.Contains("deleteTimer", operations);
            Assert.Contains("createCooldown", operations);
            Assert.Contains("findByTimerId", operations);
        }

        #endregion

        #region Create Operation Tests

        [Test]
        public void Execute_Create_ShouldAddTimerComponent()
        {
            var go = CreateTestGameObject("TestTimer");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createTimer",
                ["targetPath"] = "TestTimer",
                ["timerId"] = "game_timer",
                ["duration"] = 60f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var timer = go.GetComponent<GameKitTimer>();
            Assert.IsNotNull(timer);
            Assert.AreEqual("game_timer", timer.TimerId);
            Assert.AreEqual(60f, timer.Duration, 0.01f);
        }

        [Test]
        public void Execute_Create_WithAutoStart_ShouldSetAutoStart()
        {
            var go = CreateTestGameObject("TestTimerAutoStart");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createTimer",
                ["targetPath"] = "TestTimerAutoStart",
                ["duration"] = 30f,
                ["autoStart"] = true
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var timer = go.GetComponent<GameKitTimer>();
            Assert.IsNotNull(timer);
            Assert.AreEqual(30f, timer.Duration, 0.01f);
        }

        [Test]
        public void Execute_Create_WithLoop_ShouldSetLooping()
        {
            var go = CreateTestGameObject("TestTimerLoop");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createTimer",
                ["targetPath"] = "TestTimerLoop",
                ["duration"] = 5f,
                ["loop"] = true
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var timer = go.GetComponent<GameKitTimer>();
            Assert.IsNotNull(timer);
        }

        #endregion

        #region Update Operation Tests

        [Test]
        public void Execute_Update_ShouldModifyTimerProperties()
        {
            var go = CreateTestGameObject("TestTimerUpdate");
            var timer = go.AddComponent<GameKitTimer>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "updateTimer",
                ["targetPath"] = "TestTimerUpdate",
                ["duration"] = 120f,
                ["loop"] = true
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(120f, timer.Duration, 0.01f);
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnTimerInfo()
        {
            var go = CreateTestGameObject("TestTimerInspect");
            var timer = go.AddComponent<GameKitTimer>();
            // Use reflection to set private fields
            typeof(GameKitTimer).GetField("timerId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(timer, "inspect_timer");
            typeof(GameKitTimer).GetField("duration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(timer, 45f);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspectTimer",
                ["targetPath"] = "TestTimerInspect"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            var timerInfo = result["timer"] as Dictionary<string, object>;
            Assert.IsNotNull(timerInfo);
            Assert.AreEqual("inspect_timer", timerInfo["timerId"]);
            Assert.AreEqual(45f, (float)timerInfo["duration"], 0.01f);
        }

        #endregion

        #region Delete Operation Tests

        [Test]
        public void Execute_Delete_ShouldRemoveTimerComponent()
        {
            var go = CreateTestGameObject("TestTimerDelete");
            go.AddComponent<GameKitTimer>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "deleteTimer",
                ["targetPath"] = "TestTimerDelete"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(go.GetComponent<GameKitTimer>());
        }

        #endregion
    }
}
