# CharacterController Bundle Handler

Mid-level utility for applying CharacterController components with presets for common character types.

## Operations

### applyPreset

Apply a CharacterController preset to one or more GameObjects.

**Example - FPS Character:**
```json
{
  "operation": "applyPreset",
  "gameObjectPath": "Player",
  "preset": "fps"
}
```

**Example - Batch Apply:**
```json
{
  "operation": "applyPreset",
  "gameObjectPaths": ["Player1", "Player2", "Player3"],
  "preset": "tps"
}
```

### update

Update an existing CharacterController's properties.

**Example:**
```json
{
  "operation": "update",
  "gameObjectPath": "Player",
  "radius": 0.6,
  "height": 2.2,
  "slopeLimit": 50
}
```

### inspect

Inspect CharacterController properties on one or more GameObjects.

**Example:**
```json
{
  "operation": "inspect",
  "gameObjectPath": "Player"
}
```

**Response:**
```json
{
  "controllers": [
    {
      "path": "Player",
      "hasCharacterController": true,
      "radius": 0.5,
      "height": 2.0,
      "center": {"x": 0, "y": 1, "z": 0},
      "slopeLimit": 45,
      "stepOffset": 0.3,
      "skinWidth": 0.08,
      "minMoveDistance": 0.001,
      "isGrounded": true,
      "velocity": {"x": 0, "y": 0, "z": 0},
      "collisionFlags": "Below"
    }
  ]
}
```

## Presets

### fps (First-Person Shooter)
Standard FPS character settings.
- **Radius**: 0.5
- **Height**: 2.0
- **Center**: (0, 1, 0)
- **Slope Limit**: 45°
- **Step Offset**: 0.3

**Use Case**: Standard first-person games, realistic human character

### tps (Third-Person Shooter)
Third-person character settings (slightly smaller).
- **Radius**: 0.4
- **Height**: 1.8
- **Center**: (0, 0.9, 0)
- **Slope Limit**: 45°
- **Step Offset**: 0.3

**Use Case**: Third-person action games, better camera clearance

### platformer
Platformer character settings (tighter control).
- **Radius**: 0.3
- **Height**: 1.6
- **Center**: (0, 0.8, 0)
- **Slope Limit**: 50°
- **Step Offset**: 0.4
- **Min Move Distance**: 0 (for precise movement)

**Use Case**: 3D platformers, precise jumping games

### child
Smaller character settings.
- **Radius**: 0.35
- **Height**: 1.2
- **Center**: (0, 0.6, 0)
- **Slope Limit**: 45°
- **Step Offset**: 0.2

**Use Case**: Child characters, halflings, gnomes

### large
Large character settings.
- **Radius**: 1.0
- **Height**: 3.5
- **Center**: (0, 1.75, 0)
- **Slope Limit**: 40°
- **Step Offset**: 0.5
- **Skin Width**: 0.1

**Use Case**: Ogres, mechs, large creatures

### narrow
Narrow character settings.
- **Radius**: 0.25
- **Height**: 1.8
- **Center**: (0, 0.9, 0)
- **Slope Limit**: 45°
- **Step Offset**: 0.3

**Use Case**: Characters that need to fit through tight spaces

### custom
Base settings that can be fully customized via parameters.

## Parameters

All presets can be further customized with these parameters:

| Parameter | Type | Description |
|-----------|------|-------------|
| `radius` | number | Capsule radius |
| `height` | number | Capsule height |
| `center` | object | Center offset {x, y, z} |
| `slopeLimit` | number | Maximum slope angle in degrees |
| `stepOffset` | number | Maximum step height |
| `skinWidth` | number | Skin width for collision detection |
| `minMoveDistance` | number | Minimum move distance threshold |

## Usage Examples

### Create FPS Player with Custom Height

```json
{
  "operation": "applyPreset",
  "gameObjectPath": "Player",
  "preset": "fps",
  "height": 1.9
}
```

### Create Platformer Character with Custom Jump Height

```json
{
  "operation": "applyPreset",
  "gameObjectPath": "Hero",
  "preset": "platformer",
  "stepOffset": 0.5
}
```

### Update Existing Controller

```json
{
  "operation": "update",
  "gameObjectPath": "Player",
  "slopeLimit": 60,
  "stepOffset": 0.4
}
```

### Batch Create Multiple Characters

```json
{
  "operation": "applyPreset",
  "gameObjectPaths": ["Enemy1", "Enemy2", "Enemy3"],
  "preset": "child",
  "radius": 0.3
}
```

## Integration with GameKit

CharacterController Bundle works seamlessly with GameKit Actor's `3dCharacterController` behavior profile:

```json
{
  "operation": "create",
  "actorId": "Player",
  "behaviorProfile": "3dCharacterController",
  "controlMode": "directController"
}
```

Then apply a CharacterController preset:

```json
{
  "operation": "applyPreset",
  "gameObjectPath": "Player",
  "preset": "fps"
}
```

## Tips

1. **FPS vs TPS**: TPS preset has a smaller radius for better third-person camera clearance
2. **Platformers**: Use lower `minMoveDistance` (0) for more precise movement control
3. **Slopes**: Increase `slopeLimit` for characters that can climb steeper surfaces
4. **Steps**: Increase `stepOffset` for characters that need to climb higher obstacles
5. **Tight Spaces**: Use the `narrow` preset or custom small radius for dungeon crawlers
6. **Large Characters**: Adjust `skinWidth` proportionally to avoid collision issues

## Common Workflows

### Workflow 1: Quick FPS Setup
1. Create empty GameObject named "Player"
2. Apply FPS preset
3. Add camera as child
4. Add movement script

### Workflow 2: Platformer with Collectibles
1. Create player with platformer preset
2. Adjust `stepOffset` for level design
3. Test collision with various obstacles
4. Fine-tune `radius` and `height`

### Workflow 3: Multi-Character Game
1. Use batch operations to apply presets to all characters
2. Customize each character's size via parameters
3. Test movement in various environments
4. Adjust collision properties as needed

