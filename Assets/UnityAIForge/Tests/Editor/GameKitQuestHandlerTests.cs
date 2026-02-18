using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitQuestHandlerTests : GameKitHandlerTestBase
    {
        private GameKitQuestHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitQuestHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitQuest", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "createQuest", "updateQuest", "inspectQuest", "deleteQuest",
                "addObjective", "updateObjective", "removeObjective",
                "addPrerequisite", "removePrerequisite",
                "addReward", "removeReward",
                "startQuest", "completeQuest", "failQuest", "abandonQuest",
                "updateProgress", "listQuests",
                "createManager", "inspectManager", "deleteManager",
                "findByQuestId");
        }

        #endregion

        #region Create Quest

        [Test]
        public void CreateQuest_ShouldGenerateScript()
        {
            var result = Execute(_handler, "createQuest",
                ("questId", "test_quest"),
                ("title", "Test Quest"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "questId");
        }

        [Test]
        public void CreateQuest_GeneratedScriptClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "createQuest",
                ("questId", "test_quest"),
                ("title", "Test Quest"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomQuestData"));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomQuestData");
        }

        #endregion

        #region Create Manager

        [Test]
        public void CreateManager_ShouldGenerateScript()
        {
            var result = Execute(_handler, "createManager",
                ("questManagerId", "test_qm"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("QuestManager");

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "questManagerId");
        }

        [Test]
        public void CreateManager_GeneratedScriptClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "createManager",
                ("questManagerId", "test_qm"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomQuestManager"));
            TrackCreatedGameObject("QuestManager");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomQuestManager");
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
