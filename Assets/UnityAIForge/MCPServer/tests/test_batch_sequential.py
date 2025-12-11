"""Tests for tools/batch_sequential.py module."""

from __future__ import annotations

import asyncio
import json
from pathlib import Path
from typing import Any
from unittest.mock import AsyncMock, MagicMock, patch

import pytest


class TestBatchQueueState:
    """Tests for BatchQueueState class."""

    def test_init_defaults(self) -> None:
        from tools.batch_sequential import BatchQueueState

        state = BatchQueueState()
        assert state.operations == []
        assert state.current_index == 0
        assert state.last_error is None
        assert state.last_error_index is None
        assert state.started_at is None
        assert state.last_updated is None

    def test_to_dict(self) -> None:
        from tools.batch_sequential import BatchQueueState

        state = BatchQueueState()
        state.operations = [{"tool": "test", "arguments": {}}]
        state.current_index = 0
        state.started_at = "2025-01-01T00:00:00"

        result = state.to_dict()

        assert result["operations"] == [{"tool": "test", "arguments": {}}]
        assert result["current_index"] == 0
        assert result["total_count"] == 1
        assert result["remaining_count"] == 1
        assert result["completed_count"] == 0

    def test_from_dict(self) -> None:
        from tools.batch_sequential import BatchQueueState

        data = {
            "operations": [{"tool": "test", "arguments": {}}],
            "current_index": 1,
            "last_error": "test error",
            "last_error_index": 0,
            "started_at": "2025-01-01T00:00:00",
            "last_updated": "2025-01-01T00:01:00",
        }

        state = BatchQueueState.from_dict(data)

        assert state.operations == [{"tool": "test", "arguments": {}}]
        assert state.current_index == 1
        assert state.last_error == "test error"
        assert state.last_error_index == 0

    def test_save_and_load(self, tmp_path: Path) -> None:
        from tools.batch_sequential import BatchQueueState, STATE_FILE

        # Use a temporary state file
        temp_state_file = tmp_path / ".batch_queue_state.json"

        with patch("tools.batch_sequential.STATE_FILE", temp_state_file):
            state = BatchQueueState()
            state.operations = [{"tool": "unity_ping", "arguments": {}}]
            state.current_index = 0
            state._save_to_file()

            # Load and verify
            loaded = BatchQueueState._load_from_file()
            assert loaded.operations == state.operations
            assert loaded.current_index == state.current_index

    def test_clear(self, tmp_path: Path) -> None:
        from tools.batch_sequential import BatchQueueState

        temp_state_file = tmp_path / ".batch_queue_state.json"

        with patch("tools.batch_sequential.STATE_FILE", temp_state_file):
            state = BatchQueueState()
            state.operations = [{"tool": "test", "arguments": {}}]
            state.current_index = 1
            state.last_error = "error"
            state._save_to_file()

            assert temp_state_file.exists()

            state._clear()

            assert state.operations == []
            assert state.current_index == 0
            assert state.last_error is None
            assert not temp_state_file.exists()


class TestBatchQueueManager:
    """Tests for BatchQueueManager class."""

    def test_init(self, tmp_path: Path) -> None:
        from tools.batch_sequential import BatchQueueManager

        temp_state_file = tmp_path / ".batch_queue_state.json"

        with patch("tools.batch_sequential.STATE_FILE", temp_state_file):
            manager = BatchQueueManager()
            assert manager.state is not None
            assert manager.lock is not None

    def test_lock_is_asyncio_lock(self, tmp_path: Path) -> None:
        from tools.batch_sequential import BatchQueueManager

        temp_state_file = tmp_path / ".batch_queue_state.json"

        with patch("tools.batch_sequential.STATE_FILE", temp_state_file):
            manager = BatchQueueManager()
            assert isinstance(manager.lock, asyncio.Lock)

    @pytest.mark.asyncio
    async def test_thread_safe_access(self, tmp_path: Path) -> None:
        from tools.batch_sequential import BatchQueueManager

        temp_state_file = tmp_path / ".batch_queue_state.json"

        with patch("tools.batch_sequential.STATE_FILE", temp_state_file):
            manager = BatchQueueManager()

            async with manager.lock:
                state = manager.state
                state.operations = [{"tool": "test", "arguments": {}}]
                manager.save()

            # Verify state was saved
            async with manager.lock:
                assert manager.state.operations == [{"tool": "test", "arguments": {}}]


