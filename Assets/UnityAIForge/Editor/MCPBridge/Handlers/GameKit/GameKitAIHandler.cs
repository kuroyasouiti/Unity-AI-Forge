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
    /// GameKit AI handler: create and manage AI behaviors.
    /// Uses code generation to produce standalone AI scripts with zero package dependency.
    /// </summary>
    public class GameKitAIHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "update", "inspect", "delete",
            "setTarget", "clearTarget", "setState",
            "addPatrolPoint", "clearPatrolPoints",
            "findByAIId"
        };

        public override string Category => "gamekitAI";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateAI(payload),
                "update" => UpdateAI(payload),
                "inspect" => InspectAI(payload),
                "delete" => DeleteAI(payload),
                "setTarget" => SetTarget(payload),
                "clearTarget" => ClearTarget(payload),
                "setState" => SetState(payload),
                "addPatrolPoint" => AddPatrolPoint(payload),
                "clearPatrolPoints" => ClearPatrolPoints(payload),
                "findByAIId" => FindByAIId(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit AI operation: {operation}")
            };
        }

        #region Create

        private object CreateAI(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            if (string.IsNullOrEmpty(targetPath))
                throw new InvalidOperationException("targetPath is required for create.");

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");

            var existingAI = CodeGenHelper.FindComponentByField(targetGo, "aiId", null);
            if (existingAI != null)
                throw new InvalidOperationException($"GameObject already has an AI component.");

            var aiId = GetString(payload, "aiId") ?? $"AI_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var behaviorType = ParseBehaviorType(GetString(payload, "behaviorType") ?? "patrol");
            var use2D = GetBool(payload, "use2D", true);
            var moveSpeed = GetFloat(payload, "moveSpeed", 3f);
            var turnSpeed = GetFloat(payload, "turnSpeed", 5f);
            var detectionRadius = GetFloat(payload, "detectionRadius", 8f);
            var fieldOfView = GetFloat(payload, "fieldOfView", 360f);
            var attackRange = GetFloat(payload, "attackRange", 1.5f);
            var attackCooldown = GetFloat(payload, "attackCooldown", 1f);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(aiId, "AI");

            var variables = new Dictionary<string, object>
            {
                { "AI_ID", aiId },
                { "BEHAVIOR_TYPE", behaviorType },
                { "USE_2D", use2D.ToString().ToLowerInvariant() },
                { "MOVE_SPEED", moveSpeed },
                { "TURN_SPEED", turnSpeed },
                { "DETECTION_RADIUS", detectionRadius },
                { "FIELD_OF_VIEW", fieldOfView },
                { "ATTACK_RANGE", attackRange },
                { "ATTACK_COOLDOWN", attackCooldown }
            };

            var outputDir = GetString(payload, "outputPath");

            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "AIBehavior", aiId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate AI script.");
            }

            // Apply additional settings if component was added
            if (result.TryGetValue("componentAdded", out var added) && (bool)added)
            {
                var component = CodeGenHelper.FindComponentByField(targetGo, "aiId", aiId);
                if (component != null)
                    ApplyAISettings(component, payload);
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["aiId"] = aiId;
            result["path"] = BuildGameObjectPath(targetGo);
            result["behaviorType"] = behaviorType;

            return result;
        }

        #endregion

        #region Update

        private object UpdateAI(Dictionary<string, object> payload)
        {
            var component = ResolveAIComponent(payload);

            Undo.RecordObject(component, "Update AI");
            ApplyAISettings(component, payload);
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var so = new SerializedObject(component);
            return CreateSuccessResponse(
                ("aiId", so.FindProperty("aiId").stringValue),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        private void ApplyAISettings(Component component, Dictionary<string, object> payload)
        {
            var so = new SerializedObject(component);

            if (payload.TryGetValue("behaviorType", out var typeObj))
            {
                var typeName = ParseBehaviorType(typeObj.ToString());
                var prop = so.FindProperty("behaviorType");
                if (prop != null)
                {
                    var names = prop.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], typeName, StringComparison.OrdinalIgnoreCase))
                        {
                            prop.enumValueIndex = i;
                            break;
                        }
                    }
                }
            }

            if (payload.TryGetValue("moveSpeed", out var speedObj))
                so.FindProperty("moveSpeed").floatValue = Convert.ToSingle(speedObj);
            if (payload.TryGetValue("turnSpeed", out var turnObj))
                so.FindProperty("turnSpeed").floatValue = Convert.ToSingle(turnObj);
            if (payload.TryGetValue("use2D", out var use2DObj))
                so.FindProperty("use2DMovement").boolValue = Convert.ToBoolean(use2DObj);

            // Patrol settings
            if (payload.TryGetValue("patrolMode", out var patrolModeObj))
            {
                var modeName = ParsePatrolMode(patrolModeObj.ToString());
                var prop = so.FindProperty("patrolMode");
                if (prop != null)
                {
                    var names = prop.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], modeName, StringComparison.OrdinalIgnoreCase))
                        {
                            prop.enumValueIndex = i;
                            break;
                        }
                    }
                }
            }

            if (payload.TryGetValue("waitTimeAtPoint", out var waitObj))
                so.FindProperty("waitTimeAtPoint").floatValue = Convert.ToSingle(waitObj);
            if (payload.TryGetValue("arrivalThreshold", out var arrivalObj))
                so.FindProperty("arrivalThreshold").floatValue = Convert.ToSingle(arrivalObj);

            // Detection settings
            if (payload.TryGetValue("chaseTargetTag", out var tagObj))
                so.FindProperty("chaseTargetTag").stringValue = tagObj.ToString();
            if (payload.TryGetValue("detectionRadius", out var detectObj))
                so.FindProperty("detectionRadius").floatValue = Convert.ToSingle(detectObj);
            if (payload.TryGetValue("loseTargetDistance", out var loseObj))
                so.FindProperty("loseTargetDistance").floatValue = Convert.ToSingle(loseObj);
            if (payload.TryGetValue("fieldOfView", out var fovObj))
                so.FindProperty("fieldOfView").floatValue = Convert.ToSingle(fovObj);
            if (payload.TryGetValue("requireLineOfSight", out var losObj))
                so.FindProperty("requireLineOfSight").boolValue = Convert.ToBoolean(losObj);

            // Attack settings
            if (payload.TryGetValue("attackRange", out var attackRangeObj))
                so.FindProperty("attackRange").floatValue = Convert.ToSingle(attackRangeObj);
            if (payload.TryGetValue("attackCooldown", out var attackCdObj))
                so.FindProperty("attackCooldown").floatValue = Convert.ToSingle(attackCdObj);

            // Flee settings
            if (payload.TryGetValue("fleeDistance", out var fleeDistObj))
                so.FindProperty("fleeDistance").floatValue = Convert.ToSingle(fleeDistObj);
            if (payload.TryGetValue("safeDistance", out var safeDistObj))
                so.FindProperty("safeDistance").floatValue = Convert.ToSingle(safeDistObj);

            // Handle patrol points array
            if (payload.TryGetValue("patrolPoints", out var pointsObj) && pointsObj is List<object> pointsList)
            {
                var patrolProp = so.FindProperty("patrolPoints");
                if (patrolProp != null)
                {
                    patrolProp.ClearArray();
                    foreach (var pointObj in pointsList)
                    {
                        Transform pointTransform = null;
                        if (pointObj is string pointPath)
                        {
                            var pointGo = ResolveGameObject(pointPath);
                            if (pointGo != null) pointTransform = pointGo.transform;
                        }
                        else if (pointObj is Dictionary<string, object> pointDict)
                        {
                            var pos = GetVector3FromDict(pointDict, component.transform.position);
                            var pointGo = new GameObject($"PatrolPoint_{patrolProp.arraySize}");
                            Undo.RegisterCreatedObjectUndo(pointGo, "Create Patrol Point");
                            pointGo.transform.position = pos;
                            pointGo.transform.SetParent(component.transform.parent);
                            pointTransform = pointGo.transform;
                        }
                        if (pointTransform != null)
                        {
                            patrolProp.InsertArrayElementAtIndex(patrolProp.arraySize);
                            patrolProp.GetArrayElementAtIndex(patrolProp.arraySize - 1).objectReferenceValue = pointTransform;
                        }
                    }
                }
            }

            so.ApplyModifiedProperties();
        }

        #endregion

        #region Inspect

        private object InspectAI(Dictionary<string, object> payload)
        {
            var component = ResolveAIComponent(payload);
            var so = new SerializedObject(component);

            var patrolProp = so.FindProperty("patrolPoints");
            var patrolPointsInfo = new List<Dictionary<string, object>>();
            if (patrolProp != null)
            {
                for (int i = 0; i < patrolProp.arraySize; i++)
                {
                    var pointRef = patrolProp.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                    if (pointRef != null)
                    {
                        patrolPointsInfo.Add(new Dictionary<string, object>
                        {
                            { "index", i },
                            { "path", BuildGameObjectPath(pointRef.gameObject) },
                            { "position", new Dictionary<string, object>
                                {
                                    { "x", pointRef.position.x },
                                    { "y", pointRef.position.y },
                                    { "z", pointRef.position.z }
                                }
                            }
                        });
                    }
                }
            }

            var targetRef = so.FindProperty("chaseTarget")?.objectReferenceValue as Transform;
            var behaviorTypeProp = so.FindProperty("behaviorType");
            var currentStateProp = so.FindProperty("currentState");

            var info = new Dictionary<string, object>
            {
                { "aiId", so.FindProperty("aiId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "behaviorType", behaviorTypeProp != null && behaviorTypeProp.enumValueIndex < behaviorTypeProp.enumDisplayNames.Length
                    ? behaviorTypeProp.enumDisplayNames[behaviorTypeProp.enumValueIndex] : "Patrol" },
                { "currentState", currentStateProp != null && currentStateProp.enumValueIndex < currentStateProp.enumDisplayNames.Length
                    ? currentStateProp.enumDisplayNames[currentStateProp.enumValueIndex] : "Idle" },
                { "targetPath", targetRef != null ? BuildGameObjectPath(targetRef.gameObject) : null },
                { "moveSpeed", so.FindProperty("moveSpeed").floatValue },
                { "detectionRadius", so.FindProperty("detectionRadius").floatValue },
                { "attackRange", so.FindProperty("attackRange").floatValue },
                { "patrolPoints", patrolPointsInfo },
                { "patrolPointCount", patrolPointsInfo.Count },
                { "chaseTargetTag", so.FindProperty("chaseTargetTag").stringValue },
                { "use2D", so.FindProperty("use2DMovement").boolValue }
            };

            return CreateSuccessResponse(("ai", info));
        }

        #endregion

        #region Delete

        private object DeleteAI(Dictionary<string, object> payload)
        {
            var component = ResolveAIComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var aiId = new SerializedObject(component).FindProperty("aiId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component);
            ScriptGenerator.Delete(aiId);
            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("aiId", aiId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Runtime Operations

        private object SetTarget(Dictionary<string, object> payload)
        {
            var component = ResolveAIComponent(payload);
            var targetPath = GetString(payload, "chaseTargetPath");

            if (string.IsNullOrEmpty(targetPath))
                throw new InvalidOperationException("chaseTargetPath is required for setTarget.");

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
                throw new InvalidOperationException($"Target GameObject not found at path: {targetPath}");

            var so = new SerializedObject(component);
            so.FindProperty("chaseTarget").objectReferenceValue = targetGo.transform;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("aiId", so.FindProperty("aiId").stringValue),
                ("targetPath", targetPath),
                ("targetSet", true)
            );
        }

        private object ClearTarget(Dictionary<string, object> payload)
        {
            var component = ResolveAIComponent(payload);

            var so = new SerializedObject(component);
            so.FindProperty("chaseTarget").objectReferenceValue = null;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("aiId", so.FindProperty("aiId").stringValue),
                ("targetCleared", true)
            );
        }

        private object SetState(Dictionary<string, object> payload)
        {
            var component = ResolveAIComponent(payload);
            var stateStr = GetString(payload, "state");

            if (string.IsNullOrEmpty(stateStr))
                throw new InvalidOperationException("state is required for setState.");

            var stateName = ParseAIState(stateStr);

            var so = new SerializedObject(component);
            var stateProp = so.FindProperty("currentState");
            if (stateProp != null)
            {
                var names = stateProp.enumDisplayNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], stateName, StringComparison.OrdinalIgnoreCase))
                    {
                        stateProp.enumValueIndex = i;
                        break;
                    }
                }
            }
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("aiId", so.FindProperty("aiId").stringValue),
                ("state", stateName),
                ("note", "State set. Full behavior requires Play mode.")
            );
        }

        #endregion

        #region Patrol Point Operations

        private object AddPatrolPoint(Dictionary<string, object> payload)
        {
            var component = ResolveAIComponent(payload);
            var pointPath = GetString(payload, "pointPath");
            var so = new SerializedObject(component);
            var patrolProp = so.FindProperty("patrolPoints");
            var aiId = so.FindProperty("aiId").stringValue;

            if (string.IsNullOrEmpty(pointPath))
            {
                var pointIndex = patrolProp.arraySize;
                var pointGo = new GameObject($"PatrolPoint_{aiId}_{pointIndex}");
                Undo.RegisterCreatedObjectUndo(pointGo, "Create Patrol Point");

                if (payload.TryGetValue("position", out var posObj) && posObj is Dictionary<string, object> posDict)
                    pointGo.transform.position = GetVector3FromDict(posDict, component.transform.position + Vector3.right * (pointIndex + 1) * 2);
                else
                    pointGo.transform.position = component.transform.position + Vector3.right * (pointIndex + 1) * 2;

                pointGo.transform.SetParent(component.transform.parent);

                patrolProp.InsertArrayElementAtIndex(patrolProp.arraySize);
                patrolProp.GetArrayElementAtIndex(patrolProp.arraySize - 1).objectReferenceValue = pointGo.transform;
                so.ApplyModifiedProperties();

                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

                return CreateSuccessResponse(
                    ("aiId", aiId),
                    ("pointPath", BuildGameObjectPath(pointGo)),
                    ("pointIndex", pointIndex),
                    ("position", new Dictionary<string, object>
                    {
                        { "x", pointGo.transform.position.x },
                        { "y", pointGo.transform.position.y },
                        { "z", pointGo.transform.position.z }
                    })
                );
            }
            else
            {
                var pointGo = ResolveGameObject(pointPath);
                if (pointGo == null)
                    throw new InvalidOperationException($"GameObject not found at path: {pointPath}");

                patrolProp.InsertArrayElementAtIndex(patrolProp.arraySize);
                patrolProp.GetArrayElementAtIndex(patrolProp.arraySize - 1).objectReferenceValue = pointGo.transform;
                so.ApplyModifiedProperties();

                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

                return CreateSuccessResponse(
                    ("aiId", aiId),
                    ("pointPath", pointPath),
                    ("pointIndex", patrolProp.arraySize - 1)
                );
            }
        }

        private object ClearPatrolPoints(Dictionary<string, object> payload)
        {
            var component = ResolveAIComponent(payload);

            var so = new SerializedObject(component);
            var patrolProp = so.FindProperty("patrolPoints");
            if (patrolProp != null) patrolProp.ClearArray();
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("aiId", so.FindProperty("aiId").stringValue),
                ("cleared", true)
            );
        }

        #endregion

        #region Find

        private object FindByAIId(Dictionary<string, object> payload)
        {
            var aiId = GetString(payload, "aiId");
            if (string.IsNullOrEmpty(aiId))
                throw new InvalidOperationException("aiId is required.");

            var component = CodeGenHelper.FindComponentInSceneByField("aiId", aiId);
            if (component == null)
                return CreateSuccessResponse(("found", false), ("aiId", aiId));

            var so = new SerializedObject(component);
            var behaviorTypeProp = so.FindProperty("behaviorType");
            var currentStateProp = so.FindProperty("currentState");

            return CreateSuccessResponse(
                ("found", true),
                ("aiId", aiId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("behaviorType", behaviorTypeProp != null && behaviorTypeProp.enumValueIndex < behaviorTypeProp.enumDisplayNames.Length
                    ? behaviorTypeProp.enumDisplayNames[behaviorTypeProp.enumValueIndex] : "Patrol"),
                ("currentState", currentStateProp != null && currentStateProp.enumValueIndex < currentStateProp.enumDisplayNames.Length
                    ? currentStateProp.enumDisplayNames[currentStateProp.enumValueIndex] : "Idle")
            );
        }

        #endregion

        #region Helpers

        private Component ResolveAIComponent(Dictionary<string, object> payload)
        {
            var aiId = GetString(payload, "aiId");
            if (!string.IsNullOrEmpty(aiId))
            {
                var aiById = CodeGenHelper.FindComponentInSceneByField("aiId", aiId);
                if (aiById != null)
                    return aiById;
            }

            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var aiByPath = CodeGenHelper.FindComponentByField(targetGo, "aiId", null);
                    if (aiByPath != null)
                        return aiByPath;
                    throw new InvalidOperationException($"No AI component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either aiId or targetPath is required.");
        }

        private string ParseBehaviorType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "patrol" => "Patrol",
                "chase" => "Chase",
                "flee" => "Flee",
                "patrolandchase" or "patrol_and_chase" => "PatrolAndChase",
                _ => "Patrol"
            };
        }

        private string ParsePatrolMode(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "loop" => "Loop",
                "pingpong" => "PingPong",
                "random" => "Random",
                _ => "Loop"
            };
        }

        private string ParseAIState(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "idle" => "Idle",
                "patrol" => "Patrol",
                "chase" => "Chase",
                "attack" => "Attack",
                "flee" => "Flee",
                "return" => "Return",
                _ => "Idle"
            };
        }

        #endregion
    }
}
