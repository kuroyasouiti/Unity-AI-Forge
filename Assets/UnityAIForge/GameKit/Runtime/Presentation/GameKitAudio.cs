using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Audio: Sound effect and music wrapper.
    /// Provides audio pooling, fade controls, and easy integration with game events.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/Presentation/Audio")]
    public class GameKitAudio : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string audioId;

        [Header("Audio Source")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip audioClip;

        [Header("Audio Type")]
        [SerializeField] private AudioType audioType = AudioType.SFX;

        [Header("Playback Settings")]
        [SerializeField] private bool playOnEnable = false;
        [SerializeField] private bool loop = false;
        [SerializeField] private float volume = 1f;
        [SerializeField] private float pitch = 1f;
        [SerializeField] private float pitchVariation = 0f;
        [SerializeField] private float spatialBlend = 0f;

        [Header("Fade Settings")]
        [SerializeField] private float fadeInDuration = 0f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Header("3D Sound Settings")]
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 50f;

        [Header("Events")]
        public UnityEvent OnAudioStarted = new UnityEvent();
        public UnityEvent OnAudioStopped = new UnityEvent();
        public UnityEvent OnAudioCompleted = new UnityEvent();

        // Registry
        private static readonly Dictionary<string, GameKitAudio> _registry = new Dictionary<string, GameKitAudio>();

        // State
        private Coroutine _fadeCoroutine;
        private Coroutine _monitorCoroutine;
        private float _originalVolume;
        private bool _isPlaying;

        public string AudioId => audioId;
        public AudioType Type => audioType;
        public bool IsPlaying => _isPlaying || (audioSource != null && audioSource.isPlaying);
        public float Volume => volume;
        public AudioSource Source => audioSource;

        /// <summary>
        /// Audio type categories.
        /// </summary>
        public enum AudioType
        {
            SFX,        // One-shot sound effects
            Music,      // Background music (looping, fadeable)
            Ambient,    // Environment sounds
            Voice,      // Dialogue/voice lines
            UI          // UI feedback sounds
        }

        /// <summary>
        /// Find audio by ID.
        /// </summary>
        public static GameKitAudio FindById(string id)
        {
            return _registry.TryGetValue(id, out var audio) ? audio : null;
        }

        /// <summary>
        /// Play audio by ID (static helper).
        /// </summary>
        public static void Play(string id)
        {
            var audio = FindById(id);
            if (audio != null)
            {
                audio.PlayAudio();
            }
        }

        /// <summary>
        /// Play audio by ID at position (static helper).
        /// </summary>
        public static void PlayAt(string id, Vector3 position)
        {
            var audio = FindById(id);
            if (audio != null)
            {
                audio.PlayAtPosition(position);
            }
        }

        /// <summary>
        /// Stop audio by ID with fade (static helper).
        /// </summary>
        public static void Stop(string id, bool fade = true)
        {
            var audio = FindById(id);
            if (audio != null)
            {
                if (fade)
                {
                    audio.FadeOut();
                }
                else
                {
                    audio.StopAudio();
                }
            }
        }

        private void Awake()
        {
            EnsureEventsInitialized();

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            _originalVolume = volume;
            ApplySettings();
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(audioId))
            {
                _registry[audioId] = this;
            }

            if (playOnEnable)
            {
                PlayAudio();
            }
        }

        private void OnDisable()
        {
            if (!string.IsNullOrEmpty(audioId))
            {
                _registry.Remove(audioId);
            }

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
            if (_monitorCoroutine != null)
            {
                StopCoroutine(_monitorCoroutine);
            }
        }

        private void EnsureEventsInitialized()
        {
            OnAudioStarted ??= new UnityEvent();
            OnAudioStopped ??= new UnityEvent();
            OnAudioCompleted ??= new UnityEvent();
        }

        private void ApplySettings()
        {
            if (audioSource == null) return;

            audioSource.clip = audioClip;
            audioSource.loop = loop;
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.spatialBlend = spatialBlend;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.playOnAwake = false;
        }

        /// <summary>
        /// Initialize the audio component.
        /// </summary>
        public void Initialize(string id, AudioClip clip, AudioType type = AudioType.SFX)
        {
            audioId = id;
            audioClip = clip;
            audioType = type;

            if (!string.IsNullOrEmpty(audioId))
            {
                _registry[audioId] = this;
            }

            ApplySettings();
            EnsureEventsInitialized();
        }

        /// <summary>
        /// Play the audio.
        /// </summary>
        public void PlayAudio()
        {
            if (audioSource == null) return;

            // Apply pitch variation
            if (pitchVariation > 0)
            {
                audioSource.pitch = pitch + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
            }

            if (fadeInDuration > 0)
            {
                audioSource.volume = 0;
                FadeIn();
            }
            else
            {
                audioSource.volume = volume;
            }

            audioSource.Play();
            _isPlaying = true;
            OnAudioStarted?.Invoke();

            // Monitor for completion (non-looping)
            if (!loop)
            {
                _monitorCoroutine = StartCoroutine(MonitorCompletion());
            }
        }

        /// <summary>
        /// Play the audio as a one-shot.
        /// </summary>
        public void PlayOneShot()
        {
            if (audioSource == null || audioClip == null) return;

            float adjustedVolume = volume;
            float adjustedPitch = pitch;

            if (pitchVariation > 0)
            {
                adjustedPitch += UnityEngine.Random.Range(-pitchVariation, pitchVariation);
            }

            audioSource.pitch = adjustedPitch;
            audioSource.PlayOneShot(audioClip, adjustedVolume);
            OnAudioStarted?.Invoke();
        }

        /// <summary>
        /// Play at a specific position (3D sound).
        /// </summary>
        public void PlayAtPosition(Vector3 position)
        {
            if (audioClip == null) return;

            AudioSource.PlayClipAtPoint(audioClip, position, volume);
            OnAudioStarted?.Invoke();
        }

        /// <summary>
        /// Stop the audio immediately.
        /// </summary>
        public void StopAudio()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            if (_monitorCoroutine != null)
            {
                StopCoroutine(_monitorCoroutine);
                _monitorCoroutine = null;
            }

            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.volume = volume;
            }

            _isPlaying = false;
            OnAudioStopped?.Invoke();
        }

        /// <summary>
        /// Pause the audio.
        /// </summary>
        public void PauseAudio()
        {
            if (audioSource != null)
            {
                audioSource.Pause();
            }
        }

        /// <summary>
        /// Resume the audio.
        /// </summary>
        public void ResumeAudio()
        {
            if (audioSource != null)
            {
                audioSource.UnPause();
            }
        }

        /// <summary>
        /// Fade in the audio.
        /// </summary>
        public void FadeIn()
        {
            FadeIn(fadeInDuration);
        }

        /// <summary>
        /// Fade in the audio over a specific duration.
        /// </summary>
        public void FadeIn(float duration)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            _fadeCoroutine = StartCoroutine(FadeCoroutine(0f, volume, duration, false));
        }

        /// <summary>
        /// Fade out the audio.
        /// </summary>
        public void FadeOut()
        {
            FadeOut(fadeOutDuration);
        }

        /// <summary>
        /// Fade out the audio over a specific duration.
        /// </summary>
        public void FadeOut(float duration)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            _fadeCoroutine = StartCoroutine(FadeCoroutine(audioSource.volume, 0f, duration, true));
        }

        /// <summary>
        /// Cross-fade to another clip.
        /// </summary>
        public void CrossFadeTo(AudioClip newClip, float duration)
        {
            StartCoroutine(CrossFadeCoroutine(newClip, duration));
        }

        private IEnumerator FadeCoroutine(float fromVolume, float toVolume, float duration, bool stopOnComplete)
        {
            if (duration <= 0)
            {
                audioSource.volume = toVolume;
                if (stopOnComplete)
                {
                    StopAudio();
                }
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                audioSource.volume = Mathf.Lerp(fromVolume, toVolume, elapsed / duration);
                yield return null;
            }

            audioSource.volume = toVolume;
            _fadeCoroutine = null;

            if (stopOnComplete)
            {
                StopAudio();
            }
        }

        private IEnumerator CrossFadeCoroutine(AudioClip newClip, float duration)
        {
            // Fade out current
            yield return FadeCoroutine(audioSource.volume, 0f, duration / 2f, false);

            // Switch clip
            audioSource.clip = newClip;
            audioClip = newClip;
            audioSource.Play();

            // Fade in new
            yield return FadeCoroutine(0f, volume, duration / 2f, false);
        }

        private IEnumerator MonitorCompletion()
        {
            while (audioSource.isPlaying)
            {
                yield return null;
            }

            _isPlaying = false;
            _monitorCoroutine = null;
            OnAudioCompleted?.Invoke();
        }

        /// <summary>
        /// Set the volume.
        /// </summary>
        public void SetVolume(float newVolume)
        {
            volume = Mathf.Clamp01(newVolume);
            if (audioSource != null)
            {
                audioSource.volume = volume;
            }
        }

        /// <summary>
        /// Set the pitch.
        /// </summary>
        public void SetPitch(float newPitch)
        {
            pitch = newPitch;
            if (audioSource != null)
            {
                audioSource.pitch = pitch;
            }
        }

        /// <summary>
        /// Set the audio clip.
        /// </summary>
        public void SetClip(AudioClip clip)
        {
            audioClip = clip;
            if (audioSource != null)
            {
                audioSource.clip = clip;
            }
        }

        /// <summary>
        /// Set whether to loop.
        /// </summary>
        public void SetLoop(bool shouldLoop)
        {
            loop = shouldLoop;
            if (audioSource != null)
            {
                audioSource.loop = loop;
            }
        }

        /// <summary>
        /// Get the current playback time.
        /// </summary>
        public float GetPlaybackTime()
        {
            return audioSource != null ? audioSource.time : 0f;
        }

        /// <summary>
        /// Set the playback time.
        /// </summary>
        public void SetPlaybackTime(float time)
        {
            if (audioSource != null)
            {
                audioSource.time = time;
            }
        }

        /// <summary>
        /// Get the clip duration.
        /// </summary>
        public float GetDuration()
        {
            return audioClip != null ? audioClip.length : 0f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
            ApplySettings();
        }
#endif
    }
}
