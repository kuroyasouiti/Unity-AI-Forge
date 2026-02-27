using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MCP.Editor.Base;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Handles vector to sprite conversion operations for rapid prototyping.
    /// Supports SVG import, primitive shape generation, and vector graphics conversion.
    /// </summary>
    public class VectorSpriteConvertHandler : BaseCommandHandler
    {
        #region ICommandHandler Implementation
        
        public override string Category => "vectorSpriteConvert";
        
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "primitiveToSprite",
            "svgToSprite",
            "textureToSprite",
            "createColorSprite"
        };
        
        #endregion
        
        #region BaseCommandHandler Overrides
        
        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            switch (operation)
            {
                case "primitiveToSprite":
                    return CreatePrimitiveSprite(payload);
                case "svgToSprite":
                    return ConvertSvgToSprite(payload);
                case "textureToSprite":
                    return ConvertTextureToSprite(payload);
                case "createColorSprite":
                    return CreateColorSprite(payload);
                default:
                    throw new ArgumentException($"Unknown operation: {operation}");
            }
        }
        
        #endregion
        
        #region Operation Methods

        /// <summary>
        /// Creates a sprite from primitive shapes (circle, square, triangle, polygon).
        /// Useful for rapid prototyping without external assets.
        /// </summary>
        private Dictionary<string, object> CreatePrimitiveSprite(Dictionary<string, object> payload)
        {
            string primitiveType = GetString(payload, "primitiveType", "square");
            int width = GetInt(payload, "width", 256);
            int height = GetInt(payload, "height", 256);
            Color color = GetColor(payload, "color", Color.white);
            string outputPath = GetString(payload, "outputPath", null);
            int sides = GetInt(payload, "sides", 6); // For polygon

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentException("outputPath is required");
            }

            // Validate output path
            if (!outputPath.StartsWith("Assets/"))
            {
                throw new ArgumentException("outputPath must start with 'Assets/'");
            }

            if (!outputPath.EndsWith(".png"))
            {
                outputPath += ".png";
            }

            // Ensure directory exists
            string directory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create texture
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];

            // Initialize all pixels as transparent
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            Vector2 center = new Vector2(width / 2f, height / 2f);

            // Draw primitive shape
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 point = new Vector2(x, y);
                    bool isInside = false;

                    switch (primitiveType.ToLower())
                    {
                        case "circle":
                            float radius = Mathf.Min(width, height) / 2f * 0.9f;
                            isInside = Vector2.Distance(point, center) <= radius;
                            break;

                        case "square":
                        case "rectangle":
                            float halfWidth = width * 0.45f;
                            float halfHeight = height * 0.45f;
                            isInside = Mathf.Abs(point.x - center.x) <= halfWidth &&
                                      Mathf.Abs(point.y - center.y) <= halfHeight;
                            break;

                        case "triangle":
                            // Equilateral triangle pointing up
                            float triRadius = Mathf.Min(width, height) / 2f * 0.9f;
                            Vector2 p1 = center + new Vector2(0, triRadius);
                            Vector2 p2 = center + new Vector2(-triRadius * 0.866f, -triRadius * 0.5f);
                            Vector2 p3 = center + new Vector2(triRadius * 0.866f, -triRadius * 0.5f);
                            isInside = IsPointInTriangle(point, p1, p2, p3);
                            break;

                        case "polygon":
                            float polyRadius = Mathf.Min(width, height) / 2f * 0.9f;
                            isInside = IsPointInPolygon(point, center, polyRadius, sides);
                            break;
                    }

                    if (isInside)
                    {
                        pixels[y * width + x] = color;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            // Save texture as PNG
            byte[] pngData = texture.EncodeToPNG();
            File.WriteAllBytes(outputPath, pngData);
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);

            // Configure as sprite
            TextureImporter importer = AssetImporter.GetAtPath(outputPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            // Load the sprite
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(outputPath);

            return new Dictionary<string, object>
            {
                { "success", true },
                { "spritePath", outputPath },
                { "primitiveType", primitiveType },
                { "size", new Dictionary<string, object> { { "width", width }, { "height", height } } },
                { "spriteGuid", AssetDatabase.AssetPathToGUID(outputPath) }
            };
        }

        /// <summary>
        /// Converts an SVG file to a sprite.
        /// Requires the SVG file to be already imported in the project.
        /// </summary>
        private Dictionary<string, object> ConvertSvgToSprite(Dictionary<string, object> payload)
        {
            string svgPath = GetString(payload, "svgPath", null);
            string outputPath = GetString(payload, "outputPath", null);
            int width = GetInt(payload, "width", 256);
            int height = GetInt(payload, "height", 256);

            if (string.IsNullOrEmpty(svgPath))
            {
                throw new ArgumentException("svgPath is required");
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentException("outputPath is required");
            }

            if (!File.Exists(svgPath))
            {
                throw new ArgumentException($"SVG file not found: {svgPath}");
            }

            // Unity 2021.2+ supports SVG import
            // Configure SVG importer
            TextureImporter importer = AssetImporter.GetAtPath(svgPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                
                // Set max texture size for SVG rasterization
                TextureImporterPlatformSettings settings = importer.GetDefaultPlatformTextureSettings();
                settings.maxTextureSize = Mathf.Max(width, height);
                importer.SetPlatformTextureSettings(settings);
                
                importer.SaveAndReimport();
            }

            // Load the sprite
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(svgPath);
            
            if (sprite == null)
            {
                throw new InvalidOperationException($"Failed to load sprite from SVG: {svgPath}");
            }

            // If output path is different, duplicate the asset
            if (svgPath != outputPath)
            {
                AssetDatabase.CopyAsset(svgPath, outputPath);
                AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
            }

            return new Dictionary<string, object>
            {
                { "success", true },
                { "spritePath", outputPath },
                { "svgSourcePath", svgPath },
                { "spriteGuid", AssetDatabase.AssetPathToGUID(outputPath) }
            };
        }

        /// <summary>
        /// Converts an existing texture to a sprite with proper import settings.
        /// </summary>
        private Dictionary<string, object> ConvertTextureToSprite(Dictionary<string, object> payload)
        {
            string texturePath = GetString(payload, "texturePath", null);

            if (string.IsNullOrEmpty(texturePath))
            {
                throw new ArgumentException("texturePath is required");
            }

            if (!File.Exists(texturePath))
            {
                throw new ArgumentException($"Texture file not found: {texturePath}");
            }

            // Configure texture as sprite
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                
                // Apply additional settings if provided
                if (payload.TryGetValue("pixelsPerUnit", out var ppuObj))
                {
                    importer.spritePixelsPerUnit = Convert.ToSingle(ppuObj);
                }
                
                if (payload.TryGetValue("filterMode", out var filterObj))
                {
                    string filterMode = filterObj.ToString();
                    switch (filterMode.ToLower())
                    {
                        case "point":
                            importer.filterMode = FilterMode.Point;
                            break;
                        case "bilinear":
                            importer.filterMode = FilterMode.Bilinear;
                            break;
                        case "trilinear":
                            importer.filterMode = FilterMode.Trilinear;
                            break;
                    }
                }
                
                importer.SaveAndReimport();
            }

            // Load the sprite
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            
            if (sprite == null)
            {
                throw new InvalidOperationException($"Failed to load sprite from texture: {texturePath}");
            }

            return new Dictionary<string, object>
            {
                { "success", true },
                { "spritePath", texturePath },
                { "spriteGuid", AssetDatabase.AssetPathToGUID(texturePath) }
            };
        }

        /// <summary>
        /// Creates a simple solid color sprite.
        /// Useful for UI prototyping and placeholders.
        /// </summary>
        private Dictionary<string, object> CreateColorSprite(Dictionary<string, object> payload)
        {
            int width = GetInt(payload, "width", 256);
            int height = GetInt(payload, "height", 256);
            Color color = GetColor(payload, "color", Color.white);
            string outputPath = GetString(payload, "outputPath", null);

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentException("outputPath is required");
            }

            if (!outputPath.StartsWith("Assets/"))
            {
                throw new ArgumentException("outputPath must start with 'Assets/'");
            }

            if (!outputPath.EndsWith(".png"))
            {
                outputPath += ".png";
            }

            // Ensure directory exists
            string directory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create solid color texture
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            
            texture.SetPixels(pixels);
            texture.Apply();

            // Save texture as PNG
            byte[] pngData = texture.EncodeToPNG();
            File.WriteAllBytes(outputPath, pngData);
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);

            // Configure as sprite
            TextureImporter importer = AssetImporter.GetAtPath(outputPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            return new Dictionary<string, object>
            {
                { "success", true },
                { "spritePath", outputPath },
                { "size", new Dictionary<string, object> { { "width", width }, { "height", height } } },
                { "color", ColorToDict(color) },
                { "spriteGuid", AssetDatabase.AssetPathToGUID(outputPath) }
            };
        }

        #region Helper Methods

        // Note: Using 'new' to explicitly hide inherited methods for local use
        private new string GetString(Dictionary<string, object> dict, string key, string defaultValue)
        {
            return dict.TryGetValue(key, out var value) && value != null ? value.ToString() : defaultValue;
        }

        private new int GetInt(Dictionary<string, object> dict, string key, int defaultValue)
        {
            if (dict.TryGetValue(key, out var value) && value != null)
            {
                try
                {
                    return Convert.ToInt32(value);
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        private Color GetColor(Dictionary<string, object> dict, string key, Color defaultValue)
        {
            if (!dict.TryGetValue(key, out var value) || value == null)
            {
                return defaultValue;
            }

            if (value is Dictionary<string, object> colorDict)
            {
                float r = GetFloat(colorDict, "r", defaultValue.r);
                float g = GetFloat(colorDict, "g", defaultValue.g);
                float b = GetFloat(colorDict, "b", defaultValue.b);
                float a = GetFloat(colorDict, "a", defaultValue.a);
                return new Color(r, g, b, a);
            }
            else if (value is string colorString)
            {
                // Support hex color strings
                if (ColorUtility.TryParseHtmlString(colorString, out Color parsedColor))
                {
                    return parsedColor;
                }
            }

            return defaultValue;
        }

        private new float GetFloat(Dictionary<string, object> dict, string key, float defaultValue)
        {
            if (dict.TryGetValue(key, out var value) && value != null)
            {
                try
                {
                    return Convert.ToSingle(value);
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        private Dictionary<string, object> ColorToDict(Color color)
        {
            return new Dictionary<string, object>
            {
                { "r", color.r },
                { "g", color.g },
                { "b", color.b },
                { "a", color.a }
            };
        }

        private bool IsPointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            float d1 = Sign(pt, v1, v2);
            float d2 = Sign(pt, v2, v3);
            float d3 = Sign(pt, v3, v1);

            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(hasNeg && hasPos);
        }

        private float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        private bool IsPointInPolygon(Vector2 point, Vector2 center, float radius, int sides)
        {
            float angleStep = 360f / sides;
            
            for (int i = 0; i < sides; i++)
            {
                float angle1 = i * angleStep * Mathf.Deg2Rad;
                float angle2 = ((i + 1) % sides) * angleStep * Mathf.Deg2Rad;
                
                Vector2 v1 = center + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * radius;
                Vector2 v2 = center + new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2)) * radius;
                
                if (Sign(point, v1, v2) * Sign(center, v1, v2) < 0)
                {
                    return false;
                }
            }
            
            return true;
        }

        #endregion
        
        #endregion
    }
}

