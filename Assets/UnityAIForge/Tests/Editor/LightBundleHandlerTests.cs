using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MCP.Editor.Handlers;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// LightBundleHandler unit tests.
    /// Tests light creation, modification, and preset application.
    /// </summary>
    [TestFixture]
    public class LightBundleHandlerTests
    {
        private LightBundleHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new LightBundleHandler();
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
        public void Category_ShouldReturnLightBundle()
        {
            Assert.AreEqual("lightBundle", _handler.Category);
        }

        [Test]
        public void Version_ShouldReturn100()
        {
            Assert.AreEqual("1.0.0", _handler.Version);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("create", operations);
            Assert.Contains("update", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("applyPreset", operations);
            Assert.Contains("listPresets", operations);
        }

        #endregion

        #region Create Operation Tests

        [Test]
        public void Execute_Create_ShouldCreatePointLight()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestPointLight",
                ["lightType"] = "Point"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("gameObjectPath"));

            var path = result["gameObjectPath"] as string;
            var go = GameObject.Find(path);
            Assert.IsNotNull(go);
            _createdObjects.Add(go);

            var light = go.GetComponent<Light>();
            Assert.IsNotNull(light);
            Assert.AreEqual(LightType.Point, light.type);
        }

        [Test]
        public void Execute_Create_ShouldCreateSpotLight()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestSpotLight",
                ["lightType"] = "Spot",
                ["spotAngle"] = 45f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var path = result["gameObjectPath"] as string;
            var go = GameObject.Find(path);
            _createdObjects.Add(go);

            var light = go.GetComponent<Light>();
            Assert.AreEqual(LightType.Spot, light.type);
            Assert.AreEqual(45f, light.spotAngle);
        }

        [Test]
        public void Execute_Create_ShouldCreateDirectionalLight()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestDirectionalLight",
                ["lightType"] = "Directional"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var path = result["gameObjectPath"] as string;
            var go = GameObject.Find(path);
            _createdObjects.Add(go);

            var light = go.GetComponent<Light>();
            Assert.AreEqual(LightType.Directional, light.type);
        }

        [Test]
        public void Execute_Create_WithColor_ShouldApplyColor()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestColoredLight",
                ["lightType"] = "Point",
                ["color"] = new Dictionary<string, object>
                {
                    ["r"] = 1f,
                    ["g"] = 0f,
                    ["b"] = 0f,
                    ["a"] = 1f
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var path = result["gameObjectPath"] as string;
            var go = GameObject.Find(path);
            _createdObjects.Add(go);

            var light = go.GetComponent<Light>();
            Assert.AreEqual(1f, light.color.r, 0.01f);
            Assert.AreEqual(0f, light.color.g, 0.01f);
            Assert.AreEqual(0f, light.color.b, 0.01f);
        }

        [Test]
        public void Execute_Create_WithIntensity_ShouldApplyIntensity()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestIntenseLight",
                ["lightType"] = "Point",
                ["intensity"] = 5f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var path = result["gameObjectPath"] as string;
            var go = GameObject.Find(path);
            _createdObjects.Add(go);

            var light = go.GetComponent<Light>();
            Assert.AreEqual(5f, light.intensity, 0.01f);
        }

        #endregion

        #region Update Operation Tests

        [Test]
        public void Execute_Update_ShouldModifyExistingLight()
        {
            // First create a light
            var go = CreateTestGameObject("TestLight_Update");
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = 1f;

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = "TestLight_Update",
                ["intensity"] = 10f,
                ["range"] = 20f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(10f, light.intensity, 0.01f);
            Assert.AreEqual(20f, light.range, 0.01f);
        }

        [Test]
        public void Execute_Update_NoLightComponent_ShouldReturnError()
        {
            var go = CreateTestGameObject("TestNoLight");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = "TestNoLight",
                ["intensity"] = 5f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("error"));
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnLightProperties()
        {
            var go = CreateTestGameObject("TestLight_Inspect");
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = 3f;
            light.range = 15f;
            light.color = Color.yellow;

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = "TestLight_Inspect"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("Point", result["lightType"]);
            Assert.AreEqual(3f, (float)result["intensity"], 0.01f);
            Assert.AreEqual(15f, (float)result["range"], 0.01f);
            Assert.IsTrue(result.ContainsKey("color"));
        }

        #endregion

        #region Delete Operation Tests

        [Test]
        public void Execute_Delete_ShouldRemoveLight()
        {
            var go = CreateTestGameObject("TestLight_Delete");
            go.AddComponent<Light>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["gameObjectPath"] = "TestLight_Delete"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            // Remove from cleanup list since it's deleted
            _createdObjects.Remove(go);
        }

        #endregion

        #region ApplyPreset Operation Tests

        [Test]
        public void Execute_ApplyPreset_ShouldApplyPresetSettings()
        {
            var go = CreateTestGameObject("TestLight_Preset");
            var light = go.AddComponent<Light>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "applyPreset",
                ["gameObjectPath"] = "TestLight_Preset",
                ["preset"] = "warm"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        [Test]
        public void Execute_ApplyPreset_NoPreset_ShouldReturnError()
        {
            var go = CreateTestGameObject("TestLight_NoPreset");
            go.AddComponent<Light>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "applyPreset",
                ["gameObjectPath"] = "TestLight_NoPreset"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion

        #region ListPresets Operation Tests

        [Test]
        public void Execute_ListPresets_ShouldReturnPresetList()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "listPresets"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("lightPresets"));
            Assert.IsTrue(result.ContainsKey("setupPresets"));

            var lightPresets = result["lightPresets"] as List<Dictionary<string, object>>;
            Assert.IsNotNull(lightPresets);
            Assert.IsTrue(lightPresets.Count > 0);
        }

        #endregion
    }
}
