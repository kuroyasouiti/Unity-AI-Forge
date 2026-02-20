using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class CharacterControllerBundleHandlerTests
    {
        private CharacterControllerBundleHandler _handler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new CharacterControllerBundleHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown() => _tracker.Dispose();

        [Test]
        public void Category_ReturnsCharacterControllerBundle()
        {
            Assert.AreEqual("characterControllerBundle", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("applyPreset", ops);
            Assert.Contains("update", ops);
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
        public void ApplyPreset_ValidObject_ReturnsSuccess()
        {
            _tracker.Create("CharCtrl");
            var result = _handler.Execute(TestUtilities.CreatePayload("applyPreset",
                ("gameObjectPath", "CharCtrl"),
                ("preset", "firstPerson")));
            TestUtilities.AssertSuccess(result);
        }
    }
}
