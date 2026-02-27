"""Tests for tools/tool_definitions.py module."""

from __future__ import annotations


class TestGetToolDefinitions:
    """Tests for get_tool_definitions function."""

    def test_returns_52_tools(self) -> None:
        from tools.tool_definitions import get_tool_definitions

        defs = get_tool_definitions()
        assert len(defs) == 52

    def test_all_names_are_unique(self) -> None:
        from tools.tool_definitions import get_tool_definitions

        defs = get_tool_definitions()
        names = [t.name for t in defs]
        assert len(names) == len(set(names)), f"Duplicate tool names: {[n for n in names if names.count(n) > 1]}"

    def test_all_names_start_with_unity(self) -> None:
        from tools.tool_definitions import get_tool_definitions

        defs = get_tool_definitions()
        for tool in defs:
            assert tool.name.startswith("unity_"), f"Tool name must start with 'unity_': {tool.name}"

    def test_all_tools_have_descriptions(self) -> None:
        from tools.tool_definitions import get_tool_definitions

        defs = get_tool_definitions()
        for tool in defs:
            assert tool.description, f"Tool {tool.name} has empty description"
            assert len(tool.description) > 20, f"Tool {tool.name} description too short"

    def test_all_tools_have_input_schema(self) -> None:
        from tools.tool_definitions import get_tool_definitions

        defs = get_tool_definitions()
        for tool in defs:
            assert tool.inputSchema is not None, f"Tool {tool.name} has no inputSchema"
            assert isinstance(tool.inputSchema, dict), f"Tool {tool.name} inputSchema is not a dict"

    def test_all_bridge_tools_have_definitions(self) -> None:
        """Every tool in TOOL_NAME_TO_BRIDGE should have a definition."""
        from tools.tool_definitions import get_tool_definitions
        from tools.tool_registry import TOOL_NAME_TO_BRIDGE

        defs = get_tool_definitions()
        def_names = {t.name for t in defs}

        for mcp_name in TOOL_NAME_TO_BRIDGE:
            assert mcp_name in def_names, f"Bridge tool {mcp_name} has no definition in tool_definitions"

    def test_batch_sequential_is_included(self) -> None:
        from tools.tool_definitions import get_tool_definitions

        defs = get_tool_definitions()
        names = {t.name for t in defs}
        assert "unity_batch_sequential_execute" in names

    def test_schemas_are_called_not_functions(self) -> None:
        """Verify schemas are dict instances (functions were called), not callables."""
        from tools.tool_definitions import get_tool_definitions

        defs = get_tool_definitions()
        for tool in defs:
            assert not callable(tool.inputSchema), (
                f"Tool {tool.name} inputSchema is callable - schema function was not called with ()"
            )
