using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Effect Manager: Singleton manager for playing composite effects.
    /// Handles particle systems, audio, camera shake, and screen flash.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/Effect Manager")]
    public class GameKitEffectManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool persistent = true;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Canvas screenFlashCanvas;

        [Header("Effect Assets")]
        [SerializeField] private List<GameKitEffectAsset> registeredEffects = new List<GameKitEffectAsset>();

        [Header("Audio Pool")]
        [SerializeField] private int audioSourcePoolSize = 10;

        [Header("Events")]
        public UnityEvent<string> OnEffectPlayed = new UnityEvent<string>();
        public UnityEvent<string> OnEffectEnded = new UnityEvent<string>();

        // Singleton
        private static GameKitEffectManager _instance;
        public static GameKitEffectManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GameKitEffectManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("GameKitEffectManager");
                        _instance = go.AddComponent<GameKitEffectManager>();
                    }
                }
                return _instance;
            }
        }

        // Effect lookup
        private Dictionary<string, GameKitEffectAsset> effectLookup = new Dictionary<string, GameKitEffectAsset>();

        // Audio pool
        private List<AudioSource> audioSourcePool = new List<AudioSource>();
        private int currentAudioIndex = 0;

        // Camera shake state
        private Vector3 originalCameraPosition;
        private Coroutine cameraShakeCoroutine;
        private bool isShaking = false;

        // Screen flash
        private Image screenFlashImage;
        private Coroutine screenFlashCoroutine;

        // Time scale
        private Coroutine timeScaleCoroutine;
        private float originalTimeScale = 1f;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (persistent)
            {
                DontDestroyOnLoad(gameObject);
            }

            InitializeEffectLookup();
            InitializeAudioPool();
            InitializeScreenFlash();

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void LateUpdate()
        {
            // Store camera position when not shaking for restore
            if (!isShaking && mainCamera != null)
            {
                originalCameraPosition = mainCamera.transform.localPosition;
            }
        }

        /// <summary>
        /// Register an effect asset for lookup by ID.
        /// </summary>
        public void RegisterEffect(GameKitEffectAsset asset)
        {
            if (asset == null || string.IsNullOrEmpty(asset.EffectId)) return;

            effectLookup[asset.EffectId] = asset;
            if (!registeredEffects.Contains(asset))
            {
                registeredEffects.Add(asset);
            }
        }

        /// <summary>
        /// Unregister an effect by ID.
        /// </summary>
        public void UnregisterEffect(string effectId)
        {
            if (effectLookup.TryGetValue(effectId, out var asset))
            {
                effectLookup.Remove(effectId);
                registeredEffects.Remove(asset);
            }
        }

        /// <summary>
        /// Play an effect by ID at a position.
        /// </summary>
        public void PlayEffect(string effectId, Vector3 position)
        {
            if (!effectLookup.TryGetValue(effectId, out var asset))
            {
                Debug.LogWarning($"[GameKitEffectManager] Effect not found: {effectId}");
                return;
            }

            PlayEffect(asset, position);
        }

        /// <summary>
        /// Play an effect asset at a position.
        /// </summary>
        public void PlayEffect(GameKitEffectAsset asset, Vector3 position)
        {
            if (asset == null) return;

            foreach (var component in asset.Components)
            {
                PlayEffectComponent(component, position);
            }

            OnEffectPlayed?.Invoke(asset.EffectId);
        }

        /// <summary>
        /// Play an effect by ID at a transform (follows target).
        /// </summary>
        public void PlayEffectAtTransform(string effectId, Transform target)
        {
            if (!effectLookup.TryGetValue(effectId, out var asset))
            {
                Debug.LogWarning($"[GameKitEffectManager] Effect not found: {effectId}");
                return;
            }

            PlayEffectAtTransform(asset, target);
        }

        /// <summary>
        /// Play an effect asset at a transform.
        /// </summary>
        public void PlayEffectAtTransform(GameKitEffectAsset asset, Transform target)
        {
            if (asset == null || target == null) return;

            foreach (var component in asset.Components)
            {
                PlayEffectComponent(component, target.position, target);
            }

            OnEffectPlayed?.Invoke(asset.EffectId);
        }

        private void PlayEffectComponent(GameKitEffectAsset.EffectComponent component, Vector3 position, Transform attachTarget = null)
        {
            switch (component.type)
            {
                case GameKitEffectAsset.EffectType.Particle:
                    PlayParticle(component, position, attachTarget);
                    break;

                case GameKitEffectAsset.EffectType.Sound:
                    PlaySound(component, position);
                    break;

                case GameKitEffectAsset.EffectType.CameraShake:
                    PlayCameraShake(component);
                    break;

                case GameKitEffectAsset.EffectType.ScreenFlash:
                    PlayScreenFlash(component);
                    break;

                case GameKitEffectAsset.EffectType.TimeScale:
                    PlayTimeScale(component);
                    break;
            }
        }

        #region Particle Effects

        private void PlayParticle(GameKitEffectAsset.EffectComponent component, Vector3 position, Transform attachTarget)
        {
            if (component.particlePrefab == null) return;

            var spawnPos = position + component.positionOffset;
            var instance = Instantiate(component.particlePrefab, spawnPos, Quaternion.identity);

            if (component.particleScale != 1f)
            {
                instance.transform.localScale *= component.particleScale;
            }

            if (component.attachToTarget && attachTarget != null)
            {
                instance.transform.SetParent(attachTarget);
            }

            // Determine duration
            float duration = component.particleDuration;
            if (duration <= 0)
            {
                var ps = instance.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    duration = ps.main.duration + ps.main.startLifetime.constantMax;
                }
                else
                {
                    duration = 2f; // Default fallback
                }
            }

            Destroy(instance, duration);
        }

        #endregion

        #region Audio Effects

        private void InitializeAudioPool()
        {
            for (int i = 0; i < audioSourcePoolSize; i++)
            {
                var go = new GameObject($"AudioSource_{i}");
                go.transform.SetParent(transform);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                audioSourcePool.Add(source);
            }
        }

        private AudioSource GetPooledAudioSource()
        {
            var source = audioSourcePool[currentAudioIndex];
            currentAudioIndex = (currentAudioIndex + 1) % audioSourcePool.Count;
            return source;
        }

        private void PlaySound(GameKitEffectAsset.EffectComponent component, Vector3 position)
        {
            if (component.audioClip == null) return;

            var source = GetPooledAudioSource();
            source.transform.position = position;
            source.clip = component.audioClip;
            source.volume = component.volume;
            source.spatialBlend = component.spatialBlend;
            source.minDistance = component.minDistance;
            source.maxDistance = component.maxDistance;

            // Apply pitch variation
            float pitch = 1f;
            if (component.pitchVariation > 0)
            {
                pitch = 1f + UnityEngine.Random.Range(-component.pitchVariation, component.pitchVariation);
            }
            source.pitch = pitch;

            source.Play();
        }

        /// <summary>
        /// Play a one-shot sound at a position.
        /// </summary>
        public void PlayOneShot(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;

            var source = GetPooledAudioSource();
            source.transform.position = position;
            source.PlayOneShot(clip, volume);
        }

        #endregion

        #region Camera Shake

        private void PlayCameraShake(GameKitEffectAsset.EffectComponent component)
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            if (cameraShakeCoroutine != null)
            {
                StopCoroutine(cameraShakeCoroutine);
            }

            cameraShakeCoroutine = StartCoroutine(CameraShakeCoroutine(
                component.shakeIntensity,
                component.shakeDuration,
                component.shakeFrequency,
                component.shakeX,
                component.shakeY,
                component.shakeZ
            ));
        }

        /// <summary>
        /// Manually trigger camera shake.
        /// </summary>
        public void ShakeCamera(float intensity, float duration, float frequency = 25f)
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            if (cameraShakeCoroutine != null)
            {
                StopCoroutine(cameraShakeCoroutine);
            }

            cameraShakeCoroutine = StartCoroutine(CameraShakeCoroutine(
                intensity, duration, frequency, true, true, false
            ));
        }

        private IEnumerator CameraShakeCoroutine(float intensity, float duration, float frequency,
            bool shakeX, bool shakeY, bool shakeZ)
        {
            isShaking = true;
            float elapsed = 0f;
            float interval = 1f / frequency;
            float timer = 0f;
            Vector3 originalPos = mainCamera.transform.localPosition;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                timer += Time.unscaledDeltaTime;

                if (timer >= interval)
                {
                    timer = 0f;

                    float dampening = 1f - (elapsed / duration);
                    float currentIntensity = intensity * dampening;

                    float offsetX = shakeX ? UnityEngine.Random.Range(-1f, 1f) * currentIntensity : 0f;
                    float offsetY = shakeY ? UnityEngine.Random.Range(-1f, 1f) * currentIntensity : 0f;
                    float offsetZ = shakeZ ? UnityEngine.Random.Range(-1f, 1f) * currentIntensity : 0f;

                    mainCamera.transform.localPosition = originalPos + new Vector3(offsetX, offsetY, offsetZ);
                }

                yield return null;
            }

            mainCamera.transform.localPosition = originalPos;
            isShaking = false;
            cameraShakeCoroutine = null;
        }

        #endregion

        #region Screen Flash

        private void InitializeScreenFlash()
        {
            if (screenFlashCanvas == null)
            {
                // Create canvas for screen flash
                var canvasGo = new GameObject("ScreenFlashCanvas");
                canvasGo.transform.SetParent(transform);
                screenFlashCanvas = canvasGo.AddComponent<Canvas>();
                screenFlashCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                screenFlashCanvas.sortingOrder = 999;

                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            // Create flash image
            if (screenFlashImage == null)
            {
                var imageGo = new GameObject("FlashImage");
                imageGo.transform.SetParent(screenFlashCanvas.transform);
                screenFlashImage = imageGo.AddComponent<Image>();

                var rectTransform = screenFlashImage.rectTransform;
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;

                screenFlashImage.color = Color.clear;
                screenFlashImage.raycastTarget = false;
            }
        }

        private void PlayScreenFlash(GameKitEffectAsset.EffectComponent component)
        {
            if (screenFlashImage == null)
            {
                InitializeScreenFlash();
            }

            if (screenFlashCoroutine != null)
            {
                StopCoroutine(screenFlashCoroutine);
            }

            screenFlashCoroutine = StartCoroutine(ScreenFlashCoroutine(
                component.flashColor,
                component.flashDuration,
                component.flashFadeTime
            ));
        }

        /// <summary>
        /// Manually trigger screen flash.
        /// </summary>
        public void FlashScreen(Color color, float duration, float fadeTime = 0.1f)
        {
            if (screenFlashImage == null)
            {
                InitializeScreenFlash();
            }

            if (screenFlashCoroutine != null)
            {
                StopCoroutine(screenFlashCoroutine);
            }

            screenFlashCoroutine = StartCoroutine(ScreenFlashCoroutine(color, duration, fadeTime));
        }

        private IEnumerator ScreenFlashCoroutine(Color color, float duration, float fadeTime)
        {
            screenFlashImage.color = color;

            yield return new WaitForSecondsRealtime(duration);

            // Fade out
            float elapsed = 0f;
            Color startColor = screenFlashImage.color;
            Color endColor = new Color(color.r, color.g, color.b, 0f);

            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                screenFlashImage.color = Color.Lerp(startColor, endColor, elapsed / fadeTime);
                yield return null;
            }

            screenFlashImage.color = Color.clear;
            screenFlashCoroutine = null;
        }

        #endregion

        #region Time Scale Effects

        private void PlayTimeScale(GameKitEffectAsset.EffectComponent component)
        {
            if (timeScaleCoroutine != null)
            {
                StopCoroutine(timeScaleCoroutine);
            }

            timeScaleCoroutine = StartCoroutine(TimeScaleCoroutine(
                component.targetTimeScale,
                component.timeScaleDuration,
                component.timeScaleTransition
            ));
        }

        /// <summary>
        /// Manually trigger time scale change (hit pause / slow motion).
        /// </summary>
        public void SetTimeScale(float targetScale, float duration, float transitionTime = 0.05f)
        {
            if (timeScaleCoroutine != null)
            {
                StopCoroutine(timeScaleCoroutine);
            }

            timeScaleCoroutine = StartCoroutine(TimeScaleCoroutine(targetScale, duration, transitionTime));
        }

        private IEnumerator TimeScaleCoroutine(float targetScale, float duration, float transitionTime)
        {
            originalTimeScale = Time.timeScale;

            // Transition to target
            float elapsed = 0f;
            while (elapsed < transitionTime)
            {
                elapsed += Time.unscaledDeltaTime;
                Time.timeScale = Mathf.Lerp(originalTimeScale, targetScale, elapsed / transitionTime);
                yield return null;
            }
            Time.timeScale = targetScale;

            // Hold
            yield return new WaitForSecondsRealtime(duration);

            // Transition back
            elapsed = 0f;
            while (elapsed < transitionTime)
            {
                elapsed += Time.unscaledDeltaTime;
                Time.timeScale = Mathf.Lerp(targetScale, originalTimeScale, elapsed / transitionTime);
                yield return null;
            }
            Time.timeScale = originalTimeScale;

            timeScaleCoroutine = null;
        }

        #endregion

        #region Initialization

        private void InitializeEffectLookup()
        {
            effectLookup.Clear();
            foreach (var asset in registeredEffects)
            {
                if (asset != null && !string.IsNullOrEmpty(asset.EffectId))
                {
                    effectLookup[asset.EffectId] = asset;
                }
            }
        }

        /// <summary>
        /// Get an effect asset by ID.
        /// </summary>
        public GameKitEffectAsset GetEffect(string effectId)
        {
            effectLookup.TryGetValue(effectId, out var asset);
            return asset;
        }

        /// <summary>
        /// Check if an effect is registered.
        /// </summary>
        public bool HasEffect(string effectId)
        {
            return effectLookup.ContainsKey(effectId);
        }

        /// <summary>
        /// Get all registered effect IDs.
        /// </summary>
        public IEnumerable<string> GetRegisteredEffectIds()
        {
            return effectLookup.Keys;
        }

        #endregion
    }
}
