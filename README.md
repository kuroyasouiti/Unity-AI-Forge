# UnityMCP - Model Context Protocol Server for Unity

UnityMCP is a comprehensive Model Context Protocol (MCP) server that enables AI assistants to interact with Unity Editor in real-time. It provides extensive tools for scene management, GameObject manipulation, component editing, asset operations, 2D Tilemap design, NavMesh navigation, UI layout, prefabs, input systems, and more.

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

| Tool | Description | Key Operations |
|------|-------------|----------------|
| `unity.ping` | Verify bridge connectivity | Returns Unity version, project name, timestamp |
| `unity.scene.crud` | Scene management | create, load, save, delete, duplicate scenes |
| `unity.gameobject.crud` | GameObject hierarchy management | create, delete, move, rename, duplicate, inspect GameObjects |
| `unity.component.crud` | Component operations | add, remove, update, inspect components on GameObjects |
| `unity.asset.crud` | Asset file operations | create, update, rename, duplicate, delete, inspect Assets/ files |

**Scene Management (`unity.scene.crud`)**
- Create new scenes with default GameObjects
- Load scenes additively or single mode
- Save active or all open scenes
- Delete and duplicate scenes
- Full AssetDatabase integration

**GameObject Management (`unity.gameobject.crud`)**
- Create GameObjects with parent hierarchy
- Move GameObjects in hierarchy
- Rename and duplicate with children
- Inspect to see all attached components and properties
- Delete with undo support

**Component Management (`unity.component.crud`)**
- Add components by fully qualified type name
- Update component properties with dictionary-based changes
- Support for Unity Object references (meshes, materials, sprites)
- Built-in resource loading (`Library/unity default resources::`)
- Inspect component state and properties

