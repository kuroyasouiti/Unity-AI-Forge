"""Tests for bridge/bridge_manager.py module."""

from __future__ import annotations

import asyncio
import json
from typing import Any
from unittest.mock import AsyncMock, MagicMock, patch

import pytest
from websockets.protocol import State as ConnectionState


class TestBridgeManager:
    """Tests for BridgeManager class."""

    def test_init(self) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()

        assert manager._socket is None
        assert manager._session_id is None
        assert manager._last_heartbeat_at is None
        assert manager._context is None
        assert manager._pending_commands == {}

    def test_is_connected_false_when_no_socket(self) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()
        assert manager.is_connected() is False

    def test_is_connected_true_when_socket_open(
        self, mock_websocket: MagicMock
    ) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()
        manager._socket = mock_websocket

        assert manager.is_connected() is True

    def test_is_connected_false_when_socket_closed(
        self, mock_websocket: MagicMock
    ) -> None:
        from bridge.bridge_manager import BridgeManager

        mock_websocket.state = ConnectionState.CLOSED
        manager = BridgeManager()
        manager._socket = mock_websocket

        assert manager.is_connected() is False

    def test_get_session_id(self) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()
        assert manager.get_session_id() is None

        manager._session_id = "test-session"
        assert manager.get_session_id() == "test-session"

    def test_get_context(self) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()
        assert manager.get_context() is None

        manager._context = {"activeScene": {"name": "TestScene"}}  # type: ignore
        assert manager.get_context() == {"activeScene": {"name": "TestScene"}}

    def test_get_last_heartbeat(self) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()
        assert manager.get_last_heartbeat() is None

        manager._last_heartbeat_at = 1234567890000
        assert manager.get_last_heartbeat() == 1234567890000

    def test_on_event_valid(self) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()
        callback = MagicMock()

        manager.on("connected", callback)

        assert callback in manager._listeners["connected"]

    def test_on_event_invalid_raises(self) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()

        with pytest.raises(ValueError, match="Unsupported event"):
            manager.on("invalid_event", MagicMock())

    def test_emit_calls_callbacks(self) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()
        callback1 = MagicMock()
        callback2 = MagicMock()

        manager.on("connected", callback1)
        manager.on("connected", callback2)

        manager._emit("connected")

        callback1.assert_called_once()
        callback2.assert_called_once()

    def test_emit_handles_callback_exception(self) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()
        callback1 = MagicMock(side_effect=RuntimeError("Callback error"))
        callback2 = MagicMock()

        manager.on("connected", callback1)
        manager.on("connected", callback2)

        # Should not raise, second callback should still be called
        manager._emit("connected")

        callback1.assert_called_once()
        callback2.assert_called_once()


class TestBridgeManagerAsync:
    """Async tests for BridgeManager."""

    @pytest.mark.asyncio
    async def test_attach_socket(self, mock_websocket: MagicMock) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()

        # Mock the receive loop to avoid hanging
        with patch.object(manager, "_receive_loop", new_callable=AsyncMock):
            await manager.attach(mock_websocket)

        assert manager._socket is mock_websocket
        assert manager._last_heartbeat_at is not None

    @pytest.mark.asyncio
    async def test_send_command_raises_when_not_connected(self) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()

        with pytest.raises(RuntimeError, match="Unity bridge is not connected"):
            await manager.send_command("test_tool", {})

    @pytest.mark.asyncio
    async def test_send_command_success(self, mock_websocket: MagicMock) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()
        manager._socket = mock_websocket

        # Create a task that will resolve the pending command
        async def resolve_command():
            await asyncio.sleep(0.01)
            # Find the pending command and resolve it
            for cmd_id, pending in list(manager._pending_commands.items()):
                pending.timeout_handle.cancel()
                pending.future.set_result({"success": True})
                break

        asyncio.create_task(resolve_command())

        result = await manager.send_command("test_tool", {"key": "value"}, timeout_ms=1000)

        assert result == {"success": True}
        mock_websocket.send.assert_called_once()

    @pytest.mark.asyncio
    async def test_send_command_timeout(self, mock_websocket: MagicMock) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()
        manager._socket = mock_websocket

        with pytest.raises(TimeoutError, match="timed out"):
            await manager.send_command("test_tool", {}, timeout_ms=10)

    @pytest.mark.asyncio
    async def test_send_ping(self, mock_websocket: MagicMock) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()
        manager._socket = mock_websocket

        await manager.send_ping()

        mock_websocket.send.assert_called_once()
        sent_data = json.loads(mock_websocket.send.call_args[0][0])
        assert sent_data["type"] == "ping"
        assert "timestamp" in sent_data

    @pytest.mark.asyncio
    async def test_send_ping_no_op_when_not_connected(self) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()

        # Should not raise
        await manager.send_ping()

    @pytest.mark.asyncio
    async def test_await_compilation_raises_when_not_connected(self) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()

        with pytest.raises(RuntimeError, match="Unity bridge is not connected"):
            await manager.await_compilation(timeout_seconds=1)

    @pytest.mark.asyncio
    async def test_await_compilation_timeout(
        self, mock_websocket: MagicMock
    ) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()
        manager._socket = mock_websocket

        with pytest.raises(TimeoutError, match="Compilation did not complete"):
            await manager.await_compilation(timeout_seconds=0.01)


