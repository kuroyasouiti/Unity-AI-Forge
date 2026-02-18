using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitVFXHandlerTests : GameKitHandlerTestBase
    {
        private GameKitVFXHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitVFXHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitVFX", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete",
                "setMultipliers", "setColor", "setLoop",
                "findByVFXId");
        }

        #endregion

        #region Create

        [Test]
        public void Create_ShouldGenerateScript()
        {
            CreateTestGameObject("VFXTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "VFXTarget"),
                ("vfxId", "test_vfx"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "vfxId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("VFXTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "VFXTarget"),
                ("vfxId", "test_vfx"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomVFX"));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomVFX");
        }

        [Test]
        public void Create_DefaultClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("VFXTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "VFXTarget"),
                ("vfxId", "test_vfx"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "TestVfxVFX");
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