**Asset Management (`unity.asset.crud`)**
- Create text-based assets (C# scripts, JSON, configs)
- Update existing asset contents
- Rename, duplicate, delete assets
- Inspect asset metadata and contents
- Auto-refresh AssetDatabase

---

### 2D Tilemap System

| Tool | Description | Key Operations |
|------|-------------|----------------|
| `unity.tilemap.manage` | 2D Tilemap operations | createTilemap, setTile, getTile, clearTile, fillArea, inspectTilemap, clearAll |

**Tilemap Management (`unity.tilemap.manage`)**
- **createTilemap**: Create new Tilemap with Grid parent automatically
- **setTile**: Place tiles at specific grid coordinates (X, Y, Z)
- **getTile**: Retrieve tile information at position
- **clearTile**: Remove tile from position
- **fillArea**: Fill rectangular areas with tiles efficiently
- **inspectTilemap**: Get bounds, tile count, and statistics
- **clearAll**: Remove all tiles from Tilemap

**Features:**
- Automatic Grid parent creation
- Support for 3D tile positioning (Z-axis)
- Efficient area filling with undo support
- Integration with Unity's 2D Tilemap system
- Perfect for level design and procedural generation

**Example - Create 2D Level:**
```json
{
  "tool": "tilemapManage",
  "payload": {
    "operation": "fillArea",
    "gameObjectPath": "Grid/Level1",
    "tileAssetPath": "Assets/Tiles/Ground.asset",
    "startX": 0,
    "startY": 0,
    "endX": 20,
    "endY": 15
  }
}
```

---

### Navigation System (NavMesh)

| Tool | Description | Key Operations |
|------|-------------|----------------|
| `unity.navmesh.manage` | NavMesh and AI navigation | bakeNavMesh, clearNavMesh, addNavMeshAgent, setDestination, inspectNavMesh, createNavMeshSurface |

**NavMesh Management (`unity.navmesh.manage`)**
- **bakeNavMesh**: Bake navigation mesh for current scene
- **clearNavMesh**: Clear all baked NavMesh data
- **addNavMeshAgent**: Add NavMeshAgent component with configuration
  - Configure speed, acceleration, stopping distance
  - Set agent properties (radius, height)
- **setDestination**: Set NavMeshAgent target destination
  - Returns path status (hasPath, pathPending)
- **inspectNavMesh**: Get NavMesh statistics
  - Triangulation data (vertices, triangles, areas)
  - Agent settings (radius, height, slope, climb)
- **updateSettings**: View current NavMesh bake settings (read-only)
- **createNavMeshSurface**: Add NavMeshSurface component (requires NavMesh Components package)

**Features:**
- Real-time NavMesh baking
- Full NavMeshAgent configuration
- Runtime pathfinding control
- NavMesh statistics and debugging
- Support for NavMesh Components package

**Example - Setup AI Agent:**
```json
{
  "operations": [
    {
      "tool": "navmeshManage",
      "payload": {
        "operation": "addNavMeshAgent",
        "gameObjectPath": "Enemy",
        "agentSpeed": 3.5,
        "agentStoppingDistance": 1.0
      }
    },
    {
      "tool": "navmeshManage",
      "payload": {
        "operation": "setDestination",
        "gameObjectPath": "Enemy",
        "destinationX": 10.0,
        "destinationY": 0.0,
        "destinationZ": 5.0
      }
    }
  ]
}
```

---

### UI System (UGUI)

| Tool | Description | Key Operations |
|------|-------------|----------------|
| `unity.ugui.manage` | **Unified UGUI management** | rectAdjust, setAnchor, setAnchorPreset, convertToAnchored, convertToAbsolute, inspect, updateRect |
| `unity.ugui.rectAdjust` | Adjust RectTransform size | Layout-based size adjustments |
| `unity.ugui.anchorManage` | RectTransform anchor management | Custom anchors, presets, position conversion |

**Unified UGUI Tool (`unity.ugui.manage`)** - Recommended
- **rectAdjust**: Adjust RectTransform based on world corners
- **setAnchor**: Set custom anchor values (min/max X/Y)
- **setAnchorPreset**: Apply common presets (top-left, center, stretch, etc.)
- **convertToAnchored**: Convert absolute to anchored positioning
- **convertToAbsolute**: Convert anchored to absolute positioning
- **inspect**: Get RectTransform state
- **updateRect**: Update RectTransform properties directly

**Anchor Presets:**
- Position: top-left, top-center, top-right, middle-left, center, middle-right, bottom-left, bottom-center, bottom-right
- Stretch: stretch-horizontal, stretch-vertical, stretch-all, stretch-top, stretch-middle, stretch-bottom

**Features:**
- Preserve visual position when changing anchors
- Support for CanvasScaler reference resolution
- Direct property updates (anchoredPosition, sizeDelta, pivot, offsets)
- Complete RectTransform control

---

### Tags and Layers

| Tool | Description | Key Operations |
|------|-------------|----------------|
| `unity.tagLayer.manage` | Tag and layer management | GameObject: setTag, getTag, setLayer, getLayer, setLayerRecursive<br>Project: listTags, addTag, removeTag, listLayers, addLayer, removeLayer |

**Tag and Layer Management (`unity.tagLayer.manage`)**
- **GameObject Operations:**
  - setTag/getTag: Manage tags on individual GameObjects
  - setLayer/getLayer: Set layer by name or index
  - setLayerRecursive: Set layer on GameObject and all children
- **Project Operations:**
  - listTags/listLayers: View all available tags/layers
  - addTag/addLayer: Create new tags/layers in project
  - removeTag/removeLayer: Delete tags/layers from project

**Features:**
- Layer names or indices supported
- Recursive layer setting for hierarchies
- Complete tag/layer project management
- Integration with Physics and rendering systems

---

### Prefabs

| Tool | Description | Key Operations |
|------|-------------|----------------|
| `unity.prefab.crud` | Prefab workflow | create, update, inspect, instantiate, unpack, applyOverrides, revertOverrides |

**Prefab Management (`unity.prefab.crud`)**
- **create**: Create new prefab from scene GameObject
  - Option to include children
- **update**: Update existing prefab from modified instance
- **inspect**: Get prefab asset information
- **instantiate**: Create prefab instance in scene
  - Maintains prefab connection
  - Optional parent specification
- **unpack**: Unpack prefab instance to regular GameObjects
  - OutermostRoot or Completely modes
- **applyOverrides**: Apply instance modifications to prefab asset
- **revertOverrides**: Revert instance to prefab state

**Features:**
- Full prefab workflow support
- Nested prefab handling
- Override management
- Instance tracking

---

### Project Settings

| Tool | Description | Key Operations |
|------|-------------|----------------|
| `unity.projectSettings.crud` | Project settings management | read, write, list settings |
| `unity.renderPipeline.manage` | Render pipeline management | inspect, setAsset, getSettings |

**Project Settings (`unity.projectSettings.crud`)**
- **Categories:**
  - player: PlayerSettings (company name, product name, version, screen settings)
  - quality: QualitySettings (quality levels, shadows, anti-aliasing)
  - time: Time settings (fixedDeltaTime, timeScale)
  - physics: Physics settings (gravity, collision, iterations)
  - audio: AudioSettings (DSP buffer, sample rate, voices)
  - editor: EditorSettings (serialization mode, line endings)

**Render Pipeline (`unity.renderPipeline.manage`)**
- **inspect**: Check current pipeline (Built-in/URP/HDRP/Custom)
- **setAsset**: Change render pipeline asset
- **getSettings**: Read pipeline-specific settings
- Pipeline-specific property access through reflection

---

### Input System

| Tool | Description | Key Operations |
|------|-------------|----------------|
| `unity.inputSystem.manage` | New Input System management | listActions, createAsset, addActionMap, addAction, addBinding, inspectAsset, deleteAsset, deleteActionMap, deleteAction, deleteBinding |

**Input System (`unity.inputSystem.manage`)** - Requires Input System Package
- **Asset Management:**
  - listActions: Find all .inputactions files
  - createAsset: Create new Input Action asset
  - inspectAsset: View action maps and actions
  - deleteAsset: Remove Input Action asset
- **Action Maps:**
  - addActionMap: Add action map to asset
  - deleteActionMap: Remove action map
- **Actions:**
  - addAction: Add action to map (Button, Value, PassThrough)
  - deleteAction: Remove action
- **Bindings:**
  - addBinding: Add input binding to action
  - deleteBinding: Remove specific or all bindings

**Common Binding Paths:**
- Keyboard: `<Keyboard>/space`, `<Keyboard>/w`, `<Keyboard>/escape`
- Mouse: `<Mouse>/leftButton`, `<Mouse>/position`, `<Mouse>/delta`
- Gamepad: `<Gamepad>/buttonSouth`, `<Gamepad>/leftStick`

---

### Utilities

| Tool | Description | Key Operations |
|------|-------------|----------------|
| `unity.script.manage` | C# script management | Read, generate, patch, or delete scripts |
| `unity.batch.execute` | **Batch operations** | Execute multiple tools in one request |

**Script Manage (`unity.script.manage`)**
- Read existing C# files (operation=`read`) with optional member signatures and raw source
- Generate new MonoBehaviour/ScriptableObject/class templates (operation=`create`)
- Apply safe textual updates via ordered edits or remove scripts entirely (operations=`update`/`delete`, with dry-run support)
- Supports GUID or asset path lookup plus namespace/method/field customization

**Batch Execute (`unity.batch.execute`)** - High Performance
- Execute multiple operations sequentially
- Mix any tools (tilemap, navmesh, gameObject, etc.)
- Error handling with stopOnError flag
- Individual operation result tracking
- Perfect for complex scene setups

**Batch Execution Features:**
- **Sequential Processing**: Operations execute in order
- **Error Control**: Continue or stop on first error
- **Result Tracking**: Get success/failure for each operation
- **Tool Mixing**: Combine any Unity tools in one batch
- **Performance**: Reduced network overhead

**Supported in Batch:**
All tools can be used in batch operations: sceneManage, gameObjectManage, componentManage, assetManage, tilemapManage, navmeshManage, uguiManage, tagLayerManage, prefabManage, projectSettingsManage, renderPipelineManage, inputSystemManage

---

## Available Resources

| Resource | Description |
|----------|-------------|
| `unity://project/structure` | Project directory structure and asset listings |
| `unity://editor/log` | Unity Editor log (recent entries) |
| `unity://scene/active` | Active scene hierarchy and GameObject information |
| `unity://scene/list` | List of all scenes in the project |
| `unity://asset/{guid}` | Asset details by GUID |

---

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

---

## Example Usage

### Example 1: Build Complete 2D Level with Navigation

```json
{
  "operations": [
    {
      "tool": "tilemapManage",
      "payload": {
        "operation": "createTilemap",
        "tilemapName": "Arena"
      }
    },
    {
      "tool": "tilemapManage",
      "payload": {
        "operation": "fillArea",
        "gameObjectPath": "Grid/Arena",
        "tileAssetPath": "Assets/Tiles/Floor.asset",
        "startX": 0,
        "startY": 0,
        "endX": 20,
        "endY": 20
      }
    },
    {
      "tool": "navmeshManage",
      "payload": {
        "operation": "bakeNavMesh"
      }
    },
    {
      "tool": "gameObjectManage",
      "payload": {
        "operation": "create",
        "gameObjectPath": "Player",
        "name": "Player"
      }
    },
    {
      "tool": "navmeshManage",
      "payload": {
        "operation": "addNavMeshAgent",
        "gameObjectPath": "Player",
        "agentSpeed": 5.0
      }
    }
  ],
  "stopOnError": true
}
```

### Example 2: Create UI Hierarchy

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
        "preset": "center",
        "preservePosition": true
      }
    },
    {
      "tool": "uguiManage",
      "payload": {
        "operation": "updateRect",
        "gameObjectPath": "Canvas/Panel",
        "sizeDeltaX": 200,
        "sizeDeltaY": 100
      }
    }
  ]
}
```

### Example 3: Setup Input System

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
        "actionName": "Move",
        "actionType": "Value"
      }
    },
    {
      "tool": "inputSystemManage",
      "payload": {
        "operation": "addBinding",
        "assetPath": "Assets/Input/PlayerControls.inputactions",
        "mapName": "Player",
        "actionName": "Move",
        "path": "<Keyboard>/wasd"
      }
    }
  ]
}
```

