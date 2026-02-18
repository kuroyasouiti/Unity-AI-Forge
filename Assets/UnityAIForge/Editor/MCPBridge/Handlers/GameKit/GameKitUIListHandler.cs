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
    /// GameKit UI List handler: create and manage dynamic lists/grids.
    /// Supports inventory display, custom data sources, and selection.
    /// Uses code generation to produce standalone UIList scripts with zero package dependency.
    /// </summary>
    public class GameKitUIListHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "update", "inspect", "delete",
            "setItems", "addItem", "removeItem", "clear",
            "selectItem", "deselectItem", "clearSelection",
            "refreshFromSource", "findByListId",
            "createItemPrefab"
        };

        public override string Category => "gamekitUIList";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateList(payload),
                "update" => UpdateList(payload),
                "inspect" => InspectList(payload),
                "delete" => DeleteList(payload),
                "setItems" => SetItems(payload),
                "addItem" => AddItem(payload),
                "removeItem" => RemoveItem(payload),
                "clear" => ClearList(payload),
                "selectItem" => SelectItem(payload),
                "deselectItem" => DeselectItem(payload),
                "clearSelection" => ClearSelection(payload),
                "refreshFromSource" => RefreshFromSource(payload),
                "findByListId" => FindByListId(payload),
                "createItemPrefab" => CreateItemPrefab(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit UI List operation: {operation}")
            };
        }

        #region Create

        private object CreateList(Dictionary<string, object> payload)
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

                var existingList = CodeGenHelper.FindComponentByField(targetGo, "listId", null);
                if (existingList != null)
                {
                    throw new InvalidOperationException($"GameObject '{targetPath}' already has a UIList component.");
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

                var listName = name ?? "UIList";
                targetGo = CreateListUIGameObject(parent, listName, payload);
                createdNewUI = true;
            }
            else
            {
                throw new InvalidOperationException("Either targetPath or parentPath is required for create operation.");
            }

            var listId = GetString(payload, "listId") ?? $"List_{Guid.NewGuid().ToString().Substring(0, 8)}";

            // Parse template variables
            var layoutStr = ParseLayoutType(GetString(payload, "layout") ?? "vertical");
            var dataSourceStr = ParseDataSourceType(GetString(payload, "dataSource") ?? "custom");
            var columns = GetInt(payload, "columns", 4);
            var cellSizeX = 80f;
            var cellSizeY = 80f;
            if (payload.TryGetValue("cellSize", out var cellSizeObj) && cellSizeObj is Dictionary<string, object> cellDict)
            {
                var cellSize = GetVector2FromDict(cellDict, new Vector2(80, 80));
                cellSizeX = cellSize.x;
                cellSizeY = cellSize.y;
            }
            var spacingX = 10f;
            var spacingY = 10f;
            if (payload.TryGetValue("spacing", out var spacingObj) && spacingObj is Dictionary<string, object> spaceDict)
            {
                var spacing = GetVector2FromDict(spaceDict, new Vector2(10, 10));
                spacingX = spacing.x;
                spacingY = spacing.y;
            }
            var sourceId = GetString(payload, "sourceId") ?? "";
            var selectable = GetBool(payload, "selectable", false);
            var multiSelect = GetBool(payload, "multiSelect", false);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(listId, "UIList");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "LIST_ID", listId },
                { "LAYOUT", layoutStr },
                { "COLUMNS", columns },
                { "CELL_SIZE_X", cellSizeX },
                { "CELL_SIZE_Y", cellSizeY },
                { "SPACING_X", spacingX },
                { "SPACING_Y", spacingY },
                { "DATA_SOURCE", dataSourceStr },
                { "SOURCE_ID", sourceId },
                { "SELECTABLE", selectable.ToString().ToLowerInvariant() },
                { "MULTI_SELECT", multiSelect.ToString().ToLowerInvariant() }
            };

            // Build properties to set after component is added
            var propertiesToSet = new Dictionary<string, object>();

            // Auto-set itemContainer reference to Content child (inside Viewport for ScrollView)
            if (createdNewUI)
            {
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

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "UIList", listId, className, variables, outputDir,
                propertiesToSet.Count > 0 ? propertiesToSet : null);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate UIList script.");
            }

            // If component was added and we have object references to set via SerializedObject
            if (result.TryGetValue("componentAdded", out var added) && (bool)added)
            {
                var component = CodeGenHelper.FindComponentByField(targetGo, "listId", listId);
                if (component != null && propertiesToSet.Count > 0)
                {
                    var so = new SerializedObject(component);
                    if (propertiesToSet.TryGetValue("itemContainer", out var containerObj) && containerObj is Transform containerTransform)
                    {
                        so.FindProperty("itemContainer").objectReferenceValue = containerTransform;
                    }
                    if (propertiesToSet.TryGetValue("itemPrefab", out var prefabObj2) && prefabObj2 is GameObject prefabGo)
                    {
                        so.FindProperty("itemPrefab").objectReferenceValue = prefabGo;
                    }
                    so.ApplyModifiedProperties();
                }
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["listId"] = listId;
            result["path"] = BuildGameObjectPath(targetGo);

            return result;
        }

        #endregion

        #region Update

        private object UpdateList(Dictionary<string, object> payload)
        {
            var component = ResolveListComponent(payload);

            Undo.RecordObject(component, "Update UIList");

            var so = new SerializedObject(component);

            if (payload.TryGetValue("layout", out var layoutObj))
            {
                var layoutName = ParseLayoutType(layoutObj.ToString());
                var prop = so.FindProperty("layout");
                var names = prop.enumDisplayNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], layoutName, StringComparison.OrdinalIgnoreCase))
                    {
                        prop.enumValueIndex = i;
                        break;
                    }
                }
            }

            if (payload.TryGetValue("columns", out var columnsObj))
            {
                so.FindProperty("columns").intValue = Convert.ToInt32(columnsObj);
            }

            if (payload.TryGetValue("selectable", out var selectableObj))
            {
                so.FindProperty("selectable").boolValue = Convert.ToBoolean(selectableObj);
            }

            if (payload.TryGetValue("multiSelect", out var multiObj))
            {
                so.FindProperty("multiSelect").boolValue = Convert.ToBoolean(multiObj);
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var listId = new SerializedObject(component).FindProperty("listId").stringValue;

            return CreateSuccessResponse(
                ("listId", listId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectList(Dictionary<string, object> payload)
        {
            var component = ResolveListComponent(payload);
            var so = new SerializedObject(component);

            var layoutProp = so.FindProperty("layout");
            var dataSourceProp = so.FindProperty("dataSource");

            var info = new Dictionary<string, object>
            {
                { "listId", so.FindProperty("listId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "layout", layoutProp.enumValueIndex < layoutProp.enumDisplayNames.Length
                    ? layoutProp.enumDisplayNames[layoutProp.enumValueIndex] : "Vertical" },
                { "dataSource", dataSourceProp.enumValueIndex < dataSourceProp.enumDisplayNames.Length
                    ? dataSourceProp.enumDisplayNames[dataSourceProp.enumValueIndex] : "Custom" },
                { "sourceId", so.FindProperty("sourceId").stringValue },
                { "selectable", so.FindProperty("selectable").boolValue },
                { "multiSelect", so.FindProperty("multiSelect").boolValue }
            };

            // Try to get runtime properties via reflection (itemCount, selectedIndices)
            var itemCountProp = component.GetType().GetProperty("ItemCount");
            if (itemCountProp != null)
            {
                info["itemCount"] = itemCountProp.GetValue(component);
            }

            var selectedIndicesProp = component.GetType().GetProperty("SelectedIndices");
            if (selectedIndicesProp != null)
            {
                var selectedIndices = selectedIndicesProp.GetValue(component);
                if (selectedIndices is System.Collections.ICollection collection)
                {
                    info["selectedCount"] = collection.Count;
                }
            }

            return CreateSuccessResponse(("list", info));
        }

        #endregion

        #region Delete

        private object DeleteList(Dictionary<string, object> payload)
        {
            var component = ResolveListComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var listId = new SerializedObject(component).FindProperty("listId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component);

            // Clean up the generated script from tracker
            ScriptGenerator.Delete(listId);

            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("listId", listId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Item Operations

        private object SetItems(Dictionary<string, object> payload)
        {
            var component = ResolveListComponent(payload);

            if (!payload.TryGetValue("items", out var itemsObj) || !(itemsObj is List<object> itemsList))
            {
                throw new InvalidOperationException("items array is required for setItems.");
            }

            // Build list of item data objects via reflection
            var listItemDataType = FindNestedType(component.GetType(), "ListItemData");
            if (listItemDataType == null)
            {
                return CreateSuccessResponse(
                    ("listId", new SerializedObject(component).FindProperty("listId").stringValue),
                    ("note", "SetItems requires the generated script to be compiled. Please wait for compilation and try again.")
                );
            }

            var genericListType = typeof(List<>).MakeGenericType(listItemDataType);
            var items = Activator.CreateInstance(genericListType);
            var addMethod = genericListType.GetMethod("Add");

            foreach (var item in itemsList)
            {
                if (item is Dictionary<string, object> itemDict)
                {
                    var itemData = CreateListItemDataViaReflection(listItemDataType, itemDict);
                    addMethod.Invoke(items, new[] { itemData });
                }
            }

            var setItemsMethod = component.GetType().GetMethod("SetItems");
            if (setItemsMethod != null)
            {
                setItemsMethod.Invoke(component, new[] { items });
            }
            else
            {
                return CreateSuccessResponse(
                    ("listId", new SerializedObject(component).FindProperty("listId").stringValue),
                    ("note", "SetItems method not found. Ensure the generated script is compiled.")
                );
            }

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var so = new SerializedObject(component);
            var itemCountProp = component.GetType().GetProperty("ItemCount");
            var itemCount = itemCountProp != null ? itemCountProp.GetValue(component) : itemsList.Count;

            return CreateSuccessResponse(
                ("listId", so.FindProperty("listId").stringValue),
                ("itemCount", itemCount)
            );
        }

        private object AddItem(Dictionary<string, object> payload)
        {
            var component = ResolveListComponent(payload);

            if (!payload.TryGetValue("item", out var itemObj) || !(itemObj is Dictionary<string, object> itemDict))
            {
                throw new InvalidOperationException("item object is required for addItem.");
            }

            var listItemDataType = FindNestedType(component.GetType(), "ListItemData");
            if (listItemDataType == null)
            {
                return CreateSuccessResponse(
                    ("listId", new SerializedObject(component).FindProperty("listId").stringValue),
                    ("note", "AddItem requires the generated script to be compiled. Please wait for compilation and try again.")
                );
            }

            var itemData = CreateListItemDataViaReflection(listItemDataType, itemDict);
            var itemId = listItemDataType.GetField("id")?.GetValue(itemData)?.ToString() ?? "";

            var addItemMethod = component.GetType().GetMethod("AddItem");
            if (addItemMethod != null)
            {
                addItemMethod.Invoke(component, new[] { itemData });
            }
            else
            {
                return CreateSuccessResponse(
                    ("listId", new SerializedObject(component).FindProperty("listId").stringValue),
                    ("note", "AddItem method not found. Ensure the generated script is compiled.")
                );
            }

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var so = new SerializedObject(component);
            var itemCountProp = component.GetType().GetProperty("ItemCount");

            return CreateSuccessResponse(
                ("listId", so.FindProperty("listId").stringValue),
                ("itemId", itemId),
                ("itemCount", itemCountProp != null ? itemCountProp.GetValue(component) : -1)
            );
        }

        private object RemoveItem(Dictionary<string, object> payload)
        {
            var component = ResolveListComponent(payload);
            var so = new SerializedObject(component);
            var listId = so.FindProperty("listId").stringValue;

            int index = -1;
            if (payload.TryGetValue("index", out var indexObj))
            {
                index = Convert.ToInt32(indexObj);
            }
            else if (payload.TryGetValue("itemId", out var idObj))
            {
                var findMethod = component.GetType().GetMethod("FindItemIndex");
                if (findMethod != null)
                {
                    index = (int)findMethod.Invoke(component, new object[] { idObj.ToString() });
                }
            }

            if (index < 0)
            {
                throw new InvalidOperationException("Either index or itemId is required for removeItem.");
            }

            var removeMethod = component.GetType().GetMethod("RemoveItemAt");
            if (removeMethod != null)
            {
                removeMethod.Invoke(component, new object[] { index });
            }
            else
            {
                return CreateSuccessResponse(
                    ("listId", listId),
                    ("note", "RemoveItemAt method not found. Ensure the generated script is compiled.")
                );
            }

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var itemCountProp = component.GetType().GetProperty("ItemCount");

            return CreateSuccessResponse(
                ("listId", listId),
                ("removedIndex", index),
                ("itemCount", itemCountProp != null ? itemCountProp.GetValue(component) : -1)
            );
        }

        private object ClearList(Dictionary<string, object> payload)
        {
            var component = ResolveListComponent(payload);
            var so = new SerializedObject(component);
            var listId = so.FindProperty("listId").stringValue;

            var clearMethod = component.GetType().GetMethod("Clear");
            if (clearMethod != null)
            {
                clearMethod.Invoke(component, null);
            }
            else
            {
                return CreateSuccessResponse(
                    ("listId", listId),
                    ("note", "Clear method not found. Ensure the generated script is compiled.")
                );
            }

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("listId", listId),
                ("cleared", true)
            );
        }

        #endregion

        #region Selection Operations

        private object SelectItem(Dictionary<string, object> payload)
        {
            var component = ResolveListComponent(payload);
            var so = new SerializedObject(component);
            var listId = so.FindProperty("listId").stringValue;

            int index = -1;
            if (payload.TryGetValue("index", out var indexObj))
            {
                index = Convert.ToInt32(indexObj);
            }
            else if (payload.TryGetValue("itemId", out var idObj))
            {
                var findMethod = component.GetType().GetMethod("FindItemIndex");
                if (findMethod != null)
                {
                    index = (int)findMethod.Invoke(component, new object[] { idObj.ToString() });
                }
            }

            if (index < 0)
            {
                throw new InvalidOperationException("Either index or itemId is required for selectItem.");
            }

            var selectMethod = component.GetType().GetMethod("SelectItem");
            if (selectMethod != null)
            {
                selectMethod.Invoke(component, new object[] { index });
            }
            else
            {
                return CreateSuccessResponse(
                    ("listId", listId),
                    ("note", "SelectItem method not found. Ensure the generated script is compiled.")
                );
            }

            return CreateSuccessResponse(
                ("listId", listId),
                ("selectedIndex", index)
            );
        }

        private object DeselectItem(Dictionary<string, object> payload)
        {
            var component = ResolveListComponent(payload);
            var so = new SerializedObject(component);
            var listId = so.FindProperty("listId").stringValue;

            int index = -1;
            if (payload.TryGetValue("index", out var indexObj))
            {
                index = Convert.ToInt32(indexObj);
            }
            else if (payload.TryGetValue("itemId", out var idObj))
            {
                var findMethod = component.GetType().GetMethod("FindItemIndex");
                if (findMethod != null)
                {
                    index = (int)findMethod.Invoke(component, new object[] { idObj.ToString() });
                }
            }

            if (index < 0)
            {
                throw new InvalidOperationException("Either index or itemId is required for deselectItem.");
            }

            var deselectMethod = component.GetType().GetMethod("DeselectItem");
            if (deselectMethod != null)
            {
                deselectMethod.Invoke(component, new object[] { index });
            }
            else
            {
                return CreateSuccessResponse(
                    ("listId", listId),
                    ("note", "DeselectItem method not found. Ensure the generated script is compiled.")
                );
            }

            return CreateSuccessResponse(
                ("listId", listId),
                ("deselectedIndex", index)
            );
        }

        private object ClearSelection(Dictionary<string, object> payload)
        {
            var component = ResolveListComponent(payload);
            var so = new SerializedObject(component);
            var listId = so.FindProperty("listId").stringValue;

            var clearSelMethod = component.GetType().GetMethod("ClearSelection");
            if (clearSelMethod != null)
            {
                clearSelMethod.Invoke(component, null);
            }
            else
            {
                return CreateSuccessResponse(
                    ("listId", listId),
                    ("note", "ClearSelection method not found. Ensure the generated script is compiled.")
                );
            }

            return CreateSuccessResponse(
                ("listId", listId),
                ("selectionCleared", true)
            );
        }

        #endregion

        #region Other Operations

        private object RefreshFromSource(Dictionary<string, object> payload)
        {
            var component = ResolveListComponent(payload);
            var so = new SerializedObject(component);
            var listId = so.FindProperty("listId").stringValue;

            var refreshMethod = component.GetType().GetMethod("RefreshFromSource");
            if (refreshMethod != null)
            {
                refreshMethod.Invoke(component, null);
            }
            else
            {
                return CreateSuccessResponse(
                    ("listId", listId),
                    ("note", "RefreshFromSource method not found. Ensure the generated script is compiled.")
                );
            }

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var itemCountProp = component.GetType().GetProperty("ItemCount");

            return CreateSuccessResponse(
                ("listId", listId),
                ("itemCount", itemCountProp != null ? itemCountProp.GetValue(component) : -1),
                ("refreshed", true)
            );
        }

        private object FindByListId(Dictionary<string, object> payload)
        {
            var listId = GetString(payload, "listId");
            if (string.IsNullOrEmpty(listId))
            {
                throw new InvalidOperationException("listId is required for findByListId.");
            }

            var component = CodeGenHelper.FindComponentInSceneByField("listId", listId);
            if (component == null)
            {
                return CreateSuccessResponse(("found", false), ("listId", listId));
            }

            var so = new SerializedObject(component);
            var itemCountProp = component.GetType().GetProperty("ItemCount");

            return CreateSuccessResponse(
                ("found", true),
                ("listId", so.FindProperty("listId").stringValue),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("itemCount", itemCountProp != null ? itemCountProp.GetValue(component) : -1)
            );
        }

        #endregion

        #region UI Creation Helpers

        private GameObject CreateListUIGameObject(GameObject parent, string name, Dictionary<string, object> payload)
        {
            // Get size from payload or use defaults
            float width = 300f;
            float height = 400f;
            if (payload.TryGetValue("width", out var widthObj))
            {
                width = Convert.ToSingle(widthObj);
            }
            if (payload.TryGetValue("height", out var heightObj))
            {
                height = Convert.ToSingle(heightObj);
            }

            // Get layout type as string
            var layoutStr = "vertical";
            if (payload.TryGetValue("layout", out var layoutObj))
            {
                layoutStr = layoutObj.ToString().ToLowerInvariant();
            }

            var spacing = new Vector2(10, 10);
            if (payload.TryGetValue("spacing", out var spacingObj) && spacingObj is Dictionary<string, object> spaceDict)
            {
                spacing = GetVector2FromDict(spaceDict, spacing);
            }

            // === Create ScrollView Structure ===

            // 1. Create main ScrollView container
            var scrollViewGo = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(scrollViewGo, "Create UI List ScrollView");
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
                bgImage.color = GetColorFromDict(bgColorDict, new Color(0.1f, 0.1f, 0.1f, 0.8f));
            }
            else
            {
                bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
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
            // For vertical scroll: stretch width, flexible height at top
            // For horizontal scroll: stretch height, flexible width at left
            if (layoutStr == "horizontal")
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
            switch (layoutStr)
            {
                case "vertical":
                    var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
                    vlg.spacing = spacing.y;
                    vlg.padding = new RectOffset(10, 10, 10, 10);
                    vlg.childAlignment = TextAnchor.UpperLeft;
                    vlg.childControlWidth = true;
                    vlg.childControlHeight = false;
                    vlg.childForceExpandWidth = true;
                    vlg.childForceExpandHeight = false;
                    break;

                case "horizontal":
                    var hlg = contentGo.AddComponent<HorizontalLayoutGroup>();
                    hlg.spacing = spacing.x;
                    hlg.padding = new RectOffset(10, 10, 10, 10);
                    hlg.childAlignment = TextAnchor.MiddleLeft;
                    hlg.childControlWidth = false;
                    hlg.childControlHeight = true;
                    hlg.childForceExpandWidth = false;
                    hlg.childForceExpandHeight = true;
                    break;

                case "grid":
                    var glg = contentGo.AddComponent<GridLayoutGroup>();
                    var cellSize = new Vector2(80, 80);
                    if (payload.TryGetValue("cellSize", out var cellSizeObj2) && cellSizeObj2 is Dictionary<string, object> cellDict2)
                    {
                        cellSize = GetVector2FromDict(cellDict2, cellSize);
                    }
                    glg.cellSize = cellSize;
                    glg.spacing = spacing;
                    glg.padding = new RectOffset(10, 10, 10, 10);
                    glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    int columns = 4;
                    if (payload.TryGetValue("columns", out var columnsObj2))
                    {
                        columns = Convert.ToInt32(columnsObj2);
                    }
                    glg.constraintCount = columns;
                    break;
            }

            // Add ContentSizeFitter to Content
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            if (layoutStr == "horizontal")
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
            scrollRect.horizontal = (layoutStr == "horizontal");
            scrollRect.vertical = (layoutStr != "horizontal");
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
            float width = payload.TryGetValue("width", out var widthObj) ? Convert.ToSingle(widthObj) : 280f;
            float height = payload.TryGetValue("height", out var heightObj) ? Convert.ToSingle(heightObj) : 60f;
            bool includeIcon = payload.TryGetValue("includeIcon", out var iconObj) && Convert.ToBoolean(iconObj);
            bool includeQuantity = payload.TryGetValue("includeQuantity", out var qtyObj) && Convert.ToBoolean(qtyObj);

            // Create the item GameObject
            var itemGo = new GameObject(prefabName, typeof(RectTransform));

            var itemRect = itemGo.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(width, height);

            // Add background Image
            var bgImage = itemGo.AddComponent<Image>();
            if (payload.TryGetValue("backgroundColor", out var bgColorObj) && bgColorObj is Dictionary<string, object> bgColorDict)
            {
                bgImage.color = GetColorFromDict(bgColorDict, new Color(0.2f, 0.2f, 0.2f, 0.9f));
            }
            else
            {
                bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            }

            // Add Button component for interactivity
            var button = itemGo.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            colors.selectedColor = new Color(0.25f, 0.4f, 0.6f, 1f);
            colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            button.colors = colors;

            // Add horizontal layout for content
            var hlg = itemGo.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 5, 5);
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // Create Icon (optional)
            if (includeIcon)
            {
                var iconGo = new GameObject("Icon", typeof(RectTransform));
                iconGo.transform.SetParent(itemGo.transform, false);

                var iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.sizeDelta = new Vector2(height - 10, height - 10);

                var iconImage = iconGo.AddComponent<Image>();
                iconImage.color = Color.white;

                // Add LayoutElement to control size
                var iconLayout = iconGo.AddComponent<LayoutElement>();
                iconLayout.preferredWidth = height - 10;
                iconLayout.preferredHeight = height - 10;
            }

            // Create Label Text
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(itemGo.transform, false);

            var labelText = labelGo.AddComponent<Text>();
            labelText.text = "Item";
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 18;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;

            var labelLayout = labelGo.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1;

            // Create Quantity Text (optional)
            if (includeQuantity)
            {
                var qtyGo = new GameObject("Quantity", typeof(RectTransform));
                qtyGo.transform.SetParent(itemGo.transform, false);

                var qtyText = qtyGo.AddComponent<Text>();
                qtyText.text = "x1";
                qtyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                qtyText.fontSize = 16;
                qtyText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                qtyText.alignment = TextAnchor.MiddleRight;

                var qtyLayout = qtyGo.AddComponent<LayoutElement>();
                qtyLayout.preferredWidth = 50;
            }

            // Create Highlight overlay (for selection state)
            var highlightGo = new GameObject("Highlight", typeof(RectTransform));
            highlightGo.transform.SetParent(itemGo.transform, false);
            highlightGo.transform.SetAsLastSibling();

            var highlightRect = highlightGo.GetComponent<RectTransform>();
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.offsetMin = Vector2.zero;
            highlightRect.offsetMax = Vector2.zero;

            var highlightImage = highlightGo.AddComponent<Image>();
            highlightImage.color = new Color(0.3f, 0.6f, 1f, 0.3f);
            highlightImage.raycastTarget = false;
            highlightGo.SetActive(false);

            // Save as prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(itemGo, prefabPath);

            // Clean up scene object
            UnityEngine.Object.DestroyImmediate(itemGo);

            // Optionally assign to a list
            var listId = GetString(payload, "listId");
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(listId) || !string.IsNullOrEmpty(targetPath))
            {
                try
                {
                    var list = ResolveListComponent(payload);
                    var serializedList = new SerializedObject(list);
                    serializedList.FindProperty("itemPrefab").objectReferenceValue = prefab;
                    serializedList.ApplyModifiedProperties();
                    EditorSceneManager.MarkSceneDirty(list.gameObject.scene);

                    var resolvedListId = serializedList.FindProperty("listId").stringValue;

                    return CreateSuccessResponse(
                        ("prefabPath", prefabPath),
                        ("assignedToList", resolvedListId)
                    );
                }
                catch
                {
                    // List not found, just return prefab path
                }
            }

            AssetDatabase.Refresh();

            return CreateSuccessResponse(
                ("prefabPath", prefabPath),
                ("prefabName", prefabName)
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

        private Component ResolveListComponent(Dictionary<string, object> payload)
        {
            var listId = GetString(payload, "listId");
            if (!string.IsNullOrEmpty(listId))
            {
                var listById = CodeGenHelper.FindComponentInSceneByField("listId", listId);
                if (listById != null)
                {
                    return listById;
                }
            }

            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var listByPath = CodeGenHelper.FindComponentByField(targetGo, "listId", null);
                    if (listByPath != null)
                    {
                        return listByPath;
                    }
                    throw new InvalidOperationException($"No UIList component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either listId or targetPath is required.");
        }

        private string ParseLayoutType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "vertical" => "Vertical",
                "horizontal" => "Horizontal",
                "grid" => "Grid",
                _ => "Vertical"
            };
        }

        private string ParseDataSourceType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "inventory" => "Inventory",
                "equipment" => "Equipment",
                "custom" => "Custom",
                _ => "Custom"
            };
        }

        /// <summary>
        /// Finds a nested type within a generated component type by name.
        /// Used to locate ListItemData and similar nested structs/classes.
        /// </summary>
        private Type FindNestedType(Type componentType, string nestedTypeName)
        {
            if (componentType == null) return null;

            var nestedType = componentType.GetNestedType(nestedTypeName,
                BindingFlags.Public | BindingFlags.NonPublic);
            return nestedType;
        }

        /// <summary>
        /// Creates a ListItemData instance via reflection from a dictionary of values.
        /// Works with any generated ListItemData type that has the expected fields.
        /// </summary>
        private object CreateListItemDataViaReflection(Type listItemDataType, Dictionary<string, object> dict)
        {
            var item = Activator.CreateInstance(listItemDataType);

            var idField = listItemDataType.GetField("id");
            if (idField != null)
                idField.SetValue(item, dict.TryGetValue("id", out var idObj) ? idObj.ToString() : Guid.NewGuid().ToString().Substring(0, 8));

            var nameField = listItemDataType.GetField("name");
            if (nameField != null)
                nameField.SetValue(item, dict.TryGetValue("name", out var nameObj) ? nameObj.ToString() : "");

            var descriptionField = listItemDataType.GetField("description");
            if (descriptionField != null)
                descriptionField.SetValue(item, dict.TryGetValue("description", out var descObj) ? descObj.ToString() : "");

            var quantityField = listItemDataType.GetField("quantity");
            if (quantityField != null)
                quantityField.SetValue(item, dict.TryGetValue("quantity", out var qtyObj) ? Convert.ToInt32(qtyObj) : 1);

            var enabledField = listItemDataType.GetField("enabled");
            if (enabledField != null)
                enabledField.SetValue(item, dict.TryGetValue("enabled", out var enObj) ? Convert.ToBoolean(enObj) : true);

            if (dict.TryGetValue("iconPath", out var iconPathObj))
            {
                var iconPathField = listItemDataType.GetField("iconPath");
                if (iconPathField != null)
                    iconPathField.SetValue(item, iconPathObj.ToString());

                var iconField = listItemDataType.GetField("icon");
                if (iconField != null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPathObj.ToString());
                    if (sprite != null)
                        iconField.SetValue(item, sprite);
                }
            }

            return item;
        }

        #endregion
    }
}
