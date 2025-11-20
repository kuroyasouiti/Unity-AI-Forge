# Example 03: Building a Complete Game Level

This example demonstrates how to create a complete 3D game level with terrain, player, enemies, collectibles, and environmental objects using Unity MCP Skill.

## What You'll Create

- Ground plane with physics
- Player character with movement script
- Multiple enemy NPCs
- Collectible items
- Environmental obstacles
- Basic lighting setup

## Prerequisites

- Unity Editor running with MCP Bridge connected
- Basic understanding of Unity GameObjects and components

## Step 1: Scene Setup

First, set up a 3D scene with camera and lighting:

```python
# Create a new 3D scene with camera and directional light
unity_scene_quickSetup({"setupType": "3D"})

# Adjust camera position for better view
unity_component_crud({
    "operation": "update",
    "gameObjectPath": "Main Camera",
    "componentType": "UnityEngine.Transform",
    "propertyChanges": {
        "position": {"x": 0, "y": 8, "z": -10},
        "rotation": {"x": 30, "y": 0, "z": 0}
    }
})
```

## Step 2: Create the Ground

Create a large ground plane with physics:

```python
# Create ground
unity_gameobject_createFromTemplate({
    "template": "Plane",
    "name": "Ground",
    "position": {"x": 0, "y": 0, "z": 0},
    "scale": {"x": 10, "y": 1, "z": 10}
})

# Add collider for physics interactions
unity_component_crud({
    "operation": "add",
    "gameObjectPath": "Ground",
    "componentType": "UnityEngine.MeshCollider"
})
```

## Step 3: Create the Player

Create a player character with capsule shape:

```python
# Create player using template
unity_gameobject_createFromTemplate({
    "template": "Player",
    "name": "Player",
    "position": {"x": 0, "y": 1, "z": 0}
})

# Add physics components
unity_component_crud({
    "operation": "add",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Rigidbody",
    "propertyChanges": {
        "mass": 1.0,
        "useGravity": True,
        "constraints": 112  # Freeze rotation X and Z
    }
})

unity_component_crud({
    "operation": "add",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.CapsuleCollider",
    "propertyChanges": {
        "height": 2.0,
        "radius": 0.5,
        "center": {"x": 0, "y": 1, "z": 0}
    }
})
```

## Step 4: Create Player Movement Script

Create a simple player movement script:

```python
player_script = """using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    private Rigidbody rb;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Get input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Move player
        Vector3 movement = new Vector3(horizontal, 0f, vertical) * moveSpeed * Time.deltaTime;
        transform.Translate(movement, Space.World);

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }
}
"""

# Create the script using asset_crud
unity_asset_crud({
    "operation": "create",
    "assetPath": "Assets/Scripts/PlayerMovement.cs",
    "content": player_script
})

# Wait for Unity to compile the script before adding it as a component
unity_component_crud({
    "operation": "add",
    "gameObjectPath": "Player",
    "componentType": "PlayerMovement",
    "propertyChanges": {
        "moveSpeed": 5.0,
        "jumpForce": 5.0
    }
})

# Set the Ground tag
unity_tagLayer_manage({
    "operation": "addTag",
    "tag": "Ground"
})

unity_tagLayer_manage({
    "operation": "setTag",
    "gameObjectPath": "Ground",
    "tag": "Ground"
})
```

## Step 5: Create Enemies

Create multiple enemy NPCs using batch operations:

```python
# Create enemy hierarchy with components
unity_hierarchy_builder({
    "hierarchy": {
        "Enemies": {
            "children": {
                "Enemy1": {
                    "components": ["UnityEngine.CapsuleCollider", "UnityEngine.Rigidbody"],
                    "properties": {
                        "position": {"x": 5, "y": 1, "z": 5}
                    }
                },
                "Enemy2": {
                    "components": ["UnityEngine.CapsuleCollider", "UnityEngine.Rigidbody"],
                    "properties": {
                        "position": {"x": -5, "y": 1, "z": 5}
                    }
                },
                "Enemy3": {
                    "components": ["UnityEngine.CapsuleCollider", "UnityEngine.Rigidbody"],
                    "properties": {
                        "position": {"x": 5, "y": 1, "z": -5}
                    }
                },
                "Enemy4": {
                    "components": ["UnityEngine.CapsuleCollider", "UnityEngine.Rigidbody"],
                    "properties": {
                        "position": {"x": -5, "y": 1, "z": -5}
                    }
                }
            }
        }
    }
})

# Set all enemies to use red color (using batch update)
unity_component_crud({
    "operation": "addMultiple",
    "pattern": "Enemies/Enemy*",
    "componentType": "UnityEngine.MeshRenderer",
    "maxResults": 10
})
```

## Step 6: Create Collectibles

Add collectible spheres around the level:

