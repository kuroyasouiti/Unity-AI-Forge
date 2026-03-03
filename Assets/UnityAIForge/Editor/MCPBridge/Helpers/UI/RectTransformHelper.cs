using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace MCP.Editor.Helpers.UI
{
    /// <summary>
    /// RectTransform操作の共通ユーティリティメソッドを提供します。
    /// </summary>
    public static class RectTransformHelper
    {
        #region State Capture
        
        /// <summary>
        /// RectTransformの現在の状態をキャプチャします。
        /// </summary>
        public static Dictionary<string, object> CaptureState(RectTransform rectTransform)
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
        
        #endregion
        
        #region Anchor Operations
        
        /// <summary>
        /// カスタムアンカー値を設定します。
        /// </summary>
        public static void SetAnchor(RectTransform rectTransform, Dictionary<string, object> payload)
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
        /// アンカープリセットを設定します。
        /// </summary>
        public static void SetAnchorPreset(RectTransform rectTransform, Dictionary<string, object> payload)
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
        /// 絶対位置からアンカー位置に変換します。
        /// </summary>
        public static void ConvertToAnchored(RectTransform rectTransform, Dictionary<string, object> payload)
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
        /// ワールドコーナーを復元します（アンカー変更時の位置保持用）。
        /// </summary>
        private static void RestoreWorldCorners(RectTransform rectTransform, Vector3[] corners)
        {
            var parentRect = rectTransform.parent.GetComponent<RectTransform>();
            if (parentRect == null)
            {
                return;
            }

            // Convert all 4 world corners to parent local space and compute bounding box.
            // This correctly handles rotated parents where corners[0]/[2] may not be min/max.
            Vector2 localMin = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 localMax = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 4; i++)
            {
                Vector2 local = parentRect.InverseTransformPoint(corners[i]);
                if (local.x < localMin.x) localMin.x = local.x;
                if (local.y < localMin.y) localMin.y = local.y;
                if (local.x > localMax.x) localMax.x = local.x;
                if (local.y > localMax.y) localMax.y = local.y;
            }

            // Calculate anchor positions in parent space
            Vector2 parentSize = parentRect.rect.size;
            Vector2 anchorMin = rectTransform.anchorMin;
            Vector2 anchorMax = rectTransform.anchorMax;

            Vector2 anchorPosMin = new Vector2(anchorMin.x * parentSize.x, anchorMin.y * parentSize.y);
            Vector2 anchorPosMax = new Vector2(anchorMax.x * parentSize.x, anchorMax.y * parentSize.y);

            // Set offsetMin and offsetMax to restore world corners
            rectTransform.offsetMin = localMin - anchorPosMin;
            rectTransform.offsetMax = localMax - anchorPosMax;
        }
        
        #endregion
        
        #region Property Updates
        
        /// <summary>
        /// RectTransformのプロパティを更新します。
        /// </summary>
        public static void UpdateProperties(RectTransform rectTransform, Dictionary<string, object> payload)
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
            
            // Update pivot - supports both dictionary format and individual fields
            if (payload.TryGetValue("pivot", out var pivotObj) && pivotObj is Dictionary<string, object> pivotDict)
            {
                // Dictionary format: {"pivot": {"x": 0.5, "y": 0.5}}
                var piv = rectTransform.pivot;
                rectTransform.pivot = new Vector2(
                    GetFloat(pivotDict, "x") ?? piv.x,
                    GetFloat(pivotDict, "y") ?? piv.y
                );
            }
            else
            {
                // Individual fields format: {"pivotX": 0.5, "pivotY": 0.5}
                var pivotX = GetFloat(payload, "pivotX");
                var pivotY = GetFloat(payload, "pivotY");
                if (pivotX.HasValue || pivotY.HasValue)
                {
                    var piv = rectTransform.pivot;
                    rectTransform.pivot = new Vector2(
                        pivotX ?? piv.x,
                        pivotY ?? piv.y
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
        }
        
        #endregion
        
        #region Canvas Utilities
        
        /// <summary>
        /// RectTransformが属するCanvasを取得します。
        /// </summary>
        public static Canvas GetCanvas(RectTransform rectTransform)
        {
            var canvas = rectTransform.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                throw new InvalidOperationException("Target is not under a Canvas");
            }
            return canvas;
        }
        
        #endregion
        
        #region Payload Helpers
        
        private static string GetString(Dictionary<string, object> payload, string key)
        {
            if (payload == null || !payload.TryGetValue(key, out var value))
            {
                return null;
            }
            return value?.ToString();
        }
        
        private static bool GetBool(Dictionary<string, object> payload, string key, bool defaultValue)
        {
            if (payload == null || !payload.TryGetValue(key, out var value))
            {
                return defaultValue;
            }
            
            if (value is bool boolValue)
            {
                return boolValue;
            }
            
            if (bool.TryParse(value?.ToString(), out var parsedValue))
            {
                return parsedValue;
            }
            
            return defaultValue;
        }
        
        private static float? GetFloat(Dictionary<string, object> payload, string key)
        {
            if (payload == null || !payload.TryGetValue(key, out var value) || value == null)
            {
                return null;
            }
            
            if (value is float floatValue)
            {
                return floatValue;
            }
            if (value is double doubleValue)
            {
                return (float)doubleValue;
            }
            if (value is int intValue)
            {
                return (float)intValue;
            }
            
            if (float.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
            {
                return parsedValue;
            }
            
            return null;
        }
        
        #endregion
    }
}

