"""Schema definitions for mid-level UI MCP tools.

Includes: ui_foundation, ui_state, ui_navigation, uitk_document, uitk_asset,
ui_convert, rectTransform_batch.
"""

from __future__ import annotations

from typing import Any

from tools.schemas.common import schema_with_required


def rect_transform_batch_schema() -> dict[str, Any]:
    """Schema for the unity_rectTransform_batch MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "setAnchors",
                        "setPivot",
                        "setSizeDelta",
                        "setAnchoredPosition",
                        "alignToParent",
                        "distributeHorizontal",
                        "distributeVertical",
                        "matchSize",
                    ],
                    "description": "RectTransform batch operation to perform.",
                },
                "gameObjectPaths": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Target GameObject hierarchy paths (must have RectTransform).",
                },
                "anchorMin": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                    },
                    "description": "Minimum anchor point (0-1). Bottom-left is (0,0), top-right is (1,1).",
                },
                "anchorMax": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                    },
                    "description": "Maximum anchor point (0-1). Set equal to anchorMin for fixed position.",
                },
                "pivot": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                    },
                    "description": "Pivot point (0-1). Center is (0.5, 0.5).",
                },
                "sizeDelta": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                    },
                    "description": "Size delta in pixels (width, height offset from anchored size).",
                },
                "anchoredPosition": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                    },
                    "description": "Position relative to anchors in pixels.",
                },
                "preset": {
                    "type": "string",
                    "enum": [
                        "topLeft",
                        "topCenter",
                        "topRight",
                        "middleLeft",
                        "middleCenter",
                        "middleRight",
                        "bottomLeft",
                        "bottomCenter",
                        "bottomRight",
                        "stretchLeft",
                        "stretchCenter",
                        "stretchRight",
                        "stretchTop",
                        "stretchMiddle",
                        "stretchBottom",
                        "stretchAll",
                    ],
                    "description": "Anchor preset for alignToParent operation.",
                },
                "spacing": {
                    "type": "number",
                    "description": "Spacing between elements for distribute operations (pixels).",
                },
                "matchWidth": {
                    "type": "boolean",
                    "description": "Match width from source element in matchSize operation.",
                },
                "matchHeight": {
                    "type": "boolean",
                    "description": "Match height from source element in matchSize operation.",
                },
                "sourceGameObjectPath": {
                    "type": "string",
                    "description": "Source element path for matchSize operation.",
                },
            },
        },
        ["operation"],
    )


def ui_foundation_schema() -> dict[str, Any]:
    """Schema for the unity_ui_foundation MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "createCanvas",
                        "createPanel",
                        "createButton",
                        "createText",
                        "createImage",
                        "createInputField",
                        "createSlider",
                        "createToggle",
                        "createScrollView",
                        "addLayoutGroup",
                        "createFromTemplate",
                        "inspect",
                        "inspectTree",
                        "extractDesignContext",
                        "show",
                        "hide",
                        "toggle",
                    ],
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path for create operations.",
                },
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject path for addLayoutGroup/show/hide/toggle/inspect/inspectTree/extractDesignContext operations.",
                },
                "name": {"type": "string", "description": "UI element name."},
                "renderMode": {
                    "type": "string",
                    "enum": ["screenSpaceOverlay", "screenSpaceCamera", "worldSpace"],
                    "description": "Canvas render mode.",
                },
                "sortingOrder": {"type": "integer", "description": "Canvas sorting order."},
                "cameraPath": {
                    "type": "string",
                    "description": "Camera GameObject path for screenSpaceCamera render mode. Falls back to Camera.main if not specified.",
                },
                "text": {"type": "string", "description": "Text content."},
                "fontSize": {"type": "integer", "minimum": 1, "description": "Font size."},
                "color": {
                    "type": "object",
                    "properties": {
                        "r": {"type": "number", "minimum": 0, "maximum": 1},
                        "g": {"type": "number", "minimum": 0, "maximum": 1},
                        "b": {"type": "number", "minimum": 0, "maximum": 1},
                        "a": {"type": "number", "minimum": 0, "maximum": 1},
                    },
                    "description": "Color (RGBA 0-1).",
                },
                "anchorPreset": {
                    "type": "string",
                    "enum": [
                        "topLeft",
                        "topCenter",
                        "topRight",
                        "middleLeft",
                        "middleCenter",
                        "middleRight",
                        "bottomLeft",
                        "bottomCenter",
                        "bottomRight",
                        "stretchAll",
                    ],
                    "description": "RectTransform anchor preset.",
                },
                "width": {"type": "number", "description": "Width of UI element."},
                "height": {"type": "number", "description": "Height of UI element."},
                "spritePath": {
                    "type": "string",
                    "description": "Sprite asset path for Image/Button.",
                },
                "placeholder": {
                    "type": "string",
                    "description": "Placeholder text for InputField.",
                },
                # Slider parameters
                "minValue": {
                    "type": "number",
                    "description": "Slider minimum value (default: 0).",
                },
                "maxValue": {
                    "type": "number",
                    "description": "Slider maximum value (default: 1).",
                },
                "value": {
                    "type": "number",
                    "description": "Slider initial value (default: 0).",
                },
                "wholeNumbers": {
                    "type": "boolean",
                    "description": "Restrict slider to whole numbers (default: false).",
                },
                # Toggle parameters
                "isOn": {
                    "type": "boolean",
                    "description": "Toggle initial state (default: true).",
                },
                "label": {
                    "type": "string",
                    "description": "Label text for Toggle.",
                },
                # ScrollView parameters
                "horizontal": {
                    "type": "boolean",
                    "description": "Enable horizontal scrolling (default: false).",
                },
                "vertical": {
                    "type": "boolean",
                    "description": "Enable vertical scrolling (default: true).",
                },
                "showScrollbar": {
                    "type": "boolean",
                    "description": "Show scrollbars for enabled scroll directions (default: true).",
                },
                "contentLayout": {
                    "type": "string",
                    "enum": ["vertical", "horizontal", "grid"],
                    "description": "Add LayoutGroup to ScrollView content with ContentSizeFitter.",
                },
                "movementType": {
                    "type": "string",
                    "enum": ["Unrestricted", "Elastic", "Clamped"],
                    "description": "ScrollView movement type (default: Elastic).",
                },
                "elasticity": {
                    "type": "number",
                    "description": "Elasticity amount for Elastic movement (default: 0.1).",
                },
                "inertia": {"type": "boolean", "description": "Enable inertia (default: true)."},
                "decelerationRate": {
                    "type": "number",
                    "description": "Deceleration rate for inertia (default: 0.135).",
                },
                "scrollSensitivity": {
                    "type": "number",
                    "description": "Scroll sensitivity (default: 1).",
                },
                # LayoutGroup parameters
                "layoutType": {
                    "type": "string",
                    "enum": ["Horizontal", "Vertical", "Grid"],
                    "description": "LayoutGroup type for addLayoutGroup operation.",
                },
                "padding": {
                    "type": "object",
                    "properties": {
                        "left": {"type": "integer"},
                        "right": {"type": "integer"},
                        "top": {"type": "integer"},
                        "bottom": {"type": "integer"},
                    },
                    "description": "LayoutGroup padding (default: 0 for all).",
                },
                "paddingAll": {
                    "type": "integer",
                    "description": "Uniform padding for all sides. Alternative to padding object.",
                },
                "spacing": {
                    "oneOf": [
                        {"type": "number"},
                        {
                            "type": "object",
                            "properties": {"x": {"type": "number"}, "y": {"type": "number"}},
                        },
                    ],
                    "description": "Spacing: number for Horizontal/Vertical layouts, {x, y} object for Grid layout.",
                },
                "spacingAll": {
                    "type": "number",
                    "description": "Uniform spacing for Grid layout (both X and Y). Alternative to spacing object.",
                },
                "childAlignment": {
                    "type": "string",
                    "enum": [
                        "UpperLeft",
                        "UpperCenter",
                        "UpperRight",
                        "MiddleLeft",
                        "MiddleCenter",
                        "MiddleRight",
                        "LowerLeft",
                        "LowerCenter",
                        "LowerRight",
                    ],
                    "description": "Child alignment within the layout (default: UpperLeft).",
                },
                "childControlWidth": {
                    "type": "boolean",
                    "description": "Control child width (default: true).",
                },
                "childControlHeight": {
                    "type": "boolean",
                    "description": "Control child height (default: true).",
                },
                "childScaleWidth": {
                    "type": "boolean",
                    "description": "Use child scale for width (default: false).",
                },
                "childScaleHeight": {
                    "type": "boolean",
                    "description": "Use child scale for height (default: false).",
                },
                "childForceExpandWidth": {
                    "type": "boolean",
                    "description": "Force expand width (default: true).",
                },
                "childForceExpandHeight": {
                    "type": "boolean",
                    "description": "Force expand height (default: true).",
                },
                "reverseArrangement": {
                    "type": "boolean",
                    "description": "Reverse child arrangement (default: false).",
                },
                # Grid-specific parameters
                "startCorner": {
                    "type": "string",
                    "enum": ["UpperLeft", "UpperRight", "LowerLeft", "LowerRight"],
                    "description": "Grid start corner (default: UpperLeft).",
                },
                "startAxis": {
                    "type": "string",
                    "enum": ["Horizontal", "Vertical"],
                    "description": "Grid start axis (default: Horizontal).",
                },
                "cellSize": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}},
                    "description": "Grid cell size (default: 100x100).",
                },
                "constraint": {
                    "type": "string",
                    "enum": ["Flexible", "FixedColumnCount", "FixedRowCount"],
                    "description": "Grid constraint mode (default: Flexible).",
                },
                "constraintCount": {
                    "type": "integer",
                    "description": "Number of rows/columns when using fixed constraint.",
                },
                # ContentSizeFitter parameters
                "addContentSizeFitter": {
                    "type": "boolean",
                    "description": "Add ContentSizeFitter to target (default: false).",
                },
                "horizontalFit": {
                    "type": "string",
                    "enum": ["Unconstrained", "MinSize", "PreferredSize"],
                    "description": "ContentSizeFitter horizontal fit mode.",
                },
                "verticalFit": {
                    "type": "string",
                    "enum": ["Unconstrained", "MinSize", "PreferredSize"],
                    "description": "ContentSizeFitter vertical fit mode.",
                },
                # Template parameters
                "templateType": {
                    "type": "string",
                    "enum": ["dialog", "hud", "menu", "statusBar", "inventoryGrid"],
                    "description": "UGUI template type for createFromTemplate operation. Note: these are UGUI-specific templates. For UI Toolkit templates, use unity_uitk_asset with templateName instead.",
                },
                "title": {
                    "type": "string",
                    "description": "Title text for dialog/menu/inventoryGrid templates.",
                },
                "position": {
                    "type": "string",
                    "enum": ["top", "bottom"],
                    "description": "Position for statusBar template (default: top).",
                },
                "columns": {
                    "type": "integer",
                    "description": "Number of columns for inventoryGrid template (default: 5).",
                },
                "templateCellSize": {
                    "type": "number",
                    "minimum": 1,
                    "description": "Cell size for inventoryGrid template (default: 60).",
                },
                "slotCount": {
                    "type": "integer",
                    "description": "Number of slots for inventoryGrid template (default: 20).",
                },
                # show/hide/toggle / createPanel CanvasGroup parameters
                "gameObjectPath": {
                    "type": "string",
                    "description": "Target GameObject path for show/hide/toggle operations.",
                },
                "gameObjectPaths": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Multiple target paths for batch show/hide/toggle operations.",
                },
                "alpha": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 1,
                    "description": "CanvasGroup alpha (0-1).",
                },
                "interactable": {
                    "type": "boolean",
                    "description": "CanvasGroup interactable.",
                },
                "blocksRaycasts": {
                    "type": "boolean",
                    "description": "CanvasGroup blocksRaycasts.",
                },
                "ignoreParentGroups": {
                    "type": "boolean",
                    "description": "CanvasGroup ignoreParentGroups.",
                },
                "addCanvasGroup": {
                    "type": "boolean",
                    "description": "Add CanvasGroup when creating Panel (default: false).",
                },
                "useCanvasGroup": {
                    "type": "boolean",
                    "description": "Deprecated, ignored. Visibility is always controlled via CanvasGroup.",
                },
                "targets": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Multiple target paths for batch show/hide/toggle operations.",
                },
                "includeChildren": {
                    "type": "boolean",
                    "description": "Include children in inspectTree (default: true).",
                },
                "maxDepth": {
                    "type": "integer",
                    "description": "Maximum depth for inspectTree (default: 10) / extractDesignContext (default: 20).",
                },
                "includeInactive": {
                    "type": "boolean",
                    "description": "Include inactive GameObjects in extractDesignContext (default: false).",
                },
            },
        },
        ["operation"],
    )


