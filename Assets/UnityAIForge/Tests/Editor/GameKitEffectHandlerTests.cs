using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitEffectHandlerTests : GameKitHandlerTestBase
    {
        private GameKitEffectHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitEffectHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitEffect", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete",
                "addComponent", "removeComponent", "clearComponents",
                "play", "playAtPosition", "playAtTransform",
                "shakeCamera", "flashScreen", "setTimeScale",
                "createManager", "registerEffect", "unregisterEffect",
                "findByEffectId", "listEffects");
        }

        #endregion

        #region Create

        [Test]
        public void Create_ShouldGenerateScript()
        {
            var result = Execute(_handler, "create",
                ("effectId", "test_effect"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("test_effect");

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "effectId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "create",
                ("effectId", "test_effect"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomEffect"));
            TrackCreatedGameObject("test_effect");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomEffect");
        }

        [Test]
        public void Create_DefaultClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "create",
                ("effectId", "test_effect"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("test_effect");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "TestEffectEffect");
        }

        #endregion

        #region CreateManager

        [Test]
        public void CreateManager_ShouldGenerateScript()
        {
            var result = Execute(_handler, "createManager",
                ("managerId", "test_mgr"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("test_mgr");

            AssertSuccess(result);
            AssertScriptGenerated(result);
        }

        [Test]
        public void CreateManager_DefaultClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "createManager",
                ("managerId", "test_mgr"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("test_mgr");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "TestMgr");
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
