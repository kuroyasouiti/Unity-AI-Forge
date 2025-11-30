# GameKit Runtime Components

GameKit provides high-level game development components with a focus on flexibility and modularity.

## Actor System

### GameKitActor

The core hub component that relays input from controllers to behavior components via UnityEvents.

**Events:**
- `OnMoveInput(Vector3)` - Movement direction
- `OnJumpInput()` - Jump action
- `OnActionInput(string)` - Generic action (e.g., "interact", "attack")
- `OnLookInput(Vector2)` - Look/rotation input

**Behavior Profiles:**
- `TwoDLinear` - 2D transform-based movement
- `TwoDPhysics` - 2D physics-based movement (Rigidbody2D)
- `TwoDTileGrid` - 2D tile-based grid movement
- `GraphNode` - Node-based graph movement with A* pathfinding (2D/3D agnostic)
- `SplineMovement` - Rail/spline-based movement for 2.5D games, rail shooters, and side-scrollers
- `ThreeDCharacterController` - 3D character controller
- `ThreeDPhysics` - 3D physics-based movement (Rigidbody)
- `ThreeDNavMesh` - 3D NavMesh agent

**Control Modes:**
- `DirectController` - Player input control
- `AIAutonomous` - AI-driven control
- `UICommand` - UI button control
- `ScriptTriggerOnly` - Script-only control

### Input Controllers

#### GameKitInputSystemController (Recommended)

Uses Unity's new Input System (requires Input System package).

**Features:**
- Automatic PlayerInput integration
- Pre-configured action map (Move, Look, Jump, Action, Fire)
- WASD/Arrow keys + Gamepad support
- Mouse + Right stick for look input
- Automatic 2D/3D input conversion based on behavior profile

**Requirements:**
- Input System package (`com.unity.inputsystem`)
- PlayerInput component (auto-added)
- DefaultGameKitInputActions asset (auto-generated)

**Usage:**
```csharp
// Automatically added when creating an actor with DirectController mode
// Default bindings:
// - WASD/Left Stick: Move
// - Mouse/Right Stick: Look
// - Space/A Button: Jump
// - E/X Button: Action
// - Left Click/RT: Fire
```

#### GameKitSimpleInput (Legacy Fallback)

Uses Unity's legacy Input system for maximum compatibility.

**Features:**
- Input.GetAxis() based movement
- Keyboard + Gamepad support
- Automatic 2D/3D conversion
- No additional packages required

**Fallback Behavior:**
- Automatically used when Input System is not installed
- Manual switch available by removing GameKitInputSystemController

### AI Controller

#### GameKitSimpleAI

Autonomous AI controller for NPCs and enemies.

**Behaviors:**
- `Idle` - Do nothing
- `Patrol` - Follow waypoints
- `Follow` - Chase a target
- `Wander` - Random movement

**Example:**
```csharp
var ai = actor.GetComponent<GameKitSimpleAI>();
ai.SetBehavior(GameKitSimpleAI.AIBehaviorType.Patrol);
ai.SetPatrolPoints(waypointArray);
```

### UI Command Hub

#### GameKitUICommand

Bridges UI controls to GameKitActor's UnityEvents, acting as a central hub for UI-to-actor communication.

**Features:**
- Command type mapping (Move, Jump, Action, Look, Custom)
- Directional button support for movement
- Parameter-based actions
- Actor reference caching for performance
- Command binding management

**Command Types:**
- `Move` - Maps to `OnMoveInput(Vector3)`
- `Jump` - Maps to `OnJumpInput()`
- `Action` - Maps to `OnActionInput(string)`
- `Look` - Maps to `OnLookInput(Vector2)`
- `Custom` - SendMessage for backward compatibility

**Example:**
```csharp
// Setup UI command hub
var uiCommand = commandPanel.GetComponent<GameKitUICommand>();
uiCommand.SetTargetActor(playerActor);

// Register directional buttons
uiCommand.RegisterDirectionalButton("moveUp", upButton, Vector3.up);
uiCommand.RegisterDirectionalButton("moveDown", downButton, Vector3.down);

// Register action buttons
uiCommand.RegisterButton("jump", jumpButton, GameKitUICommand.CommandType.Jump);
uiCommand.RegisterButton("attack", attackButton, GameKitUICommand.CommandType.Action, "sword");

// Or execute commands directly
uiCommand.ExecuteMoveCommand(new Vector3(1, 0, 0));
uiCommand.ExecuteActionCommand("usePotion");
```

