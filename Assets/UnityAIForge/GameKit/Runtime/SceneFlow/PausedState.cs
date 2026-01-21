using UnityEngine;

namespace UnityAIForge.GameKit.SceneFlow
{
    /// <summary>
    /// Scene state representing a paused game.
    /// Optionally allows certain triggers to be processed while paused.
    /// </summary>
    public class PausedState : ISceneState
    {
        public string StateId => "Paused";

        private float previousTimeScale;
        private bool freezeTime;

        /// <summary>
        /// Creates a paused state.
        /// </summary>
        /// <param name="freezeTime">If true, Time.timeScale will be set to 0 on enter.</param>
        public PausedState(bool freezeTime = true)
        {
            this.freezeTime = freezeTime;
        }

        public void Enter(ISceneFlowContext context)
        {
            Debug.Log($"[PausedState] Entered paused state for scene: {context.CurrentSceneName}");

            if (freezeTime)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
        }

        public void Update(ISceneFlowContext context)
        {
            // Paused state - typically waiting for user input to resume
        }

        public void Exit(ISceneFlowContext context)
        {
            if (freezeTime)
            {
                Time.timeScale = previousTimeScale;
            }

            Debug.Log($"[PausedState] Exiting paused state, restoring timeScale to {previousTimeScale}");
        }

        public bool HandleTrigger(ISceneFlowContext context, string trigger)
        {
            // Allow "resume" and "quit" triggers while paused
            if (trigger == "resume")
            {
                Debug.Log($"[PausedState] Resume trigger received");
                // The caller (GameKitSceneFlow) should handle transitioning to PlayingState
                return true;
            }

            if (trigger == "quit" || trigger == "exit")
            {
                string targetScene = context.GetTargetSceneForTrigger(trigger);
                if (!string.IsNullOrEmpty(targetScene))
                {
                    Debug.Log($"[PausedState] Quit trigger handled - transitioning to '{targetScene}'");
                    context.RequestSceneLoad(targetScene);
                    return true;
                }
            }

            Debug.LogWarning($"[PausedState] Trigger '{trigger}' ignored while paused");
            return false;
        }
    }
}
