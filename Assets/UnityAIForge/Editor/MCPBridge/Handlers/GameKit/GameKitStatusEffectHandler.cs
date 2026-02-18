using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Status Effect handler: create and manage status effect systems (buffs/debuffs).
    /// Uses code generation to produce standalone scripts with zero package dependency.
    /// </summary>
    public class GameKitStatusEffectHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "defineEffect", "updateEffect", "inspectEffect", "deleteEffect",
            "addModifier", "updateModifier", "removeModifier", "clearModifiers",
            "create", "update", "inspect", "delete",
            "applyEffect", "removeEffect", "clearEffects",
            "getActiveEffects", "getStatModifier",
            "findByEffectId", "findByReceiverId", "listEffects"
        };

        public override string Category => "gamekitStatusEffect";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create" || operation == "defineEffect";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "defineEffect" => DefineEffect(payload),
                "updateEffect" => UpdateEffect(payload),
                "inspectEffect" => InspectEffect(payload),
                "deleteEffect" => DeleteEffect(payload),
                "addModifier" => AddModifier(payload),
                "updateModifier" => UpdateModifier(payload),
                "removeModifier" => RemoveModifier(payload),
                "clearModifiers" => ClearModifiers(payload),
                "create" => CreateReceiver(payload),
                "update" => UpdateReceiver(payload),
                "inspect" => InspectReceiver(payload),
                "delete" => DeleteReceiver(payload),
                "applyEffect" => ApplyEffect(payload),
                "removeEffect" => RemoveEffect(payload),
                "clearEffects" => ClearEffects(payload),
                "getActiveEffects" => GetActiveEffects(payload),
                "getStatModifier" => GetStatModifier(payload),
                "findByEffectId" => FindByEffectId(payload),
                "findByReceiverId" => FindByReceiverId(payload),
                "listEffects" => ListEffects(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit Status Effect operation: {operation}")
            };
        }

        #region Effect Asset CRUD

        private object DefineEffect(Dictionary<string, object> payload)
        {
            var effectId = GetString(payload, "effectId") ?? $"Effect_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var assetPath = GetString(payload, "assetPath") ?? $"Assets/StatusEffects/{effectId}.asset";
            var outputDir = GetString(payload, "outputPath");

            // Check if asset already exists at path
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath) != null)
            {
                throw new InvalidOperationException($"Status effect asset already exists at: {assetPath}");
            }

            var displayName = GetString(payload, "displayName") ?? effectId;
            var effectType = ParseEffectType(GetString(payload, "type") ?? "buff");
            var effectCategory = ParseEffectCategory(GetString(payload, "category") ?? "generic");
            var duration = GetFloat(payload, "duration", 10f);
            var stackable = GetBool(payload, "stackable", false);
            var maxStacks = GetInt(payload, "maxStacks", 1);
            var tickInterval = GetFloat(payload, "tickInterval", 0f);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(effectId, "StatusEffectData");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "EFFECT_ID", effectId },
                { "DISPLAY_NAME", displayName },
                { "EFFECT_TYPE", effectType },
                { "EFFECT_CATEGORY", effectCategory },
                { "DURATION", duration },
                { "STACKABLE", stackable ? "true" : "false" },
                { "MAX_STACKS", maxStacks },
                { "TICK_INTERVAL", tickInterval }
            };

            // Generate the StatusEffectData ScriptableObject script
            var result = ScriptGenerator.Generate(null, "StatusEffectData", className, effectId, variables, outputDir);
            if (!result.Success)
            {
                throw new InvalidOperationException(result.ErrorMessage ?? "Failed to generate StatusEffectData script.");
            }

            // Try to create the asset if the type is already compiled
            var type = ScriptGenerator.ResolveGeneratedType(className);
            bool assetCreated = false;
            if (type != null)
            {
                var directory = Path.GetDirectoryName(assetPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var effect = ScriptableObject.CreateInstance(type);
                var so = new SerializedObject(effect);

                SetPropertyIfExists(so, "effectId", effectId);
                SetPropertyIfExists(so, "displayName", displayName);

                if (payload.TryGetValue("description", out var descObj))
                    SetPropertyIfExists(so, "description", descObj.ToString());

                SetEnumPropertyIfExists(so, "type", effectType);
                SetEnumPropertyIfExists(so, "category", effectCategory);

                so.FindProperty("duration").floatValue = duration;

                if (payload.TryGetValue("isPermanent", out var permObj))
                    so.FindProperty("isPermanent").boolValue = Convert.ToBoolean(permObj);

                so.FindProperty("stackable").boolValue = stackable;
                so.FindProperty("maxStacks").intValue = maxStacks;

                if (payload.TryGetValue("stackBehavior", out var stackBehaviorObj))
                    SetEnumPropertyIfExists(so, "stackBehavior", ParseStackBehavior(stackBehaviorObj.ToString()));

                so.FindProperty("tickInterval").floatValue = tickInterval;

                if (payload.TryGetValue("tickOnApply", out var tickApplyObj))
                    so.FindProperty("tickOnApply").boolValue = Convert.ToBoolean(tickApplyObj);

                if (payload.TryGetValue("particleEffectId", out var particleObj))
                    SetPropertyIfExists(so, "particleEffectId", particleObj.ToString());

                if (payload.TryGetValue("onApplyEffectId", out var applyEffectObj))
                    SetPropertyIfExists(so, "onApplyEffectId", applyEffectObj.ToString());

                if (payload.TryGetValue("onRemoveEffectId", out var removeEffectObj))
                    SetPropertyIfExists(so, "onRemoveEffectId", removeEffectObj.ToString());

                so.ApplyModifiedPropertiesWithoutUndo();

                // Add modifiers if provided
                if (payload.TryGetValue("modifiers", out var modsObj) && modsObj is List<object> modsList)
                {
                    AddModifiersToAsset(so, modsList);
                }

                AssetDatabase.CreateAsset(effect, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                assetCreated = true;
            }

            var modifierCount = 0;
            if (payload.TryGetValue("modifiers", out var modsCountObj) && modsCountObj is List<object> modsCountList)
                modifierCount = modsCountList.Count;

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("scriptPath", result.ScriptPath),
                ("className", result.ClassName),
                ("assetPath", assetCreated ? assetPath : ""),
                ("assetCreated", assetCreated),
                ("compilationRequired", !assetCreated),
                ("displayName", displayName),
                ("type", effectType),
                ("modifierCount", modifierCount)
            );
        }

        private object UpdateEffect(Dictionary<string, object> payload)
        {
            var effect = ResolveEffectAsset(payload);
            var so = new SerializedObject(effect);

            if (payload.TryGetValue("displayName", out var nameObj))
                SetPropertyIfExists(so, "displayName", nameObj.ToString());

            if (payload.TryGetValue("description", out var descObj))
                SetPropertyIfExists(so, "description", descObj.ToString());

            if (payload.TryGetValue("type", out var typeObj))
                SetEnumPropertyIfExists(so, "type", ParseEffectType(typeObj.ToString()));

            if (payload.TryGetValue("category", out var catObj))
                SetEnumPropertyIfExists(so, "category", ParseEffectCategory(catObj.ToString()));

            if (payload.TryGetValue("duration", out var durObj))
                so.FindProperty("duration").floatValue = Convert.ToSingle(durObj);

            if (payload.TryGetValue("stackable", out var stackObj))
                so.FindProperty("stackable").boolValue = Convert.ToBoolean(stackObj);

            if (payload.TryGetValue("maxStacks", out var maxStackObj))
                so.FindProperty("maxStacks").intValue = Convert.ToInt32(maxStackObj);

            if (payload.TryGetValue("tickInterval", out var tickObj))
                so.FindProperty("tickInterval").floatValue = Convert.ToSingle(tickObj);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(effect);
            AssetDatabase.SaveAssets();

            var effectIdProp = so.FindProperty("effectId");
            var effectId = effectIdProp != null ? effectIdProp.stringValue : "unknown";

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("updated", true)
            );
        }

        private object InspectEffect(Dictionary<string, object> payload)
        {
            var effect = ResolveEffectAsset(payload);
            var so = new SerializedObject(effect);

            var modifiers = new List<Dictionary<string, object>>();
            var modsProp = so.FindProperty("modifiers");
            if (modsProp != null && modsProp.isArray)
            {
                for (int i = 0; i < modsProp.arraySize; i++)
                {
                    var modElement = modsProp.GetArrayElementAtIndex(i);
                    modifiers.Add(SerializeModifierFromProperty(modElement));
                }
            }

            var typeProp = so.FindProperty("type");
            var categoryProp = so.FindProperty("category");
            var stackBehaviorProp = so.FindProperty("stackBehavior");

            return CreateSuccessResponse(
                ("effectId", GetStringProperty(so, "effectId")),
                ("assetPath", AssetDatabase.GetAssetPath(effect)),
                ("displayName", GetStringProperty(so, "displayName")),
                ("description", GetStringProperty(so, "description")),
                ("type", GetEnumDisplayName(typeProp)),
                ("category", GetEnumDisplayName(categoryProp)),
                ("duration", so.FindProperty("duration").floatValue),
                ("isPermanent", so.FindProperty("isPermanent").boolValue),
                ("stackable", so.FindProperty("stackable").boolValue),
                ("maxStacks", so.FindProperty("maxStacks").intValue),
                ("stackBehavior", GetEnumDisplayName(stackBehaviorProp)),
                ("tickInterval", so.FindProperty("tickInterval").floatValue),
                ("tickOnApply", so.FindProperty("tickOnApply").boolValue),
                ("modifierCount", modsProp != null ? modsProp.arraySize : 0),
                ("modifiers", modifiers)
            );
        }

        private object DeleteEffect(Dictionary<string, object> payload)
        {
            var effect = ResolveEffectAsset(payload);
            var assetPath = AssetDatabase.GetAssetPath(effect);
            var so = new SerializedObject(effect);
            var effectId = GetStringProperty(so, "effectId");

            AssetDatabase.DeleteAsset(assetPath);

            // Also clean up the generated script from tracker
            ScriptGenerator.Delete(effectId);

            AssetDatabase.Refresh();

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("assetPath", assetPath),
                ("deleted", true)
            );
        }

        #endregion

        #region Modifier Operations

        private object AddModifier(Dictionary<string, object> payload)
        {
            var effect = ResolveEffectAsset(payload);

            if (!payload.TryGetValue("modifier", out var modObj) || modObj is not Dictionary<string, object> modDict)
            {
                throw new InvalidOperationException("modifier data is required for addModifier operation.");
            }

            var so = new SerializedObject(effect);
            var modsProp = so.FindProperty("modifiers");
            if (modsProp == null || !modsProp.isArray)
            {
                throw new InvalidOperationException("modifiers property not found on effect asset.");
            }

            modsProp.InsertArrayElementAtIndex(modsProp.arraySize);
            var newElement = modsProp.GetArrayElementAtIndex(modsProp.arraySize - 1);
            SetModifierProperties(newElement, modDict);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(effect);
            AssetDatabase.SaveAssets();

            var effectId = GetStringProperty(so, "effectId");

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("modifierCount", modsProp.arraySize)
            );
        }

        private object UpdateModifier(Dictionary<string, object> payload)
        {
            var effect = ResolveEffectAsset(payload);
            var modifierIndex = GetInt(payload, "modifierIndex", -1);

            if (modifierIndex < 0)
            {
                throw new InvalidOperationException("modifierIndex is required for updateModifier operation.");
            }

            if (!payload.TryGetValue("modifier", out var modObj) || modObj is not Dictionary<string, object> modDict)
            {
                throw new InvalidOperationException("modifier data is required for updateModifier operation.");
            }

            var so = new SerializedObject(effect);
            var modsProp = so.FindProperty("modifiers");
            if (modsProp == null || !modsProp.isArray || modifierIndex >= modsProp.arraySize)
            {
                throw new InvalidOperationException($"Modifier at index {modifierIndex} not found.");
            }

            var element = modsProp.GetArrayElementAtIndex(modifierIndex);
            SetModifierProperties(element, modDict);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(effect);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("effectId", GetStringProperty(so, "effectId")),
                ("modifierIndex", modifierIndex),
                ("updated", true)
            );
        }

        private object RemoveModifier(Dictionary<string, object> payload)
        {
            var effect = ResolveEffectAsset(payload);
            var modifierIndex = GetInt(payload, "modifierIndex", -1);

            if (modifierIndex < 0)
            {
                throw new InvalidOperationException("modifierIndex is required for removeModifier operation.");
            }

            var so = new SerializedObject(effect);
            var modsProp = so.FindProperty("modifiers");
            if (modsProp == null || !modsProp.isArray || modifierIndex >= modsProp.arraySize)
            {
                throw new InvalidOperationException($"Modifier at index {modifierIndex} not found.");
            }

            modsProp.DeleteArrayElementAtIndex(modifierIndex);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(effect);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("effectId", GetStringProperty(so, "effectId")),
                ("modifierIndex", modifierIndex),
                ("removed", true),
                ("modifierCount", modsProp.arraySize)
            );
        }

        private object ClearModifiers(Dictionary<string, object> payload)
        {
            var effect = ResolveEffectAsset(payload);
            var so = new SerializedObject(effect);
            var modsProp = so.FindProperty("modifiers");
            if (modsProp != null && modsProp.isArray)
            {
                modsProp.ClearArray();
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(effect);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("effectId", GetStringProperty(so, "effectId")),
                ("cleared", true),
                ("modifierCount", 0)
            );
        }

        #endregion

        #region Receiver Operations

        private object CreateReceiver(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            if (string.IsNullOrEmpty(targetPath))
            {
                throw new InvalidOperationException("targetPath is required for create operation.");
            }

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
            {
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            // Check if already has a receiver component (by checking for receiverId field)
            var existingReceiver = CodeGenHelper.FindComponentByField(targetGo, "receiverId", null);
            if (existingReceiver != null)
            {
                throw new InvalidOperationException(
                    $"GameObject '{targetPath}' already has a StatusEffectReceiver component.");
            }

            var receiverId = GetString(payload, "receiverId") ?? $"Receiver_{Guid.NewGuid().ToString().Substring(0, 8)}";

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(receiverId, "StatusEffectReceiver");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "RECEIVER_ID", receiverId }
            };

            // Build properties to set after component is added
            var propertiesToSet = new Dictionary<string, object>();
            if (payload.TryGetValue("immuneEffects", out var immuneObj) && immuneObj is List<object>)
                propertiesToSet["immuneEffects"] = immuneObj;

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "StatusEffectReceiver", receiverId, className, variables, outputDir,
                propertiesToSet.Count > 0 ? propertiesToSet : null);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate StatusEffectReceiver script.");
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["receiverId"] = receiverId;
            result["path"] = BuildGameObjectPath(targetGo);
            result["created"] = true;

            return result;
        }

        private object UpdateReceiver(Dictionary<string, object> payload)
        {
            var component = ResolveReceiver(payload);

            Undo.RecordObject(component, "Update StatusEffectReceiver");

            var so = new SerializedObject(component);

            if (payload.TryGetValue("immuneEffects", out var immuneEffObj) && immuneEffObj is List<object> effList)
            {
                var prop = so.FindProperty("immuneEffects");
                if (prop != null && prop.isArray)
                {
                    prop.ClearArray();
                    foreach (var eff in effList)
                    {
                        prop.InsertArrayElementAtIndex(prop.arraySize);
                        prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = eff.ToString();
                    }
                }
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var receiverId = new SerializedObject(component).FindProperty("receiverId").stringValue;

            return CreateSuccessResponse(
                ("receiverId", receiverId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        private object InspectReceiver(Dictionary<string, object> payload)
        {
            var component = ResolveReceiver(payload);
            var so = new SerializedObject(component);

            var receiverId = GetStringProperty(so, "receiverId");

            // Immune effects list
            var immuneEffects = new List<string>();
            var immuneProp = so.FindProperty("immuneEffects");
            if (immuneProp != null && immuneProp.isArray)
            {
                for (int i = 0; i < immuneProp.arraySize; i++)
                {
                    immuneEffects.Add(immuneProp.GetArrayElementAtIndex(i).stringValue);
                }
            }

            return CreateSuccessResponse(
                ("receiverId", receiverId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("immuneEffects", immuneEffects),
                ("note", "Active effect state is only available at runtime.")
            );
        }

        private object DeleteReceiver(Dictionary<string, object> payload)
        {
            var component = ResolveReceiver(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var receiverId = new SerializedObject(component).FindProperty("receiverId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component);

            // Also clean up the generated script from tracker
            ScriptGenerator.Delete(receiverId);

            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("receiverId", receiverId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Runtime Operations

        private object ApplyEffect(Dictionary<string, object> payload)
        {
            var effectId = GetString(payload, "effectId");
            if (string.IsNullOrEmpty(effectId))
            {
                throw new InvalidOperationException("effectId is required for applyEffect operation.");
            }

            // Verify the effect asset exists
            ResolveEffectAsset(payload);

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("note", "Effect application only works in play mode. Use the receiver's ApplyEffect() method at runtime.")
            );
        }

        private object RemoveEffect(Dictionary<string, object> payload)
        {
            var effectId = GetString(payload, "effectId");
            return CreateSuccessResponse(
                ("effectId", effectId),
                ("note", "Effect removal only works in play mode.")
            );
        }

        private object ClearEffects(Dictionary<string, object> payload)
        {
            return CreateSuccessResponse(
                ("note", "Effect clearing only works in play mode.")
            );
        }

        private object GetActiveEffects(Dictionary<string, object> payload)
        {
            var component = ResolveReceiver(payload);
            var so = new SerializedObject(component);
            var receiverId = GetStringProperty(so, "receiverId");

            return CreateSuccessResponse(
                ("receiverId", receiverId),
                ("activeEffects", new List<Dictionary<string, object>>()),
                ("count", 0),
                ("note", "Active effects are only tracked at runtime.")
            );
        }

        private object GetStatModifier(Dictionary<string, object> payload)
        {
            var component = ResolveReceiver(payload);
            var statName = GetString(payload, "statName");

            if (string.IsNullOrEmpty(statName))
            {
                throw new InvalidOperationException("statName is required for getStatModifier operation.");
            }

            var so = new SerializedObject(component);
            var receiverId = GetStringProperty(so, "receiverId");

            // Try to call GetStatModifier via reflection at runtime
            float value = 0f;
            try
            {
                var method = component.GetType().GetMethod("GetStatModifier",
                    BindingFlags.Public | BindingFlags.Instance);
                if (method != null)
                {
                    var result = method.Invoke(component, new object[] { statName });
                    if (result is float f) value = f;
                }
            }
            catch
            {
                // Stat modifiers are runtime-only
            }

            return CreateSuccessResponse(
                ("receiverId", receiverId),
                ("statName", statName),
                ("modifier", value),
                ("note", "Stat modifiers are computed at runtime from active effects.")
            );
        }

        #endregion

        #region Find Operations

        private object FindByEffectId(Dictionary<string, object> payload)
        {
            var effectId = GetString(payload, "effectId");
            if (string.IsNullOrEmpty(effectId))
            {
                throw new InvalidOperationException("effectId is required for findByEffectId.");
            }

            // Search all ScriptableObjects in the project for one with matching effectId
            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null) continue;

                var so = new SerializedObject(asset);
                var effectIdProp = so.FindProperty("effectId");
                if (effectIdProp != null && effectIdProp.propertyType == SerializedPropertyType.String
                    && effectIdProp.stringValue == effectId)
                {
                    var displayNameProp = so.FindProperty("displayName");
                    var typeProp = so.FindProperty("type");

                    return CreateSuccessResponse(
                        ("found", true),
                        ("effectId", effectId),
                        ("assetPath", path),
                        ("displayName", displayNameProp != null ? displayNameProp.stringValue : ""),
                        ("type", GetEnumDisplayName(typeProp))
                    );
                }
            }

            return CreateSuccessResponse(("found", false), ("effectId", effectId));
        }

        private object FindByReceiverId(Dictionary<string, object> payload)
        {
            var receiverId = GetString(payload, "receiverId");
            if (string.IsNullOrEmpty(receiverId))
            {
                throw new InvalidOperationException("receiverId is required for findByReceiverId.");
            }

            var component = CodeGenHelper.FindComponentInSceneByField("receiverId", receiverId);
            if (component != null)
            {
                return CreateSuccessResponse(
                    ("found", true),
                    ("receiverId", receiverId),
                    ("path", BuildGameObjectPath(component.gameObject))
                );
            }

            return CreateSuccessResponse(("found", false), ("receiverId", receiverId));
        }

        private object ListEffects(Dictionary<string, object> payload)
        {
            var effects = new List<Dictionary<string, object>>();

            // Search all ScriptableObjects for ones with an effectId field
            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null) continue;

                var so = new SerializedObject(asset);
                var effectIdProp = so.FindProperty("effectId");
                if (effectIdProp == null || effectIdProp.propertyType != SerializedPropertyType.String)
                    continue;

                // Also require displayName to filter out non-effect SOs
                var displayNameProp = so.FindProperty("displayName");
                if (displayNameProp == null) continue;

                var typeProp = so.FindProperty("type");
                var categoryProp = so.FindProperty("category");

                effects.Add(new Dictionary<string, object>
                {
                    { "effectId", effectIdProp.stringValue },
                    { "displayName", displayNameProp.stringValue },
                    { "type", GetEnumDisplayName(typeProp) },
                    { "category", GetEnumDisplayName(categoryProp) },
                    { "assetPath", path }
                });
            }

            return CreateSuccessResponse(
                ("count", effects.Count),
                ("effects", effects)
            );
        }

        #endregion

        #region Helpers

        private ScriptableObject ResolveEffectAsset(Dictionary<string, object> payload)
        {
            var effectId = GetString(payload, "effectId");
            var assetPath = GetString(payload, "assetPath");

            if (!string.IsNullOrEmpty(assetPath))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (asset != null)
                {
                    var so = new SerializedObject(asset);
                    var prop = so.FindProperty("effectId");
                    if (prop != null && prop.propertyType == SerializedPropertyType.String)
                        return asset;
                }
            }

            if (!string.IsNullOrEmpty(effectId))
            {
                var guids = AssetDatabase.FindAssets("t:ScriptableObject");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    if (asset == null) continue;

                    var so = new SerializedObject(asset);
                    var prop = so.FindProperty("effectId");
                    if (prop != null && prop.propertyType == SerializedPropertyType.String
                        && prop.stringValue == effectId)
                    {
                        return asset;
                    }
                }
            }

            throw new InvalidOperationException("Either assetPath or effectId is required to resolve effect asset.");
        }

        private Component ResolveReceiver(Dictionary<string, object> payload)
        {
            // Try by receiverId first
            var receiverId = GetString(payload, "receiverId");
            if (!string.IsNullOrEmpty(receiverId))
            {
                var receiverById = CodeGenHelper.FindComponentInSceneByField("receiverId", receiverId);
                if (receiverById != null)
                {
                    return receiverById;
                }
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var receiverByPath = CodeGenHelper.FindComponentByField(targetGo, "receiverId", null);
                    if (receiverByPath != null)
                    {
                        return receiverByPath;
                    }
                    throw new InvalidOperationException($"No StatusEffectReceiver component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either receiverId or targetPath is required.");
        }

        private string ParseEffectType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "buff" => "Buff",
                "debuff" => "Debuff",
                "neutral" => "Neutral",
                _ => "Buff"
            };
        }

        private string ParseEffectCategory(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "generic" => "Generic",
                "poison" => "Poison",
                "burn" => "Burn",
                "freeze" => "Freeze",
                "stun" => "Stun",
                "slow" => "Slow",
                "haste" => "Haste",
                "shield" => "Shield",
                "regeneration" => "Regeneration",
                "invincibility" => "Invincibility",
                "weakness" => "Weakness",
                "strength" => "Strength",
                "custom" => "Custom",
                _ => "Generic"
            };
        }

        private string ParseStackBehavior(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "refreshduration" or "refresh" => "RefreshDuration",
                "addduration" or "add" => "AddDuration",
                "independent" => "Independent",
                "increasestacks" or "increase" => "IncreaseStacks",
                _ => "RefreshDuration"
            };
        }

        private string ParseModifierType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "statmodifier" or "stat" => "StatModifier",
                "damageovertime" or "dot" => "DamageOverTime",
                "healovertime" or "hot" => "HealOverTime",
                "stun" => "Stun",
                "silence" => "Silence",
                "invincible" => "Invincible",
                "custom" => "Custom",
                _ => "StatModifier"
            };
        }

        private string ParseModifierOperation(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "add" => "Add",
                "subtract" => "Subtract",
                "multiply" => "Multiply",
                "divide" => "Divide",
                "set" => "Set",
                "percentadd" or "percent" => "PercentAdd",
                "percentmultiply" => "PercentMultiply",
                _ => "Add"
            };
        }

        private void SetPropertyIfExists(SerializedObject so, string propertyName, string value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null && prop.propertyType == SerializedPropertyType.String)
                prop.stringValue = value;
        }

        private void SetEnumPropertyIfExists(SerializedObject so, string propertyName, string enumValueName)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null || prop.propertyType != SerializedPropertyType.Enum) return;

            var names = prop.enumDisplayNames;
            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(names[i], enumValueName, StringComparison.OrdinalIgnoreCase))
                {
                    prop.enumValueIndex = i;
                    return;
                }
            }
        }

        private string GetStringProperty(SerializedObject so, string propertyName)
        {
            var prop = so.FindProperty(propertyName);
            return prop != null && prop.propertyType == SerializedPropertyType.String
                ? prop.stringValue
                : "";
        }

        private void SetModifierProperties(SerializedProperty element, Dictionary<string, object> modDict)
        {
            if (modDict.TryGetValue("type", out var typeObj))
            {
                var typeProp = element.FindPropertyRelative("type");
                if (typeProp != null)
                {
                    var typeName = ParseModifierType(typeObj.ToString());
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

            if (modDict.TryGetValue("targetStat", out var statObj))
            {
                var prop = element.FindPropertyRelative("targetStat");
                if (prop != null) prop.stringValue = statObj.ToString();
            }

            if (modDict.TryGetValue("value", out var valObj))
            {
                var prop = element.FindPropertyRelative("value");
                if (prop != null) prop.floatValue = Convert.ToSingle(valObj);
            }

            if (modDict.TryGetValue("operation", out var opObj))
            {
                var opProp = element.FindPropertyRelative("operation");
                if (opProp != null)
                {
                    var opName = ParseModifierOperation(opObj.ToString());
                    var names = opProp.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], opName, StringComparison.OrdinalIgnoreCase))
                        {
                            opProp.enumValueIndex = i;
                            break;
                        }
                    }
                }
            }

            if (modDict.TryGetValue("scaleWithStacks", out var scaleObj))
            {
                var prop = element.FindPropertyRelative("scaleWithStacks");
                if (prop != null) prop.boolValue = Convert.ToBoolean(scaleObj);
            }

            if (modDict.TryGetValue("dotDamage", out var dmgObj))
            {
                var prop = element.FindPropertyRelative("dotDamage");
                if (prop != null) prop.floatValue = Convert.ToSingle(dmgObj);
            }

            if (modDict.TryGetValue("hotHeal", out var healObj))
            {
                var prop = element.FindPropertyRelative("hotHeal");
                if (prop != null) prop.floatValue = Convert.ToSingle(healObj);
            }
        }

        private Dictionary<string, object> SerializeModifierFromProperty(SerializedProperty element)
        {
            var result = new Dictionary<string, object>();

            var typeProp = element.FindPropertyRelative("type");
            if (typeProp != null)
                result["type"] = GetEnumDisplayName(typeProp);

            var statProp = element.FindPropertyRelative("targetStat");
            if (statProp != null)
                result["targetStat"] = statProp.stringValue;

            var valueProp = element.FindPropertyRelative("value");
            if (valueProp != null)
                result["value"] = valueProp.floatValue;

            var opProp = element.FindPropertyRelative("operation");
            if (opProp != null)
                result["operation"] = GetEnumDisplayName(opProp);

            var scaleProp = element.FindPropertyRelative("scaleWithStacks");
            if (scaleProp != null)
                result["scaleWithStacks"] = scaleProp.boolValue;

            var dmgProp = element.FindPropertyRelative("dotDamage");
            if (dmgProp != null)
                result["dotDamage"] = dmgProp.floatValue;

            var healProp = element.FindPropertyRelative("hotHeal");
            if (healProp != null)
                result["hotHeal"] = healProp.floatValue;

            return result;
        }

        private void AddModifiersToAsset(SerializedObject so, List<object> modsList)
        {
            var modsProp = so.FindProperty("modifiers");
            if (modsProp == null || !modsProp.isArray) return;

            foreach (var modObj in modsList)
            {
                if (modObj is Dictionary<string, object> modDict)
                {
                    modsProp.InsertArrayElementAtIndex(modsProp.arraySize);
                    var element = modsProp.GetArrayElementAtIndex(modsProp.arraySize - 1);
                    SetModifierProperties(element, modDict);
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private string GetEnumDisplayName(SerializedProperty prop, string fallback = "")
        {
            if (prop == null) return fallback;
            if (prop.propertyType != SerializedPropertyType.Enum) return fallback;
            return prop.enumValueIndex < prop.enumDisplayNames.Length
                ? prop.enumDisplayNames[prop.enumValueIndex]
                : fallback;
        }

        #endregion
    }
}
