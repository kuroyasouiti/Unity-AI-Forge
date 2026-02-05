using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// Unit tests for PingHandler.
    /// </summary>
    [TestFixture]
    public class PingHandlerTests
    {
        private PingHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new PingHandler();
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnPing()
        {
            Assert.AreEqual("ping", _handler.Category);
        }

        [Test]
        public void Version_ShouldReturn100()
        {
            Assert.AreEqual("1.0.0", _handler.Version);
        }

        [Test]
        public void SupportedOperations_ShouldContainPing()
        {
            var operations = new List<string>(_handler.SupportedOperations);
            Assert.Contains("ping", operations);
        }

        #endregion

        #region Execute Tests

        [Test]
        public void Execute_WithEmptyPayload_ShouldReturnPong()
        {
            var payload = new Dictionary<string, object>();

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("pong", result["message"]);
        }

        [Test]
        public void Execute_WithNullPayload_ShouldReturnPong()
        {
            var result = _handler.Execute(null) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("pong", result["message"]);
        }

        [Test]
        public void Execute_ShouldIncludeUnityVersion()
        {
            var payload = new Dictionary<string, object>();

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("unityVersion"));
            Assert.IsNotNull(result["unityVersion"]);
        }

        [Test]
        public void Execute_ShouldIncludeProductName()
        {
            var payload = new Dictionary<string, object>();

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("productName"));
        }

        [Test]
        public void Execute_ShouldIncludePlatform()
        {
            var payload = new Dictionary<string, object>();

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("platform"));
            Assert.IsNotNull(result["platform"]);
        }

        [Test]
        public void Execute_ShouldIncludeIsPlaying()
        {
            var payload = new Dictionary<string, object>();

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("isPlaying"));
        }

        [Test]
        public void Execute_ShouldIncludeTimestamp()
        {
            var payload = new Dictionary<string, object>();

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("timestamp"));
            Assert.IsInstanceOf<long>(result["timestamp"]);
        }

        #endregion
    }
}
