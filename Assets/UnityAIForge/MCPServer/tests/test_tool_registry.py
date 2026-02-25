"""Tests for tools/tool_registry.py module."""

from __future__ import annotations

import pytest


class TestToolNameToBridge:
    """Tests for TOOL_NAME_TO_BRIDGE mapping."""

    def test_mapping_has_47_entries(self) -> None:
        from tools.tool_registry import TOOL_NAME_TO_BRIDGE

        assert len(TOOL_NAME_TO_BRIDGE) == 47

    def test_all_mcp_names_start_with_unity(self) -> None:
        from tools.tool_registry import TOOL_NAME_TO_BRIDGE

        for name in TOOL_NAME_TO_BRIDGE:
            assert name.startswith("unity_"), f"MCP tool name must start with 'unity_': {name}"

    def test_bridge_names_are_unique(self) -> None:
        from tools.tool_registry import TOOL_NAME_TO_BRIDGE

        bridge_names = list(TOOL_NAME_TO_BRIDGE.values())
        assert len(bridge_names) == len(set(bridge_names)), "Duplicate bridge names found"

    def test_known_mappings(self) -> None:
        """Verify a selection of known MCP→bridge mappings."""
        from tools.tool_registry import TOOL_NAME_TO_BRIDGE

        expected = {
            "unity_ping": "pingUnityEditor",
            "unity_scene_crud": "sceneManage",
            "unity_gameobject_crud": "gameObjectManage",
            "unity_component_crud": "componentManage",
            "unity_asset_crud": "assetManage",
            "unity_prefab_crud": "prefabManage",
            "unity_validate_integrity": "sceneIntegrity",
            "unity_class_dependency_graph": "classDependencyGraph",
            "unity_scene_reference_graph": "sceneReferenceGraph",
            "unity_scene_relationship_graph": "sceneRelationshipGraph",
            "unity_scene_dependency": "sceneDependency",
            "unity_script_syntax": "scriptSyntax",
        }
        for mcp_name, bridge_name in expected.items():
            assert TOOL_NAME_TO_BRIDGE[mcp_name] == bridge_name


class TestBridgeToToolName:
    """Tests for reverse mapping."""

    def test_reverse_mapping_has_same_count(self) -> None:
        from tools.tool_registry import BRIDGE_TO_TOOL_NAME, TOOL_NAME_TO_BRIDGE

        assert len(BRIDGE_TO_TOOL_NAME) == len(TOOL_NAME_TO_BRIDGE)

    def test_reverse_mapping_inverts_correctly(self) -> None:
        from tools.tool_registry import BRIDGE_TO_TOOL_NAME, TOOL_NAME_TO_BRIDGE

        for mcp_name, bridge_name in TOOL_NAME_TO_BRIDGE.items():
            assert BRIDGE_TO_TOOL_NAME[bridge_name] == mcp_name


class TestSpecialTools:
    """Tests for SPECIAL_TOOLS set."""

    def test_special_tools_count(self) -> None:
        from tools.tool_registry import SPECIAL_TOOLS

        assert len(SPECIAL_TOOLS) == 4

    def test_batch_sequential_is_special(self) -> None:
        from tools.tool_registry import SPECIAL_TOOLS

        assert "unity_batch_sequential_execute" in SPECIAL_TOOLS

    def test_special_tools_contents(self) -> None:
        from tools.tool_registry import SPECIAL_TOOLS

        expected = {
            "unity_ping",
            "unity_compilation_await",
            "unity_asset_crud",
            "unity_batch_sequential_execute",
        }
        assert SPECIAL_TOOLS == expected


class TestResolveToolName:
    """Tests for resolve_tool_name function."""

    def test_resolve_mcp_name(self) -> None:
        from tools.tool_registry import resolve_tool_name

        assert resolve_tool_name("unity_scene_crud") == "sceneManage"

    def test_resolve_bridge_name_passthrough(self) -> None:
        from tools.tool_registry import resolve_tool_name

        assert resolve_tool_name("sceneManage") == "sceneManage"

    def test_resolve_unknown_name_raises(self) -> None:
        from tools.tool_registry import resolve_tool_name

        with pytest.raises(ValueError, match="Unsupported tool name"):
            resolve_tool_name("nonexistent_tool")

    def test_resolve_all_mcp_names(self) -> None:
        from tools.tool_registry import TOOL_NAME_TO_BRIDGE, resolve_tool_name

        for mcp_name, bridge_name in TOOL_NAME_TO_BRIDGE.items():
            assert resolve_tool_name(mcp_name) == bridge_name

    def test_resolve_all_bridge_names(self) -> None:
        from tools.tool_registry import TOOL_NAME_TO_BRIDGE, resolve_tool_name

        for bridge_name in TOOL_NAME_TO_BRIDGE.values():
            assert resolve_tool_name(bridge_name) == bridge_name