**Use Cases:**
- Touch controls for mobile games
- Virtual joysticks and d-pads
- Action button panels
- Quick-action radial menus
- Command palette systems

## Movement Components

### TileGridMovement

Grid-based movement for tile-based games.

**Features:**
- Discrete tile movement with smooth interpolation
- Configurable grid size
- Diagonal movement support
- Collision detection
- Move queueing

**Auto-listens to:**
- `GameKitActor.OnMoveInput` - for grid direction input

### GraphNodeMovement

Node-based graph movement with A* pathfinding for discrete movement spaces.

**Features:**
- Works in both 2D and 3D (dimension-agnostic)
- A* pathfinding between nodes
- Weighted, traversable connections
- Reachable node queries
- Smooth interpolation or instant movement
- Debug visualization

**Use Cases:**
- Board games (chess, checkers)
- Tactical RPGs (Fire Emblem style)
- Puzzle games (sliding puzzles)
- Adventure games (room-to-room navigation)
- Tower defense (enemy path following)

**Key Components:**

#### GraphNode
Represents a position/location in the movement graph.

**Methods:**
- `AddConnection(node, cost, bidirectional)` - Connect to another node
- `RemoveConnection(node, bidirectional)` - Disconnect from a node
- `IsConnectedTo(node)` - Check if directly connected
- `SetConnectionTraversable(node, traversable)` - Enable/disable connection
- `AutoConnectToNearbyNodes(radius)` - Auto-connect within radius
- `ClearConnections(bidirectional)` - Remove all connections

#### GraphNodeMovement
Handles actor movement along the graph.

**Methods:**
- `MoveToNode(node)` - Move to adjacent node (no pathfinding)
- `MoveToNodeWithPathfinding(node)` - Find path and move to any node
- `SnapToNearestNode()` - Find and snap to closest node
- `TeleportToNode(node)` - Instant movement to node
- `GetReachableNodes(maxDistance)` - Get all nodes within distance

**Properties:**
- `CurrentNode` - Node actor is currently on
- `IsMoving` - Whether actor is currently moving
- `CurrentPath` - Active path being followed

**Auto-listens to:**
- `GameKitActor.OnMoveInput` - selects best adjacent node based on direction

**Example Setup:**
```csharp
// Create graph nodes
var node1 = new GameObject("Node1").AddComponent<GraphNode>();
var node2 = new GameObject("Node2").AddComponent<GraphNode>();
var node3 = new GameObject("Node3").AddComponent<GraphNode>();

node1.transform.position = new Vector3(0, 0, 0);
node2.transform.position = new Vector3(5, 0, 0);
node3.transform.position = new Vector3(10, 0, 0);

// Connect nodes
node1.AddConnection(node2, 1f, true);
node2.AddConnection(node3, 1f, true);

// Create actor
var actor = CreateActor("Player", graphNode);
var movement = actor.GetComponent<GraphNodeMovement>();
movement.SnapToNearestNode();

// Move to node (direct)
movement.MoveToNode(node2);

// Or use pathfinding
movement.MoveToNodeWithPathfinding(node3);

// Query reachable nodes
var reachable = movement.GetReachableNodes(2);
```

## Manager System

### GameKitManager (Hub)

Central hub for game management that automatically adds mode-specific components based on `ManagerType`.

**Architecture:**
- GameKitManager acts as a lightweight hub
- Automatically attaches mode-specific component on initialization
- Convenience methods delegate to mode-specific components
- Direct access via `GetModeComponent<T>()`

**Manager Types & Components:**

#### TurnBased → GameKitTurnManager
Turn-based game flow management.

**Features:**
- Turn phase management
- Turn counter
- Phase transitions
- Events: `OnPhaseChanged`, `OnTurnAdvanced`

