using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MCP.Editor
{
    internal static partial class McpCommandProcessor
    {
        #region UI (UGUI) Management

        private static object HandleUguiRectAdjust(Dictionary<string, object> payload)
        {
            // Check if compilation is in progress and wait if necessary
            var compilationWaitInfo = EnsureNoCompilationInProgress("uguiRectAdjust", maxWaitSeconds: 30f);

            try
            {
                var path = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
                Debug.Log($"[uguiRectAdjust] Processing: {path}");

                var target = ResolveGameObject(path);
                Debug.Log($"[uguiRectAdjust] GameObject resolved: {target.name}");

                var rectTransform = target.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    throw new InvalidOperationException("Target does not contain a RectTransform");
                }
                Debug.Log($"[uguiRectAdjust] RectTransform found");

                var canvas = rectTransform.GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    throw new InvalidOperationException("Target is not under a Canvas");
                }
                Debug.Log($"[uguiRectAdjust] Canvas found: {canvas.name}");

                var worldCorners = new Vector3[4];
                rectTransform.GetWorldCorners(worldCorners);
                Debug.Log($"[uguiRectAdjust] Got world corners");

                var width = Vector3.Distance(worldCorners[3], worldCorners[0]);
                var height = Vector3.Distance(worldCorners[1], worldCorners[0]);
                var scaleFactor = canvas.scaleFactor == 0f ? 1f : canvas.scaleFactor;
                var pixelWidth = width / scaleFactor;
                var pixelHeight = height / scaleFactor;
                Debug.Log($"[uguiRectAdjust] Calculated dimensions: {pixelWidth}x{pixelHeight}, scaleFactor: {scaleFactor}");

                var beforeAnchoredPosition = rectTransform.anchoredPosition;
                var beforeSizeDelta = rectTransform.sizeDelta;

                var before = new Dictionary<string, object>
                {
                    ["anchoredPosition"] = new Dictionary<string, object>
                    {
                        ["x"] = beforeAnchoredPosition.x,
                        ["y"] = beforeAnchoredPosition.y,
                    },
                    ["sizeDelta"] = new Dictionary<string, object>
                    {
                        ["x"] = beforeSizeDelta.x,
                        ["y"] = beforeSizeDelta.y,
                    },
                };

                rectTransform.sizeDelta = new Vector2(pixelWidth, pixelHeight);
                var afterAnchoredPosition = rectTransform.anchoredPosition;
                var afterSizeDelta = rectTransform.sizeDelta;

                EditorUtility.SetDirty(rectTransform);
                Debug.Log($"[uguiRectAdjust] Completed successfully");

                var result = new Dictionary<string, object>
                {
                    ["before"] = before,
                    ["after"] = new Dictionary<string, object>
                    {
                        ["anchoredPosition"] = new Dictionary<string, object>
                        {
                            ["x"] = afterAnchoredPosition.x,
                            ["y"] = afterAnchoredPosition.y,
                        },
                        ["sizeDelta"] = new Dictionary<string, object>
                        {
                            ["x"] = afterSizeDelta.x,
                            ["y"] = afterSizeDelta.y,
                        },
                    },
                    ["scaleFactor"] = scaleFactor,
                };

                // Add compilation wait info if we waited
                if (compilationWaitInfo != null)
                {
                    result["compilationWait"] = compilationWaitInfo;
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[uguiRectAdjust] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Handles RectTransform anchor manipulation operations.
        /// Supports setting anchors, converting between anchor-based and absolute positions,
        /// and adjusting positioning based on anchor changes.
        /// </summary>
        /// <param name="payload">Operation parameters including gameObjectPath and anchor settings.</param>
        /// <returns>Result dictionary with before/after anchor and position data.</returns>
        private static object HandleUguiAnchorManage(Dictionary<string, object> payload)
        {
            // Check if compilation is in progress and wait if necessary
            var compilationWaitInfo = EnsureNoCompilationInProgress("uguiAnchorManage", maxWaitSeconds: 30f);

            try
            {
                var path = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
                Debug.Log($"[uguiAnchorManage] Processing: {path}");

                var target = ResolveGameObject(path);
                var rectTransform = target.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    throw new InvalidOperationException("Target does not contain a RectTransform");
                }

                var canvas = rectTransform.GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    throw new InvalidOperationException("Target is not under a Canvas");
                }

                // Capture before state
                var beforeState = CaptureRectTransformState(rectTransform);

                // Get operation type
                var operation = GetString(payload, "operation");
                if (string.IsNullOrEmpty(operation))
                {
                    throw new InvalidOperationException("operation is required");
                }

                switch (operation)
                {
                    case "setAnchor":
                        SetAnchor(rectTransform, payload);
                        break;
                    case "setAnchorPreset":
                        SetAnchorPreset(rectTransform, payload);
                        break;
                    case "convertToAnchored":
                        ConvertToAnchoredPosition(rectTransform, payload);
                        break;
                    case "convertToAbsolute":
                        ConvertToAbsolutePosition(rectTransform, payload);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown uguiAnchorManage operation: {operation}");
                }

                EditorUtility.SetDirty(rectTransform);
                Debug.Log($"[uguiAnchorManage] Completed successfully");

                // Capture after state
                var afterState = CaptureRectTransformState(rectTransform);

                return new Dictionary<string, object>
                {
                    ["before"] = beforeState,
                    ["after"] = afterState,
                    ["operation"] = operation,
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[uguiAnchorManage] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Captures the current state of a RectTransform including anchors, positions, and size.
        /// </summary>
        private static Dictionary<string, object> CaptureRectTransformState(RectTransform rectTransform)
        {
            return new Dictionary<string, object>
            {
                ["anchorMin"] = new Dictionary<string, object>
                {
                    ["x"] = rectTransform.anchorMin.x,
                    ["y"] = rectTransform.anchorMin.y,
                },
                ["anchorMax"] = new Dictionary<string, object>
                {
                    ["x"] = rectTransform.anchorMax.x,
                    ["y"] = rectTransform.anchorMax.y,
                },
                ["anchoredPosition"] = new Dictionary<string, object>
                {
                    ["x"] = rectTransform.anchoredPosition.x,
                    ["y"] = rectTransform.anchoredPosition.y,
                },
                ["sizeDelta"] = new Dictionary<string, object>
                {
                    ["x"] = rectTransform.sizeDelta.x,
                    ["y"] = rectTransform.sizeDelta.y,
                },
                ["pivot"] = new Dictionary<string, object>
                {
                    ["x"] = rectTransform.pivot.x,
                    ["y"] = rectTransform.pivot.y,
                },
                ["offsetMin"] = new Dictionary<string, object>
                {
                    ["x"] = rectTransform.offsetMin.x,
                    ["y"] = rectTransform.offsetMin.y,
                },
                ["offsetMax"] = new Dictionary<string, object>
                {
                    ["x"] = rectTransform.offsetMax.x,
                    ["y"] = rectTransform.offsetMax.y,
                },
            };
        }

        /// <summary>
        /// Sets custom anchor values while preserving the visual position.
        /// </summary>
        private static void SetAnchor(RectTransform rectTransform, Dictionary<string, object> payload)
        {
            var preservePosition = GetBool(payload, "preservePosition", true);

            // Store current world corners if we need to preserve position
            Vector3[] corners = new Vector3[4];
            if (preservePosition)
            {
                rectTransform.GetWorldCorners(corners);
            }

            // Get anchor values
            var anchorMinX = GetFloat(payload, "anchorMinX");
            var anchorMinY = GetFloat(payload, "anchorMinY");
            var anchorMaxX = GetFloat(payload, "anchorMaxX");
            var anchorMaxY = GetFloat(payload, "anchorMaxY");

            if (anchorMinX.HasValue && anchorMinY.HasValue)
            {
                rectTransform.anchorMin = new Vector2(anchorMinX.Value, anchorMinY.Value);
            }
            if (anchorMaxX.HasValue && anchorMaxY.HasValue)
            {
                rectTransform.anchorMax = new Vector2(anchorMaxX.Value, anchorMaxY.Value);
            }

            // Restore world position by adjusting offsetMin/offsetMax
            if (preservePosition)
            {
                RestoreWorldCorners(rectTransform, corners);
            }
        }

        /// <summary>
        /// Restores the world corner positions of a RectTransform by adjusting offsetMin and offsetMax.
        /// This is used to preserve visual position when anchors are changed.
        /// </summary>
        private static void RestoreWorldCorners(RectTransform rectTransform, Vector3[] corners)
        {
            var parentRect = rectTransform.parent.GetComponent<RectTransform>();
            if (parentRect == null)
            {
                return;
            }

            // Convert world corners to parent local space
            // corners[0] = bottom-left, corners[2] = top-right
            Vector2 localCornerMin = parentRect.InverseTransformPoint(corners[0]);
            Vector2 localCornerMax = parentRect.InverseTransformPoint(corners[2]);

            // Calculate anchor positions in parent space
            Vector2 parentSize = parentRect.rect.size;
            Vector2 anchorMin = rectTransform.anchorMin;
            Vector2 anchorMax = rectTransform.anchorMax;

            Vector2 anchorPosMin = new Vector2(anchorMin.x * parentSize.x, anchorMin.y * parentSize.y);
            Vector2 anchorPosMax = new Vector2(anchorMax.x * parentSize.x, anchorMax.y * parentSize.y);

            // Set offsetMin and offsetMax to restore world corners
            // offsetMin = localCornerMin - anchorPosMin
            // offsetMax = localCornerMax - anchorPosMax
            rectTransform.offsetMin = localCornerMin - anchorPosMin;
            rectTransform.offsetMax = localCornerMax - anchorPosMax;
        }

        /// <summary>
        /// Sets anchor using common presets (e.g., top-left, center, stretch).
        /// </summary>
        private static void SetAnchorPreset(RectTransform rectTransform, Dictionary<string, object> payload)
        {
            var preset = GetString(payload, "preset");
            var preservePosition = GetBool(payload, "preservePosition", true);

            if (string.IsNullOrEmpty(preset))
            {
                throw new InvalidOperationException("preset is required");
            }

            // Store current corners if we need to preserve position
            Vector3[] corners = new Vector3[4];
            if (preservePosition)
            {
                rectTransform.GetWorldCorners(corners);
            }

            // Set anchor based on preset
            switch (preset.ToLower())
            {
                case "top-left":
                    rectTransform.anchorMin = new Vector2(0, 1);
                    rectTransform.anchorMax = new Vector2(0, 1);
                    break;
                case "top-center":
                    rectTransform.anchorMin = new Vector2(0.5f, 1);
                    rectTransform.anchorMax = new Vector2(0.5f, 1);
                    break;
                case "top-right":
                    rectTransform.anchorMin = new Vector2(1, 1);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    break;
                case "middle-left":
                    rectTransform.anchorMin = new Vector2(0, 0.5f);
                    rectTransform.anchorMax = new Vector2(0, 0.5f);
                    break;
                case "middle-center":
                case "center":
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    break;
                case "middle-right":
                    rectTransform.anchorMin = new Vector2(1, 0.5f);
                    rectTransform.anchorMax = new Vector2(1, 0.5f);
                    break;
                case "bottom-left":
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(0, 0);
                    break;
                case "bottom-center":
                    rectTransform.anchorMin = new Vector2(0.5f, 0);
                    rectTransform.anchorMax = new Vector2(0.5f, 0);
                    break;
                case "bottom-right":
                    rectTransform.anchorMin = new Vector2(1, 0);
                    rectTransform.anchorMax = new Vector2(1, 0);
                    break;
                case "stretch-horizontal":
                    rectTransform.anchorMin = new Vector2(0, 0.5f);
                    rectTransform.anchorMax = new Vector2(1, 0.5f);
                    break;
                case "stretch-vertical":
                    rectTransform.anchorMin = new Vector2(0.5f, 0);
                    rectTransform.anchorMax = new Vector2(0.5f, 1);
                    break;
                case "stretch-all":
                case "stretch":
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    break;
                case "stretch-top":
                    rectTransform.anchorMin = new Vector2(0, 1);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    break;
                case "stretch-middle":
                    rectTransform.anchorMin = new Vector2(0, 0.5f);
                    rectTransform.anchorMax = new Vector2(1, 0.5f);
                    break;
                case "stretch-bottom":
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(1, 0);
                    break;
                case "stretch-left":
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(0, 1);
                    break;
                case "stretch-center-vertical":
                    rectTransform.anchorMin = new Vector2(0.5f, 0);
                    rectTransform.anchorMax = new Vector2(0.5f, 1);
                    break;
                case "stretch-right":
                    rectTransform.anchorMin = new Vector2(1, 0);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown anchor preset: {preset}");
            }

            // Restore position if needed by adjusting offsetMin and offsetMax
            if (preservePosition)
            {
                RestoreWorldCorners(rectTransform, corners);
            }
        }

        /// <summary>
        /// Converts absolute position values to anchored position based on current anchors.
        /// </summary>
        private static void ConvertToAnchoredPosition(RectTransform rectTransform, Dictionary<string, object> payload)
        {
            var absoluteX = GetFloat(payload, "absoluteX");
            var absoluteY = GetFloat(payload, "absoluteY");

            var parentRect = rectTransform.parent.GetComponent<RectTransform>();
            if (parentRect == null)
            {
                throw new InvalidOperationException("Parent does not have a RectTransform");
            }

            var parentSize = parentRect.rect.size;
            var anchorMin = rectTransform.anchorMin;
            var anchorMax = rectTransform.anchorMax;
            var pivot = rectTransform.pivot;

            // Calculate anchor center in parent space
            var anchorCenter = new Vector2(
                (anchorMin.x + anchorMax.x) * 0.5f * parentSize.x,
                (anchorMin.y + anchorMax.y) * 0.5f * parentSize.y
            );

            // Calculate pivot offset
            var size = rectTransform.rect.size;
            var pivotOffset = new Vector2(
                (pivot.x - 0.5f) * size.x,
                (pivot.y - 0.5f) * size.y
            );

            // Convert absolute to anchored
            if (absoluteX.HasValue)
            {
                rectTransform.anchoredPosition = new Vector2(
                    absoluteX.Value - anchorCenter.x + pivotOffset.x,
                    rectTransform.anchoredPosition.y
                );
            }
            if (absoluteY.HasValue)
            {
                rectTransform.anchoredPosition = new Vector2(
                    rectTransform.anchoredPosition.x,
                    absoluteY.Value - anchorCenter.y + pivotOffset.y
                );
            }
        }

        /// <summary>
        /// Converts anchored position to absolute position in parent space.
        /// This is a read-only operation that returns the absolute position.
        /// </summary>
        private static void ConvertToAbsolutePosition(RectTransform _1, Dictionary<string, object> _2)
        {
            // This operation doesn't modify the transform, it just calculates values
            // The result will be returned in the "after" state which includes calculated absolute positions

            // Note: The actual absolute position calculation is implicit in Unity's RectTransform system
            // We just need to ensure the state is captured correctly
        }

        /// <summary>
        /// Unified UGUI management handler that consolidates all UGUI operations.
        /// Supports operations: rectAdjust, setAnchor, setAnchorPreset, convertToAnchored,
        /// convertToAbsolute, inspect, updateRect.
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation' type and target GameObject.</param>
        /// <returns>Result dictionary with operation-specific data.</returns>
        private static object HandleUguiManage(Dictionary<string, object> payload)
        {
            // Check if compilation is in progress and wait if necessary (except for inspect operations)
            var operation = GetString(payload, "operation");
            Dictionary<string, object> compilationWaitInfo = null;
            if (operation != "inspect")
            {
                compilationWaitInfo = EnsureNoCompilationInProgress("uguiManage", maxWaitSeconds: 30f);
            }

            try
            {
                if (string.IsNullOrEmpty(operation))
                {
                    throw new InvalidOperationException("operation is required");
                }

                var path = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
                Debug.Log($"[uguiManage] Processing operation '{operation}' on: {path}");

                var target = ResolveGameObject(path);
                var rectTransform = target.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    throw new InvalidOperationException("Target does not contain a RectTransform");
                }

                var canvas = rectTransform.GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    throw new InvalidOperationException("Target is not under a Canvas");
                }

                // Capture before state
                var beforeState = CaptureRectTransformState(rectTransform);

                object result;
                switch (operation)
                {
                    case "rectAdjust":
                        result = ExecuteRectAdjust(rectTransform, canvas, payload, beforeState);
                        break;
                    case "setAnchor":
                        SetAnchor(rectTransform, payload);
                        result = CreateStandardResult(beforeState, rectTransform, operation);
                        break;
                    case "setAnchorPreset":
                        SetAnchorPreset(rectTransform, payload);
                        result = CreateStandardResult(beforeState, rectTransform, operation);
                        break;
                    case "convertToAnchored":
                        ConvertToAnchoredPosition(rectTransform, payload);
                        result = CreateStandardResult(beforeState, rectTransform, operation);
                        break;
                    case "convertToAbsolute":
                        ConvertToAbsolutePosition(rectTransform, payload);
                        result = CreateStandardResult(beforeState, rectTransform, operation);
                        break;
                    case "inspect":
                        result = ExecuteInspect(rectTransform, canvas);
                        break;
                    case "updateRect":
                        ExecuteUpdateRect(rectTransform, payload);
                        result = CreateStandardResult(beforeState, rectTransform, operation);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown uguiManage operation: {operation}");
                }

                EditorUtility.SetDirty(rectTransform);
                Debug.Log($"[uguiManage] Completed successfully");

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[uguiManage] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Executes RectTransform size adjustment based on world corners.
        /// </summary>
        private static object ExecuteRectAdjust(RectTransform rectTransform, Canvas canvas,
            Dictionary<string, object> payload, Dictionary<string, object> beforeState)
        {
            var worldCorners = new Vector3[4];
            rectTransform.GetWorldCorners(worldCorners);

            var width = Vector3.Distance(worldCorners[3], worldCorners[0]);
            var height = Vector3.Distance(worldCorners[1], worldCorners[0]);
            var scaleFactor = canvas.scaleFactor == 0f ? 1f : canvas.scaleFactor;
            var pixelWidth = width / scaleFactor;
            var pixelHeight = height / scaleFactor;

            rectTransform.sizeDelta = new Vector2(pixelWidth, pixelHeight);

            return new Dictionary<string, object>
            {
                ["before"] = beforeState,
                ["after"] = CaptureRectTransformState(rectTransform),
                ["operation"] = "rectAdjust",
                ["scaleFactor"] = scaleFactor,
            };
        }

        /// <summary>
        /// Inspects current RectTransform state with detailed information.
        /// </summary>
        private static object ExecuteInspect(RectTransform rectTransform, Canvas canvas)
        {
            var state = CaptureRectTransformState(rectTransform);
            state["canvasName"] = canvas.name;
            state["scaleFactor"] = canvas.scaleFactor;

            // Add calculated world corners
            var worldCorners = new Vector3[4];
            rectTransform.GetWorldCorners(worldCorners);
            state["worldCorners"] = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["x"] = worldCorners[0].x, ["y"] = worldCorners[0].y, ["z"] = worldCorners[0].z },
                new Dictionary<string, object> { ["x"] = worldCorners[1].x, ["y"] = worldCorners[1].y, ["z"] = worldCorners[1].z },
                new Dictionary<string, object> { ["x"] = worldCorners[2].x, ["y"] = worldCorners[2].y, ["z"] = worldCorners[2].z },
                new Dictionary<string, object> { ["x"] = worldCorners[3].x, ["y"] = worldCorners[3].y, ["z"] = worldCorners[3].z },
            };

            // Add rect dimensions
            state["rectWidth"] = rectTransform.rect.width;
            state["rectHeight"] = rectTransform.rect.height;

            return new Dictionary<string, object>
            {
                ["state"] = state,
                ["operation"] = "inspect",
            };
        }

        /// <summary>
        /// Updates RectTransform properties from payload.
        /// </summary>
        private static void ExecuteUpdateRect(RectTransform rectTransform, Dictionary<string, object> payload)
        {
            // Update anchoredPosition - supports both dictionary format and individual fields
            if (payload.TryGetValue("anchoredPosition", out var anchoredPosObj) && anchoredPosObj is Dictionary<string, object> anchoredPosDict)
            {
                // Dictionary format: {"anchoredPosition": {"x": 100, "y": 200}}
                var pos = rectTransform.anchoredPosition;
                rectTransform.anchoredPosition = new Vector2(
                    GetFloat(anchoredPosDict, "x") ?? pos.x,
                    GetFloat(anchoredPosDict, "y") ?? pos.y
                );
            }
            else
            {
                // Individual fields format: {"anchoredPositionX": 100, "anchoredPositionY": 200}
                var anchoredPositionX = GetFloat(payload, "anchoredPositionX");
                var anchoredPositionY = GetFloat(payload, "anchoredPositionY");
                if (anchoredPositionX.HasValue || anchoredPositionY.HasValue)
                {
                    var pos = rectTransform.anchoredPosition;
                    rectTransform.anchoredPosition = new Vector2(
                        anchoredPositionX ?? pos.x,
                        anchoredPositionY ?? pos.y
                    );
                }
            }

            // Update sizeDelta - supports both dictionary format and individual fields
            if (payload.TryGetValue("sizeDelta", out var sizeDeltaObj) && sizeDeltaObj is Dictionary<string, object> sizeDeltaDict)
            {
                // Dictionary format: {"sizeDelta": {"x": 300, "y": 400}}
                var size = rectTransform.sizeDelta;
                rectTransform.sizeDelta = new Vector2(
                    GetFloat(sizeDeltaDict, "x") ?? size.x,
                    GetFloat(sizeDeltaDict, "y") ?? size.y
                );
            }
            else
            {
                // Individual fields format: {"sizeDeltaX": 300, "sizeDeltaY": 400}
                var sizeDeltaX = GetFloat(payload, "sizeDeltaX");
                var sizeDeltaY = GetFloat(payload, "sizeDeltaY");
                if (sizeDeltaX.HasValue || sizeDeltaY.HasValue)
                {
                    var size = rectTransform.sizeDelta;
                    rectTransform.sizeDelta = new Vector2(
                        sizeDeltaX ?? size.x,
                        sizeDeltaY ?? size.y
                    );
                }
            }

            // Update pivot - supports both dictionary format and individual fields
            if (payload.TryGetValue("pivot", out var pivotObj) && pivotObj is Dictionary<string, object> pivotDict)
            {
                // Dictionary format: {"pivot": {"x": 0.5, "y": 0.5}}
                var pivot = rectTransform.pivot;
                rectTransform.pivot = new Vector2(
                    GetFloat(pivotDict, "x") ?? pivot.x,
                    GetFloat(pivotDict, "y") ?? pivot.y
                );
            }
            else
            {
                // Individual fields format: {"pivotX": 0.5, "pivotY": 0.5}
                var pivotX = GetFloat(payload, "pivotX");
                var pivotY = GetFloat(payload, "pivotY");
                if (pivotX.HasValue || pivotY.HasValue)
                {
                    var pivot = rectTransform.pivot;
                    rectTransform.pivot = new Vector2(
                        pivotX ?? pivot.x,
                        pivotY ?? pivot.y
                    );
                }
            }

            // Update offsetMin - supports both dictionary format and individual fields
            if (payload.TryGetValue("offsetMin", out var offsetMinObj) && offsetMinObj is Dictionary<string, object> offsetMinDict)
            {
                // Dictionary format: {"offsetMin": {"x": 10, "y": 10}}
                var offset = rectTransform.offsetMin;
                rectTransform.offsetMin = new Vector2(
                    GetFloat(offsetMinDict, "x") ?? offset.x,
                    GetFloat(offsetMinDict, "y") ?? offset.y
                );
            }
            else
            {
                // Individual fields format: {"offsetMinX": 10, "offsetMinY": 10}
                var offsetMinX = GetFloat(payload, "offsetMinX");
                var offsetMinY = GetFloat(payload, "offsetMinY");
                if (offsetMinX.HasValue || offsetMinY.HasValue)
                {
                    var offset = rectTransform.offsetMin;
                    rectTransform.offsetMin = new Vector2(
                        offsetMinX ?? offset.x,
                        offsetMinY ?? offset.y
                    );
                }
            }

            // Update offsetMax - supports both dictionary format and individual fields
            if (payload.TryGetValue("offsetMax", out var offsetMaxObj) && offsetMaxObj is Dictionary<string, object> offsetMaxDict)
            {
                // Dictionary format: {"offsetMax": {"x": -10, "y": -10}}
                var offset = rectTransform.offsetMax;
                rectTransform.offsetMax = new Vector2(
                    GetFloat(offsetMaxDict, "x") ?? offset.x,
                    GetFloat(offsetMaxDict, "y") ?? offset.y
                );
            }
            else
            {
                // Individual fields format: {"offsetMaxX": -10, "offsetMaxY": -10}
                var offsetMaxX = GetFloat(payload, "offsetMaxX");
                var offsetMaxY = GetFloat(payload, "offsetMaxY");
                if (offsetMaxX.HasValue || offsetMaxY.HasValue)
                {
                    var offset = rectTransform.offsetMax;
                    rectTransform.offsetMax = new Vector2(
                        offsetMaxX ?? offset.x,
                        offsetMaxY ?? offset.y
                    );
                }
            }

            // Update anchorMin - supports both dictionary format and individual fields
            if (payload.TryGetValue("anchorMin", out var anchorMinObj) && anchorMinObj is Dictionary<string, object> anchorMinDict)
            {
                // Dictionary format: {"anchorMin": {"x": 0, "y": 0}}
                var anchor = rectTransform.anchorMin;
                rectTransform.anchorMin = new Vector2(
                    GetFloat(anchorMinDict, "x") ?? anchor.x,
                    GetFloat(anchorMinDict, "y") ?? anchor.y
                );
            }
            else
            {
                // Individual fields format: {"anchorMinX": 0, "anchorMinY": 0}
                var anchorMinX = GetFloat(payload, "anchorMinX");
                var anchorMinY = GetFloat(payload, "anchorMinY");
                if (anchorMinX.HasValue || anchorMinY.HasValue)
                {
                    var anchor = rectTransform.anchorMin;
                    rectTransform.anchorMin = new Vector2(
                        anchorMinX ?? anchor.x,
                        anchorMinY ?? anchor.y
                    );
                }
            }

            // Update anchorMax - supports both dictionary format and individual fields
            if (payload.TryGetValue("anchorMax", out var anchorMaxObj) && anchorMaxObj is Dictionary<string, object> anchorMaxDict)
            {
                // Dictionary format: {"anchorMax": {"x": 1, "y": 1}}
                var anchor = rectTransform.anchorMax;
                rectTransform.anchorMax = new Vector2(
                    GetFloat(anchorMaxDict, "x") ?? anchor.x,
                    GetFloat(anchorMaxDict, "y") ?? anchor.y
                );
            }
            else
            {
                // Individual fields format: {"anchorMaxX": 1, "anchorMaxY": 1}
                var anchorMaxX = GetFloat(payload, "anchorMaxX");
                var anchorMaxY = GetFloat(payload, "anchorMaxY");
                if (anchorMaxX.HasValue || anchorMaxY.HasValue)
                {
                    var anchor = rectTransform.anchorMax;
                    rectTransform.anchorMax = new Vector2(
                        anchorMaxX ?? anchor.x,
                        anchorMaxY ?? anchor.y
                    );
                }
            }
        }

        /// <summary>
        /// Creates a standard result with before/after state and operation name.
        /// </summary>
        private static Dictionary<string, object> CreateStandardResult(
            Dictionary<string, object> beforeState, RectTransform rectTransform, string operation)
        {
            return new Dictionary<string, object>
            {
                ["before"] = beforeState,
                ["after"] = CaptureRectTransformState(rectTransform),
                ["operation"] = operation,
            };
        }

        /// <summary>
        /// Creates UI elements from templates (Button, Text, Image, Panel, ScrollView, InputField, Slider, Toggle, Dropdown).
        /// Each template automatically includes necessary components and sensible defaults.
        /// </summary>
        /// <param name="payload">Template parameters including 'template' type, name, parentPath, size, position, etc.</param>
        /// <returns>Result dictionary with created GameObject information.</returns>
        private static object HandleUguiCreateFromTemplate(Dictionary<string, object> payload)
        {
            // Check if compilation is in progress and wait if necessary
            var compilationWaitInfo = EnsureNoCompilationInProgress("uguiCreateFromTemplate", maxWaitSeconds: 30f);

            try
            {
                var template = GetString(payload, "template");
                if (string.IsNullOrEmpty(template))
                {
                    throw new InvalidOperationException("template is required");
                }

                Debug.Log($"[uguiCreateFromTemplate] Creating template: {template}");

                // Get parent path or find first Canvas
                var parentPath = GetString(payload, "parentPath");
                GameObject parent = null;
                if (!string.IsNullOrEmpty(parentPath))
                {
                    parent = ResolveGameObject(parentPath);
                }
                else
                {
                    // Find first Canvas in the scene
                    var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
                    if (canvas != null)
                    {
                        parent = canvas.gameObject;
                    }
                    else
                    {
                        throw new InvalidOperationException("No Canvas found in scene. Please specify parentPath or create a Canvas first.");
                    }
                }

                // Verify parent is under a Canvas
                if (parent.GetComponentInParent<Canvas>() == null)
                {
                    throw new InvalidOperationException("Parent must be under a Canvas");
                }

                // Get name or use template as default
                var name = GetString(payload, "name");
                if (string.IsNullOrEmpty(name))
                {
                    name = template;
                }

                // Create the GameObject based on template
                GameObject go = null;
                switch (template)
                {
                    case "Button":
                        go = CreateButtonTemplate(name, parent, payload);
                        break;
                    case "Text":
                        go = CreateTextTemplate(name, parent, payload);
                        break;
                    case "Image":
                        go = CreateImageTemplate(name, parent, payload);
                        break;
                    case "RawImage":
                        go = CreateRawImageTemplate(name, parent, payload);
                        break;
                    case "Panel":
                        go = CreatePanelTemplate(name, parent, payload);
                        break;
                    case "ScrollView":
                        go = CreateScrollViewTemplate(name, parent, payload);
                        break;
                    case "InputField":
                        go = CreateInputFieldTemplate(name, parent, payload);
                        break;
                    case "Slider":
                        go = CreateSliderTemplate(name, parent, payload);
                        break;
                    case "Toggle":
                        go = CreateToggleTemplate(name, parent, payload);
                        break;
                    case "Dropdown":
                        go = CreateDropdownTemplate(name, parent, payload);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown template: {template}");
                }

                Undo.RegisterCreatedObjectUndo(go, $"Create {template}");
                Selection.activeGameObject = go;

                Debug.Log($"[uguiCreateFromTemplate] Created {template}: {GetHierarchyPath(go)}");

                return new Dictionary<string, object>
                {
                    ["template"] = template,
                    ["gameObjectPath"] = GetHierarchyPath(go),
                    ["name"] = go.name,
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[uguiCreateFromTemplate] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Creates a text component (UI.Text or TextMeshPro) based on the useTextMeshPro flag.
        /// </summary>
        private static Component CreateTextComponent(GameObject textGo, Dictionary<string, object> payload, string defaultText, int defaultFontSize)
        {
            var useTextMeshPro = GetBool(payload, "useTextMeshPro", false);
            var textContent = GetString(payload, "text") ?? defaultText;
            var fontSize = GetInt(payload, "fontSize", defaultFontSize);

            if (useTextMeshPro)
            {
                // Try to use TextMeshPro (TMPro)
                var tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
                if (tmpType == null)
                {
                    Debug.LogWarning("[CreateTextComponent] TextMeshPro package not found. Falling back to UI.Text. Install TextMeshPro package to use TMP components.");
                    useTextMeshPro = false;
                }
                else
                {
                    var tmpComponent = textGo.AddComponent(tmpType);

                    // Set text property
                    var textProp = tmpType.GetProperty("text");
                    if (textProp != null)
                    {
                        textProp.SetValue(tmpComponent, textContent);
                    }

                    // Set fontSize property
                    var fontSizeProp = tmpType.GetProperty("fontSize");
                    if (fontSizeProp != null)
                    {
                        fontSizeProp.SetValue(tmpComponent, (float)fontSize);
                    }

                    // Set alignment property
                    var alignmentProp = tmpType.GetProperty("alignment");
                    if (alignmentProp != null)
                    {
                        // TextAlignmentOptions.Center = 514
                        var alignmentType = Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
                        if (alignmentType != null)
                        {
                            alignmentProp.SetValue(tmpComponent, Enum.ToObject(alignmentType, 514));
                        }
                    }

                    // Set color property
                    var colorProp = tmpType.GetProperty("color");
                    if (colorProp != null)
                    {
                        colorProp.SetValue(tmpComponent, Color.black);
                    }

                    return tmpComponent;
                }
            }

            // Use standard UI.Text
            var text = textGo.AddComponent<UnityEngine.UI.Text>();
            text.text = textContent;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            return text;
        }

        private static GameObject CreateButtonTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(1f, 1f, 1f, 1f);

            var button = go.AddComponent<UnityEngine.UI.Button>();
            button.interactable = GetBool(payload, "interactable", true);

            // Create Text child
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            CreateTextComponent(textGo, payload, "Button", 14);

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 160, 30);

            return go;
        }

        private static GameObject CreateTextTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            CreateTextComponent(go, payload, "New Text", 14);

            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 160, 30);

            return go;
        }

        private static GameObject CreateImageTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.white;

            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 100, 100);

            return go;
        }

        private static GameObject CreateRawImageTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var rawImage = go.AddComponent<UnityEngine.UI.RawImage>();
            rawImage.color = Color.white;

            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 100, 100);

            return go;
        }

        private static GameObject CreatePanelTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(1f, 1f, 1f, 0.392f);

            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 200, 200);

            return go;
        }

        private static GameObject CreateScrollViewTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            // Create main ScrollView GameObject
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var scrollRect = go.AddComponent<UnityEngine.UI.ScrollRect>();
            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(1f, 1f, 1f, 1f);

            // Create Viewport
            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(go.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewport.AddComponent<UnityEngine.UI.Mask>();
            var viewportImage = viewport.AddComponent<UnityEngine.UI.Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);

            // Create Content
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 300);

            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 200, 200);

            return go;
        }

        private static GameObject CreateInputFieldTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.white;

            var inputField = go.AddComponent<UnityEngine.UI.InputField>();
            inputField.interactable = GetBool(payload, "interactable", true);

            // Create Text child
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<UnityEngine.UI.Text>();
            text.text = GetString(payload, "text") ?? "";
            text.fontSize = GetInt(payload, "fontSize", 14);
            text.color = Color.black;
            text.supportRichText = false;

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 6);
            textRect.offsetMax = new Vector2(-10, -7);

            // Create Placeholder child
            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
            placeholderGo.transform.SetParent(go.transform, false);
            var placeholder = placeholderGo.AddComponent<UnityEngine.UI.Text>();
            placeholder.text = "Enter text...";
            placeholder.fontSize = GetInt(payload, "fontSize", 14);
            placeholder.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            placeholder.fontStyle = FontStyle.Italic;

            var placeholderRect = placeholderGo.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(10, 6);
            placeholderRect.offsetMax = new Vector2(-10, -7);

            inputField.textComponent = text;
            inputField.placeholder = placeholder;

            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 160, 30);

            return go;
        }

        private static GameObject CreateSliderTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var slider = go.AddComponent<UnityEngine.UI.Slider>();
            slider.interactable = GetBool(payload, "interactable", true);

            // Create Background
            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(go.transform, false);
            var bgImage = bgGo.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.sizeDelta = new Vector2(0, 0);

            // Create Fill Area
            var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGo.transform.SetParent(go.transform, false);
            var fillAreaRect = fillAreaGo.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);

            // Create Fill
            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillImage = fillGo.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = new Color(0.5f, 0.8f, 1f, 1f);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.sizeDelta = new Vector2(10, 0);

            // Create Handle Slide Area
            var handleAreaGo = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleAreaGo.transform.SetParent(go.transform, false);
            var handleAreaRect = handleAreaGo.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = new Vector2(0, 0);
            handleAreaRect.anchorMax = new Vector2(1, 1);
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);

            // Create Handle
            var handleGo = new GameObject("Handle", typeof(RectTransform));
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            var handleImage = handleGo.AddComponent<UnityEngine.UI.Image>();
            handleImage.color = Color.white;
            var handleRect = handleGo.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;

            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 160, 20);

            return go;
        }

        private static GameObject CreateToggleTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var toggle = go.AddComponent<UnityEngine.UI.Toggle>();
            toggle.interactable = GetBool(payload, "interactable", true);

            // Create Background
            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(go.transform, false);
            var bgImage = bgGo.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = Color.white;
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.5f);
            bgRect.anchorMax = new Vector2(0, 0.5f);
            bgRect.anchoredPosition = new Vector2(10, 0);
            bgRect.sizeDelta = new Vector2(20, 20);

            // Create Checkmark
            var checkGo = new GameObject("Checkmark", typeof(RectTransform));
            checkGo.transform.SetParent(bgGo.transform, false);
            var checkImage = checkGo.AddComponent<UnityEngine.UI.Image>();
            checkImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);
            var checkRect = checkGo.GetComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.sizeDelta = Vector2.zero;

            // Create Label
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var label = labelGo.AddComponent<UnityEngine.UI.Text>();
            label.text = GetString(payload, "text") ?? "Toggle";
            label.fontSize = GetInt(payload, "fontSize", 14);
            label.color = Color.black;
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(23, 0);
            labelRect.offsetMax = new Vector2(0, 0);

            toggle.graphic = checkImage;
            toggle.targetGraphic = bgImage;

            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 160, 20);

            return go;
        }

        private static GameObject CreateDropdownTemplate(string name, GameObject parent, Dictionary<string, object> payload)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.white;

            var dropdown = go.AddComponent<UnityEngine.UI.Dropdown>();
            dropdown.interactable = GetBool(payload, "interactable", true);

            // Create Label
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var label = labelGo.AddComponent<UnityEngine.UI.Text>();
            label.text = GetString(payload, "text") ?? "Option A";
            label.fontSize = GetInt(payload, "fontSize", 14);
            label.color = Color.black;
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10, 6);
            labelRect.offsetMax = new Vector2(-25, -7);

            // Create Arrow
            var arrowGo = new GameObject("Arrow", typeof(RectTransform));
            arrowGo.transform.SetParent(go.transform, false);
            var arrow = arrowGo.AddComponent<UnityEngine.UI.Text>();
            arrow.text = "▼";
            arrow.fontSize = GetInt(payload, "fontSize", 14);
            arrow.color = Color.black;
            arrow.alignment = TextAnchor.MiddleCenter;
            var arrowRect = arrowGo.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0);
            arrowRect.anchorMax = new Vector2(1, 1);
            arrowRect.sizeDelta = new Vector2(20, 0);
            arrowRect.anchoredPosition = new Vector2(-15, 0);

            // Create Template (simplified version)
            var templateGo = new GameObject("Template", typeof(RectTransform));
            templateGo.transform.SetParent(go.transform, false);
            var templateRect = templateGo.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = new Vector2(0, 2);
            templateRect.sizeDelta = new Vector2(0, 150);
            templateGo.SetActive(false);

            dropdown.captionText = label;
            dropdown.template = templateRect;

            ApplyCommonRectTransformSettings(go.GetComponent<RectTransform>(), payload, 160, 30);

            return go;
        }

        private static void ApplyCommonRectTransformSettings(RectTransform rectTransform, Dictionary<string, object> payload, float defaultWidth, float defaultHeight)
        {
            // Apply anchor preset
            var anchorPreset = GetString(payload, "anchorPreset") ?? "center";
            var presetPayload = new Dictionary<string, object>
            {
                ["preset"] = anchorPreset,
                ["preservePosition"] = false
            };
            SetAnchorPreset(rectTransform, presetPayload);

            // Apply size (with validation to prevent negative values)
            var width = GetFloat(payload, "width") ?? defaultWidth;
            var height = GetFloat(payload, "height") ?? defaultHeight;

            if (width < 0)
            {
                Debug.LogWarning($"[ApplyCommonRectTransformSettings] Width cannot be negative. Clamping {width} to 0.");
                width = 0;
            }

            if (height < 0)
            {
                Debug.LogWarning($"[ApplyCommonRectTransformSettings] Height cannot be negative. Clamping {height} to 0.");
                height = 0;
            }

            rectTransform.sizeDelta = new Vector2(width, height);

            // Apply position
            var posX = GetFloat(payload, "positionX") ?? 0f;
            var posY = GetFloat(payload, "positionY") ?? 0f;
            rectTransform.anchoredPosition = new Vector2(posX, posY);
        }

        /// <summary>
        /// Manages layout components (HorizontalLayoutGroup, VerticalLayoutGroup, GridLayoutGroup,
        /// ContentSizeFitter, LayoutElement, AspectRatioFitter) on UI GameObjects.
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation', 'gameObjectPath', 'layoutType', and layout-specific settings.</param>
        /// <returns>Result dictionary with operation-specific data.</returns>
        private static object HandleUguiLayoutManage(Dictionary<string, object> payload)
        {
            // Check if compilation is in progress and wait if necessary
            var compilationWaitInfo = EnsureNoCompilationInProgress("uguiLayoutManage", maxWaitSeconds: 30f);

            try
            {
                var operation = GetString(payload, "operation");
                if (string.IsNullOrEmpty(operation))
                {
                    throw new InvalidOperationException("operation is required");
                }

                var path = GetString(payload, "gameObjectPath");
                if (string.IsNullOrEmpty(path))
                {
                    throw new InvalidOperationException("gameObjectPath is required");
                }

                Debug.Log($"[uguiLayoutManage] Processing operation '{operation}' on: {path}");

                var go = ResolveGameObject(path);

                object result;
                switch (operation)
                {
                    case "add":
                        result = AddLayoutComponent(go, payload);
                        break;
                    case "update":
                        result = UpdateLayoutComponent(go, payload);
                        break;
                    case "remove":
                        result = RemoveLayoutComponent(go, payload);
                        break;
                    case "inspect":
                        result = InspectLayoutComponent(go, payload);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown uguiLayoutManage operation: {operation}");
                }

                EditorUtility.SetDirty(go);
                Debug.Log($"[uguiLayoutManage] Completed successfully");

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[uguiLayoutManage] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private static object AddLayoutComponent(GameObject go, Dictionary<string, object> payload)
        {
            var layoutType = GetString(payload, "layoutType");
            if (string.IsNullOrEmpty(layoutType))
            {
                throw new InvalidOperationException("layoutType is required for add operation");
            }

            Component component;
            switch (layoutType)
            {
                case "HorizontalLayoutGroup":
                    component = go.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                    ApplyLayoutGroupSettings(component, payload);
                    break;
                case "VerticalLayoutGroup":
                    component = go.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                    ApplyLayoutGroupSettings(component, payload);
                    break;
                case "GridLayoutGroup":
                    component = go.AddComponent<UnityEngine.UI.GridLayoutGroup>();
                    ApplyGridLayoutGroupSettings((UnityEngine.UI.GridLayoutGroup)component, payload);
                    break;
                case "ContentSizeFitter":
                    component = go.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                    ApplyContentSizeFitterSettings((UnityEngine.UI.ContentSizeFitter)component, payload);
                    break;
                case "LayoutElement":
                    component = go.AddComponent<UnityEngine.UI.LayoutElement>();
                    ApplyLayoutElementSettings((UnityEngine.UI.LayoutElement)component, payload);
                    break;
                case "AspectRatioFitter":
                    component = go.AddComponent<UnityEngine.UI.AspectRatioFitter>();
                    ApplyAspectRatioFitterSettings((UnityEngine.UI.AspectRatioFitter)component, payload);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown layoutType: {layoutType}");
            }

            return new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["layoutType"] = layoutType,
                ["gameObjectPath"] = GetHierarchyPath(go),
            };
        }

        private static object UpdateLayoutComponent(GameObject go, Dictionary<string, object> payload)
        {
            var layoutType = GetString(payload, "layoutType");
            if (string.IsNullOrEmpty(layoutType))
            {
                throw new InvalidOperationException("layoutType is required for update operation");
            }

            Component component = null;
            switch (layoutType)
            {
                case "HorizontalLayoutGroup":
                    component = go.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                    if (component != null) ApplyLayoutGroupSettings(component, payload);
                    break;
                case "VerticalLayoutGroup":
                    component = go.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                    if (component != null) ApplyLayoutGroupSettings(component, payload);
                    break;
                case "GridLayoutGroup":
                    component = go.GetComponent<UnityEngine.UI.GridLayoutGroup>();
                    if (component != null) ApplyGridLayoutGroupSettings((UnityEngine.UI.GridLayoutGroup)component, payload);
                    break;
                case "ContentSizeFitter":
                    component = go.GetComponent<UnityEngine.UI.ContentSizeFitter>();
                    if (component != null) ApplyContentSizeFitterSettings((UnityEngine.UI.ContentSizeFitter)component, payload);
                    break;
                case "LayoutElement":
                    component = go.GetComponent<UnityEngine.UI.LayoutElement>();
                    if (component != null) ApplyLayoutElementSettings((UnityEngine.UI.LayoutElement)component, payload);
                    break;
                case "AspectRatioFitter":
                    component = go.GetComponent<UnityEngine.UI.AspectRatioFitter>();
                    if (component != null) ApplyAspectRatioFitterSettings((UnityEngine.UI.AspectRatioFitter)component, payload);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown layoutType: {layoutType}");
            }

            if (component == null)
            {
                throw new InvalidOperationException($"Component {layoutType} not found on GameObject");
            }

            return new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["layoutType"] = layoutType,
                ["gameObjectPath"] = GetHierarchyPath(go),
            };
        }

        private static object RemoveLayoutComponent(GameObject go, Dictionary<string, object> payload)
        {
            var layoutType = GetString(payload, "layoutType");
            if (string.IsNullOrEmpty(layoutType))
            {
                throw new InvalidOperationException("layoutType is required for remove operation");
            }

            Component component = null;
            switch (layoutType)
            {
                case "HorizontalLayoutGroup":
                    component = go.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                    break;
                case "VerticalLayoutGroup":
                    component = go.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                    break;
                case "GridLayoutGroup":
                    component = go.GetComponent<UnityEngine.UI.GridLayoutGroup>();
                    break;
                case "ContentSizeFitter":
                    component = go.GetComponent<UnityEngine.UI.ContentSizeFitter>();
                    break;
                case "LayoutElement":
                    component = go.GetComponent<UnityEngine.UI.LayoutElement>();
                    break;
                case "AspectRatioFitter":
                    component = go.GetComponent<UnityEngine.UI.AspectRatioFitter>();
                    break;
                default:
                    throw new InvalidOperationException($"Unknown layoutType: {layoutType}");
            }

            if (component == null)
            {
                throw new InvalidOperationException($"Component {layoutType} not found on GameObject");
            }

            UnityEngine.Object.DestroyImmediate(component);

            return new Dictionary<string, object>
            {
                ["operation"] = "remove",
                ["layoutType"] = layoutType,
                ["gameObjectPath"] = GetHierarchyPath(go),
            };
        }

        private static object InspectLayoutComponent(GameObject go, Dictionary<string, object> payload)
        {
            var layoutType = GetString(payload, "layoutType");
            var result = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = GetHierarchyPath(go),
                ["layouts"] = new List<object>(),
            };

            var layouts = new List<object>();

            if (string.IsNullOrEmpty(layoutType))
            {
                // Inspect all layout components if layoutType not specified
                var hlg = go.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                if (hlg != null) layouts.Add(SerializeLayoutGroup(hlg, "HorizontalLayoutGroup"));

                var vlg = go.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                if (vlg != null) layouts.Add(SerializeLayoutGroup(vlg, "VerticalLayoutGroup"));

                var glg = go.GetComponent<UnityEngine.UI.GridLayoutGroup>();
                if (glg != null) layouts.Add(SerializeGridLayoutGroup(glg));

                var csf = go.GetComponent<UnityEngine.UI.ContentSizeFitter>();
                if (csf != null) layouts.Add(SerializeContentSizeFitter(csf));

                var le = go.GetComponent<UnityEngine.UI.LayoutElement>();
                if (le != null) layouts.Add(SerializeLayoutElement(le));

                var arf = go.GetComponent<UnityEngine.UI.AspectRatioFitter>();
                if (arf != null) layouts.Add(SerializeAspectRatioFitter(arf));
            }
            else
            {
                // Inspect specific layout type
                switch (layoutType)
                {
                    case "HorizontalLayoutGroup":
                        var hlg = go.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                        if (hlg != null) layouts.Add(SerializeLayoutGroup(hlg, "HorizontalLayoutGroup"));
                        break;
                    case "VerticalLayoutGroup":
                        var vlg = go.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                        if (vlg != null) layouts.Add(SerializeLayoutGroup(vlg, "VerticalLayoutGroup"));
                        break;
                    case "GridLayoutGroup":
                        var glg = go.GetComponent<UnityEngine.UI.GridLayoutGroup>();
                        if (glg != null) layouts.Add(SerializeGridLayoutGroup(glg));
                        break;
                    case "ContentSizeFitter":
                        var csf = go.GetComponent<UnityEngine.UI.ContentSizeFitter>();
                        if (csf != null) layouts.Add(SerializeContentSizeFitter(csf));
                        break;
                    case "LayoutElement":
                        var le = go.GetComponent<UnityEngine.UI.LayoutElement>();
                        if (le != null) layouts.Add(SerializeLayoutElement(le));
                        break;
                    case "AspectRatioFitter":
                        var arf = go.GetComponent<UnityEngine.UI.AspectRatioFitter>();
                        if (arf != null) layouts.Add(SerializeAspectRatioFitter(arf));
                        break;
                }
            }

            result["layouts"] = layouts;
            return result;
        }

        private static void ApplyLayoutGroupSettings(Component component, Dictionary<string, object> payload)
        {
            var layoutGroup = component as UnityEngine.UI.HorizontalOrVerticalLayoutGroup;
            if (layoutGroup == null) return;

            // Apply padding
            if (payload.ContainsKey("padding"))
            {
                var paddingDict = payload["padding"] as Dictionary<string, object>;
                if (paddingDict != null)
                {
                    layoutGroup.padding = new RectOffset(
                        GetInt(paddingDict, "left", layoutGroup.padding.left),
                        GetInt(paddingDict, "right", layoutGroup.padding.right),
                        GetInt(paddingDict, "top", layoutGroup.padding.top),
                        GetInt(paddingDict, "bottom", layoutGroup.padding.bottom)
                    );
                }
            }

            // Apply spacing
            var spacing = GetFloat(payload, "spacing");
            if (spacing.HasValue) layoutGroup.spacing = spacing.Value;

            // Apply childAlignment
            var childAlignment = GetString(payload, "childAlignment");
            if (!string.IsNullOrEmpty(childAlignment))
            {
                layoutGroup.childAlignment = (TextAnchor)System.Enum.Parse(typeof(TextAnchor), childAlignment);
            }

            // Apply child control settings
            if (payload.ContainsKey("childControlWidth"))
                layoutGroup.childControlWidth = GetBool(payload, "childControlWidth");
            if (payload.ContainsKey("childControlHeight"))
                layoutGroup.childControlHeight = GetBool(payload, "childControlHeight");
            if (payload.ContainsKey("childForceExpandWidth"))
                layoutGroup.childForceExpandWidth = GetBool(payload, "childForceExpandWidth");
            if (payload.ContainsKey("childForceExpandHeight"))
                layoutGroup.childForceExpandHeight = GetBool(payload, "childForceExpandHeight");
        }

        private static void ApplyGridLayoutGroupSettings(UnityEngine.UI.GridLayoutGroup grid, Dictionary<string, object> payload)
        {
            // Apply common layout group settings
            if (payload.ContainsKey("padding"))
            {
                var paddingDict = payload["padding"] as Dictionary<string, object>;
                if (paddingDict != null)
                {
                    grid.padding = new RectOffset(
                        GetInt(paddingDict, "left", grid.padding.left),
                        GetInt(paddingDict, "right", grid.padding.right),
                        GetInt(paddingDict, "top", grid.padding.top),
                        GetInt(paddingDict, "bottom", grid.padding.bottom)
                    );
                }
            }

            var childAlignment = GetString(payload, "childAlignment");
            if (!string.IsNullOrEmpty(childAlignment))
            {
                grid.childAlignment = (TextAnchor)System.Enum.Parse(typeof(TextAnchor), childAlignment);
            }

            // Apply grid-specific settings
            var cellSizeX = GetFloat(payload, "cellSizeX");
            var cellSizeY = GetFloat(payload, "cellSizeY");
            if (cellSizeX.HasValue || cellSizeY.HasValue)
            {
                grid.cellSize = new Vector2(
                    cellSizeX ?? grid.cellSize.x,
                    cellSizeY ?? grid.cellSize.y
                );
            }

            var spacingX = GetFloat(payload, "spacing");
            var spacingY = GetFloat(payload, "spacingY");
            if (spacingX.HasValue || spacingY.HasValue)
            {
                grid.spacing = new Vector2(
                    spacingX ?? grid.spacing.x,
                    spacingY ?? grid.spacing.y
                );
            }

            var constraint = GetString(payload, "constraint");
            if (!string.IsNullOrEmpty(constraint))
            {
                grid.constraint = (UnityEngine.UI.GridLayoutGroup.Constraint)System.Enum.Parse(typeof(UnityEngine.UI.GridLayoutGroup.Constraint), constraint);
            }

            var constraintCount = GetInt(payload, "constraintCount", -1);
            if (constraintCount >= 0)
            {
                grid.constraintCount = constraintCount;
            }

            var startCorner = GetString(payload, "startCorner");
            if (!string.IsNullOrEmpty(startCorner))
            {
                grid.startCorner = (UnityEngine.UI.GridLayoutGroup.Corner)System.Enum.Parse(typeof(UnityEngine.UI.GridLayoutGroup.Corner), startCorner);
            }

            var startAxis = GetString(payload, "startAxis");
            if (!string.IsNullOrEmpty(startAxis))
            {
                grid.startAxis = (UnityEngine.UI.GridLayoutGroup.Axis)System.Enum.Parse(typeof(UnityEngine.UI.GridLayoutGroup.Axis), startAxis);
            }
        }

        private static void ApplyContentSizeFitterSettings(UnityEngine.UI.ContentSizeFitter fitter, Dictionary<string, object> payload)
        {
            var horizontalFit = GetString(payload, "horizontalFit");
            if (!string.IsNullOrEmpty(horizontalFit))
            {
                fitter.horizontalFit = (UnityEngine.UI.ContentSizeFitter.FitMode)System.Enum.Parse(typeof(UnityEngine.UI.ContentSizeFitter.FitMode), horizontalFit);
            }

            var verticalFit = GetString(payload, "verticalFit");
            if (!string.IsNullOrEmpty(verticalFit))
            {
                fitter.verticalFit = (UnityEngine.UI.ContentSizeFitter.FitMode)System.Enum.Parse(typeof(UnityEngine.UI.ContentSizeFitter.FitMode), verticalFit);
            }
        }

        private static void ApplyLayoutElementSettings(UnityEngine.UI.LayoutElement element, Dictionary<string, object> payload)
        {
            var minWidth = GetFloat(payload, "minWidth");
            if (minWidth.HasValue) element.minWidth = minWidth.Value;

            var minHeight = GetFloat(payload, "minHeight");
            if (minHeight.HasValue) element.minHeight = minHeight.Value;

            var preferredWidth = GetFloat(payload, "preferredWidth");
            if (preferredWidth.HasValue) element.preferredWidth = preferredWidth.Value;

            var preferredHeight = GetFloat(payload, "preferredHeight");
            if (preferredHeight.HasValue) element.preferredHeight = preferredHeight.Value;

            var flexibleWidth = GetFloat(payload, "flexibleWidth");
            if (flexibleWidth.HasValue) element.flexibleWidth = flexibleWidth.Value;

            var flexibleHeight = GetFloat(payload, "flexibleHeight");
            if (flexibleHeight.HasValue) element.flexibleHeight = flexibleHeight.Value;

            if (payload.ContainsKey("ignoreLayout"))
                element.ignoreLayout = GetBool(payload, "ignoreLayout");
        }

        private static void ApplyAspectRatioFitterSettings(UnityEngine.UI.AspectRatioFitter fitter, Dictionary<string, object> payload)
        {
            var aspectMode = GetString(payload, "aspectMode");
            if (!string.IsNullOrEmpty(aspectMode))
            {
                fitter.aspectMode = (UnityEngine.UI.AspectRatioFitter.AspectMode)System.Enum.Parse(typeof(UnityEngine.UI.AspectRatioFitter.AspectMode), aspectMode);
            }

            var aspectRatio = GetFloat(payload, "aspectRatio");
            if (aspectRatio.HasValue) fitter.aspectRatio = aspectRatio.Value;
        }

        private static Dictionary<string, object> SerializeLayoutGroup(Component component, string typeName)
        {
            var layoutGroup = component as UnityEngine.UI.HorizontalOrVerticalLayoutGroup;
            return new Dictionary<string, object>
            {
                ["type"] = typeName,
                ["padding"] = new Dictionary<string, object>
                {
                    ["left"] = layoutGroup.padding.left,
                    ["right"] = layoutGroup.padding.right,
                    ["top"] = layoutGroup.padding.top,
                    ["bottom"] = layoutGroup.padding.bottom,
                },
                ["spacing"] = layoutGroup.spacing,
                ["childAlignment"] = layoutGroup.childAlignment.ToString(),
                ["childControlWidth"] = layoutGroup.childControlWidth,
                ["childControlHeight"] = layoutGroup.childControlHeight,
                ["childForceExpandWidth"] = layoutGroup.childForceExpandWidth,
                ["childForceExpandHeight"] = layoutGroup.childForceExpandHeight,
            };
        }

        private static Dictionary<string, object> SerializeGridLayoutGroup(UnityEngine.UI.GridLayoutGroup grid)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "GridLayoutGroup",
                ["padding"] = new Dictionary<string, object>
                {
                    ["left"] = grid.padding.left,
                    ["right"] = grid.padding.right,
                    ["top"] = grid.padding.top,
                    ["bottom"] = grid.padding.bottom,
                },
                ["cellSize"] = new Dictionary<string, object>
                {
                    ["x"] = grid.cellSize.x,
                    ["y"] = grid.cellSize.y,
                },
                ["spacing"] = new Dictionary<string, object>
                {
                    ["x"] = grid.spacing.x,
                    ["y"] = grid.spacing.y,
                },
                ["childAlignment"] = grid.childAlignment.ToString(),
                ["constraint"] = grid.constraint.ToString(),
                ["constraintCount"] = grid.constraintCount,
                ["startCorner"] = grid.startCorner.ToString(),
                ["startAxis"] = grid.startAxis.ToString(),
            };
        }

        private static Dictionary<string, object> SerializeContentSizeFitter(UnityEngine.UI.ContentSizeFitter fitter)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "ContentSizeFitter",
                ["horizontalFit"] = fitter.horizontalFit.ToString(),
                ["verticalFit"] = fitter.verticalFit.ToString(),
            };
        }

        private static Dictionary<string, object> SerializeLayoutElement(UnityEngine.UI.LayoutElement element)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "LayoutElement",
                ["minWidth"] = element.minWidth,
                ["minHeight"] = element.minHeight,
                ["preferredWidth"] = element.preferredWidth,
                ["preferredHeight"] = element.preferredHeight,
                ["flexibleWidth"] = element.flexibleWidth,
                ["flexibleHeight"] = element.flexibleHeight,
                ["ignoreLayout"] = element.ignoreLayout,
            };
        }

        private static Dictionary<string, object> SerializeAspectRatioFitter(UnityEngine.UI.AspectRatioFitter fitter)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "AspectRatioFitter",
                ["aspectMode"] = fitter.aspectMode.ToString(),
                ["aspectRatio"] = fitter.aspectRatio,
            };
        }

        /// <summary>
        /// Detects overlapping UI elements in the scene.
        /// Can check a specific GameObject for overlaps with others, or check all UI elements for overlaps with each other.
        /// </summary>
        /// <param name="payload">Detection parameters including 'gameObjectPath', 'checkAll', 'includeChildren', and 'threshold'.</param>
        /// <returns>Result dictionary with list of overlapping UI element pairs.</returns>
        private static object HandleUguiDetectOverlaps(Dictionary<string, object> payload)
        {
            try
            {
                var gameObjectPath = GetString(payload, "gameObjectPath");
                var checkAll = GetBool(payload, "checkAll", false);
                var includeChildren = GetBool(payload, "includeChildren", false);
                var threshold = GetFloat(payload, "threshold") ?? 0f;

                Debug.Log($"[uguiDetectOverlaps] Detecting overlaps - checkAll={checkAll}, includeChildren={includeChildren}, threshold={threshold}");

                var overlaps = new List<Dictionary<string, object>>();

                if (checkAll)
                {
                    // Check all UI elements for overlaps with each other
                    var allRects = UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
                    var rectList = new List<RectTransform>();

                    foreach (var rect in allRects)
                    {
                        // Only include RectTransforms that are under a Canvas
                        if (rect.GetComponentInParent<Canvas>() != null)
                        {
                            rectList.Add(rect);
                        }
                    }

                    Debug.Log($"[uguiDetectOverlaps] Checking {rectList.Count} UI elements");

                    for (int i = 0; i < rectList.Count; i++)
                    {
                        for (int j = i + 1; j < rectList.Count; j++)
                        {
                            var overlap = DetectRectOverlap(rectList[i], rectList[j], threshold);
                            if (overlap != null)
                            {
                                overlaps.Add(overlap);
                            }
                        }
                    }
                }
                else
                {
                    // Check specific GameObject for overlaps
                    if (string.IsNullOrEmpty(gameObjectPath))
                    {
                        throw new InvalidOperationException("gameObjectPath is required when checkAll is false");
                    }

                    var targetGo = ResolveGameObject(gameObjectPath);
                    var targetRects = new List<RectTransform>();

                    if (includeChildren)
                    {
                        targetRects.AddRange(targetGo.GetComponentsInChildren<RectTransform>());
                    }
                    else
                    {
                        var rect = targetGo.GetComponent<RectTransform>();
                        if (rect != null)
                        {
                            targetRects.Add(rect);
                        }
                    }

                    // Get all other RectTransforms in the scene
                    var allRects = UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
                    var otherRects = new List<RectTransform>();

                    foreach (var rect in allRects)
                    {
                        // Only include RectTransforms that are under a Canvas and not in targetRects
                        if (rect.GetComponentInParent<Canvas>() != null && !targetRects.Contains(rect))
                        {
                            otherRects.Add(rect);
                        }
                    }

                    Debug.Log($"[uguiDetectOverlaps] Checking {targetRects.Count} target elements against {otherRects.Count} other elements");

                    foreach (var targetRect in targetRects)
                    {
                        foreach (var otherRect in otherRects)
                        {
                            var overlap = DetectRectOverlap(targetRect, otherRect, threshold);
                            if (overlap != null)
                            {
                                overlaps.Add(overlap);
                            }
                        }
                    }
                }

                Debug.Log($"[uguiDetectOverlaps] Found {overlaps.Count} overlaps");

                return new Dictionary<string, object>
                {
                    ["overlaps"] = overlaps,
                    ["count"] = overlaps.Count,
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[uguiDetectOverlaps] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Detects if two RectTransforms overlap in world space.
        /// </summary>
        /// <param name="rect1">First RectTransform.</param>
        /// <param name="rect2">Second RectTransform.</param>
        /// <param name="threshold">Minimum overlap area to be considered overlapping.</param>
        /// <returns>Dictionary with overlap information, or null if no overlap.</returns>
        private static Dictionary<string, object> DetectRectOverlap(RectTransform rect1, RectTransform rect2, float threshold)
        {
            // Get world corners for both rectangles
            Vector3[] corners1 = new Vector3[4];
            Vector3[] corners2 = new Vector3[4];
            rect1.GetWorldCorners(corners1);
            rect2.GetWorldCorners(corners2);

            // Calculate bounds in 2D (using x and y coordinates)
            float rect1MinX = Mathf.Min(corners1[0].x, corners1[1].x, corners1[2].x, corners1[3].x);
            float rect1MaxX = Mathf.Max(corners1[0].x, corners1[1].x, corners1[2].x, corners1[3].x);
            float rect1MinY = Mathf.Min(corners1[0].y, corners1[1].y, corners1[2].y, corners1[3].y);
            float rect1MaxY = Mathf.Max(corners1[0].y, corners1[1].y, corners1[2].y, corners1[3].y);

            float rect2MinX = Mathf.Min(corners2[0].x, corners2[1].x, corners2[2].x, corners2[3].x);
            float rect2MaxX = Mathf.Max(corners2[0].x, corners2[1].x, corners2[2].x, corners2[3].x);
            float rect2MinY = Mathf.Min(corners2[0].y, corners2[1].y, corners2[2].y, corners2[3].y);
            float rect2MaxY = Mathf.Max(corners2[0].y, corners2[1].y, corners2[2].y, corners2[3].y);

            // Check for overlap
            bool overlapsX = rect1MinX < rect2MaxX && rect1MaxX > rect2MinX;
            bool overlapsY = rect1MinY < rect2MaxY && rect1MaxY > rect2MinY;

            if (overlapsX && overlapsY)
            {
                // Calculate overlap area
                float overlapWidth = Mathf.Min(rect1MaxX, rect2MaxX) - Mathf.Max(rect1MinX, rect2MinX);
                float overlapHeight = Mathf.Min(rect1MaxY, rect2MaxY) - Mathf.Max(rect1MinY, rect2MinY);
                float overlapArea = overlapWidth * overlapHeight;

                if (overlapArea >= threshold)
                {
                    return new Dictionary<string, object>
                    {
                        ["element1"] = GetHierarchyPath(rect1.gameObject),
                        ["element2"] = GetHierarchyPath(rect2.gameObject),
                        ["overlapArea"] = overlapArea,
                        ["overlapWidth"] = overlapWidth,
                        ["overlapHeight"] = overlapHeight,
                        ["element1Bounds"] = new Dictionary<string, object>
                        {
                            ["minX"] = rect1MinX,
                            ["maxX"] = rect1MaxX,
                            ["minY"] = rect1MinY,
                            ["maxY"] = rect1MaxY,
                            ["width"] = rect1MaxX - rect1MinX,
                            ["height"] = rect1MaxY - rect1MinY,
                        },
                        ["element2Bounds"] = new Dictionary<string, object>
                        {
                            ["minX"] = rect2MinX,
                            ["maxX"] = rect2MaxX,
                            ["minY"] = rect2MinY,
                            ["maxY"] = rect2MaxY,
                            ["width"] = rect2MaxX - rect2MinX,
                            ["height"] = rect2MaxY - rect2MinY,
                        },
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Quickly sets up new scenes with common configurations (3D, 2D, UI, VR).
        /// Automatically creates necessary GameObjects like Camera, Lights, Canvas, EventSystem.
        /// </summary>
        /// <param name="payload">Setup type and optional camera/light settings.</param>
        /// <returns>Result dictionary with created objects.</returns>
        #endregion
    }
}
