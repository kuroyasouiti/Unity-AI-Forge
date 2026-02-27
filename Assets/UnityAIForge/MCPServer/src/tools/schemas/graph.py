"""Schema definitions for graph analysis MCP tools."""

from __future__ import annotations

from typing import Any

from tools.schemas.common import schema_with_required


def class_dependency_graph_schema() -> dict[str, Any]:
    """Schema for the unity_class_dependency_graph MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "analyzeClass",
                        "analyzeAssembly",
                        "analyzeNamespace",
                        "findDependents",
                        "findDependencies",
                    ],
                    "description": "Class dependency graph operation.",
                },
                "target": {
                    "type": "string",
                    "description": "Target class name, assembly name, or namespace to analyze.",
                },
                "depth": {
                    "type": "integer",
                    "description": "Analysis depth for recursive dependency traversal (default: 1).",
                    "default": 1,
                },
                "includeUnityTypes": {
                    "type": "boolean",
                    "description": "Include Unity/System types in the graph (default: false).",
                    "default": False,
                },
                "format": {
                    "type": "string",
                    "enum": ["json", "dot", "mermaid", "summary"],
                    "description": "Output format (default: json).",
                    "default": "json",
                },
            },
        },
        ["operation"],
    )


def scene_reference_graph_schema() -> dict[str, Any]:
    """Schema for the unity_scene_reference_graph MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "analyzeScene",
                        "analyzeObject",
                        "findReferencesTo",
                        "findReferencesFrom",
                        "findOrphans",
                    ],
                    "description": "Scene reference graph operation.",
                },
                "scenePath": {
                    "type": "string",
                    "description": "Scene path to analyze (e.g., 'Assets/Scenes/Main.unity'). If omitted, uses active scene.",
                },
                "objectPath": {
                    "type": "string",
                    "description": "GameObject path for object-specific operations (e.g., '/Player/Weapon').",
                },
                "includeHierarchy": {
                    "type": "boolean",
                    "description": "Include parent-child hierarchy relationships (default: true).",
                    "default": True,
                },
                "includeEvents": {
                    "type": "boolean",
                    "description": "Include UnityEvent listener references (default: true).",
                    "default": True,
                },
                "includeChildren": {
                    "type": "boolean",
                    "description": "Include child objects when analyzing a specific object (default: true).",
                    "default": True,
                },
                "format": {
                    "type": "string",
                    "enum": ["json", "dot", "mermaid", "summary"],
                    "description": "Output format (default: json).",
                    "default": "json",
                },
            },
        },
        ["operation"],
    )


def scene_relationship_graph_schema() -> dict[str, Any]:
    """Schema for the unity_scene_relationship_graph MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "analyzeAll",
                        "analyzeScene",
                        "findTransitionsTo",
                        "findTransitionsFrom",
                        "validateBuildSettings",
                    ],
                    "description": "Scene relationship graph operation.",
                },
                "scenePath": {
                    "type": "string",
                    "description": "Scene path for scene-specific operations.",
                },
                "includeScriptReferences": {
                    "type": "boolean",
                    "description": "Include SceneManager.LoadScene calls in scripts (default: true).",
                    "default": True,
                },
                "includeSceneFlow": {
                    "type": "boolean",
                    "description": "Include GameKitSceneFlow transitions (default: true).",
                    "default": True,
                },
                "format": {
                    "type": "string",
                    "enum": ["json", "dot", "mermaid", "summary"],
                    "description": "Output format (default: json).",
                    "default": "json",
                },
            },
        },
        ["operation"],
    )


def class_catalog_schema() -> dict[str, Any]:
    """Schema for the unity_class_catalog MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["listTypes", "inspectType"],
                    "description": "Class catalog operation.",
                },
                "searchPath": {
                    "type": "string",
                    "description": "Limit search to folder (e.g. 'Assets/Scripts').",
                },
                "typeKind": {
                    "type": "string",
                    "enum": [
                        "class",
                        "struct",
                        "interface",
                        "enum",
                        "MonoBehaviour",
                        "ScriptableObject",
                    ],
                    "description": "Filter by type kind.",
                },
                "namespace": {
                    "type": "string",
                    "description": "Filter by namespace prefix.",
                },
                "baseClass": {
                    "type": "string",
                    "description": "Filter by direct base class name.",
                },
                "namePattern": {
                    "type": "string",
                    "description": "Wildcard pattern for type name (e.g. '*Controller').",
                },
                "maxResults": {
                    "type": "integer",
                    "description": "Maximum number of results (default 100, max 1000).",
                    "default": 100,
                },
                "className": {
                    "type": "string",
                    "description": "Type name (simple or fully qualified) for inspectType.",
                },
                "includeFields": {
                    "type": "boolean",
                    "description": "Include field information in inspectType (default true).",
                    "default": True,
                },
                "includeMethods": {
                    "type": "boolean",
                    "description": "Include method information in inspectType (default false).",
                    "default": False,
                },
                "includeProperties": {
                    "type": "boolean",
                    "description": "Include property information in inspectType (default false).",
                    "default": False,
                },
            },
        },
        ["operation"],
    )


