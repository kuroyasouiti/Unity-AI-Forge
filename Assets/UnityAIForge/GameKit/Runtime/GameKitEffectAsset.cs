using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Effect Asset: ScriptableObject defining a composite effect
    /// (particle, sound, camera shake, screen flash).
    /// </summary>
    [CreateAssetMenu(fileName = "Effect", menuName = "UnityAIForge/GameKit/Effect")]
    public class GameKitEffectAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string effectId;

        [Header("Effect Components")]
        [SerializeField] private List<EffectComponent> components = new List<EffectComponent>();

        // Properties
        public string EffectId => effectId;
        public IReadOnlyList<EffectComponent> Components => components.AsReadOnly();

        /// <summary>
        /// Initialize the effect asset.
        /// </summary>
        public void Initialize(string id)
        {
            effectId = id;
        }

        /// <summary>
        /// Add a component to this effect.
        /// </summary>
        public void AddComponent(EffectComponent component)
        {
            if (component != null)
            {
                components.Add(component);
            }
        }

        /// <summary>
        /// Remove all components.
        /// </summary>
        public void ClearComponents()
        {
            components.Clear();
        }

        #region Serializable Types

        [Serializable]
        public class EffectComponent
        {
            [Tooltip("Type of effect component")]
            public EffectType type = EffectType.Particle;

            [Header("Particle Settings")]
            [Tooltip("Particle system prefab to instantiate")]
            public GameObject particlePrefab;
            [Tooltip("Duration before destroying particle (0 = auto based on particle system)")]
            public float particleDuration = 0f;
            [Tooltip("Attach particle to target position")]
            public bool attachToTarget = false;
            [Tooltip("Offset from spawn position")]
            public Vector3 positionOffset = Vector3.zero;
            [Tooltip("Scale multiplier for particle")]
            public float particleScale = 1f;

            [Header("Sound Settings")]
            [Tooltip("Audio clip to play")]
            public AudioClip audioClip;
            [Tooltip("Volume (0-1)")]
            [Range(0f, 1f)]
            public float volume = 1f;
            [Tooltip("Pitch variation (random range +/-)")]
            [Range(0f, 0.5f)]
            public float pitchVariation = 0f;
            [Tooltip("3D sound blend (0 = 2D, 1 = 3D)")]
            [Range(0f, 1f)]
            public float spatialBlend = 0f;
            [Tooltip("Min distance for 3D audio")]
            public float minDistance = 1f;
            [Tooltip("Max distance for 3D audio")]
            public float maxDistance = 500f;

            [Header("Camera Shake Settings")]
            [Tooltip("Shake intensity")]
            public float shakeIntensity = 0.3f;
            [Tooltip("Shake duration")]
            public float shakeDuration = 0.2f;
            [Tooltip("Shake frequency (per second)")]
            public float shakeFrequency = 25f;
            [Tooltip("Shake on X axis")]
            public bool shakeX = true;
            [Tooltip("Shake on Y axis")]
            public bool shakeY = true;
            [Tooltip("Shake on Z axis")]
            public bool shakeZ = false;

            [Header("Screen Flash Settings")]
            [Tooltip("Flash color")]
            public Color flashColor = new Color(1f, 1f, 1f, 0.5f);
            [Tooltip("Flash duration")]
            public float flashDuration = 0.1f;
            [Tooltip("Fade out time")]
            public float flashFadeTime = 0.05f;

            [Header("Time Scale Settings")]
            [Tooltip("Target time scale (for hit pause/slow motion)")]
            public float targetTimeScale = 0.1f;
            [Tooltip("Time scale duration")]
            public float timeScaleDuration = 0.1f;
            [Tooltip("Smooth transition time")]
            public float timeScaleTransition = 0.05f;
        }

        public enum EffectType
        {
            Particle,
            Sound,
            CameraShake,
            ScreenFlash,
            TimeScale
        }

        #endregion
    }
}
