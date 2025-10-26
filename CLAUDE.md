# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**UnityMCP** is a Model Context Protocol (MCP) server that enables AI assistants to interact with Unity Editor in real-time. It consists of two main components:

1. **Unity C# Bridge** (`Assets/Editor/MCPBridge/`) - WebSocket server running inside Unity Editor
2. **Python MCP Server** (`Assets/Runtime/MCPServer/`) - MCP protocol implementation that connects to the bridge

## Architecture

The system uses a **bidirectional WebSocket bridge** architecture:

```
AI Client (Claude Code/Cursor) <--(MCP)--> Python Server <--(WebSocket)--> Unity Editor Bridge
```

### Communication Flow

1. **Python → Unity**: MCP tools send commands via `bridge_manager.send_command()` which serializes to WebSocket messages
2. **Unity → Python**: Unity sends context updates, heartbeats, and command results back through the same WebSocket
3. **Context Updates**: Unity automatically pushes scene/hierarchy/selection state every 5 seconds (configurable)
4. **Heartbeat**: 10-second intervals to detect connection loss

### Key Components

| Component | Location | Purpose |
|-----------|----------|---------|
| **McpBridgeService.cs** | `Assets/Editor/MCPBridge/` | WebSocket listener with custom HTTP handshake, manages client lifecycle |
| **McpCommandProcessor.cs** | `Assets/Editor/MCPBridge/` | Executes tool commands (scene/GameObject/component/asset operations) |
| **McpContextCollector.cs** | `Assets/Editor/MCPBridge/` | Gathers Unity state (hierarchy, selection, assets, Git status) |
| **bridge_manager.py** | `Assets/Runtime/MCPServer/bridge/` | Python-side WebSocket client, command routing with timeout handling |
| **register_tools.py** | `Assets/Runtime/MCPServer/tools/` | MCP tool definitions and schemas |
| **main.py** | `Assets/Runtime/MCPServer/` | Server entrypoint with stdio/websocket transport modes |

## Development Commands

### Running the Python MCP Server

**From the project root:**
```bash
# Using uv (recommended)
uv run --directory Assets/Runtime/MCPServer main.py

# Using Python directly
cd Assets/Runtime/MCPServer
python main.py --transport stdio  # For MCP clients
python main.py --transport websocket  # For HTTP/WebSocket mode
```

**Environment Variables:**
- `MCP_SERVER_TRANSPORT` - Transport mode (stdio/websocket)
- `MCP_SERVER_HOST` - WebSocket server host (default: 127.0.0.1)
- `MCP_SERVER_PORT` - WebSocket server port (default: 7070)
- `MCP_BRIDGE_TOKEN` - Optional authentication token
- `MCP_LOG_LEVEL` - Log level (trace/debug/info/warn/error)

### Testing in Unity

1. Open Unity Editor
2. Go to **Tools > MCP Assistant**
3. Click **Start Bridge**
4. Run the Python server (see above)
5. Connection status appears in the MCP Assistant window

## Code Structure

### Unity C# Bridge

**WebSocket Implementation (`McpBridgeService.cs`)**
- Custom HTTP handshake parser (lines 385-485) - does not use ASP.NET
- Accepts WebSocket upgrade on path `/bridge`
- Handles compilation/assembly reload by saving connection state to EditorPrefs and reconnecting (lines 951-981)
- Thread-safe message queuing between async WebSocket tasks and Unity's main thread

**Command Execution (`McpCommandProcessor.cs`)**
- Uses reflection to set component properties from dictionaries
- Supports Unity Object references via special formats:
  - Asset paths: `"Assets/Models/Sphere.fbx"`
  - Built-in resources: `"Library/unity default resources::Sphere"`
  - Object reference dictionaries: `{"_ref": "GameObject", "path": "..."}`

**Context Collection (`McpContextCollector.cs`)**
- Serializes scene hierarchy to JSON with component lists
- Captures Selection.objects and converts to paths
- Scans Assets/ directory for file listing
- Attempts to read Git branch/status if .git exists

### Python MCP Server

**Bridge Manager (`bridge_manager.py`)**
- Manages pending commands with timeout using asyncio.TimerHandle
- Event emitter pattern for "connected", "disconnected", "contextUpdated"
- Automatic reconnection is NOT handled by bridge_manager (Unity bridge handles this)

