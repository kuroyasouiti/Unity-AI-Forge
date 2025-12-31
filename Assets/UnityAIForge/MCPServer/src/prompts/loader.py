"""Prompt loader for Unity-AI-Forge MCP Server.

Loads system instructions from external markdown files,
supporting version placeholder replacement.
"""

from __future__ import annotations

from pathlib import Path


def load_system_prompt(version: str) -> str:
    """Load system prompt from markdown file with version substitution.

    Args:
        version: Server version string to substitute in the prompt

    Returns:
        Formatted system prompt string

    Raises:
        FileNotFoundError: If the prompt file doesn't exist
    """
    prompt_path = Path(__file__).parent / "system_instructions.md"

    if not prompt_path.exists():
        raise FileNotFoundError(f"System prompt file not found: {prompt_path}")

    content = prompt_path.read_text(encoding="utf-8")

    # Replace version placeholder
    content = content.replace("{VERSION}", version)

    return content
