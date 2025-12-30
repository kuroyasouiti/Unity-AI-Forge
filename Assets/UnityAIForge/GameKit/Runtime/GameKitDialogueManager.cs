using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Dialogue Manager: Singleton manager for running dialogues at runtime.
    /// Handles dialogue flow, choices, conditions, and actions.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/Dialogue Manager")]
    public class GameKitDialogueManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float defaultTypingSpeed = 0.05f;
        [SerializeField] private bool pauseGameDuringDialogue = false;

        [Header("Events")]
        public UnityEvent<GameKitDialogueAsset, GameKitDialogueAsset.DialogueNode> OnDialogueStart = new UnityEvent<GameKitDialogueAsset, GameKitDialogueAsset.DialogueNode>();
        public UnityEvent<GameKitDialogueAsset.DialogueNode> OnNodeEnter = new UnityEvent<GameKitDialogueAsset.DialogueNode>();
        public UnityEvent<List<GameKitDialogueAsset.DialogueChoice>> OnChoicesAvailable = new UnityEvent<List<GameKitDialogueAsset.DialogueChoice>>();
        public UnityEvent<GameKitDialogueAsset.DialogueChoice> OnChoiceSelected = new UnityEvent<GameKitDialogueAsset.DialogueChoice>();
        public UnityEvent OnDialogueEnd = new UnityEvent();
        public UnityEvent<GameKitDialogueAsset.DialogueAction> OnActionExecuted = new UnityEvent<GameKitDialogueAsset.DialogueAction>();

        // Singleton
        private static GameKitDialogueManager _instance;
        public static GameKitDialogueManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<GameKitDialogueManager>();
                }
                return _instance;
            }
        }

        // State
        private GameKitDialogueAsset currentDialogue;
        private GameKitDialogueAsset.DialogueNode currentNode;
        private GameObject currentSpeaker;
        private bool isDialogueActive = false;
        private Coroutine autoAdvanceCoroutine;

        // Variable storage for dialogue conditions
        private Dictionary<string, object> dialogueVariables = new Dictionary<string, object>();

        // Registry
        private static readonly Dictionary<string, GameKitDialogueAsset> _dialogueRegistry = new Dictionary<string, GameKitDialogueAsset>();

        // Properties
        public bool IsDialogueActive => isDialogueActive;
        public GameKitDialogueAsset CurrentDialogue => currentDialogue;
        public GameKitDialogueAsset.DialogueNode CurrentNode => currentNode;
        public GameObject CurrentSpeaker => currentSpeaker;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureEventsInitialized();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void EnsureEventsInitialized()
        {
            OnDialogueStart ??= new UnityEvent<GameKitDialogueAsset, GameKitDialogueAsset.DialogueNode>();
            OnNodeEnter ??= new UnityEvent<GameKitDialogueAsset.DialogueNode>();
            OnChoicesAvailable ??= new UnityEvent<List<GameKitDialogueAsset.DialogueChoice>>();
            OnChoiceSelected ??= new UnityEvent<GameKitDialogueAsset.DialogueChoice>();
            OnDialogueEnd ??= new UnityEvent();
            OnActionExecuted ??= new UnityEvent<GameKitDialogueAsset.DialogueAction>();
        }

        /// <summary>
        /// Register a dialogue asset for runtime lookup.
        /// </summary>
        public static void RegisterDialogue(GameKitDialogueAsset dialogue)
        {
            if (dialogue != null && !string.IsNullOrEmpty(dialogue.DialogueId))
            {
                _dialogueRegistry[dialogue.DialogueId] = dialogue;
            }
        }

        /// <summary>
        /// Unregister a dialogue asset.
        /// </summary>
        public static void UnregisterDialogue(string dialogueId)
        {
            _dialogueRegistry.Remove(dialogueId);
        }

        /// <summary>
        /// Find dialogue by ID.
        /// </summary>
        public static GameKitDialogueAsset FindDialogueById(string dialogueId)
        {
            return _dialogueRegistry.TryGetValue(dialogueId, out var dialogue) ? dialogue : null;
        }

        /// <summary>
        /// Start a dialogue.
        /// </summary>
        public bool StartDialogue(GameKitDialogueAsset dialogue, GameObject speaker = null)
        {
            if (dialogue == null || isDialogueActive)
                return false;

            currentDialogue = dialogue;
            currentSpeaker = speaker;
            isDialogueActive = true;

            if (pauseGameDuringDialogue)
            {
                Time.timeScale = 0f;
            }

            var startNode = dialogue.GetStartNode();
            if (startNode == null)
            {
                EndDialogue();
                return false;
            }

            OnDialogueStart?.Invoke(dialogue, startNode);
            GoToNode(startNode);

            return true;
        }

        /// <summary>
        /// Start dialogue by ID.
        /// </summary>
        public bool StartDialogue(string dialogueId, GameObject speaker = null)
        {
            var dialogue = FindDialogueById(dialogueId);
            if (dialogue == null)
            {
                // Try loading from Resources
                dialogue = Resources.Load<GameKitDialogueAsset>($"Dialogues/{dialogueId}");
            }

            return dialogue != null && StartDialogue(dialogue, speaker);
        }

        /// <summary>
        /// Go to a specific node.
        /// </summary>
        public void GoToNode(string nodeId)
        {
            if (currentDialogue == null)
                return;

            var node = currentDialogue.GetNode(nodeId);
            if (node != null)
            {
                GoToNode(node);
            }
            else
            {
                EndDialogue();
            }
        }

        /// <summary>
        /// Go to a specific node.
        /// </summary>
        public void GoToNode(GameKitDialogueAsset.DialogueNode node)
        {
            if (node == null || !isDialogueActive)
            {
                EndDialogue();
                return;
            }

            // Stop auto advance if running
            if (autoAdvanceCoroutine != null)
            {
                StopCoroutine(autoAdvanceCoroutine);
                autoAdvanceCoroutine = null;
            }

            // Check conditions
            if (!CheckConditions(node.conditions))
            {
                // Skip to next node if conditions not met
                if (!string.IsNullOrEmpty(node.nextNodeId))
                {
                    GoToNode(node.nextNodeId);
                }
                else
                {
                    EndDialogue();
                }
                return;
            }

            currentNode = node;
            OnNodeEnter?.Invoke(node);

            // Execute actions
            ExecuteActions(node.actions);

            // Handle node type
            switch (node.type)
            {
                case GameKitDialogueAsset.NodeType.Exit:
                    EndDialogue();
                    break;

                case GameKitDialogueAsset.NodeType.Choice:
                case GameKitDialogueAsset.NodeType.Dialogue:
                    // Filter choices based on conditions
                    var availableChoices = FilterChoicesByConditions(node.choices);
                    if (availableChoices.Count > 0)
                    {
                        OnChoicesAvailable?.Invoke(availableChoices);
                    }
                    else if (currentDialogue.AutoAdvance || node.type == GameKitDialogueAsset.NodeType.Branch)
                    {
                        // Auto advance
                        autoAdvanceCoroutine = StartCoroutine(AutoAdvanceAfterDelay());
                    }
                    break;

                case GameKitDialogueAsset.NodeType.Branch:
                    // Find first choice with matching conditions or default
                    var branchChoices = FilterChoicesByConditions(node.choices);
                    var branchChoice = branchChoices.Find(c => c.isDefault) ?? (branchChoices.Count > 0 ? branchChoices[0] : null);
                    if (branchChoice != null)
                    {
                        SelectChoice(branchChoice);
                    }
                    else if (!string.IsNullOrEmpty(node.nextNodeId))
                    {
                        GoToNode(node.nextNodeId);
                    }
                    else
                    {
                        EndDialogue();
                    }
                    break;

                case GameKitDialogueAsset.NodeType.Action:
                    // Automatically proceed to next
                    if (!string.IsNullOrEmpty(node.nextNodeId))
                    {
                        GoToNode(node.nextNodeId);
                    }
                    else
                    {
                        EndDialogue();
                    }
                    break;
            }
        }

        /// <summary>
        /// Select a choice.
        /// </summary>
        public void SelectChoice(int choiceIndex)
        {
            if (currentNode == null || choiceIndex < 0 || choiceIndex >= currentNode.choices.Count)
                return;

            var availableChoices = FilterChoicesByConditions(currentNode.choices);
            if (choiceIndex < availableChoices.Count)
            {
                SelectChoice(availableChoices[choiceIndex]);
            }
        }

        /// <summary>
        /// Select a choice by ID.
        /// </summary>
        public void SelectChoice(string choiceId)
        {
            if (currentNode == null)
                return;

            var choice = currentNode.choices.Find(c => c.choiceId == choiceId);
            if (choice != null)
            {
                SelectChoice(choice);
            }
        }

        /// <summary>
        /// Select a specific choice.
        /// </summary>
        public void SelectChoice(GameKitDialogueAsset.DialogueChoice choice)
        {
            if (choice == null || !isDialogueActive)
                return;

            OnChoiceSelected?.Invoke(choice);

            // Execute choice actions
            ExecuteActions(choice.actions);

            // Go to next node
            if (!string.IsNullOrEmpty(choice.nextNodeId))
            {
                GoToNode(choice.nextNodeId);
            }
            else if (!string.IsNullOrEmpty(currentNode.nextNodeId))
            {
                GoToNode(currentNode.nextNodeId);
            }
            else
            {
                EndDialogue();
            }
        }

        /// <summary>
        /// Skip to next node (for dialogue without choices).
        /// </summary>
        public void AdvanceDialogue()
        {
            if (!isDialogueActive || currentNode == null)
                return;

            // If there are choices, do nothing
            var availableChoices = FilterChoicesByConditions(currentNode.choices);
            if (availableChoices.Count > 0)
                return;

            // Go to next node
            if (!string.IsNullOrEmpty(currentNode.nextNodeId))
            {
                GoToNode(currentNode.nextNodeId);
            }
            else
            {
                EndDialogue();
            }
        }

        /// <summary>
        /// End the current dialogue.
        /// </summary>
        public void EndDialogue()
        {
            if (!isDialogueActive)
                return;

            if (autoAdvanceCoroutine != null)
            {
                StopCoroutine(autoAdvanceCoroutine);
                autoAdvanceCoroutine = null;
            }

            isDialogueActive = false;
            currentDialogue = null;
            currentNode = null;
            currentSpeaker = null;

            if (pauseGameDuringDialogue)
            {
                Time.timeScale = 1f;
            }

            OnDialogueEnd?.Invoke();
        }

        #region Variables

        /// <summary>
        /// Set a dialogue variable.
        /// </summary>
        public void SetVariable(string name, object value)
        {
            dialogueVariables[name] = value;
        }

        /// <summary>
        /// Get a dialogue variable.
        /// </summary>
        public T GetVariable<T>(string name, T defaultValue = default)
        {
            if (dialogueVariables.TryGetValue(name, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// Check if a variable exists.
        /// </summary>
        public bool HasVariable(string name)
        {
            return dialogueVariables.ContainsKey(name);
        }

        /// <summary>
        /// Clear all variables.
        /// </summary>
        public void ClearVariables()
        {
            dialogueVariables.Clear();
        }

        #endregion

        #region Internal Methods

        private IEnumerator AutoAdvanceAfterDelay()
        {
            yield return new WaitForSecondsRealtime(currentDialogue.AutoAdvanceDelay);
            AdvanceDialogue();
        }

        private List<GameKitDialogueAsset.DialogueChoice> FilterChoicesByConditions(List<GameKitDialogueAsset.DialogueChoice> choices)
        {
            var result = new List<GameKitDialogueAsset.DialogueChoice>();
            foreach (var choice in choices)
            {
                if (CheckConditions(choice.conditions))
                {
                    result.Add(choice);
                }
            }
            return result;
        }

        private bool CheckConditions(List<GameKitDialogueAsset.DialogueCondition> conditions)
        {
            if (conditions == null || conditions.Count == 0)
                return true;

            foreach (var condition in conditions)
            {
                bool result = EvaluateCondition(condition);
                if (condition.negate) result = !result;
                if (!result) return false;
            }

            return true;
        }

        private bool EvaluateCondition(GameKitDialogueAsset.DialogueCondition condition)
        {
            switch (condition.type)
            {
                case GameKitDialogueAsset.ConditionType.Quest:
                    var quest = GameKitQuestManager.Instance?.GetQuest(condition.targetId);
                    if (quest == null) return false;
                    return CompareValues(quest.State.ToString().ToLower(), condition.value?.ToLower(), condition.comparison);

                case GameKitDialogueAsset.ConditionType.Resource:
                    var resourceManager = GameKitResourceManager.FindById(condition.targetId);
                    if (resourceManager == null) return false;
                    var resourceValue = resourceManager.GetResource(condition.propertyName);
                    return CompareValues(resourceValue.ToString(), condition.value, condition.comparison);

                case GameKitDialogueAsset.ConditionType.Inventory:
                    var inventory = GameKitInventory.FindById(condition.targetId);
                    if (inventory == null) return false;
                    var itemCount = inventory.GetItemCount(condition.propertyName);
                    return CompareValues(itemCount.ToString(), condition.value, condition.comparison);

                case GameKitDialogueAsset.ConditionType.Variable:
                    if (!dialogueVariables.TryGetValue(condition.targetId, out var varValue))
                        return false;
                    return CompareValues(varValue?.ToString(), condition.value, condition.comparison);

                case GameKitDialogueAsset.ConditionType.Health:
                    var health = GameKitHealth.FindById(condition.targetId);
                    if (health == null) return false;
                    float healthValue = condition.propertyName?.ToLower() == "percent" ? health.HealthPercent : health.CurrentHealth;
                    return CompareValues(healthValue.ToString(), condition.value, condition.comparison);

                case GameKitDialogueAsset.ConditionType.Custom:
                    // Custom conditions handled by event listeners
                    return true;

                default:
                    return true;
            }
        }

        private bool CompareValues(string actual, string expected, GameKitDialogueAsset.ComparisonOperator op)
        {
            if (actual == null || expected == null)
                return op == GameKitDialogueAsset.ComparisonOperator.NotEquals;

            // Try numeric comparison
            if (float.TryParse(actual, out float actualNum) && float.TryParse(expected, out float expectedNum))
            {
                return op switch
                {
                    GameKitDialogueAsset.ComparisonOperator.Equals => Mathf.Approximately(actualNum, expectedNum),
                    GameKitDialogueAsset.ComparisonOperator.NotEquals => !Mathf.Approximately(actualNum, expectedNum),
                    GameKitDialogueAsset.ComparisonOperator.GreaterThan => actualNum > expectedNum,
                    GameKitDialogueAsset.ComparisonOperator.LessThan => actualNum < expectedNum,
                    GameKitDialogueAsset.ComparisonOperator.GreaterOrEqual => actualNum >= expectedNum,
                    GameKitDialogueAsset.ComparisonOperator.LessOrEqual => actualNum <= expectedNum,
                    GameKitDialogueAsset.ComparisonOperator.Contains => actual.Contains(expected),
                    _ => false
                };
            }

            // String comparison
            return op switch
            {
                GameKitDialogueAsset.ComparisonOperator.Equals => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
                GameKitDialogueAsset.ComparisonOperator.NotEquals => !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
                GameKitDialogueAsset.ComparisonOperator.Contains => actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0,
                _ => false
            };
        }

        private void ExecuteActions(List<GameKitDialogueAsset.DialogueAction> actions)
        {
            if (actions == null) return;

            foreach (var action in actions)
            {
                ExecuteAction(action);
                OnActionExecuted?.Invoke(action);
            }
        }

        private void ExecuteAction(GameKitDialogueAsset.DialogueAction action)
        {
            switch (action.type)
            {
                case GameKitDialogueAsset.ActionType.StartQuest:
                    GameKitQuestManager.Instance?.StartQuest(action.targetId);
                    break;

                case GameKitDialogueAsset.ActionType.CompleteQuest:
                    GameKitQuestManager.Instance?.CompleteQuest(action.targetId);
                    break;

                case GameKitDialogueAsset.ActionType.AddResource:
                    var addResManager = GameKitResourceManager.FindById(action.targetId);
                    if (addResManager != null && float.TryParse(action.value, out float addAmount))
                    {
                        addResManager.AddResource(action.parameter, addAmount);
                    }
                    break;

                case GameKitDialogueAsset.ActionType.RemoveResource:
                    var remResManager = GameKitResourceManager.FindById(action.targetId);
                    if (remResManager != null && float.TryParse(action.value, out float remAmount))
                    {
                        // Use AddResource with negative amount to remove resources
                        remResManager.AddResource(action.parameter, -remAmount);
                    }
                    break;

                case GameKitDialogueAsset.ActionType.AddItem:
                    var addInventory = GameKitInventory.FindById(action.targetId);
                    if (addInventory != null)
                    {
                        int qty = int.TryParse(action.value, out int q) ? q : 1;
                        addInventory.AddItemById(action.parameter, qty);
                    }
                    break;

                case GameKitDialogueAsset.ActionType.RemoveItem:
                    var remInventory = GameKitInventory.FindById(action.targetId);
                    if (remInventory != null)
                    {
                        int qty = int.TryParse(action.value, out int q) ? q : 1;
                        remInventory.RemoveItem(action.parameter, qty);
                    }
                    break;

                case GameKitDialogueAsset.ActionType.SetVariable:
                    SetVariable(action.targetId, action.value);
                    break;

                case GameKitDialogueAsset.ActionType.PlayEffect:
                    GameKitEffectManager.Instance?.PlayEffect(action.targetId, currentSpeaker?.transform.position ?? Vector3.zero);
                    break;

                case GameKitDialogueAsset.ActionType.Custom:
                    // Custom actions handled by event listeners
                    break;
            }
        }

        #endregion

        /// <summary>
        /// Get save data for dialogue state.
        /// </summary>
        public DialogueSaveData GetSaveData()
        {
            return new DialogueSaveData
            {
                variables = new Dictionary<string, object>(dialogueVariables)
            };
        }

        /// <summary>
        /// Load dialogue state from save data.
        /// </summary>
        public void LoadSaveData(DialogueSaveData data)
        {
            if (data == null) return;

            dialogueVariables = new Dictionary<string, object>(data.variables ?? new Dictionary<string, object>());
        }

        [Serializable]
        public class DialogueSaveData
        {
            public Dictionary<string, object> variables;
        }
    }
}
