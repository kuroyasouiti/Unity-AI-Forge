# TileMap & NavMesh Batch Processing Examples

This document provides practical examples of using TileMap and NavMesh tools with Unity's batch execution system.

## What is Batch Processing?

The `unity.batch.execute` tool allows you to execute multiple Unity operations in a single request. This is more efficient than making separate API calls and ensures operations are executed sequentially in the order specified.

**Benefits:**
- **Performance**: Reduces network overhead by combining multiple operations
- **Atomicity**: Operations execute in sequence with optional error handling
- **Flexibility**: Mix TileMap, NavMesh, and other Unity operations in one batch

## Key Parameters

- `operations`: Array of operations to execute (required)
  - `tool`: Tool name (e.g., "tilemapManage", "navmeshManage")
  - `payload`: Operation-specific parameters
- `stopOnError`: If true, stops execution when an operation fails (default: false)

---

## Example 1: Build a Complete 2D Level

This example creates a tilemap, fills it with tiles, and adds some obstacles:

```json
{
  "operations": [
    {
      "tool": "tilemapManage",
      "payload": {
        "operation": "createTilemap",
        "tilemapName": "MainLevel"
      }
    },
    {
      "tool": "tilemapManage",
      "payload": {
        "operation": "fillArea",
        "gameObjectPath": "Grid/MainLevel",
        "tileAssetPath": "Assets/Tiles/Ground.asset",
        "startX": 0,
        "startY": 0,
        "endX": 20,
        "endY": 15
      }
    },
    {
      "tool": "tilemapManage",
      "payload": {
        "operation": "fillArea",
        "gameObjectPath": "Grid/MainLevel",
        "tileAssetPath": "Assets/Tiles/Wall.asset",
        "startX": 0,
        "startY": 0,
        "endX": 20,
        "endY": 0
      }
    },
    {
      "tool": "tilemapManage",
      "payload": {
        "operation": "fillArea",
        "gameObjectPath": "Grid/MainLevel",
        "tileAssetPath": "Assets/Tiles/Wall.asset",
        "startX": 0,
        "startY": 0,
        "endX": 0,
        "endY": 15
      }
    },
    {
      "tool": "tilemapManage",
      "payload": {
        "operation": "inspectTilemap",
        "gameObjectPath": "Grid/MainLevel"
      }
    }
  ],
  "stopOnError": true
}
```

**Result**: Creates a 21x16 level with ground tiles and walls on the bottom and left edges.

---

## Example 2: Setup Multiple NavMesh Agents

