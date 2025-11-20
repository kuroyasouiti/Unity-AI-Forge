# Design Pattern Generation Examples

This guide demonstrates how to use the `unity_designPattern_generate` tool to create production-ready implementations of common design patterns.

## Overview

The design pattern generator creates complete, commented C# code for 7 common Unity design patterns:
- **Singleton** - Single instance management with persistence
- **ObjectPool** - Efficient object reuse for performance
- **StateMachine** - State management with transitions
- **Observer** - Event-driven communication
- **Command** - Action abstraction with undo/redo
- **Factory** - Object creation pattern
- **ServiceLocator** - Global service access

## Example 1: Singleton Pattern (Game Manager)

```python
unity_designPattern_generate({
    "patternType": "singleton",
    "className": "GameManager",
    "scriptPath": "Assets/Scripts/Managers/GameManager.cs",
    "namespace": "MyGame.Managers",
    "options": {
        "persistent": True,      # Survives scene changes
        "threadSafe": True,      # Thread-safe initialization
        "monoBehaviour": True    # Unity MonoBehaviour
    }
})
```

**Generated Features:**
- DontDestroyOnLoad for persistence
- Lazy initialization with thread safety
- Awake() protection against duplicates
- Ready to add custom methods

## Example 2: ObjectPool Pattern (Bullet Pool)

```python
unity_designPattern_generate({
    "patternType": "objectpool",
    "className": "BulletPool",
    "scriptPath": "Assets/Scripts/Combat/BulletPool.cs",
    "namespace": "MyGame.Combat",
    "options": {
        "pooledType": "Bullet",       # Type to pool
        "defaultCapacity": "100",     # Initial size
        "maxSize": "500"              # Maximum size
    }
})
```

**Generated Features:**
- Unity ObjectPool<T> integration
- Configurable pool size
- Get/Release/Clear methods
- Automatic prefab instantiation

**Usage:**
```csharp
// Get bullet from pool
Bullet bullet = bulletPool.Get();
bullet.Fire(direction);

// Return to pool when done
bulletPool.Release(bullet);
```

## Example 3: StateMachine Pattern (Player States)

```python
unity_designPattern_generate({
    "patternType": "statemachine",
    "className": "PlayerStateMachine",
    "scriptPath": "Assets/Scripts/Player/PlayerStateMachine.cs",
    "namespace": "MyGame.Player"
})
```

**Generated Features:**
- IState interface with Enter/Execute/Exit
- Type-safe state registration
- State change management
- Example Idle and Move states

**Usage:**
```csharp
// Register states
stateMachine.RegisterState(new IdleState());
stateMachine.RegisterState(new MoveState());
stateMachine.RegisterState(new JumpState());

// Change state
stateMachine.ChangeState<MoveState>();
```

## Example 4: Observer Pattern (Event System)

```python
unity_designPattern_generate({
    "patternType": "observer",
    "className": "EventManager",
    "scriptPath": "Assets/Scripts/Core/EventManager.cs",
    "namespace": "MyGame.Core"
})
```

**Generated Features:**
- Singleton event manager
- Type-safe event subscription
- Generic event publishing
- String-based event names

**Usage:**
```csharp
// Subscribe to events
EventManager.Instance.Subscribe<int>("ScoreChanged", OnScoreChanged);
EventManager.Instance.Subscribe("GameOver", OnGameOver);

// Publish events
EventManager.Instance.Publish("ScoreChanged", newScore);
EventManager.Instance.Publish("GameOver");

// Unsubscribe
EventManager.Instance.Unsubscribe<int>("ScoreChanged", OnScoreChanged);
```

## Example 5: Command Pattern (Undo/Redo System)

```python
unity_designPattern_generate({
    "patternType": "command",
    "className": "CommandManager",
    "scriptPath": "Assets/Scripts/Editor/CommandManager.cs",
    "namespace": "MyGame.Editor"
})
```

**Generated Features:**
- ICommand interface
- Command history stack
- Undo/Redo functionality
- Example MoveCommand

**Usage:**
```csharp
// Execute command
var moveCmd = new MoveCommand(player.transform, newPosition);
commandManager.ExecuteCommand(moveCmd);

// Undo last action
commandManager.Undo();

// Redo
commandManager.Redo();

// Clear history
commandManager.ClearHistory();
```

## Example 6: Factory Pattern (Enemy Spawner)

```python
unity_designPattern_generate({
    "patternType": "factory",
    "className": "EnemyFactory",
    "scriptPath": "Assets/Scripts/Enemies/EnemyFactory.cs",
    "namespace": "MyGame.Enemies",
    "options": {
        "productType": "GameObject"
    }
})
```

**Generated Features:**
- Product ID to prefab mapping
- Inspector-friendly configuration
- Type-safe creation methods
- Position/rotation overloads

**Usage:**
```csharp
// Create enemy by ID
GameObject zombie = enemyFactory.CreateProduct("zombie");

// Create with position
GameObject boss = enemyFactory.CreateProduct("boss", spawnPos, spawnRot);

// Create with component access
Enemy skeleton = enemyFactory.CreateProduct<Enemy>("skeleton");
```

## Example 7: ServiceLocator Pattern (Global Services)

```python
unity_designPattern_generate({
    "patternType": "servicelocator",
    "className": "ServiceLocator",
    "scriptPath": "Assets/Scripts/Core/ServiceLocator.cs",
    "namespace": "MyGame.Core"
})
```

**Generated Features:**
- Singleton service registry
- Type-safe registration
- Service existence checks
- Example IAudioService interface

