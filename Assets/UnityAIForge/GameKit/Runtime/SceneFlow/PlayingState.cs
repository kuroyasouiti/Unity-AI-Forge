using UnityEngine;

namespace UnityAIForge.GameKit.SceneFlow
{
    /// <summary>
    /// Scene state representing normal gameplay.
    /// Handles triggers and allows scene transitions.
    /// </summary>
    public class PlayingState : ISceneState
    {
        public string StateId => "Playing";

        public void Enter(ISceneFlowContext context)
        {
            Debug.Log($"[PlayingState] Entered playing state for scene: {context.CurrentSceneName}");
        }

        public void Update(ISceneFlowContext context)
        {
            // Normal gameplay - no special update logic needed
            // Game logic is handled by other components
        }

        public void Exit(ISceneFlowContext context)
        {
            Debug.Log($"[PlayingState] Exiting playing state");
        }

        public bool HandleTrigger(ISceneFlowContext context, string trigger)
        {
            // In playing state, we process scene transition triggers
            string targetScene = context.GetTargetSceneForTrigger(trigger);

            if (!string.IsNullOrEmpty(targetScene))
            {
                Debug.Log($"[PlayingState] Trigger '{trigger}' handled - transitioning to '{targetScene}'");
                context.RequestSceneLoad(targetScene);
                return true;
            }

            Debug.LogWarning($"[PlayingState] No transition found for trigger '{trigger}' in scene '{context.CurrentSceneName}'");
            return false;
        }
    }
}
