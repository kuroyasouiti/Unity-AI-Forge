using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// Unit tests for McpCommandProcessor CRUD operations.
    /// These tests verify the core functionality of scene, GameObject, component, and asset operations.
    /// </summary>
    [TestFixture]
    public class McpCommandProcessorTests
    {
        private const string TestScenePath = "Assets/Editor/MCPBridge/Tests/TestScene.unity";
        private const string TestAssetPath = "Assets/Editor/MCPBridge/Tests/TestAsset.txt";

        [SetUp]
        public void Setup()
        {
            // Ensure test directory exists
            var testDir = System.IO.Path.GetDirectoryName(TestScenePath);
            if (!System.IO.Directory.Exists(testDir))
            {
                System.IO.Directory.CreateDirectory(testDir);
            }

            // Create a clean test scene for each test
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SetActiveScene(scene);
        }

        [TearDown]
        public void Teardown()
        {
            // Clean up test assets
            if (System.IO.File.Exists(TestScenePath))
            {
                AssetDatabase.DeleteAsset(TestScenePath);
            }
            if (System.IO.File.Exists(TestAssetPath))
            {
                AssetDatabase.DeleteAsset(TestAssetPath);
            }

            // Clean up any GameObjects created during tests
            var allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                Object.DestroyImmediate(obj);
            }

            AssetDatabase.Refresh();
        }

        #region Ping Test

        [Test]
        public void HandlePing_ReturnsValidResponse()
        {
            // Arrange
            var message = CreateCommandMessage("test-ping", "pingUnityEditor", new Dictionary<string, object>());
            McpIncomingCommand.TryParse(message, out var command);

            // Act
            var result = McpCommandProcessor.Execute(command);

            // Assert
            Assert.IsNotNull(result);
            var resultDict = result as Dictionary<string, object>;
            Assert.IsNotNull(resultDict);
            Assert.IsTrue(resultDict.ContainsKey("editor"));
            Assert.IsTrue(resultDict.ContainsKey("project"));
            Assert.IsTrue(resultDict.ContainsKey("time"));
        }

        #endregion

        #region Scene CRUD Tests

        [Test]
        public void SceneCrud_Create_CreatesNewScene()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["scenePath"] = TestScenePath,
                ["additive"] = false
            };
            var message = CreateCommandMessage("test-scene-create", "sceneCrud", payload);
            McpIncomingCommand.TryParse(message, out var command);

            // Act
            var result = McpCommandProcessor.Execute(command);

            // Assert
            Assert.IsNotNull(result);
            var resultDict = result as Dictionary<string, object>;
            Assert.IsNotNull(resultDict);
            Assert.AreEqual(TestScenePath, resultDict["path"]);
            Assert.IsTrue(System.IO.File.Exists(TestScenePath));
        }

        #endregion

        #region GameObject CRUD Tests

        [Test]
        public void GameObjectCrud_Create_CreatesNewGameObject()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestObject"
            };
            var message = CreateCommandMessage("test-go-create", "gameObjectCrud", payload);
            McpIncomingCommand.TryParse(message, out var command);

            // Act
            var result = McpCommandProcessor.Execute(command);

            // Assert
            Assert.IsNotNull(result);
            var resultDict = result as Dictionary<string, object>;
            Assert.IsNotNull(resultDict);
            Assert.AreEqual("TestObject", resultDict["path"]);

            var go = GameObject.Find("TestObject");
            Assert.IsNotNull(go);
            Assert.AreEqual("TestObject", go.name);
        }

        [Test]
        public void GameObjectCrud_Rename_RenamesGameObject()
        {
            // Arrange
            var testObject = new GameObject("OriginalName");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "rename",
                ["gameObjectPath"] = "OriginalName",
                ["name"] = "NewName"
            };
            var message = CreateCommandMessage("test-go-rename", "gameObjectCrud", payload);
            McpIncomingCommand.TryParse(message, out var command);

            // Act
            var result = McpCommandProcessor.Execute(command);

            // Assert
            Assert.IsNotNull(result);
            var resultDict = result as Dictionary<string, object>;
            Assert.AreEqual("NewName", resultDict["name"]);
            Assert.AreEqual("NewName", testObject.name);
        }

        [Test]
        public void GameObjectCrud_Delete_RemovesGameObject()
        {
            // Arrange
            var testObject = new GameObject("ToDelete");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["gameObjectPath"] = "ToDelete"
            };
            var message = CreateCommandMessage("test-go-delete", "gameObjectCrud", payload);
            McpIncomingCommand.TryParse(message, out var command);

            // Act
            var result = McpCommandProcessor.Execute(command);

            // Assert
            Assert.IsNotNull(result);
            var deletedObject = GameObject.Find("ToDelete");
            Assert.IsNull(deletedObject);
        }

        #endregion

        #region Component CRUD Tests

        [Test]
        public void ComponentCrud_Add_AddsComponentToGameObject()
        {
            // Arrange
            var testObject = new GameObject("TestObject");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["gameObjectPath"] = "TestObject",
                ["componentType"] = "UnityEngine.BoxCollider"
            };
            var message = CreateCommandMessage("test-comp-add", "componentCrud", payload);
            McpIncomingCommand.TryParse(message, out var command);

            // Act
            var result = McpCommandProcessor.Execute(command);

            // Assert
            Assert.IsNotNull(result);
            var component = testObject.GetComponent<BoxCollider>();
            Assert.IsNotNull(component);
        }

        [Test]
        public void ComponentCrud_Remove_RemovesComponentFromGameObject()
        {
            // Arrange
            var testObject = new GameObject("TestObject");
            testObject.AddComponent<BoxCollider>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "remove",
                ["gameObjectPath"] = "TestObject",
                ["componentType"] = "UnityEngine.BoxCollider"
            };
            var message = CreateCommandMessage("test-comp-remove", "componentCrud", payload);
            McpIncomingCommand.TryParse(message, out var command);

            // Act
            var result = McpCommandProcessor.Execute(command);

            // Assert
            Assert.IsNotNull(result);
            var component = testObject.GetComponent<BoxCollider>();
            Assert.IsNull(component);
        }

        [Test]
        public void ComponentCrud_Update_UpdatesComponentProperties()
        {
            // Arrange
            var testObject = new GameObject("TestObject");
            var rigidbody = testObject.AddComponent<Rigidbody>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = "TestObject",
                ["componentType"] = "UnityEngine.Rigidbody",
                ["propertyChanges"] = new Dictionary<string, object>
                {
                    ["mass"] = 5.0,
                    ["drag"] = 2.0
                }
            };
            var message = CreateCommandMessage("test-comp-update", "componentCrud", payload);
            McpIncomingCommand.TryParse(message, out var command);

            // Act
            var result = McpCommandProcessor.Execute(command);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(5.0f, rigidbody.mass, 0.001f);
            Assert.AreEqual(2.0f, rigidbody.drag, 0.001f);
        }

        #endregion

        #region Asset CRUD Tests

        [Test]
        public void AssetCrud_Create_CreatesNewAsset()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["assetPath"] = TestAssetPath,
                ["contents"] = "Test content"
            };
            var message = CreateCommandMessage("test-asset-create", "assetCrud", payload);
            McpIncomingCommand.TryParse(message, out var command);

            // Act
            var result = McpCommandProcessor.Execute(command);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(System.IO.File.Exists(TestAssetPath));
            var content = System.IO.File.ReadAllText(TestAssetPath);
            Assert.AreEqual("Test content", content);
        }

        [Test]
        public void AssetCrud_Delete_RemovesAsset()
        {
            // Arrange
            System.IO.File.WriteAllText(TestAssetPath, "Test content");
            AssetDatabase.ImportAsset(TestAssetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["assetPath"] = TestAssetPath
            };
            var message = CreateCommandMessage("test-asset-delete", "assetCrud", payload);
            McpIncomingCommand.TryParse(message, out var command);

            // Act
            var result = McpCommandProcessor.Execute(command);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(System.IO.File.Exists(TestAssetPath));
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Execute_InvalidToolName_ThrowsException()
        {
            // Arrange
            var message = CreateCommandMessage("test-invalid", "invalidToolName", new Dictionary<string, object>());
            McpIncomingCommand.TryParse(message, out var command);

            // Act & Assert
            Assert.Throws<System.InvalidOperationException>(() => McpCommandProcessor.Execute(command));
        }

        [Test]
        public void GameObjectCrud_MissingGameObject_ThrowsException()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["gameObjectPath"] = "NonExistentObject"
            };
            var message = CreateCommandMessage("test-missing-go", "gameObjectCrud", payload);
            McpIncomingCommand.TryParse(message, out var command);

            // Act & Assert
            Assert.Throws<System.InvalidOperationException>(() => McpCommandProcessor.Execute(command));
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a test command message for McpIncomingCommand.
        /// </summary>
        private Dictionary<string, object> CreateCommandMessage(string commandId, string toolName, Dictionary<string, object> payload)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "command:execute",
                ["commandId"] = commandId,
                ["toolName"] = toolName,
                ["payload"] = payload ?? new Dictionary<string, object>()
            };
        }

        #endregion
    }
}
