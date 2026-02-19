using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitSceneFlowHandlerTests : GameKitHandlerTestBase
    {
        private GameKitSceneFlowHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitSceneFlowHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitSceneFlow", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "inspect", "delete", "transition",
                "addScene", "removeScene", "updateScene",
                "addTransition", "removeTransition",
                "addSharedScene", "removeSharedScene");
        }

        #endregion

        #region Create

        [Test]
        public void Create_ShouldGenerateScript()
        {
            var result = Execute(_handler, "create",
                ("flowId", "test_flow"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("test_flow");

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "flowId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "create",
                ("flowId", "test_flow"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomSceneFlow"));
            TrackCreatedGameObject("test_flow");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomSceneFlow");
        }

        [Test]
        public void Create_WithTargetPath_ShouldAttachToExistingObject()
        {
            var go = CreateTestGameObject("FlowTarget");

            var result = Execute(_handler, "create",
                ("flowId", "test_flow_target"),
                ("targetPath", "FlowTarget"),
                ("outputPath", TestOutputDir));

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
