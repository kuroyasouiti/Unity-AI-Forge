from __future__ import annotations

import asyncio
import contextlib
import json
import time
from collections.abc import Callable
from dataclasses import dataclass
from typing import Any
from uuid import uuid4

from websockets.asyncio.client import ClientConnection
from websockets.exceptions import ConnectionClosed
from websockets.protocol import State as ConnectionState

from bridge.messages import (
    BridgeCommandResultMessage,
    BridgeContextUpdateMessage,
    BridgeHeartbeatMessage,
    BridgeHelloMessage,
    BridgeNotificationMessage,
    BridgeRestartedMessage,
    ClientInfo,
    ServerInfoMessage,
    ServerMessage,
    UnityContextPayload,
)
from logger import logger
from utils.client_detector import get_client_info


@dataclass
class PendingCommand:
    tool_name: str
    future: asyncio.Future[Any]
    timeout_handle: asyncio.TimerHandle


class BridgeManager:
    def __init__(self) -> None:
        self._socket: ClientConnection | None = None
        self._session_id: str | None = None
        self._last_heartbeat_at: int | None = None
        self._context: UnityContextPayload | None = None
        self._pending_commands: dict[str, PendingCommand] = {}
        self._compilation_waiters: list[asyncio.Future[dict[str, Any]]] = []
        self._listeners: dict[str, list[Callable[..., None]]] = {
            "connected": [],
            "disconnected": [],
            "contextUpdated": [],
        }
        self._receive_task: asyncio.Task[None] | None = None
        self._send_lock = asyncio.Lock()

    async def attach(self, socket: ClientConnection) -> None:
        await self._teardown_socket()
        self._socket = socket
        self._last_heartbeat_at = int(time.time() * 1000)
        self._receive_task = asyncio.create_task(self._receive_loop(socket))

    def on(self, event: str, callback: Callable[..., None]) -> None:
        if event not in self._listeners:
            raise ValueError(f"Unsupported event: {event}")
        self._listeners[event].append(callback)

    def is_connected(self) -> bool:
        return _is_socket_open(self._socket)

    def get_session_id(self) -> str | None:
        return self._session_id

    def get_context(self) -> UnityContextPayload | None:
        return self._context

    def get_last_heartbeat(self) -> int | None:
        return self._last_heartbeat_at

    async def await_compilation(self, timeout_seconds: int = 60) -> dict[str, Any]:
        """
        Wait for the next compilation to complete.

        Args:
            timeout_seconds: Maximum time to wait in seconds (default: 60, increased from 30)

        Returns:
            Compilation result dictionary with keys:
            - success: bool
            - completed: bool
            - timedOut: bool
            - hasErrors: bool
            - hasWarnings: bool
            - errors: list of error messages
            - warnings: list of warning messages
            - errorCount: int
            - warningCount: int
            - elapsedSeconds: int
            - message: str

        Raises:
            RuntimeError: If bridge is not connected
            TimeoutError: If compilation does not complete within timeout

        Note:
            The default timeout has been increased to 60 seconds to accommodate
            large projects. If you receive compilation:progress messages every
            5 seconds, the compilation is still active and not stuck.
        """
        self._ensure_socket()
        loop = asyncio.get_running_loop()

        future: asyncio.Future[dict[str, Any]] = loop.create_future()
        self._compilation_waiters.append(future)

        def on_timeout() -> None:
            if not future.done():
                # Remove from waiters list
                with contextlib.suppress(ValueError):
                    self._compilation_waiters.remove(future)
                future.set_exception(
                    TimeoutError(
                        f"Compilation did not complete within {timeout_seconds} seconds. "
                        f"This may indicate a very large project or Unity being unresponsive. "
                        f"Check Unity Editor console for compilation status. "
                        f"Consider increasing timeout_seconds parameter for large projects."
                    )
                )

        timeout_handle = loop.call_later(timeout_seconds, on_timeout)

        try:
            return await future
        finally:
            timeout_handle.cancel()

    async def send_command(
        self,
        tool_name: str,
        payload: Any,
        timeout_ms: int = 30_000,
    ) -> Any:
        socket = self._ensure_socket()
        loop = asyncio.get_running_loop()

        command_id = uuid4().hex
        future: asyncio.Future[Any] = loop.create_future()

        def on_timeout() -> None:
            pending = self._pending_commands.pop(command_id, None)
            if pending and not pending.future.done():
                pending.future.set_exception(
                    TimeoutError(
                        f'Bridge command "{tool_name}" timed out after {timeout_ms}ms'
                    )
                )

        timeout_handle = loop.call_later(timeout_ms / 1000, on_timeout)
        self._pending_commands[command_id] = PendingCommand(
            tool_name=tool_name,
            future=future,
            timeout_handle=timeout_handle,
        )

        message: ServerMessage = {
            "type": "command:execute",
            "commandId": command_id,
            "toolName": tool_name,
            "payload": payload,
        }

        await self._send_json(socket, message)
        return await future

    async def send_ping(self) -> None:
        socket = self._socket
        if not _is_socket_open(socket):
            return

        message: ServerMessage = {
            "type": "ping",
            "timestamp": int(time.time() * 1000),
        }
        await self._send_json(socket, message)

    async def _send_json(self, socket: ClientConnection, message: ServerMessage) -> None:
        async with self._send_lock:
            try:
                await socket.send(json.dumps(message))
            except ConnectionClosed:
                await self._handle_disconnect(socket)
                raise RuntimeError("Unity bridge is not connected") from None

    async def _receive_loop(self, socket: ClientConnection) -> None:
        logger.info("Unity bridge socket listener started")
        try:
            async for raw in socket:
                try:
                    payload = json.loads(raw)
                except json.JSONDecodeError as exc:
                    logger.error("Failed to decode bridge message: %s", exc)
                    continue

                await self._handle_message(payload)
        except ConnectionClosed as exc:
            logger.warning(
                "Unity bridge connection closed (code=%s, reason=%s)",
                exc.code,
                exc.reason,
            )
        except Exception:  # pragma: no cover - defensive
            logger.exception("Unity bridge listener crashed")
        finally:
            await self._handle_disconnect(socket)

    async def _handle_message(self, message: BridgeNotificationMessage) -> None:
        message_type = message.get("type")
        if message_type == "hello":
            await self._handle_hello(message)
        elif message_type == "heartbeat":
            self._handle_heartbeat(message)
        elif message_type == "context:update":
            self._handle_context_update(message)
        elif message_type == "command:result":
            self._handle_command_result(message)
        elif message_type == "compilation:started":
            self._handle_compilation_started(message)
        elif message_type == "compilation:progress":
            self._handle_compilation_progress(message)
        elif message_type == "compilation:complete":
            self._handle_compilation_complete(message)
        elif message_type == "bridge:restarted":
            self._handle_bridge_restarted(message)
        else:
            logger.warning("Received unsupported bridge message: %s", message_type)

    async def _handle_hello(self, message: BridgeHelloMessage) -> None:
        self._session_id = message.get("sessionId")
        logger.info(
            "Unity bridge connected (session=%s unityVersion=%s project=%s)",
            self._session_id,
            message.get("unityVersion"),
            message.get("projectName"),
        )

        # Send client info to Unity
        await self._send_client_info()

        self._emit("connected")

    def _handle_heartbeat(self, message: BridgeHeartbeatMessage) -> None:
        self._last_heartbeat_at = message.get("timestamp")

    def _handle_context_update(self, message: BridgeContextUpdateMessage) -> None:
        payload = message.get("payload")
        if not payload:
            return
        self._context = payload
        self._emit("contextUpdated", payload)

    def _handle_command_result(self, message: BridgeCommandResultMessage) -> None:
        command_id = message.get("commandId")
        if not command_id:
            logger.warning("Received command result without commandId: %s", message)
            return

        pending = self._pending_commands.pop(command_id, None)
        if not pending:
            logger.warning("Received result for unknown command: %s", command_id)
            return

        pending.timeout_handle.cancel()

        if message.get("ok"):
            pending.future.set_result(message.get("result"))
        else:
            pending.future.set_exception(
                RuntimeError(
                    message.get("errorMessage")
                    or f'Bridge command "{pending.tool_name}" failed without message'
                )
            )

    def _handle_compilation_started(self, message: dict[str, Any]) -> None:
        """Handle compilation:started message from Unity bridge."""
        timestamp = message.get("timestamp", 0)
        logger.info("Compilation started at timestamp %d", timestamp)

    def _handle_compilation_progress(self, message: dict[str, Any]) -> None:
        """Handle compilation:progress message from Unity bridge."""
        elapsed = message.get("elapsedSeconds", 0)
        status = message.get("status", "compiling")
        logger.debug(
            "Compilation progress: status=%s, elapsed=%ds",
            status,
            elapsed,
        )

        # Reset timeout for all pending compilation waiters
        # This prevents timeout while compilation is actively progressing
        for future in self._compilation_waiters:
            if not future.done():
                # The mere fact that we received a progress update means compilation is still active
                # and not stuck, so we can be patient
                pass

    def _handle_compilation_complete(self, message: dict[str, Any]) -> None:
        """Handle compilation:complete message from Unity bridge."""
        result = message.get("result", {})
        elapsed = result.get("elapsedSeconds", 0)
        logger.info(
            "Compilation complete: success=%s, errors=%s, elapsed=%ds",
            result.get("success"),
            result.get("errorCount", 0),
            elapsed,
        )

        # Resolve all pending compilation waiters
        waiters = self._compilation_waiters[:]
        self._compilation_waiters.clear()

        for future in waiters:
            if not future.done():
                future.set_result(result)

    def _handle_bridge_restarted(self, message: BridgeRestartedMessage) -> None:
        """Handle bridge:restarted message from Unity bridge."""
        reason = message.get("reason", "unknown")
        session_id = message.get("sessionId")
        logger.info(
            "Unity bridge restarted (reason=%s, sessionId=%s)",
            reason,
            session_id,
        )
        # Update session ID if it changed
        if session_id:
            self._session_id = session_id

        # Resolve all pending compilation waiters with bridge restarted result
        # This is typically triggered after compilation completes and Unity reloads assemblies
        if self._compilation_waiters:
            logger.info(
                "Bridge restarted - resolving %d pending compilation waiter(s)",
                len(self._compilation_waiters),
            )

            result = {
                "success": True,
                "completed": True,
                "bridgeRestarted": True,
                "reason": reason,
                "message": f"Unity bridge restarted due to: {reason}",
            }

            waiters = self._compilation_waiters[:]
            self._compilation_waiters.clear()

            for future in waiters:
                if not future.done():
                    future.set_result(result)

    def _emit(self, event: str, *args) -> None:
        for callback in list(self._listeners.get(event, [])):
            try:
                callback(*args)
            except Exception:  # pragma: no cover - defensive
                logger.exception("Bridge event handler failed for %s", event)

    async def _send_client_info(self) -> None:
        """Send client information to Unity bridge."""
        socket = self._socket
        if not _is_socket_open(socket):
            return

        client_info: ClientInfo = get_client_info()
        message: ServerInfoMessage = {
            "type": "server:info",
            "clientInfo": client_info,
        }

        try:
            await self._send_json(socket, message)
            logger.info(
                "Sent client info to Unity: %s (server=%s v%s, python=%s, platform=%s)",
                client_info.get("clientName"),
                client_info.get("serverName"),
                client_info.get("serverVersion"),
                client_info.get("pythonVersion"),
                client_info.get("platform"),
            )
        except Exception as exc:
            logger.warning("Failed to send client info: %s", exc)

    async def _handle_disconnect(self, socket: ClientConnection) -> None:
        if self._socket is not socket:
            return

        logger.warning("Unity bridge disconnected")
        self._socket = None
        self._session_id = None
        self._last_heartbeat_at = None
        self._emit("disconnected")
        self._flush_pending_commands(RuntimeError("Bridge disconnected"))

    def _flush_pending_commands(self, error: Exception) -> None:
        for command_id, pending in list(self._pending_commands.items()):
            pending.timeout_handle.cancel()
            if not pending.future.done():
                pending.future.set_exception(error)
            self._pending_commands.pop(command_id, None)

    def _ensure_socket(self) -> ClientConnection:
        socket = self._socket
        if not _is_socket_open(socket):
            raise RuntimeError("Unity bridge is not connected")
        return socket

    async def _teardown_socket(self) -> None:
        if not self._socket:
            return

        socket = self._socket
        self._socket = None

        if self._receive_task:
            self._receive_task.cancel()
            with contextlib.suppress(asyncio.CancelledError):
                await self._receive_task
            self._receive_task = None

        if _is_socket_open(socket):
            await socket.close()

        self._flush_pending_commands(RuntimeError("Bridge reattached"))
        self._session_id = None


bridge_manager = BridgeManager()


def _is_socket_open(socket: ClientConnection | None) -> bool:
    return bool(socket and socket.state is not ConnectionState.CLOSED)
