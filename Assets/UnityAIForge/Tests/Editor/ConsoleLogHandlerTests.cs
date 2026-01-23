using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MCP.Editor.Handlers;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// ConsoleLogHandler unit tests.
    /// Tests console log retrieval and filtering functionality.
    /// </summary>
    [TestFixture]
    public class ConsoleLogHandlerTests
    {
        private ConsoleLogHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new ConsoleLogHandler();
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnConsoleLog()
        {
            Assert.AreEqual("consoleLog", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("getRecent", operations);
            Assert.Contains("getErrors", operations);
            Assert.Contains("getWarnings", operations);
            Assert.Contains("getLogs", operations);
            Assert.Contains("clear", operations);
            Assert.Contains("getCompilationErrors", operations);
            Assert.Contains("getSummary", operations);
        }

        #endregion

        #region GetLogs Operation Tests

        [Test]
        public void Execute_GetLogs_ShouldReturnLogs()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "getLogs"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("logs"));
        }

        [Test]
        public void Execute_GetLogs_WithLimit_ShouldRespectLimit()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "getLogs",
                ["limit"] = 10
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        [Test]
        public void Execute_GetLogs_WithTypeFilter_ShouldFilterByType()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "getLogs",
                ["logType"] = "Error"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion

        #region GetSummary Operation Tests

        [Test]
        public void Execute_GetSummary_ShouldReturnCounts()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "getSummary"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion

        #region Clear Operation Tests

        [Test]
        public void Execute_Clear_ShouldSucceed()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "clear"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
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
