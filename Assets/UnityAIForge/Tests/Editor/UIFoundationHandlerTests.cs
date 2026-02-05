using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MCP.Editor.Handlers;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// Unit tests for UIFoundationHandler.
    /// </summary>
    [TestFixture]
    public class UIFoundationHandlerTests
    {
        private UIFoundationHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new UIFoundationHandler();
            _createdObjects = new List<GameObject>();
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

            // Also clean up any EventSystem that was created
            var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            foreach (var es in eventSystems)
            {
                if (es.gameObject.name == "EventSystem")
                {
                    Object.DestroyImmediate(es.gameObject);
                }
            }
        }

        private void TrackCreatedObject(string path)
        {
            var name = path.Split('/').Last();
            var go = GameObject.Find(name);
            if (go != null && !_createdObjects.Contains(go))
            {
                _createdObjects.Add(go);
            }
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnUIFoundation()
        {
            Assert.AreEqual("uiFoundation", _handler.Category);
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
            Assert.Contains("createCanvas", operations);
            Assert.Contains("createPanel", operations);
            Assert.Contains("createButton", operations);
            Assert.Contains("createText", operations);
            Assert.Contains("createImage", operations);
            Assert.Contains("createInputField", operations);
            Assert.Contains("createScrollView", operations);
            Assert.Contains("addLayoutGroup", operations);
        }

        #endregion

        #region CreateCanvas Tests

        [Test]
        public void Execute_CreateCanvas_ShouldCreateCanvasWithComponents()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createCanvas",
                ["name"] = "TestCanvas"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("path"));

            var canvasGo = GameObject.Find("TestCanvas");
            Assert.IsNotNull(canvasGo);
            _createdObjects.Add(canvasGo);

            Assert.IsNotNull(canvasGo.GetComponent<Canvas>());
            Assert.IsNotNull(canvasGo.GetComponent<CanvasScaler>());
            Assert.IsNotNull(canvasGo.GetComponent<GraphicRaycaster>());
        }

        [Test]
        public void Execute_CreateCanvas_ScreenSpaceOverlay_ShouldSetRenderMode()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createCanvas",
                ["name"] = "OverlayCanvas",
                ["renderMode"] = "screenspaceoverlay"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var canvasGo = GameObject.Find("OverlayCanvas");
            Assert.IsNotNull(canvasGo);
            _createdObjects.Add(canvasGo);

            var canvas = canvasGo.GetComponent<Canvas>();
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
        }

        [Test]
        public void Execute_CreateCanvas_WorldSpace_ShouldSetRenderMode()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createCanvas",
                ["name"] = "WorldCanvas",
                ["renderMode"] = "worldspace"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var canvasGo = GameObject.Find("WorldCanvas");
            Assert.IsNotNull(canvasGo);
            _createdObjects.Add(canvasGo);

            var canvas = canvasGo.GetComponent<Canvas>();
            Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode);
        }

        #endregion

        #region CreatePanel Tests

        [Test]
        public void Execute_CreatePanel_ShouldCreatePanelWithImage()
        {
            // First create a canvas as parent
            var canvasPayload = new Dictionary<string, object>
            {
                ["operation"] = "createCanvas",
                ["name"] = "PanelTestCanvas"
            };
            _handler.Execute(canvasPayload);
            _createdObjects.Add(GameObject.Find("PanelTestCanvas"));

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createPanel",
                ["name"] = "TestPanel",
                ["parentPath"] = "PanelTestCanvas"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var panelGo = GameObject.Find("TestPanel");
            Assert.IsNotNull(panelGo);
            _createdObjects.Add(panelGo);

            Assert.IsNotNull(panelGo.GetComponent<Image>());
            Assert.IsNotNull(panelGo.GetComponent<RectTransform>());
        }

        [Test]
        public void Execute_CreatePanel_MissingParent_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createPanel",
                ["name"] = "TestPanel"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion

        #region CreateButton Tests

        [Test]
        public void Execute_CreateButton_ShouldCreateButtonWithComponents()
        {
            // First create a canvas as parent
            var canvasPayload = new Dictionary<string, object>
            {
                ["operation"] = "createCanvas",
                ["name"] = "ButtonTestCanvas"
            };
            _handler.Execute(canvasPayload);
            _createdObjects.Add(GameObject.Find("ButtonTestCanvas"));

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createButton",
                ["name"] = "TestButton",
                ["parentPath"] = "ButtonTestCanvas",
                ["text"] = "Click Me"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var buttonGo = GameObject.Find("TestButton");
            Assert.IsNotNull(buttonGo);
            _createdObjects.Add(buttonGo);

            Assert.IsNotNull(buttonGo.GetComponent<Button>());
            Assert.IsNotNull(buttonGo.GetComponent<Image>());
        }

        #endregion

        #region CreateText Tests

        [Test]
        public void Execute_CreateText_ShouldCreateTextElement()
        {
            // First create a canvas as parent
            var canvasPayload = new Dictionary<string, object>
            {
                ["operation"] = "createCanvas",
                ["name"] = "TextTestCanvas"
            };
            _handler.Execute(canvasPayload);
            _createdObjects.Add(GameObject.Find("TextTestCanvas"));

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createText",
                ["name"] = "TestText",
                ["parentPath"] = "TextTestCanvas",
                ["text"] = "Hello World"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var textGo = GameObject.Find("TestText");
            Assert.IsNotNull(textGo);
            _createdObjects.Add(textGo);
        }

        #endregion

        #region CreateImage Tests

        [Test]
        public void Execute_CreateImage_ShouldCreateImageElement()
        {
            // First create a canvas as parent
            var canvasPayload = new Dictionary<string, object>
            {
                ["operation"] = "createCanvas",
                ["name"] = "ImageTestCanvas"
            };
            _handler.Execute(canvasPayload);
            _createdObjects.Add(GameObject.Find("ImageTestCanvas"));

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createImage",
                ["name"] = "TestImage",
                ["parentPath"] = "ImageTestCanvas",
                ["width"] = 200f,
                ["height"] = 150f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var imageGo = GameObject.Find("TestImage");
            Assert.IsNotNull(imageGo);
            _createdObjects.Add(imageGo);

            var image = imageGo.GetComponent<Image>();
            Assert.IsNotNull(image);

            var rectTransform = imageGo.GetComponent<RectTransform>();
            Assert.AreEqual(200f, rectTransform.sizeDelta.x, 0.01f);
            Assert.AreEqual(150f, rectTransform.sizeDelta.y, 0.01f);
        }

        #endregion

        #region CreateScrollView Tests

        [Test]
        public void Execute_CreateScrollView_ShouldCreateScrollViewWithComponents()
        {
            // First create a canvas as parent
            var canvasPayload = new Dictionary<string, object>
            {
                ["operation"] = "createCanvas",
                ["name"] = "ScrollViewTestCanvas"
            };
            _handler.Execute(canvasPayload);
            _createdObjects.Add(GameObject.Find("ScrollViewTestCanvas"));

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createScrollView",
                ["name"] = "TestScrollView",
                ["parentPath"] = "ScrollViewTestCanvas"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var scrollViewGo = GameObject.Find("TestScrollView");
            Assert.IsNotNull(scrollViewGo);
            _createdObjects.Add(scrollViewGo);

            Assert.IsNotNull(scrollViewGo.GetComponent<ScrollRect>());
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

        #endregion
    }
}
