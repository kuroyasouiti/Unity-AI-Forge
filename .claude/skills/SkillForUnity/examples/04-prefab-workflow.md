# Example 04: Prefab Workflow - Create, Modify, and Reuse Assets

This example demonstrates how to work with Unity prefabs using MCP Skill. You'll learn to create prefabs from GameObjects, instantiate them in scenes, make modifications, and manage prefab overrides.

## What You'll Learn

- Creating prefabs from scene GameObjects
- Instantiating prefabs in the scene
- Modifying prefab instances
- Applying and reverting instance overrides
- Unpacking prefab instances
- Inspecting prefab assets

## Prerequisites

- Unity Editor running with MCP Bridge connected
- Completed examples 01-03 (or basic Unity knowledge)

## Step 1: Create a Reusable Enemy Prefab

First, create an enemy GameObject that we'll convert to a prefab:

```python
# Set up 3D scene
unity_scene_quickSetup({"setupType": "3D"})

# Create the enemy GameObject using template
unity_gameobject_createFromTemplate({
    "template": "Capsule",
    "name": "Enemy_Template",
    "position": {"x": 0, "y": 1, "z": 0}
})

# Customize with components and children
unity_template_manage({
    "operation": "customize",
    "gameObjectPath": "Enemy_Template",
    "components": [
        {
            "type": "UnityEngine.CapsuleCollider",
            "properties": {
                "height": 2.0,
                "radius": 0.5
            }
        },
        {
            "type": "UnityEngine.Rigidbody",
            "properties": {
                "mass": 2.0,
                "useGravity": True,
                "constraints": 112  # Freeze rotation X, Z
            }
        }
    ],
    "children": [
        {
            "name": "HealthBar",
            "components": [
                {
                    "type": "UnityEngine.Canvas"
                }
            ]
        },
        {
            "name": "DetectionZone",
            "components": [
                {
                    "type": "UnityEngine.SphereCollider",
                    "properties": {
                        "isTrigger": True,
                        "radius": 3.0
                    }
                }
            ]
        }
    ]
})
```

## Step 2: Create the Prefab Asset

Now convert the GameObject to a prefab:

```python
# Create prefab from the Enemy_Template GameObject
unity_prefab_crud({
    "operation": "create",
    "gameObjectPath": "Enemy_Template",
    "prefabPath": "Assets/Prefabs/Enemy.prefab"
})

# Inspect the prefab to verify it was created correctly
unity_prefab_crud({
    "operation": "inspect",
    "prefabPath": "Assets/Prefabs/Enemy.prefab"
})
```

## Step 3: Instantiate Multiple Prefab Instances

Create several instances of the enemy prefab:

```python
# Create a container for all enemies
unity_gameobject_crud({
    "operation": "create",
    "name": "Enemies"
})

# Instantiate enemy at different positions using batch operations
positions = [
    {"x": 5, "y": 1, "z": 5},
    {"x": -5, "y": 1, "z": 5},
    {"x": 5, "y": 1, "z": -5},
    {"x": -5, "y": 1, "z": -5},
    {"x": 0, "y": 1, "z": 7}
]

# Create multiple instances
for i, pos in enumerate(positions):
    unity_prefab_crud({
        "operation": "instantiate",
        "prefabPath": "Assets/Prefabs/Enemy.prefab",
        "parentPath": "Enemies"
    })

    # Rename and position each instance
    unity_gameobject_crud({
        "operation": "rename",
        "gameObjectPath": f"Enemies/Enemy(Clone)",
        "name": f"Enemy_{i+1}"
    })

    unity_component_crud({
        "operation": "update",
        "gameObjectPath": f"Enemies/Enemy_{i+1}",
        "componentType": "UnityEngine.Transform",
        "propertyChanges": {
            "position": pos
        }
    })
```


## Step 4: Modify Prefab Instances

Make unique modifications to specific instances:

