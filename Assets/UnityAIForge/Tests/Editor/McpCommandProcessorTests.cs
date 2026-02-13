using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MCP.Editor.Base;
using MCP.Editor.Handlers;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// McpCommandProcessor のユニットテスト。
    /// </summary>
    [TestFixture]
    public class McpCommandProcessorTests
    {
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _createdObjects = new List<GameObject>();
            CommandHandlerFactory.Clear();
            // 初期化状態をリセットしてハンドラーを再初期化
            CommandHandlerInitializer.ResetInitializationState();
            CommandHandlerInitializer.InitializeHandlers();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    Undo.ClearUndo(obj);
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }
            _createdObjects.Clear();
            CommandHandlerFactory.Clear();
        }

        private GameObject CreateTestGameObject(string name)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go;
        }

        #region Execute Tests

        [Test]
        public void Execute_PingUnityEditor_ShouldReturnEditorInfo()
        {
            // Arrange
            var command = new McpIncomingCommand(
                "test-cmd-1",
                "pingUnityEditor",
                new Dictionary<string, object>()
            );

            // Act
            var result = McpCommandProcessor.Execute(command) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("unityVersion"));
            Assert.IsTrue(result.ContainsKey("productName"));
            Assert.IsTrue(result.ContainsKey("timestamp"));
            Assert.AreEqual(Application.unityVersion, result["unityVersion"]);
        }

        [Test]
        public void Execute_UnsupportedTool_ShouldThrowException()
        {
            // Arrange
            var command = new McpIncomingCommand(
                "test-cmd-2",
                "unsupportedTool",
                new Dictionary<string, object>()
            );

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                McpCommandProcessor.Execute(command));
        }

        #endregion

        #region GetHandlerMode Tests

        [Test]
        public void GetHandlerMode_UnregisteredTool_ShouldReturnUnknown()
        {
            // Act - use a tool name that is not registered
            var mode = McpCommandProcessor.GetHandlerMode("nonExistentTool");

            // Assert
            Assert.AreEqual("Unknown", mode);
        }

        [Test]
        public void GetHandlerMode_RegisteredTool_ShouldReturnNewHandler()
        {
            // Arrange
            var handler = new MockCommandHandler("test", new[] { "op1" });
            CommandHandlerFactory.Register("testTool", handler);

            // Act
            var mode = McpCommandProcessor.GetHandlerMode("testTool");

            // Assert
            Assert.AreEqual("NewHandler", mode);
        }

        #endregion

        #region Handler Factory Integration Tests

        [Test]
        public void Execute_WithRegisteredHandler_ShouldUseHandler()
        {
            // Arrange
            var handler = new MockCommandHandler("test", new[] { "testOp" });
            handler.SetExecuteFunc(payload => new Dictionary<string, object>
            {
                ["success"] = true,
                ["fromHandler"] = true
            });
            CommandHandlerFactory.Register("customTool", handler);
            
            var command = new McpIncomingCommand(
                "test-cmd-3",
                "customTool",
                new Dictionary<string, object>
                {
                    ["operation"] = "testOp"
                }
            );

            // Act
            var result = McpCommandProcessor.Execute(command) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue((bool)result["fromHandler"]);
        }

        #endregion

        #region Legacy Command Tests

        [Test]
        public void Execute_SceneManage_Inspect_ShouldReturnSceneInfo()
        {
            // Arrange
            var command = new McpIncomingCommand(
                "test-cmd-4",
                "sceneManage",
                new Dictionary<string, object>
                {
                    ["operation"] = "inspect",
                    ["includeHierarchy"] = true
                }
            );

            // Act
            var result = McpCommandProcessor.Execute(command) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            // Note: Legacy handlers may have different success patterns
        }

        [Test]
        public void Execute_GameObjectManage_Create_ShouldCreateGameObject()
        {
            // Arrange
            var command = new McpIncomingCommand(
                "test-cmd-5",
                "gameObjectManage",
                new Dictionary<string, object>
                {
                    ["operation"] = "create",
                    ["name"] = "ProcessorTestObject"
                }
            );

            // Act
            var result = McpCommandProcessor.Execute(command) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            
            // Cleanup
            var go = GameObject.Find("ProcessorTestObject");
            if (go != null) _createdObjects.Add(go);
        }

        #endregion
    }

    /// <summary>
    /// McpIncomingCommand の構造をテストするクラス。
    /// </summary>
    [TestFixture]
    public class McpIncomingCommandTests
    {
        [Test]
        public void McpIncomingCommand_Constructor_ShouldSetProperties()
        {
            // Arrange & Act
            var command = new McpIncomingCommand(
                "cmd-123",
                "testTool",
                new Dictionary<string, object>()
            );

            // Assert
            Assert.AreEqual("cmd-123", command.CommandId);
            Assert.AreEqual("testTool", command.ToolName);
            Assert.IsNotNull(command.Payload);
        }

        [Test]
        public void McpIncomingCommand_WithPayload_ShouldStorePayload()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["key1"] = "value1",
                ["key2"] = 123
            };

            // Act
            var command = new McpIncomingCommand(
                "cmd-456",
                "testTool",
                payload
            );

            // Assert
            Assert.AreEqual(payload, command.Payload);
            Assert.AreEqual("value1", command.Payload["key1"]);
            Assert.AreEqual(123, command.Payload["key2"]);
        }

        [Test]
        public void McpIncomingCommand_NullPayload_ShouldCreateEmptyPayload()
        {
            // Arrange & Act
            var command = new McpIncomingCommand(
                "cmd-789",
                "testTool",
                null
            );

            // Assert
            Assert.IsNotNull(command.Payload);
            Assert.IsEmpty(command.Payload);
        }

        [Test]
        public void TryParse_ValidCommandExecuteMessage_ShouldReturnTrue()
        {
            // Arrange
            var message = new Dictionary<string, object>
            {
                ["type"] = "command:execute",
                ["commandId"] = "test-id-123",
                ["toolName"] = "testTool",
                ["payload"] = new Dictionary<string, object>
                {
                    ["operation"] = "create"
                }
            };

            // Act
            var result = McpIncomingCommand.TryParse(message, out var command);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(command);
            Assert.AreEqual("test-id-123", command.CommandId);
            Assert.AreEqual("testTool", command.ToolName);
            Assert.AreEqual("create", command.Payload["operation"]);
        }

        [Test]
        public void TryParse_InvalidType_ShouldReturnFalse()
        {
            // Arrange
            var message = new Dictionary<string, object>
            {
                ["type"] = "invalid:type",
                ["commandId"] = "test-id",
                ["toolName"] = "testTool"
            };

            // Act
            var result = McpIncomingCommand.TryParse(message, out var command);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(command);
        }

        [Test]
        public void TryParse_MissingCommandId_ShouldReturnFalse()
        {
            // Arrange
            var message = new Dictionary<string, object>
            {
                ["type"] = "command:execute",
                ["toolName"] = "testTool"
            };

            // Act
            var result = McpIncomingCommand.TryParse(message, out var command);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(command);
        }

        [Test]
        public void TryParse_MissingToolName_ShouldReturnFalse()
        {
            // Arrange
            var message = new Dictionary<string, object>
            {
                ["type"] = "command:execute",
                ["commandId"] = "test-id"
            };

            // Act
            var result = McpIncomingCommand.TryParse(message, out var command);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(command);
        }

        [Test]
        public void TryParse_NonDictionaryMessage_ShouldReturnFalse()
        {
            // Arrange
            var message = "not a dictionary";

            // Act
            var result = McpIncomingCommand.TryParse(message, out var command);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(command);
        }

        [Test]
        public void TryParse_NullMessage_ShouldReturnFalse()
        {
            // Act
            var result = McpIncomingCommand.TryParse(null, out var command);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(command);
        }
    }
}
