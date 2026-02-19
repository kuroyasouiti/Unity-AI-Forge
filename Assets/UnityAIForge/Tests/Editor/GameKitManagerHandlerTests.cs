using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitManagerHandlerTests : GameKitHandlerTestBase
    {
        private GameKitManagerHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitManagerHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitManager", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete");
        }

        #endregion

        #region Create

        [Test]
        public void Create_ShouldGenerateScript()
        {
            var result = Execute(_handler, "create",
                ("managerId", "test_manager"),
                ("managerType", "turnBased"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("test_manager");

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "managerId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "create",
                ("managerId", "test_manager"),
                ("managerType", "realtime"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomManager"));
            TrackCreatedGameObject("test_manager");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomManager");
        }

        [Test]
        public void Create_MissingManagerType_ShouldStillSucceed()
        {
            var result = Execute(_handler, "create",
                ("managerId", "test_manager_default"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("test_manager_default");

            AssertSuccess(result);
            AssertScriptGenerated(result);
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
