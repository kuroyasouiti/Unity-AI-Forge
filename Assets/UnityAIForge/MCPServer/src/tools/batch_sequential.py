"""
Sequential batch execution tool with resume capability.

Executes operations sequentially, stops on error, and allows resuming from the failed point.

Thread Safety:
    This module uses asyncio.Lock to ensure thread-safe access to the batch queue state.
    All state modifications are protected by the lock to prevent race conditions when
    multiple clients execute batch operations concurrently.
"""

from __future__ import annotations

import asyncio
import json
import logging
from datetime import datetime
from pathlib import Path
from typing import Any

from mcp.types import TextContent, Tool

from bridge.bridge_manager import BridgeManager

logger = logging.getLogger(__name__)

# State file to persist queue
STATE_FILE = Path(__file__).parent.parent.parent / ".batch_queue_state.json"

# MCP tool name to internal bridge tool name mapping
# Users can use either the MCP name (e.g., "unity_gameobject_crud")
# or the internal name (e.g., "gameObject")
# NOTE: Internal names are taken from handler Category properties
TOOL_NAME_MAPPING: dict[str, str] = {
    # Utility
    "unity_ping": "ping",
    "unity_compilation_await": "compilationAwait",
    # Low-Level CRUD
    "unity_scene_crud": "scene",
    "unity_gameobject_crud": "gameObject",
    "unity_component_crud": "component",
    "unity_asset_crud": "asset",
    "unity_scriptableObject_crud": "scriptableObject",
    "unity_prefab_crud": "prefab",
    "unity_vector_sprite_convert": "sprite",
    "unity_projectSettings_crud": "projectSettingsManage",
    # Mid-Level Batch
    "unity_transform_batch": "transformBatch",
    "unity_rectTransform_batch": "rectTransformBatch",
    "unity_physics_bundle": "physicsBundle",
    "unity_camera_rig": "cameraRig",
    "unity_ui_foundation": "uiFoundation",
    "unity_audio_source_bundle": "audioSourceBundle",
    "unity_input_profile": "inputProfile",
    "unity_character_controller_bundle": "characterControllerBundle",
    "unity_tilemap_bundle": "tilemapBundle",
    "unity_sprite2d_bundle": "sprite2DBundle",
    "unity_animation2d_bundle": "animation2DBundle",
    # UI Management
    "unity_ui_hierarchy": "uiHierarchy",
    "unity_ui_state": "uiState",
    "unity_ui_navigation": "uiNavigation",
    # Development Cycle & Visual Tools
    "unity_playmode_control": "playModeControl",
    "unity_console_log": "consoleLog",
    "unity_material_bundle": "materialBundle",
    "unity_light_bundle": "lightBundle",
    "unity_particle_bundle": "particleBundle",
    "unity_animation3d_bundle": "animation3DBundle",
    "unity_event_wiring": "eventWiring",
    # High-Level GameKit - Core
    "unity_gamekit_actor": "gamekitActor",
    "unity_gamekit_manager": "gamekitManager",
    "unity_gamekit_interaction": "gamekitInteraction",
    "unity_gamekit_ui_command": "gamekitUICommand",
    "unity_gamekit_machinations": "gamekitMachinations",
    "unity_gamekit_sceneflow": "gamekitSceneFlow",
    # High-Level GameKit - Phase 1
    "unity_gamekit_health": "gamekitHealth",
    "unity_gamekit_spawner": "gamekitSpawner",
    "unity_gamekit_timer": "gamekitTimer",
    "unity_gamekit_ai": "gamekitAI",
    "unity_gamekit_collectible": "gamekitCollectible",
    "unity_gamekit_projectile": "gamekitProjectile",
    "unity_gamekit_waypoint": "gamekitWaypoint",
    "unity_gamekit_trigger_zone": "gamekitTriggerZone",
    "unity_gamekit_animation_sync": "gamekitAnimationSync",
    "unity_gamekit_effect": "gamekitEffect",
    # High-Level GameKit - Phase 2
    "unity_gamekit_save": "gamekitSave",
    "unity_gamekit_inventory": "gamekitInventory",
    "unity_gamekit_dialogue": "gamekitDialogue",
    "unity_gamekit_quest": "gamekitQuest",
    "unity_gamekit_status_effect": "gamekitStatusEffect",
    # High-Level GameKit - 3-Pillar Architecture (UI)
    "unity_gamekit_ui_binding": "gamekitUIBinding",
    "unity_gamekit_ui_list": "gamekitUIList",
    "unity_gamekit_ui_slot": "gamekitUISlot",
    "unity_gamekit_ui_selection": "gamekitUISelection",
    # High-Level GameKit - 3-Pillar Architecture (Logic)
    "unity_gamekit_combat": "gamekitCombat",
    # High-Level GameKit - 3-Pillar Architecture (Presentation)
    "unity_gamekit_feedback": "gamekitFeedback",
    "unity_gamekit_vfx": "gamekitVFX",
    "unity_gamekit_audio": "gamekitAudio",
}


