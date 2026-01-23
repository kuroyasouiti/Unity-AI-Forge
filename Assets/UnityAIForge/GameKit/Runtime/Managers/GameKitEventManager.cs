using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Event Manager: event hub for game-wide events.
    /// Automatically added by GameKitManager when ManagerType.EventHub is selected.
    /// Implements IGameManager for factory-based creation.
    /// </summary>
    [AddComponentMenu("")]
    public class GameKitEventManager : MonoBehaviour, IGameManager
    {
        [Header("Registered Events")]
        [SerializeField] private List<string> registeredEventNames = new List<string>();
        
        private Dictionary<string, UnityEvent> eventDictionary = new Dictionary<string, UnityEvent>();
        
        [Header("Settings")]
        [Tooltip("Log event triggers for debugging")]
        [SerializeField] private bool logEvents = false;

        private void Awake()
        {
            eventDictionary = new Dictionary<string, UnityEvent>();
        }

        public void RegisterListener(string eventName, UnityAction callback)
        {
            if (!eventDictionary.ContainsKey(eventName))
            {
                eventDictionary[eventName] = new UnityEvent();
                if (!registeredEventNames.Contains(eventName))
                {
                    registeredEventNames.Add(eventName);
                }
            }
            eventDictionary[eventName].AddListener(callback);
        }

        public void UnregisterListener(string eventName, UnityAction callback)
        {
            if (eventDictionary.ContainsKey(eventName))
            {
                eventDictionary[eventName].RemoveListener(callback);
            }
        }

        public void TriggerEvent(string eventName)
        {
            if (eventDictionary.ContainsKey(eventName))
            {
                if (logEvents)
                    Debug.Log($"[GameKitEventManager] Triggered event: {eventName}");
                eventDictionary[eventName]?.Invoke();
            }
            else if (logEvents)
            {
                Debug.LogWarning($"[GameKitEventManager] Event '{eventName}' not found");
            }
        }

        public void ClearEvent(string eventName)
        {
            if (eventDictionary.ContainsKey(eventName))
            {
                eventDictionary[eventName].RemoveAllListeners();
                eventDictionary.Remove(eventName);
                registeredEventNames.Remove(eventName);
            }
        }

        public void ClearAllEvents()
        {
            foreach (var evt in eventDictionary.Values)
            {
                evt.RemoveAllListeners();
            }
            eventDictionary.Clear();
            registeredEventNames.Clear();
        }

        public bool HasEvent(string eventName)
        {
            return eventDictionary.ContainsKey(eventName);
        }

        public List<string> GetAllEventNames()
        {
            return new List<string>(registeredEventNames);
        }

        #region IGameManager Implementation

        private string _managerId;

        /// <summary>
        /// IGameManager: Manager type identifier.
        /// </summary>
        public string ManagerTypeId => "EventHub";

        /// <summary>
        /// The manager instance ID.
        /// </summary>
        public string ManagerId => _managerId;

        /// <summary>
        /// Initializes the manager with the specified ID.
        /// IGameManager implementation.
        /// </summary>
        public void Initialize(string managerId)
        {
            _managerId = managerId;
            Debug.Log($"[GameKitEventManager] Initialized with ID: {managerId}");
        }

        /// <summary>
        /// Resets the manager to its initial state.
        /// IGameManager implementation.
        /// </summary>
        void IGameManager.Reset()
        {
            ClearAllEvents();
            Debug.Log($"[GameKitEventManager] Reset manager: {_managerId}");
        }

        /// <summary>
        /// Cleans up resources when the manager is no longer needed.
        /// IGameManager implementation.
        /// </summary>
        public void Cleanup()
        {
            ClearAllEvents();
            Debug.Log($"[GameKitEventManager] Cleaned up manager: {_managerId}");
        }

        #endregion
    }
}

