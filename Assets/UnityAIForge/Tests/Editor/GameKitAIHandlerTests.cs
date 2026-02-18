using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitAIHandlerTests : GameKitHandlerTestBase
    {
        private GameKitAIHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitAIHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitAI", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete",
                "setTarget", "clearTarget", "setState",
                "addPatrolPoint", "clearPatrolPoints", "findByAIId");
        }

        #endregion

        #region Create

        [Test]
        public void Create_ShouldGenerateScript()
        {
            CreateTestGameObject("TestTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "TestTarget"),
                ("aiId", "test_ai"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "aiId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("TestTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "TestTarget"),
                ("aiId", "test_ai"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomAI"));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomAI");
        }

        [Test]
        public void Create_DefaultClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("TestTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "TestTarget"),
                ("aiId", "test_ai"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "TestAiAI");
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
