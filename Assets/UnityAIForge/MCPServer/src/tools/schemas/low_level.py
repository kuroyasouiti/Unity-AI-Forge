"""Schema definitions for low-level MCP tools."""

from __future__ import annotations

from typing import Any

from tools.schemas.common import schema_with_required


def scene_manage_schema() -> dict[str, Any]:
    """Schema for the unity_scene_crud MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "load",
                        "save",
                        "delete",
                        "duplicate",
                        "inspect",
                    ],
                    "description": "Scene operation to perform.",
                },
                "scenePath": {
                    "type": "string",
                    "description": "Path to scene file (e.g., 'Assets/Scenes/Level1.unity').",
                },
                "newSceneName": {
                    "type": "string",
                    "description": "New name for duplicate operation.",
                },
                "additive": {
                    "type": "boolean",
                    "description": "Load scene additively (keep existing scenes loaded).",
                },
                "includeOpenScenes": {
                    "type": "boolean",
                    "description": "Include currently open scenes in inspect response.",
                },
                "includeHierarchy": {
                    "type": "boolean",
                    "description": "Include scene hierarchy (GameObjects) in inspect response.",
                },
                "includeComponents": {
                    "type": "boolean",
                    "description": "Include component details for each GameObject in hierarchy.",
                },
                "filter": {
                    "type": "string",
                    "description": "Filter GameObjects by name pattern (e.g., 'Player*', '*Enemy*').",
                },
            },
        },
        ["operation"],
    )


def game_object_manage_schema() -> dict[str, Any]:
    """Schema for the unity_gameobject_crud MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "delete",
                        "move",
                        "rename",
                        "update",
                        "duplicate",
                        "inspect",
                        "findMultiple",
                        "deleteMultiple",
                        "inspectMultiple",
                    ],
                    "description": "GameObject operation to perform.",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "Hierarchy path to target GameObject (e.g., 'Canvas/Panel/Button').",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path for create/move operations.",
                },
                "template": {
                    "type": "string",
                    "description": "Primitive template for create (e.g., 'Cube', 'Sphere', 'Capsule', 'Cylinder', 'Plane', 'Quad').",
                },
                "name": {
                    "type": "string",
                    "description": "Name for new GameObject or rename operation.",
                },
                "tag": {
                    "type": "string",
                    "description": "Tag to assign (e.g., 'Player', 'Enemy'). Must exist in project settings.",
                },
                "layer": {
                    "oneOf": [{"type": "integer"}, {"type": "string"}],
                    "description": "Layer by number (0-31) or name (e.g., 'UI', 'Player'). Use unity_projectSettings_crud to add custom layers.",
                },
                "active": {
                    "type": "boolean",
                    "description": "Set GameObject active/inactive state.",
                },
                "static": {
                    "type": "boolean",
                    "description": "Mark GameObject as static for optimization.",
                },
                "pattern": {
                    "type": "string",
                    "description": "Name pattern for batch operations (e.g., 'Enemy*', '*_LOD0').",
                },
                "useRegex": {
                    "type": "boolean",
                    "description": "Interpret pattern as regex instead of wildcard. Legacy: prefer matchMode.",
                },
                "matchMode": {
                    "type": "string",
                    "enum": ["exact", "contains", "wildcard", "regex"],
                    "description": "Pattern matching mode for batch operations (default: 'contains'). "
                    "'exact': name must equal pattern exactly. "
                    "'contains': name contains pattern (default, same as wildcard). "
                    "'wildcard': supports * and ? wildcards. "
                    "'regex': full regex matching.",
                },
                "includeComponents": {
                    "type": "boolean",
                    "description": "Include component details in inspect response.",
                },
                "maxResults": {
                    "type": "integer",
                    "description": "Maximum number of results for batch operations (default: 1000).",
                },
                "components": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "type": {
                                "type": "string",
                                "description": "Component type name (e.g., 'UnityEngine.Rigidbody2D', 'UnityEngine.BoxCollider2D').",
                            },
                            "properties": {
                                "type": "object",
                                "additionalProperties": True,
                                "description": "Property values to set on the component.",
                            },
                        },
                        "required": ["type"],
                    },
                    "description": "Components to automatically attach when creating a GameObject. Each item specifies a component type and optional property values.",
                },
            },
        },
        ["operation"],
    )


