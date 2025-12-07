using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MCP.Editor.Handlers;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// ComponentCommandHandler のユニットテスト。
    /// TextMeshPro コンポーネントを使用してテストを行います。
    /// </summary>
    [TestFixture]
    public class ComponentCommandHandlerTests
    {
        private ComponentCommandHandler _handler;
        private List<GameObject> _createdObjects;
        private GameObject _canvas;

        [SetUp]
        public void SetUp()
        {
            _handler = new ComponentCommandHandler();
            _createdObjects = new List<GameObject>();
            
            // UI テスト用の Canvas を作成
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
                    UnityEngine.Object.DestroyImmediate(obj);
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

        private GameObject CreateUIGameObject(string name)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>();
            go.transform.SetParent(_canvas.transform, false);
            _createdObjects.Add(go);
            return go;
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnComponent()
        {
            Assert.AreEqual("component", _handler.Category);
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
            
            Assert.Contains("add", operations);
            Assert.Contains("remove", operations);
            Assert.Contains("update", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("addMultiple", operations);
            Assert.Contains("removeMultiple", operations);
            Assert.Contains("updateMultiple", operations);
            Assert.Contains("inspectMultiple", operations);
        }

        #endregion

        #region Add Tests

        [Test]
        public void Execute_Add_TextMeshPro_ShouldAddComponent()
        {
            // Arrange
            var go = CreateTestGameObject("TestObject");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["gameObjectPath"] = "TestObject",
                ["componentType"] = "TMPro.TextMeshPro"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNotNull(go.GetComponent<TextMeshPro>());
        }

        [Test]
        public void Execute_Add_TextMeshProUGUI_ShouldAddComponent()
        {
            // Arrange
            var go = CreateUIGameObject("UITestObject");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["gameObjectPath"] = "TestCanvas/UITestObject",
                ["componentType"] = "TMPro.TextMeshProUGUI"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNotNull(go.GetComponent<TextMeshProUGUI>());
        }

        [Test]
        public void Execute_Add_WithPropertyChanges_TextMeshProUGUI_ShouldApplyProperties()
        {
            // Arrange
            var go = CreateUIGameObject("UITestObject");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["gameObjectPath"] = "TestCanvas/UITestObject",
                ["componentType"] = "TMPro.TextMeshProUGUI",
                ["propertyChanges"] = new Dictionary<string, object>
                {
                    ["text"] = "Hello World",
                    ["fontSize"] = 24.0f
                }
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            
            var tmp = go.GetComponent<TextMeshProUGUI>();
            Assert.IsNotNull(tmp);
            Assert.AreEqual("Hello World", tmp.text);
            Assert.AreEqual(24.0f, tmp.fontSize, 0.001f);
        }

        [Test]
        public void Execute_Add_MissingComponentType_ShouldReturnError()
        {
            // Arrange
            CreateTestGameObject("TestObject");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["gameObjectPath"] = "TestObject"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        [Test]
        public void Execute_Add_InvalidComponentType_ShouldReturnError()
        {
            // Arrange
            CreateTestGameObject("TestObject");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["gameObjectPath"] = "TestObject",
                ["componentType"] = "NonExistentComponent"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        [Test]
        public void Execute_Add_NonComponentType_ShouldReturnError()
        {
            // Arrange
            CreateTestGameObject("TestObject");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["gameObjectPath"] = "TestObject",
                ["componentType"] = "System.String"  // String is not a Component
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsTrue(result["error"].ToString().Contains("Component"));
        }

        #endregion

        #region Remove Tests

        [Test]
        public void Execute_Remove_TextMeshProUGUI_ShouldRemoveComponent()
        {
            // Arrange
            var go = CreateUIGameObject("UITestObject");
            go.AddComponent<TextMeshProUGUI>();
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "remove",
                ["gameObjectPath"] = "TestCanvas/UITestObject",
                ["componentType"] = "TMPro.TextMeshProUGUI"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(go.GetComponent<TextMeshProUGUI>());
        }

        [Test]
        public void Execute_Remove_TextMeshPro_ShouldRemoveComponent()
        {
            // Arrange
            var go = CreateTestGameObject("TestObject");
            go.AddComponent<TextMeshPro>();
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "remove",
                ["gameObjectPath"] = "TestObject",
                ["componentType"] = "TMPro.TextMeshPro"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(go.GetComponent<TextMeshPro>());
        }

        [Test]
        public void Execute_Remove_NonExistingComponent_ShouldReturnError()
        {
            // Arrange
            CreateUIGameObject("UITestObject");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "remove",
                ["gameObjectPath"] = "TestCanvas/UITestObject",
                ["componentType"] = "TMPro.TextMeshProUGUI"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsTrue(result["error"].ToString().Contains("not found"));
        }

        #endregion

        #region Update Tests

        [Test]
        public void Execute_Update_TextMeshProUGUI_ShouldUpdateProperties()
        {
            // Arrange
            var go = CreateUIGameObject("UITestObject");
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "Initial Text";
            tmp.fontSize = 12.0f;
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = "TestCanvas/UITestObject",
                ["componentType"] = "TMPro.TextMeshProUGUI",
                ["propertyChanges"] = new Dictionary<string, object>
                {
                    ["text"] = "Updated Text",
                    ["fontSize"] = 36.0f
                }
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("Updated Text", tmp.text);
            Assert.AreEqual(36.0f, tmp.fontSize, 0.001f);
        }

        [Test]
        public void Execute_Update_TextMeshPro_ShouldUpdateProperties()
        {
            // Arrange
            var go = CreateTestGameObject("TestObject");
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = "Initial";
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = "TestObject",
                ["componentType"] = "TMPro.TextMeshPro",
                ["propertyChanges"] = new Dictionary<string, object>
                {
                    ["text"] = "Updated 3D Text",
                    ["fontSize"] = 48.0f
                }
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("Updated 3D Text", tmp.text);
            Assert.AreEqual(48.0f, tmp.fontSize, 0.001f);
        }

        [Test]
        public void Execute_Update_MissingPropertyChanges_ShouldReturnError()
        {
            // Arrange
            var go = CreateUIGameObject("UITestObject");
            go.AddComponent<TextMeshProUGUI>();
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = "TestCanvas/UITestObject",
                ["componentType"] = "TMPro.TextMeshProUGUI"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion

        #region Inspect Tests

        [Test]
        public void Execute_Inspect_TextMeshProUGUI_ShouldReturnComponentInfo()
        {
            // Arrange
            var go = CreateUIGameObject("UITestObject");
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "Test Text";
            tmp.fontSize = 24.0f;
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = "TestCanvas/UITestObject",
                ["componentType"] = "TMPro.TextMeshProUGUI",
                ["includeProperties"] = true
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNotNull(result["properties"]);
            Assert.AreEqual("TestCanvas/UITestObject", result["gameObjectPath"]);
        }

        [Test]
        public void Execute_Inspect_NonExistingComponent_ShouldReturnError()
        {
            // Arrange
            CreateUIGameObject("UITestObject");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = "TestCanvas/UITestObject",
                ["componentType"] = "TMPro.TextMeshProUGUI"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion

        #region AddMultiple Tests

        [Test]
        public void Execute_AddMultiple_TextMeshProUGUI_ShouldAddToAllMatching()
        {
            // Arrange
            var go1 = CreateUIGameObject("Target_A");
            var go2 = CreateUIGameObject("Target_B");
            CreateUIGameObject("Other_C");
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "addMultiple",
                ["pattern"] = "Target_*",
                ["componentType"] = "TMPro.TextMeshProUGUI"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(2, (int)result["succeeded"]);
            Assert.IsNotNull(go1.GetComponent<TextMeshProUGUI>());
            Assert.IsNotNull(go2.GetComponent<TextMeshProUGUI>());
        }

        [Test]
        public void Execute_AddMultiple_MissingPattern_ShouldReturnError()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "addMultiple",
                ["componentType"] = "TMPro.TextMeshProUGUI"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion

        #region RemoveMultiple Tests

        [Test]
        public void Execute_RemoveMultiple_TextMeshProUGUI_ShouldRemoveFromAllMatching()
        {
            // Arrange
            var go1 = CreateUIGameObject("Remove_A");
            var go2 = CreateUIGameObject("Remove_B");
            go1.AddComponent<TextMeshProUGUI>();
            go2.AddComponent<TextMeshProUGUI>();
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "removeMultiple",
                ["pattern"] = "Remove_*",
                ["componentType"] = "TMPro.TextMeshProUGUI"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(2, (int)result["succeeded"]);
            Assert.IsNull(go1.GetComponent<TextMeshProUGUI>());
            Assert.IsNull(go2.GetComponent<TextMeshProUGUI>());
        }

        #endregion

        #region InspectMultiple Tests

        [Test]
        public void Execute_InspectMultiple_TextMeshProUGUI_ShouldReturnAllMatching()
        {
            // Arrange
            var go1 = CreateUIGameObject("Inspect_A");
            var go2 = CreateUIGameObject("Inspect_B");
            CreateUIGameObject("Other_C");
            
            var tmp1 = go1.AddComponent<TextMeshProUGUI>();
            tmp1.text = "Text A";
            var tmp2 = go2.AddComponent<TextMeshProUGUI>();
            tmp2.text = "Text B";
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspectMultiple",
                ["pattern"] = "Inspect_*",
                ["componentType"] = "TMPro.TextMeshProUGUI",
                ["includeProperties"] = true
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(2, (int)result["count"]);
        }

        #endregion

        #region PropertyFilter Tests

        [Test]
        public void Execute_Inspect_WithPropertyFilter_ListObject_ShouldFilterProperties()
        {
            // Arrange
            var go = CreateUIGameObject("UITestObject");
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "Test Text";
            tmp.fontSize = 24.0f;
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = "TestCanvas/UITestObject",
                ["componentType"] = "TMPro.TextMeshProUGUI",
                ["includeProperties"] = true,
                ["propertyFilter"] = new List<object> { "text", "fontSize" }
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            
            var properties = result["properties"] as Dictionary<string, object>;
            Assert.IsNotNull(properties);
            Assert.IsTrue(properties.ContainsKey("text"));
            Assert.IsTrue(properties.ContainsKey("fontSize"));
            // フィルタされているため、他のプロパティは含まれないはず
            Assert.IsFalse(properties.ContainsKey("alignment"));
        }

        [Test]
        public void Execute_Inspect_WithPropertyFilter_StringArray_ShouldFilterProperties()
        {
            // Arrange
            var go = CreateUIGameObject("UITestObject");
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "Test Text";
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = "TestCanvas/UITestObject",
                ["componentType"] = "TMPro.TextMeshProUGUI",
                ["includeProperties"] = true,
                ["propertyFilter"] = new string[] { "text" }
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            
            var properties = result["properties"] as Dictionary<string, object>;
            Assert.IsNotNull(properties);
            Assert.IsTrue(properties.ContainsKey("text"));
        }

        [Test]
        public void Execute_Inspect_WithPropertyFilter_CommaSeparatedString_ShouldFilterProperties()
        {
            // Arrange
            var go = CreateUIGameObject("UITestObject");
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "Test Text";
            tmp.fontSize = 24.0f;
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = "TestCanvas/UITestObject",
                ["componentType"] = "TMPro.TextMeshProUGUI",
                ["includeProperties"] = true,
                ["propertyFilter"] = "text,fontSize"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            
            var properties = result["properties"] as Dictionary<string, object>;
            Assert.IsNotNull(properties);
            Assert.IsTrue(properties.ContainsKey("text"));
            Assert.IsTrue(properties.ContainsKey("fontSize"));
        }

        [Test]
        public void Execute_InspectMultiple_WithPropertyFilter_ShouldFilterProperties()
        {
            // Arrange
            var go1 = CreateUIGameObject("Filter_A");
            var go2 = CreateUIGameObject("Filter_B");
            
            var tmp1 = go1.AddComponent<TextMeshProUGUI>();
            tmp1.text = "Text A";
            var tmp2 = go2.AddComponent<TextMeshProUGUI>();
            tmp2.text = "Text B";
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspectMultiple",
                ["pattern"] = "Filter_*",
                ["componentType"] = "TMPro.TextMeshProUGUI",
                ["includeProperties"] = true,
                ["propertyFilter"] = new List<object> { "text" }
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(2, (int)result["count"]);
            
            var results = result["results"] as System.Collections.IList;
            Assert.IsNotNull(results);
            
            foreach (var item in results)
            {
                var itemDict = item as Dictionary<string, object>;
                Assert.IsNotNull(itemDict);
                var properties = itemDict["properties"] as Dictionary<string, object>;
                Assert.IsNotNull(properties);
                Assert.IsTrue(properties.ContainsKey("text"));
            }
        }

        #endregion

        #region AddMultiple with PropertyChanges Tests

        [Test]
        public void Execute_AddMultiple_WithPropertyChanges_ShouldApplyInitialValues()
        {
            // Arrange
            var go1 = CreateUIGameObject("Init_A");
            var go2 = CreateUIGameObject("Init_B");
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "addMultiple",
                ["pattern"] = "Init_*",
                ["componentType"] = "TMPro.TextMeshProUGUI",
                ["propertyChanges"] = new Dictionary<string, object>
                {
                    ["text"] = "Initial Text",
                    ["fontSize"] = 32.0f
                }
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(2, (int)result["succeeded"]);
            
            var tmp1 = go1.GetComponent<TextMeshProUGUI>();
            var tmp2 = go2.GetComponent<TextMeshProUGUI>();
            
            Assert.IsNotNull(tmp1);
            Assert.IsNotNull(tmp2);
            Assert.AreEqual("Initial Text", tmp1.text);
            Assert.AreEqual("Initial Text", tmp2.text);
            Assert.AreEqual(32.0f, tmp1.fontSize, 0.001f);
            Assert.AreEqual(32.0f, tmp2.fontSize, 0.001f);
        }

        [Test]
        public void Execute_AddMultiple_WithPropertyChanges_ShouldReturnUpdatedProperties()
        {
            // Arrange
            CreateUIGameObject("Props_A");
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "addMultiple",
                ["pattern"] = "Props_A",
                ["componentType"] = "TMPro.TextMeshProUGUI",
                ["propertyChanges"] = new Dictionary<string, object>
                {
                    ["text"] = "Test"
                }
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            
            var results = result["results"] as System.Collections.IList;
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count);
            
            var firstResult = results[0] as Dictionary<string, object>;
            Assert.IsNotNull(firstResult);
            Assert.IsTrue(firstResult.ContainsKey("updatedProperties"));
            
            var updatedProperties = firstResult["updatedProperties"] as System.Collections.IList;
            Assert.IsNotNull(updatedProperties);
            Assert.IsTrue(updatedProperties.Contains("text"));
        }

        #endregion

        #region Color Type Conversion Tests

        [Test]
        public void Execute_Update_ColorProperty_ShouldConvertFromDictionary()
        {
            // Arrange
            var go = CreateUIGameObject("ColorTestObject");
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.color = Color.white;
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = "TestCanvas/ColorTestObject",
                ["componentType"] = "TMPro.TextMeshProUGUI",
                ["propertyChanges"] = new Dictionary<string, object>
                {
                    ["color"] = new Dictionary<string, object>
                    {
                        ["r"] = 1.0,
                        ["g"] = 0.0,
                        ["b"] = 0.0,
                        ["a"] = 1.0
                    }
                }
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            
            Assert.AreEqual(1.0f, tmp.color.r, 0.001f);
            Assert.AreEqual(0.0f, tmp.color.g, 0.001f);
            Assert.AreEqual(0.0f, tmp.color.b, 0.001f);
            Assert.AreEqual(1.0f, tmp.color.a, 0.001f);
        }

        [Test]
        public void Execute_Update_ColorProperty_WithPartialValues_ShouldUseDefaults()
        {
            // Arrange
            var go = CreateUIGameObject("ColorTestObject2");
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.color = Color.white;
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = "TestCanvas/ColorTestObject2",
                ["componentType"] = "TMPro.TextMeshProUGUI",
                ["propertyChanges"] = new Dictionary<string, object>
                {
                    ["color"] = new Dictionary<string, object>
                    {
                        ["r"] = 0.5,
                        ["g"] = 0.5
                        // b と a は指定なし - デフォルト値が使用される
                    }
                }
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            
            Assert.AreEqual(0.5f, tmp.color.r, 0.001f);
            Assert.AreEqual(0.5f, tmp.color.g, 0.001f);
            Assert.AreEqual(1.0f, tmp.color.b, 0.001f);  // デフォルト値
            Assert.AreEqual(1.0f, tmp.color.a, 0.001f);  // デフォルト値
        }

        [Test]
        public void Execute_Add_WithColorProperty_ShouldSetInitialColor()
        {
            // Arrange
            var go = CreateUIGameObject("ColorAddObject");
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["gameObjectPath"] = "TestCanvas/ColorAddObject",
                ["componentType"] = "TMPro.TextMeshProUGUI",
                ["propertyChanges"] = new Dictionary<string, object>
                {
                    ["text"] = "Colored Text",
                    ["color"] = new Dictionary<string, object>
                    {
                        ["r"] = 0.0,
                        ["g"] = 1.0,
                        ["b"] = 0.0,
                        ["a"] = 1.0
                    }
                }
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            
            var tmp = go.GetComponent<TextMeshProUGUI>();
            Assert.IsNotNull(tmp);
            Assert.AreEqual("Colored Text", tmp.text);
            Assert.AreEqual(0.0f, tmp.color.r, 0.001f);
            Assert.AreEqual(1.0f, tmp.color.g, 0.001f);
            Assert.AreEqual(0.0f, tmp.color.b, 0.001f);
        }

        #endregion

        #region Vector Type Conversion Tests

        [Test]
        public void Execute_Update_Vector3Property_ShouldConvertFromDictionary()
        {
            // Arrange
            var go = CreateTestGameObject("VectorTestObject");
            var collider = go.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = "VectorTestObject",
                ["componentType"] = "UnityEngine.BoxCollider",
                ["propertyChanges"] = new Dictionary<string, object>
                {
                    ["center"] = new Dictionary<string, object>
                    {
                        ["x"] = 1.0,
                        ["y"] = 2.0,
                        ["z"] = 3.0
                    }
                }
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            
            Assert.AreEqual(1.0f, collider.center.x, 0.001f);
            Assert.AreEqual(2.0f, collider.center.y, 0.001f);
            Assert.AreEqual(3.0f, collider.center.z, 0.001f);
        }

        [Test]
        public void Execute_Update_Vector2Property_ShouldConvertFromDictionary()
        {
            // Arrange
            var go = CreateUIGameObject("Vector2TestObject");
            var rectTransform = go.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = "TestCanvas/Vector2TestObject",
                ["componentType"] = "UnityEngine.RectTransform",
                ["propertyChanges"] = new Dictionary<string, object>
                {
                    ["anchoredPosition"] = new Dictionary<string, object>
                    {
                        ["x"] = 100.0,
                        ["y"] = 200.0
                    }
                }
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            
            Assert.AreEqual(100.0f, rectTransform.anchoredPosition.x, 0.001f);
            Assert.AreEqual(200.0f, rectTransform.anchoredPosition.y, 0.001f);
        }

        #endregion

        #region Enum Type Conversion Tests

        [Test]
        public void Execute_Update_EnumProperty_FromString_ShouldWork()
        {
            // Arrange
            var go = CreateUIGameObject("EnumTestObject");
            var tmp = go.AddComponent<TextMeshProUGUI>();
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = "TestCanvas/EnumTestObject",
                ["componentType"] = "TMPro.TextMeshProUGUI",
                ["propertyChanges"] = new Dictionary<string, object>
                {
                    ["alignment"] = "Center"
                }
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(TextAlignmentOptions.Center, tmp.alignment);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Execute_MissingOperation_ShouldReturnError()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["gameObjectPath"] = "TestObject",
                ["componentType"] = "TMPro.TextMeshProUGUI"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        [Test]
        public void Execute_UnsupportedOperation_ShouldReturnError()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "unsupportedOperation"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        [Test]
        public void Execute_MissingGameObject_ShouldReturnError()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["gameObjectPath"] = "NonExistentObject",
                ["componentType"] = "TMPro.TextMeshProUGUI"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion
    }
}
