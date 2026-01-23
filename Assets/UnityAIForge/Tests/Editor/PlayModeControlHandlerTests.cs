using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MCP.Editor.Handlers;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// PlayModeControlHandler unit tests.
    /// Tests play mode control operations (play, pause, stop, step).
    /// Note: Actual play mode state changes are not tested to avoid Unity Editor state issues.
    /// </summary>
    [TestFixture]
    public class PlayModeControlHandlerTests
    {
        private PlayModeControlHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new PlayModeControlHandler();
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnPlayModeControl()
        {
            Assert.AreEqual("playModeControl", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("play", operations);
            Assert.Contains("pause", operations);
            Assert.Contains("unpause", operations);
            Assert.Contains("stop", operations);
            Assert.Contains("step", operations);
            Assert.Contains("getState", operations);
        }

        [Test]
        public void Version_ShouldReturn100()
        {
            Assert.AreEqual("1.0.0", _handler.Version);
        }

        #endregion

        #region GetState Operation Tests

        [Test]
        public void Execute_GetState_ShouldReturnCurrentState()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "getState"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("state"));
            Assert.IsTrue(result.ContainsKey("isPlaying"));
            Assert.IsTrue(result.ContainsKey("isPaused"));
            Assert.IsTrue(result.ContainsKey("isCompiling"));
        }

        [Test]
        public void Execute_GetState_WhenStopped_ShouldReturnStoppedState()
        {
            // Ensure we're not in play mode for this test
            if (UnityEditor.EditorApplication.isPlaying)
            {
                Assert.Inconclusive("Test requires Editor to not be in Play Mode");
                return;
            }

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "getState"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("stopped", result["state"]);
            Assert.IsFalse((bool)result["isPlaying"]);
        }

        #endregion

        #region Stop Operation Tests

        [Test]
        public void Execute_Stop_WhenAlreadyStopped_ShouldReturnSuccess()
        {
            // Ensure we're not in play mode
            if (UnityEditor.EditorApplication.isPlaying)
            {
                Assert.Inconclusive("Test requires Editor to not be in Play Mode");
                return;
            }

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "stop"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("Already stopped", result["message"]);
        }

        #endregion

        #region Pause Operation Tests

        [Test]
        public void Execute_Pause_WhenNotPlaying_ShouldReturnError()
        {
            // Ensure we're not in play mode
            if (UnityEditor.EditorApplication.isPlaying)
            {
                Assert.Inconclusive("Test requires Editor to not be in Play Mode");
                return;
            }

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "pause"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("error"));
        }

        #endregion

        #region Unpause Operation Tests

        [Test]
        public void Execute_Unpause_WhenNotPlaying_ShouldReturnError()
        {
            // Ensure we're not in play mode
            if (UnityEditor.EditorApplication.isPlaying)
            {
                Assert.Inconclusive("Test requires Editor to not be in Play Mode");
                return;
            }

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "unpause"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("error"));
        }

        #endregion

        #region Step Operation Tests

        [Test]
        public void Execute_Step_WhenNotPlaying_ShouldReturnError()
        {
            // Ensure we're not in play mode
            if (UnityEditor.EditorApplication.isPlaying)
            {
                Assert.Inconclusive("Test requires Editor to not be in Play Mode");
                return;
            }

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "step"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("error"));
        }

        #endregion

        #region Invalid Operation Tests

        [Test]
        public void Execute_UnknownOperation_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "unknownOperation"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion
    }
}
