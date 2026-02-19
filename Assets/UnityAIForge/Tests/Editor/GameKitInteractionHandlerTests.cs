using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitInteractionHandlerTests : GameKitHandlerTestBase
    {
        private GameKitInteractionHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitInteractionHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitInteraction", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete");
        }

        #endregion

        #region Create

        [Test]
        public void Create_ShouldGenerateScript()
        {
            var result = Execute(_handler, "create",
                ("interactionId", "test_interaction"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("test_interaction");

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "interactionId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "create",
                ("interactionId", "test_interaction"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomInteraction"));
            TrackCreatedGameObject("test_interaction");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomInteraction");
        }

        [Test]
        public void Create_WithTargetPath_ShouldAttachToExistingObject()
        {
            var go = CreateTestGameObject("InteractionTarget");

            var result = Execute(_handler, "create",
                ("interactionId", "test_interaction_target"),
                ("parentPath", "InteractionTarget"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptGenerated(result);
        }

        #endregion

        #region Error Handling

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
