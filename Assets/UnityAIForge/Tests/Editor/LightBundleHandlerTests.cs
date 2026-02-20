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
            Assert.Contains("update", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("delete", ops);
            Assert.Contains("applyPreset", ops);
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
        public void Create_DirectionalLight_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("create",
                ("gameObjectPath", "TestLight"),
                ("lightType", "directional")));
            TestUtilities.AssertSuccess(result);
            var go = GameObject.Find("TestLight");
            if (go != null) _tracker.Track(go);
        }

        [Test]
        public void ListPresets_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("listPresets"));
            TestUtilities.AssertSuccess(result);
        }
    }
}
