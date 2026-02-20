using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers.GameKit;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitUICommandHandlerTests : GameKitHandlerTestBase
    {
        private GameKitUICommandHandler _handler;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _handler = new GameKitUICommandHandler();
        }

        [Test]
        public void Category_ReturnsGamekitUICommand()
        {
            Assert.AreEqual("gamekitUICommand", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("createCommandPanel", ops);
            Assert.Contains("addCommand", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("delete", ops);
        }

        [Test]
        public void Execute_NullPayload_ReturnsError()
        {
            AssertError(_handler.Execute(null));
        }

        [Test]
        public void Execute_UnsupportedOperation_ReturnsError()
        {
            var result = Execute(_handler, "nonExistent");
            AssertError(result, "not supported");
        }

        [Test]
        public void CreateCommandPanel_GeneratesScript()
        {
            var go = TrackGameObject(new GameObject("UIPanel"));
            var result = Execute(_handler, "createCommandPanel",
                ("targetPath", "UIPanel"),
                ("panelId", "test_panel"),
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
        public void CreateCommandPanel_CustomClassName()
        {
            var go = TrackGameObject(new GameObject("UIPanel2"));
            var result = Execute(_handler, "createCommandPanel",
                ("targetPath", "UIPanel2"),
                ("panelId", "test_panel_2"),
                ("className", "MyCustomPanel"),
                ("commands", new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["name"] = "Move",
                        ["commandType"] = "action",
                        ["label"] = "Move"
                    }
                }));
            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomPanel");
        }

        [Test]
        public void CreateCommandPanel_RequiresCompilationWait()
        {
            AssertOperationsContain(_handler, "createCommandPanel");
        }
    }
}
