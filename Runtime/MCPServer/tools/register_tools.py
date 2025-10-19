from __future__ import annotations

from typing import Any, Dict

import mcp.types as types
from mcp.server import Server

from ..bridge.bridge_manager import bridge_manager
from ..utils.json_utils import as_pretty_json


def _ensure_bridge_connected() -> None:
    if not bridge_manager.is_connected():
        raise RuntimeError(
            "Unityブリッジが切断されています。Unity Editorの `Tools/MCP Assistant` ウィンドウで接続状態を確認してください。"
        )


async def _call_bridge_tool(tool_name: str, payload: Dict[str, Any]) -> list[types.Content]:
    _ensure_bridge_connected()
    try:
        response = await bridge_manager.send_command(tool_name, payload)
    except Exception as exc:
        raise RuntimeError(
            f'Unityブリッジでツール "{tool_name}" の実行に失敗しました: {exc}'
        ) from exc

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
                    "description": "実行する操作",
                },
                "scenePath": {
                    "type": "string",
                    "description": "Assets/ からの相対パス",
                },
                "newSceneName": {"type": "string"},
                "additive": {"type": "boolean"},
                "includeOpenScenes": {"type": "boolean"},
            },
        },
        ["operation"],
    )

    hierarchy_crud_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "delete", "move", "rename", "duplicate"],
                    "description": "実行する操作",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "シーン内のGameObjectパス (例: Root/Child/Button)",
                },
                "parentPath": {
                    "type": "string",
                    "description": "移動先または新規作成先のGameObjectパス",
                },
                "template": {
                    "type": "string",
                    "description": "プレハブパスまたはテンプレートID",
                },
                "name": {
                    "type": "string",
                    "description": "新規作成または複製時のGameObject名。省略時は自動決定。",
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
                    "description": "実行する操作",
                },
                "gameObjectPath": {"type": "string"},
                "componentType": {
                    "type": "string",
                    "description": "例: UnityEngine.UI.Text, CustomNamespace.PlayerController",
                },
                "propertyChanges": {
                    "type": "object",
                    "additionalProperties": True,
                    "description": "更新するプロパティ名と値のペア",
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
                },
                "assetPath": {
                    "type": "string",
                    "description": "Assets/ からの相対パス",
                },
                "destinationPath": {"type": "string"},
                "contents": {
                    "type": "string",
                    "description": "テキストアセットを作成/更新する際の内容",
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
                    "description": "対象となるGameObjectのパス",
                },
                "referenceResolution": {
                    "type": "array",
                    "items": {"type": "number"},
                    "minItems": 2,
                    "maxItems": 2,
                    "description": "CanvasScalerの参照解像度 (幅, 高さ)",
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
                "description": "Assets/ からのパス。guidとどちらか片方が必要。",
            },
            "includeMembers": {
                "type": "boolean",
                "description": "メンバーごとの詳細を含めるかどうか",
            },
        },
        "required": [],
        "additionalProperties": False,
    }

    tool_definitions = [
        types.Tool(
            name="unity.ping",
            description="Unity Editorとの接続状態を確認し、最新のハートビートを報告します。",
            inputSchema=ping_schema,
        ),
        types.Tool(
            name="unity.scene.crud",
            description="シーンの作成・読み込み・保存・削除・複製を実行します。",
            inputSchema=scene_crud_schema,
        ),
        types.Tool(
            name="unity.hierarchy.crud",
            description="ゲームオブジェクトの作成・削除・移動・リネーム・複製を行います。",
            inputSchema=hierarchy_crud_schema,
        ),
        types.Tool(
            name="unity.component.crud",
            description="GameObjectのコンポーネントを追加・削除・更新します。",
            inputSchema=component_crud_schema,
        ),
        types.Tool(
            name="unity.asset.crud",
            description="アセットの作成・更新・削除・リネーム・複製を行います。",
            inputSchema=asset_crud_schema,
        ),
        types.Tool(
            name="unity.ugui.rectAdjust",
            description="UGUIのRectTransformを参照解像度に合わせて調整します。",
            inputSchema=ugui_rect_adjust_schema,
        ),
        types.Tool(
            name="unity.script.outline",
            description="C#スクリプトのクラス/メソッド構造を抽出します。",
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
            raise RuntimeError(f"未知のツールです: {name}")

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

        if name == "unity.hierarchy.crud":
            return await _call_bridge_tool("hierarchyCrud", args)

        if name == "unity.component.crud":
            return await _call_bridge_tool("componentCrud", args)

        if name == "unity.asset.crud":
            return await _call_bridge_tool("assetCrud", args)

        if name == "unity.ugui.rectAdjust":
            return await _call_bridge_tool("uguiRectAdjust", args)

        if name == "unity.script.outline":
            return await _call_bridge_tool("scriptOutline", args)

        raise RuntimeError(f"ツール '{name}' の処理が実装されていません。")
