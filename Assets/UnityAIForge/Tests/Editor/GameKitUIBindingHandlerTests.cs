using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers.GameKit;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitUIBindingHandlerTests : GameKitHandlerTestBase
    {
        private GameKitUIBindingHandler _handler;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _handler = new GameKitUIBindingHandler();
        }

        [Test]
        public void Category_ReturnsGamekitUIBinding()
        {
            Assert.AreEqual("gamekitUIBinding", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("create", ops);
            Assert.Contains("update", ops);
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
            AssertError(Execute(_handler, "nonExistent"), "not supported");
        }

        [Test]
        public void Create_GeneratesScript()
        {
            var go = TrackGameObject(new GameObject("BindTarget"));
            var result = Execute(_handler, "create",
                ("targetPath", "BindTarget"),
                ("bindingId", "test_binding"),
                ("sourceType", "health"),
                ("sourceId", "player_hp"));
            AssertSuccess(result);
            AssertScriptGenerated(result);
        }

        [Test]
        public void Create_CustomClassName()
        {
            var go = TrackGameObject(new GameObject("BindTarget2"));
            var result = Execute(_handler, "create",
                ("targetPath", "BindTarget2"),
                ("bindingId", "test_binding_2"),
                ("className", "MyBinding"),
                ("sourceType", "health"),
                ("sourceId", "player_hp"));
            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyBinding");
        }
    }
}
