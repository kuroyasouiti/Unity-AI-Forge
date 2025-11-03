# UnityMCP Quick Start for Claude

This is a quick reference guide for Claude to work efficiently with Unity through MCP.

## 🚀 First Steps

### 1. Check Connection
```python
unity_ping()
```

### 2. Inspect Current Scene
```python
unity_context_inspect({"includeHierarchy": True, "maxDepth": 2})
```

## 📋 Common Commands Cheat Sheet

### Scene Setup

```python
# Set up new 3D scene (Camera + Light)
unity_scene_quickSetup({"setupType": "3D"})

# Set up new 2D scene
unity_scene_quickSetup({"setupType": "2D"})

# Set up new UI scene (Canvas + EventSystem)
unity_scene_quickSetup({"setupType": "UI"})
```

### Create GameObjects

```python
# Create primitive shapes
unity_gameobject_createFromTemplate({"template": "Cube"})
unity_gameobject_createFromTemplate({"template": "Sphere"})
unity_gameobject_createFromTemplate({"template": "Plane"})

# Create with position
unity_gameobject_createFromTemplate({
    "template": "Cube",
    "position": {"x": 0, "y": 1, "z": 0}
})

# Create lights
unity_gameobject_createFromTemplate({"template": "Light-Directional"})
unity_gameobject_createFromTemplate({"template": "Light-Point"})

# Create player/enemy
unity_gameobject_createFromTemplate({"template": "Player"})
unity_gameobject_createFromTemplate({"template": "Enemy"})
```

### Create UI Elements

```python
# Create button
unity_ugui_createFromTemplate({
    "template": "Button",
    "text": "Click Me!",
    "width": 200,
    "height": 50
})

# Create text
unity_ugui_createFromTemplate({
    "template": "Text",
    "text": "Score: 0",
    "fontSize": 24
})

# Create input field
unity_ugui_createFromTemplate({
    "template": "InputField",
    "width": 300,
    "height": 40
})

# Create panel
unity_ugui_createFromTemplate({
    "template": "Panel",
    "width": 400,
    "height": 300
})

# Create slider
unity_ugui_createFromTemplate({"template": "Slider", "width": 200})

# Create toggle
unity_ugui_createFromTemplate({"template": "Toggle", "text": "Enable Sound"})
```

### Add Layout Components

```python
# Add vertical layout
unity_ugui_layoutManage({
    "operation": "add",
    "gameObjectPath": "Canvas/Panel",
    "layoutType": "VerticalLayoutGroup",
    "spacing": 10,
    "padding": {"left": 10, "right": 10, "top": 10, "bottom": 10}
})

# Add horizontal layout
unity_ugui_layoutManage({
    "operation": "add",
    "gameObjectPath": "Canvas/Panel",
    "layoutType": "HorizontalLayoutGroup",
    "spacing": 15
})

# Add grid layout
unity_ugui_layoutManage({
    "operation": "add",
    "gameObjectPath": "Canvas/Panel",
    "layoutType": "GridLayoutGroup",
    "cellSizeX": 100,
    "cellSizeY": 100,
    "spacing": 10
})
```

### Build Hierarchies

```python
# Simple hierarchy
unity_hierarchy_builder({
    "hierarchy": {
        "Player": {
            "components": ["Rigidbody", "CapsuleCollider"],
            "children": {
                "Camera": {"components": ["Camera"]},
                "Weapon": {"components": ["BoxCollider"]}
            }
        }
    }
})

# UI hierarchy
unity_hierarchy_builder({
    "hierarchy": {
        "MainMenu": {
            "components": ["UnityEngine.UI.Image"],
            "children": {
                "Title": {"components": ["UnityEngine.UI.Text"]},
                "Buttons": {
                    "components": ["UnityEngine.UI.VerticalLayoutGroup"],
                    "children": {
                        "PlayButton": {"components": ["UnityEngine.UI.Button"]},
                        "QuitButton": {"components": ["UnityEngine.UI.Button"]}
                    }
                }
            }
        }
    },
    "parentPath": "Canvas"
})
```

### Manage Components

```python
# Add component
unity_component_crud({
    "operation": "add",
    "gameObjectPath": "Player",
    "componentType": "Rigidbody"
})

# Update component properties
unity_component_crud({
    "operation": "update",
    "gameObjectPath": "Player",
    "componentType": "Rigidbody",
    "propertyChanges": {
        "mass": 2.0,
        "useGravity": True
    }
})

# Remove component
unity_component_crud({
    "operation": "remove",
    "gameObjectPath": "Player",
    "componentType": "BoxCollider"
})
```

### Script Batch Management

```python
# Always use batch format for script operations (even single scripts)
unity_script_batch_manage({
    "scripts": [
        {"operation": "create", "scriptPath": "Assets/Scripts/Player.cs", "scriptType": "monoBehaviour"},
        {"operation": "create", "scriptPath": "Assets/Scripts/Enemy.cs", "scriptType": "monoBehaviour"},
        {"operation": "create", "scriptPath": "Assets/Scripts/GameManager.cs", "scriptType": "class"}
    ],
    "stopOnError": False,
    "timeoutSeconds": 30
})
```

