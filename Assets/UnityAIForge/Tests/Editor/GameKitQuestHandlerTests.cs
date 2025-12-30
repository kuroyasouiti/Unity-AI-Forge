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
    /// GameKitQuestHandler unit tests.
    /// Tests quest asset creation, objective management, and manager setup.
    /// </summary>
    [TestFixture]
    public class GameKitQuestHandlerTests
    {
        private GameKitQuestHandler _handler;
        private List<string> _createdAssetPaths;
        private List<GameObject> _createdObjects;
        private const string TestAssetFolder = "Assets/UnityAIForge/Tests/Editor/TestAssets";

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitQuestHandler();
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
        public void Category_ShouldReturnGameKitQuest()
        {
            Assert.AreEqual("gamekitQuest", _handler.Category);
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

            Assert.Contains("createQuest", operations);
            Assert.Contains("updateQuest", operations);
            Assert.Contains("inspectQuest", operations);
            Assert.Contains("deleteQuest", operations);
            Assert.Contains("addObjective", operations);
            Assert.Contains("removeObjective", operations);
            Assert.Contains("addReward", operations);
            Assert.Contains("removeReward", operations);
            Assert.Contains("createManager", operations);
        }

        #endregion

        #region CreateQuest Operation Tests

        [Test]
        public void Execute_CreateQuest_ShouldCreateQuestAsset()
        {
            string assetPath = $"{TestAssetFolder}/TestQuest.asset";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createQuest",
                ["questId"] = "quest_001",
                ["title"] = "The First Quest",
                ["description"] = "Complete this quest to prove your worth.",
                ["assetPath"] = assetPath
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitQuestAsset>(assetPath);
            Assert.IsNotNull(asset);
            Assert.AreEqual("quest_001", asset.QuestId);
            Assert.AreEqual("The First Quest", asset.Title);
        }

        [Test]
        public void Execute_CreateQuest_WithObjectives_ShouldAddObjectives()
        {
            string assetPath = $"{TestAssetFolder}/TestQuestObjectives.asset";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createQuest",
                ["questId"] = "quest_objectives",
                ["title"] = "Quest with Objectives",
                ["assetPath"] = assetPath,
                ["objectives"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["objectiveId"] = "obj_kill",
                        ["description"] = "Defeat 10 enemies",
                        ["type"] = "Kill",
                        ["targetCount"] = 10
                    },
                    new Dictionary<string, object>
                    {
                        ["objectiveId"] = "obj_collect",
                        ["description"] = "Collect 5 items",
                        ["type"] = "Collect",
                        ["targetCount"] = 5
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitQuestAsset>(assetPath);
            Assert.AreEqual(2, asset.Objectives.Count);
        }

        [Test]
        public void Execute_CreateQuest_WithRewards_ShouldAddRewards()
        {
            string assetPath = $"{TestAssetFolder}/TestQuestRewards.asset";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createQuest",
                ["questId"] = "quest_rewards",
                ["title"] = "Quest with Rewards",
                ["assetPath"] = assetPath,
                ["rewards"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["type"] = "Experience",
                        ["amount"] = 100
                    },
                    new Dictionary<string, object>
                    {
                        ["type"] = "Gold",
                        ["amount"] = 50
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitQuestAsset>(assetPath);
            Assert.AreEqual(2, asset.Rewards.Count);
        }

        #endregion

        #region AddObjective Operation Tests

        [Test]
        public void Execute_AddObjective_ShouldAddObjectiveToQuest()
        {
            // Create quest first
            string assetPath = $"{TestAssetFolder}/TestQuestAddObjective.asset";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "createQuest",
                ["questId"] = "add_objective_test",
                ["title"] = "Add Objective Test",
                ["assetPath"] = assetPath
            };
            _handler.Execute(createPayload);

            // Add objective
            var addObjectivePayload = new Dictionary<string, object>
            {
                ["operation"] = "addObjective",
                ["assetPath"] = assetPath,
                ["objectiveId"] = "new_objective",
                ["description"] = "Talk to the merchant",
                ["type"] = "Interact",
                ["targetCount"] = 1
            };

            var result = _handler.Execute(addObjectivePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitQuestAsset>(assetPath);
            Assert.AreEqual(1, asset.Objectives.Count);
            Assert.AreEqual("new_objective", asset.Objectives[0].objectiveId);
        }

        #endregion

        #region AddReward Operation Tests

        [Test]
        public void Execute_AddReward_ShouldAddRewardToQuest()
        {
            // Create quest first
            string assetPath = $"{TestAssetFolder}/TestQuestAddReward.asset";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "createQuest",
                ["questId"] = "add_reward_test",
                ["title"] = "Add Reward Test",
                ["assetPath"] = assetPath
            };
            _handler.Execute(createPayload);

            // Add reward
            var addRewardPayload = new Dictionary<string, object>
            {
                ["operation"] = "addReward",
                ["assetPath"] = assetPath,
                ["type"] = "Item",
                ["itemId"] = "sword_001",
                ["amount"] = 1
            };

            var result = _handler.Execute(addRewardPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitQuestAsset>(assetPath);
            Assert.AreEqual(1, asset.Rewards.Count);
        }

        #endregion

        #region InspectQuest Operation Tests

        [Test]
        public void Execute_InspectQuest_ShouldReturnQuestInfo()
        {
            // Create quest with objectives
            string assetPath = $"{TestAssetFolder}/TestQuestInspect.asset";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "createQuest",
                ["questId"] = "inspect_test",
                ["title"] = "Inspect Test Quest",
                ["description"] = "A test quest",
                ["assetPath"] = assetPath,
                ["objectives"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["objectiveId"] = "obj1",
                        ["description"] = "Test objective",
                        ["type"] = "Kill",
                        ["targetCount"] = 5
                    }
                }
            };
            _handler.Execute(createPayload);

            // Inspect
            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspectQuest",
                ["assetPath"] = assetPath
            };

            var result = _handler.Execute(inspectPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("inspect_test", result["questId"]);
            Assert.AreEqual("Inspect Test Quest", result["title"]);
            Assert.IsTrue(result.ContainsKey("objectives"));
            Assert.IsTrue(result.ContainsKey("rewards"));
        }

        #endregion

        #region CreateManager Operation Tests

        [Test]
        public void Execute_CreateManager_ShouldCreateManagerComponent()
        {
            var go = CreateTestGameObject("TestQuestManager");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createManager",
                ["gameObjectPath"] = "TestQuestManager"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var manager = go.GetComponent<GameKitQuestManager>();
            Assert.IsNotNull(manager);
        }

        #endregion

        #region UpdateQuest Operation Tests

        [Test]
        public void Execute_UpdateQuest_ShouldModifyQuestProperties()
        {
            // Create quest first
            string assetPath = $"{TestAssetFolder}/TestQuestUpdate.asset";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "createQuest",
                ["questId"] = "update_test",
                ["title"] = "Original Title",
                ["assetPath"] = assetPath
            };
            _handler.Execute(createPayload);

            // Update
            var updatePayload = new Dictionary<string, object>
            {
                ["operation"] = "updateQuest",
                ["assetPath"] = assetPath,
                ["title"] = "Updated Title",
                ["description"] = "New description"
            };

            var result = _handler.Execute(updatePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitQuestAsset>(assetPath);
            Assert.AreEqual("Updated Title", asset.Title);
            Assert.AreEqual("New description", asset.Description);
        }

        #endregion

        #region DeleteQuest Operation Tests

        [Test]
        public void Execute_DeleteQuest_ShouldRemoveAsset()
        {
            // Create quest first
            string assetPath = $"{TestAssetFolder}/TestQuestDelete.asset";

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "createQuest",
                ["questId"] = "delete_test",
                ["title"] = "Delete Test",
                ["assetPath"] = assetPath
            };
            _handler.Execute(createPayload);

            // Delete
            var deletePayload = new Dictionary<string, object>
            {
                ["operation"] = "deleteQuest",
                ["assetPath"] = assetPath
            };

            var result = _handler.Execute(deletePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameKitQuestAsset>(assetPath));
        }

        #endregion
    }
}
