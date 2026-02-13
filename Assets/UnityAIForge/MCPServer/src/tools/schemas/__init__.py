"""Schema definitions for Unity-AI-Forge MCP tools.

This module organizes tool schemas into logical groups:
- common: Shared type definitions (Vector3, Color, Bounds, etc.)
- utility: Ping, compilation await, playmode, console log
- low_level: CRUD operations (Scene, GameObject, Component, Asset, etc.)
- mid_level: Batch operations and presets (Transform, Physics, UI, etc.)
- visual: Sprite, animation, material, light, particle, event wiring
- gamekit_core: Actor, Manager, Interaction, UICommand, Machinations, SceneFlow
- gamekit_systems: Health, Spawner, Timer, AI, and other game mechanics
- gamekit_pillar: 3-Pillar UI/Logic/Presentation tools
- graph: Class dependency and scene reference/relationship analysis
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
    gamekit_actor_schema,
    gamekit_interaction_schema,
    gamekit_machinations_schema,
    gamekit_manager_schema,
    gamekit_sceneflow_schema,
    gamekit_ui_command_schema,
)
from tools.schemas.gamekit_pillar import (
    gamekit_audio_schema,
    gamekit_combat_schema,
    gamekit_feedback_schema,
    gamekit_ui_binding_schema,
    gamekit_ui_list_schema,
    gamekit_ui_selection_schema,
    gamekit_ui_slot_schema,
    gamekit_vfx_schema,
)
from tools.schemas.gamekit_systems import (
    gamekit_ai_schema,
    gamekit_animation_sync_schema,
    gamekit_collectible_schema,
    gamekit_dialogue_schema,
    gamekit_effect_schema,
    gamekit_health_schema,
    gamekit_inventory_schema,
    gamekit_projectile_schema,
    gamekit_quest_schema,
    gamekit_save_schema,
    gamekit_spawner_schema,
    gamekit_status_effect_schema,
    gamekit_timer_schema,
    gamekit_trigger_zone_schema,
    gamekit_waypoint_schema,
)
from tools.schemas.graph import (
    class_dependency_graph_schema,
    scene_reference_graph_schema,
    scene_relationship_graph_schema,
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
    audio_source_bundle_schema,
    camera_rig_schema,
    character_controller_bundle_schema,
    input_profile_schema,
    physics_bundle_schema,
    rect_transform_batch_schema,
    tilemap_bundle_schema,
    transform_batch_schema,
    ui_foundation_schema,
    ui_hierarchy_schema,
    ui_navigation_schema,
    ui_state_schema,
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
    "physics_bundle_schema",
    "camera_rig_schema",
    "ui_foundation_schema",
    "ui_hierarchy_schema",
    "ui_state_schema",
    "ui_navigation_schema",
    "audio_source_bundle_schema",
    "input_profile_schema",
    "character_controller_bundle_schema",
    "tilemap_bundle_schema",
    # visual
    "sprite2d_bundle_schema",
    "animation2d_bundle_schema",
    "animation3d_bundle_schema",
    "material_bundle_schema",
    "light_bundle_schema",
    "particle_bundle_schema",
    "event_wiring_schema",
    # gamekit_core
    "gamekit_actor_schema",
    "gamekit_manager_schema",
    "gamekit_interaction_schema",
    "gamekit_ui_command_schema",
    "gamekit_machinations_schema",
    "gamekit_sceneflow_schema",
    # gamekit_systems
    "gamekit_health_schema",
    "gamekit_spawner_schema",
    "gamekit_timer_schema",
    "gamekit_ai_schema",
    "gamekit_collectible_schema",
    "gamekit_projectile_schema",
    "gamekit_waypoint_schema",
    "gamekit_trigger_zone_schema",
    "gamekit_animation_sync_schema",
    "gamekit_effect_schema",
    "gamekit_save_schema",
    "gamekit_inventory_schema",
    "gamekit_dialogue_schema",
    "gamekit_quest_schema",
    "gamekit_status_effect_schema",
    # gamekit_pillar
    "gamekit_ui_binding_schema",
    "gamekit_ui_list_schema",
    "gamekit_ui_slot_schema",
    "gamekit_ui_selection_schema",
    "gamekit_combat_schema",
    "gamekit_feedback_schema",
    "gamekit_vfx_schema",
    "gamekit_audio_schema",
    # graph
    "class_dependency_graph_schema",
    "scene_reference_graph_schema",
    "scene_relationship_graph_schema",
]
