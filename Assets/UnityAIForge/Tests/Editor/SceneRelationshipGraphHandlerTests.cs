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
    /// SceneRelationshipGraphHandler unit tests.
    /// Tests scene relationship analysis functionality.
    /// </summary>
    [TestFixture]
    public class SceneRelationshipGraphHandlerTests
    {
        private SceneRelationshipGraphHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new SceneRelationshipGraphHandler();
        }

        [TearDown]
        public void TearDown()
        {
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnSceneRelationshipGraph()
        {
            Assert.AreEqual("sceneRelationshipGraph", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("analyzeAll", operations);
            Assert.Contains("analyzeScene", operations);
            Assert.Contains("findTransitionsTo", operations);
            Assert.Contains("findTransitionsFrom", operations);
            Assert.Contains("validateBuildSettings", operations);
        }

        #endregion

        #region AnalyzeAll Operation Tests

        [Test]
        public void Execute_AnalyzeAll_ShouldReturnGraph()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "analyzeAll"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("sceneRelationship", result["graphType"]);
            Assert.IsTrue(result.ContainsKey("nodes"));
            Assert.IsTrue(result.ContainsKey("edges"));
            Assert.IsTrue(result.ContainsKey("buildOrder"));
        }

        [Test]
        public void Execute_AnalyzeAll_WithDotFormat_ShouldReturnDotString()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "analyzeAll",
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
        public void Execute_AnalyzeAll_WithMermaidFormat_ShouldReturnMermaidString()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "analyzeAll",
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

        [Test]
        public void Execute_AnalyzeAll_WithSummaryFormat_ShouldReturnSummaryString()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "analyzeAll",
                ["format"] = "summary"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("summary", result["format"]);
            Assert.IsTrue(result.ContainsKey("content"));
            var content = result["content"] as string;
            Assert.IsTrue(content.Contains("Graph Type:"));
        }

        #endregion

        #region ValidateBuildSettings Operation Tests

        [Test]
        public void Execute_ValidateBuildSettings_ShouldReturnValidationResult()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "validateBuildSettings"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("sceneRelationship", result["graphType"]);
            Assert.IsTrue(result.ContainsKey("isValid"));
            Assert.IsTrue(result.ContainsKey("issueCount"));
        }

        #endregion

        #region AnalyzeScene Operation Tests

        [Test]
        public void Execute_AnalyzeScene_WithoutPath_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "analyzeScene"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("error"));
        }

        #endregion

        #region GraphResult Tests

        [Test]
        public void SceneRelationshipResult_ToDictionary_ShouldContainRequiredFields()
        {
            var result = new SceneRelationshipResult();
            result.BuildOrder = new List<string> { "Assets/Scenes/Menu.unity", "Assets/Scenes/Game.unity" };
            result.UnregisteredScenes = new List<string> { "Assets/Scenes/Test.unity" };

            var node = new SceneNode("Assets/Scenes/Menu.unity")
            {
                BuildIndex = 0,
                InBuildSettings = true
            };
            result.AddNode(node);

            var edge = new SceneTransitionEdge("Assets/Scenes/Menu.unity", "Assets/Scenes/Game.unity", "scene_load")
            {
                LoadType = "single",
                CallerScript = "Assets/Scripts/MenuController.cs",
                CallerLine = 42
            };
            result.AddEdge(edge);

            var dict = result.ToDictionary();

            Assert.AreEqual("sceneRelationship", dict["graphType"]);
            Assert.AreEqual(1, dict["nodeCount"]);
            Assert.AreEqual(1, dict["edgeCount"]);
            Assert.IsTrue(dict.ContainsKey("buildOrder"));
            Assert.IsTrue(dict.ContainsKey("unregisteredScenes"));
        }

        [Test]
        public void SceneNode_ToDictionary_ShouldIncludeAllProperties()
        {
            var node = new SceneNode("Assets/Scenes/MainMenu.unity")
            {
                BuildIndex = 0,
                InBuildSettings = true,
                IsAddressable = false
            };

            var dict = node.ToDictionary();

            Assert.AreEqual("Assets/Scenes/MainMenu.unity", dict["id"]);
            Assert.AreEqual("Scene", dict["type"]);
            Assert.AreEqual("MainMenu", dict["name"]);
            Assert.AreEqual(0, dict["buildIndex"]);
            Assert.IsTrue((bool)dict["inBuildSettings"]);
            Assert.IsFalse((bool)dict["isAddressable"]);
        }

        [Test]
        public void SceneTransitionEdge_ToDictionary_ShouldIncludeAllDetails()
        {
            var edge = new SceneTransitionEdge("Assets/Scenes/Menu.unity", "Assets/Scenes/Level1.unity", "scene_load")
            {
                LoadType = "additive",
                CallerScript = "Assets/Scripts/LevelLoader.cs",
                CallerLine = 25
            };

            var dict = edge.ToDictionary();

            Assert.AreEqual("Assets/Scenes/Menu.unity", dict["source"]);
            Assert.AreEqual("Assets/Scenes/Level1.unity", dict["target"]);
            Assert.AreEqual("scene_load", dict["relation"]);
            Assert.AreEqual("additive", dict["loadType"]);
            Assert.AreEqual("Assets/Scripts/LevelLoader.cs", dict["callerScript"]);
            Assert.AreEqual(25, dict["callerLine"]);
        }

        [Test]
        public void GraphResult_ToMermaid_ShouldGenerateValidMermaidSyntax()
        {
            var result = new SceneRelationshipResult();

            var node1 = new SceneNode("Assets/Scenes/Menu.unity");
            var node2 = new SceneNode("Assets/Scenes/Game.unity");
            result.AddNode(node1);
            result.AddNode(node2);

            var edge = new SceneTransitionEdge("Assets/Scenes/Menu.unity", "Assets/Scenes/Game.unity", "scene_load");
            result.AddEdge(edge);

            var mermaid = result.ToMermaid();

            Assert.IsTrue(mermaid.Contains("graph TD"));
            Assert.IsTrue(mermaid.Contains("N0"));
            Assert.IsTrue(mermaid.Contains("N1"));
            Assert.IsTrue(mermaid.Contains("-->"));
        }

        [Test]
        public void GraphResult_ToDot_ShouldGenerateValidDotSyntax()
        {
            var result = new SceneRelationshipResult();

            var node = new SceneNode("Assets/Scenes/Menu.unity");
            result.AddNode(node);

            var dot = result.ToDot("TestGraph");

            Assert.IsTrue(dot.Contains("digraph TestGraph"));
            Assert.IsTrue(dot.Contains("rankdir=LR"));
            Assert.IsTrue(dot.Contains("node [shape=box]"));
        }

        #endregion
    }
}