**Tool Registration (`register_tools.py`)**
- All tools follow pattern: validate connection → send_command() → format response
- Schemas use JSON Schema with strict `additionalProperties: false`
- Tool names use dot notation: `unity.scene.crud`, `unity.gameobject.crud`, etc.

**Message Protocol (`bridge/messages.py`)**
- Message types: `hello`, `heartbeat`, `context:update`, `command:execute`, `command:result`
- All messages are JSON with required `type` field
- Commands use UUIDs for correlation between request/response

## Important Implementation Details

### Unity-Python Type Mapping

When setting component properties through `componentManage`:

1. **Primitives**: int, float, bool, string → direct assignment
2. **Vectors**: `{"x": 1, "y": 2, "z": 3}` → Vector3/Vector2
3. **Colors**: `{"r": 1, "g": 0, "b": 0, "a": 1}` → Color
4. **Enums**: String name → enum value via `Enum.Parse()`
5. **Unity Objects**: Multiple formats supported (GUID takes priority when both provided):
   - Asset reference with GUID (recommended): `{"_ref": "asset", "guid": "abc123..."}`
   - Asset reference with path: `{"_ref": "asset", "path": "Assets/Models/Sphere.fbx"}`
   - Direct asset path string: `"Assets/Models/Sphere.fbx"`
   - Built-in resources: `"Library/unity default resources::Sphere"`
6. **Nested Objects**: Recursively processes dictionaries with `_ref` key

### Reconnection Strategy

**After Compilation/Reload:**
- Unity: Saves connection state to `EditorPrefs` before compilation
- Unity: Static constructor reads EditorPrefs and calls `Connect()` on domain reload
- Python: Connection survives because the Python process is external

**After Connection Loss:**
- Unity: Detects WebSocket close, transitions to "Connecting" state, waits for new client
- Python: bridge_manager throws RuntimeError on send_command() when disconnected

### Tool Command Pattern

Every tool command follows this structure:

```python
# Python side
async def call_tool(name: str, arguments: dict):
    _ensure_bridge_connected()  # Raises if not connected
    response = await bridge_manager.send_command("toolName", arguments)
    return format_response(response)
```

```csharp
// Unity side
public static object Execute(McpIncomingCommand command)
{
    return command.ToolName switch
    {
        "toolName" => HandleTool(command.Payload),
        // ...
    };
}

private static object HandleTool(Dictionary<string, object> payload)
{
    // Extract parameters
    // Perform Unity operation
    // Return result dictionary
}
```

### Component Management Examples

**Setting Component Properties with Asset References:**

1. **Using GUID (Recommended):**
   ```json
   {
     "operation": "update",
     "gameObjectPath": "Player",
     "componentType": "UnityEngine.MeshFilter",
     "propertyChanges": {
       "mesh": {
         "_ref": "asset",
         "guid": "abc123def456789"
       }
     }
   }
   ```

2. **Using Asset Path:**
   ```json
   {
     "operation": "update",
     "gameObjectPath": "Player",
     "componentType": "UnityEngine.MeshRenderer",
     "propertyChanges": {
       "material": {
         "_ref": "asset",
         "path": "Assets/Materials/PlayerMaterial.mat"
       }
     }
   }
   ```

3. **Using Direct Path String:**
   ```json
   {
     "operation": "update",
     "gameObjectPath": "Enemy",
     "componentType": "UnityEngine.MeshFilter",
     "propertyChanges": {
       "sharedMesh": "Assets/Models/Enemy.fbx"
     }
   }
   ```

4. **Using Built-in Resources:**
   ```json
   {
     "operation": "update",
     "gameObjectPath": "Sphere",
     "componentType": "UnityEngine.MeshFilter",
     "propertyChanges": {
       "sharedMesh": "Library/unity default resources::Sphere"
     }
   }
   ```

5. **Mixed Properties (Primitives and Assets):**
   ```json
   {
     "operation": "update",
     "gameObjectPath": "Character",
     "componentType": "UnityEngine.SkinnedMeshRenderer",
     "propertyChanges": {
       "sharedMesh": {
         "_ref": "asset",
         "guid": "xyz789abc123"
       },
       "quality": "High",
       "receiveShadows": true,
       "updateWhenOffscreen": false
     }
   }
   ```

