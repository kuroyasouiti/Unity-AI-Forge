"""Schema definitions for GameKit core MCP tools."""

from __future__ import annotations

from typing import Any

from tools.schemas.common import schema_with_required


def gamekit_actor_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_actor MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "inspect", "delete"],
                    "description": "Actor operation.",
                },
                "actorId": {
                    "type": "string",
                    "description": "Unique actor identifier (used for targeting with UICommand and scripting).",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path (optional, defaults to scene root).",
                },
                "behaviorProfile": {
                    "type": "string",
                    "enum": [
                        "2dLinear",
                        "2dPhysics",
                        "2dTileGrid",
                        "graphNode",
                        "splineMovement",
                        "3dCharacterController",
                        "3dPhysics",
                        "3dNavMesh",
                    ],
                    "description": "Movement behavior profile: '2dLinear' (simple 2D movement), '2dPhysics' (Rigidbody2D physics), '2dTileGrid' (grid-based movement for tactics/roguelikes), 'graphNode' (A* pathfinding, 2D/3D agnostic), 'splineMovement' (rail-based for 2.5D/rail shooters), '3dCharacterController' (CharacterController for FPS/TPS), '3dPhysics' (Rigidbody physics), '3dNavMesh' (NavMesh agent for RTS/strategy).",
                },
                "controlMode": {
                    "type": "string",
                    "enum": ["directController", "aiAutonomous", "uiCommand", "scriptTriggerOnly"],
                    "description": "Input control mode: 'directController' (player input via New Input System or legacy), 'aiAutonomous' (AI-driven patrol/follow/wander), 'uiCommand' (controlled by UI buttons via GameKitUICommand), 'scriptTriggerOnly' (event-driven from scripts only).",
                },
                "position": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Initial world position of the actor.",
                },
                "rotation": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Initial euler rotation of the actor (optional).",
                },
                "spritePath": {
                    "type": "string",
                    "description": "Sprite asset path for 2D actors (e.g., 'Assets/Sprites/Player.png').",
                },
                "modelPath": {
                    "type": "string",
                    "description": "Model prefab path for 3D actors (e.g., 'Assets/Models/Character.prefab').",
                },
            },
        },
        ["operation"],
    )


