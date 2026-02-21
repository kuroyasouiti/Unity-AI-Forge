# MCP Tool Selection Guide

This guide helps you choose the right tool for your Unity development tasks.

---

## Quick Selection Table

| Use Case | Recommended Tool |
|----------|-----------------|
| Game action buttons (move/jump/attack) | `unity_gamekit_ui_command` |
| Auto-sync UI with game data | `unity_gamekit_ui_binding` |
| Scrollable lists / inventory grids | `unity_gamekit_ui_list` |
| Equipment / quickbar slots | `unity_gamekit_ui_slot` |
| Radio / toggle / tab groups | `unity_gamekit_ui_selection` |
| Composite effects (particle + sound + shake) | `unity_gamekit_effect` |
| Game feel (hitstop, screen shake) | `unity_gamekit_feedback` |
| Animation parameter sync | `unity_gamekit_animation_sync` |
| VFX with object pooling | `unity_gamekit_vfx` |
| Sound management (SFX, BGM) | `unity_gamekit_audio` |
| Build UI menu from JSON | `unity_ui_hierarchy` |
| Configure keyboard/gamepad navigation | `unity_ui_navigation` |
| Manage UI visibility states | `unity_ui_state` |
| Create single UI element | `unity_ui_foundation` |
| Scene integrity validation | `unity_validate_integrity` |
| Type discovery / inspection | `unity_class_catalog` |

---

## 1. GameKit Effect & Feedback Tools

### When to use `unity_gamekit_effect`

**Best for:** Composite effects (particle + sound + camera shake)

- Explosion effects (VFX + sound + camera shake)
- Pickup effects (particles + sound)
- Impact effects with multiple components
- Reusable effect presets with EffectManager

**Example use cases:**
- Explosion → particles + boom sound + camera shake + time slow
- Coin pickup → sparkle particles + bling sound
- Level up → flash + sound + screen effect

```python
unity_gamekit_effect({
    "operation": "create",
    "effectId": "explosion",
    "components": [
        {"type": "particle", "prefabPath": "Assets/Prefabs/Explosion.prefab"},
        {"type": "sound", "clipPath": "Assets/Audio/boom.wav"},
        {"type": "cameraShake", "intensity": 0.5, "shakeDuration": 0.3}
    ]
})
```

### When to use `unity_gamekit_feedback`

**Best for:** Game feel (hitstop, screen shake, flash)

- Hit reactions (hitstop + flash + knockback)
- Screen effects (shake, flash, vignette)
- Juice effects for game feel
- Intensity-adjustable feedback chains

**Example use cases:**
- Melee hit → hitstop + screen shake + color flash
- Critical hit → slow motion + chromatic aberration
- Damage taken → red flash + knockback

```python
unity_gamekit_feedback({
    "operation": "create",
    "feedbackId": "onHit",
    "components": [
        {"type": "hitstop", "duration": 0.05, "hitstopTimeScale": 0},
        {"type": "screenShake", "duration": 0.2, "intensity": 0.3},
        {"type": "colorFlash", "color": {"r": 1, "g": 0, "b": 0, "a": 0.5}}
    ]
})
```

### Decision Matrix

| Requirement | Use `effect` | Use `feedback` |
|-------------|-------------|---------------|
| Particle + Sound combo | Yes | No |
| Hitstop / slow motion | No | Yes |
| Camera shake | Yes | Yes |
| Screen flash | Yes | Yes |
| Knockback | No | Yes |
| Reusable with Manager | Yes | No |
| Intensity control | No | Yes |

---

## 2. GameKit UI Tools (Code Generation)

All GameKit UI tools generate standalone C# scripts via code generation. After `create` operations, call `unity_compilation_await({"operation": "await"})`.

### When to use `unity_gamekit_ui_command`

**Best for:** Button-based game actions

- Mobile game controls (move/jump/attack buttons)
- Strategy game actions (resource/state management)
- Turn-based game controls
- Shop/menu action panels

```python
unity_gamekit_ui_command({
    "operation": "createCommandPanel",
    "panelId": "controls",
    "layout": "horizontal",
    "commands": [
        {"name": "attack", "label": "Attack", "commandType": "action"}
    ]
})
```

### When to use `unity_gamekit_ui_binding`

**Best for:** Auto-sync UI with game data

