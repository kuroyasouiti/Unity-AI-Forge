# Changelog

All notable changes to Unity-AI-Forge will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.0] - 2025-11-29

### 🔥 Project Renamed

- **SkillForUnity** → **Unity-AI-Forge**
  - New package name: `com.unityaiforge`
  - New repository: `https://github.com/kuroyasouiti/Unity-AI-Forge`
  - Emphasizes AI-powered development and the "forging" of games through AI collaboration

### Breaking Changes

- **Project Name Change** - Update package references and imports
- **GameKit Manager** - Complete redesign to hub-based architecture. Existing code using manager methods will continue to work (backward compatible API), but the internal structure has changed.
- **GameKit Interaction** - New trigger types and action system may require updating existing interaction setups.
- **GameKit SceneFlow** - Transitions now defined per-scene rather than globally. Migration required for projects using scene transitions.

### Changed

- **GameKit Manager** - Redesigned as manager hub with mode-specific components
  - Automatically adds mode-specific components based on ManagerType
  - **TurnBased** → GameKitTurnManager (turn phases, turn counter, phase/turn events)
  - **ResourcePool** → GameKitResourceManager (Machinations-inspired resource flow system)
    - Resource pools with min/max constraints
    - Automatic resource flows (sources/drains)
    - Resource converters (crafting, transformation)
    - Resource triggers (threshold-based events)
    - Events: `OnResourceChanged`, `OnResourceTriggered`
  - **EventHub** → GameKitEventManager (event registration, event triggering)
  - **StateManager** → GameKitStateManager (state changes, state history)
  - **Realtime** → GameKitRealtimeManager (time scale, pause/resume, timers)
  - Convenience methods automatically delegate to mode-specific components
  - Backward compatible API (existing code continues to work)
  - `GetModeComponent<T>()` for direct access to mode-specific components

- **GameKit Interaction** - Redesigned as interaction hub
  - Supports traditional triggers (Collision, Trigger, Input, Proximity, Raycast)
  - **New specialized triggers**: TilemapCell, GraphNode, SplineProgress
  - **Extended actions**: TriggerActorAction, UpdateManagerResource, TriggerSceneFlow, TeleportToTile, MoveToGraphNode, SetSplineProgress
  - **Extended conditions**: ActorId, ManagerResource
  - UnityEvent integration (`OnInteractionTriggered`)
  - Cooldown and repeat settings
  - Manual trigger support
  - Debug logging option
  - Gizmo visualization for proximity and tilemap triggers

### Added
- **CharacterController Bundle** (`unity_character_controller_bundle`) - Mid-level tool
  - Apply CharacterController with presets: fps, tps, platformer, child, large, narrow, custom
  - Batch operations for multiple GameObjects
  - Configurable collision properties (radius, height, center, slope limit, step offset)
  - Inspect CharacterController properties including runtime state (isGrounded, velocity)
  
- **GameKit Actor Input System Integration**
  - `GameKitInputSystemController` component for Unity's new Input System
  - Automatic PlayerInput configuration with pre-built action map
  - Default input actions asset generation (WASD, Mouse, Gamepad support)
  - Automatic fallback to `GameKitSimpleInput` when Input System unavailable
  - 2D/3D input conversion based on behavior profile

- **GameKit AI Controller**
  - `GameKitSimpleAI` component for autonomous character control
  - AI behaviors: Idle, Patrol, Follow, Wander
  - Configurable waypoints, follow targets, wander radius

### Changed
- **GameKit UI Command Hub** - Redesigned as UI-to-Actor bridge
  - Now acts as centralized hub bridging UI controls to `GameKitActor`'s UnityEvents
  - Command type system (Move, Jump, Action, Look, Custom)
  - Directional button support for movement commands
  - Parameter-based action commands
  - Actor reference caching for better performance
  - Backward compatible with `SendMessage` via Custom command type
  - Improved API: `ExecuteMoveCommand()`, `ExecuteJumpCommand()`, `ExecuteActionCommand()`, `ExecuteLookCommand()`
  - Command binding management: `RegisterButton()`, `RegisterDirectionalButton()`, `ClearBindings()`
  - Enhanced debugging with optional command logging