This example creates multiple AI characters and configures them with NavMesh agents:

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
        "agentAcceleration": 8.0,
        "agentStoppingDistance": 1.0
      }
    },
    {
      "tool": "gameObjectManage",
      "payload": {
        "operation": "create",
        "gameObjectPath": "Enemy2",
        "name": "Enemy2"
      }
    },
    {
      "tool": "navmeshManage",
      "payload": {
        "operation": "addNavMeshAgent",
        "gameObjectPath": "Enemy2",
        "agentSpeed": 2.5,
        "agentAcceleration": 6.0,
        "agentStoppingDistance": 1.5
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
        "agentSpeed": 5.0,
        "agentAcceleration": 10.0,
        "agentStoppingDistance": 0.5
      }
    }
  ],
  "stopOnError": true
}
```

**Result**: Creates 2 enemies and 1 player, each with configured NavMeshAgent components.

---

## Example 3: Complete Game Arena Setup

This example combines everything: creates a tilemap arena, bakes NavMesh, and adds AI characters:

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
        "tileAssetPath": "Assets/Tiles/ArenaFloor.asset",
        "startX": -10,
        "startY": -10,
        "endX": 10,
        "endY": 10
      }
    },
    {
      "tool": "gameObjectManage",
      "payload": {
        "operation": "create",
        "gameObjectPath": "ArenaFloor",
        "name": "ArenaFloor"
      }
    },
    {
      "tool": "componentManage",
      "payload": {
        "operation": "add",
        "gameObjectPath": "ArenaFloor",
        "componentType": "UnityEngine.BoxCollider"
      }
    },
    {
      "tool": "componentManage",
      "payload": {
        "operation": "update",
        "gameObjectPath": "ArenaFloor",
        "componentType": "UnityEngine.Transform",
        "propertyChanges": {
          "position": {"x": 0, "y": -0.5, "z": 0},
          "localScale": {"x": 20, "y": 1, "z": 20}
        }
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
        "gameObjectPath": "AIAgent1",
        "name": "AIAgent1"
      }
    },
    {
      "tool": "componentManage",
      "payload": {
        "operation": "update",
        "gameObjectPath": "AIAgent1",
        "componentType": "UnityEngine.Transform",
        "propertyChanges": {
          "position": {"x": -5, "y": 0, "z": -5}
        }
      }
    },
    {
      "tool": "navmeshManage",
      "payload": {
        "operation": "addNavMeshAgent",
        "gameObjectPath": "AIAgent1",
        "agentSpeed": 3.5,
        "agentStoppingDistance": 1.0
      }
    },
    {
      "tool": "navmeshManage",
      "payload": {
        "operation": "setDestination",
        "gameObjectPath": "AIAgent1",
        "destinationX": 5.0,
        "destinationY": 0.0,
        "destinationZ": 5.0
      }
    },
    {
      "tool": "navmeshManage",
      "payload": {
        "operation": "inspectNavMesh"
      }
    }
  ],
  "stopOnError": true
}
```

**Result**: Complete game arena with tilemap floor, NavMesh baked, and an AI agent that starts moving to a destination.

---

## Example 4: Multi-Layer Tilemap Creation

Create multiple tilemap layers for a 2D game with foreground and background:

```json
{
  "operations": [
    {
      "tool": "tilemapManage",
      "payload": {
        "operation": "createTilemap",
        "tilemapName": "Background"
      }
    },
    {
      "tool": "tilemapManage",
      "payload": {
        "operation": "fillArea",
        "gameObjectPath": "Grid/Background",
        "tileAssetPath": "Assets/Tiles/Sky.asset",
        "startX": 0,
        "startY": 0,
        "endX": 30,
        "endY": 20
      }
    },
    {
      "tool": "gameObjectManage",
      "payload": {
        "operation": "create",
        "gameObjectPath": "Grid/Foreground",
        "name": "Foreground",
        "parentPath": "Grid"
      }
    },
    {
      "tool": "componentManage",
      "payload": {
        "operation": "add",
        "gameObjectPath": "Grid/Foreground",
        "componentType": "UnityEngine.Tilemaps.Tilemap"
      }
    },
    {
      "tool": "componentManage",
      "payload": {
        "operation": "add",
        "gameObjectPath": "Grid/Foreground",
        "componentType": "UnityEngine.Tilemaps.TilemapRenderer"
      }
    },
    {
      "tool": "tilemapManage",
      "payload": {
        "operation": "fillArea",
        "gameObjectPath": "Grid/Foreground",
        "tileAssetPath": "Assets/Tiles/Platform.asset",
        "startX": 5,
        "startY": 5,
        "endX": 25,
        "endY": 7
      }
    }
  ],
  "stopOnError": false
}
```

**Result**: Two tilemap layers - a sky background and platform foreground.

---

## Example 5: NavMesh Agent Patrol Setup

Set up multiple patrol points for NavMesh agents:

