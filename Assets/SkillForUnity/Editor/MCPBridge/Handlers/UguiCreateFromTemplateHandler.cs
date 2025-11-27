using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using MCP.Editor.Interfaces;
using MCP.Editor.Helpers.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// UI要素をテンプレートから作成するコマンドハンドラー。
    /// Button, Text, Image, Panel, ScrollView, InputField, Slider, Toggle, Dropdownをサポート。
    /// </summary>
    public class UguiCreateFromTemplateHandler : BaseCommandHandler
    {
        public override string Category => "uguiCreateFromTemplate";
        
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "create" // メイン操作（テンプレートタイプによって分岐）
        };
        
        public UguiCreateFromTemplateHandler() : base()
        {
        }
        
        public UguiCreateFromTemplateHandler(
            IPayloadValidator validator,
            IGameObjectResolver gameObjectResolver,
            IAssetResolver assetResolver,
            ITypeResolver typeResolver)
            : base(validator, gameObjectResolver, assetResolver, typeResolver)
        {
        }
        
        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            var template = GetString(payload, "template");
            if (string.IsNullOrEmpty(template))
            {
                throw new InvalidOperationException("template is required");
            }
            
            Debug.Log($"[UguiCreateFromTemplateHandler] Creating template: {template}");
            
            // Get parent or find first Canvas
            var parentPath = GetString(payload, "parentPath");
            GameObject parent = null;
            
            if (!string.IsNullOrEmpty(parentPath))
            {
                parent = GameObjectResolver.Resolve(parentPath);
            }
            else
            {
                // Find first Canvas in the scene
                var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    parent = canvas.gameObject;
                }
                else
                {
                    throw new InvalidOperationException("No Canvas found in scene. Please specify parentPath or create a Canvas first.");
                }
            }
            
            // Verify parent is under a Canvas
            if (parent.GetComponentInParent<Canvas>() == null)
            {
                throw new InvalidOperationException("Parent must be under a Canvas");
            }
            
            // Get name or use template as default
            var name = GetString(payload, "name");
            if (string.IsNullOrEmpty(name))
            {
                name = template;
            }
            
            // Create the GameObject based on template
            GameObject go = template switch
            {
                "Button" => CreateButtonTemplate(name, parent, payload),
                "Text" => CreateTextTemplate(name, parent, payload),
                "Image" => CreateImageTemplate(name, parent, payload),
                "RawImage" => CreateRawImageTemplate(name, parent, payload),
                "Panel" => CreatePanelTemplate(name, parent, payload),
                "ScrollView" => CreateScrollViewTemplate(name, parent, payload),
                "InputField" => CreateInputFieldTemplate(name, parent, payload),
                "Slider" => CreateSliderTemplate(name, parent, payload),
                "Toggle" => CreateToggleTemplate(name, parent, payload),
                "Dropdown" => CreateDropdownTemplate(name, parent, payload),
                _ => throw new InvalidOperationException($"Unknown template: {template}")
            };
            
            Undo.RegisterCreatedObjectUndo(go, $"Create {template}");
            Selection.activeGameObject = go;
            
            Debug.Log($"[UguiCreateFromTemplateHandler] Created {template}: {GetGameObjectPath(go)}");
            
            return new Dictionary<string, object>
            {
                ["template"] = template,
                ["gameObjectPath"] = GetGameObjectPath(go),
                ["name"] = go.name,
            };
        }
        
        #region Template Creation Methods
        
        private GameObject CreateButtonTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            
            var image = go.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 1f);
            
            var button = go.AddComponent<Button>();
            button.interactable = GetBool(payload, "interactable", true);
            
            // Create Text child
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            CreateTextComponent(textGo, payload, "Button", 14);
            
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 160, 30);
            
            return go;
        }
        
        private GameObject CreateTextTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            
            CreateTextComponent(go, payload, "New Text", 14);
            
            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 160, 30);
            
            return go;
        }
        
        private GameObject CreateImageTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            
            var image = go.AddComponent<Image>();
            image.color = Color.white;
            
            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 100, 100);
            
            return go;
        }
        
        private GameObject CreateRawImageTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            
            var rawImage = go.AddComponent<RawImage>();
            rawImage.color = Color.white;
            
            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 100, 100);
            
            return go;
        }
        
        private GameObject CreatePanelTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            
            var image = go.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.392f);
            
            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 200, 200);
            
            return go;
        }
        
        private GameObject CreateScrollViewTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            // Create main ScrollView GameObject
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            
            var scrollRect = go.AddComponent<ScrollRect>();
            var image = go.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 1f);
            
            // Create Viewport
            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(go.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewport.AddComponent<Mask>();
            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            
            // Create Content
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 300);
            
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            
            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 200, 200);
            
            return go;
        }
        
        private GameObject CreateInputFieldTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            
            var image = go.AddComponent<Image>();
            image.color = Color.white;
            
            var inputField = go.AddComponent<InputField>();
            inputField.interactable = GetBool(payload, "interactable", true);
            
            // Create Text child
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.text = GetString(payload, "text") ?? "";
            text.fontSize = GetInt(payload, "fontSize", 14);
            text.color = Color.black;
            text.supportRichText = false;
            
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 6);
            textRect.offsetMax = new Vector2(-10, -7);
            
            // Create Placeholder child
            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
            placeholderGo.transform.SetParent(go.transform, false);
            var placeholder = placeholderGo.AddComponent<Text>();
            placeholder.text = "Enter text...";
            placeholder.fontSize = GetInt(payload, "fontSize", 14);
            placeholder.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            placeholder.fontStyle = FontStyle.Italic;
            
            var placeholderRect = placeholderGo.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(10, 6);
            placeholderRect.offsetMax = new Vector2(-10, -7);
            
            inputField.textComponent = text;
            inputField.placeholder = placeholder;
            
            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 160, 30);
            
            return go;
        }
        
        private GameObject CreateSliderTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            
            var slider = go.AddComponent<Slider>();
            slider.interactable = GetBool(payload, "interactable", true);
            
            // Create Background
            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(go.transform, false);
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.sizeDelta = new Vector2(0, 0);
            
            // Create Fill Area
            var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGo.transform.SetParent(go.transform, false);
            var fillAreaRect = fillAreaGo.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);
            
            // Create Fill
            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillImage = fillGo.AddComponent<Image>();
            fillImage.color = new Color(0.5f, 0.8f, 1f, 1f);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.sizeDelta = new Vector2(10, 0);
            
            // Create Handle Slide Area
            var handleAreaGo = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleAreaGo.transform.SetParent(go.transform, false);
            var handleAreaRect = handleAreaGo.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = new Vector2(0, 0);
            handleAreaRect.anchorMax = new Vector2(1, 1);
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);
            
            // Create Handle
            var handleGo = new GameObject("Handle", typeof(RectTransform));
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            var handleImage = handleGo.AddComponent<Image>();
            handleImage.color = Color.white;
            var handleRect = handleGo.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);
            
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            
            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 160, 20);
            
            return go;
        }
        
        private GameObject CreateToggleTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            
            var toggle = go.AddComponent<Toggle>();
            toggle.interactable = GetBool(payload, "interactable", true);
            
            // Create Background
            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(go.transform, false);
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = Color.white;
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.5f);
            bgRect.anchorMax = new Vector2(0, 0.5f);
            bgRect.anchoredPosition = new Vector2(10, 0);
            bgRect.sizeDelta = new Vector2(20, 20);
            
            // Create Checkmark
            var checkGo = new GameObject("Checkmark", typeof(RectTransform));
            checkGo.transform.SetParent(bgGo.transform, false);
            var checkImage = checkGo.AddComponent<Image>();
            checkImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);
            var checkRect = checkGo.GetComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.sizeDelta = Vector2.zero;
            
            // Create Label
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var label = labelGo.AddComponent<Text>();
            label.text = GetString(payload, "text") ?? "Toggle";
            label.fontSize = GetInt(payload, "fontSize", 14);
            label.color = Color.black;
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(23, 0);
            labelRect.offsetMax = new Vector2(0, 0);
            
            toggle.graphic = checkImage;
            toggle.targetGraphic = bgImage;
            
            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 160, 20);
            
            return go;
        }
        
        private GameObject CreateDropdownTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            
            var image = go.AddComponent<Image>();
            image.color = Color.white;
            
            var dropdown = go.AddComponent<Dropdown>();
            dropdown.interactable = GetBool(payload, "interactable", true);
            
            // Create Label
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var label = labelGo.AddComponent<Text>();
            label.text = GetString(payload, "text") ?? "Option A";
            label.fontSize = GetInt(payload, "fontSize", 14);
            label.color = Color.black;
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10, 6);
            labelRect.offsetMax = new Vector2(-25, -7);
            
            // Create Arrow
            var arrowGo = new GameObject("Arrow", typeof(RectTransform));
            arrowGo.transform.SetParent(go.transform, false);
            var arrow = arrowGo.AddComponent<Text>();
            arrow.text = "▼";
            arrow.fontSize = GetInt(payload, "fontSize", 14);
            arrow.color = Color.black;
            arrow.alignment = TextAnchor.MiddleCenter;
            var arrowRect = arrowGo.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0);
            arrowRect.anchorMax = new Vector2(1, 1);
            arrowRect.sizeDelta = new Vector2(20, 0);
            arrowRect.anchoredPosition = new Vector2(-15, 0);
            
            // Create Template (simplified version)
            var templateGo = new GameObject("Template", typeof(RectTransform));
            templateGo.transform.SetParent(go.transform, false);
            var templateRect = templateGo.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = new Vector2(0, 2);
            templateRect.sizeDelta = new Vector2(0, 150);
            templateGo.SetActive(false);
            
            dropdown.captionText = label;
            dropdown.template = templateRect;
            
            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 160, 30);
            
            return go;
        }
        
        #endregion
        
        #region Helper Methods
        
        private Component CreateTextComponent(GameObject textGo, Dictionary<string, object> payload, string defaultText, int defaultFontSize)
        {
            var useTextMeshPro = GetBool(payload, "useTextMeshPro", false);
            var textContent = GetString(payload, "text") ?? defaultText;
            var fontSize = GetInt(payload, "fontSize", defaultFontSize);
            
            if (useTextMeshPro)
            {
                // Try to use TextMeshPro (TMPro)
                var tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
                if (tmpType == null)
                {
                    Debug.LogWarning("[CreateTextComponent] TextMeshPro package not found. Falling back to UI.Text. Install TextMeshPro package to use TMP components.");
                }
                else
                {
                    var tmpComponent = textGo.AddComponent(tmpType);
                    
                    // Set text property
                    var textProp = tmpType.GetProperty("text");
                    if (textProp != null)
                    {
                        textProp.SetValue(tmpComponent, textContent);
                    }
                    
                    // Set fontSize property
                    var fontSizeProp = tmpType.GetProperty("fontSize");
                    if (fontSizeProp != null)
                    {
                        fontSizeProp.SetValue(tmpComponent, (float)fontSize);
                    }
                    
                    // Set alignment property
                    var alignmentProp = tmpType.GetProperty("alignment");
                    if (alignmentProp != null)
                    {
                        // TextAlignmentOptions.Center = 514
                        var alignmentType = Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
                        if (alignmentType != null)
                        {
                            alignmentProp.SetValue(tmpComponent, Enum.ToObject(alignmentType, 514));
                        }
                    }
                    
                    // Set color property
                    var colorProp = tmpType.GetProperty("color");
                    if (colorProp != null)
                    {
                        colorProp.SetValue(tmpComponent, Color.black);
                    }
                    
                    return tmpComponent;
                }
            }
            
            // Use standard UI.Text
            var text = textGo.AddComponent<Text>();
            text.text = textContent;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            return text;
        }
        
        private void ApplyCommonRectTransformSettings(RectTransform rectTransform, Dictionary<string, object> payload, float defaultWidth, float defaultHeight)
        {
            // Apply anchor preset
            var anchorPreset = GetString(payload, "anchorPreset") ?? "center";
            var presetPayload = new Dictionary<string, object>
            {
                ["preset"] = anchorPreset,
                ["preservePosition"] = false
            };
            RectTransformHelper.SetAnchorPreset(rectTransform, presetPayload);
            
            // Apply size (with validation to prevent negative values)
            var width = GetFloat(payload, "width", defaultWidth);
            var height = GetFloat(payload, "height", defaultHeight);
            
            if (width < 0)
            {
                Debug.LogWarning($"[ApplyCommonRectTransformSettings] Width cannot be negative. Clamping {width} to 0.");
                width = 0;
            }
            
            if (height < 0)
            {
                Debug.LogWarning($"[ApplyCommonRectTransformSettings] Height cannot be negative. Clamping {height} to 0.");
                height = 0;
            }
            
            rectTransform.sizeDelta = new Vector2(width, height);
            
            // Apply position
            var posX = GetFloat(payload, "positionX", 0f);
            var posY = GetFloat(payload, "positionY", 0f);
            rectTransform.anchoredPosition = new Vector2(posX, posY);
        }
        
        private string GetGameObjectPath(GameObject go)
        {
            string path = go.name;
            Transform parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
        
        #endregion
        
        protected override bool RequiresCompilationWait(string operation)
        {
            // Template creation requires compilation wait
            return true;
        }
    }
}

