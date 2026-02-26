using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers.HighLevel;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class ScriptSyntaxHandlerTests
    {
        private ScriptSyntaxHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new ScriptSyntaxHandler();
        }

        [Test]
        public void Category_ReturnsScriptSyntax()
        {
            Assert.AreEqual("scriptSyntax", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("analyzeScript", ops);
            Assert.Contains("findReferences", ops);
            Assert.Contains("findUnusedCode", ops);
            Assert.Contains("analyzeMetrics", ops);
            Assert.AreEqual(4, ops.Count);
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
        public void AnalyzeScript_MissingScriptPath_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(TestUtilities.CreatePayload("analyzeScript")), "scriptPath");
        }

        [Test]
        public void AnalyzeScript_ValidPath_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("analyzeScript",
                ("scriptPath", "Assets/UnityAIForge/Editor/MCPBridge/Handlers/HighLevel/ScriptSyntaxHandler.cs")));
            TestUtilities.AssertSuccess(result);
            var dict = result as Dictionary<string, object>;
            Assert.IsTrue(dict.ContainsKey("types") || dict.ContainsKey("namespaces") || dict.ContainsKey("scriptPath"));
        }

        [Test]
        public void FindReferences_MissingSymbolName_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(TestUtilities.CreatePayload("findReferences")), "symbolName");
        }

        [Test]
        public void FindReferences_ValidSymbol_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("findReferences",
                ("symbolName", "ScriptSyntaxHandler"),
                ("searchPath", "Assets/UnityAIForge/Editor/MCPBridge/Handlers/HighLevel")));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void FindUnusedCode_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("findUnusedCode",
                ("searchPath", "Assets/UnityAIForge/Editor/MCPBridge/Handlers/HighLevel")));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void FindUnusedCode_WithTargetType_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("findUnusedCode",
                ("searchPath", "Assets/UnityAIForge/Editor/MCPBridge/Handlers/HighLevel"),
                ("targetType", "method")));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void AnalyzeMetrics_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("analyzeMetrics",
                ("scriptPath", "Assets/UnityAIForge/Editor/MCPBridge/Handlers/HighLevel/ScriptSyntaxHandler.cs")));
            TestUtilities.AssertSuccess(result);
        }
    }
}
