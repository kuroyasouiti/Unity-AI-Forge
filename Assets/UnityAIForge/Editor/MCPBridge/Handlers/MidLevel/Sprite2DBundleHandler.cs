using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Mid-level 2D sprite bundle: SpriteRenderer management, sprite assignment,
    /// sorting layers, flipX/Y, color tinting, and sprite sheet slicing.
    /// </summary>
    public class Sprite2DBundleHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            // SpriteRenderer management
            "createSprite",
            "updateSprite",
            "inspect",
            // Batch operations
            "updateMultiple",
            "setSortingLayer",
            "setColor",
            // Sprite asset operations
            "sliceSpriteSheet",
            "createSpriteAtlas",
        };

        public override string Category => "sprite2DBundle";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "createSprite" => CreateSprite(payload),
                "updateSprite" => UpdateSprite(payload),
                "inspect" => InspectSprite(payload),
                "updateMultiple" => UpdateMultiple(payload),
                "setSortingLayer" => SetSortingLayer(payload),
                "setColor" => SetColor(payload),
                "sliceSpriteSheet" => SliceSpriteSheet(payload),
                "createSpriteAtlas" => CreateSpriteAtlas(payload),
                _ => throw new InvalidOperationException($"Unsupported sprite2D bundle operation: {operation}"),
            };
        }

        #region Create Sprite

        private object CreateSprite(Dictionary<string, object> payload)
        {
            var name = GetString(payload, "name") ?? "NewSprite";
            var parentPath = GetString(payload, "parentPath");
            var spritePath = GetString(payload, "spritePath");
            var sortingLayerName = GetString(payload, "sortingLayerName") ?? "Default";
            var sortingOrder = GetInt(payload, "sortingOrder", 0);
            var color = GetColor(payload, "color", Color.white);
            var flipX = GetBool(payload, "flipX", false);
            var flipY = GetBool(payload, "flipY", false);

            // Create GameObject
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create Sprite");

            // Set parent
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = GameObject.Find(parentPath);
                if (parent != null)
                {
                    go.transform.SetParent(parent.transform, false);
                }
            }

            // Set position
            if (payload.ContainsKey("position"))
            {
                go.transform.position = GetVector3(payload, "position", Vector3.zero);
            }

            // Add SpriteRenderer
            var sr = go.AddComponent<SpriteRenderer>();

            // Load and assign sprite
            if (!string.IsNullOrEmpty(spritePath))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite != null)
                {
                    sr.sprite = sprite;
                }
                else
                {
                    Debug.LogWarning($"[Sprite2DBundle] Sprite not found at: {spritePath}");
                }
            }

            // Configure renderer
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = sortingOrder;
            sr.color = color;
            sr.flipX = flipX;
            sr.flipY = flipY;

            // Apply additional properties
            if (payload.ContainsKey("drawMode"))
            {
                sr.drawMode = ParseDrawMode(GetString(payload, "drawMode"));
            }

            if (payload.ContainsKey("maskInteraction"))
            {
                sr.maskInteraction = ParseMaskInteraction(GetString(payload, "maskInteraction"));
            }

            EditorSceneManager.MarkSceneDirty(go.scene);

            return CreateSuccessResponse(
                ("gameObjectPath", BuildGameObjectPath(go)),
                ("instanceID", go.GetInstanceID()),
                ("spriteRendererID", sr.GetInstanceID())
            );
        }

        #endregion

        #region Update Sprite

        private object UpdateSprite(Dictionary<string, object> payload)
        {
            var go = ResolveGameObjectFromPayload(payload);
            var sr = go.GetComponent<SpriteRenderer>();

            if (sr == null)
            {
                throw new InvalidOperationException($"GameObject '{BuildGameObjectPath(go)}' does not have a SpriteRenderer");
            }

            Undo.RecordObject(sr, "Update SpriteRenderer");

            // Update sprite
            if (payload.ContainsKey("spritePath"))
            {
                var spritePath = GetString(payload, "spritePath");
                if (!string.IsNullOrEmpty(spritePath))
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if (sprite != null)
                    {
                        sr.sprite = sprite;
                    }
                }
                else
                {
                    sr.sprite = null;
                }
            }

            // Update sorting
            if (payload.ContainsKey("sortingLayerName"))
            {
                sr.sortingLayerName = GetString(payload, "sortingLayerName");
            }

            if (payload.ContainsKey("sortingOrder"))
            {
                sr.sortingOrder = GetInt(payload, "sortingOrder", 0);
            }

            // Update appearance
            if (payload.ContainsKey("color"))
            {
                sr.color = GetColor(payload, "color", Color.white);
            }

            if (payload.ContainsKey("flipX"))
            {
                sr.flipX = GetBool(payload, "flipX", false);
            }

            if (payload.ContainsKey("flipY"))
            {
                sr.flipY = GetBool(payload, "flipY", false);
            }

            // Update draw mode
            if (payload.ContainsKey("drawMode"))
            {
                sr.drawMode = ParseDrawMode(GetString(payload, "drawMode"));
            }

            if (payload.ContainsKey("size") && sr.drawMode != SpriteDrawMode.Simple)
            {
                sr.size = GetVector2(payload, "size", sr.size);
            }

            // Update mask interaction
            if (payload.ContainsKey("maskInteraction"))
            {
                sr.maskInteraction = ParseMaskInteraction(GetString(payload, "maskInteraction"));
            }

            // Update material
            if (payload.ContainsKey("materialPath"))
            {
                var materialPath = GetString(payload, "materialPath");
                if (!string.IsNullOrEmpty(materialPath))
                {
                    var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (material != null)
                    {
                        sr.material = material;
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(go.scene);

            return CreateSuccessResponse(
                ("gameObjectPath", BuildGameObjectPath(go)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectSprite(Dictionary<string, object> payload)
        {
            var go = ResolveGameObjectFromPayload(payload);
            var sr = go.GetComponent<SpriteRenderer>();

            if (sr == null)
            {
                throw new InvalidOperationException($"GameObject '{BuildGameObjectPath(go)}' does not have a SpriteRenderer");
            }

            var info = new Dictionary<string, object>
            {
                ["success"] = true,
                ["gameObjectPath"] = BuildGameObjectPath(go),
                ["sprite"] = sr.sprite != null ? AssetDatabase.GetAssetPath(sr.sprite) : null,
                ["spriteName"] = sr.sprite != null ? sr.sprite.name : null,
                ["sortingLayerName"] = sr.sortingLayerName,
                ["sortingLayerID"] = sr.sortingLayerID,
                ["sortingOrder"] = sr.sortingOrder,
                ["color"] = new Dictionary<string, object>
                {
                    ["r"] = sr.color.r,
                    ["g"] = sr.color.g,
                    ["b"] = sr.color.b,
                    ["a"] = sr.color.a
                },
                ["flipX"] = sr.flipX,
                ["flipY"] = sr.flipY,
                ["drawMode"] = sr.drawMode.ToString(),
                ["size"] = new Dictionary<string, object> { ["x"] = sr.size.x, ["y"] = sr.size.y },
                ["maskInteraction"] = sr.maskInteraction.ToString(),
                ["bounds"] = new Dictionary<string, object>
                {
                    ["center"] = new Dictionary<string, object>
                    {
                        ["x"] = sr.bounds.center.x,
                        ["y"] = sr.bounds.center.y,
                        ["z"] = sr.bounds.center.z
                    },
                    ["size"] = new Dictionary<string, object>
                    {
                        ["x"] = sr.bounds.size.x,
                        ["y"] = sr.bounds.size.y,
                        ["z"] = sr.bounds.size.z
                    }
                }
            };

            return info;
        }

        #endregion

        #region Batch Operations

        private object UpdateMultiple(Dictionary<string, object> payload)
        {
            var pattern = GetString(payload, "pattern");
            var useRegex = GetBool(payload, "useRegex", false);
            var maxResults = GetInt(payload, "maxResults", 1000);

            if (string.IsNullOrEmpty(pattern))
            {
                throw new InvalidOperationException("pattern parameter is required");
            }

            var gameObjects = FindGameObjectsByPattern(pattern, useRegex, maxResults).ToList();
            var results = new List<Dictionary<string, object>>();
            var successCount = 0;
            var failureCount = 0;

            foreach (var go in gameObjects)
            {
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    failureCount++;
                    results.Add(new Dictionary<string, object>
                    {
                        ["success"] = false,
                        ["gameObjectPath"] = BuildGameObjectPath(go),
                        ["error"] = "No SpriteRenderer component"
                    });
                    continue;
                }

                try
                {
                    Undo.RecordObject(sr, "Update SpriteRenderer Batch");

                    if (payload.ContainsKey("sortingLayerName"))
                    {
                        sr.sortingLayerName = GetString(payload, "sortingLayerName");
                    }

                    if (payload.ContainsKey("sortingOrder"))
                    {
                        sr.sortingOrder = GetInt(payload, "sortingOrder", 0);
                    }

                    if (payload.ContainsKey("color"))
                    {
                        sr.color = GetColor(payload, "color", Color.white);
                    }

                    if (payload.ContainsKey("flipX"))
                    {
                        sr.flipX = GetBool(payload, "flipX", false);
                    }

                    if (payload.ContainsKey("flipY"))
                    {
                        sr.flipY = GetBool(payload, "flipY", false);
                    }

                    successCount++;
                    results.Add(new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["gameObjectPath"] = BuildGameObjectPath(go)
                    });
                }
                catch (Exception ex)
                {
                    failureCount++;
                    results.Add(new Dictionary<string, object>
                    {
                        ["success"] = false,
                        ["gameObjectPath"] = BuildGameObjectPath(go),
                        ["error"] = ex.Message
                    });
                }
            }

            MarkScenesDirty(gameObjects);

            return CreateSuccessResponse(
                ("results", results),
                ("processed", gameObjects.Count),
                ("succeeded", successCount),
                ("failed", failureCount)
            );
        }

        private object SetSortingLayer(Dictionary<string, object> payload)
        {
            var targets = GetTargetGameObjects(payload);
            var sortingLayerName = GetString(payload, "sortingLayerName") ?? "Default";
            var sortingOrder = payload.ContainsKey("sortingOrder") ? GetInt(payload, "sortingOrder", 0) : (int?)null;

            var updated = new List<string>();

            foreach (var go in targets)
            {
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Undo.RecordObject(sr, "Set Sorting Layer");
                    sr.sortingLayerName = sortingLayerName;
                    if (sortingOrder.HasValue)
                    {
                        sr.sortingOrder = sortingOrder.Value;
                    }
                    updated.Add(BuildGameObjectPath(go));
                }
            }

            MarkScenesDirty(targets);

            return CreateSuccessResponse(
                ("updated", updated),
                ("count", updated.Count)
            );
        }

        private object SetColor(Dictionary<string, object> payload)
        {
            var targets = GetTargetGameObjects(payload);
            var color = GetColor(payload, "color", Color.white);

            var updated = new List<string>();

            foreach (var go in targets)
            {
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Undo.RecordObject(sr, "Set Sprite Color");
                    sr.color = color;
                    updated.Add(BuildGameObjectPath(go));
                }
            }

            MarkScenesDirty(targets);

            return CreateSuccessResponse(
                ("updated", updated),
                ("count", updated.Count)
            );
        }

        #endregion

        #region Sprite Asset Operations

        private object SliceSpriteSheet(Dictionary<string, object> payload)
        {
            var texturePath = GetString(payload, "texturePath");
            var sliceMode = GetString(payload, "sliceMode")?.ToLowerInvariant() ?? "grid";
            var cellSizeX = GetInt(payload, "cellSizeX", 32);
            var cellSizeY = GetInt(payload, "cellSizeY", 32);
            var pivot = GetVector2(payload, "pivot", new Vector2(0.5f, 0.5f));
            var pixelsPerUnit = GetFloat(payload, "pixelsPerUnit", 100f);

            if (string.IsNullOrEmpty(texturePath))
            {
                throw new InvalidOperationException("texturePath parameter is required");
            }

            // Get texture importer
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not get TextureImporter for: {texturePath}");
            }

            // Configure for sprite
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = pixelsPerUnit;

            // Load texture to get dimensions
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                throw new InvalidOperationException($"Could not load texture at: {texturePath}");
            }

            var spriteRects = new List<SpriteMetaData>();

            if (sliceMode == "grid")
            {
                // Grid-based slicing
                var cols = texture.width / cellSizeX;
                var rows = texture.height / cellSizeY;
                var index = 0;

                for (var row = rows - 1; row >= 0; row--)
                {
                    for (var col = 0; col < cols; col++)
                    {
                        var rect = new Rect(col * cellSizeX, row * cellSizeY, cellSizeX, cellSizeY);
                        spriteRects.Add(new SpriteMetaData
                        {
                            name = $"{System.IO.Path.GetFileNameWithoutExtension(texturePath)}_{index}",
                            rect = rect,
                            pivot = pivot,
                            alignment = (int)SpriteAlignment.Custom
                        });
                        index++;
                    }
                }
            }
            else if (sliceMode == "automatic")
            {
                // For automatic slicing, we use Unity's built-in algorithm
                // This requires reimporting with SpriteImportMode.Multiple and letting Unity detect
                importer.spriteImportMode = SpriteImportMode.Multiple;
                // Unity will auto-detect sprites on reimport
            }

            // Apply sprite sheet data
            if (spriteRects.Count > 0)
            {
                #pragma warning disable CS0618 // Type or member is obsolete
                importer.spritesheet = spriteRects.ToArray();
                #pragma warning restore CS0618
            }

            // Reimport
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            // Get resulting sprites
            var sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
                .OfType<Sprite>()
                .Select(s => s.name)
                .ToList();

            return CreateSuccessResponse(
                ("texturePath", texturePath),
                ("sliceMode", sliceMode),
                ("spriteCount", sprites.Count),
                ("spriteNames", sprites)
            );
        }

        private object CreateSpriteAtlas(Dictionary<string, object> payload)
        {
            var atlasPath = GetString(payload, "atlasPath");
            var spritePaths = GetStringList(payload, "spritePaths");

            if (string.IsNullOrEmpty(atlasPath))
            {
                throw new InvalidOperationException("atlasPath parameter is required");
            }

            if (!atlasPath.EndsWith(".spriteatlas"))
            {
                atlasPath += ".spriteatlas";
            }

            // Ensure directory exists
            var directory = System.IO.Path.GetDirectoryName(atlasPath);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // Create SpriteAtlas
            var atlas = new UnityEngine.U2D.SpriteAtlas();

            // Add sprites or folders
            var addedObjects = new List<UnityEngine.Object>();
            foreach (var path in spritePaths)
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (obj != null)
                {
                    addedObjects.Add(obj);
                }
            }

            if (addedObjects.Count > 0)
            {
                atlas.Add(addedObjects.ToArray());
            }

            // Configure atlas settings
            var packingSettings = atlas.GetPackingSettings();
            packingSettings.enableRotation = false;
            packingSettings.enableTightPacking = true;
            packingSettings.padding = 2;
            atlas.SetPackingSettings(packingSettings);

            var textureSettings = atlas.GetTextureSettings();
            textureSettings.filterMode = FilterMode.Bilinear;
            textureSettings.generateMipMaps = false;
            atlas.SetTextureSettings(textureSettings);

            // Save atlas
            AssetDatabase.CreateAsset(atlas, atlasPath);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("atlasPath", atlasPath),
                ("spriteCount", addedObjects.Count)
            );
        }

        #endregion

        #region Helper Methods

        private List<GameObject> GetTargetGameObjects(Dictionary<string, object> payload)
        {
            // Single target
            if (payload.ContainsKey("gameObjectPath") || payload.ContainsKey("gameObjectGlobalObjectId"))
            {
                return new List<GameObject> { ResolveGameObjectFromPayload(payload) };
            }

            // Multiple targets by paths
            if (payload.ContainsKey("gameObjectPaths"))
            {
                var paths = GetStringList(payload, "gameObjectPaths");
                return paths.Select(GameObject.Find).Where(go => go != null).ToList();
            }

            // Pattern matching
            if (payload.ContainsKey("pattern"))
            {
                var pattern = GetString(payload, "pattern");
                var useRegex = GetBool(payload, "useRegex", false);
                var maxResults = GetInt(payload, "maxResults", 1000);
                return FindGameObjectsByPattern(pattern, useRegex, maxResults).ToList();
            }

            throw new InvalidOperationException("No target GameObjects specified");
        }

        private void MarkScenesDirty(IEnumerable<GameObject> gameObjects)
        {
            var scenes = gameObjects.Select(go => go.scene).Distinct();
            foreach (var scene in scenes)
            {
                if (scene.IsValid())
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }
        }

        private SpriteDrawMode ParseDrawMode(string value)
        {
            if (string.IsNullOrEmpty(value)) return SpriteDrawMode.Simple;

            return value.ToLowerInvariant() switch
            {
                "simple" => SpriteDrawMode.Simple,
                "sliced" => SpriteDrawMode.Sliced,
                "tiled" => SpriteDrawMode.Tiled,
                _ => SpriteDrawMode.Simple
            };
        }

        private SpriteMaskInteraction ParseMaskInteraction(string value)
        {
            if (string.IsNullOrEmpty(value)) return SpriteMaskInteraction.None;

            return value.ToLowerInvariant() switch
            {
                "none" => SpriteMaskInteraction.None,
                "visibleinsidemask" => SpriteMaskInteraction.VisibleInsideMask,
                "visibleoutsidemask" => SpriteMaskInteraction.VisibleOutsideMask,
                _ => SpriteMaskInteraction.None
            };
        }

        private Color GetColor(Dictionary<string, object> payload, string key, Color defaultValue)
        {
            if (!payload.TryGetValue(key, out var value)) return defaultValue;

            if (value is Dictionary<string, object> dict)
            {
                return new Color(
                    dict.TryGetValue("r", out var r) ? Convert.ToSingle(r) : defaultValue.r,
                    dict.TryGetValue("g", out var g) ? Convert.ToSingle(g) : defaultValue.g,
                    dict.TryGetValue("b", out var b) ? Convert.ToSingle(b) : defaultValue.b,
                    dict.TryGetValue("a", out var a) ? Convert.ToSingle(a) : defaultValue.a
                );
            }

            return defaultValue;
        }

        private Vector3 GetVector3(Dictionary<string, object> payload, string key, Vector3 defaultValue)
        {
            if (!payload.TryGetValue(key, out var value)) return defaultValue;

            if (value is Dictionary<string, object> dict)
            {
                return new Vector3(
                    dict.TryGetValue("x", out var x) ? Convert.ToSingle(x) : defaultValue.x,
                    dict.TryGetValue("y", out var y) ? Convert.ToSingle(y) : defaultValue.y,
                    dict.TryGetValue("z", out var z) ? Convert.ToSingle(z) : defaultValue.z
                );
            }

            return defaultValue;
        }

        private Vector2 GetVector2(Dictionary<string, object> payload, string key, Vector2 defaultValue)
        {
            if (!payload.TryGetValue(key, out var value)) return defaultValue;

            if (value is Dictionary<string, object> dict)
            {
                return new Vector2(
                    dict.TryGetValue("x", out var x) ? Convert.ToSingle(x) : defaultValue.x,
                    dict.TryGetValue("y", out var y) ? Convert.ToSingle(y) : defaultValue.y
                );
            }

            return defaultValue;
        }

        private List<string> GetStringList(Dictionary<string, object> payload, string key)
        {
            if (!payload.TryGetValue(key, out var value)) return new List<string>();

            if (value is List<object> list)
            {
                return list.Select(o => o?.ToString()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            }

            return new List<string>();
        }

        private string BuildGameObjectPath(GameObject go)
        {
            var path = go.name;
            var current = go.transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        #endregion
    }
}
