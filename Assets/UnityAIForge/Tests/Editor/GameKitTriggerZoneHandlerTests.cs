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
    /// GameKitTriggerZoneHandler unit tests (Phase 2).
    /// Tests trigger zone creation (checkpoint/damage/teleport, etc.).
    /// </summary>
    [TestFixture]
    public class GameKitTriggerZoneHandlerTests
    {
        private GameKitTriggerZoneHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitTriggerZoneHandler();
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
        public void Category_ShouldReturnGamekitTriggerZone()
        {
            Assert.AreEqual("gamekitTriggerZone", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("create", operations);
            Assert.Contains("update", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("activate", operations);
        }

        #endregion

        #region Create Operation Tests

        [Test]
        public void Execute_Create_ShouldAddTriggerZoneComponent()
        {
            var go = CreateTestGameObject("TestTriggerZone");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestTriggerZone",
                ["zoneId"] = "checkpoint_001",
                ["zoneType"] = "Checkpoint"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var zone = go.GetComponent<GameKitTriggerZone>();
            Assert.IsNotNull(zone);
            Assert.AreEqual("checkpoint_001", zone.ZoneId);
            Assert.AreEqual(GameKitTriggerZone.ZoneType.Checkpoint, zone.Type);
        }

        [Test]
        public void Execute_Create_DamageZone_ShouldSetDamageSettings()
        {
            var go = CreateTestGameObject("TestDamageZone");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestDamageZone",
                ["zoneType"] = "DamageZone",
                ["effectValue"] = 10f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var zone = go.GetComponent<GameKitTriggerZone>();
            Assert.AreEqual(GameKitTriggerZone.ZoneType.DamageZone, zone.Type);
            Assert.AreEqual(10f, zone.EffectValue, 0.01f);
        }

        [Test]
        public void Execute_Create_TeleportZone_ShouldSetDestination()
        {
            var go = CreateTestGameObject("TestTeleportZone");
            var destination = CreateTestGameObject("TeleportDestination");

            // First, create the teleport zone
            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestTeleportZone",
                ["zoneType"] = "Teleport"
            };

            var createResult = _handler.Execute(createPayload) as Dictionary<string, object>;
            Assert.IsNotNull(createResult);
            Assert.IsTrue((bool)createResult["success"]);

            // Then, set the teleport destination using setTeleportDestination operation
            var setDestPayload = new Dictionary<string, object>
            {
                ["operation"] = "setTeleportDestination",
                ["targetPath"] = "TestTeleportZone",
                ["destinationPath"] = "TeleportDestination"
            };

            var result = _handler.Execute(setDestPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var zone = go.GetComponent<GameKitTriggerZone>();
            Assert.AreEqual(GameKitTriggerZone.ZoneType.Teleport, zone.Type);
            Assert.AreEqual(destination.transform, zone.TeleportDestination);
        }

        [Test]
        public void Execute_Create_WithColliderSize_ShouldSetCollider()
        {
            var go = CreateTestGameObject("TestTriggerZoneCollider");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestTriggerZoneCollider",
                ["zoneType"] = "Checkpoint",
                ["colliderSize"] = new Dictionary<string, object>
                {
                    ["x"] = 5f,
                    ["y"] = 3f,
                    ["z"] = 5f
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var collider = go.GetComponent<BoxCollider>();
            Assert.IsNotNull(collider);
            Assert.IsTrue(collider.isTrigger);
            Assert.AreEqual(5f, collider.size.x, 0.01f);
        }

        #endregion

        #region Update Operation Tests

        [Test]
        public void Execute_Update_ShouldChangeZoneType()
        {
            var go = CreateTestGameObject("TestTriggerTypeChange");
            var zone = go.AddComponent<GameKitTriggerZone>();
            zone.Type = GameKitTriggerZone.ZoneType.Checkpoint;

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["targetPath"] = "TestTriggerTypeChange",
                ["zoneType"] = "HealZone"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(GameKitTriggerZone.ZoneType.HealZone, zone.Type);
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnTriggerZoneInfo()
        {
            var go = CreateTestGameObject("TestTriggerZoneInspect");
            var zone = go.AddComponent<GameKitTriggerZone>();
            zone.ZoneId = "zone_inspect";
            zone.Type = GameKitTriggerZone.ZoneType.DamageZone;
            zone.EffectValue = 15f;

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["targetPath"] = "TestTriggerZoneInspect"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            var zoneInfo = result["triggerZone"] as Dictionary<string, object>;
            Assert.IsNotNull(zoneInfo);
            Assert.AreEqual("zone_inspect", zoneInfo["zoneId"]);
            Assert.AreEqual("DamageZone", zoneInfo["zoneType"]);
        }

        #endregion

        #region Delete Operation Tests

        [Test]
        public void Execute_Delete_ShouldRemoveTriggerZoneComponent()
        {
            var go = CreateTestGameObject("TestTriggerZoneDelete");
            go.AddComponent<GameKitTriggerZone>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["targetPath"] = "TestTriggerZoneDelete"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(go.GetComponent<GameKitTriggerZone>());
        }

        #endregion
    }
}
