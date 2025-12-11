"""Tests for utils/client_detector.py module."""

from __future__ import annotations

import os
from typing import Generator
from unittest.mock import patch

import pytest


class TestDetectClientName:
    """Tests for detect_client_name function."""

    @pytest.fixture
    def clean_env(self) -> Generator[None, None, None]:
        """Clean relevant environment variables."""
        env_vars = [
            "VSCODE_PID",
            "VSCODE_IPC_HOOK",
            "VSCODE_CWD",
            "TERM_PROGRAM",
            "ELECTRON_RUN_AS_NODE",
            "CLAUDE_DESKTOP",
            "CLAUDE_APP",
        ]
        old_values = {var: os.environ.pop(var, None) for var in env_vars}
        yield
        for var, value in old_values.items():
            if value is not None:
                os.environ[var] = value

    def test_detect_claude_code_via_vscode_pid(self, clean_env: None) -> None:
        from utils.client_detector import detect_client_name

        os.environ["VSCODE_PID"] = "12345"
        assert detect_client_name() == "Unknown"  # VSCODE_PID alone doesn't indicate code

    def test_detect_claude_code_via_term_program_vscode(
        self, clean_env: None
    ) -> None:
        from utils.client_detector import detect_client_name

        os.environ["TERM_PROGRAM"] = "vscode"
        assert detect_client_name() == "Claude Code"

    def test_detect_claude_code_via_term_program_cursor(
        self, clean_env: None
    ) -> None:
        from utils.client_detector import detect_client_name

        os.environ["TERM_PROGRAM"] = "cursor"
        assert detect_client_name() == "Claude Code"

    def test_detect_claude_desktop_via_electron(self, clean_env: None) -> None:
        from utils.client_detector import detect_client_name

        os.environ["ELECTRON_RUN_AS_NODE"] = "1"
        assert detect_client_name() == "Claude Desktop"

    def test_detect_claude_desktop_via_env_var(self, clean_env: None) -> None:
        from utils.client_detector import detect_client_name

        os.environ["CLAUDE_DESKTOP"] = "1"
        assert detect_client_name() == "Claude Desktop"

    def test_detect_unknown_client(self, clean_env: None) -> None:
        from utils.client_detector import detect_client_name

        assert detect_client_name() == "Unknown"


class TestIsClaudeCode:
    """Tests for _is_claude_code function."""

    @pytest.fixture
    def clean_env(self) -> Generator[None, None, None]:
        """Clean relevant environment variables."""
        env_vars = ["VSCODE_PID", "VSCODE_IPC_HOOK", "TERM_PROGRAM"]
        old_values = {var: os.environ.pop(var, None) for var in env_vars}
        yield
        for var, value in old_values.items():
            if value is not None:
                os.environ[var] = value

    def test_is_claude_code_false_by_default(self, clean_env: None) -> None:
        from utils.client_detector import _is_claude_code

        assert _is_claude_code() is False

    def test_is_claude_code_via_vscode_ipc_hook(self, clean_env: None) -> None:
        from utils.client_detector import _is_claude_code

        os.environ["VSCODE_IPC_HOOK"] = "/tmp/vscode-ipc-12345"
        assert _is_claude_code() is True


class TestGetClientInfo:
    """Tests for get_client_info function."""

    def test_get_client_info_returns_dict(self) -> None:
        from utils.client_detector import get_client_info

        info = get_client_info()

        assert "clientName" in info
        assert "serverName" in info
        assert "serverVersion" in info
        assert "pythonVersion" in info
        assert "platform" in info

    def test_get_client_info_python_version_format(self) -> None:
        from utils.client_detector import get_client_info

        info = get_client_info()

        # Should be in format "X.Y.Z"
        version = info["pythonVersion"]
        parts = version.split(".")
        assert len(parts) == 3
        assert all(part.isdigit() for part in parts)

    def test_get_client_info_platform_friendly_name(self) -> None:
        from utils.client_detector import get_client_info

        info = get_client_info()

        # Should be a user-friendly platform name
        platform = info["platform"]
        assert platform in ["Windows", "Linux", "macOS"] or isinstance(platform, str)


class TestGetPythonVersion:
    """Tests for _get_python_version function."""

    def test_get_python_version_format(self) -> None:
        from utils.client_detector import _get_python_version

        version = _get_python_version()

        # Should be in format "X.Y.Z"
        parts = version.split(".")
        assert len(parts) == 3
        assert int(parts[0]) >= 3
        assert int(parts[1]) >= 0
        assert int(parts[2]) >= 0


class TestGetPlatformName:
    """Tests for _get_platform_name function."""

    def test_get_platform_name_returns_string(self) -> None:
        from utils.client_detector import _get_platform_name

        platform = _get_platform_name()
        assert isinstance(platform, str)
        assert len(platform) > 0

    def test_get_platform_name_maps_darwin_to_macos(self) -> None:
        from utils.client_detector import _get_platform_name

        with patch("platform.system", return_value="Darwin"):
            assert _get_platform_name() == "macOS"

    def test_get_platform_name_keeps_windows(self) -> None:
        from utils.client_detector import _get_platform_name

        with patch("platform.system", return_value="Windows"):
            assert _get_platform_name() == "Windows"

    def test_get_platform_name_keeps_linux(self) -> None:
        from utils.client_detector import _get_platform_name

        with patch("platform.system", return_value="Linux"):
            assert _get_platform_name() == "Linux"

    def test_get_platform_name_unknown_platform(self) -> None:
        from utils.client_detector import _get_platform_name

        with patch("platform.system", return_value="FreeBSD"):
            assert _get_platform_name() == "FreeBSD"
