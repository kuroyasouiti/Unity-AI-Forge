using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MCP.Editor.Handlers.GameKit;
using UnityAIForge.GameKit;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// GameKitCombatHandler unit tests (3-Pillar Architecture - Logic).
    /// Tests combat system creation, damage configuration, and target management.
    /// </summary>
    [TestFixture]
    public class GameKitCombatHandlerTests
    {
        private GameKitCombatHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitCombatHandler();
            _createdObjects = new List<GameObject>();
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
        }

        private GameObject CreateTestGameObject(string name)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go;
        }

        private void SetSerializedField(Component component, string fieldName, object value)
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                if (value is string strValue)
                    prop.stringValue = strValue;
                else if (value is int intValue)
                    prop.intValue = intValue;
                else if (value is float floatValue)
                    prop.floatValue = floatValue;
                else if (value is bool boolValue)
                    prop.boolValue = boolValue;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnGamekitCombat()
        {
            Assert.AreEqual("gamekitCombat", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("create", operations);
            Assert.Contains("update", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("addTargetTag", operations);
            Assert.Contains("removeTargetTag", operations);
            Assert.Contains("resetCooldown", operations);
            Assert.Contains("findByCombatId", operations);
        }

        #endregion

        #region Create Operation Tests

        [Test]
        public void Execute_Create_ShouldAddCombatComponent()
        {
            var go = CreateTestGameObject("TestCombat");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestCombat",
                ["combatId"] = "test_combat",
                ["baseDamage"] = 10f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var combat = go.GetComponent<GameKitCombat>();
            Assert.IsNotNull(combat);
            Assert.AreEqual("test_combat", combat.CombatId);
        }

        [Test]
        public void Execute_Create_WithoutTargetPath_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        [Test]
        public void Execute_Create_WithDamageConfig_ShouldSetDamage()
        {
            var go = CreateTestGameObject("TestCombatDamage");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestCombatDamage",
                ["combatId"] = "damage_test",
                ["baseDamage"] = 25f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var combat = go.GetComponent<GameKitCombat>();
            Assert.AreEqual(25f, combat.BaseDamage, 0.01f);
        }

        #endregion

        #region Delete Operation Tests

        [Test]
        public void Execute_Delete_ShouldRemoveComponent()
        {
            var go = CreateTestGameObject("TestCombatDelete");
            go.AddComponent<GameKitCombat>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["targetPath"] = "TestCombatDelete"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(go.GetComponent<GameKitCombat>());
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnCombatInfo()
        {
            var go = CreateTestGameObject("TestCombatInspect");
            var combat = go.AddComponent<GameKitCombat>();
            SetSerializedField(combat, "combatId", "inspect_test");
            SetSerializedField(combat, "baseDamage", 15f);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["targetPath"] = "TestCombatInspect"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            var combatInfo = result["combat"] as Dictionary<string, object>;
            Assert.IsNotNull(combatInfo);
            Assert.AreEqual("inspect_test", combatInfo["combatId"]);
        }

        #endregion
    }
}
