using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Particle Bundle Handler: Creates and configures particle systems with presets.
    /// Provides ready-to-use VFX for common game effects.
    /// </summary>
    public class ParticleBundleHandler : BaseCommandHandler
    {
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "create",
            "update",
            "applyPreset",
            "play",
            "stop",
            "pause",
            "inspect",
            "delete",
            "duplicate",
            "listPresets"
        };

        public override string Category => "particleBundle";
        public override string Version => "1.0.0";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => HandleCreate(payload),
                "update" => HandleUpdate(payload),
                "applyPreset" => HandleApplyPreset(payload),
                "play" => HandlePlay(payload),
                "stop" => HandleStop(payload),
                "pause" => HandlePause(payload),
                "inspect" => HandleInspect(payload),
                "delete" => HandleDelete(payload),
                "duplicate" => HandleDuplicate(payload),
                "listPresets" => HandleListPresets(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }

        /// <summary>
        /// Create a new particle system.
        /// </summary>
        private object HandleCreate(Dictionary<string, object> payload)
        {
            string name = GetString(payload, "name", "Particle System");
            string parentPath = GetString(payload, "parentPath");
            string preset = GetString(payload, "preset");

            // Create GameObject with ParticleSystem
            GameObject particleGO = new GameObject(name);
            ParticleSystem ps = particleGO.AddComponent<ParticleSystem>();

            // Set parent if specified
            if (!string.IsNullOrEmpty(parentPath))
            {
                GameObject parent = TryResolveGameObject(parentPath);
                if (parent != null)
                {
                    particleGO.transform.SetParent(parent.transform, false);
                }
            }

            // Apply position from payload
            if (payload.ContainsKey("position") && payload["position"] is Dictionary<string, object> posDict)
            {
                particleGO.transform.position = new Vector3(
                    Convert.ToSingle(posDict.GetValueOrDefault("x", 0f)),
                    Convert.ToSingle(posDict.GetValueOrDefault("y", 0f)),
                    Convert.ToSingle(posDict.GetValueOrDefault("z", 0f))
                );
            }

            // Apply preset if specified
            if (!string.IsNullOrEmpty(preset))
            {
                ApplyPreset(ps, preset, payload);
            }
            else
            {
                // Apply basic settings
                ApplyBasicSettings(ps, payload);
            }

            Undo.RegisterCreatedObjectUndo(particleGO, $"Create {name}");

            return CreateSuccessResponse(
                ("message", $"Particle System '{name}' created"),
                ("gameObjectPath", GetGameObjectPath(particleGO)),
                ("preset", preset ?? "custom")
            );
        }

        /// <summary>
        /// Update an existing particle system.
        /// </summary>
        private object HandleUpdate(Dictionary<string, object> payload)
        {
            GameObject go = ResolveGameObjectFromPayload(payload);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();

            if (ps == null)
            {
                return CreateFailureResponse($"GameObject '{go.name}' does not have a ParticleSystem component");
            }

            ApplyBasicSettings(ps, payload);
            EditorUtility.SetDirty(go);

            return CreateSuccessResponse(
                ("message", $"Particle System updated"),
                ("gameObjectPath", GetGameObjectPath(go))
            );
        }

        /// <summary>
        /// Apply preset to particle system.
        /// </summary>
        private object HandleApplyPreset(Dictionary<string, object> payload)
        {
            GameObject go = ResolveGameObjectFromPayload(payload);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            string preset = GetString(payload, "preset");

            if (ps == null)
            {
                return CreateFailureResponse($"GameObject '{go.name}' does not have a ParticleSystem component");
            }

            if (string.IsNullOrEmpty(preset))
            {
                return CreateFailureResponse("'preset' is required");
            }

            ApplyPreset(ps, preset, payload);
            EditorUtility.SetDirty(go);

            return CreateSuccessResponse(
                ("message", $"Preset '{preset}' applied"),
                ("gameObjectPath", GetGameObjectPath(go))
            );
        }

        /// <summary>
        /// Play particle system.
        /// </summary>
        private object HandlePlay(Dictionary<string, object> payload)
        {
            GameObject go = ResolveGameObjectFromPayload(payload);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();

            if (ps == null)
            {
                return CreateFailureResponse($"GameObject '{go.name}' does not have a ParticleSystem component");
            }

            bool withChildren = GetBool(payload, "withChildren", true);
            ps.Play(withChildren);

            return CreateSuccessResponse(
                ("message", "Particle System playing"),
                ("gameObjectPath", GetGameObjectPath(go))
            );
        }

        /// <summary>
        /// Stop particle system.
        /// </summary>
        private object HandleStop(Dictionary<string, object> payload)
        {
            GameObject go = ResolveGameObjectFromPayload(payload);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();

            if (ps == null)
            {
                return CreateFailureResponse($"GameObject '{go.name}' does not have a ParticleSystem component");
            }

            bool withChildren = GetBool(payload, "withChildren", true);
            bool clear = GetBool(payload, "clear", false);

            ps.Stop(withChildren, clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);

            return CreateSuccessResponse(
                ("message", "Particle System stopped"),
                ("gameObjectPath", GetGameObjectPath(go))
            );
        }

        /// <summary>
        /// Pause particle system.
        /// </summary>
        private object HandlePause(Dictionary<string, object> payload)
        {
            GameObject go = ResolveGameObjectFromPayload(payload);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();

            if (ps == null)
            {
                return CreateFailureResponse($"GameObject '{go.name}' does not have a ParticleSystem component");
            }

            bool withChildren = GetBool(payload, "withChildren", true);
            ps.Pause(withChildren);

            return CreateSuccessResponse(
                ("message", "Particle System paused"),
                ("gameObjectPath", GetGameObjectPath(go))
            );
        }

        /// <summary>
        /// Inspect particle system.
        /// </summary>
        private object HandleInspect(Dictionary<string, object> payload)
        {
            GameObject go = ResolveGameObjectFromPayload(payload);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();

            if (ps == null)
            {
                return CreateFailureResponse($"GameObject '{go.name}' does not have a ParticleSystem component");
            }

            var main = ps.main;
            var emission = ps.emission;
            var shape = ps.shape;

            return CreateSuccessResponse(
                ("gameObjectPath", GetGameObjectPath(go)),
                ("isPlaying", ps.isPlaying),
                ("isPaused", ps.isPaused),
                ("isStopped", ps.isStopped),
                ("particleCount", ps.particleCount),
                ("main", new Dictionary<string, object>
                {
                    ["duration"] = main.duration,
                    ["looping"] = main.loop,
                    ["startLifetime"] = main.startLifetime.constant,
                    ["startSpeed"] = main.startSpeed.constant,
                    ["startSize"] = main.startSize.constant,
                    ["startColor"] = ColorToDict(main.startColor.color),
                    ["maxParticles"] = main.maxParticles,
                    ["simulationSpace"] = main.simulationSpace.ToString(),
                    ["playOnAwake"] = main.playOnAwake
                }),
                ("emission", new Dictionary<string, object>
                {
                    ["enabled"] = emission.enabled,
                    ["rateOverTime"] = emission.rateOverTime.constant
                }),
                ("shape", new Dictionary<string, object>
                {
                    ["enabled"] = shape.enabled,
                    ["shapeType"] = shape.shapeType.ToString(),
                    ["radius"] = shape.radius,
                    ["angle"] = shape.angle
                })
            );
        }

        /// <summary>
        /// Delete particle system.
        /// </summary>
        private object HandleDelete(Dictionary<string, object> payload)
        {
            GameObject go = ResolveGameObjectFromPayload(payload);
            string path = GetGameObjectPath(go);

            Undo.DestroyObjectImmediate(go);

            return CreateSuccessResponse(
                ("message", $"Particle System deleted"),
                ("gameObjectPath", path)
            );
        }

        /// <summary>
        /// Duplicate particle system.
        /// </summary>
        private object HandleDuplicate(Dictionary<string, object> payload)
        {
            GameObject go = ResolveGameObjectFromPayload(payload);
            string newName = GetString(payload, "newName", go.name + "_Copy");

            GameObject duplicate = UnityEngine.Object.Instantiate(go);
            duplicate.name = newName;

            Undo.RegisterCreatedObjectUndo(duplicate, $"Duplicate {go.name}");

            return CreateSuccessResponse(
                ("message", $"Particle System duplicated"),
                ("sourcePath", GetGameObjectPath(go)),
                ("newPath", GetGameObjectPath(duplicate))
            );
        }

        /// <summary>
        /// List available presets.
        /// </summary>
        private object HandleListPresets(Dictionary<string, object> payload)
        {
            var presets = new List<Dictionary<string, object>>
            {
                new() { ["name"] = "explosion", ["description"] = "Burst explosion effect", ["type"] = "burst" },
                new() { ["name"] = "fire", ["description"] = "Continuous fire flames", ["type"] = "continuous" },
                new() { ["name"] = "smoke", ["description"] = "Rising smoke", ["type"] = "continuous" },
                new() { ["name"] = "sparkle", ["description"] = "Sparkling particles", ["type"] = "continuous" },
                new() { ["name"] = "rain", ["description"] = "Falling rain drops", ["type"] = "continuous" },
                new() { ["name"] = "snow", ["description"] = "Falling snow flakes", ["type"] = "continuous" },
                new() { ["name"] = "dust", ["description"] = "Floating dust particles", ["type"] = "continuous" },
                new() { ["name"] = "trail", ["description"] = "Motion trail effect", ["type"] = "trail" },
                new() { ["name"] = "hit", ["description"] = "Impact hit effect", ["type"] = "burst" },
                new() { ["name"] = "heal", ["description"] = "Healing/buff effect", ["type"] = "continuous" },
                new() { ["name"] = "magic", ["description"] = "Magical sparkles", ["type"] = "continuous" },
                new() { ["name"] = "leaves", ["description"] = "Falling leaves", ["type"] = "continuous" }
            };

            return CreateSuccessResponse(
                ("presets", presets)
            );
        }

        #region Preset Implementations

        private void ApplyPreset(ParticleSystem ps, string preset, Dictionary<string, object> payload)
        {
            // Get override values from payload
            float overrideSize = GetFloat(payload, "startSize", -1f);
            float overrideLifetime = GetFloat(payload, "startLifetime", -1f);
            Color? overrideColor = null;

            if (payload.ContainsKey("startColor"))
            {
                var colorValue = payload["startColor"];
                if (colorValue is string hexColor && ColorUtility.TryParseHtmlString(hexColor, out Color parsed))
                {
                    overrideColor = parsed;
                }
                else if (colorValue is Dictionary<string, object> colorDict)
                {
                    overrideColor = new Color(
                        Convert.ToSingle(colorDict.GetValueOrDefault("r", 1f)),
                        Convert.ToSingle(colorDict.GetValueOrDefault("g", 1f)),
                        Convert.ToSingle(colorDict.GetValueOrDefault("b", 1f)),
                        Convert.ToSingle(colorDict.GetValueOrDefault("a", 1f))
                    );
                }
            }

            switch (preset.ToLower())
            {
                case "explosion":
                    SetupExplosion(ps, overrideSize, overrideLifetime, overrideColor);
                    break;
                case "fire":
                    SetupFire(ps, overrideSize, overrideLifetime, overrideColor);
                    break;
                case "smoke":
                    SetupSmoke(ps, overrideSize, overrideLifetime, overrideColor);
                    break;
                case "sparkle":
                    SetupSparkle(ps, overrideSize, overrideLifetime, overrideColor);
                    break;
                case "rain":
                    SetupRain(ps, overrideSize, overrideLifetime, overrideColor);
                    break;
                case "snow":
                    SetupSnow(ps, overrideSize, overrideLifetime, overrideColor);
                    break;
                case "dust":
                    SetupDust(ps, overrideSize, overrideLifetime, overrideColor);
                    break;
                case "trail":
                    SetupTrail(ps, overrideSize, overrideLifetime, overrideColor);
                    break;
                case "hit":
                    SetupHit(ps, overrideSize, overrideLifetime, overrideColor);
                    break;
                case "heal":
                    SetupHeal(ps, overrideSize, overrideLifetime, overrideColor);
                    break;
                case "magic":
                    SetupMagic(ps, overrideSize, overrideLifetime, overrideColor);
                    break;
                case "leaves":
                    SetupLeaves(ps, overrideSize, overrideLifetime, overrideColor);
                    break;
            }
        }

        private void SetupExplosion(ParticleSystem ps, float size, float lifetime, Color? color)
        {
            var main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = lifetime > 0 ? lifetime : 0.5f;
            main.startSpeed = 10f;
            main.startSize = size > 0 ? size : 0.3f;
            main.startColor = color ?? new Color(1f, 0.5f, 0f);
            main.maxParticles = 100;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 50) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.yellow, 0.5f), new GradientColorKey(Color.red, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;
        }

        private void SetupFire(ParticleSystem ps, float size, float lifetime, Color? color)
        {
            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = lifetime > 0 ? lifetime : 1.5f;
            main.startSpeed = 2f;
            main.startSize = size > 0 ? size : 0.5f;
            main.startColor = color ?? new Color(1f, 0.4f, 0f);
            main.maxParticles = 200;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 30;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.2f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.yellow, 0f), new GradientColorKey(new Color(1f, 0.3f, 0f), 0.5f), new GradientColorKey(Color.red, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.5f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
        }

        private void SetupSmoke(ParticleSystem ps, float size, float lifetime, Color? color)
        {
            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = lifetime > 0 ? lifetime : 3f;
            main.startSpeed = 1f;
            main.startSize = size > 0 ? size : 1f;
            main.startColor = color ?? new Color(0.5f, 0.5f, 0.5f, 0.5f);
            main.maxParticles = 100;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 10;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.5f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 2f));

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.gray, 0f), new GradientColorKey(Color.gray, 1f) },
                new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;
        }

        private void SetupSparkle(ParticleSystem ps, float size, float lifetime, Color? color)
        {
            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = lifetime > 0 ? lifetime : 1f;
            main.startSpeed = 0.5f;
            main.startSize = size > 0 ? size : 0.1f;
            main.startColor = color ?? Color.yellow;
            main.maxParticles = 50;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 15;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.yellow, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;
        }

        private void SetupRain(ParticleSystem ps, float size, float lifetime, Color? color)
        {
            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = lifetime > 0 ? lifetime : 1f;
            main.startSpeed = 15f;
            main.startSize3D = true;
            main.startSizeX = size > 0 ? size * 0.5f : 0.02f;
            main.startSizeY = size > 0 ? size * 5f : 0.5f;
            main.startSizeZ = size > 0 ? size * 0.5f : 0.02f;
            main.startColor = color ?? new Color(0.7f, 0.8f, 1f, 0.5f);
            main.maxParticles = 1000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 500;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20f, 0f, 20f);
            shape.position = new Vector3(0f, 10f, 0f);
        }

        private void SetupSnow(ParticleSystem ps, float size, float lifetime, Color? color)
        {
            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = lifetime > 0 ? lifetime : 5f;
            main.startSpeed = 1f;
            main.startSize = size > 0 ? size : 0.1f;
            main.startColor = color ?? Color.white;
            main.maxParticles = 500;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.1f;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 50;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20f, 0f, 20f);
            shape.position = new Vector3(0f, 10f, 0f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 0.5f;
        }

        private void SetupDust(ParticleSystem ps, float size, float lifetime, Color? color)
        {
            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = lifetime > 0 ? lifetime : 4f;
            main.startSpeed = 0.2f;
            main.startSize = size > 0 ? size : 0.05f;
            main.startColor = color ?? new Color(0.8f, 0.7f, 0.6f, 0.3f);
            main.maxParticles = 100;
            main.gravityModifier = 0f;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 10;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(5f, 3f, 5f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.3f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.3f, 0.3f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;
        }

        private void SetupTrail(ParticleSystem ps, float size, float lifetime, Color? color)
        {
            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = lifetime > 0 ? lifetime : 0.5f;
            main.startSpeed = 0f;
            main.startSize = size > 0 ? size : 0.2f;
            main.startColor = color ?? Color.cyan;
            main.maxParticles = 100;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 50;

            var shape = ps.shape;
            shape.enabled = false;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.cyan, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        }

        private void SetupHit(ParticleSystem ps, float size, float lifetime, Color? color)
        {
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = lifetime > 0 ? lifetime : 0.3f;
            main.startSpeed = 5f;
            main.startSize = size > 0 ? size : 0.2f;
            main.startColor = color ?? Color.white;
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.1f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
        }

        private void SetupHeal(ParticleSystem ps, float size, float lifetime, Color? color)
        {
            var main = ps.main;
            main.duration = 2f;
            main.loop = true;
            main.startLifetime = lifetime > 0 ? lifetime : 1.5f;
            main.startSpeed = 1f;
            main.startSize = size > 0 ? size : 0.15f;
            main.startColor = color ?? Color.green;
            main.maxParticles = 50;
            main.gravityModifier = -0.2f;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 20;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.green, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;
        }

        private void SetupMagic(ParticleSystem ps, float size, float lifetime, Color? color)
        {
            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = lifetime > 0 ? lifetime : 1f;
            main.startSpeed = 2f;
            main.startSize = size > 0 ? size : 0.1f;
            main.startColor = color ?? new Color(0.5f, 0f, 1f);
            main.maxParticles = 100;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 30;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.5f, 0f, 1f), 0.5f), new GradientColorKey(Color.blue, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.5f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 1f;
            noise.frequency = 2f;
        }

        private void SetupLeaves(ParticleSystem ps, float size, float lifetime, Color? color)
        {
            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = lifetime > 0 ? lifetime : 5f;
            main.startSpeed = 0.5f;
            main.startSize = size > 0 ? size : 0.2f;
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, 360f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, 360f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, 360f);
            main.startColor = color ?? new Color(0.4f, 0.6f, 0.2f);
            main.maxParticles = 100;
            main.gravityModifier = 0.3f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 10;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(10f, 0f, 10f);
            shape.position = new Vector3(0f, 5f, 0f);

            var rotationOverLifetime = ps.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.x = new ParticleSystem.MinMaxCurve(0f, 180f);
            rotationOverLifetime.y = new ParticleSystem.MinMaxCurve(0f, 180f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 1f;
            noise.frequency = 0.5f;
        }

        #endregion

        #region Helper Methods

        private void ApplyBasicSettings(ParticleSystem ps, Dictionary<string, object> payload)
        {
            var main = ps.main;

            if (payload.ContainsKey("duration"))
            {
                main.duration = GetFloat(payload, "duration", main.duration);
            }

            if (payload.ContainsKey("looping"))
            {
                main.loop = GetBool(payload, "looping", main.loop);
            }

            if (payload.ContainsKey("startLifetime"))
            {
                main.startLifetime = GetFloat(payload, "startLifetime", main.startLifetime.constant);
            }

            if (payload.ContainsKey("startSpeed"))
            {
                main.startSpeed = GetFloat(payload, "startSpeed", main.startSpeed.constant);
            }

            if (payload.ContainsKey("startSize"))
            {
                main.startSize = GetFloat(payload, "startSize", main.startSize.constant);
            }

            if (payload.ContainsKey("startColor"))
            {
                var colorValue = payload["startColor"];
                if (colorValue is string hexColor && ColorUtility.TryParseHtmlString(hexColor, out Color parsed))
                {
                    main.startColor = parsed;
                }
                else if (colorValue is Dictionary<string, object> colorDict)
                {
                    main.startColor = new Color(
                        Convert.ToSingle(colorDict.GetValueOrDefault("r", 1f)),
                        Convert.ToSingle(colorDict.GetValueOrDefault("g", 1f)),
                        Convert.ToSingle(colorDict.GetValueOrDefault("b", 1f)),
                        Convert.ToSingle(colorDict.GetValueOrDefault("a", 1f))
                    );
                }
            }

            if (payload.ContainsKey("maxParticles"))
            {
                main.maxParticles = GetInt(payload, "maxParticles", main.maxParticles);
            }

            if (payload.ContainsKey("gravityModifier"))
            {
                main.gravityModifier = GetFloat(payload, "gravityModifier", main.gravityModifier.constant);
            }

            if (payload.ContainsKey("rateOverTime"))
            {
                var emission = ps.emission;
                emission.rateOverTime = GetFloat(payload, "rateOverTime", emission.rateOverTime.constant);
            }
        }

        private Dictionary<string, float> ColorToDict(Color color)
        {
            return new Dictionary<string, float>
            {
                ["r"] = color.r,
                ["g"] = color.g,
                ["b"] = color.b,
                ["a"] = color.a
            };
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
