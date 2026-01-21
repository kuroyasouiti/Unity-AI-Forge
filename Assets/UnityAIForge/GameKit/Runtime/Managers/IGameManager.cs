namespace UnityAIForge.GameKit
{
    /// <summary>
    /// Interface for game manager implementations.
    /// Provides a unified contract for different manager types (Turn, Resource, State, Event, Realtime).
    /// Part of the Abstract Factory Pattern for manager creation.
    /// </summary>
    public interface IGameManager
    {
        /// <summary>
        /// Unique identifier for this manager type.
        /// </summary>
        string ManagerTypeId { get; }

        /// <summary>
        /// Initializes the manager with the specified ID.
        /// </summary>
        /// <param name="managerId">The unique manager instance ID.</param>
        void Initialize(string managerId);

        /// <summary>
        /// Resets the manager to its initial state.
        /// </summary>
        void Reset();

        /// <summary>
        /// Cleans up resources when the manager is no longer needed.
        /// </summary>
        void Cleanup();
    }
}
