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
    /// GameKit TriggerZone handler: create and manage trigger zones for games.
    /// Supports checkpoints, damage zones, heal zones, teleporters, and custom triggers.
    /// </summary>
    public class GameKitTriggerZoneHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "update", "inspect", "delete",
            "activate", "deactivate", "reset",
            "setTeleportDestination", "findByZoneId"
        };

        public override string Category => "gamekitTriggerZone";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateTriggerZone(payload),
                "update" => UpdateTriggerZone(payload),
                "inspect" => InspectTriggerZone(payload),
                "delete" => DeleteTriggerZone(payload),
                "activate" => ActivateZone(payload),
                "deactivate" => DeactivateZone(payload),
                "reset" => ResetZone(payload),
                "setTeleportDestination" => SetTeleportDestination(payload),
                "findByZoneId" => FindByZoneId(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit TriggerZone operation: {operation}")
            };
        }

        #region Create

        private object CreateTriggerZone(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            GameObject targetGo;

            if (string.IsNullOrEmpty(targetPath))
            {
                // Create a new GameObject
                var name = GetString(payload, "name") ?? "TriggerZone";
                targetGo = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(targetGo, "Create Trigger Zone");

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

            // Check if already has trigger zone component
            var existingZone = targetGo.GetComponent<GameKitTriggerZone>();
            if (existingZone != null)
            {
                throw new InvalidOperationException($"GameObject '{BuildGameObjectPath(targetGo)}' already has a GameKitTriggerZone component.");
            }

            var zoneId = GetString(payload, "zoneId") ?? $"Zone_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var zoneType = ParseZoneType(GetString(payload, "zoneType") ?? "generic");
            var triggerMode = ParseTriggerMode(GetString(payload, "triggerMode") ?? "repeat");

            // Add component
            var zone = Undo.AddComponent<GameKitTriggerZone>(targetGo);

            // Initialize via serialized object
            var serialized = new SerializedObject(zone);
            serialized.FindProperty("zoneId").stringValue = zoneId;
            serialized.FindProperty("zoneType").enumValueIndex = (int)zoneType;
            serialized.FindProperty("triggerMode").enumValueIndex = (int)triggerMode;

            // Apply zone-specific settings
            ApplyTriggerZoneSettings(serialized, payload);
            serialized.ApplyModifiedProperties();

            // Add collider if none exists
            var is2D = GetBool(payload, "is2D", false);
            var colliderShape = GetString(payload, "colliderShape") ?? "box";
            var colliderSize = GetVector3(payload, "colliderSize", new Vector3(2f, 2f, 2f));

            if (targetGo.GetComponent<Collider>() == null && targetGo.GetComponent<Collider2D>() == null)
            {
                if (is2D)
                {
                    CreateCollider2D(targetGo, colliderShape, colliderSize);
                }
                else
                {
                    CreateCollider3D(targetGo, colliderShape, colliderSize);
                }
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("zoneId", zoneId),
                ("path", BuildGameObjectPath(targetGo)),
                ("zoneType", zoneType.ToString()),
                ("triggerMode", triggerMode.ToString())
            );
        }

        private void CreateCollider2D(GameObject go, string shape, Vector3 size)
        {
            switch (shape.ToLowerInvariant())
            {
                case "circle":
                    var circle = Undo.AddComponent<CircleCollider2D>(go);
                    circle.isTrigger = true;
                    circle.radius = size.x / 2f;
                    break;
                case "capsule":
                    var capsule = Undo.AddComponent<CapsuleCollider2D>(go);
                    capsule.isTrigger = true;
                    capsule.size = new Vector2(size.x, size.y);
                    break;
                default: // box
                    var box = Undo.AddComponent<BoxCollider2D>(go);
                    box.isTrigger = true;
                    box.size = new Vector2(size.x, size.y);
                    break;
            }
        }

        private void CreateCollider3D(GameObject go, string shape, Vector3 size)
        {
            switch (shape.ToLowerInvariant())
            {
                case "sphere":
                    var sphere = Undo.AddComponent<SphereCollider>(go);
                    sphere.isTrigger = true;
                    sphere.radius = size.x / 2f;
                    break;
                case "capsule":
                    var capsule = Undo.AddComponent<CapsuleCollider>(go);
                    capsule.isTrigger = true;
                    capsule.radius = size.x / 2f;
                    capsule.height = size.y;
                    break;
                default: // box
                    var box = Undo.AddComponent<BoxCollider>(go);
                    box.isTrigger = true;
                    box.size = size;
                    break;
            }
        }

        #endregion

        #region Update

        private object UpdateTriggerZone(Dictionary<string, object> payload)
        {
            var zone = ResolveTriggerZoneComponent(payload);

            Undo.RecordObject(zone, "Update GameKit TriggerZone");

            var serialized = new SerializedObject(zone);

            if (payload.TryGetValue("zoneType", out var zoneTypeObj))
            {
                var zoneType = ParseZoneType(zoneTypeObj.ToString());
                serialized.FindProperty("zoneType").enumValueIndex = (int)zoneType;
            }

            if (payload.TryGetValue("triggerMode", out var triggerModeObj))
            {
                var triggerMode = ParseTriggerMode(triggerModeObj.ToString());
                serialized.FindProperty("triggerMode").enumValueIndex = (int)triggerMode;
            }

            ApplyTriggerZoneSettings(serialized, payload);
            serialized.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(zone.gameObject.scene);

            return CreateSuccessResponse(
                ("zoneId", zone.ZoneId),
                ("path", BuildGameObjectPath(zone.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectTriggerZone(Dictionary<string, object> payload)
        {
            var zone = ResolveTriggerZoneComponent(payload);

            var serialized = new SerializedObject(zone);

            var info = new Dictionary<string, object>
            {
                { "zoneId", zone.ZoneId },
                { "path", BuildGameObjectPath(zone.gameObject) },
                { "zoneType", zone.Type.ToString() },
                { "triggerMode", zone.Mode.ToString() },
                { "isActive", zone.gameObject.activeInHierarchy },
                { "hasTriggered", zone.HasTriggered },
                { "entitiesInZoneCount", zone.EntitiesInZoneCount },
                { "requiredTag", serialized.FindProperty("requiredTag").stringValue },
                { "position", new Dictionary<string, object>
                    {
                        { "x", zone.transform.position.x },
                        { "y", zone.transform.position.y },
                        { "z", zone.transform.position.z }
                    }
                }
            };

            // Add zone-specific info
            switch (zone.Type)
            {
                case GameKitTriggerZone.ZoneType.DamageZone:
                case GameKitTriggerZone.ZoneType.HealZone:
                    info["effectValue"] = serialized.FindProperty("effectValue").floatValue;
                    info["effectInterval"] = serialized.FindProperty("effectInterval").floatValue;
                    break;
                case GameKitTriggerZone.ZoneType.Teleport:
                    var teleportDest = serialized.FindProperty("teleportDestination").objectReferenceValue as Transform;
                    if (teleportDest != null)
                    {
                        info["teleportDestination"] = BuildGameObjectPath(teleportDest.gameObject);
                    }
                    info["preserveVelocity"] = serialized.FindProperty("preserveVelocity").boolValue;
                    info["preserveRotation"] = serialized.FindProperty("preserveRotation").boolValue;
                    break;
                case GameKitTriggerZone.ZoneType.SpeedBoost:
                case GameKitTriggerZone.ZoneType.SlowDown:
                    info["speedMultiplier"] = serialized.FindProperty("speedMultiplier").floatValue;
                    break;
                case GameKitTriggerZone.ZoneType.Checkpoint:
                    info["isActiveCheckpoint"] = zone.IsActiveCheckpoint;
                    var respawnOffset = serialized.FindProperty("respawnOffset").vector3Value;
                    info["respawnOffset"] = new Dictionary<string, object>
                    {
                        { "x", respawnOffset.x },
                        { "y", respawnOffset.y },
                        { "z", respawnOffset.z }
                    };
                    break;
            }

            return CreateSuccessResponse(("triggerZone", info));
        }

        #endregion

        #region Delete

        private object DeleteTriggerZone(Dictionary<string, object> payload)
        {
            var zone = ResolveTriggerZoneComponent(payload);
            var path = BuildGameObjectPath(zone.gameObject);
            var zoneId = zone.ZoneId;
            var scene = zone.gameObject.scene;

            var deleteGameObject = GetBool(payload, "deleteGameObject", false);
            if (deleteGameObject)
            {
                Undo.DestroyObjectImmediate(zone.gameObject);
            }
            else
            {
                Undo.DestroyObjectImmediate(zone);
            }

            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("zoneId", zoneId),
                ("path", path),
                ("deleted", true),
                ("gameObjectDeleted", deleteGameObject)
            );
        }

        #endregion

        #region Zone Control

        private object ActivateZone(Dictionary<string, object> payload)
        {
            var zone = ResolveTriggerZoneComponent(payload);

            Undo.RecordObject(zone.gameObject, "Activate Trigger Zone");

            // Enable the GameObject
            zone.gameObject.SetActive(true);

            // Enable colliders
            var collider = zone.GetComponent<Collider>();
            if (collider != null)
            {
                Undo.RecordObject(collider, "Enable Collider");
                collider.enabled = true;
            }

            var collider2D = zone.GetComponent<Collider2D>();
            if (collider2D != null)
            {
                Undo.RecordObject(collider2D, "Enable Collider2D");
                collider2D.enabled = true;
            }

            EditorSceneManager.MarkSceneDirty(zone.gameObject.scene);

            return CreateSuccessResponse(
                ("zoneId", zone.ZoneId),
                ("activated", true)
            );
        }

        private object DeactivateZone(Dictionary<string, object> payload)
        {
            var zone = ResolveTriggerZoneComponent(payload);

            // Disable colliders (keep GameObject active for visibility)
            var collider = zone.GetComponent<Collider>();
            if (collider != null)
            {
                Undo.RecordObject(collider, "Disable Collider");
                collider.enabled = false;
            }

            var collider2D = zone.GetComponent<Collider2D>();
            if (collider2D != null)
            {
                Undo.RecordObject(collider2D, "Disable Collider2D");
                collider2D.enabled = false;
            }

            EditorSceneManager.MarkSceneDirty(zone.gameObject.scene);

            return CreateSuccessResponse(
                ("zoneId", zone.ZoneId),
                ("deactivated", true)
            );
        }

        private object ResetZone(Dictionary<string, object> payload)
        {
            var zone = ResolveTriggerZoneComponent(payload);

            Undo.RecordObject(zone.gameObject, "Reset Trigger Zone");

            // Enable GameObject
            zone.gameObject.SetActive(true);

            // Reset trigger state via public method
            zone.ResetTrigger();

            // Enable colliders
            var collider = zone.GetComponent<Collider>();
            if (collider != null)
            {
                Undo.RecordObject(collider, "Enable Collider");
                collider.enabled = true;
            }

            var collider2D = zone.GetComponent<Collider2D>();
            if (collider2D != null)
            {
                Undo.RecordObject(collider2D, "Enable Collider2D");
                collider2D.enabled = true;
            }

            EditorSceneManager.MarkSceneDirty(zone.gameObject.scene);

            return CreateSuccessResponse(
                ("zoneId", zone.ZoneId),
                ("reset", true)
            );
        }

        private object SetTeleportDestination(Dictionary<string, object> payload)
        {
            var zone = ResolveTriggerZoneComponent(payload);

            var serialized = new SerializedObject(zone);

            // Ensure zone type is Teleport
            if (zone.Type != GameKitTriggerZone.ZoneType.Teleport)
            {
                serialized.FindProperty("zoneType").enumValueIndex = (int)GameKitTriggerZone.ZoneType.Teleport;
            }

            var destinationPath = GetString(payload, "destinationPath");
            if (string.IsNullOrEmpty(destinationPath))
            {
                // Create destination from position
                if (payload.TryGetValue("destinationPosition", out var posObj) && posObj is Dictionary<string, object> posDict)
                {
                    var pos = GetVector3FromDict(posDict, Vector3.zero);

                    // Create a destination marker
                    var destGo = new GameObject($"{zone.name}_Destination");
                    destGo.transform.position = pos;
                    Undo.RegisterCreatedObjectUndo(destGo, "Create Teleport Destination");

                    serialized.FindProperty("teleportDestination").objectReferenceValue = destGo.transform;
                }
                else
                {
                    throw new InvalidOperationException("Either destinationPath or destinationPosition is required.");
                }
            }
            else
            {
                var destGo = ResolveGameObject(destinationPath);
                if (destGo == null)
                {
                    throw new InvalidOperationException($"Destination not found at path: {destinationPath}");
                }
                serialized.FindProperty("teleportDestination").objectReferenceValue = destGo.transform;
            }

            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(zone.gameObject.scene);

            return CreateSuccessResponse(
                ("zoneId", zone.ZoneId),
                ("teleportDestinationSet", true)
            );
        }

        #endregion

        #region Find

        private object FindByZoneId(Dictionary<string, object> payload)
        {
            var zoneId = GetString(payload, "zoneId");
            if (string.IsNullOrEmpty(zoneId))
            {
                throw new InvalidOperationException("zoneId is required for findByZoneId.");
            }

            var zone = FindZoneById(zoneId);
            if (zone == null)
            {
                return CreateSuccessResponse(("found", false), ("zoneId", zoneId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("zoneId", zone.ZoneId),
                ("path", BuildGameObjectPath(zone.gameObject)),
                ("zoneType", zone.Type.ToString()),
                ("isActive", zone.gameObject.activeInHierarchy),
                ("entitiesInZoneCount", zone.EntitiesInZoneCount)
            );
        }

        #endregion

        #region Helpers

        private GameKitTriggerZone ResolveTriggerZoneComponent(Dictionary<string, object> payload)
        {
            // Try by zoneId first
            var zoneId = GetString(payload, "zoneId");
            if (!string.IsNullOrEmpty(zoneId))
            {
                var byId = FindZoneById(zoneId);
                if (byId != null) return byId;
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var byPath = targetGo.GetComponent<GameKitTriggerZone>();
                    if (byPath != null) return byPath;
                    throw new InvalidOperationException($"No GameKitTriggerZone component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either zoneId or targetPath is required.");
        }

        private GameKitTriggerZone FindZoneById(string zoneId)
        {
            var zones = UnityEngine.Object.FindObjectsByType<GameKitTriggerZone>(FindObjectsSortMode.None);
            foreach (var zone in zones)
            {
                if (zone.ZoneId == zoneId)
                {
                    return zone;
                }
            }
            return null;
        }

        private void ApplyTriggerZoneSettings(SerializedObject serialized, Dictionary<string, object> payload)
        {
            // Trigger settings
            if (payload.TryGetValue("requiredTag", out var tagObj))
            {
                serialized.FindProperty("requiredTag").stringValue = tagObj.ToString();
            }

            if (payload.TryGetValue("requireTriggerCollider", out var requireTriggerObj))
            {
                serialized.FindProperty("requireTriggerCollider").boolValue = Convert.ToBoolean(requireTriggerObj);
            }

            // Zone-specific settings
            if (payload.TryGetValue("effectValue", out var effectValObj))
            {
                serialized.FindProperty("effectValue").floatValue = Convert.ToSingle(effectValObj);
            }

            if (payload.TryGetValue("effectInterval", out var effectIntObj))
            {
                serialized.FindProperty("effectInterval").floatValue = Convert.ToSingle(effectIntObj);
            }

            if (payload.TryGetValue("speedMultiplier", out var speedMultObj))
            {
                serialized.FindProperty("speedMultiplier").floatValue = Convert.ToSingle(speedMultObj);
            }

            // Teleport settings
            if (payload.TryGetValue("preserveVelocity", out var preserveVelObj))
            {
                serialized.FindProperty("preserveVelocity").boolValue = Convert.ToBoolean(preserveVelObj);
            }

            if (payload.TryGetValue("preserveRotation", out var preserveRotObj))
            {
                serialized.FindProperty("preserveRotation").boolValue = Convert.ToBoolean(preserveRotObj);
            }

            // Checkpoint settings
            if (payload.TryGetValue("respawnOffset", out var respawnOffsetObj) && respawnOffsetObj is Dictionary<string, object> respawnDict)
            {
                var offset = GetVector3FromDict(respawnDict, Vector3.up);
                serialized.FindProperty("respawnOffset").vector3Value = offset;
            }

            // Visual settings
            if (payload.TryGetValue("changeColorOnEnter", out var changeColorObj))
            {
                serialized.FindProperty("changeColorOnEnter").boolValue = Convert.ToBoolean(changeColorObj);
            }

            if (payload.TryGetValue("activeColor", out var activeColorObj) && activeColorObj is Dictionary<string, object> activeColorDict)
            {
                var color = new Color(
                    activeColorDict.TryGetValue("r", out var r) ? Convert.ToSingle(r) : 0f,
                    activeColorDict.TryGetValue("g", out var g) ? Convert.ToSingle(g) : 1f,
                    activeColorDict.TryGetValue("b", out var b) ? Convert.ToSingle(b) : 0f,
                    activeColorDict.TryGetValue("a", out var a) ? Convert.ToSingle(a) : 1f
                );
                serialized.FindProperty("activeColor").colorValue = color;
            }

            if (payload.TryGetValue("inactiveColor", out var inactiveColorObj) && inactiveColorObj is Dictionary<string, object> inactiveColorDict)
            {
                var color = new Color(
                    inactiveColorDict.TryGetValue("r", out var r) ? Convert.ToSingle(r) : 0.5f,
                    inactiveColorDict.TryGetValue("g", out var g) ? Convert.ToSingle(g) : 0.5f,
                    inactiveColorDict.TryGetValue("b", out var b) ? Convert.ToSingle(b) : 0.5f,
                    inactiveColorDict.TryGetValue("a", out var a) ? Convert.ToSingle(a) : 1f
                );
                serialized.FindProperty("inactiveColor").colorValue = color;
            }
        }

        private GameKitTriggerZone.ZoneType ParseZoneType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "generic" => GameKitTriggerZone.ZoneType.Generic,
                "checkpoint" => GameKitTriggerZone.ZoneType.Checkpoint,
                "damagezone" => GameKitTriggerZone.ZoneType.DamageZone,
                "healzone" => GameKitTriggerZone.ZoneType.HealZone,
                "teleport" => GameKitTriggerZone.ZoneType.Teleport,
                "speedboost" => GameKitTriggerZone.ZoneType.SpeedBoost,
                "slowdown" => GameKitTriggerZone.ZoneType.SlowDown,
                "killzone" => GameKitTriggerZone.ZoneType.KillZone,
                "safezone" => GameKitTriggerZone.ZoneType.SafeZone,
                "trigger" => GameKitTriggerZone.ZoneType.Trigger,
                _ => GameKitTriggerZone.ZoneType.Generic
            };
        }

        private GameKitTriggerZone.TriggerMode ParseTriggerMode(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "once" => GameKitTriggerZone.TriggerMode.Once,
                "onceperentity" => GameKitTriggerZone.TriggerMode.OncePerEntity,
                "repeat" => GameKitTriggerZone.TriggerMode.Repeat,
                "whileinside" => GameKitTriggerZone.TriggerMode.WhileInside,
                _ => GameKitTriggerZone.TriggerMode.Repeat
            };
        }

        private Vector3 GetVector3(Dictionary<string, object> payload, string key, Vector3 fallback)
        {
            if (payload.TryGetValue(key, out var value) && value is Dictionary<string, object> dict)
            {
                return GetVector3FromDict(dict, fallback);
            }
            return fallback;
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
