using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class BaseCommandHandlerTests
    {
        private TestHandler _handler;

        /// <summary>
        /// Concrete test implementation of BaseCommandHandler.
        /// </summary>
        private class TestHandler : BaseCommandHandler
        {
            public override string Category => "test";
            public override IEnumerable<string> SupportedOperations => new[] { "doSomething", "inspect" };

            protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
            {
                return CreateSuccessResponse(("operation", operation));
            }
        }

        [SetUp]
        public void SetUp()
        {
            _handler = new TestHandler();
        }

        [Test]
        public void Category_ReturnsExpected()
        {
            Assert.AreEqual("test", _handler.Category);
        }

        [Test]
        public void Version_ReturnsDefault()
        {
            Assert.AreEqual("1.0.0", _handler.Version);
        }

        [Test]
        public void SupportedOperations_ReturnsExpectedOperations()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("doSomething", ops);
            Assert.Contains("inspect", ops);
        }

        [Test]
        public void Execute_NullPayload_ReturnsError()
        {
            var result = _handler.Execute(null) as Dictionary<string, object>;
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsTrue(result["error"].ToString().Contains("null"));
        }

        [Test]
        public void Execute_MissingOperation_ReturnsError()
        {
            var payload = new Dictionary<string, object> { ["someKey"] = "value" };
            var result = _handler.Execute(payload) as Dictionary<string, object>;
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsTrue(result["error"].ToString().Contains("operation"));
        }

        [Test]
        public void Execute_UnsupportedOperation_ReturnsError()
        {
            var payload = TestUtilities.CreatePayload("nonExistentOp");
            var result = _handler.Execute(payload) as Dictionary<string, object>;
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            StringAssert.Contains("not supported", result["error"].ToString());
        }

        [Test]
        public void Execute_SupportedOperation_ReturnsSuccess()
        {
            var payload = TestUtilities.CreatePayload("doSomething");
            var result = _handler.Execute(payload) as Dictionary<string, object>;
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("doSomething", result["operation"]);
        }

        [Test]
        public void Execute_ErrorResponse_ContainsCategory()
        {
            var payload = TestUtilities.CreatePayload("unsupported");
            var result = _handler.Execute(payload) as Dictionary<string, object>;
            Assert.IsNotNull(result);
            Assert.AreEqual("test", result["category"]);
        }

        [Test]
        public void Execute_ErrorResponse_ContainsErrorType()
        {
            var result = _handler.Execute(null) as Dictionary<string, object>;
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("errorType"));
        }
    }
}
