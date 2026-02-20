using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers.HighLevel;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class SceneRelationshipGraphHandlerTests
    {
        private SceneRelationshipGraphHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new SceneRelationshipGraphHandler();
        }

        [Test]
        public void Category_ReturnsSceneRelationshipGraph()
        {
            Assert.AreEqual("sceneRelationshipGraph", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("analyzeAll", ops);
            Assert.Contains("analyzeScene", ops);
            Assert.Contains("findTransitionsTo", ops);
            Assert.Contains("findTransitionsFrom", ops);
            Assert.Contains("validateBuildSettings", ops);
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
        public void AnalyzeAll_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("analyzeAll"));
            TestUtilities.AssertSuccess(result);
        }
    }
}
