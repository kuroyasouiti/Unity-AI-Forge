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
    try:
        response = await bridge_manager.send_command(tool_name, payload)
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
                    "enum": ["create", "delete", "move", "rename", "duplicate", "inspect"],
                    "description": "Operation to perform. Use 'inspect' to read GameObject details including all attached components.",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "Hierarchy path of the GameObject (e.g. Root/Child/Button).",
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
                "payload": {
                    "type": "object",
                    "additionalProperties": True,
                },
            },
        },
        ["operation", "gameObjectPath"],
    )

    component_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["add", "remove", "update", "inspect"],
                    "description": "Operation to perform. Use 'inspect' to read component state.",
                },
                "gameObjectPath": {"type": "string"},
                "componentType": {
                    "type": "string",
                    "description": "Fully qualified component type (e.g. UnityEngine.UI.Text).",
                },
                "propertyChanges": {
                    "type": "object",
                    "additionalProperties": True,
                    "description": "Property/value pairs to apply to the component. For UnityEngine.Object properties (e.g. Mesh, Material), use asset paths like 'Assets/Models/Sphere.fbx' or built-in resources like 'Library/unity default resources::Sphere'.",
                },
                "applyDefaults": {"type": "boolean"},
            },
        },
        ["operation", "gameObjectPath", "componentType"],
    )

    asset_manage_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "delete", "rename", "duplicate", "inspect"],
                    "description": "Operation to perform. Use 'inspect' to read asset details.",
                },
                "assetPath": {
                    "type": "string",
                    "description": "Path under Assets/ for the target asset.",
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
            },
        },
        ["operation", "assetPath"],
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

    script_outline_schema = {
        "type": "object",
        "properties": {
            "guid": {"type": "string"},
            "assetPath": {
                "type": "string",
                "description": "Path under Assets/. Either guid or assetPath is required.",
            },
            "includeMembers": {
                "type": "boolean",
                "description": "Whether to include member details in the outline.",
            },
        },
        "required": [],
        "additionalProperties": False,
    }

    script_create_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "scriptPath": {
                    "type": "string",
                    "description": "Path under Assets/ for the new script (e.g. Assets/Scripts/PlayerController.cs). The .cs extension is optional.",
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
                    "description": "List of fields to add. Can be simple strings like 'float speed' or objects with name, type, visibility, serialize, and defaultValue.",
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
            },
        },
        ["scriptPath"],
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

    compile_schema: Dict[str, Any] = {
        "type": "object",
        "properties": {},
        "additionalProperties": False,
    }

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
            name="unity.ping",
            description="Verify bridge connectivity and return the latest heartbeat information.",
            inputSchema=ping_schema,
        ),
        types.Tool(
            name="unity.scene.crud",
            description="Create, load, save, delete, or duplicate Unity scenes.",
            inputSchema=scene_manage_schema,
        ),
        types.Tool(
            name="unity.gameobject.crud",
            description="Modify the active scene hierarchy (create, delete, move, rename, duplicate) or inspect GameObjects. Use 'inspect' operation to read all attached components with their properties.",
            inputSchema=game_object_manage_schema,
        ),
        types.Tool(
            name="unity.component.crud",
            description="Add, remove, update, or inspect components on a GameObject.",
            inputSchema=component_manage_schema,
        ),
        types.Tool(
            name="unity.asset.crud",
            description="Create, update, rename, duplicate, delete, or inspect Assets/ files.",
            inputSchema=asset_manage_schema,
        ),
        types.Tool(
            name="unity.ugui.rectAdjust",
            description="Adjust a RectTransform using uGUI layout utilities.",
            inputSchema=ugui_rect_adjust_schema,
        ),
        types.Tool(
            name="unity.ugui.anchorManage",
            description="Manage RectTransform anchors: set custom values, apply presets (top-left, center, stretch, etc.), or convert between anchor-based and absolute positioning.",
            inputSchema=ugui_anchor_manage_schema,
        ),
        types.Tool(
            name="unity.ugui.manage",
            description="Unified UGUI management tool. Consolidates all UGUI operations: adjust RectTransform size (rectAdjust), set anchors (setAnchor/setAnchorPreset), convert positioning (convertToAnchored/convertToAbsolute), inspect RectTransform state (inspect), and update properties (updateRect).",
            inputSchema=ugui_manage_schema,
        ),
        types.Tool(
            name="unity.tagLayer.manage",
            description="Manage tags and layers in Unity. Set/get tags and layers on GameObjects (supports recursive layer setting for hierarchies). Add/remove tags and layers from the project. List all available tags and layers.",
            inputSchema=tag_layer_manage_schema,
        ),
        types.Tool(
            name="unity.script.outline",
            description="Produce a summary of a C# script, optionally including member signatures.",
            inputSchema=script_outline_schema,
        ),
        types.Tool(
            name="unity.script.create",
            description="Create Unity C# scripts with templates. Generate MonoBehaviour, ScriptableObject, Editor scripts, or plain C# classes/interfaces/structs with custom namespaces, methods, fields, and attributes. Supports auto-generation of common Unity methods (Start, Update, Awake, etc.).",
            inputSchema=script_create_schema,
        ),
        types.Tool(
            name="unity.prefab.crud",
            description="Manage Unity prefabs: create prefabs from GameObjects, update existing prefabs, inspect prefab assets, instantiate prefabs in scenes, unpack prefab instances, apply or revert instance overrides.",
            inputSchema=prefab_manage_schema,
        ),
        types.Tool(
            name="unity.projectSettings.crud",
            description="Read, write, or list Unity Project Settings. Supports PlayerSettings, QualitySettings, TimeSettings, PhysicsSettings, AudioSettings, and EditorSettings.",
            inputSchema=project_settings_manage_schema,
        ),
        types.Tool(
            name="unity.renderPipeline.manage",
            description="Manage Unity Render Pipeline. Inspect current pipeline (Built-in/URP/HDRP), change pipeline asset, read and update pipeline-specific settings.",
            inputSchema=render_pipeline_manage_schema,
        ),
        types.Tool(
            name="unity.inputSystem.manage",
            description="Manage Unity New Input System. Create Input Action assets, add action maps and actions, configure bindings, and inspect existing assets. Requires Input System package to be installed.",
            inputSchema=input_system_manage_schema,
        ),
        types.Tool(
            name="unity.batch.execute",
            description="Execute multiple Unity operations in a single batch. Allows sequential execution of any tool operations with optional error handling. Returns results for all operations including successes and failures.",
            inputSchema=batch_execute_schema,
        ),
        types.Tool(
            name="unity.tilemap.manage",
            description="Manage Unity Tilemap system. Create tilemaps with Grid parent, set/get/clear tiles at positions, fill rectangular areas with tiles, inspect tilemap information. Supports 2D grid-based tile placement for level design.",
            inputSchema=tilemap_manage_schema,
        ),
        types.Tool(
            name="unity.navmesh.manage",
            description="Manage Unity NavMesh navigation system. Bake/clear NavMesh, add NavMeshAgent components to GameObjects, set agent destinations, inspect NavMesh statistics, and update bake settings. Supports runtime pathfinding for AI characters.",
            inputSchema=navmesh_manage_schema,
        ),
        types.Tool(
            name="unity.project.compile",
            description="Compile Unity C# scripts. Refreshes the asset database and requests script compilation. Returns compilation status and any compilation errors. Use this after creating or modifying C# scripts to ensure they compile correctly.",
            inputSchema=compile_schema,
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

        if name == "unity.ping":
            _ensure_bridge_connected()
            heartbeat = bridge_manager.get_last_heartbeat()
            bridge_response = await bridge_manager.send_command("pingUnityEditor", {})
            payload = {
                "connected": True,
                "lastHeartbeatAt": heartbeat,
                "bridgeResponse": bridge_response,
            }
            return [types.TextContent(type="text", text=as_pretty_json(payload))]

        if name == "unity.scene.crud":
            return await _call_bridge_tool("sceneManage", args)

        if name == "unity.gameobject.crud":
            return await _call_bridge_tool("gameObjectManage", args)

        if name == "unity.component.crud":
            return await _call_bridge_tool("componentManage", args)

        if name == "unity.asset.crud":
            return await _call_bridge_tool("assetManage", args)

        if name == "unity.ugui.rectAdjust":
            return await _call_bridge_tool("uguiRectAdjust", args)

        if name == "unity.ugui.anchorManage":
            return await _call_bridge_tool("uguiAnchorManage", args)

        if name == "unity.ugui.manage":
            return await _call_bridge_tool("uguiManage", args)

        if name == "unity.tagLayer.manage":
            return await _call_bridge_tool("tagLayerManage", args)

        if name == "unity.script.outline":
            return await _call_bridge_tool("scriptOutline", args)

        if name == "unity.script.create":
            return await _call_bridge_tool("scriptCreate", args)

        if name == "unity.prefab.crud":
            return await _call_bridge_tool("prefabManage", args)

        if name == "unity.projectSettings.crud":
            return await _call_bridge_tool("projectSettingsManage", args)

        if name == "unity.renderPipeline.manage":
            return await _call_bridge_tool("renderPipelineManage", args)

        if name == "unity.inputSystem.manage":
            return await _call_bridge_tool("inputSystemManage", args)

        if name == "unity.batch.execute":
            return await _call_bridge_tool("batchExecute", args)

        if name == "unity.tilemap.manage":
            return await _call_bridge_tool("tilemapManage", args)

        if name == "unity.navmesh.manage":
            return await _call_bridge_tool("navmeshManage", args)

        if name == "unity.project.compile":
            return await _call_bridge_tool("projectCompile", args)

        raise RuntimeError(f"No handler registered for tool '{name}'.")
