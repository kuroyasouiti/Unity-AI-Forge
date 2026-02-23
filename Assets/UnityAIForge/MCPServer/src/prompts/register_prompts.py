"""MCP Prompt handler registration for Unity-AI-Forge.

Registers list_prompts and get_prompt handlers on the MCP server.
"""

from __future__ import annotations

import mcp.types as types
from mcp.server import Server

from prompts.prompt_definitions import (
    PROMPT_ARG_NAME,
    PROMPT_DEFINITIONS,
    PROMPT_TEMPLATE_MAP,
    PROMPT_VALID_VALUES,
)
from prompts.template_loader import load_prompt_template


def register_prompts(server: Server) -> None:
    """Register MCP prompt handlers on the server.

    Provides list_prompts and get_prompt for game development guide prompts.
    """

    @server.list_prompts()
    async def list_prompts() -> list[types.Prompt]:
        """List all available prompts."""
        return PROMPT_DEFINITIONS

    @server.get_prompt()
    async def get_prompt(
        name: str, arguments: dict[str, str] | None = None
    ) -> types.GetPromptResult:
        """Get a prompt by name with arguments."""
        if name not in PROMPT_ARG_NAME:
            raise ValueError(f"不明なプロンプト名です: {name}")

        arg_name = PROMPT_ARG_NAME[name]
        valid_values = PROMPT_VALID_VALUES[name]

        if not arguments or arg_name not in arguments:
            raise ValueError(
                f"引数 '{arg_name}' は必須です。" f" 選択肢: {', '.join(valid_values)}"
            )

        arg_value = arguments[arg_name]

        if arg_value not in valid_values:
            raise ValueError(
                f"'{arg_value}' は無効な値です。" f" 選択肢: {', '.join(valid_values)}"
            )

        template_key = (name, arg_value)
        template_path = PROMPT_TEMPLATE_MAP[template_key]
        content = load_prompt_template(template_path)

        return types.GetPromptResult(
            description=f"{name} - {arg_value}",
            messages=[
                types.PromptMessage(
                    role="user",
                    content=types.TextContent(type="text", text=content),
                )
            ],
        )
