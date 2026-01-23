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
    /// GameKitUISlotHandler unit tests (3-Pillar Architecture - UI).
    /// Tests UI slot creation, item assignment, and slot bar management.
    /// </summary>
    [TestFixture]
    public class GameKitUISlotHandlerTests
    {
        private GameKitUISlotHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitUISlotHandler();
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

        private void SetSerializedField(Component component, string fieldName, object value)
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                if (value is string strValue)
                    prop.stringValue = strValue;
                else if (value is int intValue)
                    prop.intValue = intValue;
                else if (value is float floatValue)
                    prop.floatValue = floatValue;
                else if (value is bool boolValue)
                    prop.boolValue = boolValue;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnGamekitUISlot()
        {
            Assert.AreEqual("gamekitUISlot", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("create", operations);
            Assert.Contains("update", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("setItem", operations);
            Assert.Contains("clearSlot", operations);
            Assert.Contains("setHighlight", operations);
            Assert.Contains("createSlotBar", operations);
            Assert.Contains("updateSlotBar", operations);
            Assert.Contains("inspectSlotBar", operations);
            Assert.Contains("deleteSlotBar", operations);
            Assert.Contains("useSlot", operations);
            Assert.Contains("refreshFromInventory", operations);
            Assert.Contains("findBySlotId", operations);
            Assert.Contains("findByBarId", operations);
        }

        #endregion

        #region Create Operation Tests

        [Test]
        public void Execute_Create_ShouldAddUISlotComponent()
        {
            var go = CreateTestGameObject("TestSlot");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestSlot",
                ["slotId"] = "test_slot"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var slot = go.GetComponent<GameKitUISlot>();
            Assert.IsNotNull(slot);
            Assert.AreEqual("test_slot", slot.SlotId);
        }

        [Test]
        public void Execute_Create_WithoutTargetPath_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion

        #region Delete Operation Tests

        [Test]
        public void Execute_Delete_ShouldRemoveComponent()
        {
            var go = CreateTestGameObject("TestSlotDelete");
            go.AddComponent<GameKitUISlot>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["targetPath"] = "TestSlotDelete"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(go.GetComponent<GameKitUISlot>());
        }

        #endregion

        #region ClearSlot Operation Tests

        [Test]
        public void Execute_ClearSlot_ShouldClearItem()
        {
            var go = CreateTestGameObject("TestSlotClear");
            var slot = go.AddComponent<GameKitUISlot>();
            SetSerializedField(slot, "slotId", "clear_test");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "clearSlot",
                ["targetPath"] = "TestSlotClear"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnSlotInfo()
        {
            var go = CreateTestGameObject("TestSlotInspect");
            var slot = go.AddComponent<GameKitUISlot>();
            SetSerializedField(slot, "slotId", "inspect_test");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["targetPath"] = "TestSlotInspect"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            var slotInfo = result["slot"] as Dictionary<string, object>;
            Assert.IsNotNull(slotInfo);
            Assert.AreEqual("inspect_test", slotInfo["slotId"]);
        }

        #endregion
    }
}
