using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class TransformBatchHandlerTests
    {
        private TransformBatchHandler _handler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new TransformBatchHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown() => _tracker.Dispose();

        [Test]
        public void Category_ReturnsTransformBatch()
        {
            Assert.AreEqual("transformBatch", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("arrangeCircle", ops);
            Assert.Contains("arrangeLine", ops);
            Assert.Contains("renameSequential", ops);
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
        public void ArrangeCircle_ValidObjects_ReturnsSuccess()
        {
            _tracker.Create("C1");
            _tracker.Create("C2");
            _tracker.Create("C3");
            var result = _handler.Execute(TestUtilities.CreatePayload("arrangeCircle",
                ("gameObjectPaths", new List<object> { "C1", "C2", "C3" }),
                ("radius", 5.0)));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void RenameSequential_ValidObjects_ReturnsSuccess()
        {
            _tracker.Create("Item1");
            _tracker.Create("Item2");
            var result = _handler.Execute(TestUtilities.CreatePayload("renameSequential",
                ("gameObjectPaths", new List<object> { "Item1", "Item2" }),
                ("baseName", "Enemy"),
                ("startIndex", 1)));
            TestUtilities.AssertSuccess(result);
        }
    }
}