def resolve_tool_name(tool_name: str) -> str:
    """Resolve MCP tool name to internal bridge tool name.

    Args:
        tool_name: Either MCP name (e.g., "unity_gameobject_crud")
                   or internal name (e.g., "gameObjectManage")

    Returns:
        Internal bridge tool name

    Raises:
        ValueError: If tool name is not recognized
    """
    # Check if it's an MCP name that needs mapping
    if tool_name in TOOL_NAME_MAPPING:
        return TOOL_NAME_MAPPING[tool_name]

    # Check if it's already an internal name (exists as a value in mapping)
    if tool_name in TOOL_NAME_MAPPING.values():
        return tool_name

    # Unknown tool name
    raise ValueError(
        f"Unsupported tool name: {tool_name}. "
        f"Use MCP names (e.g., 'unity_gameobject_crud') or internal names (e.g., 'gameObjectManage'). "
        f"Available tools: {', '.join(sorted(TOOL_NAME_MAPPING.keys()))}"
    )


class BatchQueueState:
    """Manages the state of the batch queue.

    This class is NOT thread-safe by itself. Use BatchQueueManager for
    thread-safe access to the batch queue state.
    """

    def __init__(self) -> None:
        self.operations: list[dict[str, Any]] = []
        self.current_index: int = 0
        self.last_error: str | None = None
        self.last_error_index: int | None = None
        self.started_at: str | None = None
        self.last_updated: str | None = None

    def to_dict(self) -> dict[str, Any]:
        """Convert state to dictionary."""
        return {
            "operations": self.operations,
            "current_index": self.current_index,
            "last_error": self.last_error,
            "last_error_index": self.last_error_index,
            "started_at": self.started_at,
            "last_updated": self.last_updated,
            "remaining_count": len(self.operations) - self.current_index,
            "completed_count": self.current_index,
            "total_count": len(self.operations),
        }

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> BatchQueueState:
        """Create state from dictionary."""
        state = cls()
        state.operations = data.get("operations", [])
        state.current_index = data.get("current_index", 0)
        state.last_error = data.get("last_error")
        state.last_error_index = data.get("last_error_index")
        state.started_at = data.get("started_at")
        state.last_updated = data.get("last_updated")
        return state

    def _save_to_file(self) -> None:
        """Save state to file (internal, not thread-safe)."""
        try:
            STATE_FILE.parent.mkdir(parents=True, exist_ok=True)
            with open(STATE_FILE, "w", encoding="utf-8") as f:
                json.dump(self.to_dict(), f, indent=2, ensure_ascii=False)
            logger.info(
                "Batch queue state saved: %d/%d",
                self.current_index,
                len(self.operations),
            )
        except OSError as exc:
            logger.error("Failed to save batch queue state: %s", exc)

    @classmethod
    def _load_from_file(cls) -> BatchQueueState:
        """Load state from file (internal, not thread-safe)."""
        try:
            if STATE_FILE.exists():
                with open(STATE_FILE, encoding="utf-8") as f:
                    data = json.load(f)
                logger.info(
                    "Batch queue state loaded: %d/%d",
                    data.get("current_index", 0),
                    data.get("total_count", 0),
                )
                return cls.from_dict(data)
        except json.JSONDecodeError as exc:
            logger.error("Failed to parse batch queue state file: %s", exc)
        except OSError as exc:
            logger.error("Failed to load batch queue state: %s", exc)
        return cls()

    def _clear(self) -> None:
        """Clear the state (internal, not thread-safe)."""
        self.operations = []
        self.current_index = 0
        self.last_error = None
        self.last_error_index = None
        self.started_at = None
        self.last_updated = None
        if STATE_FILE.exists():
            try:
                STATE_FILE.unlink()
            except OSError as exc:
                logger.warning("Failed to delete batch queue state file: %s", exc)
        logger.info("Batch queue state cleared")


