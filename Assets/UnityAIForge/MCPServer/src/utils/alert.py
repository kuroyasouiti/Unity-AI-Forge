"""Cross-platform alert sound for compilation wait notifications."""

from __future__ import annotations

import os
import sys

from logger import logger

# Environment variable to disable alert: MCP_COMPILATION_ALERT=0
_enabled: bool = os.environ.get("MCP_COMPILATION_ALERT", "1").strip().lower() not in {
    "0",
    "false",
    "no",
    "off",
}


def play_compilation_alert() -> None:
    """Play a short alert sound to notify the user that compilation wait has started."""
    if not _enabled:
        return

    try:
        if sys.platform == "win32":
            import winsound

            # MB_ICONEXCLAMATION: system exclamation sound (non-blocking)
            winsound.MessageBeep(winsound.MB_ICONEXCLAMATION)
        elif sys.platform == "darwin":
            # macOS: use system sound via afplay (async, non-blocking)
            os.popen("afplay /System/Library/Sounds/Glass.aiff &")
        else:
            # Linux/other: terminal bell
            print("\a", end="", flush=True)
    except Exception as exc:
        logger.debug("Failed to play compilation alert: %s", exc)
