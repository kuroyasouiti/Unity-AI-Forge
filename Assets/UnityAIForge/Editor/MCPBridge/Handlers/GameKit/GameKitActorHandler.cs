using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using UnityAIForge.GameKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

#if UNITY_EDITOR
// Ensure GameKit Runtime assembly is referenced
#endif

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Actor handler: create and manage game actors as controller-to-behavior hubs.
    /// Actors relay input from controllers to behavior components via UnityEvents.
    /// </summary>
    public class GameKitActorHandler : BaseCommandHandler
    {
        private static readonly string[] Operations = { "create", "update", "inspect", "delete" };

        public override string Category => "gamekitActor";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

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

            // Add GameKitActor component
            var actor = Undo.AddComponent<GameKitActor>(actorGo);
            var behavior = ParseBehaviorProfile(behaviorStr);
            var control = ParseControlMode(controlStr);
            actor.Initialize(actorId, behavior, control);

            // Apply behavior-specific components
            ApplyBehaviorComponents(actorGo, behavior);

            // Apply control-specific components
            ApplyControlComponents(actorGo, control);

            // Load sprite or model
            LoadVisuals(actorGo, payload, behavior);

            EditorSceneManager.MarkSceneDirty(actorGo.scene);
            return CreateSuccessResponse(("actorId", actorId), ("path", BuildGameObjectPath(actorGo)));
        }

        private void ApplyBehaviorComponents(GameObject go, GameKitActor.BehaviorProfile behavior)
        {
            switch (behavior)
            {
                case GameKitActor.BehaviorProfile.TwoDPhysics:
                    Undo.AddComponent<Rigidbody2D>(go);
                    Undo.AddComponent<BoxCollider2D>(go);
                    break;

                case GameKitActor.BehaviorProfile.TwoDTileGrid:
                    var tileGridType = System.Type.GetType("UnityAIForge.GameKit.TileGridMovement, UnityAIForge.GameKit.Runtime");
                    if (tileGridType != null)
                    {
                        var tileGridComponent = Undo.AddComponent(go, tileGridType);

                        // Auto-find Grid and Tilemaps in scene
                        var autoFindMethod = tileGridType.GetMethod("AutoFindTilemaps");
                        if (autoFindMethod != null)
                        {
                            autoFindMethod.Invoke(tileGridComponent, null);
                        }
                    }
                    break;

                case GameKitActor.BehaviorProfile.GraphNode:
                    var graphNodeMovementType = System.Type.GetType("UnityAIForge.GameKit.GraphNodeMovement, UnityAIForge.GameKit.Runtime");
                    if (graphNodeMovementType != null)
                    {
                        Undo.AddComponent(go, graphNodeMovementType);
                    }
                    break;

                case GameKitActor.BehaviorProfile.SplineMovement:
                    var splineMovementType = System.Type.GetType("UnityAIForge.GameKit.SplineMovement, UnityAIForge.GameKit.Runtime");
                    if (splineMovementType != null)
                    {
                        Undo.AddComponent(go, splineMovementType);
                    }
                    break;

                case GameKitActor.BehaviorProfile.ThreeDCharacterController:
                    Undo.AddComponent<CharacterController>(go);
                    break;

                case GameKitActor.BehaviorProfile.ThreeDPhysics:
                    Undo.AddComponent<Rigidbody>(go);
                    Undo.AddComponent<CapsuleCollider>(go);
                    break;

                case GameKitActor.BehaviorProfile.ThreeDNavMesh:
                    var navAgent = Undo.AddComponent<UnityEngine.AI.NavMeshAgent>(go);
                    break;
            }
        }

        private void ApplyControlComponents(GameObject go, GameKitActor.ControlMode control)
        {
            switch (control)
            {
                case GameKitActor.ControlMode.DirectController:
                    // Priority 1: Try to add Input System controller
                    var inputSystemControllerType = System.Type.GetType("UnityAIForge.GameKit.GameKitInputSystemController, UnityAIForge.GameKit.Runtime");
                    var playerInputType = System.Type.GetType("UnityEngine.InputSystem.PlayerInput, Unity.InputSystem");
                    
                    if (inputSystemControllerType != null && playerInputType != null)
                    {
                        // Get or create default input actions
                        var inputActions = GetOrCreateDefaultInputActions();
                        
                        // Add PlayerInput component first
                        var playerInput = Undo.AddComponent(go, playerInputType);
                        
                        // Configure PlayerInput
                        var serializedInput = new SerializedObject(playerInput);
                        
                        // Set input actions asset
                        if (inputActions != null)
                        {
                            var actionsProp = serializedInput.FindProperty("m_Actions");
                            if (actionsProp != null)
                            {
                                actionsProp.objectReferenceValue = inputActions;
                            }
                        }
                        
                        // Set notification behavior to SendMessages
                        var notificationBehaviorProp = serializedInput.FindProperty("m_NotificationBehavior");
                        if (notificationBehaviorProp != null)
                        {
                            notificationBehaviorProp.intValue = 2; // SendMessages
                        }
                        
                        // Set default action map to "Player"
                        var defaultActionMapProp = serializedInput.FindProperty("m_DefaultActionMap");
                        if (defaultActionMapProp != null)
                        {
                            defaultActionMapProp.stringValue = "Player";
                        }
                        
                        serializedInput.ApplyModifiedProperties();
                        
                        // Add our controller component
                        Undo.AddComponent(go, inputSystemControllerType);
                        
                        Debug.Log($"[GameKitActorHandler] Added Input System controller to {go.name}");
                    }
                    else
                    {
                        // Fallback: Use legacy input system
                        var simpleInputType = System.Type.GetType("UnityAIForge.GameKit.GameKitSimpleInput, UnityAIForge.GameKit.Runtime");
                        if (simpleInputType != null)
                        {
                            Undo.AddComponent(go, simpleInputType);
                            Debug.Log($"[GameKitActorHandler] Added legacy input controller to {go.name} (Input System not available)");
                        }
                        else
                        {
                            Debug.LogWarning($"[GameKitActorHandler] No input controller available for {go.name}");
                        }
                    }
                    break;

                case GameKitActor.ControlMode.AIAutonomous:
                    // Add simple AI controller for autonomous behavior
                    var simpleAIType = System.Type.GetType("UnityAIForge.GameKit.GameKitSimpleAI, UnityAIForge.GameKit.Runtime");
                    if (simpleAIType != null)
                    {
                        Undo.AddComponent(go, simpleAIType);
                    }
                    break;

                case GameKitActor.ControlMode.UICommand:
                    // UI Command mode doesn't require additional components on the actor.
                    // GameKitUICommand components are typically added to UI elements instead.
                    // The actor will receive commands via SendMessage or UnityEvents from UI.
                    break;

                case GameKitActor.ControlMode.ScriptTriggerOnly:
                    // Script-only control doesn't require additional components.
                    // The actor's input methods (SendMoveInput, etc.) will be called directly from scripts.
                    break;
            }
        }

        private void LoadVisuals(GameObject go, Dictionary<string, object> payload, GameKitActor.BehaviorProfile behavior)
        {
            // Check if visual type is explicitly specified for 2.5D games
            var visualType = GetString(payload, "visualType");
            
            bool use2DVisual = behavior == GameKitActor.BehaviorProfile.TwoDLinear ||
                               behavior == GameKitActor.BehaviorProfile.TwoDPhysics ||
                               behavior == GameKitActor.BehaviorProfile.TwoDTileGrid ||
                               (behavior == GameKitActor.BehaviorProfile.SplineMovement && visualType != "3d");
            
            if (use2DVisual)
            {
                // 2D sprite (or 2.5D sprite)
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
                    // Add default sprite renderer
                    Undo.AddComponent<SpriteRenderer>(go);
                }
            }
            else
            {
                // 3D model
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
            var actorId = GetString(payload, "actorId");
            if (string.IsNullOrEmpty(actorId))
            {
                throw new InvalidOperationException("actorId is required for update.");
            }

            var actor = FindActorById(actorId);
            if (actor == null)
            {
                throw new InvalidOperationException($"Actor with ID '{actorId}' not found.");
            }

            Undo.RecordObject(actor, "Update GameKit Actor");

            // Update behavior profile if provided
            if (payload.TryGetValue("behaviorProfile", out var behaviorObj))
            {
                var behavior = ParseBehaviorProfile(behaviorObj.ToString());
                var serializedActor = new UnityEditor.SerializedObject(actor);
                serializedActor.FindProperty("behaviorProfile").enumValueIndex = (int)behavior;
                serializedActor.ApplyModifiedProperties();
            }

            // Update control mode if provided
            if (payload.TryGetValue("controlMode", out var controlObj))
            {
                var control = ParseControlMode(controlObj.ToString());
                var serializedActor = new UnityEditor.SerializedObject(actor);
                serializedActor.FindProperty("controlMode").enumValueIndex = (int)control;
                serializedActor.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(actor.gameObject.scene);
            return CreateSuccessResponse(("actorId", actorId), ("path", BuildGameObjectPath(actor.gameObject)));
        }

        #endregion

        #region Inspect Actor

        private object InspectActor(Dictionary<string, object> payload)
        {
            var actorId = GetString(payload, "actorId");
            if (string.IsNullOrEmpty(actorId))
            {
                throw new InvalidOperationException("actorId is required for inspect.");
            }

            var actor = FindActorById(actorId);
            if (actor == null)
            {
                return CreateSuccessResponse(("found", false), ("actorId", actorId));
            }

            var info = new Dictionary<string, object>
            {
                { "found", true },
                { "actorId", actor.ActorId },
                { "path", BuildGameObjectPath(actor.gameObject) },
                { "behaviorProfile", actor.Behavior.ToString() },
                { "controlMode", actor.Control.ToString() }
            };

            return CreateSuccessResponse(("actor", info));
        }

        #endregion

        #region Delete Actor

        private object DeleteActor(Dictionary<string, object> payload)
        {
            var actorId = GetString(payload, "actorId");
            if (string.IsNullOrEmpty(actorId))
            {
                throw new InvalidOperationException("actorId is required for delete.");
            }

            var actor = FindActorById(actorId);
            if (actor == null)
            {
                throw new InvalidOperationException($"Actor with ID '{actorId}' not found.");
            }

            var scene = actor.gameObject.scene;
            Undo.DestroyObjectImmediate(actor.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(("actorId", actorId), ("deleted", true));
        }

        #endregion

        #region Helpers

        private GameKitActor FindActorById(string actorId)
        {
            var actors = UnityEngine.Object.FindObjectsByType<GameKitActor>(FindObjectsSortMode.None);
            foreach (var actor in actors)
            {
                if (actor.ActorId == actorId)
                {
                    return actor;
                }
            }
            return null;
        }

        private GameKitActor.BehaviorProfile ParseBehaviorProfile(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "2dlinear" => GameKitActor.BehaviorProfile.TwoDLinear,
                "2dphysics" => GameKitActor.BehaviorProfile.TwoDPhysics,
                "2dtilegrid" => GameKitActor.BehaviorProfile.TwoDTileGrid,
                "graphnode" => GameKitActor.BehaviorProfile.GraphNode,
                "graph" => GameKitActor.BehaviorProfile.GraphNode,
                "splinemovement" => GameKitActor.BehaviorProfile.SplineMovement,
                "spline" => GameKitActor.BehaviorProfile.SplineMovement,
                "rail" => GameKitActor.BehaviorProfile.SplineMovement,
                "3dcharactercontroller" => GameKitActor.BehaviorProfile.ThreeDCharacterController,
                "3dphysics" => GameKitActor.BehaviorProfile.ThreeDPhysics,
                "3dnavmesh" => GameKitActor.BehaviorProfile.ThreeDNavMesh,
                _ => GameKitActor.BehaviorProfile.TwoDLinear
            };
        }

        private GameKitActor.ControlMode ParseControlMode(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "directcontroller" => GameKitActor.ControlMode.DirectController,
                "aiautonomous" => GameKitActor.ControlMode.AIAutonomous,
                "uicommand" => GameKitActor.ControlMode.UICommand,
                "scripttriggeronly" => GameKitActor.ControlMode.ScriptTriggerOnly,
                _ => GameKitActor.ControlMode.DirectController
            };
        }

        private Vector3 GetVector3FromDict(Dictionary<string, object> dict, Vector3 fallback)
        {
            float x = dict.TryGetValue("x", out var xObj) ? Convert.ToSingle(xObj) : fallback.x;
            float y = dict.TryGetValue("y", out var yObj) ? Convert.ToSingle(yObj) : fallback.y;
            float z = dict.TryGetValue("z", out var zObj) ? Convert.ToSingle(zObj) : fallback.z;
            return new Vector3(x, y, z);
        }

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

        /// <summary>
        /// Creates or gets a default Input Actions asset for GameKit.
        /// </summary>
        private UnityEngine.Object GetOrCreateDefaultInputActions()
        {
            var inputActionsType = System.Type.GetType("UnityEngine.InputSystem.InputActionAsset, Unity.InputSystem");
            if (inputActionsType == null)
            {
                return null;
            }

            // Check if default asset already exists
            const string defaultPath = "Assets/UnityAIForge/GameKit/Runtime/DefaultGameKitInputActions.inputactions";
            var existingAsset = AssetDatabase.LoadAssetAtPath(defaultPath, inputActionsType);
            if (existingAsset != null)
            {
                return existingAsset;
            }

            // Create default Input Actions asset via JSON
            var inputActionsJson = @"{
    ""name"": ""DefaultGameKitInputActions"",
    ""maps"": [
        {
            ""name"": ""Player"",
            ""id"": ""d8f91a5c-1c5e-4c9a-9a1c-5c9a9a1c5c9a"",
            ""actions"": [
                {
                    ""name"": ""Move"",
                    ""type"": ""Value"",
                    ""id"": ""1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Look"",
                    ""type"": ""Value"",
                    ""id"": ""2b3c4d5e-6f7a-8b9c-0d1e-2f3a4b5c6d7e"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Jump"",
                    ""type"": ""Button"",
                    ""id"": ""3c4d5e6f-7a8b-9c0d-1e2f-3a4b5c6d7e8f"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Action"",
                    ""type"": ""Button"",
                    ""id"": ""4d5e6f7a-8b9c-0d1e-2f3a-4b5c6d7e8f9a"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Fire"",
                    ""type"": ""Button"",
                    ""id"": ""5e6f7a8b-9c0d-1e2f-3a4b-5c6d7e8f9a0b"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""WASD"",
                    ""id"": ""6f7a8b9c-0d1e-2f3a-4b5c-6d7e8f9a0b1c"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""7a8b9c0d-1e2f-3a4b-5c6d-7e8f9a0b1c2d"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""8b9c0d1e-2f3a-4b5c-6d7e-8f9a0b1c2d3e"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""9c0d1e2f-3a4b-5c6d-7e8f-9a0b1c2d3e4f"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""0d1e2f3a-4b5c-6d7e-8f9a-0b1c2d3e4f5a"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""1e2f3a4b-5c6d-7e8f-9a0b-1c2d3e4f5a6b"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""2f3a4b5c-6d7e-8f9a-0b1c-2d3e4f5a6b7c"",
                    ""path"": ""<Mouse>/delta"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Look"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""3a4b5c6d-7e8f-9a0b-1c2d-3e4f5a6b7c8d"",
                    ""path"": ""<Gamepad>/rightStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Look"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""4b5c6d7e-8f9a-0b1c-2d3e-4f5a6b7c8d9e"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Jump"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""5c6d7e8f-9a0b-1c2d-3e4f-5a6b7c8d9e0f"",
                    ""path"": ""<Gamepad>/buttonSouth"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Jump"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""6d7e8f9a-0b1c-2d3e-4f5a-6b7c8d9e0f1a"",
                    ""path"": ""<Keyboard>/e"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Action"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""7e8f9a0b-1c2d-3e4f-5a6b-7c8d9e0f1a2b"",
                    ""path"": ""<Gamepad>/buttonWest"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Action"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8f9a0b1c-2d3e-4f5a-6b7c-8d9e0f1a2b3c"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Fire"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""9a0b1c2d-3e4f-5a6b-7c8d-9e0f1a2b3c4d"",
                    ""path"": ""<Gamepad>/rightTrigger"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Fire"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}";

            // Ensure directory exists
            var directory = System.IO.Path.GetDirectoryName(defaultPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // Write JSON file
            System.IO.File.WriteAllText(defaultPath, inputActionsJson);
            AssetDatabase.Refresh();

            // Load and return the created asset
            return AssetDatabase.LoadAssetAtPath(defaultPath, inputActionsType);
        }

        #endregion
    }
}

