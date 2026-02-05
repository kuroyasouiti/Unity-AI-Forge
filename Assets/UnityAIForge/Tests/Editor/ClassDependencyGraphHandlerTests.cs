using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MCP.Editor.Handlers.HighLevel;
using MCP.Editor.Utilities.GraphAnalysis;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// ClassDependencyGraphHandler unit tests.
    /// Tests class dependency analysis functionality.
    /// </summary>
    [TestFixture]
    public class ClassDependencyGraphHandlerTests
    {
        private ClassDependencyGraphHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new ClassDependencyGraphHandler();
        }

        [TearDown]
        public void TearDown()
        {
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnClassDependencyGraph()
        {
            Assert.AreEqual("classDependencyGraph", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("analyzeClass", operations);
            Assert.Contains("analyzeAssembly", operations);
            Assert.Contains("analyzeNamespace", operations);
            Assert.Contains("findDependents", operations);
            Assert.Contains("findDependencies", operations);
        }

        #endregion

        #region AnalyzeClass Operation Tests

        [Test]
        public void Execute_AnalyzeClass_WithValidClass_ShouldReturnGraph()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "analyzeClass",
                ["target"] = "UnityEngine.MonoBehaviour",
                ["depth"] = 1,
                ["includeUnityTypes"] = true
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("classDependency", result["graphType"]);
            Assert.IsTrue(result.ContainsKey("nodes"));
            Assert.IsTrue(result.ContainsKey("edges"));
        }

        [Test]
        public void Execute_AnalyzeClass_WithInvalidClass_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "analyzeClass",
                ["target"] = "NonExistentClass.ThatDoesNotExist"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("error"));
        }

        [Test]
        public void Execute_AnalyzeClass_WithDotFormat_ShouldReturnDotString()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "analyzeClass",
                ["target"] = "UnityEngine.Component",
                ["depth"] = 1,
                ["includeUnityTypes"] = true,
                ["format"] = "dot"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("dot", result["format"]);
            Assert.IsTrue(result.ContainsKey("content"));
            var content = result["content"] as string;
            Assert.IsTrue(content.Contains("digraph"));
        }

        [Test]
        public void Execute_AnalyzeClass_WithMermaidFormat_ShouldReturnMermaidString()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "analyzeClass",
                ["target"] = "UnityEngine.Component",
                ["depth"] = 1,
                ["includeUnityTypes"] = true,
                ["format"] = "mermaid"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("mermaid", result["format"]);
            Assert.IsTrue(result.ContainsKey("content"));
            var content = result["content"] as string;
            Assert.IsTrue(content.Contains("graph TD"));
        }

        #endregion

        #region AnalyzeNamespace Operation Tests

        [Test]
        public void Execute_AnalyzeNamespace_WithValidNamespace_ShouldReturnGraph()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "analyzeNamespace",
                ["target"] = "MCP.Editor.Utilities.GraphAnalysis",
                ["depth"] = 1
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("classDependency", result["graphType"]);
            Assert.IsTrue(result.ContainsKey("nodes"));
        }

        #endregion

        #region FindDependencies Operation Tests

        [Test]
        public void Execute_FindDependencies_ShouldReturnDependencies()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "findDependencies",
                ["target"] = "UnityEngine.MonoBehaviour",
                ["depth"] = 1,
                ["includeUnityTypes"] = true
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("classDependency", result["graphType"]);
        }

        #endregion

        #region GraphResult Tests

        [Test]
        public void GraphResult_ToDictionary_ShouldContainRequiredFields()
        {
            var graphResult = new ClassDependencyResult();
            graphResult.AnalysisTarget = "TestClass";
            graphResult.Depth = 2;

            var node = new ClassGraphNode("TestNamespace.TestClass", "class")
            {
                FilePath = "Assets/Scripts/Test.cs",
                Namespace = "TestNamespace"
            };
            graphResult.AddNode(node);

            var edge = new ClassDependencyEdge("TestNamespace.TestClass", "TestNamespace.BaseClass", "inherits");
            graphResult.AddEdge(edge);

            var dict = graphResult.ToDictionary();

            Assert.AreEqual("classDependency", dict["graphType"]);
            Assert.AreEqual(1, dict["nodeCount"]);
            Assert.AreEqual(1, dict["edgeCount"]);
            Assert.AreEqual("TestClass", dict["analysisTarget"]);
            Assert.AreEqual(2, dict["depth"]);
        }

        #endregion
    }
}
