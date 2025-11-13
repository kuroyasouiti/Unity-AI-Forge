from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

import anyio

from utils.fs_utils import path_exists

TARGET_EXTENSIONS = {
    ".cs",
    ".unity",
    ".prefab",
    ".asset",
    ".json",
    ".shader",
    ".uxml",
    ".uss",
}


@dataclass
class DirectorySummary:
    name: str
    exists: bool
    subdirectories: list[str]
    file_counts: dict[str, int]


def _count_extensions(entries: Iterable[Path]) -> dict[str, int]:
    counts: dict[str, int] = {}
    for entry in entries:
        ext = entry.suffix.lower()
        if ext in TARGET_EXTENSIONS:
            counts[ext] = counts.get(ext, 0) + 1
    return counts


def _summarize_directory(root: Path, relative: Path) -> DirectorySummary:
    absolute = root / relative
    if not path_exists(absolute):
        return DirectorySummary(
            name=str(relative),
            exists=False,
            subdirectories=[],
            file_counts={},
        )

    subdirectories: list[str] = []
    files: list[Path] = []

    for entry in absolute.iterdir():
        try:
            if entry.is_dir():
                subdirectories.append(entry.name)
            elif entry.is_file():
                files.append(entry)
        except OSError:
            continue

    return DirectorySummary(
        name=str(relative),
        exists=True,
        subdirectories=sorted(subdirectories),
        file_counts=_count_extensions(files),
    )


def _format_summary(items: list[DirectorySummary], root: Path) -> str:
    parts: list[str] = [f"## プロジェクト構造 ({root.name})"]

    for item in items:
        if not item.exists:
            parts.append(
                f"### {item.name}\n"
                "> フォルダが見つかりませんでした。"
            )
            continue

        subdirs = (
            "\n".join(f"- {name}" for name in item.subdirectories)
            if item.subdirectories
            else "- (サブフォルダなし)"
        )
        counts = (
            ", ".join(f"{ext}: {count}" for ext, count in item.file_counts.items())
            if item.file_counts
            else "対象拡張子のファイルなし"
        )

        parts.append(
            "\n".join(
                [
                    f"### {item.name}",
                    f"- サブフォルダ:\n{subdirs}",
                    f"- ファイル集計: {counts}",
                ]
            )
        )

    return "\n\n".join(parts)


def _build_summary_sync(root: Path) -> str:
    targets = [
        Path("Assets"),
        Path("Packages"),
        Path("ProjectSettings"),
        Path("UserSettings"),
        Path("Logs"),
    ]

    summaries = [_summarize_directory(root, target) for target in targets]
    return _format_summary(summaries, root)


async def build_project_structure_summary(root: Path) -> str:
    return await anyio.to_thread.run_sync(_build_summary_sync, root)
