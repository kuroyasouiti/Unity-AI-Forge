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
    /// GameKitVFXHandler unit tests (3-Pillar Architecture - Presentation).
    /// Tests VFX manager creation, multiplier settings, and visual configuration.
    /// </summary>
    [TestFixture]
    public class GameKitVFXHandlerTests
    {
        private GameKitVFXHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitVFXHandler();
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
        public void Category_ShouldReturnGamekitVFX()
        {
            Assert.AreEqual("gamekitVFX", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("create", operations);
            Assert.Contains("update", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("setMultipliers", operations);
            Assert.Contains("setColor", operations);
            Assert.Contains("setLoop", operations);
            Assert.Contains("findByVFXId", operations);
        }

        #endregion

        #region Create Operation Tests

        [Test]
        public void Execute_Create_ShouldAddVFXComponent()
        {
            var go = CreateTestGameObject("TestVFX");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestVFX",
                ["vfxId"] = "test_vfx"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var vfx = go.GetComponent<GameKitVFX>();
            Assert.IsNotNull(vfx);
            Assert.AreEqual("test_vfx", vfx.VFXId);
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
            var go = CreateTestGameObject("TestVFXDelete");
            go.AddComponent<GameKitVFX>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["targetPath"] = "TestVFXDelete"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(go.GetComponent<GameKitVFX>());
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnVFXInfo()
        {
            var go = CreateTestGameObject("TestVFXInspect");
            var vfx = go.AddComponent<GameKitVFX>();
            SetSerializedField(vfx, "vfxId", "inspect_test");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["targetPath"] = "TestVFXInspect"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            var vfxInfo = result["vfx"] as Dictionary<string, object>;
            Assert.IsNotNull(vfxInfo);
            Assert.AreEqual("inspect_test", vfxInfo["vfxId"]);
        }

        #endregion

        #region SetLoop Operation Tests

        [Test]
        public void Execute_SetLoop_ShouldUpdateLoopSetting()
        {
            var go = CreateTestGameObject("TestVFXLoop");
            var vfx = go.AddComponent<GameKitVFX>();
            SetSerializedField(vfx, "vfxId", "loop_test");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "setLoop",
                ["targetPath"] = "TestVFXLoop",
                ["loop"] = true
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion
    }
}
