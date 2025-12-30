using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Quest Asset: ScriptableObject that defines quest data.
    /// Supports objectives, prerequisites, and rewards.
    /// </summary>
    [CreateAssetMenu(fileName = "Quest", menuName = "GameKit/Quest")]
    public class GameKitQuestAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string questId;
        [SerializeField] private string title;
        [TextArea(3, 10)]
        [SerializeField] private string description;

        [Header("Category")]
        [SerializeField] private QuestCategory category = QuestCategory.Side;
        [SerializeField] private string customCategory;

        [Header("Prerequisites")]
        [SerializeField] private List<QuestPrerequisite> prerequisites = new List<QuestPrerequisite>();

        [Header("Objectives")]
        [SerializeField] private List<QuestObjective> objectives = new List<QuestObjective>();
        [SerializeField] private bool requireAllObjectives = true;

        [Header("Rewards")]
        [SerializeField] private List<QuestReward> rewards = new List<QuestReward>();

        [Header("Settings")]
        [SerializeField] private bool autoComplete = true;
        [SerializeField] private bool repeatable = false;
        [SerializeField] private float cooldownTime = 0f;
        [SerializeField] private int maxCompletions = 0;

        [Header("Tracking")]
        [SerializeField] private bool trackOnStart = true;
        [SerializeField] private bool showNotifications = true;

        // Properties
        public string QuestId => questId;
        public string Title => title;
        public string Description => description;
        public QuestCategory Category => category;
        public string CustomCategory => customCategory;
        public IReadOnlyList<QuestPrerequisite> Prerequisites => prerequisites.AsReadOnly();
        public IReadOnlyList<QuestObjective> Objectives => objectives.AsReadOnly();
        public IReadOnlyList<QuestReward> Rewards => rewards.AsReadOnly();
        public bool RequireAllObjectives => requireAllObjectives;
        public bool AutoComplete => autoComplete;
        public bool Repeatable => repeatable;
        public float CooldownTime => cooldownTime;
        public int MaxCompletions => maxCompletions;
        public bool TrackOnStart => trackOnStart;
        public bool ShowNotifications => showNotifications;

        /// <summary>
        /// Initialize the quest asset.
        /// </summary>
        public void Initialize(string id, string questTitle, string questDescription)
        {
            questId = id;
            title = questTitle;
            description = questDescription;
        }

        /// <summary>
        /// Add a prerequisite.
        /// </summary>
        public void AddPrerequisite(QuestPrerequisite prerequisite)
        {
            if (prerequisite != null)
            {
                prerequisites.Add(prerequisite);
            }
        }

        /// <summary>
        /// Remove a prerequisite.
        /// </summary>
        public bool RemovePrerequisite(string prerequisiteId)
        {
            return prerequisites.RemoveAll(p => p.prerequisiteId == prerequisiteId) > 0;
        }

        /// <summary>
        /// Add an objective.
        /// </summary>
        public void AddObjective(QuestObjective objective)
        {
            if (objective == null) return;

            if (string.IsNullOrEmpty(objective.objectiveId))
            {
                objective.objectiveId = $"obj_{Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            objectives.Add(objective);
        }

        /// <summary>
        /// Get objective by ID.
        /// </summary>
        public QuestObjective GetObjective(string objectiveId)
        {
            return objectives.Find(o => o.objectiveId == objectiveId);
        }

        /// <summary>
        /// Remove an objective.
        /// </summary>
        public bool RemoveObjective(string objectiveId)
        {
            return objectives.RemoveAll(o => o.objectiveId == objectiveId) > 0;
        }

        /// <summary>
        /// Add a reward.
        /// </summary>
        public void AddReward(QuestReward reward)
        {
            if (reward != null)
            {
                rewards.Add(reward);
            }
        }

        /// <summary>
        /// Remove a reward.
        /// </summary>
        public bool RemoveReward(string rewardId)
        {
            return rewards.RemoveAll(r => r.rewardId == rewardId) > 0;
        }

        #region Serializable Types

        [Serializable]
        public class QuestPrerequisite
        {
            public string prerequisiteId;
            public PrerequisiteType type;
            public string targetId;
            public string value;
            public ComparisonOperator comparison = ComparisonOperator.GreaterOrEqual;
        }

        [Serializable]
        public class QuestObjective
        {
            public string objectiveId;
            public ObjectiveType type;
            public string description;

            [Header("Target")]
            public string targetId;
            public string targetTag;
            public string targetName;

            [Header("Progress")]
            public int requiredCount = 1;
            public bool hidden = false;
            public bool optional = false;

            [Header("Location")]
            public Vector3 targetPosition;
            public float radius = 5f;
            public string sceneName;

            [Header("UI")]
            public string markerPrefabPath;
            public bool showOnMap = true;
        }

        [Serializable]
        public class QuestReward
        {
            public string rewardId;
            public RewardType type;
            public string targetId;
            public string itemId;
            public float amount;
            public string customData;
        }

        public enum QuestCategory
        {
            Main,
            Side,
            Daily,
            Weekly,
            Event,
            Tutorial,
            Hidden,
            Custom
        }

        public enum PrerequisiteType
        {
            Level,
            Quest,
            Resource,
            Item,
            Achievement,
            Reputation,
            Custom
        }

        public enum ObjectiveType
        {
            Kill,
            Collect,
            Talk,
            Location,
            Interact,
            Escort,
            Defend,
            Deliver,
            Explore,
            Craft,
            Custom
        }

        public enum RewardType
        {
            Resource,
            Item,
            Experience,
            Reputation,
            Unlock,
            Dialogue,
            Custom
        }

        public enum ComparisonOperator
        {
            Equals,
            NotEquals,
            GreaterThan,
            LessThan,
            GreaterOrEqual,
            LessOrEqual
        }

        #endregion
    }
}
