"""Pytest configuration and fixtures for MCP server tests."""

from __future__ import annotations

import asyncio
import sys
from pathlib import Path
from typing import Any, Generator
from unittest.mock import AsyncMock, MagicMock

import pytest

# Add src to path for imports
src_path = Path(__file__).parent.parent / "src"
sys.path.insert(0, str(src_path))


@pytest.fixture(scope="session")
def event_loop() -> Generator[asyncio.AbstractEventLoop, None, None]:
    """Create an event loop for the test session."""
    loop = asyncio.new_event_loop()
    yield loop
    loop.close()


@pytest.fixture
def mock_bridge_manager() -> MagicMock:
    """Create a mock BridgeManager for testing."""
    mock = MagicMock()
    mock.is_connected.return_value = True
    mock.send_command = AsyncMock(return_value={"success": True, "result": {}})
    mock.get_session_id.return_value = "test-session-123"
    mock.get_context.return_value = None
    mock.get_last_heartbeat.return_value = 1234567890000
    return mock


@pytest.fixture
def mock_websocket() -> MagicMock:
    """Create a mock WebSocket connection."""
    from websockets.protocol import State as ConnectionState

    mock = MagicMock()
    mock.state = ConnectionState.OPEN
    mock.send = AsyncMock()
    mock.close = AsyncMock()
    return mock


@pytest.fixture
def temp_project_dir(tmp_path: Path) -> Path:
    """Create a temporary Unity project directory structure."""
    project_dir = tmp_path / "UnityProject"
    project_dir.mkdir()
    (project_dir / "Assets").mkdir()
    return project_dir
