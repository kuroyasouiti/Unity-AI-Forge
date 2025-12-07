from __future__ import annotations

import os
from dataclasses import dataclass
from pathlib import Path
from typing import Literal

from dotenv import load_dotenv

load_dotenv()

LogLevel = Literal["fatal", "error", "warn", "info", "debug", "trace", "silent"]


def _parse_bool(value: str | None, default: bool) -> bool:
    if value is None:
        return default
    return value.strip().lower() in {"1", "true", "yes", "on"}


def _parse_int(
    value: str | None, default: int, minimum: int | None = None, maximum: int | None = None
) -> int:
    try:
        parsed = int(value) if value is not None else default
    except ValueError:
        return default

    if minimum is not None and parsed < minimum:
        return default
    if maximum is not None and parsed > maximum:
        return default
    return parsed


def _resolve_path(value: str | None, default: Path) -> Path:
    if not value:
        return default

    path = Path(value)
    if not path.is_absolute():
        path = Path.cwd() / path
    return path.resolve()


def _default_editor_log() -> Path:
    if os.name == "nt":
        local_app_data = os.environ.get("LOCALAPPDATA", "")
        return Path(local_app_data) / "Unity" / "Editor" / "Editor.log"

    home = os.environ.get("HOME", "")
    return Path(home) / "Library" / "Logs" / "Unity" / "Editor.log"


def _parse_log_level(value: str | None) -> LogLevel:
    normalized = (value or "").strip().lower()
    allowed: tuple[LogLevel, ...] = (
        "fatal",
        "error",
        "warn",
        "info",
        "debug",
        "trace",
        "silent",
    )
    return normalized if normalized in allowed else "info"


@dataclass(frozen=True)
class ServerEnv:
    port: int
    host: str
    log_level: LogLevel
    unity_project_root: Path
    unity_editor_log_path: Path
    enable_file_watcher: bool
    unity_bridge_host: str
    unity_bridge_port: int
    bridge_reconnect_ms: int


_project_root = _resolve_path(os.environ.get("UNITY_PROJECT_ROOT"), Path.cwd())

env = ServerEnv(
    port=_parse_int(os.environ.get("MCP_SERVER_PORT"), default=6007, minimum=1, maximum=65535),
    host=os.environ.get("MCP_SERVER_HOST", "127.0.0.1"),
    log_level=_parse_log_level(os.environ.get("MCP_SERVER_LOG_LEVEL")),
    unity_project_root=_project_root,
    unity_editor_log_path=_resolve_path(
        os.environ.get("UNITY_EDITOR_LOG_PATH"), _default_editor_log()
    ),
    enable_file_watcher=_parse_bool(os.environ.get("MCP_ENABLE_FILE_WATCHER"), True),
    unity_bridge_host=os.environ.get("UNITY_BRIDGE_HOST", "127.0.0.1"),
    unity_bridge_port=_parse_int(
        os.environ.get("UNITY_BRIDGE_PORT"), default=7070, minimum=1, maximum=65535
    ),
    bridge_reconnect_ms=_parse_int(
        os.environ.get("MCP_BRIDGE_RECONNECT_MS"), default=5000, minimum=0
    ),
)
