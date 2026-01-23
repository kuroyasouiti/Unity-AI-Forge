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
    /// GameKitUISelectionHandler unit tests (3-Pillar Architecture - UI).
    /// Tests UI selection menu creation and item management.
    /// </summary>
    [TestFixture]
    public class GameKitUISelectionHandlerTests
    {
        private GameKitUISelectionHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitUISelectionHandler();
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
        public void Category_ShouldReturnGamekitUISelection()
        {
            Assert.AreEqual("gamekitUISelection", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("create", operations);
            Assert.Contains("update", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("setItems", operations);
            Assert.Contains("addItem", operations);
            Assert.Contains("removeItem", operations);
            Assert.Contains("clear", operations);
            Assert.Contains("selectItem", operations);
            Assert.Contains("selectItemById", operations);
            Assert.Contains("deselectItem", operations);
            Assert.Contains("clearSelection", operations);
            Assert.Contains("setSelectionActions", operations);
            Assert.Contains("setItemEnabled", operations);
            Assert.Contains("findBySelectionId", operations);
        }

        #endregion

        #region Create Operation Tests

        [Test]
        public void Execute_Create_ShouldAddUISelectionComponent()
        {
            var go = CreateTestGameObject("TestSelection");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestSelection",
                ["selectionId"] = "test_selection"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var selection = go.GetComponent<GameKitUISelection>();
            Assert.IsNotNull(selection);
            Assert.AreEqual("test_selection", selection.SelectionId);
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
            var go = CreateTestGameObject("TestSelectionDelete");
            go.AddComponent<GameKitUISelection>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["targetPath"] = "TestSelectionDelete"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(go.GetComponent<GameKitUISelection>());
        }

        #endregion

        #region Clear Operation Tests

        [Test]
        public void Execute_Clear_ShouldClearItems()
        {
            var go = CreateTestGameObject("TestSelectionClear");
            var selection = go.AddComponent<GameKitUISelection>();
            SetSerializedField(selection, "selectionId", "clear_test");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "clear",
                ["targetPath"] = "TestSelectionClear"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnSelectionInfo()
        {
            var go = CreateTestGameObject("TestSelectionInspect");
            var selection = go.AddComponent<GameKitUISelection>();
            SetSerializedField(selection, "selectionId", "inspect_test");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["targetPath"] = "TestSelectionInspect"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            var selectionInfo = result["selection"] as Dictionary<string, object>;
            Assert.IsNotNull(selectionInfo);
            Assert.AreEqual("inspect_test", selectionInfo["selectionId"]);
        }

        #endregion
    }
}
