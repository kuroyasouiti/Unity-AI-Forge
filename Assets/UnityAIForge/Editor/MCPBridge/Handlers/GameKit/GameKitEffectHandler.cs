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
    /// GameKit Effect handler: create and manage composite effect systems.
    /// Provides particle, sound, camera shake, and screen flash effects without custom scripts.
    /// </summary>
    public class GameKitEffectHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "update", "inspect", "delete",
            "addComponent", "removeComponent", "clearComponents",
            "play", "playAtPosition", "playAtTransform",
            "shakeCamera", "flashScreen", "setTimeScale",
            "createManager", "registerEffect", "unregisterEffect",
            "findByEffectId", "listEffects"
        };

        public override string Category => "gamekitEffect";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateEffect(payload),
                "update" => UpdateEffect(payload),
                "inspect" => InspectEffect(payload),
                "delete" => DeleteEffect(payload),
                "addComponent" => AddEffectComponent(payload),
                "removeComponent" => RemoveEffectComponent(payload),
                "clearComponents" => ClearEffectComponents(payload),
                "play" => PlayEffect(payload),
                "playAtPosition" => PlayEffectAtPosition(payload),
                "playAtTransform" => PlayEffectAtTransform(payload),
                "shakeCamera" => ShakeCamera(payload),
                "flashScreen" => FlashScreen(payload),
                "setTimeScale" => SetTimeScale(payload),
                "createManager" => CreateEffectManager(payload),
                "registerEffect" => RegisterEffect(payload),
                "unregisterEffect" => UnregisterEffect(payload),
                "findByEffectId" => FindByEffectId(payload),
                "listEffects" => ListEffects(payload),
                _ => throw new InvalidOperationException($"Unsupported operation: {operation}")
            };
        }

        #region Create Effect Asset

        private object CreateEffect(Dictionary<string, object> payload)
        {
            var effectId = GetString(payload, "effectId");
            if (string.IsNullOrEmpty(effectId))
            {
                effectId = $"Effect_{Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            var assetPath = GetString(payload, "assetPath");
            if (string.IsNullOrEmpty(assetPath))
            {
                assetPath = $"Assets/GameKit/Effects/{effectId}.asset";
            }

            // Ensure directory exists
            var directory = System.IO.Path.GetDirectoryName(assetPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // Create ScriptableObject
            var effectAsset = ScriptableObject.CreateInstance<GameKitEffectAsset>();
            effectAsset.Initialize(effectId);

            // Add components if provided
            if (payload.TryGetValue("components", out var componentsObj) && componentsObj is List<object> componentsList)
            {
                foreach (var compObj in componentsList)
                {
                    if (compObj is Dictionary<string, object> compDict)
                    {
                        var component = CreateEffectComponentFromDict(compDict);
                        effectAsset.AddComponent(component);
                    }
                }
            }

            AssetDatabase.CreateAsset(effectAsset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("assetPath", assetPath),
                ("componentCount", effectAsset.Components.Count)
            );
        }

        #endregion

        #region Update Effect

        private object UpdateEffect(Dictionary<string, object> payload)
        {
            var effectAsset = ResolveEffectAsset(payload);

            // Re-initialize effectId if provided
            var newEffectId = GetString(payload, "newEffectId");
            if (!string.IsNullOrEmpty(newEffectId))
            {
                var serializedAsset = new SerializedObject(effectAsset);
                serializedAsset.FindProperty("effectId").stringValue = newEffectId;
                serializedAsset.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(effectAsset);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("effectId", effectAsset.EffectId),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect Effect

        private object InspectEffect(Dictionary<string, object> payload)
        {
            var effectAsset = ResolveEffectAsset(payload);

            var componentsList = new List<Dictionary<string, object>>();
            foreach (var comp in effectAsset.Components)
            {
                componentsList.Add(SerializeEffectComponent(comp));
            }

            return CreateSuccessResponse(
                ("effectId", effectAsset.EffectId),
                ("assetPath", AssetDatabase.GetAssetPath(effectAsset)),
                ("componentCount", effectAsset.Components.Count),
                ("components", componentsList)
            );
        }

        #endregion

        #region Delete Effect

        private object DeleteEffect(Dictionary<string, object> payload)
        {
            var effectAsset = ResolveEffectAsset(payload);
            var assetPath = AssetDatabase.GetAssetPath(effectAsset);
            var effectId = effectAsset.EffectId;

            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.Refresh();

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("assetPath", assetPath),
                ("deleted", true)
            );
        }

        #endregion

        #region Effect Components

        private object AddEffectComponent(Dictionary<string, object> payload)
        {
            var effectAsset = ResolveEffectAsset(payload);

            if (!payload.TryGetValue("component", out var compObj) || !(compObj is Dictionary<string, object> compDict))
            {
                throw new InvalidOperationException("'component' parameter is required for addComponent.");
            }

            var component = CreateEffectComponentFromDict(compDict);
            effectAsset.AddComponent(component);

            EditorUtility.SetDirty(effectAsset);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("effectId", effectAsset.EffectId),
                ("componentType", component.type.ToString()),
                ("totalComponents", effectAsset.Components.Count)
            );
        }

        private object RemoveEffectComponent(Dictionary<string, object> payload)
        {
            var effectAsset = ResolveEffectAsset(payload);
            var index = GetInt(payload, "componentIndex", -1);

            if (index < 0 || index >= effectAsset.Components.Count)
            {
                throw new InvalidOperationException($"Invalid component index: {index}. Valid range: 0-{effectAsset.Components.Count - 1}");
            }

            var serializedAsset = new SerializedObject(effectAsset);
            var componentsProperty = serializedAsset.FindProperty("components");
            componentsProperty.DeleteArrayElementAtIndex(index);
            serializedAsset.ApplyModifiedProperties();

            EditorUtility.SetDirty(effectAsset);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("effectId", effectAsset.EffectId),
                ("removedIndex", index),
                ("remainingComponents", effectAsset.Components.Count)
            );
        }

        private object ClearEffectComponents(Dictionary<string, object> payload)
        {
            var effectAsset = ResolveEffectAsset(payload);
            var previousCount = effectAsset.Components.Count;

            effectAsset.ClearComponents();

            EditorUtility.SetDirty(effectAsset);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("effectId", effectAsset.EffectId),
                ("clearedCount", previousCount)
            );
        }

        #endregion

        #region Play Effects (Runtime)

        private object PlayEffect(Dictionary<string, object> payload)
        {
            if (!Application.isPlaying)
            {
                return CreateSuccessResponse(
                    ("note", "Effect playback requires play mode.")
                );
            }

            var effectId = GetString(payload, "effectId");
            if (string.IsNullOrEmpty(effectId))
            {
                throw new InvalidOperationException("'effectId' is required for play.");
            }

            var position = GetVector3(payload, "position", Vector3.zero);

            GameKitEffectManager.Instance.PlayEffect(effectId, position);

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("position", new Dictionary<string, object>
                {
                    { "x", position.x },
                    { "y", position.y },
                    { "z", position.z }
                }),
                ("played", true)
            );
        }

        private object PlayEffectAtPosition(Dictionary<string, object> payload)
        {
            return PlayEffect(payload);
        }

        private object PlayEffectAtTransform(Dictionary<string, object> payload)
        {
            if (!Application.isPlaying)
            {
                return CreateSuccessResponse(
                    ("note", "Effect playback requires play mode.")
                );
            }

            var effectId = GetString(payload, "effectId");
            var targetPath = GetString(payload, "targetPath");

            if (string.IsNullOrEmpty(effectId))
            {
                throw new InvalidOperationException("'effectId' is required.");
            }

            if (string.IsNullOrEmpty(targetPath))
            {
                throw new InvalidOperationException("'targetPath' is required for playAtTransform.");
            }

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
            {
                throw new InvalidOperationException($"GameObject not found: {targetPath}");
            }

            GameKitEffectManager.Instance.PlayEffectAtTransform(effectId, targetGo.transform);

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("targetPath", targetPath),
                ("played", true)
            );
        }

        #endregion

        #region Direct Effects

        private object ShakeCamera(Dictionary<string, object> payload)
        {
            if (!Application.isPlaying)
            {
                return CreateSuccessResponse(
                    ("note", "Camera shake requires play mode.")
                );
            }

            var intensity = GetFloat(payload, "intensity", 0.3f);
            var duration = GetFloat(payload, "duration", 0.2f);
            var frequency = GetFloat(payload, "frequency", 25f);

            GameKitEffectManager.Instance.ShakeCamera(intensity, duration, frequency);

            return CreateSuccessResponse(
                ("intensity", intensity),
                ("duration", duration),
                ("frequency", frequency),
                ("triggered", true)
            );
        }

        private object FlashScreen(Dictionary<string, object> payload)
        {
            if (!Application.isPlaying)
            {
                return CreateSuccessResponse(
                    ("note", "Screen flash requires play mode.")
                );
            }

            var color = GetColor(payload, "color", new Color(1f, 1f, 1f, 0.5f));
            var duration = GetFloat(payload, "duration", 0.1f);
            var fadeTime = GetFloat(payload, "fadeTime", 0.05f);

            GameKitEffectManager.Instance.FlashScreen(color, duration, fadeTime);

            return CreateSuccessResponse(
                ("color", new Dictionary<string, object>
                {
                    { "r", color.r },
                    { "g", color.g },
                    { "b", color.b },
                    { "a", color.a }
                }),
                ("duration", duration),
                ("triggered", true)
            );
        }

        private object SetTimeScale(Dictionary<string, object> payload)
        {
            if (!Application.isPlaying)
            {
                return CreateSuccessResponse(
                    ("note", "Time scale change requires play mode.")
                );
            }

            var targetScale = GetFloat(payload, "targetScale", 0.1f);
            var duration = GetFloat(payload, "duration", 0.1f);
            var transitionTime = GetFloat(payload, "transitionTime", 0.05f);

            GameKitEffectManager.Instance.SetTimeScale(targetScale, duration, transitionTime);

            return CreateSuccessResponse(
                ("targetScale", targetScale),
                ("duration", duration),
                ("triggered", true)
            );
        }

        #endregion

        #region Effect Manager

        private object CreateEffectManager(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            GameObject targetGo;

            if (string.IsNullOrEmpty(targetPath))
            {
                // Create new GameObject
                targetGo = new GameObject("GameKitEffectManager");
                Undo.RegisterCreatedObjectUndo(targetGo, "Create Effect Manager");
            }
            else
            {
                targetGo = ResolveGameObject(targetPath);
                if (targetGo == null)
                {
                    throw new InvalidOperationException($"GameObject not found: {targetPath}");
                }
            }

            // Check if already has component
            var existing = targetGo.GetComponent<GameKitEffectManager>();
            if (existing != null)
            {
                return CreateSuccessResponse(
                    ("path", BuildGameObjectPath(targetGo)),
                    ("note", "GameKitEffectManager already exists on this GameObject.")
                );
            }

            var manager = Undo.AddComponent<GameKitEffectManager>(targetGo);
            var persistent = GetBool(payload, "persistent", true);

            var serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("persistent").boolValue = persistent;
            serializedManager.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("path", BuildGameObjectPath(targetGo)),
                ("persistent", persistent),
                ("created", true)
            );
        }

        private object RegisterEffect(Dictionary<string, object> payload)
        {
            var effectAsset = ResolveEffectAsset(payload);
            var managerPath = GetString(payload, "managerPath");

            GameKitEffectManager manager;
            if (!string.IsNullOrEmpty(managerPath))
            {
                var managerGo = ResolveGameObject(managerPath);
                manager = managerGo?.GetComponent<GameKitEffectManager>();
            }
            else
            {
                manager = UnityEngine.Object.FindFirstObjectByType<GameKitEffectManager>();
            }

            if (manager == null)
            {
                throw new InvalidOperationException("No GameKitEffectManager found. Create one first with 'createManager' operation.");
            }

            // Add to registered effects
            var serializedManager = new SerializedObject(manager);
            var effectsProperty = serializedManager.FindProperty("registeredEffects");
            effectsProperty.InsertArrayElementAtIndex(effectsProperty.arraySize);
            effectsProperty.GetArrayElementAtIndex(effectsProperty.arraySize - 1).objectReferenceValue = effectAsset;
            serializedManager.ApplyModifiedProperties();

            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);

            return CreateSuccessResponse(
                ("effectId", effectAsset.EffectId),
                ("managerPath", BuildGameObjectPath(manager.gameObject)),
                ("registered", true)
            );
        }

        private object UnregisterEffect(Dictionary<string, object> payload)
        {
            var effectId = GetString(payload, "effectId");
            if (string.IsNullOrEmpty(effectId))
            {
                throw new InvalidOperationException("'effectId' is required for unregisterEffect.");
            }

            var managerPath = GetString(payload, "managerPath");

            GameKitEffectManager manager;
            if (!string.IsNullOrEmpty(managerPath))
            {
                var managerGo = ResolveGameObject(managerPath);
                manager = managerGo?.GetComponent<GameKitEffectManager>();
            }
            else
            {
                manager = UnityEngine.Object.FindFirstObjectByType<GameKitEffectManager>();
            }

            if (manager == null)
            {
                return CreateSuccessResponse(
                    ("effectId", effectId),
                    ("unregistered", false),
                    ("note", "No GameKitEffectManager found.")
                );
            }

            // Find and remove from registered effects
            var serializedManager = new SerializedObject(manager);
            var effectsProperty = serializedManager.FindProperty("registeredEffects");
            bool removed = false;

            for (int i = effectsProperty.arraySize - 1; i >= 0; i--)
            {
                var element = effectsProperty.GetArrayElementAtIndex(i);
                var asset = element.objectReferenceValue as GameKitEffectAsset;
                if (asset != null && asset.EffectId == effectId)
                {
                    effectsProperty.DeleteArrayElementAtIndex(i);
                    removed = true;
                    break;
                }
            }

            serializedManager.ApplyModifiedProperties();

            if (removed)
            {
                EditorUtility.SetDirty(manager);
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("unregistered", removed)
            );
        }

        #endregion

        #region Find & List

        private object FindByEffectId(Dictionary<string, object> payload)
        {
            var effectId = GetString(payload, "effectId");
            if (string.IsNullOrEmpty(effectId))
            {
                throw new InvalidOperationException("'effectId' is required for findByEffectId.");
            }

            // Search in all GameKitEffectAssets
            var guids = AssetDatabase.FindAssets("t:GameKitEffectAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameKitEffectAsset>(path);
                if (asset != null && asset.EffectId == effectId)
                {
                    return CreateSuccessResponse(
                        ("found", true),
                        ("effectId", asset.EffectId),
                        ("assetPath", path),
                        ("componentCount", asset.Components.Count)
                    );
                }
            }

            return CreateSuccessResponse(
                ("found", false),
                ("effectId", effectId)
            );
        }

        private object ListEffects(Dictionary<string, object> payload)
        {
            var effects = new List<Dictionary<string, object>>();
            var guids = AssetDatabase.FindAssets("t:GameKitEffectAsset");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameKitEffectAsset>(path);
                if (asset != null)
                {
                    effects.Add(new Dictionary<string, object>
                    {
                        { "effectId", asset.EffectId },
                        { "assetPath", path },
                        { "componentCount", asset.Components.Count }
                    });
                }
            }

            return CreateSuccessResponse(
                ("count", effects.Count),
                ("effects", effects)
            );
        }

        #endregion

        #region Helpers

        private GameKitEffectAsset ResolveEffectAsset(Dictionary<string, object> payload)
        {
            // Try by effectId first
            var effectId = GetString(payload, "effectId");
            if (!string.IsNullOrEmpty(effectId))
            {
                var guids = AssetDatabase.FindAssets("t:GameKitEffectAsset");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<GameKitEffectAsset>(path);
                    if (asset != null && asset.EffectId == effectId)
                    {
                        return asset;
                    }
                }
            }

            // Try by assetPath
            var assetPath = GetString(payload, "assetPath");
            if (!string.IsNullOrEmpty(assetPath))
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameKitEffectAsset>(assetPath);
                if (asset != null)
                {
                    return asset;
                }
                throw new InvalidOperationException($"Effect asset not found at path: {assetPath}");
            }

            throw new InvalidOperationException("Either 'effectId' or 'assetPath' is required.");
        }

        private GameKitEffectAsset.EffectComponent CreateEffectComponentFromDict(Dictionary<string, object> dict)
        {
            var component = new GameKitEffectAsset.EffectComponent();

            if (dict.TryGetValue("type", out var typeObj))
            {
                component.type = ParseEffectType(typeObj?.ToString());
            }

            // Particle settings
            if (dict.TryGetValue("prefabPath", out var prefabPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath?.ToString());
                component.particlePrefab = prefab;
            }

            if (dict.TryGetValue("duration", out var durationObj))
            {
                component.particleDuration = Convert.ToSingle(durationObj);
            }

            if (dict.TryGetValue("attachToTarget", out var attachObj))
            {
                component.attachToTarget = Convert.ToBoolean(attachObj);
            }

            if (dict.TryGetValue("positionOffset", out var offsetObj) && offsetObj is Dictionary<string, object> offsetDict)
            {
                component.positionOffset = GetVector3FromDict(offsetDict, Vector3.zero);
            }

            if (dict.TryGetValue("particleScale", out var scaleObj))
            {
                component.particleScale = Convert.ToSingle(scaleObj);
            }

            // Sound settings
            if (dict.TryGetValue("clipPath", out var clipPath))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath?.ToString());
                component.audioClip = clip;
            }

            if (dict.TryGetValue("volume", out var volObj))
            {
                component.volume = Mathf.Clamp01(Convert.ToSingle(volObj));
            }

            if (dict.TryGetValue("pitchVariation", out var pitchObj))
            {
                component.pitchVariation = Mathf.Clamp(Convert.ToSingle(pitchObj), 0f, 0.5f);
            }

            if (dict.TryGetValue("spatialBlend", out var spatialObj))
            {
                component.spatialBlend = Mathf.Clamp01(Convert.ToSingle(spatialObj));
            }

            // Camera shake settings
            if (dict.TryGetValue("intensity", out var intensityObj))
            {
                component.shakeIntensity = Convert.ToSingle(intensityObj);
            }

            if (dict.TryGetValue("shakeDuration", out var shakeDurObj))
            {
                component.shakeDuration = Convert.ToSingle(shakeDurObj);
            }

            if (dict.TryGetValue("frequency", out var freqObj))
            {
                component.shakeFrequency = Convert.ToSingle(freqObj);
            }

            // Screen flash settings
            if (dict.TryGetValue("color", out var colorObj) && colorObj is Dictionary<string, object> colorDict)
            {
                component.flashColor = GetColorFromDict(colorDict, Color.white);
            }

            if (dict.TryGetValue("flashDuration", out var flashDurObj))
            {
                component.flashDuration = Convert.ToSingle(flashDurObj);
            }

            if (dict.TryGetValue("fadeTime", out var fadeObj))
            {
                component.flashFadeTime = Convert.ToSingle(fadeObj);
            }

            // Time scale settings
            if (dict.TryGetValue("targetTimeScale", out var timeScaleObj))
            {
                component.targetTimeScale = Convert.ToSingle(timeScaleObj);
            }

            if (dict.TryGetValue("timeScaleDuration", out var tsDurObj))
            {
                component.timeScaleDuration = Convert.ToSingle(tsDurObj);
            }

            if (dict.TryGetValue("timeScaleTransition", out var tsTransObj))
            {
                component.timeScaleTransition = Convert.ToSingle(tsTransObj);
            }

            return component;
        }

        private Dictionary<string, object> SerializeEffectComponent(GameKitEffectAsset.EffectComponent comp)
        {
            var dict = new Dictionary<string, object>
            {
                { "type", comp.type.ToString() }
            };

            switch (comp.type)
            {
                case GameKitEffectAsset.EffectType.Particle:
                    dict["prefabPath"] = comp.particlePrefab != null ? AssetDatabase.GetAssetPath(comp.particlePrefab) : null;
                    dict["duration"] = comp.particleDuration;
                    dict["attachToTarget"] = comp.attachToTarget;
                    dict["positionOffset"] = new Dictionary<string, object>
                    {
                        { "x", comp.positionOffset.x },
                        { "y", comp.positionOffset.y },
                        { "z", comp.positionOffset.z }
                    };
                    dict["particleScale"] = comp.particleScale;
                    break;

                case GameKitEffectAsset.EffectType.Sound:
                    dict["clipPath"] = comp.audioClip != null ? AssetDatabase.GetAssetPath(comp.audioClip) : null;
                    dict["volume"] = comp.volume;
                    dict["pitchVariation"] = comp.pitchVariation;
                    dict["spatialBlend"] = comp.spatialBlend;
                    break;

                case GameKitEffectAsset.EffectType.CameraShake:
                    dict["intensity"] = comp.shakeIntensity;
                    dict["duration"] = comp.shakeDuration;
                    dict["frequency"] = comp.shakeFrequency;
                    break;

                case GameKitEffectAsset.EffectType.ScreenFlash:
                    dict["color"] = new Dictionary<string, object>
                    {
                        { "r", comp.flashColor.r },
                        { "g", comp.flashColor.g },
                        { "b", comp.flashColor.b },
                        { "a", comp.flashColor.a }
                    };
                    dict["duration"] = comp.flashDuration;
                    dict["fadeTime"] = comp.flashFadeTime;
                    break;

                case GameKitEffectAsset.EffectType.TimeScale:
                    dict["targetTimeScale"] = comp.targetTimeScale;
                    dict["duration"] = comp.timeScaleDuration;
                    dict["transitionTime"] = comp.timeScaleTransition;
                    break;
            }

            return dict;
        }

        private GameKitEffectAsset.EffectType ParseEffectType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "particle" => GameKitEffectAsset.EffectType.Particle,
                "sound" => GameKitEffectAsset.EffectType.Sound,
                "camerashake" or "shake" => GameKitEffectAsset.EffectType.CameraShake,
                "screenflash" or "flash" => GameKitEffectAsset.EffectType.ScreenFlash,
                "timescale" or "slowmo" or "hitpause" => GameKitEffectAsset.EffectType.TimeScale,
                _ => GameKitEffectAsset.EffectType.Particle
            };
        }

        #endregion
    }
}
