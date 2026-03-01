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
    /// GameKit UI Slot handler: create and manage slot-based UI using UI Toolkit.
    /// Generates UXML/USS for slot layout and C# script for item management.
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

        #region Slot Create

        private object CreateSlot(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            var parentPath = GetString(payload, "parentPath");
            var slotName = GetString(payload, "name") ?? "UISlot";
            var uiOutputDir = GetString(payload, "uiOutputDir") ?? UITKGenerationHelper.DefaultUIOutputDir;

            GameObject targetGo;

            if (!string.IsNullOrEmpty(targetPath))
            {
                targetGo = ResolveGameObject(targetPath);
                if (targetGo == null)
                    throw new InvalidOperationException($"GameObject not found at path: {targetPath}");

                var existingSlot = CodeGenHelper.FindComponentByField(targetGo, "slotId", null);
                if (existingSlot != null)
                    throw new InvalidOperationException($"GameObject '{targetPath}' already has a UISlot component.");
            }
            else if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = ResolveGameObject(parentPath);
                if (parent == null)
                    throw new InvalidOperationException($"Parent GameObject not found at path: {parentPath}");

                var slotId = GetString(payload, "slotId") ?? $"Slot_{Guid.NewGuid().ToString().Substring(0, 8)}";
                var className = GetString(payload, "className") ?? ScriptGenerator.ToPascalCase(slotId, "UISlot");
                float size = payload.TryGetValue("size", out var sizeObj) ? Convert.ToSingle(sizeObj) : 64f;
                float slotW = payload.TryGetValue("width", out var wObj) ? Convert.ToSingle(wObj) : size;
                float slotH = payload.TryGetValue("height", out var hObj) ? Convert.ToSingle(hObj) : size;

                // Generate UXML + USS (USS written first to avoid import errors)
                var uxmlContent = BuildSlotUXML(className);
                var ussContent = BuildSlotUSS(slotW, slotH);
                var (uxmlPath, ussPath) = UITKGenerationHelper.WriteUXMLAndUSS(uiOutputDir, className, uxmlContent, ussContent);

                // Create UIDocument GameObject
                targetGo = UITKGenerationHelper.CreateUIDocumentGameObject(slotName, parent.transform, uxmlPath, ussPath);

                return FinishCreateSlot(targetGo, slotId, className, payload, uxmlPath, ussPath);
            }
            else
            {
                throw new InvalidOperationException("Either targetPath or parentPath is required for create operation.");
            }

            var slotIdVal = GetString(payload, "slotId") ?? $"Slot_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var classNameVal = GetString(payload, "className") ?? ScriptGenerator.ToPascalCase(slotIdVal, "UISlot");
            return FinishCreateSlot(targetGo, slotIdVal, classNameVal, payload, null, null);
        }

        private object FinishCreateSlot(GameObject targetGo, string slotId, string className,
            Dictionary<string, object> payload, string uxmlPath, string ussPath)
        {
            var slotTypeStr = ParseSlotType(GetString(payload, "slotType") ?? "storage");
            var equipSlotName = GetString(payload, "equipmentSlot") ?? "";
            var inventoryId = GetString(payload, "inventoryId") ?? "";
            var slotIndex = GetInt(payload, "slotIndex", 0);
            var dragDropEnabled = GetBool(payload, "dragDropEnabled", true);

            var variables = new Dictionary<string, object>
            {
                { "SLOT_ID", slotId },
                { "SLOT_INDEX", slotIndex },
                { "SLOT_TYPE", slotTypeStr },
                { "EQUIP_SLOT_NAME", equipSlotName },
                { "INVENTORY_ID", inventoryId },
                { "DRAG_DROP_ENABLED", dragDropEnabled }
            };

            if (!string.IsNullOrEmpty(uxmlPath)) variables["UXML_PATH"] = uxmlPath;
            if (!string.IsNullOrEmpty(ussPath)) variables["USS_PATH"] = ussPath;

            // Handle accepted categories
            if (payload.TryGetValue("acceptedCategories", out var catObj) && catObj is List<object> cats)
            {
                var catStr = string.Join(",", cats);
                variables["ACCEPT_CATEGORIES"] = catStr;
            }

            var outputDir = GetString(payload, "outputPath");

            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "UISlot", slotId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString() : "Failed to generate UISlot script.");

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["slotId"] = slotId;
            result["path"] = BuildGameObjectPath(targetGo);
            if (!string.IsNullOrEmpty(uxmlPath)) result["uxmlPath"] = uxmlPath;
            if (!string.IsNullOrEmpty(ussPath)) result["ussPath"] = ussPath;

            return result;
        }

        private string BuildSlotUXML(string className)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" xmlns:uie=\"UnityEditor.UIElements\">");
            sb.AppendLine($"    <Style src=\"{className}.uss\" />");
            sb.AppendLine("    <ui:VisualElement name=\"slot\" class=\"slot slot--empty\">");
            sb.AppendLine("        <ui:VisualElement name=\"icon\" class=\"slot-icon\" />");
            sb.AppendLine("        <ui:Label name=\"quantity\" class=\"slot-quantity\" text=\"\" />");
            sb.AppendLine("        <ui:VisualElement name=\"highlight\" class=\"slot-highlight\" />");
            sb.AppendLine("    </ui:VisualElement>");
            sb.AppendLine("</ui:UXML>");
            return sb.ToString();
        }

        private string BuildSlotUSS(float width, float height)
        {
            var sb = new StringBuilder();
            sb.AppendLine(".slot {");
            sb.AppendLine($"    width: {width}px;");
            sb.AppendLine($"    height: {height}px;");
            sb.AppendLine("    background-color: rgba(51, 51, 51, 0.5);");
            sb.AppendLine("    border-width: 2px;");
            sb.AppendLine("    border-color: rgba(100, 100, 100, 0.8);");
            sb.AppendLine("    border-radius: 4px;");
            sb.AppendLine("    justify-content: center;");
            sb.AppendLine("    align-items: center;");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine(".slot--empty {");
            sb.AppendLine("    background-color: rgba(51, 51, 51, 0.5);");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine(".slot--filled {");
            sb.AppendLine("    background-color: rgba(77, 77, 77, 0.8);");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine(".slot-icon {");
            sb.AppendLine($"    width: {width * 0.7f}px;");
            sb.AppendLine($"    height: {height * 0.7f}px;");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine(".slot-quantity {");
            sb.AppendLine("    position: absolute;");
            sb.AppendLine("    right: 2px;");
            sb.AppendLine("    bottom: 2px;");
            sb.AppendLine("    font-size: 11px;");
            sb.AppendLine("    color: white;");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine(".slot-highlight {");
            sb.AppendLine("    position: absolute;");
            sb.AppendLine("    left: 0;");
            sb.AppendLine("    top: 0;");
            sb.AppendLine("    right: 0;");
            sb.AppendLine("    bottom: 0;");
            sb.AppendLine("    background-color: rgba(128, 179, 255, 0.5);");
            sb.AppendLine("    display: none;");
            sb.AppendLine("}");

            return sb.ToString();
        }

        #endregion

        #region Slot CRUD

        private object UpdateSlot(Dictionary<string, object> payload)
        {
            var component = ResolveSlotComponent(payload);
            Undo.RecordObject(component, "Update UISlot");
            var so = new SerializedObject(component);

            if (payload.TryGetValue("slotType", out var typeObj))
            {
                var typeName = ParseSlotType(typeObj.ToString());
                var prop = so.FindProperty("slotType");
                var names = prop.enumDisplayNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], typeName, StringComparison.OrdinalIgnoreCase))
                    { prop.enumValueIndex = i; break; }
                }
            }

            if (payload.TryGetValue("equipmentSlot", out var equipObj))
                so.FindProperty("equipSlotName").stringValue = equipObj.ToString();

            if (payload.TryGetValue("dragDropEnabled", out var ddObj))
                so.FindProperty("dragDropEnabled").boolValue = Convert.ToBoolean(ddObj);

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("slotId", so.FindProperty("slotId").stringValue),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        private object InspectSlot(Dictionary<string, object> payload)
        {
            var component = ResolveSlotComponent(payload);
            var so = new SerializedObject(component);

            var slotTypeProp = so.FindProperty("slotType");

            var info = new Dictionary<string, object>
            {
                { "slotId", so.FindProperty("slotId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "slotIndex", so.FindProperty("slotIndex").intValue },
                { "slotType", slotTypeProp.enumValueIndex < slotTypeProp.enumDisplayNames.Length
                    ? slotTypeProp.enumDisplayNames[slotTypeProp.enumValueIndex] : "Storage" },
                { "equipSlotName", so.FindProperty("equipSlotName").stringValue },
                { "dragDropEnabled", so.FindProperty("dragDropEnabled").boolValue }
            };

            var isEmptyProp = component.GetType().GetProperty("IsEmpty");
            if (isEmptyProp != null)
                info["isEmpty"] = isEmptyProp.GetValue(component);

            var currentItemProp = component.GetType().GetProperty("CurrentItem");
            if (currentItemProp != null)
            {
                var item = currentItemProp.GetValue(component);
                if (item != null)
                {
                    var itemIdField = item.GetType().GetField("itemId");
                    var itemNameField = item.GetType().GetField("itemName");
                    if (itemIdField != null)
                        info["currentItemId"] = itemIdField.GetValue(item)?.ToString();
                    if (itemNameField != null)
                        info["currentItemName"] = itemNameField.GetValue(item)?.ToString();
                }
            }

            return CreateSuccessResponse(("slot", info));
        }

        private object DeleteSlot(Dictionary<string, object> payload)
        {
            var slotId = GetString(payload, "slotId");

            try
            {
                var component = ResolveSlotComponent(payload);
                var path = BuildGameObjectPath(component.gameObject);
                slotId = new SerializedObject(component).FindProperty("slotId").stringValue;
                var scene = component.gameObject.scene;

                UITKGenerationHelper.DeleteUIAssets(slotId);
                Undo.DestroyObjectImmediate(component.gameObject);
                ScriptGenerator.Delete(slotId);
                EditorSceneManager.MarkSceneDirty(scene);

                return CreateSuccessResponse(("slotId", slotId), ("path", path), ("deleted", true));
            }
            catch (InvalidOperationException) when (!string.IsNullOrEmpty(slotId))
            {
                UITKGenerationHelper.DeleteUIAssets(slotId);
                ScriptGenerator.Delete(slotId);

                return CreateSuccessResponse(
                    ("slotId", slotId),
                    ("deleted", true),
                    ("note", "Component not found in scene; orphaned script cleaned up.")
                );
            }
        }

        #endregion

        #region Slot Item Operations

        private object SetItem(Dictionary<string, object> payload)
        {
            var component = ResolveSlotComponent(payload);
            var so = new SerializedObject(component);
            var slotId = so.FindProperty("slotId").stringValue;

            var slotDataType = CodeGenHelper.FindNestedType(component.GetType(), "SlotData");
            if (slotDataType == null)
                return CreateSuccessResponse(("slotId", slotId),
                    ("note", "SetItem requires the generated script to be compiled. Please wait for compilation and try again."));

            var slotData = Activator.CreateInstance(slotDataType);
            if (payload.TryGetValue("itemId", out var idObj))
                slotDataType.GetField("itemId")?.SetValue(slotData, idObj.ToString());
            if (payload.TryGetValue("itemName", out var nameObj))
                slotDataType.GetField("itemName")?.SetValue(slotData, nameObj.ToString());
            if (payload.TryGetValue("quantity", out var qtyObj))
                slotDataType.GetField("quantity")?.SetValue(slotData, Convert.ToInt32(qtyObj));
            if (payload.TryGetValue("iconPath", out var iconObj))
                slotDataType.GetField("iconPath")?.SetValue(slotData, iconObj.ToString());

            var setItemMethod = component.GetType().GetMethod("SetItem");
            if (setItemMethod != null)
            {
                Undo.RecordObject(component, "Set UISlot Item");
                setItemMethod.Invoke(component, new[] { slotData });
            }
            else
            {
                return CreateSuccessResponse(("slotId", slotId),
                    ("note", "SetItem requires the generated script to be compiled. Please wait for compilation and try again."));
            }

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            return CreateSuccessResponse(("slotId", slotId), ("itemSet", true));
        }

        private object ClearSlot(Dictionary<string, object> payload)
        {
            var component = ResolveSlotComponent(payload);
            var so = new SerializedObject(component);
            var slotId = so.FindProperty("slotId").stringValue;

            var clearMethod = component.GetType().GetMethod("ClearSlot");
            if (clearMethod != null)
            {
                Undo.RecordObject(component, "Clear UISlot");
                clearMethod.Invoke(component, null);
            }
            else
            {
                return CreateSuccessResponse(("slotId", slotId),
                    ("note", "ClearSlot requires the generated script to be compiled. Please wait for compilation and try again."));
            }

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            return CreateSuccessResponse(("slotId", slotId), ("cleared", true));
        }

        private object SetHighlight(Dictionary<string, object> payload)
        {
            var component = ResolveSlotComponent(payload);
            var so = new SerializedObject(component);
            var slotId = so.FindProperty("slotId").stringValue;
            var highlighted = GetBool(payload, "highlighted", false);

            var highlightMethod = component.GetType().GetMethod("SetHighlight");
            if (highlightMethod != null)
            {
                Undo.RecordObject(component, "Set UISlot Highlight");
                highlightMethod.Invoke(component, new object[] { highlighted });
            }
            else
            {
                return CreateSuccessResponse(("slotId", slotId),
                    ("note", "SetHighlight requires the generated script to be compiled. Please wait for compilation and try again."));
            }

            return CreateSuccessResponse(("slotId", slotId), ("highlighted", highlighted));
        }

        #endregion

        #region Slot Bar

        private object CreateSlotBar(Dictionary<string, object> payload)
        {
            var parentPath = GetString(payload, "parentPath");
            if (string.IsNullOrEmpty(parentPath))
                throw new InvalidOperationException("parentPath is required for createSlotBar.");

            var parent = ResolveGameObject(parentPath);
            if (parent == null)
                throw new InvalidOperationException($"Parent GameObject not found at path: {parentPath}");

            var barId = GetString(payload, "barId") ?? $"SlotBar_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var barName = GetString(payload, "name") ?? "UISlotBar";
            var slotCount = GetInt(payload, "slotCount", 4);
            var layout = GetString(payload, "layout") ?? "horizontal";
            var spacing = payload.TryGetValue("spacing", out var spacObj) ? Convert.ToSingle(spacObj) : 5f;
            var uiOutputDir = GetString(payload, "uiOutputDir") ?? UITKGenerationHelper.DefaultUIOutputDir;

            float slotW = 64f, slotH = 64f;
            if (payload.TryGetValue("slotSize", out var sizeObj) && sizeObj is Dictionary<string, object> sizeDict)
            {
                slotW = sizeDict.TryGetValue("x", out var sx) ? Convert.ToSingle(sx) : 64f;
                slotH = sizeDict.TryGetValue("y", out var sy) ? Convert.ToSingle(sy) : 64f;
            }

            var className = GetString(payload, "className") ?? ScriptGenerator.ToPascalCase(barId, "UISlotBar");

            // Generate UXML + USS (USS written first to avoid import errors)
            var uxmlContent = BuildSlotBarUXML(className, slotCount);
            var ussContent = BuildSlotBarUSS(layout, spacing, slotW, slotH);
            var (uxmlPath, ussPath) = UITKGenerationHelper.WriteUXMLAndUSS(uiOutputDir, className, uxmlContent, ussContent);

            var barGo = UITKGenerationHelper.CreateUIDocumentGameObject(barName, parent.transform, uxmlPath, ussPath);

            var variables = new Dictionary<string, object>
            {
                { "SLOT_ID", barId },
                { "SLOT_INDEX", 0 },
                { "SLOT_TYPE", "Storage" },
                { "EQUIP_SLOT_NAME", "" },
                { "INVENTORY_ID", GetString(payload, "inventoryId") ?? "" },
                { "DRAG_DROP_ENABLED", true },
                { "UXML_PATH", uxmlPath },
                { "USS_PATH", ussPath }
            };

            var outputDir = GetString(payload, "outputPath");

            var result = CodeGenHelper.GenerateAndAttach(
                barGo, "UISlot", barId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString() : "Failed to generate UISlotBar script.");

            EditorSceneManager.MarkSceneDirty(barGo.scene);

            result["barId"] = barId;
            result["path"] = BuildGameObjectPath(barGo);
            result["slotCount"] = slotCount;
            result["uxmlPath"] = uxmlPath;
            result["ussPath"] = ussPath;

            return result;
        }

        private string BuildSlotBarUXML(string className, int slotCount)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" xmlns:uie=\"UnityEditor.UIElements\">");
            sb.AppendLine($"    <Style src=\"{className}.uss\" />");
            sb.AppendLine("    <ui:VisualElement name=\"slot-bar\" class=\"slot-bar\">");

            for (int i = 0; i < slotCount; i++)
            {
                sb.AppendLine($"        <ui:VisualElement name=\"slot-{i}\" class=\"slot slot--empty\">");
                sb.AppendLine($"            <ui:VisualElement name=\"icon-{i}\" class=\"slot-icon\" />");
                sb.AppendLine($"            <ui:Label name=\"quantity-{i}\" class=\"slot-quantity\" text=\"\" />");
                sb.AppendLine($"            <ui:VisualElement name=\"highlight-{i}\" class=\"slot-highlight\" />");
                sb.AppendLine("        </ui:VisualElement>");
            }

            sb.AppendLine("    </ui:VisualElement>");
            sb.AppendLine("</ui:UXML>");
            return sb.ToString();
        }

        private string BuildSlotBarUSS(string layout, float spacing, float slotW, float slotH)
        {
            var flexDir = layout.ToLowerInvariant() switch
            {
                "vertical" => "column",
                "grid" => "row",
                _ => "row"
            };
            var flexWrap = layout.ToLowerInvariant() == "grid" ? "wrap" : "nowrap";

            var sb = new StringBuilder();
            sb.AppendLine(".slot-bar {");
            sb.AppendLine($"    flex-direction: {flexDir};");
            sb.AppendLine($"    flex-wrap: {flexWrap};");
            sb.AppendLine("    padding: 5px;");
            sb.AppendLine("    background-color: rgba(26, 26, 26, 0.8);");
            sb.AppendLine("    border-radius: 4px;");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine(".slot {");
            sb.AppendLine($"    width: {slotW}px;");
            sb.AppendLine($"    height: {slotH}px;");
            sb.AppendLine($"    margin: {spacing / 2}px;");
            sb.AppendLine("    background-color: rgba(51, 51, 51, 0.5);");
            sb.AppendLine("    border-width: 2px;");
            sb.AppendLine("    border-color: rgba(100, 100, 100, 0.8);");
            sb.AppendLine("    border-radius: 4px;");
            sb.AppendLine("    justify-content: center;");
            sb.AppendLine("    align-items: center;");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine(".slot--empty { background-color: rgba(51, 51, 51, 0.5); }");
            sb.AppendLine(".slot--filled { background-color: rgba(77, 77, 77, 0.8); }");
            sb.AppendLine();
            sb.AppendLine(".slot-icon {");
            sb.AppendLine($"    width: {slotW * 0.7f}px;");
            sb.AppendLine($"    height: {slotH * 0.7f}px;");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine(".slot-quantity {");
            sb.AppendLine("    position: absolute;");
            sb.AppendLine("    right: 2px;");
            sb.AppendLine("    bottom: 2px;");
            sb.AppendLine("    font-size: 11px;");
            sb.AppendLine("    color: white;");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine(".slot-highlight {");
            sb.AppendLine("    position: absolute;");
            sb.AppendLine("    left: 0; top: 0; right: 0; bottom: 0;");
            sb.AppendLine("    background-color: rgba(128, 179, 255, 0.5);");
            sb.AppendLine("    display: none;");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private object UpdateSlotBar(Dictionary<string, object> payload) => UpdateSlot(payload);
        private object InspectSlotBar(Dictionary<string, object> payload) => InspectSlot(payload);

        private object DeleteSlotBar(Dictionary<string, object> payload)
        {
            var barId = GetString(payload, "barId");
            if (string.IsNullOrEmpty(barId))
                throw new InvalidOperationException("barId is required for deleteSlotBar.");

            try
            {
                var component = CodeGenHelper.FindComponentInSceneByField("slotId", barId);
                if (component == null)
                    throw new InvalidOperationException($"Slot bar with ID '{barId}' not found.");

                var scene = component.gameObject.scene;
                UITKGenerationHelper.DeleteUIAssets(barId);
                Undo.DestroyObjectImmediate(component.gameObject);
                ScriptGenerator.Delete(barId);
                EditorSceneManager.MarkSceneDirty(scene);

                return CreateSuccessResponse(("barId", barId), ("deleted", true));
            }
            catch (InvalidOperationException)
            {
                UITKGenerationHelper.DeleteUIAssets(barId);
                ScriptGenerator.Delete(barId);

                return CreateSuccessResponse(
                    ("barId", barId),
                    ("deleted", true),
                    ("note", "Component not found in scene; orphaned script cleaned up.")
                );
            }
        }

        private object UseSlot(Dictionary<string, object> payload)
        {
            var component = ResolveSlotComponent(payload);
            var so = new SerializedObject(component);
            var slotId = so.FindProperty("slotId").stringValue;

            return CreateSuccessResponse(("slotId", slotId), ("note", "UseSlot will take effect in play mode."));
        }

        private object RefreshFromInventory(Dictionary<string, object> payload)
        {
            var component = ResolveSlotComponent(payload);
            var so = new SerializedObject(component);
            var slotId = so.FindProperty("slotId").stringValue;

            return CreateSuccessResponse(("slotId", slotId), ("note", "RefreshFromInventory will take effect in play mode."));
        }

        #endregion

        #region Find

        private object FindBySlotId(Dictionary<string, object> payload)
        {
            var slotId = GetString(payload, "slotId");
            if (string.IsNullOrEmpty(slotId))
                throw new InvalidOperationException("slotId is required for findBySlotId.");

            var component = CodeGenHelper.FindComponentInSceneByField("slotId", slotId);
            if (component == null)
                return CreateSuccessResponse(("found", false), ("slotId", slotId));

            return CreateSuccessResponse(
                ("found", true),
                ("slotId", slotId),
                ("path", BuildGameObjectPath(component.gameObject))
            );
        }

        private object FindByBarId(Dictionary<string, object> payload)
        {
            var barId = GetString(payload, "barId");
            if (string.IsNullOrEmpty(barId))
                throw new InvalidOperationException("barId is required for findByBarId.");

            var component = CodeGenHelper.FindComponentInSceneByField("slotId", barId);
            if (component == null)
                return CreateSuccessResponse(("found", false), ("barId", barId));

            return CreateSuccessResponse(
                ("found", true),
                ("barId", barId),
                ("path", BuildGameObjectPath(component.gameObject))
            );
        }

        #endregion

        #region Helpers

        private Component ResolveSlotComponent(Dictionary<string, object> payload)
            => ResolveGeneratedComponent(payload, "slotId", "slotId", "UISlot", "barId");

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

        #endregion
    }
}
