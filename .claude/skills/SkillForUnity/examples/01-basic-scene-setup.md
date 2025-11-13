# Example 1: Basic 3D Scene Setup

**Goal**: Create a simple 3D game scene with a player, ground, camera, and lighting.

**Difficulty**: Beginner
**Time**: 5 minutes

## Prerequisites

- Unity Editor 2021.3 or higher
- MCP Bridge running (Tools > MCP Assistant > Start Bridge)
- MCP client connected

## What You'll Create

- A 3D scene with proper lighting
- A player capsule at the spawn point
- A ground plane
- A directional light
- A main camera positioned correctly

## Step-by-Step Guide

### 1. Set Up the Scene

First, create a new 3D scene with default settings:

```python
unity_scene_quickSetup({
    "setupType": "3D"
})
```

This automatically creates:
- Main Camera at (0, 1, -10)
- Directional Light with default intensity

### 2. Create the Ground

Add a large plane for the ground:

```python
unity_gameobject_createFromTemplate({
    "template": "Plane",
    "name": "Ground",
    "position": {"x": 0, "y": 0, "z": 0},
    "scale": {"x": 10, "y": 1, "z": 10}
})
```

### 3. Create the Player

Add a capsule to represent the player:

```python
unity_gameobject_createFromTemplate({
    "template": "Player",
    "name": "Player",
    "position": {"x": 0, "y": 1, "z": 0}
})
```

The Player template includes:
- Capsule mesh
- Capsule collider
- Rigidbody (for physics)

### 4. Add Some Obstacles

Create a few cube obstacles:

```python
unity_batch_execute({
    "operations": [
        {
            "tool": "gameObjectCreateFromTemplate",
            "payload": {
                "template": "Cube",
                "name": "Obstacle1",
                "position": {"x": 3, "y": 0.5, "z": 0}
            }
        },
        {
            "tool": "gameObjectCreateFromTemplate",
            "payload": {
                "template": "Cube",
                "name": "Obstacle2",
                "position": {"x": -3, "y": 0.5, "z": 0}
            }
        },
        {
            "tool": "gameObjectCreateFromTemplate",
            "payload": {
                "template": "Cube",
                "name": "Obstacle3",
                "position": {"x": 0, "y": 0.5, "z": 3}
            }
        }
    ]
})
```

### 5. Verify the Scene

Check the hierarchy to confirm everything was created:

```python
unity_context_inspect({
    "includeHierarchy": True,
    "maxDepth": 2
})
```

## Expected Result

Your scene hierarchy should look like:

```
Main Camera
Directional Light
Ground
Player
Obstacle1
Obstacle2
Obstacle3
```

In the Scene view, you should see:
- A large gray plane (ground)
- A capsule at the center (player)
- Three cubes positioned around the player
- Good lighting from the directional light

## Next Steps

Try these enhancements:

1. **Add Colors**: Create materials and apply them to objects
2. **Adjust Camera**: Position the camera to follow the player
3. **Add Physics**: Make obstacles fall or react to collisions
4. **Add Scripts**: Attach movement scripts to the player

## Common Issues

**Issue**: Objects are too dark
- **Solution**: Adjust the Directional Light intensity:
  ```python
  unity_component_crud({
      "operation": "update",
      "gameObjectPath": "Directional Light",
      "componentType": "UnityEngine.Light",
      "propertyChanges": {
          "intensity": 1.5
      }
  })
  ```

**Issue**: Player falls through the ground
- **Solution**: Make sure the Ground has a collider:
  ```python
  unity_component_crud({
      "operation": "add",
      "gameObjectPath": "Ground",
      "componentType": "UnityEngine.MeshCollider"
  })
  ```

## Related Examples

- [02-ui-creation.md](02-ui-creation.md) - Add UI to this scene
- [03-game-level.md](03-game-level.md) - Expand into a full game level
