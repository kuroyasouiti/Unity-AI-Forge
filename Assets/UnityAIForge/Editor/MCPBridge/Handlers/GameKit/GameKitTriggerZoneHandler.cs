using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit TriggerZone handler: create and manage trigger zones for games.
    /// Supports checkpoints, damage zones, heal zones, teleporters, and custom triggers.
    /// Uses code generation to produce standalone TriggerZone scripts with zero package dependency.
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

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

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

            // Check if already has a trigger zone component (by checking for zoneId field)
            var existingZone = CodeGenHelper.FindComponentByField(targetGo, "zoneId", null);
            if (existingZone != null)
            {
                throw new InvalidOperationException(
                    $"GameObject '{BuildGameObjectPath(targetGo)}' already has a TriggerZone component.");
            }

            var zoneId = GetString(payload, "zoneId") ?? $"Zone_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var zoneType = ParseZoneType(GetString(payload, "zoneType") ?? "generic");
            var triggerMode = ParseTriggerMode(GetString(payload, "triggerMode") ?? "repeat");

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(zoneId, "TriggerZone");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "ZONE_ID", zoneId },
                { "ZONE_TYPE", zoneType },
                { "TRIGGER_MODE", triggerMode }
            };

            // Add optional template variables
            if (payload.TryGetValue("requiredTag", out var tagObj))
                variables["REQUIRED_TAG"] = tagObj.ToString();
            if (payload.TryGetValue("effectValue", out var effectValObj))
                variables["EFFECT_VALUE"] = Convert.ToSingle(effectValObj);
            if (payload.TryGetValue("effectInterval", out var effectIntObj))
                variables["EFFECT_INTERVAL"] = Convert.ToSingle(effectIntObj);
            if (payload.TryGetValue("speedMultiplier", out var speedMultObj))
                variables["SPEED_MULTIPLIER"] = Convert.ToSingle(speedMultObj);
            if (payload.TryGetValue("preserveVelocity", out var preserveVelObj))
                variables["PRESERVE_VELOCITY"] = Convert.ToBoolean(preserveVelObj);
            if (payload.TryGetValue("preserveRotation", out var preserveRotObj))
                variables["PRESERVE_ROTATION"] = Convert.ToBoolean(preserveRotObj);

            // Build properties to set after component is added
            var propertiesToSet = new Dictionary<string, object>();
            if (payload.TryGetValue("requireTriggerCollider", out var requireTriggerObj))
                propertiesToSet["requireTriggerCollider"] = Convert.ToBoolean(requireTriggerObj);
            if (payload.TryGetValue("respawnOffset", out var respawnOffsetObj) && respawnOffsetObj is Dictionary<string, object> respawnDict)
                propertiesToSet["respawnOffset"] = respawnDict;
            if (payload.TryGetValue("changeColorOnEnter", out var changeColorObj))
                propertiesToSet["changeColorOnEnter"] = Convert.ToBoolean(changeColorObj);
            if (payload.TryGetValue("activeColor", out var activeColorObj) && activeColorObj is Dictionary<string, object> activeColorDict)
                propertiesToSet["activeColor"] = activeColorDict;
            if (payload.TryGetValue("inactiveColor", out var inactiveColorObj) && inactiveColorObj is Dictionary<string, object> inactiveColorDict)
                propertiesToSet["inactiveColor"] = inactiveColorDict;

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "TriggerZone", zoneId, className, variables, outputDir,
                propertiesToSet.Count > 0 ? propertiesToSet : null);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate TriggerZone script.");
            }

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

            result["zoneId"] = zoneId;
            result["path"] = BuildGameObjectPath(targetGo);
            result["zoneType"] = zoneType;
            result["triggerMode"] = triggerMode;

            return result;
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
            var component = ResolveTriggerZoneComponent(payload);

            Undo.RecordObject(component, "Update TriggerZone");

            var so = new SerializedObject(component);

            if (payload.TryGetValue("zoneType", out var zoneTypeObj))
            {
                var zoneType = ParseZoneType(zoneTypeObj.ToString());
                var zoneTypeProp = so.FindProperty("zoneType");
                var names = zoneTypeProp.enumDisplayNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], zoneType, StringComparison.OrdinalIgnoreCase))
                    {
                        zoneTypeProp.enumValueIndex = i;
                        break;
                    }
                }
            }

            if (payload.TryGetValue("triggerMode", out var triggerModeObj))
            {
                var triggerMode = ParseTriggerMode(triggerModeObj.ToString());
                var triggerModeProp = so.FindProperty("triggerMode");
                var names = triggerModeProp.enumDisplayNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], triggerMode, StringComparison.OrdinalIgnoreCase))
                    {
                        triggerModeProp.enumValueIndex = i;
                        break;
                    }
                }
            }

            ApplyTriggerZoneSettings(so, payload);
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var zoneId = new SerializedObject(component).FindProperty("zoneId").stringValue;

            return CreateSuccessResponse(
                ("zoneId", zoneId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectTriggerZone(Dictionary<string, object> payload)
        {
            var component = ResolveTriggerZoneComponent(payload);
            var so = new SerializedObject(component);

            var zoneTypeProp = so.FindProperty("zoneType");
            var zoneType = zoneTypeProp.enumValueIndex < zoneTypeProp.enumDisplayNames.Length
                ? zoneTypeProp.enumDisplayNames[zoneTypeProp.enumValueIndex]
                : "Generic";

            var triggerModeProp = so.FindProperty("triggerMode");
            var triggerMode = triggerModeProp.enumValueIndex < triggerModeProp.enumDisplayNames.Length
                ? triggerModeProp.enumDisplayNames[triggerModeProp.enumValueIndex]
                : "Repeat";

            var info = new Dictionary<string, object>
            {
                { "zoneId", so.FindProperty("zoneId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "zoneType", zoneType },
                { "triggerMode", triggerMode },
                { "isActive", component.gameObject.activeInHierarchy },
                { "requiredTag", so.FindProperty("requiredTag").stringValue },
                { "position", new Dictionary<string, object>
                    {
                        { "x", component.transform.position.x },
                        { "y", component.transform.position.y },
                        { "z", component.transform.position.z }
                    }
                }
            };

            // Add zone-specific info based on zone type
            var zoneTypeLower = zoneType.ToLowerInvariant();
            switch (zoneTypeLower)
            {
                case "damagezone":
                case "healzone":
                    info["effectValue"] = so.FindProperty("effectValue").floatValue;
                    info["effectInterval"] = so.FindProperty("effectInterval").floatValue;
                    break;
                case "teleport":
                    var teleportDest = so.FindProperty("teleportDestination").objectReferenceValue as Transform;
                    if (teleportDest != null)
                    {
                        info["teleportDestination"] = BuildGameObjectPath(teleportDest.gameObject);
                    }
                    info["preserveVelocity"] = so.FindProperty("preserveVelocity").boolValue;
                    info["preserveRotation"] = so.FindProperty("preserveRotation").boolValue;
                    break;
                case "speedboost":
                case "slowdown":
                    info["speedMultiplier"] = so.FindProperty("speedMultiplier").floatValue;
                    break;
                case "checkpoint":
                    var respawnOffset = so.FindProperty("respawnOffset").vector3Value;
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
            var component = ResolveTriggerZoneComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var zoneId = new SerializedObject(component).FindProperty("zoneId").stringValue;
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

            // Also clean up the generated script from tracker
            ScriptGenerator.Delete(zoneId);

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
            var component = ResolveTriggerZoneComponent(payload);

            Undo.RecordObject(component.gameObject, "Activate Trigger Zone");

            // Enable the GameObject
            component.gameObject.SetActive(true);

            // Enable colliders
            var collider = component.GetComponent<Collider>();
            if (collider != null)
            {
                Undo.RecordObject(collider, "Enable Collider");
                collider.enabled = true;
            }

            var collider2D = component.GetComponent<Collider2D>();
            if (collider2D != null)
            {
                Undo.RecordObject(collider2D, "Enable Collider2D");
                collider2D.enabled = true;
            }

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var zoneId = new SerializedObject(component).FindProperty("zoneId").stringValue;

            return CreateSuccessResponse(
                ("zoneId", zoneId),
                ("activated", true)
            );
        }

        private object DeactivateZone(Dictionary<string, object> payload)
        {
            var component = ResolveTriggerZoneComponent(payload);

            // Disable colliders (keep GameObject active for visibility)
            var collider = component.GetComponent<Collider>();
            if (collider != null)
            {
                Undo.RecordObject(collider, "Disable Collider");
                collider.enabled = false;
            }

            var collider2D = component.GetComponent<Collider2D>();
            if (collider2D != null)
            {
                Undo.RecordObject(collider2D, "Disable Collider2D");
                collider2D.enabled = false;
            }

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var zoneId = new SerializedObject(component).FindProperty("zoneId").stringValue;

            return CreateSuccessResponse(
                ("zoneId", zoneId),
                ("deactivated", true)
            );
        }

        private object ResetZone(Dictionary<string, object> payload)
        {
            var component = ResolveTriggerZoneComponent(payload);

            Undo.RecordObject(component.gameObject, "Reset Trigger Zone");

            // Enable GameObject
            component.gameObject.SetActive(true);

            // Reset trigger state via serialized property
            var so = new SerializedObject(component);
            var hasTriggeredProp = so.FindProperty("hasTriggered");
            if (hasTriggeredProp != null)
            {
                hasTriggeredProp.boolValue = false;
            }
            so.ApplyModifiedProperties();

            // Enable colliders
            var collider = component.GetComponent<Collider>();
            if (collider != null)
            {
                Undo.RecordObject(collider, "Enable Collider");
                collider.enabled = true;
            }

            var collider2D = component.GetComponent<Collider2D>();
            if (collider2D != null)
            {
                Undo.RecordObject(collider2D, "Enable Collider2D");
                collider2D.enabled = true;
            }

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var zoneId = new SerializedObject(component).FindProperty("zoneId").stringValue;

            return CreateSuccessResponse(
                ("zoneId", zoneId),
                ("reset", true)
            );
        }

        private object SetTeleportDestination(Dictionary<string, object> payload)
        {
            var component = ResolveTriggerZoneComponent(payload);

            var so = new SerializedObject(component);

            // Ensure zone type is Teleport
            var zoneTypeProp = so.FindProperty("zoneType");
            var teleportStr = ParseZoneType("teleport");
            var names = zoneTypeProp.enumDisplayNames;
            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(names[i], teleportStr, StringComparison.OrdinalIgnoreCase))
                {
                    zoneTypeProp.enumValueIndex = i;
                    break;
                }
            }

            var destinationPath = GetString(payload, "destinationPath");
            if (string.IsNullOrEmpty(destinationPath))
            {
                // Create destination from position
                if (payload.TryGetValue("destinationPosition", out var posObj) && posObj is Dictionary<string, object> posDict)
                {
                    var pos = GetVector3FromDict(posDict, Vector3.zero);

                    // Create a destination marker
                    var destGo = new GameObject($"{component.name}_Destination");
                    destGo.transform.position = pos;
                    Undo.RegisterCreatedObjectUndo(destGo, "Create Teleport Destination");

                    so.FindProperty("teleportDestination").objectReferenceValue = destGo.transform;
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
                so.FindProperty("teleportDestination").objectReferenceValue = destGo.transform;
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var zoneId = new SerializedObject(component).FindProperty("zoneId").stringValue;

            return CreateSuccessResponse(
                ("zoneId", zoneId),
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

            var component = CodeGenHelper.FindComponentInSceneByField("zoneId", zoneId);
            if (component == null)
            {
                return CreateSuccessResponse(("found", false), ("zoneId", zoneId));
            }

            var so = new SerializedObject(component);

            var zoneTypeProp = so.FindProperty("zoneType");
            var zoneType = zoneTypeProp.enumValueIndex < zoneTypeProp.enumDisplayNames.Length
                ? zoneTypeProp.enumDisplayNames[zoneTypeProp.enumValueIndex]
                : "Generic";

            return CreateSuccessResponse(
                ("found", true),
                ("zoneId", so.FindProperty("zoneId").stringValue),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("zoneType", zoneType),
                ("isActive", component.gameObject.activeInHierarchy)
            );
        }

        #endregion

        #region Helpers

        private Component ResolveTriggerZoneComponent(Dictionary<string, object> payload)
        {
            // Try by zoneId first
            var zoneId = GetString(payload, "zoneId");
            if (!string.IsNullOrEmpty(zoneId))
            {
                var byId = CodeGenHelper.FindComponentInSceneByField("zoneId", zoneId);
                if (byId != null) return byId;
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var byPath = CodeGenHelper.FindComponentByField(targetGo, "zoneId", null);
                    if (byPath != null) return byPath;
                    throw new InvalidOperationException($"No TriggerZone component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either zoneId or targetPath is required.");
        }

        private void ApplyTriggerZoneSettings(SerializedObject so, Dictionary<string, object> payload)
        {
            // Trigger settings
            if (payload.TryGetValue("requiredTag", out var tagObj))
            {
                so.FindProperty("requiredTag").stringValue = tagObj.ToString();
            }

            if (payload.TryGetValue("requireTriggerCollider", out var requireTriggerObj))
            {
                so.FindProperty("requireTriggerCollider").boolValue = Convert.ToBoolean(requireTriggerObj);
            }

            // Zone-specific settings
            if (payload.TryGetValue("effectValue", out var effectValObj))
            {
                so.FindProperty("effectValue").floatValue = Convert.ToSingle(effectValObj);
            }

            if (payload.TryGetValue("effectInterval", out var effectIntObj))
            {
                so.FindProperty("effectInterval").floatValue = Convert.ToSingle(effectIntObj);
            }

            if (payload.TryGetValue("speedMultiplier", out var speedMultObj))
            {
                so.FindProperty("speedMultiplier").floatValue = Convert.ToSingle(speedMultObj);
            }

            // Teleport settings
            if (payload.TryGetValue("preserveVelocity", out var preserveVelObj))
            {
                so.FindProperty("preserveVelocity").boolValue = Convert.ToBoolean(preserveVelObj);
            }

            if (payload.TryGetValue("preserveRotation", out var preserveRotObj))
            {
                so.FindProperty("preserveRotation").boolValue = Convert.ToBoolean(preserveRotObj);
            }

            // Checkpoint settings
            if (payload.TryGetValue("respawnOffset", out var respawnOffsetObj) && respawnOffsetObj is Dictionary<string, object> respawnDict)
            {
                var offset = GetVector3FromDict(respawnDict, Vector3.up);
                so.FindProperty("respawnOffset").vector3Value = offset;
            }

            // Visual settings
            if (payload.TryGetValue("changeColorOnEnter", out var changeColorObj))
            {
                so.FindProperty("changeColorOnEnter").boolValue = Convert.ToBoolean(changeColorObj);
            }

            if (payload.TryGetValue("activeColor", out var activeColorObj) && activeColorObj is Dictionary<string, object> activeColorDict)
            {
                var color = new Color(
                    activeColorDict.TryGetValue("r", out var r) ? Convert.ToSingle(r) : 0f,
                    activeColorDict.TryGetValue("g", out var g) ? Convert.ToSingle(g) : 1f,
                    activeColorDict.TryGetValue("b", out var b) ? Convert.ToSingle(b) : 0f,
                    activeColorDict.TryGetValue("a", out var a) ? Convert.ToSingle(a) : 1f
                );
                so.FindProperty("activeColor").colorValue = color;
            }

            if (payload.TryGetValue("inactiveColor", out var inactiveColorObj) && inactiveColorObj is Dictionary<string, object> inactiveColorDict)
            {
                var color = new Color(
                    inactiveColorDict.TryGetValue("r", out var r) ? Convert.ToSingle(r) : 0.5f,
                    inactiveColorDict.TryGetValue("g", out var g) ? Convert.ToSingle(g) : 0.5f,
                    inactiveColorDict.TryGetValue("b", out var b) ? Convert.ToSingle(b) : 0.5f,
                    inactiveColorDict.TryGetValue("a", out var a) ? Convert.ToSingle(a) : 1f
                );
                so.FindProperty("inactiveColor").colorValue = color;
            }
        }

        private string ParseZoneType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "generic" => "Generic",
                "checkpoint" => "Checkpoint",
                "damagezone" => "DamageZone",
                "healzone" => "HealZone",
                "teleport" => "Teleport",
                "speedboost" => "SpeedBoost",
                "slowdown" => "SlowDown",
                "killzone" => "KillZone",
                "safezone" => "SafeZone",
                "trigger" => "Trigger",
                _ => "Generic"
            };
        }

        private string ParseTriggerMode(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "once" => "Once",
                "onceperentity" => "OncePerEntity",
                "repeat" => "Repeat",
                "whileinside" => "WhileInside",
                _ => "Repeat"
            };
        }

        #endregion
    }
}
