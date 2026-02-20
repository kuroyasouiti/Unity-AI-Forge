using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers.GameKit;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitUISelectionHandlerTests : GameKitHandlerTestBase
    {
        private GameKitUISelectionHandler _handler;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _handler = new GameKitUISelectionHandler();
        }

        [Test]
        public void Category_ReturnsGamekitUISelection()
        {
            Assert.AreEqual("gamekitUISelection", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("create", ops);
            Assert.Contains("update", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("delete", ops);
            Assert.Contains("selectItem", ops);
            Assert.Contains("clearSelection", ops);
        }

        [Test]
        public void Execute_NullPayload_ReturnsError()
        {
            AssertError(_handler.Execute(null));
        }

        [Test]
        public void Execute_UnsupportedOperation_ReturnsError()
        {
            AssertError(Execute(_handler, "nonExistent"), "not supported");
        }

        [Test]
        public void Create_GeneratesScript()
        {
            var go = TrackGameObject(new GameObject("SelectionTarget"));
            var result = Execute(_handler, "create",
                ("targetPath", "SelectionTarget"),
                ("selectionId", "test_selection"));
            AssertSuccess(result);
            AssertScriptGenerated(result);
        }
    }
}
