# TileMap and NavMesh Tools Documentation

This document describes the newly added TileMap and NavMesh management tools for UnityMCP.

## TileMap Management Tool (`unity.tilemap.manage`)

The TileMap management tool provides comprehensive operations for working with Unity's 2D Tilemap system.

### Operations

#### 1. createTilemap - Create a new Tilemap

Creates a new Tilemap GameObject with a Grid parent.

**Parameters:**
- `operation`: "createTilemap" (required)
- `tilemapName`: Name for the new Tilemap (optional, default: "Tilemap")
- `parentPath`: Hierarchy path for the parent GameObject (optional)

**Example:**
```json
{
  "operation": "createTilemap",
  "tilemapName": "LevelTilemap",
  "parentPath": "Environment"
}
```

**Response:**
```json
{
  "gridPath": "Grid",
  "tilemapPath": "Grid/LevelTilemap",
  "success": true
}
```

#### 2. setTile - Place a tile at a position

Sets a tile at the specified grid position.

**Parameters:**
- `operation`: "setTile" (required)
- `gameObjectPath`: Path to the Tilemap GameObject (required)
- `tileAssetPath`: Asset path to the tile (e.g., "Assets/Tiles/GroundTile.asset") (required)
- `positionX`: X coordinate in grid space (required)
- `positionY`: Y coordinate in grid space (required)
- `positionZ`: Z coordinate in grid space (optional, default: 0)

**Example:**
```json
{
  "operation": "setTile",
  "gameObjectPath": "Grid/Tilemap",
  "tileAssetPath": "Assets/Tiles/GroundTile.asset",
  "positionX": 0,
  "positionY": 0,
  "positionZ": 0
}
```

**Response:**
```json
{
  "success": true,
  "position": {
    "x": 0,
    "y": 0,
    "z": 0
  }
}
```

#### 3. getTile - Get tile information at a position

Retrieves information about the tile at the specified grid position.

**Parameters:**
- `operation`: "getTile" (required)
- `gameObjectPath`: Path to the Tilemap GameObject (required)
- `positionX`: X coordinate in grid space (required)
- `positionY`: Y coordinate in grid space (required)
- `positionZ`: Z coordinate in grid space (optional, default: 0)

**Example:**
```json
{
  "operation": "getTile",
  "gameObjectPath": "Grid/Tilemap",
  "positionX": 0,
  "positionY": 0
}
```

**Response:**
```json
{
  "hasTile": true,
  "tileName": "GroundTile",
  "position": {
    "x": 0,
    "y": 0,
    "z": 0
  }
}
```

#### 4. clearTile - Remove a tile at a position

Removes the tile at the specified grid position.

**Parameters:**
- `operation`: "clearTile" (required)
- `gameObjectPath`: Path to the Tilemap GameObject (required)
- `positionX`: X coordinate in grid space (required)
- `positionY`: Y coordinate in grid space (required)
- `positionZ`: Z coordinate in grid space (optional, default: 0)

**Example:**
```json
{
  "operation": "clearTile",
  "gameObjectPath": "Grid/Tilemap",
  "positionX": 0,
  "positionY": 0
}
```

#### 5. fillArea - Fill a rectangular area with tiles

Fills a rectangular area with the specified tile.

**Parameters:**
- `operation`: "fillArea" (required)
- `gameObjectPath`: Path to the Tilemap GameObject (required)
- `tileAssetPath`: Asset path to the tile (required)
- `startX`: Start X coordinate (required)
- `startY`: Start Y coordinate (required)
- `endX`: End X coordinate (required)
- `endY`: End Y coordinate (required)

**Example:**
```json
{
  "operation": "fillArea",
  "gameObjectPath": "Grid/Tilemap",
  "tileAssetPath": "Assets/Tiles/GroundTile.asset",
  "startX": 0,
  "startY": 0,
  "endX": 10,
  "endY": 10
}
```

