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
            var assetPath = GetString(payload, "inputActionsAssetPath");
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("inputActionsAssetPath is required for createInputActions.");
            }

            if (!assetPath.EndsWith(".inputactions"))
            {
                assetPath += ".inputactions";
            }

            // Check for preset
            var preset = GetString(payload, "preset")?.ToLowerInvariant();
            string jsonContent;

            if (!string.IsNullOrEmpty(preset) && preset != "custom")
            {
                jsonContent = GetPresetJson(preset, assetPath);
            }
            else if (payload.TryGetValue("actionMaps", out var mapsObj) && mapsObj is List<object> mapsList)
            {
                jsonContent = BuildInputActionsJson(assetPath, mapsList);
            }
            else
            {
                // Empty InputActions with Player map
                jsonContent = BuildEmptyInputActionsJson(assetPath);
            }

            // Ensure directory exists
            var dir = System.IO.Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            System.IO.File.WriteAllText(assetPath, jsonContent);
            AssetDatabase.Refresh();

            return CreateSuccessResponse(
                ("assetPath", assetPath),
                ("preset", preset ?? "custom"),
                ("message", $"InputActions asset created at {assetPath}")
            );
        }

        private string BuildEmptyInputActionsJson(string assetPath)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            return "{\n" +
                   $"    \"name\": \"{name}\",\n" +
                   "    \"maps\": [],\n" +
                   "    \"controlSchemes\": []\n" +
                   "}";
        }

        private string BuildInputActionsJson(string assetPath, List<object> mapsList)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            var mapsJson = new List<string>();

            foreach (var mapObj in mapsList)
            {
                if (mapObj is not Dictionary<string, object> mapDef) continue;
                var mapName = mapDef.ContainsKey("name") ? mapDef["name"]?.ToString() : "Default";
                var mapId = Guid.NewGuid().ToString();

                var actions = new List<string>();
                var bindings = new List<string>();

                if (mapDef.TryGetValue("actions", out var actionsObj) && actionsObj is List<object> actionsList)
                {
                    foreach (var actionObj in actionsList)
                    {
                        if (actionObj is not Dictionary<string, object> actionDef) continue;
                        var actionName = actionDef.ContainsKey("name") ? actionDef["name"]?.ToString() : "Action";
                        var actionType = actionDef.ContainsKey("type") ? actionDef["type"]?.ToString() : "Button";
                        var valueType = actionDef.ContainsKey("valueType") ? actionDef["valueType"]?.ToString() : "";
                        var actionId = Guid.NewGuid().ToString();

                        var expectedControlType = actionType == "Value" && !string.IsNullOrEmpty(valueType) ? valueType : (actionType == "Button" ? "Button" : "");

                        actions.Add(
                            "                {\n" +
                            $"                    \"name\": \"{actionName}\",\n" +
                            $"                    \"type\": \"{actionType}\",\n" +
                            $"                    \"id\": \"{actionId}\",\n" +
                            $"                    \"expectedControlType\": \"{expectedControlType}\",\n" +
                            "                    \"processors\": \"\",\n" +
                            "                    \"interactions\": \"\",\n" +
                            $"                    \"initialStateCheck\": {(actionType == "Value" ? "true" : "false")}\n" +
                            "                }");

                        // Add bindings
                        if (actionDef.TryGetValue("bindings", out var bindingsObj) && bindingsObj is List<object> bindingsList)
                        {
                            foreach (var bindingObj in bindingsList)
                            {
                                if (bindingObj is not Dictionary<string, object> bindingDef) continue;
                                var path = bindingDef.ContainsKey("path") ? bindingDef["path"]?.ToString() : "";
                                var bindingId = Guid.NewGuid().ToString();

                                bindings.Add(
                                    "                {\n" +
                                    "                    \"name\": \"\",\n" +
                                    $"                    \"id\": \"{bindingId}\",\n" +
                                    $"                    \"path\": \"{path}\",\n" +
                                    "                    \"interactions\": \"\",\n" +
                                    "                    \"processors\": \"\",\n" +
                                    "                    \"groups\": \"\",\n" +
                                    $"                    \"action\": \"{actionName}\",\n" +
                                    "                    \"isComposite\": false,\n" +
                                    "                    \"isPartOfComposite\": false\n" +
                                    "                }");
                            }
                        }

                        // Handle composite bindings (e.g., 2DVector for WASD)
                        if (actionDef.TryGetValue("compositeBindings", out var compObj) && compObj is List<object> compositeList)
                        {
                            foreach (var compItem in compositeList)
                            {
                                if (compItem is not Dictionary<string, object> compDef) continue;
                                var compName = compDef.ContainsKey("name") ? compDef["name"]?.ToString() : "WASD";
                                var compType = compDef.ContainsKey("compositeType") ? compDef["compositeType"]?.ToString() : "2DVector";
                                var compId = Guid.NewGuid().ToString();

                                // Composite parent
                                bindings.Add(
                                    "                {\n" +
                                    $"                    \"name\": \"{compName}\",\n" +
                                    $"                    \"id\": \"{compId}\",\n" +
                                    $"                    \"path\": \"{compType}\",\n" +
                                    "                    \"interactions\": \"\",\n" +
                                    "                    \"processors\": \"\",\n" +
                                    "                    \"groups\": \"\",\n" +
                                    $"                    \"action\": \"{actionName}\",\n" +
                                    "                    \"isComposite\": true,\n" +
                                    "                    \"isPartOfComposite\": false\n" +
                                    "                }");

                                // Composite parts
                                if (compDef.TryGetValue("parts", out var partsObj) && partsObj is Dictionary<string, object> partsDef)
                                {
                                    foreach (var kvp in partsDef)
                                    {
                                        var partId = Guid.NewGuid().ToString();
                                        bindings.Add(
                                            "                {\n" +
                                            $"                    \"name\": \"{kvp.Key}\",\n" +
                                            $"                    \"id\": \"{partId}\",\n" +
                                            $"                    \"path\": \"{kvp.Value}\",\n" +
                                            "                    \"interactions\": \"\",\n" +
                                            "                    \"processors\": \"\",\n" +
                                            "                    \"groups\": \"\",\n" +
                                            $"                    \"action\": \"{actionName}\",\n" +
                                            "                    \"isComposite\": false,\n" +
                                            "                    \"isPartOfComposite\": true\n" +
                                            "                }");
                                    }
                                }
                            }
                        }
                    }
                }

                mapsJson.Add(
                    "        {\n" +
                    $"            \"name\": \"{mapName}\",\n" +
                    $"            \"id\": \"{mapId}\",\n" +
                    "            \"actions\": [\n" +
                    string.Join(",\n", actions) + "\n" +
                    "            ],\n" +
                    "            \"bindings\": [\n" +
                    string.Join(",\n", bindings) + "\n" +
                    "            ]\n" +
                    "        }");
            }

            return "{\n" +
                   $"    \"name\": \"{name}\",\n" +
                   "    \"maps\": [\n" +
                   string.Join(",\n", mapsJson) + "\n" +
                   "    ],\n" +
                   "    \"controlSchemes\": []\n" +
                   "}";
        }

        private string GetPresetJson(string preset, string assetPath)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            return preset switch
            {
                "shooter2d" => BuildShooter2DPreset(name),
                "platformer2d" => BuildPlatformer2DPreset(name),
                _ => throw new InvalidOperationException(
                    $"Unknown input preset: {preset}. Available: shooter2d, platformer2d, custom")
            };
        }

        private string BuildShooter2DPreset(string name)
        {
            var mapsList = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["name"] = "Player",
                    ["actions"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["name"] = "Move", ["type"] = "Value", ["valueType"] = "Vector2",
                            ["compositeBindings"] = new List<object>
                            {
                                new Dictionary<string, object>
                                {
                                    ["name"] = "WASD", ["compositeType"] = "2DVector",
                                    ["parts"] = new Dictionary<string, object>
                                    {
                                        ["up"] = "<Keyboard>/w", ["down"] = "<Keyboard>/s",
                                        ["left"] = "<Keyboard>/a", ["right"] = "<Keyboard>/d"
                                    }
                                },
                                new Dictionary<string, object>
                                {
                                    ["name"] = "Arrows", ["compositeType"] = "2DVector",
                                    ["parts"] = new Dictionary<string, object>
                                    {
                                        ["up"] = "<Keyboard>/upArrow", ["down"] = "<Keyboard>/downArrow",
                                        ["left"] = "<Keyboard>/leftArrow", ["right"] = "<Keyboard>/rightArrow"
                                    }
                                }
                            }
                        },
                        new Dictionary<string, object>
                        {
                            ["name"] = "Shoot", ["type"] = "Button",
                            ["bindings"] = new List<object>
                            {
                                new Dictionary<string, object> { ["path"] = "<Keyboard>/z" },
                                new Dictionary<string, object> { ["path"] = "<Gamepad>/buttonSouth" }
                            }
                        },
                        new Dictionary<string, object>
                        {
                            ["name"] = "Bomb", ["type"] = "Button",
                            ["bindings"] = new List<object>
                            {
                                new Dictionary<string, object> { ["path"] = "<Keyboard>/x" },
                                new Dictionary<string, object> { ["path"] = "<Gamepad>/buttonWest" }
                            }
                        },
                        new Dictionary<string, object>
                        {
                            ["name"] = "SlowMove", ["type"] = "Button",
                            ["bindings"] = new List<object>
                            {
                                new Dictionary<string, object> { ["path"] = "<Keyboard>/leftShift" },
                                new Dictionary<string, object> { ["path"] = "<Gamepad>/leftShoulder" }
                            }
                        },
                        new Dictionary<string, object>
                        {
                            ["name"] = "Pause", ["type"] = "Button",
                            ["bindings"] = new List<object>
                            {
                                new Dictionary<string, object> { ["path"] = "<Keyboard>/escape" },
                                new Dictionary<string, object> { ["path"] = "<Gamepad>/start" }
                            }
                        }
                    }
                }
            };

            return BuildInputActionsJson($"dummy/{name}.inputactions", mapsList);
        }

        private string BuildPlatformer2DPreset(string name)
        {
            var mapsList = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["name"] = "Player",
                    ["actions"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["name"] = "Move", ["type"] = "Value", ["valueType"] = "Vector2",
                            ["compositeBindings"] = new List<object>
                            {
                                new Dictionary<string, object>
                                {
                                    ["name"] = "WASD", ["compositeType"] = "2DVector",
                                    ["parts"] = new Dictionary<string, object>
                                    {
                                        ["up"] = "<Keyboard>/w", ["down"] = "<Keyboard>/s",
                                        ["left"] = "<Keyboard>/a", ["right"] = "<Keyboard>/d"
                                    }
                                },
                                new Dictionary<string, object>
                                {
                                    ["name"] = "Arrows", ["compositeType"] = "2DVector",
                                    ["parts"] = new Dictionary<string, object>
                                    {
                                        ["up"] = "<Keyboard>/upArrow", ["down"] = "<Keyboard>/downArrow",
                                        ["left"] = "<Keyboard>/leftArrow", ["right"] = "<Keyboard>/rightArrow"
                                    }
                                }
                            }
                        },
                        new Dictionary<string, object>
                        {
                            ["name"] = "Jump", ["type"] = "Button",
                            ["bindings"] = new List<object>
                            {
                                new Dictionary<string, object> { ["path"] = "<Keyboard>/space" },
                                new Dictionary<string, object> { ["path"] = "<Gamepad>/buttonSouth" }
                            }
                        },
                        new Dictionary<string, object>
                        {
                            ["name"] = "Attack", ["type"] = "Button",
                            ["bindings"] = new List<object>
                            {
                                new Dictionary<string, object> { ["path"] = "<Keyboard>/z" },
                                new Dictionary<string, object> { ["path"] = "<Gamepad>/buttonWest" }
                            }
                        },
                        new Dictionary<string, object>
                        {
                            ["name"] = "Dash", ["type"] = "Button",
                            ["bindings"] = new List<object>
                            {
                                new Dictionary<string, object> { ["path"] = "<Keyboard>/leftShift" },
                                new Dictionary<string, object> { ["path"] = "<Gamepad>/rightShoulder" }
                            }
                        },
                        new Dictionary<string, object>
                        {
                            ["name"] = "Pause", ["type"] = "Button",
                            ["bindings"] = new List<object>
                            {
                                new Dictionary<string, object> { ["path"] = "<Keyboard>/escape" },
                                new Dictionary<string, object> { ["path"] = "<Gamepad>/start" }
                            }
                        }
                    }
                }
            };

            return BuildInputActionsJson($"dummy/{name}.inputactions", mapsList);
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

    }
}

