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
    }
}
