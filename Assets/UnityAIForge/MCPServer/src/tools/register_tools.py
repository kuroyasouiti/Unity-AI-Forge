from __future__ import annotations

from typing import Any

import mcp.types as types
from mcp.server import Server

from bridge.bridge_manager import bridge_manager
from logger import logger
from utils.json_utils import as_pretty_json
from tools.batch_sequential import TOOL as batch_sequential_tool, handle_batch_sequential


def _ensure_bridge_connected() -> None:
    if not bridge_manager.is_connected():
        raise RuntimeError(
            "Unity bridge is not connected. In the Unity Editor choose Tools/MCP Assistant to start the bridge."
        )


async def _call_bridge_tool(tool_name: str, payload: dict[str, Any]) -> list[types.Content]:
    _ensure_bridge_connected()

    timeout_ms = 45_000
    if "timeoutSeconds" in payload:
        unity_timeout = payload["timeoutSeconds"]
        timeout_ms = (unity_timeout + 20) * 1000

    try:
        response = await bridge_manager.send_command(tool_name, payload, timeout_ms=timeout_ms)
    except Exception as exc:  # pragma: no cover - surface bridge errors to client
        raise RuntimeError(f'Unity bridge tool "{tool_name}" failed: {exc}') from exc

    text = response if isinstance(response, str) else as_pretty_json(response)
    return [types.TextContent(type="text", text=text)]


def _schema_with_required(schema: dict[str, Any], required: list[str]) -> dict[str, Any]:
    enriched = dict(schema)
    enriched["required"] = required
    enriched["additionalProperties"] = False
    return enriched


