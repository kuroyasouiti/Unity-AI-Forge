using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MCP.Editor.Handlers;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// Unit tests for CameraRigHandler.
    /// </summary>
    [TestFixture]
    public class CameraRigHandlerTests
    {
        private CameraRigHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new CameraRigHandler();
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

        private void TrackCreatedObject(string path)
        {
            var go = GameObject.Find(path.Split('/').Last());
            if (go != null && !_createdObjects.Contains(go))
            {
                _createdObjects.Add(go);
            }
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnCameraRig()
        {
            Assert.AreEqual("cameraRig", _handler.Category);
        }

        [Test]
        public void Version_ShouldReturn100()
        {
            Assert.AreEqual("1.0.0", _handler.Version);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = new List<string>(_handler.SupportedOperations);
            Assert.Contains("createRig", operations);
            Assert.Contains("updateRig", operations);
            Assert.Contains("inspect", operations);
        }

        #endregion

        #region CreateRig Tests

        [Test]
        public void Execute_CreateRig_Follow_ShouldCreateCameraRig()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createRig",
                ["rigType"] = "follow",
                ["rigName"] = "TestFollowRig"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("rigPath"));
            Assert.IsTrue(result.ContainsKey("cameraPath"));
            Assert.AreEqual("follow", result["rigType"]);

            TrackCreatedObject(result["rigPath"].ToString());
        }

        [Test]
        public void Execute_CreateRig_Orbit_ShouldCreateCameraRig()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createRig",
                ["rigType"] = "orbit",
                ["rigName"] = "TestOrbitRig"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("orbit", result["rigType"]);

            TrackCreatedObject(result["rigPath"].ToString());
        }

        [Test]
        public void Execute_CreateRig_Fixed_ShouldCreateCameraRig()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createRig",
                ["rigType"] = "fixed",
                ["rigName"] = "TestFixedRig"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("fixed", result["rigType"]);

            TrackCreatedObject(result["rigPath"].ToString());
        }

        [Test]
        public void Execute_CreateRig_SplitScreen_ShouldCreateCameraRig()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createRig",
                ["rigType"] = "splitscreen",
                ["rigName"] = "TestSplitScreenRig"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("splitscreen", result["rigType"]);

            TrackCreatedObject(result["rigPath"].ToString());
        }

        [Test]
        public void Execute_CreateRig_Dolly_ShouldCreateCameraRig()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createRig",
                ["rigType"] = "dolly",
                ["rigName"] = "TestDollyRig"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("dolly", result["rigType"]);

            TrackCreatedObject(result["rigPath"].ToString());
        }

        [Test]
        public void Execute_CreateRig_WithFieldOfView_ShouldApplyFOV()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createRig",
                ["rigType"] = "follow",
                ["rigName"] = "TestFOVRig",
                ["fieldOfView"] = 90f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var cameraPath = result["cameraPath"].ToString();
            var cameraGo = GameObject.Find(cameraPath.Split('/').Last());
            if (cameraGo == null)
            {
                // Try finding by full hierarchy
                var rigGo = GameObject.Find(result["rigPath"].ToString().Split('/').Last());
                if (rigGo != null)
                {
                    cameraGo = rigGo.transform.Find("Camera")?.gameObject;
                    _createdObjects.Add(rigGo);
                }
            }

            if (cameraGo != null)
            {
                var camera = cameraGo.GetComponent<Camera>();
                Assert.IsNotNull(camera);
                Assert.AreEqual(90f, camera.fieldOfView, 0.01f);
            }

            TrackCreatedObject(result["rigPath"].ToString());
        }

        [Test]
        public void Execute_CreateRig_DefaultType_ShouldCreateFollowRig()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createRig",
                ["rigName"] = "TestDefaultRig"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("follow", result["rigType"]);

            TrackCreatedObject(result["rigPath"].ToString());
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Execute_MissingOperation_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["rigType"] = "follow"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        [Test]
        public void Execute_UnsupportedOperation_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "unsupportedOperation"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion
    }
}
