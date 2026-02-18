using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitSaveHandlerTests : GameKitHandlerTestBase
    {
        private GameKitSaveHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitSaveHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitSave", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "createProfile", "updateProfile", "inspectProfile", "deleteProfile",
                "addTarget", "removeTarget", "clearTargets",
                "save", "load", "listSlots", "deleteSlot",
                "createManager", "inspectManager", "deleteManager",
                "findByProfileId");
        }

        #endregion

        #region Create Profile

        [Test]
        public void CreateProfile_ShouldGenerateScript()
        {
            var result = Execute(_handler, "createProfile",
                ("profileId", "test_profile"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "profileId");
        }

        [Test]
        public void CreateProfile_GeneratedScriptClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "createProfile",
                ("profileId", "test_profile"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomSaveProfile"));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomSaveProfile");
        }

        #endregion

        #region Create Manager

        [Test]
        public void CreateManager_ShouldGenerateScript()
        {
            var result = Execute(_handler, "createManager",
                ("saveManagerId", "test_sm"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("SaveManager");

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "saveManagerId");
        }

        [Test]
        public void CreateManager_GeneratedScriptClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "createManager",
                ("saveManagerId", "test_sm"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomSaveManager"));
            TrackCreatedGameObject("SaveManager");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomSaveManager");
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
