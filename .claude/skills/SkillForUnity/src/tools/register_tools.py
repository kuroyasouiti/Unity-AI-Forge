from __future__ import annotations

from typing import Any, Dict

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


async def _call_bridge_tool(tool_name: str, payload: Dict[str, Any]) -> list[types.Content]:
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


def _schema_with_required(schema: Dict[str, Any], required: list[str]) -> Dict[str, Any]:
    enriched = dict(schema)
    enriched["required"] = required
    enriched["additionalProperties"] = False
    return enriched


def register_tools(server: Server) -> None:
    ping_schema: Dict[str, Any] = {
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
                    "enum": ["create", "load", "save", "delete", "duplicate", "listBuildSettings", "addToBuildSettings", "removeFromBuildSettings", "reorderBuildSettings", "setBuildSettingsEnabled"],
                    "description": "Operation to perform. Scene operations: create, load, save, delete, duplicate. Build settings operations: listBuildSettings (get all scenes in build), addToBuildSettings (add scene to build), removeFromBuildSettings (remove scene from build), reorderBuildSettings (change scene order), setBuildSettingsEnabled (enable/disable scene in build).",
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
                    "enum": ["create", "delete", "move", "rename", "duplicate", "inspect", "findMultiple", "deleteMultiple", "inspectMultiple"],
                    "description": "Operation to perform. Use 'inspect' to read GameObject details including all attached components. Use 'findMultiple', 'deleteMultiple', or 'inspectMultiple' with 'pattern' to perform operations on multiple GameObjects matching a wildcard or regex pattern.",
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
                    "enum": ["updateImporter", "delete", "rename", "duplicate", "inspect", "findMultiple", "deleteMultiple", "inspectMultiple"],
                    "description": "Operation to perform. 'updateImporter' modifies asset importer settings only (file content changes must be done via Claude Code's file tools). Use 'inspect' to read asset details including importer settings. Use 'findMultiple', 'deleteMultiple', or 'inspectMultiple' with 'pattern' to perform operations on multiple assets matching a wildcard or regex pattern. NOTE: This tool does NOT create or modify file contents - use Claude Code's Write/Edit tools for file operations.",
                },
                "assetPath": {
                    "type": "string",
                    "description": "Path under Assets/ for the target asset. Not required for multiple operations (use 'pattern' instead).",
                },
                "assetGuid": {
                    "type": "string",
                    "description": "Optional GUID string to uniquely identify the asset (e.g., 'abc123def456789'). If provided, this takes priority over assetPath. Use this for precise asset identification.",
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
                    "description": "Wildcard pattern (e.g. 'Assets/Scripts/*.cs', 'Assets/Prefabs/Enemy*') or regex pattern for multiple operations. Supports * (any characters) and ? (single character).",
                },
                "useRegex": {
                    "type": "boolean",
                    "description": "If true, treats 'pattern' as a regular expression instead of wildcard pattern. Default is false.",
                },
                "includeProperties": {
                    "type": "boolean",
                    "description": "For inspect/inspectMultiple operations: if true, includes detailed asset importer properties in results. Default is false.",
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

    batch_execute_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operations": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "tool": {
                                "type": "string",
                                "description": "Tool name to execute (e.g., 'assetManage', 'gameObjectManage', 'componentManage'). Use the internal tool name without 'unity_' prefix.",
                            },
                            "payload": {
                                "type": "object",
                                "additionalProperties": True,
                                "description": "Tool-specific payload/arguments.",
                            },
                        },
                        "required": ["tool", "payload"],
                    },
                    "description": "Array of tool operations to execute in sequence. Each operation specifies a tool name and its payload.",
                },
                "stopOnError": {
                    "type": "boolean",
                    "description": "If true, stops batch execution on first error. Default is false (continues on errors).",
                },
                "awaitCompilation": {
                    "type": "boolean",
                    "description": "If true, automatically detects script changes and waits for compilation to complete. Default is true.",
                },
                "timeoutSeconds": {
                    "type": "integer",
                    "description": "Maximum seconds to wait for compilation to complete when awaitCompilation is true. Default is 60.",
                },
            },
        },
        ["operations"],
    )

    console_log_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "logType": {
                    "type": "string",
                    "enum": ["all", "normal", "warning", "error"],
                    "description": "Type of log messages to retrieve. 'all' returns all messages, 'normal' returns info/debug messages, 'warning' returns warnings, 'error' returns errors.",
                },
                "limit": {
                    "type": "integer",
                    "description": "Maximum number of log lines to retrieve. Default is 800.",
                },
            },
        },
        [],
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

    script_batch_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "scripts": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "operation": {
                                "type": "string",
                                "enum": ["create", "update", "delete", "inspect"],
                                "description": "Operation to perform on the script file.",
                            },
                            "scriptPath": {
                                "type": "string",
                                "description": "Path to the C# script file (e.g., 'Assets/Scripts/Player.cs'). Must end with .cs extension.",
                            },
                            "content": {
                                "type": "string",
                                "description": "Script content for create/update operations. Should be valid C# code.",
                            },
                            "namespace": {
                                "type": "string",
                                "description": "Optional namespace for the script. If not specified, uses the default namespace or none.",
                            },
                        },
                        "required": ["operation", "scriptPath"],
                    },
                    "description": "Array of script operations to perform. Each operation specifies what to do with a script file. All operations are executed atomically, then a single compilation is triggered.",
                },
                "stopOnError": {
                    "type": "boolean",
                    "description": "If true, stops execution on first error. If false (default), continues processing remaining scripts and returns both successes and errors.",
                },
                "timeoutSeconds": {
                    "type": "integer",
                    "description": "Maximum time to wait for compilation to complete in seconds. Default is 30. Increase for large projects with many scripts.",
                },
            },
        },
        ["scripts"],
    )

    hierarchy_builder_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "hierarchy": {
                    "type": "object",
                    "description": "Hierarchical structure definition. Each key is the GameObject name, and the value is an object with 'components' (list of component types), 'properties' (dict of property changes), and 'children' (nested hierarchy).",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path. If not specified, creates at root level.",
                },
            },
        },
        ["hierarchy"],
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

    context_inspect_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "includeHierarchy": {
                    "type": "boolean",
                    "description": "Include full scene hierarchy. Default is true.",
                },
                "includeComponents": {
                    "type": "boolean",
                    "description": "Include component types for each GameObject. Default is false.",
                },
                "maxDepth": {
                    "type": "integer",
                    "description": "Maximum hierarchy depth to inspect. Default is unlimited.",
                },
                "filter": {
                    "type": "string",
                    "description": "Filter GameObjects by name pattern (supports wildcards * and ?).",
                },
            },
        },
        [],
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

    tool_definitions = [
        types.Tool(
            name="unity_ping",
            description="Verify bridge connectivity and return the latest heartbeat information.",
            inputSchema=ping_schema,
        ),
        types.Tool(
            name="unity_scene_crud",
            description="Create, load, save, delete, or duplicate Unity scenes. Also manages build settings: list scenes in build, add/remove scenes from build, reorder scenes, and enable/disable scenes in build settings.",
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
            description="Manage Unity asset importer settings, and perform asset operations (rename, duplicate, delete, inspect). This tool does NOT create or modify file contents - use Claude Code's Write/Edit tools for file operations. Use 'updateImporter' to change asset import settings (texture type, model import options, etc.). Supports wildcard/regex patterns with 'findMultiple', 'deleteMultiple', and 'inspectMultiple' operations. IMPORTANT: For C# scripts, use unity_script_batch_manage instead.",
            inputSchema=asset_manage_schema,
        ),
        types.Tool(
            name="unity_ugui_rectAdjust",
            description="Adjust a RectTransform using uGUI layout utilities.",
            inputSchema=ugui_rect_adjust_schema,
        ),
        types.Tool(
            name="unity_ugui_anchorManage",
            description="Manage RectTransform anchors: set custom values, apply presets (top-left, center, stretch, etc.), or convert between anchor-based and absolute positioning.",
            inputSchema=ugui_anchor_manage_schema,
        ),
        types.Tool(
            name="unity_ugui_manage",
            description="Unified UGUI management tool. Consolidates all UGUI operations: adjust RectTransform size (rectAdjust), set anchors (setAnchor/setAnchorPreset), convert positioning (convertToAnchored/convertToAbsolute), inspect RectTransform state (inspect), and update properties (updateRect). The updateRect operation supports BOTH dictionary format (e.g., anchoredPosition={'x': 100, 'y': 200}) and individual fields format (e.g., anchoredPositionX=100, anchoredPositionY=200) for flexibility and consistency with unity.component.crud.",
            inputSchema=ugui_manage_schema,
        ),
        types.Tool(
            name="unity_hierarchy_builder",
            description="Build complex GameObject hierarchies declaratively in one command! Define nested structures with components and properties using a simple JSON format. Perfect for creating multi-level UI layouts, scene structures, or prefab hierarchies without multiple separate commands.",
            inputSchema=hierarchy_builder_schema,
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
            name="unity_context_inspect",
            description="Get a comprehensive overview of the current scene structure! Returns scene hierarchy, active GameObjects, components, and other context information. Optionally filter by pattern and control detail level. Helps Claude understand the current state before making changes.",
            inputSchema=context_inspect_schema,
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
            name="unity_batch_execute",
            description="Execute multiple Unity tool operations in a single batch. Supports any combination of tools (assetManage, gameObjectManage, componentManage, etc.). Automatically detects script changes and waits for compilation to complete. Perfect for complex multi-step operations like creating multiple GameObjects, setting up scenes, or managing assets. Each operation specifies a tool name and its payload. Operations are executed sequentially, and results are returned for each operation.",
            inputSchema=batch_execute_schema,
        ),
        types.Tool(
            name="unity_console_log",
            description="Retrieve Unity Editor console log messages. Returns recent log output filtered by type (all/normal/warning/error). Useful for debugging compilation errors, runtime issues, and monitoring Unity's console output.",
            inputSchema=console_log_schema,
        ),
        types.Tool(
            name="unity_await_compilation",
            description="Wait for Unity compilation to complete without triggering it. Use this when Unity is already compiling (e.g., after script changes detected by file watcher) and you want to wait for the compilation to finish. Returns compilation result with success status, error count, and error messages. Does NOT start compilation - only waits for ongoing compilation to finish.",
            inputSchema=await_compilation_schema,
        ),
        types.Tool(
            name="unity_script_batch_manage",
            description="CRITICAL: ALWAYS use this tool for ALL C# script operations! Batch manage C# scripts with automatic compilation handling. Supports create, update, delete, and inspect operations. This is the ONLY correct way to manage scripts - using unity_asset_crud for scripts will cause compilation issues! Benefits: (1) 10-20x faster for multiple scripts by doing single compilation, (2) Atomic operations - all succeed or fail together, (3) Automatic compilation detection and waiting, (4) Proper error reporting with per-script results. IMPORTANT: Always use 'scripts' array even for single script operations. DO NOT use unity_asset_crud for .cs files!",
            inputSchema=script_batch_manage_schema,
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
            return await _call_bridge_tool("uguiRectAdjust", args)

        if name == "unity_ugui_anchorManage":
            return await _call_bridge_tool("uguiAnchorManage", args)

        if name == "unity_ugui_manage":
            return await _call_bridge_tool("uguiManage", args)

        if name == "unity_hierarchy_builder":
            return await _call_bridge_tool("hierarchyBuilder", args)

        if name == "unity_scene_quickSetup":
            return await _call_bridge_tool("sceneQuickSetup", args)

        if name == "unity_gameobject_createFromTemplate":
            return await _call_bridge_tool("gameObjectCreateFromTemplate", args)

        if name == "unity_context_inspect":
            return await _call_bridge_tool("contextInspect", args)

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

        if name == "unity_batch_execute":
            # Batch execute multiple tool operations
            await_compilation = args.get("awaitCompilation", True)
            timeout_seconds = args.get("timeoutSeconds", 60)

            logger.info("Executing batch operations...")

            # Execute the batch operations
            batch_result = await bridge_manager.send_command("batchExecute", args)

            # If awaitCompilation is enabled and compilation was triggered, wait for it
            if await_compilation and batch_result.get("compilationTriggered", False):
                try:
                    logger.info(
                        "Compilation was triggered, waiting for completion (timeout=%ss)...",
                        timeout_seconds,
                    )
                    compilation_result = await bridge_manager.await_compilation(timeout_seconds)

                    # Combine batch result with compilation result
                    response_payload = {
                        "batch": batch_result,
                        "compilation": compilation_result,
                    }

                    logger.info(
                        "Batch completed: success=%s, compilation_success=%s",
                        batch_result.get("success"),
                        compilation_result.get("success"),
                    )

                    return [types.TextContent(type="text", text=as_pretty_json(response_payload))]

                except TimeoutError:
                    logger.warning(
                        "Compilation did not finish within %s seconds", timeout_seconds
                    )
                    response_payload = {
                        "batch": batch_result,
                        "compilation": {
                            "success": False,
                            "completed": False,
                            "timedOut": True,
                            "message": f"Compilation did not finish within {timeout_seconds} seconds.",
                        },
                    }
                    return [types.TextContent(type="text", text=as_pretty_json(response_payload))]

                except Exception as exc:
                    logger.error("Error while waiting for compilation: %s", exc)
                    response_payload = {
                        "batch": batch_result,
                        "compilation": {
                            "success": False,
                            "completed": False,
                            "error": str(exc),
                        },
                    }
                    return [types.TextContent(type="text", text=as_pretty_json(response_payload))]
            else:
                # No compilation needed or awaitCompilation disabled
                return [types.TextContent(type="text", text=as_pretty_json(batch_result))]

        if name == "unity_console_log":
            log_type = args.get("logType", "all")
            limit = args.get("limit", 800)

            snapshot = editor_log_watcher.get_snapshot(limit)

            if log_type == "all":
                lines = snapshot.lines
                message = "No log events captured yet. Confirm the Unity Editor log path." if not lines else None
            elif log_type == "normal":
                lines = snapshot.normal_lines
                message = "No normal log events captured yet." if not lines else None
            elif log_type == "warning":
                lines = snapshot.warning_lines
                message = "No warning log events captured yet." if not lines else None
            elif log_type == "error":
                lines = snapshot.error_lines
                message = "No error log events captured yet." if not lines else None
            else:
                return [types.TextContent(type="text", text=f"Unknown log type: {log_type}")]

            body = "\n".join(lines) if lines else (message or "")
            return [types.TextContent(type="text", text=body)]

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

                return [types.TextContent(type="text", text=as_pretty_json(compilation_result))]

            except TimeoutError:
                logger.warning("Compilation did not finish within %s seconds", timeout_seconds)
                error_payload = {
                    "success": False,
                    "completed": False,
                    "timedOut": True,
                    "message": f"Compilation did not finish within {timeout_seconds} seconds.",
                }
                return [types.TextContent(type="text", text=as_pretty_json(error_payload))]

            except Exception as exc:
                logger.error("Error while waiting for compilation: %s", exc)
                error_payload = {
                    "success": False,
                    "completed": False,
                    "error": str(exc),
                    "message": f"Failed to wait for compilation: {exc}",
                }
                return [types.TextContent(type="text", text=as_pretty_json(error_payload))]

        if name == "unity_script_batch_manage":
            _ensure_bridge_connected()

            # Extract timeout with increased default for script operations
            timeout_seconds = args.get("timeoutSeconds", 30)

            try:
                logger.info("Executing script batch operations (timeout=%ss)...", timeout_seconds)

                # Execute the batch script operations
                batch_result = await bridge_manager.send_command("scriptBatchManage", args, timeout_ms=(timeout_seconds + 20) * 1000)

                # Check if compilation was triggered
                if batch_result.get("compilationTriggered", False):
                    logger.info("Script operations triggered compilation, waiting for completion...")

                    try:
                        compilation_result = await bridge_manager.await_compilation(timeout_seconds)

                        # Combine batch result with compilation result
                        response_payload = {
                            "batch": batch_result,
                            "compilation": compilation_result,
                        }

                        logger.info(
                            "Script batch completed: success=%s, compilation_success=%s",
                            batch_result.get("success"),
                            compilation_result.get("success"),
                        )

                        return [types.TextContent(type="text", text=as_pretty_json(response_payload))]

                    except TimeoutError:
                        logger.warning("Compilation did not finish within %s seconds", timeout_seconds)
                        response_payload = {
                            "batch": batch_result,
                            "compilation": {
                                "success": False,
                                "completed": False,
                                "timedOut": True,
                                "message": f"Compilation did not finish within {timeout_seconds} seconds.",
                            },
                        }
                        return [types.TextContent(type="text", text=as_pretty_json(response_payload))]

                    except Exception as exc:
                        logger.error("Error while waiting for compilation: %s", exc)
                        response_payload = {
                            "batch": batch_result,
                            "compilation": {
                                "success": False,
                                "completed": False,
                                "error": str(exc),
                            },
                        }
                        return [types.TextContent(type="text", text=as_pretty_json(response_payload))]
                else:
                    # No compilation needed (e.g., only inspect operations)
                    logger.info("Script batch completed without triggering compilation")
                    return [types.TextContent(type="text", text=as_pretty_json(batch_result))]

            except Exception as exc:
                logger.error("Script batch operation failed: %s", exc)
                raise RuntimeError(f"Script batch operation failed: {exc}") from exc

        raise RuntimeError(f"No handler registered for tool '{name}'.")




