"""Tool definitions for all 41 MCP tools (40 registry + 1 batch_sequential).

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
    ui_convert_schema,
    ui_foundation_schema,
    ui_navigation_schema,
    ui_state_schema,
    uitk_asset_schema,
    uitk_document_schema,
    validate_integrity_schema,
    vector_sprite_convert_schema,
)


def get_tool_definitions() -> list[types.Tool]:
    """Return the list of all 42 MCP tool definitions."""
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
            description="Wait for Unity's script compilation to complete. Use this after creating or modifying C# scripts to ensure the code is compiled before using new types or components. Returns compilation status including any errors or warnings. Supports configurable timeout (default 60 seconds). Auto-reconnects if bridge is disconnected during compilation (no need for separate ping/sleep).",
            inputSchema=compilation_await_schema(),
        ),
        batch_sequential_tool,  # Sequential batch execution with resume capability
        types.Tool(
            name="unity_event_wiring",
            description=(
                "Wire UnityEvents dynamically (Button.onClick, Slider.onValueChanged, etc.).\n\n"
                "**Operations:**\n"
                "- wire: Add listener to UnityEvent\n"
                "- inspect: View event listeners\n"
                "- listEvents: List UnityEvent fields on component\n"
                "- wireMultiple: Wire multiple events at once\n\n"
                "**Argument Modes:** Void, Int, Float, String, Bool, Object\n\n"
                "Supports: Button, Toggle, Slider, InputField, Dropdown, ScrollRect, and custom UnityEvents."
            ),
            inputSchema=event_wiring_schema(),
        ),
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
                "- getState: Get current play mode state (stopped/playing/paused)\n"
                "- captureState: Capture runtime state of specified GameObjects (position, rotation, components). "
                "With includeSerializedFields=true, reads private [SerializeField] fields and public properties "
                "of custom MonoBehaviours via reflection (e.g., _currentWave, _score, _currentLives). Requires play mode.\n"
                "- waitForScene: Check if a scene is loaded by name/path. Poll until loaded=true for scene transitions.\n"
                "- validateState: Validate runtime manager state — check MonoBehaviours exist and collections meet minimum counts (requires play mode).\n\n"
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
                "- getSummary: Get log count summary (errors/warnings/logs)\n"
                "- snapshot: Take a snapshot of current logs for later diff comparison\n"
                "- diff: Compare current logs against last snapshot, returning only new entries. Supports severity/keyword filters.\n"
                "- filter: Filter all logs by severity array and/or keyword regex pattern\n\n"
                "**Snapshot workflow:** snapshot → (make changes / play test) → diff → fix issues → repeat.\n"
                "**Domain reload safe:** Snapshot survives play mode transitions (persisted to EditorPrefs).\n\n"
                "Essential for LLMs to debug and fix issues autonomously."
            ),
            inputSchema=console_log_schema(),
        ),
        # ── Low-Level CRUD ─────────────────────────────────────────
        types.Tool(
            name="unity_scene_crud",
            description="Unity scene management: create/load/save/delete/duplicate scenes, inspect scene hierarchy with optional component filtering. Use 'inspect' operation with 'includeHierarchy=true' to get scene context before making changes. Supports additive scene loading. For build settings operations (add/remove/reorder scenes), use unity_projectSettings_crud tool instead.",
            inputSchema=scene_manage_schema(),
        ),
        types.Tool(
            name="unity_gameobject_crud",
            description=(
                "Full GameObject lifecycle management: create (with templates and auto-attach components), delete, move, rename, "
                "duplicate, update (tag/layer/active/static), inspect, and batch operations (findMultiple/deleteMultiple/inspectMultiple).\n\n"
                "**Batch matchMode**: 'exact' (name must equal pattern), 'contains' (default, name-contains), "
                "'wildcard' (supports * and ?), 'regex' (full regex). Use 'exact' to avoid accidental matches "
                "(e.g., matchMode='exact', pattern='Boss' won't match 'BossHPBar').\n\n"
                "Use 'components' array on create to auto-attach: [{type: 'Rigidbody2D', properties: {gravityScale: 0}}]."
            ),
            inputSchema=game_object_manage_schema(),
        ),
        types.Tool(
            name="unity_component_crud",
            description=(
                "Complete component management with batch operations: add/remove/update/inspect components on GameObjects.\n\n"
                "**Wildcard:** Use componentType='*' to inspect all components or remove all (except Transform).\n\n"
                "**Property Changes:** Supports complex property changes including nested objects and asset references.\n\n"
                "**Unity Object References:** For properties that expect Unity Object references, use:\n"
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
            description=(
                "Comprehensive asset file management under Assets/ folder: create (any file type including C# scripts, JSON, text), "
                "update (modify file contents), delete, rename, duplicate, inspect (view properties and content), "
                "updateImporter (modify asset import settings), and batch operations "
                "(findMultiple/deleteMultiple/inspectMultiple with pattern matching). "
                "Essential for managing scripts, textures, audio, data files, and all Unity assets.\n\n"
                "**Note:** For .cs files, create/update/delete operations automatically wait for Unity compilation "
                "to complete and return compilation results — no need to call compilation_await separately."
            ),
            inputSchema=asset_manage_schema(),
        ),
        types.Tool(
            name="unity_scriptableObject_crud",
            description="ScriptableObject asset management: create new instances from type name, inspect/update properties, delete, duplicate, list all instances, or find by type. ScriptableObjects are Unity's data container assets perfect for game configuration (stats, settings, levels). Use 'create' to instantiate from existing type, 'update' to modify properties, 'list' to see all instances, 'findByType' to search by class name. Supports property filtering and batch operations.",
            inputSchema=scriptable_object_manage_schema(),
        ),
        types.Tool(
            name="unity_prefab_crud",
            description=(
                "Complete prefab workflow management: create, update, inspect, instantiate, unpack, applyOverrides, revertOverrides, "
                "editAsset (direct prefab editing without scene instantiation), editMultiple (batch edit multiple prefabs).\n\n"
                "**editAsset**: Edit a prefab asset directly — set tag, layer, add/update/remove components with property changes. "
                "No need to instantiate→modify→apply→delete. Example: editAsset with tag='Enemy', layer='Enemy', "
                "componentChanges=[{componentType:'Rigidbody2D', propertyChanges:{gravityScale:0}}].\n\n"
                "**editMultiple**: Apply the same tag/layer/component changes to multiple prefabs at once. "
                "Pass prefabPaths array + shared tag/layer/componentChanges.\n\n"
                "Use 'create' to save GameObjects as prefabs, 'instantiate' to spawn instances, "
                "'applyOverrides' to update from modified instance, 'editAsset'/'editMultiple' for direct asset editing."
            ),
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
            name="unity_camera_bundle",
            description="Mid-level Camera creation with presets: create Camera GameObjects, apply presets (default, orthographic2D, firstPerson, thirdPerson, topDown, splitScreenLeft/Right/Top/Bottom, minimap, uiCamera). Full Camera property control on create: fieldOfView, orthographic, orthographicSize, clearFlags, backgroundColor, cullingMask, depth, nearClipPlane, farClipPlane, rect (viewport), targetDisplay, renderingPath, allowHDR, allowMSAA. For update/inspect/delete, use component_crud (Camera) or gameobject_crud.",
            inputSchema=camera_bundle_schema(),
        ),
        types.Tool(
            name="unity_ui_foundation",
            description="Mid-level UI foundation for UGUI: create complete UI elements with single commands (Canvas with EventSystem, Panel with Image, Button with Text child, Text with styling, Image with sprite support, InputField with placeholder, Slider with fill/handle, Toggle with checkmark/label, ScrollView with Viewport/Content/Scrollbars). Supports addLayoutGroup (Horizontal/Vertical/Grid) with full configuration, ContentSizeFitter, and UI templates (dialog/hud/menu/statusBar/inventoryGrid). createPanel supports addCanvasGroup option. Visibility control: show/hide/toggle always uses CanvasGroup (alpha, interactable, blocksRaycasts). GameObjects remain active; SetActive is not used. inspectTree exports recursive UI hierarchy as JSON. inspect reports single element properties. extractDesignContext exports comprehensive UI hierarchy with typography (fontFamily, fontSize, fontStyle, alignment, lineSpacing, overflowMode), visual properties (backgroundColor, sprite, imageType, material), interaction states (navigationMode, transitionType, colorBlock with all 5 states), and a summary (totalElements, elementsByType, canvasSettings) — designed for AI design-to-code workflows. Supports render modes, anchor presets, automatic sizing, and color configuration. For updateLayoutGroup/removeLayoutGroup/configureCanvasGroup, use component_crud directly.",
            inputSchema=ui_foundation_schema(),
        ),
        types.Tool(
            name="unity_input_profile",
            description=(
                "Mid-level input system setup with New Input System.\n\n"
                "**Operations:**\n"
                "- createPlayerInput: Add PlayerInput component to GameObject with preset notification behavior\n"
                "- createInputActions: Generate .inputactions JSON file with full action maps, bindings, and composite bindings\n"
                "- inspect: Inspect PlayerInput component state\n\n"
                "**Genre Presets for createInputActions:** "
                "'shooter2d' (Move/Shoot/Bomb/SlowMove/Pause with WASD+Arrows+Gamepad), "
                "'platformer2d' (Move/Jump/Attack/Dash/Pause with WASD+Arrows+Gamepad).\n\n"
                "**Custom actionMaps**: Define action maps with actions, simple bindings, and composite bindings "
                "(2DVector for WASD, 1DAxis for triggers). Writes a complete .inputactions JSON file that Unity can load.\n\n"
                "Essential for setting up player input. Use genre presets for quick setup or actionMaps for full control."
            ),
            inputSchema=input_profile_schema(),
        ),
        types.Tool(
            name="unity_tilemap_bundle",
            description="Mid-level Tilemap management: create Tilemaps with Grid parent, set/get/clear individual tiles, fill rectangular areas, create Tile and RuleTile assets from sprites. Operations: createTilemap (auto-creates Grid), setTile/getTile/setTiles (place tiles at positions), clearTile/clearTiles/clearAllTiles (remove tiles), fillArea/boxFill (batch placement), worldToCell/cellToWorld (coordinate conversion), createTile/createRuleTile/inspectTile/updateTile (tile asset management). Supports Rectangle/Hexagon/Isometric layouts, sorting layers. For TilemapRenderer/TilemapCollider2D settings, use component_crud directly.",
            inputSchema=tilemap_bundle_schema(),
        ),
        types.Tool(
            name="unity_ui_state",
            description=(
                "Mid-level UI state management: define, save, load, and transition between UI states.\n"
                "**This is the standard tool for switching between gameplay and menu modes** "
                "(e.g., gameplay ↔ pause menu, gameplay ↔ inventory, title ↔ in-game).\n\n"
                "**Operations:**\n"
                "- defineState: Define a named UI state with element configurations (visible, interactable, alpha, blocksRaycasts, ignoreParentGroups, position, size). Visibility uses CanvasGroup only (no SetActive).\n"
                "- applyState: Apply a saved state to UI elements\n"
                "- saveState: Capture current UI state (including children)\n"
                "- loadState: Load state definition without applying\n"
                "- listStates: List all defined states for a root\n"
                "- createStateGroup: Create a group of mutually exclusive states\n"
                "- transitionTo: Transition to a state (alias for applyState)\n"
                "- getActiveState: Get currently active state name\n\n"
                "**Primary Use Case — Gameplay / Menu Switching:**\n"
                "Define states like 'gameplay', 'paused', 'inventory', 'settings' etc., "
                "then call applyState to switch. Use createStateGroup to make them mutually exclusive.\n\n"
                "**Other Use Cases:**\n"
                "- Dialog states (open, closed, minimized)\n"
                "- HUD mode variants (combat, exploration, cutscene)\n"
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
                "- inspect: View current navigation configuration\n\n"
                "For reset/disable, use component_crud to update Selectable navigation mode directly.\n\n"
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
                "- query: Search the live VisualElement tree by name, USS class, or element type (UQuery-style)\n\n"
                "For update/delete, use component_crud (UIDocument) or gameobject_crud.\n\n"
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
                "- createPanelSettings: Create PanelSettings ScriptableObject asset\n"
                "- createFromTemplate: Generate UXML+USS from built-in templates (menu, dialog, hud, settings, inventory)\n"
                "- validateDependencies: Validate UXML references (Style src, Template src) and report missing files\n"
                "- auditUSS: Check USS files for missing pseudo-classes (:active/:focus/:disabled) on button selectors and missing transitions\n"
                "- auditUXML: Check UXML files for layout issues (small button sizes, fixed widths without responsive fallback, missing min-height on dynamic containers)\n\n"
                "**Dependency Validation:** createUXML, createFromTemplate, and inspectUXML automatically include "
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
        types.Tool(
            name="unity_ui_convert",
            description=(
                "Mid-level UGUI ↔ UI Toolkit conversion: analyze, convert, and extract styles between "
                "Canvas-based UGUI and UI Toolkit (UXML/USS).\n\n"
                "**Operations:**\n"
                "- analyze: Non-destructive analysis of a UGUI Canvas or UITK UXML file. Reports convertible elements, "
                "warnings, unsupported items, and estimated accuracy. Use before conversion to understand scope.\n"
                "- toUITK: Convert UGUI Canvas hierarchy to UXML + USS files. Traverses Canvas tree, maps elements to "
                "UITK equivalents, extracts styles to USS, generates UXML with class references.\n"
                "- toUGUI: Convert UXML file to UGUI Canvas hierarchy in scene. Parses UXML + referenced USS, creates "
                "Canvas with mapped UGUI elements (Button, Text, Image, Slider, ScrollRect, etc.).\n"
                "- extractStyles: Extract styles from UGUI Canvas hierarchy to a standalone USS file without conversion. "
                "Useful for creating a USS baseline from existing UGUI styling.\n"
                "- extractTokens: Scan UGUI hierarchy and extract deduplicated design tokens — color palette, font sizes, "
                "font families, spacing values, element sizes — with usage counts and near-duplicate detection. "
                "Returns a consolidated design system inventory, not per-element styles.\n\n"
                "**Recommended workflow:** analyze → review report → toUITK/toUGUI → manual adjustments.\n\n"
                "**sourceType (for analyze):** ugui (Canvas→UITK) or uitk (UXML→UGUI).\n"
                "**Limitations:** UnityEvent callbacks, Animator-driven UI, and custom components require manual migration."
            ),
            inputSchema=ui_convert_schema(),
        ),
        # ── Visual ─────────────────────────────────────────────────
        types.Tool(
            name="unity_sprite2d_bundle",
            description="Mid-level 2D sprite management: create SpriteRenderer GameObjects, slice sprite sheets, create SpriteAtlas assets. Operations: createSprite (new GameObject with SpriteRenderer), sliceSpriteSheet (grid/automatic slicing), createSpriteAtlas (pack sprites). For update/inspect/batch operations on SpriteRenderer, use component_crud directly.",
            inputSchema=sprite2d_bundle_schema(),
        ),
        types.Tool(
            name="unity_animation2d_bundle",
            description="Mid-level 2D animation setup: create Animator components, AnimatorControllers, states, transitions, and AnimationClips from sprite sequences. Operations: setupAnimator (Animator component), createController/addState/addTransition/addParameter/inspectController (AnimatorController), createClipFromSprites/updateClip/inspectClip (AnimationClip). For Animator component update/inspect, use component_crud directly.",
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
                "- listPresets: List available presets\n\n"
                "For delete/duplicate, use asset_crud. For applying material to objects, use component_crud (update Renderer.sharedMaterial).\n\n"
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
                "- applyPreset: Apply light preset\n"
                "- createLightingSetup: Create complete lighting setup\n"
                "- listPresets: List available presets\n\n"
                "For update/inspect/delete, use component_crud (Light) or gameobject_crud.\n\n"
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
                "- listPresets: List available presets\n\n"
                "For delete/duplicate, use gameobject_crud.\n\n"
                "**Presets:** explosion, fire, smoke, sparkle, rain, snow, dust, trail, hit, heal, magic, leaves"
            ),
            inputSchema=particle_bundle_schema(),
        ),
        # ── Mid-Level Physics & NavMesh ────────────────────────────
        types.Tool(
            name="unity_physics_bundle",
            description=(
                "Mid-level physics configuration: apply physics presets, configure collision matrices, and create physics materials.\n\n"
                "**Operations:**\n"
                "- applyPreset: Apply physics preset to GameObject (platformer2D, topDown2D, fps3D, thirdPerson3D, space, racing)\n"
                "- setCollisionMatrix: Configure single layer collision pair (Physics or Physics2D)\n"
                "- setCollisionMatrixBatch: Configure multiple layer collision pairs in one call. "
                "Pass pairs=[{layerA, layerB, ignore?}, ...] and is2D=true/false. "
                "Much faster than multiple setCollisionMatrix calls.\n"
                "- createPhysicsMaterial: Create PhysicMaterial asset (3D) with friction/bounciness\n"
                "- createPhysicsMaterial2D: Create PhysicsMaterial2D asset with friction/bounciness\n"
                "- inspect: View current Rigidbody/Collider/PhysicsMaterial configuration\n\n"
                "**Presets:** platformer2D (high gravity, freeze rotation Z), topDown2D (no gravity, kinematic-optional), "
                "fps3D (CharacterController-ready), thirdPerson3D (capsule collider + rigidbody), "
                "space (zero gravity, no drag), racing (wheel collider-ready)\n\n"
                "Use for quick physics setup instead of manually configuring Rigidbody + Collider via unity_component_crud."
            ),
            inputSchema=physics_bundle_schema(),
        ),
        types.Tool(
            name="unity_navmesh_bundle",
            description=(
                "Mid-level NavMesh navigation setup: bake navigation meshes, add agents, obstacles, links, and modifiers.\n\n"
                "**Operations:**\n"
                "- bake: Add NavMeshSurface and bake NavMesh (requires com.unity.ai.navigation package, falls back to legacy API)\n"
                "- addAgent: Add NavMeshAgent with speed/stopping/radius configuration\n"
                "- addObstacle: Add NavMeshObstacle with shape/carve settings\n"
                "- addLink: Add NavMeshLink between two points (requires com.unity.ai.navigation)\n"
                "- addModifier: Add NavMeshModifier for area overrides (requires com.unity.ai.navigation)\n"
                "- inspect: View NavMesh agent/obstacle/surface configuration\n"
                "- clearNavMesh: Clear baked NavMesh data\n\n"
                "**Package Support:** Built-in types (NavMeshAgent, NavMeshObstacle) always available. "
                "Advanced types (NavMeshSurface, NavMeshLink, NavMeshModifier) require com.unity.ai.navigation package "
                "and are accessed via reflection for compatibility."
            ),
            inputSchema=navmesh_bundle_schema(),
        ),
        # ── GameKit – UI ──────────────────────────────────────────
        types.Tool(
            name="unity_gamekit_ui",
            description=(
                "High-level GameKit UI: unified UI widget system using UI Toolkit (UXML/USS).\n\n"
                "**widgetType** selects the widget category:\n\n"
                "**command** — Command panels with buttons that send commands to GameKitActors/Managers.\n"
                "  Operations: createCommandPanel, addCommand, inspect\n"
                "  Actor Commands: move, jump, action, look, custom\n"
                "  Manager Commands: addResource, setResource, consumeResource, changeState, nextTurn, triggerScene\n\n"
                "**binding** — Declarative UI data binding.\n"
                "  Operations: create, inspect, setRange, refresh, findByBindingId\n"
                "  Source Types: health, economy, timer, custom | Formats: raw, percent, ratio, formatted\n\n"
                "**list** — Dynamic list/grid for displaying collections.\n"
                "  Operations: create, inspect, setItems, addItem, removeItem, clear,\n"
                "    selectItem, deselectItem, clearSelection, refreshFromSource, findByListId\n"
                "  Layout: vertical, horizontal, grid | Data Sources: custom, inventory, equipment\n\n"
                "**slot** — Slot-based UI for equipment/quickslots/item management.\n"
                "  Slot: create, inspect, setItem, clearSlot, setHighlight, findBySlotId\n"
                "  Bar: createSlotBar, inspectSlotBar, useSlot, refreshFromInventory, findByBarId\n"
                "  Slot Types: storage, equipment, quickslot, trash\n\n"
                "**selection** — Selection groups (toggles, radios, checkboxes, tabs).\n"
                "  Operations: create, inspect, setItems, addItem, removeItem, clear,\n"
                "    selectItem, selectItemById, deselectItem, clearSelection, setSelectionActions, setItemEnabled, findBySelectionId\n"
                "  Selection Types: radio, toggle, checkbox, tab\n\n"
                "All generated scripts are standalone — no dependency on Unity-AI-Forge package."
            ),
            inputSchema=gamekit_ui_schema(),
        ),
        # ── GameKit – Data ────────────────────────────────────────
        types.Tool(
            name="unity_gamekit_data",
            description=(
                "High-level GameKit Data: unified data architecture for pools, events, containers, and runtime sets.\n\n"
                "**dataType** selects the data category:\n\n"
                "**pool** — Object pooling using UnityEngine.Pool.\n"
                "  Operations: create, inspect, find\n"
                "  Configurable: prefab, initialSize, maxSize, collectionCheck, defaultParent\n\n"
                "**eventChannel** — ScriptableObject-based typed event channels.\n"
                "  Operations: create, inspect, find\n"
                "  Event Types: void, int, float, string, Vector3, GameObject\n"
                "  Optional: createListener (EventListener MonoBehaviour on targetPath)\n\n"
                "**dataContainer** — ScriptableObject data containers with custom fields.\n"
                "  Operations: create, inspect, find\n"
                "  Field Types: int, float, string, bool, Vector2, Vector3, Color\n"
                "  Optional: resetOnPlay (reset values on play mode entry)\n\n"
                "**runtimeSet** — ScriptableObject runtime sets for auto-register/unregister patterns.\n"
                "  Operations: create, inspect, find\n"
                "  Configurable: elementType (default: GameObject)\n\n"
                "**getIntegrationCode** — Returns C# code snippets and MCP wiring commands for integrating\n"
                "  a generated asset into game scripts. Pass dataType + dataId.\n"
                "  Returns: SerializeField declarations, Raise/Register/Get calls, Inspector wiring examples.\n\n"
                "All generated scripts are standalone — no dependency on Unity-AI-Forge package."
            ),
            inputSchema=gamekit_data_schema(),
        ),
        # ── GameKit – Logic Pillar ─────────────────────────────────
        types.Tool(
            name="unity_validate_integrity",
            description=(
                "High-level GameKit Integrity: validate scene integrity by detecting broken references and missing assets.\n\n"
                "**Operations:**\n"
                "- missingScripts: Detect GameObjects with missing (null) MonoBehaviour scripts\n"
                "- nullReferences: Detect SerializedProperty object references pointing to destroyed objects\n"
                "- brokenEvents: Detect UnityEvent listeners with null targets or missing methods\n"
                "- brokenPrefabs: Detect prefab instances with missing or disconnected assets\n"
                "- all: Run all checks and return categorized summary\n"
                "- typeCheck: Detect type mismatches in object reference fields (field type vs actual object type)\n"
                "- report: Run all integrity checks across multiple scenes (scope: active_scene/build_scenes/all_scenes)\n"
                "- checkPrefab: Validate a prefab asset for missing scripts, null refs, and broken events\n"
                "- canvasGroupAudit: Detect CanvasGroup alpha conflicts (parent alpha=0 blocks child) and mismatched references\n"
                "- referenceSemantics: Detect logical reference issues (references to inactive objects, self-references)\n"
                "- requiredFieldAudit: Detect null SerializedFields that are used in code without null guards\n"
                "- uiOverflowAudit: Detect UI layout overflow (content exceeding parent bounds without ScrollRect, sizeDelta overflow)\n"
                "- uiOverlapAudit: Detect UI sibling overlap (same-position without LayoutGroup, interactive overlap, raycast blocking)\n"
                "- nullAssetAudit: Detect null asset references (Sprite, AudioClip, etc.) in ScriptableObject assets\n"
                "- touchTargetAudit: Detect interactive UI elements (Button, Toggle, Slider) smaller than 44x44 minimum touch target size\n"
                "- eventSystemAudit: Detect scenes with Canvas/UIDocument but no EventSystem, or duplicate EventSystems\n"
                "- textOverflowAudit: Detect Text/TextMeshPro elements where content exceeds RectTransform bounds\n"
                "- styleConsistencyAudit: Detect cross-element design consistency issues (button color variation, "
                "font size scale violations, spacing inconsistency, no-op CanvasGroups, missing interaction feedback, "
                "unnecessary raycast targets, inconsistent sibling anchor patterns)\n"
                "- inputSystemAudit: Detect Input System consistency issues (PlayerInput notificationBehavior vs method signatures, "
                "action expectedControlType vs binding composite type, missing callback methods)\n\n"
                "**Use after:** Deleting GameObjects/Components, renaming objects, changing prefab references, "
                "modifying UnityEvent connections, or changing ScriptableObject references.\n\n"
                "Returns a flat issue list with type, severity (error/warning), gameObjectPath, message, and optional suggestion. "
                "Use 'rootPath' parameter to limit analysis to a specific subtree."
            ),
            inputSchema=validate_integrity_schema(),
        ),
        types.Tool(
            name="unity_class_dependency_graph",
            description=(
                "High-level GameKit Class Dependency Graph: analyze class/script dependency relationships in the Unity project.\n\n"
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
            name="unity_class_catalog",
            description=(
                "High-level GameKit Class Catalog: enumerate and inspect types (classes, structs, interfaces, enums, MonoBehaviours, ScriptableObjects) in the Unity project.\n\n"
                "**Operations:**\n"
                "- listTypes: List types with optional filters (searchPath, typeKind, namespace, baseClass, namePattern, maxResults)\n"
                "- inspectType: Inspect a single type in detail (fields, methods, properties, attributes)\n\n"
                "**Use Cases:**\n"
                "- Discover available MonoBehaviours/ScriptableObjects before using unity_component_crud or unity_scriptableObject_crud\n"
                "- Find serializable fields to know which properties can be set via unity_component_crud propertyChanges\n"
                "- Explore project type structure by namespace, base class, or name pattern\n\n"
                "**Complements unity_class_dependency_graph** which focuses on relationships between types, "
                "while this tool focuses on the types themselves and their members."
            ),
            inputSchema=class_catalog_schema(),
        ),
        types.Tool(
            name="unity_scene_reference_graph",
            description=(
                "High-level GameKit Scene Reference Graph: analyze object reference relationships within a Unity scene.\n\n"
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
                "High-level GameKit Scene Relationship Graph: analyze relationships between Unity scenes (transitions, dependencies).\n\n"
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
        types.Tool(
            name="unity_scene_dependency",
            description=(
                "High-level GameKit Scene Dependency: analyze asset dependencies of Unity scenes using AssetDatabase.\n\n"
                "**Operations:**\n"
                "- analyzeScene: List all asset dependencies of a scene, categorized by type "
                "(Material, Texture, Shader, Model, Audio, Prefab, Script, etc.)\n"
                "- findAssetUsage: Find all scenes that reference a specific asset (reverse lookup)\n"
                "- findSharedAssets: Find assets shared across multiple scenes\n"
                "- findUnusedAssets: Find assets not referenced by any scene\n\n"
                "**Parameters:**\n"
                "- scenePath: Scene to analyze (required for analyzeScene)\n"
                "- assetPath: Asset to look up (required for findAssetUsage)\n"
                "- includeIndirect: Include transitive dependencies (default: true)\n"
                "- typeFilter: Filter by asset category (Material, Texture, Script, etc.)\n"
                "- searchPath: Limit search scope to a folder\n"
                "- scenePaths: Array of scenes for findSharedAssets (default: all)\n"
                "- minSharedCount: Minimum shared count for findSharedAssets (default: 2)\n\n"
                "**Use Cases:**\n"
                "- Audit scene dependencies before building\n"
                "- Find which scenes use a specific material or texture\n"
                "- Identify shared assets for optimization (atlasing, bundling)\n"
                "- Clean up unused assets to reduce project size"
            ),
            inputSchema=scene_dependency_schema(),
        ),
        types.Tool(
            name="unity_script_syntax",
            description=(
                "High-level GameKit Script Syntax: analyze C# source code structure with line numbers.\n\n"
                "**Operations:**\n"
                "- analyzeScript: Parse a C# file and return its full structure "
                "(using directives, namespaces, types, methods, fields, properties) with line numbers\n"
                "- findReferences: Find all references to a symbol (method, class, field, property) "
                "across project scripts, with reference type classification\n"
                "- findUnusedCode: Detect methods and fields declared but never referenced "
                "in other files (dead code detection)\n"
                "- analyzeMetrics: Compute code metrics — lines of code, comment ratio, "
                "method count/length, nesting depth, complexity\n"
                "- eventCoverage: Detect orphaned event publishes (no subscribers) and subscribes (no publishers), "
                "plus critical event violations (too few subscribers)\n"
                "- fsmReachability: Detect FSM enum states with no handler — unreachable states that stall game loops\n\n"
                "**Complements existing tools:**\n"
                "- unity_class_dependency_graph: uses reflection on compiled types (no line numbers)\n"
                "- unity_class_catalog: inspects compiled type metadata via reflection\n"
                "- unity_script_syntax: parses source code directly (line numbers, reference search, metrics)\n\n"
                "**Reference Types Detected:** method_call, instantiation, type_usage, inheritance, "
                "typeof, generic_argument, static_access, member_access"
            ),
            inputSchema=script_syntax_schema(),
        ),
    ]
