using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitStatusEffectHandlerTests : GameKitHandlerTestBase
    {
        private GameKitStatusEffectHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitStatusEffectHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitStatusEffect", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "defineEffect", "updateEffect", "inspectEffect", "deleteEffect",
                "addModifier", "updateModifier", "removeModifier", "clearModifiers",
                "create", "update", "inspect", "delete",
                "applyEffect", "removeEffect", "clearEffects",
                "getActiveEffects", "getStatModifier",
                "findByEffectId", "findByReceiverId", "listEffects");
        }

        #endregion

        #region Define Effect

        [Test]
        public void DefineEffect_ShouldGenerateScript()
        {
            var result = Execute(_handler, "defineEffect",
                ("effectId", "test_effect"),
                ("displayName", "Test Effect"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "effectId");
        }

        [Test]
        public void DefineEffect_GeneratedScriptClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "defineEffect",
                ("effectId", "test_effect"),
                ("displayName", "Test Effect"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomStatusEffectData"));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomStatusEffectData");
        }

        #endregion

        #region Create Receiver

        [Test]
        public void Create_ShouldGenerateScript()
        {
            CreateTestGameObject("TestTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "TestTarget"),
                ("receiverId", "test_receiver"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "receiverId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("TestTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "TestTarget"),
                ("receiverId", "test_receiver"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomReceiver"));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomReceiver");
        }

        [Test]
        public void Create_MissingTargetPath_ShouldReturnError()
        {
            var result = Execute(_handler, "create",
                ("receiverId", "test_receiver"),
                ("outputPath", TestOutputDir));
            AssertError(result);
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
