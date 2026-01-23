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
    /// GameKit Audio handler: create and manage audio playback.
    /// Supports SFX, music, ambient, voice, and UI audio types with fade controls.
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

        protected override bool RequiresCompilationWait(string operation) => false;

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

            // Check if already has audio component
            var existingAudio = targetGo.GetComponent<GameKitAudio>();
            if (existingAudio != null)
            {
                throw new InvalidOperationException($"GameObject '{targetPath}' already has a GameKitAudio component.");
            }

            var audioId = GetString(payload, "audioId") ?? $"Audio_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var audioType = ParseAudioType(GetString(payload, "audioType") ?? "sfx");

            // Add component
            var audio = Undo.AddComponent<GameKitAudio>(targetGo);

            // Set properties via SerializedObject
            var serializedAudio = new SerializedObject(audio);
            serializedAudio.FindProperty("audioId").stringValue = audioId;
            serializedAudio.FindProperty("audioType").enumValueIndex = (int)audioType;

            if (payload.TryGetValue("playOnEnable", out var playOnEnableObj))
            {
                serializedAudio.FindProperty("playOnEnable").boolValue = Convert.ToBoolean(playOnEnableObj);
            }

            if (payload.TryGetValue("loop", out var loopObj))
            {
                serializedAudio.FindProperty("loop").boolValue = Convert.ToBoolean(loopObj);
            }

            if (payload.TryGetValue("volume", out var volumeObj))
            {
                serializedAudio.FindProperty("volume").floatValue = Convert.ToSingle(volumeObj);
            }

            if (payload.TryGetValue("pitch", out var pitchObj))
            {
                serializedAudio.FindProperty("pitch").floatValue = Convert.ToSingle(pitchObj);
            }

            if (payload.TryGetValue("pitchVariation", out var pitchVarObj))
            {
                serializedAudio.FindProperty("pitchVariation").floatValue = Convert.ToSingle(pitchVarObj);
            }

            if (payload.TryGetValue("spatialBlend", out var spatialObj))
            {
                serializedAudio.FindProperty("spatialBlend").floatValue = Convert.ToSingle(spatialObj);
            }

            if (payload.TryGetValue("fadeInDuration", out var fadeInObj))
            {
                serializedAudio.FindProperty("fadeInDuration").floatValue = Convert.ToSingle(fadeInObj);
            }

            if (payload.TryGetValue("fadeOutDuration", out var fadeOutObj))
            {
                serializedAudio.FindProperty("fadeOutDuration").floatValue = Convert.ToSingle(fadeOutObj);
            }

            if (payload.TryGetValue("minDistance", out var minDistObj))
            {
                serializedAudio.FindProperty("minDistance").floatValue = Convert.ToSingle(minDistObj);
            }

            if (payload.TryGetValue("maxDistance", out var maxDistObj))
            {
                serializedAudio.FindProperty("maxDistance").floatValue = Convert.ToSingle(maxDistObj);
            }

            // Link audio clip if provided
            if (payload.TryGetValue("audioClipPath", out var clipPathObj))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPathObj.ToString());
                if (clip != null)
                {
                    serializedAudio.FindProperty("audioClip").objectReferenceValue = clip;
                }
            }

            serializedAudio.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("audioId", audioId),
                ("path", BuildGameObjectPath(targetGo)),
                ("audioType", audioType.ToString())
            );
        }

        #endregion

        #region Update

        private object UpdateAudio(Dictionary<string, object> payload)
        {
            var audio = ResolveAudioComponent(payload);

            Undo.RecordObject(audio, "Update GameKit Audio");

            var serializedAudio = new SerializedObject(audio);

            if (payload.TryGetValue("audioType", out var typeObj))
            {
                var audioType = ParseAudioType(typeObj.ToString());
                serializedAudio.FindProperty("audioType").enumValueIndex = (int)audioType;
            }

            if (payload.TryGetValue("playOnEnable", out var playOnEnableObj))
            {
                serializedAudio.FindProperty("playOnEnable").boolValue = Convert.ToBoolean(playOnEnableObj);
            }

            if (payload.TryGetValue("loop", out var loopObj))
            {
                serializedAudio.FindProperty("loop").boolValue = Convert.ToBoolean(loopObj);
            }

            if (payload.TryGetValue("volume", out var volumeObj))
            {
                serializedAudio.FindProperty("volume").floatValue = Convert.ToSingle(volumeObj);
            }

            if (payload.TryGetValue("pitch", out var pitchObj))
            {
                serializedAudio.FindProperty("pitch").floatValue = Convert.ToSingle(pitchObj);
            }

            if (payload.TryGetValue("pitchVariation", out var pitchVarObj))
            {
                serializedAudio.FindProperty("pitchVariation").floatValue = Convert.ToSingle(pitchVarObj);
            }

            if (payload.TryGetValue("spatialBlend", out var spatialObj))
            {
                serializedAudio.FindProperty("spatialBlend").floatValue = Convert.ToSingle(spatialObj);
            }

            if (payload.TryGetValue("fadeInDuration", out var fadeInObj))
            {
                serializedAudio.FindProperty("fadeInDuration").floatValue = Convert.ToSingle(fadeInObj);
            }

            if (payload.TryGetValue("fadeOutDuration", out var fadeOutObj))
            {
                serializedAudio.FindProperty("fadeOutDuration").floatValue = Convert.ToSingle(fadeOutObj);
            }

            if (payload.TryGetValue("audioClipPath", out var clipPathObj))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPathObj.ToString());
                if (clip != null)
                {
                    serializedAudio.FindProperty("audioClip").objectReferenceValue = clip;
                }
            }

            serializedAudio.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(audio.gameObject.scene);

            return CreateSuccessResponse(
                ("audioId", audio.AudioId),
                ("path", BuildGameObjectPath(audio.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectAudio(Dictionary<string, object> payload)
        {
            var audio = ResolveAudioComponent(payload);

            var serializedAudio = new SerializedObject(audio);

            var info = new Dictionary<string, object>
            {
                { "audioId", audio.AudioId },
                { "path", BuildGameObjectPath(audio.gameObject) },
                { "audioType", audio.Type.ToString() },
                { "isPlaying", audio.IsPlaying },
                { "volume", audio.Volume },
                { "playOnEnable", serializedAudio.FindProperty("playOnEnable").boolValue },
                { "loop", serializedAudio.FindProperty("loop").boolValue },
                { "pitch", serializedAudio.FindProperty("pitch").floatValue },
                { "pitchVariation", serializedAudio.FindProperty("pitchVariation").floatValue },
                { "spatialBlend", serializedAudio.FindProperty("spatialBlend").floatValue },
                { "fadeInDuration", serializedAudio.FindProperty("fadeInDuration").floatValue },
                { "fadeOutDuration", serializedAudio.FindProperty("fadeOutDuration").floatValue },
                { "hasAudioClip", serializedAudio.FindProperty("audioClip").objectReferenceValue != null }
            };

            if (serializedAudio.FindProperty("audioClip").objectReferenceValue != null)
            {
                var clip = serializedAudio.FindProperty("audioClip").objectReferenceValue as AudioClip;
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
            var audio = ResolveAudioComponent(payload);
            var path = BuildGameObjectPath(audio.gameObject);
            var audioId = audio.AudioId;
            var scene = audio.gameObject.scene;

            Undo.DestroyObjectImmediate(audio);
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
            var audio = ResolveAudioComponent(payload);
            var volume = GetFloat(payload, "volume", 1f);

            var serializedAudio = new SerializedObject(audio);
            serializedAudio.FindProperty("volume").floatValue = Mathf.Clamp01(volume);
            serializedAudio.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(audio.gameObject.scene);

            return CreateSuccessResponse(
                ("audioId", audio.AudioId),
                ("volume", volume)
            );
        }

        private object SetPitch(Dictionary<string, object> payload)
        {
            var audio = ResolveAudioComponent(payload);
            var pitch = GetFloat(payload, "pitch", 1f);

            var serializedAudio = new SerializedObject(audio);
            serializedAudio.FindProperty("pitch").floatValue = pitch;
            serializedAudio.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(audio.gameObject.scene);

            return CreateSuccessResponse(
                ("audioId", audio.AudioId),
                ("pitch", pitch)
            );
        }

        private object SetLoop(Dictionary<string, object> payload)
        {
            var audio = ResolveAudioComponent(payload);
            var shouldLoop = GetBool(payload, "loop", true);

            var serializedAudio = new SerializedObject(audio);
            serializedAudio.FindProperty("loop").boolValue = shouldLoop;
            serializedAudio.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(audio.gameObject.scene);

            return CreateSuccessResponse(
                ("audioId", audio.AudioId),
                ("loop", shouldLoop)
            );
        }

        private object SetClip(Dictionary<string, object> payload)
        {
            var audio = ResolveAudioComponent(payload);
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

            var serializedAudio = new SerializedObject(audio);
            serializedAudio.FindProperty("audioClip").objectReferenceValue = clip;
            serializedAudio.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(audio.gameObject.scene);

            return CreateSuccessResponse(
                ("audioId", audio.AudioId),
                ("clipPath", clipPath),
                ("clipDuration", clip.length)
            );
        }

        private object FindByAudioId(Dictionary<string, object> payload)
        {
            var audioId = GetString(payload, "audioId");
            if (string.IsNullOrEmpty(audioId))
            {
                throw new InvalidOperationException("audioId is required for findByAudioId.");
            }

            var audio = FindAudioById(audioId);
            if (audio == null)
            {
                return CreateSuccessResponse(("found", false), ("audioId", audioId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("audioId", audio.AudioId),
                ("path", BuildGameObjectPath(audio.gameObject)),
                ("audioType", audio.Type.ToString()),
                ("isPlaying", audio.IsPlaying)
            );
        }

        #endregion

        #region Helpers

        private GameKitAudio ResolveAudioComponent(Dictionary<string, object> payload)
        {
            // Try by audioId first
            var audioId = GetString(payload, "audioId");
            if (!string.IsNullOrEmpty(audioId))
            {
                var audioById = FindAudioById(audioId);
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
                    var audioByPath = targetGo.GetComponent<GameKitAudio>();
                    if (audioByPath != null)
                    {
                        return audioByPath;
                    }
                    throw new InvalidOperationException($"No GameKitAudio component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either audioId or targetPath is required.");
        }

        private GameKitAudio FindAudioById(string audioId)
        {
            var audios = UnityEngine.Object.FindObjectsByType<GameKitAudio>(FindObjectsSortMode.None);
            foreach (var audio in audios)
            {
                if (audio.AudioId == audioId)
                {
                    return audio;
                }
            }
            return null;
        }

        private GameKitAudio.AudioType ParseAudioType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "sfx" => GameKitAudio.AudioType.SFX,
                "music" => GameKitAudio.AudioType.Music,
                "ambient" => GameKitAudio.AudioType.Ambient,
                "voice" => GameKitAudio.AudioType.Voice,
                "ui" => GameKitAudio.AudioType.UI,
                _ => GameKitAudio.AudioType.SFX
            };
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