def ui_state_schema() -> dict[str, Any]:
    """Schema for the unity_ui_state MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "defineState",
                        "applyState",
                        "saveState",
                        "loadState",
                        "listStates",
                        "createStateGroup",
                        "getActiveState",
                    ],
                    "description": "UI state operation. Use defineState + applyState for gameplay/menu switching (e.g., gameplay ↔ pause, gameplay ↔ inventory). Use createStateGroup for mutually exclusive screen states.",
                },
                "stateName": {"type": "string", "description": "Name of the UI state."},
                "rootPath": {
                    "type": "string",
                    "description": "Root GameObject path for the UI state.",
                },
                "elements": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "path": {
                                "type": "string",
                                "description": "Relative path from root (empty for root itself).",
                            },
                            "visible": {
                                "type": "boolean",
                                "description": "Visible state via CanvasGroup (alpha > 0). 'active' is accepted as alias for backward compatibility.",
                            },
                            "interactable": {
                                "type": "boolean",
                                "description": "CanvasGroup interactable.",
                            },
                            "alpha": {"type": "number", "description": "CanvasGroup alpha (0-1)."},
                            "blocksRaycasts": {
                                "type": "boolean",
                                "description": "CanvasGroup blocksRaycasts.",
                            },
                            "ignoreParentGroups": {
                                "type": "boolean",
                                "description": "CanvasGroup ignoreParentGroups.",
                            },
                            "anchoredPosition": {
                                "type": "object",
                                "properties": {"x": {"type": "number"}, "y": {"type": "number"}},
                            },
                            "sizeDelta": {
                                "type": "object",
                                "properties": {"x": {"type": "number"}, "y": {"type": "number"}},
                            },
                        },
                    },
                    "description": "Element states for defineState operation.",
                },
                "includeChildren": {
                    "type": "boolean",
                    "description": "Include children when saving state (default: true).",
                },
                "maxDepth": {
                    "type": "integer",
                    "description": "Maximum depth for saveState (default: 10).",
                },
                "groupName": {"type": "string", "description": "Name for state group."},
                "states": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "List of state names for createStateGroup.",
                },
                "defaultState": {"type": "string", "description": "Default state for state group."},
            },
        },
        ["operation"],
    )


def uitk_document_schema() -> dict[str, Any]:
    """Schema for the unity_uitk_document MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "inspect", "query"],
                    "description": "UIDocument operation.",
                },
                "name": {
                    "type": "string",
                    "description": "GameObject name for create operation.",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "Target GameObject path for inspect/update/delete/query.",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path for create operation.",
                },
                "sourceAsset": {
                    "type": "string",
                    "description": "UXML VisualTreeAsset path (e.g., 'Assets/UI/main.uxml'). Set to empty string to clear.",
                },
                "panelSettings": {
                    "type": "string",
                    "description": "PanelSettings asset path. Set to empty string to clear.",
                },
                "sortingOrder": {
                    "type": "number",
                    "description": "UIDocument sorting order (higher renders on top).",
                },
                "maxDepth": {
                    "type": "integer",
                    "description": "Max depth for VisualElement tree inspection (default: 5).",
                },
                "queryName": {
                    "type": "string",
                    "description": "Find element by name (UQuery).",
                },
                "queryClass": {
                    "type": "string",
                    "description": "Find elements by USS class name.",
                },
                "queryType": {
                    "type": "string",
                    "description": "Find elements by type name (e.g., 'Button', 'Label').",
                },
                "maxResults": {
                    "type": "integer",
                    "description": "Maximum results for query (default: 100).",
                },
            },
        },
        ["operation"],
    )