**Response:**
```json
{
  "success": true,
  "tilesSet": 121,
  "area": {
    "startX": 0,
    "startY": 0,
    "endX": 10,
    "endY": 10
  }
}
```

#### 6. inspectTilemap - Get Tilemap information

Returns information about the Tilemap including bounds and tile count.

**Parameters:**
- `operation`: "inspectTilemap" (required)
- `gameObjectPath`: Path to the Tilemap GameObject (required)

**Example:**
```json
{
  "operation": "inspectTilemap",
  "gameObjectPath": "Grid/Tilemap"
}
```

**Response:**
```json
{
  "gameObjectPath": "Grid/Tilemap",
  "tileCount": 121,
  "bounds": {
    "xMin": 0,
    "yMin": 0,
    "zMin": 0,
    "xMax": 11,
    "yMax": 11,
    "zMax": 1,
    "size": {
      "x": 11,
      "y": 11,
      "z": 1
    }
  }
}
```

#### 7. clearAll - Clear all tiles from Tilemap

Removes all tiles from the Tilemap.

**Parameters:**
- `operation`: "clearAll" (required)
- `gameObjectPath`: Path to the Tilemap GameObject (required)

**Example:**
```json
{
  "operation": "clearAll",
  "gameObjectPath": "Grid/Tilemap"
}
```

---

## NavMesh Management Tool (`unity.navmesh.manage`)

The NavMesh management tool provides operations for working with Unity's Navigation system.

### Operations

#### 1. bakeNavMesh - Bake the NavMesh

Bakes the NavMesh for the current scene.

**Parameters:**
- `operation`: "bakeNavMesh" (required)

**Example:**
```json
{
  "operation": "bakeNavMesh"
}
```

**Response:**
```json
{
  "success": true,
  "message": "NavMesh bake completed"
}
```

#### 2. clearNavMesh - Clear baked NavMesh data

Clears all baked NavMesh data from the scene.

**Parameters:**
- `operation`: "clearNavMesh" (required)

**Example:**
```json
{
  "operation": "clearNavMesh"
}
```

**Response:**
```json
{
  "success": true,
  "message": "NavMesh cleared"
}
```

#### 3. addNavMeshAgent - Add NavMeshAgent component

Adds a NavMeshAgent component to a GameObject and optionally configures it.

**Parameters:**
- `operation`: "addNavMeshAgent" (required)
- `gameObjectPath`: Path to the GameObject (required)
- `agentSpeed`: Agent movement speed (optional)
- `agentAcceleration`: Agent acceleration (optional)
- `agentStoppingDistance`: Stopping distance from target (optional)

**Example:**
```json
{
  "operation": "addNavMeshAgent",
  "gameObjectPath": "Player",
  "agentSpeed": 3.5,
  "agentAcceleration": 8.0,
  "agentStoppingDistance": 0.5
}
```

**Response:**
```json
{
  "success": true,
  "gameObjectPath": "Player",
  "agentProperties": {
    "speed": 3.5,
    "acceleration": 8.0,
    "stoppingDistance": 0.5,
    "radius": 0.5,
    "height": 2.0
  }
}
```

#### 4. setDestination - Set NavMeshAgent destination

Sets the destination for a NavMeshAgent to navigate to.

**Parameters:**
- `operation`: "setDestination" (required)
- `gameObjectPath`: Path to the GameObject with NavMeshAgent (required)
- `destinationX`: X coordinate of destination (required)
- `destinationY`: Y coordinate of destination (required)
- `destinationZ`: Z coordinate of destination (required)

**Example:**
```json
{
  "operation": "setDestination",
  "gameObjectPath": "Player",
  "destinationX": 10.0,
  "destinationY": 0.0,
  "destinationZ": 5.0
}
```

**Response:**
```json
{
  "success": true,
  "destination": {
    "x": 10.0,
    "y": 0.0,
    "z": 5.0
  },
  "hasPath": true,
  "pathPending": false
}
```

