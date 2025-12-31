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
    /// GameKitSaveHandler unit tests (Phase 4).
    /// Tests save/load system with profiles and slots.
    /// </summary>
    [TestFixture]
    public class GameKitSaveHandlerTests
    {
        private GameKitSaveHandler _handler;
        private List<string> _createdAssetPaths;
        private List<GameObject> _createdObjects;
        private const string TestAssetFolder = "Assets/UnityAIForge/Tests/Editor/TestAssets";

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitSaveHandler();
            _createdAssetPaths = new List<string>();
            _createdObjects = new List<GameObject>();

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
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    Undo.ClearUndo(obj);
                    Object.DestroyImmediate(obj);
                }
            }
            _createdObjects.Clear();

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
        public void Category_ShouldReturnGamekitSave()
        {
            Assert.AreEqual("gamekitSave", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("createProfile", operations);
            Assert.Contains("updateProfile", operations);
            Assert.Contains("inspectProfile", operations);
            Assert.Contains("deleteProfile", operations);
            Assert.Contains("addTarget", operations);
            Assert.Contains("removeTarget", operations);
            Assert.Contains("createManager", operations);
        }

        #endregion

        #region CreateProfile Operation Tests

        [Test]
        public void Execute_CreateProfile_ShouldCreateSaveProfileAsset()
        {
            string assetPath = $"{TestAssetFolder}/TestSaveProfile.asset";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createProfile",
                ["profileId"] = "main_save",
                ["displayName"] = "Main Save Profile",
                ["assetPath"] = assetPath
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitSaveProfile>(assetPath);
            Assert.IsNotNull(asset);
            Assert.AreEqual("main_save", asset.ProfileId);
        }

        [Test]
        public void Execute_CreateProfile_WithAutoSave_ShouldEnableAutoSave()
        {
            string assetPath = $"{TestAssetFolder}/TestSaveProfileAuto.asset";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createProfile",
                ["profileId"] = "auto_save",
                ["assetPath"] = assetPath,
                ["autoSave"] = new Dictionary<string, object>
                {
                    ["enabled"] = true,
                    ["intervalSeconds"] = 60f
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitSaveProfile>(assetPath);
            Assert.IsTrue(asset.AutoSaveConfig.enabled);
            Assert.AreEqual(60f, asset.AutoSaveConfig.intervalSeconds, 0.01f);
        }

        [Test]
        public void Execute_CreateProfile_WithTargets_ShouldAddSaveTargets()
        {
            string assetPath = $"{TestAssetFolder}/TestSaveProfileTargets.asset";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createProfile",
                ["profileId"] = "targets_save",
                ["assetPath"] = assetPath,
                ["saveTargets"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["type"] = "transform",
                        ["saveKey"] = "player_position"
                    },
                    new Dictionary<string, object>
                    {
                        ["type"] = "playerPrefs",
                        ["saveKey"] = "high_score"
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitSaveProfile>(assetPath);
            Assert.AreEqual(2, asset.SaveTargets.Count);
        }

        #endregion

        #region AddTarget Operation Tests

        [Test]
        public void Execute_AddTarget_ShouldAddTargetToProfile()
        {
            string assetPath = $"{TestAssetFolder}/TestSaveAddTarget.asset";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "createProfile",
                ["profileId"] = "add_target_save",
                ["assetPath"] = assetPath
            };
            _handler.Execute(createPayload);

            var addPayload = new Dictionary<string, object>
            {
                ["operation"] = "addTarget",
                ["assetPath"] = assetPath,
                ["target"] = new Dictionary<string, object>
                {
                    ["type"] = "health",
                    ["saveKey"] = "player_health"
                }
            };

            var result = _handler.Execute(addPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitSaveProfile>(assetPath);
            Assert.AreEqual(1, asset.SaveTargets.Count);
        }

        #endregion

        #region CreateManager Operation Tests

        [Test]
        public void Execute_CreateManager_ShouldCreateSaveManagerComponent()
        {
            var go = CreateTestGameObject("TestSaveManager");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createManager",
                ["targetPath"] = "TestSaveManager"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var manager = go.GetComponent<GameKitSaveManager>();
            Assert.IsNotNull(manager);
        }

        #endregion

        #region InspectProfile Operation Tests

        [Test]
        public void Execute_InspectProfile_ShouldReturnProfileInfo()
        {
            string assetPath = $"{TestAssetFolder}/TestSaveInspect.asset";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "createProfile",
                ["profileId"] = "inspect_save",
                ["displayName"] = "Inspect Save",
                ["assetPath"] = assetPath
            };
            _handler.Execute(createPayload);

            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspectProfile",
                ["assetPath"] = assetPath
            };

            var result = _handler.Execute(inspectPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("inspect_save", result["profileId"]);
        }

        #endregion

        #region DeleteProfile Operation Tests

        [Test]
        public void Execute_DeleteProfile_ShouldRemoveProfileAsset()
        {
            string assetPath = $"{TestAssetFolder}/TestSaveDelete.asset";

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "createProfile",
                ["profileId"] = "delete_save",
                ["assetPath"] = assetPath
            };
            _handler.Execute(createPayload);

            var deletePayload = new Dictionary<string, object>
            {
                ["operation"] = "deleteProfile",
                ["assetPath"] = assetPath
            };

            var result = _handler.Execute(deletePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameKitSaveProfile>(assetPath));
        }

        #endregion
    }
}
