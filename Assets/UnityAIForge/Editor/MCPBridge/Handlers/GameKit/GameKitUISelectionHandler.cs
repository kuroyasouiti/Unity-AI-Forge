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
    /// GameKit UI Selection handler: create and manage selection groups.
    /// Supports radio buttons, toggles, checkboxes, and tabs.
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

        protected override bool RequiresCompilationWait(string operation) => false;

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

                var existingSelection = targetGo.GetComponent<GameKitUISelection>();
                if (existingSelection != null)
                {
                    throw new InvalidOperationException($"GameObject '{targetPath}' already has a GameKitUISelection component.");
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

            var selection = Undo.AddComponent<GameKitUISelection>(targetGo);
            var serializedSelection = new SerializedObject(selection);

            serializedSelection.FindProperty("selectionId").stringValue = selectionId;

            // Auto-set itemContainer reference to Content child (inside Viewport for ScrollView)
            if (createdNewUI)
            {
                var contentTransform = targetGo.transform.Find("Viewport/Content");
                if (contentTransform != null)
                {
                    serializedSelection.FindProperty("itemContainer").objectReferenceValue = contentTransform;
                }
                else
                {
                    // Fallback to self if Content not found
                    serializedSelection.FindProperty("itemContainer").objectReferenceValue = targetGo.transform;
                }
            }

            if (payload.TryGetValue("selectionType", out var typeObj))
            {
                var selectionType = ParseSelectionType(typeObj.ToString());
                serializedSelection.FindProperty("selectionType").enumValueIndex = (int)selectionType;
            }

            if (payload.TryGetValue("allowNone", out var allowNoneObj))
            {
                serializedSelection.FindProperty("allowNone").boolValue = Convert.ToBoolean(allowNoneObj);
            }

            if (payload.TryGetValue("defaultIndex", out var defaultIdxObj))
            {
                serializedSelection.FindProperty("defaultIndex").intValue = Convert.ToInt32(defaultIdxObj);
            }

            if (payload.TryGetValue("layout", out var layoutObj))
            {
                var layoutType = ParseLayoutType(layoutObj.ToString());
                serializedSelection.FindProperty("layout").enumValueIndex = (int)layoutType;
            }

            if (payload.TryGetValue("spacing", out var spacingObj))
            {
                serializedSelection.FindProperty("spacing").floatValue = Convert.ToSingle(spacingObj);
            }

            if (payload.TryGetValue("itemPrefabPath", out var prefabPathObj))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPathObj.ToString());
                if (prefab != null)
                {
                    serializedSelection.FindProperty("itemPrefab").objectReferenceValue = prefab;
                }
            }

            // Visual colors
            if (payload.TryGetValue("normalColor", out var normalObj) && normalObj is Dictionary<string, object> normalDict)
            {
                serializedSelection.FindProperty("normalColor").colorValue = GetColorFromDict(normalDict);
            }

            if (payload.TryGetValue("selectedColor", out var selectedObj) && selectedObj is Dictionary<string, object> selectedDict)
            {
                serializedSelection.FindProperty("selectedColor").colorValue = GetColorFromDict(selectedDict);
            }

            serializedSelection.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("selectionId", selectionId),
                ("path", BuildGameObjectPath(targetGo))
            );
        }

        #endregion

        #region Update

        private object UpdateSelection(Dictionary<string, object> payload)
        {
            var selection = ResolveSelectionComponent(payload);

            Undo.RecordObject(selection, "Update GameKit UI Selection");

            var serializedSelection = new SerializedObject(selection);

            if (payload.TryGetValue("selectionType", out var typeObj))
            {
                var selectionType = ParseSelectionType(typeObj.ToString());
                serializedSelection.FindProperty("selectionType").enumValueIndex = (int)selectionType;
            }

            if (payload.TryGetValue("allowNone", out var allowNoneObj))
            {
                serializedSelection.FindProperty("allowNone").boolValue = Convert.ToBoolean(allowNoneObj);
            }

            if (payload.TryGetValue("defaultIndex", out var defaultIdxObj))
            {
                serializedSelection.FindProperty("defaultIndex").intValue = Convert.ToInt32(defaultIdxObj);
            }

            if (payload.TryGetValue("layout", out var layoutObj))
            {
                var layoutType = ParseLayoutType(layoutObj.ToString());
                serializedSelection.FindProperty("layout").enumValueIndex = (int)layoutType;
            }

            if (payload.TryGetValue("spacing", out var spacingObj))
            {
                serializedSelection.FindProperty("spacing").floatValue = Convert.ToSingle(spacingObj);
            }

            serializedSelection.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(selection.gameObject.scene);

            return CreateSuccessResponse(
                ("selectionId", selection.SelectionId),
                ("path", BuildGameObjectPath(selection.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectSelection(Dictionary<string, object> payload)
        {
            var selection = ResolveSelectionComponent(payload);
            var serializedSelection = new SerializedObject(selection);

            var selectedIds = selection.GetSelectedIds();
            var selectedIndex = selection.GetSelectedIndex();

            var info = new Dictionary<string, object>
            {
                { "selectionId", selection.SelectionId },
                { "path", BuildGameObjectPath(selection.gameObject) },
                { "selectionType", selection.Type.ToString() },
                { "itemCount", selection.ItemCount },
                { "selectedIndex", selectedIndex },
                { "selectedIds", selectedIds },
                { "allowNone", serializedSelection.FindProperty("allowNone").boolValue }
            };

            // Include items info
            var itemsInfo = new List<Dictionary<string, object>>();
            var items = selection.Items;
            for (int i = 0; i < items.Count; i++)
            {
                itemsInfo.Add(new Dictionary<string, object>
                {
                    { "index", i },
                    { "id", items[i].id },
                    { "label", items[i].label },
                    { "enabled", items[i].enabled },
                    { "selected", ((ICollection<int>)selection.SelectedIndices).Contains(i) }
                });
            }
            info["items"] = itemsInfo;

            return CreateSuccessResponse(("selection", info));
        }

        #endregion

        #region Delete

        private object DeleteSelection(Dictionary<string, object> payload)
        {
            var selection = ResolveSelectionComponent(payload);
            var path = BuildGameObjectPath(selection.gameObject);
            var selectionId = selection.SelectionId;
            var scene = selection.gameObject.scene;

            Undo.DestroyObjectImmediate(selection);
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
            var selection = ResolveSelectionComponent(payload);

            if (!payload.TryGetValue("items", out var itemsObj) || !(itemsObj is List<object> itemsList))
            {
                throw new InvalidOperationException("items array is required for setItems.");
            }

            var items = new List<GameKitUISelection.SelectionItem>();
            foreach (var item in itemsList)
            {
                if (item is Dictionary<string, object> itemDict)
                {
                    items.Add(ParseSelectionItem(itemDict));
                }
            }

            selection.SetItems(items);
            EditorSceneManager.MarkSceneDirty(selection.gameObject.scene);

            return CreateSuccessResponse(
                ("selectionId", selection.SelectionId),
                ("itemCount", items.Count)
            );
        }

        private object AddItem(Dictionary<string, object> payload)
        {
            var selection = ResolveSelectionComponent(payload);

            if (!payload.TryGetValue("item", out var itemObj) || !(itemObj is Dictionary<string, object> itemDict))
            {
                throw new InvalidOperationException("item object is required for addItem.");
            }

            var item = ParseSelectionItem(itemDict);
            selection.AddItem(item);
            EditorSceneManager.MarkSceneDirty(selection.gameObject.scene);

            return CreateSuccessResponse(
                ("selectionId", selection.SelectionId),
                ("itemId", item.id),
                ("itemCount", selection.ItemCount)
            );
        }

        private object RemoveItem(Dictionary<string, object> payload)
        {
            var selection = ResolveSelectionComponent(payload);

            int index = -1;
            if (payload.TryGetValue("index", out var indexObj))
            {
                index = Convert.ToInt32(indexObj);
            }
            else if (payload.TryGetValue("itemId", out var idObj))
            {
                index = selection.FindItemIndex(idObj.ToString());
            }

            if (index < 0)
            {
                throw new InvalidOperationException("Either index or itemId is required for removeItem.");
            }

            selection.RemoveItemAt(index);
            EditorSceneManager.MarkSceneDirty(selection.gameObject.scene);

            return CreateSuccessResponse(
                ("selectionId", selection.SelectionId),
                ("removedIndex", index),
                ("itemCount", selection.ItemCount)
            );
        }

        private object ClearSelection(Dictionary<string, object> payload)
        {
            var selection = ResolveSelectionComponent(payload);
            selection.Clear();
            EditorSceneManager.MarkSceneDirty(selection.gameObject.scene);

            return CreateSuccessResponse(
                ("selectionId", selection.SelectionId),
                ("cleared", true)
            );
        }

        #endregion

        #region Selection Operations

        private object SelectItem(Dictionary<string, object> payload)
        {
            var selection = ResolveSelectionComponent(payload);

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

            selection.SelectItem(index, fireEvents);

            return CreateSuccessResponse(
                ("selectionId", selection.SelectionId),
                ("selectedIndex", index)
            );
        }

        private object SelectItemById(Dictionary<string, object> payload)
        {
            var selection = ResolveSelectionComponent(payload);

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

            selection.SelectItemById(itemId, fireEvents);

            return CreateSuccessResponse(
                ("selectionId", selection.SelectionId),
                ("selectedItemId", itemId),
                ("selectedIndex", selection.FindItemIndex(itemId))
            );
        }

        private object DeselectItem(Dictionary<string, object> payload)
        {
            var selection = ResolveSelectionComponent(payload);

            int index = -1;
            if (payload.TryGetValue("index", out var indexObj))
            {
                index = Convert.ToInt32(indexObj);
            }
            else if (payload.TryGetValue("itemId", out var idObj))
            {
                index = selection.FindItemIndex(idObj.ToString());
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

            selection.DeselectItem(index, fireEvents);

            return CreateSuccessResponse(
                ("selectionId", selection.SelectionId),
                ("deselectedIndex", index)
            );
        }

        private object ClearAllSelections(Dictionary<string, object> payload)
        {
            var selection = ResolveSelectionComponent(payload);
            selection.ClearSelection();

            return CreateSuccessResponse(
                ("selectionId", selection.SelectionId),
                ("selectionCleared", true)
            );
        }

        #endregion

        #region Action Operations

        private object SetSelectionActions(Dictionary<string, object> payload)
        {
            var selection = ResolveSelectionComponent(payload);

            if (!payload.TryGetValue("actions", out var actionsObj) || !(actionsObj is List<object> actionsList))
            {
                throw new InvalidOperationException("actions array is required for setSelectionActions.");
            }

            var actions = new List<GameKitUISelection.SelectionAction>();
            foreach (var action in actionsList)
            {
                if (action is Dictionary<string, object> actionDict)
                {
                    var selectionAction = new GameKitUISelection.SelectionAction
                    {
                        selectedId = actionDict.TryGetValue("selectedId", out var idObj) ? idObj.ToString() : ""
                    };

                    if (actionDict.TryGetValue("showPaths", out var showObj) && showObj is List<object> showList)
                    {
                        foreach (var path in showList)
                        {
                            selectionAction.showPaths.Add(path.ToString());
                        }
                    }

                    if (actionDict.TryGetValue("hidePaths", out var hideObj) && hideObj is List<object> hideList)
                    {
                        foreach (var path in hideList)
                        {
                            selectionAction.hidePaths.Add(path.ToString());
                        }
                    }

                    actions.Add(selectionAction);
                }
            }

            selection.SetSelectionActions(actions);

            return CreateSuccessResponse(
                ("selectionId", selection.SelectionId),
                ("actionsCount", actions.Count)
            );
        }

        private object SetItemEnabled(Dictionary<string, object> payload)
        {
            var selection = ResolveSelectionComponent(payload);

            int index = -1;
            if (payload.TryGetValue("index", out var indexObj))
            {
                index = Convert.ToInt32(indexObj);
            }
            else if (payload.TryGetValue("itemId", out var idObj))
            {
                index = selection.FindItemIndex(idObj.ToString());
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
            selection.SetItemEnabled(index, enabled);

            return CreateSuccessResponse(
                ("selectionId", selection.SelectionId),
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

            var selection = GameKitUISelection.FindById(selectionId);
            if (selection == null)
            {
                return CreateSuccessResponse(("found", false), ("selectionId", selectionId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("selectionId", selection.SelectionId),
                ("path", BuildGameObjectPath(selection.gameObject)),
                ("selectionType", selection.Type.ToString()),
                ("itemCount", selection.ItemCount),
                ("selectedIndex", selection.GetSelectedIndex())
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
            var layoutType = GameKitUISelection.LayoutType.Horizontal;
            if (payload.TryGetValue("layout", out var layoutObj))
            {
                layoutType = ParseLayoutType(layoutObj.ToString());
            }

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
                bgImage.color = GetColorFromDict(bgColorDict);
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
            // For horizontal scroll: stretch height, flexible width at left
            // For vertical scroll: stretch width, flexible height at top
            if (layoutType == GameKitUISelection.LayoutType.Horizontal)
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
            switch (layoutType)
            {
                case GameKitUISelection.LayoutType.Horizontal:
                    var hlg = contentGo.AddComponent<HorizontalLayoutGroup>();
                    hlg.spacing = spacing;
                    hlg.padding = new RectOffset(10, 10, 5, 5);
                    hlg.childAlignment = TextAnchor.MiddleLeft;
                    hlg.childControlWidth = false;
                    hlg.childControlHeight = true;
                    hlg.childForceExpandWidth = false;
                    hlg.childForceExpandHeight = true;
                    break;

                case GameKitUISelection.LayoutType.Vertical:
                    var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
                    vlg.spacing = spacing;
                    vlg.padding = new RectOffset(10, 10, 5, 5);
                    vlg.childAlignment = TextAnchor.UpperLeft;
                    vlg.childControlWidth = true;
                    vlg.childControlHeight = false;
                    vlg.childForceExpandWidth = true;
                    vlg.childForceExpandHeight = false;
                    break;

                case GameKitUISelection.LayoutType.Grid:
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
                    break;
            }

            // Add ContentSizeFitter to Content
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            if (layoutType == GameKitUISelection.LayoutType.Horizontal)
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
            scrollRect.horizontal = (layoutType == GameKitUISelection.LayoutType.Horizontal);
            scrollRect.vertical = (layoutType != GameKitUISelection.LayoutType.Horizontal);
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
                normalColor = GetColorFromDict(ncDict);
            if (payload.TryGetValue("selectedColor", out var scObj) && scObj is Dictionary<string, object> scDict)
                selectedColor = GetColorFromDict(scDict);
            if (payload.TryGetValue("highlightedColor", out var hcObj) && hcObj is Dictionary<string, object> hcDict)
                highlightedColor = GetColorFromDict(hcDict);

            // Add background Image
            var bgImage = itemGo.AddComponent<Image>();
            bgImage.color = normalColor;

            // Style-specific setup
            if (style == "tab")
            {
                // Tab style: no rounded corners simulation, border at bottom when selected
                bgImage.type = Image.Type.Sliced;
            }
            else if (style == "radio" || style == "toggle")
            {
                // Radio/Toggle: smaller, circular indicator
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
                    var selection = ResolveSelectionComponent(payload);
                    var serializedSelection = new SerializedObject(selection);
                    serializedSelection.FindProperty("itemPrefab").objectReferenceValue = prefab;
                    serializedSelection.ApplyModifiedProperties();
                    EditorSceneManager.MarkSceneDirty(selection.gameObject.scene);

                    return CreateSuccessResponse(
                        ("prefabPath", prefabPath),
                        ("style", style),
                        ("assignedToSelection", selection.SelectionId)
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

        private GameKitUISelection ResolveSelectionComponent(Dictionary<string, object> payload)
        {
            var selectionId = GetString(payload, "selectionId");
            if (!string.IsNullOrEmpty(selectionId))
            {
                var selectionById = GameKitUISelection.FindById(selectionId);
                if (selectionById != null)
                {
                    return selectionById;
                }
            }

            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var selectionByPath = targetGo.GetComponent<GameKitUISelection>();
                    if (selectionByPath != null)
                    {
                        return selectionByPath;
                    }
                    throw new InvalidOperationException($"No GameKitUISelection component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either selectionId or targetPath is required.");
        }

        private GameKitUISelection.SelectionType ParseSelectionType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "radio" => GameKitUISelection.SelectionType.Radio,
                "toggle" => GameKitUISelection.SelectionType.Toggle,
                "checkbox" => GameKitUISelection.SelectionType.Checkbox,
                "tab" => GameKitUISelection.SelectionType.Tab,
                _ => GameKitUISelection.SelectionType.Radio
            };
        }

        private GameKitUISelection.LayoutType ParseLayoutType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "horizontal" => GameKitUISelection.LayoutType.Horizontal,
                "vertical" => GameKitUISelection.LayoutType.Vertical,
                "grid" => GameKitUISelection.LayoutType.Grid,
                _ => GameKitUISelection.LayoutType.Horizontal
            };
        }

        private GameKitUISelection.SelectionItem ParseSelectionItem(Dictionary<string, object> dict)
        {
            var item = new GameKitUISelection.SelectionItem
            {
                id = dict.TryGetValue("id", out var idObj) ? idObj.ToString() : Guid.NewGuid().ToString().Substring(0, 8),
                label = dict.TryGetValue("label", out var labelObj) ? labelObj.ToString() : "",
                enabled = dict.TryGetValue("enabled", out var enObj) ? Convert.ToBoolean(enObj) : true,
                defaultSelected = dict.TryGetValue("defaultSelected", out var defObj) ? Convert.ToBoolean(defObj) : false
            };

            if (dict.TryGetValue("iconPath", out var iconPathObj))
            {
                item.icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPathObj.ToString());
            }

            if (dict.TryGetValue("associatedPanelPath", out var panelPathObj))
            {
                item.associatedPanel = GameObject.Find(panelPathObj.ToString());
            }

            return item;
        }

        private Color GetColorFromDict(Dictionary<string, object> dict)
        {
            float r = dict.TryGetValue("r", out var rObj) ? Convert.ToSingle(rObj) : 1f;
            float g = dict.TryGetValue("g", out var gObj) ? Convert.ToSingle(gObj) : 1f;
            float b = dict.TryGetValue("b", out var bObj) ? Convert.ToSingle(bObj) : 1f;
            float a = dict.TryGetValue("a", out var aObj) ? Convert.ToSingle(aObj) : 1f;
            return new Color(r, g, b, a);
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
