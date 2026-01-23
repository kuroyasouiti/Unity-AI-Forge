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
    /// GameKit UI List handler: create and manage dynamic lists/grids.
    /// Supports inventory display, custom data sources, and selection.
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

        protected override bool RequiresCompilationWait(string operation) => false;

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

                var existingList = targetGo.GetComponent<GameKitUIList>();
                if (existingList != null)
                {
                    throw new InvalidOperationException($"GameObject '{targetPath}' already has a GameKitUIList component.");
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

            var list = Undo.AddComponent<GameKitUIList>(targetGo);
            var serializedList = new SerializedObject(list);

            serializedList.FindProperty("listId").stringValue = listId;

            // Auto-set itemContainer reference to Content child (inside Viewport for ScrollView)
            if (createdNewUI)
            {
                var contentTransform = targetGo.transform.Find("Viewport/Content");
                if (contentTransform != null)
                {
                    serializedList.FindProperty("itemContainer").objectReferenceValue = contentTransform;
                }
                else
                {
                    // Fallback to self if Content not found
                    serializedList.FindProperty("itemContainer").objectReferenceValue = targetGo.transform;
                }
            }

            if (payload.TryGetValue("layout", out var layoutObj))
            {
                var layoutType = ParseLayoutType(layoutObj.ToString());
                serializedList.FindProperty("layout").enumValueIndex = (int)layoutType;
            }

            if (payload.TryGetValue("columns", out var columnsObj))
            {
                serializedList.FindProperty("columns").intValue = Convert.ToInt32(columnsObj);
            }

            if (payload.TryGetValue("cellSize", out var cellSizeObj) && cellSizeObj is Dictionary<string, object> cellDict)
            {
                var cellSize = GetVector2FromDict(cellDict, new Vector2(80, 80));
                serializedList.FindProperty("cellSize").vector2Value = cellSize;
            }

            if (payload.TryGetValue("spacing", out var spacingObj) && spacingObj is Dictionary<string, object> spaceDict)
            {
                var spacing = GetVector2FromDict(spaceDict, new Vector2(10, 10));
                serializedList.FindProperty("spacing").vector2Value = spacing;
            }

            if (payload.TryGetValue("dataSource", out var sourceObj))
            {
                var sourceType = ParseDataSourceType(sourceObj.ToString());
                serializedList.FindProperty("dataSource").enumValueIndex = (int)sourceType;
            }

            if (payload.TryGetValue("sourceId", out var srcIdObj))
            {
                serializedList.FindProperty("sourceId").stringValue = srcIdObj.ToString();
            }

            if (payload.TryGetValue("selectable", out var selectableObj))
            {
                serializedList.FindProperty("selectable").boolValue = Convert.ToBoolean(selectableObj);
            }

            if (payload.TryGetValue("multiSelect", out var multiObj))
            {
                serializedList.FindProperty("multiSelect").boolValue = Convert.ToBoolean(multiObj);
            }

            if (payload.TryGetValue("itemPrefabPath", out var prefabPathObj))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPathObj.ToString());
                if (prefab != null)
                {
                    serializedList.FindProperty("itemPrefab").objectReferenceValue = prefab;
                }
            }

            serializedList.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("listId", listId),
                ("path", BuildGameObjectPath(targetGo))
            );
        }

        #endregion

        #region Update

        private object UpdateList(Dictionary<string, object> payload)
        {
            var list = ResolveListComponent(payload);

            Undo.RecordObject(list, "Update GameKit UI List");

            var serializedList = new SerializedObject(list);

            if (payload.TryGetValue("layout", out var layoutObj))
            {
                var layoutType = ParseLayoutType(layoutObj.ToString());
                serializedList.FindProperty("layout").enumValueIndex = (int)layoutType;
            }

            if (payload.TryGetValue("columns", out var columnsObj))
            {
                serializedList.FindProperty("columns").intValue = Convert.ToInt32(columnsObj);
            }

            if (payload.TryGetValue("selectable", out var selectableObj))
            {
                serializedList.FindProperty("selectable").boolValue = Convert.ToBoolean(selectableObj);
            }

            if (payload.TryGetValue("multiSelect", out var multiObj))
            {
                serializedList.FindProperty("multiSelect").boolValue = Convert.ToBoolean(multiObj);
            }

            serializedList.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(list.gameObject.scene);

            return CreateSuccessResponse(
                ("listId", list.ListId),
                ("path", BuildGameObjectPath(list.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectList(Dictionary<string, object> payload)
        {
            var list = ResolveListComponent(payload);
            var serializedList = new SerializedObject(list);

            var info = new Dictionary<string, object>
            {
                { "listId", list.ListId },
                { "path", BuildGameObjectPath(list.gameObject) },
                { "layout", list.Layout.ToString() },
                { "dataSource", list.Source.ToString() },
                { "sourceId", list.SourceId },
                { "itemCount", list.ItemCount },
                { "selectedCount", list.SelectedIndices.Count },
                { "selectable", serializedList.FindProperty("selectable").boolValue },
                { "multiSelect", serializedList.FindProperty("multiSelect").boolValue }
            };

            return CreateSuccessResponse(("list", info));
        }

        #endregion

        #region Delete

        private object DeleteList(Dictionary<string, object> payload)
        {
            var list = ResolveListComponent(payload);
            var path = BuildGameObjectPath(list.gameObject);
            var listId = list.ListId;
            var scene = list.gameObject.scene;

            Undo.DestroyObjectImmediate(list);
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
            var list = ResolveListComponent(payload);

            if (!payload.TryGetValue("items", out var itemsObj) || !(itemsObj is List<object> itemsList))
            {
                throw new InvalidOperationException("items array is required for setItems.");
            }

            var items = new List<GameKitUIList.ListItemData>();
            foreach (var item in itemsList)
            {
                if (item is Dictionary<string, object> itemDict)
                {
                    items.Add(ParseListItemData(itemDict));
                }
            }

            list.SetItems(items);
            EditorSceneManager.MarkSceneDirty(list.gameObject.scene);

            return CreateSuccessResponse(
                ("listId", list.ListId),
                ("itemCount", items.Count)
            );
        }

        private object AddItem(Dictionary<string, object> payload)
        {
            var list = ResolveListComponent(payload);

            if (!payload.TryGetValue("item", out var itemObj) || !(itemObj is Dictionary<string, object> itemDict))
            {
                throw new InvalidOperationException("item object is required for addItem.");
            }

            var item = ParseListItemData(itemDict);
            list.AddItem(item);
            EditorSceneManager.MarkSceneDirty(list.gameObject.scene);

            return CreateSuccessResponse(
                ("listId", list.ListId),
                ("itemId", item.id),
                ("itemCount", list.ItemCount)
            );
        }

        private object RemoveItem(Dictionary<string, object> payload)
        {
            var list = ResolveListComponent(payload);

            int index = -1;
            if (payload.TryGetValue("index", out var indexObj))
            {
                index = Convert.ToInt32(indexObj);
            }
            else if (payload.TryGetValue("itemId", out var idObj))
            {
                index = list.FindItemIndex(idObj.ToString());
            }

            if (index < 0)
            {
                throw new InvalidOperationException("Either index or itemId is required for removeItem.");
            }

            list.RemoveItemAt(index);
            EditorSceneManager.MarkSceneDirty(list.gameObject.scene);

            return CreateSuccessResponse(
                ("listId", list.ListId),
                ("removedIndex", index),
                ("itemCount", list.ItemCount)
            );
        }

        private object ClearList(Dictionary<string, object> payload)
        {
            var list = ResolveListComponent(payload);
            list.Clear();
            EditorSceneManager.MarkSceneDirty(list.gameObject.scene);

            return CreateSuccessResponse(
                ("listId", list.ListId),
                ("cleared", true)
            );
        }

        #endregion

        #region Selection Operations

        private object SelectItem(Dictionary<string, object> payload)
        {
            var list = ResolveListComponent(payload);

            int index = -1;
            if (payload.TryGetValue("index", out var indexObj))
            {
                index = Convert.ToInt32(indexObj);
            }
            else if (payload.TryGetValue("itemId", out var idObj))
            {
                index = list.FindItemIndex(idObj.ToString());
            }

            if (index < 0)
            {
                throw new InvalidOperationException("Either index or itemId is required for selectItem.");
            }

            list.SelectItem(index);

            return CreateSuccessResponse(
                ("listId", list.ListId),
                ("selectedIndex", index)
            );
        }

        private object DeselectItem(Dictionary<string, object> payload)
        {
            var list = ResolveListComponent(payload);

            int index = -1;
            if (payload.TryGetValue("index", out var indexObj))
            {
                index = Convert.ToInt32(indexObj);
            }
            else if (payload.TryGetValue("itemId", out var idObj))
            {
                index = list.FindItemIndex(idObj.ToString());
            }

            if (index < 0)
            {
                throw new InvalidOperationException("Either index or itemId is required for deselectItem.");
            }

            list.DeselectItem(index);

            return CreateSuccessResponse(
                ("listId", list.ListId),
                ("deselectedIndex", index)
            );
        }

        private object ClearSelection(Dictionary<string, object> payload)
        {
            var list = ResolveListComponent(payload);
            list.ClearSelection();

            return CreateSuccessResponse(
                ("listId", list.ListId),
                ("selectionCleared", true)
            );
        }

        #endregion

        #region Other Operations

        private object RefreshFromSource(Dictionary<string, object> payload)
        {
            var list = ResolveListComponent(payload);
            list.RefreshFromSource();
            EditorSceneManager.MarkSceneDirty(list.gameObject.scene);

            return CreateSuccessResponse(
                ("listId", list.ListId),
                ("itemCount", list.ItemCount),
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

            var list = GameKitUIList.FindById(listId);
            if (list == null)
            {
                return CreateSuccessResponse(("found", false), ("listId", listId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("listId", list.ListId),
                ("path", BuildGameObjectPath(list.gameObject)),
                ("itemCount", list.ItemCount)
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

            // Get layout type
            var layoutType = GameKitUIList.LayoutType.Vertical;
            if (payload.TryGetValue("layout", out var layoutObj))
            {
                layoutType = ParseLayoutType(layoutObj.ToString());
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
            if (layoutType == GameKitUIList.LayoutType.Horizontal)
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
                case GameKitUIList.LayoutType.Vertical:
                    var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
                    vlg.spacing = spacing.y;
                    vlg.padding = new RectOffset(10, 10, 10, 10);
                    vlg.childAlignment = TextAnchor.UpperLeft;
                    vlg.childControlWidth = true;
                    vlg.childControlHeight = false;
                    vlg.childForceExpandWidth = true;
                    vlg.childForceExpandHeight = false;
                    break;

                case GameKitUIList.LayoutType.Horizontal:
                    var hlg = contentGo.AddComponent<HorizontalLayoutGroup>();
                    hlg.spacing = spacing.x;
                    hlg.padding = new RectOffset(10, 10, 10, 10);
                    hlg.childAlignment = TextAnchor.MiddleLeft;
                    hlg.childControlWidth = false;
                    hlg.childControlHeight = true;
                    hlg.childForceExpandWidth = false;
                    hlg.childForceExpandHeight = true;
                    break;

                case GameKitUIList.LayoutType.Grid:
                    var glg = contentGo.AddComponent<GridLayoutGroup>();
                    var cellSize = new Vector2(80, 80);
                    if (payload.TryGetValue("cellSize", out var cellSizeObj) && cellSizeObj is Dictionary<string, object> cellDict)
                    {
                        cellSize = GetVector2FromDict(cellDict, cellSize);
                    }
                    glg.cellSize = cellSize;
                    glg.spacing = spacing;
                    glg.padding = new RectOffset(10, 10, 10, 10);
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
            if (layoutType == GameKitUIList.LayoutType.Horizontal)
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
            scrollRect.horizontal = (layoutType == GameKitUIList.LayoutType.Horizontal);
            scrollRect.vertical = (layoutType != GameKitUIList.LayoutType.Horizontal);
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 1f;

            return scrollViewGo;
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

                    return CreateSuccessResponse(
                        ("prefabPath", prefabPath),
                        ("assignedToList", list.ListId)
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

        private GameKitUIList ResolveListComponent(Dictionary<string, object> payload)
        {
            var listId = GetString(payload, "listId");
            if (!string.IsNullOrEmpty(listId))
            {
                var listById = GameKitUIList.FindById(listId);
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
                    var listByPath = targetGo.GetComponent<GameKitUIList>();
                    if (listByPath != null)
                    {
                        return listByPath;
                    }
                    throw new InvalidOperationException($"No GameKitUIList component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either listId or targetPath is required.");
        }

        private GameKitUIList.LayoutType ParseLayoutType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "vertical" => GameKitUIList.LayoutType.Vertical,
                "horizontal" => GameKitUIList.LayoutType.Horizontal,
                "grid" => GameKitUIList.LayoutType.Grid,
                _ => GameKitUIList.LayoutType.Vertical
            };
        }

        private GameKitUIList.DataSourceType ParseDataSourceType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "inventory" => GameKitUIList.DataSourceType.Inventory,
                "equipment" => GameKitUIList.DataSourceType.Equipment,
                "custom" => GameKitUIList.DataSourceType.Custom,
                _ => GameKitUIList.DataSourceType.Custom
            };
        }

        private GameKitUIList.ListItemData ParseListItemData(Dictionary<string, object> dict)
        {
            var item = new GameKitUIList.ListItemData
            {
                id = dict.TryGetValue("id", out var idObj) ? idObj.ToString() : Guid.NewGuid().ToString().Substring(0, 8),
                name = dict.TryGetValue("name", out var nameObj) ? nameObj.ToString() : "",
                description = dict.TryGetValue("description", out var descObj) ? descObj.ToString() : "",
                quantity = dict.TryGetValue("quantity", out var qtyObj) ? Convert.ToInt32(qtyObj) : 1,
                enabled = dict.TryGetValue("enabled", out var enObj) ? Convert.ToBoolean(enObj) : true
            };

            if (dict.TryGetValue("iconPath", out var iconPathObj))
            {
                item.iconPath = iconPathObj.ToString();
                item.icon = AssetDatabase.LoadAssetAtPath<Sprite>(item.iconPath);
            }

            return item;
        }

        private Vector2 GetVector2FromDict(Dictionary<string, object> dict, Vector2 fallback)
        {
            float x = dict.TryGetValue("x", out var xObj) ? Convert.ToSingle(xObj) : fallback.x;
            float y = dict.TryGetValue("y", out var yObj) ? Convert.ToSingle(yObj) : fallback.y;
            return new Vector2(x, y);
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
