from __future__ import annotations

import argparse
import asyncio
import contextlib
import os
import sys
from json import JSONDecodeError
from pathlib import Path
from typing import Any

import uvicorn
from mcp.server import NotificationOptions
from mcp.server.stdio import stdio_server as mcp_stdio_server
from mcp.server.websocket import websocket_server as mcp_websocket_server
from starlette.applications import Starlette
from starlette.requests import Request
from starlette.responses import JSONResponse, PlainTextResponse
from starlette.routing import Route, WebSocketRoute
from starlette.websockets import WebSocket, WebSocketDisconnect

# Add src directory to path for direct imports
_package_root = Path(__file__).resolve().parent
if str(_package_root) not in sys.path:
    sys.path.insert(0, str(_package_root))

from bridge.bridge_connector import bridge_connector
from bridge.bridge_manager import bridge_manager
from config.env import env
from logger import logger
from server.create_mcp_server import create_mcp_server
from services.editor_log_watcher import editor_log_watcher
from version import SERVER_NAME, SERVER_VERSION

mcp_server = create_mcp_server()


def _create_init_options() -> Any:
    return mcp_server.create_initialization_options(
        notification_options=NotificationOptions(
            resources_changed=True,
            tools_changed=True,
        )
    )


def _bridge_connected() -> None:
    logger.info("Unity bridge handshake completed")


def _bridge_disconnected() -> None:
    logger.warning("Unity bridge disconnected")


def _bridge_context_updated(context: dict[str, Any]) -> None:
    active_scene = context.get("activeScene") or {}
    logger.debug(
        "Unity context updated (scene=%s updatedAt=%s)",
        active_scene.get("name"),
        context.get("updatedAt"),
    )


bridge_manager.on("connected", _bridge_connected)
bridge_manager.on("disconnected", _bridge_disconnected)
bridge_manager.on("contextUpdated", _bridge_context_updated)


async def health_endpoint(_: Request) -> JSONResponse:
    return JSONResponse(
        {
            "status": "ok",
            "bridgeConnected": bridge_manager.is_connected(),
            "lastHeartbeatAt": bridge_manager.get_last_heartbeat(),
            "server": SERVER_NAME,
            "version": SERVER_VERSION,
        }
)


async def bridge_status_endpoint(_: Request) -> JSONResponse:
    return JSONResponse(
        {
            "connected": bridge_manager.is_connected(),
            "sessionId": bridge_manager.get_session_id(),
            "lastHeartbeatAt": bridge_manager.get_last_heartbeat(),
            "context": bridge_manager.get_context(),
        }
    )


async def bridge_command_endpoint(request: Request) -> JSONResponse:
    if not bridge_manager.is_connected():
        return JSONResponse(
            {"error": "Unity bridge is not connected"}, status_code=503
        )

    try:
        body = await request.json()
    except JSONDecodeError:
        return JSONResponse({"error": "Invalid JSON payload"}, status_code=400)

    tool_name = body.get("toolName")
    if not tool_name:
        return JSONResponse(
            {"error": "Field 'toolName' is required"}, status_code=400
        )

    payload = body.get("payload")
    timeout_ms = body.get("timeoutMs")
    resolved_timeout = (
        timeout_ms if isinstance(timeout_ms, int) and timeout_ms > 0 else 30_000
    )

    try:
        result = await bridge_manager.send_command(
            tool_name,
            payload if payload is not None else {},
            resolved_timeout,
        )
    except TimeoutError:
        return JSONResponse(
            {
                "error": "Bridge command timed out",
                "toolName": tool_name,
                "timeoutMs": resolved_timeout,
            },
            status_code=504,
        )
    except Exception as exc:  # pragma: no cover - defensive
        logger.error("Bridge command failed: %s", exc)
        return JSONResponse(
            {"error": f"Bridge command failed: {exc}"}, status_code=500
        )

    return JSONResponse({"ok": True, "result": result})


async def default_endpoint(_: Request) -> PlainTextResponse:
    return PlainTextResponse("Not Found", status_code=404)


async def mcp_ws_endpoint(websocket: WebSocket) -> None:
    client = websocket.client
    logger.info(
        "MCP client connected via WebSocket (address=%s:%s user_agent=%s)",
        client.host if client else "unknown",
        client.port if client else "unknown",
        websocket.headers.get("user-agent"),
    )

    try:
        async with mcp_websocket_server(websocket.scope, websocket.receive, websocket.send) as (
            read_stream,
            write_stream,
        ):
            await mcp_server.run(read_stream, write_stream, _create_init_options())
    except WebSocketDisconnect:
        logger.info("MCP client disconnected")
    except Exception as exc:  # pragma: no cover - defensive
        logger.exception("Failed to serve MCP client: %s", exc)
    finally:
        with contextlib.suppress(Exception):
            await websocket.close()


