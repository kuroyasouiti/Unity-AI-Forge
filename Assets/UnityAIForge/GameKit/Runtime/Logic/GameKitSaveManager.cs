using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Save Manager: Singleton manager for save/load operations.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/Save Manager")]
    public class GameKitSaveManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool persistent = true;
        [SerializeField] private string saveDirectory = "Saves";
        [SerializeField] private string fileExtension = ".sav";

        [Header("Registered Profiles")]
        [SerializeField] private List<GameKitSaveProfile> registeredProfiles = new List<GameKitSaveProfile>();

        [Header("Events")]
        public UnityEvent<string, string> OnSaveComplete = new UnityEvent<string, string>();
        public UnityEvent<string, string> OnLoadComplete = new UnityEvent<string, string>();
        public UnityEvent<string, string> OnSaveError = new UnityEvent<string, string>();
        public UnityEvent<string, string> OnLoadError = new UnityEvent<string, string>();
        public UnityEvent<string, string> OnSlotDeleted = new UnityEvent<string, string>();

        // Singleton
        private static GameKitSaveManager _instance;
        public static GameKitSaveManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GameKitSaveManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("GameKitSaveManager");
                        _instance = go.AddComponent<GameKitSaveManager>();
                    }
                }
                return _instance;
            }
        }

        // Profile lookup
        private Dictionary<string, GameKitSaveProfile> profileLookup = new Dictionary<string, GameKitSaveProfile>();

        // Auto-save state
        private Dictionary<string, Coroutine> autoSaveCoroutines = new Dictionary<string, Coroutine>();

        // Properties
        public string SavePath => Path.Combine(Application.persistentDataPath, saveDirectory);

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

            InitializeProfileLookup();
            EnsureSaveDirectoryExists();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.quitting += OnApplicationQuitting;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Application.quitting -= OnApplicationQuitting;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                TriggerAutoSave(AutoSaveTrigger.ApplicationPause);
            }
        }

        private void OnApplicationQuitting()
        {
            TriggerAutoSave(AutoSaveTrigger.ApplicationQuit);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TriggerAutoSave(AutoSaveTrigger.SceneChange);
        }

        #region Profile Management

        /// <summary>
        /// Register a save profile.
        /// </summary>
        public void RegisterProfile(GameKitSaveProfile profile)
        {
            if (profile == null || string.IsNullOrEmpty(profile.ProfileId)) return;

            profileLookup[profile.ProfileId] = profile;
            if (!registeredProfiles.Contains(profile))
            {
                registeredProfiles.Add(profile);
            }

            // Start auto-save if enabled
            StartAutoSave(profile);
        }

        /// <summary>
        /// Unregister a profile.
        /// </summary>
        public void UnregisterProfile(string profileId)
        {
            if (profileLookup.TryGetValue(profileId, out var profile))
            {
                StopAutoSave(profileId);
                profileLookup.Remove(profileId);
                registeredProfiles.Remove(profile);
            }
        }

        /// <summary>
        /// Get a registered profile by ID.
        /// </summary>
        public GameKitSaveProfile GetProfile(string profileId)
        {
            profileLookup.TryGetValue(profileId, out var profile);
            return profile;
        }

        #endregion

        #region Save Operations

        /// <summary>
        /// Save data using a profile to a slot.
        /// </summary>
        public bool Save(string profileId, string slotId)
        {
            if (!profileLookup.TryGetValue(profileId, out var profile))
            {
                OnSaveError?.Invoke(profileId, $"Profile not found: {profileId}");
                return false;
            }

            try
            {
                var saveData = CollectSaveData(profile);
                var json = JsonUtility.ToJson(saveData, true);
                var filePath = GetSlotFilePath(profileId, slotId);

                File.WriteAllText(filePath, json);

                OnSaveComplete?.Invoke(profileId, slotId);
                return true;
            }
            catch (Exception ex)
            {
                OnSaveError?.Invoke(profileId, ex.Message);
                Debug.LogError($"[GameKitSaveManager] Save failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load data from a slot using a profile.
        /// </summary>
        public bool Load(string profileId, string slotId)
        {
            if (!profileLookup.TryGetValue(profileId, out var profile))
            {
                OnLoadError?.Invoke(profileId, $"Profile not found: {profileId}");
                return false;
            }

            var filePath = GetSlotFilePath(profileId, slotId);
            if (!File.Exists(filePath))
            {
                OnLoadError?.Invoke(profileId, $"Save slot not found: {slotId}");
                return false;
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var saveData = JsonUtility.FromJson<SaveData>(json);

                ApplySaveData(profile, saveData);

                OnLoadComplete?.Invoke(profileId, slotId);
                return true;
            }
            catch (Exception ex)
            {
                OnLoadError?.Invoke(profileId, ex.Message);
                Debug.LogError($"[GameKitSaveManager] Load failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Delete a save slot.
        /// </summary>
        public bool DeleteSlot(string profileId, string slotId)
        {
            var filePath = GetSlotFilePath(profileId, slotId);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                OnSlotDeleted?.Invoke(profileId, slotId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get all save slots for a profile.
        /// </summary>
        public List<SaveSlotInfo> GetSlots(string profileId)
        {
            var slots = new List<SaveSlotInfo>();
            var profilePath = GetProfileDirectory(profileId);

            if (!Directory.Exists(profilePath))
            {
                return slots;
            }

            var files = Directory.GetFiles(profilePath, $"*{fileExtension}");
            foreach (var file in files)
            {
                var slotId = Path.GetFileNameWithoutExtension(file);
                var fileInfo = new FileInfo(file);

                slots.Add(new SaveSlotInfo
                {
                    slotId = slotId,
                    profileId = profileId,
                    timestamp = fileInfo.LastWriteTime,
                    fileSize = fileInfo.Length
                });
            }

            slots.Sort((a, b) => b.timestamp.CompareTo(a.timestamp));
            return slots;
        }

        /// <summary>
        /// Check if a slot exists.
        /// </summary>
        public bool SlotExists(string profileId, string slotId)
        {
            return File.Exists(GetSlotFilePath(profileId, slotId));
        }

        #endregion

        #region Data Collection & Application

        private SaveData CollectSaveData(GameKitSaveProfile profile)
        {
            var saveData = new SaveData
            {
                profileId = profile.ProfileId,
                timestamp = DateTime.Now.ToString("o"),
                entries = new List<SaveEntry>()
            };

            foreach (var target in profile.SaveTargets)
            {
                var entry = CollectTargetData(target);
                if (entry != null)
                {
                    saveData.entries.Add(entry);
                }
            }

            return saveData;
        }

        private SaveEntry CollectTargetData(GameKitSaveProfile.SaveTarget target)
        {
            var entry = new SaveEntry
            {
                saveKey = target.saveKey,
                targetType = target.type.ToString()
            };

            switch (target.type)
            {
                case GameKitSaveProfile.SaveTargetType.Transform:
                    entry.data = CollectTransformData(target);
                    break;

                case GameKitSaveProfile.SaveTargetType.Component:
                    entry.data = CollectComponentData(target);
                    break;

                case GameKitSaveProfile.SaveTargetType.ResourceManager:
                    entry.data = CollectResourceManagerData(target);
                    break;

                case GameKitSaveProfile.SaveTargetType.Health:
                    entry.data = CollectHealthData(target);
                    break;

                case GameKitSaveProfile.SaveTargetType.Inventory:
                    entry.data = CollectInventoryData(target);
                    break;

                case GameKitSaveProfile.SaveTargetType.PlayerPrefs:
                    entry.data = CollectPlayerPrefsData(target);
                    break;
            }

            return entry;
        }

        private string CollectTransformData(GameKitSaveProfile.SaveTarget target)
        {
            var go = GameObject.Find(target.gameObjectPath);
            if (go == null) return null;

            var data = new TransformSaveData();
            if (target.savePosition)
            {
                data.position = go.transform.position;
                data.hasPosition = true;
            }
            if (target.saveRotation)
            {
                data.rotation = go.transform.eulerAngles;
                data.hasRotation = true;
            }
            if (target.saveScale)
            {
                data.scale = go.transform.localScale;
                data.hasScale = true;
            }

            return JsonUtility.ToJson(data);
        }

        private string CollectComponentData(GameKitSaveProfile.SaveTarget target)
        {
            var go = GameObject.Find(target.gameObjectPath);
            if (go == null) return null;

            var componentType = FindType(target.componentType);
            if (componentType == null) return null;

            var component = go.GetComponent(componentType);
            if (component == null) return null;

            var data = new Dictionary<string, object>();
            foreach (var propName in target.properties)
            {
                var prop = componentType.GetProperty(propName);
                if (prop != null && prop.CanRead)
                {
                    var value = prop.GetValue(component);
                    data[propName] = value?.ToString() ?? "";
                }

                var field = componentType.GetField(propName);
                if (field != null)
                {
                    var value = field.GetValue(component);
                    data[propName] = value?.ToString() ?? "";
                }
            }

            return JsonUtility.ToJson(new KeyValueList(data));
        }

        private string CollectResourceManagerData(GameKitSaveProfile.SaveTarget target)
        {
            var managers = FindObjectsByType<GameKitResourceManager>(FindObjectsSortMode.None);
            foreach (var manager in managers)
            {
                if (manager.ManagerId == target.resourceManagerId)
                {
                    var data = new ResourceManagerSaveData
                    {
                        managerId = manager.ManagerId,
                        resources = new List<ResourceEntry>()
                    };

                    foreach (var resource in manager.GetAllResources())
                    {
                        data.resources.Add(new ResourceEntry
                        {
                            name = resource.Key,
                            amount = resource.Value
                        });
                    }

                    return JsonUtility.ToJson(data);
                }
            }
            return null;
        }

        private string CollectHealthData(GameKitSaveProfile.SaveTarget target)
        {
            var healths = FindObjectsByType<GameKitHealth>(FindObjectsSortMode.None);
            foreach (var health in healths)
            {
                if (health.HealthId == target.healthId)
                {
                    var data = new HealthSaveData
                    {
                        healthId = health.HealthId,
                        currentHealth = health.CurrentHealth,
                        maxHealth = health.MaxHealth
                    };
                    return JsonUtility.ToJson(data);
                }
            }
            return null;
        }

        private string CollectInventoryData(GameKitSaveProfile.SaveTarget target)
        {
            var inventories = FindObjectsByType<GameKitInventory>(FindObjectsSortMode.None);
            foreach (var inventory in inventories)
            {
                if (inventory.InventoryId == target.inventoryId)
                {
                    return JsonUtility.ToJson(inventory.GetSaveData());
                }
            }
            return null;
        }

        private string CollectPlayerPrefsData(GameKitSaveProfile.SaveTarget target)
        {
            var data = new Dictionary<string, string>();
            foreach (var key in target.properties)
            {
                if (PlayerPrefs.HasKey(key))
                {
                    data[key] = PlayerPrefs.GetString(key);
                }
            }
            return JsonUtility.ToJson(new KeyValueList(data));
        }

        private void ApplySaveData(GameKitSaveProfile profile, SaveData saveData)
        {
            foreach (var entry in saveData.entries)
            {
                var target = FindTarget(profile, entry.saveKey);
                if (target == null) continue;

                ApplyTargetData(target, entry);
            }
        }

        private void ApplyTargetData(GameKitSaveProfile.SaveTarget target, SaveEntry entry)
        {
            if (string.IsNullOrEmpty(entry.data)) return;

            switch (target.type)
            {
                case GameKitSaveProfile.SaveTargetType.Transform:
                    ApplyTransformData(target, entry.data);
                    break;

                case GameKitSaveProfile.SaveTargetType.Component:
                    ApplyComponentData(target, entry.data);
                    break;

                case GameKitSaveProfile.SaveTargetType.ResourceManager:
                    ApplyResourceManagerData(target, entry.data);
                    break;

                case GameKitSaveProfile.SaveTargetType.Health:
                    ApplyHealthData(target, entry.data);
                    break;

                case GameKitSaveProfile.SaveTargetType.Inventory:
                    ApplyInventoryData(target, entry.data);
                    break;

                case GameKitSaveProfile.SaveTargetType.PlayerPrefs:
                    ApplyPlayerPrefsData(target, entry.data);
                    break;
            }
        }

        private void ApplyTransformData(GameKitSaveProfile.SaveTarget target, string json)
        {
            var go = GameObject.Find(target.gameObjectPath);
            if (go == null) return;

            var data = JsonUtility.FromJson<TransformSaveData>(json);
            if (data.hasPosition)
            {
                go.transform.position = data.position;
            }
            if (data.hasRotation)
            {
                go.transform.eulerAngles = data.rotation;
            }
            if (data.hasScale)
            {
                go.transform.localScale = data.scale;
            }
        }

        private void ApplyComponentData(GameKitSaveProfile.SaveTarget target, string json)
        {
            var go = GameObject.Find(target.gameObjectPath);
            if (go == null) return;

            var componentType = FindType(target.componentType);
            if (componentType == null) return;

            var component = go.GetComponent(componentType);
            if (component == null) return;

            var kvList = JsonUtility.FromJson<KeyValueList>(json);
            if (kvList == null) return;
            var data = kvList.ToDictionary();

            foreach (var kvp in data)
            {
                var prop = componentType.GetProperty(kvp.Key);
                if (prop != null && prop.CanWrite)
                {
                    try
                    {
                        var value = Convert.ChangeType(kvp.Value, prop.PropertyType);
                        prop.SetValue(component, value);
                    }
                    catch { }
                }

                var field = componentType.GetField(kvp.Key);
                if (field != null)
                {
                    try
                    {
                        var value = Convert.ChangeType(kvp.Value, field.FieldType);
                        field.SetValue(component, value);
                    }
                    catch { }
                }
            }
        }

        private void ApplyResourceManagerData(GameKitSaveProfile.SaveTarget target, string json)
        {
            var managers = FindObjectsByType<GameKitResourceManager>(FindObjectsSortMode.None);
            foreach (var manager in managers)
            {
                if (manager.ManagerId == target.resourceManagerId)
                {
                    var data = JsonUtility.FromJson<ResourceManagerSaveData>(json);
                    foreach (var resource in data.resources)
                    {
                        manager.SetResource(resource.name, resource.amount);
                    }
                    break;
                }
            }
        }

        private void ApplyHealthData(GameKitSaveProfile.SaveTarget target, string json)
        {
            var healths = FindObjectsByType<GameKitHealth>(FindObjectsSortMode.None);
            foreach (var health in healths)
            {
                if (health.HealthId == target.healthId)
                {
                    var data = JsonUtility.FromJson<HealthSaveData>(json);
                    health.SetMaxHealth(data.maxHealth, false);
                    health.SetHealth(data.currentHealth);
                    break;
                }
            }
        }

        private void ApplyInventoryData(GameKitSaveProfile.SaveTarget target, string json)
        {
            var inventories = FindObjectsByType<GameKitInventory>(FindObjectsSortMode.None);
            foreach (var inventory in inventories)
            {
                if (inventory.InventoryId == target.inventoryId)
                {
                    var data = JsonUtility.FromJson<GameKitInventory.InventoryData>(json);
                    inventory.LoadSaveData(data);
                    break;
                }
            }
        }

        private void ApplyPlayerPrefsData(GameKitSaveProfile.SaveTarget target, string json)
        {
            var kvList = JsonUtility.FromJson<KeyValueList>(json);
            if (kvList == null) return;
            var data = kvList.ToDictionary();

            foreach (var kvp in data)
            {
                PlayerPrefs.SetString(kvp.Key, kvp.Value ?? "");
            }
            PlayerPrefs.Save();
        }

        #endregion

        #region Auto-Save

        private void StartAutoSave(GameKitSaveProfile profile)
        {
            if (!profile.AutoSaveConfig.enabled) return;

            StopAutoSave(profile.ProfileId);

            if (profile.AutoSaveConfig.intervalSeconds > 0)
            {
                var coroutine = StartCoroutine(AutoSaveCoroutine(profile));
                autoSaveCoroutines[profile.ProfileId] = coroutine;
            }
        }

        private void StopAutoSave(string profileId)
        {
            if (autoSaveCoroutines.TryGetValue(profileId, out var coroutine))
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
                autoSaveCoroutines.Remove(profileId);
            }
        }

        private IEnumerator AutoSaveCoroutine(GameKitSaveProfile profile)
        {
            while (true)
            {
                yield return new WaitForSeconds(profile.AutoSaveConfig.intervalSeconds);
                Save(profile.ProfileId, profile.AutoSaveConfig.autoSaveSlotId);
            }
        }

        private void TriggerAutoSave(AutoSaveTrigger trigger)
        {
            foreach (var profile in registeredProfiles)
            {
                if (profile == null || !profile.AutoSaveConfig.enabled) continue;

                bool shouldSave = trigger switch
                {
                    AutoSaveTrigger.SceneChange => profile.AutoSaveConfig.onSceneChange,
                    AutoSaveTrigger.ApplicationPause => profile.AutoSaveConfig.onApplicationPause,
                    AutoSaveTrigger.ApplicationQuit => profile.AutoSaveConfig.onApplicationPause,
                    _ => false
                };

                if (shouldSave)
                {
                    Save(profile.ProfileId, profile.AutoSaveConfig.autoSaveSlotId);
                }
            }
        }

        private enum AutoSaveTrigger
        {
            Interval,
            SceneChange,
            ApplicationPause,
            ApplicationQuit
        }

        #endregion

        #region Helpers

        private void InitializeProfileLookup()
        {
            profileLookup.Clear();
            foreach (var profile in registeredProfiles)
            {
                if (profile != null && !string.IsNullOrEmpty(profile.ProfileId))
                {
                    profileLookup[profile.ProfileId] = profile;
                    StartAutoSave(profile);
                }
            }
        }

        private void EnsureSaveDirectoryExists()
        {
            if (!Directory.Exists(SavePath))
            {
                Directory.CreateDirectory(SavePath);
            }
        }

        private string GetProfileDirectory(string profileId)
        {
            return Path.Combine(SavePath, profileId);
        }

        private string GetSlotFilePath(string profileId, string slotId)
        {
            var profileDir = GetProfileDirectory(profileId);
            if (!Directory.Exists(profileDir))
            {
                Directory.CreateDirectory(profileDir);
            }
            return Path.Combine(profileDir, slotId + fileExtension);
        }

        private GameKitSaveProfile.SaveTarget FindTarget(GameKitSaveProfile profile, string saveKey)
        {
            foreach (var target in profile.SaveTargets)
            {
                if (target.saveKey == saveKey)
                {
                    return target;
                }
            }
            return null;
        }

        private Type FindType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null) return type;
            }
            return null;
        }

        #endregion

        #region Serializable Types

        [Serializable]
        public class SaveData
        {
            public string profileId;
            public string timestamp;
            public List<SaveEntry> entries = new List<SaveEntry>();
        }

        [Serializable]
        public class SaveEntry
        {
            public string saveKey;
            public string targetType;
            public string data;
        }

        [Serializable]
        public class SaveSlotInfo
        {
            public string slotId;
            public string profileId;
            public DateTime timestamp;
            public long fileSize;
        }

        [Serializable]
        private class TransformSaveData
        {
            public Vector3 position;
            public Vector3 rotation;
            public Vector3 scale;
            public bool hasPosition;
            public bool hasRotation;
            public bool hasScale;
        }

        [Serializable]
        private class ResourceManagerSaveData
        {
            public string managerId;
            public List<ResourceEntry> resources = new List<ResourceEntry>();
        }

        [Serializable]
        private class ResourceEntry
        {
            public string name;
            public float amount;
        }

        [Serializable]
        private class HealthSaveData
        {
            public string healthId;
            public float currentHealth;
            public float maxHealth;
        }

        [Serializable]
        private class KeyValueList
        {
            public List<KeyValueEntry> entries = new List<KeyValueEntry>();

            public KeyValueList() { }

            public KeyValueList(Dictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    entries.Add(new KeyValueEntry { key = kvp.Key, value = kvp.Value?.ToString() ?? "" });
                }
            }

            public KeyValueList(Dictionary<string, string> dict)
            {
                foreach (var kvp in dict)
                {
                    entries.Add(new KeyValueEntry { key = kvp.Key, value = kvp.Value ?? "" });
                }
            }

            public Dictionary<string, string> ToDictionary()
            {
                var dict = new Dictionary<string, string>();
                foreach (var entry in entries)
                {
                    dict[entry.key] = entry.value;
                }
                return dict;
            }
        }

        [Serializable]
        private class KeyValueEntry
        {
            public string key;
            public string value;
        }

        #endregion
    }
}
