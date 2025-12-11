# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> 💡 **NEW TO SKILLFORUNITY?** Check out the [Quick Start Guide](QUICKSTART.md) for a fast introduction with common commands and examples!

## 🎯 For Claude: How to Use This System Effectively

**You are working with SkillForUnity - a powerful system that lets you control Unity Editor directly!**

### What You Can Do

- ✅ Create complete UI systems with single commands
- ✅ Build 3D game scenes instantly
- ✅ Manage GameObjects, components, and assets
- ✅ Set up entire scene hierarchies declaratively
- ✅ Inspect and understand Unity project structure

### Quick Command Reference

**Most Common Commands:**
```python
# Set up a new scene
unity_scene_quickSetup({"setupType": "UI"})  # or "3D", "2D"

# Create UI elements
unity_ugui_createFromTemplate({"template": "Button", "text": "Click Me!"})

# Create GameObjects
unity_gameobject_createFromTemplate({"template": "Cube", "position": {"x": 0, "y": 1, "z": 0}})

# Build menu hierarchies with navigation and buttons
unity_menu_hierarchyCreate({"menuName": "MainMenu", "menuStructure": {"Play": "Start Game", "Quit": "Exit"}})

# Inspect current scene (returns one level of hierarchy)
unity_scene_crud({"operation": "inspect", "includeHierarchy": True, "includeComponents": False})

# Inspect specific GameObject (for deeper exploration)
unity_gameobject_crud({"operation": "inspect", "gameObjectPath": "Player"})

# Inspect component (fast - specific properties only)
unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Transform",
    "propertyFilter": ["position", "rotation"]
})

# Batch add components (with limit & error handling)
unity_component_crud({
    "operation": "addMultiple",
    "pattern": "Enemy*",
    "componentType": "UnityEngine.Rigidbody",
    "maxResults": 1000,
    "stopOnError": False
})
```

**See [QUICKSTART.md](QUICKSTART.md) for complete examples and workflows!**

### Important Guidelines

1. **Always use templates when available** - Much faster than manual creation
2. **Check context before making changes** - Use `unity_scene_crud` with `operation="inspect"`
3. **Use hierarchy builder for complex structures** - Create entire trees in one command
4. **Use script templates for quick generation** - Use `unity_script_template_generate()` for MonoBehaviour/ScriptableObject scaffolding
5. **Optimize inspect operations** - Use `includeProperties=false` for existence checks, `propertyFilter` for specific properties
6. **Limit batch operations** - Use `maxResults` parameter to prevent timeouts (default: 1000)
7. **Handle errors gracefully** - Use `stopOnError=false` in batch operations to continue on failures
8. **NEVER edit .meta files** - Unity manages these automatically; manual editing can break references and cause corruption
9. **Refer to the Quick Start guide** - Contains copy-paste examples for common tasks

## Project Overview

**SkillForUnity** is a Model Context Protocol (MCP) server that enables AI assistants to interact with Unity Editor in real-time. It consists of two main components:

1. **Unity C# Bridge** (`Assets/SkillForUnity/Editor/MCPBridge/`) - WebSocket server running inside Unity Editor
2. **Claude Skill (Python MCP Server)** (`SkillForUnity/src/`) - MCP protocol implementation that connects to the bridge

### Project Structure

The Claude Skill is located at the project root in `SkillForUnity/` directory. For Claude Code integration, a junction link exists at `.claude/skills/SkillForUnity` → `SkillForUnity/` to enable automatic skill recognition.

The Unity package bundles the Claude Skill archive at `Assets/SkillForUnity/SkillForUnity.zip`. When setting up on other machines, extract it to the project root as `SkillForUnity/`, or to `~/.claude/skills/SkillForUnity` for traditional setup.

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
| **McpBridgeService.cs** | `Assets/SkillForUnity/Editor/MCPBridge/` | WebSocket listener with custom HTTP handshake, manages client lifecycle |
| **McpCommandProcessor.cs** | `Assets/SkillForUnity/Editor/MCPBridge/` | Executes tool commands (scene/GameObject/component/asset operations) |
| **McpContextCollector.cs** | `Assets/SkillForUnity/Editor/MCPBridge/` | Gathers Unity state (hierarchy, selection, assets, Git status) |
| **bridge_manager.py** | `SkillForUnity/src/bridge/` | Python-side WebSocket client, command routing with timeout handling |
| **register_tools.py** | `SkillForUnity/src/tools/` | MCP tool definitions and schemas |
| **main.py** | `SkillForUnity/src/` | Server entrypoint with stdio/websocket transport modes |

## Development Commands

### Running the Python MCP Server

**From the project root:**
```bash
# Using uv (recommended)
uv run --directory SkillForUnity src/main.py

# Using Python directly
cd SkillForUnity
python src/main.py --transport stdio  # For MCP clients
python src/main.py --transport websocket  # For HTTP/WebSocket mode
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

**Using GlobalObjectId for GameObject Identification (Optional):**

All component operations support `gameObjectGlobalObjectId` as an alternative to `gameObjectPath`:

```json
{
  "operation": "update",
  "gameObjectGlobalObjectId": "GlobalObjectId_V1-1-abc123def456789-1234-0",
  "componentType": "UnityEngine.UI.Button",
  "propertyChanges": {
    "interactable": false
  }
}
```

**Benefits of GlobalObjectId:**
- Uniquely identifies GameObjects across scene reloads
- More reliable than hierarchy paths when objects move in the hierarchy
- Survives GameObject renames
- Priority over gameObjectPath when both are provided

**When to use:**
- When you need stable references to GameObjects
- When GameObjects might move in the hierarchy
- When you have the GlobalObjectId from a previous inspect operation

**When to use gameObjectPath instead:**
- For human-readable operations
- When you know the exact hierarchy path
- For simple, one-off operations

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

### Setting UnityEvent Listeners (NEW!)

The component management tool now supports setting UnityEvent listeners (Button.onClick, Slider.onValueChanged, etc.)!

**Simple Format (Single Listener, No Arguments):**
```json
{
  "operation": "update",
  "gameObjectPath": "Canvas/Button",
  "componentType": "UnityEngine.UI.Button",
  "propertyChanges": {
    "onClick": "GameManager.OnButtonClick"
  }
}
```

**Complex Format (Multiple Listeners with Arguments):**
```json
{
  "operation": "update",
  "gameObjectPath": "Canvas/Button",
  "componentType": "UnityEngine.UI.Button",
  "propertyChanges": {
    "onClick": {
      "clearListeners": true,
      "listeners": [
        {
          "targetPath": "GameManager",
          "methodName": "OnButtonClick",
          "mode": "Void"
        },
        {
          "targetPath": "AudioManager",
          "methodName": "PlaySound",
          "mode": "String",
          "argument": "button_click"
        }
      ]
    }
  }
}
```

**Supported Listener Modes:**
- **Void** - No arguments: `void MethodName()`
- **Int** - Integer argument: `void MethodName(int value)`
- **Float** - Float argument: `void MethodName(float value)`
- **String** - String argument: `void MethodName(string text)`
- **Bool** - Boolean argument: `void MethodName(bool flag)`
- **Object** - UnityEngine.Object argument: `void MethodName(GameObject obj)`

**Examples:**

1. **Button Click Event:**
   ```json
   {
     "operation": "update",
     "gameObjectPath": "Canvas/StartButton",
     "componentType": "UnityEngine.UI.Button",
     "propertyChanges": {
       "onClick": "GameManager.StartGame"
     }
   }
   ```

2. **Slider Value Changed:**
   ```json
   {
     "operation": "update",
     "gameObjectPath": "Canvas/VolumeSlider",
     "componentType": "UnityEngine.UI.Slider",
     "propertyChanges": {
       "onValueChanged": {
         "listeners": [
           {
             "targetPath": "AudioManager",
             "methodName": "SetVolume",
             "mode": "Float"
           }
         ]
       }
     }
   }
   ```

3. **Toggle State Changed:**
   ```json
   {
     "operation": "update",
     "gameObjectPath": "Canvas/MuteToggle",
     "componentType": "UnityEngine.UI.Toggle",
     "propertyChanges": {
       "onValueChanged": {
         "listeners": [
           {
             "targetPath": "AudioManager",
             "methodName": "SetMute",
             "mode": "Bool"
           }
         ]
       }
     }
   }
   ```

4. **Multiple Listeners on One Event:**
   ```json
   {
     "operation": "update",
     "gameObjectPath": "Canvas/ActionButton",
     "componentType": "UnityEngine.UI.Button",
     "propertyChanges": {
       "onClick": {
         "listeners": [
           {
             "targetPath": "GameManager",
             "methodName": "OnAction",
             "mode": "Void"
           },
           {
             "targetPath": "UIManager",
             "methodName": "ShowFeedback",
             "mode": "String",
             "argument": "Action completed!"
           },
           {
             "targetPath": "AudioManager",
             "methodName": "PlaySound",
             "mode": "String",
             "argument": "action_sound"
           }
         ]
       }
     }
   }
   ```

5. **Clear Existing Listeners:**
   ```json
   {
     "operation": "update",
     "gameObjectPath": "Canvas/ResetButton",
     "componentType": "UnityEngine.UI.Button",
     "propertyChanges": {
       "onClick": {
         "clearListeners": true,
         "listeners": [
           {
             "targetPath": "GameManager",
             "methodName": "Reset",
             "mode": "Void"
           }
         ]
       }
     }
   }
   ```

**Important Notes:**
- The target GameObject must exist in the scene
- The method must be public on a component attached to the target GameObject
- The method signature must match the specified mode
- Listeners are added as **persistent listeners** (saved in the scene)
- Use `"clearListeners": true` to remove all existing listeners before adding new ones

### Working with SerializeField Private Fields (NEW!)

The component management tool now fully supports accessing and modifying **private fields with the [SerializeField] attribute**!

**What's Supported:**
- ✅ Reading SerializeField private fields during inspection
- ✅ Writing SerializeField private fields (primitives, vectors, assets, etc.)
- ✅ Property filters work with SerializeField field names
- ✅ Error messages include SerializeField fields in suggestions
- ✅ Batch operations on SerializeField fields

**Example Script:**
```csharp
public class PlayerController : MonoBehaviour
{
    // Private fields with SerializeField - NOW ACCESSIBLE via MCP!
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private GameObject weaponPrefab;
    [SerializeField] private Material playerMaterial;
    [SerializeField] private Vector3 spawnPosition;

    // Public fields - always accessible
    public int currentHealth = 100;

    // Private without SerializeField - NOT accessible (as intended)
    private int secretValue = 42;
}
```

**Reading SerializeField Fields:**
```python
# Inspect component - SerializeField fields are included
result = unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "componentType": "PlayerController",
    "includeProperties": True
})

# result["properties"] includes:
# - maxHealth (SerializeField private)
# - moveSpeed (SerializeField private)
# - weaponPrefab (SerializeField private)
# - currentHealth (public)
# Does NOT include: secretValue (private without SerializeField)
```

**Updating SerializeField Primitive Values:**
```python
unity_component_crud({
    "operation": "update",
    "gameObjectPath": "Player",
    "componentType": "PlayerController",
    "propertyChanges": {
        "maxHealth": 200,          # SerializeField private int
        "moveSpeed": 10.0,         # SerializeField private float
        "spawnPosition": {"x": 10, "y": 5, "z": 0}  # SerializeField private Vector3
    }
})
```

**Updating SerializeField Asset References:**
```python
# Using GUID (recommended)
unity_component_crud({
    "operation": "update",
    "gameObjectPath": "Player",
    "componentType": "PlayerController",
    "propertyChanges": {
        "weaponPrefab": {
            "_ref": "asset",
            "guid": "abc123def456789"
        },
        "playerMaterial": {
            "_ref": "asset",
            "guid": "xyz789abc123"
        }
    }
})

