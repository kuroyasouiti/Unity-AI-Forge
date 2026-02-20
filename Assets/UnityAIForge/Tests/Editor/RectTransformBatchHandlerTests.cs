using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class RectTransformBatchHandlerTests
    {
        private RectTransformBatchHandler _handler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new RectTransformBatchHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown() => _tracker.Dispose();

        [Test]
        public void Category_ReturnsRectTransformBatch()
        {
            Assert.AreEqual("rectTransformBatch", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("setAnchors", ops);
            Assert.Contains("setPivot", ops);
            Assert.Contains("distributeHorizontal", ops);
            Assert.Contains("distributeVertical", ops);
            Assert.Contains("alignToParent", ops);
            Assert.Contains("matchSize", ops);
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
