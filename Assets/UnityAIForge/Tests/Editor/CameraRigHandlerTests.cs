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

        [Test]
        public void CreateRig_ExistingGameObject_ReusesIt()
        {
            var existing = _tracker.Create("ExistingRig");
            var target = _tracker.Create("Target");

            var result = _handler.Execute(TestUtilities.CreatePayload("createRig",
                ("rigType", "follow"),
                ("rigName", "ExistingRig"),
                ("targetPath", "Target")));
            TestUtilities.AssertSuccess(result);

            var reused = TestUtilities.GetResultValue<bool>(result, "reused");
            Assert.IsTrue(reused, "Should report reused=true");

            // Ensure no duplicate GameObjects were created
            var allRigs = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int count = 0;
            foreach (var go in allRigs)
                if (go.name == "ExistingRig") count++;
            Assert.AreEqual(1, count, "Should not create a duplicate GameObject");

            // Helper should be attached
            Assert.IsNotNull(existing.GetComponent<CameraFollowHelper>(), "Follow helper should be added");
        }

        [Test]
        public void CreateRig_ExistingWithCamera_DoesNotCreateChild()
        {
            var existing = _tracker.Create("CamRig");
            existing.AddComponent<Camera>();

            var result = _handler.Execute(TestUtilities.CreatePayload("createRig",
                ("rigType", "follow"),
                ("rigName", "CamRig")));
            TestUtilities.AssertSuccess(result);

            // Should reuse existing Camera, not create a child "Camera" object
            Assert.AreEqual(0, existing.transform.childCount,
                "Should not create child Camera when one already exists on the root");
            Assert.IsNotNull(existing.GetComponent<Camera>(),
                "Existing Camera component should still be present");
        }

        [Test]
        public void CreateRig_ExistingWithHelper_UpdatesHelper()
        {
            var existing = _tracker.Create("HelperRig");
            var helper = existing.AddComponent<CameraFollowHelper>();
            helper.followSpeed = 1f;

            var result = _handler.Execute(TestUtilities.CreatePayload("createRig",
                ("rigType", "follow"),
                ("rigName", "HelperRig"),
                ("followSpeed", 10f)));
            TestUtilities.AssertSuccess(result);

            // Should update existing helper, not add a second one
            var helpers = existing.GetComponents<CameraFollowHelper>();
            Assert.AreEqual(1, helpers.Length, "Should not add a duplicate helper component");
            Assert.AreEqual(10f, helpers[0].followSpeed, 0.01f, "Follow speed should be updated");
        }
    }
}
