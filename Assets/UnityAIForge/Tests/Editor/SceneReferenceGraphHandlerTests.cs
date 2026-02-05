using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MCP.Editor.Handlers.HighLevel;
using MCP.Editor.Utilities.GraphAnalysis;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// SceneReferenceGraphHandler unit tests.
    /// Tests scene object reference analysis functionality.
    /// </summary>
    [TestFixture]
    public class SceneReferenceGraphHandlerTests
    {
        private SceneReferenceGraphHandler _handler;
        private List<GameObject> _createdObjects;
        private Scene _testScene;

        [SetUp]
        public void SetUp()
        {
            _handler = new SceneReferenceGraphHandler();
            _createdObjects = new List<GameObject>();
            _testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    Undo.ClearUndo(obj);
                    Object.DestroyImmediate(obj);
                }
            }
            _createdObjects.Clear();
        }

        private GameObject CreateTestGameObject(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent);
            }
            _createdObjects.Add(go);
            return go;
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnSceneReferenceGraph()
        {
            Assert.AreEqual("sceneReferenceGraph", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("analyzeScene", operations);
            Assert.Contains("analyzeObject", operations);
            Assert.Contains("findReferencesTo", operations);
            Assert.Contains("findReferencesFrom", operations);
            Assert.Contains("findOrphans", operations);
        }

        #endregion

        #region AnalyzeScene Operation Tests

        [Test]
        public void Execute_AnalyzeScene_WithEmptyScene_ShouldReturnEmptyGraph()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "analyzeScene"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("sceneReference", result["graphType"]);
            Assert.IsTrue(result.ContainsKey("nodes"));
            Assert.IsTrue(result.ContainsKey("edges"));
        }

        [Test]
        public void Execute_AnalyzeScene_WithHierarchy_ShouldIncludeHierarchyEdges()
        {
            var parent = CreateTestGameObject("Parent");
            var child = CreateTestGameObject("Child", parent.transform);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "analyzeScene",
                ["includeHierarchy"] = true
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var nodes = result["nodes"];
            Assert.IsNotNull(nodes);

            var edges = result["edges"];
            Assert.IsNotNull(edges);

            // Convert to appropriate types for checking
            var edgesList = edges as IEnumerable<object>;
            if (edgesList != null)
            {
                var edgeCount = edgesList.Count();
                Assert.GreaterOrEqual(edgeCount, 1);

                // Check for hierarchy edge
                var hasHierarchyEdge = edgesList
                    .OfType<Dictionary<string, object>>()
                    .Any(e => e.ContainsKey("relation") && e["relation"] as string == "hierarchy_child");
                Assert.IsTrue(hasHierarchyEdge);
            }
        }

        [Test]
        public void Execute_AnalyzeScene_WithDotFormat_ShouldReturnDotString()
        {
            var go = CreateTestGameObject("TestObject");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "analyzeScene",
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

        #endregion

        #region AnalyzeObject Operation Tests

        [Test]
        public void Execute_AnalyzeObject_WithValidPath_ShouldReturnGraph()
        {
            var go = CreateTestGameObject("TestObject");
            var child = CreateTestGameObject("Child", go.transform);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "analyzeObject",
                ["objectPath"] = "TestObject",
                ["includeChildren"] = true
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("sceneReference", result["graphType"]);
        }

        [Test]
        public void Execute_AnalyzeObject_WithInvalidPath_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "analyzeObject",
                ["objectPath"] = "NonExistentObject"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("error"));
        }

        [Test]
        public void Execute_AnalyzeObject_WithoutPath_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "analyzeObject"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("error"));
        }

        #endregion

        #region FindOrphans Operation Tests

        [Test]
        public void Execute_FindOrphans_ShouldIdentifyUnreferencedObjects()
        {
            // Create orphan objects (not referenced by anything)
            var orphan1 = CreateTestGameObject("Orphan1");
            var orphan2 = CreateTestGameObject("Orphan2");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "findOrphans"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("sceneReference", result["graphType"]);

            // Orphans should be identified
            Assert.IsTrue(result.ContainsKey("orphans"));
        }

        #endregion

        #region GraphResult Tests

        [Test]
        public void SceneReferenceResult_ToDictionary_ShouldContainRequiredFields()
        {
            var result = new SceneReferenceResult();
            result.ScenePath = "Assets/Scenes/Test.unity";

            var node = new SceneObjectNode("/Player", "/Player")
            {
                InstanceId = 12345,
                Components = new List<string> { "Transform", "PlayerController" }
            };
            result.AddNode(node);

            var edge = new SceneReferenceEdge("/GameManager", "/Player", "component_reference")
            {
                SourceComponent = "GameManager",
                SourceField = "player"
            };
            result.AddEdge(edge);

            var dict = result.ToDictionary();

            Assert.AreEqual("sceneReference", dict["graphType"]);
            Assert.AreEqual(1, dict["nodeCount"]);
            Assert.AreEqual(1, dict["edgeCount"]);
            Assert.AreEqual("Assets/Scenes/Test.unity", dict["scenePath"]);
        }

        [Test]
        public void SceneObjectNode_ToDictionary_ShouldIncludeAllProperties()
        {
            var node = new SceneObjectNode("/Player", "/Player")
            {
                InstanceId = 12345,
                IsPrefabInstance = true,
                PrefabAsset = "Assets/Prefabs/Player.prefab",
                Components = new List<string> { "Transform", "Rigidbody" }
            };

            var dict = node.ToDictionary();

            Assert.AreEqual("/Player", dict["id"]);
            Assert.AreEqual("/Player", dict["path"]);
            Assert.AreEqual(12345, dict["instanceId"]);
            Assert.IsTrue((bool)dict["isPrefabInstance"]);
            Assert.AreEqual("Assets/Prefabs/Player.prefab", dict["prefabAsset"]);
        }

        #endregion
    }
}