class TestExecuteBatchSequential:
    """Tests for execute_batch_sequential function."""

    @pytest.mark.asyncio
    async def test_execute_single_operation_success(
        self, mock_bridge_manager: MagicMock, tmp_path: Path
    ) -> None:
        from tools.batch_sequential import execute_batch_sequential

        temp_state_file = tmp_path / ".batch_queue_state.json"

        with patch("tools.batch_sequential.STATE_FILE", temp_state_file):
            # Reset global manager
            from tools import batch_sequential

            batch_sequential._batch_manager = batch_sequential.BatchQueueManager()

            operations = [{"tool": "unity_ping", "arguments": {}}]

            result = await execute_batch_sequential(
                bridge_client=mock_bridge_manager,
                operations=operations,
                resume=False,
                stop_on_error=True,
            )

            assert result["success"] is True
            assert result["total_operations"] == 1
            assert len(result["completed"]) == 1
            assert len(result["errors"]) == 0

    @pytest.mark.asyncio
    async def test_execute_stops_on_error(
        self, mock_bridge_manager: MagicMock, tmp_path: Path
    ) -> None:
        from tools.batch_sequential import execute_batch_sequential

        temp_state_file = tmp_path / ".batch_queue_state.json"

        # Make second operation fail
        mock_bridge_manager.send_command = AsyncMock(
            side_effect=[
                {"success": True, "result": {}},
                {"success": False, "error": "Test error"},
            ]
        )

        with patch("tools.batch_sequential.STATE_FILE", temp_state_file):
            from tools import batch_sequential

            batch_sequential._batch_manager = batch_sequential.BatchQueueManager()

            operations = [
                {"tool": "op1", "arguments": {}},
                {"tool": "op2", "arguments": {}},
                {"tool": "op3", "arguments": {}},
            ]

            result = await execute_batch_sequential(
                bridge_client=mock_bridge_manager,
                operations=operations,
                resume=False,
                stop_on_error=True,
            )

            assert result["success"] is False
            assert result["stopped_at_index"] == 1
            assert result["remaining_operations"] == 2
            assert len(result["completed"]) == 1
            assert len(result["errors"]) == 1

    @pytest.mark.asyncio
    async def test_execute_resume(
        self, mock_bridge_manager: MagicMock, tmp_path: Path
    ) -> None:
        from tools.batch_sequential import execute_batch_sequential

        temp_state_file = tmp_path / ".batch_queue_state.json"

        with patch("tools.batch_sequential.STATE_FILE", temp_state_file):
            from tools import batch_sequential

            batch_sequential._batch_manager = batch_sequential.BatchQueueManager()

            # First run - stop at operation 2
            mock_bridge_manager.send_command = AsyncMock(
                side_effect=[
                    {"success": True, "result": {}},
                    {"success": False, "error": "Test error"},
                ]
            )

            operations = [
                {"tool": "op1", "arguments": {}},
                {"tool": "op2", "arguments": {}},
                {"tool": "op3", "arguments": {}},
            ]

            result1 = await execute_batch_sequential(
                bridge_client=mock_bridge_manager,
                operations=operations,
                resume=False,
                stop_on_error=True,
            )

            assert result1["success"] is False
            assert result1["stopped_at_index"] == 1

            # Resume - all remaining operations succeed
            mock_bridge_manager.send_command = AsyncMock(
                return_value={"success": True, "result": {}}
            )

            result2 = await execute_batch_sequential(
                bridge_client=mock_bridge_manager,
                operations=[],  # Empty when resuming
                resume=True,
                stop_on_error=True,
            )

            assert result2["success"] is True

    @pytest.mark.asyncio
    async def test_execute_handles_exception(
        self, mock_bridge_manager: MagicMock, tmp_path: Path
    ) -> None:
        from tools.batch_sequential import execute_batch_sequential

        temp_state_file = tmp_path / ".batch_queue_state.json"

        mock_bridge_manager.send_command = AsyncMock(
            side_effect=RuntimeError("Connection lost")
        )

        with patch("tools.batch_sequential.STATE_FILE", temp_state_file):
            from tools import batch_sequential

            batch_sequential._batch_manager = batch_sequential.BatchQueueManager()

            operations = [{"tool": "op1", "arguments": {}}]

            result = await execute_batch_sequential(
                bridge_client=mock_bridge_manager,
                operations=operations,
                resume=False,
                stop_on_error=True,
            )

            assert result["success"] is False
            assert result["stopped_at_index"] == 0
            assert "Connection lost" in result["last_error"]
            assert result["errors"][0]["exception"] is True


class TestHandleBatchSequential:
    """Tests for handle_batch_sequential function."""

    @pytest.mark.asyncio
    async def test_handle_no_operations_error(
        self, mock_bridge_manager: MagicMock
    ) -> None:
        from tools.batch_sequential import handle_batch_sequential

        result = await handle_batch_sequential(
            arguments={},
            bridge_client=mock_bridge_manager,
        )

        assert len(result) == 1
        content = json.loads(result[0].text)
        assert content["success"] is False
        assert "No operations provided" in content["error"]

    @pytest.mark.asyncio
    async def test_handle_with_operations(
        self, mock_bridge_manager: MagicMock, tmp_path: Path
    ) -> None:
        from tools.batch_sequential import handle_batch_sequential

        temp_state_file = tmp_path / ".batch_queue_state.json"

        with patch("tools.batch_sequential.STATE_FILE", temp_state_file):
            from tools import batch_sequential

            batch_sequential._batch_manager = batch_sequential.BatchQueueManager()

            result = await handle_batch_sequential(
                arguments={
                    "operations": [{"tool": "unity_ping", "arguments": {}}],
                    "resume": False,
                    "stop_on_error": True,
                },
                bridge_client=mock_bridge_manager,
            )

            assert len(result) == 1
            content = json.loads(result[0].text)
            assert content["success"] is True
