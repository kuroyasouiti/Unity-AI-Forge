"""Common schema type definitions for Unity-AI-Forge MCP tools.

Provides reusable schema helpers for common Unity types like Vector3, Color, Bounds, etc.
These ensure consistency across all tool schemas.
"""

from __future__ import annotations

from typing import Any


def schema_with_required(schema: dict[str, Any], required: list[str]) -> dict[str, Any]:
    """Add required fields and disable additional properties to a schema.

    Args:
        schema: Base JSON schema dictionary
        required: List of required property names

    Returns:
        Enhanced schema with required fields and additionalProperties=False
    """
    enriched = dict(schema)
    enriched["required"] = required
    enriched["additionalProperties"] = False
    return enriched


def vector2_schema(description: str = "") -> dict[str, Any]:
    """Create a Vector2 schema (x, y).

    Args:
        description: Optional description for the field

    Returns:
        JSON schema for a 2D vector

    Example:
        >>> vector2_schema("Anchor position (0-1 range)")
        {"type": "object", "properties": {"x": ..., "y": ...}, "description": "..."}
    """
    schema: dict[str, Any] = {
        "type": "object",
        "properties": {
            "x": {"type": "number"},
            "y": {"type": "number"},
        },
    }
    if description:
        schema["description"] = description
    return schema


def vector3_schema(description: str = "") -> dict[str, Any]:
    """Create a Vector3 schema (x, y, z).

    Args:
        description: Optional description for the field

    Returns:
        JSON schema for a 3D vector

    Example:
        >>> vector3_schema("World position of the object")
        {"type": "object", "properties": {"x": ..., "y": ..., "z": ...}, "description": "..."}
    """
    schema: dict[str, Any] = {
        "type": "object",
        "properties": {
            "x": {"type": "number"},
            "y": {"type": "number"},
            "z": {"type": "number"},
        },
    }
    if description:
        schema["description"] = description
    return schema


def position_schema(description: str = "World position (x, y, z).") -> dict[str, Any]:
    """Create a position schema (alias for Vector3 with default description).

    Args:
        description: Description for the position field

    Returns:
        JSON schema for a position
    """
    return vector3_schema(description)


def rotation_schema(description: str = "Euler rotation in degrees (x, y, z).") -> dict[str, Any]:
    """Create a rotation schema (Euler angles).

    Args:
        description: Description for the rotation field

    Returns:
        JSON schema for rotation
    """
    return vector3_schema(description)


def size_schema(description: str = "Size (width, height, depth).") -> dict[str, Any]:
    """Create a size schema (Vector3).

    Args:
        description: Description for the size field

    Returns:
        JSON schema for size
    """
    return vector3_schema(description)


def color_rgba_schema(
    description: str = "RGBA color (0-1 range for each component).",
) -> dict[str, Any]:
    """Create a Color schema (r, g, b, a).

    Args:
        description: Description for the color field

    Returns:
        JSON schema for an RGBA color with value constraints

    Example:
        >>> color_rgba_schema("Sprite tint color")
        {"type": "object", "properties": {"r": ..., "g": ..., "b": ..., "a": ...}, ...}
    """
    return {
        "type": "object",
        "properties": {
            "r": {
                "type": "number",
                "minimum": 0,
                "maximum": 1,
                "description": "Red component (0-1)",
            },
            "g": {
                "type": "number",
                "minimum": 0,
                "maximum": 1,
                "description": "Green component (0-1)",
            },
            "b": {
                "type": "number",
                "minimum": 0,
                "maximum": 1,
                "description": "Blue component (0-1)",
            },
            "a": {
                "type": "number",
                "minimum": 0,
                "maximum": 1,
                "description": "Alpha component (0-1)",
            },
        },
        "description": description,
    }


def bounds_schema(
    description: str = "Bounding box (xMin, xMax, yMin, yMax, zMin, zMax).",
) -> dict[str, Any]:
    """Create a Bounds schema for 3D bounding boxes.

    Args:
        description: Description for the bounds field

    Returns:
        JSON schema for 3D bounds
    """
    return {
        "type": "object",
        "properties": {
            "xMin": {"type": "integer", "description": "Minimum X coordinate"},
            "xMax": {"type": "integer", "description": "Maximum X coordinate"},
            "yMin": {"type": "integer", "description": "Minimum Y coordinate"},
            "yMax": {"type": "integer", "description": "Maximum Y coordinate"},
            "zMin": {"type": "integer", "description": "Minimum Z coordinate"},
            "zMax": {"type": "integer", "description": "Maximum Z coordinate"},
        },
        "description": description,
    }


def bounds_2d_schema(
    description: str = "2D bounding box (xMin, xMax, yMin, yMax).",
) -> dict[str, Any]:
    """Create a 2D Bounds schema.

    Args:
        description: Description for the bounds field

    Returns:
        JSON schema for 2D bounds
    """
    return {
        "type": "object",
        "properties": {
            "xMin": {"type": "integer", "description": "Minimum X coordinate"},
            "xMax": {"type": "integer", "description": "Maximum X coordinate"},
            "yMin": {"type": "integer", "description": "Minimum Y coordinate"},
            "yMax": {"type": "integer", "description": "Maximum Y coordinate"},
        },
        "description": description,
    }


def cell_position_schema(
    description: str = "Grid cell position (integer coordinates).",
) -> dict[str, Any]:
    """Create a cell position schema for tilemap operations.

    Args:
        description: Description for the cell position

    Returns:
        JSON schema for integer grid coordinates
    """
    return {
        "type": "object",
        "properties": {
            "x": {"type": "integer", "description": "X cell coordinate"},
            "y": {"type": "integer", "description": "Y cell coordinate"},
            "z": {"type": "integer", "description": "Z cell coordinate (layer)"},
        },
        "description": description,
    }


def operation_enum_schema(
    operations: list[str], description: str = "Operation to perform."
) -> dict[str, Any]:
    """Create an operation enum schema.

    Args:
        operations: List of valid operation names
        description: Description for the operation field

    Returns:
        JSON schema for operation enum
    """
    return {
        "type": "string",
        "enum": operations,
        "description": description,
    }