### Example 4: Manage Tags and Layers

```json
{
  "operations": [
    {
      "tool": "tagLayerManage",
      "payload": {
        "operation": "addTag",
        "tag": "Enemy"
      }
    },
    {
      "tool": "tagLayerManage",
      "payload": {
        "operation": "addLayer",
        "layer": "Characters"
      }
    },
    {
      "tool": "gameObjectManage",
      "payload": {
        "operation": "create",
        "gameObjectPath": "EnemyGroup",
        "name": "EnemyGroup"
      }
    },
    {
      "tool": "tagLayerManage",
      "payload": {
        "operation": "setLayerRecursive",
        "gameObjectPath": "EnemyGroup",
        "layer": "Characters"
      }
    },
    {
      "tool": "tagLayerManage",
      "payload": {
        "operation": "setTag",
        "gameObjectPath": "EnemyGroup",
        "tag": "Enemy"
      }
    }
  ]
}
```

### Example 5: Prefab Workflow

```json
{
  "operations": [
    {
      "tool": "gameObjectManage",
      "payload": {
        "operation": "create",
        "gameObjectPath": "PlayerCharacter",
        "name": "PlayerCharacter"
      }
    },
    {
      "tool": "componentManage",
      "payload": {
        "operation": "add",
        "gameObjectPath": "PlayerCharacter",
        "componentType": "UnityEngine.CharacterController"
      }
    },
    {
      "tool": "prefabManage",
      "payload": {
        "operation": "create",
        "gameObjectPath": "PlayerCharacter",
        "prefabPath": "Assets/Prefabs/Player.prefab",
        "includeChildren": true
      }
    },
    {
      "tool": "prefabManage",
      "payload": {
        "operation": "instantiate",
        "prefabPath": "Assets/Prefabs/Player.prefab",
        "parentPath": "GameWorld"
      }
    }
  ]
}
```

