using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Mid-level UI hierarchy handler: create complex UI hierarchies declaratively
    /// and manage UI state (show/hide). For navigation, use UINavigationHandler.
    /// </summary>
    public class UIHierarchyHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create",
            "clone",
            "inspect",
            "delete",
            "show",
            "hide",
            "toggle",
        };

        public override string Category => "uiHierarchy";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateHierarchy(payload),
                "clone" => CloneHierarchy(payload),
                "inspect" => InspectHierarchy(payload),
                "delete" => DeleteHierarchy(payload),
                "show" => SetVisibility(payload, true),
                "hide" => SetVisibility(payload, false),
                "toggle" => ToggleVisibility(payload),
                _ => throw new InvalidOperationException($"Unsupported UI hierarchy operation: {operation}"),
            };
        }

        #region Create Hierarchy

        private object CreateHierarchy(Dictionary<string, object> payload)
        {
            var parentPath = GetString(payload, "parentPath");
            if (string.IsNullOrEmpty(parentPath))
            {
                throw new InvalidOperationException("parentPath is required for create operation.");
            }

            var parent = ResolveGameObject(parentPath);

            // Verify parent is under a Canvas
            var canvas = parent.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                throw new InvalidOperationException($"Parent '{parentPath}' must be under a Canvas.");
            }

            if (!payload.TryGetValue("structure", out var structureObj) || structureObj is not Dictionary<string, object> structure)
            {
                throw new InvalidOperationException("structure is required for create operation.");
            }

            var createdObjects = new List<string>();
            var rootGo = CreateUIElement(structure, parent.transform, createdObjects);

            EditorSceneManager.MarkSceneDirty(rootGo.scene);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["rootPath"] = BuildGameObjectPath(rootGo),
                ["createdCount"] = createdObjects.Count,
                ["createdObjects"] = createdObjects
            };
        }

        private GameObject CreateUIElement(Dictionary<string, object> element, Transform parent, List<string> createdObjects)
        {
            var type = GetString(element, "type")?.ToLowerInvariant() ?? "panel";
            var name = GetString(element, "name") ?? $"UI_{type}_{Guid.NewGuid().ToString().Substring(0, 4)}";

            GameObject go;

            switch (type)
            {
                case "panel":
                    go = CreatePanel(element, name);
                    break;
                case "button":
                    go = CreateButton(element, name);
                    break;
                case "text":
                    go = CreateText(element, name);
                    break;
                case "image":
                    go = CreateImage(element, name);
                    break;
                case "inputfield":
                    go = CreateInputField(element, name);
                    break;
                case "scrollview":
                    go = CreateScrollView(element, name, createdObjects);
                    break;
                case "toggle":
                    go = CreateToggle(element, name);
                    break;
                case "slider":
                    go = CreateSlider(element, name);
                    break;
                case "dropdown":
                    go = CreateDropdown(element, name);
                    break;
                default:
                    go = CreatePanel(element, name);
                    break;
            }

            Undo.RegisterCreatedObjectUndo(go, $"Create UI {type}");
            go.transform.SetParent(parent, false);

            // Apply common properties
            ApplyCommonProperties(go, element);

            // Apply layout if specified
            ApplyLayout(go, element);

            createdObjects.Add(BuildGameObjectPath(go));

            // Process children (skip for scrollview as it handles its own children)
            if (type != "scrollview" && element.TryGetValue("children", out var childrenObj) && childrenObj is List<object> children)
            {
                foreach (var childObj in children)
                {
                    if (childObj is Dictionary<string, object> childElement)
                    {
                        CreateUIElement(childElement, go.transform, createdObjects);
                    }
                }
            }

            return go;
        }

        private GameObject CreatePanel(Dictionary<string, object> element, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var image = go.AddComponent<Image>();

            // Set default panel color
            if (element.TryGetValue("color", out var colorObj) && colorObj is Dictionary<string, object> colorDict)
            {
                image.color = GetColorFromDict(colorDict, new Color(1, 1, 1, 0.392f));
            }
            else
            {
                image.color = new Color(1, 1, 1, 0.392f);
            }

            // Add CanvasGroup for visibility control
            if (GetBool(element, "addCanvasGroup", false))
            {
                go.AddComponent<CanvasGroup>();
            }

            return go;
        }

        private GameObject CreateButton(Dictionary<string, object> element, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var image = go.AddComponent<Image>();
            var button = go.AddComponent<Button>();

            // Set button colors
            if (element.TryGetValue("color", out var colorObj) && colorObj is Dictionary<string, object> colorDict)
            {
                image.color = GetColorFromDict(colorDict, Color.white);
            }

            // Create text child
            var text = GetString(element, "text");
            if (!string.IsNullOrEmpty(text))
            {
                var textGo = new GameObject("Text", typeof(RectTransform));
                textGo.transform.SetParent(go.transform, false);

                var textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                // Try TextMeshPro first, fallback to legacy Text
                if (TryAddTextMeshPro(textGo, text, element))
                {
                    // TMP added successfully
                }
                else
                {
                    var textComponent = textGo.AddComponent<Text>();
                    textComponent.text = text;
                    textComponent.alignment = TextAnchor.MiddleCenter;
                    textComponent.color = Color.black;
                    textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                    var fontSize = GetInt(element, "fontSize", 14);
                    textComponent.fontSize = fontSize;
                }
            }

            return go;
        }

        private GameObject CreateText(Dictionary<string, object> element, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var text = GetString(element, "text") ?? "Text";

            // Try TextMeshPro first
            if (TryAddTextMeshPro(go, text, element))
            {
                // TMP added successfully
            }
            else
            {
                var textComponent = go.AddComponent<Text>();
                textComponent.text = text;
                textComponent.alignment = ParseTextAnchor(GetString(element, "alignment") ?? "MiddleCenter");

                if (element.TryGetValue("color", out var colorObj) && colorObj is Dictionary<string, object> colorDict)
                {
                    textComponent.color = GetColorFromDict(colorDict, Color.black);
                }
                else
                {
                    textComponent.color = Color.black;
                }

                textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                textComponent.fontSize = GetInt(element, "fontSize", 14);
            }

            return go;
        }

        private GameObject CreateImage(Dictionary<string, object> element, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var image = go.AddComponent<Image>();

            if (element.TryGetValue("color", out var colorObj) && colorObj is Dictionary<string, object> colorDict)
            {
                image.color = GetColorFromDict(colorDict, Color.white);
            }

            var spritePath = GetString(element, "spritePath");
            if (!string.IsNullOrEmpty(spritePath))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite != null)
                {
                    image.sprite = sprite;
                }
            }

            var raycastTarget = GetBool(element, "raycastTarget", true);
            image.raycastTarget = raycastTarget;

            return go;
        }

        private GameObject CreateInputField(Dictionary<string, object> element, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var image = go.AddComponent<Image>();
            image.color = Color.white;

            // Create text area
            var textAreaGo = new GameObject("Text Area", typeof(RectTransform));
            textAreaGo.transform.SetParent(go.transform, false);
            var textAreaRect = textAreaGo.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 6);
            textAreaRect.offsetMax = new Vector2(-10, -7);
            var rectMask = textAreaGo.AddComponent<RectMask2D>();

            // Create placeholder
            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
            placeholderGo.transform.SetParent(textAreaGo.transform, false);
            var placeholderRect = placeholderGo.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;

            var placeholderText = placeholderGo.AddComponent<Text>();
            placeholderText.text = GetString(element, "placeholder") ?? "Enter text...";
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontStyle = FontStyle.Italic;
            placeholderText.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            placeholderText.alignment = TextAnchor.MiddleLeft;

            // Create text
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(textAreaGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var textComponent = textGo.AddComponent<Text>();
            textComponent.text = "";
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.color = Color.black;
            textComponent.alignment = TextAnchor.MiddleLeft;
            textComponent.supportRichText = false;

            // Add InputField component
            var inputField = go.AddComponent<InputField>();
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderText;

            return go;
        }

        private GameObject CreateScrollView(Dictionary<string, object> element, string name, List<string> createdObjects)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var image = go.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            var scrollRect = go.AddComponent<ScrollRect>();

            // Create Viewport
            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            viewportGo.transform.SetParent(go.transform, false);
            var viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportGo.AddComponent<Image>().color = Color.clear;
            viewportGo.AddComponent<Mask>().showMaskGraphic = false;

            // Create Content
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            // Add ContentSizeFitter for auto-sizing
            var sizeFitter = contentGo.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Add VerticalLayoutGroup to Content by default
            var layoutGroup = contentGo.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 5;
            layoutGroup.padding = new RectOffset(5, 5, 5, 5);

            // Configure ScrollRect
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = GetBool(element, "horizontal", false);
            scrollRect.vertical = GetBool(element, "vertical", true);

            // Process children into Content
            if (element.TryGetValue("children", out var childrenObj) && childrenObj is List<object> children)
            {
                foreach (var childObj in children)
                {
                    if (childObj is Dictionary<string, object> childElement)
                    {
                        CreateUIElement(childElement, contentGo.transform, createdObjects);
                    }
                }
            }

            return go;
        }

        private GameObject CreateToggle(Dictionary<string, object> element, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var toggle = go.AddComponent<Toggle>();

            // Background
            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(go.transform, false);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.5f);
            bgRect.anchorMax = new Vector2(0, 0.5f);
            bgRect.pivot = new Vector2(0, 0.5f);
            bgRect.sizeDelta = new Vector2(20, 20);
            bgRect.anchoredPosition = Vector2.zero;
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = Color.white;

            // Checkmark
            var checkGo = new GameObject("Checkmark", typeof(RectTransform));
            checkGo.transform.SetParent(bgGo.transform, false);
            var checkRect = checkGo.GetComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.offsetMin = new Vector2(2, 2);
            checkRect.offsetMax = new Vector2(-2, -2);
            var checkImage = checkGo.AddComponent<Image>();
            checkImage.color = new Color(0.2f, 0.6f, 0.2f, 1f);

            // Label
            var text = GetString(element, "text");
            if (!string.IsNullOrEmpty(text))
            {
                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(go.transform, false);
                var labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0, 0);
                labelRect.anchorMax = new Vector2(1, 1);
                labelRect.offsetMin = new Vector2(25, 0);
                labelRect.offsetMax = Vector2.zero;

                var labelText = labelGo.AddComponent<Text>();
                labelText.text = text;
                labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                labelText.color = Color.black;
                labelText.alignment = TextAnchor.MiddleLeft;
            }

            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = GetBool(element, "isOn", true);

            return go;
        }

        private GameObject CreateSlider(Dictionary<string, object> element, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var slider = go.AddComponent<Slider>();

            // Background
            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(go.transform, false);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);

            // Fill Area
            var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGo.transform.SetParent(go.transform, false);
            var fillAreaRect = fillAreaGo.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);

            // Fill
            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fillGo.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.6f, 0.8f, 1f);

            // Handle Slide Area
            var handleAreaGo = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleAreaGo.transform.SetParent(go.transform, false);
            var handleAreaRect = handleAreaGo.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);

            // Handle
            var handleGo = new GameObject("Handle", typeof(RectTransform));
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            var handleRect = handleGo.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.zero;
            handleRect.sizeDelta = new Vector2(20, 0);
            handleRect.anchoredPosition = Vector2.zero;
            var handleImage = handleGo.AddComponent<Image>();
            handleImage.color = Color.white;

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.minValue = GetFloatFromPayload(element, "minValue", 0f);
            slider.maxValue = GetFloatFromPayload(element, "maxValue", 1f);
            slider.value = GetFloatFromPayload(element, "value", 0.5f);
            slider.wholeNumbers = GetBool(element, "wholeNumbers", false);

            return go;
        }

        private GameObject CreateDropdown(Dictionary<string, object> element, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var image = go.AddComponent<Image>();
            image.color = Color.white;
            var dropdown = go.AddComponent<Dropdown>();

            // Label
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10, 0);
            labelRect.offsetMax = new Vector2(-25, 0);

            var labelText = labelGo.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.color = Color.black;
            labelText.alignment = TextAnchor.MiddleLeft;

            // Arrow
            var arrowGo = new GameObject("Arrow", typeof(RectTransform));
            arrowGo.transform.SetParent(go.transform, false);
            var arrowRect = arrowGo.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.pivot = new Vector2(1, 0.5f);
            arrowRect.sizeDelta = new Vector2(20, 20);
            arrowRect.anchoredPosition = new Vector2(-5, 0);
            var arrowImage = arrowGo.AddComponent<Image>();
            arrowImage.color = Color.black;

            dropdown.captionText = labelText;
            dropdown.targetGraphic = image;

            // Add options if provided
            if (element.TryGetValue("options", out var optionsObj) && optionsObj is List<object> options)
            {
                dropdown.options.Clear();
                foreach (var option in options)
                {
                    dropdown.options.Add(new Dropdown.OptionData(option.ToString()));
                }
                if (dropdown.options.Count > 0)
                {
                    dropdown.value = 0;
                    dropdown.RefreshShownValue();
                }
            }

            return go;
        }

        private void ApplyCommonProperties(GameObject go, Dictionary<string, object> element)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) return;

            // Size
            var width = GetFloatFromPayload(element, "width", 0);
            var height = GetFloatFromPayload(element, "height", 0);
            if (width > 0 || height > 0)
            {
                rect.sizeDelta = new Vector2(
                    width > 0 ? width : rect.sizeDelta.x,
                    height > 0 ? height : rect.sizeDelta.y
                );
            }

            // Anchors
            if (element.TryGetValue("anchorPreset", out var presetObj))
            {
                ApplyAnchorPreset(rect, presetObj.ToString());
            }

            // Position
            if (element.TryGetValue("position", out var posObj) && posObj is Dictionary<string, object> posDict)
            {
                var x = GetFloatFromDict(posDict, "x", 0);
                var y = GetFloatFromDict(posDict, "y", 0);
                rect.anchoredPosition = new Vector2(x, y);
            }

            // Pivot
            if (element.TryGetValue("pivot", out var pivotObj) && pivotObj is Dictionary<string, object> pivotDict)
            {
                var x = GetFloatFromDict(pivotDict, "x", 0.5f);
                var y = GetFloatFromDict(pivotDict, "y", 0.5f);
                rect.pivot = new Vector2(x, y);
            }
        }

        private void ApplyLayout(GameObject go, Dictionary<string, object> element)
        {
            if (!element.TryGetValue("layout", out var layoutObj))
                return;

            string layoutType;
            Dictionary<string, object> layoutConfig = null;

            if (layoutObj is string layoutStr)
            {
                layoutType = layoutStr;
            }
            else if (layoutObj is Dictionary<string, object> layoutDict)
            {
                layoutType = GetString(layoutDict, "type") ?? "Vertical";
                layoutConfig = layoutDict;
            }
            else
            {
                return;
            }

            HorizontalOrVerticalLayoutGroup layoutGroup = null;
            GridLayoutGroup gridLayout = null;

            switch (layoutType.ToLowerInvariant())
            {
                case "horizontal":
                    layoutGroup = go.AddComponent<HorizontalLayoutGroup>();
                    break;
                case "vertical":
                    layoutGroup = go.AddComponent<VerticalLayoutGroup>();
                    break;
                case "grid":
                    gridLayout = go.AddComponent<GridLayoutGroup>();
                    break;
            }

            if (layoutGroup != null && layoutConfig != null)
            {
                ConfigureLayoutGroup(layoutGroup, layoutConfig);
            }
            else if (gridLayout != null && layoutConfig != null)
            {
                ConfigureGridLayout(gridLayout, layoutConfig);
            }
        }

        private void ConfigureLayoutGroup(HorizontalOrVerticalLayoutGroup layout, Dictionary<string, object> config)
        {
            // Padding
            if (config.TryGetValue("padding", out var paddingObj) && paddingObj is Dictionary<string, object> paddingDict)
            {
                layout.padding = new RectOffset(
                    GetIntFromDict(paddingDict, "left", 0),
                    GetIntFromDict(paddingDict, "right", 0),
                    GetIntFromDict(paddingDict, "top", 0),
                    GetIntFromDict(paddingDict, "bottom", 0)
                );
            }

            // Spacing
            layout.spacing = GetFloatFromPayload(config, "spacing", 0);

            // Child control
            layout.childControlWidth = GetBool(config, "childControlWidth", true);
            layout.childControlHeight = GetBool(config, "childControlHeight", true);
            layout.childScaleWidth = GetBool(config, "childScaleWidth", false);
            layout.childScaleHeight = GetBool(config, "childScaleHeight", false);
            layout.childForceExpandWidth = GetBool(config, "childForceExpandWidth", true);
            layout.childForceExpandHeight = GetBool(config, "childForceExpandHeight", true);

            // Alignment
            var alignment = GetString(config, "childAlignment");
            if (!string.IsNullOrEmpty(alignment))
            {
                layout.childAlignment = ParseTextAnchor(alignment);
            }
        }

        private void ConfigureGridLayout(GridLayoutGroup layout, Dictionary<string, object> config)
        {
            // Padding
            if (config.TryGetValue("padding", out var paddingObj) && paddingObj is Dictionary<string, object> paddingDict)
            {
                layout.padding = new RectOffset(
                    GetIntFromDict(paddingDict, "left", 0),
                    GetIntFromDict(paddingDict, "right", 0),
                    GetIntFromDict(paddingDict, "top", 0),
                    GetIntFromDict(paddingDict, "bottom", 0)
                );
            }

            // Cell size
            if (config.TryGetValue("cellSize", out var cellSizeObj) && cellSizeObj is Dictionary<string, object> cellDict)
            {
                layout.cellSize = new Vector2(
                    GetFloatFromDict(cellDict, "x", 100),
                    GetFloatFromDict(cellDict, "y", 100)
                );
            }

            // Spacing
            if (config.TryGetValue("spacing", out var spacingObj) && spacingObj is Dictionary<string, object> spacingDict)
            {
                layout.spacing = new Vector2(
                    GetFloatFromDict(spacingDict, "x", 0),
                    GetFloatFromDict(spacingDict, "y", 0)
                );
            }
            else
            {
                var spacing = GetFloatFromPayload(config, "spacing", 0);
                layout.spacing = new Vector2(spacing, spacing);
            }

            // Constraint
            var constraint = GetString(config, "constraint")?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(constraint))
            {
                layout.constraint = constraint switch
                {
                    "fixedcolumncount" => GridLayoutGroup.Constraint.FixedColumnCount,
                    "fixedrowcount" => GridLayoutGroup.Constraint.FixedRowCount,
                    _ => GridLayoutGroup.Constraint.Flexible
                };
                layout.constraintCount = GetInt(config, "constraintCount", 2);
            }

            // Alignment
            var alignment = GetString(config, "childAlignment");
            if (!string.IsNullOrEmpty(alignment))
            {
                layout.childAlignment = ParseTextAnchor(alignment);
            }
        }

        private void ApplyAnchorPreset(RectTransform rect, string preset)
        {
            switch (preset?.ToLowerInvariant())
            {
                case "topleft":
                    rect.anchorMin = new Vector2(0, 1);
                    rect.anchorMax = new Vector2(0, 1);
                    rect.pivot = new Vector2(0, 1);
                    break;
                case "topcenter":
                    rect.anchorMin = new Vector2(0.5f, 1);
                    rect.anchorMax = new Vector2(0.5f, 1);
                    rect.pivot = new Vector2(0.5f, 1);
                    break;
                case "topright":
                    rect.anchorMin = new Vector2(1, 1);
                    rect.anchorMax = new Vector2(1, 1);
                    rect.pivot = new Vector2(1, 1);
                    break;
                case "middleleft":
                    rect.anchorMin = new Vector2(0, 0.5f);
                    rect.anchorMax = new Vector2(0, 0.5f);
                    rect.pivot = new Vector2(0, 0.5f);
                    break;
                case "middlecenter":
                case "center":
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;
                case "middleright":
                    rect.anchorMin = new Vector2(1, 0.5f);
                    rect.anchorMax = new Vector2(1, 0.5f);
                    rect.pivot = new Vector2(1, 0.5f);
                    break;
                case "bottomleft":
                    rect.anchorMin = new Vector2(0, 0);
                    rect.anchorMax = new Vector2(0, 0);
                    rect.pivot = new Vector2(0, 0);
                    break;
                case "bottomcenter":
                    rect.anchorMin = new Vector2(0.5f, 0);
                    rect.anchorMax = new Vector2(0.5f, 0);
                    rect.pivot = new Vector2(0.5f, 0);
                    break;
                case "bottomright":
                    rect.anchorMin = new Vector2(1, 0);
                    rect.anchorMax = new Vector2(1, 0);
                    rect.pivot = new Vector2(1, 0);
                    break;
                case "stretchall":
                case "stretch":
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    break;
            }
        }

        #endregion

        #region Clone Hierarchy

        private object CloneHierarchy(Dictionary<string, object> payload)
        {
            var sourcePath = GetString(payload, "sourcePath");
            var targetParentPath = GetString(payload, "targetParentPath");
            var newName = GetString(payload, "newName");

            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new InvalidOperationException("sourcePath is required for clone operation.");
            }

            var source = ResolveGameObject(sourcePath);
            Transform targetParent = null;

            if (!string.IsNullOrEmpty(targetParentPath))
            {
                targetParent = ResolveGameObject(targetParentPath).transform;
            }
            else
            {
                targetParent = source.transform.parent;
            }

            var clone = UnityEngine.Object.Instantiate(source, targetParent);
            Undo.RegisterCreatedObjectUndo(clone, "Clone UI Hierarchy");

            if (!string.IsNullOrEmpty(newName))
            {
                clone.name = newName;
            }
            else
            {
                clone.name = source.name + "_Clone";
            }

            EditorSceneManager.MarkSceneDirty(clone.scene);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["clonePath"] = BuildGameObjectPath(clone),
                ["sourcePath"] = sourcePath
            };
        }

        #endregion

        #region Inspect Hierarchy

        private object InspectHierarchy(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            if (string.IsNullOrEmpty(targetPath))
            {
                throw new InvalidOperationException("targetPath is required for inspect operation.");
            }

            var target = ResolveGameObject(targetPath);
            var includeChildren = GetBool(payload, "includeChildren", true);
            var maxDepth = GetInt(payload, "maxDepth", 10);

            var structure = InspectUIElement(target, includeChildren, maxDepth, 0);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["structure"] = structure
            };
        }

        private Dictionary<string, object> InspectUIElement(GameObject go, bool includeChildren, int maxDepth, int currentDepth)
        {
            var result = new Dictionary<string, object>
            {
                ["name"] = go.name,
                ["path"] = BuildGameObjectPath(go),
                ["active"] = go.activeSelf
            };

            // Determine type
            if (go.GetComponent<Button>() != null)
                result["type"] = "button";
            else if (go.GetComponent<InputField>() != null)
                result["type"] = "inputfield";
            else if (go.GetComponent<Toggle>() != null)
                result["type"] = "toggle";
            else if (go.GetComponent<Slider>() != null)
                result["type"] = "slider";
            else if (go.GetComponent<Dropdown>() != null)
                result["type"] = "dropdown";
            else if (go.GetComponent<ScrollRect>() != null)
                result["type"] = "scrollview";
            else if (go.GetComponent<Text>() != null || HasTextMeshPro(go))
                result["type"] = "text";
            else if (go.GetComponent<Image>() != null)
                result["type"] = "panel";
            else
                result["type"] = "container";

            // RectTransform info
            var rect = go.GetComponent<RectTransform>();
            if (rect != null)
            {
                result["rectTransform"] = new Dictionary<string, object>
                {
                    ["anchoredPosition"] = new Dictionary<string, object> { ["x"] = rect.anchoredPosition.x, ["y"] = rect.anchoredPosition.y },
                    ["sizeDelta"] = new Dictionary<string, object> { ["x"] = rect.sizeDelta.x, ["y"] = rect.sizeDelta.y },
                    ["anchorMin"] = new Dictionary<string, object> { ["x"] = rect.anchorMin.x, ["y"] = rect.anchorMin.y },
                    ["anchorMax"] = new Dictionary<string, object> { ["x"] = rect.anchorMax.x, ["y"] = rect.anchorMax.y },
                    ["pivot"] = new Dictionary<string, object> { ["x"] = rect.pivot.x, ["y"] = rect.pivot.y }
                };
            }

            // Layout info
            var hLayout = go.GetComponent<HorizontalLayoutGroup>();
            var vLayout = go.GetComponent<VerticalLayoutGroup>();
            var gridLayout = go.GetComponent<GridLayoutGroup>();

            if (hLayout != null)
                result["layout"] = "Horizontal";
            else if (vLayout != null)
                result["layout"] = "Vertical";
            else if (gridLayout != null)
                result["layout"] = "Grid";

            // CanvasGroup info
            var canvasGroup = go.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                result["canvasGroup"] = new Dictionary<string, object>
                {
                    ["alpha"] = canvasGroup.alpha,
                    ["interactable"] = canvasGroup.interactable,
                    ["blocksRaycasts"] = canvasGroup.blocksRaycasts
                };
            }

            // Children
            if (includeChildren && currentDepth < maxDepth)
            {
                var children = new List<Dictionary<string, object>>();
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    var child = go.transform.GetChild(i).gameObject;
                    children.Add(InspectUIElement(child, includeChildren, maxDepth, currentDepth + 1));
                }
                result["children"] = children;
                result["childCount"] = go.transform.childCount;
            }

            return result;
        }

        #endregion

        #region Delete Hierarchy

        private object DeleteHierarchy(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            if (string.IsNullOrEmpty(targetPath))
            {
                throw new InvalidOperationException("targetPath is required for delete operation.");
            }

            var target = ResolveGameObject(targetPath);
            var childCount = CountDescendants(target.transform);

            Undo.DestroyObjectImmediate(target);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["deletedPath"] = targetPath,
                ["deletedCount"] = childCount + 1
            };
        }

        private int CountDescendants(Transform t)
        {
            int count = 0;
            for (int i = 0; i < t.childCount; i++)
            {
                count += 1 + CountDescendants(t.GetChild(i));
            }
            return count;
        }

        #endregion

        #region Visibility Control

        private object SetVisibility(Dictionary<string, object> payload, bool visible)
        {
            var targets = GetTargetPaths(payload);
            var useCanvasGroup = GetBool(payload, "useCanvasGroup", true);
            var results = new List<Dictionary<string, object>>();

            foreach (var targetPath in targets)
            {
                var target = ResolveGameObject(targetPath);

                if (useCanvasGroup)
                {
                    var canvasGroup = target.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        canvasGroup = Undo.AddComponent<CanvasGroup>(target);
                    }

                    Undo.RecordObject(canvasGroup, visible ? "Show UI" : "Hide UI");
                    canvasGroup.alpha = visible ? 1f : 0f;
                    canvasGroup.interactable = visible;
                    canvasGroup.blocksRaycasts = visible;
                }
                else
                {
                    Undo.RecordObject(target, visible ? "Show UI" : "Hide UI");
                    target.SetActive(visible);
                }

                results.Add(new Dictionary<string, object>
                {
                    ["path"] = targetPath,
                    ["visible"] = visible
                });

                EditorSceneManager.MarkSceneDirty(target.scene);
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["results"] = results
            };
        }

        private object ToggleVisibility(Dictionary<string, object> payload)
        {
            var targets = GetTargetPaths(payload);
            var useCanvasGroup = GetBool(payload, "useCanvasGroup", true);
            var results = new List<Dictionary<string, object>>();

            foreach (var targetPath in targets)
            {
                var target = ResolveGameObject(targetPath);
                bool newVisible;

                if (useCanvasGroup)
                {
                    var canvasGroup = target.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        canvasGroup = Undo.AddComponent<CanvasGroup>(target);
                        newVisible = false; // Default to hiding when adding new CanvasGroup
                    }
                    else
                    {
                        newVisible = canvasGroup.alpha < 0.5f;
                    }

                    Undo.RecordObject(canvasGroup, "Toggle UI Visibility");
                    canvasGroup.alpha = newVisible ? 1f : 0f;
                    canvasGroup.interactable = newVisible;
                    canvasGroup.blocksRaycasts = newVisible;
                }
                else
                {
                    newVisible = !target.activeSelf;
                    Undo.RecordObject(target, "Toggle UI Visibility");
                    target.SetActive(newVisible);
                }

                results.Add(new Dictionary<string, object>
                {
                    ["path"] = targetPath,
                    ["visible"] = newVisible
                });

                EditorSceneManager.MarkSceneDirty(target.scene);
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["results"] = results
            };
        }

        private List<string> GetTargetPaths(Dictionary<string, object> payload)
        {
            var paths = new List<string>();

            if (payload.TryGetValue("targets", out var targetsObj) && targetsObj is List<object> targetsList)
            {
                foreach (var t in targetsList)
                {
                    paths.Add(t.ToString());
                }
            }
            else if (payload.TryGetValue("targetPath", out var targetPathObj))
            {
                paths.Add(targetPathObj.ToString());
            }

            if (paths.Count == 0)
            {
                throw new InvalidOperationException("targets or targetPath is required.");
            }

            return paths;
        }

        #endregion

        #region Helper Methods

        private bool TryAddTextMeshPro(GameObject go, string text, Dictionary<string, object> element)
        {
            try
            {
                var tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
                if (tmpType == null) return false;

                var tmp = go.AddComponent(tmpType);

                // Set text
                var textProp = tmpType.GetProperty("text");
                textProp?.SetValue(tmp, text);

                // Set font size
                var fontSize = GetInt(element, "fontSize", 14);
                var fontSizeProp = tmpType.GetProperty("fontSize");
                fontSizeProp?.SetValue(tmp, (float)fontSize);

                // Set color
                if (element.TryGetValue("color", out var colorObj) && colorObj is Dictionary<string, object> colorDict)
                {
                    var colorProp = tmpType.GetProperty("color");
                    colorProp?.SetValue(tmp, GetColorFromDict(colorDict, Color.black));
                }

                // Set alignment
                var alignment = GetString(element, "alignment");
                if (!string.IsNullOrEmpty(alignment))
                {
                    var alignmentProp = tmpType.GetProperty("alignment");
                    var alignmentEnumType = Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
                    if (alignmentEnumType != null && alignmentProp != null)
                    {
                        if (Enum.TryParse(alignmentEnumType, alignment, true, out var alignValue))
                        {
                            alignmentProp.SetValue(tmp, alignValue);
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool HasTextMeshPro(GameObject go)
        {
            var tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            return tmpType != null && go.GetComponent(tmpType) != null;
        }

        private TextAnchor ParseTextAnchor(string alignment)
        {
            return alignment?.ToLowerInvariant() switch
            {
                "upperleft" => TextAnchor.UpperLeft,
                "uppercenter" => TextAnchor.UpperCenter,
                "upperright" => TextAnchor.UpperRight,
                "middleleft" => TextAnchor.MiddleLeft,
                "middlecenter" => TextAnchor.MiddleCenter,
                "middleright" => TextAnchor.MiddleRight,
                "lowerleft" => TextAnchor.LowerLeft,
                "lowercenter" => TextAnchor.LowerCenter,
                "lowerright" => TextAnchor.LowerRight,
                _ => TextAnchor.MiddleCenter
            };
        }

        private Color GetColorFromDict(Dictionary<string, object> dict, Color defaultColor)
        {
            return new Color(
                GetFloatFromDict(dict, "r", defaultColor.r),
                GetFloatFromDict(dict, "g", defaultColor.g),
                GetFloatFromDict(dict, "b", defaultColor.b),
                GetFloatFromDict(dict, "a", defaultColor.a)
            );
        }

        private float GetFloatFromDict(Dictionary<string, object> dict, string key, float defaultValue)
        {
            if (dict.TryGetValue(key, out var value))
            {
                return Convert.ToSingle(value);
            }
            return defaultValue;
        }

        private int GetIntFromDict(Dictionary<string, object> dict, string key, int defaultValue)
        {
            if (dict.TryGetValue(key, out var value))
            {
                return Convert.ToInt32(value);
            }
            return defaultValue;
        }

        private float GetFloatFromPayload(Dictionary<string, object> payload, string key, float defaultValue)
        {
            if (payload.TryGetValue(key, out var value))
            {
                return Convert.ToSingle(value);
            }
            return defaultValue;
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
