# Getting Started with Unity MCP Skill

This guide will help you get started with Unity MCP Skill and create your first Unity project using AI assistance.

## Prerequisites

Before you begin, ensure you have:

- ✅ Unity Editor 2021.3 or higher installed
- ✅ Python 3.10 or higher installed
- ✅ Basic understanding of Unity concepts (GameObjects, Components, Scenes)
- ✅ An MCP client (Claude Desktop, Cursor, or compatible AI client)

## Installation

See [QUICKSTART.md](../../QUICKSTART.md) for detailed installation instructions.

Quick summary:
1. Install the skill using the setup script
2. Start Unity MCP Bridge in Unity Editor
3. Configure your MCP client
4. Test the connection

## Your First Unity MCP Session

### Step 1: Verify Connection

First, ensure the Unity MCP bridge is connected:

```
Can you check if Unity is connected?
```

The AI will call `unity_ping()` and confirm connectivity.

### Step 2: Create a Simple 3D Scene

Let's create a basic 3D scene with a player and some obstacles:

```
Create a 3D scene with a player character and 3 cubes positioned around it as obstacles.
```

The AI will:
1. Set up a 3D scene with camera and lighting
2. Create a player GameObject
3. Create obstacle cubes
4. Position everything appropriately

### Step 3: Add Physics

Next, let's add physics components:

```
Add a Rigidbody to the player and BoxColliders to the obstacles.
```

### Step 4: Inspect the Scene

Check what was created:

```
Show me the current scene hierarchy with all components.
```

## Understanding Tool Usage

The AI uses specific Unity MCP tools behind the scenes. Here's what happens:

### When you say: "Create a 3D scene"

The AI uses:
```python
unity_scene_quickSetup({"setupType": "3D"})
```

### When you say: "Create a player"

The AI uses:
```python
unity_gameobject_createFromTemplate({
    "template": "Player",
    "position": {"x": 0, "y": 1, "z": 0}
})
```

### When you say: "Add a Rigidbody"

The AI uses:
```python
unity_component_crud({
    "operation": "add",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Rigidbody"
})
```

## Common Workflows

### Creating a UI Menu

```
Create a UI scene with a main menu. The menu should have:
- A title at the top
- Three buttons in the center: Start, Options, Quit
- Use vertical layout for the buttons
```

### Building a Level

```
Create a game level with:
- A large ground plane
- A player at the center
- 5 enemies positioned around the edges
- Some collectible spheres scattered around
```

### Creating C# Scripts

```
Create a PlayerMovement script that moves the player using WASD keys with a speed of 5 units per second.
```

**IMPORTANT:** The AI will automatically use `unity_script_batch_manage` for script operations, which handles compilation correctly.

## Best Practices

### 1. Be Specific

❌ "Create some enemies"
✅ "Create 5 enemy capsules positioned in a circle around the origin at radius 10"

### 2. Check Before Major Changes

```
Show me all GameObjects with Rigidbody components before I modify them.
```

### 3. Use Natural Language

You can ask questions naturally:
- "What components does the Player have?"
- "Move all enemies higher by 2 units"
- "Make all buttons 20% larger"

### 4. Iterate Incrementally

Instead of asking for everything at once, build gradually:

```
1. Create a UI scene
2. Add a panel with a vertical layout
3. Add 3 buttons to the panel
4. Make the buttons taller
5. Add onClick listeners to the buttons
```

## Understanding Context

The AI maintains context about your Unity project:

```
# First request
Create a player at position (0, 1, 0)

# Later requests (AI remembers the player exists)
Add a camera as a child of the player
Move the player to (5, 1, 0)
Add a health bar UI above the player
```

## Performance Optimization

### For Large Scenes

When working with many GameObjects:

```
Find all enemies (limit to first 100 objects for speed)
```

The AI will use:
```python
unity_gameobject_crud({
    "operation": "findMultiple",
    "pattern": "Enemy*",
    "maxResults": 100
})
```

### For Quick Checks

```
Check if the player has a Rigidbody (don't show all properties)
```

The AI will use:
```python
unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Rigidbody",
    "includeProperties": False  # 10x faster
})
```

## Common Mistakes to Avoid

### 1. ❌ Creating UI Without Canvas

```
# This will fail
Create a button
```

```
# This works
Create a UI scene, then add a button
```

### 2. ❌ Forgetting Physics Setup

```
# Incomplete
Create a player and add a Rigidbody
```

```
# Complete
Create a player, add a Rigidbody, and add a CapsuleCollider
```

### 3. ❌ Not Saving Work

```
# Save after major changes
Save the current scene
```

## Next Steps

Now that you understand the basics:

1. **Try the examples** - Work through [examples/](../../examples/)
2. **Learn the tools** - Read [API Reference](../api-reference/)
3. **Optimize your workflow** - Study [Best Practices](best-practices.md)
4. **Troubleshoot issues** - Check [Troubleshooting](../troubleshooting.md)

## Getting Help

If you encounter issues:

1. Check [Troubleshooting Guide](../troubleshooting.md)
2. Verify Unity MCP Bridge is running (Tools > MCP Assistant)
3. Check Unity Console for error messages
4. Review [CLAUDE.md](../../../../../CLAUDE.md) for detailed documentation

## Example: Complete First Project

Here's a complete conversation to create a simple game:

```
User: Create a 3D scene

AI: [Creates 3D scene with camera and light]

User: Add a ground plane scaled to 10x10

AI: [Creates and scales ground plane]

User: Create a player capsule at (0, 1, 0) with a Rigidbody and CapsuleCollider

AI: [Creates player with components]

User: Create a PlayerMovement script that uses WASD for movement at 5 units/second

AI: [Creates script using unity_script_batch_manage, waits for compilation]

User: Add the PlayerMovement script to the player with moveSpeed set to 5

AI: [Adds script component to player GameObject]

User: Create 3 cube obstacles around the player

AI: [Creates 3 cubes at different positions]

User: Save the scene as "Assets/Scenes/FirstGame.unity"

AI: [Saves scene]

User: Show me the final scene hierarchy

AI: [Displays complete hierarchy with all components]
```

Congratulations! You've created your first Unity project using AI assistance!

---

**Next:** [Best Practices](best-practices.md)
