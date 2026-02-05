using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Mid-level CharacterController bundle: apply CharacterController with presets for FPS, TPS, platformer, etc.
    /// </summary>
    public class CharacterControllerBundleHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "applyPreset",
            "update",
            "inspect",
        };

        public override string Category => "characterControllerBundle";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "applyPreset" => ApplyPreset(payload),
                "update" => UpdateController(payload),
                "inspect" => InspectController(payload),
                _ => throw new InvalidOperationException($"Unsupported CharacterController bundle operation: {operation}"),
            };
        }

        #region Apply Preset

        private object ApplyPreset(Dictionary<string, object> payload)
        {
            var targets = GetTargetGameObjects(payload);
            var preset = GetString(payload, "preset")?.ToLowerInvariant() ?? "fps";

            var updated = new List<string>();

            foreach (var go in targets)
            {
                Undo.RecordObject(go, "Apply CharacterController Preset");

                var controller = go.GetComponent<CharacterController>();
                if (controller == null)
                {
                    controller = Undo.AddComponent<CharacterController>(go);
                }

                ApplyCharacterControllerPreset(controller, preset, payload);

                updated.Add(BuildGameObjectPath(go));
            }

            MarkScenesDirty(targets);
            return CreateSuccessResponse(("updated", updated), ("count", updated.Count), ("preset", preset));
        }

        private void ApplyCharacterControllerPreset(CharacterController controller, string preset, Dictionary<string, object> payload)
        {
            switch (preset)
            {
                case "fps":
                    // Standard FPS character settings
                    controller.radius = 0.5f;
                    controller.height = 2f;
                    controller.center = new Vector3(0, 1, 0);
                    controller.slopeLimit = 45f;
                    controller.stepOffset = 0.3f;
                    controller.skinWidth = 0.08f;
                    controller.minMoveDistance = 0.001f;
                    break;

                case "tps":
                    // Third-person shooter settings (slightly smaller)
                    controller.radius = 0.4f;
                    controller.height = 1.8f;
                    controller.center = new Vector3(0, 0.9f, 0);
                    controller.slopeLimit = 45f;
                    controller.stepOffset = 0.3f;
                    controller.skinWidth = 0.08f;
                    controller.minMoveDistance = 0.001f;
                    break;

                case "platformer":
                    // Platformer character settings (tighter control)
                    controller.radius = 0.3f;
                    controller.height = 1.6f;
                    controller.center = new Vector3(0, 0.8f, 0);
                    controller.slopeLimit = 50f;
                    controller.stepOffset = 0.4f;
                    controller.skinWidth = 0.08f;
                    controller.minMoveDistance = 0f;
                    break;

                case "child":
                    // Smaller character (e.g., child, halfling)
                    controller.radius = 0.35f;
                    controller.height = 1.2f;
                    controller.center = new Vector3(0, 0.6f, 0);
                    controller.slopeLimit = 45f;
                    controller.stepOffset = 0.2f;
                    controller.skinWidth = 0.08f;
                    controller.minMoveDistance = 0.001f;
                    break;

                case "large":
                    // Large character (e.g., ogre, mech)
                    controller.radius = 1f;
                    controller.height = 3.5f;
                    controller.center = new Vector3(0, 1.75f, 0);
                    controller.slopeLimit = 40f;
                    controller.stepOffset = 0.5f;
                    controller.skinWidth = 0.1f;
                    controller.minMoveDistance = 0.001f;
                    break;

                case "narrow":
                    // Narrow character (e.g., for tight spaces)
                    controller.radius = 0.25f;
                    controller.height = 1.8f;
                    controller.center = new Vector3(0, 0.9f, 0);
                    controller.slopeLimit = 45f;
                    controller.stepOffset = 0.3f;
                    controller.skinWidth = 0.08f;
                    controller.minMoveDistance = 0.001f;
                    break;

                case "custom":
                    // Custom settings from payload
                    ApplyCustomSettings(controller, payload);
                    break;

                default:
                    // Default to FPS
                    goto case "fps";
            }

            // Override with any specific values from payload
            ApplyOverrides(controller, payload);
        }

        private void ApplyCustomSettings(CharacterController controller, Dictionary<string, object> payload)
        {
            // Set reasonable defaults for custom preset
            controller.radius = 0.5f;
            controller.height = 2f;
            controller.center = new Vector3(0, 1, 0);
            controller.slopeLimit = 45f;
            controller.stepOffset = 0.3f;
            controller.skinWidth = 0.08f;
            controller.minMoveDistance = 0.001f;
        }

        private void ApplyOverrides(CharacterController controller, Dictionary<string, object> payload)
        {
            if (payload.TryGetValue("radius", out var radiusObj))
            {
                controller.radius = Convert.ToSingle(radiusObj);
            }

            if (payload.TryGetValue("height", out var heightObj))
            {
                controller.height = Convert.ToSingle(heightObj);
            }

            if (payload.TryGetValue("center", out var centerObj) && centerObj is Dictionary<string, object> centerDict)
            {
                controller.center = GetVector3FromDict(centerDict, controller.center);
            }

            if (payload.TryGetValue("slopeLimit", out var slopeLimitObj))
            {
                controller.slopeLimit = Convert.ToSingle(slopeLimitObj);
            }

            if (payload.TryGetValue("stepOffset", out var stepOffsetObj))
            {
                controller.stepOffset = Convert.ToSingle(stepOffsetObj);
            }

            if (payload.TryGetValue("skinWidth", out var skinWidthObj))
            {
                controller.skinWidth = Convert.ToSingle(skinWidthObj);
            }

            if (payload.TryGetValue("minMoveDistance", out var minMoveDistanceObj))
            {
                controller.minMoveDistance = Convert.ToSingle(minMoveDistanceObj);
            }
        }

        #endregion

        #region Update Controller

        private object UpdateController(Dictionary<string, object> payload)
        {
            var targets = GetTargetGameObjects(payload);
            var updated = new List<string>();

            foreach (var go in targets)
            {
                var controller = go.GetComponent<CharacterController>();
                if (controller == null)
                {
                    throw new InvalidOperationException($"GameObject '{BuildGameObjectPath(go)}' does not have a CharacterController component.");
                }

                Undo.RecordObject(controller, "Update CharacterController");
                ApplyOverrides(controller, payload);

                updated.Add(BuildGameObjectPath(go));
            }

            MarkScenesDirty(targets);
            return CreateSuccessResponse(("updated", updated), ("count", updated.Count));
        }

        #endregion

        #region Inspect Controller

        private object InspectController(Dictionary<string, object> payload)
        {
            var targets = GetTargetGameObjects(payload);
            var results = new List<Dictionary<string, object>>();

            foreach (var go in targets)
            {
                var controller = go.GetComponent<CharacterController>();
                if (controller == null)
                {
                    results.Add(new Dictionary<string, object>
                    {
                        { "path", BuildGameObjectPath(go) },
                        { "hasCharacterController", false }
                    });
                    continue;
                }

                var info = new Dictionary<string, object>
                {
                    { "path", BuildGameObjectPath(go) },
                    { "hasCharacterController", true },
                    { "radius", controller.radius },
                    { "height", controller.height },
                    { "center", new Dictionary<string, object>
                        {
                            { "x", controller.center.x },
                            { "y", controller.center.y },
                            { "z", controller.center.z }
                        }
                    },
                    { "slopeLimit", controller.slopeLimit },
                    { "stepOffset", controller.stepOffset },
                    { "skinWidth", controller.skinWidth },
                    { "minMoveDistance", controller.minMoveDistance },
                    { "isGrounded", controller.isGrounded },
                    { "velocity", new Dictionary<string, object>
                        {
                            { "x", controller.velocity.x },
                            { "y", controller.velocity.y },
                            { "z", controller.velocity.z }
                        }
                    },
                    { "collisionFlags", controller.collisionFlags.ToString() }
                };

                results.Add(info);
            }

            return CreateSuccessResponse(("controllers", results), ("count", results.Count));
        }

        #endregion

        #region Helpers

        private List<GameObject> GetTargetGameObjects(Dictionary<string, object> payload)
        {
            var targets = new List<GameObject>();

            if (payload.TryGetValue("gameObjectPath", out var pathObj) && pathObj is string path && !string.IsNullOrEmpty(path))
            {
                targets.Add(ResolveGameObject(path));
            }
            else if (payload.TryGetValue("gameObjectPaths", out var pathsObj) && pathsObj is List<object> pathsList)
            {
                foreach (var pathItem in pathsList)
                {
                    if (pathItem is string itemPath && !string.IsNullOrEmpty(itemPath))
                    {
                        targets.Add(ResolveGameObject(itemPath));
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("Either 'gameObjectPath' or 'gameObjectPaths' is required.");
            }

            return targets;
        }

        #endregion
    }
}

