using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitDialogueHandlerTests : GameKitHandlerTestBase
    {
        private GameKitDialogueHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitDialogueHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitDialogue", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "createDialogue", "updateDialogue", "inspectDialogue", "deleteDialogue",
                "addNode", "updateNode", "removeNode",
                "addChoice", "updateChoice", "removeChoice",
                "startDialogue", "selectChoice", "advanceDialogue", "endDialogue",
                "createManager", "inspectManager", "deleteManager",
                "findByDialogueId");
        }

        #endregion

        #region Create Dialogue

        [Test]
        public void CreateDialogue_ShouldGenerateScript()
        {
            var result = Execute(_handler, "createDialogue",
                ("dialogueId", "test_dialogue"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "dialogueId");
        }

        [Test]
        public void CreateDialogue_GeneratedScriptClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "createDialogue",
                ("dialogueId", "test_dialogue"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomDialogueData"));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomDialogueData");
        }

        #endregion

        #region Create Manager

        [Test]
        public void CreateManager_ShouldGenerateScript()
        {
            var result = Execute(_handler, "createManager",
                ("dialogueManagerId", "test_dm"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("DialogueManager");

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "dialogueManagerId");
        }

        [Test]
        public void CreateManager_GeneratedScriptClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "createManager",
                ("dialogueManagerId", "test_dm"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomDialogueManager"));
            TrackCreatedGameObject("DialogueManager");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomDialogueManager");
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
