using System.Collections.Generic;

namespace UnityAIForge.GameKit.SceneFlow
{
    /// <summary>
    /// Interface for scene flow states.
    /// Provides a unified contract for different scene states (Loading, Playing, Paused, Transitioning).
    /// Part of the State Pattern for scene management.
    /// </summary>
    public interface ISceneState
    {
        /// <summary>
        /// Unique identifier for this state.
        /// </summary>
        string StateId { get; }

        /// <summary>
        /// Called when entering this state.
        /// </summary>
        /// <param name="context">The scene flow context.</param>
        void Enter(ISceneFlowContext context);

        /// <summary>
        /// Called every frame while in this state.
        /// </summary>
        /// <param name="context">The scene flow context.</param>
        void Update(ISceneFlowContext context);

        /// <summary>
        /// Called when exiting this state.
        /// </summary>
        /// <param name="context">The scene flow context.</param>
        void Exit(ISceneFlowContext context);

        /// <summary>
        /// Handles a trigger event while in this state.
        /// </summary>
        /// <param name="context">The scene flow context.</param>
        /// <param name="trigger">The trigger name.</param>
        /// <returns>True if the trigger was handled, false otherwise.</returns>
        bool HandleTrigger(ISceneFlowContext context, string trigger);
    }

    /// <summary>
    /// Interface for the scene flow context.
    /// Provides access to scene flow operations and state management.
    /// </summary>
    public interface ISceneFlowContext
    {
        /// <summary>
        /// The name of the currently active scene.
        /// </summary>
        string CurrentSceneName { get; }

        /// <summary>
        /// List of currently loaded shared scenes.
        /// </summary>
        List<string> LoadedSharedScenes { get; }

        /// <summary>
        /// Sets a new state for the scene flow.
        /// </summary>
        /// <param name="newState">The new state to transition to.</param>
        void SetState(ISceneState newState);

        /// <summary>
        /// Requests a scene to be loaded.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        void RequestSceneLoad(string sceneName);

        /// <summary>
        /// Gets a list of available triggers from the current scene.
        /// </summary>
        /// <returns>List of trigger names.</returns>
        List<string> GetAvailableTriggersForState();

        /// <summary>
        /// Gets the target scene for a given trigger from the current scene.
        /// </summary>
        /// <param name="trigger">The trigger name.</param>
        /// <returns>The target scene name, or null if not found.</returns>
        string GetTargetSceneForTrigger(string trigger);
    }
}
