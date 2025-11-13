from __future__ import annotations

from pathlib import Path


def path_exists(target_path: Path) -> bool:
    try:
        return target_path.exists()
    except OSError:
        return False
