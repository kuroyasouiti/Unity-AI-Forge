from __future__ import annotations

from typing import Any, Dict

import mcp.types as types
from mcp.server import Server

from ..bridge.bridge_manager import bridge_manager
from ..utils.json_utils import as_pretty_json


def _ensure_bridge_connected() -> None:
    if not bridge_manager.is_connected():
        raise RuntimeError(
            "Unity bridge is not connected. In the Unity Editor choose Tools/MCP Assistant to start the bridge."
        )


async def _call_bridge_tool(tool_name: str, payload: Dict[str, Any]) -> list[types.Content]:
    _ensure_bridge_connected()

    # Unity側のタイムアウトに15秒のバッファを追加してPython側のタイムアウトを設定
    # これにより、Unity側でコンパイル完了を待つ時間が確保される
    timeout_ms = 30_000  # デフォルト30秒
    if "timeoutSeconds" in payload:
        unity_timeout = payload["timeoutSeconds"]
        timeout_ms = (unity_timeout + 15) * 1000

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
                    "enum": ["create", "load", "save", "delete", "duplicate"],
                    "description": "Operation to perform.",
                },
                "scenePath": {
                    "type": "string",
                    "description": "Path under Assets/, e.g. Assets/Scenes/Main.unity.",
                },
                "newSceneName": {"type": "string"},
                "additive": {"type": "boolean"},
                "includeOpenScenes": {"type": "boolean"},
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
                "payload": {
                    "type": "object",
                    "additionalProperties": True,
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
                    "enum": ["create", "update", "delete", "rename", "duplicate", "inspect", "findMultiple", "deleteMultiple", "inspectMultiple"],
                    "description": "Operation to perform. Use 'inspect' to read asset details. Use 'findMultiple', 'deleteMultiple', or 'inspectMultiple' with 'pattern' to perform operations on multiple assets matching a wildcard or regex pattern.",
                },
                "assetPath": {
                    "type": "string",
                    "description": "Path under Assets/ for the target asset. Not required for multiple operations (use 'pattern' instead).",
                },
                "destinationPath": {"type": "string"},
                "contents": {
                    "type": "string",
                    "description": "File contents for create and update operations.",
                },
                "overwrite": {"type": "boolean"},
                "metadata": {
                    "type": "object",
                    "additionalProperties": True,
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
                    "description": "For inspectMultiple operation: if true, includes detailed asset properties in results. Default is false.",
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

    script_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["read", "create", "update", "delete", "outline"],
                    "description": "Operation to perform. Use 'read' to analyze scripts (and fetch source), 'create' to generate new scripts, 'update' to apply edits, and 'delete' to remove scripts. 'outline' is accepted for backwards compatibility.",
                },
                "guid": {
                    "type": "string",
                    "description": "Optional GUID lookup for scriptPath/assetPath.",
                },
                "assetPath": {
                    "type": "string",
                    "description": "Path under Assets/. Required for outline when guid is not supplied and accepted as fallback for update/delete.",
                },
                "includeMembers": {
                    "type": "boolean",
                    "description": "Whether to include member details in the outline (read operation).",
                },
                "includeSource": {
                    "type": "boolean",
                    "description": "Whether to include the full script text in read responses. Default true.",
                },
                "waitForCompilation": {
                    "type": "boolean",
                    "description": "Whether to wait for ongoing compilation to complete before reading the script (read operation). Default true. Set to false to read immediately even if compilation is in progress.",
                },
                "scriptPath": {
                    "type": "string",
                    "description": "Path under Assets/ for the script (e.g. Assets/Scripts/PlayerController.cs). Required for create/update/delete. The .cs extension is optional when creating.",
                },
                "scriptType": {
                    "type": "string",
                    "enum": ["monoBehaviour", "scriptableObject", "editor", "class", "interface", "struct"],
                    "description": "Type of script to create. Default is 'monoBehaviour'.",
                },
                "namespace": {
                    "type": "string",
                    "description": "Optional namespace for the script.",
                },
                "methods": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "List of method names to generate (e.g. ['Start', 'Update', 'Awake']). Common methods are auto-templated.",
                },
                "fields": {
                    "type": "array",
                    "items": {
                        "oneOf": [
                            {"type": "string"},
                            {
                                "type": "object",
                                "properties": {
                                    "name": {"type": "string"},
                                    "type": {"type": "string"},
                                    "visibility": {
                                        "type": "string",
                                        "enum": ["public", "private", "protected"],
                                    },
                                    "serialize": {"type": "boolean"},
                                    "defaultValue": {"type": "string"},
                                },
                                "required": ["name"],
                            },
                        ]
                    },
                    "description": "List of fields to add. Can be simple strings like 'float speed' or objects with name, type, visibility, serialize, and defaultValue (create operation).",
                },
                "attributes": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "List of class-level attributes (e.g. ['RequireComponent(typeof(Rigidbody))']).",
                },
                "baseClass": {
                    "type": "string",
                    "description": "Custom base class to inherit from. Overrides the default base class for the script type.",
                },
                "interfaces": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "List of interfaces to implement (e.g. ['IPointerClickHandler', 'IBeginDragHandler']).",
                },
                "includeUsings": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Additional using statements to include (e.g. ['UnityEngine.UI', 'System.Collections']).",
                },
                "edits": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "action": {
                                "type": "string",
                                "enum": ["replace", "insertBefore", "insertAfter", "delete"],
                                "description": "Edit action applied during update.",
                            },
                            "match": {
                                "type": "string",
                                "description": "Text to locate in the script before applying the edit.",
                            },
                            "replacement": {
                                "type": "string",
                                "description": "Replacement text (replace/delete actions).",
                            },
                            "text": {
                                "type": "string",
                                "description": "Text to insert (insertBefore/insertAfter actions).",
                            },
                            "count": {
                                "type": "integer",
                                "minimum": 0,
                                "description": "Maximum occurrences to apply. Use 0 to apply to all matches.",
                            },
                            "caseSensitive": {
                                "type": "boolean",
                                "description": "Whether match comparison is case-sensitive. Default true.",
                            },
                            "allowMissingMatch": {
                                "type": "boolean",
                                "description": "When true, silently skip edits whose match text is missing.",
                            },
                        },
                        "required": ["action", "match"],
                        "additionalProperties": False,
                    },
                    "description": "Ordered list of textual edits for the update operation.",
                },
                "dryRun": {
                    "type": "boolean",
                    "description": "Preview the result without writing to disk (update/delete operations).",
                },
                "timeoutSeconds": {
                    "type": "integer",
                    "minimum": 1,
                    "description": "Maximum time to wait for compilation in seconds. Default is 30 seconds. Compilation is now automatically awaited after script creation/update.",
                },
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

    input_system_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["listActions", "createAsset", "addActionMap", "addAction", "addBinding", "inspectAsset", "deleteAsset", "deleteActionMap", "deleteAction", "deleteBinding"],
                    "description": "Operation to perform. 'listActions' lists all Input Action assets, 'createAsset' creates new asset, 'addActionMap' adds action map, 'addAction' adds action to map, 'addBinding' adds binding to action, 'inspectAsset' inspects asset contents, 'deleteAsset' deletes entire asset, 'deleteActionMap' deletes action map, 'deleteAction' deletes action, 'deleteBinding' deletes specific binding or all bindings.",
                },
                "assetPath": {
                    "type": "string",
                    "description": "Path to InputActionAsset file (e.g., Assets/Input/PlayerControls.inputactions).",
                },
                "mapName": {
                    "type": "string",
                    "description": "Name of the action map (for addActionMap, addAction, addBinding, deleteActionMap, deleteAction, deleteBinding operations).",
                },
                "actionName": {
                    "type": "string",
                    "description": "Name of the action (for addAction, addBinding, deleteAction, deleteBinding operations).",
                },
                "actionType": {
                    "type": "string",
                    "enum": ["Button", "Value", "PassThrough"],
                    "description": "Type of action (for addAction operation). Default is 'Button'.",
                },
                "path": {
                    "type": "string",
                    "description": "Binding path (e.g., '<Keyboard>/space', '<Mouse>/leftButton') for addBinding operation.",
                },
                "bindingIndex": {
                    "type": "integer",
                    "description": "Index of binding to delete (for deleteBinding operation). Omit or set to -1 to delete all bindings.",
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
                                "description": "Tool name to execute (e.g., 'gameObjectManage', 'componentManage', 'assetManage', etc.).",
                            },
                            "payload": {
                                "type": "object",
                                "additionalProperties": True,
                                "description": "Payload for the tool operation.",
                            },
                        },
                        "required": ["tool", "payload"],
                        "additionalProperties": False,
                    },
                    "description": "Array of operations to execute in sequence.",
                },
                "stopOnError": {
                    "type": "boolean",
                    "description": "If true, stops execution when an operation fails. Default is false (continues on error).",
                },
            },
        },
        ["operations"],
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

    tilemap_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["createTilemap", "setTile", "getTile", "clearTile", "fillArea", "inspectTilemap", "clearAll"],
                    "description": "Operation to perform. 'createTilemap' creates a new Tilemap GameObject with Grid parent, 'setTile' places a tile at position, 'getTile' retrieves tile at position, 'clearTile' removes tile at position, 'fillArea' fills rectangular area with tiles, 'inspectTilemap' returns Tilemap information, 'clearAll' clears all tiles from Tilemap.",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "Hierarchy path of the Tilemap GameObject (for setTile, getTile, clearTile, fillArea, inspectTilemap, clearAll operations).",
                },
                "tilemapName": {
                    "type": "string",
                    "description": "Name for the new Tilemap (for createTilemap operation).",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent path for the new Tilemap Grid (for createTilemap operation).",
                },
                "tileAssetPath": {
                    "type": "string",
                    "description": "Asset path to the tile (e.g., 'Assets/Tiles/MyTile.asset'). Required for setTile and fillArea operations.",
                },
                "positionX": {
                    "type": "integer",
                    "description": "X position in tilemap grid coordinates (for setTile, getTile, clearTile operations).",
                },
                "positionY": {
                    "type": "integer",
                    "description": "Y position in tilemap grid coordinates (for setTile, getTile, clearTile operations).",
                },
                "positionZ": {
                    "type": "integer",
                    "description": "Z position in tilemap grid coordinates (for setTile, getTile, clearTile operations). Default is 0.",
                },
                "startX": {
                    "type": "integer",
                    "description": "Start X position for area fill (for fillArea operation).",
                },
                "startY": {
                    "type": "integer",
                    "description": "Start Y position for area fill (for fillArea operation).",
                },
                "endX": {
                    "type": "integer",
                    "description": "End X position for area fill (for fillArea operation).",
                },
                "endY": {
                    "type": "integer",
                    "description": "End Y position for area fill (for fillArea operation).",
                },
            },
        },
        ["operation"],
    )

    navmesh_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["bakeNavMesh", "clearNavMesh", "addNavMeshAgent", "setDestination", "inspectNavMesh", "updateSettings", "createNavMeshSurface"],
                    "description": "Operation to perform. 'bakeNavMesh' bakes the NavMesh, 'clearNavMesh' clears baked NavMesh data, 'addNavMeshAgent' adds NavMeshAgent component to GameObject, 'setDestination' sets NavMeshAgent destination, 'inspectNavMesh' returns NavMesh statistics, 'updateSettings' modifies NavMesh bake settings, 'createNavMeshSurface' creates a NavMesh Surface component (requires NavMesh Components package).",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "Hierarchy path of the GameObject (for addNavMeshAgent, setDestination, createNavMeshSurface operations).",
                },
                "destinationX": {
                    "type": "number",
                    "description": "X coordinate of destination (for setDestination operation).",
                },
                "destinationY": {
                    "type": "number",
                    "description": "Y coordinate of destination (for setDestination operation).",
                },
                "destinationZ": {
                    "type": "number",
                    "description": "Z coordinate of destination (for setDestination operation).",
                },
                "settings": {
                    "type": "object",
                    "additionalProperties": True,
                    "description": "NavMesh bake settings to update (for updateSettings operation). Properties: agentRadius, agentHeight, agentSlope, agentClimb, etc.",
                },
                "agentSpeed": {
                    "type": "number",
                    "description": "NavMeshAgent speed (for addNavMeshAgent operation).",
                },
                "agentAcceleration": {
                    "type": "number",
                    "description": "NavMeshAgent acceleration (for addNavMeshAgent operation).",
                },
                "agentStoppingDistance": {
                    "type": "number",
                    "description": "NavMeshAgent stopping distance (for addNavMeshAgent operation).",
                },
            },
        },
        ["operation"],
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
                    "description": "Width of the UI element. Default varies by template.",
                },
                "height": {
                    "type": "number",
                    "description": "Height of the UI element. Default varies by template.",
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
                    "description": "Cell width for GridLayoutGroup.",
                },
                "cellSizeY": {
                    "type": "number",
                    "description": "Cell height for GridLayoutGroup.",
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
                "minWidth": {"type": "number", "description": "Minimum width for LayoutElement."},
                "minHeight": {"type": "number", "description": "Minimum height for LayoutElement."},
                "preferredWidth": {"type": "number", "description": "Preferred width for LayoutElement."},
                "preferredHeight": {"type": "number", "description": "Preferred height for LayoutElement."},
                "flexibleWidth": {"type": "number", "description": "Flexible width for LayoutElement."},
                "flexibleHeight": {"type": "number", "description": "Flexible height for LayoutElement."},
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
                # setAnchor parameters
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
                # updateRect parameters
                "anchoredPositionX": {
                    "type": "number",
                    "description": "Anchored position X. Used with updateRect operation.",
                },
                "anchoredPositionY": {
                    "type": "number",
                    "description": "Anchored position Y. Used with updateRect operation.",
                },
                "sizeDeltaX": {
                    "type": "number",
                    "description": "Size delta X. Used with updateRect operation.",
                },
                "sizeDeltaY": {
                    "type": "number",
                    "description": "Size delta Y. Used with updateRect operation.",
                },
                "pivotX": {
                    "type": "number",
                    "description": "Pivot X (0-1 range). Used with updateRect operation.",
                },
                "pivotY": {
                    "type": "number",
                    "description": "Pivot Y (0-1 range). Used with updateRect operation.",
                },
                "offsetMinX": {
                    "type": "number",
                    "description": "Offset min X. Used with updateRect operation.",
                },
                "offsetMinY": {
                    "type": "number",
                    "description": "Offset min Y. Used with updateRect operation.",
                },
                "offsetMaxX": {
                    "type": "number",
                    "description": "Offset max X. Used with updateRect operation.",
                },
                "offsetMaxY": {
                    "type": "number",
                    "description": "Offset max Y. Used with updateRect operation.",
                },
            },
        },
        ["gameObjectPath", "operation"],
    )

    tool_definitions = [
        types.Tool(
            name="unity_ping",
            description="Verify bridge connectivity and return the latest heartbeat information.",
            inputSchema=ping_schema,
        ),
        types.Tool(
            name="unity_scene_crud",
            description="Create, load, save, delete, or duplicate Unity scenes.",
            inputSchema=scene_manage_schema,
        ),
        types.Tool(
            name="unity_gameobject_crud",
            description="Modify the active scene hierarchy (create, delete, move, rename, duplicate) or inspect GameObjects. Use 'inspect' operation to read all attached components with their properties. Supports wildcard/regex patterns with 'findMultiple', 'deleteMultiple', and 'inspectMultiple' operations (e.g., pattern='Enemy*' to find all enemies).",
            inputSchema=game_object_manage_schema,
        ),
        types.Tool(
            name="unity_component_crud",
            description="Add, remove, update, or inspect components on a GameObject. Supports wildcard/regex patterns with 'addMultiple', 'removeMultiple', 'updateMultiple', and 'inspectMultiple' operations to perform bulk operations on multiple GameObjects (e.g., pattern='Player/Weapon*' to add colliders to all weapons).",
            inputSchema=component_manage_schema,
        ),
        types.Tool(
            name="unity_asset_crud",
            description="Create, update, rename, duplicate, delete, or inspect Assets/ files. Supports wildcard/regex patterns with 'findMultiple', 'deleteMultiple', and 'inspectMultiple' operations to perform bulk operations on multiple assets (e.g., pattern='Assets/Scripts/*.cs' to find all C# scripts).",
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
            description="Unified UGUI management tool. Consolidates all UGUI operations: adjust RectTransform size (rectAdjust), set anchors (setAnchor/setAnchorPreset), convert positioning (convertToAnchored/convertToAbsolute), inspect RectTransform state (inspect), and update properties (updateRect).",
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
            name="unity_tagLayer_manage",
            description="Manage tags and layers in Unity. Set/get tags and layers on GameObjects (supports recursive layer setting for hierarchies). Add/remove tags and layers from the project. List all available tags and layers.",
            inputSchema=tag_layer_manage_schema,
        ),
        types.Tool(
            name="unity_script_manage",
            description="Manage Unity C# scripts from a unified tool. Use operation='read' to analyze scripts (outline + source), 'create' to scaffold new ones, 'update' to apply textual edits, or 'delete' to remove scripts safely (with optional dry-run preview). All operations (create/update/delete/read) automatically wait for compilation to complete.",
            inputSchema=script_manage_schema,
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
            name="unity_inputSystem_manage",
            description="Manage Unity New Input System. Create Input Action assets, add action maps and actions, configure bindings, and inspect existing assets. Requires Input System package to be installed.",
            inputSchema=input_system_manage_schema,
        ),
        types.Tool(
            name="unity_batch_execute",
            description="Execute multiple Unity operations in a single batch. Allows sequential execution of any tool operations with optional error handling. Returns results for all operations including successes and failures.",
            inputSchema=batch_execute_schema,
        ),
        types.Tool(
            name="unity_tilemap_manage",
            description="Manage Unity Tilemap system. Create tilemaps with Grid parent, set/get/clear tiles at positions, fill rectangular areas with tiles, inspect tilemap information. Supports 2D grid-based tile placement for level design.",
            inputSchema=tilemap_manage_schema,
        ),
        types.Tool(
            name="unity_navmesh_manage",
            description="Manage Unity NavMesh navigation system. Bake/clear NavMesh, add NavMeshAgent components to GameObjects, set agent destinations, inspect NavMesh statistics, and update bake settings. Supports runtime pathfinding for AI characters.",
            inputSchema=navmesh_manage_schema,
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

        if name == "unity_tagLayer_manage":
            return await _call_bridge_tool("tagLayerManage", args)

        if name == "unity_script_manage":
            return await _call_bridge_tool("scriptManage", args)

        if name == "unity_prefab_crud":
            return await _call_bridge_tool("prefabManage", args)

        if name == "unity_projectSettings_crud":
            return await _call_bridge_tool("projectSettingsManage", args)

        if name == "unity_renderPipeline_manage":
            return await _call_bridge_tool("renderPipelineManage", args)

        if name == "unity_inputSystem_manage":
            return await _call_bridge_tool("inputSystemManage", args)

        if name == "unity_batch_execute":
            return await _call_bridge_tool("batchExecute", args)

        if name == "unity_tilemap_manage":
            return await _call_bridge_tool("tilemapManage", args)

        if name == "unity_navmesh_manage":
            return await _call_bridge_tool("navmeshManage", args)

        raise RuntimeError(f"No handler registered for tool '{name}'.")
