using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class McpCommandProcessorTests
    {
        [SetUp]
        public void SetUp()
        {
            // Ensure handlers are initialized
            CommandHandlerFactory.Initialize();
        }

        [Test]
        public void Execute_RegisteredTool_ReturnsResult()
        {
            var command = new McpIncomingCommand("test-1", "pingUnityEditor", new Dictionary<string, object>());
            var result = McpCommandProcessor.Execute(command);
            Assert.IsNotNull(result);
        }

        [Test]
        public void Execute_UnregisteredTool_ThrowsInvalidOperation()
        {
            var command = new McpIncomingCommand("test-2", "nonExistentTool12345", new Dictionary<string, object>());
            Assert.Throws<InvalidOperationException>(() => McpCommandProcessor.Execute(command));
        }

        [Test]
        public void GetHandlerMode_RegisteredTool_ReturnsNewHandler()
        {
            Assert.AreEqual("NewHandler", McpCommandProcessor.GetHandlerMode("gameObjectManage"));
        }

        [Test]
        public void GetHandlerMode_UnregisteredTool_ReturnsUnknown()
        {
            Assert.AreEqual("Unknown", McpCommandProcessor.GetHandlerMode("nonExistentTool12345"));
        }

        [Test]
        public void GetCompilationResult_ReturnsValidStructure()
        {
            var result = McpCommandProcessor.GetCompilationResult();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("success"));
            Assert.IsTrue(result.ContainsKey("errorCount"));
            Assert.IsTrue(result.ContainsKey("warningCount"));
            Assert.IsTrue(result.ContainsKey("errors"));
            Assert.IsTrue(result.ContainsKey("warnings"));
            Assert.IsTrue(result.ContainsKey("consoleLogs"));
        }
    }
}
