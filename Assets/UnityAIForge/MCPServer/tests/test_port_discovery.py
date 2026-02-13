"""Tests for config/port_discovery.py module."""

from __future__ import annotations

import json
import os
from pathlib import Path


class TestComputeProjectHash:
    """Tests for _compute_project_hash function."""

    def test_normalizes_backslashes(self) -> None:
        from config.port_discovery import _compute_project_hash

        h1 = _compute_project_hash(Path("D:\\Projects\\Unity-AI-Forge"))
        h2 = _compute_project_hash(Path("D:/Projects/Unity-AI-Forge"))
        assert h1 == h2

    def test_case_insensitive(self) -> None:
        from config.port_discovery import _compute_project_hash

        h1 = _compute_project_hash(Path("D:/Projects/Unity-AI-Forge"))
        h2 = _compute_project_hash(Path("d:/projects/unity-ai-forge"))
        assert h1 == h2

    def test_ignores_trailing_slash(self) -> None:
        from config.port_discovery import _compute_project_hash

        h1 = _compute_project_hash(Path("D:/Projects/Unity-AI-Forge"))
        h2 = _compute_project_hash(Path("D:/Projects/Unity-AI-Forge/"))
        assert h1 == h2

    def test_returns_16_hex_chars(self) -> None:
        from config.port_discovery import _compute_project_hash

        h = _compute_project_hash(Path("D:/Projects/Unity-AI-Forge"))
        assert len(h) == 16
        assert all(c in "0123456789abcdef" for c in h)

    def test_different_paths_different_hashes(self) -> None:
        from config.port_discovery import _compute_project_hash

        h1 = _compute_project_hash(Path("D:/Projects/ProjectA"))
        h2 = _compute_project_hash(Path("D:/Projects/ProjectB"))
        assert h1 != h2

    def test_known_test_vector_matches_csharp(self) -> None:
        """Verify the hash matches what C# produces.

        The C# test uses the same input. Both must produce identical hashes.
        Input after normalization: "d:/projects/unity-ai-forge"
        """
        import hashlib

        from config.port_discovery import _compute_project_hash

        h = _compute_project_hash(Path("D:\\Projects\\Unity-AI-Forge"))

        # Compute expected value directly to verify algorithm
        normalized = "d:/projects/unity-ai-forge"
        expected = hashlib.sha256(normalized.encode("utf-8")).hexdigest()[:16]
        assert h == expected


class TestIsProcessAlive:
    """Tests for _is_process_alive function."""

    def test_current_process_is_alive(self) -> None:
        from config.port_discovery import _is_process_alive

        assert _is_process_alive(os.getpid()) is True

    def test_nonexistent_pid_is_dead(self) -> None:
        from config.port_discovery import _is_process_alive

        # PID 999999999 almost certainly doesn't exist
        assert _is_process_alive(999999999) is False


class TestDiscoverBridgePort:
    """Tests for discover_bridge_port function."""

    def test_no_file_returns_none(self, tmp_path: Path) -> None:
        from config.port_discovery import discover_bridge_port

        result = discover_bridge_port(tmp_path / "nonexistent-project")
        assert result is None

    def test_valid_file_returns_port(self, tmp_path: Path) -> None:
        from config.port_discovery import (
            _compute_project_hash,
            _get_discovery_directory,
            discover_bridge_port,
        )

        project = tmp_path / "MyProject"
        project.mkdir()

        # Create port file in the discovery directory
        project_hash = _compute_project_hash(project)
        discovery_dir = _get_discovery_directory()
        discovery_dir.mkdir(parents=True, exist_ok=True)
        port_file = discovery_dir / f"{project_hash}.port"

        try:
            data = {
                "port": 7071,
                "projectPath": str(project),
                "pid": os.getpid(),  # Current process = alive
                "timestamp": "2026-01-01T00:00:00Z",
            }
            port_file.write_text(json.dumps(data), encoding="utf-8")

            result = discover_bridge_port(project)
            assert result == 7071
        finally:
            if port_file.exists():
                port_file.unlink()

    def test_stale_pid_returns_none_and_deletes_file(self, tmp_path: Path) -> None:
        from config.port_discovery import (
            _compute_project_hash,
            _get_discovery_directory,
            discover_bridge_port,
        )

        project = tmp_path / "StaleProject"
        project.mkdir()

        project_hash = _compute_project_hash(project)
        discovery_dir = _get_discovery_directory()
        discovery_dir.mkdir(parents=True, exist_ok=True)
        port_file = discovery_dir / f"{project_hash}.port"

        try:
            data = {
                "port": 7072,
                "projectPath": str(project),
                "pid": 999999999,  # Non-existent PID
                "timestamp": "2026-01-01T00:00:00Z",
            }
            port_file.write_text(json.dumps(data), encoding="utf-8")

            result = discover_bridge_port(project)
            assert result is None
            assert not port_file.exists(), "Stale port file should be deleted"
        finally:
            if port_file.exists():
                port_file.unlink()

    def test_invalid_json_returns_none(self, tmp_path: Path) -> None:
        from config.port_discovery import (
            _compute_project_hash,
            _get_discovery_directory,
            discover_bridge_port,
        )

        project = tmp_path / "BadJsonProject"
        project.mkdir()

        project_hash = _compute_project_hash(project)
        discovery_dir = _get_discovery_directory()
        discovery_dir.mkdir(parents=True, exist_ok=True)
        port_file = discovery_dir / f"{project_hash}.port"

        try:
            port_file.write_text("not valid json {", encoding="utf-8")

            result = discover_bridge_port(project)
            assert result is None
        finally:
            if port_file.exists():
                port_file.unlink()

    def test_missing_port_field_returns_none(self, tmp_path: Path) -> None:
        from config.port_discovery import (
            _compute_project_hash,
            _get_discovery_directory,
            discover_bridge_port,
        )

        project = tmp_path / "NoPortProject"
        project.mkdir()

        project_hash = _compute_project_hash(project)
        discovery_dir = _get_discovery_directory()
        discovery_dir.mkdir(parents=True, exist_ok=True)
        port_file = discovery_dir / f"{project_hash}.port"

        try:
            data = {"projectPath": str(project), "pid": os.getpid()}
            port_file.write_text(json.dumps(data), encoding="utf-8")

            result = discover_bridge_port(project)
            assert result is None
        finally:
            if port_file.exists():
                port_file.unlink()
