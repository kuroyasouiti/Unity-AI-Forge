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
    /// GameKitWaypointHandler unit tests (Phase 2).
    /// Tests waypoint path creation and path following functionality.
    /// </summary>
    [TestFixture]
    public class GameKitWaypointHandlerTests
    {
        private GameKitWaypointHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitWaypointHandler();
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
        public void Category_ShouldReturnGamekitWaypoint()
        {
            Assert.AreEqual("gamekitWaypoint", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("create", operations);
            Assert.Contains("update", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("addWaypoint", operations);
            Assert.Contains("removeWaypoint", operations);
            Assert.Contains("clearWaypoints", operations);
        }

        #endregion

        #region Create Operation Tests

        [Test]
        public void Execute_Create_ShouldAddWaypointComponent()
        {
            var go = CreateTestGameObject("TestWaypoint");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestWaypoint",
                ["waypointId"] = "patrol_path_001"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var waypoint = go.GetComponent<GameKitWaypoint>();
            Assert.IsNotNull(waypoint);
            Assert.AreEqual("patrol_path_001", waypoint.WaypointId);
        }

        [Test]
        public void Execute_Create_WithLoop_ShouldSetLoopMode()
        {
            var go = CreateTestGameObject("TestWaypointLoop");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestWaypointLoop",
                ["loop"] = true,
                ["moveSpeed"] = 5f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var waypoint = go.GetComponent<GameKitWaypoint>();
            Assert.IsNotNull(waypoint);
            Assert.AreEqual(5f, waypoint.MoveSpeed, 0.01f);
        }

        [Test]
        public void Execute_Create_WithWaypoints_ShouldAddInitialWaypoints()
        {
            var go = CreateTestGameObject("TestWaypointPoints");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestWaypointPoints",
                ["waypointPositions"] = new List<object>
                {
                    new Dictionary<string, object> { ["x"] = 0f, ["y"] = 0f, ["z"] = 0f },
                    new Dictionary<string, object> { ["x"] = 10f, ["y"] = 0f, ["z"] = 0f },
                    new Dictionary<string, object> { ["x"] = 10f, ["y"] = 0f, ["z"] = 10f }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var waypoint = go.GetComponent<GameKitWaypoint>();
            Assert.IsNotNull(waypoint);
        }

        #endregion

        #region AddWaypoint Operation Tests

        [Test]
        public void Execute_AddWaypoint_ShouldAddPointToPath()
        {
            var go = CreateTestGameObject("TestWaypointAdd");
            var waypoint = go.AddComponent<GameKitWaypoint>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "addWaypoint",
                ["targetPath"] = "TestWaypointAdd",
                ["position"] = new Dictionary<string, object>
                {
                    ["x"] = 5f,
                    ["y"] = 0f,
                    ["z"] = 10f
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion

        #region ClearWaypoints Operation Tests

        [Test]
        public void Execute_ClearWaypoints_ShouldRemoveAllPoints()
        {
            var go = CreateTestGameObject("TestWaypointClear");
            var waypoint = go.AddComponent<GameKitWaypoint>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "clearWaypoints",
                ["targetPath"] = "TestWaypointClear"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnWaypointInfo()
        {
            var go = CreateTestGameObject("TestWaypointInspect");
            var waypoint = go.AddComponent<GameKitWaypoint>();
            waypoint.WaypointId = "inspect_path";

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["targetPath"] = "TestWaypointInspect"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            var waypointInfo = result["waypoint"] as Dictionary<string, object>;
            Assert.IsNotNull(waypointInfo);
            Assert.AreEqual("inspect_path", waypointInfo["waypointId"]);
        }

        #endregion

        #region Delete Operation Tests

        [Test]
        public void Execute_Delete_ShouldRemoveWaypointComponent()
        {
            var go = CreateTestGameObject("TestWaypointDelete");
            go.AddComponent<GameKitWaypoint>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["targetPath"] = "TestWaypointDelete"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(go.GetComponent<GameKitWaypoint>());
        }

        #endregion
    }
}
