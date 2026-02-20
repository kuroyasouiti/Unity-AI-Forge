using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class PhysicsBundleHandlerTests
    {
        private PhysicsBundleHandler _handler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new PhysicsBundleHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown() => _tracker.Dispose();

        [Test]
        public void Category_ReturnsPhysicsBundle()
        {
            Assert.AreEqual("physicsBundle", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("applyPreset2D", ops);
            Assert.Contains("applyPreset3D", ops);
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
        public void ApplyPreset2D_Character_ReturnsSuccess()
        {
            _tracker.Create("PhysObj");
            var result = _handler.Execute(TestUtilities.CreatePayload("applyPreset2D",
                ("gameObjectPaths", new List<object> { "PhysObj" }),
                ("preset", "character")));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void Inspect_WithRigidbody_ReturnsSuccess()
        {
            var go = _tracker.Create("InspectPhys");
            go.AddComponent<Rigidbody2D>();
            var result = _handler.Execute(TestUtilities.CreatePayload("inspect",
                ("gameObjectPaths", new List<object> { "InspectPhys" })));
            TestUtilities.AssertSuccess(result);
        }
    }
}
