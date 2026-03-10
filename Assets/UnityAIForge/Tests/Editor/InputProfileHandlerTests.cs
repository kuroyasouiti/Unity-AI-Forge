using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class InputProfileHandlerTests
    {
        private InputProfileHandler _handler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new InputProfileHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown() => _tracker.Dispose();

        [Test]
        public void Category_ReturnsInputProfile()
        {
            Assert.AreEqual("inputProfile", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("createPlayerInput", ops);
            Assert.Contains("createInputActions", ops);
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
        public void CreatePlayerInput_ValidObject_ReturnsSuccess()
        {
            var go = _tracker.Create("InputTarget");
            var result = _handler.Execute(TestUtilities.CreatePayload("createPlayerInput",
                ("gameObjectPath", "InputTarget")));
            TestUtilities.AssertSuccess(result);
        }
    }
}
