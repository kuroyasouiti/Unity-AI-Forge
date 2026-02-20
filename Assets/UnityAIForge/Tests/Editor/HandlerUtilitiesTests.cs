using System.Collections.Generic;
using MCP.Editor.Utilities;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class HandlerUtilitiesTests
    {
        #region BuildGameObjectPath

        [Test]
        public void BuildGameObjectPath_NullGameObject_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, HandlerUtilities.BuildGameObjectPath(null));
        }

        [Test]
        public void BuildGameObjectPath_RootObject_ReturnsName()
        {
            var go = new GameObject("Root");
            try
            {
                Assert.AreEqual("Root", HandlerUtilities.BuildGameObjectPath(go));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BuildGameObjectPath_ChildObject_ReturnsFullPath()
        {
            var parent = new GameObject("Parent");
            var child = new GameObject("Child");
            child.transform.SetParent(parent.transform);
            try
            {
                Assert.AreEqual("Parent/Child", HandlerUtilities.BuildGameObjectPath(child));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        #endregion

        #region Vector3 Conversions

        [Test]
        public void GetVector3FromDict_NullDict_ReturnsFallback()
        {
            var result = HandlerUtilities.GetVector3FromDict(null, Vector3.one);
            Assert.AreEqual(Vector3.one, result);
        }

        [Test]
        public void GetVector3FromDict_ValidDict_ReturnsVector()
        {
            var dict = new Dictionary<string, object> { ["x"] = 1.0f, ["y"] = 2.0f, ["z"] = 3.0f };
            var result = HandlerUtilities.GetVector3FromDict(dict);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), result);
        }

        [Test]
        public void GetVector3FromDict_PartialDict_UsesFallback()
        {
            var dict = new Dictionary<string, object> { ["x"] = 5.0f };
            var fallback = new Vector3(0, 10, 20);
            var result = HandlerUtilities.GetVector3FromDict(dict, fallback);
            Assert.AreEqual(5f, result.x);
            Assert.AreEqual(10f, result.y);
            Assert.AreEqual(20f, result.z);
        }

        [Test]
        public void GetVector3FromPayload_MissingKey_ReturnsFallback()
        {
            var payload = new Dictionary<string, object>();
            var result = HandlerUtilities.GetVector3FromPayload(payload, "position", Vector3.up);
            Assert.AreEqual(Vector3.up, result);
        }

        [Test]
        public void GetVector3FromPayload_ValidKey_ReturnsVector()
        {
            var payload = new Dictionary<string, object>
            {
                ["position"] = new Dictionary<string, object> { ["x"] = 1.0, ["y"] = 2.0, ["z"] = 3.0 }
            };
            var result = HandlerUtilities.GetVector3FromPayload(payload, "position");
            Assert.AreEqual(new Vector3(1f, 2f, 3f), result);
        }

        #endregion

        #region Vector2 Conversions

        [Test]
        public void GetVector2FromDict_NullDict_ReturnsFallback()
        {
            var result = HandlerUtilities.GetVector2FromDict(null, Vector2.one);
            Assert.AreEqual(Vector2.one, result);
        }

        [Test]
        public void GetVector2FromDict_ValidDict_ReturnsVector()
        {
            var dict = new Dictionary<string, object> { ["x"] = 3.0f, ["y"] = 4.0f };
            var result = HandlerUtilities.GetVector2FromDict(dict);
            Assert.AreEqual(new Vector2(3f, 4f), result);
        }

        [Test]
        public void GetVector2FromPayload_MissingKey_ReturnsFallback()
        {
            var payload = new Dictionary<string, object>();
            var result = HandlerUtilities.GetVector2FromPayload(payload, "size", Vector2.one);
            Assert.AreEqual(Vector2.one, result);
        }

        #endregion

        #region Color Conversions

        [Test]
        public void GetColorFromDict_NullDict_ReturnsFallback()
        {
            var result = HandlerUtilities.GetColorFromDict(null, Color.red);
            Assert.AreEqual(Color.red, result);
        }

        [Test]
        public void GetColorFromDict_ValidDict_ReturnsColor()
        {
            var dict = new Dictionary<string, object> { ["r"] = 0.5f, ["g"] = 0.6f, ["b"] = 0.7f, ["a"] = 1.0f };
            var result = HandlerUtilities.GetColorFromDict(dict);
            Assert.AreEqual(0.5f, result.r, 0.001f);
            Assert.AreEqual(0.6f, result.g, 0.001f);
            Assert.AreEqual(0.7f, result.b, 0.001f);
            Assert.AreEqual(1.0f, result.a, 0.001f);
        }

        [Test]
        public void GetColorFromPayload_MissingKey_ReturnsFallback()
        {
            var payload = new Dictionary<string, object>();
            var result = HandlerUtilities.GetColorFromPayload(payload, "color", Color.white);
            Assert.AreEqual(Color.white, result);
        }

        #endregion

        #region Serialization

        [Test]
        public void SerializeVector3_ReturnsCorrectDict()
        {
            var dict = HandlerUtilities.SerializeVector3(new Vector3(1, 2, 3));
            Assert.AreEqual(1f, dict["x"]);
            Assert.AreEqual(2f, dict["y"]);
            Assert.AreEqual(3f, dict["z"]);
        }

        [Test]
        public void SerializeVector2_ReturnsCorrectDict()
        {
            var dict = HandlerUtilities.SerializeVector2(new Vector2(5, 6));
            Assert.AreEqual(5f, dict["x"]);
            Assert.AreEqual(6f, dict["y"]);
        }

        [Test]
        public void SerializeColor_ReturnsCorrectDict()
        {
            var dict = HandlerUtilities.SerializeColor(new Color(0.1f, 0.2f, 0.3f, 0.4f));
            Assert.AreEqual(0.1f, (float)dict["r"], 0.001f);
            Assert.AreEqual(0.2f, (float)dict["g"], 0.001f);
            Assert.AreEqual(0.3f, (float)dict["b"], 0.001f);
            Assert.AreEqual(0.4f, (float)dict["a"], 0.001f);
        }

        [Test]
        public void SerializeQuaternion_ReturnsCorrectDict()
        {
            var q = Quaternion.identity;
            var dict = HandlerUtilities.SerializeQuaternion(q);
            Assert.AreEqual(0f, (float)dict["x"], 0.001f);
            Assert.AreEqual(0f, (float)dict["y"], 0.001f);
            Assert.AreEqual(0f, (float)dict["z"], 0.001f);
            Assert.AreEqual(1f, (float)dict["w"], 0.001f);
        }

        #endregion

        #region GetStringList

        [Test]
        public void GetStringList_NullPayload_ReturnsEmptyList()
        {
            var result = HandlerUtilities.GetStringList(null, "key");
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetStringList_MissingKey_ReturnsEmptyList()
        {
            var payload = new Dictionary<string, object>();
            var result = HandlerUtilities.GetStringList(payload, "missing");
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetStringList_ListOfObjects_ReturnsStrings()
        {
            var payload = new Dictionary<string, object>
            {
                ["items"] = new List<object> { "a", "b", "c" }
            };
            var result = HandlerUtilities.GetStringList(payload, "items");
            Assert.AreEqual(3, result.Count);
            Assert.Contains("a", result);
            Assert.Contains("b", result);
            Assert.Contains("c", result);
        }

        [Test]
        public void GetStringList_SingleString_ReturnsListWithOneItem()
        {
            var payload = new Dictionary<string, object>
            {
                ["item"] = "single"
            };
            var result = HandlerUtilities.GetStringList(payload, "item");
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("single", result[0]);
        }

        #endregion

        #region Payload Helpers

        [Test]
        public void GetBoolOrDefault_MissingKey_ReturnsDefault()
        {
            var payload = new Dictionary<string, object>();
            Assert.IsTrue(HandlerUtilities.GetBoolOrDefault(payload, "key", true));
            Assert.IsFalse(HandlerUtilities.GetBoolOrDefault(payload, "key", false));
        }

        [Test]
        public void GetBoolOrDefault_BoolValue_ReturnsBool()
        {
            var payload = new Dictionary<string, object> { ["flag"] = true };
            Assert.IsTrue(HandlerUtilities.GetBoolOrDefault(payload, "flag"));
        }

        [Test]
        public void GetIntOrDefault_MissingKey_ReturnsDefault()
        {
            var payload = new Dictionary<string, object>();
            Assert.AreEqual(42, HandlerUtilities.GetIntOrDefault(payload, "key", 42));
        }

        [Test]
        public void GetFloatOrDefault_MissingKey_ReturnsDefault()
        {
            var payload = new Dictionary<string, object>();
            Assert.AreEqual(1.5f, HandlerUtilities.GetFloatOrDefault(payload, "key", 1.5f));
        }

        [Test]
        public void GetDictOrDefault_MissingKey_ReturnsNull()
        {
            var payload = new Dictionary<string, object>();
            Assert.IsNull(HandlerUtilities.GetDictOrDefault(payload, "key"));
        }

        [Test]
        public void GetDictOrDefault_ValidKey_ReturnsDict()
        {
            var inner = new Dictionary<string, object> { ["a"] = 1 };
            var payload = new Dictionary<string, object> { ["nested"] = inner };
            var result = HandlerUtilities.GetDictOrDefault(payload, "nested");
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result["a"]);
        }

        #endregion
    }
}
