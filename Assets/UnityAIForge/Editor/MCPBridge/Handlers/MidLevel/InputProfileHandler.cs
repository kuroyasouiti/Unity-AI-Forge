using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Mid-level input profile utilities: create PlayerInput with New Input System.
    /// </summary>
    public class InputProfileHandler : BaseCommandHandler
    {
        private static readonly string[] Operations = { "createPlayerInput", "createInputActions", "inspect" };

        public override string Category => "inputProfile";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "createPlayerInput" => CreatePlayerInput(payload),
                "createInputActions" => CreateInputActions(payload),
                "inspect" => InspectPlayerInput(payload),
                _ => throw new InvalidOperationException($"Unsupported input profile operation: {operation}"),
            };
        }

        #region Create Player Input

        private object CreatePlayerInput(Dictionary<string, object> payload)
        {
            // Check if New Input System is available
            var playerInputType = Type.GetType("UnityEngine.InputSystem.PlayerInput, Unity.InputSystem");
            if (playerInputType == null)
            {
                throw new InvalidOperationException("New Input System (PlayerInput) is not available. Please install the Input System package.");
            }

            var gameObjectPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required for createPlayerInput.");
            }

            var go = ResolveGameObject(gameObjectPath);
            var playerInput = go.GetComponent(playerInputType);

            if (playerInput == null)
            {
                playerInput = Undo.AddComponent(go, playerInputType);
            }
            else
            {
                Undo.RecordObject(playerInput, "Update PlayerInput");
            }

            var preset = GetString(payload, "preset")?.ToLowerInvariant() ?? "custom";
            ApplyPlayerInputPreset(playerInput, preset, payload);

            EditorSceneManager.MarkSceneDirty(go.scene);
            return CreateSuccessResponse(("path", BuildGameObjectPath(go)));
        }

        private void ApplyPlayerInputPreset(Component playerInput, string preset, Dictionary<string, object> payload)
        {
            var type = playerInput.GetType();

            // Load InputActions asset if specified
            if (payload.TryGetValue("inputActionsAssetPath", out var assetPathObj))
            {
                var assetPath = assetPathObj.ToString();
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var inputActions = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                    if (inputActions != null)
                    {
                        var actionsProperty = type.GetProperty("actions");
                        if (actionsProperty != null)
                        {
                            actionsProperty.SetValue(playerInput, inputActions);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"InputActions asset not found at path: {assetPath}");
                    }
                }
            }

            // Set default action map
            if (payload.TryGetValue("defaultActionMap", out var actionMapObj))
            {
                var actionMap = actionMapObj.ToString();
                if (!string.IsNullOrEmpty(actionMap))
                {
                    var defaultActionMapProperty = type.GetProperty("defaultActionMap");
                    if (defaultActionMapProperty != null)
                    {
                        defaultActionMapProperty.SetValue(playerInput, actionMap);
                    }
                }
            }

            // Set notification behavior
            if (payload.TryGetValue("notificationBehavior", out var behaviorObj))
            {
                var behavior = behaviorObj.ToString();
                SetNotificationBehavior(playerInput, behavior);
            }
            else
            {
                // Apply preset-specific notification behavior
                switch (preset)
                {
                    case "player":
                        SetNotificationBehavior(playerInput, "invokeUnityEvents");
                        break;
                    case "ui":
                        SetNotificationBehavior(playerInput, "sendMessages");
                        break;
                    case "vehicle":
                        SetNotificationBehavior(playerInput, "invokeCSharpEvents");
                        break;
                }
            }
        }

        private void SetNotificationBehavior(Component playerInput, string behavior)
        {
            var type = playerInput.GetType();
            var notificationBehaviorProperty = type.GetProperty("notificationBehavior");
            if (notificationBehaviorProperty == null) return;

            var enumType = notificationBehaviorProperty.PropertyType;
            object enumValue = null;

            switch (behavior.ToLowerInvariant())
            {
                case "sendmessages":
                    enumValue = Enum.Parse(enumType, "SendMessages");
                    break;
                case "broadcastmessages":
                    enumValue = Enum.Parse(enumType, "BroadcastMessages");
                    break;
                case "invokeunityevents":
                    enumValue = Enum.Parse(enumType, "InvokeUnityEvents");
                    break;
                case "invokecsharpevents":
                    enumValue = Enum.Parse(enumType, "InvokeCSharpEvents");
                    break;
            }

            if (enumValue != null)
            {
                notificationBehaviorProperty.SetValue(playerInput, enumValue);
            }
        }

        #endregion

        #region Create Input Actions

        private object CreateInputActions(Dictionary<string, object> payload)
        {
            // Check if New Input System is available
            var inputActionAssetType = Type.GetType("UnityEngine.InputSystem.InputActionAsset, Unity.InputSystem");
            if (inputActionAssetType == null)
            {
                throw new InvalidOperationException("New Input System (InputActionAsset) is not available. Please install the Input System package.");
            }

            var assetPath = GetString(payload, "inputActionsAssetPath");
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("inputActionsAssetPath is required for createInputActions.");
            }

            // Ensure path ends with .inputactions
            if (!assetPath.EndsWith(".inputactions"))
            {
                assetPath += ".inputactions";
            }

            // Create InputActionAsset
            var inputActionAsset = ScriptableObject.CreateInstance(inputActionAssetType);
            AssetDatabase.CreateAsset(inputActionAsset, assetPath);

            // Add custom actions if specified
            if (payload.TryGetValue("actions", out var actionsObj) && actionsObj is List<object> actionsList)
            {
                // Note: Programmatic action creation requires more complex reflection
                // This is a simplified placeholder
                Debug.Log($"Created InputActionAsset at {assetPath}. Custom actions should be configured in the Inspector.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return CreateSuccessResponse(("assetPath", assetPath));
        }

        #endregion

        #region Inspect

        private object InspectPlayerInput(Dictionary<string, object> payload)
        {
            var playerInputType = Type.GetType("UnityEngine.InputSystem.PlayerInput, Unity.InputSystem");
            if (playerInputType == null)
            {
                return CreateSuccessResponse(("hasInputSystem", false));
            }

            var gameObjectPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required for inspect.");
            }

            var go = ResolveGameObject(gameObjectPath);
            var playerInput = go.GetComponent(playerInputType);

            if (playerInput == null)
            {
                return CreateSuccessResponse(("hasPlayerInput", false), ("path", BuildGameObjectPath(go)));
            }

            var type = playerInput.GetType();
            var info = new Dictionary<string, object>
            {
                { "path", BuildGameObjectPath(go) },
                { "hasPlayerInput", true }
            };

            // Get actions
            var actionsProperty = type.GetProperty("actions");
            if (actionsProperty != null)
            {
                var actions = actionsProperty.GetValue(playerInput);
                if (actions != null)
                {
                    info["actionsAssetPath"] = AssetDatabase.GetAssetPath(actions as UnityEngine.Object);
                }
            }

            // Get default action map
            var defaultActionMapProperty = type.GetProperty("defaultActionMap");
            if (defaultActionMapProperty != null)
            {
                var defaultActionMap = defaultActionMapProperty.GetValue(playerInput);
                if (defaultActionMap != null)
                {
                    info["defaultActionMap"] = defaultActionMap.ToString();
                }
            }

            // Get notification behavior
            var notificationBehaviorProperty = type.GetProperty("notificationBehavior");
            if (notificationBehaviorProperty != null)
            {
                var notificationBehavior = notificationBehaviorProperty.GetValue(playerInput);
                if (notificationBehavior != null)
                {
                    info["notificationBehavior"] = notificationBehavior.ToString();
                }
            }

            return CreateSuccessResponse(("playerInput", info));
        }

        #endregion

        #region Helpers

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

