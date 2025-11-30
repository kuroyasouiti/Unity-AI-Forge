# GameKitUICommand - UI Command Hub

## Overview

`GameKitUICommand` acts as a bridge between UI controls and `GameKitActor`'s UnityEvents, providing a structured way to translate UI interactions into actor commands. It's designed for games that use UI buttons for character control, such as mobile games, touch-based interfaces, or strategic games with command panels.

## Core Concept

Instead of directly wiring UI buttons to actor methods, `GameKitUICommand` provides:
- **Centralized Command Management**: All UI-to-actor bindings in one place
- **Type-Safe Command System**: Predefined command types with parameter support
- **Flexible Actor Targeting**: Switch targets dynamically
- **Performance Optimization**: Cached actor references

## Command Types

### Move Command
Maps to `GameKitActor.OnMoveInput(Vector3)`

```csharp
// Register directional button
uiCommand.RegisterDirectionalButton("moveRight", rightButton, new Vector3(1, 0, 0));

// Or execute directly
uiCommand.ExecuteMoveCommand(Vector3.forward);
```

### Jump Command
Maps to `GameKitActor.OnJumpInput()`

```csharp
uiCommand.RegisterButton("jump", jumpButton, GameKitUICommand.CommandType.Jump);

// Or execute directly
uiCommand.ExecuteJumpCommand();
```

### Action Command
Maps to `GameKitActor.OnActionInput(string)` with parameter support

```csharp
// Register with custom parameter
uiCommand.RegisterButton("attack", attackButton, GameKitUICommand.CommandType.Action, "sword");
uiCommand.RegisterButton("heal", healButton, GameKitUICommand.CommandType.Action, "potion");

// Or execute directly
uiCommand.ExecuteActionCommand("interact");
```

### Look Command
Maps to `GameKitActor.OnLookInput(Vector2)`

```csharp
// Register look direction button
var binding = new GameKitUICommand.UICommandBinding
{
    commandName = "lookRight",
    commandType = GameKitUICommand.CommandType.Look,
    button = lookButton,
    lookDirection = new Vector2(1, 0)
};
uiCommand.RegisterButton("lookRight", lookButton, GameKitUICommand.CommandType.Look);

// Or execute directly
uiCommand.ExecuteLookCommand(new Vector2(0, 1));
```

### Custom Command
Sends `SendMessage` for backward compatibility

```csharp
uiCommand.RegisterButton("special", specialButton, GameKitUICommand.CommandType.Custom);
// Sends "OnCommand_special" message to actor GameObject
```

## Setup Examples

### Mobile Touch Controls

```csharp
public class MobileTouchController : MonoBehaviour
{
    public GameKitUICommand uiCommand;
    public GameKitActor player;
    
    // UI Buttons
    public Button upButton;
    public Button downButton;
    public Button leftButton;
    public Button rightButton;
    public Button jumpButton;
    public Button attackButton;
    
    void Start()
    {
        // Set target
        uiCommand.SetTargetActor(player);
        
        // Register directional buttons (for 2D)
        uiCommand.RegisterDirectionalButton("moveUp", upButton, new Vector3(0, 1, 0));
        uiCommand.RegisterDirectionalButton("moveDown", downButton, new Vector3(0, -1, 0));
        uiCommand.RegisterDirectionalButton("moveLeft", leftButton, new Vector3(-1, 0, 0));
        uiCommand.RegisterDirectionalButton("moveRight", rightButton, new Vector3(1, 0, 0));
        
        // Register action buttons
        uiCommand.RegisterButton("jump", jumpButton, GameKitUICommand.CommandType.Jump);
        uiCommand.RegisterButton("attack", attackButton, GameKitUICommand.CommandType.Action, "melee");
    }
}
```

### Virtual Joystick Integration

```csharp
public class VirtualJoystickController : MonoBehaviour
{
    public GameKitUICommand uiCommand;
    public GameKitActor player;
    public Joystick virtualJoystick; // Your joystick component
    
    void Start()
    {
        uiCommand.SetTargetActor(player);
    }
    
    void Update()
    {
        // Convert joystick input to move command
        Vector3 direction = new Vector3(virtualJoystick.Horizontal, 0, virtualJoystick.Vertical);
        
        if (direction.magnitude > 0.1f)
        {
            uiCommand.ExecuteMoveCommand(direction.normalized);
        }
    }
}
```

