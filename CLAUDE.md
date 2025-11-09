# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> 💡 **NEW TO UNITYMCP?** Check out the [Quick Start Guide](QUICKSTART.md) for a fast introduction with common commands and examples!

## 🎯 For Claude: How to Use This System Effectively

**You are working with UnityMCP - a powerful system that lets you control Unity Editor directly!**

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

# Build complex hierarchies
unity_hierarchy_builder({"hierarchy": {...}})

# Check current scene (fast)
unity_context_inspect({"includeHierarchy": True, "includeComponents": False})

# Inspect GameObject (fast - no properties)
unity_gameobject_crud({"operation": "inspect", "gameObjectPath": "Player", "includeProperties": False})

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
2. **Check context before making changes** - Use `unity_context_inspect()`
3. **Use hierarchy builder for complex structures** - Create entire trees in one command
4. **Always batch script operations** - Use `unity_script_batch_manage()` with scripts array for all script operations
5. **Optimize inspect operations** - Use `includeProperties=false` for existence checks, `propertyFilter` for specific properties
6. **Limit batch operations** - Use `maxResults` parameter to prevent timeouts (default: 1000)
7. **Handle errors gracefully** - Use `stopOnError=false` in batch operations to continue on failures
8. **Refer to the Quick Start guide** - Contains copy-paste examples for common tasks

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

### Building Complex Hierarchies

Use the hierarchy builder for complex nested structures:

```python
unity_hierarchy_builder({
    "hierarchy": {
        "Player": {
            "components": ["Rigidbody", "CapsuleCollider"],
            "properties": {
                "position": {"x": 0, "y": 1, "z": 0}
            },
            "children": {
                "Camera": {
                    "components": ["Camera"],
                    "properties": {
                        "position": {"x": 0, "y": 0.5, "z": -3}
                    }
                },
                "Weapon": {
                    "components": ["BoxCollider"]
                }
            }
        }
    }
})
```

## Best Practices for Claude

### 1. Always Check Context First

Before making changes, inspect the scene to understand current state:

```python
# Get overview of current scene
unity_context_inspect({
    "includeHierarchy": True,
    "includeComponents": False,
    "maxDepth": 2
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

### 3. Use Hierarchy Builder for Complex Structures

When creating multi-level hierarchies, use hierarchy builder instead of individual commands:

```python
# One command creates entire UI layout
unity_hierarchy_builder({
    "hierarchy": {
        "MainMenu": {
            "components": ["UnityEngine.UI.Image"],
            "children": {
                "Title": {"components": ["UnityEngine.UI.Text"]},
                "ButtonContainer": {
                    "components": ["UnityEngine.UI.VerticalLayoutGroup"],
                    "children": {
                        "PlayButton": {"components": ["UnityEngine.UI.Button", "UnityEngine.UI.Image"]},
                        "SettingsButton": {"components": ["UnityEngine.UI.Button", "UnityEngine.UI.Image"]},
                        "QuitButton": {"components": ["UnityEngine.UI.Button", "UnityEngine.UI.Image"]}
                    }
                }
            }
        }
    },
    "parentPath": "Canvas"
})
```

### 4. Always Use Script Batch Management

**CRITICAL**: Script operations are now BATCH-ONLY. Always use `unity_script_batch_manage` with the `scripts` array, even for single scripts:

✅ **Single script operation:**
```python
unity_script_batch_manage({
    "scripts": [
        {"operation": "create", "scriptPath": "Assets/Scripts/Player.cs", ...}
    ],
    "timeoutSeconds": 30
})
```

✅ **Multiple script operations (recommended):**
```python
unity_script_batch_manage({
    "scripts": [
        {"operation": "create", "scriptPath": "Assets/Scripts/Player.cs", ...},
        {"operation": "create", "scriptPath": "Assets/Scripts/Enemy.cs", ...},
        {"operation": "create", "scriptPath": "Assets/Scripts/Weapon.cs", ...}
    ],
    "stopOnError": False,
    "timeoutSeconds": 30
})
# All scripts created atomically, then single compilation (10-20x faster!)
```

**Benefits of batch-only script management:**
- Consistent API - always use `scripts` array
- 10-20x faster for multiple scripts
- Atomic operations - all succeed or fail together (if stopOnError=true)
- Single consolidated compilation
- Better error reporting with per-script results

## Common Use Cases

### Use Case 1: Create a Game Menu

```python
# Step 1: Setup UI scene
unity_scene_quickSetup({"setupType": "UI"})

