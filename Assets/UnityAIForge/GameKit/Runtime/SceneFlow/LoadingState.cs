using UnityEngine;

namespace UnityAIForge.GameKit.SceneFlow
{
    /// <summary>
    /// Scene state representing scene loading.
    /// Handles the initial loading phase when a scene is being loaded.
    /// </summary>
    public class LoadingState : ISceneState
    {
        public string StateId => "Loading";

        private float loadingStartTime;
        private bool loadComplete;

        public void Enter(ISceneFlowContext context)
        {
            loadingStartTime = Time.time;
            loadComplete = false;
            Debug.Log($"[LoadingState] Entered loading state for scene: {context.CurrentSceneName}");
        }

        public void Update(ISceneFlowContext context)
        {
            // Loading state typically waits for async scene load to complete
            // The actual loading is handled by GameKitSceneFlow coroutines
        }

        public void Exit(ISceneFlowContext context)
        {
            float loadDuration = Time.time - loadingStartTime;
            Debug.Log($"[LoadingState] Exiting loading state. Duration: {loadDuration:F2}s");
        }

        public bool HandleTrigger(ISceneFlowContext context, string trigger)
        {
            // Ignore triggers during loading
            Debug.LogWarning($"[LoadingState] Trigger '{trigger}' ignored during loading");
            return false;
        }

        /// <summary>
        /// Marks the loading as complete.
        /// </summary>
        public void MarkLoadComplete()
        {
            loadComplete = true;
        }

        /// <summary>
        /// Whether loading is complete.
        /// </summary>
        public bool IsLoadComplete => loadComplete;
    }
}
