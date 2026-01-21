using UnityEngine;

namespace UnityAIForge.GameKit.SceneFlow
{
    /// <summary>
    /// Scene state representing an active scene transition.
    /// Blocks triggers and waits for the transition to complete.
    /// </summary>
    public class TransitioningState : ISceneState
    {
        public string StateId => "Transitioning";

        private string fromScene;
        private string toScene;
        private float transitionStartTime;
        private bool transitionComplete;

        /// <summary>
        /// Creates a transitioning state.
        /// </summary>
        /// <param name="fromScene">The scene being transitioned from.</param>
        /// <param name="toScene">The scene being transitioned to.</param>
        public TransitioningState(string fromScene, string toScene)
        {
            this.fromScene = fromScene;
            this.toScene = toScene;
            this.transitionComplete = false;
        }

        public void Enter(ISceneFlowContext context)
        {
            transitionStartTime = Time.time;
            transitionComplete = false;
            Debug.Log($"[TransitioningState] Starting transition from '{fromScene}' to '{toScene}'");
        }

        public void Update(ISceneFlowContext context)
        {
            // Transition state - waiting for scene load to complete
            // The actual transition is handled by GameKitSceneFlow coroutines
        }

        public void Exit(ISceneFlowContext context)
        {
            float transitionDuration = Time.time - transitionStartTime;
            Debug.Log($"[TransitioningState] Transition complete. Duration: {transitionDuration:F2}s");
        }

        public bool HandleTrigger(ISceneFlowContext context, string trigger)
        {
            // Ignore all triggers during transition
            Debug.LogWarning($"[TransitioningState] Trigger '{trigger}' ignored during scene transition");
            return false;
        }

        /// <summary>
        /// Marks the transition as complete.
        /// </summary>
        public void MarkTransitionComplete()
        {
            transitionComplete = true;
        }

        /// <summary>
        /// Whether the transition is complete.
        /// </summary>
        public bool IsTransitionComplete => transitionComplete;

        /// <summary>
        /// The scene being transitioned from.
        /// </summary>
        public string FromScene => fromScene;

        /// <summary>
        /// The scene being transitioned to.
        /// </summary>
        public string ToScene => toScene;

        /// <summary>
        /// Time elapsed since the transition started.
        /// </summary>
        public float ElapsedTime => Time.time - transitionStartTime;
    }
}
