using UnityEngine;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// Simple input controller that reads keyboard/gamepad input and sends to GameKitActor.
    /// Uses Unity's legacy Input system for maximum compatibility.
    /// </summary>
    [RequireComponent(typeof(GameKitActor))]
    [AddComponentMenu("SkillForUnity/GameKit/Simple Input")]
    public class GameKitSimpleInput : MonoBehaviour
    {
        [Header("Input Settings")]
        [Tooltip("Enable keyboard input")]
        [SerializeField] private bool enableKeyboard = true;
        
        [Tooltip("Enable gamepad input")]
        [SerializeField] private bool enableGamepad = true;
        
        [Header("Key Bindings")]
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode actionKey = KeyCode.E;
        
        [Header("Sensitivity")]
        [SerializeField] private float movementDeadzone = 0.1f;
        [SerializeField] private float lookSensitivity = 2f;

        private GameKitActor actor;

        private void Awake()
        {
            actor = GetComponent<GameKitActor>();
        }

        private void Update()
        {
            if (actor == null) return;

            // Movement input
            Vector3 moveInput = GetMovementInput();
            if (moveInput.sqrMagnitude > movementDeadzone * movementDeadzone)
            {
                actor.SendMoveInput(moveInput);
            }

            // Jump input
            if (enableKeyboard && Input.GetKeyDown(jumpKey))
            {
                actor.SendJumpInput();
            }

            // Action input
            if (enableKeyboard && Input.GetKeyDown(actionKey))
            {
                actor.SendActionInput("interact");
            }

            // Look input (mouse or right stick)
            Vector2 lookInput = GetLookInput();
            if (lookInput.sqrMagnitude > 0)
            {
                actor.SendLookInput(lookInput);
            }
        }

        private Vector3 GetMovementInput()
        {
            Vector3 input = Vector3.zero;

            if (enableKeyboard)
            {
                // WASD or Arrow keys
                input.x = Input.GetAxis("Horizontal");
                input.y = 0;
                input.z = Input.GetAxis("Vertical");
            }
            else if (enableGamepad)
            {
                // Left stick
                input.x = Input.GetAxis("Horizontal");
                input.y = 0;
                input.z = Input.GetAxis("Vertical");
            }

            // For 2D games, we use x and y instead
            if (actor.Behavior == GameKitActor.BehaviorProfile.TwoDLinear ||
                actor.Behavior == GameKitActor.BehaviorProfile.TwoDPhysics ||
                actor.Behavior == GameKitActor.BehaviorProfile.TwoDTileGrid)
            {
                input = new Vector3(input.x, input.z, 0);
            }

            return input.normalized * Mathf.Min(input.magnitude, 1f);
        }

        private Vector2 GetLookInput()
        {
            Vector2 look = Vector2.zero;

            if (enableKeyboard)
            {
                // Mouse delta
                look.x = Input.GetAxis("Mouse X") * lookSensitivity;
                look.y = Input.GetAxis("Mouse Y") * lookSensitivity;
            }
            else if (enableGamepad)
            {
                // Right stick (if available)
                look.x = Input.GetAxis("RightStickHorizontal") * lookSensitivity;
                look.y = Input.GetAxis("RightStickVertical") * lookSensitivity;
            }

            return look;
        }
    }
}