def register_tools(server: Server) -> None:
    ping_schema: dict[str, Any] = {
        "type": "object",
        "properties": {},
        "additionalProperties": False,
    }

    scene_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "load",
                        "save",
                        "delete",
                        "duplicate",
                        "inspect",
                        "listBuildSettings",
                        "addToBuildSettings",
                        "removeFromBuildSettings",
                        "reorderBuildSettings",
                        "setBuildSettingsEnabled",
                    ],
                },
                "scenePath": {"type": "string"},
                "newSceneName": {"type": "string"},
                "additive": {"type": "boolean"},
                "includeOpenScenes": {"type": "boolean"},
                "includeHierarchy": {"type": "boolean"},
                "includeComponents": {"type": "boolean"},
                "filter": {"type": "string"},
                "enabled": {"type": "boolean"},
                "index": {"type": "integer"},
                "fromIndex": {"type": "integer"},
                "toIndex": {"type": "integer"},
            },
        },
        ["operation"],
    )

    game_object_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "delete",
                        "move",
                        "rename",
                        "update",
                        "duplicate",
                        "inspect",
                        "findMultiple",
                        "deleteMultiple",
                        "inspectMultiple",
                    ],
                },
                "gameObjectPath": {"type": "string"},
                "parentPath": {"type": "string"},
                "template": {"type": "string"},
                "name": {"type": "string"},
                "tag": {"type": "string"},
                "layer": {"oneOf": [{"type": "integer"}, {"type": "string"}]},
                "active": {"type": "boolean"},
                "static": {"type": "boolean"},
                "pattern": {"type": "string"},
                "useRegex": {"type": "boolean"},
                "includeComponents": {"type": "boolean"},
                "maxResults": {"type": "integer"},
                "components": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "type": {"type": "string", "description": "Component type name (e.g., 'UnityEngine.Rigidbody2D', 'UnityEngine.BoxCollider2D')."},
                            "properties": {"type": "object", "additionalProperties": True, "description": "Property values to set on the component."},
                        },
                        "required": ["type"],
                    },
                    "description": "Components to automatically attach when creating a GameObject. Each item specifies a component type and optional property values.",
                },
            },
        },
        ["operation"],
    )

    component_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "add",
                        "remove",
                        "update",
                        "inspect",
                        "addMultiple",
                        "removeMultiple",
                        "updateMultiple",
                        "inspectMultiple",
                    ],
                },
                "gameObjectPath": {"type": "string"},
                "gameObjectGlobalObjectId": {"type": "string"},
                "componentType": {"type": "string"},
                "propertyChanges": {"type": "object", "additionalProperties": True},
                "applyDefaults": {"type": "boolean"},
                "pattern": {"type": "string"},
                "useRegex": {"type": "boolean"},
                "includeProperties": {"type": "boolean"},
                "propertyFilter": {"type": "array", "items": {"type": "string"}},
                "maxResults": {"type": "integer"},
                "stopOnError": {"type": "boolean"},
            },
        },
        ["operation", "componentType"],
    )

    asset_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "updateImporter",
                        "delete",
                        "rename",
                        "duplicate",
                        "inspect",
                        "findMultiple",
                        "deleteMultiple",
                        "inspectMultiple",
                    ],
                },
                "assetPath": {"type": "string"},
                "assetGuid": {"type": "string"},
                "content": {"type": "string"},
                "destinationPath": {"type": "string"},
                "propertyChanges": {"type": "object", "additionalProperties": True},
                "pattern": {"type": "string"},
                "useRegex": {"type": "boolean"},
                "includeProperties": {"type": "boolean"},
                "maxResults": {"type": "integer"},
            },
        },
        ["operation"],
    )

    project_settings_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["read", "write", "list", "addSceneToBuild", "removeSceneFromBuild", "listBuildScenes", "reorderBuildScenes", "setBuildSceneEnabled"],
                },
                "category": {
                    "type": "string",
                    "enum": ["player", "quality", "time", "physics", "physics2d", "audio", "editor", "tagsLayers"],
                },
                "property": {"type": "string"},
                "value": {},
                "scenePath": {"type": "string", "description": "Path to scene file for build settings operations"},
                "index": {"type": "integer", "description": "Scene index for build settings operations"},
                "fromIndex": {"type": "integer", "description": "Source index for reordering build scenes"},
                "toIndex": {"type": "integer", "description": "Target index for reordering build scenes"},
                "enabled": {"type": "boolean", "description": "Whether scene is enabled in build settings"},
            },
        },
        ["operation"],
    )

    scriptable_object_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "inspect",
                        "update",
                        "delete",
                        "duplicate",
                        "list",
                        "findByType",
                    ],
                },
                "typeName": {"type": "string"},
                "assetPath": {"type": "string"},
                "assetGuid": {"type": "string"},
                "properties": {"type": "object", "additionalProperties": True},
                "includeProperties": {"type": "boolean"},
                "propertyFilter": {"type": "array", "items": {"type": "string"}},
                "searchPath": {"type": "string"},
                "maxResults": {"type": "integer"},
                "offset": {"type": "integer"},
                "sourceAssetPath": {"type": "string"},
                "sourceAssetGuid": {"type": "string"},
                "destinationAssetPath": {"type": "string"},
            },
        },
        ["operation"],
    )

    prefab_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "inspect",
                        "instantiate",
                        "unpack",
                        "applyOverrides",
                        "revertOverrides",
                    ],
                    "description": "Prefab operation to perform.",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "Hierarchy path of GameObject (for create/update/unpack operations).",
                },
                "prefabPath": {
                    "type": "string",
                    "description": "Asset path to prefab file (e.g., 'Assets/Prefabs/MyPrefab.prefab').",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path for instantiation.",
                },
                "position": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Position for instantiated prefab.",
                },
                "rotation": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Euler rotation for instantiated prefab.",
                },
                "unpackMode": {
                    "type": "string",
                    "enum": ["completely", "outermost"],
                    "description": "Unpack mode: 'completely' or 'outermost'.",
                },
                "includeOverrides": {
                    "type": "boolean",
                    "description": "Include override information in inspect operation.",
                },
            },
        },
        ["operation"],
    )

    vector_sprite_convert_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "primitiveToSprite",
                        "svgToSprite",
                        "textureToSprite",
                        "createColorSprite",
                    ],
                    "description": "Vector/sprite conversion operation.",
                },
                "primitiveType": {
                    "type": "string",
                    "enum": ["square", "circle", "triangle", "polygon"],
                    "description": "Primitive shape type for sprite generation.",
                },
                "width": {
                    "type": "integer",
                    "description": "Width of generated sprite in pixels.",
                },
                "height": {
                    "type": "integer",
                    "description": "Height of generated sprite in pixels.",
                },
                "color": {
                    "type": "object",
                    "properties": {
                        "r": {"type": "number", "minimum": 0, "maximum": 1},
                        "g": {"type": "number", "minimum": 0, "maximum": 1},
                        "b": {"type": "number", "minimum": 0, "maximum": 1},
                        "a": {"type": "number", "minimum": 0, "maximum": 1},
                    },
                    "description": "RGBA color (0-1 range).",
                },
                "sides": {
                    "type": "integer",
                    "description": "Number of sides for polygon primitive.",
                },
                "svgPath": {
                    "type": "string",
                    "description": "Path to SVG file for conversion.",
                },
                "texturePath": {
                    "type": "string",
                    "description": "Path to texture file for sprite conversion.",
                },
                "outputPath": {
                    "type": "string",
                    "description": "Output path for generated sprite asset.",
                },
                "pixelsPerUnit": {
                    "type": "number",
                    "description": "Pixels per unit for sprite import settings.",
                },
                "spriteMode": {
                    "type": "string",
                    "enum": ["single", "multiple"],
                    "description": "Sprite mode: 'single' or 'multiple'.",
                },
            },
        },
        ["operation"],
    )

    transform_batch_schema = _schema_with_required(
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
                },
                "radius": {"type": "number"},
                "startAngle": {"type": "number"},
                "clockwise": {"type": "boolean"},
                "plane": {"type": "string", "enum": ["XY", "XZ", "YZ"]},
                "localSpace": {"type": "boolean"},
                "startPosition": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                },
                "endPosition": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                },
                "spacing": {"type": "number"},
                "baseName": {"type": "string"},
                "startIndex": {"type": "integer"},
                "padding": {"type": "integer"},
                "names": {"type": "array", "items": {"type": "string"}},
                "parentPath": {"type": "string"},
                "prefabPath": {"type": "string"},
                "axis": {"type": "string", "enum": ["horizontal", "vertical"]},
                "offset": {"type": "number"},
            },
        },
        ["operation"],
    )

    rect_transform_batch_schema = _schema_with_required(
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
                },
                "anchorMax": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                    },
                },
                "pivot": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                    },
                },
                "sizeDelta": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                    },
                },
                "anchoredPosition": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                    },
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
                },
                "spacing": {"type": "number"},
                "matchWidth": {"type": "boolean"},
                "matchHeight": {"type": "boolean"},
                "sourceGameObjectPath": {"type": "string"},
            },
        },
        ["operation"],
    )

    physics_bundle_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "applyPreset2D",
                        "applyPreset3D",
                        "updateRigidbody2D",
                        "updateRigidbody3D",
                        "updateCollider2D",
                        "updateCollider3D",
                        "inspect",
                    ],
                },
                "gameObjectPaths": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Target GameObject hierarchy paths.",
                },
                "preset": {
                    "type": "string",
                    "enum": [
                        "dynamic",
                        "kinematic",
                        "static",
                        "character",
                        "platformer",
                        "topDown",
                        "vehicle",
                        "projectile",
                    ],
                    "description": "Physics preset template.",
                },
                "colliderType": {
                    "type": "string",
                    "enum": ["box", "sphere", "capsule", "mesh", "circle", "polygon", "edge"],
                    "description": "Collider type to add (2D: box/circle/polygon/edge, 3D: box/sphere/capsule/mesh).",
                },
                "isTrigger": {"type": "boolean"},
                "rigidbodyType": {
                    "type": "string",
                    "enum": ["dynamic", "kinematic", "static"],
                },
                "mass": {"type": "number"},
                "drag": {"type": "number"},
                "angularDrag": {"type": "number"},
                "gravityScale": {"type": "number"},
                "useGravity": {"type": "boolean"},
                "isKinematic": {"type": "boolean"},
                "interpolate": {
                    "type": "string",
                    "enum": ["none", "interpolate", "extrapolate"],
                },
                "collisionDetection": {
                    "type": "string",
                    "enum": ["discrete", "continuous", "continuousDynamic", "continuousSpeculative"],
                },
                "constraints": {
                    "type": "object",
                    "properties": {
                        "freezePositionX": {"type": "boolean"},
                        "freezePositionY": {"type": "boolean"},
                        "freezePositionZ": {"type": "boolean"},
                        "freezeRotationX": {"type": "boolean"},
                        "freezeRotationY": {"type": "boolean"},
                        "freezeRotationZ": {"type": "boolean"},
                    },
                },
                "size": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Collider size (Vector2 for 2D, Vector3 for 3D).",
                },
                "center": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Collider center offset.",
                },
                "radius": {"type": "number", "description": "Radius for sphere/circle/capsule colliders."},
                "height": {"type": "number", "description": "Height for capsule colliders."},
                "material": {"type": "string", "description": "Physics material asset path."},
            },
        },
        ["operation"],
    )

    camera_rig_schema = _schema_with_required(
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
                "parentPath": {"type": "string", "description": "Parent GameObject path for the rig."},
                "rigName": {"type": "string", "description": "Name for the camera rig."},
                "targetPath": {"type": "string", "description": "Target GameObject to follow/orbit."},
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
                "lookAtTarget": {"type": "boolean", "description": "Whether camera should look at target."},
                "fieldOfView": {"type": "number", "description": "Camera field of view."},
                "orthographic": {"type": "boolean", "description": "Use orthographic projection."},
                "orthographicSize": {"type": "number", "description": "Orthographic camera size."},
                "splitScreenIndex": {"type": "integer", "description": "Split screen viewport index (0-3)."},
            },
        },
        ["operation"],
    )

    ui_foundation_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "createCanvas", "createPanel", "createButton", "createText",
                        "createImage", "createInputField", "createScrollView",
                        "addLayoutGroup", "updateLayoutGroup", "removeLayoutGroup",
                        "createFromTemplate", "inspect",
                    ],
                },
                "parentPath": {"type": "string", "description": "Parent GameObject path."},
                "name": {"type": "string", "description": "UI element name."},
                "renderMode": {
                    "type": "string",
                    "enum": ["screenSpaceOverlay", "screenSpaceCamera", "worldSpace"],
                    "description": "Canvas render mode.",
                },
                "sortingOrder": {"type": "integer", "description": "Canvas sorting order."},
                "text": {"type": "string", "description": "Text content."},
                "fontSize": {"type": "integer", "description": "Font size."},
                "color": {
                    "type": "object",
                    "properties": {
                        "r": {"type": "number"},
                        "g": {"type": "number"},
                        "b": {"type": "number"},
                        "a": {"type": "number"},
                    },
                    "description": "Color (RGBA 0-1).",
                },
                "anchorPreset": {
                    "type": "string",
                    "enum": [
                        "topLeft", "topCenter", "topRight",
                        "middleLeft", "middleCenter", "middleRight",
                        "bottomLeft", "bottomCenter", "bottomRight",
                        "stretchAll",
                    ],
                    "description": "RectTransform anchor preset.",
                },
                "width": {"type": "number", "description": "Width of UI element."},
                "height": {"type": "number", "description": "Height of UI element."},
                "spritePath": {"type": "string", "description": "Sprite asset path for Image/Button."},
                "placeholder": {"type": "string", "description": "Placeholder text for InputField."},
                # ScrollView parameters
                "horizontal": {"type": "boolean", "description": "Enable horizontal scrolling (default: false)."},
                "vertical": {"type": "boolean", "description": "Enable vertical scrolling (default: true)."},
                "horizontalScrollbar": {"type": "boolean", "description": "Show horizontal scrollbar (default: false)."},
                "verticalScrollbar": {"type": "boolean", "description": "Show vertical scrollbar (default: true)."},
                "movementType": {
                    "type": "string",
                    "enum": ["Unrestricted", "Elastic", "Clamped"],
                    "description": "ScrollView movement type (default: Elastic).",
                },
                "elasticity": {"type": "number", "description": "Elasticity amount for Elastic movement (default: 0.1)."},
                "inertia": {"type": "boolean", "description": "Enable inertia (default: true)."},
                "decelerationRate": {"type": "number", "description": "Deceleration rate for inertia (default: 0.135)."},
                "scrollSensitivity": {"type": "number", "description": "Scroll sensitivity (default: 1)."},
                "viewportSize": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}},
                    "description": "Viewport size (width, height).",
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
                "spacing": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}},
                    "description": "Spacing between elements (x for Horizontal/Grid, y for Vertical/Grid).",
                },
                "childAlignment": {
                    "type": "string",
                    "enum": [
                        "UpperLeft", "UpperCenter", "UpperRight",
                        "MiddleLeft", "MiddleCenter", "MiddleRight",
                        "LowerLeft", "LowerCenter", "LowerRight",
                    ],
                    "description": "Child alignment within the layout (default: UpperLeft).",
                },
                "childControlWidth": {"type": "boolean", "description": "Control child width (default: true)."},
                "childControlHeight": {"type": "boolean", "description": "Control child height (default: true)."},
                "childScaleWidth": {"type": "boolean", "description": "Use child scale for width (default: false)."},
                "childScaleHeight": {"type": "boolean", "description": "Use child scale for height (default: false)."},
                "childForceExpandWidth": {"type": "boolean", "description": "Force expand width (default: true)."},
                "childForceExpandHeight": {"type": "boolean", "description": "Force expand height (default: true)."},
                "reverseArrangement": {"type": "boolean", "description": "Reverse child arrangement (default: false)."},
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
                "constraintCount": {"type": "integer", "description": "Number of rows/columns when using fixed constraint."},
                # ContentSizeFitter parameters
                "addContentSizeFitter": {"type": "boolean", "description": "Add ContentSizeFitter to target (default: false)."},
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
                "templateOptions": {
                    "type": "object",
                    "additionalProperties": True,
                    "description": "Template-specific options (varies by template type).",
                },
            },
        },
        ["operation"],
    )

    audio_source_bundle_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["createAudioSource", "updateAudioSource", "inspect"],
                },
                "gameObjectPath": {"type": "string", "description": "Target GameObject path."},
                "preset": {
                    "type": "string",
                    "enum": ["music", "sfx", "ambient", "voice", "ui", "custom"],
                    "description": "Audio source preset type.",
                },
                "audioClipPath": {"type": "string", "description": "AudioClip asset path."},
                "volume": {"type": "number", "description": "Volume (0-1)."},
                "pitch": {"type": "number", "description": "Pitch (-3 to 3)."},
                "loop": {"type": "boolean", "description": "Loop playback."},
                "playOnAwake": {"type": "boolean", "description": "Play on awake."},
                "spatialBlend": {"type": "number", "description": "2D/3D blend (0=2D, 1=3D)."},
                "minDistance": {"type": "number", "description": "Min distance for 3D sound."},
                "maxDistance": {"type": "number", "description": "Max distance for 3D sound."},
                "priority": {"type": "integer", "description": "Priority (0-256, 0=highest)."},
                "mixerGroupPath": {"type": "string", "description": "Audio mixer group asset path."},
            },
        },
        ["operation"],
    )

    input_profile_schema = _schema_with_required(
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
                "inputActionsAssetPath": {"type": "string", "description": "InputActions asset path."},
                "defaultActionMap": {"type": "string", "description": "Default action map name."},
                "notificationBehavior": {
                    "type": "string",
                    "enum": ["sendMessages", "broadcastMessages", "invokeUnityEvents", "invokeCSharpEvents"],
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

    character_controller_bundle_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["applyPreset", "update", "inspect"],
                },
                "gameObjectPath": {"type": "string", "description": "Target GameObject path."},
                "gameObjectPaths": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Multiple GameObject paths for batch operations.",
                },
                "preset": {
                    "type": "string",
                    "enum": ["fps", "tps", "platformer", "child", "large", "narrow", "custom"],
                    "description": "CharacterController preset type.",
                },
                "radius": {"type": "number", "description": "Capsule radius."},
                "height": {"type": "number", "description": "Capsule height."},
                "center": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    "description": "Center offset of the capsule.",
                },
                "slopeLimit": {"type": "number", "description": "Maximum slope angle in degrees."},
                "stepOffset": {"type": "number", "description": "Maximum step height."},
                "skinWidth": {"type": "number", "description": "Skin width for collision detection."},
                "minMoveDistance": {"type": "number", "description": "Minimum move distance threshold."},
            },
        },
        ["operation"],
    )

    tilemap_bundle_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "createTilemap", "inspect",
                        "setTile", "getTile", "setTiles",
                        "clearTile", "clearTiles", "clearAllTiles",
                        "fillArea", "boxFill",
                        "worldToCell", "cellToWorld",
                        "updateRenderer", "updateCollider", "addCollider",
                        "createTile", "createRuleTile", "inspectTile", "updateTile",
                    ],
                    "description": "Tilemap operation.",
                },
                "name": {"type": "string", "description": "Tilemap GameObject name (for createTilemap)."},
                "tilemapPath": {"type": "string", "description": "Tilemap GameObject hierarchy path."},
                "parentPath": {"type": "string", "description": "Parent GameObject path (for createTilemap)."},
                "cellLayout": {
                    "type": "string",
                    "enum": ["Rectangle", "Hexagon", "Isometric", "IsometricZAsY"],
                    "description": "Grid cell layout type.",
                },
                "cellSize": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    "description": "Grid cell size.",
                },
                "position": {
                    "type": "object",
                    "properties": {"x": {"type": "integer"}, "y": {"type": "integer"}, "z": {"type": "integer"}},
                    "description": "Tile position (grid coordinates, integers).",
                },
                "positions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {"x": {"type": "integer"}, "y": {"type": "integer"}, "z": {"type": "integer"}},
                    },
                    "description": "Multiple tile positions for setTiles.",
                },
                "worldPosition": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    "description": "World position for worldToCell conversion.",
                },
                "cellPosition": {
                    "type": "object",
                    "properties": {"x": {"type": "integer"}, "y": {"type": "integer"}, "z": {"type": "integer"}},
                    "description": "Cell position for cellToWorld conversion.",
                },
                "bounds": {
                    "type": "object",
                    "properties": {
                        "xMin": {"type": "integer"}, "yMin": {"type": "integer"}, "zMin": {"type": "integer"},
                        "xMax": {"type": "integer"}, "yMax": {"type": "integer"}, "zMax": {"type": "integer"},
                    },
                    "description": "Bounding box for area operations (fillArea, clearTiles, boxFill).",
                },
                "tileAssetPath": {"type": "string", "description": "Tile asset path (.asset file)."},
                "spritePath": {"type": "string", "description": "Sprite asset path for createTile."},
                "defaultSprite": {"type": "string", "description": "Default sprite path for createRuleTile."},
                "colliderType": {
                    "type": "string",
                    "enum": ["None", "Sprite", "Grid"],
                    "description": "Tile collider type for createTile.",
                },
                "color": {
                    "type": "object",
                    "properties": {"r": {"type": "number"}, "g": {"type": "number"}, "b": {"type": "number"}, "a": {"type": "number"}},
                    "description": "Tile color (RGBA 0-1).",
                },
                "rules": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "sprites": {"type": "array", "items": {"type": "string"}, "description": "Sprite paths for this rule."},
                            "neighbors": {"type": "object", "description": "Neighbor constraints."},
                        },
                    },
                    "description": "RuleTile tiling rules (requires 2D Tilemap Extras package).",
                },
                "sortingLayerName": {"type": "string", "description": "Sorting layer name for TilemapRenderer."},
                "sortingOrder": {"type": "integer", "description": "Sorting order for TilemapRenderer."},
                "mode": {
                    "type": "string",
                    "enum": ["Chunk", "Individual"],
                    "description": "TilemapRenderer mode.",
                },
                "usedByComposite": {"type": "boolean", "description": "Whether TilemapCollider2D is used by CompositeCollider2D."},
                "usedByEffector": {"type": "boolean", "description": "Whether TilemapCollider2D is used by effector."},
                "isTrigger": {"type": "boolean", "description": "Whether TilemapCollider2D is a trigger."},
                "includeAllTiles": {"type": "boolean", "description": "Include all tile data in inspect response."},
            },
        },
        ["operation"],
    )

    gamekit_actor_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "inspect", "delete"],
                    "description": "Actor operation.",
                },
                "actorId": {"type": "string", "description": "Unique actor identifier (used for targeting with UICommand and scripting)."},
                "parentPath": {"type": "string", "description": "Parent GameObject path (optional, defaults to scene root)."},
                "behaviorProfile": {
                    "type": "string",
                    "enum": ["2dLinear", "2dPhysics", "2dTileGrid", "graphNode", "splineMovement", "3dCharacterController", "3dPhysics", "3dNavMesh"],
                    "description": "Movement behavior profile: '2dLinear' (simple 2D movement), '2dPhysics' (Rigidbody2D physics), '2dTileGrid' (grid-based movement for tactics/roguelikes), 'graphNode' (A* pathfinding, 2D/3D agnostic), 'splineMovement' (rail-based for 2.5D/rail shooters), '3dCharacterController' (CharacterController for FPS/TPS), '3dPhysics' (Rigidbody physics), '3dNavMesh' (NavMesh agent for RTS/strategy).",
                },
                "controlMode": {
                    "type": "string",
                    "enum": ["directController", "aiAutonomous", "uiCommand", "scriptTriggerOnly"],
                    "description": "Input control mode: 'directController' (player input via New Input System or legacy), 'aiAutonomous' (AI-driven patrol/follow/wander), 'uiCommand' (controlled by UI buttons via GameKitUICommand), 'scriptTriggerOnly' (event-driven from scripts only).",
                },
                "position": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    "description": "Initial world position of the actor.",
                },
                "rotation": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    "description": "Initial euler rotation of the actor (optional).",
                },
                "spritePath": {"type": "string", "description": "Sprite asset path for 2D actors (e.g., 'Assets/Sprites/Player.png')."},
                "modelPath": {"type": "string", "description": "Model prefab path for 3D actors (e.g., 'Assets/Models/Character.prefab')."},
            },
        },
        ["operation"],
    )

    gamekit_manager_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "inspect", "delete", "exportState", "importState", "setFlowEnabled"],
                    "description": "Manager operation.",
                },
                "managerId": {"type": "string", "description": "Unique manager identifier."},
                "managerType": {
                    "type": "string",
                    "enum": ["turnBased", "realtime", "resourcePool", "eventHub", "stateManager"],
                    "description": "Manager type: 'turnBased' for turn-based games, 'realtime' for real-time coordination, 'resourcePool' for resource/economy management, 'eventHub' for global events, 'stateManager' for finite state machines.",
                },
                "parentPath": {"type": "string", "description": "Parent GameObject path."},
                "persistent": {"type": "boolean", "description": "DontDestroyOnLoad flag (survives scene changes)."},
                "turnPhases": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Turn phase names for turn-based managers (e.g., ['PlayerTurn', 'EnemyTurn', 'ResolveEffects']).",
                },
                "resourceTypes": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Resource type names for resource pool managers (deprecated, use initialResources instead).",
                },
                "initialResources": {
                    "type": "object",
                    "additionalProperties": {"type": "number"},
                    "description": "Initial resource amounts for resource pool managers (e.g., {'health': 100, 'mana': 50, 'gold': 1000}).",
                },
                "stateData": {
                    "type": "object",
                    "additionalProperties": True,
                    "description": "State data for importState operation (JSON-serializable state from exportState).",
                },
                "flowId": {"type": "string", "description": "Flow identifier for setFlowEnabled operation."},
                "enabled": {"type": "boolean", "description": "Enable/disable flow for setFlowEnabled operation."},
            },
        },
        ["operation"],
    )

    gamekit_interaction_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "inspect", "delete"],
                    "description": "Interaction operation.",
                },
                "interactionId": {"type": "string", "description": "Unique interaction identifier (e.g., 'GoldCoin', 'AutoDoor')."},
                "parentPath": {"type": "string", "description": "Parent GameObject path (optional, creates new GameObject if not specified)."},
                "triggerType": {
                    "type": "string",
                    "enum": ["collision", "trigger", "raycast", "proximity", "input"],
                    "description": "Trigger detection type: 'collision' (OnCollisionEnter), 'trigger' (OnTriggerEnter), 'raycast' (ray hit detection), 'proximity' (distance-based), 'input' (key press).",
                },
                "triggerShape": {
                    "type": "string",
                    "enum": ["box", "sphere", "circle", "capsule", "polygon", "mesh"],
                    "description": "Collider shape. 2D: box/circle/capsule/polygon. 3D: box/sphere/capsule/mesh.",
                },
                "triggerSize": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    "description": "Collider size/radius (Vector3 for box/capsule, x for sphere/circle radius).",
                },
                "is2D": {
                    "type": "boolean",
                    "description": "Use 2D colliders (BoxCollider2D, CircleCollider2D, etc.) instead of 3D. Default: false.",
                },
                "actions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "type": {"type": "string", "enum": ["spawnPrefab", "destroyObject", "playSound", "sendMessage", "changeScene"], "description": "Action type to execute."},
                            "target": {"type": "string", "description": "Target GameObject name/path or 'self' for the interaction GameObject."},
                            "parameter": {"type": "string", "description": "Action parameter (prefab path, message name, scene name, etc.)."},
                        },
                    },
                    "description": "Declarative actions to execute when trigger conditions are met (executed in order).",
                },
                "conditions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "type": {"type": "string", "enum": ["tag", "layer", "distance", "custom"], "description": "Condition type."},
                            "value": {"type": "string", "description": "Condition value (tag name, layer name/number, distance threshold, custom script)."},
                        },
                    },
                    "description": "Conditions to check before executing actions (all conditions must pass, AND logic).",
                },
            },
        },
        ["operation"],
    )

    gamekit_ui_command_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["createCommandPanel", "addCommand", "inspect", "delete"],
                },
                "panelId": {"type": "string", "description": "Unique command panel identifier."},
                "canvasPath": {"type": "string", "description": "Canvas GameObject path."},
                "targetType": {
                    "type": "string",
                    "enum": ["actor", "manager"],
                    "description": "Target type: 'actor' for GameKitActor or 'manager' for GameKitManager.",
                },
                "targetActorId": {"type": "string", "description": "Target actor ID (when targetType is 'actor')."},
                "targetManagerId": {"type": "string", "description": "Target manager ID (when targetType is 'manager')."},
                "commands": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": {"type": "string"},
                            "label": {"type": "string"},
                            "icon": {"type": "string"},
                            "commandType": {
                                "type": "string",
                                "enum": ["move", "jump", "action", "look", "custom", "addResource", "setResource", "consumeResource", "changeState", "nextTurn", "triggerScene"],
                                "description": "Command type: Actor commands (move/jump/action/look/custom) or Manager commands (addResource/setResource/consumeResource/changeState/nextTurn/triggerScene).",
                            },
                            "commandParameter": {"type": "string", "description": "Parameter for action/resource/state commands."},
                            "resourceAmount": {"type": "number", "description": "Amount for resource commands (addResource/setResource/consumeResource)."},
                            "moveDirection": {
                                "type": "object",
                                "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                                "description": "Direction vector for move commands.",
                            },
                            "lookDirection": {
                                "type": "object",
                                "properties": {"x": {"type": "number"}, "y": {"type": "number"}},
                                "description": "Direction vector for look commands.",
                            },
                        },
                    },
                    "description": "List of commands to create as buttons.",
                },
                "layout": {
                    "type": "string",
                    "enum": ["horizontal", "vertical", "grid"],
                    "description": "Button layout style.",
                },
                "buttonSize": {
                    "type": "object",
                    "properties": {"width": {"type": "number"}, "height": {"type": "number"}},
                },
            },
        },
        ["operation"],
    )

    gamekit_machinations_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "inspect", "delete", "apply", "export"],
                    "description": "Machinations asset operation.",
                },
                "diagramId": {"type": "string", "description": "Unique diagram identifier."},
                "assetPath": {"type": "string", "description": "Path to Machinations asset file."},
                "managerId": {"type": "string", "description": "Manager ID to apply/export diagram to/from."},
                "resetExisting": {"type": "boolean", "description": "Reset existing resources when applying."},
                "initialResources": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": {"type": "string"},
                            "initialAmount": {"type": "number"},
                            "minValue": {"type": "number"},
                            "maxValue": {"type": "number"},
                        },
                    },
                    "description": "Resource pool definitions.",
                },
                "flows": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "flowId": {"type": "string"},
                            "resourceName": {"type": "string"},
                            "ratePerSecond": {"type": "number"},
                            "isSource": {"type": "boolean"},
                            "enabledByDefault": {"type": "boolean"},
                        },
                    },
                    "description": "Resource flow definitions (automatic generation/consumption).",
                },
                "converters": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "converterId": {"type": "string"},
                            "fromResource": {"type": "string"},
                            "toResource": {"type": "string"},
                            "conversionRate": {"type": "number"},
                            "inputCost": {"type": "number"},
                            "enabledByDefault": {"type": "boolean"},
                        },
                    },
                    "description": "Resource converter definitions (transform resources).",
                },
                "triggers": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "triggerName": {"type": "string"},
                            "resourceName": {"type": "string"},
                            "thresholdType": {"type": "string", "enum": ["above", "below", "equal", "notEqual"]},
                            "thresholdValue": {"type": "number"},
                            "enabledByDefault": {"type": "boolean"},
                        },
                    },
                    "description": "Resource trigger definitions (threshold events).",
                },
            },
        },
        ["operation"],
    )

    gamekit_sceneflow_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "inspect", "delete", "transition", "addScene", "removeScene", "updateScene", "addTransition", "removeTransition", "addSharedScene", "removeSharedScene"],
                    "description": "SceneFlow operation: 'create' for initial setup, then use individual add/remove/update operations for granular control.",
                },
                "flowId": {"type": "string", "description": "Unique scene flow identifier (e.g., 'MainGameFlow')."},
                "sceneName": {"type": "string", "description": "Scene name for single-scene operations (addScene, removeScene, updateScene, addSharedScene, removeSharedScene)."},
                "scenePath": {"type": "string", "description": "Unity scene asset path (e.g., 'Assets/Scenes/Level1.unity') for addScene/updateScene."},
                "loadMode": {"type": "string", "enum": ["single", "additive"], "description": "'single' unloads all scenes, 'additive' loads on top of existing (for addScene/updateScene)."},
                "sharedScenePaths": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Array of shared scene paths to load with this scene (for addScene/updateScene), e.g., ['Assets/Scenes/UIOverlay.unity', 'Assets/Scenes/AudioManager.unity'].",
                },
                "sharedScenePath": {"type": "string", "description": "Single shared scene path for addSharedScene/removeSharedScene operations."},
                "fromScene": {"type": "string", "description": "Source scene name for transition operations (addTransition/removeTransition)."},
                "toScene": {"type": "string", "description": "Destination scene name for addTransition operation."},
                "trigger": {"type": "string", "description": "Trigger name for transition operations (addTransition/removeTransition, e.g., 'startGame', 'levelComplete')."},
                "triggerName": {"type": "string", "description": "Transition trigger name for 'transition' operation (runtime execution)."},
            },
        },
        ["operation"],
    )

    sprite2d_bundle_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["createSprite", "updateSprite", "inspect", "updateMultiple", "setSortingLayer", "setColor", "sliceSpriteSheet", "createSpriteAtlas"],
                    "description": "Sprite2D bundle operation.",
                },
                "gameObjectPath": {"type": "string", "description": "Target GameObject hierarchy path."},
                "gameObjectGlobalObjectId": {"type": "string", "description": "Target GameObject GlobalObjectId."},
                "gameObjectPaths": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Multiple target GameObject paths for batch operations.",
                },
                "pattern": {"type": "string", "description": "Pattern for matching GameObjects."},
                "useRegex": {"type": "boolean", "description": "Use regex pattern matching."},
                "maxResults": {"type": "integer", "description": "Maximum results for batch operations."},
                "name": {"type": "string", "description": "Name for new sprite GameObject."},
                "parentPath": {"type": "string", "description": "Parent GameObject path."},
                "spritePath": {"type": "string", "description": "Sprite asset path."},
                "sortingLayerName": {"type": "string", "description": "Sorting layer name (default: 'Default')."},
                "sortingOrder": {"type": "integer", "description": "Sorting order within layer."},
                "color": {
                    "type": "object",
                    "properties": {"r": {"type": "number"}, "g": {"type": "number"}, "b": {"type": "number"}, "a": {"type": "number"}},
                    "description": "Sprite color (RGBA 0-1).",
                },
                "flipX": {"type": "boolean", "description": "Flip sprite horizontally."},
                "flipY": {"type": "boolean", "description": "Flip sprite vertically."},
                "drawMode": {
                    "type": "string",
                    "enum": ["simple", "sliced", "tiled"],
                    "description": "Sprite draw mode.",
                },
                "size": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}},
                    "description": "Sprite size (for sliced/tiled modes).",
                },
                "maskInteraction": {
                    "type": "string",
                    "enum": ["none", "visibleInsideMask", "visibleOutsideMask"],
                    "description": "Sprite mask interaction mode.",
                },
                "materialPath": {"type": "string", "description": "Material asset path."},
                "position": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    "description": "Initial position for new sprite.",
                },
                "texturePath": {"type": "string", "description": "Texture path for slicing."},
                "sliceMode": {
                    "type": "string",
                    "enum": ["grid", "automatic"],
                    "description": "Sprite sheet slicing mode.",
                },
                "cellSizeX": {"type": "integer", "description": "Grid cell width in pixels."},
                "cellSizeY": {"type": "integer", "description": "Grid cell height in pixels."},
                "pivot": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}},
                    "description": "Sprite pivot point (0-1).",
                },
                "pixelsPerUnit": {"type": "number", "description": "Pixels per unit for sprite import."},
                "atlasPath": {"type": "string", "description": "Output path for sprite atlas."},
                "spritePaths": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Sprite paths to include in atlas.",
                },
            },
        },
        ["operation"],
    )

    ui_hierarchy_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "clone", "inspect", "delete", "show", "hide", "toggle", "setNavigation"],
                    "description": "UI hierarchy operation.",
                },
                "parentPath": {"type": "string", "description": "Parent GameObject path (usually Canvas or Panel)."},
                "gameObjectPath": {"type": "string", "description": "Target UI hierarchy root path for inspect/delete/show/hide/toggle/clone/setNavigation."},
                "hierarchyId": {"type": "string", "description": "Optional identifier for the UI hierarchy (used for referencing)."},
                "hierarchy": {
                    "type": "object",
                    "description": "Declarative UI hierarchy definition (recursive structure).",
                    "additionalProperties": True,
                },
                "recursive": {"type": "boolean", "description": "Apply visibility/navigation recursively to children (default: true)."},
                "interactable": {"type": "boolean", "description": "Set interactable state when showing/hiding."},
                "blocksRaycasts": {"type": "boolean", "description": "Set blocksRaycasts state when showing/hiding."},
                "navigationMode": {
                    "type": "string",
                    "enum": ["none", "auto-vertical", "auto-horizontal", "explicit"],
                    "description": "Navigation mode for setNavigation operation.",
                },
                "selectables": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "List of selectable element paths for explicit navigation setup.",
                },
                "wrapAround": {"type": "boolean", "description": "Enable wrap-around navigation (last→first, first→last)."},
                "newName": {"type": "string", "description": "New name for cloned hierarchy root."},
            },
        },
        ["operation"],
    )

    ui_state_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["defineState", "applyState", "saveState", "loadState", "listStates", "deleteState", "createStateGroup", "transitionTo", "getActiveState"],
                    "description": "UI state operation.",
                },
                "stateName": {"type": "string", "description": "Name of the UI state."},
                "rootPath": {"type": "string", "description": "Root GameObject path for the UI state."},
                "elements": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "path": {"type": "string", "description": "Relative path from root (empty for root itself)."},
                            "active": {"type": "boolean", "description": "GameObject active state."},
                            "visible": {"type": "boolean", "description": "Visible (alpha > 0)."},
                            "interactable": {"type": "boolean", "description": "CanvasGroup interactable."},
                            "alpha": {"type": "number", "description": "CanvasGroup alpha (0-1)."},
                            "blocksRaycasts": {"type": "boolean", "description": "CanvasGroup blocksRaycasts."},
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
                "includeChildren": {"type": "boolean", "description": "Include children when saving state (default: true)."},
                "maxDepth": {"type": "integer", "description": "Maximum depth for saveState (default: 10)."},
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

    ui_navigation_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["configure", "setExplicit", "autoSetup", "createGroup", "setFirstSelected", "inspect", "reset", "disable"],
                    "description": "UI navigation operation.",
                },
                "gameObjectPath": {"type": "string", "description": "Target Selectable GameObject path."},
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
                "columns": {"type": "integer", "description": "Number of columns for grid navigation."},
                "includeDisabled": {"type": "boolean", "description": "Include disabled Selectables in autoSetup."},
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
                "isolate": {"type": "boolean", "description": "Isolate group navigation (default: true)."},
                "recursive": {"type": "boolean", "description": "Apply recursively for reset/disable."},
            },
        },
        ["operation"],
    )

    animation2d_bundle_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["setupAnimator", "updateAnimator", "inspectAnimator", "createController", "addState", "addTransition", "addParameter", "inspectController", "createClipFromSprites", "updateClip", "inspectClip"],
                    "description": "Animation2D bundle operation.",
                },
                "gameObjectPath": {"type": "string", "description": "Target GameObject hierarchy path."},
                "gameObjectGlobalObjectId": {"type": "string", "description": "Target GameObject GlobalObjectId."},
                "controllerPath": {"type": "string", "description": "AnimatorController asset path."},
                "clipPath": {"type": "string", "description": "AnimationClip asset path."},
                "applyRootMotion": {"type": "boolean", "description": "Enable root motion."},
                "updateMode": {
                    "type": "string",
                    "enum": ["Normal", "AnimatePhysics", "UnscaledTime"],
                    "description": "Animator update mode.",
                },
                "cullingMode": {
                    "type": "string",
                    "enum": ["AlwaysAnimate", "CullCompletely", "CullUpdateTransforms"],
                    "description": "Animator culling mode.",
                },
                "speed": {"type": "number", "description": "Animator playback speed."},
                "parameters": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": {"type": "string"},
                            "type": {"type": "string", "enum": ["Bool", "Float", "Int", "Trigger"]},
                        },
                    },
                    "description": "Animator parameters to add.",
                },
                "stateName": {"type": "string", "description": "Animation state name."},
                "layerIndex": {"type": "integer", "description": "Animator layer index (default: 0)."},
                "isDefault": {"type": "boolean", "description": "Set as default state."},
                "fromState": {"type": "string", "description": "Source state for transition ('Any' for AnyState)."},
                "toState": {"type": "string", "description": "Destination state for transition."},
                "hasExitTime": {"type": "boolean", "description": "Transition has exit time."},
                "exitTime": {"type": "number", "description": "Exit time (0-1)."},
                "duration": {"type": "number", "description": "Transition duration in seconds."},
                "conditions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "parameter": {"type": "string"},
                            "mode": {"type": "string", "enum": ["If", "IfNot", "Greater", "Less", "Equals", "NotEqual"]},
                            "threshold": {"type": "number"},
                        },
                    },
                    "description": "Transition conditions.",
                },
                "parameterName": {"type": "string", "description": "Parameter name."},
                "parameterType": {
                    "type": "string",
                    "enum": ["Bool", "Float", "Int", "Trigger"],
                    "description": "Parameter type.",
                },
                "spritePaths": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Sprite paths for animation clip creation.",
                },
                "frameRate": {"type": "number", "description": "Animation frame rate (default: 12)."},
                "loop": {"type": "boolean", "description": "Loop animation."},
            },
        },
        ["operation"],
    )

    tool_definitions = [
        types.Tool(
            name="unity_ping",
            description="Verify bridge connectivity and return the latest heartbeat information.",
            inputSchema=ping_schema,
        ),
        batch_sequential_tool,  # Sequential batch execution with resume capability
        types.Tool(
            name="unity_scene_crud",
            description="Comprehensive Unity scene management: create/load/save/delete/duplicate scenes, inspect scene hierarchy with optional component filtering, manage build settings (add/remove/reorder scenes). Use 'inspect' operation with 'includeHierarchy=true' to get scene context before making changes. Supports additive scene loading and build configuration operations.",
            inputSchema=scene_manage_schema,
        ),
        types.Tool(
            name="unity_gameobject_crud",
            description="Full GameObject lifecycle management: create (with templates like Cube/Sphere/Player/Enemy, and optional auto-attach components with properties), delete, move (reparent), rename, duplicate, update (tag/layer/active/static), inspect (with optional component details), and batch operations (findMultiple/deleteMultiple/inspectMultiple with pattern matching). Use 'components' array on create to auto-attach components: [{type: 'UnityEngine.Rigidbody2D', properties: {gravityScale: 0}}]. Supports regex pattern matching for batch operations.",
            inputSchema=game_object_manage_schema,
        ),
        types.Tool(
            name="unity_component_crud",
            description="""Complete component management with batch operations: add/remove/update/inspect components on GameObjects.

**Property Changes:** Supports complex property changes including nested objects and asset references.

**Unity Object References:** For properties that expect Unity Object references (TMP_Text, Button, InputField, etc.), use one of these formats:
- `{ "$ref": "Canvas/Panel/Text" }` - Recommended format
- `{ "_gameObjectPath": "Canvas/Panel/Text" }` - Alternative format
- `"Canvas/Panel/Text"` - Simple string (only when target type is UnityEngine.Object)

**Property Filtering:** Inspect supports fast existence checks (includeProperties=false, 10x faster) and property filtering for specific fields.

**Batch Operations:** addMultiple/removeMultiple/updateMultiple/inspectMultiple support pattern matching with maxResults safety limits.

Essential for configuring GameObject behavior and wiring up component references.""",
            inputSchema=component_manage_schema,
        ),
        types.Tool(
            name="unity_asset_crud",
            description="Comprehensive asset file management under Assets/ folder: create (any file type including C# scripts, JSON, text), update (modify file contents), delete, rename, duplicate, inspect (view properties and content), updateImporter (modify asset import settings), and batch operations (findMultiple/deleteMultiple/inspectMultiple with pattern matching). Essential for managing scripts, textures, audio, data files, and all Unity assets. Use with unity_script_template_generate for creating properly structured C# scripts.",
            inputSchema=asset_manage_schema,
        ),
        types.Tool(
            name="unity_scriptableObject_crud",
            description="ScriptableObject asset management: create new instances from type name, inspect/update properties, delete, duplicate, list all instances, or find by type. ScriptableObjects are Unity's data container assets perfect for game configuration (stats, settings, levels). Use 'create' to instantiate from existing type, 'update' to modify properties, 'list' to see all instances, 'findByType' to search by class name. Supports property filtering and batch operations.",
            inputSchema=scriptable_object_manage_schema,
        ),
        types.Tool(
            name="unity_prefab_crud",
            description="Complete prefab workflow management: create prefabs from scene GameObjects, update existing prefabs, inspect prefab contents and overrides, instantiate prefabs into scenes with custom position/rotation, unpack prefab instances (completely or outermost only), apply instance overrides back to prefab, or revert instance changes. Essential for creating reusable game objects (enemies, pickups, UI elements, buildings). Use 'create' to save GameObjects as prefabs, 'instantiate' to spawn prefab instances, 'applyOverrides' to update prefab from modified instance.",
            inputSchema=prefab_manage_schema,
        ),
        types.Tool(
            name="unity_vector_sprite_convert",
            description="Vector and primitive to sprite conversion: generate 2D sprites from primitives (square/circle/triangle/polygon with custom sides), import SVG vector files to sprites, convert existing textures to sprite assets with custom import settings, or create solid color sprites. Supports custom dimensions (width/height in pixels), RGBA colors (0-1 range), pixels per unit (sprite scale), and sprite modes (single/multiple for sprite sheets). Perfect for procedural sprite generation, prototyping without art assets, UI element creation, and SVG integration. Outputs ready-to-use sprite assets.",
            inputSchema=vector_sprite_convert_schema,
        ),
        types.Tool(
            name="unity_projectSettings_crud",
            description="Unity Project Settings management: read/write/list settings across 8 categories (player: build settings & configurations, quality: quality levels & graphics, time: time scale & fixed timestep, physics: 3D gravity & collision settings, physics2d: 2D gravity & collision settings, audio: volume & DSP buffer, editor: serialization & asset pipeline, tagsLayers: custom tags, layers & sorting layers). Build Settings operations: addSceneToBuild (add scene to build with optional index), removeSceneFromBuild (remove by path or index), listBuildScenes (view all build scenes), reorderBuildScenes (change scene order), setBuildSceneEnabled (enable/disable scene). Use 'list' to see available properties per category, 'read' to get specific property value, 'write' to modify settings. Essential for configuring project-wide settings, 2D/3D physics parameters, quality presets, sorting layers, and build configurations.",
            inputSchema=project_settings_manage_schema,
        ),
        types.Tool(
            name="unity_transform_batch",
            description="Mid-level batch transform operations: arrange multiple GameObjects in patterns (arrangeCircle: circular formation, arrangeLine: linear spacing, createMenuList: vertical/horizontal menu layout from prefabs), rename objects sequentially (Item_01, Item_02) or from custom name lists, all in local or world space. Supports custom center points, radius, spacing, angles, and planes (XY/XZ/YZ). Perfect for organizing level objects, UI elements, menu items, and creating structured layouts without manual positioning.",
            inputSchema=transform_batch_schema,
        ),
        types.Tool(
            name="unity_rectTransform_batch",
            description="Mid-level batch UI RectTransform operations: set anchors (topLeft/middleCenter/stretchAll, 16 presets), pivot points, size delta, anchored position for multiple UI elements simultaneously. Supports alignment to parent edges, horizontal/vertical distribution with custom spacing, and size matching (width/height/both) from source element. Essential for precise UI layout control, responsive design setup, and batch UI element positioning. Use for aligning panels, distributing buttons, matching UI element sizes, and creating consistent layouts.",
            inputSchema=rect_transform_batch_schema,
        ),
        types.Tool(
            name="unity_physics_bundle",
            description="Mid-level physics setup: apply complete physics presets (dynamic: movable with physics, kinematic: movable without physics, static: immovable, character: player/NPC, platformer: 2D side-scrolling, topDown: 2D top-down, vehicle: car physics, projectile: bullets/arrows) or update individual Rigidbody/Collider properties. Automatically adds Rigidbody2D/Rigidbody + Collider (box/sphere/capsule/circle) with appropriate settings. Supports 2D and 3D physics, constraints (freeze position/rotation), collision detection modes (discrete/continuous), and physics materials. Perfect for rapid physics prototyping.",
            inputSchema=physics_bundle_schema,
        ),
        types.Tool(
            name="unity_camera_rig",
            description="Mid-level camera rig creation: create complete camera systems with single commands (follow: smooth following camera, orbit: rotate around target, splitScreen: multiplayer viewports, fixed: static camera, dolly: cinematic rail camera). Automatically configures Camera component, target tracking, follow smoothing, orbit distance, field of view, orthographic/perspective mode, and split-screen viewports. Perfect for quickly setting up player cameras, cinematic cameras, or multiplayer camera systems without manual rigging.",
            inputSchema=camera_rig_schema,
        ),
        types.Tool(
            name="unity_ui_foundation",
            description="Mid-level UI foundation for UGUI: create complete UI elements with single commands (Canvas with EventSystem, Panel with Image, Button with Text child, Text with styling, Image with sprite support, InputField with placeholder, ScrollView with Viewport/Content/Scrollbars). Supports LayoutGroups (Horizontal/Vertical/Grid) with full configuration, ContentSizeFitter, and UI templates (dialog/hud/menu/statusBar/inventoryGrid). Supports render modes (screenSpaceOverlay/screenSpaceCamera/worldSpace), anchor presets (topLeft/middleCenter/stretchAll, etc.), automatic sizing, and color configuration. Perfect for rapid UI prototyping and hierarchical UI design. Use for basic UI setup, then customize with unity_component_crud if needed.",
            inputSchema=ui_foundation_schema,
        ),
        types.Tool(
            name="unity_audio_source_bundle",
            description="Mid-level audio source setup: create and configure AudioSource components with presets (music: looping background music with lower priority, sfx: one-shot sound effects with high priority, ambient: looping environmental sounds, voice: dialogue with high priority, ui: button clicks/menu sounds). Automatically configures volume, pitch, loop, playOnAwake, spatialBlend (2D/3D), min/max distance for 3D audio, priority (0-256), and audio mixer group routing. Perfect for quickly setting up game audio without manual AudioSource configuration.",
            inputSchema=audio_source_bundle_schema,
        ),
        types.Tool(
            name="unity_input_profile",
            description="Mid-level input system setup: create PlayerInput component with New Input System, configure action maps (player: move/jump/fire, ui: navigate/submit/cancel, vehicle: accelerate/brake/steer), set up notification behaviors (sendMessages/broadcastMessages/invokeUnityEvents/invokeCSharpEvents), and define custom actions with bindings. Automatically generates or uses existing InputActions assets. Essential for setting up player input handling with Unity's modern Input System. Use presets for quick setup or 'custom' for full control.",
            inputSchema=input_profile_schema,
        ),
        types.Tool(
            name="unity_character_controller_bundle",
            description="Mid-level CharacterController setup: apply CharacterController component with presets optimized for different character types (fps: 1.8m height for first-person, tps: 2.0m for third-person, platformer: 1.0m for platformers, child: 0.5m for small characters, large: 3.0m for large characters, narrow: thin capsule for tight spaces, custom: full manual control). Automatically configures capsule radius, height, center offset, slope limit (max climbable angle), step offset (max stair height), skin width (collision padding), and minimum move distance. Perfect for 3D character setup without manual physics configuration.",
            inputSchema=character_controller_bundle_schema,
        ),
        types.Tool(
            name="unity_tilemap_bundle",
            description="Mid-level Tilemap management: create Tilemaps with Grid parent, set/get/clear individual tiles, fill rectangular areas, create Tile and RuleTile assets from sprites. Operations: createTilemap (auto-creates Grid), setTile/getTile/setTiles (place tiles at positions), clearTile/clearTiles/clearAllTiles (remove tiles), fillArea/boxFill (batch placement), worldToCell/cellToWorld (coordinate conversion), updateRenderer/updateCollider/addCollider (component settings), createTile/createRuleTile/inspectTile/updateTile (tile asset management). Supports Rectangle/Hexagon/Isometric layouts, sorting layers, TilemapCollider2D with CompositeCollider2D support. RuleTile requires 2D Tilemap Extras package. Essential for 2D level design, roguelikes, platformers, and procedural map generation.",
            inputSchema=tilemap_bundle_schema,
        ),
        types.Tool(
            name="unity_gamekit_actor",
            description="High-level GameKit Actor: create game actors with controller-behavior separation. Choose from 8 behavior profiles (2dLinear/2dPhysics/2dTileGrid/graphNode/splineMovement/3dCharacterController/3dPhysics/3dNavMesh) and 4 control modes (directController for player input via New Input System or legacy, aiAutonomous for AI patrol/follow/wander, uiCommand for UI button control, scriptTriggerOnly for event-driven). Actors relay input to behaviors via UnityEvents (OnMoveInput/OnJumpInput/OnActionInput/OnLookInput). Perfect for players, NPCs, enemies, and interactive characters.",
            inputSchema=gamekit_actor_schema,
        ),
        types.Tool(
            name="unity_gamekit_manager",
            description="High-level GameKit Manager: create centralized game system managers for turn-based games (TurnManager), real-time coordination (RealtimeManager), resource/economy management (ResourceManager with Machinations support), global events (EventHub), or finite state machines (StateManager). Supports persistence (DontDestroyOnLoad), state export/import for save/load systems, and integration with GameKitUICommand for UI control. Essential for managing game-wide state, resources (health/mana/gold), turn phases, and game flow.",
            inputSchema=gamekit_manager_schema,
        ),
        types.Tool(
            name="unity_gamekit_interaction",
            description="High-level GameKit Interaction: create trigger-based interactions with declarative actions. Choose from 5 trigger types (collision/trigger/raycast/proximity/input) and 5 action types (spawnPrefab/destroyObject/playSound/sendMessage/changeScene). Add conditions (tag/layer/distance/custom) for filtering. Supports both 2D (BoxCollider2D, CircleCollider2D, CapsuleCollider2D, PolygonCollider2D) and 3D (BoxCollider, SphereCollider, CapsuleCollider, MeshCollider) colliders via is2D parameter. Perfect for collectibles, doors, switches, treasure chests, and interactive objects. No scripting required - define complete interactions declaratively.",
            inputSchema=gamekit_interaction_schema,
        ),
        types.Tool(
            name="unity_gamekit_ui_command",
            description="High-level GameKit UI Command: create command panels with buttons that send commands to GameKitActors (move/jump/action) or GameKitManagers (resources/state/turn/scene). Supports both actor control and manager control via targetType parameter.",
            inputSchema=gamekit_ui_command_schema,
        ),
        types.Tool(
            name="unity_gamekit_machinations",
            description="High-level GameKit Machinations: create and manage Machinations diagram assets for economic systems. Define resource pools, flows (automatic generation/consumption), converters (resource transformation), and triggers (threshold events). Apply diagrams to ResourceManagers or export current manager state to assets.",
            inputSchema=gamekit_machinations_schema,
        ),
        types.Tool(
            name="unity_gamekit_sceneflow",
            description="High-level GameKit SceneFlow: manage scene transitions with granular control. Use 'create' to initialize, then 'addScene' to add individual scenes with load modes and shared scenes. Use 'addTransition' to define state machine transitions between scenes. Use 'removeScene'/'removeTransition' to modify flow. Each scene can have its own transitions - same trigger can lead to different destinations per scene. Perfect for level progression, menu systems, and complex scene workflows.",
            inputSchema=gamekit_sceneflow_schema,
        ),
        types.Tool(
            name="unity_sprite2d_bundle",
            description="Mid-level 2D sprite management: create/update SpriteRenderer GameObjects, batch update sprites, set sorting layers and colors, slice sprite sheets into multiple sprites, create SpriteAtlas assets. Operations: createSprite (new GameObject with SpriteRenderer), updateSprite (modify sprite/color/flip/sortingLayer), inspect (view sprite properties), updateMultiple/setSortingLayer/setColor (batch operations with pattern matching), sliceSpriteSheet (grid/automatic slicing), createSpriteAtlas (pack sprites). Perfect for 2D game sprite setup, sprite sheet management, and batch sprite configuration.",
            inputSchema=sprite2d_bundle_schema,
        ),
        types.Tool(
            name="unity_animation2d_bundle",
            description="Mid-level 2D animation setup: manage Animator components, create AnimatorControllers, add states and transitions, create AnimationClips from sprite sequences. Operations: setupAnimator/updateAnimator/inspectAnimator (Animator component), createController/addState/addTransition/addParameter/inspectController (AnimatorController), createClipFromSprites/updateClip/inspectClip (AnimationClip). Supports transition conditions (If/Greater/Less), animation parameters (Bool/Float/Int/Trigger), and sprite-based animation creation. Essential for 2D character animation, state machines, and procedural animation setup.",
            inputSchema=animation2d_bundle_schema,
        ),
        types.Tool(
            name="unity_ui_hierarchy",
            description="""Mid-level declarative UI hierarchy management: create complex UI structures from single JSON definitions, manage visibility states, configure keyboard/gamepad navigation.

**Operations:**
- create: Build complete UI hierarchy from declarative JSON structure (panels, buttons, text, images, inputs, scrollviews, toggles, sliders, dropdowns)
- clone: Duplicate existing UI hierarchy with optional rename
- inspect: Export UI hierarchy as JSON structure
- delete: Remove UI hierarchy
- show/hide/toggle: Control visibility using CanvasGroup (alpha, interactable, blocksRaycasts)
- setNavigation: Configure keyboard/gamepad navigation (none/auto-vertical/auto-horizontal/explicit)

**Hierarchy Structure Example:**
```json
{
  "type": "panel",
  "name": "MainMenu",
  "children": [
    {"type": "text", "name": "Title", "text": "Game Title", "fontSize": 48},
    {"type": "button", "name": "StartBtn", "text": "Start Game"},
    {"type": "button", "name": "OptionsBtn", "text": "Options"}
  ],
  "layout": "Vertical",
  "spacing": 20
}
```

**Supported Element Types:** panel, button, text, image, inputfield, scrollview, toggle, slider, dropdown

Perfect for rapid UI prototyping, menu systems, dialog boxes, and complex UI structures without multiple API calls.""",
            inputSchema=ui_hierarchy_schema,
        ),
        types.Tool(
            name="unity_ui_state",
            description="""Mid-level UI state management: define, save, load, and transition between UI states.

**Operations:**
- defineState: Define a named UI state with element configurations (active, visible, interactable, alpha, position, size)
- applyState: Apply a saved state to UI elements
- saveState: Capture current UI state (including children)
- loadState: Load state definition without applying
- listStates: List all defined states for a root
- deleteState: Remove a state definition
- createStateGroup: Create a group of mutually exclusive states
- transitionTo: Transition to a state (alias for applyState)
- getActiveState: Get currently active state name

**Use Cases:**
- Menu screens (main menu, pause menu, settings)
- Dialog states (open, closed, minimized)
- HUD states (combat, exploration, cutscene)
- Form validation states (valid, invalid, loading)

**Example:**
```python
# Define a "hidden" state for dialog
unity_ui_state({
    "operation": "defineState",
    "stateName": "hidden",
    "rootPath": "Canvas/Dialog",
    "elements": [
        {"path": "", "active": False, "alpha": 0}
    ]
})

# Apply the state
unity_ui_state({
    "operation": "applyState",
    "stateName": "hidden",
    "rootPath": "Canvas/Dialog"
})
```""",
            inputSchema=ui_state_schema,
        ),
        types.Tool(
            name="unity_ui_navigation",
            description="""Mid-level UI navigation management: configure keyboard/gamepad navigation for UI elements.

**Operations:**
- configure: Set navigation mode for a single Selectable (none/horizontal/vertical/automatic/explicit)
- setExplicit: Set explicit up/down/left/right navigation targets
- autoSetup: Automatically configure navigation for all Selectables under a root (vertical/horizontal/grid)
- createGroup: Create a navigation group with isolated navigation
- setFirstSelected: Set the first selected element for EventSystem
- inspect: View current navigation configuration
- reset: Reset navigation to automatic mode
- disable: Disable navigation (mode=none)

**Direction Options:**
- vertical: Up/Down navigation in order
- horizontal: Left/Right navigation in order
- grid: Full 2D navigation with automatic column detection
- both: Combine vertical and horizontal

**Example:**
```python
# Auto-setup vertical menu navigation
unity_ui_navigation({
    "operation": "autoSetup",
    "rootPath": "Canvas/MainMenu",
    "direction": "vertical",
    "wrapAround": True
})

# Create isolated navigation group
unity_ui_navigation({
    "operation": "createGroup",
    "groupName": "InventorySlots",
    "elements": ["Canvas/Inventory/Slot1", "Canvas/Inventory/Slot2", "Canvas/Inventory/Slot3"],
    "direction": "horizontal",
    "wrapAround": True,
    "isolate": True
})
```""",
            inputSchema=ui_navigation_schema,
        ),
    ]

    tool_map = {tool.name: tool for tool in tool_definitions}

    @server.list_tools()
    async def list_tools() -> list[types.Tool]:
        return tool_definitions

    @server.call_tool()
    async def call_tool(name: str, arguments: dict | None) -> list[types.Content]:
        if name not in tool_map:
            raise RuntimeError(f"Unknown tool requested: {name}")

        args = arguments or {}

        if name == "unity_ping":
            _ensure_bridge_connected()
            heartbeat = bridge_manager.get_last_heartbeat()
            bridge_response = await bridge_manager.send_command("pingUnityEditor", {})
            payload = {
                "connected": True,
                "lastHeartbeatAt": heartbeat,
                "bridgeResponse": bridge_response,
            }
            return [types.TextContent(type="text", text=as_pretty_json(payload))]

        if name == "unity_scene_crud":
            return await _call_bridge_tool("sceneManage", args)

        if name == "unity_gameobject_crud":
            return await _call_bridge_tool("gameObjectManage", args)

        if name == "unity_component_crud":
            return await _call_bridge_tool("componentManage", args)

        if name == "unity_asset_crud":
            # Handle asset CRUD operations
            result = await _call_bridge_tool("assetManage", args)
            
            # Check if we need to wait for compilation (C# script creation/update/deletion)
            operation = args.get("operation")
            asset_path = args.get("assetPath", "")
            
            if operation in ["create", "update", "delete"] and asset_path.lower().endswith(".cs"):
                logger.info(
                    "C# script %s operation '%s' detected - waiting for compilation to complete...",
                    asset_path,
                    operation,
                )
                
                try:
                    # Wait for compilation with extended timeout (60 seconds)
                    compilation_result = await bridge_manager.await_compilation(timeout_seconds=60)
                    
                    logger.info(
                        "Compilation completed: success=%s, errors=%s, elapsed=%ss",
                        compilation_result.get("success"),
                        compilation_result.get("errorCount", 0),
                        compilation_result.get("elapsedSeconds", 0),
                    )
                    
                    # Add compilation result to the response
                    if isinstance(result[0].text, str):
                        import json
                        try:
                            result_data = json.loads(result[0].text)
                            result_data["compilation"] = compilation_result
                            result[0].text = as_pretty_json(result_data)
                        except (json.JSONDecodeError, AttributeError):
                            # If we can't parse the result, just append compilation info
                            result[0].text += f"\n\nCompilation: {as_pretty_json(compilation_result)}"
                
                except TimeoutError as exc:
                    logger.warning("Compilation wait timed out: %s", exc)
                    # Don't fail the operation, just log the timeout
                except Exception as exc:
                    logger.warning("Error while waiting for compilation: %s", exc)
                    # Don't fail the operation, just log the error
            
            return result

        if name == "unity_scriptableObject_crud":
            return await _call_bridge_tool("scriptableObjectManage", args)

        if name == "unity_prefab_crud":
            return await _call_bridge_tool("prefabManage", args)

        if name == "unity_vector_sprite_convert":
            return await _call_bridge_tool("vectorSpriteConvert", args)

        if name == "unity_projectSettings_crud":
            return await _call_bridge_tool("projectSettingsManage", args)

        if name == "unity_transform_batch":
            return await _call_bridge_tool("transformBatch", args)

        if name == "unity_rectTransform_batch":
            return await _call_bridge_tool("rectTransformBatch", args)

        if name == "unity_physics_bundle":
            return await _call_bridge_tool("physicsBundle", args)

        if name == "unity_camera_rig":
            return await _call_bridge_tool("cameraRig", args)

        if name == "unity_ui_foundation":
            return await _call_bridge_tool("uiFoundation", args)

        if name == "unity_audio_source_bundle":
            return await _call_bridge_tool("audioSourceBundle", args)

        if name == "unity_input_profile":
            return await _call_bridge_tool("inputProfile", args)

        if name == "unity_character_controller_bundle":
            return await _call_bridge_tool("characterControllerBundle", args)

        if name == "unity_tilemap_bundle":
            return await _call_bridge_tool("tilemapBundle", args)

        if name == "unity_gamekit_actor":
            return await _call_bridge_tool("gamekitActor", args)

        if name == "unity_gamekit_manager":
            return await _call_bridge_tool("gamekitManager", args)

        if name == "unity_gamekit_interaction":
            return await _call_bridge_tool("gamekitInteraction", args)

        if name == "unity_gamekit_ui_command":
            return await _call_bridge_tool("gamekitUICommand", args)

        if name == "unity_gamekit_machinations":
            return await _call_bridge_tool("gamekitMachinations", args)

        if name == "unity_gamekit_sceneflow":
            return await _call_bridge_tool("gamekitSceneFlow", args)

        if name == "unity_sprite2d_bundle":
            return await _call_bridge_tool("sprite2DBundle", args)

        if name == "unity_animation2d_bundle":
            return await _call_bridge_tool("animation2DBundle", args)

        if name == "unity_ui_hierarchy":
            return await _call_bridge_tool("uiHierarchy", args)

        if name == "unity_ui_state":
            return await _call_bridge_tool("uiState", args)

        if name == "unity_ui_navigation":
            return await _call_bridge_tool("uiNavigation", args)

        if name == "unity_batch_sequential_execute":
            # Special handling for batch sequential tool (doesn't use bridge directly)
            return await handle_batch_sequential(args, bridge_manager)

        raise RuntimeError(f"No handler registered for tool '{name}'.")

