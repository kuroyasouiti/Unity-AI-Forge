"""Single source of truth for MCP tool name to Unity bridge name mapping.

All MCP tools (except ``unity_batch_sequential_execute``, which is handled
separately) are listed here.  Both ``register_tools.py`` and
``batch_sequential.py`` import from this module so that name mappings are
never duplicated.
"""

from __future__ import annotations

# MCP tool name → internal Unity bridge handler name.
# Kept in the same order as CommandHandlerInitializer.cs registrations.
TOOL_NAME_TO_BRIDGE: dict[str, str] = {
    # ── Utility ──────────────────────────────────────────────
    "unity_ping": "pingUnityEditor",
    "unity_compilation_await": "compilationAwait",
    # ── Low-Level CRUD ───────────────────────────────────────
    "unity_scene_crud": "sceneManage",
    "unity_gameobject_crud": "gameObjectManage",
    "unity_component_crud": "componentManage",
    "unity_asset_crud": "assetManage",
    "unity_scriptableObject_crud": "scriptableObjectManage",
    "unity_prefab_crud": "prefabManage",
    "unity_vector_sprite_convert": "vectorSpriteConvert",
    "unity_projectSettings_crud": "projectSettingsManage",
    # ── Mid-Level Batch ──────────────────────────────────────
    "unity_transform_batch": "transformBatch",
    "unity_rectTransform_batch": "rectTransformBatch",
    "unity_physics_bundle": "physicsBundle",
    "unity_camera_rig": "cameraRig",
    "unity_ui_foundation": "uiFoundation",
    "unity_ui_hierarchy": "uiHierarchy",
    "unity_ui_state": "uiState",
    "unity_ui_navigation": "uiNavigation",
    "unity_audio_source_bundle": "audioSourceBundle",
    "unity_input_profile": "inputProfile",
    "unity_character_controller_bundle": "characterControllerBundle",
    "unity_tilemap_bundle": "tilemapBundle",
    "unity_sprite2d_bundle": "sprite2DBundle",
    "unity_animation2d_bundle": "animation2DBundle",
    # ── Mid-Level UI Toolkit ────────────────────────────────
    "unity_uitk_document": "uitkDocument",
    "unity_uitk_asset": "uitkAsset",
    # ── Dev-Cycle & Visual ───────────────────────────────────
    "unity_playmode_control": "playModeControl",
    "unity_console_log": "consoleLog",
    "unity_material_bundle": "materialBundle",
    "unity_light_bundle": "lightBundle",
    "unity_particle_bundle": "particleBundle",
    "unity_animation3d_bundle": "animation3DBundle",
    "unity_event_wiring": "eventWiring",
    # ── High-Level GameKit – Core ────────────────────────────
    "unity_gamekit_actor": "gamekitActor",
    "unity_gamekit_manager": "gamekitManager",
    "unity_gamekit_interaction": "gamekitInteraction",
    "unity_gamekit_ui_command": "gamekitUICommand",
    "unity_gamekit_machinations": "gamekitMachinations",
    "unity_gamekit_sceneflow": "gamekitSceneFlow",
    # ── High-Level GameKit – Phase 1 ────────────────────────
    "unity_gamekit_health": "gamekitHealth",
    "unity_gamekit_spawner": "gamekitSpawner",
    "unity_gamekit_timer": "gamekitTimer",
    "unity_gamekit_ai": "gamekitAI",
    # ── High-Level GameKit – Phase 2 ────────────────────────
    "unity_gamekit_collectible": "gamekitCollectible",
    "unity_gamekit_projectile": "gamekitProjectile",
    "unity_gamekit_waypoint": "gamekitWaypoint",
    "unity_gamekit_trigger_zone": "gamekitTriggerZone",
    # ── High-Level GameKit – Phase 3 ────────────────────────
    "unity_gamekit_animation_sync": "gamekitAnimationSync",
    "unity_gamekit_effect": "gamekitEffect",
    # ── High-Level GameKit – Phase 4 ────────────────────────
    "unity_gamekit_save": "gamekitSave",
    "unity_gamekit_inventory": "gamekitInventory",
    # ── High-Level GameKit – Phase 5 ────────────────────────
    "unity_gamekit_dialogue": "gamekitDialogue",
    "unity_gamekit_quest": "gamekitQuest",
    "unity_gamekit_status_effect": "gamekitStatusEffect",
    # ── 3-Pillar Architecture – UI ───────────────────────────
    "unity_gamekit_ui_binding": "gamekitUIBinding",
    "unity_gamekit_ui_list": "gamekitUIList",
    "unity_gamekit_ui_slot": "gamekitUISlot",
    "unity_gamekit_ui_selection": "gamekitUISelection",
    # ── 3-Pillar Architecture – Logic ────────────────────────
    "unity_gamekit_combat": "gamekitCombat",
    # ── 3-Pillar Architecture – Presentation ─────────────────
    "unity_gamekit_feedback": "gamekitFeedback",
    "unity_gamekit_vfx": "gamekitVFX",
    "unity_gamekit_audio": "gamekitAudio",
    # ── Graph Analysis ───────────────────────────────────────
    "unity_class_dependency_graph": "classDependencyGraph",
    "unity_scene_reference_graph": "sceneReferenceGraph",
    "unity_scene_relationship_graph": "sceneRelationshipGraph",
}

# Reverse mapping: bridge name → MCP tool name.
BRIDGE_TO_TOOL_NAME: dict[str, str] = {v: k for k, v in TOOL_NAME_TO_BRIDGE.items()}

# Tools that require special handling in register_tools.py and are NOT
# simple ``_call_bridge_tool(bridge_name, args)`` dispatches.
SPECIAL_TOOLS: set[str] = {
    "unity_ping",
    "unity_compilation_await",
    "unity_asset_crud",
    "unity_batch_sequential_execute",
}


def resolve_tool_name(tool_name: str) -> str:
    """Resolve an MCP tool name to its internal bridge tool name.

    Accepts either an MCP name (``unity_gameobject_crud``) or a bridge name
    already (``gameObjectManage``).  Raises ``ValueError`` for unknown names.
    """
    # MCP name → bridge name
    if tool_name in TOOL_NAME_TO_BRIDGE:
        return TOOL_NAME_TO_BRIDGE[tool_name]

    # Already a bridge name
    if tool_name in BRIDGE_TO_TOOL_NAME:
        return tool_name

    raise ValueError(
        f"Unsupported tool name: {tool_name}. "
        f"Use MCP names (e.g., 'unity_gameobject_crud') or internal names (e.g., 'gameObjectManage'). "
        f"Available tools: {', '.join(sorted(TOOL_NAME_TO_BRIDGE.keys()))}"
    )
