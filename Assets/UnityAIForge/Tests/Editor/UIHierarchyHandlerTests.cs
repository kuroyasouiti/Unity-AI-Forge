using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class UIHierarchyHandlerTests
    {
        private UIHierarchyHandler _handler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new UIHierarchyHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown() => _tracker.Dispose();

        [Test]
        public void Category_ReturnsUIHierarchy()
        {
            Assert.AreEqual("uiHierarchy", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("create", ops);
            Assert.Contains("clone", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("delete", ops);
            Assert.Contains("show", ops);
            Assert.Contains("hide", ops);
            Assert.Contains("toggle", ops);
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
    }
}
