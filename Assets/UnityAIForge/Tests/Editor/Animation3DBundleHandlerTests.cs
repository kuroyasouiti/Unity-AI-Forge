using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class Animation3DBundleHandlerTests
    {
        private Animation3DBundleHandler _handler;
        private GameObjectTracker _tracker;
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _handler = new Animation3DBundleHandler();
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
        public void Category_ReturnsAnimation3DBundle()
        {
            Assert.AreEqual("animation3DBundle", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("setupAnimator", ops);
            Assert.Contains("createController", ops);
            Assert.Contains("addState", ops);
            Assert.Contains("addTransition", ops);
            Assert.Contains("inspect", ops);
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
                ("controllerPath", $"{_tempDir}/Test3DController.controller")));
            TestUtilities.AssertSuccess(result);
        }
    }
}
