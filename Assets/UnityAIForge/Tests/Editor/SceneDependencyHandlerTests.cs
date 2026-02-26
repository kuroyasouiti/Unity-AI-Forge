using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers.HighLevel;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class SceneDependencyHandlerTests
    {
        private SceneDependencyHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new SceneDependencyHandler();
        }

        [Test]
        public void Category_ReturnsSceneDependency()
        {
            Assert.AreEqual("sceneDependency", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("analyzeScene", ops);
            Assert.Contains("findAssetUsage", ops);
            Assert.Contains("findSharedAssets", ops);
            Assert.Contains("findUnusedAssets", ops);
            Assert.AreEqual(4, ops.Count);
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
        public void AnalyzeScene_MissingScenePath_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(TestUtilities.CreatePayload("analyzeScene")), "scenePath");
        }

        [Test]
        public void FindAssetUsage_MissingAssetPath_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(TestUtilities.CreatePayload("findAssetUsage")), "assetPath");
        }

        [Test]
        public void FindSharedAssets_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("findSharedAssets"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void FindUnusedAssets_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("findUnusedAssets"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void FindUnusedAssets_WithSearchPath_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("findUnusedAssets",
                ("searchPath", "Assets")));
            TestUtilities.AssertSuccess(result);
        }
    }
}