---

## Development

### File Structure

```
Assets/
├── Editor/
│   └── MCPBridge/                    # Unity C# bridge
│       ├── McpBridgeService.cs            # WebSocket server
│       ├── McpCommandProcessor.cs         # Tool execution (4700+ lines)
│       ├── McpContextCollector.cs         # Context gathering
│       ├── McpBridgeWindow.cs             # Unity Editor UI
│       └── McpBridgeSettings.cs           # Configuration
└── Runtime/
    └── MCPServer/                    # Python MCP server
        ├── main.py                        # Server entrypoint
        ├── bridge/                        # WebSocket client
        │   ├── bridge_manager.py          # Connection management
        │   └── messages.py                # Message protocol
        ├── tools/                         # Tool definitions
        │   └── register_tools.py          # All tool schemas (800+ lines)
        └── resources/                     # Resource providers
            └── register_resources.py
```

### Adding New Tools

See detailed guide in:
- [CLAUDE.md](../CLAUDE.md) - Complete development documentation
- [TILEMAP_NAVMESH_TOOLS.md](../TILEMAP_NAVMESH_TOOLS.md) - TileMap & NavMesh tool reference
- [BATCH_PROCESSING_EXAMPLES.md](../BATCH_PROCESSING_EXAMPLES.md) - Batch operation examples

---

## Features

### Core Features
- ✅ Real-time Unity Editor integration via WebSocket
- ✅ Comprehensive scene and GameObject management
- ✅ Component manipulation with property updates
- ✅ Asset creation and modification
- ✅ Automatic reconnection after compilation

