using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEditor;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class ScriptableObjectCommandHandlerTests
    {
        private ScriptableObjectCommandHandler _handler;
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _handler = new ScriptableObjectCommandHandler();
            _tempDir = TestUtilities.CreateTempAssetDirectory();
        }

        [TearDown]
        public void TearDown()
        {
            TestUtilities.CleanupTempAssetDirectory(_tempDir);
        }

        [Test]
        public void Category_ReturnsScriptableObject()
        {
            Assert.AreEqual("scriptableObjectManage", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("create", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("update", ops);
            Assert.Contains("delete", ops);
            Assert.Contains("findByType", ops);
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
        public void Execute_MissingOperation_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(new Dictionary<string, object>()));
        }
    }
}
