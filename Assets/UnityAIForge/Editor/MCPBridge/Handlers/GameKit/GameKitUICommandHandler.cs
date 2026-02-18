using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit UI Command handler: create command panels with buttons.
    /// Uses code generation to produce standalone UICommand scripts with zero package dependency.
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
            var canvasPath = GetString(payload, "canvasPath");
            var layout = GetString(payload, "layout") ?? "vertical";

            if (string.IsNullOrEmpty(canvasPath))
            {
                throw new InvalidOperationException("canvasPath is required for createCommandPanel.");
            }

            var canvas = ResolveGameObject(canvasPath);

            // Create panel GameObject
            var panelGo = new GameObject(panelId, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(panelGo, "Create Command Panel");
            panelGo.transform.SetParent(canvas.transform, false);

            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 0);
            panelRect.pivot = new Vector2(0.5f, 0);
            panelRect.sizeDelta = new Vector2(0, 100);
            panelRect.anchoredPosition = Vector2.zero;

            // Add background image
            var panelImage = Undo.AddComponent<Image>(panelGo);
            panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Add layout group
            AddLayoutGroup(panelGo, layout);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(panelId, "UICommand");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "PANEL_ID", panelId }
            };

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                panelGo, "UICommand", panelId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate UICommand script.");
            }

            // Create command buttons if specified and the component was added
            if (payload.TryGetValue("commands", out var commandsObj) && commandsObj is List<object> commandsList)
            {
                var buttonSize = GetButtonSize(payload);

                // Try to find the component that was just added
                var uiCommandComp = CodeGenHelper.FindComponentByField(panelGo, "panelId", panelId);

                foreach (var commandObj in commandsList)
                {
                    if (commandObj is Dictionary<string, object> commandDict)
                    {
                        var name = GetStringFromDict(commandDict, "name");
                        var label = GetStringFromDict(commandDict, "label") ?? name;
                        var commandTypeStr = GetStringFromDict(commandDict, "commandType") ?? "action";

                        CreateCommandButton(panelGo, uiCommandComp, name, label, buttonSize, commandTypeStr);
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(panelGo.scene);

            result["panelId"] = panelId;
            result["path"] = BuildGameObjectPath(panelGo);

            return result;
        }

        private void AddLayoutGroup(GameObject go, string layout)
        {
            switch (layout.ToLowerInvariant())
            {
                case "horizontal":
                    var hLayout = Undo.AddComponent<HorizontalLayoutGroup>(go);
                    hLayout.spacing = 10;
                    hLayout.padding = new RectOffset(10, 10, 10, 10);
                    hLayout.childAlignment = TextAnchor.MiddleCenter;
                    hLayout.childControlWidth = false;
                    hLayout.childControlHeight = false;
                    break;

                case "vertical":
                    var vLayout = Undo.AddComponent<VerticalLayoutGroup>(go);
                    vLayout.spacing = 10;
                    vLayout.padding = new RectOffset(10, 10, 10, 10);
                    vLayout.childAlignment = TextAnchor.UpperCenter;
                    vLayout.childControlWidth = false;
                    vLayout.childControlHeight = false;
                    break;

                case "grid":
                    var gLayout = Undo.AddComponent<GridLayoutGroup>(go);
                    gLayout.spacing = new Vector2(10, 10);
                    gLayout.padding = new RectOffset(10, 10, 10, 10);
                    gLayout.cellSize = new Vector2(100, 50);
                    break;
            }
        }

        private Vector2 GetButtonSize(Dictionary<string, object> payload)
        {
            if (payload.TryGetValue("buttonSize", out var sizeObj) && sizeObj is Dictionary<string, object> sizeDict)
            {
                float width = sizeDict.TryGetValue("width", out var wObj) ? Convert.ToSingle(wObj) : 100f;
                float height = sizeDict.TryGetValue("height", out var hObj) ? Convert.ToSingle(hObj) : 50f;
                return new Vector2(width, height);
            }
            return new Vector2(100, 50);
        }

        private void CreateCommandButton(GameObject parent, Component uiCommandComp, string commandName,
            string label, Vector2 size, string commandTypeStr = "action")
        {
            // Create button GameObject
            var buttonGo = new GameObject(commandName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(buttonGo, "Create Command Button");
            buttonGo.transform.SetParent(parent.transform, false);

            var buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.sizeDelta = size;

            var buttonImage = Undo.AddComponent<Image>(buttonGo);
            buttonImage.color = Color.white;

            var button = Undo.AddComponent<Button>(buttonGo);

            // Create text child
            var textGo = new GameObject("Text", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(textGo, "Create Button Text");
            textGo.transform.SetParent(buttonGo.transform, false);

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var text = Undo.AddComponent<Text>(textGo);
            text.text = label;
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;

            // Register button with UICommand component if available
            if (uiCommandComp != null)
            {
                Undo.RecordObject(uiCommandComp, "Register Button");

                // Parse command type to the generated enum
                var commandType = ParseCommandType(commandTypeStr);

                // Use reflection to call RegisterButton on the generated component
                var registerMethod = uiCommandComp.GetType().GetMethod("RegisterButton",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (registerMethod != null)
                {
                    // Resolve the CommandType enum value from the generated type
                    var cmdTypeEnum = uiCommandComp.GetType().GetNestedType("CommandType");
                    if (cmdTypeEnum != null)
                    {
                        var enumValue = Enum.Parse(cmdTypeEnum, commandType);
                        registerMethod.Invoke(uiCommandComp, new object[] { commandName, button, enumValue, null });
                    }
                }
            }
        }

        #endregion

        #region Add Command

        private object AddCommand(Dictionary<string, object> payload)
        {
            var panelId = GetString(payload, "panelId");
            if (string.IsNullOrEmpty(panelId))
            {
                throw new InvalidOperationException("panelId is required for addCommand.");
            }

            var component = CodeGenHelper.FindComponentInSceneByField("panelId", panelId);
            if (component == null)
            {
                throw new InvalidOperationException($"Command panel with ID '{panelId}' not found.");
            }

            if (payload.TryGetValue("commands", out var commandsObj) && commandsObj is List<object> commandsList)
            {
                var buttonSize = GetButtonSize(payload);
                foreach (var commandObj in commandsList)
                {
                    if (commandObj is Dictionary<string, object> commandDict)
                    {
                        var name = GetStringFromDict(commandDict, "name");
                        var label = GetStringFromDict(commandDict, "label") ?? name;
                        var commandTypeStr = GetStringFromDict(commandDict, "commandType") ?? "action";
                        CreateCommandButton(component.gameObject, component, name, label, buttonSize, commandTypeStr);
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            return CreateSuccessResponse(("panelId", panelId), ("path", BuildGameObjectPath(component.gameObject)));
        }

        #endregion

        #region Inspect

        private object InspectCommandPanel(Dictionary<string, object> payload)
        {
            var panelId = GetString(payload, "panelId");
            if (string.IsNullOrEmpty(panelId))
            {
                throw new InvalidOperationException("panelId is required for inspect.");
            }

            var component = CodeGenHelper.FindComponentInSceneByField("panelId", panelId);
            if (component == null)
            {
                return CreateSuccessResponse(("found", false), ("panelId", panelId));
            }

            var so = new SerializedObject(component);

            var info = new Dictionary<string, object>
            {
                { "found", true },
                { "panelId", so.FindProperty("panelId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) }
            };

            // Read command bindings count if available
            var bindingsProp = so.FindProperty("commandBindings");
            if (bindingsProp != null && bindingsProp.isArray)
            {
                var commandNames = new List<string>();
                for (int i = 0; i < bindingsProp.arraySize; i++)
                {
                    var element = bindingsProp.GetArrayElementAtIndex(i);
                    var cmdNameProp = element.FindPropertyRelative("commandName");
                    if (cmdNameProp != null && !string.IsNullOrEmpty(cmdNameProp.stringValue))
                    {
                        commandNames.Add(cmdNameProp.stringValue);
                    }
                }
                info["commandCount"] = bindingsProp.arraySize;
                info["commands"] = commandNames;
            }

            return CreateSuccessResponse(("commandPanel", info));
        }

        #endregion

        #region Delete

        private object DeleteCommandPanel(Dictionary<string, object> payload)
        {
            var panelId = GetString(payload, "panelId");
            if (string.IsNullOrEmpty(panelId))
            {
                throw new InvalidOperationException("panelId is required for delete.");
            }

            var component = CodeGenHelper.FindComponentInSceneByField("panelId", panelId);
            if (component == null)
            {
                throw new InvalidOperationException($"Command panel with ID '{panelId}' not found.");
            }

            var scene = component.gameObject.scene;
            Undo.DestroyObjectImmediate(component.gameObject);

            // Clean up the generated script from tracker
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

        #endregion
    }
}
