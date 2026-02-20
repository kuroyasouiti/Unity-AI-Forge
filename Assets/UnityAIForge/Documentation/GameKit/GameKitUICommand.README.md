# GameKitUICommand - Code-Generated Command Panel

## Overview

`GameKitUICommand` generates a standalone C# script with UXML/USS assets that creates a button-based command panel using UI Toolkit. The generated code has zero dependency on the Unity-AI-Forge package.

## Core Concept

Instead of using runtime MonoBehaviours, GameKitUICommand uses **code generation**:

1. **UXML** - UI layout definition (buttons, labels)
2. **USS** - Stylesheet (colors, spacing, sizes)
3. **C# Script** - Component with UnityEvent bindings and button wiring

All three files are generated from templates and placed in the user's `Assets/` folder.

## MCP Tool

**Tool name:** `unity_gamekit_ui_command`
**Bridge name:** `gamekitUICommand`

## Operations

| Operation | Description | Compilation Required |
|-----------|-------------|---------------------|
| `createCommandPanel` | Generate UXML/USS + C# with buttons | Yes |
| `addCommand` | Add button to existing panel | No |
| `inspect` | View panel configuration | No |
| `delete` | Remove panel and generated files | No |

## Command Types

| Type | Description |
|------|-------------|
| `move` | Movement direction command (Vector3) |
| `jump` | Jump action |
| `action` | Generic action with string parameter |
| `look` | Look direction command (Vector2) |
| `custom` | Custom command (backward compatibility) |

## Usage via MCP

### Create a Command Panel

```python
unity_gamekit_ui_command({
    "operation": "createCommandPanel",
    "panelId": "playerControls",
    "layout": "horizontal",
    "commands": [
        {
            "name": "moveLeft",
            "label": "←",
            "commandType": "move",
            "moveDirection": {"x": -1, "y": 0, "z": 0}
        },
        {
            "name": "moveRight",
            "label": "→",
            "commandType": "move",
            "moveDirection": {"x": 1, "y": 0, "z": 0}
        },
        {
            "name": "jump",
            "label": "Jump",
            "commandType": "jump"
        },
        {
            "name": "attack",
            "label": "Attack",
            "commandType": "action",
            "commandParameter": "melee"
        }
    ]
})

# Wait for compilation (required after create)
unity_compilation_await()
```

### Add a Command

```python
unity_gamekit_ui_command({
    "operation": "addCommand",
    "panelId": "playerControls",
    "command": {
        "name": "dodge",
        "label": "Dodge",
        "commandType": "action",
        "commandParameter": "dodge"
    }
})
```

### Inspect

```python
unity_gamekit_ui_command({
    "operation": "inspect",
    "panelId": "playerControls"
})
```

### Delete

```python
unity_gamekit_ui_command({
    "operation": "delete",
    "panelId": "playerControls"
})
```

## Generated Code

### What Gets Generated

When you call `createCommandPanel`, three files are generated:

```
Assets/Scripts/Generated/PlayerControlsCommandPanel.cs   ← C# component
Assets/UI/Generated/playerControls.uxml                  ← UI layout
Assets/UI/Generated/playerControls.uss                   ← Stylesheet
```

### Generated C# Features

The generated script includes:

- **Static Registry**: `FindById(string id)` for easy access from other scripts
- **UnityEvent Bindings**: Each command fires a UnityEvent
- **Auto-wiring**: Buttons are automatically bound to their commands on `OnEnable`
- **Inspector Support**: Serialized fields for editor configuration

### Using Generated Code in Scripts

```csharp
// Find the generated command panel by its ID
var controls = PlayerControlsCommandPanel.FindById("playerControls");

// Subscribe to command events
controls.OnCommandExecuted.AddListener((commandName) => {
    Debug.Log($"Command executed: {commandName}");
});

// Execute commands programmatically
controls.ExecuteCommand("jump");
controls.ExecuteCommand("attack");
```

## Parameters Reference

### createCommandPanel

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `panelId` | string | Yes | Unique identifier for the panel |
| `commands` | array | Yes | Array of command definitions |
| `layout` | string | No | `horizontal`, `vertical`, `grid` |
| `className` | string | No | Custom C# class name |
| `outputPath` | string | No | Custom output directory |

### Command Object

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `name` | string | Yes | Command identifier |
| `label` | string | Yes | Button display text |
| `commandType` | string | Yes | `move`, `jump`, `action`, `look`, `custom` |
| `commandParameter` | string | No | Parameter for action/custom |
| `moveDirection` | Vector3 | No | Direction for move commands |
| `lookDirection` | Vector2 | No | Direction for look commands |
| `icon` | string | No | Icon path |

## Common Patterns

### Mobile D-Pad

```python
unity_gamekit_ui_command({
    "operation": "createCommandPanel",
    "panelId": "dpad",
    "layout": "grid",
    "commands": [
        {"name": "up", "label": "↑", "commandType": "move",
         "moveDirection": {"x": 0, "y": 0, "z": 1}},
        {"name": "down", "label": "↓", "commandType": "move",
         "moveDirection": {"x": 0, "y": 0, "z": -1}},
        {"name": "left", "label": "←", "commandType": "move",
         "moveDirection": {"x": -1, "y": 0, "z": 0}},
        {"name": "right", "label": "→", "commandType": "move",
         "moveDirection": {"x": 1, "y": 0, "z": 0}}
    ]
})
```

### Action Button Bar

```python
unity_gamekit_ui_command({
    "operation": "createCommandPanel",
    "panelId": "actionBar",
    "layout": "horizontal",
    "commands": [
        {"name": "attack", "label": "Attack", "commandType": "action",
         "commandParameter": "attack"},
        {"name": "defend", "label": "Defend", "commandType": "action",
         "commandParameter": "defend"},
        {"name": "skill", "label": "Skill", "commandType": "action",
         "commandParameter": "skill"},
        {"name": "item", "label": "Item", "commandType": "action",
         "commandParameter": "item"}
    ]
})
```

## See Also

- [GameKit Overview](./README.md) - 3-pillar architecture overview
- [GameKit Complete Guide](../MCPServer/SKILL_GAMEKIT.md) - All tool documentation
