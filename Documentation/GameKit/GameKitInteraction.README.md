# GameKitInteraction Hub

## Overview

`GameKitInteraction` is a hub that bridges game events (triggers) to actions across all GameKit components. It provides a declarative way to create interactive gameplay without writing custom scripts. Supports traditional triggers (collision, input) and specialized triggers for Tilemap, Graph, and Spline movement.

## Core Concept

GameKitInteraction acts as an event hub:
- **Triggers**: When something happens (collision, reaching a tile, spline progress, etc.)
- **Conditions**: Check if requirements are met (actor ID, resources, distance, etc.)
- **Actions**: Execute responses (spawn, destroy, send to actor, update resources, etc.)
- **Events**: Invoke UnityEvents for custom scripting

## Trigger Types

### Traditional Triggers

#### Collision (3D/2D)
Triggered on collision enter.

```csharp
interaction.Initialize("wallHit", GameKitInteraction.TriggerType.Collision);
interaction.AddAction(ActionType.PlaySound, "Sounds/Bump", "");
```

#### Trigger (3D/2D)
Triggered when entering a trigger collider (requires collider with `isTrigger = true`).

```csharp
interaction.Initialize("pickupItem", GameKitInteraction.TriggerType.Trigger);
interaction.AddCondition(ConditionType.Tag, "Player");
interaction.AddAction(ActionType.DestroyObject, "self", "");
```

#### Input
Triggered on key press.

```csharp
// Set inputKey = KeyCode.E in Inspector
interaction.Initialize("openDoor", GameKitInteraction.TriggerType.Input);
interaction.AddAction(ActionType.TriggerActorAction, "door_001", "open");
```

#### Proximity
Triggered when an actor enters detection radius.

```csharp
// Set proximityRadius = 3f in Inspector
interaction.Initialize("enemyDetect", GameKitInteraction.TriggerType.Proximity);
interaction.AddCondition(ConditionType.Tag, "Player");
interaction.AddAction(ActionType.TriggerActorAction, "enemy_001", "attack");
```

### Specialized Triggers (2D Tilemap, Graph, Spline)

#### TilemapCell
Triggered when actor reaches specific tilemap cell.

```csharp
// Requires TileGridMovement component on same GameObject
// Set tilemapCell = (5, 3) in Inspector
interaction.Initialize("reachCheckpoint", GameKitInteraction.TriggerType.TilemapCell);
interaction.AddAction(ActionType.UpdateManagerResource, "checkpoints", "1");
```

**Use Cases:**
- Tile-based checkpoints
- Trigger zone in puzzle games
- Event tiles in tactics games
- Secret tile discovery

#### GraphNode
Triggered when actor reaches specific graph node.

```csharp
// Requires GraphNodeMovement component on same GameObject
// Set targetGraphNodeId = "node_boss_room" in Inspector
interaction.Initialize("reachBoss", GameKitInteraction.TriggerType.GraphNode);
interaction.AddAction(ActionType.TriggerActorAction, "boss_001", "awaken");
```

**Use Cases:**
- Room-based events in adventure games
- Board game space effects
- Tower defense waypoint triggers
- Navigation-based cutscenes

#### SplineProgress
Triggered when actor reaches specific progress on spline (0-1).

```csharp
// Requires SplineMovement component on same GameObject
// Set splineProgress = 0.5f in Inspector (halfway)
interaction.Initialize("midpointEvent", GameKitInteraction.TriggerType.SplineProgress);
interaction.AddAction(ActionType.SpawnPrefab, "Prefabs/Enemy", "10,0,0");
```

**Use Cases:**
- Rail shooter enemy spawns
- Cutscene events at specific points
- Racing checkpoint triggers
- Roller coaster events

## Action Types

### Traditional Actions

#### SpawnPrefab
Instantiate a prefab at position.

```csharp
interaction.AddAction(ActionType.SpawnPrefab, "Prefabs/Coin", "0,0,0");
// parameter: "x,y,z" position (optional, defaults to interaction position)
```

#### DestroyObject
Destroy GameObject by name or reference.

```csharp
interaction.AddAction(ActionType.DestroyObject, "self", "");  // Destroy interaction object
interaction.AddAction(ActionType.DestroyObject, "other", ""); // Destroy triggering object
interaction.AddAction(ActionType.DestroyObject, "Enemy1", ""); // Destroy by name
```

#### PlaySound
Play audio clip at interaction position.

```csharp
interaction.AddAction(ActionType.PlaySound, "Sounds/Explosion", "");
// target: Resource path to AudioClip
```

