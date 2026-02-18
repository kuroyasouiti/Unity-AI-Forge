using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitTimerHandlerTests : GameKitHandlerTestBase
    {
        private GameKitTimerHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitTimerHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitTimer", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "createTimer", "updateTimer", "inspectTimer", "deleteTimer",
                "createCooldown", "updateCooldown", "inspectCooldown", "deleteCooldown",
                "createCooldownManager", "addCooldownToManager", "inspectCooldownManager",
                "findByTimerId", "findByCooldownId");
        }

        #endregion

        #region Create Timer

        [Test]
        public void CreateTimer_ShouldGenerateScript()
        {
            CreateTestGameObject("TestTarget");
            var result = Execute(_handler, "createTimer",
                ("targetPath", "TestTarget"),
                ("timerId", "test_timer"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "timerId");
        }

        [Test]
        public void CreateTimer_GeneratedScriptClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("TestTarget");
            var result = Execute(_handler, "createTimer",
                ("targetPath", "TestTarget"),
                ("timerId", "test_timer"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomTimer"));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomTimer");
        }

        [Test]
        public void CreateTimer_DefaultClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("TestTarget");
            var result = Execute(_handler, "createTimer",
                ("targetPath", "TestTarget"),
                ("timerId", "test_timer"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "TestTimerTimer");
        }

        #endregion

        #region Create Cooldown

        [Test]
        public void CreateCooldown_ShouldGenerateScript()
        {
            CreateTestGameObject("TestTarget");
            var result = Execute(_handler, "createCooldown",
                ("targetPath", "TestTarget"),
                ("cooldownId", "test_cd"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "cooldownId");
        }

        #endregion

        #region Error Handling

        [Test]
        public void CreateTimer_MissingTargetPath_ShouldReturnError()
        {
            var result = Execute(_handler, "createTimer");
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
