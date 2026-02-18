using System;
using System.Collections.Generic;
using System.IO;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Save handler: create and manage save/load systems.
    /// Provides declarative save profile configuration and save slot management.
    /// Uses code generation to produce standalone scripts with zero package dependency.
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

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "createManager" || operation == "createProfile";

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

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(profileId, "SaveProfile");

            var assetPath = GetString(payload, "assetPath");
            if (string.IsNullOrEmpty(assetPath))
            {
                assetPath = $"Assets/GameKit/SaveProfiles/{className}.asset";
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Check if asset already exists at path
            var existingAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (existingAsset != null)
            {
                throw new InvalidOperationException($"SaveProfile already exists at: {assetPath}");
            }

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "PROFILE_ID", profileId }
            };

            var outputDir = GetString(payload, "outputPath");

            // Generate the SaveProfile script via ScriptGenerator
            var result = ScriptGenerator.Generate(null, "SaveProfile", className, profileId, variables, outputDir);
            if (!result.Success)
            {
                throw new InvalidOperationException(result.ErrorMessage ?? "Failed to generate SaveProfile script.");
            }

            // Try to create the asset if the type is already compiled
            var profileType = ScriptGenerator.ResolveGeneratedType(className);
            var response = new Dictionary<string, object>
            {
                ["success"] = true,
                ["profileId"] = profileId,
                ["className"] = className,
                ["scriptPath"] = result.ScriptPath
            };

            if (profileType != null)
            {
                var profile = ScriptableObject.CreateInstance(profileType);
                if (profile != null)
                {
                    // Set profileId via SerializedObject
                    var so = new SerializedObject(profile);
                    var profileIdProp = so.FindProperty("profileId");
                    if (profileIdProp != null)
                    {
                        profileIdProp.stringValue = profileId;
                    }

                    // Add targets if provided
                    if (payload.TryGetValue("saveTargets", out var targetsObj) && targetsObj is List<object> targetsList)
                    {
                        var targetsProp = so.FindProperty("saveTargets");
                        if (targetsProp != null)
                        {
                            int idx = 0;
                            foreach (var targetObj in targetsList)
                            {
                                if (targetObj is Dictionary<string, object> targetDict)
                                {
                                    targetsProp.InsertArrayElementAtIndex(idx);
                                    var element = targetsProp.GetArrayElementAtIndex(idx);
                                    SetSaveTargetProperties(element, targetDict);
                                    idx++;
                                }
                            }
                        }
                    }

                    // Configure auto-save
                    if (payload.TryGetValue("autoSave", out var autoSaveObj) && autoSaveObj is Dictionary<string, object> autoSaveDict)
                    {
                        ConfigureAutoSave(so, autoSaveDict);
                    }

                    so.ApplyModifiedProperties();

                    AssetDatabase.CreateAsset(profile, assetPath);
                    AssetDatabase.SaveAssets();

                    response["assetPath"] = assetPath;
                    response["assetCreated"] = true;
                    response["compilationRequired"] = false;

                    // Count targets
                    var countSo = new SerializedObject(profile);
                    var countProp = countSo.FindProperty("saveTargets");
                    response["targetCount"] = countProp != null ? countProp.arraySize : 0;
                }
            }
            else
            {
                response["assetCreated"] = false;
                response["compilationRequired"] = true;
                response["note"] = "SaveProfile script generated. Asset will be created after compilation.";
            }

            return response;
        }

        private object UpdateProfile(Dictionary<string, object> payload)
        {
            var profile = ResolveProfile(payload);
            if (profile == null)
            {
                throw new InvalidOperationException("Could not find SaveProfile.");
            }

            Undo.RecordObject(profile, "Update SaveProfile");

            var so = new SerializedObject(profile);

            // Update auto-save settings
            if (payload.TryGetValue("autoSave", out var autoSaveObj) && autoSaveObj is Dictionary<string, object> autoSaveDict)
            {
                ConfigureAutoSave(so, autoSaveDict);
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            var profileIdProp = so.FindProperty("profileId");
            var profileIdValue = profileIdProp != null ? profileIdProp.stringValue : "unknown";

            return CreateSuccessResponse(
                ("profileId", profileIdValue),
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

            var so = new SerializedObject(profile);
            var profileIdProp = so.FindProperty("profileId");
            var profileIdValue = profileIdProp != null ? profileIdProp.stringValue : "unknown";

            var targets = new List<object>();
            var targetsProp = so.FindProperty("saveTargets");
            if (targetsProp != null)
            {
                for (int i = 0; i < targetsProp.arraySize; i++)
                {
                    var element = targetsProp.GetArrayElementAtIndex(i);
                    var typeProp = element.FindPropertyRelative("type");
                    var typeStr = typeProp != null && typeProp.enumValueIndex < typeProp.enumDisplayNames.Length
                        ? typeProp.enumDisplayNames[typeProp.enumValueIndex]
                        : "Transform";

                    var propsList = new List<string>();
                    var propsProp = element.FindPropertyRelative("properties");
                    if (propsProp != null)
                    {
                        for (int j = 0; j < propsProp.arraySize; j++)
                        {
                            propsList.Add(propsProp.GetArrayElementAtIndex(j).stringValue);
                        }
                    }

                    targets.Add(new Dictionary<string, object>
                    {
                        { "type", typeStr },
                        { "saveKey", element.FindPropertyRelative("saveKey")?.stringValue ?? "" },
                        { "gameObjectPath", element.FindPropertyRelative("gameObjectPath")?.stringValue ?? "" },
                        { "componentType", element.FindPropertyRelative("componentType")?.stringValue ?? "" },
                        { "properties", propsList }
                    });
                }
            }

            var autoSaveProp = so.FindProperty("autoSave");
            var autoSaveInfo = new Dictionary<string, object>();
            if (autoSaveProp != null)
            {
                autoSaveInfo["enabled"] = autoSaveProp.FindPropertyRelative("enabled")?.boolValue ?? false;
                autoSaveInfo["intervalSeconds"] = autoSaveProp.FindPropertyRelative("intervalSeconds")?.floatValue ?? 60f;
                autoSaveInfo["onSceneChange"] = autoSaveProp.FindPropertyRelative("onSceneChange")?.boolValue ?? false;
                autoSaveInfo["onApplicationPause"] = autoSaveProp.FindPropertyRelative("onApplicationPause")?.boolValue ?? true;
                autoSaveInfo["autoSaveSlotId"] = autoSaveProp.FindPropertyRelative("autoSaveSlotId")?.stringValue ?? "autosave";
            }

            return CreateSuccessResponse(
                ("profileId", profileIdValue),
                ("assetPath", AssetDatabase.GetAssetPath(profile)),
                ("targets", targets),
                ("autoSave", autoSaveInfo)
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
            var so = new SerializedObject(profile);
            var profileIdProp = so.FindProperty("profileId");
            var profileIdValue = profileIdProp != null ? profileIdProp.stringValue : "unknown";

            AssetDatabase.DeleteAsset(assetPath);

            // Also clean up the generated script from tracker
            ScriptGenerator.Delete(profileIdValue);

            return CreateSuccessResponse(
                ("profileId", profileIdValue),
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

            Undo.RecordObject(profile, "Add Save Target");

            var so = new SerializedObject(profile);
            var targetsProp = so.FindProperty("saveTargets");
            if (targetsProp == null)
            {
                throw new InvalidOperationException("SaveProfile does not have saveTargets property.");
            }

            var newIndex = targetsProp.arraySize;
            targetsProp.InsertArrayElementAtIndex(newIndex);
            var element = targetsProp.GetArrayElementAtIndex(newIndex);
            SetSaveTargetProperties(element, targetDict);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            var profileIdProp = so.FindProperty("profileId");
            var profileIdValue = profileIdProp != null ? profileIdProp.stringValue : "unknown";
            var saveKey = GetStringFromDict(targetDict, "saveKey", "");
            var typeStr = GetStringFromDict(targetDict, "type", "transform");

            return CreateSuccessResponse(
                ("profileId", profileIdValue),
                ("saveKey", saveKey),
                ("targetType", ParseSaveTargetType(typeStr)),
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

            var so = new SerializedObject(profile);
            var targetsProp = so.FindProperty("saveTargets");
            bool removed = false;
            if (targetsProp != null)
            {
                for (int i = targetsProp.arraySize - 1; i >= 0; i--)
                {
                    var element = targetsProp.GetArrayElementAtIndex(i);
                    var keyProp = element.FindPropertyRelative("saveKey");
                    if (keyProp != null && keyProp.stringValue == saveKey)
                    {
                        targetsProp.DeleteArrayElementAtIndex(i);
                        removed = true;
                    }
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            var profileIdProp = so.FindProperty("profileId");
            var profileIdValue = profileIdProp != null ? profileIdProp.stringValue : "unknown";

            return CreateSuccessResponse(
                ("profileId", profileIdValue),
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

            var so = new SerializedObject(profile);
            var targetsProp = so.FindProperty("saveTargets");
            if (targetsProp != null)
            {
                targetsProp.ClearArray();
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            var profileIdProp = so.FindProperty("profileId");
            var profileIdValue = profileIdProp != null ? profileIdProp.stringValue : "unknown";

            return CreateSuccessResponse(
                ("profileId", profileIdValue),
                ("cleared", true)
            );
        }

        #endregion

        #region Save/Load Operations

        private object ExecuteSave(Dictionary<string, object> payload)
        {
            var component = ResolveManagerComponent(payload);
            var slotId = GetString(payload, "slotId") ?? "default";

            // Invoke Save(slotId) via reflection
            var saveMethod = component.GetType().GetMethod("Save",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null, new[] { typeof(string) }, null);
            if (saveMethod == null)
            {
                throw new InvalidOperationException("SaveManager component does not have a Save(string) method.");
            }

            saveMethod.Invoke(component, new object[] { slotId });

            var so = new SerializedObject(component);
            var saveManagerId = so.FindProperty("saveManagerId")?.stringValue ?? "unknown";

            return CreateSuccessResponse(
                ("saved", true),
                ("saveManagerId", saveManagerId),
                ("slotId", slotId),
                ("note", "Save executed.")
            );
        }

        private object ExecuteLoad(Dictionary<string, object> payload)
        {
            var component = ResolveManagerComponent(payload);
            var slotId = GetString(payload, "slotId") ?? "default";

            // Invoke Load(slotId) via reflection
            var loadMethod = component.GetType().GetMethod("Load",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null, new[] { typeof(string) }, null);
            if (loadMethod == null)
            {
                throw new InvalidOperationException("SaveManager component does not have a Load(string) method.");
            }

            var result = loadMethod.Invoke(component, new object[] { slotId });
            var loaded = result != null;

            var so = new SerializedObject(component);
            var saveManagerId = so.FindProperty("saveManagerId")?.stringValue ?? "unknown";

            return CreateSuccessResponse(
                ("loaded", loaded),
                ("saveManagerId", saveManagerId),
                ("slotId", slotId),
                ("note", loaded ? "Load executed." : "No save data found for this slot.")
            );
        }

        private object ListSlots(Dictionary<string, object> payload)
        {
            var component = ResolveManagerComponent(payload);

            // Invoke GetSlots() via reflection
            var getSlotsMethod = component.GetType().GetMethod("GetSlots",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null, Type.EmptyTypes, null);
            if (getSlotsMethod == null)
            {
                throw new InvalidOperationException("SaveManager component does not have a GetSlots() method.");
            }

            var slotsResult = getSlotsMethod.Invoke(component, null);

            var slotList = new List<object>();
            if (slotsResult is System.Collections.IList slots)
            {
                foreach (var slot in slots)
                {
                    var slotType = slot.GetType();
                    var slotIdField = slotType.GetField("slotId");
                    var timestampField = slotType.GetField("timestamp");
                    var displayNameField = slotType.GetField("displayName");

                    slotList.Add(new Dictionary<string, object>
                    {
                        { "slotId", slotIdField?.GetValue(slot)?.ToString() ?? "" },
                        { "timestamp", timestampField?.GetValue(slot)?.ToString() ?? "" },
                        { "displayName", displayNameField?.GetValue(slot)?.ToString() ?? "" }
                    });
                }
            }

            var so = new SerializedObject(component);
            var saveManagerId = so.FindProperty("saveManagerId")?.stringValue ?? "unknown";

            return CreateSuccessResponse(
                ("saveManagerId", saveManagerId),
                ("slots", slotList),
                ("count", slotList.Count)
            );
        }

        private object DeleteSlot(Dictionary<string, object> payload)
        {
            var component = ResolveManagerComponent(payload);
            var slotId = GetString(payload, "slotId");

            if (string.IsNullOrEmpty(slotId))
            {
                throw new InvalidOperationException("slotId is required to delete a slot.");
            }

            // Invoke DeleteSlot(slotId) via reflection
            var deleteMethod = component.GetType().GetMethod("DeleteSlot",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null, new[] { typeof(string) }, null);
            if (deleteMethod == null)
            {
                throw new InvalidOperationException("SaveManager component does not have a DeleteSlot(string) method.");
            }

            deleteMethod.Invoke(component, new object[] { slotId });

            var so = new SerializedObject(component);
            var saveManagerId = so.FindProperty("saveManagerId")?.stringValue ?? "unknown";

            return CreateSuccessResponse(
                ("saveManagerId", saveManagerId),
                ("slotId", slotId),
                ("deleted", true)
            );
        }

        #endregion

        #region Manager Operations

        private object CreateManager(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            var saveManagerId = GetString(payload, "saveManagerId")
                ?? $"SaveManager_{Guid.NewGuid().ToString().Substring(0, 8)}";
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

            // Check if already has a save manager component
            var existingManager = CodeGenHelper.FindComponentByField(targetGo, "saveManagerId", null);
            if (existingManager != null)
            {
                throw new InvalidOperationException("GameObject already has a SaveManager component.");
            }

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(saveManagerId, "SaveManager");

            var persistent = GetBool(payload, "persistent", true);
            var saveDirectory = GetString(payload, "saveDirectory") ?? "Saves";
            var fileExtension = GetString(payload, "fileExtension") ?? ".sav";
            var autoSaveEnabled = GetBool(payload, "autoSaveEnabled");
            var autoSaveInterval = GetFloat(payload, "autoSaveInterval", 60f);
            var autoSaveSlotId = GetString(payload, "autoSaveSlotId") ?? "autosave";

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "SAVE_MANAGER_ID", saveManagerId },
                { "PERSISTENT", persistent.ToString().ToLowerInvariant() },
                { "SAVE_DIRECTORY", saveDirectory },
                { "FILE_EXTENSION", fileExtension },
                { "AUTO_SAVE_ENABLED", autoSaveEnabled.ToString().ToLowerInvariant() },
                { "AUTO_SAVE_INTERVAL", autoSaveInterval },
                { "AUTO_SAVE_SLOT_ID", autoSaveSlotId }
            };

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "SaveManager", saveManagerId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate SaveManager script.");
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["saveManagerId"] = saveManagerId;
            result["path"] = BuildGameObjectPath(targetGo);

            return result;
        }

        private object InspectManager(Dictionary<string, object> payload)
        {
            var component = ResolveManagerComponent(payload);
            var so = new SerializedObject(component);

            var saveManagerId = so.FindProperty("saveManagerId")?.stringValue ?? "unknown";
            var persistent = so.FindProperty("persistent")?.boolValue ?? true;
            var saveDirectory = so.FindProperty("saveDirectory")?.stringValue ?? "Saves";
            var fileExtension = so.FindProperty("fileExtension")?.stringValue ?? ".sav";
            var autoSaveEnabled = so.FindProperty("autoSaveEnabled")?.boolValue ?? false;
            var autoSaveInterval = so.FindProperty("autoSaveInterval")?.floatValue ?? 60f;
            var autoSaveSlotId = so.FindProperty("autoSaveSlotId")?.stringValue ?? "autosave";

            var info = new Dictionary<string, object>
            {
                { "saveManagerId", saveManagerId },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "persistent", persistent },
                { "saveDirectory", saveDirectory },
                { "fileExtension", fileExtension },
                { "autoSave", new Dictionary<string, object>
                    {
                        { "enabled", autoSaveEnabled },
                        { "interval", autoSaveInterval },
                        { "slotId", autoSaveSlotId }
                    }
                }
            };

            return CreateSuccessResponse(("saveManager", info));
        }

        private object DeleteManager(Dictionary<string, object> payload)
        {
            var component = ResolveManagerComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var so = new SerializedObject(component);
            var saveManagerId = so.FindProperty("saveManagerId")?.stringValue ?? "unknown";
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component);

            // Also clean up the generated script from tracker
            ScriptGenerator.Delete(saveManagerId);

            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("saveManagerId", saveManagerId),
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

            var so = new SerializedObject(profile);
            var targetsProp = so.FindProperty("saveTargets");
            var targetCount = targetsProp != null ? targetsProp.arraySize : 0;

            return CreateSuccessResponse(
                ("found", true),
                ("profileId", profileId),
                ("assetPath", AssetDatabase.GetAssetPath(profile)),
                ("targetCount", targetCount)
            );
        }

        #endregion

        #region Helpers

        private ScriptableObject ResolveProfile(Dictionary<string, object> payload)
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
                return AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            }

            return null;
        }

        private ScriptableObject FindProfileById(string profileId)
        {
            // Search all ScriptableObjects that have a profileId field matching the given value
            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null) continue;

                try
                {
                    var so = new SerializedObject(asset);
                    var profileIdProp = so.FindProperty("profileId");
                    if (profileIdProp != null && profileIdProp.propertyType == SerializedPropertyType.String
                        && profileIdProp.stringValue == profileId)
                    {
                        return asset;
                    }
                }
                catch
                {
                    // Skip assets that can't be serialized
                }
            }
            return null;
        }

        private Component ResolveManagerComponent(Dictionary<string, object> payload)
        {
            // Try by saveManagerId first
            var saveManagerId = GetString(payload, "saveManagerId");
            if (!string.IsNullOrEmpty(saveManagerId))
            {
                var managerById = CodeGenHelper.FindComponentInSceneByField("saveManagerId", saveManagerId);
                if (managerById != null)
                {
                    return managerById;
                }
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var managerByPath = CodeGenHelper.FindComponentByField(targetGo, "saveManagerId", null);
                    if (managerByPath != null)
                    {
                        return managerByPath;
                    }

                    throw new InvalidOperationException($"No SaveManager component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            // Find any manager in scene with a saveManagerId field
            var allMonoBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var comp in allMonoBehaviours)
            {
                if (comp == null) continue;
                try
                {
                    var so = new SerializedObject(comp);
                    var prop = so.FindProperty("saveManagerId");
                    if (prop != null && prop.propertyType == SerializedPropertyType.String)
                    {
                        return comp;
                    }
                }
                catch
                {
                    // Skip components that can't be serialized
                }
            }

            throw new InvalidOperationException("No SaveManager found in scene. Either saveManagerId or targetPath is required.");
        }

        private void SetSaveTargetProperties(SerializedProperty element, Dictionary<string, object> dict)
        {
            // Type
            var typeStr = GetStringFromDict(dict, "type", "transform");
            var typeParsed = ParseSaveTargetType(typeStr);
            var typeProp = element.FindPropertyRelative("type");
            if (typeProp != null)
            {
                var names = typeProp.enumDisplayNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], typeParsed, StringComparison.OrdinalIgnoreCase))
                    {
                        typeProp.enumValueIndex = i;
                        break;
                    }
                }
            }

            // Common
            var saveKeyProp = element.FindPropertyRelative("saveKey");
            if (saveKeyProp != null)
                saveKeyProp.stringValue = GetStringFromDict(dict, "saveKey", "");

            var goPathProp = element.FindPropertyRelative("gameObjectPath");
            if (goPathProp != null)
                goPathProp.stringValue = GetStringFromDict(dict, "gameObjectPath", "");

            // Transform
            if (dict.TryGetValue("savePosition", out var posBool))
            {
                var prop = element.FindPropertyRelative("savePosition");
                if (prop != null) prop.boolValue = Convert.ToBoolean(posBool);
            }
            if (dict.TryGetValue("saveRotation", out var rotBool))
            {
                var prop = element.FindPropertyRelative("saveRotation");
                if (prop != null) prop.boolValue = Convert.ToBoolean(rotBool);
            }
            if (dict.TryGetValue("saveScale", out var scaleBool))
            {
                var prop = element.FindPropertyRelative("saveScale");
                if (prop != null) prop.boolValue = Convert.ToBoolean(scaleBool);
            }

            // Component
            var compTypeProp = element.FindPropertyRelative("componentType");
            if (compTypeProp != null)
                compTypeProp.stringValue = GetStringFromDict(dict, "componentType", "");

            if (dict.TryGetValue("properties", out var propsObj) && propsObj is List<object> propsList)
            {
                var propsProp = element.FindPropertyRelative("properties");
                if (propsProp != null)
                {
                    propsProp.ClearArray();
                    for (int i = 0; i < propsList.Count; i++)
                    {
                        propsProp.InsertArrayElementAtIndex(i);
                        propsProp.GetArrayElementAtIndex(i).stringValue = propsList[i].ToString();
                    }
                }
            }

            // GameKit integration IDs
            var rmIdProp = element.FindPropertyRelative("resourceManagerId");
            if (rmIdProp != null)
                rmIdProp.stringValue = GetStringFromDict(dict, "resourceManagerId", "");

            var healthIdProp = element.FindPropertyRelative("healthId");
            if (healthIdProp != null)
                healthIdProp.stringValue = GetStringFromDict(dict, "healthId", "");

            var sfIdProp = element.FindPropertyRelative("sceneFlowId");
            if (sfIdProp != null)
                sfIdProp.stringValue = GetStringFromDict(dict, "sceneFlowId", "");

            var invIdProp = element.FindPropertyRelative("inventoryId");
            if (invIdProp != null)
                invIdProp.stringValue = GetStringFromDict(dict, "inventoryId", "");
        }

        private void ConfigureAutoSave(SerializedObject so, Dictionary<string, object> dict)
        {
            var autoSaveProp = so.FindProperty("autoSave");
            if (autoSaveProp == null) return;

            if (dict.TryGetValue("enabled", out var enabledObj))
            {
                var prop = autoSaveProp.FindPropertyRelative("enabled");
                if (prop != null) prop.boolValue = Convert.ToBoolean(enabledObj);
            }

            if (dict.TryGetValue("intervalSeconds", out var intervalObj))
            {
                var prop = autoSaveProp.FindPropertyRelative("intervalSeconds");
                if (prop != null) prop.floatValue = Convert.ToSingle(intervalObj);
            }

            if (dict.TryGetValue("onSceneChange", out var sceneChangeObj))
            {
                var prop = autoSaveProp.FindPropertyRelative("onSceneChange");
                if (prop != null) prop.boolValue = Convert.ToBoolean(sceneChangeObj);
            }

            if (dict.TryGetValue("onApplicationPause", out var pauseObj))
            {
                var prop = autoSaveProp.FindPropertyRelative("onApplicationPause");
                if (prop != null) prop.boolValue = Convert.ToBoolean(pauseObj);
            }

            if (dict.TryGetValue("autoSaveSlotId", out var slotIdObj))
            {
                var prop = autoSaveProp.FindPropertyRelative("autoSaveSlotId");
                if (prop != null) prop.stringValue = slotIdObj.ToString();
            }
        }

        private string ParseSaveTargetType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "transform" => "Transform",
                "component" => "Component",
                "resourcemanager" => "ResourceManager",
                "health" => "Health",
                "sceneflow" => "SceneFlow",
                "inventory" => "Inventory",
                "playerprefs" => "PlayerPrefs",
                _ => "Transform"
            };
        }

        private string GetStringFromDict(Dictionary<string, object> dict, string key, string defaultValue)
        {
            return dict.TryGetValue(key, out var value) ? value?.ToString() ?? defaultValue : defaultValue;
        }

        #endregion
    }
}
