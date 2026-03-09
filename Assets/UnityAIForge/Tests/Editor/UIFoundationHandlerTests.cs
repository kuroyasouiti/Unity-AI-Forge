using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class UIFoundationHandlerTests
    {
        private UIFoundationHandler _handler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new UIFoundationHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown() => _tracker.Dispose();

        [Test]
        public void Category_ReturnsUIFoundation()
        {
            Assert.AreEqual("uiFoundation", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("createCanvas", ops);
            Assert.Contains("createPanel", ops);
            Assert.Contains("createButton", ops);
            Assert.Contains("createText", ops);
            Assert.Contains("createImage", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("inspectTree", ops);
            Assert.Contains("show", ops);
            Assert.Contains("hide", ops);
            Assert.Contains("toggle", ops);
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
        public void CreateCanvas_Default_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("createCanvas",
                ("name", "TestCanvas")));
            TestUtilities.AssertSuccess(result);
            var canvas = GameObject.Find("TestCanvas");
            if (canvas != null) _tracker.Track(canvas);
        }

        [Test]
        public void CreateButton_OnCanvas_ReturnsSuccess()
        {
            _handler.Execute(TestUtilities.CreatePayload("createCanvas", ("name", "BtnCanvas")));
            var canvas = GameObject.Find("BtnCanvas");
            if (canvas != null) _tracker.Track(canvas);

            var result = _handler.Execute(TestUtilities.CreatePayload("createButton",
                ("name", "TestButton"),
                ("parentPath", "BtnCanvas"),
                ("text", "Click Me")));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void CreatePanel_WithAddCanvasGroup_HasCanvasGroup()
        {
            _handler.Execute(TestUtilities.CreatePayload("createCanvas", ("name", "ACGCanvas")));
            var canvas = GameObject.Find("ACGCanvas");
            if (canvas != null) _tracker.Track(canvas);

            var result = _handler.Execute(TestUtilities.CreatePayload("createPanel",
                ("name", "ACGPanel"),
                ("parentPath", "ACGCanvas"),
                ("addCanvasGroup", true),
                ("alpha", 0.8),
                ("ignoreParentGroups", true)));
            TestUtilities.AssertSuccess(result);

            var panel = GameObject.Find("ACGCanvas/ACGPanel");
            Assert.IsNotNull(panel);
            var cg = panel.GetComponent<CanvasGroup>();
            Assert.IsNotNull(cg, "CanvasGroup should be added");
            Assert.AreEqual(0.8f, cg.alpha, 0.01f);
            Assert.IsTrue(cg.ignoreParentGroups);
        }

    }
}
