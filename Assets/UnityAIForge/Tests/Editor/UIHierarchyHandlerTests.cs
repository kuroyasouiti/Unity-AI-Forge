using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using MCP.Editor.Handlers;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// Unit tests for UIHierarchyHandler.
    /// </summary>
    [TestFixture]
    public class UIHierarchyHandlerTests
    {
        private UIHierarchyHandler _handler;
        private List<GameObject> _createdObjects;
        private Canvas _testCanvas;

        [SetUp]
        public void SetUp()
        {
            _handler = new UIHierarchyHandler();
            _createdObjects = new List<GameObject>();

            // Create a canvas for UI testing
            var canvasGo = new GameObject("TestCanvas");
            _testCanvas = canvasGo.AddComponent<Canvas>();
            _testCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
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
        public void Category_ShouldReturnUIHierarchy()
        {
            Assert.AreEqual("uiHierarchy", _handler.Category);
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
            Assert.Contains("createRow", operations);
            Assert.Contains("createColumn", operations);
            Assert.Contains("createGrid", operations);
            Assert.Contains("createCard", operations);
            Assert.Contains("createHeader", operations);
            Assert.Contains("createFooter", operations);
        }

        #endregion

        #region CreateRow Tests

        [Test]
        public void Execute_CreateRow_ShouldCreateHorizontalLayoutGroup()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createRow",
                ["name"] = "TestRow",
                ["parentPath"] = "TestCanvas"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var rowGo = GameObject.Find("TestRow");
            Assert.IsNotNull(rowGo);
            _createdObjects.Add(rowGo);

            var layoutGroup = rowGo.GetComponent<HorizontalLayoutGroup>();
            Assert.IsNotNull(layoutGroup);
        }

        [Test]
        public void Execute_CreateRow_WithSpacing_ShouldSetSpacing()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createRow",
                ["name"] = "SpacedRow",
                ["parentPath"] = "TestCanvas",
                ["spacing"] = 20f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var rowGo = GameObject.Find("SpacedRow");
            Assert.IsNotNull(rowGo);
            _createdObjects.Add(rowGo);

            var layoutGroup = rowGo.GetComponent<HorizontalLayoutGroup>();
            Assert.IsNotNull(layoutGroup);
            Assert.AreEqual(20f, layoutGroup.spacing, 0.01f);
        }

        #endregion

        #region CreateColumn Tests

        [Test]
        public void Execute_CreateColumn_ShouldCreateVerticalLayoutGroup()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createColumn",
                ["name"] = "TestColumn",
                ["parentPath"] = "TestCanvas"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var columnGo = GameObject.Find("TestColumn");
            Assert.IsNotNull(columnGo);
            _createdObjects.Add(columnGo);

            var layoutGroup = columnGo.GetComponent<VerticalLayoutGroup>();
            Assert.IsNotNull(layoutGroup);
        }

        #endregion

        #region CreateGrid Tests

        [Test]
        public void Execute_CreateGrid_ShouldCreateGridLayoutGroup()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createGrid",
                ["name"] = "TestGrid",
                ["parentPath"] = "TestCanvas"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var gridGo = GameObject.Find("TestGrid");
            Assert.IsNotNull(gridGo);
            _createdObjects.Add(gridGo);

            var gridLayout = gridGo.GetComponent<GridLayoutGroup>();
            Assert.IsNotNull(gridLayout);
        }

        [Test]
        public void Execute_CreateGrid_WithCellSize_ShouldSetCellSize()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createGrid",
                ["name"] = "SizedGrid",
                ["parentPath"] = "TestCanvas",
                ["cellSize"] = new Dictionary<string, object> { ["x"] = 100f, ["y"] = 80f }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var gridGo = GameObject.Find("SizedGrid");
            Assert.IsNotNull(gridGo);
            _createdObjects.Add(gridGo);

            var gridLayout = gridGo.GetComponent<GridLayoutGroup>();
            Assert.IsNotNull(gridLayout);
            Assert.AreEqual(100f, gridLayout.cellSize.x, 0.01f);
            Assert.AreEqual(80f, gridLayout.cellSize.y, 0.01f);
        }

        #endregion

        #region CreateCard Tests

        [Test]
        public void Execute_CreateCard_ShouldCreateCardStructure()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createCard",
                ["name"] = "TestCard",
                ["parentPath"] = "TestCanvas"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var cardGo = GameObject.Find("TestCard");
            Assert.IsNotNull(cardGo);
            _createdObjects.Add(cardGo);

            // Card should have an Image component
            Assert.IsNotNull(cardGo.GetComponent<Image>());
        }

        #endregion

        #region CreateHeader Tests

        [Test]
        public void Execute_CreateHeader_ShouldCreateHeaderElement()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createHeader",
                ["name"] = "TestHeader",
                ["parentPath"] = "TestCanvas",
                ["text"] = "Header Title"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var headerGo = GameObject.Find("TestHeader");
            Assert.IsNotNull(headerGo);
            _createdObjects.Add(headerGo);
        }

        #endregion

        #region CreateFooter Tests

        [Test]
        public void Execute_CreateFooter_ShouldCreateFooterElement()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createFooter",
                ["name"] = "TestFooter",
                ["parentPath"] = "TestCanvas"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var footerGo = GameObject.Find("TestFooter");
            Assert.IsNotNull(footerGo);
            _createdObjects.Add(footerGo);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Execute_MissingOperation_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["name"] = "TestElement"
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
        public void Execute_MissingParentPath_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createRow",
                ["name"] = "TestRow"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion
    }
}