class BatchQueueManager:
    """Thread-safe manager for batch queue state.

    This class wraps BatchQueueState with asyncio.Lock to provide
    thread-safe access to the batch queue state. All public methods
    acquire the lock before accessing or modifying the state.

    Usage:
        manager = BatchQueueManager()
        async with manager.lock:
            state = manager.state
            # ... modify state ...
            manager.save()
    """

    def __init__(self) -> None:
        self._lock = asyncio.Lock()
        self._state = BatchQueueState._load_from_file()

    @property
    def lock(self) -> asyncio.Lock:
        """Get the lock for thread-safe access."""
        return self._lock

    @property
    def state(self) -> BatchQueueState:
        """Get the state (caller must hold lock)."""
        return self._state

    def save(self) -> None:
        """Save state to file (caller must hold lock)."""
        self._state._save_to_file()

    def clear(self) -> None:
        """Clear the state (caller must hold lock)."""
        self._state._clear()

    def get_state_dict(self) -> dict[str, Any]:
        """Get state as dictionary (caller must hold lock)."""
        return self._state.to_dict()


# Global thread-safe manager instance
_batch_manager = BatchQueueManager()


def get_batch_manager() -> BatchQueueManager:
    """Get the batch queue manager for thread-safe state access."""
    return _batch_manager


def get_batch_state() -> BatchQueueState:
    """Get the current batch state (for backwards compatibility).

    WARNING: This returns the state without locking. For thread-safe access,
    use get_batch_manager() and acquire the lock before accessing state.
    """
    return _batch_manager.state


async def execute_batch_sequential(
    bridge_client: BridgeManager,
    operations: list[dict[str, Any]],
    resume: bool = False,
    stop_on_error: bool = True,
) -> dict[str, Any]:
    """Execute operations sequentially with resume capability.

    This function is thread-safe and uses asyncio.Lock to prevent race conditions
    when multiple clients execute batch operations concurrently.

    Args:
        bridge_client: Unity bridge client
        operations: List of operations to execute. Each operation should have:
                   - tool: Tool name (e.g., "unity_gameobject_crud")
                   - arguments: Tool arguments as dict
        resume: If True, resume from previous error point. If False, start fresh.
        stop_on_error: If True, stop on first error. If False, continue.

    Returns:
        Dict with execution results and status
    """
    async with _batch_manager.lock:
        state = _batch_manager.state
        current_time = datetime.utcnow().isoformat()

        # Initialize or resume
        if not resume or not state.operations:
            # Start fresh
            state.operations = operations
            state.current_index = 0
            state.last_error = None
            state.last_error_index = None
            state.started_at = current_time
            logger.info("Starting new batch execution with %d operations", len(operations))
        else:
            # Resume from saved state
            logger.info(
                "Resuming batch execution from operation %d/%d",
                state.current_index,
                len(state.operations),
            )

        state.last_updated = current_time
        _batch_manager.save()

        results: list[dict[str, Any]] = []
        errors: list[dict[str, Any]] = []

        # Execute operations sequentially
        while state.current_index < len(state.operations):
            idx = state.current_index
            operation = state.operations[idx]

            original_tool_name = operation.get("tool")
            arguments = operation.get("arguments", {})

            # Resolve MCP tool name to internal bridge tool name
            try:
                tool_name = resolve_tool_name(original_tool_name)
            except ValueError as exc:
                error_msg = str(exc)
                errors.append(
                    {
                        "index": idx,
                        "tool": original_tool_name,
                        "error": error_msg,
                        "exception": True,
                    }
                )
                state.last_error = error_msg
                state.last_error_index = idx
                logger.error("Tool name resolution failed for operation %d: %s", idx + 1, error_msg)

                if stop_on_error:
                    _batch_manager.save()
                    return {
                        "success": False,
                        "stopped_at_index": idx,
                        "completed": results,
                        "errors": errors,
                        "remaining_operations": len(state.operations) - state.current_index,
                        "message": f"Execution stopped at operation {idx + 1} due to invalid tool name.",
                        "last_error": error_msg,
                    }
                # Continue to next operation if not stopping on error
                state.current_index += 1
                _batch_manager.save()
                continue

            logger.info(
                "Executing operation %d/%d: %s (resolved: %s)",
                idx + 1,
                len(state.operations),
                original_tool_name,
                tool_name,
            )

            try:
                # Send operation to Unity bridge
                response = await bridge_client.send_command(tool_name, arguments)

                if response.get("success"):
                    results.append(
                        {
                            "index": idx,
                            "tool": original_tool_name,
                            "success": True,
                            "result": response.get("result"),
                        }
                    )
                    logger.info("Operation %d completed successfully", idx + 1)
                else:
                    # Operation failed
                    error_msg = response.get("error", "Unknown error")
                    errors.append(
                        {
                            "index": idx,
                            "tool": original_tool_name,
                            "error": error_msg,
                        }
                    )
                    state.last_error = error_msg
                    state.last_error_index = idx
                    logger.error("Operation %d failed: %s", idx + 1, error_msg)

                    if stop_on_error:
                        _batch_manager.save()
                        return {
                            "success": False,
                            "stopped_at_index": idx,
                            "completed": results,
                            "errors": errors,
                            "remaining_operations": len(state.operations) - state.current_index,
                            "message": f"Execution stopped at operation {idx + 1} due to error. Use resume=true to continue.",
                            "last_error": error_msg,
                        }

            except Exception as exc:
                error_msg = str(exc)
                errors.append(
                    {
                        "index": idx,
                        "tool": original_tool_name,
                        "error": error_msg,
                        "exception": True,
                    }
                )
                state.last_error = error_msg
                state.last_error_index = idx
                logger.exception("Exception in operation %d", idx + 1)

                if stop_on_error:
                    _batch_manager.save()
                    return {
                        "success": False,
                        "stopped_at_index": idx,
                        "completed": results,
                        "errors": errors,
                        "remaining_operations": len(state.operations) - state.current_index,
                        "message": f"Execution stopped at operation {idx + 1} due to exception. Use resume=true to continue.",
                        "last_error": error_msg,
                    }

            # Move to next operation
            state.current_index += 1
            _batch_manager.save()

        # All operations completed
        _batch_manager.clear()

        return {
            "success": len(errors) == 0,
            "completed": results,
            "errors": errors,
            "total_operations": len(operations),
            "message": (
                f"All {len(operations)} operations completed successfully."
                if len(errors) == 0
                else f"Completed with {len(errors)} error(s)."
            ),
        }


