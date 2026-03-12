"""Sound notification utility for MCP tool completion.

Plays system sounds when tools complete, so the user gets feedback
even when Unity Editor is not focused.

Windows: uses winsound (built-in).
Other OS: uses terminal bell as fallback.
"""

from __future__ import annotations

import logging
import os
import threading
from typing import Literal

_logger = logging.getLogger(__name__)

NotificationMode = Literal["compilation", "all", "none"]
SoundType = Literal["success", "error", "warning"]


def _play_windows_sound(sound_type: SoundType) -> None:
    """Play a Windows system sound via winsound."""
    import winsound

    alias_map: dict[SoundType, str] = {
        "success": "SystemAsterisk",
        "error": "SystemHand",
        "warning": "SystemExclamation",
    }
    alias = alias_map.get(sound_type, "SystemDefault")
    try:
        winsound.PlaySound(alias, winsound.SND_ALIAS | winsound.SND_ASYNC)
    except Exception:
        # Fallback to MessageBeep
        try:
            flag_map = {
                "success": winsound.MB_OK,
                "error": winsound.MB_ICONHAND,
                "warning": winsound.MB_ICONEXCLAMATION,
            }
            winsound.MessageBeep(flag_map.get(sound_type, winsound.MB_OK))
        except Exception:
            pass


def _play_bell() -> None:
    """Emit a terminal bell character as a cross-platform fallback."""
    try:
        print("\a", end="", flush=True)
    except Exception:
        pass


def play_sound(sound_type: SoundType) -> None:
    """Play a notification sound in a background thread (non-blocking).

    Args:
        sound_type: One of 'success', 'error', or 'warning'.
    """

    def _play() -> None:
        try:
            if os.name == "nt":
                _play_windows_sound(sound_type)
            else:
                _play_bell()
        except Exception as exc:
            _logger.debug("Notification sound failed: %s", exc)

    thread = threading.Thread(target=_play, daemon=True)
    thread.start()


def notify_tool_result(
    tool_name: str,
    result_text: str,
    mode: NotificationMode,
) -> None:
    """Conditionally play a notification sound based on tool result and mode.

    Args:
        tool_name: The MCP tool name (e.g. 'unity_compilation_await').
        result_text: The JSON result text returned to the client.
        mode: Notification mode ('compilation', 'all', 'none').
    """
    if mode == "none":
        return

    is_compilation = tool_name in {
        "unity_compilation_await",
        "unity_asset_crud",
    }

    if mode == "compilation" and not is_compilation:
        return

    # Determine sound type from result content
    sound_type = _classify_result(result_text)
    if sound_type is None:
        return

    _logger.debug("Playing %s notification for %s", sound_type, tool_name)
    play_sound(sound_type)


def _classify_result(result_text: str) -> SoundType | None:
    """Classify a JSON result string to determine which sound to play.

    Returns None if no notification is warranted (e.g. no compilation detected).
    """
    # Quick heuristic checks on the JSON text to avoid parsing overhead
    has_errors = '"errorCount": 0' not in result_text and '"errorCount"' in result_text
    has_compilation_errors = '"compilationErrors"' in result_text and '"compilationErrors": []' not in result_text
    is_failure = '"success": false' in result_text.lower()
    is_timed_out = '"timedOut": true' in result_text.lower()
    no_compilation = '"message": "No compilation detected"' in result_text

    if no_compilation:
        return None

    if is_failure or has_compilation_errors or is_timed_out:
        return "error"
    if has_errors:
        return "warning"

    # For compilation results, always notify on success
    if '"compilationCompleted": true' in result_text.lower():
        return "success"
    if '"wasCompiling": true' in result_text.lower():
        return "success"

    # For non-compilation tools in "all" mode
    if '"success": true' in result_text.lower():
        return "success"

    return None