#### 5. inspectNavMesh - Get NavMesh information

Returns statistics and settings about the baked NavMesh.

**Parameters:**
- `operation`: "inspectNavMesh" (required)

**Example:**
```json
{
  "operation": "inspectNavMesh"
}
```

**Response:**
```json
{
  "triangulation": {
    "vertexCount": 1024,
    "triangleCount": 512,
    "areaCount": 512
  },
  "settings": {
    "agentTypeID": 0,
    "agentRadius": 0.5,
    "agentHeight": 2.0,
    "agentSlope": 45.0,
    "agentClimb": 0.4
  }
}
```

#### 6. updateSettings - Update NavMesh bake settings

**Note:** This operation is currently not supported due to Unity API limitations. NavMesh bake settings must be modified through Unity's Navigation window (Window > AI > Navigation).

**Parameters:**
- `operation`: "updateSettings" (required)
- `settings`: Dictionary of settings to update (required)
  - `agentRadius`: Agent radius (optional)
  - `agentHeight`: Agent height (optional)
  - `agentSlope`: Maximum slope angle (optional)
  - `agentClimb`: Maximum step height (optional)

**Example:**
```json
{
  "operation": "updateSettings",
  "settings": {
    "agentRadius": 0.6,
    "agentHeight": 2.0,
    "agentSlope": 50.0,
    "agentClimb": 0.5
  }
}
```

**Response:**
```json
{
  "success": false,
  "message": "NavMesh settings modification is currently not supported. Please use Unity's Navigation window (Window > AI > Navigation) to modify NavMesh bake settings.",
  "currentSettings": {
    "agentTypeID": 0,
    "agentRadius": 0.5,
    "agentHeight": 2.0,
    "agentSlope": 45.0,
    "agentClimb": 0.4
  },
  "requestedSettings": {
    "agentRadius": 0.6,
    "agentHeight": 2.0,
    "agentSlope": 50.0,
    "agentClimb": 0.5
  }
}
```

#### 7. createNavMeshSurface - Create NavMeshSurface component

Creates a NavMeshSurface component on a GameObject. Requires the NavMesh Components package to be installed.

**Parameters:**
- `operation`: "createNavMeshSurface" (required)
- `gameObjectPath`: Path to the GameObject (required)

**Example:**
```json
{
  "operation": "createNavMeshSurface",
  "gameObjectPath": "Floor"
}
```

**Response:**
```json
{
  "success": true,
  "gameObjectPath": "Floor",
  "componentType": "Unity.AI.Navigation.NavMeshSurface"
}
```

**Note:** If the NavMesh Components package is not installed, this operation will return an error message instructing you to install the package from the Package Manager.

---

## Batch Processing Support

Both TileMap and NavMesh tools are fully supported in batch operations using `unity.batch.execute`. This allows you to perform multiple operations efficiently in a single request.

### Batch TileMap Example

```json
{
  "operations": [
    {
      "tool": "tilemapManage",
      "payload": {
        "operation": "createTilemap",
        "tilemapName": "LevelMap"
      }
    },
    {
      "tool": "tilemapManage",
      "payload": {
        "operation": "fillArea",
        "gameObjectPath": "Grid/LevelMap",
        "tileAssetPath": "Assets/Tiles/GroundTile.asset",
        "startX": 0,
        "startY": 0,
        "endX": 10,
        "endY": 10
      }
    },
    {
      "tool": "tilemapManage",
      "payload": {
        "operation": "setTile",
        "gameObjectPath": "Grid/LevelMap",
        "tileAssetPath": "Assets/Tiles/WallTile.asset",
        "positionX": 5,
        "positionY": 5
      }
    }
  ],
  "stopOnError": false
}
```

### Batch NavMesh Example