**Note:** When both GUID and path are provided in an asset reference, GUID takes priority for resolution.

## Adding New Tools

1. **Define schema in `register_tools.py`:**
   ```python
   my_tool_schema = _schema_with_required(
       {
           "type": "object",
           "properties": {
               "operation": {"type": "string", "enum": ["foo", "bar"]},
               # ...
           }
       },
       ["operation"]  # required fields
   )
   ```

2. **Add tool to `tool_definitions` list:**
   ```python
   types.Tool(
       name="unity.my.tool",
       description="Does something with Unity",
       inputSchema=my_tool_schema
   )
   ```

3. **Add handler in `call_tool()` function:**
   ```python
   if name == "unity.my.tool":
       return await _call_bridge_tool("myTool", args)
   ```

4. **Implement in `McpCommandProcessor.cs`:**
   ```csharp
   public static object Execute(McpIncomingCommand command)
   {
       return command.ToolName switch
       {
           "myTool" => HandleMyTool(command.Payload),
           // ...
       };
   }

   private static object HandleMyTool(Dictionary<string, object> payload)
   {
       // Implementation
       return new Dictionary<string, object> { ["result"] = "success" };
   }
   ```

## Available Tools

### Batch Execute (`unity.batch.execute`)

The batch execute tool allows you to execute multiple Unity operations in a single request, improving efficiency for bulk operations.

**Usage:**

```json
{
  "operations": [
    {
      "tool": "gameObjectManage",
      "payload": {
        "operation": "create",
        "gameObjectPath": "Player",
        "name": "Player"
      }
    },
    {
      "tool": "componentManage",
      "payload": {
        "operation": "add",
        "gameObjectPath": "Player",
        "componentType": "UnityEngine.Rigidbody"
      }
    },
    {
      "tool": "componentManage",
      "payload": {
        "operation": "update",
        "gameObjectPath": "Player",
        "componentType": "UnityEngine.Rigidbody",
        "propertyChanges": {
          "mass": 2.0,
          "useGravity": true
        }
      }
    }
  ],
  "stopOnError": false
}
```

**Parameters:**

- **operations** (required): Array of operations to execute
  - **tool**: Tool name (e.g., "gameObjectManage", "componentManage", "assetManage")
  - **payload**: Operation-specific parameters
- **stopOnError** (optional): If true, stops execution when an operation fails. Default is false.

**Response:**

```json
{
  "totalOperations": 3,
  "executedOperations": 3,
  "successCount": 3,
  "failureCount": 0,
  "results": [
    {
      "index": 0,
      "success": true,
      "tool": "gameObjectManage",
      "result": { ... }
    },
    {
      "index": 1,
      "success": true,
      "tool": "componentManage",
      "result": { ... }
    },
    {
      "index": 2,
      "success": true,
      "tool": "componentManage",
      "result": { ... }
    }
  ]
}
```

**Use Cases:**

1. **Setup Multiple GameObjects:**
   ```json
   {
     "operations": [
       {"tool": "gameObjectManage", "payload": {"operation": "create", "gameObjectPath": "UI/Panel", "name": "Panel"}},
       {"tool": "gameObjectManage", "payload": {"operation": "create", "gameObjectPath": "UI/Panel/Button", "name": "Button"}},
       {"tool": "componentManage", "payload": {"operation": "add", "gameObjectPath": "UI/Panel/Button", "componentType": "UnityEngine.UI.Button"}}
     ]
   }
   ```

2. **Bulk Asset Creation:**
   ```json
   {
     "operations": [
       {"tool": "assetManage", "payload": {"operation": "create", "assetPath": "Assets/Scripts/Player.cs", "contents": "..."}},
       {"tool": "assetManage", "payload": {"operation": "create", "assetPath": "Assets/Scripts/Enemy.cs", "contents": "..."}},
       {"tool": "assetManage", "payload": {"operation": "create", "assetPath": "Assets/Scripts/GameManager.cs", "contents": "..."}}
     ]
   }
   ```

