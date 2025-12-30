using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCP.Editor.Base;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Animation3D Bundle Handler: 3D Character Animation Setup.
    /// Enables LLMs to create and configure Animator Controllers, states, transitions, and blend trees.
    /// </summary>
    public class Animation3DBundleHandler : BaseCommandHandler
    {
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "setupAnimator",
            "createController",
            "addState",
            "addTransition",
            "setParameter",
            "addBlendTree",
            "createAvatarMask",
            "inspect",
            "delete",
            "listParameters",
            "listStates"
        };

        public override string Category => "Animation3DBundle";
        public override string Version => "1.0.0";

        protected override bool RequiresCompilationWait(string operation)
        {
            return false;
        }

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "setupAnimator" => HandleSetupAnimator(payload),
                "createController" => HandleCreateController(payload),
                "addState" => HandleAddState(payload),
                "addTransition" => HandleAddTransition(payload),
                "setParameter" => HandleSetParameter(payload),
                "addBlendTree" => HandleAddBlendTree(payload),
                "createAvatarMask" => HandleCreateAvatarMask(payload),
                "inspect" => HandleInspect(payload),
                "delete" => HandleDelete(payload),
                "listParameters" => HandleListParameters(payload),
                "listStates" => HandleListStates(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }

        /// <summary>
        /// Setup Animator component on a GameObject.
        /// </summary>
        private object HandleSetupAnimator(Dictionary<string, object> payload)
        {
            string gameObjectPath = GetString(payload, "gameObjectPath", null);
            string controllerPath = GetString(payload, "controllerPath", null);
            bool applyRootMotion = GetBool(payload, "applyRootMotion", false);
            string updateMode = GetString(payload, "updateMode", "Normal");
            string cullingMode = GetString(payload, "cullingMode", "AlwaysAnimate");

            if (string.IsNullOrEmpty(gameObjectPath))
            {
                throw new ArgumentException("gameObjectPath is required");
            }

            var go = GameObject.Find(gameObjectPath);
            if (go == null)
            {
                throw new ArgumentException($"GameObject not found: {gameObjectPath}");
            }

            var animator = go.GetComponent<Animator>();
            if (animator == null)
            {
                animator = go.AddComponent<Animator>();
            }

            // Set controller if provided
            if (!string.IsNullOrEmpty(controllerPath))
            {
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
                if (controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                }
                else
                {
                    throw new ArgumentException($"AnimatorController not found: {controllerPath}");
                }
            }

            animator.applyRootMotion = applyRootMotion;

            // Set update mode
            if (Enum.TryParse<AnimatorUpdateMode>(updateMode, true, out var updateModeEnum))
            {
                animator.updateMode = updateModeEnum;
            }

            // Set culling mode
            if (Enum.TryParse<AnimatorCullingMode>(cullingMode, true, out var cullingModeEnum))
            {
                animator.cullingMode = cullingModeEnum;
            }

            EditorUtility.SetDirty(go);

            return CreateSuccessResponse(
                ("gameObjectPath", gameObjectPath),
                ("hasController", animator.runtimeAnimatorController != null),
                ("applyRootMotion", animator.applyRootMotion),
                ("updateMode", animator.updateMode.ToString()),
                ("cullingMode", animator.cullingMode.ToString())
            );
        }

        /// <summary>
        /// Create a new AnimatorController asset.
        /// </summary>
        private object HandleCreateController(Dictionary<string, object> payload)
        {
            string name = GetString(payload, "name", "NewAnimatorController");
            string savePath = GetString(payload, "savePath", null);
            var parametersData = GetListFromPayload(payload, "parameters");
            var statesData = GetListFromPayload(payload, "states");
            var transitionsData = GetListFromPayload(payload, "transitions");

            if (string.IsNullOrEmpty(savePath))
            {
                savePath = $"Assets/Animations/{name}.controller";
            }

            // Ensure directory exists
            string directory = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create controller
            var controller = AnimatorController.CreateAnimatorControllerAtPath(savePath);

            // Add parameters
            if (parametersData != null)
            {
                foreach (var paramData in parametersData)
                {
                    if (paramData is Dictionary<string, object> param)
                    {
                        string paramName = param.ContainsKey("name") ? param["name"]?.ToString() : "";
                        string paramType = param.ContainsKey("type") ? param["type"]?.ToString() : "float";

                        if (!string.IsNullOrEmpty(paramName))
                        {
                            AddParameterToController(controller, paramName, paramType, param);
                        }
                    }
                }
            }

            // Get base layer
            var rootStateMachine = controller.layers[0].stateMachine;

            // Track created states for transitions
            var stateMap = new Dictionary<string, AnimatorState>();

            // Add states
            if (statesData != null)
            {
                foreach (var stateData in statesData)
                {
                    if (stateData is Dictionary<string, object> state)
                    {
                        string stateName = state.ContainsKey("name") ? state["name"]?.ToString() : "";
                        string clipPath = state.ContainsKey("clip") ? state["clip"]?.ToString() : "";
                        bool isDefault = state.ContainsKey("isDefault") && Convert.ToBoolean(state["isDefault"]);

                        if (!string.IsNullOrEmpty(stateName))
                        {
                            var animState = rootStateMachine.AddState(stateName);
                            stateMap[stateName] = animState;

                            // Load and assign animation clip
                            if (!string.IsNullOrEmpty(clipPath))
                            {
                                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                                if (clip != null)
                                {
                                    animState.motion = clip;
                                }
                            }

                            // Set as default state
                            if (isDefault)
                            {
                                rootStateMachine.defaultState = animState;
                            }

                            // Set speed
                            if (state.ContainsKey("speed"))
                            {
                                animState.speed = Convert.ToSingle(state["speed"]);
                            }
                        }
                    }
                }
            }

            // Add transitions
            if (transitionsData != null)
            {
                foreach (var transData in transitionsData)
                {
                    if (transData is Dictionary<string, object> trans)
                    {
                        string fromState = trans.ContainsKey("from") ? trans["from"]?.ToString() : "";
                        string toState = trans.ContainsKey("to") ? trans["to"]?.ToString() : "";

                        if (!string.IsNullOrEmpty(toState) && stateMap.ContainsKey(toState))
                        {
                            AnimatorStateTransition transition;

                            if (fromState == "Any" || fromState == "AnyState")
                            {
                                transition = rootStateMachine.AddAnyStateTransition(stateMap[toState]);
                            }
                            else if (!string.IsNullOrEmpty(fromState) && stateMap.ContainsKey(fromState))
                            {
                                transition = stateMap[fromState].AddTransition(stateMap[toState]);
                            }
                            else
                            {
                                continue;
                            }

                            // Set transition properties
                            if (trans.ContainsKey("hasExitTime"))
                            {
                                transition.hasExitTime = Convert.ToBoolean(trans["hasExitTime"]);
                            }
                            if (trans.ContainsKey("exitTime"))
                            {
                                transition.exitTime = Convert.ToSingle(trans["exitTime"]);
                            }
                            if (trans.ContainsKey("duration"))
                            {
                                transition.duration = Convert.ToSingle(trans["duration"]);
                            }

                            // Add conditions
                            if (trans.ContainsKey("conditions") && trans["conditions"] is List<object> conditions)
                            {
                                foreach (var condData in conditions)
                                {
                                    if (condData is Dictionary<string, object> cond)
                                    {
                                        AddTransitionCondition(transition, cond);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return CreateSuccessResponse(
                ("name", name),
                ("savePath", savePath),
                ("parameterCount", controller.parameters.Length),
                ("stateCount", stateMap.Count)
            );
        }

        /// <summary>
        /// Add a state to an existing AnimatorController.
        /// </summary>
        private object HandleAddState(Dictionary<string, object> payload)
        {
            string controllerPath = GetString(payload, "controllerPath", null);
            string stateName = GetString(payload, "stateName", null);
            string clipPath = GetString(payload, "clipPath", null);
            int layerIndex = GetInt(payload, "layerIndex", 0);
            bool isDefault = GetBool(payload, "isDefault", false);
            float speed = GetFloat(payload, "speed", 1f);
            var positionData = GetDictFromPayload(payload, "position");

            if (string.IsNullOrEmpty(controllerPath))
            {
                throw new ArgumentException("controllerPath is required");
            }

            if (string.IsNullOrEmpty(stateName))
            {
                throw new ArgumentException("stateName is required");
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                throw new ArgumentException($"AnimatorController not found: {controllerPath}");
            }

            if (layerIndex >= controller.layers.Length)
            {
                throw new ArgumentException($"Layer index {layerIndex} is out of range");
            }

            var stateMachine = controller.layers[layerIndex].stateMachine;

            // Create position
            Vector3 position = Vector3.zero;
            if (positionData != null)
            {
                position = new Vector3(
                    positionData.ContainsKey("x") ? Convert.ToSingle(positionData["x"]) : 0,
                    positionData.ContainsKey("y") ? Convert.ToSingle(positionData["y"]) : 0,
                    0
                );
            }

            var state = stateMachine.AddState(stateName, position);
            state.speed = speed;

            // Load and assign clip
            if (!string.IsNullOrEmpty(clipPath))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip != null)
                {
                    state.motion = clip;
                }
            }

            if (isDefault)
            {
                stateMachine.defaultState = state;
            }

            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("controllerPath", controllerPath),
                ("stateName", stateName),
                ("layerIndex", layerIndex),
                ("hasClip", state.motion != null),
                ("isDefault", isDefault)
            );
        }

        /// <summary>
        /// Add a transition between states.
        /// </summary>
        private object HandleAddTransition(Dictionary<string, object> payload)
        {
            string controllerPath = GetString(payload, "controllerPath", null);
            string fromState = GetString(payload, "fromState", null);
            string toState = GetString(payload, "toState", null);
            int layerIndex = GetInt(payload, "layerIndex", 0);
            bool hasExitTime = GetBool(payload, "hasExitTime", true);
            float exitTime = GetFloat(payload, "exitTime", 0.9f);
            float duration = GetFloat(payload, "duration", 0.25f);
            var conditionsData = GetListFromPayload(payload, "conditions");

            if (string.IsNullOrEmpty(controllerPath))
            {
                throw new ArgumentException("controllerPath is required");
            }

            if (string.IsNullOrEmpty(toState))
            {
                throw new ArgumentException("toState is required");
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                throw new ArgumentException($"AnimatorController not found: {controllerPath}");
            }

            var stateMachine = controller.layers[layerIndex].stateMachine;

            // Find target state
            AnimatorState toStateObj = FindState(stateMachine, toState);
            if (toStateObj == null)
            {
                throw new ArgumentException($"Target state not found: {toState}");
            }

            AnimatorStateTransition transition;

            if (string.IsNullOrEmpty(fromState) || fromState == "Any" || fromState == "AnyState")
            {
                transition = stateMachine.AddAnyStateTransition(toStateObj);
            }
            else
            {
                AnimatorState fromStateObj = FindState(stateMachine, fromState);
                if (fromStateObj == null)
                {
                    throw new ArgumentException($"Source state not found: {fromState}");
                }
                transition = fromStateObj.AddTransition(toStateObj);
            }

            transition.hasExitTime = hasExitTime;
            transition.exitTime = exitTime;
            transition.duration = duration;

            // Add conditions
            if (conditionsData != null)
            {
                foreach (var condData in conditionsData)
                {
                    if (condData is Dictionary<string, object> cond)
                    {
                        AddTransitionCondition(transition, cond);
                    }
                }
            }

            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("controllerPath", controllerPath),
                ("fromState", fromState ?? "AnyState"),
                ("toState", toState),
                ("hasExitTime", hasExitTime),
                ("conditionCount", transition.conditions.Length)
            );
        }

        /// <summary>
        /// Set or add a parameter to an AnimatorController.
        /// </summary>
        private object HandleSetParameter(Dictionary<string, object> payload)
        {
            string controllerPath = GetString(payload, "controllerPath", null);
            string paramName = GetString(payload, "parameterName", null);
            string paramType = GetString(payload, "parameterType", "float");
            object defaultValue = payload.ContainsKey("defaultValue") ? payload["defaultValue"] : null;

            if (string.IsNullOrEmpty(controllerPath))
            {
                throw new ArgumentException("controllerPath is required");
            }

            if (string.IsNullOrEmpty(paramName))
            {
                throw new ArgumentException("parameterName is required");
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                throw new ArgumentException($"AnimatorController not found: {controllerPath}");
            }

            // Check if parameter already exists
            var existingParam = controller.parameters.FirstOrDefault(p => p.name == paramName);
            if (existingParam != null)
            {
                // Update existing parameter's default value
                SetParameterDefaultValue(controller, existingParam, defaultValue);
            }
            else
            {
                // Add new parameter
                var paramDict = new Dictionary<string, object>
                {
                    ["defaultValue"] = defaultValue
                };
                AddParameterToController(controller, paramName, paramType, paramDict);
            }

            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("controllerPath", controllerPath),
                ("parameterName", paramName),
                ("parameterType", paramType),
                ("action", existingParam != null ? "updated" : "added")
            );
        }

        /// <summary>
        /// Add a BlendTree to the AnimatorController.
        /// </summary>
        private object HandleAddBlendTree(Dictionary<string, object> payload)
        {
            string controllerPath = GetString(payload, "controllerPath", null);
            string blendTreeName = GetString(payload, "blendTreeName", "New BlendTree");
            int layerIndex = GetInt(payload, "layerIndex", 0);
            string blendType = GetString(payload, "blendType", "Simple1D");
            string blendParameter = GetString(payload, "blendParameter", "Speed");
            string blendParameterY = GetString(payload, "blendParameterY", null);
            var motionsData = GetListFromPayload(payload, "motions");

            if (string.IsNullOrEmpty(controllerPath))
            {
                throw new ArgumentException("controllerPath is required");
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                throw new ArgumentException($"AnimatorController not found: {controllerPath}");
            }

            var stateMachine = controller.layers[layerIndex].stateMachine;

            // Create BlendTree
            var blendTree = new BlendTree
            {
                name = blendTreeName
            };

            // Set blend type
            if (Enum.TryParse<BlendTreeType>(blendType, true, out var blendTypeEnum))
            {
                blendTree.blendType = blendTypeEnum;
            }

            blendTree.blendParameter = blendParameter;
            if (!string.IsNullOrEmpty(blendParameterY))
            {
                blendTree.blendParameterY = blendParameterY;
            }

            // Add motions
            if (motionsData != null)
            {
                foreach (var motionData in motionsData)
                {
                    if (motionData is Dictionary<string, object> motion)
                    {
                        string clipPath = motion.ContainsKey("clip") ? motion["clip"]?.ToString() : "";
                        float threshold = motion.ContainsKey("threshold") ? Convert.ToSingle(motion["threshold"]) : 0;

                        if (!string.IsNullOrEmpty(clipPath))
                        {
                            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                            if (clip != null)
                            {
                                if (blendTree.blendType == BlendTreeType.Simple1D)
                                {
                                    blendTree.AddChild(clip, threshold);
                                }
                                else
                                {
                                    float posX = motion.ContainsKey("positionX") ? Convert.ToSingle(motion["positionX"]) : 0;
                                    float posY = motion.ContainsKey("positionY") ? Convert.ToSingle(motion["positionY"]) : 0;
                                    blendTree.AddChild(clip, new Vector2(posX, posY));
                                }
                            }
                        }
                    }
                }
            }

            // Create state with blend tree
            var state = stateMachine.AddState(blendTreeName);
            state.motion = blendTree;

            // Ensure blend parameter exists
            if (!controller.parameters.Any(p => p.name == blendParameter))
            {
                controller.AddParameter(blendParameter, AnimatorControllerParameterType.Float);
            }

            if (!string.IsNullOrEmpty(blendParameterY) && !controller.parameters.Any(p => p.name == blendParameterY))
            {
                controller.AddParameter(blendParameterY, AnimatorControllerParameterType.Float);
            }

            AssetDatabase.AddObjectToAsset(blendTree, controller);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("controllerPath", controllerPath),
                ("blendTreeName", blendTreeName),
                ("blendType", blendType),
                ("blendParameter", blendParameter),
                ("motionCount", blendTree.children.Length)
            );
        }

        /// <summary>
        /// Create an AvatarMask asset.
        /// </summary>
        private object HandleCreateAvatarMask(Dictionary<string, object> payload)
        {
            string name = GetString(payload, "name", "NewAvatarMask");
            string savePath = GetString(payload, "savePath", null);
            var enabledPartsData = GetListFromPayload(payload, "enabledParts");
            var disabledPartsData = GetListFromPayload(payload, "disabledParts");

            if (string.IsNullOrEmpty(savePath))
            {
                savePath = $"Assets/Animations/{name}.mask";
            }

            // Ensure directory exists
            string directory = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var mask = new AvatarMask();

            // All humanoid body parts
            var allParts = new[]
            {
                AvatarMaskBodyPart.Root,
                AvatarMaskBodyPart.Body,
                AvatarMaskBodyPart.Head,
                AvatarMaskBodyPart.LeftLeg,
                AvatarMaskBodyPart.RightLeg,
                AvatarMaskBodyPart.LeftArm,
                AvatarMaskBodyPart.RightArm,
                AvatarMaskBodyPart.LeftFingers,
                AvatarMaskBodyPart.RightFingers,
                AvatarMaskBodyPart.LeftFootIK,
                AvatarMaskBodyPart.RightFootIK,
                AvatarMaskBodyPart.LeftHandIK,
                AvatarMaskBodyPart.RightHandIK
            };

            // Enable all by default
            foreach (var part in allParts)
            {
                mask.SetHumanoidBodyPartActive(part, true);
            }

            // Disable specified parts
            if (disabledPartsData != null)
            {
                foreach (var partName in disabledPartsData)
                {
                    if (partName != null && Enum.TryParse<AvatarMaskBodyPart>(partName.ToString(), true, out var part))
                    {
                        mask.SetHumanoidBodyPartActive(part, false);
                    }
                }
            }

            // Enable only specified parts (overrides previous)
            if (enabledPartsData != null)
            {
                // First disable all
                foreach (var part in allParts)
                {
                    mask.SetHumanoidBodyPartActive(part, false);
                }
                // Then enable specified
                foreach (var partName in enabledPartsData)
                {
                    if (partName != null && Enum.TryParse<AvatarMaskBodyPart>(partName.ToString(), true, out var part))
                    {
                        mask.SetHumanoidBodyPartActive(part, true);
                    }
                }
            }

            AssetDatabase.CreateAsset(mask, savePath);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("name", name),
                ("savePath", savePath),
                ("message", "AvatarMask created successfully")
            );
        }

        /// <summary>
        /// Inspect an AnimatorController.
        /// </summary>
        private object HandleInspect(Dictionary<string, object> payload)
        {
            string controllerPath = GetString(payload, "controllerPath", null);

            if (string.IsNullOrEmpty(controllerPath))
            {
                throw new ArgumentException("controllerPath is required");
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                throw new ArgumentException($"AnimatorController not found: {controllerPath}");
            }

            // Get parameters
            var parameters = controller.parameters.Select(p => new Dictionary<string, object>
            {
                ["name"] = p.name,
                ["type"] = p.type.ToString(),
                ["defaultFloat"] = p.defaultFloat,
                ["defaultInt"] = p.defaultInt,
                ["defaultBool"] = p.defaultBool
            }).ToList();

            // Get layers
            var layers = new List<Dictionary<string, object>>();
            for (int i = 0; i < controller.layers.Length; i++)
            {
                var layer = controller.layers[i];
                var states = new List<string>();
                CollectStateNames(layer.stateMachine, states);

                layers.Add(new Dictionary<string, object>
                {
                    ["index"] = i,
                    ["name"] = layer.name,
                    ["defaultWeight"] = layer.defaultWeight,
                    ["stateCount"] = states.Count,
                    ["states"] = states
                });
            }

            return CreateSuccessResponse(
                ("controllerPath", controllerPath),
                ("name", controller.name),
                ("parameterCount", controller.parameters.Length),
                ("parameters", parameters),
                ("layerCount", controller.layers.Length),
                ("layers", layers)
            );
        }

        /// <summary>
        /// Delete an AnimatorController or state.
        /// </summary>
        private object HandleDelete(Dictionary<string, object> payload)
        {
            string controllerPath = GetString(payload, "controllerPath", null);
            string stateName = GetString(payload, "stateName", null);
            int layerIndex = GetInt(payload, "layerIndex", 0);

            if (string.IsNullOrEmpty(controllerPath))
            {
                throw new ArgumentException("controllerPath is required");
            }

            if (string.IsNullOrEmpty(stateName))
            {
                // Delete entire controller
                if (AssetDatabase.DeleteAsset(controllerPath))
                {
                    return CreateSuccessResponse(
                        ("deleted", controllerPath),
                        ("type", "controller")
                    );
                }
                throw new Exception($"Failed to delete controller: {controllerPath}");
            }

            // Delete state
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                throw new ArgumentException($"AnimatorController not found: {controllerPath}");
            }

            var stateMachine = controller.layers[layerIndex].stateMachine;
            var state = FindState(stateMachine, stateName);
            if (state == null)
            {
                throw new ArgumentException($"State not found: {stateName}");
            }

            stateMachine.RemoveState(state);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("controllerPath", controllerPath),
                ("deletedState", stateName),
                ("type", "state")
            );
        }

        /// <summary>
        /// List all parameters in an AnimatorController.
        /// </summary>
        private object HandleListParameters(Dictionary<string, object> payload)
        {
            string controllerPath = GetString(payload, "controllerPath", null);

            if (string.IsNullOrEmpty(controllerPath))
            {
                throw new ArgumentException("controllerPath is required");
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                throw new ArgumentException($"AnimatorController not found: {controllerPath}");
            }

            var parameters = controller.parameters.Select(p => new Dictionary<string, object>
            {
                ["name"] = p.name,
                ["type"] = p.type.ToString(),
                ["defaultFloat"] = p.defaultFloat,
                ["defaultInt"] = p.defaultInt,
                ["defaultBool"] = p.defaultBool
            }).ToList();

            return CreateSuccessResponse(
                ("controllerPath", controllerPath),
                ("parameters", parameters),
                ("count", parameters.Count)
            );
        }

        /// <summary>
        /// List all states in an AnimatorController layer.
        /// </summary>
        private object HandleListStates(Dictionary<string, object> payload)
        {
            string controllerPath = GetString(payload, "controllerPath", null);
            int layerIndex = GetInt(payload, "layerIndex", 0);

            if (string.IsNullOrEmpty(controllerPath))
            {
                throw new ArgumentException("controllerPath is required");
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                throw new ArgumentException($"AnimatorController not found: {controllerPath}");
            }

            if (layerIndex >= controller.layers.Length)
            {
                throw new ArgumentException($"Layer index {layerIndex} is out of range");
            }

            var stateMachine = controller.layers[layerIndex].stateMachine;
            var stateInfos = new List<Dictionary<string, object>>();

            foreach (var childState in stateMachine.states)
            {
                var state = childState.state;
                stateInfos.Add(new Dictionary<string, object>
                {
                    ["name"] = state.name,
                    ["hasMotion"] = state.motion != null,
                    ["motionName"] = state.motion?.name ?? "",
                    ["speed"] = state.speed,
                    ["isDefault"] = stateMachine.defaultState == state,
                    ["transitionCount"] = state.transitions.Length
                });
            }

            return CreateSuccessResponse(
                ("controllerPath", controllerPath),
                ("layerIndex", layerIndex),
                ("layerName", controller.layers[layerIndex].name),
                ("states", stateInfos),
                ("count", stateInfos.Count)
            );
        }

        #region Helper Methods

        // Note: GetDictFromPayload and GetListFromPayload are inherited from BaseCommandHandler

        private void AddParameterToController(AnimatorController controller, string name, string type, Dictionary<string, object> options)
        {
            AnimatorControllerParameterType paramType = type.ToLower() switch
            {
                "float" => AnimatorControllerParameterType.Float,
                "int" => AnimatorControllerParameterType.Int,
                "bool" => AnimatorControllerParameterType.Bool,
                "trigger" => AnimatorControllerParameterType.Trigger,
                _ => AnimatorControllerParameterType.Float
            };

            controller.AddParameter(name, paramType);

            // Set default value if provided
            if (options != null && options.ContainsKey("defaultValue"))
            {
                var param = controller.parameters.FirstOrDefault(p => p.name == name);
                if (param != null)
                {
                    SetParameterDefaultValue(controller, param, options["defaultValue"]);
                }
            }
        }

        private void SetParameterDefaultValue(AnimatorController controller, AnimatorControllerParameter param, object value)
        {
            if (value == null) return;

            var parameters = controller.parameters.ToList();
            int index = parameters.IndexOf(param);
            if (index < 0) return;

            switch (param.type)
            {
                case AnimatorControllerParameterType.Float:
                    param.defaultFloat = Convert.ToSingle(value);
                    break;
                case AnimatorControllerParameterType.Int:
                    param.defaultInt = Convert.ToInt32(value);
                    break;
                case AnimatorControllerParameterType.Bool:
                    param.defaultBool = Convert.ToBoolean(value);
                    break;
            }

            parameters[index] = param;
            controller.parameters = parameters.ToArray();
        }

        private AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
        {
            foreach (var childState in stateMachine.states)
            {
                if (childState.state.name == stateName)
                {
                    return childState.state;
                }
            }

            // Check sub-state machines
            foreach (var childSM in stateMachine.stateMachines)
            {
                var found = FindState(childSM.stateMachine, stateName);
                if (found != null) return found;
            }

            return null;
        }

        private void CollectStateNames(AnimatorStateMachine stateMachine, List<string> states)
        {
            foreach (var childState in stateMachine.states)
            {
                states.Add(childState.state.name);
            }

            foreach (var childSM in stateMachine.stateMachines)
            {
                CollectStateNames(childSM.stateMachine, states);
            }
        }

        private void AddTransitionCondition(AnimatorStateTransition transition, Dictionary<string, object> cond)
        {
            string paramName = cond.ContainsKey("param") ? cond["param"]?.ToString() : "";
            string mode = cond.ContainsKey("mode") ? cond["mode"]?.ToString() : "greater";
            float threshold = cond.ContainsKey("value") ? Convert.ToSingle(cond["value"]) : 0;

            if (string.IsNullOrEmpty(paramName)) return;

            AnimatorConditionMode condMode = mode.ToLower() switch
            {
                "if" or "true" => AnimatorConditionMode.If,
                "ifnot" or "false" => AnimatorConditionMode.IfNot,
                "greater" => AnimatorConditionMode.Greater,
                "less" => AnimatorConditionMode.Less,
                "equals" => AnimatorConditionMode.Equals,
                "notequal" or "notequals" => AnimatorConditionMode.NotEqual,
                _ => AnimatorConditionMode.Greater
            };

            transition.AddCondition(condMode, threshold, paramName);
        }

        #endregion
    }
}