- HP bars, mana bars (percent format)
- Score displays (formatted)
- Timer displays
- Resource counters with smooth transitions

```python
unity_gamekit_ui_binding({
    "operation": "create",
    "bindingId": "hpBar",
    "sourceType": "health",
    "sourceId": "playerHP",
    "format": "percent",
    "smoothTransition": true
})
```

### When to use `unity_gamekit_ui_list` / `ui_slot` / `ui_selection`

| Tool | Best For | Example |
|------|----------|---------|
| `ui_list` | Scrollable item lists, inventory grids | Shop item list, leaderboard |
| `ui_slot` | Equipment slots, quickbar slots | Weapon slots, skill bar |
| `ui_selection` | Radio buttons, tabs, toggle groups | Difficulty selector, tab menu |

### Decision Matrix

| Requirement | command | binding | list | slot | selection |
|-------------|---------|---------|------|------|-----------|
| Action buttons | Yes | No | No | No | No |
| Data display | No | Yes | No | No | No |
| Scrollable list | No | No | Yes | No | No |
| Equipment slots | No | No | No | Yes | No |
| Tab/radio groups | No | No | No | No | Yes |

---

## 3. Physics Setup Tools

### When to use `unity_physics_bundle`

**Best for:** Rigidbody-based physics

- Objects with realistic physics (gravity, collision response)
- Ragdoll-ready characters
- Vehicles with physics simulation
- Platformer characters (2D with Rigidbody2D)
- Top-down 2D games
- Projectiles with physics

**Presets available:**
- `dynamic` - Movable with physics
- `kinematic` - Movable without physics
- `static` - Immovable
- `character` - Physics-based character
- `platformer` - 2D platformer character
- `topDown` - 2D top-down game
- `vehicle` - Car physics
- `projectile` - Bullets/arrows

```python
unity_physics_bundle({
    "operation": "applyPreset2D",
    "gameObjectPaths": ["Player"],
    "preset": "platformer"
})
```

### When to use `unity_character_controller_bundle`

**Best for:** CharacterController-based movement

- First-person shooter characters
- Third-person action game characters
- Precise movement control (no physics sliding)
- Stair/slope climbing
- Custom gravity implementation

**Presets available:**
- `fps` - First-person (1.8m height)
- `tps` - Third-person (2.0m height)
- `platformer` - 3D platformer (1.0m height)
- `child` - Small character (0.5m height)
- `large` - Large character (3.0m height)
- `narrow` - Thin capsule for tight spaces

```python
unity_character_controller_bundle({
    "operation": "applyPreset",
    "gameObjectPath": "Player",
    "preset": "fps"
})
```

### Decision Matrix

| Requirement | Use `physics_bundle` | Use `character_controller_bundle` |
|-------------|---------------------|----------------------------------|
| Gravity/collision response | Yes | No (manual) |
| Ragdoll support | Yes | No |
| Precise stair climbing | No | Yes |
| 2D games | Yes | No |
| Physics interactions | Yes | Limited |
| FPS/TPS movement | Possible | Recommended |

---

## 4. UGUI Tools (Mid-Level)

### When to use `unity_ui_hierarchy`

**Best for:** Declarative UI creation from JSON

- Build entire menu systems from JSON definition
- Complex nested UI structures
- Rapid prototyping
- Show/hide UI panels

```python
unity_ui_hierarchy({
    "operation": "create",
    "parentPath": "Canvas",
    "hierarchy": {
        "type": "panel",
        "name": "MainMenu",
        "children": [
            {"type": "button", "name": "StartBtn", "text": "Start"},
            {"type": "button", "name": "QuitBtn", "text": "Quit"}
        ],
        "layout": "Vertical"
    }
})
```

### When to use `unity_ui_navigation`

**Best for:** Keyboard/gamepad navigation setup

- Auto-setup vertical/horizontal navigation
- Grid navigation for inventories
- Explicit navigation (custom up/down/left/right)
- Navigation groups with isolation
- First selected element setting

```python
unity_ui_navigation({
    "operation": "autoSetup",
    "rootPath": "Canvas/MainMenu",
    "direction": "vertical",
    "wrapAround": true
})
```

### When to use `unity_ui_state`

**Best for:** UI state management

