using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class LightBundleHandlerTests
    {
        private LightBundleHandler _handler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new LightBundleHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown() => _tracker.Dispose();

        [Test]
        public void Category_ReturnsLightBundle()
        {
            Assert.AreEqual("lightBundle", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("create", ops);
            Assert.Contains("applyPreset", ops);
            Assert.Contains("createLightingSetup", ops);
            Assert.Contains("listPresets", ops);
        }

        [Test]
        public void Execute_NullPayload_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(null));
        }

        [Test]
        public void Execute_UnsupportedOperation_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(TestUtilities.CreatePayload("nonExistent")), "not supported");
        }

        [Test]
        public void Create_DirectionalLight_HasLightComponent()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("create",
                ("name", "TestLight"),
                ("lightType", "directional"))) as Dictionary<string, object>;
            TestUtilities.AssertSuccess(result);

            Assert.IsTrue(result.ContainsKey("gameObjectPath"));
            var goPath = result["gameObjectPath"].ToString();
            var go = GameObject.Find(goPath);
            Assert.IsNotNull(go, $"GameObject should exist at '{goPath}'");
            var light = go.GetComponent<Light>();
            Assert.IsNotNull(light, "Light component should exist");
            Assert.AreEqual(LightType.Directional, light.type);
            _tracker.Track(go);
        }

        [Test]
        public void Create_PointLight_HasCorrectType()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("create",
                ("name", "PointLight"),
                ("lightType", "point"))) as Dictionary<string, object>;
            TestUtilities.AssertSuccess(result);

            var go = GameObject.Find(result["gameObjectPath"].ToString());
            Assert.IsNotNull(go);
            Assert.AreEqual(LightType.Point, go.GetComponent<Light>().type);
            _tracker.Track(go);
        }

        [Test]
        public void ListPresets_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("listPresets"));
            TestUtilities.AssertSuccess(result);
        }
    }
}