def scene_dependency_schema() -> dict[str, Any]:
    """Schema for the unity_scene_dependency MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "analyzeScene",
                        "findAssetUsage",
                        "findSharedAssets",
                        "findUnusedAssets",
                    ],
                    "description": (
                        "Scene dependency operation. "
                        "'analyzeScene': list all asset dependencies of a scene, categorized by type. "
                        "'findAssetUsage': find all scenes that reference a specific asset. "
                        "'findSharedAssets': find assets shared across multiple scenes. "
                        "'findUnusedAssets': find assets not referenced by any scene."
                    ),
                },
                "scenePath": {
                    "type": "string",
                    "description": (
                        "Scene path for analyzeScene (e.g., 'Assets/Scenes/Main.unity'). Required for analyzeScene."
                    ),
                },
                "assetPath": {
                    "type": "string",
                    "description": "Asset path for findAssetUsage (e.g., 'Assets/Materials/Player.mat').",
                },
                "searchPath": {
                    "type": "string",
                    "description": (
                        "Folder path to limit search scope. "
                        "For findAssetUsage: limit scene search. "
                        "For findUnusedAssets: folder to scan for unused assets (default: 'Assets')."
                    ),
                },
                "scenePaths": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": (
                        "Array of scene paths for findSharedAssets. "
                        "If omitted, all project scenes are analyzed."
                    ),
                },
                "includeIndirect": {
                    "type": "boolean",
                    "description": (
                        "Include transitive (indirect) dependencies in analyzeScene (default: true). "
                        "When false, only direct dependencies are returned."
                    ),
                    "default": True,
                },
                "typeFilter": {
                    "type": "string",
                    "description": (
                        "Filter results by asset category. "
                        "Categories: Material, Texture, Shader, Model, Audio, AnimationClip, "
                        "AnimatorController, Prefab, Script, Font, Asset, UXML, USS, Video, Data, Other."
                    ),
                },
                "minSharedCount": {
                    "type": "integer",
                    "description": (
                        "Minimum number of scenes an asset must be shared across "
                        "for findSharedAssets (default: 2)."
                    ),
                    "default": 2,
                },
            },
        },
        ["operation"],
    )


def script_syntax_schema() -> dict[str, Any]:
    """Schema for the unity_script_syntax MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "analyzeScript",
                        "findReferences",
                        "findUnusedCode",
                        "analyzeMetrics",
                    ],
                    "description": (
                        "Script syntax analysis operation. "
                        "'analyzeScript': parse a C# file and return its structure "
                        "(namespaces, types, methods, fields, properties) with line numbers. "
                        "'findReferences': find all references to a symbol across project scripts. "
                        "'findUnusedCode': find methods/fields that are declared but never referenced. "
                        "'analyzeMetrics': compute code metrics (lines, complexity, nesting depth)."
                    ),
                },
                "scriptPath": {
                    "type": "string",
                    "description": (
                        "Path to a C# script file (e.g., 'Assets/Scripts/PlayerController.cs'). "
                        "Required for analyzeScript. Optional for findUnusedCode and analyzeMetrics "
                        "(analyzes single file instead of project-wide)."
                    ),
                },
                "symbolName": {
                    "type": "string",
                    "description": (
                        "Symbol name to search for in findReferences "
                        "(e.g., 'Initialize', 'health', 'PlayerController')."
                    ),
                },
                "symbolType": {
                    "type": "string",
                    "enum": ["class", "method", "field", "property"],
                    "description": (
                        "Type of symbol for findReferences. "
                        "Helps classify reference types more accurately. "
                        "If omitted, all reference types are detected."
                    ),
                },
                "searchPath": {
                    "type": "string",
                    "description": (
                        "Folder path to limit search scope "
                        "(e.g., 'Assets/Scripts'). "
                        "Used by findReferences, findUnusedCode, and analyzeMetrics."
                    ),
                },
                "targetType": {
                    "type": "string",
                    "enum": ["method", "field"],
                    "description": (
                        "Filter findUnusedCode to only methods or only fields. "
                        "If omitted, both are checked."
                    ),
                },
            },
        },
        ["operation"],
    )


def validate_integrity_schema() -> dict[str, Any]:
    """Schema for the unity_validate_integrity MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "missingScripts",
                        "nullReferences",
                        "brokenEvents",
                        "brokenPrefabs",
                        "removeMissingScripts",
                        "all",
                        "typeCheck",
                        "report",
                        "checkPrefab",
                    ],
                    "description": (
                        "Integrity check operation. "
                        "'missingScripts': detect null MonoBehaviours. "
                        "'nullReferences': detect dangling object references. "
                        "'brokenEvents': detect UnityEvent listeners with null targets or missing methods. "
                        "'brokenPrefabs': detect prefab instances with missing or disconnected assets. "
                        "'removeMissingScripts': detect and remove all missing MonoBehaviour scripts (undoable). "
                        "'all': run all checks and return categorized summary. "
                        "'typeCheck': detect type mismatches in object reference fields. "
                        "'report': run integrity checks across multiple scenes (active/build/all). "
                        "'checkPrefab': validate a prefab asset for integrity issues."
                    ),
                },
                "rootPath": {
                    "type": "string",
                    "description": (
                        "Optional GameObject path to limit analysis to a subtree "
                        "(e.g., '/Canvas/Panel'). If omitted, scans the entire active scene."
                    ),
                },
                "scope": {
                    "type": "string",
                    "enum": ["active_scene", "build_scenes", "all_scenes"],
                    "description": (
                        "Scope for report operation. "
                        "'active_scene': check only the active scene (default). "
                        "'build_scenes': check all scenes in Build Settings. "
                        "'all_scenes': check all scene assets in the project (max 20)."
                    ),
                },
                "prefabPath": {
                    "type": "string",
                    "description": (
                        "Asset path to a prefab for checkPrefab operation "
                        "(e.g., 'Assets/Prefabs/Player.prefab')."
                    ),
                },
            },
        },
        ["operation"],
    )
