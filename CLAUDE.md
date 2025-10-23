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
5. **Unity Objects**: Asset path string → `AssetDatabase.LoadAssetAtPath()`
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
