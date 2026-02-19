"""Tool definitions for all 64 MCP tools.

Each entry is a ``types.Tool`` with name, description, and inputSchema.
Schema functions are imported from ``tools.schemas`` and called to produce
the JSON Schema dicts.
"""

from __future__ import annotations

import mcp.types as types

from tools.batch_sequential import TOOL as batch_sequential_tool
from tools.schemas import (
    animation2d_bundle_schema,
    animation3d_bundle_schema,
    asset_manage_schema,
    audio_source_bundle_schema,
    camera_rig_schema,
    character_controller_bundle_schema,
    class_dependency_graph_schema,
    compilation_await_schema,
    component_manage_schema,
    console_log_schema,
    event_wiring_schema,
    game_object_manage_schema,
    gamekit_actor_schema,
    gamekit_ai_schema,
    gamekit_animation_sync_schema,
    gamekit_audio_schema,
    gamekit_collectible_schema,
    gamekit_combat_schema,
    gamekit_dialogue_schema,
    gamekit_effect_schema,
    gamekit_feedback_schema,
    gamekit_health_schema,
    gamekit_interaction_schema,
    gamekit_inventory_schema,
    gamekit_machinations_schema,
    gamekit_manager_schema,
    gamekit_projectile_schema,
    gamekit_quest_schema,
    gamekit_save_schema,
    gamekit_sceneflow_schema,
    gamekit_spawner_schema,
    gamekit_status_effect_schema,
    gamekit_timer_schema,
    gamekit_trigger_zone_schema,
    gamekit_ui_binding_schema,
    gamekit_ui_command_schema,
    gamekit_ui_list_schema,
    gamekit_ui_selection_schema,
    gamekit_ui_slot_schema,
    gamekit_vfx_schema,
    gamekit_waypoint_schema,
    input_profile_schema,
    light_bundle_schema,
    material_bundle_schema,
    particle_bundle_schema,
    physics_bundle_schema,
    ping_schema,
    playmode_control_schema,
    prefab_manage_schema,
    project_settings_manage_schema,
    rect_transform_batch_schema,
    scene_manage_schema,
    scene_reference_graph_schema,
    scene_relationship_graph_schema,
    scriptable_object_manage_schema,
    sprite2d_bundle_schema,
    tilemap_bundle_schema,
    transform_batch_schema,
    ui_foundation_schema,
    ui_hierarchy_schema,
    ui_navigation_schema,
    ui_state_schema,
    uitk_asset_schema,
    uitk_document_schema,
    vector_sprite_convert_schema,
)


