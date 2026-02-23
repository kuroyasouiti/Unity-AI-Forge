"""Tests for server/create_mcp_server.py module."""

from __future__ import annotations

from unittest.mock import patch

import server.create_mcp_server as _mod


class TestCreateMcpServer:
    """Tests for create_mcp_server factory function."""

    def test_returns_server_instance(self) -> None:
        server = _mod.create_mcp_server()
        assert server is not None

    def test_server_has_name(self) -> None:
        from version import SERVER_NAME

        server = _mod.create_mcp_server()
        assert server.name == SERVER_NAME

    def test_calls_register_tools(self) -> None:
        with patch.object(_mod, "register_tools") as mock_register:
            server = _mod.create_mcp_server()
            mock_register.assert_called_once_with(server)

    def test_calls_register_resources(self) -> None:
        with patch.object(_mod, "register_resources") as mock_register:
            server = _mod.create_mcp_server()
            mock_register.assert_called_once_with(server)

    def test_calls_register_prompts(self) -> None:
        with patch.object(_mod, "register_prompts") as mock_register:
            server = _mod.create_mcp_server()
            mock_register.assert_called_once_with(server)

    def test_loads_system_prompt(self) -> None:
        with patch.object(_mod, "load_system_prompt", return_value="test instructions") as mock_load:
            server = _mod.create_mcp_server()
            mock_load.assert_called_once()
