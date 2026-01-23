# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity-AI-Forge is an AI-powered Unity development toolkit that integrates with the Model Context Protocol (MCP). It provides 50+ tools for AI-driven game development, including a GameKit framework with 3-pillar architecture (UI, Logic, Presentation) for rapid prototyping.

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
- `tools/register_tools.py` - MCP tool definitions (maps to C# handlers)
- `tools/batch_sequential.py` - Sequential command execution with resume capability
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

1. Add tool definition in `Assets/UnityAIForge/MCPServer/src/tools/register_tools.py`
2. Create or extend a handler in `Assets/UnityAIForge/Editor/MCPBridge/Handlers/`
3. Register the handler in `Assets/UnityAIForge/Editor/MCPBridge/Base/CommandHandlerInitializer.cs`

### Component Reference Formats

ComponentCommandHandler supports multiple reference formats:
- Path: `"Player/Weapon"`
- Name: `"Enemy"`
- Pattern with wildcard: `"Enemy*"`
- Explicit type: `"type:Enemy"` or `"name:Enemy"`

### Wildcard Matching

`McpWildcardUtility.cs` provides pattern matching for batch operations using `*` and `?` wildcards.
