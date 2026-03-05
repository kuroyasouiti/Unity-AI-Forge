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

        #endregion
    }
}
