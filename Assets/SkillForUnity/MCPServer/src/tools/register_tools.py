from __future__ import annotations

from typing import Any

import mcp.types as types
from mcp.server import Server

from bridge.bridge_manager import bridge_manager
from logger import logger
from services.editor_log_watcher import editor_log_watcher
from utils.json_utils import as_pretty_json


def _ensure_bridge_connected() -> None:
    if not bridge_manager.is_connected():
        raise RuntimeError(
            "Unity bridge is not connected. In the Unity Editor choose Tools/MCP Assistant to start the bridge."
        )


async def _call_bridge_tool(tool_name: str, payload: dict[str, Any]) -> list[types.Content]:
    _ensure_bridge_connected()

    # Unity側のタイムアウトに20秒のバッファを追加してPython側のタイムアウトを設定
    # これにより、Unity側でコンパイル完了を待つ時間が確保される
    # デフォルトを30秒から45秒に増加（大規模プロジェクトに対応）
    timeout_ms = 45_000  # デフォルト45秒（30秒から増加）
    if "timeoutSeconds" in payload:
        unity_timeout = payload["timeoutSeconds"]
        timeout_ms = (unity_timeout + 20) * 1000  # バッファを15秒から20秒に増加

    try:
        response = await bridge_manager.send_command(tool_name, payload, timeout_ms=timeout_ms)
    except Exception as exc:
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
                    "enum": ["create", "load", "save", "delete", "duplicate", "inspect", "listBuildSettings", "addToBuildSettings", "removeFromBuildSettings", "reorderBuildSettings", "setBuildSettingsEnabled"],
                    "description": "Operation to perform. Scene operations: create, load, save, delete, duplicate, inspect. Build settings operations: listBuildSettings (get all scenes in build), addToBuildSettings (add scene to build), removeFromBuildSettings (remove scene from build), reorderBuildSettings (change scene order), setBuildSettingsEnabled (enable/disable scene in build).",
                },
                "scenePath": {
                    "type": "string",
                    "description": "Path under Assets/, e.g. Assets/Scenes/Main.unity. Required for most operations including build settings operations.",
                },
                "newSceneName": {
                    "type": "string",
                    "description": "New name for duplicate operation."
                },
                "additive": {
                    "type": "boolean",
                    "description": "For load/create: open scene additively without closing current scenes."
                },
                "includeOpenScenes": {
                    "type": "boolean",
                    "description": "For save: save all open scenes instead of just the active one."
                },
                "includeHierarchy": {
                    "type": "boolean",
                    "description": "For inspect: Include scene hierarchy (one level only). Default is true. Returns root GameObjects with their direct child names.",
                },
                "includeComponents": {
                    "type": "boolean",
                    "description": "For inspect: Include component types for each GameObject. Default is false.",
                },
                "filter": {
                    "type": "string",
                    "description": "For inspect: Filter GameObjects by name pattern (supports wildcards * and ?).",
                },
                "enabled": {
                    "type": "boolean",
                    "description": "For addToBuildSettings/setBuildSettingsEnabled: whether the scene should be enabled in build settings. Default: true."
                },
                "index": {
                    "type": "integer",
                    "description": "For addToBuildSettings: position to insert scene (0-based). For removeFromBuildSettings/setBuildSettingsEnabled: index of scene to modify. For reorderBuildSettings: source index (fromIndex) or target index (toIndex)."
                },
                "fromIndex": {
                    "type": "integer",
                    "description": "For reorderBuildSettings: source index to move from. Alternative to scenePath."
                },
                "toIndex": {
                    "type": "integer",
                    "description": "For reorderBuildSettings: target index to move to. Required."
                },
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
                    "enum": ["create", "delete", "move", "rename", "update", "duplicate", "inspect", "findMultiple", "deleteMultiple", "inspectMultiple"],
                    "description": "Operation to perform. Use 'inspect' to read GameObject details including all attached components. Use 'update' to change GameObject properties (tag, layer, active state, static flag). Use 'findMultiple', 'deleteMultiple', or 'inspectMultiple' with 'pattern' to perform operations on multiple GameObjects matching a wildcard or regex pattern.",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "Hierarchy path of the GameObject (e.g. Root/Child/Button). Not required for multiple operations (use 'pattern' instead).",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Target parent path for move or create operations.",
                },
                "template": {
                    "type": "string",
                    "description": "Prefab path or template identifier to instantiate.",
                },
                "name": {
                    "type": "string",
                    "description": "Name for the new or renamed GameObject.",
                },
                "tag": {
                    "type": "string",
                    "description": "For update operation: tag to assign to the GameObject (e.g. 'Player', 'Enemy', 'Untagged').",
                },
                "layer": {
                    "oneOf": [
                        {"type": "integer"},
                        {"type": "string"},
                    ],
                    "description": "For update operation: layer to assign to the GameObject. Can be either layer name (string, e.g. 'Default', 'UI') or layer index (integer 0-31).",
                },
                "active": {
                    "type": "boolean",
                    "description": "For update operation: activate (true) or deactivate (false) the GameObject.",
                },
                "static": {
                    "type": "boolean",
                    "description": "For update operation: mark GameObject as static (true) or non-static (false). Static objects are optimized for rendering but cannot move at runtime.",
                },
                "pattern": {
                    "type": "string",
                    "description": "Wildcard pattern (e.g. 'Enemy*', 'Player?') or regex pattern for multiple operations. Supports * (any characters) and ? (single character).",
                },
                "useRegex": {
                    "type": "boolean",
                    "description": "If true, treats 'pattern' as a regular expression instead of wildcard pattern. Default is false.",
                },
                "includeComponents": {
                    "type": "boolean",
                    "description": "For inspectMultiple operation: if true, includes component type names in results. Default is false.",
                },
                "maxResults": {
                    "type": "integer",
                    "description": "For multiple operations (findMultiple, deleteMultiple, inspectMultiple): maximum number of GameObjects to process. Default is 1000. Use this to prevent timeouts when working with large numbers of objects.",
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
                    "enum": ["add", "remove", "update", "inspect", "addMultiple", "removeMultiple", "updateMultiple", "inspectMultiple"],
                    "description": "Operation to perform. Use 'inspect' to read component state. Use 'addMultiple', 'removeMultiple', 'updateMultiple', or 'inspectMultiple' with 'pattern' to perform operations on multiple GameObjects matching a wildcard or regex pattern.",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "Hierarchy path of the GameObject. Not required for multiple operations (use 'pattern' instead).",
                },
                "gameObjectGlobalObjectId": {
                    "type": "string",
                    "description": "Optional GlobalObjectId string to uniquely identify the GameObject (e.g., 'GlobalObjectId_V1-1-abc123-456-0'). If provided, this takes priority over gameObjectPath. Use this for precise GameObject identification across scene reloads.",
                },
                "componentType": {
                    "type": "string",
                    "description": "Fully qualified component type (e.g. UnityEngine.UI.Text).",
                },
                "propertyChanges": {
                    "type": "object",
                    "additionalProperties": True,
                    "description": "Property/value pairs to apply to the component. For UnityEngine.Object properties (e.g. Mesh, Material), you can use: 1) Asset reference with GUID (recommended): {'_ref': 'asset', 'guid': 'abc123...'}, 2) Asset reference with path: {'_ref': 'asset', 'path': 'Assets/Models/Sphere.fbx'}, 3) Direct asset path string: 'Assets/Models/Sphere.fbx', or 4) Built-in resources: 'Library/unity default resources::Sphere'. When both GUID and path are provided, GUID takes priority.",
                },
                "applyDefaults": {"type": "boolean"},
                "pattern": {
                    "type": "string",
                    "description": "Wildcard pattern (e.g. 'Enemy*', 'Player?') or regex pattern for multiple operations. Supports * (any characters) and ? (single character).",
                },
                "useRegex": {
                    "type": "boolean",
                    "description": "If true, treats 'pattern' as a regular expression instead of wildcard pattern. Default is false.",
                },
                "includeProperties": {
                    "type": "boolean",
                    "description": "For inspect/inspectMultiple operations: if false, only returns component type without properties. Default is true. Use this to improve performance when you only need to check if a component exists.",
                },
                "propertyFilter": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "For inspect/inspectMultiple operations: optional list of property/field names to inspect (e.g. ['position', 'rotation', 'enabled']). If specified, only these properties will be inspected. Use this to improve performance when you only need specific properties.",
                },
                "maxResults": {
                    "type": "integer",
                    "description": "For multiple operations (addMultiple, removeMultiple, updateMultiple, inspectMultiple): maximum number of GameObjects to process. Default is 1000. Use this to prevent timeouts when working with large numbers of objects.",
                },
                "stopOnError": {
                    "type": "boolean",
                    "description": "For multiple operations: if true, stops execution on first error. If false (default), continues processing remaining items and returns both successes and errors.",
                },
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
                    "enum": ["create", "update", "updateImporter", "delete", "rename", "duplicate", "inspect", "findMultiple", "deleteMultiple", "inspectMultiple"],
                    "description": "Operation to perform. 'create' creates a new asset file (JSON, XML, TXT, config, etc.). 'update' updates existing asset content. NOTE: C# script files (.cs) cannot be created/updated - use code editor tools or designPatternGenerate/scriptTemplateGenerate instead. 'updateImporter' modifies asset importer settings only. Use 'inspect' to read asset details. Use 'findMultiple', 'deleteMultiple', or 'inspectMultiple' with 'pattern' for bulk operations.",
                },
                "assetPath": {
                    "type": "string",
                    "description": "Path under Assets/ for the target asset (e.g., 'Assets/Config/settings.json', 'Assets/Data/items.xml'). Required for create, update, rename, duplicate, inspect, and updateImporter operations.",
                },
                "assetGuid": {
                    "type": "string",
                    "description": "Optional GUID string to uniquely identify the asset (e.g., 'abc123def456789'). If provided, this takes priority over assetPath. Use this for precise asset identification.",
                },
                "content": {
                    "type": "string",
                    "description": "Text content for the asset file. Required for 'create' and 'update' operations. Use this for JSON, XML, TXT, and other text-based files. C# scripts (.cs) are NOT supported - use code editor tools instead.",
                },
                "destinationPath": {
                    "type": "string",
                    "description": "Target path for rename or duplicate operations.",
                },
                "propertyChanges": {
                    "type": "object",
                    "additionalProperties": True,
                    "description": "Property/value pairs to apply to the asset's importer settings (for updateImporter operation). For example, TextureImporter properties (textureType, isReadable, filterMode), ModelImporter properties (importNormals, importTangents), etc. Changes are applied via AssetImporter reflection.",
                },
                "pattern": {
                    "type": "string",
                    "description": "Wildcard pattern (e.g. 'Assets/Data/*.json', 'Assets/Config/settings*') or regex pattern for multiple operations. Supports * (any characters) and ? (single character).",
                },
                "useRegex": {
                    "type": "boolean",
                    "description": "If true, treats 'pattern' as a regular expression instead of wildcard pattern. Default is false.",
                },
                "includeProperties": {
                    "type": "boolean",
                    "description": "For inspect/inspectMultiple operations: if true, includes detailed asset importer properties in results. Default is false.",
                },
                "maxResults": {
                    "type": "integer",
                    "description": "For multiple operations (findMultiple, deleteMultiple, inspectMultiple): maximum number of assets to process. Default: 1000. Use this to prevent timeouts when working with large numbers of assets.",
                },
            },
        },
        ["operation"],
    )

    ugui_rect_adjust_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "gameObjectPath": {
                    "type": "string",
                    "description": "Target GameObject path containing the RectTransform.",
                },
                "referenceResolution": {
                    "type": "array",
                    "items": {"type": "number"},
                    "minItems": 2,
                    "maxItems": 2,
                    "description": "CanvasScaler reference resolution (width, height).",
                },
                "matchMode": {
                    "type": "string",
                    "enum": ["widthOrHeight", "expand", "shrink"],
                },
            },
        },
        ["gameObjectPath"],
    )

    ugui_anchor_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "gameObjectPath": {
                    "type": "string",
                    "description": "Target GameObject path containing the RectTransform.",
                },
                "operation": {
                    "type": "string",
                    "enum": ["setAnchor", "setAnchorPreset", "convertToAnchored", "convertToAbsolute"],
                    "description": "Operation type: setAnchor (custom values), setAnchorPreset (common presets), convertToAnchored (absolute to anchored), convertToAbsolute (anchored to absolute).",
                },
                "anchorMinX": {
                    "type": "number",
                    "description": "Anchor min X (0-1 range). Used with setAnchor operation.",
                },
                "anchorMinY": {
                    "type": "number",
                    "description": "Anchor min Y (0-1 range). Used with setAnchor operation.",
                },
                "anchorMaxX": {
                    "type": "number",
                    "description": "Anchor max X (0-1 range). Used with setAnchor operation.",
                },
                "anchorMaxY": {
                    "type": "number",
                    "description": "Anchor max Y (0-1 range). Used with setAnchor operation.",
                },
                "preset": {
                    "type": "string",
                    "enum": [
                        "top-left", "top-center", "top-right",
                        "middle-left", "middle-center", "middle-right", "center",
                        "bottom-left", "bottom-center", "bottom-right",
                        "stretch-horizontal", "stretch-vertical", "stretch-all", "stretch",
                        "stretch-top", "stretch-middle", "stretch-bottom",
                        "stretch-left", "stretch-center-vertical", "stretch-right"
                    ],
                    "description": "Anchor preset name. Used with setAnchorPreset operation.",
                },
                "preservePosition": {
                    "type": "boolean",
                    "description": "Whether to preserve visual position when changing anchors. Default is true.",
                },
                "absoluteX": {
                    "type": "number",
                    "description": "Absolute X position in parent space. Used with convertToAnchored operation.",
                },
                "absoluteY": {
                    "type": "number",
                    "description": "Absolute Y position in parent space. Used with convertToAnchored operation.",
                },
            },
        },
        ["gameObjectPath", "operation"],
    )

    prefab_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "inspect", "instantiate", "unpack", "applyOverrides", "revertOverrides"],
                    "description": "Operation to perform. 'create' creates a new prefab from a GameObject, 'update' updates an existing prefab, 'inspect' reads prefab details, 'instantiate' creates an instance in the scene, 'unpack' unpacks a prefab instance, 'applyOverrides' applies instance modifications to the prefab asset, 'revertOverrides' reverts instance to prefab state.",
                },
                "prefabPath": {
                    "type": "string",
                    "description": "Asset path to the prefab (e.g. Assets/Prefabs/MyPrefab.prefab).",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "Hierarchy path of the GameObject (for create/instantiate/unpack operations).",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Target parent path for instantiate operations.",
                },
                "replacePrefabOptions": {
                    "type": "string",
                    "enum": ["Default", "ConnectToPrefab", "ReplaceNameBased"],
                    "description": "Options for updating prefab (update operation).",
                },
                "unpackMode": {
                    "type": "string",
                    "enum": ["Completely", "OutermostRoot"],
                    "description": "Unpack mode: 'Completely' unpacks entire hierarchy, 'OutermostRoot' unpacks only the root.",
                },
                "includeChildren": {
                    "type": "boolean",
                    "description": "Whether to include child GameObjects when creating prefab.",
                },
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
                    "enum": ["read", "write", "list"],
                    "description": "Operation to perform. 'read' reads settings values, 'write' writes settings values, 'list' lists available categories or properties.",
                },
                "category": {
                    "type": "string",
                    "enum": ["player", "quality", "time", "physics", "audio", "editor"],
                    "description": "Settings category: 'player' for PlayerSettings, 'quality' for QualitySettings, 'time' for Time settings, 'physics' for Physics settings, 'audio' for AudioSettings, 'editor' for EditorSettings.",
                },
                "property": {
                    "type": "string",
                    "description": "Specific property name to read or write. If omitted in read operation, returns all properties for the category.",
                },
                "value": {
                    "description": "Value to write for the property. Required for write operation. Type depends on the property (string, number, boolean, or object for complex values like vectors).",
                },
            },
        },
        ["operation"],
    )

    render_pipeline_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["inspect", "setAsset", "getSettings", "updateSettings"],
                    "description": "Operation to perform. 'inspect' checks current pipeline, 'setAsset' changes the pipeline asset, 'getSettings' reads pipeline settings, 'updateSettings' modifies pipeline settings.",
                },
                "assetPath": {
                    "type": "string",
                    "description": "Path to RenderPipelineAsset (for setAsset operation). Empty string to clear and use Built-in.",
                },
                "settings": {
                    "type": "object",
                    "additionalProperties": True,
                    "description": "Dictionary of settings to update (for updateSettings operation).",
                },
            },
        },
        ["operation"],
    )

    tag_layer_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["setTag", "getTag", "setLayer", "getLayer", "setLayerRecursive", "listTags", "addTag", "removeTag", "listLayers", "addLayer", "removeLayer"],
                    "description": "Operation to perform. GameObject operations: 'setTag' sets tag on GameObject, 'getTag' gets tag from GameObject, 'setLayer' sets layer on GameObject, 'getLayer' gets layer from GameObject, 'setLayerRecursive' sets layer on GameObject and all children. Project operations: 'listTags' lists all tags, 'addTag' creates new tag, 'removeTag' deletes tag, 'listLayers' lists all layers, 'addLayer' creates new layer, 'removeLayer' deletes layer.",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "Hierarchy path of the GameObject (for setTag, getTag, setLayer, getLayer, setLayerRecursive operations).",
                },
                "tag": {
                    "type": "string",
                    "description": "Tag name (for setTag, addTag, removeTag operations).",
                },
                "layer": {
                    "description": "Layer name (string) or layer index (integer) for setLayer, setLayerRecursive operations. Layer name (string) for addLayer, removeLayer operations.",
                },
            },
        },
        ["operation"],
    )

    constant_convert_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["enumToValue", "valueToEnum", "colorToRGBA", "rgbaToColor", "layerToIndex", "indexToLayer", "listEnums", "listColors", "listLayers"],
                    "description": "Operation to perform. 'enumToValue' converts enum name to numeric value, 'valueToEnum' converts numeric value to enum name, 'colorToRGBA' converts Unity color name to RGBA values, 'rgbaToColor' converts RGBA values to nearest color name, 'layerToIndex' converts layer name to index, 'indexToLayer' converts layer index to name, 'listEnums' lists all enum values, 'listColors' lists all Unity built-in colors, 'listLayers' lists all layers.",
                },
                "enumType": {
                    "type": "string",
                    "description": "Fully qualified enum type name (e.g., 'UnityEngine.KeyCode', 'UnityEngine.FontStyle'). Required for enumToValue, valueToEnum, and listEnums operations.",
                },
                "enumValue": {
                    "type": "string",
                    "description": "Enum value name (e.g., 'Space', 'Bold'). Required for enumToValue operation.",
                },
                "numericValue": {
                    "type": "integer",
                    "description": "Numeric value. Required for valueToEnum operation.",
                },
                "colorName": {
                    "type": "string",
                    "description": "Unity built-in color name (e.g., 'red', 'green', 'blue'). Required for colorToRGBA operation.",
                },
                "r": {
                    "type": "number",
                    "description": "Red component (0-1). Required for rgbaToColor operation.",
                },
                "g": {
                    "type": "number",
                    "description": "Green component (0-1). Required for rgbaToColor operation.",
                },
                "b": {
                    "type": "number",
                    "description": "Blue component (0-1). Required for rgbaToColor operation.",
                },
                "a": {
                    "type": "number",
                    "description": "Alpha component (0-1). Optional for rgbaToColor operation, defaults to 1.0.",
                },
                "layerName": {
                    "type": "string",
                    "description": "Layer name (e.g., 'Default', 'UI'). Required for layerToIndex operation.",
                },
                "layerIndex": {
                    "type": "integer",
                    "description": "Layer index (0-31). Required for indexToLayer operation.",
                },
            },
        },
        ["operation"],
    )


    await_compilation_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "timeoutSeconds": {
                    "type": "integer",
                    "description": "Maximum time to wait for compilation to complete in seconds. Default is 60 (increased from 30 to accommodate large projects).",
                },
            },
        },
        [],
    )

    scene_quick_setup_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "setupType": {
                    "type": "string",
                    "enum": ["3D", "2D", "UI", "VR", "Empty"],
                    "description": "Type of scene setup: '3D' (Main Camera + Directional Light), '2D' (2D Camera + 2D settings), 'UI' (Canvas + EventSystem), 'VR' (VR Camera + XR settings), 'Empty' (no default objects).",
                },
                "includeEventSystem": {
                    "type": "boolean",
                    "description": "Whether to include EventSystem (for UI). Default is true for UI setup, false otherwise.",
                },
                "cameraPosition": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Camera position. Default varies by setup type.",
                },
                "cameraRotation": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Camera rotation (Euler angles). Default varies by setup type.",
                },
                "lightIntensity": {
                    "type": "number",
                    "description": "Directional Light intensity (for 3D setup). Default is 1.0.",
                },
            },
        },
        ["setupType"],
    )

    gameobject_template_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "template": {
                    "type": "string",
                    "enum": ["Camera", "Light-Directional", "Light-Point", "Light-Spot", "Cube", "Sphere", "Plane", "Cylinder", "Capsule", "Quad", "Empty", "Player", "Enemy", "Particle System", "Audio Source"],
                    "description": "GameObject template to create. Each template includes appropriate components and settings.",
                },
                "name": {
                    "type": "string",
                    "description": "Name for the GameObject. If not specified, uses template name.",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path. If not specified, creates at root level.",
                },
                "position": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Position in world space. Default is (0, 0, 0).",
                },
                "rotation": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Rotation (Euler angles). Default is (0, 0, 0).",
                },
                "scale": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Scale. Default is (1, 1, 1).",
                },
            },
        },
        ["template"],
    )

    ugui_template_create_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "template": {
                    "type": "string",
                    "enum": ["Button", "Text", "Image", "RawImage", "Panel", "ScrollView", "InputField", "Slider", "Toggle", "Dropdown"],
                    "description": "UI element template to create. Each template includes necessary components and default settings.",
                },
                "name": {
                    "type": "string",
                    "description": "Name for the new GameObject. If not specified, uses template name as default.",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Hierarchy path of the parent GameObject. Must be under a Canvas. If not specified, creates under first Canvas found.",
                },
                "anchorPreset": {
                    "type": "string",
                    "enum": [
                        "top-left", "top-center", "top-right",
                        "middle-left", "middle-center", "middle-right", "center",
                        "bottom-left", "bottom-center", "bottom-right",
                        "stretch-horizontal", "stretch-vertical", "stretch-all", "stretch"
                    ],
                    "description": "Anchor preset to apply. Default is 'center'.",
                },
                "width": {
                    "type": "number",
                    "minimum": 0,
                    "description": "Width of the UI element. Must be non-negative. Default varies by template.",
                },
                "height": {
                    "type": "number",
                    "minimum": 0,
                    "description": "Height of the UI element. Must be non-negative. Default varies by template.",
                },
                "positionX": {
                    "type": "number",
                    "description": "Anchored position X. Default is 0.",
                },
                "positionY": {
                    "type": "number",
                    "description": "Anchored position Y. Default is 0.",
                },
                "text": {
                    "type": "string",
                    "description": "Text content for Button, Text, InputField, Toggle, or Dropdown templates.",
                },
                "fontSize": {
                    "type": "integer",
                    "description": "Font size for text elements. Default varies by template.",
                },
                "interactable": {
                    "type": "boolean",
                    "description": "Whether the element is interactable (for Button, InputField, Slider, Toggle, Dropdown). Default is true.",
                },
                "useTextMeshPro": {
                    "type": "boolean",
                    "description": "Use TextMeshPro (TMP) components instead of standard UI.Text for text elements. Requires TextMeshPro package to be installed. Default is false.",
                },
            },
        },
        ["template"],
    )

    ugui_layout_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "gameObjectPath": {
                    "type": "string",
                    "description": "Target GameObject path to add/update/remove layout component.",
                },
                "operation": {
                    "type": "string",
                    "enum": ["add", "update", "remove", "inspect"],
                    "description": "Operation type: 'add' adds layout component, 'update' modifies existing component, 'remove' removes component, 'inspect' retrieves current layout settings.",
                },
                "layoutType": {
                    "type": "string",
                    "enum": ["HorizontalLayoutGroup", "VerticalLayoutGroup", "GridLayoutGroup", "ContentSizeFitter", "LayoutElement", "AspectRatioFitter"],
                    "description": "Layout component type to add/update/remove.",
                },
                # Common layout group properties
                "padding": {
                    "type": "object",
                    "properties": {
                        "left": {"type": "integer"},
                        "right": {"type": "integer"},
                        "top": {"type": "integer"},
                        "bottom": {"type": "integer"},
                    },
                    "description": "Padding for layout groups (left, right, top, bottom).",
                },
                "spacing": {
                    "type": "number",
                    "description": "Spacing between elements for Horizontal/VerticalLayoutGroup, or x-spacing for GridLayoutGroup.",
                },
                "spacingY": {
                    "type": "number",
                    "description": "Y-spacing between elements for GridLayoutGroup.",
                },
                "childAlignment": {
                    "type": "string",
                    "enum": ["UpperLeft", "UpperCenter", "UpperRight", "MiddleLeft", "MiddleCenter", "MiddleRight", "LowerLeft", "LowerCenter", "LowerRight"],
                    "description": "Child alignment for layout groups.",
                },
                "childControlWidth": {
                    "type": "boolean",
                    "description": "Whether the layout group controls child width.",
                },
                "childControlHeight": {
                    "type": "boolean",
                    "description": "Whether the layout group controls child height.",
                },
                "childForceExpandWidth": {
                    "type": "boolean",
                    "description": "Whether children should be forced to expand width.",
                },
                "childForceExpandHeight": {
                    "type": "boolean",
                    "description": "Whether children should be forced to expand height.",
                },
                # GridLayoutGroup specific
                "cellSizeX": {
                    "type": "number",
                    "minimum": 0,
                    "description": "Cell width for GridLayoutGroup. Must be non-negative.",
                },
                "cellSizeY": {
                    "type": "number",
                    "minimum": 0,
                    "description": "Cell height for GridLayoutGroup. Must be non-negative.",
                },
                "constraint": {
                    "type": "string",
                    "enum": ["Flexible", "FixedColumnCount", "FixedRowCount"],
                    "description": "Constraint mode for GridLayoutGroup.",
                },
                "constraintCount": {
                    "type": "integer",
                    "description": "Number of columns/rows for GridLayoutGroup constraint.",
                },
                "startCorner": {
                    "type": "string",
                    "enum": ["UpperLeft", "UpperRight", "LowerLeft", "LowerRight"],
                    "description": "Start corner for GridLayoutGroup.",
                },
                "startAxis": {
                    "type": "string",
                    "enum": ["Horizontal", "Vertical"],
                    "description": "Start axis for GridLayoutGroup.",
                },
                # ContentSizeFitter specific
                "horizontalFit": {
                    "type": "string",
                    "enum": ["Unconstrained", "MinSize", "PreferredSize"],
                    "description": "Horizontal fit mode for ContentSizeFitter.",
                },
                "verticalFit": {
                    "type": "string",
                    "enum": ["Unconstrained", "MinSize", "PreferredSize"],
                    "description": "Vertical fit mode for ContentSizeFitter.",
                },
                # LayoutElement specific
                "minWidth": {"type": "number", "minimum": 0, "description": "Minimum width for LayoutElement. Must be non-negative."},
                "minHeight": {"type": "number", "minimum": 0, "description": "Minimum height for LayoutElement. Must be non-negative."},
                "preferredWidth": {"type": "number", "minimum": 0, "description": "Preferred width for LayoutElement. Must be non-negative."},
                "preferredHeight": {"type": "number", "minimum": 0, "description": "Preferred height for LayoutElement. Must be non-negative."},
                "flexibleWidth": {"type": "number", "minimum": 0, "description": "Flexible width for LayoutElement. Must be non-negative."},
                "flexibleHeight": {"type": "number", "minimum": 0, "description": "Flexible height for LayoutElement. Must be non-negative."},
                "ignoreLayout": {
                    "type": "boolean",
                    "description": "Whether to ignore parent layout for LayoutElement.",
                },
                # AspectRatioFitter specific
                "aspectMode": {
                    "type": "string",
                    "enum": ["None", "WidthControlsHeight", "HeightControlsWidth", "FitInParent", "EnvelopeParent"],
                    "description": "Aspect mode for AspectRatioFitter.",
                },
                "aspectRatio": {
                    "type": "number",
                    "description": "Aspect ratio (width/height) for AspectRatioFitter.",
                },
            },
        },
        ["gameObjectPath", "operation"],
    )

    ugui_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "gameObjectPath": {
                    "type": "string",
                    "description": "Target GameObject path containing the RectTransform.",
                },
                "operation": {
                    "type": "string",
                    "enum": ["rectAdjust", "setAnchor", "setAnchorPreset", "convertToAnchored", "convertToAbsolute", "inspect", "updateRect"],
                    "description": "Operation type: 'rectAdjust' adjusts RectTransform size based on world corners, 'setAnchor' sets custom anchor values, 'setAnchorPreset' applies anchor presets, 'convertToAnchored' converts absolute to anchored position, 'convertToAbsolute' converts anchored to absolute position, 'inspect' retrieves RectTransform state, 'updateRect' updates RectTransform properties.",
                },
                # rectAdjust parameters (legacy support)
                "referenceResolution": {
                    "type": "array",
                    "items": {"type": "number"},
                    "minItems": 2,
                    "maxItems": 2,
                    "description": "CanvasScaler reference resolution (width, height). Used with rectAdjust operation.",
                },
                "matchMode": {
                    "type": "string",
                    "enum": ["widthOrHeight", "expand", "shrink"],
                    "description": "Match mode for rectAdjust operation.",
                },
                # setAnchor parameters (also used with updateRect for individual field format)
                "anchorMinX": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 1,
                    "description": "Anchor min X (0-1 range). Used with setAnchor operation. Alternative to anchorMin dictionary with updateRect operation.",
                },
                "anchorMinY": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 1,
                    "description": "Anchor min Y (0-1 range). Used with setAnchor operation. Alternative to anchorMin dictionary with updateRect operation.",
                },
                "anchorMaxX": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 1,
                    "description": "Anchor max X (0-1 range). Used with setAnchor operation. Alternative to anchorMax dictionary with updateRect operation.",
                },
                "anchorMaxY": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 1,
                    "description": "Anchor max Y (0-1 range). Used with setAnchor operation. Alternative to anchorMax dictionary with updateRect operation.",
                },
                # setAnchorPreset parameters
                "preset": {
                    "type": "string",
                    "enum": [
                        "top-left", "top-center", "top-right",
                        "middle-left", "middle-center", "middle-right", "center",
                        "bottom-left", "bottom-center", "bottom-right",
                        "stretch-horizontal", "stretch-vertical", "stretch-all", "stretch",
                        "stretch-top", "stretch-middle", "stretch-bottom",
                        "stretch-left", "stretch-center-vertical", "stretch-right"
                    ],
                    "description": "Anchor preset name. Used with setAnchorPreset operation.",
                },
                "preservePosition": {
                    "type": "boolean",
                    "description": "Whether to preserve visual position when changing anchors. Default is true.",
                },
                # convertToAnchored parameters
                "absoluteX": {
                    "type": "number",
                    "description": "Absolute X position in parent space. Used with convertToAnchored operation.",
                },
                "absoluteY": {
                    "type": "number",
                    "description": "Absolute Y position in parent space. Used with convertToAnchored operation.",
                },
                # updateRect parameters - supports both dictionary format and individual fields
                "anchoredPosition": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                    },
                    "description": "Anchored position as dictionary (e.g., {'x': 100, 'y': 200}). Alternative to anchoredPositionX/Y. Used with updateRect operation.",
                },
                "anchoredPositionX": {
                    "type": "number",
                    "description": "Anchored position X (individual field format). Alternative to anchoredPosition dictionary. Used with updateRect operation.",
                },
                "anchoredPositionY": {
                    "type": "number",
                    "description": "Anchored position Y (individual field format). Alternative to anchoredPosition dictionary. Used with updateRect operation.",
                },
                "sizeDelta": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number", "minimum": 0},
                        "y": {"type": "number", "minimum": 0},
                    },
                    "description": "Size delta as dictionary (e.g., {'x': 300, 'y': 400}). Alternative to sizeDeltaX/Y. Used with updateRect operation.",
                },
                "sizeDeltaX": {
                    "type": "number",
                    "minimum": 0,
                    "description": "Size delta X (individual field format). Must be non-negative. Alternative to sizeDelta dictionary. Used with updateRect operation.",
                },
                "sizeDeltaY": {
                    "type": "number",
                    "minimum": 0,
                    "description": "Size delta Y (individual field format). Must be non-negative. Alternative to sizeDelta dictionary. Used with updateRect operation.",
                },
                "pivot": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number", "minimum": 0, "maximum": 1},
                        "y": {"type": "number", "minimum": 0, "maximum": 1},
                    },
                    "description": "Pivot point as dictionary (e.g., {'x': 0.5, 'y': 0.5}). Alternative to pivotX/Y. Used with updateRect operation.",
                },
                "pivotX": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 1,
                    "description": "Pivot X (0-1 range, individual field format). Alternative to pivot dictionary. Used with updateRect operation.",
                },
                "pivotY": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 1,
                    "description": "Pivot Y (0-1 range, individual field format). Alternative to pivot dictionary. Used with updateRect operation.",
                },
                "offsetMin": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                    },
                    "description": "Offset min as dictionary (e.g., {'x': 10, 'y': 10}). Alternative to offsetMinX/Y. Used with updateRect operation.",
                },
                "offsetMinX": {
                    "type": "number",
                    "description": "Offset min X (individual field format). Alternative to offsetMin dictionary. Used with updateRect operation.",
                },
                "offsetMinY": {
                    "type": "number",
                    "description": "Offset min Y (individual field format). Alternative to offsetMin dictionary. Used with updateRect operation.",
                },
                "offsetMax": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                    },
                    "description": "Offset max as dictionary (e.g., {'x': -10, 'y': -10}). Alternative to offsetMaxX/Y. Used with updateRect operation.",
                },
                "offsetMaxX": {
                    "type": "number",
                    "description": "Offset max X (individual field format). Alternative to offsetMax dictionary. Used with updateRect operation.",
                },
                "offsetMaxY": {
                    "type": "number",
                    "description": "Offset max Y (individual field format). Alternative to offsetMax dictionary. Used with updateRect operation.",
                },
                "anchorMin": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number", "minimum": 0, "maximum": 1},
                        "y": {"type": "number", "minimum": 0, "maximum": 1},
                    },
                    "description": "Anchor min as dictionary (e.g., {'x': 0, 'y': 0}). Used with updateRect operation.",
                },
                "anchorMax": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number", "minimum": 0, "maximum": 1},
                        "y": {"type": "number", "minimum": 0, "maximum": 1},
                    },
                    "description": "Anchor max as dictionary (e.g., {'x': 1, 'y': 1}). Used with updateRect operation.",
                },
            },
        },
        ["gameObjectPath", "operation"],
    )

    ugui_detect_overlaps_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "gameObjectPath": {
                    "type": "string",
                    "description": "Target GameObject path to check for overlaps. If not specified, checks all UI elements in the scene.",
                },
                "checkAll": {
                    "type": "boolean",
                    "description": "If true, checks all UI elements in the scene for overlaps with each other. If false (default), checks the specified GameObject against others.",
                },
                "includeChildren": {
                    "type": "boolean",
                    "description": "If true, includes child UI elements in the check. Default is false.",
                },
                "threshold": {
                    "type": "number",
                    "minimum": 0,
                    "description": "Minimum overlap area (in square units) to be considered overlapping. Default is 0 (any overlap).",
                },
            },
        },
        [],
    )

    menu_hierarchy_create_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "menuName": {
                    "type": "string",
                    "description": "Name of the root menu (e.g., 'MainMenu'). Will be created as a child of Canvas.",
                },
                "menuStructure": {
                    "type": "object",
                    "description": "Hierarchical menu structure. Each key is a menu item name, each value can be: a string (button text), an array of submenu items, or an object with 'text' and 'submenus' properties.",
                    "additionalProperties": True,
                },
                "generateStateMachine": {
                    "type": "boolean",
                    "description": "If true, generates a MenuStateMachine script with State pattern for navigation control. Default: true.",
                },
                "stateMachineScriptPath": {
                    "type": "string",
                    "description": "Path for the generated MenuStateMachine script (e.g., 'Assets/Scripts/MenuStateMachine.cs'). Required if generateStateMachine is true.",
                },
                "navigationMode": {
                    "type": "string",
                    "enum": ["keyboard", "gamepad", "both"],
                    "description": "Input mode for menu navigation. 'keyboard': arrow keys + Enter, 'gamepad': D-pad + A button, 'both': supports both. Default: 'both'.",
                },
                "buttonWidth": {
                    "type": "number",
                    "description": "Width of menu buttons in pixels. Default: 200.",
                },
                "buttonHeight": {
                    "type": "number",
                    "description": "Height of menu buttons in pixels. Default: 50.",
                },
                "spacing": {
                    "type": "number",
                    "description": "Vertical spacing between menu items in pixels. Default: 10.",
                },
                "enableBackNavigation": {
                    "type": "boolean",
                    "description": "If true, adds 'Back' buttons to submenus for returning to parent menu. Default: true.",
                },
            },
        },
        ["menuName", "menuStructure"],
    )

    template_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["customize", "convertToPrefab"],
                    "description": "Operation to perform. 'customize': Add components and child objects to an existing GameObject. 'convertToPrefab': Convert a GameObject to a prefab asset.",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "Path to the GameObject in the hierarchy (e.g., 'Player', 'Canvas/Panel'). Required for all operations.",
                },
                "components": {
                    "type": "array",
                    "description": "For 'customize' operation: Array of components to add to the GameObject.",
                    "items": {
                        "type": "object",
                        "properties": {
                            "type": {
                                "type": "string",
                                "description": "Fully-qualified component type name (e.g., 'UnityEngine.Rigidbody', 'UnityEngine.UI.Button').",
                            },
                            "properties": {
                                "type": "object",
                                "description": "Optional properties to set on the component after adding it.",
                            },
                            "allowDuplicates": {
                                "type": "boolean",
                                "description": "If true, allows adding the component even if it already exists. Default: false.",
                            },
                        },
                    },
                },
                "children": {
                    "type": "array",
                    "description": "For 'customize' operation: Array of child GameObjects to create under the target GameObject.",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": {
                                "type": "string",
                                "description": "Name of the child GameObject to create.",
                            },
                            "isUI": {
                                "type": "boolean",
                                "description": "If true, creates the child with RectTransform (for UI). Default: false.",
                            },
                            "components": {
                                "type": "array",
                                "description": "Components to add to the child GameObject.",
                                "items": {
                                    "type": "object",
                                    "properties": {
                                        "type": {
                                            "type": "string",
                                            "description": "Fully-qualified component type name.",
                                        },
                                        "properties": {
                                            "type": "object",
                                            "description": "Properties to set on the component.",
                                        },
                                    },
                                },
                            },
                            "position": {
                                "type": "object",
                                "description": "Local position of the child (Vector3 with x, y, z keys).",
                                "properties": {
                                    "x": {"type": "number"},
                                    "y": {"type": "number"},
                                    "z": {"type": "number"},
                                },
                            },
                            "rotation": {
                                "type": "object",
                                "description": "Local euler rotation of the child (Vector3 with x, y, z keys).",
                                "properties": {
                                    "x": {"type": "number"},
                                    "y": {"type": "number"},
                                    "z": {"type": "number"},
                                },
                            },
                            "scale": {
                                "type": "object",
                                "description": "Local scale of the child (Vector3 with x, y, z keys). Default: (1, 1, 1).",
                                "properties": {
                                    "x": {"type": "number"},
                                    "y": {"type": "number"},
                                    "z": {"type": "number"},
                                },
                            },
                        },
                    },
                },
                "prefabPath": {
                    "type": "string",
                    "description": "For 'convertToPrefab' operation: Path where to save the prefab (must start with 'Assets/' and end with '.prefab'). Directory will be created if it doesn't exist.",
                },
                "overwrite": {
                    "type": "boolean",
                    "description": "For 'convertToPrefab' operation: If true, overwrites existing prefab at the path. Default: false.",
                },
            },
        },
        ["operation", "gameObjectPath"],
    )

    design_pattern_generate_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "patternType": {
                    "type": "string",
                    "enum": ["singleton", "objectpool", "statemachine", "observer", "command", "factory", "servicelocator"],
                    "description": "Type of design pattern to generate. Available patterns: singleton (single instance management), objectpool (object reuse pattern), statemachine (state management), observer (event system), command (action abstraction with undo/redo), factory (object creation pattern), servicelocator (global service access).",
                },
                "className": {
                    "type": "string",
                    "description": "Name of the class to generate (e.g., 'GameManager', 'EnemyPool', 'PlayerStateMachine').",
                },
                "scriptPath": {
                    "type": "string",
                    "description": "Full path to the C# script file to create (must start with 'Assets/' and end with '.cs'). Example: 'Assets/Scripts/GameManager.cs'",
                },
                "namespace": {
                    "type": "string",
                    "description": "Optional C# namespace for the generated class (e.g., 'MyGame.Managers'). If not specified, no namespace will be used.",
                },
                "options": {
                    "type": "object",
                    "description": "Pattern-specific options to customize the generated code.",
                    "properties": {
                        # Singleton options
                        "persistent": {
                            "type": "boolean",
                            "description": "For Singleton: If true, uses DontDestroyOnLoad to persist across scenes. Default: false.",
                        },
                        "threadSafe": {
                            "type": "boolean",
                            "description": "For Singleton: If true, uses thread-safe lazy initialization. Default: true.",
                        },
                        "monoBehaviour": {
                            "type": "boolean",
                            "description": "For Singleton: If true, inherits from MonoBehaviour. If false, creates a plain C# singleton. Default: true.",
                        },
                        # ObjectPool options
                        "pooledType": {
                            "type": "string",
                            "description": "For ObjectPool: Type of object to pool (e.g., 'GameObject', 'Bullet', 'Enemy'). Default: 'GameObject'.",
                        },
                        "defaultCapacity": {
                            "type": "string",
                            "description": "For ObjectPool: Initial pool capacity. Default: '10'.",
                        },
                        "maxSize": {
                            "type": "string",
                            "description": "For ObjectPool: Maximum pool size. Default: '100'.",
                        },
                        # Factory options
                        "productType": {
                            "type": "string",
                            "description": "For Factory: Type of products to create (e.g., 'GameObject', 'Enemy', 'Weapon'). Default: 'GameObject'.",
                        },
                    },
                },
            },
        },
        ["patternType", "className", "scriptPath"],
    )

    script_template_generate_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "templateType": {
                    "type": "string",
                    "enum": ["MonoBehaviour", "ScriptableObject"],
                    "description": "Type of Unity script template to generate. 'MonoBehaviour' creates a standard Unity component script. 'ScriptableObject' creates a data container asset class.",
                },
                "className": {
                    "type": "string",
                    "description": "Name of the class to generate (e.g., 'PlayerController', 'GameConfig'). Must be a valid C# identifier.",
                },
                "scriptPath": {
                    "type": "string",
                    "description": "Full path to the C# script file to create (must start with 'Assets/' and end with '.cs'). Example: 'Assets/Scripts/PlayerController.cs'",
                },
                "namespace": {
                    "type": "string",
                    "description": "Optional C# namespace for the generated class (e.g., 'MyGame.Controllers'). If not specified, no namespace will be used.",
                },
            },
        },
        ["templateType", "className", "scriptPath"],
    )

    scriptable_object_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "inspect", "update", "delete", "duplicate", "list", "findByType"],
                    "description": "Operation to perform. 'create' creates a new ScriptableObject asset, 'inspect' retrieves detailed information, 'update' modifies property values, 'delete' removes the asset, 'duplicate' creates a copy, 'list' finds all ScriptableObjects in a folder, 'findByType' searches for ScriptableObjects of a specific type including derived types.",
                },
                "typeName": {
                    "type": "string",
                    "description": "Fully qualified type name of the ScriptableObject (e.g., 'MyGame.PlayerData'). Required for 'create' and 'findByType' operations. Optional for 'list' operation to filter by type.",
                },
                "assetPath": {
                    "type": "string",
                    "description": "Asset path to the ScriptableObject (must start with 'Assets/' and end with '.asset'). Required for create, inspect, update, and delete operations (unless assetGuid is provided).",
                },
                "assetGuid": {
                    "type": "string",
                    "description": "Optional GUID string to uniquely identify the asset. If provided, this takes priority over assetPath. Use this for precise asset identification.",
                },
                "properties": {
                    "type": "object",
                    "additionalProperties": True,
                    "description": "Dictionary of property names to values. For 'create' operation: initial property values. For 'update' operation: properties to change. Failed properties are reported in the response.",
                },
                "includeProperties": {
                    "type": "boolean",
                    "description": "Whether to include property values in the result. Default: true for 'inspect', false for 'findByType'. Use this to control output detail.",
                },
                "propertyFilter": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Optional list of property names to include in inspect results. If specified, only these properties will be returned. Use this to improve performance when you only need specific properties.",
                },
                "searchPath": {
                    "type": "string",
                    "description": "Folder path to search in for 'list' and 'findByType' operations. Default: 'Assets'. Use this to limit search scope.",
                },
                "maxResults": {
                    "type": "integer",
                    "description": "Maximum number of results to return for 'list' and 'findByType' operations. Default: 1000. Use this to prevent performance issues with large datasets.",
                },
                "offset": {
                    "type": "integer",
                    "description": "Number of results to skip for 'list' and 'findByType' operations (pagination). Default: 0. Use with maxResults for pagination.",
                },
                "sourceAssetPath": {
                    "type": "string",
                    "description": "Source asset path for 'duplicate' operation. Can also use sourceAssetGuid instead.",
                },
                "sourceAssetGuid": {
                    "type": "string",
                    "description": "Source asset GUID for 'duplicate' operation. Alternative to sourceAssetPath.",
                },
                "destinationAssetPath": {
                    "type": "string",
                    "description": "Destination asset path for 'duplicate' operation (must start with 'Assets/' and end with '.asset').",
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
                    "enum": ["primitiveToSprite", "svgToSprite", "textureToSprite", "createColorSprite"],
                    "description": "Operation to perform. 'primitiveToSprite' creates sprite from primitive shapes (circle, square, triangle, polygon), 'svgToSprite' converts SVG to sprite, 'textureToSprite' converts texture to sprite with proper import settings, 'createColorSprite' creates solid color sprite for prototyping.",
                },
                "primitiveType": {
                    "type": "string",
                    "enum": ["circle", "square", "rectangle", "triangle", "polygon"],
                    "description": "Type of primitive shape to generate. Used with 'primitiveToSprite' operation.",
                },
                "width": {
                    "type": "integer",
                    "description": "Width of the generated sprite in pixels. Default: 256.",
                },
                "height": {
                    "type": "integer",
                    "description": "Height of the generated sprite in pixels. Default: 256.",
                },
                "color": {
                    "description": "Color for the sprite. Can be a dictionary with r, g, b, a keys (0-1 range) or a hex color string (e.g., '#FF0000'). Default: white.",
                    "oneOf": [
                        {
                            "type": "object",
                            "properties": {
                                "r": {"type": "number", "minimum": 0, "maximum": 1},
                                "g": {"type": "number", "minimum": 0, "maximum": 1},
                                "b": {"type": "number", "minimum": 0, "maximum": 1},
                                "a": {"type": "number", "minimum": 0, "maximum": 1},
                            },
                        },
                        {"type": "string"},
                    ],
                },
                "outputPath": {
                    "type": "string",
                    "description": "Output path for the generated sprite (must start with 'Assets/' and end with '.png'). Required for all operations except 'textureToSprite'.",
                },
                "sides": {
                    "type": "integer",
                    "description": "Number of sides for polygon primitive. Default: 6. Used with 'primitiveToSprite' operation when primitiveType is 'polygon'.",
                },
                "svgPath": {
                    "type": "string",
                    "description": "Path to the SVG file to convert. Required for 'svgToSprite' operation.",
                },
                "texturePath": {
                    "type": "string",
                    "description": "Path to the texture file to convert to sprite. Required for 'textureToSprite' operation.",
                },
                "pixelsPerUnit": {
                    "type": "number",
                    "description": "Pixels per unit for sprite. Used with 'textureToSprite' operation. Default: Unity's default value.",
                },
                "filterMode": {
                    "type": "string",
                    "enum": ["point", "bilinear", "trilinear"],
                    "description": "Filter mode for sprite texture. Used with 'textureToSprite' operation. Default: 'bilinear'.",
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
            name="unity_scene_crud",
            description="Manage Unity scenes and inspect scene context. Scene operations: create, load, save, delete, duplicate. Inspect operation: get comprehensive scene overview including hierarchy (one level only), GameObjects, and components with optional filtering. Build settings: list scenes in build, add/remove scenes from build, reorder scenes, and enable/disable scenes.",
            inputSchema=scene_manage_schema,
        ),
        types.Tool(
            name="unity_gameobject_crud",
            description="Modify the active scene hierarchy (create, delete, move, rename, duplicate) or inspect GameObjects. Use 'inspect' operation to read all attached components with their properties. Supports wildcard/regex patterns with 'findMultiple', 'deleteMultiple', and 'inspectMultiple' operations (e.g., pattern='Enemy*' to find all enemies). Performance tips: Use 'includeProperties=false' for faster inspect, 'componentFilter' to inspect specific components only, and 'maxResults' to limit multiple operations (default 1000).",
            inputSchema=game_object_manage_schema,
        ),
        types.Tool(
            name="unity_component_crud",
            description="Add, remove, update, or inspect components on a GameObject. Supports wildcard/regex patterns with 'addMultiple', 'removeMultiple', 'updateMultiple', and 'inspectMultiple' operations to perform bulk operations on multiple GameObjects (e.g., pattern='Player/Weapon*' to add colliders to all weapons). Performance tips: Use 'includeProperties=false' for faster inspect, 'propertyFilter' to inspect specific properties only, 'maxResults' to limit multiple operations (default 1000), and 'stopOnError=false' for better error handling in batch operations.",
            inputSchema=component_manage_schema,
        ),
        types.Tool(
            name="unity_asset_crud",
            description="Manage Unity assets and asset operations. Use 'create' to create new files (JSON, XML, config files, etc.). Use 'update' to modify existing file content. NOTE: C# script files (.cs) cannot be created/updated - use unity_designPattern_generate, unity_script_template_generate, or code editor tools instead. Use 'delete', 'rename', 'duplicate', 'inspect' for asset operations. Use 'updateImporter' to change asset import settings. Supports wildcard/regex patterns with 'findMultiple', 'deleteMultiple', and 'inspectMultiple'.",
            inputSchema=asset_manage_schema,
        ),
        types.Tool(
            name="unity_ugui_rectAdjust",
            description="[DEPRECATED] Use unity_ugui_manage with operation='rectAdjust' instead. This tool will be removed in a future version. Adjust a RectTransform using uGUI layout utilities.",
            inputSchema=ugui_rect_adjust_schema,
        ),
        types.Tool(
            name="unity_ugui_anchorManage",
            description="[DEPRECATED] Use unity_ugui_manage with operation='setAnchor' or 'setAnchorPreset' instead. This tool will be removed in a future version. Manage RectTransform anchors: set custom values, apply presets (top-left, center, stretch, etc.), or convert between anchor-based and absolute positioning.",
            inputSchema=ugui_anchor_manage_schema,
        ),
        types.Tool(
            name="unity_ugui_manage",
            description="Unified UGUI management tool. Consolidates all UGUI operations: adjust RectTransform size (rectAdjust), set anchors (setAnchor/setAnchorPreset), convert positioning (convertToAnchored/convertToAbsolute), inspect RectTransform state (inspect), and update properties (updateRect). The updateRect operation supports BOTH dictionary format (e.g., anchoredPosition={'x': 100, 'y': 200}) and individual fields format (e.g., anchoredPositionX=100, anchoredPositionY=200) for flexibility and consistency with unity.component.crud.",
            inputSchema=ugui_manage_schema,
        ),
        types.Tool(
            name="unity_scene_quickSetup",
            description="Instantly set up new scenes with common configurations! Choose from 3D (Camera + Light), 2D (2D Camera), UI (Canvas + EventSystem), or VR setups. Saves time by automatically creating all necessary GameObjects with proper settings.",
            inputSchema=scene_quick_setup_schema,
        ),
        types.Tool(
            name="unity_gameobject_createFromTemplate",
            description="Create common GameObjects from templates with one command! Supports primitives (Cube, Sphere, Plane, etc.), lights (Directional, Point, Spot), Camera, Empty, Player, Enemy, Particle System, and Audio Source. Each template includes appropriate components and sensible defaults.",
            inputSchema=gameobject_template_schema,
        ),
        types.Tool(
            name="unity_ugui_createFromTemplate",
            description="Create UI elements from templates with one command. Supports Button, Text, Image, Panel, ScrollView, InputField, Slider, Toggle, and Dropdown. Each template automatically includes necessary components (Image, Button, Text, etc.) with sensible defaults. This is the easiest way for Claude to create common UI elements!",
            inputSchema=ugui_template_create_schema,
        ),
        types.Tool(
            name="unity_ugui_layoutManage",
            description="Manage layout components (HorizontalLayoutGroup, VerticalLayoutGroup, GridLayoutGroup, ContentSizeFitter, LayoutElement, AspectRatioFitter) on UI GameObjects. Add, update, remove, or inspect layout settings. Makes it easy for Claude to create organized UI layouts with proper spacing and alignment.",
            inputSchema=ugui_layout_manage_schema,
        ),
        types.Tool(
            name="unity_ugui_detectOverlaps",
            description="Detect overlapping UI elements in the scene. Can check a specific GameObject for overlaps with others, or check all UI elements for overlaps with each other. Returns a list of overlapping pairs with their overlap areas and bounds. Useful for debugging UI layout issues and ensuring proper UI element positioning.",
            inputSchema=ugui_detect_overlaps_schema,
        ),
        types.Tool(
            name="unity_tagLayer_manage",
            description="Manage tags and layers in Unity. Set/get tags and layers on GameObjects (supports recursive layer setting for hierarchies). Add/remove tags and layers from the project. List all available tags and layers.",
            inputSchema=tag_layer_manage_schema,
        ),
        types.Tool(
            name="unity_prefab_crud",
            description="Manage Unity prefabs: create prefabs from GameObjects, update existing prefabs, inspect prefab assets, instantiate prefabs in scenes, unpack prefab instances, apply or revert instance overrides.",
            inputSchema=prefab_manage_schema,
        ),
        types.Tool(
            name="unity_projectSettings_crud",
            description="Read, write, or list Unity Project Settings. Supports PlayerSettings, QualitySettings, TimeSettings, PhysicsSettings, AudioSettings, and EditorSettings.",
            inputSchema=project_settings_manage_schema,
        ),
        types.Tool(
            name="unity_renderPipeline_manage",
            description="Manage Unity Render Pipeline. Inspect current pipeline (Built-in/URP/HDRP), change pipeline asset, read and update pipeline-specific settings.",
            inputSchema=render_pipeline_manage_schema,
        ),
        types.Tool(
            name="unity_constant_convert",
            description="Convert between Unity constants and numeric values. Supports enum types (e.g., KeyCode.Space ↔ 32), Unity built-in colors (e.g., 'red' ↔ RGBA), and layer names/indices. Also provides listing operations for available values.",
            inputSchema=constant_convert_schema,
        ),
        types.Tool(
            name="unity_await_compilation",
            description="Wait for Unity compilation to complete. Use AFTER making code changes (via unity_designPattern_generate, unity_script_template_generate, or code editor tools) when you need to ensure compilation finishes before proceeding. Returns compilation status, error count, error messages, and console logs. Typical wait time: 5-30 seconds depending on project size. NOTE: This does NOT start compilation - it only waits for ongoing compilation triggered by Unity's file watcher.",
            inputSchema=await_compilation_schema,
        ),
        types.Tool(
            name="unity_designPattern_generate",
            description="Generate C# code for common Unity design patterns! Instantly create production-ready implementations of: Singleton (single instance management with optional persistence), ObjectPool (efficient object reuse), StateMachine (state management with transitions), Observer (event system), Command (action abstraction with undo/redo), Factory (object creation pattern), and ServiceLocator (global service access). Each pattern comes with complete, commented code ready to use. Perfect for quickly implementing best practices in your Unity project!",
            inputSchema=design_pattern_generate_schema,
        ),
        types.Tool(
            name="unity_script_template_generate",
            description="Generate Unity script templates for MonoBehaviour or ScriptableObject. Quickly create starter scripts with standard Unity lifecycle methods. MonoBehaviour template includes Awake, Start, Update, and OnDestroy methods. ScriptableObject template creates a data container class with CreateAssetMenu attribute for easy creation in Unity Editor. Supports optional namespace wrapping.",
            inputSchema=script_template_generate_schema,
        ),
        types.Tool(
            name="unity_template_manage",
            description="Customize existing GameObjects by adding components and child objects, then optionally convert them to reusable prefabs! This tool lets you transform any existing GameObject into a custom template by: (1) Adding multiple components with properties in one operation, (2) Creating child GameObjects with their own components and transforms, (3) Converting the customized GameObject to a prefab for reuse. Perfect for building complex GameObject structures from simple starting points and saving them as prefabs for later use.",
            inputSchema=template_manage_schema,
        ),
        types.Tool(
            name="unity_menu_hierarchyCreate",
            description="Create hierarchical menu systems with nested submenus and automatic State pattern navigation! Generates complete menu UI with vertical layout groups for each menu level, supports keyboard/gamepad navigation, and optionally creates a MenuStateMachine script for managing menu states and transitions. Perfect for creating main menus, pause menus, settings menus with nested options. Features: (1) Declarative menu structure definition, (2) Automatic button creation and layout, (3) State pattern for clean menu navigation code, (4) Input handling for keyboard and gamepad, (5) Parent-child menu transitions with back navigation.",
            inputSchema=menu_hierarchy_create_schema,
        ),
        types.Tool(
            name="unity_scriptableObject_crud",
            description="Manage Unity ScriptableObject assets. Create new ScriptableObject instances with initial property values, inspect existing assets with optional property filtering, update property values, delete assets, duplicate assets to create copies, list all ScriptableObjects in a folder with optional type filtering, and find ScriptableObjects by type including derived types. Supports both asset path and GUID-based identification for precise asset management.",
            inputSchema=scriptable_object_manage_schema,
        ),
        types.Tool(
            name="unity_vectorSprite_convert",
            description="Convert vector data to sprites for rapid prototyping. Generate sprites from primitive shapes (circle, square, triangle, polygon), convert SVG files to sprites, configure existing textures as sprites, or create solid color sprites for UI placeholders. Perfect for prototyping without external assets.",
            inputSchema=vector_sprite_convert_schema,
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
            return await _call_bridge_tool("assetManage", args)

        if name == "unity_ugui_rectAdjust":
            logger.warning(
                "[DEPRECATED] unity_ugui_rectAdjust is deprecated. "
                "Use unity_ugui_manage with operation='rectAdjust' instead. "
                "This tool will be removed in a future version."
            )
            return await _call_bridge_tool("uguiRectAdjust", args)

        if name == "unity_ugui_anchorManage":
            logger.warning(
                "[DEPRECATED] unity_ugui_anchorManage is deprecated. "
                "Use unity_ugui_manage with operation='setAnchor' or 'setAnchorPreset' instead. "
                "This tool will be removed in a future version."
            )
            return await _call_bridge_tool("uguiAnchorManage", args)

        if name == "unity_ugui_manage":
            return await _call_bridge_tool("uguiManage", args)

        if name == "unity_scene_quickSetup":
            return await _call_bridge_tool("sceneQuickSetup", args)

        if name == "unity_gameobject_createFromTemplate":
            return await _call_bridge_tool("gameObjectCreateFromTemplate", args)

        if name == "unity_ugui_createFromTemplate":
            return await _call_bridge_tool("uguiCreateFromTemplate", args)

        if name == "unity_ugui_layoutManage":
            return await _call_bridge_tool("uguiLayoutManage", args)

        if name == "unity_ugui_detectOverlaps":
            return await _call_bridge_tool("uguiDetectOverlaps", args)

        if name == "unity_tagLayer_manage":
            return await _call_bridge_tool("tagLayerManage", args)

        if name == "unity_prefab_crud":
            return await _call_bridge_tool("prefabManage", args)

        if name == "unity_projectSettings_crud":
            return await _call_bridge_tool("projectSettingsManage", args)

        if name == "unity_renderPipeline_manage":
            return await _call_bridge_tool("renderPipelineManage", args)

        if name == "unity_constant_convert":
            return await _call_bridge_tool("constantConvert", args)

        if name == "unity_await_compilation":
            _ensure_bridge_connected()
            timeout_seconds = args.get("timeoutSeconds", 60)  # Increased from 30 to 60 seconds

            try:
                logger.info("Waiting for Unity compilation to complete (timeout=%ss)...", timeout_seconds)
                compilation_result = await bridge_manager.await_compilation(timeout_seconds)

                logger.info(
                    "Compilation finished: success=%s, errors=%s",
                    compilation_result.get("success"),
                    compilation_result.get("errorCount", 0),
                )

                # Get console logs after compilation
                snapshot = editor_log_watcher.get_snapshot(800)

                # Add console logs to the result
                compilation_result["consoleLogs"] = {
                    "all": snapshot.lines,
                    "errors": snapshot.error_lines,
                    "warnings": snapshot.warning_lines,
                    "normal": snapshot.normal_lines,
                }

                return [types.TextContent(type="text", text=as_pretty_json(compilation_result))]

            except TimeoutError:
                logger.warning("Compilation did not finish within %s seconds", timeout_seconds)

                # Get console logs even on timeout
                snapshot = editor_log_watcher.get_snapshot(800)

                error_payload = {
                    "success": False,
                    "completed": False,
                    "timedOut": True,
                    "message": f"Compilation did not finish within {timeout_seconds} seconds.",
                    "consoleLogs": {
                        "all": snapshot.lines,
                        "errors": snapshot.error_lines,
                        "warnings": snapshot.warning_lines,
                        "normal": snapshot.normal_lines,
                    }
                }
                return [types.TextContent(type="text", text=as_pretty_json(error_payload))]

            except Exception as exc:
                logger.error("Error while waiting for compilation: %s", exc)

                # Get console logs even on error
                snapshot = editor_log_watcher.get_snapshot(800)

                error_payload = {
                    "success": False,
                    "completed": False,
                    "error": str(exc),
                    "message": f"Failed to wait for compilation: {exc}",
                    "consoleLogs": {
                        "all": snapshot.lines,
                        "errors": snapshot.error_lines,
                        "warnings": snapshot.warning_lines,
                        "normal": snapshot.normal_lines,
                    }
                }
                return [types.TextContent(type="text", text=as_pretty_json(error_payload))]

        if name == "unity_designPattern_generate":
            return await _call_bridge_tool("designPatternGenerate", args)

        if name == "unity_script_template_generate":
            return await _call_bridge_tool("scriptTemplateGenerate", args)

        if name == "unity_template_manage":
            return await _call_bridge_tool("templateManage", args)

        if name == "unity_menu_hierarchyCreate":
            return await _call_bridge_tool("menuHierarchyCreate", args)

        if name == "unity_scriptableObject_crud":
            return await _call_bridge_tool("scriptableObjectManage", args)

        if name == "unity_vectorSprite_convert":
            return await _call_bridge_tool("vectorSpriteConvert", args)

        raise RuntimeError(f"No handler registered for tool '{name}'.")




