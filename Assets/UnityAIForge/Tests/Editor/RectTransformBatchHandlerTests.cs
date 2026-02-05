using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MCP.Editor.Handlers;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// Unit tests for RectTransformBatchHandler.
    /// </summary>
    [TestFixture]
    public class RectTransformBatchHandlerTests
    {
        private RectTransformBatchHandler _handler;
        private List<GameObject> _createdObjects;
        private Canvas _testCanvas;

        [SetUp]
        public void SetUp()
        {
            _handler = new RectTransformBatchHandler();
            _createdObjects = new List<GameObject>();

            // Create a canvas for UI testing
            var canvasGo = new GameObject("TestCanvas");
            _testCanvas = canvasGo.AddComponent<Canvas>();
            _testCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _createdObjects.Add(canvasGo);
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

        private GameObject CreateTestUIElement(string name, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent ?? _testCanvas.transform, false);
            _createdObjects.Add(go);
            return go;
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnRectTransformBatch()
        {
            Assert.AreEqual("rectTransformBatch", _handler.Category);
        }

        [Test]
        public void Version_ShouldReturn100()
        {
            Assert.AreEqual("1.0.0", _handler.Version);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = new List<string>(_handler.SupportedOperations);
            Assert.Contains("setAnchors", operations);
            Assert.Contains("setSize", operations);
            Assert.Contains("setPosition", operations);
            Assert.Contains("alignHorizontal", operations);
            Assert.Contains("alignVertical", operations);
            Assert.Contains("distributeHorizontal", operations);
            Assert.Contains("distributeVertical", operations);
        }

        #endregion

        #region SetAnchors Tests

        [Test]
        public void Execute_SetAnchors_ShouldSetAnchorValues()
        {
            var element = CreateTestUIElement("AnchorTest");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "setAnchors",
                ["gameObjectPaths"] = new List<object> { "TestCanvas/AnchorTest" },
                ["anchorMin"] = new Dictionary<string, object> { ["x"] = 0f, ["y"] = 0f },
                ["anchorMax"] = new Dictionary<string, object> { ["x"] = 1f, ["y"] = 1f }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var rectTransform = element.GetComponent<RectTransform>();
            Assert.AreEqual(0f, rectTransform.anchorMin.x, 0.01f);
            Assert.AreEqual(0f, rectTransform.anchorMin.y, 0.01f);
            Assert.AreEqual(1f, rectTransform.anchorMax.x, 0.01f);
            Assert.AreEqual(1f, rectTransform.anchorMax.y, 0.01f);
        }

        #endregion

        #region SetSize Tests

        [Test]
        public void Execute_SetSize_ShouldSetSizeDelta()
        {
            var element = CreateTestUIElement("SizeTest");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "setSize",
                ["gameObjectPaths"] = new List<object> { "TestCanvas/SizeTest" },
                ["size"] = new Dictionary<string, object> { ["x"] = 200f, ["y"] = 100f }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var rectTransform = element.GetComponent<RectTransform>();
            Assert.AreEqual(200f, rectTransform.sizeDelta.x, 0.01f);
            Assert.AreEqual(100f, rectTransform.sizeDelta.y, 0.01f);
        }

        #endregion

        #region SetPosition Tests

        [Test]
        public void Execute_SetPosition_ShouldSetAnchoredPosition()
        {
            var element = CreateTestUIElement("PositionTest");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "setPosition",
                ["gameObjectPaths"] = new List<object> { "TestCanvas/PositionTest" },
                ["position"] = new Dictionary<string, object> { ["x"] = 50f, ["y"] = 75f }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var rectTransform = element.GetComponent<RectTransform>();
            Assert.AreEqual(50f, rectTransform.anchoredPosition.x, 0.01f);
            Assert.AreEqual(75f, rectTransform.anchoredPosition.y, 0.01f);
        }

        #endregion

        #region Alignment Tests

        [Test]
        public void Execute_AlignHorizontal_Left_ShouldAlignElements()
        {
            var element1 = CreateTestUIElement("AlignH1");
            var element2 = CreateTestUIElement("AlignH2");

            var rt1 = element1.GetComponent<RectTransform>();
            var rt2 = element2.GetComponent<RectTransform>();
            rt1.anchoredPosition = new Vector2(100f, 0f);
            rt2.anchoredPosition = new Vector2(200f, 0f);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "alignHorizontal",
                ["gameObjectPaths"] = new List<object> { "TestCanvas/AlignH1", "TestCanvas/AlignH2" },
                ["alignment"] = "left"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        [Test]
        public void Execute_AlignVertical_Top_ShouldAlignElements()
        {
            var element1 = CreateTestUIElement("AlignV1");
            var element2 = CreateTestUIElement("AlignV2");

            var rt1 = element1.GetComponent<RectTransform>();
            var rt2 = element2.GetComponent<RectTransform>();
            rt1.anchoredPosition = new Vector2(0f, 100f);
            rt2.anchoredPosition = new Vector2(0f, 200f);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "alignVertical",
                ["gameObjectPaths"] = new List<object> { "TestCanvas/AlignV1", "TestCanvas/AlignV2" },
                ["alignment"] = "top"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion

        #region Distribution Tests

        [Test]
        public void Execute_DistributeHorizontal_ShouldDistributeElements()
        {
            var element1 = CreateTestUIElement("DistH1");
            var element2 = CreateTestUIElement("DistH2");
            var element3 = CreateTestUIElement("DistH3");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "distributeHorizontal",
                ["gameObjectPaths"] = new List<object> { "TestCanvas/DistH1", "TestCanvas/DistH2", "TestCanvas/DistH3" },
                ["spacing"] = 50f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        [Test]
        public void Execute_DistributeVertical_ShouldDistributeElements()
        {
            var element1 = CreateTestUIElement("DistV1");
            var element2 = CreateTestUIElement("DistV2");
            var element3 = CreateTestUIElement("DistV3");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "distributeVertical",
                ["gameObjectPaths"] = new List<object> { "TestCanvas/DistV1", "TestCanvas/DistV2", "TestCanvas/DistV3" },
                ["spacing"] = 50f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Execute_MissingOperation_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["gameObjectPaths"] = new List<object> { "SomeElement" }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        [Test]
        public void Execute_UnsupportedOperation_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "unsupportedOperation"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        [Test]
        public void Execute_EmptyGameObjectPaths_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "setSize",
                ["gameObjectPaths"] = new List<object>()
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion
    }
}
