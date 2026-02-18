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
    /// GameKit UI Selection handler: create and manage selection groups.
    /// Supports radio buttons, toggles, checkboxes, and tabs.
    /// Uses code generation to produce standalone UISelection scripts with zero package dependency.
    /// </summary>
    public class GameKitUISelectionHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "update", "inspect", "delete",
            "setItems", "addItem", "removeItem", "clear",
            "selectItem", "selectItemById", "deselectItem", "clearSelection",
            "setSelectionActions", "setItemEnabled",
            "findBySelectionId",
            "createItemPrefab"
        };

        public override string Category => "gamekitUISelection";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateSelection(payload),
                "update" => UpdateSelection(payload),
                "inspect" => InspectSelection(payload),
                "delete" => DeleteSelection(payload),
                "setItems" => SetItems(payload),
                "addItem" => AddItem(payload),
                "removeItem" => RemoveItem(payload),
                "clear" => ClearSelection(payload),
                "selectItem" => SelectItem(payload),
                "selectItemById" => SelectItemById(payload),
                "deselectItem" => DeselectItem(payload),
                "clearSelection" => ClearAllSelections(payload),
                "setSelectionActions" => SetSelectionActions(payload),
                "setItemEnabled" => SetItemEnabled(payload),
                "findBySelectionId" => FindBySelectionId(payload),
                "createItemPrefab" => CreateItemPrefab(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit UI Selection operation: {operation}")
            };
        }

        #region Create

        private object CreateSelection(Dictionary<string, object> payload)
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

                var existingSelection = CodeGenHelper.FindComponentByField(targetGo, "selectionId", null);
                if (existingSelection != null)
                {
                    throw new InvalidOperationException($"GameObject '{targetPath}' already has a UISelection component.");
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

                var selectionName = name ?? "UISelection";
                targetGo = CreateSelectionUIGameObject(parent, selectionName, payload);
                createdNewUI = true;
            }
            else
            {
                throw new InvalidOperationException("Either targetPath or parentPath is required for create operation.");
            }

            var selectionId = GetString(payload, "selectionId") ?? $"Selection_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var selectionType = ParseSelectionType(GetString(payload, "selectionType") ?? "radio");
            var allowNone = GetBool(payload, "allowNone", false);
            var defaultIndex = GetInt(payload, "defaultIndex", 0);
            var layout = ParseLayoutType(GetString(payload, "layout") ?? "horizontal");
            var spacing = GetFloat(payload, "spacing", 10f);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(selectionId, "UISelection");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "SELECTION_ID", selectionId },
                { "SELECTION_TYPE", selectionType },
                { "ALLOW_NONE", allowNone },
                { "DEFAULT_INDEX", defaultIndex },
                { "LAYOUT", layout },
                { "SPACING", spacing }
            };

            // Build properties to set after component is added
            var propertiesToSet = new Dictionary<string, object>();

            if (createdNewUI)
            {
                // itemContainer reference will need to be set after component is added
                var contentTransform = targetGo.transform.Find("Viewport/Content");
                if (contentTransform != null)
                {
                    propertiesToSet["itemContainer"] = contentTransform;
                }
                else
                {
                    propertiesToSet["itemContainer"] = targetGo.transform;
                }
            }

            if (payload.TryGetValue("itemPrefabPath", out var prefabPathObj))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPathObj.ToString());
                if (prefab != null)
                {
                    propertiesToSet["itemPrefab"] = prefab;
                }
            }

            if (payload.TryGetValue("normalColor", out var normalObj) && normalObj is Dictionary<string, object> normalDict)
            {
                propertiesToSet["normalColor"] = normalDict;
            }

            if (payload.TryGetValue("selectedColor", out var selectedObj) && selectedObj is Dictionary<string, object> selectedDict)
            {
                propertiesToSet["selectedColor"] = selectedDict;
            }

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "UISelection", selectionId, className, variables, outputDir,
                propertiesToSet.Count > 0 ? propertiesToSet : null);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate UISelection script.");
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["selectionId"] = selectionId;
            result["path"] = BuildGameObjectPath(targetGo);

            return result;
        }

        #endregion

        #region Update

        private object UpdateSelection(Dictionary<string, object> payload)
        {
            var component = ResolveSelectionComponent(payload);

            Undo.RecordObject(component, "Update UISelection");

            var so = new SerializedObject(component);

            if (payload.TryGetValue("selectionType", out var typeObj))
            {
                var selectionType = ParseSelectionType(typeObj.ToString());
                var prop = so.FindProperty("selectionType");
                var names = prop.enumDisplayNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], selectionType, StringComparison.OrdinalIgnoreCase))
                    {
                        prop.enumValueIndex = i;
                        break;
                    }
                }
            }

            if (payload.TryGetValue("allowNone", out var allowNoneObj))
            {
                so.FindProperty("allowNone").boolValue = Convert.ToBoolean(allowNoneObj);
            }

            if (payload.TryGetValue("defaultIndex", out var defaultIdxObj))
            {
                so.FindProperty("defaultIndex").intValue = Convert.ToInt32(defaultIdxObj);
            }

            if (payload.TryGetValue("layout", out var layoutObj))
            {
                var layoutType = ParseLayoutType(layoutObj.ToString());
                var prop = so.FindProperty("layout");
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

            if (payload.TryGetValue("spacing", out var spacingObj))
            {
                so.FindProperty("spacing").floatValue = Convert.ToSingle(spacingObj);
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("selectionId", so.FindProperty("selectionId").stringValue),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectSelection(Dictionary<string, object> payload)
        {
            var component = ResolveSelectionComponent(payload);
            var so = new SerializedObject(component);

            var selectionTypeProp = so.FindProperty("selectionType");

            // Use reflection to call runtime methods for state info
            var selectedIds = InvokeMethod<List<string>>(component, "GetSelectedIds") ?? new List<string>();
            var selectedIndex = InvokeMethod<int>(component, "GetSelectedIndex");
            var itemCount = GetPropertyValue<int>(component, "ItemCount");

            var info = new Dictionary<string, object>
            {
                { "selectionId", so.FindProperty("selectionId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "selectionType", selectionTypeProp.enumValueIndex < selectionTypeProp.enumDisplayNames.Length
                    ? selectionTypeProp.enumDisplayNames[selectionTypeProp.enumValueIndex] : "Radio" },
                { "itemCount", itemCount },
                { "selectedIndex", selectedIndex },
                { "selectedIds", selectedIds },
                { "allowNone", so.FindProperty("allowNone").boolValue }
            };

            // Include items info via reflection
            var items = GetPropertyValue<object>(component, "Items");
            if (items != null)
            {
                var itemsInfo = new List<Dictionary<string, object>>();
                var selectedIndices = GetPropertyValue<object>(component, "SelectedIndices");
                var itemsList = items as System.Collections.IList;
                if (itemsList != null)
                {
                    for (int i = 0; i < itemsList.Count; i++)
                    {
                        var item = itemsList[i];
                        var itemType = item.GetType();
                        var id = itemType.GetField("id")?.GetValue(item)?.ToString() ?? "";
                        var label = itemType.GetField("label")?.GetValue(item)?.ToString() ?? "";
                        var enabled = (bool)(itemType.GetField("enabled")?.GetValue(item) ?? true);
                        var isSelected = false;

                        // Check if this index is in selectedIndices
                        if (selectedIndices is ICollection<int> selectedSet)
                        {
                            isSelected = selectedSet.Contains(i);
                        }

                        itemsInfo.Add(new Dictionary<string, object>
                        {
                            { "index", i },
                            { "id", id },
                            { "label", label },
                            { "enabled", enabled },
                            { "selected", isSelected }
                        });
                    }
                }
                info["items"] = itemsInfo;
            }

            return CreateSuccessResponse(("selection", info));
        }

        #endregion

        #region Delete

        private object DeleteSelection(Dictionary<string, object> payload)
        {
            var component = ResolveSelectionComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var selectionId = new SerializedObject(component).FindProperty("selectionId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component);

            // Also clean up the generated script from tracker
            ScriptGenerator.Delete(selectionId);

            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("selectionId", selectionId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Item Operations

        private object SetItems(Dictionary<string, object> payload)
        {
            var component = ResolveSelectionComponent(payload);

            if (!payload.TryGetValue("items", out var itemsObj) || !(itemsObj is List<object> itemsList))
            {
                throw new InvalidOperationException("items array is required for setItems.");
            }

            // Build items list via reflection (create instances of the nested SelectionItem type)
            var compType = component.GetType();
            var itemType = compType.GetNestedType("SelectionItem")
                ?? FindNestedType(compType, "SelectionItem");

            if (itemType == null)
            {
                throw new InvalidOperationException("Could not resolve SelectionItem type on the generated component.");
            }

            var listType = typeof(List<>).MakeGenericType(itemType);
            var items = (System.Collections.IList)Activator.CreateInstance(listType);

            foreach (var item in itemsList)
            {
                if (item is Dictionary<string, object> itemDict)
                {
                    var selectionItem = CreateSelectionItemViaReflection(itemType, itemDict);
                    items.Add(selectionItem);
                }
            }

            // Invoke SetItems method
            var setItemsMethod = compType.GetMethod("SetItems");
            if (setItemsMethod != null)
            {
                setItemsMethod.Invoke(component, new object[] { items });
            }

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("selectionId", new SerializedObject(component).FindProperty("selectionId").stringValue),
                ("itemCount", items.Count)
            );
        }

        private object AddItem(Dictionary<string, object> payload)
        {
            var component = ResolveSelectionComponent(payload);

            if (!payload.TryGetValue("item", out var itemObj) || !(itemObj is Dictionary<string, object> itemDict))
            {
                throw new InvalidOperationException("item object is required for addItem.");
            }

            var compType = component.GetType();
            var itemType = compType.GetNestedType("SelectionItem")
                ?? FindNestedType(compType, "SelectionItem");

            if (itemType == null)
            {
                throw new InvalidOperationException("Could not resolve SelectionItem type on the generated component.");
            }

            var selectionItem = CreateSelectionItemViaReflection(itemType, itemDict);
            var itemId = itemType.GetField("id")?.GetValue(selectionItem)?.ToString() ?? "";

            var addItemMethod = compType.GetMethod("AddItem");
            if (addItemMethod != null)
            {
                addItemMethod.Invoke(component, new object[] { selectionItem });
            }

            var itemCount = GetPropertyValue<int>(component, "ItemCount");

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("selectionId", new SerializedObject(component).FindProperty("selectionId").stringValue),
                ("itemId", itemId),
                ("itemCount", itemCount)
            );
        }

        private object RemoveItem(Dictionary<string, object> payload)
        {
            var component = ResolveSelectionComponent(payload);

            int index = -1;
            if (payload.TryGetValue("index", out var indexObj))
            {
                index = Convert.ToInt32(indexObj);
            }
            else if (payload.TryGetValue("itemId", out var idObj))
            {
                index = InvokeMethod<int>(component, "FindItemIndex", idObj.ToString());
            }

            if (index < 0)
            {
                throw new InvalidOperationException("Either index or itemId is required for removeItem.");
            }

            InvokeMethod(component, "RemoveItemAt", index);
            var itemCount = GetPropertyValue<int>(component, "ItemCount");

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("selectionId", new SerializedObject(component).FindProperty("selectionId").stringValue),
                ("removedIndex", index),
                ("itemCount", itemCount)
            );
        }

        private object ClearSelection(Dictionary<string, object> payload)
        {
            var component = ResolveSelectionComponent(payload);
            InvokeMethod(component, "Clear");
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("selectionId", new SerializedObject(component).FindProperty("selectionId").stringValue),
                ("cleared", true)
            );
        }

        #endregion

        #region Selection Operations

        private object SelectItem(Dictionary<string, object> payload)
        {
            var component = ResolveSelectionComponent(payload);

            if (!payload.TryGetValue("index", out var indexObj))
            {
                throw new InvalidOperationException("index is required for selectItem.");
            }

            int index = Convert.ToInt32(indexObj);
            bool fireEvents = true;
            if (payload.TryGetValue("fireEvents", out var fireObj))
            {
                fireEvents = Convert.ToBoolean(fireObj);
            }

            InvokeMethod(component, "SelectItem", index, fireEvents);

            return CreateSuccessResponse(
                ("selectionId", new SerializedObject(component).FindProperty("selectionId").stringValue),
                ("selectedIndex", index)
            );
        }

        private object SelectItemById(Dictionary<string, object> payload)
        {
            var component = ResolveSelectionComponent(payload);

            var itemId = GetString(payload, "itemId");
            if (string.IsNullOrEmpty(itemId))
            {
                throw new InvalidOperationException("itemId is required for selectItemById.");
            }

            bool fireEvents = true;
            if (payload.TryGetValue("fireEvents", out var fireObj))
            {
                fireEvents = Convert.ToBoolean(fireObj);
            }

            InvokeMethod(component, "SelectItemById", itemId, fireEvents);

            var foundIndex = InvokeMethod<int>(component, "FindItemIndex", itemId);

            return CreateSuccessResponse(
                ("selectionId", new SerializedObject(component).FindProperty("selectionId").stringValue),
                ("selectedItemId", itemId),
                ("selectedIndex", foundIndex)
            );
        }

        private object DeselectItem(Dictionary<string, object> payload)
        {
            var component = ResolveSelectionComponent(payload);

            int index = -1;
            if (payload.TryGetValue("index", out var indexObj))
            {
                index = Convert.ToInt32(indexObj);
            }
            else if (payload.TryGetValue("itemId", out var idObj))
            {
                index = InvokeMethod<int>(component, "FindItemIndex", idObj.ToString());
            }

            if (index < 0)
            {
                throw new InvalidOperationException("Either index or itemId is required for deselectItem.");
            }

            bool fireEvents = true;
            if (payload.TryGetValue("fireEvents", out var fireObj))
            {
                fireEvents = Convert.ToBoolean(fireObj);
            }

            InvokeMethod(component, "DeselectItem", index, fireEvents);

            return CreateSuccessResponse(
                ("selectionId", new SerializedObject(component).FindProperty("selectionId").stringValue),
                ("deselectedIndex", index)
            );
        }

        private object ClearAllSelections(Dictionary<string, object> payload)
        {
            var component = ResolveSelectionComponent(payload);
            InvokeMethod(component, "ClearSelection");

            return CreateSuccessResponse(
                ("selectionId", new SerializedObject(component).FindProperty("selectionId").stringValue),
                ("selectionCleared", true)
            );
        }

        #endregion

        #region Action Operations

        private object SetSelectionActions(Dictionary<string, object> payload)
        {
            var component = ResolveSelectionComponent(payload);

            if (!payload.TryGetValue("actions", out var actionsObj) || !(actionsObj is List<object> actionsList))
            {
                throw new InvalidOperationException("actions array is required for setSelectionActions.");
            }

            var compType = component.GetType();
            var actionType = compType.GetNestedType("SelectionAction")
                ?? FindNestedType(compType, "SelectionAction");

            if (actionType == null)
            {
                throw new InvalidOperationException("Could not resolve SelectionAction type on the generated component.");
            }

            var listType = typeof(List<>).MakeGenericType(actionType);
            var actions = (System.Collections.IList)Activator.CreateInstance(listType);

            foreach (var action in actionsList)
            {
                if (action is Dictionary<string, object> actionDict)
                {
                    var selectionAction = Activator.CreateInstance(actionType);

                    var selectedIdField = actionType.GetField("selectedId");
                    if (selectedIdField != null && actionDict.TryGetValue("selectedId", out var idObj))
                    {
                        selectedIdField.SetValue(selectionAction, idObj.ToString());
                    }

                    var showPathsField = actionType.GetField("showPaths");
                    if (showPathsField != null && actionDict.TryGetValue("showPaths", out var showObj) && showObj is List<object> showList)
                    {
                        var pathsList = (List<string>)showPathsField.GetValue(selectionAction);
                        if (pathsList == null)
                        {
                            pathsList = new List<string>();
                            showPathsField.SetValue(selectionAction, pathsList);
                        }
                        foreach (var path in showList)
                        {
                            pathsList.Add(path.ToString());
                        }
                    }

                    var hidePathsField = actionType.GetField("hidePaths");
                    if (hidePathsField != null && actionDict.TryGetValue("hidePaths", out var hideObj) && hideObj is List<object> hideList)
                    {
                        var pathsList = (List<string>)hidePathsField.GetValue(selectionAction);
                        if (pathsList == null)
                        {
                            pathsList = new List<string>();
                            hidePathsField.SetValue(selectionAction, pathsList);
                        }
                        foreach (var path in hideList)
                        {
                            pathsList.Add(path.ToString());
                        }
                    }

                    actions.Add(selectionAction);
                }
            }

            var setActionsMethod = compType.GetMethod("SetSelectionActions");
            if (setActionsMethod != null)
            {
                setActionsMethod.Invoke(component, new object[] { actions });
            }

            return CreateSuccessResponse(
                ("selectionId", new SerializedObject(component).FindProperty("selectionId").stringValue),
                ("actionsCount", actions.Count)
            );
        }

        private object SetItemEnabled(Dictionary<string, object> payload)
        {
            var component = ResolveSelectionComponent(payload);

            int index = -1;
            if (payload.TryGetValue("index", out var indexObj))
            {
                index = Convert.ToInt32(indexObj);
            }
            else if (payload.TryGetValue("itemId", out var idObj))
            {
                index = InvokeMethod<int>(component, "FindItemIndex", idObj.ToString());
            }

            if (index < 0)
            {
                throw new InvalidOperationException("Either index or itemId is required for setItemEnabled.");
            }

            if (!payload.TryGetValue("enabled", out var enabledObj))
            {
                throw new InvalidOperationException("enabled is required for setItemEnabled.");
            }

            bool enabled = Convert.ToBoolean(enabledObj);
            InvokeMethod(component, "SetItemEnabled", index, enabled);

            return CreateSuccessResponse(
                ("selectionId", new SerializedObject(component).FindProperty("selectionId").stringValue),
                ("index", index),
                ("enabled", enabled)
            );
        }

        #endregion

        #region Find Operations

        private object FindBySelectionId(Dictionary<string, object> payload)
        {
            var selectionId = GetString(payload, "selectionId");
            if (string.IsNullOrEmpty(selectionId))
            {
                throw new InvalidOperationException("selectionId is required for findBySelectionId.");
            }

            var component = CodeGenHelper.FindComponentInSceneByField("selectionId", selectionId);
            if (component == null)
            {
                return CreateSuccessResponse(("found", false), ("selectionId", selectionId));
            }

            var so = new SerializedObject(component);
            var selectionTypeProp = so.FindProperty("selectionType");
            var itemCount = GetPropertyValue<int>(component, "ItemCount");
            var selectedIndex = InvokeMethod<int>(component, "GetSelectedIndex");

            return CreateSuccessResponse(
                ("found", true),
                ("selectionId", selectionId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("selectionType", selectionTypeProp.enumValueIndex < selectionTypeProp.enumDisplayNames.Length
                    ? selectionTypeProp.enumDisplayNames[selectionTypeProp.enumValueIndex] : "Radio"),
                ("itemCount", itemCount),
                ("selectedIndex", selectedIndex)
            );
        }

        #endregion

        #region UI Creation Helpers

        private GameObject CreateSelectionUIGameObject(GameObject parent, string name, Dictionary<string, object> payload)
        {
            // Get size from payload or use defaults
            float width = 300f;
            float height = 50f;
            if (payload.TryGetValue("width", out var widthObj))
            {
                width = Convert.ToSingle(widthObj);
            }
            if (payload.TryGetValue("height", out var heightObj))
            {
                height = Convert.ToSingle(heightObj);
            }

            // Get layout type
            var layoutType = ParseLayoutType(GetString(payload, "layout") ?? "horizontal");
            var isHorizontal = string.Equals(layoutType, "Horizontal", StringComparison.OrdinalIgnoreCase);

            float spacing = 10f;
            if (payload.TryGetValue("spacing", out var spacingObj))
            {
                spacing = Convert.ToSingle(spacingObj);
            }

            // === Create ScrollView Structure ===

            // 1. Create main ScrollView container
            var scrollViewGo = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(scrollViewGo, "Create UI Selection ScrollView");
            scrollViewGo.transform.SetParent(parent.transform, false);

            var scrollViewRect = scrollViewGo.GetComponent<RectTransform>();
            scrollViewRect.sizeDelta = new Vector2(width, height);
            scrollViewRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollViewRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollViewRect.pivot = new Vector2(0.5f, 0.5f);
            scrollViewRect.anchoredPosition = Vector2.zero;

            // Add background image
            var bgImage = Undo.AddComponent<Image>(scrollViewGo);
            if (payload.TryGetValue("backgroundColor", out var bgColorObj) && bgColorObj is Dictionary<string, object> bgColorDict)
            {
                bgImage.color = GetColorFromDict(bgColorDict, new Color(0.15f, 0.15f, 0.15f, 0.9f));
            }
            else
            {
                bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            }

            // Add ScrollRect component
            var scrollRect = Undo.AddComponent<ScrollRect>(scrollViewGo);

            // 2. Create Viewport
            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            viewportGo.transform.SetParent(scrollViewGo.transform, false);

            var viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportRect.pivot = new Vector2(0f, 1f);

            // Add Mask and Image to viewport
            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = Color.white;
            var mask = viewportGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // 3. Create Content container
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);

            var contentRect = contentGo.GetComponent<RectTransform>();
            if (isHorizontal)
            {
                contentRect.anchorMin = new Vector2(0f, 0f);
                contentRect.anchorMax = new Vector2(0f, 1f);
                contentRect.pivot = new Vector2(0f, 0.5f);
                contentRect.offsetMin = Vector2.zero;
                contentRect.offsetMax = Vector2.zero;
                contentRect.sizeDelta = new Vector2(0, 0);
            }
            else // Vertical or Grid
            {
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.offsetMin = Vector2.zero;
                contentRect.offsetMax = Vector2.zero;
                contentRect.sizeDelta = new Vector2(0, 0);
            }

            // Add LayoutGroup to Content based on layout type
            var isGrid = string.Equals(layoutType, "Grid", StringComparison.OrdinalIgnoreCase);
            var isVertical = string.Equals(layoutType, "Vertical", StringComparison.OrdinalIgnoreCase);

            if (isHorizontal)
            {
                var hlg = contentGo.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = spacing;
                hlg.padding = new RectOffset(10, 10, 5, 5);
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childControlWidth = false;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = true;
            }
            else if (isVertical)
            {
                var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = spacing;
                vlg.padding = new RectOffset(10, 10, 5, 5);
                vlg.childAlignment = TextAnchor.UpperLeft;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
            }
            else if (isGrid)
            {
                var glg = contentGo.AddComponent<GridLayoutGroup>();
                var cellSize = new Vector2(80, 40);
                if (payload.TryGetValue("cellSize", out var cellSizeObj) && cellSizeObj is Dictionary<string, object> cellDict)
                {
                    float cx = cellDict.TryGetValue("x", out var cxObj) ? Convert.ToSingle(cxObj) : cellSize.x;
                    float cy = cellDict.TryGetValue("y", out var cyObj) ? Convert.ToSingle(cyObj) : cellSize.y;
                    cellSize = new Vector2(cx, cy);
                }
                glg.cellSize = cellSize;
                glg.spacing = new Vector2(spacing, spacing);
                glg.padding = new RectOffset(10, 10, 5, 5);
                glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                int columns = 4;
                if (payload.TryGetValue("columns", out var columnsObj))
                {
                    columns = Convert.ToInt32(columnsObj);
                }
                glg.constraintCount = columns;
            }

            // Add ContentSizeFitter to Content
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            if (isHorizontal)
            {
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
            else
            {
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // 4. Configure ScrollRect
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = isHorizontal;
            scrollRect.vertical = !isHorizontal;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 1f;

            return scrollViewGo;
        }

        #endregion

        #region Prefab Creation

        private object CreateItemPrefab(Dictionary<string, object> payload)
        {
            var prefabPath = GetString(payload, "prefabPath");
            if (string.IsNullOrEmpty(prefabPath))
            {
                throw new InvalidOperationException("prefabPath is required for createItemPrefab.");
            }

            // Ensure path ends with .prefab
            if (!prefabPath.EndsWith(".prefab"))
            {
                prefabPath += ".prefab";
            }

            // Ensure directory exists
            var directory = System.IO.Path.GetDirectoryName(prefabPath);
            if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
            {
                CreateFolderRecursively(directory);
            }

            // Get prefab configuration
            var prefabName = GetString(payload, "name") ?? System.IO.Path.GetFileNameWithoutExtension(prefabPath);
            float width = payload.TryGetValue("width", out var widthObj) ? Convert.ToSingle(widthObj) : 120f;
            float height = payload.TryGetValue("height", out var heightObj) ? Convert.ToSingle(heightObj) : 40f;
            bool includeIcon = payload.TryGetValue("includeIcon", out var iconObj) && Convert.ToBoolean(iconObj);
            var style = GetString(payload, "style") ?? "button"; // "button" | "tab" | "radio" | "toggle"

            // Create the item GameObject
            var itemGo = new GameObject(prefabName, typeof(RectTransform));

            var itemRect = itemGo.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(width, height);

            // Get colors from payload or use defaults
            Color normalColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            Color selectedColor = new Color(0.3f, 0.5f, 0.8f, 1f);
            Color highlightedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            Color pressedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            Color disabledColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);

            if (payload.TryGetValue("normalColor", out var ncObj) && ncObj is Dictionary<string, object> ncDict)
                normalColor = GetColorFromDict(ncDict, normalColor);
            if (payload.TryGetValue("selectedColor", out var scObj) && scObj is Dictionary<string, object> scDict)
                selectedColor = GetColorFromDict(scDict, selectedColor);
            if (payload.TryGetValue("highlightedColor", out var hcObj) && hcObj is Dictionary<string, object> hcDict)
                highlightedColor = GetColorFromDict(hcDict, highlightedColor);

            // Add background Image
            var bgImage = itemGo.AddComponent<Image>();
            bgImage.color = normalColor;

            // Style-specific setup
            if (style == "tab")
            {
                bgImage.type = Image.Type.Sliced;
            }
            else if (style == "radio" || style == "toggle")
            {
                itemRect.sizeDelta = new Vector2(width, height);
            }

            // Add Button component for interactivity
            var button = itemGo.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightedColor;
            colors.pressedColor = pressedColor;
            colors.selectedColor = selectedColor;
            colors.disabledColor = disabledColor;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            // Add content layout
            var hlg = itemGo.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 5, 5);
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // Create indicator for radio/toggle style
            if (style == "radio" || style == "toggle")
            {
                var indicatorGo = new GameObject("Indicator", typeof(RectTransform));
                indicatorGo.transform.SetParent(itemGo.transform, false);

                var indicatorRect = indicatorGo.GetComponent<RectTransform>();
                indicatorRect.sizeDelta = new Vector2(20, 20);

                var indicatorBg = indicatorGo.AddComponent<Image>();
                indicatorBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

                // Inner check/dot
                var checkGo = new GameObject("Check", typeof(RectTransform));
                checkGo.transform.SetParent(indicatorGo.transform, false);

                var checkRect = checkGo.GetComponent<RectTransform>();
                checkRect.anchorMin = new Vector2(0.2f, 0.2f);
                checkRect.anchorMax = new Vector2(0.8f, 0.8f);
                checkRect.offsetMin = Vector2.zero;
                checkRect.offsetMax = Vector2.zero;

                var checkImage = checkGo.AddComponent<Image>();
                checkImage.color = selectedColor;
                checkGo.SetActive(false); // Hidden by default, shown when selected

                var indicatorLayout = indicatorGo.AddComponent<LayoutElement>();
                indicatorLayout.preferredWidth = 20;
                indicatorLayout.preferredHeight = 20;
            }

            // Create Icon (optional)
            if (includeIcon)
            {
                var iconGo = new GameObject("Icon", typeof(RectTransform));
                iconGo.transform.SetParent(itemGo.transform, false);

                var iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.sizeDelta = new Vector2(height - 16, height - 16);

                var iconImage = iconGo.AddComponent<Image>();
                iconImage.color = Color.white;

                var iconLayout = iconGo.AddComponent<LayoutElement>();
                iconLayout.preferredWidth = height - 16;
                iconLayout.preferredHeight = height - 16;
            }

            // Create Label Text
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(itemGo.transform, false);

            var labelText = labelGo.AddComponent<Text>();
            labelText.text = "Option";
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 16;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;

            var labelLayout = labelGo.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1;
            labelLayout.minWidth = 40;

            // Create Selection highlight overlay
            var highlightGo = new GameObject("SelectionHighlight", typeof(RectTransform));
            highlightGo.transform.SetParent(itemGo.transform, false);
            highlightGo.transform.SetAsLastSibling();

            var highlightRect = highlightGo.GetComponent<RectTransform>();
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.offsetMin = Vector2.zero;
            highlightRect.offsetMax = Vector2.zero;

            var highlightImage = highlightGo.AddComponent<Image>();
            highlightImage.color = new Color(selectedColor.r, selectedColor.g, selectedColor.b, 0.3f);
            highlightImage.raycastTarget = false;
            highlightGo.SetActive(false); // Hidden by default

            // Save as prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(itemGo, prefabPath);

            // Clean up scene object
            UnityEngine.Object.DestroyImmediate(itemGo);

            // Optionally assign to a selection
            var selectionId = GetString(payload, "selectionId");
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(selectionId) || !string.IsNullOrEmpty(targetPath))
            {
                try
                {
                    var component = ResolveSelectionComponent(payload);
                    var so = new SerializedObject(component);
                    so.FindProperty("itemPrefab").objectReferenceValue = prefab;
                    so.ApplyModifiedProperties();
                    EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

                    return CreateSuccessResponse(
                        ("prefabPath", prefabPath),
                        ("style", style),
                        ("assignedToSelection", so.FindProperty("selectionId").stringValue)
                    );
                }
                catch
                {
                    // Selection not found, just return prefab path
                }
            }

            AssetDatabase.Refresh();

            return CreateSuccessResponse(
                ("prefabPath", prefabPath),
                ("prefabName", prefabName),
                ("style", style)
            );
        }

        private void CreateFolderRecursively(string path)
        {
            var parts = path.Replace("\\", "/").Split('/');
            var currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                var nextPath = currentPath + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }
                currentPath = nextPath;
            }
        }

        #endregion

        #region Helpers

        private Component ResolveSelectionComponent(Dictionary<string, object> payload)
        {
            // Try by selectionId first
            var selectionId = GetString(payload, "selectionId");
            if (!string.IsNullOrEmpty(selectionId))
            {
                var byId = CodeGenHelper.FindComponentInSceneByField("selectionId", selectionId);
                if (byId != null)
                {
                    return byId;
                }
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var byPath = CodeGenHelper.FindComponentByField(targetGo, "selectionId", null);
                    if (byPath != null)
                    {
                        return byPath;
                    }
                    throw new InvalidOperationException($"No UISelection component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either selectionId or targetPath is required.");
        }

        private string ParseSelectionType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "radio" => "Radio",
                "toggle" => "Toggle",
                "checkbox" => "Checkbox",
                "tab" => "Tab",
                _ => "Radio"
            };
        }

        private string ParseLayoutType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "horizontal" => "Horizontal",
                "vertical" => "Vertical",
                "grid" => "Grid",
                _ => "Horizontal"
            };
        }

        private object CreateSelectionItemViaReflection(Type itemType, Dictionary<string, object> dict)
        {
            var item = Activator.CreateInstance(itemType);

            var idField = itemType.GetField("id");
            if (idField != null)
            {
                idField.SetValue(item, dict.TryGetValue("id", out var idObj) ? idObj.ToString() : Guid.NewGuid().ToString().Substring(0, 8));
            }

            var labelField = itemType.GetField("label");
            if (labelField != null)
            {
                labelField.SetValue(item, dict.TryGetValue("label", out var labelObj) ? labelObj.ToString() : "");
            }

            var enabledField = itemType.GetField("enabled");
            if (enabledField != null)
            {
                enabledField.SetValue(item, dict.TryGetValue("enabled", out var enObj) ? Convert.ToBoolean(enObj) : true);
            }

            var defaultSelectedField = itemType.GetField("defaultSelected");
            if (defaultSelectedField != null)
            {
                defaultSelectedField.SetValue(item, dict.TryGetValue("defaultSelected", out var defObj) ? Convert.ToBoolean(defObj) : false);
            }

            if (dict.TryGetValue("iconPath", out var iconPathObj))
            {
                var iconField = itemType.GetField("icon");
                if (iconField != null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPathObj.ToString());
                    if (sprite != null)
                    {
                        iconField.SetValue(item, sprite);
                    }
                }
            }

            if (dict.TryGetValue("associatedPanelPath", out var panelPathObj))
            {
                var panelField = itemType.GetField("associatedPanel");
                if (panelField != null)
                {
                    var panel = GameObject.Find(panelPathObj.ToString());
                    if (panel != null)
                    {
                        panelField.SetValue(item, panel);
                    }
                }
            }

            return item;
        }

        /// <summary>
        /// Find a nested type by name, searching up the inheritance chain.
        /// </summary>
        private static Type FindNestedType(Type type, string nestedTypeName)
        {
            var current = type;
            while (current != null && current != typeof(object))
            {
                var nested = current.GetNestedType(nestedTypeName,
                    BindingFlags.Public | BindingFlags.NonPublic);
                if (nested != null) return nested;
                current = current.BaseType;
            }
            return null;
        }

        /// <summary>
        /// Invoke a public method by name via reflection.
        /// </summary>
        private static T InvokeMethod<T>(Component component, string methodName, params object[] args)
        {
            var method = component.GetType().GetMethod(methodName,
                BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                return default;
            }
            var result = method.Invoke(component, args);
            if (result is T typed) return typed;
            if (result != null) return (T)Convert.ChangeType(result, typeof(T));
            return default;
        }

        /// <summary>
        /// Invoke a public void method by name via reflection.
        /// </summary>
        private static void InvokeMethod(Component component, string methodName, params object[] args)
        {
            var method = component.GetType().GetMethod(methodName,
                BindingFlags.Public | BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(component, args);
            }
        }

        /// <summary>
        /// Get a public property value via reflection.
        /// </summary>
        private static T GetPropertyValue<T>(Component component, string propertyName)
        {
            var prop = component.GetType().GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) return default;
            var value = prop.GetValue(component);
            if (value is T typed) return typed;
            if (value != null) return (T)Convert.ChangeType(value, typeof(T));
            return default;
        }

        #endregion
    }
}
