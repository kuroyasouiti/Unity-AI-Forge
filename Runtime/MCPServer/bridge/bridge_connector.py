from __future__ import annotations

import asyncio
from contextlib import suppress

import websockets
from websockets.asyncio.client import ClientConnection
from websockets.protocol import State as ConnectionState

from ..config.env import env
from ..logger import logger
from .bridge_manager import bridge_manager


class BridgeConnector:
    def __init__(self) -> None:
        self._task: asyncio.Task[None] | None = None
        self._stop_event = asyncio.Event()
        self._intentional_close = False

    def start(self) -> None:
        if self._task:
            return
        self._intentional_close = False
        self._stop_event.clear()
        self._task = asyncio.create_task(self._run())

    async def stop(self) -> None:
        self._intentional_close = True
        self._stop_event.set()
        if not self._task:
            return

        self._task.cancel()
        with suppress(asyncio.CancelledError):
            await self._task
        self._task = None

    async def _run(self) -> None:
        delay_seconds = 0.0
        try:
            while not self._stop_event.is_set():
                if delay_seconds > 0:
                    logger.debug("Waiting %.2fs before reconnecting Unity bridge", delay_seconds)
                    try:
                        await asyncio.wait_for(self._stop_event.wait(), delay_seconds)
                        break
                    except asyncio.TimeoutError:
                        pass

                try:
                    await self._connect_once()
                    delay_seconds = env.bridge_reconnect_ms / 1000
                except Exception as exc:  # pragma: no cover - defensive
                    logger.error("Unity bridge connection attempt failed: %s", exc)
                    delay_seconds = max(1.0, env.bridge_reconnect_ms / 1000)
        finally:
            self._task = None

    async def _connect_once(self) -> None:
        url = f"ws://{env.unity_bridge_host}:{env.unity_bridge_port}/bridge"
        logger.info("Attempting connection to Unity bridge at %s", url)

        try:
            async with websockets.connect(url, open_timeout=10) as socket:
                await bridge_manager.attach(socket)
                logger.info("Connected to Unity bridge")
                await self._monitor_connection(socket)
        except Exception as exc:
            logger.warning("Unity bridge connection error: %s", exc)
            raise

    async def _monitor_connection(self, socket: ClientConnection) -> None:
        ping_interval = max(5.0, env.bridge_reconnect_ms / 1000)

        async def ping_loop() -> None:
            while not self._stop_event.is_set():
                await asyncio.sleep(ping_interval)
                try:
                    await bridge_manager.send_ping()
                except Exception as exc:  # pragma: no cover - defensive
                    logger.warning("Unity bridge ping failed: %s", exc)
                    return

        ping_task = asyncio.create_task(ping_loop())
        stop_task = asyncio.create_task(self._stop_event.wait())
        wait_task = asyncio.create_task(socket.wait_closed())

        done, pending = await asyncio.wait(
            {ping_task, stop_task, wait_task},
            return_when=asyncio.FIRST_COMPLETED,
        )

        for task in pending:
            task.cancel()

        if stop_task in done and _is_socket_open(socket):
            with suppress(Exception):
                await socket.close(code=1000, reason="shutdown")

        for task in pending:
            with suppress(asyncio.CancelledError):
                await task

        if ping_task in done and ping_task.exception():
            logger.debug("Unity bridge ping task finished with: %s", ping_task.exception())

        if wait_task in done and wait_task.exception():
            logger.debug("Unity bridge wait_closed exception: %s", wait_task.exception())

        if stop_task in done:
            logger.info("Unity bridge connector stopping on request")

        if (
            wait_task in done
            and not self._intentional_close
            and not self._stop_event.is_set()
        ):
            logger.warning("Unity bridge connection closed unexpectedly")


bridge_connector = BridgeConnector()


def _is_socket_open(socket: ClientConnection) -> bool:
    return socket.state is not ConnectionState.CLOSED
