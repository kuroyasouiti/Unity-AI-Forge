using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class ConsoleLogHandlerTests
    {
        private ConsoleLogHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new ConsoleLogHandler();
        }

        [Test]
        public void Category_ReturnsConsoleLog()
        {
            Assert.AreEqual("consoleLog", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("getRecent", ops);
            Assert.Contains("getErrors", ops);
            Assert.Contains("getWarnings", ops);
            Assert.Contains("getLogs", ops);
            Assert.Contains("clear", ops);
            Assert.Contains("getSummary", ops);
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
        public void GetRecent_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("getRecent"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void GetSummary_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("getSummary"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void Clear_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("clear"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void SupportedOperations_ContainsNewOps()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("snapshot", ops);
            Assert.Contains("diff", ops);
            Assert.Contains("filter", ops);
        }

        [Test]
        public void Snapshot_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("snapshot"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        [Order(1)]
        public void Diff_WithoutSnapshot_ReturnsError()
        {
            // This test must run before any snapshot is taken.
            // Static _snapshotLogs starts as null, so diff should fail.
            TestUtilities.AssertError(_handler.Execute(TestUtilities.CreatePayload("diff")),
                "No snapshot taken");
        }

        [Test]
        [Order(2)]
        public void Diff_AfterSnapshot_ReturnsSuccess()
        {
            _handler.Execute(TestUtilities.CreatePayload("snapshot"));
            var result = _handler.Execute(TestUtilities.CreatePayload("diff"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void Filter_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("filter"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void Filter_WithKeyword_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("filter",
                ("keyword", "test")));
            TestUtilities.AssertSuccess(result);
        }
    }
}
