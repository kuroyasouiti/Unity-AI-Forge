# UnityMCP - Model Context Protocol Server for Unity

UnityMCP is a Model Context Protocol (MCP) server that enables AI assistants to interact with Unity Editor in real-time. It provides comprehensive tools for scene management, GameObject manipulation, component editing, asset operations, and more.

## Architecture

UnityMCP uses a **bidirectional WebSocket bridge** architecture:

```
AI Client (Claude Code/Cursor) <--(MCP)--> Python Server <--(WebSocket)--> Unity Editor Bridge
```

### Components

1. **Unity C# Bridge** (`Assets/Editor/MCPBridge/`) - WebSocket server running inside Unity Editor
2. **Python MCP Server** (`Assets/Runtime/MCPServer/`) - MCP protocol implementation that connects to the bridge

## Quick Start

### 1. Unity Editor Setup

1. Open your Unity project
2. Import the UnityMCP package
3. Go to **Tools > MCP Assistant**
4. Click **Start Bridge**
5. The bridge will listen on `ws://localhost:7077/bridge` by default

### 2. Python Server Setup

```bash
# Navigate to the MCP server directory
cd Assets/Runtime/MCPServer

# Run with uv (recommended)
uv run main.py

# Or with Python directly
python main.py --transport stdio
```

### 3. Configure MCP Client

Add to your MCP client configuration (e.g., Claude Desktop):

```json
{
  "mcpServers": {
    "unity": {
      "command": "uv",
      "args": ["run", "--directory", "D:/Projects/MCP/Assets/Runtime/MCPServer", "main.py"],
      "env": {
        "MCP_SERVER_TRANSPORT": "stdio",
        "MCP_LOG_LEVEL": "info"
      }
    }
  }
}
```

## Available Tools

### Core Operations

| Tool | Description |
|------|-------------|
| `unity.ping` | Verify bridge connectivity and return heartbeat information |
| `unity.scene.crud` | Create, load, save, delete, or duplicate Unity scenes |
| `unity.gameobject.crud` | Manage scene hierarchy (create, delete, move, rename, duplicate, inspect GameObjects) |
| `unity.component.crud` | Add, remove, update, or inspect components on GameObjects |
| `unity.asset.crud` | Create, update, rename, duplicate, delete, or inspect Assets/ files |

### UGUI Management

| Tool | Description |
|------|-------------|
| `unity.ugui.rectAdjust` | Adjust RectTransform using uGUI layout utilities |
| `unity.ugui.anchorManage` | Manage RectTransform anchors (set custom values, apply presets, convert positioning) |
| `unity.ugui.manage` | **Unified UGUI tool** - Consolidates all UGUI operations in one interface |

### Tags and Layers

| Tool | Description |
|------|-------------|
| `unity.tagLayer.manage` | Manage tags and layers - Set/get on GameObjects, add/remove from project, list all available |

### Prefabs

| Tool | Description |
|------|-------------|
| `unity.prefab.crud` | Create, update, inspect, instantiate, unpack prefabs, apply/revert overrides |

### Project Settings

| Tool | Description |
|------|-------------|
| `unity.projectSettings.crud` | Read/write Unity Project Settings (Player, Quality, Time, Physics, Audio, Editor) |
| `unity.renderPipeline.manage` | Manage render pipeline (Built-in/URP/HDRP), change assets, update settings |

### Input System

| Tool | Description |
|------|-------------|
| `unity.inputSystem.manage` | Manage New Input System - Create Input Action assets, add action maps/actions, configure bindings |

### Utilities

| Tool | Description |
|------|-------------|
| `unity.script.outline` | Produce a summary of a C# script, optionally including member signatures |
| `unity.batch.execute` | Execute multiple Unity operations in a single batch with optional error handling |

## Available Resources

| Resource | Description |
|----------|-------------|
| `unity://project/structure` | Project directory structure and asset listings |
| `unity://editor/log` | Unity Editor log (recent entries) |
| `unity://scene/active` | Active scene hierarchy and GameObject information |
| `unity://scene/list` | List of all scenes in the project |
| `unity://asset/{guid}` | Asset details by GUID |

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `MCP_SERVER_TRANSPORT` | Transport mode: `stdio` or `websocket` | `stdio` |
| `MCP_SERVER_HOST` | WebSocket server host (websocket mode) | `127.0.0.1` |
| `MCP_SERVER_PORT` | WebSocket server port (websocket mode) | `7070` |
| `MCP_BRIDGE_TOKEN` | Optional authentication token | - |
| `MCP_LOG_LEVEL` | Log level: `trace`, `debug`, `info`, `warn`, `error` | `info` |

