using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers.GameKit;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitAudioHandlerTests : GameKitHandlerTestBase
    {
        private GameKitAudioHandler _handler;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _handler = new GameKitAudioHandler();
        }

        [Test]
        public void Category_ReturnsGamekitAudio()
        {
            Assert.AreEqual("gamekitAudio", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("create", ops);
            Assert.Contains("update", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("delete", ops);
            Assert.Contains("setVolume", ops);
            Assert.Contains("setPitch", ops);
            Assert.Contains("setLoop", ops);
        }

        [Test]
        public void Execute_NullPayload_ReturnsError()
        {
            AssertError(_handler.Execute(null));
        }

        [Test]
        public void Execute_UnsupportedOperation_ReturnsError()
        {
            AssertError(Execute(_handler, "nonExistent"), "not supported");
        }

        [Test]
        public void Create_GeneratesScript()
        {
            var go = TrackGameObject(new GameObject("AudioTarget"));
            var result = Execute(_handler, "create",
                ("targetPath", "AudioTarget"),
                ("audioId", "test_audio"),
                ("audioType", "sfx"));
            AssertSuccess(result);
            AssertScriptGenerated(result);
        }

        [Test]
        public void Create_CustomClassName()
        {
            var go = TrackGameObject(new GameObject("AudioTarget2"));
            var result = Execute(_handler, "create",
                ("targetPath", "AudioTarget2"),
                ("audioId", "test_audio_2"),
                ("className", "MyAudio"),
                ("audioType", "music"));
            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyAudio");
        }
    }
}
