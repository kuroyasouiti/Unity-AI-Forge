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
    }
}
