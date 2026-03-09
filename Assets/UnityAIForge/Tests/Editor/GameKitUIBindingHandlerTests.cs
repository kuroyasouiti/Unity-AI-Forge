using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers.HighLevel;
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
            Assert.Contains("createMultiple", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("setRange", ops);
            Assert.Contains("refresh", ops);
            Assert.Contains("findByBindingId", ops);
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

        #region CreateMultiple

        [Test]
        public void CreateMultiple_MissingBindings_ReturnsError()
        {
            var go = TrackGameObject(new GameObject("BindMultiErr"));
            var result = Execute(_handler, "createMultiple",
                ("targetPath", "BindMultiErr"));
            AssertError(result, "bindings");
        }

        [Test]
        public void CreateMultiple_EmptyBindings_ReturnsError()
        {
            var go = TrackGameObject(new GameObject("BindMultiEmpty"));
            var result = Execute(_handler, "createMultiple",
                ("targetPath", "BindMultiEmpty"),
                ("bindings", new List<object>()));
            AssertError(result, "bindings");
        }

        [Test]
        public void CreateMultiple_CreatesAllBindings()
        {
            var go = TrackGameObject(new GameObject("BindMultiTarget"));
            var bindings = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["bindingId"] = "multi_bind_1",
                    ["sourceType"] = "health",
                    ["sourceId"] = "player_hp"
                },
                new Dictionary<string, object>
                {
                    ["bindingId"] = "multi_bind_2",
                    ["sourceType"] = "economy",
                    ["sourceId"] = "gold_mgr",
                    ["targetProperty"] = "gold"
                }
            };
            var result = Execute(_handler, "createMultiple",
                ("targetPath", "BindMultiTarget"),
                ("bindings", bindings));
            AssertSuccess(result);
            var dict = result as Dictionary<string, object>;
            Assert.IsNotNull(dict);
            Assert.IsTrue(dict.ContainsKey("createdCount"));
            Assert.AreEqual(2, dict["createdCount"]);

            // Track generated scripts for cleanup
            var created = dict["created"] as List<Dictionary<string, object>>;
            if (created != null)
            {
                foreach (var item in created)
                {
                    if (item.ContainsKey("scriptPath"))
                        TrackScriptPath(item["scriptPath"]?.ToString());
                }
            }
        }

        #endregion
    }
}