# Using asset path
unity_component_crud({
    "operation": "update",
    "gameObjectPath": "Player",
    "componentType": "PlayerController",
    "propertyChanges": {
        "weaponPrefab": {
            "_ref": "asset",
            "path": "Assets/Prefabs/Sword.prefab"
        },
        "playerMaterial": {
            "_ref": "asset",
            "path": "Assets/Materials/PlayerMat.mat"
        }
    }
})
```

**Property Filter with SerializeField:**
```python
# Get only specific SerializeField fields (fast!)
result = unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "componentType": "PlayerController",
    "propertyFilter": ["maxHealth", "moveSpeed"]  # Both are SerializeField private
})
```

**Batch Operations with SerializeField:**
```python
# Update SerializeField on multiple GameObjects
unity_component_crud({
    "operation": "updateMultiple",
    "pattern": "Enemy*",
    "componentType": "EnemyController",
    "propertyChanges": {
        "maxHealth": 150,      # SerializeField private
        "attackDamage": 25.0   # SerializeField private
    },
    "maxResults": 100
})
```

**Important Notes:**
- Only fields with `[SerializeField]` attribute are accessible (not all private fields)
- This follows Unity's serialization rules - if Unity can serialize it, MCP can access it
- Works with all Unity-serializable types: primitives, vectors, colors, Object references, etc.
- Private fields WITHOUT `[SerializeField]` remain inaccessible (by design)
- No performance impact - attribute check is minimal

### Updating RectTransform Properties

**IMPORTANT:** RectTransform properties can be updated using **TWO METHODS** with the same results:

**Method 1: Using `unity_component_crud` (Recommended for consistency)**
```python
unity_component_crud({
    "operation": "update",
    "gameObjectPath": "Canvas/Panel",
    "componentType": "UnityEngine.RectTransform",
    "propertyChanges": {
        "anchoredPosition": {"x": 100, "y": 200},  # Dictionary format
        "sizeDelta": {"x": 300, "y": 400},
        "pivot": {"x": 0.5, "y": 0.5},
        "anchorMin": {"x": 0, "y": 0},
        "anchorMax": {"x": 1, "y": 1}
    }
})
```

**Method 2: Using `unity_ugui_manage` with `updateRect` operation**

Supports BOTH dictionary format and individual fields format:

```python
# Dictionary format (same as unity_component_crud)
unity_ugui_manage({
    "operation": "updateRect",
    "gameObjectPath": "Canvas/Panel",
    "anchoredPosition": {"x": 100, "y": 200},
    "sizeDelta": {"x": 300, "y": 400},
    "pivot": {"x": 0.5, "y": 0.5}
})

# Individual fields format (legacy support)
unity_ugui_manage({
    "operation": "updateRect",
    "gameObjectPath": "Canvas/Panel",
    "anchoredPositionX": 100,
    "anchoredPositionY": 200,
    "sizeDeltaX": 300,
    "sizeDeltaY": 400,
    "pivotX": 0.5,
    "pivotY": 0.5
})
```

**Supported RectTransform Properties:**
- `anchoredPosition` / `anchoredPositionX`, `anchoredPositionY` - Position relative to anchors
- `sizeDelta` / `sizeDeltaX`, `sizeDeltaY` - Size when not stretched
- `pivot` / `pivotX`, `pivotY` - Pivot point (0-1 range)
- `offsetMin` / `offsetMinX`, `offsetMinY` - Lower-left corner offset
- `offsetMax` / `offsetMaxX`, `offsetMaxY` - Upper-right corner offset
- `anchorMin` / `anchorMinX`, `anchorMinY` - Anchor min point (0-1 range)
- `anchorMax` / `anchorMaxX`, `anchorMaxY` - Anchor max point (0-1 range)

**Best Practices:**
1. Use **dictionary format** for consistency with other component updates
2. Use `unity_component_crud` for general component property updates
3. Use `unity_ugui_manage` for UGUI-specific operations like anchor presets and conversions
4. Both methods produce identical results - choose based on your workflow preference

**Common UI Positioning Examples:**

```python
# Center a UI element (dictionary format)
unity_ugui_manage({
    "operation": "updateRect",
    "gameObjectPath": "Canvas/Title",
    "anchoredPosition": {"x": 0, "y": 0},
    "anchorMin": {"x": 0.5, "y": 0.5},
    "anchorMax": {"x": 0.5, "y": 0.5}
})

# Center a UI element (individual fields format - equivalent to above)
unity_ugui_manage({
    "operation": "updateRect",
    "gameObjectPath": "Canvas/Title",
    "anchoredPositionX": 0,
    "anchoredPositionY": 0,
    "anchorMinX": 0.5,
    "anchorMinY": 0.5,
    "anchorMaxX": 0.5,
    "anchorMaxY": 0.5
})

# Stretch horizontally at the top
unity_ugui_manage({
    "operation": "updateRect",
    "gameObjectPath": "Canvas/TopBar",
    "sizeDelta": {"x": 0, "y": 50},  # Full width, 50px height
    "anchorMin": {"x": 0, "y": 1},
    "anchorMax": {"x": 1, "y": 1}
})

# Position at specific pixel coordinates
unity_ugui_manage({
    "operation": "updateRect",
    "gameObjectPath": "Canvas/Button",
    "anchoredPosition": {"x": 100, "y": -50},
    "sizeDelta": {"x": 200, "y": 60}
})
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

## Quick Start for Claude

### Creating Your First UI

The easiest way to create UI in Unity:

```python
# 1. Set up a UI scene (creates Canvas + EventSystem automatically)
unity_scene_quickSetup({"setupType": "UI"})

# 2. Create a button with one command
unity_ugui_createFromTemplate({
    "template": "Button",
    "text": "Click Me!",
    "width": 200,
    "height": 50
})

# 3. Add a layout to organize elements
unity_ugui_layoutManage({
    "operation": "add",
    "gameObjectPath": "Canvas",
    "layoutType": "VerticalLayoutGroup",
    "spacing": 10,
    "padding": {"left": 20, "right": 20, "top": 20, "bottom": 20}
})
```

### Creating a 3D Scene

```python
# 1. Set up 3D scene (Camera + Light automatically)
unity_scene_quickSetup({"setupType": "3D"})

# 2. Create a player
unity_gameobject_createFromTemplate({
    "template": "Player",
    "position": {"x": 0, "y": 1, "z": 0}
})

# 3. Create some obstacles
unity_gameobject_createFromTemplate({
    "template": "Cube",
    "name": "Obstacle1",
    "position": {"x": 5, "y": 0.5, "z": 0}
})
```

## Best Practices for Claude

### 1. Always Check Context First

Before making changes, inspect the scene to understand current state:

```python
# Get overview of current scene (returns one level of hierarchy)
unity_scene_crud({
    "operation": "inspect",
    "includeHierarchy": True,
    "includeComponents": False
})
```

### 2. Use Templates Instead of Manual Creation

❌ **Avoid this (slow, error-prone):**
```python
unity_gameobject_crud({"operation": "create", "name": "Button"})
unity_component_crud({"operation": "add", "gameObjectPath": "Button", "componentType": "UnityEngine.UI.Image"})
unity_component_crud({"operation": "add", "gameObjectPath": "Button", "componentType": "UnityEngine.UI.Button"})
# ... more steps
```

✅ **Use this instead (fast, reliable):**
```python
unity_ugui_createFromTemplate({"template": "Button", "text": "Click Me!"})
```

### 3. Use Menu Builder for Menu Creation

When creating menu systems with buttons and navigation, use menu builder to create complete menus:

```python
# Creates complete menu with buttons, layout, and optional State pattern navigation
unity_menu_hierarchyCreate({
    "menuName": "MainMenu",
    "menuStructure": {
        "Play": "Start Game",
        "Settings": {
            "text": "Game Settings",
            "submenus": {
                "Graphics": "Graphics Options",
                "Audio": "Audio Settings"
            }
        },
        "Quit": "Exit Game"
    },
    "generateStateMachine": True,
    "stateMachineScriptPath": "Assets/Scripts/MenuManager.cs"
})
# Creates panels, buttons, layout groups, and MenuStateMachine script
```

### 4. Use Script Templates for Quick Generation

For creating new Unity scripts, use `unity_script_template_generate` to quickly bootstrap MonoBehaviour and ScriptableObject classes:

✅ **MonoBehaviour component:**
```python
unity_script_template_generate({
    "templateType": "MonoBehaviour",
    "className": "PlayerController",
    "scriptPath": "Assets/Scripts/PlayerController.cs",
    "namespace": "MyGame.Player"
})
```

✅ **ScriptableObject data container:**
```python
unity_script_template_generate({
    "templateType": "ScriptableObject",
    "className": "GameConfig",
    "scriptPath": "Assets/ScriptableObjects/GameConfig.cs"
})
```

After generating the template, use `unity_asset_crud` with `operation="update"` to modify the script content:

```python
unity_asset_crud({
    "operation": "update",
    "assetPath": "Assets/Scripts/PlayerController.cs",
    "content": "using UnityEngine;\n\nnamespace MyGame.Player\n{\n    public class PlayerController : MonoBehaviour\n    {\n        public float speed = 5f;\n        \n        void Update()\n        {\n            // Movement code\n        }\n    }\n}"
})
```

**Benefits:**
- Quick scaffolding with proper Unity structure
- Standard lifecycle methods included
- CreateAssetMenu attribute for ScriptableObjects
- Modify with asset_crud as needed

## Common Use Cases

### Use Case 1: Create a Game Menu

```python
# Step 1: Setup UI scene
unity_scene_quickSetup({"setupType": "UI"})

# Step 2: Create complete menu with menu builder
unity_menu_hierarchyCreate({
    "menuName": "MainMenu",
    "menuStructure": {
        "StartGame": "Start New Game",
        "Options": "Game Options",
        "Quit": "Exit Game"
    },
    "generateStateMachine": True,
    "stateMachineScriptPath": "Assets/Scripts/MainMenuManager.cs",
    "buttonWidth": 300,
    "buttonHeight": 60
})
# Creates panels, buttons, layout groups, and navigation script in one command!
```

### Use Case 2: Create a Simple Game Level

```python
# Setup 3D scene
unity_scene_quickSetup({"setupType": "3D"})

# Create player
unity_gameobject_createFromTemplate({
    "template": "Player",
    "name": "Player",
    "position": {"x": 0, "y": 1, "z": 0}
})

# Create ground
unity_gameobject_createFromTemplate({
    "template": "Plane",
    "name": "Ground",
    "scale": {"x": 10, "y": 1, "z": 10}
})

# Create obstacles
unity_gameobject_createFromTemplate({
    "template": "Cube",
    "name": "Obstacle1",
    "position": {"x": 3, "y": 0.5, "z": 0}
})
unity_gameobject_createFromTemplate({
    "template": "Cube",
    "name": "Obstacle2",
    "position": {"x": -3, "y": 0.5, "z": 0}
})
unity_gameobject_createFromTemplate({
    "template": "Cube",
    "name": "Obstacle3",
    "position": {"x": 0, "y": 0.5, "z": 3}
})
```

### Use Case 3: Create an Inventory UI

```python
# Create inventory panel using UI template
unity_ugui_createFromTemplate({
    "template": "Panel",
    "name": "InventoryPanel",
    "parentPath": "Canvas"
})

# Configure grid layout
unity_ugui_layoutManage({
    "operation": "add",
    "gameObjectPath": "Canvas/InventoryPanel",
    "layoutType": "GridLayoutGroup",
    "cellSizeX": 80,
    "cellSizeY": 80,
    "spacing": 10,
    "constraint": "FixedColumnCount",
    "constraintCount": 5
})

# Add inventory slots
for i in range(20):
    unity_ugui_createFromTemplate({
        "template": "Image",
        "name": f"Slot{i}",
        "parentPath": "Canvas/InventoryPanel"
    })
```

### Use Case 4: Setup Scene Flow with Auto-Loading ⭐ NEW in v2.3.2

```python
# Create SceneFlow (saved as prefab, auto-loads in Play Mode!)
unity_gamekit_sceneflow({
    "operation": "create",
    "flowId": "MainGameFlow"
})

# Add scenes to the flow
unity_gamekit_sceneflow({
    "operation": "addScene",
    "flowId": "MainGameFlow",
    "sceneName": "MainMenu",
    "scenePath": "Assets/Scenes/MainMenu.unity",
    "loadMode": "single"
})

unity_gamekit_sceneflow({
    "operation": "addScene",
    "flowId": "MainGameFlow",
    "sceneName": "Level1",
    "scenePath": "Assets/Scenes/Level1.unity",
    "loadMode": "additive"
})

# Setup transitions
unity_gamekit_sceneflow({
    "operation": "addTransition",
    "flowId": "MainGameFlow",
    "fromScene": "MainMenu",
    "trigger": "startGame",
    "toScene": "Level1"
})

unity_gamekit_sceneflow({
    "operation": "addTransition",
    "flowId": "MainGameFlow",
    "fromScene": "Level1",
    "trigger": "backToMenu",
    "toScene": "MainMenu"
})

# That's it! The SceneFlow prefab is automatically loaded when you press Play.
# No need to manually place it in scenes!
```

