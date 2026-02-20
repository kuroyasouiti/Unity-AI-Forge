using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class PingHandlerTests
    {
        private PingHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new PingHandler();
        }

        [Test]
        public void Category_ReturnsPing()
        {
            Assert.AreEqual("ping", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsPing()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("ping", ops);
        }

        [Test]
        public void Execute_EmptyPayload_ReturnsSuccess()
        {
            var result = _handler.Execute(new Dictionary<string, object>()) as Dictionary<string, object>;
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        [Test]
        public void Execute_NullPayload_ReturnsSuccess()
        {
            // PingHandler overrides ValidatePayload to skip validation
            var result = _handler.Execute(null) as Dictionary<string, object>;
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        [Test]
        public void Execute_ReturnsUnityVersion()
        {
            var result = _handler.Execute(new Dictionary<string, object>()) as Dictionary<string, object>;
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("unityVersion"));
        }

        [Test]
        public void Execute_ReturnsPlatform()
        {
            var result = _handler.Execute(new Dictionary<string, object>()) as Dictionary<string, object>;
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("platform"));
        }

        [Test]
        public void Execute_ReturnsTimestamp()
        {
            var result = _handler.Execute(new Dictionary<string, object>()) as Dictionary<string, object>;
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("timestamp"));
        }
    }
}
