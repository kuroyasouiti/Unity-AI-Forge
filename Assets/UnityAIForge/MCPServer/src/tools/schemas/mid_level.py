"""Schema definitions for mid-level non-UI MCP tools.

Includes: transform_batch, camera_bundle, input_profile, tilemap_bundle,
physics_bundle, navmesh_bundle.

UI-related mid-level schemas are in mid_level_ui.py.
"""

from __future__ import annotations

from typing import Any

from tools.schemas.common import schema_with_required


def transform_batch_schema() -> dict[str, Any]:
    """Schema for the unity_transform_batch MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "arrangeCircle",
                        "arrangeLine",
                        "renameSequential",
                        "renameFromList",
                        "createMenuList",
                    ],
                    "description": "Transform batch operation to perform.",
                },
                "gameObjectPaths": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Target GameObject hierarchy paths.",
                },
                "center": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Center point for circular arrangement.",
                },
                "radius": {
                    "type": "number",
                    "minimum": 0,
                    "description": "Radius for circular arrangement.",
                },
                "startAngle": {
                    "type": "number",
                    "description": "Starting angle in degrees for circular arrangement.",
                },
                "clockwise": {
                    "type": "boolean",
                    "description": "Arrange objects clockwise (default: false = counter-clockwise).",
                },
                "plane": {
                    "type": "string",
                    "enum": ["XY", "XZ", "YZ"],
                    "description": "Plane for circular arrangement (default: XY for 2D, XZ for 3D top-down).",
                },
                "localSpace": {
                    "type": "boolean",
                    "description": "Use local space coordinates instead of world space.",
                },
                "startPosition": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Start position for linear arrangement.",
                },
                "endPosition": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "End position for linear arrangement.",
                },
                "spacing": {
                    "type": "number",
                    "description": "Spacing between objects for linear arrangement or menu list.",
                },
                "baseName": {
                    "type": "string",
                    "description": "Base name for sequential renaming (e.g., 'Enemy' -> Enemy_001, Enemy_002).",
                },
                "startIndex": {
                    "type": "integer",
                    "description": "Starting index for sequential renaming (default: 1).",
                },
                "padding": {
                    "type": "integer",
                    "description": "Zero-padding for sequential numbers (e.g., 3 -> 001, 002).",
                },
                "names": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Custom name list for renameFromList operation.",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path for createMenuList operation.",
                },
                "prefabPath": {
                    "type": "string",
                    "description": "Prefab asset path for createMenuList operation.",
                },
                "axis": {
                    "type": "string",
                    "enum": ["horizontal", "vertical"],
                    "description": "Layout axis for createMenuList operation.",
                },
                "offset": {
                    "type": "number",
                    "description": "Offset from parent for createMenuList operation.",
                },
            },
        },
        ["operation"],
    )


def camera_bundle_schema() -> dict[str, Any]:
    """Schema for the unity_camera_bundle MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "applyPreset",
                        "listPresets",
                    ],
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "Target GameObject path (required for applyPreset).",
                },
                "name": {
                    "type": "string",
                    "description": "Name for the new Camera GameObject (create only). Falls back to gameObjectPath if omitted.",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path (create only).",
                },
                "preset": {
                    "type": "string",
                    "enum": [
                        "default",
                        "orthographic2D",
                        "firstPerson",
                        "thirdPerson",
                        "topDown",
                        "splitScreenLeft",
                        "splitScreenRight",
                        "splitScreenTop",
                        "splitScreenBottom",
                        "minimap",
                        "uiCamera",
                    ],
                    "description": "Camera preset to apply.",
                },
                "position": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "World position.",
                },
                "rotation": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Euler rotation.",
                },
                "fieldOfView": {
                    "type": "number",
                    "description": "Camera field of view (perspective mode).",
                },
                "orthographic": {"type": "boolean", "description": "Use orthographic projection."},
                "orthographicSize": {"type": "number", "description": "Orthographic camera size."},
                "clearFlags": {
                    "type": "string",
                    "enum": ["skybox", "solidColor", "depth", "nothing"],
                    "description": "Camera clear flags.",
                },
                "backgroundColor": {
                    "description": "Background color (hex string or {r,g,b,a} object).",
                },
                "cullingMask": {"type": "integer", "description": "Culling mask (layer bitmask)."},
                "depth": {"type": "number", "description": "Camera depth (render order)."},
                "nearClipPlane": {"type": "number", "description": "Near clip plane distance."},
                "farClipPlane": {"type": "number", "description": "Far clip plane distance."},
                "rect": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "width": {"type": "number"},
                        "height": {"type": "number"},
                    },
                    "description": "Viewport rect (0-1 normalized).",
                },
                "targetDisplay": {"type": "integer", "description": "Target display index."},
                "renderingPath": {
                    "type": "string",
                    "enum": ["usePlayerSettings", "forward", "deferred", "vertexLit"],
                    "description": "Rendering path.",
                },
                "allowHDR": {"type": "boolean", "description": "Allow HDR rendering."},
                "allowMSAA": {"type": "boolean", "description": "Allow MSAA."},
            },
        },
        ["operation"],
    )


