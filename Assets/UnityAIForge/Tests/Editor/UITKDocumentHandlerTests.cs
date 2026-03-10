using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class UITKDocumentHandlerTests
    {
        private UITKDocumentHandler _handler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new UITKDocumentHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown() => _tracker.Dispose();

        [Test]
        public void Category_ReturnsUitkDocument()
        {
            Assert.AreEqual("uitkDocument", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("create", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("query", ops);
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
        public void Create_Default_HasUIDocumentComponent()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("create",
                ("name", "TestUIDoc"))) as Dictionary<string, object>;
            TestUtilities.AssertSuccess(result);

            Assert.IsTrue(result.ContainsKey("gameObjectPath"));
            var go = GameObject.Find("TestUIDoc");
            Assert.IsNotNull(go);
            Assert.IsNotNull(go.GetComponent<UIDocument>(), "UIDocument component should exist");
            _tracker.Track(go);
        }
    }
}
