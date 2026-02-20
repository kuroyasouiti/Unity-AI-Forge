# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity-AI-Forge is an AI-powered Unity development toolkit that integrates with the Model Context Protocol (MCP). It provides 49 tools for AI-driven game development, including a GameKit framework with 3-pillar architecture (UI, Logic, Presentation). GameKit uses code generation to produce standalone C# scripts from templates, so user projects have zero runtime dependency on Unity-AI-Forge.

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

The project uses Unity's Assembly Definition system with two main assemblies:

- **UnityAIForge.Editor** (`Assets/UnityAIForge/Editor/`) - Editor-only code containing the MCP Bridge, command handlers, and code generation infrastructure. Namespace: `MCP.Editor`
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

**HighLevel/** - Analysis and integrity tools (registered as GameKit Logic Pillar):
- `SceneIntegrityHandler.cs` - Scene integrity validation
- `ClassCatalogHandler.cs` - Type enumeration and inspection
- `ClassDependencyGraphHandler.cs` - Analyzes class dependencies in C# scripts
- `SceneReferenceGraphHandler.cs` - Analyzes references between GameObjects in scene
- `SceneRelationshipGraphHandler.cs` - Comprehensive scene relationship analysis

**Utility/** - Helper tools:
- `PingHandler.cs` - Bridge connectivity check
- `CompilationAwaitHandler.cs` - Compilation monitoring
- `ConsoleLogHandler.cs` - Console log retrieval
- `PlayModeControlHandler.cs` - Play mode control
- `EventWiringHandler.cs` - UnityEvent wiring

**GameKit/** - Game systems (3-Pillar Architecture, code generation):
- UI Pillar: `GameKitUICommandHandler.cs`, `GameKitUIBindingHandler.cs`, `GameKitUIListHandler.cs`, `GameKitUISlotHandler.cs`, `GameKitUISelectionHandler.cs`
- Presentation Pillar: `GameKitAnimationSyncHandler.cs`, `GameKitEffectHandler.cs`, `GameKitFeedbackHandler.cs`, `GameKitVFXHandler.cs`, `GameKitAudioHandler.cs`

### MCP Server (Python)

Located in `Assets/UnityAIForge/MCPServer/src/`:

- `main.py` - Entry point
- `tools/register_tools.py` - MCP tool registration and dispatch (~240 lines). Handles 4 special tools (ping, compilation_await, asset_crud, batch_sequential) and delegates remaining 43 tools via dict lookup.
- `tools/tool_registry.py` - Single source of truth for 47 MCP tool name → bridge name mappings. Used by both `register_tools.py` and `batch_sequential.py`.
- `tools/tool_definitions.py` - All 48 `types.Tool` definitions with descriptions and schema references.
- `tools/batch_sequential.py` - Sequential command execution with resume capability
- `tools/schemas/` - JSON Schema definitions split into 8 category files:
  - `common.py` - Shared type helpers (Vector3, Color, etc.)
  - `utility.py`, `low_level.py`, `mid_level.py`, `visual.py`
  - `gamekit_core.py`, `gamekit_systems.py`, `gamekit_pillar.py`, `graph.py`
- `bridge/` - WebSocket client to Unity Bridge
- `server/` - MCP server implementation

### Code Generation Architecture

GameKit handlers generate standalone C# scripts from templates instead of using runtime MonoBehaviours. Generated scripts have zero dependency on the Unity-AI-Forge package.

- **Templates**: `Assets/UnityAIForge/Editor/CodeGen/Templates/*.cs.txt`
- **Infrastructure**: `Assets/UnityAIForge/Editor/CodeGen/` (ScriptGenerator, TemplateRenderer, GeneratedScriptTracker, CodeGenHelper)
- **Output**: Generated scripts are placed in the user's `Assets/` folder
- **Entry point**: `CodeGenHelper.GenerateAndAttach(go, templateName, componentId, className, variables, outputDir)`
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
