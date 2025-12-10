using System;
using System.Collections.Generic;
using MCP.Editor.Base.ValueConverters;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests.ValueConverters
{
    /// <summary>
    /// UnityStructValueConverterのテストクラス。
    /// </summary>
    [TestFixture]
    public class UnityStructValueConverterTests
    {
        private UnityStructValueConverter _converter;

        [SetUp]
        public void SetUp()
        {
            _converter = new UnityStructValueConverter();
        }

        #region CanConvert Tests

        [Test]
        public void CanConvert_Vector2_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(Vector2)));
        }

        [Test]
        public void CanConvert_Vector3_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(Vector3)));
        }

        [Test]
        public void CanConvert_Vector4_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(Vector4)));
        }

        [Test]
        public void CanConvert_Color_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(Color)));
        }

        [Test]
        public void CanConvert_Color32_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(Color32)));
        }

        [Test]
        public void CanConvert_Quaternion_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(Quaternion)));
        }

        [Test]
        public void CanConvert_Rect_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(Rect)));
        }

        [Test]
        public void CanConvert_Bounds_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(Bounds)));
        }

        [Test]
        public void CanConvert_Int_ReturnsFalse()
        {
            Assert.IsFalse(_converter.CanConvert(typeof(int)));
        }

        [Test]
        public void CanConvert_String_ReturnsFalse()
        {
            Assert.IsFalse(_converter.CanConvert(typeof(string)));
        }

        #endregion

        #region Convert Vector2 Tests

        [Test]
        public void Convert_DictionaryToVector2_Success()
        {
            var dict = new Dictionary<string, object>
            {
                ["x"] = 1.0,
                ["y"] = 2.0
            };

            var result = _converter.Convert(dict, typeof(Vector2));

            Assert.IsInstanceOf<Vector2>(result);
            var vector = (Vector2)result;
            Assert.AreEqual(1f, vector.x);
            Assert.AreEqual(2f, vector.y);
        }

        [Test]
        public void Convert_DictionaryToVector2_WithIntValues_Success()
        {
            var dict = new Dictionary<string, object>
            {
                ["x"] = 5,
                ["y"] = 10
            };

            var result = _converter.Convert(dict, typeof(Vector2));

            Assert.IsInstanceOf<Vector2>(result);
            var vector = (Vector2)result;
            Assert.AreEqual(5f, vector.x);
            Assert.AreEqual(10f, vector.y);
        }

        [Test]
        public void Convert_DictionaryToVector2_MissingKeys_UsesDefaults()
        {
            var dict = new Dictionary<string, object>
            {
                ["x"] = 5.0
            };

            var result = _converter.Convert(dict, typeof(Vector2));

            Assert.IsInstanceOf<Vector2>(result);
            var vector = (Vector2)result;
            Assert.AreEqual(5f, vector.x);
            Assert.AreEqual(0f, vector.y);
        }

        #endregion

        #region Convert Vector3 Tests

        [Test]
        public void Convert_DictionaryToVector3_Success()
        {
            var dict = new Dictionary<string, object>
            {
                ["x"] = 1.0,
                ["y"] = 2.0,
                ["z"] = 3.0
            };

            var result = _converter.Convert(dict, typeof(Vector3));

            Assert.IsInstanceOf<Vector3>(result);
            var vector = (Vector3)result;
            Assert.AreEqual(1f, vector.x);
            Assert.AreEqual(2f, vector.y);
            Assert.AreEqual(3f, vector.z);
        }

        [Test]
        public void Convert_DictionaryToVector3_PartialValues_Success()
        {
            var dict = new Dictionary<string, object>
            {
                ["y"] = 5.0
            };

            var result = _converter.Convert(dict, typeof(Vector3));

            Assert.IsInstanceOf<Vector3>(result);
            var vector = (Vector3)result;
            Assert.AreEqual(0f, vector.x);
            Assert.AreEqual(5f, vector.y);
            Assert.AreEqual(0f, vector.z);
        }

        #endregion

        #region Convert Vector4 Tests

        [Test]
        public void Convert_DictionaryToVector4_Success()
        {
            var dict = new Dictionary<string, object>
            {
                ["x"] = 1.0,
                ["y"] = 2.0,
                ["z"] = 3.0,
                ["w"] = 4.0
            };

            var result = _converter.Convert(dict, typeof(Vector4));

            Assert.IsInstanceOf<Vector4>(result);
            var vector = (Vector4)result;
            Assert.AreEqual(1f, vector.x);
            Assert.AreEqual(2f, vector.y);
            Assert.AreEqual(3f, vector.z);
            Assert.AreEqual(4f, vector.w);
        }

        #endregion

        #region Convert Color Tests

        [Test]
        public void Convert_DictionaryToColor_Success()
        {
            var dict = new Dictionary<string, object>
            {
                ["r"] = 0.5,
                ["g"] = 0.6,
                ["b"] = 0.7,
                ["a"] = 0.8
            };

            var result = _converter.Convert(dict, typeof(Color));

            Assert.IsInstanceOf<Color>(result);
            var color = (Color)result;
            Assert.AreEqual(0.5f, color.r, 0.001f);
            Assert.AreEqual(0.6f, color.g, 0.001f);
            Assert.AreEqual(0.7f, color.b, 0.001f);
            Assert.AreEqual(0.8f, color.a, 0.001f);
        }

        [Test]
        public void Convert_DictionaryToColor_DefaultAlpha()
        {
            var dict = new Dictionary<string, object>
            {
                ["r"] = 1.0,
                ["g"] = 0.0,
                ["b"] = 0.0
            };

            var result = _converter.Convert(dict, typeof(Color));

            Assert.IsInstanceOf<Color>(result);
            var color = (Color)result;
            Assert.AreEqual(1f, color.r);
            Assert.AreEqual(0f, color.g);
            Assert.AreEqual(0f, color.b);
            Assert.AreEqual(1f, color.a); // デフォルト値
        }

        #endregion

        #region Convert Quaternion Tests

        [Test]
        public void Convert_DictionaryToQuaternion_Success()
        {
            var dict = new Dictionary<string, object>
            {
                ["x"] = 0.0,
                ["y"] = 0.0,
                ["z"] = 0.0,
                ["w"] = 1.0
            };

            var result = _converter.Convert(dict, typeof(Quaternion));

            Assert.IsInstanceOf<Quaternion>(result);
            var quat = (Quaternion)result;
            Assert.AreEqual(0f, quat.x);
            Assert.AreEqual(0f, quat.y);
            Assert.AreEqual(0f, quat.z);
            Assert.AreEqual(1f, quat.w);
        }

        [Test]
        public void Convert_DictionaryToQuaternion_DefaultW()
        {
            var dict = new Dictionary<string, object>
            {
                ["x"] = 0.0,
                ["y"] = 0.0,
                ["z"] = 0.0
            };

            var result = _converter.Convert(dict, typeof(Quaternion));

            Assert.IsInstanceOf<Quaternion>(result);
            var quat = (Quaternion)result;
            Assert.AreEqual(1f, quat.w); // デフォルト値
        }

        #endregion

        #region Convert Rect Tests

        [Test]
        public void Convert_DictionaryToRect_Success()
        {
            var dict = new Dictionary<string, object>
            {
                ["x"] = 10.0,
                ["y"] = 20.0,
                ["width"] = 100.0,
                ["height"] = 50.0
            };

            var result = _converter.Convert(dict, typeof(Rect));

            Assert.IsInstanceOf<Rect>(result);
            var rect = (Rect)result;
            Assert.AreEqual(10f, rect.x);
            Assert.AreEqual(20f, rect.y);
            Assert.AreEqual(100f, rect.width);
            Assert.AreEqual(50f, rect.height);
        }

        #endregion

        #region Convert Bounds Tests

        [Test]
        public void Convert_DictionaryToBounds_Success()
        {
            var dict = new Dictionary<string, object>
            {
                ["center"] = new Dictionary<string, object>
                {
                    ["x"] = 1.0,
                    ["y"] = 2.0,
                    ["z"] = 3.0
                },
                ["size"] = new Dictionary<string, object>
                {
                    ["x"] = 10.0,
                    ["y"] = 20.0,
                    ["z"] = 30.0
                }
            };

            var result = _converter.Convert(dict, typeof(Bounds));

            Assert.IsInstanceOf<Bounds>(result);
            var bounds = (Bounds)result;
            Assert.AreEqual(new Vector3(1f, 2f, 3f), bounds.center);
            Assert.AreEqual(new Vector3(10f, 20f, 30f), bounds.size);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Convert_NonDictionaryToVector3_ThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                _converter.Convert("not a dictionary", typeof(Vector3));
            });
        }

        [Test]
        public void Convert_Null_ReturnsDefault()
        {
            var result = _converter.Convert(null, typeof(Vector3));
            Assert.IsInstanceOf<Vector3>(result);
            Assert.AreEqual(Vector3.zero, result);
        }

        #endregion

        #region Priority Tests

        [Test]
        public void Priority_Returns200()
        {
            Assert.AreEqual(200, _converter.Priority);
        }

        #endregion
    }
}
