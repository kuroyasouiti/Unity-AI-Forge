using System;
using System.Collections.Generic;
using System.IO;
using MCP.Editor.Base;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Material Bundle Handler: Creates and configures materials with presets.
    /// Supports Standard, URP, and HDRP render pipelines.
    /// </summary>
    public class MaterialBundleHandler : BaseCommandHandler
    {
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "create",
            "update",
            "setTexture",
            "setColor",
            "applyPreset",
            "inspect",
            "applyToObjects",
            "delete",
            "duplicate",
            "listPresets"
        };

        public override string Category => "MaterialBundle";
        public override string Version => "1.0.0";

        // Render pipeline detection
        private enum RenderPipeline { Standard, URP, HDRP }

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => HandleCreate(payload),
                "update" => HandleUpdate(payload),
                "setTexture" => HandleSetTexture(payload),
                "setColor" => HandleSetColor(payload),
                "applyPreset" => HandleApplyPreset(payload),
                "inspect" => HandleInspect(payload),
                "applyToObjects" => HandleApplyToObjects(payload),
                "delete" => HandleDelete(payload),
                "duplicate" => HandleDuplicate(payload),
                "listPresets" => HandleListPresets(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }

        /// <summary>
        /// Create a new material.
        /// </summary>
        private object HandleCreate(Dictionary<string, object> payload)
        {
            string name = GetString(payload, "name", "NewMaterial");
            string savePath = GetString(payload, "savePath");
            string preset = GetString(payload, "preset", "lit");

            if (string.IsNullOrEmpty(savePath))
            {
                savePath = $"Assets/Materials/{name}.mat";
            }

            // Normalize path separators for Unity
            savePath = savePath.Replace("\\", "/");
            string directory = Path.GetDirectoryName(savePath)?.Replace("\\", "/");

            // Ensure Unity asset folder exists using AssetDatabase (this creates both Unity metadata and physical folders)
            if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
            {
                CreateFolderRecursive(directory);
                // Force refresh to ensure folder is registered
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            // Also ensure physical directory exists (belt and suspenders approach)
            // Only replace the leading "Assets" prefix, not any other occurrence (e.g., "TestAssets")
            string physicalDirectory = null;
            if (!string.IsNullOrEmpty(directory) && directory.StartsWith("Assets"))
            {
                physicalDirectory = Application.dataPath + directory.Substring("Assets".Length);
            }
            if (!string.IsNullOrEmpty(physicalDirectory) && !Directory.Exists(physicalDirectory))
            {
                Directory.CreateDirectory(physicalDirectory);
            }

            // Get appropriate shader based on render pipeline and preset
            Shader shader = GetShaderForPreset(preset);
            if (shader == null)
            {
                // Last resort: create a basic shader programmatically isn't possible,
                // so we must have at least one shader. Log detailed error.
                var rp = DetectRenderPipeline();
                return CreateFailureResponse($"Could not find any shader for preset '{preset}'. RenderPipeline: {rp}. " +
                    "Ensure project has Standard shader or URP package installed.");
            }

            // Create material
            Material material = new Material(shader);
            material.name = name;

            // Apply preset settings
            ApplyPresetSettings(material, preset, payload);

            // Apply custom properties if provided
            if (payload.ContainsKey("properties"))
            {
                ApplyProperties(material, payload["properties"] as Dictionary<string, object>);
            }

            // Create the asset
            AssetDatabase.CreateAsset(material, savePath);

            // Mark dirty and save
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("message", $"Material '{name}' created"),
                ("assetPath", savePath),
                ("shader", shader.name),
                ("preset", preset),
                ("renderPipeline", DetectRenderPipeline().ToString())
            );
        }

        /// <summary>
        /// Update an existing material.
        /// </summary>
        private object HandleUpdate(Dictionary<string, object> payload)
        {
            string assetPath = GetString(payload, "assetPath");
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("'assetPath' is required");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                return CreateFailureResponse($"Material not found at '{assetPath}'");
            }

            // Apply properties
            if (payload.ContainsKey("properties"))
            {
                ApplyProperties(material, payload["properties"] as Dictionary<string, object>);
            }

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("message", $"Material updated"),
                ("assetPath", assetPath)
            );
        }

        /// <summary>
        /// Set texture on material.
        /// </summary>
        private object HandleSetTexture(Dictionary<string, object> payload)
        {
            string assetPath = GetString(payload, "assetPath");
            string propertyName = GetString(payload, "propertyName", "_MainTex");
            string texturePath = GetString(payload, "texturePath");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                return CreateFailureResponse($"Material not found at '{assetPath}'");
            }

            Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
            if (texture == null)
            {
                return CreateFailureResponse($"Texture not found at '{texturePath}'");
            }

            // Handle common property name aliases
            string actualPropertyName = GetActualPropertyName(material, propertyName);
            material.SetTexture(actualPropertyName, texture);

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("message", $"Texture set on {actualPropertyName}"),
                ("assetPath", assetPath),
                ("texturePath", texturePath)
            );
        }

        /// <summary>
        /// Set color on material.
        /// </summary>
        private object HandleSetColor(Dictionary<string, object> payload)
        {
            string assetPath = GetString(payload, "assetPath");
            string propertyName = GetString(payload, "propertyName", "_Color");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                return CreateFailureResponse($"Material not found at '{assetPath}'");
            }

            Color color = ParseColor(payload);
            string actualPropertyName = GetActualPropertyName(material, propertyName);
            material.SetColor(actualPropertyName, color);

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("message", $"Color set on {actualPropertyName}"),
                ("assetPath", assetPath),
                ("color", new Dictionary<string, float>
                {
                    ["r"] = color.r,
                    ["g"] = color.g,
                    ["b"] = color.b,
                    ["a"] = color.a
                })
            );
        }

        /// <summary>
        /// Apply preset to material.
        /// </summary>
        private object HandleApplyPreset(Dictionary<string, object> payload)
        {
            string assetPath = GetString(payload, "assetPath");
            string preset = GetString(payload, "preset");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                return CreateFailureResponse($"Material not found at '{assetPath}'");
            }

            // Change shader if needed
            Shader shader = GetShaderForPreset(preset);
            if (shader != null && material.shader != shader)
            {
                material.shader = shader;
            }

            ApplyPresetSettings(material, preset, payload);

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("message", $"Preset '{preset}' applied"),
                ("assetPath", assetPath),
                ("shader", material.shader.name)
            );
        }

        /// <summary>
        /// Inspect material.
        /// </summary>
        private object HandleInspect(Dictionary<string, object> payload)
        {
            string assetPath = GetString(payload, "assetPath");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                return CreateFailureResponse($"Material not found at '{assetPath}'");
            }

            var properties = new List<Dictionary<string, object>>();
            Shader shader = material.shader;

            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                string propName = shader.GetPropertyName(i);
                var propType = shader.GetPropertyType(i);

                var propInfo = new Dictionary<string, object>
                {
                    ["name"] = propName,
                    ["type"] = propType.ToString()
                };

                switch (propType)
                {
                    case ShaderPropertyType.Color:
                        var color = material.GetColor(propName);
                        propInfo["value"] = new Dictionary<string, float>
                        {
                            ["r"] = color.r,
                            ["g"] = color.g,
                            ["b"] = color.b,
                            ["a"] = color.a
                        };
                        break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        propInfo["value"] = material.GetFloat(propName);
                        break;
                    case ShaderPropertyType.Texture:
                        var tex = material.GetTexture(propName);
                        propInfo["value"] = tex != null ? AssetDatabase.GetAssetPath(tex) : null;
                        break;
                    case ShaderPropertyType.Vector:
                        var vec = material.GetVector(propName);
                        propInfo["value"] = new Dictionary<string, float>
                        {
                            ["x"] = vec.x,
                            ["y"] = vec.y,
                            ["z"] = vec.z,
                            ["w"] = vec.w
                        };
                        break;
                }

                properties.Add(propInfo);
            }

            return CreateSuccessResponse(
                ("assetPath", assetPath),
                ("name", material.name),
                ("shader", shader.name),
                ("renderQueue", material.renderQueue),
                ("renderPipeline", DetectRenderPipeline().ToString()),
                ("properties", properties)
            );
        }

        /// <summary>
        /// Apply material to multiple objects.
        /// </summary>
        private object HandleApplyToObjects(Dictionary<string, object> payload)
        {
            string assetPath = GetString(payload, "assetPath");
            string pattern = GetString(payload, "pattern");
            int maxResults = GetInt(payload, "maxResults", 100);

            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                return CreateFailureResponse($"Material not found at '{assetPath}'");
            }

            var gameObjects = FindGameObjectsByPattern(pattern, false, maxResults);
            int appliedCount = 0;

            foreach (var go in gameObjects)
            {
                var renderers = go.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    renderer.sharedMaterial = material;
                    appliedCount++;
                }
            }

            return CreateSuccessResponse(
                ("message", $"Material applied to {appliedCount} renderers"),
                ("assetPath", assetPath),
                ("appliedCount", appliedCount)
            );
        }

        /// <summary>
        /// Delete material.
        /// </summary>
        private object HandleDelete(Dictionary<string, object> payload)
        {
            string assetPath = GetString(payload, "assetPath");

            if (!AssetDatabase.DeleteAsset(assetPath))
            {
                return CreateFailureResponse($"Failed to delete material at '{assetPath}'");
            }

            return CreateSuccessResponse(
                ("message", $"Material deleted"),
                ("assetPath", assetPath)
            );
        }

        /// <summary>
        /// Duplicate material.
        /// </summary>
        private object HandleDuplicate(Dictionary<string, object> payload)
        {
            string sourcePath = GetString(payload, "assetPath");
            string destPath = GetString(payload, "destinationPath");

            Material source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
            if (source == null)
            {
                return CreateFailureResponse($"Material not found at '{sourcePath}'");
            }

            if (!AssetDatabase.CopyAsset(sourcePath, destPath))
            {
                return CreateFailureResponse($"Failed to duplicate material to '{destPath}'");
            }

            return CreateSuccessResponse(
                ("message", $"Material duplicated"),
                ("sourcePath", sourcePath),
                ("destinationPath", destPath)
            );
        }

        /// <summary>
        /// List available presets.
        /// </summary>
        private object HandleListPresets(Dictionary<string, object> payload)
        {
            var presets = new List<Dictionary<string, object>>
            {
                new() { ["name"] = "unlit", ["description"] = "No lighting, flat color", ["pipelines"] = "All" },
                new() { ["name"] = "lit", ["description"] = "Standard PBR lighting", ["pipelines"] = "All" },
                new() { ["name"] = "transparent", ["description"] = "Transparent material", ["pipelines"] = "All" },
                new() { ["name"] = "cutout", ["description"] = "Cutout/alpha test", ["pipelines"] = "All" },
                new() { ["name"] = "fade", ["description"] = "Fade transparency", ["pipelines"] = "All" },
                new() { ["name"] = "sprite", ["description"] = "Sprite rendering", ["pipelines"] = "All" },
                new() { ["name"] = "ui", ["description"] = "UI elements", ["pipelines"] = "All" },
                new() { ["name"] = "emissive", ["description"] = "Self-illuminating", ["pipelines"] = "All" },
                new() { ["name"] = "metallic", ["description"] = "Metallic surface", ["pipelines"] = "All" },
                new() { ["name"] = "glass", ["description"] = "Glass-like material", ["pipelines"] = "All" }
            };

            return CreateSuccessResponse(
                ("presets", presets),
                ("renderPipeline", DetectRenderPipeline().ToString())
            );
        }

        #region Helper Methods

        private RenderPipeline DetectRenderPipeline()
        {
            var currentRP = GraphicsSettings.currentRenderPipeline;
            if (currentRP == null)
            {
                return RenderPipeline.Standard;
            }

            string rpName = currentRP.GetType().Name;
            if (rpName.Contains("Universal") || rpName.Contains("URP"))
            {
                return RenderPipeline.URP;
            }
            if (rpName.Contains("HD") || rpName.Contains("HDRP"))
            {
                return RenderPipeline.HDRP;
            }

            return RenderPipeline.Standard;
        }

        private Shader GetShaderForPreset(string preset)
        {
            var rp = DetectRenderPipeline();

            Shader shader = preset.ToLower() switch
            {
                "unlit" => rp switch
                {
                    RenderPipeline.URP => Shader.Find("Universal Render Pipeline/Unlit"),
                    RenderPipeline.HDRP => Shader.Find("HDRP/Unlit"),
                    _ => Shader.Find("Unlit/Color")
                },
                "lit" or "standard" => rp switch
                {
                    RenderPipeline.URP => Shader.Find("Universal Render Pipeline/Lit"),
                    RenderPipeline.HDRP => Shader.Find("HDRP/Lit"),
                    _ => Shader.Find("Standard")
                },
                "transparent" or "fade" => rp switch
                {
                    RenderPipeline.URP => Shader.Find("Universal Render Pipeline/Lit"),
                    RenderPipeline.HDRP => Shader.Find("HDRP/Lit"),
                    _ => Shader.Find("Standard")
                },
                "cutout" => rp switch
                {
                    RenderPipeline.URP => Shader.Find("Universal Render Pipeline/Lit"),
                    _ => Shader.Find("Standard")
                },
                "sprite" => rp switch
                {
                    RenderPipeline.URP => Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"),
                    _ => Shader.Find("Sprites/Default")
                },
                "ui" => Shader.Find("UI/Default"),
                "emissive" or "metallic" or "glass" => rp switch
                {
                    RenderPipeline.URP => Shader.Find("Universal Render Pipeline/Lit"),
                    RenderPipeline.HDRP => Shader.Find("HDRP/Lit"),
                    _ => Shader.Find("Standard")
                },
                _ => rp switch
                {
                    RenderPipeline.URP => Shader.Find("Universal Render Pipeline/Lit"),
                    RenderPipeline.HDRP => Shader.Find("HDRP/Lit"),
                    _ => Shader.Find("Standard")
                }
            };

            // Fallback chain if primary shader not found
            if (shader == null)
            {
                // Try common fallback shaders that should always exist
                shader = Shader.Find("Standard")
                    ?? Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Unlit/Color")
                    ?? Shader.Find("UI/Default")
                    ?? Shader.Find("Sprites/Default")
                    ?? Shader.Find("Hidden/InternalErrorShader");
            }

            return shader;
        }

        private void ApplyPresetSettings(Material material, string preset, Dictionary<string, object> payload)
        {
            var rp = DetectRenderPipeline();

            switch (preset.ToLower())
            {
                case "transparent":
                case "fade":
                    SetupTransparentMaterial(material, rp);
                    break;
                case "cutout":
                    SetupCutoutMaterial(material, rp);
                    break;
                case "emissive":
                    SetupEmissiveMaterial(material, rp, payload);
                    break;
                case "metallic":
                    SetupMetallicMaterial(material, rp);
                    break;
                case "glass":
                    SetupGlassMaterial(material, rp);
                    break;
            }
        }

        private void SetupTransparentMaterial(Material material, RenderPipeline rp)
        {
            switch (rp)
            {
                case RenderPipeline.URP:
                    material.SetFloat("_Surface", 1); // Transparent
                    material.SetFloat("_Blend", 0); // Alpha
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.renderQueue = (int)RenderQueue.Transparent;
                    break;
                case RenderPipeline.Standard:
                    material.SetFloat("_Mode", 3); // Transparent
                    material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)RenderQueue.Transparent;
                    break;
            }
        }

        private void SetupCutoutMaterial(Material material, RenderPipeline rp)
        {
            switch (rp)
            {
                case RenderPipeline.URP:
                    material.SetFloat("_AlphaClip", 1);
                    material.SetFloat("_Cutoff", 0.5f);
                    material.EnableKeyword("_ALPHATEST_ON");
                    break;
                case RenderPipeline.Standard:
                    material.SetFloat("_Mode", 1); // Cutout
                    material.SetFloat("_Cutoff", 0.5f);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.renderQueue = (int)RenderQueue.AlphaTest;
                    break;
            }
        }

        private void SetupEmissiveMaterial(Material material, RenderPipeline rp, Dictionary<string, object> payload)
        {
            Color emissionColor = Color.white;
            float intensity = GetFloat(payload, "emissionIntensity", 1f);

            switch (rp)
            {
                case RenderPipeline.URP:
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", emissionColor * intensity);
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    break;
                case RenderPipeline.Standard:
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", emissionColor * intensity);
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    break;
            }
        }

        private void SetupMetallicMaterial(Material material, RenderPipeline rp)
        {
            switch (rp)
            {
                case RenderPipeline.URP:
                    material.SetFloat("_Metallic", 1f);
                    material.SetFloat("_Smoothness", 0.8f);
                    break;
                case RenderPipeline.Standard:
                    material.SetFloat("_Metallic", 1f);
                    material.SetFloat("_Glossiness", 0.8f);
                    break;
            }
        }

        private void SetupGlassMaterial(Material material, RenderPipeline rp)
        {
            SetupTransparentMaterial(material, rp);
            material.SetColor("_Color", new Color(1, 1, 1, 0.3f));

            switch (rp)
            {
                case RenderPipeline.URP:
                    material.SetFloat("_Metallic", 0f);
                    material.SetFloat("_Smoothness", 1f);
                    break;
                case RenderPipeline.Standard:
                    material.SetFloat("_Metallic", 0f);
                    material.SetFloat("_Glossiness", 1f);
                    break;
            }
        }

        private void ApplyProperties(Material material, Dictionary<string, object> properties)
        {
            if (properties == null) return;

            foreach (var kvp in properties)
            {
                string propName = GetActualPropertyName(material, kvp.Key);

                if (kvp.Value is Dictionary<string, object> dictValue)
                {
                    // Color or Vector
                    if (dictValue.ContainsKey("r"))
                    {
                        var color = new Color(
                            Convert.ToSingle(dictValue.GetValueOrDefault("r", 1f)),
                            Convert.ToSingle(dictValue.GetValueOrDefault("g", 1f)),
                            Convert.ToSingle(dictValue.GetValueOrDefault("b", 1f)),
                            Convert.ToSingle(dictValue.GetValueOrDefault("a", 1f))
                        );
                        material.SetColor(propName, color);
                    }
                    else if (dictValue.ContainsKey("x"))
                    {
                        var vector = new Vector4(
                            Convert.ToSingle(dictValue.GetValueOrDefault("x", 0f)),
                            Convert.ToSingle(dictValue.GetValueOrDefault("y", 0f)),
                            Convert.ToSingle(dictValue.GetValueOrDefault("z", 0f)),
                            Convert.ToSingle(dictValue.GetValueOrDefault("w", 0f))
                        );
                        material.SetVector(propName, vector);
                    }
                }
                else if (kvp.Value is string strValue)
                {
                    // Could be color hex or texture path
                    if (strValue.StartsWith("#"))
                    {
                        if (ColorUtility.TryParseHtmlString(strValue, out Color color))
                        {
                            material.SetColor(propName, color);
                        }
                    }
                    else if (strValue.StartsWith("Assets/"))
                    {
                        var texture = AssetDatabase.LoadAssetAtPath<Texture>(strValue);
                        if (texture != null)
                        {
                            material.SetTexture(propName, texture);
                        }
                    }
                }
                else if (kvp.Value is float floatValue)
                {
                    material.SetFloat(propName, floatValue);
                }
                else if (kvp.Value is double doubleValue)
                {
                    material.SetFloat(propName, (float)doubleValue);
                }
                else if (kvp.Value is int intValue)
                {
                    material.SetFloat(propName, intValue);
                }
                else if (kvp.Value is long longValue)
                {
                    material.SetFloat(propName, longValue);
                }
            }
        }

        private string GetActualPropertyName(Material material, string propertyName)
        {
            // Handle common aliases
            string alias = propertyName.ToLower() switch
            {
                "color" or "basecolor" => "_Color",
                "maintex" or "basetexture" or "albedo" => "_MainTex",
                "metallic" => "_Metallic",
                "smoothness" or "glossiness" => material.HasProperty("_Smoothness") ? "_Smoothness" : "_Glossiness",
                "emission" or "emissioncolor" => "_EmissionColor",
                "normal" or "normalmap" or "bumpmap" => "_BumpMap",
                _ => propertyName
            };

            // Check if property exists, fallback to URP names
            if (!material.HasProperty(alias))
            {
                alias = propertyName.ToLower() switch
                {
                    "color" or "basecolor" => "_BaseColor",
                    "maintex" or "basetexture" or "albedo" => "_BaseMap",
                    _ => alias
                };
            }

            return alias;
        }

        private Color ParseColor(Dictionary<string, object> payload)
        {
            if (payload.ContainsKey("color"))
            {
                var colorValue = payload["color"];
                if (colorValue is string hexColor)
                {
                    if (ColorUtility.TryParseHtmlString(hexColor, out Color parsed))
                    {
                        return parsed;
                    }
                }
                else if (colorValue is Dictionary<string, object> colorDict)
                {
                    return new Color(
                        Convert.ToSingle(colorDict.GetValueOrDefault("r", 1f)),
                        Convert.ToSingle(colorDict.GetValueOrDefault("g", 1f)),
                        Convert.ToSingle(colorDict.GetValueOrDefault("b", 1f)),
                        Convert.ToSingle(colorDict.GetValueOrDefault("a", 1f))
                    );
                }
            }

            // Individual color components
            return new Color(
                GetFloat(payload, "r", 1f),
                GetFloat(payload, "g", 1f),
                GetFloat(payload, "b", 1f),
                GetFloat(payload, "a", 1f)
            );
        }

        private void CreateFolderRecursive(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            var parentFolder = System.IO.Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
            var folderName = System.IO.Path.GetFileName(folderPath);

            if (!string.IsNullOrEmpty(parentFolder) && !AssetDatabase.IsValidFolder(parentFolder))
            {
                CreateFolderRecursive(parentFolder);
            }

            if (!string.IsNullOrEmpty(parentFolder) && !string.IsNullOrEmpty(folderName))
            {
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }

        #endregion
    }
}
