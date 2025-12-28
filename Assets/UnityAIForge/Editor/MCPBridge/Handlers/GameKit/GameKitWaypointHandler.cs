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
    /// GameKit Waypoint handler: create and manage waypoint path followers.
    /// Supports NPCs, moving platforms, and patrol routes with various path modes.
    /// </summary>
    public class GameKitWaypointHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "update", "inspect", "delete",
            "addWaypoint", "removeWaypoint", "clearWaypoints",
            "startPath", "stopPath", "pausePath", "resumePath", "resetPath",
            "goToWaypoint", "findByWaypointId"
        };

        public override string Category => "gamekitWaypoint";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateWaypoint(payload),
                "update" => UpdateWaypoint(payload),
                "inspect" => InspectWaypoint(payload),
                "delete" => DeleteWaypoint(payload),
                "addWaypoint" => AddWaypointPosition(payload),
                "removeWaypoint" => RemoveWaypointPosition(payload),
                "clearWaypoints" => ClearWaypointPositions(payload),
                "startPath" => StartPath(payload),
                "stopPath" => StopPath(payload),
                "pausePath" => PausePath(payload),
                "resumePath" => ResumePath(payload),
                "resetPath" => ResetPath(payload),
                "goToWaypoint" => GoToWaypoint(payload),
                "findByWaypointId" => FindByWaypointId(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit Waypoint operation: {operation}")
            };
        }

        #region Create

        private object CreateWaypoint(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            GameObject targetGo;

            if (string.IsNullOrEmpty(targetPath))
            {
                // Create a new GameObject
                var name = GetString(payload, "name") ?? "WaypointFollower";
                targetGo = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(targetGo, "Create Waypoint Follower");

                // Set position if provided
                if (payload.TryGetValue("position", out var posObj) && posObj is Dictionary<string, object> posDict)
                {
                    targetGo.transform.position = GetVector3FromDict(posDict, Vector3.zero);
                }
            }
            else
            {
                targetGo = ResolveGameObject(targetPath);
                if (targetGo == null)
                {
                    throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
                }
            }

            // Check if already has waypoint component
            var existingWaypoint = targetGo.GetComponent<GameKitWaypoint>();
            if (existingWaypoint != null)
            {
                throw new InvalidOperationException($"GameObject '{BuildGameObjectPath(targetGo)}' already has a GameKitWaypoint component.");
            }

            var waypointId = GetString(payload, "waypointId") ?? $"Waypoint_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var pathMode = ParsePathMode(GetString(payload, "pathMode") ?? "loop");
            var movementType = ParseMovementType(GetString(payload, "movementType") ?? "transform");

            // Add component
            var waypoint = Undo.AddComponent<GameKitWaypoint>(targetGo);

            // Initialize via serialized object
            var serialized = new SerializedObject(waypoint);
            serialized.FindProperty("waypointId").stringValue = waypointId;
            serialized.FindProperty("pathMode").enumValueIndex = (int)pathMode;
            serialized.FindProperty("movementType").enumValueIndex = (int)movementType;

            // Set basic properties
            if (payload.TryGetValue("moveSpeed", out var speedObj))
            {
                serialized.FindProperty("moveSpeed").floatValue = Convert.ToSingle(speedObj);
            }

            if (payload.TryGetValue("autoStart", out var autoStartObj))
            {
                serialized.FindProperty("autoStart").boolValue = Convert.ToBoolean(autoStartObj);
            }

            // Apply additional settings
            ApplyWaypointSettings(serialized, payload);

            // Create waypoint positions from payload
            if (payload.TryGetValue("waypointPositions", out var positionsObj) && positionsObj is List<object> positions)
            {
                CreateWaypointChildren(targetGo, positions, serialized);
            }

            serialized.ApplyModifiedProperties();

            // Add Rigidbody if needed
            if (movementType == GameKitWaypoint.MovementType.Rigidbody && targetGo.GetComponent<Rigidbody>() == null)
            {
                var rb = Undo.AddComponent<Rigidbody>(targetGo);
                rb.useGravity = false;
                rb.isKinematic = false;
            }
            else if (movementType == GameKitWaypoint.MovementType.Rigidbody2D && targetGo.GetComponent<Rigidbody2D>() == null)
            {
                var rb2d = Undo.AddComponent<Rigidbody2D>(targetGo);
                rb2d.gravityScale = 0f;
                rb2d.bodyType = RigidbodyType2D.Kinematic;
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("waypointId", waypointId),
                ("path", BuildGameObjectPath(targetGo)),
                ("pathMode", pathMode.ToString()),
                ("movementType", movementType.ToString()),
                ("waypointCount", serialized.FindProperty("waypoints").arraySize)
            );
        }

        private void CreateWaypointChildren(GameObject parent, List<object> positions, SerializedObject serialized)
        {
            var waypointsProperty = serialized.FindProperty("waypoints");
            waypointsProperty.ClearArray();

            for (int i = 0; i < positions.Count; i++)
            {
                if (positions[i] is Dictionary<string, object> posDict)
                {
                    var pos = GetVector3FromDict(posDict, Vector3.zero);

                    // Create waypoint child object
                    var wpGo = new GameObject($"WP_{i}");
                    wpGo.transform.SetParent(parent.transform);
                    wpGo.transform.position = pos;
                    Undo.RegisterCreatedObjectUndo(wpGo, "Create Waypoint");

                    // Add to array
                    waypointsProperty.InsertArrayElementAtIndex(i);
                    waypointsProperty.GetArrayElementAtIndex(i).objectReferenceValue = wpGo.transform;
                }
            }
        }

        #endregion

        #region Update

        private object UpdateWaypoint(Dictionary<string, object> payload)
        {
            var waypoint = ResolveWaypointComponent(payload);

            Undo.RecordObject(waypoint, "Update GameKit Waypoint");

            var serialized = new SerializedObject(waypoint);

            if (payload.TryGetValue("pathMode", out var pathModeObj))
            {
                var pathMode = ParsePathMode(pathModeObj.ToString());
                serialized.FindProperty("pathMode").enumValueIndex = (int)pathMode;
            }

            if (payload.TryGetValue("movementType", out var moveTypeObj))
            {
                var movementType = ParseMovementType(moveTypeObj.ToString());
                serialized.FindProperty("movementType").enumValueIndex = (int)movementType;
            }

            if (payload.TryGetValue("moveSpeed", out var speedObj))
            {
                serialized.FindProperty("moveSpeed").floatValue = Convert.ToSingle(speedObj);
            }

            if (payload.TryGetValue("autoStart", out var autoStartObj))
            {
                serialized.FindProperty("autoStart").boolValue = Convert.ToBoolean(autoStartObj);
            }

            ApplyWaypointSettings(serialized, payload);
            serialized.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(waypoint.gameObject.scene);

            return CreateSuccessResponse(
                ("waypointId", waypoint.WaypointId),
                ("path", BuildGameObjectPath(waypoint.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectWaypoint(Dictionary<string, object> payload)
        {
            var waypoint = ResolveWaypointComponent(payload);

            var serialized = new SerializedObject(waypoint);
            var waypointsProperty = serialized.FindProperty("waypoints");

            var waypointPositions = new List<Dictionary<string, object>>();
            for (int i = 0; i < waypointsProperty.arraySize; i++)
            {
                var wpTransform = waypointsProperty.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                if (wpTransform != null)
                {
                    waypointPositions.Add(new Dictionary<string, object>
                    {
                        { "index", i },
                        { "x", wpTransform.position.x },
                        { "y", wpTransform.position.y },
                        { "z", wpTransform.position.z }
                    });
                }
            }

            var info = new Dictionary<string, object>
            {
                { "waypointId", waypoint.WaypointId },
                { "path", BuildGameObjectPath(waypoint.gameObject) },
                { "pathMode", waypoint.Mode.ToString() },
                { "moveSpeed", waypoint.MoveSpeed },
                { "waitTimeAtPoint", waypoint.WaitTimeAtPoint },
                { "isMoving", waypoint.IsMoving },
                { "isWaiting", waypoint.IsWaiting },
                { "currentWaypointIndex", waypoint.CurrentWaypointIndex },
                { "waypointCount", waypoint.WaypointCount },
                { "pathCompleted", waypoint.PathCompleted },
                { "waypointPositions", waypointPositions },
                { "currentTargetPosition", new Dictionary<string, object>
                    {
                        { "x", waypoint.CurrentTargetPosition.x },
                        { "y", waypoint.CurrentTargetPosition.y },
                        { "z", waypoint.CurrentTargetPosition.z }
                    }
                }
            };

            return CreateSuccessResponse(("waypoint", info));
        }

        #endregion

        #region Delete

        private object DeleteWaypoint(Dictionary<string, object> payload)
        {
            var waypoint = ResolveWaypointComponent(payload);
            var path = BuildGameObjectPath(waypoint.gameObject);
            var waypointId = waypoint.WaypointId;
            var scene = waypoint.gameObject.scene;

            var deleteGameObject = GetBool(payload, "deleteGameObject", false);
            var deleteWaypointChildren = GetBool(payload, "deleteWaypointChildren", true);

            // Delete waypoint children first if requested
            if (deleteWaypointChildren)
            {
                var serialized = new SerializedObject(waypoint);
                var waypointsProperty = serialized.FindProperty("waypoints");
                for (int i = waypointsProperty.arraySize - 1; i >= 0; i--)
                {
                    var wpTransform = waypointsProperty.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                    if (wpTransform != null)
                    {
                        Undo.DestroyObjectImmediate(wpTransform.gameObject);
                    }
                }
            }

            if (deleteGameObject)
            {
                Undo.DestroyObjectImmediate(waypoint.gameObject);
            }
            else
            {
                Undo.DestroyObjectImmediate(waypoint);
            }

            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("waypointId", waypointId),
                ("path", path),
                ("deleted", true),
                ("gameObjectDeleted", deleteGameObject)
            );
        }

        #endregion

        #region Waypoint Management

        private object AddWaypointPosition(Dictionary<string, object> payload)
        {
            var waypoint = ResolveWaypointComponent(payload);

            if (!payload.TryGetValue("position", out var posObj) || !(posObj is Dictionary<string, object> posDict))
            {
                throw new InvalidOperationException("position is required for addWaypoint.");
            }

            var pos = GetVector3FromDict(posDict, Vector3.zero);
            var index = GetInt(payload, "index", -1);

            var serialized = new SerializedObject(waypoint);
            var waypointsProperty = serialized.FindProperty("waypoints");

            // Create waypoint child object
            var wpGo = new GameObject($"WP_{waypointsProperty.arraySize}");
            wpGo.transform.SetParent(waypoint.transform);
            wpGo.transform.position = pos;
            Undo.RegisterCreatedObjectUndo(wpGo, "Add Waypoint");

            // Add to array
            int insertIndex = index >= 0 && index <= waypointsProperty.arraySize ? index : waypointsProperty.arraySize;
            waypointsProperty.InsertArrayElementAtIndex(insertIndex);
            waypointsProperty.GetArrayElementAtIndex(insertIndex).objectReferenceValue = wpGo.transform;

            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(waypoint.gameObject.scene);

            return CreateSuccessResponse(
                ("waypointId", waypoint.WaypointId),
                ("addedAtIndex", insertIndex),
                ("totalWaypoints", waypointsProperty.arraySize),
                ("position", new Dictionary<string, object>
                {
                    { "x", pos.x },
                    { "y", pos.y },
                    { "z", pos.z }
                })
            );
        }

        private object RemoveWaypointPosition(Dictionary<string, object> payload)
        {
            var waypoint = ResolveWaypointComponent(payload);
            var index = GetInt(payload, "index", -1);

            if (index < 0)
            {
                throw new InvalidOperationException("index is required for removeWaypoint.");
            }

            var serialized = new SerializedObject(waypoint);
            var waypointsProperty = serialized.FindProperty("waypoints");

            if (index >= waypointsProperty.arraySize)
            {
                throw new InvalidOperationException($"Index {index} is out of range. Total waypoints: {waypointsProperty.arraySize}");
            }

            // Get and destroy the waypoint child
            var wpTransform = waypointsProperty.GetArrayElementAtIndex(index).objectReferenceValue as Transform;
            if (wpTransform != null)
            {
                Undo.DestroyObjectImmediate(wpTransform.gameObject);
            }

            // Remove from array
            waypointsProperty.DeleteArrayElementAtIndex(index);
            if (waypointsProperty.GetArrayElementAtIndex(index).objectReferenceValue != null)
            {
                waypointsProperty.DeleteArrayElementAtIndex(index); // Unity quirk: need to delete twice
            }

            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(waypoint.gameObject.scene);

            return CreateSuccessResponse(
                ("waypointId", waypoint.WaypointId),
                ("removedIndex", index),
                ("totalWaypoints", waypointsProperty.arraySize)
            );
        }

        private object ClearWaypointPositions(Dictionary<string, object> payload)
        {
            var waypoint = ResolveWaypointComponent(payload);

            var serialized = new SerializedObject(waypoint);
            var waypointsProperty = serialized.FindProperty("waypoints");

            // Destroy all waypoint children
            for (int i = waypointsProperty.arraySize - 1; i >= 0; i--)
            {
                var wpTransform = waypointsProperty.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                if (wpTransform != null)
                {
                    Undo.DestroyObjectImmediate(wpTransform.gameObject);
                }
            }

            waypointsProperty.ClearArray();
            serialized.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(waypoint.gameObject.scene);

            return CreateSuccessResponse(
                ("waypointId", waypoint.WaypointId),
                ("cleared", true),
                ("totalWaypoints", 0)
            );
        }

        #endregion

        #region Path Control

        private object StartPath(Dictionary<string, object> payload)
        {
            var waypoint = ResolveWaypointComponent(payload);

            return CreateSuccessResponse(
                ("waypointId", waypoint.WaypointId),
                ("note", "Path will start in play mode. Use autoStart=true for automatic start.")
            );
        }

        private object StopPath(Dictionary<string, object> payload)
        {
            var waypoint = ResolveWaypointComponent(payload);

            return CreateSuccessResponse(
                ("waypointId", waypoint.WaypointId),
                ("note", "Path control requires play mode.")
            );
        }

        private object PausePath(Dictionary<string, object> payload)
        {
            var waypoint = ResolveWaypointComponent(payload);

            return CreateSuccessResponse(
                ("waypointId", waypoint.WaypointId),
                ("note", "Path control requires play mode.")
            );
        }

        private object ResumePath(Dictionary<string, object> payload)
        {
            var waypoint = ResolveWaypointComponent(payload);

            return CreateSuccessResponse(
                ("waypointId", waypoint.WaypointId),
                ("note", "Path control requires play mode.")
            );
        }

        private object ResetPath(Dictionary<string, object> payload)
        {
            var waypoint = ResolveWaypointComponent(payload);

            // In editor mode, we can move to first waypoint position
            var serialized = new SerializedObject(waypoint);
            var waypointsProperty = serialized.FindProperty("waypoints");

            if (waypointsProperty.arraySize > 0)
            {
                var firstWp = waypointsProperty.GetArrayElementAtIndex(0).objectReferenceValue as Transform;
                if (firstWp != null)
                {
                    waypoint.transform.position = firstWp.position;
                }
            }

            EditorSceneManager.MarkSceneDirty(waypoint.gameObject.scene);

            return CreateSuccessResponse(
                ("waypointId", waypoint.WaypointId),
                ("reset", true),
                ("position", new Dictionary<string, object>
                {
                    { "x", waypoint.transform.position.x },
                    { "y", waypoint.transform.position.y },
                    { "z", waypoint.transform.position.z }
                })
            );
        }

        private object GoToWaypoint(Dictionary<string, object> payload)
        {
            var waypoint = ResolveWaypointComponent(payload);
            var index = GetInt(payload, "index", 0);

            var serialized = new SerializedObject(waypoint);
            var waypointsProperty = serialized.FindProperty("waypoints");

            if (index < 0 || index >= waypointsProperty.arraySize)
            {
                throw new InvalidOperationException($"Index {index} is out of range. Total waypoints: {waypointsProperty.arraySize}");
            }

            // Move to the specified waypoint position
            var wpTransform = waypointsProperty.GetArrayElementAtIndex(index).objectReferenceValue as Transform;
            if (wpTransform != null)
            {
                waypoint.transform.position = wpTransform.position;
            }

            EditorSceneManager.MarkSceneDirty(waypoint.gameObject.scene);

            return CreateSuccessResponse(
                ("waypointId", waypoint.WaypointId),
                ("movedToIndex", index),
                ("position", new Dictionary<string, object>
                {
                    { "x", waypoint.transform.position.x },
                    { "y", waypoint.transform.position.y },
                    { "z", waypoint.transform.position.z }
                })
            );
        }

        #endregion

        #region Find

        private object FindByWaypointId(Dictionary<string, object> payload)
        {
            var waypointId = GetString(payload, "waypointId");
            if (string.IsNullOrEmpty(waypointId))
            {
                throw new InvalidOperationException("waypointId is required for findByWaypointId.");
            }

            var waypoint = FindWaypointById(waypointId);
            if (waypoint == null)
            {
                return CreateSuccessResponse(("found", false), ("waypointId", waypointId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("waypointId", waypoint.WaypointId),
                ("path", BuildGameObjectPath(waypoint.gameObject)),
                ("pathMode", waypoint.Mode.ToString()),
                ("waypointCount", waypoint.WaypointCount),
                ("isMoving", waypoint.IsMoving)
            );
        }

        #endregion

        #region Helpers

        private GameKitWaypoint ResolveWaypointComponent(Dictionary<string, object> payload)
        {
            // Try by waypointId first
            var waypointId = GetString(payload, "waypointId");
            if (!string.IsNullOrEmpty(waypointId))
            {
                var byId = FindWaypointById(waypointId);
                if (byId != null) return byId;
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var byPath = targetGo.GetComponent<GameKitWaypoint>();
                    if (byPath != null) return byPath;
                    throw new InvalidOperationException($"No GameKitWaypoint component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either waypointId or targetPath is required.");
        }

        private GameKitWaypoint FindWaypointById(string waypointId)
        {
            var waypoints = UnityEngine.Object.FindObjectsByType<GameKitWaypoint>(FindObjectsSortMode.None);
            foreach (var waypoint in waypoints)
            {
                if (waypoint.WaypointId == waypointId)
                {
                    return waypoint;
                }
            }
            return null;
        }

        private void ApplyWaypointSettings(SerializedObject serialized, Dictionary<string, object> payload)
        {
            if (payload.TryGetValue("rotationSpeed", out var rotSpeedObj))
            {
                serialized.FindProperty("rotationSpeed").floatValue = Convert.ToSingle(rotSpeedObj);
            }

            if (payload.TryGetValue("rotationMode", out var rotModeObj))
            {
                var rotMode = ParseRotationMode(rotModeObj.ToString());
                serialized.FindProperty("rotationMode").enumValueIndex = (int)rotMode;
            }

            if (payload.TryGetValue("waitTimeAtPoint", out var waitObj))
            {
                serialized.FindProperty("waitTimeAtPoint").floatValue = Convert.ToSingle(waitObj);
            }

            if (payload.TryGetValue("startDelay", out var delayObj))
            {
                serialized.FindProperty("startDelay").floatValue = Convert.ToSingle(delayObj);
            }

            if (payload.TryGetValue("smoothMovement", out var smoothObj))
            {
                serialized.FindProperty("smoothMovement").boolValue = Convert.ToBoolean(smoothObj);
            }

            if (payload.TryGetValue("smoothTime", out var smoothTimeObj))
            {
                serialized.FindProperty("smoothTime").floatValue = Convert.ToSingle(smoothTimeObj);
            }

            if (payload.TryGetValue("arrivalThreshold", out var arrivalObj))
            {
                serialized.FindProperty("arrivalThreshold").floatValue = Convert.ToSingle(arrivalObj);
            }

            if (payload.TryGetValue("useLocalSpace", out var localSpaceObj))
            {
                serialized.FindProperty("useLocalSpace").boolValue = Convert.ToBoolean(localSpaceObj);
            }
        }

        private GameKitWaypoint.PathMode ParsePathMode(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "once" => GameKitWaypoint.PathMode.Once,
                "loop" => GameKitWaypoint.PathMode.Loop,
                "pingpong" => GameKitWaypoint.PathMode.PingPong,
                _ => GameKitWaypoint.PathMode.Loop
            };
        }

        private GameKitWaypoint.MovementType ParseMovementType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "transform" => GameKitWaypoint.MovementType.Transform,
                "rigidbody" => GameKitWaypoint.MovementType.Rigidbody,
                "rigidbody2d" => GameKitWaypoint.MovementType.Rigidbody2D,
                _ => GameKitWaypoint.MovementType.Transform
            };
        }

        private GameKitWaypoint.RotationMode ParseRotationMode(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "none" => GameKitWaypoint.RotationMode.None,
                "lookattarget" => GameKitWaypoint.RotationMode.LookAtTarget,
                "aligntopath" => GameKitWaypoint.RotationMode.AlignToPath,
                _ => GameKitWaypoint.RotationMode.LookAtTarget
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

        private float GetFloat(Dictionary<string, object> payload, string key, float defaultValue)
        {
            if (payload.TryGetValue(key, out var value))
            {
                return Convert.ToSingle(value);
            }
            return defaultValue;
        }

        private int GetInt(Dictionary<string, object> payload, string key, int defaultValue)
        {
            if (payload.TryGetValue(key, out var value))
            {
                return Convert.ToInt32(value);
            }
            return defaultValue;
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
