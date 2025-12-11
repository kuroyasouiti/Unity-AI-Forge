using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Mid-level 2D animation bundle: Animator setup, AnimatorController creation,
    /// animation clip management, and state machine configuration.
    /// </summary>
    public class Animation2DBundleHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            // Animator management
            "setupAnimator",
            "updateAnimator",
            "inspectAnimator",
            // AnimatorController operations
            "createController",
            "addState",
            "addTransition",
            "addParameter",
            "inspectController",
            // Animation clip operations
            "createClipFromSprites",
            "updateClip",
            "inspectClip",
        };

        public override string Category => "animation2DBundle";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "setupAnimator" => SetupAnimator(payload),
                "updateAnimator" => UpdateAnimator(payload),
                "inspectAnimator" => InspectAnimator(payload),
                "createController" => CreateController(payload),
                "addState" => AddState(payload),
                "addTransition" => AddTransition(payload),
                "addParameter" => AddParameter(payload),
                "inspectController" => InspectController(payload),
                "createClipFromSprites" => CreateClipFromSprites(payload),
                "updateClip" => UpdateClip(payload),
                "inspectClip" => InspectClip(payload),
                _ => throw new InvalidOperationException($"Unsupported animation2D bundle operation: {operation}"),
            };
        }

        #region Animator Management

        private object SetupAnimator(Dictionary<string, object> payload)
        {
            var go = ResolveGameObjectFromPayload(payload);
            var controllerPath = GetString(payload, "controllerPath");
            var applyRootMotion = GetBool(payload, "applyRootMotion", false);
            var updateMode = GetString(payload, "updateMode") ?? "Normal";
            var cullingMode = GetString(payload, "cullingMode") ?? "AlwaysAnimate";

            Undo.RecordObject(go, "Setup Animator");

            // Get or add Animator component
            var animator = go.GetComponent<Animator>();
            if (animator == null)
            {
                animator = Undo.AddComponent<Animator>(go);
            }

            // Load and assign controller
            if (!string.IsNullOrEmpty(controllerPath))
            {
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
                if (controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                }
                else
                {
                    Debug.LogWarning($"[Animation2DBundle] Controller not found at: {controllerPath}");
                }
            }

            // Configure animator
            animator.applyRootMotion = applyRootMotion;
            animator.updateMode = ParseAnimatorUpdateMode(updateMode);
            animator.cullingMode = ParseAnimatorCullingMode(cullingMode);

            EditorSceneManager.MarkSceneDirty(go.scene);

            return CreateSuccessResponse(
                ("gameObjectPath", BuildGameObjectPath(go)),
                ("animatorID", animator.GetInstanceID()),
                ("controllerPath", controllerPath)
            );
        }

        private object UpdateAnimator(Dictionary<string, object> payload)
        {
            var go = ResolveGameObjectFromPayload(payload);
            var animator = go.GetComponent<Animator>();

            if (animator == null)
            {
                throw new InvalidOperationException($"GameObject '{BuildGameObjectPath(go)}' does not have an Animator");
            }

            Undo.RecordObject(animator, "Update Animator");

            if (payload.ContainsKey("controllerPath"))
            {
                var controllerPath = GetString(payload, "controllerPath");
                if (!string.IsNullOrEmpty(controllerPath))
                {
                    var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
                    if (controller != null)
                    {
                        animator.runtimeAnimatorController = controller;
                    }
                }
                else
                {
                    animator.runtimeAnimatorController = null;
                }
            }

            if (payload.ContainsKey("applyRootMotion"))
            {
                animator.applyRootMotion = GetBool(payload, "applyRootMotion", false);
            }

            if (payload.ContainsKey("updateMode"))
            {
                animator.updateMode = ParseAnimatorUpdateMode(GetString(payload, "updateMode"));
            }

            if (payload.ContainsKey("cullingMode"))
            {
                animator.cullingMode = ParseAnimatorCullingMode(GetString(payload, "cullingMode"));
            }

            if (payload.ContainsKey("speed"))
            {
                animator.speed = GetFloat(payload, "speed", 1f);
            }

            EditorSceneManager.MarkSceneDirty(go.scene);

            return CreateSuccessResponse(
                ("gameObjectPath", BuildGameObjectPath(go)),
                ("updated", true)
            );
        }

        private object InspectAnimator(Dictionary<string, object> payload)
        {
            var go = ResolveGameObjectFromPayload(payload);
            var animator = go.GetComponent<Animator>();

            if (animator == null)
            {
                throw new InvalidOperationException($"GameObject '{BuildGameObjectPath(go)}' does not have an Animator");
            }

            var info = new Dictionary<string, object>
            {
                ["success"] = true,
                ["gameObjectPath"] = BuildGameObjectPath(go),
                ["hasController"] = animator.runtimeAnimatorController != null,
                ["controllerPath"] = animator.runtimeAnimatorController != null
                    ? AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)
                    : null,
                ["applyRootMotion"] = animator.applyRootMotion,
                ["updateMode"] = animator.updateMode.ToString(),
                ["cullingMode"] = animator.cullingMode.ToString(),
                ["speed"] = animator.speed,
                ["layerCount"] = animator.layerCount,
                ["parameterCount"] = animator.parameterCount
            };

            // Get parameters
            if (animator.parameterCount > 0)
            {
                var parameters = new List<Dictionary<string, object>>();
                foreach (var param in animator.parameters)
                {
                    parameters.Add(new Dictionary<string, object>
                    {
                        ["name"] = param.name,
                        ["type"] = param.type.ToString()
                    });
                }
                info["parameters"] = parameters;
            }

            return info;
        }

        #endregion

        #region AnimatorController Operations

        private object CreateController(Dictionary<string, object> payload)
        {
            var controllerPath = GetString(payload, "controllerPath");

            if (string.IsNullOrEmpty(controllerPath))
            {
                throw new InvalidOperationException("controllerPath parameter is required");
            }

            if (!controllerPath.EndsWith(".controller"))
            {
                controllerPath += ".controller";
            }

            // Ensure directory exists
            var directory = System.IO.Path.GetDirectoryName(controllerPath);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // Create controller
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            // Add default parameters if specified
            if (payload.ContainsKey("parameters"))
            {
                var parameters = payload["parameters"] as List<object>;
                if (parameters != null)
                {
                    foreach (var paramObj in parameters)
                    {
                        if (paramObj is Dictionary<string, object> paramDict)
                        {
                            var paramName = paramDict.TryGetValue("name", out var n) ? n?.ToString() : null;
                            var paramType = paramDict.TryGetValue("type", out var t) ? t?.ToString() : "Bool";

                            if (!string.IsNullOrEmpty(paramName))
                            {
                                controller.AddParameter(paramName, ParseAnimatorParameterType(paramType));
                            }
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("controllerPath", controllerPath),
                ("layerCount", controller.layers.Length),
                ("parameterCount", controller.parameters.Length)
            );
        }

        private object AddState(Dictionary<string, object> payload)
        {
            var controllerPath = GetString(payload, "controllerPath");
            var stateName = GetString(payload, "stateName");
            var clipPath = GetString(payload, "clipPath");
            var layerIndex = GetInt(payload, "layerIndex", 0);
            var isDefault = GetBool(payload, "isDefault", false);
            var speed = GetFloat(payload, "speed", 1f);

            if (string.IsNullOrEmpty(controllerPath))
            {
                throw new InvalidOperationException("controllerPath parameter is required");
            }

            if (string.IsNullOrEmpty(stateName))
            {
                throw new InvalidOperationException("stateName parameter is required");
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"AnimatorController not found at: {controllerPath}");
            }

            // Get layer
            if (layerIndex >= controller.layers.Length)
            {
                throw new InvalidOperationException($"Layer index {layerIndex} out of range");
            }

            var layer = controller.layers[layerIndex];
            var stateMachine = layer.stateMachine;

            // Add state
            var state = stateMachine.AddState(stateName);
            state.speed = speed;

            // Load and assign animation clip
            if (!string.IsNullOrEmpty(clipPath))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip != null)
                {
                    state.motion = clip;
                }
                else
                {
                    Debug.LogWarning($"[Animation2DBundle] Animation clip not found at: {clipPath}");
                }
            }

            // Set as default if requested
            if (isDefault)
            {
                stateMachine.defaultState = state;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("controllerPath", controllerPath),
                ("stateName", stateName),
                ("layerIndex", layerIndex),
                ("isDefault", isDefault)
            );
        }

        private object AddTransition(Dictionary<string, object> payload)
        {
            var controllerPath = GetString(payload, "controllerPath");
            var fromState = GetString(payload, "fromState");
            var toState = GetString(payload, "toState");
            var layerIndex = GetInt(payload, "layerIndex", 0);
            var hasExitTime = GetBool(payload, "hasExitTime", true);
            var exitTime = GetFloat(payload, "exitTime", 1f);
            var duration = GetFloat(payload, "duration", 0f);

            if (string.IsNullOrEmpty(controllerPath))
            {
                throw new InvalidOperationException("controllerPath parameter is required");
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"AnimatorController not found at: {controllerPath}");
            }

            var layer = controller.layers[layerIndex];
            var stateMachine = layer.stateMachine;

            // Find states
            AnimatorState sourceState = null;
            AnimatorState destState = null;

            foreach (var childState in stateMachine.states)
            {
                if (childState.state.name == fromState)
                {
                    sourceState = childState.state;
                }
                if (childState.state.name == toState)
                {
                    destState = childState.state;
                }
            }

            // Handle "Any State" or "Entry"
            AnimatorStateTransition transition;

            if (string.IsNullOrEmpty(fromState) || fromState.ToLowerInvariant() == "any")
            {
                // Add from Any State
                transition = stateMachine.AddAnyStateTransition(destState);
            }
            else if (sourceState == null)
            {
                throw new InvalidOperationException($"Source state '{fromState}' not found");
            }
            else if (destState == null)
            {
                throw new InvalidOperationException($"Destination state '{toState}' not found");
            }
            else
            {
                transition = sourceState.AddTransition(destState);
            }

            // Configure transition
            transition.hasExitTime = hasExitTime;
            transition.exitTime = exitTime;
            transition.duration = duration;
            transition.hasFixedDuration = true;

            // Add conditions
            if (payload.ContainsKey("conditions"))
            {
                var conditions = payload["conditions"] as List<object>;
                if (conditions != null)
                {
                    foreach (var condObj in conditions)
                    {
                        if (condObj is Dictionary<string, object> condDict)
                        {
                            var paramName = condDict.TryGetValue("parameter", out var p) ? p?.ToString() : null;
                            var mode = condDict.TryGetValue("mode", out var m) ? m?.ToString() : "If";
                            var threshold = condDict.TryGetValue("threshold", out var th) ? Convert.ToSingle(th) : 0f;

                            if (!string.IsNullOrEmpty(paramName))
                            {
                                transition.AddCondition(ParseAnimatorConditionMode(mode), threshold, paramName);
                            }
                        }
                    }
                }
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("controllerPath", controllerPath),
                ("fromState", fromState),
                ("toState", toState),
                ("hasExitTime", hasExitTime)
            );
        }

        private object AddParameter(Dictionary<string, object> payload)
        {
            var controllerPath = GetString(payload, "controllerPath");
            var paramName = GetString(payload, "parameterName");
            var paramType = GetString(payload, "parameterType") ?? "Bool";

            if (string.IsNullOrEmpty(controllerPath))
            {
                throw new InvalidOperationException("controllerPath parameter is required");
            }

            if (string.IsNullOrEmpty(paramName))
            {
                throw new InvalidOperationException("parameterName parameter is required");
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"AnimatorController not found at: {controllerPath}");
            }

            controller.AddParameter(paramName, ParseAnimatorParameterType(paramType));

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("controllerPath", controllerPath),
                ("parameterName", paramName),
                ("parameterType", paramType)
            );
        }

        private object InspectController(Dictionary<string, object> payload)
        {
            var controllerPath = GetString(payload, "controllerPath");

            if (string.IsNullOrEmpty(controllerPath))
            {
                throw new InvalidOperationException("controllerPath parameter is required");
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"AnimatorController not found at: {controllerPath}");
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

            // Get layers info
            var layers = controller.layers.Select((layer, index) => new Dictionary<string, object>
            {
                ["index"] = index,
                ["name"] = layer.name,
                ["weight"] = layer.defaultWeight,
                ["stateCount"] = layer.stateMachine.states.Length,
                ["states"] = layer.stateMachine.states.Select(s => new Dictionary<string, object>
                {
                    ["name"] = s.state.name,
                    ["speed"] = s.state.speed,
                    ["hasMotion"] = s.state.motion != null,
                    ["motionName"] = s.state.motion?.name
                }).ToList(),
                ["defaultState"] = layer.stateMachine.defaultState?.name
            }).ToList();

            return CreateSuccessResponse(
                ("controllerPath", controllerPath),
                ("parameters", parameters),
                ("layers", layers)
            );
        }

        #endregion

        #region Animation Clip Operations

        private object CreateClipFromSprites(Dictionary<string, object> payload)
        {
            var clipPath = GetString(payload, "clipPath");
            var spritePaths = GetStringList(payload, "spritePaths");
            var frameRate = GetFloat(payload, "frameRate", 12f);
            var loop = GetBool(payload, "loop", true);

            if (string.IsNullOrEmpty(clipPath))
            {
                throw new InvalidOperationException("clipPath parameter is required");
            }

            if (spritePaths.Count == 0)
            {
                throw new InvalidOperationException("spritePaths parameter is required");
            }

            if (!clipPath.EndsWith(".anim"))
            {
                clipPath += ".anim";
            }

            // Ensure directory exists
            var directory = System.IO.Path.GetDirectoryName(clipPath);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // Create clip
            var clip = new AnimationClip();
            clip.frameRate = frameRate;

            // Load sprites
            var sprites = new List<Sprite>();
            foreach (var path in spritePaths)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    sprites.Add(sprite);
                }
                else
                {
                    // Try loading from sprite sheet
                    var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                    var spriteAssets = allAssets.OfType<Sprite>().ToList();
                    sprites.AddRange(spriteAssets);
                }
            }

            if (sprites.Count == 0)
            {
                throw new InvalidOperationException("No sprites could be loaded from the provided paths");
            }

            // Create keyframes
            var keyframes = new ObjectReferenceKeyframe[sprites.Count];
            var frameDuration = 1f / frameRate;

            for (var i = 0; i < sprites.Count; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i * frameDuration,
                    value = sprites[i]
                };
            }

            // Create sprite binding
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            // Set loop settings
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            // Save clip
            AssetDatabase.CreateAsset(clip, clipPath);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("clipPath", clipPath),
                ("frameCount", sprites.Count),
                ("frameRate", frameRate),
                ("duration", sprites.Count * frameDuration),
                ("loop", loop)
            );
        }

        private object UpdateClip(Dictionary<string, object> payload)
        {
            var clipPath = GetString(payload, "clipPath");

            if (string.IsNullOrEmpty(clipPath))
            {
                throw new InvalidOperationException("clipPath parameter is required");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Animation clip not found at: {clipPath}");
            }

            if (payload.ContainsKey("frameRate"))
            {
                clip.frameRate = GetFloat(payload, "frameRate", 12f);
            }

            if (payload.ContainsKey("loop"))
            {
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = GetBool(payload, "loop", true);
                AnimationUtility.SetAnimationClipSettings(clip, settings);
            }

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("clipPath", clipPath),
                ("updated", true)
            );
        }

        private object InspectClip(Dictionary<string, object> payload)
        {
            var clipPath = GetString(payload, "clipPath");

            if (string.IsNullOrEmpty(clipPath))
            {
                throw new InvalidOperationException("clipPath parameter is required");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Animation clip not found at: {clipPath}");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);

            var info = new Dictionary<string, object>
            {
                ["success"] = true,
                ["clipPath"] = clipPath,
                ["name"] = clip.name,
                ["length"] = clip.length,
                ["frameRate"] = clip.frameRate,
                ["isLooping"] = settings.loopTime,
                ["isHumanMotion"] = clip.humanMotion,
                ["legacy"] = clip.legacy,
                ["wrapMode"] = clip.wrapMode.ToString()
            };

            // Get curve bindings
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);

            info["curveCount"] = bindings.Length;
            info["objectReferenceCount"] = objectBindings.Length;

            // Get sprite keyframes if this is a sprite animation
            if (objectBindings.Length > 0)
            {
                var spriteBinding = objectBindings.FirstOrDefault(b => b.propertyName == "m_Sprite");
                if (spriteBinding.path != null || spriteBinding.propertyName != null)
                {
                    var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, spriteBinding);
                    info["spriteKeyframeCount"] = keyframes?.Length ?? 0;
                }
            }

            return info;
        }

        #endregion

        #region Helper Methods

        private AnimatorUpdateMode ParseAnimatorUpdateMode(string value)
        {
            if (string.IsNullOrEmpty(value)) return AnimatorUpdateMode.Normal;

            return value.ToLowerInvariant() switch
            {
                "normal" => AnimatorUpdateMode.Normal,
                "animatephysics" => AnimatorUpdateMode.Fixed,
                "unscaledtime" => AnimatorUpdateMode.UnscaledTime,
                _ => AnimatorUpdateMode.Normal
            };
        }

        private AnimatorCullingMode ParseAnimatorCullingMode(string value)
        {
            if (string.IsNullOrEmpty(value)) return AnimatorCullingMode.AlwaysAnimate;

            return value.ToLowerInvariant() switch
            {
                "alwaysanimate" => AnimatorCullingMode.AlwaysAnimate,
                "cullcompletely" => AnimatorCullingMode.CullCompletely,
                "cullupdatetransforms" => AnimatorCullingMode.CullUpdateTransforms,
                _ => AnimatorCullingMode.AlwaysAnimate
            };
        }

        private AnimatorControllerParameterType ParseAnimatorParameterType(string value)
        {
            if (string.IsNullOrEmpty(value)) return AnimatorControllerParameterType.Bool;

            return value.ToLowerInvariant() switch
            {
                "bool" => AnimatorControllerParameterType.Bool,
                "float" => AnimatorControllerParameterType.Float,
                "int" => AnimatorControllerParameterType.Int,
                "trigger" => AnimatorControllerParameterType.Trigger,
                _ => AnimatorControllerParameterType.Bool
            };
        }

        private AnimatorConditionMode ParseAnimatorConditionMode(string value)
        {
            if (string.IsNullOrEmpty(value)) return AnimatorConditionMode.If;

            return value.ToLowerInvariant() switch
            {
                "if" => AnimatorConditionMode.If,
                "ifnot" => AnimatorConditionMode.IfNot,
                "greater" => AnimatorConditionMode.Greater,
                "less" => AnimatorConditionMode.Less,
                "equals" => AnimatorConditionMode.Equals,
                "notequal" => AnimatorConditionMode.NotEqual,
                _ => AnimatorConditionMode.If
            };
        }

        private List<string> GetStringList(Dictionary<string, object> payload, string key)
        {
            if (!payload.TryGetValue(key, out var value)) return new List<string>();

            if (value is List<object> list)
            {
                return list.Select(o => o?.ToString()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            }

            return new List<string>();
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

        #endregion
    }
}
