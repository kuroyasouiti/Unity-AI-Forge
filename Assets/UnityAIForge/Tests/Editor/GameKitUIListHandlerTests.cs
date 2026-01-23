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
    /// GameKitUIListHandler unit tests (3-Pillar Architecture - UI).
    /// Tests UI list creation, item management, and selection.
    /// </summary>
    [TestFixture]
    public class GameKitUIListHandlerTests
    {
        private GameKitUIListHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitUIListHandler();
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
        public void Category_ShouldReturnGamekitUIList()
        {
            Assert.AreEqual("gamekitUIList", _handler.Category);
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
            Assert.Contains("deselectItem", operations);
            Assert.Contains("clearSelection", operations);
            Assert.Contains("refreshFromSource", operations);
            Assert.Contains("findByListId", operations);
        }

        #endregion

        #region Create Operation Tests

        [Test]
        public void Execute_Create_ShouldAddUIListComponent()
        {
            var go = CreateTestGameObject("TestList");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestList",
                ["listId"] = "test_list"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var list = go.GetComponent<GameKitUIList>();
            Assert.IsNotNull(list);
            Assert.AreEqual("test_list", list.ListId);
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
            var go = CreateTestGameObject("TestListDelete");
            go.AddComponent<GameKitUIList>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["targetPath"] = "TestListDelete"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(go.GetComponent<GameKitUIList>());
        }

        #endregion

        #region Clear Operation Tests

        [Test]
        public void Execute_Clear_ShouldClearItems()
        {
            var go = CreateTestGameObject("TestListClear");
            var list = go.AddComponent<GameKitUIList>();
            SetSerializedField(list, "listId", "clear_test");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "clear",
                ["targetPath"] = "TestListClear"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnListInfo()
        {
            var go = CreateTestGameObject("TestListInspect");
            var list = go.AddComponent<GameKitUIList>();
            SetSerializedField(list, "listId", "inspect_test");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["targetPath"] = "TestListInspect"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            var listInfo = result["list"] as Dictionary<string, object>;
            Assert.IsNotNull(listInfo);
            Assert.AreEqual("inspect_test", listInfo["listId"]);
        }

        #endregion
    }
}
