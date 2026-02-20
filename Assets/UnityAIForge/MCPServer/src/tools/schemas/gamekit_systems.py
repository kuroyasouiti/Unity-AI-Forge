"""Schema definitions for GameKit systems MCP tools (Presentation Pillar)."""

from __future__ import annotations

from typing import Any

from tools.schemas.common import schema_with_required


def gamekit_animation_sync_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_animation_sync MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "inspect",
                        "delete",
                        "addSyncRule",
                        "removeSyncRule",
                        "addTriggerRule",
                        "removeTriggerRule",
                        "fireTrigger",
                        "setParameter",
                        "findBySyncId",
                    ],
                    "description": "Animation sync operation to perform.",
                },
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject hierarchy path.",
                },
                "syncId": {"type": "string", "description": "Unique animation sync identifier."},
                "animatorPath": {
                    "type": "string",
                    "description": "Path to GameObject with Animator component.",
                },
                "autoFindAnimator": {
                    "type": "boolean",
                    "description": "Auto-find Animator on same GameObject.",
                },
                "syncRules": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "parameter": {
                                "type": "string",
                                "description": "Animator parameter name.",
                            },
                            "parameterType": {
                                "type": "string",
                                "enum": ["float", "int", "bool"],
                                "description": "Parameter type.",
                            },
                            "sourceType": {
                                "type": "string",
                                "enum": [
                                    "rigidbody3d",
                                    "rigidbody2d",
                                    "transform",
                                    "health",
                                    "custom",
                                ],
                                "description": "Value source type.",
                            },
                            "sourceProperty": {
                                "type": "string",
                                "description": "Property to read (e.g., 'velocity.magnitude', 'position.y').",
                            },
                            "healthId": {
                                "type": "string",
                                "description": "Health ID when sourceType is 'health'.",
                            },
                            "multiplier": {
                                "type": "number",
                                "description": "Value multiplier (default: 1.0).",
                            },
                            "boolThreshold": {
                                "type": "number",
                                "description": "Threshold for bool parameters.",
                            },
                        },
                    },
                    "description": "Sync rules for animator parameters.",
                },
                "triggers": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "triggerName": {
                                "type": "string",
                                "description": "Animator trigger name.",
                            },
                            "eventSource": {
                                "type": "string",
                                "enum": ["health", "input", "manual"],
                                "description": "Event source type.",
                            },
                            "inputAction": {"type": "string", "description": "Input action name."},
                            "healthId": {"type": "string", "description": "Health component ID."},
                            "healthEvent": {
                                "type": "string",
                                "enum": [
                                    "OnDamaged",
                                    "OnHealed",
                                    "OnDeath",
                                    "OnRespawn",
                                    "OnInvincibilityStart",
                                    "OnInvincibilityEnd",
                                ],
                                "description": "Health event type.",
                            },
                        },
                    },
                    "description": "Trigger rules for animator triggers.",
                },
                "rule": {
                    "type": "object",
                    "properties": {
                        "parameter": {"type": "string"},
                        "parameterType": {"type": "string"},
                        "sourceType": {"type": "string"},
                        "sourceProperty": {"type": "string"},
                        "healthId": {"type": "string"},
                        "multiplier": {"type": "number"},
                        "boolThreshold": {"type": "number"},
                    },
                    "description": "Single sync rule for addSyncRule operation.",
                },
                "trigger": {
                    "type": "object",
                    "properties": {
                        "triggerName": {"type": "string"},
                        "eventSource": {"type": "string"},
                        "inputAction": {"type": "string"},
                        "healthId": {"type": "string"},
                        "healthEvent": {"type": "string"},
                    },
                    "description": "Single trigger rule for addTriggerRule operation.",
                },
                "parameterName": {
                    "type": "string",
                    "description": "Parameter/trigger name for remove/set operations.",
                },
                "triggerName": {"type": "string", "description": "Trigger name to fire."},
                "value": {"type": "number", "description": "Value for setParameter operation."},
            },
        },
        ["operation"],
    )


def gamekit_effect_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_effect MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "inspect",
                        "delete",
                        "addComponent",
                        "removeComponent",
                        "clearComponents",
                        "play",
                        "playAtPosition",
                        "playAtTransform",
                        "shakeCamera",
                        "flashScreen",
                        "setTimeScale",
                        "createManager",
                        "registerEffect",
                        "unregisterEffect",
                        "findByEffectId",
                        "listEffects",
                    ],
                    "description": "Effect operation to perform.",
                },
                "effectId": {"type": "string", "description": "Unique effect identifier."},
                "assetPath": {
                    "type": "string",
                    "description": "Effect asset path (e.g., 'Assets/Effects/HitEffect.asset').",
                },
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject path for playAtTransform.",
                },
                "managerPath": {
                    "type": "string",
                    "description": "Path to EffectManager GameObject.",
                },
                "newEffectId": {
                    "type": "string",
                    "description": "New effect ID for update operation.",
                },
                "components": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "type": {
                                "type": "string",
                                "enum": [
                                    "particle",
                                    "sound",
                                    "cameraShake",
                                    "screenFlash",
                                    "timeScale",
                                ],
                                "description": "Effect component type.",
                            },
                            "prefabPath": {
                                "type": "string",
                                "description": "Particle prefab path.",
                            },
                            "duration": {"type": "number", "description": "Effect duration."},
                            "attachToTarget": {
                                "type": "boolean",
                                "description": "Attach particle to target.",
                            },
                            "positionOffset": {
                                "type": "object",
                                "properties": {
                                    "x": {"type": "number"},
                                    "y": {"type": "number"},
                                    "z": {"type": "number"},
                                },
                            },
                            "particleScale": {
                                "type": "number",
                                "description": "Particle scale multiplier.",
                            },
                            "clipPath": {"type": "string", "description": "Audio clip path."},
                            "volume": {"type": "number", "description": "Audio volume (0-1)."},
                            "pitchVariation": {
                                "type": "number",
                                "description": "Pitch variation range.",
                            },
                            "spatialBlend": {
                                "type": "number",
                                "description": "3D spatial blend (0=2D, 1=3D).",
                            },
                            "intensity": {
                                "type": "number",
                                "description": "Camera shake intensity.",
                            },
                            "shakeDuration": {
                                "type": "number",
                                "description": "Camera shake duration.",
                            },
                            "frequency": {
                                "type": "number",
                                "description": "Camera shake frequency.",
                            },
                            "color": {
                                "type": "object",
                                "properties": {
                                    "r": {"type": "number"},
                                    "g": {"type": "number"},
                                    "b": {"type": "number"},
                                    "a": {"type": "number"},
                                },
                                "description": "Flash color.",
                            },
                            "flashDuration": {
                                "type": "number",
                                "description": "Screen flash duration.",
                            },
                            "fadeTime": {"type": "number", "description": "Flash fade time."},
                            "targetTimeScale": {
                                "type": "number",
                                "description": "Target time scale for slow-mo.",
                            },
                            "timeScaleDuration": {
                                "type": "number",
                                "description": "Time scale effect duration.",
                            },
                            "timeScaleTransition": {
                                "type": "number",
                                "description": "Time scale transition time.",
                            },
                        },
                    },
                    "description": "Effect components for create operation.",
                },
                "component": {
                    "type": "object",
                    "description": "Single effect component for addComponent operation.",
                },
                "componentIndex": {
                    "type": "integer",
                    "description": "Component index for removeComponent.",
                },
                "position": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Position for play operation.",
                },
                "persistent": {
                    "type": "boolean",
                    "description": "Manager persists across scenes (DontDestroyOnLoad).",
                },
            },
        },
        ["operation"],
    )
