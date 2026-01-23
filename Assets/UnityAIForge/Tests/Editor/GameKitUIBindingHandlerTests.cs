using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using MCP.Editor.Handlers.GameKit;
using UnityAIForge.GameKit;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// GameKitUIBindingHandler unit tests (3-Pillar Architecture - UI).
    /// Tests UI data binding creation and configuration.
    /// </summary>
    [TestFixture]
    public class GameKitUIBindingHandlerTests
    {
        private GameKitUIBindingHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitUIBindingHandler();
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
        public void Category_ShouldReturnGamekitUIBinding()
        {
            Assert.AreEqual("gamekitUIBinding", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("create", operations);
            Assert.Contains("update", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("setRange", operations);
            Assert.Contains("refresh", operations);
            Assert.Contains("findByBindingId", operations);
        }

        #endregion

        #region Create Operation Tests

        [Test]
        public void Execute_Create_ShouldAddUIBindingComponent()
        {
            var go = CreateTestGameObject("TestBinding");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestBinding",
                ["bindingId"] = "test_binding"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var binding = go.GetComponent<GameKitUIBinding>();
            Assert.IsNotNull(binding);
            Assert.AreEqual("test_binding", binding.BindingId);
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
            var go = CreateTestGameObject("TestBindingDelete");
            go.AddComponent<GameKitUIBinding>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["targetPath"] = "TestBindingDelete"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(go.GetComponent<GameKitUIBinding>());
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnBindingInfo()
        {
            var go = CreateTestGameObject("TestBindingInspect");
            var binding = go.AddComponent<GameKitUIBinding>();
            SetSerializedField(binding, "bindingId", "inspect_test");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["targetPath"] = "TestBindingInspect"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            var bindingInfo = result["binding"] as Dictionary<string, object>;
            Assert.IsNotNull(bindingInfo);
            Assert.AreEqual("inspect_test", bindingInfo["bindingId"]);
        }

        #endregion
    }
}
