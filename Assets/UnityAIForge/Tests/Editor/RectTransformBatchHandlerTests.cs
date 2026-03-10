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

        [Test]
        public void SetAnchors_ValidUIObject_UpdatesAnchors()
        {
            var canvas = _tracker.Create("RTCanvas");
            canvas.AddComponent<Canvas>();
            var child = new GameObject("RTChild", typeof(RectTransform));
            child.transform.SetParent(canvas.transform);
            _tracker.Track(child);

            var result = _handler.Execute(TestUtilities.CreatePayload("setAnchors",
                ("gameObjectPaths", new List<object> { "RTCanvas/RTChild" }),
                ("anchorMin", new Dictionary<string, object> { ["x"] = 0.0, ["y"] = 0.0 }),
                ("anchorMax", new Dictionary<string, object> { ["x"] = 1.0, ["y"] = 1.0 }))) as Dictionary<string, object>;
            TestUtilities.AssertSuccess(result);

            Assert.AreEqual(1, result["count"]);
        }
    }
}
