"""Port discovery module for finding the Unity bridge port via temp files.

When multiple Unity projects run Unity-AI-Forge simultaneously, each binds to
a different port. The Unity side writes a port file to %TEMP%/unity-ai-forge/
keyed by a hash of the project path. This module reads that file to discover
the correct port automatically.
"""

from __future__ import annotations

import hashlib
import json
import logging
import os
import tempfile
from pathlib import Path

_logger = logging.getLogger(__name__)

_DIRECTORY_NAME = "unity-ai-forge"


def _compute_project_hash(project_path: Path) -> str:
    """Compute deterministic hash for a project path.

    Normalization (must match C# PortDiscoveryFile.ComputeProjectHash):
    1. Replace backslashes with forward slashes
    2. Lowercase
    3. Strip trailing slash
    4. SHA256 of UTF-8 bytes, first 16 hex characters
    """
    normalized = str(project_path).replace("\\", "/").lower().rstrip("/")
    return hashlib.sha256(normalized.encode("utf-8")).hexdigest()[:16]


def _get_discovery_directory() -> Path:
    """Return the discovery directory: %TEMP%/unity-ai-forge/"""
    return Path(tempfile.gettempdir()) / _DIRECTORY_NAME


def _is_process_alive(pid: int) -> bool:
    """Check whether a process with the given PID is still running."""
    try:
        os.kill(pid, 0)
        return True
    except ProcessLookupError:
        return False
    except PermissionError:
        # Process exists but we don't have permission to signal it
        return True
    except OSError:
        return False


def discover_bridge_port(project_root: Path) -> int | None:
    """Discover the bridge port for a Unity project via its port file.

    Args:
        project_root: Path to the Unity project root directory.

    Returns:
        The bridge port number if a valid port file exists, None otherwise.
        Stale port files (dead PID) are deleted automatically.
    """
    project_hash = _compute_project_hash(project_root)
    port_file = _get_discovery_directory() / f"{project_hash}.port"

    if not port_file.exists():
        _logger.debug("No port file found at %s", port_file)
        return None

    try:
        data = json.loads(port_file.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, OSError) as exc:
        _logger.warning("Failed to read port file %s: %s", port_file, exc)
        return None

    port = data.get("port")
    if not isinstance(port, int):
        _logger.warning("Invalid port value in %s: %s", port_file, port)
        return None

    pid = data.get("pid")
    if isinstance(pid, int) and not _is_process_alive(pid):
        _logger.info("Stale port file (PID %d dead), removing %s", pid, port_file)
        try:
            port_file.unlink()
        except OSError:
            pass
        return None

    _logger.info("Discovered bridge port %d from %s", port, port_file)
    return port
