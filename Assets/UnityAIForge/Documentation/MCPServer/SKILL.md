---
name: unityaiforge
description: AI-powered Unity development toolkit with Model Context Protocol integration
license: MIT
---

# Unity-AI-Forge - AI-Powered Unity Development

**Forge Unity games through AI collaboration. Model Context Protocol integration with 3-pillar GameKit framework.**

You are now working with Unity-AI-Forge, a powerful system that lets you create, modify, and manage Unity projects directly from this conversation through intelligent AI collaboration.

## Prerequisites

Before using these tools, ensure:
1. Unity Editor is open
2. MCP Bridge is started (Tools > MCP Assistant > Start Bridge)
3. Connection status shows "Connected"

## 3-Layer Architecture

Unity-AI-Forge provides **52 tools** organized in 3 layers. Always prefer higher-level tools first:

```
High-Level GameKit (19 tools)  ← Game systems & analysis (use first)
Mid-Level Batch   (22 tools)  ← Batch operations & presets
Low-Level CRUD     (8 tools)  ← Individual object operations
Utility            (2 tools)  ← Helpers & diagnostics
Batch Operations   (1 tool)   ← Sequential execution
```

| Purpose | Recommended Layer | Example |
|---------|------------------|---------|
| Game systems (UI, effects, audio) | High-Level GameKit | `unity_gamekit_ui_command`, `unity_gamekit_effect` |
| Scene/code analysis | High-Level Analysis | `unity_scene_reference_graph`, `unity_class_catalog` |
| Batch operations & presets | Mid-Level Batch | `unity_transform_batch`, `unity_material_bundle` |
| Individual object control | Low-Level CRUD | `unity_gameobject_crud`, `unity_component_crud` |

---

## GameKit Framework (3-Pillar Architecture)

GameKit uses **code generation** to produce standalone C# scripts from templates. Generated scripts have zero runtime dependency on Unity-AI-Forge.

