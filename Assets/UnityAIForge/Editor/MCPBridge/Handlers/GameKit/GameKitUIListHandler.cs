using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using UnityAIForge.GameKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
            "refreshFromSource", "findByListId"
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
                _ => throw new InvalidOperationException($"Unsupported GameKit UI List operation: {operation}")
            };
        }

        #region Create

        private object CreateList(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            if (string.IsNullOrEmpty(targetPath))
            {
                throw new InvalidOperationException("targetPath is required for create operation.");
            }

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
            {
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            var existingList = targetGo.GetComponent<GameKitUIList>();
            if (existingList != null)
            {
                throw new InvalidOperationException($"GameObject '{targetPath}' already has a GameKitUIList component.");
            }

            var listId = GetString(payload, "listId") ?? $"List_{Guid.NewGuid().ToString().Substring(0, 8)}";

            var list = Undo.AddComponent<GameKitUIList>(targetGo);
            var serializedList = new SerializedObject(list);

            serializedList.FindProperty("listId").stringValue = listId;

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
