"""Tests for tools/schemas/ module.

Validates that all schema functions return valid JSON Schema dicts
with expected structure and constraints.
"""

from __future__ import annotations

import pytest

from tools.schemas import (
    animation2d_bundle_schema,
    animation3d_bundle_schema,
    asset_manage_schema,
    camera_bundle_schema,
    class_catalog_schema,
    class_dependency_graph_schema,
    compilation_await_schema,
    component_manage_schema,
    console_log_schema,
    event_wiring_schema,
    game_object_manage_schema,
    gamekit_data_schema,
    gamekit_ui_schema,
    input_profile_schema,
    light_bundle_schema,
    material_bundle_schema,
    navmesh_bundle_schema,
    particle_bundle_schema,
    physics_bundle_schema,
    ping_schema,
    playmode_control_schema,
    prefab_manage_schema,
    project_settings_manage_schema,
    rect_transform_batch_schema,
    scene_dependency_schema,
    scene_manage_schema,
    scene_reference_graph_schema,
    scene_relationship_graph_schema,
    script_syntax_schema,
    scriptable_object_manage_schema,
    sprite2d_bundle_schema,
    tilemap_bundle_schema,
    transform_batch_schema,
    ui_foundation_schema,
    ui_navigation_schema,
    ui_state_schema,
    uitk_asset_schema,
    uitk_document_schema,
    validate_integrity_schema,
    vector_sprite_convert_schema,
)


# Collect all schema functions for parametrized tests
ALL_SCHEMA_FUNCTIONS = [
    ("ping", ping_schema),
    ("compilation_await", compilation_await_schema),
    ("playmode_control", playmode_control_schema),
    ("console_log", console_log_schema),
    ("scene_manage", scene_manage_schema),
    ("game_object_manage", game_object_manage_schema),
    ("component_manage", component_manage_schema),
    ("asset_manage", asset_manage_schema),
    ("project_settings_manage", project_settings_manage_schema),
    ("scriptable_object_manage", scriptable_object_manage_schema),
    ("prefab_manage", prefab_manage_schema),
    ("vector_sprite_convert", vector_sprite_convert_schema),
    ("transform_batch", transform_batch_schema),
    ("rect_transform_batch", rect_transform_batch_schema),
    ("camera_bundle", camera_bundle_schema),
    ("ui_foundation", ui_foundation_schema),
    ("ui_state", ui_state_schema),
    ("ui_navigation", ui_navigation_schema),
    ("input_profile", input_profile_schema),
    ("tilemap_bundle", tilemap_bundle_schema),
    ("uitk_document", uitk_document_schema),
    ("uitk_asset", uitk_asset_schema),
    ("sprite2d_bundle", sprite2d_bundle_schema),
    ("animation2d_bundle", animation2d_bundle_schema),
    ("animation3d_bundle", animation3d_bundle_schema),
    ("material_bundle", material_bundle_schema),
    ("light_bundle", light_bundle_schema),
    ("particle_bundle", particle_bundle_schema),
    ("event_wiring", event_wiring_schema),
    ("physics_bundle", physics_bundle_schema),
    ("navmesh_bundle", navmesh_bundle_schema),
    ("gamekit_ui", gamekit_ui_schema),
    ("gamekit_data", gamekit_data_schema),
    ("class_catalog", class_catalog_schema),
    ("class_dependency_graph", class_dependency_graph_schema),
    ("scene_dependency", scene_dependency_schema),
    ("scene_reference_graph", scene_reference_graph_schema),
    ("scene_relationship_graph", scene_relationship_graph_schema),
    ("script_syntax", script_syntax_schema),
    ("validate_integrity", validate_integrity_schema),
]