3. **Input System Setup:**
   ```json
   {
     "operations": [
       {"tool": "inputSystemManage", "payload": {"operation": "createAsset", "assetPath": "Assets/Input/Controls.inputactions"}},
       {"tool": "inputSystemManage", "payload": {"operation": "addActionMap", "assetPath": "Assets/Input/Controls.inputactions", "mapName": "Player"}},
       {"tool": "inputSystemManage", "payload": {"operation": "addAction", "assetPath": "Assets/Input/Controls.inputactions", "mapName": "Player", "actionName": "Move"}},
       {"tool": "inputSystemManage", "payload": {"operation": "addBinding", "assetPath": "Assets/Input/Controls.inputactions", "mapName": "Player", "actionName": "Move", "path": "<Keyboard>/wasd"}}
     ]
   }
   ```

**Implementation Notes:**
- Operations are executed sequentially in the order provided
- Each operation result is recorded independently
- If `stopOnError` is false (default), all operations are attempted even if some fail
- If `stopOnError` is true, execution stops at the first error
- The response includes detailed results for each operation with success/failure status
- Failed operations include error messages

### Project Settings Management (`unity.projectSettings.crud`)

The project settings management tool provides comprehensive operations for reading and writing Unity Project Settings across multiple categories.

**Operations:**

1. **list** - List available settings categories or properties
   ```json
   {
     "operation": "list"
   }
   ```
   Returns all available categories: player, quality, time, physics, audio, editor.

   ```json
   {
     "operation": "list",
     "category": "player"
   }
   ```
   Returns all available properties for the specified category.

2. **read** - Read project settings values
   ```json
   {
     "operation": "read",
     "category": "player",
     "property": "companyName"
   }
   ```
   Returns the value of a specific property.

   ```json
   {
     "operation": "read",
     "category": "player"
   }
   ```
   Returns all properties for the specified category (when property is omitted).

3. **write** - Write project settings values
   ```json
   {
     "operation": "write",
     "category": "player",
     "property": "companyName",
     "value": "My Company"
   }
   ```

**Supported Categories:**

- **player** - PlayerSettings (company name, product name, version, screen resolution, etc.)
  - Properties: companyName, productName, version, defaultScreenWidth, defaultScreenHeight, runInBackground, defaultIsFullScreen, etc.

- **quality** - QualitySettings (quality levels, shadows, anti-aliasing, etc.)
  - Properties: names, currentLevel, pixelLightCount, shadowDistance, vSyncCount, antiAliasing, etc.

- **time** - Time settings (fixed delta time, time scale, etc.)
  - Properties: fixedDeltaTime, maximumDeltaTime, timeScale, maximumParticleDeltaTime

- **physics** - Physics settings (gravity, collision detection, etc.)
  - Properties: gravity (Vector3), defaultSolverIterations, bounceThreshold, queriesHitTriggers, etc.

- **audio** - AudioSettings (DSP buffer size, sample rate, etc.)
  - Properties: dspBufferSize, sampleRate, speakerMode, numRealVoices, numVirtualVoices

- **editor** - EditorSettings (serialization mode, line endings, etc.)
  - Properties: serializationMode, spritePackerMode, lineEndingsForNewScripts, defaultBehaviorMode

**Implementation Notes:**
- The tool uses Unity's PlayerSettings, QualitySettings, Time, Physics, AudioSettings, and EditorSettings APIs
- All property names are case-insensitive for convenience
- Complex values like Vector3 (gravity) are represented as dictionaries with x, y, z keys
- Enum values are returned as strings and can be set using string names

### Render Pipeline Management (`unity.renderPipeline.manage`)

The render pipeline management tool provides operations for inspecting and configuring Unity's render pipeline (Built-in, URP, HDRP, or custom).

**Operations:**

1. **inspect** - Check current render pipeline
   ```json
   {
     "operation": "inspect"
   }
   ```
   Returns pipeline type (Built-in/URP/HDRP/Custom), asset name, and path.

2. **setAsset** - Change the render pipeline asset
   ```json
   {
     "operation": "setAsset",
     "assetPath": "Assets/Settings/UniversalRP-HighQuality.asset"
   }
   ```
   Empty assetPath clears the pipeline and returns to Built-in renderer.

