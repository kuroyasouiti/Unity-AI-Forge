using UnityEngine;

#if UNITY_INPUT_SYSTEM_INSTALLED
using UnityEngine.InputSystem;
#endif

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// Input System controller that reads input via Unity's new Input System and sends to GameKitActor.
    /// Requires the Input System package to be installed.
    /// </summary>
    [RequireComponent(typeof(GameKitActor))]
    [AddComponentMenu("SkillForUnity/GameKit/Input System Controller")]
    public class GameKitInputSystemController : MonoBehaviour
    {
#if UNITY_INPUT_SYSTEM_INSTALLED
        [Header("Settings")]
        [Tooltip("Look sensitivity multiplier")]
        [SerializeField] private float lookSensitivity = 2f;
        
        [Tooltip("Movement input deadzone")]
        [SerializeField] private float movementDeadzone = 0.1f;

        private GameKitActor actor;
        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool jumpPressed;

        private void Awake()
        {
            actor = GetComponent<GameKitActor>();
        }

        private void Update()
        {
            if (actor == null) return;

            // Process movement
            if (moveInput.sqrMagnitude > movementDeadzone * movementDeadzone)
            {
                Vector3 moveDirection = ConvertToMoveDirection(moveInput);
                actor.SendMoveInput(moveDirection);
            }

            // Process jump
            if (jumpPressed)
            {
                actor.SendJumpInput();
                jumpPressed = false; // Reset after processing
            }

            // Process look
            if (lookInput.sqrMagnitude > 0)
            {
                actor.SendLookInput(lookInput * lookSensitivity);
            }
        }

        /// <summary>
        /// Converts 2D input to appropriate 3D direction based on behavior profile.
        /// </summary>
        private Vector3 ConvertToMoveDirection(Vector2 input)
        {
            // For 2D games, use x and y
            if (actor.Behavior == GameKitActor.BehaviorProfile.TwoDLinear ||
                actor.Behavior == GameKitActor.BehaviorProfile.TwoDPhysics ||
                actor.Behavior == GameKitActor.BehaviorProfile.TwoDTileGrid)
            {
                return new Vector3(input.x, input.y, 0);
            }

            // For 3D games, use x and z (y is up)
            return new Vector3(input.x, 0, input.y);
        }

        // Input System callback methods
        public void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            lookInput = context.ReadValue<Vector2>();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                jumpPressed = true;
            }
        }

        public void OnAction(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                actor?.SendActionInput("interact");
            }
        }

        public void OnFire(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                actor?.SendActionInput("fire");
            }
        }

        // Alternative: Direct Input Action references (more flexible)
        [Header("Input Actions (Optional)")]
        [SerializeField] private InputAction moveAction;
        [SerializeField] private InputAction lookAction;
        [SerializeField] private InputAction jumpAction;
        [SerializeField] private InputAction actionAction;

        private void OnEnable()
        {
            // Enable actions if using direct references
            moveAction?.Enable();
            lookAction?.Enable();
            jumpAction?.Enable();
            actionAction?.Enable();
        }

        private void OnDisable()
        {
            // Disable actions if using direct references
            moveAction?.Disable();
            lookAction?.Disable();
            jumpAction?.Disable();
            actionAction?.Disable();
        }
#else
        private void Awake()
        {
            Debug.LogError("[GameKitInputSystemController] Input System package is not installed. Please install it via Package Manager or use GameKitSimpleInput instead.");
            enabled = false;
        }
#endif
    }
}

