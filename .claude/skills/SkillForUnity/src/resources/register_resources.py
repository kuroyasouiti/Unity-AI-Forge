from __future__ import annotations

from pathlib import Path
from typing import Iterable
from urllib.parse import urlparse

import mcp.types as types
from mcp.server import Server
from mcp.server.lowlevel.helper_types import ReadResourceContents

from ..bridge.bridge_manager import bridge_manager
from ..config.env import env
from ..logger import logger
from ..utils.fs_utils import path_exists
from ..utils.project_structure import build_project_structure_summary


def _sanitize_unity_path(asset_path: str) -> str:
    return asset_path.replace("\\", "/")


def _is_likely_text_asset(file_path: Path) -> bool:
    return file_path.suffix.lower() in {
        ".cs",
        ".shader",
        ".cginc",
        ".compute",
        ".json",
        ".txt",
        ".md",
        ".meta",
        ".asmdef",
        ".uxml",
        ".uss",
        ".prefab",
        ".unity",
    }


def _render_hierarchy(node: dict, depth: int = 0) -> Iterable[str]:
    prefix = "  " * depth
    components = node.get("components") or []
    component_summary = ", ".join(
        comp.get("type", "") for comp in components if comp.get("type")
    )
    header = f"{prefix}- {node.get('name', '(unknown)')} ({node.get('type', 'Unknown')}"
    if component_summary:
        header += f" | {component_summary}"
    header += ")"

    yield header
    for child in node.get("children") or []:
        yield from _render_hierarchy(child, depth + 1)


def _text_content(text: str, mime_type: str = "text/plain") -> ReadResourceContents:
    return ReadResourceContents(content=text, mime_type=mime_type)


def register_resources(server: Server) -> None:
    @server.list_resources()
    async def list_resources() -> list[types.Resource]:
        base_resources = [
            types.Resource(
                uri="unity://project/structure",
                name="Unity Project Structure",
                description="Summary of the project folders and key assets.",
                mimeType="text/markdown",
            ),
            types.Resource(
                uri="unity://scene/active",
                name="Active Unity Scene",
                description="Details about the currently active scene and hierarchy.",
                mimeType="text/markdown",
            ),
        ]

        context = bridge_manager.get_context() or {}
        assets = context.get("assets") or []

        asset_resources = [
            types.Resource(
                uri=f"unity://asset/{asset['guid']}",
                name=_sanitize_unity_path(asset.get("path", asset["guid"])),
                description=asset.get("label") or asset.get("type"),
            )
            for asset in assets
            if asset.get("guid")
        ]

        return base_resources + asset_resources

    @server.list_resource_templates()
    async def list_resource_templates() -> list[types.ResourceTemplate]:
        return [
            types.ResourceTemplate(
                uriTemplate="unity://asset/{guid}",
                name="Unity Asset (GUID)",
                description="Access a Unity asset by supplying its GUID from the bridge context.",
            )
        ]

    @server.read_resource()
    async def read_resource(uri: types.AnyUrl) -> Iterable[ReadResourceContents]:
        parsed = urlparse(str(uri))
        category = parsed.netloc
        path = parsed.path.lstrip("/")

        if category == "project" and path == "structure":
            try:
                summary = await build_project_structure_summary(env.unity_project_root)
                return [_text_content(summary, "text/markdown")]
            except Exception as exc:  # pragma: no cover - defensive
                logger.error("Failed to build project summary: %s", exc)
                return [_text_content(f"Failed to build project summary: {exc}")]

        if category == "scene" and path == "active":
            context = bridge_manager.get_context()
            if not context or not context.get("hierarchy"):
                return [
                    _text_content(
                        "## Active Scene\nUnable to retrieve the active scene from the Unity bridge.",
                        "text/markdown",
                    )
                ]

            hierarchy_lines = "\n".join(_render_hierarchy(context["hierarchy"]))
            active_scene = context.get("activeScene") or {}
            header = (
                f"{active_scene.get('name')} ({active_scene.get('path')})"
                if active_scene
                else "Unknown Scene"
            )
            return [
                _text_content(
                    f"## Active Scene: {header}\n\n{hierarchy_lines}",
                    "text/markdown",
                )
            ]

        if category == "asset" and path:
            guid = path
            context = bridge_manager.get_context() or {}
            asset = next(
                (
                    item
                    for item in context.get("assets") or []
                    if item.get("guid") == guid
                ),
                None,
            )

            if not asset:
                return [_text_content(f"No asset with GUID {guid} is available in the bridge context.")]

            project_path = env.unity_project_root / Path(asset.get("path", ""))
            if not path_exists(project_path):
                return [_text_content(f"Asset file is missing on disk: {project_path}")]

            if not _is_likely_text_asset(project_path):
                return [
                    _text_content(
                        f"Binary asset types are not supported for text preview ({asset.get('path')})."
                    )
                ]

            try:
                contents = project_path.read_text(encoding="utf-8")
                return [_text_content(contents)]
            except UnicodeDecodeError:
                return [_text_content("Asset could not be decoded as UTF-8.")]
            except OSError as exc:
                logger.error("Failed to read asset %s: %s", project_path, exc)
                return [_text_content(f"Reading the asset failed: {exc}")]

        return [_text_content(f"Unknown Unity resource URI: {uri}")]
