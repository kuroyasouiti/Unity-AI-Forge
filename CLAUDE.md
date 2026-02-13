# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity-AI-Forge is an AI-powered Unity development toolkit that integrates with the Model Context Protocol (MCP). It provides 64 tools for AI-driven game development, including a GameKit framework with 3-pillar architecture (UI, Logic, Presentation) for rapid prototyping.

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

The project uses Unity's Assembly Definition system with three main assemblies:

- **UnityAIForge.Editor** (`Assets/UnityAIForge/Editor/`) - Editor-only code containing the MCP Bridge and command handlers. Namespace: `MCP.Editor`
- **UnityAIForge.GameKit.Runtime** (`Assets/UnityAIForge/GameKit/Runtime/`) - Runtime GameKit components. Namespace: `UnityAIForge.GameKit`
- **UnityAIForge.Editor.Tests** (`Assets/UnityAIForge/Tests/Editor/`) - Editor tests. Namespace: `MCP.Editor.Tests`

### MCP Bridge (C# Editor)

The bridge enables WebSocket communication between AI clients and Unity Editor:

- `McpBridgeService.cs` - Core WebSocket server handling connections and message routing
- `McpBridgeWindow.cs` - Unity Editor window for controlling the bridge
- `McpCommandProcessor.cs` - Dispatches incoming commands to appropriate handlers

### Command Handlers

Located in `Assets/UnityAIForge/Editor/MCPBridge/Handlers/`, handlers are organized into categories:

**LowLevel/** - Basic Unity operations:
- `SceneCommandHandler.cs` - Scene management
- `GameObjectCommandHandler.cs` - GameObject operations
- `ComponentCommandHandler.cs` - Component manipulation
- `AssetCommandHandler.cs` - Asset management
- `PrefabCommandHandler.cs` - Prefab operations
- `ScriptableObjectCommandHandler.cs` - ScriptableObject management
- `VectorSpriteConvertHandler.cs` - Sprite generation

**MidLevel/** - Batch operations and presets:
- `TransformBatchHandler.cs`, `RectTransformBatchHandler.cs` - Transform batching
- `PhysicsBundleHandler.cs` - Physics2D/3D setup
- `UIFoundationHandler.cs`, `UIHierarchyHandler.cs`, `UIStateHandler.cs`, `UINavigationHandler.cs` - UI systems
- `CameraRigHandler.cs` - Camera presets
- `AudioSourceBundleHandler.cs` - Audio setup
- Animation, Sprite, Material, Light, Particle bundle handlers

**HighLevel/** - Analysis and graph tools:
- `SceneReferenceGraphHandler.cs` - Analyzes references between GameObjects in scene
- `ClassDependencyGraphHandler.cs` - Analyzes class dependencies in C# scripts
- `SceneRelationshipGraphHandler.cs` - Comprehensive scene relationship analysis

**Utility/** - Helper tools:
- `PingHandler.cs` - Bridge connectivity check
- `CompilationAwaitHandler.cs` - Compilation monitoring
- `ConsoleLogHandler.cs` - Console log retrieval
- `PlayModeControlHandler.cs` - Play mode control
- `EventWiringHandler.cs` - UnityEvent wiring

**GameKit/** - Game systems (3-Pillar Architecture):
- UI Pillar: `GameKitUIBindingHandler.cs`, `GameKitUIListHandler.cs`, `GameKitUISlotHandler.cs`, `GameKitUISelectionHandler.cs`, `GameKitUICommandHandler.cs`
- Logic Pillar: `GameKitActorHandler.cs`, `GameKitCombatHandler.cs`, `GameKitHealthHandler.cs`, `GameKitManagerHandler.cs`, `GameKitAIHandler.cs`, `GameKitTimerHandler.cs`, `GameKitSpawnerHandler.cs`
- Presentation Pillar: `GameKitFeedbackHandler.cs`, `GameKitVFXHandler.cs`, `GameKitAudioHandler.cs`, `GameKitProjectileHandler.cs`, `GameKitEffectHandler.cs`
- Economy: `GameKitMachinationsHandler.cs`, `GameKitInventoryHandler.cs`
- Flow: `GameKitSceneFlowHandler.cs`, `GameKitInteractionHandler.cs`, `GameKitDialogueHandler.cs`

### MCP Server (Python)

Located in `Assets/UnityAIForge/MCPServer/src/`:

- `main.py` - Entry point
- `tools/register_tools.py` - MCP tool registration and dispatch (~240 lines). Handles 4 special tools (ping, compilation_await, asset_crud, batch_sequential) and delegates remaining 59 tools via dict lookup.
- `tools/tool_registry.py` - Single source of truth for 63 MCP tool name → bridge name mappings. Used by both `register_tools.py` and `batch_sequential.py`.
- `tools/tool_definitions.py` - All 64 `types.Tool` definitions with descriptions and schema references.
- `tools/batch_sequential.py` - Sequential command execution with resume capability
- `tools/schemas/` - JSON Schema definitions split into 8 category files:
  - `common.py` - Shared type helpers (Vector3, Color, etc.)
  - `utility.py`, `low_level.py`, `mid_level.py`, `visual.py`
  - `gamekit_core.py`, `gamekit_systems.py`, `gamekit_pillar.py`, `graph.py`
- `bridge/` - WebSocket client to Unity Bridge
- `server/` - MCP server implementation

### GameKit Runtime Components (3-Pillar Architecture)

GameKit uses a 3-pillar architecture separating UI, Logic, and Presentation concerns:

**UI Pillar** (`Assets/UnityAIForge/GameKit/Runtime/UI/`):
- `GameKitUIBinding.cs` - Declarative UI data binding to game state
- `GameKitUIList.cs` - Dynamic list/grid for collections
- `GameKitUISlot.cs` - Item slots for inventory/equipment
- `GameKitUISelection.cs` - Selection groups (radio, toggle, tabs)
- `GameKitUICommand.cs` - UI button to command binding

**Logic Pillar** (`Assets/UnityAIForge/GameKit/Runtime/Logic/`):
- `GameKitActor.cs` - Character controller with 8 movement profiles
- `GameKitCombat.cs` - Unified damage calculation and attack system
- `GameKitHealth.cs` - HP/damage system with UnityEvents
- `GameKitManager.cs` - Central game management
- `GameKitSpawner.cs`, `GameKitTimer.cs`, `GameKitAIBehavior.cs`
- `GameKitMachinationsAsset.cs` - Economic system
- `GameKitInventory.cs`, `GameKitInteraction.cs`, `GameKitDialogueManager.cs`

**Presentation Pillar** (`Assets/UnityAIForge/GameKit/Runtime/Presentation/`):
- `GameKitFeedback.cs` - Game feel effects (hitstop, screen shake, flash)
- `GameKitVFX.cs` - Visual effects wrapper with pooling
- `GameKitAudio.cs` - Audio wrapper with fade controls
- `GameKitProjectile.cs`, `GameKitWaypoint.cs`, `GameKitEffectManager.cs`

**Supporting Modules**:
- `Managers/` - GameKitResourceManager, StateManager, TurnManager, EventManager
- `Movement/` - GraphNodeMovement, SplineMovement, TileGridMovement
- `SceneFlow/` - GameKitSceneFlow and state classes
- `Input/` - GameKitInputSystemController, GameKitSimpleInput

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

**HandlerUtilities.cs** - Common utilities for command handlers (GameObject finding, component operations).

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
