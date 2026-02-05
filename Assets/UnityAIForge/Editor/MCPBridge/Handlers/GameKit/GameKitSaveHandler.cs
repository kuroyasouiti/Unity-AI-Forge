using System;
using System.Collections.Generic;
using System.IO;
using MCP.Editor.Base;
using UnityAIForge.GameKit;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Save handler: create and manage save/load systems.
    /// Provides declarative save profile configuration and save slot management.
    /// </summary>
    public class GameKitSaveHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "createProfile", "updateProfile", "inspectProfile", "deleteProfile",
            "addTarget", "removeTarget", "clearTargets",
            "save", "load", "listSlots", "deleteSlot",
            "createManager", "inspectManager", "deleteManager",
            "findByProfileId"
        };

        public override string Category => "gamekitSave";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "createProfile" => CreateProfile(payload),
                "updateProfile" => UpdateProfile(payload),
                "inspectProfile" => InspectProfile(payload),
                "deleteProfile" => DeleteProfile(payload),
                "addTarget" => AddTarget(payload),
                "removeTarget" => RemoveTarget(payload),
                "clearTargets" => ClearTargets(payload),
                "save" => ExecuteSave(payload),
                "load" => ExecuteLoad(payload),
                "listSlots" => ListSlots(payload),
                "deleteSlot" => DeleteSlot(payload),
                "createManager" => CreateManager(payload),
                "inspectManager" => InspectManager(payload),
                "deleteManager" => DeleteManager(payload),
                "findByProfileId" => FindByProfileId(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit Save operation: {operation}")
            };
        }

        #region Profile Operations

        private object CreateProfile(Dictionary<string, object> payload)
        {
            var profileId = GetString(payload, "profileId");
            if (string.IsNullOrEmpty(profileId))
            {
                profileId = $"SaveProfile_{Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            var assetPath = GetString(payload, "assetPath");
            if (string.IsNullOrEmpty(assetPath))
            {
                assetPath = $"Assets/GameKit/SaveProfiles/{profileId}.asset";
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Check if already exists
            var existingAsset = AssetDatabase.LoadAssetAtPath<GameKitSaveProfile>(assetPath);
            if (existingAsset != null)
            {
                throw new InvalidOperationException($"SaveProfile already exists at: {assetPath}");
            }

            // Create asset
            var profile = ScriptableObject.CreateInstance<GameKitSaveProfile>();
            profile.Initialize(profileId);

            // Add targets if provided
            if (payload.TryGetValue("saveTargets", out var targetsObj) && targetsObj is List<object> targetsList)
            {
                foreach (var targetObj in targetsList)
                {
                    if (targetObj is Dictionary<string, object> targetDict)
                    {
                        var target = CreateSaveTarget(targetDict);
                        profile.AddTarget(target);
                    }
                }
            }

            // Configure auto-save
            if (payload.TryGetValue("autoSave", out var autoSaveObj) && autoSaveObj is Dictionary<string, object> autoSaveDict)
            {
                ConfigureAutoSave(profile, autoSaveDict);
            }

            AssetDatabase.CreateAsset(profile, assetPath);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("profileId", profileId),
                ("assetPath", assetPath),
                ("targetCount", profile.SaveTargets.Count)
            );
        }

        private object UpdateProfile(Dictionary<string, object> payload)
        {
            var profile = ResolveProfile(payload);
            if (profile == null)
            {
                throw new InvalidOperationException("Could not find SaveProfile.");
            }

            Undo.RecordObject(profile, "Update SaveProfile");

            // Update auto-save settings
            if (payload.TryGetValue("autoSave", out var autoSaveObj) && autoSaveObj is Dictionary<string, object> autoSaveDict)
            {
                ConfigureAutoSave(profile, autoSaveDict);
            }

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("profileId", profile.ProfileId),
                ("updated", true)
            );
        }

        private object InspectProfile(Dictionary<string, object> payload)
        {
            var profile = ResolveProfile(payload);
            if (profile == null)
            {
                throw new InvalidOperationException("Could not find SaveProfile.");
            }

            var targets = new List<object>();
            foreach (var target in profile.SaveTargets)
            {
                targets.Add(new Dictionary<string, object>
                {
                    { "type", target.type.ToString() },
                    { "saveKey", target.saveKey },
                    { "gameObjectPath", target.gameObjectPath },
                    { "componentType", target.componentType },
                    { "properties", target.properties }
                });
            }

            return CreateSuccessResponse(
                ("profileId", profile.ProfileId),
                ("assetPath", AssetDatabase.GetAssetPath(profile)),
                ("targets", targets),
                ("autoSave", new Dictionary<string, object>
                {
                    { "enabled", profile.AutoSaveConfig.enabled },
                    { "intervalSeconds", profile.AutoSaveConfig.intervalSeconds },
                    { "onSceneChange", profile.AutoSaveConfig.onSceneChange },
                    { "onApplicationPause", profile.AutoSaveConfig.onApplicationPause },
                    { "autoSaveSlotId", profile.AutoSaveConfig.autoSaveSlotId }
                })
            );
        }

        private object DeleteProfile(Dictionary<string, object> payload)
        {
            var profile = ResolveProfile(payload);
            if (profile == null)
            {
                throw new InvalidOperationException("Could not find SaveProfile.");
            }

            var assetPath = AssetDatabase.GetAssetPath(profile);
            var profileId = profile.ProfileId;

            AssetDatabase.DeleteAsset(assetPath);

            return CreateSuccessResponse(
                ("profileId", profileId),
                ("assetPath", assetPath),
                ("deleted", true)
            );
        }

        #endregion

        #region Target Operations

        private object AddTarget(Dictionary<string, object> payload)
        {
            var profile = ResolveProfile(payload);
            if (profile == null)
            {
                throw new InvalidOperationException("Could not find SaveProfile.");
            }

            var targetDict = payload.TryGetValue("target", out var tObj) && tObj is Dictionary<string, object> dict
                ? dict
                : payload;

            var target = CreateSaveTarget(targetDict);

            Undo.RecordObject(profile, "Add Save Target");
            profile.AddTarget(target);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("profileId", profile.ProfileId),
                ("saveKey", target.saveKey),
                ("targetType", target.type.ToString()),
                ("added", true)
            );
        }

        private object RemoveTarget(Dictionary<string, object> payload)
        {
            var profile = ResolveProfile(payload);
            if (profile == null)
            {
                throw new InvalidOperationException("Could not find SaveProfile.");
            }

            var saveKey = GetString(payload, "saveKey");
            if (string.IsNullOrEmpty(saveKey))
            {
                throw new InvalidOperationException("saveKey is required to remove a target.");
            }

            Undo.RecordObject(profile, "Remove Save Target");
            var removed = profile.RemoveTarget(saveKey);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("profileId", profile.ProfileId),
                ("saveKey", saveKey),
                ("removed", removed)
            );
        }

        private object ClearTargets(Dictionary<string, object> payload)
        {
            var profile = ResolveProfile(payload);
            if (profile == null)
            {
                throw new InvalidOperationException("Could not find SaveProfile.");
            }

            Undo.RecordObject(profile, "Clear Save Targets");
            profile.ClearTargets();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("profileId", profile.ProfileId),
                ("cleared", true)
            );
        }

        #endregion

        #region Save/Load Operations

        private object ExecuteSave(Dictionary<string, object> payload)
        {
            var manager = ResolveManager(payload);
            var profileId = GetString(payload, "profileId");
            var slotId = GetString(payload, "slotId") ?? "default";

            if (string.IsNullOrEmpty(profileId))
            {
                throw new InvalidOperationException("profileId is required for save operation.");
            }

            var success = manager.Save(profileId, slotId);

            return CreateSuccessResponse(
                ("saved", success),
                ("profileId", profileId),
                ("slotId", slotId),
                ("note", success ? "Save executed." : "Save failed. Check profile registration.")
            );
        }

        private object ExecuteLoad(Dictionary<string, object> payload)
        {
            var manager = ResolveManager(payload);
            var profileId = GetString(payload, "profileId");
            var slotId = GetString(payload, "slotId") ?? "default";

            if (string.IsNullOrEmpty(profileId))
            {
                throw new InvalidOperationException("profileId is required for load operation.");
            }

            var success = manager.Load(profileId, slotId);

            return CreateSuccessResponse(
                ("loaded", success),
                ("profileId", profileId),
                ("slotId", slotId),
                ("note", success ? "Load executed." : "No save data found for this slot.")
            );
        }

        private object ListSlots(Dictionary<string, object> payload)
        {
            var manager = ResolveManager(payload);
            var profileId = GetString(payload, "profileId");

            if (string.IsNullOrEmpty(profileId))
            {
                throw new InvalidOperationException("profileId is required to list slots.");
            }

            var slots = manager.GetSlots(profileId);

            var slotList = new List<object>();
            foreach (var slot in slots)
            {
                slotList.Add(new Dictionary<string, object>
                {
                    { "slotId", slot.slotId },
                    { "saveTime", slot.timestamp.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "fileSize", slot.fileSize }
                });
            }

            return CreateSuccessResponse(
                ("profileId", profileId),
                ("slots", slotList),
                ("count", slotList.Count)
            );
        }

        private object DeleteSlot(Dictionary<string, object> payload)
        {
            var manager = ResolveManager(payload);
            var profileId = GetString(payload, "profileId");
            var slotId = GetString(payload, "slotId");

            if (string.IsNullOrEmpty(profileId))
            {
                throw new InvalidOperationException("profileId is required to delete a slot.");
            }
            if (string.IsNullOrEmpty(slotId))
            {
                throw new InvalidOperationException("slotId is required to delete a slot.");
            }

            var success = manager.DeleteSlot(profileId, slotId);

            return CreateSuccessResponse(
                ("profileId", profileId),
                ("slotId", slotId),
                ("deleted", success)
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
                // Create a new GameObject for the manager
                targetGo = new GameObject("SaveManager");
                Undo.RegisterCreatedObjectUndo(targetGo, "Create SaveManager");
            }
            else
            {
                targetGo = ResolveGameObject(targetPath);
                if (targetGo == null)
                {
                    throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
                }
            }

            var existingManager = targetGo.GetComponent<GameKitSaveManager>();
            if (existingManager != null)
            {
                throw new InvalidOperationException($"GameObject already has a GameKitSaveManager component.");
            }

            var manager = Undo.AddComponent<GameKitSaveManager>(targetGo);

            // Assign profile if provided
            var profileId = GetString(payload, "profileId");
            if (!string.IsNullOrEmpty(profileId))
            {
                var profile = FindProfileById(profileId);
                if (profile != null)
                {
                    var serializedManager = new SerializedObject(manager);
                    var registeredProfiles = serializedManager.FindProperty("registeredProfiles");
                    registeredProfiles.arraySize++;
                    registeredProfiles.GetArrayElementAtIndex(registeredProfiles.arraySize - 1).objectReferenceValue = profile;
                    serializedManager.ApplyModifiedProperties();
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("path", BuildGameObjectPath(targetGo)),
                ("created", true)
            );
        }

        private object InspectManager(Dictionary<string, object> payload)
        {
            var manager = ResolveManager(payload);

            var serializedManager = new SerializedObject(manager);
            var registeredProfilesProp = serializedManager.FindProperty("registeredProfiles");

            var profileIds = new List<string>();
            for (int i = 0; i < registeredProfilesProp.arraySize; i++)
            {
                var profileRef = registeredProfilesProp.GetArrayElementAtIndex(i).objectReferenceValue as GameKitSaveProfile;
                if (profileRef != null && !string.IsNullOrEmpty(profileRef.ProfileId))
                {
                    profileIds.Add(profileRef.ProfileId);
                }
            }

            return CreateSuccessResponse(
                ("path", BuildGameObjectPath(manager.gameObject)),
                ("registeredProfiles", profileIds),
                ("profileCount", profileIds.Count)
            );
        }

        private object DeleteManager(Dictionary<string, object> payload)
        {
            var manager = ResolveManager(payload);
            var path = BuildGameObjectPath(manager.gameObject);

            Undo.DestroyObjectImmediate(manager);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);

            return CreateSuccessResponse(
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Find

        private object FindByProfileId(Dictionary<string, object> payload)
        {
            var profileId = GetString(payload, "profileId");
            if (string.IsNullOrEmpty(profileId))
            {
                throw new InvalidOperationException("profileId is required for findByProfileId.");
            }

            var profile = FindProfileById(profileId);
            if (profile == null)
            {
                return CreateSuccessResponse(("found", false), ("profileId", profileId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("profileId", profile.ProfileId),
                ("assetPath", AssetDatabase.GetAssetPath(profile)),
                ("targetCount", profile.SaveTargets.Count)
            );
        }

        #endregion

        #region Helpers

        private GameKitSaveProfile ResolveProfile(Dictionary<string, object> payload)
        {
            // Try by profileId
            var profileId = GetString(payload, "profileId");
            if (!string.IsNullOrEmpty(profileId))
            {
                return FindProfileById(profileId);
            }

            // Try by assetPath
            var assetPath = GetString(payload, "assetPath");
            if (!string.IsNullOrEmpty(assetPath))
            {
                return AssetDatabase.LoadAssetAtPath<GameKitSaveProfile>(assetPath);
            }

            return null;
        }

        private GameKitSaveProfile FindProfileById(string profileId)
        {
            var guids = AssetDatabase.FindAssets("t:GameKitSaveProfile");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<GameKitSaveProfile>(path);
                if (profile != null && profile.ProfileId == profileId)
                {
                    return profile;
                }
            }
            return null;
        }

        private GameKitSaveManager ResolveManager(Dictionary<string, object> payload)
        {
            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var manager = targetGo.GetComponent<GameKitSaveManager>();
                    if (manager != null)
                    {
                        return manager;
                    }
                }
            }

            // Find any manager in scene
            var managers = UnityEngine.Object.FindObjectsByType<GameKitSaveManager>(FindObjectsSortMode.None);
            if (managers.Length > 0)
            {
                return managers[0];
            }

            throw new InvalidOperationException("No GameKitSaveManager found in scene.");
        }

        private GameKitSaveProfile.SaveTarget CreateSaveTarget(Dictionary<string, object> dict)
        {
            var target = new GameKitSaveProfile.SaveTarget();

            // Type
            var typeStr = GetStringFromDict(dict, "type", "transform");
            target.type = typeStr.ToLowerInvariant() switch
            {
                "transform" => GameKitSaveProfile.SaveTargetType.Transform,
                "component" => GameKitSaveProfile.SaveTargetType.Component,
                "resourcemanager" => GameKitSaveProfile.SaveTargetType.ResourceManager,
                "health" => GameKitSaveProfile.SaveTargetType.Health,
                "sceneflow" => GameKitSaveProfile.SaveTargetType.SceneFlow,
                "inventory" => GameKitSaveProfile.SaveTargetType.Inventory,
                "playerprefs" => GameKitSaveProfile.SaveTargetType.PlayerPrefs,
                _ => GameKitSaveProfile.SaveTargetType.Transform
            };

            // Common
            target.saveKey = GetStringFromDict(dict, "saveKey", "");
            target.gameObjectPath = GetStringFromDict(dict, "gameObjectPath", "");

            // Transform
            if (dict.TryGetValue("savePosition", out var posBool))
                target.savePosition = Convert.ToBoolean(posBool);
            if (dict.TryGetValue("saveRotation", out var rotBool))
                target.saveRotation = Convert.ToBoolean(rotBool);
            if (dict.TryGetValue("saveScale", out var scaleBool))
                target.saveScale = Convert.ToBoolean(scaleBool);

            // Component
            target.componentType = GetStringFromDict(dict, "componentType", "");
            if (dict.TryGetValue("properties", out var propsObj) && propsObj is List<object> propsList)
            {
                target.properties = new List<string>();
                foreach (var prop in propsList)
                {
                    target.properties.Add(prop.ToString());
                }
            }

            // GameKit integration
            target.resourceManagerId = GetStringFromDict(dict, "resourceManagerId", "");
            target.healthId = GetStringFromDict(dict, "healthId", "");
            target.sceneFlowId = GetStringFromDict(dict, "sceneFlowId", "");
            target.inventoryId = GetStringFromDict(dict, "inventoryId", "");

            return target;
        }

        private void ConfigureAutoSave(GameKitSaveProfile profile, Dictionary<string, object> dict)
        {
            var serialized = new SerializedObject(profile);
            var autoSaveProp = serialized.FindProperty("autoSave");

            if (dict.TryGetValue("enabled", out var enabledObj))
            {
                autoSaveProp.FindPropertyRelative("enabled").boolValue = Convert.ToBoolean(enabledObj);
            }

            if (dict.TryGetValue("intervalSeconds", out var intervalObj))
            {
                autoSaveProp.FindPropertyRelative("intervalSeconds").floatValue = Convert.ToSingle(intervalObj);
            }

            if (dict.TryGetValue("onSceneChange", out var sceneChangeObj))
            {
                autoSaveProp.FindPropertyRelative("onSceneChange").boolValue = Convert.ToBoolean(sceneChangeObj);
            }

            if (dict.TryGetValue("onApplicationPause", out var pauseObj))
            {
                autoSaveProp.FindPropertyRelative("onApplicationPause").boolValue = Convert.ToBoolean(pauseObj);
            }

            if (dict.TryGetValue("autoSaveSlotId", out var slotIdObj))
            {
                autoSaveProp.FindPropertyRelative("autoSaveSlotId").stringValue = slotIdObj.ToString();
            }

            serialized.ApplyModifiedProperties();
        }

        private string GetStringFromDict(Dictionary<string, object> dict, string key, string defaultValue)
        {
            return dict.TryGetValue(key, out var value) ? value?.ToString() ?? defaultValue : defaultValue;
        }

        #endregion
    }
}
