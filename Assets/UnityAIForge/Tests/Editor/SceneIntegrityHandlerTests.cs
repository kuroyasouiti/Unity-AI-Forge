using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers.HighLevel;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class SceneIntegrityHandlerTests
    {
        private SceneIntegrityHandler _handler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new SceneIntegrityHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown() => _tracker.Dispose();

        [Test]
        public void Category_ReturnsSceneIntegrity()
        {
            Assert.AreEqual("sceneIntegrity", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("missingScripts", ops);
            Assert.Contains("nullReferences", ops);
            Assert.Contains("brokenEvents", ops);
            Assert.Contains("brokenPrefabs", ops);
            Assert.Contains("all", ops);
        }

        [Test]
        public void Execute_NullPayload_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(null));
        }

        [Test]
        public void Execute_UnsupportedOperation_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(TestUtilities.CreatePayload("nonExistent")), "not supported");
        }

        [Test]
        public void All_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("all"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void MissingScripts_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("missingScripts"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void SupportedOperations_ContainsNewOps()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("typeCheck", ops);
            Assert.Contains("report", ops);
            Assert.Contains("checkPrefab", ops);
        }

        [Test]
        public void TypeCheck_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("typeCheck"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void Report_ActiveScene_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("report",
                ("scope", "active_scene")));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void Report_DefaultScope_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("report"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void CheckPrefab_InvalidPath_ReturnsError()
        {
            TestUtilities.AssertError(
                _handler.Execute(TestUtilities.CreatePayload("checkPrefab",
                    ("prefabPath", "Assets/NonExistent.prefab"))),
                "not found");
        }

        [Test]
        public void SupportedOperations_ContainsCanvasGroupAudit()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("canvasGroupAudit", ops);
        }

        [Test]
        public void SupportedOperations_ContainsReferenceSemantics()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("referenceSemantics", ops);
        }

        [Test]
        public void CanvasGroupAudit_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("canvasGroupAudit"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void ReferenceSemantics_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("referenceSemantics"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void All_IncludesNewChecksInSummary()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("all"));
            TestUtilities.AssertSuccess(result);
            var dict = result as Dictionary<string, object>;
            Assert.IsTrue(dict.ContainsKey("summary"), "Result should contain 'summary' key");
            var summary = dict["summary"] as Dictionary<string, int>;
            Assert.IsNotNull(summary, "Summary should be a Dictionary<string, int>");
            Assert.IsTrue(summary.ContainsKey("canvasGroupIssues"), "Summary should contain 'canvasGroupIssues'");
            Assert.IsTrue(summary.ContainsKey("semanticRefIssues"), "Summary should contain 'semanticRefIssues'");
        }

        [Test]
        public void SupportedOperations_ContainsAuditOps()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("requiredFieldAudit", ops);
            Assert.Contains("uiOverflowAudit", ops);
            Assert.Contains("nullAssetAudit", ops);
        }

        [Test]
        public void RequiredFieldAudit_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("requiredFieldAudit"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void UIOverflowAudit_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("uiOverflowAudit"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void NullAssetAudit_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("nullAssetAudit"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void NullAssetAudit_WithSearchPath_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("nullAssetAudit",
                ("searchPath", "Assets")));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void All_IncludesAuditChecksInSummary()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("all"));
            TestUtilities.AssertSuccess(result);
            var dict = result as Dictionary<string, object>;
            var summary = dict["summary"] as Dictionary<string, int>;
            Assert.IsNotNull(summary, "Summary should be a Dictionary<string, int>");
            Assert.IsTrue(summary.ContainsKey("requiredFieldIssues"), "Summary should contain 'requiredFieldIssues'");
            Assert.IsTrue(summary.ContainsKey("uiOverflowIssues"), "Summary should contain 'uiOverflowIssues'");
            Assert.IsTrue(summary.ContainsKey("nullAssetIssues"), "Summary should contain 'nullAssetIssues'");
        }

        [Test]
        public void SupportedOperations_ContainsUIAuditOps()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("touchTargetAudit", ops);
            Assert.Contains("eventSystemAudit", ops);
            Assert.Contains("textOverflowAudit", ops);
            Assert.Contains("styleConsistencyAudit", ops);
        }

        [Test]
        public void TouchTargetAudit_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("touchTargetAudit"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void EventSystemAudit_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("eventSystemAudit"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void TextOverflowAudit_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("textOverflowAudit"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void All_IncludesUIAuditChecksInSummary()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("all"));
            TestUtilities.AssertSuccess(result);
            var dict = result as Dictionary<string, object>;
            var summary = dict["summary"] as Dictionary<string, int>;
            Assert.IsNotNull(summary, "Summary should be a Dictionary<string, int>");
            Assert.IsTrue(summary.ContainsKey("touchTargetIssues"), "Summary should contain 'touchTargetIssues'");
            Assert.IsTrue(summary.ContainsKey("eventSystemIssues"), "Summary should contain 'eventSystemIssues'");
            Assert.IsTrue(summary.ContainsKey("textOverflowIssues"), "Summary should contain 'textOverflowIssues'");
        }

        [Test]
        public void StyleConsistencyAudit_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("styleConsistencyAudit"));
            TestUtilities.AssertSuccess(result);
        }

        #region StyleConsistencyAudit Detail Tests

        private List<Dictionary<string, object>> GetIssues(object result)
        {
            var dict = result as Dictionary<string, object>;
            return dict["issues"] as List<Dictionary<string, object>>;
        }

        private bool HasIssueType(List<Dictionary<string, object>> issues, string type)
        {
            return issues.Any(i => i["type"]?.ToString() == type);
        }

        [Test]
        public void StyleConsistencyAudit_ExcessiveButtonColorVariation_DetectedWith4Colors()
        {
            var root = _tracker.Create("SCRoot_BtnColor");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Create 4 buttons with different normalColor values (>3 triggers the issue)
            Color[] colors = { Color.red, Color.green, Color.blue, Color.yellow };
            for (int i = 0; i < 4; i++)
            {
                var btnGo = _tracker.Create($"Btn{i}");
                btnGo.transform.SetParent(root.transform);
                btnGo.AddComponent<Image>();
                var btn = btnGo.AddComponent<Button>();
                btn.transition = Selectable.Transition.ColorTint;
                var cb = btn.colors;
                cb.normalColor = colors[i];
                btn.colors = cb;
            }

            var result = _handler.Execute(TestUtilities.CreatePayload("styleConsistencyAudit",
                ("rootPath", "SCRoot_BtnColor")));
            TestUtilities.AssertSuccess(result);
            var issues = GetIssues(result);
            Assert.IsTrue(HasIssueType(issues, "excessiveButtonColorVariation"),
                "Should detect excessive button color variation with 4 distinct colors");
        }

        [Test]
        public void StyleConsistencyAudit_ExcessiveButtonColorVariation_NotDetectedWith3Colors()
        {
            var root = _tracker.Create("SCRoot_BtnColor3");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            Color[] colors = { Color.red, Color.green, Color.blue };
            for (int i = 0; i < 3; i++)
            {
                var btnGo = _tracker.Create($"Btn3_{i}");
                btnGo.transform.SetParent(root.transform);
                btnGo.AddComponent<Image>();
                var btn = btnGo.AddComponent<Button>();
                btn.transition = Selectable.Transition.ColorTint;
                var cb = btn.colors;
                cb.normalColor = colors[i];
                btn.colors = cb;
            }

            var result = _handler.Execute(TestUtilities.CreatePayload("styleConsistencyAudit",
                ("rootPath", "SCRoot_BtnColor3")));
            TestUtilities.AssertSuccess(result);
            var issues = GetIssues(result);
            Assert.IsFalse(HasIssueType(issues, "excessiveButtonColorVariation"),
                "Should NOT detect excessive button color variation with only 3 colors");
        }

        [Test]
        public void StyleConsistencyAudit_FontSizeScaleViolation_DetectedWithInconsistentRatios()
        {
            var root = _tracker.Create("SCRoot_Font");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Sizes: 10, 12, 30 — ratio1 = 1.2, ratio2 = 2.5 — abs(1.2-2.5)/1.2 = 1.08 > 0.5
            int[] sizes = { 10, 12, 30 };
            for (int i = 0; i < sizes.Length; i++)
            {
                var textGo = _tracker.Create($"FontText{i}");
                textGo.transform.SetParent(root.transform);
                var text = textGo.AddComponent<Text>();
                text.fontSize = sizes[i];
            }

            var result = _handler.Execute(TestUtilities.CreatePayload("styleConsistencyAudit",
                ("rootPath", "SCRoot_Font")));
            TestUtilities.AssertSuccess(result);
            var issues = GetIssues(result);
            Assert.IsTrue(HasIssueType(issues, "fontSizeScaleViolation"),
                "Should detect inconsistent font size scale with ratios 1.2 vs 2.5");
        }

        [Test]
        public void StyleConsistencyAudit_SpacingInconsistency_DetectedWith5Values()
        {
            var root = _tracker.Create("SCRoot_Spacing");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Create 5 layout groups with distinct non-zero spacing values (>4 triggers)
            float[] spacings = { 2f, 5f, 8f, 13f, 21f };
            for (int i = 0; i < spacings.Length; i++)
            {
                var layoutGo = _tracker.Create($"Layout{i}");
                layoutGo.transform.SetParent(root.transform);
                var vl = layoutGo.AddComponent<VerticalLayoutGroup>();
                vl.spacing = spacings[i];
            }

            var result = _handler.Execute(TestUtilities.CreatePayload("styleConsistencyAudit",
                ("rootPath", "SCRoot_Spacing")));
            TestUtilities.AssertSuccess(result);
            var issues = GetIssues(result);
            Assert.IsTrue(HasIssueType(issues, "spacingInconsistency"),
                "Should detect spacing inconsistency with 5 distinct non-zero spacing values");
        }

        [Test]
        public void StyleConsistencyAudit_NoOpCanvasGroup_DetectedWithDefaultValues()
        {
            var root = _tracker.Create("SCRoot_CG");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var cgGo = _tracker.Create("NoOpCG");
            cgGo.transform.SetParent(root.transform);
            // CanvasGroup defaults: alpha=1, interactable=true, blocksRaycasts=true, ignoreParentGroups=false
            cgGo.AddComponent<CanvasGroup>();

            var result = _handler.Execute(TestUtilities.CreatePayload("styleConsistencyAudit",
                ("rootPath", "SCRoot_CG")));
            TestUtilities.AssertSuccess(result);
            var issues = GetIssues(result);
            Assert.IsTrue(HasIssueType(issues, "noOpCanvasGroup"),
                "Should detect CanvasGroup with all default values as no-op");
        }

        [Test]
        public void StyleConsistencyAudit_NoOpCanvasGroup_NotDetectedWhenAlphaChanged()
        {
            var root = _tracker.Create("SCRoot_CG2");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var cgGo = _tracker.Create("ActiveCG");
            cgGo.transform.SetParent(root.transform);
            var cg = cgGo.AddComponent<CanvasGroup>();
            cg.alpha = 0.5f;

            var result = _handler.Execute(TestUtilities.CreatePayload("styleConsistencyAudit",
                ("rootPath", "SCRoot_CG2")));
            TestUtilities.AssertSuccess(result);
            var issues = GetIssues(result);
            Assert.IsFalse(HasIssueType(issues, "noOpCanvasGroup"),
                "Should NOT detect noOpCanvasGroup when alpha is non-default");
        }

        [Test]
        public void StyleConsistencyAudit_MissingInteractionFeedback_DetectedWithTransitionNone()
        {
            var root = _tracker.Create("SCRoot_Feedback");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var btnGo = _tracker.Create("NoFeedbackBtn");
            btnGo.transform.SetParent(root.transform);
            btnGo.AddComponent<Image>();
            var btn = btnGo.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;

            var result = _handler.Execute(TestUtilities.CreatePayload("styleConsistencyAudit",
                ("rootPath", "SCRoot_Feedback")));
            TestUtilities.AssertSuccess(result);
            var issues = GetIssues(result);
            Assert.IsTrue(HasIssueType(issues, "missingInteractionFeedback"),
                "Should detect Selectable with Transition.None as missing interaction feedback");
        }

        [Test]
        public void StyleConsistencyAudit_UnnecessaryRaycastTarget_DetectedOnImageWithoutSelectable()
        {
            var root = _tracker.Create("SCRoot_Raycast");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var imgGo = _tracker.Create("DecoImage");
            imgGo.transform.SetParent(root.transform);
            var img = imgGo.AddComponent<Image>();
            img.raycastTarget = true;
            // No Selectable, no ScrollRect parent → should trigger

            var result = _handler.Execute(TestUtilities.CreatePayload("styleConsistencyAudit",
                ("rootPath", "SCRoot_Raycast")));
            TestUtilities.AssertSuccess(result);
            var issues = GetIssues(result);
            Assert.IsTrue(HasIssueType(issues, "unnecessaryRaycastTarget"),
                "Should detect Image with raycastTarget=true but no Selectable component");
        }

        [Test]
        public void StyleConsistencyAudit_UnnecessaryRaycastTarget_NotDetectedWithSelectable()
        {
            var root = _tracker.Create("SCRoot_Raycast2");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var btnGo = _tracker.Create("RaycastBtn");
            btnGo.transform.SetParent(root.transform);
            var img = btnGo.AddComponent<Image>();
            img.raycastTarget = true;
            btnGo.AddComponent<Button>(); // Has Selectable → valid use

            var result = _handler.Execute(TestUtilities.CreatePayload("styleConsistencyAudit",
                ("rootPath", "SCRoot_Raycast2")));
            TestUtilities.AssertSuccess(result);
            var issues = GetIssues(result);
            Assert.IsFalse(HasIssueType(issues, "unnecessaryRaycastTarget"),
                "Should NOT detect unnecessaryRaycastTarget when Selectable is present");
        }

        [Test]
        public void StyleConsistencyAudit_InconsistentSiblingAnchors_DetectedWith3Patterns()
        {
            var root = _tracker.Create("SCRoot_Anchors");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var parent = _tracker.Create("AnchorParent");
            parent.AddComponent<RectTransform>();
            parent.transform.SetParent(root.transform);

            // Child 1: top-left anchor (anchorMin=anchorMax=(0,1))
            var child1 = _tracker.Create("AnchorChild1");
            var rt1 = child1.AddComponent<RectTransform>();
            rt1.anchorMin = new Vector2(0, 1);
            rt1.anchorMax = new Vector2(0, 1);
            child1.transform.SetParent(parent.transform);

            // Child 2: center anchor (anchorMin=anchorMax=(0.5,0.5))
            var child2 = _tracker.Create("AnchorChild2");
            var rt2 = child2.AddComponent<RectTransform>();
            rt2.anchorMin = new Vector2(0.5f, 0.5f);
            rt2.anchorMax = new Vector2(0.5f, 0.5f);
            child2.transform.SetParent(parent.transform);

            // Child 3: stretch-all anchor (anchorMin=(0,0), anchorMax=(1,1))
            var child3 = _tracker.Create("AnchorChild3");
            var rt3 = child3.AddComponent<RectTransform>();
            rt3.anchorMin = new Vector2(0, 0);
            rt3.anchorMax = new Vector2(1, 1);
            child3.transform.SetParent(parent.transform);

            var result = _handler.Execute(TestUtilities.CreatePayload("styleConsistencyAudit",
                ("rootPath", "SCRoot_Anchors")));
            TestUtilities.AssertSuccess(result);
            var issues = GetIssues(result);
            Assert.IsTrue(HasIssueType(issues, "inconsistentSiblingAnchors"),
                "Should detect inconsistent sibling anchors with 3 different patterns");
        }

        [Test]
        public void StyleConsistencyAudit_InconsistentSiblingAnchors_NotDetectedWithLayoutGroup()
        {
            var root = _tracker.Create("SCRoot_Anchors2");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var parent = _tracker.Create("LayoutParent");
            parent.AddComponent<RectTransform>();
            parent.AddComponent<VerticalLayoutGroup>(); // LayoutGroup present → skipped
            parent.transform.SetParent(root.transform);

            var child1 = _tracker.Create("LChild1");
            var rt1 = child1.AddComponent<RectTransform>();
            rt1.anchorMin = new Vector2(0, 1);
            rt1.anchorMax = new Vector2(0, 1);
            child1.transform.SetParent(parent.transform);

            var child2 = _tracker.Create("LChild2");
            var rt2 = child2.AddComponent<RectTransform>();
            rt2.anchorMin = new Vector2(0.5f, 0.5f);
            rt2.anchorMax = new Vector2(0.5f, 0.5f);
            child2.transform.SetParent(parent.transform);

            var child3 = _tracker.Create("LChild3");
            var rt3 = child3.AddComponent<RectTransform>();
            rt3.anchorMin = new Vector2(0, 0);
            rt3.anchorMax = new Vector2(1, 1);
            child3.transform.SetParent(parent.transform);

            var result = _handler.Execute(TestUtilities.CreatePayload("styleConsistencyAudit",
                ("rootPath", "SCRoot_Anchors2")));
            TestUtilities.AssertSuccess(result);
            var issues = GetIssues(result);
            Assert.IsFalse(HasIssueType(issues, "inconsistentSiblingAnchors"),
                "Should NOT detect inconsistent anchors when parent has LayoutGroup");
        }

        #endregion
    }
}
