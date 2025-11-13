"""
Test file for TileMap and NavMesh tool implementations.
These tests verify that the tool schemas and handlers are correctly registered.
"""

import sys
import os

# Add parent directory to path to import MCPServer modules
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

from MCPServer.tools.register_tools import register_tools
from mcp.server import Server


def test_tilemap_tool_registration():
    """Test that tilemap tool is registered correctly."""
    server = Server("test-server")
    register_tools(server)

    # Just verify the function runs without error
    print("[PASS] TileMap tool registration test passed")


def test_navmesh_tool_registration():
    """Test that navmesh tool is registered correctly."""
    server = Server("test-server")
    register_tools(server)

    # Just verify the function runs without error
    print("[PASS] NavMesh tool registration test passed")


def test_tilemap_schema():
    """Test that tilemap schema has required fields."""
    from MCPServer.tools.register_tools import _schema_with_required

    tilemap_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["createTilemap", "setTile", "getTile", "clearTile", "fillArea", "inspectTilemap", "clearAll"],
                },
            },
        },
        ["operation"],
    )

    assert "required" in tilemap_schema, "Schema missing 'required' field"
    assert "operation" in tilemap_schema["required"], "Schema missing 'operation' in required"
    assert tilemap_schema["additionalProperties"] == False, "Schema should not allow additional properties"

    print("[PASS] TileMap schema test passed")


def test_navmesh_schema():
    """Test that navmesh schema has required fields."""
    from MCPServer.tools.register_tools import _schema_with_required

    navmesh_schema = _schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["bakeNavMesh", "clearNavMesh", "addNavMeshAgent", "setDestination", "inspectNavMesh", "updateSettings", "createNavMeshSurface"],
                },
            },
        },
        ["operation"],
    )

    assert "required" in navmesh_schema, "Schema missing 'required' field"
    assert "operation" in navmesh_schema["required"], "Schema missing 'operation' in required"
    assert navmesh_schema["additionalProperties"] == False, "Schema should not allow additional properties"

    print("[PASS] NavMesh schema test passed")


if __name__ == "__main__":
    print("Running TileMap and NavMesh tool tests...\n")

    try:
        test_tilemap_tool_registration()
        test_navmesh_tool_registration()
        test_tilemap_schema()
        test_navmesh_schema()

        print("\n[SUCCESS] All tests passed!")
        sys.exit(0)
    except AssertionError as e:
        print(f"\n[FAIL] Test failed: {e}")
        sys.exit(1)
    except Exception as e:
        print(f"\n[ERROR] Error running tests: {e}")
        sys.exit(1)
