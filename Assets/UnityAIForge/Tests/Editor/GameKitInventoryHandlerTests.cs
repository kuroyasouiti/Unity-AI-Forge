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
    /// GameKitInventoryHandler unit tests (Phase 4).
    /// Tests inventory system with items and equipment.
    /// </summary>
    [TestFixture]
    public class GameKitInventoryHandlerTests
    {
        private GameKitInventoryHandler _handler;
        private List<string> _createdAssetPaths;
        private List<GameObject> _createdObjects;
        private const string TestAssetFolder = "Assets/UnityAIForge/Tests/Editor/TestAssets";

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitInventoryHandler();
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
        public void Category_ShouldReturnGamekitInventory()
        {
            Assert.AreEqual("gamekitInventory", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("defineItem", operations);
            Assert.Contains("updateItem", operations);
            Assert.Contains("inspectItem", operations);
            Assert.Contains("deleteItem", operations);
            Assert.Contains("create", operations);
            Assert.Contains("update", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("addItem", operations);
            Assert.Contains("removeItem", operations);
        }

        #endregion

        #region CreateItem Operation Tests

        [Test]
        public void Execute_CreateItem_ShouldCreateItemAsset()
        {
            string assetPath = $"{TestAssetFolder}/TestItem.asset";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "defineItem",
                ["itemId"] = "sword_001",
                ["displayName"] = "Iron Sword",
                ["description"] = "A basic iron sword",
                ["category"] = "Weapon",
                ["assetPath"] = assetPath
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitItemAsset>(assetPath);
            Assert.IsNotNull(asset);
            Assert.AreEqual("sword_001", asset.ItemId);
            Assert.AreEqual("Iron Sword", asset.DisplayName);
            Assert.AreEqual(GameKitItemAsset.ItemCategory.Weapon, asset.Category);
        }

        [Test]
        public void Execute_CreateItem_WithStatModifiers_ShouldAddModifiers()
        {
            string assetPath = $"{TestAssetFolder}/TestItemStats.asset";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "defineItem",
                ["itemId"] = "armor_001",
                ["assetPath"] = assetPath,
                ["itemData"] = new Dictionary<string, object>
                {
                    ["displayName"] = "Steel Armor",
                    ["category"] = "Armor",
                    ["equippable"] = true,
                    ["equipSlot"] = "body",
                    ["equipStats"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["statName"] = "Defense",
                            ["value"] = 15f,
                            ["modifierType"] = "flat"
                        },
                        new Dictionary<string, object>
                        {
                            ["statName"] = "Speed",
                            ["value"] = -2f,
                            ["modifierType"] = "flat"
                        }
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitItemAsset>(assetPath);
            Assert.AreEqual(2, asset.EquipStats.Count);
        }

        [Test]
        public void Execute_CreateItem_Consumable_ShouldSetUseAction()
        {
            string assetPath = $"{TestAssetFolder}/TestItemConsumable.asset";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "defineItem",
                ["itemId"] = "potion_001",
                ["assetPath"] = assetPath,
                ["itemData"] = new Dictionary<string, object>
                {
                    ["displayName"] = "Health Potion",
                    ["category"] = "Consumable",
                    ["onUse"] = new Dictionary<string, object>
                    {
                        ["type"] = "heal",
                        ["amount"] = 50f
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<GameKitItemAsset>(assetPath);
            Assert.AreEqual(GameKitItemAsset.ItemCategory.Consumable, asset.Category);
            Assert.IsNotNull(asset.OnUse);
        }

        #endregion

        #region Create Inventory Operation Tests

        [Test]
        public void Execute_Create_ShouldAddInventoryComponent()
        {
            var go = CreateTestGameObject("TestInventory");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["gameObjectPath"] = "TestInventory",
                ["inventoryId"] = "player_inventory",
                ["maxSlots"] = 20
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var inventory = go.GetComponent<GameKitInventory>();
            Assert.IsNotNull(inventory);
            Assert.AreEqual("player_inventory", inventory.InventoryId);
            Assert.AreEqual(20, inventory.MaxSlots);
        }

        [Test]
        public void Execute_Create_WithMaxSlots_ShouldSetCapacity()
        {
            var go = CreateTestGameObject("TestInventoryCapacity");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["gameObjectPath"] = "TestInventoryCapacity",
                ["maxSlots"] = 30
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var inventory = go.GetComponent<GameKitInventory>();
            Assert.AreEqual(30, inventory.MaxSlots);
        }

        #endregion

        #region AddItem/RemoveItem Operation Tests

        [Test]
        public void Execute_AddItem_ShouldAddItemToInventory()
        {
            // Create item first
            string itemPath = $"{TestAssetFolder}/TestInvItem.asset";
            _createdAssetPaths.Add(itemPath);

            var createItemPayload = new Dictionary<string, object>
            {
                ["operation"] = "defineItem",
                ["itemId"] = "inv_item_001",
                ["displayName"] = "Test Item",
                ["category"] = "Misc",
                ["assetPath"] = itemPath,
                ["itemData"] = new Dictionary<string, object>
                {
                    ["stackable"] = true,
                    ["maxStack"] = 99
                }
            };
            _handler.Execute(createItemPayload);

            // Create inventory
            var go = CreateTestGameObject("TestInventoryAddItem");
            var inventory = go.AddComponent<GameKitInventory>();
            inventory.Initialize("test_inv", 10);

            var addPayload = new Dictionary<string, object>
            {
                ["operation"] = "addItem",
                ["gameObjectPath"] = "TestInventoryAddItem",
                ["itemId"] = "inv_item_001",
                ["quantity"] = 5
            };

            var result = _handler.Execute(addPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(1, inventory.UsedSlots);
        }

        #endregion

        #region Inspect Operations Tests

        [Test]
        public void Execute_InspectItem_ShouldReturnItemInfo()
        {
            string assetPath = $"{TestAssetFolder}/TestItemInspect.asset";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "defineItem",
                ["itemId"] = "inspect_item",
                ["displayName"] = "Inspect Item",
                ["category"] = "Material",
                ["assetPath"] = assetPath
            };
            _handler.Execute(createPayload);

            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspectItem",
                ["assetPath"] = assetPath
            };

            var result = _handler.Execute(inspectPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("inspect_item", result["itemId"]);
        }

        [Test]
        public void Execute_Inspect_ShouldReturnInventoryInfo()
        {
            var go = CreateTestGameObject("TestInventoryInspect");
            var inventory = go.AddComponent<GameKitInventory>();
            // Use reflection to set private fields since properties are read-only
            typeof(GameKitInventory).GetField("inventoryId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(inventory, "inspect_inventory");
            typeof(GameKitInventory).GetField("maxSlots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(inventory, 25);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = "TestInventoryInspect"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("inspect_inventory", result["inventoryId"]);
        }

        #endregion

        #region Delete Operations Tests

        [Test]
        public void Execute_DeleteItem_ShouldRemoveItemAsset()
        {
            string assetPath = $"{TestAssetFolder}/TestItemDelete.asset";

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "defineItem",
                ["itemId"] = "delete_item",
                ["displayName"] = "Delete Item",
                ["category"] = "Misc",
                ["assetPath"] = assetPath
            };
            _handler.Execute(createPayload);

            var deletePayload = new Dictionary<string, object>
            {
                ["operation"] = "deleteItem",
                ["assetPath"] = assetPath
            };

            var result = _handler.Execute(deletePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameKitItemAsset>(assetPath));
        }

        [Test]
        public void Execute_Delete_ShouldRemoveInventoryComponent()
        {
            var go = CreateTestGameObject("TestInventoryDelete");
            go.AddComponent<GameKitInventory>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["gameObjectPath"] = "TestInventoryDelete"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(go.GetComponent<GameKitInventory>());
        }

        #endregion
    }
}
