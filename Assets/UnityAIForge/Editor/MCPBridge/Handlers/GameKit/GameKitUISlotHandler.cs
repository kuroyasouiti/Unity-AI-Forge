using System;
using System.Collections.Generic;
using System.Reflection;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit UI Slot handler: create and manage slot-based UI.
    /// Supports equipment slots, quickslots, and slot bars.
    /// Uses code generation to produce standalone UISlot/UISlotBar scripts with zero package dependency.
    /// </summary>
    public class GameKitUISlotHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "update", "inspect", "delete",
            "setItem", "clearSlot", "setHighlight",
            "createSlotBar", "updateSlotBar", "inspectSlotBar", "deleteSlotBar",
            "useSlot", "refreshFromInventory",
            "findBySlotId", "findByBarId"
        };

        public override string Category => "gamekitUISlot";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create" || operation == "createSlotBar";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateSlot(payload),
                "update" => UpdateSlot(payload),
                "inspect" => InspectSlot(payload),
                "delete" => DeleteSlot(payload),
                "setItem" => SetItem(payload),
                "clearSlot" => ClearSlot(payload),
                "setHighlight" => SetHighlight(payload),
                "createSlotBar" => CreateSlotBar(payload),
                "updateSlotBar" => UpdateSlotBar(payload),
                "inspectSlotBar" => InspectSlotBar(payload),
                "deleteSlotBar" => DeleteSlotBar(payload),
                "useSlot" => UseSlot(payload),
                "refreshFromInventory" => RefreshFromInventory(payload),
                "findBySlotId" => FindBySlotId(payload),
                "findByBarId" => FindByBarId(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit UI Slot operation: {operation}")
            };
        }

        #region Slot Operations

        private object CreateSlot(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            var parentPath = GetString(payload, "parentPath");
            var name = GetString(payload, "name");

            GameObject targetGo;
            bool createdNewUI = false;

            // If targetPath is provided, use existing GameObject
            if (!string.IsNullOrEmpty(targetPath))
            {
                targetGo = ResolveGameObject(targetPath);
                if (targetGo == null)
                {
                    throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
                }

                var existingSlot = CodeGenHelper.FindComponentByField(targetGo, "slotId", null);
                if (existingSlot != null)
                {
                    throw new InvalidOperationException($"GameObject '{targetPath}' already has a UISlot component.");
                }
            }
            // If parentPath is provided, create new UI GameObject
            else if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = ResolveGameObject(parentPath);
                if (parent == null)
                {
                    throw new InvalidOperationException($"Parent GameObject not found at path: {parentPath}");
                }

                var slotName = name ?? "UISlot";
                targetGo = CreateSlotUIGameObject(parent, slotName, payload);
                createdNewUI = true;
            }
            else
            {
                throw new InvalidOperationException("Either targetPath or parentPath is required for create operation.");
            }

            var slotId = GetString(payload, "slotId") ?? $"Slot_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var slotIndex = payload.TryGetValue("slotIndex", out var indexObj) ? Convert.ToInt32(indexObj) : -1;
            var slotType = ParseSlotType(GetString(payload, "slotType") ?? "storage");
            var equipSlotName = GetString(payload, "equipSlotName") ?? "";
            var inventoryId = GetString(payload, "inventoryId") ?? "";
            var dragDropEnabled = payload.TryGetValue("dragDropEnabled", out var dragObj) ? Convert.ToBoolean(dragObj) : true;

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(slotId, "UISlot");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "SLOT_ID", slotId },
                { "SLOT_INDEX", slotIndex },
                { "SLOT_TYPE", slotType },
                { "EQUIP_SLOT_NAME", equipSlotName },
                { "INVENTORY_ID", inventoryId },
                { "DRAG_DROP_ENABLED", dragDropEnabled }
            };

            // Build properties to set after component is added
            var propertiesToSet = new Dictionary<string, object>();

            if (payload.TryGetValue("acceptCategories", out var catObj) && catObj is List<object> catList)
            {
                // acceptCategories will be set manually after component creation
                // since it's an array type that needs special handling
            }

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "UISlot", slotId, className, variables, outputDir,
                propertiesToSet.Count > 0 ? propertiesToSet : null);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate UISlot script.");
            }

            // If component was added immediately, set UI references and array properties
            if (result.TryGetValue("componentAdded", out var added) && (bool)added)
            {
                var component = CodeGenHelper.FindComponentByField(targetGo, "slotId", slotId);
                if (component != null)
                {
                    var so = new SerializedObject(component);

                    // Auto-set UI references when creating new UI
                    if (createdNewUI)
                    {
                        SetSlotUIReferences(targetGo, so);
                    }

                    // Set acceptCategories array
                    if (catObj is List<object> categories)
                    {
                        var catProp = so.FindProperty("acceptCategories");
                        if (catProp != null)
                        {
                            catProp.ClearArray();
                            for (int i = 0; i < categories.Count; i++)
                            {
                                catProp.InsertArrayElementAtIndex(i);
                                catProp.GetArrayElementAtIndex(i).stringValue = categories[i].ToString();
                            }
                        }
                    }

                    so.ApplyModifiedProperties();
                }
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["slotId"] = slotId;
            result["path"] = BuildGameObjectPath(targetGo);

            return result;
        }

        private object UpdateSlot(Dictionary<string, object> payload)
        {
            var component = ResolveSlotComponent(payload);

            Undo.RecordObject(component, "Update UISlot");

            var so = new SerializedObject(component);

            if (payload.TryGetValue("slotType", out var typeObj))
            {
                var slotType = ParseSlotType(typeObj.ToString());
                var prop = so.FindProperty("slotType");
                if (prop != null)
                {
                    var names = prop.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], slotType, StringComparison.OrdinalIgnoreCase))
                        {
                            prop.enumValueIndex = i;
                            break;
                        }
                    }
                }
            }

            if (payload.TryGetValue("equipSlotName", out var equipObj))
                so.FindProperty("equipSlotName").stringValue = equipObj.ToString();

            if (payload.TryGetValue("dragDropEnabled", out var dragObj))
                so.FindProperty("dragDropEnabled").boolValue = Convert.ToBoolean(dragObj);

            if (payload.TryGetValue("acceptCategories", out var catObj) && catObj is List<object> catList)
            {
                var catProp = so.FindProperty("acceptCategories");
                if (catProp != null)
                {
                    catProp.ClearArray();
                    for (int i = 0; i < catList.Count; i++)
                    {
                        catProp.InsertArrayElementAtIndex(i);
                        catProp.GetArrayElementAtIndex(i).stringValue = catList[i].ToString();
                    }
                }
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var slotId = new SerializedObject(component).FindProperty("slotId").stringValue;

            return CreateSuccessResponse(
                ("slotId", slotId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        private object InspectSlot(Dictionary<string, object> payload)
        {
            var component = ResolveSlotComponent(payload);
            var so = new SerializedObject(component);

            var slotTypeProp = so.FindProperty("slotType");
            var slotTypeStr = slotTypeProp != null && slotTypeProp.enumValueIndex < slotTypeProp.enumDisplayNames.Length
                ? slotTypeProp.enumDisplayNames[slotTypeProp.enumValueIndex]
                : "Storage";

            // Check IsEmpty via reflection
            var isEmptyProp = component.GetType().GetProperty("IsEmpty",
                BindingFlags.Public | BindingFlags.Instance);
            var isEmpty = isEmptyProp != null ? (bool)isEmptyProp.GetValue(component) : true;

            var info = new Dictionary<string, object>
            {
                { "slotId", so.FindProperty("slotId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "slotIndex", so.FindProperty("slotIndex").intValue },
                { "slotType", slotTypeStr },
                { "equipSlotName", so.FindProperty("equipSlotName").stringValue },
                { "inventoryId", so.FindProperty("inventoryId").stringValue },
                { "isEmpty", isEmpty },
                { "dragDropEnabled", so.FindProperty("dragDropEnabled").boolValue }
            };

            // Try to get current item data via reflection
            if (!isEmpty)
            {
                var currentItemProp = component.GetType().GetProperty("CurrentItem",
                    BindingFlags.Public | BindingFlags.Instance);
                if (currentItemProp != null)
                {
                    var currentItem = currentItemProp.GetValue(component);
                    if (currentItem != null)
                    {
                        var itemType = currentItem.GetType();
                        var itemId = itemType.GetField("itemId")?.GetValue(currentItem)?.ToString() ?? "";
                        var itemName = itemType.GetField("itemName")?.GetValue(currentItem)?.ToString() ?? "";
                        var quantity = itemType.GetField("quantity")?.GetValue(currentItem);

                        info["currentItem"] = new Dictionary<string, object>
                        {
                            { "itemId", itemId },
                            { "itemName", itemName },
                            { "quantity", quantity ?? 1 }
                        };
                    }
                }
            }

            return CreateSuccessResponse(("slot", info));
        }

        private object DeleteSlot(Dictionary<string, object> payload)
        {
            var component = ResolveSlotComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var slotId = new SerializedObject(component).FindProperty("slotId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component);

            // Also clean up the generated script from tracker
            ScriptGenerator.Delete(slotId);

            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("slotId", slotId),
                ("path", path),
                ("deleted", true)
            );
        }

        private object SetItem(Dictionary<string, object> payload)
        {
            var component = ResolveSlotComponent(payload);

            if (!payload.TryGetValue("item", out var itemObj) || !(itemObj is Dictionary<string, object> itemDict))
            {
                throw new InvalidOperationException("item object is required for setItem.");
            }

            // Use reflection to call SetItem with a SlotData instance
            var compType = component.GetType();

            // Find the SlotData nested type
            var slotDataType = compType.GetNestedType("SlotData");
            if (slotDataType == null)
            {
                // Try to find a non-nested SlotData type in the same assembly
                slotDataType = compType.Assembly.GetType(compType.Namespace + ".SlotData")
                    ?? compType.Assembly.GetType("SlotData");
            }

            if (slotDataType != null)
            {
                var slotData = Activator.CreateInstance(slotDataType);

                var itemIdField = slotDataType.GetField("itemId");
                var itemNameField = slotDataType.GetField("itemName");
                var quantityField = slotDataType.GetField("quantity");
                var categoryField = slotDataType.GetField("category");
                var iconField = slotDataType.GetField("icon");

                if (itemIdField != null)
                    itemIdField.SetValue(slotData, itemDict.TryGetValue("itemId", out var idObj) ? idObj.ToString() : "");
                if (itemNameField != null)
                    itemNameField.SetValue(slotData, itemDict.TryGetValue("itemName", out var nameObj) ? nameObj.ToString() : "");
                if (quantityField != null)
                    quantityField.SetValue(slotData, itemDict.TryGetValue("quantity", out var qtyObj) ? Convert.ToInt32(qtyObj) : 1);
                if (categoryField != null)
                    categoryField.SetValue(slotData, itemDict.TryGetValue("category", out var catObj) ? catObj.ToString() : "");

                if (iconField != null && itemDict.TryGetValue("iconPath", out var iconObj))
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconObj.ToString());
                    if (sprite != null)
                        iconField.SetValue(slotData, sprite);
                }

                var setItemMethod = compType.GetMethod("SetItem", BindingFlags.Public | BindingFlags.Instance);
                if (setItemMethod != null)
                {
                    setItemMethod.Invoke(component, new[] { slotData });
                }
            }
            else
            {
                throw new InvalidOperationException("Could not find SlotData type for SetItem. The generated UISlot script may need to be compiled first.");
            }

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var so = new SerializedObject(component);
            return CreateSuccessResponse(
                ("slotId", so.FindProperty("slotId").stringValue),
                ("itemSet", true)
            );
        }

        private object ClearSlot(Dictionary<string, object> payload)
        {
            var component = ResolveSlotComponent(payload);

            var clearMethod = component.GetType().GetMethod("ClearSlot",
                BindingFlags.Public | BindingFlags.Instance);
            if (clearMethod != null)
            {
                clearMethod.Invoke(component, null);
            }

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var so = new SerializedObject(component);
            return CreateSuccessResponse(
                ("slotId", so.FindProperty("slotId").stringValue),
                ("cleared", true)
            );
        }

        private object SetHighlight(Dictionary<string, object> payload)
        {
            var component = ResolveSlotComponent(payload);
            var show = payload.TryGetValue("show", out var showObj) && Convert.ToBoolean(showObj);

            var setHighlightMethod = component.GetType().GetMethod("SetHighlight",
                BindingFlags.Public | BindingFlags.Instance);
            if (setHighlightMethod != null)
            {
                setHighlightMethod.Invoke(component, new object[] { show });
            }

            var so = new SerializedObject(component);
            return CreateSuccessResponse(
                ("slotId", so.FindProperty("slotId").stringValue),
                ("highlighted", show)
            );
        }

        #endregion

        #region Slot Bar Operations

        private object CreateSlotBar(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            var parentPath = GetString(payload, "parentPath");
            var name = GetString(payload, "name");

            GameObject targetGo;

            // If targetPath is provided, use existing GameObject
            if (!string.IsNullOrEmpty(targetPath))
            {
                targetGo = ResolveGameObject(targetPath);
                if (targetGo == null)
                {
                    throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
                }

                var existingBar = CodeGenHelper.FindComponentByField(targetGo, "barId", null);
                if (existingBar != null)
                {
                    throw new InvalidOperationException($"GameObject '{targetPath}' already has a UISlotBar component.");
                }
            }
            // If parentPath is provided, create new UI GameObject
            else if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = ResolveGameObject(parentPath);
                if (parent == null)
                {
                    throw new InvalidOperationException($"Parent GameObject not found at path: {parentPath}");
                }

                var barName = name ?? "UISlotBar";
                targetGo = CreateSlotBarUIGameObject(parent, barName, payload);
            }
            else
            {
                throw new InvalidOperationException("Either targetPath or parentPath is required for createSlotBar operation.");
            }

            var barId = GetString(payload, "barId") ?? $"Bar_{Guid.NewGuid().ToString().Substring(0, 8)}";
            int slotCount = payload.TryGetValue("slotCount", out var countObj) ? Convert.ToInt32(countObj) : 8;
            var layout = ParseBarLayoutType(GetString(payload, "layout") ?? "horizontal");
            var inventoryId = GetString(payload, "inventoryId") ?? "";

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(barId, "UISlotBar");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "BAR_ID", barId },
                { "SLOT_COUNT", slotCount },
                { "LAYOUT", layout },
                { "INVENTORY_ID", inventoryId }
            };

            // Build properties to set after component is added
            var propertiesToSet = new Dictionary<string, object>();

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "UISlotBar", barId, className, variables, outputDir,
                propertiesToSet.Count > 0 ? propertiesToSet : null);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate UISlotBar script.");
            }

            // If component was added immediately, set additional properties
            if (result.TryGetValue("componentAdded", out var added) && (bool)added)
            {
                var component = CodeGenHelper.FindComponentByField(targetGo, "barId", barId);
                if (component != null)
                {
                    var so = new SerializedObject(component);

                    // Set slotPrefab if provided
                    if (payload.TryGetValue("slotPrefabPath", out var prefabObj))
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabObj.ToString());
                        if (prefab != null)
                        {
                            var slotPrefabProp = so.FindProperty("slotPrefab");
                            if (slotPrefabProp != null)
                                slotPrefabProp.objectReferenceValue = prefab;
                        }
                    }

                    // Set keyBindings array
                    if (payload.TryGetValue("keyBindings", out var keysObj) && keysObj is List<object> keysList)
                    {
                        var keysProp = so.FindProperty("keyBindings");
                        if (keysProp != null)
                        {
                            keysProp.ClearArray();
                            for (int i = 0; i < keysList.Count; i++)
                            {
                                keysProp.InsertArrayElementAtIndex(i);
                                keysProp.GetArrayElementAtIndex(i).stringValue = keysList[i].ToString();
                            }
                        }
                    }

                    so.ApplyModifiedProperties();

                    // Call CreateSlots via reflection
                    var createSlotsMethod = component.GetType().GetMethod("CreateSlots",
                        BindingFlags.Public | BindingFlags.Instance);
                    if (createSlotsMethod != null)
                    {
                        createSlotsMethod.Invoke(component, null);
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["barId"] = barId;
            result["path"] = BuildGameObjectPath(targetGo);
            result["slotCount"] = slotCount;

            return result;
        }

        private object UpdateSlotBar(Dictionary<string, object> payload)
        {
            var component = ResolveSlotBarComponent(payload);

            Undo.RecordObject(component, "Update UISlotBar");

            var so = new SerializedObject(component);

            if (payload.TryGetValue("layout", out var layoutObj))
            {
                var layoutType = ParseBarLayoutType(layoutObj.ToString());
                var prop = so.FindProperty("layout");
                if (prop != null)
                {
                    var names = prop.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], layoutType, StringComparison.OrdinalIgnoreCase))
                        {
                            prop.enumValueIndex = i;
                            break;
                        }
                    }
                }
            }

            if (payload.TryGetValue("keyBindings", out var keysObj) && keysObj is List<object> keysList)
            {
                var keysProp = so.FindProperty("keyBindings");
                if (keysProp != null)
                {
                    keysProp.ClearArray();
                    for (int i = 0; i < keysList.Count; i++)
                    {
                        keysProp.InsertArrayElementAtIndex(i);
                        keysProp.GetArrayElementAtIndex(i).stringValue = keysList[i].ToString();
                    }
                }
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var barId = new SerializedObject(component).FindProperty("barId").stringValue;

            return CreateSuccessResponse(
                ("barId", barId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        private object InspectSlotBar(Dictionary<string, object> payload)
        {
            var component = ResolveSlotBarComponent(payload);
            var so = new SerializedObject(component);

            var barId = so.FindProperty("barId").stringValue;
            var slotCount = so.FindProperty("slotCount").intValue;

            // Get slots via reflection
            var slots = new List<Dictionary<string, object>>();
            var slotsProp = component.GetType().GetProperty("Slots",
                BindingFlags.Public | BindingFlags.Instance);
            if (slotsProp != null)
            {
                var slotsValue = slotsProp.GetValue(component) as System.Collections.IList;
                if (slotsValue != null)
                {
                    foreach (var slot in slotsValue)
                    {
                        if (slot == null) continue;
                        var slotComp = slot as Component;
                        if (slotComp == null) continue;

                        var slotSo = new SerializedObject(slotComp);
                        var slotIdProp = slotSo.FindProperty("slotId");
                        var slotIndexProp = slotSo.FindProperty("slotIndex");

                        var isEmptyProp = slot.GetType().GetProperty("IsEmpty",
                            BindingFlags.Public | BindingFlags.Instance);
                        var isEmpty = isEmptyProp != null ? (bool)isEmptyProp.GetValue(slot) : true;

                        slots.Add(new Dictionary<string, object>
                        {
                            { "slotId", slotIdProp?.stringValue ?? "" },
                            { "slotIndex", slotIndexProp?.intValue ?? -1 },
                            { "isEmpty", isEmpty }
                        });
                    }
                }
            }

            var info = new Dictionary<string, object>
            {
                { "barId", barId },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "slotCount", slotCount },
                { "slots", slots }
            };

            return CreateSuccessResponse(("slotBar", info));
        }

        private object DeleteSlotBar(Dictionary<string, object> payload)
        {
            var component = ResolveSlotBarComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var barId = new SerializedObject(component).FindProperty("barId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component);

            // Also clean up the generated script from tracker
            ScriptGenerator.Delete(barId);

            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("barId", barId),
                ("path", path),
                ("deleted", true)
            );
        }

        private object UseSlot(Dictionary<string, object> payload)
        {
            var component = ResolveSlotBarComponent(payload);

            if (!payload.TryGetValue("index", out var indexObj))
            {
                throw new InvalidOperationException("index is required for useSlot.");
            }

            int index = Convert.ToInt32(indexObj);

            var useSlotMethod = component.GetType().GetMethod("UseSlot",
                BindingFlags.Public | BindingFlags.Instance);
            if (useSlotMethod != null)
            {
                useSlotMethod.Invoke(component, new object[] { index });
            }

            var so = new SerializedObject(component);
            return CreateSuccessResponse(
                ("barId", so.FindProperty("barId").stringValue),
                ("usedIndex", index)
            );
        }

        private object RefreshFromInventory(Dictionary<string, object> payload)
        {
            var component = ResolveSlotBarComponent(payload);

            var refreshMethod = component.GetType().GetMethod("RefreshFromInventory",
                BindingFlags.Public | BindingFlags.Instance);
            if (refreshMethod != null)
            {
                refreshMethod.Invoke(component, null);
            }

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var so = new SerializedObject(component);
            return CreateSuccessResponse(
                ("barId", so.FindProperty("barId").stringValue),
                ("refreshed", true)
            );
        }

        #endregion

        #region Find Operations

        private object FindBySlotId(Dictionary<string, object> payload)
        {
            var slotId = GetString(payload, "slotId");
            if (string.IsNullOrEmpty(slotId))
            {
                throw new InvalidOperationException("slotId is required for findBySlotId.");
            }

            var component = CodeGenHelper.FindComponentInSceneByField("slotId", slotId);
            if (component == null)
            {
                return CreateSuccessResponse(("found", false), ("slotId", slotId));
            }

            var isEmptyProp = component.GetType().GetProperty("IsEmpty",
                BindingFlags.Public | BindingFlags.Instance);
            var isEmpty = isEmptyProp != null ? (bool)isEmptyProp.GetValue(component) : true;

            return CreateSuccessResponse(
                ("found", true),
                ("slotId", slotId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("isEmpty", isEmpty)
            );
        }

        private object FindByBarId(Dictionary<string, object> payload)
        {
            var barId = GetString(payload, "barId");
            if (string.IsNullOrEmpty(barId))
            {
                throw new InvalidOperationException("barId is required for findByBarId.");
            }

            var component = CodeGenHelper.FindComponentInSceneByField("barId", barId);
            if (component == null)
            {
                return CreateSuccessResponse(("found", false), ("barId", barId));
            }

            var so = new SerializedObject(component);

            return CreateSuccessResponse(
                ("found", true),
                ("barId", barId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("slotCount", so.FindProperty("slotCount").intValue)
            );
        }

        #endregion

        #region UI Creation Helpers

        private GameObject CreateSlotUIGameObject(GameObject parent, string name, Dictionary<string, object> payload)
        {
            // Create the slot GameObject
            var slotGo = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(slotGo, "Create UI Slot");
            slotGo.transform.SetParent(parent.transform, false);

            // Setup RectTransform
            var rectTransform = slotGo.GetComponent<RectTransform>();

            // Get size from payload or use defaults
            float size = 64f;
            if (payload.TryGetValue("size", out var sizeObj))
            {
                size = Convert.ToSingle(sizeObj);
            }
            float width = payload.TryGetValue("width", out var widthObj) ? Convert.ToSingle(widthObj) : size;
            float height = payload.TryGetValue("height", out var heightObj) ? Convert.ToSingle(heightObj) : size;

            rectTransform.sizeDelta = new Vector2(width, height);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;

            // Add Image component for background
            var image = Undo.AddComponent<Image>(slotGo);
            if (payload.TryGetValue("backgroundColor", out var bgColorObj) && bgColorObj is Dictionary<string, object> bgColorDict)
            {
                image.color = GetColorFromDict(bgColorDict, new Color(0.2f, 0.2f, 0.2f, 0.9f));
            }
            else
            {
                image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            }

            // Create icon child for item display
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(slotGo.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            var iconImage = iconGo.AddComponent<Image>();
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;

            // Create quantity text child
            var quantityGo = new GameObject("QuantityText", typeof(RectTransform));
            quantityGo.transform.SetParent(slotGo.transform, false);
            var quantityRect = quantityGo.GetComponent<RectTransform>();
            quantityRect.anchorMin = new Vector2(0.5f, 0f);
            quantityRect.anchorMax = new Vector2(1f, 0.3f);
            quantityRect.offsetMin = Vector2.zero;
            quantityRect.offsetMax = Vector2.zero;
            var quantityText = quantityGo.AddComponent<Text>();
            quantityText.text = "";
            quantityText.alignment = TextAnchor.LowerRight;
            quantityText.fontSize = 12;
            quantityText.color = Color.white;
            quantityText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Create highlight overlay (hidden by default)
            var highlightGo = new GameObject("Highlight", typeof(RectTransform));
            highlightGo.transform.SetParent(slotGo.transform, false);
            var highlightRect = highlightGo.GetComponent<RectTransform>();
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.offsetMin = Vector2.zero;
            highlightRect.offsetMax = Vector2.zero;
            var highlightImage = highlightGo.AddComponent<Image>();
            highlightImage.color = new Color(0.5f, 0.7f, 1f, 0.3f);
            highlightGo.SetActive(false);

            return slotGo;
        }

        private GameObject CreateSlotBarUIGameObject(GameObject parent, string name, Dictionary<string, object> payload)
        {
            // Create the slot bar container GameObject
            var barGo = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(barGo, "Create UI Slot Bar");
            barGo.transform.SetParent(parent.transform, false);

            // Setup RectTransform
            var rectTransform = barGo.GetComponent<RectTransform>();

            int slotCount = 8;
            if (payload.TryGetValue("slotCount", out var countObj))
            {
                slotCount = Convert.ToInt32(countObj);
            }

            float slotSize = 64f;
            if (payload.TryGetValue("slotSize", out var slotSizeObj))
            {
                slotSize = Convert.ToSingle(slotSizeObj);
            }

            float spacing = 5f;
            if (payload.TryGetValue("spacing", out var spacingObj))
            {
                spacing = Convert.ToSingle(spacingObj);
            }

            // Calculate size based on layout and slot count
            var layoutType = ParseBarLayoutType(GetString(payload, "layout") ?? "horizontal");

            float width, height;
            switch (layoutType.ToLowerInvariant())
            {
                case "horizontal":
                    width = slotCount * slotSize + (slotCount - 1) * spacing + 20;
                    height = slotSize + 20;
                    break;
                case "vertical":
                    width = slotSize + 20;
                    height = slotCount * slotSize + (slotCount - 1) * spacing + 20;
                    break;
                default: // Grid - assume 4 columns
                    int columns = 4;
                    int rows = (slotCount + columns - 1) / columns;
                    width = columns * slotSize + (columns - 1) * spacing + 20;
                    height = rows * slotSize + (rows - 1) * spacing + 20;
                    break;
            }

            rectTransform.sizeDelta = new Vector2(width, height);
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0, 20);

            // Add Image component for background
            var image = Undo.AddComponent<Image>(barGo);
            if (payload.TryGetValue("backgroundColor", out var bgColorObj) && bgColorObj is Dictionary<string, object> bgColorDict)
            {
                image.color = GetColorFromDict(bgColorDict, new Color(0.1f, 0.1f, 0.1f, 0.8f));
            }
            else
            {
                image.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            }

            // Add LayoutGroup based on layout type
            switch (layoutType.ToLowerInvariant())
            {
                case "horizontal":
                    var hlg = Undo.AddComponent<HorizontalLayoutGroup>(barGo);
                    hlg.spacing = spacing;
                    hlg.padding = new RectOffset(10, 10, 10, 10);
                    hlg.childAlignment = TextAnchor.MiddleCenter;
                    hlg.childControlWidth = false;
                    hlg.childControlHeight = false;
                    hlg.childForceExpandWidth = false;
                    hlg.childForceExpandHeight = false;
                    break;

                case "vertical":
                    var vlg = Undo.AddComponent<VerticalLayoutGroup>(barGo);
                    vlg.spacing = spacing;
                    vlg.padding = new RectOffset(10, 10, 10, 10);
                    vlg.childAlignment = TextAnchor.UpperCenter;
                    vlg.childControlWidth = false;
                    vlg.childControlHeight = false;
                    vlg.childForceExpandWidth = false;
                    vlg.childForceExpandHeight = false;
                    break;

                default: // Grid
                    var glg = Undo.AddComponent<GridLayoutGroup>(barGo);
                    glg.cellSize = new Vector2(slotSize, slotSize);
                    glg.spacing = new Vector2(spacing, spacing);
                    glg.padding = new RectOffset(10, 10, 10, 10);
                    glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    glg.constraintCount = 4;
                    break;
            }

            return barGo;
        }

        #endregion

        #region Helpers

        private void SetSlotUIReferences(GameObject targetGo, SerializedObject so)
        {
            // Background image is on the slot itself
            var bgImage = targetGo.GetComponent<Image>();
            if (bgImage != null)
            {
                var bgProp = so.FindProperty("backgroundImage");
                if (bgProp != null)
                    bgProp.objectReferenceValue = bgImage;
            }

            // Find child elements by name
            var iconTransform = targetGo.transform.Find("Icon");
            if (iconTransform != null)
            {
                var iconImage = iconTransform.GetComponent<Image>();
                if (iconImage != null)
                {
                    var iconProp = so.FindProperty("iconImage");
                    if (iconProp != null)
                        iconProp.objectReferenceValue = iconImage;
                }
            }

            var quantityTransform = targetGo.transform.Find("QuantityText");
            if (quantityTransform != null)
            {
                var quantityText = quantityTransform.GetComponent<Text>();
                if (quantityText != null)
                {
                    var quantityProp = so.FindProperty("quantityText");
                    if (quantityProp != null)
                        quantityProp.objectReferenceValue = quantityText;
                }
            }

            var highlightTransform = targetGo.transform.Find("Highlight");
            if (highlightTransform != null)
            {
                var highlightImage = highlightTransform.GetComponent<Image>();
                if (highlightImage != null)
                {
                    var highlightProp = so.FindProperty("highlightImage");
                    if (highlightProp != null)
                        highlightProp.objectReferenceValue = highlightImage;
                }
            }
        }

        private Component ResolveSlotComponent(Dictionary<string, object> payload)
        {
            // Try by slotId first
            var slotId = GetString(payload, "slotId");
            if (!string.IsNullOrEmpty(slotId))
            {
                var slotById = CodeGenHelper.FindComponentInSceneByField("slotId", slotId);
                if (slotById != null)
                {
                    return slotById;
                }
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var slotByPath = CodeGenHelper.FindComponentByField(targetGo, "slotId", null);
                    if (slotByPath != null)
                    {
                        return slotByPath;
                    }
                    throw new InvalidOperationException($"No UISlot component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either slotId or targetPath is required.");
        }

        private Component ResolveSlotBarComponent(Dictionary<string, object> payload)
        {
            // Try by barId first
            var barId = GetString(payload, "barId");
            if (!string.IsNullOrEmpty(barId))
            {
                var barById = CodeGenHelper.FindComponentInSceneByField("barId", barId);
                if (barById != null)
                {
                    return barById;
                }
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var barByPath = CodeGenHelper.FindComponentByField(targetGo, "barId", null);
                    if (barByPath != null)
                    {
                        return barByPath;
                    }
                    throw new InvalidOperationException($"No UISlotBar component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either barId or targetPath is required.");
        }

        private string ParseSlotType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "storage" => "Storage",
                "equipment" => "Equipment",
                "quickslot" => "Quickslot",
                "trash" => "Trash",
                _ => "Storage"
            };
        }

        private string ParseBarLayoutType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "horizontal" => "Horizontal",
                "vertical" => "Vertical",
                "grid" => "Grid",
                _ => "Horizontal"
            };
        }

        #endregion
    }
}