```json
{
  "operations": [
    {
      "tool": "gameObjectManage",
      "payload": {
        "operation": "create",
        "gameObjectPath": "PatrolAgent",
        "name": "PatrolAgent"
      }
    },
    {
      "tool": "navmeshManage",
      "payload": {
        "operation": "addNavMeshAgent",
        "gameObjectPath": "PatrolAgent",
        "agentSpeed": 3.0,
        "agentStoppingDistance": 0.5
      }
    },
    {
      "tool": "gameObjectManage",
      "payload": {
        "operation": "create",
        "gameObjectPath": "PatrolPoint1",
        "name": "PatrolPoint1"
      }
    },
    {
      "tool": "componentManage",
      "payload": {
        "operation": "update",
        "gameObjectPath": "PatrolPoint1",
        "componentType": "UnityEngine.Transform",
        "propertyChanges": {
          "position": {"x": 10, "y": 0, "z": 10}
        }
      }
    },
    {
      "tool": "gameObjectManage",
      "payload": {
        "operation": "create",
        "gameObjectPath": "PatrolPoint2",
        "name": "PatrolPoint2"
      }
    },
    {
      "tool": "componentManage",
      "payload": {
        "operation": "update",
        "gameObjectPath": "PatrolPoint2",
        "componentType": "UnityEngine.Transform",
        "propertyChanges": {
          "position": {"x": -10, "y": 0, "z": -10}
        }
      }
    },
    {
      "tool": "navmeshManage",
      "payload": {
        "operation": "setDestination",
        "gameObjectPath": "PatrolAgent",
        "destinationX": 10.0,
        "destinationY": 0.0,
        "destinationZ": 10.0
      }
    }
  ],
  "stopOnError": true
}
```

**Result**: A patrol agent and two patrol points set up. The agent will navigate to the first patrol point.

---

## Error Handling

### Stop on Error (stopOnError: true)

When `stopOnError` is true, execution stops at the first error:

```json
{
  "operations": [
    {"tool": "tilemapManage", "payload": {"operation": "createTilemap", "tilemapName": "Level1"}},
    {"tool": "tilemapManage", "payload": {"operation": "setTile", "gameObjectPath": "InvalidPath", "tileAssetPath": "Assets/Tiles/Tile.asset", "positionX": 0, "positionY": 0}},
    {"tool": "tilemapManage", "payload": {"operation": "inspectTilemap", "gameObjectPath": "Grid/Level1"}}
  ],
  "stopOnError": true
}
```

**Result**: First operation succeeds, second fails (invalid path), third is never executed.

### Continue on Error (stopOnError: false)

When `stopOnError` is false, all operations are attempted:

```json
{
  "operations": [
    {"tool": "tilemapManage", "payload": {"operation": "createTilemap", "tilemapName": "Level1"}},
    {"tool": "tilemapManage", "payload": {"operation": "setTile", "gameObjectPath": "InvalidPath", "tileAssetPath": "Assets/Tiles/Tile.asset", "positionX": 0, "positionY": 0}},
    {"tool": "tilemapManage", "payload": {"operation": "inspectTilemap", "gameObjectPath": "Grid/Level1"}}
  ],
  "stopOnError": false
}
```

**Result**: First operation succeeds, second fails (error recorded), third succeeds. All results are returned.

---

## Best Practices

1. **Use stopOnError: true for setup sequences**: When operations depend on each other
2. **Use stopOnError: false for independent operations**: When you want to complete as many operations as possible
3. **Group related operations**: Keep tilemap operations together, NavMesh operations together
4. **Inspect at the end**: Add inspect operations at the end to verify results
5. **Keep batches reasonable**: Don't exceed 20-30 operations per batch for maintainability

---

## Response Format

All batch operations return a standardized response:

```json
{
  "totalOperations": 5,
  "executedOperations": 5,
  "successCount": 4,
  "failureCount": 1,
  "results": [
    {
      "index": 0,
      "success": true,
      "tool": "tilemapManage",
      "result": { "gridPath": "Grid", "tilemapPath": "Grid/Level1", "success": true }
    },
    {
      "index": 1,
      "success": false,
      "tool": "tilemapManage",
      "error": "GameObject not found: InvalidPath"
    },
    ...
  ]
}
```

This allows you to check which operations succeeded and which failed, making debugging easier.