#### SendMessage
Send Unity message to target GameObject.

```csharp
interaction.AddAction(ActionType.SendMessage, "self", "OnActivate");
interaction.AddAction(ActionType.SendMessage, "other", "OnHit");
interaction.AddAction(ActionType.SendMessage, "Boss", "OnDefeat");
```

### GameKit Integration Actions

#### TriggerActorAction
Send action command to GameKitActor.

```csharp
interaction.AddAction(ActionType.TriggerActorAction, "player_001", "collect");
// Sends to actor.OnActionInput("collect")
```

#### UpdateManagerResource
Modify GameKitManager resource.

```csharp
interaction.AddAction(ActionType.UpdateManagerResource, "gold", "100");  // Add 100
interaction.AddAction(ActionType.UpdateManagerResource, "health", "-10"); // Subtract 10
```

#### TriggerSceneFlow
Trigger GameKitSceneFlow transition.

```csharp
interaction.AddAction(ActionType.TriggerSceneFlow, "nextLevel", "");
// Calls GameKitSceneFlow.Transition("nextLevel")
```

### Movement-Specific Actions

#### TeleportToTile
Teleport actor to specific tilemap cell.

```csharp
interaction.AddAction(ActionType.TeleportToTile, "self", "10,5");
interaction.AddAction(ActionType.TeleportToTile, "player_001", "0,0");
// parameter: "x,y" grid coordinates
```

#### MoveToGraphNode
Move actor to specific graph node.

```csharp
interaction.AddAction(ActionType.MoveToGraphNode, "node_exit", "");
// target: node ID
```

#### SetSplineProgress
Set actor's progress on spline.

```csharp
interaction.AddAction(ActionType.SetSplineProgress, "self", "0.0");      // Reset to start
interaction.AddAction(ActionType.SetSplineProgress, "player_001", "1.0"); // Jump to end
// parameter: 0-1 progress value
```

## Condition Types

### Traditional Conditions

#### Tag
Check GameObject tag.

```csharp
interaction.AddCondition(ConditionType.Tag, "Player");
```

#### Layer
Check GameObject layer.

```csharp
interaction.AddCondition(ConditionType.Layer, "Enemy");
```

#### Distance
Check distance threshold.

```csharp
interaction.AddCondition(ConditionType.Distance, "5.0");
```

### GameKit Integration Conditions

#### ActorId
Check if triggering GameObject has specific GameKitActor ID.

```csharp
interaction.AddCondition(ConditionType.ActorId, "player_001");
```

#### ManagerResource
Check if GameKitManager has sufficient resource.

```csharp
interaction.AddCondition(ConditionType.ManagerResource, "gold:100");
// Format: "resourceName:minAmount"
```

## Common Usage Patterns

### Tilemap Treasure Chest

```csharp
// Place on treasure chest GameObject with TileGridMovement
var interaction = chest.AddComponent<GameKitInteraction>();
interaction.Initialize("treasureChest", TriggerType.TilemapCell);

// Configure in Inspector: tilemapCell = (10, 5)

// Actions when reaching cell
interaction.AddAction(ActionType.UpdateManagerResource, "gold", "500");
interaction.AddAction(ActionType.PlaySound, "Sounds/Treasure", "");
interaction.AddAction(ActionType.DestroyObject, "self", "");
```

### Graph Node Boss Battle

```csharp
// Place on actor with GraphNodeMovement
var interaction = player.AddComponent<GameKitInteraction>();
interaction.Initialize("bossRoom", TriggerType.GraphNode);

// Configure in Inspector: targetGraphNodeId = "node_boss_room"

// Trigger boss when entering room
interaction.AddAction(ActionType.TriggerActorAction, "boss_001", "awaken");
interaction.AddAction(ActionType.PlaySound, "Sounds/BossMusic", "");
interaction.AddAction(ActionType.TriggerSceneFlow, "startBossBattle", "");
```

### Rail Shooter Enemy Spawner

```csharp
// Place on player with SplineMovement
var spawner1 = player.AddComponent<GameKitInteraction>();
spawner1.Initialize("wave1Spawn", TriggerType.SplineProgress);

// Configure in Inspector: splineProgress = 0.25f (25% progress)

spawner1.AddAction(ActionType.SpawnPrefab, "Prefabs/EnemyWave1", "10,0,0");

// Add more spawners at different progress points
var spawner2 = player.AddComponent<GameKitInteraction>();
spawner2.Initialize("wave2Spawn", TriggerType.SplineProgress);
// splineProgress = 0.5f

spawner2.AddAction(ActionType.SpawnPrefab, "Prefabs/EnemyWave2", "10,0,0");
```