## 💡 Best Practices

### ✅ DO

1. **Always use templates for common objects**
   ```python
   unity_ugui_createFromTemplate({"template": "Button"})
   ```

2. **Use hierarchy builder for complex structures**
   ```python
   unity_hierarchy_builder({"hierarchy": {...}})
   ```

3. **Check context before making changes**
   ```python
   unity_context_inspect({"includeHierarchy": True})
   ```

4. **Always use batch format for script operations**
   ```python
   unity_script_batch_manage({"scripts": [...]})
   ```

### ❌ DON'T

1. **Don't create UI manually when templates exist**
   ```python
   # Bad
   unity_gameobject_crud({"operation": "create", "name": "Button"})
   unity_component_crud({"operation": "add", ...})
   # ... many more steps

   # Good
   unity_ugui_createFromTemplate({"template": "Button"})
   ```

2. **Don't forget to set up the scene first**
   ```python
   # Always set up scene type first
   unity_scene_quickSetup({"setupType": "UI"})
   ```

## 🎯 Common Workflows

### Workflow 1: Create a Simple Menu

```python
# 1. Setup UI scene
unity_scene_quickSetup({"setupType": "UI"})

# 2. Create menu panel
unity_ugui_createFromTemplate({
    "template": "Panel",
    "name": "MenuPanel",
    "width": 400,
    "height": 600
})

# 3. Add layout to panel
unity_ugui_layoutManage({
    "operation": "add",
    "gameObjectPath": "Canvas/MenuPanel",
    "layoutType": "VerticalLayoutGroup",
    "spacing": 20,
    "padding": {"left": 30, "right": 30, "top": 30, "bottom": 30}
})

# 4. Add buttons
for btn_text in ["Start", "Options", "Quit"]:
    unity_ugui_createFromTemplate({
        "template": "Button",
        "name": f"{btn_text}Button",
        "parentPath": "Canvas/MenuPanel",
        "text": btn_text,
        "height": 60
    })
```

### Workflow 2: Create a 3D Game Scene

```python
# 1. Setup 3D scene
unity_scene_quickSetup({"setupType": "3D"})

# 2. Create ground
unity_gameobject_createFromTemplate({
    "template": "Plane",
    "name": "Ground",
    "scale": {"x": 10, "y": 1, "z": 10}
})

# 3. Create player
unity_gameobject_createFromTemplate({
    "template": "Player",
    "position": {"x": 0, "y": 1, "z": 0}
})

# 4. Create obstacles
for i in range(5):
    unity_gameobject_createFromTemplate({
        "template": "Cube",
        "name": f"Obstacle{i}",
        "position": {"x": i*2-4, "y": 0.5, "z": 0}
    })
```

### Workflow 3: Create an Inventory System

```python
# 1. Create inventory panel
unity_ugui_createFromTemplate({
    "template": "Panel",
    "name": "InventoryPanel",
    "parentPath": "Canvas"
})

# 2. Add grid layout
unity_ugui_layoutManage({
    "operation": "add",
    "gameObjectPath": "Canvas/InventoryPanel",
    "layoutType": "GridLayoutGroup",
    "cellSizeX": 80,
    "cellSizeY": 80,
    "spacing": 5,
    "constraint": "FixedColumnCount",
    "constraintCount": 6
})

# 3. Create inventory slots
for i in range(24):
    unity_ugui_createFromTemplate({
        "template": "Image",
        "name": f"Slot{i}",
        "parentPath": "Canvas/InventoryPanel"
    })
```

## 🔍 Debugging Tips

### Check if GameObject exists
```python
unity_context_inspect({"filter": "MyObject"})
```

### Inspect specific GameObject
```python
unity_gameobject_crud({
    "operation": "inspect",
    "gameObjectPath": "Player"
})
```

### List all GameObjects in scene
```python
unity_context_inspect({
    "includeHierarchy": True,
    "includeComponents": True
})
```

## 📚 Tool Name Reference

| Task | Tool Name |
|------|-----------|
| Scene setup | `unity_scene_quickSetup` |
| Create GameObject | `unity_gameobject_createFromTemplate` |
| Create UI element | `unity_ugui_createFromTemplate` |
| Build hierarchy | `unity_hierarchy_builder` |
| Manage layout | `unity_ugui_layoutManage` |
| Inspect scene | `unity_context_inspect` |
| Script batch management | `unity_script_batch_manage` |
| Manage components | `unity_component_crud` |
| Manage GameObjects | `unity_gameobject_crud` |
| Manage assets | `unity_asset_crud` |

## 🎨 Anchor Presets for UI

Use these with `anchorPreset` parameter:

- `"top-left"`, `"top-center"`, `"top-right"`
- `"middle-left"`, `"middle-center"`, `"middle-right"`, `"center"`
- `"bottom-left"`, `"bottom-center"`, `"bottom-right"`
- `"stretch-horizontal"`, `"stretch-vertical"`, `"stretch-all"`, `"stretch"`

**Example:**
```python
unity_ugui_createFromTemplate({
    "template": "Button",
    "anchorPreset": "bottom-center"
})
```