def component_manage_schema() -> dict[str, Any]:
    """Schema for the unity_component_crud MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "add",
                        "remove",
                        "update",
                        "inspect",
                        "addMultiple",
                        "removeMultiple",
                        "updateMultiple",
                        "inspectMultiple",
                        "crossSceneUpdate",
                    ],
                    "description": "Component operation to perform.",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "Target GameObject hierarchy path.",
                },
                "gameObjectGlobalObjectId": {
                    "type": "string",
                    "description": "Target GameObject GlobalObjectId (alternative to path).",
                },
                "componentType": {
                    "type": "string",
                    "description": "Full component type name (e.g., 'UnityEngine.Rigidbody2D'). Use '*' to target all components (inspect: returns all, remove: removes all except Transform).",
                },
                "propertyChanges": {
                    "type": "object",
                    "additionalProperties": True,
                    "description": "Property values to set. Use {'$ref': 'path'} for references: 'Assets/...' for assets, 'Canvas/Panel/Button' for scene objects (including inactive).",
                },
                "applyDefaults": {
                    "type": "boolean",
                    "description": "Apply default property values when adding component.",
                },
                "pattern": {
                    "type": "string",
                    "description": "GameObject name pattern for batch operations.",
                },
                "useRegex": {
                    "type": "boolean",
                    "description": "Interpret pattern as regex instead of wildcard.",
                },
                "includeProperties": {
                    "type": "boolean",
                    "description": "Include property values in inspect response (default: true).",
                },
                "propertyFilter": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Filter specific properties to include in response.",
                },
                "maxResults": {
                    "type": "integer",
                    "description": "Maximum results for batch operations (default: 1000).",
                },
                "stopOnError": {
                    "type": "boolean",
                    "description": "Stop batch operation on first error (default: true).",
                },
                "updates": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "scenePath": {
                                "type": "string",
                                "description": "Scene file path (e.g., 'Assets/Scenes/Level1.unity').",
                            },
                            "gameObjectPath": {
                                "type": "string",
                                "description": "Target GameObject hierarchy path within the scene.",
                            },
                            "componentType": {
                                "type": "string",
                                "description": "Component type name.",
                            },
                            "propertyChanges": {
                                "type": "object",
                                "additionalProperties": True,
                                "description": "Property values to set.",
                            },
                        },
                        "required": [
                            "scenePath",
                            "gameObjectPath",
                            "componentType",
                            "propertyChanges",
                        ],
                    },
                    "description": "Array of cross-scene component updates for crossSceneUpdate operation. Auto-loads/saves each scene.",
                },
            },
        },
        ["operation"],
    )


def asset_manage_schema() -> dict[str, Any]:
    """Schema for the unity_asset_crud MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "updateImporter",
                        "delete",
                        "rename",
                        "duplicate",
                        "inspect",
                        "findMultiple",
                        "deleteMultiple",
                        "inspectMultiple",
                    ],
                    "description": (
                        "Asset operation to perform. "
                        "Note: For .cs files, create/update/delete operations automatically wait for "
                        "Unity compilation to complete — no need to call compilation_await separately."
                    ),
                },
                "assetPath": {
                    "type": "string",
                    "description": "Asset file path (e.g., 'Assets/Scripts/Player.cs').",
                },
                "assetGuid": {
                    "type": "string",
                    "description": "Asset GUID (alternative to assetPath).",
                },
                "content": {
                    "type": "string",
                    "description": "File content for create/update operations.",
                },
                "destinationPath": {
                    "type": "string",
                    "description": "Destination path for rename/duplicate operations.",
                },
                "propertyChanges": {
                    "type": "object",
                    "additionalProperties": True,
                    "description": "Importer property changes for updateImporter operation.",
                },
                "pattern": {
                    "type": "string",
                    "description": "Asset path pattern for batch operations (e.g., 'Assets/Textures/*.png').",
                },
                "useRegex": {
                    "type": "boolean",
                    "description": "Interpret pattern as regex instead of glob.",
                },
                "includeProperties": {
                    "type": "boolean",
                    "description": "Include asset properties in inspect response.",
                },
                "maxResults": {
                    "type": "integer",
                    "description": "Maximum results for batch operations (default: 1000).",
                },
            },
        },
        ["operation"],
    )


