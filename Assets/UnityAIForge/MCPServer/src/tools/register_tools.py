"""Register all MCP tools with the server.

This module wires up ``list_tools`` and ``call_tool`` handlers.  Schema
definitions live in ``tools.schemas``, the 64 tool entries in
``tools.tool_definitions``, and the name→bridge mapping in
``tools.tool_registry``.
"""

from __future__ import annotations

import asyncio
import time
from typing import Any

import mcp.types as types
from mcp.server import Server

from bridge.bridge_manager import bridge_manager
from logger import logger
from tools.batch_sequential import handle_batch_sequential
from tools.tool_definitions import get_tool_definitions
from tools.tool_registry import TOOL_NAME_TO_BRIDGE
from utils.json_utils import as_pretty_json


def _ensure_bridge_connected() -> None:
    if not bridge_manager.is_connected():
        raise RuntimeError(
            "Unity bridge is not connected. In the Unity Editor choose Tools/MCP Assistant to start the bridge."
        )


async def _call_bridge_tool(tool_name: str, payload: dict[str, Any]) -> list[types.Content]:
    _ensure_bridge_connected()

    timeout_ms = 45_000
    if "timeoutSeconds" in payload:
        unity_timeout = payload["timeoutSeconds"]
        timeout_ms = (unity_timeout + 20) * 1000

    try:
        response = await bridge_manager.send_command(tool_name, payload, timeout_ms=timeout_ms)
    except Exception as exc:  # pragma: no cover - surface bridge errors to client
        raise RuntimeError(f'Unity bridge tool "{tool_name}" failed: {exc}') from exc

    text = response if isinstance(response, str) else as_pretty_json(response)
    return [types.TextContent(type="text", text=text)]


