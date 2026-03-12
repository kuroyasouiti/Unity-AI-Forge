using System;
using System.Collections.Generic;
using System.IO;
using MCP.Editor.Base;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Physics Bundle Handler: Apply physics presets, configure collision matrices,
    /// and create physics materials.
    /// </summary>
    public class PhysicsBundleHandler : BaseCommandHandler
    {
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "applyPreset",
            "setCollisionMatrix",
            "setCollisionMatrixBatch",
            "createPhysicsMaterial",
            "createPhysicsMaterial2D",
            "inspect"
        };

        public override string Category => "physicsBundle";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "applyPreset" => HandleApplyPreset(payload),
                "setCollisionMatrix" => HandleSetCollisionMatrix(payload),
                "setCollisionMatrixBatch" => HandleSetCollisionMatrixBatch(payload),
                "createPhysicsMaterial" => HandleCreatePhysicsMaterial(payload),
                "createPhysicsMaterial2D" => HandleCreatePhysicsMaterial2D(payload),
                "inspect" => HandleInspect(payload),
                _ => throw new ArgumentException($"Unknown operation: {operation}")
            };
        }

        private object HandleApplyPreset(Dictionary<string, object> payload)
        {
            var goPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(goPath))
                throw new ArgumentException("Required parameter missing: gameObjectPath");
            var preset = GetString(payload, "preset");
            if (string.IsNullOrEmpty(preset))
                throw new ArgumentException("Required parameter missing: preset");
            var go = ResolveGameObject(goPath);
            var reportChanges = GetBool(payload, "reportChanges", false);

            // Capture before-state for change report
            Dictionary<string, object> beforeState = null;
            if (reportChanges)
            {
                beforeState = new Dictionary<string, object>
                {
                    { "tag", go.tag },
                    { "layer", LayerMask.LayerToName(go.layer) },
                    { "hasRigidbody", go.GetComponent<Rigidbody>() != null },
                    { "hasRigidbody2D", go.GetComponent<Rigidbody2D>() != null },
                    { "hasCharacterController", go.GetComponent<CharacterController>() != null },
                    { "hasBoxCollider", go.GetComponent<BoxCollider>() != null },
                    { "hasBoxCollider2D", go.GetComponent<BoxCollider2D>() != null },
                    { "hasCapsuleCollider", go.GetComponent<CapsuleCollider>() != null },
                    { "hasSphereCollider", go.GetComponent<SphereCollider>() != null },
                    { "hasCircleCollider2D", go.GetComponent<CircleCollider2D>() != null }
                };
            }

            switch (preset)
            {
                case "platformer2D":
                    ApplyPlatformer2D(go);
                    break;
                case "topDown2D":
                    ApplyTopDown2D(go);
                    break;
                case "fps3D":
                    ApplyFps3D(go);
                    break;
                case "thirdPerson3D":
                    ApplyThirdPerson3D(go);
                    break;
                case "space":
                    ApplySpace(go);
                    break;
                case "racing":
                    ApplyRacing(go);
                    break;
                default:
                    throw new ArgumentException($"Unknown preset: {preset}. Available: platformer2D, topDown2D, fps3D, thirdPerson3D, space, racing");
            }

            var result = CreateSuccessResponse(("gameObject", goPath), ("preset", preset));

            if (reportChanges && beforeState != null)
            {
                var afterState = new Dictionary<string, object>
                {
                    { "tag", go.tag },
                    { "layer", LayerMask.LayerToName(go.layer) },
                    { "hasRigidbody", go.GetComponent<Rigidbody>() != null },
                    { "hasRigidbody2D", go.GetComponent<Rigidbody2D>() != null },
                    { "hasCharacterController", go.GetComponent<CharacterController>() != null },
                    { "hasBoxCollider", go.GetComponent<BoxCollider>() != null },
                    { "hasBoxCollider2D", go.GetComponent<BoxCollider2D>() != null },
                    { "hasCapsuleCollider", go.GetComponent<CapsuleCollider>() != null },
                    { "hasSphereCollider", go.GetComponent<SphereCollider>() != null },
                    { "hasCircleCollider2D", go.GetComponent<CircleCollider2D>() != null }
                };

                var changes = new Dictionary<string, object>();
                foreach (var kv in beforeState)
                {
                    var afterVal = afterState.ContainsKey(kv.Key) ? afterState[kv.Key] : null;
                    if (!Equals(kv.Value, afterVal))
                        changes[kv.Key] = new Dictionary<string, object> { { "before", kv.Value }, { "after", afterVal } };
                }
                result["changes"] = changes;

                if (beforeState["tag"].ToString() != afterState["tag"].ToString() ||
                    beforeState["layer"].ToString() != afterState["layer"].ToString())
                {
                    result["warning"] = "tag and/or layer may have been affected by preset. Verify and re-set with gameobject_crud(update) if needed.";
                }
            }

            return result;
        }

        private void ApplyPlatformer2D(GameObject go)
        {
            var rb = Undo.AddComponent<Rigidbody2D>(go);
            rb.gravityScale = 3f;
            rb.mass = 1f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var col = Undo.AddComponent<BoxCollider2D>(go);
            col.size = new Vector2(1f, 1f);
        }

        private void ApplyTopDown2D(GameObject go)
        {
            var rb = Undo.AddComponent<Rigidbody2D>(go);
            rb.gravityScale = 0f;
            rb.mass = 1f;
            rb.linearDamping = 5f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = Undo.AddComponent<CircleCollider2D>(go);
            col.radius = 0.5f;
        }

        private void ApplyFps3D(GameObject go)
        {
            var cc = Undo.AddComponent<CharacterController>(go);
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.slopeLimit = 45f;
            cc.stepOffset = 0.3f;
            cc.skinWidth = 0.08f;
        }

        private void ApplyThirdPerson3D(GameObject go)
        {
            var rb = Undo.AddComponent<Rigidbody>(go);
            rb.mass = 70f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.05f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var col = Undo.AddComponent<CapsuleCollider>(go);
            col.height = 2f;
            col.radius = 0.5f;
            col.center = new Vector3(0f, 1f, 0f);
        }

        private void ApplySpace(GameObject go)
        {
            var rb = Undo.AddComponent<Rigidbody>(go);
            rb.mass = 1f;
            rb.useGravity = false;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;

            var col = Undo.AddComponent<SphereCollider>(go);
            col.radius = 0.5f;
        }

        private void ApplyRacing(GameObject go)
        {
            var rb = Undo.AddComponent<Rigidbody>(go);
            rb.mass = 1500f;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            var col = Undo.AddComponent<BoxCollider>(go);
            col.size = new Vector3(2f, 1f, 4f);
            col.center = new Vector3(0f, 0.5f, 0f);
        }

        private object HandleSetCollisionMatrix(Dictionary<string, object> payload)
        {
            var layerA = GetString(payload, "layerA");
            if (string.IsNullOrEmpty(layerA))
                throw new ArgumentException("Required parameter missing: layerA");
            var layerB = GetString(payload, "layerB");
            if (string.IsNullOrEmpty(layerB))
                throw new ArgumentException("Required parameter missing: layerB");
            var ignore = GetBool(payload, "ignore", true);
            var is2D = GetBool(payload, "is2D", false);

            int layerIndexA = LayerMask.NameToLayer(layerA);
            int layerIndexB = LayerMask.NameToLayer(layerB);

            if (layerIndexA < 0) throw new ArgumentException($"Layer not found: {layerA}");
            if (layerIndexB < 0) throw new ArgumentException($"Layer not found: {layerB}");

            if (is2D)
                Physics2D.IgnoreLayerCollision(layerIndexA, layerIndexB, ignore);
            else
                Physics.IgnoreLayerCollision(layerIndexA, layerIndexB, ignore);

            return CreateSuccessResponse(
                ("layerA", layerA),
                ("layerB", layerB),
                ("ignore", ignore),
                ("is2D", is2D),
                ("message", $"Collision between '{layerA}' and '{layerB}' {(ignore ? "ignored" : "enabled")} ({(is2D ? "2D" : "3D")})")
            );
        }

        private object HandleSetCollisionMatrixBatch(Dictionary<string, object> payload)
        {
            var pairs = GetListFromPayload(payload, "pairs");
            if (pairs == null || pairs.Count == 0)
                throw new ArgumentException("Required parameter missing: pairs (array of {layerA, layerB, ignore?})");

            var is2D = GetBool(payload, "is2D", false);
            var results = new List<Dictionary<string, object>>();
            int successCount = 0;
            int errorCount = 0;

            foreach (var item in pairs)
            {
                if (item is not Dictionary<string, object> pair) continue;

                var layerA = pair.ContainsKey("layerA") ? pair["layerA"]?.ToString() : null;
                var layerB = pair.ContainsKey("layerB") ? pair["layerB"]?.ToString() : null;
                var ignore = pair.ContainsKey("ignore") ? Convert.ToBoolean(pair["ignore"]) : true;

                if (string.IsNullOrEmpty(layerA) || string.IsNullOrEmpty(layerB))
                {
                    results.Add(new Dictionary<string, object>
                    {
                        ["layerA"] = layerA ?? "",
                        ["layerB"] = layerB ?? "",
                        ["success"] = false,
                        ["error"] = "layerA and layerB are required"
                    });
                    errorCount++;
                    continue;
                }

                int indexA = LayerMask.NameToLayer(layerA);
                int indexB = LayerMask.NameToLayer(layerB);

                if (indexA < 0 || indexB < 0)
                {
                    results.Add(new Dictionary<string, object>
                    {
                        ["layerA"] = layerA,
                        ["layerB"] = layerB,
                        ["success"] = false,
                        ["error"] = $"Layer not found: {(indexA < 0 ? layerA : layerB)}"
                    });
                    errorCount++;
                    continue;
                }

                if (is2D)
                    Physics2D.IgnoreLayerCollision(indexA, indexB, ignore);
                else
                    Physics.IgnoreLayerCollision(indexA, indexB, ignore);

                results.Add(new Dictionary<string, object>
                {
                    ["layerA"] = layerA,
                    ["layerB"] = layerB,
                    ["ignore"] = ignore,
                    ["success"] = true
                });
                successCount++;
            }

            return CreateSuccessResponse(
                ("results", results),
                ("is2D", is2D),
                ("totalCount", pairs.Count),
                ("successCount", successCount),
                ("errorCount", errorCount),
                ("message", $"{successCount} collision pairs configured ({(is2D ? "2D" : "3D")})")
            );
        }

        private object HandleCreatePhysicsMaterial(Dictionary<string, object> payload)
        {
            var materialPath = GetString(payload, "materialPath");
            if (string.IsNullOrEmpty(materialPath))
                throw new ArgumentException("Required parameter missing: materialPath");
            EnsureDirectoryExists(materialPath);

            var mat = new PhysicsMaterial();
            mat.dynamicFriction = GetFloat(payload, "dynamicFriction", 0.6f);
            mat.staticFriction = GetFloat(payload, "staticFriction", 0.6f);
            mat.bounciness = GetFloat(payload, "bounciness", 0f);

            if (payload.ContainsKey("frictionCombine"))
                mat.frictionCombine = ParseCombine(GetString(payload, "frictionCombine"));
            if (payload.ContainsKey("bounceCombine"))
                mat.bounceCombine = ParseCombine(GetString(payload, "bounceCombine"));

            AssetDatabase.CreateAsset(mat, materialPath);
            AssetDatabase.SaveAssets();

            // Optionally assign to collider
            if (payload.ContainsKey("assignTo"))
            {
                var assignPath = payload["assignTo"].ToString();
                var assignGo = ResolveGameObject(assignPath);
                var collider = assignGo.GetComponent<Collider>();
                if (collider != null)
                {
                    Undo.RecordObject(collider, "Assign PhysicsMaterial");
                    collider.material = mat;
                }
            }

            return CreateSuccessResponse(
                ("assetPath", materialPath),
                ("dynamicFriction", mat.dynamicFriction),
                ("staticFriction", mat.staticFriction),
                ("bounciness", mat.bounciness)
            );
        }

        private object HandleCreatePhysicsMaterial2D(Dictionary<string, object> payload)
        {
            var materialPath = GetString(payload, "materialPath");
            if (string.IsNullOrEmpty(materialPath))
                throw new ArgumentException("Required parameter missing: materialPath");
            EnsureDirectoryExists(materialPath);

            var mat = new PhysicsMaterial2D();
            mat.friction = GetFloat(payload, "friction", 0.4f);
            mat.bounciness = GetFloat(payload, "bounciness", 0f);

            AssetDatabase.CreateAsset(mat, materialPath);
            AssetDatabase.SaveAssets();

            // Optionally assign to collider
            if (payload.ContainsKey("assignTo"))
            {
                var assignPath = payload["assignTo"].ToString();
                var assignGo = ResolveGameObject(assignPath);
                var collider = assignGo.GetComponent<Collider2D>();
                if (collider != null)
                {
                    Undo.RecordObject(collider, "Assign PhysicsMaterial2D");
                    collider.sharedMaterial = mat;
                }
            }

            return CreateSuccessResponse(
                ("assetPath", materialPath),
                ("friction", mat.friction),
                ("bounciness", mat.bounciness)
            );
        }

        private object HandleInspect(Dictionary<string, object> payload)
        {
            var goPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(goPath))
                throw new ArgumentException("Required parameter missing: gameObjectPath");
            var go = ResolveGameObject(goPath);

            var result = CreateSuccessResponse(("gameObject", goPath));

            // 3D Physics
            var rb3d = go.GetComponent<Rigidbody>();
            if (rb3d != null)
            {
                result["rigidbody"] = new
                {
                    mass = rb3d.mass,
                    drag = rb3d.linearDamping,
                    angularDrag = rb3d.angularDamping,
                    useGravity = rb3d.useGravity,
                    isKinematic = rb3d.isKinematic,
                    interpolation = rb3d.interpolation.ToString(),
                    constraints = rb3d.constraints.ToString()
                };
            }

            // 2D Physics
            var rb2d = go.GetComponent<Rigidbody2D>();
            if (rb2d != null)
            {
                result["rigidbody2D"] = new
                {
                    mass = rb2d.mass,
                    gravityScale = rb2d.gravityScale,
                    drag = rb2d.linearDamping,
                    angularDrag = rb2d.angularDamping,
                    bodyType = rb2d.bodyType.ToString(),
                    collisionDetection = rb2d.collisionDetectionMode.ToString(),
                    constraints = rb2d.constraints.ToString()
                };
            }

            // CharacterController
            var cc = go.GetComponent<CharacterController>();
            if (cc != null)
            {
                result["characterController"] = new
                {
                    height = cc.height,
                    radius = cc.radius,
                    center = SerializeVector3(cc.center),
                    slopeLimit = cc.slopeLimit,
                    stepOffset = cc.stepOffset,
                    skinWidth = cc.skinWidth
                };
            }

            // 3D Colliders
            var colliders3d = go.GetComponents<Collider>();
            if (colliders3d.Length > 0)
            {
                var colList = new List<object>();
                foreach (var col in colliders3d)
                {
                    var colInfo = new Dictionary<string, object>
                    {
                        ["type"] = col.GetType().Name,
                        ["isTrigger"] = col.isTrigger
                    };
                    if (col.material != null)
                    {
                        colInfo["material"] = new
                        {
                            name = col.material.name,
                            dynamicFriction = col.material.dynamicFriction,
                            staticFriction = col.material.staticFriction,
                            bounciness = col.material.bounciness
                        };
                    }
                    colList.Add(colInfo);
                }
                result["colliders"] = colList;
            }

            // 2D Colliders
            var colliders2d = go.GetComponents<Collider2D>();
            if (colliders2d.Length > 0)
            {
                var colList = new List<object>();
                foreach (var col in colliders2d)
                {
                    var colInfo = new Dictionary<string, object>
                    {
                        ["type"] = col.GetType().Name,
                        ["isTrigger"] = col.isTrigger
                    };
                    if (col.sharedMaterial != null)
                    {
                        colInfo["material"] = new
                        {
                            name = col.sharedMaterial.name,
                            friction = col.sharedMaterial.friction,
                            bounciness = col.sharedMaterial.bounciness
                        };
                    }
                    colList.Add(colInfo);
                }
                result["colliders2D"] = colList;
            }

            return result;
        }

        // ── Helpers ──────────────────────────────────────────────

        private static PhysicsMaterialCombine ParseCombine(string value)
        {
            return value switch
            {
                "Average" => PhysicsMaterialCombine.Average,
                "Minimum" => PhysicsMaterialCombine.Minimum,
                "Maximum" => PhysicsMaterialCombine.Maximum,
                "Multiply" => PhysicsMaterialCombine.Multiply,
                _ => throw new ArgumentException($"Unknown combine mode: {value}")
            };
        }

        private static void EnsureDirectoryExists(string assetPath)
        {
            var dir = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }
        }
    }
}
