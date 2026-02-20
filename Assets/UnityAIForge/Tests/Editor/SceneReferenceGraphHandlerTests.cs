using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers.HighLevel;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class SceneReferenceGraphHandlerTests
    {
        private SceneReferenceGraphHandler _handler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new SceneReferenceGraphHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown() => _tracker.Dispose();

        [Test]
        public void Category_ReturnsSceneReferenceGraph()
        {
            Assert.AreEqual("sceneReferenceGraph", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("analyzeScene", ops);
            Assert.Contains("analyzeObject", ops);
            Assert.Contains("findReferencesTo", ops);
            Assert.Contains("findReferencesFrom", ops);
            Assert.Contains("findOrphans", ops);
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
        public void AnalyzeScene_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("analyzeScene"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void FindOrphans_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("findOrphans"));
            TestUtilities.AssertSuccess(result);
        }
    }
}