async def startup() -> None:
    logger.info(
        "Starting Unity MCP server (host=%s port=%s)",
        env.host,
        env.port,
    )
    logger.info(
        "Unity Bridge target: %s:%s (token=%s)",
        env.unity_bridge_host,
        env.unity_bridge_port,
        "****" + env.bridge_token[-4:] if env.bridge_token and len(env.bridge_token) > 4 else ("set" if env.bridge_token else "not set"),
    )
    await editor_log_watcher.start()
    bridge_connector.start()


async def shutdown() -> None:
    logger.info("Shutting down Unity MCP server")
    await bridge_connector.stop()
    await editor_log_watcher.stop()


routes = [
    Route("/healthz", health_endpoint, methods=["GET"]),
    Route("/bridge/status", bridge_status_endpoint, methods=["GET"]),
    Route("/bridge/command", bridge_command_endpoint, methods=["POST"]),
    Route("/{path:path}", default_endpoint, methods=["GET", "POST", "PUT", "PATCH", "DELETE"]),
    WebSocketRoute("/mcp", mcp_ws_endpoint),
]

app = Starlette(routes=routes, on_startup=[startup], on_shutdown=[shutdown])


def _run_with_uv(config: uvicorn.Config) -> bool:
    try:
        import uv  # type: ignore[import-not-found]
    except ImportError:
        return False

    run = getattr(uv, "run", None)
    if not callable(run):
        logger.warning("python 'uv' module has no callable 'run'; falling back to asyncio")
        return False

    try:
        run(_serve(config))  # type: ignore[arg-type]
    except TypeError:
        logger.warning("python 'uv.run' signature incompatible; falling back to asyncio")
        return False
    except KeyboardInterrupt:
        logger.info("Received interrupt, shutting down")
    except Exception:
        logger.exception("MCP server failed while running under uv")
        sys.exit(1)

    return True


def _run_with_asyncio(config: uvicorn.Config) -> None:
    try:
        asyncio.run(_serve(config))
    except KeyboardInterrupt:
        logger.info("Received interrupt, shutting down")
    except Exception:
        logger.exception("Uvicorn server failed")
        sys.exit(1)


async def _serve(config: uvicorn.Config) -> None:
    server = uvicorn.Server(config)
    await server.serve()


async def _run_stdio() -> None:
    await startup()
    try:
        async with mcp_stdio_server() as (read_stream, write_stream):
            await mcp_server.run(read_stream, write_stream, _create_init_options())
    finally:
        await shutdown()


def _parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Unity MCP server entrypoint")
    parser.add_argument(
        "--transport",
        choices=("websocket", "stdio"),
        default=os.environ.get("MCP_SERVER_TRANSPORT", "stdio"),
        help="Transport to expose (defaults to MCP_SERVER_TRANSPORT or 'stdio')",
    )
    parser.add_argument(
        "--bridge-token",
        default=None,
        help="Authentication token for Unity Bridge connection (overrides env/file)",
    )
    parser.add_argument(
        "--bridge-host",
        default=None,
        help="Unity Bridge host address (overrides UNITY_BRIDGE_HOST, default: 127.0.0.1)",
    )
    parser.add_argument(
        "--bridge-port",
        type=int,
        default=None,
        help="Unity Bridge port (overrides UNITY_BRIDGE_PORT, default: 7070)",
    )
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> None:
    args = _parse_args(argv)

    # Apply CLI argument overrides before accessing env
    from config.env import apply_cli_overrides
    apply_cli_overrides(
        bridge_token=args.bridge_token,
        bridge_host=args.bridge_host,
        bridge_port=args.bridge_port,
    )

    if args.transport == "stdio":
        try:
            asyncio.run(_run_stdio())
        except KeyboardInterrupt:
            logger.info("Received interrupt, shutting down")
        except Exception:
            logger.exception("MCP server failed while running on stdio")
            sys.exit(1)
        return

    log_level = {
        "trace": "debug",
        "debug": "debug",
        "info": "info",
        "warn": "warning",
        "error": "error",
        "fatal": "critical",
        "silent": "critical",
    }.get(env.log_level, "info")

    config = uvicorn.Config(
        app,
        host=env.host,
        port=env.port,
        log_level=log_level,
        loop="asyncio",
        lifespan="on",
    )
    if _run_with_uv(config):
        return

    _run_with_asyncio(config)


__all__ = ["app", "main"]


if __name__ == "__main__":
    main()
