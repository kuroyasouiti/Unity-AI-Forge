using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class ParticleBundleHandlerTests
    {
        private ParticleBundleHandler _handler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new ParticleBundleHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown() => _tracker.Dispose();

        [Test]
        public void Category_ReturnsParticleBundle()
        {
            Assert.AreEqual("particleBundle", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("create", ops);
            Assert.Contains("update", ops);
            Assert.Contains("applyPreset", ops);
            Assert.Contains("play", ops);
            Assert.Contains("stop", ops);
            Assert.Contains("pause", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("listPresets", ops);
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
        public void Create_DefaultParticle_HasParticleSystem()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("create",
                ("name", "TestParticle"))) as Dictionary<string, object>;
            TestUtilities.AssertSuccess(result);

            Assert.IsTrue(result.ContainsKey("gameObjectPath"));
            var go = GameObject.Find(result["gameObjectPath"].ToString());
            Assert.IsNotNull(go, "Particle GameObject should exist");
            Assert.IsNotNull(go.GetComponent<ParticleSystem>(), "ParticleSystem component should exist");
            _tracker.Track(go);
        }

        [Test]
        public void Inspect_CreatedParticle_ReturnsSuccess()
        {
            var go = _tracker.Create("InspectPS");
            go.AddComponent<ParticleSystem>();
            var result = _handler.Execute(TestUtilities.CreatePayload("inspect",
                ("gameObjectPath", "InspectPS")));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void ListPresets_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("listPresets"));
            TestUtilities.AssertSuccess(result);
        }
    }
}
