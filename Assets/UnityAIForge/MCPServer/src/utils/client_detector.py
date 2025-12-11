"""
Client detection utility to identify if the MCP server is running in Claude Desktop or Claude Code.
"""

from __future__ import annotations

import os
import platform
import sys

from bridge.messages import ClientInfo
from version import SERVER_NAME, SERVER_VERSION


def detect_client_name() -> str:
    """
    Detect which Claude client is running this MCP server.

    Returns:
        "Claude Desktop", "Claude Code", or "Unknown"
    """
    # Check environment variables that might indicate the client
    # Claude Code sets NODE_ENV and might have specific env vars
    if _is_claude_code():
        return "Claude Code"

    # Claude Desktop might have different indicators
    if _is_claude_desktop():
        return "Claude Desktop"

    return "Unknown"


def _is_claude_code() -> bool:
    """Check if running in Claude Code environment."""
    # Claude Code specific indicators:
    # 1. Check for VSCODE environment variables
    # 2. Check for electron environment
    # 3. Check process hierarchy (parent process might be code/cursor)

    # Check for VS Code / Cursor specific env vars
    vscode_indicators = [
        "VSCODE_PID",
        "VSCODE_IPC_HOOK",
        "VSCODE_CWD",
        "TERM_PROGRAM",  # "vscode" or "cursor"
    ]

    for indicator in vscode_indicators:
        value = os.environ.get(indicator, "")
        if value and ("vscode" in value.lower() or "code" in value.lower() or "cursor" in value.lower()):
            return True

    # Check TERM_PROGRAM specifically
    term_program = os.environ.get("TERM_PROGRAM", "").lower()
    if term_program in ["vscode", "cursor", "code"]:
        return True

    return False


def _is_claude_desktop() -> bool:
    """Check if running in Claude Desktop environment."""
    # Claude Desktop specific indicators:
    # 1. Electron environment without VS Code
    # 2. Specific Claude Desktop env vars (if any)

    # Check for Electron but NOT VS Code
    if os.environ.get("ELECTRON_RUN_AS_NODE"):
        if not _is_claude_code():
            return True

    # Check for Claude Desktop specific paths or env vars
    # (These are hypothetical - adjust based on actual Claude Desktop behavior)
    claude_desktop_indicators = [
        "CLAUDE_DESKTOP",
        "CLAUDE_APP",
    ]

    for indicator in claude_desktop_indicators:
        if os.environ.get(indicator):
            return True

    return False


def get_client_info() -> ClientInfo:
    """
    Get comprehensive client information.

    Returns:
        ClientInfo TypedDict containing:
        - clientName: "Claude Desktop", "Claude Code", or "Unknown"
        - serverName: Name of this MCP server
        - serverVersion: Version of this MCP server
        - pythonVersion: Python version string
        - platform: Platform string (Windows/Linux/Darwin)
    """

    info: ClientInfo = {
        "clientName": detect_client_name(),
        "serverName": SERVER_NAME,
        "serverVersion": SERVER_VERSION,
        "pythonVersion": _get_python_version(),
        "platform": _get_platform_name(),
    }
    return info


def _get_python_version() -> str:
    """Get Python version string."""
    return f"{sys.version_info.major}.{sys.version_info.minor}.{sys.version_info.micro}"


def _get_platform_name() -> str:
    """Get platform name."""
    system = platform.system()

    # Map to user-friendly names
    platform_map = {
        "Windows": "Windows",
        "Linux": "Linux",
        "Darwin": "macOS",
    }

    return platform_map.get(system, system)
