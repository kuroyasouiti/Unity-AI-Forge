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
    /// GameKit AI handler: create and manage AI behaviors.
    /// Supports patrol, chase, flee, and combined patrol-and-chase patterns.
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

        protected override bool RequiresCompilationWait(string operation) => false;

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
            {
                throw new InvalidOperationException("targetPath is required for create.");
            }

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
            {
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            var existingAI = targetGo.GetComponent<GameKitAIBehavior>();
            if (existingAI != null)
            {
                throw new InvalidOperationException($"GameObject already has a GameKitAIBehavior component.");
            }

            var aiId = GetString(payload, "aiId") ?? $"AI_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var behaviorType = ParseBehaviorType(GetString(payload, "behaviorType") ?? "patrol");
            var use2D = GetBool(payload, "use2D", true);

            var ai = Undo.AddComponent<GameKitAIBehavior>(targetGo);
            ai.Initialize(aiId, behaviorType, use2D);

            // Apply settings
            ApplyAISettings(ai, payload);

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("aiId", aiId),
                ("path", BuildGameObjectPath(targetGo)),
                ("behaviorType", behaviorType.ToString())
            );
        }

        #endregion

        #region Update

        private object UpdateAI(Dictionary<string, object> payload)
        {
            var ai = ResolveAIComponent(payload);

            Undo.RecordObject(ai, "Update GameKit AI");
            ApplyAISettings(ai, payload);
            EditorSceneManager.MarkSceneDirty(ai.gameObject.scene);

            return CreateSuccessResponse(
                ("aiId", ai.AIId),
                ("path", BuildGameObjectPath(ai.gameObject)),
                ("updated", true)
            );
        }

        private void ApplyAISettings(GameKitAIBehavior ai, Dictionary<string, object> payload)
        {
            var serialized = new SerializedObject(ai);

            if (payload.TryGetValue("behaviorType", out var typeObj))
            {
                var behaviorType = ParseBehaviorType(typeObj.ToString());
                serialized.FindProperty("behaviorType").enumValueIndex = (int)behaviorType;
            }

            if (payload.TryGetValue("moveSpeed", out var speedObj))
            {
                serialized.FindProperty("moveSpeed").floatValue = Convert.ToSingle(speedObj);
            }

            if (payload.TryGetValue("turnSpeed", out var turnObj))
            {
                serialized.FindProperty("turnSpeed").floatValue = Convert.ToSingle(turnObj);
            }

            if (payload.TryGetValue("use2D", out var use2DObj))
            {
                serialized.FindProperty("use2DMovement").boolValue = Convert.ToBoolean(use2DObj);
            }

            // Patrol settings
            if (payload.TryGetValue("patrolMode", out var patrolModeObj))
            {
                var patrolMode = ParsePatrolMode(patrolModeObj.ToString());
                serialized.FindProperty("patrolMode").enumValueIndex = (int)patrolMode;
            }

            if (payload.TryGetValue("waitTimeAtPoint", out var waitObj))
            {
                serialized.FindProperty("waitTimeAtPoint").floatValue = Convert.ToSingle(waitObj);
            }

            if (payload.TryGetValue("arrivalThreshold", out var arrivalObj))
            {
                serialized.FindProperty("arrivalThreshold").floatValue = Convert.ToSingle(arrivalObj);
            }

            // Detection settings
            if (payload.TryGetValue("chaseTargetTag", out var tagObj))
            {
                serialized.FindProperty("chaseTargetTag").stringValue = tagObj.ToString();
            }

            if (payload.TryGetValue("detectionRadius", out var detectObj))
            {
                serialized.FindProperty("detectionRadius").floatValue = Convert.ToSingle(detectObj);
            }

            if (payload.TryGetValue("loseTargetDistance", out var loseObj))
            {
                serialized.FindProperty("loseTargetDistance").floatValue = Convert.ToSingle(loseObj);
            }

            if (payload.TryGetValue("fieldOfView", out var fovObj))
            {
                serialized.FindProperty("fieldOfView").floatValue = Convert.ToSingle(fovObj);
            }

            if (payload.TryGetValue("requireLineOfSight", out var losObj))
            {
                serialized.FindProperty("requireLineOfSight").boolValue = Convert.ToBoolean(losObj);
            }

            // Attack settings
            if (payload.TryGetValue("attackRange", out var attackRangeObj))
            {
                serialized.FindProperty("attackRange").floatValue = Convert.ToSingle(attackRangeObj);
            }

            if (payload.TryGetValue("attackCooldown", out var attackCdObj))
            {
                serialized.FindProperty("attackCooldown").floatValue = Convert.ToSingle(attackCdObj);
            }

            // Flee settings
            if (payload.TryGetValue("fleeDistance", out var fleeDistObj))
            {
                serialized.FindProperty("fleeDistance").floatValue = Convert.ToSingle(fleeDistObj);
            }

            if (payload.TryGetValue("safeDistance", out var safeDistObj))
            {
                serialized.FindProperty("safeDistance").floatValue = Convert.ToSingle(safeDistObj);
            }

            // Handle patrol points array
            if (payload.TryGetValue("patrolPoints", out var pointsObj) && pointsObj is List<object> pointsList)
            {
                var patrolProp = serialized.FindProperty("patrolPoints");
                patrolProp.ClearArray();

                foreach (var pointObj in pointsList)
                {
                    Transform pointTransform = null;

                    if (pointObj is string pointPath)
                    {
                        var pointGo = ResolveGameObject(pointPath);
                        if (pointGo != null)
                        {
                            pointTransform = pointGo.transform;
                        }
                    }
                    else if (pointObj is Dictionary<string, object> pointDict)
                    {
                        // Create new patrol point GameObject
                        var pos = GetVector3FromDict(pointDict, ai.transform.position);
                        var pointGo = new GameObject($"PatrolPoint_{patrolProp.arraySize}");
                        Undo.RegisterCreatedObjectUndo(pointGo, "Create Patrol Point");
                        pointGo.transform.position = pos;
                        pointGo.transform.SetParent(ai.transform.parent);
                        pointTransform = pointGo.transform;
                    }

                    if (pointTransform != null)
                    {
                        patrolProp.InsertArrayElementAtIndex(patrolProp.arraySize);
                        patrolProp.GetArrayElementAtIndex(patrolProp.arraySize - 1).objectReferenceValue = pointTransform;
                    }
                }
            }

            serialized.ApplyModifiedProperties();
        }

        #endregion

        #region Inspect

        private object InspectAI(Dictionary<string, object> payload)
        {
            var ai = ResolveAIComponent(payload);

            var serialized = new SerializedObject(ai);
            var patrolProp = serialized.FindProperty("patrolPoints");
            var patrolPointsInfo = new List<Dictionary<string, object>>();

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

            var targetRef = serialized.FindProperty("chaseTarget").objectReferenceValue as Transform;

            var info = new Dictionary<string, object>
            {
                { "aiId", ai.AIId },
                { "path", BuildGameObjectPath(ai.gameObject) },
                { "behaviorType", ai.BehaviorType.ToString() },
                { "currentState", ai.CurrentState.ToString() },
                { "hasTarget", ai.HasTarget },
                { "targetPath", targetRef != null ? BuildGameObjectPath(targetRef.gameObject) : null },
                { "moveSpeed", ai.MoveSpeed },
                { "detectionRadius", ai.DetectionRadius },
                { "attackRange", ai.AttackRange },
                { "patrolPoints", patrolPointsInfo },
                { "patrolPointCount", patrolProp.arraySize },
                { "chaseTargetTag", serialized.FindProperty("chaseTargetTag").stringValue },
                { "use2D", serialized.FindProperty("use2DMovement").boolValue }
            };

            return CreateSuccessResponse(("ai", info));
        }

        #endregion

        #region Delete

        private object DeleteAI(Dictionary<string, object> payload)
        {
            var ai = ResolveAIComponent(payload);
            var path = BuildGameObjectPath(ai.gameObject);
            var aiId = ai.AIId;
            var scene = ai.gameObject.scene;

            Undo.DestroyObjectImmediate(ai);
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
            var ai = ResolveAIComponent(payload);
            var targetPath = GetString(payload, "chaseTargetPath");

            if (string.IsNullOrEmpty(targetPath))
            {
                throw new InvalidOperationException("chaseTargetPath is required for setTarget.");
            }

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
            {
                throw new InvalidOperationException($"Target GameObject not found at path: {targetPath}");
            }

            var serialized = new SerializedObject(ai);
            serialized.FindProperty("chaseTarget").objectReferenceValue = targetGo.transform;
            serialized.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(ai.gameObject.scene);

            return CreateSuccessResponse(
                ("aiId", ai.AIId),
                ("targetPath", targetPath),
                ("targetSet", true)
            );
        }

        private object ClearTarget(Dictionary<string, object> payload)
        {
            var ai = ResolveAIComponent(payload);

            var serialized = new SerializedObject(ai);
            serialized.FindProperty("chaseTarget").objectReferenceValue = null;
            serialized.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(ai.gameObject.scene);

            return CreateSuccessResponse(
                ("aiId", ai.AIId),
                ("targetCleared", true)
            );
        }

        private object SetState(Dictionary<string, object> payload)
        {
            var ai = ResolveAIComponent(payload);
            var stateStr = GetString(payload, "state");

            if (string.IsNullOrEmpty(stateStr))
            {
                throw new InvalidOperationException("state is required for setState.");
            }

            var state = ParseAIState(stateStr);

            var serialized = new SerializedObject(ai);
            serialized.FindProperty("currentState").enumValueIndex = (int)state;
            serialized.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(ai.gameObject.scene);

            return CreateSuccessResponse(
                ("aiId", ai.AIId),
                ("state", state.ToString()),
                ("note", "State set. Full behavior requires Play mode.")
            );
        }

        #endregion

        #region Patrol Point Operations

        private object AddPatrolPoint(Dictionary<string, object> payload)
        {
            var ai = ResolveAIComponent(payload);
            var pointPath = GetString(payload, "pointPath");

            Transform pointTransform;

            if (string.IsNullOrEmpty(pointPath))
            {
                // Create new patrol point
                var serialized = new SerializedObject(ai);
                var patrolProp = serialized.FindProperty("patrolPoints");
                var pointIndex = patrolProp.arraySize;

                var pointGo = new GameObject($"PatrolPoint_{ai.AIId}_{pointIndex}");
                Undo.RegisterCreatedObjectUndo(pointGo, "Create Patrol Point");

                if (payload.TryGetValue("position", out var posObj) && posObj is Dictionary<string, object> posDict)
                {
                    pointGo.transform.position = GetVector3FromDict(posDict, ai.transform.position + Vector3.right * (pointIndex + 1) * 2);
                }
                else
                {
                    // Default position offset from AI
                    pointGo.transform.position = ai.transform.position + Vector3.right * (pointIndex + 1) * 2;
                }

                pointGo.transform.SetParent(ai.transform.parent);
                pointTransform = pointGo.transform;

                patrolProp.InsertArrayElementAtIndex(patrolProp.arraySize);
                patrolProp.GetArrayElementAtIndex(patrolProp.arraySize - 1).objectReferenceValue = pointTransform;
                serialized.ApplyModifiedProperties();

                EditorSceneManager.MarkSceneDirty(ai.gameObject.scene);

                return CreateSuccessResponse(
                    ("aiId", ai.AIId),
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
                {
                    throw new InvalidOperationException($"GameObject not found at path: {pointPath}");
                }

                var serialized = new SerializedObject(ai);
                var patrolProp = serialized.FindProperty("patrolPoints");

                patrolProp.InsertArrayElementAtIndex(patrolProp.arraySize);
                patrolProp.GetArrayElementAtIndex(patrolProp.arraySize - 1).objectReferenceValue = pointGo.transform;
                serialized.ApplyModifiedProperties();

                EditorSceneManager.MarkSceneDirty(ai.gameObject.scene);

                return CreateSuccessResponse(
                    ("aiId", ai.AIId),
                    ("pointPath", pointPath),
                    ("pointIndex", patrolProp.arraySize - 1)
                );
            }
        }

        private object ClearPatrolPoints(Dictionary<string, object> payload)
        {
            var ai = ResolveAIComponent(payload);

            var serialized = new SerializedObject(ai);
            var patrolProp = serialized.FindProperty("patrolPoints");
            patrolProp.ClearArray();
            serialized.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(ai.gameObject.scene);

            return CreateSuccessResponse(
                ("aiId", ai.AIId),
                ("cleared", true)
            );
        }

        #endregion

        #region Find

        private object FindByAIId(Dictionary<string, object> payload)
        {
            var aiId = GetString(payload, "aiId");
            if (string.IsNullOrEmpty(aiId))
            {
                throw new InvalidOperationException("aiId is required.");
            }

            var ai = FindAIById(aiId);
            if (ai == null)
            {
                return CreateSuccessResponse(("found", false), ("aiId", aiId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("aiId", ai.AIId),
                ("path", BuildGameObjectPath(ai.gameObject)),
                ("behaviorType", ai.BehaviorType.ToString()),
                ("currentState", ai.CurrentState.ToString())
            );
        }

        #endregion

        #region Helpers

        private GameKitAIBehavior ResolveAIComponent(Dictionary<string, object> payload)
        {
            var aiId = GetString(payload, "aiId");
            if (!string.IsNullOrEmpty(aiId))
            {
                var aiById = FindAIById(aiId);
                if (aiById != null)
                {
                    return aiById;
                }
            }

            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var aiByPath = targetGo.GetComponent<GameKitAIBehavior>();
                    if (aiByPath != null)
                    {
                        return aiByPath;
                    }
                    throw new InvalidOperationException($"No GameKitAIBehavior component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either aiId or targetPath is required.");
        }

        private GameKitAIBehavior FindAIById(string aiId)
        {
            var ais = UnityEngine.Object.FindObjectsByType<GameKitAIBehavior>(FindObjectsSortMode.None);
            foreach (var ai in ais)
            {
                if (ai.AIId == aiId)
                {
                    return ai;
                }
            }
            return null;
        }

        private GameKitAIBehavior.AIBehaviorType ParseBehaviorType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "patrol" => GameKitAIBehavior.AIBehaviorType.Patrol,
                "chase" => GameKitAIBehavior.AIBehaviorType.Chase,
                "flee" => GameKitAIBehavior.AIBehaviorType.Flee,
                "patrolandchase" => GameKitAIBehavior.AIBehaviorType.PatrolAndChase,
                "patrol_and_chase" => GameKitAIBehavior.AIBehaviorType.PatrolAndChase,
                _ => GameKitAIBehavior.AIBehaviorType.Patrol
            };
        }

        private GameKitAIBehavior.PatrolMode ParsePatrolMode(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "loop" => GameKitAIBehavior.PatrolMode.Loop,
                "pingpong" => GameKitAIBehavior.PatrolMode.PingPong,
                "random" => GameKitAIBehavior.PatrolMode.Random,
                _ => GameKitAIBehavior.PatrolMode.Loop
            };
        }

        private GameKitAIBehavior.AIState ParseAIState(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "idle" => GameKitAIBehavior.AIState.Idle,
                "patrol" => GameKitAIBehavior.AIState.Patrol,
                "chase" => GameKitAIBehavior.AIState.Chase,
                "attack" => GameKitAIBehavior.AIState.Attack,
                "flee" => GameKitAIBehavior.AIState.Flee,
                "return" => GameKitAIBehavior.AIState.Return,
                _ => GameKitAIBehavior.AIState.Idle
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

        private bool GetBool(Dictionary<string, object> payload, string key, bool defaultValue)
        {
            if (payload.TryGetValue(key, out var value))
            {
                return Convert.ToBoolean(value);
            }
            return defaultValue;
        }

        #endregion
    }
}