### Unity Bridge Settings

Configure via **Tools > MCP Assistant** window:

- **Bridge Port**: Port for WebSocket listener (default: 7077)
- **Context Update Interval**: How often to push scene updates (default: 5s)
- **Heartbeat Interval**: Connection health check interval (default: 10s)

## Example Usage

### Create UI Hierarchy

```json
{
  "operations": [
    {
      "tool": "gameObjectManage",
      "payload": {
        "operation": "create",
        "gameObjectPath": "Canvas/Panel",
        "name": "Panel"
      }
    },
    {
      "tool": "componentManage",
      "payload": {
        "operation": "add",
        "gameObjectPath": "Canvas/Panel",
        "componentType": "UnityEngine.UI.Image"
      }
    },
    {
      "tool": "uguiManage",
      "payload": {
        "operation": "setAnchorPreset",
        "gameObjectPath": "Canvas/Panel",
        "preset": "center"
      }
    }
  ]
}
```

### Setup Input Actions

```json
{
  "operations": [
    {
      "tool": "inputSystemManage",
      "payload": {
        "operation": "createAsset",
        "assetPath": "Assets/Input/PlayerControls.inputactions"
      }
    },
    {
      "tool": "inputSystemManage",
      "payload": {
        "operation": "addActionMap",
        "assetPath": "Assets/Input/PlayerControls.inputactions",
        "mapName": "Player"
      }
    },
    {
      "tool": "inputSystemManage",
      "payload": {
        "operation": "addAction",
        "assetPath": "Assets/Input/PlayerControls.inputactions",
        "mapName": "Player",
        "actionName": "Jump",
        "actionType": "Button"
      }
    }
  ]
}
```

### Manage Tags and Layers

```json
{
  "tool": "tagLayerManage",
  "payload": {
    "operation": "addTag",
    "tag": "Enemy"
  }
}
```

```json
{
  "tool": "tagLayerManage",
  "payload": {
    "operation": "setLayerRecursive",
    "gameObjectPath": "Environment",
    "layer": "Environment"
  }
}
```

## Development

### File Structure

```
Assets/
├── Editor/
│   └── MCPBridge/              # Unity C# bridge
│       ├── McpBridgeService.cs      # WebSocket server
│       ├── McpCommandProcessor.cs   # Tool execution
│       └── McpContextCollector.cs   # Context gathering
└── Runtime/
    └── MCPServer/              # Python MCP server
        ├── main.py                  # Server entrypoint
        ├── bridge/                  # WebSocket client
        │   └── bridge_manager.py
        └── tools/                   # Tool definitions
            └── register_tools.py
```

### Adding New Tools

See [CLAUDE.md](./CLAUDE.md) for detailed development guide.

## Features

- ✅ Real-time Unity Editor integration
- ✅ Comprehensive scene and GameObject management
- ✅ Component manipulation with property updates
- ✅ Asset creation and modification
- ✅ UGUI layout and positioning tools
- ✅ Tag and layer management
- ✅ Prefab workflow support
- ✅ Project settings configuration
- ✅ Input System integration
- ✅ Batch operation execution
- ✅ Automatic reconnection after compilation
- ✅ Context-aware assistance with scene state

## Requirements

- Unity 2021.3 or later
- Python 3.10 or later
- uv (recommended) or pip

## Troubleshooting

### Bridge not connecting

1. Check Unity Console for errors
2. Verify bridge is started in **Tools > MCP Assistant**
3. Ensure no firewall blocking localhost:7077
4. Check Python server logs for connection errors

### Tools failing

1. Verify GameObject paths are correct (use hierarchy paths like "Canvas/Panel/Button")
2. Check component type names are fully qualified (e.g., "UnityEngine.UI.Text")
3. Review Unity Console for detailed error messages

### After compilation

The bridge automatically saves connection state and reconnects after Unity recompiles scripts.

## License

[Add your license here]

## Contributing

Contributions are welcome! Please read the development guide in [CLAUDE.md](./CLAUDE.md).
