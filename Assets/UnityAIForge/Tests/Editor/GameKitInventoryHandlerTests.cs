using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitInventoryHandlerTests : GameKitHandlerTestBase
    {
        private GameKitInventoryHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitInventoryHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitInventory", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete",
                "defineItem", "updateItem", "inspectItem", "deleteItem",
                "addItem", "removeItem", "useItem",
                "equip", "unequip", "getEquipped",
                "clear", "sort",
                "findByInventoryId", "findByItemId");
        }

        #endregion

        #region Create Inventory

        [Test]
        public void Create_ShouldGenerateScript()
        {
            CreateTestGameObject("InvTarget");
            var result = Execute(_handler, "create",
                ("gameObjectPath", "InvTarget"),
                ("inventoryId", "test_inv"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "inventoryId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("InvTarget");
            var result = Execute(_handler, "create",
                ("gameObjectPath", "InvTarget"),
                ("inventoryId", "test_inv"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomInventory"));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomInventory");
        }

        [Test]
        public void Create_MissingGameObjectPath_ShouldReturnError()
        {
            var result = Execute(_handler, "create",
                ("inventoryId", "test_inv"),
                ("outputPath", TestOutputDir));
            AssertError(result);
        }

        #endregion

        #region Define Item

        [Test]
        public void DefineItem_ShouldGenerateScript()
        {
            var result = Execute(_handler, "defineItem",
                ("itemId", "test_item"),
                ("displayName", "Test Item"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "itemId");
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
