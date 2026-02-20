"""Tests for tools/register_tools.py module."""

from __future__ import annotations

from unittest.mock import AsyncMock, MagicMock, patch

import pytest

from tools.register_tools import _call_bridge_tool, _ensure_bridge_connected


class TestEnsureBridgeConnected:
    """Tests for _ensure_bridge_connected helper."""

    def test_connected_does_not_raise(self) -> None:
        with patch("tools.register_tools.bridge_manager") as mock_bm:
            mock_bm.is_connected.return_value = True
            _ensure_bridge_connected()  # should not raise

    def test_disconnected_raises_runtime_error(self) -> None:
        with patch("tools.register_tools.bridge_manager") as mock_bm:
            mock_bm.is_connected.return_value = False
            with pytest.raises(RuntimeError, match="Unity bridge is not connected"):
                _ensure_bridge_connected()


class TestCallBridgeTool:
    """Tests for _call_bridge_tool helper."""

    async def test_returns_text_content(self) -> None:
        with patch("tools.register_tools.bridge_manager") as mock_bm:
            mock_bm.is_connected.return_value = True
            mock_bm.send_command = AsyncMock(return_value={"success": True})
            result = await _call_bridge_tool("testTool", {"operation": "inspect"})
            assert len(result) == 1
            assert result[0].type == "text"

    async def test_passes_payload_to_bridge(self) -> None:
        with patch("tools.register_tools.bridge_manager") as mock_bm:
            mock_bm.is_connected.return_value = True
            mock_bm.send_command = AsyncMock(return_value={"ok": True})
            payload = {"operation": "create", "name": "Test"}
            await _call_bridge_tool("gameObjectManage", payload)
            mock_bm.send_command.assert_called_once_with(
                "gameObjectManage", payload, timeout_ms=45_000
            )

    async def test_custom_timeout_from_payload(self) -> None:
        with patch("tools.register_tools.bridge_manager") as mock_bm:
            mock_bm.is_connected.return_value = True
            mock_bm.send_command = AsyncMock(return_value={})
            payload = {"timeoutSeconds": 30}
            await _call_bridge_tool("testTool", payload)
            # timeout should be (30 + 20) * 1000 = 50000
            mock_bm.send_command.assert_called_once_with(
                "testTool", payload, timeout_ms=50_000
            )

    async def test_bridge_error_raises_runtime_error(self) -> None:
        with patch("tools.register_tools.bridge_manager") as mock_bm:
            mock_bm.is_connected.return_value = True
            mock_bm.send_command = AsyncMock(side_effect=Exception("connection lost"))
            with pytest.raises(RuntimeError, match="failed"):
                await _call_bridge_tool("testTool", {})

    async def test_disconnected_raises(self) -> None:
        with patch("tools.register_tools.bridge_manager") as mock_bm:
            mock_bm.is_connected.return_value = False
            with pytest.raises(RuntimeError, match="Unity bridge is not connected"):
                await _call_bridge_tool("testTool", {})


class TestRegisterTools:
    """Tests for register_tools function and tool dispatch."""

    def test_register_tools_sets_up_handlers(self) -> None:
        from tools.register_tools import register_tools

        server = MagicMock()
        register_tools(server)
        server.list_tools.assert_called_once()
        server.call_tool.assert_called_once()

    async def test_call_tool_unknown_raises(self) -> None:
        from tools.register_tools import register_tools

        server = MagicMock()
        call_handler = None

        def capture_call_tool():
            def decorator(func):
                nonlocal call_handler
                call_handler = func
                return func
            return decorator

        server.call_tool = capture_call_tool
        server.list_tools = lambda: lambda f: f
        register_tools(server)

        assert call_handler is not None
        with pytest.raises(RuntimeError, match="Unknown tool"):
            await call_handler("totally_fake_tool", {})

    async def test_call_tool_ping_dispatches_special(self) -> None:
        from tools.register_tools import register_tools

        server = MagicMock()
        call_handler = None

        def capture_call_tool():
            def decorator(func):
                nonlocal call_handler
                call_handler = func
                return func
            return decorator

        server.call_tool = capture_call_tool
        server.list_tools = lambda: lambda f: f
        register_tools(server)

        with patch("tools.register_tools.bridge_manager") as mock_bm:
            mock_bm.is_connected.return_value = True
            mock_bm.get_last_heartbeat.return_value = 123456
            mock_bm.send_command = AsyncMock(return_value={"pong": True})
            result = await call_handler("unity_ping", {})
            assert len(result) == 1
            assert "connected" in result[0].text

    async def test_call_tool_standard_bridge_dispatch(self) -> None:
        from tools.register_tools import register_tools

        server = MagicMock()
        call_handler = None

        def capture_call_tool():
            def decorator(func):
                nonlocal call_handler
                call_handler = func
                return func
            return decorator

        server.call_tool = capture_call_tool
        server.list_tools = lambda: lambda f: f
        register_tools(server)

        with patch("tools.register_tools.bridge_manager") as mock_bm:
            mock_bm.is_connected.return_value = True
            mock_bm.send_command = AsyncMock(return_value={"success": True})
            result = await call_handler("unity_scene_crud", {"operation": "inspect"})
            assert len(result) == 1
            mock_bm.send_command.assert_called_once_with(
                "sceneManage", {"operation": "inspect"}, timeout_ms=45_000
            )
