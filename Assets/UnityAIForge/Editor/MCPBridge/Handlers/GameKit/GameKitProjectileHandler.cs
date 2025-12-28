using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using UnityAIForge.GameKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Projectile handler: create and manage projectiles for games.
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

        protected override bool RequiresCompilationWait(string operation) => false;

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

            // Check if already has projectile component
            var existingProjectile = targetGo.GetComponent<GameKitProjectile>();
            if (existingProjectile != null)
            {
                throw new InvalidOperationException($"GameObject '{BuildGameObjectPath(targetGo)}' already has a GameKitProjectile component.");
            }

            var projectileId = GetString(payload, "projectileId") ?? $"Projectile_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var movementType = ParseMovementType(GetString(payload, "movementType") ?? "transform");

            // Add component
            var projectile = Undo.AddComponent<GameKitProjectile>(targetGo);

            // Initialize via serialized object
            var serialized = new SerializedObject(projectile);
            serialized.FindProperty("projectileId").stringValue = projectileId;
            serialized.FindProperty("movementType").enumValueIndex = (int)movementType;

            // Set basic properties
            if (payload.TryGetValue("speed", out var speedObj))
            {
                serialized.FindProperty("speed").floatValue = Convert.ToSingle(speedObj);
            }

            if (payload.TryGetValue("damage", out var damageObj))
            {
                serialized.FindProperty("damage").floatValue = Convert.ToSingle(damageObj);
            }

            if (payload.TryGetValue("lifetime", out var lifetimeObj))
            {
                serialized.FindProperty("lifetime").floatValue = Convert.ToSingle(lifetimeObj);
            }

            // Apply additional settings
            ApplyProjectileSettings(serialized, payload);
            serialized.ApplyModifiedProperties();

            // Add appropriate physics components based on movement type
            var is2D = movementType == GameKitProjectile.MovementType.Rigidbody2D;

            if (movementType == GameKitProjectile.MovementType.Rigidbody && targetGo.GetComponent<Rigidbody>() == null)
            {
                var rb = Undo.AddComponent<Rigidbody>(targetGo);
                rb.useGravity = GetBool(payload, "useGravity", false);
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
            else if (is2D && targetGo.GetComponent<Rigidbody2D>() == null)
            {
                var rb2d = Undo.AddComponent<Rigidbody2D>(targetGo);
                rb2d.gravityScale = GetBool(payload, "useGravity", false) ? GetFloat(payload, "gravityScale", 1f) : 0f;
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

            return CreateSuccessResponse(
                ("projectileId", projectileId),
                ("path", BuildGameObjectPath(targetGo)),
                ("movementType", movementType.ToString()),
                ("speed", serialized.FindProperty("speed").floatValue),
                ("damage", serialized.FindProperty("damage").floatValue)
            );
        }

        #endregion

        #region Update

        private object UpdateProjectile(Dictionary<string, object> payload)
        {
            var projectile = ResolveProjectileComponent(payload);

            Undo.RecordObject(projectile, "Update GameKit Projectile");

            var serialized = new SerializedObject(projectile);

            if (payload.TryGetValue("movementType", out var moveTypeObj))
            {
                var movementType = ParseMovementType(moveTypeObj.ToString());
                serialized.FindProperty("movementType").enumValueIndex = (int)movementType;
            }

            if (payload.TryGetValue("speed", out var speedObj))
            {
                serialized.FindProperty("speed").floatValue = Convert.ToSingle(speedObj);
            }

            if (payload.TryGetValue("damage", out var damageObj))
            {
                serialized.FindProperty("damage").floatValue = Convert.ToSingle(damageObj);
            }

            if (payload.TryGetValue("lifetime", out var lifetimeObj))
            {
                serialized.FindProperty("lifetime").floatValue = Convert.ToSingle(lifetimeObj);
            }

            ApplyProjectileSettings(serialized, payload);
            serialized.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(projectile.gameObject.scene);

            return CreateSuccessResponse(
                ("projectileId", projectile.ProjectileId),
                ("path", BuildGameObjectPath(projectile.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectProjectile(Dictionary<string, object> payload)
        {
            var projectile = ResolveProjectileComponent(payload);

            var serialized = new SerializedObject(projectile);

            var info = new Dictionary<string, object>
            {
                { "projectileId", projectile.ProjectileId },
                { "path", BuildGameObjectPath(projectile.gameObject) },
                { "speed", projectile.Speed },
                { "damage", projectile.Damage },
                { "lifetime", projectile.Lifetime },
                { "isHoming", projectile.IsHoming },
                { "canBounce", projectile.CanBounce },
                { "canPierce", projectile.CanPierce },
                { "isLaunched", projectile.IsLaunched },
                { "useGravity", serialized.FindProperty("useGravity").boolValue },
                { "position", new Dictionary<string, object>
                    {
                        { "x", projectile.transform.position.x },
                        { "y", projectile.transform.position.y },
                        { "z", projectile.transform.position.z }
                    }
                },
                { "direction", new Dictionary<string, object>
                    {
                        { "x", projectile.Direction.x },
                        { "y", projectile.Direction.y },
                        { "z", projectile.Direction.z }
                    }
                }
            };

            // Add homing target if exists
            if (projectile.HomingTarget != null)
            {
                info["homingTarget"] = BuildGameObjectPath(projectile.HomingTarget.gameObject);
            }

            return CreateSuccessResponse(("projectile", info));
        }

        #endregion

        #region Delete

        private object DeleteProjectile(Dictionary<string, object> payload)
        {
            var projectile = ResolveProjectileComponent(payload);
            var path = BuildGameObjectPath(projectile.gameObject);
            var projectileId = projectile.ProjectileId;
            var scene = projectile.gameObject.scene;

            var deleteGameObject = GetBool(payload, "deleteGameObject", false);
            if (deleteGameObject)
            {
                Undo.DestroyObjectImmediate(projectile.gameObject);
            }
            else
            {
                Undo.DestroyObjectImmediate(projectile);
            }

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
            var projectile = ResolveProjectileComponent(payload);

            // Get direction
            Vector3 direction = Vector3.forward;
            if (payload.TryGetValue("direction", out var dirObj) && dirObj is Dictionary<string, object> dirDict)
            {
                direction = GetVector3FromDict(dirDict, Vector3.forward).normalized;
            }
            else if (payload.TryGetValue("targetPosition", out var targetPosObj) && targetPosObj is Dictionary<string, object> targetPosDict)
            {
                var targetPos = GetVector3FromDict(targetPosDict, projectile.transform.position + Vector3.forward);
                direction = (targetPos - projectile.transform.position).normalized;
            }
            else if (!string.IsNullOrEmpty(GetString(payload, "targetPath")))
            {
                var targetGo = ResolveGameObject(GetString(payload, "targetPath"));
                if (targetGo != null)
                {
                    direction = (targetGo.transform.position - projectile.transform.position).normalized;
                }
            }

            // Set the forward direction in editor (actual launch happens in play mode)
            projectile.transform.forward = direction;

            EditorSceneManager.MarkSceneDirty(projectile.gameObject.scene);

            return CreateSuccessResponse(
                ("projectileId", projectile.ProjectileId),
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
            var projectile = ResolveProjectileComponent(payload);

            var serialized = new SerializedObject(projectile);

            var targetPath = GetString(payload, "homingTargetPath");
            if (string.IsNullOrEmpty(targetPath))
            {
                // Clear homing target
                serialized.FindProperty("homingTarget").objectReferenceValue = null;
                serialized.FindProperty("isHoming").boolValue = false;
            }
            else
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo == null)
                {
                    throw new InvalidOperationException($"Homing target not found at path: {targetPath}");
                }

                serialized.FindProperty("homingTarget").objectReferenceValue = targetGo.transform;
                serialized.FindProperty("isHoming").boolValue = true;
            }

            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(projectile.gameObject.scene);

            return CreateSuccessResponse(
                ("projectileId", projectile.ProjectileId),
                ("isHoming", serialized.FindProperty("isHoming").boolValue),
                ("homingTarget", targetPath ?? "none")
            );
        }

        private object DestroyProjectile(Dictionary<string, object> payload)
        {
            var projectile = ResolveProjectileComponent(payload);
            var path = BuildGameObjectPath(projectile.gameObject);
            var projectileId = projectile.ProjectileId;
            var scene = projectile.gameObject.scene;

            Undo.DestroyObjectImmediate(projectile.gameObject);
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

            var projectile = FindProjectileById(projectileId);
            if (projectile == null)
            {
                return CreateSuccessResponse(("found", false), ("projectileId", projectileId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("projectileId", projectile.ProjectileId),
                ("path", BuildGameObjectPath(projectile.gameObject)),
                ("speed", projectile.Speed),
                ("damage", projectile.Damage),
                ("isLaunched", projectile.IsLaunched)
            );
        }

        #endregion

        #region Helpers

        private GameKitProjectile ResolveProjectileComponent(Dictionary<string, object> payload)
        {
            // Try by projectileId first
            var projectileId = GetString(payload, "projectileId");
            if (!string.IsNullOrEmpty(projectileId))
            {
                var byId = FindProjectileById(projectileId);
                if (byId != null) return byId;
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var byPath = targetGo.GetComponent<GameKitProjectile>();
                    if (byPath != null) return byPath;
                    throw new InvalidOperationException($"No GameKitProjectile component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either projectileId or targetPath is required.");
        }

        private GameKitProjectile FindProjectileById(string projectileId)
        {
            var projectiles = UnityEngine.Object.FindObjectsByType<GameKitProjectile>(FindObjectsSortMode.None);
            foreach (var projectile in projectiles)
            {
                if (projectile.ProjectileId == projectileId)
                {
                    return projectile;
                }
            }
            return null;
        }

        private void ApplyProjectileSettings(SerializedObject serialized, Dictionary<string, object> payload)
        {
            // Gravity
            if (payload.TryGetValue("useGravity", out var gravityObj))
            {
                serialized.FindProperty("useGravity").boolValue = Convert.ToBoolean(gravityObj);
            }

            if (payload.TryGetValue("gravityScale", out var gravScaleObj))
            {
                serialized.FindProperty("gravityScale").floatValue = Convert.ToSingle(gravScaleObj);
            }

            // Damage settings
            if (payload.TryGetValue("damageOnHit", out var dmgHitObj))
            {
                serialized.FindProperty("damageOnHit").boolValue = Convert.ToBoolean(dmgHitObj);
            }

            if (payload.TryGetValue("targetTag", out var tagObj))
            {
                serialized.FindProperty("targetTag").stringValue = tagObj.ToString();
            }

            // Bouncing
            if (payload.TryGetValue("canBounce", out var bounceObj))
            {
                serialized.FindProperty("canBounce").boolValue = Convert.ToBoolean(bounceObj);
            }

            if (payload.TryGetValue("maxBounces", out var maxBouncesObj))
            {
                serialized.FindProperty("maxBounces").intValue = Convert.ToInt32(maxBouncesObj);
            }

            if (payload.TryGetValue("bounciness", out var bouncinessObj))
            {
                serialized.FindProperty("bounciness").floatValue = Convert.ToSingle(bouncinessObj);
            }

            // Homing
            if (payload.TryGetValue("isHoming", out var homingObj))
            {
                serialized.FindProperty("isHoming").boolValue = Convert.ToBoolean(homingObj);
            }

            if (payload.TryGetValue("homingStrength", out var homingStrObj))
            {
                serialized.FindProperty("homingStrength").floatValue = Convert.ToSingle(homingStrObj);
            }

            if (payload.TryGetValue("maxHomingAngle", out var maxAngleObj))
            {
                serialized.FindProperty("maxHomingAngle").floatValue = Convert.ToSingle(maxAngleObj);
            }

            // Piercing
            if (payload.TryGetValue("canPierce", out var pierceObj))
            {
                serialized.FindProperty("canPierce").boolValue = Convert.ToBoolean(pierceObj);
            }

            if (payload.TryGetValue("maxPierceCount", out var maxPierceObj))
            {
                serialized.FindProperty("maxPierceCount").intValue = Convert.ToInt32(maxPierceObj);
            }

            if (payload.TryGetValue("pierceDamageReduction", out var pierceRedObj))
            {
                serialized.FindProperty("pierceDamageReduction").floatValue = Convert.ToSingle(pierceRedObj);
            }
        }

        private GameKitProjectile.MovementType ParseMovementType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "transform" => GameKitProjectile.MovementType.Transform,
                "rigidbody" => GameKitProjectile.MovementType.Rigidbody,
                "rigidbody2d" => GameKitProjectile.MovementType.Rigidbody2D,
                _ => GameKitProjectile.MovementType.Transform
            };
        }

        private Vector3 GetVector3FromDict(Dictionary<string, object> dict, Vector3 fallback)
        {
            float x = dict.TryGetValue("x", out var xObj) ? Convert.ToSingle(xObj) : fallback.x;
            float y = dict.TryGetValue("y", out var yObj) ? Convert.ToSingle(yObj) : fallback.y;
            float z = dict.TryGetValue("z", out var zObj) ? Convert.ToSingle(zObj) : fallback.z;
            return new Vector3(x, y, z);
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

        private float GetFloat(Dictionary<string, object> payload, string key, float defaultValue)
        {
            if (payload.TryGetValue(key, out var value))
            {
                return Convert.ToSingle(value);
            }
            return defaultValue;
        }

        private bool GetBool(Dictionary<string, object> payload, string key, bool defaultValue)
        {
            if (payload.TryGetValue(key, out var value))
            {
                return Convert.ToBoolean(value);
            }
            return defaultValue;
        }

        #endregion
    }
}