# Step 2: Create menu structure
unity_hierarchy_builder({
    "hierarchy": {
        "MenuPanel": {
            "components": ["UnityEngine.UI.Image"],
            "properties": {
                "Image": {"color": {"r": 0, "g": 0, "b": 0, "a": 0.8}}
            },
            "children": {
                "Title": {
                    "components": ["UnityEngine.UI.Text"],
                    "properties": {
                        "Text": {"text": "Main Menu", "fontSize": 48, "alignment": "MiddleCenter"}
                    }
                },
                "ButtonList": {
                    "components": ["UnityEngine.UI.VerticalLayoutGroup"]
                }
            }
        }
    },
    "parentPath": "Canvas"
})

# Step 3: Add buttons to ButtonList
for button_text in ["Start Game", "Options", "Quit"]:
    unity_ugui_createFromTemplate({
        "template": "Button",
        "name": f"{button_text}Button",
        "parentPath": "Canvas/MenuPanel/ButtonList",
        "text": button_text,
        "width": 300,
        "height": 60
    })
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

# Create obstacles using batch execute
unity_batch_execute({
    "operations": [
        {"tool": "gameObjectCreateFromTemplate", "payload": {"template": "Cube", "name": "Obstacle1", "position": {"x": 3, "y": 0.5, "z": 0}}},
        {"tool": "gameObjectCreateFromTemplate", "payload": {"template": "Cube", "name": "Obstacle2", "position": {"x": -3, "y": 0.5, "z": 0}}},
        {"tool": "gameObjectCreateFromTemplate", "payload": {"template": "Cube", "name": "Obstacle3", "position": {"x": 0, "y": 0.5, "z": 3}}}
    ]
})
```

### Use Case 3: Create an Inventory UI

```python
# Create inventory panel with grid layout
unity_hierarchy_builder({
    "hierarchy": {
        "InventoryPanel": {
            "components": ["UnityEngine.UI.Image", "UnityEngine.UI.GridLayoutGroup"],
            "properties": {
                "position": {"x": 0, "y": 0, "z": 0}
            }
        }
    },
    "parentPath": "Canvas"
})