### Conditional Door (Resource Check)

```csharp
var door = doorObject.AddComponent<GameKitInteraction>();
door.Initialize("lockedDoor", TriggerType.Input); // Press E to open

// Requires key
door.AddCondition(ConditionType.ManagerResource, "keys:1");
door.AddCondition(ConditionType.Distance, "2.0");

// Actions
door.AddAction(ActionType.UpdateManagerResource, "keys", "-1"); // Consume key
door.AddAction(ActionType.TriggerActorAction, "door_001", "open");
door.AddAction(ActionType.PlaySound, "Sounds/DoorOpen", "");
```

### Tile-Based Puzzle Switch

```csharp
// Push block onto switch tile
var switchTile = block.AddComponent<GameKitInteraction>();
switchTile.Initialize("puzzleSwitch", TriggerType.TilemapCell);

// Configure: tilemapCell = (8, 3)

switchTile.AddAction(ActionType.TriggerActorAction, "door_puzzle", "unlock");
switchTile.AddAction(ActionType.PlaySound, "Sounds/Switch", "");
```

### Spline Cutscene Trigger

```csharp
// Trigger cutscene at specific spline progress
var cutsceneTrigger = camera.AddComponent<GameKitInteraction>();
cutsceneTrigger.Initialize("cutscene1", TriggerType.SplineProgress);

// Configure: splineProgress = 0.75f

cutsceneTrigger.AddAction(ActionType.TriggerSceneFlow, "playCutscene", "");
cutsceneTrigger.AddAction(ActionType.SetSplineProgress, "self", "1.0"); // Skip to end
```

## Advanced Features

### UnityEvent Integration

Subscribe to interaction events for custom logic:

```csharp
interaction.OnInteractionTriggered.AddListener(triggeringObject => {
    Debug.Log($"Interaction triggered by {triggeringObject.name}");
    // Custom logic here
});
```

### Cooldown and Repeating

Configure trigger behavior:

```csharp
// In Inspector:
allowRepeatedTrigger = false; // Trigger only once
triggerCooldown = 2.0f;       // 2 second cooldown between triggers
```

### Manual Triggering

Trigger from scripts:

```csharp
public void OnButtonClick()
{
    interaction.ManualTrigger();
}
```

### Logging

Enable debug logging:

```csharp
// In Inspector:
logInteractions = true; // Log all trigger/action execution
```

## API Reference

### Properties

- `string InteractionId` - Unique identifier
- `TriggerType Trigger` - Trigger type
- `UnityEvent<GameObject> OnInteractionTriggered` - Event invoked on trigger

### Methods

- `void Initialize(string id, TriggerType trigger)` - Initialize interaction
- `void AddAction(ActionType type, string target, string parameter)` - Add action
- `void AddCondition(ConditionType type, string value)` - Add condition
- `void ManualTrigger(GameObject triggeringObject = null)` - Manually trigger
- `void AddSharedScenesToScene(string sceneName, string[] paths)` - Add shared scenes

### Trigger Parameters (Inspector)

- `tilemapCell` (Vector2Int) - For TilemapCell trigger
- `targetGraphNodeId` (string) - For GraphNode trigger
- `splineProgress` (float 0-1) - For SplineProgress trigger
- `proximityRadius` (float) - For Proximity trigger
- `inputKey` (KeyCode) - For Input trigger

### Settings (Inspector)

- `logInteractions` - Enable debug logging
- `allowRepeatedTrigger` - Allow multiple triggers
- `triggerCooldown` - Cooldown between triggers (seconds)

## Best Practices

### 1. Trigger Selection

Choose the appropriate trigger for your use case:
- **Collision/Trigger**: Physical interactions
- **Input**: Player-initiated actions
- **Proximity**: Detection-based events
- **TilemapCell**: Tile-based gameplay
- **GraphNode**: Room/waypoint-based events
- **SplineProgress**: Time/position-based events on rails

### 2. Condition Chaining

Use multiple conditions for precise control:

```csharp
interaction.AddCondition(ConditionType.ActorId, "player_001");
interaction.AddCondition(ConditionType.ManagerResource, "keys:1");
interaction.AddCondition(ConditionType.Distance, "3.0");
// All conditions must be true
```

### 3. Action Sequencing

Actions execute in order:

