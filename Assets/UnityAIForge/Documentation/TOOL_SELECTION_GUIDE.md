# MCP Tool Selection Guide

This guide helps you choose the right tool for your Unity development tasks.

---

## Quick Selection Table

| Use Case | Recommended Tool |
|----------|-----------------|
| Build UI menu from JSON | `unity_ui_hierarchy` |
| Configure keyboard/gamepad navigation | `unity_ui_navigation` |
| Manage UI visibility states | `unity_ui_state` |
| Create single UI element | `unity_ui_foundation` |

---

## 1. Trigger & Interaction Tools

### When to use `unity_gamekit_interaction`

**Best for:** Custom triggers with scripted actions

- Door/switch/treasure chest interactions
- Custom script message sending (SendMessage, BroadcastMessage)
- Complex multi-action sequences
- Spawn prefab on trigger
- Scene changes triggered by player input

**Example use cases:**
- Player presses button → spawn enemy + play sound + send message
- Proximity trigger → open dialog + disable collider
- Raycast hit → spawn particles + destroy object

```python
unity_gamekit_interaction(
    operation='create',
    interactionId='door_trigger',
    triggerType='trigger',
    actions=[
        {'type': 'sendMessage', 'target': 'DoorController', 'parameter': 'Open'},
        {'type': 'playSound', 'target': 'self', 'parameter': 'door_open'}
    ],
    conditions=[{'type': 'tag', 'value': 'Player'}]
)
```

### When to use `unity_gamekit_trigger_zone`

**Best for:** Built-in zone effects (no scripting required)

- Checkpoint zones (save progress)
- Damage zones (lava, poison, etc.)
- Heal zones (regeneration areas)
- Teleport zones (portals)
- Speed boost/slow down zones
- Kill zones (instant death)
- Safe zones (prevent damage)

**Example use cases:**
- Walk into lava → take 10 damage per second
- Touch checkpoint → save position
- Enter portal → teleport to destination
- Enter water → slow down movement

```python
unity_gamekit_trigger_zone(
    operation='create',
    zoneId='lava_zone',
    zoneType='DamageZone',
    effectAmount=10.0,
    effectInterval=0.5,
    is2D=True
)
```

### Decision Matrix

| Requirement | Use `interaction` | Use `trigger_zone` |
|-------------|-------------------|-------------------|
| Custom script calls | Yes | No |
| Built-in damage/heal | No | Yes |
| Checkpoint system | No | Yes |
| Teleportation | Possible | Built-in |
| Spawn prefab | Yes | No |
| Complex conditions | Yes | Basic |

---

## 2. AI & Path Following Tools

### When to use `unity_gamekit_ai`

**Best for:** Intelligent behaviors with state machines

- Patrol + chase player when detected
- Flee when health is low
- Line-of-sight detection
- Field of view constraints
- State transitions (Idle → Patrol → Chase → Attack)
- Attack range detection

**Example use cases:**
- Enemy patrols between points, chases player when detected
- NPC follows player, returns to patrol when player leaves area
- Guard with field of view detection

```python
unity_gamekit_ai(
    operation='create',
    aiId='enemy_ai',
    behaviorType='PatrolAndChase',
    moveSpeed=3.0,
    detectionRadius=8.0,
    fieldOfView=120
)
```

### When to use `unity_gamekit_waypoint`

**Best for:** Simple path following (no AI decision-making)

- Moving platforms
- Elevator systems
- NPCs on fixed routes (no player interaction)
- Camera rails
- Animated props

**Example use cases:**
- Platform moves between two points
- Train follows track
- Flying bird follows path
- Camera dolly movement

```python
unity_gamekit_waypoint(
    operation='create',
    waypointId='platform_path',
    pathMode='PingPong',
    moveSpeed=2.0,
    autoStart=True
)
```

### Decision Matrix

| Requirement | Use `gamekit_ai` | Use `gamekit_waypoint` |
|-------------|-----------------|----------------------|
| Player detection | Yes | No |
| State machine | Yes | No |
| Simple path follow | Possible | Yes |
| Moving platforms | No | Yes |
| Chase behavior | Yes | No |
| Attack behavior | Yes | No |

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
unity_physics_bundle(
    operation='applyPreset2D',
    gameObjectPath='Player',
    preset='platformer'
)
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
unity_character_controller_bundle(
    operation='applyPreset',
    gameObjectPath='Player',
    preset='fps'
)
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

## 4. UI Tools

### When to use `unity_ui_hierarchy`