- Define show/hidden states
- Manage dialog open/close
- State groups (mutually exclusive menus)
- Save/restore UI state

```python
unity_ui_state({
    "operation": "defineState",
    "stateName": "hidden",
    "rootPath": "Canvas/Dialog",
    "elements": [{"path": "", "active": false, "alpha": 0}]
})
```

### When to use `unity_ui_foundation`

**Best for:** Single UI element creation

- Create individual buttons, panels, text
- Add LayoutGroup to existing panels
- Simple one-off UI elements

```python
unity_ui_foundation({
    "operation": "createButton",
    "name": "MyButton",
    "parentPath": "Canvas",
    "text": "Click Me",
    "width": 200,
    "height": 50
})
```

### Decision Matrix

| Requirement | hierarchy | navigation | state | foundation |
|-------------|-----------|------------|-------|------------|
| Build UI from JSON | Yes | No | No | No |
| Keyboard navigation | No | Yes | No | No |
| Show/hide panels | Yes | No | Yes | No |
| State management | No | No | Yes | No |
| Single element | No | No | No | Yes |
| LayoutGroup | No | No | No | Yes |

---

## 5. Scene Analysis & Validation (Logic Pillar)

### When to use `unity_validate_integrity`

**Best for:** Finding broken references

- Missing script components
- Null object references
- Broken UnityEvent listeners
- Disconnected prefab instances

```python
unity_validate_integrity({"operation": "all"})
```

### When to use `unity_scene_reference_graph`

**Best for:** Understanding object dependencies

- Find what references a specific object (before deleting/renaming)
- Find orphan objects with no references
- Analyze reference chains

```python
unity_scene_reference_graph({
    "operation": "analyzeObject",
    "objectPath": "Player",
    "format": "summary"
})
```

### When to use `unity_class_catalog`

**Best for:** Type discovery and inspection

- List all MonoBehaviours in a project
- Find ScriptableObject types
- Inspect class fields and methods

```python
unity_class_catalog({
    "operation": "listTypes",
    "typeKind": "MonoBehaviour",
    "namePattern": "*Controller"
})
```

### Decision Matrix

| Requirement | integrity | reference_graph | class_catalog | dependency_graph | relationship_graph |
|-------------|-----------|----------------|---------------|-----------------|-------------------|
| Find broken refs | Yes | No | No | No | No |
| Object dependencies | No | Yes | No | No | No |
| Type discovery | No | No | Yes | No | No |
| Code dependencies | No | No | No | Yes | No |
| Scene transitions | No | No | No | No | Yes |

---

## 6. Sprite & Animation Tools

### When to use `unity_vector_sprite_convert`

**Best for:** Asset generation pipeline

- Create primitive sprites (circle, square, triangle)
- Convert SVG to sprite
- Convert texture to sprite
- Prototyping without art assets

```python
unity_vector_sprite_convert({
    "operation": "primitiveToSprite",
    "primitiveType": "circle",
    "color": {"r": 1, "g": 0, "b": 0, "a": 1},
    "outputPath": "Assets/Sprites/Circle.png"
})
```

### When to use `unity_sprite2d_bundle`

**Best for:** Runtime sprite object management

- Create SpriteRenderer GameObjects
- Manage sorting layers
- Slice sprite sheets
- Create sprite atlases

```python
unity_sprite2d_bundle({
    "operation": "createSprite",
    "name": "Player",
    "spritePath": "Assets/Sprites/Player.png",
    "sortingLayerName": "Characters",
    "sortingOrder": 1
})
```

---

## Summary: Tool Layer Selection

| Layer | Tools | When to Use |
|-------|-------|-------------|
| **High-Level GameKit** | UI Pillar (5), Presentation Pillar (5), Logic Pillar (5) | Game systems, analysis, validation |
| **Mid-Level Batch** | transform_batch, physics_bundle, ui_hierarchy, etc. (21) | Batch operations, presets |
| **Low-Level CRUD** | gameobject_crud, component_crud, asset_crud, etc. (8) | Fine-grained control |
| **Utility** | ping, compilation_await, playmode_control, etc. (5) | Diagnostics, helpers |

**General Principle:** Start with High-Level GameKit, drop to Mid-Level for batch operations, use Low-Level only for precise control. Always validate with Logic Pillar tools after significant changes.
