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
            "findBySelectionId"
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
                _ => throw new InvalidOperationException($"Unsupported GameKit UI Selection operation: {operation}")
            };
        }

        #region Create

        private object CreateSelection(Dictionary<string, object> payload)
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

            var existingSelection = targetGo.GetComponent<GameKitUISelection>();
            if (existingSelection != null)
            {
                throw new InvalidOperationException($"GameObject '{targetPath}' already has a GameKitUISelection component.");
            }

            var selectionId = GetString(payload, "selectionId") ?? $"Selection_{Guid.NewGuid().ToString().Substring(0, 8)}";

            var selection = Undo.AddComponent<GameKitUISelection>(targetGo);
            var serializedSelection = new SerializedObject(selection);

            serializedSelection.FindProperty("selectionId").stringValue = selectionId;

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
