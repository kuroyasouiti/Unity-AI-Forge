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
    /// GameKit Feedback handler: create and manage game feel effects.
    /// Uses code generation to produce standalone Feedback scripts with zero package dependency.
    /// Supports hitstop, screen shake, flash, scale punch, and other feedback components.
    /// </summary>
    public class GameKitFeedbackHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "update", "inspect", "delete",
            "addComponent", "clearComponents", "setIntensity",
            "findByFeedbackId"
        };

        public override string Category => "gamekitFeedback";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateFeedback(payload),
                "update" => UpdateFeedback(payload),
                "inspect" => InspectFeedback(payload),
                "delete" => DeleteFeedback(payload),
                "addComponent" => AddComponent(payload),
                "clearComponents" => ClearComponents(payload),
                "setIntensity" => SetIntensity(payload),
                "findByFeedbackId" => FindByFeedbackId(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit Feedback operation: {operation}")
            };
        }

        #region Create

        private object CreateFeedback(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            if (string.IsNullOrEmpty(targetPath))
                throw new InvalidOperationException("targetPath is required for create operation.");

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");

            // Check if already has a feedback component (by checking for feedbackId field)
            var existingFeedback = CodeGenHelper.FindComponentByField(targetGo, "feedbackId", null);
            if (existingFeedback != null)
                throw new InvalidOperationException($"GameObject '{targetPath}' already has a Feedback component.");

            var feedbackId = GetString(payload, "feedbackId") ?? $"Feedback_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var playOnEnable = GetBool(payload, "playOnEnable", false);
            var globalIntensity = GetFloat(payload, "globalIntensityMultiplier", 1f);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(feedbackId, "Feedback");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "FEEDBACK_ID", feedbackId },
                { "PLAY_ON_ENABLE", playOnEnable },
                { "GLOBAL_INTENSITY", globalIntensity }
            };

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "Feedback", feedbackId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate Feedback script.");
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["feedbackId"] = feedbackId;
            result["path"] = BuildGameObjectPath(targetGo);
            result["playOnEnable"] = playOnEnable;
            result["globalIntensityMultiplier"] = globalIntensity;

            return result;
        }

        #endregion

        #region Update

        private object UpdateFeedback(Dictionary<string, object> payload)
        {
            var component = ResolveFeedbackComponent(payload);

            Undo.RecordObject(component, "Update Feedback");

            var so = new SerializedObject(component);

            if (payload.TryGetValue("playOnEnable", out var playObj))
                so.FindProperty("playOnEnable").boolValue = Convert.ToBoolean(playObj);

            if (payload.TryGetValue("globalIntensityMultiplier", out var intensityObj))
                so.FindProperty("globalIntensityMultiplier").floatValue = Convert.ToSingle(intensityObj);

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var feedbackId = new SerializedObject(component).FindProperty("feedbackId").stringValue;

            return CreateSuccessResponse(
                ("feedbackId", feedbackId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectFeedback(Dictionary<string, object> payload)
        {
            var component = ResolveFeedbackComponent(payload);
            var so = new SerializedObject(component);

            var componentInfos = new List<Dictionary<string, object>>();
            var componentsProp = so.FindProperty("components");

            if (componentsProp != null)
            {
                for (int i = 0; i < componentsProp.arraySize; i++)
                {
                    var element = componentsProp.GetArrayElementAtIndex(i);
                    var typeProp = element.FindPropertyRelative("type");
                    componentInfos.Add(new Dictionary<string, object>
                    {
                        { "type", typeProp.enumValueIndex < typeProp.enumDisplayNames.Length
                            ? typeProp.enumDisplayNames[typeProp.enumValueIndex] : "ScreenShake" },
                        { "delay", element.FindPropertyRelative("delay").floatValue },
                        { "duration", element.FindPropertyRelative("duration").floatValue },
                        { "intensity", element.FindPropertyRelative("intensity").floatValue }
                    });
                }
            }

            var info = new Dictionary<string, object>
            {
                { "feedbackId", so.FindProperty("feedbackId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "playOnEnable", so.FindProperty("playOnEnable").boolValue },
                { "globalIntensityMultiplier", so.FindProperty("globalIntensityMultiplier").floatValue },
                { "componentCount", componentsProp != null ? componentsProp.arraySize : 0 },
                { "components", componentInfos }
            };

            return CreateSuccessResponse(("feedback", info));
        }

        #endregion

        #region Delete

        private object DeleteFeedback(Dictionary<string, object> payload)
        {
            var component = ResolveFeedbackComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var feedbackId = new SerializedObject(component).FindProperty("feedbackId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component);

            // Also clean up the generated script from tracker
            ScriptGenerator.Delete(feedbackId);

            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("feedbackId", feedbackId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Operations

        private object AddComponent(Dictionary<string, object> payload)
        {
            var component = ResolveFeedbackComponent(payload);

            if (!payload.TryGetValue("component", out var compObj) || !(compObj is Dictionary<string, object> compDict))
                throw new InvalidOperationException("component object is required for addComponent.");

            var so = new SerializedObject(component);
            var componentsProp = so.FindProperty("components");

            int index = componentsProp.arraySize;
            componentsProp.InsertArrayElementAtIndex(index);
            var element = componentsProp.GetArrayElementAtIndex(index);

            ApplyComponentProperties(element, compDict);

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var feedbackId = new SerializedObject(component).FindProperty("feedbackId").stringValue;

            return CreateSuccessResponse(
                ("feedbackId", feedbackId),
                ("componentIndex", index),
                ("componentType", compDict.TryGetValue("type", out var typeObj) ? typeObj.ToString() : "unknown"),
                ("added", true)
            );
        }

        private object ClearComponents(Dictionary<string, object> payload)
        {
            var component = ResolveFeedbackComponent(payload);

            var so = new SerializedObject(component);
            var componentsProp = so.FindProperty("components");
            int previousCount = componentsProp.arraySize;
            componentsProp.ClearArray();
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var feedbackId = new SerializedObject(component).FindProperty("feedbackId").stringValue;

            return CreateSuccessResponse(
                ("feedbackId", feedbackId),
                ("previousCount", previousCount),
                ("cleared", true)
            );
        }

        private object SetIntensity(Dictionary<string, object> payload)
        {
            var component = ResolveFeedbackComponent(payload);
            var intensity = GetFloat(payload, "intensity", 1f);

            var so = new SerializedObject(component);
            so.FindProperty("globalIntensityMultiplier").floatValue = intensity;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var feedbackId = new SerializedObject(component).FindProperty("feedbackId").stringValue;

            return CreateSuccessResponse(
                ("feedbackId", feedbackId),
                ("intensity", intensity)
            );
        }

        private object FindByFeedbackId(Dictionary<string, object> payload)
        {
            var feedbackId = GetString(payload, "feedbackId");
            if (string.IsNullOrEmpty(feedbackId))
                throw new InvalidOperationException("feedbackId is required for findByFeedbackId.");

            var component = CodeGenHelper.FindComponentInSceneByField("feedbackId", feedbackId);
            if (component == null)
                return CreateSuccessResponse(("found", false), ("feedbackId", feedbackId));

            var so = new SerializedObject(component);
            var componentsProp = so.FindProperty("components");

            return CreateSuccessResponse(
                ("found", true),
                ("feedbackId", feedbackId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("componentCount", componentsProp != null ? componentsProp.arraySize : 0)
            );
        }

        #endregion

        #region Helpers

        private void ApplyComponentProperties(SerializedProperty element, Dictionary<string, object> compDict)
        {
            if (compDict.TryGetValue("type", out var typeObj))
            {
                var typeName = ParseFeedbackType(typeObj.ToString());
                var typeProp = element.FindPropertyRelative("type");
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

            if (compDict.TryGetValue("delay", out var delayObj))
                element.FindPropertyRelative("delay").floatValue = Convert.ToSingle(delayObj);

            if (compDict.TryGetValue("duration", out var durationObj))
                element.FindPropertyRelative("duration").floatValue = Convert.ToSingle(durationObj);

            if (compDict.TryGetValue("intensity", out var intensityObj))
                element.FindPropertyRelative("intensity").floatValue = Convert.ToSingle(intensityObj);

            // Hitstop
            if (compDict.TryGetValue("hitstopTimeScale", out var timeScaleObj))
                element.FindPropertyRelative("hitstopTimeScale").floatValue = Convert.ToSingle(timeScaleObj);

            // Shake
            if (compDict.TryGetValue("shakeFrequency", out var freqObj))
                element.FindPropertyRelative("shakeFrequency").floatValue = Convert.ToSingle(freqObj);

            // Flash
            if (compDict.TryGetValue("color", out var colorObj) && colorObj is Dictionary<string, object> colorDict)
            {
                var color = GetColorFromDict(colorDict, Color.white);
                element.FindPropertyRelative("flashColor").colorValue = color;
            }

            if (compDict.TryGetValue("fadeTime", out var fadeObj))
                element.FindPropertyRelative("fadeTime").floatValue = Convert.ToSingle(fadeObj);

            // Scale
            if (compDict.TryGetValue("scaleAmount", out var scaleObj) && scaleObj is Dictionary<string, object> scaleDict)
            {
                var scale = GetVector3FromDict(scaleDict, new Vector3(1.2f, 1.2f, 1.2f));
                element.FindPropertyRelative("scaleAmount").vector3Value = scale;
            }

            // Position
            if (compDict.TryGetValue("positionAmount", out var posObj) && posObj is Dictionary<string, object> posDict)
            {
                var pos = GetVector3FromDict(posDict, Vector3.zero);
                element.FindPropertyRelative("positionAmount").vector3Value = pos;
            }

            // Sound
            if (compDict.TryGetValue("soundVolume", out var volObj))
                element.FindPropertyRelative("soundVolume").floatValue = Convert.ToSingle(volObj);

            // Haptic
            if (compDict.TryGetValue("hapticIntensity", out var hapticObj))
                element.FindPropertyRelative("hapticIntensity").floatValue = Convert.ToSingle(hapticObj);
        }

        private Component ResolveFeedbackComponent(Dictionary<string, object> payload)
        {
            // Try by feedbackId first
            var feedbackId = GetString(payload, "feedbackId");
            if (!string.IsNullOrEmpty(feedbackId))
            {
                var feedbackById = CodeGenHelper.FindComponentInSceneByField("feedbackId", feedbackId);
                if (feedbackById != null)
                    return feedbackById;
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var feedbackByPath = CodeGenHelper.FindComponentByField(targetGo, "feedbackId", null);
                    if (feedbackByPath != null)
                        return feedbackByPath;

                    throw new InvalidOperationException($"No Feedback component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either feedbackId or targetPath is required.");
        }

        private string ParseFeedbackType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "hitstop" => "Hitstop",
                "screenshake" => "ScreenShake",
                "flash" => "Flash",
                "colorflash" => "ColorFlash",
                "scale" => "Scale",
                "position" => "Position",
                "rotation" => "Rotation",
                "sound" => "Sound",
                "particle" => "Particle",
                "haptic" => "Haptic",
                _ => "ScreenShake"
            };
        }

        #endregion
    }
}
