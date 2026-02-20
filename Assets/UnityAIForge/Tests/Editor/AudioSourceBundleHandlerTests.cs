using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class AudioSourceBundleHandlerTests
    {
        private AudioSourceBundleHandler _handler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new AudioSourceBundleHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown() => _tracker.Dispose();

        [Test]
        public void Category_ReturnsAudioSourceBundle()
        {
            Assert.AreEqual("audioSourceBundle", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("createAudioSource", ops);
            Assert.Contains("updateAudioSource", ops);
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
        public void CreateAudioSource_ValidObject_ReturnsSuccess()
        {
            _tracker.Create("AudioObj");
            var result = _handler.Execute(TestUtilities.CreatePayload("createAudioSource",
                ("gameObjectPath", "AudioObj")));
            TestUtilities.AssertSuccess(result);
        }
    }
}
