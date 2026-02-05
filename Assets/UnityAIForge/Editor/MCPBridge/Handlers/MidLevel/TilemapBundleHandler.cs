using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Mid-level tilemap bundle: create/manage Tilemaps, set/clear tiles, create Tile assets.
    /// </summary>
    public class TilemapBundleHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            // Tilemap management
            "createTilemap",
            "inspect",
            // Tile operations
            "setTile",
            "getTile",
            "setTiles",
            "clearTile",
            "clearTiles",
            "clearAllTiles",
            "fillArea",
            "boxFill",
            // Coordinate conversion
            "worldToCell",
            "cellToWorld",
            // Component settings
            "updateRenderer",
            "updateCollider",
            "addCollider",
            // Tile asset creation
            "createTile",
            "createRuleTile",
            "inspectTile",
            "updateTile",
        };

        public override string Category => "tilemapBundle";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                // Tilemap management
                "createTilemap" => CreateTilemap(payload),
                "inspect" => InspectTilemap(payload),
                // Tile operations
                "setTile" => SetTile(payload),
                "getTile" => GetTile(payload),
                "setTiles" => SetTiles(payload),
                "clearTile" => ClearTile(payload),
                "clearTiles" => ClearTiles(payload),
                "clearAllTiles" => ClearAllTiles(payload),
                "fillArea" => FillArea(payload),
                "boxFill" => BoxFill(payload),
                // Coordinate conversion
                "worldToCell" => WorldToCell(payload),
                "cellToWorld" => CellToWorld(payload),
                // Component settings
                "updateRenderer" => UpdateRenderer(payload),
                "updateCollider" => UpdateCollider(payload),
                "addCollider" => AddCollider(payload),
                // Tile asset creation
                "createTile" => CreateTileAsset(payload),
                "createRuleTile" => CreateRuleTile(payload),
                "inspectTile" => InspectTileAsset(payload),
                "updateTile" => UpdateTileAsset(payload),
                _ => throw new InvalidOperationException($"Unsupported tilemap bundle operation: {operation}"),
            };
        }

        #region Tilemap Management

        private object CreateTilemap(Dictionary<string, object> payload)
        {
            var name = GetString(payload, "name") ?? "Tilemap";
            var parentPath = GetString(payload, "parentPath");
            var cellLayoutStr = GetString(payload, "cellLayout")?.ToLowerInvariant() ?? "rectangle";
            var sortingLayerName = GetString(payload, "sortingLayerName");
            var sortingOrder = GetInt(payload, "sortingOrder", 0);

            var cellLayout = cellLayoutStr switch
            {
                "hexagon" or "hexagonalpointtop" => GridLayout.CellLayout.Hexagon,
                "isometric" => GridLayout.CellLayout.Isometric,
                "isometriczdasy" => GridLayout.CellLayout.IsometricZAsY,
                _ => GridLayout.CellLayout.Rectangle
            };

            // Get or create parent
            GameObject parent = null;
            Grid grid = null;

            if (!string.IsNullOrEmpty(parentPath))
            {
                parent = TryResolveGameObject(parentPath);
                if (parent != null)
                {
                    grid = parent.GetComponent<Grid>();
                }
            }

            // Create Grid if needed
            if (grid == null)
            {
                var gridGo = new GameObject("Grid");
                Undo.RegisterCreatedObjectUndo(gridGo, "Create Grid");
                grid = gridGo.AddComponent<Grid>();
                grid.cellLayout = cellLayout;

                if (payload.TryGetValue("cellSize", out var cellSizeObj) && cellSizeObj is Dictionary<string, object> cellSizeDict)
                {
                    grid.cellSize = GetVector3FromDict(cellSizeDict, Vector3.one);
                }

                if (parent != null)
                {
                    gridGo.transform.SetParent(parent.transform);
                }

                parent = gridGo;
            }

            // Create Tilemap GameObject
            var tilemapGo = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(tilemapGo, "Create Tilemap");
            tilemapGo.transform.SetParent(parent.transform);

            // Add Tilemap component
            var tilemap = tilemapGo.AddComponent<Tilemap>();

            // Add TilemapRenderer
            var renderer = tilemapGo.AddComponent<TilemapRenderer>();
            if (!string.IsNullOrEmpty(sortingLayerName))
            {
                renderer.sortingLayerName = sortingLayerName;
            }
            renderer.sortingOrder = sortingOrder;

            MarkSceneDirty(tilemapGo);

            return CreateSuccessResponse(
                ("path", BuildGameObjectPath(tilemapGo)),
                ("gridPath", BuildGameObjectPath(parent)),
                ("cellLayout", cellLayout.ToString())
            );
        }

        private object InspectTilemap(Dictionary<string, object> payload)
        {
            var tilemapPath = GetString(payload, "tilemapPath");
            var includeAllTiles = GetBool(payload, "includeAllTiles", false);

            if (string.IsNullOrEmpty(tilemapPath))
            {
                throw new InvalidOperationException("'tilemapPath' is required");
            }

            var go = ResolveGameObject(tilemapPath);
            var tilemap = go.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                throw new InvalidOperationException($"GameObject '{tilemapPath}' does not have a Tilemap component");
            }

            var renderer = go.GetComponent<TilemapRenderer>();
            var collider = go.GetComponent<TilemapCollider2D>();
            var grid = tilemap.layoutGrid;

            var result = new Dictionary<string, object>
            {
                ["path"] = BuildGameObjectPath(go),
                ["name"] = go.name,
                ["cellLayout"] = grid != null ? grid.cellLayout.ToString() : "Unknown",
                ["cellSize"] = grid != null ? SerializeVector3(grid.cellSize) : null,
                ["tileCount"] = CountTiles(tilemap),
                ["bounds"] = SerializeBoundsInt(tilemap.cellBounds),
                ["origin"] = SerializeVector3Int(tilemap.origin),
                ["size"] = SerializeVector3Int(tilemap.size),
            };

            if (renderer != null)
            {
                result["renderer"] = new Dictionary<string, object>
                {
                    ["sortingLayerName"] = renderer.sortingLayerName,
                    ["sortingOrder"] = renderer.sortingOrder,
                    ["mode"] = renderer.mode.ToString(),
                };
            }

            if (collider != null)
            {
                #pragma warning disable CS0618 // Type or member is obsolete
                result["collider"] = new Dictionary<string, object>
                {
                    ["usedByComposite"] = collider.usedByComposite,
                    ["usedByEffector"] = collider.usedByEffector,
                };
                #pragma warning restore CS0618
            }

            if (includeAllTiles)
            {
                var tiles = new List<Dictionary<string, object>>();
                var bounds = tilemap.cellBounds;
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    for (int y = bounds.yMin; y < bounds.yMax; y++)
                    {
                        for (int z = bounds.zMin; z < bounds.zMax; z++)
                        {
                            var pos = new Vector3Int(x, y, z);
                            var tile = tilemap.GetTile(pos);
                            if (tile != null)
                            {
                                tiles.Add(new Dictionary<string, object>
                                {
                                    ["position"] = SerializeVector3Int(pos),
                                    ["tileName"] = tile.name,
                                    ["tileAssetPath"] = AssetDatabase.GetAssetPath(tile),
                                });
                            }
                        }
                    }
                }
                result["tiles"] = tiles;
            }

            return CreateSuccessResponse(("tilemap", result));
        }

        private int CountTiles(Tilemap tilemap)
        {
            int count = 0;
            var bounds = tilemap.cellBounds;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    for (int z = bounds.zMin; z < bounds.zMax; z++)
                    {
                        if (tilemap.HasTile(new Vector3Int(x, y, z)))
                        {
                            count++;
                        }
                    }
                }
            }
            return count;
        }

        #endregion

        #region Tile Operations

        private object SetTile(Dictionary<string, object> payload)
        {
            var tilemap = GetTilemap(payload);
            var position = GetVector3IntFromPayload(payload, "position");
            var tileAssetPath = GetString(payload, "tileAssetPath");

            if (string.IsNullOrEmpty(tileAssetPath))
            {
                throw new InvalidOperationException("'tileAssetPath' is required");
            }

            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(tileAssetPath);
            if (tile == null)
            {
                throw new InvalidOperationException($"Tile asset not found at '{tileAssetPath}'");
            }

            Undo.RecordObject(tilemap, "Set Tile");
            tilemap.SetTile(position, tile);
            MarkSceneDirty(tilemap.gameObject);

            return CreateSuccessResponse(
                ("position", SerializeVector3Int(position)),
                ("tileName", tile.name)
            );
        }

        private object GetTile(Dictionary<string, object> payload)
        {
            var tilemap = GetTilemap(payload);
            var position = GetVector3IntFromPayload(payload, "position");

            var tile = tilemap.GetTile(position);
            if (tile == null)
            {
                return CreateSuccessResponse(
                    ("position", SerializeVector3Int(position)),
                    ("hasTile", false),
                    ("tile", (object)null)
                );
            }

            return CreateSuccessResponse(
                ("position", SerializeVector3Int(position)),
                ("hasTile", true),
                ("tileName", tile.name),
                ("tileAssetPath", AssetDatabase.GetAssetPath(tile))
            );
        }

        private object SetTiles(Dictionary<string, object> payload)
        {
            var tilemap = GetTilemap(payload);
            var tileAssetPath = GetString(payload, "tileAssetPath");

            if (string.IsNullOrEmpty(tileAssetPath))
            {
                throw new InvalidOperationException("'tileAssetPath' is required");
            }

            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(tileAssetPath);
            if (tile == null)
            {
                throw new InvalidOperationException($"Tile asset not found at '{tileAssetPath}'");
            }

            var positions = GetVector3IntListFromPayload(payload, "positions");
            if (positions.Count == 0)
            {
                throw new InvalidOperationException("'positions' array is required and cannot be empty");
            }

            Undo.RecordObject(tilemap, "Set Tiles");

            var posArray = positions.ToArray();
            var tileArray = new TileBase[posArray.Length];
            for (int i = 0; i < tileArray.Length; i++)
            {
                tileArray[i] = tile;
            }

            tilemap.SetTiles(posArray, tileArray);
            MarkSceneDirty(tilemap.gameObject);

            return CreateSuccessResponse(
                ("count", positions.Count),
                ("tileName", tile.name)
            );
        }

        private object ClearTile(Dictionary<string, object> payload)
        {
            var tilemap = GetTilemap(payload);
            var position = GetVector3IntFromPayload(payload, "position");

            Undo.RecordObject(tilemap, "Clear Tile");
            tilemap.SetTile(position, null);
            MarkSceneDirty(tilemap.gameObject);

            return CreateSuccessResponse(("position", SerializeVector3Int(position)));
        }

        private object ClearTiles(Dictionary<string, object> payload)
        {
            var tilemap = GetTilemap(payload);
            var bounds = GetBoundsIntFromPayload(payload, "bounds");

            Undo.RecordObject(tilemap, "Clear Tiles");

            int count = 0;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    for (int z = bounds.zMin; z < bounds.zMax; z++)
                    {
                        var pos = new Vector3Int(x, y, z);
                        if (tilemap.HasTile(pos))
                        {
                            tilemap.SetTile(pos, null);
                            count++;
                        }
                    }
                }
            }

            MarkSceneDirty(tilemap.gameObject);

            return CreateSuccessResponse(
                ("bounds", SerializeBoundsInt(bounds)),
                ("clearedCount", count)
            );
        }

        private object ClearAllTiles(Dictionary<string, object> payload)
        {
            var tilemap = GetTilemap(payload);

            var previousCount = CountTiles(tilemap);

            Undo.RecordObject(tilemap, "Clear All Tiles");
            tilemap.ClearAllTiles();
            MarkSceneDirty(tilemap.gameObject);

            return CreateSuccessResponse(("clearedCount", previousCount));
        }

        private object FillArea(Dictionary<string, object> payload)
        {
            var tilemap = GetTilemap(payload);
            var bounds = GetBoundsIntFromPayload(payload, "bounds");
            var tileAssetPath = GetString(payload, "tileAssetPath");

            if (string.IsNullOrEmpty(tileAssetPath))
            {
                throw new InvalidOperationException("'tileAssetPath' is required");
            }

            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(tileAssetPath);
            if (tile == null)
            {
                throw new InvalidOperationException($"Tile asset not found at '{tileAssetPath}'");
            }

            Undo.RecordObject(tilemap, "Fill Area");

            var positions = new List<Vector3Int>();
            var tiles = new List<TileBase>();

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    for (int z = bounds.zMin; z < bounds.zMax; z++)
                    {
                        positions.Add(new Vector3Int(x, y, z));
                        tiles.Add(tile);
                    }
                }
            }

            tilemap.SetTiles(positions.ToArray(), tiles.ToArray());
            MarkSceneDirty(tilemap.gameObject);

            return CreateSuccessResponse(
                ("bounds", SerializeBoundsInt(bounds)),
                ("filledCount", positions.Count),
                ("tileName", tile.name)
            );
        }

        private object BoxFill(Dictionary<string, object> payload)
        {
            var tilemap = GetTilemap(payload);
            var bounds = GetBoundsIntFromPayload(payload, "bounds");
            var tileAssetPath = GetString(payload, "tileAssetPath");

            if (string.IsNullOrEmpty(tileAssetPath))
            {
                throw new InvalidOperationException("'tileAssetPath' is required");
            }

            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(tileAssetPath);
            if (tile == null)
            {
                throw new InvalidOperationException($"Tile asset not found at '{tileAssetPath}'");
            }

            Undo.RecordObject(tilemap, "Box Fill");

            var positions = new List<Vector3Int>();
            var tiles = new List<TileBase>();

            // Draw box outline
            for (int z = bounds.zMin; z < bounds.zMax; z++)
            {
                // Top and bottom edges
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    positions.Add(new Vector3Int(x, bounds.yMin, z));
                    tiles.Add(tile);
                    if (bounds.yMax - 1 != bounds.yMin)
                    {
                        positions.Add(new Vector3Int(x, bounds.yMax - 1, z));
                        tiles.Add(tile);
                    }
                }

                // Left and right edges (excluding corners already added)
                for (int y = bounds.yMin + 1; y < bounds.yMax - 1; y++)
                {
                    positions.Add(new Vector3Int(bounds.xMin, y, z));
                    tiles.Add(tile);
                    if (bounds.xMax - 1 != bounds.xMin)
                    {
                        positions.Add(new Vector3Int(bounds.xMax - 1, y, z));
                        tiles.Add(tile);
                    }
                }
            }

            tilemap.SetTiles(positions.ToArray(), tiles.ToArray());
            MarkSceneDirty(tilemap.gameObject);

            return CreateSuccessResponse(
                ("bounds", SerializeBoundsInt(bounds)),
                ("filledCount", positions.Count),
                ("tileName", tile.name)
            );
        }

        #endregion

        #region Coordinate Conversion

        private object WorldToCell(Dictionary<string, object> payload)
        {
            var tilemap = GetTilemap(payload);
            var worldPosition = GetVector3FromPayload(payload, "worldPosition");

            var cellPosition = tilemap.WorldToCell(worldPosition);

            return CreateSuccessResponse(
                ("worldPosition", SerializeVector3(worldPosition)),
                ("cellPosition", SerializeVector3Int(cellPosition))
            );
        }

        private object CellToWorld(Dictionary<string, object> payload)
        {
            var tilemap = GetTilemap(payload);
            var cellPosition = GetVector3IntFromPayload(payload, "cellPosition");

            var worldPosition = tilemap.CellToWorld(cellPosition);
            var centerWorld = tilemap.GetCellCenterWorld(cellPosition);

            return CreateSuccessResponse(
                ("cellPosition", SerializeVector3Int(cellPosition)),
                ("worldPosition", SerializeVector3(worldPosition)),
                ("centerWorld", SerializeVector3(centerWorld))
            );
        }

        #endregion

        #region Component Settings

        private object UpdateRenderer(Dictionary<string, object> payload)
        {
            var tilemapPath = GetString(payload, "tilemapPath");
            if (string.IsNullOrEmpty(tilemapPath))
            {
                throw new InvalidOperationException("'tilemapPath' is required");
            }

            var go = ResolveGameObject(tilemapPath);
            var renderer = go.GetComponent<TilemapRenderer>();
            if (renderer == null)
            {
                throw new InvalidOperationException($"GameObject '{tilemapPath}' does not have a TilemapRenderer component");
            }

            Undo.RecordObject(renderer, "Update Tilemap Renderer");

            var sortingLayerName = GetString(payload, "sortingLayerName");
            if (!string.IsNullOrEmpty(sortingLayerName))
            {
                renderer.sortingLayerName = sortingLayerName;
            }

            if (payload.ContainsKey("sortingOrder"))
            {
                renderer.sortingOrder = GetInt(payload, "sortingOrder", 0);
            }

            if (payload.TryGetValue("mode", out var modeObj))
            {
                var modeStr = modeObj.ToString().ToLowerInvariant();
                renderer.mode = modeStr switch
                {
                    "chunk" => TilemapRenderer.Mode.Chunk,
                    "individual" => TilemapRenderer.Mode.Individual,
                    _ => renderer.mode
                };
            }

            MarkSceneDirty(go);

            return CreateSuccessResponse(
                ("path", BuildGameObjectPath(go)),
                ("sortingLayerName", renderer.sortingLayerName),
                ("sortingOrder", renderer.sortingOrder),
                ("mode", renderer.mode.ToString())
            );
        }

        private object UpdateCollider(Dictionary<string, object> payload)
        {
            var tilemapPath = GetString(payload, "tilemapPath");
            if (string.IsNullOrEmpty(tilemapPath))
            {
                throw new InvalidOperationException("'tilemapPath' is required");
            }

            var go = ResolveGameObject(tilemapPath);
            var collider = go.GetComponent<TilemapCollider2D>();
            if (collider == null)
            {
                throw new InvalidOperationException($"GameObject '{tilemapPath}' does not have a TilemapCollider2D component");
            }

            Undo.RecordObject(collider, "Update Tilemap Collider");

            #pragma warning disable CS0618 // Type or member is obsolete
            if (payload.ContainsKey("usedByComposite"))
            {
                collider.usedByComposite = GetBool(payload, "usedByComposite", false);
            }

            if (payload.ContainsKey("usedByEffector"))
            {
                collider.usedByEffector = GetBool(payload, "usedByEffector", false);
            }
            #pragma warning restore CS0618

            if (payload.ContainsKey("isTrigger"))
            {
                collider.isTrigger = GetBool(payload, "isTrigger", false);
            }

            MarkSceneDirty(go);

            #pragma warning disable CS0618 // Type or member is obsolete
            return CreateSuccessResponse(
                ("path", BuildGameObjectPath(go)),
                ("usedByComposite", collider.usedByComposite),
                ("usedByEffector", collider.usedByEffector),
                ("isTrigger", collider.isTrigger)
            );
            #pragma warning restore CS0618
        }

        private object AddCollider(Dictionary<string, object> payload)
        {
            var tilemapPath = GetString(payload, "tilemapPath");
            if (string.IsNullOrEmpty(tilemapPath))
            {
                throw new InvalidOperationException("'tilemapPath' is required");
            }

            var go = ResolveGameObject(tilemapPath);
            var tilemap = go.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                throw new InvalidOperationException($"GameObject '{tilemapPath}' does not have a Tilemap component");
            }

            var collider = go.GetComponent<TilemapCollider2D>();
            if (collider == null)
            {
                collider = Undo.AddComponent<TilemapCollider2D>(go);
            }

            #pragma warning disable CS0618 // Type or member is obsolete
            var usedByComposite = GetBool(payload, "usedByComposite", false);
            collider.usedByComposite = usedByComposite;
            #pragma warning restore CS0618

            // If using composite, add CompositeCollider2D and Rigidbody2D if not present
            if (usedByComposite)
            {
                var composite = go.GetComponent<CompositeCollider2D>();
                if (composite == null)
                {
                    // CompositeCollider2D requires Rigidbody2D
                    var rb = go.GetComponent<Rigidbody2D>();
                    if (rb == null)
                    {
                        rb = Undo.AddComponent<Rigidbody2D>(go);
                        rb.bodyType = RigidbodyType2D.Static;
                    }
                    composite = Undo.AddComponent<CompositeCollider2D>(go);
                }
            }

            MarkSceneDirty(go);

            #pragma warning disable CS0618 // Type or member is obsolete
            return CreateSuccessResponse(
                ("path", BuildGameObjectPath(go)),
                ("usedByComposite", collider.usedByComposite),
                ("hasCompositeCollider", go.GetComponent<CompositeCollider2D>() != null)
            );
            #pragma warning restore CS0618
        }

        #endregion

        #region Tile Asset Creation

        private object CreateTileAsset(Dictionary<string, object> payload)
        {
            var tileAssetPath = GetString(payload, "tileAssetPath");
            var spritePath = GetString(payload, "spritePath");

            if (string.IsNullOrEmpty(tileAssetPath))
            {
                throw new InvalidOperationException("'tileAssetPath' is required");
            }

            if (string.IsNullOrEmpty(spritePath))
            {
                throw new InvalidOperationException("'spritePath' is required");
            }

            // Ensure path ends with .asset
            if (!tileAssetPath.EndsWith(".asset"))
            {
                tileAssetPath += ".asset";
            }

            // Load sprite
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                throw new InvalidOperationException($"Sprite not found at '{spritePath}'");
            }

            // Create tile
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;

            // Set collider type
            var colliderTypeStr = GetString(payload, "colliderType")?.ToLowerInvariant() ?? "none";
            tile.colliderType = colliderTypeStr switch
            {
                "sprite" => Tile.ColliderType.Sprite,
                "grid" => Tile.ColliderType.Grid,
                _ => Tile.ColliderType.None
            };

            // Set color if specified
            if (payload.TryGetValue("color", out var colorObj) && colorObj is Dictionary<string, object> colorDict)
            {
                tile.color = GetColorFromDict(colorDict);
            }

            // Ensure directory exists
            var directory = System.IO.Path.GetDirectoryName(tileAssetPath);
            if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
            {
                CreateFoldersRecursively(directory);
            }

            AssetDatabase.CreateAsset(tile, tileAssetPath);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("tileAssetPath", tileAssetPath),
                ("spritePath", spritePath),
                ("colliderType", tile.colliderType.ToString())
            );
        }

        private object CreateRuleTile(Dictionary<string, object> payload)
        {
            // RuleTile requires 2D Tilemap Extras package
            // Check if RuleTile type exists
            var ruleTileType = GetRuleTileType();
            if (ruleTileType == null)
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "RuleTile requires '2D Tilemap Extras' package. Please install it from Package Manager.",
                    ["errorType"] = "MissingPackageException",
                    ["category"] = Category
                };
            }

            var tileAssetPath = GetString(payload, "tileAssetPath");
            var defaultSpritePath = GetString(payload, "defaultSprite");

            if (string.IsNullOrEmpty(tileAssetPath))
            {
                throw new InvalidOperationException("'tileAssetPath' is required");
            }

            // Ensure path ends with .asset
            if (!tileAssetPath.EndsWith(".asset"))
            {
                tileAssetPath += ".asset";
            }

            // Create RuleTile instance
            var ruleTile = ScriptableObject.CreateInstance(ruleTileType);

            // Set default sprite if provided
            if (!string.IsNullOrEmpty(defaultSpritePath))
            {
                var defaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>(defaultSpritePath);
                if (defaultSprite != null)
                {
                    var defaultSpriteField = ruleTileType.GetField("m_DefaultSprite",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (defaultSpriteField != null)
                    {
                        defaultSpriteField.SetValue(ruleTile, defaultSprite);
                    }
                }
            }

            // Process rules if provided
            if (payload.TryGetValue("rules", out var rulesObj) && rulesObj is List<object> rulesList)
            {
                SetRuleTileRules(ruleTile, ruleTileType, rulesList);
            }

            // Ensure directory exists
            var directory = System.IO.Path.GetDirectoryName(tileAssetPath);
            if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
            {
                CreateFoldersRecursively(directory);
            }

            AssetDatabase.CreateAsset(ruleTile, tileAssetPath);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("tileAssetPath", tileAssetPath),
                ("type", "RuleTile")
            );
        }

        private Type GetRuleTileType()
        {
            // Try to find RuleTile in assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("UnityEngine.Tilemaps.RuleTile");
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

        private void SetRuleTileRules(ScriptableObject ruleTile, Type ruleTileType, List<object> rulesList)
        {
            // Get the TilingRules field
            var tilingRulesField = ruleTileType.GetField("m_TilingRules",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (tilingRulesField == null) return;

            // Get TilingRule nested type
            var tilingRuleType = ruleTileType.GetNestedType("TilingRule");
            if (tilingRuleType == null) return;

            var ruleListType = typeof(List<>).MakeGenericType(tilingRuleType);
            var rules = Activator.CreateInstance(ruleListType);
            var addMethod = ruleListType.GetMethod("Add");

            foreach (var ruleObj in rulesList)
            {
                if (ruleObj is Dictionary<string, object> ruleDict)
                {
                    var rule = Activator.CreateInstance(tilingRuleType);

                    // Set sprites
                    if (ruleDict.TryGetValue("sprites", out var spritesObj) && spritesObj is List<object> spritesList)
                    {
                        var sprites = new List<Sprite>();
                        foreach (var spritePath in spritesList)
                        {
                            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath.ToString());
                            if (sprite != null)
                            {
                                sprites.Add(sprite);
                            }
                        }

                        var spritesField = tilingRuleType.GetField("m_Sprites",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (spritesField != null)
                        {
                            spritesField.SetValue(rule, sprites.ToArray());
                        }
                    }

                    addMethod.Invoke(rules, new[] { rule });
                }
            }

            tilingRulesField.SetValue(ruleTile, rules);
        }

        private object InspectTileAsset(Dictionary<string, object> payload)
        {
            var tileAssetPath = GetString(payload, "tileAssetPath");
            if (string.IsNullOrEmpty(tileAssetPath))
            {
                throw new InvalidOperationException("'tileAssetPath' is required");
            }

            var tileBase = AssetDatabase.LoadAssetAtPath<TileBase>(tileAssetPath);
            if (tileBase == null)
            {
                throw new InvalidOperationException($"Tile asset not found at '{tileAssetPath}'");
            }

            var result = new Dictionary<string, object>
            {
                ["path"] = tileAssetPath,
                ["name"] = tileBase.name,
                ["type"] = tileBase.GetType().Name,
            };

            if (tileBase is Tile tile)
            {
                result["sprite"] = tile.sprite != null ? AssetDatabase.GetAssetPath(tile.sprite) : null;
                result["color"] = SerializeColor(tile.color);
                result["colliderType"] = tile.colliderType.ToString();
            }

            return CreateSuccessResponse(("tile", result));
        }

        private object UpdateTileAsset(Dictionary<string, object> payload)
        {
            var tileAssetPath = GetString(payload, "tileAssetPath");
            if (string.IsNullOrEmpty(tileAssetPath))
            {
                throw new InvalidOperationException("'tileAssetPath' is required");
            }

            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tileAssetPath);
            if (tile == null)
            {
                throw new InvalidOperationException($"Tile asset not found at '{tileAssetPath}' (must be a Tile, not RuleTile)");
            }

            Undo.RecordObject(tile, "Update Tile Asset");

            // Update sprite
            var spritePath = GetString(payload, "spritePath");
            if (!string.IsNullOrEmpty(spritePath))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite != null)
                {
                    tile.sprite = sprite;
                }
            }

            // Update color
            if (payload.TryGetValue("color", out var colorObj) && colorObj is Dictionary<string, object> colorDict)
            {
                tile.color = GetColorFromDict(colorDict);
            }

            // Update collider type
            if (payload.TryGetValue("colliderType", out var colliderTypeObj))
            {
                var colliderTypeStr = colliderTypeObj.ToString().ToLowerInvariant();
                tile.colliderType = colliderTypeStr switch
                {
                    "sprite" => Tile.ColliderType.Sprite,
                    "grid" => Tile.ColliderType.Grid,
                    _ => Tile.ColliderType.None
                };
            }

            EditorUtility.SetDirty(tile);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("tileAssetPath", tileAssetPath),
                ("sprite", tile.sprite != null ? AssetDatabase.GetAssetPath(tile.sprite) : null),
                ("colliderType", tile.colliderType.ToString())
            );
        }

        #endregion

        #region Helper Methods

        private Tilemap GetTilemap(Dictionary<string, object> payload)
        {
            var tilemapPath = GetString(payload, "tilemapPath");
            if (string.IsNullOrEmpty(tilemapPath))
            {
                throw new InvalidOperationException("'tilemapPath' is required");
            }

            var go = ResolveGameObject(tilemapPath);
            var tilemap = go.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                throw new InvalidOperationException($"GameObject '{tilemapPath}' does not have a Tilemap component");
            }

            return tilemap;
        }

        private Vector3Int GetVector3IntFromPayload(Dictionary<string, object> payload, string key)
        {
            if (!payload.TryGetValue(key, out var value) || !(value is Dictionary<string, object> dict))
            {
                throw new InvalidOperationException($"'{key}' is required and must be an object with x, y, z properties");
            }

            int x = dict.TryGetValue("x", out var xObj) ? Convert.ToInt32(xObj) : 0;
            int y = dict.TryGetValue("y", out var yObj) ? Convert.ToInt32(yObj) : 0;
            int z = dict.TryGetValue("z", out var zObj) ? Convert.ToInt32(zObj) : 0;

            return new Vector3Int(x, y, z);
        }

        private List<Vector3Int> GetVector3IntListFromPayload(Dictionary<string, object> payload, string key)
        {
            var result = new List<Vector3Int>();

            if (!payload.TryGetValue(key, out var value) || !(value is List<object> list))
            {
                return result;
            }

            foreach (var item in list)
            {
                if (item is Dictionary<string, object> dict)
                {
                    int x = dict.TryGetValue("x", out var xObj) ? Convert.ToInt32(xObj) : 0;
                    int y = dict.TryGetValue("y", out var yObj) ? Convert.ToInt32(yObj) : 0;
                    int z = dict.TryGetValue("z", out var zObj) ? Convert.ToInt32(zObj) : 0;
                    result.Add(new Vector3Int(x, y, z));
                }
            }

            return result;
        }

        private Vector3 GetVector3FromPayload(Dictionary<string, object> payload, string key)
        {
            if (!payload.TryGetValue(key, out var value) || !(value is Dictionary<string, object> dict))
            {
                throw new InvalidOperationException($"'{key}' is required and must be an object with x, y, z properties");
            }

            return GetVector3FromDict(dict, Vector3.zero);
        }

        private BoundsInt GetBoundsIntFromPayload(Dictionary<string, object> payload, string key)
        {
            if (!payload.TryGetValue(key, out var value) || !(value is Dictionary<string, object> dict))
            {
                throw new InvalidOperationException($"'{key}' is required");
            }

            int xMin = dict.TryGetValue("xMin", out var xMinObj) ? Convert.ToInt32(xMinObj) : 0;
            int yMin = dict.TryGetValue("yMin", out var yMinObj) ? Convert.ToInt32(yMinObj) : 0;
            int zMin = dict.TryGetValue("zMin", out var zMinObj) ? Convert.ToInt32(zMinObj) : 0;
            int xMax = dict.TryGetValue("xMax", out var xMaxObj) ? Convert.ToInt32(xMaxObj) : 0;
            int yMax = dict.TryGetValue("yMax", out var yMaxObj) ? Convert.ToInt32(yMaxObj) : 0;
            int zMax = dict.TryGetValue("zMax", out var zMaxObj) ? Convert.ToInt32(zMaxObj) : 0;

            // Ensure zMax is at least zMin + 1 for proper iteration
            if (zMax <= zMin) zMax = zMin + 1;

            var position = new Vector3Int(xMin, yMin, zMin);
            var size = new Vector3Int(xMax - xMin, yMax - yMin, zMax - zMin);

            return new BoundsInt(position, size);
        }

        private Dictionary<string, object> SerializeVector3(Vector3 v)
        {
            return new Dictionary<string, object> { ["x"] = v.x, ["y"] = v.y, ["z"] = v.z };
        }

        private Dictionary<string, object> SerializeVector3Int(Vector3Int v)
        {
            return new Dictionary<string, object> { ["x"] = v.x, ["y"] = v.y, ["z"] = v.z };
        }

        private Dictionary<string, object> SerializeBoundsInt(BoundsInt bounds)
        {
            return new Dictionary<string, object>
            {
                ["xMin"] = bounds.xMin,
                ["yMin"] = bounds.yMin,
                ["zMin"] = bounds.zMin,
                ["xMax"] = bounds.xMax,
                ["yMax"] = bounds.yMax,
                ["zMax"] = bounds.zMax,
                ["size"] = SerializeVector3Int(bounds.size),
            };
        }

        private Dictionary<string, object> SerializeColor(Color c)
        {
            return new Dictionary<string, object> { ["r"] = c.r, ["g"] = c.g, ["b"] = c.b, ["a"] = c.a };
        }

        private void CreateFoldersRecursively(string path)
        {
            var parts = path.Replace("\\", "/").Split('/');
            var currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                var nextPath = currentPath + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }
                currentPath = nextPath;
            }
        }

        #endregion
    }
}
