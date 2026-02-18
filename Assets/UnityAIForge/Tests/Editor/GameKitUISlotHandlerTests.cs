using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitUISlotHandlerTests : GameKitHandlerTestBase
    {
        private GameKitUISlotHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitUISlotHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitUISlot", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete",
                "setItem", "clearSlot", "setHighlight",
                "createSlotBar", "updateSlotBar", "inspectSlotBar", "deleteSlotBar",
                "useSlot", "refreshFromInventory",
                "findBySlotId", "findByBarId");
        }

        #endregion

        #region Create Slot

        [Test]
        public void Create_ShouldGenerateScript()
        {
            CreateTestGameObject("SlotParent");
            var result = Execute(_handler, "create",
                ("parentPath", "SlotParent"),
                ("slotId", "test_slot"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("UISlot");

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "slotId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("SlotParent");
            var result = Execute(_handler, "create",
                ("parentPath", "SlotParent"),
                ("slotId", "test_slot"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomUISlot"));
            TrackCreatedGameObject("UISlot");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomUISlot");
        }

        #endregion

        #region Create Slot Bar

        [Test]
        public void CreateSlotBar_ShouldGenerateScript()
        {
            CreateTestGameObject("BarParent");
            var result = Execute(_handler, "createSlotBar",
                ("parentPath", "BarParent"),
                ("barId", "test_bar"),
                ("slotCount", 4),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("UISlotBar");

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "barId");
        }

        #endregion

        #region Error Handling

        [Test]
        public void Create_MissingTargetAndParentPath_ShouldReturnError()
        {
            var result = Execute(_handler, "create",
                ("slotId", "test_slot"),
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
