using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MCP.Editor.Handlers.GameKit;
using UnityAIForge.GameKit;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// GameKitDialogueHandler unit tests.
    /// Tests dialogue asset creation, node management, and manager setup.
    /// </summary>
    [TestFixture]
    public class GameKitDialogueHandlerTests
    {
        private GameKitDialogueHandler _handler;
        private List<string> _createdAssetPaths;
        private List<GameObject> _createdObjects;
        private const string TestAssetFolder = "Assets/UnityAIForge/Tests/Editor/TestAssets";

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitDialogueHandler();
            _createdAssetPaths = new List<string>();
            _createdObjects = new List<GameObject>();

            // Ensure test folder exists
            if (!AssetDatabase.IsValidFolder(TestAssetFolder))
            {
                string parentFolder = Path.GetDirectoryName(TestAssetFolder).Replace("\\", "/");
                string folderName = Path.GetFileName(TestAssetFolder);
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up GameObjects
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    Undo.ClearUndo(obj);
                    Object.DestroyImmediate(obj);
                }
            }
            _createdObjects.Clear();

            // Clean up assets
            foreach (var path in _createdAssetPaths)
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
            _createdAssetPaths.Clear();
            AssetDatabase.Refresh();
        }

        private GameObject CreateTestGameObject(string name)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go;
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnGameKitDialogue()
        {
            Assert.AreEqual("gamekitDialogue", _handler.Category);
        }

        [Test]
        public void Version_ShouldReturn100()
        {
            Assert.AreEqual("1.0.0", _handler.Version);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("createDialogue", operations);
            Assert.Contains("updateDialogue", operations);
            Assert.Contains("inspectDialogue", operations);
            Assert.Contains("deleteDialogue", operations);
            Assert.Contains("addNode", operations);
            Assert.Contains("removeNode", operations);
            Assert.Contains("addChoice", operations);
            Assert.Contains("removeChoice", operations);
            Assert.Contains("createManager", operations);
        }

        #endregion

        #region CreateDialogue Operation Tests

        [Test]
        public void Execute_CreateDialogue_ShouldCreateDialogueAsset()
        {
            string assetPath = $"{TestAssetFolder}/TestDialogue.asset";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createDialogue",
                ["dialogueId"] = "test_dialogue_001",
                ["title"] = "Test Dialogue",
                ["assetPath"] = assetPath
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitDialogueAsset>(assetPath);
            Assert.IsNotNull(asset);
            Assert.AreEqual("test_dialogue_001", asset.DialogueId);
        }

        [Test]
        public void Execute_CreateDialogue_WithNodes_ShouldAddNodes()
        {
            string assetPath = $"{TestAssetFolder}/TestDialogueNodes.asset";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createDialogue",
                ["dialogueId"] = "test_dialogue_nodes",
                ["title"] = "Test Dialogue With Nodes",
                ["assetPath"] = assetPath,
                ["nodes"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["nodeId"] = "node_start",
                        ["speaker"] = "NPC",
                        ["text"] = "Hello, adventurer!"
                    },
                    new Dictionary<string, object>
                    {
                        ["nodeId"] = "node_response",
                        ["speaker"] = "Player",
                        ["text"] = "Hello!"
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitDialogueAsset>(assetPath);
            Assert.AreEqual(2, asset.Nodes.Count);
        }

        #endregion

        #region AddNode Operation Tests

        [Test]
        public void Execute_AddNode_ShouldAddNodeToDialogue()
        {
            // Create dialogue first
            string assetPath = $"{TestAssetFolder}/TestDialogueAddNode.asset";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "createDialogue",
                ["dialogueId"] = "add_node_test",
                ["title"] = "Add Node Test",
                ["assetPath"] = assetPath
            };
            _handler.Execute(createPayload);

            // Add node
            var addNodePayload = new Dictionary<string, object>
            {
                ["operation"] = "addNode",
                ["assetPath"] = assetPath,
                ["node"] = new Dictionary<string, object>
                {
                    ["nodeId"] = "new_node",
                    ["speaker"] = "Merchant",
                    ["text"] = "Welcome to my shop!"
                }
            };

            var result = _handler.Execute(addNodePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitDialogueAsset>(assetPath);
            Assert.AreEqual(1, asset.Nodes.Count);
            Assert.AreEqual("new_node", asset.Nodes[0].nodeId);
        }

        #endregion

        #region AddChoice Operation Tests

        [Test]
        public void Execute_AddChoice_ShouldAddChoiceToNode()
        {
            // Create dialogue with node
            string assetPath = $"{TestAssetFolder}/TestDialogueAddChoice.asset";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "createDialogue",
                ["dialogueId"] = "add_choice_test",
                ["title"] = "Add Choice Test",
                ["assetPath"] = assetPath,
                ["nodes"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["nodeId"] = "question_node",
                        ["speaker"] = "NPC",
                        ["text"] = "What would you like to do?"
                    }
                }
            };
            _handler.Execute(createPayload);

            // Add choice - use assetPath for reliable asset resolution
            var addChoicePayload = new Dictionary<string, object>
            {
                ["operation"] = "addChoice",
                ["assetPath"] = assetPath,
                ["nodeId"] = "question_node",
                ["choice"] = new Dictionary<string, object>
                {
                    ["text"] = "Buy items",
                    ["nextNodeId"] = "shop_node"
                }
            };

            var result = _handler.Execute(addChoicePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitDialogueAsset>(assetPath);
            Assert.AreEqual(1, asset.Nodes[0].choices.Count);
        }

        #endregion

        #region InspectDialogue Operation Tests

        [Test]
        public void Execute_InspectDialogue_ShouldReturnDialogueInfo()
        {
            // Create dialogue with nodes
            string assetPath = $"{TestAssetFolder}/TestDialogueInspect.asset";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "createDialogue",
                ["dialogueId"] = "inspect_test",
                ["title"] = "Inspect Test",
                ["assetPath"] = assetPath,
                ["nodes"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["nodeId"] = "node1",
                        ["speaker"] = "NPC",
                        ["text"] = "Test"
                    }
                }
            };
            _handler.Execute(createPayload);

            // Inspect
            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspectDialogue",
                ["assetPath"] = assetPath
            };

            var result = _handler.Execute(inspectPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("inspect_test", result["dialogueId"]);
            Assert.IsTrue(result.ContainsKey("nodes"));
        }

        #endregion

        #region CreateManager Operation Tests

        [Test]
        public void Execute_CreateManager_ShouldCreateManagerComponent()
        {
            var go = CreateTestGameObject("TestDialogueManager");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createManager",
                ["targetPath"] = "TestDialogueManager"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var manager = go.GetComponent<GameKitDialogueManager>();
            Assert.IsNotNull(manager);
        }

        #endregion

        #region DeleteDialogue Operation Tests

        [Test]
        public void Execute_DeleteDialogue_ShouldRemoveAsset()
        {
            // Create dialogue first
            string assetPath = $"{TestAssetFolder}/TestDialogueDelete.asset";

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "createDialogue",
                ["dialogueId"] = "delete_test",
                ["title"] = "Delete Test",
                ["assetPath"] = assetPath
            };
            _handler.Execute(createPayload);

            // Delete
            var deletePayload = new Dictionary<string, object>
            {
                ["operation"] = "deleteDialogue",
                ["assetPath"] = assetPath
            };

            var result = _handler.Execute(deletePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameKitDialogueAsset>(assetPath));
        }

        #endregion
    }
}
