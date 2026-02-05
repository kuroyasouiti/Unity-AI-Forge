using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MCP.Editor.Handlers;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// Unit tests for PhysicsBundleHandler.
    /// </summary>
    [TestFixture]
    public class PhysicsBundleHandlerTests
    {
        private PhysicsBundleHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new PhysicsBundleHandler();
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
        public void Category_ShouldReturnPhysicsBundle()
        {
            Assert.AreEqual("physicsBundle", _handler.Category);
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
            Assert.Contains("addRigidbody", operations);
            Assert.Contains("addRigidbody2D", operations);
            Assert.Contains("addCollider", operations);
            Assert.Contains("addCollider2D", operations);
        }

        #endregion

        #region AddRigidbody Tests

        [Test]
        public void Execute_AddRigidbody_ShouldAddRigidbodyComponent()
        {
            var go = CreateTestGameObject("RigidbodyTest");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "addRigidbody",
                ["gameObjectPath"] = "RigidbodyTest"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNotNull(go.GetComponent<Rigidbody>());
        }

        [Test]
        public void Execute_AddRigidbody_WithMass_ShouldSetMass()
        {
            var go = CreateTestGameObject("RigidbodyMassTest");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "addRigidbody",
                ["gameObjectPath"] = "RigidbodyMassTest",
                ["mass"] = 5f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var rb = go.GetComponent<Rigidbody>();
            Assert.IsNotNull(rb);
            Assert.AreEqual(5f, rb.mass, 0.01f);
        }

        [Test]
        public void Execute_AddRigidbody_WithKinematic_ShouldSetKinematic()
        {
            var go = CreateTestGameObject("RigidbodyKinematicTest");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "addRigidbody",
                ["gameObjectPath"] = "RigidbodyKinematicTest",
                ["isKinematic"] = true
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var rb = go.GetComponent<Rigidbody>();
            Assert.IsNotNull(rb);
            Assert.IsTrue(rb.isKinematic);
        }

        #endregion

        #region AddRigidbody2D Tests

        [Test]
        public void Execute_AddRigidbody2D_ShouldAddRigidbody2DComponent()
        {
            var go = CreateTestGameObject("Rigidbody2DTest");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "addRigidbody2D",
                ["gameObjectPath"] = "Rigidbody2DTest"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNotNull(go.GetComponent<Rigidbody2D>());
        }

        [Test]
        public void Execute_AddRigidbody2D_WithGravityScale_ShouldSetGravityScale()
        {
            var go = CreateTestGameObject("Rigidbody2DGravityTest");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "addRigidbody2D",
                ["gameObjectPath"] = "Rigidbody2DGravityTest",
                ["gravityScale"] = 2f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var rb2d = go.GetComponent<Rigidbody2D>();
            Assert.IsNotNull(rb2d);
            Assert.AreEqual(2f, rb2d.gravityScale, 0.01f);
        }

        #endregion

        #region AddCollider Tests

        [Test]
        public void Execute_AddCollider_Box_ShouldAddBoxCollider()
        {
            var go = CreateTestGameObject("BoxColliderTest");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "addCollider",
                ["gameObjectPath"] = "BoxColliderTest",
                ["colliderType"] = "box"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNotNull(go.GetComponent<BoxCollider>());
        }

        [Test]
        public void Execute_AddCollider_Sphere_ShouldAddSphereCollider()
        {
            var go = CreateTestGameObject("SphereColliderTest");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "addCollider",
                ["gameObjectPath"] = "SphereColliderTest",
                ["colliderType"] = "sphere"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNotNull(go.GetComponent<SphereCollider>());
        }

        [Test]
        public void Execute_AddCollider_Capsule_ShouldAddCapsuleCollider()
        {
            var go = CreateTestGameObject("CapsuleColliderTest");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "addCollider",
                ["gameObjectPath"] = "CapsuleColliderTest",
                ["colliderType"] = "capsule"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNotNull(go.GetComponent<CapsuleCollider>());
        }

        [Test]
        public void Execute_AddCollider_WithIsTrigger_ShouldSetIsTrigger()
        {
            var go = CreateTestGameObject("TriggerColliderTest");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "addCollider",
                ["gameObjectPath"] = "TriggerColliderTest",
                ["colliderType"] = "box",
                ["isTrigger"] = true
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var collider = go.GetComponent<BoxCollider>();
            Assert.IsNotNull(collider);
            Assert.IsTrue(collider.isTrigger);
        }

        #endregion

        #region AddCollider2D Tests

        [Test]
        public void Execute_AddCollider2D_Box_ShouldAddBoxCollider2D()
        {
            var go = CreateTestGameObject("BoxCollider2DTest");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "addCollider2D",
                ["gameObjectPath"] = "BoxCollider2DTest",
                ["colliderType"] = "box"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNotNull(go.GetComponent<BoxCollider2D>());
        }

        [Test]
        public void Execute_AddCollider2D_Circle_ShouldAddCircleCollider2D()
        {
            var go = CreateTestGameObject("CircleCollider2DTest");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "addCollider2D",
                ["gameObjectPath"] = "CircleCollider2DTest",
                ["colliderType"] = "circle"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNotNull(go.GetComponent<CircleCollider2D>());
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Execute_MissingOperation_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["gameObjectPath"] = "SomeObject"
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
        public void Execute_NonExistentGameObject_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "addRigidbody",
                ["gameObjectPath"] = "NonExistentObject"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion
    }
}
