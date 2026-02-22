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
                    ],
                    "description": (
                        "Integrity check operation. "
                        "'missingScripts': detect null MonoBehaviours. "
                        "'nullReferences': detect dangling object references. "
                        "'brokenEvents': detect UnityEvent listeners with null targets or missing methods. "
                        "'brokenPrefabs': detect prefab instances with missing or disconnected assets. "
                        "'removeMissingScripts': detect and remove all missing MonoBehaviour scripts (undoable). "
                        "'all': run all checks and return categorized summary."
                    ),
                },
                "rootPath": {
                    "type": "string",
                    "description": (
                        "Optional GameObject path to limit analysis to a subtree "
                        "(e.g., '/Canvas/Panel'). If omitted, scans the entire active scene."
                    ),
                },
            },
        },
        ["operation"],
    )
