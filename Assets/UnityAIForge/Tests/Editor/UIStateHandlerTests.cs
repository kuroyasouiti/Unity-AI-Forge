using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class UIStateHandlerTests
    {
        private UIStateHandler _handler;
        private UIFoundationHandler _uiHandler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new UIStateHandler();
            _uiHandler = new UIFoundationHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown() => _tracker.Dispose();

        [Test]
        public void Category_ReturnsUIState()
        {
            Assert.AreEqual("uiState", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("defineState", ops);
            Assert.Contains("applyState", ops);
            Assert.Contains("saveState", ops);
            Assert.Contains("loadState", ops);
            Assert.Contains("listStates", ops);
            Assert.Contains("createStateGroup", ops);
            Assert.Contains("transitionTo", ops);
            Assert.Contains("getActiveState", ops);
        }

        [Test]
        public void Execute_NullPayload_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(null));
        }

        [Test]
        public void Execute_UnsupportedOperation_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(TestUtilities.CreatePayload("nonExistent")), "not supported");
        }

        [Test]
        public void DefineState_ValidCanvas_ReturnsStateInfo()
        {
            _uiHandler.Execute(TestUtilities.CreatePayload("createCanvas", ("name", "StateCanvas")));
            var canvas = GameObject.Find("StateCanvas");
            if (canvas != null) _tracker.Track(canvas);

            _uiHandler.Execute(TestUtilities.CreatePayload("createPanel",
                ("name", "HUD"), ("parentPath", "StateCanvas")));

            var result = _handler.Execute(TestUtilities.CreatePayload("defineState",
                ("stateName", "gameplay"),
                ("rootPath", "StateCanvas"),
                ("elements", new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["path"] = "HUD",
                        ["visible"] = true
                    }
                }))) as Dictionary<string, object>;
            TestUtilities.AssertSuccess(result);

            Assert.AreEqual("gameplay", result["stateName"]);
            Assert.AreEqual("StateCanvas", result["rootPath"]);
            Assert.AreEqual(1, result["elementCount"]);
        }

        [Test]
        public void ListStates_ReturnsStateCount()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("listStates")) as Dictionary<string, object>;
            TestUtilities.AssertSuccess(result);
            Assert.IsTrue(result.ContainsKey("stateCount"));
            Assert.IsTrue(result.ContainsKey("states"));
        }
    }
}
