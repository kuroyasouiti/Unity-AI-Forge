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
    # ── High-Level GameKit – UI Pillar ───────────────────────
    "unity_gamekit_ui_command": "gamekitUICommand",
    "unity_gamekit_ui_binding": "gamekitUIBinding",
    "unity_gamekit_ui_list": "gamekitUIList",
    "unity_gamekit_ui_slot": "gamekitUISlot",
    "unity_gamekit_ui_selection": "gamekitUISelection",
    # ── High-Level GameKit – Presentation Pillar ──────────────
    "unity_gamekit_animation_sync": "gamekitAnimationSync",
    "unity_gamekit_effect": "gamekitEffect",
    "unity_gamekit_feedback": "gamekitFeedback",
    "unity_gamekit_vfx": "gamekitVFX",
    "unity_gamekit_audio": "gamekitAudio",
    # ── High-Level GameKit – Logic Pillar ─────────────────────
    "unity_validate_integrity": "sceneIntegrity",
    "unity_class_dependency_graph": "classDependencyGraph",
    "unity_class_catalog": "classCatalog",
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