**Benefits:**
- ✅ No manual GameObject placement required
- ✅ Works from any scene (even empty scenes)
- ✅ Persists across scene changes (DontDestroyOnLoad)
- ✅ Git-friendly prefab files
- ✅ Auto-loaded in Editor Play Mode and runtime builds

## Available Tools

### Tool Classification

SkillForUnity provides two categories of tools:

- **High-Level Tools (Recommended)**: Template-based and convenience tools for quick Unity workflows
- **Low-Level Tools (Core)**: Fundamental CRUD operations for precise control

### High-Level Tools (Recommended)

These tools make common Unity tasks much easier with templates and automation:

#### 1. Scene Quick Setup (`unity_scene_quickSetup`)

Instantly set up new scenes with common configurations.

**Setup Types:**
- `"3D"` - Main Camera + Directional Light
- `"2D"` - 2D Camera (orthographic)
- `"UI"` - Canvas + EventSystem
- `"VR"` - VR Camera setup
- `"Empty"` - Empty scene

**Example:**
```python
unity_scene_quickSetup({"setupType": "UI"})
```

#### 2. GameObject Templates (`unity_gameobject_createFromTemplate`)

Create common GameObjects with one command.

**Available Templates:**
- **Primitives**: Cube, Sphere, Plane, Cylinder, Capsule, Quad
- **Lights**: Light-Directional, Light-Point, Light-Spot
- **Special**: Camera, Empty, Player, Enemy, Particle System, Audio Source

**Example:**
```python
unity_gameobject_createFromTemplate({
    "template": "Sphere",
    "name": "Ball",
    "position": {"x": 0, "y": 5, "z": 0},
    "scale": {"x": 0.5, "y": 0.5, "z": 0.5}
})
```

#### 3. UI Templates (`unity_ugui_createFromTemplate`)

Create complete UI elements with all necessary components.

**Available Templates:**
- Button, Text, Image, RawImage, Panel
- ScrollView, InputField, Slider, Toggle, Dropdown

**Example:**
```python
unity_ugui_createFromTemplate({
    "template": "Button",
    "text": "Start Game",
    "width": 200,
    "height": 50,
    "anchorPreset": "middle-center"
})
```

#### 4. Layout Management (`unity_ugui_layoutManage`)

Manage layout components on UI GameObjects.

**Layout Types:**
- HorizontalLayoutGroup, VerticalLayoutGroup
- GridLayoutGroup
- ContentSizeFitter, LayoutElement, AspectRatioFitter

**Example:**
```python
unity_ugui_layoutManage({
    "operation": "add",
    "gameObjectPath": "Canvas/Panel",
    "layoutType": "VerticalLayoutGroup",
    "spacing": 15,
    "padding": {"left": 20, "right": 20, "top": 20, "bottom": 20},
    "childControlWidth": True,
    "childControlHeight": False
})
```

#### 5. Template Customization (`unity_template_manage`)

Transform existing GameObjects into custom templates by adding components and child objects, then optionally save them as reusable prefabs!

**Operations:**
- **`customize`** - Add multiple components and child GameObjects to an existing object in one command
- **`convertToPrefab`** - Convert the customized GameObject to a prefab asset for reuse

**Benefits:**
- 🚀 Customize existing objects without creating from scratch
- 🔧 Add multiple components with properties in one operation
- 👶 Create child hierarchies with their own components
- 💾 Save as prefabs for reuse across scenes
- ⚡ Perfect for building complex GameObjects from simple starting points

**Example 1: Customize GameObject with Components and Children**
```python
# Start with a simple Cube
unity_gameobject_createFromTemplate({"template": "Cube", "name": "Player"})

# Customize it with multiple components and child objects
unity_template_manage({
    "operation": "customize",
    "gameObjectPath": "Player",
    "components": [
        {
            "type": "UnityEngine.Rigidbody",
            "properties": {
                "mass": 2.0,
                "useGravity": True
            }
        },
        {
            "type": "UnityEngine.CapsuleCollider",
            "properties": {
                "radius": 0.5,
                "height": 2.0
            }
        }
    ],
    "children": [
        {
            "name": "Camera",
            "components": [
                {
                    "type": "UnityEngine.Camera",
                    "properties": {
                        "fieldOfView": 60
                    }
                }
            ],
            "position": {"x": 0, "y": 1, "z": -3}
        },
        {
            "name": "Weapon",
            "position": {"x": 0.5, "y": 0, "z": 0.5},
            "components": [
                {
                    "type": "UnityEngine.BoxCollider",
                    "properties": {
                        "isTrigger": True
                    }
                }
            ]
        }
    ]
})
```

**Example 2: Convert to Prefab**
```python
# After customizing, save as prefab for reuse
unity_template_manage({
    "operation": "convertToPrefab",
    "gameObjectPath": "Player",
    "prefabPath": "Assets/Prefabs/CustomPlayer.prefab",
    "overwrite": False
})
```

**Example 3: Customize UI Element**
```python
# Create a button, then customize it
unity_ugui_createFromTemplate({"template": "Button", "name": "CustomButton"})

unity_template_manage({
    "operation": "customize",
    "gameObjectPath": "Canvas/CustomButton",
    "components": [
        {
            "type": "UnityEngine.UI.Shadow",
            "properties": {
                "effectDistance": {"x": 2, "y": -2}
            }
        }
    ],
    "children": [
        {
            "name": "Icon",
            "isUI": True,
            "components": [
                {
                    "type": "UnityEngine.UI.Image"
                }
            ],
            "position": {"x": -50, "y": 0, "z": 0}
        }
    ]
})
```

**Common Use Cases:**
- 🎮 **Game Characters**: Start with primitive, add physics, colliders, and child objects for weapons/cameras
- 🎨 **Custom UI Elements**: Enhance basic UI components with shadows, outlines, and icon children
- 🏗️ **Prefab Creation**: Build complex structures once, save as prefab, reuse everywhere
- 🔄 **Iterative Design**: Quickly experiment with different component combinations

**Important Notes:**
- Component types must be fully qualified (e.g., `UnityEngine.Rigidbody`)
- For UI children, set `isUI: True` to create RectTransform instead of Transform
- Use `allowDuplicates: True` to add a component even if it already exists
- Prefab paths must start with `Assets/` and end with `.prefab`

#### 6. Scene Inspector (`unity_scene_crud` with `operation="inspect"`)

Get comprehensive scene information including hierarchy, statistics, and context.

**Important:** Hierarchy inspection returns **one level only** to optimize performance. For deeper exploration, query specific GameObjects by path.

**Example:**
```python
# Returns root GameObjects with their direct child names
unity_scene_crud({
    "operation": "inspect",
    "includeHierarchy": True,
    "includeComponents": True,
    "filter": "Player*"  # Optional: filter by pattern
})
# Result includes:
# - sceneName, scenePath
# - hierarchy: list of root GameObjects with childCount and childNames
# - totalGameObjects, cameraCount, lightCount, canvasCount

# To explore deeper, use GameObject inspect with specific path
unity_gameobject_crud({
    "operation": "inspect",
    "gameObjectPath": "Player/Weapon"  # Query specific child
})
```

#### 7. Design Pattern Generation (`unity_designPattern_generate`)

Generate production-ready C# implementations of common Unity design patterns. Instantly create complete, commented, and ready-to-use pattern implementations in seconds!

**Available Patterns:**
- **Singleton** - Single instance management with optional persistence
- **ObjectPool** - Efficient object reuse pattern
- **StateMachine** - State management with transitions
- **Observer** - Event system for decoupled communication
- **Command** - Action abstraction with undo/redo support
- **Factory** - Object creation pattern with prefab management
- **ServiceLocator** - Global service access pattern

**Quick Example:**
```python
unity_designPattern_generate({
    "patternType": "singleton",
    "className": "GameManager",
    "scriptPath": "Assets/Scripts/GameManager.cs",
    "options": {"persistent": True, "monoBehaviour": True}
})
```

See the detailed Design Pattern Generation section below for complete examples and all pattern options.

#### 8. Hierarchical Menu Creation (`unity_menu_hierarchyCreate`)

Create complete hierarchical menu systems with nested submenus and automatic State pattern navigation. Perfect for main menus, pause menus, and settings menus with multiple levels.

**Quick Example:**
```python
unity_menu_hierarchyCreate({
    "menuName": "MainMenu",
    "menuStructure": {
        "Play": "Start Game",
        "Settings": {
            "text": "Game Settings",
            "submenus": {
                "Graphics": "Graphics Options",
                "Audio": "Audio Settings",
                "Controls": "Control Mapping"
            }
        },
        "Quit": "Exit Game"
    },
    "generateStateMachine": True,
    "stateMachineScriptPath": "Assets/Scripts/MenuManager.cs",
    "navigationMode": "both"
})
```

See the detailed Hierarchical Menu Creation section below for complete documentation.

#### 9. Additional High-Level Tools

- **`unity_ugui_manage`** - Unified UGUI management for RectTransform operations (anchors, positions, etc.)
- **`unity_ugui_rectAdjust`** - Adjust RectTransform size based on reference resolution
- **`unity_ugui_anchorManage`** - Manage RectTransform anchors with presets
- **`unity_ugui_detectOverlaps`** - Detect overlapping UI elements for debugging
- **`unity_renderPipeline_manage`** - Manage render pipeline settings (URP/HDRP)

#### 10. UI Hierarchy (`unity_ui_hierarchy`) ⭐ NEW

**Declarative UI hierarchy management** - Create complex UI structures from single JSON definitions, manage visibility states, configure keyboard/gamepad navigation.

**Operations:**
- `create` - Build complete UI hierarchy from declarative JSON structure
- `clone` - Duplicate existing UI hierarchy with optional rename
- `inspect` - Export UI hierarchy as JSON structure
- `delete` - Remove UI hierarchy
- `show/hide/toggle` - Control visibility using CanvasGroup
- `setNavigation` - Configure keyboard/gamepad navigation

**Example: Create Main Menu**
```python
unity_ui_hierarchy({
    "operation": "create",
    "parentPath": "Canvas",
    "hierarchy": {
        "type": "panel",
        "name": "MainMenu",
        "width": 400,
        "height": 500,
        "layout": "Vertical",
        "spacing": 20,
        "padding": {"left": 30, "right": 30, "top": 50, "bottom": 30},
        "children": [
            {"type": "text", "name": "Title", "text": "My Game", "fontSize": 48},
            {"type": "button", "name": "StartBtn", "text": "Start Game", "width": 200, "height": 50},
            {"type": "button", "name": "OptionsBtn", "text": "Options", "width": 200, "height": 50},
            {"type": "button", "name": "QuitBtn", "text": "Quit", "width": 200, "height": 50}
        ]
    }
})
```

**Supported Element Types:**
- `panel` - Panel with Image component
- `button` - Button with Text child
- `text` - Text element (TMP or legacy)
- `image` - Image element
- `inputfield` - Input field with placeholder
- `scrollview` - Scroll view with viewport/content
- `toggle` - Toggle with label
- `slider` - Slider with min/max
- `dropdown` - Dropdown with options

**Visibility Control:**
```python
# Hide UI hierarchy
unity_ui_hierarchy({
    "operation": "hide",
    "gameObjectPath": "Canvas/MainMenu",
    "interactable": False,
    "blocksRaycasts": False
})

# Show with toggle
unity_ui_hierarchy({
    "operation": "toggle",
    "gameObjectPath": "Canvas/MainMenu"
})
```

**Navigation Setup:**
```python
# Auto vertical navigation for menu
unity_ui_hierarchy({
    "operation": "setNavigation",
    "gameObjectPath": "Canvas/MainMenu",
    "navigationMode": "auto-vertical",
    "wrapAround": True
})
```

#### 11. UI State Management (`unity_ui_state`) ⭐ NEW

**UI state management** - Define, save, load, and transition between named UI states.

