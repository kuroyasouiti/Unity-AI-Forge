using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class Sprite2DBundleHandlerTests
    {
        private Sprite2DBundleHandler _handler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new Sprite2DBundleHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown() => _tracker.Dispose();

        [Test]
        public void Category_ReturnsSprite2DBundle()
        {
            Assert.AreEqual("sprite2DBundle", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("createSprite", ops);
            Assert.Contains("updateSprite", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("setColor", ops);
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
        public void CreateSprite_ValidObject_ReturnsSuccess()
        {
            _tracker.Create("SpriteObj");
            var result = _handler.Execute(TestUtilities.CreatePayload("createSprite",
                ("gameObjectPath", "SpriteObj")));
            TestUtilities.AssertSuccess(result);
        }
    }
}
