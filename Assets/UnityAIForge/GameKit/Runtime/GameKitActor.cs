using System;
using UnityEngine;
using UnityEngine.Events;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Actor component: hub that relays controller input to behavior components.
    /// Acts as a communication bridge between control systems and behavior implementations.
    /// </summary>
    [AddComponentMenu("SkillForUnity/GameKit/Actor")]
    public class GameKitActor : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string actorId;
        
        [Header("Behavior Configuration")]
        [SerializeField] private BehaviorProfile behaviorProfile;
        [SerializeField] private ControlMode controlMode;
        
        [Header("Input Events")]
        [Tooltip("Invoked when movement input is received (Vector2 for 2D, Vector3 for 3D)")]
        public UnityEvent<Vector3> OnMoveInput = new UnityEvent<Vector3>();
        
        [Tooltip("Invoked when jump input is received")]
        public UnityEvent OnJumpInput = new UnityEvent();
        
        [Tooltip("Invoked when action input is received (e.g., attack, interact)")]
        public UnityEvent<string> OnActionInput = new UnityEvent<string>();
        
        [Tooltip("Invoked when look/rotation input is received")]
        public UnityEvent<Vector2> OnLookInput = new UnityEvent<Vector2>();

        public string ActorId => actorId;
        public BehaviorProfile Behavior => behaviorProfile;
        public ControlMode Control => controlMode;

        private void Awake()
        {
            // Ensure events are initialized (defensive programming)
            EnsureEventsInitialized();
        }

        public void Initialize(string id, BehaviorProfile behavior, ControlMode control)
        {
            actorId = id;
            behaviorProfile = behavior;
            controlMode = control;
            
            // Ensure events are initialized for editor tests
            EnsureEventsInitialized();
        }

        private void EnsureEventsInitialized()
        {
            OnMoveInput ??= new UnityEvent<Vector3>();
            OnJumpInput ??= new UnityEvent();
            OnActionInput ??= new UnityEvent<string>();
            OnLookInput ??= new UnityEvent<Vector2>();
        }

        /// <summary>
        /// Sends movement input to behavior components.
        /// </summary>
        public void SendMoveInput(Vector3 direction)
        {
            OnMoveInput?.Invoke(direction);
        }

        /// <summary>
        /// Sends jump input to behavior components.
        /// </summary>
        public void SendJumpInput()
        {
            OnJumpInput?.Invoke();
        }

        /// <summary>
        /// Sends action input to behavior components.
        /// </summary>
        public void SendActionInput(string actionName)
        {
            OnActionInput?.Invoke(actionName);
        }

        /// <summary>
        /// Sends look/rotation input to behavior components.
        /// </summary>
        public void SendLookInput(Vector2 lookDelta)
        {
            OnLookInput?.Invoke(lookDelta);
        }

        /// <summary>
        /// Gets the movement strategy component attached to this actor.
        /// Returns null if no movement strategy is attached.
        /// </summary>
        /// <returns>The IMovementStrategy component, or null if not found.</returns>
        public IMovementStrategy GetMovementStrategy()
        {
            return GetComponent<IMovementStrategy>();
        }

        /// <summary>
        /// Gets a specific type of movement strategy component.
        /// </summary>
        /// <typeparam name="T">The specific movement strategy type (must implement IMovementStrategy).</typeparam>
        /// <returns>The movement strategy component of type T, or null if not found.</returns>
        public T GetMovementStrategy<T>() where T : class, IMovementStrategy
        {
            return GetComponent<T>();
        }

        public enum BehaviorProfile
        {
            TwoDLinear,
            TwoDPhysics,
            TwoDTileGrid,
            GraphNode,              // Node-based graph movement (2D/3D agnostic)
            SplineMovement,         // Spline/rail-based movement (ideal for 2.5D games, rail shooters, side-scrollers)
            ThreeDCharacterController,
            ThreeDPhysics,
            ThreeDNavMesh
        }

        public enum ControlMode
        {
            DirectController,
            AIAutonomous,
            UICommand,
            ScriptTriggerOnly
        }
    }
}