```python
# Make Enemy_1 larger and heavier (boss enemy)
unity_component_crud({
    "operation": "update",
    "gameObjectPath": "Enemies/Enemy_1",
    "componentType": "UnityEngine.Transform",
    "propertyChanges": {
        "scale": {"x": 1.5, "y": 1.5, "z": 1.5}
    }
})

unity_component_crud({
    "operation": "update",
    "gameObjectPath": "Enemies/Enemy_1",
    "componentType": "UnityEngine.Rigidbody",
    "propertyChanges": {
        "mass": 5.0
    }
})

# Make Enemy_2 faster (scout enemy)
unity_component_crud({
    "operation": "update",
    "gameObjectPath": "Enemies/Enemy_2",
    "componentType": "UnityEngine.Rigidbody",
    "propertyChanges": {
        "mass": 1.0
    }
})
```

## Step 5: Apply Instance Overrides to Prefab

If you want to save the modifications from Enemy_1 back to the prefab:

```python
# Apply overrides from Enemy_1 back to the Enemy prefab
unity_prefab_crud({
    "operation": "applyOverrides",
    "gameObjectPath": "Enemies/Enemy_1"
})

# Now all future instances will have the larger scale and heavier mass
```

## Step 6: Revert Instance Overrides

If you want to reset an instance back to the prefab's original state:

```python
# Revert Enemy_2 back to prefab defaults
unity_prefab_crud({
    "operation": "revertOverrides",
    "gameObjectPath": "Enemies/Enemy_2"
})

# Enemy_2 is now identical to the prefab again
```

## Step 7: Update Prefab Asset Directly

Modify the prefab asset itself:

```python
# First, let's modify the original template GameObject
unity_component_crud({
    "operation": "update",
    "gameObjectPath": "Enemy_Template",
    "componentType": "UnityEngine.CapsuleCollider",
    "propertyChanges": {
        "radius": 0.6  # Make collider slightly larger
    }
})

# Update the prefab asset with changes from the template
unity_prefab_crud({
    "operation": "update",
    "gameObjectPath": "Enemy_Template",
    "prefabPath": "Assets/Prefabs/Enemy.prefab"
})

# All instances without overrides will automatically update!
```

## Step 8: Unpack a Prefab Instance

Convert a prefab instance back to regular GameObjects:

```python
# Unpack Enemy_3 (removes prefab connection)
unity_prefab_crud({
    "operation": "unpack",
    "gameObjectPath": "Enemies/Enemy_3",
    "unpackMode": "Completely"
})

# Enemy_3 is now a regular GameObject, not connected to the prefab
# Changes to the prefab won't affect it anymore
```

## Step 9: Create Nested Prefabs

Create a weapon prefab and add it to the enemy:

```python
# Create a weapon GameObject
unity_gameobject_createFromTemplate({
    "template": "Cube",
    "name": "Sword",
    "scale": {"x": 0.2, "y": 1.0, "z": 0.1}
})

# Create weapon prefab
unity_prefab_crud({
    "operation": "create",
    "gameObjectPath": "Sword",
    "prefabPath": "Assets/Prefabs/Sword.prefab"
})

# Instantiate sword as child of enemy template
unity_prefab_crud({
    "operation": "instantiate",
    "prefabPath": "Assets/Prefabs/Sword.prefab",
    "parentPath": "Enemy_Template"
})

# Position the sword
unity_component_crud({
    "operation": "update",
    "gameObjectPath": "Enemy_Template/Sword(Clone)",
    "componentType": "UnityEngine.Transform",
    "propertyChanges": {
        "position": {"x": 0.5, "y": 1, "z": 0},
        "rotation": {"x": 0, "y": 0, "z": 45}
    }
})

# Update the enemy prefab to include the sword
unity_prefab_crud({
    "operation": "update",
    "gameObjectPath": "Enemy_Template",
    "prefabPath": "Assets/Prefabs/Enemy.prefab"
})

# Now all new enemy instances will have a sword!
```

## Step 10: Inspect and Verify

