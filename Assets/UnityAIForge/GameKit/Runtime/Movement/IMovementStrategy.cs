using UnityEngine;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// Interface for movement strategy implementations.
    /// Provides a unified contract for different movement behaviors (tile grid, spline, graph node).
    /// Part of the Strategy Pattern for movement systems.
    /// </summary>
    public interface IMovementStrategy
    {
        /// <summary>
        /// Whether the entity is currently moving.
        /// </summary>
        bool IsMoving { get; }

        /// <summary>
        /// Movement speed property. Implementation varies by strategy:
        /// - TileGridMovement: time to move between tiles
        /// - SplineMovement: units per second
        /// - GraphNodeMovement: time to move between nodes
        /// </summary>
        float MoveSpeed { get; set; }

        /// <summary>
        /// Handles movement input from a controller or AI.
        /// </summary>
        /// <param name="direction">Direction vector for movement input.</param>
        void HandleMoveInput(Vector3 direction);

        /// <summary>
        /// Immediately stops any ongoing movement.
        /// </summary>
        void StopMovement();

        /// <summary>
        /// Instantly teleports to a position.
        /// </summary>
        /// <param name="position">Target world position.</param>
        void TeleportTo(Vector3 position);

        /// <summary>
        /// Initializes the movement strategy. Called when the component is set up.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Cleans up resources when the movement strategy is no longer needed.
        /// </summary>
        void Cleanup();
    }
}
