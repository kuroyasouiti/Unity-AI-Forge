using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Mid-level physics bundle: apply Rigidbody + Collider presets for 2D/3D physics.
    /// </summary>
    public class PhysicsBundleHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "applyPreset2D",
            "applyPreset3D",
            "updateRigidbody2D",
            "updateRigidbody3D",
            "updateCollider2D",
            "updateCollider3D",
            "inspect",
        };

        public override string Category => "physicsBundle";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "applyPreset2D" => ApplyPreset2D(payload),
                "applyPreset3D" => ApplyPreset3D(payload),
                "updateRigidbody2D" => UpdateRigidbody2D(payload),
                "updateRigidbody3D" => UpdateRigidbody3D(payload),
                "updateCollider2D" => UpdateCollider2D(payload),
                "updateCollider3D" => UpdateCollider3D(payload),
                "inspect" => InspectPhysics(payload),
                _ => throw new InvalidOperationException($"Unsupported physics bundle operation: {operation}"),
            };
        }

        #region Apply Preset 2D

        private object ApplyPreset2D(Dictionary<string, object> payload)
        {
            var targets = GetTargetGameObjects(payload);
            var preset = GetString(payload, "preset")?.ToLowerInvariant() ?? "dynamic";
            var colliderType = GetString(payload, "colliderType")?.ToLowerInvariant() ?? "box";

            var updated = new List<string>();

            foreach (var go in targets)
            {
                Undo.RecordObject(go, "Apply Physics Preset 2D");

                var rb = go.GetComponent<Rigidbody2D>();
                if (rb == null)
                {
                    rb = Undo.AddComponent<Rigidbody2D>(go);
                }

                ApplyRigidbody2DPreset(rb, preset);

                // Add collider if specified
                if (!string.IsNullOrEmpty(colliderType))
                {
                    AddCollider2D(go, colliderType, payload);
                }

                updated.Add(BuildGameObjectPath(go));
            }

            MarkScenesDirty(targets);
            return CreateSuccessResponse(("updated", updated), ("count", updated.Count));
        }

        private void ApplyRigidbody2DPreset(Rigidbody2D rb, string preset)
        {
            switch (preset)
            {
                case "dynamic":
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.mass = 1f;
                    rb.linearDamping = 0f;
                    rb.angularDamping = 0.05f;
                    rb.gravityScale = 1f;
                    rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
                    rb.constraints = RigidbodyConstraints2D.None;
                    break;

                case "kinematic":
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.gravityScale = 0f;
                    rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
                    break;

                case "static":
                    rb.bodyType = RigidbodyType2D.Static;
                    rb.gravityScale = 0f;
                    break;

                case "character":
                case "platformer":
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.mass = 1f;
                    rb.linearDamping = 0f;
                    rb.angularDamping = 0.05f;
                    rb.gravityScale = 3f;
                    rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                    break;

                case "topdown":
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.mass = 1f;
                    rb.linearDamping = 5f;
                    rb.angularDamping = 5f;
                    rb.gravityScale = 0f;
                    rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                    break;

                case "vehicle":
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.mass = 2f;
                    rb.linearDamping = 1f;
                    rb.angularDamping = 2f;
                    rb.gravityScale = 0f;
                    rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    break;

                case "projectile":
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.mass = 0.1f;
                    rb.linearDamping = 0f;
                    rb.angularDamping = 0f;
                    rb.gravityScale = 0.5f;
                    rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                    break;
            }
        }

        private void AddCollider2D(GameObject go, string colliderType, Dictionary<string, object> payload)
        {
            var isTrigger = GetBool(payload, "isTrigger");

            switch (colliderType)
            {
                case "box":
                    var boxCol = go.GetComponent<BoxCollider2D>();
                    if (boxCol == null)
                    {
                        boxCol = Undo.AddComponent<BoxCollider2D>(go);
                    }
                    boxCol.isTrigger = isTrigger;
                    if (payload.TryGetValue("size", out var sizeObj) && sizeObj is Dictionary<string, object> sizeDict)
                    {
                        boxCol.size = GetVector2FromDict(sizeDict, Vector2.one);
                    }
                    if (payload.TryGetValue("center", out var centerObj) && centerObj is Dictionary<string, object> centerDict)
                    {
                        boxCol.offset = GetVector2FromDict(centerDict, Vector2.zero);
                    }
                    break;

                case "circle":
                    var circleCol = go.GetComponent<CircleCollider2D>();
                    if (circleCol == null)
                    {
                        circleCol = Undo.AddComponent<CircleCollider2D>(go);
                    }
                    circleCol.isTrigger = isTrigger;
                    if (payload.TryGetValue("radius", out var radiusObj))
                    {
                        circleCol.radius = Convert.ToSingle(radiusObj);
                    }
                    if (payload.TryGetValue("center", out var circleCenterObj) && circleCenterObj is Dictionary<string, object> circleCenterDict)
                    {
                        circleCol.offset = GetVector2FromDict(circleCenterDict, Vector2.zero);
                    }
                    break;

                case "polygon":
                    var polyCol = go.GetComponent<PolygonCollider2D>();
                    if (polyCol == null)
                    {
                        polyCol = Undo.AddComponent<PolygonCollider2D>(go);
                    }
                    polyCol.isTrigger = isTrigger;
                    break;

                case "edge":
                    var edgeCol = go.GetComponent<EdgeCollider2D>();
                    if (edgeCol == null)
                    {
                        edgeCol = Undo.AddComponent<EdgeCollider2D>(go);
                    }
                    edgeCol.isTrigger = isTrigger;
                    break;
            }
        }

        #endregion

        #region Apply Preset 3D

        private object ApplyPreset3D(Dictionary<string, object> payload)
        {
            var targets = GetTargetGameObjects(payload);
            var preset = GetString(payload, "preset")?.ToLowerInvariant() ?? "dynamic";
            var colliderType = GetString(payload, "colliderType")?.ToLowerInvariant() ?? "box";

            var updated = new List<string>();

            foreach (var go in targets)
            {
                Undo.RecordObject(go, "Apply Physics Preset 3D");

                var rb = go.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = Undo.AddComponent<Rigidbody>(go);
                }

                ApplyRigidbody3DPreset(rb, preset);

                // Add collider if specified
                if (!string.IsNullOrEmpty(colliderType))
                {
                    AddCollider3D(go, colliderType, payload);
                }

                updated.Add(BuildGameObjectPath(go));
            }

            MarkScenesDirty(targets);
            return CreateSuccessResponse(("updated", updated), ("count", updated.Count));
        }

        private void ApplyRigidbody3DPreset(Rigidbody rb, string preset)
        {
            switch (preset)
            {
                case "dynamic":
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.mass = 1f;
                    rb.linearDamping = 0f;
                    rb.angularDamping = 0.05f;
                    rb.interpolation = RigidbodyInterpolation.None;
                    rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                    rb.constraints = RigidbodyConstraints.None;
                    break;

                case "kinematic":
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.interpolation = RigidbodyInterpolation.None;
                    rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                    break;

                case "static":
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.constraints = RigidbodyConstraints.FreezeAll;
                    break;

                case "character":
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.mass = 1f;
                    rb.linearDamping = 0f;
                    rb.angularDamping = 0.05f;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    rb.constraints = RigidbodyConstraints.FreezeRotation;
                    break;

                case "platformer":
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.mass = 1f;
                    rb.linearDamping = 0f;
                    rb.angularDamping = 0.05f;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                    break;

                case "topdown":
                    rb.isKinematic = false;
                    rb.useGravity = false;
                    rb.mass = 1f;
                    rb.linearDamping = 5f;
                    rb.angularDamping = 5f;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
                    break;

                case "vehicle":
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.mass = 1500f;
                    rb.linearDamping = 0.5f;
                    rb.angularDamping = 2f;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    rb.constraints = RigidbodyConstraints.None;
                    break;

                case "projectile":
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.mass = 0.1f;
                    rb.linearDamping = 0f;
                    rb.angularDamping = 0f;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    rb.constraints = RigidbodyConstraints.None;
                    break;
            }
        }

        private void AddCollider3D(GameObject go, string colliderType, Dictionary<string, object> payload)
        {
            var isTrigger = GetBool(payload, "isTrigger");

            switch (colliderType)
            {
                case "box":
                    var boxCol = go.GetComponent<BoxCollider>();
                    if (boxCol == null)
                    {
                        boxCol = Undo.AddComponent<BoxCollider>(go);
                    }
                    boxCol.isTrigger = isTrigger;
                    if (payload.TryGetValue("size", out var sizeObj) && sizeObj is Dictionary<string, object> sizeDict)
                    {
                        boxCol.size = GetVector3FromDict(sizeDict, Vector3.one);
                    }
                    if (payload.TryGetValue("center", out var centerObj) && centerObj is Dictionary<string, object> centerDict)
                    {
                        boxCol.center = GetVector3FromDict(centerDict, Vector3.zero);
                    }
                    break;

                case "sphere":
                    var sphereCol = go.GetComponent<SphereCollider>();
                    if (sphereCol == null)
                    {
                        sphereCol = Undo.AddComponent<SphereCollider>(go);
                    }
                    sphereCol.isTrigger = isTrigger;
                    if (payload.TryGetValue("radius", out var radiusObj))
                    {
                        sphereCol.radius = Convert.ToSingle(radiusObj);
                    }
                    if (payload.TryGetValue("center", out var sphereCenterObj) && sphereCenterObj is Dictionary<string, object> sphereCenterDict)
                    {
                        sphereCol.center = GetVector3FromDict(sphereCenterDict, Vector3.zero);
                    }
                    break;

                case "capsule":
                    var capsuleCol = go.GetComponent<CapsuleCollider>();
                    if (capsuleCol == null)
                    {
                        capsuleCol = Undo.AddComponent<CapsuleCollider>(go);
                    }
                    capsuleCol.isTrigger = isTrigger;
                    if (payload.TryGetValue("radius", out var capRadiusObj))
                    {
                        capsuleCol.radius = Convert.ToSingle(capRadiusObj);
                    }
                    if (payload.TryGetValue("height", out var heightObj))
                    {
                        capsuleCol.height = Convert.ToSingle(heightObj);
                    }
                    if (payload.TryGetValue("center", out var capCenterObj) && capCenterObj is Dictionary<string, object> capCenterDict)
                    {
                        capsuleCol.center = GetVector3FromDict(capCenterDict, Vector3.zero);
                    }
                    break;

                case "mesh":
                    var meshCol = go.GetComponent<MeshCollider>();
                    if (meshCol == null)
                    {
                        meshCol = Undo.AddComponent<MeshCollider>(go);
                    }
                    meshCol.isTrigger = isTrigger;
                    meshCol.convex = isTrigger; // Triggers must be convex
                    break;
            }
        }

        #endregion

        #region Update Rigidbody 2D

        private object UpdateRigidbody2D(Dictionary<string, object> payload)
        {
            var targets = GetTargetGameObjects(payload);
            var updated = new List<string>();

            foreach (var go in targets)
            {
                var rb = go.GetComponent<Rigidbody2D>();
                if (rb == null)
                {
                    throw new InvalidOperationException($"GameObject '{BuildGameObjectPath(go)}' does not have a Rigidbody2D component.");
                }

                Undo.RecordObject(rb, "Update Rigidbody2D");

                if (payload.TryGetValue("rigidbodyType", out var typeObj))
                {
                    var typeStr = typeObj.ToString().ToLowerInvariant();
                    rb.bodyType = typeStr switch
                    {
                        "dynamic" => RigidbodyType2D.Dynamic,
                        "kinematic" => RigidbodyType2D.Kinematic,
                        "static" => RigidbodyType2D.Static,
                        _ => rb.bodyType
                    };
                }

                if (payload.TryGetValue("mass", out var massObj))
                {
                    rb.mass = Convert.ToSingle(massObj);
                }

                if (payload.TryGetValue("drag", out var dragObj))
                {
                    rb.linearDamping = Convert.ToSingle(dragObj);
                }

                if (payload.TryGetValue("angularDrag", out var angDragObj))
                {
                    rb.angularDamping = Convert.ToSingle(angDragObj);
                }

                if (payload.TryGetValue("gravityScale", out var gravObj))
                {
                    rb.gravityScale = Convert.ToSingle(gravObj);
                }

                if (payload.TryGetValue("collisionDetection", out var colDetObj))
                {
                    var colDetStr = colDetObj.ToString().ToLowerInvariant();
                    rb.collisionDetectionMode = colDetStr switch
                    {
                        "discrete" => CollisionDetectionMode2D.Discrete,
                        "continuous" => CollisionDetectionMode2D.Continuous,
                        _ => rb.collisionDetectionMode
                    };
                }

                if (payload.TryGetValue("constraints", out var constraintsObj) && constraintsObj is Dictionary<string, object> constraintsDict)
                {
                    ApplyConstraints2D(rb, constraintsDict);
                }

                updated.Add(BuildGameObjectPath(go));
            }

            MarkScenesDirty(targets);
            return CreateSuccessResponse(("updated", updated), ("count", updated.Count));
        }

        private void ApplyConstraints2D(Rigidbody2D rb, Dictionary<string, object> constraints)
        {
            var newConstraints = RigidbodyConstraints2D.None;

            if (constraints.TryGetValue("freezePositionX", out var freezePosX) && Convert.ToBoolean(freezePosX))
            {
                newConstraints |= RigidbodyConstraints2D.FreezePositionX;
            }

            if (constraints.TryGetValue("freezePositionY", out var freezePosY) && Convert.ToBoolean(freezePosY))
            {
                newConstraints |= RigidbodyConstraints2D.FreezePositionY;
            }

            if (constraints.TryGetValue("freezeRotationZ", out var freezeRotZ) && Convert.ToBoolean(freezeRotZ))
            {
                newConstraints |= RigidbodyConstraints2D.FreezeRotation;
            }

            rb.constraints = newConstraints;
        }

        #endregion

        #region Update Rigidbody 3D

        private object UpdateRigidbody3D(Dictionary<string, object> payload)
        {
            var targets = GetTargetGameObjects(payload);
            var updated = new List<string>();

            foreach (var go in targets)
            {
                var rb = go.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    throw new InvalidOperationException($"GameObject '{BuildGameObjectPath(go)}' does not have a Rigidbody component.");
                }

                Undo.RecordObject(rb, "Update Rigidbody");

                if (payload.TryGetValue("isKinematic", out var kinematicObj))
                {
                    rb.isKinematic = Convert.ToBoolean(kinematicObj);
                }

                if (payload.TryGetValue("useGravity", out var gravObj))
                {
                    rb.useGravity = Convert.ToBoolean(gravObj);
                }

                if (payload.TryGetValue("mass", out var massObj))
                {
                    rb.mass = Convert.ToSingle(massObj);
                }

                if (payload.TryGetValue("drag", out var dragObj))
                {
                    rb.linearDamping = Convert.ToSingle(dragObj);
                }

                if (payload.TryGetValue("angularDrag", out var angDragObj))
                {
                    rb.angularDamping = Convert.ToSingle(angDragObj);
                }

                if (payload.TryGetValue("interpolate", out var interpObj))
                {
                    var interpStr = interpObj.ToString().ToLowerInvariant();
                    rb.interpolation = interpStr switch
                    {
                        "none" => RigidbodyInterpolation.None,
                        "interpolate" => RigidbodyInterpolation.Interpolate,
                        "extrapolate" => RigidbodyInterpolation.Extrapolate,
                        _ => rb.interpolation
                    };
                }

                if (payload.TryGetValue("collisionDetection", out var colDetObj))
                {
                    var colDetStr = colDetObj.ToString().ToLowerInvariant();
                    rb.collisionDetectionMode = colDetStr switch
                    {
                        "discrete" => CollisionDetectionMode.Discrete,
                        "continuous" => CollisionDetectionMode.Continuous,
                        "continuousdynamic" => CollisionDetectionMode.ContinuousDynamic,
                        "continuousspeculative" => CollisionDetectionMode.ContinuousSpeculative,
                        _ => rb.collisionDetectionMode
                    };
                }

                if (payload.TryGetValue("constraints", out var constraintsObj) && constraintsObj is Dictionary<string, object> constraintsDict)
                {
                    ApplyConstraints3D(rb, constraintsDict);
                }

                updated.Add(BuildGameObjectPath(go));
            }

            MarkScenesDirty(targets);
            return CreateSuccessResponse(("updated", updated), ("count", updated.Count));
        }

        private void ApplyConstraints3D(Rigidbody rb, Dictionary<string, object> constraints)
        {
            var newConstraints = RigidbodyConstraints.None;

            if (constraints.TryGetValue("freezePositionX", out var freezePosX) && Convert.ToBoolean(freezePosX))
            {
                newConstraints |= RigidbodyConstraints.FreezePositionX;
            }

            if (constraints.TryGetValue("freezePositionY", out var freezePosY) && Convert.ToBoolean(freezePosY))
            {
                newConstraints |= RigidbodyConstraints.FreezePositionY;
            }

            if (constraints.TryGetValue("freezePositionZ", out var freezePosZ) && Convert.ToBoolean(freezePosZ))
            {
                newConstraints |= RigidbodyConstraints.FreezePositionZ;
            }

            if (constraints.TryGetValue("freezeRotationX", out var freezeRotX) && Convert.ToBoolean(freezeRotX))
            {
                newConstraints |= RigidbodyConstraints.FreezeRotationX;
            }

            if (constraints.TryGetValue("freezeRotationY", out var freezeRotY) && Convert.ToBoolean(freezeRotY))
            {
                newConstraints |= RigidbodyConstraints.FreezeRotationY;
            }

            if (constraints.TryGetValue("freezeRotationZ", out var freezeRotZ) && Convert.ToBoolean(freezeRotZ))
            {
                newConstraints |= RigidbodyConstraints.FreezeRotationZ;
            }

            rb.constraints = newConstraints;
        }

        #endregion

        #region Update Collider 2D

        private object UpdateCollider2D(Dictionary<string, object> payload)
        {
            var targets = GetTargetGameObjects(payload);
            var updated = new List<string>();

            foreach (var go in targets)
            {
                var colliders = go.GetComponents<Collider2D>();
                if (colliders.Length == 0)
                {
                    throw new InvalidOperationException($"GameObject '{BuildGameObjectPath(go)}' does not have any Collider2D components.");
                }

                foreach (var col in colliders)
                {
                    Undo.RecordObject(col, "Update Collider2D");

                    if (payload.TryGetValue("isTrigger", out var triggerObj))
                    {
                        col.isTrigger = Convert.ToBoolean(triggerObj);
                    }

                    // Type-specific updates
                    if (col is BoxCollider2D boxCol)
                    {
                        if (payload.TryGetValue("size", out var sizeObj) && sizeObj is Dictionary<string, object> sizeDict)
                        {
                            boxCol.size = GetVector2FromDict(sizeDict, boxCol.size);
                        }
                        if (payload.TryGetValue("center", out var centerObj) && centerObj is Dictionary<string, object> centerDict)
                        {
                            boxCol.offset = GetVector2FromDict(centerDict, boxCol.offset);
                        }
                    }
                    else if (col is CircleCollider2D circleCol)
                    {
                        if (payload.TryGetValue("radius", out var radiusObj))
                        {
                            circleCol.radius = Convert.ToSingle(radiusObj);
                        }
                        if (payload.TryGetValue("center", out var centerObj) && centerObj is Dictionary<string, object> centerDict)
                        {
                            circleCol.offset = GetVector2FromDict(centerDict, circleCol.offset);
                        }
                    }
                }

                updated.Add(BuildGameObjectPath(go));
            }

            MarkScenesDirty(targets);
            return CreateSuccessResponse(("updated", updated), ("count", updated.Count));
        }

        #endregion

        #region Update Collider 3D

        private object UpdateCollider3D(Dictionary<string, object> payload)
        {
            var targets = GetTargetGameObjects(payload);
            var updated = new List<string>();

            foreach (var go in targets)
            {
                var colliders = go.GetComponents<Collider>();
                if (colliders.Length == 0)
                {
                    throw new InvalidOperationException($"GameObject '{BuildGameObjectPath(go)}' does not have any Collider components.");
                }

                foreach (var col in colliders)
                {
                    Undo.RecordObject(col, "Update Collider");

                    if (payload.TryGetValue("isTrigger", out var triggerObj))
                    {
                        col.isTrigger = Convert.ToBoolean(triggerObj);
                    }

                    // Type-specific updates
                    if (col is BoxCollider boxCol)
                    {
                        if (payload.TryGetValue("size", out var sizeObj) && sizeObj is Dictionary<string, object> sizeDict)
                        {
                            boxCol.size = GetVector3FromDict(sizeDict, boxCol.size);
                        }
                        if (payload.TryGetValue("center", out var centerObj) && centerObj is Dictionary<string, object> centerDict)
                        {
                            boxCol.center = GetVector3FromDict(centerDict, boxCol.center);
                        }
                    }
                    else if (col is SphereCollider sphereCol)
                    {
                        if (payload.TryGetValue("radius", out var radiusObj))
                        {
                            sphereCol.radius = Convert.ToSingle(radiusObj);
                        }
                        if (payload.TryGetValue("center", out var centerObj) && centerObj is Dictionary<string, object> centerDict)
                        {
                            sphereCol.center = GetVector3FromDict(centerDict, sphereCol.center);
                        }
                    }
                    else if (col is CapsuleCollider capsuleCol)
                    {
                        if (payload.TryGetValue("radius", out var radiusObj))
                        {
                            capsuleCol.radius = Convert.ToSingle(radiusObj);
                        }
                        if (payload.TryGetValue("height", out var heightObj))
                        {
                            capsuleCol.height = Convert.ToSingle(heightObj);
                        }
                        if (payload.TryGetValue("center", out var centerObj) && centerObj is Dictionary<string, object> centerDict)
                        {
                            capsuleCol.center = GetVector3FromDict(centerDict, capsuleCol.center);
                        }
                    }
                    else if (col is MeshCollider meshCol)
                    {
                        if (payload.TryGetValue("isTrigger", out var meshTriggerObj))
                        {
                            var isTrigger = Convert.ToBoolean(meshTriggerObj);
                            meshCol.isTrigger = isTrigger;
                            meshCol.convex = isTrigger; // Triggers must be convex
                        }
                    }
                }

                updated.Add(BuildGameObjectPath(go));
            }

            MarkScenesDirty(targets);
            return CreateSuccessResponse(("updated", updated), ("count", updated.Count));
        }

        #endregion

        #region Inspect

        private object InspectPhysics(Dictionary<string, object> payload)
        {
            var targets = GetTargetGameObjects(payload);
            var results = new List<Dictionary<string, object>>();

            foreach (var go in targets)
            {
                var info = new Dictionary<string, object>
                {
                    { "path", BuildGameObjectPath(go) },
                    { "name", go.name }
                };

                // Check for Rigidbody2D
                var rb2D = go.GetComponent<Rigidbody2D>();
                if (rb2D != null)
                {
                    info["rigidbody2D"] = SerializeRigidbody2D(rb2D);
                }

                // Check for Rigidbody
                var rb3D = go.GetComponent<Rigidbody>();
                if (rb3D != null)
                {
                    info["rigidbody3D"] = SerializeRigidbody3D(rb3D);
                }

                // Check for Collider2D
                var colliders2D = go.GetComponents<Collider2D>();
                if (colliders2D.Length > 0)
                {
                    var col2DList = new List<Dictionary<string, object>>();
                    foreach (var col in colliders2D)
                    {
                        col2DList.Add(SerializeCollider2D(col));
                    }
                    info["colliders2D"] = col2DList;
                }

                // Check for Collider
                var colliders3D = go.GetComponents<Collider>();
                if (colliders3D.Length > 0)
                {
                    var col3DList = new List<Dictionary<string, object>>();
                    foreach (var col in colliders3D)
                    {
                        col3DList.Add(SerializeCollider3D(col));
                    }
                    info["colliders3D"] = col3DList;
                }

                results.Add(info);
            }

            return CreateSuccessResponse(("objects", results), ("count", results.Count));
        }

        private Dictionary<string, object> SerializeRigidbody2D(Rigidbody2D rb)
        {
            return new Dictionary<string, object>
            {
                { "bodyType", rb.bodyType.ToString() },
                { "mass", rb.mass },
                { "drag", rb.linearDamping },
                { "angularDrag", rb.angularDamping },
                { "gravityScale", rb.gravityScale },
                { "collisionDetectionMode", rb.collisionDetectionMode.ToString() },
                { "constraints", rb.constraints.ToString() }
            };
        }

        private Dictionary<string, object> SerializeRigidbody3D(Rigidbody rb)
        {
            return new Dictionary<string, object>
            {
                { "isKinematic", rb.isKinematic },
                { "useGravity", rb.useGravity },
                { "mass", rb.mass },
                { "drag", rb.linearDamping },
                { "angularDrag", rb.angularDamping },
                { "interpolation", rb.interpolation.ToString() },
                { "collisionDetectionMode", rb.collisionDetectionMode.ToString() },
                { "constraints", rb.constraints.ToString() }
            };
        }

        private Dictionary<string, object> SerializeCollider2D(Collider2D col)
        {
            var info = new Dictionary<string, object>
            {
                { "type", col.GetType().Name },
                { "isTrigger", col.isTrigger }
            };

            if (col is BoxCollider2D boxCol)
            {
                info["size"] = new Dictionary<string, object> { { "x", boxCol.size.x }, { "y", boxCol.size.y } };
                info["offset"] = new Dictionary<string, object> { { "x", boxCol.offset.x }, { "y", boxCol.offset.y } };
            }
            else if (col is CircleCollider2D circleCol)
            {
                info["radius"] = circleCol.radius;
                info["offset"] = new Dictionary<string, object> { { "x", circleCol.offset.x }, { "y", circleCol.offset.y } };
            }

            return info;
        }

        private Dictionary<string, object> SerializeCollider3D(Collider col)
        {
            var info = new Dictionary<string, object>
            {
                { "type", col.GetType().Name },
                { "isTrigger", col.isTrigger }
            };

            if (col is BoxCollider boxCol)
            {
                info["size"] = new Dictionary<string, object> { { "x", boxCol.size.x }, { "y", boxCol.size.y }, { "z", boxCol.size.z } };
                info["center"] = new Dictionary<string, object> { { "x", boxCol.center.x }, { "y", boxCol.center.y }, { "z", boxCol.center.z } };
            }
            else if (col is SphereCollider sphereCol)
            {
                info["radius"] = sphereCol.radius;
                info["center"] = new Dictionary<string, object> { { "x", sphereCol.center.x }, { "y", sphereCol.center.y }, { "z", sphereCol.center.z } };
            }
            else if (col is CapsuleCollider capsuleCol)
            {
                info["radius"] = capsuleCol.radius;
                info["height"] = capsuleCol.height;
                info["center"] = new Dictionary<string, object> { { "x", capsuleCol.center.x }, { "y", capsuleCol.center.y }, { "z", capsuleCol.center.z } };
            }
            else if (col is MeshCollider meshCol)
            {
                info["convex"] = meshCol.convex;
            }

            return info;
        }

        #endregion

        #region Helpers

        private List<GameObject> GetTargetGameObjects(Dictionary<string, object> payload)
        {
            var paths = GetStringList(payload, "gameObjectPaths");
            var result = new List<GameObject>();
            foreach (var path in paths)
            {
                var go = ResolveGameObject(path);
                result.Add(go);
            }
            return result;
        }

        #endregion
    }
}

