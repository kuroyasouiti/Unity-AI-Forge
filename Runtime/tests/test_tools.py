import unittest
from unittest.mock import AsyncMock, patch

from mcp.server import Server
import mcp.types as mcp_types

from MCPServer.tools import register_tools as tools_module


class RegisterToolsTests(unittest.IsolatedAsyncioTestCase):
    def setUp(self) -> None:
        self.server = Server("test", version="test")
        tools_module.register_tools(self.server)
        self.list_handler = self.server.request_handlers[mcp_types.ListToolsRequest]
        self.call_handler = self.server.request_handlers[mcp_types.CallToolRequest]

    async def test_list_tools_returns_expected_definitions(self) -> None:
        result = await self.list_handler(
            mcp_types.ListToolsRequest(method="tools/list")
        )
        tools_result = result.root
        self.assertIsInstance(tools_result, mcp_types.ListToolsResult)
        names = [tool.name for tool in tools_result.tools]
        expected_core = {
            "unity.ping",
            "unity.scene.crud",
            "unity.gameobject.crud",
            "unity.component.crud",
            "unity.asset.crud",
            "unity.ugui.rectAdjust",
            "unity.ugui.anchorManage",
            "unity.script.manage",
            "unity.prefab.crud",
        }
        self.assertTrue(expected_core.issubset(set(names)))

    async def test_call_tool_ping_reports_bridge_status(self) -> None:
        request = mcp_types.CallToolRequest(
            method="tools/call",
            params=mcp_types.CallToolRequestParams(name="unity.ping", arguments={}),
        )
        with patch.object(
            tools_module.bridge_manager, "is_connected", return_value=True
        ), patch.object(
            tools_module.bridge_manager, "get_last_heartbeat", return_value=1234567890
        ), patch.object(
            tools_module.bridge_manager,
            "send_command",
            new=AsyncMock(return_value={"ok": True}),
        ) as send_command:
            result = await self.call_handler(request)

        send_command.assert_awaited_once_with("pingUnityEditor", {})
        ping_result = result.root
        self.assertIsInstance(ping_result, mcp_types.CallToolResult)
        self.assertFalse(ping_result.isError)
        content_text = ping_result.content[0].text
        self.assertIn('"connected": true', content_text)
        self.assertIn('"bridgeResponse"', content_text)

    async def test_call_tool_routes_commands_through_bridge(self) -> None:
        tool_cases = [
            ("unity.scene.crud", "sceneManage", {"operation": "create"}),
            (
                "unity.gameobject.crud",
                "gameObjectManage",
                {"operation": "create", "gameObjectPath": "Root"},
            ),
            (
                "unity.component.crud",
                "componentManage",
                {
                    "operation": "add",
                    "gameObjectPath": "Root/Button",
                    "componentType": "UnityEngine.UI.Text",
                },
            ),
            (
                "unity.component.crud",
                "componentManage",
                {
                    "operation": "inspect",
                    "gameObjectPath": "Root/Button",
                    "componentType": "UnityEngine.UI.Text",
                },
            ),
            (
                "unity.gameobject.crud",
                "gameObjectManage",
                {
                    "operation": "inspect",
                    "gameObjectPath": "Root/Button",
                },
            ),
            (
                "unity.asset.crud",
                "assetManage",
                {"operation": "create", "assetPath": "Assets/Example.prefab"},
            ),
            (
                "unity.asset.crud",
                "assetManage",
                {"operation": "inspect", "assetPath": "Assets/Example.prefab"},
            ),
            (
                "unity.ugui.rectAdjust",
                "uguiRectAdjust",
                {"gameObjectPath": "Canvas/Button"},
            ),
            (
                "unity.ugui.anchorManage",
                "uguiAnchorManage",
                {"gameObjectPath": "Canvas/Button", "operation": "setAnchorPreset", "preset": "center"},
            ),
            (
                "unity.script.manage",
                "scriptManage",
                {"operation": "read", "assetPath": "Assets/Scripts/Foo.cs", "includeSource": True},
            ),
            (
                "unity.script.manage",
                "scriptManage",
                {"operation": "create", "scriptPath": "Assets/Scripts/Foo.cs", "scriptType": "monoBehaviour"},
            ),
            (
                "unity.script.manage",
                "scriptManage",
                {
                    "operation": "update",
                    "scriptPath": "Assets/Scripts/Foo.cs",
                    "edits": [{"action": "replace", "match": "foo", "replacement": "bar"}],
                },
            ),
            (
                "unity.script.manage",
                "scriptManage",
                {"operation": "delete", "scriptPath": "Assets/Scripts/Foo.cs", "dryRun": True},
            ),
            (
                "unity.prefab.crud",
                "prefabManage",
                {"operation": "create", "gameObjectPath": "Player", "prefabPath": "Assets/Prefabs/Player.prefab"},
            ),
            (
                "unity.prefab.crud",
                "prefabManage",
                {"operation": "instantiate", "prefabPath": "Assets/Prefabs/Player.prefab"},
            ),
        ]

        with patch.object(
            tools_module.bridge_manager, "is_connected", return_value=True
        ):
            for tool_name, bridge_command, arguments in tool_cases:
                send_command = AsyncMock(return_value={"route": bridge_command})
                with patch.object(
                    tools_module.bridge_manager, "send_command", send_command
                ):
                    request = mcp_types.CallToolRequest(
                        method="tools/call",
                        params=mcp_types.CallToolRequestParams(
                            name=tool_name, arguments=arguments
                        )
                    )
                    result = await self.call_handler(request)

                send_command.assert_awaited_once_with(bridge_command, arguments)
                call_result = result.root
                self.assertIsInstance(call_result, mcp_types.CallToolResult)
                self.assertFalse(call_result.isError)
                self.assertIn(bridge_command, call_result.content[0].text)

    async def test_call_tool_returns_error_when_bridge_disconnected(self) -> None:
        request = mcp_types.CallToolRequest(
            method="tools/call",
            params=mcp_types.CallToolRequestParams(
                name="unity.scene.crud", arguments={"operation": "create"}
            )
        )
        with patch.object(
            tools_module.bridge_manager, "is_connected", return_value=False
        ):
            result = await self.call_handler(request)

        call_result = result.root
        self.assertTrue(call_result.isError)
        self.assertIn("Unity", call_result.content[0].text)


if __name__ == "__main__":
    unittest.main()
