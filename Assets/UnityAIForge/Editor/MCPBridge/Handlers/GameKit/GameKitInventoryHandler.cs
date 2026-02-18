using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Inventory handler: create and manage inventory systems.
    /// Uses code generation to produce standalone Inventory and ItemData scripts with zero package dependency.
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

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create" || operation == "defineItem";

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

            // Check if already has an inventory component (by checking for inventoryId field)
            var existingInventory = CodeGenHelper.FindComponentByField(targetGo, "inventoryId", null);
            if (existingInventory != null)
            {
                throw new InvalidOperationException($"GameObject '{targetPath}' already has an Inventory component.");
            }

            var inventoryId = GetString(payload, "inventoryId") ?? $"Inventory_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var maxSlots = GetInt(payload, "maxSlots", 20);
            var maxStackSize = GetInt(payload, "maxStackSize", 99);

            // Parse categories
            var categoriesStr = "";
            if (payload.TryGetValue("categories", out var catObj) && catObj is List<object> catList)
            {
                var quoted = new List<string>();
                foreach (var c in catList)
                    quoted.Add($"\"{c.ToString().ToLowerInvariant()}\"");
                categoriesStr = string.Join(", ", quoted);
            }

            // Parse stackable categories
            var stackableCategoriesStr = "";
            if (payload.TryGetValue("stackableCategories", out var stackObj) && stackObj is List<object> stackList)
            {
                var quoted = new List<string>();
                foreach (var s in stackList)
                    quoted.Add($"\"{s.ToString().ToLowerInvariant()}\"");
                stackableCategoriesStr = string.Join(", ", quoted);
            }

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(inventoryId, "Inventory");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "INVENTORY_ID", inventoryId },
                { "MAX_SLOTS", maxSlots },
                { "DEFAULT_MAX_STACK", maxStackSize }
            };

            // Build properties to set after component is added (for list fields)
            var propertiesToSet = new Dictionary<string, object>();

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "Inventory", inventoryId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate Inventory script.");
            }

            // If component was added, set list-type properties via SerializedObject
            if (result.TryGetValue("componentAdded", out var added) && (bool)added)
            {
                var component = CodeGenHelper.FindComponentByField(targetGo, "inventoryId", inventoryId);
                if (component != null)
                {
                    var so = new SerializedObject(component);

                    if (payload.TryGetValue("categories", out var catObj2) && catObj2 is List<object> catList2)
                    {
                        var prop = so.FindProperty("allowedCategories");
                        if (prop != null)
                        {
                            prop.ClearArray();
                            foreach (var cat in catList2)
                            {
                                prop.InsertArrayElementAtIndex(prop.arraySize);
                                prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = cat.ToString().ToLowerInvariant();
                            }
                        }
                    }

                    if (payload.TryGetValue("stackableCategories", out var stackObj2) && stackObj2 is List<object> stackList2)
                    {
                        var prop = so.FindProperty("stackableCategories");
                        if (prop != null)
                        {
                            prop.ClearArray();
                            foreach (var stack in stackList2)
                            {
                                prop.InsertArrayElementAtIndex(prop.arraySize);
                                prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = stack.ToString().ToLowerInvariant();
                            }
                        }
                    }

                    so.ApplyModifiedProperties();
                }
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["inventoryId"] = inventoryId;
            result["path"] = BuildGameObjectPath(targetGo);
            result["maxSlots"] = maxSlots;

            return result;
        }

        private object UpdateInventory(Dictionary<string, object> payload)
        {
            var component = ResolveInventory(payload);

            Undo.RecordObject(component, "Update Inventory");

            var so = new SerializedObject(component);

            if (payload.TryGetValue("maxSlots", out var slotsObj))
            {
                so.FindProperty("maxSlots").intValue = Convert.ToInt32(slotsObj);
            }

            if (payload.TryGetValue("categories", out var catObj) && catObj is List<object> catList)
            {
                var prop = so.FindProperty("allowedCategories");
                if (prop != null)
                {
                    prop.ClearArray();
                    foreach (var cat in catList)
                    {
                        prop.InsertArrayElementAtIndex(prop.arraySize);
                        prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = cat.ToString().ToLowerInvariant();
                    }
                }
            }

            if (payload.TryGetValue("stackableCategories", out var stackObj) && stackObj is List<object> stackList)
            {
                var prop = so.FindProperty("stackableCategories");
                if (prop != null)
                {
                    prop.ClearArray();
                    foreach (var stack in stackList)
                    {
                        prop.InsertArrayElementAtIndex(prop.arraySize);
                        prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = stack.ToString().ToLowerInvariant();
                    }
                }
            }

            if (payload.TryGetValue("maxStackSize", out var maxStackObj))
            {
                so.FindProperty("defaultMaxStack").intValue = Convert.ToInt32(maxStackObj);
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var inventoryId = new SerializedObject(component).FindProperty("inventoryId").stringValue;

            return CreateSuccessResponse(
                ("inventoryId", inventoryId),
                ("updated", true)
            );
        }

        private object InspectInventory(Dictionary<string, object> payload)
        {
            var component = ResolveInventory(payload);
            var so = new SerializedObject(component);

            var inventoryId = so.FindProperty("inventoryId").stringValue;
            var maxSlots = so.FindProperty("maxSlots").intValue;

            // Read slots via SerializedObject
            var slots = new List<object>();
            var slotsProp = so.FindProperty("slots");
            if (slotsProp != null)
            {
                for (int i = 0; i < slotsProp.arraySize; i++)
                {
                    var slot = slotsProp.GetArrayElementAtIndex(i);
                    var itemId = slot.FindPropertyRelative("itemId").stringValue;
                    if (!string.IsNullOrEmpty(itemId))
                    {
                        slots.Add(new Dictionary<string, object>
                        {
                            { "slotIndex", i },
                            { "itemId", itemId },
                            { "quantity", slot.FindPropertyRelative("quantity").intValue },
                            { "displayName", slot.FindPropertyRelative("displayName").stringValue }
                        });
                    }
                }
            }

            // Read equipped items via SerializedObject
            var equipped = new List<object>();
            var equippedProp = so.FindProperty("equippedItems");
            if (equippedProp != null)
            {
                for (int i = 0; i < equippedProp.arraySize; i++)
                {
                    var item = equippedProp.GetArrayElementAtIndex(i);
                    equipped.Add(new Dictionary<string, object>
                    {
                        { "equipSlot", item.FindPropertyRelative("equipSlot").stringValue },
                        { "itemId", item.FindPropertyRelative("itemId").stringValue },
                        { "displayName", item.FindPropertyRelative("displayName").stringValue }
                    });
                }
            }

            var usedSlots = slots.Count;

            return CreateSuccessResponse(
                ("inventoryId", inventoryId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("maxSlots", maxSlots),
                ("usedSlots", usedSlots),
                ("freeSlots", maxSlots - usedSlots),
                ("isFull", usedSlots >= maxSlots),
                ("slots", slots),
                ("equipped", equipped)
            );
        }

        private object DeleteInventory(Dictionary<string, object> payload)
        {
            var component = ResolveInventory(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var inventoryId = new SerializedObject(component).FindProperty("inventoryId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component);

            // Clean up generated script from tracker
            ScriptGenerator.Delete(inventoryId);

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

            // Check for existing asset at the specified path
            var existingAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (existingAsset != null)
            {
                throw new InvalidOperationException($"Item asset already exists at: {assetPath}");
            }

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

            // Stacking
            bool stackable = false;
            int maxStack = 1;
            if (itemData != null && itemData.TryGetValue("stackable", out var stackableObj))
            {
                stackable = Convert.ToBoolean(stackableObj);
                maxStack = itemData.TryGetValue("maxStack", out var maxStackObj) ? Convert.ToInt32(maxStackObj) : 99;
            }

            // Prices
            int buyPrice = 0;
            int sellPrice = 0;
            if (itemData != null)
            {
                buyPrice = itemData.TryGetValue("buyPrice", out var buyObj) ? Convert.ToInt32(buyObj) : 0;
                sellPrice = itemData.TryGetValue("sellPrice", out var sellObj) ? Convert.ToInt32(sellObj) : 0;
            }

            // Equipment
            bool equippable = false;
            string equipSlot = "None";
            if (itemData != null && itemData.TryGetValue("equippable", out var equipObj) && Convert.ToBoolean(equipObj))
            {
                equippable = true;
                equipSlot = ParseEquipmentSlot(GetStringFromDict(itemData, "equipSlot", "none"));
            }

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(itemId, "ItemData");

            // Build template variables for ItemData
            var variables = new Dictionary<string, object>
            {
                { "ITEM_ID", itemId },
                { "DISPLAY_NAME", displayName },
                { "DESCRIPTION", description },
                { "CATEGORY", category },
                { "STACKABLE", stackable.ToString().ToLowerInvariant() },
                { "MAX_STACK", maxStack },
                { "EQUIPPABLE", equippable.ToString().ToLowerInvariant() },
                { "EQUIP_SLOT", equipSlot },
                { "BUY_PRICE", buyPrice },
                { "SELL_PRICE", sellPrice }
            };

            var outputDir = GetString(payload, "outputPath");

            // Generate the ItemData script (ScriptableObject - no GameObject target)
            var genResult = ScriptGenerator.Generate(null, "ItemData", className, itemId, variables, outputDir);
            if (!genResult.Success)
            {
                throw new InvalidOperationException(genResult.ErrorMessage ?? "Failed to generate ItemData script.");
            }

            // Try to create the asset if the type is already compiled
            var itemType = ScriptGenerator.ResolveGeneratedType(className);
            if (itemType != null)
            {
                // Delete any existing assets with the same itemId
                DeleteExistingItemAssets(itemId);

                var item = ScriptableObject.CreateInstance(itemType);

                // Set fields via SerializedObject
                var serializedObject = new SerializedObject(item);
                serializedObject.FindProperty("itemId").stringValue = itemId;
                serializedObject.FindProperty("displayName").stringValue = displayName;
                serializedObject.FindProperty("description").stringValue = description;

                // Set category enum
                var categoryProp = serializedObject.FindProperty("category");
                if (categoryProp != null)
                {
                    var names = categoryProp.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], category, StringComparison.OrdinalIgnoreCase))
                        {
                            categoryProp.enumValueIndex = i;
                            break;
                        }
                    }
                }

                serializedObject.FindProperty("stackable").boolValue = stackable;
                serializedObject.FindProperty("maxStack").intValue = maxStack;
                serializedObject.FindProperty("buyPrice").intValue = buyPrice;
                serializedObject.FindProperty("sellPrice").intValue = sellPrice;
                serializedObject.FindProperty("equippable").boolValue = equippable;

                // Set equipSlot enum
                var equipSlotProp = serializedObject.FindProperty("equipSlot");
                if (equipSlotProp != null)
                {
                    var names = equipSlotProp.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], equipSlot, StringComparison.OrdinalIgnoreCase))
                        {
                            equipSlotProp.enumValueIndex = i;
                            break;
                        }
                    }
                }

                // Equipment stats
                if (equippable && itemData != null && itemData.TryGetValue("equipStats", out var statsObj) && statsObj is List<object> statsList)
                {
                    var statsProp = serializedObject.FindProperty("equipStats");
                    if (statsProp != null)
                    {
                        statsProp.ClearArray();
                        foreach (var statObj in statsList)
                        {
                            if (statObj is Dictionary<string, object> statDict)
                            {
                                statsProp.InsertArrayElementAtIndex(statsProp.arraySize);
                                var elem = statsProp.GetArrayElementAtIndex(statsProp.arraySize - 1);
                                elem.FindPropertyRelative("statName").stringValue = GetStringFromDict(statDict, "statName", "");

                                var modTypeProp = elem.FindPropertyRelative("modifierType");
                                if (modTypeProp != null)
                                {
                                    var modType = ParseModifierType(GetStringFromDict(statDict, "modifierType", "flat"));
                                    var modNames = modTypeProp.enumDisplayNames;
                                    for (int i = 0; i < modNames.Length; i++)
                                    {
                                        if (string.Equals(modNames[i], modType, StringComparison.OrdinalIgnoreCase))
                                        {
                                            modTypeProp.enumValueIndex = i;
                                            break;
                                        }
                                    }
                                }

                                elem.FindPropertyRelative("value").floatValue =
                                    statDict.TryGetValue("value", out var valObj) ? Convert.ToSingle(valObj) : 0;
                            }
                        }
                    }
                }

                // Use action
                if (itemData != null && itemData.TryGetValue("onUse", out var useObj) && useObj is Dictionary<string, object> useData)
                {
                    var onUseProp = serializedObject.FindProperty("onUse");
                    if (onUseProp != null)
                    {
                        if (useData.TryGetValue("type", out var typeObj))
                        {
                            var useType = ParseUseActionType(typeObj.ToString());
                            var typeProp = onUseProp.FindPropertyRelative("type");
                            if (typeProp != null)
                            {
                                var typeNames = typeProp.enumDisplayNames;
                                for (int i = 0; i < typeNames.Length; i++)
                                {
                                    if (string.Equals(typeNames[i], useType, StringComparison.OrdinalIgnoreCase))
                                    {
                                        typeProp.enumValueIndex = i;
                                        break;
                                    }
                                }
                            }
                        }
                        if (useData.TryGetValue("amount", out var amountObj))
                        {
                            var amountProp = onUseProp.FindPropertyRelative("amount");
                            if (amountProp != null)
                                amountProp.floatValue = Convert.ToSingle(amountObj);
                        }
                        if (useData.TryGetValue("resourceName", out var rnObj))
                        {
                            var rnProp = onUseProp.FindPropertyRelative("resourceName");
                            if (rnProp != null)
                                rnProp.stringValue = rnObj.ToString();
                        }
                        if (useData.TryGetValue("effectId", out var efIdObj))
                        {
                            var efProp = onUseProp.FindPropertyRelative("effectId");
                            if (efProp != null)
                                efProp.stringValue = efIdObj.ToString();
                        }
                        if (useData.TryGetValue("customEventName", out var ceObj))
                        {
                            var ceProp = onUseProp.FindPropertyRelative("customEventName");
                            if (ceProp != null)
                                ceProp.stringValue = ceObj.ToString();
                        }
                        if (useData.TryGetValue("consumeOnUse", out var consumeObj))
                        {
                            var consumeProp = onUseProp.FindPropertyRelative("consumeOnUse");
                            if (consumeProp != null)
                                consumeProp.boolValue = Convert.ToBoolean(consumeObj);
                        }
                    }
                }

                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                // Create the asset on disk
                AssetDatabase.CreateAsset(item, assetPath);
                EditorUtility.SetDirty(item);
                AssetDatabase.SaveAssets();

                // Force reserialize to ensure binary data is written to disk
                AssetDatabase.ForceReserializeAssets(
                    new[] { assetPath },
                    ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata
                );

                AssetDatabase.ReleaseCachedFileHandles();
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

                // Reload the asset fresh from disk
                var savedItem = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                var savedSo = savedItem != null ? new SerializedObject(savedItem) : null;

                return CreateSuccessResponse(
                    ("itemId", itemId),
                    ("assetPath", assetPath),
                    ("scriptPath", genResult.ScriptPath),
                    ("className", genResult.ClassName),
                    ("displayName", savedSo?.FindProperty("displayName")?.stringValue ?? displayName),
                    ("stackable", savedSo?.FindProperty("stackable")?.boolValue ?? stackable),
                    ("maxStack", savedSo?.FindProperty("maxStack")?.intValue ?? maxStack),
                    ("compilationRequired", false)
                );
            }

            // Type not yet compiled -- asset creation will happen after recompilation
            return CreateSuccessResponse(
                ("itemId", itemId),
                ("assetPath", assetPath),
                ("scriptPath", genResult.ScriptPath),
                ("className", genResult.ClassName),
                ("displayName", displayName),
                ("stackable", stackable),
                ("maxStack", maxStack),
                ("compilationRequired", true),
                ("note", "ItemData script generated. Create the asset after Unity recompiles.")
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
                ConfigureItemAssetSerialized(item, itemData);
            }

            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssets();

            var itemIdValue = new SerializedObject(item).FindProperty("itemId").stringValue;

            return CreateSuccessResponse(
                ("itemId", itemIdValue),
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

            var so = new SerializedObject(item);

            var equipStats = new List<object>();
            var statsProp = so.FindProperty("equipStats");
            if (statsProp != null)
            {
                for (int i = 0; i < statsProp.arraySize; i++)
                {
                    var elem = statsProp.GetArrayElementAtIndex(i);
                    var modTypeProp = elem.FindPropertyRelative("modifierType");
                    var modTypeStr = modTypeProp.enumValueIndex < modTypeProp.enumDisplayNames.Length
                        ? modTypeProp.enumDisplayNames[modTypeProp.enumValueIndex]
                        : "Flat";

                    equipStats.Add(new Dictionary<string, object>
                    {
                        { "statName", elem.FindPropertyRelative("statName").stringValue },
                        { "modifierType", modTypeStr },
                        { "value", elem.FindPropertyRelative("value").floatValue }
                    });
                }
            }

            var categoryProp = so.FindProperty("category");
            var categoryStr = categoryProp.enumValueIndex < categoryProp.enumDisplayNames.Length
                ? categoryProp.enumDisplayNames[categoryProp.enumValueIndex]
                : "Misc";

            var equipSlotProp = so.FindProperty("equipSlot");
            var equipSlotStr = equipSlotProp.enumValueIndex < equipSlotProp.enumDisplayNames.Length
                ? equipSlotProp.enumDisplayNames[equipSlotProp.enumValueIndex]
                : "None";

            return CreateSuccessResponse(
                ("itemId", so.FindProperty("itemId").stringValue),
                ("assetPath", AssetDatabase.GetAssetPath(item)),
                ("displayName", so.FindProperty("displayName").stringValue),
                ("description", so.FindProperty("description").stringValue),
                ("category", categoryStr),
                ("stackable", so.FindProperty("stackable").boolValue),
                ("maxStack", so.FindProperty("maxStack").intValue),
                ("equippable", so.FindProperty("equippable").boolValue),
                ("equipSlot", equipSlotStr),
                ("equipStats", equipStats),
                ("buyPrice", so.FindProperty("buyPrice").intValue),
                ("sellPrice", so.FindProperty("sellPrice").intValue)
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
            var itemId = new SerializedObject(item).FindProperty("itemId").stringValue;

            AssetDatabase.DeleteAsset(assetPath);

            // Clean up generated script from tracker
            ScriptGenerator.Delete(itemId);

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
            var component = ResolveInventory(payload);

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

            // Read item data from the ScriptableObject asset
            var itemSo = new SerializedObject(item);
            var displayName = itemSo.FindProperty("displayName").stringValue;
            var category = "";
            var categoryProp = itemSo.FindProperty("category");
            if (categoryProp != null)
            {
                category = categoryProp.enumValueIndex < categoryProp.enumDisplayNames.Length
                    ? categoryProp.enumDisplayNames[categoryProp.enumValueIndex].ToLowerInvariant()
                    : "misc";
            }
            var stackable = itemSo.FindProperty("stackable").boolValue;
            var maxStack = itemSo.FindProperty("maxStack").intValue;

            // Try to call AddItem via reflection on the inventory component
            Undo.RecordObject(component, "Add Item");

            var addMethod = component.GetType().GetMethod("AddItem",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(string), typeof(string), typeof(int), typeof(bool), typeof(int), typeof(string) },
                null);

            if (addMethod != null)
            {
                var added = (bool)addMethod.Invoke(component, new object[] { itemId, displayName, category, quantity, stackable, maxStack, null });
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

                var invSo = new SerializedObject(component);
                var inventoryId = invSo.FindProperty("inventoryId").stringValue;

                return CreateSuccessResponse(
                    ("inventoryId", inventoryId),
                    ("itemId", itemId),
                    ("requested", quantity),
                    ("added", added ? quantity : 0),
                    ("remaining", added ? 0 : quantity)
                );
            }

            // Fallback: directly manipulate serialized slots
            return AddItemViaSerializedObject(component, itemId, displayName, category, quantity, stackable, maxStack);
        }

        private object AddItemViaSerializedObject(Component component, string itemId, string displayName,
            string category, int quantity, bool stackable, int maxStack)
        {
            var so = new SerializedObject(component);
            var slotsProp = so.FindProperty("slots");
            var maxSlots = so.FindProperty("maxSlots").intValue;
            var defaultMaxStack = so.FindProperty("defaultMaxStack").intValue;
            var inventoryId = so.FindProperty("inventoryId").stringValue;

            int ms = maxStack > 0 ? maxStack : defaultMaxStack;
            int remaining = quantity;

            // Ensure we have enough slot entries
            while (slotsProp.arraySize < maxSlots)
            {
                slotsProp.InsertArrayElementAtIndex(slotsProp.arraySize);
                var newSlot = slotsProp.GetArrayElementAtIndex(slotsProp.arraySize - 1);
                newSlot.FindPropertyRelative("itemId").stringValue = "";
                newSlot.FindPropertyRelative("quantity").intValue = 0;
            }

            // Try to stack into existing slots
            if (stackable)
            {
                for (int i = 0; i < slotsProp.arraySize && remaining > 0; i++)
                {
                    var slot = slotsProp.GetArrayElementAtIndex(i);
                    var slotItemId = slot.FindPropertyRelative("itemId").stringValue;
                    var slotQty = slot.FindPropertyRelative("quantity").intValue;
                    if (slotItemId == itemId && slotQty < ms)
                    {
                        int canAdd = Mathf.Min(remaining, ms - slotQty);
                        slot.FindPropertyRelative("quantity").intValue = slotQty + canAdd;
                        remaining -= canAdd;
                    }
                }
            }

            // Fill empty slots
            while (remaining > 0)
            {
                int emptyIdx = -1;
                for (int i = 0; i < slotsProp.arraySize; i++)
                {
                    var slot = slotsProp.GetArrayElementAtIndex(i);
                    var slotItemId = slot.FindPropertyRelative("itemId").stringValue;
                    if (string.IsNullOrEmpty(slotItemId))
                    {
                        emptyIdx = i;
                        break;
                    }
                }

                if (emptyIdx < 0) break; // inventory full

                var emptySlot = slotsProp.GetArrayElementAtIndex(emptyIdx);
                int toAdd = stackable ? Mathf.Min(remaining, ms) : 1;
                emptySlot.FindPropertyRelative("itemId").stringValue = itemId;
                emptySlot.FindPropertyRelative("displayName").stringValue = displayName;
                emptySlot.FindPropertyRelative("category").stringValue = category;
                emptySlot.FindPropertyRelative("quantity").intValue = toAdd;
                emptySlot.FindPropertyRelative("stackable").boolValue = stackable;
                emptySlot.FindPropertyRelative("maxStack").intValue = ms;
                remaining -= toAdd;
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            int added = quantity - remaining;

            return CreateSuccessResponse(
                ("inventoryId", inventoryId),
                ("itemId", itemId),
                ("requested", quantity),
                ("added", added),
                ("remaining", remaining)
            );
        }

        private object RemoveItem(Dictionary<string, object> payload)
        {
            var component = ResolveInventory(payload);

            var itemId = GetString(payload, "itemId");
            if (string.IsNullOrEmpty(itemId))
            {
                throw new InvalidOperationException("itemId is required to remove an item.");
            }

            var quantity = GetInt(payload, "quantity", 1);

            Undo.RecordObject(component, "Remove Item");

            // Try via reflection
            var removeMethod = component.GetType().GetMethod("RemoveItem",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(int) },
                null);

            if (removeMethod != null)
            {
                var removed = (bool)removeMethod.Invoke(component, new object[] { itemId, quantity });
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

                var invSo = new SerializedObject(component);
                var inventoryId = invSo.FindProperty("inventoryId").stringValue;

                return CreateSuccessResponse(
                    ("inventoryId", inventoryId),
                    ("itemId", itemId),
                    ("requested", quantity),
                    ("removed", removed ? quantity : 0)
                );
            }

            // Fallback: remove via SerializedObject
            return RemoveItemViaSerializedObject(component, itemId, quantity);
        }

        private object RemoveItemViaSerializedObject(Component component, string itemId, int quantity)
        {
            var so = new SerializedObject(component);
            var slotsProp = so.FindProperty("slots");
            var inventoryId = so.FindProperty("inventoryId").stringValue;
            int remaining = quantity;

            for (int i = slotsProp.arraySize - 1; i >= 0 && remaining > 0; i--)
            {
                var slot = slotsProp.GetArrayElementAtIndex(i);
                if (slot.FindPropertyRelative("itemId").stringValue != itemId) continue;

                int slotQty = slot.FindPropertyRelative("quantity").intValue;
                int toRemove = Mathf.Min(remaining, slotQty);
                slot.FindPropertyRelative("quantity").intValue = slotQty - toRemove;
                remaining -= toRemove;

                if (slotQty - toRemove <= 0)
                {
                    ClearSlotProperties(slot);
                }
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("inventoryId", inventoryId),
                ("itemId", itemId),
                ("requested", quantity),
                ("removed", quantity - remaining)
            );
        }

        private object UseItem(Dictionary<string, object> payload)
        {
            var component = ResolveInventory(payload);

            var slotIndex = GetInt(payload, "slotIndex", -1);
            if (slotIndex < 0)
            {
                throw new InvalidOperationException("slotIndex is required to use an item.");
            }

            var so = new SerializedObject(component);
            var slotsProp = so.FindProperty("slots");

            if (slotIndex >= slotsProp.arraySize)
            {
                return CreateSuccessResponse(
                    ("used", false),
                    ("reason", "Slot index out of range")
                );
            }

            var slot = slotsProp.GetArrayElementAtIndex(slotIndex);
            var slotItemId = slot.FindPropertyRelative("itemId").stringValue;

            if (string.IsNullOrEmpty(slotItemId))
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
                ("itemId", slotItemId)
            );
        }

        private object Equip(Dictionary<string, object> payload)
        {
            var component = ResolveInventory(payload);

            var slotIndex = GetInt(payload, "slotIndex", -1);
            if (slotIndex < 0)
            {
                throw new InvalidOperationException("slotIndex is required to equip an item.");
            }

            var equipSlot = GetString(payload, "equipSlot");

            Undo.RecordObject(component, "Equip Item");

            // Try via reflection
            var equipMethod = component.GetType().GetMethod("Equip",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(int), typeof(string) },
                null);

            if (equipMethod != null)
            {
                var success = (bool)equipMethod.Invoke(component, new object[] { slotIndex, equipSlot });
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

                var invSo = new SerializedObject(component);
                return CreateSuccessResponse(
                    ("inventoryId", invSo.FindProperty("inventoryId").stringValue),
                    ("equipped", success),
                    ("slotIndex", slotIndex),
                    ("equipSlot", equipSlot ?? "default")
                );
            }

            // Fallback: equip via SerializedObject
            return EquipViaSerializedObject(component, slotIndex, equipSlot);
        }

        private object EquipViaSerializedObject(Component component, int slotIndex, string equipSlot)
        {
            var so = new SerializedObject(component);
            var slotsProp = so.FindProperty("slots");
            var equippedProp = so.FindProperty("equippedItems");
            var inventoryId = so.FindProperty("inventoryId").stringValue;

            if (slotIndex >= slotsProp.arraySize)
            {
                return CreateSuccessResponse(
                    ("inventoryId", inventoryId),
                    ("equipped", false),
                    ("reason", "Slot index out of range")
                );
            }

            var slot = slotsProp.GetArrayElementAtIndex(slotIndex);
            var slotItemId = slot.FindPropertyRelative("itemId").stringValue;
            if (string.IsNullOrEmpty(slotItemId))
            {
                return CreateSuccessResponse(
                    ("inventoryId", inventoryId),
                    ("equipped", false),
                    ("reason", "Slot is empty")
                );
            }

            // Unequip any existing item in that slot
            if (equippedProp != null && !string.IsNullOrEmpty(equipSlot))
            {
                for (int i = equippedProp.arraySize - 1; i >= 0; i--)
                {
                    var existing = equippedProp.GetArrayElementAtIndex(i);
                    if (existing.FindPropertyRelative("equipSlot").stringValue == equipSlot)
                    {
                        equippedProp.DeleteArrayElementAtIndex(i);
                    }
                }
            }

            // Add new equipped entry
            if (equippedProp != null)
            {
                equippedProp.InsertArrayElementAtIndex(equippedProp.arraySize);
                var newEntry = equippedProp.GetArrayElementAtIndex(equippedProp.arraySize - 1);
                newEntry.FindPropertyRelative("equipSlot").stringValue = equipSlot ?? "default";
                newEntry.FindPropertyRelative("itemId").stringValue = slotItemId;
                newEntry.FindPropertyRelative("displayName").stringValue =
                    slot.FindPropertyRelative("displayName").stringValue;
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("inventoryId", inventoryId),
                ("equipped", true),
                ("slotIndex", slotIndex),
                ("equipSlot", equipSlot ?? "default")
            );
        }

        private object Unequip(Dictionary<string, object> payload)
        {
            var component = ResolveInventory(payload);

            var equipSlot = GetString(payload, "equipSlot");
            if (string.IsNullOrEmpty(equipSlot))
            {
                throw new InvalidOperationException("equipSlot is required to unequip an item.");
            }

            Undo.RecordObject(component, "Unequip Item");

            // Try via reflection
            var unequipMethod = component.GetType().GetMethod("Unequip",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(string) },
                null);

            if (unequipMethod != null)
            {
                var success = (bool)unequipMethod.Invoke(component, new object[] { equipSlot });
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

                var invSo = new SerializedObject(component);
                return CreateSuccessResponse(
                    ("inventoryId", invSo.FindProperty("inventoryId").stringValue),
                    ("unequipped", success),
                    ("equipSlot", equipSlot)
                );
            }

            // Fallback: unequip via SerializedObject
            var so = new SerializedObject(component);
            var equippedProp = so.FindProperty("equippedItems");
            var inventoryId = so.FindProperty("inventoryId").stringValue;
            bool found = false;

            if (equippedProp != null)
            {
                for (int i = equippedProp.arraySize - 1; i >= 0; i--)
                {
                    var entry = equippedProp.GetArrayElementAtIndex(i);
                    if (entry.FindPropertyRelative("equipSlot").stringValue == equipSlot)
                    {
                        equippedProp.DeleteArrayElementAtIndex(i);
                        found = true;
                    }
                }
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("inventoryId", inventoryId),
                ("unequipped", found),
                ("equipSlot", equipSlot)
            );
        }

        private object GetEquipped(Dictionary<string, object> payload)
        {
            var component = ResolveInventory(payload);
            var so = new SerializedObject(component);
            var inventoryId = so.FindProperty("inventoryId").stringValue;
            var equippedProp = so.FindProperty("equippedItems");

            var equipSlot = GetString(payload, "equipSlot");
            if (!string.IsNullOrEmpty(equipSlot) && equippedProp != null)
            {
                for (int i = 0; i < equippedProp.arraySize; i++)
                {
                    var entry = equippedProp.GetArrayElementAtIndex(i);
                    if (entry.FindPropertyRelative("equipSlot").stringValue == equipSlot)
                    {
                        return CreateSuccessResponse(
                            ("equipSlot", equipSlot),
                            ("equipped", true),
                            ("itemId", entry.FindPropertyRelative("itemId").stringValue),
                            ("displayName", entry.FindPropertyRelative("displayName").stringValue)
                        );
                    }
                }

                return CreateSuccessResponse(
                    ("equipSlot", equipSlot),
                    ("equipped", false)
                );
            }

            // Return all equipped
            var all = new List<object>();
            if (equippedProp != null)
            {
                for (int i = 0; i < equippedProp.arraySize; i++)
                {
                    var entry = equippedProp.GetArrayElementAtIndex(i);
                    all.Add(new Dictionary<string, object>
                    {
                        { "equipSlot", entry.FindPropertyRelative("equipSlot").stringValue },
                        { "itemId", entry.FindPropertyRelative("itemId").stringValue },
                        { "displayName", entry.FindPropertyRelative("displayName").stringValue }
                    });
                }
            }

            return CreateSuccessResponse(
                ("inventoryId", inventoryId),
                ("equippedItems", all)
            );
        }

        private object ClearInventory(Dictionary<string, object> payload)
        {
            var component = ResolveInventory(payload);

            Undo.RecordObject(component, "Clear Inventory");

            // Try via reflection
            var clearMethod = component.GetType().GetMethod("Clear",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            if (clearMethod != null)
            {
                clearMethod.Invoke(component, null);
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

                var invSo = new SerializedObject(component);
                return CreateSuccessResponse(
                    ("inventoryId", invSo.FindProperty("inventoryId").stringValue),
                    ("cleared", true)
                );
            }

            // Fallback: clear via SerializedObject
            var so = new SerializedObject(component);
            var slotsProp = so.FindProperty("slots");
            if (slotsProp != null)
            {
                for (int i = 0; i < slotsProp.arraySize; i++)
                {
                    ClearSlotProperties(slotsProp.GetArrayElementAtIndex(i));
                }
            }

            var equippedProp = so.FindProperty("equippedItems");
            if (equippedProp != null)
            {
                equippedProp.ClearArray();
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var inventoryId = so.FindProperty("inventoryId").stringValue;

            return CreateSuccessResponse(
                ("inventoryId", inventoryId),
                ("cleared", true)
            );
        }

        private object SortInventory(Dictionary<string, object> payload)
        {
            var component = ResolveInventory(payload);

            Undo.RecordObject(component, "Sort Inventory");

            // Try via reflection
            var sortMethod = component.GetType().GetMethod("Sort",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            if (sortMethod != null)
            {
                sortMethod.Invoke(component, null);
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

                var invSo = new SerializedObject(component);
                return CreateSuccessResponse(
                    ("inventoryId", invSo.FindProperty("inventoryId").stringValue),
                    ("sorted", true)
                );
            }

            // Fallback note: sorting via SerializedObject is complex; recommend using reflection
            var so = new SerializedObject(component);
            var inventoryId = so.FindProperty("inventoryId").stringValue;

            return CreateSuccessResponse(
                ("inventoryId", inventoryId),
                ("sorted", false),
                ("note", "Sort requires the generated component to be compiled. Try again after compilation.")
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

            var component = CodeGenHelper.FindComponentInSceneByField("inventoryId", inventoryId);
            if (component == null)
            {
                return CreateSuccessResponse(("found", false), ("inventoryId", inventoryId));
            }

            var so = new SerializedObject(component);
            var maxSlots = so.FindProperty("maxSlots").intValue;

            // Count used slots
            int usedSlots = 0;
            var slotsProp = so.FindProperty("slots");
            if (slotsProp != null)
            {
                for (int i = 0; i < slotsProp.arraySize; i++)
                {
                    var slot = slotsProp.GetArrayElementAtIndex(i);
                    if (!string.IsNullOrEmpty(slot.FindPropertyRelative("itemId").stringValue))
                        usedSlots++;
                }
            }

            return CreateSuccessResponse(
                ("found", true),
                ("inventoryId", inventoryId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("usedSlots", usedSlots),
                ("maxSlots", maxSlots)
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

            var so = new SerializedObject(item);

            return CreateSuccessResponse(
                ("found", true),
                ("itemId", so.FindProperty("itemId").stringValue),
                ("assetPath", AssetDatabase.GetAssetPath(item)),
                ("displayName", so.FindProperty("displayName").stringValue)
            );
        }

        #endregion

        #region Helpers

        private Component ResolveInventory(Dictionary<string, object> payload)
        {
            // Try by inventoryId first
            var inventoryId = GetString(payload, "inventoryId");
            if (!string.IsNullOrEmpty(inventoryId))
            {
                var invById = CodeGenHelper.FindComponentInSceneByField("inventoryId", inventoryId);
                if (invById != null)
                {
                    return invById;
                }
            }

            // Try by gameObjectPath
            var targetPath = GetString(payload, "gameObjectPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var invByPath = CodeGenHelper.FindComponentByField(targetGo, "inventoryId", null);
                    if (invByPath != null)
                    {
                        return invByPath;
                    }

                    throw new InvalidOperationException($"No Inventory component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either inventoryId or gameObjectPath is required.");
        }

        private ScriptableObject ResolveItemAsset(Dictionary<string, object> payload)
        {
            var itemId = GetString(payload, "itemId");
            if (!string.IsNullOrEmpty(itemId))
            {
                return FindItemAssetById(itemId);
            }

            var assetPath = GetString(payload, "assetPath");
            if (!string.IsNullOrEmpty(assetPath))
            {
                return AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            }

            return null;
        }

        private ScriptableObject FindItemAssetById(string itemId)
        {
            // Search all ScriptableObject assets that have an "itemId" field
            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null) continue;

                try
                {
                    var so = new SerializedObject(asset);
                    var itemIdProp = so.FindProperty("itemId");
                    if (itemIdProp != null && itemIdProp.propertyType == SerializedPropertyType.String
                        && itemIdProp.stringValue == itemId)
                    {
                        return asset;
                    }
                }
                catch
                {
                    // Skip assets that can't be serialized
                }
            }
            return null;
        }

        private void DeleteExistingItemAssets(string itemId)
        {
            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null) continue;

                try
                {
                    var so = new SerializedObject(asset);
                    var itemIdProp = so.FindProperty("itemId");
                    if (itemIdProp != null && itemIdProp.propertyType == SerializedPropertyType.String
                        && itemIdProp.stringValue == itemId)
                    {
                        AssetDatabase.DeleteAsset(path);
                    }
                }
                catch
                {
                    // Skip assets that can't be serialized
                }
            }
        }

        private void ConfigureItemAssetSerialized(ScriptableObject item, Dictionary<string, object> data)
        {
            var so = new SerializedObject(item);

            if (data.TryGetValue("displayName", out var nameObj))
                so.FindProperty("displayName").stringValue = nameObj.ToString();

            if (data.TryGetValue("description", out var descObj))
                so.FindProperty("description").stringValue = descObj.ToString();

            if (data.TryGetValue("category", out var catObj))
            {
                var catStr = ParseItemCategory(catObj.ToString());
                var catProp = so.FindProperty("category");
                if (catProp != null)
                {
                    var names = catProp.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], catStr, StringComparison.OrdinalIgnoreCase))
                        {
                            catProp.enumValueIndex = i;
                            break;
                        }
                    }
                }
            }

            if (data.TryGetValue("stackable", out var stackableObj))
                so.FindProperty("stackable").boolValue = Convert.ToBoolean(stackableObj);

            if (data.TryGetValue("maxStack", out var maxStackObj))
                so.FindProperty("maxStack").intValue = Convert.ToInt32(maxStackObj);

            if (data.TryGetValue("buyPrice", out var buyObj))
                so.FindProperty("buyPrice").intValue = Convert.ToInt32(buyObj);

            if (data.TryGetValue("sellPrice", out var sellObj))
                so.FindProperty("sellPrice").intValue = Convert.ToInt32(sellObj);

            if (data.TryGetValue("equippable", out var equipObj))
                so.FindProperty("equippable").boolValue = Convert.ToBoolean(equipObj);

            if (data.TryGetValue("equipSlot", out var slotObj))
            {
                var slotStr = ParseEquipmentSlot(slotObj.ToString());
                var slotProp = so.FindProperty("equipSlot");
                if (slotProp != null)
                {
                    var names = slotProp.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], slotStr, StringComparison.OrdinalIgnoreCase))
                        {
                            slotProp.enumValueIndex = i;
                            break;
                        }
                    }
                }
            }

            // Equipment stats
            if (data.TryGetValue("equipStats", out var statsObj) && statsObj is List<object> statsList)
            {
                var statsProp = so.FindProperty("equipStats");
                if (statsProp != null)
                {
                    statsProp.ClearArray();
                    foreach (var statObj in statsList)
                    {
                        if (statObj is Dictionary<string, object> statDict)
                        {
                            statsProp.InsertArrayElementAtIndex(statsProp.arraySize);
                            var elem = statsProp.GetArrayElementAtIndex(statsProp.arraySize - 1);
                            elem.FindPropertyRelative("statName").stringValue = GetStringFromDict(statDict, "statName", "");

                            var modTypeProp = elem.FindPropertyRelative("modifierType");
                            if (modTypeProp != null)
                            {
                                var modType = ParseModifierType(GetStringFromDict(statDict, "modifierType", "flat"));
                                var modNames = modTypeProp.enumDisplayNames;
                                for (int i = 0; i < modNames.Length; i++)
                                {
                                    if (string.Equals(modNames[i], modType, StringComparison.OrdinalIgnoreCase))
                                    {
                                        modTypeProp.enumValueIndex = i;
                                        break;
                                    }
                                }
                            }

                            elem.FindPropertyRelative("value").floatValue =
                                statDict.TryGetValue("value", out var valObj) ? Convert.ToSingle(valObj) : 0;
                        }
                    }
                }
            }

            // Use action
            if (data.TryGetValue("onUse", out var useObj) && useObj is Dictionary<string, object> useData)
            {
                var onUseProp = so.FindProperty("onUse");
                if (onUseProp != null)
                {
                    if (useData.TryGetValue("type", out var typeObj))
                    {
                        var useType = ParseUseActionType(typeObj.ToString());
                        var typeProp = onUseProp.FindPropertyRelative("type");
                        if (typeProp != null)
                        {
                            var typeNames = typeProp.enumDisplayNames;
                            for (int i = 0; i < typeNames.Length; i++)
                            {
                                if (string.Equals(typeNames[i], useType, StringComparison.OrdinalIgnoreCase))
                                {
                                    typeProp.enumValueIndex = i;
                                    break;
                                }
                            }
                        }
                    }
                    if (useData.TryGetValue("amount", out var amountObj))
                    {
                        var amountProp = onUseProp.FindPropertyRelative("amount");
                        if (amountProp != null)
                            amountProp.floatValue = Convert.ToSingle(amountObj);
                    }
                    if (useData.TryGetValue("resourceName", out var rnObj))
                    {
                        var rnProp = onUseProp.FindPropertyRelative("resourceName");
                        if (rnProp != null)
                            rnProp.stringValue = rnObj.ToString();
                    }
                    if (useData.TryGetValue("effectId", out var efIdObj))
                    {
                        var efProp = onUseProp.FindPropertyRelative("effectId");
                        if (efProp != null)
                            efProp.stringValue = efIdObj.ToString();
                    }
                    if (useData.TryGetValue("customEventName", out var ceObj))
                    {
                        var ceProp = onUseProp.FindPropertyRelative("customEventName");
                        if (ceProp != null)
                            ceProp.stringValue = ceObj.ToString();
                    }
                    if (useData.TryGetValue("consumeOnUse", out var consumeObj))
                    {
                        var consumeProp = onUseProp.FindPropertyRelative("consumeOnUse");
                        if (consumeProp != null)
                            consumeProp.boolValue = Convert.ToBoolean(consumeObj);
                    }
                }
            }

            so.ApplyModifiedProperties();
        }

        private void ClearSlotProperties(SerializedProperty slot)
        {
            slot.FindPropertyRelative("itemId").stringValue = "";
            slot.FindPropertyRelative("displayName").stringValue = "";
            slot.FindPropertyRelative("category").stringValue = "";
            slot.FindPropertyRelative("quantity").intValue = 0;
            slot.FindPropertyRelative("stackable").boolValue = false;
            slot.FindPropertyRelative("maxStack").intValue = 0;
            var customDataProp = slot.FindPropertyRelative("customData");
            if (customDataProp != null)
                customDataProp.stringValue = "";
        }

        private string ParseItemCategory(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "weapon" => "Weapon",
                "armor" => "Armor",
                "consumable" => "Consumable",
                "material" => "Material",
                "key" => "Key",
                "quest" => "Quest",
                _ => "Misc"
            };
        }

        private string ParseEquipmentSlot(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "mainhand" => "MainHand",
                "offhand" => "OffHand",
                "head" => "Head",
                "body" => "Body",
                "hands" => "Hands",
                "feet" => "Feet",
                "accessory1" => "Accessory1",
                "accessory2" => "Accessory2",
                _ => "None"
            };
        }

        private string ParseModifierType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "percentadd" => "PercentAdd",
                "percentmultiply" => "PercentMultiply",
                _ => "Flat"
            };
        }

        private string ParseUseActionType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "heal" => "Heal",
                "addresource" => "AddResource",
                "playeffect" => "PlayEffect",
                "custom" => "Custom",
                _ => "None"
            };
        }

        private string GetStringFromDict(Dictionary<string, object> dict, string key, string defaultValue)
        {
            return dict.TryGetValue(key, out var value) ? value?.ToString() ?? defaultValue : defaultValue;
        }

        #endregion
    }
}
