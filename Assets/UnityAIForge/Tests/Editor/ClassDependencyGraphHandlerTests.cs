using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers.HighLevel;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class ClassDependencyGraphHandlerTests
    {
        private ClassDependencyGraphHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new ClassDependencyGraphHandler();
        }

        [Test]
        public void Category_ReturnsClassDependencyGraph()
        {
            Assert.AreEqual("classDependencyGraph", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("analyzeClass", ops);
            Assert.Contains("analyzeAssembly", ops);
            Assert.Contains("analyzeNamespace", ops);
            Assert.Contains("findDependents", ops);
            Assert.Contains("findDependencies", ops);
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
        public void AnalyzeAssembly_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("analyzeAssembly",
                ("target", "UnityAIForge.Editor")));
            TestUtilities.AssertSuccess(result);
        }
    }
}
