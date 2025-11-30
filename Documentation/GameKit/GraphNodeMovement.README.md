# GraphNodeMovement - Node-Based Graph Movement

Node-based movement system with A* pathfinding for games with discrete movement spaces.

## Overview

GraphNodeMovement provides a flexible, dimension-agnostic movement system where characters move between defined nodes in a graph. Unlike grid-based movement (which enforces regular spacing), graph movement allows irregular, hand-placed nodes with custom connections.

## Key Concepts

### GraphNode
A point in space that characters can occupy. Nodes can be connected to other nodes with weighted, traversable edges.

### NodeConnection
A directional edge from one node to another with:
- **Cost**: Movement cost (typically distance, but can represent time, danger, etc.)
- **IsTraversable**: Whether the connection can be used (for dynamic obstacle/door mechanics)

### GraphNodeMovement
Component that moves actors between nodes, handling pathfinding and interpolation.

## Use Cases

### 1. Board Games
Chess, checkers, Go, or custom board games.

```csharp
// Create 8x8 board
for (int x = 0; x < 8; x++)
{
    for (int y = 0; y < 8; y++)
    {
        var node = CreateNode($"Square_{x}_{y}", new Vector3(x, 0, y));
        
        // Connect to adjacent squares (4-directional or 8-directional)
        if (x > 0) ConnectToExistingNode(node, x-1, y);
        if (y > 0) ConnectToExistingNode(node, x, y-1);
    }
}
```

### 2. Tactical RPG
Fire Emblem or Final Fantasy Tactics style movement.

```csharp
// Create terrain nodes with different costs
var plainNode = CreateNode("Plain", position, cost: 1f);
var forestNode = CreateNode("Forest", position, cost: 2f);
var mountainNode = CreateNode("Mountain", position, cost: 3f);

// Query reachable nodes based on character's move range
var movement = character.GetComponent<GraphNodeMovement>();
var reachable = movement.GetReachableNodes(maxDistance: 5);
```

### 3. Puzzle Games
Sliding puzzles, Sokoban, or pathfinding puzzles.

```csharp
// Dynamic traversability for puzzles
node1.SetConnectionTraversable(node2, false); // Block path
// Player tries to find alternative route
movement.MoveToNodeWithPathfinding(goalNode);
```

### 4. Adventure Games
Point-and-click style room navigation.

```csharp
// Rooms as nodes
var livingRoom = CreateNode("LivingRoom", pos1);
var kitchen = CreateNode("Kitchen", pos2);
var bedroom = CreateNode("Bedroom", pos3);

livingRoom.AddConnection(kitchen, 1f);
livingRoom.AddConnection(bedroom, 1f);
// Kitchen not connected to bedroom (must go through living room)
```

### 5. Tower Defense
Define enemy paths with waypoints.

```csharp
// Create spawn-to-goal path
var spawn = CreateNode("Spawn", startPos);
var waypoint1 = CreateNode("WP1", pos1);
var waypoint2 = CreateNode("WP2", pos2);
var goal = CreateNode("Goal", endPos);

spawn.AddConnection(waypoint1);
waypoint1.AddConnection(waypoint2);
waypoint2.AddConnection(goal);

// Enemy follows path automatically
enemy.GetComponent<GraphNodeMovement>().MoveToNodeWithPathfinding(goal);
```

## API Reference

### GraphNode Methods

#### AddConnection
```csharp
public void AddConnection(GraphNode targetNode, float cost = 1.0f, bool bidirectional = true)
```
Creates a connection to another node.
- **bidirectional**: If true, creates connection in both directions

#### RemoveConnection
```csharp
public void RemoveConnection(GraphNode targetNode, bool bidirectional = true)
```
Removes a connection to another node.

#### IsConnectedTo
```csharp
public bool IsConnectedTo(GraphNode targetNode)
```
Checks if this node is directly connected and traversable to another node.

#### SetConnectionTraversable
```csharp
public void SetConnectionTraversable(GraphNode targetNode, bool traversable)
```
Enables or disables a connection dynamically (for doors, obstacles, etc.).

