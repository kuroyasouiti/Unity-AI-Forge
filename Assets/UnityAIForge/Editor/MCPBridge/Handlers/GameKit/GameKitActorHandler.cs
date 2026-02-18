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
    /// GameKit Actor handler: create and manage game actors as controller-to-behavior hubs.
    /// Uses code generation to produce standalone Actor scripts with zero package dependency.
    /// </summary>
    public class GameKitActorHandler : BaseCommandHandler
    {
        private static readonly string[] Operations = { "create", "update", "inspect", "delete" };

        public override string Category => "gamekitActor";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateActor(payload),
                "update" => UpdateActor(payload),
                "inspect" => InspectActor(payload),
                "delete" => DeleteActor(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit Actor operation: {operation}"),
            };
        }

        #region Create Actor

        private object CreateActor(Dictionary<string, object> payload)
        {
            var actorId = GetString(payload, "actorId") ?? $"Actor_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var parentPath = GetString(payload, "parentPath");
            var behaviorStr = GetString(payload, "behaviorProfile") ?? "2dLinear";
            var controlStr = GetString(payload, "controlMode") ?? "directController";

            // Create GameObject
            var actorGo = new GameObject(actorId);
            Undo.RegisterCreatedObjectUndo(actorGo, "Create GameKit Actor");

            if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = ResolveGameObject(parentPath);
                actorGo.transform.SetParent(parent.transform, false);
            }

            // Set position
            if (payload.TryGetValue("position", out var posObj) && posObj is Dictionary<string, object> posDict)
            {
                actorGo.transform.position = GetVector3FromDict(posDict, Vector3.zero);
            }

            var behaviorProfile = ParseBehaviorProfile(behaviorStr);
            var controlMode = ParseControlMode(controlStr);
            var moveSpeed = GetFloat(payload, "moveSpeed", 5f);
            var acceleration = GetFloat(payload, "acceleration", 10f);
            var jumpForce = GetFloat(payload, "jumpForce", 5f);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(actorId, "Actor");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "ACTOR_ID", actorId },
                { "BEHAVIOR_PROFILE", behaviorProfile },
                { "CONTROL_MODE", controlMode },
                { "MOVE_SPEED", moveSpeed },
                { "ACCELERATION", acceleration },
                { "JUMP_FORCE", jumpForce }
            };

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                actorGo, "Actor", actorId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate Actor script.");
            }

            // Apply behavior-specific Unity components
            ApplyBehaviorComponents(actorGo, behaviorStr);

            // Apply control-specific components
            ApplyControlComponents(actorGo, controlStr);

            // Load sprite or model
            LoadVisuals(actorGo, payload, behaviorStr);

            EditorSceneManager.MarkSceneDirty(actorGo.scene);

            result["actorId"] = actorId;
            result["path"] = BuildGameObjectPath(actorGo);
            result["behaviorProfile"] = behaviorProfile;
            result["controlMode"] = controlMode;

            return result;
        }

        private void ApplyBehaviorComponents(GameObject go, string behaviorStr)
        {
            switch (behaviorStr.ToLowerInvariant())
            {
                case "2dphysics":
                    if (go.GetComponent<Rigidbody2D>() == null) Undo.AddComponent<Rigidbody2D>(go);
                    if (go.GetComponent<BoxCollider2D>() == null) Undo.AddComponent<BoxCollider2D>(go);
                    break;
                case "3dcharactercontroller":
                    if (go.GetComponent<CharacterController>() == null) Undo.AddComponent<CharacterController>(go);
                    break;
                case "3dphysics":
                    if (go.GetComponent<Rigidbody>() == null) Undo.AddComponent<Rigidbody>(go);
                    if (go.GetComponent<CapsuleCollider>() == null) Undo.AddComponent<CapsuleCollider>(go);
                    break;
                case "3dnavmesh":
                    if (go.GetComponent<UnityEngine.AI.NavMeshAgent>() == null) Undo.AddComponent<UnityEngine.AI.NavMeshAgent>(go);
                    break;
            }
        }

        private void ApplyControlComponents(GameObject go, string controlStr)
        {
            switch (controlStr.ToLowerInvariant())
            {
                case "directcontroller":
                    // Try to add Input System controller
                    var playerInputType = System.Type.GetType("UnityEngine.InputSystem.PlayerInput, Unity.InputSystem");
                    if (playerInputType != null)
                    {
                        var inputActions = GetOrCreateDefaultInputActions();
                        var playerInput = Undo.AddComponent(go, playerInputType);

                        var serializedInput = new SerializedObject(playerInput);
                        if (inputActions != null)
                        {
                            var actionsProp = serializedInput.FindProperty("m_Actions");
                            if (actionsProp != null)
                                actionsProp.objectReferenceValue = inputActions;
                        }
                        var notificationBehaviorProp = serializedInput.FindProperty("m_NotificationBehavior");
                        if (notificationBehaviorProp != null)
                            notificationBehaviorProp.intValue = 2; // SendMessages
                        var defaultActionMapProp = serializedInput.FindProperty("m_DefaultActionMap");
                        if (defaultActionMapProp != null)
                            defaultActionMapProp.stringValue = "Player";
                        serializedInput.ApplyModifiedProperties();
                    }
                    break;
            }
        }

        private void LoadVisuals(GameObject go, Dictionary<string, object> payload, string behaviorStr)
        {
            var visualType = GetString(payload, "visualType");
            var lower = behaviorStr.ToLowerInvariant();

            bool use2DVisual = lower == "2dlinear" || lower == "2dphysics" || lower == "2dtilegrid" ||
                               (lower == "splinemovement" && visualType != "3d");

            if (use2DVisual)
            {
                var spritePath = GetString(payload, "spritePath");
                if (!string.IsNullOrEmpty(spritePath))
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if (sprite != null)
                    {
                        var spriteRenderer = Undo.AddComponent<SpriteRenderer>(go);
                        spriteRenderer.sprite = sprite;
                    }
                }
                else
                {
                    Undo.AddComponent<SpriteRenderer>(go);
                }
            }
            else
            {
                var modelPath = GetString(payload, "modelPath");
                if (!string.IsNullOrEmpty(modelPath))
                {
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                    if (model != null)
                    {
                        var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
                        if (instance != null)
                        {
                            Undo.RegisterCreatedObjectUndo(instance, "Instantiate Model");
                            instance.transform.SetParent(go.transform, false);
                        }
                    }
                }
            }
        }

        #endregion

        #region Update Actor

        private object UpdateActor(Dictionary<string, object> payload)
        {
            var component = ResolveActorComponent(payload);

            Undo.RecordObject(component, "Update Actor");

            var so = new SerializedObject(component);

            if (payload.TryGetValue("behaviorProfile", out var behaviorObj))
            {
                var behaviorName = ParseBehaviorProfile(behaviorObj.ToString());
                var prop = so.FindProperty("behaviorProfile");
                var names = prop.enumDisplayNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], behaviorName, StringComparison.OrdinalIgnoreCase))
                    {
                        prop.enumValueIndex = i;
                        break;
                    }
                }
            }

            if (payload.TryGetValue("controlMode", out var controlObj))
            {
                var controlName = ParseControlMode(controlObj.ToString());
                var prop = so.FindProperty("controlMode");
                var names = prop.enumDisplayNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], controlName, StringComparison.OrdinalIgnoreCase))
                    {
                        prop.enumValueIndex = i;
                        break;
                    }
                }
            }

            if (payload.TryGetValue("moveSpeed", out var speedObj))
                so.FindProperty("moveSpeed").floatValue = Convert.ToSingle(speedObj);

            if (payload.TryGetValue("acceleration", out var accelObj))
                so.FindProperty("acceleration").floatValue = Convert.ToSingle(accelObj);

            if (payload.TryGetValue("jumpForce", out var jumpObj))
                so.FindProperty("jumpForce").floatValue = Convert.ToSingle(jumpObj);

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var actorId = new SerializedObject(component).FindProperty("actorId").stringValue;

            return CreateSuccessResponse(
                ("actorId", actorId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect Actor

        private object InspectActor(Dictionary<string, object> payload)
        {
            var component = ResolveActorComponent(payload);
            var so = new SerializedObject(component);

            var behaviorProp = so.FindProperty("behaviorProfile");
            var controlProp = so.FindProperty("controlMode");

            var info = new Dictionary<string, object>
            {
                { "found", true },
                { "actorId", so.FindProperty("actorId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "behaviorProfile", behaviorProp.enumValueIndex < behaviorProp.enumDisplayNames.Length
                    ? behaviorProp.enumDisplayNames[behaviorProp.enumValueIndex] : "Linear2D" },
                { "controlMode", controlProp.enumValueIndex < controlProp.enumDisplayNames.Length
                    ? controlProp.enumDisplayNames[controlProp.enumValueIndex] : "DirectController" },
                { "moveSpeed", so.FindProperty("moveSpeed").floatValue },
                { "acceleration", so.FindProperty("acceleration").floatValue },
                { "jumpForce", so.FindProperty("jumpForce").floatValue }
            };

            return CreateSuccessResponse(("actor", info));
        }

        #endregion

        #region Delete Actor

        private object DeleteActor(Dictionary<string, object> payload)
        {
            var component = ResolveActorComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var actorId = new SerializedObject(component).FindProperty("actorId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component.gameObject);

            // Clean up generated script from tracker
            ScriptGenerator.Delete(actorId);

            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("actorId", actorId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Helpers

        private Component ResolveActorComponent(Dictionary<string, object> payload)
        {
            // Try by actorId first
            var actorId = GetString(payload, "actorId");
            if (!string.IsNullOrEmpty(actorId))
            {
                var actorById = CodeGenHelper.FindComponentInSceneByField("actorId", actorId);
                if (actorById != null)
                    return actorById;
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var actorByPath = CodeGenHelper.FindComponentByField(targetGo, "actorId", null);
                    if (actorByPath != null)
                        return actorByPath;

                    throw new InvalidOperationException($"No Actor component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either actorId or targetPath is required.");
        }

        private string ParseBehaviorProfile(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "2dlinear" => "Linear2D",
                "2dphysics" => "Physics2D",
                "2dtilegrid" => "TileGrid",
                "graphnode" or "graph" => "GraphNode",
                "splinemovement" or "spline" or "rail" => "SplineMovement",
                "3dcharactercontroller" => "CharacterController3D",
                "3dphysics" => "Physics3D",
                "3dnavmesh" => "NavMesh",
                _ => "Linear2D"
            };
        }

        private string ParseControlMode(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "directcontroller" => "DirectController",
                "aiautonomous" => "AIAutonomous",
                "uicommand" => "UICommand",
                "scripttriggeronly" => "ScriptTriggerOnly",
                _ => "DirectController"
            };
        }

        /// <summary>
        /// Creates or gets a default Input Actions asset for GameKit.
        /// </summary>
        private UnityEngine.Object GetOrCreateDefaultInputActions()
        {
            var inputActionsType = System.Type.GetType("UnityEngine.InputSystem.InputActionAsset, Unity.InputSystem");
            if (inputActionsType == null)
                return null;

            const string defaultPath = "Assets/UnityAIForge/GameKit/Runtime/DefaultGameKitInputActions.inputactions";
            var existingAsset = AssetDatabase.LoadAssetAtPath(defaultPath, inputActionsType);
            if (existingAsset != null)
                return existingAsset;

            var inputActionsJson = @"{
    ""name"": ""DefaultGameKitInputActions"",
    ""maps"": [
        {
            ""name"": ""Player"",
            ""id"": ""d8f91a5c-1c5e-4c9a-9a1c-5c9a9a1c5c9a"",
            ""actions"": [
                { ""name"": ""Move"", ""type"": ""Value"", ""id"": ""1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d"", ""expectedControlType"": ""Vector2"" },
                { ""name"": ""Look"", ""type"": ""Value"", ""id"": ""2b3c4d5e-6f7a-8b9c-0d1e-2f3a4b5c6d7e"", ""expectedControlType"": ""Vector2"" },
                { ""name"": ""Jump"", ""type"": ""Button"", ""id"": ""3c4d5e6f-7a8b-9c0d-1e2f-3a4b5c6d7e8f"", ""expectedControlType"": ""Button"" },
                { ""name"": ""Action"", ""type"": ""Button"", ""id"": ""4d5e6f7a-8b9c-0d1e-2f3a-4b5c6d7e8f9a"", ""expectedControlType"": ""Button"" },
                { ""name"": ""Fire"", ""type"": ""Button"", ""id"": ""5e6f7a8b-9c0d-1e2f-3a4b-5c6d7e8f9a0b"", ""expectedControlType"": ""Button"" }
            ],
            ""bindings"": [
                { ""name"": ""WASD"", ""id"": ""6f7a8b9c-0d1e-2f3a-4b5c-6d7e8f9a0b1c"", ""path"": ""2DVector"", ""action"": ""Move"", ""isComposite"": true },
                { ""name"": ""up"", ""id"": ""7a8b9c0d-1e2f-3a4b-5c6d-7e8f9a0b1c2d"", ""path"": ""<Keyboard>/w"", ""action"": ""Move"", ""isPartOfComposite"": true },
                { ""name"": ""down"", ""id"": ""8b9c0d1e-2f3a-4b5c-6d7e-8f9a0b1c2d3e"", ""path"": ""<Keyboard>/s"", ""action"": ""Move"", ""isPartOfComposite"": true },
                { ""name"": ""left"", ""id"": ""9c0d1e2f-3a4b-5c6d-7e8f-9a0b1c2d3e4f"", ""path"": ""<Keyboard>/a"", ""action"": ""Move"", ""isPartOfComposite"": true },
                { ""name"": ""right"", ""id"": ""0d1e2f3a-4b5c-6d7e-8f9a-0b1c2d3e4f5a"", ""path"": ""<Keyboard>/d"", ""action"": ""Move"", ""isPartOfComposite"": true },
                { ""id"": ""1e2f3a4b-5c6d-7e8f-9a0b-1c2d3e4f5a6b"", ""path"": ""<Gamepad>/leftStick"", ""action"": ""Move"" },
                { ""id"": ""2f3a4b5c-6d7e-8f9a-0b1c-2d3e4f5a6b7c"", ""path"": ""<Mouse>/delta"", ""action"": ""Look"" },
                { ""id"": ""3a4b5c6d-7e8f-9a0b-1c2d-3e4f5a6b7c8d"", ""path"": ""<Gamepad>/rightStick"", ""action"": ""Look"" },
                { ""id"": ""4b5c6d7e-8f9a-0b1c-2d3e-4f5a6b7c8d9e"", ""path"": ""<Keyboard>/space"", ""action"": ""Jump"" },
                { ""id"": ""5c6d7e8f-9a0b-1c2d-3e4f-5a6b7c8d9e0f"", ""path"": ""<Gamepad>/buttonSouth"", ""action"": ""Jump"" },
                { ""id"": ""6d7e8f9a-0b1c-2d3e-4f5a-6b7c8d9e0f1a"", ""path"": ""<Keyboard>/e"", ""action"": ""Action"" },
                { ""id"": ""7e8f9a0b-1c2d-3e4f-5a6b-7c8d9e0f1a2b"", ""path"": ""<Gamepad>/buttonWest"", ""action"": ""Action"" },
                { ""id"": ""8f9a0b1c-2d3e-4f5a-6b7c-8d9e0f1a2b3c"", ""path"": ""<Mouse>/leftButton"", ""action"": ""Fire"" },
                { ""id"": ""9a0b1c2d-3e4f-5a6b-7c8d-9e0f1a2b3c4d"", ""path"": ""<Gamepad>/rightTrigger"", ""action"": ""Fire"" }
            ]
        }
    ],
    ""controlSchemes"": []
}";

            var directory = System.IO.Path.GetDirectoryName(defaultPath);
            if (!System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);

            System.IO.File.WriteAllText(defaultPath, inputActionsJson);
            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath(defaultPath, inputActionsType);
        }

        #endregion
    }
}
