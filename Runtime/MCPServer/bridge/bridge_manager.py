from __future__ import annotations

import asyncio
import contextlib
import json
import time
from dataclasses import dataclass
from typing import Any, Callable, Dict
from uuid import uuid4

from websockets.asyncio.client import ClientConnection
from websockets.exceptions import ConnectionClosed
from websockets.protocol import State as ConnectionState

from ..config.env import env
from ..logger import logger
from .messages import (
    BridgeCommandResultMessage,
    BridgeContextUpdateMessage,
    BridgeHelloMessage,
    BridgeHeartbeatMessage,
    BridgeNotificationMessage,
    ServerMessage,
    UnityContextPayload,
)

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
        self._pending_commands: Dict[str, PendingCommand] = {}
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

    async def _receive_loop(self, socket: WebSocketClientProtocol) -> None:
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
        else:
            logger.warning("Received unsupported bridge message: %s", message_type)

    async def _handle_hello(self, message: BridgeHelloMessage) -> None:
        token = message.get("token")
        socket = self._socket
        if env.bridge_token and token != env.bridge_token:
            logger.error("Bridge authentication failed: invalid token")
            if socket and _is_socket_open(socket):
                await socket.close(code=4401, reason="Invalid bridge token")
            return

        self._session_id = message.get("sessionId")
        logger.info(
            "Unity bridge authenticated (session=%s unityVersion=%s project=%s)",
            self._session_id,
            message.get("unityVersion"),
            message.get("projectName"),
        )
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

    def _emit(self, event: str, *args) -> None:
        for callback in list(self._listeners.get(event, [])):
            try:
                callback(*args)
            except Exception:  # pragma: no cover - defensive
                logger.exception("Bridge event handler failed for %s", event)

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
