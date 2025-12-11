# Tilemap Bundle Handler

Mid-level utility for creating and managing Unity Tilemaps, placing/clearing tiles, and creating Tile assets.

## Operations

### Tilemap Management

#### createTilemap

Create a new Tilemap with Grid parent (auto-created if needed).

**Example - Basic Tilemap:**
```json
{
  "operation": "createTilemap",
  "name": "Ground"
}
```

**Example - Tilemap with Options:**
```json
{
  "operation": "createTilemap",
  "name": "Background",
  "cellLayout": "Rectangle",
  "sortingLayerName": "Background",
  "sortingOrder": -10,
  "cellSize": {"x": 1, "y": 1, "z": 0}
}
```

**Parameters:**
- `name` - Tilemap GameObject name
- `parentPath` - Parent path (optional)
- `cellLayout` - Grid layout: "Rectangle" (default), "Hexagon", "Isometric", "IsometricZAsY"
- `cellSize` - Grid cell size (default: 1x1)
- `sortingLayerName` - Sorting layer for rendering
- `sortingOrder` - Sorting order within layer

#### inspect

Get comprehensive information about a Tilemap.

**Example:**
```json
{
  "operation": "inspect",
  "tilemapPath": "Grid/Ground",
  "includeAllTiles": false
}
```

**Response:**
```json
{
  "tilemap": {
    "path": "Grid/Ground",
    "name": "Ground",
    "cellLayout": "Rectangle",
    "cellSize": {"x": 1, "y": 1, "z": 0},
    "tileCount": 150,
    "bounds": {"xMin": -10, "yMin": -5, "xMax": 10, "yMax": 5},
    "renderer": {
      "sortingLayerName": "Default",
      "sortingOrder": 0,
      "mode": "Chunk"
    }
  }
}
```

### Tile Operations

#### setTile

Place a single tile at a position.

**Example:**
```json
{
  "operation": "setTile",
  "tilemapPath": "Grid/Ground",
  "position": {"x": 0, "y": 0, "z": 0},
  "tileAssetPath": "Assets/Tiles/Grass.asset"
}
```

#### getTile

Get tile information at a position.

**Example:**
```json
{
  "operation": "getTile",
  "tilemapPath": "Grid/Ground",
  "position": {"x": 0, "y": 0, "z": 0}
}
```

**Response:**
```json
{
  "position": {"x": 0, "y": 0, "z": 0},
  "hasTile": true,
  "tileName": "Grass",
  "tileAssetPath": "Assets/Tiles/Grass.asset"
}
```

#### setTiles

Place tiles at multiple positions.

**Example:**
```json
{
  "operation": "setTiles",
  "tilemapPath": "Grid/Ground",
  "positions": [
    {"x": 0, "y": 0, "z": 0},
    {"x": 1, "y": 0, "z": 0},
    {"x": 2, "y": 0, "z": 0}
  ],
  "tileAssetPath": "Assets/Tiles/Grass.asset"
}
```

#### clearTile

Remove a single tile.

**Example:**
```json
{
  "operation": "clearTile",
  "tilemapPath": "Grid/Ground",
  "position": {"x": 0, "y": 0, "z": 0}
}
```

#### clearTiles

Clear tiles in a rectangular area.

**Example:**
```json
{
  "operation": "clearTiles",
  "tilemapPath": "Grid/Ground",
  "bounds": {"xMin": -5, "yMin": -5, "xMax": 5, "yMax": 5}
}
```

#### clearAllTiles

Clear all tiles from a Tilemap.

**Example:**
```json
{
  "operation": "clearAllTiles",
  "tilemapPath": "Grid/Ground"
}
```

#### fillArea

Fill a rectangular area with tiles.

**Example:**
```json
{
  "operation": "fillArea",
  "tilemapPath": "Grid/Ground",
  "bounds": {"xMin": 0, "yMin": 0, "xMax": 10, "yMax": 5},
  "tileAssetPath": "Assets/Tiles/Grass.asset"
}
```

#### boxFill