#### AutoConnectToNearbyNodes
```csharp
public void AutoConnectToNearbyNodes(float radius, LayerMask nodeLayer = default)
```
Automatically connects to all nodes within a radius. Useful for rapid prototyping.

#### ConnectToNodes
```csharp
public void ConnectToNodes(List<GraphNode> nodes, bool bidirectional = true)
```
Connects to a specific list of nodes.

#### ClearConnections
```csharp
public void ClearConnections(bool bidirectional = true)
```
Removes all connections from this node.

### GraphNodeMovement Methods

#### MoveToNode
```csharp
public bool MoveToNode(GraphNode node)
```
Moves to an adjacent node (no pathfinding). Returns false if not adjacent or already moving.

#### MoveToNodeWithPathfinding
```csharp
public bool MoveToNodeWithPathfinding(GraphNode target)
```
Uses A* to find shortest path and move to target node. Returns false if no path exists.

#### SnapToNearestNode
```csharp
public void SnapToNearestNode()
```
Finds and snaps to the closest node in the scene. Called automatically on Start.

#### TeleportToNode
```csharp
public void TeleportToNode(GraphNode node)
```
Instantly moves to a node, bypassing pathfinding and animation.

#### GetReachableNodes
```csharp
public List<GraphNode> GetReachableNodes(int maxDistance)
```
Returns all nodes reachable within a given number of steps. Useful for highlighting valid moves in tactical games.

## Configuration

### Movement Settings
- **moveSpeed**: Time to move between nodes
- **smoothMovement**: Enable smooth interpolation
- **movementCurve**: Animation curve for movement

### Pathfinding Settings
- **maxPathLength**: Maximum path length (0 = unlimited)
- **diagonalCost**: Cost multiplier for diagonal movement (default: √2)

### Visualization
- **showDebugVisualization**: Show nodes and connections in Scene view
- **nodeColor**: Color for current node
- **connectionColor**: Color for connections

## Workflow Examples

### Example 1: 4x4 Board Game

```csharp
// 1. Create nodes in a grid
List<GraphNode> nodes = new List<GraphNode>();
for (int x = 0; x < 4; x++)
{
    for (int y = 0; y < 4; y++)
    {
        var go = new GameObject($"Node_{x}_{y}");
        go.transform.position = new Vector3(x, 0, y);
        var node = go.AddComponent<GraphNode>();
        nodes.Add(node);
    }
}

// 2. Connect adjacent nodes (4-directional)
for (int i = 0; i < nodes.Count; i++)
{
    int x = i % 4;
    int y = i / 4;
    
    if (x > 0) nodes[i].AddConnection(nodes[i - 1], 1f, true);
    if (y > 0) nodes[i].AddConnection(nodes[i - 4], 1f, true);
}

// 3. Create actor
var actor = CreateActor("GamePiece", BehaviorProfile.GraphNode);
var movement = actor.GetComponent<GraphNodeMovement>();
movement.SnapToNearestNode();

// 4. Move actor
movement.MoveToNodeWithPathfinding(nodes[15]); // Move to corner
```

### Example 2: Irregular Graph (Adventure Game)

```csharp
// Manually placed rooms/locations
var entrance = CreateNodeAt("Entrance", new Vector3(0, 0, 0));
var hallway = CreateNodeAt("Hallway", new Vector3(5, 0, 0));
var kitchen = CreateNodeAt("Kitchen", new Vector3(5, 0, 5));
var library = CreateNodeAt("Library", new Vector3(10, 0, 0));
var lockedRoom = CreateNodeAt("LockedRoom", new Vector3(10, 0, 5));

// Connect rooms
entrance.AddConnection(hallway);
hallway.AddConnection(kitchen);
hallway.AddConnection(library);
library.AddConnection(lockedRoom);

// Initially lock the room
library.SetConnectionTraversable(lockedRoom, false);

// Later, unlock when player gets key
library.SetConnectionTraversable(lockedRoom, true);
```

### Example 3: Tactical RPG Movement Range

