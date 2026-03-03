using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class CameraBundleHandlerTests
    {
        private CameraBundleHandler _handler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new CameraBundleHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown() => _tracker.Dispose();

        [Test]
        public void Category_ReturnsCameraBundle()
        {
            Assert.AreEqual("cameraBundle", _handler.Category);
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
        public void Create_DefaultCamera_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("create",
                ("name", "TestCamera")));
            TestUtilities.AssertSuccess(result);
            var go = GameObject.Find("TestCamera");
            Assert.IsNotNull(go);
            Assert.IsNotNull(go.GetComponent<Camera>());
            if (go != null) _tracker.Track(go);
        }

        [Test]
        public void Create_WithPreset_AppliesPreset()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("create",
                ("name", "OrthoCamera"),
                ("preset", "orthographic2D")));
            TestUtilities.AssertSuccess(result);
            var go = GameObject.Find("OrthoCamera");
            Assert.IsNotNull(go);
            var camera = go.GetComponent<Camera>();
            Assert.IsTrue(camera.orthographic);
            Assert.AreEqual(5f, camera.orthographicSize, 0.01f);
            if (go != null) _tracker.Track(go);
        }

        [Test]
        public void Update_ExistingCamera_ReturnsSuccess()
        {
            var go = _tracker.Create("UpdateCam");
            go.AddComponent<Camera>();

            var result = _handler.Execute(TestUtilities.CreatePayload("update",
                ("gameObjectPath", "UpdateCam"),
                ("fieldOfView", 90f)));
            TestUtilities.AssertSuccess(result);
            Assert.AreEqual(90f, go.GetComponent<Camera>().fieldOfView, 0.01f);
        }

        [Test]
        public void Inspect_ExistingCamera_ReturnsSuccess()
        {
            var go = _tracker.Create("InspectCam");
            go.AddComponent<Camera>();

            var result = _handler.Execute(TestUtilities.CreatePayload("inspect",
                ("gameObjectPath", "InspectCam")));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void Delete_ExistingCamera_ReturnsSuccess()
        {
            var go = _tracker.Create("DeleteCam");
            go.AddComponent<Camera>();

            var result = _handler.Execute(TestUtilities.CreatePayload("delete",
                ("gameObjectPath", "DeleteCam")));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void ApplyPreset_ExistingCamera_ReturnsSuccess()
        {
            var go = _tracker.Create("PresetCam");
            var camera = go.AddComponent<Camera>();

            var result = _handler.Execute(TestUtilities.CreatePayload("applyPreset",
                ("gameObjectPath", "PresetCam"),
                ("preset", "minimap")));
            TestUtilities.AssertSuccess(result);
            Assert.IsTrue(camera.orthographic);
            Assert.AreEqual(1, camera.depth, 0.01f);
        }

        [Test]
        public void ListPresets_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("listPresets"));
            TestUtilities.AssertSuccess(result);
        }
    }
}
