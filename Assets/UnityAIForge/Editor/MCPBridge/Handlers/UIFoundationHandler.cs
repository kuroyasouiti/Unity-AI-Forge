using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MCP.Editor.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Mid-level UI foundation: create Canvas, Panel, Button, Text, Image, InputField with presets.
    /// </summary>
    public class UIFoundationHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "createCanvas",
            "createPanel",
            "createButton",
            "createText",
            "createImage",
            "createInputField",
            "inspect",
        };

        public override string Category => "uiFoundation";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "createCanvas" => CreateCanvas(payload),
                "createPanel" => CreatePanel(payload),
                "createButton" => CreateButton(payload),
                "createText" => CreateText(payload),
                "createImage" => CreateImage(payload),
                "createInputField" => CreateInputField(payload),
                "inspect" => InspectUI(payload),
                _ => throw new InvalidOperationException($"Unsupported UI foundation operation: {operation}"),
            };
        }

        #region Create Canvas

        private object CreateCanvas(Dictionary<string, object> payload)
        {
            var name = GetString(payload, "name") ?? "Canvas";
            var renderMode = GetString(payload, "renderMode")?.ToLowerInvariant() ?? "screenspaceoverlay";
            var sortingOrder = GetInt(payload, "sortingOrder", 0);

            var canvasGo = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");

            var canvas = Undo.AddComponent<Canvas>(canvasGo);
            var scaler = Undo.AddComponent<CanvasScaler>(canvasGo);
            var raycaster = Undo.AddComponent<GraphicRaycaster>(canvasGo);

            // Set render mode
            switch (renderMode)
            {
                case "screenspaceoverlay":
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    break;
                case "screenspacecamera":
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    break;
                case "worldspace":
                    canvas.renderMode = RenderMode.WorldSpace;
                    break;
            }

            canvas.sortingOrder = sortingOrder;

            // Configure scaler for UI scaling
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Set parent if specified
            var parentPath = GetString(payload, "parentPath");
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = ResolveGameObject(parentPath);
                canvasGo.transform.SetParent(parent.transform, false);
            }

            EditorSceneManager.MarkSceneDirty(canvasGo.scene);
            return CreateSuccessResponse(("path", BuildGameObjectPath(canvasGo)));
        }

        #endregion

        #region Create Panel

        private object CreatePanel(Dictionary<string, object> payload)
        {
            var name = GetString(payload, "name") ?? "Panel";
            var parentPath = GetString(payload, "parentPath");

            if (string.IsNullOrEmpty(parentPath))
            {
                throw new InvalidOperationException("parentPath is required for createPanel.");
            }

            var parent = ResolveGameObject(parentPath);
            var panelGo = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(panelGo, "Create Panel");
            panelGo.transform.SetParent(parent.transform, false);

            var rectTransform = panelGo.GetComponent<RectTransform>();
            var image = Undo.AddComponent<Image>(panelGo);

            // Apply anchor preset
            ApplyAnchorPreset(rectTransform, payload);

            // Set size
            var width = GetFloatFromPayload(payload, "width", 400f);
            var height = GetFloatFromPayload(payload, "height", 300f);
            rectTransform.sizeDelta = new Vector2(width, height);

            // Set color
            if (payload.TryGetValue("color", out var colorObj) && colorObj is Dictionary<string, object> colorDict)
            {
                image.color = GetColorFromDict(colorDict, new Color(1, 1, 1, 0.392f));
            }
            else
            {
                image.color = new Color(1, 1, 1, 0.392f); // Default semi-transparent white
            }

            EditorSceneManager.MarkSceneDirty(panelGo.scene);
            return CreateSuccessResponse(("path", BuildGameObjectPath(panelGo)));
        }

        #endregion

        #region Create Button

        private object CreateButton(Dictionary<string, object> payload)
        {
            var name = GetString(payload, "name") ?? "Button";
            var parentPath = GetString(payload, "parentPath");

            if (string.IsNullOrEmpty(parentPath))
            {
                throw new InvalidOperationException("parentPath is required for createButton.");
            }

            var parent = ResolveGameObject(parentPath);
            var buttonGo = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(buttonGo, "Create Button");
            buttonGo.transform.SetParent(parent.transform, false);

            var rectTransform = buttonGo.GetComponent<RectTransform>();
            var image = Undo.AddComponent<Image>(buttonGo);
            var button = Undo.AddComponent<Button>(buttonGo);

            // Apply anchor preset
            ApplyAnchorPreset(rectTransform, payload);

            // Set size
            var width = GetFloatFromPayload(payload, "width", 160f);
            var height = GetFloatFromPayload(payload, "height", 30f);
            rectTransform.sizeDelta = new Vector2(width, height);

            // Set color
            if (payload.TryGetValue("color", out var colorObj) && colorObj is Dictionary<string, object> colorDict)
            {
                image.color = GetColorFromDict(colorDict, Color.white);
            }

            // Load sprite if specified
            var spritePath = GetString(payload, "spritePath");
            if (!string.IsNullOrEmpty(spritePath))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite != null)
                {
                    image.sprite = sprite;
                }
            }

            // Create text child
            var textGo = new GameObject("Text", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(textGo, "Create Button Text");
            textGo.transform.SetParent(buttonGo.transform, false);

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            // Try TextMeshPro first, fallback to legacy Text
            if (TryAddTextMeshPro(textGo, payload))
            {
                // TextMeshPro added successfully
            }
            else
            {
                var text = Undo.AddComponent<Text>(textGo);
                text.text = GetString(payload, "text") ?? "Button";
                text.fontSize = GetInt(payload, "fontSize", 14);
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.black;
            }

            EditorSceneManager.MarkSceneDirty(buttonGo.scene);
            return CreateSuccessResponse(("path", BuildGameObjectPath(buttonGo)));
        }

        #endregion

        #region Create Text

        private object CreateText(Dictionary<string, object> payload)
        {
            var name = GetString(payload, "name") ?? "Text";
            var parentPath = GetString(payload, "parentPath");

            if (string.IsNullOrEmpty(parentPath))
            {
                throw new InvalidOperationException("parentPath is required for createText.");
            }

            var parent = ResolveGameObject(parentPath);
            var textGo = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(textGo, "Create Text");
            textGo.transform.SetParent(parent.transform, false);

            var rectTransform = textGo.GetComponent<RectTransform>();

            // Apply anchor preset
            ApplyAnchorPreset(rectTransform, payload);

            // Set size
            var width = GetFloatFromPayload(payload, "width", 160f);
            var height = GetFloatFromPayload(payload, "height", 30f);
            rectTransform.sizeDelta = new Vector2(width, height);

            // Try TextMeshPro first, fallback to legacy Text
            if (TryAddTextMeshPro(textGo, payload))
            {
                // TextMeshPro added successfully
            }
            else
            {
                var text = Undo.AddComponent<Text>(textGo);
                text.text = GetString(payload, "text") ?? "New Text";
                text.fontSize = GetInt(payload, "fontSize", 14);
                text.alignment = TextAnchor.MiddleCenter;

                if (payload.TryGetValue("color", out var colorObj) && colorObj is Dictionary<string, object> colorDict)
                {
                    text.color = GetColorFromDict(colorDict, Color.black);
                }
                else
                {
                    text.color = Color.black;
                }
            }

            EditorSceneManager.MarkSceneDirty(textGo.scene);
            return CreateSuccessResponse(("path", BuildGameObjectPath(textGo)));
        }

        #endregion

        #region Create Image

        private object CreateImage(Dictionary<string, object> payload)
        {
            var name = GetString(payload, "name") ?? "Image";
            var parentPath = GetString(payload, "parentPath");

            if (string.IsNullOrEmpty(parentPath))
            {
                throw new InvalidOperationException("parentPath is required for createImage.");
            }

            var parent = ResolveGameObject(parentPath);
            var imageGo = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(imageGo, "Create Image");
            imageGo.transform.SetParent(parent.transform, false);

            var rectTransform = imageGo.GetComponent<RectTransform>();
            var image = Undo.AddComponent<Image>(imageGo);

            // Apply anchor preset
            ApplyAnchorPreset(rectTransform, payload);

            // Set size
            var width = GetFloatFromPayload(payload, "width", 100f);
            var height = GetFloatFromPayload(payload, "height", 100f);
            rectTransform.sizeDelta = new Vector2(width, height);

            // Set color
            if (payload.TryGetValue("color", out var colorObj) && colorObj is Dictionary<string, object> colorDict)
            {
                image.color = GetColorFromDict(colorDict, Color.white);
            }

            // Load sprite if specified
            var spritePath = GetString(payload, "spritePath");
            if (!string.IsNullOrEmpty(spritePath))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite != null)
                {
                    image.sprite = sprite;
                }
            }

            EditorSceneManager.MarkSceneDirty(imageGo.scene);
            return CreateSuccessResponse(("path", BuildGameObjectPath(imageGo)));
        }

        #endregion

        #region Create InputField

        private object CreateInputField(Dictionary<string, object> payload)
        {
            var name = GetString(payload, "name") ?? "InputField";
            var parentPath = GetString(payload, "parentPath");

            if (string.IsNullOrEmpty(parentPath))
            {
                throw new InvalidOperationException("parentPath is required for createInputField.");
            }

            var parent = ResolveGameObject(parentPath);
            var inputGo = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(inputGo, "Create InputField");
            inputGo.transform.SetParent(parent.transform, false);

            var rectTransform = inputGo.GetComponent<RectTransform>();
            var image = Undo.AddComponent<Image>(inputGo);

            // Apply anchor preset
            ApplyAnchorPreset(rectTransform, payload);

            // Set size
            var width = GetFloatFromPayload(payload, "width", 160f);
            var height = GetFloatFromPayload(payload, "height", 30f);
            rectTransform.sizeDelta = new Vector2(width, height);

            image.color = Color.white;

            // Try TextMeshPro InputField first, fallback to legacy InputField
            if (TryAddTMPInputField(inputGo, payload))
            {
                // TMP InputField added successfully
            }
            else
            {
                var inputField = Undo.AddComponent<InputField>(inputGo);

                // Create text child
                var textGo = new GameObject("Text", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(textGo, "Create InputField Text");
                textGo.transform.SetParent(inputGo.transform, false);

                var textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = new Vector2(-10, 0);

                var text = Undo.AddComponent<Text>(textGo);
                text.supportRichText = false;
                text.fontSize = GetInt(payload, "fontSize", 14);
                text.color = Color.black;

                inputField.textComponent = text;

                var placeholder = GetString(payload, "placeholder");
                if (!string.IsNullOrEmpty(placeholder))
                {
                    var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
                    Undo.RegisterCreatedObjectUndo(placeholderGo, "Create Placeholder");
                    placeholderGo.transform.SetParent(inputGo.transform, false);

                    var placeholderRect = placeholderGo.GetComponent<RectTransform>();
                    placeholderRect.anchorMin = Vector2.zero;
                    placeholderRect.anchorMax = Vector2.one;
                    placeholderRect.sizeDelta = new Vector2(-10, 0);

                    var placeholderText = Undo.AddComponent<Text>(placeholderGo);
                    placeholderText.text = placeholder;
                    placeholderText.fontSize = GetInt(payload, "fontSize", 14);
                    placeholderText.fontStyle = FontStyle.Italic;
                    placeholderText.color = new Color(0, 0, 0, 0.5f);

                    inputField.placeholder = placeholderText;
                }
            }

            EditorSceneManager.MarkSceneDirty(inputGo.scene);
            return CreateSuccessResponse(("path", BuildGameObjectPath(inputGo)));
        }

        #endregion

        #region Inspect

        private object InspectUI(Dictionary<string, object> payload)
        {
            var parentPath = GetString(payload, "parentPath");
            if (string.IsNullOrEmpty(parentPath))
            {
                throw new InvalidOperationException("parentPath is required for inspect.");
            }

            var go = ResolveGameObject(parentPath);
            var info = new Dictionary<string, object>
            {
                { "path", BuildGameObjectPath(go) },
                { "name", go.name }
            };

            var canvas = go.GetComponent<Canvas>();
            if (canvas != null)
            {
                info["type"] = "Canvas";
                info["renderMode"] = canvas.renderMode.ToString();
                info["sortingOrder"] = canvas.sortingOrder;
            }

            var image = go.GetComponent<Image>();
            if (image != null)
            {
                info["hasImage"] = true;
                info["color"] = new Dictionary<string, object>
                {
                    { "r", image.color.r },
                    { "g", image.color.g },
                    { "b", image.color.b },
                    { "a", image.color.a }
                };
            }

            var button = go.GetComponent<Button>();
            if (button != null)
            {
                info["type"] = "Button";
            }

            var text = go.GetComponent<Text>();
            if (text != null)
            {
                info["type"] = "Text";
                info["text"] = text.text;
                info["fontSize"] = text.fontSize;
            }

            // Check for TextMeshProUGUI via reflection
            var tmpTextType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (tmpTextType != null)
            {
                var tmpText = go.GetComponent(tmpTextType);
                if (tmpText != null)
                {
                    info["type"] = "TextMeshPro";
                    
                    var textProp = tmpTextType.GetProperty("text");
                    if (textProp != null)
                        info["text"] = textProp.GetValue(tmpText);
                    
                    var fontSizeProp = tmpTextType.GetProperty("fontSize");
                    if (fontSizeProp != null)
                        info["fontSize"] = fontSizeProp.GetValue(tmpText);
                }
            }

            return CreateSuccessResponse(("ui", info));
        }

        #endregion

        #region Helpers

        private void ApplyAnchorPreset(RectTransform rectTransform, Dictionary<string, object> payload)
        {
            var preset = GetString(payload, "anchorPreset")?.ToLowerInvariant() ?? "middlecenter";

            switch (preset)
            {
                case "topleft":
                    rectTransform.anchorMin = new Vector2(0, 1);
                    rectTransform.anchorMax = new Vector2(0, 1);
                    rectTransform.pivot = new Vector2(0, 1);
                    break;
                case "topcenter":
                    rectTransform.anchorMin = new Vector2(0.5f, 1);
                    rectTransform.anchorMax = new Vector2(0.5f, 1);
                    rectTransform.pivot = new Vector2(0.5f, 1);
                    break;
                case "topright":
                    rectTransform.anchorMin = new Vector2(1, 1);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.pivot = new Vector2(1, 1);
                    break;
                case "middleleft":
                    rectTransform.anchorMin = new Vector2(0, 0.5f);
                    rectTransform.anchorMax = new Vector2(0, 0.5f);
                    rectTransform.pivot = new Vector2(0, 0.5f);
                    break;
                case "middlecenter":
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    break;
                case "middleright":
                    rectTransform.anchorMin = new Vector2(1, 0.5f);
                    rectTransform.anchorMax = new Vector2(1, 0.5f);
                    rectTransform.pivot = new Vector2(1, 0.5f);
                    break;
                case "bottomleft":
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(0, 0);
                    rectTransform.pivot = new Vector2(0, 0);
                    break;
                case "bottomcenter":
                    rectTransform.anchorMin = new Vector2(0.5f, 0);
                    rectTransform.anchorMax = new Vector2(0.5f, 0);
                    rectTransform.pivot = new Vector2(0.5f, 0);
                    break;
                case "bottomright":
                    rectTransform.anchorMin = new Vector2(1, 0);
                    rectTransform.anchorMax = new Vector2(1, 0);
                    rectTransform.pivot = new Vector2(1, 0);
                    break;
                case "stretchall":
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.sizeDelta = Vector2.zero;
                    break;
            }
        }

        private bool TryAddTextMeshPro(GameObject go, Dictionary<string, object> payload)
        {
            var tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (tmpType == null) return false;

            var tmp = Undo.AddComponent(go, tmpType);
            if (tmp == null) return false;

            // Use reflection to set properties
            var textProp = tmpType.GetProperty("text");
            if (textProp != null)
                textProp.SetValue(tmp, GetString(payload, "text") ?? "New Text");

            var fontSizeProp = tmpType.GetProperty("fontSize");
            if (fontSizeProp != null)
                fontSizeProp.SetValue(tmp, (float)GetInt(payload, "fontSize", 14));

            // Set alignment via reflection
            var alignmentProp = tmpType.GetProperty("alignment");
            if (alignmentProp != null)
            {
                var alignmentEnumType = Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
                if (alignmentEnumType != null)
                {
                    var centerValue = Enum.Parse(alignmentEnumType, "Center");
                    alignmentProp.SetValue(tmp, centerValue);
                }
            }

            var colorProp = tmpType.GetProperty("color");
            if (colorProp != null)
            {
                if (payload.TryGetValue("color", out var colorObj) && colorObj is Dictionary<string, object> colorDict)
                {
                    colorProp.SetValue(tmp, GetColorFromDict(colorDict, Color.black));
                }
                else
                {
                    colorProp.SetValue(tmp, Color.black);
                }
            }

            return true;
        }

        private bool TryAddTMPInputField(GameObject go, Dictionary<string, object> payload)
        {
            var tmpInputType = Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
            if (tmpInputType == null) return false;

            var tmpInput = Undo.AddComponent(go, tmpInputType);
            if (tmpInput == null) return false;

            // Create text area
            var textAreaGo = new GameObject("Text Area", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(textAreaGo, "Create Text Area");
            textAreaGo.transform.SetParent(go.transform, false);

            var textAreaRect = textAreaGo.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.sizeDelta = new Vector2(-10, 0);

            var textAreaMask = Undo.AddComponent<RectMask2D>(textAreaGo);

            // Create text child
            var textGo = new GameObject("Text", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(textGo, "Create TMP Text");
            textGo.transform.SetParent(textAreaGo.transform, false);

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            // Add TextMeshProUGUI via reflection
            var tmpTextType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (tmpTextType == null) return false;

            var tmpText = Undo.AddComponent(textGo, tmpTextType);
            if (tmpText == null) return false;

            // Set text properties via reflection
            var fontSizeProp = tmpTextType.GetProperty("fontSize");
            if (fontSizeProp != null)
                fontSizeProp.SetValue(tmpText, (float)GetInt(payload, "fontSize", 14));

            var colorProp = tmpTextType.GetProperty("color");
            if (colorProp != null)
                colorProp.SetValue(tmpText, Color.black);

            // Set input field properties via reflection
            var textViewportProp = tmpInputType.GetProperty("textViewport");
            if (textViewportProp != null)
                textViewportProp.SetValue(tmpInput, textAreaRect);

            var textComponentProp = tmpInputType.GetProperty("textComponent");
            if (textComponentProp != null)
                textComponentProp.SetValue(tmpInput, tmpText);

            var placeholder = GetString(payload, "placeholder");
            if (!string.IsNullOrEmpty(placeholder))
            {
                var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(placeholderGo, "Create TMP Placeholder");
                placeholderGo.transform.SetParent(textAreaGo.transform, false);

                var placeholderRect = placeholderGo.GetComponent<RectTransform>();
                placeholderRect.anchorMin = Vector2.zero;
                placeholderRect.anchorMax = Vector2.one;
                placeholderRect.sizeDelta = Vector2.zero;

                var placeholderText = Undo.AddComponent(placeholderGo, tmpTextType);
                if (placeholderText != null)
                {
                    var textProp = tmpTextType.GetProperty("text");
                    if (textProp != null)
                        textProp.SetValue(placeholderText, placeholder);

                    if (fontSizeProp != null)
                        fontSizeProp.SetValue(placeholderText, (float)GetInt(payload, "fontSize", 14));

                    // Set font style via reflection
                    var fontStyleProp = tmpTextType.GetProperty("fontStyle");
                    if (fontStyleProp != null)
                    {
                        var fontStylesType = Type.GetType("TMPro.FontStyles, Unity.TextMeshPro");
                        if (fontStylesType != null)
                        {
                            var italicValue = Enum.Parse(fontStylesType, "Italic");
                            fontStyleProp.SetValue(placeholderText, italicValue);
                        }
                    }

                    if (colorProp != null)
                        colorProp.SetValue(placeholderText, new Color(0, 0, 0, 0.5f));

                    var placeholderProp = tmpInputType.GetProperty("placeholder");
                    if (placeholderProp != null)
                        placeholderProp.SetValue(tmpInput, placeholderText);
                }
            }

            return true;
        }

        private Color GetColorFromDict(Dictionary<string, object> dict, Color fallback)
        {
            float r = dict.TryGetValue("r", out var rObj) ? Convert.ToSingle(rObj) : fallback.r;
            float g = dict.TryGetValue("g", out var gObj) ? Convert.ToSingle(gObj) : fallback.g;
            float b = dict.TryGetValue("b", out var bObj) ? Convert.ToSingle(bObj) : fallback.b;
            float a = dict.TryGetValue("a", out var aObj) ? Convert.ToSingle(aObj) : fallback.a;
            return new Color(r, g, b, a);
        }

        private float GetFloatFromPayload(Dictionary<string, object> payload, string key, float defaultValue)
        {
            if (!payload.TryGetValue(key, out var value) || value == null)
            {
                return defaultValue;
            }
            return Convert.ToSingle(value);
        }

        // Note: Using 'new' to explicitly hide inherited method for local use
        private new int GetInt(Dictionary<string, object> payload, string key, int defaultValue)
        {
            if (!payload.TryGetValue(key, out var value) || value == null)
            {
                return defaultValue;
            }
            return Convert.ToInt32(value);
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

