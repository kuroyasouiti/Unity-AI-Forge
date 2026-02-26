"""Schema definitions for mid-level MCP tools."""

from __future__ import annotations

from typing import Any

from tools.schemas.common import schema_with_required


def transform_batch_schema() -> dict[str, Any]:
    """Schema for the unity_transform_batch MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "arrangeCircle",
                        "arrangeLine",
                        "renameSequential",
                        "renameFromList",
                        "createMenuList",
                    ],
                    "description": "Transform batch operation to perform.",
                },
                "gameObjectPaths": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Target GameObject hierarchy paths.",
                },
                "center": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Center point for circular arrangement.",
                },
                "radius": {"type": "number", "minimum": 0, "description": "Radius for circular arrangement."},
                "startAngle": {
                    "type": "number",
                    "description": "Starting angle in degrees for circular arrangement (0 = right, 90 = up).",
                },
                "clockwise": {
                    "type": "boolean",
                    "description": "Arrange objects clockwise (default: false = counter-clockwise).",
                },
                "plane": {
                    "type": "string",
                    "enum": ["XY", "XZ", "YZ"],
                    "description": "Plane for circular arrangement (default: XY for 2D, XZ for 3D top-down).",
                },
                "localSpace": {
                    "type": "boolean",
                    "description": "Use local space coordinates instead of world space.",
                },
                "startPosition": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Start position for linear arrangement.",
                },
                "endPosition": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "End position for linear arrangement.",
                },
                "spacing": {
                    "type": "number",
                    "description": "Spacing between objects for linear arrangement or menu list.",
                },
                "baseName": {
                    "type": "string",
                    "description": "Base name for sequential renaming (e.g., 'Enemy' -> Enemy_001, Enemy_002).",
                },
                "startIndex": {
                    "type": "integer",
                    "description": "Starting index for sequential renaming (default: 1).",
                },
                "padding": {
                    "type": "integer",
                    "description": "Zero-padding for sequential numbers (e.g., 3 -> 001, 002).",
                },
                "names": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Custom name list for renameFromList operation.",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path for createMenuList operation.",
                },
                "prefabPath": {
                    "type": "string",
                    "description": "Prefab asset path for createMenuList operation.",
                },
                "axis": {
                    "type": "string",
                    "enum": ["horizontal", "vertical"],
                    "description": "Layout axis for createMenuList operation.",
                },
                "offset": {
                    "type": "number",
                    "description": "Offset from parent for createMenuList operation.",
                },
            },
        },
        ["operation"],
    )


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


def camera_rig_schema() -> dict[str, Any]:
    """Schema for the unity_camera_rig MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["createRig", "updateRig", "inspect"],
                },
                "rigType": {
                    "type": "string",
                    "enum": ["follow", "orbit", "splitScreen", "fixed", "dolly"],
                    "description": "Camera rig preset type.",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path for the rig.",
                },
                "rigName": {"type": "string", "description": "Name for the camera rig."},
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject to follow/orbit.",
                },
                "offset": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Camera offset from target.",
                },
                "distance": {"type": "number", "description": "Distance from target (for orbit)."},
                "followSpeed": {"type": "number", "description": "Follow smoothing speed."},
                "lookAtTarget": {
                    "type": "boolean",
                    "description": "Whether camera should look at target.",
                },
                "fieldOfView": {"type": "number", "description": "Camera field of view."},
                "orthographic": {"type": "boolean", "description": "Use orthographic projection."},
                "orthographicSize": {"type": "number", "description": "Orthographic camera size."},
                "splitScreenIndex": {
                    "type": "integer",
                    "description": "Split screen viewport index (0-3).",
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
                        "createScrollView",
                        "addLayoutGroup",
                        "updateLayoutGroup",
                        "removeLayoutGroup",
                        "createFromTemplate",
                        "inspect",
                    ],
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path for create operations.",
                },
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject path for addLayoutGroup/updateLayoutGroup/removeLayoutGroup operations.",
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
                    "description": "UI template type for createFromTemplate operation.",
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
            },
        },
        ["operation"],
    )