### Action Button Panel

```csharp
public class ActionPanel : MonoBehaviour
{
    public GameKitUICommand uiCommand;
    public Button[] actionButtons;
    public string[] actionNames = { "attack", "defend", "useItem", "special" };
    
    void Start()
    {
        // Register all action buttons
        for (int i = 0; i < actionButtons.Length && i < actionNames.Length; i++)
        {
            string actionName = actionNames[i];
            uiCommand.RegisterButton(
                actionName,
                actionButtons[i],
                GameKitUICommand.CommandType.Action,
                actionName
            );
        }
    }
}
```

### Strategy Game Command Panel

```csharp
public class StrategyCommandPanel : MonoBehaviour
{
    public GameKitUICommand uiCommand;
    public Button moveButton;
    public Button attackButton;
    public Button defendButton;
    public Button specialButton;
    
    private GameKitActor selectedUnit;
    
    public void SelectUnit(GameKitActor unit)
    {
        selectedUnit = unit;
        uiCommand.SetTargetActor(unit);
        UpdateButtonStates();
    }
    
    void Start()
    {
        // Register commands
        uiCommand.RegisterButton("move", moveButton, GameKitUICommand.CommandType.Action, "move");
        uiCommand.RegisterButton("attack", attackButton, GameKitUICommand.CommandType.Action, "attack");
        uiCommand.RegisterButton("defend", defendButton, GameKitUICommand.CommandType.Action, "defend");
        uiCommand.RegisterButton("special", specialButton, GameKitUICommand.CommandType.Action, "special");
    }
    
    void UpdateButtonStates()
    {
        // Enable/disable buttons based on selected unit's abilities
        bool hasUnit = selectedUnit != null;
        moveButton.interactable = hasUnit;
        attackButton.interactable = hasUnit && selectedUnit.Behavior == GameKitActor.BehaviorProfile.TwoDLinear;
        // etc.
    }
}
```

### Dynamic Actor Switching

```csharp
public class PartyController : MonoBehaviour
{
    public GameKitUICommand uiCommand;
    public GameKitActor[] partyMembers;
    private int currentMemberIndex = 0;
    
    void Start()
    {
        SwitchToMember(0);
    }
    
    public void SwitchToNextMember()
    {
        currentMemberIndex = (currentMemberIndex + 1) % partyMembers.Length;
        SwitchToMember(currentMemberIndex);
    }
    
    void SwitchToMember(int index)
    {
        if (index >= 0 && index < partyMembers.Length)
        {
            uiCommand.SetTargetActor(partyMembers[index]);
            Debug.Log($"Switched to {partyMembers[index].ActorId}");
        }
    }
}
```

## Best Practices

### 1. Actor Reference Management

**Preferred: Direct Reference**
```csharp
// Best performance - no lookup needed
uiCommand.SetTargetActor(playerActor);
```

**Alternative: ID Lookup**
```csharp
// Useful when actor isn't immediately available
uiCommand.SetTargetActor("player_001");
// Actor will be found on first command execution
```

### 2. Command Naming

Use clear, descriptive names:
```csharp
// Good
uiCommand.RegisterButton("moveUp", upButton, ...);
uiCommand.RegisterButton("attackPrimary", attackButton, ...);

// Avoid
uiCommand.RegisterButton("btn1", button1, ...);
uiCommand.RegisterButton("cmd", commandButton, ...);
```

### 3. Parameter Organization

Group related actions with parameters:
```csharp
// Weapon actions
uiCommand.RegisterButton("attackSword", swordButton, CommandType.Action, "sword");
uiCommand.RegisterButton("attackBow", bowButton, CommandType.Action, "bow");
uiCommand.RegisterButton("attackMagic", magicButton, CommandType.Action, "magic");

// Item actions
uiCommand.RegisterButton("useHealthPotion", healthButton, CommandType.Action, "potion_health");
uiCommand.RegisterButton("useManaPotion", manaButton, CommandType.Action, "potion_mana");
```

### 4. Performance

Enable actor caching for better performance:
```csharp
// In Inspector or code:
uiCommand.cacheActorReference = true; // Default is true
```

