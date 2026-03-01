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
    /// GameKit UI Selection handler: create and manage selection groups using UI Toolkit.
    /// Supports radio buttons, toggles, checkboxes, and tabs.
    /// Generates UXML/USS for the selection layout and a C# script for selection logic.
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
                _ => throw new InvalidOperationException($"Unsupported GameKit UI Selection operation: {operation}")
            };
        }

        #region Create

        private object CreateSelection(Dictionary<string, object> payload)
        {
            var selectionId = GetString(payload, "selectionId") ?? $"Selection_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var parentPath = GetString(payload, "parentPath") ?? GetString(payload, "targetPath");
            var selectionType = ParseSelectionType(GetString(payload, "selectionType") ?? "radio");
            var allowNone = GetBool(payload, "allowNone", false);
            var defaultIndex = GetInt(payload, "defaultIndex", 0);
            var layout = ParseLayoutType(GetString(payload, "layout") ?? "horizontal");
            var spacing = GetFloat(payload, "spacing", 10f);
            var uiOutputDir = GetString(payload, "uiOutputDir") ?? UITKGenerationHelper.DefaultUIOutputDir;

            Transform parent = null;
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parentGo = ResolveGameObject(parentPath);
                parent = parentGo.transform;
            }

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(selectionId, "UISelection");

            // Generate UXML + USS (USS written first to avoid import errors)
            var uxmlContent = BuildSelectionUXML(className, selectionId, layout, selectionType);
            var ussContent = BuildSelectionUSS(layout, spacing, selectionType);
            var (uxmlPath, ussPath) = UITKGenerationHelper.WriteUXMLAndUSS(uiOutputDir, className, uxmlContent, ussContent);

            // Create UIDocument GameObject
            var selectionGo = UITKGenerationHelper.CreateUIDocumentGameObject(
                GetString(payload, "name") ?? selectionId, parent, uxmlPath, ussPath);

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "SELECTION_ID", selectionId },
                { "SELECTION_TYPE", selectionType },
                { "ALLOW_NONE", allowNone },
                { "DEFAULT_INDEX", defaultIndex },
                { "LAYOUT", layout },
                { "SPACING", spacing },
                { "UXML_PATH", uxmlPath },
                { "USS_PATH", ussPath }
            };

            var outputDir = GetString(payload, "outputPath");

            // Generate C# script and attach component
            var result = CodeGenHelper.GenerateAndAttach(
                selectionGo, "UISelection", selectionId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate UISelection script.");
            }

            EditorSceneManager.MarkSceneDirty(selectionGo.scene);

            result["selectionId"] = selectionId;
            result["path"] = BuildGameObjectPath(selectionGo);
            result["uxmlPath"] = uxmlPath;
            result["ussPath"] = ussPath;

            return result;
        }

        private string BuildSelectionUXML(string className, string selectionId, string layout, string selectionType)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" xmlns:uie=\"UnityEditor.UIElements\">");
            sb.AppendLine($"    <Style src=\"{className}.uss\" />");
            sb.AppendLine($"    <ui:VisualElement name=\"selection-container\" class=\"selection-container selection-{selectionType.ToLowerInvariant()}\">");
            sb.AppendLine("    </ui:VisualElement>");
            sb.AppendLine("</ui:UXML>");
            return sb.ToString();
        }

        private string BuildSelectionUSS(string layout, float spacing, string selectionType)
        {
            var flexDirection = layout switch
            {
                "Horizontal" => "row",
                "Grid" => "row",
                _ => "column"
            };
            var flexWrap = layout == "Grid" ? "wrap" : "nowrap";

            var sb = new StringBuilder();
            sb.AppendLine(".selection-container {");
            sb.AppendLine($"    flex-direction: {flexDirection};");
            sb.AppendLine($"    flex-wrap: {flexWrap};");
            sb.AppendLine($"    padding: 5px;");
            sb.AppendLine("    align-items: stretch;");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine(".selection-item {");
            sb.AppendLine($"    margin: {spacing / 2}px;");
            sb.AppendLine("    padding: 8px 16px;");
            sb.AppendLine("    background-color: rgba(60, 60, 60, 0.8);");
            sb.AppendLine("    border-radius: 4px;");
            sb.AppendLine("    border-width: 1px;");
            sb.AppendLine("    border-color: rgba(100, 100, 100, 0.5);");
            sb.AppendLine("    cursor: link;");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine(".selection-item:hover {");
            sb.AppendLine("    background-color: rgba(80, 80, 80, 0.9);");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine(".selection-item--selected {");
            sb.AppendLine("    background-color: rgba(50, 100, 200, 0.8);");
            sb.AppendLine("    border-color: rgba(80, 140, 255, 0.9);");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine(".selection-item--disabled {");
            sb.AppendLine("    opacity: 0.5;");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine(".selection-item-label {");
            sb.AppendLine("    font-size: 14px;");
            sb.AppendLine("    color: white;");
            sb.AppendLine("    -unity-text-align: middle-center;");
            sb.AppendLine("}");

            // Tab-specific styling
            if (selectionType == "Tab")
            {
                sb.AppendLine();
                sb.AppendLine(".selection-tab .selection-item {");
                sb.AppendLine("    border-bottom-left-radius: 0;");
                sb.AppendLine("    border-bottom-right-radius: 0;");
                sb.AppendLine("    border-bottom-width: 2px;");
                sb.AppendLine("    border-bottom-color: transparent;");
                sb.AppendLine("}");
                sb.AppendLine();
                sb.AppendLine(".selection-tab .selection-item--selected {");
                sb.AppendLine("    border-bottom-color: rgba(80, 140, 255, 1);");
                sb.AppendLine("}");
            }

            return sb.ToString();
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
            var selectedIds = CodeGenHelper.InvokeMethod<List<string>>(component, "GetSelectedIds") ?? new List<string>();
            var selectedIndex = CodeGenHelper.InvokeMethod<int>(component, "GetSelectedIndex");
            var itemCount = CodeGenHelper.GetPropertyValue<int>(component, "ItemCount");

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
            var items = CodeGenHelper.GetPropertyValue<object>(component, "Items");
            if (items != null)
            {
                var itemsInfo = new List<Dictionary<string, object>>();
                var selectedIndices = CodeGenHelper.GetPropertyValue<object>(component, "SelectedIndices");
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

            // Add UXML/USS paths from tracker
            var tracker = GeneratedScriptTracker.Instance;
            var entry = tracker.FindByComponentId(so.FindProperty("selectionId").stringValue);
            if (entry != null)
            {
                var vars = ScriptGenerator.DeserializeVariables(entry.variablesJson);
                if (vars.TryGetValue("UXML_PATH", out var uxmlPath))
                    info["uxmlPath"] = uxmlPath;
                if (vars.TryGetValue("USS_PATH", out var ussPath))
                    info["ussPath"] = ussPath;
            }

            return CreateSuccessResponse(("selection", info));
        }

        #endregion

        #region Delete

        private object DeleteSelection(Dictionary<string, object> payload)
        {
            var selectionId = GetString(payload, "selectionId");

            try
            {
                var component = ResolveSelectionComponent(payload);
                var path = BuildGameObjectPath(component.gameObject);
                selectionId = new SerializedObject(component).FindProperty("selectionId").stringValue;
                var scene = component.gameObject.scene;

                // Delete UXML/USS assets
                UITKGenerationHelper.DeleteUIAssets(selectionId);

                Undo.DestroyObjectImmediate(component.gameObject);
                ScriptGenerator.Delete(selectionId);

                EditorSceneManager.MarkSceneDirty(scene);

                return CreateSuccessResponse(
                    ("selectionId", selectionId),
                    ("path", path),
                    ("deleted", true)
                );
            }
            catch (InvalidOperationException) when (!string.IsNullOrEmpty(selectionId))
            {
                UITKGenerationHelper.DeleteUIAssets(selectionId);
                ScriptGenerator.Delete(selectionId);

                return CreateSuccessResponse(
                    ("selectionId", selectionId),
                    ("deleted", true),
                    ("note", "Component not found in scene; orphaned script cleaned up.")
                );
            }
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

            var compType = component.GetType();
            var itemType = compType.GetNestedType("SelectionItem")
                ?? CodeGenHelper.FindNestedType(compType, "SelectionItem");

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

            var setItemsMethod = compType.GetMethod("SetItems");
            if (setItemsMethod != null)
            {
                Undo.RecordObject(component, "Set UISelection Items");
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
                ?? CodeGenHelper.FindNestedType(compType, "SelectionItem");

            if (itemType == null)
            {
                throw new InvalidOperationException("Could not resolve SelectionItem type on the generated component.");
            }

            var selectionItem = CreateSelectionItemViaReflection(itemType, itemDict);
            var itemId = itemType.GetField("id")?.GetValue(selectionItem)?.ToString() ?? "";

            var addItemMethod = compType.GetMethod("AddItem");
            if (addItemMethod != null)
            {
                Undo.RecordObject(component, "Add UISelection Item");
                addItemMethod.Invoke(component, new object[] { selectionItem });
            }

            var itemCount = CodeGenHelper.GetPropertyValue<int>(component, "ItemCount");

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
                index = CodeGenHelper.InvokeMethod<int>(component, "FindItemIndex", idObj.ToString());
            }

            if (index < 0)
            {
                throw new InvalidOperationException("Either index or itemId is required for removeItem.");
            }

            Undo.RecordObject(component, "Remove UISelection Item");
            CodeGenHelper.InvokeMethod(component, "RemoveItemAt", index);
            var itemCount = CodeGenHelper.GetPropertyValue<int>(component, "ItemCount");

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
            Undo.RecordObject(component, "Clear UISelection");
            CodeGenHelper.InvokeMethod(component, "Clear");
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

            Undo.RecordObject(component, "Select UISelection Item");
            CodeGenHelper.InvokeMethod(component, "SelectItem", index, fireEvents);

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

            Undo.RecordObject(component, "Select UISelection Item By Id");
            CodeGenHelper.InvokeMethod(component, "SelectItemById", itemId, fireEvents);

            var foundIndex = CodeGenHelper.InvokeMethod<int>(component, "FindItemIndex", itemId);

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
                index = CodeGenHelper.InvokeMethod<int>(component, "FindItemIndex", idObj.ToString());
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

            Undo.RecordObject(component, "Deselect UISelection Item");
            CodeGenHelper.InvokeMethod(component, "DeselectItem", index, fireEvents);

            return CreateSuccessResponse(
                ("selectionId", new SerializedObject(component).FindProperty("selectionId").stringValue),
                ("deselectedIndex", index)
            );
        }

        private object ClearAllSelections(Dictionary<string, object> payload)
        {
            var component = ResolveSelectionComponent(payload);
            Undo.RecordObject(component, "Clear All UISelection");
            CodeGenHelper.InvokeMethod(component, "ClearSelection");

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
                ?? CodeGenHelper.FindNestedType(compType, "SelectionAction");

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
                Undo.RecordObject(component, "Set UISelection Actions");
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
                index = CodeGenHelper.InvokeMethod<int>(component, "FindItemIndex", idObj.ToString());
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
            Undo.RecordObject(component, "Set UISelection Item Enabled");
            CodeGenHelper.InvokeMethod(component, "SetItemEnabled", index, enabled);

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
            var itemCount = CodeGenHelper.GetPropertyValue<int>(component, "ItemCount");
            var selectedIndex = CodeGenHelper.InvokeMethod<int>(component, "GetSelectedIndex");

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

        #region Helpers

        private Component ResolveSelectionComponent(Dictionary<string, object> payload)
            => ResolveGeneratedComponent(payload, "selectionId", "selectionId", "UISelection");

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
                var iconField = itemType.GetField("iconPath");
                if (iconField != null)
                {
                    iconField.SetValue(item, iconPathObj.ToString());
                }
            }

            if (dict.TryGetValue("associatedPanelPath", out var panelPathObj))
            {
                var panelField = itemType.GetField("associatedPanelPath");
                if (panelField != null)
                {
                    panelField.SetValue(item, panelPathObj.ToString());
                }
            }

            return item;
        }

        #endregion
    }
}
