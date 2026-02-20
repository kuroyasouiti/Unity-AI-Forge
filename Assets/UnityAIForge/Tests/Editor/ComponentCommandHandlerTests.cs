using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class ComponentCommandHandlerTests
    {
        private ComponentCommandHandler _handler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new ComponentCommandHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown()
        {
            _tracker.Dispose();
        }

        [Test]
        public void Category_ReturnsComponent()
        {
            Assert.AreEqual("component", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("add", ops);
            Assert.Contains("remove", ops);
            Assert.Contains("update", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("addMultiple", ops);
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
        public void Add_Rigidbody2D_ReturnsSuccess()
        {
            var go = _tracker.Create("CompTarget");
            var result = _handler.Execute(TestUtilities.CreatePayload("add",
                ("gameObjectPath", "CompTarget"),
                ("componentType", "UnityEngine.Rigidbody2D")));
            TestUtilities.AssertSuccess(result);
            Assert.IsNotNull(go.GetComponent<Rigidbody2D>());
        }

        [Test]
        public void Inspect_WithComponent_ReturnsDetails()
        {
            var go = _tracker.Create("InspectComp");
            go.AddComponent<BoxCollider2D>();
            var result = _handler.Execute(TestUtilities.CreatePayload("inspect",
                ("gameObjectPath", "InspectComp"),
                ("componentType", "UnityEngine.BoxCollider2D")));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void Remove_ExistingComponent_ReturnsSuccess()
        {
            var go = _tracker.Create("RemoveComp");
            go.AddComponent<BoxCollider2D>();
            var result = _handler.Execute(TestUtilities.CreatePayload("remove",
                ("gameObjectPath", "RemoveComp"),
                ("componentType", "UnityEngine.BoxCollider2D")));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void Update_Properties_ReturnsSuccess()
        {
            var go = _tracker.Create("UpdateComp");
            go.AddComponent<Rigidbody2D>();
            var result = _handler.Execute(TestUtilities.CreatePayload("update",
                ("gameObjectPath", "UpdateComp"),
                ("componentType", "UnityEngine.Rigidbody2D"),
                ("propertyChanges", new Dictionary<string, object> { ["gravityScale"] = 0 })));
            TestUtilities.AssertSuccess(result);
        }
    }
}
