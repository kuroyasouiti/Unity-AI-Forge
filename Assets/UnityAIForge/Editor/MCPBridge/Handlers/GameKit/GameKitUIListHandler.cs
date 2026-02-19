using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit UI List handler: create and manage dynamic lists/grids using UI Toolkit.
    /// Generates UXML/USS for ScrollView layout and C# script for item management.
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
                _ => throw new InvalidOperationException($"Unsupported GameKit UI List operation: {operation}")
            };
        }

        #region Create

        private object CreateList(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            var parentPath = GetString(payload, "parentPath");
            var listName = GetString(payload, "name") ?? "UIList";
            var uiOutputDir = GetString(payload, "uiOutputDir") ?? UITKGenerationHelper.DefaultUIOutputDir;

            GameObject targetGo;

            if (!string.IsNullOrEmpty(targetPath))
            {
                targetGo = ResolveGameObject(targetPath);
                if (targetGo == null)
                    throw new InvalidOperationException($"GameObject not found at path: {targetPath}");

                var existingList = CodeGenHelper.FindComponentByField(targetGo, "listId", null);
                if (existingList != null)
                    throw new InvalidOperationException($"GameObject '{targetPath}' already has a UIList component.");
            }
            else if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = ResolveGameObject(parentPath);
                if (parent == null)
                    throw new InvalidOperationException($"Parent GameObject not found at path: {parentPath}");

                var listId = GetString(payload, "listId") ?? $"List_{Guid.NewGuid().ToString().Substring(0, 8)}";
                var className = GetString(payload, "className") ?? ScriptGenerator.ToPascalCase(listId, "UIList");
                var layout = GetString(payload, "layout") ?? "vertical";

                // Generate UXML
                var uxmlContent = BuildListUXML(className, layout, payload);
                var uxmlPath = UITKGenerationHelper.WriteUXML(uiOutputDir, className, uxmlContent);

                // Generate USS
                var ussContent = BuildListUSS(layout, payload);
                var ussPath = UITKGenerationHelper.WriteUSS(uiOutputDir, className, ussContent);

                // Create UIDocument GameObject
                targetGo = UITKGenerationHelper.CreateUIDocumentGameObject(listName, parent.transform, uxmlPath, ussPath);

                return FinishCreateList(targetGo, listId, className, payload, uxmlPath, ussPath);
            }
            else
            {
                throw new InvalidOperationException("Either targetPath or parentPath is required for create operation.");
            }

            var listIdVal = GetString(payload, "listId") ?? $"List_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var classNameVal = GetString(payload, "className") ?? ScriptGenerator.ToPascalCase(listIdVal, "UIList");

            return FinishCreateList(targetGo, listIdVal, classNameVal, payload, null, null);
        }

        private object FinishCreateList(GameObject targetGo, string listId, string className,
            Dictionary<string, object> payload, string uxmlPath, string ussPath)
        {
            var layoutStr = ParseLayoutType(GetString(payload, "layout") ?? "vertical");
            var dataSourceStr = ParseDataSourceType(GetString(payload, "dataSource") ?? "custom");
            var columns = GetInt(payload, "columns", 4);
            var cellSizeX = 80f;
            var cellSizeY = 80f;
            if (payload.TryGetValue("cellSize", out var cellSizeObj) && cellSizeObj is Dictionary<string, object> cellDict)
            {
                cellSizeX = cellDict.TryGetValue("x", out var cx) ? Convert.ToSingle(cx) : 80f;
                cellSizeY = cellDict.TryGetValue("y", out var cy) ? Convert.ToSingle(cy) : 80f;
            }
            var spacingX = 10f;
            var spacingY = 10f;
            if (payload.TryGetValue("spacing", out var spacingObj) && spacingObj is Dictionary<string, object> spaceDict)
            {
                spacingX = spaceDict.TryGetValue("x", out var sx) ? Convert.ToSingle(sx) : 10f;
                spacingY = spaceDict.TryGetValue("y", out var sy) ? Convert.ToSingle(sy) : 10f;
            }
            var sourceId = GetString(payload, "sourceId") ?? "";
            var selectable = GetBool(payload, "selectable", false);
            var multiSelect = GetBool(payload, "multiSelect", false);

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

            if (!string.IsNullOrEmpty(uxmlPath)) variables["UXML_PATH"] = uxmlPath;
            if (!string.IsNullOrEmpty(ussPath)) variables["USS_PATH"] = ussPath;

            var outputDir = GetString(payload, "outputPath");

            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "UIList", listId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString() : "Failed to generate UIList script.");

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["listId"] = listId;
            result["path"] = BuildGameObjectPath(targetGo);
            if (!string.IsNullOrEmpty(uxmlPath)) result["uxmlPath"] = uxmlPath;
            if (!string.IsNullOrEmpty(ussPath)) result["ussPath"] = ussPath;

            return result;
        }

        private string BuildListUXML(string className, string layout, Dictionary<string, object> payload)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" xmlns:uie=\"UnityEditor.UIElements\">");
            sb.AppendLine($"    <Style src=\"{className}.uss\" />");
            sb.AppendLine("    <ui:ScrollView name=\"list-scroll\" class=\"list-scroll\">");
            sb.AppendLine("        <ui:VisualElement name=\"list-content\" class=\"list-content\" />");
            sb.AppendLine("    </ui:ScrollView>");
            sb.AppendLine("</ui:UXML>");
            return sb.ToString();
        }

        private string BuildListUSS(string layout, Dictionary<string, object> payload)
        {
            var flexDirection = layout.ToLowerInvariant() switch
            {
                "horizontal" => "row",
                "grid" => "row",
                _ => "column"
            };
            var flexWrap = layout.ToLowerInvariant() == "grid" ? "wrap" : "nowrap";

            float width = payload.TryGetValue("width", out var w) ? Convert.ToSingle(w) : 300f;
            float height = payload.TryGetValue("height", out var h) ? Convert.ToSingle(h) : 400f;

            var sb = new StringBuilder();
            sb.AppendLine(".list-scroll {");
            sb.AppendLine($"    width: {width}px;");
            sb.AppendLine($"    height: {height}px;");
            sb.AppendLine("    background-color: rgba(26, 26, 26, 0.8);");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine(".list-content {");
            sb.AppendLine($"    flex-direction: {flexDirection};");
            sb.AppendLine($"    flex-wrap: {flexWrap};");
            sb.AppendLine("    padding: 10px;");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine(".list-item {");
            sb.AppendLine("    flex-direction: row;");
            sb.AppendLine("    padding: 8px 12px;");
            sb.AppendLine("    margin: 2px 0;");
            sb.AppendLine("    background-color: rgba(51, 51, 51, 0.9);");
            sb.AppendLine("    border-radius: 4px;");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine(".list-item:hover {");
            sb.AppendLine("    background-color: rgba(64, 64, 64, 1);");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine(".list-item--selected {");
            sb.AppendLine("    background-color: rgba(51, 102, 204, 0.8);");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine(".list-item-label {");
            sb.AppendLine("    flex-grow: 1;");
            sb.AppendLine("    color: white;");
            sb.AppendLine("    font-size: 14px;");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine(".list-item-quantity {");
            sb.AppendLine("    color: rgba(200, 200, 200, 1);");
            sb.AppendLine("    font-size: 12px;");
            sb.AppendLine("    width: 40px;");
            sb.AppendLine("    -unity-text-align: middle-right;");
            sb.AppendLine("}");

            return sb.ToString();
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
                    { prop.enumValueIndex = i; break; }
                }
            }

            if (payload.TryGetValue("columns", out var columnsObj))
                so.FindProperty("columns").intValue = Convert.ToInt32(columnsObj);

            if (payload.TryGetValue("selectable", out var selectableObj))
                so.FindProperty("selectable").boolValue = Convert.ToBoolean(selectableObj);

            if (payload.TryGetValue("multiSelect", out var multiObj))
                so.FindProperty("multiSelect").boolValue = Convert.ToBoolean(multiObj);

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("listId", new SerializedObject(component).FindProperty("listId").stringValue),
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

            var itemCountProp = component.GetType().GetProperty("ItemCount");
            if (itemCountProp != null)
                info["itemCount"] = itemCountProp.GetValue(component);

            // Add UXML/USS paths from tracker
            var tracker = GeneratedScriptTracker.Instance;
            var listId = so.FindProperty("listId").stringValue;
            var entry = tracker.FindByComponentId(listId);
            if (entry != null)
            {
                var vars = ScriptGenerator.DeserializeVariables(entry.variablesJson);
                if (vars.TryGetValue("UXML_PATH", out var uxmlPath))
                    info["uxmlPath"] = uxmlPath;
                if (vars.TryGetValue("USS_PATH", out var ussPath))
                    info["ussPath"] = ussPath;
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

            UITKGenerationHelper.DeleteUIAssets(listId);
            Undo.DestroyObjectImmediate(component.gameObject);
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
                throw new InvalidOperationException("items array is required for setItems.");

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
                setItemsMethod.Invoke(component, new[] { items });
            else
                return CreateSuccessResponse(
                    ("listId", new SerializedObject(component).FindProperty("listId").stringValue),
                    ("note", "SetItems method not found. Ensure the generated script is compiled.")
                );

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var so = new SerializedObject(component);
            var itemCountProp = component.GetType().GetProperty("ItemCount");

            return CreateSuccessResponse(
                ("listId", so.FindProperty("listId").stringValue),
                ("itemCount", itemCountProp != null ? itemCountProp.GetValue(component) : itemsList.Count)
            );
        }

        private object AddItem(Dictionary<string, object> payload)
        {
            var component = ResolveListComponent(payload);

            if (!payload.TryGetValue("item", out var itemObj) || !(itemObj is Dictionary<string, object> itemDict))
                throw new InvalidOperationException("item object is required for addItem.");

            var listItemDataType = FindNestedType(component.GetType(), "ListItemData");
            if (listItemDataType == null)
                return CreateSuccessResponse(
                    ("listId", new SerializedObject(component).FindProperty("listId").stringValue),
                    ("note", "AddItem requires the generated script to be compiled.")
                );

            var itemData = CreateListItemDataViaReflection(listItemDataType, itemDict);
            var itemId = listItemDataType.GetField("id")?.GetValue(itemData)?.ToString() ?? "";

            var addItemMethod = component.GetType().GetMethod("AddItem");
            if (addItemMethod != null)
                addItemMethod.Invoke(component, new[] { itemData });
            else
                return CreateSuccessResponse(
                    ("listId", new SerializedObject(component).FindProperty("listId").stringValue),
                    ("note", "AddItem method not found.")
                );

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

            int index = ResolveItemIndex(component, payload);

            var removeMethod = component.GetType().GetMethod("RemoveItemAt");
            if (removeMethod != null)
                removeMethod.Invoke(component, new object[] { index });
            else
                return CreateSuccessResponse(("listId", listId), ("note", "RemoveItemAt method not found."));

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
                clearMethod.Invoke(component, null);
            else
                return CreateSuccessResponse(("listId", listId), ("note", "Clear method not found."));

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            return CreateSuccessResponse(("listId", listId), ("cleared", true));
        }

        #endregion

        #region Selection Operations

        private object SelectItem(Dictionary<string, object> payload)
        {
            var component = ResolveListComponent(payload);
            var so = new SerializedObject(component);
            var listId = so.FindProperty("listId").stringValue;
            int index = ResolveItemIndex(component, payload);

            var selectMethod = component.GetType().GetMethod("SelectItem");
            if (selectMethod != null)
                selectMethod.Invoke(component, new object[] { index });
            else
                return CreateSuccessResponse(("listId", listId), ("note", "SelectItem method not found."));

            return CreateSuccessResponse(("listId", listId), ("selectedIndex", index));
        }

        private object DeselectItem(Dictionary<string, object> payload)
        {
            var component = ResolveListComponent(payload);
            var so = new SerializedObject(component);
            var listId = so.FindProperty("listId").stringValue;
            int index = ResolveItemIndex(component, payload);

            var deselectMethod = component.GetType().GetMethod("DeselectItem");
            if (deselectMethod != null)
                deselectMethod.Invoke(component, new object[] { index });
            else
                return CreateSuccessResponse(("listId", listId), ("note", "DeselectItem method not found."));

            return CreateSuccessResponse(("listId", listId), ("deselectedIndex", index));
        }

        private object ClearSelection(Dictionary<string, object> payload)
        {
            var component = ResolveListComponent(payload);
            var so = new SerializedObject(component);
            var listId = so.FindProperty("listId").stringValue;

            var clearSelMethod = component.GetType().GetMethod("ClearSelection");
            if (clearSelMethod != null)
                clearSelMethod.Invoke(component, null);
            else
                return CreateSuccessResponse(("listId", listId), ("note", "ClearSelection method not found."));

            return CreateSuccessResponse(("listId", listId), ("selectionCleared", true));
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
                refreshMethod.Invoke(component, null);

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
                throw new InvalidOperationException("listId is required for findByListId.");

            var component = CodeGenHelper.FindComponentInSceneByField("listId", listId);
            if (component == null)
                return CreateSuccessResponse(("found", false), ("listId", listId));

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

        #region Helpers

        private Component ResolveListComponent(Dictionary<string, object> payload)
        {
            var listId = GetString(payload, "listId");
            if (!string.IsNullOrEmpty(listId))
            {
                var byId = CodeGenHelper.FindComponentInSceneByField("listId", listId);
                if (byId != null) return byId;
            }

            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var byPath = CodeGenHelper.FindComponentByField(targetGo, "listId", null);
                    if (byPath != null) return byPath;
                    throw new InvalidOperationException($"No UIList component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either listId or targetPath is required.");
        }

        private int ResolveItemIndex(Component component, Dictionary<string, object> payload)
        {
            if (payload.TryGetValue("index", out var indexObj))
                return Convert.ToInt32(indexObj);

            if (payload.TryGetValue("itemId", out var idObj))
            {
                var findMethod = component.GetType().GetMethod("FindItemIndex");
                if (findMethod != null)
                    return (int)findMethod.Invoke(component, new object[] { idObj.ToString() });
            }

            throw new InvalidOperationException("Either index or itemId is required.");
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

        private Type FindNestedType(Type componentType, string nestedTypeName)
        {
            return componentType?.GetNestedType(nestedTypeName,
                BindingFlags.Public | BindingFlags.NonPublic);
        }

        private object CreateListItemDataViaReflection(Type listItemDataType, Dictionary<string, object> dict)
        {
            var item = Activator.CreateInstance(listItemDataType);

            SetFieldIfExists(listItemDataType, item, "id", dict, "id",
                v => v?.ToString() ?? Guid.NewGuid().ToString().Substring(0, 8));
            SetFieldIfExists(listItemDataType, item, "name", dict, "name", v => v?.ToString() ?? "");
            SetFieldIfExists(listItemDataType, item, "description", dict, "description", v => v?.ToString() ?? "");
            SetFieldIfExists(listItemDataType, item, "iconPath", dict, "iconPath", v => v?.ToString() ?? "");
            SetFieldIfExists(listItemDataType, item, "quantity", dict, "quantity", v => Convert.ToInt32(v ?? 1));
            SetFieldIfExists(listItemDataType, item, "enabled", dict, "enabled", v => v != null ? Convert.ToBoolean(v) : true);

            return item;
        }

        private void SetFieldIfExists(Type type, object instance, string fieldName,
            Dictionary<string, object> dict, string dictKey, Func<object, object> converter)
        {
            var field = type.GetField(fieldName);
            if (field == null) return;
            dict.TryGetValue(dictKey, out var value);
            field.SetValue(instance, converter(value));
        }

        #endregion
    }
}