def input_profile_schema() -> dict[str, Any]:
    """Schema for the unity_input_profile MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["createPlayerInput", "createInputActions", "inspect"],
                },
                "gameObjectPath": {"type": "string", "description": "Target GameObject path."},
                "preset": {
                    "type": "string",
                    "enum": ["player", "ui", "vehicle", "custom", "shooter2d", "platformer2d"],
                    "description": (
                        "For createPlayerInput: notification behavior preset (player/ui/vehicle/custom). "
                        "For createInputActions: genre preset that auto-generates full action maps with bindings "
                        "(shooter2d: Move/Shoot/Bomb/SlowMove/Pause, platformer2d: Move/Jump/Attack/Dash/Pause)."
                    ),
                },
                "inputActionsAssetPath": {
                    "type": "string",
                    "description": "InputActions asset path (must end with .inputactions).",
                },
                "defaultActionMap": {"type": "string", "description": "Default action map name."},
                "notificationBehavior": {
                    "type": "string",
                    "enum": [
                        "sendMessages",
                        "broadcastMessages",
                        "invokeUnityEvents",
                        "invokeCSharpEvents",
                    ],
                    "description": "Input notification behavior.",
                },
                "actionMaps": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": {
                                "type": "string",
                                "description": "Action map name (e.g., 'Player').",
                            },
                            "actions": {
                                "type": "array",
                                "items": {
                                    "type": "object",
                                    "properties": {
                                        "name": {"type": "string"},
                                        "type": {
                                            "type": "string",
                                            "enum": ["Button", "Value", "PassThrough"],
                                        },
                                        "valueType": {
                                            "type": "string",
                                            "description": "Expected control type for Value actions (e.g., 'Vector2').",
                                        },
                                        "bindings": {
                                            "type": "array",
                                            "items": {
                                                "type": "object",
                                                "properties": {
                                                    "path": {
                                                        "type": "string",
                                                        "description": "Input binding path (e.g., '<Keyboard>/z').",
                                                    },
                                                },
                                            },
                                            "description": "Simple bindings for this action.",
                                        },
                                        "compositeBindings": {
                                            "type": "array",
                                            "items": {
                                                "type": "object",
                                                "properties": {
                                                    "name": {
                                                        "type": "string",
                                                        "description": "Composite name (e.g., 'WASD').",
                                                    },
                                                    "compositeType": {
                                                        "type": "string",
                                                        "description": "Composite type (e.g., '2DVector', '1DAxis').",
                                                    },
                                                    "parts": {
                                                        "type": "object",
                                                        "additionalProperties": {"type": "string"},
                                                        "description": "Composite parts (e.g., {up: '<Keyboard>/w', down: '<Keyboard>/s', left: '<Keyboard>/a', right: '<Keyboard>/d'}).",
                                                    },
                                                },
                                            },
                                            "description": "Composite bindings (e.g., 2DVector for WASD movement).",
                                        },
                                    },
                                },
                            },
                        },
                    },
                    "description": (
                        "Action maps with full action and binding definitions for createInputActions. "
                        "Writes a complete .inputactions JSON file. Supports simple bindings and composite bindings (2DVector, 1DAxis)."
                    ),
                },
                "actions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": {"type": "string"},
                            "type": {"type": "string", "enum": ["button", "value", "passThrough"]},
                            "binding": {"type": "string"},
                        },
                    },
                    "description": "Legacy: simple action definitions. Prefer actionMaps for full control.",
                },
            },
        },
        ["operation"],
    )


def tilemap_bundle_schema() -> dict[str, Any]:
    """Schema for the unity_tilemap_bundle MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "createTilemap",
                        "inspect",
                        "setTile",
                        "getTile",
                        "setTiles",
                        "clearTile",
                        "clearTiles",
                        "clearAllTiles",
                        "fillArea",
                        "boxFill",
                        "worldToCell",
                        "cellToWorld",
                        "createTile",
                        "createRuleTile",
                        "inspectTile",
                        "updateTile",
                    ],
                    "description": "Tilemap operation.",
                },
                "name": {
                    "type": "string",
                    "description": "Tilemap GameObject name (for createTilemap).",
                },
                "tilemapPath": {
                    "type": "string",
                    "description": "Tilemap GameObject hierarchy path.",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path (for createTilemap).",
                },
                "cellLayout": {
                    "type": "string",
                    "enum": ["Rectangle", "Hexagon", "Isometric", "IsometricZAsY"],
                    "description": "Grid cell layout type.",
                },
                "cellSize": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Grid cell size.",
                },
                "position": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "integer"},
                        "y": {"type": "integer"},
                        "z": {"type": "integer"},
                    },
                    "description": "Tile position (grid coordinates, integers).",
                },
                "positions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "x": {"type": "integer"},
                            "y": {"type": "integer"},
                            "z": {"type": "integer"},
                        },
                    },
                    "description": "Multiple tile positions for setTiles.",
                },
                "worldPosition": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "World position for worldToCell conversion.",
                },
                "cellPosition": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "integer"},
                        "y": {"type": "integer"},
                        "z": {"type": "integer"},
                    },
                    "description": "Cell position for cellToWorld conversion.",
                },
                "bounds": {
                    "type": "object",
                    "properties": {
                        "xMin": {"type": "integer"},
                        "yMin": {"type": "integer"},
                        "zMin": {"type": "integer"},
                        "xMax": {"type": "integer"},
                        "yMax": {"type": "integer"},
                        "zMax": {"type": "integer"},
                    },
                    "description": "Bounding box for area operations (fillArea, clearTiles, boxFill).",
                },
                "tileAssetPath": {
                    "type": "string",
                    "description": "Tile asset path (.asset file).",
                },
                "spritePath": {
                    "type": "string",
                    "description": "Sprite asset path for createTile.",
                },
                "defaultSprite": {
                    "type": "string",
                    "description": "Default sprite path for createRuleTile.",
                },
                "colliderType": {
                    "type": "string",
                    "enum": ["None", "Sprite", "Grid"],
                    "description": "Tile collider type for createTile.",
                },
                "color": {
                    "type": "object",
                    "properties": {
                        "r": {"type": "number"},
                        "g": {"type": "number"},
                        "b": {"type": "number"},
                        "a": {"type": "number"},
                    },
                    "description": "Tile color (RGBA 0-1).",
                },
                "rules": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "sprites": {
                                "type": "array",
                                "items": {"type": "string"},
                                "description": "Sprite paths for this rule.",
                            },
                            "neighbors": {"type": "object", "description": "Neighbor constraints."},
                        },
                    },
                    "description": "RuleTile tiling rules (requires 2D Tilemap Extras package).",
                },
                "sortingLayerName": {
                    "type": "string",
                    "description": "Sorting layer name for TilemapRenderer.",
                },
                "sortingOrder": {
                    "type": "integer",
                    "description": "Sorting order for TilemapRenderer.",
                },
                "mode": {
                    "type": "string",
                    "enum": ["Chunk", "Individual"],
                    "description": "TilemapRenderer mode.",
                },
                "usedByComposite": {
                    "type": "boolean",
                    "description": "Whether TilemapCollider2D is used by CompositeCollider2D.",
                },
                "usedByEffector": {
                    "type": "boolean",
                    "description": "Whether TilemapCollider2D is used by effector.",
                },
                "isTrigger": {
                    "type": "boolean",
                    "description": "Whether TilemapCollider2D is a trigger.",
                },
                "includeAllTiles": {
                    "type": "boolean",
                    "description": "Include all tile data in inspect response.",
                },
            },
        },
        ["operation"],
    )