Check the prefab structure and instances:

```python
# Inspect the enemy prefab
unity_prefab_crud({
    "operation": "inspect",
    "prefabPath": "Assets/Prefabs/Enemy.prefab"
})

# Find all instances of the enemy prefab in the scene
unity_gameobject_crud({
    "operation": "findMultiple",
    "pattern": "Enemies/Enemy*",
    "maxResults": 20
})

# Inspect a specific instance with details
unity_gameobject_crud({
    "operation": "inspect",
    "gameObjectPath": "Enemies/Enemy_1",
    "includeProperties": True
})
```

## Complete Prefab Workflow Example

Here's a complete script combining all concepts:

```python
# 1. Create template
unity_gameobject_createFromTemplate({
    "template": "Cube",
    "name": "Powerup_Template",
    "scale": {"x": 0.5, "y": 0.5, "z": 0.5}
})

# 2. Add components
unity_component_crud({
    "operation": "add",
    "gameObjectPath": "Powerup_Template",
    "componentType": "UnityEngine.SphereCollider",
    "propertyChanges": {"isTrigger": True}
})

# 3. Create prefab
unity_prefab_crud({
    "operation": "create",
    "gameObjectPath": "Powerup_Template",
    "prefabPath": "Assets/Prefabs/Powerup.prefab"
})

# 4. Instantiate multiple times
for i in range(5):
    unity_prefab_crud({
        "operation": "instantiate",
        "prefabPath": "Assets/Prefabs/Powerup.prefab"
    })

# 5. Modify one instance
unity_component_crud({
    "operation": "update",
    "gameObjectPath": "Powerup(Clone)",
    "componentType": "UnityEngine.Transform",
    "propertyChanges": {
        "scale": {"x": 1.0, "y": 1.0, "z": 1.0}
    }
})

# 6. Apply changes back to prefab
unity_prefab_crud({
    "operation": "applyOverrides",
    "gameObjectPath": "Powerup(Clone)"
})
```

## Best Practices

1. **Always Use Prefabs for Repeated Objects**
   - Enemies, collectibles, UI elements, environmental props
   - Easier to maintain and update

2. **Organize Prefabs in Folders**
   - `Assets/Prefabs/Characters/`
   - `Assets/Prefabs/Environment/`
   - `Assets/Prefabs/UI/`

3. **Use Nested Prefabs**
   - Break complex objects into smaller prefab components
   - Easier to reuse parts (e.g., wheels on different vehicles)

4. **Apply vs. Revert Carefully**
   - `applyOverrides`: Saves changes to the prefab (affects all instances)
   - `revertOverrides`: Discards changes (resets to prefab)

5. **Unpack Only When Necessary**
   - Unpacking breaks the prefab connection
   - Only unpack if you need a unique, one-off variation

## Performance Tips

When working with many prefabs:

```python
# Use includeProperties=false for faster inspection
unity_gameobject_crud({
    "operation": "inspectMultiple",
    "pattern": "Enemies/*",
    "includeComponents": False,
    "maxResults": 100
})

# Instantiate multiple prefabs
for i in range(5):
    unity_prefab_crud({
        "operation": "instantiate",
        "prefabPath": "Assets/Prefabs/Enemy.prefab",
        "parentPath": "Enemies"
    })
```

## Common Issues

**Issue**: Changes to prefab not affecting instances
**Solution**: Ensure instances don't have overrides. Use `revertOverrides` first.

**Issue**: Cannot modify prefab instances
**Solution**: Check that instances are still connected to prefab (not unpacked).

**Issue**: Nested prefab modifications lost
**Solution**: Apply overrides from innermost to outermost prefab.

---

**See Also:**
- [01-basic-scene-setup.md](01-basic-scene-setup.md) - Basic scene creation
- [02-ui-creation.md](02-ui-creation.md) - UI prefab workflow
- [03-game-level.md](03-game-level.md) - Using prefabs in levels
- CLAUDE.md - Prefab Management section for API details
