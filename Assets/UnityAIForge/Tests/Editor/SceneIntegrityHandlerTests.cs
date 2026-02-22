using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers.HighLevel;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class SceneIntegrityHandlerTests
    {
        private SceneIntegrityHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new SceneIntegrityHandler();
        }

        [Test]
        public void Category_ReturnsSceneIntegrity()
        {
            Assert.AreEqual("sceneIntegrity", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("missingScripts", ops);
            Assert.Contains("nullReferences", ops);
            Assert.Contains("brokenEvents", ops);
            Assert.Contains("brokenPrefabs", ops);
            Assert.Contains("removeMissingScripts", ops);
            Assert.Contains("all", ops);
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
        public void All_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("all"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void MissingScripts_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("missingScripts"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void RemoveMissingScripts_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("removeMissingScripts"));
            TestUtilities.AssertSuccess(result);
        }
    }
}