def project_settings_manage_schema() -> dict[str, Any]:
    """Schema for the unity_projectSettings_crud MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "read",
                        "write",
                        "list",
                        "addSceneToBuild",
                        "removeSceneFromBuild",
                        "listBuildScenes",
                        "reorderBuildScenes",
                        "setBuildSceneEnabled",
                    ],
                },
                "category": {
                    "type": "string",
                    "enum": [
                        "player",
                        "quality",
                        "time",
                        "physics",
                        "physics2d",
                        "audio",
                        "editor",
                        "tagsLayers",
                    ],
                    "description": "Settings category to read/write.",
                },
                "property": {
                    "type": "string",
                    "description": "Property name to read/write. Use 'list' operation to see available properties.",
                },
                "value": {
                    "description": "Value to set. Type depends on property (string, number, boolean, object for Vector types like gravity)."
                },
                "scenePath": {
                    "type": "string",
                    "description": "Path to scene file for build settings operations",
                },
                "index": {
                    "type": "integer",
                    "description": "Scene index for build settings operations",
                },
                "fromIndex": {
                    "type": "integer",
                    "description": "Source index for reordering build scenes",
                },
                "toIndex": {
                    "type": "integer",
                    "description": "Target index for reordering build scenes",
                },
                "enabled": {
                    "type": "boolean",
                    "description": "Whether scene is enabled in build settings",
                },
            },
        },
        ["operation"],
    )


def scriptable_object_manage_schema() -> dict[str, Any]:
    """Schema for the unity_scriptableObject_crud MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "inspect",
                        "update",
                        "delete",
                        "duplicate",
                        "list",
                        "findByType",
                    ],
                    "description": "ScriptableObject operation to perform.",
                },
                "typeName": {
                    "type": "string",
                    "description": "Full type name for create/findByType (e.g., 'MyGame.GameConfig').",
                },
                "assetPath": {
                    "type": "string",
                    "description": "ScriptableObject asset path (e.g., 'Assets/Data/Config.asset').",
                },
                "assetGuid": {
                    "type": "string",
                    "description": "Asset GUID (alternative to assetPath).",
                },
                "properties": {
                    "type": "object",
                    "additionalProperties": True,
                    "description": "Property values to set on the ScriptableObject.",
                },
                "includeProperties": {
                    "type": "boolean",
                    "description": "Include property values in inspect response (default: true).",
                },
                "propertyFilter": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Filter specific properties to include in response.",
                },
                "searchPath": {
                    "type": "string",
                    "description": "Directory to search for findByType/list (default: 'Assets').",
                },
                "maxResults": {
                    "type": "integer",
                    "description": "Maximum results for list/findByType (default: 100).",
                },
                "offset": {
                    "type": "integer",
                    "description": "Skip first N results for pagination.",
                },
                "sourceAssetPath": {
                    "type": "string",
                    "description": "Source asset path for duplicate operation.",
                },
                "sourceAssetGuid": {
                    "type": "string",
                    "description": "Source asset GUID for duplicate operation.",
                },
                "destinationAssetPath": {
                    "type": "string",
                    "description": "Destination path for duplicate operation.",
                },
            },
        },
        ["operation"],
    )