**Example:**
```csharp
var manager = managerGo.AddComponent<GameKitManager>();
manager.Initialize("gameManager", ManagerType.TurnBased, false);

manager.AddTurnPhase("PlayerTurn");
manager.AddTurnPhase("EnemyTurn");
manager.AddTurnPhase("EndTurn");

manager.NextPhase(); // PlayerTurn → EnemyTurn

// Direct access to TurnManager
var turnManager = manager.GetModeComponent<GameKitTurnManager>();
turnManager.OnPhaseChanged.AddListener(phase => {
    Debug.Log($"Phase changed to: {phase}");
});
```

#### ResourcePool → GameKitResourceManager
Machinations-inspired resource flow system for game economies.

**Features:**
- Resource pools with min/max constraints
- Automatic flows (sources generate, drains consume)
- Resource converters (crafting, transformation chains)
- Resource triggers (threshold-based events)
- Events: `OnResourceChanged`, `OnResourceTriggered`

**Example:**
```csharp
var manager = managerGo.AddComponent<GameKitManager>();
manager.Initialize("resourceManager", ManagerType.ResourcePool, false);

// Direct access to ResourceManager
var resourceManager = manager.GetModeComponent<GameKitResourceManager>();

// Basic resources
manager.SetResource("gold", 100);
resourceManager.SetResourceConstraints("health", 0f, 100f);

// Automatic flows
resourceManager.AddFlow("gold", 5f, isSource: true);  // 5 gold/sec income
resourceManager.AddFlow("mana", 2f, isSource: false); // 2 mana/sec drain

// Resource conversion (crafting)
resourceManager.AddConverter("wood", "planks", conversionRate: 4f, inputCost: 1f);
bool crafted = resourceManager.Convert("wood", "planks", 10f); // 10 wood → 40 planks

// Threshold triggers
resourceManager.AddTrigger("lowHealth", "health", ThresholdType.Below, 30f);
resourceManager.OnResourceTriggered.AddListener((trigger, resource, value) => {
    if (trigger == "lowHealth") ShowWarning();
});

// Or use convenience methods
bool consumed = manager.ConsumeResource("gold", 75);
```

See [GameKitResourceManager.README.md](./GameKitResourceManager.README.md) for detailed documentation.

#### EventHub → GameKitEventManager
Game-wide event hub for custom events.

**Features:**
- Event registration/unregistration
- Event triggering
- Named event system

**Example:**
```csharp
var manager = managerGo.AddComponent<GameKitManager>();
manager.Initialize("eventHub", ManagerType.EventHub, false);

manager.RegisterEventListener("OnLevelComplete", () => {
    Debug.Log("Level completed!");
});

manager.TriggerEvent("OnLevelComplete");
```

#### StateManager → GameKitStateManager
Game state management (menu, playing, paused, etc.)

**Features:**
- State transitions
- State history
- Previous state tracking
- Events: `OnStateChanged`

**Example:**
```csharp
var manager = managerGo.AddComponent<GameKitManager>();
manager.Initialize("stateManager", ManagerType.StateManager, false);

manager.ChangeState("MainMenu");
manager.ChangeState("Playing");
manager.ChangeState("Paused");

manager.ReturnToPreviousState(); // Paused → Playing

var currentState = manager.GetCurrentState(); // "Playing"

// Direct access to StateManager
var stateManager = manager.GetModeComponent<GameKitStateManager>();
stateManager.OnStateChanged.AddListener((newState, oldState) => {
    Debug.Log($"State: {oldState} → {newState}");
});
```

#### Realtime → GameKitRealtimeManager
Real-time game flow management (time scale, pause, timers)

**Features:**
- Time scale control
- Pause/Resume
- Timer management
- Elapsed time tracking
- Events: `OnTimeScaleChanged`, `OnPauseChanged`

**Example:**
```csharp
var manager = managerGo.AddComponent<GameKitManager>();
manager.Initialize("timeManager", ManagerType.Realtime, false);

manager.SetTimeScale(0.5f); // Slow motion
manager.Pause();
manager.Resume();

// Direct access to RealtimeManager for timers
var realtimeManager = manager.GetModeComponent<GameKitRealtimeManager>();
realtimeManager.AddTimer("powerup", 5f, () => {
    Debug.Log("Powerup expired!");
});
```

**Backward Compatibility:**
All existing code using GameKitManager continues to work. Convenience methods automatically delegate to the appropriate mode-specific component.

## Integration Examples

### Creating a Player Character

