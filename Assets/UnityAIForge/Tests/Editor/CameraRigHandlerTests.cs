using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class CameraRigHandlerTests
    {
        private CameraRigHandler _handler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new CameraRigHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown() => _tracker.Dispose();

        [Test]
        public void Category_ReturnsCameraRig()
        {
            Assert.AreEqual("cameraRig", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("createRig", ops);
            Assert.Contains("updateRig", ops);
            Assert.Contains("inspect", ops);
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
        public void CreateRig_Follow_ReturnsSuccess()
        {
            var target = _tracker.Create("CameraTarget");
            var result = _handler.Execute(TestUtilities.CreatePayload("createRig",
                ("rigType", "follow"),
                ("rigName", "TestCam"),
                ("targetPath", "CameraTarget")));
            TestUtilities.AssertSuccess(result);
            var cam = GameObject.Find("TestCam");
            if (cam != null) _tracker.Track(cam);
        }
    }
}
