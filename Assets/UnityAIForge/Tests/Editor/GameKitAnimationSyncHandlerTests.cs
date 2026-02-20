using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers.GameKit;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitAnimationSyncHandlerTests : GameKitHandlerTestBase
    {
        private GameKitAnimationSyncHandler _handler;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _handler = new GameKitAnimationSyncHandler();
        }

        [Test]
        public void Category_ReturnsGamekitAnimationSync()
        {
            Assert.AreEqual("gamekitAnimationSync", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("create", ops);
            Assert.Contains("update", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("delete", ops);
            Assert.Contains("addSyncRule", ops);
            Assert.Contains("fireTrigger", ops);
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
            var go = TrackGameObject(new GameObject("AnimSyncTarget"));
            go.AddComponent<Animator>();
            var result = Execute(_handler, "create",
                ("targetPath", "AnimSyncTarget"),
                ("syncId", "test_sync"));
            AssertSuccess(result);
            AssertScriptGenerated(result);
        }
    }
}
