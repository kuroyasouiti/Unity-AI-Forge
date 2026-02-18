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
    /// GameKit Collectible handler: create and manage collectible items for games.
    /// Supports coins, health pickups, power-ups, keys, and custom collectibles.
    /// Uses code generation to produce standalone Collectible scripts with zero package dependency.
    /// </summary>
    public class GameKitCollectibleHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "update", "inspect", "delete",
            "collect", "respawn", "reset", "findByCollectibleId"
        };

        public override string Category => "gamekitCollectible";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateCollectible(payload),
                "update" => UpdateCollectible(payload),
                "inspect" => InspectCollectible(payload),
                "delete" => DeleteCollectible(payload),
                "collect" => CollectItem(payload),
                "respawn" => RespawnItem(payload),
                "reset" => ResetItem(payload),
                "findByCollectibleId" => FindByCollectibleId(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit Collectible operation: {operation}")
            };
        }

        #region Create

        private object CreateCollectible(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            GameObject targetGo;

            if (string.IsNullOrEmpty(targetPath))
            {
                // Create a new GameObject
                var name = GetString(payload, "name") ?? "Collectible";
                targetGo = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(targetGo, "Create Collectible");

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

            // Check if already has a collectible component (by checking for collectibleId field)
            var existingCollectible = CodeGenHelper.FindComponentByField(targetGo, "collectibleId", null);
            if (existingCollectible != null)
            {
                throw new InvalidOperationException(
                    $"GameObject '{BuildGameObjectPath(targetGo)}' already has a Collectible component.");
            }

            var collectibleId = GetString(payload, "collectibleId") ?? $"Collectible_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var collectibleType = ParseCollectibleType(GetString(payload, "collectibleType") ?? "coin");
            var collectionBehavior = ParseCollectionBehavior(GetString(payload, "collectionBehavior") ?? "destroy");
            var value = GetFloat(payload, "value", 1f);
            var intValue = payload.TryGetValue("intValue", out var intValueObj) ? Convert.ToInt32(intValueObj) : 0;
            var customTypeName = GetString(payload, "customTypeName") ?? "";
            var respawnDelay = GetFloat(payload, "respawnDelay", 3f);
            var is2D = GetBool(payload, "is2D", false);
            var colliderRadius = GetFloat(payload, "colliderRadius", 0.5f);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(collectibleId, "Collectible");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "COLLECTIBLE_ID", collectibleId },
                { "COLLECTIBLE_TYPE", collectibleType },
                { "COLLECTION_BEHAVIOR", collectionBehavior },
                { "VALUE", value },
                { "INT_VALUE", intValue },
                { "CUSTOM_TYPE_NAME", customTypeName },
                { "RESPAWN_DELAY", respawnDelay },
                { "IS_2D", is2D },
                { "COLLIDER_RADIUS", colliderRadius }
            };

            // Build properties to set after component is added
            var propertiesToSet = new Dictionary<string, object>();
            if (payload.TryGetValue("collectable", out var collectableObj))
                propertiesToSet["collectable"] = Convert.ToBoolean(collectableObj);
            if (payload.TryGetValue("requiredTag", out var tagObj))
                propertiesToSet["requiredTag"] = tagObj.ToString();
            if (payload.TryGetValue("enableFloatAnimation", out var floatAnimObj))
                propertiesToSet["enableFloatAnimation"] = Convert.ToBoolean(floatAnimObj);
            if (payload.TryGetValue("floatAmplitude", out var ampObj))
                propertiesToSet["floatAmplitude"] = Convert.ToSingle(ampObj);
            if (payload.TryGetValue("floatFrequency", out var freqObj))
                propertiesToSet["floatFrequency"] = Convert.ToSingle(freqObj);
            if (payload.TryGetValue("enableRotation", out var rotAnimObj))
                propertiesToSet["enableRotation"] = Convert.ToBoolean(rotAnimObj);
            if (payload.TryGetValue("rotationSpeed", out var rotSpeedObj))
                propertiesToSet["rotationSpeed"] = Convert.ToSingle(rotSpeedObj);
            if (payload.TryGetValue("rotationAxis", out var rotAxisObj) && rotAxisObj is Dictionary<string, object> rotAxisDict)
                propertiesToSet["rotationAxis"] = rotAxisDict;

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "Collectible", collectibleId, className, variables, outputDir,
                propertiesToSet.Count > 0 ? propertiesToSet : null);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate Collectible script.");
            }

            // Add collider if none exists
            if (targetGo.GetComponent<Collider>() == null && targetGo.GetComponent<Collider2D>() == null)
            {
                if (is2D)
                {
                    var collider = Undo.AddComponent<CircleCollider2D>(targetGo);
                    collider.isTrigger = true;
                    collider.radius = colliderRadius;
                }
                else
                {
                    var collider = Undo.AddComponent<SphereCollider>(targetGo);
                    collider.isTrigger = true;
                    collider.radius = colliderRadius;
                }
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["collectibleId"] = collectibleId;
            result["path"] = BuildGameObjectPath(targetGo);
            result["collectibleType"] = collectibleType;
            result["collectionBehavior"] = collectionBehavior;
            result["value"] = value;

            return result;
        }

        #endregion

        #region Update

        private object UpdateCollectible(Dictionary<string, object> payload)
        {
            var component = ResolveCollectibleComponent(payload);

            Undo.RecordObject(component, "Update Collectible");

            var so = new SerializedObject(component);

            if (payload.TryGetValue("collectibleType", out var typeObj))
            {
                var collectibleType = ParseCollectibleType(typeObj.ToString());
                var typeProp = so.FindProperty("collectibleType");
                var names = typeProp.enumDisplayNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], collectibleType, StringComparison.OrdinalIgnoreCase))
                    {
                        typeProp.enumValueIndex = i;
                        break;
                    }
                }
            }

            if (payload.TryGetValue("collectionBehavior", out var behaviorObj))
            {
                var behavior = ParseCollectionBehavior(behaviorObj.ToString());
                var behaviorProp = so.FindProperty("collectionBehavior");
                var names = behaviorProp.enumDisplayNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], behavior, StringComparison.OrdinalIgnoreCase))
                    {
                        behaviorProp.enumValueIndex = i;
                        break;
                    }
                }
            }

            if (payload.TryGetValue("value", out var valueObj))
            {
                so.FindProperty("value").floatValue = Convert.ToSingle(valueObj);
            }

            if (payload.TryGetValue("intValue", out var intValueObj))
            {
                so.FindProperty("intValue").intValue = Convert.ToInt32(intValueObj);
            }

            if (payload.TryGetValue("customTypeName", out var customTypeObj))
            {
                so.FindProperty("customTypeName").stringValue = customTypeObj.ToString();
            }

            ApplyCollectibleSettings(so, payload);
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var collectibleId = new SerializedObject(component).FindProperty("collectibleId").stringValue;

            return CreateSuccessResponse(
                ("collectibleId", collectibleId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectCollectible(Dictionary<string, object> payload)
        {
            var component = ResolveCollectibleComponent(payload);
            var so = new SerializedObject(component);

            var collectibleTypeProp = so.FindProperty("collectibleType");
            var collectibleType = collectibleTypeProp.enumValueIndex < collectibleTypeProp.enumDisplayNames.Length
                ? collectibleTypeProp.enumDisplayNames[collectibleTypeProp.enumValueIndex]
                : "Coin";

            var behaviorProp = so.FindProperty("collectionBehavior");
            var collectionBehavior = behaviorProp.enumValueIndex < behaviorProp.enumDisplayNames.Length
                ? behaviorProp.enumDisplayNames[behaviorProp.enumValueIndex]
                : "Destroy";

            var info = new Dictionary<string, object>
            {
                { "collectibleId", so.FindProperty("collectibleId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "collectibleType", collectibleType },
                { "customTypeName", so.FindProperty("customTypeName").stringValue },
                { "value", so.FindProperty("value").floatValue },
                { "intValue", so.FindProperty("intValue").intValue },
                { "collectionBehavior", collectionBehavior },
                { "respawnDelay", so.FindProperty("respawnDelay").floatValue },
                { "isCollectable", so.FindProperty("collectable").boolValue },
                { "requiredTag", so.FindProperty("requiredTag").stringValue },
                { "position", new Dictionary<string, object>
                    {
                        { "x", component.transform.position.x },
                        { "y", component.transform.position.y },
                        { "z", component.transform.position.z }
                    }
                }
            };

            return CreateSuccessResponse(("collectible", info));
        }

        #endregion

        #region Delete

        private object DeleteCollectible(Dictionary<string, object> payload)
        {
            var component = ResolveCollectibleComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var collectibleId = new SerializedObject(component).FindProperty("collectibleId").stringValue;
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
            ScriptGenerator.Delete(collectibleId);

            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("collectibleId", collectibleId),
                ("path", path),
                ("deleted", true),
                ("gameObjectDeleted", deleteGameObject)
            );
        }

        #endregion

        #region Runtime Operations

        private object CollectItem(Dictionary<string, object> payload)
        {
            var component = ResolveCollectibleComponent(payload);
            var so = new SerializedObject(component);

            // Note: Actual collection with effects happens in play mode
            return CreateSuccessResponse(
                ("collectibleId", so.FindProperty("collectibleId").stringValue),
                ("collected", true),
                ("value", so.FindProperty("value").floatValue),
                ("note", "Collection simulated. Full effects require play mode.")
            );
        }

        private object RespawnItem(Dictionary<string, object> payload)
        {
            var component = ResolveCollectibleComponent(payload);

            // Make visible and collectable again
            component.gameObject.SetActive(true);

            var renderer = component.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = true;

            var collider = component.GetComponent<Collider>();
            if (collider != null) collider.enabled = true;

            var collider2D = component.GetComponent<Collider2D>();
            if (collider2D != null) collider2D.enabled = true;

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var so = new SerializedObject(component);

            return CreateSuccessResponse(
                ("collectibleId", so.FindProperty("collectibleId").stringValue),
                ("respawned", true)
            );
        }

        private object ResetItem(Dictionary<string, object> payload)
        {
            var component = ResolveCollectibleComponent(payload);

            // Reset position if provided
            if (payload.TryGetValue("position", out var posObj) && posObj is Dictionary<string, object> posDict)
            {
                component.transform.position = GetVector3FromDict(posDict, component.transform.position);
            }

            // Make visible and collectable
            component.gameObject.SetActive(true);

            var renderer = component.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = true;

            var collider = component.GetComponent<Collider>();
            if (collider != null) collider.enabled = true;

            var collider2D = component.GetComponent<Collider2D>();
            if (collider2D != null) collider2D.enabled = true;

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var so = new SerializedObject(component);

            return CreateSuccessResponse(
                ("collectibleId", so.FindProperty("collectibleId").stringValue),
                ("reset", true),
                ("position", new Dictionary<string, object>
                {
                    { "x", component.transform.position.x },
                    { "y", component.transform.position.y },
                    { "z", component.transform.position.z }
                })
            );
        }

        #endregion

        #region Find

        private object FindByCollectibleId(Dictionary<string, object> payload)
        {
            var collectibleId = GetString(payload, "collectibleId");
            if (string.IsNullOrEmpty(collectibleId))
            {
                throw new InvalidOperationException("collectibleId is required for findByCollectibleId.");
            }

            var component = CodeGenHelper.FindComponentInSceneByField("collectibleId", collectibleId);
            if (component == null)
            {
                return CreateSuccessResponse(("found", false), ("collectibleId", collectibleId));
            }

            var so = new SerializedObject(component);

            var collectibleTypeProp = so.FindProperty("collectibleType");
            var collectibleType = collectibleTypeProp.enumValueIndex < collectibleTypeProp.enumDisplayNames.Length
                ? collectibleTypeProp.enumDisplayNames[collectibleTypeProp.enumValueIndex]
                : "Coin";

            return CreateSuccessResponse(
                ("found", true),
                ("collectibleId", so.FindProperty("collectibleId").stringValue),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("collectibleType", collectibleType),
                ("value", so.FindProperty("value").floatValue)
            );
        }

        #endregion

        #region Helpers

        private Component ResolveCollectibleComponent(Dictionary<string, object> payload)
        {
            // Try by collectibleId first
            var collectibleId = GetString(payload, "collectibleId");
            if (!string.IsNullOrEmpty(collectibleId))
            {
                var byId = CodeGenHelper.FindComponentInSceneByField("collectibleId", collectibleId);
                if (byId != null) return byId;
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var byPath = CodeGenHelper.FindComponentByField(targetGo, "collectibleId", null);
                    if (byPath != null) return byPath;
                    throw new InvalidOperationException($"No Collectible component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either collectibleId or targetPath is required.");
        }

        private void ApplyCollectibleSettings(SerializedObject so, Dictionary<string, object> payload)
        {
            if (payload.TryGetValue("respawnDelay", out var respawnObj))
            {
                so.FindProperty("respawnDelay").floatValue = Convert.ToSingle(respawnObj);
            }

            if (payload.TryGetValue("collectable", out var collectableObj))
            {
                so.FindProperty("collectable").boolValue = Convert.ToBoolean(collectableObj);
            }

            if (payload.TryGetValue("requiredTag", out var tagObj))
            {
                so.FindProperty("requiredTag").stringValue = tagObj.ToString();
            }

            // Animation settings
            if (payload.TryGetValue("enableFloatAnimation", out var floatAnimObj))
            {
                so.FindProperty("enableFloatAnimation").boolValue = Convert.ToBoolean(floatAnimObj);
            }

            if (payload.TryGetValue("floatAmplitude", out var ampObj))
            {
                so.FindProperty("floatAmplitude").floatValue = Convert.ToSingle(ampObj);
            }

            if (payload.TryGetValue("floatFrequency", out var freqObj))
            {
                so.FindProperty("floatFrequency").floatValue = Convert.ToSingle(freqObj);
            }

            if (payload.TryGetValue("enableRotation", out var rotAnimObj))
            {
                so.FindProperty("enableRotation").boolValue = Convert.ToBoolean(rotAnimObj);
            }

            if (payload.TryGetValue("rotationSpeed", out var rotSpeedObj))
            {
                so.FindProperty("rotationSpeed").floatValue = Convert.ToSingle(rotSpeedObj);
            }

            if (payload.TryGetValue("rotationAxis", out var rotAxisObj) && rotAxisObj is Dictionary<string, object> rotAxisDict)
            {
                so.FindProperty("rotationAxis").vector3Value = GetVector3FromDict(rotAxisDict, Vector3.up);
            }
        }

        private string ParseCollectibleType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "coin" => "Coin",
                "health" => "Health",
                "mana" => "Mana",
                "powerup" => "PowerUp",
                "key" => "Key",
                "ammo" => "Ammo",
                "experience" => "Experience",
                "custom" => "Custom",
                _ => "Coin"
            };
        }

        private string ParseCollectionBehavior(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "destroy" => "Destroy",
                "disable" => "Disable",
                "respawn" => "Respawn",
                _ => "Destroy"
            };
        }

        #endregion
    }
}
