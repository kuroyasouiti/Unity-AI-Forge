using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Dialogue Asset: ScriptableObject that defines dialogue data for NPCs and conversations.
    /// Supports branching dialogues with choices, conditions, and actions.
    /// </summary>
    [CreateAssetMenu(fileName = "Dialogue", menuName = "GameKit/Dialogue")]
    public class GameKitDialogueAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string dialogueId;

        [Header("Nodes")]
        [SerializeField] private List<DialogueNode> nodes = new List<DialogueNode>();

        [Header("Settings")]
        [SerializeField] private string startNodeId = "start";
        [SerializeField] private bool autoAdvance = false;
        [SerializeField] private float autoAdvanceDelay = 2f;

        // Properties
        public string DialogueId => dialogueId;
        public IReadOnlyList<DialogueNode> Nodes => nodes.AsReadOnly();
        public string StartNodeId => startNodeId;
        public bool AutoAdvance => autoAdvance;
        public float AutoAdvanceDelay => autoAdvanceDelay;

        /// <summary>
        /// Get node by ID.
        /// </summary>
        public DialogueNode GetNode(string nodeId)
        {
            return nodes.Find(n => n.nodeId == nodeId);
        }

        /// <summary>
        /// Get the start node.
        /// </summary>
        public DialogueNode GetStartNode()
        {
            return GetNode(startNodeId) ?? (nodes.Count > 0 ? nodes[0] : null);
        }

        /// <summary>
        /// Add a node to the dialogue.
        /// </summary>
        public void AddNode(DialogueNode node)
        {
            if (node == null) return;

            // Ensure unique ID
            if (string.IsNullOrEmpty(node.nodeId))
            {
                node.nodeId = $"node_{Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            nodes.Add(node);
        }

        /// <summary>
        /// Remove a node from the dialogue.
        /// </summary>
        public bool RemoveNode(string nodeId)
        {
            return nodes.RemoveAll(n => n.nodeId == nodeId) > 0;
        }

        /// <summary>
        /// Update an existing node.
        /// </summary>
        public bool UpdateNode(DialogueNode updatedNode)
        {
            if (updatedNode == null) return false;

            var index = nodes.FindIndex(n => n.nodeId == updatedNode.nodeId);
            if (index < 0) return false;

            nodes[index] = updatedNode;
            return true;
        }

        /// <summary>
        /// Initialize the dialogue asset.
        /// </summary>
        public void Initialize(string id)
        {
            dialogueId = id;
            nodes.Clear();
        }

        #region Serializable Types

        [Serializable]
        public class DialogueNode
        {
            [Header("Identity")]
            public string nodeId;
            public NodeType type = NodeType.Dialogue;

            [Header("Content")]
            public string speaker;
            [TextArea(3, 10)]
            public string text;
            public string portrait;

            [Header("Choices")]
            public List<DialogueChoice> choices = new List<DialogueChoice>();

            [Header("Flow")]
            public string nextNodeId;

            [Header("Conditions")]
            public List<DialogueCondition> conditions = new List<DialogueCondition>();

            [Header("Actions")]
            public List<DialogueAction> actions = new List<DialogueAction>();

            [Header("Audio")]
            public string voiceClipPath;
            public string soundEffectPath;
        }

        [Serializable]
        public class DialogueChoice
        {
            public string choiceId;
            public string text;
            public string nextNodeId;
            public List<DialogueCondition> conditions = new List<DialogueCondition>();
            public List<DialogueAction> actions = new List<DialogueAction>();
            public bool isDefault = false;
        }

        [Serializable]
        public class DialogueCondition
        {
            public ConditionType type;
            public string targetId;
            public string propertyName;
            public ComparisonOperator comparison = ComparisonOperator.Equals;
            public string value;
            public bool negate = false;
        }

        [Serializable]
        public class DialogueAction
        {
            public ActionType type;
            public string targetId;
            public string parameter;
            public string value;
        }

        public enum NodeType
        {
            Dialogue,
            Choice,
            Branch,
            Action,
            Exit
        }

        public enum ConditionType
        {
            Quest,
            Resource,
            Inventory,
            Variable,
            Health,
            Custom
        }

        public enum ComparisonOperator
        {
            Equals,
            NotEquals,
            GreaterThan,
            LessThan,
            GreaterOrEqual,
            LessOrEqual,
            Contains
        }

        public enum ActionType
        {
            StartQuest,
            CompleteQuest,
            AddResource,
            RemoveResource,
            AddItem,
            RemoveItem,
            SetVariable,
            PlayEffect,
            OpenShop,
            Teleport,
            Custom
        }

        #endregion
    }
}
