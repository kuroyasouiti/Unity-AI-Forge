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
    /// GameKitFeedbackHandler unit tests (3-Pillar Architecture - Presentation).
    /// Tests feedback system creation, component management, and intensity settings.
    /// </summary>
    [TestFixture]
    public class GameKitFeedbackHandlerTests
    {
        private GameKitFeedbackHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitFeedbackHandler();
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
        public void Category_ShouldReturnGamekitFeedback()
        {
            Assert.AreEqual("gamekitFeedback", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("create", operations);
            Assert.Contains("update", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("addComponent", operations);
            Assert.Contains("clearComponents", operations);
            Assert.Contains("setIntensity", operations);
            Assert.Contains("findByFeedbackId", operations);
        }

        #endregion

        #region Create Operation Tests

        [Test]
        public void Execute_Create_ShouldAddFeedbackComponent()
        {
            var go = CreateTestGameObject("TestFeedback");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestFeedback",
                ["feedbackId"] = "test_feedback"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var feedback = go.GetComponent<GameKitFeedback>();
            Assert.IsNotNull(feedback);
            Assert.AreEqual("test_feedback", feedback.FeedbackId);
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
            var go = CreateTestGameObject("TestFeedbackDelete");
            go.AddComponent<GameKitFeedback>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["targetPath"] = "TestFeedbackDelete"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(go.GetComponent<GameKitFeedback>());
        }

        #endregion

        #region ClearComponents Operation Tests

        [Test]
        public void Execute_ClearComponents_ShouldClearFeedbackComponents()
        {
            var go = CreateTestGameObject("TestFeedbackClear");
            var feedback = go.AddComponent<GameKitFeedback>();
            SetSerializedField(feedback, "feedbackId", "clear_test");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "clearComponents",
                ["targetPath"] = "TestFeedbackClear"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnFeedbackInfo()
        {
            var go = CreateTestGameObject("TestFeedbackInspect");
            var feedback = go.AddComponent<GameKitFeedback>();
            SetSerializedField(feedback, "feedbackId", "inspect_test");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["targetPath"] = "TestFeedbackInspect"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            var feedbackInfo = result["feedback"] as Dictionary<string, object>;
            Assert.IsNotNull(feedbackInfo);
            Assert.AreEqual("inspect_test", feedbackInfo["feedbackId"]);
        }

        #endregion
    }
}
