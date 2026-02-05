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
    /// GameKit Collectible handler: create and manage collectible items for games.
    /// Supports coins, health pickups, power-ups, keys, and custom collectibles.
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

        protected override bool RequiresCompilationWait(string operation) => false;

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

            // Check if already has collectible component
            var existingCollectible = targetGo.GetComponent<GameKitCollectible>();
            if (existingCollectible != null)
            {
                throw new InvalidOperationException($"GameObject '{BuildGameObjectPath(targetGo)}' already has a GameKitCollectible component.");
            }

            var collectibleId = GetString(payload, "collectibleId") ?? $"Collectible_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var collectibleType = ParseCollectibleType(GetString(payload, "collectibleType") ?? "coin");
            var collectionBehavior = ParseCollectionBehavior(GetString(payload, "collectionBehavior") ?? "destroy");

            // Add component
            var collectible = Undo.AddComponent<GameKitCollectible>(targetGo);

            // Initialize via serialized object
            var serialized = new SerializedObject(collectible);
            serialized.FindProperty("collectibleId").stringValue = collectibleId;
            serialized.FindProperty("collectibleType").enumValueIndex = (int)collectibleType;
            serialized.FindProperty("collectionBehavior").enumValueIndex = (int)collectionBehavior;

            // Set value
            if (payload.TryGetValue("value", out var valueObj))
            {
                serialized.FindProperty("value").floatValue = Convert.ToSingle(valueObj);
            }
            if (payload.TryGetValue("intValue", out var intValueObj))
            {
                serialized.FindProperty("intValue").intValue = Convert.ToInt32(intValueObj);
            }

            // Set custom type name
            if (payload.TryGetValue("customTypeName", out var customTypeObj))
            {
                serialized.FindProperty("customTypeName").stringValue = customTypeObj.ToString();
            }

            // Apply additional settings
            ApplyCollectibleSettings(serialized, payload);
            serialized.ApplyModifiedProperties();

            // Add collider if none exists
            if (targetGo.GetComponent<Collider>() == null && targetGo.GetComponent<Collider2D>() == null)
            {
                var is2D = GetBool(payload, "is2D", false);
                if (is2D)
                {
                    var collider = Undo.AddComponent<CircleCollider2D>(targetGo);
                    collider.isTrigger = true;
                    collider.radius = GetFloat(payload, "colliderRadius", 0.5f);
                }
                else
                {
                    var collider = Undo.AddComponent<SphereCollider>(targetGo);
                    collider.isTrigger = true;
                    collider.radius = GetFloat(payload, "colliderRadius", 0.5f);
                }
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("collectibleId", collectibleId),
                ("path", BuildGameObjectPath(targetGo)),
                ("collectibleType", collectibleType.ToString()),
                ("collectionBehavior", collectionBehavior.ToString()),
                ("value", serialized.FindProperty("value").floatValue)
            );
        }

        #endregion

        #region Update

        private object UpdateCollectible(Dictionary<string, object> payload)
        {
            var collectible = ResolveCollectibleComponent(payload);

            Undo.RecordObject(collectible, "Update GameKit Collectible");

            var serialized = new SerializedObject(collectible);

            if (payload.TryGetValue("collectibleType", out var typeObj))
            {
                var collectibleType = ParseCollectibleType(typeObj.ToString());
                serialized.FindProperty("collectibleType").enumValueIndex = (int)collectibleType;
            }

            if (payload.TryGetValue("collectionBehavior", out var behaviorObj))
            {
                var behavior = ParseCollectionBehavior(behaviorObj.ToString());
                serialized.FindProperty("collectionBehavior").enumValueIndex = (int)behavior;
            }

            if (payload.TryGetValue("value", out var valueObj))
            {
                serialized.FindProperty("value").floatValue = Convert.ToSingle(valueObj);
            }

            if (payload.TryGetValue("intValue", out var intValueObj))
            {
                serialized.FindProperty("intValue").intValue = Convert.ToInt32(intValueObj);
            }

            if (payload.TryGetValue("customTypeName", out var customTypeObj))
            {
                serialized.FindProperty("customTypeName").stringValue = customTypeObj.ToString();
            }

            ApplyCollectibleSettings(serialized, payload);
            serialized.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(collectible.gameObject.scene);

            return CreateSuccessResponse(
                ("collectibleId", collectible.CollectibleId),
                ("path", BuildGameObjectPath(collectible.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectCollectible(Dictionary<string, object> payload)
        {
            var collectible = ResolveCollectibleComponent(payload);

            var info = new Dictionary<string, object>
            {
                { "collectibleId", collectible.CollectibleId },
                { "path", BuildGameObjectPath(collectible.gameObject) },
                { "collectibleType", collectible.Type.ToString() },
                { "customTypeName", collectible.CustomTypeName },
                { "value", collectible.Value },
                { "intValue", collectible.IntValue },
                { "collectionBehavior", collectible.Behavior.ToString() },
                { "respawnDelay", collectible.RespawnDelay },
                { "isCollectable", collectible.IsCollectable },
                { "isCollected", collectible.IsCollected },
                { "requiredTag", collectible.RequiredTag },
                { "position", new Dictionary<string, object>
                    {
                        { "x", collectible.transform.position.x },
                        { "y", collectible.transform.position.y },
                        { "z", collectible.transform.position.z }
                    }
                }
            };

            return CreateSuccessResponse(("collectible", info));
        }

        #endregion

        #region Delete

        private object DeleteCollectible(Dictionary<string, object> payload)
        {
            var collectible = ResolveCollectibleComponent(payload);
            var path = BuildGameObjectPath(collectible.gameObject);
            var collectibleId = collectible.CollectibleId;
            var scene = collectible.gameObject.scene;

            var deleteGameObject = GetBool(payload, "deleteGameObject", false);
            if (deleteGameObject)
            {
                Undo.DestroyObjectImmediate(collectible.gameObject);
            }
            else
            {
                Undo.DestroyObjectImmediate(collectible);
            }

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
            var collectible = ResolveCollectibleComponent(payload);

            // In editor mode, simulate collection
            var serialized = new SerializedObject(collectible);

            // Note: Actual collection with effects happens in play mode
            return CreateSuccessResponse(
                ("collectibleId", collectible.CollectibleId),
                ("collected", true),
                ("value", collectible.Value),
                ("note", "Collection simulated. Full effects require play mode.")
            );
        }

        private object RespawnItem(Dictionary<string, object> payload)
        {
            var collectible = ResolveCollectibleComponent(payload);

            // Make visible and collectable again
            collectible.gameObject.SetActive(true);

            var renderer = collectible.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = true;

            var collider = collectible.GetComponent<Collider>();
            if (collider != null) collider.enabled = true;

            var collider2D = collectible.GetComponent<Collider2D>();
            if (collider2D != null) collider2D.enabled = true;

            EditorSceneManager.MarkSceneDirty(collectible.gameObject.scene);

            return CreateSuccessResponse(
                ("collectibleId", collectible.CollectibleId),
                ("respawned", true)
            );
        }

        private object ResetItem(Dictionary<string, object> payload)
        {
            var collectible = ResolveCollectibleComponent(payload);

            // Reset position if provided
            if (payload.TryGetValue("position", out var posObj) && posObj is Dictionary<string, object> posDict)
            {
                collectible.transform.position = GetVector3FromDict(posDict, collectible.transform.position);
            }

            // Make visible and collectable
            collectible.gameObject.SetActive(true);

            var renderer = collectible.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = true;

            var collider = collectible.GetComponent<Collider>();
            if (collider != null) collider.enabled = true;

            var collider2D = collectible.GetComponent<Collider2D>();
            if (collider2D != null) collider2D.enabled = true;

            EditorSceneManager.MarkSceneDirty(collectible.gameObject.scene);

            return CreateSuccessResponse(
                ("collectibleId", collectible.CollectibleId),
                ("reset", true),
                ("position", new Dictionary<string, object>
                {
                    { "x", collectible.transform.position.x },
                    { "y", collectible.transform.position.y },
                    { "z", collectible.transform.position.z }
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

            var collectible = FindCollectibleById(collectibleId);
            if (collectible == null)
            {
                return CreateSuccessResponse(("found", false), ("collectibleId", collectibleId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("collectibleId", collectible.CollectibleId),
                ("path", BuildGameObjectPath(collectible.gameObject)),
                ("collectibleType", collectible.Type.ToString()),
                ("value", collectible.Value),
                ("isCollected", collectible.IsCollected)
            );
        }

        #endregion

        #region Helpers

        private GameKitCollectible ResolveCollectibleComponent(Dictionary<string, object> payload)
        {
            // Try by collectibleId first
            var collectibleId = GetString(payload, "collectibleId");
            if (!string.IsNullOrEmpty(collectibleId))
            {
                var byId = FindCollectibleById(collectibleId);
                if (byId != null) return byId;
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var byPath = targetGo.GetComponent<GameKitCollectible>();
                    if (byPath != null) return byPath;
                    throw new InvalidOperationException($"No GameKitCollectible component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either collectibleId or targetPath is required.");
        }

        private GameKitCollectible FindCollectibleById(string collectibleId)
        {
            var collectibles = UnityEngine.Object.FindObjectsByType<GameKitCollectible>(FindObjectsSortMode.None);
            foreach (var collectible in collectibles)
            {
                if (collectible.CollectibleId == collectibleId)
                {
                    return collectible;
                }
            }
            return null;
        }

        private void ApplyCollectibleSettings(SerializedObject serialized, Dictionary<string, object> payload)
        {
            if (payload.TryGetValue("respawnDelay", out var respawnObj))
            {
                serialized.FindProperty("respawnDelay").floatValue = Convert.ToSingle(respawnObj);
            }

            if (payload.TryGetValue("collectable", out var collectableObj))
            {
                serialized.FindProperty("collectable").boolValue = Convert.ToBoolean(collectableObj);
            }

            if (payload.TryGetValue("requiredTag", out var tagObj))
            {
                serialized.FindProperty("requiredTag").stringValue = tagObj.ToString();
            }

            // Animation settings
            if (payload.TryGetValue("enableFloatAnimation", out var floatAnimObj))
            {
                serialized.FindProperty("enableFloatAnimation").boolValue = Convert.ToBoolean(floatAnimObj);
            }

            if (payload.TryGetValue("floatAmplitude", out var ampObj))
            {
                serialized.FindProperty("floatAmplitude").floatValue = Convert.ToSingle(ampObj);
            }

            if (payload.TryGetValue("floatFrequency", out var freqObj))
            {
                serialized.FindProperty("floatFrequency").floatValue = Convert.ToSingle(freqObj);
            }

            if (payload.TryGetValue("enableRotation", out var rotAnimObj))
            {
                serialized.FindProperty("enableRotation").boolValue = Convert.ToBoolean(rotAnimObj);
            }

            if (payload.TryGetValue("rotationSpeed", out var rotSpeedObj))
            {
                serialized.FindProperty("rotationSpeed").floatValue = Convert.ToSingle(rotSpeedObj);
            }

            if (payload.TryGetValue("rotationAxis", out var rotAxisObj) && rotAxisObj is Dictionary<string, object> rotAxisDict)
            {
                serialized.FindProperty("rotationAxis").vector3Value = GetVector3FromDict(rotAxisDict, Vector3.up);
            }
        }

        private GameKitCollectible.CollectibleType ParseCollectibleType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "coin" => GameKitCollectible.CollectibleType.Coin,
                "health" => GameKitCollectible.CollectibleType.Health,
                "mana" => GameKitCollectible.CollectibleType.Mana,
                "powerup" => GameKitCollectible.CollectibleType.PowerUp,
                "key" => GameKitCollectible.CollectibleType.Key,
                "ammo" => GameKitCollectible.CollectibleType.Ammo,
                "experience" => GameKitCollectible.CollectibleType.Experience,
                "custom" => GameKitCollectible.CollectibleType.Custom,
                _ => GameKitCollectible.CollectibleType.Coin
            };
        }

        private GameKitCollectible.CollectionBehavior ParseCollectionBehavior(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "destroy" => GameKitCollectible.CollectionBehavior.Destroy,
                "disable" => GameKitCollectible.CollectionBehavior.Disable,
                "respawn" => GameKitCollectible.CollectionBehavior.Respawn,
                _ => GameKitCollectible.CollectionBehavior.Destroy
            };
        }

        #endregion
    }
}
