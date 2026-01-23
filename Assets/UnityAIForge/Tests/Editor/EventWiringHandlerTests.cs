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
    /// EventWiringHandler unit tests.
    /// Tests event wiring between UI elements and game logic.
    /// </summary>
    [TestFixture]
    public class EventWiringHandlerTests
    {
        private EventWiringHandler _handler;
        private List<GameObject> _createdObjects;
        private GameObject _canvas;

        [SetUp]
        public void SetUp()
        {
            _handler = new EventWiringHandler();
            _createdObjects = new List<GameObject>();

            // Create Canvas for UI tests
            _canvas = CreateCanvas();
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

        private GameObject CreateCanvas()
        {
            var canvasGo = new GameObject("TestCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            _createdObjects.Add(canvasGo);
            return canvasGo;
        }

        private GameObject CreateTestGameObject(string name)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go;
        }

        private GameObject CreateUIButton(string name)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>();
            go.AddComponent<Image>();
            go.AddComponent<Button>();
            go.transform.SetParent(_canvas.transform, false);
            _createdObjects.Add(go);
            return go;
        }

        private GameObject CreateUIToggle(string name)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>();
            go.AddComponent<Toggle>();
            go.transform.SetParent(_canvas.transform, false);
            _createdObjects.Add(go);
            return go;
        }

        private GameObject CreateUISlider(string name)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>();
            go.AddComponent<Slider>();
            go.transform.SetParent(_canvas.transform, false);
            _createdObjects.Add(go);
            return go;
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnEventWiring()
        {
            Assert.AreEqual("eventWiring", _handler.Category);
        }

        [Test]
        public void Version_ShouldReturn100()
        {
            Assert.AreEqual("1.0.0", _handler.Version);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("wire", operations);
            Assert.Contains("unwire", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("clearEvent", operations);
            Assert.Contains("wireMultiple", operations);
        }

        #endregion

        #region Wire Operation Tests

        [Test]
        public void Execute_Wire_ButtonToGameObject_ShouldCreateWiring()
        {
            var button = CreateUIButton("TestButton");
            var target = CreateTestGameObject("TestTarget");
            target.AddComponent<AudioSource>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "wire",
                ["source"] = new Dictionary<string, object>
                {
                    ["gameObject"] = "TestCanvas/TestButton",
                    ["component"] = "Button",
                    ["event"] = "onClick"
                },
                ["target"] = new Dictionary<string, object>
                {
                    ["gameObject"] = "TestTarget",
                    ["component"] = "AudioSource",
                    ["method"] = "Play"
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var btn = button.GetComponent<Button>();
            Assert.AreEqual(1, btn.onClick.GetPersistentEventCount());
        }

        [Test]
        public void Execute_Wire_ToggleToGameObject_ShouldCreateWiring()
        {
            var toggle = CreateUIToggle("TestToggle");
            var target = CreateTestGameObject("TestTarget");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "wire",
                ["source"] = new Dictionary<string, object>
                {
                    ["gameObject"] = "TestCanvas/TestToggle",
                    ["component"] = "Toggle",
                    ["event"] = "onValueChanged"
                },
                ["target"] = new Dictionary<string, object>
                {
                    ["gameObject"] = "TestTarget",
                    ["method"] = "SetActive"
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var tgl = toggle.GetComponent<Toggle>();
            Assert.AreEqual(1, tgl.onValueChanged.GetPersistentEventCount());
        }

        [Test]
        public void Execute_Wire_SliderToTransform_ShouldCreateWiring()
        {
            var slider = CreateUISlider("TestSlider");
            var target = CreateTestGameObject("TestTarget");
            var light = target.AddComponent<Light>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "wire",
                ["source"] = new Dictionary<string, object>
                {
                    ["gameObject"] = "TestCanvas/TestSlider",
                    ["component"] = "Slider",
                    ["event"] = "onValueChanged"
                },
                ["target"] = new Dictionary<string, object>
                {
                    ["gameObject"] = "TestTarget",
                    ["component"] = "Light",
                    ["method"] = "set_intensity",
                    ["mode"] = "Float"
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion

        #region Unwire Operation Tests

        [Test]
        public void Execute_Unwire_ShouldRemoveWiring()
        {
            // First wire a button
            var button = CreateUIButton("TestButton_Unwire");
            var target = CreateTestGameObject("TestTarget_Unwire");
            target.AddComponent<AudioSource>();

            var wirePayload = new Dictionary<string, object>
            {
                ["operation"] = "wire",
                ["source"] = new Dictionary<string, object>
                {
                    ["gameObject"] = "TestCanvas/TestButton_Unwire",
                    ["component"] = "Button",
                    ["event"] = "onClick"
                },
                ["target"] = new Dictionary<string, object>
                {
                    ["gameObject"] = "TestTarget_Unwire",
                    ["component"] = "AudioSource",
                    ["method"] = "Play"
                }
            };
            _handler.Execute(wirePayload);

            // Then unwire
            var unwirePayload = new Dictionary<string, object>
            {
                ["operation"] = "unwire",
                ["source"] = new Dictionary<string, object>
                {
                    ["gameObject"] = "TestCanvas/TestButton_Unwire",
                    ["component"] = "Button",
                    ["event"] = "onClick"
                },
                ["listenerIndex"] = 0
            };

            var result = _handler.Execute(unwirePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var btn = button.GetComponent<Button>();
            Assert.AreEqual(0, btn.onClick.GetPersistentEventCount());
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnWiringInfo()
        {
            // Wire a button first
            var button = CreateUIButton("TestButton_Inspect");
            var target = CreateTestGameObject("TestTarget_Inspect");
            target.AddComponent<AudioSource>();

            var wirePayload = new Dictionary<string, object>
            {
                ["operation"] = "wire",
                ["source"] = new Dictionary<string, object>
                {
                    ["gameObject"] = "TestCanvas/TestButton_Inspect",
                    ["component"] = "Button",
                    ["event"] = "onClick"
                },
                ["target"] = new Dictionary<string, object>
                {
                    ["gameObject"] = "TestTarget_Inspect",
                    ["component"] = "AudioSource",
                    ["method"] = "Play"
                }
            };
            _handler.Execute(wirePayload);

            // Inspect
            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["source"] = new Dictionary<string, object>
                {
                    ["gameObject"] = "TestCanvas/TestButton_Inspect",
                    ["component"] = "Button",
                    ["event"] = "onClick"
                }
            };

            var result = _handler.Execute(inspectPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("listeners"));
        }

        [Test]
        public void Execute_Inspect_NoWirings_ShouldReturnEmptyList()
        {
            var button = CreateUIButton("TestButton_NoWirings");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["source"] = new Dictionary<string, object>
                {
                    ["gameObject"] = "TestCanvas/TestButton_NoWirings",
                    ["component"] = "Button",
                    ["event"] = "onClick"
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("listeners"));
        }

        #endregion

        #region Clear Operation Tests

        [Test]
        public void Execute_Clear_ShouldRemoveAllWirings()
        {
            // Wire multiple handlers
            var button = CreateUIButton("TestButton_Clear");
            var target1 = CreateTestGameObject("TestTarget1");
            var target2 = CreateTestGameObject("TestTarget2");
            target1.AddComponent<AudioSource>();
            target2.AddComponent<AudioSource>();

            var wirePayload1 = new Dictionary<string, object>
            {
                ["operation"] = "wire",
                ["source"] = new Dictionary<string, object>
                {
                    ["gameObject"] = "TestCanvas/TestButton_Clear",
                    ["component"] = "Button",
                    ["event"] = "onClick"
                },
                ["target"] = new Dictionary<string, object>
                {
                    ["gameObject"] = "TestTarget1",
                    ["component"] = "AudioSource",
                    ["method"] = "Play"
                }
            };
            _handler.Execute(wirePayload1);

            var wirePayload2 = new Dictionary<string, object>
            {
                ["operation"] = "wire",
                ["source"] = new Dictionary<string, object>
                {
                    ["gameObject"] = "TestCanvas/TestButton_Clear",
                    ["component"] = "Button",
                    ["event"] = "onClick"
                },
                ["target"] = new Dictionary<string, object>
                {
                    ["gameObject"] = "TestTarget2",
                    ["component"] = "AudioSource",
                    ["method"] = "Play"
                }
            };
            _handler.Execute(wirePayload2);

            // Clear all
            var clearPayload = new Dictionary<string, object>
            {
                ["operation"] = "clearEvent",
                ["source"] = new Dictionary<string, object>
                {
                    ["gameObject"] = "TestCanvas/TestButton_Clear",
                    ["component"] = "Button",
                    ["event"] = "onClick"
                }
            };

            var result = _handler.Execute(clearPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var btn = button.GetComponent<Button>();
            Assert.AreEqual(0, btn.onClick.GetPersistentEventCount());
        }

        #endregion

        #region WireBatch Operation Tests

        [Test]
        public void Execute_WireBatch_ShouldCreateMultipleWirings()
        {
            var button1 = CreateUIButton("TestButton_Batch1");
            var button2 = CreateUIButton("TestButton_Batch2");
            var target = CreateTestGameObject("TestTarget_Batch");
            target.AddComponent<AudioSource>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "wireMultiple",
                ["wirings"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["source"] = new Dictionary<string, object>
                        {
                            ["gameObject"] = "TestCanvas/TestButton_Batch1",
                            ["component"] = "Button",
                            ["event"] = "onClick"
                        },
                        ["target"] = new Dictionary<string, object>
                        {
                            ["gameObject"] = "TestTarget_Batch",
                            ["component"] = "AudioSource",
                            ["method"] = "Play"
                        }
                    },
                    new Dictionary<string, object>
                    {
                        ["source"] = new Dictionary<string, object>
                        {
                            ["gameObject"] = "TestCanvas/TestButton_Batch2",
                            ["component"] = "Button",
                            ["event"] = "onClick"
                        },
                        ["target"] = new Dictionary<string, object>
                        {
                            ["gameObject"] = "TestTarget_Batch",
                            ["component"] = "AudioSource",
                            ["method"] = "Stop"
                        }
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var btn1 = button1.GetComponent<Button>();
            var btn2 = button2.GetComponent<Button>();
            Assert.AreEqual(1, btn1.onClick.GetPersistentEventCount());
            Assert.AreEqual(1, btn2.onClick.GetPersistentEventCount());
        }

        #endregion
    }
}
