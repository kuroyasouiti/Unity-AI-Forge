using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitUIListHandlerTests : GameKitHandlerTestBase
    {
        private GameKitUIListHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitUIListHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitUIList", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete",
                "setItems", "addItem", "removeItem", "clear",
                "selectItem", "deselectItem", "clearSelection",
                "refreshFromSource", "findByListId",
                "createItemPrefab");
        }

        #endregion

        #region Create

        [Test]
        public void Create_ShouldGenerateScript()
        {
            CreateTestGameObject("ListParent");
            var result = Execute(_handler, "create",
                ("parentPath", "ListParent"),
                ("listId", "test_list"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("UIList");

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "listId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("ListParent");
            var result = Execute(_handler, "create",
                ("parentPath", "ListParent"),
                ("listId", "test_list"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomUIList"));
            TrackCreatedGameObject("UIList");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomUIList");
        }

        #endregion

        #region Error Handling

        [Test]
        public void Create_MissingTargetAndParentPath_ShouldReturnError()
        {
            var result = Execute(_handler, "create",
                ("listId", "test_list"),
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
