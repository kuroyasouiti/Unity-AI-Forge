using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitCombatHandlerTests : GameKitHandlerTestBase
    {
        private GameKitCombatHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitCombatHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitCombat", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete",
                "addTargetTag", "removeTargetTag",
                "resetCooldown", "findByCombatId");
        }

        #endregion

        #region Create

        [Test]
        public void Create_ShouldGenerateScript()
        {
            CreateTestGameObject("TestTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "TestTarget"),
                ("combatId", "test_combat"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "combatId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("TestTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "TestTarget"),
                ("combatId", "test_combat"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomCombat"));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomCombat");
        }

        [Test]
        public void Create_DefaultClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("TestTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "TestTarget"),
                ("combatId", "test_combat"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "TestCombatCombat");
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
