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

    scene_crud_schema = _schema_with_required(
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

    game_object_crud_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "delete", "move", "rename", "duplicate"],
                    "description": "Operation to perform.",
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

    component_crud_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["add", "remove", "update"],
                    "description": "Operation to perform.",
                },
                "gameObjectPath": {"type": "string"},
                "componentType": {
                    "type": "string",
                    "description": "Fully qualified component type (e.g. UnityEngine.UI.Text).",
                },
                "propertyChanges": {
                    "type": "object",
                    "additionalProperties": True,
                    "description": "Property/value pairs to apply to the component.",
                },
                "applyDefaults": {"type": "boolean"},
            },
        },
        ["operation", "gameObjectPath", "componentType"],
    )

    asset_crud_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "delete", "rename", "duplicate"],
                    "description": "Operation to perform.",
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

    tool_definitions = [
        types.Tool(
            name="unity.ping",
            description="Verify bridge connectivity and return the latest heartbeat information.",
            inputSchema=ping_schema,
        ),
        types.Tool(
            name="unity.scene.crud",
            description="Create, load, save, delete, or duplicate Unity scenes.",
            inputSchema=scene_crud_schema,
        ),
        types.Tool(
            name="unity_gameobject_crud",
            description="Modify the active scene hierarchy (create, delete, move, rename, duplicate).",
            inputSchema=game_object_crud_schema,
        ),
        types.Tool(
            name="unity.component.crud",
            description="Add, remove, or update components on a GameObject.",
            inputSchema=component_crud_schema,
        ),
        types.Tool(
            name="unity.asset.crud",
            description="Create, update, rename, duplicate, or delete Assets/ files.",
            inputSchema=asset_crud_schema,
        ),
        types.Tool(
            name="unity.ugui.rectAdjust",
            description="Adjust a RectTransform using uGUI layout utilities.",
            inputSchema=ugui_rect_adjust_schema,
        ),
        types.Tool(
            name="unity.script.outline",
            description="Produce a summary of a C# script, optionally including member signatures.",
            inputSchema=script_outline_schema,
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
            return await _call_bridge_tool("sceneCrud", args)

        if name == "unity_gameobject_crud":
            return await _call_bridge_tool("gameObjectCrud", args)

        if name == "unity.component.crud":
            return await _call_bridge_tool("componentCrud", args)

        if name == "unity.asset.crud":
            return await _call_bridge_tool("assetCrud", args)

        if name == "unity.ugui.rectAdjust":
            return await _call_bridge_tool("uguiRectAdjust", args)

        if name == "unity.script.outline":
            return await _call_bridge_tool("scriptOutline", args)

        raise RuntimeError(f"No handler registered for tool '{name}'.")