```csharp
interaction.AddAction(ActionType.PlaySound, "Sounds/Pickup", "");
interaction.AddAction(ActionType.UpdateManagerResource, "gold", "100");
interaction.AddAction(ActionType.TriggerActorAction, "player", "celebrate");
interaction.AddAction(ActionType.DestroyObject, "self", "");
```

### 4. One-Time vs Repeating

For pickups and one-time events:
```csharp
allowRepeatedTrigger = false;
```

For pressure plates and repeating events:
```csharp
allowRepeatedTrigger = true;
triggerCooldown = 0.5f; // Prevent spam
```

## Integration Examples

### Tile-Based RPG Event

```csharp
// Player reaches inn tile
var innInteraction = player.AddComponent<GameKitInteraction>();
innInteraction.Initialize("innEvent", TriggerType.TilemapCell);
// tilemapCell = (15, 10)

// Check if player has enough gold
innInteraction.AddCondition(ConditionType.ManagerResource, "gold:50");

// Rest at inn
innInteraction.AddAction(ActionType.UpdateManagerResource, "gold", "-50");
innInteraction.AddAction(ActionType.UpdateManagerResource, "health", "100");
innInteraction.AddAction(ActionType.PlaySound, "Sounds/Rest", "");
```

### Graph-Based Adventure Game

```csharp
// Entering library room
var libraryInteraction = player.AddComponent<GameKitInteraction>();
libraryInteraction.Initialize("library", TriggerType.GraphNode);
// targetGraphNodeId = "node_library"

// Trigger dialogue
libraryInteraction.AddAction(ActionType.TriggerActorAction, "npc_librarian", "greet");
libraryInteraction.AddAction(ActionType.TriggerSceneFlow, "enterLibrary", "");
```

### Rail Shooter Boss Spawn

```csharp
// Spawn boss at 80% progress
var bossSpawn = player.AddComponent<GameKitInteraction>();
bossSpawn.Initialize("bossSpawn", TriggerType.SplineProgress);
// splineProgress = 0.8f

bossSpawn.AddAction(ActionType.SpawnPrefab, "Prefabs/Boss", "0,0,20");
bossSpawn.AddAction(ActionType.PlaySound, "Sounds/BossWarning", "");
bossSpawn.AddAction(ActionType.SetSplineProgress, "self", "0.7"); // Slow down for boss fight
```

### Multi-Component Interaction Chain

```csharp
// Complex interaction using multiple GameKit components
var complexInteraction = trigger.AddComponent<GameKitInteraction>();
complexInteraction.Initialize("questComplete", TriggerType.Input);

// Conditions
complexInteraction.AddCondition(ConditionType.ActorId, "player");
complexInteraction.AddCondition(ConditionType.ManagerResource, "questItems:5");

// Actions across multiple systems
complexInteraction.AddAction(ActionType.UpdateManagerResource, "questItems", "-5");
complexInteraction.AddAction(ActionType.UpdateManagerResource, "gold", "1000");
complexInteraction.AddAction(ActionType.TriggerActorAction, "npc_quest_giver", "thank");
complexInteraction.AddAction(ActionType.TriggerSceneFlow, "questComplete", "");
complexInteraction.AddAction(ActionType.PlaySound, "Sounds/Victory", "");
```

## Troubleshooting

**Trigger not firing:**
- Verify trigger type matches setup (e.g., TilemapCell requires TileGridMovement)
- Check conditions are met (use `logInteractions = true`)
- Ensure colliders/triggers are properly configured
- Check cooldown hasn't prevented trigger

**Actions not executing:**
- Enable `logInteractions` to see execution flow
- Verify target objects/actors exist
- Check resource names match exactly
- Ensure manager/actors are in scene

**Tilemap trigger not working:**
- Verify TileGridMovement component is present
- Check tilemapCell coordinates are correct
- Ensure actor is moving to that cell

**Graph trigger not working:**
- Verify GraphNodeMovement component is present
- Check targetGraphNodeId matches node
- Ensure node exists in scene

**Spline trigger not working:**
- Verify SplineMovement component is present
- Check splineProgress is between 0-1
- Note: 5% threshold for triggering

## See Also

- [GameKit Actor](./README.md#gamekit-actor)
- [GameKit Manager](./README.md#gamekit-manager)
- [GameKit SceneFlow](./GameKitSceneFlow.README.md)
- [TileGridMovement](./TileGridMovement.README.md)
- [GraphNodeMovement](./GraphNodeMovement.README.md)
- [SplineMovement](./SplineMovement.README.md)

