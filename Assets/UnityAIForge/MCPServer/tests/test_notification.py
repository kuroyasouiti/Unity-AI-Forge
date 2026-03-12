"""Tests for utils/notification.py module."""

from __future__ import annotations

from unittest.mock import patch

from utils.notification import _classify_result, notify_tool_result


class TestClassifyResult:
    """Tests for _classify_result helper."""

    def test_no_compilation_detected_returns_none(self) -> None:
        text = '{"wasCompiling": false, "message": "No compilation detected", "errorCount": 0}'
        assert _classify_result(text) is None

    def test_compilation_success_returns_success(self) -> None:
        text = '{"wasCompiling": true, "compilationCompleted": true, "success": true, "errorCount": 0}'
        assert _classify_result(text) == "success"

    def test_compilation_error_returns_error(self) -> None:
        text = '{"wasCompiling": true, "compilationCompleted": true, "success": false, "errorCount": 3}'
        assert _classify_result(text) == "error"

    def test_compilation_timeout_returns_error(self) -> None:
        text = '{"wasCompiling": true, "compilationCompleted": false, "timedOut": true, "success": false, "errorCount": 0}'
        assert _classify_result(text) == "error"

    def test_has_compilation_errors_returns_error(self) -> None:
        text = '{"success": true, "errorCount": 0, "compilationErrors": ["CS0103: error"]}'
        assert _classify_result(text) == "error"

    def test_nonzero_error_count_returns_warning(self) -> None:
        text = '{"success": true, "errorCount": 2}'
        assert _classify_result(text) == "warning"

    def test_standard_success_returns_success(self) -> None:
        text = '{"success": true, "errorCount": 0}'
        assert _classify_result(text) == "success"

    def test_empty_compilation_errors_not_flagged(self) -> None:
        text = '{"success": true, "errorCount": 0, "compilationErrors": []}'
        assert _classify_result(text) == "success"


class TestNotifyToolResult:
    """Tests for notify_tool_result function."""

    def test_mode_none_does_not_play(self) -> None:
        with patch("utils.notification.play_sound") as mock_play:
            notify_tool_result("unity_compilation_await", '{"success": true}', "none")
            mock_play.assert_not_called()

    def test_mode_compilation_plays_for_compilation_await(self) -> None:
        text = '{"wasCompiling": true, "compilationCompleted": true, "success": true, "errorCount": 0}'
        with patch("utils.notification.play_sound") as mock_play:
            notify_tool_result("unity_compilation_await", text, "compilation")
            mock_play.assert_called_once_with("success")

    def test_mode_compilation_skips_standard_tools(self) -> None:
        with patch("utils.notification.play_sound") as mock_play:
            notify_tool_result("unity_scene_crud", '{"success": true, "errorCount": 0}', "compilation")
            mock_play.assert_not_called()

    def test_mode_compilation_plays_for_asset_crud(self) -> None:
        text = '{"wasCompiling": true, "compilationCompleted": true, "success": true, "errorCount": 0}'
        with patch("utils.notification.play_sound") as mock_play:
            notify_tool_result("unity_asset_crud", text, "compilation")
            mock_play.assert_called_once_with("success")

    def test_mode_all_plays_for_standard_tools(self) -> None:
        with patch("utils.notification.play_sound") as mock_play:
            notify_tool_result("unity_scene_crud", '{"success": true, "errorCount": 0}', "all")
            mock_play.assert_called_once_with("success")

    def test_mode_all_plays_error_for_failure(self) -> None:
        with patch("utils.notification.play_sound") as mock_play:
            notify_tool_result("unity_scene_crud", '{"success": false, "errorCount": 1}', "all")
            mock_play.assert_called_once_with("error")

    def test_no_compilation_detected_no_sound(self) -> None:
        text = '{"wasCompiling": false, "message": "No compilation detected", "errorCount": 0}'
        with patch("utils.notification.play_sound") as mock_play:
            notify_tool_result("unity_compilation_await", text, "compilation")
            mock_play.assert_not_called()
