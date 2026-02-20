using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class EventWiringHandlerTests
    {
        private EventWiringHandler _handler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new EventWiringHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown() => _tracker.Dispose();

        [Test]
        public void Category_ReturnsEventWiring()
        {
            Assert.AreEqual("eventWiring", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("wire", ops);
            Assert.Contains("unwire", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("listEvents", ops);
            Assert.Contains("clearEvent", ops);
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
        public void ListEvents_ValidObject_ReturnsSuccess()
        {
            var go = _tracker.Create("EventObj");
            go.AddComponent<Button>();
            var result = _handler.Execute(TestUtilities.CreatePayload("listEvents",
                ("gameObjectPath", "EventObj")));
            TestUtilities.AssertSuccess(result);
        }
    }
}