**Best for:** Declarative UI creation from JSON

- Build entire menu systems from JSON definition
- Complex nested UI structures
- Rapid prototyping
- Show/hide UI panels

```python
unity_ui_hierarchy(
    operation='create',
    parentPath='Canvas',
    hierarchy={
        'type': 'panel',
        'name': 'MainMenu',
        'children': [
            {'type': 'button', 'name': 'StartBtn', 'text': 'Start'},
            {'type': 'button', 'name': 'QuitBtn', 'text': 'Quit'}
        ],
        'layout': 'Vertical'
    }
)
```

### When to use `unity_ui_navigation`

**Best for:** Keyboard/gamepad navigation setup

- Auto-setup vertical/horizontal navigation
- Grid navigation for inventories
- Explicit navigation (custom up/down/left/right)
- Navigation groups with isolation
- First selected element setting

```python
unity_ui_navigation(
    operation='autoSetup',
    rootPath='Canvas/MainMenu',
    direction='vertical',
    wrapAround=True
)
```

### When to use `unity_ui_state`

**Best for:** UI state management

- Define show/hidden states
- Manage dialog open/close
- State groups (mutually exclusive menus)
- Save/restore UI state

```python
unity_ui_state(
    operation='defineState',
    stateName='hidden',
    rootPath='Canvas/Dialog',
    elements=[{'path': '', 'active': False, 'alpha': 0}]
)
```

### When to use `unity_ui_foundation`

**Best for:** Single UI element creation

- Create individual buttons, panels, text
- Add LayoutGroup to existing panels
- Simple one-off UI elements

```python
unity_ui_foundation(
    operation='createButton',
    name='MyButton',
    parentPath='Canvas',
    text='Click Me',
    width=200,
    height=50
)
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

## 5. Actor & Manager Tools

### When to use `unity_gamekit_actor`

**Best for:** Creating game characters

- Player characters with movement profiles
- Enemies with AI control
- NPCs with script triggers

**Control modes:**
- `directController` - Player input
- `aiAutonomous` - Basic AI (wander)
- `uiCommand` - Controlled by UI buttons
- `scriptTriggerOnly` - Event-driven only

```python
unity_gamekit_actor(
    operation='create',
    actorId='player',
    behaviorProfile='2dPhysics',
    controlMode='directController'
)
```

### When to use `unity_gamekit_ai` with Actor

**Important:** `Actor.aiAutonomous` provides only basic autonomous behavior.
For advanced AI (patrol, chase, flee), combine Actor with `unity_gamekit_ai`:

```python
# Step 1: Create actor
unity_gamekit_actor(
    operation='create',
    actorId='enemy',
    behaviorProfile='2dPhysics',
    controlMode='aiAutonomous'
)

# Step 2: Add advanced AI behavior
unity_gamekit_ai(
    operation='create',
    gameObjectPath='enemy',
    aiId='enemy_ai',
    behaviorType='PatrolAndChase',
    detectionRadius=8.0
)
```

---

## 6. Sprite & Animation Tools

### When to use `unity_vector_sprite_convert`

**Best for:** Asset generation pipeline

- Create primitive sprites (circle, square, triangle)
- Convert SVG to sprite
- Convert texture to sprite
- Prototyping without art assets

```python
unity_vector_sprite_convert(
    operation='primitiveToSprite',
    primitiveType='circle',
    color={'r': 1, 'g': 0, 'b': 0, 'a': 1},
    outputPath='Assets/Sprites/Circle.png'
)
```

### When to use `unity_sprite2d_bundle`

**Best for:** Runtime sprite object management

- Create SpriteRenderer GameObjects
- Manage sorting layers
- Slice sprite sheets
- Create sprite atlases

```python
unity_sprite2d_bundle(
    operation='createSprite',
    name='Player',
    spritePath='Assets/Sprites/Player.png',
    sortingLayerName='Characters',
    sortingOrder=1
)
```

---

## Summary: Tool Layer Selection

| Layer | Tools | When to Use |
|-------|-------|-------------|
| **High-Level GameKit** | actor, manager, interaction, trigger_zone, etc. | Game mechanics, systems |
| **Mid-Level Batch** | transform_batch, physics_bundle, ui_hierarchy, etc. | Batch operations, presets |
| **Low-Level CRUD** | gameobject_crud, component_crud, asset_crud, etc. | Fine-grained control |

**General Principle:** Start with High-Level, drop to Mid-Level for batch operations, use Low-Level only for precise control.
