using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// Unit tests for McpCommandProcessor Management operations.
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

        #region Scene Management Tests

        [Test]
        public void SceneManage_Create_CreatesNewScene()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["scenePath"] = TestScenePath,
                ["additive"] = false
            };
            var message = CreateCommandMessage("test-scene-create", "sceneManage", payload);
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

        #region GameObject Management Tests

        [Test]
        public void GameObjectManage_Create_CreatesNewGameObject()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestObject"
            };
            var message = CreateCommandMessage("test-go-create", "gameObjectManage", payload);
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
        public void GameObjectManage_Rename_RenamesGameObject()
        {
            // Arrange
            var testObject = new GameObject("OriginalName");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "rename",
                ["gameObjectPath"] = "OriginalName",
                ["name"] = "NewName"
            };
            var message = CreateCommandMessage("test-go-rename", "gameObjectManage", payload);
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
        public void GameObjectManage_Delete_RemovesGameObject()
        {
            // Arrange
            var testObject = new GameObject("ToDelete");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["gameObjectPath"] = "ToDelete"
            };
            var message = CreateCommandMessage("test-go-delete", "gameObjectManage", payload);
            McpIncomingCommand.TryParse(message, out var command);

            // Act
            var result = McpCommandProcessor.Execute(command);

            // Assert
            Assert.IsNotNull(result);
            var deletedObject = GameObject.Find("ToDelete");
            Assert.IsNull(deletedObject);
        }

        [Test]
        public void GameObjectManage_Inspect_ReturnsAllAttachedComponents()
        {
            // Arrange
            var testObject = new GameObject("TestObject");
            var rigidbody = testObject.AddComponent<Rigidbody>();
            rigidbody.mass = 3.0f;
            var boxCollider = testObject.AddComponent<BoxCollider>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = "TestObject"
            };
            var message = CreateCommandMessage("test-go-inspect", "gameObjectManage", payload);
            McpIncomingCommand.TryParse(message, out var command);

            // Act
            var result = McpCommandProcessor.Execute(command) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("TestObject", result["gameObject"]);
            Assert.IsTrue(result.TryGetValue("components", out var componentsObj));
            var components = componentsObj as System.Collections.IList;
            Assert.IsNotNull(components);

            // Should have at least Transform (default), Rigidbody, and BoxCollider
            Assert.GreaterOrEqual(components.Count, 3);

            // Verify count field
            Assert.IsTrue(result.TryGetValue("count", out var countObj));
            Assert.AreEqual(components.Count, countObj);

            // Check that we can find our components
            bool foundRigidbody = false;
            bool foundBoxCollider = false;
            foreach (var compObj in components)
            {
                var comp = compObj as Dictionary<string, object>;
                if (comp != null && comp.TryGetValue("type", out var typeObj))
                {
                    var typeName = typeObj as string;
                    if (typeName == "UnityEngine.Rigidbody")
                    {
                        foundRigidbody = true;
                        // Verify properties are included
                        Assert.IsTrue(comp.ContainsKey("properties"));
                        var props = comp["properties"] as Dictionary<string, object>;
                        Assert.IsNotNull(props);
                        Assert.IsTrue(props.ContainsKey("mass"));
                    }
                    else if (typeName == "UnityEngine.BoxCollider")
                    {
                        foundBoxCollider = true;
                    }
                }
            }

            Assert.IsTrue(foundRigidbody, "Rigidbody component not found in list");
            Assert.IsTrue(foundBoxCollider, "BoxCollider component not found in list");
        }

        #endregion

        #region Component Management Tests

        [Test]
        public void ComponentManage_Add_AddsComponentToGameObject()
        {
            // Arrange
            var testObject = new GameObject("TestObject");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["gameObjectPath"] = "TestObject",
                ["componentType"] = "UnityEngine.BoxCollider"
            };
            var message = CreateCommandMessage("test-comp-add", "componentManage", payload);
            McpIncomingCommand.TryParse(message, out var command);

            // Act
            var result = McpCommandProcessor.Execute(command);

            // Assert
            Assert.IsNotNull(result);
            var component = testObject.GetComponent<BoxCollider>();
            Assert.IsNotNull(component);
        }

        [Test]
        public void ComponentManage_Remove_RemovesComponentFromGameObject()
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
            var message = CreateCommandMessage("test-comp-remove", "componentManage", payload);
            McpIncomingCommand.TryParse(message, out var command);

            // Act
            var result = McpCommandProcessor.Execute(command);

            // Assert
            Assert.IsNotNull(result);
            var component = testObject.GetComponent<BoxCollider>();
            Assert.IsNull(component);
        }

        [Test]
        public void ComponentManage_Update_UpdatesComponentProperties()
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
            var message = CreateCommandMessage("test-comp-update", "componentManage", payload);
            McpIncomingCommand.TryParse(message, out var command);

            // Act
            var result = McpCommandProcessor.Execute(command);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(5.0f, rigidbody.mass, 0.001f);
            Assert.AreEqual(2.0f, rigidbody.linearDamping, 0.001f);
        }

        [Test]
        public void ComponentManage_Inspect_ReturnsComponentSnapshot()
        {
            // Arrange
            var testObject = new GameObject("TestObject");
            var light = testObject.AddComponent<Light>();
            light.range = 12.5f;

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = "TestObject",
                ["componentType"] = "UnityEngine.Light"
            };
            var message = CreateCommandMessage("test-comp-inspect", "componentManage", payload);
            McpIncomingCommand.TryParse(message, out var command);

            // Act
            var result = McpCommandProcessor.Execute(command) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("TestObject", result["gameObject"]);
            Assert.AreEqual("UnityEngine.Light", result["type"]);
            Assert.IsTrue(result.TryGetValue("properties", out var propertiesObj));
            var properties = propertiesObj as Dictionary<string, object>;
            Assert.IsNotNull(properties);
            Assert.IsTrue(properties.ContainsKey("range"));
        }

        #endregion

        #region Asset Management Tests

        [Test]
        public void AssetManage_Create_CreatesNewAsset()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["assetPath"] = TestAssetPath,
                ["contents"] = "Test content"
            };
            var message = CreateCommandMessage("test-asset-create", "assetManage", payload);
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
        public void AssetManage_Delete_RemovesAsset()
        {
            // Arrange
            System.IO.File.WriteAllText(TestAssetPath, "Test content");
            AssetDatabase.ImportAsset(TestAssetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["assetPath"] = TestAssetPath
            };
            var message = CreateCommandMessage("test-asset-delete", "assetManage", payload);
            McpIncomingCommand.TryParse(message, out var command);

            // Act
            var result = McpCommandProcessor.Execute(command);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(System.IO.File.Exists(TestAssetPath));
        }

        [Test]
        public void AssetManage_Inspect_ReturnsAssetMetadata()
        {
            // Arrange
            System.IO.File.WriteAllText(TestAssetPath, "Inspect content");
            AssetDatabase.ImportAsset(TestAssetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["assetPath"] = TestAssetPath
            };
            var message = CreateCommandMessage("test-asset-inspect", "assetManage", payload);
            McpIncomingCommand.TryParse(message, out var command);

            // Act
            var result = McpCommandProcessor.Execute(command) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(TestAssetPath, result["path"]);
            Assert.AreEqual(true, result["exists"]);
            Assert.IsTrue(result.ContainsKey("properties"));
        }

        #endregion

        #region Prefab Management Tests

        [Test]
        public void PrefabManage_Create_CreatesNewPrefab()
        {
            // Arrange
            var testObject = new GameObject("PrefabSource");
            testObject.AddComponent<BoxCollider>();

            var prefabPath = "Assets/Editor/MCPBridge/Tests/TestPrefab.prefab";
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["gameObjectPath"] = "PrefabSource",
                ["prefabPath"] = prefabPath
            };
            var message = CreateCommandMessage("test-prefab-create", "prefabManage", payload);
            McpIncomingCommand.TryParse(message, out var command);

            try
            {
                // Act
                var result = McpCommandProcessor.Execute(command);

                // Assert
                Assert.IsNotNull(result);
                var resultDict = result as Dictionary<string, object>;
                Assert.IsNotNull(resultDict);
                Assert.AreEqual(prefabPath, resultDict["prefabPath"]);
                Assert.IsTrue(System.IO.File.Exists(prefabPath));
                Assert.IsTrue(resultDict.ContainsKey("guid"));
            }
            finally
            {
                // Cleanup
                if (System.IO.File.Exists(prefabPath))
                {
                    AssetDatabase.DeleteAsset(prefabPath);
                }
            }
        }

        [Test]
        public void PrefabManage_Instantiate_CreatesInstanceInScene()
        {
            // Arrange - First create a prefab
            var sourceObject = new GameObject("PrefabSource");
            sourceObject.AddComponent<Rigidbody>();

            var prefabPath = "Assets/Editor/MCPBridge/Tests/TestPrefab.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(sourceObject, prefabPath);
            Object.DestroyImmediate(sourceObject);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "instantiate",
                ["prefabPath"] = prefabPath
            };
            var message = CreateCommandMessage("test-prefab-instantiate", "prefabManage", payload);
            McpIncomingCommand.TryParse(message, out var command);

            try
            {
                // Act
                var result = McpCommandProcessor.Execute(command);

                // Assert
                Assert.IsNotNull(result);
                var resultDict = result as Dictionary<string, object>;
                Assert.IsNotNull(resultDict);
                Assert.AreEqual(prefabPath, resultDict["prefabPath"]);
                Assert.IsTrue((bool)resultDict["isPrefabInstance"]);

                var instancePath = resultDict["instancePath"] as string;
                Assert.IsFalse(string.IsNullOrEmpty(instancePath));

                // Verify the instance exists in scene
                var instance = GameObject.Find(instancePath);
                Assert.IsNotNull(instance);
                Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(instance));
            }
            finally
            {
                // Cleanup
                if (System.IO.File.Exists(prefabPath))
                {
                    AssetDatabase.DeleteAsset(prefabPath);
                }
            }
        }

        [Test]
        public void PrefabManage_Inspect_ReturnsPrefabMetadata()
        {
            // Arrange - Create a prefab with components
            var sourceObject = new GameObject("PrefabSource");
            sourceObject.AddComponent<BoxCollider>();
            sourceObject.AddComponent<Rigidbody>();

            var prefabPath = "Assets/Editor/MCPBridge/Tests/TestPrefab.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(sourceObject, prefabPath);
            Object.DestroyImmediate(sourceObject);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["prefabPath"] = prefabPath
            };
            var message = CreateCommandMessage("test-prefab-inspect", "prefabManage", payload);
            McpIncomingCommand.TryParse(message, out var command);

            try
            {
                // Act
                var result = McpCommandProcessor.Execute(command) as Dictionary<string, object>;

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(prefabPath, result["prefabPath"]);
                Assert.IsTrue(result.ContainsKey("guid"));
                Assert.IsTrue(result.ContainsKey("components"));
                Assert.IsTrue((bool)result["isPrefabAsset"]);

                var components = result["components"] as System.Collections.IList;
                Assert.IsNotNull(components);
                // Should have Transform, BoxCollider, Rigidbody
                Assert.GreaterOrEqual(components.Count, 3);
            }
            finally
            {
                // Cleanup
                if (System.IO.File.Exists(prefabPath))
                {
                    AssetDatabase.DeleteAsset(prefabPath);
                }
            }
        }

        [Test]
        public void PrefabManage_Unpack_UnpacksPrefabInstance()
        {
            // Arrange - Create prefab and instantiate it
            var sourceObject = new GameObject("PrefabSource");
            var prefabPath = "Assets/Editor/MCPBridge/Tests/TestPrefab.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(sourceObject, prefabPath);
            Object.DestroyImmediate(sourceObject);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var instancePath = instance.name;

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "unpack",
                ["gameObjectPath"] = instancePath,
                ["unpackMode"] = "OutermostRoot"
            };
            var message = CreateCommandMessage("test-prefab-unpack", "prefabManage", payload);
            McpIncomingCommand.TryParse(message, out var command);

            try
            {
                // Verify it's a prefab instance before unpacking
                Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(instance));

                // Act
                var result = McpCommandProcessor.Execute(command) as Dictionary<string, object>;

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(instancePath, result["gameObjectPath"]);
                Assert.IsFalse((bool)result["isPrefabInstance"]);

                // Verify the instance is no longer a prefab instance
                Assert.IsFalse(PrefabUtility.IsPartOfPrefabInstance(instance));
            }
            finally
            {
                // Cleanup
                if (System.IO.File.Exists(prefabPath))
                {
                    AssetDatabase.DeleteAsset(prefabPath);
                }
            }
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
        public void GameObjectManage_MissingGameObject_ThrowsException()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["gameObjectPath"] = "NonExistentObject"
            };
            var message = CreateCommandMessage("test-missing-go", "gameObjectManage", payload);
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