def physics_bundle_schema() -> dict[str, Any]:
    """Schema for the unity_physics_bundle MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "applyPreset",
                        "setCollisionMatrix",
                        "setCollisionMatrixBatch",
                        "createPhysicsMaterial",
                        "createPhysicsMaterial2D",
                        "inspect",
                    ],
                    "description": "Physics bundle operation to perform.",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "Target GameObject hierarchy path (for applyPreset/inspect).",
                },
                "preset": {
                    "type": "string",
                    "enum": [
                        "platformer2D",
                        "topDown2D",
                        "fps3D",
                        "thirdPerson3D",
                        "space",
                        "racing",
                    ],
                    "description": "Physics preset to apply.",
                },
                "layerA": {
                    "type": "string",
                    "description": "First layer name for collision matrix (setCollisionMatrix).",
                },
                "layerB": {
                    "type": "string",
                    "description": "Second layer name for collision matrix (setCollisionMatrix).",
                },
                "ignore": {
                    "type": "boolean",
                    "description": "Whether to ignore collisions between layers (default: true).",
                },
                "is2D": {
                    "type": "boolean",
                    "description": "Use Physics2D collision matrix instead of Physics (default: false).",
                },
                "materialPath": {
                    "type": "string",
                    "description": "Asset path for physics material (createPhysicsMaterial/createPhysicsMaterial2D).",
                },
                "dynamicFriction": {
                    "type": "number",
                    "minimum": 0,
                    "description": "Dynamic friction (3D material, default: 0.6).",
                },
                "staticFriction": {
                    "type": "number",
                    "minimum": 0,
                    "description": "Static friction (3D material, default: 0.6).",
                },
                "bounciness": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 1,
                    "description": "Bounciness (0-1, default: 0).",
                },
                "friction": {
                    "type": "number",
                    "minimum": 0,
                    "description": "Friction (2D material, default: 0.4).",
                },
                "frictionCombine": {
                    "type": "string",
                    "enum": ["Average", "Minimum", "Maximum", "Multiply"],
                    "description": "Friction combine mode (3D material).",
                },
                "bounceCombine": {
                    "type": "string",
                    "enum": ["Average", "Minimum", "Maximum", "Multiply"],
                    "description": "Bounce combine mode (3D material).",
                },
                "assignTo": {
                    "type": "string",
                    "description": "GameObject path to assign created material to its collider.",
                },
                "pairs": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "layerA": {
                                "type": "string",
                                "description": "First layer name.",
                            },
                            "layerB": {
                                "type": "string",
                                "description": "Second layer name.",
                            },
                            "ignore": {
                                "type": "boolean",
                                "description": "Whether to ignore collisions (default: true).",
                            },
                        },
                        "required": ["layerA", "layerB"],
                    },
                    "description": "Array of layer pairs for setCollisionMatrixBatch. Set multiple collision pairs in one call.",
                },
                "reportChanges": {
                    "type": "boolean",
                    "description": "Return a before/after diff report for applyPreset, showing which properties (tag, layer, components) were changed. Warns if tag/layer were overwritten (default: false).",
                },
            },
        },
        ["operation"],
    )


def navmesh_bundle_schema() -> dict[str, Any]:
    """Schema for the unity_navmesh_bundle MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "bake",
                        "addAgent",
                        "addObstacle",
                        "addLink",
                        "addModifier",
                        "inspect",
                        "clearNavMesh",
                    ],
                    "description": "NavMesh bundle operation to perform.",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "Target GameObject hierarchy path.",
                },
                "agentTypeId": {
                    "type": "integer",
                    "description": "NavMesh agent type ID (default: 0 = Humanoid).",
                },
                "agentRadius": {
                    "type": "number",
                    "minimum": 0,
                    "description": "Agent bake radius (default: 0.5).",
                },
                "agentHeight": {
                    "type": "number",
                    "minimum": 0,
                    "description": "Agent bake height (default: 2.0).",
                },
                "agentSlope": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 60,
                    "description": "Max walkable slope in degrees (default: 45).",
                },
                "agentClimb": {
                    "type": "number",
                    "minimum": 0,
                    "description": "Step height the agent can climb (default: 0.4).",
                },
                "speed": {
                    "type": "number",
                    "minimum": 0,
                    "description": "NavMeshAgent speed (default: 3.5).",
                },
                "angularSpeed": {
                    "type": "number",
                    "minimum": 0,
                    "description": "NavMeshAgent angular speed (default: 120).",
                },
                "acceleration": {
                    "type": "number",
                    "minimum": 0,
                    "description": "NavMeshAgent acceleration (default: 8).",
                },
                "stoppingDistance": {
                    "type": "number",
                    "minimum": 0,
                    "description": "NavMeshAgent stopping distance (default: 0).",
                },
                "radius": {
                    "type": "number",
                    "minimum": 0,
                    "description": "NavMeshAgent/NavMeshObstacle radius.",
                },
                "height": {
                    "type": "number",
                    "minimum": 0,
                    "description": "NavMeshObstacle height.",
                },
                "shape": {
                    "type": "string",
                    "enum": ["Capsule", "Box"],
                    "description": "NavMeshObstacle shape (default: Capsule).",
                },
                "carve": {
                    "type": "boolean",
                    "description": "Whether the obstacle carves the NavMesh (default: true).",
                },
                "startPoint": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Start point for NavMeshLink.",
                },
                "endPoint": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "End point for NavMeshLink.",
                },
                "linkWidth": {
                    "type": "number",
                    "minimum": 0,
                    "description": "NavMeshLink width (default: 1.0).",
                },
                "bidirectional": {
                    "type": "boolean",
                    "description": "Whether NavMeshLink is bidirectional (default: true).",
                },
                "area": {
                    "type": "integer",
                    "description": "NavMesh area index (0=Walkable, 1=NotWalkable, 2=Jump).",
                },
                "overrideArea": {
                    "type": "boolean",
                    "description": "Whether NavMeshModifier overrides area (default: false).",
                },
                "affectedAgents": {
                    "type": "boolean",
                    "description": "Whether NavMeshModifier affects specific agent types.",
                },
                "collectObjects": {
                    "type": "string",
                    "enum": ["All", "Volume", "Children"],
                    "description": "NavMeshSurface object collection mode (default: All).",
                },
            },
        },
        ["operation"],
    )
