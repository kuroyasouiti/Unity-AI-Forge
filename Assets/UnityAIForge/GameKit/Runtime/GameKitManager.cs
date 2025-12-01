using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Manager Hub: central hub for game management that delegates to specialized components.
    /// Acts as a hub that adds mode-specific components based on ManagerType.
    /// </summary>
    [AddComponentMenu("SkillForUnity/GameKit/Manager Hub")]
    public class GameKitManager : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string managerId;
        [SerializeField] private ManagerType managerType;
        
        [Header("Persistence")]
        [SerializeField] private bool persistent = false;

        // Component references (populated based on manager type)
        private Component modeComponent;

        public string ManagerId => managerId;
        public ManagerType Type => managerType;
        public bool IsPersistent => persistent;

        private void Awake()
        {
            if (persistent)
            {
                DontDestroyOnLoad(gameObject);
            }
            
            // Ensure mode component is attached
            if (modeComponent == null)
            {
                AttachModeComponent();
            }
        }

        public void Initialize(string id, ManagerType type, bool isPersistent)
        {
            managerId = id;
            managerType = type;
            persistent = isPersistent;
            
            AttachModeComponent();
        }

        private void AttachModeComponent()
        {
            // Remove existing mode component if type changed
            if (modeComponent != null)
            {
                if (Application.isPlaying)
                    Destroy(modeComponent);
                else
                    DestroyImmediate(modeComponent);
            }

            // Add mode-specific component
            switch (managerType)
            {
                case ManagerType.TurnBased:
                    var turnType = System.Type.GetType("UnityAIForge.GameKit.GameKitTurnManager, UnityAIForge.GameKit.Runtime");
                    if (turnType != null)
                    {
                        modeComponent = gameObject.AddComponent(turnType);
                    }
                    break;

                case ManagerType.ResourcePool:
                    var resourceType = System.Type.GetType("UnityAIForge.GameKit.GameKitResourceManager, UnityAIForge.GameKit.Runtime");
                    if (resourceType != null)
                    {
                        modeComponent = gameObject.AddComponent(resourceType);
                    }
                    break;

                case ManagerType.EventHub:
                    var eventType = System.Type.GetType("UnityAIForge.GameKit.GameKitEventManager, UnityAIForge.GameKit.Runtime");
                    if (eventType != null)
                    {
                        modeComponent = gameObject.AddComponent(eventType);
                    }
                    break;

                case ManagerType.StateManager:
                    var stateType = System.Type.GetType("UnityAIForge.GameKit.GameKitStateManager, UnityAIForge.GameKit.Runtime");
                    if (stateType != null)
                    {
                        modeComponent = gameObject.AddComponent(stateType);
                    }
                    break;

                case ManagerType.Realtime:
                    var realtimeType = System.Type.GetType("UnityAIForge.GameKit.GameKitRealtimeManager, UnityAIForge.GameKit.Runtime");
                    if (realtimeType != null)
                    {
                        modeComponent = gameObject.AddComponent(realtimeType);
                    }
                    break;
            }
        }

        /// <summary>
        /// Get the mode-specific component (e.g., GameKitResourceManager).
        /// </summary>
        public T GetModeComponent<T>() where T : Component
        {
            return GetComponent<T>();
        }

        #region Convenience Methods (Delegate to Mode Components via Reflection)

        // Turn-Based convenience methods
        public void AddTurnPhase(string phaseName)
        {
            var turnType = System.Type.GetType("UnityAIForge.GameKit.GameKitTurnManager, UnityAIForge.GameKit.Runtime");
            if (turnType != null)
            {
                var turnManager = GetComponent(turnType);
                turnType.GetMethod("AddTurnPhase")?.Invoke(turnManager, new object[] { phaseName });
            }
        }

        public string GetCurrentPhase()
        {
            var turnType = System.Type.GetType("UnityAIForge.GameKit.GameKitTurnManager, UnityAIForge.GameKit.Runtime");
            if (turnType != null)
            {
                var turnManager = GetComponent(turnType);
                return turnType.GetMethod("GetCurrentPhase")?.Invoke(turnManager, null) as string;
            }
            return null;
        }

        public void NextPhase()
        {
            var turnType = System.Type.GetType("UnityAIForge.GameKit.GameKitTurnManager, UnityAIForge.GameKit.Runtime");
            if (turnType != null)
            {
                var turnManager = GetComponent(turnType);
                turnType.GetMethod("NextPhase")?.Invoke(turnManager, null);
            }
        }

        // Resource Pool convenience methods
        public void SetResource(string resourceName, float amount)
        {
            var resourceType = System.Type.GetType("UnityAIForge.GameKit.GameKitResourceManager, UnityAIForge.GameKit.Runtime");
            if (resourceType != null)
            {
                var resourceManager = GetComponent(resourceType);
                resourceType.GetMethod("SetResource")?.Invoke(resourceManager, new object[] { resourceName, amount });
            }
        }

        public float GetResource(string resourceName, float defaultValue = 0f)
        {
            var resourceType = System.Type.GetType("UnityAIForge.GameKit.GameKitResourceManager, UnityAIForge.GameKit.Runtime");
            if (resourceType != null)
            {
                var resourceManager = GetComponent(resourceType);
                var result = resourceType.GetMethod("GetResource")?.Invoke(resourceManager, new object[] { resourceName, defaultValue });
                return result != null ? (float)result : defaultValue;
            }
            return defaultValue;
        }

        public bool ConsumeResource(string resourceName, float amount)
        {
            var resourceType = System.Type.GetType("UnityAIForge.GameKit.GameKitResourceManager, UnityAIForge.GameKit.Runtime");
            if (resourceType != null)
            {
                var resourceManager = GetComponent(resourceType);
                var result = resourceType.GetMethod("ConsumeResource")?.Invoke(resourceManager, new object[] { resourceName, amount });
                return result != null && (bool)result;
            }
            return false;
        }

        public void AddResource(string resourceName, float amount)
        {
            var resourceType = System.Type.GetType("UnityAIForge.GameKit.GameKitResourceManager, UnityAIForge.GameKit.Runtime");
            if (resourceType != null)
            {
                var resourceManager = GetComponent(resourceType);
                resourceType.GetMethod("AddResource")?.Invoke(resourceManager, new object[] { resourceName, amount });
            }
        }

        public Dictionary<string, float> GetAllResources()
        {
            var resourceType = System.Type.GetType("UnityAIForge.GameKit.GameKitResourceManager, UnityAIForge.GameKit.Runtime");
            if (resourceType != null)
            {
                var resourceManager = GetComponent(resourceType);
                var result = resourceType.GetMethod("GetAllResources")?.Invoke(resourceManager, null);
                return result as Dictionary<string, float> ?? new Dictionary<string, float>();
            }
            return new Dictionary<string, float>();
        }

        // Resource state persistence convenience methods
        public void SaveResourceState(string filePath = null)
        {
            var resourceType = System.Type.GetType("UnityAIForge.GameKit.GameKitResourceManager, UnityAIForge.GameKit.Runtime");
            if (resourceType != null)
            {
                var resourceManager = GetComponent(resourceType);
                if (string.IsNullOrEmpty(filePath))
                {
                    // Save to PlayerPrefs
                    resourceType.GetMethod("SaveStateToPlayerPrefs")?.Invoke(resourceManager, new object[] { $"GameKitState_{ManagerId}" });
                }
                else
                {
                    // Save to file
                    resourceType.GetMethod("SaveStateToFile")?.Invoke(resourceManager, new object[] { filePath, ManagerId });
                }
            }
        }

        public bool LoadResourceState(string filePath = null)
        {
            var resourceType = System.Type.GetType("UnityAIForge.GameKit.GameKitResourceManager, UnityAIForge.GameKit.Runtime");
            if (resourceType != null)
            {
                var resourceManager = GetComponent(resourceType);
                object result;
                if (string.IsNullOrEmpty(filePath))
                {
                    // Load from PlayerPrefs
                    result = resourceType.GetMethod("LoadStateFromPlayerPrefs")?.Invoke(resourceManager, new object[] { $"GameKitState_{ManagerId}", true });
                }
                else
                {
                    // Load from file
                    result = resourceType.GetMethod("LoadStateFromFile")?.Invoke(resourceManager, new object[] { filePath, true });
                }
                return result != null && (bool)result;
            }
            return false;
        }

        // Event Hub convenience methods
        public void TriggerEvent(string eventName)
        {
            var eventType = System.Type.GetType("UnityAIForge.GameKit.GameKitEventManager, UnityAIForge.GameKit.Runtime");
            if (eventType != null)
            {
                var eventManager = GetComponent(eventType);
                eventType.GetMethod("TriggerEvent")?.Invoke(eventManager, new object[] { eventName });
            }
        }

        public void RegisterEventListener(string eventName, System.Action callback)
        {
            var eventType = System.Type.GetType("UnityAIForge.GameKit.GameKitEventManager, UnityAIForge.GameKit.Runtime");
            if (eventType != null)
            {
                var eventManager = GetComponent(eventType);
                eventType.GetMethod("RegisterListener")?.Invoke(eventManager, new object[] { eventName, callback });
            }
        }

        // State Manager convenience methods
        public void ChangeState(string stateName)
        {
            var stateType = System.Type.GetType("UnityAIForge.GameKit.GameKitStateManager, UnityAIForge.GameKit.Runtime");
            if (stateType != null)
            {
                var stateManager = GetComponent(stateType);
                stateType.GetMethod("ChangeState")?.Invoke(stateManager, new object[] { stateName });
            }
        }

        public string GetCurrentState()
        {
            var stateType = System.Type.GetType("UnityAIForge.GameKit.GameKitStateManager, UnityAIForge.GameKit.Runtime");
            if (stateType != null)
            {
                var stateManager = GetComponent(stateType);
                return stateType.GetMethod("GetCurrentState")?.Invoke(stateManager, null) as string;
            }
            return null;
        }

        // Realtime Manager convenience methods
        public void SetTimeScale(float scale)
        {
            var realtimeType = System.Type.GetType("UnityAIForge.GameKit.GameKitRealtimeManager, UnityAIForge.GameKit.Runtime");
            if (realtimeType != null)
            {
                var realtimeManager = GetComponent(realtimeType);
                realtimeType.GetMethod("SetTimeScale")?.Invoke(realtimeManager, new object[] { scale });
            }
        }

        public void Pause()
        {
            var realtimeType = System.Type.GetType("UnityAIForge.GameKit.GameKitRealtimeManager, UnityAIForge.GameKit.Runtime");
            if (realtimeType != null)
            {
                var realtimeManager = GetComponent(realtimeType);
                realtimeType.GetMethod("Pause")?.Invoke(realtimeManager, null);
            }
        }

        public void Resume()
        {
            var realtimeType = System.Type.GetType("UnityAIForge.GameKit.GameKitRealtimeManager, UnityAIForge.GameKit.Runtime");
            if (realtimeType != null)
            {
                var realtimeManager = GetComponent(realtimeType);
                realtimeType.GetMethod("Resume")?.Invoke(realtimeManager, null);
            }
        }

        #endregion

        public enum ManagerType
        {
            TurnBased,
            Realtime,
            ResourcePool,
            EventHub,
            StateManager
        }
    }
}

