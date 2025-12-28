using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Timer component: provides timer and cooldown functionality.
    /// Supports single timers, looping timers, and action cooldowns.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/Timer")]
    public class GameKitTimer : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string timerId;

        [Header("Timer Settings")]
        [SerializeField] private float duration = 5f;
        [SerializeField] private bool loop = false;
        [SerializeField] private bool autoStart = false;
        [SerializeField] private bool unscaledTime = false;

        [Header("Events")]
        public UnityEvent OnTimerStart = new UnityEvent();
        public UnityEvent OnTimerComplete = new UnityEvent();
        public UnityEvent OnTimerPaused = new UnityEvent();
        public UnityEvent OnTimerResumed = new UnityEvent();
        public UnityEvent<float> OnTimerTick = new UnityEvent<float>(); // normalized time 0-1

        // State
        private bool isRunning = false;
        private bool isPaused = false;
        private float remainingTime = 0f;
        private float elapsedTime = 0f;

        // Properties
        public string TimerId => timerId;
        public float Duration => duration;
        public float RemainingTime => remainingTime;
        public float ElapsedTime => elapsedTime;
        public float NormalizedTime => duration > 0 ? Mathf.Clamp01(elapsedTime / duration) : 0f;
        public bool IsRunning => isRunning;
        public bool IsPaused => isPaused;
        public bool IsComplete => !isRunning && elapsedTime >= duration;

        private void Awake()
        {
            EnsureEventsInitialized();
        }

        private void Start()
        {
            if (autoStart)
            {
                StartTimer();
            }
        }

        private void Update()
        {
            if (!isRunning || isPaused)
                return;

            float deltaTime = unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsedTime += deltaTime;
            remainingTime = Mathf.Max(0, duration - elapsedTime);

            OnTimerTick?.Invoke(NormalizedTime);

            if (elapsedTime >= duration)
            {
                CompleteTimer();
            }
        }

        /// <summary>
        /// Initialize the timer with specified settings.
        /// </summary>
        public void Initialize(string id, float timerDuration, bool shouldLoop = false, bool useUnscaledTime = false)
        {
            timerId = id;
            duration = timerDuration;
            loop = shouldLoop;
            unscaledTime = useUnscaledTime;
            EnsureEventsInitialized();
        }

        private void EnsureEventsInitialized()
        {
            OnTimerStart ??= new UnityEvent();
            OnTimerComplete ??= new UnityEvent();
            OnTimerPaused ??= new UnityEvent();
            OnTimerResumed ??= new UnityEvent();
            OnTimerTick ??= new UnityEvent<float>();
        }

        /// <summary>
        /// Start the timer from the beginning.
        /// </summary>
        public void StartTimer()
        {
            elapsedTime = 0f;
            remainingTime = duration;
            isRunning = true;
            isPaused = false;

            OnTimerStart?.Invoke();
        }

        /// <summary>
        /// Start the timer with a custom duration.
        /// </summary>
        public void StartTimer(float customDuration)
        {
            duration = customDuration;
            StartTimer();
        }

        /// <summary>
        /// Stop the timer completely.
        /// </summary>
        public void StopTimer()
        {
            isRunning = false;
            isPaused = false;
        }

        /// <summary>
        /// Pause the timer (can be resumed).
        /// </summary>
        public void PauseTimer()
        {
            if (!isRunning || isPaused)
                return;

            isPaused = true;
            OnTimerPaused?.Invoke();
        }

        /// <summary>
        /// Resume a paused timer.
        /// </summary>
        public void ResumeTimer()
        {
            if (!isRunning || !isPaused)
                return;

            isPaused = false;
            OnTimerResumed?.Invoke();
        }

        /// <summary>
        /// Reset the timer to initial state without starting.
        /// </summary>
        public void ResetTimer()
        {
            elapsedTime = 0f;
            remainingTime = duration;
            isRunning = false;
            isPaused = false;
        }

        /// <summary>
        /// Add time to the current timer.
        /// </summary>
        public void AddTime(float time)
        {
            if (!isRunning)
                return;

            remainingTime += time;
            duration += time;
        }

        /// <summary>
        /// Set remaining time directly.
        /// </summary>
        public void SetRemainingTime(float time)
        {
            elapsedTime = duration - time;
            remainingTime = time;
        }

        private void CompleteTimer()
        {
            OnTimerComplete?.Invoke();

            if (loop)
            {
                elapsedTime = 0f;
                remainingTime = duration;
                OnTimerStart?.Invoke();
            }
            else
            {
                isRunning = false;
            }
        }
    }

    /// <summary>
    /// GameKit Cooldown component: manages action cooldowns.
    /// Provides a simple interface for cooldown-gated actions.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/Cooldown")]
    public class GameKitCooldown : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string cooldownId;

        [Header("Cooldown Settings")]
        [SerializeField] private float cooldownDuration = 1f;
        [SerializeField] private bool startReady = true;

        [Header("Events")]
        public UnityEvent OnCooldownStarted = new UnityEvent();
        public UnityEvent OnCooldownComplete = new UnityEvent();
        public UnityEvent<float> OnCooldownTick = new UnityEvent<float>(); // remaining time

        // State
        private float remainingCooldown = 0f;
        private bool isOnCooldown = false;

        // Properties
        public string CooldownId => cooldownId;
        public float CooldownDuration => cooldownDuration;
        public float RemainingCooldown => remainingCooldown;
        public float NormalizedCooldown => cooldownDuration > 0 ? remainingCooldown / cooldownDuration : 0f;
        public bool IsReady => !isOnCooldown;
        public bool IsOnCooldown => isOnCooldown;

        private void Awake()
        {
            EnsureEventsInitialized();

            if (!startReady)
            {
                StartCooldown();
            }
        }

        private void Update()
        {
            if (!isOnCooldown)
                return;

            remainingCooldown -= Time.deltaTime;
            OnCooldownTick?.Invoke(remainingCooldown);

            if (remainingCooldown <= 0)
            {
                CompleteCooldown();
            }
        }

        /// <summary>
        /// Initialize the cooldown with specified settings.
        /// </summary>
        public void Initialize(string id, float duration, bool readyAtStart = true)
        {
            cooldownId = id;
            cooldownDuration = duration;
            startReady = readyAtStart;
            EnsureEventsInitialized();

            if (!readyAtStart)
            {
                StartCooldown();
            }
        }

        private void EnsureEventsInitialized()
        {
            OnCooldownStarted ??= new UnityEvent();
            OnCooldownComplete ??= new UnityEvent();
            OnCooldownTick ??= new UnityEvent<float>();
        }

        /// <summary>
        /// Try to use the cooldown. Returns true if successful, false if on cooldown.
        /// </summary>
        public bool TryUse()
        {
            if (isOnCooldown)
                return false;

            StartCooldown();
            return true;
        }

        /// <summary>
        /// Force start the cooldown.
        /// </summary>
        public void StartCooldown()
        {
            remainingCooldown = cooldownDuration;
            isOnCooldown = true;
            OnCooldownStarted?.Invoke();
        }

        /// <summary>
        /// Force start the cooldown with a custom duration.
        /// </summary>
        public void StartCooldown(float customDuration)
        {
            cooldownDuration = customDuration;
            StartCooldown();
        }

        /// <summary>
        /// Reset the cooldown (make ready immediately).
        /// </summary>
        public void ResetCooldown()
        {
            remainingCooldown = 0f;
            isOnCooldown = false;
        }

        /// <summary>
        /// Reduce the remaining cooldown by a specified amount.
        /// </summary>
        public void ReduceCooldown(float amount)
        {
            remainingCooldown = Mathf.Max(0, remainingCooldown - amount);

            if (remainingCooldown <= 0)
            {
                CompleteCooldown();
            }
        }

        /// <summary>
        /// Set the cooldown duration.
        /// </summary>
        public void SetCooldownDuration(float duration)
        {
            cooldownDuration = Mathf.Max(0, duration);
        }

        private void CompleteCooldown()
        {
            isOnCooldown = false;
            remainingCooldown = 0f;
            OnCooldownComplete?.Invoke();
        }
    }

    /// <summary>
    /// GameKit Cooldown Manager: manages multiple cooldowns on a single GameObject.
    /// Useful for characters with multiple abilities.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/Cooldown Manager")]
    public class GameKitCooldownManager : MonoBehaviour
    {
        [Header("Cooldowns")]
        [SerializeField] private List<CooldownEntry> cooldowns = new List<CooldownEntry>();

        private Dictionary<string, CooldownState> cooldownStates = new Dictionary<string, CooldownState>();

        // Events
        public UnityEvent<string> OnCooldownStarted = new UnityEvent<string>();
        public UnityEvent<string> OnCooldownComplete = new UnityEvent<string>();

        private void Awake()
        {
            InitializeCooldowns();
        }

        private void Update()
        {
            UpdateCooldowns();
        }

        private void InitializeCooldowns()
        {
            cooldownStates.Clear();
            foreach (var cd in cooldowns)
            {
                cooldownStates[cd.id] = new CooldownState
                {
                    duration = cd.duration,
                    remainingTime = cd.startReady ? 0 : cd.duration,
                    isOnCooldown = !cd.startReady
                };
            }
        }

        private void UpdateCooldowns()
        {
            var completedCooldowns = new List<string>();

            foreach (var kvp in cooldownStates)
            {
                if (kvp.Value.isOnCooldown)
                {
                    kvp.Value.remainingTime -= Time.deltaTime;

                    if (kvp.Value.remainingTime <= 0)
                    {
                        kvp.Value.remainingTime = 0;
                        kvp.Value.isOnCooldown = false;
                        completedCooldowns.Add(kvp.Key);
                    }
                }
            }

            foreach (var id in completedCooldowns)
            {
                OnCooldownComplete?.Invoke(id);
            }
        }

        /// <summary>
        /// Add a new cooldown configuration.
        /// </summary>
        public void AddCooldown(string id, float duration, bool startReady = true)
        {
            cooldowns.Add(new CooldownEntry { id = id, duration = duration, startReady = startReady });
            cooldownStates[id] = new CooldownState
            {
                duration = duration,
                remainingTime = startReady ? 0 : duration,
                isOnCooldown = !startReady
            };
        }

        /// <summary>
        /// Try to use a cooldown by ID. Returns true if successful.
        /// </summary>
        public bool TryUse(string id)
        {
            if (!cooldownStates.TryGetValue(id, out var state))
                return false;

            if (state.isOnCooldown)
                return false;

            state.remainingTime = state.duration;
            state.isOnCooldown = true;
            OnCooldownStarted?.Invoke(id);
            return true;
        }

        /// <summary>
        /// Check if a cooldown is ready.
        /// </summary>
        public bool IsReady(string id)
        {
            if (!cooldownStates.TryGetValue(id, out var state))
                return false;

            return !state.isOnCooldown;
        }

        /// <summary>
        /// Get remaining cooldown time.
        /// </summary>
        public float GetRemainingTime(string id)
        {
            if (!cooldownStates.TryGetValue(id, out var state))
                return 0;

            return state.remainingTime;
        }

        /// <summary>
        /// Get normalized cooldown (0 = ready, 1 = just started).
        /// </summary>
        public float GetNormalizedCooldown(string id)
        {
            if (!cooldownStates.TryGetValue(id, out var state))
                return 0;

            return state.duration > 0 ? state.remainingTime / state.duration : 0;
        }

        /// <summary>
        /// Reset a specific cooldown.
        /// </summary>
        public void ResetCooldown(string id)
        {
            if (cooldownStates.TryGetValue(id, out var state))
            {
                state.remainingTime = 0;
                state.isOnCooldown = false;
            }
        }

        /// <summary>
        /// Reset all cooldowns.
        /// </summary>
        public void ResetAllCooldowns()
        {
            foreach (var state in cooldownStates.Values)
            {
                state.remainingTime = 0;
                state.isOnCooldown = false;
            }
        }

        [Serializable]
        public class CooldownEntry
        {
            public string id;
            public float duration = 1f;
            public bool startReady = true;
        }

        private class CooldownState
        {
            public float duration;
            public float remainingTime;
            public bool isOnCooldown;
        }
    }
}
