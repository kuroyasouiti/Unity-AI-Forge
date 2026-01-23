using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Turn Manager: manages turn-based game flow.
    /// Automatically added by GameKitManager when ManagerType.TurnBased is selected.
    /// Implements IGameManager for factory-based creation.
    /// </summary>
    [AddComponentMenu("")]
    public class GameKitTurnManager : MonoBehaviour, IGameManager
    {
        [Header("Turn Phases")]
        [SerializeField] private List<string> turnPhases = new List<string>();
        [SerializeField] private int currentPhaseIndex = 0;
        
        [Header("Turn Counter")]
        [SerializeField] private int currentTurn = 1;
        
        [Header("Events")]
        [Tooltip("Invoked when phase changes (phaseName)")]
        public PhaseChangedEvent OnPhaseChanged = new PhaseChangedEvent();
        
        [Tooltip("Invoked when turn advances (turnNumber)")]
        public TurnAdvancedEvent OnTurnAdvanced = new TurnAdvancedEvent();

        public int CurrentTurn => currentTurn;
        public int CurrentPhaseIndex => currentPhaseIndex;

        public void AddTurnPhase(string phaseName)
        {
            if (!turnPhases.Contains(phaseName))
            {
                turnPhases.Add(phaseName);
            }
        }

        public string GetCurrentPhase()
        {
            if (turnPhases.Count == 0) return null;
            return turnPhases[currentPhaseIndex];
        }

        public void NextPhase()
        {
            if (turnPhases.Count == 0) return;
            
            currentPhaseIndex++;
            
            // If wrapped around, advance turn
            if (currentPhaseIndex >= turnPhases.Count)
            {
                currentPhaseIndex = 0;
                currentTurn++;
                OnTurnAdvanced?.Invoke(currentTurn);
                Debug.Log($"[GameKitTurnManager] Turn {currentTurn} started");
            }
            
            string phaseName = GetCurrentPhase();
            OnPhaseChanged?.Invoke(phaseName);
            Debug.Log($"[GameKitTurnManager] Advanced to phase: {phaseName}");
        }

        public void SetPhase(int phaseIndex)
        {
            if (phaseIndex >= 0 && phaseIndex < turnPhases.Count)
            {
                currentPhaseIndex = phaseIndex;
                OnPhaseChanged?.Invoke(GetCurrentPhase());
            }
        }

        public void SetPhase(string phaseName)
        {
            int index = turnPhases.IndexOf(phaseName);
            if (index >= 0)
            {
                currentPhaseIndex = index;
                OnPhaseChanged?.Invoke(phaseName);
            }
        }

        public void ResetTurn()
        {
            currentTurn = 1;
            currentPhaseIndex = 0;
            OnTurnAdvanced?.Invoke(currentTurn);
            if (turnPhases.Count > 0)
            {
                OnPhaseChanged?.Invoke(GetCurrentPhase());
            }
        }

        public List<string> GetAllPhases()
        {
            return new List<string>(turnPhases);
        }

        #region IGameManager Implementation

        private string _managerId;

        /// <summary>
        /// IGameManager: Manager type identifier.
        /// </summary>
        public string ManagerTypeId => "TurnBased";

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
            Debug.Log($"[GameKitTurnManager] Initialized with ID: {managerId}");
        }

        /// <summary>
        /// Resets the manager to its initial state.
        /// IGameManager implementation.
        /// </summary>
        void IGameManager.Reset()
        {
            ResetTurn();
        }

        /// <summary>
        /// Cleans up resources when the manager is no longer needed.
        /// IGameManager implementation.
        /// </summary>
        public void Cleanup()
        {
            turnPhases.Clear();
            currentPhaseIndex = 0;
            currentTurn = 1;
            OnPhaseChanged?.RemoveAllListeners();
            OnTurnAdvanced?.RemoveAllListeners();
            Debug.Log($"[GameKitTurnManager] Cleaned up manager: {_managerId}");
        }

        #endregion

        [Serializable]
        public class PhaseChangedEvent : UnityEngine.Events.UnityEvent<string> { }

        [Serializable]
        public class TurnAdvancedEvent : UnityEngine.Events.UnityEvent<int> { }
    }
}

