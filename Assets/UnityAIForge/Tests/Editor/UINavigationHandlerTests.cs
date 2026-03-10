using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class UINavigationHandlerTests
    {
        private UINavigationHandler _handler;
        private UIFoundationHandler _uiHandler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new UINavigationHandler();
            _uiHandler = new UIFoundationHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown() => _tracker.Dispose();

        [Test]
        public void Category_ReturnsUINavigation()
        {
            Assert.AreEqual("uiNavigation", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("configure", ops);
            Assert.Contains("setExplicit", ops);
            Assert.Contains("autoSetup", ops);
            Assert.Contains("createGroup", ops);
            Assert.Contains("setFirstSelected", ops);
            Assert.Contains("inspect", ops);
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
        public void AutoSetup_CanvasWithButtons_ConfiguresNavigation()
        {
            _uiHandler.Execute(TestUtilities.CreatePayload("createCanvas", ("name", "NavCanvas")));
            var canvas = GameObject.Find("NavCanvas");
            if (canvas != null) _tracker.Track(canvas);

            _uiHandler.Execute(TestUtilities.CreatePayload("createButton",
                ("name", "NavBtn1"), ("parentPath", "NavCanvas"), ("text", "A")));
            _uiHandler.Execute(TestUtilities.CreatePayload("createButton",
                ("name", "NavBtn2"), ("parentPath", "NavCanvas"), ("text", "B")));

            var result = _handler.Execute(TestUtilities.CreatePayload("autoSetup",
                ("rootPath", "NavCanvas"),
                ("direction", "vertical"))) as Dictionary<string, object>;
            TestUtilities.AssertSuccess(result);

            Assert.IsTrue((int)result["configuredCount"] >= 2);
        }
    }
}
