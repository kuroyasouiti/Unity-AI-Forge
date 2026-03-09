"""Tests for utils/alert.py module."""

from __future__ import annotations

import sys
from unittest.mock import MagicMock, patch

import pytest


class TestPlayCompilationAlert:
    """Tests for play_compilation_alert function."""

    def test_alert_disabled_via_env(self) -> None:
        """When MCP_COMPILATION_ALERT=0, alert should not play."""
        with patch.dict("os.environ", {"MCP_COMPILATION_ALERT": "0"}):
            # Re-import to pick up the env change
            import importlib

            import utils.alert as alert_module

            importlib.reload(alert_module)

            # On Windows, winsound should not be called
            with patch.object(alert_module, "_enabled", False):
                alert_module.play_compilation_alert()
                # No exception = success (nothing was called)

    def test_alert_enabled_by_default(self) -> None:
        """Alert should be enabled by default."""
        with patch.dict("os.environ", {}, clear=False):
            import importlib

            import utils.alert as alert_module

            # Remove MCP_COMPILATION_ALERT if set
            import os

            env_val = os.environ.pop("MCP_COMPILATION_ALERT", None)
            try:
                importlib.reload(alert_module)
                assert alert_module._enabled is True
            finally:
                if env_val is not None:
                    os.environ["MCP_COMPILATION_ALERT"] = env_val

    @pytest.mark.skipif(sys.platform != "win32", reason="Windows-only test")
    def test_alert_calls_winsound_on_windows(self) -> None:
        """On Windows, should call winsound.MessageBeep."""
        import utils.alert as alert_module

        with patch.object(alert_module, "_enabled", True):
            mock_winsound = MagicMock()
            mock_winsound.MB_ICONEXCLAMATION = 0x00000030
            with patch.dict("sys.modules", {"winsound": mock_winsound}):
                alert_module.play_compilation_alert()
                mock_winsound.MessageBeep.assert_called_once()

    def test_alert_handles_exception_gracefully(self) -> None:
        """Alert should not raise if underlying platform call fails."""
        import utils.alert as alert_module

        with patch.object(alert_module, "_enabled", True):
            # Force an exception regardless of platform
            if sys.platform == "win32":
                with patch.dict("sys.modules", {"winsound": None}):
                    # Should not raise
                    alert_module.play_compilation_alert()
            else:
                with patch("os.popen", side_effect=OSError("test")):
                    alert_module.play_compilation_alert()

    def test_disabled_values(self) -> None:
        """Various falsy env values should disable the alert."""
        import importlib

        import utils.alert as alert_module

        for val in ["0", "false", "no", "off", "False", "NO", "OFF"]:
            with patch.dict("os.environ", {"MCP_COMPILATION_ALERT": val}):
                importlib.reload(alert_module)
                assert alert_module._enabled is False, f"Expected disabled for '{val}'"

        # Restore
        import os

        os.environ.pop("MCP_COMPILATION_ALERT", None)
        importlib.reload(alert_module)
