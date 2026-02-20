using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class SceneCommandHandlerTests
    {
        private SceneCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new SceneCommandHandler();
        }

        [Test]
        public void Category_ReturnsScene()
        {
            Assert.AreEqual("scene", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("create", ops);
            Assert.Contains("load", ops);
            Assert.Contains("save", ops);
            Assert.Contains("delete", ops);
            Assert.Contains("duplicate", ops);
            Assert.Contains("inspect", ops);
        }

        [Test]
        public void Execute_NullPayload_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(null));
        }

        [Test]
        public void Execute_MissingOperation_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(new Dictionary<string, object>()));
        }

        [Test]
        public void Execute_UnsupportedOperation_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(TestUtilities.CreatePayload("nonExistent")), "not supported");
        }

        [Test]
        public void Inspect_CurrentScene_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("inspect"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void Inspect_WithHierarchy_ReturnsData()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("inspect",
                ("includeHierarchy", true))) as Dictionary<string, object>;
            TestUtilities.AssertSuccess(result);
        }
    }
}
