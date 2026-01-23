using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Save Profile: ScriptableObject defining what data to save/load.
    /// </summary>
    [CreateAssetMenu(fileName = "SaveProfile", menuName = "UnityAIForge/GameKit/Save Profile")]
    public class GameKitSaveProfile : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string profileId;

        [Header("Save Targets")]
        [SerializeField] private List<SaveTarget> saveTargets = new List<SaveTarget>();

        [Header("Auto Save Settings")]
        [SerializeField] private AutoSaveSettings autoSave = new AutoSaveSettings();

        // Properties
        public string ProfileId => profileId;
        public IReadOnlyList<SaveTarget> SaveTargets => saveTargets.AsReadOnly();
        public AutoSaveSettings AutoSaveConfig => autoSave;

        /// <summary>
        /// Initialize the save profile.
        /// </summary>
        public void Initialize(string id)
        {
            profileId = id;
        }

        /// <summary>
        /// Add a save target.
        /// </summary>
        public void AddTarget(SaveTarget target)
        {
            if (target != null)
            {
                saveTargets.Add(target);
            }
        }

        /// <summary>
        /// Remove a save target by key.
        /// </summary>
        public bool RemoveTarget(string saveKey)
        {
            return saveTargets.RemoveAll(t => t.saveKey == saveKey) > 0;
        }

        /// <summary>
        /// Clear all save targets.
        /// </summary>
        public void ClearTargets()
        {
            saveTargets.Clear();
        }

        #region Serializable Types

        [Serializable]
        public class SaveTarget
        {
            [Tooltip("Type of data to save")]
            public SaveTargetType type = SaveTargetType.Transform;

            [Tooltip("Unique key for this save data")]
            public string saveKey;

            [Header("Transform Settings")]
            [Tooltip("GameObject path for transform saves")]
            public string gameObjectPath;
            [Tooltip("Save position")]
            public bool savePosition = true;
            [Tooltip("Save rotation")]
            public bool saveRotation = true;
            [Tooltip("Save scale")]
            public bool saveScale = false;

            [Header("Component Settings")]
            [Tooltip("Component type name for custom saves")]
            public string componentType;
            [Tooltip("Properties to save from component")]
            public List<string> properties = new List<string>();

            [Header("GameKit Integration")]
            [Tooltip("ResourceManager ID for resource saves")]
            public string resourceManagerId;
            [Tooltip("Health ID for health saves")]
            public string healthId;
            [Tooltip("SceneFlow ID for scene saves")]
            public string sceneFlowId;
            [Tooltip("Inventory ID for inventory saves")]
            public string inventoryId;
        }

        [Serializable]
        public class AutoSaveSettings
        {
            [Tooltip("Enable auto-save")]
            public bool enabled = false;

            [Tooltip("Auto-save interval in seconds")]
            public float intervalSeconds = 300f;

            [Tooltip("Auto-save on scene change")]
            public bool onSceneChange = false;

            [Tooltip("Auto-save on application pause/quit")]
            public bool onApplicationPause = true;

            [Tooltip("Default slot for auto-save")]
            public string autoSaveSlotId = "autosave";
        }

        public enum SaveTargetType
        {
            Transform,
            Component,
            ResourceManager,
            Health,
            SceneFlow,
            Inventory,
            PlayerPrefs
        }

        #endregion
    }
}