def input_profile_schema() -> dict[str, Any]:
    """Schema for the unity_input_profile MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["createPlayerInput", "createInputActions", "inspect"],
                },
                "gameObjectPath": {"type": "string", "description": "Target GameObject path."},
                "preset": {
                    "type": "string",
                    "enum": ["player", "ui", "vehicle", "custom"],
                    "description": "Input profile preset type.",
                },
                "inputActionsAssetPath": {
                    "type": "string",
                    "description": "InputActions asset path.",
                },
                "defaultActionMap": {"type": "string", "description": "Default action map name."},
                "notificationBehavior": {
                    "type": "string",
                    "enum": [
                        "sendMessages",
                        "broadcastMessages",
                        "invokeUnityEvents",
                        "invokeCSharpEvents",
                    ],
                    "description": "Input notification behavior.",
                },
                "actions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": {"type": "string"},
                            "type": {"type": "string", "enum": ["button", "value", "passThrough"]},
                            "binding": {"type": "string"},
                        },
                    },
                    "description": "Custom action definitions.",
                },
            },
        },
        ["operation"],
    )


def tilemap_bundle_schema() -> dict[str, Any]:
    """Schema for the unity_tilemap_bundle MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "createTilemap",
                        "inspect",
                        "setTile",
                        "getTile",
                        "setTiles",
                        "clearTile",
                        "clearTiles",
                        "clearAllTiles",
                        "fillArea",
                        "boxFill",
                        "worldToCell",
                        "cellToWorld",
                        "updateRenderer",
                        "updateCollider",
                        "addCollider",
                        "createTile",
                        "createRuleTile",
                        "inspectTile",
                        "updateTile",
                    ],
                    "description": "Tilemap operation.",
                },
                "name": {
                    "type": "string",
                    "description": "Tilemap GameObject name (for createTilemap).",
                },
                "tilemapPath": {
                    "type": "string",
                    "description": "Tilemap GameObject hierarchy path.",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path (for createTilemap).",
                },
                "cellLayout": {
                    "type": "string",
                    "enum": ["Rectangle", "Hexagon", "Isometric", "IsometricZAsY"],
                    "description": "Grid cell layout type.",
                },
                "cellSize": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Grid cell size.",
                },
                "position": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "integer"},
                        "y": {"type": "integer"},
                        "z": {"type": "integer"},
                    },
                    "description": "Tile position (grid coordinates, integers).",
                },
                "positions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "x": {"type": "integer"},
                            "y": {"type": "integer"},
                            "z": {"type": "integer"},
                        },
                    },
                    "description": "Multiple tile positions for setTiles.",
                },
                "worldPosition": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "World position for worldToCell conversion.",
                },
                "cellPosition": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "integer"},
                        "y": {"type": "integer"},
                        "z": {"type": "integer"},
                    },
                    "description": "Cell position for cellToWorld conversion.",
                },
                "bounds": {
                    "type": "object",
                    "properties": {
                        "xMin": {"type": "integer"},
                        "yMin": {"type": "integer"},
                        "zMin": {"type": "integer"},
                        "xMax": {"type": "integer"},
                        "yMax": {"type": "integer"},
                        "zMax": {"type": "integer"},
                    },
                    "description": "Bounding box for area operations (fillArea, clearTiles, boxFill).",
                },
                "tileAssetPath": {
                    "type": "string",
                    "description": "Tile asset path (.asset file).",
                },
                "spritePath": {
                    "type": "string",
                    "description": "Sprite asset path for createTile.",
                },
                "defaultSprite": {
                    "type": "string",
                    "description": "Default sprite path for createRuleTile.",
                },
                "colliderType": {
                    "type": "string",
                    "enum": ["None", "Sprite", "Grid"],
                    "description": "Tile collider type for createTile.",
                },
                "color": {
                    "type": "object",
                    "properties": {
                        "r": {"type": "number"},
                        "g": {"type": "number"},
                        "b": {"type": "number"},
                        "a": {"type": "number"},
                    },
                    "description": "Tile color (RGBA 0-1).",
                },
                "rules": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "sprites": {
                                "type": "array",
                                "items": {"type": "string"},
                                "description": "Sprite paths for this rule.",
                            },
                            "neighbors": {"type": "object", "description": "Neighbor constraints."},
                        },
                    },
                    "description": "RuleTile tiling rules (requires 2D Tilemap Extras package).",
                },
                "sortingLayerName": {
                    "type": "string",
                    "description": "Sorting layer name for TilemapRenderer.",
                },
                "sortingOrder": {
                    "type": "integer",
                    "description": "Sorting order for TilemapRenderer.",
                },
                "mode": {
                    "type": "string",
                    "enum": ["Chunk", "Individual"],
                    "description": "TilemapRenderer mode.",
                },
                "usedByComposite": {
                    "type": "boolean",
                    "description": "Whether TilemapCollider2D is used by CompositeCollider2D.",
                },
                "usedByEffector": {
                    "type": "boolean",
                    "description": "Whether TilemapCollider2D is used by effector.",
                },
                "isTrigger": {
                    "type": "boolean",
                    "description": "Whether TilemapCollider2D is a trigger.",
                },
                "includeAllTiles": {
                    "type": "boolean",
                    "description": "Include all tile data in inspect response.",
                },
            },
        },
        ["operation"],
    )


