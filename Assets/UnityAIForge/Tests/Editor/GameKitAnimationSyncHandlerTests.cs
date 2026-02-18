using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitAnimationSyncHandlerTests : GameKitHandlerTestBase
    {
        private GameKitAnimationSyncHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitAnimationSyncHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitAnimationSync", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete",
                "addSyncRule", "removeSyncRule",
                "addTriggerRule", "removeTriggerRule",
                "fireTrigger", "setParameter",
                "findBySyncId");
        }

        #endregion

        #region Create

        [Test]
        public void Create_ShouldGenerateScript()
        {
            CreateTestGameObject("AnimTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "AnimTarget"),
                ("syncId", "test_sync"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "syncId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("AnimTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "AnimTarget"),
                ("syncId", "test_sync"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomAnimSync"));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomAnimSync");
        }

        [Test]
        public void Create_DefaultClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("AnimTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "AnimTarget"),
                ("syncId", "test_sync"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "TestSyncAnimationSync");
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