3. **getSettings** - Read render pipeline settings
   ```json
   {
     "operation": "getSettings"
   }
   ```
   Returns all public properties of the current pipeline asset.

4. **updateSettings** - Modify render pipeline settings
   ```json
   {
     "operation": "updateSettings",
     "settings": {
       "shadowDistance": 150,
       "cascadeCount": 4
     }
   }
   ```

**Implementation Notes:**
- Uses UnityEngine.Rendering.GraphicsSettings API
- Settings are pipeline-specific (URP and HDRP have different properties)
- Uses reflection to access pipeline-specific settings
- All changes are saved to the asset automatically

### Input System Management (`unity.inputSystem.manage`)

The input system management tool provides operations for working with Unity's New Input System (requires Input System package).

**Operations:**

1. **listActions** - List all Input Action assets
   ```json
   {
     "operation": "listActions"
   }
   ```
   Returns all .inputactions files in the project.

2. **createAsset** - Create a new Input Action asset
   ```json
   {
     "operation": "createAsset",
     "assetPath": "Assets/Input/PlayerControls.inputactions"
   }
   ```

3. **addActionMap** - Add an action map to an asset
   ```json
   {
     "operation": "addActionMap",
     "assetPath": "Assets/Input/PlayerControls.inputactions",
     "mapName": "Player"
   }
   ```

4. **addAction** - Add an action to a map
   ```json
   {
     "operation": "addAction",
     "assetPath": "Assets/Input/PlayerControls.inputactions",
     "mapName": "Player",
     "actionName": "Jump",
     "actionType": "Button"
   }
   ```
   Action types: Button, Value, PassThrough

5. **addBinding** - Add a binding to an action
   ```json
   {
     "operation": "addBinding",
     "assetPath": "Assets/Input/PlayerControls.inputactions",
     "mapName": "Player",
     "actionName": "Jump",
     "path": "<Keyboard>/space"
   }
   ```
   Common binding paths:
   - Keyboard: `<Keyboard>/space`, `<Keyboard>/w`, `<Keyboard>/escape`
   - Mouse: `<Mouse>/leftButton`, `<Mouse>/position`, `<Mouse>/delta`
   - Gamepad: `<Gamepad>/buttonSouth`, `<Gamepad>/leftStick`

6. **inspectAsset** - Inspect Input Action asset contents
   ```json
   {
     "operation": "inspectAsset",
     "assetPath": "Assets/Input/PlayerControls.inputactions"
   }
   ```
   Returns action maps and their actions.

7. **deleteAsset** - Delete entire Input Action asset
   ```json
   {
     "operation": "deleteAsset",
     "assetPath": "Assets/Input/PlayerControls.inputactions"
   }
   ```
   Permanently deletes the .inputactions file.

8. **deleteActionMap** - Delete an action map from asset
   ```json
   {
     "operation": "deleteActionMap",
     "assetPath": "Assets/Input/PlayerControls.inputactions",
     "mapName": "Player"
   }
   ```

9. **deleteAction** - Delete an action from a map
   ```json
   {
     "operation": "deleteAction",
     "assetPath": "Assets/Input/PlayerControls.inputactions",
     "mapName": "Player",
     "actionName": "Jump"
   }
   ```

10. **deleteBinding** - Delete binding(s) from an action
    ```json
    {
      "operation": "deleteBinding",
      "assetPath": "Assets/Input/PlayerControls.inputactions",
      "mapName": "Player",
      "actionName": "Jump",
      "bindingIndex": 0
    }
    ```
    - Omit `bindingIndex` or set to -1 to delete ALL bindings from the action
    - Specify `bindingIndex` (0, 1, 2, ...) to delete a specific binding

**Implementation Notes:**
- Uses reflection to access Input System API (UnityEngine.InputSystem.InputActionAsset)
- Requires Input System package to be installed via Package Manager
- All changes are automatically saved to the asset
- The tool creates .inputactions files which can be used in runtime code
- Delete operations are permanent and cannot be undone

### Prefab Management (`unity.prefab.crud`)

The prefab management tool provides comprehensive operations for working with Unity prefabs:

**Operations:**

