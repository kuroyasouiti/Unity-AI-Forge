using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitAudioHandlerTests : GameKitHandlerTestBase
    {
        private GameKitAudioHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitAudioHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitAudio", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete",
                "setVolume", "setPitch", "setLoop", "setClip",
                "findByAudioId");
        }

        #endregion

        #region Create

        [Test]
        public void Create_ShouldGenerateScript()
        {
            CreateTestGameObject("AudioTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "AudioTarget"),
                ("audioId", "test_audio"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "audioId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("AudioTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "AudioTarget"),
                ("audioId", "test_audio"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomAudio"));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomAudio");
        }

        [Test]
        public void Create_DefaultClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("AudioTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "AudioTarget"),
                ("audioId", "test_audio"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "TestAudioAudio");
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
