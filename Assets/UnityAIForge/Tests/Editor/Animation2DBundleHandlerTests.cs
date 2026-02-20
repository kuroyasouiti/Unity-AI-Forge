using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class Animation2DBundleHandlerTests
    {
        private Animation2DBundleHandler _handler;
        private GameObjectTracker _tracker;
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _handler = new Animation2DBundleHandler();
            _tracker = new GameObjectTracker();
            _tempDir = TestUtilities.CreateTempAssetDirectory();
        }

        [TearDown]
        public void TearDown()
        {
            _tracker.Dispose();
            TestUtilities.CleanupTempAssetDirectory(_tempDir);
        }

        [Test]
        public void Category_ReturnsAnimation2DBundle()
        {
            Assert.AreEqual("animation2DBundle", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("setupAnimator", ops);
            Assert.Contains("createController", ops);
            Assert.Contains("addState", ops);
            Assert.Contains("addTransition", ops);
            Assert.Contains("inspectController", ops);
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
        public void CreateController_ValidPath_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("createController",
                ("controllerPath", $"{_tempDir}/TestController.controller")));
            TestUtilities.AssertSuccess(result);
        }
    }
}
