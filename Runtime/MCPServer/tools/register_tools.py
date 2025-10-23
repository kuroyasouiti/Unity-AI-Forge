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
            name="unity.script.outline",
            description="Produce a summary of a C# script, optionally including member signatures.",
            inputSchema=script_outline_schema,
        ),
        types.Tool(
            name="unity.prefab.crud",
            description="Manage Unity prefabs: create prefabs from GameObjects, update existing prefabs, inspect prefab assets, instantiate prefabs in scenes, unpack prefab instances, apply or revert instance overrides.",
            inputSchema=prefab_manage_schema,
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

        if name == "unity.script.outline":
            return await _call_bridge_tool("scriptOutline", args)

        if name == "unity.prefab.crud":
            return await _call_bridge_tool("prefabManage", args)

        raise RuntimeError(f"No handler registered for tool '{name}'.")
