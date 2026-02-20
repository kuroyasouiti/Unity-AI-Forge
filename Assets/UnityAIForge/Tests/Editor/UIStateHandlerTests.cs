using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class UIStateHandlerTests
    {
        private UIStateHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new UIStateHandler();
        }

        [Test]
        public void Category_ReturnsUIState()
        {
            Assert.AreEqual("uiState", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("defineState", ops);
            Assert.Contains("applyState", ops);
            Assert.Contains("saveState", ops);
            Assert.Contains("loadState", ops);
            Assert.Contains("listStates", ops);
            Assert.Contains("deleteState", ops);
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