def gamekit_manager_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_manager MCP tool."""
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
                        "exportState",
                        "importState",
                        "setFlowEnabled",
                    ],
                    "description": "Manager operation.",
                },
                "managerId": {"type": "string", "description": "Unique manager identifier."},
                "managerType": {
                    "type": "string",
                    "enum": ["turnBased", "realtime", "resourcePool", "eventHub", "stateManager"],
                    "description": "Manager type: 'turnBased' for turn-based games, 'realtime' for real-time coordination, 'resourcePool' for resource/economy management, 'eventHub' for global events, 'stateManager' for finite state machines.",
                },
                "parentPath": {"type": "string", "description": "Parent GameObject path."},
                "persistent": {
                    "type": "boolean",
                    "description": "DontDestroyOnLoad flag (survives scene changes).",
                },
                "turnPhases": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Turn phase names for turn-based managers (e.g., ['PlayerTurn', 'EnemyTurn', 'ResolveEffects']).",
                },
                "resourceTypes": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Resource type names for resource pool managers (deprecated, use initialResources instead).",
                },
                "initialResources": {
                    "type": "object",
                    "additionalProperties": {"type": "number"},
                    "description": "Initial resource amounts for resource pool managers (e.g., {'health': 100, 'mana': 50, 'gold': 1000}).",
                },
                "stateData": {
                    "type": "object",
                    "additionalProperties": True,
                    "description": "State data for importState operation (JSON-serializable state from exportState).",
                },
                "flowId": {
                    "type": "string",
                    "description": "Flow identifier for setFlowEnabled operation.",
                },
                "enabled": {
                    "type": "boolean",
                    "description": "Enable/disable flow for setFlowEnabled operation.",
                },
            },
        },
        ["operation"],
    )


def gamekit_interaction_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_interaction MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "inspect", "delete"],
                    "description": "Interaction operation.",
                },
                "interactionId": {
                    "type": "string",
                    "description": "Unique interaction identifier (e.g., 'GoldCoin', 'AutoDoor').",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path (optional, creates new GameObject if not specified).",
                },
                "triggerType": {
                    "type": "string",
                    "enum": ["collision", "trigger", "raycast", "proximity", "input"],
                    "description": "Trigger detection type: 'collision' (OnCollisionEnter), 'trigger' (OnTriggerEnter), 'raycast' (ray hit detection), 'proximity' (distance-based), 'input' (key press).",
                },
                "triggerShape": {
                    "type": "string",
                    "enum": ["box", "sphere", "circle", "capsule", "polygon", "mesh"],
                    "description": "Collider shape. 2D: box/circle/capsule/polygon. 3D: box/sphere/capsule/mesh.",
                },
                "triggerSize": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Collider size/radius (Vector3 for box/capsule, x for sphere/circle radius).",
                },
                "is2D": {
                    "type": "boolean",
                    "description": "Use 2D colliders (BoxCollider2D, CircleCollider2D, etc.) instead of 3D. Default: false.",
                },
                "actions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "type": {
                                "type": "string",
                                "enum": [
                                    "spawnPrefab",
                                    "destroyObject",
                                    "playSound",
                                    "sendMessage",
                                    "changeScene",
                                ],
                                "description": "Action type to execute.",
                            },
                            "target": {
                                "type": "string",
                                "description": "Target GameObject name/path or 'self' for the interaction GameObject.",
                            },
                            "parameter": {
                                "type": "string",
                                "description": "Action parameter (prefab path, message name, scene name, etc.).",
                            },
                        },
                    },
                    "description": "Declarative actions to execute when trigger conditions are met (executed in order).",
                },
                "conditions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "type": {
                                "type": "string",
                                "enum": ["tag", "layer", "distance", "custom"],
                                "description": "Condition type.",
                            },
                            "value": {
                                "type": "string",
                                "description": "Condition value (tag name, layer name/number, distance threshold, custom script).",
                            },
                        },
                    },
                    "description": "Conditions to check before executing actions (all conditions must pass, AND logic).",
                },
            },
        },
        ["operation"],
    )


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
                "canvasPath": {"type": "string", "description": "Canvas GameObject path."},
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
                    "description": "List of commands to create as buttons.",
                },
                "layout": {
                    "type": "string",
                    "enum": ["horizontal", "vertical", "grid"],
                    "description": "Button layout style.",
                },
                "buttonSize": {
                    "type": "object",
                    "properties": {"width": {"type": "number"}, "height": {"type": "number"}},
                },
            },
        },
        ["operation"],
    )


def gamekit_machinations_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_machinations MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["create", "update", "inspect", "delete", "apply", "export"],
                    "description": "Machinations asset operation.",
                },
                "diagramId": {"type": "string", "description": "Unique diagram identifier."},
                "assetPath": {"type": "string", "description": "Path to Machinations asset file."},
                "managerId": {
                    "type": "string",
                    "description": "Manager ID to apply/export diagram to/from.",
                },
                "resetExisting": {
                    "type": "boolean",
                    "description": "Reset existing resources when applying.",
                },
                "initialResources": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": {"type": "string"},
                            "initialAmount": {"type": "number"},
                            "minValue": {"type": "number"},
                            "maxValue": {"type": "number"},
                        },
                    },
                    "description": "Resource pool definitions.",
                },
                "flows": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "flowId": {"type": "string"},
                            "resourceName": {"type": "string"},
                            "ratePerSecond": {"type": "number"},
                            "isSource": {"type": "boolean"},
                            "enabledByDefault": {"type": "boolean"},
                        },
                    },
                    "description": "Resource flow definitions (automatic generation/consumption).",
                },
                "converters": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "converterId": {"type": "string"},
                            "fromResource": {"type": "string"},
                            "toResource": {"type": "string"},
                            "conversionRate": {"type": "number"},
                            "inputCost": {"type": "number"},
                            "enabledByDefault": {"type": "boolean"},
                        },
                    },
                    "description": "Resource converter definitions (transform resources).",
                },
                "triggers": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "triggerName": {"type": "string"},
                            "resourceName": {"type": "string"},
                            "thresholdType": {
                                "type": "string",
                                "enum": ["above", "below", "equal", "notEqual"],
                            },
                            "thresholdValue": {"type": "number"},
                            "enabledByDefault": {"type": "boolean"},
                        },
                    },
                    "description": "Resource trigger definitions (threshold events).",
                },
            },
        },
        ["operation"],
    )


def gamekit_sceneflow_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_sceneflow MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "inspect",
                        "delete",
                        "transition",
                        "addScene",
                        "removeScene",
                        "updateScene",
                        "addTransition",
                        "removeTransition",
                        "addSharedScene",
                        "removeSharedScene",
                    ],
                    "description": "SceneFlow operation: 'create' for initial setup, then use individual add/remove/update operations for granular control.",
                },
                "flowId": {
                    "type": "string",
                    "description": "Unique scene flow identifier (e.g., 'MainGameFlow').",
                },
                "sceneName": {
                    "type": "string",
                    "description": "Scene name for single-scene operations (addScene, removeScene, updateScene, addSharedScene, removeSharedScene).",
                },
                "scenePath": {
                    "type": "string",
                    "description": "Unity scene asset path (e.g., 'Assets/Scenes/Level1.unity') for addScene/updateScene.",
                },
                "loadMode": {
                    "type": "string",
                    "enum": ["single", "additive"],
                    "description": "'single' unloads all scenes, 'additive' loads on top of existing (for addScene/updateScene).",
                },
                "sharedScenePaths": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Array of shared scene paths to load with this scene (for addScene/updateScene), e.g., ['Assets/Scenes/UIOverlay.unity', 'Assets/Scenes/AudioManager.unity'].",
                },
                "sharedScenePath": {
                    "type": "string",
                    "description": "Single shared scene path for addSharedScene/removeSharedScene operations.",
                },
                "fromScene": {
                    "type": "string",
                    "description": "Source scene name for transition operations (addTransition/removeTransition).",
                },
                "toScene": {
                    "type": "string",
                    "description": "Destination scene name for addTransition operation.",
                },
                "trigger": {
                    "type": "string",
                    "description": "Trigger name for transition operations (addTransition/removeTransition, e.g., 'startGame', 'levelComplete').",
                },
                "triggerName": {
                    "type": "string",
                    "description": "Transition trigger name for 'transition' operation (runtime execution).",
                },
            },
        },
        ["operation"],
    )
