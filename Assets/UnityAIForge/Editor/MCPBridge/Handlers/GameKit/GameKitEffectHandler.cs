using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Effect handler: create and manage composite effect systems.
    /// Uses code generation to produce standalone EffectManager scripts with zero package dependency.
    /// Provides particle, sound, camera shake, and screen flash effects without custom scripts.
    /// </summary>
    public class GameKitEffectHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "update", "inspect", "delete",
            "addComponent", "removeComponent", "clearComponents",
            "play", "playAtPosition", "playAtTransform",
            "shakeCamera", "flashScreen", "setTimeScale",
            "createManager", "registerEffect", "unregisterEffect",
            "findByEffectId", "listEffects"
        };

        public override string Category => "gamekitEffect";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create" || operation == "createManager";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateEffect(payload),
                "update" => UpdateEffect(payload),
                "inspect" => InspectEffect(payload),
                "delete" => DeleteEffect(payload),
                "addComponent" => AddEffectComponent(payload),
                "removeComponent" => RemoveEffectComponent(payload),
                "clearComponents" => ClearEffectComponents(payload),
                "play" => PlayEffect(payload),
                "playAtPosition" => PlayEffectAtPosition(payload),
                "playAtTransform" => PlayEffectAtTransform(payload),
                "shakeCamera" => ShakeCamera(payload),
                "flashScreen" => FlashScreen(payload),
                "setTimeScale" => SetTimeScale(payload),
                "createManager" => CreateEffectManager(payload),
                "registerEffect" => RegisterEffect(payload),
                "unregisterEffect" => UnregisterEffect(payload),
                "findByEffectId" => FindByEffectId(payload),
                "listEffects" => ListEffects(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit Effect operation: {operation}")
            };
        }

        #region Create Effect Asset

        private object CreateEffect(Dictionary<string, object> payload)
        {
            var effectId = GetString(payload, "effectId");
            if (string.IsNullOrEmpty(effectId))
            {
                effectId = $"Effect_{Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            var targetPath = GetString(payload, "targetPath");
            GameObject targetGo;

            if (string.IsNullOrEmpty(targetPath))
            {
                targetGo = new GameObject(effectId);
                Undo.RegisterCreatedObjectUndo(targetGo, "Create Effect");
            }
            else
            {
                targetGo = ResolveGameObject(targetPath);
                if (targetGo == null)
                    throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            // Check if already has an effect component
            var existingEffect = CodeGenHelper.FindComponentByField(targetGo, "effectId", null);
            if (existingEffect != null)
                throw new InvalidOperationException($"GameObject already has an Effect component.");

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(effectId, "Effect");

            // Collect effect component definitions as serialized data
            var componentDefs = new List<Dictionary<string, object>>();
            if (payload.TryGetValue("components", out var componentsObj) && componentsObj is List<object> componentsList)
            {
                foreach (var compObj in componentsList)
                {
                    if (compObj is Dictionary<string, object> compDict)
                        componentDefs.Add(compDict);
                }
            }

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "EFFECT_ID", effectId },
                { "COMPONENT_COUNT", componentDefs.Count }
            };

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "Effect", effectId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate Effect script.");
            }

            // If component was added, set up effect components via serialized properties
            if (result.TryGetValue("componentAdded", out var added) && (bool)added && componentDefs.Count > 0)
            {
                var component = CodeGenHelper.FindComponentByField(targetGo, "effectId", effectId);
                if (component != null)
                {
                    ApplyEffectComponents(component, componentDefs);
                }
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["effectId"] = effectId;
            result["path"] = BuildGameObjectPath(targetGo);
            result["componentCount"] = componentDefs.Count;

            return result;
        }

        #endregion

        #region Update Effect

        private object UpdateEffect(Dictionary<string, object> payload)
        {
            var component = ResolveEffectComponent(payload);

            Undo.RecordObject(component, "Update Effect");

            var so = new SerializedObject(component);

            // Re-initialize effectId if provided
            if (payload.TryGetValue("newEffectId", out var newIdObj))
            {
                var newEffectId = newIdObj.ToString();
                var idProp = so.FindProperty("effectId");
                if (idProp != null)
                    idProp.stringValue = newEffectId;
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var effectId = new SerializedObject(component).FindProperty("effectId").stringValue;

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect Effect

        private object InspectEffect(Dictionary<string, object> payload)
        {
            var component = ResolveEffectComponent(payload);
            var so = new SerializedObject(component);

            var effectId = so.FindProperty("effectId").stringValue;

            // Read components array
            var componentsList = new List<Dictionary<string, object>>();
            var compsProp = so.FindProperty("components");
            if (compsProp != null)
            {
                for (int i = 0; i < compsProp.arraySize; i++)
                {
                    var compProp = compsProp.GetArrayElementAtIndex(i);
                    componentsList.Add(SerializeEffectComponentProperty(compProp));
                }
            }

            var info = new Dictionary<string, object>
            {
                { "effectId", effectId },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "componentCount", componentsList.Count },
                { "components", componentsList }
            };

            return CreateSuccessResponse(("effect", info));
        }

        #endregion

        #region Delete Effect

        private object DeleteEffect(Dictionary<string, object> payload)
        {
            var component = ResolveEffectComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var effectId = new SerializedObject(component).FindProperty("effectId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component);

            // Clean up the generated script from tracker
            ScriptGenerator.Delete(effectId);

            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Effect Components

        private object AddEffectComponent(Dictionary<string, object> payload)
        {
            var component = ResolveEffectComponent(payload);

            if (!payload.TryGetValue("component", out var compObj) || !(compObj is Dictionary<string, object> compDict))
            {
                throw new InvalidOperationException("'component' parameter is required for addComponent.");
            }

            var so = new SerializedObject(component);
            var compsProp = so.FindProperty("components");
            if (compsProp == null)
            {
                throw new InvalidOperationException("Effect component does not have a 'components' array. Regenerate the script.");
            }

            compsProp.InsertArrayElementAtIndex(compsProp.arraySize);
            var newComp = compsProp.GetArrayElementAtIndex(compsProp.arraySize - 1);
            ApplySingleEffectComponent(newComp, compDict);
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var effectId = new SerializedObject(component).FindProperty("effectId").stringValue;
            var componentType = compDict.TryGetValue("type", out var typeObj) ? typeObj.ToString() : "Particle";

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("componentType", componentType),
                ("totalComponents", compsProp.arraySize)
            );
        }

        private object RemoveEffectComponent(Dictionary<string, object> payload)
        {
            var component = ResolveEffectComponent(payload);
            var index = GetInt(payload, "componentIndex", -1);

            var so = new SerializedObject(component);
            var compsProp = so.FindProperty("components");
            if (compsProp == null)
            {
                throw new InvalidOperationException("Effect component does not have a 'components' array.");
            }

            if (index < 0 || index >= compsProp.arraySize)
            {
                throw new InvalidOperationException($"Invalid component index: {index}. Valid range: 0-{compsProp.arraySize - 1}");
            }

            compsProp.DeleteArrayElementAtIndex(index);
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var effectId = new SerializedObject(component).FindProperty("effectId").stringValue;

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("removedIndex", index),
                ("remainingComponents", compsProp.arraySize)
            );
        }

        private object ClearEffectComponents(Dictionary<string, object> payload)
        {
            var component = ResolveEffectComponent(payload);

            var so = new SerializedObject(component);
            var compsProp = so.FindProperty("components");
            var previousCount = compsProp != null ? compsProp.arraySize : 0;

            if (compsProp != null)
            {
                compsProp.ClearArray();
                so.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var effectId = new SerializedObject(component).FindProperty("effectId").stringValue;

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("clearedCount", previousCount)
            );
        }

        #endregion

        #region Play Effects (Runtime)

        private object PlayEffect(Dictionary<string, object> payload)
        {
            if (!Application.isPlaying)
            {
                return CreateSuccessResponse(
                    ("note", "Effect playback requires play mode.")
                );
            }

            var effectId = GetString(payload, "effectId");
            if (string.IsNullOrEmpty(effectId))
            {
                throw new InvalidOperationException("'effectId' is required for play.");
            }

            var position = GetVector3(payload, "position", Vector3.zero);

            // Use reflection to call PlayEffect on the manager instance
            var manager = FindEffectManagerInScene();
            if (manager != null)
            {
                InvokeMethod(manager, "PlayEffect", new object[] { effectId, position });
            }

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("position", new Dictionary<string, object>
                {
                    { "x", position.x },
                    { "y", position.y },
                    { "z", position.z }
                }),
                ("played", manager != null)
            );
        }

        private object PlayEffectAtPosition(Dictionary<string, object> payload)
        {
            return PlayEffect(payload);
        }

        private object PlayEffectAtTransform(Dictionary<string, object> payload)
        {
            if (!Application.isPlaying)
            {
                return CreateSuccessResponse(
                    ("note", "Effect playback requires play mode.")
                );
            }

            var effectId = GetString(payload, "effectId");
            var targetPath = GetString(payload, "targetPath");

            if (string.IsNullOrEmpty(effectId))
            {
                throw new InvalidOperationException("'effectId' is required.");
            }

            if (string.IsNullOrEmpty(targetPath))
            {
                throw new InvalidOperationException("'targetPath' is required for playAtTransform.");
            }

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
            {
                throw new InvalidOperationException($"GameObject not found: {targetPath}");
            }

            // Use reflection to call PlayEffectAtTransform on the manager instance
            var manager = FindEffectManagerInScene();
            if (manager != null)
            {
                InvokeMethod(manager, "PlayEffectAtTransform", new object[] { effectId, targetGo.transform });
            }

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("targetPath", targetPath),
                ("played", manager != null)
            );
        }

        #endregion

        #region Direct Effects (Runtime via Reflection)

        private object ShakeCamera(Dictionary<string, object> payload)
        {
            if (!Application.isPlaying)
            {
                return CreateSuccessResponse(
                    ("note", "Camera shake requires play mode.")
                );
            }

            var intensity = GetFloat(payload, "intensity", 0.3f);
            var duration = GetFloat(payload, "duration", 0.2f);
            var frequency = GetFloat(payload, "frequency", 25f);

            var manager = FindEffectManagerInScene();
            if (manager != null)
            {
                InvokeMethod(manager, "ShakeCamera", new object[] { intensity, duration, frequency });
            }

            return CreateSuccessResponse(
                ("intensity", intensity),
                ("duration", duration),
                ("frequency", frequency),
                ("triggered", manager != null)
            );
        }

        private object FlashScreen(Dictionary<string, object> payload)
        {
            if (!Application.isPlaying)
            {
                return CreateSuccessResponse(
                    ("note", "Screen flash requires play mode.")
                );
            }

            var color = GetColor(payload, "color", new Color(1f, 1f, 1f, 0.5f));
            var duration = GetFloat(payload, "duration", 0.1f);
            var fadeTime = GetFloat(payload, "fadeTime", 0.05f);

            var manager = FindEffectManagerInScene();
            if (manager != null)
            {
                InvokeMethod(manager, "FlashScreen", new object[] { color, duration, fadeTime });
            }

            return CreateSuccessResponse(
                ("color", new Dictionary<string, object>
                {
                    { "r", color.r },
                    { "g", color.g },
                    { "b", color.b },
                    { "a", color.a }
                }),
                ("duration", duration),
                ("triggered", manager != null)
            );
        }

        private object SetTimeScale(Dictionary<string, object> payload)
        {
            if (!Application.isPlaying)
            {
                return CreateSuccessResponse(
                    ("note", "Time scale change requires play mode.")
                );
            }

            var targetScale = GetFloat(payload, "targetScale", 0.1f);
            var duration = GetFloat(payload, "duration", 0.1f);
            var transitionTime = GetFloat(payload, "transitionTime", 0.05f);

            var manager = FindEffectManagerInScene();
            if (manager != null)
            {
                InvokeMethod(manager, "SetTimeScale", new object[] { targetScale, duration, transitionTime });
            }

            return CreateSuccessResponse(
                ("targetScale", targetScale),
                ("duration", duration),
                ("triggered", manager != null)
            );
        }

        #endregion

        #region Effect Manager

        private object CreateEffectManager(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            GameObject targetGo;

            var managerId = GetString(payload, "managerId") ?? $"EffectManager_{Guid.NewGuid().ToString().Substring(0, 8)}";

            if (string.IsNullOrEmpty(targetPath))
            {
                targetGo = new GameObject(managerId);
                Undo.RegisterCreatedObjectUndo(targetGo, "Create Effect Manager");
            }
            else
            {
                targetGo = ResolveGameObject(targetPath);
                if (targetGo == null)
                {
                    throw new InvalidOperationException($"GameObject not found: {targetPath}");
                }
            }

            // Check if already has a manager component
            var existing = CodeGenHelper.FindComponentByField(targetGo, "managerId", null);
            if (existing != null)
            {
                // Verify it's an effect manager by checking for audioPoolSize field
                var existingSo = new SerializedObject(existing);
                if (existingSo.FindProperty("audioPoolSize") != null)
                {
                    return CreateSuccessResponse(
                        ("path", BuildGameObjectPath(targetGo)),
                        ("note", "EffectManager already exists on this GameObject.")
                    );
                }
            }

            var persistent = GetBool(payload, "persistent", true);
            var audioPoolSize = GetInt(payload, "audioPoolSize", 5);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(managerId, "");

            var variables = new Dictionary<string, object>
            {
                { "MANAGER_ID", managerId },
                { "PERSISTENT", persistent.ToString().ToLowerInvariant() },
                { "AUDIO_POOL_SIZE", audioPoolSize }
            };

            var outputDir = GetString(payload, "outputPath");

            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "EffectManager", managerId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate EffectManager script.");
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["managerId"] = managerId;
            result["path"] = BuildGameObjectPath(targetGo);
            result["persistent"] = persistent;
            result["audioPoolSize"] = audioPoolSize;

            return result;
        }

        private object RegisterEffect(Dictionary<string, object> payload)
        {
            var effectId = GetString(payload, "effectId");
            if (string.IsNullOrEmpty(effectId))
            {
                throw new InvalidOperationException("'effectId' is required for registerEffect.");
            }

            // Find the effect component in the scene
            var effectComponent = CodeGenHelper.FindComponentInSceneByField("effectId", effectId);
            if (effectComponent == null)
            {
                throw new InvalidOperationException($"No Effect component found with effectId '{effectId}'. Create one first with 'create' operation.");
            }

            // Find the manager component
            var manager = ResolveEffectManagerComponent(payload);

            // Add to registered effects via serialized property
            var so = new SerializedObject(manager);
            var effectsProp = so.FindProperty("registeredEffects");
            if (effectsProp == null)
            {
                throw new InvalidOperationException("EffectManager does not have a 'registeredEffects' array. Regenerate the script.");
            }

            // Check if already registered (by effectId string)
            var effectIdsProperty = so.FindProperty("registeredEffectIds");
            if (effectIdsProperty != null)
            {
                for (int i = 0; i < effectIdsProperty.arraySize; i++)
                {
                    if (effectIdsProperty.GetArrayElementAtIndex(i).stringValue == effectId)
                    {
                        return CreateSuccessResponse(
                            ("effectId", effectId),
                            ("managerPath", BuildGameObjectPath(manager.gameObject)),
                            ("registered", true),
                            ("note", "Effect was already registered.")
                        );
                    }
                }

                effectIdsProperty.InsertArrayElementAtIndex(effectIdsProperty.arraySize);
                effectIdsProperty.GetArrayElementAtIndex(effectIdsProperty.arraySize - 1).stringValue = effectId;
            }

            // Also store the object reference if the property exists
            if (effectsProp.propertyType == SerializedPropertyType.Generic && effectsProp.isArray)
            {
                effectsProp.InsertArrayElementAtIndex(effectsProp.arraySize);
                var newElement = effectsProp.GetArrayElementAtIndex(effectsProp.arraySize - 1);
                if (newElement.propertyType == SerializedPropertyType.ObjectReference)
                {
                    newElement.objectReferenceValue = effectComponent;
                }
                else if (newElement.propertyType == SerializedPropertyType.String)
                {
                    newElement.stringValue = effectId;
                }
            }

            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("managerPath", BuildGameObjectPath(manager.gameObject)),
                ("registered", true)
            );
        }

        private object UnregisterEffect(Dictionary<string, object> payload)
        {
            var effectId = GetString(payload, "effectId");
            if (string.IsNullOrEmpty(effectId))
            {
                throw new InvalidOperationException("'effectId' is required for unregisterEffect.");
            }

            Component manager;
            try
            {
                manager = ResolveEffectManagerComponent(payload);
            }
            catch
            {
                return CreateSuccessResponse(
                    ("effectId", effectId),
                    ("unregistered", false),
                    ("note", "No EffectManager found.")
                );
            }

            var so = new SerializedObject(manager);
            bool removed = false;

            // Remove from registeredEffectIds if it exists
            var effectIdsProperty = so.FindProperty("registeredEffectIds");
            if (effectIdsProperty != null)
            {
                for (int i = effectIdsProperty.arraySize - 1; i >= 0; i--)
                {
                    if (effectIdsProperty.GetArrayElementAtIndex(i).stringValue == effectId)
                    {
                        effectIdsProperty.DeleteArrayElementAtIndex(i);
                        removed = true;
                        break;
                    }
                }
            }

            // Also remove from registeredEffects array
            var effectsProp = so.FindProperty("registeredEffects");
            if (effectsProp != null)
            {
                for (int i = effectsProp.arraySize - 1; i >= 0; i--)
                {
                    var element = effectsProp.GetArrayElementAtIndex(i);
                    bool match = false;

                    if (element.propertyType == SerializedPropertyType.String)
                    {
                        match = element.stringValue == effectId;
                    }
                    else if (element.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        var obj = element.objectReferenceValue;
                        if (obj != null)
                        {
                            var objSo = new SerializedObject(obj);
                            var idProp = objSo.FindProperty("effectId");
                            match = idProp != null && idProp.stringValue == effectId;
                        }
                    }

                    if (match)
                    {
                        effectsProp.DeleteArrayElementAtIndex(i);
                        removed = true;
                        break;
                    }
                }
            }

            so.ApplyModifiedProperties();

            if (removed)
            {
                EditorUtility.SetDirty(manager);
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }

            return CreateSuccessResponse(
                ("effectId", effectId),
                ("unregistered", removed)
            );
        }

        #endregion

        #region Find & List

        private object FindByEffectId(Dictionary<string, object> payload)
        {
            var effectId = GetString(payload, "effectId");
            if (string.IsNullOrEmpty(effectId))
            {
                throw new InvalidOperationException("'effectId' is required for findByEffectId.");
            }

            var component = CodeGenHelper.FindComponentInSceneByField("effectId", effectId);
            if (component == null)
            {
                return CreateSuccessResponse(
                    ("found", false),
                    ("effectId", effectId)
                );
            }

            var so = new SerializedObject(component);
            var compsProp = so.FindProperty("components");
            var componentCount = compsProp != null ? compsProp.arraySize : 0;

            return CreateSuccessResponse(
                ("found", true),
                ("effectId", effectId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("componentCount", componentCount)
            );
        }

        private object ListEffects(Dictionary<string, object> payload)
        {
            var effects = new List<Dictionary<string, object>>();

            // Search all MonoBehaviours in scene for components with an "effectId" field
            var allMonoBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var mb in allMonoBehaviours)
            {
                if (mb == null) continue;
                try
                {
                    var so = new SerializedObject(mb);
                    var idProp = so.FindProperty("effectId");
                    if (idProp != null && idProp.propertyType == SerializedPropertyType.String
                        && !string.IsNullOrEmpty(idProp.stringValue))
                    {
                        // Distinguish effects from managers by checking for "components" array
                        var compsProp = so.FindProperty("components");
                        if (compsProp != null && compsProp.isArray)
                        {
                            effects.Add(new Dictionary<string, object>
                            {
                                { "effectId", idProp.stringValue },
                                { "path", BuildGameObjectPath(mb.gameObject) },
                                { "componentCount", compsProp.arraySize }
                            });
                        }
                    }
                }
                catch
                {
                    // Skip components that can't be serialized
                }
            }

            return CreateSuccessResponse(
                ("count", effects.Count),
                ("effects", effects)
            );
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Resolves an effect component (the per-effect MonoBehaviour with effectId + components array)
        /// by effectId or targetPath.
        /// </summary>
        private Component ResolveEffectComponent(Dictionary<string, object> payload)
        {
            // Try by effectId first
            var effectId = GetString(payload, "effectId");
            if (!string.IsNullOrEmpty(effectId))
            {
                var effectById = CodeGenHelper.FindComponentInSceneByField("effectId", effectId);
                if (effectById != null)
                {
                    // Verify it has a "components" array (distinguishes Effect from EffectManager)
                    var so = new SerializedObject(effectById);
                    if (so.FindProperty("components") != null)
                        return effectById;
                }
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var effectByPath = CodeGenHelper.FindComponentByField(targetGo, "effectId", null);
                    if (effectByPath != null)
                    {
                        var so = new SerializedObject(effectByPath);
                        if (so.FindProperty("components") != null)
                            return effectByPath;
                    }

                    throw new InvalidOperationException($"No Effect component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            // Try by assetPath for backward compatibility
            var assetPath = GetString(payload, "assetPath");
            if (!string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException(
                    "ScriptableObject-based effects are no longer supported. " +
                    "Use 'create' with a targetPath to generate a standalone Effect component.");
            }

            throw new InvalidOperationException("Either 'effectId' or 'targetPath' is required.");
        }

        /// <summary>
        /// Resolves the EffectManager component in the scene.
        /// Looks by managerPath, or falls back to searching for a component with managerId + audioPoolSize fields.
        /// </summary>
        private Component ResolveEffectManagerComponent(Dictionary<string, object> payload)
        {
            var managerPath = GetString(payload, "managerPath");

            if (!string.IsNullOrEmpty(managerPath))
            {
                var managerGo = ResolveGameObject(managerPath);
                if (managerGo != null)
                {
                    // Look for a component with managerId field that also has audioPoolSize (effect-manager-specific)
                    foreach (var comp in managerGo.GetComponents<MonoBehaviour>())
                    {
                        if (comp == null) continue;
                        var so = new SerializedObject(comp);
                        if (so.FindProperty("managerId") != null && so.FindProperty("audioPoolSize") != null)
                            return comp;
                    }
                    throw new InvalidOperationException($"No EffectManager component found on '{managerPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {managerPath}");
            }

            // Search entire scene for an EffectManager (component with managerId + audioPoolSize)
            var allMonoBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var mb in allMonoBehaviours)
            {
                if (mb == null) continue;
                try
                {
                    var so = new SerializedObject(mb);
                    if (so.FindProperty("managerId") != null && so.FindProperty("audioPoolSize") != null)
                        return mb;
                }
                catch
                {
                    // Skip
                }
            }

            throw new InvalidOperationException("No EffectManager found. Create one first with 'createManager' operation.");
        }

        /// <summary>
        /// Finds any EffectManager MonoBehaviour in the scene for runtime play-mode operations.
        /// Returns null if none found.
        /// </summary>
        private Component FindEffectManagerInScene()
        {
            var allMonoBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var mb in allMonoBehaviours)
            {
                if (mb == null) continue;
                try
                {
                    var so = new SerializedObject(mb);
                    if (so.FindProperty("managerId") != null && so.FindProperty("audioPoolSize") != null)
                        return mb;
                }
                catch
                {
                    // Skip
                }
            }
            return null;
        }

        /// <summary>
        /// Invokes a method on a component using reflection. Used for runtime-only operations.
        /// </summary>
        private void InvokeMethod(Component component, string methodName, object[] args)
        {
            var type = component.GetType();
            var method = type.GetMethod(methodName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(component, args);
            }
            else
            {
                Debug.LogWarning($"[GameKitEffectHandler] Method '{methodName}' not found on {type.Name}.");
            }
        }

        /// <summary>
        /// Applies a list of effect component definitions to the effect's serialized "components" array.
        /// </summary>
        private void ApplyEffectComponents(Component component, List<Dictionary<string, object>> componentDefs)
        {
            var so = new SerializedObject(component);
            var compsProp = so.FindProperty("components");
            if (compsProp == null) return;

            compsProp.ClearArray();
            for (int i = 0; i < componentDefs.Count; i++)
            {
                compsProp.InsertArrayElementAtIndex(i);
                var compProp = compsProp.GetArrayElementAtIndex(i);
                ApplySingleEffectComponent(compProp, componentDefs[i]);
            }

            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// Applies a single effect component dictionary to a serialized array element.
        /// Maps the incoming JSON dict keys to serialized property names.
        /// </summary>
        private void ApplySingleEffectComponent(SerializedProperty compProp, Dictionary<string, object> dict)
        {
            if (dict.TryGetValue("type", out var typeObj))
            {
                var typeName = ParseEffectType(typeObj?.ToString());
                var typeProp = compProp.FindPropertyRelative("type");
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

            // Particle settings
            if (dict.TryGetValue("prefabPath", out var prefabPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath?.ToString());
                var prop = compProp.FindPropertyRelative("particlePrefab");
                if (prop != null) prop.objectReferenceValue = prefab;
            }

            if (dict.TryGetValue("duration", out var durationObj))
            {
                var prop = compProp.FindPropertyRelative("particleDuration");
                if (prop != null) prop.floatValue = Convert.ToSingle(durationObj);
            }

            if (dict.TryGetValue("attachToTarget", out var attachObj))
            {
                var prop = compProp.FindPropertyRelative("attachToTarget");
                if (prop != null) prop.boolValue = Convert.ToBoolean(attachObj);
            }

            if (dict.TryGetValue("positionOffset", out var offsetObj) && offsetObj is Dictionary<string, object> offsetDict)
            {
                var prop = compProp.FindPropertyRelative("positionOffset");
                if (prop != null) prop.vector3Value = GetVector3FromDict(offsetDict, Vector3.zero);
            }

            if (dict.TryGetValue("particleScale", out var scaleObj))
            {
                var prop = compProp.FindPropertyRelative("particleScale");
                if (prop != null) prop.floatValue = Convert.ToSingle(scaleObj);
            }

            // Sound settings
            if (dict.TryGetValue("clipPath", out var clipPath))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath?.ToString());
                var prop = compProp.FindPropertyRelative("audioClip");
                if (prop != null) prop.objectReferenceValue = clip;
            }

            if (dict.TryGetValue("volume", out var volObj))
            {
                var prop = compProp.FindPropertyRelative("volume");
                if (prop != null) prop.floatValue = Mathf.Clamp01(Convert.ToSingle(volObj));
            }

            if (dict.TryGetValue("pitchVariation", out var pitchObj))
            {
                var prop = compProp.FindPropertyRelative("pitchVariation");
                if (prop != null) prop.floatValue = Mathf.Clamp(Convert.ToSingle(pitchObj), 0f, 0.5f);
            }

            if (dict.TryGetValue("spatialBlend", out var spatialObj))
            {
                var prop = compProp.FindPropertyRelative("spatialBlend");
                if (prop != null) prop.floatValue = Mathf.Clamp01(Convert.ToSingle(spatialObj));
            }

            // Camera shake settings
            if (dict.TryGetValue("intensity", out var intensityObj))
            {
                var prop = compProp.FindPropertyRelative("shakeIntensity");
                if (prop != null) prop.floatValue = Convert.ToSingle(intensityObj);
            }

            if (dict.TryGetValue("shakeDuration", out var shakeDurObj))
            {
                var prop = compProp.FindPropertyRelative("shakeDuration");
                if (prop != null) prop.floatValue = Convert.ToSingle(shakeDurObj);
            }

            if (dict.TryGetValue("frequency", out var freqObj))
            {
                var prop = compProp.FindPropertyRelative("shakeFrequency");
                if (prop != null) prop.floatValue = Convert.ToSingle(freqObj);
            }

            // Screen flash settings
            if (dict.TryGetValue("color", out var colorObj) && colorObj is Dictionary<string, object> colorDict)
            {
                var prop = compProp.FindPropertyRelative("flashColor");
                if (prop != null) prop.colorValue = GetColorFromDict(colorDict, Color.white);
            }

            if (dict.TryGetValue("flashDuration", out var flashDurObj))
            {
                var prop = compProp.FindPropertyRelative("flashDuration");
                if (prop != null) prop.floatValue = Convert.ToSingle(flashDurObj);
            }

            if (dict.TryGetValue("fadeTime", out var fadeObj))
            {
                var prop = compProp.FindPropertyRelative("flashFadeTime");
                if (prop != null) prop.floatValue = Convert.ToSingle(fadeObj);
            }

            // Time scale settings
            if (dict.TryGetValue("targetTimeScale", out var timeScaleObj))
            {
                var prop = compProp.FindPropertyRelative("targetTimeScale");
                if (prop != null) prop.floatValue = Convert.ToSingle(timeScaleObj);
            }

            if (dict.TryGetValue("timeScaleDuration", out var tsDurObj))
            {
                var prop = compProp.FindPropertyRelative("timeScaleDuration");
                if (prop != null) prop.floatValue = Convert.ToSingle(tsDurObj);
            }

            if (dict.TryGetValue("timeScaleTransition", out var tsTransObj))
            {
                var prop = compProp.FindPropertyRelative("timeScaleTransition");
                if (prop != null) prop.floatValue = Convert.ToSingle(tsTransObj);
            }
        }

        /// <summary>
        /// Serializes a single effect component SerializedProperty into a dictionary for inspect output.
        /// </summary>
        private Dictionary<string, object> SerializeEffectComponentProperty(SerializedProperty compProp)
        {
            var dict = new Dictionary<string, object>();

            var typeProp = compProp.FindPropertyRelative("type");
            var typeName = "Particle";
            if (typeProp != null && typeProp.enumValueIndex < typeProp.enumDisplayNames.Length)
                typeName = typeProp.enumDisplayNames[typeProp.enumValueIndex];

            dict["type"] = typeName;

            switch (typeName.ToLowerInvariant())
            {
                case "particle":
                    var particlePrefab = compProp.FindPropertyRelative("particlePrefab");
                    dict["prefabPath"] = particlePrefab?.objectReferenceValue != null
                        ? AssetDatabase.GetAssetPath(particlePrefab.objectReferenceValue)
                        : null;
                    var particleDuration = compProp.FindPropertyRelative("particleDuration");
                    if (particleDuration != null) dict["duration"] = particleDuration.floatValue;
                    var attach = compProp.FindPropertyRelative("attachToTarget");
                    if (attach != null) dict["attachToTarget"] = attach.boolValue;
                    var offset = compProp.FindPropertyRelative("positionOffset");
                    if (offset != null)
                    {
                        dict["positionOffset"] = new Dictionary<string, object>
                        {
                            { "x", offset.vector3Value.x },
                            { "y", offset.vector3Value.y },
                            { "z", offset.vector3Value.z }
                        };
                    }
                    var pScale = compProp.FindPropertyRelative("particleScale");
                    if (pScale != null) dict["particleScale"] = pScale.floatValue;
                    break;

                case "sound":
                    var clip = compProp.FindPropertyRelative("audioClip");
                    dict["clipPath"] = clip?.objectReferenceValue != null
                        ? AssetDatabase.GetAssetPath(clip.objectReferenceValue)
                        : null;
                    var vol = compProp.FindPropertyRelative("volume");
                    if (vol != null) dict["volume"] = vol.floatValue;
                    var pitch = compProp.FindPropertyRelative("pitchVariation");
                    if (pitch != null) dict["pitchVariation"] = pitch.floatValue;
                    var spatial = compProp.FindPropertyRelative("spatialBlend");
                    if (spatial != null) dict["spatialBlend"] = spatial.floatValue;
                    break;

                case "camerashake":
                    var shakeI = compProp.FindPropertyRelative("shakeIntensity");
                    if (shakeI != null) dict["intensity"] = shakeI.floatValue;
                    var shakeD = compProp.FindPropertyRelative("shakeDuration");
                    if (shakeD != null) dict["duration"] = shakeD.floatValue;
                    var shakeF = compProp.FindPropertyRelative("shakeFrequency");
                    if (shakeF != null) dict["frequency"] = shakeF.floatValue;
                    break;

                case "screenflash":
                    var flashColor = compProp.FindPropertyRelative("flashColor");
                    if (flashColor != null)
                    {
                        var c = flashColor.colorValue;
                        dict["color"] = new Dictionary<string, object>
                        {
                            { "r", c.r },
                            { "g", c.g },
                            { "b", c.b },
                            { "a", c.a }
                        };
                    }
                    var flashDur = compProp.FindPropertyRelative("flashDuration");
                    if (flashDur != null) dict["duration"] = flashDur.floatValue;
                    var flashFade = compProp.FindPropertyRelative("flashFadeTime");
                    if (flashFade != null) dict["fadeTime"] = flashFade.floatValue;
                    break;

                case "timescale":
                    var tsTarget = compProp.FindPropertyRelative("targetTimeScale");
                    if (tsTarget != null) dict["targetTimeScale"] = tsTarget.floatValue;
                    var tsDur = compProp.FindPropertyRelative("timeScaleDuration");
                    if (tsDur != null) dict["duration"] = tsDur.floatValue;
                    var tsTrans = compProp.FindPropertyRelative("timeScaleTransition");
                    if (tsTrans != null) dict["transitionTime"] = tsTrans.floatValue;
                    break;
            }

            return dict;
        }

        private string ParseEffectType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "particle" => "Particle",
                "sound" => "Sound",
                "camerashake" or "shake" => "CameraShake",
                "screenflash" or "flash" => "ScreenFlash",
                "timescale" or "slowmo" or "hitpause" => "TimeScale",
                _ => "Particle"
            };
        }

        #endregion
    }
}
