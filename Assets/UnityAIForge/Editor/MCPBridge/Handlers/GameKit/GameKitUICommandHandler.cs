using System;
using System.Collections.Generic;
using System.Text;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit UI Command handler: create command panels with buttons using UI Toolkit.
    /// Generates UXML/USS for the panel layout and a C# script for button-to-command wiring.
    /// </summary>
    public class GameKitUICommandHandler : BaseCommandHandler
    {
        private static readonly string[] Operations = { "createCommandPanel", "addCommand", "inspect", "delete" };

        public override string Category => "gamekitUICommand";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "createCommandPanel";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "createCommandPanel" => CreateCommandPanel(payload),
                "addCommand" => AddCommand(payload),
                "inspect" => InspectCommandPanel(payload),
                "delete" => DeleteCommandPanel(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit UI Command operation: {operation}"),
            };
        }

        #region Create Command Panel

        private object CreateCommandPanel(Dictionary<string, object> payload)
        {
            var panelId = GetString(payload, "panelId") ?? $"CommandPanel_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var parentPath = GetString(payload, "parentPath") ?? GetString(payload, "canvasPath");
            var layout = GetString(payload, "layout") ?? "vertical";
            var uiOutputDir = GetString(payload, "uiOutputDir") ?? UITKGenerationHelper.DefaultUIOutputDir;

            Transform parent = null;
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parentGo = ResolveGameObject(parentPath);
                parent = parentGo.transform;
            }

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(panelId, "UICommand");

            // Collect commands for UXML generation
            var commands = new List<(string name, string label, string commandType)>();
            if (payload.TryGetValue("commands", out var commandsObj) && commandsObj is List<object> commandsList)
            {
                foreach (var commandObj in commandsList)
                {
                    if (commandObj is Dictionary<string, object> commandDict)
                    {
                        var name = GetStringFromDict(commandDict, "name") ?? "Command";
                        var label = GetStringFromDict(commandDict, "label") ?? name;
                        var cmdType = GetStringFromDict(commandDict, "commandType") ?? "action";
                        commands.Add((name, label, cmdType));
                    }
                }
            }

            // Generate UXML
            var uxmlContent = BuildCommandPanelUXML(className, panelId, commands, layout);
            var uxmlPath = UITKGenerationHelper.WriteUXML(uiOutputDir, className, uxmlContent);

            // Generate USS
            var ussContent = BuildCommandPanelUSS(layout);
            var ussPath = UITKGenerationHelper.WriteUSS(uiOutputDir, className, ussContent);

            // Create UIDocument GameObject
            var panelGo = UITKGenerationHelper.CreateUIDocumentGameObject(panelId, parent, uxmlPath, ussPath);

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "PANEL_ID", panelId },
                { "UXML_PATH", uxmlPath },
                { "USS_PATH", ussPath }
            };

            var outputDir = GetString(payload, "outputPath");

            // Generate C# script and attach component
            var result = CodeGenHelper.GenerateAndAttach(
                panelGo, "UICommand", panelId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate UICommand script.");
            }

            // Register command bindings on the component if it was added
            var uiCommandComp = CodeGenHelper.FindComponentByField(panelGo, "panelId", panelId);
            if (uiCommandComp != null)
            {
                foreach (var (name, label, cmdType) in commands)
                {
                    RegisterCommandBinding(uiCommandComp, name, name, cmdType);
                }
            }

            EditorSceneManager.MarkSceneDirty(panelGo.scene);

            result["panelId"] = panelId;
            result["path"] = BuildGameObjectPath(panelGo);
            result["uxmlPath"] = uxmlPath;
            result["ussPath"] = ussPath;

            return result;
        }

        private string BuildCommandPanelUXML(string className, string panelId, List<(string name, string label, string commandType)> commands, string layout)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" xmlns:uie=\"UnityEditor.UIElements\">");
            sb.AppendLine($"    <Style src=\"{className}.uss\" />");
            sb.AppendLine($"    <ui:VisualElement name=\"command-panel\" class=\"command-panel\">");

            foreach (var (name, label, _) in commands)
            {
                sb.AppendLine($"        <ui:Button name=\"{name}\" text=\"{EscapeXml(label)}\" class=\"command-button\" />");
            }

            sb.AppendLine("    </ui:VisualElement>");
            sb.AppendLine("</ui:UXML>");
            return sb.ToString();
        }

        private string BuildCommandPanelUSS(string layout)
        {
            var flexDirection = layout.ToLowerInvariant() switch
            {
                "horizontal" => "row",
                "grid" => "row",
                _ => "column"
            };
            var flexWrap = layout.ToLowerInvariant() == "grid" ? "wrap" : "nowrap";

            var sb = new StringBuilder();
            sb.AppendLine(".command-panel {");
            sb.AppendLine($"    flex-direction: {flexDirection};");
            sb.AppendLine($"    flex-wrap: {flexWrap};");
            sb.AppendLine("    padding: 10px;");
            sb.AppendLine("    background-color: rgba(51, 51, 51, 0.8);");
            sb.AppendLine("    align-items: center;");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine(".command-button {");
            sb.AppendLine("    width: 100px;");
            sb.AppendLine("    height: 50px;");
            sb.AppendLine("    margin: 5px;");
            sb.AppendLine("    font-size: 14px;");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private void RegisterCommandBinding(Component uiCommandComp, string commandName, string buttonElementName, string commandTypeStr)
        {
            var commandType = ParseCommandType(commandTypeStr);

            var registerMethod = uiCommandComp.GetType().GetMethod("RegisterButton",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (registerMethod != null)
            {
                var cmdTypeEnum = uiCommandComp.GetType().GetNestedType("CommandType");
                if (cmdTypeEnum != null)
                {
                    var enumValue = Enum.Parse(cmdTypeEnum, commandType);
                    registerMethod.Invoke(uiCommandComp, new object[] { commandName, buttonElementName, enumValue, null });
                }
            }
        }

        #endregion

        #region Add Command

        private object AddCommand(Dictionary<string, object> payload)
        {
            var panelId = GetString(payload, "panelId");
            if (string.IsNullOrEmpty(panelId))
                throw new InvalidOperationException("panelId is required for addCommand.");

            var component = CodeGenHelper.FindComponentInSceneByField("panelId", panelId);
            if (component == null)
                throw new InvalidOperationException($"Command panel with ID '{panelId}' not found.");

            // Get tracker entry to find UXML path
            var tracker = GeneratedScriptTracker.Instance;
            var entry = tracker.FindByComponentId(panelId);

            if (payload.TryGetValue("commands", out var commandsObj) && commandsObj is List<object> commandsList)
            {
                var newButtons = new List<(string name, string label)>();

                foreach (var commandObj in commandsList)
                {
                    if (commandObj is Dictionary<string, object> commandDict)
                    {
                        var name = GetStringFromDict(commandDict, "name");
                        var label = GetStringFromDict(commandDict, "label") ?? name;
                        var commandTypeStr = GetStringFromDict(commandDict, "commandType") ?? "action";

                        RegisterCommandBinding(component, name, name, commandTypeStr);
                        newButtons.Add((name, label));
                    }
                }

                // Update UXML to add new buttons
                if (entry != null && newButtons.Count > 0)
                {
                    var vars = ScriptGenerator.DeserializeVariables(entry.variablesJson);
                    if (vars.TryGetValue("UXML_PATH", out var uxmlPathObj) && uxmlPathObj is string uxmlPath)
                    {
                        AppendButtonsToUXML(uxmlPath, newButtons);
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            return CreateSuccessResponse(("panelId", panelId), ("path", BuildGameObjectPath(component.gameObject)));
        }

        private void AppendButtonsToUXML(string uxmlPath, List<(string name, string label)> buttons)
        {
            if (!System.IO.File.Exists(uxmlPath)) return;

            var content = System.IO.File.ReadAllText(uxmlPath);
            var insertPoint = content.LastIndexOf("    </ui:VisualElement>");
            if (insertPoint < 0) return;

            var sb = new StringBuilder();
            foreach (var (name, label) in buttons)
            {
                sb.AppendLine($"        <ui:Button name=\"{name}\" text=\"{EscapeXml(label)}\" class=\"command-button\" />");
            }

            content = content.Insert(insertPoint, sb.ToString());
            System.IO.File.WriteAllText(uxmlPath, content, System.Text.Encoding.UTF8);
            AssetDatabase.ImportAsset(uxmlPath, ImportAssetOptions.ForceUpdate);
        }

        #endregion

        #region Inspect

        private object InspectCommandPanel(Dictionary<string, object> payload)
        {
            var panelId = GetString(payload, "panelId");
            if (string.IsNullOrEmpty(panelId))
                throw new InvalidOperationException("panelId is required for inspect.");

            var component = CodeGenHelper.FindComponentInSceneByField("panelId", panelId);
            if (component == null)
                return CreateSuccessResponse(("found", false), ("panelId", panelId));

            var so = new SerializedObject(component);

            var info = new Dictionary<string, object>
            {
                { "found", true },
                { "panelId", so.FindProperty("panelId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) }
            };

            var bindingsProp = so.FindProperty("commandBindings");
            if (bindingsProp != null && bindingsProp.isArray)
            {
                var commandNames = new List<string>();
                for (int i = 0; i < bindingsProp.arraySize; i++)
                {
                    var element = bindingsProp.GetArrayElementAtIndex(i);
                    var cmdNameProp = element.FindPropertyRelative("commandName");
                    if (cmdNameProp != null && !string.IsNullOrEmpty(cmdNameProp.stringValue))
                        commandNames.Add(cmdNameProp.stringValue);
                }
                info["commandCount"] = bindingsProp.arraySize;
                info["commands"] = commandNames;
            }

            // Add UXML/USS paths from tracker
            var tracker = GeneratedScriptTracker.Instance;
            var entry = tracker.FindByComponentId(panelId);
            if (entry != null)
            {
                var vars = ScriptGenerator.DeserializeVariables(entry.variablesJson);
                if (vars.TryGetValue("UXML_PATH", out var uxmlPath))
                    info["uxmlPath"] = uxmlPath;
                if (vars.TryGetValue("USS_PATH", out var ussPath))
                    info["ussPath"] = ussPath;
            }

            return CreateSuccessResponse(("commandPanel", info));
        }

        #endregion

        #region Delete

        private object DeleteCommandPanel(Dictionary<string, object> payload)
        {
            var panelId = GetString(payload, "panelId");
            if (string.IsNullOrEmpty(panelId))
                throw new InvalidOperationException("panelId is required for delete.");

            var component = CodeGenHelper.FindComponentInSceneByField("panelId", panelId);
            if (component == null)
                throw new InvalidOperationException($"Command panel with ID '{panelId}' not found.");

            var scene = component.gameObject.scene;

            // Delete UXML/USS assets
            UITKGenerationHelper.DeleteUIAssets(panelId);

            Undo.DestroyObjectImmediate(component.gameObject);
            ScriptGenerator.Delete(panelId);

            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(("panelId", panelId), ("deleted", true));
        }

        #endregion

        #region Helpers

        private string GetStringFromDict(Dictionary<string, object> dict, string key)
        {
            return dict.TryGetValue(key, out var value) ? value?.ToString() : null;
        }

        private string ParseCommandType(string typeStr)
        {
            return typeStr.ToLowerInvariant() switch
            {
                "move" => "Move",
                "jump" => "Jump",
                "action" => "Action",
                "look" => "Look",
                "custom" => "Custom",
                _ => "Action"
            };
        }

        private static string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        #endregion
    }
}