def ui_hierarchy_schema() -> dict[str, Any]:
    """Schema for the unity_ui_hierarchy MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "clone", "inspect", "delete", "show", "hide", "toggle"],
                    "description": "UI hierarchy operation.",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path (usually Canvas or Panel).",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "Target UI hierarchy root path for inspect/delete/show/hide/toggle/clone.",
                },
                "hierarchyId": {
                    "type": "string",
                    "description": "Optional identifier for the UI hierarchy (used for referencing).",
                },
                "hierarchy": {
                    "type": "object",
                    "description": "Declarative UI hierarchy definition (recursive structure).",
                    "additionalProperties": True,
                },
                "recursive": {
                    "type": "boolean",
                    "description": "Apply visibility recursively to children (default: true).",
                },
                "interactable": {
                    "type": "boolean",
                    "description": "Set interactable state when showing/hiding.",
                },
                "blocksRaycasts": {
                    "type": "boolean",
                    "description": "Set blocksRaycasts state when showing/hiding.",
                },
                "newName": {"type": "string", "description": "New name for cloned hierarchy root."},
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
                        "deleteState",
                        "createStateGroup",
                        "transitionTo",
                        "getActiveState",
                    ],
                    "description": "UI state operation.",
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
                            "active": {
                                "type": "boolean",
                                "description": "GameObject active state.",
                            },
                            "visible": {"type": "boolean", "description": "Visible (alpha > 0)."},
                            "interactable": {
                                "type": "boolean",
                                "description": "CanvasGroup interactable.",
                            },
                            "alpha": {"type": "number", "description": "CanvasGroup alpha (0-1)."},
                            "blocksRaycasts": {
                                "type": "boolean",
                                "description": "CanvasGroup blocksRaycasts.",
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
                    "enum": ["create", "inspect", "update", "delete", "query"],
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
                "deleteGameObject": {
                    "type": "boolean",
                    "description": "If true, delete the entire GameObject instead of just the UIDocument component.",
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
                        "updateUXML",
                        "updateUSS",
                        "createPanelSettings",
                        "createFromTemplate",
                        "validateDependencies",
                    ],
                    "description": "UI Toolkit asset operation.",
                },
                "assetPath": {
                    "type": "string",
                    "description": "Asset file path (e.g., 'Assets/UI/main.uxml').",
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
                    "description": "UXML element definitions for createUXML or updateUXML (add action).",
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
                    "description": "USS rule definitions for createUSS or updateUSS.",
                },
                "action": {
                    "type": "string",
                    "enum": ["add", "remove", "replace", "update"],
                    "description": "Update action type (default: 'add'). 'update' is alias for 'add' in USS.",
                },
                "parentElementName": {
                    "type": "string",
                    "description": "Parent element name for updateUXML add action (default: root).",
                },
                "elementName": {
                    "type": "string",
                    "description": "Target element name for updateUXML remove/replace.",
                },
                "element": {
                    "type": "object",
                    "additionalProperties": True,
                    "description": "Replacement element definition for updateUXML replace action.",
                },
                "selector": {
                    "type": "string",
                    "description": "USS selector for updateUSS remove action.",
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
                    "description": "Template name for createFromTemplate.",
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
                        "reset",
                        "disable",
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
                "recursive": {
                    "type": "boolean",
                    "description": "Apply recursively for reset/disable.",
                },
            },
        },
        ["operation"],
    )
