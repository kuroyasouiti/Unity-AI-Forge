"""Tests for config/env.py module."""

from __future__ import annotations

import json
import os
from pathlib import Path
from typing import Generator
from unittest.mock import patch

import pytest


class TestParseBool:
    """Tests for _parse_bool function."""

    def test_parse_bool_none_returns_default(self) -> None:
        from config.env import _parse_bool

        assert _parse_bool(None, True) is True
        assert _parse_bool(None, False) is False

    def test_parse_bool_true_values(self) -> None:
        from config.env import _parse_bool

        for value in ["1", "true", "True", "TRUE", "yes", "YES", "on", "ON"]:
            assert _parse_bool(value, False) is True

    def test_parse_bool_false_values(self) -> None:
        from config.env import _parse_bool

        for value in ["0", "false", "False", "FALSE", "no", "NO", "off", "OFF", ""]:
            assert _parse_bool(value, True) is False


class TestParseInt:
    """Tests for _parse_int function."""

    def test_parse_int_valid(self) -> None:
        from config.env import _parse_int

        assert _parse_int("42", 0) == 42
        assert _parse_int("0", 10) == 0
        assert _parse_int("-5", 0) == -5

    def test_parse_int_none_returns_default(self) -> None:
        from config.env import _parse_int

        assert _parse_int(None, 100) == 100

    def test_parse_int_invalid_returns_default(self) -> None:
        from config.env import _parse_int

        assert _parse_int("not_a_number", 50) == 50
        assert _parse_int("12.34", 50) == 50

    def test_parse_int_with_minimum(self) -> None:
        from config.env import _parse_int

        assert _parse_int("5", 10, minimum=10) == 10  # Below minimum, returns default
        assert _parse_int("15", 10, minimum=10) == 15  # Above minimum, returns value

    def test_parse_int_with_maximum(self) -> None:
        from config.env import _parse_int

        assert _parse_int("100", 10, maximum=50) == 10  # Above maximum, returns default
        assert _parse_int("30", 10, maximum=50) == 30  # Below maximum, returns value


class TestParseLogLevel:
    """Tests for _parse_log_level function."""

    def test_parse_log_level_valid(self) -> None:
        from config.env import _parse_log_level

        assert _parse_log_level("debug") == "debug"
        assert _parse_log_level("DEBUG") == "debug"
        assert _parse_log_level("info") == "info"
        assert _parse_log_level("error") == "error"
        assert _parse_log_level("trace") == "trace"

    def test_parse_log_level_invalid_returns_info(self) -> None:
        from config.env import _parse_log_level

        assert _parse_log_level("invalid") == "info"
        assert _parse_log_level("") == "info"
        assert _parse_log_level(None) == "info"


class TestLoadBridgeToken:
    """Tests for _load_bridge_token function."""

    @pytest.fixture
    def clean_env(self) -> Generator[None, None, None]:
        """Clean up environment variables."""
        old_token = os.environ.pop("MCP_BRIDGE_TOKEN", None)
        yield
        if old_token is not None:
            os.environ["MCP_BRIDGE_TOKEN"] = old_token

    def test_load_from_env_variable(self, clean_env: None) -> None:
        from config.env import _load_bridge_token

        os.environ["MCP_BRIDGE_TOKEN"] = "test-token-123"
        assert _load_bridge_token() == "test-token-123"

    def test_load_from_json_file(
        self, clean_env: None, temp_project_dir: Path
    ) -> None:
        from config import env

        # Create token file
        token_file = temp_project_dir / ".mcp_bridge_tokens.json"
        token_file.write_text(json.dumps({"tokens": ["json-token-456"]}))

        # Patch project root
        with patch.object(env, "_project_root", temp_project_dir):
            # Re-import to use patched _project_root
            token = env._load_bridge_token()
            assert token == "json-token-456"

    def test_load_from_legacy_file(
        self, clean_env: None, temp_project_dir: Path
    ) -> None:
        from config import env

        # Create legacy token file
        legacy_file = temp_project_dir / ".mcp_bridge_token"
        legacy_file.write_text("legacy-token-789")

        with patch.object(env, "_project_root", temp_project_dir):
            token = env._load_bridge_token()
            assert token == "legacy-token-789"

    def test_load_returns_none_when_no_token(
        self, clean_env: None, temp_project_dir: Path
    ) -> None:
        from config import env

        with patch.object(env, "_project_root", temp_project_dir):
            token = env._load_bridge_token()
            assert token is None

    def test_load_handles_invalid_json(
        self, clean_env: None, temp_project_dir: Path
    ) -> None:
        from config import env

        # Create invalid JSON file
        token_file = temp_project_dir / ".mcp_bridge_tokens.json"
        token_file.write_text("not valid json {")

        with patch.object(env, "_project_root", temp_project_dir):
            # Should not raise, should return None
            token = env._load_bridge_token()
            assert token is None


class TestCliOverrides:
    """Tests for CLI override functionality."""

    def test_apply_cli_overrides(self) -> None:
        from config import env

        # Reset globals
        env._cli_bridge_token = None
        env._cli_bridge_host = None
        env._cli_bridge_port = None

        env.apply_cli_overrides(
            bridge_token="cli-token",
            bridge_host="192.168.1.1",
            bridge_port=8080,
        )

        assert env._cli_bridge_token == "cli-token"
        assert env._cli_bridge_host == "192.168.1.1"
        assert env._cli_bridge_port == 8080

    def test_apply_cli_overrides_partial(self) -> None:
        from config import env

        # Reset globals
        env._cli_bridge_token = None
        env._cli_bridge_host = None
        env._cli_bridge_port = None

        env.apply_cli_overrides(bridge_token="only-token")

        assert env._cli_bridge_token == "only-token"
        assert env._cli_bridge_host is None
        assert env._cli_bridge_port is None
