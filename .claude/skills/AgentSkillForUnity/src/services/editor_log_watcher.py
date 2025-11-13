from __future__ import annotations

import asyncio
import contextlib
from dataclasses import dataclass
from pathlib import Path
from typing import Optional

from ..config.env import env
from ..logger import logger

MAX_LINES = 2000


def _classify_log_line(line: str) -> str:
    """Classify a Unity log line as 'error', 'warning', or 'normal'."""
    line_lower = line.lower()
    if any(marker in line_lower for marker in ["error:", "exception:", "assertion failed"]):
        return "error"
    if "warning:" in line_lower:
        return "warning"
    return "normal"


@dataclass
class EditorLogSnapshot:
    updated_at: float
    lines: list[str]
    source_path: str
    normal_lines: list[str]
    warning_lines: list[str]
    error_lines: list[str]


class EditorLogWatcher:
    def __init__(self, explicit_path: Optional[Path] = None, poll_interval: float = 2.0):
        self._target_path = explicit_path or env.unity_editor_log_path
        self._poll_interval = poll_interval
        self._buffer: list[str] = []
        self._updated_at: float = 0
        self._last_mtime: float | None = None
        self._task: asyncio.Task[None] | None = None
        self._lock = asyncio.Lock()

    async def start(self) -> None:
        await self.refresh()

        if self._task or not env.enable_file_watcher:
            return

        loop = asyncio.get_running_loop()
        self._task = loop.create_task(self._poll_loop())

    async def stop(self) -> None:
        task = self._task
        if not task:
            return

        task.cancel()
        with contextlib.suppress(asyncio.CancelledError):
            await task
        self._task = None

    async def refresh(self) -> None:
        path = Path(self._target_path)

        try:
            stat_result = path.stat()
        except FileNotFoundError:
            logger.warning("Unity editor log not found: %s", path)
            async with self._lock:
                self._buffer = []
                self._updated_at = 0
            return
        except OSError as exc:
            logger.warning("Failed to stat Unity editor log %s: %s", path, exc)
            return

        if self._last_mtime is not None and stat_result.st_mtime <= self._last_mtime:
            return

        try:
            contents = path.read_text(encoding="utf-8")
        except OSError as exc:
            logger.warning("Failed to read Unity editor log %s: %s", path, exc)
            return

        lines = contents.splitlines()
        capped = lines[-MAX_LINES:]

        async with self._lock:
            self._buffer = capped
            self._updated_at = asyncio.get_event_loop().time()
            self._last_mtime = stat_result.st_mtime

    def get_snapshot(self, limit: int = MAX_LINES) -> EditorLogSnapshot:
        clamp = max(0, min(limit, MAX_LINES))
        buffer = self._buffer[-clamp:] if clamp else []

        # Classify log lines
        normal_lines = []
        warning_lines = []
        error_lines = []

        for line in buffer:
            classification = _classify_log_line(line)
            if classification == "error":
                error_lines.append(line)
            elif classification == "warning":
                warning_lines.append(line)
            else:
                normal_lines.append(line)

        return EditorLogSnapshot(
            updated_at=self._updated_at,
            lines=list(buffer),
            source_path=str(self._target_path),
            normal_lines=normal_lines,
            warning_lines=warning_lines,
            error_lines=error_lines,
        )

    async def _poll_loop(self) -> None:
        try:
            while True:
                try:
                    await self.refresh()
                except Exception:  # pragma: no cover - defensive
                    logger.exception("Editor log poller crashed")
                await asyncio.sleep(self._poll_interval)
        except asyncio.CancelledError:
            raise


editor_log_watcher = EditorLogWatcher()