def uitk_asset_schema() -> dict[str, Any]:
    """Schema for the unity_uitk_asset MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "createUXML",
                        "createUSS",
                        "inspectUXML",
                        "inspectUSS",
                        "createPanelSettings",
                        "createFromTemplate",
                        "validateDependencies",
                        "auditUSS",
                        "auditUXML",
                    ],
                    "description": "UI Toolkit asset operation.",
                },
                "assetPath": {
                    "type": "string",
                    "description": "Asset file path (e.g., 'Assets/UI/main.uxml').",
                },
                "searchPath": {
                    "type": "string",
                    "description": "Folder path to scan for USS/UXML files (e.g., 'Assets/UI/USS'). Alternative to assetPath for batch auditing.",
                },
                "elements": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "type": {
                                "type": "string",
                                "description": "UXML element type (e.g., VisualElement, Button, Label, TextField, Toggle, Slider, ScrollView, ProgressBar, Image, DropdownField, etc.).",
                            },
                            "name": {"type": "string", "description": "Element name attribute."},
                            "classes": {
                                "type": "array",
                                "items": {"type": "string"},
                                "description": "USS class names.",
                            },
                            "text": {"type": "string", "description": "Text content."},
                            "style": {
                                "type": "object",
                                "additionalProperties": {"type": "string"},
                                "description": "Inline style properties (e.g., {'width': '200px', 'height': '50px'}).",
                            },
                            "attributes": {
                                "type": "object",
                                "additionalProperties": {"type": "string"},
                                "description": "Additional UXML attributes (e.g., {'tooltip': 'hint', 'value': '10'}).",
                            },
                            "children": {
                                "type": "array",
                                "description": "Nested child elements (recursive).",
                            },
                        },
                    },
                    "description": "UXML element definitions for createUXML.",
                },
                "styleSheets": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "USS stylesheet paths to reference in UXML.",
                },
                "rules": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "selector": {
                                "type": "string",
                                "description": "USS selector (e.g., '.primary', '#my-button', 'Button').",
                            },
                            "properties": {
                                "type": "object",
                                "additionalProperties": {"type": "string"},
                                "description": "USS property-value pairs (e.g., {'background-color': '#2d5a27', 'font-size': '16px'}).",
                            },
                        },
                    },
                    "description": "USS rule definitions for createUSS.",
                },
                "scaleMode": {
                    "type": "string",
                    "enum": ["constantPixelSize", "scaleWithScreenSize", "constantPhysicalSize"],
                    "description": "PanelSettings scale mode (default: scaleWithScreenSize).",
                },
                "referenceResolution": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}},
                    "description": "Reference resolution for PanelSettings (default: 1920x1080).",
                },
                "match": {
                    "type": "number",
                    "description": "Width/height match for PanelSettings (0=width, 1=height, 0.5=both).",
                },
                "themeStyleSheet": {
                    "type": "string",
                    "description": "Theme StyleSheet asset path for PanelSettings.",
                },
                "templateName": {
                    "type": "string",
                    "enum": ["menu", "dialog", "hud", "settings", "inventory"],
                    "description": "UI Toolkit template name for createFromTemplate. Note: these are UI Toolkit-specific templates (UXML/USS). For UGUI templates, use unity_ui_foundation with templateType instead.",
                },
                "outputDir": {
                    "type": "string",
                    "description": "Output directory for template files (default: 'Assets/UI').",
                },
                "prefix": {
                    "type": "string",
                    "description": "File name prefix for template output (default: template name).",
                },
                "title": {"type": "string", "description": "Title text for templates."},
                "message": {"type": "string", "description": "Message text for dialog template."},
                "buttons": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Button labels for menu template.",
                },
                "columns": {
                    "type": "integer",
                    "description": "Grid columns for inventory template (default: 4).",
                },
                "slotCount": {
                    "type": "integer",
                    "description": "Number of slots for inventory template (default: 16).",
                },
            },
        },
        ["operation"],
    )


def ui_navigation_schema() -> dict[str, Any]:
    """Schema for the unity_ui_navigation MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "configure",
                        "setExplicit",
                        "autoSetup",
                        "createGroup",
                        "setFirstSelected",
                        "inspect",
                    ],
                    "description": "UI navigation operation.",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "Target Selectable GameObject path.",
                },
                "rootPath": {"type": "string", "description": "Root path for autoSetup operation."},
                "mode": {
                    "type": "string",
                    "enum": ["none", "horizontal", "vertical", "automatic", "explicit"],
                    "description": "Navigation mode.",
                },
                "wrapAround": {"type": "boolean", "description": "Enable wrap-around navigation."},
                "direction": {
                    "type": "string",
                    "enum": ["vertical", "horizontal", "grid", "both"],
                    "description": "Navigation direction for autoSetup/createGroup.",
                },
                "columns": {
                    "type": "integer",
                    "description": "Number of columns for grid navigation.",
                },
                "includeDisabled": {
                    "type": "boolean",
                    "description": "Include disabled Selectables in autoSetup.",
                },
                "up": {"type": "string", "description": "Path for selectOnUp (explicit)."},
                "down": {"type": "string", "description": "Path for selectOnDown (explicit)."},
                "left": {"type": "string", "description": "Path for selectOnLeft (explicit)."},
                "right": {"type": "string", "description": "Path for selectOnRight (explicit)."},
                "groupName": {"type": "string", "description": "Name for navigation group."},
                "elements": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "List of element paths for createGroup.",
                },
                "isolate": {
                    "type": "boolean",
                    "description": "Isolate group navigation (default: true).",
                },
            },
        },
        ["operation"],
    )