# Configure grid layout
unity_ugui_layoutManage({
    "operation": "update",
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

## Available Tools

### New High-Level Tools (Recommended)

These tools make common Unity tasks much easier:

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

#### 4. Hierarchy Builder (`unity_hierarchy_builder`)

Build complex nested GameObject structures declaratively.

**Example:**
```python
unity_hierarchy_builder({
    "hierarchy": {
        "GameManager": {
            "components": ["MyNamespace.GameManager"],
            "children": {
                "UI": {
                    "children": {
                        "ScoreText": {"components": ["UnityEngine.UI.Text"]},
                        "HealthBar": {"components": ["UnityEngine.UI.Slider"]}
                    }
                },
                "Audio": {
                    "children": {
                        "MusicSource": {"components": ["AudioSource"]},
                        "SFXSource": {"components": ["AudioSource"]}
                    }
                }
            }
        }
    }
})
```

#### 5. Layout Management (`unity_ugui_layoutManage`)

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

#### 6. Context Inspector (`unity_context_inspect`)

Get comprehensive scene information.

**Example:**
```python
unity_context_inspect({
    "includeHierarchy": True,
    "includeComponents": True,
    "maxDepth": 3,
    "filter": "Player*"  # Optional: filter by pattern
})
```

### Asset Management (`unity_asset_crud`)

The asset management tool provides operations for working with Unity asset files.

**IMPORTANT:** DO NOT use this tool for creating or updating C# scripts. Use `unity_script_batch_manage` instead, which handles compilation properly.

**Operations:**

1. **create** - Create a new asset file
   ```python
   unity_asset_crud({
       "operation": "create",
       "assetPath": "Assets/Data/Config.asset",
       "content": "..."
   })
   ```

2. **update** - Update an existing asset file
   ```python
   unity_asset_crud({
       "operation": "update",
       "assetPath": "Assets/Data/Config.asset",
       "content": "..."
   })
   ```

3. **rename** - Rename an asset
   ```python
   unity_asset_crud({
       "operation": "rename",
       "assetPath": "Assets/Textures/OldName.png",
       "newName": "NewName.png"
   })
   ```

4. **duplicate** - Duplicate an asset
   ```python
   unity_asset_crud({
       "operation": "duplicate",
       "assetPath": "Assets/Materials/Material.mat",
       "newPath": "Assets/Materials/Material_Copy.mat"
   })
   ```

5. **delete** - Delete an asset
   ```python
   unity_asset_crud({
       "operation": "delete",
       "assetPath": "Assets/Unused/OldAsset.asset"
   })
   ```

6. **inspect** - Get information about an asset
   ```python
   unity_asset_crud({
       "operation": "inspect",
       "assetPath": "Assets/Textures/Sprite.png"
   })
   ```

7. **findMultiple** - Find multiple assets using wildcard patterns
   ```python
   unity_asset_crud({
       "operation": "findMultiple",
       "pattern": "Assets/Textures/*.png"
   })
   ```

8. **deleteMultiple** - Delete multiple assets using wildcard patterns
   ```python
   unity_asset_crud({
       "operation": "deleteMultiple",
       "pattern": "Assets/Temp/*.asset"
   })
   ```

9. **inspectMultiple** - Inspect multiple assets using wildcard patterns
   ```python
   unity_asset_crud({
       "operation": "inspectMultiple",
       "pattern": "Assets/Materials/*.mat"
   })
   ```

**Important Notes:**
- This tool is for general asset files (textures, materials, prefabs, etc.)
- For C# scripts, ALWAYS use `unity_script_batch_manage` instead
- All asset paths must start with "Assets/"
- Wildcard patterns support glob syntax (*, ?, etc.)

### GameObject Management (`unity_gameobject_crud`)

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

5. **duplicate** - Duplicate a GameObject
   ```python
   unity_gameobject_crud({
       "operation": "duplicate",
       "gameObjectPath": "Enemy",
       "name": "Enemy2",  # Optional
       "parentPath": "Enemies"  # Optional
   })
   ```

6. **inspect** - Inspect a GameObject and its components
   ```python
   # Full inspection (all components with properties)
   unity_gameobject_crud({
       "operation": "inspect",
       "gameObjectPath": "Player"
   })

   # Light inspection (component types only, no properties)
   unity_gameobject_crud({
       "operation": "inspect",
       "gameObjectPath": "Player",
       "includeProperties": False  # 10x faster!
   })

   # Filter specific components
   unity_gameobject_crud({
       "operation": "inspect",
       "gameObjectPath": "Player",
       "componentFilter": ["UnityEngine.Transform", "UnityEngine.Rigidbody"]
   })
   ```

7. **findMultiple** - Find GameObjects by wildcard/regex pattern
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

8. **deleteMultiple** - Delete multiple GameObjects
   ```python
   unity_gameobject_crud({
       "operation": "deleteMultiple",
       "pattern": "Temp*",
       "maxResults": 500
   })
   ```

9. **inspectMultiple** - Inspect multiple GameObjects
   ```python
   unity_gameobject_crud({
       "operation": "inspectMultiple",
       "pattern": "Enemy*",
       "includeComponents": True,  # Include component type names
       "maxResults": 100
   })
   ```

**Performance Parameters:**

- **`includeProperties`** (inspect only): Set to `false` to skip property reading (10x faster)
- **`componentFilter`** (inspect only): Array of component types to inspect
- **`maxResults`** (multiple operations): Maximum objects to process (default: 1000)
- **`includeComponents`** (inspectMultiple only): Include component type names

**Performance Tips:**

```python
# ✅ Fast: Check if GameObject exists and get component list
unity_gameobject_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "includeProperties": False
})

# ✅ Fast: Get only Transform properties
unity_gameobject_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "componentFilter": ["UnityEngine.Transform"]
})

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

### Component Management (`unity_component_crud`)

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
1. Use `unity_context_inspect()` to see current hierarchy
2. Check the path is correct (case-sensitive)
3. Verify the GameObject exists in the active scene
4. Use hierarchy path format: `"Parent/Child/Target"`

**Example:**
```python
# Check what exists first
unity_context_inspect({"includeHierarchy": True, "maxDepth": 2})

# Then use correct path
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
unity_context_inspect({"includeHierarchy": True})

# Then make informed changes
unity_component_crud({"operation": "update", "gameObjectPath": "Player", ...})
```

#### ❌ Mistake 3: Creating hierarchy manually instead of using hierarchy builder

**Wrong:**
```python
unity_gameobject_crud({"operation": "create", "name": "Player"})
unity_component_crud({"operation": "add", "gameObjectPath": "Player", "componentType": "Rigidbody"})
unity_gameobject_crud({"operation": "create", "name": "Camera", "parentPath": "Player"})
unity_component_crud({"operation": "add", "gameObjectPath": "Player/Camera", "componentType": "Camera"})
unity_gameobject_crud({"operation": "create", "name": "Weapon", "parentPath": "Player"})
# ... many more steps
```

**Right:**
```python
unity_hierarchy_builder({
    "hierarchy": {
        "Player": {
            "components": ["Rigidbody"],
            "children": {
                "Camera": {"components": ["Camera"]},
                "Weapon": {"components": ["BoxCollider"]}
            }
        }
    }
})
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

#### ❌ Mistake 5: Not using batch execute for related operations

**Wrong:**
```python
unity_gameobject_createFromTemplate({"template": "Cube", "name": "Wall1", "position": {"x": 5, "y": 0, "z": 0}})
unity_gameobject_createFromTemplate({"template": "Cube", "name": "Wall2", "position": {"x": -5, "y": 0, "z": 0}})
unity_gameobject_createFromTemplate({"template": "Cube", "name": "Wall3", "position": {"x": 0, "y": 0, "z": 5}})
unity_gameobject_createFromTemplate({"template": "Cube", "name": "Wall4", "position": {"x": 0, "y": 0, "z": -5}})
```

**Right:**
```python
unity_batch_execute({
    "operations": [
        {"tool": "gameObjectCreateFromTemplate", "payload": {"template": "Cube", "name": "Wall1", "position": {"x": 5, "y": 0, "z": 0}}},
        {"tool": "gameObjectCreateFromTemplate", "payload": {"template": "Cube", "name": "Wall2", "position": {"x": -5, "y": 0, "z": 0}}},
        {"tool": "gameObjectCreateFromTemplate", "payload": {"template": "Cube", "name": "Wall3", "position": {"x": 0, "y": 0, "z": 5}}},
        {"tool": "gameObjectCreateFromTemplate", "payload": {"template": "Cube", "name": "Wall4", "position": {"x": 0, "y": 0, "z": -5}}}
    ]
})
```

#### ❌ Mistake 6: Using asset management tool for C# scripts

**Wrong:**
```python
# Using asset tool to create scripts - will cause compilation issues!
unity_asset_crud({
    "operation": "create",
    "assetPath": "Assets/Scripts/Player.cs",
    "content": "using UnityEngine; public class Player : MonoBehaviour { }"
})
unity_asset_crud({
    "operation": "create",
    "assetPath": "Assets/Scripts/Enemy.cs",
    "content": "using UnityEngine; public class Enemy : MonoBehaviour { }"
})
```

**Right:**
```python
# Always use script batch manager for C# scripts
unity_script_batch_manage({
    "scripts": [
        {"operation": "create", "scriptPath": "Assets/Scripts/Player.cs", "content": "using UnityEngine;\n\npublic class Player : MonoBehaviour\n{\n}"},
        {"operation": "create", "scriptPath": "Assets/Scripts/Enemy.cs", "content": "using UnityEngine;\n\npublic class Enemy : MonoBehaviour\n{\n}"}
    ],
    "timeoutSeconds": 30
})
# Handles compilation automatically and properly!
```

### Performance Tips for Claude

#### General Performance

1. **Use templates** - 10x faster than manual creation
2. **Batch operations** - Reduce round trips to Unity
3. **Use hierarchy builder** - Create entire trees in one command
4. **Check context once** - Don't repeatedly inspect the same thing
5. **Filter context queries** - Use `maxDepth` and `filter` parameters

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
- **MCP Client Config**: Auto-registered via ServerInstallerUtility.cs

## Important Notes

- This is a **Unity Editor tool** - it does not work in builds/runtime
- The bridge listens on localhost by default for security
- Setting host to `*` or `0.0.0.0` exposes the bridge to the network (use with caution)
- Bridge token authentication is available but stored in plain text
- The Python server supports both stdio (for MCP clients) and websocket (for HTTP testing)
