"""Schema definitions for Unity-AI-Forge MCP tools.

This module organizes tool schemas into logical groups:
- common: Shared type definitions (Vector3, Color, Bounds, etc.)
- utility: Ping, compilation await, playmode, console log
- low_level: CRUD operations (Scene, GameObject, Component, Asset, etc.)
- mid_level: Batch operations and presets (Transform, Physics, UI, etc.)
- visual: Sprite, animation, material, light, particle, event wiring
- gamekit_core: UICommand (UI Pillar)
- gamekit_systems: AnimationSync, Effect (Presentation Pillar)
- gamekit_pillar: UI Pillar + Presentation Pillar tools
- graph: Integrity validation, class/scene dependency & reference analysis (Logic Pillar)
"""

from tools.schemas.common import (
    bounds_2d_schema,
    bounds_schema,
    cell_position_schema,
    color_rgba_schema,
    operation_enum_schema,
    position_schema,
    rotation_schema,
    schema_with_required,
    size_schema,
    vector2_schema,
    vector3_schema,
)
from tools.schemas.gamekit_core import (
    gamekit_ui_command_schema,
)
from tools.schemas.gamekit_pillar import (
    gamekit_audio_schema,
    gamekit_feedback_schema,
    gamekit_ui_binding_schema,
    gamekit_ui_list_schema,
    gamekit_ui_selection_schema,
    gamekit_ui_slot_schema,
    gamekit_vfx_schema,
)
from tools.schemas.gamekit_systems import (
    gamekit_animation_sync_schema,
    gamekit_effect_schema,
)
from tools.schemas.graph import (
    class_catalog_schema,
    class_dependency_graph_schema,
    scene_dependency_schema,
    scene_reference_graph_schema,
    scene_relationship_graph_schema,
    script_syntax_schema,
    validate_integrity_schema,
)
from tools.schemas.low_level import (
    asset_manage_schema,
    component_manage_schema,
    game_object_manage_schema,
    prefab_manage_schema,
    project_settings_manage_schema,
    scene_manage_schema,
    scriptable_object_manage_schema,
    vector_sprite_convert_schema,
)
from tools.schemas.mid_level import (
    camera_rig_schema,
    input_profile_schema,
    rect_transform_batch_schema,
    tilemap_bundle_schema,
    transform_batch_schema,
    ui_foundation_schema,
    ui_hierarchy_schema,
    ui_navigation_schema,
    ui_state_schema,
    uitk_asset_schema,
    uitk_document_schema,
)
from tools.schemas.utility import (
    compilation_await_schema,
    console_log_schema,
    ping_schema,
    playmode_control_schema,
)
from tools.schemas.visual import (
    animation2d_bundle_schema,
    animation3d_bundle_schema,
    event_wiring_schema,
    light_bundle_schema,
    material_bundle_schema,
    particle_bundle_schema,
    sprite2d_bundle_schema,
)

__all__ = [
    # common
    "schema_with_required",
    "vector2_schema",
    "vector3_schema",
    "position_schema",
    "rotation_schema",
    "size_schema",
    "color_rgba_schema",
    "bounds_schema",
    "bounds_2d_schema",
    "cell_position_schema",
    "operation_enum_schema",
    # utility
    "ping_schema",
    "compilation_await_schema",
    "playmode_control_schema",
    "console_log_schema",
    # low_level
    "scene_manage_schema",
    "game_object_manage_schema",
    "component_manage_schema",
    "asset_manage_schema",
    "project_settings_manage_schema",
    "scriptable_object_manage_schema",
    "prefab_manage_schema",
    "vector_sprite_convert_schema",
    # mid_level
    "transform_batch_schema",
    "rect_transform_batch_schema",
    "camera_rig_schema",
    "ui_foundation_schema",
    "ui_hierarchy_schema",
    "ui_state_schema",
    "ui_navigation_schema",
    "input_profile_schema",
    "tilemap_bundle_schema",
    "uitk_document_schema",
    "uitk_asset_schema",
    # visual
    "sprite2d_bundle_schema",
    "animation2d_bundle_schema",
    "animation3d_bundle_schema",
    "material_bundle_schema",
    "light_bundle_schema",
    "particle_bundle_schema",
    "event_wiring_schema",
    # gamekit_core (UI Pillar)
    "gamekit_ui_command_schema",
    # gamekit_systems (Presentation Pillar)
    "gamekit_animation_sync_schema",
    "gamekit_effect_schema",
    # gamekit_pillar (UI + Presentation Pillar)
    "gamekit_ui_binding_schema",
    "gamekit_ui_list_schema",
    "gamekit_ui_slot_schema",
    "gamekit_ui_selection_schema",
    "gamekit_feedback_schema",
    "gamekit_vfx_schema",
    "gamekit_audio_schema",
    # graph (Logic Pillar — integrity validation + dependency/reference analysis + type catalog)
    "validate_integrity_schema",
    "class_catalog_schema",
    "class_dependency_graph_schema",
    "scene_dependency_schema",
    "scene_reference_graph_schema",
    "scene_relationship_graph_schema",
    "script_syntax_schema",
]
