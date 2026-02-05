using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using UnityAIForge.GameKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Feedback handler: create and manage game feel effects.
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

        protected override bool RequiresCompilationWait(string operation) => false;

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
            {
                throw new InvalidOperationException("targetPath is required for create operation.");
            }

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
            {
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            // Check if already has feedback component
            var existingFeedback = targetGo.GetComponent<GameKitFeedback>();
            if (existingFeedback != null)
            {
                throw new InvalidOperationException($"GameObject '{targetPath}' already has a GameKitFeedback component.");
            }

            var feedbackId = GetString(payload, "feedbackId") ?? $"Feedback_{Guid.NewGuid().ToString().Substring(0, 8)}";

            // Add component
            var feedback = Undo.AddComponent<GameKitFeedback>(targetGo);

            // Set properties via SerializedObject
            var serializedFeedback = new SerializedObject(feedback);
            serializedFeedback.FindProperty("feedbackId").stringValue = feedbackId;

            if (payload.TryGetValue("playOnEnable", out var playObj))
            {
                serializedFeedback.FindProperty("playOnEnable").boolValue = Convert.ToBoolean(playObj);
            }

            if (payload.TryGetValue("globalIntensityMultiplier", out var intensityObj))
            {
                serializedFeedback.FindProperty("globalIntensityMultiplier").floatValue = Convert.ToSingle(intensityObj);
            }

            // Add components if provided
            if (payload.TryGetValue("components", out var componentsObj) && componentsObj is List<object> componentsList)
            {
                var componentsProp = serializedFeedback.FindProperty("components");
                componentsProp.ClearArray();

                for (int i = 0; i < componentsList.Count; i++)
                {
                    if (componentsList[i] is Dictionary<string, object> compDict)
                    {
                        componentsProp.InsertArrayElementAtIndex(i);
                        var element = componentsProp.GetArrayElementAtIndex(i);
                        ApplyComponentProperties(element, compDict);
                    }
                }
            }

            serializedFeedback.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("feedbackId", feedbackId),
                ("path", BuildGameObjectPath(targetGo)),
                ("componentCount", feedback.Components.Count)
            );
        }

        #endregion

        #region Update

        private object UpdateFeedback(Dictionary<string, object> payload)
        {
            var feedback = ResolveFeedbackComponent(payload);

            Undo.RecordObject(feedback, "Update GameKit Feedback");

            var serializedFeedback = new SerializedObject(feedback);

            if (payload.TryGetValue("playOnEnable", out var playObj))
            {
                serializedFeedback.FindProperty("playOnEnable").boolValue = Convert.ToBoolean(playObj);
            }

            if (payload.TryGetValue("globalIntensityMultiplier", out var intensityObj))
            {
                serializedFeedback.FindProperty("globalIntensityMultiplier").floatValue = Convert.ToSingle(intensityObj);
            }

            serializedFeedback.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(feedback.gameObject.scene);

            return CreateSuccessResponse(
                ("feedbackId", feedback.FeedbackId),
                ("path", BuildGameObjectPath(feedback.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectFeedback(Dictionary<string, object> payload)
        {
            var feedback = ResolveFeedbackComponent(payload);

            var serializedFeedback = new SerializedObject(feedback);

            var componentInfos = new List<Dictionary<string, object>>();
            var componentsProp = serializedFeedback.FindProperty("components");

            for (int i = 0; i < componentsProp.arraySize; i++)
            {
                var element = componentsProp.GetArrayElementAtIndex(i);
                componentInfos.Add(new Dictionary<string, object>
                {
                    { "type", element.FindPropertyRelative("type").enumNames[element.FindPropertyRelative("type").enumValueIndex] },
                    { "delay", element.FindPropertyRelative("delay").floatValue },
                    { "duration", element.FindPropertyRelative("duration").floatValue },
                    { "intensity", element.FindPropertyRelative("intensity").floatValue }
                });
            }

            var info = new Dictionary<string, object>
            {
                { "feedbackId", feedback.FeedbackId },
                { "path", BuildGameObjectPath(feedback.gameObject) },
                { "isPlaying", feedback.IsPlaying },
                { "playOnEnable", serializedFeedback.FindProperty("playOnEnable").boolValue },
                { "globalIntensityMultiplier", serializedFeedback.FindProperty("globalIntensityMultiplier").floatValue },
                { "componentCount", feedback.Components.Count },
                { "components", componentInfos }
            };

            return CreateSuccessResponse(("feedback", info));
        }

        #endregion

        #region Delete

        private object DeleteFeedback(Dictionary<string, object> payload)
        {
            var feedback = ResolveFeedbackComponent(payload);
            var path = BuildGameObjectPath(feedback.gameObject);
            var feedbackId = feedback.FeedbackId;
            var scene = feedback.gameObject.scene;

            Undo.DestroyObjectImmediate(feedback);
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
            var feedback = ResolveFeedbackComponent(payload);

            if (!payload.TryGetValue("component", out var compObj) || !(compObj is Dictionary<string, object> compDict))
            {
                throw new InvalidOperationException("component object is required for addComponent.");
            }

            var serializedFeedback = new SerializedObject(feedback);
            var componentsProp = serializedFeedback.FindProperty("components");

            int index = componentsProp.arraySize;
            componentsProp.InsertArrayElementAtIndex(index);
            var element = componentsProp.GetArrayElementAtIndex(index);

            ApplyComponentProperties(element, compDict);

            serializedFeedback.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(feedback.gameObject.scene);

            return CreateSuccessResponse(
                ("feedbackId", feedback.FeedbackId),
                ("componentIndex", index),
                ("componentType", compDict.TryGetValue("type", out var typeObj) ? typeObj.ToString() : "unknown"),
                ("added", true)
            );
        }

        private object ClearComponents(Dictionary<string, object> payload)
        {
            var feedback = ResolveFeedbackComponent(payload);

            var serializedFeedback = new SerializedObject(feedback);
            var componentsProp = serializedFeedback.FindProperty("components");
            int previousCount = componentsProp.arraySize;
            componentsProp.ClearArray();
            serializedFeedback.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(feedback.gameObject.scene);

            return CreateSuccessResponse(
                ("feedbackId", feedback.FeedbackId),
                ("previousCount", previousCount),
                ("cleared", true)
            );
        }

        private object SetIntensity(Dictionary<string, object> payload)
        {
            var feedback = ResolveFeedbackComponent(payload);
            var intensity = GetFloat(payload, "intensity", 1f);

            var serializedFeedback = new SerializedObject(feedback);
            serializedFeedback.FindProperty("globalIntensityMultiplier").floatValue = intensity;
            serializedFeedback.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(feedback.gameObject.scene);

            return CreateSuccessResponse(
                ("feedbackId", feedback.FeedbackId),
                ("intensity", intensity)
            );
        }

        private object FindByFeedbackId(Dictionary<string, object> payload)
        {
            var feedbackId = GetString(payload, "feedbackId");
            if (string.IsNullOrEmpty(feedbackId))
            {
                throw new InvalidOperationException("feedbackId is required for findByFeedbackId.");
            }

            var feedback = FindFeedbackById(feedbackId);
            if (feedback == null)
            {
                return CreateSuccessResponse(("found", false), ("feedbackId", feedbackId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("feedbackId", feedback.FeedbackId),
                ("path", BuildGameObjectPath(feedback.gameObject)),
                ("componentCount", feedback.Components.Count)
            );
        }

        #endregion

        #region Helpers

        private void ApplyComponentProperties(SerializedProperty element, Dictionary<string, object> compDict)
        {
            if (compDict.TryGetValue("type", out var typeObj))
            {
                var type = ParseFeedbackType(typeObj.ToString());
                element.FindPropertyRelative("type").enumValueIndex = (int)type;
            }

            if (compDict.TryGetValue("delay", out var delayObj))
            {
                element.FindPropertyRelative("delay").floatValue = Convert.ToSingle(delayObj);
            }

            if (compDict.TryGetValue("duration", out var durationObj))
            {
                element.FindPropertyRelative("duration").floatValue = Convert.ToSingle(durationObj);
            }

            if (compDict.TryGetValue("intensity", out var intensityObj))
            {
                element.FindPropertyRelative("intensity").floatValue = Convert.ToSingle(intensityObj);
            }

            // Hitstop
            if (compDict.TryGetValue("hitstopTimeScale", out var timeScaleObj))
            {
                element.FindPropertyRelative("hitstopTimeScale").floatValue = Convert.ToSingle(timeScaleObj);
            }

            // Shake
            if (compDict.TryGetValue("shakeFrequency", out var freqObj))
            {
                element.FindPropertyRelative("shakeFrequency").floatValue = Convert.ToSingle(freqObj);
            }

            // Flash
            if (compDict.TryGetValue("color", out var colorObj) && colorObj is Dictionary<string, object> colorDict)
            {
                var color = GetColorFromDict(colorDict, Color.white);
                element.FindPropertyRelative("flashColor").colorValue = color;
            }

            if (compDict.TryGetValue("fadeTime", out var fadeObj))
            {
                element.FindPropertyRelative("fadeTime").floatValue = Convert.ToSingle(fadeObj);
            }

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
            {
                element.FindPropertyRelative("soundVolume").floatValue = Convert.ToSingle(volObj);
            }

            // Haptic
            if (compDict.TryGetValue("hapticIntensity", out var hapticObj))
            {
                element.FindPropertyRelative("hapticIntensity").floatValue = Convert.ToSingle(hapticObj);
            }
        }

        private GameKitFeedback ResolveFeedbackComponent(Dictionary<string, object> payload)
        {
            // Try by feedbackId first
            var feedbackId = GetString(payload, "feedbackId");
            if (!string.IsNullOrEmpty(feedbackId))
            {
                var feedbackById = FindFeedbackById(feedbackId);
                if (feedbackById != null)
                {
                    return feedbackById;
                }
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var feedbackByPath = targetGo.GetComponent<GameKitFeedback>();
                    if (feedbackByPath != null)
                    {
                        return feedbackByPath;
                    }
                    throw new InvalidOperationException($"No GameKitFeedback component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either feedbackId or targetPath is required.");
        }

        private GameKitFeedback FindFeedbackById(string feedbackId)
        {
            var feedbacks = UnityEngine.Object.FindObjectsByType<GameKitFeedback>(FindObjectsSortMode.None);
            foreach (var feedback in feedbacks)
            {
                if (feedback.FeedbackId == feedbackId)
                {
                    return feedback;
                }
            }
            return null;
        }

        private GameKitFeedback.FeedbackType ParseFeedbackType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "hitstop" => GameKitFeedback.FeedbackType.Hitstop,
                "screenshake" => GameKitFeedback.FeedbackType.ScreenShake,
                "flash" => GameKitFeedback.FeedbackType.Flash,
                "colorflash" => GameKitFeedback.FeedbackType.ColorFlash,
                "scale" => GameKitFeedback.FeedbackType.Scale,
                "position" => GameKitFeedback.FeedbackType.Position,
                "rotation" => GameKitFeedback.FeedbackType.Rotation,
                "sound" => GameKitFeedback.FeedbackType.Sound,
                "particle" => GameKitFeedback.FeedbackType.Particle,
                "haptic" => GameKitFeedback.FeedbackType.Haptic,
                _ => GameKitFeedback.FeedbackType.ScreenShake
            };
        }


        #endregion
    }
}
