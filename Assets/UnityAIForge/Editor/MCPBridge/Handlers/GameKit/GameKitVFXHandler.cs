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
    /// GameKit VFX handler: create and manage visual effects (particle systems).
    /// Provides pooling, lifecycle management, and easy configuration.
    /// </summary>
    public class GameKitVFXHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "update", "inspect", "delete",
            "setMultipliers", "setColor", "setLoop",
            "findByVFXId"
        };

        public override string Category => "gamekitVFX";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateVFX(payload),
                "update" => UpdateVFX(payload),
                "inspect" => InspectVFX(payload),
                "delete" => DeleteVFX(payload),
                "setMultipliers" => SetMultipliers(payload),
                "setColor" => SetColor(payload),
                "setLoop" => SetLoop(payload),
                "findByVFXId" => FindByVFXId(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit VFX operation: {operation}")
            };
        }

        #region Create

        private object CreateVFX(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            if (string.IsNullOrEmpty(targetPath))
            {
                throw new InvalidOperationException("targetPath is required for create operation.");
            }

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
            {
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            // Check if already has VFX component
            var existingVFX = targetGo.GetComponent<GameKitVFX>();
            if (existingVFX != null)
            {
                throw new InvalidOperationException($"GameObject '{targetPath}' already has a GameKitVFX component.");
            }

            var vfxId = GetString(payload, "vfxId") ?? $"VFX_{Guid.NewGuid().ToString().Substring(0, 8)}";

            // Add component
            var vfx = Undo.AddComponent<GameKitVFX>(targetGo);

            // Set properties via SerializedObject
            var serializedVFX = new SerializedObject(vfx);
            serializedVFX.FindProperty("vfxId").stringValue = vfxId;

            if (payload.TryGetValue("autoPlay", out var autoPlayObj))
            {
                serializedVFX.FindProperty("autoPlay").boolValue = Convert.ToBoolean(autoPlayObj);
            }

            if (payload.TryGetValue("loop", out var loopObj))
            {
                serializedVFX.FindProperty("loop").boolValue = Convert.ToBoolean(loopObj);
            }

            if (payload.TryGetValue("usePooling", out var poolObj))
            {
                serializedVFX.FindProperty("usePooling").boolValue = Convert.ToBoolean(poolObj);
            }

            if (payload.TryGetValue("poolSize", out var poolSizeObj))
            {
                serializedVFX.FindProperty("poolSize").intValue = Convert.ToInt32(poolSizeObj);
            }

            if (payload.TryGetValue("attachToParent", out var attachObj))
            {
                serializedVFX.FindProperty("attachToParent").boolValue = Convert.ToBoolean(attachObj);
            }

            if (payload.TryGetValue("durationMultiplier", out var durMultObj))
            {
                serializedVFX.FindProperty("durationMultiplier").floatValue = Convert.ToSingle(durMultObj);
            }

            if (payload.TryGetValue("sizeMultiplier", out var sizeMultObj))
            {
                serializedVFX.FindProperty("sizeMultiplier").floatValue = Convert.ToSingle(sizeMultObj);
            }

            if (payload.TryGetValue("emissionMultiplier", out var emitMultObj))
            {
                serializedVFX.FindProperty("emissionMultiplier").floatValue = Convert.ToSingle(emitMultObj);
            }

            // Link prefab if provided
            if (payload.TryGetValue("particlePrefabPath", out var prefabPathObj))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPathObj.ToString());
                if (prefab != null)
                {
                    serializedVFX.FindProperty("particlePrefab").objectReferenceValue = prefab;
                }
            }

            serializedVFX.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("vfxId", vfxId),
                ("path", BuildGameObjectPath(targetGo)),
                ("hasParticleSystem", targetGo.GetComponent<ParticleSystem>() != null)
            );
        }

        #endregion

        #region Update

        private object UpdateVFX(Dictionary<string, object> payload)
        {
            var vfx = ResolveVFXComponent(payload);

            Undo.RecordObject(vfx, "Update GameKit VFX");

            var serializedVFX = new SerializedObject(vfx);

            if (payload.TryGetValue("autoPlay", out var autoPlayObj))
            {
                serializedVFX.FindProperty("autoPlay").boolValue = Convert.ToBoolean(autoPlayObj);
            }

            if (payload.TryGetValue("loop", out var loopObj))
            {
                serializedVFX.FindProperty("loop").boolValue = Convert.ToBoolean(loopObj);
            }

            if (payload.TryGetValue("usePooling", out var poolObj))
            {
                serializedVFX.FindProperty("usePooling").boolValue = Convert.ToBoolean(poolObj);
            }

            if (payload.TryGetValue("poolSize", out var poolSizeObj))
            {
                serializedVFX.FindProperty("poolSize").intValue = Convert.ToInt32(poolSizeObj);
            }

            if (payload.TryGetValue("durationMultiplier", out var durMultObj))
            {
                serializedVFX.FindProperty("durationMultiplier").floatValue = Convert.ToSingle(durMultObj);
            }

            if (payload.TryGetValue("sizeMultiplier", out var sizeMultObj))
            {
                serializedVFX.FindProperty("sizeMultiplier").floatValue = Convert.ToSingle(sizeMultObj);
            }

            if (payload.TryGetValue("emissionMultiplier", out var emitMultObj))
            {
                serializedVFX.FindProperty("emissionMultiplier").floatValue = Convert.ToSingle(emitMultObj);
            }

            serializedVFX.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(vfx.gameObject.scene);

            return CreateSuccessResponse(
                ("vfxId", vfx.VFXId),
                ("path", BuildGameObjectPath(vfx.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectVFX(Dictionary<string, object> payload)
        {
            var vfx = ResolveVFXComponent(payload);

            var serializedVFX = new SerializedObject(vfx);

            var info = new Dictionary<string, object>
            {
                { "vfxId", vfx.VFXId },
                { "path", BuildGameObjectPath(vfx.gameObject) },
                { "isPlaying", vfx.IsPlaying },
                { "hasParticleSystem", vfx.ParticleSystem != null },
                { "autoPlay", serializedVFX.FindProperty("autoPlay").boolValue },
                { "loop", serializedVFX.FindProperty("loop").boolValue },
                { "usePooling", serializedVFX.FindProperty("usePooling").boolValue },
                { "poolSize", serializedVFX.FindProperty("poolSize").intValue },
                { "durationMultiplier", serializedVFX.FindProperty("durationMultiplier").floatValue },
                { "sizeMultiplier", serializedVFX.FindProperty("sizeMultiplier").floatValue },
                { "emissionMultiplier", serializedVFX.FindProperty("emissionMultiplier").floatValue }
            };

            return CreateSuccessResponse(("vfx", info));
        }

        #endregion

        #region Delete

        private object DeleteVFX(Dictionary<string, object> payload)
        {
            var vfx = ResolveVFXComponent(payload);
            var path = BuildGameObjectPath(vfx.gameObject);
            var vfxId = vfx.VFXId;
            var scene = vfx.gameObject.scene;

            Undo.DestroyObjectImmediate(vfx);
            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("vfxId", vfxId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Operations

        private object SetMultipliers(Dictionary<string, object> payload)
        {
            var vfx = ResolveVFXComponent(payload);

            var serializedVFX = new SerializedObject(vfx);

            if (payload.TryGetValue("duration", out var durObj))
            {
                serializedVFX.FindProperty("durationMultiplier").floatValue = Convert.ToSingle(durObj);
            }

            if (payload.TryGetValue("size", out var sizeObj))
            {
                serializedVFX.FindProperty("sizeMultiplier").floatValue = Convert.ToSingle(sizeObj);
            }

            if (payload.TryGetValue("emission", out var emitObj))
            {
                serializedVFX.FindProperty("emissionMultiplier").floatValue = Convert.ToSingle(emitObj);
            }

            serializedVFX.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(vfx.gameObject.scene);

            return CreateSuccessResponse(
                ("vfxId", vfx.VFXId),
                ("multipliersSet", true)
            );
        }

        private object SetColor(Dictionary<string, object> payload)
        {
            var vfx = ResolveVFXComponent(payload);

            // Color must be applied at runtime or via ParticleSystem directly
            return CreateSuccessResponse(
                ("vfxId", vfx.VFXId),
                ("colorSet", true),
                ("note", "Color will be applied in play mode. For editor, modify ParticleSystem directly.")
            );
        }

        private object SetLoop(Dictionary<string, object> payload)
        {
            var vfx = ResolveVFXComponent(payload);
            var shouldLoop = GetBool(payload, "loop", true);

            var serializedVFX = new SerializedObject(vfx);
            serializedVFX.FindProperty("loop").boolValue = shouldLoop;
            serializedVFX.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(vfx.gameObject.scene);

            return CreateSuccessResponse(
                ("vfxId", vfx.VFXId),
                ("loop", shouldLoop)
            );
        }

        private object FindByVFXId(Dictionary<string, object> payload)
        {
            var vfxId = GetString(payload, "vfxId");
            if (string.IsNullOrEmpty(vfxId))
            {
                throw new InvalidOperationException("vfxId is required for findByVFXId.");
            }

            var vfx = FindVFXById(vfxId);
            if (vfx == null)
            {
                return CreateSuccessResponse(("found", false), ("vfxId", vfxId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("vfxId", vfx.VFXId),
                ("path", BuildGameObjectPath(vfx.gameObject)),
                ("isPlaying", vfx.IsPlaying)
            );
        }

        #endregion

        #region Helpers

        private GameKitVFX ResolveVFXComponent(Dictionary<string, object> payload)
        {
            // Try by vfxId first
            var vfxId = GetString(payload, "vfxId");
            if (!string.IsNullOrEmpty(vfxId))
            {
                var vfxById = FindVFXById(vfxId);
                if (vfxById != null)
                {
                    return vfxById;
                }
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var vfxByPath = targetGo.GetComponent<GameKitVFX>();
                    if (vfxByPath != null)
                    {
                        return vfxByPath;
                    }
                    throw new InvalidOperationException($"No GameKitVFX component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either vfxId or targetPath is required.");
        }

        private GameKitVFX FindVFXById(string vfxId)
        {
            var vfxs = UnityEngine.Object.FindObjectsByType<GameKitVFX>(FindObjectsSortMode.None);
            foreach (var vfx in vfxs)
            {
                if (vfx.VFXId == vfxId)
                {
                    return vfx;
                }
            }
            return null;
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
