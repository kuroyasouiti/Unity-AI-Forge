using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Mid-level audio source utilities: create and configure AudioSource with presets.
    /// </summary>
    public class AudioSourceBundleHandler : BaseCommandHandler
    {
        private static readonly string[] Operations = { "createAudioSource", "updateAudioSource", "inspect" };

        public override string Category => "audioSourceBundle";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "createAudioSource" => CreateAudioSource(payload),
                "updateAudioSource" => UpdateAudioSource(payload),
                "inspect" => InspectAudioSource(payload),
                _ => throw new InvalidOperationException($"Unsupported audio source operation: {operation}"),
            };
        }

        #region Create Audio Source

        private object CreateAudioSource(Dictionary<string, object> payload)
        {
            var gameObjectPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required for createAudioSource.");
            }

            var go = ResolveGameObject(gameObjectPath);
            var audioSource = go.GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = Undo.AddComponent<AudioSource>(go);
            }
            else
            {
                Undo.RecordObject(audioSource, "Update AudioSource");
            }

            var preset = GetString(payload, "preset")?.ToLowerInvariant() ?? "custom";
            ApplyAudioSourcePreset(audioSource, preset);

            // Apply custom settings
            ConfigureAudioSource(audioSource, payload);

            EditorSceneManager.MarkSceneDirty(go.scene);
            return CreateSuccessResponse(("path", BuildGameObjectPath(go)));
        }

        private void ApplyAudioSourcePreset(AudioSource audioSource, string preset)
        {
            switch (preset)
            {
                case "music":
                    audioSource.volume = 0.5f;
                    audioSource.pitch = 1f;
                    audioSource.loop = true;
                    audioSource.playOnAwake = true;
                    audioSource.spatialBlend = 0f; // 2D
                    audioSource.priority = 128;
                    break;

                case "sfx":
                    audioSource.volume = 1f;
                    audioSource.pitch = 1f;
                    audioSource.loop = false;
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0f; // 2D
                    audioSource.priority = 128;
                    break;

                case "ambient":
                    audioSource.volume = 0.3f;
                    audioSource.pitch = 1f;
                    audioSource.loop = true;
                    audioSource.playOnAwake = true;
                    audioSource.spatialBlend = 1f; // 3D
                    audioSource.minDistance = 5f;
                    audioSource.maxDistance = 50f;
                    audioSource.priority = 200;
                    break;

                case "voice":
                    audioSource.volume = 1f;
                    audioSource.pitch = 1f;
                    audioSource.loop = false;
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0.5f; // Blend 2D/3D
                    audioSource.priority = 64;
                    break;

                case "ui":
                    audioSource.volume = 0.8f;
                    audioSource.pitch = 1f;
                    audioSource.loop = false;
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0f; // 2D
                    audioSource.priority = 0; // Highest priority
                    break;

                case "custom":
                    // Keep existing settings or defaults
                    break;
            }
        }

        private void ConfigureAudioSource(AudioSource audioSource, Dictionary<string, object> payload)
        {
            if (payload.TryGetValue("audioClipPath", out var clipPathObj))
            {
                var clipPath = clipPathObj.ToString();
                if (!string.IsNullOrEmpty(clipPath))
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                    if (clip != null)
                    {
                        audioSource.clip = clip;
                    }
                    else
                    {
                        Debug.LogWarning($"AudioClip not found at path: {clipPath}");
                    }
                }
            }

            if (payload.TryGetValue("volume", out var volumeObj))
            {
                audioSource.volume = Mathf.Clamp01(Convert.ToSingle(volumeObj));
            }

            if (payload.TryGetValue("pitch", out var pitchObj))
            {
                audioSource.pitch = Mathf.Clamp(Convert.ToSingle(pitchObj), -3f, 3f);
            }

            if (payload.TryGetValue("loop", out var loopObj))
            {
                audioSource.loop = Convert.ToBoolean(loopObj);
            }

            if (payload.TryGetValue("playOnAwake", out var playOnAwakeObj))
            {
                audioSource.playOnAwake = Convert.ToBoolean(playOnAwakeObj);
            }

            if (payload.TryGetValue("spatialBlend", out var spatialBlendObj))
            {
                audioSource.spatialBlend = Mathf.Clamp01(Convert.ToSingle(spatialBlendObj));
            }

            if (payload.TryGetValue("minDistance", out var minDistObj))
            {
                audioSource.minDistance = Convert.ToSingle(minDistObj);
            }

            if (payload.TryGetValue("maxDistance", out var maxDistObj))
            {
                audioSource.maxDistance = Convert.ToSingle(maxDistObj);
            }

            if (payload.TryGetValue("priority", out var priorityObj))
            {
                audioSource.priority = Mathf.Clamp(Convert.ToInt32(priorityObj), 0, 256);
            }

            if (payload.TryGetValue("mixerGroupPath", out var mixerPathObj))
            {
                var mixerPath = mixerPathObj.ToString();
                if (!string.IsNullOrEmpty(mixerPath))
                {
                    var mixerGroup = AssetDatabase.LoadAssetAtPath<AudioMixerGroup>(mixerPath);
                    if (mixerGroup != null)
                    {
                        audioSource.outputAudioMixerGroup = mixerGroup;
                    }
                    else
                    {
                        Debug.LogWarning($"AudioMixerGroup not found at path: {mixerPath}");
                    }
                }
            }
        }

        #endregion

        #region Update Audio Source

        private object UpdateAudioSource(Dictionary<string, object> payload)
        {
            var gameObjectPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required for updateAudioSource.");
            }

            var go = ResolveGameObject(gameObjectPath);
            var audioSource = go.GetComponent<AudioSource>();

            if (audioSource == null)
            {
                throw new InvalidOperationException($"GameObject '{gameObjectPath}' does not have an AudioSource component.");
            }

            Undo.RecordObject(audioSource, "Update AudioSource");
            ConfigureAudioSource(audioSource, payload);

            EditorSceneManager.MarkSceneDirty(go.scene);
            return CreateSuccessResponse(("path", BuildGameObjectPath(go)));
        }

        #endregion

        #region Inspect

        private object InspectAudioSource(Dictionary<string, object> payload)
        {
            var gameObjectPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required for inspect.");
            }

            var go = ResolveGameObject(gameObjectPath);
            var audioSource = go.GetComponent<AudioSource>();

            if (audioSource == null)
            {
                return CreateSuccessResponse(("hasAudioSource", false), ("path", BuildGameObjectPath(go)));
            }

            var info = new Dictionary<string, object>
            {
                { "path", BuildGameObjectPath(go) },
                { "hasAudioSource", true },
                { "volume", audioSource.volume },
                { "pitch", audioSource.pitch },
                { "loop", audioSource.loop },
                { "playOnAwake", audioSource.playOnAwake },
                { "spatialBlend", audioSource.spatialBlend },
                { "minDistance", audioSource.minDistance },
                { "maxDistance", audioSource.maxDistance },
                { "priority", audioSource.priority }
            };

            if (audioSource.clip != null)
            {
                info["clipName"] = audioSource.clip.name;
                info["clipPath"] = AssetDatabase.GetAssetPath(audioSource.clip);
            }

            if (audioSource.outputAudioMixerGroup != null)
            {
                info["mixerGroupName"] = audioSource.outputAudioMixerGroup.name;
                info["mixerGroupPath"] = AssetDatabase.GetAssetPath(audioSource.outputAudioMixerGroup);
            }

            return CreateSuccessResponse(("audioSource", info));
        }

        #endregion

    }
}