**Operations:**
- `defineState` - Define a named state with element configurations
- `applyState` - Apply a saved state to UI elements
- `saveState` - Capture current UI state
- `loadState` - Load state definition without applying
- `listStates` - List all defined states
- `deleteState` - Remove a state definition
- `createStateGroup` - Create mutually exclusive state group
- `transitionTo` - Transition to a state
- `getActiveState` - Get currently active state

**Example: Dialog States**
```python
# Define "open" state for dialog
unity_ui_state({
    "operation": "defineState",
    "stateName": "open",
    "rootPath": "Canvas/Dialog",
    "elements": [
        {"path": "", "active": True, "alpha": 1, "interactable": True},
        {"path": "Content", "visible": True},
        {"path": "CloseButton", "interactable": True}
    ]
})

# Define "closed" state
unity_ui_state({
    "operation": "defineState",
    "stateName": "closed",
    "rootPath": "Canvas/Dialog",
    "elements": [
        {"path": "", "active": False, "alpha": 0}
    ]
})

# Apply state
unity_ui_state({
    "operation": "applyState",
    "stateName": "open",
    "rootPath": "Canvas/Dialog"
})

# Save current state snapshot
unity_ui_state({
    "operation": "saveState",
    "stateName": "current_snapshot",
    "rootPath": "Canvas/Dialog",
    "includeChildren": True
})
```

#### 12. UI Navigation (`unity_ui_navigation`) ⭐ NEW

**UI navigation management** - Configure keyboard/gamepad navigation for Selectable UI elements.

**Operations:**
- `configure` - Set navigation mode (none/horizontal/vertical/automatic/explicit)
- `setExplicit` - Set explicit up/down/left/right targets
- `autoSetup` - Auto-configure navigation for all Selectables
- `createGroup` - Create isolated navigation group
- `setFirstSelected` - Set EventSystem first selected
- `inspect` - View navigation configuration
- `reset` - Reset to automatic navigation
- `disable` - Disable navigation

**Example: Menu Navigation**
```python
# Auto-setup vertical menu navigation with wrap-around
unity_ui_navigation({
    "operation": "autoSetup",
    "rootPath": "Canvas/MainMenu",
    "direction": "vertical",
    "wrapAround": True
})

# Grid navigation for inventory
unity_ui_navigation({
    "operation": "autoSetup",
    "rootPath": "Canvas/Inventory/Grid",
    "direction": "grid",
    "columns": 4,
    "wrapAround": True
})

# Create isolated navigation group
unity_ui_navigation({
    "operation": "createGroup",
    "groupName": "DialogButtons",
    "elements": [
        "Canvas/Dialog/OKButton",
        "Canvas/Dialog/CancelButton"
    ],
    "direction": "horizontal",
    "wrapAround": True,
    "isolate": True
})

# Set first selected element
unity_ui_navigation({
    "operation": "setFirstSelected",
    "gameObjectPath": "Canvas/MainMenu/StartButton"
})
```

#### 13. GameKit SceneFlow (`unity_gamekit_sceneflow`) ⭐ NEW in v2.3.2

**Scene transition management with automatic prefab-based loading:**

```python
# Create SceneFlow (automatically saved as prefab)
unity_gamekit_sceneflow({
    "operation": "create",
    "flowId": "MainGameFlow"
})
# → Saved to Assets/Resources/GameKitSceneFlows/MainGameFlow.prefab
# → Auto-loaded in Play Mode and at runtime!

# Add scenes
unity_gamekit_sceneflow({
    "operation": "addScene",
    "flowId": "MainGameFlow",
    "sceneName": "MainMenu",
    "scenePath": "Assets/Scenes/MainMenu.unity",
    "loadMode": "single"  # or "additive"
})

# Add transitions
unity_gamekit_sceneflow({
    "operation": "addTransition",
    "flowId": "MainGameFlow",
    "fromScene": "MainMenu",
    "trigger": "startGame",
    "toScene": "Level1"
})
```

**Key Features:**
- ✅ **Prefab-Based**: All configs saved as prefabs in `Resources/GameKitSceneFlows/`
- ✅ **Auto-Load**: Automatically loaded in Play Mode (Editor) and at runtime (builds)
- ✅ **No Manual Setup**: No need to place in initial scenes
- ✅ **Git-Friendly**: Prefabs can be version controlled
- ✅ **DontDestroyOnLoad**: Persists across scene changes automatically

**Operations:** `create`, `addScene`, `removeScene`, `updateScene`, `addTransition`, `removeTransition`, `addSharedScene`, `removeSharedScene`, `inspect`, `delete`, `transition`

---

### Low-Level Tools (Core)

These tools provide fundamental CRUD operations for precise control over Unity objects:

#### 1. Scene Management (`unity_scene_crud`)

The scene management tool provides operations for working with Unity scene files and inspecting scene content.

**Operations:**

1. **create** - Create a new scene
   ```python
   unity_scene_crud({
       "operation": "create",
       "scenePath": "Assets/Scenes/NewLevel.unity",
       "additive": False  # Single mode (default) or additive
   })
   ```

2. **load** - Load an existing scene
   ```python
   unity_scene_crud({
       "operation": "load",
       "scenePath": "Assets/Scenes/MainMenu.unity",
       "additive": False
   })
   ```

3. **save** - Save current scene(s)
   ```python
   unity_scene_crud({
       "operation": "save",
       "scenePath": "Assets/Scenes/Level1.unity",  # Optional: specific scene
       "includeOpenScenes": False  # Save all open scenes if true
   })
   ```

4. **delete** - Delete a scene file
   ```python
   unity_scene_crud({
       "operation": "delete",
       "scenePath": "Assets/Scenes/OldLevel.unity"
   })
   ```

5. **duplicate** - Duplicate a scene
   ```python
   unity_scene_crud({
       "operation": "duplicate",
       "scenePath": "Assets/Scenes/Level1.unity",
       "newSceneName": "Level1_Copy"
   })
   ```

6. **inspect** - Inspect current scene context (see Section 6 above for details)

7. **listBuildSettings** - List all scenes in build settings
   ```python
   unity_scene_crud({
       "operation": "listBuildSettings"
   })
   ```

8. **addToBuildSettings** - Add scene to build settings
   ```python
   unity_scene_crud({
       "operation": "addToBuildSettings",
       "scenePath": "Assets/Scenes/Level2.unity",
       "enabled": True,
       "index": 1  # Optional: position in build
   })
   ```

9. **removeFromBuildSettings** - Remove scene from build settings
   ```python
   unity_scene_crud({
       "operation": "removeFromBuildSettings",
       "scenePath": "Assets/Scenes/OldLevel.unity"
   })
   ```

10. **reorderBuildSettings** - Reorder scenes in build settings
    ```python
    unity_scene_crud({
        "operation": "reorderBuildSettings",
        "fromIndex": 2,
        "toIndex": 0
    })
    ```

11. **setBuildSettingsEnabled** - Enable/disable scene in build
    ```python
    unity_scene_crud({
        "operation": "setBuildSettingsEnabled",
        "scenePath": "Assets/Scenes/TestLevel.unity",
        "enabled": False
    })
    ```

#### 2. Asset Management (`unity_asset_crud`)

Manage Unity assets including C# scripts, JSON, XML, config files, and other text-based files.

