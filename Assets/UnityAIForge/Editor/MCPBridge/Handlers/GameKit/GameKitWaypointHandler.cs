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
    /// GameKit Waypoint handler: create and manage waypoint path followers.
    /// Uses code generation to produce standalone Waypoint scripts with zero package dependency.
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

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

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

            // Check if already has waypoint component (by checking for waypointId field)
            var existingWaypoint = CodeGenHelper.FindComponentByField(targetGo, "waypointId", null);
            if (existingWaypoint != null)
            {
                throw new InvalidOperationException(
                    $"GameObject '{BuildGameObjectPath(targetGo)}' already has a Waypoint component.");
            }

            var waypointId = GetString(payload, "waypointId") ?? $"Waypoint_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var pathMode = ParsePathMode(GetString(payload, "pathMode") ?? "loop");
            var autoStart = GetBool(payload, "autoStart", false);
            var useLocalSpace = GetBool(payload, "useLocalSpace", false);
            var movementType = ParseMovementType(GetString(payload, "movementType") ?? "transform");
            var moveSpeed = GetFloat(payload, "moveSpeed", 5f);
            var rotationSpeed = GetFloat(payload, "rotationSpeed", 5f);
            var rotationMode = ParseRotationMode(GetString(payload, "rotationMode") ?? "none");
            var waitTimeAtPoint = GetFloat(payload, "waitTimeAtPoint", 0f);
            var startDelay = GetFloat(payload, "startDelay", 0f);
            var smoothMovement = GetBool(payload, "smoothMovement", false);
            var smoothTime = GetFloat(payload, "smoothTime", 0.3f);
            var arrivalThreshold = GetFloat(payload, "arrivalThreshold", 0.1f);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(waypointId, "Waypoint");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "WAYPOINT_ID", waypointId },
                { "PATH_MODE", pathMode },
                { "AUTO_START", autoStart.ToString().ToLowerInvariant() },
                { "USE_LOCAL_SPACE", useLocalSpace.ToString().ToLowerInvariant() },
                { "MOVEMENT_TYPE", movementType },
                { "MOVE_SPEED", moveSpeed },
                { "ROTATION_SPEED", rotationSpeed },
                { "ROTATION_MODE", rotationMode },
                { "WAIT_TIME_AT_POINT", waitTimeAtPoint },
                { "START_DELAY", startDelay },
                { "SMOOTH_MOVEMENT", smoothMovement.ToString().ToLowerInvariant() },
                { "SMOOTH_TIME", smoothTime },
                { "ARRIVAL_THRESHOLD", arrivalThreshold }
            };

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "Waypoint", waypointId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate Waypoint script.");
            }

            // Create waypoint child positions from payload
            int waypointCount = 0;
            if (payload.TryGetValue("waypointPositions", out var positionsObj) && positionsObj is List<object> positions)
            {
                waypointCount = CreateWaypointChildren(targetGo, positions, waypointId);
            }

            // Add Rigidbody if needed for the movement type
            if (movementType == "Rigidbody" && targetGo.GetComponent<Rigidbody>() == null)
            {
                var rb = Undo.AddComponent<Rigidbody>(targetGo);
                rb.useGravity = false;
                rb.isKinematic = false;
            }
            else if (movementType == "Rigidbody2D" && targetGo.GetComponent<Rigidbody2D>() == null)
            {
                var rb2d = Undo.AddComponent<Rigidbody2D>(targetGo);
                rb2d.gravityScale = 0f;
                rb2d.bodyType = RigidbodyType2D.Kinematic;
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["waypointId"] = waypointId;
            result["path"] = BuildGameObjectPath(targetGo);
            result["pathMode"] = pathMode;
            result["movementType"] = movementType;
            result["waypointCount"] = waypointCount;

            return result;
        }

        private int CreateWaypointChildren(GameObject parent, List<object> positions, string waypointId)
        {
            // Find the waypoint component to populate its waypoints array
            var component = CodeGenHelper.FindComponentByField(parent, "waypointId", waypointId);
            SerializedObject serialized = null;
            SerializedProperty waypointsProperty = null;

            if (component != null)
            {
                serialized = new SerializedObject(component);
                waypointsProperty = serialized.FindProperty("waypoints");
                if (waypointsProperty != null)
                {
                    waypointsProperty.ClearArray();
                }
            }

            int count = 0;
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

                    // Add to array if component is available
                    if (waypointsProperty != null)
                    {
                        waypointsProperty.InsertArrayElementAtIndex(i);
                        waypointsProperty.GetArrayElementAtIndex(i).objectReferenceValue = wpGo.transform;
                    }

                    count++;
                }
            }

            if (serialized != null)
            {
                serialized.ApplyModifiedProperties();
            }

            return count;
        }

        #endregion

        #region Update

        private object UpdateWaypoint(Dictionary<string, object> payload)
        {
            var component = ResolveWaypointComponent(payload);

            Undo.RecordObject(component, "Update Waypoint");

            var so = new SerializedObject(component);

            if (payload.TryGetValue("pathMode", out var pathModeObj))
            {
                var pathMode = ParsePathMode(pathModeObj.ToString());
                var prop = so.FindProperty("pathMode");
                if (prop != null)
                {
                    var names = prop.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], pathMode, StringComparison.OrdinalIgnoreCase))
                        {
                            prop.enumValueIndex = i;
                            break;
                        }
                    }
                }
            }

            if (payload.TryGetValue("movementType", out var moveTypeObj))
            {
                var movementType = ParseMovementType(moveTypeObj.ToString());
                var prop = so.FindProperty("movementType");
                if (prop != null)
                {
                    var names = prop.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], movementType, StringComparison.OrdinalIgnoreCase))
                        {
                            prop.enumValueIndex = i;
                            break;
                        }
                    }
                }
            }

            if (payload.TryGetValue("moveSpeed", out var speedObj))
                so.FindProperty("moveSpeed").floatValue = Convert.ToSingle(speedObj);

            if (payload.TryGetValue("autoStart", out var autoStartObj))
                so.FindProperty("autoStart").boolValue = Convert.ToBoolean(autoStartObj);

            ApplyWaypointSettings(so, payload);
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var waypointId = new SerializedObject(component).FindProperty("waypointId").stringValue;

            return CreateSuccessResponse(
                ("waypointId", waypointId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectWaypoint(Dictionary<string, object> payload)
        {
            var component = ResolveWaypointComponent(payload);

            var so = new SerializedObject(component);
            var waypointsProperty = so.FindProperty("waypoints");

            var waypointPositions = new List<Dictionary<string, object>>();
            if (waypointsProperty != null)
            {
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
            }

            var pathModeProp = so.FindProperty("pathMode");
            var pathModeStr = pathModeProp != null && pathModeProp.enumValueIndex < pathModeProp.enumDisplayNames.Length
                ? pathModeProp.enumDisplayNames[pathModeProp.enumValueIndex]
                : "Loop";

            var movementTypeProp = so.FindProperty("movementType");
            var movementTypeStr = movementTypeProp != null && movementTypeProp.enumValueIndex < movementTypeProp.enumDisplayNames.Length
                ? movementTypeProp.enumDisplayNames[movementTypeProp.enumValueIndex]
                : "Transform";

            var rotationModeProp = so.FindProperty("rotationMode");
            var rotationModeStr = rotationModeProp != null && rotationModeProp.enumValueIndex < rotationModeProp.enumDisplayNames.Length
                ? rotationModeProp.enumDisplayNames[rotationModeProp.enumValueIndex]
                : "None";

            // Read properties with null safety
            var moveSpeedProp = so.FindProperty("moveSpeed");
            var waitTimeProp = so.FindProperty("waitTimeAtPoint");
            var autoStartProp = so.FindProperty("autoStart");
            var smoothMovementProp = so.FindProperty("smoothMovement");
            var useLocalSpaceProp = so.FindProperty("useLocalSpace");
            var arrivalThresholdProp = so.FindProperty("arrivalThreshold");

            var info = new Dictionary<string, object>
            {
                { "waypointId", so.FindProperty("waypointId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "pathMode", pathModeStr },
                { "movementType", movementTypeStr },
                { "rotationMode", rotationModeStr },
                { "moveSpeed", moveSpeedProp != null ? moveSpeedProp.floatValue : 5f },
                { "waitTimeAtPoint", waitTimeProp != null ? waitTimeProp.floatValue : 0f },
                { "autoStart", autoStartProp != null && autoStartProp.boolValue },
                { "smoothMovement", smoothMovementProp != null && smoothMovementProp.boolValue },
                { "useLocalSpace", useLocalSpaceProp != null && useLocalSpaceProp.boolValue },
                { "arrivalThreshold", arrivalThresholdProp != null ? arrivalThresholdProp.floatValue : 0.1f },
                { "waypointCount", waypointsProperty != null ? waypointsProperty.arraySize : 0 },
                { "waypointPositions", waypointPositions }
            };

            // Try to read runtime state via reflection (available during play mode)
            try
            {
                var compType = component.GetType();
                var isMovingProp = compType.GetProperty("IsMoving");
                if (isMovingProp != null)
                    info["isMoving"] = isMovingProp.GetValue(component);

                var isWaitingProp = compType.GetProperty("IsWaiting");
                if (isWaitingProp != null)
                    info["isWaiting"] = isWaitingProp.GetValue(component);

                var currentIndexProp = compType.GetProperty("CurrentWaypointIndex");
                if (currentIndexProp != null)
                    info["currentWaypointIndex"] = currentIndexProp.GetValue(component);

                var pathCompletedProp = compType.GetProperty("PathCompleted");
                if (pathCompletedProp != null)
                    info["pathCompleted"] = pathCompletedProp.GetValue(component);

                var currentTargetProp = compType.GetProperty("CurrentTargetPosition");
                if (currentTargetProp != null)
                {
                    var targetPos = (Vector3)currentTargetProp.GetValue(component);
                    info["currentTargetPosition"] = new Dictionary<string, object>
                    {
                        { "x", targetPos.x },
                        { "y", targetPos.y },
                        { "z", targetPos.z }
                    };
                }
            }
            catch
            {
                // Runtime properties not available in edit mode - that's fine
            }

            return CreateSuccessResponse(("waypoint", info));
        }

        #endregion

        #region Delete

        private object DeleteWaypoint(Dictionary<string, object> payload)
        {
            var component = ResolveWaypointComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var waypointId = new SerializedObject(component).FindProperty("waypointId").stringValue;
            var scene = component.gameObject.scene;

            var deleteGameObject = GetBool(payload, "deleteGameObject", false);
            var deleteWaypointChildren = GetBool(payload, "deleteWaypointChildren", true);

            // Delete waypoint children first if requested
            if (deleteWaypointChildren)
            {
                var so = new SerializedObject(component);
                var waypointsProperty = so.FindProperty("waypoints");
                if (waypointsProperty != null)
                {
                    for (int i = waypointsProperty.arraySize - 1; i >= 0; i--)
                    {
                        var wpTransform = waypointsProperty.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                        if (wpTransform != null)
                        {
                            Undo.DestroyObjectImmediate(wpTransform.gameObject);
                        }
                    }
                }
            }

            if (deleteGameObject)
            {
                Undo.DestroyObjectImmediate(component.gameObject);
            }
            else
            {
                Undo.DestroyObjectImmediate(component);
            }

            // Clean up the generated script from tracker
            ScriptGenerator.Delete(waypointId);

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
            var component = ResolveWaypointComponent(payload);

            if (!payload.TryGetValue("position", out var posObj) || !(posObj is Dictionary<string, object> posDict))
            {
                throw new InvalidOperationException("position is required for addWaypoint.");
            }

            var pos = GetVector3FromDict(posDict, Vector3.zero);
            var index = GetInt(payload, "index", -1);

            var so = new SerializedObject(component);
            var waypointsProperty = so.FindProperty("waypoints");
            var waypointId = so.FindProperty("waypointId").stringValue;

            // Create waypoint child object
            var wpGo = new GameObject($"WP_{waypointsProperty.arraySize}");
            wpGo.transform.SetParent(component.transform);
            wpGo.transform.position = pos;
            Undo.RegisterCreatedObjectUndo(wpGo, "Add Waypoint");

            // Add to array
            int insertIndex = index >= 0 && index <= waypointsProperty.arraySize ? index : waypointsProperty.arraySize;
            waypointsProperty.InsertArrayElementAtIndex(insertIndex);
            waypointsProperty.GetArrayElementAtIndex(insertIndex).objectReferenceValue = wpGo.transform;

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("waypointId", waypointId),
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
            var component = ResolveWaypointComponent(payload);
            var index = GetInt(payload, "index", -1);

            if (index < 0)
            {
                throw new InvalidOperationException("index is required for removeWaypoint.");
            }

            var so = new SerializedObject(component);
            var waypointsProperty = so.FindProperty("waypoints");
            var waypointId = so.FindProperty("waypointId").stringValue;

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
            // Unity quirk: if the element was an object reference, first delete sets it to null,
            // second delete actually removes the array element
            if (index < waypointsProperty.arraySize)
            {
                var element = waypointsProperty.GetArrayElementAtIndex(index);
                if (element.propertyType == SerializedPropertyType.ObjectReference
                    && element.objectReferenceValue == null)
                {
                    waypointsProperty.DeleteArrayElementAtIndex(index);
                }
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("waypointId", waypointId),
                ("removedIndex", index),
                ("totalWaypoints", waypointsProperty.arraySize)
            );
        }

        private object ClearWaypointPositions(Dictionary<string, object> payload)
        {
            var component = ResolveWaypointComponent(payload);

            var so = new SerializedObject(component);
            var waypointsProperty = so.FindProperty("waypoints");
            var waypointId = so.FindProperty("waypointId").stringValue;

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
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("waypointId", waypointId),
                ("cleared", true),
                ("totalWaypoints", 0)
            );
        }

        #endregion

        #region Path Control

        private object StartPath(Dictionary<string, object> payload)
        {
            var component = ResolveWaypointComponent(payload);
            var so = new SerializedObject(component);
            var waypointId = so.FindProperty("waypointId").stringValue;

            // Try invoking via reflection in play mode
            try
            {
                var method = component.GetType().GetMethod("StartPath");
                if (method != null && Application.isPlaying)
                {
                    method.Invoke(component, null);
                    return CreateSuccessResponse(
                        ("waypointId", waypointId),
                        ("started", true)
                    );
                }
            }
            catch
            {
                // Fall through to editor-mode response
            }

            return CreateSuccessResponse(
                ("waypointId", waypointId),
                ("note", "Path will start in play mode. Use autoStart=true for automatic start.")
            );
        }

        private object StopPath(Dictionary<string, object> payload)
        {
            var component = ResolveWaypointComponent(payload);
            var so = new SerializedObject(component);
            var waypointId = so.FindProperty("waypointId").stringValue;

            try
            {
                var method = component.GetType().GetMethod("StopPath");
                if (method != null && Application.isPlaying)
                {
                    method.Invoke(component, null);
                    return CreateSuccessResponse(
                        ("waypointId", waypointId),
                        ("stopped", true)
                    );
                }
            }
            catch
            {
                // Fall through
            }

            return CreateSuccessResponse(
                ("waypointId", waypointId),
                ("note", "Path control requires play mode.")
            );
        }

        private object PausePath(Dictionary<string, object> payload)
        {
            var component = ResolveWaypointComponent(payload);
            var so = new SerializedObject(component);
            var waypointId = so.FindProperty("waypointId").stringValue;

            try
            {
                var method = component.GetType().GetMethod("PausePath");
                if (method != null && Application.isPlaying)
                {
                    method.Invoke(component, null);
                    return CreateSuccessResponse(
                        ("waypointId", waypointId),
                        ("paused", true)
                    );
                }
            }
            catch
            {
                // Fall through
            }

            return CreateSuccessResponse(
                ("waypointId", waypointId),
                ("note", "Path control requires play mode.")
            );
        }

        private object ResumePath(Dictionary<string, object> payload)
        {
            var component = ResolveWaypointComponent(payload);
            var so = new SerializedObject(component);
            var waypointId = so.FindProperty("waypointId").stringValue;

            try
            {
                var method = component.GetType().GetMethod("ResumePath");
                if (method != null && Application.isPlaying)
                {
                    method.Invoke(component, null);
                    return CreateSuccessResponse(
                        ("waypointId", waypointId),
                        ("resumed", true)
                    );
                }
            }
            catch
            {
                // Fall through
            }

            return CreateSuccessResponse(
                ("waypointId", waypointId),
                ("note", "Path control requires play mode.")
            );
        }

        private object ResetPath(Dictionary<string, object> payload)
        {
            var component = ResolveWaypointComponent(payload);
            var so = new SerializedObject(component);
            var waypointsProperty = so.FindProperty("waypoints");
            var waypointId = so.FindProperty("waypointId").stringValue;

            // Try reflection-based reset in play mode
            try
            {
                var method = component.GetType().GetMethod("ResetPath");
                if (method != null && Application.isPlaying)
                {
                    method.Invoke(component, null);
                }
            }
            catch
            {
                // Fall through to editor-mode reset
            }

            // In editor mode, move to first waypoint position
            if (waypointsProperty != null && waypointsProperty.arraySize > 0)
            {
                var firstWp = waypointsProperty.GetArrayElementAtIndex(0).objectReferenceValue as Transform;
                if (firstWp != null)
                {
                    component.transform.position = firstWp.position;
                }
            }

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("waypointId", waypointId),
                ("reset", true),
                ("position", new Dictionary<string, object>
                {
                    { "x", component.transform.position.x },
                    { "y", component.transform.position.y },
                    { "z", component.transform.position.z }
                })
            );
        }

        private object GoToWaypoint(Dictionary<string, object> payload)
        {
            var component = ResolveWaypointComponent(payload);
            var index = GetInt(payload, "index", 0);

            var so = new SerializedObject(component);
            var waypointsProperty = so.FindProperty("waypoints");
            var waypointId = so.FindProperty("waypointId").stringValue;

            if (waypointsProperty == null || index < 0 || index >= waypointsProperty.arraySize)
            {
                throw new InvalidOperationException($"Index {index} is out of range. Total waypoints: {(waypointsProperty != null ? waypointsProperty.arraySize : 0)}");
            }

            // Try reflection-based goToWaypoint in play mode
            try
            {
                var method = component.GetType().GetMethod("GoToWaypoint");
                if (method != null && Application.isPlaying)
                {
                    method.Invoke(component, new object[] { index });
                }
            }
            catch
            {
                // Fall through to editor-mode position set
            }

            // Move to the specified waypoint position
            var wpTransform = waypointsProperty.GetArrayElementAtIndex(index).objectReferenceValue as Transform;
            if (wpTransform != null)
            {
                component.transform.position = wpTransform.position;
            }

            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("waypointId", waypointId),
                ("movedToIndex", index),
                ("position", new Dictionary<string, object>
                {
                    { "x", component.transform.position.x },
                    { "y", component.transform.position.y },
                    { "z", component.transform.position.z }
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

            var component = CodeGenHelper.FindComponentInSceneByField("waypointId", waypointId);
            if (component == null)
            {
                return CreateSuccessResponse(("found", false), ("waypointId", waypointId));
            }

            var so = new SerializedObject(component);

            var pathModeProp = so.FindProperty("pathMode");
            var pathModeStr = pathModeProp != null && pathModeProp.enumValueIndex < pathModeProp.enumDisplayNames.Length
                ? pathModeProp.enumDisplayNames[pathModeProp.enumValueIndex]
                : "Loop";

            var waypointsProperty = so.FindProperty("waypoints");
            var waypointCount = waypointsProperty != null ? waypointsProperty.arraySize : 0;

            return CreateSuccessResponse(
                ("found", true),
                ("waypointId", waypointId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("pathMode", pathModeStr),
                ("waypointCount", waypointCount)
            );
        }

        #endregion

        #region Helpers

        private Component ResolveWaypointComponent(Dictionary<string, object> payload)
        {
            // Try by waypointId first
            var waypointId = GetString(payload, "waypointId");
            if (!string.IsNullOrEmpty(waypointId))
            {
                var byId = CodeGenHelper.FindComponentInSceneByField("waypointId", waypointId);
                if (byId != null) return byId;
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var byPath = CodeGenHelper.FindComponentByField(targetGo, "waypointId", null);
                    if (byPath != null) return byPath;
                    throw new InvalidOperationException($"No Waypoint component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either waypointId or targetPath is required.");
        }

        private void ApplyWaypointSettings(SerializedObject so, Dictionary<string, object> payload)
        {
            if (payload.TryGetValue("rotationSpeed", out var rotSpeedObj))
            {
                var prop = so.FindProperty("rotationSpeed");
                if (prop != null)
                    prop.floatValue = Convert.ToSingle(rotSpeedObj);
            }

            if (payload.TryGetValue("rotationMode", out var rotModeObj))
            {
                var rotMode = ParseRotationMode(rotModeObj.ToString());
                var prop = so.FindProperty("rotationMode");
                if (prop != null)
                {
                    var names = prop.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], rotMode, StringComparison.OrdinalIgnoreCase))
                        {
                            prop.enumValueIndex = i;
                            break;
                        }
                    }
                }
            }

            if (payload.TryGetValue("waitTimeAtPoint", out var waitObj))
            {
                var prop = so.FindProperty("waitTimeAtPoint");
                if (prop != null)
                    prop.floatValue = Convert.ToSingle(waitObj);
            }

            if (payload.TryGetValue("startDelay", out var delayObj))
            {
                var prop = so.FindProperty("startDelay");
                if (prop != null)
                    prop.floatValue = Convert.ToSingle(delayObj);
            }

            if (payload.TryGetValue("smoothMovement", out var smoothObj))
            {
                var prop = so.FindProperty("smoothMovement");
                if (prop != null)
                    prop.boolValue = Convert.ToBoolean(smoothObj);
            }

            if (payload.TryGetValue("smoothTime", out var smoothTimeObj))
            {
                var prop = so.FindProperty("smoothTime");
                if (prop != null)
                    prop.floatValue = Convert.ToSingle(smoothTimeObj);
            }

            if (payload.TryGetValue("arrivalThreshold", out var arrivalObj))
            {
                var prop = so.FindProperty("arrivalThreshold");
                if (prop != null)
                    prop.floatValue = Convert.ToSingle(arrivalObj);
            }

            if (payload.TryGetValue("useLocalSpace", out var localSpaceObj))
            {
                var prop = so.FindProperty("useLocalSpace");
                if (prop != null)
                    prop.boolValue = Convert.ToBoolean(localSpaceObj);
            }
        }

        private string ParsePathMode(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "once" => "Once",
                "loop" => "Loop",
                "pingpong" => "PingPong",
                _ => "Loop"
            };
        }

        private string ParseMovementType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "transform" => "Transform",
                "rigidbody" => "Rigidbody",
                "rigidbody2d" => "Rigidbody2D",
                _ => "Transform"
            };
        }

        private string ParseRotationMode(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "none" => "None",
                "lookattarget" => "LookAtTarget",
                "aligntopath" => "AlignToPath",
                _ => "None"
            };
        }

        #endregion
    }
}
