using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Light Bundle Handler: Creates and configures lights with presets.
    /// Supports complete lighting setups for various scenarios.
    /// </summary>
    public class LightBundleHandler : BaseCommandHandler
    {
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "create",
            "update",
            "inspect",
            "delete",
            "applyPreset",
            "createLightingSetup",
            "listPresets"
        };

        public override string Category => "lightBundle";
        public override string Version => "1.0.0";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => HandleCreate(payload),
                "update" => HandleUpdate(payload),
                "inspect" => HandleInspect(payload),
                "delete" => HandleDelete(payload),
                "applyPreset" => HandleApplyPreset(payload),
                "createLightingSetup" => HandleCreateLightingSetup(payload),
                "listPresets" => HandleListPresets(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }

        /// <summary>
        /// Create a new light.
        /// </summary>
        private object HandleCreate(Dictionary<string, object> payload)
        {
            string name = GetString(payload, "name", "New Light");
            string lightTypeStr = GetString(payload, "lightType", "directional");
            string parentPath = GetString(payload, "parentPath");
            string preset = GetString(payload, "preset");

            // Parse light type
            LightType lightType = lightTypeStr.ToLower() switch
            {
                "directional" or "sun" => LightType.Directional,
                "point" => LightType.Point,
                "spot" => LightType.Spot,
                "area" or "rectangle" => LightType.Rectangle,
                _ => LightType.Directional
            };

            // Create GameObject with Light
            GameObject lightGO = new GameObject(name);
            Light light = lightGO.AddComponent<Light>();
            light.type = lightType;

            // Set parent if specified
            if (!string.IsNullOrEmpty(parentPath))
            {
                GameObject parent = TryResolveGameObject(parentPath);
                if (parent != null)
                {
                    lightGO.transform.SetParent(parent.transform, false);
                }
            }

            // Apply preset if specified
            if (!string.IsNullOrEmpty(preset))
            {
                ApplyLightPreset(light, preset);
            }

            // Apply position/rotation from payload
            ApplyTransformFromPayload(lightGO.transform, payload);

            // Apply light properties
            ApplyLightProperties(light, payload);

            Undo.RegisterCreatedObjectUndo(lightGO, $"Create {name}");

            return CreateSuccessResponse(
                ("message", $"Light '{name}' created"),
                ("gameObjectPath", GetGameObjectPath(lightGO)),
                ("lightType", lightType.ToString()),
                ("preset", preset ?? "none")
            );
        }

        /// <summary>
        /// Update an existing light.
        /// </summary>
        private object HandleUpdate(Dictionary<string, object> payload)
        {
            GameObject go = ResolveGameObjectFromPayload(payload);
            Light light = go.GetComponent<Light>();

            if (light == null)
            {
                return CreateFailureResponse($"GameObject '{go.name}' does not have a Light component");
            }

            ApplyLightProperties(light, payload);

            EditorUtility.SetDirty(go);

            return CreateSuccessResponse(
                ("message", $"Light updated"),
                ("gameObjectPath", GetGameObjectPath(go))
            );
        }

        /// <summary>
        /// Inspect light properties.
        /// </summary>
        private object HandleInspect(Dictionary<string, object> payload)
        {
            GameObject go = ResolveGameObjectFromPayload(payload);
            Light light = go.GetComponent<Light>();

            if (light == null)
            {
                return CreateFailureResponse($"GameObject '{go.name}' does not have a Light component");
            }

            return CreateSuccessResponse(
                ("gameObjectPath", GetGameObjectPath(go)),
                ("lightType", light.type.ToString()),
                ("color", new Dictionary<string, float>
                {
                    ["r"] = light.color.r,
                    ["g"] = light.color.g,
                    ["b"] = light.color.b,
                    ["a"] = light.color.a
                }),
                ("intensity", light.intensity),
                ("range", light.range),
                ("spotAngle", light.spotAngle),
                ("innerSpotAngle", light.innerSpotAngle),
                ("shadows", light.shadows.ToString()),
                ("shadowStrength", light.shadowStrength),
                ("cookieSize", light.cookieSize),
                ("renderMode", light.renderMode.ToString()),
                ("cullingMask", light.cullingMask),
                ("bounceIntensity", light.bounceIntensity),
                ("position", new Dictionary<string, float>
                {
                    ["x"] = light.transform.position.x,
                    ["y"] = light.transform.position.y,
                    ["z"] = light.transform.position.z
                }),
                ("rotation", new Dictionary<string, float>
                {
                    ["x"] = light.transform.eulerAngles.x,
                    ["y"] = light.transform.eulerAngles.y,
                    ["z"] = light.transform.eulerAngles.z
                })
            );
        }

        /// <summary>
        /// Delete light.
        /// </summary>
        private object HandleDelete(Dictionary<string, object> payload)
        {
            GameObject go = ResolveGameObjectFromPayload(payload);
            string path = GetGameObjectPath(go);

            Undo.DestroyObjectImmediate(go);

            return CreateSuccessResponse(
                ("message", $"Light deleted"),
                ("gameObjectPath", path)
            );
        }

        /// <summary>
        /// Apply preset to existing light.
        /// </summary>
        private object HandleApplyPreset(Dictionary<string, object> payload)
        {
            GameObject go = ResolveGameObjectFromPayload(payload);
            Light light = go.GetComponent<Light>();
            string preset = GetString(payload, "preset");

            if (light == null)
            {
                return CreateFailureResponse($"GameObject '{go.name}' does not have a Light component");
            }

            if (string.IsNullOrEmpty(preset))
            {
                return CreateFailureResponse("'preset' is required");
            }

            ApplyLightPreset(light, preset);
            EditorUtility.SetDirty(go);

            return CreateSuccessResponse(
                ("message", $"Preset '{preset}' applied"),
                ("gameObjectPath", GetGameObjectPath(go))
            );
        }

        /// <summary>
        /// Create a complete lighting setup with multiple lights.
        /// </summary>
        private object HandleCreateLightingSetup(Dictionary<string, object> payload)
        {
            string setupType = GetString(payload, "setupType", "daylight");
            string parentPath = GetString(payload, "parentPath");

            // Create parent container
            GameObject container = new GameObject($"Lighting_{setupType}");
            if (!string.IsNullOrEmpty(parentPath))
            {
                GameObject parent = TryResolveGameObject(parentPath);
                if (parent != null)
                {
                    container.transform.SetParent(parent.transform, false);
                }
            }

            var createdLights = new List<string>();

            switch (setupType.ToLower())
            {
                case "daylight":
                    CreateDaylightSetup(container.transform, createdLights);
                    break;
                case "nighttime":
                    CreateNighttimeSetup(container.transform, createdLights);
                    break;
                case "indoor":
                    CreateIndoorSetup(container.transform, createdLights);
                    break;
                case "dramatic":
                    CreateDramaticSetup(container.transform, createdLights);
                    break;
                case "studio":
                    CreateStudioSetup(container.transform, createdLights);
                    break;
                case "sunset":
                    CreateSunsetSetup(container.transform, createdLights);
                    break;
                default:
                    CreateDaylightSetup(container.transform, createdLights);
                    break;
            }

            Undo.RegisterCreatedObjectUndo(container, $"Create {setupType} lighting");

            return CreateSuccessResponse(
                ("message", $"Lighting setup '{setupType}' created"),
                ("containerPath", GetGameObjectPath(container)),
                ("createdLights", createdLights),
                ("lightCount", createdLights.Count)
            );
        }

        /// <summary>
        /// List available presets.
        /// </summary>
        private object HandleListPresets(Dictionary<string, object> payload)
        {
            var lightPresets = new List<Dictionary<string, object>>
            {
                new() { ["name"] = "daylight", ["description"] = "Warm sun light", ["lightType"] = "Directional" },
                new() { ["name"] = "moonlight", ["description"] = "Cool blue moonlight", ["lightType"] = "Directional" },
                new() { ["name"] = "warm", ["description"] = "Warm indoor light", ["lightType"] = "Point" },
                new() { ["name"] = "cool", ["description"] = "Cool fluorescent light", ["lightType"] = "Point" },
                new() { ["name"] = "spotlight", ["description"] = "Focused spotlight", ["lightType"] = "Spot" },
                new() { ["name"] = "candle", ["description"] = "Flickering candle light", ["lightType"] = "Point" },
                new() { ["name"] = "neon", ["description"] = "Bright neon light", ["lightType"] = "Point" }
            };

            var setupPresets = new List<Dictionary<string, object>>
            {
                new() { ["name"] = "daylight", ["description"] = "Outdoor sunny day", ["lights"] = 2 },
                new() { ["name"] = "nighttime", ["description"] = "Moonlit night", ["lights"] = 2 },
                new() { ["name"] = "indoor", ["description"] = "Interior room lighting", ["lights"] = 3 },
                new() { ["name"] = "dramatic", ["description"] = "High contrast dramatic", ["lights"] = 2 },
                new() { ["name"] = "studio", ["description"] = "3-point lighting setup", ["lights"] = 3 },
                new() { ["name"] = "sunset", ["description"] = "Golden hour lighting", ["lights"] = 2 }
            };

            return CreateSuccessResponse(
                ("lightPresets", lightPresets),
                ("setupPresets", setupPresets)
            );
        }

        #region Light Preset Helpers

        private void ApplyLightPreset(Light light, string preset)
        {
            switch (preset.ToLower())
            {
                case "daylight":
                    light.color = new Color(1f, 0.957f, 0.839f); // Warm white
                    light.intensity = 1.2f;
                    light.shadows = LightShadows.Soft;
                    light.shadowStrength = 0.8f;
                    break;

                case "moonlight":
                    light.color = new Color(0.678f, 0.847f, 1f); // Cool blue
                    light.intensity = 0.3f;
                    light.shadows = LightShadows.Soft;
                    light.shadowStrength = 0.5f;
                    break;

                case "warm":
                    light.color = new Color(1f, 0.843f, 0.667f); // Warm incandescent
                    light.intensity = 1f;
                    light.range = 10f;
                    break;

                case "cool":
                    light.color = new Color(0.902f, 0.957f, 1f); // Cool fluorescent
                    light.intensity = 1f;
                    light.range = 10f;
                    break;

                case "spotlight":
                    light.type = LightType.Spot;
                    light.color = Color.white;
                    light.intensity = 2f;
                    light.spotAngle = 30f;
                    light.range = 20f;
                    light.shadows = LightShadows.Hard;
                    break;

                case "candle":
                    light.color = new Color(1f, 0.6f, 0.2f); // Orange candle
                    light.intensity = 0.5f;
                    light.range = 5f;
                    break;

                case "neon":
                    light.color = new Color(0.5f, 1f, 1f); // Cyan neon
                    light.intensity = 2f;
                    light.range = 8f;
                    break;
            }
        }

        #endregion

        #region Lighting Setup Helpers

        private void CreateDaylightSetup(Transform parent, List<string> createdLights)
        {
            // Main sun light
            var sun = CreateLightInternal("Sun", LightType.Directional, parent);
            sun.color = new Color(1f, 0.957f, 0.839f);
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.8f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            createdLights.Add("Sun");

            // Fill light (ambient)
            var fill = CreateLightInternal("Fill Light", LightType.Directional, parent);
            fill.color = new Color(0.678f, 0.847f, 1f); // Sky blue
            fill.intensity = 0.3f;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(-45f, 180f, 0f);
            createdLights.Add("Fill Light");
        }

        private void CreateNighttimeSetup(Transform parent, List<string> createdLights)
        {
            // Moon light
            var moon = CreateLightInternal("Moon", LightType.Directional, parent);
            moon.color = new Color(0.678f, 0.847f, 1f);
            moon.intensity = 0.3f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = 0.5f;
            moon.transform.rotation = Quaternion.Euler(45f, -60f, 0f);
            createdLights.Add("Moon");

            // Ambient fill
            var ambient = CreateLightInternal("Ambient", LightType.Directional, parent);
            ambient.color = new Color(0.1f, 0.1f, 0.2f); // Dark blue
            ambient.intensity = 0.1f;
            ambient.shadows = LightShadows.None;
            ambient.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            createdLights.Add("Ambient");
        }

        private void CreateIndoorSetup(Transform parent, List<string> createdLights)
        {
            // Ceiling light 1
            var ceiling1 = CreateLightInternal("Ceiling Light 1", LightType.Point, parent);
            ceiling1.color = new Color(1f, 0.957f, 0.839f);
            ceiling1.intensity = 1f;
            ceiling1.range = 10f;
            ceiling1.transform.localPosition = new Vector3(-3f, 3f, 0f);
            createdLights.Add("Ceiling Light 1");

            // Ceiling light 2
            var ceiling2 = CreateLightInternal("Ceiling Light 2", LightType.Point, parent);
            ceiling2.color = new Color(1f, 0.957f, 0.839f);
            ceiling2.intensity = 1f;
            ceiling2.range = 10f;
            ceiling2.transform.localPosition = new Vector3(3f, 3f, 0f);
            createdLights.Add("Ceiling Light 2");

            // Window light (simulated)
            var window = CreateLightInternal("Window Light", LightType.Directional, parent);
            window.color = new Color(0.9f, 0.95f, 1f);
            window.intensity = 0.5f;
            window.shadows = LightShadows.Soft;
            window.transform.rotation = Quaternion.Euler(30f, -90f, 0f);
            createdLights.Add("Window Light");
        }

        private void CreateDramaticSetup(Transform parent, List<string> createdLights)
        {
            // Key light (strong, directional)
            var key = CreateLightInternal("Key Light", LightType.Spot, parent);
            key.color = new Color(1f, 0.9f, 0.8f);
            key.intensity = 3f;
            key.spotAngle = 45f;
            key.range = 30f;
            key.shadows = LightShadows.Hard;
            key.shadowStrength = 1f;
            key.transform.position = new Vector3(5f, 5f, 5f);
            key.transform.LookAt(Vector3.zero);
            createdLights.Add("Key Light");

            // Rim light (back light for silhouette)
            var rim = CreateLightInternal("Rim Light", LightType.Directional, parent);
            rim.color = new Color(0.8f, 0.9f, 1f);
            rim.intensity = 0.8f;
            rim.shadows = LightShadows.None;
            rim.transform.rotation = Quaternion.Euler(10f, 180f, 0f);
            createdLights.Add("Rim Light");
        }

        private void CreateStudioSetup(Transform parent, List<string> createdLights)
        {
            // Key light (main, 45 degrees)
            var key = CreateLightInternal("Key Light", LightType.Directional, parent);
            key.color = Color.white;
            key.intensity = 1.2f;
            key.shadows = LightShadows.Soft;
            key.transform.rotation = Quaternion.Euler(45f, 45f, 0f);
            createdLights.Add("Key Light");

            // Fill light (softer, opposite side)
            var fill = CreateLightInternal("Fill Light", LightType.Directional, parent);
            fill.color = Color.white;
            fill.intensity = 0.5f;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(30f, -45f, 0f);
            createdLights.Add("Fill Light");

            // Back light (rim/hair light)
            var back = CreateLightInternal("Back Light", LightType.Directional, parent);
            back.color = Color.white;
            back.intensity = 0.8f;
            back.shadows = LightShadows.None;
            back.transform.rotation = Quaternion.Euler(30f, 180f, 0f);
            createdLights.Add("Back Light");
        }

        private void CreateSunsetSetup(Transform parent, List<string> createdLights)
        {
            // Sun (low angle, orange)
            var sun = CreateLightInternal("Sun", LightType.Directional, parent);
            sun.color = new Color(1f, 0.5f, 0.2f); // Orange
            sun.intensity = 1f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.9f;
            sun.transform.rotation = Quaternion.Euler(10f, -30f, 0f);
            createdLights.Add("Sun");

            // Sky fill (purple/blue)
            var sky = CreateLightInternal("Sky Fill", LightType.Directional, parent);
            sky.color = new Color(0.6f, 0.4f, 0.8f); // Purple
            sky.intensity = 0.3f;
            sky.shadows = LightShadows.None;
            sky.transform.rotation = Quaternion.Euler(-45f, 90f, 0f);
            createdLights.Add("Sky Fill");
        }

        private Light CreateLightInternal(string name, LightType type, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Light light = go.AddComponent<Light>();
            light.type = type;
            return light;
        }

        #endregion

        #region Helper Methods

        private void ApplyTransformFromPayload(Transform transform, Dictionary<string, object> payload)
        {
            if (payload.ContainsKey("position") && payload["position"] is Dictionary<string, object> posDict)
            {
                transform.position = new Vector3(
                    Convert.ToSingle(posDict.GetValueOrDefault("x", 0f)),
                    Convert.ToSingle(posDict.GetValueOrDefault("y", 0f)),
                    Convert.ToSingle(posDict.GetValueOrDefault("z", 0f))
                );
            }

            if (payload.ContainsKey("rotation") && payload["rotation"] is Dictionary<string, object> rotDict)
            {
                transform.eulerAngles = new Vector3(
                    Convert.ToSingle(rotDict.GetValueOrDefault("x", 0f)),
                    Convert.ToSingle(rotDict.GetValueOrDefault("y", 0f)),
                    Convert.ToSingle(rotDict.GetValueOrDefault("z", 0f))
                );
            }
        }

        private void ApplyLightProperties(Light light, Dictionary<string, object> payload)
        {
            if (payload.ContainsKey("color"))
            {
                var colorValue = payload["color"];
                if (colorValue is string hexColor)
                {
                    if (ColorUtility.TryParseHtmlString(hexColor, out Color parsed))
                    {
                        light.color = parsed;
                    }
                }
                else if (colorValue is Dictionary<string, object> colorDict)
                {
                    light.color = new Color(
                        Convert.ToSingle(colorDict.GetValueOrDefault("r", 1f)),
                        Convert.ToSingle(colorDict.GetValueOrDefault("g", 1f)),
                        Convert.ToSingle(colorDict.GetValueOrDefault("b", 1f)),
                        Convert.ToSingle(colorDict.GetValueOrDefault("a", 1f))
                    );
                }
            }

            if (payload.ContainsKey("intensity"))
            {
                light.intensity = GetFloat(payload, "intensity", light.intensity);
            }

            if (payload.ContainsKey("range"))
            {
                light.range = GetFloat(payload, "range", light.range);
            }

            if (payload.ContainsKey("spotAngle"))
            {
                light.spotAngle = GetFloat(payload, "spotAngle", light.spotAngle);
            }

            if (payload.ContainsKey("innerSpotAngle"))
            {
                light.innerSpotAngle = GetFloat(payload, "innerSpotAngle", light.innerSpotAngle);
            }

            if (payload.ContainsKey("shadows"))
            {
                string shadowStr = GetString(payload, "shadows", "none");
                light.shadows = shadowStr.ToLower() switch
                {
                    "hard" => LightShadows.Hard,
                    "soft" => LightShadows.Soft,
                    _ => LightShadows.None
                };
            }

            if (payload.ContainsKey("shadowStrength"))
            {
                light.shadowStrength = GetFloat(payload, "shadowStrength", light.shadowStrength);
            }

            if (payload.ContainsKey("bounceIntensity"))
            {
                light.bounceIntensity = GetFloat(payload, "bounceIntensity", light.bounceIntensity);
            }
        }

        private string GetGameObjectPath(GameObject go)
        {
            string path = go.name;
            Transform parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        #endregion
    }
}
