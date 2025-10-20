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
        result = await self.list_handler(mcp_types.ListToolsRequest())
        tools_result = result.root
        self.assertIsInstance(tools_result, mcp_types.ListToolsResult)
        names = [tool.name for tool in tools_result.tools]
        self.assertEqual(
            names,
            [
                "unity.ping",
                "unity.scene.crud",
                "unity_gameobject_crud",
                "unity.component.crud",
                "unity.asset.crud",
                "unity.ugui.rectAdjust",
                "unity.script.outline",
            ],
        )

    async def test_call_tool_ping_reports_bridge_status(self) -> None:
        request = mcp_types.CallToolRequest(
            params=mcp_types.CallToolRequestParams(name="unity.ping", arguments={})
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
            ("unity.scene.crud", "sceneCrud", {"operation": "create"}),
            (
                "unity_gameobject_crud",
                "gameObjectCrud",
                {"operation": "create", "gameObjectPath": "Root"},
            ),
            (
                "unity.component.crud",
                "componentCrud",
                {
                    "operation": "add",
                    "gameObjectPath": "Root/Button",
                    "componentType": "UnityEngine.UI.Text",
                },
            ),
            (
                "unity.asset.crud",
                "assetCrud",
                {"operation": "create", "assetPath": "Assets/Example.prefab"},
            ),
            (
                "unity.ugui.rectAdjust",
                "uguiRectAdjust",
                {"gameObjectPath": "Canvas/Button"},
            ),
            (
                "unity.script.outline",
                "scriptOutline",
                {"assetPath": "Assets/Scripts/Foo.cs"},
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