- **GameKit SceneFlow** - Redesigned as scene-centric state machine
  - Transitions now integrated into scene definitions (scene-centric design)
  - Same trigger can lead to different destinations based on current scene (e.g., "nextPage" from Page1→Page2, from Page2→Page3)
  - **Simplified shared scene management**: Removed `SharedSceneGroup`, scenes now directly define their shared scene paths
  - Scene definitions include transitions and shared scene paths
  - Improved shared scene management (only reload what's needed)
  - New API: `SetCurrentScene()`, `GetAvailableTriggers()`, `GetSceneNames()`, `AddSharedScenesToScene()`
  - Enhanced logging for scene transitions
  - Backward compatible API (AddTransition parameter order changed to: fromScene, trigger, toScene)
  - `sharedGroups` parameter renamed to `sharedScenePaths` (legacy `sharedGroups` still supported for backward compatibility)

- **GameKit Graph Node Movement** - New behavior profile
  - `GraphNode` component for defining movement nodes

- **GameKit Spline Movement** - New behavior profile for 2.5D games
  - `SplineMovement` component for rail/spline-based movement
  - Catmull-Rom spline interpolation for smooth curved paths
  - Closed loop support for circular tracks
  - Lateral offset for lane-based gameplay (rail shooters, side-scrollers)
  - Manual and automatic speed control with acceleration/deceleration
  - Forward and backward movement support
  - Auto-rotation to face movement direction (configurable axis)
  - Visual spline debugging in Scene view
  - Ideal for rail shooters, 2.5D platformers, racing games, on-rails sequences
  - `GraphNodeMovement` component with A* pathfinding
  - Node connections with cost and traversability
  - Works in both 2D and 3D (dimension-agnostic)
  - Use cases: board games, tactical RPGs, puzzle games, adventure games
  - Features: weighted edges, pathfinding, reachable node queries, debug visualization

### Changed
- Updated `GameKitActorHandler.ApplyControlComponents()` to use Input System by default
- Added `UNITY_INPUT_SYSTEM_INSTALLED` define constraint to GameKit Runtime assembly

### Documentation
- Added CharacterController Bundle comprehensive documentation
- Added GameKit Runtime components README with architecture overview
- Updated README.md and README_ja.md with new features

## [1.8.0] - 2025-11-29

### Added

#### New Tools
- **Prefab Management** (`unity_prefab_crud`)
  - Create prefabs from GameObjects
  - Update, inspect, instantiate prefabs
  - Unpack prefabs (completely or outermost)
  - Apply/revert prefab overrides
  
- **Vector Sprite Conversion** (`unity_vector_sprite_convert`)
  - Generate sprites from primitives (square, circle, triangle, polygon)
  - Import SVG to sprite
  - Convert textures to sprites
  - Create solid color sprites

#### GameKit Framework (High-Level Tools)
- **GameKit Actor** (`unity_gamekit_actor`)
  - Behavior profiles: 2D/3D physics, linear, tilemap movement
  - Control modes: direct controller, AI, UI command
  - Stats, abilities, weapon loadouts
  
- **GameKit Manager** (`unity_gamekit_manager`)
  - Manager types: turn-based, realtime, resource pool, event hub, state manager
  - Turn phase management
  - Resource pool with Machinations framework support
  - Persistence (DontDestroyOnLoad)
  
- **GameKit Interaction** (`unity_gamekit_interaction`)
  - Trigger types: collision, trigger, raycast, proximity, input
  - Declarative actions: spawn prefab, destroy object, play sound, send message, change scene
  - Conditions: tag, layer, distance, custom
  
- **GameKit UI Command** (`unity_gamekit_ui_command`)
  - Command panels with button layouts (horizontal, vertical, grid)
  - Actor command dispatch
  - Icon and label support
  
- **GameKit SceneFlow** (`unity_gamekit_sceneflow`)
  - Scene state machine with transitions
  - Additive scene loading
  - Persistent manager scene
  - Shared scene groups (UI, Audio)
  - Scene-crossing reference resolution

#### Mid-Level Tools
- **Transform Batch** (`unity_transform_batch`)
  - Arrange objects in circles/lines
  - Sequential/list-based renaming
  - Auto-generate menu hierarchies
  
- **RectTransform Batch** (`unity_rectTransform_batch`)
  - Set anchors, pivot, size, position
  - Align to parent presets
  - Distribute horizontally/vertically
  - Match size from source
  
- **Physics Bundle** (`unity_physics_bundle`)
  - 2D/3D Rigidbody + Collider presets
  - Presets: dynamic, kinematic, static, character, platformer, topDown, vehicle, projectile
  - Update individual physics properties
  
- **Camera Rig** (`unity_camera_rig`)
  - Camera rig presets: follow, orbit, split-screen, fixed, dolly
  - Target tracking and smooth movement
  - Viewport configuration
  
- **UI Foundation** (`unity_ui_foundation`)
  - Create Canvas, Panel, Button, Text, Image, InputField
  - Anchor presets
  - TextMeshPro support
  - Automatic layout
  
- **Audio Source Bundle** (`unity_audio_source_bundle`)
  - Audio presets: music, sfx, ambient, voice, ui
  - 2D/3D spatial audio
  - Mixer group integration
  
- **Input Profile** (`unity_input_profile`)
  - New Input System integration
  - Action map configuration
  - Notification behaviors: sendMessages, broadcastMessages, invokeUnityEvents, invokeCSharpEvents
  - Create InputActions assets

#### Features
- **Compilation Wait System**
  - Operations execute first, then wait for compilation if triggered
  - Bridge reconnection detection for early wait release
  - 60-second timeout with configurable intervals
  - Transparent wait information in responses
  - Automatic handling in BaseCommandHandler

- **Comprehensive Test Suite**
  - 100+ unit tests covering all tool categories
  - Unity Test Framework integration
  - 97.7% pass rate (42/43 tests)
  - Editor menu integration: `Tools > SkillForUnity > Run All Tests`
  - Command-line test runners (PowerShell, Bash)
  - CI/CD with GitHub Actions

#### Documentation
- Test suite documentation (`Assets/SkillForUnity/Tests/Editor/README.md`)
- Test results summary (`docs/TestResults_Summary.md`)
- Tooling roadmap - Japanese (`docs/tooling-roadmap.ja.md`)
- Compilation wait feature guide (`docs/Compilation_Wait_Feature.md`)
- Legacy cleanup summary (`docs/Unused_Handlers_Cleanup_Summary.md`)

### Changed

- **Tool Count**: Increased from 7 to 21 tools
- **BaseCommandHandler**: Compilation wait moved from before to after operation execution
- **AssetCommandHandler**: Added `AssetDatabase.Refresh()` after create/update operations
- **skill.yml**: Updated tool count and added new categories (prefab_management, sprite_conversion, batch_operations, gamekit_systems)

### Removed

- Legacy test files in `Assets/SkillForUnity/Editor/Tests/`
  - BaseCommandHandlerTests.cs
  - PayloadValidatorTests.cs
  - ResourceResolverTests.cs
  - CommandHandlerIntegrationTests.cs

- Unused handlers (not registered in MCP)
  - TemplateCommandHandler
  - UguiCreateFromTemplateHandler
  - UguiDetectOverlapsHandler
  - UguiLayoutManageHandler
  - UguiManageCommandHandler
  - ConstantConvertHandler
  - RenderPipelineManageHandler
  - TagLayerManageHandler (integrated into ProjectSettingsManageHandler)
  - RectTransformAnchorHandler (functionality in RectTransformBatchHandler)
  - RectTransformBasicHandler (functionality in RectTransformBatchHandler)

### Fixed

- Compilation wait now occurs after operation execution (more reliable)
- Bridge reconnection properly releases compilation wait
- Test suite compilation errors resolved

---

## [1.7.1] - 2025-11-XX

### Fixed

- **Template Tools**: Fixed scene quick setup, GameObject templates, UI templates, design patterns, script templates
- **Constant Conversion**: Fixed enum type resolution for Unity 2024.2+ module system
- **SerializedField Support**: Added support for `[SerializeField]` private fields in Component and ScriptableObject operations
- **Type Resolution**: 99%+ performance improvement through caching

### Added

- `listCommonEnums` operation: Lists commonly used Unity enum types by category
- Enhanced error messages with debugging information

### Changed

- Streamlined toolset: Focus on low-level CRUD operations

---

## [1.7.0] - 2025-XX-XX

### Added

- Initial MCP server implementation
- WebSocket bridge for Unity Editor
- Core CRUD operations: Scene, GameObject, Component, Asset, ScriptableObject
- Project settings management

---

[1.8.0]: https://github.com/kuroyasouiti/SkillForUnity/releases/tag/v1.8.0
[1.7.1]: https://github.com/kuroyasouiti/SkillForUnity/releases/tag/v1.7.1
[1.7.0]: https://github.com/kuroyasouiti/SkillForUnity/releases/tag/v1.7.0