Draw a box outline with tiles.

**Example:**
```json
{
  "operation": "boxFill",
  "tilemapPath": "Grid/Ground",
  "bounds": {"xMin": 0, "yMin": 0, "xMax": 10, "yMax": 10},
  "tileAssetPath": "Assets/Tiles/Wall.asset"
}
```

### Coordinate Conversion

#### worldToCell

Convert world position to cell position.

**Example:**
```json
{
  "operation": "worldToCell",
  "tilemapPath": "Grid/Ground",
  "worldPosition": {"x": 5.5, "y": 3.2, "z": 0}
}
```

**Response:**
```json
{
  "worldPosition": {"x": 5.5, "y": 3.2, "z": 0},
  "cellPosition": {"x": 5, "y": 3, "z": 0}
}
```

#### cellToWorld

Convert cell position to world position.

**Example:**
```json
{
  "operation": "cellToWorld",
  "tilemapPath": "Grid/Ground",
  "cellPosition": {"x": 5, "y": 3, "z": 0}
}
```

**Response:**
```json
{
  "cellPosition": {"x": 5, "y": 3, "z": 0},
  "worldPosition": {"x": 5, "y": 3, "z": 0},
  "centerWorld": {"x": 5.5, "y": 3.5, "z": 0}
}
```

### Component Settings

#### updateRenderer

Update TilemapRenderer settings.

**Example:**
```json
{
  "operation": "updateRenderer",
  "tilemapPath": "Grid/Ground",
  "sortingLayerName": "Foreground",
  "sortingOrder": 10,
  "mode": "Individual"
}
```

#### addCollider

Add TilemapCollider2D with optional CompositeCollider2D.

**Example - Basic Collider:**
```json
{
  "operation": "addCollider",
  "tilemapPath": "Grid/Ground"
}
```

**Example - With Composite Collider:**
```json
{
  "operation": "addCollider",
  "tilemapPath": "Grid/Ground",
  "usedByComposite": true
}
```

#### updateCollider

Update existing TilemapCollider2D settings.

**Example:**
```json
{
  "operation": "updateCollider",
  "tilemapPath": "Grid/Ground",
  "usedByComposite": true,
  "isTrigger": false
}
```

### Tile Asset Creation

#### createTile

Create a new Tile asset from a sprite.

**Example:**
```json
{
  "operation": "createTile",
  "tileAssetPath": "Assets/Tiles/Grass.asset",
  "spritePath": "Assets/Sprites/Grass.png",
  "colliderType": "Grid"
}
```

**Parameters:**
- `tileAssetPath` - Path for the new tile asset
- `spritePath` - Sprite to use for the tile
- `colliderType` - "None" (default), "Sprite", or "Grid"
- `color` - Tile color tint (optional)

#### createRuleTile

Create a RuleTile for auto-tiling (requires 2D Tilemap Extras package).

**Example:**
```json
{
  "operation": "createRuleTile",
  "tileAssetPath": "Assets/Tiles/Ground_Rule.asset",
  "defaultSprite": "Assets/Sprites/Ground_Default.png",
  "rules": [
    {
      "sprites": ["Assets/Sprites/Ground_Center.png"],
      "neighbors": {"up": 1, "down": 1, "left": 1, "right": 1}
    }
  ]
}
```

#### inspectTile

Get information about a Tile asset.

**Example:**
```json
{
  "operation": "inspectTile",
  "tileAssetPath": "Assets/Tiles/Grass.asset"
}
```

**Response:**
```json
{
  "tile": {
    "path": "Assets/Tiles/Grass.asset",
    "name": "Grass",
    "type": "Tile",
    "sprite": "Assets/Sprites/Grass.png",
    "color": {"r": 1, "g": 1, "b": 1, "a": 1},
    "colliderType": "Grid"
  }
}
```

#### updateTile

Update an existing Tile asset.

**Example:**
```json
{
  "operation": "updateTile",
  "tileAssetPath": "Assets/Tiles/Grass.asset",
  "spritePath": "Assets/Sprites/Grass_New.png",
  "colliderType": "Sprite"
}
```

