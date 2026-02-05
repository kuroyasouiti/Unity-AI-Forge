using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// Unit tests for GameKitActorHandler.
    /// </summary>
    [TestFixture]
    public class GameKitActorHandlerTests
    {
        private GameKitActorHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitActorHandler();
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

        private GameObject CreateTestGameObject(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            _createdObjects.Add(go);
            return go;
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnGameKitActor()
        {
            Assert.AreEqual("gameKitActor", _handler.Category);
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
            Assert.Contains("create", operations);
            Assert.Contains("configure", operations);
            Assert.Contains("inspect", operations);
        }

        #endregion

        #region Create Tests

        [Test]
        public void Execute_Create_ShouldCreateActorWithComponent()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestActor"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("gameObjectPath"));

            var actorName = result["gameObjectPath"].ToString().Split('/').Last();
            var actorGo = GameObject.Find(actorName);
            if (actorGo != null)
            {
                _createdObjects.Add(actorGo);
            }
        }

        [Test]
        public void Execute_Create_WithMovementProfile_ShouldApplyProfile()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "PlatformerActor",
                ["movementProfile"] = "platformer"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var actorName = result["gameObjectPath"].ToString().Split('/').Last();
            var actorGo = GameObject.Find(actorName);
            if (actorGo != null)
            {
                _createdObjects.Add(actorGo);
            }
        }

        [Test]
        public void Execute_Create_WithTopDown_ShouldApplyTopDownProfile()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TopDownActor",
                ["movementProfile"] = "topdown"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var actorName = result["gameObjectPath"].ToString().Split('/').Last();
            var actorGo = GameObject.Find(actorName);
            if (actorGo != null)
            {
                _createdObjects.Add(actorGo);
            }
        }

        #endregion

        #region Configure Tests

        [Test]
        public void Execute_Configure_WithSpeed_ShouldUpdateActor()
        {
            // First create an actor
            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "ConfigureActor"
            };

            var createResult = _handler.Execute(createPayload) as Dictionary<string, object>;
            Assert.IsTrue((bool)createResult["success"]);

            var actorPath = createResult["gameObjectPath"].ToString();
            var actorGo = GameObject.Find(actorPath.Split('/').Last());
            if (actorGo != null)
            {
                _createdObjects.Add(actorGo);
            }

            // Now configure it
            var configurePayload = new Dictionary<string, object>
            {
                ["operation"] = "configure",
                ["gameObjectPath"] = actorPath,
                ["moveSpeed"] = 10f
            };

            var configureResult = _handler.Execute(configurePayload) as Dictionary<string, object>;

            Assert.IsNotNull(configureResult);
            Assert.IsTrue((bool)configureResult["success"]);
        }

        #endregion

        #region Inspect Tests

        [Test]
        public void Execute_Inspect_ShouldReturnActorInfo()
        {
            // First create an actor
            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "InspectActor"
            };

            var createResult = _handler.Execute(createPayload) as Dictionary<string, object>;
            Assert.IsTrue((bool)createResult["success"]);

            var actorPath = createResult["gameObjectPath"].ToString();
            var actorGo = GameObject.Find(actorPath.Split('/').Last());
            if (actorGo != null)
            {
                _createdObjects.Add(actorGo);
            }

            // Now inspect it
            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = actorPath
            };

            var inspectResult = _handler.Execute(inspectPayload) as Dictionary<string, object>;

            Assert.IsNotNull(inspectResult);
            Assert.IsTrue((bool)inspectResult["success"]);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Execute_MissingOperation_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["name"] = "TestActor"
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

        [Test]
        public void Execute_Configure_NonExistentActor_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "configure",
                ["gameObjectPath"] = "NonExistentActor"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        [Test]
        public void Execute_Inspect_NonExistentActor_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = "NonExistentActor"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion
    }
}
