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
                "enum": ["await"],
                "description": "Operation to perform. Currently only 'await' is supported.",
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
                    "enum": ["play", "pause", "unpause", "stop", "step", "getState"],
                    "description": "PlayMode control operation.",
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
                    ],
                    "description": "Console log operation.",
                },
                "count": {
                    "type": "integer",
                    "description": "Number of logs to retrieve (default: 50 for getRecent, 100 for filtered).",
                },
            },
        },
        ["operation"],
    )
