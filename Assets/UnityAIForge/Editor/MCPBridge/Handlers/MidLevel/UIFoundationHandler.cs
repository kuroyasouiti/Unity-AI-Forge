using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MCP.Editor.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Mid-level UI foundation: create Canvas, Panel, Button, Text, Image, InputField, ScrollView,
    /// LayoutGroup with presets and UI templates (dialog, hud, menu).
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
            "createScrollView",
            "addLayoutGroup",
            "updateLayoutGroup",
            "removeLayoutGroup",
            "createFromTemplate",
            "inspect",
            "configureCanvasGroup",
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
                "createScrollView" => CreateScrollView(payload),
                "addLayoutGroup" => AddLayoutGroup(payload),
                "updateLayoutGroup" => UpdateLayoutGroup(payload),
                "removeLayoutGroup" => RemoveLayoutGroup(payload),
                "createFromTemplate" => CreateFromTemplate(payload),
                "inspect" => InspectUI(payload),
                "configureCanvasGroup" => ConfigureCanvasGroup(payload),
                _ => throw new InvalidOperationException($"Unsupported UI foundation operation: {operation}"),
            };
        }

        #region Create Canvas

        private object CreateCanvas(Dictionary<string, object> payload)
        {
            var name = GetString(payload, "name") ?? "Canvas";
            var renderMode = GetString(payload, "renderMode")?.ToLowerInvariant() ?? "screenspaceoverlay";
            var sortingOrder = GetInt(payload, "sortingOrder", 0);

            // Resolve parent before creating Canvas to avoid orphan on failure
            var parentPath = GetString(payload, "parentPath");
            Transform parentTransform = null;
            if (!string.IsNullOrEmpty(parentPath))
            {
                parentTransform = ResolveGameObject(parentPath).transform;
            }

            var canvasGo = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");

            if (parentTransform != null)
            {
                canvasGo.transform.SetParent(parentTransform, false);
            }

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
                    // Assign camera: use explicit cameraPath or fallback to Camera.main
                    var cameraPath = GetString(payload, "cameraPath");
                    if (!string.IsNullOrEmpty(cameraPath))
                    {
                        var cameraGo = ResolveGameObject(cameraPath);
                        var cam = cameraGo.GetComponent<Camera>();
                        if (cam != null) canvas.worldCamera = cam;
                    }
                    else if (Camera.main != null)
                    {
                        canvas.worldCamera = Camera.main;
                    }
                    break;
                case "worldspace":
                    canvas.renderMode = RenderMode.WorldSpace;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unknown renderMode: '{renderMode}'. Use 'screenSpaceOverlay', 'screenSpaceCamera', or 'worldSpace'.");
            }

            canvas.sortingOrder = sortingOrder;

            // Configure scaler for UI scaling
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Create EventSystem if it doesn't exist in the scene
            var eventSystemPath = EnsureEventSystem();

            EditorSceneManager.MarkSceneDirty(canvasGo.scene);

            var response = CreateSuccessResponse(("path", BuildGameObjectPath(canvasGo)));
            if (!string.IsNullOrEmpty(eventSystemPath))
            {
                response["eventSystemCreated"] = true;
                response["eventSystemPath"] = eventSystemPath;
            }
            return response;
        }

        /// <summary>
        /// Ensures an EventSystem exists in the scene. Creates one if it doesn't exist.
        /// </summary>
        /// <returns>The path to the created EventSystem, or null if one already existed.</returns>
        private string EnsureEventSystem()
        {
            // Check if EventSystem already exists
            var existingEventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (existingEventSystem != null)
            {
                return null; // EventSystem already exists
            }

            // Create EventSystem
            var eventSystemGo = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(eventSystemGo, "Create EventSystem");

            Undo.AddComponent<EventSystem>(eventSystemGo);

            // Use InputSystemUIInputModule if New Input System is available, otherwise StandaloneInputModule
            var inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModuleType != null)
                Undo.AddComponent(eventSystemGo, inputSystemModuleType);
            else
                Undo.AddComponent<StandaloneInputModule>(eventSystemGo);

            return BuildGameObjectPath(eventSystemGo);
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
            var isStretch = ApplyAnchorPreset(rectTransform, payload);

            // Set size (skip if stretchAll — sizeDelta is already zero)
            if (!isStretch)
            {
                var width = GetFloat(payload, "width", 400f);
                var height = GetFloat(payload, "height", 300f);
                rectTransform.sizeDelta = new Vector2(width, height);
            }

            // Set color
            if (payload.TryGetValue("color", out var colorObj) && colorObj is Dictionary<string, object> colorDict)
            {
                image.color = GetColorFromDict(colorDict, new Color(1, 1, 1, 0.392f));
            }
            else
            {
                image.color = new Color(1, 1, 1, 0.392f); // Default semi-transparent white
            }

            // Add CanvasGroup if requested
            if (GetBool(payload, "addCanvasGroup", false))
            {
                var canvasGroup = Undo.AddComponent<CanvasGroup>(panelGo);
                if (payload.ContainsKey("alpha"))
                    canvasGroup.alpha = GetFloat(payload, "alpha", 1f);
                if (payload.ContainsKey("interactable"))
                    canvasGroup.interactable = GetBool(payload, "interactable", true);
                if (payload.ContainsKey("blocksRaycasts"))
                    canvasGroup.blocksRaycasts = GetBool(payload, "blocksRaycasts", true);
                if (payload.ContainsKey("ignoreParentGroups"))
                    canvasGroup.ignoreParentGroups = GetBool(payload, "ignoreParentGroups", false);
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
            var isStretch = ApplyAnchorPreset(rectTransform, payload);

            // Set size (skip if stretchAll — sizeDelta is already zero)
            if (!isStretch)
            {
                var width = GetFloat(payload, "width", 160f);
                var height = GetFloat(payload, "height", 30f);
                rectTransform.sizeDelta = new Vector2(width, height);
            }

            // Set color
            if (payload.TryGetValue("color", out var colorObj) && colorObj is Dictionary<string, object> colorDict)
            {
                image.color = GetColorFromDict(colorDict, Color.white);
            }

            // Load sprite if specified
            string spriteWarning = null;
            var spritePath = GetString(payload, "spritePath");
            if (!string.IsNullOrEmpty(spritePath))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite != null)
                    image.sprite = sprite;
                else
                    spriteWarning = $"Sprite not found at '{spritePath}'. Ensure the asset exists and is imported as Sprite.";
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
            var response = CreateSuccessResponse(("path", BuildGameObjectPath(buttonGo)));
            if (spriteWarning != null) response["warning"] = spriteWarning;
            return response;
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
            var isStretch = ApplyAnchorPreset(rectTransform, payload);

            // Set size (skip if stretchAll — sizeDelta is already zero)
            if (!isStretch)
            {
                var width = GetFloat(payload, "width", 160f);
                var height = GetFloat(payload, "height", 30f);
                rectTransform.sizeDelta = new Vector2(width, height);
            }

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
            var isStretch = ApplyAnchorPreset(rectTransform, payload);

            // Set size (skip if stretchAll — sizeDelta is already zero)
            if (!isStretch)
            {
                var width = GetFloat(payload, "width", 100f);
                var height = GetFloat(payload, "height", 100f);
                rectTransform.sizeDelta = new Vector2(width, height);
            }

            // Set color
            if (payload.TryGetValue("color", out var colorObj) && colorObj is Dictionary<string, object> colorDict)
            {
                image.color = GetColorFromDict(colorDict, Color.white);
            }

            // Load sprite if specified
            string spriteWarning = null;
            var spritePath = GetString(payload, "spritePath");
            if (!string.IsNullOrEmpty(spritePath))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite != null)
                    image.sprite = sprite;
                else
                    spriteWarning = $"Sprite not found at '{spritePath}'. Ensure the asset exists and is imported as Sprite.";
            }

            EditorSceneManager.MarkSceneDirty(imageGo.scene);
            var response = CreateSuccessResponse(("path", BuildGameObjectPath(imageGo)));
            if (spriteWarning != null) response["warning"] = spriteWarning;
            return response;
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
            var isStretch = ApplyAnchorPreset(rectTransform, payload);

            // Set size (skip if stretchAll — sizeDelta is already zero)
            if (!isStretch)
            {
                var width = GetFloat(payload, "width", 160f);
                var height = GetFloat(payload, "height", 30f);
                rectTransform.sizeDelta = new Vector2(width, height);
            }

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

        #region Create ScrollView

        private object CreateScrollView(Dictionary<string, object> payload)
        {
            var name = GetString(payload, "name") ?? "ScrollView";
            var parentPath = GetString(payload, "parentPath");

            if (string.IsNullOrEmpty(parentPath))
            {
                throw new InvalidOperationException("parentPath is required for createScrollView.");
            }

            var parent = ResolveGameObject(parentPath);

            // Create ScrollView root
            var scrollViewGo = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(scrollViewGo, "Create ScrollView");
            scrollViewGo.transform.SetParent(parent.transform, false);

            var scrollRect = Undo.AddComponent<ScrollRect>(scrollViewGo);
            var scrollImage = Undo.AddComponent<Image>(scrollViewGo);
            scrollImage.color = new Color(1, 1, 1, 0.1f);

            var rectTransform = scrollViewGo.GetComponent<RectTransform>();
            var isStretch = ApplyAnchorPreset(rectTransform, payload);

            var width = GetFloat(payload, "width", 400f);
            var height = GetFloat(payload, "height", 300f);
            if (!isStretch)
            {
                rectTransform.sizeDelta = new Vector2(width, height);
            }

            // Create Viewport
            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(viewportGo, "Create Viewport");
            viewportGo.transform.SetParent(scrollViewGo.transform, false);

            var viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.pivot = new Vector2(0, 1);

            var viewportImage = Undo.AddComponent<Image>(viewportGo);
            viewportImage.color = Color.white;
            var mask = Undo.AddComponent<Mask>(viewportGo);
            mask.showMaskGraphic = false;

            // Create Content
            var contentGo = new GameObject("Content", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(contentGo, "Create Content");
            contentGo.transform.SetParent(viewportGo.transform, false);

            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1);
            contentRect.sizeDelta = new Vector2(0, height); // Initial content height

            // Configure ScrollRect
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;

            var horizontal = GetBool(payload, "horizontal", false);
            var vertical = GetBool(payload, "vertical", true);
            scrollRect.horizontal = horizontal;
            scrollRect.vertical = vertical;

            // Movement type
            var movementType = GetString(payload, "movementType")?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(movementType))
            {
                scrollRect.movementType = movementType switch
                {
                    "unrestricted" => ScrollRect.MovementType.Unrestricted,
                    "elastic" => ScrollRect.MovementType.Elastic,
                    "clamped" => ScrollRect.MovementType.Clamped,
                    _ => scrollRect.movementType
                };
            }

            // Elasticity
            if (payload.ContainsKey("elasticity"))
                scrollRect.elasticity = GetFloat(payload, "elasticity", scrollRect.elasticity);

            // Inertia
            if (payload.ContainsKey("inertia"))
                scrollRect.inertia = GetBool(payload, "inertia", scrollRect.inertia);

            // Deceleration rate
            if (payload.ContainsKey("decelerationRate"))
                scrollRect.decelerationRate = GetFloat(payload, "decelerationRate", scrollRect.decelerationRate);

            // Scroll sensitivity
            if (payload.ContainsKey("scrollSensitivity"))
                scrollRect.scrollSensitivity = GetFloat(payload, "scrollSensitivity", scrollRect.scrollSensitivity);

            // Add layout group to content if specified
            var contentLayout = GetString(payload, "contentLayout")?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(contentLayout))
            {
                switch (contentLayout)
                {
                    case "vertical":
                        var vlg = Undo.AddComponent<VerticalLayoutGroup>(contentGo);
                        vlg.childControlWidth = true;
                        vlg.childControlHeight = false;
                        vlg.childForceExpandWidth = true;
                        vlg.childForceExpandHeight = false;
                        var vcsf = Undo.AddComponent<ContentSizeFitter>(contentGo);
                        vcsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                        break;
                    case "horizontal":
                        var hlg = Undo.AddComponent<HorizontalLayoutGroup>(contentGo);
                        hlg.childControlWidth = false;
                        hlg.childControlHeight = true;
                        hlg.childForceExpandWidth = false;
                        hlg.childForceExpandHeight = true;
                        var hcsf = Undo.AddComponent<ContentSizeFitter>(contentGo);
                        hcsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                        break;
                    case "grid":
                        var glg = Undo.AddComponent<GridLayoutGroup>(contentGo);
                        glg.cellSize = new Vector2(100, 100);
                        glg.spacing = new Vector2(10, 10);
                        var gcsf = Undo.AddComponent<ContentSizeFitter>(contentGo);
                        gcsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                        break;
                }
            }

            // Create scrollbars if needed
            var showScrollbar = GetBool(payload, "showScrollbar", true);
            if (showScrollbar)
            {
                if (vertical)
                {
                    var vScrollbar = CreateScrollbar(scrollViewGo, "Scrollbar Vertical", true);
                    scrollRect.verticalScrollbar = vScrollbar;
                    scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
                }
                if (horizontal)
                {
                    var hScrollbar = CreateScrollbar(scrollViewGo, "Scrollbar Horizontal", false);
                    scrollRect.horizontalScrollbar = hScrollbar;
                    scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
                }
            }

            EditorSceneManager.MarkSceneDirty(scrollViewGo.scene);
            return CreateSuccessResponse(
                ("path", BuildGameObjectPath(scrollViewGo)),
                ("contentPath", BuildGameObjectPath(contentGo)),
                ("viewportPath", BuildGameObjectPath(viewportGo))
            );
        }

        private Scrollbar CreateScrollbar(GameObject parent, string name, bool vertical)
        {
            var scrollbarGo = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(scrollbarGo, "Create Scrollbar");
            scrollbarGo.transform.SetParent(parent.transform, false);

            var scrollbarRect = scrollbarGo.GetComponent<RectTransform>();
            var scrollbarImage = Undo.AddComponent<Image>(scrollbarGo);
            scrollbarImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            var scrollbar = Undo.AddComponent<Scrollbar>(scrollbarGo);

            // Create sliding area
            var slidingAreaGo = new GameObject("Sliding Area", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(slidingAreaGo, "Create Sliding Area");
            slidingAreaGo.transform.SetParent(scrollbarGo.transform, false);

            var slidingAreaRect = slidingAreaGo.GetComponent<RectTransform>();
            slidingAreaRect.anchorMin = Vector2.zero;
            slidingAreaRect.anchorMax = Vector2.one;
            slidingAreaRect.sizeDelta = new Vector2(-20, -20);

            // Create handle
            var handleGo = new GameObject("Handle", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(handleGo, "Create Handle");
            handleGo.transform.SetParent(slidingAreaGo.transform, false);

            var handleRect = handleGo.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 20);
            var handleImage = Undo.AddComponent<Image>(handleGo);
            handleImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);

            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;

            if (vertical)
            {
                scrollbar.direction = Scrollbar.Direction.BottomToTop;
                scrollbarRect.anchorMin = new Vector2(1, 0);
                scrollbarRect.anchorMax = new Vector2(1, 1);
                scrollbarRect.pivot = new Vector2(1, 1);
                scrollbarRect.sizeDelta = new Vector2(20, 0);
            }
            else
            {
                scrollbar.direction = Scrollbar.Direction.LeftToRight;
                scrollbarRect.anchorMin = new Vector2(0, 0);
                scrollbarRect.anchorMax = new Vector2(1, 0);
                scrollbarRect.pivot = new Vector2(0, 0);
                scrollbarRect.sizeDelta = new Vector2(0, 20);
            }

            return scrollbar;
        }

        #endregion

        #region Layout Group Operations

        private object AddLayoutGroup(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            if (string.IsNullOrEmpty(targetPath))
            {
                throw new InvalidOperationException("targetPath is required for addLayoutGroup.");
            }

            var go = ResolveGameObject(targetPath);
            var layoutType = GetString(payload, "layoutType")?.ToLowerInvariant() ?? "vertical";

            // Remove existing layout group if any
            RemoveExistingLayoutGroup(go);

            LayoutGroup layoutGroup = null;

            switch (layoutType)
            {
                case "horizontal":
                    var hlg = Undo.AddComponent<HorizontalLayoutGroup>(go);
                    ConfigureHorizontalOrVerticalLayoutGroup(hlg, payload);
                    layoutGroup = hlg;
                    break;

                case "vertical":
                    var vlg = Undo.AddComponent<VerticalLayoutGroup>(go);
                    ConfigureHorizontalOrVerticalLayoutGroup(vlg, payload);
                    layoutGroup = vlg;
                    break;

                case "grid":
                    var glg = Undo.AddComponent<GridLayoutGroup>(go);
                    ConfigureGridLayoutGroup(glg, payload);
                    layoutGroup = glg;
                    break;

                default:
                    throw new InvalidOperationException($"Unknown layout type: {layoutType}. Use 'horizontal', 'vertical', or 'grid'.");
            }

            // Add ContentSizeFitter if requested
            if (GetBool(payload, "addContentSizeFitter", false))
            {
                var csf = go.GetComponent<ContentSizeFitter>();
                if (csf == null)
                {
                    csf = Undo.AddComponent<ContentSizeFitter>(go);
                }

                var horizontalFit = GetString(payload, "horizontalFit")?.ToLowerInvariant() ?? "unconstrained";
                var verticalFit = GetString(payload, "verticalFit")?.ToLowerInvariant() ?? "preferredsize";

                csf.horizontalFit = ParseFitMode(horizontalFit);
                csf.verticalFit = ParseFitMode(verticalFit);
            }

            EditorSceneManager.MarkSceneDirty(go.scene);
            return CreateSuccessResponse(
                ("path", BuildGameObjectPath(go)),
                ("layoutType", layoutType)
            );
        }

        private object UpdateLayoutGroup(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            if (string.IsNullOrEmpty(targetPath))
            {
                throw new InvalidOperationException("targetPath is required for updateLayoutGroup.");
            }

            var go = ResolveGameObject(targetPath);

            // Check for existing layout groups
            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            var glg = go.GetComponent<GridLayoutGroup>();

            if (hlg != null)
            {
                Undo.RecordObject(hlg, "Update HorizontalLayoutGroup");
                ConfigureHorizontalOrVerticalLayoutGroup(hlg, payload);
                EditorSceneManager.MarkSceneDirty(go.scene);
                return CreateSuccessResponse(("path", BuildGameObjectPath(go)), ("layoutType", "horizontal"));
            }

            if (vlg != null)
            {
                Undo.RecordObject(vlg, "Update VerticalLayoutGroup");
                ConfigureHorizontalOrVerticalLayoutGroup(vlg, payload);
                EditorSceneManager.MarkSceneDirty(go.scene);
                return CreateSuccessResponse(("path", BuildGameObjectPath(go)), ("layoutType", "vertical"));
            }

            if (glg != null)
            {
                Undo.RecordObject(glg, "Update GridLayoutGroup");
                ConfigureGridLayoutGroup(glg, payload);
                EditorSceneManager.MarkSceneDirty(go.scene);
                return CreateSuccessResponse(("path", BuildGameObjectPath(go)), ("layoutType", "grid"));
            }

            throw new InvalidOperationException($"No LayoutGroup found on '{targetPath}'.");
        }

        private object RemoveLayoutGroup(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            if (string.IsNullOrEmpty(targetPath))
            {
                throw new InvalidOperationException("targetPath is required for removeLayoutGroup.");
            }

            var go = ResolveGameObject(targetPath);
            var removed = RemoveExistingLayoutGroup(go);

            // Also remove ContentSizeFitter if requested
            if (GetBool(payload, "removeContentSizeFitter", true))
            {
                var csf = go.GetComponent<ContentSizeFitter>();
                if (csf != null)
                {
                    Undo.DestroyObjectImmediate(csf);
                }
            }

            EditorSceneManager.MarkSceneDirty(go.scene);
            return CreateSuccessResponse(
                ("path", BuildGameObjectPath(go)),
                ("removed", removed)
            );
        }

        private bool RemoveExistingLayoutGroup(GameObject go)
        {
            var removed = false;

            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                Undo.DestroyObjectImmediate(hlg);
                removed = true;
            }

            var vlg = go.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                Undo.DestroyObjectImmediate(vlg);
                removed = true;
            }

            var glg = go.GetComponent<GridLayoutGroup>();
            if (glg != null)
            {
                Undo.DestroyObjectImmediate(glg);
                removed = true;
            }

            return removed;
        }

        private void ConfigureHorizontalOrVerticalLayoutGroup(HorizontalOrVerticalLayoutGroup lg, Dictionary<string, object> payload)
        {
            // Padding
            if (payload.TryGetValue("padding", out var paddingObj) && paddingObj is Dictionary<string, object> paddingDict)
            {
                lg.padding = new RectOffset(
                    paddingDict.TryGetValue("left", out var l) ? Convert.ToInt32(l) : lg.padding.left,
                    paddingDict.TryGetValue("right", out var r) ? Convert.ToInt32(r) : lg.padding.right,
                    paddingDict.TryGetValue("top", out var t) ? Convert.ToInt32(t) : lg.padding.top,
                    paddingDict.TryGetValue("bottom", out var b) ? Convert.ToInt32(b) : lg.padding.bottom
                );
            }
            else if (payload.ContainsKey("paddingAll"))
            {
                var p = GetInt(payload, "paddingAll", 0);
                lg.padding = new RectOffset(p, p, p, p);
            }

            // Spacing
            if (payload.ContainsKey("spacing"))
            {
                lg.spacing = GetFloat(payload, "spacing", lg.spacing);
            }

            // Child alignment
            var alignment = GetString(payload, "childAlignment")?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(alignment))
            {
                lg.childAlignment = ParseTextAnchor(alignment);
            }

            // Control child size
            if (payload.ContainsKey("childControlWidth"))
            {
                lg.childControlWidth = GetBool(payload, "childControlWidth", lg.childControlWidth);
            }
            if (payload.ContainsKey("childControlHeight"))
            {
                lg.childControlHeight = GetBool(payload, "childControlHeight", lg.childControlHeight);
            }

            // Use child scale
            if (payload.ContainsKey("childScaleWidth"))
            {
                lg.childScaleWidth = GetBool(payload, "childScaleWidth", lg.childScaleWidth);
            }
            if (payload.ContainsKey("childScaleHeight"))
            {
                lg.childScaleHeight = GetBool(payload, "childScaleHeight", lg.childScaleHeight);
            }

            // Force expand
            if (payload.ContainsKey("childForceExpandWidth"))
            {
                lg.childForceExpandWidth = GetBool(payload, "childForceExpandWidth", lg.childForceExpandWidth);
            }
            if (payload.ContainsKey("childForceExpandHeight"))
            {
                lg.childForceExpandHeight = GetBool(payload, "childForceExpandHeight", lg.childForceExpandHeight);
            }

            // Reverse arrangement
            if (payload.ContainsKey("reverseArrangement"))
            {
                lg.reverseArrangement = GetBool(payload, "reverseArrangement", lg.reverseArrangement);
            }
        }

        private void ConfigureGridLayoutGroup(GridLayoutGroup glg, Dictionary<string, object> payload)
        {
            // Padding
            if (payload.TryGetValue("padding", out var paddingObj) && paddingObj is Dictionary<string, object> paddingDict)
            {
                glg.padding = new RectOffset(
                    paddingDict.TryGetValue("left", out var l) ? Convert.ToInt32(l) : glg.padding.left,
                    paddingDict.TryGetValue("right", out var r) ? Convert.ToInt32(r) : glg.padding.right,
                    paddingDict.TryGetValue("top", out var t) ? Convert.ToInt32(t) : glg.padding.top,
                    paddingDict.TryGetValue("bottom", out var b) ? Convert.ToInt32(b) : glg.padding.bottom
                );
            }
            else if (payload.ContainsKey("paddingAll"))
            {
                var p = GetInt(payload, "paddingAll", 0);
                glg.padding = new RectOffset(p, p, p, p);
            }

            // Cell size
            if (payload.TryGetValue("cellSize", out var cellSizeObj) && cellSizeObj is Dictionary<string, object> cellSizeDict)
            {
                glg.cellSize = new Vector2(
                    cellSizeDict.TryGetValue("x", out var cx) ? Convert.ToSingle(cx) : glg.cellSize.x,
                    cellSizeDict.TryGetValue("y", out var cy) ? Convert.ToSingle(cy) : glg.cellSize.y
                );
            }

            // Spacing (supports both numeric and {x, y} object)
            if (payload.TryGetValue("spacing", out var spacingObj))
            {
                if (spacingObj is Dictionary<string, object> spacingDict)
                {
                    glg.spacing = new Vector2(
                        spacingDict.TryGetValue("x", out var sx) ? Convert.ToSingle(sx) : glg.spacing.x,
                        spacingDict.TryGetValue("y", out var sy) ? Convert.ToSingle(sy) : glg.spacing.y
                    );
                }
                else
                {
                    // Numeric value: apply uniformly to both axes
                    var s = Convert.ToSingle(spacingObj);
                    glg.spacing = new Vector2(s, s);
                }
            }
            else if (payload.ContainsKey("spacingAll"))
            {
                var s = GetFloat(payload, "spacingAll", 0);
                glg.spacing = new Vector2(s, s);
            }

            // Start corner
            var startCorner = GetString(payload, "startCorner")?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(startCorner))
            {
                glg.startCorner = startCorner switch
                {
                    "upperleft" => GridLayoutGroup.Corner.UpperLeft,
                    "upperright" => GridLayoutGroup.Corner.UpperRight,
                    "lowerleft" => GridLayoutGroup.Corner.LowerLeft,
                    "lowerright" => GridLayoutGroup.Corner.LowerRight,
                    _ => glg.startCorner
                };
            }

            // Start axis
            var startAxis = GetString(payload, "startAxis")?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(startAxis))
            {
                glg.startAxis = startAxis switch
                {
                    "horizontal" => GridLayoutGroup.Axis.Horizontal,
                    "vertical" => GridLayoutGroup.Axis.Vertical,
                    _ => glg.startAxis
                };
            }

            // Child alignment
            var alignment = GetString(payload, "childAlignment")?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(alignment))
            {
                glg.childAlignment = ParseTextAnchor(alignment);
            }

            // Constraint
            var constraint = GetString(payload, "constraint")?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(constraint))
            {
                glg.constraint = constraint switch
                {
                    "flexible" => GridLayoutGroup.Constraint.Flexible,
                    "fixedcolumncount" => GridLayoutGroup.Constraint.FixedColumnCount,
                    "fixedrowcount" => GridLayoutGroup.Constraint.FixedRowCount,
                    _ => glg.constraint
                };
            }

            // Constraint count
            if (payload.ContainsKey("constraintCount"))
            {
                glg.constraintCount = GetInt(payload, "constraintCount", glg.constraintCount);
            }
        }

        private TextAnchor ParseTextAnchor(string alignment)
        {
            return alignment switch
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
                _ => TextAnchor.UpperLeft
            };
        }

        private ContentSizeFitter.FitMode ParseFitMode(string mode)
        {
            return mode switch
            {
                "unconstrained" => ContentSizeFitter.FitMode.Unconstrained,
                "minsize" => ContentSizeFitter.FitMode.MinSize,
                "preferredsize" => ContentSizeFitter.FitMode.PreferredSize,
                _ => ContentSizeFitter.FitMode.Unconstrained
            };
        }

        #endregion

        #region UI Templates

        private object CreateFromTemplate(Dictionary<string, object> payload)
        {
            var template = GetString(payload, "templateType")?.ToLowerInvariant()
                           ?? GetString(payload, "template")?.ToLowerInvariant();
            if (string.IsNullOrEmpty(template))
            {
                throw new InvalidOperationException("templateType is required for createFromTemplate. Available: dialog, hud, menu, statusBar, inventoryGrid");
            }

            var parentPath = GetString(payload, "parentPath");
            if (string.IsNullOrEmpty(parentPath))
            {
                throw new InvalidOperationException("parentPath (Canvas path) is required for createFromTemplate.");
            }

            var name = GetString(payload, "name") ?? template;

            return template switch
            {
                "dialog" => CreateDialogTemplate(parentPath, name, payload),
                "hud" => CreateHUDTemplate(parentPath, name, payload),
                "menu" => CreateMenuTemplate(parentPath, name, payload),
                "statusbar" => CreateStatusBarTemplate(parentPath, name, payload),
                "inventorygrid" => CreateInventoryGridTemplate(parentPath, name, payload),
                _ => throw new InvalidOperationException($"Unknown template: {template}. Available: dialog, hud, menu, statusBar, inventoryGrid")
            };
        }

        private object CreateDialogTemplate(string parentPath, string name, Dictionary<string, object> payload)
        {
            var parent = ResolveGameObject(parentPath);

            // Dialog root (centered panel with background)
            var dialogGo = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(dialogGo, "Create Dialog");
            dialogGo.transform.SetParent(parent.transform, false);

            var dialogRect = dialogGo.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.sizeDelta = new Vector2(
                GetFloat(payload, "width", 400f),
                GetFloat(payload, "height", 300f)
            );

            var dialogImage = Undo.AddComponent<Image>(dialogGo);
            dialogImage.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

            // Add vertical layout
            var vlg = Undo.AddComponent<VerticalLayoutGroup>(dialogGo);
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.spacing = 10;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Title
            var titleGo = new GameObject("Title", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(titleGo, "Create Title");
            titleGo.transform.SetParent(dialogGo.transform, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(0, 40);
            var titleLE = Undo.AddComponent<LayoutElement>(titleGo);
            titleLE.minHeight = 40;
            titleLE.preferredHeight = 40;

            if (!TryAddTextMeshPro(titleGo, new Dictionary<string, object>
                {
                    { "text", GetString(payload, "title") ?? "Dialog Title" },
                    { "fontSize", 24 }
                }))
            {
                var titleText = Undo.AddComponent<Text>(titleGo);
                titleText.text = GetString(payload, "title") ?? "Dialog Title";
                titleText.fontSize = 24;
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.color = Color.white;
            }

            // Content area
            var contentGo = new GameObject("Content", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(contentGo, "Create Content");
            contentGo.transform.SetParent(dialogGo.transform, false);
            var contentLE = Undo.AddComponent<LayoutElement>(contentGo);
            contentLE.flexibleHeight = 1;

            // Button container
            var buttonsGo = new GameObject("Buttons", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(buttonsGo, "Create Buttons");
            buttonsGo.transform.SetParent(dialogGo.transform, false);
            var buttonsRect = buttonsGo.GetComponent<RectTransform>();
            buttonsRect.sizeDelta = new Vector2(0, 40);
            var buttonsLE = Undo.AddComponent<LayoutElement>(buttonsGo);
            buttonsLE.minHeight = 40;
            buttonsLE.preferredHeight = 40;

            var buttonsHlg = Undo.AddComponent<HorizontalLayoutGroup>(buttonsGo);
            buttonsHlg.spacing = 10;
            buttonsHlg.childAlignment = TextAnchor.MiddleRight;
            buttonsHlg.childControlWidth = false;
            buttonsHlg.childControlHeight = true;
            buttonsHlg.childForceExpandWidth = false;
            buttonsHlg.childForceExpandHeight = true;

            // OK button
            var okBtnGo = new GameObject("OKButton", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(okBtnGo, "Create OK Button");
            okBtnGo.transform.SetParent(buttonsGo.transform, false);
            var okBtnRect = okBtnGo.GetComponent<RectTransform>();
            okBtnRect.sizeDelta = new Vector2(80, 35);
            var okBtnImage = Undo.AddComponent<Image>(okBtnGo);
            okBtnImage.color = new Color(0.3f, 0.6f, 0.3f, 1f);
            var okBtn = Undo.AddComponent<Button>(okBtnGo);
            var okBtnLE = Undo.AddComponent<LayoutElement>(okBtnGo);
            okBtnLE.minWidth = 80;
            okBtnLE.preferredWidth = 80;

            var okTextGo = new GameObject("Text", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(okTextGo, "Create OK Text");
            okTextGo.transform.SetParent(okBtnGo.transform, false);
            var okTextRect = okTextGo.GetComponent<RectTransform>();
            okTextRect.anchorMin = Vector2.zero;
            okTextRect.anchorMax = Vector2.one;
            okTextRect.sizeDelta = Vector2.zero;
            if (!TryAddTextMeshPro(okTextGo, new Dictionary<string, object> { { "text", "OK" }, { "fontSize", 14 } }))
            {
                var okText = Undo.AddComponent<Text>(okTextGo);
                okText.text = "OK";
                okText.alignment = TextAnchor.MiddleCenter;
                okText.color = Color.white;
            }

            // Cancel button
            var cancelBtnGo = new GameObject("CancelButton", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(cancelBtnGo, "Create Cancel Button");
            cancelBtnGo.transform.SetParent(buttonsGo.transform, false);
            var cancelBtnRect = cancelBtnGo.GetComponent<RectTransform>();
            cancelBtnRect.sizeDelta = new Vector2(80, 35);
            var cancelBtnImage = Undo.AddComponent<Image>(cancelBtnGo);
            cancelBtnImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            var cancelBtn = Undo.AddComponent<Button>(cancelBtnGo);
            var cancelBtnLE = Undo.AddComponent<LayoutElement>(cancelBtnGo);
            cancelBtnLE.minWidth = 80;
            cancelBtnLE.preferredWidth = 80;

            var cancelTextGo = new GameObject("Text", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(cancelTextGo, "Create Cancel Text");
            cancelTextGo.transform.SetParent(cancelBtnGo.transform, false);
            var cancelTextRect = cancelTextGo.GetComponent<RectTransform>();
            cancelTextRect.anchorMin = Vector2.zero;
            cancelTextRect.anchorMax = Vector2.one;
            cancelTextRect.sizeDelta = Vector2.zero;
            if (!TryAddTextMeshPro(cancelTextGo, new Dictionary<string, object> { { "text", "Cancel" }, { "fontSize", 14 } }))
            {
                var cancelText = Undo.AddComponent<Text>(cancelTextGo);
                cancelText.text = "Cancel";
                cancelText.alignment = TextAnchor.MiddleCenter;
                cancelText.color = Color.white;
            }

            EditorSceneManager.MarkSceneDirty(dialogGo.scene);
            return CreateSuccessResponse(
                ("path", BuildGameObjectPath(dialogGo)),
                ("template", "dialog"),
                ("contentPath", BuildGameObjectPath(contentGo)),
                ("buttonsPath", BuildGameObjectPath(buttonsGo))
            );
        }

        private object CreateHUDTemplate(string parentPath, string name, Dictionary<string, object> payload)
        {
            var parent = ResolveGameObject(parentPath);

            // HUD root (stretch to fill screen)
            var hudGo = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(hudGo, "Create HUD");
            hudGo.transform.SetParent(parent.transform, false);

            var hudRect = hudGo.GetComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.sizeDelta = Vector2.zero;

            // Top bar (HP, MP, etc.)
            var topBarGo = new GameObject("TopBar", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(topBarGo, "Create TopBar");
            topBarGo.transform.SetParent(hudGo.transform, false);

            var topBarRect = topBarGo.GetComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0, 1);
            topBarRect.anchorMax = new Vector2(1, 1);
            topBarRect.pivot = new Vector2(0.5f, 1);
            topBarRect.sizeDelta = new Vector2(0, 60);

            var topBarImage = Undo.AddComponent<Image>(topBarGo);
            topBarImage.color = new Color(0, 0, 0, 0.5f);

            var topBarHlg = Undo.AddComponent<HorizontalLayoutGroup>(topBarGo);
            topBarHlg.padding = new RectOffset(20, 20, 10, 10);
            topBarHlg.spacing = 30;
            topBarHlg.childAlignment = TextAnchor.MiddleLeft;
            topBarHlg.childControlWidth = false;
            topBarHlg.childControlHeight = true;
            topBarHlg.childForceExpandWidth = false;
            topBarHlg.childForceExpandHeight = true;

            // HP display
            var hpGo = CreateStatusDisplay(topBarGo, "HP", "100/100", new Color(0.8f, 0.2f, 0.2f));

            // MP display
            var mpGo = CreateStatusDisplay(topBarGo, "MP", "50/50", new Color(0.2f, 0.4f, 0.8f));

            // Gold display
            var goldGo = CreateStatusDisplay(topBarGo, "Gold", "0", new Color(1f, 0.84f, 0f));

            // Bottom bar (action buttons)
            var bottomBarGo = new GameObject("BottomBar", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(bottomBarGo, "Create BottomBar");
            bottomBarGo.transform.SetParent(hudGo.transform, false);

            var bottomBarRect = bottomBarGo.GetComponent<RectTransform>();
            bottomBarRect.anchorMin = new Vector2(0.5f, 0);
            bottomBarRect.anchorMax = new Vector2(0.5f, 0);
            bottomBarRect.pivot = new Vector2(0.5f, 0);
            bottomBarRect.sizeDelta = new Vector2(400, 80);
            bottomBarRect.anchoredPosition = new Vector2(0, 20);

            var bottomBarImage = Undo.AddComponent<Image>(bottomBarGo);
            bottomBarImage.color = new Color(0, 0, 0, 0.5f);

            var bottomBarHlg = Undo.AddComponent<HorizontalLayoutGroup>(bottomBarGo);
            bottomBarHlg.padding = new RectOffset(10, 10, 10, 10);
            bottomBarHlg.spacing = 10;
            bottomBarHlg.childAlignment = TextAnchor.MiddleCenter;
            bottomBarHlg.childControlWidth = false;
            bottomBarHlg.childControlHeight = true;
            bottomBarHlg.childForceExpandWidth = false;
            bottomBarHlg.childForceExpandHeight = true;

            // Minimap area (top right)
            var minimapGo = new GameObject("Minimap", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(minimapGo, "Create Minimap");
            minimapGo.transform.SetParent(hudGo.transform, false);

            var minimapRect = minimapGo.GetComponent<RectTransform>();
            minimapRect.anchorMin = new Vector2(1, 1);
            minimapRect.anchorMax = new Vector2(1, 1);
            minimapRect.pivot = new Vector2(1, 1);
            minimapRect.sizeDelta = new Vector2(150, 150);
            minimapRect.anchoredPosition = new Vector2(-20, -80);

            var minimapImage = Undo.AddComponent<Image>(minimapGo);
            minimapImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            EditorSceneManager.MarkSceneDirty(hudGo.scene);
            return CreateSuccessResponse(
                ("path", BuildGameObjectPath(hudGo)),
                ("template", "hud"),
                ("topBarPath", BuildGameObjectPath(topBarGo)),
                ("bottomBarPath", BuildGameObjectPath(bottomBarGo)),
                ("minimapPath", BuildGameObjectPath(minimapGo))
            );
        }

        private GameObject CreateStatusDisplay(GameObject parent, string label, string value, Color color)
        {
            var statusGo = new GameObject(label + "Display", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(statusGo, "Create Status Display");
            statusGo.transform.SetParent(parent.transform, false);

            var statusLE = Undo.AddComponent<LayoutElement>(statusGo);
            statusLE.minWidth = 100;
            statusLE.preferredWidth = 120;

            var statusHlg = Undo.AddComponent<HorizontalLayoutGroup>(statusGo);
            statusHlg.spacing = 5;
            statusHlg.childAlignment = TextAnchor.MiddleLeft;
            statusHlg.childControlWidth = false;
            statusHlg.childControlHeight = true;
            statusHlg.childForceExpandWidth = false;
            statusHlg.childForceExpandHeight = true;

            // Icon placeholder
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(iconGo, "Create Icon");
            iconGo.transform.SetParent(statusGo.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(24, 24);
            var iconImage = Undo.AddComponent<Image>(iconGo);
            iconImage.color = color;
            var iconLE = Undo.AddComponent<LayoutElement>(iconGo);
            iconLE.minWidth = 24;
            iconLE.preferredWidth = 24;

            // Value text
            var valueGo = new GameObject("Value", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(valueGo, "Create Value");
            valueGo.transform.SetParent(statusGo.transform, false);

            if (!TryAddTextMeshPro(valueGo, new Dictionary<string, object> { { "text", value }, { "fontSize", 18 }, { "color", new Dictionary<string, object> { { "r", 1 }, { "g", 1 }, { "b", 1 }, { "a", 1 } } } }))
            {
                var valueText = Undo.AddComponent<Text>(valueGo);
                valueText.text = value;
                valueText.fontSize = 18;
                valueText.color = Color.white;
            }

            return statusGo;
        }

        private object CreateMenuTemplate(string parentPath, string name, Dictionary<string, object> payload)
        {
            var parent = ResolveGameObject(parentPath);

            // Menu root (centered vertical list)
            var menuGo = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(menuGo, "Create Menu");
            menuGo.transform.SetParent(parent.transform, false);

            var menuRect = menuGo.GetComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(0.5f, 0.5f);
            menuRect.anchorMax = new Vector2(0.5f, 0.5f);
            menuRect.pivot = new Vector2(0.5f, 0.5f);
            menuRect.sizeDelta = new Vector2(
                GetFloat(payload, "width", 300f),
                GetFloat(payload, "height", 400f)
            );

            var menuImage = Undo.AddComponent<Image>(menuGo);
            menuImage.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            var vlg = Undo.AddComponent<VerticalLayoutGroup>(menuGo);
            vlg.padding = new RectOffset(30, 30, 40, 40);
            vlg.spacing = 15;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Title
            var titleGo = new GameObject("Title", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(titleGo, "Create Title");
            titleGo.transform.SetParent(menuGo.transform, false);
            var titleLE = Undo.AddComponent<LayoutElement>(titleGo);
            titleLE.minHeight = 50;
            titleLE.preferredHeight = 50;

            if (!TryAddTextMeshPro(titleGo, new Dictionary<string, object> { { "text", GetString(payload, "title") ?? "Menu" }, { "fontSize", 32 } }))
            {
                var titleText = Undo.AddComponent<Text>(titleGo);
                titleText.text = GetString(payload, "title") ?? "Menu";
                titleText.fontSize = 32;
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.color = Color.white;
            }

            // Menu items container
            var itemsGo = new GameObject("Items", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(itemsGo, "Create Items");
            itemsGo.transform.SetParent(menuGo.transform, false);
            var itemsLE = Undo.AddComponent<LayoutElement>(itemsGo);
            itemsLE.flexibleHeight = 1;

            var itemsVlg = Undo.AddComponent<VerticalLayoutGroup>(itemsGo);
            itemsVlg.spacing = 10;
            itemsVlg.childAlignment = TextAnchor.UpperCenter;
            itemsVlg.childControlWidth = true;
            itemsVlg.childControlHeight = false;
            itemsVlg.childForceExpandWidth = true;
            itemsVlg.childForceExpandHeight = false;

            // Default menu items
            var items = new[] { "Start Game", "Options", "Exit" };
            foreach (var item in items)
            {
                CreateMenuButton(itemsGo, item);
            }

            EditorSceneManager.MarkSceneDirty(menuGo.scene);
            return CreateSuccessResponse(
                ("path", BuildGameObjectPath(menuGo)),
                ("template", "menu"),
                ("itemsPath", BuildGameObjectPath(itemsGo))
            );
        }

        private void CreateMenuButton(GameObject parent, string text)
        {
            var btnGo = new GameObject(text.Replace(" ", "") + "Button", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(btnGo, "Create Menu Button");
            btnGo.transform.SetParent(parent.transform, false);

            var btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(0, 45);

            var btnImage = Undo.AddComponent<Image>(btnGo);
            btnImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            var btn = Undo.AddComponent<Button>(btnGo);

            var btnLE = Undo.AddComponent<LayoutElement>(btnGo);
            btnLE.minHeight = 45;
            btnLE.preferredHeight = 45;

            var textGo = new GameObject("Text", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(textGo, "Create Button Text");
            textGo.transform.SetParent(btnGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            if (!TryAddTextMeshPro(textGo, new Dictionary<string, object> { { "text", text }, { "fontSize", 20 } }))
            {
                var btnText = Undo.AddComponent<Text>(textGo);
                btnText.text = text;
                btnText.fontSize = 20;
                btnText.alignment = TextAnchor.MiddleCenter;
                btnText.color = Color.white;
            }
        }

        private object CreateStatusBarTemplate(string parentPath, string name, Dictionary<string, object> payload)
        {
            var parent = ResolveGameObject(parentPath);

            // Status bar root
            var statusBarGo = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(statusBarGo, "Create StatusBar");
            statusBarGo.transform.SetParent(parent.transform, false);

            var position = GetString(payload, "position")?.ToLowerInvariant() ?? "top";
            var statusBarRect = statusBarGo.GetComponent<RectTransform>();

            if (position == "bottom")
            {
                statusBarRect.anchorMin = new Vector2(0, 0);
                statusBarRect.anchorMax = new Vector2(1, 0);
                statusBarRect.pivot = new Vector2(0.5f, 0);
            }
            else // top
            {
                statusBarRect.anchorMin = new Vector2(0, 1);
                statusBarRect.anchorMax = new Vector2(1, 1);
                statusBarRect.pivot = new Vector2(0.5f, 1);
            }
            statusBarRect.sizeDelta = new Vector2(0, GetFloat(payload, "height", 50f));

            var statusBarImage = Undo.AddComponent<Image>(statusBarGo);
            statusBarImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            var hlg = Undo.AddComponent<HorizontalLayoutGroup>(statusBarGo);
            hlg.padding = new RectOffset(15, 15, 5, 5);
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            EditorSceneManager.MarkSceneDirty(statusBarGo.scene);
            return CreateSuccessResponse(
                ("path", BuildGameObjectPath(statusBarGo)),
                ("template", "statusBar")
            );
        }

        private object CreateInventoryGridTemplate(string parentPath, string name, Dictionary<string, object> payload)
        {
            var parent = ResolveGameObject(parentPath);

            // Inventory root
            var inventoryGo = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(inventoryGo, "Create Inventory");
            inventoryGo.transform.SetParent(parent.transform, false);

            var inventoryRect = inventoryGo.GetComponent<RectTransform>();
            inventoryRect.anchorMin = new Vector2(0.5f, 0.5f);
            inventoryRect.anchorMax = new Vector2(0.5f, 0.5f);
            inventoryRect.pivot = new Vector2(0.5f, 0.5f);
            inventoryRect.sizeDelta = new Vector2(
                GetFloat(payload, "width", 400f),
                GetFloat(payload, "height", 300f)
            );

            var inventoryImage = Undo.AddComponent<Image>(inventoryGo);
            inventoryImage.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

            var vlg = Undo.AddComponent<VerticalLayoutGroup>(inventoryGo);
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 10;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Title
            var titleGo = new GameObject("Title", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(titleGo, "Create Title");
            titleGo.transform.SetParent(inventoryGo.transform, false);
            var titleLE = Undo.AddComponent<LayoutElement>(titleGo);
            titleLE.minHeight = 35;
            titleLE.preferredHeight = 35;

            if (!TryAddTextMeshPro(titleGo, new Dictionary<string, object> { { "text", GetString(payload, "title") ?? "Inventory" }, { "fontSize", 20 } }))
            {
                var titleText = Undo.AddComponent<Text>(titleGo);
                titleText.text = GetString(payload, "title") ?? "Inventory";
                titleText.fontSize = 20;
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.color = Color.white;
            }

            // Grid container with scroll
            var scrollGo = new GameObject("Scroll", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(scrollGo, "Create Scroll");
            scrollGo.transform.SetParent(inventoryGo.transform, false);

            var scrollRect = scrollGo.GetComponent<RectTransform>();
            var scrollLE = Undo.AddComponent<LayoutElement>(scrollGo);
            scrollLE.flexibleHeight = 1;

            var scroll = Undo.AddComponent<ScrollRect>(scrollGo);
            var scrollImage = Undo.AddComponent<Image>(scrollGo);
            scrollImage.color = new Color(0, 0, 0, 0.3f);

            // Viewport
            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(viewportGo, "Create Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);

            var viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.pivot = new Vector2(0, 1);

            var viewportImage = Undo.AddComponent<Image>(viewportGo);
            viewportImage.color = Color.white;
            var mask = Undo.AddComponent<Mask>(viewportGo);
            mask.showMaskGraphic = false;

            // Content (grid)
            var contentGo = new GameObject("Content", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(contentGo, "Create Content");
            contentGo.transform.SetParent(viewportGo.transform, false);

            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            var columns = GetInt(payload, "columns", 5);
            var cellSize = GetFloat(payload, "templateCellSize", 60f);

            var glg = Undo.AddComponent<GridLayoutGroup>(contentGo);
            glg.cellSize = new Vector2(cellSize, cellSize);
            glg.spacing = new Vector2(5, 5);
            glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
            glg.startAxis = GridLayoutGroup.Axis.Horizontal;
            glg.childAlignment = TextAnchor.UpperLeft;
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = columns;

            var csf = Undo.AddComponent<ContentSizeFitter>(contentGo);
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;

            // Create sample slots
            var slotCount = GetInt(payload, "slotCount", 20);
            for (int i = 0; i < slotCount; i++)
            {
                var slotGo = new GameObject($"Slot_{i}", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(slotGo, "Create Slot");
                slotGo.transform.SetParent(contentGo.transform, false);

                var slotImage = Undo.AddComponent<Image>(slotGo);
                slotImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            }

            EditorSceneManager.MarkSceneDirty(inventoryGo.scene);
            return CreateSuccessResponse(
                ("path", BuildGameObjectPath(inventoryGo)),
                ("template", "inventoryGrid"),
                ("contentPath", BuildGameObjectPath(contentGo)),
                ("slotCount", slotCount)
            );
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
            var components = new List<string>();
            var info = new Dictionary<string, object>
            {
                { "path", BuildGameObjectPath(go) },
                { "name", go.name },
                { "components", components }
            };

            // RectTransform
            var rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                info["rectTransform"] = new Dictionary<string, object>
                {
                    { "anchorMin", new Dictionary<string, object> { { "x", rectTransform.anchorMin.x }, { "y", rectTransform.anchorMin.y } } },
                    { "anchorMax", new Dictionary<string, object> { { "x", rectTransform.anchorMax.x }, { "y", rectTransform.anchorMax.y } } },
                    { "pivot", new Dictionary<string, object> { { "x", rectTransform.pivot.x }, { "y", rectTransform.pivot.y } } },
                    { "sizeDelta", new Dictionary<string, object> { { "x", rectTransform.sizeDelta.x }, { "y", rectTransform.sizeDelta.y } } },
                    { "anchoredPosition", new Dictionary<string, object> { { "x", rectTransform.anchoredPosition.x }, { "y", rectTransform.anchoredPosition.y } } }
                };
            }

            // Canvas
            var canvas = go.GetComponent<Canvas>();
            if (canvas != null)
            {
                components.Add("Canvas");
                info["renderMode"] = canvas.renderMode.ToString();
                info["sortingOrder"] = canvas.sortingOrder;
            }

            // Image
            var image = go.GetComponent<Image>();
            if (image != null)
            {
                components.Add("Image");
                info["color"] = new Dictionary<string, object>
                {
                    { "r", image.color.r },
                    { "g", image.color.g },
                    { "b", image.color.b },
                    { "a", image.color.a }
                };
                if (image.sprite != null)
                    info["sprite"] = AssetDatabase.GetAssetPath(image.sprite);
            }

            // Button
            var button = go.GetComponent<Button>();
            if (button != null)
            {
                components.Add("Button");
                info["interactable"] = button.interactable;
            }

            // Legacy Text
            var text = go.GetComponent<Text>();
            if (text != null)
            {
                components.Add("Text");
                info["text"] = text.text;
                info["fontSize"] = text.fontSize;
            }

            // TextMeshProUGUI via reflection
            var tmpTextType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (tmpTextType != null)
            {
                var tmpText = go.GetComponent(tmpTextType);
                if (tmpText != null)
                {
                    components.Add("TextMeshProUGUI");

                    var textProp = tmpTextType.GetProperty("text");
                    if (textProp != null)
                        info["text"] = textProp.GetValue(tmpText);

                    var fontSizeProp = tmpTextType.GetProperty("fontSize");
                    if (fontSizeProp != null)
                        info["fontSize"] = fontSizeProp.GetValue(tmpText);
                }
            }

            // InputField (legacy)
            var inputField = go.GetComponent<InputField>();
            if (inputField != null)
            {
                components.Add("InputField");
                info["inputText"] = inputField.text;
                info["characterLimit"] = inputField.characterLimit;
            }

            // TMP_InputField via reflection
            var tmpInputType = Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
            if (tmpInputType != null)
            {
                var tmpInput = go.GetComponent(tmpInputType);
                if (tmpInput != null)
                {
                    components.Add("TMP_InputField");
                    var tmpInputTextProp = tmpInputType.GetProperty("text");
                    if (tmpInputTextProp != null)
                        info["inputText"] = tmpInputTextProp.GetValue(tmpInput);
                    var charLimitProp = tmpInputType.GetProperty("characterLimit");
                    if (charLimitProp != null)
                        info["characterLimit"] = charLimitProp.GetValue(tmpInput);
                }
            }

            // ScrollRect
            var scrollRect = go.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                components.Add("ScrollRect");
                info["horizontal"] = scrollRect.horizontal;
                info["vertical"] = scrollRect.vertical;
                info["movementType"] = scrollRect.movementType.ToString();
            }

            // LayoutGroup
            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                components.Add("HorizontalLayoutGroup");
                info["layoutSpacing"] = hlg.spacing;
            }

            var vlg = go.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                components.Add("VerticalLayoutGroup");
                info["layoutSpacing"] = vlg.spacing;
            }

            var glg = go.GetComponent<GridLayoutGroup>();
            if (glg != null)
            {
                components.Add("GridLayoutGroup");
                info["cellSize"] = new Dictionary<string, object> { { "x", glg.cellSize.x }, { "y", glg.cellSize.y } };
                info["layoutSpacing"] = new Dictionary<string, object> { { "x", glg.spacing.x }, { "y", glg.spacing.y } };
                info["constraintCount"] = glg.constraintCount;
            }

            // ContentSizeFitter
            var csf = go.GetComponent<ContentSizeFitter>();
            if (csf != null)
            {
                components.Add("ContentSizeFitter");
                info["horizontalFit"] = csf.horizontalFit.ToString();
                info["verticalFit"] = csf.verticalFit.ToString();
            }

            // Mask
            var mask = go.GetComponent<Mask>();
            if (mask != null)
                components.Add("Mask");

            var rectMask = go.GetComponent<RectMask2D>();
            if (rectMask != null)
                components.Add("RectMask2D");

            // CanvasGroup
            var canvasGroup = go.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                components.Add("CanvasGroup");
                info["canvasGroup"] = new Dictionary<string, object>
                {
                    ["alpha"] = canvasGroup.alpha,
                    ["interactable"] = canvasGroup.interactable,
                    ["blocksRaycasts"] = canvasGroup.blocksRaycasts,
                    ["ignoreParentGroups"] = canvasGroup.ignoreParentGroups
                };
            }

            // Child count
            info["childCount"] = go.transform.childCount;

            return CreateSuccessResponse(("ui", info));
        }

        #endregion

        #region Configure CanvasGroup

        private object ConfigureCanvasGroup(Dictionary<string, object> payload)
        {
            // Resolve targets: gameObjectPaths (batch) or gameObjectPath (single)
            var targets = new List<string>();
            if (payload.TryGetValue("gameObjectPaths", out var pathsObj) && pathsObj is List<object> pathsList)
            {
                foreach (var p in pathsList)
                    targets.Add(p.ToString());
            }
            else
            {
                var singlePath = GetString(payload, "gameObjectPath");
                if (string.IsNullOrEmpty(singlePath))
                    throw new InvalidOperationException("gameObjectPath or gameObjectPaths is required for configureCanvasGroup.");
                targets.Add(singlePath);
            }

            var results = new List<Dictionary<string, object>>();

            foreach (var targetPath in targets)
            {
                var go = ResolveGameObject(targetPath);

                var canvasGroup = go.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = Undo.AddComponent<CanvasGroup>(go);
                }
                else
                {
                    Undo.RecordObject(canvasGroup, "Configure CanvasGroup");
                }

                // Partial update: only set properties that are present in payload
                if (payload.ContainsKey("alpha"))
                    canvasGroup.alpha = GetFloat(payload, "alpha", 1f);
                if (payload.ContainsKey("interactable"))
                    canvasGroup.interactable = GetBool(payload, "interactable", true);
                if (payload.ContainsKey("blocksRaycasts"))
                    canvasGroup.blocksRaycasts = GetBool(payload, "blocksRaycasts", true);
                if (payload.ContainsKey("ignoreParentGroups"))
                    canvasGroup.ignoreParentGroups = GetBool(payload, "ignoreParentGroups", false);

                EditorSceneManager.MarkSceneDirty(go.scene);

                results.Add(new Dictionary<string, object>
                {
                    ["path"] = targetPath,
                    ["alpha"] = canvasGroup.alpha,
                    ["interactable"] = canvasGroup.interactable,
                    ["blocksRaycasts"] = canvasGroup.blocksRaycasts,
                    ["ignoreParentGroups"] = canvasGroup.ignoreParentGroups
                });
            }

            if (targets.Count == 1)
            {
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["canvasGroup"] = results[0]
                };
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["canvasGroups"] = results
            };
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Applies an anchor preset to the RectTransform.
        /// Returns true if the preset is "stretchAll" (callers should skip sizeDelta assignment).
        /// </summary>
        private bool ApplyAnchorPreset(RectTransform rectTransform, Dictionary<string, object> payload)
        {
            var preset = GetString(payload, "anchorPreset")?.ToLowerInvariant() ?? "middlecenter";

            switch (preset)
            {
                case "topleft":
                    rectTransform.anchorMin = new Vector2(0, 1);
                    rectTransform.anchorMax = new Vector2(0, 1);
                    rectTransform.pivot = new Vector2(0, 1);
                    return false;
                case "topcenter":
                    rectTransform.anchorMin = new Vector2(0.5f, 1);
                    rectTransform.anchorMax = new Vector2(0.5f, 1);
                    rectTransform.pivot = new Vector2(0.5f, 1);
                    return false;
                case "topright":
                    rectTransform.anchorMin = new Vector2(1, 1);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.pivot = new Vector2(1, 1);
                    return false;
                case "middleleft":
                    rectTransform.anchorMin = new Vector2(0, 0.5f);
                    rectTransform.anchorMax = new Vector2(0, 0.5f);
                    rectTransform.pivot = new Vector2(0, 0.5f);
                    return false;
                case "middlecenter":
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    return false;
                case "middleright":
                    rectTransform.anchorMin = new Vector2(1, 0.5f);
                    rectTransform.anchorMax = new Vector2(1, 0.5f);
                    rectTransform.pivot = new Vector2(1, 0.5f);
                    return false;
                case "bottomleft":
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(0, 0);
                    rectTransform.pivot = new Vector2(0, 0);
                    return false;
                case "bottomcenter":
                    rectTransform.anchorMin = new Vector2(0.5f, 0);
                    rectTransform.anchorMax = new Vector2(0.5f, 0);
                    rectTransform.pivot = new Vector2(0.5f, 0);
                    return false;
                case "bottomright":
                    rectTransform.anchorMin = new Vector2(1, 0);
                    rectTransform.anchorMax = new Vector2(1, 0);
                    rectTransform.pivot = new Vector2(1, 0);
                    return false;
                case "stretchall":
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.sizeDelta = Vector2.zero;
                    return true;
                default:
                    throw new InvalidOperationException(
                        $"Unknown anchorPreset: '{preset}'. Use 'topLeft', 'topCenter', 'topRight', 'middleLeft', 'middleCenter', 'middleRight', 'bottomLeft', 'bottomCenter', 'bottomRight', or 'stretchAll'.");
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

            var tmpTextType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (tmpTextType == null) return false;

            var tmpInput = Undo.AddComponent(go, tmpInputType);
            if (tmpInput == null) return false;

            // Track created child objects for cleanup on failure
            var createdChildren = new List<GameObject>();

            try
            {
                // Create text area
                var textAreaGo = new GameObject("Text Area", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(textAreaGo, "Create Text Area");
                createdChildren.Add(textAreaGo);
                textAreaGo.transform.SetParent(go.transform, false);

                var textAreaRect = textAreaGo.GetComponent<RectTransform>();
                textAreaRect.anchorMin = Vector2.zero;
                textAreaRect.anchorMax = Vector2.one;
                textAreaRect.sizeDelta = new Vector2(-10, 0);

                var textAreaMask = Undo.AddComponent<RectMask2D>(textAreaGo);

                // Create text child
                var textGo = new GameObject("Text", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(textGo, "Create TMP Text");
                createdChildren.Add(textGo);
                textGo.transform.SetParent(textAreaGo.transform, false);

                var textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;

                var tmpText = Undo.AddComponent(textGo, tmpTextType);
                if (tmpText == null)
                {
                    CleanupChildren(createdChildren);
                    Undo.DestroyObjectImmediate(tmpInput);
                    return false;
                }

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
                    createdChildren.Add(placeholderGo);
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
            catch
            {
                CleanupChildren(createdChildren);
                Undo.DestroyObjectImmediate(tmpInput);
                return false;
            }
        }

        private static void CleanupChildren(List<GameObject> children)
        {
            // Destroy in reverse order to avoid parent-child issues
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i] != null)
                    Undo.DestroyObjectImmediate(children[i]);
            }
        }

        #endregion
    }
}