**IMPORTANT:**
- Use `create` and `update` operations for text-based assets (C# scripts, JSON, XML, TXT, config files)
- For new MonoBehaviour/ScriptableObject scripts, consider using `unity_script_template_generate` first for proper scaffolding
- This tool handles C# scripts and triggers Unity's automatic compilation

**Operations:**

1. **create** - Create a new text asset file
   ```python
   unity_asset_crud({
       "operation": "create",
       "assetPath": "Assets/Config/settings.json",
       "content": '{\n  "volume": 0.8,\n  "quality": "High"\n}'
   })
   ```

2. **update** - Update existing text asset content
   ```python
   unity_asset_crud({
       "operation": "update",
       "assetPath": "Assets/Config/settings.json",
       "content": '{\n  "volume": 1.0,\n  "quality": "Ultra"\n}'
   })
   ```

3. **updateImporter** - Update asset importer settings
   ```python
   unity_asset_crud({
       "operation": "updateImporter",
       "assetPath": "Assets/Textures/Sprite.png",
       "propertyChanges": {
           "textureType": "Sprite",
           "filterMode": "Bilinear",
           "isReadable": True
       }
   })
   ```

4. **rename** - Rename an asset
   ```python
   unity_asset_crud({
       "operation": "rename",
       "assetPath": "Assets/Textures/OldName.png",
       "destinationPath": "Assets/Textures/NewName.png"
   })
   ```

5. **duplicate** - Duplicate an asset
   ```python
   unity_asset_crud({
       "operation": "duplicate",
       "assetPath": "Assets/Materials/Material.mat",
       "destinationPath": "Assets/Materials/Material_Copy.mat"
   })
   ```

6. **delete** - Delete an asset
   ```python
   unity_asset_crud({
       "operation": "delete",
       "assetPath": "Assets/Unused/OldAsset.asset"
   })
   ```

7. **inspect** - Get asset information and importer settings
   ```python
   unity_asset_crud({
       "operation": "inspect",
       "assetPath": "Assets/Textures/Sprite.png",
       "includeProperties": True  # Include importer settings
   })
   ```

8. **findMultiple** - Find multiple assets using wildcard patterns
   ```python
   unity_asset_crud({
       "operation": "findMultiple",
       "pattern": "Assets/Textures/*.png"
   })
   ```

9. **deleteMultiple** - Delete multiple assets using wildcard patterns
   ```python
   unity_asset_crud({
       "operation": "deleteMultiple",
       "pattern": "Assets/Temp/*.asset"
   })
   ```

10. **inspectMultiple** - Inspect multiple assets using wildcard patterns
   ```python
   unity_asset_crud({
       "operation": "inspectMultiple",
       "pattern": "Assets/Materials/*.mat",
       "includeProperties": False  # Skip importer settings for performance
   })
   ```

**Important Notes:**
- Supports all text-based assets including C# scripts, JSON, XML, TXT, and config files
- For new MonoBehaviour/ScriptableObject scripts, use `unity_script_template_generate` for proper scaffolding
- All asset paths must start with "Assets/"
- Wildcard patterns support glob syntax (*, ?, etc.)
- `create` will fail if the file already exists - use `update` for existing files
- `update` will fail if the file doesn't exist - use `create` for new files

#### 3. GameObject Management (`unity_gameobject_crud`)

Comprehensive tool for managing GameObjects in the scene hierarchy.

**Operations:**

1. **create** - Create a new GameObject
   ```python
   unity_gameobject_crud({
       "operation": "create",
       "name": "Player",
       "parentPath": "Game"  # Optional
   })
   ```

2. **delete** - Delete a GameObject
   ```python
   unity_gameobject_crud({
       "operation": "delete",
       "gameObjectPath": "Player"
   })
   ```

3. **move** - Move a GameObject to a new parent
   ```python
   unity_gameobject_crud({
       "operation": "move",
       "gameObjectPath": "Player",
       "parentPath": "Characters"  # null/empty for root
   })
   ```

4. **rename** - Rename a GameObject
   ```python
   unity_gameobject_crud({
       "operation": "rename",
       "gameObjectPath": "Player",
       "name": "MainPlayer"
   })
   ```

5. **update** - Update GameObject properties (tag, layer, active, static)
   ```python
   # Update tag
   unity_gameobject_crud({
       "operation": "update",
       "gameObjectPath": "Player",
       "tag": "Player"
   })

   # Update layer (by name or index)
   unity_gameobject_crud({
       "operation": "update",
       "gameObjectPath": "Player",
       "layer": "UI"  # or layer: 5
   })

   # Update active state
   unity_gameobject_crud({
       "operation": "update",
       "gameObjectPath": "Player",
       "active": False
   })

   # Update static flag
   unity_gameobject_crud({
       "operation": "update",
       "gameObjectPath": "Environment",
       "static": True
   })

   # Update multiple properties at once
   unity_gameobject_crud({
       "operation": "update",
       "gameObjectPath": "Player",
       "tag": "Player",
       "layer": 0,
       "active": True,
       "static": False
   })
   ```

6. **duplicate** - Duplicate a GameObject
   ```python
   unity_gameobject_crud({
       "operation": "duplicate",
       "gameObjectPath": "Enemy",
       "name": "Enemy2",  # Optional
       "parentPath": "Enemies"  # Optional
   })
   ```

7. **inspect** - Inspect a GameObject
   ```python
   unity_gameobject_crud({
       "operation": "inspect",
       "gameObjectPath": "Player"
   })

   # Note: For component property details, use unity_component_crud instead
   # This operation returns component TYPE NAMES only, not their properties
   ```

8. **findMultiple** - Find GameObjects by wildcard/regex pattern
   ```python
   unity_gameobject_crud({
       "operation": "findMultiple",
       "pattern": "Enemy*",  # Wildcard
       "maxResults": 100  # Limit results (default: 1000)
   })

   # Regex pattern
   unity_gameobject_crud({
       "operation": "findMultiple",
       "pattern": "^Enemy_[0-9]+$",
       "useRegex": True
   })
   ```

9. **deleteMultiple** - Delete multiple GameObjects
   ```python
   unity_gameobject_crud({
       "operation": "deleteMultiple",
       "pattern": "Temp*",
       "maxResults": 500
   })
   ```

10. **inspectMultiple** - Inspect multiple GameObjects
   ```python
   unity_gameobject_crud({
       "operation": "inspectMultiple",
       "pattern": "Enemy*",
       "includeComponents": True,  # Include component type names
       "maxResults": 100
   })
   ```

**Performance Parameters:**

- **`maxResults`** (multiple operations): Maximum objects to process (default: 1000)
- **`includeComponents`** (inspectMultiple only): Include component type names

**Performance Tips:**

```python
# ❌ Slow: Full inspection of 1000+ objects
unity_gameobject_crud({
    "operation": "inspectMultiple",
    "pattern": "*",
    "includeComponents": True
})

# ✅ Fast: Light inspection with limit
unity_gameobject_crud({
    "operation": "inspectMultiple",
    "pattern": "Enemy*",
    "includeComponents": False,
    "maxResults": 100
})
```

#### 4. Component Management (`unity_component_crud`)

Comprehensive tool for managing components on GameObjects.

**Operations:**

1. **add** - Add a component to a GameObject
   ```python
   unity_component_crud({
       "operation": "add",
       "gameObjectPath": "Player",
       "componentType": "UnityEngine.Rigidbody"
   })
   ```

2. **remove** - Remove a component from a GameObject
   ```python
   unity_component_crud({
       "operation": "remove",
       "gameObjectPath": "Player",
       "componentType": "UnityEngine.Rigidbody"
   })
   ```

3. **update** - Update component properties
   ```python
   unity_component_crud({
       "operation": "update",
       "gameObjectPath": "Player",
       "componentType": "UnityEngine.Transform",
       "propertyChanges": {
           "position": {"x": 0, "y": 1, "z": 0},
           "rotation": {"x": 0, "y": 45, "z": 0}
       }
   })
   ```

4. **inspect** - Inspect a component
   ```python
   # Full inspection (all properties)
   unity_component_crud({
       "operation": "inspect",
       "gameObjectPath": "Player",
       "componentType": "UnityEngine.Transform"
   })

   # Light inspection (type only, no properties)
   unity_component_crud({
       "operation": "inspect",
       "gameObjectPath": "Player",
       "componentType": "UnityEngine.CharacterController",
       "includeProperties": False  # 10x faster!
   })

   # Filter specific properties
   unity_component_crud({
       "operation": "inspect",
       "gameObjectPath": "Player",
       "componentType": "UnityEngine.Transform",
       "propertyFilter": ["position", "rotation"]  # Only these properties
   })
   ```

5. **addMultiple** - Add component to multiple GameObjects
   ```python
   unity_component_crud({
       "operation": "addMultiple",
       "pattern": "Enemy*",
       "componentType": "UnityEngine.Rigidbody",
       "propertyChanges": {  # Optional: set initial properties
           "mass": 2.0,
           "useGravity": True
       },
       "maxResults": 100,
       "stopOnError": False  # Continue on errors
   })
   ```

6. **removeMultiple** - Remove component from multiple GameObjects
   ```python
   unity_component_crud({
       "operation": "removeMultiple",
       "pattern": "Temp*",
       "componentType": "UnityEngine.BoxCollider",
       "maxResults": 500
   })
   ```

7. **updateMultiple** - Update component on multiple GameObjects
   ```python
   unity_component_crud({
       "operation": "updateMultiple",
       "pattern": "Enemy*",
       "componentType": "UnityEngine.Rigidbody",
       "propertyChanges": {
           "mass": 5.0,
           "drag": 0.5
       },
       "maxResults": 200
   })
   ```

8. **inspectMultiple** - Inspect component on multiple GameObjects
   ```python
   unity_component_crud({
       "operation": "inspectMultiple",
       "pattern": "Player/Weapon*",
       "componentType": "UnityEngine.BoxCollider",
       "includeProperties": False,  # Fast mode
       "maxResults": 100
   })

   # Inspect specific properties only
   unity_component_crud({
       "operation": "inspectMultiple",
       "pattern": "Enemy*",
       "componentType": "UnityEngine.Transform",
       "propertyFilter": ["position", "rotation"],
       "maxResults": 500
   })
   ```

**Performance Parameters:**

- **`includeProperties`** (inspect operations): Set to `false` to skip property reading (10x faster)
- **`propertyFilter`** (inspect operations): Array of property names to inspect
- **`maxResults`** (multiple operations): Maximum objects to process (default: 1000)
- **`stopOnError`** (multiple operations): Stop on first error if `true` (default: `false`)

**Return Value for Multiple Operations:**

```python
{
    "pattern": "Enemy*",
    "componentType": "UnityEngine.Rigidbody",
    "totalCount": 5000,        # Total matching objects
    "successCount": 1000,      # Successfully processed
    "errorCount": 0,           # Failed operations
    "truncated": True,         # Was result limited by maxResults?
    "results": [...],          # Successful operations
    "errors": []               # Error details (if any)
}
```

**Performance Tips:**

```python
# ✅ Fast: Check if component exists
unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.CharacterController",
    "includeProperties": False
})

# ✅ Fast: Get specific properties only
unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Transform",
    "propertyFilter": ["position"]
})

# ✅ Safe: Batch operation with error handling
unity_component_crud({
    "operation": "addMultiple",
    "pattern": "Enemy*",
    "componentType": "UnityEngine.BoxCollider",
    "stopOnError": False,  # Don't stop on errors
    "maxResults": 1000
})

# Check for errors after batch operation
result = unity_component_crud(...)
if result["errorCount"] > 0:
    print(f"Errors occurred: {result['errors']}")
```

**Best Practices:**

1. **Use `includeProperties=false` when you only need to check existence:**
   ```python
   # Fast existence check
   result = unity_component_crud({
       "operation": "inspect",
       "gameObjectPath": "Player",
       "componentType": "UnityEngine.Rigidbody",
       "includeProperties": False
   })
   # Result just tells you if component exists
   ```

2. **Use `propertyFilter` when you need specific properties:**
   ```python
   # Only get position and rotation
   result = unity_component_crud({
       "operation": "inspect",
       "gameObjectPath": "Player",
       "componentType": "UnityEngine.Transform",
       "propertyFilter": ["position", "rotation"]
   })
   ```

3. **Test with small `maxResults` first:**
   ```python
   # Test with 10 objects first
   test = unity_component_crud({
       "operation": "addMultiple",
       "pattern": "Enemy*",
       "componentType": "UnityEngine.Rigidbody",
       "maxResults": 10
   })

   # If successful, scale up
   if test["errorCount"] == 0:
       final = unity_component_crud({
           ...
           "maxResults": 5000
       })
   ```

4. **Use `stopOnError=false` for resilient batch operations:**
   ```python
   # Continue on errors, review them later
   result = unity_component_crud({
       "operation": "updateMultiple",
       "pattern": "Enemy*",
       "componentType": "UnityEngine.Rigidbody",
       "propertyChanges": {"mass": 5.0},
       "stopOnError": False
   })

   # Check what failed
   for error in result["errors"]:
       print(f"Failed on {error['gameObject']}: {error['error']}")
   ```

#### 5. Prefab Management (`unity_prefab_crud`)

Manage Unity prefabs: create from GameObjects, update existing prefabs, instantiate in scenes, apply/revert overrides.

**Operations:** create, update, inspect, instantiate, unpack, applyOverrides, revertOverrides

See the detailed Prefab Management section below for complete documentation.

#### 6. Project Settings Management (`unity_projectSettings_crud`)

Read and write Unity Project Settings across multiple categories (player, quality, time, physics, audio, editor).

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

**Build Settings Operations:**

4. **listBuildScenes** - List all scenes in build settings
   ```json
   {
     "operation": "listBuildScenes"
   }
   ```
   Returns array of build scenes with path, GUID, enabled status, and index.

5. **addSceneToBuild** - Add scene to build settings
   ```json
   {
     "operation": "addSceneToBuild",
     "scenePath": "Assets/Scenes/Level1.unity",
     "index": 0,
     "enabled": true
   }
   ```
   - `scenePath`: Path to scene asset (required)
   - `index`: Insert at specific index (optional, -1 = append to end)
   - `enabled`: Enable scene in build (optional, default: true)

6. **removeSceneFromBuild** - Remove scene from build settings
   ```json
   {
     "operation": "removeSceneFromBuild",
     "scenePath": "Assets/Scenes/Level1.unity"
   }
   ```
   OR
   ```json
   {
     "operation": "removeSceneFromBuild",
     "index": 0
   }
   ```
   - Specify either `scenePath` or `index`

7. **reorderBuildScenes** - Change scene order in build
   ```json
   {
     "operation": "reorderBuildScenes",
     "fromIndex": 0,
     "toIndex": 2
   }
   ```
   - Moves scene from `fromIndex` to `toIndex`

8. **setBuildSceneEnabled** - Enable/disable scene in build
   ```json
   {
     "operation": "setBuildSceneEnabled",
     "scenePath": "Assets/Scenes/Level1.unity",
     "enabled": false
   }
   ```
   OR
   ```json
   {
     "operation": "setBuildSceneEnabled",
     "index": 0,
     "enabled": true
   }
   ```
   - Specify either `scenePath` or `index`
   - `enabled`: true to enable, false to disable

**Implementation Notes:**
- The tool uses Unity's PlayerSettings, QualitySettings, Time, Physics, AudioSettings, and EditorSettings APIs
- All property names are case-insensitive for convenience
- Complex values like Vector3 (gravity) are represented as dictionaries with x, y, z keys
- Enum values are returned as strings and can be set using string names

#### 7. Script Template Generation (`unity_script_template_generate`)

Generate Unity script templates for MonoBehaviour or ScriptableObject. Quickly create starter scripts with standard Unity lifecycle methods.

**Parameters:**

- `templateType`: "MonoBehaviour" or "ScriptableObject" (required)
- `className`: Name of the class to generate (required)
- `scriptPath`: Full path to the C# script file to create (required, must start with "Assets/" and end with ".cs")
- `namespace`: Optional C# namespace for the generated class

**Template Types:**

1. **MonoBehaviour** - Standard Unity component script with lifecycle methods:
   ```python
   unity_script_template_generate({
       "templateType": "MonoBehaviour",
       "className": "PlayerController",
       "scriptPath": "Assets/Scripts/PlayerController.cs",
       "namespace": "MyGame.Player"
   })
   ```

   Generates:
   ```csharp
   using UnityEngine;

   namespace MyGame.Player
   {
       public class PlayerController : MonoBehaviour
       {
           void Awake()
           {

           }

           void Start()
           {

           }

           void Update()
           {

           }

           void OnDestroy()
           {

           }
       }
   }
   ```

2. **ScriptableObject** - Data container asset class with CreateAssetMenu attribute:
   ```python
   unity_script_template_generate({
       "templateType": "ScriptableObject",
       "className": "GameConfig",
       "scriptPath": "Assets/ScriptableObjects/GameConfig.cs"
   })
   ```

   Generates:
   ```csharp
   using UnityEngine;

   [CreateAssetMenu(fileName = "GameConfig", menuName = "ScriptableObjects/GameConfig")]
   public class GameConfig : ScriptableObject
   {
       // Add your fields here

   }
   ```

**Common Use Cases:**

- Quickly create new component scripts with proper structure
- Generate data container classes for configuration and assets
- Bootstrap new script files with Unity's standard patterns

**Note:** After generation, use `unity_asset_crud` with `operation="update"` to modify the generated scripts as needed.

#### 8. Tag and Layer Management

Tag and layer management is handled by two tools:

**GameObject Tag/Layer Setting** - Use `unity_gameobject_crud` with `update` operation:

```python
# Set tag on GameObject
unity_gameobject_crud({
    "operation": "update",
    "gameObjectPath": "Player",
    "tag": "Player"
})

# Set layer on GameObject (by name)
unity_gameobject_crud({
    "operation": "update",
    "gameObjectPath": "Player",
    "layer": "UI"
})

# Set layer on GameObject (by number)
unity_gameobject_crud({
    "operation": "update",
    "gameObjectPath": "Player",
    "layer": 5
})

# Set both tag and layer
unity_gameobject_crud({
    "operation": "update",
    "gameObjectPath": "Player",
    "tag": "Player",
    "layer": "Default"
})
```

**Project Tag/Layer Management** - Use `unity_projectSettings_crud` with `tagsLayers` category:

```python
# List all tags and layers
unity_projectSettings_crud({
    "operation": "read",
    "category": "tagsLayers"
})

# Add a new tag
unity_projectSettings_crud({
    "operation": "write",
    "category": "tagsLayers",
    "property": "addTag",
    "value": "Enemy"
})

# Add a new layer
unity_projectSettings_crud({
    "operation": "write",
    "category": "tagsLayers",
    "property": "addLayer",
    "value": "Projectile"
})

# Add a sorting layer (for 2D sprite rendering order)
unity_projectSettings_crud({
    "operation": "write",
    "category": "tagsLayers",
    "property": "addSortingLayer",
    "value": "Background"
})

# Add a rendering layer (for URP/HDRP rendering pipeline)
unity_projectSettings_crud({
    "operation": "write",
    "category": "tagsLayers",
    "property": "addRenderingLayer",
    "value": "Transparent"
})

# Remove a tag
unity_projectSettings_crud({
    "operation": "write",
    "category": "tagsLayers",
    "property": "removeTag",
    "value": "OldTag"
})

# Remove a sorting layer
unity_projectSettings_crud({
    "operation": "write",
    "category": "tagsLayers",
    "property": "removeSortingLayer",
    "value": "OldSortingLayer"
})

# Remove a rendering layer
unity_projectSettings_crud({
    "operation": "write",
    "category": "tagsLayers",
    "property": "removeRenderingLayer",
    "value": "OldRenderingLayer"
})
```

**Layer Types Explained:**

- **Tags**: Used for identifying GameObjects (e.g., "Player", "Enemy"). Maximum 10,000 tags.
- **Layers**: Used for physics collision and rendering culling (0-31). User-defined layers start at index 8.
- **Sorting Layers**: Control 2D sprite rendering order. Used with SpriteRenderer's "Sorting Layer" property.
- **Rendering Layers**: Control which objects are rendered by which lights/cameras in URP/HDRP (0-31). Available in Unity 2022.2+.

**Note:** Layer assignment in `unity_gameobject_crud` only affects the single GameObject. To apply layers recursively to all children, you would need to use batch operations or iterate through children.

#### 9. Constant Conversion (`unity_constant_convert`)

Convert between Unity constants and numeric values (enums, colors, layers).

**Operations:**
- `enumToValue` / `valueToEnum` - Convert enum names ↔ numeric values
- `colorToRGBA` / `rgbaToColor` - Convert Unity color names ↔ RGBA values
- `layerToIndex` / `indexToLayer` - Convert layer names ↔ indices
- `listEnums` / `listColors` / `listLayers` - List available values

**Example:**
```python
# Convert enum to value
unity_constant_convert({
    "operation": "enumToValue",
    "enumType": "UnityEngine.KeyCode",
    "enumValue": "Space"
})
# Returns: 32

# Convert color name to RGBA
unity_constant_convert({
    "operation": "colorToRGBA",
    "colorName": "red"
})
# Returns: {"r": 1, "g": 0, "b": 0, "a": 1}
```

---

### Utility Tools

#### Await Compilation (`unity_await_compilation`)

Wait for Unity compilation to complete (does NOT trigger compilation, only waits). Returns compilation result including success status, error count, error messages, and console logs from the compilation period.

**Example:**
```python
unity_await_compilation({"timeoutSeconds": 60})
```

**Return Value:**
```python
{
    "success": True,
    "completed": True,
    "errorCount": 0,
    "warningCount": 2,
    "errors": [],
    "warnings": ["Warning message 1", "Warning message 2"],
    "elapsedSeconds": 3,
    "consoleLogs": {
        "all": ["All log lines..."],
        "errors": ["Error lines only..."],
        "warnings": ["Warning lines only..."],
        "normal": ["Normal/info lines only..."]
    }
}
```

#### Ping (`unity_ping`)

Verify bridge connectivity and get Unity Editor version.

**Example:**
```python
unity_ping({})
```

---

### Detailed Tool Documentation

The following sections provide complete documentation for select tools:

### Design Pattern Generation (`unity_designPattern_generate`)

The design pattern generation tool provides instant creation of production-ready C# implementations for common Unity design patterns. Instead of writing boilerplate code manually, generate complete, commented, and ready-to-use pattern implementations in seconds!

**Supported Patterns:**

1. **Singleton** - Single instance management with optional persistence
2. **ObjectPool** - Efficient object reuse pattern for performance
3. **StateMachine** - State management with clean transitions
4. **Observer** - Event system for decoupled communication
5. **Command** - Action abstraction with undo/redo support
6. **Factory** - Object creation pattern with prefab management
7. **ServiceLocator** - Global service access pattern

**Basic Usage:**

```python
unity_designPattern_generate({
    "patternType": "singleton",
    "className": "GameManager",
    "scriptPath": "Assets/Scripts/GameManager.cs"
})
```

**Pattern Examples:**

#### 1. Singleton Pattern

Perfect for managers that should have only one instance across the entire game.

```python
# Basic Singleton
unity_designPattern_generate({
    "patternType": "singleton",
    "className": "GameManager",
    "scriptPath": "Assets/Scripts/GameManager.cs",
    "options": {
        "persistent": True,      # Persists across scenes with DontDestroyOnLoad
        "threadSafe": True,      # Thread-safe initialization
        "monoBehaviour": True    # Inherits from MonoBehaviour
    }
})

# Plain C# Singleton (non-MonoBehaviour)
unity_designPattern_generate({
    "patternType": "singleton",
    "className": "ConfigManager",
    "scriptPath": "Assets/Scripts/ConfigManager.cs",
    "namespace": "MyGame.Core",
    "options": {
        "monoBehaviour": False   # Plain C# class
    }
})
```

**Generated Features:**
- Thread-safe lazy initialization
- Automatic instance creation
- DontDestroyOnLoad support for persistence
- Awake() protection against duplicates

#### 2. ObjectPool Pattern

Optimize performance by reusing objects instead of constantly creating/destroying them.

```python
unity_designPattern_generate({
    "patternType": "objectpool",
    "className": "BulletPool",
    "scriptPath": "Assets/Scripts/BulletPool.cs",
    "options": {
        "pooledType": "GameObject",  # Type of object to pool
        "defaultCapacity": "50",     # Initial pool size
        "maxSize": "200"             # Maximum pool size
    }
})
```

**Generated Features:**
- Unity's ObjectPool<T> integration
- Automatic object creation/destruction callbacks
- Configurable capacity and max size
- Get/Release/Clear methods

**Usage Example:**
```csharp
// Get from pool
GameObject bullet = bulletPool.Get();

// Return to pool when done
bulletPool.Release(bullet);
```

#### 3. StateMachine Pattern

Clean state management with enter/exit/update lifecycle.

```python
unity_designPattern_generate({
    "patternType": "statemachine",
    "className": "PlayerStateMachine",
    "scriptPath": "Assets/Scripts/PlayerStateMachine.cs",
    "namespace": "MyGame.Player"
})
```

**Generated Features:**
- IState interface with Enter/Execute/Exit
- State registration and switching
- Example states (Idle, Move)
- Type-safe state transitions

**Usage Example:**
```csharp
// Register states
stateMachine.RegisterState(new IdleState());
stateMachine.RegisterState(new MoveState());

// Change state
stateMachine.ChangeState<MoveState>();
```

#### 4. Observer Pattern (Event System)

Decoupled event-driven communication between game components.

```python
unity_designPattern_generate({
    "patternType": "observer",
    "className": "EventManager",
    "scriptPath": "Assets/Scripts/EventManager.cs"
})
```

**Generated Features:**
- Singleton event manager
- Generic event subscription/publishing
- String-based event names
- Support for typed and parameterless events

**Usage Example:**
```csharp
// Subscribe to event
EventManager.Instance.Subscribe<int>("ScoreChanged", OnScoreChanged);

// Publish event
EventManager.Instance.Publish("ScoreChanged", newScore);

// Unsubscribe
EventManager.Instance.Unsubscribe<int>("ScoreChanged", OnScoreChanged);
```

#### 5. Command Pattern

Action abstraction with built-in undo/redo support - perfect for games with rewind mechanics or level editors.

```python
unity_designPattern_generate({
    "patternType": "command",
    "className": "CommandManager",
    "scriptPath": "Assets/Scripts/CommandManager.cs"
})
```

**Generated Features:**
- ICommand interface with Execute/Undo
- Command history stack
- Undo/Redo functionality
- Example MoveCommand implementation

**Usage Example:**
```csharp
// Execute command
var moveCmd = new MoveCommand(player.transform, newPosition);
commandManager.ExecuteCommand(moveCmd);

// Undo last action
commandManager.Undo();

// Redo
commandManager.Redo();
```

#### 6. Factory Pattern

Centralized object creation with prefab management.

```python
unity_designPattern_generate({
    "patternType": "factory",
    "className": "EnemyFactory",
    "scriptPath": "Assets/Scripts/EnemyFactory.cs",
    "options": {
        "productType": "GameObject"
    }
})
```

**Generated Features:**
- Product ID to prefab mapping
- Inspector-friendly prefab list
- Type-safe product creation
- Position/rotation overloads

**Usage Example:**
```csharp
// Create enemy by ID
GameObject enemy = factory.CreateProduct("zombie");

// Create with position/rotation
GameObject boss = factory.CreateProduct("boss", spawnPos, spawnRot);

// Create with component access
Enemy enemyComponent = factory.CreateProduct<Enemy>("skeleton");
```

#### 7. ServiceLocator Pattern

Global service access for cross-cutting concerns (audio, input, analytics, etc.).

```python
unity_designPattern_generate({
    "patternType": "servicelocator",
    "className": "ServiceLocator",
    "scriptPath": "Assets/Scripts/ServiceLocator.cs"
})
```

**Generated Features:**
- Singleton service registry
- Type-safe service registration/retrieval
- Service existence checks
- Example IAudioService interface

**Usage Example:**
```csharp
// Register service
ServiceLocator.Instance.RegisterService<IAudioService>(new AudioService());

// Get service
IAudioService audio = ServiceLocator.Instance.GetService<IAudioService>();
audio.PlaySound("explosion");

// Check if service exists
if (ServiceLocator.Instance.HasService<IAnalytics>()) {
    // Use analytics
}
```

**Advanced Usage with Namespaces:**

Organize your code with namespaces:

```python
unity_designPattern_generate({
    "patternType": "singleton",
    "className": "AudioManager",
    "scriptPath": "Assets/Scripts/Managers/AudioManager.cs",
    "namespace": "MyGame.Managers",
    "options": {
        "persistent": True,
        "monoBehaviour": True
    }
})
```

**Combining Patterns:**

Create multiple related patterns in one go:

```python
# Create event system and command manager together
unity_script_batch_manage({
    "scripts": [
        {
            "operation": "create",
            "scriptPath": "Assets/Scripts/EventManager.cs",
            "content": generate_observer_pattern("EventManager")
        },
        {
            "operation": "create",
            "scriptPath": "Assets/Scripts/CommandManager.cs",
            "content": generate_command_pattern("CommandManager")
        }
    ]
})
```

**Best Practices:**

1. **Use Singleton for Managers** - GameManager, AudioManager, InputManager
2. **Use ObjectPool for Frequently Spawned Objects** - Bullets, particles, enemies
3. **Use StateMachine for Complex Behavior** - Player states, AI states, UI states
4. **Use Observer for Decoupled Events** - Score changes, game events, achievements
5. **Use Command for Undoable Actions** - Level editors, gameplay rewind mechanics
6. **Use Factory for Runtime Object Creation** - Enemy spawners, item generation
7. **Use ServiceLocator for Cross-Cutting Concerns** - Audio, analytics, localization

**Performance Notes:**

- All patterns are optimized for Unity's lifecycle
- MonoBehaviour patterns use proper initialization (Awake, Start)
- ObjectPool uses Unity's built-in ObjectPool<T> for best performance
- Singleton patterns include thread-safety for multithreaded scenarios

**Return Value:**

```python
{
    "success": True,
    "scriptPath": "Assets/Scripts/GameManager.cs",
    "patternType": "singleton",
    "className": "GameManager",
    "code": "using UnityEngine;...",  # Full generated code
    "message": "Successfully generated singleton pattern for class GameManager"
}
```

**Important Notes:**
- Generated code is production-ready with comments
- All patterns follow Unity best practices
- Code includes example usage in comments
- Automatic compilation is triggered after generation
- Edit generated code to customize for your specific needs

### Hierarchical Menu Creation (`unity_menu_hierarchyCreate`)

Create complete hierarchical menu systems with nested submenus and automatic State pattern navigation. This tool generates the entire UI hierarchy along with a MenuStateMachine script for clean menu navigation.

**Features:**
- **Declarative menu structure** - Define entire menu hierarchy with simple dictionaries
- **Automatic UI generation** - Creates all panels, buttons, and layout groups
- **State pattern navigation** - Generates MenuStateMachine script with clean state transitions
- **Input handling** - Built-in support for keyboard and gamepad navigation
- **Parent-child transitions** - Automatic back buttons for submenu navigation
- **CanvasGroup management** - Smooth show/hide transitions between menus

**Parameters:**

| Parameter | Type | Description | Required | Default |
|-----------|------|-------------|----------|---------|
| `menuName` | string | Name of the root menu container | **Yes** | - |
| `menuStructure` | object | Hierarchical menu structure definition | **Yes** | - |
| `generateStateMachine` | boolean | Generate MenuStateMachine script | No | `true` |
| `stateMachineScriptPath` | string | Path for generated script (e.g., `Assets/Scripts/MenuManager.cs`) | If `generateStateMachine=true` | - |
| `navigationMode` | string | Input mode: `"keyboard"`, `"gamepad"`, or `"both"` | No | `"both"` |
| `buttonWidth` | number | Button width in pixels | No | `200` |
| `buttonHeight` | number | Button height in pixels | No | `50` |
| `spacing` | number | Vertical spacing between buttons in pixels | No | `10` |
| `enableBackNavigation` | boolean | Add "Back" buttons to submenus | No | `true` |

**Menu Structure Definition:**

The `menuStructure` parameter accepts three formats for defining menu items:

1. **Simple button** - String value represents button text:
   ```python
   "ButtonName": "Display Text"
   ```

2. **Button with submenus** - Object with `text` and `submenus`:
   ```python
   "ParentMenu": {
       "text": "Parent Menu Text",
       "submenus": {
           "SubMenu1": "Submenu 1 Text",
           "SubMenu2": "Submenu 2 Text"
       }
   }
   ```

3. **List-based submenus** - Array of submenu items:
   ```python
   "Settings": ["Graphics", "Audio", "Controls"]
   ```

**Example 1: Simple Main Menu**

```python
unity_menu_hierarchyCreate({
    "menuName": "MainMenu",
    "menuStructure": {
        "Play": "Start New Game",
        "Continue": "Continue Game",
        "Options": "Game Options",
        "Quit": "Exit Game"
    },
    "generateStateMachine": True,
    "stateMachineScriptPath": "Assets/Scripts/MainMenuManager.cs",
    "buttonWidth": 250,
    "buttonHeight": 60,
    "spacing": 15
})
```

**Generated UI Structure:**
```
Canvas/
  └─ MainMenu (CanvasGroup, VerticalLayoutGroup)
      ├─ PlayButton
      ├─ ContinueButton
      ├─ OptionsButton
      └─ QuitButton
```

**Example 2: Multi-Level Settings Menu**

```python
unity_menu_hierarchyCreate({
    "menuName": "SettingsMenu",
    "menuStructure": {
        "Graphics": {
            "text": "Graphics Settings",
            "submenus": {
                "Quality": "Quality Level",
                "Resolution": "Screen Resolution",
                "Fullscreen": "Toggle Fullscreen",
                "VSync": "Vertical Sync",
                "Advanced": {
                    "text": "Advanced Graphics",
                    "submenus": {
                        "Shadows": "Shadow Quality",
                        "AntiAliasing": "Anti-Aliasing",
                        "TextureQuality": "Texture Quality"
                    }
                }
            }
        },
        "Audio": {
            "text": "Audio Settings",
            "submenus": {
                "Master": "Master Volume",
                "Music": "Music Volume",
                "SFX": "Sound Effects",
                "Voice": "Voice Volume"
            }
        },
        "Gameplay": {
            "text": "Gameplay Settings",
            "submenus": {
                "Difficulty": "Game Difficulty",
                "Subtitles": "Toggle Subtitles",
                "AutoSave": "Auto-Save",
                "HUD": "HUD Opacity"
            }
        }
    },
    "generateStateMachine": True,
    "stateMachineScriptPath": "Assets/Scripts/SettingsMenuManager.cs",
    "navigationMode": "both",
    "enableBackNavigation": True
})
```

**Generated UI Structure:**
```
Canvas/
  ├─ SettingsMenu (CanvasGroup, VerticalLayoutGroup)
  │   ├─ GraphicsButton
  │   ├─ AudioButton
  │   └─ GameplayButton
  │
  ├─ GraphicsMenu (CanvasGroup, VerticalLayoutGroup)
  │   ├─ BackButton
  │   ├─ QualityButton
  │   ├─ ResolutionButton
  │   ├─ FullscreenButton
  │   ├─ VSyncButton
  │   └─ AdvancedButton
  │
  ├─ AdvancedMenu (CanvasGroup, VerticalLayoutGroup)
  │   ├─ BackButton
  │   ├─ ShadowsButton
  │   ├─ AntiAliasingButton
  │   └─ TextureQualityButton
  │
  ├─ AudioMenu (CanvasGroup, VerticalLayoutGroup)
  │   ├─ BackButton
  │   ├─ MasterButton
  │   ├─ MusicButton
  │   ├─ SFXButton
  │   └─ VoiceButton
  │
  └─ GameplayMenu (CanvasGroup, VerticalLayoutGroup)
      ├─ BackButton
      ├─ DifficultyButton
      ├─ SubtitlesButton
      ├─ AutoSaveButton
      └─ HUDButton
```

**Generated MenuStateMachine Script Features:**

The automatically generated MenuStateMachine script includes:

1. **State Pattern Implementation:**
   - `IMenuState` interface with `Enter()`, `Update()`, and `Exit()` methods
   - `MenuState` concrete implementation for each menu
   - Clean state transitions with lifecycle management

2. **Input Handling:**
   - **Keyboard Navigation:**
     - Arrow keys / WASD for menu item selection
     - Enter / Space for confirmation
   - **Gamepad Navigation:**
     - D-pad / Left stick for menu item selection
     - A button / Submit for confirmation
   - **Visual Feedback:**
     - Automatic button highlighting
     - Focus management

3. **Menu Management:**
   - `ChangeState(menuName)` - Switch between menus
   - `ShowMenu(panel)` / `HideMenu(panel)` - CanvasGroup control
   - Automatic button collection for current menu
   - Selected button index tracking

**Example Generated Code Structure:**

```csharp
public class MenuManager : MonoBehaviour
{
    [SerializeField] private List<CanvasGroup> menuPanels;

    private IMenuState currentState;
    private Dictionary<string, CanvasGroup> menuDict;
    private int selectedButtonIndex;
    private List<Button> currentButtons;

    public void ChangeState(string menuName)
    {
        // Hide all menus
        // Show target menu
        // Update button list
        // Create new state
    }

    private interface IMenuState
    {
        void Enter();
        void Update();
        void Exit();
    }
}
```

**Using the Generated System:**

1. **Attach the MenuStateMachine script** to a GameObject in your scene
2. **Assign menu panels** in the Inspector (drag all CanvasGroup objects)
3. **Wire up button events** in the Inspector or via code:
   ```csharp
   // In button onClick event
   menuManager.ChangeState("SettingsMenu");
   ```

**Best Practices:**

1. **Keep menu depth reasonable** - 2-3 levels max for best UX
2. **Consistent button sizes** - Use same dimensions across menus
3. **Clear naming** - Use descriptive menu and button names
4. **Test navigation** - Verify keyboard and gamepad work correctly
5. **Customize the generated script** - Add transitions, animations, sound effects

**Common Use Cases:**

- **Main Menu** - Play, Continue, Settings, Quit
- **Pause Menu** - Resume, Settings, Main Menu
- **Settings Menu** - Graphics, Audio, Controls with nested options
- **Level Select** - World selection with level submenus
- **Shop/Inventory** - Categories with item submenus

**Return Value:**

```python
{
    "success": True,
    "menuName": "SettingsMenu",
    "menuPath": "Canvas/SettingsMenu",
    "createdMenus": ["SettingsMenu", "GraphicsMenu", "AudioMenu", "GameplayMenu", "AdvancedMenu"],
    "menuStateCount": 5,
    "stateMachineGenerated": True,
    "stateMachineScriptPath": "Assets/Scripts/SettingsMenuManager.cs",
    "message": "Successfully created hierarchical menu 'SettingsMenu' with 5 menu panels"
}
```

**Important Notes:**
- Requires an existing Canvas in the scene (use `unity_scene_quickSetup({"setupType": "UI"})` first)
- All menu panels are created as children of the Canvas
- Generated MenuStateMachine script uses State pattern for clean code
- Supports unlimited menu depth (though 2-3 levels recommended)
- All menus use VerticalLayoutGroup for automatic button positioning
- CanvasGroup components enable smooth show/hide transitions

### Prefab Management (`unity_prefab_crud`)

Comprehensive operations for working with Unity prefabs:

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
- C# tests: `Assets/SkillForUnity/Editor/MCPBridge/Tests/McpCommandProcessorTests.cs`
- Python tests: `SkillForUnity/tests/test_*.py`

## Common Pitfalls

1. **Thread Safety**: Unity API calls must happen on main thread. Use `lock (MainThreadActions)` queue pattern in McpBridgeService.
2. **Asset Paths**: Always use forward slashes and "Assets/" prefix for asset paths.
3. **Component Type Names**: Must be fully qualified (e.g., `UnityEngine.UI.Text`, not just `Text`).
4. **JSON Serialization**: MiniJson.cs is used on C# side (not Newtonsoft). It only supports basic types + List/Dictionary.
5. **WebSocket Protocol**: Unity's WebSocket implementation requires manual HTTP handshake (lines 385-591 in McpBridgeService.cs).
6. **EditorPrefs Cleanup**: Remember to delete EditorPrefs keys after using them to avoid state pollution.
7. **Prefab Paths**: Prefab files must have `.prefab` extension and be under `Assets/` directory.
8. **Prefab Instances**: Operations like `update`, `applyOverrides`, and `revertOverrides` require the GameObject to be a prefab instance (checked via `PrefabUtility.IsPartOfPrefabInstance()`).

## Troubleshooting for Claude

### Error: "Unity bridge is not connected"

**Problem:** The Python MCP server cannot connect to Unity Editor.

**Solutions:**
1. Check Unity Editor is open
2. Go to **Tools > MCP Assistant** in Unity
3. Click **Start Bridge** button
4. Verify the status shows "Connected"
5. Check firewall isn't blocking localhost connections

### Error: "GameObject not found"

**Problem:** Cannot find the specified GameObject by path.

**Solutions:**
1. Use `unity_scene_crud` with `operation="inspect"` to see current hierarchy
2. Check the path is correct (case-sensitive)
3. Verify the GameObject exists in the active scene
4. Use hierarchy path format: `"Parent/Child/Target"`

**Example:**
```python
# Check what exists first (returns one level)
unity_scene_crud({"operation": "inspect", "includeHierarchy": True})

# To explore deeper, inspect specific GameObject by path
unity_gameobject_crud({"operation": "inspect", "gameObjectPath": "Canvas/Panel/Button"})
```

### Error: "Component type not found"

**Problem:** Component type name is not fully qualified.

**Solutions:**
1. Use full namespace: `UnityEngine.UI.Button` not `Button`
2. Common namespaces:
   - UI components: `UnityEngine.UI.*`
   - Standard components: `UnityEngine.*`
   - Custom scripts: `YourNamespace.ClassName`

**Example:**
```python
# Wrong
unity_component_crud({"componentType": "Button"})

# Correct
unity_component_crud({"componentType": "UnityEngine.UI.Button"})
```

### Error: "Must be under a Canvas"

**Problem:** Trying to create UI elements without a Canvas.

**Solution:** Always set up UI scene first:
```python
# Set up UI scene (creates Canvas + EventSystem)
unity_scene_quickSetup({"setupType": "UI"})

# Now you can create UI elements
unity_ugui_createFromTemplate({"template": "Button"})
```

### Error: "Parent must be under a Canvas"

**Problem:** Trying to create UI element under a non-Canvas parent.

**Solution:** Ensure parent path is under Canvas:
```python
# Wrong - "Player" is not under Canvas
unity_ugui_createFromTemplate({"template": "Button", "parentPath": "Player"})

# Correct
unity_ugui_createFromTemplate({"template": "Button", "parentPath": "Canvas"})
```

### Common Mistakes for Claude to Avoid

#### ❌ Mistake 1: Creating UI manually instead of using templates

**Wrong:**
```python
unity_gameobject_crud({"operation": "create", "name": "Button"})
unity_component_crud({"operation": "add", "gameObjectPath": "Button", "componentType": "UnityEngine.UI.Image"})
unity_component_crud({"operation": "add", "gameObjectPath": "Button", "componentType": "UnityEngine.UI.Button"})
unity_gameobject_crud({"operation": "create", "name": "Text", "parentPath": "Button"})
unity_component_crud({"operation": "add", "gameObjectPath": "Button/Text", "componentType": "UnityEngine.UI.Text"})
# ... many more steps
```

**Right:**
```python
unity_ugui_createFromTemplate({"template": "Button", "text": "Click Me!"})
```

#### ❌ Mistake 2: Not checking context before making changes

**Wrong:**
```python
# Directly try to modify something that might not exist
unity_component_crud({"operation": "update", "gameObjectPath": "Player", ...})
```

**Right:**
```python
# Check what exists first
unity_scene_crud({"operation": "inspect", "includeHierarchy": True})

# Then make informed changes
unity_component_crud({"operation": "update", "gameObjectPath": "Player", ...})
```

#### ❌ Mistake 3: Creating menu UI manually instead of using menu builder

**Wrong:**
```python
unity_gameobject_crud({"operation": "create", "name": "MainMenu"})
unity_gameobject_crud({"operation": "create", "name": "PlayButton", "parentPath": "Canvas/MainMenu"})
unity_component_crud({"operation": "add", "gameObjectPath": "Canvas/MainMenu/PlayButton", "componentType": "UnityEngine.UI.Button"})
unity_gameobject_crud({"operation": "create", "name": "SettingsButton", "parentPath": "Canvas/MainMenu"})
unity_component_crud({"operation": "add", "gameObjectPath": "Canvas/MainMenu/SettingsButton", "componentType": "UnityEngine.UI.Button"})
# ... many separate commands
```

**Right:**
```python
unity_menu_hierarchyCreate({
    "menuName": "MainMenu",
    "menuStructure": {
        "Play": "Start Game",
        "Settings": "Game Settings",
        "Quit": "Exit Game"
    },
    "generateStateMachine": True,
    "stateMachineScriptPath": "Assets/Scripts/MenuManager.cs"
})
# Creates complete menu with buttons, layout, and navigation in one command!
```

#### ❌ Mistake 4: Forgetting to set up scene first

**Wrong:**
```python
# Try to create UI without Canvas
unity_ugui_createFromTemplate({"template": "Button"})  # Error!
```

**Right:**
```python
# Set up scene first
unity_scene_quickSetup({"setupType": "UI"})

# Now create UI
unity_ugui_createFromTemplate({"template": "Button"})
```

#### ❌ Mistake 5: Not checking scene context before making changes

**Wrong:**
```python
# Directly try to create something without checking current state
unity_gameobject_crud({"operation": "create", "name": "Player"})
```

**Right:**
```python
# Check scene first
unity_scene_crud({"operation": "inspect", "includeHierarchy": True})

# Then make informed changes based on what exists
unity_gameobject_crud({"operation": "create", "name": "Player"})
```

#### ❌ Mistake 6: Not using templates for script scaffolding

**Less efficient:**
```python
# Creating scripts from scratch - more work, no structure
unity_asset_crud({
    "operation": "create",
    "assetPath": "Assets/Scripts/Player.cs",
    "content": "using UnityEngine;\n\npublic class Player : MonoBehaviour\n{\n    void Start() { }\n    void Update() { }\n}"
})
```

**Better:**
```python
# Use templates for proper scaffolding with all lifecycle methods
unity_script_template_generate({
    "templateType": "MonoBehaviour",
    "className": "Player",
    "scriptPath": "Assets/Scripts/Player.cs",
    "namespace": "MyGame"
})

# Then modify the generated template as needed
unity_asset_crud({
    "operation": "update",
    "assetPath": "Assets/Scripts/Player.cs",
    "content": "using UnityEngine;\n\nnamespace MyGame\n{\n    public class Player : MonoBehaviour\n    {\n        public float speed = 5f;\n        \n        void Update()\n        {\n            // Movement code\n        }\n    }\n}"
})
```

#### ❌ Mistake 7: Editing Unity .meta files

**Wrong:**
```python
# NEVER directly edit .meta files - they're managed by Unity!
unity_asset_crud({
    "operation": "update",
    "assetPath": "Assets/Scripts/Player.cs.meta",
    "content": "fileFormatVersion: 2\nguid: 12345..."
})
```

**Why this is dangerous:**
- Unity automatically generates and manages .meta files
- Each .meta file contains critical GUID information that Unity uses to track references
- Manual editing can break asset references throughout your project
- Can cause scenes, prefabs, and scripts to lose their connections
- May result in "missing script" errors or broken prefabs

**What to do instead:**
- Let Unity manage .meta files automatically
- If you need to fix broken references, use Unity's built-in tools
- Never use Read, Write, or Edit tools on .meta files
- If you accidentally created a .meta file, delete it and let Unity regenerate it

### Performance Tips for Claude

#### General Performance

1. **Use templates** - 10x faster than manual creation
2. **Batch operations** - Reduce round trips to Unity
3. **Use hierarchy builder** - Create entire trees in one command
4. **Check context once** - Don't repeatedly inspect the same thing
5. **Filter context queries** - Use `filter` parameter to narrow results
6. **Query layer by layer** - Hierarchy returns one level; query specific paths for deeper exploration

#### GameObject/Component Inspection Performance

**Fast Inspection Strategies:**

```python
# ⚡ Ultra-fast: Component existence check (0.1s)
unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.CharacterController",
    "includeProperties": False  # Skip all property reading
})

# ⚡ Fast: Specific properties only (0.3s)
unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Transform",
    "propertyFilter": ["position", "rotation"]  # Only these 2 properties
})

# 🐌 Slow: Full inspection (3s)
unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Transform"
    # Reads ALL properties - can be 50+ fields!
})
```

**Multi-Object Operations:**

```python
# ⚡ Fast: Limited batch with error handling (2s for 1000 objects)
unity_component_crud({
    "operation": "addMultiple",
    "pattern": "Enemy*",
    "componentType": "UnityEngine.Rigidbody",
    "maxResults": 1000,  # Limit to prevent timeout
    "stopOnError": False  # Continue on errors
})

# 🐌 Slow: Unlimited batch (timeout risk!)
unity_component_crud({
    "operation": "addMultiple",
    "pattern": "*",  # Matches everything!
    "componentType": "UnityEngine.Rigidbody"
    # No maxResults - could process 10000+ objects!
})
```

**Inspection Workflow:**

```python
# Step 1: Light inspection to see what exists
result = unity_gameobject_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "includeProperties": False  # Fast, just component types
})
print(result["components"])  # ['UnityEngine.Transform', 'UnityEngine.Rigidbody', ...]

# Step 2: Get specific component details you need
transform_data = unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Transform",
    "propertyFilter": ["position", "rotation"]  # Only what you need
})
```

#### Batch Operation Best Practices

**1. Test Small, Scale Up:**

```python
# Test with 10 objects
test_result = unity_component_crud({
    "operation": "updateMultiple",
    "pattern": "Enemy*",
    "componentType": "UnityEngine.Rigidbody",
    "propertyChanges": {"mass": 5.0},
    "maxResults": 10
})

# Check for issues
if test_result["errorCount"] > 0:
    print("Errors found, review before scaling up")
    for error in test_result["errors"]:
        print(f"  {error['gameObject']}: {error['error']}")
else:
    # Scale up to full batch
    final_result = unity_component_crud({
        "operation": "updateMultiple",
        "pattern": "Enemy*",
        "componentType": "UnityEngine.Rigidbody",
        "propertyChanges": {"mass": 5.0},
        "maxResults": 5000
    })
```

**2. Handle Errors Gracefully:**

```python
# Don't stop on first error
result = unity_component_crud({
    "operation": "addMultiple",
    "pattern": "Enemy*",
    "componentType": "UnityEngine.BoxCollider",
    "stopOnError": False,  # Continue processing
    "maxResults": 1000
})

# Review results
print(f"Success: {result['successCount']}/{result['totalCount']}")
print(f"Errors: {result['errorCount']}")

if result["errorCount"] > 0:
    print("\nFailed objects:")
    for error in result["errors"]:
        print(f"  - {error['gameObject']}: {error['error']}")
```

**3. Use Appropriate Limits:**

```python
# For inspection operations
unity_gameobject_crud({
    "operation": "findMultiple",
    "pattern": "Enemy*",
    "maxResults": 100  # Smaller limit for inspection
})

# For modification operations
unity_component_crud({
    "operation": "addMultiple",
    "pattern": "Enemy*",
    "componentType": "UnityEngine.Rigidbody",
    "maxResults": 1000  # Larger limit for modifications
})
```

#### Performance Benchmarks

| Operation | No Optimization | With Optimization | Speedup |
|-----------|----------------|-------------------|---------|
| Inspect GameObject (all components) | 3s | 0.3s (`includeProperties=false`) | **10x** |
| Inspect Component (all properties) | 2s | 0.2s (`includeProperties=false`) | **10x** |
| Inspect Component (specific props) | 2s | 0.5s (`propertyFilter=[...]`) | **4x** |
| Find 10000 GameObjects | Timeout | 2s (`maxResults=1000`) | **No timeout** |
| Add components to 5000 objects | Timeout | 5s (`maxResults=1000`) | **No timeout** |
| Inspect 1000 components | Timeout | 3s (`includeProperties=false`) | **No timeout** |

#### When to Use Each Optimization

| Optimization | Use When | Example |
|--------------|----------|---------|
| `includeProperties=false` | Checking existence only | "Does Player have Rigidbody?" |
| `propertyFilter=[...]` | Need specific properties | "Get player position and rotation" |
| `componentFilter=[...]` | Need specific components | "Get only Transform and Rigidbody" |
| `maxResults=100` | Exploratory queries | "Find first 100 enemies" |
| `maxResults=1000` | Bulk modifications | "Add colliders to all enemies" |
| `stopOnError=false` | Resilient batch ops | "Update all, report failures later" |

## Configuration Files

- **Unity Settings**: `ProjectSettings/McpBridgeSettings.asset` (gitignore recommended if using bridge tokens)
- **Python Dependencies**: Managed by `uv` (no requirements.txt/pyproject.toml in repo)
- **MCP Client Config**: Auto-registered via MCP Server Manager

## Important Notes

- This is a **Unity Editor tool** - it does not work in builds/runtime
- The bridge listens on localhost by default for security
- Setting host to `*` or `0.0.0.0` exposes the bridge to the network (use with caution)
- Bridge token authentication is available but stored in plain text
- The Python server supports both stdio (for MCP clients) and websocket (for HTTP testing)
