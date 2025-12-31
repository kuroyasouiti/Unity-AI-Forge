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
    /// GameKitEffectHandler unit tests (Phase 3).
    /// Tests composite effect system (particle + sound + camera shake + screen flash).
    /// </summary>
    [TestFixture]
    public class GameKitEffectHandlerTests
    {
        private GameKitEffectHandler _handler;
        private List<string> _createdAssetPaths;
        private List<GameObject> _createdObjects;
        private const string TestAssetFolder = "Assets/UnityAIForge/Tests/Editor/TestAssets";

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitEffectHandler();
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
        public void Category_ShouldReturnGamekitEffect()
        {
            Assert.AreEqual("gamekitEffect", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("create", operations);
            Assert.Contains("update", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("addComponent", operations);
            Assert.Contains("removeComponent", operations);
            Assert.Contains("createManager", operations);
        }

        #endregion

        #region Create Operation Tests

        [Test]
        public void Execute_Create_ShouldCreateEffectAsset()
        {
            string assetPath = $"{TestAssetFolder}/TestEffect.asset";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["effectId"] = "explosion_effect",
                ["displayName"] = "Explosion Effect",
                ["assetPath"] = assetPath
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitEffectAsset>(assetPath);
            Assert.IsNotNull(asset);
            Assert.AreEqual("explosion_effect", asset.EffectId);
        }

        [Test]
        public void Execute_Create_WithComponents_ShouldAddEffectComponents()
        {
            string assetPath = $"{TestAssetFolder}/TestEffectComponents.asset";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["effectId"] = "hit_effect",
                ["displayName"] = "Hit Effect",
                ["assetPath"] = assetPath,
                ["components"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["type"] = "CameraShake",
                        ["intensity"] = 0.5f,
                        ["duration"] = 0.2f
                    },
                    new Dictionary<string, object>
                    {
                        ["type"] = "ScreenFlash",
                        ["color"] = new Dictionary<string, object> { ["r"] = 1f, ["g"] = 0f, ["b"] = 0f, ["a"] = 0.3f },
                        ["duration"] = 0.1f
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitEffectAsset>(assetPath);
            Assert.AreEqual(2, asset.Components.Count);
        }

        #endregion

        #region AddComponent Operation Tests

        [Test]
        public void Execute_AddComponent_ShouldAddComponentToEffect()
        {
            string assetPath = $"{TestAssetFolder}/TestEffectAddComp.asset";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["effectId"] = "add_comp_effect",
                ["assetPath"] = assetPath
            };
            _handler.Execute(createPayload);

            var addPayload = new Dictionary<string, object>
            {
                ["operation"] = "addComponent",
                ["assetPath"] = assetPath,
                ["component"] = new Dictionary<string, object>
                {
                    ["type"] = "TimeScale",
                    ["targetTimeScale"] = 0.1f,
                    ["timeScaleDuration"] = 0.5f
                }
            };

            var result = _handler.Execute(addPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitEffectAsset>(assetPath);
            Assert.AreEqual(1, asset.Components.Count);
        }

        #endregion

        #region CreateManager Operation Tests

        [Test]
        public void Execute_CreateManager_ShouldCreateEffectManagerComponent()
        {
            var go = CreateTestGameObject("TestEffectManager");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createManager",
                ["targetPath"] = "TestEffectManager"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var manager = go.GetComponent<GameKitEffectManager>();
            Assert.IsNotNull(manager);
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnEffectInfo()
        {
            string assetPath = $"{TestAssetFolder}/TestEffectInspect.asset";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["effectId"] = "inspect_effect",
                ["displayName"] = "Inspect Effect",
                ["assetPath"] = assetPath
            };
            _handler.Execute(createPayload);

            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["assetPath"] = assetPath
            };

            var result = _handler.Execute(inspectPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("inspect_effect", result["effectId"]);
        }

        #endregion

        #region Delete Operation Tests

        [Test]
        public void Execute_Delete_ShouldRemoveEffectAsset()
        {
            string assetPath = $"{TestAssetFolder}/TestEffectDelete.asset";

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["effectId"] = "delete_effect",
                ["assetPath"] = assetPath
            };
            _handler.Execute(createPayload);

            var deletePayload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["assetPath"] = assetPath
            };

            var result = _handler.Execute(deletePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameKitEffectAsset>(assetPath));
        }

        #endregion
    }
}