def get_tool_definitions() -> list[types.Tool]:
    """Return the list of all 66 MCP tool definitions."""
    return [
        # ── Utility ────────────────────────────────────────────────
        types.Tool(
            name="unity_ping",
            description=(
                "Verify Unity bridge connectivity and return heartbeat information.\n\n"
                "Use this tool to:\n"
                "- Check if Unity Editor bridge is running and responsive\n"
                "- Diagnose connection issues before running other tools\n"
                "- Get bridge version and status information\n\n"
                "Returns connection status, last heartbeat timestamp, and bridge version.\n"
                "If the bridge is not connected, other unity_* tools will fail with connection errors."
            ),
            inputSchema=ping_schema(),
        ),
        types.Tool(
            name="unity_compilation_await",
            description="Wait for Unity's script compilation to complete. Use this after creating or modifying C# scripts to ensure the code is compiled before using new types or components. Returns compilation status including any errors or warnings. Supports configurable timeout (default 60 seconds).",
            inputSchema=compilation_await_schema(),
        ),
        batch_sequential_tool,  # Sequential batch execution with resume capability
        # ── Low-Level CRUD ─────────────────────────────────────────
        types.Tool(
            name="unity_scene_crud",
            description="Unity scene management: create/load/save/delete/duplicate scenes, inspect scene hierarchy with optional component filtering. Use 'inspect' operation with 'includeHierarchy=true' to get scene context before making changes. Supports additive scene loading. For build settings operations (add/remove/reorder scenes), use unity_projectSettings_crud tool instead.",
            inputSchema=scene_manage_schema(),
        ),
        types.Tool(
            name="unity_gameobject_crud",
            description="Full GameObject lifecycle management: create (with templates like Cube/Sphere/Player/Enemy, and optional auto-attach components with properties), delete, move (reparent), rename, duplicate, update (tag/layer/active/static), inspect (with optional component details), and batch operations (findMultiple/deleteMultiple/inspectMultiple with pattern matching). Use 'components' array on create to auto-attach components: [{type: 'UnityEngine.Rigidbody2D', properties: {gravityScale: 0}}]. Supports regex pattern matching for batch operations.",
            inputSchema=game_object_manage_schema(),
        ),
        types.Tool(
            name="unity_component_crud",
            description=(
                "Complete component management with batch operations: add/remove/update/inspect components on GameObjects.\n\n"
                "**Property Changes:** Supports complex property changes including nested objects and asset references.\n\n"
                '**Unity Object References:** For properties that expect Unity Object references, use:\n'
                '- `{ "$ref": "path" }` - Object reference format (recommended)\n'
                '- `"path"` - Simple string (for UnityEngine.Object types)\n\n'
                "Path auto-detection:\n"
                '- "Assets/..." or "Packages/..." \u2192 Load from AssetDatabase\n'
                '- Other paths (e.g., "Canvas/Panel/Button") \u2192 Find scene objects by hierarchy path\n\n'
                "Scene object search features:\n"
                "- Finds inactive GameObjects (SetActive=false)\n"
                "- Searches all loaded scenes\n"
                '- Supports hierarchy paths: "Parent/Child/GrandChild"\n\n'
                "**Property Filtering:** Inspect supports fast existence checks (includeProperties=false, 10x faster) and property filtering for specific fields.\n\n"
                "**Batch Operations:** addMultiple/removeMultiple/updateMultiple/inspectMultiple support pattern matching with maxResults safety limits.\n\n"
                "Essential for configuring GameObject behavior and wiring up component references."
            ),
            inputSchema=component_manage_schema(),
        ),
        types.Tool(
            name="unity_asset_crud",
            description="Comprehensive asset file management under Assets/ folder: create (any file type including C# scripts, JSON, text), update (modify file contents), delete, rename, duplicate, inspect (view properties and content), updateImporter (modify asset import settings), and batch operations (findMultiple/deleteMultiple/inspectMultiple with pattern matching). Essential for managing scripts, textures, audio, data files, and all Unity assets. Use with unity_script_template_generate for creating properly structured C# scripts.",
            inputSchema=asset_manage_schema(),
        ),
        types.Tool(
            name="unity_scriptableObject_crud",
            description="ScriptableObject asset management: create new instances from type name, inspect/update properties, delete, duplicate, list all instances, or find by type. ScriptableObjects are Unity's data container assets perfect for game configuration (stats, settings, levels). Use 'create' to instantiate from existing type, 'update' to modify properties, 'list' to see all instances, 'findByType' to search by class name. Supports property filtering and batch operations.",
            inputSchema=scriptable_object_manage_schema(),
        ),
        types.Tool(
            name="unity_prefab_crud",
            description="Complete prefab workflow management: create prefabs from scene GameObjects, update existing prefabs, inspect prefab contents and overrides, instantiate prefabs into scenes with custom position/rotation, unpack prefab instances (completely or outermost only), apply instance overrides back to prefab, or revert instance changes. Essential for creating reusable game objects (enemies, pickups, UI elements, buildings). Use 'create' to save GameObjects as prefabs, 'instantiate' to spawn prefab instances, 'applyOverrides' to update prefab from modified instance.",
            inputSchema=prefab_manage_schema(),
        ),
        types.Tool(
            name="unity_vector_sprite_convert",
            description="Vector and primitive to sprite conversion: generate 2D sprites from primitives (square/circle/triangle/polygon with custom sides), import SVG vector files to sprites, convert existing textures to sprite assets with custom import settings, or create solid color sprites. Supports custom dimensions (width/height in pixels), RGBA colors (0-1 range), pixels per unit (sprite scale), and sprite modes (single/multiple for sprite sheets). Perfect for procedural sprite generation, prototyping without art assets, UI element creation, and SVG integration. Outputs ready-to-use sprite assets.",
            inputSchema=vector_sprite_convert_schema(),
        ),
        types.Tool(
            name="unity_projectSettings_crud",
            description="Unity Project Settings management: read/write/list settings across 8 categories (player: build settings & configurations, quality: quality levels & graphics, time: time scale & fixed timestep, physics: 3D gravity & collision settings, physics2d: 2D gravity & collision settings, audio: volume & DSP buffer, editor: serialization & asset pipeline, tagsLayers: custom tags, layers & sorting layers). Build Settings operations: addSceneToBuild (add scene to build with optional index), removeSceneFromBuild (remove by path or index), listBuildScenes (view all build scenes), reorderBuildScenes (change scene order), setBuildSceneEnabled (enable/disable scene). Use 'list' to see available properties per category, 'read' to get specific property value, 'write' to modify settings. Essential for configuring project-wide settings, 2D/3D physics parameters, quality presets, sorting layers, and build configurations.",
            inputSchema=project_settings_manage_schema(),
        ),
        # ── Mid-Level Batch ────────────────────────────────────────
        types.Tool(
            name="unity_transform_batch",
            description="Mid-level batch transform operations: arrange multiple GameObjects in patterns (arrangeCircle: circular formation, arrangeLine: linear spacing, createMenuList: vertical/horizontal menu layout from prefabs), rename objects sequentially (Item_01, Item_02) or from custom name lists, all in local or world space. Supports custom center points, radius, spacing, angles, and planes (XY/XZ/YZ). Perfect for organizing level objects, UI elements, menu items, and creating structured layouts without manual positioning.",
            inputSchema=transform_batch_schema(),
        ),
        types.Tool(
            name="unity_rectTransform_batch",
            description="Mid-level batch UI RectTransform operations: set anchors (topLeft/middleCenter/stretchAll, 16 presets), pivot points, size delta, anchored position for multiple UI elements simultaneously. Supports alignment to parent edges, horizontal/vertical distribution with custom spacing, and size matching (width/height/both) from source element. Essential for precise UI layout control, responsive design setup, and batch UI element positioning. Use for aligning panels, distributing buttons, matching UI element sizes, and creating consistent layouts.",
            inputSchema=rect_transform_batch_schema(),
        ),
        types.Tool(
            name="unity_physics_bundle",
            description="Mid-level physics setup: apply complete physics presets (dynamic: movable with physics, kinematic: movable without physics, static: immovable, character: player/NPC, platformer: 2D side-scrolling, topDown: 2D top-down, vehicle: car physics, projectile: bullets/arrows) or update individual Rigidbody/Collider properties. Automatically adds Rigidbody2D/Rigidbody + Collider (box/sphere/capsule/circle) with appropriate settings. Supports 2D and 3D physics, constraints (freeze position/rotation), collision detection modes (discrete/continuous), and physics materials. Perfect for rapid physics prototyping.",
            inputSchema=physics_bundle_schema(),
        ),
        types.Tool(
            name="unity_camera_rig",
            description="Mid-level camera rig creation: create complete camera systems with single commands (follow: smooth following camera, orbit: rotate around target, splitScreen: multiplayer viewports, fixed: static camera, dolly: cinematic rail camera). Automatically configures Camera component, target tracking, follow smoothing, orbit distance, field of view, orthographic/perspective mode, and split-screen viewports. Perfect for quickly setting up player cameras, cinematic cameras, or multiplayer camera systems without manual rigging.",
            inputSchema=camera_rig_schema(),
        ),
        types.Tool(
            name="unity_ui_foundation",
            description="Mid-level UI foundation for UGUI: create complete UI elements with single commands (Canvas with EventSystem, Panel with Image, Button with Text child, Text with styling, Image with sprite support, InputField with placeholder, ScrollView with Viewport/Content/Scrollbars). Supports LayoutGroups (Horizontal/Vertical/Grid) with full configuration, ContentSizeFitter, and UI templates (dialog/hud/menu/statusBar/inventoryGrid). Supports render modes (screenSpaceOverlay/screenSpaceCamera/worldSpace), anchor presets (topLeft/middleCenter/stretchAll, etc.), automatic sizing, and color configuration. Perfect for rapid UI prototyping and hierarchical UI design. Use for basic UI setup, then customize with unity_component_crud if needed.",
            inputSchema=ui_foundation_schema(),
        ),
        types.Tool(
            name="unity_audio_source_bundle",
            description="Mid-level audio source setup: create and configure AudioSource components with presets (music: looping background music with lower priority, sfx: one-shot sound effects with high priority, ambient: looping environmental sounds, voice: dialogue with high priority, ui: button clicks/menu sounds). Automatically configures volume, pitch, loop, playOnAwake, spatialBlend (2D/3D), min/max distance for 3D audio, priority (0-256), and audio mixer group routing. Perfect for quickly setting up game audio without manual AudioSource configuration.",
            inputSchema=audio_source_bundle_schema(),
        ),
        types.Tool(
            name="unity_input_profile",
            description="Mid-level input system setup: create PlayerInput component with New Input System, configure action maps (player: move/jump/fire, ui: navigate/submit/cancel, vehicle: accelerate/brake/steer), set up notification behaviors (sendMessages/broadcastMessages/invokeUnityEvents/invokeCSharpEvents), and define custom actions with bindings. Automatically generates or uses existing InputActions assets. Essential for setting up player input handling with Unity's modern Input System. Use presets for quick setup or 'custom' for full control.",
            inputSchema=input_profile_schema(),
        ),
        types.Tool(
            name="unity_character_controller_bundle",
            description="Mid-level CharacterController setup: apply CharacterController component with presets optimized for different character types (fps: 1.8m height for first-person, tps: 2.0m for third-person, platformer: 1.0m for platformers, child: 0.5m for small characters, large: 3.0m for large characters, narrow: thin capsule for tight spaces, custom: full manual control). Automatically configures capsule radius, height, center offset, slope limit (max climbable angle), step offset (max stair height), skin width (collision padding), and minimum move distance. Perfect for 3D character setup without manual physics configuration.",
            inputSchema=character_controller_bundle_schema(),
        ),
        types.Tool(
            name="unity_tilemap_bundle",
            description="Mid-level Tilemap management: create Tilemaps with Grid parent, set/get/clear individual tiles, fill rectangular areas, create Tile and RuleTile assets from sprites. Operations: createTilemap (auto-creates Grid), setTile/getTile/setTiles (place tiles at positions), clearTile/clearTiles/clearAllTiles (remove tiles), fillArea/boxFill (batch placement), worldToCell/cellToWorld (coordinate conversion), updateRenderer/updateCollider/addCollider (component settings), createTile/createRuleTile/inspectTile/updateTile (tile asset management). Supports Rectangle/Hexagon/Isometric layouts, sorting layers, TilemapCollider2D with CompositeCollider2D support. RuleTile requires 2D Tilemap Extras package. Essential for 2D level design, roguelikes, platformers, and procedural map generation.",
            inputSchema=tilemap_bundle_schema(),
        ),
        types.Tool(
            name="unity_ui_hierarchy",
            description=(
                "Mid-level declarative UI hierarchy management: create complex UI structures from single JSON definitions, manage visibility states.\n\n"
                "**Operations:**\n"
                "- create: Build complete UI hierarchy from declarative JSON structure (panels, buttons, text, images, inputs, scrollviews, toggles, sliders, dropdowns)\n"
                "- clone: Duplicate existing UI hierarchy with optional rename\n"
                "- inspect: Export UI hierarchy as JSON structure\n"
                "- delete: Remove UI hierarchy\n"
                "- show/hide/toggle: Control visibility using CanvasGroup (alpha, interactable, blocksRaycasts)\n\n"
                "For navigation, use unity_ui_navigation tool.\n\n"
                "**Hierarchy Structure Example:**\n"
                '```json\n'
                '{\n'
                '  "type": "panel",\n'
                '  "name": "MainMenu",\n'
                '  "children": [\n'
                '    {"type": "text", "name": "Title", "text": "Game Title", "fontSize": 48},\n'
                '    {"type": "button", "name": "StartBtn", "text": "Start Game"},\n'
                '    {"type": "button", "name": "OptionsBtn", "text": "Options"}\n'
                '  ],\n'
                '  "layout": "Vertical",\n'
                '  "spacing": 20\n'
                '}\n'
                '```\n\n'
                "**Supported Element Types:** panel, button, text, image, inputfield, scrollview, toggle, slider, dropdown\n\n"
                "Perfect for rapid UI prototyping, menu systems, dialog boxes, and complex UI structures without multiple API calls."
            ),
            inputSchema=ui_hierarchy_schema(),
        ),
        types.Tool(
            name="unity_ui_state",
            description=(
                "Mid-level UI state management: define, save, load, and transition between UI states.\n\n"
                "**Operations:**\n"
                "- defineState: Define a named UI state with element configurations (active, visible, interactable, alpha, position, size)\n"
                "- applyState: Apply a saved state to UI elements\n"
                "- saveState: Capture current UI state (including children)\n"
                "- loadState: Load state definition without applying\n"
                "- listStates: List all defined states for a root\n"
                "- deleteState: Remove a state definition\n"
                "- createStateGroup: Create a group of mutually exclusive states\n"
                "- transitionTo: Transition to a state (alias for applyState)\n"
                "- getActiveState: Get currently active state name\n\n"
                "**Use Cases:**\n"
                "- Menu screens (main menu, pause menu, settings)\n"
                "- Dialog states (open, closed, minimized)\n"
                "- HUD states (combat, exploration, cutscene)\n"
                "- Form validation states (valid, invalid, loading)"
            ),
            inputSchema=ui_state_schema(),
        ),
        types.Tool(
            name="unity_ui_navigation",
            description=(
                "Mid-level UI navigation management: configure keyboard/gamepad navigation for UI elements.\n\n"
                "**Operations:**\n"
                "- configure: Set navigation mode for a single Selectable (none/horizontal/vertical/automatic/explicit)\n"
                "- setExplicit: Set explicit up/down/left/right navigation targets\n"
                "- autoSetup: Automatically configure navigation for all Selectables under a root (vertical/horizontal/grid)\n"
                "- createGroup: Create a navigation group with isolated navigation\n"
                "- setFirstSelected: Set the first selected element for EventSystem\n"
                "- inspect: View current navigation configuration\n"
                "- reset: Reset navigation to automatic mode\n"
                "- disable: Disable navigation (mode=none)\n\n"
                "**Direction Options:**\n"
                "- vertical: Up/Down navigation in order\n"
                "- horizontal: Left/Right navigation in order\n"
                "- grid: Full 2D navigation with automatic column detection\n"
                "- both: Combine vertical and horizontal"
            ),
            inputSchema=ui_navigation_schema(),
        ),
        # ── UI Toolkit ──────────────────────────────────────────────
        types.Tool(
            name="unity_uitk_document",
            description=(
                "Mid-level UI Toolkit document management: create and manage UIDocument components in scene.\n\n"
                "**Operations:**\n"
                "- create: Create a new GameObject with UIDocument component, optional PanelSettings and UXML source\n"
                "- inspect: View UIDocument configuration and live VisualElement tree (if available)\n"
                "- update: Update UIDocument properties (sourceAsset, panelSettings, sortingOrder)\n"
                "- delete: Remove UIDocument component or delete entire GameObject\n"
                "- query: Search the live VisualElement tree by name, USS class, or element type (UQuery-style)\n\n"
                "**Key Concepts:**\n"
                "- UIDocument is the bridge between UXML assets and the scene\n"
                "- PanelSettings controls rendering (scale mode, reference resolution)\n"
                "- Live tree inspection requires play mode or active panel\n"
                "- Use with unity_uitk_asset to create UXML/USS files, then assign to UIDocument"
            ),
            inputSchema=uitk_document_schema(),
        ),
        types.Tool(
            name="unity_uitk_asset",
            description=(
                "Mid-level UI Toolkit asset management: create and manage UXML, USS, and PanelSettings assets.\n\n"
                "**Operations:**\n"
                "- createUXML: Generate UXML file from JSON element definitions\n"
                "- createUSS: Generate USS stylesheet from rule definitions\n"
                "- inspectUXML: Parse UXML file and return as JSON structure\n"
                "- inspectUSS: Parse USS file and return rules with selectors/properties\n"
                "- updateUXML: Add, remove, or replace elements in existing UXML\n"
                "- updateUSS: Add, update, or remove rules in existing USS\n"
                "- createPanelSettings: Create PanelSettings ScriptableObject asset\n"
                "- createFromTemplate: Generate UXML+USS from built-in templates (menu, dialog, hud, settings, inventory)\n"
                "- validateDependencies: Validate UXML references (Style src, Template src) and report missing files\n\n"
                "**Dependency Validation:** createUXML, updateUXML, createFromTemplate, and inspectUXML automatically include "
                "dependency validation results (dependencies, issues, isValid) in their responses. Use validateDependencies "
                "for standalone validation of any UXML file.\n\n"
                "**UXML Element Types:** VisualElement, Button, Label, TextField, Toggle, Slider, SliderInt, MinMaxSlider, "
                "Foldout, ScrollView, ListView, DropdownField, RadioButton, RadioButtonGroup, GroupBox, ProgressBar, "
                "Image, IntegerField, FloatField\n\n"
                "**Templates:** menu (title + button list), dialog (overlay + OK/Cancel), hud (HP/MP bars + action buttons), "
                "settings (toggles/sliders + Apply/Back), inventory (grid of slots)\n\n"
                "UI Toolkit (UIElements) is Unity's modern UI system using Flexbox layout, UXML markup, and USS styling. "
                "Use this for runtime UI as an alternative to uGUI (Canvas/RectTransform). "
                "Pair with unity_uitk_document to assign generated assets to scene objects."
            ),
            inputSchema=uitk_asset_schema(),
        ),
        # ── Visual ─────────────────────────────────────────────────
        types.Tool(
            name="unity_sprite2d_bundle",
            description="Mid-level 2D sprite management: create/update SpriteRenderer GameObjects, batch update sprites, set sorting layers and colors, slice sprite sheets into multiple sprites, create SpriteAtlas assets. Operations: createSprite (new GameObject with SpriteRenderer), updateSprite (modify sprite/color/flip/sortingLayer), inspect (view sprite properties), updateMultiple/setSortingLayer/setColor (batch operations with pattern matching), sliceSpriteSheet (grid/automatic slicing), createSpriteAtlas (pack sprites). Perfect for 2D game sprite setup, sprite sheet management, and batch sprite configuration.",
            inputSchema=sprite2d_bundle_schema(),
        ),
        types.Tool(
            name="unity_animation2d_bundle",
            description="Mid-level 2D animation setup: manage Animator components, create AnimatorControllers, add states and transitions, create AnimationClips from sprite sequences. Operations: setupAnimator/updateAnimator/inspectAnimator (Animator component), createController/addState/addTransition/addParameter/inspectController (AnimatorController), createClipFromSprites/updateClip/inspectClip (AnimationClip). Supports transition conditions (If/Greater/Less), animation parameters (Bool/Float/Int/Trigger), and sprite-based animation creation. Essential for 2D character animation, state machines, and procedural animation setup.",
            inputSchema=animation2d_bundle_schema(),
        ),
        types.Tool(
            name="unity_animation3d_bundle",
            description=(
                "Create and configure 3D character animations.\n\n"
                "**Operations:**\n"
                "- setupAnimator: Setup Animator component on GameObject\n"
                "- createController: Create AnimatorController with parameters/states/transitions\n"
                "- addState: Add animation state\n"
                "- addTransition: Add state transition with conditions\n"
                "- setParameter: Add/update animator parameter\n"
                "- addBlendTree: Create BlendTree for smooth animation blending\n"
                "- createAvatarMask: Create AvatarMask for partial body animation\n"
                "- inspect: Get controller structure\n"
                "- delete: Delete controller or state\n"
                "- listParameters: List all parameters\n"
                "- listStates: List all states in layer"
            ),
            inputSchema=animation3d_bundle_schema(),
        ),
        types.Tool(
            name="unity_material_bundle",
            description=(
                "Create and configure materials with presets.\n\n"
                "**Operations:**\n"
                "- create: Create new material with optional preset\n"
                "- update: Update material properties (color, metallic, smoothness)\n"
                "- setTexture: Set texture with tiling/offset\n"
                "- setColor: Set color property\n"
                "- applyPreset: Apply material preset\n"
                "- inspect: Get material properties\n"
                "- applyToObjects: Apply material to multiple GameObjects\n"
                "- delete: Delete material asset\n"
                "- duplicate: Duplicate material\n"
                "- listPresets: List available presets\n\n"
                "**Presets:** unlit, lit, transparent, cutout, fade, sprite, ui, emissive, metallic, glass\n\n"
                "Supports Standard, URP, and HDRP render pipelines."
            ),
            inputSchema=material_bundle_schema(),
        ),
        types.Tool(
            name="unity_light_bundle",
            description=(
                "Create and configure lights with presets.\n\n"
                "**Operations:**\n"
                "- create: Create light with type and preset\n"
                "- update: Update light properties\n"
                "- inspect: Get light properties\n"
                "- delete: Delete light GameObject\n"
                "- applyPreset: Apply light preset\n"
                "- createLightingSetup: Create complete lighting setup\n"
                "- listPresets: List available presets\n\n"
                "**Light Presets:** daylight, moonlight, warm, cool, spotlight, candle, neon\n\n"
                "**Setup Presets:** daylight (sun+ambient), nighttime (moon), indoor (points), dramatic (contrast), studio (3-point), sunset (warm)"
            ),
            inputSchema=light_bundle_schema(),
        ),
        types.Tool(
            name="unity_particle_bundle",
            description=(
                "Create and configure particle systems with presets.\n\n"
                "**Operations:**\n"
                "- create: Create particle system with preset\n"
                "- update: Update particle properties\n"
                "- applyPreset: Apply particle preset\n"
                "- play: Start particle playback\n"
                "- stop: Stop particle playback\n"
                "- pause: Pause particle playback\n"
                "- inspect: Get particle system properties\n"
                "- delete: Delete particle system GameObject\n"
                "- duplicate: Duplicate particle system\n"
                "- listPresets: List available presets\n\n"
                "**Presets:** explosion, fire, smoke, sparkle, rain, snow, dust, trail, hit, heal, magic, leaves"
            ),
            inputSchema=particle_bundle_schema(),
        ),
        types.Tool(
            name="unity_event_wiring",
            description=(
                "Wire UnityEvents dynamically (Button.onClick, Slider.onValueChanged, etc.).\n\n"
                "**Operations:**\n"
                "- wire: Add listener to UnityEvent\n"
                "- unwire: Remove listener(s) from UnityEvent\n"
                "- inspect: View event listeners\n"
                "- listEvents: List UnityEvent fields on component\n"
                "- clearEvent: Clear all listeners from event\n"
                "- wireMultiple: Wire multiple events at once\n\n"
                "**Argument Modes:** Void, Int, Float, String, Bool, Object\n\n"
                "Supports: Button, Toggle, Slider, InputField, Dropdown, ScrollRect, and custom UnityEvents."
            ),
            inputSchema=event_wiring_schema(),
        ),
        # ── GameKit Core ───────────────────────────────────────────
        types.Tool(
            name="unity_gamekit_actor",
            description="High-level GameKit Actor: create game actors with controller-behavior separation. Choose from 8 behavior profiles (2dLinear/2dPhysics/2dTileGrid/graphNode/splineMovement/3dCharacterController/3dPhysics/3dNavMesh) and 4 control modes (directController for player input via New Input System or legacy, aiAutonomous for AI patrol/follow/wander, uiCommand for UI button control, scriptTriggerOnly for event-driven). Actors relay input to behaviors via UnityEvents (OnMoveInput/OnJumpInput/OnActionInput/OnLookInput). Perfect for players, NPCs, enemies, and interactive characters.",
            inputSchema=gamekit_actor_schema(),
        ),
        types.Tool(
            name="unity_gamekit_manager",
            description="High-level GameKit Manager: create centralized game system managers for turn-based games (TurnManager), real-time coordination (RealtimeManager), resource/economy management (ResourceManager with Machinations support), global events (EventHub), or finite state machines (StateManager). Supports persistence (DontDestroyOnLoad) and integration with GameKitUICommand for UI control. Essential for managing game-wide state, resources (health/mana/gold), turn phases, and game flow.",
            inputSchema=gamekit_manager_schema(),
        ),
        types.Tool(
            name="unity_gamekit_interaction",
            description="High-level GameKit Interaction: create trigger-based interactions with declarative actions. Choose from 5 trigger types (collision/trigger/raycast/proximity/input) and 5 action types (spawnPrefab/destroyObject/playSound/sendMessage/changeScene). Add conditions (tag/layer/distance/custom) for filtering. Supports both 2D (BoxCollider2D, CircleCollider2D, CapsuleCollider2D, PolygonCollider2D) and 3D (BoxCollider, SphereCollider, CapsuleCollider, MeshCollider) colliders via is2D parameter. Perfect for collectibles, doors, switches, treasure chests, and interactive objects. No scripting required - define complete interactions declaratively. **When to use:** For custom script actions (sendMessage, spawnPrefab). For built-in zone effects (damage, heal, checkpoint, teleport), use unity_gamekit_trigger_zone instead.",
            inputSchema=gamekit_interaction_schema(),
        ),
        types.Tool(
            name="unity_gamekit_ui_command",
            description=(
                "High-level GameKit UI Command: create command panels using UI Toolkit (UXML/USS) with buttons that send commands to GameKitActors or GameKitManagers.\n\n"
                "**Operations:**\n"
                "- createCommandPanel: Generate UXML/USS and UIDocument with command buttons\n"
                "- addCommand: Add a command button to existing panel (updates UXML)\n"
                "- inspect: View panel configuration and UXML/USS paths\n"
                "- delete: Remove command panel and generated UXML/USS files\n\n"
                "**Actor Commands:** move, jump, action, look, custom\n"
                "**Manager Commands:** addResource, setResource, consumeResource, changeState, nextTurn, triggerScene"
            ),
            inputSchema=gamekit_ui_command_schema(),
        ),
        types.Tool(
            name="unity_gamekit_machinations",
            description=(
                "High-level GameKit Machinations: create and manage Machinations diagram assets for economic systems.\n\n"
                "**Operations:**\n"
                "- create: Create a new Machinations asset (ScriptableObject)\n"
                "- update: Update asset properties (pools, flows, converters, triggers)\n"
                "- inspect: View diagram configuration\n"
                "- delete: Delete Machinations asset\n"
                "- apply: Apply diagram to a ResourceManager\n"
                "- export: Export ResourceManager state to asset\n\n"
                "**Components:**\n"
                "- **Resource Pools:** Define resources with initial/min/max values (health, mana, gold)\n"
                "- **Flows:** Automatic generation/consumption over time (mana regen, hunger drain)\n"
                "- **Converters:** Transform resources (gold \u2192 health potion)\n"
                "- **Triggers:** Threshold events (HP\u22640 \u2192 death event)"
            ),
            inputSchema=gamekit_machinations_schema(),
        ),
        types.Tool(
            name="unity_gamekit_sceneflow",
            description=(
                "High-level GameKit SceneFlow: manage scene transitions with granular control.\n\n"
                "**Operations:**\n"
                "- create: Initialize a new SceneFlow\n"
                "- inspect: View flow configuration\n"
                "- delete: Delete SceneFlow\n"
                "- transition: Execute a transition at runtime\n"
                "- addScene: Add a scene to the flow\n"
                "- removeScene: Remove a scene\n"
                "- updateScene: Update scene settings\n"
                "- addTransition: Add transition between scenes\n"
                "- removeTransition: Remove a transition\n"
                "- addSharedScene: Add shared scene (UI, Audio overlay)\n"
                "- removeSharedScene: Remove shared scene\n\n"
                "**Load Modes:**\n"
                "- single: Unload all scenes, load new one\n"
                "- additive: Load on top of existing scenes\n\n"
                "Perfect for level progression, menu systems, and complex scene workflows."
            ),
            inputSchema=gamekit_sceneflow_schema(),
        ),
        # ── GameKit Systems ────────────────────────────────────────
        types.Tool(
            name="unity_gamekit_health",
            description=(
                "High-level GameKit Health: create and manage health/damage systems for game entities.\n\n"
                "**Operations:**\n"
                "- create: Add GameKitHealth component with configurable settings\n"
                "- update: Modify health parameters (maxHealth, invincibilityDuration, deathBehavior)\n"
                "- inspect: View health status and configuration\n"
                "- delete: Remove GameKitHealth component\n"
                "- applyDamage: Deal damage to the entity\n"
                "- heal: Restore health\n"
                "- kill: Instantly kill the entity\n"
                "- respawn: Respawn at configured position\n"
                "- setInvincible: Enable/disable invincibility\n"
                "- findByHealthId: Find health component by ID\n\n"
                "**Death Behaviors:** Destroy, Disable, Respawn, EventOnly\n\n"
                "**Features:**\n"
                "- Invincibility frames after damage\n"
                "- UnityEvents: OnDamage, OnHeal, OnDeath, OnRespawn, OnInvincibilityStart/End\n"
                "- Auto-respawn with configurable delay"
            ),
            inputSchema=gamekit_health_schema(),
        ),
        types.Tool(
            name="unity_gamekit_spawner",
            description=(
                "High-level GameKit Spawner: create spawn systems for enemies, items, and objects.\n\n"
                "**Operations:**\n"
                "- create: Add GameKitSpawner component\n"
                "- update: Modify spawner settings\n"
                "- inspect: View spawner status\n"
                "- delete: Remove spawner\n"
                "- start/stop/reset: Control spawning\n"
                "- spawnOne: Spawn a single instance\n"
                "- spawnBurst: Spawn multiple at once\n"
                "- despawnAll: Despawn all spawned objects\n"
                "- addSpawnPoint: Add spawn position\n"
                "- addWave: Add wave configuration\n"
                "- findBySpawnerId: Find spawner by ID\n\n"
                "**Spawn Modes:** Interval, Wave, Burst, Manual\n\n"
                "**Features:**\n"
                "- Object pooling support\n"
                "- Multiple spawn points\n"
                "- Wave system with enemy counts and delays\n"
                "- UnityEvents: OnSpawn, OnDespawn, OnWaveStart/Complete, OnAllWavesComplete"
            ),
            inputSchema=gamekit_spawner_schema(),
        ),
        types.Tool(
            name="unity_gamekit_timer",
            description=(
                "High-level GameKit Timer: create timers and cooldown systems.\n\n"
                "**Operations:**\n"
                "- createTimer/updateTimer/inspectTimer/deleteTimer: Timer CRUD\n"
                "- startTimer/stopTimer/pauseTimer/resumeTimer/resetTimer: Timer control\n"
                "- createCooldown/inspectCooldown/triggerCooldown/resetCooldown/deleteCooldown: Cooldown management\n"
                "- findByTimerId: Find timer by ID\n\n"
                "**Timer Features:**\n"
                "- Loop mode for repeating timers\n"
                "- Unscaled time for pause-immune timers\n"
                "- UnityEvents: OnTimerStart, OnTimerComplete, OnTimerTick\n\n"
                "**Cooldown Features:**\n"
                "- Multiple cooldowns on single GameObject\n"
                "- Ready state checking\n"
                "- Remaining time queries"
            ),
            inputSchema=gamekit_timer_schema(),
        ),
        types.Tool(
            name="unity_gamekit_ai",
            description=(
                "High-level GameKit AI: create AI behaviors for NPCs and enemies.\n\n"
                "**Operations:**\n"
                "- create/update/inspect/delete: AI component CRUD\n"
                "- setTarget/clearTarget: Target management\n"
                "- setState: Force state change\n"
                "- addPatrolPoint/clearPatrolPoints: Patrol configuration\n"
                "- findByAIId: Find AI by ID\n\n"
                "**Behavior Types:** Patrol, Chase, Flee, PatrolAndChase\n"
                "**AI States:** Idle, Patrol, Chase, Attack, Flee, Return\n"
                "**Patrol Modes:** Loop, PingPong, Random\n\n"
                "**Detection:**\n"
                "- Detection radius, field of view angle\n"
                "- Line of sight (raycast), target layer mask\n\n"
                "**When to use:** For intelligent behaviors (patrol + chase, detection, state machines). "
                "For simple path following without AI (moving platforms, rails), use unity_gamekit_waypoint instead."
            ),
            inputSchema=gamekit_ai_schema(),
        ),
        types.Tool(
            name="unity_gamekit_collectible",
            description=(
                "High-level GameKit Collectible: create collectible items for games.\n\n"
                "**Operations:**\n"
                "- create/update/inspect/delete: Collectible CRUD\n"
                "- collect: Simulate collection (editor mode)\n"
                "- respawn/reset: State management\n"
                "- findByCollectibleId: Find by ID\n\n"
                "**Collectible Types:** Coin, Health, Mana, PowerUp, Key, Ammo, Experience, Custom\n"
                "**Collection Behaviors:** Destroy, Disable, Respawn\n\n"
                "**Features:**\n"
                "- Auto-apply values to GameKitHealth/ResourceManager\n"
                "- Float animation (bobbing) and rotation animation\n"
                "- Customizable collider, tag/layer filtering"
            ),
            inputSchema=gamekit_collectible_schema(),
        ),
        types.Tool(
            name="unity_gamekit_projectile",
            description=(
                "High-level GameKit Projectile: create projectiles for games.\n\n"
                "**Operations:**\n"
                "- create/update/inspect/delete: Projectile CRUD\n"
                "- launch: Set launch direction (play mode)\n"
                "- setHomingTarget: Set homing target\n"
                "- destroy: Destroy projectile\n"
                "- findByProjectileId: Find by ID\n\n"
                "**Movement Types:** Transform, Rigidbody, Rigidbody2D\n\n"
                "**Features:**\n"
                "- Homing missiles (target tracking)\n"
                "- Bouncing and piercing projectiles\n"
                "- Gravity support\n"
                "- Damage on hit (GameKitHealth integration)\n"
                "- Target tag/layer filtering"
            ),
            inputSchema=gamekit_projectile_schema(),
        ),
        types.Tool(
            name="unity_gamekit_waypoint",
            description=(
                "High-level GameKit Waypoint: create path followers for NPCs and platforms.\n\n"
                "**Operations:**\n"
                "- create/update/inspect/delete: Waypoint CRUD\n"
                "- addWaypoint/removeWaypoint/clearWaypoints: Path editing\n"
                "- startPath/stopPath/pausePath/resumePath/resetPath: Path control\n"
                "- goToWaypoint: Jump to waypoint index\n"
                "- findByWaypointId: Find by ID\n\n"
                "**Path Modes:** Once, Loop, PingPong\n"
                "**Movement Types:** Transform, Rigidbody, Rigidbody2D\n"
                "**Rotation Modes:** None, LookAtTarget, AlignToPath\n\n"
                "**When to use:** For simple path following (moving platforms, rails, fixed NPC routes). "
                "For AI behaviors with detection and chase, use unity_gamekit_ai instead."
            ),
            inputSchema=gamekit_waypoint_schema(),
        ),
        types.Tool(
            name="unity_gamekit_trigger_zone",
            description=(
                "High-level GameKit TriggerZone: create trigger zones for game mechanics.\n\n"
                "**Operations:**\n"
                "- create/update/inspect/delete: Zone CRUD\n"
                "- activate/deactivate: Enable/disable zone\n"
                "- reset: Reset trigger state\n"
                "- setTeleportDestination: Set teleport target\n"
                "- findByZoneId: Find by ID\n\n"
                "**Zone Types:** Generic, Checkpoint, DamageZone, HealZone, Teleport, SpeedBoost, SlowDown, KillZone, SafeZone, Trigger\n"
                "**Trigger Modes:** Once, OncePerEntity, Repeat, WhileInside\n\n"
                "**Features:**\n"
                "- 2D/3D collider support, tag/layer filtering\n"
                "- Cooldown system, effect intervals, editor gizmo visualization\n\n"
                "**When to use:** For built-in zone effects (damage, heal, checkpoint, teleport, speed zones). "
                "For custom script actions (sendMessage, spawnPrefab), use unity_gamekit_interaction instead."
            ),
            inputSchema=gamekit_trigger_zone_schema(),
        ),
        types.Tool(
            name="unity_gamekit_animation_sync",
            description=(
                "High-level GameKit Animation Sync: declarative animation synchronization with game state.\n\n"
                "**Operations:**\n"
                "- create/update/inspect/delete: AnimationSync CRUD\n"
                "- addSyncRule/removeSyncRule: Parameter sync rule management\n"
                "- addTriggerRule/removeTriggerRule: Trigger rule management\n"
                "- fireTrigger: Manually fire an animator trigger\n"
                "- setParameter: Set an animator parameter value\n"
                "- findBySyncId: Find animation sync by ID\n\n"
                "**Sync Source Types:** rigidbody3d/rigidbody2d (velocity), transform (position/rotation/scale), health (GameKitHealth), custom\n\n"
                "**Trigger Event Sources:** health (damage/heal/death), input (action), manual (API call)"
            ),
            inputSchema=gamekit_animation_sync_schema(),
        ),
        types.Tool(
            name="unity_gamekit_effect",
            description=(
                "High-level GameKit Effect: composite effect system for particles, sound, camera shake, and screen flash.\n\n"
                "**Operations:**\n"
                "- create/update/inspect/delete: Effect asset CRUD\n"
                "- addComponent/removeComponent/clearComponents: Effect component management\n"
                "- play/playAtPosition/playAtTransform: Play effect (runtime)\n"
                "- shakeCamera/flashScreen/setTimeScale: Direct effects (runtime)\n"
                "- createManager/registerEffect/unregisterEffect: Manager management\n"
                "- findByEffectId/listEffects: Lookup\n\n"
                "**Effect Component Types:** particle, sound, cameraShake, screenFlash, timeScale"
            ),
            inputSchema=gamekit_effect_schema(),
        ),
        types.Tool(
            name="unity_gamekit_save",
            description=(
                "High-level GameKit Save: declarative save/load system with profiles and slots.\n\n"
                "**Operations:**\n"
                "- createProfile/updateProfile/inspectProfile/deleteProfile: Profile CRUD\n"
                "- addTarget/removeTarget/clearTargets: Save target management\n"
                "- save/load: Execute save/load\n"
                "- listSlots/deleteSlot: Slot management\n"
                "- createManager/inspectManager/deleteManager: Manager CRUD\n"
                "- findByProfileId: Lookup\n\n"
                "**Save Target Types:** transform, component, resourceManager, health, sceneFlow, inventory, playerPrefs"
            ),
            inputSchema=gamekit_save_schema(),
        ),
        types.Tool(
            name="unity_gamekit_inventory",
            description=(
                "High-level GameKit Inventory: complete inventory system with items, stacking, and equipment.\n\n"
                "**Operations:**\n"
                "- create/update/inspect/delete: Inventory CRUD\n"
                "- defineItem/updateItem/inspectItem/deleteItem: Item asset CRUD\n"
                "- addItem/removeItem/useItem: Item management\n"
                "- equip/unequip/getEquipped: Equipment management\n"
                "- clear/sort: Inventory utilities\n"
                "- findByInventoryId/findByItemId: Lookup\n\n"
                "**Item Categories:** weapon, armor, consumable, material, key, quest, misc\n"
                "**Equipment Slots:** mainHand, offHand, head, body, hands, feet, accessory1, accessory2"
            ),
            inputSchema=gamekit_inventory_schema(),
        ),
        types.Tool(
            name="unity_gamekit_dialogue",
            description=(
                "High-level GameKit Dialogue: declarative dialogue system for NPC conversations with choices and conditions.\n\n"
                "**Operations:**\n"
                "- createDialogue/updateDialogue/inspectDialogue/deleteDialogue: Dialogue asset CRUD\n"
                "- addNode/updateNode/removeNode: Dialogue node management\n"
                "- addChoice/updateChoice/removeChoice: Choice management\n"
                "- startDialogue/selectChoice/advanceDialogue/endDialogue: Runtime control\n"
                "- createManager/inspectManager/deleteManager: Manager CRUD\n"
                "- findByDialogueId: Lookup\n\n"
                "**Node Types:** dialogue, choice, branch, action, exit\n"
                "**Condition Types:** quest, resource, inventory, variable, health, custom"
            ),
            inputSchema=gamekit_dialogue_schema(),
        ),
        types.Tool(
            name="unity_gamekit_quest",
            description=(
                "High-level GameKit Quest: complete quest system with objectives, prerequisites, and rewards.\n\n"
                "**Operations:**\n"
                "- createQuest/updateQuest/inspectQuest/deleteQuest: Quest asset CRUD\n"
                "- addObjective/updateObjective/removeObjective: Objective management\n"
                "- addPrerequisite/removePrerequisite: Prerequisite management\n"
                "- addReward/removeReward: Reward management\n"
                "- startQuest/completeQuest/failQuest/abandonQuest: Quest state control\n"
                "- updateProgress: Update objective progress\n"
                "- listQuests: List quests by filter\n"
                "- createManager/inspectManager/deleteManager: Manager CRUD\n"
                "- findByQuestId: Lookup\n\n"
                "**Quest Categories:** main, side, daily, weekly, event, tutorial, hidden, custom\n"
                "**Objective Types:** kill, collect, talk, location, interact, escort, defend, deliver, explore, craft, custom\n"
                "**Reward Types:** resource, item, experience, reputation, unlock, dialogue, custom"
            ),
            inputSchema=gamekit_quest_schema(),
        ),
        types.Tool(
            name="unity_gamekit_status_effect",
            description=(
                "High-level GameKit Status Effect: buff/debuff system with DoT, stat modifiers, and stacking.\n\n"
                "**Operations:**\n"
                "- defineEffect/updateEffect/inspectEffect/deleteEffect: Effect asset CRUD\n"
                "- addModifier/updateModifier/removeModifier/clearModifiers: Modifier management\n"
                "- create/update/inspect/delete: StatusEffectReceiver CRUD\n"
                "- applyEffect/removeEffect/clearEffects: Active effect management\n"
                "- getActiveEffects/getStatModifier: Queries\n"
                "- findByEffectId/findByReceiverId/listEffects: Lookup\n\n"
                "**Effect Types:** buff, debuff, neutral\n"
                "**Categories:** generic, poison, burn, freeze, stun, slow, haste, shield, regeneration, invincibility, weakness, strength, custom\n"
                "**Modifier Types:** statModifier, damageOverTime, healOverTime, stun, silence, invincible\n"
                "**Stack Behaviors:** refreshDuration, addDuration, independent, increaseStacks"
            ),
            inputSchema=gamekit_status_effect_schema(),
        ),
        # ── GameKit Pillar (3-Pillar Architecture v2.7.0) ──────────
        types.Tool(
            name="unity_gamekit_ui_binding",
            description=(
                "High-level GameKit UI Binding: declarative UI data binding system using UI Toolkit.\n\n"
                "**Operations:**\n"
                "- create/update/inspect/delete: Binding CRUD\n"
                "- setRange: Set min/max value range\n"
                "- refresh: Force refresh from source\n"
                "- findByBindingId: Find binding by ID\n\n"
                "**Source Types:** health (GameKitHealth), economy (GameKitManager resource), timer (GameKitTimer), custom\n"
                "**Value Formats:** raw, percent, ratio, formatted\n\n"
                "**Auto-detected UI Elements:** ProgressBar, Label, Slider, SliderInt, TextElement (queried from UIDocument)"
            ),
            inputSchema=gamekit_ui_binding_schema(),
        ),
        types.Tool(
            name="unity_gamekit_ui_list",
            description=(
                "High-level GameKit UI List: dynamic list/grid using UI Toolkit (UXML/USS) for displaying collections.\n\n"
                "**Operations:**\n"
                "- create/update/inspect/delete: List CRUD (generates UIDocument with ScrollView UXML/USS)\n"
                "- setItems/addItem/removeItem/clear: Item management (items rendered as VisualElements)\n"
                "- selectItem/deselectItem/clearSelection: Selection with USS class toggling\n"
                "- refreshFromSource: Refresh from data source\n"
                "- findByListId: Lookup\n\n"
                "**Layout Types:** vertical, horizontal, grid\n"
                "**Data Sources:** custom, inventory, equipment"
            ),
            inputSchema=gamekit_ui_list_schema(),
        ),
        types.Tool(
            name="unity_gamekit_ui_slot",
            description=(
                "High-level GameKit UI Slot: slot-based UI using UI Toolkit (UXML/USS) for equipment, quickslots, and item management.\n\n"
                "**Slot Operations:** create/update/inspect/delete, setItem/clearSlot/setHighlight\n"
                "**Slot Bar Operations:** createSlotBar/updateSlotBar/inspectSlotBar/deleteSlotBar, useSlot/refreshFromInventory\n"
                "**Find Operations:** findBySlotId/findByBarId\n\n"
                "**Slot Types:** storage, equipment, quickslot, trash\n\n"
                "**Features:** Click handling via UITK events, category filtering, inventory binding, USS class toggling for visual states"
            ),
            inputSchema=gamekit_ui_slot_schema(),
        ),
        types.Tool(
            name="unity_gamekit_ui_selection",
            description=(
                "High-level GameKit UI Selection: selection group management using UI Toolkit (UXML/USS) for toggles, radios, checkboxes, and tabs.\n\n"
                "**Operations:**\n"
                "- create/update/inspect/delete: Selection group CRUD (generates UIDocument with UXML/USS)\n"
                "- setItems/addItem/removeItem/clear: Item management (items rendered as VisualElements)\n"
                "- selectItem/selectItemById/deselectItem/clearSelection: Selection control with USS class toggling\n"
                "- setSelectionActions/setItemEnabled: Configuration\n"
                "- findBySelectionId: Lookup\n\n"
                "**Selection Types:** radio, toggle, checkbox, tab\n\n"
                "**Features:** USS-based visual state management, associated panels for tab mode, default selection support"
            ),
            inputSchema=gamekit_ui_selection_schema(),
        ),
        types.Tool(
            name="unity_gamekit_combat",
            description=(
                "High-level GameKit Combat: unified damage calculation and attack system.\n\n"
                "**Operations:**\n"
                "- create/update/inspect/delete: Combat component CRUD\n"
                "- addTargetTag/removeTargetTag: Target filtering\n"
                "- resetCooldown: Reset attack cooldown\n"
                "- findByCombatId: Find combat by ID\n\n"
                "**Attack Types:** melee, ranged, aoe, projectile\n"
                "**Hitbox Shapes:** sphere, box, capsule, cone\n\n"
                "**Features:** Damage variance, critical hit system, multi-target support, cooldown management, "
                "integration with GameKitHealth, effect triggers on hit/crit"
            ),
            inputSchema=gamekit_combat_schema(),
        ),
        types.Tool(
            name="unity_gamekit_feedback",
            description=(
                "High-level GameKit Feedback: game feel effects system.\n\n"
                "**Operations:**\n"
                "- create/update/inspect/delete: Feedback CRUD\n"
                "- addComponent/clearComponents: Effect component management\n"
                "- setIntensity: Set global intensity multiplier\n"
                "- findByFeedbackId: Find feedback by ID\n\n"
                "**Component Types:** hitstop, screenShake, flash, colorFlash, scale, position, rotation, sound, particle, haptic"
            ),
            inputSchema=gamekit_feedback_schema(),
        ),
        types.Tool(
            name="unity_gamekit_vfx",
            description=(
                "High-level GameKit VFX: visual effects wrapper with pooling.\n\n"
                "**Operations:**\n"
                "- create/update/inspect/delete: VFX CRUD\n"
                "- setMultipliers: Set duration/size/emission multipliers\n"
                "- setColor/setLoop: Configuration\n"
                "- findByVFXId: Find VFX by ID\n\n"
                "**Features:** Object pooling, duration/size/emission multipliers, auto-play on enable, attach to parent transform, registry-based lookup"
            ),
            inputSchema=gamekit_vfx_schema(),
        ),
        types.Tool(
            name="unity_gamekit_audio",
            description=(
                "High-level GameKit Audio: sound effect and music wrapper.\n\n"
                "**Operations:**\n"
                "- create/update/inspect/delete: Audio CRUD\n"
                "- setVolume/setPitch/setLoop/setClip: Configuration\n"
                "- findByAudioId: Find audio by ID\n\n"
                "**Audio Types:** sfx, music, ambient, voice, ui\n\n"
                "**Features:** Pitch variation, fade in/out, 2D/3D spatial blend, registry-based lookup, cross-fade support"
            ),
            inputSchema=gamekit_audio_schema(),
        ),
        # ── Development Cycle & Visual Tools ───────────────────────
        types.Tool(
            name="unity_playmode_control",
            description=(
                "Control Unity Editor play mode for testing games.\n\n"
                "**Operations:**\n"
                "- play: Start play mode\n"
                "- pause: Pause play mode\n"
                "- unpause: Resume paused play mode\n"
                "- stop: Stop play mode\n"
                "- step: Step one frame (while paused)\n"
                "- getState: Get current play mode state (stopped/playing/paused)\n\n"
                "Essential for LLMs to execute and test games autonomously."
            ),
            inputSchema=playmode_control_schema(),
        ),
        types.Tool(
            name="unity_console_log",
            description=(
                "Retrieve Unity Console logs for debugging.\n\n"
                "**Operations:**\n"
                "- getRecent: Get recent N logs (default: 50)\n"
                "- getErrors: Get error logs only\n"
                "- getWarnings: Get warning logs only\n"
                "- getLogs: Get normal Debug.Log messages only\n"
                "- clear: Clear console\n"
                "- getCompilationErrors: Get detailed compilation errors with file/line info\n"
                "- getSummary: Get log count summary (errors/warnings/logs)\n\n"
                "Essential for LLMs to debug and fix issues autonomously."
            ),
            inputSchema=console_log_schema(),
        ),
        # ── Graph Analysis ─────────────────────────────────────────
        types.Tool(
            name="unity_class_dependency_graph",
            description=(
                "Analyze class/script dependency relationships in the Unity project.\n\n"
                "**Operations:**\n"
                "- analyzeClass: Analyze a single class and its dependencies\n"
                "- analyzeAssembly: Analyze all classes in an assembly\n"
                "- analyzeNamespace: Analyze all classes in a namespace\n"
                "- findDependents: Find classes that depend on the specified class\n"
                "- findDependencies: Find classes that the specified class depends on\n\n"
                "**Dependency Types:** field_reference, inherits, implements, requires_component\n"
                "**Output Formats:** json, dot, mermaid, summary"
            ),
            inputSchema=class_dependency_graph_schema(),
        ),
        types.Tool(
            name="unity_scene_reference_graph",
            description=(
                "Analyze object reference relationships within a Unity scene.\n\n"
                "**Operations:**\n"
                "- analyzeScene: Analyze all references in the current or specified scene\n"
                "- analyzeObject: Analyze references for a specific GameObject\n"
                "- findReferencesTo: Find all objects that reference the specified object\n"
                "- findReferencesFrom: Find all objects that the specified object references\n"
                "- findOrphans: Find objects not referenced by anything\n\n"
                "**Reference Types:** component_reference, unity_event, hierarchy_child, prefab_source\n"
                "**Output Formats:** json, dot, mermaid, summary"
            ),
            inputSchema=scene_reference_graph_schema(),
        ),
        types.Tool(
            name="unity_scene_relationship_graph",
            description=(
                "Analyze relationships between Unity scenes (transitions, dependencies).\n\n"
                "**Operations:**\n"
                "- analyzeAll: Analyze all scene relationships in the project\n"
                "- analyzeScene: Analyze transitions from a specific scene\n"
                "- findTransitionsTo: Find scenes that can transition to the specified scene\n"
                "- findTransitionsFrom: Find scenes the specified scene can transition to\n"
                "- validateBuildSettings: Validate that referenced scenes are in Build Settings\n\n"
                "**Transition Types:** scene_load, scene_load_additive, sceneflow_transition\n"
                "**Output Formats:** json, dot, mermaid, summary"
            ),
            inputSchema=scene_relationship_graph_schema(),
        ),
    ]
