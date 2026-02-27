using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class PlayModeControlHandlerTests
    {
        private PlayModeControlHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new PlayModeControlHandler();
        }

        [Test]
        public void Category_ReturnsPlayModeControl()
        {
            Assert.AreEqual("playModeControl", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("play", ops);
            Assert.Contains("pause", ops);
            Assert.Contains("unpause", ops);
            Assert.Contains("stop", ops);
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
        public void SupportedOperations_ContainsNewOps()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("captureState", ops);
            Assert.Contains("waitForScene", ops);
        }

        [Test]
        public void CaptureState_NotPlaying_ReturnsError()
        {
            TestUtilities.AssertError(
                _handler.Execute(TestUtilities.CreatePayload("captureState")),
                "play mode");
        }

        [Test]
        public void WaitForScene_MissingSceneName_ReturnsError()
        {
            TestUtilities.AssertError(
                _handler.Execute(TestUtilities.CreatePayload("waitForScene")),
                "sceneName is required");
        }

        [Test]
        public void WaitForScene_ReturnsLoadedStatus()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("waitForScene",
                ("sceneName", "NonExistentScene")));
            TestUtilities.AssertSuccess(result);
            var loaded = TestUtilities.GetResultValue<bool>(result, "loaded");
            Assert.IsFalse(loaded);
        }

        [Test]
        public void SupportedOperations_ContainsValidateState()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("validateState", ops);
        }

        [Test]
        public void ValidateState_NotPlaying_ReturnsError()
        {
            TestUtilities.AssertError(
                _handler.Execute(TestUtilities.CreatePayload("validateState")),
                "play mode");
        }

        [Test]
        public void ValidateState_MissingManagers_ReturnsError()
        {
            // Not in play mode, so this should fail with "play mode" error
            // This test verifies the play mode check comes first
            TestUtilities.AssertError(
                _handler.Execute(TestUtilities.CreatePayload("validateState")),
                "play mode");
        }
    }
}
