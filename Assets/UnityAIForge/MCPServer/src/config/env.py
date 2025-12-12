from __future__ import annotations

import logging
import os
from dataclasses import dataclass
from pathlib import Path
from typing import Literal

from dotenv import load_dotenv

load_dotenv()

# Logger for configuration loading - uses standard logging since our custom logger
# depends on env which would create circular import
_config_logger = logging.getLogger(__name__)

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
    allowed: dict[str, LogLevel] = {
        "fatal": "fatal",
        "error": "error",
        "warn": "warn",
        "info": "info",
        "debug": "debug",
        "trace": "trace",
        "silent": "silent",
    }
    return allowed.get(normalized, "info")


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
    bridge_token: str | None


# CLI argument overrides storage
_cli_bridge_token: str | None = None
_cli_bridge_host: str | None = None
_cli_bridge_port: int | None = None


def apply_cli_overrides(
    bridge_token: str | None = None,
    bridge_host: str | None = None,
    bridge_port: int | None = None,
) -> None:
    """Apply command-line argument overrides.

    CLI arguments have the highest priority and override environment variables
    and token files. This function must be called before the first access to `env`.

    Args:
        bridge_token: Authentication token for Unity Bridge connection.
        bridge_host: Unity Bridge host address.
        bridge_port: Unity Bridge port number.
    """
    global _cli_bridge_token, _cli_bridge_host, _cli_bridge_port
    if bridge_token is not None:
        _cli_bridge_token = bridge_token
    if bridge_host is not None:
        _cli_bridge_host = bridge_host
    if bridge_port is not None:
        _cli_bridge_port = bridge_port


def _find_unity_project_root() -> Path:
    """Find Unity project root by looking for Assets folder."""
    # 1. Check environment variable first
    env_path = os.environ.get("UNITY_PROJECT_ROOT")
    if env_path:
        return _resolve_path(env_path, Path.cwd())

    # 2. Check current working directory
    cwd = Path.cwd()
    if (cwd / "Assets").is_dir():
        return cwd

    # 3. Look for Unity project root relative to MCP Server location
    # MCPServer is typically at: <project>/Assets/UnityAIForge/MCPServer
    script_path = Path(__file__).resolve()
    # Go up: env.py -> config -> src -> MCPServer -> UnityAIForge -> Assets -> ProjectRoot
    potential_root = script_path.parent.parent.parent.parent.parent.parent
    if (potential_root / "Assets").is_dir():
        return potential_root

    # 4. Fallback to cwd
    return cwd


_project_root = _find_unity_project_root()

def _load_bridge_token() -> str | None:
    """Load bridge token from environment variable.

    Priority order:
    1. CLI argument (--bridge-token) - handled by apply_cli_overrides
    2. Environment variable (MCP_BRIDGE_TOKEN)

    Note: Token files are no longer searched. Use --bridge-token CLI argument
    or MCP_BRIDGE_TOKEN environment variable instead.

    Returns:
        Token string if found, None otherwise.
    """
    env_token = os.environ.get("MCP_BRIDGE_TOKEN")
    if env_token and env_token.strip():
        _config_logger.debug("Bridge token loaded from MCP_BRIDGE_TOKEN environment variable")
        return env_token.strip()

    _config_logger.debug(
        "No bridge token found. Use --bridge-token CLI argument or MCP_BRIDGE_TOKEN env var."
    )
    return None


# Lazy initialization for ServerEnv
_env_instance: ServerEnv | None = None


def _create_env() -> ServerEnv:
    """Create ServerEnv instance with CLI overrides applied.

    Priority order (highest to lowest):
    1. CLI arguments (--bridge-token, --bridge-host, --bridge-port)
    2. Environment variables (MCP_BRIDGE_TOKEN, UNITY_BRIDGE_HOST, UNITY_BRIDGE_PORT)
    3. Token files (.mcp_bridge_tokens.json, .mcp_bridge_token)
    4. Default values
    """
    # Resolve bridge token: CLI > env > file
    bridge_token: str | None = _cli_bridge_token
    if bridge_token is None:
        bridge_token = _load_bridge_token()

    # Resolve bridge host: CLI > env > default
    bridge_host: str = _cli_bridge_host or os.environ.get("UNITY_BRIDGE_HOST", "127.0.0.1")

    # Resolve bridge port: CLI > env > default
    resolved_bridge_port: int
    if _cli_bridge_port is not None:
        resolved_bridge_port = _cli_bridge_port
    else:
        resolved_bridge_port = _parse_int(
            os.environ.get("UNITY_BRIDGE_PORT"), default=7070, minimum=1, maximum=65535
        )

    return ServerEnv(
        port=_parse_int(os.environ.get("MCP_SERVER_PORT"), default=6007, minimum=1, maximum=65535),
        host=os.environ.get("MCP_SERVER_HOST", "127.0.0.1"),
        log_level=_parse_log_level(os.environ.get("MCP_SERVER_LOG_LEVEL")),
        unity_project_root=_project_root,
        unity_editor_log_path=_resolve_path(
            os.environ.get("UNITY_EDITOR_LOG_PATH"), _default_editor_log()
        ),
        enable_file_watcher=_parse_bool(os.environ.get("MCP_ENABLE_FILE_WATCHER"), True),
        unity_bridge_host=bridge_host,
        unity_bridge_port=resolved_bridge_port,
        bridge_reconnect_ms=_parse_int(
            os.environ.get("MCP_BRIDGE_RECONNECT_MS"), default=5000, minimum=0
        ),
        bridge_token=bridge_token,
    )


def _get_env() -> ServerEnv:
    """Get the ServerEnv singleton, creating it on first access."""
    global _env_instance
    if _env_instance is None:
        _env_instance = _create_env()
    return _env_instance


class _EnvProxy:
    """Proxy class for lazy initialization of ServerEnv.

    This allows CLI arguments to be applied before the first access to env attributes.
    """

    def __getattr__(self, name: str) -> object:
        return getattr(_get_env(), name)

    def __repr__(self) -> str:
        return repr(_get_env())


env: ServerEnv = _EnvProxy()  # type: ignore[assignment]
