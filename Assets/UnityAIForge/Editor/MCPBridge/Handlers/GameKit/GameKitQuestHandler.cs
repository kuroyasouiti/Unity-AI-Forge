using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Quest handler: create and manage quest systems.
    /// Uses code generation to produce standalone QuestManager and QuestData scripts
    /// with zero package dependency.
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

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "createManager" || operation == "createQuest";

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

            // Check if asset already exists at path
            var existingAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (existingAsset != null)
            {
                throw new InvalidOperationException($"Quest asset already exists at: {assetPath}");
            }

            var title = GetString(payload, "title") ?? questId;
            var description = GetString(payload, "description") ?? "";
            var category = ParseQuestCategory(GetString(payload, "category") ?? "Side");

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(questId, "QuestData");

            // Build template variables for the QuestData ScriptableObject
            var variables = new Dictionary<string, object>
            {
                { "QUEST_ID", questId },
                { "TITLE", title },
                { "DESCRIPTION", description },
                { "CATEGORY", category }
            };

            var outputDir = GetString(payload, "outputPath");

            // Generate the QuestData script (ScriptableObject, no target GameObject)
            var genResult = ScriptGenerator.Generate(null, "QuestData", className, questId, variables, outputDir);
            if (!genResult.Success)
            {
                throw new InvalidOperationException(genResult.ErrorMessage ?? "Failed to generate QuestData script.");
            }

            // Try to resolve the generated type and create the asset
            var questType = ScriptGenerator.ResolveGeneratedType(className);
            if (questType != null)
            {
                var quest = ScriptableObject.CreateInstance(questType);
                var so = new SerializedObject(quest);

                so.FindProperty("questId").stringValue = questId;
                so.FindProperty("title").stringValue = title;
                so.FindProperty("description").stringValue = description;

                // Set category enum
                var categoryProp = so.FindProperty("category");
                if (categoryProp != null)
                {
                    var names = categoryProp.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], category, StringComparison.OrdinalIgnoreCase))
                        {
                            categoryProp.enumValueIndex = i;
                            break;
                        }
                    }
                }

                if (payload.TryGetValue("customCategory", out var customCat))
                    SetPropertyIfExists(so, "customCategory", customCat.ToString());

                if (payload.TryGetValue("requireAllObjectives", out var reqAll))
                    SetBoolPropertyIfExists(so, "requireAllObjectives", Convert.ToBoolean(reqAll));

                if (payload.TryGetValue("autoComplete", out var autoComplete))
                    SetBoolPropertyIfExists(so, "autoComplete", Convert.ToBoolean(autoComplete));

                if (payload.TryGetValue("repeatable", out var repeatable))
                    SetBoolPropertyIfExists(so, "repeatable", Convert.ToBoolean(repeatable));

                if (payload.TryGetValue("maxCompletions", out var maxComp))
                    SetIntPropertyIfExists(so, "maxCompletions", Convert.ToInt32(maxComp));

                so.ApplyModifiedPropertiesWithoutUndo();

                // Add objectives if provided
                if (payload.TryGetValue("objectives", out var objsObj) && objsObj is List<object> objsList)
                {
                    var objectivesProp = so.FindProperty("objectives");
                    if (objectivesProp != null)
                    {
                        foreach (var objObj in objsList)
                        {
                            if (objObj is Dictionary<string, object> objDict)
                            {
                                AddObjectiveToArray(objectivesProp, objDict);
                            }
                        }
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                // Add prerequisites if provided
                if (payload.TryGetValue("prerequisites", out var prereqsObj) && prereqsObj is List<object> prereqsList)
                {
                    var prereqsProp = so.FindProperty("prerequisites");
                    if (prereqsProp != null)
                    {
                        foreach (var prereqObj in prereqsList)
                        {
                            if (prereqObj is Dictionary<string, object> prereqDict)
                            {
                                AddPrerequisiteToArray(prereqsProp, prereqDict);
                            }
                        }
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                // Add rewards if provided
                if (payload.TryGetValue("rewards", out var rewardsObj) && rewardsObj is List<object> rewardsList)
                {
                    var rewardsProp = so.FindProperty("rewards");
                    if (rewardsProp != null)
                    {
                        foreach (var rewardObj in rewardsList)
                        {
                            if (rewardObj is Dictionary<string, object> rewardDict)
                            {
                                AddRewardToArray(rewardsProp, rewardDict);
                            }
                        }
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                // Save asset
                AssetDatabase.CreateAsset(quest, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var objectiveCount = so.FindProperty("objectives")?.arraySize ?? 0;
                var rewardCount = so.FindProperty("rewards")?.arraySize ?? 0;

                return CreateSuccessResponse(
                    ("questId", questId),
                    ("assetPath", assetPath),
                    ("title", title),
                    ("scriptPath", genResult.ScriptPath),
                    ("className", genResult.ClassName),
                    ("objectiveCount", objectiveCount),
                    ("rewardCount", rewardCount),
                    ("compilationRequired", false)
                );
            }

            // Type not compiled yet; script was generated but asset creation deferred
            return CreateSuccessResponse(
                ("questId", questId),
                ("assetPath", assetPath),
                ("title", title),
                ("scriptPath", genResult.ScriptPath),
                ("className", genResult.ClassName),
                ("compilationRequired", true),
                ("note", "QuestData script generated. After Unity recompiles, run createQuest again to create the asset.")
            );
        }

        private object UpdateQuest(Dictionary<string, object> payload)
        {
            var quest = ResolveQuestAsset(payload);
            var so = new SerializedObject(quest);

            if (payload.TryGetValue("title", out var titleObj))
                so.FindProperty("title").stringValue = titleObj.ToString();

            if (payload.TryGetValue("description", out var descObj))
                so.FindProperty("description").stringValue = descObj.ToString();

            if (payload.TryGetValue("category", out var categoryObj))
            {
                var category = ParseQuestCategory(categoryObj.ToString());
                var categoryProp = so.FindProperty("category");
                if (categoryProp != null)
                {
                    var names = categoryProp.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], category, StringComparison.OrdinalIgnoreCase))
                        {
                            categoryProp.enumValueIndex = i;
                            break;
                        }
                    }
                }
            }

            if (payload.TryGetValue("customCategory", out var customCat))
                SetPropertyIfExists(so, "customCategory", customCat.ToString());

            if (payload.TryGetValue("requireAllObjectives", out var reqAll))
                SetBoolPropertyIfExists(so, "requireAllObjectives", Convert.ToBoolean(reqAll));

            if (payload.TryGetValue("autoComplete", out var autoComplete))
                SetBoolPropertyIfExists(so, "autoComplete", Convert.ToBoolean(autoComplete));

            if (payload.TryGetValue("repeatable", out var repeatable))
                SetBoolPropertyIfExists(so, "repeatable", Convert.ToBoolean(repeatable));

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();

            var questIdProp = so.FindProperty("questId");
            var resolvedQuestId = questIdProp != null ? questIdProp.stringValue : "unknown";

            return CreateSuccessResponse(
                ("questId", resolvedQuestId),
                ("updated", true)
            );
        }

        private object InspectQuest(Dictionary<string, object> payload)
        {
            var quest = ResolveQuestAsset(payload);
            var so = new SerializedObject(quest);

            var questId = so.FindProperty("questId")?.stringValue ?? "";
            var title = so.FindProperty("title")?.stringValue ?? "";
            var description = so.FindProperty("description")?.stringValue ?? "";

            var categoryProp = so.FindProperty("category");
            var category = categoryProp != null && categoryProp.enumValueIndex < categoryProp.enumDisplayNames.Length
                ? categoryProp.enumDisplayNames[categoryProp.enumValueIndex]
                : "Side";

            var requireAllObjectives = so.FindProperty("requireAllObjectives")?.boolValue ?? true;
            var autoComplete = so.FindProperty("autoComplete")?.boolValue ?? true;
            var repeatable = so.FindProperty("repeatable")?.boolValue ?? false;
            var maxCompletions = so.FindProperty("maxCompletions")?.intValue ?? 0;

            // Serialize objectives
            var objectives = new List<Dictionary<string, object>>();
            var objectivesProp = so.FindProperty("objectives");
            if (objectivesProp != null)
            {
                for (int i = 0; i < objectivesProp.arraySize; i++)
                {
                    objectives.Add(SerializeObjectiveFromProperty(objectivesProp.GetArrayElementAtIndex(i)));
                }
            }

            // Serialize rewards
            var rewards = new List<Dictionary<string, object>>();
            var rewardsProp = so.FindProperty("rewards");
            if (rewardsProp != null)
            {
                for (int i = 0; i < rewardsProp.arraySize; i++)
                {
                    rewards.Add(SerializeRewardFromProperty(rewardsProp.GetArrayElementAtIndex(i)));
                }
            }

            // Serialize prerequisites
            var prerequisites = new List<Dictionary<string, object>>();
            var prereqsProp = so.FindProperty("prerequisites");
            if (prereqsProp != null)
            {
                for (int i = 0; i < prereqsProp.arraySize; i++)
                {
                    prerequisites.Add(SerializePrerequisiteFromProperty(prereqsProp.GetArrayElementAtIndex(i)));
                }
            }

            return CreateSuccessResponse(
                ("questId", questId),
                ("assetPath", AssetDatabase.GetAssetPath(quest)),
                ("title", title),
                ("description", description),
                ("category", category),
                ("requireAllObjectives", requireAllObjectives),
                ("autoComplete", autoComplete),
                ("repeatable", repeatable),
                ("maxCompletions", maxCompletions),
                ("objectiveCount", objectives.Count),
                ("objectives", objectives),
                ("rewardCount", rewards.Count),
                ("rewards", rewards),
                ("prerequisiteCount", prerequisites.Count),
                ("prerequisites", prerequisites)
            );
        }

        private object DeleteQuest(Dictionary<string, object> payload)
        {
            var quest = ResolveQuestAsset(payload);
            var assetPath = AssetDatabase.GetAssetPath(quest);
            var so = new SerializedObject(quest);
            var questId = so.FindProperty("questId")?.stringValue ?? "";

            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.Refresh();

            // Clean up the generated script from tracker
            ScriptGenerator.Delete(questId);

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
            var so = new SerializedObject(quest);

            if (!payload.TryGetValue("objective", out var objObj) || objObj is not Dictionary<string, object> objDict)
            {
                throw new InvalidOperationException("objective data is required for addObjective operation.");
            }

            var objectivesProp = so.FindProperty("objectives");
            if (objectivesProp == null)
            {
                throw new InvalidOperationException("objectives property not found on quest asset.");
            }

            var objectiveId = objDict.TryGetValue("objectiveId", out var id)
                ? id?.ToString()
                : $"obj_{Guid.NewGuid().ToString().Substring(0, 8)}";

            AddObjectiveToArray(objectivesProp, objDict);
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();

            var questId = so.FindProperty("questId")?.stringValue ?? "";

            return CreateSuccessResponse(
                ("questId", questId),
                ("objectiveId", objectiveId),
                ("objectiveCount", objectivesProp.arraySize)
            );
        }

        private object UpdateObjective(Dictionary<string, object> payload)
        {
            var quest = ResolveQuestAsset(payload);
            var so = new SerializedObject(quest);
            var objectiveId = GetString(payload, "objectiveId");

            if (string.IsNullOrEmpty(objectiveId))
            {
                throw new InvalidOperationException("objectiveId is required for updateObjective operation.");
            }

            if (!payload.TryGetValue("objective", out var objObj) || objObj is not Dictionary<string, object> objDict)
            {
                throw new InvalidOperationException("objective data is required for updateObjective operation.");
            }

            var objectivesProp = so.FindProperty("objectives");
            if (objectivesProp == null)
            {
                throw new InvalidOperationException("objectives property not found on quest asset.");
            }

            // Find and remove old objective
            int foundIndex = -1;
            for (int i = 0; i < objectivesProp.arraySize; i++)
            {
                var elem = objectivesProp.GetArrayElementAtIndex(i);
                var elemId = elem.FindPropertyRelative("objectiveId")?.stringValue;
                if (elemId == objectiveId)
                {
                    foundIndex = i;
                    break;
                }
            }

            if (foundIndex < 0)
            {
                throw new InvalidOperationException($"Objective '{objectiveId}' not found in quest.");
            }

            objectivesProp.DeleteArrayElementAtIndex(foundIndex);

            // Add updated objective
            objDict["objectiveId"] = objectiveId;
            AddObjectiveToArray(objectivesProp, objDict);
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();

            var questIdVal = so.FindProperty("questId")?.stringValue ?? "";

            return CreateSuccessResponse(
                ("questId", questIdVal),
                ("objectiveId", objectiveId),
                ("updated", true)
            );
        }

        private object RemoveObjective(Dictionary<string, object> payload)
        {
            var quest = ResolveQuestAsset(payload);
            var so = new SerializedObject(quest);
            var objectiveId = GetString(payload, "objectiveId");

            if (string.IsNullOrEmpty(objectiveId))
            {
                throw new InvalidOperationException("objectiveId is required for removeObjective operation.");
            }

            var objectivesProp = so.FindProperty("objectives");
            if (objectivesProp == null)
            {
                throw new InvalidOperationException("objectives property not found on quest asset.");
            }

            bool removed = false;
            for (int i = 0; i < objectivesProp.arraySize; i++)
            {
                var elem = objectivesProp.GetArrayElementAtIndex(i);
                var elemId = elem.FindPropertyRelative("objectiveId")?.stringValue;
                if (elemId == objectiveId)
                {
                    objectivesProp.DeleteArrayElementAtIndex(i);
                    removed = true;
                    break;
                }
            }

            if (!removed)
            {
                throw new InvalidOperationException($"Objective '{objectiveId}' not found in quest.");
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();

            var questIdVal = so.FindProperty("questId")?.stringValue ?? "";

            return CreateSuccessResponse(
                ("questId", questIdVal),
                ("objectiveId", objectiveId),
                ("removed", true),
                ("objectiveCount", objectivesProp.arraySize)
            );
        }

        #endregion

        #region Prerequisite Operations

        private object AddPrerequisite(Dictionary<string, object> payload)
        {
            var quest = ResolveQuestAsset(payload);
            var so = new SerializedObject(quest);

            if (!payload.TryGetValue("prerequisite", out var prereqObj) || prereqObj is not Dictionary<string, object> prereqDict)
            {
                throw new InvalidOperationException("prerequisite data is required for addPrerequisite operation.");
            }

            var prereqsProp = so.FindProperty("prerequisites");
            if (prereqsProp == null)
            {
                throw new InvalidOperationException("prerequisites property not found on quest asset.");
            }

            AddPrerequisiteToArray(prereqsProp, prereqDict);
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();

            var questId = so.FindProperty("questId")?.stringValue ?? "";
            var prereqTarget = prereqDict.TryGetValue("target", out var t) ? t?.ToString() : "";

            return CreateSuccessResponse(
                ("questId", questId),
                ("prerequisiteTarget", prereqTarget),
                ("prerequisiteCount", prereqsProp.arraySize)
            );
        }

        private object RemovePrerequisite(Dictionary<string, object> payload)
        {
            var quest = ResolveQuestAsset(payload);
            var so = new SerializedObject(quest);
            var prereqIndex = GetInt(payload, "prerequisiteIndex", -1);

            var prereqsProp = so.FindProperty("prerequisites");
            if (prereqsProp == null)
            {
                throw new InvalidOperationException("prerequisites property not found on quest asset.");
            }

            // Try to remove by target string if index not provided
            if (prereqIndex < 0)
            {
                var prereqTarget = GetString(payload, "prerequisiteTarget") ?? GetString(payload, "prerequisiteId");
                if (string.IsNullOrEmpty(prereqTarget))
                {
                    throw new InvalidOperationException("prerequisiteIndex or prerequisiteTarget is required for removePrerequisite operation.");
                }

                for (int i = 0; i < prereqsProp.arraySize; i++)
                {
                    var elem = prereqsProp.GetArrayElementAtIndex(i);
                    var target = elem.FindPropertyRelative("target")?.stringValue;
                    if (target == prereqTarget)
                    {
                        prereqIndex = i;
                        break;
                    }
                }

                if (prereqIndex < 0)
                {
                    throw new InvalidOperationException($"Prerequisite with target '{prereqTarget}' not found in quest.");
                }
            }

            if (prereqIndex < 0 || prereqIndex >= prereqsProp.arraySize)
            {
                throw new InvalidOperationException($"Prerequisite index {prereqIndex} is out of range.");
            }

            prereqsProp.DeleteArrayElementAtIndex(prereqIndex);
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();

            var questId = so.FindProperty("questId")?.stringValue ?? "";

            return CreateSuccessResponse(
                ("questId", questId),
                ("removed", true),
                ("prerequisiteCount", prereqsProp.arraySize)
            );
        }

        #endregion

        #region Reward Operations

        private object AddReward(Dictionary<string, object> payload)
        {
            var quest = ResolveQuestAsset(payload);
            var so = new SerializedObject(quest);

            if (!payload.TryGetValue("reward", out var rewardObj) || rewardObj is not Dictionary<string, object> rewardDict)
            {
                throw new InvalidOperationException("reward data is required for addReward operation.");
            }

            var rewardsProp = so.FindProperty("rewards");
            if (rewardsProp == null)
            {
                throw new InvalidOperationException("rewards property not found on quest asset.");
            }

            var rewardId = rewardDict.TryGetValue("rewardId", out var rid)
                ? rid?.ToString()
                : $"reward_{Guid.NewGuid().ToString().Substring(0, 8)}";

            AddRewardToArray(rewardsProp, rewardDict);
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();

            var questId = so.FindProperty("questId")?.stringValue ?? "";

            return CreateSuccessResponse(
                ("questId", questId),
                ("rewardId", rewardId),
                ("rewardCount", rewardsProp.arraySize)
            );
        }

        private object RemoveReward(Dictionary<string, object> payload)
        {
            var quest = ResolveQuestAsset(payload);
            var so = new SerializedObject(quest);
            var rewardId = GetString(payload, "rewardId");

            if (string.IsNullOrEmpty(rewardId))
            {
                throw new InvalidOperationException("rewardId is required for removeReward operation.");
            }

            var rewardsProp = so.FindProperty("rewards");
            if (rewardsProp == null)
            {
                throw new InvalidOperationException("rewards property not found on quest asset.");
            }

            bool removed = false;
            for (int i = 0; i < rewardsProp.arraySize; i++)
            {
                var elem = rewardsProp.GetArrayElementAtIndex(i);
                var elemId = elem.FindPropertyRelative("rewardId")?.stringValue;
                if (elemId == rewardId)
                {
                    rewardsProp.DeleteArrayElementAtIndex(i);
                    removed = true;
                    break;
                }
            }

            if (!removed)
            {
                throw new InvalidOperationException($"Reward '{rewardId}' not found in quest.");
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();

            var questIdVal = so.FindProperty("questId")?.stringValue ?? "";

            return CreateSuccessResponse(
                ("questId", questIdVal),
                ("rewardId", rewardId),
                ("removed", true),
                ("rewardCount", rewardsProp.arraySize)
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

            // Verify the quest asset exists
            ResolveQuestAsset(payload);

            // Try to find a QuestManager in the scene and register via reflection
            var manager = ResolveQuestManagerComponent();
            if (manager != null)
            {
                var quest = ResolveQuestAsset(payload);
                try
                {
                    var registerMethod = manager.GetType().GetMethod("RegisterQuest",
                        BindingFlags.Public | BindingFlags.Instance);
                    if (registerMethod != null)
                    {
                        // Build a QuestDefinition via reflection
                        var defType = manager.GetType().GetNestedType("QuestDefinition");
                        if (defType != null)
                        {
                            var def = Activator.CreateInstance(defType);
                            var so = new SerializedObject(quest);
                            SetFieldViaReflection(def, "questId", so.FindProperty("questId")?.stringValue ?? questId);
                            SetFieldViaReflection(def, "title", so.FindProperty("title")?.stringValue ?? questId);
                            SetFieldViaReflection(def, "description", so.FindProperty("description")?.stringValue ?? "");
                            registerMethod.Invoke(manager, new[] { def });
                        }
                    }
                }
                catch
                {
                    // Reflection-based registration is best-effort
                }
            }

            return CreateSuccessResponse(
                ("questId", questId),
                ("registered", manager != null),
                ("note", "Quest registered. Start quest in play mode with QuestManager.Instance.StartQuest().")
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

            // Search for all ScriptableObjects that have a "questId" field
            var guids = AssetDatabase.FindAssets("t:ScriptableObject");

            var quests = new List<Dictionary<string, object>>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null) continue;

                var so = new SerializedObject(asset);
                var questIdProp = so.FindProperty("questId");
                if (questIdProp == null || questIdProp.propertyType != SerializedPropertyType.String) continue;

                // Also verify it has objectives property (to distinguish from other SOs with questId)
                var objectivesProp = so.FindProperty("objectives");
                if (objectivesProp == null) continue;

                var categoryProp = so.FindProperty("category");
                var categoryStr = categoryProp != null && categoryProp.enumValueIndex < categoryProp.enumDisplayNames.Length
                    ? categoryProp.enumDisplayNames[categoryProp.enumValueIndex]
                    : "Side";

                quests.Add(new Dictionary<string, object>
                {
                    { "questId", questIdProp.stringValue },
                    { "title", so.FindProperty("title")?.stringValue ?? "" },
                    { "category", categoryStr },
                    { "assetPath", path },
                    { "objectiveCount", objectivesProp.arraySize }
                });
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

            // Check if already has a quest manager component (by checking for questManagerId field)
            var existingManager = CodeGenHelper.FindComponentByField(targetGo, "questManagerId", null);
            if (existingManager != null)
            {
                throw new InvalidOperationException("GameObject already has a QuestManager component.");
            }

            var questManagerId = GetString(payload, "questManagerId")
                ?? $"QuestMgr_{Guid.NewGuid().ToString().Substring(0, 8)}";

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(questManagerId, "QuestManager");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "QUEST_MANAGER_ID", questManagerId }
            };

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "QuestManager", questManagerId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate QuestManager script.");
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["questManagerId"] = questManagerId;
            result["path"] = BuildGameObjectPath(targetGo);

            return result;
        }

        private object InspectManager(Dictionary<string, object> payload)
        {
            var manager = ResolveQuestManagerByPayload(payload);

            var info = new Dictionary<string, object>
            {
                { "path", BuildGameObjectPath(manager.gameObject) }
            };

            // Read properties via SerializedObject (the manager is a generated component)
            var so = new SerializedObject(manager);

            var questManagerIdProp = so.FindProperty("questManagerId");
            if (questManagerIdProp != null)
                info["questManagerId"] = questManagerIdProp.stringValue;

            // Runtime state is only available in play mode; provide what we can from serialized data
            info["note"] = "Active/completed quest lists are runtime-only and available in play mode.";

            return CreateSuccessResponse(("manager", info));
        }

        private object DeleteManager(Dictionary<string, object> payload)
        {
            var manager = ResolveQuestManagerByPayload(payload);
            var path = BuildGameObjectPath(manager.gameObject);
            var so = new SerializedObject(manager);
            var questManagerId = so.FindProperty("questManagerId")?.stringValue ?? "";
            var scene = manager.gameObject.scene;

            Undo.DestroyObjectImmediate(manager);

            // Clean up generated script from tracker
            ScriptGenerator.Delete(questManagerId);

            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("questManagerId", questManagerId),
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

            // Search for ScriptableObjects with matching questId
            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null) continue;

                try
                {
                    var so = new SerializedObject(asset);
                    var questIdProp = so.FindProperty("questId");
                    if (questIdProp != null && questIdProp.propertyType == SerializedPropertyType.String
                        && questIdProp.stringValue == questId)
                    {
                        // Verify it also has objectives (quest data asset signature)
                        var objectivesProp = so.FindProperty("objectives");
                        if (objectivesProp == null) continue;

                        return CreateSuccessResponse(
                            ("found", true),
                            ("questId", questId),
                            ("assetPath", path),
                            ("title", so.FindProperty("title")?.stringValue ?? "")
                        );
                    }
                }
                catch
                {
                    // Skip assets that can't be serialized
                }
            }

            return CreateSuccessResponse(("found", false), ("questId", questId));
        }

        #endregion

        #region Helpers

        private ScriptableObject ResolveQuestAsset(Dictionary<string, object> payload)
        {
            var questId = GetString(payload, "questId");
            var assetPath = GetString(payload, "assetPath");

            // Try by asset path first
            if (!string.IsNullOrEmpty(assetPath))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (asset != null)
                {
                    var so = new SerializedObject(asset);
                    var questIdProp = so.FindProperty("questId");
                    if (questIdProp != null)
                        return asset;
                }
            }

            // Try by questId - search all ScriptableObjects
            if (!string.IsNullOrEmpty(questId))
            {
                var guids = AssetDatabase.FindAssets("t:ScriptableObject");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    if (asset == null) continue;

                    try
                    {
                        var so = new SerializedObject(asset);
                        var questIdProp = so.FindProperty("questId");
                        if (questIdProp != null && questIdProp.propertyType == SerializedPropertyType.String
                            && questIdProp.stringValue == questId)
                        {
                            // Verify it also has objectives (quest data asset signature)
                            var objectivesProp = so.FindProperty("objectives");
                            if (objectivesProp != null)
                                return asset;
                        }
                    }
                    catch
                    {
                        // Skip assets that can't be serialized
                    }
                }
            }

            throw new InvalidOperationException("Either assetPath or questId is required to resolve quest asset.");
        }

        private Component ResolveQuestManagerComponent()
        {
            // Search all MonoBehaviours in the scene for one with a questManagerId field
            var allMonoBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var comp in allMonoBehaviours)
            {
                if (comp == null) continue;
                try
                {
                    var so = new SerializedObject(comp);
                    var prop = so.FindProperty("questManagerId");
                    if (prop != null && prop.propertyType == SerializedPropertyType.String)
                        return comp;
                }
                catch
                {
                    // Skip components that can't be serialized
                }
            }
            return null;
        }

        private Component ResolveQuestManagerByPayload(Dictionary<string, object> payload)
        {
            // Try by questManagerId
            var questManagerId = GetString(payload, "questManagerId");
            if (!string.IsNullOrEmpty(questManagerId))
            {
                var managerById = CodeGenHelper.FindComponentInSceneByField("questManagerId", questManagerId);
                if (managerById != null)
                    return managerById;
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var managerByPath = CodeGenHelper.FindComponentByField(targetGo, "questManagerId", null);
                    if (managerByPath != null)
                        return managerByPath;

                    throw new InvalidOperationException($"No QuestManager component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            // Fallback: find any quest manager in scene
            var anyManager = ResolveQuestManagerComponent();
            if (anyManager != null)
                return anyManager;

            throw new InvalidOperationException("No QuestManager found in scene. Create one first with createManager.");
        }

        #endregion

        #region Array Helpers

        private void AddObjectiveToArray(SerializedProperty arrayProp, Dictionary<string, object> dict)
        {
            var index = arrayProp.arraySize;
            arrayProp.InsertArrayElementAtIndex(index);
            var elem = arrayProp.GetArrayElementAtIndex(index);

            var objectiveId = dict.TryGetValue("objectiveId", out var id)
                ? id?.ToString()
                : $"obj_{Guid.NewGuid().ToString().Substring(0, 8)}";

            SetRelativePropertyIfExists(elem, "objectiveId", objectiveId);

            if (dict.TryGetValue("type", out var typeObj))
            {
                var typeName = ParseObjectiveType(typeObj.ToString());
                var typeProp = elem.FindPropertyRelative("type");
                if (typeProp != null)
                {
                    var names = typeProp.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], typeName, StringComparison.OrdinalIgnoreCase))
                        {
                            typeProp.enumValueIndex = i;
                            break;
                        }
                    }
                }
            }

            if (dict.TryGetValue("description", out var desc))
                SetRelativePropertyIfExists(elem, "description", desc?.ToString() ?? "");

            if (dict.TryGetValue("targetId", out var targetId))
                SetRelativePropertyIfExists(elem, "targetId", targetId?.ToString() ?? "");

            if (dict.TryGetValue("targetTag", out var targetTag))
                SetRelativePropertyIfExists(elem, "targetTag", targetTag?.ToString() ?? "");

            if (dict.TryGetValue("requiredCount", out var count))
            {
                var countProp = elem.FindPropertyRelative("requiredCount");
                if (countProp != null)
                    countProp.intValue = Convert.ToInt32(count);
            }

            if (dict.TryGetValue("sceneName", out var scene))
                SetRelativePropertyIfExists(elem, "locationScene", scene?.ToString() ?? "");

            if (dict.TryGetValue("radius", out var radius))
            {
                var radiusProp = elem.FindPropertyRelative("locationRadius");
                if (radiusProp != null)
                    radiusProp.floatValue = Convert.ToSingle(radius);
            }
        }

        private void AddPrerequisiteToArray(SerializedProperty arrayProp, Dictionary<string, object> dict)
        {
            var index = arrayProp.arraySize;
            arrayProp.InsertArrayElementAtIndex(index);
            var elem = arrayProp.GetArrayElementAtIndex(index);

            if (dict.TryGetValue("type", out var typeObj))
            {
                var typeName = ParsePrerequisiteType(typeObj.ToString());
                var typeProp = elem.FindPropertyRelative("type");
                if (typeProp != null)
                {
                    var names = typeProp.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], typeName, StringComparison.OrdinalIgnoreCase))
                        {
                            typeProp.enumValueIndex = i;
                            break;
                        }
                    }
                }
            }

            if (dict.TryGetValue("targetId", out var targetId))
                SetRelativePropertyIfExists(elem, "target", targetId?.ToString() ?? "");
            else if (dict.TryGetValue("target", out var target))
                SetRelativePropertyIfExists(elem, "target", target?.ToString() ?? "");

            if (dict.TryGetValue("value", out var val))
            {
                var valueProp = elem.FindPropertyRelative("value");
                if (valueProp != null)
                    valueProp.floatValue = Convert.ToSingle(val);
            }

            if (dict.TryGetValue("comparison", out var comp))
                SetRelativePropertyIfExists(elem, "comparison", ParseComparisonOperator(comp.ToString()));
        }

        private void AddRewardToArray(SerializedProperty arrayProp, Dictionary<string, object> dict)
        {
            var index = arrayProp.arraySize;
            arrayProp.InsertArrayElementAtIndex(index);
            var elem = arrayProp.GetArrayElementAtIndex(index);

            var rewardId = dict.TryGetValue("rewardId", out var rid)
                ? rid?.ToString()
                : $"reward_{Guid.NewGuid().ToString().Substring(0, 8)}";

            SetRelativePropertyIfExists(elem, "rewardId", rewardId);

            if (dict.TryGetValue("type", out var typeObj))
            {
                var typeName = ParseRewardType(typeObj.ToString());
                var typeProp = elem.FindPropertyRelative("type");
                if (typeProp != null)
                {
                    var names = typeProp.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], typeName, StringComparison.OrdinalIgnoreCase))
                        {
                            typeProp.enumValueIndex = i;
                            break;
                        }
                    }
                }
            }

            if (dict.TryGetValue("targetId", out var targetId))
                SetRelativePropertyIfExists(elem, "target", targetId?.ToString() ?? "");
            else if (dict.TryGetValue("target", out var target))
                SetRelativePropertyIfExists(elem, "target", target?.ToString() ?? "");

            if (dict.TryGetValue("itemId", out var itemId))
                SetRelativePropertyIfExists(elem, "itemId", itemId?.ToString() ?? "");

            if (dict.TryGetValue("amount", out var amount))
            {
                var amountProp = elem.FindPropertyRelative("amount");
                if (amountProp != null)
                    amountProp.floatValue = Convert.ToSingle(amount);
            }

            if (dict.TryGetValue("customData", out var custom))
                SetRelativePropertyIfExists(elem, "customData", custom?.ToString() ?? "");
        }

        #endregion

        #region Serialization Helpers

        private Dictionary<string, object> SerializeObjectiveFromProperty(SerializedProperty elem)
        {
            var result = new Dictionary<string, object>();

            var objectiveId = elem.FindPropertyRelative("objectiveId");
            if (objectiveId != null) result["objectiveId"] = objectiveId.stringValue;

            var typeProp = elem.FindPropertyRelative("type");
            if (typeProp != null)
            {
                result["type"] = typeProp.enumValueIndex < typeProp.enumDisplayNames.Length
                    ? typeProp.enumDisplayNames[typeProp.enumValueIndex]
                    : "Custom";
            }

            var desc = elem.FindPropertyRelative("description");
            if (desc != null) result["description"] = desc.stringValue;

            var targetId = elem.FindPropertyRelative("targetId");
            if (targetId != null) result["targetId"] = targetId.stringValue;

            var targetTag = elem.FindPropertyRelative("targetTag");
            if (targetTag != null) result["targetTag"] = targetTag.stringValue;

            var requiredCount = elem.FindPropertyRelative("requiredCount");
            if (requiredCount != null) result["requiredCount"] = requiredCount.intValue;

            return result;
        }

        private Dictionary<string, object> SerializePrerequisiteFromProperty(SerializedProperty elem)
        {
            var result = new Dictionary<string, object>();

            var typeProp = elem.FindPropertyRelative("type");
            if (typeProp != null)
            {
                result["type"] = typeProp.enumValueIndex < typeProp.enumDisplayNames.Length
                    ? typeProp.enumDisplayNames[typeProp.enumValueIndex]
                    : "Custom";
            }

            var target = elem.FindPropertyRelative("target");
            if (target != null) result["target"] = target.stringValue;

            var value = elem.FindPropertyRelative("value");
            if (value != null) result["value"] = value.floatValue;

            var comparison = elem.FindPropertyRelative("comparison");
            if (comparison != null) result["comparison"] = comparison.stringValue;

            return result;
        }

        private Dictionary<string, object> SerializeRewardFromProperty(SerializedProperty elem)
        {
            var result = new Dictionary<string, object>();

            var rewardId = elem.FindPropertyRelative("rewardId");
            if (rewardId != null) result["rewardId"] = rewardId.stringValue;

            var typeProp = elem.FindPropertyRelative("type");
            if (typeProp != null)
            {
                result["type"] = typeProp.enumValueIndex < typeProp.enumDisplayNames.Length
                    ? typeProp.enumDisplayNames[typeProp.enumValueIndex]
                    : "Custom";
            }

            var target = elem.FindPropertyRelative("target");
            if (target != null) result["target"] = target.stringValue;

            var itemId = elem.FindPropertyRelative("itemId");
            if (itemId != null) result["itemId"] = itemId.stringValue;

            var amount = elem.FindPropertyRelative("amount");
            if (amount != null) result["amount"] = amount.floatValue;

            return result;
        }

        #endregion

        #region Property Helpers

        private void SetPropertyIfExists(SerializedObject so, string propertyName, string value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null && prop.propertyType == SerializedPropertyType.String)
                prop.stringValue = value;
        }

        private void SetBoolPropertyIfExists(SerializedObject so, string propertyName, bool value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null && prop.propertyType == SerializedPropertyType.Boolean)
                prop.boolValue = value;
        }

        private void SetIntPropertyIfExists(SerializedObject so, string propertyName, int value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null && prop.propertyType == SerializedPropertyType.Integer)
                prop.intValue = value;
        }

        private void SetRelativePropertyIfExists(SerializedProperty parent, string propertyName, string value)
        {
            var prop = parent.FindPropertyRelative(propertyName);
            if (prop != null && prop.propertyType == SerializedPropertyType.String)
                prop.stringValue = value;
        }

        private void SetFieldViaReflection(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
                field.SetValue(target, value);
        }

        #endregion

        #region Parse Helpers

        private string ParseQuestCategory(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "main" => "Main",
                "side" => "Side",
                "daily" => "Daily",
                "weekly" => "Weekly",
                "event" => "Event",
                "tutorial" => "Tutorial",
                "hidden" => "Hidden",
                "custom" => "Custom",
                _ => "Side"
            };
        }

        private string ParseObjectiveType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "kill" => "Kill",
                "collect" => "Collect",
                "talk" => "Talk",
                "location" => "Location",
                "interact" => "Interact",
                "escort" => "Escort",
                "defend" => "Defend",
                "deliver" => "Deliver",
                "explore" => "Explore",
                "craft" => "Craft",
                "custom" => "Custom",
                _ => "Custom"
            };
        }

        private string ParsePrerequisiteType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "level" => "Level",
                "quest" => "Quest",
                "resource" => "Resource",
                "item" => "Item",
                "achievement" => "Achievement",
                "reputation" => "Reputation",
                "custom" => "Custom",
                _ => "Custom"
            };
        }

        private string ParseRewardType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "resource" => "Resource",
                "item" => "Item",
                "experience" => "Experience",
                "reputation" => "Reputation",
                "unlock" => "Unlock",
                "custom" => "Custom",
                _ => "Custom"
            };
        }

        private string ParseComparisonOperator(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "equals" or "==" or "eq" => "==",
                "notequals" or "!=" or "ne" => "!=",
                "greaterthan" or ">" or "gt" => ">",
                "lessthan" or "<" or "lt" => "<",
                "greaterorequal" or ">=" or "gte" => ">=",
                "lessorequal" or "<=" or "lte" => "<=",
                _ => ">="
            };
        }

        #endregion
    }
}