```csharp
// Show valid movement range
var movement = selectedUnit.GetComponent<GraphNodeMovement>();
int moveRange = selectedUnit.MovePoints;

var reachableNodes = movement.GetReachableNodes(moveRange);

// Highlight reachable nodes
foreach (var node in reachableNodes)
{
    node.GetComponent<Renderer>().material.color = Color.blue;
}

// On click, move to selected node
if (Input.GetMouseButtonDown(0))
{
    if (reachableNodes.Contains(clickedNode))
    {
        movement.MoveToNodeWithPathfinding(clickedNode);
    }
}
```

### Example 4: Auto-Generate Graph from Grid

```csharp
// Quick prototype: auto-connect nearby nodes
var allNodes = FindObjectsOfType<GraphNode>();
foreach (var node in allNodes)
{
    node.AutoConnectToNearbyNodes(radius: 1.5f);
}
```

## Performance Considerations

- **A* Pathfinding**: O(E log V) where E = edges, V = vertices
- **maxPathLength**: Limits search space for large graphs
- **Reachable Nodes**: Uses BFS, efficient for small distances
- **Recommended**: < 1000 nodes for real-time pathfinding

## Tips

1. **Node Placement**: Use empty GameObjects as nodes, positioned at valid movement locations
2. **Connection Costs**: Use actual distance for realistic movement, or custom costs for gameplay (difficult terrain = higher cost)
3. **Bidirectional**: Most games use bidirectional connections, but one-way (jumps, slides) can add variety
4. **Debug Visualization**: Enable in inspector to see your graph structure
5. **Dynamic Graphs**: Change traversability at runtime for doors, destructible obstacles, etc.
6. **Integration with UI**: Query reachable nodes to show valid moves to player
7. **AI Pathfinding**: AI can use same pathfinding to navigate intelligently

## Comparison with Other Movement Systems

| Feature | TileGrid | GraphNode | NavMesh |
|---------|----------|-----------|---------|
| **Structure** | Regular grid | Irregular graph | Continuous mesh |
| **Flexibility** | Low | High | Very High |
| **Setup** | Automatic | Manual node placement | Bake navmesh |
| **Pathfinding** | Grid-based | A* on graph | NavMesh pathfinding |
| **Best For** | Retro RPGs, puzzle games | Board games, tactical | Open world, realistic |
| **Dimension** | 2D only | 2D/3D | 3D only |

## Integration with GameKit Actor

GraphNodeMovement automatically subscribes to `GameKitActor.OnMoveInput`:
- **Input direction** selects best adjacent node
- **Direct calls** for precise control (`MoveToNode`, `MoveToNodeWithPathfinding`)
- **UI commands** can trigger specific node movements
- **AI** can use pathfinding for intelligent navigation

## Common Patterns

### Pattern 1: Hex Grid
```csharp
// 6 connections per node in hexagonal pattern
foreach (var node in hexNodes)
{
    var neighbors = GetHexNeighbors(node.GridPosition);
    foreach (var neighbor in neighbors)
    {
        node.AddConnection(neighbor, 1f, true);
    }
}
```

### Pattern 2: Weighted Terrain
```csharp
// Different movement costs based on terrain
plainNode.AddConnection(nextNode, cost: 1f);
forestNode.AddConnection(nextNode, cost: 2f);
mountainNode.AddConnection(nextNode, cost: 3f);
waterNode.AddConnection(nextNode, cost: 999f); // Nearly impassable
```

### Pattern 3: One-Way Paths
```csharp
// Ledge: can jump down but not climb up
highNode.AddConnection(lowNode, cost: 1f, bidirectional: false);
```

### Pattern 4: Dynamic Obstacles
```csharp
// Door that can be opened/closed
doorNode1.AddConnection(doorNode2, cost: 1f);
if (doorLocked)
{
    doorNode1.SetConnectionTraversable(doorNode2, false);
}
```

## Debugging

Enable `showDebugVisualization` in the inspector to see:
- **Yellow sphere**: Current node
- **Cyan lines**: Traversable connections
- **Red lines**: Blocked connections
- **Magenta lines**: Current path being followed
- **Green spheres**: All connected nodes (when selected)

## Version Notes

- Added in v1.9.0 (Unreleased)
- Requires Unity 2021.3+
- Works with or without Input System package

