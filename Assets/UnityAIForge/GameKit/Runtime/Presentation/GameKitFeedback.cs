using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Feedback: Game feel effects system.
    /// Orchestrates multiple feedback components (hitstop, screen shake, flash, etc.)
    /// for cohesive game feel.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/Presentation/Feedback")]
    public class GameKitFeedback : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string feedbackId;

        [Header("Feedback Components")]
        [SerializeField] private List<FeedbackComponent> components = new List<FeedbackComponent>();

        [Header("Settings")]
        [SerializeField] private bool playOnEnable = false;
        [SerializeField] private float globalIntensityMultiplier = 1f;

        [Header("Events")]
        public UnityEvent OnFeedbackStarted = new UnityEvent();
        public UnityEvent OnFeedbackCompleted = new UnityEvent();

        // Registry
        private static readonly Dictionary<string, GameKitFeedback> _registry = new Dictionary<string, GameKitFeedback>();

        // State
        private Coroutine _playCoroutine;
        private bool _isPlaying;

        public string FeedbackId => feedbackId;
        public bool IsPlaying => _isPlaying;
        public List<FeedbackComponent> Components => components;

        /// <summary>
        /// Feedback component types.
        /// </summary>
        public enum FeedbackType
        {
            Hitstop,        // Time scale pause
            ScreenShake,    // Camera shake
            Flash,          // Screen flash
            ColorFlash,     // Object color flash
            Scale,          // Scale punch effect
            Position,       // Position shake/punch
            Rotation,       // Rotation shake
            Sound,          // Play audio
            Particle,       // Play particle effect
            Haptic          // Controller vibration
        }

        /// <summary>
        /// Single feedback component configuration.
        /// </summary>
        [Serializable]
        public class FeedbackComponent
        {
            public FeedbackType type = FeedbackType.ScreenShake;
            public float delay = 0f;
            public float duration = 0.1f;
            public float intensity = 1f;

            // Hitstop specific
            public float hitstopTimeScale = 0f;

            // Shake specific
            public float shakeFrequency = 25f;
            public bool shakeX = true;
            public bool shakeY = true;
            public bool shakeZ = false;

            // Flash specific
            public Color flashColor = Color.white;
            public float fadeTime = 0.1f;

            // Scale specific
            public Vector3 scaleAmount = new Vector3(1.2f, 1.2f, 1.2f);
            public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

            // Position specific
            public Vector3 positionAmount = Vector3.zero;

            // Sound specific
            public AudioClip soundClip;
            public float soundVolume = 1f;

            // Particle specific
            public GameObject particlePrefab;
            public Vector3 particleOffset = Vector3.zero;

            // Haptic specific
            public float hapticIntensity = 0.5f;
        }

        /// <summary>
        /// Find feedback by ID.
        /// </summary>
        public static GameKitFeedback FindById(string id)
        {
            return _registry.TryGetValue(id, out var feedback) ? feedback : null;
        }

        /// <summary>
        /// Play feedback by ID (static helper).
        /// </summary>
        public static void Play(string id)
        {
            var feedback = FindById(id);
            if (feedback != null)
            {
                feedback.PlayFeedback();
            }
        }

        private void Awake()
        {
            EnsureEventsInitialized();
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(feedbackId))
            {
                _registry[feedbackId] = this;
            }

            if (playOnEnable)
            {
                PlayFeedback();
            }
        }

        private void OnDisable()
        {
            if (!string.IsNullOrEmpty(feedbackId))
            {
                _registry.Remove(feedbackId);
            }

            StopFeedback();
        }

        private void EnsureEventsInitialized()
        {
            OnFeedbackStarted ??= new UnityEvent();
            OnFeedbackCompleted ??= new UnityEvent();
        }

        /// <summary>
        /// Initialize the feedback with specified ID and components.
        /// </summary>
        public void Initialize(string id, List<FeedbackComponent> feedbackComponents)
        {
            feedbackId = id;
            components = feedbackComponents ?? new List<FeedbackComponent>();

            if (!string.IsNullOrEmpty(feedbackId))
            {
                _registry[feedbackId] = this;
            }

            EnsureEventsInitialized();
        }

        /// <summary>
        /// Play all feedback components.
        /// </summary>
        public void PlayFeedback()
        {
            PlayFeedback(globalIntensityMultiplier);
        }

        /// <summary>
        /// Play all feedback components with intensity multiplier.
        /// </summary>
        public void PlayFeedback(float intensityMultiplier)
        {
            if (_playCoroutine != null)
            {
                StopCoroutine(_playCoroutine);
            }

            _playCoroutine = StartCoroutine(PlayFeedbackCoroutine(intensityMultiplier));
        }

        /// <summary>
        /// Stop all feedback components.
        /// </summary>
        public void StopFeedback()
        {
            if (_playCoroutine != null)
            {
                StopCoroutine(_playCoroutine);
                _playCoroutine = null;
            }

            _isPlaying = false;
        }

        private IEnumerator PlayFeedbackCoroutine(float intensityMultiplier)
        {
            _isPlaying = true;
            OnFeedbackStarted?.Invoke();

            float maxDuration = 0f;

            foreach (var component in components)
            {
                StartCoroutine(PlayComponentCoroutine(component, intensityMultiplier));
                maxDuration = Mathf.Max(maxDuration, component.delay + component.duration + component.fadeTime);
            }

            yield return new WaitForSecondsRealtime(maxDuration);

            _isPlaying = false;
            _playCoroutine = null;
            OnFeedbackCompleted?.Invoke();
        }

        private IEnumerator PlayComponentCoroutine(FeedbackComponent component, float intensityMultiplier)
        {
            if (component.delay > 0)
            {
                yield return new WaitForSecondsRealtime(component.delay);
            }

            float intensity = component.intensity * intensityMultiplier;

            switch (component.type)
            {
                case FeedbackType.Hitstop:
                    yield return PlayHitstop(component, intensity);
                    break;

                case FeedbackType.ScreenShake:
                    PlayScreenShake(component, intensity);
                    break;

                case FeedbackType.Flash:
                    PlayScreenFlash(component, intensity);
                    break;

                case FeedbackType.ColorFlash:
                    yield return PlayColorFlash(component, intensity);
                    break;

                case FeedbackType.Scale:
                    yield return PlayScalePunch(component, intensity);
                    break;

                case FeedbackType.Position:
                    yield return PlayPositionPunch(component, intensity);
                    break;

                case FeedbackType.Rotation:
                    yield return PlayRotationShake(component, intensity);
                    break;

                case FeedbackType.Sound:
                    PlaySound(component, intensity);
                    break;

                case FeedbackType.Particle:
                    PlayParticle(component);
                    break;

                case FeedbackType.Haptic:
                    PlayHaptic(component, intensity);
                    break;
            }
        }

        private IEnumerator PlayHitstop(FeedbackComponent component, float intensity)
        {
            float originalTimeScale = Time.timeScale;
            Time.timeScale = component.hitstopTimeScale;

            yield return new WaitForSecondsRealtime(component.duration * intensity);

            Time.timeScale = originalTimeScale;
        }

        private void PlayScreenShake(FeedbackComponent component, float intensity)
        {
            if (GameKitEffectManager.Instance != null)
            {
                GameKitEffectManager.Instance.ShakeCamera(
                    component.intensity * intensity,
                    component.duration,
                    component.shakeFrequency
                );
            }
        }

        private void PlayScreenFlash(FeedbackComponent component, float intensity)
        {
            if (GameKitEffectManager.Instance != null)
            {
                Color flashColor = component.flashColor;
                flashColor.a *= intensity;
                GameKitEffectManager.Instance.FlashScreen(
                    flashColor,
                    component.duration,
                    component.fadeTime
                );
            }
        }

        private IEnumerator PlayColorFlash(FeedbackComponent component, float intensity)
        {
            var renderers = GetComponentsInChildren<Renderer>();
            var originalColors = new Dictionary<Renderer, Color>();

            // Store original colors and apply flash
            foreach (var renderer in renderers)
            {
                if (renderer.material.HasProperty("_Color"))
                {
                    originalColors[renderer] = renderer.material.color;
                    renderer.material.color = component.flashColor;
                }
            }

            yield return new WaitForSecondsRealtime(component.duration);

            // Fade back
            float elapsed = 0f;
            while (elapsed < component.fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / component.fadeTime;

                foreach (var kvp in originalColors)
                {
                    if (kvp.Key != null)
                    {
                        kvp.Key.material.color = Color.Lerp(component.flashColor, kvp.Value, t);
                    }
                }

                yield return null;
            }

            // Restore original
            foreach (var kvp in originalColors)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.material.color = kvp.Value;
                }
            }
        }

        private IEnumerator PlayScalePunch(FeedbackComponent component, float intensity)
        {
            Vector3 originalScale = transform.localScale;
            Vector3 targetScale = Vector3.Scale(originalScale, component.scaleAmount);
            targetScale = Vector3.Lerp(originalScale, targetScale, intensity);

            float elapsed = 0f;
            float halfDuration = component.duration / 2f;

            // Scale up
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = component.scaleCurve.Evaluate(elapsed / halfDuration);
                transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }

            // Scale back
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = component.scaleCurve.Evaluate(elapsed / halfDuration);
                transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }

            transform.localScale = originalScale;
        }

        private IEnumerator PlayPositionPunch(FeedbackComponent component, float intensity)
        {
            Vector3 originalPosition = transform.localPosition;
            Vector3 punchAmount = component.positionAmount * intensity;

            float elapsed = 0f;
            while (elapsed < component.duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float decay = 1f - (elapsed / component.duration);

                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-punchAmount.x, punchAmount.x),
                    UnityEngine.Random.Range(-punchAmount.y, punchAmount.y),
                    UnityEngine.Random.Range(-punchAmount.z, punchAmount.z)
                ) * decay;

                transform.localPosition = originalPosition + offset;
                yield return null;
            }

            transform.localPosition = originalPosition;
        }

        private IEnumerator PlayRotationShake(FeedbackComponent component, float intensity)
        {
            Quaternion originalRotation = transform.localRotation;
            float shakeAmount = component.intensity * intensity * 10f;

            float elapsed = 0f;
            while (elapsed < component.duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float decay = 1f - (elapsed / component.duration);

                Vector3 euler = new Vector3(
                    component.shakeX ? UnityEngine.Random.Range(-shakeAmount, shakeAmount) * decay : 0f,
                    component.shakeY ? UnityEngine.Random.Range(-shakeAmount, shakeAmount) * decay : 0f,
                    component.shakeZ ? UnityEngine.Random.Range(-shakeAmount, shakeAmount) * decay : 0f
                );

                transform.localRotation = originalRotation * Quaternion.Euler(euler);
                yield return null;
            }

            transform.localRotation = originalRotation;
        }

        private void PlaySound(FeedbackComponent component, float intensity)
        {
            if (component.soundClip == null) return;

            if (GameKitEffectManager.Instance != null)
            {
                GameKitEffectManager.Instance.PlayOneShot(
                    component.soundClip,
                    transform.position,
                    component.soundVolume * intensity
                );
            }
            else
            {
                AudioSource.PlayClipAtPoint(component.soundClip, transform.position, component.soundVolume * intensity);
            }
        }

        private void PlayParticle(FeedbackComponent component)
        {
            if (component.particlePrefab == null) return;

            Vector3 spawnPos = transform.position + component.particleOffset;
            var instance = Instantiate(component.particlePrefab, spawnPos, Quaternion.identity);

            var ps = instance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                Destroy(instance, duration);
            }
            else
            {
                Destroy(instance, 2f);
            }
        }

        private void PlayHaptic(FeedbackComponent component, float intensity)
        {
            // Platform-specific haptic feedback
#if UNITY_INPUT_SYSTEM_INSTALLED
            var gamepad = UnityEngine.InputSystem.Gamepad.current;
            if (gamepad != null)
            {
                float motorIntensity = component.hapticIntensity * intensity;
                gamepad.SetMotorSpeeds(motorIntensity, motorIntensity);

                // Schedule stop
                StartCoroutine(StopHapticAfterDelay(component.duration));
            }
#endif
        }

        private IEnumerator StopHapticAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

#if UNITY_INPUT_SYSTEM_INSTALLED
            var gamepad = UnityEngine.InputSystem.Gamepad.current;
            if (gamepad != null)
            {
                gamepad.SetMotorSpeeds(0f, 0f);
            }
#endif
        }

        /// <summary>
        /// Add a feedback component.
        /// </summary>
        public void AddComponent(FeedbackComponent component)
        {
            components.Add(component);
        }

        /// <summary>
        /// Remove all feedback components.
        /// </summary>
        public void ClearComponents()
        {
            components.Clear();
        }

        /// <summary>
        /// Set the global intensity multiplier.
        /// </summary>
        public void SetIntensity(float intensity)
        {
            globalIntensityMultiplier = intensity;
        }
    }
}
