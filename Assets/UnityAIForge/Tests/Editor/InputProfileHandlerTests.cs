using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class InputProfileHandlerTests
    {
        private InputProfileHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new InputProfileHandler();
        }

        [Test]
        public void Category_ReturnsInputProfile()
        {
            Assert.AreEqual("inputProfile", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("createPlayerInput", ops);
            Assert.Contains("createInputActions", ops);
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
    }
}