def prefab_manage_schema() -> dict[str, Any]:
    """Schema for the unity_prefab_crud MCP tool."""
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
                        "instantiate",
                        "unpack",
                        "applyOverrides",
                        "revertOverrides",
                        "editAsset",
                        "editMultiple",
                    ],
                    "description": "Prefab operation to perform.",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "Hierarchy path of GameObject (for create/update/unpack operations).",
                },
                "prefabPath": {
                    "type": "string",
                    "description": "Asset path to prefab file (e.g., 'Assets/Prefabs/MyPrefab.prefab').",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path for instantiation.",
                },
                "position": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Position for instantiated prefab.",
                },
                "rotation": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Euler rotation for instantiated prefab.",
                },
                "unpackMode": {
                    "type": "string",
                    "enum": ["completely", "outermost"],
                    "description": "Unpack mode: 'completely' or 'outermost'.",
                },
                "includeOverrides": {
                    "type": "boolean",
                    "description": "Include override information in inspect operation.",
                },
                "tag": {
                    "type": "string",
                    "description": "Tag to set on prefab root (editAsset/editMultiple).",
                },
                "layer": {
                    "oneOf": [{"type": "integer"}, {"type": "string"}],
                    "description": "Layer to set on prefab root by name or number (editAsset/editMultiple).",
                },
                "componentChanges": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "componentType": {
                                "type": "string",
                                "description": "Component type name (e.g., 'Rigidbody2D'). Added if not present.",
                            },
                            "propertyChanges": {
                                "type": "object",
                                "additionalProperties": True,
                                "description": "Property values to set on the component.",
                            },
                        },
                        "required": ["componentType"],
                    },
                    "description": "Components to add/update on prefab (editAsset/editMultiple). Component is added if missing.",
                },
                "removeComponents": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Component type names to remove from prefab (editAsset/editMultiple).",
                },
                "prefabPaths": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Array of prefab asset paths for editMultiple operation.",
                },
            },
        },
        ["operation"],
    )


def vector_sprite_convert_schema() -> dict[str, Any]:
    """Schema for the unity_vector_sprite_convert MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "primitiveToSprite",
                        "svgToSprite",
                        "textureToSprite",
                        "createColorSprite",
                    ],
                    "description": "Vector/sprite conversion operation.",
                },
                "primitiveType": {
                    "type": "string",
                    "enum": ["square", "circle", "triangle", "polygon"],
                    "description": "Primitive shape type for sprite generation.",
                },
                "width": {
                    "type": "integer",
                    "description": "Width of generated sprite in pixels.",
                },
                "height": {
                    "type": "integer",
                    "description": "Height of generated sprite in pixels.",
                },
                "color": {
                    "type": "object",
                    "properties": {
                        "r": {"type": "number", "minimum": 0, "maximum": 1},
                        "g": {"type": "number", "minimum": 0, "maximum": 1},
                        "b": {"type": "number", "minimum": 0, "maximum": 1},
                        "a": {"type": "number", "minimum": 0, "maximum": 1},
                    },
                    "description": "Fill RGBA color (0-1 range).",
                },
                "outlineColor": {
                    "type": "object",
                    "properties": {
                        "r": {"type": "number", "minimum": 0, "maximum": 1},
                        "g": {"type": "number", "minimum": 0, "maximum": 1},
                        "b": {"type": "number", "minimum": 0, "maximum": 1},
                        "a": {"type": "number", "minimum": 0, "maximum": 1},
                    },
                    "description": "Outline RGBA color (0-1 range). Used with outlineWidth for primitiveToSprite and createColorSprite. If omitted when outlineWidth > 0, defaults to a contrasting color based on fill color.",
                },
                "outlineWidth": {
                    "type": "integer",
                    "description": "Outline width in pixels. 0 = no outline (default).",
                },
                "sides": {
                    "type": "integer",
                    "description": "Number of sides for polygon primitive.",
                },
                "svgPath": {
                    "type": "string",
                    "description": "Path to SVG file for conversion.",
                },
                "texturePath": {
                    "type": "string",
                    "description": "Path to texture file for sprite conversion.",
                },
                "outputPath": {
                    "type": "string",
                    "description": "Output path for generated sprite asset.",
                },
                "pixelsPerUnit": {
                    "type": "number",
                    "description": "Pixels per unit for sprite import settings.",
                },
                "spriteMode": {
                    "type": "string",
                    "enum": ["single", "multiple"],
                    "description": "Sprite mode: 'single' or 'multiple'.",
                },
            },
        },
        ["operation"],
    )
