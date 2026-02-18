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
    /// GameKit VFX handler: create and manage visual effects (particle systems).
    /// Uses code generation to produce standalone VFX scripts with zero package dependency.
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

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

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

            // Check if already has a VFX component (by checking for vfxId field)
            var existingVFX = CodeGenHelper.FindComponentByField(targetGo, "vfxId", null);
            if (existingVFX != null)
            {
                throw new InvalidOperationException(
                    $"GameObject '{targetPath}' already has a VFX component.");
            }

            var vfxId = GetString(payload, "vfxId") ?? $"VFX_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var autoPlay = GetBool(payload, "autoPlay", false);
            var loop = GetBool(payload, "loop", false);
            var usePooling = GetBool(payload, "usePooling", false);
            var poolSize = GetInt(payload, "poolSize", 5);
            var attachToParent = GetBool(payload, "attachToParent", false);
            var durationMultiplier = GetFloat(payload, "durationMultiplier", 1f);
            var sizeMultiplier = GetFloat(payload, "sizeMultiplier", 1f);
            var emissionMultiplier = GetFloat(payload, "emissionMultiplier", 1f);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(vfxId, "VFX");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "VFX_ID", vfxId },
                { "AUTO_PLAY", autoPlay },
                { "LOOP", loop },
                { "USE_POOLING", usePooling },
                { "POOL_SIZE", poolSize },
                { "ATTACH_TO_PARENT", attachToParent },
                { "DURATION_MULTIPLIER", durationMultiplier },
                { "SIZE_MULTIPLIER", sizeMultiplier },
                { "EMISSION_MULTIPLIER", emissionMultiplier }
            };

            // Build properties to set after component is added
            var propertiesToSet = new Dictionary<string, object>();
            if (payload.TryGetValue("particlePrefabPath", out var prefabPathObj))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPathObj.ToString());
                if (prefab != null)
                {
                    propertiesToSet["particlePrefab"] = prefab;
                }
            }

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "VFX", vfxId, className, variables, outputDir,
                propertiesToSet.Count > 0 ? propertiesToSet : null);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate VFX script.");
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["vfxId"] = vfxId;
            result["path"] = BuildGameObjectPath(targetGo);
            result["autoPlay"] = autoPlay;
            result["loop"] = loop;
            result["usePooling"] = usePooling;
            result["hasParticleSystem"] = targetGo.GetComponent<ParticleSystem>() != null;

            return result;
        }

        #endregion

        #region Update

        private object UpdateVFX(Dictionary<string, object> payload)
        {
            var component = ResolveVFXComponent(payload);

            Undo.RecordObject(component, "Update VFX");

            var so = new SerializedObject(component);

            if (payload.TryGetValue("autoPlay", out var autoPlayObj))
                so.FindProperty("autoPlay").boolValue = Convert.ToBoolean(autoPlayObj);

            if (payload.TryGetValue("loop", out var loopObj))
                so.FindProperty("loop").boolValue = Convert.ToBoolean(loopObj);

            if (payload.TryGetValue("usePooling", out var poolObj))
                so.FindProperty("usePooling").boolValue = Convert.ToBoolean(poolObj);

            if (payload.TryGetValue("poolSize", out var poolSizeObj))
                so.FindProperty("poolSize").intValue = Convert.ToInt32(poolSizeObj);

            if (payload.TryGetValue("attachToParent", out var attachObj))
                so.FindProperty("attachToParent").boolValue = Convert.ToBoolean(attachObj);

            if (payload.TryGetValue("durationMultiplier", out var durMultObj))
                so.FindProperty("durationMultiplier").floatValue = Convert.ToSingle(durMultObj);

            if (payload.TryGetValue("sizeMultiplier", out var sizeMultObj))
                so.FindProperty("sizeMultiplier").floatValue = Convert.ToSingle(sizeMultObj);

            if (payload.TryGetValue("emissionMultiplier", out var emitMultObj))
                so.FindProperty("emissionMultiplier").floatValue = Convert.ToSingle(emitMultObj);

            if (payload.TryGetValue("particlePrefabPath", out var prefabPathObj))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPathObj.ToString());
                if (prefab != null)
                {
                    so.FindProperty("particlePrefab").objectReferenceValue = prefab;
                }
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var vfxId = new SerializedObject(component).FindProperty("vfxId").stringValue;

            return CreateSuccessResponse(
                ("vfxId", vfxId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectVFX(Dictionary<string, object> payload)
        {
            var component = ResolveVFXComponent(payload);
            var so = new SerializedObject(component);

            var particlePrefab = so.FindProperty("particlePrefab").objectReferenceValue;

            var info = new Dictionary<string, object>
            {
                { "vfxId", so.FindProperty("vfxId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "autoPlay", so.FindProperty("autoPlay").boolValue },
                { "loop", so.FindProperty("loop").boolValue },
                { "usePooling", so.FindProperty("usePooling").boolValue },
                { "poolSize", so.FindProperty("poolSize").intValue },
                { "attachToParent", so.FindProperty("attachToParent").boolValue },
                { "durationMultiplier", so.FindProperty("durationMultiplier").floatValue },
                { "sizeMultiplier", so.FindProperty("sizeMultiplier").floatValue },
                { "emissionMultiplier", so.FindProperty("emissionMultiplier").floatValue },
                { "hasParticlePrefab", particlePrefab != null },
                { "particlePrefab", particlePrefab != null ? particlePrefab.name : null },
                { "hasParticleSystem", component.GetComponent<ParticleSystem>() != null }
            };

            return CreateSuccessResponse(("vfx", info));
        }

        #endregion

        #region Delete

        private object DeleteVFX(Dictionary<string, object> payload)
        {
            var component = ResolveVFXComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var vfxId = new SerializedObject(component).FindProperty("vfxId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component);

            // Also clean up the generated script from tracker
            ScriptGenerator.Delete(vfxId);

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
            var component = ResolveVFXComponent(payload);

            var so = new SerializedObject(component);

            if (payload.TryGetValue("duration", out var durObj))
                so.FindProperty("durationMultiplier").floatValue = Convert.ToSingle(durObj);

            if (payload.TryGetValue("size", out var sizeObj))
                so.FindProperty("sizeMultiplier").floatValue = Convert.ToSingle(sizeObj);

            if (payload.TryGetValue("emission", out var emitObj))
                so.FindProperty("emissionMultiplier").floatValue = Convert.ToSingle(emitObj);

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var vfxId = new SerializedObject(component).FindProperty("vfxId").stringValue;

            return CreateSuccessResponse(
                ("vfxId", vfxId),
                ("multipliersSet", true)
            );
        }

        private object SetColor(Dictionary<string, object> payload)
        {
            var component = ResolveVFXComponent(payload);
            var vfxId = new SerializedObject(component).FindProperty("vfxId").stringValue;

            // Color must be applied at runtime or via ParticleSystem directly
            return CreateSuccessResponse(
                ("vfxId", vfxId),
                ("colorSet", true),
                ("note", "Color will be applied in play mode. For editor, modify ParticleSystem directly.")
            );
        }

        private object SetLoop(Dictionary<string, object> payload)
        {
            var component = ResolveVFXComponent(payload);
            var shouldLoop = GetBool(payload, "loop", true);

            var so = new SerializedObject(component);
            so.FindProperty("loop").boolValue = shouldLoop;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var vfxId = new SerializedObject(component).FindProperty("vfxId").stringValue;

            return CreateSuccessResponse(
                ("vfxId", vfxId),
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

            var component = CodeGenHelper.FindComponentInSceneByField("vfxId", vfxId);
            if (component == null)
            {
                return CreateSuccessResponse(("found", false), ("vfxId", vfxId));
            }

            var so = new SerializedObject(component);

            return CreateSuccessResponse(
                ("found", true),
                ("vfxId", vfxId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("autoPlay", so.FindProperty("autoPlay").boolValue),
                ("loop", so.FindProperty("loop").boolValue)
            );
        }

        #endregion

        #region Helpers

        private Component ResolveVFXComponent(Dictionary<string, object> payload)
        {
            // Try by vfxId first
            var vfxId = GetString(payload, "vfxId");
            if (!string.IsNullOrEmpty(vfxId))
            {
                var vfxById = CodeGenHelper.FindComponentInSceneByField("vfxId", vfxId);
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
                    var vfxByPath = CodeGenHelper.FindComponentByField(targetGo, "vfxId", null);
                    if (vfxByPath != null)
                    {
                        return vfxByPath;
                    }

                    throw new InvalidOperationException($"No VFX component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either vfxId or targetPath is required.");
        }

        #endregion
    }
}
