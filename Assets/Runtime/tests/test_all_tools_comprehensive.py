"""
Comprehensive test for all UnityMCP tools.
Validates tool definitions, schemas, and basic functionality.
"""
import unittest
import json
from typing import Any, Dict

from mcp.server import Server
import mcp.types as mcp_types

from MCPServer.tools import register_tools as tools_module


class ComprehensiveToolTests(unittest.IsolatedAsyncioTestCase):
    """Comprehensive tests for all UnityMCP tools."""

    def setUp(self) -> None:
        self.server = Server("test", version="test")
        tools_module.register_tools(self.server)
        self.list_handler = self.server.request_handlers[mcp_types.ListToolsRequest]
        self.call_handler = self.server.request_handlers[mcp_types.CallToolRequest]

    async def test_all_tools_registered(self) -> None:
        """Test that all expected tools are registered."""
        result = await self.list_handler(
            mcp_types.ListToolsRequest(method="tools/list")
        )
        tools_result = result.root
        self.assertIsInstance(tools_result, mcp_types.ListToolsResult)

        tool_names = [tool.name for tool in tools_result.tools]

        expected_tools = [
            "unity_ping",
            "unity_scene_crud",
            "unity_gameobject_crud",
            "unity_component_crud",
            "unity_asset_crud",
            "unity_ugui_rectAdjust",
            "unity_ugui_anchorManage",
            "unity_ugui_manage",
            "unity_hierarchy_builder",
            "unity_scene_quickSetup",
            "unity_gameobject_createFromTemplate",
            "unity_context_inspect",
            "unity_ugui_createFromTemplate",
            "unity_ugui_layoutManage",
            "unity_tagLayer_manage",
            "unity_project_compile",
            "unity_prefab_crud",
            "unity_projectSettings_crud",
            "unity_renderPipeline_manage",
            "unity_inputSystem_manage",
            "unity_tilemap_manage",
            "unity_navmesh_manage",
        ]

        print(f"\n[OK] Found {len(tool_names)} tools")
        for tool_name in expected_tools:
            self.assertIn(tool_name, tool_names, f"Tool {tool_name} not found")
            print(f"  [OK] {tool_name}")

    async def test_all_tools_have_valid_schemas(self) -> None:
        """Test that all tools have valid JSON schemas."""
        result = await self.list_handler(
            mcp_types.ListToolsRequest(method="tools/list")
        )
        tools_result = result.root

        print(f"\n[OK] Validating schemas for {len(tools_result.tools)} tools")

        for tool in tools_result.tools:
            # Check that tool has a name
            self.assertIsNotNone(tool.name)
            self.assertTrue(len(tool.name) > 0)

            # Check that tool has a description
            self.assertIsNotNone(tool.description)
            self.assertTrue(len(tool.description) > 0)

            # Check that tool has a valid schema
            self.assertIsNotNone(tool.inputSchema)
            schema = tool.inputSchema

            # Validate schema structure
            self.assertEqual(schema.get("type"), "object")
            self.assertIn("properties", schema)
            self.assertIsInstance(schema["properties"], dict)

            # Check additionalProperties is False (strict schema)
            self.assertIn("additionalProperties", schema)
            self.assertFalse(schema["additionalProperties"])

            print(f"  [OK] {tool.name}: {len(schema['properties'])} properties")

    async def test_tool_schemas_have_required_fields(self) -> None:
        """Test that tool schemas properly define required fields."""
        result = await self.list_handler(
            mcp_types.ListToolsRequest(method="tools/list")
        )
        tools_result = result.root

        print(f"\n[OK] Checking required fields for {len(tools_result.tools)} tools")

        for tool in tools_result.tools:
            schema = tool.inputSchema

            # Most tools should have required fields (except ping)
            if tool.name != "unity_ping":
                if "required" in schema:
                    required_fields = schema["required"]
                    self.assertIsInstance(required_fields, list)
                    print(f"  [OK] {tool.name}: {len(required_fields)} required fields {required_fields}")
                else:
                    # Context inspect has no required fields
                    if tool.name != "unity_context_inspect":
                        print(f"  [WARN] {tool.name}: No required fields defined")
            else:
                print(f"  [OK] {tool.name}: No required fields (as expected)")

    async def test_operation_based_tools_have_operation_enum(self) -> None:
        """Test that operation-based tools have proper operation enums."""
        result = await self.list_handler(
            mcp_types.ListToolsRequest(method="tools/list")
        )
        tools_result = result.root

        operation_tools = [
            tool for tool in tools_result.tools
            if "crud" in tool.name or "manage" in tool.name or "Manage" in tool.name
        ]

        print(f"\n[OK] Checking operation enums for {len(operation_tools)} tools")

        for tool in operation_tools:
            schema = tool.inputSchema
            properties = schema.get("properties", {})

            if "operation" in properties:
                operation_prop = properties["operation"]
                self.assertIn("enum", operation_prop)
                self.assertIsInstance(operation_prop["enum"], list)
                self.assertTrue(len(operation_prop["enum"]) > 0)

                operations = operation_prop["enum"]
                print(f"  [OK] {tool.name}: {len(operations)} operations {operations[:3]}{'...' if len(operations) > 3 else ''}")
            else:
                print(f"  [WARN] {tool.name}: No operation field (may be intentional)")

    async def test_template_tools_have_template_enum(self) -> None:
        """Test that template-based tools have proper template enums."""
        result = await self.list_handler(
            mcp_types.ListToolsRequest(method="tools/list")
        )
        tools_result = result.root

        template_tools = [
            tool for tool in tools_result.tools
            if "Template" in tool.name or "template" in tool.description.lower()
        ]

        print(f"\n[OK] Checking template enums for {len(template_tools)} tools")

        for tool in template_tools:
            schema = tool.inputSchema
            properties = schema.get("properties", {})

            if "template" in properties:
                template_prop = properties["template"]
                self.assertIn("enum", template_prop)
                self.assertIsInstance(template_prop["enum"], list)
                self.assertTrue(len(template_prop["enum"]) > 0)

                templates = template_prop["enum"]
                print(f"  [OK] {tool.name}: {len(templates)} templates {templates[:3]}{'...' if len(templates) > 3 else ''}")

    async def test_ugui_tools_have_proper_schemas(self) -> None:
        """Test that uGUI-related tools have proper schemas."""
        result = await self.list_handler(
            mcp_types.ListToolsRequest(method="tools/list")
        )
        tools_result = result.root

        ugui_tools = [tool for tool in tools_result.tools if "ugui" in tool.name.lower()]

        print(f"\n[OK] Checking uGUI tools ({len(ugui_tools)} tools)")

        for tool in ugui_tools:
            schema = tool.inputSchema
            properties = schema.get("properties", {})

            # Most uGUI tools should have gameObjectPath
            if tool.name not in ["unity_ugui_createFromTemplate"]:
                self.assertIn("gameObjectPath", properties)

            print(f"  [OK] {tool.name}: {len(properties)} properties")

    async def test_tools_with_multiple_operations(self) -> None:
        """Test tools that support multiple operations on multiple targets."""
        result = await self.list_handler(
            mcp_types.ListToolsRequest(method="tools/list")
        )
        tools_result = result.root

        multiple_op_tools = [
            tool for tool in tools_result.tools
            if any(keyword in tool.description.lower()
                   for keyword in ["multiple", "pattern", "wildcard", "regex"])
        ]

        print(f"\n[OK] Checking tools with multiple operations ({len(multiple_op_tools)} tools)")

        for tool in multiple_op_tools:
            schema = tool.inputSchema
            properties = schema.get("properties", {})

            # Should have pattern property
            if "Multiple" in tool.description:
                self.assertIn("pattern", properties, f"{tool.name} should have pattern property")
                print(f"  [OK] {tool.name}: Supports pattern-based operations")

    def test_tool_count(self) -> None:
        """Test that we have the expected number of tools."""
        # Based on register_tools.py, we should have 22 tools
        expected_count = 22

        # We'll run this synchronously
        import asyncio
        result = asyncio.run(self.list_handler(
            mcp_types.ListToolsRequest(method="tools/list")
        ))
        tools_result = result.root

        actual_count = len(tools_result.tools)
        print(f"\n[OK] Tool count: {actual_count} tools (expected: {expected_count})")
        self.assertEqual(actual_count, expected_count)

    async def test_tool_categories(self) -> None:
        """Test and categorize all tools."""
        result = await self.list_handler(
            mcp_types.ListToolsRequest(method="tools/list")
        )
        tools_result = result.root

        categories: Dict[str, list] = {
            "Basic": [],
            "Scene Management": [],
            "GameObject Management": [],
            "Component Management": [],
            "Asset Management": [],
            "UI (uGUI)": [],
            "Project Settings": [],
            "Advanced": [],
        }

        for tool in tools_result.tools:
            name = tool.name
            if "ping" in name or "context" in name:
                categories["Basic"].append(name)
            elif "scene" in name:
                categories["Scene Management"].append(name)
            elif "gameobject" in name.lower() or "hierarchy" in name:
                categories["GameObject Management"].append(name)
            elif "component" in name:
                categories["Component Management"].append(name)
            elif "asset" in name:
                categories["Asset Management"].append(name)
            elif "ugui" in name.lower():
                categories["UI (uGUI)"].append(name)
            elif any(keyword in name for keyword in ["Settings", "compile", "tagLayer"]):
                categories["Project Settings"].append(name)
            else:
                categories["Advanced"].append(name)

        print("\n[OK] Tool Categories:")
        for category, tools in categories.items():
            if tools:
                print(f"\n  {category} ({len(tools)} tools):")
                for tool_name in tools:
                    print(f"    * {tool_name}")


def run_comprehensive_tests():
    """Run all comprehensive tests and generate a report."""
    suite = unittest.TestLoader().loadTestsFromTestCase(ComprehensiveToolTests)
    runner = unittest.TextTestRunner(verbosity=2)
    result = runner.run(suite)

    return result.wasSuccessful()


if __name__ == "__main__":
    import sys
    success = run_comprehensive_tests()
    sys.exit(0 if success else 1)
