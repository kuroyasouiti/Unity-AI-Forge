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
    /// GameKit Audio handler: create and manage audio playback.
    /// Supports SFX, music, ambient, voice, and UI audio types with fade controls.
    /// Uses code generation to produce standalone Audio scripts with zero package dependency.
    /// </summary>
    public class GameKitAudioHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "update", "inspect", "delete",
            "setVolume", "setPitch", "setLoop", "setClip",
            "findByAudioId"
        };

        public override string Category => "gamekitAudio";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateAudio(payload),
                "update" => UpdateAudio(payload),
                "inspect" => InspectAudio(payload),
                "delete" => DeleteAudio(payload),
                "setVolume" => SetVolume(payload),
                "setPitch" => SetPitch(payload),
                "setLoop" => SetLoop(payload),
                "setClip" => SetClip(payload),
                "findByAudioId" => FindByAudioId(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit Audio operation: {operation}")
            };
        }

        #region Create

        private object CreateAudio(Dictionary<string, object> payload)
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

            // Check if already has an audio component (by checking for audioId field)
            var existingAudio = CodeGenHelper.FindComponentByField(targetGo, "audioId", null);
            if (existingAudio != null)
            {
                throw new InvalidOperationException(
                    $"GameObject '{targetPath}' already has an Audio component.");
            }

            var audioId = GetString(payload, "audioId") ?? $"Audio_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var audioType = ParseAudioType(GetString(payload, "audioType") ?? "sfx");
            var playOnEnable = payload.TryGetValue("playOnEnable", out var playObj) && Convert.ToBoolean(playObj);
            var loop = payload.TryGetValue("loop", out var loopObj) && Convert.ToBoolean(loopObj);
            var volume = GetFloat(payload, "volume", 1f);
            var pitch = GetFloat(payload, "pitch", 1f);
            var pitchVariation = GetFloat(payload, "pitchVariation", 0f);
            var spatialBlend = GetFloat(payload, "spatialBlend", 0f);
            var fadeInDuration = GetFloat(payload, "fadeInDuration", 0f);
            var fadeOutDuration = GetFloat(payload, "fadeOutDuration", 0f);
            var minDistance = GetFloat(payload, "minDistance", 1f);
            var maxDistance = GetFloat(payload, "maxDistance", 500f);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(audioId, "Audio");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "AUDIO_ID", audioId },
                { "AUDIO_TYPE", audioType },
                { "PLAY_ON_ENABLE", playOnEnable },
                { "LOOP", loop },
                { "VOLUME", volume },
                { "PITCH", pitch },
                { "PITCH_VARIATION", pitchVariation },
                { "SPATIAL_BLEND", spatialBlend },
                { "FADE_IN_DURATION", fadeInDuration },
                { "FADE_OUT_DURATION", fadeOutDuration },
                { "MIN_DISTANCE", minDistance },
                { "MAX_DISTANCE", maxDistance }
            };

            // Build properties to set after component is added
            var propertiesToSet = new Dictionary<string, object>();
            if (payload.TryGetValue("audioClipPath", out var clipPathObj))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPathObj.ToString());
                if (clip != null)
                {
                    propertiesToSet["audioClip"] = clip;
                }
            }

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "Audio", audioId, className, variables, outputDir,
                propertiesToSet.Count > 0 ? propertiesToSet : null);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate Audio script.");
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["audioId"] = audioId;
            result["path"] = BuildGameObjectPath(targetGo);
            result["audioType"] = audioType;

            return result;
        }

        #endregion

        #region Update

        private object UpdateAudio(Dictionary<string, object> payload)
        {
            var component = ResolveAudioComponent(payload);

            Undo.RecordObject(component, "Update Audio");

            var so = new SerializedObject(component);

            if (payload.TryGetValue("audioType", out var typeObj))
            {
                var audioType = ParseAudioType(typeObj.ToString());
                var typeProp = so.FindProperty("audioType");
                var names = typeProp.enumDisplayNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], audioType, StringComparison.OrdinalIgnoreCase))
                    {
                        typeProp.enumValueIndex = i;
                        break;
                    }
                }
            }

            if (payload.TryGetValue("playOnEnable", out var playOnEnableObj))
                so.FindProperty("playOnEnable").boolValue = Convert.ToBoolean(playOnEnableObj);

            if (payload.TryGetValue("loop", out var loopObj))
                so.FindProperty("loop").boolValue = Convert.ToBoolean(loopObj);

            if (payload.TryGetValue("volume", out var volumeObj))
                so.FindProperty("volume").floatValue = Convert.ToSingle(volumeObj);

            if (payload.TryGetValue("pitch", out var pitchObj))
                so.FindProperty("pitch").floatValue = Convert.ToSingle(pitchObj);

            if (payload.TryGetValue("pitchVariation", out var pitchVarObj))
                so.FindProperty("pitchVariation").floatValue = Convert.ToSingle(pitchVarObj);

            if (payload.TryGetValue("spatialBlend", out var spatialObj))
                so.FindProperty("spatialBlend").floatValue = Convert.ToSingle(spatialObj);

            if (payload.TryGetValue("fadeInDuration", out var fadeInObj))
                so.FindProperty("fadeInDuration").floatValue = Convert.ToSingle(fadeInObj);

            if (payload.TryGetValue("fadeOutDuration", out var fadeOutObj))
                so.FindProperty("fadeOutDuration").floatValue = Convert.ToSingle(fadeOutObj);

            if (payload.TryGetValue("minDistance", out var minDistObj))
                so.FindProperty("minDistance").floatValue = Convert.ToSingle(minDistObj);

            if (payload.TryGetValue("maxDistance", out var maxDistObj))
                so.FindProperty("maxDistance").floatValue = Convert.ToSingle(maxDistObj);

            if (payload.TryGetValue("audioClipPath", out var clipPathObj))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPathObj.ToString());
                if (clip != null)
                {
                    so.FindProperty("audioClip").objectReferenceValue = clip;
                }
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var audioId = new SerializedObject(component).FindProperty("audioId").stringValue;

            return CreateSuccessResponse(
                ("audioId", audioId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectAudio(Dictionary<string, object> payload)
        {
            var component = ResolveAudioComponent(payload);
            var so = new SerializedObject(component);

            var audioTypeProp = so.FindProperty("audioType");
            var audioType = audioTypeProp.enumValueIndex < audioTypeProp.enumDisplayNames.Length
                ? audioTypeProp.enumDisplayNames[audioTypeProp.enumValueIndex]
                : "SFX";

            var info = new Dictionary<string, object>
            {
                { "audioId", so.FindProperty("audioId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "audioType", audioType },
                { "playOnEnable", so.FindProperty("playOnEnable").boolValue },
                { "loop", so.FindProperty("loop").boolValue },
                { "volume", so.FindProperty("volume").floatValue },
                { "pitch", so.FindProperty("pitch").floatValue },
                { "pitchVariation", so.FindProperty("pitchVariation").floatValue },
                { "spatialBlend", so.FindProperty("spatialBlend").floatValue },
                { "fadeInDuration", so.FindProperty("fadeInDuration").floatValue },
                { "fadeOutDuration", so.FindProperty("fadeOutDuration").floatValue },
                { "minDistance", so.FindProperty("minDistance").floatValue },
                { "maxDistance", so.FindProperty("maxDistance").floatValue },
                { "hasAudioClip", so.FindProperty("audioClip").objectReferenceValue != null }
            };

            if (so.FindProperty("audioClip").objectReferenceValue != null)
            {
                var clip = so.FindProperty("audioClip").objectReferenceValue as AudioClip;
                if (clip != null)
                {
                    info["clipDuration"] = clip.length;
                    info["clipPath"] = AssetDatabase.GetAssetPath(clip);
                }
            }

            return CreateSuccessResponse(("audio", info));
        }

        #endregion

        #region Delete

        private object DeleteAudio(Dictionary<string, object> payload)
        {
            var component = ResolveAudioComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var audioId = new SerializedObject(component).FindProperty("audioId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component);

            // Also clean up the generated script from tracker
            ScriptGenerator.Delete(audioId);

            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("audioId", audioId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Operations

        private object SetVolume(Dictionary<string, object> payload)
        {
            var component = ResolveAudioComponent(payload);
            var volume = GetFloat(payload, "volume", 1f);

            var so = new SerializedObject(component);
            so.FindProperty("volume").floatValue = Mathf.Clamp01(volume);
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("audioId", so.FindProperty("audioId").stringValue),
                ("volume", volume)
            );
        }

        private object SetPitch(Dictionary<string, object> payload)
        {
            var component = ResolveAudioComponent(payload);
            var pitch = GetFloat(payload, "pitch", 1f);

            var so = new SerializedObject(component);
            so.FindProperty("pitch").floatValue = pitch;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("audioId", so.FindProperty("audioId").stringValue),
                ("pitch", pitch)
            );
        }

        private object SetLoop(Dictionary<string, object> payload)
        {
            var component = ResolveAudioComponent(payload);
            var shouldLoop = GetBool(payload, "loop", true);

            var so = new SerializedObject(component);
            so.FindProperty("loop").boolValue = shouldLoop;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("audioId", so.FindProperty("audioId").stringValue),
                ("loop", shouldLoop)
            );
        }

        private object SetClip(Dictionary<string, object> payload)
        {
            var component = ResolveAudioComponent(payload);
            var clipPath = GetString(payload, "audioClipPath");

            if (string.IsNullOrEmpty(clipPath))
            {
                throw new InvalidOperationException("audioClipPath is required for setClip.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"AudioClip not found at path: {clipPath}");
            }

            var so = new SerializedObject(component);
            so.FindProperty("audioClip").objectReferenceValue = clip;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("audioId", so.FindProperty("audioId").stringValue),
                ("clipPath", clipPath),
                ("clipDuration", clip.length)
            );
        }

        #endregion

        #region Find

        private object FindByAudioId(Dictionary<string, object> payload)
        {
            var audioId = GetString(payload, "audioId");
            if (string.IsNullOrEmpty(audioId))
            {
                throw new InvalidOperationException("audioId is required for findByAudioId.");
            }

            var component = CodeGenHelper.FindComponentInSceneByField("audioId", audioId);
            if (component == null)
            {
                return CreateSuccessResponse(("found", false), ("audioId", audioId));
            }

            var so = new SerializedObject(component);
            var audioTypeProp = so.FindProperty("audioType");
            var audioType = audioTypeProp.enumValueIndex < audioTypeProp.enumDisplayNames.Length
                ? audioTypeProp.enumDisplayNames[audioTypeProp.enumValueIndex]
                : "SFX";

            return CreateSuccessResponse(
                ("found", true),
                ("audioId", audioId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("audioType", audioType)
            );
        }

        #endregion

        #region Helpers

        private Component ResolveAudioComponent(Dictionary<string, object> payload)
        {
            // Try by audioId first
            var audioId = GetString(payload, "audioId");
            if (!string.IsNullOrEmpty(audioId))
            {
                var audioById = CodeGenHelper.FindComponentInSceneByField("audioId", audioId);
                if (audioById != null)
                {
                    return audioById;
                }
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var audioByPath = CodeGenHelper.FindComponentByField(targetGo, "audioId", null);
                    if (audioByPath != null)
                    {
                        return audioByPath;
                    }

                    throw new InvalidOperationException($"No Audio component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either audioId or targetPath is required.");
        }

        private string ParseAudioType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "sfx" => "SFX",
                "music" => "Music",
                "ambient" => "Ambient",
                "voice" => "Voice",
                "ui" => "UI",
                _ => "SFX"
            };
        }

        #endregion
    }
}
