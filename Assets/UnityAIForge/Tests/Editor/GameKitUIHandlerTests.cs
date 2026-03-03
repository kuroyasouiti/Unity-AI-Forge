using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers.HighLevel;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitUIHandlerTests : GameKitHandlerTestBase
    {
        private GameKitUIHandler _handler;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _handler = new GameKitUIHandler();
        }

        [Test]
        public void Category_ReturnsGamekitUI()
        {
            Assert.AreEqual("gamekitUI", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsAll33Ops()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.AreEqual(33, ops.Count, $"Expected 33 operations, got {ops.Count}: {string.Join(", ", ops)}");
        }

        [Test]
        public void SupportedOperations_ContainsCommandOps()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("createCommandPanel", ops);
            Assert.Contains("addCommand", ops);
        }

        [Test]
        public void SupportedOperations_ContainsBindingOps()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("setRange", ops);
            Assert.Contains("refresh", ops);
            Assert.Contains("findByBindingId", ops);
        }

        [Test]
        public void SupportedOperations_ContainsListOps()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("setItems", ops);
            Assert.Contains("refreshFromSource", ops);
            Assert.Contains("findByListId", ops);
        }

        [Test]
        public void SupportedOperations_ContainsSlotOps()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("createSlotBar", ops);
            Assert.Contains("findBySlotId", ops);
            Assert.Contains("findByBarId", ops);
        }

        [Test]
        public void SupportedOperations_ContainsSelectionOps()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("selectItemById", ops);
            Assert.Contains("setSelectionActions", ops);
            Assert.Contains("findBySelectionId", ops);
        }

        [Test]
        public void Execute_NullPayload_ReturnsError()
        {
            AssertError(_handler.Execute(null));
        }

        [Test]
        public void Execute_MissingWidgetType_ReturnsError()
        {
            var result = Execute(_handler, "create");
            AssertError(result, "widgetType");
        }

        [Test]
        public void Execute_InvalidWidgetType_ReturnsError()
        {
            var result = Execute(_handler, "create", ("widgetType", "invalid"));
            AssertError(result, "Unknown widgetType");
        }

        [Test]
        public void Execute_OperationNotSupportedByWidget_ReturnsError()
        {
            // "createSlotBar" is a slot-only operation, not supported by "command"
            var result = Execute(_handler, "createSlotBar", ("widgetType", "command"));
            AssertError(result, "not supported");
        }

        [Test]
        public void Execute_CommandWidget_CreateCommandPanel_GeneratesScript()
        {
            var go = TrackGameObject(new GameObject("CmdPanel"));
            var result = Execute(_handler, "createCommandPanel",
                ("widgetType", "command"),
                ("targetPath", "CmdPanel"),
                ("panelId", "test_cmd"),
                ("commands", new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["name"] = "Attack",
                        ["commandType"] = "action",
                        ["label"] = "Attack"
                    }
                }));
            AssertSuccess(result);
            AssertScriptGenerated(result);
        }

        [Test]
        public void Execute_ListWidget_Create_GeneratesScript()
        {
            var go = TrackGameObject(new GameObject("ListPanel"));
            var result = Execute(_handler, "create",
                ("widgetType", "list"),
                ("targetPath", "ListPanel"),
                ("listId", "test_list"),
                ("layout", "vertical"));
            AssertSuccess(result);
            AssertScriptGenerated(result);
        }
    }
}
