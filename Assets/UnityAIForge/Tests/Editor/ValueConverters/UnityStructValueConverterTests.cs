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
        public void Convert_UnknownStringConstantToVector3_ThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                _converter.Convert("not_a_valid_constant", typeof(Vector3));
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

        #region String Constant Tests - Color

        [Test]
        public void Convert_StringRedToColor_ReturnsColorRed()
        {
            var result = _converter.Convert("red", typeof(Color));

            Assert.IsInstanceOf<Color>(result);
            Assert.AreEqual(Color.red, result);
        }

        [Test]
        public void Convert_StringRedToColor_CaseInsensitive()
        {
            var result = _converter.Convert("RED", typeof(Color));

            Assert.IsInstanceOf<Color>(result);
            Assert.AreEqual(Color.red, result);
        }

        [Test]
        public void Convert_StringGreenToColor_ReturnsColorGreen()
        {
            var result = _converter.Convert("green", typeof(Color));

            Assert.IsInstanceOf<Color>(result);
            Assert.AreEqual(Color.green, result);
        }

        [Test]
        public void Convert_StringBlueToColor_ReturnsColorBlue()
        {
            var result = _converter.Convert("blue", typeof(Color));

            Assert.IsInstanceOf<Color>(result);
            Assert.AreEqual(Color.blue, result);
        }

        [Test]
        public void Convert_StringWhiteToColor_ReturnsColorWhite()
        {
            var result = _converter.Convert("white", typeof(Color));

            Assert.IsInstanceOf<Color>(result);
            Assert.AreEqual(Color.white, result);
        }

        [Test]
        public void Convert_StringBlackToColor_ReturnsColorBlack()
        {
            var result = _converter.Convert("black", typeof(Color));

            Assert.IsInstanceOf<Color>(result);
            Assert.AreEqual(Color.black, result);
        }

        [Test]
        public void Convert_StringYellowToColor_ReturnsColorYellow()
        {
            var result = _converter.Convert("yellow", typeof(Color));

            Assert.IsInstanceOf<Color>(result);
            Assert.AreEqual(Color.yellow, result);
        }

        [Test]
        public void Convert_StringCyanToColor_ReturnsColorCyan()
        {
            var result = _converter.Convert("cyan", typeof(Color));

            Assert.IsInstanceOf<Color>(result);
            Assert.AreEqual(Color.cyan, result);
        }

        [Test]
        public void Convert_StringMagentaToColor_ReturnsColorMagenta()
        {
            var result = _converter.Convert("magenta", typeof(Color));

            Assert.IsInstanceOf<Color>(result);
            Assert.AreEqual(Color.magenta, result);
        }

        [Test]
        public void Convert_StringGrayToColor_ReturnsColorGray()
        {
            var result = _converter.Convert("gray", typeof(Color));

            Assert.IsInstanceOf<Color>(result);
            Assert.AreEqual(Color.gray, result);
        }

        [Test]
        public void Convert_StringClearToColor_ReturnsColorClear()
        {
            var result = _converter.Convert("clear", typeof(Color));

            Assert.IsInstanceOf<Color>(result);
            Assert.AreEqual(Color.clear, result);
        }

        [Test]
        public void Convert_StringRedToColor32_ReturnsColor32Red()
        {
            var result = _converter.Convert("red", typeof(Color32));

            Assert.IsInstanceOf<Color32>(result);
            Assert.AreEqual((Color32)Color.red, result);
        }

        #endregion

        #region String Constant Tests - Vector3

        [Test]
        public void Convert_StringZeroToVector3_ReturnsVector3Zero()
        {
            var result = _converter.Convert("zero", typeof(Vector3));

            Assert.IsInstanceOf<Vector3>(result);
            Assert.AreEqual(Vector3.zero, result);
        }

        [Test]
        public void Convert_StringOneToVector3_ReturnsVector3One()
        {
            var result = _converter.Convert("one", typeof(Vector3));

            Assert.IsInstanceOf<Vector3>(result);
            Assert.AreEqual(Vector3.one, result);
        }

        [Test]
        public void Convert_StringUpToVector3_ReturnsVector3Up()
        {
            var result = _converter.Convert("up", typeof(Vector3));

            Assert.IsInstanceOf<Vector3>(result);
            Assert.AreEqual(Vector3.up, result);
        }

        [Test]
        public void Convert_StringDownToVector3_ReturnsVector3Down()
        {
            var result = _converter.Convert("down", typeof(Vector3));

            Assert.IsInstanceOf<Vector3>(result);
            Assert.AreEqual(Vector3.down, result);
        }

        [Test]
        public void Convert_StringLeftToVector3_ReturnsVector3Left()
        {
            var result = _converter.Convert("left", typeof(Vector3));

            Assert.IsInstanceOf<Vector3>(result);
            Assert.AreEqual(Vector3.left, result);
        }

        [Test]
        public void Convert_StringRightToVector3_ReturnsVector3Right()
        {
            var result = _converter.Convert("right", typeof(Vector3));

            Assert.IsInstanceOf<Vector3>(result);
            Assert.AreEqual(Vector3.right, result);
        }

        [Test]
        public void Convert_StringForwardToVector3_ReturnsVector3Forward()
        {
            var result = _converter.Convert("forward", typeof(Vector3));

            Assert.IsInstanceOf<Vector3>(result);
            Assert.AreEqual(Vector3.forward, result);
        }

        [Test]
        public void Convert_StringBackToVector3_ReturnsVector3Back()
        {
            var result = _converter.Convert("back", typeof(Vector3));

            Assert.IsInstanceOf<Vector3>(result);
            Assert.AreEqual(Vector3.back, result);
        }

        #endregion

        #region String Constant Tests - Vector2

        [Test]
        public void Convert_StringZeroToVector2_ReturnsVector2Zero()
        {
            var result = _converter.Convert("zero", typeof(Vector2));

            Assert.IsInstanceOf<Vector2>(result);
            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void Convert_StringOneToVector2_ReturnsVector2One()
        {
            var result = _converter.Convert("one", typeof(Vector2));

            Assert.IsInstanceOf<Vector2>(result);
            Assert.AreEqual(Vector2.one, result);
        }

        [Test]
        public void Convert_StringUpToVector2_ReturnsVector2Up()
        {
            var result = _converter.Convert("up", typeof(Vector2));

            Assert.IsInstanceOf<Vector2>(result);
            Assert.AreEqual(Vector2.up, result);
        }

        #endregion

        #region String Constant Tests - Vector4

        [Test]
        public void Convert_StringZeroToVector4_ReturnsVector4Zero()
        {
            var result = _converter.Convert("zero", typeof(Vector4));

            Assert.IsInstanceOf<Vector4>(result);
            Assert.AreEqual(Vector4.zero, result);
        }

        [Test]
        public void Convert_StringOneToVector4_ReturnsVector4One()
        {
            var result = _converter.Convert("one", typeof(Vector4));

            Assert.IsInstanceOf<Vector4>(result);
            Assert.AreEqual(Vector4.one, result);
        }

        #endregion

        #region String Constant Tests - Quaternion

        [Test]
        public void Convert_StringIdentityToQuaternion_ReturnsQuaternionIdentity()
        {
            var result = _converter.Convert("identity", typeof(Quaternion));

            Assert.IsInstanceOf<Quaternion>(result);
            Assert.AreEqual(Quaternion.identity, result);
        }

        #endregion

        #region GetSupportedConstants Tests

        [Test]
        public void GetSupportedConstants_Color_ReturnsColorConstants()
        {
            var constants = UnityStructValueConverter.GetSupportedConstants(typeof(Color));

            Assert.IsNotNull(constants);
            Assert.Contains("red", constants);
            Assert.Contains("green", constants);
            Assert.Contains("blue", constants);
            Assert.Contains("white", constants);
            Assert.Contains("black", constants);
        }

        [Test]
        public void GetSupportedConstants_Vector3_ReturnsVector3Constants()
        {
            var constants = UnityStructValueConverter.GetSupportedConstants(typeof(Vector3));

            Assert.IsNotNull(constants);
            Assert.Contains("zero", constants);
            Assert.Contains("one", constants);
            Assert.Contains("up", constants);
            Assert.Contains("forward", constants);
        }

        [Test]
        public void GetSupportedConstants_UnsupportedType_ReturnsEmptyArray()
        {
            var constants = UnityStructValueConverter.GetSupportedConstants(typeof(int));

            Assert.IsNotNull(constants);
            Assert.AreEqual(0, constants.Length);
        }

        #endregion
    }
}