```json
{
  "operations": [
    {
      "tool": "gameObjectManage",
      "payload": {
        "operation": "create",
        "gameObjectPath": "Enemy1",
        "name": "Enemy1"
      }
    },
    {
      "tool": "navmeshManage",
      "payload": {
        "operation": "addNavMeshAgent",
        "gameObjectPath": "Enemy1",
        "agentSpeed": 3.5,
        "agentStoppingDistance": 1.0
      }
    },
    {
      "tool": "navmeshManage",
      "payload": {
        "operation": "setDestination",
        "gameObjectPath": "Enemy1",
        "destinationX": 10.0,
        "destinationY": 0.0,
        "destinationZ": 5.0
      }
    }
  ],
  "stopOnError": true
}
```

### Mixed Batch Operations

You can mix TileMap, NavMesh, and other Unity operations in a single batch:

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
  "stopOnError": false
}
```

---

## Usage Examples

### Creating a simple 2D level with TileMap

```python
# Create a tilemap
await call_tool("unity.tilemap.manage", {
    "operation": "createTilemap",
    "tilemapName": "Level1"
})

# Fill the ground with tiles
await call_tool("unity.tilemap.manage", {
    "operation": "fillArea",
    "gameObjectPath": "Grid/Level1",
    "tileAssetPath": "Assets/Tiles/GroundTile.asset",
    "startX": 0,
    "startY": 0,
    "endX": 20,
    "endY": 5
})

# Place some obstacles
await call_tool("unity.tilemap.manage", {
    "operation": "setTile",
    "gameObjectPath": "Grid/Level1",
    "tileAssetPath": "Assets/Tiles/WallTile.asset",
    "positionX": 10,
    "positionY": 3
})
```

### Setting up NavMesh for AI pathfinding

```python
# Update NavMesh settings
await call_tool("unity.navmesh.manage", {
    "operation": "updateSettings",
    "settings": {
        "agentRadius": 0.5,
        "agentHeight": 2.0,
        "agentSlope": 45.0
    }
})

# Bake the NavMesh
await call_tool("unity.navmesh.manage", {
    "operation": "bakeNavMesh"
})

# Add NavMeshAgent to an AI character
await call_tool("unity.navmesh.manage", {
    "operation": "addNavMeshAgent",
    "gameObjectPath": "Enemy",
    "agentSpeed": 3.0,
    "agentStoppingDistance": 1.0
})

# Set the agent's destination
await call_tool("unity.navmesh.manage", {
    "operation": "setDestination",
    "gameObjectPath": "Enemy",
    "destinationX": 15.0,
    "destinationY": 0.0,
    "destinationZ": 10.0
})

# Inspect the NavMesh
await call_tool("unity.navmesh.manage", {
    "operation": "inspectNavMesh"
})
```

---

## Implementation Details

### TileMap Tool

The TileMap tool is implemented in:
- **Python Schema**: `Assets/Runtime/MCPServer/tools/register_tools.py` (lines 437-493)
- **C# Handler**: `Assets/Editor/MCPBridge/McpCommandProcessor.cs` (lines 4234-4545)

Key features:
- Creates Tilemap with Grid parent automatically
- Supports 3D positioning (X, Y, Z coordinates)
- Efficient area filling with undo support
- Bounds and tile count inspection

### NavMesh Tool

The NavMesh tool is implemented in:
- **Python Schema**: `Assets/Runtime/MCPServer/tools/register_tools.py` (lines 495-540)
- **C# Handler**: `Assets/Editor/MCPBridge/McpCommandProcessor.cs` (lines 4547-4784)

Key features:
- Bake and clear NavMesh operations
- NavMeshAgent component management
- Runtime destination setting for pathfinding
- NavMesh statistics and settings inspection
- Optional NavMeshSurface support (requires package)

---

## Testing

Tests for both tools are available in:
- `Assets/Runtime/tests/test_tilemap_navmesh.py`

Run tests with:
```bash
python D:\Projects\MCP\Assets\Runtime\tests\test_tilemap_navmesh.py
```

All tests should pass, confirming:
- Tool registration
- Schema validation
- Required field checks
- Property constraints
