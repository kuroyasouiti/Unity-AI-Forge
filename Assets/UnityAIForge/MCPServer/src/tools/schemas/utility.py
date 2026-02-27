"""Schema definitions for utility MCP tools."""

from __future__ import annotations

from typing import Any

from tools.schemas.common import schema_with_required


def ping_schema() -> dict[str, Any]:
    """Schema for the unity_ping MCP tool."""
    return {"type": "object", "properties": {}, "additionalProperties": False}


def compilation_await_schema() -> dict[str, Any]:
    """Schema for the unity_compilation_await MCP tool."""
    return {
        "type": "object",
        "properties": {
            "operation": {
                "type": "string",
                "enum": ["await", "status"],
                "description": "Operation to perform. 'await' waits for compilation; 'status' checks current state.",
            },
            "timeoutSeconds": {
                "type": "integer",
                "description": "Maximum time to wait for compilation to complete (default: 60 seconds).",
                "default": 60,
            },
        },
        "required": ["operation"],
        "additionalProperties": False,
    }


def playmode_control_schema() -> dict[str, Any]:
    """Schema for the unity_playmode_control MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "play",
                        "pause",
                        "unpause",
                        "stop",
                        "step",
                        "getState",
                        "captureState",
                        "waitForScene",
                        "validateState",
                    ],
                    "description": (
                        "PlayMode control operation. "
                        "'captureState': capture runtime state of specified GameObjects (requires play mode). "
                        "'waitForScene': check if a scene is loaded (poll until loaded=true). "
                        "'validateState': validate runtime manager state (requires play mode). "
                        "Checks that specified MonoBehaviours exist and their collections meet minimum counts."
                    ),
                },
                "targets": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": (
                        "GameObject paths to capture state for in captureState operation "
                        "(e.g., ['Player', 'GameManager'])."
                    ),
                },
                "includeConsole": {
                    "type": "boolean",
                    "description": "Include console log summary in captureState (default: false).",
                    "default": False,
                },
                "sceneName": {
                    "type": "string",
                    "description": "Scene name or path to wait for in waitForScene operation.",
                },
                "timeout": {
                    "type": "integer",
                    "description": (
                        "Guidance timeout in seconds for waitForScene. "
                        "The tool returns immediately; AI client should poll until loaded=true or timeout."
                    ),
                },
                "managers": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "type": {
                                "type": "string",
                                "description": "MonoBehaviour type name to find in scene.",
                            },
                            "field": {
                                "type": "string",
                                "description": "Field name (collection) to check count on.",
                            },
                            "minCount": {
                                "type": "integer",
                                "description": "Minimum required element count (default: 0).",
                                "default": 0,
                            },
                        },
                        "required": ["type"],
                    },
                    "description": (
                        "Array of manager specifications for validateState. "
                        "Each entry checks that a MonoBehaviour of the given type exists "
                        "and optionally that a collection field meets a minimum count."
                    ),
                },
            },
        },
        ["operation"],
    )


def console_log_schema() -> dict[str, Any]:
    """Schema for the unity_console_log MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "getRecent",
                        "getErrors",
                        "getWarnings",
                        "getLogs",
                        "clear",
                        "getCompilationErrors",
                        "getSummary",
                        "snapshot",
                        "diff",
                        "filter",
                    ],
                    "description": (
                        "Console log operation. "
                        "'snapshot': take a snapshot of current logs for later diff. "
                        "'diff': compare current logs against last snapshot, returning only new entries. "
                        "'filter': filter all logs by severity and/or keyword regex."
                    ),
                },
                "count": {
                    "type": "integer",
                    "description": "Number of logs to retrieve (default: 50 for getRecent, 100 for filtered).",
                },
                "severity": {
                    "type": "array",
                    "items": {
                        "type": "string",
                        "enum": ["error", "warning", "log"],
                    },
                    "description": (
                        "Filter by severity types. Used by snapshot, diff, and filter operations. "
                        "Example: ['error', 'warning'] to get only errors and warnings."
                    ),
                },
                "keyword": {
                    "type": "string",
                    "description": (
                        "Regex pattern to filter log messages. Used by diff and filter operations. "
                        "Falls back to literal match if regex is invalid."
                    ),
                },
                "limit": {
                    "type": "integer",
                    "description": "Maximum number of results to return for diff/filter (default: 100).",
                    "default": 100,
                },
            },
        },
        ["operation"],
    )
