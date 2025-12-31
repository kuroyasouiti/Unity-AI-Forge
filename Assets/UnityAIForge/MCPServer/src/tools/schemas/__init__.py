"""Schema definitions for Unity-AI-Forge MCP tools.

This module organizes tool schemas into logical groups:
- common: Shared type definitions (Vector3, Color, Bounds, etc.)
- low_level: CRUD operations (Scene, GameObject, Component, Asset, etc.)
- mid_level: Batch operations and presets (Transform, Physics, UI, etc.)
- high_level: GameKit high-level tools (Actor, Manager, Health, etc.)
"""

from tools.schemas.common import (
    bounds_schema,
    color_rgba_schema,
    position_schema,
    rotation_schema,
    schema_with_required,
    size_schema,
    vector2_schema,
    vector3_schema,
)

__all__ = [
    "schema_with_required",
    "vector2_schema",
    "vector3_schema",
    "position_schema",
    "rotation_schema",
    "size_schema",
    "color_rgba_schema",
    "bounds_schema",
]
