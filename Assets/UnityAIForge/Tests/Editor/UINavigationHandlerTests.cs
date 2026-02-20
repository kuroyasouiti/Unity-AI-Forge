using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class UINavigationHandlerTests
    {
        private UINavigationHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new UINavigationHandler();
        }

        [Test]
        public void Category_ReturnsUINavigation()
        {
            Assert.AreEqual("uiNavigation", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("configure", ops);
            Assert.Contains("setExplicit", ops);
            Assert.Contains("autoSetup", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("reset", ops);
            Assert.Contains("disable", ops);
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