## Common Workflows

### Workflow 1: Create a Basic 2D Level

```python
# 1. Create Tilemap
unity_tilemap_bundle(operation='createTilemap', name='Ground', sortingLayerName='Background')

# 2. Create Tile assets from sprites
unity_tilemap_bundle(operation='createTile', tileAssetPath='Assets/Tiles/Grass.asset',
                     spritePath='Assets/Sprites/Grass.png', colliderType='Grid')
unity_tilemap_bundle(operation='createTile', tileAssetPath='Assets/Tiles/Dirt.asset',
                     spritePath='Assets/Sprites/Dirt.png', colliderType='Grid')

# 3. Fill ground area
unity_tilemap_bundle(operation='fillArea', tilemapPath='Grid/Ground',
                     bounds={'xMin': -20, 'yMin': -10, 'xMax': 20, 'yMax': 0},
                     tileAssetPath='Assets/Tiles/Grass.asset')

# 4. Add collider for physics
unity_tilemap_bundle(operation='addCollider', tilemapPath='Grid/Ground', usedByComposite=True)
```

### Workflow 2: Create Multi-Layer Tilemap Scene

```python
# 1. Setup scene (optional)
unity_scene_quickSetup(setupType='2D')

# 2. Create background layer
unity_tilemap_bundle(operation='createTilemap', name='Background', sortingLayerName='Background', sortingOrder=-100)

# 3. Create ground layer
unity_tilemap_bundle(operation='createTilemap', name='Ground', sortingLayerName='Default', sortingOrder=0)

# 4. Create foreground layer
unity_tilemap_bundle(operation='createTilemap', name='Foreground', sortingLayerName='Foreground', sortingOrder=100)
```

### Workflow 3: Procedural Level Generation

```python
# 1. Create tilemap
unity_tilemap_bundle(operation='createTilemap', name='Dungeon')

# 2. Create room
unity_tilemap_bundle(operation='boxFill', tilemapPath='Grid/Dungeon',
                     bounds={'xMin': 0, 'yMin': 0, 'xMax': 10, 'yMax': 8},
                     tileAssetPath='Assets/Tiles/Wall.asset')

# 3. Fill floor inside room
unity_tilemap_bundle(operation='fillArea', tilemapPath='Grid/Dungeon',
                     bounds={'xMin': 1, 'yMin': 1, 'xMax': 9, 'yMax': 7},
                     tileAssetPath='Assets/Tiles/Floor.asset')

# 4. Create door opening
unity_tilemap_bundle(operation='clearTiles', tilemapPath='Grid/Dungeon',
                     bounds={'xMin': 4, 'yMin': 0, 'xMax': 6, 'yMax': 1})
```

## Cell Layouts

### Rectangle (Default)
Standard rectangular grid for most 2D games.

### Hexagon
Hexagonal grid for strategy games.

### Isometric
Isometric projection for pseudo-3D games.

### IsometricZAsY
Isometric with Z-axis representing Y position.

## Collider Types

### None
No collision detection.

### Sprite
Collider follows sprite outline.

### Grid
Collider fills entire grid cell.

## Tips

1. **Use Sorting Layers** - Organize tilemap rendering order with sorting layers
2. **Composite Collider** - Use `usedByComposite=true` for better performance with many tiles
3. **Batch Operations** - Use `fillArea` instead of multiple `setTile` calls
4. **RuleTiles** - Use RuleTiles for automatic edge/corner tiling
5. **Coordinate Conversion** - Use `worldToCell`/`cellToWorld` for positioning game objects on tiles

## Requirements

- Unity 2021.3 or higher
- 2D Tilemap package (included by default)
- 2D Tilemap Extras package (required for RuleTile)

## Notes

- Grid is automatically created when creating the first Tilemap
- Bounds use integer coordinates (cell positions)
- Z coordinate in positions is typically 0 for 2D games
- TilemapCollider2D requires Rigidbody2D when using composite collider
