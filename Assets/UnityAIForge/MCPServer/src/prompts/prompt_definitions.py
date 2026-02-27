"""Prompt definitions for Unity-AI-Forge MCP Server.

Defines 3 MCP prompts with their arguments and template mappings.
This is the single source of truth for all prompt metadata.
"""

from __future__ import annotations

import mcp.types as types

# --- Argument option lists ---

GENRE_OPTIONS: list[str] = [
    "platformer_2d",
    "rpg_turnbased",
    "puzzle",
    "shooter_2d",
    "action_2d",
    "card_game",
]

MECHANIC_OPTIONS: list[str] = [
    "state_machine",
    "inventory",
    "save_load",
    "enemy_ai",
    "level_design",
    "ui_ux",
    "object_pooling",
    "event_channel",
    "animation_controller",
]

WORKFLOW_OPTIONS: list[str] = [
    "planning",
    "design",
    "project_setup",
    "prototype",
    "alpha",
    "beta",
    "release",
    "testing",
    "scene_structure",
]

# --- Prompt definitions ---

PROMPT_DEFINITIONS: list[types.Prompt] = [
    types.Prompt(
        name="game_genre_guide",
        description="ジャンル別ゲーム設計ガイド。推奨シーン構成、フォルダ構造、実装ワークフローをMCPツール使用例付きで提供します。",
        arguments=[
            types.PromptArgument(
                name="genre",
                description=f"ゲームジャンル ({', '.join(GENRE_OPTIONS)})",
                required=True,
            ),
        ],
    ),
    types.Prompt(
        name="game_mechanics_guide",
        description="汎用ゲームメカニクス実装ガイド。状態管理、インベントリ、セーブ/ロードなど、ジャンル横断的なシステム設計パターンを提供します。",
        arguments=[
            types.PromptArgument(
                name="mechanic",
                description=f"メカニクス種別 ({', '.join(MECHANIC_OPTIONS)})",
                required=True,
            ),
        ],
    ),
    types.Prompt(
        name="game_workflow_guide",
        description="ゲーム制作ワークフローガイド。企画・設計からプロトタイプ、アルファ、ベータ、リリースまでの各フェーズにおける推奨手順を提供します。",
        arguments=[
            types.PromptArgument(
                name="phase",
                description=f"制作フェーズ ({', '.join(WORKFLOW_OPTIONS)})",
                required=True,
            ),
        ],
    ),
]

# --- Template path mapping ---
# Maps (prompt_name, argument_value) -> relative template path

PROMPT_TEMPLATE_MAP: dict[tuple[str, str], str] = {}

for _genre in GENRE_OPTIONS:
    PROMPT_TEMPLATE_MAP[("game_genre_guide", _genre)] = f"genre/{_genre}.md"

for _mechanic in MECHANIC_OPTIONS:
    PROMPT_TEMPLATE_MAP[("game_mechanics_guide", _mechanic)] = f"mechanics/{_mechanic}.md"

for _phase in WORKFLOW_OPTIONS:
    PROMPT_TEMPLATE_MAP[("game_workflow_guide", _phase)] = f"workflow/{_phase}.md"

# --- Argument name per prompt ---

PROMPT_ARG_NAME: dict[str, str] = {
    "game_genre_guide": "genre",
    "game_mechanics_guide": "mechanic",
    "game_workflow_guide": "phase",
}

# --- Valid values per prompt ---

PROMPT_VALID_VALUES: dict[str, list[str]] = {
    "game_genre_guide": GENRE_OPTIONS,
    "game_mechanics_guide": MECHANIC_OPTIONS,
    "game_workflow_guide": WORKFLOW_OPTIONS,
}
