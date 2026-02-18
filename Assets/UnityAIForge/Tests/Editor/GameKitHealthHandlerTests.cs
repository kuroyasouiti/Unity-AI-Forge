using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitHealthHandlerTests : GameKitHandlerTestBase
    {
        private GameKitHealthHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitHealthHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitHealth", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete",
                "applyDamage", "heal", "kill", "respawn",
                "setInvincible", "findByHealthId");
        }

        #endregion

        #region Create

        [Test]
        public void Create_ShouldGenerateScript()
        {
            CreateTestGameObject("TestTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "TestTarget"),
                ("healthId", "test_hp"),
                ("maxHealth", 100f),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "healthId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("TestTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "TestTarget"),
                ("healthId", "test_hp"),
                ("maxHealth", 100f),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomHealth"));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomHealth");
        }

        [Test]
        public void Create_DefaultClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("TestTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "TestTarget"),
                ("healthId", "test_hp"),
                ("maxHealth", 100f),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "TestHpHealth");
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