def ui_convert_schema() -> dict[str, Any]:
    """Schema for the unity_ui_convert MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "analyze",
                        "toUITK",
                        "toUGUI",
                        "extractStyles",
                        "extractTokens",
                    ],
                    "description": (
                        "UI conversion operation.\n"
                        "- analyze: Analyze UGUI or UITK source and report conversion feasibility, "
                        "element mapping, warnings, and unsupported elements\n"
                        "- toUITK: Convert UGUI Canvas hierarchy to UXML + USS files\n"
                        "- toUGUI: Convert UXML file to UGUI Canvas hierarchy in scene\n"
                        "- extractStyles: Extract styles from UGUI Canvas hierarchy to USS file only\n"
                        "- extractTokens: Scan UGUI hierarchy and extract deduplicated design tokens "
                        "(color palette, font sizes, font families, spacing, element sizes) "
                        "with usage counts and near-duplicate detection"
                    ),
                },
                "sourceType": {
                    "type": "string",
                    "enum": ["ugui", "uitk"],
                    "description": (
                        "Source UI framework type (required for analyze).\n"
                        "- ugui: Analyze Canvas hierarchy for conversion to UI Toolkit (UXML/USS)\n"
                        "- uitk: Analyze UXML file for conversion to UGUI (Canvas)"
                    ),
                },
                "sourcePath": {
                    "type": "string",
                    "description": (
                        "Source path.\n"
                        "- For ugui/toUITK/extractStyles: GameObject path of the Canvas (e.g., 'Canvas', 'BattleCanvas')\n"
                        "- For uitk/toUGUI: Asset path of the UXML file (e.g., 'Assets/UI/Menu.uxml')"
                    ),
                },
                "outputDir": {
                    "type": "string",
                    "description": (
                        "Output directory for toUITK operation (default: 'Assets/UI/Generated')."
                    ),
                },
                "outputName": {
                    "type": "string",
                    "description": (
                        "Output file name (without extension) for toUITK operation (default: 'ConvertedUI')."
                    ),
                },
                "outputPath": {
                    "type": "string",
                    "description": (
                        "Output USS file path for extractStyles operation (e.g., 'Assets/UI/extracted.uss')."
                    ),
                },
                "parentPath": {
                    "type": "string",
                    "description": (
                        "Parent GameObject path for toUGUI operation. "
                        "Empty string for scene root (default: '')."
                    ),
                },
                "canvasRenderMode": {
                    "type": "string",
                    "enum": ["screenSpaceOverlay", "screenSpaceCamera", "worldSpace"],
                    "description": (
                        "Canvas render mode for toUGUI operation (default: 'screenSpaceOverlay')."
                    ),
                },
            },
        },
        ["operation"],
    )
