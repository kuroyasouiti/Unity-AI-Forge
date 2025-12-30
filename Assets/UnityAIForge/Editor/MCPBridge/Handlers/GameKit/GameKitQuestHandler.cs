using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCP.Editor.Base;
using UnityAIForge.GameKit;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Quest handler: create and manage quest systems.
    /// Provides declarative quest creation without custom scripts.
    /// </summary>
    public class GameKitQuestHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "createQuest", "updateQuest", "inspectQuest", "deleteQuest",
            "addObjective", "updateObjective", "removeObjective",
            "addPrerequisite", "removePrerequisite",
            "addReward", "removeReward",
            "startQuest", "completeQuest", "failQuest", "abandonQuest",
            "updateProgress", "listQuests",
            "createManager", "inspectManager", "deleteManager",
            "findByQuestId"
        };

        public override string Category => "gamekitQuest";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "createQuest" => CreateQuest(payload),
                "updateQuest" => UpdateQuest(payload),
                "inspectQuest" => InspectQuest(payload),
                "deleteQuest" => DeleteQuest(payload),
                "addObjective" => AddObjective(payload),
                "updateObjective" => UpdateObjective(payload),
                "removeObjective" => RemoveObjective(payload),
                "addPrerequisite" => AddPrerequisite(payload),
                "removePrerequisite" => RemovePrerequisite(payload),
                "addReward" => AddReward(payload),
                "removeReward" => RemoveReward(payload),
                "startQuest" => StartQuest(payload),
                "completeQuest" => CompleteQuest(payload),
                "failQuest" => FailQuest(payload),
                "abandonQuest" => AbandonQuest(payload),
                "updateProgress" => UpdateProgress(payload),
                "listQuests" => ListQuests(payload),
                "createManager" => CreateManager(payload),
                "inspectManager" => InspectManager(payload),
                "deleteManager" => DeleteManager(payload),
                "findByQuestId" => FindByQuestId(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit Quest operation: {operation}")
            };
        }

        #region Quest CRUD

        private object CreateQuest(Dictionary<string, object> payload)
        {
            var questId = GetString(payload, "questId") ?? $"Quest_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var assetPath = GetString(payload, "assetPath") ?? $"Assets/Quests/{questId}.asset";

            // Ensure directory exists
            var directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Check if asset already exists
            if (AssetDatabase.LoadAssetAtPath<GameKitQuestAsset>(assetPath) != null)
            {
                throw new InvalidOperationException($"Quest asset already exists at: {assetPath}");
            }

            var title = GetString(payload, "title") ?? questId;
            var description = GetString(payload, "description") ?? "";

            // Create ScriptableObject
            var quest = ScriptableObject.CreateInstance<GameKitQuestAsset>();
            quest.Initialize(questId, title, description);

            // Set settings via SerializedObject
            var serializedQuest = new SerializedObject(quest);

            if (payload.TryGetValue("category", out var categoryObj))
            {
                var category = ParseQuestCategory(categoryObj.ToString());
                serializedQuest.FindProperty("category").enumValueIndex = (int)category;
            }

            if (payload.TryGetValue("customCategory", out var customCat))
            {
                serializedQuest.FindProperty("customCategory").stringValue = customCat.ToString();
            }

            if (payload.TryGetValue("requireAllObjectives", out var reqAll))
            {
                serializedQuest.FindProperty("requireAllObjectives").boolValue = Convert.ToBoolean(reqAll);
            }

            if (payload.TryGetValue("autoComplete", out var autoComplete))
            {
                serializedQuest.FindProperty("autoComplete").boolValue = Convert.ToBoolean(autoComplete);
            }

            if (payload.TryGetValue("repeatable", out var repeatable))
            {
                serializedQuest.FindProperty("repeatable").boolValue = Convert.ToBoolean(repeatable);
            }

            if (payload.TryGetValue("maxCompletions", out var maxComp))
            {
                serializedQuest.FindProperty("maxCompletions").intValue = Convert.ToInt32(maxComp);
            }

            serializedQuest.ApplyModifiedPropertiesWithoutUndo();

            // Add objectives if provided
            if (payload.TryGetValue("objectives", out var objsObj) && objsObj is List<object> objsList)
            {
                foreach (var objObj in objsList)
                {
                    if (objObj is Dictionary<string, object> objDict)
                    {
                        var objective = ParseObjective(objDict);
                        quest.AddObjective(objective);
                    }
                }
            }

            // Add prerequisites if provided
            if (payload.TryGetValue("prerequisites", out var prereqsObj) && prereqsObj is List<object> prereqsList)
            {
                foreach (var prereqObj in prereqsList)
                {
                    if (prereqObj is Dictionary<string, object> prereqDict)
                    {
                        var prereq = ParsePrerequisite(prereqDict);
                        quest.AddPrerequisite(prereq);
                    }
                }
            }

            // Add rewards if provided
            if (payload.TryGetValue("rewards", out var rewardsObj) && rewardsObj is List<object> rewardsList)
            {
                foreach (var rewardObj in rewardsList)
                {
                    if (rewardObj is Dictionary<string, object> rewardDict)
                    {
                        var reward = ParseReward(rewardDict);
                        quest.AddReward(reward);
                    }
                }
            }

            // Save asset
            AssetDatabase.CreateAsset(quest, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return CreateSuccessResponse(
                ("questId", questId),
                ("assetPath", assetPath),
                ("title", title),
                ("objectiveCount", quest.Objectives.Count),
                ("rewardCount", quest.Rewards.Count)
            );
        }

        private object UpdateQuest(Dictionary<string, object> payload)
        {
            var quest = ResolveQuestAsset(payload);
            var serializedQuest = new SerializedObject(quest);

            if (payload.TryGetValue("title", out var titleObj))
            {
                serializedQuest.FindProperty("title").stringValue = titleObj.ToString();
            }

            if (payload.TryGetValue("description", out var descObj))
            {
                serializedQuest.FindProperty("description").stringValue = descObj.ToString();
            }

            if (payload.TryGetValue("category", out var categoryObj))
            {
                var category = ParseQuestCategory(categoryObj.ToString());
                serializedQuest.FindProperty("category").enumValueIndex = (int)category;
            }

            if (payload.TryGetValue("requireAllObjectives", out var reqAll))
            {
                serializedQuest.FindProperty("requireAllObjectives").boolValue = Convert.ToBoolean(reqAll);
            }

            if (payload.TryGetValue("autoComplete", out var autoComplete))
            {
                serializedQuest.FindProperty("autoComplete").boolValue = Convert.ToBoolean(autoComplete);
            }

            if (payload.TryGetValue("repeatable", out var repeatable))
            {
                serializedQuest.FindProperty("repeatable").boolValue = Convert.ToBoolean(repeatable);
            }

            serializedQuest.ApplyModifiedProperties();
            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("questId", quest.QuestId),
                ("updated", true)
            );
        }

        private object InspectQuest(Dictionary<string, object> payload)
        {
            var quest = ResolveQuestAsset(payload);

            var objectives = new List<Dictionary<string, object>>();
            foreach (var obj in quest.Objectives)
            {
                objectives.Add(SerializeObjective(obj));
            }

            var rewards = new List<Dictionary<string, object>>();
            foreach (var reward in quest.Rewards)
            {
                rewards.Add(SerializeReward(reward));
            }

            var prerequisites = new List<Dictionary<string, object>>();
            foreach (var prereq in quest.Prerequisites)
            {
                prerequisites.Add(SerializePrerequisite(prereq));
            }

            return CreateSuccessResponse(
                ("questId", quest.QuestId),
                ("assetPath", AssetDatabase.GetAssetPath(quest)),
                ("title", quest.Title),
                ("description", quest.Description),
                ("category", quest.Category.ToString()),
                ("requireAllObjectives", quest.RequireAllObjectives),
                ("autoComplete", quest.AutoComplete),
                ("repeatable", quest.Repeatable),
                ("maxCompletions", quest.MaxCompletions),
                ("objectiveCount", quest.Objectives.Count),
                ("objectives", objectives),
                ("rewardCount", quest.Rewards.Count),
                ("rewards", rewards),
                ("prerequisiteCount", quest.Prerequisites.Count),
                ("prerequisites", prerequisites)
            );
        }

        private object DeleteQuest(Dictionary<string, object> payload)
        {
            var quest = ResolveQuestAsset(payload);
            var assetPath = AssetDatabase.GetAssetPath(quest);
            var questId = quest.QuestId;

            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.Refresh();

            return CreateSuccessResponse(
                ("questId", questId),
                ("assetPath", assetPath),
                ("deleted", true)
            );
        }

        #endregion

        #region Objective Operations

        private object AddObjective(Dictionary<string, object> payload)
        {
            var quest = ResolveQuestAsset(payload);

            if (!payload.TryGetValue("objective", out var objObj) || objObj is not Dictionary<string, object> objDict)
            {
                throw new InvalidOperationException("objective data is required for addObjective operation.");
            }

            var objective = ParseObjective(objDict);
            quest.AddObjective(objective);

            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("questId", quest.QuestId),
                ("objectiveId", objective.objectiveId),
                ("objectiveCount", quest.Objectives.Count)
            );
        }

        private object UpdateObjective(Dictionary<string, object> payload)
        {
            var quest = ResolveQuestAsset(payload);
            var objectiveId = GetString(payload, "objectiveId");

            if (string.IsNullOrEmpty(objectiveId))
            {
                throw new InvalidOperationException("objectiveId is required for updateObjective operation.");
            }

            var existing = quest.GetObjective(objectiveId);
            if (existing == null)
            {
                throw new InvalidOperationException($"Objective '{objectiveId}' not found in quest.");
            }

            if (!payload.TryGetValue("objective", out var objObj) || objObj is not Dictionary<string, object> objDict)
            {
                throw new InvalidOperationException("objective data is required for updateObjective operation.");
            }

            // Remove old and add updated
            quest.RemoveObjective(objectiveId);
            objDict["objectiveId"] = objectiveId;
            var updated = ParseObjective(objDict);
            quest.AddObjective(updated);

            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("questId", quest.QuestId),
                ("objectiveId", objectiveId),
                ("updated", true)
            );
        }

        private object RemoveObjective(Dictionary<string, object> payload)
        {
            var quest = ResolveQuestAsset(payload);
            var objectiveId = GetString(payload, "objectiveId");

            if (string.IsNullOrEmpty(objectiveId))
            {
                throw new InvalidOperationException("objectiveId is required for removeObjective operation.");
            }

            var removed = quest.RemoveObjective(objectiveId);
            if (!removed)
            {
                throw new InvalidOperationException($"Objective '{objectiveId}' not found in quest.");
            }

            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("questId", quest.QuestId),
                ("objectiveId", objectiveId),
                ("removed", true),
                ("objectiveCount", quest.Objectives.Count)
            );
        }

        #endregion

        #region Prerequisite Operations

        private object AddPrerequisite(Dictionary<string, object> payload)
        {
            var quest = ResolveQuestAsset(payload);

            if (!payload.TryGetValue("prerequisite", out var prereqObj) || prereqObj is not Dictionary<string, object> prereqDict)
            {
                throw new InvalidOperationException("prerequisite data is required for addPrerequisite operation.");
            }

            var prereq = ParsePrerequisite(prereqDict);
            quest.AddPrerequisite(prereq);

            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("questId", quest.QuestId),
                ("prerequisiteId", prereq.prerequisiteId),
                ("prerequisiteCount", quest.Prerequisites.Count)
            );
        }

        private object RemovePrerequisite(Dictionary<string, object> payload)
        {
            var quest = ResolveQuestAsset(payload);
            var prereqId = GetString(payload, "prerequisiteId");

            if (string.IsNullOrEmpty(prereqId))
            {
                throw new InvalidOperationException("prerequisiteId is required for removePrerequisite operation.");
            }

            var removed = quest.RemovePrerequisite(prereqId);
            if (!removed)
            {
                throw new InvalidOperationException($"Prerequisite '{prereqId}' not found in quest.");
            }

            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("questId", quest.QuestId),
                ("prerequisiteId", prereqId),
                ("removed", true),
                ("prerequisiteCount", quest.Prerequisites.Count)
            );
        }

        #endregion

        #region Reward Operations

        private object AddReward(Dictionary<string, object> payload)
        {
            var quest = ResolveQuestAsset(payload);

            if (!payload.TryGetValue("reward", out var rewardObj) || rewardObj is not Dictionary<string, object> rewardDict)
            {
                throw new InvalidOperationException("reward data is required for addReward operation.");
            }

            var reward = ParseReward(rewardDict);
            quest.AddReward(reward);

            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("questId", quest.QuestId),
                ("rewardId", reward.rewardId),
                ("rewardCount", quest.Rewards.Count)
            );
        }

        private object RemoveReward(Dictionary<string, object> payload)
        {
            var quest = ResolveQuestAsset(payload);
            var rewardId = GetString(payload, "rewardId");

            if (string.IsNullOrEmpty(rewardId))
            {
                throw new InvalidOperationException("rewardId is required for removeReward operation.");
            }

            var removed = quest.RemoveReward(rewardId);
            if (!removed)
            {
                throw new InvalidOperationException($"Reward '{rewardId}' not found in quest.");
            }

            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("questId", quest.QuestId),
                ("rewardId", rewardId),
                ("removed", true),
                ("rewardCount", quest.Rewards.Count)
            );
        }

        #endregion

        #region Runtime Operations

        private object StartQuest(Dictionary<string, object> payload)
        {
            var questId = GetString(payload, "questId");
            if (string.IsNullOrEmpty(questId))
            {
                throw new InvalidOperationException("questId is required for startQuest operation.");
            }

            var quest = ResolveQuestAsset(payload);
            GameKitQuestManager.RegisterQuest(quest);

            return CreateSuccessResponse(
                ("questId", questId),
                ("registered", true),
                ("note", "Quest registered. Start quest in play mode with GameKitQuestManager.Instance.StartQuest().")
            );
        }

        private object CompleteQuest(Dictionary<string, object> payload)
        {
            var questId = GetString(payload, "questId");
            return CreateSuccessResponse(
                ("questId", questId),
                ("note", "Quest completion only works in play mode.")
            );
        }

        private object FailQuest(Dictionary<string, object> payload)
        {
            var questId = GetString(payload, "questId");
            return CreateSuccessResponse(
                ("questId", questId),
                ("note", "Quest failure only works in play mode.")
            );
        }

        private object AbandonQuest(Dictionary<string, object> payload)
        {
            var questId = GetString(payload, "questId");
            return CreateSuccessResponse(
                ("questId", questId),
                ("note", "Quest abandonment only works in play mode.")
            );
        }

        private object UpdateProgress(Dictionary<string, object> payload)
        {
            var questId = GetString(payload, "questId");
            var objectiveId = GetString(payload, "objectiveId");
            var progress = GetInt(payload, "progress", 0);

            return CreateSuccessResponse(
                ("questId", questId),
                ("objectiveId", objectiveId),
                ("progress", progress),
                ("note", "Progress updates only work in play mode.")
            );
        }

        private object ListQuests(Dictionary<string, object> payload)
        {
            var filter = GetString(payload, "filter") ?? "all";
            var guids = AssetDatabase.FindAssets("t:GameKitQuestAsset");

            var quests = new List<Dictionary<string, object>>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var quest = AssetDatabase.LoadAssetAtPath<GameKitQuestAsset>(path);
                if (quest != null)
                {
                    quests.Add(new Dictionary<string, object>
                    {
                        { "questId", quest.QuestId },
                        { "title", quest.Title },
                        { "category", quest.Category.ToString() },
                        { "assetPath", path },
                        { "objectiveCount", quest.Objectives.Count }
                    });
                }
            }

            return CreateSuccessResponse(
                ("filter", filter),
                ("count", quests.Count),
                ("quests", quests)
            );
        }

        #endregion

        #region Manager Operations

        private object CreateManager(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            GameObject targetGo;

            if (string.IsNullOrEmpty(targetPath))
            {
                targetGo = new GameObject("QuestManager");
                Undo.RegisterCreatedObjectUndo(targetGo, "Create Quest Manager");
            }
            else
            {
                targetGo = ResolveGameObject(targetPath);
            }

            if (targetGo == null)
            {
                throw new InvalidOperationException("Failed to create or find target GameObject.");
            }

            var existingManager = targetGo.GetComponent<GameKitQuestManager>();
            if (existingManager != null)
            {
                throw new InvalidOperationException("GameObject already has a GameKitQuestManager component.");
            }

            Undo.AddComponent<GameKitQuestManager>(targetGo);

            return CreateSuccessResponse(
                ("path", BuildGameObjectPath(targetGo)),
                ("created", true)
            );
        }

        private object InspectManager(Dictionary<string, object> payload)
        {
            var manager = GetQuestManager();

            var activeQuests = new List<string>();
            foreach (var kvp in manager.ActiveQuests)
            {
                activeQuests.Add(kvp.Key);
            }

            var completedQuests = new List<string>();
            foreach (var kvp in manager.CompletedQuests)
            {
                completedQuests.Add(kvp.Key);
            }

            return CreateSuccessResponse(
                ("path", BuildGameObjectPath(manager.gameObject)),
                ("activeQuestCount", manager.ActiveQuests.Count),
                ("activeQuests", activeQuests),
                ("completedQuestCount", manager.CompletedQuests.Count),
                ("completedQuests", completedQuests),
                ("failedQuestCount", manager.FailedQuests.Count)
            );
        }

        private object DeleteManager(Dictionary<string, object> payload)
        {
            var manager = GetQuestManager();
            var path = BuildGameObjectPath(manager.gameObject);

            Undo.DestroyObjectImmediate(manager);

            return CreateSuccessResponse(
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Find

        private object FindByQuestId(Dictionary<string, object> payload)
        {
            var questId = GetString(payload, "questId");
            if (string.IsNullOrEmpty(questId))
            {
                throw new InvalidOperationException("questId is required for findByQuestId.");
            }

            var guids = AssetDatabase.FindAssets("t:GameKitQuestAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var quest = AssetDatabase.LoadAssetAtPath<GameKitQuestAsset>(path);
                if (quest != null && quest.QuestId == questId)
                {
                    return CreateSuccessResponse(
                        ("found", true),
                        ("questId", quest.QuestId),
                        ("assetPath", path),
                        ("title", quest.Title)
                    );
                }
            }

            return CreateSuccessResponse(("found", false), ("questId", questId));
        }

        #endregion

        #region Helpers

        private GameKitQuestAsset ResolveQuestAsset(Dictionary<string, object> payload)
        {
            var questId = GetString(payload, "questId");
            var assetPath = GetString(payload, "assetPath");

            if (!string.IsNullOrEmpty(assetPath))
            {
                var quest = AssetDatabase.LoadAssetAtPath<GameKitQuestAsset>(assetPath);
                if (quest != null) return quest;
            }

            if (!string.IsNullOrEmpty(questId))
            {
                var guids = AssetDatabase.FindAssets("t:GameKitQuestAsset");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var quest = AssetDatabase.LoadAssetAtPath<GameKitQuestAsset>(path);
                    if (quest != null && quest.QuestId == questId)
                    {
                        return quest;
                    }
                }
            }

            throw new InvalidOperationException("Either assetPath or questId is required to resolve quest asset.");
        }

        private GameKitQuestManager GetQuestManager()
        {
            var manager = UnityEngine.Object.FindAnyObjectByType<GameKitQuestManager>();
            if (manager == null)
            {
                throw new InvalidOperationException("No GameKitQuestManager found in scene. Create one first.");
            }
            return manager;
        }

        private GameKitQuestAsset.QuestObjective ParseObjective(Dictionary<string, object> dict)
        {
            return new GameKitQuestAsset.QuestObjective
            {
                objectiveId = dict.TryGetValue("objectiveId", out var id) ? id?.ToString() : $"obj_{Guid.NewGuid().ToString().Substring(0, 8)}",
                type = dict.TryGetValue("type", out var type) ? ParseObjectiveType(type.ToString()) : GameKitQuestAsset.ObjectiveType.Custom,
                description = dict.TryGetValue("description", out var desc) ? desc?.ToString() : "",
                targetId = dict.TryGetValue("targetId", out var targetId) ? targetId?.ToString() : "",
                targetTag = dict.TryGetValue("targetTag", out var targetTag) ? targetTag?.ToString() : "",
                targetName = dict.TryGetValue("targetName", out var targetName) ? targetName?.ToString() : "",
                requiredCount = dict.TryGetValue("requiredCount", out var count) ? Convert.ToInt32(count) : 1,
                hidden = dict.TryGetValue("hidden", out var hidden) && Convert.ToBoolean(hidden),
                optional = dict.TryGetValue("optional", out var optional) && Convert.ToBoolean(optional),
                radius = dict.TryGetValue("radius", out var radius) ? Convert.ToSingle(radius) : 5f,
                sceneName = dict.TryGetValue("sceneName", out var scene) ? scene?.ToString() : ""
            };
        }

        private GameKitQuestAsset.QuestPrerequisite ParsePrerequisite(Dictionary<string, object> dict)
        {
            return new GameKitQuestAsset.QuestPrerequisite
            {
                prerequisiteId = dict.TryGetValue("prerequisiteId", out var id) ? id?.ToString() : $"prereq_{Guid.NewGuid().ToString().Substring(0, 8)}",
                type = dict.TryGetValue("type", out var type) ? ParsePrerequisiteType(type.ToString()) : GameKitQuestAsset.PrerequisiteType.Custom,
                targetId = dict.TryGetValue("targetId", out var targetId) ? targetId?.ToString() : "",
                value = dict.TryGetValue("value", out var val) ? val?.ToString() : "",
                comparison = dict.TryGetValue("comparison", out var comp) ? ParseComparisonOperator(comp.ToString()) : GameKitQuestAsset.ComparisonOperator.GreaterOrEqual
            };
        }

        private GameKitQuestAsset.QuestReward ParseReward(Dictionary<string, object> dict)
        {
            return new GameKitQuestAsset.QuestReward
            {
                rewardId = dict.TryGetValue("rewardId", out var id) ? id?.ToString() : $"reward_{Guid.NewGuid().ToString().Substring(0, 8)}",
                type = dict.TryGetValue("type", out var type) ? ParseRewardType(type.ToString()) : GameKitQuestAsset.RewardType.Custom,
                targetId = dict.TryGetValue("targetId", out var targetId) ? targetId?.ToString() : "",
                itemId = dict.TryGetValue("itemId", out var itemId) ? itemId?.ToString() : "",
                amount = dict.TryGetValue("amount", out var amount) ? Convert.ToSingle(amount) : 0f,
                customData = dict.TryGetValue("customData", out var custom) ? custom?.ToString() : ""
            };
        }

        private Dictionary<string, object> SerializeObjective(GameKitQuestAsset.QuestObjective obj)
        {
            return new Dictionary<string, object>
            {
                { "objectiveId", obj.objectiveId },
                { "type", obj.type.ToString() },
                { "description", obj.description },
                { "targetId", obj.targetId },
                { "targetTag", obj.targetTag },
                { "requiredCount", obj.requiredCount },
                { "hidden", obj.hidden },
                { "optional", obj.optional }
            };
        }

        private Dictionary<string, object> SerializePrerequisite(GameKitQuestAsset.QuestPrerequisite prereq)
        {
            return new Dictionary<string, object>
            {
                { "prerequisiteId", prereq.prerequisiteId },
                { "type", prereq.type.ToString() },
                { "targetId", prereq.targetId },
                { "value", prereq.value },
                { "comparison", prereq.comparison.ToString() }
            };
        }

        private Dictionary<string, object> SerializeReward(GameKitQuestAsset.QuestReward reward)
        {
            return new Dictionary<string, object>
            {
                { "rewardId", reward.rewardId },
                { "type", reward.type.ToString() },
                { "targetId", reward.targetId },
                { "itemId", reward.itemId },
                { "amount", reward.amount }
            };
        }

        private GameKitQuestAsset.QuestCategory ParseQuestCategory(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "main" => GameKitQuestAsset.QuestCategory.Main,
                "side" => GameKitQuestAsset.QuestCategory.Side,
                "daily" => GameKitQuestAsset.QuestCategory.Daily,
                "weekly" => GameKitQuestAsset.QuestCategory.Weekly,
                "event" => GameKitQuestAsset.QuestCategory.Event,
                "tutorial" => GameKitQuestAsset.QuestCategory.Tutorial,
                "hidden" => GameKitQuestAsset.QuestCategory.Hidden,
                "custom" => GameKitQuestAsset.QuestCategory.Custom,
                _ => GameKitQuestAsset.QuestCategory.Side
            };
        }

        private GameKitQuestAsset.ObjectiveType ParseObjectiveType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "kill" => GameKitQuestAsset.ObjectiveType.Kill,
                "collect" => GameKitQuestAsset.ObjectiveType.Collect,
                "talk" => GameKitQuestAsset.ObjectiveType.Talk,
                "location" => GameKitQuestAsset.ObjectiveType.Location,
                "interact" => GameKitQuestAsset.ObjectiveType.Interact,
                "escort" => GameKitQuestAsset.ObjectiveType.Escort,
                "defend" => GameKitQuestAsset.ObjectiveType.Defend,
                "deliver" => GameKitQuestAsset.ObjectiveType.Deliver,
                "explore" => GameKitQuestAsset.ObjectiveType.Explore,
                "craft" => GameKitQuestAsset.ObjectiveType.Craft,
                "custom" => GameKitQuestAsset.ObjectiveType.Custom,
                _ => GameKitQuestAsset.ObjectiveType.Custom
            };
        }

        private GameKitQuestAsset.PrerequisiteType ParsePrerequisiteType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "level" => GameKitQuestAsset.PrerequisiteType.Level,
                "quest" => GameKitQuestAsset.PrerequisiteType.Quest,
                "resource" => GameKitQuestAsset.PrerequisiteType.Resource,
                "item" => GameKitQuestAsset.PrerequisiteType.Item,
                "achievement" => GameKitQuestAsset.PrerequisiteType.Achievement,
                "reputation" => GameKitQuestAsset.PrerequisiteType.Reputation,
                "custom" => GameKitQuestAsset.PrerequisiteType.Custom,
                _ => GameKitQuestAsset.PrerequisiteType.Custom
            };
        }

        private GameKitQuestAsset.RewardType ParseRewardType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "resource" => GameKitQuestAsset.RewardType.Resource,
                "item" => GameKitQuestAsset.RewardType.Item,
                "experience" => GameKitQuestAsset.RewardType.Experience,
                "reputation" => GameKitQuestAsset.RewardType.Reputation,
                "unlock" => GameKitQuestAsset.RewardType.Unlock,
                "dialogue" => GameKitQuestAsset.RewardType.Dialogue,
                "custom" => GameKitQuestAsset.RewardType.Custom,
                _ => GameKitQuestAsset.RewardType.Custom
            };
        }

        private GameKitQuestAsset.ComparisonOperator ParseComparisonOperator(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "equals" or "==" or "eq" => GameKitQuestAsset.ComparisonOperator.Equals,
                "notequals" or "!=" or "ne" => GameKitQuestAsset.ComparisonOperator.NotEquals,
                "greaterthan" or ">" or "gt" => GameKitQuestAsset.ComparisonOperator.GreaterThan,
                "lessthan" or "<" or "lt" => GameKitQuestAsset.ComparisonOperator.LessThan,
                "greaterorequal" or ">=" or "gte" => GameKitQuestAsset.ComparisonOperator.GreaterOrEqual,
                "lessorequal" or "<=" or "lte" => GameKitQuestAsset.ComparisonOperator.LessOrEqual,
                _ => GameKitQuestAsset.ComparisonOperator.GreaterOrEqual
            };
        }

        private string BuildGameObjectPath(GameObject go)
        {
            var path = go.name;
            var current = go.transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        #endregion
    }
}
