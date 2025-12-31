"""MCP Server factory for Unity-AI-Forge.

Creates and configures the MCP server with all tools and resources.
"""

from __future__ import annotations

from mcp.server import Server

from prompts.loader import load_system_prompt
from resources.register_resources import register_resources
from tools.register_tools import register_tools
from version import SERVER_NAME, SERVER_VERSION


def create_mcp_server() -> Server:
    """Create and configure the Unity-AI-Forge MCP server.

    Returns:
        Configured MCP Server instance with all tools and resources registered.
    """
    # Load system prompt from external markdown file
    instructions = load_system_prompt(SERVER_VERSION)

    server = Server(
        SERVER_NAME,
        version=SERVER_VERSION,
        instructions=instructions,
    )

    register_resources(server)
    register_tools(server)

    return server
