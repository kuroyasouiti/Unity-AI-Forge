using System;
using System.Collections.Generic;
using System.Reflection;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Projectile handler: create and manage projectiles for games.
    /// Uses code generation to produce standalone Projectile scripts with zero package dependency.
    /// Supports bullets, missiles, homing projectiles, and bouncing projectiles.
    /// </summary>
    public class GameKitProjectileHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "update", "inspect", "delete",
            "launch", "setHomingTarget", "destroy", "findByProjectileId"
        };

        public override string Category => "gamekitProjectile";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateProjectile(payload),
                "update" => UpdateProjectile(payload),
                "inspect" => InspectProjectile(payload),
                "delete" => DeleteProjectile(payload),
                "launch" => LaunchProjectile(payload),
                "setHomingTarget" => SetHomingTarget(payload),
                "destroy" => DestroyProjectile(payload),
                "findByProjectileId" => FindByProjectileId(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit Projectile operation: {operation}")
            };
        }

        #region Create

        private object CreateProjectile(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            GameObject targetGo;

            if (string.IsNullOrEmpty(targetPath))
            {
                // Create a new GameObject
                var name = GetString(payload, "name") ?? "Projectile";
                targetGo = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(targetGo, "Create Projectile");

                // Set position if provided
                if (payload.TryGetValue("position", out var posObj) && posObj is Dictionary<string, object> posDict)
                {
                    targetGo.transform.position = GetVector3FromDict(posDict, Vector3.zero);
                }
            }
            else
            {
                targetGo = ResolveGameObject(targetPath);
                if (targetGo == null)
                {
                    throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
                }
            }

            // Check if already has a projectile component (by checking for projectileId field)
            var existingProjectile = CodeGenHelper.FindComponentByField(targetGo, "projectileId", null);
            if (existingProjectile != null)
            {
                throw new InvalidOperationException(
                    $"GameObject '{BuildGameObjectPath(targetGo)}' already has a Projectile component.");
            }

            var projectileId = GetString(payload, "projectileId") ?? $"Projectile_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var movementType = ParseMovementType(GetString(payload, "movementType") ?? "transform");
            var speed = GetFloat(payload, "speed", 20f);
            var lifetime = GetFloat(payload, "lifetime", 5f);
            var useGravity = GetBool(payload, "useGravity", false);
            var gravityScale = GetFloat(payload, "gravityScale", 1f);
            var damage = GetFloat(payload, "damage", 10f);
            var damageOnHit = GetBool(payload, "damageOnHit", true);
            var targetTag = GetString(payload, "targetTag") ?? "";
            var canBounce = GetBool(payload, "canBounce", false);
            var maxBounces = GetInt(payload, "maxBounces", 3);
            var bounciness = GetFloat(payload, "bounciness", 0.8f);
            var isHoming = GetBool(payload, "isHoming", false);
            var homingStrength = GetFloat(payload, "homingStrength", 5f);
            var maxHomingAngle = GetFloat(payload, "maxHomingAngle", 90f);
            var canPierce = GetBool(payload, "canPierce", false);
            var maxPierceCount = GetInt(payload, "maxPierceCount", 1);
            var pierceDamageReduction = GetFloat(payload, "pierceDamageReduction", 0.25f);
            var effectDuration = GetFloat(payload, "effectDuration", 0f);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(projectileId, "Projectile");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "PROJECTILE_ID", projectileId },
                { "MOVEMENT_TYPE", movementType },
                { "SPEED", speed },
                { "LIFETIME", lifetime },
                { "USE_GRAVITY", useGravity },
                { "GRAVITY_SCALE", gravityScale },
                { "DAMAGE", damage },
                { "DAMAGE_ON_HIT", damageOnHit },
                { "TARGET_TAG", targetTag },
                { "CAN_BOUNCE", canBounce },
                { "MAX_BOUNCES", maxBounces },
                { "BOUNCINESS", bounciness },
                { "IS_HOMING", isHoming },
                { "HOMING_STRENGTH", homingStrength },
                { "MAX_HOMING_ANGLE", maxHomingAngle },
                { "CAN_PIERCE", canPierce },
                { "MAX_PIERCE_COUNT", maxPierceCount },
                { "PIERCE_DAMAGE_REDUCTION", pierceDamageReduction },
                { "EFFECT_DURATION", effectDuration }
            };

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "Projectile", projectileId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate Projectile script.");
            }

            // Add appropriate physics components based on movement type
            var is2D = movementType == "Rigidbody2D";

            if (movementType == "Rigidbody" && targetGo.GetComponent<Rigidbody>() == null)
            {
                var rb = Undo.AddComponent<Rigidbody>(targetGo);
                rb.useGravity = useGravity;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
            else if (is2D && targetGo.GetComponent<Rigidbody2D>() == null)
            {
                var rb2d = Undo.AddComponent<Rigidbody2D>(targetGo);
                rb2d.gravityScale = useGravity ? gravityScale : 0f;
                rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            // Add collider if none exists
            if (targetGo.GetComponent<Collider>() == null && targetGo.GetComponent<Collider2D>() == null)
            {
                if (is2D)
                {
                    var collider = Undo.AddComponent<CircleCollider2D>(targetGo);
                    collider.isTrigger = GetBool(payload, "isTrigger", true);
                    collider.radius = GetFloat(payload, "colliderRadius", 0.25f);
                }
                else
                {
                    var collider = Undo.AddComponent<SphereCollider>(targetGo);
                    collider.isTrigger = GetBool(payload, "isTrigger", true);
                    collider.radius = GetFloat(payload, "colliderRadius", 0.25f);
                }
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["projectileId"] = projectileId;
            result["path"] = BuildGameObjectPath(targetGo);
            result["movementType"] = movementType;
            result["speed"] = speed;
            result["damage"] = damage;

            return result;
        }

        #endregion

        #region Update

        private object UpdateProjectile(Dictionary<string, object> payload)
        {
            var component = ResolveProjectileComponent(payload);

            Undo.RecordObject(component, "Update Projectile");

            var so = new SerializedObject(component);

            if (payload.TryGetValue("movementType", out var moveTypeObj))
            {
                var movementType = ParseMovementType(moveTypeObj.ToString());
                var moveProp = so.FindProperty("movementType");
                var names = moveProp.enumDisplayNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], movementType, StringComparison.OrdinalIgnoreCase))
                    {
                        moveProp.enumValueIndex = i;
                        break;
                    }
                }
            }

            if (payload.TryGetValue("speed", out var speedObj))
                so.FindProperty("speed").floatValue = Convert.ToSingle(speedObj);

            if (payload.TryGetValue("damage", out var damageObj))
                so.FindProperty("damage").floatValue = Convert.ToSingle(damageObj);

            if (payload.TryGetValue("lifetime", out var lifetimeObj))
                so.FindProperty("lifetime").floatValue = Convert.ToSingle(lifetimeObj);

            ApplyProjectileSettings(so, payload);
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var projectileId = new SerializedObject(component).FindProperty("projectileId").stringValue;

            return CreateSuccessResponse(
                ("projectileId", projectileId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectProjectile(Dictionary<string, object> payload)
        {
            var component = ResolveProjectileComponent(payload);
            var so = new SerializedObject(component);

            var projectileId = so.FindProperty("projectileId").stringValue;
            var speed = so.FindProperty("speed").floatValue;
            var damage = so.FindProperty("damage").floatValue;
            var lifetime = so.FindProperty("lifetime").floatValue;
            var useGravity = so.FindProperty("useGravity").boolValue;

            var isHomingProp = so.FindProperty("isHoming");
            var isHoming = isHomingProp != null && isHomingProp.boolValue;

            var canBounceProp = so.FindProperty("canBounce");
            var canBounce = canBounceProp != null && canBounceProp.boolValue;

            var canPierceProp = so.FindProperty("canPierce");
            var canPierce = canPierceProp != null && canPierceProp.boolValue;

            var info = new Dictionary<string, object>
            {
                { "projectileId", projectileId },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "speed", speed },
                { "damage", damage },
                { "lifetime", lifetime },
                { "isHoming", isHoming },
                { "canBounce", canBounce },
                { "canPierce", canPierce },
                { "useGravity", useGravity },
                { "position", new Dictionary<string, object>
                    {
                        { "x", component.transform.position.x },
                        { "y", component.transform.position.y },
                        { "z", component.transform.position.z }
                    }
                },
                { "direction", new Dictionary<string, object>
                    {
                        { "x", component.transform.forward.x },
                        { "y", component.transform.forward.y },
                        { "z", component.transform.forward.z }
                    }
                }
            };

            // Check for homing target via reflection
            var homingTargetField = component.GetType().GetField("homingTarget",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (homingTargetField != null)
            {
                var homingTarget = homingTargetField.GetValue(component) as Transform;
                if (homingTarget != null)
                {
                    info["homingTarget"] = BuildGameObjectPath(homingTarget.gameObject);
                }
            }

            // Check isLaunched via reflection
            var isLaunchedField = component.GetType().GetField("isLaunched",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (isLaunchedField != null)
            {
                info["isLaunched"] = isLaunchedField.GetValue(component);
            }

            return CreateSuccessResponse(("projectile", info));
        }

        #endregion

        #region Delete

        private object DeleteProjectile(Dictionary<string, object> payload)
        {
            var component = ResolveProjectileComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var projectileId = new SerializedObject(component).FindProperty("projectileId").stringValue;
            var scene = component.gameObject.scene;

            var deleteGameObject = GetBool(payload, "deleteGameObject", false);
            if (deleteGameObject)
            {
                Undo.DestroyObjectImmediate(component.gameObject);
            }
            else
            {
                Undo.DestroyObjectImmediate(component);
            }

            // Clean up the generated script from tracker
            ScriptGenerator.Delete(projectileId);

            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("projectileId", projectileId),
                ("path", path),
                ("deleted", true),
                ("gameObjectDeleted", deleteGameObject)
            );
        }

        #endregion

        #region Runtime Operations

        private object LaunchProjectile(Dictionary<string, object> payload)
        {
            var component = ResolveProjectileComponent(payload);

            // Get direction
            Vector3 direction = Vector3.forward;
            if (payload.TryGetValue("direction", out var dirObj) && dirObj is Dictionary<string, object> dirDict)
            {
                direction = GetVector3FromDict(dirDict, Vector3.forward).normalized;
            }
            else if (payload.TryGetValue("targetPosition", out var targetPosObj) && targetPosObj is Dictionary<string, object> targetPosDict)
            {
                var targetPos = GetVector3FromDict(targetPosDict, component.transform.position + Vector3.forward);
                direction = (targetPos - component.transform.position).normalized;
            }
            else if (!string.IsNullOrEmpty(GetString(payload, "targetPath")))
            {
                var targetGo = ResolveGameObject(GetString(payload, "targetPath"));
                if (targetGo != null)
                {
                    direction = (targetGo.transform.position - component.transform.position).normalized;
                }
            }

            // Try to invoke Launch method via reflection if available
            var launchMethod = component.GetType().GetMethod("Launch",
                BindingFlags.Public | BindingFlags.Instance,
                null, new[] { typeof(Vector3) }, null);
            if (launchMethod != null)
            {
                launchMethod.Invoke(component, new object[] { direction });
            }
            else
            {
                // Fallback: set the forward direction in editor
                component.transform.forward = direction;
            }

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("projectileId", new SerializedObject(component).FindProperty("projectileId").stringValue),
                ("direction", new Dictionary<string, object>
                {
                    { "x", direction.x },
                    { "y", direction.y },
                    { "z", direction.z }
                }),
                ("note", "Direction set. Actual launch happens in play mode.")
            );
        }

        private object SetHomingTarget(Dictionary<string, object> payload)
        {
            var component = ResolveProjectileComponent(payload);

            var so = new SerializedObject(component);

            var targetPath = GetString(payload, "homingTargetPath");
            if (string.IsNullOrEmpty(targetPath))
            {
                // Clear homing target
                var homingTargetProp = so.FindProperty("homingTarget");
                if (homingTargetProp != null)
                    homingTargetProp.objectReferenceValue = null;

                var isHomingProp = so.FindProperty("isHoming");
                if (isHomingProp != null)
                    isHomingProp.boolValue = false;
            }
            else
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo == null)
                {
                    throw new InvalidOperationException($"Homing target not found at path: {targetPath}");
                }

                var homingTargetProp = so.FindProperty("homingTarget");
                if (homingTargetProp != null)
                    homingTargetProp.objectReferenceValue = targetGo.transform;

                var isHomingProp = so.FindProperty("isHoming");
                if (isHomingProp != null)
                    isHomingProp.boolValue = true;
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var isHomingResult = so.FindProperty("isHoming");

            return CreateSuccessResponse(
                ("projectileId", so.FindProperty("projectileId").stringValue),
                ("isHoming", isHomingResult != null && isHomingResult.boolValue),
                ("homingTarget", targetPath ?? "none")
            );
        }

        private object DestroyProjectile(Dictionary<string, object> payload)
        {
            var component = ResolveProjectileComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var projectileId = new SerializedObject(component).FindProperty("projectileId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("projectileId", projectileId),
                ("path", path),
                ("destroyed", true)
            );
        }

        #endregion

        #region Find

        private object FindByProjectileId(Dictionary<string, object> payload)
        {
            var projectileId = GetString(payload, "projectileId");
            if (string.IsNullOrEmpty(projectileId))
            {
                throw new InvalidOperationException("projectileId is required for findByProjectileId.");
            }

            var component = CodeGenHelper.FindComponentInSceneByField("projectileId", projectileId);
            if (component == null)
            {
                return CreateSuccessResponse(("found", false), ("projectileId", projectileId));
            }

            var so = new SerializedObject(component);
            var speed = so.FindProperty("speed").floatValue;
            var damage = so.FindProperty("damage").floatValue;

            return CreateSuccessResponse(
                ("found", true),
                ("projectileId", projectileId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("speed", speed),
                ("damage", damage)
            );
        }

        #endregion

        #region Helpers

        private Component ResolveProjectileComponent(Dictionary<string, object> payload)
        {
            // Try by projectileId first
            var projectileId = GetString(payload, "projectileId");
            if (!string.IsNullOrEmpty(projectileId))
            {
                var byId = CodeGenHelper.FindComponentInSceneByField("projectileId", projectileId);
                if (byId != null) return byId;
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var byPath = CodeGenHelper.FindComponentByField(targetGo, "projectileId", null);
                    if (byPath != null) return byPath;
                    throw new InvalidOperationException($"No Projectile component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either projectileId or targetPath is required.");
        }

        private void ApplyProjectileSettings(SerializedObject so, Dictionary<string, object> payload)
        {
            // Gravity
            if (payload.TryGetValue("useGravity", out var gravityObj))
                so.FindProperty("useGravity").boolValue = Convert.ToBoolean(gravityObj);

            if (payload.TryGetValue("gravityScale", out var gravScaleObj))
                so.FindProperty("gravityScale").floatValue = Convert.ToSingle(gravScaleObj);

            // Damage settings
            if (payload.TryGetValue("damageOnHit", out var dmgHitObj))
                so.FindProperty("damageOnHit").boolValue = Convert.ToBoolean(dmgHitObj);

            if (payload.TryGetValue("targetTag", out var tagObj))
                so.FindProperty("targetTag").stringValue = tagObj.ToString();

            // Bouncing
            if (payload.TryGetValue("canBounce", out var bounceObj))
                so.FindProperty("canBounce").boolValue = Convert.ToBoolean(bounceObj);

            if (payload.TryGetValue("maxBounces", out var maxBouncesObj))
                so.FindProperty("maxBounces").intValue = Convert.ToInt32(maxBouncesObj);

            if (payload.TryGetValue("bounciness", out var bouncinessObj))
                so.FindProperty("bounciness").floatValue = Convert.ToSingle(bouncinessObj);

            // Homing
            if (payload.TryGetValue("isHoming", out var homingObj))
                so.FindProperty("isHoming").boolValue = Convert.ToBoolean(homingObj);

            if (payload.TryGetValue("homingStrength", out var homingStrObj))
                so.FindProperty("homingStrength").floatValue = Convert.ToSingle(homingStrObj);

            if (payload.TryGetValue("maxHomingAngle", out var maxAngleObj))
                so.FindProperty("maxHomingAngle").floatValue = Convert.ToSingle(maxAngleObj);

            // Piercing
            if (payload.TryGetValue("canPierce", out var pierceObj))
                so.FindProperty("canPierce").boolValue = Convert.ToBoolean(pierceObj);

            if (payload.TryGetValue("maxPierceCount", out var maxPierceObj))
                so.FindProperty("maxPierceCount").intValue = Convert.ToInt32(maxPierceObj);

            if (payload.TryGetValue("pierceDamageReduction", out var pierceRedObj))
                so.FindProperty("pierceDamageReduction").floatValue = Convert.ToSingle(pierceRedObj);
        }

        private string ParseMovementType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "transform" => "Transform",
                "rigidbody" => "Rigidbody",
                "rigidbody2d" => "Rigidbody2D",
                _ => "Transform"
            };
        }

        #endregion
    }
}
