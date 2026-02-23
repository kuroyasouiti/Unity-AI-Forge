"""Tests for MCP prompt definitions, template loader, and registration."""

from __future__ import annotations

from unittest.mock import MagicMock

import pytest

from prompts.prompt_definitions import (
    GENRE_OPTIONS,
    MECHANIC_OPTIONS,
    PROMPT_ARG_NAME,
    PROMPT_DEFINITIONS,
    PROMPT_TEMPLATE_MAP,
    PROMPT_VALID_VALUES,
    WORKFLOW_OPTIONS,
)
from prompts.register_prompts import register_prompts
from prompts.template_loader import (
    TEMPLATES_DIR,
    list_available_templates,
    load_prompt_template,
)

# ---------------------------------------------------------------------------
# Prompt Definitions
# ---------------------------------------------------------------------------


class TestPromptDefinitions:
    """Tests for prompt_definitions.py."""

    def test_has_three_prompts(self) -> None:
        assert len(PROMPT_DEFINITIONS) == 3

    def test_prompt_names(self) -> None:
        names = [p.name for p in PROMPT_DEFINITIONS]
        assert names == ["game_genre_guide", "game_mechanics_guide", "game_workflow_guide"]

    def test_all_prompts_have_one_required_argument(self) -> None:
        for prompt in PROMPT_DEFINITIONS:
            assert prompt.arguments is not None
            assert len(prompt.arguments) == 1
            assert prompt.arguments[0].required is True

    def test_genre_options_count(self) -> None:
        assert len(GENRE_OPTIONS) == 6

    def test_mechanic_options_count(self) -> None:
        assert len(MECHANIC_OPTIONS) == 6

    def test_workflow_options_count(self) -> None:
        assert len(WORKFLOW_OPTIONS) == 5

    def test_template_map_covers_all_options(self) -> None:
        """Every (prompt_name, option) pair must have a template path."""
        expected_count = len(GENRE_OPTIONS) + len(MECHANIC_OPTIONS) + len(WORKFLOW_OPTIONS)
        assert len(PROMPT_TEMPLATE_MAP) == expected_count

    def test_template_map_keys_match_valid_values(self) -> None:
        for prompt_name, valid_values in PROMPT_VALID_VALUES.items():
            for value in valid_values:
                assert (prompt_name, value) in PROMPT_TEMPLATE_MAP

    def test_arg_name_map_covers_all_prompts(self) -> None:
        for prompt in PROMPT_DEFINITIONS:
            assert prompt.name in PROMPT_ARG_NAME

    def test_valid_values_map_covers_all_prompts(self) -> None:
        for prompt in PROMPT_DEFINITIONS:
            assert prompt.name in PROMPT_VALID_VALUES


# ---------------------------------------------------------------------------
# Template Loader
# ---------------------------------------------------------------------------


class TestTemplateLoader:
    """Tests for template_loader.py."""

    def test_load_existing_template(self) -> None:
        """Loading a template that exists should return content with version replaced."""
        content = load_prompt_template("genre/platformer_2d.md")
        assert len(content) > 0
        assert "{VERSION}" not in content

    def test_load_nonexistent_template_raises(self) -> None:
        with pytest.raises(FileNotFoundError, match="テンプレートファイルが見つかりません"):
            load_prompt_template("nonexistent/missing.md")

    def test_version_replacement(self) -> None:
        content = load_prompt_template("genre/platformer_2d.md")
        assert "{VERSION}" not in content

    def test_list_available_templates(self) -> None:
        templates = list_available_templates()
        assert len(templates) == 17
        assert "genre/platformer_2d.md" in templates
        assert "mechanics/state_machine.md" in templates
        assert "workflow/prototyping.md" in templates

    def test_list_templates_uses_forward_slashes(self) -> None:
        templates = list_available_templates()
        for t in templates:
            assert "\\" not in t

    def test_all_template_map_files_exist(self) -> None:
        """Every path in PROMPT_TEMPLATE_MAP must correspond to an existing file."""
        for key, relative_path in PROMPT_TEMPLATE_MAP.items():
            full_path = TEMPLATES_DIR / relative_path
            assert full_path.exists(), f"Missing template: {relative_path} (for {key})"


# ---------------------------------------------------------------------------
# Register Prompts
# ---------------------------------------------------------------------------


class TestRegisterPrompts:
    """Tests for register_prompts.py."""

    def _capture_handlers(self) -> tuple[MagicMock, dict]:
        """Create a mock server and capture registered handlers."""
        server = MagicMock()
        handlers: dict = {}

        def make_decorator(name: str):
            def decorator():
                def wrapper(fn):
                    handlers[name] = fn
                    return fn

                return wrapper

            return decorator

        server.list_prompts = make_decorator("list_prompts")
        server.get_prompt = make_decorator("get_prompt")

        register_prompts(server)
        return server, handlers

    def test_registers_list_prompts_handler(self) -> None:
        _, handlers = self._capture_handlers()
        assert "list_prompts" in handlers

    def test_registers_get_prompt_handler(self) -> None:
        _, handlers = self._capture_handlers()
        assert "get_prompt" in handlers

    @pytest.mark.asyncio
    async def test_list_prompts_returns_all_definitions(self) -> None:
        _, handlers = self._capture_handlers()
        result = await handlers["list_prompts"]()
        assert len(result) == 3
        names = [p.name for p in result]
        assert "game_genre_guide" in names
        assert "game_mechanics_guide" in names
        assert "game_workflow_guide" in names

    @pytest.mark.asyncio
    async def test_get_prompt_valid_genre(self) -> None:
        _, handlers = self._capture_handlers()
        result = await handlers["get_prompt"]("game_genre_guide", {"genre": "platformer_2d"})
        assert result.description == "game_genre_guide - platformer_2d"
        assert len(result.messages) == 1
        assert result.messages[0].role == "user"
        assert len(result.messages[0].content.text) > 0

    @pytest.mark.asyncio
    async def test_get_prompt_valid_mechanic(self) -> None:
        _, handlers = self._capture_handlers()
        result = await handlers["get_prompt"]("game_mechanics_guide", {"mechanic": "inventory"})
        assert result.description == "game_mechanics_guide - inventory"

    @pytest.mark.asyncio
    async def test_get_prompt_valid_workflow(self) -> None:
        _, handlers = self._capture_handlers()
        result = await handlers["get_prompt"]("game_workflow_guide", {"phase": "prototyping"})
        assert result.description == "game_workflow_guide - prototyping"

    @pytest.mark.asyncio
    async def test_get_prompt_unknown_name_raises(self) -> None:
        _, handlers = self._capture_handlers()
        with pytest.raises(ValueError, match="不明なプロンプト名です"):
            await handlers["get_prompt"]("nonexistent", {})

    @pytest.mark.asyncio
    async def test_get_prompt_missing_argument_raises(self) -> None:
        _, handlers = self._capture_handlers()
        with pytest.raises(ValueError, match="引数 'genre' は必須です"):
            await handlers["get_prompt"]("game_genre_guide", {})

    @pytest.mark.asyncio
    async def test_get_prompt_none_arguments_raises(self) -> None:
        _, handlers = self._capture_handlers()
        with pytest.raises(ValueError, match="引数 'genre' は必須です"):
            await handlers["get_prompt"]("game_genre_guide", None)

    @pytest.mark.asyncio
    async def test_get_prompt_invalid_value_raises(self) -> None:
        _, handlers = self._capture_handlers()
        with pytest.raises(ValueError, match="無効な値です"):
            await handlers["get_prompt"]("game_genre_guide", {"genre": "invalid_genre"})
