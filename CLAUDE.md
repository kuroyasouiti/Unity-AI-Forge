# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity-AI-Forge is an AI-powered Unity development toolkit that integrates with the Model Context Protocol (MCP). It provides 52 tools for AI-driven game development, including a GameKit framework with 3-pillar architecture (UI, Logic, Presentation) plus a Systems pillar (Pool, Data). GameKit uses code generation to produce standalone C# scripts from templates, so user projects have zero runtime dependency on Unity-AI-Forge.

## Requirements

- Unity 2022.3 LTS or later
- Python 3.10+ (for MCP Server)
- .NET Standard 2.1

## Build & Test Commands

### Unity Editor Tests
```powershell
# Windows (PowerShell)
.\run-tests.ps1 -UnityPath "C:\Program Files\Unity\Hub\Editor\2022.3.0f1\Editor\Unity.exe"

# macOS/Linux (Bash)
./run-tests.sh --unity-path "/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity"
```

Test results are written to `TestResults.xml` and logs to `TestLog.txt`.

### Python MCP Server
```bash
cd Assets/UnityAIForge/MCPServer

# Install dependencies
uv sync

# Run linting
uv run ruff check src/
uv run black --check src/

# Run type checking
uv run mypy src/

# Run tests
uv run pytest
```

## Architecture

### Assembly Structure

The project uses Unity's Assembly Definition system with three assemblies:

- **UnityAIForge.Editor** (`Assets/UnityAIForge/Editor/`) - Editor-only code containing the MCP Bridge, command handlers, code generation, and server management. Namespace: `MCP.Editor`
- **UnityAIForge.Editor.Tests** (`Assets/UnityAIForge/Tests/Editor/`) - Editor tests. References: `UnityAIForge.Editor`, `Unity.TextMeshPro`. Namespace: `MCP.Editor.Tests`
- **UnityAIForge.Tests.Runtime** (`Assets/UnityAIForge/Tests/Runtime/`) - Runtime test assembly (placeholder). Namespace: `MCP.Tests.Runtime`

### MCP Bridge (C# Editor)

The bridge enables WebSocket communication between AI clients and Unity Editor. Core files in `Assets/UnityAIForge/Editor/MCPBridge/`:

- `McpBridgeService.cs` - Core WebSocket server handling connections and message routing
- `McpBridgeWindow.cs` - Unity Editor window for controlling the bridge
- `McpCommandProcessor.cs` - Dispatches incoming commands to appropriate handlers
- `McpBridgeSettings.cs` - Bridge configuration (port, auto-start, etc.)
- `McpBridgeMessages.cs` - Message type definitions for WebSocket protocol
- `McpContextCollector.cs` - Collects context information from Unity Editor
- `McpPendingCommandStorage.cs` - Persists pending commands across domain reloads
- `McpConstantConverter.cs` - Converts constant values between MCP and Unity types

### Handler Infrastructure

Located in `Assets/UnityAIForge/Editor/MCPBridge/Base/`:

- `BaseCommandHandler.cs` - Abstract base class for all command handlers
- `CommandHandlerInitializer.cs` - `[InitializeOnLoad]` class that registers all handlers at startup
- `CommandHandlerFactory.cs` - Factory for handler lookup by bridge name
- `StandardPayloadValidator.cs` - Validates incoming command payloads
- `ComponentPropertyApplier.cs` - Applies property changes to Unity components
- `ValueConverterManager.cs` - Manages type conversions (string/JSON to Unity types)
- `UnityResourceResolver.cs` - Resolves asset paths and scene object references
- `JsonConverters/UnityJsonSettings.cs` - JSON serialization settings for Unity types
- `JsonConverters/UnityTypesJsonConverter.cs` - Custom JSON converters for Vector3, Color, etc.

Interfaces in `Assets/UnityAIForge/Editor/MCPBridge/Interfaces/`:

- `ICommandHandler.cs`, `IOperationHandler.cs`, `IPayloadValidator.cs`, `IPropertyApplier.cs`, `IResourceResolver.cs`, `IRectTransformOperationHandler.cs`

### Command Handlers

Located in `Assets/UnityAIForge/Editor/MCPBridge/Handlers/`, handlers are organized into 6 categories:

