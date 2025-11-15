---
name: SkillForUnity
description: Comprehensive Unity Editor integration through Model Context Protocol
license: MIT
---

# SkillForUnity - Unity Editor Integration via MCP

**Control Unity Editor directly from AI assistants through the Model Context Protocol.**

You are now working with SkillForUnity, a powerful system that lets you create, modify, and manage Unity projects directly from this conversation.

## Prerequisites

Before using these tools, ensure:
1. Unity Editor is open
2. MCP Bridge is started (Tools > MCP Assistant > Start Bridge)
3. Connection status shows "Connected"

## Core Capabilities

### 🎮 Scene Management
- **Quick setup**: Instantly create 3D, 2D, UI, or VR scenes with proper configuration
- **Scene operations**: Create, load, save, delete, and list scenes
- **Context inspection**: Get real-time scene hierarchy and GameObject information

### 🎨 GameObject Operations
- **Templates**: Create common GameObjects (Cube, Sphere, Player, Enemy, etc.) with one command
- **CRUD operations**: Create, rename, move, duplicate, delete GameObjects
- **Batch operations**: Find and modify multiple GameObjects using patterns
- **Hierarchy builder**: Build complex nested structures declaratively

### 🧩 Component Management
- **Add/Remove/Update**: Manage components on any GameObject
- **Property setting**: Set component properties including asset references
- **UnityEvent listeners**: Configure UI event handlers (Button.onClick, etc.)
- **Batch operations**: Add/remove/update components on multiple GameObjects

### 🖼️ UI Creation (UGUI)
- **Templates**: Create complete UI elements (Button, Panel, ScrollView, Dropdown, etc.)
- **Layout management**: Add and configure layout groups (Vertical, Horizontal, Grid)
- **Anchor presets**: Position UI elements correctly on Canvas

### 📦 Asset & Script Management
- **Asset CRUD**: Create, update, rename, duplicate, delete, and inspect assets
- **Script batch management**: Create/update multiple C# scripts with automatic compilation
- **Prefab workflow**: Create, instantiate, update, apply/revert prefab overrides

### 🎯 Advanced Features
- **Tilemap operations**: Create and modify 2D tilemaps
- **NavMesh**: Bake navigation meshes and configure NavMesh agents
- **Input System**: Create and manage Input Action assets
- **Project settings**: Read/write Unity project settings
- **Render pipeline**: Inspect and configure render pipeline settings
- **Build settings**: Manage build configurations and player settings

## Quick Start Commands

### Scene Setup
```python
# Set up a 3D game scene (Camera + Light)
unity_scene_quickSetup({"setupType": "3D"})

# Set up a UI scene (Canvas + EventSystem)
unity_scene_quickSetup({"setupType": "UI"})

# Set up a 2D scene
unity_scene_quickSetup({"setupType": "2D"})
```

### GameObject Creation
```python
# Create from template (fastest way)
unity_gameobject_createFromTemplate({
    "template": "Sphere",  # Cube, Sphere, Player, Enemy, etc.
    "name": "Ball",
    "position": {"x": 0, "y": 5, "z": 0},
    "scale": {"x": 0.5, "y": 0.5, "z": 0.5}
})

# Build complex hierarchy
unity_hierarchy_builder({
    "hierarchy": {
        "Player": {
            "components": ["Rigidbody", "CapsuleCollider"],
            "properties": {"position": {"x": 0, "y": 1, "z": 0}},
            "children": {
                "Camera": {
                    "components": ["Camera"],
                    "properties": {"position": {"x": 0, "y": 0.5, "z": -3}}
                }
            }
        }
    }
})
```

### UI Creation
```python
# Create button with one command
unity_ugui_createFromTemplate({
    "template": "Button",
    "text": "Start Game",
    "width": 200,
    "height": 50,
    "anchorPreset": "middle-center"
})

# Create complete UI layout
unity_hierarchy_builder({
    "hierarchy": {
        "MenuPanel": {
            "components": ["UnityEngine.UI.Image"],
            "children": {
                "Title": {"components": ["UnityEngine.UI.Text"]},
                "ButtonContainer": {
                    "components": ["UnityEngine.UI.VerticalLayoutGroup"],
                    "children": {
                        "PlayButton": {"components": ["UnityEngine.UI.Button", "UnityEngine.UI.Image"]},
                        "SettingsButton": {"components": ["UnityEngine.UI.Button", "UnityEngine.UI.Image"]}
                    }
                }
            }
        }
    },
    "parentPath": "Canvas"
})
```

### Component Management
```python
# Add component
unity_component_crud({
    "operation": "add",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Rigidbody"
})

# Update component properties
unity_component_crud({
    "operation": "update",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Transform",
    "propertyChanges": {
        "position": {"x": 0, "y": 1, "z": 0},
        "rotation": {"x": 0, "y": 45, "z": 0}
    }
})

# Fast inspection (existence check only)
unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.CharacterController",
    "includeProperties": False  # 10x faster!
})
```

