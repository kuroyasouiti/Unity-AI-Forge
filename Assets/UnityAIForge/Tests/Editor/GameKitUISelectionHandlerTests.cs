using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitUISelectionHandlerTests : GameKitHandlerTestBase
    {
        private GameKitUISelectionHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitUISelectionHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitUISelection", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete",
                "setItems", "addItem", "removeItem", "clear",
                "selectItem", "selectItemById", "deselectItem", "clearSelection",
                "setSelectionActions", "setItemEnabled",
                "findBySelectionId",
                "createItemPrefab");
        }

        #endregion

        #region Create

        [Test]
        public void Create_ShouldGenerateScript()
        {
            CreateTestGameObject("SelParent");
            var result = Execute(_handler, "create",
                ("parentPath", "SelParent"),
                ("selectionId", "test_selection"),
                ("selectionType", "radio"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("UISelection");

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "selectionId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("SelParent");
            var result = Execute(_handler, "create",
                ("parentPath", "SelParent"),
                ("selectionId", "test_selection"),
                ("selectionType", "radio"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomUISelection"));
            TrackCreatedGameObject("UISelection");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomUISelection");
        }

        #endregion

        #region Error Handling

        [Test]
        public void Create_MissingTargetAndParentPath_ShouldReturnError()
        {
            var result = Execute(_handler, "create",
                ("selectionId", "test_selection"),
                ("outputPath", TestOutputDir));
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
