"""
MCP Resources for Unity-AI-Forge
"""

from __future__ import annotations

from mcp import types as mcp_types
from mcp.server import Server

from resources.batch_queue import get_batch_queue_resources, read_batch_queue_resource


def register_resources(server: Server) -> None:
    """Register MCP resources.

    Resources provide read-only access to server state and information.
    """

    @server.list_resources()
    async def list_resources() -> list[mcp_types.Resource]:
        """List all available resources."""
        resources: list[mcp_types.Resource] = []
        resources.extend(get_batch_queue_resources())
        return resources

    @server.read_resource()
    async def read_resource(uri: str) -> str:
        """Read a resource by URI."""
        # Batch queue resources
        if uri.startswith("batch://"):
            return await read_batch_queue_resource(uri)

        raise ValueError(f"Unknown resource URI: {uri}")
