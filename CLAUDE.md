# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity-AI-Forge is an AI-powered Unity development toolkit that integrates with the Model Context Protocol (MCP). It provides 24+ tools for AI-driven game development, including a GameKit framework for rapid prototyping.

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

Located in `Assets/UnityAIForge/Editor/MCPBridge/Handlers/`, each handler processes specific MCP tool commands:

- `SceneCommandHandler.cs` - Scene management (create, load, save)
- `GameObjectCommandHandler.cs` - GameObject operations (create, find, modify, delete)
- `ComponentCommandHandler.cs` - Component manipulation (add, configure, remove)
- `AssetCommandHandler.cs` - Asset management
- `PrefabCommandHandler.cs` - Prefab operations
- `PhysicsBundleHandler.cs` - Physics2D/3D setup
- `TransformBatchHandler.cs` - Batch transform operations
- `UIFoundationHandler.cs` - UI element creation

GameKit-specific handlers in `Handlers/GameKit/`:
- `GameKitActorHandler.cs` - Actor component setup with movement profiles
- `GameKitMachinationsHandler.cs` - Economic system configuration
- `GameKitSceneFlowHandler.cs` - State machine transitions
- `GameKitUICommandHandler.cs` - UI-to-logic binding
- `GameKitManagerHandler.cs` - Manager component setup
- `GameKitInteractionHandler.cs` - Interaction system configuration

### MCP Server (Python)

Located in `Assets/UnityAIForge/MCPServer/src/`:

- `main.py` - Entry point
- `tools/register_tools.py` - MCP tool definitions (maps to C# handlers)
- `tools/batch_sequential.py` - Sequential command execution with resume capability
- `bridge/` - WebSocket client to Unity Bridge
- `server/` - MCP server implementation

### GameKit Runtime Components

Located in `Assets/UnityAIForge/GameKit/Runtime/`:

- `GameKitActor.cs` - Character controller with 8 movement profile types
- `GameKitManager.cs` - Central game management
- `GameKitResourceManager.cs` - Resource pool management
- `GameKitStateManager.cs` - Game state tracking
- `GameKitTurnManager.cs` - Turn-based game support
- `GameKitSceneFlow.cs` - State machine scene transitions
- `GameKitUICommand.cs` - UI button to command binding
- `GameKitMachinationsAsset.cs` - Economic system ScriptableObject
- `GameKitInteraction.cs` - Interaction system

## Key Patterns

### Adding New MCP Tools

1. Add tool definition in `Assets/UnityAIForge/MCPServer/src/tools/register_tools.py`
2. Create or extend a handler in `Assets/UnityAIForge/Editor/MCPBridge/Handlers/`
3. Register the handler in `McpCommandProcessor.cs`

### Component Reference Formats

ComponentCommandHandler supports multiple reference formats:
- Path: `"Player/Weapon"`
- Name: `"Enemy"`
- Pattern with wildcard: `"Enemy*"`
- Explicit type: `"type:Enemy"` or `"name:Enemy"`

### Wildcard Matching

`McpWildcardUtility.cs` provides pattern matching for batch operations using `*` and `?` wildcards.