**Usage:**
```csharp
// Register services
ServiceLocator.Instance.RegisterService<IAudioService>(new AudioService());
ServiceLocator.Instance.RegisterService<IInputService>(new InputService());

// Get service
IAudioService audio = ServiceLocator.Instance.GetService<IAudioService>();
audio.PlaySound("explosion");

// Check if service exists
if (ServiceLocator.Instance.HasService<IAnalytics>()) {
    var analytics = ServiceLocator.Instance.GetService<IAnalytics>();
    analytics.LogEvent("level_complete");
}
```

## Complete Game Architecture Example

Combine multiple patterns for a robust game architecture:

```python
# 1. Core infrastructure
unity_designPattern_generate({
    "patternType": "singleton",
    "className": "GameManager",
    "scriptPath": "Assets/Scripts/Core/GameManager.cs",
    "namespace": "MyGame.Core",
    "options": {"persistent": True, "monoBehaviour": True}
})

# 2. Event system for decoupled communication
unity_designPattern_generate({
    "patternType": "observer",
    "className": "EventManager",
    "scriptPath": "Assets/Scripts/Core/EventManager.cs",
    "namespace": "MyGame.Core"
})

# 3. Service locator for global services
unity_designPattern_generate({
    "patternType": "servicelocator",
    "className": "ServiceLocator",
    "scriptPath": "Assets/Scripts/Core/ServiceLocator.cs",
    "namespace": "MyGame.Core"
})

# 4. Object pooling for performance
unity_designPattern_generate({
    "patternType": "objectpool",
    "className": "BulletPool",
    "scriptPath": "Assets/Scripts/Combat/BulletPool.cs",
    "namespace": "MyGame.Combat",
    "options": {"pooledType": "Bullet", "defaultCapacity": "100", "maxSize": "500"}
})

# 5. Player state management
unity_designPattern_generate({
    "patternType": "statemachine",
    "className": "PlayerStateMachine",
    "scriptPath": "Assets/Scripts/Player/PlayerStateMachine.cs",
    "namespace": "MyGame.Player"
})

# 6. Enemy spawning
unity_designPattern_generate({
    "patternType": "factory",
    "className": "EnemyFactory",
    "scriptPath": "Assets/Scripts/Enemies/EnemyFactory.cs",
    "namespace": "MyGame.Enemies"
})
```

## Best Practices

### 1. Use Appropriate Patterns
- **Singleton**: Managers (GameManager, AudioManager, InputManager)
- **ObjectPool**: Frequently spawned objects (bullets, particles, enemies)
- **StateMachine**: Complex behavior (player states, AI states, UI states)
- **Observer**: Decoupled events (score changes, achievements, game events)
- **Command**: Undoable actions (level editor, gameplay rewind)
- **Factory**: Runtime object creation (enemy spawners, item generation)
- **ServiceLocator**: Cross-cutting concerns (audio, analytics, localization)

### 2. Combine Patterns Effectively
```python
# Core systems
GameManager (Singleton) + EventManager (Observer) + ServiceLocator

# Combat systems
BulletPool (ObjectPool) + EnemyFactory (Factory)

# Player systems
PlayerStateMachine (StateMachine) + CommandManager (Command for abilities)
```

### 3. Namespace Organization
```python
unity_designPattern_generate({
    "namespace": "MyGame.Core",      # Core infrastructure
    # or
    "namespace": "MyGame.Combat",    # Combat systems
    # or
    "namespace": "MyGame.UI",        # UI systems
    ...
})
```

### 4. Customize Generated Code
After generation, edit the code to:
- Add custom methods and properties
- Implement game-specific logic
- Configure inspector fields
- Add documentation comments

## Common Workflows

### Workflow 1: New Project Setup
```python
# 1. Core infrastructure
unity_designPattern_generate({"patternType": "singleton", "className": "GameManager", ...})
unity_designPattern_generate({"patternType": "observer", "className": "EventManager", ...})
unity_designPattern_generate({"patternType": "servicelocator", "className": "ServiceLocator", ...})

# 2. Initialize in GameManager.Awake()
# 3. Register services in GameManager.Start()
```

### Workflow 2: Combat System
```python
# 1. Object pools for performance
unity_designPattern_generate({"patternType": "objectpool", "className": "BulletPool", ...})
unity_designPattern_generate({"patternType": "objectpool", "className": "ParticlePool", ...})

# 2. Enemy spawning
unity_designPattern_generate({"patternType": "factory", "className": "EnemyFactory", ...})

# 3. Wire up event system
# EventManager.Instance.Publish("EnemyKilled", enemyType)
```

### Workflow 3: Player Controller
```python
# 1. State machine for player
unity_designPattern_generate({"patternType": "statemachine", "className": "PlayerStateMachine", ...})

# 2. Command pattern for abilities (with undo)
unity_designPattern_generate({"patternType": "command", "className": "AbilityManager", ...})

# 3. Implement states: Idle, Move, Jump, Attack, Die
```

## Tips

1. **Always use namespaces** - Organize your code properly
2. **Edit generated code** - Customize for your specific needs
3. **Test patterns** - Unity menu: Tools > SkillForUnity > Test Pattern Generation
4. **Read generated comments** - They include usage examples
5. **Combine wisely** - Don't over-engineer, use what you need

## Next Steps

After generating patterns:
1. Review the generated code
2. Customize for your game's needs
3. Write unit tests
4. Integrate with your existing systems
5. Document your architecture

**Happy coding!** 🎮
