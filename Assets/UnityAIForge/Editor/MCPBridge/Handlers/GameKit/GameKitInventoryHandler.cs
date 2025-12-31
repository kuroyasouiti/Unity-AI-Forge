using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCP.Editor.Base;
using UnityAIForge.GameKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Inventory handler: create and manage inventory systems.
    /// Provides item definition, inventory creation, and item management without custom scripts.
    /// </summary>
    public class GameKitInventoryHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "update", "inspect", "delete",
            "defineItem", "updateItem", "inspectItem", "deleteItem",
            "addItem", "removeItem", "useItem",
            "equip", "unequip", "getEquipped",
            "clear", "sort",
            "findByInventoryId", "findByItemId"
        };

        public override string Category => "gamekitInventory";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateInventory(payload),
                "update" => UpdateInventory(payload),
                "inspect" => InspectInventory(payload),
                "delete" => DeleteInventory(payload),
                "defineItem" => DefineItem(payload),
                "updateItem" => UpdateItem(payload),
                "inspectItem" => InspectItem(payload),
                "deleteItem" => DeleteItem(payload),
                "addItem" => AddItem(payload),
                "removeItem" => RemoveItem(payload),
                "useItem" => UseItem(payload),
                "equip" => Equip(payload),
                "unequip" => Unequip(payload),
                "getEquipped" => GetEquipped(payload),
                "clear" => ClearInventory(payload),
                "sort" => SortInventory(payload),
                "findByInventoryId" => FindByInventoryId(payload),
                "findByItemId" => FindByItemId(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit Inventory operation: {operation}")
            };
        }

        #region Inventory Operations

        private object CreateInventory(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(targetPath))
            {
                throw new InvalidOperationException("gameObjectPath is required for create operation.");
            }

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
            {
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            var existingInventory = targetGo.GetComponent<GameKitInventory>();
            if (existingInventory != null)
            {
                throw new InvalidOperationException($"GameObject '{targetPath}' already has a GameKitInventory component.");
            }

            var inventoryId = GetString(payload, "inventoryId") ?? $"Inventory_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var maxSlots = GetInt(payload, "maxSlots", 20);

            // Parse categories
            List<string> categories = null;
            if (payload.TryGetValue("categories", out var catObj) && catObj is List<object> catList)
            {
                categories = catList.Select(c => c.ToString().ToLowerInvariant()).ToList();
            }

            // Parse stackable categories
            List<string> stackableCategories = null;
            if (payload.TryGetValue("stackableCategories", out var stackObj) && stackObj is List<object> stackList)
            {
                stackableCategories = stackList.Select(s => s.ToString().ToLowerInvariant()).ToList();
            }

            var maxStackSize = GetInt(payload, "maxStackSize", 99);

            // Create component
            var inventory = Undo.AddComponent<GameKitInventory>(targetGo);
            inventory.Initialize(inventoryId, maxSlots, categories, stackableCategories, maxStackSize);

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("inventoryId", inventoryId),
                ("path", BuildGameObjectPath(targetGo)),
                ("maxSlots", maxSlots),
                ("categories", categories ?? new List<string>()),
                ("stackableCategories", stackableCategories ?? new List<string>())
            );
        }

        private object UpdateInventory(Dictionary<string, object> payload)
        {
            var inventory = ResolveInventory(payload);
            if (inventory == null)
            {
                throw new InvalidOperationException("Could not find GameKitInventory.");
            }

            Undo.RecordObject(inventory, "Update GameKitInventory");

            var serialized = new SerializedObject(inventory);

            if (payload.TryGetValue("maxSlots", out var slotsObj))
            {
                serialized.FindProperty("maxSlots").intValue = Convert.ToInt32(slotsObj);
            }

            if (payload.TryGetValue("categories", out var catObj) && catObj is List<object> catList)
            {
                var prop = serialized.FindProperty("allowedCategories");
                prop.ClearArray();
                foreach (var cat in catList)
                {
                    prop.InsertArrayElementAtIndex(prop.arraySize);
                    prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = cat.ToString().ToLowerInvariant();
                }
            }

            if (payload.TryGetValue("stackableCategories", out var stackObj) && stackObj is List<object> stackList)
            {
                var prop = serialized.FindProperty("stackableCategories");
                prop.ClearArray();
                foreach (var stack in stackList)
                {
                    prop.InsertArrayElementAtIndex(prop.arraySize);
                    prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = stack.ToString().ToLowerInvariant();
                }
            }

            if (payload.TryGetValue("maxStackSize", out var maxStackObj))
            {
                serialized.FindProperty("defaultMaxStack").intValue = Convert.ToInt32(maxStackObj);
            }

            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(inventory.gameObject.scene);

            return CreateSuccessResponse(
                ("inventoryId", inventory.InventoryId),
                ("updated", true)
            );
        }

        private object InspectInventory(Dictionary<string, object> payload)
        {
            var inventory = ResolveInventory(payload);
            if (inventory == null)
            {
                throw new InvalidOperationException("Could not find GameKitInventory.");
            }

            var slots = new List<object>();
            foreach (var slot in inventory.Slots)
            {
                if (!slot.IsEmpty)
                {
                    slots.Add(new Dictionary<string, object>
                    {
                        { "itemId", slot.ItemId },
                        { "quantity", slot.Quantity },
                        { "displayName", slot.ItemAsset?.DisplayName ?? "" }
                    });
                }
            }

            var equipped = new List<object>();
            foreach (var item in inventory.EquippedItems)
            {
                equipped.Add(new Dictionary<string, object>
                {
                    { "equipSlot", item.EquipSlot },
                    { "itemId", item.ItemId },
                    { "displayName", item.ItemAsset?.DisplayName ?? "" }
                });
            }

            return CreateSuccessResponse(
                ("inventoryId", inventory.InventoryId),
                ("path", BuildGameObjectPath(inventory.gameObject)),
                ("maxSlots", inventory.MaxSlots),
                ("usedSlots", inventory.UsedSlots),
                ("freeSlots", inventory.FreeSlots),
                ("isFull", inventory.IsFull),
                ("slots", slots),
                ("equipped", equipped)
            );
        }

        private object DeleteInventory(Dictionary<string, object> payload)
        {
            var inventory = ResolveInventory(payload);
            if (inventory == null)
            {
                throw new InvalidOperationException("Could not find GameKitInventory.");
            }

            var path = BuildGameObjectPath(inventory.gameObject);
            var inventoryId = inventory.InventoryId;
            var scene = inventory.gameObject.scene;

            Undo.DestroyObjectImmediate(inventory);
            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("inventoryId", inventoryId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Item Definition Operations

        private object DefineItem(Dictionary<string, object> payload)
        {
            var itemId = GetString(payload, "itemId");
            if (string.IsNullOrEmpty(itemId))
            {
                itemId = $"Item_{Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            var assetPath = GetString(payload, "assetPath");
            if (string.IsNullOrEmpty(assetPath))
            {
                assetPath = $"Assets/GameKit/Items/{itemId}.asset";
            }

            var directory = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var existingAsset = AssetDatabase.LoadAssetAtPath<GameKitItemAsset>(assetPath);
            if (existingAsset != null)
            {
                throw new InvalidOperationException($"Item asset already exists at: {assetPath}");
            }

            // Check for any existing asset with the same itemId (at any path) and delete it
            // This prevents stale assets from interfering with FindItemAssetById
            var existingGuids = AssetDatabase.FindAssets("t:GameKitItemAsset");
            foreach (var guid in existingGuids)
            {
                var existingPath = AssetDatabase.GUIDToAssetPath(guid);
                var existingItem = AssetDatabase.LoadAssetAtPath<GameKitItemAsset>(existingPath);
                if (existingItem != null && existingItem.ItemId == itemId)
                {
                    AssetDatabase.DeleteAsset(existingPath);
                }
            }

            // Create ScriptableObject instance
            var item = ScriptableObject.CreateInstance<GameKitItemAsset>();

            // Get basic properties
            string displayName, description, categoryStr;
            Dictionary<string, object> itemData = null;

            if (payload.TryGetValue("itemData", out var dataObj) && dataObj is Dictionary<string, object> data)
            {
                itemData = data;
                displayName = GetStringFromDict(data, "displayName", null) ?? GetString(payload, "displayName") ?? itemId;
                description = GetStringFromDict(data, "description", null) ?? GetString(payload, "description") ?? "";
                categoryStr = GetStringFromDict(data, "category", null) ?? GetString(payload, "category") ?? "misc";
            }
            else
            {
                displayName = GetString(payload, "displayName") ?? itemId;
                description = GetString(payload, "description") ?? "";
                categoryStr = GetString(payload, "category") ?? "misc";
            }

            var category = ParseItemCategory(categoryStr);

            // Initialize basic properties on the in-memory instance BEFORE CreateAsset
            item.Initialize(itemId, displayName, description, category);

            // Set stacking properties BEFORE CreateAsset
            if (itemData != null && itemData.TryGetValue("stackable", out var stackableObj))
            {
                bool isStackable = Convert.ToBoolean(stackableObj);
                int maxStack = itemData.TryGetValue("maxStack", out var maxStackObj) ? Convert.ToInt32(maxStackObj) : 99;
                item.SetStacking(isStackable, maxStack);
            }

            // Set prices BEFORE CreateAsset
            if (itemData != null)
            {
                int buyPrice = itemData.TryGetValue("buyPrice", out var buyObj) ? Convert.ToInt32(buyObj) : 0;
                int sellPrice = itemData.TryGetValue("sellPrice", out var sellObj) ? Convert.ToInt32(sellObj) : 0;
                item.SetPrices(buyPrice, sellPrice);
            }

            // Now create the asset with fully configured instance
            AssetDatabase.CreateAsset(item, assetPath);

            // Use SerializedObject for equipment and use action (complex nested types)
            if (itemData != null)
            {
                var serializedObject = new SerializedObject(item);
                bool needsApply = false;

                // Equipment
                if (itemData.TryGetValue("equippable", out var equipObj) && Convert.ToBoolean(equipObj))
                {
                    serializedObject.FindProperty("equippable").boolValue = true;
                    if (itemData.TryGetValue("equipSlot", out var slotObj))
                    {
                        var equipSlot = ParseEquipmentSlot(slotObj.ToString());
                        serializedObject.FindProperty("equipSlot").enumValueIndex = (int)equipSlot;
                    }

                    // Equipment stats
                    if (itemData.TryGetValue("equipStats", out var statsObj) && statsObj is List<object> statsList)
                    {
                        var statsProp = serializedObject.FindProperty("equipStats");
                        statsProp.ClearArray();
                        foreach (var statObj in statsList)
                        {
                            if (statObj is Dictionary<string, object> statDict)
                            {
                                statsProp.InsertArrayElementAtIndex(statsProp.arraySize);
                                var elem = statsProp.GetArrayElementAtIndex(statsProp.arraySize - 1);
                                elem.FindPropertyRelative("statName").stringValue = GetStringFromDict(statDict, "statName", "");
                                elem.FindPropertyRelative("modifierType").enumValueIndex = (int)ParseModifierType(GetStringFromDict(statDict, "modifierType", "flat"));
                                elem.FindPropertyRelative("value").floatValue = statDict.TryGetValue("value", out var valObj) ? Convert.ToSingle(valObj) : 0;
                            }
                        }
                    }
                    needsApply = true;
                }

                // Use action
                if (itemData.TryGetValue("onUse", out var useObj) && useObj is Dictionary<string, object> useData)
                {
                    var onUseProp = serializedObject.FindProperty("onUse");
                    if (useData.TryGetValue("type", out var typeObj))
                    {
                        var useType = ParseUseActionType(typeObj.ToString());
                        onUseProp.FindPropertyRelative("type").enumValueIndex = (int)useType;
                    }
                    if (useData.TryGetValue("amount", out var amountObj))
                    {
                        onUseProp.FindPropertyRelative("healAmount").floatValue = Convert.ToSingle(amountObj);
                        onUseProp.FindPropertyRelative("resourceAmount").floatValue = Convert.ToSingle(amountObj);
                    }
                    if (useData.TryGetValue("healthId", out var healthIdObj))
                    {
                        onUseProp.FindPropertyRelative("healthId").stringValue = healthIdObj.ToString();
                    }
                    if (useData.TryGetValue("consumeOnUse", out var consumeObj))
                    {
                        onUseProp.FindPropertyRelative("consumeOnUse").boolValue = Convert.ToBoolean(consumeObj);
                    }
                    needsApply = true;
                }

                if (needsApply)
                {
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // Mark dirty and save to disk
            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssets();

            // Force reserialize to ensure binary data is written to disk
            AssetDatabase.ForceReserializeAssets(
                new[] { assetPath },
                ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata
            );

            // Release cached file handles and refresh asset database
            AssetDatabase.ReleaseCachedFileHandles();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            // Force reimport to ensure disk state is loaded fresh
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            // Reload the asset fresh from disk
            var savedItem = AssetDatabase.LoadAssetAtPath<GameKitItemAsset>(assetPath);

            return CreateSuccessResponse(
                ("itemId", itemId),
                ("assetPath", assetPath),
                ("displayName", savedItem?.DisplayName ?? itemId),
                ("stackable", savedItem?.Stackable ?? false),
                ("maxStack", savedItem?.MaxStack ?? 1)
            );
        }

        private object UpdateItem(Dictionary<string, object> payload)
        {
            var item = ResolveItemAsset(payload);
            if (item == null)
            {
                throw new InvalidOperationException("Could not find item asset.");
            }

            Undo.RecordObject(item, "Update Item Asset");

            if (payload.TryGetValue("itemData", out var dataObj) && dataObj is Dictionary<string, object> itemData)
            {
                ConfigureItemAsset(item, item.ItemId, itemData);
            }

            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("itemId", item.ItemId),
                ("updated", true)
            );
        }

        private object InspectItem(Dictionary<string, object> payload)
        {
            var item = ResolveItemAsset(payload);
            if (item == null)
            {
                throw new InvalidOperationException("Could not find item asset.");
            }

            var equipStats = new List<object>();
            foreach (var stat in item.EquipStats)
            {
                equipStats.Add(new Dictionary<string, object>
                {
                    { "statName", stat.statName },
                    { "modifierType", stat.modifierType.ToString() },
                    { "value", stat.value }
                });
            }

            return CreateSuccessResponse(
                ("itemId", item.ItemId),
                ("assetPath", AssetDatabase.GetAssetPath(item)),
                ("displayName", item.DisplayName),
                ("description", item.Description),
                ("category", item.Category.ToString()),
                ("stackable", item.Stackable),
                ("maxStack", item.MaxStack),
                ("equippable", item.Equippable),
                ("equipSlot", item.EquipSlot.ToString()),
                ("equipStats", equipStats),
                ("buyPrice", item.BuyPrice),
                ("sellPrice", item.SellPrice)
            );
        }

        private object DeleteItem(Dictionary<string, object> payload)
        {
            var item = ResolveItemAsset(payload);
            if (item == null)
            {
                throw new InvalidOperationException("Could not find item asset.");
            }

            var assetPath = AssetDatabase.GetAssetPath(item);
            var itemId = item.ItemId;

            AssetDatabase.DeleteAsset(assetPath);

            return CreateSuccessResponse(
                ("itemId", itemId),
                ("assetPath", assetPath),
                ("deleted", true)
            );
        }

        #endregion

        #region Inventory Item Operations

        private object AddItem(Dictionary<string, object> payload)
        {
            var inventory = ResolveInventory(payload);
            if (inventory == null)
            {
                throw new InvalidOperationException("Could not find GameKitInventory.");
            }

            var itemId = GetString(payload, "itemId");
            if (string.IsNullOrEmpty(itemId))
            {
                throw new InvalidOperationException("itemId is required to add an item.");
            }

            var quantity = GetInt(payload, "quantity", 1);

            // Refresh to ensure we get the latest asset data from disk
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var item = FindItemAssetById(itemId);
            if (item == null)
            {
                throw new InvalidOperationException($"Item asset not found: {itemId}");
            }

            Undo.RecordObject(inventory, "Add Item");
            var added = inventory.AddItem(item, quantity);
            EditorSceneManager.MarkSceneDirty(inventory.gameObject.scene);

            return CreateSuccessResponse(
                ("inventoryId", inventory.InventoryId),
                ("itemId", itemId),
                ("requested", quantity),
                ("added", added),
                ("remaining", quantity - added)
            );
        }

        private object RemoveItem(Dictionary<string, object> payload)
        {
            var inventory = ResolveInventory(payload);
            if (inventory == null)
            {
                throw new InvalidOperationException("Could not find GameKitInventory.");
            }

            var itemId = GetString(payload, "itemId");
            if (string.IsNullOrEmpty(itemId))
            {
                throw new InvalidOperationException("itemId is required to remove an item.");
            }

            var quantity = GetInt(payload, "quantity", 1);

            Undo.RecordObject(inventory, "Remove Item");
            var removed = inventory.RemoveItem(itemId, quantity);
            EditorSceneManager.MarkSceneDirty(inventory.gameObject.scene);

            return CreateSuccessResponse(
                ("inventoryId", inventory.InventoryId),
                ("itemId", itemId),
                ("requested", quantity),
                ("removed", removed)
            );
        }

        private object UseItem(Dictionary<string, object> payload)
        {
            var inventory = ResolveInventory(payload);
            if (inventory == null)
            {
                throw new InvalidOperationException("Could not find GameKitInventory.");
            }

            var slotIndex = GetInt(payload, "slotIndex", -1);
            if (slotIndex < 0)
            {
                throw new InvalidOperationException("slotIndex is required to use an item.");
            }

            // Note: Using items in editor mode has limited functionality
            var slot = inventory.GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty)
            {
                return CreateSuccessResponse(
                    ("used", false),
                    ("reason", "Slot is empty")
                );
            }

            return CreateSuccessResponse(
                ("used", false),
                ("note", "Item use actions are executed in play mode only."),
                ("slotIndex", slotIndex),
                ("itemId", slot.ItemId)
            );
        }

        private object Equip(Dictionary<string, object> payload)
        {
            var inventory = ResolveInventory(payload);
            if (inventory == null)
            {
                throw new InvalidOperationException("Could not find GameKitInventory.");
            }

            var slotIndex = GetInt(payload, "slotIndex", -1);
            if (slotIndex < 0)
            {
                throw new InvalidOperationException("slotIndex is required to equip an item.");
            }

            var equipSlot = GetString(payload, "equipSlot");

            Undo.RecordObject(inventory, "Equip Item");
            var success = inventory.Equip(slotIndex, equipSlot);
            EditorSceneManager.MarkSceneDirty(inventory.gameObject.scene);

            var slot = inventory.GetSlot(slotIndex);

            return CreateSuccessResponse(
                ("inventoryId", inventory.InventoryId),
                ("equipped", success),
                ("slotIndex", slotIndex),
                ("equipSlot", equipSlot ?? "default")
            );
        }

        private object Unequip(Dictionary<string, object> payload)
        {
            var inventory = ResolveInventory(payload);
            if (inventory == null)
            {
                throw new InvalidOperationException("Could not find GameKitInventory.");
            }

            var equipSlot = GetString(payload, "equipSlot");
            if (string.IsNullOrEmpty(equipSlot))
            {
                throw new InvalidOperationException("equipSlot is required to unequip an item.");
            }

            Undo.RecordObject(inventory, "Unequip Item");
            var success = inventory.Unequip(equipSlot);
            EditorSceneManager.MarkSceneDirty(inventory.gameObject.scene);

            return CreateSuccessResponse(
                ("inventoryId", inventory.InventoryId),
                ("unequipped", success),
                ("equipSlot", equipSlot)
            );
        }

        private object GetEquipped(Dictionary<string, object> payload)
        {
            var inventory = ResolveInventory(payload);
            if (inventory == null)
            {
                throw new InvalidOperationException("Could not find GameKitInventory.");
            }

            var equipSlot = GetString(payload, "equipSlot");
            if (!string.IsNullOrEmpty(equipSlot))
            {
                var equipped = inventory.GetEquipped(equipSlot);
                if (equipped == null)
                {
                    return CreateSuccessResponse(
                        ("equipSlot", equipSlot),
                        ("equipped", false)
                    );
                }

                return CreateSuccessResponse(
                    ("equipSlot", equipSlot),
                    ("equipped", true),
                    ("itemId", equipped.ItemId),
                    ("displayName", equipped.ItemAsset?.DisplayName ?? "")
                );
            }

            // Return all equipped
            var all = new List<object>();
            foreach (var item in inventory.EquippedItems)
            {
                all.Add(new Dictionary<string, object>
                {
                    { "equipSlot", item.EquipSlot },
                    { "itemId", item.ItemId },
                    { "displayName", item.ItemAsset?.DisplayName ?? "" }
                });
            }

            return CreateSuccessResponse(
                ("inventoryId", inventory.InventoryId),
                ("equippedItems", all)
            );
        }

        private object ClearInventory(Dictionary<string, object> payload)
        {
            var inventory = ResolveInventory(payload);
            if (inventory == null)
            {
                throw new InvalidOperationException("Could not find GameKitInventory.");
            }

            Undo.RecordObject(inventory, "Clear Inventory");
            inventory.Clear();
            EditorSceneManager.MarkSceneDirty(inventory.gameObject.scene);

            return CreateSuccessResponse(
                ("inventoryId", inventory.InventoryId),
                ("cleared", true)
            );
        }

        private object SortInventory(Dictionary<string, object> payload)
        {
            var inventory = ResolveInventory(payload);
            if (inventory == null)
            {
                throw new InvalidOperationException("Could not find GameKitInventory.");
            }

            Undo.RecordObject(inventory, "Sort Inventory");
            inventory.Sort();
            EditorSceneManager.MarkSceneDirty(inventory.gameObject.scene);

            return CreateSuccessResponse(
                ("inventoryId", inventory.InventoryId),
                ("sorted", true)
            );
        }

        #endregion

        #region Find Operations

        private object FindByInventoryId(Dictionary<string, object> payload)
        {
            var inventoryId = GetString(payload, "inventoryId");
            if (string.IsNullOrEmpty(inventoryId))
            {
                throw new InvalidOperationException("inventoryId is required for findByInventoryId.");
            }

            var inventory = FindInventoryById(inventoryId);
            if (inventory == null)
            {
                return CreateSuccessResponse(("found", false), ("inventoryId", inventoryId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("inventoryId", inventory.InventoryId),
                ("path", BuildGameObjectPath(inventory.gameObject)),
                ("usedSlots", inventory.UsedSlots),
                ("maxSlots", inventory.MaxSlots)
            );
        }

        private object FindByItemId(Dictionary<string, object> payload)
        {
            var itemId = GetString(payload, "itemId");
            if (string.IsNullOrEmpty(itemId))
            {
                throw new InvalidOperationException("itemId is required for findByItemId.");
            }

            var item = FindItemAssetById(itemId);
            if (item == null)
            {
                return CreateSuccessResponse(("found", false), ("itemId", itemId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("itemId", item.ItemId),
                ("assetPath", AssetDatabase.GetAssetPath(item)),
                ("displayName", item.DisplayName)
            );
        }

        #endregion

        #region Helpers

        private GameKitInventory ResolveInventory(Dictionary<string, object> payload)
        {
            var inventoryId = GetString(payload, "inventoryId");
            if (!string.IsNullOrEmpty(inventoryId))
            {
                return FindInventoryById(inventoryId);
            }

            var targetPath = GetString(payload, "gameObjectPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    return targetGo.GetComponent<GameKitInventory>();
                }
            }

            return null;
        }

        private GameKitInventory FindInventoryById(string inventoryId)
        {
            var inventories = UnityEngine.Object.FindObjectsByType<GameKitInventory>(FindObjectsSortMode.None);
            foreach (var inv in inventories)
            {
                if (inv.InventoryId == inventoryId)
                {
                    return inv;
                }
            }
            return null;
        }

        private GameKitItemAsset ResolveItemAsset(Dictionary<string, object> payload)
        {
            var itemId = GetString(payload, "itemId");
            if (!string.IsNullOrEmpty(itemId))
            {
                return FindItemAssetById(itemId);
            }

            var assetPath = GetString(payload, "assetPath");
            if (!string.IsNullOrEmpty(assetPath))
            {
                return AssetDatabase.LoadAssetAtPath<GameKitItemAsset>(assetPath);
            }

            return null;
        }

        private GameKitItemAsset FindItemAssetById(string itemId)
        {
            var guids = AssetDatabase.FindAssets("t:GameKitItemAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                // Force reimport to get fresh data from disk
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                var item = AssetDatabase.LoadAssetAtPath<GameKitItemAsset>(path);
                if (item != null && item.ItemId == itemId)
                {
                    return item;
                }
            }
            return null;
        }

        private void ConfigureItemAsset(GameKitItemAsset item, string itemId, Dictionary<string, object> data)
        {
            var displayName = GetStringFromDict(data, "displayName", itemId);
            var description = GetStringFromDict(data, "description", "");
            var categoryStr = GetStringFromDict(data, "category", "misc");
            var category = ParseItemCategory(categoryStr);

            item.Initialize(itemId, displayName, description, category);

            // Stacking
            if (data.TryGetValue("stackable", out var stackableObj))
            {
                var stackable = Convert.ToBoolean(stackableObj);
                var maxStack = data.TryGetValue("maxStack", out var maxStackObj) ? Convert.ToInt32(maxStackObj) : 99;
                item.SetStacking(stackable, maxStack);
            }

            // Configure extended properties
            ConfigureItemAssetExtended(item, data);
        }

        /// <summary>
        /// Configure extended item properties (prices, equipment, use actions).
        /// Used by both DefineItem and UpdateItem operations.
        /// </summary>
        private void ConfigureItemAssetExtended(GameKitItemAsset item, Dictionary<string, object> data)
        {
            // Prices
            if (data.TryGetValue("buyPrice", out var buyObj) || data.TryGetValue("sellPrice", out var sellObj))
            {
                var buyPrice = data.TryGetValue("buyPrice", out var bObj) ? Convert.ToInt32(bObj) : 0;
                var sellPrice = data.TryGetValue("sellPrice", out var sObj) ? Convert.ToInt32(sObj) : 0;
                item.SetPrices(buyPrice, sellPrice);
            }

            // Equipment
            if (data.TryGetValue("equippable", out var equipObj) && Convert.ToBoolean(equipObj))
            {
                var equipSlotStr = GetStringFromDict(data, "equipSlot", "none");
                var equipSlot = ParseEquipmentSlot(equipSlotStr);

                List<GameKitItemAsset.StatModifier> stats = null;
                if (data.TryGetValue("equipStats", out var statsObj) && statsObj is List<object> statsList)
                {
                    stats = new List<GameKitItemAsset.StatModifier>();
                    foreach (var statObj in statsList)
                    {
                        if (statObj is Dictionary<string, object> statDict)
                        {
                            var modifier = new GameKitItemAsset.StatModifier
                            {
                                statName = GetStringFromDict(statDict, "statName", ""),
                                modifierType = ParseModifierType(GetStringFromDict(statDict, "modifierType", "flat")),
                                value = statDict.TryGetValue("value", out var valObj) ? Convert.ToSingle(valObj) : 0
                            };
                            stats.Add(modifier);
                        }
                    }
                }

                item.SetEquipment(true, equipSlot, stats);
            }

            // Use action
            if (data.TryGetValue("onUse", out var useObj) && useObj is Dictionary<string, object> useData)
            {
                var useAction = new GameKitItemAsset.ItemUseAction();
                var useType = GetStringFromDict(useData, "type", "none");
                useAction.type = ParseUseActionType(useType);

                if (useData.TryGetValue("healthId", out var hIdObj))
                    useAction.healthId = hIdObj.ToString();
                if (useData.TryGetValue("amount", out var amountObj))
                    useAction.healAmount = Convert.ToSingle(amountObj);
                if (useData.TryGetValue("resourceManagerId", out var rmIdObj))
                    useAction.resourceManagerId = rmIdObj.ToString();
                if (useData.TryGetValue("resourceName", out var rnObj))
                    useAction.resourceName = rnObj.ToString();
                if (useData.TryGetValue("resourceAmount", out var raObj))
                    useAction.resourceAmount = Convert.ToSingle(raObj);
                if (useData.TryGetValue("effectId", out var efIdObj))
                    useAction.effectId = efIdObj.ToString();
                if (useData.TryGetValue("consumeOnUse", out var consumeObj))
                    useAction.consumeOnUse = Convert.ToBoolean(consumeObj);

                item.SetUseAction(useAction);
            }
        }

        /// <summary>
        /// Configure extended item properties using SerializedObject for on-disk persistence.
        /// Currently a placeholder - stacking is handled separately in DefineItem.
        /// </summary>
        private void ConfigureItemAssetSerializedExtended(SerializedObject so, Dictionary<string, object> data)
        {
            // Prices - using SerializedProperty for direct serialization
            if (data.TryGetValue("buyPrice", out var buyObj) || data.TryGetValue("sellPrice", out var sellObj))
            {
                var buyPriceProp = so.FindProperty("buyPrice");
                var sellPriceProp = so.FindProperty("sellPrice");

                if (buyPriceProp != null && data.TryGetValue("buyPrice", out var bObj))
                    buyPriceProp.intValue = Convert.ToInt32(bObj);
                if (sellPriceProp != null && data.TryGetValue("sellPrice", out var sObj))
                    sellPriceProp.intValue = Convert.ToInt32(sObj);
            }

            // Equipment - handled via direct properties after SerializedObject changes are applied
            // Note: Complex nested structures like equipStats are better handled after loading
        }

        private GameKitItemAsset.ItemCategory ParseItemCategory(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "weapon" => GameKitItemAsset.ItemCategory.Weapon,
                "armor" => GameKitItemAsset.ItemCategory.Armor,
                "consumable" => GameKitItemAsset.ItemCategory.Consumable,
                "material" => GameKitItemAsset.ItemCategory.Material,
                "key" => GameKitItemAsset.ItemCategory.Key,
                "quest" => GameKitItemAsset.ItemCategory.Quest,
                _ => GameKitItemAsset.ItemCategory.Misc
            };
        }

        private GameKitItemAsset.EquipmentSlot ParseEquipmentSlot(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "mainhand" => GameKitItemAsset.EquipmentSlot.MainHand,
                "offhand" => GameKitItemAsset.EquipmentSlot.OffHand,
                "head" => GameKitItemAsset.EquipmentSlot.Head,
                "body" => GameKitItemAsset.EquipmentSlot.Body,
                "hands" => GameKitItemAsset.EquipmentSlot.Hands,
                "feet" => GameKitItemAsset.EquipmentSlot.Feet,
                "accessory1" => GameKitItemAsset.EquipmentSlot.Accessory1,
                "accessory2" => GameKitItemAsset.EquipmentSlot.Accessory2,
                _ => GameKitItemAsset.EquipmentSlot.None
            };
        }

        private GameKitItemAsset.ModifierType ParseModifierType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "percentadd" => GameKitItemAsset.ModifierType.PercentAdd,
                "percentmultiply" => GameKitItemAsset.ModifierType.PercentMultiply,
                _ => GameKitItemAsset.ModifierType.Flat
            };
        }

        private GameKitItemAsset.UseActionType ParseUseActionType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "heal" => GameKitItemAsset.UseActionType.Heal,
                "addresource" => GameKitItemAsset.UseActionType.AddResource,
                "playeffect" => GameKitItemAsset.UseActionType.PlayEffect,
                "custom" => GameKitItemAsset.UseActionType.Custom,
                _ => GameKitItemAsset.UseActionType.None
            };
        }

        private string BuildGameObjectPath(GameObject go)
        {
            var path = go.name;
            var current = go.transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        private int GetInt(Dictionary<string, object> payload, string key, int defaultValue)
        {
            if (payload.TryGetValue(key, out var value))
            {
                return Convert.ToInt32(value);
            }
            return defaultValue;
        }

        private string GetStringFromDict(Dictionary<string, object> dict, string key, string defaultValue)
        {
            return dict.TryGetValue(key, out var value) ? value?.ToString() ?? defaultValue : defaultValue;
        }

        #endregion
    }
}
