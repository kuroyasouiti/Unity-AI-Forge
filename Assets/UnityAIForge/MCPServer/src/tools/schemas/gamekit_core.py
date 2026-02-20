"""Schema definitions for GameKit core MCP tools."""

from __future__ import annotations

from typing import Any

from tools.schemas.common import schema_with_required


def gamekit_ui_command_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_ui_command MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["createCommandPanel", "addCommand", "inspect", "delete"],
                },
                "panelId": {"type": "string", "description": "Unique command panel identifier."},
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path for the UIDocument (optional, creates at scene root if omitted).",
                },
                "uiOutputDir": {
                    "type": "string",
                    "description": "Output directory for generated UXML/USS files (default: 'Assets/UI/Generated').",
                },
                "targetType": {
                    "type": "string",
                    "enum": ["actor", "manager"],
                    "description": "Target type: 'actor' for GameKitActor or 'manager' for GameKitManager.",
                },
                "targetActorId": {
                    "type": "string",
                    "description": "Target actor ID (when targetType is 'actor').",
                },
                "targetManagerId": {
                    "type": "string",
                    "description": "Target manager ID (when targetType is 'manager').",
                },
                "commands": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": {"type": "string"},
                            "label": {"type": "string"},
                            "icon": {"type": "string"},
                            "commandType": {
                                "type": "string",
                                "enum": [
                                    "move",
                                    "jump",
                                    "action",
                                    "look",
                                    "custom",
                                    "addResource",
                                    "setResource",
                                    "consumeResource",
                                    "changeState",
                                    "nextTurn",
                                    "triggerScene",
                                ],
                                "description": "Command type: Actor commands (move/jump/action/look/custom) or Manager commands (addResource/setResource/consumeResource/changeState/nextTurn/triggerScene).",
                            },
                            "commandParameter": {
                                "type": "string",
                                "description": "Parameter for action/resource/state commands.",
                            },
                            "resourceAmount": {
                                "type": "number",
                                "description": "Amount for resource commands (addResource/setResource/consumeResource).",
                            },
                            "moveDirection": {
                                "type": "object",
                                "properties": {
                                    "x": {"type": "number"},
                                    "y": {"type": "number"},
                                    "z": {"type": "number"},
                                },
                                "description": "Direction vector for move commands.",
                            },
                            "lookDirection": {
                                "type": "object",
                                "properties": {"x": {"type": "number"}, "y": {"type": "number"}},
                                "description": "Direction vector for look commands.",
                            },
                        },
                    },
                    "description": "List of commands to create as buttons in UXML.",
                },
                "layout": {
                    "type": "string",
                    "enum": ["horizontal", "vertical", "grid"],
                    "description": "Button layout style (maps to USS flex-direction).",
                },
            },
        },
        ["operation"],
    )