### Scene Inspection
```python
# Get scene overview (fast)
unity_context_inspect({
    "includeHierarchy": True,
    "includeComponents": False,  # Skip properties for speed
    "maxDepth": 2
})

# Detailed GameObject inspection
unity_gameobject_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "includeProperties": False  # Use false for fast checks
})
```

### Script Management
```python
# ALWAYS use batch management for scripts
unity_script_batch_manage({
    "scripts": [
        {
            "operation": "create",
            "scriptPath": "Assets/Scripts/Player.cs",
            "content": "using UnityEngine;\n\npublic class Player : MonoBehaviour\n{\n    void Start()\n    {\n    }\n}"
        },
        {
            "operation": "create",
            "scriptPath": "Assets/Scripts/Enemy.cs",
            "content": "using UnityEngine;\n\npublic class Enemy : MonoBehaviour\n{\n    void Start()\n    {\n    }\n}"
        }
    ],
    "timeoutSeconds": 30
})
```

## Best Practices

### DO ✅
1. **Use templates** - 10x faster than manual creation
   ```python
   unity_ugui_createFromTemplate({"template": "Button"})  # Not manual GameObject + components
   ```

2. **Check context first** - Understand current state before changes
   ```python
   unity_context_inspect({"includeHierarchy": True, "includeComponents": False})
   ```

3. **Use hierarchy builder** - Create entire structures at once
   ```python
   unity_hierarchy_builder({"hierarchy": {...}})  # Not multiple individual creates
   ```

4. **Batch script operations** - Always use script batch manager
   ```python
   unity_script_batch_manage({"scripts": [...]})  # Single compilation for all scripts
   ```

5. **Optimize inspections** - Use `includeProperties=false` and `propertyFilter`
   ```python
   unity_component_crud({
       "operation": "inspect",
       "gameObjectPath": "Player",
       "componentType": "UnityEngine.Transform",
       "propertyFilter": ["position", "rotation"]  # Only specific properties
   })
   ```

6. **Limit batch operations** - Use `maxResults` to prevent timeouts
   ```python
   unity_component_crud({
       "operation": "addMultiple",
       "pattern": "Enemy*",
       "componentType": "UnityEngine.Rigidbody",
       "maxResults": 1000  # Safe limit
   })
   ```

### DON'T ❌
1. **Don't create UI manually** - Use templates instead
2. **Don't edit .meta files** - Unity manages these automatically
3. **Don't use asset tool for scripts** - Use script batch manager
4. **Don't skip context inspection** - Know what exists before modifying
5. **Don't use unlimited batch operations** - Always set `maxResults`

## Component Type Reference

### Common Unity Components
- Transform: `UnityEngine.Transform`
- Rigidbody: `UnityEngine.Rigidbody`
- Colliders: `UnityEngine.BoxCollider`, `UnityEngine.SphereCollider`, `UnityEngine.CapsuleCollider`
- Renderer: `UnityEngine.MeshRenderer`, `UnityEngine.SpriteRenderer`
- Camera: `UnityEngine.Camera`
- Light: `UnityEngine.Light`
- Audio: `UnityEngine.AudioSource`, `UnityEngine.AudioListener`

### UI Components (UGUI)
- Canvas: `UnityEngine.Canvas`, `UnityEngine.UI.CanvasScaler`, `UnityEngine.UI.GraphicRaycaster`
- Controls: `UnityEngine.UI.Button`, `UnityEngine.UI.Toggle`, `UnityEngine.UI.Slider`, `UnityEngine.UI.InputField`
- Display: `UnityEngine.UI.Text`, `UnityEngine.UI.Image`, `UnityEngine.UI.RawImage`
- Layout: `UnityEngine.UI.VerticalLayoutGroup`, `UnityEngine.UI.HorizontalLayoutGroup`, `UnityEngine.UI.GridLayoutGroup`

## Performance Tips

### Fast Operations
```python
# ⚡ Ultra-fast: Check existence only (0.1s)
unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Rigidbody",
    "includeProperties": False
})

# ⚡ Fast: Get specific properties (0.3s)
unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Transform",
    "propertyFilter": ["position"]
})
```

### Batch Operations with Safety
```python
# Test small first
test = unity_component_crud({
    "operation": "addMultiple",
    "pattern": "Enemy*",
    "componentType": "UnityEngine.Rigidbody",
    "maxResults": 10,  # Test with 10 first
    "stopOnError": False
})

# If successful, scale up
if test["errorCount"] == 0:
    unity_component_crud({...,"maxResults": 1000})
```

## Common Workflows