def register_tools(server: Server) -> None:
    tool_definitions = get_tool_definitions()
    tool_map = {tool.name: tool for tool in tool_definitions}

    @server.list_tools()
    async def list_tools() -> list[types.Tool]:
        return tool_definitions

    @server.call_tool()
    async def call_tool(name: str, arguments: dict | None) -> list[types.Content]:
        if name not in tool_map:
            raise RuntimeError(f"Unknown tool requested: {name}")

        args = arguments or {}

        # ── Special tools ───────────────────────────────────────────
        if name == "unity_ping":
            _ensure_bridge_connected()
            heartbeat = bridge_manager.get_last_heartbeat()
            bridge_response = await bridge_manager.send_command("pingUnityEditor", {})
            payload = {
                "connected": True,
                "lastHeartbeatAt": heartbeat,
                "bridgeResponse": bridge_response,
            }
            return [types.TextContent(type="text", text=as_pretty_json(payload))]

        if name == "unity_compilation_await":
            # Async polling approach: wait for compilation to start, then wait for completion
            # NOTE: We don't require bridge connection here because disconnection during
            # compilation is expected (Unity domain reload disconnects the bridge).
            timeout_seconds = args.get("timeoutSeconds", 60)
            poll_interval = 0.5  # Poll every 500ms
            poll_timeout = 5.0  # Wait up to 5 seconds for compilation to start

            start_time = time.time()
            is_compiling = False
            unity_status: dict[str, Any] = {}

            # Check if bridge is already disconnected (likely compiling)
            if not bridge_manager.is_connected():
                if bridge_manager.is_compiling():
                    is_compiling = True
                    logger.info(
                        "Bridge disconnected but compilation flag set - waiting for reconnection"
                    )
                else:
                    is_compiling = True
                    logger.info(
                        "Bridge disconnected - assuming compilation in progress, waiting for reconnection"
                    )

            # Phase 1: Poll until compilation starts or timeout (only if connected)
            if not is_compiling:
                logger.info(
                    "Polling for compilation start (poll_timeout: %.1fs, total_timeout: %ds)...",
                    poll_timeout,
                    timeout_seconds,
                )

                while time.time() - start_time < poll_timeout:
                    if bridge_manager.is_compiling():
                        is_compiling = True
                        logger.info("Compilation detected via local state")
                        break

                    if not bridge_manager.is_connected():
                        is_compiling = True
                        logger.info("Bridge disconnected during polling - assuming compilation started")
                        break

                    try:
                        unity_status = await bridge_manager.send_command(
                            "compilationAwait", {"operation": "status"}
                        )
                        if unity_status.get("isCompiling", False):
                            is_compiling = True
                            logger.info("Compilation detected via Unity query")
                            break
                    except Exception as exc:
                        logger.debug("Unity query failed (may be compiling): %s", exc)
                        if not bridge_manager.is_connected():
                            is_compiling = True
                            logger.info("Bridge disconnected - assuming compilation started")
                            break

                    await asyncio.sleep(poll_interval)

            poll_elapsed = time.time() - start_time

            if not is_compiling:
                logger.info("No compilation detected after %.1fs polling", poll_elapsed)
                if unity_status:
                    unity_status["wasCompiling"] = False
                    unity_status["compilationCompleted"] = True
                    unity_status["waitTimeSeconds"] = poll_elapsed
                    unity_status["message"] = "No compilation detected"
                    return [types.TextContent(type="text", text=as_pretty_json(unity_status))]
                result = {
                    "wasCompiling": False,
                    "compilationCompleted": True,
                    "waitTimeSeconds": poll_elapsed,
                    "success": True,
                    "errorCount": 0,
                    "message": "No compilation detected",
                }
                return [types.TextContent(type="text", text=as_pretty_json(result))]

            # Phase 2: Compilation in progress - wait for completion asynchronously
            remaining_timeout = max(1, timeout_seconds - int(poll_elapsed))
            logger.info(
                "Compilation in progress - waiting for completion (timeout: %ds, bridge_connected: %s)...",
                remaining_timeout,
                bridge_manager.is_connected(),
            )

            try:
                compilation_result = await bridge_manager.await_compilation(remaining_timeout)
                total_elapsed = time.time() - start_time
                compilation_result["wasCompiling"] = True
                compilation_result["compilationCompleted"] = compilation_result.get(
                    "completed", True
                )
                compilation_result["waitTimeSeconds"] = total_elapsed
                return [types.TextContent(type="text", text=as_pretty_json(compilation_result))]
            except TimeoutError as exc:
                total_elapsed = time.time() - start_time
                result = {
                    "wasCompiling": True,
                    "compilationCompleted": False,
                    "waitTimeSeconds": total_elapsed,
                    "success": False,
                    "errorCount": 0,
                    "timedOut": True,
                    "message": str(exc),
                }
                return [types.TextContent(type="text", text=as_pretty_json(result))]

        if name == "unity_asset_crud":
            # Handle asset CRUD operations
            result = await _call_bridge_tool("assetManage", args)

            # Check if we need to wait for compilation (C# script creation/update/deletion)
            operation = args.get("operation")
            asset_path = args.get("assetPath", "")

            if operation in ["create", "update", "delete"] and asset_path.lower().endswith(".cs"):
                logger.info(
                    "C# script %s operation '%s' detected - waiting for compilation to complete...",
                    asset_path,
                    operation,
                )

                try:
                    compilation_result = await bridge_manager.await_compilation(timeout_seconds=60)

                    logger.info(
                        "Compilation completed: success=%s, errors=%s, elapsed=%ss",
                        compilation_result.get("success"),
                        compilation_result.get("errorCount", 0),
                        compilation_result.get("elapsedSeconds", 0),
                    )

                    if isinstance(result[0].text, str):
                        import json

                        try:
                            result_data = json.loads(result[0].text)
                            result_data["compilation"] = compilation_result
                            result[0].text = as_pretty_json(result_data)
                        except (json.JSONDecodeError, AttributeError):
                            result[
                                0
                            ].text += f"\n\nCompilation: {as_pretty_json(compilation_result)}"

                except TimeoutError as exc:
                    logger.warning("Compilation wait timed out: %s", exc)
                except Exception as exc:
                    logger.warning("Error while waiting for compilation: %s", exc)

            return result

        if name == "unity_batch_sequential_execute":
            return await handle_batch_sequential(args, bridge_manager)

        # ── Standard bridge tools (dict lookup) ─────────────────────
        bridge_name = TOOL_NAME_TO_BRIDGE.get(name)
        if bridge_name:
            return await _call_bridge_tool(bridge_name, args)

        raise RuntimeError(f"No handler registered for tool '{name}'.")
