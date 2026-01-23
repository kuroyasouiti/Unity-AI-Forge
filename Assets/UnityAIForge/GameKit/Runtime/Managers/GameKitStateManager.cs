using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit State Manager: manages game states (menu, playing, paused, etc.)
    /// Automatically added by GameKitManager when ManagerType.StateManager is selected.
    /// Implements IGameManager for factory-based creation.
    /// </summary>
    [AddComponentMenu("")]
    public class GameKitStateManager : MonoBehaviour, IGameManager
    {
        [Header("State Management")]
        [SerializeField] private string currentState;
        [SerializeField] private string previousState;
        [SerializeField] private List<string> stateHistory = new List<string>();
        
        [Header("Settings")]
        [Tooltip("Maximum state history size")]
        [SerializeField] private int maxHistorySize = 10;
        
        [Tooltip("Log state changes for debugging")]
        [SerializeField] private bool logStateChanges = false;
        
        [Header("Events")]
        [Tooltip("Invoked when state changes (newState, oldState)")]
        public StateChangedEvent OnStateChanged = new StateChangedEvent();

        public string CurrentState => currentState;
        public string PreviousState => previousState;

        public void ChangeState(string newState)
        {
            if (currentState == newState)
                return;

            previousState = currentState;
            currentState = newState;
            
            // Add to history
            stateHistory.Add(newState);
            if (stateHistory.Count > maxHistorySize)
            {
                stateHistory.RemoveAt(0);
            }
            
            OnStateChanged?.Invoke(newState, previousState);
            
            if (logStateChanges)
                Debug.Log($"[GameKitStateManager] State changed: {previousState} → {newState}");
        }

        public void ReturnToPreviousState()
        {
            if (!string.IsNullOrEmpty(previousState))
            {
                ChangeState(previousState);
            }
        }

        public bool IsInState(string stateName)
        {
            return currentState == stateName;
        }

        public List<string> GetStateHistory()
        {
            return new List<string>(stateHistory);
        }

        public void ClearHistory()
        {
            stateHistory.Clear();
        }

        #region IGameManager Implementation

        private string _managerId;

        /// <summary>
        /// IGameManager: Manager type identifier.
        /// </summary>
        public string ManagerTypeId => "StateManager";

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
            Debug.Log($"[GameKitStateManager] Initialized with ID: {managerId}");
        }

        /// <summary>
        /// Resets the manager to its initial state.
        /// IGameManager implementation.
        /// </summary>
        void IGameManager.Reset()
        {
            currentState = null;
            previousState = null;
            ClearHistory();
            Debug.Log($"[GameKitStateManager] Reset manager: {_managerId}");
        }

        /// <summary>
        /// Cleans up resources when the manager is no longer needed.
        /// IGameManager implementation.
        /// </summary>
        public void Cleanup()
        {
            currentState = null;
            previousState = null;
            ClearHistory();
            OnStateChanged?.RemoveAllListeners();
            Debug.Log($"[GameKitStateManager] Cleaned up manager: {_managerId}");
        }

        #endregion

        [Serializable]
        public class StateChangedEvent : UnityEngine.Events.UnityEvent<string, string> { }
    }
}