### Create a Main Menu
```python
# 1. Setup UI scene
unity_scene_quickSetup({"setupType": "UI"})

# 2. Create menu structure with hierarchy builder
unity_hierarchy_builder({
    "hierarchy": {
        "MenuPanel": {
            "components": ["UnityEngine.UI.Image"],
            "children": {
                "Title": {"components": ["UnityEngine.UI.Text"]},
                "ButtonList": {"components": ["UnityEngine.UI.VerticalLayoutGroup"]}
            }
        }
    },
    "parentPath": "Canvas"
})

# 3. Add buttons
for button_text in ["Start", "Settings", "Quit"]:
    unity_ugui_createFromTemplate({
        "template": "Button",
        "name": f"{button_text}Button",
        "parentPath": "Canvas/MenuPanel/ButtonList",
        "text": button_text
    })
```

### Create a Game Level
```python
# 1. Setup 3D scene
unity_scene_quickSetup({"setupType": "3D"})

# 2. Create player
unity_gameobject_createFromTemplate({
    "template": "Player",
    "position": {"x": 0, "y": 1, "z": 0}
})

# 3. Create ground
unity_gameobject_createFromTemplate({
    "template": "Plane",
    "name": "Ground",
    "scale": {"x": 10, "y": 1, "z": 10}
})

# 4. Batch create obstacles
unity_batch_execute({
    "operations": [
        {"tool": "gameObjectCreateFromTemplate", "payload": {"template": "Cube", "name": f"Obstacle{i}", "position": {"x": i*2, "y": 0.5, "z": 0}}}
        for i in range(5)
    ]
})
```

## Troubleshooting

### Unity Bridge Not Connected
**Solution**: Open Unity → Tools → MCP Assistant → Start Bridge

### GameObject Not Found
**Solution**: Use `unity_context_inspect()` to see what exists

### Component Type Not Found
**Solution**: Use fully qualified names (e.g., `UnityEngine.UI.Button`, not just `Button`)

### Operation Timeout
**Solution**:
- Use `includeProperties=false` for faster operations
- Set `maxResults` limit for batch operations
- Check Unity isn't compiling scripts

## Complete Tool Reference

**📚 For detailed documentation of all 28 tools, see [TOOLS_REFERENCE.md](docs/TOOLS_REFERENCE.md)**

The tools are organized into 9 categories:

| Category | Tools | Description |
|----------|-------|-------------|
| **Core Tools** | 5 | Connection, context, hierarchy builder, batch operations, logging |
| **Scene Management** | 2 | Scene CRUD, quick setup templates |
| **GameObject Operations** | 3 | GameObject CRUD, templates, tag/layer management |
| **Component Management** | 1 | Component CRUD with batch operations |
| **Asset Management** | 2 | Asset operations, C# script batch management |
| **UI (UGUI) Tools** | 6 | UI templates, layouts, RectTransform, overlap detection |
| **Prefab Management** | 1 | Prefab workflow (create, instantiate, apply/revert) |
| **Advanced Features** | 7 | Settings, pipeline, input system, tilemap, navmesh, constants |
| **Utility Tools** | 1 | Compilation waiting |

**Total: 28 Tools**

---

## Quick Tool Reference

### Scene Management
- `unity_scene_quickSetup` - Quick scene setup (3D/2D/UI/VR)
- `unity_scene_crud` - Create, load, save, delete scenes, manage build settings
- `unity_context_inspect` - Get scene hierarchy and state

### GameObject Operations
- `unity_gameobject_createFromTemplate` - Create from templates
- `unity_gameobject_crud` - Full GameObject CRUD operations
- `unity_hierarchy_builder` - Build nested structures

### Component Management
- `unity_component_crud` - Add, update, remove, inspect components

### UI Creation
- `unity_ugui_createFromTemplate` - Create UI elements from templates
- `unity_ugui_layoutManage` - Manage layout components

### Asset & Script Management
- `unity_asset_crud` - Asset file operations
- `unity_script_batch_manage` - Batch script operations with compilation

### Advanced Features
- `unity_prefab_crud` - Prefab workflow operations
- `unity_tilemap_crud` - 2D tilemap operations
- `unity_navmesh_manage` - NavMesh baking and configuration
- `unity_inputSystem_manage` - Input System management
- `unity_projectSettings_crud` - Project settings management
- `unity_renderPipeline_manage` - Render pipeline configuration
- `unity_batch_execute` - Execute multiple operations

### Utility
- `unity_ping` - Test connection and get Unity version

## Tips for Success

1. **Always check context before major operations** - Know what you're working with
2. **Use templates whenever possible** - They're optimized and reliable
3. **Batch similar operations** - More efficient than individual commands
4. **Set appropriate timeouts** - Some operations need more time (script compilation)
5. **Use property filters** - Get only the data you need
6. **Test with small limits first** - Before scaling up batch operations
7. **Follow Unity naming conventions** - Use full component type names

---

**You now have complete control over Unity Editor. Build amazing projects!** 🚀
