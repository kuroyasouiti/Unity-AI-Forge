using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// Unit tests for CompilationAwaitHandler.
    /// </summary>
    [TestFixture]
    public class CompilationAwaitHandlerTests
    {
        private CompilationAwaitHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new CompilationAwaitHandler();
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnCompilationAwait()
        {
            Assert.AreEqual("compilationAwait", _handler.Category);
        }

        [Test]
        public void Version_ShouldReturn100()
        {
            Assert.AreEqual("1.0.0", _handler.Version);
        }

        [Test]
        public void SupportedOperations_ShouldContainAwaitAndStatus()
        {
            var operations = new List<string>(_handler.SupportedOperations);
            Assert.Contains("await", operations);
            Assert.Contains("status", operations);
        }

        #endregion

        #region Status Operation Tests

        [Test]
        public void Execute_Status_ShouldReturnIsCompilingFlag()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "status"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("isCompiling"));
        }

        [Test]
        public void Execute_Status_ShouldReturnCompilationCompleted()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "status"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("compilationCompleted"));
        }

        [Test]
        public void Execute_Status_ShouldReturnErrorCount()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "status"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("errorCount"));
        }

        #endregion

        #region Await Operation Tests

        [Test]
        public void Execute_Await_ShouldReturnIsCompilingFlag()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "await"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("isCompiling"));
        }

        [Test]
        public void Execute_Await_ShouldReturnWaitTimeSeconds()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "await"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("waitTimeSeconds"));
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Execute_UnsupportedOperation_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "unsupportedOperation"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion
    }
}
