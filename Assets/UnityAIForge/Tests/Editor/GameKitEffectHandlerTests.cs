using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers.GameKit;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitEffectHandlerTests : GameKitHandlerTestBase
    {
        private GameKitEffectHandler _handler;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _handler = new GameKitEffectHandler();
        }

        [Test]
        public void Category_ReturnsGamekitEffect()
        {
            Assert.AreEqual("gamekitEffect", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("create", ops);
            Assert.Contains("update", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("delete", ops);
            Assert.Contains("addComponent", ops);
            Assert.Contains("createManager", ops);
            Assert.Contains("play", ops);
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
            var go = TrackGameObject(new GameObject("EffectTarget"));
            var result = Execute(_handler, "create",
                ("targetPath", "EffectTarget"),
                ("effectId", "test_effect"));
            AssertSuccess(result);
            AssertScriptGenerated(result);
        }

        [Test]
        public void Create_CustomClassName()
        {
            var go = TrackGameObject(new GameObject("EffectTarget2"));
            var result = Execute(_handler, "create",
                ("targetPath", "EffectTarget2"),
                ("effectId", "test_effect_2"),
                ("className", "MyEffect"));
            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyEffect");
        }

        [Test]
        public void CreateManager_GeneratesScript()
        {
            var go = TrackGameObject(new GameObject("EffectMgr"));
            var result = Execute(_handler, "createManager",
                ("targetPath", "EffectMgr"),
                ("managerId", "test_manager"));
            AssertSuccess(result);
            AssertScriptGenerated(result);
        }
    }
}
