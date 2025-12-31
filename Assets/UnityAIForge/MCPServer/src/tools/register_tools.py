from __future__ import annotations

import asyncio
import time
from typing import Any

import mcp.types as types
from mcp.server import Server

from bridge.bridge_manager import bridge_manager
from logger import logger
from tools.batch_sequential import TOOL as batch_sequential_tool
from tools.batch_sequential import handle_batch_sequential
from utils.json_utils import as_pretty_json


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

    compilation_await_schema: dict[str, Any] = {
        "type": "object",
        "properties": {
            "operation": {
                "type": "string",
                "enum": ["await"],
                "description": "Operation to perform. Currently only 'await' is supported.",
            },
            "timeoutSeconds": {
                "type": "integer",
                "description": "Maximum time to wait for compilation to complete (default: 60 seconds).",
                "default": 60,
            },
        },
        "required": ["operation"],
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
                    ],
                    "description": "Scene operation to perform.",
                },
                "scenePath": {"type": "string", "description": "Path to scene file (e.g., 'Assets/Scenes/Level1.unity')."},
                "newSceneName": {"type": "string", "description": "New name for duplicate operation."},
                "additive": {"type": "boolean", "description": "Load scene additively (keep existing scenes loaded)."},
                "includeOpenScenes": {"type": "boolean", "description": "Include currently open scenes in inspect response."},
                "includeHierarchy": {"type": "boolean", "description": "Include scene hierarchy (GameObjects) in inspect response."},
                "includeComponents": {"type": "boolean", "description": "Include component details for each GameObject in hierarchy."},
                "filter": {"type": "string", "description": "Filter GameObjects by name pattern (e.g., 'Player*', '*Enemy*')."},
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
                    "description": "GameObject operation to perform.",
                },
                "gameObjectPath": {"type": "string", "description": "Hierarchy path to target GameObject (e.g., 'Canvas/Panel/Button')."},
                "parentPath": {"type": "string", "description": "Parent GameObject path for create/move operations."},
                "template": {"type": "string", "description": "Primitive template for create (e.g., 'Cube', 'Sphere', 'Capsule', 'Cylinder', 'Plane', 'Quad')."},
                "name": {"type": "string", "description": "Name for new GameObject or rename operation."},
                "tag": {"type": "string", "description": "Tag to assign (e.g., 'Player', 'Enemy'). Must exist in project settings."},
                "layer": {"oneOf": [{"type": "integer"}, {"type": "string"}], "description": "Layer by number (0-31) or name (e.g., 'UI', 'Player'). Use unity_projectSettings_crud to add custom layers."},
                "active": {"type": "boolean", "description": "Set GameObject active/inactive state."},
                "static": {"type": "boolean", "description": "Mark GameObject as static for optimization."},
                "pattern": {"type": "string", "description": "Name pattern for batch operations (e.g., 'Enemy*', '*_LOD0')."},
                "useRegex": {"type": "boolean", "description": "Interpret pattern as regex instead of wildcard."},
                "includeComponents": {"type": "boolean", "description": "Include component details in inspect response."},
                "maxResults": {"type": "integer", "description": "Maximum number of results for batch operations (default: 1000)."},
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
                    "description": "Component operation to perform.",
                },
                "gameObjectPath": {"type": "string", "description": "Target GameObject hierarchy path."},
                "gameObjectGlobalObjectId": {"type": "string", "description": "Target GameObject GlobalObjectId (alternative to path)."},
                "componentType": {"type": "string", "description": "Full component type name (e.g., 'UnityEngine.Rigidbody2D', 'UnityEngine.UI.Button')."},
                "propertyChanges": {"type": "object", "additionalProperties": True, "description": "Property values to set. Use {'$ref': 'path'} for references: 'Assets/...' for assets, 'Canvas/Panel/Button' for scene objects (including inactive)."},
                "applyDefaults": {"type": "boolean", "description": "Apply default property values when adding component."},
                "pattern": {"type": "string", "description": "GameObject name pattern for batch operations."},
                "useRegex": {"type": "boolean", "description": "Interpret pattern as regex instead of wildcard."},
                "includeProperties": {"type": "boolean", "description": "Include property values in inspect response (default: true)."},
                "propertyFilter": {"type": "array", "items": {"type": "string"}, "description": "Filter specific properties to include in response."},
                "maxResults": {"type": "integer", "description": "Maximum results for batch operations (default: 1000)."},
                "stopOnError": {"type": "boolean", "description": "Stop batch operation on first error (default: true)."},
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
                    "description": "Asset operation to perform.",
                },
                "assetPath": {"type": "string", "description": "Asset file path (e.g., 'Assets/Scripts/Player.cs')."},
                "assetGuid": {"type": "string", "description": "Asset GUID (alternative to assetPath)."},
                "content": {"type": "string", "description": "File content for create/update operations."},
                "destinationPath": {"type": "string", "description": "Destination path for rename/duplicate operations."},
                "propertyChanges": {"type": "object", "additionalProperties": True, "description": "Importer property changes for updateImporter operation."},
                "pattern": {"type": "string", "description": "Asset path pattern for batch operations (e.g., 'Assets/Textures/*.png')."},
                "useRegex": {"type": "boolean", "description": "Interpret pattern as regex instead of glob."},
                "includeProperties": {"type": "boolean", "description": "Include asset properties in inspect response."},
                "maxResults": {"type": "integer", "description": "Maximum results for batch operations (default: 1000)."},
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
                    "description": "Settings category to read/write.",
                },
                "property": {"type": "string", "description": "Property name to read/write. Use 'list' operation to see available properties."},
                "value": {"description": "Value to set. Type depends on property (string, number, boolean, object for Vector types like gravity)."},
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
                    "description": "ScriptableObject operation to perform.",
                },
                "typeName": {"type": "string", "description": "Full type name for create/findByType (e.g., 'MyGame.GameConfig')."},
                "assetPath": {"type": "string", "description": "ScriptableObject asset path (e.g., 'Assets/Data/Config.asset')."},
                "assetGuid": {"type": "string", "description": "Asset GUID (alternative to assetPath)."},
                "properties": {"type": "object", "additionalProperties": True, "description": "Property values to set on the ScriptableObject."},
                "includeProperties": {"type": "boolean", "description": "Include property values in inspect response (default: true)."},
                "propertyFilter": {"type": "array", "items": {"type": "string"}, "description": "Filter specific properties to include in response."},
                "searchPath": {"type": "string", "description": "Directory to search for findByType/list (default: 'Assets')."},
                "maxResults": {"type": "integer", "description": "Maximum results for list/findByType (default: 100)."},
                "offset": {"type": "integer", "description": "Skip first N results for pagination."},
                "sourceAssetPath": {"type": "string", "description": "Source asset path for duplicate operation."},
                "sourceAssetGuid": {"type": "string", "description": "Source asset GUID for duplicate operation."},
                "destinationAssetPath": {"type": "string", "description": "Destination path for duplicate operation."},
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
                "radius": {"type": "number", "description": "Radius for circular arrangement."},
                "startAngle": {"type": "number", "description": "Starting angle in degrees for circular arrangement (0 = right, 90 = up)."},
                "clockwise": {"type": "boolean", "description": "Arrange objects clockwise (default: false = counter-clockwise)."},
                "plane": {"type": "string", "enum": ["XY", "XZ", "YZ"], "description": "Plane for circular arrangement (default: XY for 2D, XZ for 3D top-down)."},
                "localSpace": {"type": "boolean", "description": "Use local space coordinates instead of world space."},
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
                "spacing": {"type": "number", "description": "Spacing between objects for linear arrangement or menu list."},
                "baseName": {"type": "string", "description": "Base name for sequential renaming (e.g., 'Enemy' -> Enemy_001, Enemy_002)."},
                "startIndex": {"type": "integer", "description": "Starting index for sequential renaming (default: 1)."},
                "padding": {"type": "integer", "description": "Zero-padding for sequential numbers (e.g., 3 -> 001, 002)."},
                "names": {"type": "array", "items": {"type": "string"}, "description": "Custom name list for renameFromList operation."},
                "parentPath": {"type": "string", "description": "Parent GameObject path for createMenuList operation."},
                "prefabPath": {"type": "string", "description": "Prefab asset path for createMenuList operation."},
                "axis": {"type": "string", "enum": ["horizontal", "vertical"], "description": "Layout axis for createMenuList operation."},
                "offset": {"type": "number", "description": "Offset from parent for createMenuList operation."},
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
                "spacing": {"type": "number", "description": "Spacing between elements for distribute operations (pixels)."},
                "matchWidth": {"type": "boolean", "description": "Match width from source element in matchSize operation."},
                "matchHeight": {"type": "boolean", "description": "Match height from source element in matchSize operation."},
                "sourceGameObjectPath": {"type": "string", "description": "Source element path for matchSize operation."},
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
                "parentPath": {"type": "string", "description": "Parent GameObject path for create operations."},
                "targetPath": {"type": "string", "description": "Target GameObject path for addLayoutGroup/updateLayoutGroup/removeLayoutGroup operations."},
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
                    "oneOf": [
                        {"type": "number"},
                        {"type": "object", "properties": {"x": {"type": "number"}, "y": {"type": "number"}}}
                    ],
                    "description": "Spacing: number for Horizontal/Vertical layouts, {x, y} object for Grid layout.",
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

    # Phase 1 GameKit tools - Common game mechanics
    gamekit_health_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "inspect", "delete", "applyDamage", "heal", "kill", "respawn", "setInvincible", "findByHealthId"],
                    "description": "Health operation to perform.",
                },
                "targetPath": {"type": "string", "description": "Target GameObject hierarchy path."},
                "healthId": {"type": "string", "description": "Unique health component identifier."},
                "maxHealth": {"type": "number", "description": "Maximum health value.", "default": 100},
                "currentHealth": {"type": "number", "description": "Current health value."},
                "invincibilityDuration": {"type": "number", "description": "Duration of invincibility after taking damage (seconds).", "default": 0.5},
                "canTakeDamage": {"type": "boolean", "description": "Whether the entity can take damage."},
                "onDeath": {
                    "type": "string",
                    "enum": ["destroy", "disable", "respawn", "event"],
                    "description": "Behavior when health reaches zero.",
                },
                "respawnPosition": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    "description": "Position to respawn at.",
                },
                "respawnDelay": {"type": "number", "description": "Delay before respawning (seconds)."},
                "resetHealthOnRespawn": {"type": "boolean", "description": "Reset health to max on respawn."},
                "amount": {"type": "number", "description": "Amount for applyDamage/heal operations."},
                "invincible": {"type": "boolean", "description": "Set invincibility state for setInvincible operation."},
                "duration": {"type": "number", "description": "Invincibility duration for setInvincible operation."},
            },
        },
        ["operation"],
    )

    gamekit_spawner_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "inspect", "delete", "start", "stop", "reset", "spawnOne", "spawnBurst", "despawnAll", "addSpawnPoint", "addWave", "findBySpawnerId"],
                    "description": "Spawner operation to perform.",
                },
                "targetPath": {"type": "string", "description": "Target GameObject hierarchy path."},
                "spawnerId": {"type": "string", "description": "Unique spawner identifier."},
                "prefabPath": {"type": "string", "description": "Path to prefab asset to spawn."},
                "spawnMode": {
                    "type": "string",
                    "enum": ["interval", "wave", "burst", "manual"],
                    "description": "Spawning mode.",
                },
                "autoStart": {"type": "boolean", "description": "Start spawning automatically on scene start."},
                "spawnInterval": {"type": "number", "description": "Time between spawns (seconds).", "default": 3.0},
                "initialDelay": {"type": "number", "description": "Delay before first spawn."},
                "maxActive": {"type": "integer", "description": "Maximum active instances at once.", "default": 10},
                "maxTotal": {"type": "integer", "description": "Maximum total spawns (-1 for unlimited)."},
                "spawnPointMode": {
                    "type": "string",
                    "enum": ["sequential", "random", "randomNoRepeat"],
                    "description": "How to select spawn points.",
                },
                "usePool": {"type": "boolean", "description": "Use object pooling.", "default": True},
                "poolInitialSize": {"type": "integer", "description": "Initial pool size."},
                "loopWaves": {"type": "boolean", "description": "Loop waves after completing all."},
                "delayBetweenWaves": {"type": "number", "description": "Delay between waves (seconds)."},
                "waves": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "count": {"type": "integer", "description": "Number of enemies in wave."},
                            "delay": {"type": "number", "description": "Delay before wave starts."},
                            "spawnInterval": {"type": "number", "description": "Time between spawns in wave."},
                        },
                    },
                    "description": "Wave configurations.",
                },
                "positionRandomness": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    "description": "Random offset range for spawn positions.",
                },
                "position": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    "description": "Position for new spawn point.",
                },
                "pointPath": {"type": "string", "description": "Path to existing GameObject to use as spawn point."},
                "count": {"type": "integer", "description": "Number to spawn for spawnBurst."},
            },
        },
        ["operation"],
    )

    gamekit_timer_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["createTimer", "updateTimer", "inspectTimer", "deleteTimer", "createCooldown", "updateCooldown", "inspectCooldown", "deleteCooldown", "createCooldownManager", "addCooldownToManager", "inspectCooldownManager", "findByTimerId", "findByCooldownId"],
                    "description": "Timer/Cooldown operation to perform.",
                },
                "targetPath": {"type": "string", "description": "Target GameObject hierarchy path."},
                "timerId": {"type": "string", "description": "Unique timer identifier."},
                "duration": {"type": "number", "description": "Timer duration (seconds).", "default": 5.0},
                "loop": {"type": "boolean", "description": "Loop timer when complete.", "default": False},
                "autoStart": {"type": "boolean", "description": "Start timer automatically.", "default": False},
                "unscaledTime": {"type": "boolean", "description": "Use unscaled time (ignores Time.timeScale).", "default": False},
                "cooldownId": {"type": "string", "description": "Unique cooldown identifier."},
                "cooldownDuration": {"type": "number", "description": "Cooldown duration (seconds).", "default": 1.0},
                "startReady": {"type": "boolean", "description": "Start with cooldown ready.", "default": True},
                "cooldowns": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "id": {"type": "string", "description": "Cooldown ID."},
                            "duration": {"type": "number", "description": "Cooldown duration."},
                            "startReady": {"type": "boolean", "description": "Start ready."},
                        },
                    },
                    "description": "Cooldown configurations for CooldownManager.",
                },
            },
        },
        ["operation"],
    )

    gamekit_ai_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "inspect", "delete", "setTarget", "clearTarget", "setState", "addPatrolPoint", "clearPatrolPoints", "findByAIId"],
                    "description": "AI behavior operation to perform.",
                },
                "targetPath": {"type": "string", "description": "Target GameObject hierarchy path."},
                "aiId": {"type": "string", "description": "Unique AI behavior identifier."},
                "behaviorType": {
                    "type": "string",
                    "enum": ["patrol", "chase", "flee", "patrolAndChase"],
                    "description": "AI behavior type.",
                },
                "use2D": {"type": "boolean", "description": "Use 2D movement.", "default": True},
                "moveSpeed": {"type": "number", "description": "Movement speed.", "default": 3.0},
                "turnSpeed": {"type": "number", "description": "Turn speed.", "default": 5.0},
                "patrolMode": {
                    "type": "string",
                    "enum": ["loop", "pingPong", "random"],
                    "description": "Patrol point traversal mode.",
                },
                "waitTimeAtPoint": {"type": "number", "description": "Wait time at each patrol point."},
                "patrolPoints": {
                    "type": "array",
                    "items": {
                        "oneOf": [
                            {"type": "string", "description": "Path to existing GameObject."},
                            {
                                "type": "object",
                                "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                                "description": "Position to create new patrol point.",
                            },
                        ],
                    },
                    "description": "Patrol point paths or positions.",
                },
                "chaseTargetTag": {"type": "string", "description": "Tag of GameObjects to chase.", "default": "Player"},
                "chaseTargetPath": {"type": "string", "description": "Path to specific chase target."},
                "detectionRadius": {"type": "number", "description": "Detection range.", "default": 10.0},
                "loseTargetDistance": {"type": "number", "description": "Distance at which to lose target.", "default": 15.0},
                "fieldOfView": {"type": "number", "description": "Field of view in degrees.", "default": 360},
                "requireLineOfSight": {"type": "boolean", "description": "Require line of sight for detection."},
                "attackRange": {"type": "number", "description": "Attack range.", "default": 2.0},
                "attackCooldown": {"type": "number", "description": "Attack cooldown (seconds).", "default": 1.0},
                "fleeDistance": {"type": "number", "description": "Distance to flee."},
                "safeDistance": {"type": "number", "description": "Distance considered safe."},
                "state": {
                    "type": "string",
                    "enum": ["idle", "patrol", "chase", "attack", "flee", "return"],
                    "description": "AI state to set.",
                },
                "pointPath": {"type": "string", "description": "Path to GameObject for patrol point."},
                "position": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    "description": "Position for new patrol point.",
                },
            },
        },
        ["operation"],
    )

    # Phase 2 GameKit Schemas - Additional game mechanics

    gamekit_collectible_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "inspect", "delete", "collect", "respawn", "reset", "findByCollectibleId"],
                    "description": "Collectible operation to perform.",
                },
                "targetPath": {"type": "string", "description": "Target GameObject hierarchy path."},
                "collectibleId": {"type": "string", "description": "Unique collectible identifier."},
                "name": {"type": "string", "description": "Name for new collectible GameObject."},
                "collectibleType": {
                    "type": "string",
                    "enum": ["coin", "health", "mana", "powerup", "key", "ammo", "experience", "custom"],
                    "description": "Type of collectible item.",
                },
                "customTypeName": {"type": "string", "description": "Custom type name for 'custom' collectibleType."},
                "value": {"type": "number", "description": "Float value of collectible."},
                "intValue": {"type": "integer", "description": "Integer value of collectible."},
                "collectionBehavior": {
                    "type": "string",
                    "enum": ["destroy", "disable", "respawn"],
                    "description": "What happens when collected.",
                },
                "respawnDelay": {"type": "number", "description": "Respawn delay in seconds."},
                "collectable": {"type": "boolean", "description": "Whether the item can be collected."},
                "requiredTag": {"type": "string", "description": "Required tag for collector."},
                "is2D": {"type": "boolean", "description": "Use 2D collider instead of 3D."},
                "colliderRadius": {"type": "number", "description": "Collider radius."},
                "enableFloatAnimation": {"type": "boolean", "description": "Enable floating animation."},
                "floatAmplitude": {"type": "number", "description": "Float animation amplitude."},
                "floatFrequency": {"type": "number", "description": "Float animation frequency."},
                "enableRotation": {"type": "boolean", "description": "Enable rotation animation."},
                "rotationSpeed": {"type": "number", "description": "Rotation speed in degrees per second."},
                "rotationAxis": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    "description": "Rotation axis.",
                },
                "position": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    "description": "Position for new collectible.",
                },
                "deleteGameObject": {"type": "boolean", "description": "Delete entire GameObject (not just component)."},
            },
        },
        ["operation"],
    )

    gamekit_projectile_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "inspect", "delete", "launch", "setHomingTarget", "destroy", "findByProjectileId"],
                    "description": "Projectile operation to perform.",
                },
                "targetPath": {"type": "string", "description": "Target GameObject hierarchy path."},
                "projectileId": {"type": "string", "description": "Unique projectile identifier."},
                "name": {"type": "string", "description": "Name for new projectile GameObject."},
                "movementType": {
                    "type": "string",
                    "enum": ["transform", "rigidbody", "rigidbody2d"],
                    "description": "Movement physics type.",
                },
                "speed": {"type": "number", "description": "Projectile speed."},
                "damage": {"type": "number", "description": "Damage dealt on hit."},
                "lifetime": {"type": "number", "description": "Time before auto-destroy."},
                "useGravity": {"type": "boolean", "description": "Apply gravity to projectile."},
                "gravityScale": {"type": "number", "description": "Gravity scale multiplier."},
                "damageOnHit": {"type": "boolean", "description": "Apply damage on collision."},
                "targetTag": {"type": "string", "description": "Tag of valid targets."},
                "canBounce": {"type": "boolean", "description": "Allow bouncing off surfaces."},
                "maxBounces": {"type": "integer", "description": "Maximum bounce count."},
                "bounciness": {"type": "number", "description": "Bounce velocity retention (0-1)."},
                "isHoming": {"type": "boolean", "description": "Enable homing behavior."},
                "homingTargetPath": {"type": "string", "description": "Path to homing target."},
                "homingStrength": {"type": "number", "description": "Homing turning strength."},
                "maxHomingAngle": {"type": "number", "description": "Max homing angle in degrees."},
                "canPierce": {"type": "boolean", "description": "Pass through targets."},
                "maxPierceCount": {"type": "integer", "description": "Maximum pierce count."},
                "pierceDamageReduction": {"type": "number", "description": "Damage reduction per pierce (0-1)."},
                "direction": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    "description": "Launch direction.",
                },
                "targetPosition": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    "description": "Target position to launch at.",
                },
                "position": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    "description": "Initial position.",
                },
                "isTrigger": {"type": "boolean", "description": "Use trigger collider."},
                "colliderRadius": {"type": "number", "description": "Collider radius."},
                "deleteGameObject": {"type": "boolean", "description": "Delete entire GameObject."},
            },
        },
        ["operation"],
    )

    gamekit_waypoint_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create", "update", "inspect", "delete",
                        "addWaypoint", "removeWaypoint", "clearWaypoints",
                        "startPath", "stopPath", "pausePath", "resumePath", "resetPath",
                        "goToWaypoint", "findByWaypointId"
                    ],
                    "description": "Waypoint operation to perform.",
                },
                "targetPath": {"type": "string", "description": "Target GameObject hierarchy path."},
                "waypointId": {"type": "string", "description": "Unique waypoint follower identifier."},
                "name": {"type": "string", "description": "Name for new waypoint follower GameObject."},
                "pathMode": {
                    "type": "string",
                    "enum": ["once", "loop", "pingpong"],
                    "description": "Path traversal mode.",
                },
                "movementType": {
                    "type": "string",
                    "enum": ["transform", "rigidbody", "rigidbody2d"],
                    "description": "Movement physics type.",
                },
                "moveSpeed": {"type": "number", "description": "Movement speed."},
                "rotationSpeed": {"type": "number", "description": "Rotation speed."},
                "rotationMode": {
                    "type": "string",
                    "enum": ["none", "lookattarget", "aligntopath"],
                    "description": "Rotation behavior.",
                },
                "autoStart": {"type": "boolean", "description": "Start moving automatically."},
                "waitTimeAtPoint": {"type": "number", "description": "Wait time at each waypoint."},
                "startDelay": {"type": "number", "description": "Delay before starting path."},
                "smoothMovement": {"type": "boolean", "description": "Use smooth movement."},
                "smoothTime": {"type": "number", "description": "Smoothing time."},
                "arrivalThreshold": {"type": "number", "description": "Distance threshold for arrival."},
                "useLocalSpace": {"type": "boolean", "description": "Use local coordinates."},
                "waypointPositions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    },
                    "description": "Initial waypoint positions.",
                },
                "position": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    "description": "Position for addWaypoint or initial position.",
                },
                "index": {"type": "integer", "description": "Waypoint index for operations."},
                "deleteGameObject": {"type": "boolean", "description": "Delete entire GameObject."},
                "deleteWaypointChildren": {"type": "boolean", "description": "Delete waypoint child objects."},
            },
        },
        ["operation"],
    )

    gamekit_trigger_zone_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "inspect", "delete", "activate", "deactivate", "reset", "setTeleportDestination", "findByZoneId"],
                    "description": "Trigger zone operation to perform.",
                },
                "targetPath": {"type": "string", "description": "Target GameObject hierarchy path."},
                "zoneId": {"type": "string", "description": "Unique trigger zone identifier."},
                "name": {"type": "string", "description": "Name for new trigger zone GameObject."},
                "zoneType": {
                    "type": "string",
                    "enum": ["generic", "checkpoint", "damagezone", "healzone", "teleport", "speedboost", "slowdown", "killzone", "safezone", "trigger"],
                    "description": "Type of trigger zone.",
                },
                "triggerMode": {
                    "type": "string",
                    "enum": ["once", "onceperentity", "repeat", "whileinside"],
                    "description": "Trigger activation mode.",
                },
                "isActive": {"type": "boolean", "description": "Whether zone is active."},
                "requiredTag": {"type": "string", "description": "Required tag for triggering."},
                "cooldown": {"type": "number", "description": "Cooldown between triggers."},
                "maxTriggerCount": {"type": "integer", "description": "Maximum trigger count (0 = unlimited)."},
                "effectAmount": {"type": "number", "description": "Damage/heal amount for DamageZone/HealZone."},
                "effectInterval": {"type": "number", "description": "Effect interval for WhileInside mode."},
                "speedMultiplier": {"type": "number", "description": "Speed multiplier for SpeedBoost/SlowDown."},
                "checkpointIndex": {"type": "integer", "description": "Checkpoint index for ordering."},
                "destinationPath": {"type": "string", "description": "Teleport destination GameObject path."},
                "destinationPosition": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    "description": "Teleport destination position.",
                },
                "is2D": {"type": "boolean", "description": "Use 2D colliders."},
                "colliderShape": {
                    "type": "string",
                    "enum": ["box", "sphere", "circle", "capsule"],
                    "description": "Collider shape.",
                },
                "colliderSize": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    "description": "Collider size.",
                },
                "showGizmo": {"type": "boolean", "description": "Show editor gizmo."},
                "gizmoColor": {
                    "type": "object",
                    "properties": {"r": {"type": "number"}, "g": {"type": "number"}, "b": {"type": "number"}, "a": {"type": "number"}},
                    "description": "Gizmo color (RGBA 0-1).",
                },
                "position": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    "description": "Initial position.",
                },
                "deleteGameObject": {"type": "boolean", "description": "Delete entire GameObject."},
            },
        },
        ["operation"],
    )

    # Phase 3 GameKit Tools - Animation & Effects
    gamekit_animation_sync_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "inspect", "delete", "addSyncRule", "removeSyncRule", "addTriggerRule", "removeTriggerRule", "fireTrigger", "setParameter", "findBySyncId"],
                    "description": "Animation sync operation to perform.",
                },
                "targetPath": {"type": "string", "description": "Target GameObject hierarchy path."},
                "syncId": {"type": "string", "description": "Unique animation sync identifier."},
                "animatorPath": {"type": "string", "description": "Path to GameObject with Animator component."},
                "autoFindAnimator": {"type": "boolean", "description": "Auto-find Animator on same GameObject."},
                "syncRules": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "parameter": {"type": "string", "description": "Animator parameter name."},
                            "parameterType": {"type": "string", "enum": ["float", "int", "bool"], "description": "Parameter type."},
                            "sourceType": {"type": "string", "enum": ["rigidbody3d", "rigidbody2d", "transform", "health", "custom"], "description": "Value source type."},
                            "sourceProperty": {"type": "string", "description": "Property to read (e.g., 'velocity.magnitude', 'position.y')."},
                            "healthId": {"type": "string", "description": "Health ID when sourceType is 'health'."},
                            "multiplier": {"type": "number", "description": "Value multiplier (default: 1.0)."},
                            "boolThreshold": {"type": "number", "description": "Threshold for bool parameters."},
                        },
                    },
                    "description": "Sync rules for animator parameters.",
                },
                "triggers": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "triggerName": {"type": "string", "description": "Animator trigger name."},
                            "eventSource": {"type": "string", "enum": ["health", "input", "manual"], "description": "Event source type."},
                            "inputAction": {"type": "string", "description": "Input action name."},
                            "healthId": {"type": "string", "description": "Health component ID."},
                            "healthEvent": {"type": "string", "enum": ["OnDamaged", "OnHealed", "OnDeath", "OnRespawn", "OnInvincibilityStart", "OnInvincibilityEnd"], "description": "Health event type."},
                        },
                    },
                    "description": "Trigger rules for animator triggers.",
                },
                "rule": {
                    "type": "object",
                    "properties": {
                        "parameter": {"type": "string"},
                        "parameterType": {"type": "string"},
                        "sourceType": {"type": "string"},
                        "sourceProperty": {"type": "string"},
                        "healthId": {"type": "string"},
                        "multiplier": {"type": "number"},
                        "boolThreshold": {"type": "number"},
                    },
                    "description": "Single sync rule for addSyncRule operation.",
                },
                "trigger": {
                    "type": "object",
                    "properties": {
                        "triggerName": {"type": "string"},
                        "eventSource": {"type": "string"},
                        "inputAction": {"type": "string"},
                        "healthId": {"type": "string"},
                        "healthEvent": {"type": "string"},
                    },
                    "description": "Single trigger rule for addTriggerRule operation.",
                },
                "parameterName": {"type": "string", "description": "Parameter/trigger name for remove/set operations."},
                "triggerName": {"type": "string", "description": "Trigger name to fire."},
                "value": {"type": "number", "description": "Value for setParameter operation."},
            },
        },
        ["operation"],
    )

    gamekit_effect_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "inspect", "delete", "addComponent", "removeComponent", "clearComponents", "play", "playAtPosition", "playAtTransform", "shakeCamera", "flashScreen", "setTimeScale", "createManager", "registerEffect", "unregisterEffect", "findByEffectId", "listEffects"],
                    "description": "Effect operation to perform.",
                },
                "effectId": {"type": "string", "description": "Unique effect identifier."},
                "assetPath": {"type": "string", "description": "Effect asset path (e.g., 'Assets/Effects/HitEffect.asset')."},
                "targetPath": {"type": "string", "description": "Target GameObject path for playAtTransform."},
                "managerPath": {"type": "string", "description": "Path to EffectManager GameObject."},
                "newEffectId": {"type": "string", "description": "New effect ID for update operation."},
                "components": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "type": {"type": "string", "enum": ["particle", "sound", "cameraShake", "screenFlash", "timeScale"], "description": "Effect component type."},
                            "prefabPath": {"type": "string", "description": "Particle prefab path."},
                            "duration": {"type": "number", "description": "Effect duration."},
                            "attachToTarget": {"type": "boolean", "description": "Attach particle to target."},
                            "positionOffset": {"type": "object", "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}}},
                            "particleScale": {"type": "number", "description": "Particle scale multiplier."},
                            "clipPath": {"type": "string", "description": "Audio clip path."},
                            "volume": {"type": "number", "description": "Audio volume (0-1)."},
                            "pitchVariation": {"type": "number", "description": "Pitch variation range."},
                            "spatialBlend": {"type": "number", "description": "3D spatial blend (0=2D, 1=3D)."},
                            "intensity": {"type": "number", "description": "Camera shake intensity."},
                            "shakeDuration": {"type": "number", "description": "Camera shake duration."},
                            "frequency": {"type": "number", "description": "Camera shake frequency."},
                            "color": {"type": "object", "properties": {"r": {"type": "number"}, "g": {"type": "number"}, "b": {"type": "number"}, "a": {"type": "number"}}, "description": "Flash color."},
                            "flashDuration": {"type": "number", "description": "Screen flash duration."},
                            "fadeTime": {"type": "number", "description": "Flash fade time."},
                            "targetTimeScale": {"type": "number", "description": "Target time scale for slow-mo."},
                            "timeScaleDuration": {"type": "number", "description": "Time scale effect duration."},
                            "timeScaleTransition": {"type": "number", "description": "Time scale transition time."},
                        },
                    },
                    "description": "Effect components for create operation.",
                },
                "component": {
                    "type": "object",
                    "description": "Single effect component for addComponent operation.",
                },
                "componentIndex": {"type": "integer", "description": "Component index for removeComponent."},
                "position": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}},
                    "description": "Position for play operation.",
                },
                "persistent": {"type": "boolean", "description": "Manager persists across scenes (DontDestroyOnLoad)."},
            },
        },
        ["operation"],
    )

    # Phase 4: Persistence & Inventory
    gamekit_save_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["createProfile", "updateProfile", "inspectProfile", "deleteProfile", "addTarget", "removeTarget", "clearTargets", "save", "load", "listSlots", "deleteSlot", "createManager", "inspectManager", "deleteManager", "findByProfileId"],
                    "description": "Save system operation.",
                },
                "profileId": {"type": "string", "description": "Save profile identifier."},
                "assetPath": {"type": "string", "description": "Asset path for profile (e.g., 'Assets/GameKit/SaveProfiles/MainSave.asset')."},
                "targetPath": {"type": "string", "description": "GameObject path for manager."},
                "slotId": {"type": "string", "description": "Save slot identifier for save/load operations."},
                "saveTargets": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "type": {"type": "string", "enum": ["transform", "component", "resourceManager", "health", "sceneFlow", "inventory", "playerPrefs"], "description": "Type of data to save."},
                            "saveKey": {"type": "string", "description": "Unique key for this save data."},
                            "gameObjectPath": {"type": "string", "description": "GameObject path for transform/component saves."},
                            "savePosition": {"type": "boolean", "description": "Save position (for transform type)."},
                            "saveRotation": {"type": "boolean", "description": "Save rotation (for transform type)."},
                            "saveScale": {"type": "boolean", "description": "Save scale (for transform type)."},
                            "componentType": {"type": "string", "description": "Component type name (for component type)."},
                            "properties": {"type": "array", "items": {"type": "string"}, "description": "Properties to save from component."},
                            "resourceManagerId": {"type": "string", "description": "ResourceManager ID (for resourceManager type)."},
                            "healthId": {"type": "string", "description": "Health ID (for health type)."},
                            "sceneFlowId": {"type": "string", "description": "SceneFlow ID (for sceneFlow type)."},
                            "inventoryId": {"type": "string", "description": "Inventory ID (for inventory type)."},
                        },
                    },
                    "description": "Save targets for createProfile operation.",
                },
                "target": {
                    "type": "object",
                    "description": "Single save target for addTarget operation.",
                },
                "saveKey": {"type": "string", "description": "Save key for removeTarget operation."},
                "autoSave": {
                    "type": "object",
                    "properties": {
                        "enabled": {"type": "boolean", "description": "Enable auto-save."},
                        "intervalSeconds": {"type": "number", "description": "Auto-save interval in seconds."},
                        "onSceneChange": {"type": "boolean", "description": "Auto-save on scene change."},
                        "onApplicationPause": {"type": "boolean", "description": "Auto-save on application pause."},
                        "autoSaveSlotId": {"type": "string", "description": "Slot ID for auto-save."},
                    },
                    "description": "Auto-save configuration.",
                },
            },
        },
        ["operation"],
    )

    gamekit_inventory_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "inspect", "delete", "defineItem", "updateItem", "inspectItem", "deleteItem", "addItem", "removeItem", "useItem", "equip", "unequip", "getEquipped", "clear", "sort", "findByInventoryId", "findByItemId"],
                    "description": "Inventory operation.",
                },
                "inventoryId": {"type": "string", "description": "Inventory identifier."},
                "gameObjectPath": {"type": "string", "description": "GameObject path for inventory component."},
                "maxSlots": {"type": "integer", "description": "Maximum inventory slots (default: 20)."},
                "categories": {"type": "array", "items": {"type": "string"}, "description": "Allowed item categories (e.g., ['weapon', 'armor', 'consumable'])."},
                "stackableCategories": {"type": "array", "items": {"type": "string"}, "description": "Categories that allow stacking."},
                "maxStackSize": {"type": "integer", "description": "Default max stack size (default: 99)."},
                "itemId": {"type": "string", "description": "Item identifier."},
                "assetPath": {"type": "string", "description": "Asset path for item (e.g., 'Assets/GameKit/Items/HealthPotion.asset')."},
                "quantity": {"type": "integer", "description": "Quantity to add/remove (default: 1)."},
                "slotIndex": {"type": "integer", "description": "Slot index for useItem/equip operations."},
                "equipSlot": {"type": "string", "description": "Equipment slot (mainHand/offHand/head/body/hands/feet/accessory1/accessory2)."},
                "displayName": {"type": "string", "description": "Item display name."},
                "description": {"type": "string", "description": "Item description."},
                "category": {"type": "string", "description": "Item category (weapon/armor/consumable/material/key/quest/misc)."},
                "itemData": {
                    "type": "object",
                    "properties": {
                        "displayName": {"type": "string", "description": "Item display name."},
                        "description": {"type": "string", "description": "Item description."},
                        "category": {"type": "string", "description": "Item category."},
                        "stackable": {"type": "boolean", "description": "Can items stack."},
                        "maxStack": {"type": "integer", "description": "Max stack size."},
                        "buyPrice": {"type": "integer", "description": "Buy price."},
                        "sellPrice": {"type": "integer", "description": "Sell price."},
                        "equippable": {"type": "boolean", "description": "Can item be equipped."},
                        "equipSlot": {"type": "string", "description": "Equipment slot for equippable items."},
                        "equipStats": {
                            "type": "array",
                            "items": {
                                "type": "object",
                                "properties": {
                                    "statName": {"type": "string"},
                                    "modifierType": {"type": "string", "enum": ["flat", "percentAdd", "percentMultiply"]},
                                    "value": {"type": "number"},
                                },
                            },
                            "description": "Stat modifiers when equipped.",
                        },
                        "onUse": {
                            "type": "object",
                            "properties": {
                                "type": {"type": "string", "enum": ["none", "heal", "addResource", "playEffect", "custom"], "description": "Use action type."},
                                "healthId": {"type": "string", "description": "Health ID for heal action."},
                                "amount": {"type": "number", "description": "Heal/resource amount."},
                                "resourceManagerId": {"type": "string", "description": "ResourceManager ID."},
                                "resourceName": {"type": "string", "description": "Resource name to add."},
                                "resourceAmount": {"type": "number", "description": "Resource amount."},
                                "effectId": {"type": "string", "description": "Effect ID to play."},
                                "consumeOnUse": {"type": "boolean", "description": "Consume item on use (default: true)."},
                            },
                            "description": "Action to perform when item is used.",
                        },
                    },
                    "description": "Item data for defineItem operation.",
                },
            },
        },
        ["operation"],
    )

    # Phase 5 GameKit Schemas - Story & Quest Systems
    gamekit_dialogue_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "createDialogue", "updateDialogue", "inspectDialogue", "deleteDialogue",
                        "addNode", "updateNode", "removeNode",
                        "addChoice", "updateChoice", "removeChoice",
                        "startDialogue", "selectChoice", "advanceDialogue", "endDialogue",
                        "createManager", "inspectManager", "deleteManager",
                        "findByDialogueId"
                    ],
                    "description": "Dialogue operation.",
                },
                "dialogueId": {"type": "string", "description": "Dialogue identifier."},
                "assetPath": {"type": "string", "description": "Asset path for dialogue (e.g., 'Assets/Dialogues/NPC_Greeting.asset')."},
                "gameObjectPath": {"type": "string", "description": "GameObject path for dialogue manager."},
                "managerId": {"type": "string", "description": "Dialogue manager identifier."},
                "displayName": {"type": "string", "description": "Dialogue display name."},
                "description": {"type": "string", "description": "Dialogue description."},
                "nodeId": {"type": "string", "description": "Node identifier."},
                "choiceId": {"type": "string", "description": "Choice identifier."},
                "choiceIndex": {"type": "integer", "description": "Choice index for selectChoice operation."},
                "nodeData": {
                    "type": "object",
                    "properties": {
                        "nodeId": {"type": "string", "description": "Node ID."},
                        "nodeType": {"type": "string", "enum": ["dialogue", "choice", "branch", "action", "exit"], "description": "Node type."},
                        "speakerName": {"type": "string", "description": "Speaker name for dialogue nodes."},
                        "dialogueText": {"type": "string", "description": "Dialogue text content."},
                        "nextNodeId": {"type": "string", "description": "Next node ID."},
                        "delaySeconds": {"type": "number", "description": "Delay before auto-advancing."},
                    },
                    "description": "Node data for addNode/updateNode operations.",
                },
                "choiceData": {
                    "type": "object",
                    "properties": {
                        "choiceId": {"type": "string", "description": "Choice ID."},
                        "choiceText": {"type": "string", "description": "Choice display text."},
                        "targetNodeId": {"type": "string", "description": "Target node when selected."},
                        "conditions": {
                            "type": "array",
                            "items": {
                                "type": "object",
                                "properties": {
                                    "type": {"type": "string", "enum": ["quest", "resource", "inventory", "variable", "health", "custom"]},
                                    "questId": {"type": "string"},
                                    "questState": {"type": "string"},
                                    "resourceManagerId": {"type": "string"},
                                    "resourceName": {"type": "string"},
                                    "comparison": {"type": "string", "enum": ["greaterThan", "lessThan", "equalTo", "greaterOrEqual", "lessOrEqual", "notEqual"]},
                                    "value": {"type": "number"},
                                },
                            },
                            "description": "Conditions for this choice to be available.",
                        },
                    },
                    "description": "Choice data for addChoice/updateChoice operations.",
                },
            },
        },
        ["operation"],
    )

    gamekit_quest_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "createQuest", "updateQuest", "inspectQuest", "deleteQuest",
                        "addObjective", "updateObjective", "removeObjective",
                        "addPrerequisite", "removePrerequisite",
                        "addReward", "removeReward",
                        "startQuest", "completeQuest", "failQuest", "abandonQuest",
                        "updateProgress", "listQuests",
                        "createManager", "inspectManager", "deleteManager",
                        "findByQuestId"
                    ],
                    "description": "Quest operation.",
                },
                "questId": {"type": "string", "description": "Quest identifier."},
                "assetPath": {"type": "string", "description": "Asset path for quest (e.g., 'Assets/Quests/MainQuest_01.asset')."},
                "gameObjectPath": {"type": "string", "description": "GameObject path for quest manager."},
                "managerId": {"type": "string", "description": "Quest manager identifier."},
                "displayName": {"type": "string", "description": "Quest display name."},
                "description": {"type": "string", "description": "Quest description."},
                "category": {"type": "string", "enum": ["main", "side", "daily", "weekly", "event", "tutorial", "hidden", "custom"], "description": "Quest category."},
                "objectiveId": {"type": "string", "description": "Objective identifier."},
                "progressAmount": {"type": "integer", "description": "Progress amount for updateProgress."},
                "filter": {"type": "string", "enum": ["all", "active", "completed", "failed", "available"], "description": "Filter for listQuests."},
                "objectiveData": {
                    "type": "object",
                    "properties": {
                        "objectiveId": {"type": "string", "description": "Objective ID."},
                        "objectiveType": {"type": "string", "enum": ["kill", "collect", "talk", "location", "interact", "escort", "defend", "deliver", "explore", "craft", "custom"], "description": "Objective type."},
                        "description": {"type": "string", "description": "Objective description."},
                        "targetId": {"type": "string", "description": "Target ID (enemy type, item ID, NPC ID, etc.)."},
                        "requiredAmount": {"type": "integer", "description": "Required amount to complete."},
                        "isOptional": {"type": "boolean", "description": "Whether objective is optional."},
                        "isSilent": {"type": "boolean", "description": "Whether to hide objective from UI."},
                    },
                    "description": "Objective data for addObjective/updateObjective operations.",
                },
                "rewardData": {
                    "type": "object",
                    "properties": {
                        "rewardId": {"type": "string", "description": "Reward ID."},
                        "rewardType": {"type": "string", "enum": ["resource", "item", "experience", "reputation", "unlock", "dialogue", "custom"], "description": "Reward type."},
                        "resourceManagerId": {"type": "string", "description": "ResourceManager ID for resource rewards."},
                        "resourceName": {"type": "string", "description": "Resource name."},
                        "amount": {"type": "number", "description": "Reward amount."},
                        "itemId": {"type": "string", "description": "Item ID for item rewards."},
                        "quantity": {"type": "integer", "description": "Item quantity."},
                    },
                    "description": "Reward data for addReward operation.",
                },
                "prerequisiteData": {
                    "type": "object",
                    "properties": {
                        "prerequisiteId": {"type": "string", "description": "Prerequisite ID."},
                        "type": {"type": "string", "enum": ["questComplete", "questActive", "level", "resource", "item", "reputation", "custom"], "description": "Prerequisite type."},
                        "questId": {"type": "string", "description": "Quest ID for quest-based prerequisites."},
                        "resourceManagerId": {"type": "string", "description": "ResourceManager ID for resource prerequisites."},
                        "resourceName": {"type": "string", "description": "Resource name."},
                        "requiredValue": {"type": "number", "description": "Required value."},
                    },
                    "description": "Prerequisite data for addPrerequisite operation.",
                },
            },
        },
        ["operation"],
    )

    gamekit_status_effect_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "defineEffect", "updateEffect", "inspectEffect", "deleteEffect",
                        "addModifier", "updateModifier", "removeModifier", "clearModifiers",
                        "create", "update", "inspect", "delete",
                        "applyEffect", "removeEffect", "clearEffects",
                        "getActiveEffects", "getStatModifier",
                        "findByEffectId", "findByReceiverId", "listEffects"
                    ],
                    "description": "Status effect operation.",
                },
                "effectId": {"type": "string", "description": "Effect identifier."},
                "receiverId": {"type": "string", "description": "Receiver component identifier."},
                "assetPath": {"type": "string", "description": "Asset path for effect (e.g., 'Assets/Effects/Poison.asset')."},
                "gameObjectPath": {"type": "string", "description": "GameObject path for receiver component."},
                "displayName": {"type": "string", "description": "Effect display name."},
                "description": {"type": "string", "description": "Effect description."},
                "effectType": {"type": "string", "enum": ["buff", "debuff", "neutral"], "description": "Effect type."},
                "category": {"type": "string", "enum": ["generic", "poison", "burn", "freeze", "stun", "slow", "haste", "shield", "regeneration", "invincibility", "weakness", "strength", "custom"], "description": "Effect category."},
                "duration": {"type": "number", "description": "Effect duration in seconds."},
                "isPermanent": {"type": "boolean", "description": "Whether effect is permanent."},
                "stackable": {"type": "boolean", "description": "Whether effect can stack."},
                "maxStacks": {"type": "integer", "description": "Maximum stacks."},
                "stackBehavior": {"type": "string", "enum": ["refreshDuration", "addDuration", "independent", "increaseStacks"], "description": "Behavior when effect is applied while active."},
                "tickInterval": {"type": "number", "description": "Tick interval in seconds."},
                "tickOnApply": {"type": "boolean", "description": "Whether to tick immediately on apply."},
                "stacks": {"type": "integer", "description": "Number of stacks for applyEffect."},
                "modifierId": {"type": "string", "description": "Modifier identifier."},
                "statName": {"type": "string", "description": "Stat name for getStatModifier."},
                "modifierData": {
                    "type": "object",
                    "properties": {
                        "modifierId": {"type": "string", "description": "Modifier ID."},
                        "type": {"type": "string", "enum": ["statModifier", "damageOverTime", "healOverTime", "stun", "silence", "invincible", "custom"], "description": "Modifier type."},
                        "targetHealthId": {"type": "string", "description": "Target health component ID."},
                        "targetStat": {"type": "string", "description": "Target stat name."},
                        "value": {"type": "number", "description": "Modifier value."},
                        "operation": {"type": "string", "enum": ["add", "subtract", "multiply", "divide", "set", "percentAdd", "percentMultiply"], "description": "Modifier operation."},
                        "scaleWithStacks": {"type": "boolean", "description": "Scale value with stacks."},
                        "damagePerTick": {"type": "number", "description": "Damage per tick for DoT."},
                        "healPerTick": {"type": "number", "description": "Heal per tick for HoT."},
                        "damageType": {"type": "string", "enum": ["physical", "magic", "fire", "ice", "lightning", "poison", "true"], "description": "Damage type."},
                    },
                    "description": "Modifier data for addModifier/updateModifier operations.",
                },
                "effectData": {
                    "type": "object",
                    "properties": {
                        "displayName": {"type": "string"},
                        "description": {"type": "string"},
                        "effectType": {"type": "string", "enum": ["buff", "debuff", "neutral"]},
                        "category": {"type": "string"},
                        "duration": {"type": "number"},
                        "isPermanent": {"type": "boolean"},
                        "stackable": {"type": "boolean"},
                        "maxStacks": {"type": "integer"},
                        "stackBehavior": {"type": "string"},
                        "tickInterval": {"type": "number"},
                        "tickOnApply": {"type": "boolean"},
                        "particleEffectId": {"type": "string"},
                        "onApplyEffectId": {"type": "string"},
                        "onRemoveEffectId": {"type": "string"},
                        "onTickEffectId": {"type": "string"},
                    },
                    "description": "Effect data for defineEffect/updateEffect operations.",
                },
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
                    "enum": ["create", "clone", "inspect", "delete", "show", "hide", "toggle"],
                    "description": "UI hierarchy operation.",
                },
                "parentPath": {"type": "string", "description": "Parent GameObject path (usually Canvas or Panel)."},
                "gameObjectPath": {"type": "string", "description": "Target UI hierarchy root path for inspect/delete/show/hide/toggle/clone."},
                "hierarchyId": {"type": "string", "description": "Optional identifier for the UI hierarchy (used for referencing)."},
                "hierarchy": {
                    "type": "object",
                    "description": "Declarative UI hierarchy definition (recursive structure).",
                    "additionalProperties": True,
                },
                "recursive": {"type": "boolean", "description": "Apply visibility recursively to children (default: true)."},
                "interactable": {"type": "boolean", "description": "Set interactable state when showing/hiding."},
                "blocksRaycasts": {"type": "boolean", "description": "Set blocksRaycasts state when showing/hiding."},
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

    # ======================================================================
    # Development Cycle & Visual Tools (ROADMAP_MCP_TOOLS.md)
    # ======================================================================

    # Phase 1.1: PlayMode Control
    playmode_control_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["play", "pause", "unpause", "stop", "step", "getState"],
                    "description": "PlayMode control operation.",
                },
            },
        },
        ["operation"],
    )

    # Phase 1.2: Console Log
    console_log_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["getRecent", "getErrors", "getWarnings", "getLogs", "clear", "getCompilationErrors", "getSummary"],
                    "description": "Console log operation.",
                },
                "count": {"type": "integer", "description": "Number of logs to retrieve (default: 50 for getRecent, 100 for filtered)."},
            },
        },
        ["operation"],
    )

    # Phase 2.1: Material Bundle
    material_bundle_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "setTexture", "setColor", "applyPreset", "inspect", "applyToObjects", "delete", "duplicate", "listPresets"],
                    "description": "Material bundle operation.",
                },
                "name": {"type": "string", "description": "Material name."},
                "savePath": {"type": "string", "description": "Save path for material asset (e.g., 'Assets/Materials/MyMat.mat')."},
                "materialPath": {"type": "string", "description": "Existing material asset path."},
                "preset": {
                    "type": "string",
                    "enum": ["unlit", "lit", "transparent", "cutout", "fade", "sprite", "ui", "emissive", "metallic", "glass"],
                    "description": "Material preset to apply.",
                },
                "shader": {"type": "string", "description": "Shader name override (e.g., 'Standard', 'Universal Render Pipeline/Lit')."},
                "color": {"type": "string", "description": "Main color (hex format: '#RRGGBB' or '#RRGGBBAA')."},
                "metallic": {"type": "number", "description": "Metallic value (0-1)."},
                "smoothness": {"type": "number", "description": "Smoothness value (0-1)."},
                "emission": {"type": "boolean", "description": "Enable emission."},
                "emissionColor": {"type": "string", "description": "Emission color (hex format)."},
                "emissionIntensity": {"type": "number", "description": "Emission intensity."},
                "texturePath": {"type": "string", "description": "Texture asset path."},
                "textureProperty": {"type": "string", "description": "Texture property name (e.g., '_MainTex', '_BumpMap')."},
                "tiling": {"type": "object", "properties": {"x": {"type": "number"}, "y": {"type": "number"}}, "description": "Texture tiling."},
                "offset": {"type": "object", "properties": {"x": {"type": "number"}, "y": {"type": "number"}}, "description": "Texture offset."},
                "pattern": {"type": "string", "description": "Pattern for applyToObjects (e.g., 'Cube*')."},
                "targetMaterialPath": {"type": "string", "description": "Target path for duplicate operation."},
            },
        },
        ["operation"],
    )

    # Phase 2.2: Light Bundle
    light_bundle_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "inspect", "delete", "applyPreset", "createLightingSetup", "listPresets"],
                    "description": "Light bundle operation.",
                },
                "name": {"type": "string", "description": "Light GameObject name."},
                "gameObjectPath": {"type": "string", "description": "Existing light GameObject path."},
                "lightType": {
                    "type": "string",
                    "enum": ["Directional", "Point", "Spot", "Area"],
                    "description": "Light type.",
                },
                "preset": {
                    "type": "string",
                    "enum": ["daylight", "moonlight", "warm", "cool", "spotlight", "candle", "neon"],
                    "description": "Light preset to apply.",
                },
                "setupPreset": {
                    "type": "string",
                    "enum": ["daylight", "nighttime", "indoor", "dramatic", "studio", "sunset"],
                    "description": "Complete lighting setup preset.",
                },
                "color": {"type": "string", "description": "Light color (hex format)."},
                "intensity": {"type": "number", "description": "Light intensity."},
                "range": {"type": "number", "description": "Light range (for Point/Spot)."},
                "spotAngle": {"type": "number", "description": "Spot light angle."},
                "shadows": {
                    "type": "string",
                    "enum": ["none", "hard", "soft"],
                    "description": "Shadow type.",
                },
                "position": {"type": "object", "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}}, "description": "Light position."},
                "rotation": {"type": "object", "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}}, "description": "Light rotation."},
                "renderMode": {
                    "type": "string",
                    "enum": ["Auto", "ForcePixel", "ForceVertex"],
                    "description": "Light render mode.",
                },
                "bounceIntensity": {"type": "number", "description": "Bounce intensity for indirect lighting."},
            },
        },
        ["operation"],
    )

    # Phase 2.3: Particle Bundle
    particle_bundle_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "applyPreset", "play", "stop", "pause", "inspect", "delete", "duplicate", "listPresets"],
                    "description": "Particle bundle operation.",
                },
                "name": {"type": "string", "description": "ParticleSystem GameObject name."},
                "gameObjectPath": {"type": "string", "description": "Existing ParticleSystem GameObject path."},
                "preset": {
                    "type": "string",
                    "enum": ["explosion", "fire", "smoke", "sparkle", "rain", "snow", "dust", "trail", "hit", "heal", "magic", "leaves"],
                    "description": "Particle preset to apply.",
                },
                "position": {"type": "object", "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "z": {"type": "number"}}, "description": "Particle system position."},
                "startSize": {"type": "number", "description": "Start size."},
                "startLifetime": {"type": "number", "description": "Start lifetime."},
                "startSpeed": {"type": "number", "description": "Start speed."},
                "startColor": {"type": "string", "description": "Start color (hex format)."},
                "maxParticles": {"type": "integer", "description": "Maximum particles."},
                "emissionRate": {"type": "number", "description": "Emission rate over time."},
                "duration": {"type": "number", "description": "System duration."},
                "loop": {"type": "boolean", "description": "Loop playback."},
                "playOnAwake": {"type": "boolean", "description": "Play on awake."},
                "simulationSpace": {
                    "type": "string",
                    "enum": ["Local", "World", "Custom"],
                    "description": "Simulation space.",
                },
                "gravity": {"type": "number", "description": "Gravity modifier."},
                "shape": {
                    "type": "string",
                    "enum": ["Sphere", "Hemisphere", "Cone", "Box", "Circle", "Edge"],
                    "description": "Emission shape.",
                },
                "shapeRadius": {"type": "number", "description": "Shape radius."},
                "shapeAngle": {"type": "number", "description": "Shape angle (for Cone)."},
                "targetPath": {"type": "string", "description": "Target path for duplicate operation."},
            },
        },
        ["operation"],
    )

    # Phase 3.1: Animation3D Bundle
    animation3d_bundle_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["setupAnimator", "createController", "addState", "addTransition", "setParameter", "addBlendTree", "createAvatarMask", "inspect", "delete", "listParameters", "listStates"],
                    "description": "Animation3D bundle operation.",
                },
                "gameObjectPath": {"type": "string", "description": "Target GameObject for setupAnimator."},
                "controllerPath": {"type": "string", "description": "AnimatorController asset path."},
                "name": {"type": "string", "description": "Name for new controller/mask."},
                "savePath": {"type": "string", "description": "Save path for asset."},
                "applyRootMotion": {"type": "boolean", "description": "Apply root motion."},
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
                "parameters": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": {"type": "string"},
                            "type": {"type": "string", "enum": ["float", "int", "bool", "trigger"]},
                            "defaultValue": {},
                        },
                    },
                    "description": "Animator parameters.",
                },
                "states": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": {"type": "string"},
                            "clip": {"type": "string"},
                            "isDefault": {"type": "boolean"},
                            "speed": {"type": "number"},
                        },
                    },
                    "description": "Animation states.",
                },
                "transitions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "from": {"type": "string"},
                            "to": {"type": "string"},
                            "hasExitTime": {"type": "boolean"},
                            "exitTime": {"type": "number"},
                            "duration": {"type": "number"},
                            "conditions": {"type": "array"},
                        },
                    },
                    "description": "State transitions.",
                },
                "stateName": {"type": "string", "description": "State name."},
                "clipPath": {"type": "string", "description": "Animation clip path."},
                "layerIndex": {"type": "integer", "description": "Layer index (default: 0)."},
                "isDefault": {"type": "boolean", "description": "Set as default state."},
                "speed": {"type": "number", "description": "State playback speed."},
                "fromState": {"type": "string", "description": "Source state for transition ('Any' for AnyState)."},
                "toState": {"type": "string", "description": "Destination state for transition."},
                "hasExitTime": {"type": "boolean", "description": "Transition has exit time."},
                "exitTime": {"type": "number", "description": "Exit time (0-1)."},
                "duration": {"type": "number", "description": "Transition duration."},
                "conditions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "param": {"type": "string"},
                            "mode": {"type": "string", "enum": ["if", "ifnot", "greater", "less", "equals", "notequal"]},
                            "value": {"type": "number"},
                        },
                    },
                    "description": "Transition conditions.",
                },
                "parameterName": {"type": "string", "description": "Parameter name."},
                "parameterType": {"type": "string", "enum": ["float", "int", "bool", "trigger"], "description": "Parameter type."},
                "defaultValue": {"description": "Default parameter value."},
                "blendTreeName": {"type": "string", "description": "BlendTree name."},
                "blendType": {
                    "type": "string",
                    "enum": ["Simple1D", "SimpleDirectional2D", "FreeformDirectional2D", "FreeformCartesian2D"],
                    "description": "BlendTree type.",
                },
                "blendParameter": {"type": "string", "description": "Blend parameter name."},
                "blendParameterY": {"type": "string", "description": "Blend parameter Y (for 2D)."},
                "motions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "clip": {"type": "string"},
                            "threshold": {"type": "number"},
                            "positionX": {"type": "number"},
                            "positionY": {"type": "number"},
                        },
                    },
                    "description": "BlendTree motions.",
                },
                "enabledParts": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Enabled body parts for AvatarMask.",
                },
                "disabledParts": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Disabled body parts for AvatarMask.",
                },
            },
        },
        ["operation"],
    )

    # Phase 3.2: Event Wiring
    event_wiring_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["wire", "unwire", "inspect", "listEvents", "clearEvent", "wireMultiple"],
                    "description": "Event wiring operation.",
                },
                "source": {
                    "type": "object",
                    "properties": {
                        "gameObject": {"type": "string", "description": "Source GameObject path."},
                        "component": {"type": "string", "description": "Source component type (e.g., 'Button', 'UnityEngine.UI.Button')."},
                        "event": {"type": "string", "description": "Event name (e.g., 'onClick', 'm_OnClick')."},
                    },
                    "description": "Event source.",
                },
                "target": {
                    "type": "object",
                    "properties": {
                        "gameObject": {"type": "string", "description": "Target GameObject path."},
                        "component": {"type": "string", "description": "Target component type (optional, defaults to searching GameObject)."},
                        "method": {"type": "string", "description": "Target method name."},
                        "mode": {
                            "type": "string",
                            "enum": ["Void", "Int", "Float", "String", "Bool", "Object"],
                            "description": "Argument mode.",
                        },
                        "argument": {"description": "Argument value (type depends on mode)."},
                    },
                    "description": "Event target.",
                },
                "gameObjectPath": {"type": "string", "description": "GameObject for listEvents."},
                "componentType": {"type": "string", "description": "Component type for listEvents (optional)."},
                "targetGameObject": {"type": "string", "description": "Target GameObject for unwire filtering."},
                "targetMethod": {"type": "string", "description": "Target method for unwire filtering."},
                "listenerIndex": {"type": "integer", "description": "Specific listener index for unwire."},
                "wirings": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "source": {"type": "object"},
                            "target": {"type": "object"},
                        },
                    },
                    "description": "Multiple wirings for wireMultiple.",
                },
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
        types.Tool(
            name="unity_compilation_await",
            description="Wait for Unity's script compilation to complete. Use this after creating or modifying C# scripts to ensure the code is compiled before using new types or components. Returns compilation status including any errors or warnings. Supports configurable timeout (default 60 seconds).",
            inputSchema=compilation_await_schema,
        ),
        batch_sequential_tool,  # Sequential batch execution with resume capability
        types.Tool(
            name="unity_scene_crud",
            description="Unity scene management: create/load/save/delete/duplicate scenes, inspect scene hierarchy with optional component filtering. Use 'inspect' operation with 'includeHierarchy=true' to get scene context before making changes. Supports additive scene loading. For build settings operations (add/remove/reorder scenes), use unity_projectSettings_crud tool instead.",
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

**Unity Object References:** For properties that expect Unity Object references, use:
- `{ "$ref": "path" }` - Object reference format (recommended)
- `"path"` - Simple string (for UnityEngine.Object types)

Path auto-detection:
- "Assets/..." or "Packages/..." → Load from AssetDatabase
- Other paths (e.g., "Canvas/Panel/Button") → Find scene objects by hierarchy path

Scene object search features:
- Finds inactive GameObjects (SetActive=false)
- Searches all loaded scenes
- Supports hierarchy paths: "Parent/Child/GrandChild"

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
            description="High-level GameKit Interaction: create trigger-based interactions with declarative actions. Choose from 5 trigger types (collision/trigger/raycast/proximity/input) and 5 action types (spawnPrefab/destroyObject/playSound/sendMessage/changeScene). Add conditions (tag/layer/distance/custom) for filtering. Supports both 2D (BoxCollider2D, CircleCollider2D, CapsuleCollider2D, PolygonCollider2D) and 3D (BoxCollider, SphereCollider, CapsuleCollider, MeshCollider) colliders via is2D parameter. Perfect for collectibles, doors, switches, treasure chests, and interactive objects. No scripting required - define complete interactions declaratively. **When to use:** For custom script actions (sendMessage, spawnPrefab). For built-in zone effects (damage, heal, checkpoint, teleport), use unity_gamekit_trigger_zone instead.",
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
            description="""Mid-level declarative UI hierarchy management: create complex UI structures from single JSON definitions, manage visibility states.

**Operations:**
- create: Build complete UI hierarchy from declarative JSON structure (panels, buttons, text, images, inputs, scrollviews, toggles, sliders, dropdowns)
- clone: Duplicate existing UI hierarchy with optional rename
- inspect: Export UI hierarchy as JSON structure
- delete: Remove UI hierarchy
- show/hide/toggle: Control visibility using CanvasGroup (alpha, interactable, blocksRaycasts)

For navigation, use unity_ui_navigation tool.

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
        # Phase 1 GameKit Tools - Common game mechanics
        types.Tool(
            name="unity_gamekit_health",
            description="""High-level GameKit Health: create and manage health/damage systems for game entities.

**Operations:**
- create: Add GameKitHealth component with configurable settings
- update: Modify health parameters (maxHealth, invincibilityDuration, deathBehavior)
- inspect: View health status and configuration
- delete: Remove GameKitHealth component
- applyDamage: Deal damage to the entity
- heal: Restore health
- kill: Instantly kill the entity
- respawn: Respawn at configured position
- setInvincible: Enable/disable invincibility
- findByHealthId: Find health component by ID

**Death Behaviors:** Destroy, Disable, Respawn, EventOnly

**Features:**
- Invincibility frames after damage
- UnityEvents: OnDamage, OnHeal, OnDeath, OnRespawn, OnInvincibilityStart/End
- Auto-respawn with configurable delay

**Example:**
```python
# Create a player health component
unity_gamekit_health({
    "operation": "create",
    "gameObjectPath": "Player",
    "healthId": "player_health",
    "maxHealth": 100,
    "invincibilityDuration": 1.0,
    "deathBehavior": "Respawn",
    "respawnDelay": 2.0
})

# Apply damage
unity_gamekit_health({
    "operation": "applyDamage",
    "healthId": "player_health",
    "amount": 25
})
```""",
            inputSchema=gamekit_health_schema,
        ),
        types.Tool(
            name="unity_gamekit_spawner",
            description="""High-level GameKit Spawner: create spawn systems for enemies, items, and objects.

**Operations:**
- create: Add GameKitSpawner component
- update: Modify spawner settings
- inspect: View spawner status
- delete: Remove spawner
- start: Start spawning
- stop: Stop spawning
- reset: Reset spawn count
- spawnOne: Spawn a single instance
- spawnBurst: Spawn multiple at once
- despawnAll: Despawn all spawned objects
- addSpawnPoint: Add spawn position
- addWave: Add wave configuration
- findBySpawnerId: Find spawner by ID

**Spawn Modes:** Interval, Wave, Burst, Manual

**Features:**
- Object pooling support
- Multiple spawn points
- Wave system with enemy counts and delays
- Random rotation/offset options
- UnityEvents: OnSpawn, OnDespawn, OnWaveStart/Complete, OnAllWavesComplete

**Example:**
```python
# Create enemy spawner
unity_gamekit_spawner({
    "operation": "create",
    "gameObjectPath": "EnemySpawner",
    "spawnerId": "enemy_spawner",
    "prefabPath": "Assets/Prefabs/Enemy.prefab",
    "spawnMode": "Interval",
    "spawnInterval": 3.0,
    "maxSpawnCount": 10,
    "autoStart": True
})

# Add wave configuration
unity_gamekit_spawner({
    "operation": "addWave",
    "spawnerId": "enemy_spawner",
    "enemyCount": 5,
    "spawnInterval": 1.0,
    "delayAfterWave": 5.0
})
```""",
            inputSchema=gamekit_spawner_schema,
        ),
        types.Tool(
            name="unity_gamekit_timer",
            description="""High-level GameKit Timer: create timers and cooldown systems.

**Operations:**
- createTimer: Add GameKitTimer component
- updateTimer: Modify timer settings
- inspectTimer: View timer status
- deleteTimer: Remove timer
- startTimer: Start the timer
- stopTimer: Stop the timer
- pauseTimer: Pause (can resume)
- resumeTimer: Resume paused timer
- resetTimer: Reset to initial duration
- createCooldown: Add cooldown to CooldownManager
- inspectCooldown: Check cooldown status
- triggerCooldown: Start a cooldown
- resetCooldown: Reset cooldown
- deleteCooldown: Remove cooldown
- findByTimerId: Find timer by ID

**Timer Features:**
- Loop mode for repeating timers
- Unscaled time for pause-immune timers
- UnityEvents: OnTimerStart, OnTimerComplete, OnTimerTick

**Cooldown Features:**
- Multiple cooldowns on single GameObject
- Ready state checking
- Remaining time queries

**Example:**
```python
# Create a countdown timer
unity_gamekit_timer({
    "operation": "createTimer",
    "gameObjectPath": "GameManager",
    "timerId": "round_timer",
    "duration": 60.0,
    "loop": False,
    "autoStart": True
})

# Create attack cooldown
unity_gamekit_timer({
    "operation": "createCooldown",
    "gameObjectPath": "Player",
    "cooldownId": "attack_cooldown",
    "duration": 0.5
})
```""",
            inputSchema=gamekit_timer_schema,
        ),
        types.Tool(
            name="unity_gamekit_ai",
            description="""High-level GameKit AI: create AI behaviors for NPCs and enemies.

**Operations:**
- create: Add GameKitAIBehavior component
- update: Modify AI settings
- inspect: View AI status
- delete: Remove AI component
- setTarget: Set chase/flee target
- clearTarget: Clear current target
- setState: Force state change
- addPatrolPoint: Add patrol waypoint
- clearPatrolPoints: Clear all waypoints
- findByAIId: Find AI by ID

**Behavior Types:** Patrol, Chase, Flee, PatrolAndChase

**AI States:** Idle, Patrol, Chase, Attack, Flee, Return

**Patrol Modes:** Loop, PingPong, Random

**Detection:**
- Detection radius
- Field of view angle
- Line of sight (raycast)
- Target layer mask

**Example:**
```python
# Create patrol enemy
unity_gamekit_ai({
    "operation": "create",
    "gameObjectPath": "Enemy",
    "aiId": "enemy_ai",
    "behaviorType": "PatrolAndChase",
    "moveSpeed": 3.0,
    "detectionRadius": 8.0,
    "fieldOfView": 120,
    "patrolMode": "PingPong"
})

# Add patrol points
unity_gamekit_ai({
    "operation": "addPatrolPoint",
    "aiId": "enemy_ai",
    "position": {"x": 0, "y": 0, "z": 5}
})
```

**When to use:** For intelligent behaviors (patrol + chase, detection, state machines). For simple path following without AI (moving platforms, rails), use unity_gamekit_waypoint instead.""",
            inputSchema=gamekit_ai_schema,
        ),
        # Phase 2 GameKit Tools - Additional game mechanics
        types.Tool(
            name="unity_gamekit_collectible",
            description="""High-level GameKit Collectible: create collectible items for games.

**Operations:**
- create: Add GameKitCollectible component
- update: Modify collectible settings
- inspect: View collectible status
- delete: Remove collectible component
- collect: Simulate collection (editor mode)
- respawn: Respawn collected item
- reset: Reset to initial state
- findByCollectibleId: Find by ID

**Collectible Types:** Coin, Health, Mana, PowerUp, Key, Ammo, Experience, Custom

**Collection Behaviors:** Destroy, Disable, Respawn

**Features:**
- Auto-apply values to GameKitHealth/ResourceManager
- Float animation (bobbing)
- Rotation animation
- Customizable collider
- Tag/layer filtering

**Example:**
```python
# Create a coin collectible
unity_gamekit_collectible({
    "operation": "create",
    "name": "GoldCoin",
    "collectibleId": "coin_01",
    "collectibleType": "coin",
    "value": 10,
    "enableFloatAnimation": True,
    "enableRotation": True,
    "collectionBehavior": "destroy",
    "position": {"x": 0, "y": 1, "z": 0}
})

# Create a health pickup with respawn
unity_gamekit_collectible({
    "operation": "create",
    "name": "HealthPack",
    "collectibleType": "health",
    "value": 25,
    "collectionBehavior": "respawn",
    "respawnDelay": 10.0
})
```""",
            inputSchema=gamekit_collectible_schema,
        ),
        types.Tool(
            name="unity_gamekit_projectile",
            description="""High-level GameKit Projectile: create projectiles for games.

**Operations:**
- create: Add GameKitProjectile component
- update: Modify projectile settings
- inspect: View projectile status
- delete: Remove projectile component
- launch: Set launch direction (play mode)
- setHomingTarget: Set homing target
- destroy: Destroy projectile
- findByProjectileId: Find by ID

**Movement Types:** Transform, Rigidbody, Rigidbody2D

**Features:**
- Homing missiles (target tracking)
- Bouncing projectiles
- Piercing projectiles
- Gravity support
- Damage on hit (GameKitHealth integration)
- Target tag/layer filtering

**Example:**
```python
# Create a bullet projectile
unity_gamekit_projectile({
    "operation": "create",
    "name": "Bullet",
    "projectileId": "bullet_01",
    "movementType": "rigidbody",
    "speed": 20,
    "damage": 10,
    "lifetime": 5,
    "targetTag": "Enemy"
})

# Create a homing missile
unity_gamekit_projectile({
    "operation": "create",
    "name": "Missile",
    "movementType": "rigidbody",
    "speed": 15,
    "damage": 50,
    "isHoming": True,
    "homingStrength": 5.0,
    "maxHomingAngle": 60
})

# Create a bouncing ball
unity_gamekit_projectile({
    "operation": "create",
    "name": "BouncyBall",
    "canBounce": True,
    "maxBounces": 5,
    "bounciness": 0.8
})
```""",
            inputSchema=gamekit_projectile_schema,
        ),
        types.Tool(
            name="unity_gamekit_waypoint",
            description="""High-level GameKit Waypoint: create path followers for NPCs and platforms.

**Operations:**
- create: Add GameKitWaypoint component
- update: Modify waypoint settings
- inspect: View waypoint status
- delete: Remove waypoint component
- addWaypoint: Add waypoint position
- removeWaypoint: Remove waypoint at index
- clearWaypoints: Remove all waypoints
- startPath/stopPath/pausePath/resumePath: Path control
- resetPath: Reset to first waypoint
- goToWaypoint: Jump to waypoint index
- findByWaypointId: Find by ID

**Path Modes:** Once, Loop, PingPong

**Movement Types:** Transform, Rigidbody, Rigidbody2D

**Rotation Modes:** None, LookAtTarget, AlignToPath

**Features:**
- Wait time at points
- Smooth movement
- Editor gizmo visualization
- Per-point wait times

**Example:**
```python
# Create a patrol route
unity_gamekit_waypoint({
    "operation": "create",
    "name": "PatrolEnemy",
    "waypointId": "enemy_patrol",
    "pathMode": "pingpong",
    "moveSpeed": 3,
    "waitTimeAtPoint": 1.0,
    "waypointPositions": [
        {"x": 0, "y": 0, "z": 0},
        {"x": 5, "y": 0, "z": 0},
        {"x": 5, "y": 0, "z": 5},
        {"x": 0, "y": 0, "z": 5}
    ]
})

# Create a moving platform
unity_gamekit_waypoint({
    "operation": "create",
    "name": "MovingPlatform",
    "pathMode": "loop",
    "moveSpeed": 2,
    "smoothMovement": True,
    "waypointPositions": [
        {"x": 0, "y": 0, "z": 0},
        {"x": 0, "y": 5, "z": 0}
    ]
})
```

**When to use:** For simple path following (moving platforms, rails, fixed NPC routes). For AI behaviors with detection and chase, use unity_gamekit_ai instead.""",
            inputSchema=gamekit_waypoint_schema,
        ),
        types.Tool(
            name="unity_gamekit_trigger_zone",
            description="""High-level GameKit TriggerZone: create trigger zones for game mechanics.

**Operations:**
- create: Add GameKitTriggerZone component
- update: Modify zone settings
- inspect: View zone status
- delete: Remove zone component
- activate/deactivate: Enable/disable zone
- reset: Reset trigger state
- setTeleportDestination: Set teleport target
- findByZoneId: Find by ID

**Zone Types:**
- Generic: Custom trigger events
- Checkpoint: Save progress points
- DamageZone: Deal damage over time
- HealZone: Heal over time
- Teleport: Teleport to destination
- SpeedBoost: Increase movement speed
- SlowDown: Decrease movement speed
- KillZone: Instant death
- SafeZone: Prevent damage
- Trigger: One-time trigger events

**Trigger Modes:** Once, OncePerEntity, Repeat, WhileInside

**Features:**
- 2D/3D collider support
- Tag/layer filtering
- Cooldown system
- Effect intervals
- Editor gizmo visualization

**Example:**
```python
# Create a checkpoint
unity_gamekit_trigger_zone({
    "operation": "create",
    "name": "Checkpoint1",
    "zoneId": "checkpoint_1",
    "zoneType": "checkpoint",
    "checkpointIndex": 1,
    "colliderSize": {"x": 2, "y": 3, "z": 2}
})

# Create a damage zone (lava)
unity_gamekit_trigger_zone({
    "operation": "create",
    "name": "LavaZone",
    "zoneType": "damagezone",
    "triggerMode": "whileinside",
    "effectAmount": 10,
    "effectInterval": 0.5,
    "gizmoColor": {"r": 1, "g": 0.3, "b": 0, "a": 0.5}
})

# Create a teleporter
unity_gamekit_trigger_zone({
    "operation": "create",
    "name": "Teleporter",
    "zoneType": "teleport",
    "triggerMode": "once"
})
unity_gamekit_trigger_zone({
    "operation": "setTeleportDestination",
    "zoneId": "Teleporter",
    "destinationPosition": {"x": 10, "y": 0, "z": 20}
})
```

**When to use:** For built-in zone effects (damage, heal, checkpoint, teleport, speed zones). For custom script actions (sendMessage, spawnPrefab), use unity_gamekit_interaction instead.""",
            inputSchema=gamekit_trigger_zone_schema,
        ),
        # Phase 3 GameKit Tools - Animation & Effects
        types.Tool(
            name="unity_gamekit_animation_sync",
            description="""High-level GameKit Animation Sync: declarative animation synchronization with game state.

**Operations:**
- create: Add GameKitAnimationSync component with sync rules
- update: Modify sync configuration
- inspect: View sync rules and trigger rules
- delete: Remove GameKitAnimationSync component
- addSyncRule: Add a parameter sync rule
- removeSyncRule: Remove sync rule by parameter name
- addTriggerRule: Add a trigger rule
- removeTriggerRule: Remove trigger rule by name
- fireTrigger: Manually fire an animator trigger
- setParameter: Set an animator parameter value
- findBySyncId: Find animation sync by ID

**Sync Source Types:**
- rigidbody3d/rigidbody2d: Sync with velocity, angular velocity
- transform: Sync with position, rotation, scale
- health: Sync with GameKitHealth component
- custom: Manual value setting

**Trigger Event Sources:**
- health: Fire trigger on damage, heal, death, respawn, invincibility
- input: Fire trigger on input action
- manual: Fire via API call

**Example:**
```python
# Create animation sync with movement speed
unity_gamekit_animation_sync({
    "operation": "create",
    "targetPath": "Player",
    "syncId": "player_anim",
    "syncRules": [
        {
            "parameter": "Speed",
            "sourceType": "rigidbody3d",
            "sourceProperty": "velocity.magnitude",
            "multiplier": 1.0
        },
        {
            "parameter": "HealthPercent",
            "sourceType": "health",
            "healthId": "player_hp",
            "sourceProperty": "healthPercent"
        }
    ],
    "triggers": [
        {
            "triggerName": "Hit",
            "eventSource": "health",
            "healthId": "player_hp",
            "healthEvent": "OnDamaged"
        }
    ]
})

# Add a sync rule
unity_gamekit_animation_sync({
    "operation": "addSyncRule",
    "syncId": "player_anim",
    "rule": {
        "parameter": "IsGrounded",
        "parameterType": "bool",
        "sourceType": "custom",
        "boolThreshold": 0.5
    }
})
```""",
            inputSchema=gamekit_animation_sync_schema,
        ),
        types.Tool(
            name="unity_gamekit_effect",
            description="""High-level GameKit Effect: composite effect system for particles, sound, camera shake, and screen flash.

**Operations:**
- create: Create a new effect asset (ScriptableObject)
- update: Update effect asset properties
- inspect: View effect components
- delete: Delete effect asset
- addComponent: Add a component to effect
- removeComponent: Remove component by index
- clearComponents: Clear all components
- play: Play effect at position (runtime)
- playAtPosition: Same as play
- playAtTransform: Play effect attached to transform (runtime)
- shakeCamera: Direct camera shake (runtime)
- flashScreen: Direct screen flash (runtime)
- setTimeScale: Time scale effect for hit pause (runtime)
- createManager: Create GameKitEffectManager in scene
- registerEffect: Register effect with manager
- unregisterEffect: Unregister effect
- findByEffectId: Find effect asset by ID
- listEffects: List all registered effects

**Effect Component Types:**
- particle: Spawn particle prefab
- sound: Play audio clip
- cameraShake: Shake camera
- screenFlash: Flash screen overlay
- timeScale: Slow motion / hit pause

**Example:**
```python
# Create hit effect asset
unity_gamekit_effect({
    "operation": "create",
    "effectId": "hit_effect",
    "assetPath": "Assets/Effects/HitEffect.asset",
    "components": [
        {
            "type": "particle",
            "prefabPath": "Assets/Particles/HitSpark.prefab",
            "duration": 0.5
        },
        {
            "type": "sound",
            "clipPath": "Assets/Audio/hit.wav",
            "volume": 0.8,
            "pitchVariation": 0.1
        },
        {
            "type": "cameraShake",
            "intensity": 0.3,
            "shakeDuration": 0.2,
            "frequency": 25
        },
        {
            "type": "screenFlash",
            "color": {"r": 1, "g": 0, "b": 0, "a": 0.3},
            "flashDuration": 0.1
        }
    ]
})

# Create effect manager
unity_gamekit_effect({
    "operation": "createManager",
    "persistent": true
})

# Register effect with manager
unity_gamekit_effect({
    "operation": "registerEffect",
    "effectId": "hit_effect"
})

# Play effect (in play mode)
unity_gamekit_effect({
    "operation": "play",
    "effectId": "hit_effect",
    "position": {"x": 0, "y": 1, "z": 0}
})
```""",
            inputSchema=gamekit_effect_schema,
        ),
        # Phase 4: Persistence & Inventory
        types.Tool(
            name="unity_gamekit_save",
            description="""High-level GameKit Save: declarative save/load system with profiles and slots.

**Operations:**
- createProfile: Create save profile (ScriptableObject)
- updateProfile: Update profile settings
- inspectProfile: View profile targets and settings
- deleteProfile: Delete profile asset
- addTarget: Add a save target to profile
- removeTarget: Remove save target by key
- clearTargets: Clear all targets
- save: Execute save to slot
- load: Execute load from slot
- listSlots: List all save slots
- deleteSlot: Delete a save slot
- createManager: Create GameKitSaveManager component
- inspectManager: View manager state
- deleteManager: Delete manager component
- findByProfileId: Find profile by ID

**Save Target Types:**
- transform: Save position/rotation/scale
- component: Save custom component properties
- resourceManager: Save GameKitResourceManager state
- health: Save GameKitHealth state
- sceneFlow: Save GameKitSceneFlow state
- inventory: Save GameKitInventory state
- playerPrefs: Save to PlayerPrefs

**Example:**
```python
# Create save profile
unity_gamekit_save({
    "operation": "createProfile",
    "profileId": "main_save",
    "saveTargets": [
        {"type": "transform", "saveKey": "playerPos", "gameObjectPath": "Player"},
        {"type": "resourceManager", "saveKey": "resources", "resourceManagerId": "player_resources"},
        {"type": "inventory", "saveKey": "inventory", "inventoryId": "player_inventory"}
    ],
    "autoSave": {"enabled": true, "intervalSeconds": 300}
})

# Save to slot
unity_gamekit_save({
    "operation": "save",
    "slotId": "slot_1"
})

# Load from slot
unity_gamekit_save({
    "operation": "load",
    "slotId": "slot_1"
})
```""",
            inputSchema=gamekit_save_schema,
        ),
        types.Tool(
            name="unity_gamekit_inventory",
            description="""High-level GameKit Inventory: complete inventory system with items, stacking, and equipment.

**Operations:**
- create: Create inventory component on GameObject
- update: Update inventory settings
- inspect: View inventory slots and equipped items
- delete: Delete inventory component
- defineItem: Create item asset (ScriptableObject)
- updateItem: Update item asset properties
- inspectItem: View item details
- deleteItem: Delete item asset
- addItem: Add item to inventory
- removeItem: Remove item from inventory
- useItem: Use item at slot index
- equip: Equip item from slot
- unequip: Unequip item from equipment slot
- getEquipped: Get equipped item(s)
- clear: Clear all inventory items
- sort: Sort inventory by category
- findByInventoryId: Find inventory by ID
- findByItemId: Find item asset by ID

**Item Categories:**
- weapon, armor, consumable, material, key, quest, misc

**Equipment Slots:**
- mainHand, offHand, head, body, hands, feet, accessory1, accessory2

**Example:**
```python
# Create inventory
unity_gamekit_inventory({
    "operation": "create",
    "gameObjectPath": "Player",
    "inventoryId": "player_inventory",
    "maxSlots": 20,
    "categories": ["weapon", "armor", "consumable", "key"],
    "stackableCategories": ["consumable"],
    "maxStackSize": 99
})

# Define item asset
unity_gamekit_inventory({
    "operation": "defineItem",
    "itemId": "health_potion",
    "assetPath": "Assets/Items/HealthPotion.asset",
    "itemData": {
        "displayName": "Health Potion",
        "description": "Restores 50 HP",
        "category": "consumable",
        "stackable": true,
        "maxStack": 10,
        "onUse": {
            "type": "heal",
            "healthId": "player_hp",
            "amount": 50
        }
    }
})

# Add item to inventory
unity_gamekit_inventory({
    "operation": "addItem",
    "inventoryId": "player_inventory",
    "itemId": "health_potion",
    "quantity": 3
})

# Equip item
unity_gamekit_inventory({
    "operation": "equip",
    "inventoryId": "player_inventory",
    "slotIndex": 0,
    "equipSlot": "mainHand"
})
```""",
            inputSchema=gamekit_inventory_schema,
        ),
        # Phase 5 GameKit Tools - Story & Quest Systems
        types.Tool(
            name="unity_gamekit_dialogue",
            description="""High-level GameKit Dialogue: declarative dialogue system for NPC conversations with choices and conditions.

**Operations:**
- createDialogue: Create dialogue asset (ScriptableObject)
- updateDialogue: Update dialogue properties
- inspectDialogue: View dialogue structure
- deleteDialogue: Delete dialogue asset
- addNode: Add dialogue node
- updateNode: Update node properties
- removeNode: Remove node
- addChoice: Add choice to choice node
- updateChoice: Update choice properties
- removeChoice: Remove choice
- startDialogue: Start dialogue at runtime
- selectChoice: Select choice during dialogue
- advanceDialogue: Advance to next node
- endDialogue: End current dialogue
- createManager: Create DialogueManager component
- inspectManager: View manager state
- deleteManager: Delete manager component
- findByDialogueId: Find dialogue by ID

**Node Types:**
- dialogue: Standard dialogue with speaker and text
- choice: Multiple choice branch
- branch: Conditional branch
- action: Execute actions (add items, start quests, etc.)
- exit: End dialogue

**Condition Types:**
- quest, resource, inventory, variable, health, custom

**Example:**
```python
# Create dialogue asset
unity_gamekit_dialogue({
    "operation": "createDialogue",
    "dialogueId": "npc_greeting",
    "assetPath": "Assets/Dialogues/NPC_Greeting.asset",
    "displayName": "NPC Greeting"
})

# Add dialogue node
unity_gamekit_dialogue({
    "operation": "addNode",
    "dialogueId": "npc_greeting",
    "nodeData": {
        "nodeId": "start",
        "nodeType": "dialogue",
        "speakerName": "Old Man",
        "dialogueText": "Hello, traveler! Are you looking for work?",
        "nextNodeId": "choice_1"
    }
})

# Add choice node with conditions
unity_gamekit_dialogue({
    "operation": "addNode",
    "dialogueId": "npc_greeting",
    "nodeData": {
        "nodeId": "choice_1",
        "nodeType": "choice"
    }
})

unity_gamekit_dialogue({
    "operation": "addChoice",
    "dialogueId": "npc_greeting",
    "nodeId": "choice_1",
    "choiceData": {
        "choiceId": "accept",
        "choiceText": "Yes, I'm interested!",
        "targetNodeId": "quest_start"
    }
})
```""",
            inputSchema=gamekit_dialogue_schema,
        ),
        types.Tool(
            name="unity_gamekit_quest",
            description="""High-level GameKit Quest: complete quest system with objectives, prerequisites, and rewards.

**Operations:**
- createQuest: Create quest asset (ScriptableObject)
- updateQuest: Update quest properties
- inspectQuest: View quest details
- deleteQuest: Delete quest asset
- addObjective: Add quest objective
- updateObjective: Update objective properties
- removeObjective: Remove objective
- addPrerequisite: Add quest prerequisite
- removePrerequisite: Remove prerequisite
- addReward: Add quest reward
- removeReward: Remove reward
- startQuest: Start quest at runtime
- completeQuest: Complete quest
- failQuest: Fail quest
- abandonQuest: Abandon active quest
- updateProgress: Update objective progress
- listQuests: List quests by filter
- createManager: Create QuestManager component
- inspectManager: View manager state
- deleteManager: Delete manager component
- findByQuestId: Find quest by ID

**Quest Categories:**
- main, side, daily, weekly, event, tutorial, hidden, custom

**Objective Types:**
- kill, collect, talk, location, interact, escort, defend, deliver, explore, craft, custom

**Reward Types:**
- resource, item, experience, reputation, unlock, dialogue, custom

**Example:**
```python
# Create quest asset
unity_gamekit_quest({
    "operation": "createQuest",
    "questId": "main_quest_01",
    "assetPath": "Assets/Quests/MainQuest_01.asset",
    "displayName": "The Lost Artifact",
    "description": "Find the ancient artifact in the dungeon.",
    "category": "main"
})

# Add objective
unity_gamekit_quest({
    "operation": "addObjective",
    "questId": "main_quest_01",
    "objectiveData": {
        "objectiveId": "kill_goblins",
        "objectiveType": "kill",
        "description": "Defeat 5 goblins",
        "targetId": "goblin",
        "requiredAmount": 5
    }
})

# Add reward
unity_gamekit_quest({
    "operation": "addReward",
    "questId": "main_quest_01",
    "rewardData": {
        "rewardId": "gold_reward",
        "rewardType": "resource",
        "resourceManagerId": "player_resources",
        "resourceName": "gold",
        "amount": 100
    }
})

# Start quest at runtime
unity_gamekit_quest({
    "operation": "startQuest",
    "questId": "main_quest_01"
})

# Update progress
unity_gamekit_quest({
    "operation": "updateProgress",
    "questId": "main_quest_01",
    "objectiveId": "kill_goblins",
    "progressAmount": 1
})
```""",
            inputSchema=gamekit_quest_schema,
        ),
        types.Tool(
            name="unity_gamekit_status_effect",
            description="""High-level GameKit Status Effect: buff/debuff system with DoT, stat modifiers, and stacking.

**Operations:**
- defineEffect: Create effect asset (ScriptableObject)
- updateEffect: Update effect properties
- inspectEffect: View effect details
- deleteEffect: Delete effect asset
- addModifier: Add effect modifier
- updateModifier: Update modifier properties
- removeModifier: Remove modifier
- clearModifiers: Clear all modifiers
- create: Create StatusEffectReceiver component
- update: Update receiver properties
- inspect: View receiver state
- delete: Delete receiver component
- applyEffect: Apply effect to receiver
- removeEffect: Remove active effect
- clearEffects: Clear all active effects
- getActiveEffects: List active effects
- getStatModifier: Get cumulative stat modifier
- findByEffectId: Find effect asset by ID
- findByReceiverId: Find receiver by ID
- listEffects: List all effect assets

**Effect Types:**
- buff, debuff, neutral

**Effect Categories:**
- generic, poison, burn, freeze, stun, slow, haste, shield, regeneration, invincibility, weakness, strength, custom

**Modifier Types:**
- statModifier: Modify stat values
- damageOverTime: Deal periodic damage
- healOverTime: Heal periodically
- stun, silence, invincible: Status conditions

**Stack Behaviors:**
- refreshDuration, addDuration, independent, increaseStacks

**Example:**
```python
# Define poison effect
unity_gamekit_status_effect({
    "operation": "defineEffect",
    "effectId": "poison_weak",
    "assetPath": "Assets/Effects/Poison_Weak.asset",
    "effectData": {
        "displayName": "Weak Poison",
        "description": "Deals 5 damage per second for 10 seconds",
        "effectType": "debuff",
        "category": "poison",
        "duration": 10,
        "tickInterval": 1,
        "stackable": true,
        "maxStacks": 3,
        "stackBehavior": "increaseStacks"
    }
})

# Add DoT modifier
unity_gamekit_status_effect({
    "operation": "addModifier",
    "effectId": "poison_weak",
    "modifierData": {
        "modifierId": "poison_dot",
        "type": "damageOverTime",
        "targetHealthId": "target_hp",
        "damagePerTick": 5,
        "damageType": "poison",
        "scaleWithStacks": true
    }
})

# Create receiver on enemy
unity_gamekit_status_effect({
    "operation": "create",
    "gameObjectPath": "Enemy",
    "receiverId": "enemy_receiver"
})

# Apply effect at runtime
unity_gamekit_status_effect({
    "operation": "applyEffect",
    "receiverId": "enemy_receiver",
    "effectId": "poison_weak",
    "stacks": 1
})

# Get active effects
unity_gamekit_status_effect({
    "operation": "getActiveEffects",
    "receiverId": "enemy_receiver"
})
```""",
            inputSchema=gamekit_status_effect_schema,
        ),
        # ======================================================================
        # Development Cycle & Visual Tools (ROADMAP_MCP_TOOLS.md)
        # ======================================================================
        types.Tool(
            name="unity_playmode_control",
            description="""Control Unity Editor play mode for testing games.

**Operations:**
- play: Start play mode
- pause: Pause play mode
- unpause: Resume paused play mode
- stop: Stop play mode
- step: Step one frame (while paused)
- getState: Get current play mode state (stopped/playing/paused)

Essential for LLMs to execute and test games autonomously.""",
            inputSchema=playmode_control_schema,
        ),
        types.Tool(
            name="unity_console_log",
            description="""Retrieve Unity Console logs for debugging.

**Operations:**
- getRecent: Get recent N logs (default: 50)
- getErrors: Get error logs only
- getWarnings: Get warning logs only
- getLogs: Get normal Debug.Log messages only
- clear: Clear console
- getCompilationErrors: Get detailed compilation errors with file/line info
- getSummary: Get log count summary (errors/warnings/logs)

Essential for LLMs to debug and fix issues autonomously.""",
            inputSchema=console_log_schema,
        ),
        types.Tool(
            name="unity_material_bundle",
            description="""Create and configure materials with presets.

**Operations:**
- create: Create new material with optional preset
- update: Update material properties (color, metallic, smoothness)
- setTexture: Set texture with tiling/offset
- setColor: Set color property
- applyPreset: Apply material preset
- inspect: Get material properties
- applyToObjects: Apply material to multiple GameObjects
- delete: Delete material asset
- duplicate: Duplicate material
- listPresets: List available presets

**Presets:** unlit, lit, transparent, cutout, fade, sprite, ui, emissive, metallic, glass

Supports Standard, URP, and HDRP render pipelines.""",
            inputSchema=material_bundle_schema,
        ),
        types.Tool(
            name="unity_light_bundle",
            description="""Create and configure lights with presets.

**Operations:**
- create: Create light with type and preset
- update: Update light properties
- inspect: Get light properties
- delete: Delete light GameObject
- applyPreset: Apply light preset
- createLightingSetup: Create complete lighting setup
- listPresets: List available presets

**Light Presets:** daylight, moonlight, warm, cool, spotlight, candle, neon

**Setup Presets:** daylight (sun+ambient), nighttime (moon), indoor (points), dramatic (contrast), studio (3-point), sunset (warm)""",
            inputSchema=light_bundle_schema,
        ),
        types.Tool(
            name="unity_particle_bundle",
            description="""Create and configure particle systems with presets.

**Operations:**
- create: Create particle system with preset
- update: Update particle properties
- applyPreset: Apply particle preset
- play: Start particle playback
- stop: Stop particle playback
- pause: Pause particle playback
- inspect: Get particle system properties
- delete: Delete particle system GameObject
- duplicate: Duplicate particle system
- listPresets: List available presets

**Presets:** explosion, fire, smoke, sparkle, rain, snow, dust, trail, hit, heal, magic, leaves""",
            inputSchema=particle_bundle_schema,
        ),
        types.Tool(
            name="unity_animation3d_bundle",
            description="""Create and configure 3D character animations.

**Operations:**
- setupAnimator: Setup Animator component on GameObject
- createController: Create AnimatorController with parameters/states/transitions
- addState: Add animation state
- addTransition: Add state transition with conditions
- setParameter: Add/update animator parameter
- addBlendTree: Create BlendTree for smooth animation blending
- createAvatarMask: Create AvatarMask for partial body animation
- inspect: Get controller structure
- delete: Delete controller or state
- listParameters: List all parameters
- listStates: List all states in layer

**Example:**
```python
unity_animation3d_bundle({
    "operation": "createController",
    "name": "PlayerAnimator",
    "savePath": "Assets/Animations/PlayerAnimator.controller",
    "parameters": [
        {"name": "Speed", "type": "float"},
        {"name": "IsGrounded", "type": "bool"}
    ],
    "states": [
        {"name": "Idle", "clip": "Assets/Animations/Idle.anim", "isDefault": True},
        {"name": "Walk", "clip": "Assets/Animations/Walk.anim"}
    ],
    "transitions": [
        {"from": "Idle", "to": "Walk", "conditions": [{"param": "Speed", "mode": "greater", "value": 0.1}]}
    ]
})
```""",
            inputSchema=animation3d_bundle_schema,
        ),
        types.Tool(
            name="unity_event_wiring",
            description="""Wire UnityEvents dynamically (Button.onClick, Slider.onValueChanged, etc.).

**Operations:**
- wire: Add listener to UnityEvent
- unwire: Remove listener(s) from UnityEvent
- inspect: View event listeners
- listEvents: List UnityEvent fields on component
- clearEvent: Clear all listeners from event
- wireMultiple: Wire multiple events at once

**Argument Modes:** Void, Int, Float, String, Bool, Object

**Example:**
```python
# Wire button click to method
unity_event_wiring({
    "operation": "wire",
    "source": {
        "gameObject": "Canvas/StartButton",
        "component": "Button",
        "event": "onClick"
    },
    "target": {
        "gameObject": "GameManager",
        "method": "StartGame",
        "mode": "Void"
    }
})

# Wire slider with value argument
unity_event_wiring({
    "operation": "wire",
    "source": {
        "gameObject": "Canvas/VolumeSlider",
        "component": "Slider",
        "event": "onValueChanged"
    },
    "target": {
        "gameObject": "AudioManager",
        "method": "SetVolume",
        "mode": "Float"
    }
})
```

Supports: Button, Toggle, Slider, InputField, Dropdown, ScrollRect, and custom UnityEvents.""",
            inputSchema=event_wiring_schema,
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

        if name == "unity_compilation_await":
            # Async polling approach: wait for compilation to start, then wait for completion
            _ensure_bridge_connected()
            timeout_seconds = args.get("timeoutSeconds", 60)
            poll_interval = 0.5  # Poll every 500ms
            poll_timeout = 5.0  # Wait up to 5 seconds for compilation to start

            start_time = time.time()
            is_compiling = False
            unity_status: dict[str, Any] = {}

            # Phase 1: Poll until compilation starts or timeout
            logger.info(
                "Polling for compilation start (poll_timeout: %.1fs, total_timeout: %ds)...",
                poll_timeout,
                timeout_seconds,
            )

            while time.time() - start_time < poll_timeout:
                # Check local state first (faster)
                if bridge_manager.is_compiling():
                    is_compiling = True
                    logger.info("Compilation detected via local state")
                    break

                # Query Unity for actual compilation status
                try:
                    unity_status = await bridge_manager.send_command(
                        "compilationAwait", {"operation": "status"}
                    )
                    if unity_status.get("isCompiling", False):
                        is_compiling = True
                        logger.info("Compilation detected via Unity query")
                        break
                except Exception as exc:
                    # Bridge might be disconnected during compilation
                    logger.debug("Unity query failed (may be compiling): %s", exc)
                    # If bridge disconnected, assume compilation started
                    if not bridge_manager.is_connected():
                        is_compiling = True
                        logger.info("Bridge disconnected - assuming compilation started")
                        break

                # Wait before next poll
                await asyncio.sleep(poll_interval)

            poll_elapsed = time.time() - start_time

            if not is_compiling:
                # No compilation detected after polling - return current status
                logger.info("No compilation detected after %.1fs polling", poll_elapsed)
                if unity_status:
                    unity_status["wasCompiling"] = False
                    unity_status["compilationCompleted"] = True
                    unity_status["waitTimeSeconds"] = poll_elapsed
                    unity_status["message"] = "No compilation detected"
                    return [types.TextContent(type="text", text=as_pretty_json(unity_status))]
                result = {
                    "wasCompiling": False,
                    "compilationCompleted": True,
                    "waitTimeSeconds": poll_elapsed,
                    "success": True,
                    "errorCount": 0,
                    "message": "No compilation detected",
                }
                return [types.TextContent(type="text", text=as_pretty_json(result))]

            # Phase 2: Compilation in progress - wait for completion asynchronously
            remaining_timeout = max(1, timeout_seconds - int(poll_elapsed))
            logger.info(
                "Compilation in progress - waiting for completion (timeout: %ds)...",
                remaining_timeout,
            )

            try:
                compilation_result = await bridge_manager.await_compilation(remaining_timeout)
                total_elapsed = time.time() - start_time
                # Add wasCompiling flag for consistency with original API
                compilation_result["wasCompiling"] = True
                compilation_result["compilationCompleted"] = compilation_result.get("completed", True)
                compilation_result["waitTimeSeconds"] = total_elapsed
                return [types.TextContent(type="text", text=as_pretty_json(compilation_result))]
            except TimeoutError as exc:
                total_elapsed = time.time() - start_time
                result = {
                    "wasCompiling": True,
                    "compilationCompleted": False,
                    "waitTimeSeconds": total_elapsed,
                    "success": False,
                    "errorCount": 0,
                    "timedOut": True,
                    "message": str(exc),
                }
                return [types.TextContent(type="text", text=as_pretty_json(result))]

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

        # Phase 1 GameKit Tools - Common game mechanics
        if name == "unity_gamekit_health":
            return await _call_bridge_tool("gamekitHealth", args)

        if name == "unity_gamekit_spawner":
            return await _call_bridge_tool("gamekitSpawner", args)

        if name == "unity_gamekit_timer":
            return await _call_bridge_tool("gamekitTimer", args)

        if name == "unity_gamekit_ai":
            return await _call_bridge_tool("gamekitAI", args)

        # Phase 2 GameKit Tools - Additional game mechanics
        if name == "unity_gamekit_collectible":
            return await _call_bridge_tool("gamekitCollectible", args)

        if name == "unity_gamekit_projectile":
            return await _call_bridge_tool("gamekitProjectile", args)

        if name == "unity_gamekit_waypoint":
            return await _call_bridge_tool("gamekitWaypoint", args)

        if name == "unity_gamekit_trigger_zone":
            return await _call_bridge_tool("gamekitTriggerZone", args)

        # Phase 3 GameKit Tools - Animation & Effects
        if name == "unity_gamekit_animation_sync":
            return await _call_bridge_tool("gamekitAnimationSync", args)

        if name == "unity_gamekit_effect":
            return await _call_bridge_tool("gamekitEffect", args)

        # Phase 4 GameKit Tools - Persistence & Inventory
        if name == "unity_gamekit_save":
            return await _call_bridge_tool("gamekitSave", args)

        if name == "unity_gamekit_inventory":
            return await _call_bridge_tool("gamekitInventory", args)

        # Phase 5 GameKit Tools - Story & Quest Systems
        if name == "unity_gamekit_dialogue":
            return await _call_bridge_tool("gamekitDialogue", args)

        if name == "unity_gamekit_quest":
            return await _call_bridge_tool("gamekitQuest", args)

        if name == "unity_gamekit_status_effect":
            return await _call_bridge_tool("gamekitStatusEffect", args)

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

        # Development Cycle & Visual Tools (ROADMAP_MCP_TOOLS.md)
        if name == "unity_playmode_control":
            return await _call_bridge_tool("playModeControl", args)

        if name == "unity_console_log":
            return await _call_bridge_tool("consoleLog", args)

        if name == "unity_material_bundle":
            return await _call_bridge_tool("materialBundle", args)

        if name == "unity_light_bundle":
            return await _call_bridge_tool("lightBundle", args)

        if name == "unity_particle_bundle":
            return await _call_bridge_tool("particleBundle", args)

        if name == "unity_animation3d_bundle":
            return await _call_bridge_tool("animation3DBundle", args)

        if name == "unity_event_wiring":
            return await _call_bridge_tool("eventWiring", args)

        if name == "unity_batch_sequential_execute":
            # Special handling for batch sequential tool (doesn't use bridge directly)
            return await handle_batch_sequential(args, bridge_manager)

        raise RuntimeError(f"No handler registered for tool '{name}'.")