### 5. Debugging

Enable command logging during development:
```csharp
uiCommand.logCommands = true; // Will log all executed commands
```

## API Reference

### Properties

- `string PanelId` - Unique identifier for this command panel
- `string TargetActorId` - Target actor's ID
- `GameKitActor TargetActor` - Direct reference to target actor
- `bool cacheActorReference` - Cache actor lookup for performance
- `bool logCommands` - Log command execution for debugging

### Methods

#### Initialization
- `void Initialize(string id, string actorId)` - Initialize panel with ID and target actor ID
- `void SetTargetActor(GameKitActor actor)` - Set target by reference
- `void SetTargetActor(string actorId)` - Set target by ID

#### Button Registration
- `void RegisterButton(string commandName, Button button, CommandType commandType = Action, string commandParam = null)` - Register a button with command
- `void RegisterDirectionalButton(string commandName, Button button, Vector3 direction)` - Register movement button with direction

#### Command Execution
- `void ExecuteCommand(string commandName)` - Execute registered command by name
- `void ExecuteMoveCommand(Vector3 direction)` - Execute move command
- `void ExecuteJumpCommand()` - Execute jump command
- `void ExecuteActionCommand(string actionName)` - Execute action command
- `void ExecuteLookCommand(Vector2 direction)` - Execute look command

#### Management
- `void ClearBindings()` - Remove all command bindings
- `List<string> GetCommandNames()` - Get list of registered command names
- `bool HasCommand(string commandName)` - Check if command exists

## Common Patterns

### D-Pad Controller
```csharp
// 4-directional movement
uiCommand.RegisterDirectionalButton("up", upBtn, Vector3.forward);
uiCommand.RegisterDirectionalButton("down", downBtn, Vector3.back);
uiCommand.RegisterDirectionalButton("left", leftBtn, Vector3.left);
uiCommand.RegisterDirectionalButton("right", rightBtn, Vector3.right);
```

### 8-Directional Controller
```csharp
// Include diagonals
uiCommand.RegisterDirectionalButton("up", upBtn, Vector3.forward);
uiCommand.RegisterDirectionalButton("upRight", upRightBtn, (Vector3.forward + Vector3.right).normalized);
uiCommand.RegisterDirectionalButton("right", rightBtn, Vector3.right);
// ... etc
```

### Context-Sensitive Actions
```csharp
// Update action button based on context
public void UpdateContextAction(string context)
{
    // Clear old binding
    uiCommand.ClearBindings();
    
    // Register new context-specific action
    switch (context)
    {
        case "door":
            uiCommand.RegisterButton("action", actionBtn, CommandType.Action, "openDoor");
            actionButtonText.text = "Open";
            break;
        case "chest":
            uiCommand.RegisterButton("action", actionBtn, CommandType.Action, "openChest");
            actionButtonText.text = "Loot";
            break;
        case "npc":
            uiCommand.RegisterButton("action", actionBtn, CommandType.Action, "talk");
            actionButtonText.text = "Talk";
            break;
    }
}
```

## Integration with GameKit Actor

`GameKitUICommand` is specifically designed to work with actors using `ControlMode.UICommand`:

```csharp
// Create actor with UI command mode
var actor = gameObject.AddComponent<GameKitActor>();
actor.Initialize("player", BehaviorProfile.TwoDLinear, ControlMode.UICommand);

// Actor's events are now ready to receive UI commands
uiCommand.SetTargetActor(actor);
```

## Troubleshooting

**Commands not executing:**
- Verify target actor is set: `uiCommand.TargetActor != null`
- Check command is registered: `uiCommand.HasCommand("commandName")`
- Enable logging: `uiCommand.logCommands = true`

**Performance issues:**
- Enable actor caching: `uiCommand.cacheActorReference = true`
- Use direct actor reference instead of ID lookup

**Button not responding:**
- Ensure button has `Button` component
- Check button `interactable` is true
- Verify canvas has `GraphicRaycaster`

## See Also

- [GameKit Actor](./README.md#gamekit-actor)
- [GameKitInputSystemController](./README.md#gamekitinputsystemcontroller)
- [GameKitSimpleAI](./README.md#gamekitsimpleai)

