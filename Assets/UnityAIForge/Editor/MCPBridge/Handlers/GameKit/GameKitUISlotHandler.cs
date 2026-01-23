using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using UnityAIForge.GameKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit UI Slot handler: create and manage slot-based UI.
    /// Supports equipment slots, quickslots, and slot bars.
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

        protected override bool RequiresCompilationWait(string operation) => false;

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

                var existingSlot = targetGo.GetComponent<GameKitUISlot>();
                if (existingSlot != null)
                {
                    throw new InvalidOperationException($"GameObject '{targetPath}' already has a GameKitUISlot component.");
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

            var slot = Undo.AddComponent<GameKitUISlot>(targetGo);
            var serializedSlot = new SerializedObject(slot);

            serializedSlot.FindProperty("slotId").stringValue = slotId;

            // Auto-set UI references when creating new UI
            if (createdNewUI)
            {
                // Background image is on the slot itself
                var bgImage = targetGo.GetComponent<Image>();
                if (bgImage != null)
                {
                    serializedSlot.FindProperty("backgroundImage").objectReferenceValue = bgImage;
                }

                // Find child elements by name
                var iconTransform = targetGo.transform.Find("Icon");
                if (iconTransform != null)
                {
                    var iconImage = iconTransform.GetComponent<Image>();
                    if (iconImage != null)
                    {
                        serializedSlot.FindProperty("iconImage").objectReferenceValue = iconImage;
                    }
                }

                var quantityTransform = targetGo.transform.Find("QuantityText");
                if (quantityTransform != null)
                {
                    var quantityText = quantityTransform.GetComponent<Text>();
                    if (quantityText != null)
                    {
                        serializedSlot.FindProperty("quantityText").objectReferenceValue = quantityText;
                    }
                }

                var highlightTransform = targetGo.transform.Find("Highlight");
                if (highlightTransform != null)
                {
                    var highlightImage = highlightTransform.GetComponent<Image>();
                    if (highlightImage != null)
                    {
                        serializedSlot.FindProperty("highlightImage").objectReferenceValue = highlightImage;
                    }
                }
            }

            if (payload.TryGetValue("slotIndex", out var indexObj))
            {
                serializedSlot.FindProperty("slotIndex").intValue = Convert.ToInt32(indexObj);
            }

            if (payload.TryGetValue("slotType", out var typeObj))
            {
                var slotType = ParseSlotType(typeObj.ToString());
                serializedSlot.FindProperty("slotType").enumValueIndex = (int)slotType;
            }

            if (payload.TryGetValue("equipSlotName", out var equipObj))
            {
                serializedSlot.FindProperty("equipSlotName").stringValue = equipObj.ToString();
            }

            if (payload.TryGetValue("inventoryId", out var invObj))
            {
                serializedSlot.FindProperty("inventoryId").stringValue = invObj.ToString();
            }

            if (payload.TryGetValue("dragDropEnabled", out var dragObj))
            {
                serializedSlot.FindProperty("dragDropEnabled").boolValue = Convert.ToBoolean(dragObj);
            }

            if (payload.TryGetValue("acceptCategories", out var catObj) && catObj is List<object> catList)
            {
                var catProp = serializedSlot.FindProperty("acceptCategories");
                catProp.ClearArray();
                for (int i = 0; i < catList.Count; i++)
                {
                    catProp.InsertArrayElementAtIndex(i);
                    catProp.GetArrayElementAtIndex(i).stringValue = catList[i].ToString();
                }
            }

            serializedSlot.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("slotId", slotId),
                ("path", BuildGameObjectPath(targetGo))
            );
        }

        private object UpdateSlot(Dictionary<string, object> payload)
        {
            var slot = ResolveSlotComponent(payload);

            Undo.RecordObject(slot, "Update GameKit UI Slot");

            var serializedSlot = new SerializedObject(slot);

            if (payload.TryGetValue("slotType", out var typeObj))
            {
                var slotType = ParseSlotType(typeObj.ToString());
                serializedSlot.FindProperty("slotType").enumValueIndex = (int)slotType;
            }

            if (payload.TryGetValue("equipSlotName", out var equipObj))
            {
                serializedSlot.FindProperty("equipSlotName").stringValue = equipObj.ToString();
            }

            if (payload.TryGetValue("dragDropEnabled", out var dragObj))
            {
                serializedSlot.FindProperty("dragDropEnabled").boolValue = Convert.ToBoolean(dragObj);
            }

            serializedSlot.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(slot.gameObject.scene);

            return CreateSuccessResponse(
                ("slotId", slot.SlotId),
                ("path", BuildGameObjectPath(slot.gameObject)),
                ("updated", true)
            );
        }

        private object InspectSlot(Dictionary<string, object> payload)
        {
            var slot = ResolveSlotComponent(payload);
            var serializedSlot = new SerializedObject(slot);

            var info = new Dictionary<string, object>
            {
                { "slotId", slot.SlotId },
                { "path", BuildGameObjectPath(slot.gameObject) },
                { "slotIndex", slot.SlotIndex },
                { "slotType", slot.Type.ToString() },
                { "equipSlotName", slot.EquipSlotName },
                { "inventoryId", slot.InventoryId },
                { "isEmpty", slot.IsEmpty },
                { "dragDropEnabled", slot.DragDropEnabled }
            };

            if (!slot.IsEmpty && slot.CurrentItem != null)
            {
                info["currentItem"] = new Dictionary<string, object>
                {
                    { "itemId", slot.CurrentItem.itemId },
                    { "itemName", slot.CurrentItem.itemName },
                    { "quantity", slot.CurrentItem.quantity }
                };
            }

            return CreateSuccessResponse(("slot", info));
        }

        private object DeleteSlot(Dictionary<string, object> payload)
        {
            var slot = ResolveSlotComponent(payload);
            var path = BuildGameObjectPath(slot.gameObject);
            var slotId = slot.SlotId;
            var scene = slot.gameObject.scene;

            Undo.DestroyObjectImmediate(slot);
            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("slotId", slotId),
                ("path", path),
                ("deleted", true)
            );
        }

        private object SetItem(Dictionary<string, object> payload)
        {
            var slot = ResolveSlotComponent(payload);

            if (!payload.TryGetValue("item", out var itemObj) || !(itemObj is Dictionary<string, object> itemDict))
            {
                throw new InvalidOperationException("item object is required for setItem.");
            }

            var slotData = new GameKitUISlot.SlotData
            {
                itemId = itemDict.TryGetValue("itemId", out var idObj) ? idObj.ToString() : "",
                itemName = itemDict.TryGetValue("itemName", out var nameObj) ? nameObj.ToString() : "",
                quantity = itemDict.TryGetValue("quantity", out var qtyObj) ? Convert.ToInt32(qtyObj) : 1,
                category = itemDict.TryGetValue("category", out var catObj) ? catObj.ToString() : ""
            };

            if (itemDict.TryGetValue("iconPath", out var iconObj))
            {
                slotData.icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconObj.ToString());
            }

            slot.SetItem(slotData);
            EditorSceneManager.MarkSceneDirty(slot.gameObject.scene);

            return CreateSuccessResponse(
                ("slotId", slot.SlotId),
                ("itemSet", true)
            );
        }

        private object ClearSlot(Dictionary<string, object> payload)
        {
            var slot = ResolveSlotComponent(payload);
            slot.ClearSlot();
            EditorSceneManager.MarkSceneDirty(slot.gameObject.scene);

            return CreateSuccessResponse(
                ("slotId", slot.SlotId),
                ("cleared", true)
            );
        }

        private object SetHighlight(Dictionary<string, object> payload)
        {
            var slot = ResolveSlotComponent(payload);
            var show = payload.TryGetValue("show", out var showObj) && Convert.ToBoolean(showObj);
            slot.SetHighlight(show);

            return CreateSuccessResponse(
                ("slotId", slot.SlotId),
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

                var existingBar = targetGo.GetComponent<GameKitUISlotBar>();
                if (existingBar != null)
                {
                    throw new InvalidOperationException($"GameObject '{targetPath}' already has a GameKitUISlotBar component.");
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

            var bar = Undo.AddComponent<GameKitUISlotBar>(targetGo);
            var serializedBar = new SerializedObject(bar);

            serializedBar.FindProperty("barId").stringValue = barId;

            int slotCount = 8;
            if (payload.TryGetValue("slotCount", out var countObj))
            {
                slotCount = Convert.ToInt32(countObj);
                serializedBar.FindProperty("slotCount").intValue = slotCount;
            }

            if (payload.TryGetValue("layout", out var layoutObj))
            {
                var layoutType = ParseBarLayoutType(layoutObj.ToString());
                serializedBar.FindProperty("layout").enumValueIndex = (int)layoutType;
            }

            if (payload.TryGetValue("inventoryId", out var invObj))
            {
                serializedBar.FindProperty("inventoryId").stringValue = invObj.ToString();
            }

            if (payload.TryGetValue("slotPrefabPath", out var prefabObj))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabObj.ToString());
                if (prefab != null)
                {
                    serializedBar.FindProperty("slotPrefab").objectReferenceValue = prefab;
                }
            }

            if (payload.TryGetValue("keyBindings", out var keysObj) && keysObj is List<object> keysList)
            {
                var keysProp = serializedBar.FindProperty("keyBindings");
                keysProp.ClearArray();
                for (int i = 0; i < keysList.Count; i++)
                {
                    keysProp.InsertArrayElementAtIndex(i);
                    keysProp.GetArrayElementAtIndex(i).stringValue = keysList[i].ToString();
                }
            }

            serializedBar.ApplyModifiedProperties();

            // Create slots
            bar.CreateSlots();

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("barId", barId),
                ("path", BuildGameObjectPath(targetGo)),
                ("slotCount", slotCount)
            );
        }

        private object UpdateSlotBar(Dictionary<string, object> payload)
        {
            var bar = ResolveSlotBarComponent(payload);

            Undo.RecordObject(bar, "Update GameKit UI Slot Bar");

            var serializedBar = new SerializedObject(bar);

            if (payload.TryGetValue("layout", out var layoutObj))
            {
                var layoutType = ParseBarLayoutType(layoutObj.ToString());
                serializedBar.FindProperty("layout").enumValueIndex = (int)layoutType;
            }

            if (payload.TryGetValue("keyBindings", out var keysObj) && keysObj is List<object> keysList)
            {
                var keysProp = serializedBar.FindProperty("keyBindings");
                keysProp.ClearArray();
                for (int i = 0; i < keysList.Count; i++)
                {
                    keysProp.InsertArrayElementAtIndex(i);
                    keysProp.GetArrayElementAtIndex(i).stringValue = keysList[i].ToString();
                }
            }

            serializedBar.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(bar.gameObject.scene);

            return CreateSuccessResponse(
                ("barId", bar.BarId),
                ("path", BuildGameObjectPath(bar.gameObject)),
                ("updated", true)
            );
        }

        private object InspectSlotBar(Dictionary<string, object> payload)
        {
            var bar = ResolveSlotBarComponent(payload);

            var slots = new List<Dictionary<string, object>>();
            foreach (var slot in bar.Slots)
            {
                slots.Add(new Dictionary<string, object>
                {
                    { "slotId", slot.SlotId },
                    { "slotIndex", slot.SlotIndex },
                    { "isEmpty", slot.IsEmpty }
                });
            }

            var info = new Dictionary<string, object>
            {
                { "barId", bar.BarId },
                { "path", BuildGameObjectPath(bar.gameObject) },
                { "slotCount", bar.SlotCount },
                { "slots", slots }
            };

            return CreateSuccessResponse(("slotBar", info));
        }

        private object DeleteSlotBar(Dictionary<string, object> payload)
        {
            var bar = ResolveSlotBarComponent(payload);
            var path = BuildGameObjectPath(bar.gameObject);
            var barId = bar.BarId;
            var scene = bar.gameObject.scene;

            Undo.DestroyObjectImmediate(bar);
            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("barId", barId),
                ("path", path),
                ("deleted", true)
            );
        }

        private object UseSlot(Dictionary<string, object> payload)
        {
            var bar = ResolveSlotBarComponent(payload);

            if (!payload.TryGetValue("index", out var indexObj))
            {
                throw new InvalidOperationException("index is required for useSlot.");
            }

            int index = Convert.ToInt32(indexObj);
            bar.UseSlot(index);

            return CreateSuccessResponse(
                ("barId", bar.BarId),
                ("usedIndex", index)
            );
        }

        private object RefreshFromInventory(Dictionary<string, object> payload)
        {
            var bar = ResolveSlotBarComponent(payload);
            bar.RefreshFromInventory();
            EditorSceneManager.MarkSceneDirty(bar.gameObject.scene);

            return CreateSuccessResponse(
                ("barId", bar.BarId),
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

            var slot = GameKitUISlot.FindById(slotId);
            if (slot == null)
            {
                return CreateSuccessResponse(("found", false), ("slotId", slotId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("slotId", slot.SlotId),
                ("path", BuildGameObjectPath(slot.gameObject)),
                ("isEmpty", slot.IsEmpty)
            );
        }

        private object FindByBarId(Dictionary<string, object> payload)
        {
            var barId = GetString(payload, "barId");
            if (string.IsNullOrEmpty(barId))
            {
                throw new InvalidOperationException("barId is required for findByBarId.");
            }

            var bar = GameKitUISlotBar.FindById(barId);
            if (bar == null)
            {
                return CreateSuccessResponse(("found", false), ("barId", barId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("barId", bar.BarId),
                ("path", BuildGameObjectPath(bar.gameObject)),
                ("slotCount", bar.SlotCount)
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
            var layoutType = GameKitUISlotBar.LayoutType.Horizontal;
            if (payload.TryGetValue("layout", out var layoutObj))
            {
                layoutType = ParseBarLayoutType(layoutObj.ToString());
            }

            float width, height;
            switch (layoutType)
            {
                case GameKitUISlotBar.LayoutType.Horizontal:
                    width = slotCount * slotSize + (slotCount - 1) * spacing + 20;
                    height = slotSize + 20;
                    break;
                case GameKitUISlotBar.LayoutType.Vertical:
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
            switch (layoutType)
            {
                case GameKitUISlotBar.LayoutType.Horizontal:
                    var hlg = Undo.AddComponent<HorizontalLayoutGroup>(barGo);
                    hlg.spacing = spacing;
                    hlg.padding = new RectOffset(10, 10, 10, 10);
                    hlg.childAlignment = TextAnchor.MiddleCenter;
                    hlg.childControlWidth = false;
                    hlg.childControlHeight = false;
                    hlg.childForceExpandWidth = false;
                    hlg.childForceExpandHeight = false;
                    break;

                case GameKitUISlotBar.LayoutType.Vertical:
                    var vlg = Undo.AddComponent<VerticalLayoutGroup>(barGo);
                    vlg.spacing = spacing;
                    vlg.padding = new RectOffset(10, 10, 10, 10);
                    vlg.childAlignment = TextAnchor.UpperCenter;
                    vlg.childControlWidth = false;
                    vlg.childControlHeight = false;
                    vlg.childForceExpandWidth = false;
                    vlg.childForceExpandHeight = false;
                    break;

                case GameKitUISlotBar.LayoutType.Grid:
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

        private Color GetColorFromDict(Dictionary<string, object> dict, Color fallback)
        {
            float r = dict.TryGetValue("r", out var rObj) ? Convert.ToSingle(rObj) : fallback.r;
            float g = dict.TryGetValue("g", out var gObj) ? Convert.ToSingle(gObj) : fallback.g;
            float b = dict.TryGetValue("b", out var bObj) ? Convert.ToSingle(bObj) : fallback.b;
            float a = dict.TryGetValue("a", out var aObj) ? Convert.ToSingle(aObj) : fallback.a;
            return new Color(r, g, b, a);
        }

        #endregion

        #region Helpers

        private GameKitUISlot ResolveSlotComponent(Dictionary<string, object> payload)
        {
            var slotId = GetString(payload, "slotId");
            if (!string.IsNullOrEmpty(slotId))
            {
                var slotById = GameKitUISlot.FindById(slotId);
                if (slotById != null)
                {
                    return slotById;
                }
            }

            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var slotByPath = targetGo.GetComponent<GameKitUISlot>();
                    if (slotByPath != null)
                    {
                        return slotByPath;
                    }
                    throw new InvalidOperationException($"No GameKitUISlot component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either slotId or targetPath is required.");
        }

        private GameKitUISlotBar ResolveSlotBarComponent(Dictionary<string, object> payload)
        {
            var barId = GetString(payload, "barId");
            if (!string.IsNullOrEmpty(barId))
            {
                var barById = GameKitUISlotBar.FindById(barId);
                if (barById != null)
                {
                    return barById;
                }
            }

            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var barByPath = targetGo.GetComponent<GameKitUISlotBar>();
                    if (barByPath != null)
                    {
                        return barByPath;
                    }
                    throw new InvalidOperationException($"No GameKitUISlotBar component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either barId or targetPath is required.");
        }

        private GameKitUISlot.SlotType ParseSlotType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "storage" => GameKitUISlot.SlotType.Storage,
                "equipment" => GameKitUISlot.SlotType.Equipment,
                "quickslot" => GameKitUISlot.SlotType.Quickslot,
                "trash" => GameKitUISlot.SlotType.Trash,
                _ => GameKitUISlot.SlotType.Storage
            };
        }

        private GameKitUISlotBar.LayoutType ParseBarLayoutType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "horizontal" => GameKitUISlotBar.LayoutType.Horizontal,
                "vertical" => GameKitUISlotBar.LayoutType.Vertical,
                "grid" => GameKitUISlotBar.LayoutType.Grid,
                _ => GameKitUISlotBar.LayoutType.Horizontal
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

        #endregion
    }
}
