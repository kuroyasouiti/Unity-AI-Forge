using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MCP.Editor.Base;
using MCP.Editor.Interfaces;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// CommandHandlerFactory のユニットテスト。
    /// </summary>
    [TestFixture]
    public class CommandHandlerFactoryTests
    {
        [SetUp]
        public void SetUp()
        {
            // 各テスト前にファクトリーをクリア
            CommandHandlerFactory.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            // テスト後にファクトリーをクリア
            CommandHandlerFactory.Clear();
        }

        #region Register Tests

        [Test]
        public void Register_ValidHandler_ShouldSucceed()
        {
            // Arrange
            var handler = new MockCommandHandler("test", new[] { "op1", "op2" });

            // Act
            CommandHandlerFactory.Register("testTool", handler);

            // Assert
            Assert.IsTrue(CommandHandlerFactory.IsRegistered("testTool"));
        }

        [Test]
        public void Register_NullToolName_ShouldThrowArgumentNullException()
        {
            // Arrange
            var handler = new MockCommandHandler("test", new[] { "op1" });

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                CommandHandlerFactory.Register(null, handler));
        }

        [Test]
        public void Register_EmptyToolName_ShouldThrowArgumentNullException()
        {
            // Arrange
            var handler = new MockCommandHandler("test", new[] { "op1" });

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                CommandHandlerFactory.Register("", handler));
        }

        [Test]
        public void Register_NullHandler_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                CommandHandlerFactory.Register("testTool", null));
        }

        [Test]
        public void Register_DuplicateToolName_ShouldOverwrite()
        {
            // Arrange
            var handler1 = new MockCommandHandler("category1", new[] { "op1" });
            var handler2 = new MockCommandHandler("category2", new[] { "op2" });

            // Act
            CommandHandlerFactory.Register("testTool", handler1);
            CommandHandlerFactory.Register("testTool", handler2);

            // Assert
            var result = CommandHandlerFactory.GetHandler("testTool");
            Assert.AreEqual("category2", result.Category);
        }

        #endregion

        #region GetHandler Tests

        [Test]
        public void GetHandler_RegisteredHandler_ShouldReturnHandler()
        {
            // Arrange
            var handler = new MockCommandHandler("test", new[] { "op1" });
            CommandHandlerFactory.Register("testTool", handler);

            // Act
            var result = CommandHandlerFactory.GetHandler("testTool");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(handler, result);
        }

        [Test]
        public void GetHandler_UnregisteredHandler_ShouldThrowInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                CommandHandlerFactory.GetHandler("nonExistentTool"));
        }

        #endregion

        #region TryGetHandler Tests

        [Test]
        public void TryGetHandler_RegisteredHandler_ShouldReturnTrue()
        {
            // Arrange
            var handler = new MockCommandHandler("test", new[] { "op1" });
            CommandHandlerFactory.Register("testTool", handler);

            // Act
            var result = CommandHandlerFactory.TryGetHandler("testTool", out var retrievedHandler);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(retrievedHandler);
            Assert.AreEqual(handler, retrievedHandler);
        }

        [Test]
        public void TryGetHandler_UnregisteredHandler_ShouldReturnFalse()
        {
            // Act
            var result = CommandHandlerFactory.TryGetHandler("nonExistentTool", out var handler);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(handler);
        }

        #endregion

        #region IsRegistered Tests

        [Test]
        public void IsRegistered_RegisteredHandler_ShouldReturnTrue()
        {
            // Arrange
            var handler = new MockCommandHandler("test", new[] { "op1" });
            CommandHandlerFactory.Register("testTool", handler);

            // Act & Assert
            Assert.IsTrue(CommandHandlerFactory.IsRegistered("testTool"));
        }

        [Test]
        public void IsRegistered_UnregisteredHandler_ShouldReturnFalse()
        {
            // Act & Assert
            Assert.IsFalse(CommandHandlerFactory.IsRegistered("nonExistentTool"));
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_WithRegisteredHandlers_ShouldRemoveAll()
        {
            // Arrange
            CommandHandlerFactory.Register("tool1", new MockCommandHandler("cat1", new[] { "op1" }));
            CommandHandlerFactory.Register("tool2", new MockCommandHandler("cat2", new[] { "op2" }));

            // Act
            CommandHandlerFactory.Clear();

            // Assert
            Assert.IsFalse(CommandHandlerFactory.IsRegistered("tool1"));
            Assert.IsFalse(CommandHandlerFactory.IsRegistered("tool2"));
        }

        #endregion

        #region GetRegisteredToolNames Tests

        [Test]
        public void GetRegisteredToolNames_WithHandlers_ShouldReturnAllNames()
        {
            // Arrange
            CommandHandlerFactory.Register("tool1", new MockCommandHandler("cat1", new[] { "op1" }));
            CommandHandlerFactory.Register("tool2", new MockCommandHandler("cat2", new[] { "op2" }));
            CommandHandlerFactory.Register("tool3", new MockCommandHandler("cat3", new[] { "op3" }));

            // Act
            var names = CommandHandlerFactory.GetRegisteredToolNames().ToList();

            // Assert
            Assert.AreEqual(3, names.Count);
            Assert.Contains("tool1", names);
            Assert.Contains("tool2", names);
            Assert.Contains("tool3", names);
        }

        [Test]
        public void GetRegisteredToolNames_NoHandlers_ShouldReturnEmpty()
        {
            // Act
            var names = CommandHandlerFactory.GetRegisteredToolNames().ToList();

            // Assert
            Assert.IsEmpty(names);
        }

        #endregion

        #region GetStatistics Tests

        [Test]
        public void GetStatistics_WithHandlers_ShouldReturnCorrectStats()
        {
            // Arrange
            CommandHandlerFactory.Register("tool1", new MockCommandHandler("cat1", new[] { "op1", "op2" }));
            CommandHandlerFactory.Register("tool2", new MockCommandHandler("cat2", new[] { "op3" }));

            // Act
            var stats = CommandHandlerFactory.GetStatistics();

            // Assert
            Assert.AreEqual(2, stats["totalHandlers"]);
            Assert.IsTrue((bool)stats["initialized"]);
            
            var handlers = (List<Dictionary<string, object>>)stats["registeredHandlers"];
            Assert.AreEqual(2, handlers.Count);
        }

        #endregion
    }

    /// <summary>
    /// テスト用のモックコマンドハンドラー。
    /// </summary>
    public class MockCommandHandler : ICommandHandler
    {
        private readonly string _category;
        private readonly string[] _supportedOperations;
        private readonly string _version;
        private Func<Dictionary<string, object>, object> _executeFunc;

        public MockCommandHandler(string category, string[] supportedOperations, string version = "1.0.0")
        {
            _category = category;
            _supportedOperations = supportedOperations;
            _version = version;
        }

        public string Category => _category;
        public string Version => _version;
        public IEnumerable<string> SupportedOperations => _supportedOperations;

        public void SetExecuteFunc(Func<Dictionary<string, object>, object> func)
        {
            _executeFunc = func;
        }

        public object Execute(Dictionary<string, object> payload)
        {
            if (_executeFunc != null)
            {
                return _executeFunc(payload);
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["category"] = _category,
                ["operation"] = payload.ContainsKey("operation") ? payload["operation"] : "unknown"
            };
        }
    }
}