```csharp
// Via MCP
unity_gamekit_actor({
    "operation": "create",
    "actorId": "Player",
    "behaviorProfile": "2dPhysics",
    "controlMode": "directController",
    "spritePath": "Assets/Sprites/player.png",
    "position": {"x": 0, "y": 0, "z": 0}
})
```

Result:
- GameObject with GameKitActor
- Rigidbody2D + BoxCollider2D (from 2dPhysics profile)
- PlayerInput + GameKitInputSystemController (from directController mode)
- SpriteRenderer with assigned sprite

### Creating an AI Enemy

```csharp
unity_gamekit_actor({
    "operation": "create",
    "actorId": "Enemy",
    "behaviorProfile": "2dPhysics",
    "controlMode": "aiAutonomous",
    "spritePath": "Assets/Sprites/enemy.png"
})
```

Result:
- GameObject with GameKitActor
- Rigidbody2D + BoxCollider2D
- GameKitSimpleAI (from aiAutonomous mode)

### Creating a Grid-Based Character

```csharp
unity_gamekit_actor({
    "operation": "create",
    "actorId": "GridHero",
    "behaviorProfile": "2dTileGrid",
    "controlMode": "directController"
})
```

Result:
- GameObject with GameKitActor
- TileGridMovement component
- Input controller (Input System or legacy)

### Creating a Graph-Based Character (Board Game)

```csharp
// 1. Create graph nodes
unity_gameobject_crud({
    "operation": "create",
    "objectName": "BoardSpace1",
    "position": {"x": 0, "y": 0, "z": 0}
})

unity_component_crud({
    "operation": "add",
    "gameObjectPath": "BoardSpace1",
    "componentType": "Unity-AI-Forge.GameKit.GraphNode"
})

// Repeat for more nodes...

// 2. Connect nodes (via script or manually in editor)
// node1.AddConnection(node2, cost: 1.0f, bidirectional: true)

// 3. Create actor with graph movement
unity_gamekit_actor({
    "operation": "create",
    "actorId": "GamePiece",
    "behaviorProfile": "graphNode",
    "controlMode": "uiCommand",
    "position": {"x": 0, "y": 0, "z": 0}
})
```

Result:
- GameObject with GameKitActor
- GraphNodeMovement component
- Snaps to nearest node on start
- Move via input direction or pathfinding API

**Common Graph Patterns:**
- **Board Game**: Square grid with diagonal connections
- **Tactical RPG**: Hex grid or irregular terrain nodes
- **Puzzle Game**: Connected puzzle pieces/tiles
- **Adventure Game**: Room-to-room navigation graph
- **Tower Defense**: Enemy path waypoints

### SplineMovement Component

Provides smooth rail/spline-based movement for 2.5D games using Catmull-Rom splines.

**Features:**
- Smooth curved paths defined by control points
- Automatic tangent calculation for natural rotation
- Closed loop support for circular tracks
- Lateral offset for lane-based gameplay
- Manual or automatic speed control
- Forward and backward movement support
- Visual spline debugging in Scene view

**Key Properties:**
- `controlPoints` - Transform array defining the spline path
- `moveSpeed` - Movement speed along spline
- `closedLoop` - Connect last point to first
- `autoRotate` - Face movement direction
- `allowManualControl` - Use input for speed control
- `lateralOffset` - Offset from path (for lanes)

**Common Use Cases:**
- **Rail Shooter**: Camera follows fixed path with lateral movement
- **Side-Scroller**: Character follows winding path through 2.5D environment
- **Racing Game**: Vehicles follow track with lane changes
- **On-Rails Sequence**: Cutscene or scripted movement along path
- **Roller Coaster**: Physics-disabled ride along track

## Architecture

```
Input Source (Keyboard/AI/UI)
    ↓
Controller Component (GameKitInputSystemController/GameKitSimpleAI)
    ↓
GameKitActor (Hub with UnityEvents)
    ↓
Behavior Component (TileGridMovement/Custom Scripts)
    ↓
Game Logic
```

This decoupled architecture allows:
- Swapping input sources without changing behaviors
- Swapping behaviors without changing controllers
- Multiple listeners per event
- Easy testing and debugging

## Version Defines

- `UNITY_INPUT_SYSTEM_INSTALLED` - Defined when Input System package is installed