class TestSchemaStructure:
    """Verify that all schema functions return valid JSON Schema objects."""

    @pytest.mark.parametrize("name,schema_fn", ALL_SCHEMA_FUNCTIONS, ids=[s[0] for s in ALL_SCHEMA_FUNCTIONS])
    def test_returns_dict(self, name: str, schema_fn) -> None:
        schema = schema_fn()
        assert isinstance(schema, dict), f"{name} schema should return a dict"

    @pytest.mark.parametrize("name,schema_fn", ALL_SCHEMA_FUNCTIONS, ids=[s[0] for s in ALL_SCHEMA_FUNCTIONS])
    def test_has_type_object(self, name: str, schema_fn) -> None:
        schema = schema_fn()
        assert schema.get("type") == "object", f"{name} schema should have type 'object'"

    @pytest.mark.parametrize("name,schema_fn", ALL_SCHEMA_FUNCTIONS, ids=[s[0] for s in ALL_SCHEMA_FUNCTIONS])
    def test_has_properties(self, name: str, schema_fn) -> None:
        schema = schema_fn()
        assert "properties" in schema, f"{name} schema should have 'properties'"
        assert isinstance(schema["properties"], dict)

    @pytest.mark.parametrize("name,schema_fn", ALL_SCHEMA_FUNCTIONS, ids=[s[0] for s in ALL_SCHEMA_FUNCTIONS])
    def test_has_required_field(self, name: str, schema_fn) -> None:
        schema = schema_fn()
        # Schemas with no properties (e.g. ping) may omit "required"
        if schema.get("properties"):
            assert "required" in schema, f"{name} schema with properties should have 'required'"
            assert isinstance(schema["required"], list)

    @pytest.mark.parametrize("name,schema_fn", ALL_SCHEMA_FUNCTIONS, ids=[s[0] for s in ALL_SCHEMA_FUNCTIONS])
    def test_required_fields_exist_in_properties(self, name: str, schema_fn) -> None:
        schema = schema_fn()
        required = schema.get("required", [])
        properties = schema.get("properties", {})
        for field in required:
            assert field in properties, (
                f"{name}: required field '{field}' not in properties"
            )


class TestOperationEnums:
    """Verify schemas that use operation enum have valid operation values."""

    @pytest.mark.parametrize("name,schema_fn", ALL_SCHEMA_FUNCTIONS, ids=[s[0] for s in ALL_SCHEMA_FUNCTIONS])
    def test_operation_has_enum_or_const(self, name: str, schema_fn) -> None:
        schema = schema_fn()
        properties = schema.get("properties", {})
        if "operation" in properties:
            op_schema = properties["operation"]
            has_enum = "enum" in op_schema
            has_const = "const" in op_schema
            has_oneof = "oneOf" in op_schema
            assert has_enum or has_const or has_oneof, (
                f"{name}: 'operation' property should have 'enum', 'const', or 'oneOf'"
            )

    @pytest.mark.parametrize("name,schema_fn", ALL_SCHEMA_FUNCTIONS, ids=[s[0] for s in ALL_SCHEMA_FUNCTIONS])
    def test_operation_is_required(self, name: str, schema_fn) -> None:
        schema = schema_fn()
        required = schema.get("required", [])
        properties = schema.get("properties", {})
        if "operation" in properties:
            assert "operation" in required, (
                f"{name}: 'operation' should be in required list"
            )


class TestSchemaCount:
    """Verify the total number of schema functions matches expectations."""

    def test_total_schema_functions(self) -> None:
        assert len(ALL_SCHEMA_FUNCTIONS) == 40, (
            f"Expected 40 schema functions but found {len(ALL_SCHEMA_FUNCTIONS)}"
        )


class TestNewFeatureSchemas:
    """Verify schemas for newly added features."""

    def test_component_manage_has_crossSceneUpdate(self) -> None:
        schema = component_manage_schema()
        ops = schema["properties"]["operation"]["enum"]
        assert "crossSceneUpdate" in ops

    def test_component_manage_has_updates_property(self) -> None:
        schema = component_manage_schema()
        assert "updates" in schema["properties"]
        updates = schema["properties"]["updates"]
        assert updates["type"] == "array"
        # Verify items have required scenePath, gameObjectPath, etc.
        items_required = updates["items"]["required"]
        assert "scenePath" in items_required
        assert "gameObjectPath" in items_required
        assert "componentType" in items_required
        assert "propertyChanges" in items_required

    def test_component_manage_operation_required_only(self) -> None:
        """crossSceneUpdate doesn't need top-level componentType, so required should be ['operation']."""
        schema = component_manage_schema()
        assert schema["required"] == ["operation"]

    def test_gamekit_data_has_createMultiple(self) -> None:
        schema = gamekit_data_schema()
        ops = schema["properties"]["operation"]["enum"]
        assert "createMultiple" in ops

    def test_gamekit_data_has_autoCreateAsset(self) -> None:
        schema = gamekit_data_schema()
        assert "autoCreateAsset" in schema["properties"]
        assert schema["properties"]["autoCreateAsset"]["type"] == "boolean"

    def test_gamekit_data_has_items(self) -> None:
        schema = gamekit_data_schema()
        assert "items" in schema["properties"]
        assert schema["properties"]["items"]["type"] == "array"

    def test_gamekit_ui_has_createMultiple(self) -> None:
        schema = gamekit_ui_schema()
        ops = schema["properties"]["operation"]["enum"]
        assert "createMultiple" in ops

    def test_gamekit_ui_has_bindings(self) -> None:
        schema = gamekit_ui_schema()
        assert "bindings" in schema["properties"]
        assert schema["properties"]["bindings"]["type"] == "array"