# Tool definition
TOOL = Tool(
    name="unity_batch_sequential_execute",
    description="""Execute multiple Unity operations sequentially with resume capability.

This tool executes operations one by one in order. If an error occurs, execution stops and the remaining operations are saved. You can resume from the failed operation by calling the tool again with resume=true.

Key features:
- Sequential execution (one operation at a time)
- Stops on first error
- Saves remaining operations for resume
- Check remaining operations via unity_batch_queue_status resource

Use cases:
- Multi-step scene setup that might fail midway
- Batch GameObject creation with dependencies
- Sequential configuration that must succeed in order
- Any workflow where you want to retry from failure point""",
    inputSchema={
        "type": "object",
        "properties": {
            "operations": {
                "type": "array",
                "description": "List of operations to execute. Each operation has 'tool' and 'arguments' fields.",
                "items": {
                    "type": "object",
                    "properties": {
                        "tool": {
                            "type": "string",
                            "description": "Tool name (e.g., 'unity_gameobject_crud', 'unity_component_crud')",
                        },
                        "arguments": {
                            "type": "object",
                            "description": "Tool arguments as a dictionary",
                        },
                    },
                    "required": ["tool", "arguments"],
                },
            },
            "resume": {
                "type": "boolean",
                "description": "If true, resume from previous failure point. If false, start fresh (clears saved queue).",
                "default": False,
            },
            "stop_on_error": {
                "type": "boolean",
                "description": "If true, stop on first error. If false, continue (not recommended for sequential workflows).",
                "default": True,
            },
        },
        "required": [],
    },
)


async def handle_batch_sequential(
    arguments: dict[str, Any], bridge_client: BridgeManager
) -> list[TextContent]:
    """Handle the unity_batch_sequential_execute tool call."""
    operations = arguments.get("operations", [])
    resume = arguments.get("resume", False)
    stop_on_error = arguments.get("stop_on_error", True)

    # Validate operations
    if not resume and not operations:
        return [
            TextContent(
                type="text",
                text=json.dumps(
                    {
                        "success": False,
                        "error": "No operations provided. Specify 'operations' array or set 'resume' to true.",
                    },
                    indent=2,
                ),
            )
        ]

    # Execute batch
    result = await execute_batch_sequential(
        bridge_client=bridge_client,
        operations=operations,
        resume=resume,
        stop_on_error=stop_on_error,
    )

    return [TextContent(type="text", text=json.dumps(result, indent=2, ensure_ascii=False))]
