using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MCP.Editor.Utilities;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// Unit tests for HandlerUtilities.
    /// </summary>
    [TestFixture]
    public class HandlerUtilitiesTests
    {
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _createdObjects = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
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
                go.transform.SetParent(parent, false);
            }
            _createdObjects.Add(go);
            return go;
        }

        #region BuildGameObjectPath Tests

        [Test]
        public void BuildGameObjectPath_WithNull_ShouldReturnEmptyString()
        {
            var result = HandlerUtilities.BuildGameObjectPath(null);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void BuildGameObjectPath_WithRootObject_ShouldReturnName()
        {
            var go = CreateTestGameObject("RootObject");
            var result = HandlerUtilities.BuildGameObjectPath(go);
            Assert.AreEqual("RootObject", result);
        }

        [Test]
        public void BuildGameObjectPath_WithNestedObject_ShouldReturnFullPath()
        {
            var parent = CreateTestGameObject("Parent");
            var child = CreateTestGameObject("Child", parent.transform);
            var grandchild = CreateTestGameObject("Grandchild", child.transform);

            var result = HandlerUtilities.BuildGameObjectPath(grandchild);
            Assert.AreEqual("Parent/Child/Grandchild", result);
        }

        #endregion

        #region GetVector3FromDict Tests

        [Test]
        public void GetVector3FromDict_WithNull_ShouldReturnFallback()
        {
            var fallback = new Vector3(1, 2, 3);
            var result = HandlerUtilities.GetVector3FromDict(null, fallback);
            Assert.AreEqual(fallback, result);
        }

        [Test]
        public void GetVector3FromDict_WithValidDict_ShouldReturnVector()
        {
            var dict = new Dictionary<string, object>
            {
                ["x"] = 1.5f,
                ["y"] = 2.5f,
                ["z"] = 3.5f
            };

            var result = HandlerUtilities.GetVector3FromDict(dict);
            Assert.AreEqual(new Vector3(1.5f, 2.5f, 3.5f), result);
        }

        [Test]
        public void GetVector3FromDict_WithPartialDict_ShouldUseFallbackForMissing()
        {
            var dict = new Dictionary<string, object>
            {
                ["x"] = 1.0f
            };
            var fallback = new Vector3(0, 5, 10);

            var result = HandlerUtilities.GetVector3FromDict(dict, fallback);
            Assert.AreEqual(new Vector3(1.0f, 5f, 10f), result);
        }

        [Test]
        public void GetVector3FromDict_WithIntValues_ShouldConvert()
        {
            var dict = new Dictionary<string, object>
            {
                ["x"] = 1,
                ["y"] = 2,
                ["z"] = 3
            };

            var result = HandlerUtilities.GetVector3FromDict(dict);
            Assert.AreEqual(new Vector3(1, 2, 3), result);
        }

        #endregion

        #region GetVector3FromPayload Tests

        [Test]
        public void GetVector3FromPayload_WithNullPayload_ShouldReturnFallback()
        {
            var fallback = new Vector3(1, 2, 3);
            var result = HandlerUtilities.GetVector3FromPayload(null, "position", fallback);
            Assert.AreEqual(fallback, result);
        }

        [Test]
        public void GetVector3FromPayload_WithMissingKey_ShouldReturnFallback()
        {
            var payload = new Dictionary<string, object>();
            var fallback = new Vector3(1, 2, 3);

            var result = HandlerUtilities.GetVector3FromPayload(payload, "position", fallback);
            Assert.AreEqual(fallback, result);
        }

        [Test]
        public void GetVector3FromPayload_WithValidNestedDict_ShouldReturnVector()
        {
            var payload = new Dictionary<string, object>
            {
                ["position"] = new Dictionary<string, object>
                {
                    ["x"] = 5f,
                    ["y"] = 10f,
                    ["z"] = 15f
                }
            };

            var result = HandlerUtilities.GetVector3FromPayload(payload, "position");
            Assert.AreEqual(new Vector3(5f, 10f, 15f), result);
        }

        #endregion

        #region GetVector2FromDict Tests

        [Test]
        public void GetVector2FromDict_WithNull_ShouldReturnFallback()
        {
            var fallback = new Vector2(1, 2);
            var result = HandlerUtilities.GetVector2FromDict(null, fallback);
            Assert.AreEqual(fallback, result);
        }

        [Test]
        public void GetVector2FromDict_WithValidDict_ShouldReturnVector()
        {
            var dict = new Dictionary<string, object>
            {
                ["x"] = 1.5f,
                ["y"] = 2.5f
            };

            var result = HandlerUtilities.GetVector2FromDict(dict);
            Assert.AreEqual(new Vector2(1.5f, 2.5f), result);
        }

        #endregion

        #region GetColorFromDict Tests

        [Test]
        public void GetColorFromDict_WithNull_ShouldReturnFallback()
        {
            var fallback = Color.red;
            var result = HandlerUtilities.GetColorFromDict(null, fallback);
            Assert.AreEqual(fallback, result);
        }

        [Test]
        public void GetColorFromDict_WithValidDict_ShouldReturnColor()
        {
            var dict = new Dictionary<string, object>
            {
                ["r"] = 0.5f,
                ["g"] = 0.6f,
                ["b"] = 0.7f,
                ["a"] = 0.8f
            };

            var result = HandlerUtilities.GetColorFromDict(dict);
            Assert.AreEqual(new Color(0.5f, 0.6f, 0.7f, 0.8f), result);
        }

        [Test]
        public void GetColorFromDict_WithPartialDict_ShouldUseFallbackForMissing()
        {
            var dict = new Dictionary<string, object>
            {
                ["r"] = 1.0f,
                ["g"] = 0.5f
            };
            var fallback = new Color(0, 0, 0.2f, 1f);

            var result = HandlerUtilities.GetColorFromDict(dict, fallback);
            Assert.AreEqual(new Color(1.0f, 0.5f, 0.2f, 1f), result);
        }

        #endregion

        #region SerializeVector3 Tests

        [Test]
        public void SerializeVector3_ShouldReturnCorrectDict()
        {
            var vector = new Vector3(1.5f, 2.5f, 3.5f);
            var result = HandlerUtilities.SerializeVector3(vector);

            Assert.AreEqual(1.5f, result["x"]);
            Assert.AreEqual(2.5f, result["y"]);
            Assert.AreEqual(3.5f, result["z"]);
        }

        #endregion

        #region SerializeVector2 Tests

        [Test]
        public void SerializeVector2_ShouldReturnCorrectDict()
        {
            var vector = new Vector2(1.5f, 2.5f);
            var result = HandlerUtilities.SerializeVector2(vector);

            Assert.AreEqual(1.5f, result["x"]);
            Assert.AreEqual(2.5f, result["y"]);
        }

        #endregion

        #region SerializeColor Tests

        [Test]
        public void SerializeColor_ShouldReturnCorrectDict()
        {
            var color = new Color(0.1f, 0.2f, 0.3f, 0.4f);
            var result = HandlerUtilities.SerializeColor(color);

            Assert.AreEqual(0.1f, result["r"]);
            Assert.AreEqual(0.2f, result["g"]);
            Assert.AreEqual(0.3f, result["b"]);
            Assert.AreEqual(0.4f, result["a"]);
        }

        #endregion

        #region SerializeQuaternion Tests

        [Test]
        public void SerializeQuaternion_ShouldReturnCorrectDict()
        {
            var quat = new Quaternion(0.1f, 0.2f, 0.3f, 0.4f);
            var result = HandlerUtilities.SerializeQuaternion(quat);

            Assert.AreEqual(0.1f, result["x"]);
            Assert.AreEqual(0.2f, result["y"]);
            Assert.AreEqual(0.3f, result["z"]);
            Assert.AreEqual(0.4f, result["w"]);
        }

        #endregion

        #region GetBoolOrDefault Tests

        [Test]
        public void GetBoolOrDefault_WithNullPayload_ShouldReturnDefault()
        {
            var result = HandlerUtilities.GetBoolOrDefault(null, "key", true);
            Assert.IsTrue(result);
        }

        [Test]
        public void GetBoolOrDefault_WithMissingKey_ShouldReturnDefault()
        {
            var payload = new Dictionary<string, object>();
            var result = HandlerUtilities.GetBoolOrDefault(payload, "missing", true);
            Assert.IsTrue(result);
        }

        [Test]
        public void GetBoolOrDefault_WithBoolValue_ShouldReturnValue()
        {
            var payload = new Dictionary<string, object> { ["enabled"] = true };
            var result = HandlerUtilities.GetBoolOrDefault(payload, "enabled", false);
            Assert.IsTrue(result);
        }

        [Test]
        public void GetBoolOrDefault_WithStringValue_ShouldParseValue()
        {
            var payload = new Dictionary<string, object> { ["enabled"] = "true" };
            var result = HandlerUtilities.GetBoolOrDefault(payload, "enabled", false);
            Assert.IsTrue(result);
        }

        #endregion

        #region GetIntOrDefault Tests

        [Test]
        public void GetIntOrDefault_WithNullPayload_ShouldReturnDefault()
        {
            var result = HandlerUtilities.GetIntOrDefault(null, "key", 42);
            Assert.AreEqual(42, result);
        }

        [Test]
        public void GetIntOrDefault_WithMissingKey_ShouldReturnDefault()
        {
            var payload = new Dictionary<string, object>();
            var result = HandlerUtilities.GetIntOrDefault(payload, "missing", 42);
            Assert.AreEqual(42, result);
        }

        [Test]
        public void GetIntOrDefault_WithIntValue_ShouldReturnValue()
        {
            var payload = new Dictionary<string, object> { ["count"] = 100 };
            var result = HandlerUtilities.GetIntOrDefault(payload, "count", 0);
            Assert.AreEqual(100, result);
        }

        [Test]
        public void GetIntOrDefault_WithLongValue_ShouldConvert()
        {
            var payload = new Dictionary<string, object> { ["count"] = 100L };
            var result = HandlerUtilities.GetIntOrDefault(payload, "count", 0);
            Assert.AreEqual(100, result);
        }

        #endregion

        #region GetFloatOrDefault Tests

        [Test]
        public void GetFloatOrDefault_WithNullPayload_ShouldReturnDefault()
        {
            var result = HandlerUtilities.GetFloatOrDefault(null, "key", 1.5f);
            Assert.AreEqual(1.5f, result);
        }

        [Test]
        public void GetFloatOrDefault_WithMissingKey_ShouldReturnDefault()
        {
            var payload = new Dictionary<string, object>();
            var result = HandlerUtilities.GetFloatOrDefault(payload, "missing", 1.5f);
            Assert.AreEqual(1.5f, result);
        }

        [Test]
        public void GetFloatOrDefault_WithFloatValue_ShouldReturnValue()
        {
            var payload = new Dictionary<string, object> { ["speed"] = 2.5f };
            var result = HandlerUtilities.GetFloatOrDefault(payload, "speed", 0f);
            Assert.AreEqual(2.5f, result);
        }

        [Test]
        public void GetFloatOrDefault_WithDoubleValue_ShouldConvert()
        {
            var payload = new Dictionary<string, object> { ["speed"] = 2.5 };
            var result = HandlerUtilities.GetFloatOrDefault(payload, "speed", 0f);
            Assert.AreEqual(2.5f, result);
        }

        #endregion

        #region GetStringList Tests

        [Test]
        public void GetStringList_WithNullPayload_ShouldReturnEmptyList()
        {
            var result = HandlerUtilities.GetStringList(null, "items");
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetStringList_WithMissingKey_ShouldReturnEmptyList()
        {
            var payload = new Dictionary<string, object>();
            var result = HandlerUtilities.GetStringList(payload, "items");
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetStringList_WithListOfObjects_ShouldConvertToStrings()
        {
            var payload = new Dictionary<string, object>
            {
                ["items"] = new List<object> { "a", "b", "c" }
            };

            var result = HandlerUtilities.GetStringList(payload, "items");
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("a", result[0]);
            Assert.AreEqual("b", result[1]);
            Assert.AreEqual("c", result[2]);
        }

        [Test]
        public void GetStringList_WithSingleString_ShouldReturnSingleItemList()
        {
            var payload = new Dictionary<string, object>
            {
                ["items"] = "single"
            };

            var result = HandlerUtilities.GetStringList(payload, "items");
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("single", result[0]);
        }

        [Test]
        public void GetStringList_WithNullsInList_ShouldFilterNulls()
        {
            var payload = new Dictionary<string, object>
            {
                ["items"] = new List<object> { "a", null, "b", "", "c" }
            };

            var result = HandlerUtilities.GetStringList(payload, "items");
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("a", result[0]);
            Assert.AreEqual("b", result[1]);
            Assert.AreEqual("c", result[2]);
        }

        #endregion

        #region GetDictOrDefault Tests

        [Test]
        public void GetDictOrDefault_WithNullPayload_ShouldReturnNull()
        {
            var result = HandlerUtilities.GetDictOrDefault(null, "key");
            Assert.IsNull(result);
        }

        [Test]
        public void GetDictOrDefault_WithMissingKey_ShouldReturnNull()
        {
            var payload = new Dictionary<string, object>();
            var result = HandlerUtilities.GetDictOrDefault(payload, "missing");
            Assert.IsNull(result);
        }

        [Test]
        public void GetDictOrDefault_WithValidDict_ShouldReturnDict()
        {
            var nested = new Dictionary<string, object> { ["inner"] = "value" };
            var payload = new Dictionary<string, object> { ["outer"] = nested };

            var result = HandlerUtilities.GetDictOrDefault(payload, "outer");
            Assert.IsNotNull(result);
            Assert.AreEqual("value", result["inner"]);
        }

        [Test]
        public void GetDictOrDefault_WithNonDictValue_ShouldReturnNull()
        {
            var payload = new Dictionary<string, object> { ["key"] = "not a dict" };
            var result = HandlerUtilities.GetDictOrDefault(payload, "key");
            Assert.IsNull(result);
        }

        #endregion
    }
}