1. **create** - Create a new prefab from a scene GameObject
   ```json
   {
     "operation": "create",
     "gameObjectPath": "MyObject",
     "prefabPath": "Assets/Prefabs/MyPrefab.prefab"
   }
   ```

2. **update** - Update an existing prefab from a modified instance
   ```json
   {
     "operation": "update",
     "gameObjectPath": "MyPrefabInstance",
     "prefabPath": "Assets/Prefabs/MyPrefab.prefab"
   }
   ```

3. **inspect** - Get information about a prefab asset
   ```json
   {
     "operation": "inspect",
     "prefabPath": "Assets/Prefabs/MyPrefab.prefab"
   }
   ```

4. **instantiate** - Create a prefab instance in the scene
   ```json
   {
     "operation": "instantiate",
     "prefabPath": "Assets/Prefabs/MyPrefab.prefab",
     "parentPath": "Canvas"
   }
   ```

5. **unpack** - Unpack a prefab instance to regular GameObjects
   ```json
   {
     "operation": "unpack",
     "gameObjectPath": "MyPrefabInstance",
     "unpackMode": "OutermostRoot"
   }
   ```
   - `unpackMode`: "OutermostRoot" (default) or "Completely"

6. **applyOverrides** - Apply instance modifications back to the prefab
   ```json
   {
     "operation": "applyOverrides",
     "gameObjectPath": "MyPrefabInstance"
   }
   ```

7. **revertOverrides** - Revert instance to match the prefab
   ```json
   {
     "operation": "revertOverrides",
     "gameObjectPath": "MyPrefabInstance"
   }
   ```

**Implementation Notes:**
- The `create` operation uses `PrefabUtility.SaveAsPrefabAsset()`
- The `update` operation requires a prefab instance and saves changes to the source prefab
- The `instantiate` operation uses `PrefabUtility.InstantiatePrefab()` to maintain prefab connection
- All operations handle AssetDatabase refresh automatically

## Testing

**Manual Testing Checklist:**
1. Start Unity bridge in Tools > MCP Assistant
2. Run Python server with `--transport stdio`
3. Connect from Claude Code/Cursor
4. Verify ping tool returns Unity version
5. Create/modify scene elements
6. Trigger compilation and verify reconnection
7. Check context updates appear in logs
8. Test prefab operations (create, instantiate, apply/revert overrides)

**Unit Tests:**
- C# tests: `Assets/Editor/MCPBridge/Tests/McpCommandProcessorTests.cs`
- Python tests: `Assets/Runtime/tests/test_*.py`

## Common Pitfalls

1. **Thread Safety**: Unity API calls must happen on main thread. Use `lock (MainThreadActions)` queue pattern in McpBridgeService.
2. **Asset Paths**: Always use forward slashes and "Assets/" prefix for asset paths.
3. **Component Type Names**: Must be fully qualified (e.g., `UnityEngine.UI.Text`, not just `Text`).
4. **JSON Serialization**: MiniJson.cs is used on C# side (not Newtonsoft). It only supports basic types + List/Dictionary.
5. **WebSocket Protocol**: Unity's WebSocket implementation requires manual HTTP handshake (lines 385-591 in McpBridgeService.cs).
6. **EditorPrefs Cleanup**: Remember to delete EditorPrefs keys after using them to avoid state pollution.
7. **Prefab Paths**: Prefab files must have `.prefab` extension and be under `Assets/` directory.
8. **Prefab Instances**: Operations like `update`, `applyOverrides`, and `revertOverrides` require the GameObject to be a prefab instance (checked via `PrefabUtility.IsPartOfPrefabInstance()`).

## Configuration Files

- **Unity Settings**: `ProjectSettings/McpBridgeSettings.asset` (gitignore recommended if using bridge tokens)
- **Python Dependencies**: Managed by `uv` (no requirements.txt/pyproject.toml in repo)
- **MCP Client Config**: Auto-registered via ServerInstallerUtility.cs

## Important Notes

- This is a **Unity Editor tool** - it does not work in builds/runtime
- The bridge listens on localhost by default for security
- Setting host to `*` or `0.0.0.0` exposes the bridge to the network (use with caution)
- Bridge token authentication is available but stored in plain text
- The Python server supports both stdio (for MCP clients) and websocket (for HTTP testing)
