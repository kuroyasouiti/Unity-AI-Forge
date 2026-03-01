using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Mid-level batch utilities for RectTransform: anchors, pivot, size, position, alignment, distribution.
    /// </summary>
    public class RectTransformBatchHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "setAnchors",
            "setPivot",
            "setSizeDelta",
            "setAnchoredPosition",
            "alignToParent",
            "distributeHorizontal",
            "distributeVertical",
            "matchSize",
        };

        public override string Category => "rectTransformBatch";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "setAnchors" => SetAnchors(payload),
                "setPivot" => SetPivot(payload),
                "setSizeDelta" => SetSizeDelta(payload),
                "setAnchoredPosition" => SetAnchoredPosition(payload),
                "alignToParent" => AlignToParent(payload),
                "distributeHorizontal" => DistributeHorizontal(payload),
                "distributeVertical" => DistributeVertical(payload),
                "matchSize" => MatchSize(payload),
                _ => throw new InvalidOperationException($"Unsupported RectTransform batch operation: {operation}"),
            };
        }

        #region Set Anchors

        private object SetAnchors(Dictionary<string, object> payload)
        {
            var targets = GetTargetRectTransforms(payload);
            var anchorMin = GetVector2(payload, "anchorMin", Vector2.zero);
            var anchorMax = GetVector2(payload, "anchorMax", Vector2.one);

            foreach (var rt in targets)
            {
                Undo.RecordObject(rt, "Set Anchors");
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
            }

            MarkScenesDirty(targets.Select(rt => rt.gameObject));
            return CreateSuccessResponse(("count", targets.Count));
        }

        #endregion

        #region Set Pivot

        private object SetPivot(Dictionary<string, object> payload)
        {
            var targets = GetTargetRectTransforms(payload);
            var pivot = GetVector2(payload, "pivot", new Vector2(0.5f, 0.5f));

            foreach (var rt in targets)
            {
                Undo.RecordObject(rt, "Set Pivot");
                rt.pivot = pivot;
            }

            MarkScenesDirty(targets.Select(rt => rt.gameObject));
            return CreateSuccessResponse(("count", targets.Count));
        }

        #endregion

        #region Set Size Delta

        private object SetSizeDelta(Dictionary<string, object> payload)
        {
            var targets = GetTargetRectTransforms(payload);
            var sizeDelta = GetVector2(payload, "sizeDelta", Vector2.zero);

            foreach (var rt in targets)
            {
                Undo.RecordObject(rt, "Set Size Delta");
                rt.sizeDelta = sizeDelta;
            }

            MarkScenesDirty(targets.Select(rt => rt.gameObject));
            return CreateSuccessResponse(("count", targets.Count));
        }

        #endregion

        #region Set Anchored Position

        private object SetAnchoredPosition(Dictionary<string, object> payload)
        {
            var targets = GetTargetRectTransforms(payload);
            var anchoredPosition = GetVector2(payload, "anchoredPosition", Vector2.zero);

            foreach (var rt in targets)
            {
                Undo.RecordObject(rt, "Set Anchored Position");
                rt.anchoredPosition = anchoredPosition;
            }

            MarkScenesDirty(targets.Select(rt => rt.gameObject));
            return CreateSuccessResponse(("count", targets.Count));
        }

        #endregion

        #region Align To Parent

        private object AlignToParent(Dictionary<string, object> payload)
        {
            var targets = GetTargetRectTransforms(payload);
            var preset = GetString(payload, "preset")?.ToLowerInvariant() ?? "middlecenter";

            foreach (var rt in targets)
            {
                Undo.RecordObject(rt, "Align To Parent");
                ApplyAnchorPreset(rt, preset);
            }

            MarkScenesDirty(targets.Select(rt => rt.gameObject));
            return CreateSuccessResponse(("count", targets.Count));
        }

        private void ApplyAnchorPreset(RectTransform rt, string preset)
        {
            switch (preset)
            {
                case "topleft":
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 1);
                    break;
                case "topcenter":
                    rt.anchorMin = new Vector2(0.5f, 1);
                    rt.anchorMax = new Vector2(0.5f, 1);
                    rt.pivot = new Vector2(0.5f, 1);
                    break;
                case "topright":
                    rt.anchorMin = new Vector2(1, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(1, 1);
                    break;
                case "middleleft":
                    rt.anchorMin = new Vector2(0, 0.5f);
                    rt.anchorMax = new Vector2(0, 0.5f);
                    rt.pivot = new Vector2(0, 0.5f);
                    break;
                case "middlecenter":
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    break;
                case "middleright":
                    rt.anchorMin = new Vector2(1, 0.5f);
                    rt.anchorMax = new Vector2(1, 0.5f);
                    rt.pivot = new Vector2(1, 0.5f);
                    break;
                case "bottomleft":
                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(0, 0);
                    rt.pivot = new Vector2(0, 0);
                    break;
                case "bottomcenter":
                    rt.anchorMin = new Vector2(0.5f, 0);
                    rt.anchorMax = new Vector2(0.5f, 0);
                    rt.pivot = new Vector2(0.5f, 0);
                    break;
                case "bottomright":
                    rt.anchorMin = new Vector2(1, 0);
                    rt.anchorMax = new Vector2(1, 0);
                    rt.pivot = new Vector2(1, 0);
                    break;
                case "stretchleft":
                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 0.5f);
                    break;
                case "stretchcenter":
                    rt.anchorMin = new Vector2(0.5f, 0);
                    rt.anchorMax = new Vector2(0.5f, 1);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    break;
                case "stretchright":
                    rt.anchorMin = new Vector2(1, 0);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(1, 0.5f);
                    break;
                case "stretchtop":
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(0.5f, 1);
                    break;
                case "stretchmiddle":
                    rt.anchorMin = new Vector2(0, 0.5f);
                    rt.anchorMax = new Vector2(1, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    break;
                case "stretchbottom":
                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(1, 0);
                    rt.pivot = new Vector2(0.5f, 0);
                    break;
                case "stretchall":
                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = Vector2.zero;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unknown preset: '{preset}'. Use 'topLeft', 'topCenter', 'topRight', 'middleLeft', 'middleCenter', 'middleRight', 'bottomLeft', 'bottomCenter', 'bottomRight', 'stretchLeft', 'stretchCenter', 'stretchRight', 'stretchTop', 'stretchMiddle', 'stretchBottom', or 'stretchAll'.");
            }
        }

        #endregion

        #region Distribute Horizontal

        private object DistributeHorizontal(Dictionary<string, object> payload)
        {
            var targets = GetTargetRectTransforms(payload);
            if (targets.Count < 2)
            {
                throw new InvalidOperationException("At least 2 RectTransforms are required for distribution.");
            }

            var spacing = GetFloat(payload, "spacing", 0f);

            // Sort by current X position
            var sorted = targets.OrderBy(rt => rt.anchoredPosition.x).ToList();
            var first = sorted.First();
            var last = sorted.Last();

            if (sorted.Count == 2)
            {
                // Just apply spacing
                Undo.RecordObject(last, "Distribute Horizontal");
                last.anchoredPosition = new Vector2(first.anchoredPosition.x + spacing, last.anchoredPosition.y);
            }
            else
            {
                var totalSpacing = (sorted.Count - 1) * spacing;
                var availableWidth = last.anchoredPosition.x - first.anchoredPosition.x - totalSpacing;
                var step = availableWidth / (sorted.Count - 1);

                for (var i = 1; i < sorted.Count - 1; i++)
                {
                    Undo.RecordObject(sorted[i], "Distribute Horizontal");
                    var newX = first.anchoredPosition.x + (step + spacing) * i;
                    sorted[i].anchoredPosition = new Vector2(newX, sorted[i].anchoredPosition.y);
                }
            }

            MarkScenesDirty(targets.Select(rt => rt.gameObject));
            return CreateSuccessResponse(("count", targets.Count));
        }

        #endregion

        #region Distribute Vertical

        private object DistributeVertical(Dictionary<string, object> payload)
        {
            var targets = GetTargetRectTransforms(payload);
            if (targets.Count < 2)
            {
                throw new InvalidOperationException("At least 2 RectTransforms are required for distribution.");
            }

            var spacing = GetFloat(payload, "spacing", 0f);

            // Sort by current Y position (descending, since UI Y is bottom-to-top)
            var sorted = targets.OrderByDescending(rt => rt.anchoredPosition.y).ToList();
            var first = sorted.First();
            var last = sorted.Last();

            if (sorted.Count == 2)
            {
                // Just apply spacing
                Undo.RecordObject(last, "Distribute Vertical");
                last.anchoredPosition = new Vector2(last.anchoredPosition.x, first.anchoredPosition.y - spacing);
            }
            else
            {
                var totalSpacing = (sorted.Count - 1) * spacing;
                var availableHeight = first.anchoredPosition.y - last.anchoredPosition.y - totalSpacing;
                var step = availableHeight / (sorted.Count - 1);

                for (var i = 1; i < sorted.Count - 1; i++)
                {
                    Undo.RecordObject(sorted[i], "Distribute Vertical");
                    var newY = first.anchoredPosition.y - (step + spacing) * i;
                    sorted[i].anchoredPosition = new Vector2(sorted[i].anchoredPosition.x, newY);
                }
            }

            MarkScenesDirty(targets.Select(rt => rt.gameObject));
            return CreateSuccessResponse(("count", targets.Count));
        }

        #endregion

        #region Match Size

        private object MatchSize(Dictionary<string, object> payload)
        {
            var targets = GetTargetRectTransforms(payload);
            var sourcePath = GetString(payload, "sourceGameObjectPath");
            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new InvalidOperationException("sourceGameObjectPath is required for matchSize.");
            }

            var sourceGo = ResolveGameObject(sourcePath);
            var sourceRt = sourceGo.GetComponent<RectTransform>();
            if (sourceRt == null)
            {
                throw new InvalidOperationException($"Source GameObject '{sourcePath}' does not have a RectTransform.");
            }

            var matchWidth = GetBool(payload, "matchWidth");
            var matchHeight = GetBool(payload, "matchHeight");

            if (!matchWidth && !matchHeight)
                throw new InvalidOperationException("At least one of 'matchWidth' or 'matchHeight' must be true.");

            foreach (var rt in targets)
            {
                Undo.RecordObject(rt, "Match Size");
                var newSize = rt.sizeDelta;
                if (matchWidth)
                {
                    newSize.x = sourceRt.sizeDelta.x;
                }
                if (matchHeight)
                {
                    newSize.y = sourceRt.sizeDelta.y;
                }
                rt.sizeDelta = newSize;
            }

            MarkScenesDirty(targets.Select(rt => rt.gameObject));
            return CreateSuccessResponse(("count", targets.Count));
        }

        #endregion

        #region Helpers

        private List<RectTransform> GetTargetRectTransforms(Dictionary<string, object> payload)
        {
            var paths = GetStringList(payload, "gameObjectPaths");
            if (paths.Count == 0)
                throw new InvalidOperationException("'gameObjectPaths' is required and must contain at least one path.");
            var result = new List<RectTransform>();
            foreach (var path in paths)
            {
                var go = ResolveGameObject(path);
                var rt = go.GetComponent<RectTransform>();
                if (rt == null)
                {
                    throw new InvalidOperationException($"GameObject '{path}' does not have a RectTransform.");
                }
                result.Add(rt);
            }
            return result;
        }

        #endregion
    }
}