### 2D & Navigation
- ✅ **2D Tilemap system** - Grid-based tile placement and area filling
- ✅ **NavMesh system** - AI pathfinding and navigation
- ✅ Real-time NavMesh baking and agent control

### UI & Layout
- ✅ UGUI layout and positioning tools
- ✅ RectTransform anchor management
- ✅ Position conversion (anchored ↔ absolute)
- ✅ Anchor presets (stretch, center, corners)

### Advanced Systems
- ✅ Tag and layer management (GameObject & Project)
- ✅ Prefab workflow (create, update, instantiate, override management)
- ✅ Project settings configuration (6 categories)
- ✅ Render pipeline management (Built-in/URP/HDRP)
- ✅ Input System integration (New Input System)

### Developer Tools
- ✅ **Batch operation execution** - Combine multiple operations
- ✅ C# script read (structure + source)
- ✅ Context-aware assistance with scene state
- ✅ Structured error handling and reporting

---

## Tool Reference Summary

| Category | Tools | Operations |
|----------|-------|------------|
| **Core** | 5 tools | ping, scenes, GameObjects, components, assets |
| **2D** | 1 tool | Tilemap (7 operations) |
| **Navigation** | 1 tool | NavMesh (7 operations) |
| **UI** | 3 tools | UGUI unified + specialized tools |
| **Systems** | 5 tools | Tags/Layers, Prefabs, Settings, Render Pipeline, Input |
| **Utilities** | 2 tools | Script read, Batch execute |
| **Total** | **17 tools** | **100+ operations** |

---

## Requirements

- Unity 2021.3 or later (2022.3+ recommended)
- Python 3.10 or later
- uv (recommended) or pip
- Optional: Input System package (for `unity.inputSystem.manage`)
- Optional: NavMesh Components package (for `createNavMeshSurface`)

---

## Troubleshooting

### Bridge not connecting

1. Check Unity Console for errors
2. Verify bridge is started in **Tools > MCP Assistant**
3. Ensure no firewall blocking localhost:7077
4. Check Python server logs for connection errors

### Tools failing

1. Verify GameObject paths are correct (use hierarchy paths like "Canvas/Panel/Button")
2. Check component type names are fully qualified (e.g., "UnityEngine.UI.Text")
3. For Tilemap: ensure Grid and Tilemap components exist
4. For NavMesh: ensure NavMesh is baked before adding agents
5. Review Unity Console for detailed error messages

### After compilation

The bridge automatically saves connection state and reconnects after Unity recompiles scripts. No manual intervention needed.

### TileMap issues

- Verify tile assets exist at specified paths
- Check Grid and Tilemap hierarchy structure
- Use `inspectTilemap` to verify state

### NavMesh issues

- Ensure geometry is marked as Navigation Static
- Bake NavMesh before testing agents
- Check NavMesh visualization in Scene view (Window > AI > Navigation)
- `updateSettings` is read-only - use Unity Navigation window for bake settings

---

## Performance Tips

1. **Use Batch Operations**: Combine multiple operations for better performance
2. **Limit Context Updates**: Increase context update interval for large scenes
3. **Batch Tilemap Operations**: Use `fillArea` instead of multiple `setTile` calls
4. **Cache Asset References**: Load assets once, reuse in multiple operations
5. **Stop on Error**: Set `stopOnError: true` for dependent operations

---

## Documentation

- **Main Documentation**: [CLAUDE.md](../CLAUDE.md)
- **TileMap & NavMesh Guide**: [TILEMAP_NAVMESH_TOOLS.md](../TILEMAP_NAVMESH_TOOLS.md)
- **Batch Processing Examples**: [BATCH_PROCESSING_EXAMPLES.md](../BATCH_PROCESSING_EXAMPLES.md)
- **This File**: Complete tool reference and quick start

---

## License

[Add your license here]

## Contributing

Contributions are welcome! Please read the development guide in [CLAUDE.md](../CLAUDE.md).

## Support

For issues, questions, or feature requests:
1. Check Unity Console for error messages
2. Review documentation in [CLAUDE.md](../CLAUDE.md)
3. Check example batch operations in [BATCH_PROCESSING_EXAMPLES.md](../BATCH_PROCESSING_EXAMPLES.md)
4. Create an issue on the project repository

---

**UnityMCP** - Comprehensive Unity Editor automation through Model Context Protocol