| Pillar | Tools | Purpose |
|--------|-------|---------|
| **UI** (5 tools) | Command, Binding, List, Slot, Selection | UI Toolkit-based components (UXML/USS + C#) |
| **Presentation** (5 tools) | AnimationSync, Effect, Feedback, VFX, Audio | Visual effects, animation, and audio components |
| **Logic** (7 tools) | Integrity, Catalog, Dependencies, References, Relationships, SceneDependency, ScriptSyntax | Scene/code analysis and validation |

After `create` operations, call `unity_compilation_await` to wait for Unity to compile the generated scripts.

**See [SKILL_GAMEKIT.md](SKILL_GAMEKIT.md) for complete GameKit documentation with examples.**

### Quick GameKit Examples

```python
# Create a mobile control panel (UI Pillar)
unity_gamekit_ui_command({
    "operation": "createCommandPanel",
    "panelId": "mobileControls",
    "layout": "grid",
    "commands": [
        {"name": "moveUp", "label": "Up", "commandType": "move",
         "moveDirection": {"x": 0, "y": 0, "z": 1}},
        {"name": "attack", "label": "Attack", "commandType": "action",
         "commandParameter": "melee"}
    ]
})
unity_compilation_await({"operation": "await"})

# Create hit feedback (Presentation Pillar)
unity_gamekit_feedback({
    "operation": "create",
    "feedbackId": "onHit",
    "components": [
        {"type": "hitstop", "duration": 0.05, "hitstopTimeScale": 0},
        {"type": "screenShake", "duration": 0.2, "intensity": 0.3}
    ]
})
unity_compilation_await({"operation": "await"})

# Validate scene integrity (Logic Pillar)
unity_validate_integrity({"operation": "all"})
```

---

## Quick Start

### Scene Setup

```python
# Create a new scene
unity_scene_crud({"operation": "create", "scenePath": "Assets/Scenes/GameLevel.unity"})

# Inspect current scene hierarchy
unity_scene_crud({
    "operation": "inspect",
    "includeHierarchy": true,
    "includeComponents": false
})
```

### GameObject Creation

```python
# Create a GameObject with template
unity_gameobject_crud({
    "operation": "create",
    "name": "Player",
    "template": "Sphere"
})

# Set position/scale via Transform component
unity_component_crud({
    "operation": "update",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Transform",
    "propertyChanges": {
        "position": {"x": 0, "y": 1, "z": 0},
        "localScale": {"x": 0.5, "y": 0.5, "z": 0.5}
    }
})

# Create with auto-attached components
unity_gameobject_crud({
    "operation": "create",
    "name": "Enemy",
    "template": "Cube",
    "components": [
        {"type": "UnityEngine.Rigidbody", "properties": {"mass": 2.0}},
        {"type": "UnityEngine.BoxCollider", "properties": {"isTrigger": true}}
    ]
})

# Batch find objects by pattern
unity_gameobject_crud({
    "operation": "findMultiple",
    "pattern": "Enemy*",
    "maxResults": 100
})
```

### UI Creation (UGUI)

```python
# Create Canvas with EventSystem
unity_ui_foundation({
    "operation": "createCanvas",
    "name": "GameUI",
    "renderMode": "screenSpaceOverlay"
})

# Create Button
unity_ui_foundation({
    "operation": "createButton",
    "name": "StartButton",
    "parentPath": "GameUI",
    "text": "Start Game",
    "anchorPreset": "middleCenter"
})

# Create complete UI hierarchy declaratively
unity_ui_hierarchy({
    "operation": "create",
    "parentPath": "GameUI",
    "hierarchy": {
        "type": "panel",
        "name": "MainMenu",
        "children": [
            {"type": "text", "name": "Title", "text": "Game Title", "fontSize": 48},
            {"type": "button", "name": "StartBtn", "text": "Start Game"},
            {"type": "button", "name": "OptionsBtn", "text": "Options"},
            {"type": "button", "name": "QuitBtn", "text": "Quit"}
        ],
        "layout": "Vertical",
        "spacing": 20
    }
})
```

### UI Toolkit (Modern UI)

```python
# Create UXML + USS assets from template
unity_uitk_asset({
    "operation": "createFromTemplate",
    "templateName": "menu",
    "outputDir": "Assets/UI/MainMenu",
    "title": "My Game",
    "buttons": ["New Game", "Load Game", "Settings", "Quit"]
})

# Create UIDocument in scene to display the UXML
unity_uitk_document({
    "operation": "create",
    "name": "MainMenuUI",
    "sourceAsset": "Assets/UI/MainMenu/MainMenu.uxml"
})
```

### Component Management

```python
# Add component with properties
unity_component_crud({
    "operation": "add",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Rigidbody",
    "propertyChanges": {
        "mass": 2.0,
        "useGravity": true
    }
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

# Fast existence check (10x faster)
unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Rigidbody",
    "includeProperties": false
})

# Batch operations with pattern matching
unity_component_crud({
    "operation": "addMultiple",
    "pattern": "Enemy*",
    "componentType": "UnityEngine.BoxCollider",
    "maxResults": 1000
})
```

### Asset & Script Management

```python
# Create a C# script
unity_asset_crud({
    "operation": "create",
    "assetPath": "Assets/Scripts/PlayerController.cs",
    "content": "using UnityEngine;\n\npublic class PlayerController : MonoBehaviour\n{\n    public float speed = 5f;\n    void Update() { /* movement */ }\n}"
})

# Wait for compilation after script creation
unity_compilation_await({"operation": "await"})

# Inspect asset properties
unity_asset_crud({
    "operation": "inspect",
    "assetPath": "Assets/Textures/player.png"
})

# Update importer settings
unity_asset_crud({
    "operation": "updateImporter",
    "assetPath": "Assets/Textures/player.png",
    "propertyChanges": {"textureType": "Sprite"}
})
```

### Prefab Workflow

```python
# Create prefab from scene object
unity_prefab_crud({
    "operation": "create",
    "gameObjectPath": "Player",
    "prefabPath": "Assets/Prefabs/Player.prefab"
})

# Instantiate prefab
unity_prefab_crud({
    "operation": "instantiate",
    "prefabPath": "Assets/Prefabs/Enemy.prefab",
    "parentPath": "Enemies",
    "position": {"x": 5, "y": 0, "z": 0}
})

# Apply instance overrides back to prefab
unity_prefab_crud({
    "operation": "applyOverrides",
    "gameObjectPath": "Player"
})
```

### ScriptableObject Management

```python
# Create instance from existing type
unity_scriptableObject_crud({
    "operation": "create",
    "typeName": "MyGame.GameConfig",
    "assetPath": "Assets/Data/GameConfig.asset",
    "properties": {"version": 1, "difficulty": "normal"}
})

# Find all instances of a type
unity_scriptableObject_crud({
    "operation": "findByType",
    "typeName": "MyGame.GameConfig",
    "searchPath": "Assets/Data"
})
```

### Project Settings

```python
# Read a setting
unity_projectSettings_crud({
    "operation": "read",
    "category": "physics2d",
    "property": "gravity"
})

# Add tag
unity_projectSettings_crud({
    "operation": "write",
    "category": "tagsLayers",
    "property": "addTag",
    "value": "Enemy"
})

# Add scene to build settings
unity_projectSettings_crud({
    "operation": "addSceneToBuild",
    "scenePath": "Assets/Scenes/Level1.unity"
})
```

---

## Mid-Level Batch Tools

### Transform Batch

```python
# Arrange objects in a circle
unity_transform_batch({
    "operation": "arrangeCircle",
    "gameObjectPaths": ["Obj1", "Obj2", "Obj3"],
    "radius": 5.0
})

# Arrange in a line
unity_transform_batch({
    "operation": "arrangeLine",
    "gameObjectPaths": ["Obj1", "Obj2", "Obj3"],
    "startPosition": {"x": 0, "y": 0, "z": 0},
    "endPosition": {"x": 10, "y": 0, "z": 0}
})

# Rename sequentially
unity_transform_batch({
    "operation": "renameSequential",
    "gameObjectPaths": ["Obj1", "Obj2", "Obj3"],
    "baseName": "Enemy",
    "startIndex": 1,
    "padding": 3
})
```

### Camera Rig

```python
# Create follow camera
unity_camera_rig({
    "operation": "createRig",
    "rigType": "follow",
    "rigName": "MainCamera",
    "targetPath": "Player",
    "offset": {"x": 0, "y": 5, "z": -10}
})
# rigType: follow, orbit, splitScreen, fixed, dolly
```

### Material, Light & Particle Bundles

```python
# Create material with preset
unity_material_bundle({"operation": "create", "savePath": "Assets/Materials/Player.mat", "preset": "metallic"})

# Create lighting setup
unity_light_bundle({"operation": "createLightingSetup", "setupPreset": "daylight"})

# Create particle system with preset
unity_particle_bundle({"operation": "create", "gameObjectPath": "Effects/Fire", "preset": "fire"})
```

### 2D Tools

```python
# Sprite management
unity_sprite2d_bundle({"operation": "createSprite", "name": "Player", "spritePath": "Assets/Sprites/player.png"})

# Tilemap creation
unity_tilemap_bundle({"operation": "createTilemap", "name": "Ground", "cellLayout": "Rectangle"})

# 2D animation setup
unity_animation2d_bundle({"operation": "createController", "controllerPath": "Assets/Animations/Player.controller"})
```

### 3D Animation

```python
# Create AnimatorController with BlendTree
unity_animation3d_bundle({
    "operation": "createController",
    "controllerPath": "Assets/Animations/Character.controller"
})
unity_animation3d_bundle({
    "operation": "addBlendTree",
    "controllerPath": "Assets/Animations/Character.controller",
    "blendTreeName": "Locomotion",
    "blendParameter": "Speed"
})
```

### Event Wiring

```python
# Wire Button.onClick to a method
unity_event_wiring({
    "operation": "wire",
    "source": {
        "gameObject": "Canvas/StartButton",
        "component": "UnityEngine.UI.Button",
        "event": "onClick"
    },
    "target": {
        "gameObject": "GameManager",
        "method": "StartGame"
    }
})
```

### Input Profile

```python
# Setup player input with preset
unity_input_profile({
    "operation": "createPlayerInput",
    "gameObjectPath": "Player",
    "preset": "player"
})
# Presets: player (move/jump/fire), ui (navigate/submit/cancel), vehicle (accelerate/brake/steer)
```

### UI State & Navigation

```python
# Define UI states
unity_ui_state({
    "operation": "defineState",
    "rootPath": "Canvas/HUD",
    "stateName": "combat",
    "elements": [
        {"path": "HPBar", "visible": true},
        {"path": "ManaBar", "visible": true},
        {"path": "MinimapPanel", "visible": false}
    ]
})

# Configure navigation for gamepad support
unity_ui_navigation({
    "operation": "autoSetup",
    "rootPath": "Canvas/MainMenu",
    "direction": "vertical"
})
```

### RectTransform Batch

```python
# Distribute buttons evenly
unity_rectTransform_batch({
    "operation": "distributeHorizontal",
    "gameObjectPaths": ["Btn1", "Btn2", "Btn3"],
    "spacing": 10
})

# Align to parent with anchor preset
unity_rectTransform_batch({
    "operation": "alignToParent",
    "gameObjectPaths": ["Panel"],
    "preset": "stretchAll"
})
```

### Vector Sprite Convert

```python
# Generate sprite from primitive
unity_vector_sprite_convert({
    "operation": "primitiveToSprite",
    "primitiveType": "circle",
    "width": 128,
    "height": 128,
    "color": {"r": 1, "g": 0, "b": 0, "a": 1},
    "outputPath": "Assets/Sprites/circle.png"
})
```

---

## Utility & Batch Tools

```python
# Check bridge connection
unity_ping()

# Wait for script compilation
unity_compilation_await({"operation": "await", "timeoutSeconds": 60})

# Sequential batch execution with resume
unity_batch_sequential_execute({
    "operations": [
        {"tool": "unity_gameobject_crud", "arguments": {"operation": "create", "name": "Enemy1"}},
        {"tool": "unity_component_crud", "arguments": {"operation": "add", "gameObjectPath": "Enemy1", "componentType": "UnityEngine.Rigidbody"}}
    ],
    "stop_on_error": true
})
```

---

## PDCA Development Workflow

All development follows the **Plan - Do - Check - Act** cycle:

### P (Plan) - Investigate before changes

```python
# Inspect scene hierarchy
unity_scene_crud({"operation": "inspect", "includeHierarchy": true})

# Check what references an object (before deleting/renaming)
unity_scene_reference_graph({"operation": "analyzeObject", "objectPath": "TargetObject"})

# Discover available types
unity_class_catalog({"operation": "listTypes", "typeKind": "MonoBehaviour"})
```

### D (Do) - Execute with appropriate tools

```python
# High-Level: Game systems
unity_gamekit_ui_command({"operation": "createCommandPanel", ...})

# Mid-Level: Batch operations
unity_transform_batch({"operation": "arrangeCircle", "gameObjectPaths": ["Obj1", "Obj2"], "radius": 5.0})

# Low-Level: Individual control
unity_component_crud({"operation": "add", "gameObjectPath": "Player", "componentType": "..."})

# Wait for compilation after script changes
unity_compilation_await({"operation": "await"})
```

### C (Check) - Verify after changes

```python
# Scene integrity validation
unity_validate_integrity({"operation": "all"})

# Check references and events
unity_scene_relationship_graph({"operation": "analyzeAll"})

# Check console for errors
unity_console_log({"operation": "getErrors"})
```

### A (Act) - Fix issues and test

```python
# Fix broken event wiring
unity_event_wiring({"operation": "wire", ...})

# Test in play mode
unity_playmode_control({"operation": "play"})
unity_console_log({"operation": "getErrors"})
unity_playmode_control({"operation": "stop"})
```

---

## Best Practices

### DO

1. **Inspect before modifying** - Understand current state first
   ```python
   unity_scene_crud({"operation": "inspect", "includeHierarchy": true, "includeComponents": false})
   ```

2. **Use higher-level tools first** - GameKit > Batch > CRUD
   ```python
   # Use ui_hierarchy instead of manual GameObject + component creation
   unity_ui_hierarchy({"operation": "create", "parentPath": "Canvas", "hierarchy": {...}})
   ```

3. **Use `includeProperties=false` for fast inspection**
   ```python
   unity_component_crud({"operation": "inspect", "gameObjectPath": "Player",
       "componentType": "UnityEngine.Rigidbody", "includeProperties": false})
   ```

4. **Set `maxResults` for batch operations** - Prevent timeouts
   ```python
   unity_component_crud({"operation": "addMultiple", "pattern": "Enemy*",
       "componentType": "UnityEngine.Rigidbody", "maxResults": 1000})
   ```

5. **Wait for compilation after script changes**
   ```python
   unity_asset_crud({"operation": "create", "assetPath": "Assets/Scripts/Foo.cs", "content": "..."})
   unity_compilation_await({"operation": "await"})
   ```

6. **Validate integrity after significant changes**
   ```python
   unity_validate_integrity({"operation": "all"})
   ```

### DON'T

1. **Don't edit .meta files** - Unity manages these automatically
2. **Don't skip inspection** - Know what exists before modifying
3. **Don't use unlimited batch operations** - Always set `maxResults`
4. **Don't forget compilation waits** - After C# script creation/modification
5. **Don't use low-level tools for complex setups** - Use Mid/High-Level tools instead

---

## Performance Tips

```python
# Ultra-fast existence check (0.1s)
unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Rigidbody",
    "includeProperties": false
})

# Fast: Get specific properties only (0.3s)
unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Transform",
    "propertyFilter": ["position", "rotation"]
})

# Use Mid-Level Batch instead of loops
unity_transform_batch({"operation": "arrangeCircle", "gameObjectPaths": [...]})

# Use *Multiple operations for batch processing
unity_component_crud({"operation": "addMultiple", "pattern": "Enemy*", "componentType": "..."})

# Use batch_sequential_execute for multi-step operations
unity_batch_sequential_execute({"operations": [...]})
```

---

## Component Type Reference

### Common Unity Components
- Transform: `UnityEngine.Transform`
- Rigidbody: `UnityEngine.Rigidbody` / `UnityEngine.Rigidbody2D`
- Colliders: `UnityEngine.BoxCollider`, `UnityEngine.SphereCollider`, `UnityEngine.CapsuleCollider`
- 2D Colliders: `UnityEngine.BoxCollider2D`, `UnityEngine.CircleCollider2D`, `UnityEngine.CapsuleCollider2D`
- Renderer: `UnityEngine.MeshRenderer`, `UnityEngine.SpriteRenderer`
- Camera: `UnityEngine.Camera`
- Light: `UnityEngine.Light`
- Audio: `UnityEngine.AudioSource`, `UnityEngine.AudioListener`
- Animation: `UnityEngine.Animator`
- CharacterController: `UnityEngine.CharacterController`

### UI Components (UGUI)
- Canvas: `UnityEngine.Canvas`, `UnityEngine.UI.CanvasScaler`, `UnityEngine.UI.GraphicRaycaster`
- Controls: `UnityEngine.UI.Button`, `UnityEngine.UI.Toggle`, `UnityEngine.UI.Slider`, `UnityEngine.UI.InputField`
- Display: `UnityEngine.UI.Image`, `UnityEngine.UI.RawImage`, `TMPro.TextMeshProUGUI`
- Layout: `UnityEngine.UI.VerticalLayoutGroup`, `UnityEngine.UI.HorizontalLayoutGroup`, `UnityEngine.UI.GridLayoutGroup`

### Unity Object References (in propertyChanges)
```python
{"$ref": "Assets/Materials/Player.mat"}     # Asset reference
{"$ref": "Canvas/Panel/Button"}             # Scene object reference
```

---

## Troubleshooting

### Unity Bridge Not Connected
**Solution**: Open Unity > Tools > MCP Assistant > Start Bridge. Verify with `unity_ping()`.

### GameObject Not Found
**Solution**: Use `unity_scene_crud({"operation": "inspect", "includeHierarchy": true})` to see what exists.

### Component Type Not Found
**Solution**: Use fully qualified names (e.g., `UnityEngine.UI.Button`, not `Button`). Use `unity_class_catalog` to discover available types.

### Operation Timeout
**Solution**:
- Use `includeProperties=false` for faster inspections
- Set `maxResults` for batch operations
- Check compilation status with `unity_compilation_await`
- Use Mid-Level Batch tools instead of loops

---

## Complete Tool Reference (52 Tools)

### High-Level GameKit - UI Pillar (5 tools)

| Tool | Description |
|------|-------------|
| `unity_gamekit_ui_command` | Button command panels (UXML/USS + C#) |
| `unity_gamekit_ui_binding` | Declarative UI data binding |
| `unity_gamekit_ui_list` | Dynamic ScrollView list/grid |
| `unity_gamekit_ui_slot` | Equipment/quickslot UI |
| `unity_gamekit_ui_selection` | Radio/toggle/checkbox/tab groups |

### High-Level GameKit - Presentation Pillar (5 tools)

| Tool | Description |
|------|-------------|
| `unity_gamekit_animation_sync` | Animator parameter sync with game state |
| `unity_gamekit_effect` | Composite effects (particle + sound + shake) |
| `unity_gamekit_feedback` | Game feel (hitstop, screen shake, flash) |
| `unity_gamekit_vfx` | ParticleSystem wrapper with pooling |
| `unity_gamekit_audio` | Sound management (SFX, music, ambient) |

### High-Level GameKit - Logic Pillar (7 tools)

| Tool | Description |
|------|-------------|
| `unity_validate_integrity` | Scene integrity validation (missing scripts, null refs) |
| `unity_class_catalog` | Type enumeration and inspection |
| `unity_class_dependency_graph` | C# class dependency analysis |
| `unity_scene_reference_graph` | Scene object reference analysis |
| `unity_scene_relationship_graph` | Scene transition and relationship analysis |
| `unity_scene_dependency` | Scene asset dependency analysis (AssetDatabase) |
| `unity_script_syntax` | C# source code structure analysis with line numbers |

### High-Level GameKit - Systems (2 tools)

| Tool | Description |
|------|-------------|
| `unity_gamekit_pool` | Object pooling (UnityEngine.Pool) with code generation |
| `unity_gamekit_data` | Event channels, data containers, runtime sets (ScriptableObject) |

### Mid-Level Batch (22 tools)

| Tool | Description |
|------|-------------|
| `unity_transform_batch` | Batch arrange, rename patterns |
| `unity_rectTransform_batch` | UI anchors, alignment, distribution |
| `unity_camera_rig` | Camera rig presets (follow, orbit, etc.) |
| `unity_ui_foundation` | UGUI element creation (Canvas, Button, Text, etc.) |
| `unity_ui_hierarchy` | Declarative UI hierarchy from JSON |
| `unity_ui_state` | UI state management |
| `unity_ui_navigation` | Keyboard/gamepad navigation setup |
| `unity_input_profile` | New Input System setup |
| `unity_tilemap_bundle` | Tilemap creation and tile management |
| `unity_sprite2d_bundle` | 2D sprite management |
| `unity_animation2d_bundle` | 2D animation setup |
| `unity_animation3d_bundle` | 3D animation (BlendTree, AvatarMask) |
| `unity_material_bundle` | Material creation and presets |
| `unity_light_bundle` | Light setup and presets |
| `unity_particle_bundle` | Particle system presets |
| `unity_event_wiring` | UnityEvent wiring (onClick, onValueChanged, etc.) |
| `unity_playmode_control` | Play mode control (play/pause/stop/step) |
| `unity_console_log` | Console log retrieval and filtering |
| `unity_uitk_document` | UI Toolkit UIDocument management |
| `unity_uitk_asset` | UI Toolkit asset creation (UXML, USS) |
| `unity_physics_bundle` | Physics presets, collision matrix, physics materials |
| `unity_navmesh_bundle` | NavMesh baking, agents, obstacles, links, modifiers |

### Low-Level CRUD (8 tools)

| Tool | Description |
|------|-------------|
| `unity_scene_crud` | Scene create/load/save/delete/inspect |
| `unity_gameobject_crud` | GameObject lifecycle (create/delete/move/rename/batch) |
| `unity_component_crud` | Component add/remove/update/inspect/batch |
| `unity_asset_crud` | Asset file management (create/update/delete/inspect) |
| `unity_scriptableObject_crud` | ScriptableObject management |
| `unity_prefab_crud` | Prefab workflow (create/instantiate/apply/revert) |
| `unity_vector_sprite_convert` | Vector/primitive to sprite conversion |
| `unity_projectSettings_crud` | Project settings (player, quality, physics, tags/layers, build) |

### Utility (2 tools) + Batch (1 tool)

| Tool | Description |
|------|-------------|
| `unity_ping` | Bridge connectivity check |
| `unity_compilation_await` | Wait for script compilation |
| `unity_batch_sequential_execute` | Sequential batch execution with resume |

---

## Additional Documentation

### GameKit Framework
**[SKILL_GAMEKIT.md](SKILL_GAMEKIT.md)** - Complete guide to GameKit's 3-pillar architecture:
- **UI Pillar**: UICommand, UIBinding, UIList, UISlot, UISelection
- **Presentation Pillar**: AnimationSync, Effect, Feedback, VFX, Audio
- **Logic Pillar**: Integrity validation, class catalog, dependency/reference/relationship graphs
- Code generation workflow
- Complete game examples and best practices

---

**You now have complete control over Unity Editor with 52 tools across 3 layers. Build amazing projects!**
