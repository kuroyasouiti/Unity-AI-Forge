using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitUICommandHandlerTests : GameKitHandlerTestBase
    {
        private GameKitUICommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitUICommandHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitUICommand", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "createCommandPanel", "addCommand", "inspect", "delete");
        }

        #endregion

        #region Create

        [Test]
        public void CreateCommandPanel_ShouldGenerateScript()
        {
            CreateTestGameObject("CmdParent");
            var result = Execute(_handler, "createCommandPanel",
                ("parentPath", "CmdParent"),
                ("panelId", "test_panel"),
                ("uiOutputDir", TestOutputDir),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("test_panel");

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "panelId");
            AssertHasField(result, "uxmlPath");
            AssertHasField(result, "ussPath");
        }

        [Test]
        public void CreateCommandPanel_GeneratedScriptClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("CmdParent");
            var result = Execute(_handler, "createCommandPanel",
                ("parentPath", "CmdParent"),
                ("panelId", "test_panel"),
                ("uiOutputDir", TestOutputDir),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomUICommand"));
            TrackCreatedGameObject("test_panel");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomUICommand");
        }

        [Test]
        public void CreateCommandPanel_WithCommands_ShouldGenerateScript()
        {
            CreateTestGameObject("CmdParent");
            var commands = new List<object>
            {
                new Dictionary<string, object>
                {
                    { "name", "Attack" },
                    { "label", "Attack" },
                    { "commandType", "action" }
                }
            };
            var result = Execute(_handler, "createCommandPanel",
                ("parentPath", "CmdParent"),
                ("panelId", "test_panel_cmds"),
                ("commands", commands),
                ("uiOutputDir", TestOutputDir),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("test_panel_cmds");

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
