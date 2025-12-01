using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit UI Command Hub: bridges UI controls to GameKitActor and GameKitManager.
    /// Acts as a central hub for translating UI interactions into game commands.
    /// Supports both Actor commands (movement, actions) and Manager commands (resources, states).
    /// </summary>
    [AddComponentMenu("SkillForUnity/GameKit/UI Command Hub")]
    public class GameKitUICommand : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string panelId;
        
        [Header("Target Type")]
        [Tooltip("What type of target this command hub controls")]
        [SerializeField] private TargetType targetType = TargetType.Actor;
        
        [Header("Target Actor")]
        [Tooltip("Reference to the target actor (when targetType is Actor)")]
        [SerializeField] private GameKitActor targetActor;
        
        [Tooltip("Target actor ID (fallback if actor reference is not set)")]
        [SerializeField] private string targetActorId;
        
        [Header("Target Manager")]
        [Tooltip("Reference to the target manager (when targetType is Manager)")]
        [SerializeField] private GameKitManager targetManager;
        
        [Tooltip("Target manager ID (fallback if manager reference is not set)")]
        [SerializeField] private string targetManagerId;
        
        [Header("Command Bindings")]
        [SerializeField] private List<UICommandBinding> commandBindings = new List<UICommandBinding>();

        [Header("Settings")]
        [Tooltip("Cache target reference on Start for better performance")]
        [SerializeField] private bool cacheTargetReference = true;
        
        [Tooltip("Log command execution for debugging")]
        [SerializeField] private bool logCommands = false;

        private Dictionary<string, UICommandBinding> bindingLookup;
        private bool isInitialized = false;

        public string PanelId => panelId;
        public TargetType Type => targetType;
        public string TargetActorId => targetActorId;
        public GameKitActor TargetActor => targetActor;
        public string TargetManagerId => targetManagerId;
        public GameKitManager TargetManager => targetManager;
        
        /// <summary>
        /// Target type for UI commands
        /// </summary>
        public enum TargetType
        {
            Actor,      // Commands target GameKitActor
            Manager     // Commands target GameKitManager
        }

        /// <summary>
        /// Command types that map to GameKitActor's UnityEvents or GameKitManager's methods
        /// </summary>
        public enum CommandType
        {
            // Actor Commands
            Move,           // Maps to OnMoveInput (Vector3)
            Jump,           // Maps to OnJumpInput (void)
            Action,         // Maps to OnActionInput (string)
            Look,           // Maps to OnLookInput (Vector2)
            Custom,         // Custom command via SendMessage
            
            // Manager Commands - Resource
            AddResource,    // Add resource amount
            SetResource,    // Set resource to specific amount
            ConsumeResource,// Try to consume resource
            
            // Manager Commands - State
            ChangeState,    // Change state manager state
            
            // Manager Commands - Turn
            NextTurn,       // Advance to next turn phase
            
            // Manager Commands - Scene
            TriggerScene    // Trigger scene transition
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (isInitialized)
                return;

            // Build lookup dictionary for faster command execution
            bindingLookup = new Dictionary<string, UICommandBinding>();
            foreach (var binding in commandBindings)
            {
                if (!string.IsNullOrEmpty(binding.commandName))
                {
                    bindingLookup[binding.commandName] = binding;
                }
            }

            // Cache target reference if enabled
            if (cacheTargetReference)
            {
                if (targetType == TargetType.Actor && targetActor == null && !string.IsNullOrEmpty(targetActorId))
                {
                    targetActor = FindActorById(targetActorId);
                }
                else if (targetType == TargetType.Manager && targetManager == null && !string.IsNullOrEmpty(targetManagerId))
                {
                    targetManager = FindManagerById(targetManagerId);
                }
            }

            isInitialized = true;
        }

        /// <summary>
        /// Initialize the UI command hub with panel ID and target actor.
        /// </summary>
        public void Initialize(string id, string actorId)
        {
            panelId = id;
            targetActorId = actorId;
            targetActor = null; // Clear cached reference
            isInitialized = false;
            Initialize();
        }

        /// <summary>
        /// Register a UI button with a command binding.
        /// </summary>
        public void RegisterButton(string commandName, Button button, CommandType commandType = CommandType.Action, string commandParam = null)
        {
            if (!isInitialized)
                Initialize();

            var binding = new UICommandBinding
            {
                commandName = commandName,
                commandType = commandType,
                button = button,
                commandParameter = commandParam
            };

            commandBindings.Add(binding);
            bindingLookup[commandName] = binding;

            // Wire up button click to command execution
            button.onClick.AddListener(() => ExecuteCommand(commandName));
        }

        /// <summary>
        /// Register a UI button with direction (for movement commands).
        /// </summary>
        public void RegisterDirectionalButton(string commandName, Button button, Vector3 direction)
        {
            if (!isInitialized)
                Initialize();

            var binding = new UICommandBinding
            {
                commandName = commandName,
                commandType = CommandType.Move,
                button = button,
                moveDirection = direction
            };

            commandBindings.Add(binding);
            bindingLookup[commandName] = binding;

            button.onClick.AddListener(() => ExecuteCommand(commandName));
        }

        /// <summary>
        /// Execute a command by name.
        /// </summary>
        public void ExecuteCommand(string commandName)
        {
            if (!isInitialized)
                Initialize();

            // Get binding
            if (!bindingLookup.TryGetValue(commandName, out var binding))
            {
                Debug.LogWarning($"[GameKitUICommand] Command '{commandName}' not found in bindings");
                return;
            }

            // Execute based on target type
            if (targetType == TargetType.Actor)
            {
                ExecuteActorCommand(binding, commandName);
            }
            else if (targetType == TargetType.Manager)
            {
                ExecuteManagerCommand(binding, commandName);
            }
        }

        private void ExecuteActorCommand(UICommandBinding binding, string commandName)
        {
            // Get or find target actor
            var actor = GetTargetActor();
            if (actor == null)
            {
                Debug.LogWarning($"[GameKitUICommand] Target actor not found for command '{commandName}'");
                return;
            }

            // Execute command based on type
            switch (binding.commandType)
            {
                case CommandType.Move:
                    actor.SendMoveInput(binding.moveDirection);
                    if (logCommands)
                        Debug.Log($"[GameKitUICommand] Move command: {binding.moveDirection}");
                    break;

                case CommandType.Jump:
                    actor.SendJumpInput();
                    if (logCommands)
                        Debug.Log($"[GameKitUICommand] Jump command");
                    break;

                case CommandType.Action:
                    string actionParam = binding.commandParameter ?? commandName;
                    actor.SendActionInput(actionParam);
                    if (logCommands)
                        Debug.Log($"[GameKitUICommand] Action command: {actionParam}");
                    break;

                case CommandType.Look:
                    actor.SendLookInput(binding.lookDirection);
                    if (logCommands)
                        Debug.Log($"[GameKitUICommand] Look command: {binding.lookDirection}");
                    break;

                case CommandType.Custom:
                    // Send custom message for backward compatibility
                    actor.gameObject.SendMessage($"OnCommand_{commandName}", SendMessageOptions.DontRequireReceiver);
                    if (logCommands)
                        Debug.Log($"[GameKitUICommand] Custom command: {commandName}");
                    break;
                    
                default:
                    Debug.LogWarning($"[GameKitUICommand] Command type '{binding.commandType}' is not valid for Actor target");
                    break;
            }
        }

        private void ExecuteManagerCommand(UICommandBinding binding, string commandName)
        {
            // Get or find target manager
            var manager = GetTargetManager();
            if (manager == null)
            {
                Debug.LogWarning($"[GameKitUICommand] Target manager not found for command '{commandName}'");
                return;
            }

            // Execute command based on type
            switch (binding.commandType)
            {
                case CommandType.AddResource:
                    {
                        string resourceName = binding.commandParameter;
                        float amount = binding.resourceAmount;
                        manager.AddResource(resourceName, amount);
                        if (logCommands)
                            Debug.Log($"[GameKitUICommand] AddResource: {resourceName} +{amount}");
                    }
                    break;

                case CommandType.SetResource:
                    {
                        string resourceName = binding.commandParameter;
                        float amount = binding.resourceAmount;
                        manager.SetResource(resourceName, amount);
                        if (logCommands)
                            Debug.Log($"[GameKitUICommand] SetResource: {resourceName} = {amount}");
                    }
                    break;

                case CommandType.ConsumeResource:
                    {
                        string resourceName = binding.commandParameter;
                        float amount = binding.resourceAmount;
                        bool success = manager.ConsumeResource(resourceName, amount);
                        if (logCommands)
                            Debug.Log($"[GameKitUICommand] ConsumeResource: {resourceName} -{amount} (success: {success})");
                    }
                    break;

                case CommandType.ChangeState:
                    {
                        string stateName = binding.commandParameter ?? commandName;
                        manager.ChangeState(stateName);
                        if (logCommands)
                            Debug.Log($"[GameKitUICommand] ChangeState: {stateName}");
                    }
                    break;

                case CommandType.NextTurn:
                    {
                        manager.NextPhase();
                        if (logCommands)
                            Debug.Log($"[GameKitUICommand] NextTurn");
                    }
                    break;

                case CommandType.TriggerScene:
                    {
                        string triggerName = binding.commandParameter ?? commandName;
                        // Assuming there's a GameKitSceneFlow instance
                        GameKitSceneFlow.Transition(triggerName);
                        if (logCommands)
                            Debug.Log($"[GameKitUICommand] TriggerScene: {triggerName}");
                    }
                    break;

                case CommandType.Custom:
                    // Send custom message to manager
                    manager.gameObject.SendMessage($"OnCommand_{commandName}", SendMessageOptions.DontRequireReceiver);
                    if (logCommands)
                        Debug.Log($"[GameKitUICommand] Custom command: {commandName}");
                    break;
                    
                default:
                    Debug.LogWarning($"[GameKitUICommand] Command type '{binding.commandType}' is not valid for Manager target");
                    break;
            }
        }

        /// <summary>
        /// Execute a move command with direction.
        /// </summary>
        public void ExecuteMoveCommand(Vector3 direction)
        {
            var actor = GetTargetActor();
            if (actor != null)
            {
                actor.SendMoveInput(direction);
                if (logCommands)
                    Debug.Log($"[GameKitUICommand] Move: {direction}");
            }
        }

        /// <summary>
        /// Execute a jump command.
        /// </summary>
        public void ExecuteJumpCommand()
        {
            var actor = GetTargetActor();
            if (actor != null)
            {
                actor.SendJumpInput();
                if (logCommands)
                    Debug.Log($"[GameKitUICommand] Jump");
            }
        }

        /// <summary>
        /// Execute an action command with parameter.
        /// </summary>
        public void ExecuteActionCommand(string actionName)
        {
            var actor = GetTargetActor();
            if (actor != null)
            {
                actor.SendActionInput(actionName);
                if (logCommands)
                    Debug.Log($"[GameKitUICommand] Action: {actionName}");
            }
        }

        /// <summary>
        /// Execute a look command with direction.
        /// </summary>
        public void ExecuteLookCommand(Vector2 direction)
        {
            var actor = GetTargetActor();
            if (actor != null)
            {
                actor.SendLookInput(direction);
                if (logCommands)
                    Debug.Log($"[GameKitUICommand] Look: {direction}");
            }
        }

        /// <summary>
        /// Set target actor by reference.
        /// </summary>
        public void SetTargetActor(GameKitActor actor)
        {
            targetActor = actor;
            if (actor != null)
            {
                targetActorId = actor.ActorId;
            }
        }

        /// <summary>
        /// Set target actor by ID.
        /// </summary>
        public void SetTargetActor(string actorId)
        {
            targetActorId = actorId;
            if (cacheTargetReference)
            {
                targetActor = FindActorById(actorId);
            }
            else
            {
                targetActor = null; // Force lookup on next command
            }
        }

        /// <summary>
        /// Clear all command bindings.
        /// </summary>
        public void ClearBindings()
        {
            // Remove all button listeners
            foreach (var binding in commandBindings)
            {
                if (binding.button != null)
                {
                    binding.button.onClick.RemoveAllListeners();
                }
            }

            commandBindings.Clear();
            bindingLookup?.Clear();
        }

        private GameKitActor GetTargetActor()
        {
            // Use cached reference if available
            if (targetActor != null)
                return targetActor;

            // Find by ID if not cached
            if (!string.IsNullOrEmpty(targetActorId))
            {
                targetActor = FindActorById(targetActorId);
                return targetActor;
            }

            return null;
        }

        private GameKitActor FindActorById(string actorId)
        {
            if (string.IsNullOrEmpty(actorId))
                return null;

            var actors = FindObjectsByType<GameKitActor>(FindObjectsSortMode.None);
            foreach (var actor in actors)
            {
                if (actor.ActorId == actorId)
                {
                    return actor;
                }
            }

            return null;
        }

        private GameKitManager GetTargetManager()
        {
            // Use cached reference if available
            if (targetManager != null)
                return targetManager;

            // Find by ID if not cached
            if (!string.IsNullOrEmpty(targetManagerId))
            {
                targetManager = FindManagerById(targetManagerId);
                return targetManager;
            }

            return null;
        }

        private GameKitManager FindManagerById(string managerId)
        {
            if (string.IsNullOrEmpty(managerId))
                return null;

            var managers = FindObjectsByType<GameKitManager>(FindObjectsSortMode.None);
            foreach (var manager in managers)
            {
                if (manager.ManagerId == managerId)
                {
                    return manager;
                }
            }

            return null;
        }

        /// <summary>
        /// Set target manager by reference.
        /// </summary>
        public void SetTargetManager(GameKitManager manager)
        {
            targetManager = manager;
            if (manager != null)
            {
                targetManagerId = manager.ManagerId;
            }
        }

        /// <summary>
        /// Set target manager by ID.
        /// </summary>
        public void SetTargetManager(string managerId)
        {
            targetManagerId = managerId;
            if (cacheTargetReference)
            {
                targetManager = FindManagerById(managerId);
            }
            else
            {
                targetManager = null; // Force lookup on next command
            }
        }

        /// <summary>
        /// Get all registered command names.
        /// </summary>
        public List<string> GetCommandNames()
        {
            var names = new List<string>();
            foreach (var binding in commandBindings)
            {
                if (!string.IsNullOrEmpty(binding.commandName))
                {
                    names.Add(binding.commandName);
                }
            }
            return names;
        }

        /// <summary>
        /// Check if a command is registered.
        /// </summary>
        public bool HasCommand(string commandName)
        {
            if (!isInitialized)
                Initialize();
            return bindingLookup.ContainsKey(commandName);
        }

        [Serializable]
        public class UICommandBinding
        {
            [Tooltip("Unique command identifier")]
            public string commandName;
            
            [Tooltip("Type of command (maps to GameKitActor events or GameKitManager methods)")]
            public CommandType commandType = CommandType.Action;
            
            [Tooltip("UI button that triggers this command")]
            public Button button;
            
            [Tooltip("Direction for Move commands")]
            public Vector3 moveDirection = Vector3.zero;
            
            [Tooltip("Direction for Look commands")]
            public Vector2 lookDirection = Vector2.zero;
            
            [Tooltip("Parameter string for Action, Resource, State, or Scene commands")]
            public string commandParameter;
            
            [Tooltip("Amount for resource commands (AddResource, SetResource, ConsumeResource)")]
            public float resourceAmount = 0f;
        }
    }
}