**LowLevel/** (7 handlers) - Basic Unity operations:
- `SceneCommandHandler.cs` - Scene management (create/load/save/delete/inspect)
- `GameObjectCommandHandler.cs` - GameObject lifecycle (create/delete/move/rename/duplicate/inspect/batch)
- `ComponentCommandHandler.cs` - Component manipulation (add/remove/update/inspect/batch)
- `AssetCommandHandler.cs` - Asset file management (create/update/delete/rename/inspect)
- `PrefabCommandHandler.cs` - Prefab operations (create/instantiate/apply/revert/unpack)
- `ScriptableObjectCommandHandler.cs` - ScriptableObject management
- `VectorSpriteConvertHandler.cs` - Vector/primitive to sprite conversion

**MidLevel/** (21 handlers) - Batch operations, presets, visual control, and UI Toolkit:
- `TransformBatchHandler.cs` - Transform batch operations (arrange, rename patterns)
- `RectTransformBatchHandler.cs` - UI RectTransform batch (anchors, alignment, distribution)
- `CameraRigHandler.cs` - Camera rig presets (follow, orbit, splitScreen, etc.)
- `UIFoundationHandler.cs` - UGUI element creation (Canvas, Button, Text, ScrollView, etc.)
- `UIHierarchyHandler.cs` - Declarative UI hierarchy from JSON definitions
- `UIStateHandler.cs` - UI state management (define/apply/save/load states)
- `UINavigationHandler.cs` - Keyboard/gamepad navigation setup
- `InputProfileHandler.cs` - New Input System setup with action maps
- `TilemapBundleHandler.cs` - Tilemap creation and tile management
- `Sprite2DBundleHandler.cs` - 2D sprite management and sprite sheet slicing
- `Animation2DBundleHandler.cs` - 2D animation setup (Animator, AnimatorController, clips)
- `Animation3DBundleHandler.cs` - 3D animation setup (BlendTree, AvatarMask)
- `MaterialBundleHandler.cs` - Material creation and property management (Standard, URP, HDRP)
- `LightBundleHandler.cs` - Light setup with presets (directional, point, spot, area)
- `ParticleBundleHandler.cs` - Particle system creation and configuration
- `UITKDocumentHandler.cs` - UI Toolkit UIDocument management in scene
- `UITKAssetHandler.cs` - UI Toolkit asset creation (UXML, USS, PanelSettings)
- `PhysicsBundleHandler.cs` - Physics presets, collision matrix, physics materials
- `NavMeshBundleHandler.cs` - NavMesh baking, agents, obstacles, links, modifiers

**HighLevel/** (7 handlers) - Analysis and integrity tools (registered as GameKit Logic Pillar):
- `SceneIntegrityHandler.cs` - Scene integrity validation (missing scripts, null refs, broken events/prefabs, CanvasGroup audit, reference semantics)
- `ClassCatalogHandler.cs` - Type enumeration and inspection (classes, MonoBehaviours, enums)
- `ClassDependencyGraphHandler.cs` - Analyzes class dependencies in C# scripts
- `SceneReferenceGraphHandler.cs` - Analyzes references between GameObjects in scene
- `SceneRelationshipGraphHandler.cs` - Scene transition and relationship analysis
- `SceneDependencyHandler.cs` - Scene asset dependency analysis (AssetDatabase-based)
- `ScriptSyntaxHandler.cs` - C# source code structure analysis with line numbers, event coverage, FSM reachability

**Utility/** (5 handlers) - Helper tools:
- `PingHandler.cs` - Bridge connectivity check
- `CompilationAwaitHandler.cs` - Compilation monitoring with async polling
- `ConsoleLogHandler.cs` - Console log retrieval and filtering
- `PlayModeControlHandler.cs` - Play mode control (play/pause/stop/step/validateState)
- `EventWiringHandler.cs` - UnityEvent wiring (Button.onClick, Slider.onValueChanged, etc.)

**GameKit/** (12 handlers) - Game systems (3-Pillar Architecture, code generation):
- UI Pillar: `GameKitUICommandHandler.cs`, `GameKitUIBindingHandler.cs`, `GameKitUIListHandler.cs`, `GameKitUISlotHandler.cs`, `GameKitUISelectionHandler.cs`
- Presentation Pillar: `GameKitAnimationSyncHandler.cs`, `GameKitEffectHandler.cs`, `GameKitFeedbackHandler.cs`, `GameKitVFXHandler.cs`, `GameKitAudioHandler.cs`
- Systems: `GameKitPoolHandler.cs` (object pooling), `GameKitDataHandler.cs` (event channels, data containers, runtime sets)

**Settings/** (1 handler):
- `ProjectSettingsManageHandler.cs` - Project settings management (player, quality, physics, tags/layers, build settings)

### MCP Server (Python)

Located in `Assets/UnityAIForge/MCPServer/src/`:

- `main.py` - Entry point, sys.path setup and server launch
- `version.py` - Package version info
- `logger.py` - Logging configuration
- `tools/register_tools.py` - MCP tool registration and dispatch. Handles 4 special tools (ping, compilation_await, asset_crud, batch_sequential) and delegates remaining 48 tools via dict lookup from `TOOL_NAME_TO_BRIDGE`.
- `tools/tool_registry.py` - Single source of truth for 52 MCP tool name → bridge name mappings. Used by both `register_tools.py` and `batch_sequential.py`. Also provides `resolve_tool_name()` for bidirectional name resolution.
- `tools/tool_definitions.py` - All 52 `types.Tool` definitions with descriptions and schema references.
- `tools/batch_sequential.py` - Sequential command execution with resume capability
- `tools/schemas/` - JSON Schema definitions split into 8 category files:
  - `common.py` - Shared type helpers (Vector3, Color, etc.)
  - `utility.py`, `low_level.py`, `mid_level.py`, `visual.py`
  - `gamekit_core.py`, `gamekit_systems.py`, `gamekit_pillar.py`, `graph.py`
- `bridge/bridge_connector.py` - WebSocket connection to Unity Bridge
- `bridge/bridge_manager.py` - Bridge lifecycle, heartbeat, and compilation await
- `bridge/messages.py` - Bridge message serialization
- `config/env.py` - Environment variable configuration
- `config/port_discovery.py` - Auto-discovers Unity Bridge port from port file
- `server/create_mcp_server.py` - MCP server factory (creates Server instance, registers tools/resources/prompts)
- `prompts/loader.py` - MCP prompt template loader
- `resources/register_resources.py` - MCP resource registration
- `resources/batch_queue.py` - Batch command queue management
- `services/editor_log_watcher.py` - Unity Editor log file watcher
- `utils/client_detector.py` - Detects MCP client type (Claude Desktop, Cursor, etc.)
- `utils/fs_utils.py` - File system utilities
- `utils/json_utils.py` - JSON serialization helpers (`as_pretty_json`, etc.)

### MCP Server Manager (C# Editor)

Located in `Assets/UnityAIForge/Editor/MCPServerManager/`, provides Unity Editor UI for managing the Python MCP Server:

- `McpServerManager.cs` - Server lifecycle management (start/stop/restart)
- `McpConfigManager.cs` - MCP client configuration file management
- `McpToolRegistry.cs` - Tool registration and discovery
- `McpProjectRegistry.cs` - Project-specific registry
- `McpServerInstaller.cs` - Python environment and dependency installation
- `McpCliRegistry.cs` - CLI tool registry for MCP client integration

### Code Generation Architecture

GameKit handlers generate standalone C# scripts from templates instead of using runtime MonoBehaviours. Generated scripts have zero dependency on the Unity-AI-Forge package.

- **Templates** (16 files, 11 UI/Presentation + 5 Systems): `Assets/UnityAIForge/Editor/CodeGen/Templates/*.cs.txt`
  - UI Pillar: `UICommand.cs.txt`, `UIBinding.cs.txt`, `UIList.cs.txt`, `UISlot.cs.txt`, `UISelection.cs.txt`
  - Presentation Pillar: `AnimationSync.cs.txt`, `Effect.cs.txt`, `EffectManager.cs.txt`, `Feedback.cs.txt`, `VFX.cs.txt`, `Audio.cs.txt`
  - Systems: `ObjectPool.cs.txt`, `EventChannel.cs.txt`, `EventListener.cs.txt`, `DataContainer.cs.txt`, `RuntimeSet.cs.txt`
- **Infrastructure**: `Assets/UnityAIForge/Editor/CodeGen/`
  - `CodeGenHelper.cs` - Entry point: `CodeGenHelper.GenerateAndAttach(go, templateName, componentId, className, variables, outputDir)`
  - `ScriptGenerator.cs` - Script file generation logic
  - `TemplateRenderer.cs` - Template variable substitution engine
  - `GeneratedScriptTracker.cs` - Tracks generated scripts for cleanup
  - `PendingComponentAttacher.cs` - Attaches generated components after compilation
  - `UITKGenerationHelper.cs` - UI Toolkit (UXML/USS) specific generation
- **Output**: Generated scripts are placed in the user's `Assets/` folder
- **After create operations**: Compilation wait is required (`unity_compilation_await`)

## Key Patterns

### Adding New MCP Tools

1. Add MCP name → bridge name mapping in `Assets/UnityAIForge/MCPServer/src/tools/tool_registry.py`
2. Add schema function in the appropriate `tools/schemas/*.py` file
3. Add `types.Tool` entry in `Assets/UnityAIForge/MCPServer/src/tools/tool_definitions.py`
4. Create or extend a handler in `Assets/UnityAIForge/Editor/MCPBridge/Handlers/`
5. Register the handler in `Assets/UnityAIForge/Editor/MCPBridge/Base/CommandHandlerInitializer.cs`

### Component Reference Formats

ComponentCommandHandler supports multiple reference formats:
- Path: `"Player/Weapon"`
- Name: `"Enemy"`
- Pattern with wildcard: `"Enemy*"`
- Explicit type: `"type:Enemy"` or `"name:Enemy"`

### Wildcard Matching

`McpWildcardUtility.cs` provides pattern matching for batch operations using `*` and `?` wildcards.

### Utilities

**GraphAnalysis/** (`Assets/UnityAIForge/Editor/MCPBridge/Utilities/GraphAnalysis/`):
- `GraphNode.cs`, `GraphEdge.cs`, `GraphResult.cs` - Graph data structures
- `SceneReferenceAnalyzer.cs` - Analyzes component references in scene
- `ClassDependencyAnalyzer.cs` - Analyzes C# class dependencies
- `SceneRelationshipAnalyzer.cs` - Combines multiple relationship types
- `SceneIntegrityAnalyzer.cs` - Scene integrity validation logic
- `TypeCatalogAnalyzer.cs` - Type enumeration and reflection analysis
- `SceneDependencyAnalyzer.cs` - Analyzes scene asset dependencies via AssetDatabase
- `ScriptSyntaxAnalyzer.cs` - Parses C# source code structure with line numbers

**Other Utilities** (`Assets/UnityAIForge/Editor/MCPBridge/`):
- `Utilities/HandlerUtilities.cs` - Common utilities for command handlers (GameObject finding, component operations)
- `Utilities/McpCommandProcessor.Utilities.cs` - Command processor utility methods
- `Core/McpCommandProcessor.Helpers.cs` - Helper methods for command processing
- `Helpers/UI/RectTransformHelper.cs` - RectTransform utility methods
- `McpWildcardUtility.cs` - Wildcard pattern matching (`*`, `?`)
- `ProcessHelper.cs` - External process management
- `PatternTemplates.cs` - Pattern template definitions
- `PortDiscoveryFile.cs` - Port discovery file I/O for bridge connection
- `MiniJson.cs` - Lightweight JSON parser (zero-dependency)
- `Samples/SceneCommandHandlerSample.cs` - Example handler implementation

### Verifying Changes with Relationship Graphs

After making significant changes (deletion, rename, move, reference changes), use relationship graph tools to check for broken references:

```python
# Check for broken references after changes
unity_scene_relationship_graph(operation='analyze', includeReferences=True, includeEvents=True)

# Check incoming references before deleting an object
unity_scene_reference_graph(operation='analyze', rootPath='TargetObject', direction='incoming')
```

Use these tools to verify:
- After deleting GameObjects/Components
- After renaming objects
- After changing Prefab references
- After modifying UnityEvent connections
- After changing ScriptableObject references