```python
# Create collectibles container
unity_gameobject_crud({
    "operation": "create",
    "name": "Collectibles"
})

# Create multiple collectibles
unity_gameobject_createFromTemplate({
    "template": "Sphere",
    "name": "Coin1",
    "parentPath": "Collectibles",
    "position": {"x": 2, "y": 0.5, "z": 0},
    "scale": {"x": 0.3, "y": 0.3, "z": 0.3}
})

unity_gameobject_createFromTemplate({
    "template": "Sphere",
    "name": "Coin2",
    "parentPath": "Collectibles",
    "position": {"x": -2, "y": 0.5, "z": 2},
    "scale": {"x": 0.3, "y": 0.3, "z": 0.3}
})

unity_gameobject_createFromTemplate({
    "template": "Sphere",
    "name": "Coin3",
    "parentPath": "Collectibles",
    "position": {"x": 0, "y": 0.5, "z": -3},
    "scale": {"x": 0.3, "y": 0.3, "z": 0.3}
})

unity_gameobject_createFromTemplate({
    "template": "Sphere",
    "name": "Coin4",
    "parentPath": "Collectibles",
    "position": {"x": 3, "y": 0.5, "z": 3},
    "scale": {"x": 0.3, "y": 0.3, "z": 0.3}
})

# Add trigger colliders to all coins
unity_component_crud({
    "operation": "addMultiple",
    "pattern": "Collectibles/Coin*",
    "componentType": "UnityEngine.SphereCollider",
    "propertyChanges": {
        "isTrigger": True,
        "radius": 0.5
    },
    "maxResults": 10
})
```

## Step 7: Add Environmental Obstacles

Create walls and obstacles:

```python
# Create obstacles container
unity_gameobject_crud({
    "operation": "create",
    "name": "Obstacles"
})

# Create walls using hierarchy builder
unity_hierarchy_builder({
    "hierarchy": {
        "Obstacles": {
            "children": {
                "WallNorth": {
                    "components": ["UnityEngine.BoxCollider"],
                    "properties": {
                        "position": {"x": 0, "y": 1, "z": 10},
                        "scale": {"x": 20, "y": 2, "z": 0.5}
                    }
                },
                "WallSouth": {
                    "components": ["UnityEngine.BoxCollider"],
                    "properties": {
                        "position": {"x": 0, "y": 1, "z": -10},
                        "scale": {"x": 20, "y": 2, "z": 0.5}
                    }
                },
                "WallEast": {
                    "components": ["UnityEngine.BoxCollider"],
                    "properties": {
                        "position": {"x": 10, "y": 1, "z": 0},
                        "scale": {"x": 0.5, "y": 2, "z": 20}
                    }
                },
                "WallWest": {
                    "components": ["UnityEngine.BoxCollider"],
                    "properties": {
                        "position": {"x": -10, "y": 1, "z": 0},
                        "scale": {"x": 0.5, "y": 2, "z": 20}
                    }
                }
            }
        }
    }
})
```

## Step 8: Inspect the Final Scene

Check the complete scene hierarchy:

```python
# Get a complete overview of the scene
unity_context_inspect({
    "includeHierarchy": True,
    "includeComponents": True,
    "maxDepth": 3
})
```

## Expected Result

You should now have a complete game level with:
- ✓ Playable character with movement controls (WASD + Space)
- ✓ 4 enemy NPCs positioned around the level
- ✓ 4 collectible coins
- ✓ Boundary walls preventing player from falling off
- ✓ Physics-enabled ground
- ✓ Proper lighting and camera setup

## Next Steps

To enhance this level, you could:

1. **Add Enemy AI**: Create scripts to make enemies patrol or chase the player
2. **Collectible System**: Add scripts to detect when player collects coins
3. **UI Elements**: Create a score display and health bar
4. **Prefabs**: Convert enemies and collectibles to prefabs for reusability
5. **Materials**: Add colors and textures to differentiate objects

## Performance Tips

When creating large levels:

- Use `includeProperties=false` when inspecting to speed up queries
- Use `maxResults` parameter to limit batch operations
- Use component batch operations (`addMultiple`, `updateMultiple`) for efficiency
- Use hierarchy builder for complex nested structures

## Common Issues

**Issue**: Player falls through ground
**Solution**: Ensure Ground has a collider and Player has Rigidbody

**Issue**: Scripts not compiling
**Solution**: Use `unity_asset_crud` for creating/updating C# scripts. Unity will automatically detect and compile changes.

**Issue**: Enemy objects not visible
**Solution**: Add MeshRenderer and MeshFilter components with appropriate mesh references

---

**See Also:**
- [01-basic-scene-setup.md](01-basic-scene-setup.md) - Basic 3D scene creation
- [02-ui-creation.md](02-ui-creation.md) - UI system creation
- [04-prefab-workflow.md](04-prefab-workflow.md) - Working with prefabs