class TestBridgeManagerMessageHandling:
    """Tests for message handling in BridgeManager."""

    @pytest.mark.asyncio
    async def test_handle_hello(self, mock_websocket: MagicMock) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()
        manager._socket = mock_websocket

        connected_callback = MagicMock()
        manager.on("connected", connected_callback)

        message = {
            "type": "hello",
            "sessionId": "test-session-123",
            "unityVersion": "2022.3.0",
            "projectName": "TestProject",
        }

        await manager._handle_message(message)

        assert manager._session_id == "test-session-123"
        connected_callback.assert_called_once()

    def test_handle_heartbeat(self) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()

        message = {"type": "heartbeat", "timestamp": 1234567890000}

        manager._handle_heartbeat(message)

        assert manager._last_heartbeat_at == 1234567890000

    def test_handle_context_update(self) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()

        context_callback = MagicMock()
        manager.on("contextUpdated", context_callback)

        message = {
            "type": "context:update",
            "payload": {"activeScene": {"name": "TestScene"}, "updatedAt": 1234567890},
        }

        manager._handle_context_update(message)

        assert manager._context is not None
        assert manager._context["activeScene"]["name"] == "TestScene"
        context_callback.assert_called_once()

    def test_handle_command_result_success(self) -> None:
        from bridge.bridge_manager import BridgeManager, PendingCommand

        manager = BridgeManager()
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)

        try:
            future: asyncio.Future[Any] = loop.create_future()
            timeout_handle = MagicMock()

            manager._pending_commands["cmd-123"] = PendingCommand(
                tool_name="test_tool",
                future=future,
                timeout_handle=timeout_handle,
            )

            message = {
                "type": "command:result",
                "commandId": "cmd-123",
                "ok": True,
                "result": {"data": "test"},
            }

            manager._handle_command_result(message)

            timeout_handle.cancel.assert_called_once()
            assert future.done()
            assert future.result() == {"data": "test"}
            assert "cmd-123" not in manager._pending_commands
        finally:
            loop.close()

    def test_handle_command_result_failure(self) -> None:
        from bridge.bridge_manager import BridgeManager, PendingCommand

        manager = BridgeManager()
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)

        try:
            future: asyncio.Future[Any] = loop.create_future()
            timeout_handle = MagicMock()

            manager._pending_commands["cmd-456"] = PendingCommand(
                tool_name="test_tool",
                future=future,
                timeout_handle=timeout_handle,
            )

            message = {
                "type": "command:result",
                "commandId": "cmd-456",
                "ok": False,
                "errorMessage": "Operation failed",
            }

            manager._handle_command_result(message)

            timeout_handle.cancel.assert_called_once()
            assert future.done()

            with pytest.raises(RuntimeError, match="Operation failed"):
                future.result()
        finally:
            loop.close()

    def test_handle_compilation_complete(self) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)

        try:
            future: asyncio.Future[dict[str, Any]] = loop.create_future()
            manager._compilation_waiters.append(future)

            message = {
                "type": "compilation:complete",
                "result": {
                    "success": True,
                    "errorCount": 0,
                    "elapsedSeconds": 5,
                },
            }

            manager._handle_compilation_complete(message)

            assert len(manager._compilation_waiters) == 0
            assert future.done()
            result = future.result()
            assert result["success"] is True
        finally:
            loop.close()

    def test_handle_bridge_restarted(self) -> None:
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)

        try:
            future: asyncio.Future[dict[str, Any]] = loop.create_future()
            manager._compilation_waiters.append(future)
            manager._session_id = "old-session"

            message = {
                "type": "bridge:restarted",
                "timestamp": 1234567890,
                "reason": "assembly_reload",
                "sessionId": "new-session-789",
            }

            manager._handle_bridge_restarted(message)

            assert manager._session_id == "new-session-789"
            assert len(manager._compilation_waiters) == 0
            assert future.done()
            result = future.result()
            assert result["bridgeRestarted"] is True
        finally:
            loop.close()
