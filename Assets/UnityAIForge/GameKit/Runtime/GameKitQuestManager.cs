using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Quest Manager: Singleton manager for tracking and managing quests at runtime.
    /// Handles quest progress, objectives, prerequisites, and rewards.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/Quest Manager")]
    public class GameKitQuestManager : MonoBehaviour
    {
        [Header("Events")]
        public UnityEvent<string> OnQuestStarted = new UnityEvent<string>();
        public UnityEvent<string, string, int, int> OnObjectiveUpdated = new UnityEvent<string, string, int, int>();
        public UnityEvent<string, string> OnObjectiveCompleted = new UnityEvent<string, string>();
        public UnityEvent<string> OnQuestCompleted = new UnityEvent<string>();
        public UnityEvent<string> OnQuestFailed = new UnityEvent<string>();
        public UnityEvent<string> OnQuestAbandoned = new UnityEvent<string>();
        public UnityEvent<GameKitQuestAsset.QuestReward> OnRewardGranted = new UnityEvent<GameKitQuestAsset.QuestReward>();

        // Singleton
        private static GameKitQuestManager _instance;
        public static GameKitQuestManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<GameKitQuestManager>();
                }
                return _instance;
            }
        }

        // Quest tracking
        private Dictionary<string, QuestState> activeQuests = new Dictionary<string, QuestState>();
        private Dictionary<string, QuestState> completedQuests = new Dictionary<string, QuestState>();
        private HashSet<string> failedQuests = new HashSet<string>();

        // Quest registry
        private static readonly Dictionary<string, GameKitQuestAsset> _questRegistry = new Dictionary<string, GameKitQuestAsset>();

        // Properties
        public IReadOnlyDictionary<string, QuestState> ActiveQuests => activeQuests;
        public IReadOnlyDictionary<string, QuestState> CompletedQuests => completedQuests;
        public IReadOnlyCollection<string> FailedQuests => failedQuests;

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
            OnQuestStarted ??= new UnityEvent<string>();
            OnObjectiveUpdated ??= new UnityEvent<string, string, int, int>();
            OnObjectiveCompleted ??= new UnityEvent<string, string>();
            OnQuestCompleted ??= new UnityEvent<string>();
            OnQuestFailed ??= new UnityEvent<string>();
            OnQuestAbandoned ??= new UnityEvent<string>();
            OnRewardGranted ??= new UnityEvent<GameKitQuestAsset.QuestReward>();
        }

        #region Registry

        /// <summary>
        /// Register a quest asset for runtime lookup.
        /// </summary>
        public static void RegisterQuest(GameKitQuestAsset quest)
        {
            if (quest != null && !string.IsNullOrEmpty(quest.QuestId))
            {
                _questRegistry[quest.QuestId] = quest;
            }
        }

        /// <summary>
        /// Unregister a quest asset.
        /// </summary>
        public static void UnregisterQuest(string questId)
        {
            _questRegistry.Remove(questId);
        }

        /// <summary>
        /// Find quest asset by ID.
        /// </summary>
        public static GameKitQuestAsset FindQuestAssetById(string questId)
        {
            if (_questRegistry.TryGetValue(questId, out var quest))
                return quest;

            // Try loading from Resources
            return Resources.Load<GameKitQuestAsset>($"Quests/{questId}");
        }

        #endregion

        #region Quest Management

        /// <summary>
        /// Check if quest can be started.
        /// </summary>
        public bool CanStartQuest(string questId)
        {
            var quest = FindQuestAssetById(questId);
            if (quest == null) return false;

            // Already active?
            if (activeQuests.ContainsKey(questId)) return false;

            // Check if repeatable
            if (!quest.Repeatable && completedQuests.ContainsKey(questId)) return false;

            // Check max completions
            if (quest.MaxCompletions > 0 && completedQuests.TryGetValue(questId, out var completed))
            {
                if (completed.CompletionCount >= quest.MaxCompletions) return false;
            }

            // Check prerequisites
            return CheckPrerequisites(quest);
        }

        /// <summary>
        /// Start a quest.
        /// </summary>
        public bool StartQuest(string questId)
        {
            if (!CanStartQuest(questId))
                return false;

            var quest = FindQuestAssetById(questId);
            if (quest == null) return false;

            var state = new QuestState
            {
                QuestId = questId,
                QuestAsset = quest,
                State = QuestStateType.Active,
                StartTime = DateTime.Now,
                ObjectiveProgress = new Dictionary<string, int>()
            };

            // Initialize objective progress
            foreach (var objective in quest.Objectives)
            {
                state.ObjectiveProgress[objective.objectiveId] = 0;
            }

            activeQuests[questId] = state;
            OnQuestStarted?.Invoke(questId);

            return true;
        }

        /// <summary>
        /// Abandon a quest.
        /// </summary>
        public bool AbandonQuest(string questId)
        {
            if (!activeQuests.TryGetValue(questId, out var state))
                return false;

            activeQuests.Remove(questId);
            OnQuestAbandoned?.Invoke(questId);

            return true;
        }

        /// <summary>
        /// Update objective progress.
        /// </summary>
        public void UpdateObjective(string questId, string objectiveId, int progress)
        {
            if (!activeQuests.TryGetValue(questId, out var state))
                return;

            var objective = state.QuestAsset.GetObjective(objectiveId);
            if (objective == null) return;

            int previousProgress = state.ObjectiveProgress.TryGetValue(objectiveId, out var prev) ? prev : 0;
            int newProgress = Mathf.Min(progress, objective.requiredCount);
            state.ObjectiveProgress[objectiveId] = newProgress;

            if (newProgress != previousProgress)
            {
                OnObjectiveUpdated?.Invoke(questId, objectiveId, newProgress, objective.requiredCount);

                // Check if objective completed
                if (newProgress >= objective.requiredCount)
                {
                    OnObjectiveCompleted?.Invoke(questId, objectiveId);

                    // Check if quest is complete
                    if (state.QuestAsset.AutoComplete && CheckQuestComplete(state))
                    {
                        CompleteQuest(questId);
                    }
                }
            }
        }

        /// <summary>
        /// Increment objective progress.
        /// </summary>
        public void IncrementObjective(string questId, string objectiveId, int amount = 1)
        {
            if (!activeQuests.TryGetValue(questId, out var state))
                return;

            int current = state.ObjectiveProgress.TryGetValue(objectiveId, out var progress) ? progress : 0;
            UpdateObjective(questId, objectiveId, current + amount);
        }

        /// <summary>
        /// Report an event for automatic objective tracking.
        /// </summary>
        public void ReportEvent(ObjectiveEventType eventType, string targetId, int amount = 1)
        {
            foreach (var kvp in activeQuests)
            {
                var state = kvp.Value;
                foreach (var objective in state.QuestAsset.Objectives)
                {
                    if (MatchesObjective(objective, eventType, targetId))
                    {
                        IncrementObjective(state.QuestId, objective.objectiveId, amount);
                    }
                }
            }
        }

        /// <summary>
        /// Complete a quest.
        /// </summary>
        public bool CompleteQuest(string questId)
        {
            if (!activeQuests.TryGetValue(questId, out var state))
                return false;

            // Move to completed
            activeQuests.Remove(questId);
            state.State = QuestStateType.Completed;
            state.EndTime = DateTime.Now;

            // Track completion count
            if (completedQuests.TryGetValue(questId, out var existing))
            {
                existing.CompletionCount++;
                existing.EndTime = DateTime.Now;
            }
            else
            {
                state.CompletionCount = 1;
                completedQuests[questId] = state;
            }

            // Grant rewards
            GrantRewards(state.QuestAsset);

            OnQuestCompleted?.Invoke(questId);

            return true;
        }

        /// <summary>
        /// Fail a quest.
        /// </summary>
        public bool FailQuest(string questId)
        {
            if (!activeQuests.TryGetValue(questId, out var state))
                return false;

            activeQuests.Remove(questId);
            state.State = QuestStateType.Failed;
            state.EndTime = DateTime.Now;
            failedQuests.Add(questId);

            OnQuestFailed?.Invoke(questId);

            return true;
        }

        /// <summary>
        /// Get quest state.
        /// </summary>
        public QuestState GetQuest(string questId)
        {
            if (activeQuests.TryGetValue(questId, out var active))
                return active;
            if (completedQuests.TryGetValue(questId, out var completed))
                return completed;
            return null;
        }

        /// <summary>
        /// Get all quests by filter.
        /// </summary>
        public List<QuestState> GetQuests(QuestFilter filter)
        {
            var result = new List<QuestState>();

            switch (filter)
            {
                case QuestFilter.Active:
                    result.AddRange(activeQuests.Values);
                    break;
                case QuestFilter.Completed:
                    result.AddRange(completedQuests.Values);
                    break;
                case QuestFilter.Failed:
                    foreach (var questId in failedQuests)
                    {
                        var quest = FindQuestAssetById(questId);
                        if (quest != null)
                        {
                            result.Add(new QuestState { QuestId = questId, QuestAsset = quest, State = QuestStateType.Failed });
                        }
                    }
                    break;
                case QuestFilter.Available:
                    foreach (var kvp in _questRegistry)
                    {
                        if (CanStartQuest(kvp.Key))
                        {
                            result.Add(new QuestState { QuestId = kvp.Key, QuestAsset = kvp.Value, State = QuestStateType.Available });
                        }
                    }
                    break;
                case QuestFilter.All:
                    result.AddRange(activeQuests.Values);
                    result.AddRange(completedQuests.Values);
                    break;
            }

            return result;
        }

        #endregion

        #region Internal Methods

        private bool CheckPrerequisites(GameKitQuestAsset quest)
        {
            foreach (var prereq in quest.Prerequisites)
            {
                if (!CheckPrerequisite(prereq))
                    return false;
            }
            return true;
        }

        private bool CheckPrerequisite(GameKitQuestAsset.QuestPrerequisite prereq)
        {
            switch (prereq.type)
            {
                case GameKitQuestAsset.PrerequisiteType.Level:
                    // Would need a level system to check this
                    return true;

                case GameKitQuestAsset.PrerequisiteType.Quest:
                    var questState = GetQuest(prereq.targetId);
                    if (prereq.value?.ToLower() == "completed")
                        return questState?.State == QuestStateType.Completed;
                    if (prereq.value?.ToLower() == "notstarted")
                        return questState == null || questState.State == QuestStateType.Available;
                    return questState != null;

                case GameKitQuestAsset.PrerequisiteType.Resource:
                    var resManager = GameKitResourceManager.FindById(prereq.targetId);
                    if (resManager == null) return false;
                    float resValue = resManager.GetResource(prereq.value);
                    // Use the value field as the resource name and compare
                    return true;

                case GameKitQuestAsset.PrerequisiteType.Item:
                    var inventory = GameKitInventory.FindById(prereq.targetId);
                    if (inventory == null) return false;
                    int itemCount = inventory.GetItemCount(prereq.value);
                    return CompareValues(itemCount, prereq);

                case GameKitQuestAsset.PrerequisiteType.Custom:
                    return true;

                default:
                    return true;
            }
        }

        private bool CompareValues(float actual, GameKitQuestAsset.QuestPrerequisite prereq)
        {
            if (!float.TryParse(prereq.value, out float expected))
                return false;

            return prereq.comparison switch
            {
                GameKitQuestAsset.ComparisonOperator.Equals => Mathf.Approximately(actual, expected),
                GameKitQuestAsset.ComparisonOperator.NotEquals => !Mathf.Approximately(actual, expected),
                GameKitQuestAsset.ComparisonOperator.GreaterThan => actual > expected,
                GameKitQuestAsset.ComparisonOperator.LessThan => actual < expected,
                GameKitQuestAsset.ComparisonOperator.GreaterOrEqual => actual >= expected,
                GameKitQuestAsset.ComparisonOperator.LessOrEqual => actual <= expected,
                _ => false
            };
        }

        private bool CheckQuestComplete(QuestState state)
        {
            var quest = state.QuestAsset;

            if (quest.RequireAllObjectives)
            {
                // All required objectives must be complete
                foreach (var objective in quest.Objectives)
                {
                    if (objective.optional) continue;

                    int progress = state.ObjectiveProgress.TryGetValue(objective.objectiveId, out var p) ? p : 0;
                    if (progress < objective.requiredCount)
                        return false;
                }
                return true;
            }
            else
            {
                // At least one objective must be complete
                foreach (var objective in quest.Objectives)
                {
                    int progress = state.ObjectiveProgress.TryGetValue(objective.objectiveId, out var p) ? p : 0;
                    if (progress >= objective.requiredCount)
                        return true;
                }
                return false;
            }
        }

        private bool MatchesObjective(GameKitQuestAsset.QuestObjective objective, ObjectiveEventType eventType, string targetId)
        {
            // Match event type to objective type
            var objectiveType = objective.type;
            bool typeMatches = (eventType, objectiveType) switch
            {
                (ObjectiveEventType.Kill, GameKitQuestAsset.ObjectiveType.Kill) => true,
                (ObjectiveEventType.Collect, GameKitQuestAsset.ObjectiveType.Collect) => true,
                (ObjectiveEventType.Talk, GameKitQuestAsset.ObjectiveType.Talk) => true,
                (ObjectiveEventType.Interact, GameKitQuestAsset.ObjectiveType.Interact) => true,
                (ObjectiveEventType.Location, GameKitQuestAsset.ObjectiveType.Location) => true,
                _ => false
            };

            if (!typeMatches) return false;

            // Match target
            if (!string.IsNullOrEmpty(objective.targetId) && objective.targetId == targetId)
                return true;
            if (!string.IsNullOrEmpty(objective.targetTag) && objective.targetTag == targetId)
                return true;
            if (!string.IsNullOrEmpty(objective.targetName) && objective.targetName == targetId)
                return true;

            return false;
        }

        private void GrantRewards(GameKitQuestAsset quest)
        {
            foreach (var reward in quest.Rewards)
            {
                GrantReward(reward);
            }
        }

        private void GrantReward(GameKitQuestAsset.QuestReward reward)
        {
            switch (reward.type)
            {
                case GameKitQuestAsset.RewardType.Resource:
                    var resManager = GameKitResourceManager.FindById(reward.targetId);
                    if (resManager != null)
                    {
                        resManager.AddResource(reward.itemId ?? "default", reward.amount);
                    }
                    break;

                case GameKitQuestAsset.RewardType.Item:
                    var inventory = GameKitInventory.FindById(reward.targetId);
                    if (inventory != null)
                    {
                        inventory.AddItemById(reward.itemId, (int)reward.amount);
                    }
                    break;

                case GameKitQuestAsset.RewardType.Experience:
                    // Would need an XP system
                    break;

                case GameKitQuestAsset.RewardType.Custom:
                    // Handled by event listeners
                    break;
            }

            OnRewardGranted?.Invoke(reward);
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Get save data for quest state.
        /// </summary>
        public QuestSaveData GetSaveData()
        {
            return new QuestSaveData
            {
                activeQuests = activeQuests.Values.Select(q => new QuestStateSaveData
                {
                    questId = q.QuestId,
                    objectiveProgress = new Dictionary<string, int>(q.ObjectiveProgress),
                    startTime = q.StartTime.ToBinary()
                }).ToList(),
                completedQuests = completedQuests.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.CompletionCount),
                failedQuests = new List<string>(failedQuests)
            };
        }

        /// <summary>
        /// Load quest state from save data.
        /// </summary>
        public void LoadSaveData(QuestSaveData data)
        {
            if (data == null) return;

            activeQuests.Clear();
            completedQuests.Clear();
            failedQuests.Clear();

            // Load active quests
            foreach (var savedQuest in data.activeQuests)
            {
                var quest = FindQuestAssetById(savedQuest.questId);
                if (quest != null)
                {
                    activeQuests[savedQuest.questId] = new QuestState
                    {
                        QuestId = savedQuest.questId,
                        QuestAsset = quest,
                        State = QuestStateType.Active,
                        ObjectiveProgress = new Dictionary<string, int>(savedQuest.objectiveProgress),
                        StartTime = DateTime.FromBinary(savedQuest.startTime)
                    };
                }
            }

            // Load completed quests
            foreach (var kvp in data.completedQuests)
            {
                var quest = FindQuestAssetById(kvp.Key);
                if (quest != null)
                {
                    completedQuests[kvp.Key] = new QuestState
                    {
                        QuestId = kvp.Key,
                        QuestAsset = quest,
                        State = QuestStateType.Completed,
                        CompletionCount = kvp.Value
                    };
                }
            }

            // Load failed quests
            failedQuests = new HashSet<string>(data.failedQuests);
        }

        #endregion

        #region Types

        public class QuestState
        {
            public string QuestId;
            public GameKitQuestAsset QuestAsset;
            public QuestStateType State;
            public Dictionary<string, int> ObjectiveProgress = new Dictionary<string, int>();
            public DateTime StartTime;
            public DateTime EndTime;
            public int CompletionCount;
        }

        public enum QuestStateType
        {
            Available,
            Active,
            Completed,
            Failed
        }

        public enum QuestFilter
        {
            Active,
            Completed,
            Failed,
            Available,
            All
        }

        public enum ObjectiveEventType
        {
            Kill,
            Collect,
            Talk,
            Interact,
            Location
        }

        [Serializable]
        public class QuestSaveData
        {
            public List<QuestStateSaveData> activeQuests = new List<QuestStateSaveData>();
            public Dictionary<string, int> completedQuests = new Dictionary<string, int>();
            public List<string> failedQuests = new List<string>();
        }

        [Serializable]
        public class QuestStateSaveData
        {
            public string questId;
            public Dictionary<string, int> objectiveProgress;
            public long startTime;
        }

        #endregion
    }
}
