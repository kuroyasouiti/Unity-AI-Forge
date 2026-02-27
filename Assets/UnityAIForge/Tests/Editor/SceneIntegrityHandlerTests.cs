using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers.HighLevel;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class SceneIntegrityHandlerTests
    {
        private SceneIntegrityHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new SceneIntegrityHandler();
        }

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
            Assert.Contains("removeMissingScripts", ops);
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
        public void RemoveMissingScripts_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("removeMissingScripts"));
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
    }
}
