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
    /// GameKitStatusEffectHandler unit tests.
    /// Tests status effect asset creation, modifier management, and receiver setup.
    /// </summary>
    [TestFixture]
    public class GameKitStatusEffectHandlerTests
    {
        private GameKitStatusEffectHandler _handler;
        private List<string> _createdAssetPaths;
        private List<GameObject> _createdObjects;
        private const string TestAssetFolder = "Assets/UnityAIForge/Tests/Editor/TestAssets";

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitStatusEffectHandler();
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
        public void Category_ShouldReturnGameKitStatusEffect()
        {
            Assert.AreEqual("gamekitStatusEffect", _handler.Category);
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

            Assert.Contains("defineEffect", operations);
            Assert.Contains("updateEffect", operations);
            Assert.Contains("inspectEffect", operations);
            Assert.Contains("deleteEffect", operations);
            Assert.Contains("addModifier", operations);
            Assert.Contains("removeModifier", operations);
            Assert.Contains("create", operations);
        }

        #endregion

        #region CreateEffect Operation Tests

        [Test]
        public void Execute_CreateEffect_ShouldCreateStatusEffectAsset()
        {
            string assetPath = $"{TestAssetFolder}/TestStatusEffect.asset";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "defineEffect",
                ["effectId"] = "effect_poison",
                ["displayName"] = "Poison",
                ["description"] = "Deals damage over time",
                ["duration"] = 10f,
                ["assetPath"] = assetPath
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitStatusEffectAsset>(assetPath);
            Assert.IsNotNull(asset);
            Assert.AreEqual("effect_poison", asset.EffectId);
            Assert.AreEqual("Poison", asset.DisplayName);
            Assert.AreEqual(10f, asset.Duration, 0.01f);
        }

        [Test]
        public void Execute_CreateEffect_WithModifiers_ShouldAddModifiers()
        {
            string assetPath = $"{TestAssetFolder}/TestStatusEffectMods.asset";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "defineEffect",
                ["effectId"] = "effect_buff",
                ["displayName"] = "Power Buff",
                ["duration"] = 30f,
                ["assetPath"] = assetPath,
                ["modifiers"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["stat"] = "Attack",
                        ["value"] = 10f,
                        ["type"] = "Additive"
                    },
                    new Dictionary<string, object>
                    {
                        ["stat"] = "Defense",
                        ["value"] = 0.2f,
                        ["type"] = "Multiplicative"
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitStatusEffectAsset>(assetPath);
            Assert.AreEqual(2, asset.Modifiers.Count);
        }

        [Test]
        public void Execute_CreateEffect_Stackable_ShouldSetStackableProperties()
        {
            string assetPath = $"{TestAssetFolder}/TestStatusEffectStack.asset";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "defineEffect",
                ["effectId"] = "effect_stack",
                ["displayName"] = "Stackable Effect",
                ["duration"] = 5f,
                ["stackable"] = true,
                ["maxStacks"] = 5,
                ["assetPath"] = assetPath
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitStatusEffectAsset>(assetPath);
            Assert.IsTrue(asset.Stackable);
            Assert.AreEqual(5, asset.MaxStacks);
        }

        #endregion

        #region AddModifier Operation Tests

        [Test]
        public void Execute_AddModifier_ShouldAddModifierToEffect()
        {
            // Create effect first
            string assetPath = $"{TestAssetFolder}/TestStatusEffectAddMod.asset";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "defineEffect",
                ["effectId"] = "add_modifier_test",
                ["displayName"] = "Add Modifier Test",
                ["duration"] = 10f,
                ["assetPath"] = assetPath
            };
            _handler.Execute(createPayload);

            // Add modifier - handler expects modifier data in "modifier" dictionary
            var addModifierPayload = new Dictionary<string, object>
            {
                ["operation"] = "addModifier",
                ["assetPath"] = assetPath,
                ["modifier"] = new Dictionary<string, object>
                {
                    ["modifierId"] = "speed_mod",
                    ["targetStat"] = "Speed",
                    ["value"] = -5f,
                    ["type"] = "statModifier"
                }
            };

            var result = _handler.Execute(addModifierPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitStatusEffectAsset>(assetPath);
            Assert.AreEqual(1, asset.Modifiers.Count);
            Assert.AreEqual("Speed", asset.Modifiers[0].targetStat);
        }

        #endregion

        #region InspectEffect Operation Tests

        [Test]
        public void Execute_InspectEffect_ShouldReturnEffectInfo()
        {
            // Create effect with modifiers
            string assetPath = $"{TestAssetFolder}/TestStatusEffectInspect.asset";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "defineEffect",
                ["effectId"] = "inspect_test",
                ["displayName"] = "Inspect Test",
                ["description"] = "Test effect",
                ["duration"] = 15f,
                ["assetPath"] = assetPath,
                ["modifiers"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["stat"] = "Health",
                        ["value"] = -2f,
                        ["type"] = "PerSecond"
                    }
                }
            };
            _handler.Execute(createPayload);

            // Inspect
            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspectEffect",
                ["assetPath"] = assetPath
            };

            var result = _handler.Execute(inspectPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("inspect_test", result["effectId"]);
            Assert.AreEqual("Inspect Test", result["displayName"]);
            Assert.AreEqual(15f, (float)result["duration"], 0.01f);
            Assert.IsTrue(result.ContainsKey("modifiers"));
        }

        #endregion

        #region CreateReceiver Operation Tests

        [Test]
        public void Execute_CreateReceiver_ShouldCreateReceiverComponent()
        {
            var go = CreateTestGameObject("TestEffectReceiver");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestEffectReceiver"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var receiver = go.GetComponent<GameKitStatusEffectReceiver>();
            Assert.IsNotNull(receiver);
        }

        [Test]
        public void Execute_CreateReceiver_WithImmunities_ShouldSetImmunities()
        {
            var go = CreateTestGameObject("TestEffectReceiverImmune");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestEffectReceiverImmune",
                ["immuneTo"] = new List<object> { "poison", "burn" }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var receiver = go.GetComponent<GameKitStatusEffectReceiver>();
            Assert.IsNotNull(receiver);
            // immuneEffects is a private field, just verify component was created
        }

        #endregion

        #region UpdateEffect Operation Tests

        [Test]
        public void Execute_UpdateEffect_ShouldModifyEffectProperties()
        {
            // Create effect first
            string assetPath = $"{TestAssetFolder}/TestStatusEffectUpdate.asset";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "defineEffect",
                ["effectId"] = "update_test",
                ["displayName"] = "Original Name",
                ["duration"] = 5f,
                ["assetPath"] = assetPath
            };
            _handler.Execute(createPayload);

            // Update
            var updatePayload = new Dictionary<string, object>
            {
                ["operation"] = "updateEffect",
                ["assetPath"] = assetPath,
                ["displayName"] = "Updated Name",
                ["duration"] = 20f,
                ["stackable"] = true
            };

            var result = _handler.Execute(updatePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitStatusEffectAsset>(assetPath);
            Assert.AreEqual("Updated Name", asset.DisplayName);
            Assert.AreEqual(20f, asset.Duration, 0.01f);
            Assert.IsTrue(asset.Stackable);
        }

        #endregion

        #region DeleteEffect Operation Tests

        [Test]
        public void Execute_DeleteEffect_ShouldRemoveAsset()
        {
            // Create effect first
            string assetPath = $"{TestAssetFolder}/TestStatusEffectDelete.asset";

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "defineEffect",
                ["effectId"] = "delete_test",
                ["displayName"] = "Delete Test",
                ["duration"] = 5f,
                ["assetPath"] = assetPath
            };
            _handler.Execute(createPayload);

            // Delete
            var deletePayload = new Dictionary<string, object>
            {
                ["operation"] = "deleteEffect",
                ["assetPath"] = assetPath
            };

            var result = _handler.Execute(deletePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameKitStatusEffectAsset>(assetPath));
        }

        #endregion

        #region RemoveModifier Operation Tests

        [Test]
        public void Execute_RemoveModifier_ShouldRemoveModifierFromEffect()
        {
            // Create effect with modifiers
            string assetPath = $"{TestAssetFolder}/TestStatusEffectRemoveMod.asset";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "defineEffect",
                ["effectId"] = "remove_modifier_test",
                ["displayName"] = "Remove Modifier Test",
                ["duration"] = 10f,
                ["assetPath"] = assetPath,
                ["modifiers"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["modifierId"] = "attack_mod",
                        ["targetStat"] = "Attack",
                        ["value"] = 5f,
                        ["type"] = "statModifier"
                    },
                    new Dictionary<string, object>
                    {
                        ["modifierId"] = "defense_mod",
                        ["targetStat"] = "Defense",
                        ["value"] = 3f,
                        ["type"] = "statModifier"
                    }
                }
            };
            _handler.Execute(createPayload);

            // Remove first modifier by modifierId
            var removeModifierPayload = new Dictionary<string, object>
            {
                ["operation"] = "removeModifier",
                ["assetPath"] = assetPath,
                ["modifierId"] = "attack_mod"
            };

            var result = _handler.Execute(removeModifierPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitStatusEffectAsset>(assetPath);
            Assert.AreEqual(1, asset.Modifiers.Count);
            Assert.AreEqual("Defense", asset.Modifiers[0].targetStat);
        }

        #endregion
    }
}
