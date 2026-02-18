using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitFeedbackHandlerTests : GameKitHandlerTestBase
    {
        private GameKitFeedbackHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitFeedbackHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitFeedback", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete",
                "addComponent", "clearComponents", "setIntensity",
                "findByFeedbackId");
        }

        #endregion

        #region Create

        [Test]
        public void Create_ShouldGenerateScript()
        {
            CreateTestGameObject("FeedbackTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "FeedbackTarget"),
                ("feedbackId", "test_feedback"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "feedbackId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("FeedbackTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "FeedbackTarget"),
                ("feedbackId", "test_feedback"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomFeedback"));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomFeedback");
        }

        [Test]
        public void Create_DefaultClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("FeedbackTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "FeedbackTarget"),
                ("feedbackId", "test_feedback"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "TestFeedbackFeedback");
        }

        #endregion

        #region Error Handling

        [Test]
        public void Create_MissingTargetPath_ShouldReturnError()
        {
            var result = Execute(_handler, "create");
            AssertError(result);
        }

        [Test]
        public void Execute_UnsupportedOperation_ShouldReturnError()
        {
            var result = Execute(_handler, "nonexistent_operation");
            AssertError(result);
        }

        [Test]
        public void Execute_NullPayload_ShouldReturnError()
        {
            var result = _handler.Execute(null) as Dictionary<string, object>;
            AssertError(result);
        }

        #endregion
    }
}
