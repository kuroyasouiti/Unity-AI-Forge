using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Realtime Manager: manages real-time game flow (timers, time scale, etc.)
    /// Automatically added by GameKitManager when ManagerType.Realtime is selected.
    /// Implements IGameManager for factory-based creation.
    /// </summary>
    [AddComponentMenu("")]
    public class GameKitRealtimeManager : MonoBehaviour, IGameManager
    {
        [Header("Time Management")]
        [SerializeField] private float gameTimeScale = 1f;
        [SerializeField] private float elapsedTime = 0f;
        [SerializeField] private bool isPaused = false;
        
        [Header("Timers")]
        [SerializeField] private List<TimerEntry> timers = new List<TimerEntry>();
        
        [Header("Events")]
        [Tooltip("Invoked when time scale changes (newScale)")]
        public TimeScaleChangedEvent OnTimeScaleChanged = new TimeScaleChangedEvent();
        
        [Tooltip("Invoked when game is paused/resumed (isPaused)")]
        public PauseChangedEvent OnPauseChanged = new PauseChangedEvent();

        public float GameTimeScale => gameTimeScale;
        public float ElapsedTime => elapsedTime;
        public bool IsPaused => isPaused;

        private void Update()
        {
            if (!isPaused)
            {
                elapsedTime += Time.deltaTime;
                UpdateTimers(Time.deltaTime);
            }
        }

        public void SetTimeScale(float scale)
        {
            gameTimeScale = Mathf.Max(0f, scale);
            Time.timeScale = gameTimeScale;
            OnTimeScaleChanged?.Invoke(gameTimeScale);
        }

        public void Pause()
        {
            isPaused = true;
            Time.timeScale = 0f;
            OnPauseChanged?.Invoke(true);
            Debug.Log("[GameKitRealtimeManager] Game paused");
        }

        public void Resume()
        {
            isPaused = false;
            Time.timeScale = gameTimeScale;
            OnPauseChanged?.Invoke(false);
            Debug.Log("[GameKitRealtimeManager] Game resumed");
        }

        public void TogglePause()
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }

        public void AddTimer(string timerName, float duration, System.Action callback)
        {
            timers.Add(new TimerEntry
            {
                name = timerName,
                duration = duration,
                elapsed = 0f,
                callback = callback
            });
        }

        public void RemoveTimer(string timerName)
        {
            timers.RemoveAll(t => t.name == timerName);
        }

        public float GetTimerProgress(string timerName)
        {
            var timer = timers.Find(t => t.name == timerName);
            if (timer != null)
            {
                return Mathf.Clamp01(timer.elapsed / timer.duration);
            }
            return -1f;
        }

        private void UpdateTimers(float deltaTime)
        {
            for (int i = timers.Count - 1; i >= 0; i--)
            {
                var timer = timers[i];
                timer.elapsed += deltaTime;
                
                if (timer.elapsed >= timer.duration)
                {
                    timer.callback?.Invoke();
                    timers.RemoveAt(i);
                }
            }
        }

        public void ResetElapsedTime()
        {
            elapsedTime = 0f;
        }

        #region IGameManager Implementation

        private string _managerId;

        /// <summary>
        /// IGameManager: Manager type identifier.
        /// </summary>
        public string ManagerTypeId => "Realtime";

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
            Debug.Log($"[GameKitRealtimeManager] Initialized with ID: {managerId}");
        }

        /// <summary>
        /// Resets the manager to its initial state.
        /// IGameManager implementation.
        /// </summary>
        void IGameManager.Reset()
        {
            ResetElapsedTime();
            timers.Clear();
            gameTimeScale = 1f;
            isPaused = false;
            Time.timeScale = 1f;
            Debug.Log($"[GameKitRealtimeManager] Reset manager: {_managerId}");
        }

        /// <summary>
        /// Cleans up resources when the manager is no longer needed.
        /// IGameManager implementation.
        /// </summary>
        public void Cleanup()
        {
            timers.Clear();
            Time.timeScale = 1f;
            OnTimeScaleChanged?.RemoveAllListeners();
            OnPauseChanged?.RemoveAllListeners();
            Debug.Log($"[GameKitRealtimeManager] Cleaned up manager: {_managerId}");
        }

        #endregion

        [Serializable]
        public class TimerEntry
        {
            public string name;
            public float duration;
            public float elapsed;
            [NonSerialized]
            public System.Action callback;
        }

        [Serializable]
        public class TimeScaleChangedEvent : UnityEngine.Events.UnityEvent<float> { }

        [Serializable]
        public class PauseChangedEvent : UnityEngine.Events.UnityEvent<bool> { }
    }
}

