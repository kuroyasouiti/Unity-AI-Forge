using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using MCP.Editor.Interfaces;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests.ValueConverters
{
    /// <summary>
    /// ValueConverterManagerのテストクラス。
    /// </summary>
    [TestFixture]
    public class ValueConverterManagerTests
    {
        private ValueConverterManager _manager;

        [SetUp]
        public void SetUp()
        {
            // シングルトンをリセットしてテスト用のインスタンスを取得
            ValueConverterManager.ResetInstance();
            _manager = ValueConverterManager.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            ValueConverterManager.ResetInstance();
        }

        #region Singleton Tests

        [Test]
        public void Instance_ReturnsSameInstance()
        {
            var instance1 = ValueConverterManager.Instance;
            var instance2 = ValueConverterManager.Instance;

            Assert.AreSame(instance1, instance2);
        }

        [Test]
        public void ResetInstance_CreatesNewInstance()
        {
            var instance1 = ValueConverterManager.Instance;
            ValueConverterManager.ResetInstance();
            var instance2 = ValueConverterManager.Instance;

            Assert.AreNotSame(instance1, instance2);
        }

        #endregion

        #region Convert Primitive Types Tests

        [Test]
        public void Convert_DoubleToFloat_Success()
        {
            var result = _manager.Convert(3.14, typeof(float));

            Assert.IsInstanceOf<float>(result);
            Assert.AreEqual(3.14f, result);
        }

        [Test]
        public void Convert_StringToInt_Success()
        {
            var result = _manager.Convert("42", typeof(int));

            Assert.IsInstanceOf<int>(result);
            Assert.AreEqual(42, result);
        }

        [Test]
        public void Convert_StringToBool_Success()
        {
            var result = _manager.Convert("true", typeof(bool));

            Assert.IsInstanceOf<bool>(result);
            Assert.AreEqual(true, result);
        }

        #endregion

        #region Convert Unity Struct Tests

        [Test]
        public void Convert_DictionaryToVector3_Success()
        {
            var dict = new Dictionary<string, object>
            {
                ["x"] = 1.0,
                ["y"] = 2.0,
                ["z"] = 3.0
            };

            var result = _manager.Convert(dict, typeof(Vector3));

            Assert.IsInstanceOf<Vector3>(result);
            var vector = (Vector3)result;
            Assert.AreEqual(new Vector3(1f, 2f, 3f), vector);
        }

        [Test]
        public void Convert_DictionaryToColor_Success()
        {
            var dict = new Dictionary<string, object>
            {
                ["r"] = 1.0,
                ["g"] = 0.5,
                ["b"] = 0.0,
                ["a"] = 1.0
            };

            var result = _manager.Convert(dict, typeof(Color));

            Assert.IsInstanceOf<Color>(result);
            var color = (Color)result;
            Assert.AreEqual(1f, color.r, 0.001f);
            Assert.AreEqual(0.5f, color.g, 0.001f);
            Assert.AreEqual(0f, color.b, 0.001f);
        }

        #endregion

        #region Convert Enum Tests

        [Test]
        public void Convert_StringToRigidbodyType2D_Success()
        {
            var result = _manager.Convert("Dynamic", typeof(RigidbodyType2D));

            Assert.IsInstanceOf<RigidbodyType2D>(result);
            Assert.AreEqual(RigidbodyType2D.Dynamic, result);
        }

        [Test]
        public void Convert_IntToRigidbodyType2D_Success()
        {
            var result = _manager.Convert(1, typeof(RigidbodyType2D));

            Assert.IsInstanceOf<RigidbodyType2D>(result);
            Assert.AreEqual(RigidbodyType2D.Kinematic, result);
        }

        #endregion

        #region Same Type Tests

        [Test]
        public void Convert_SameType_ReturnsSameValue()
        {
            var original = new Vector3(1, 2, 3);
            var result = _manager.Convert(original, typeof(Vector3));

            Assert.AreEqual(original, result);
        }

        [Test]
        public void Convert_IntToInt_ReturnsSameValue()
        {
            var result = _manager.Convert(42, typeof(int));

            Assert.AreEqual(42, result);
        }

        #endregion

        #region Null Handling Tests

        [Test]
        public void Convert_Null_ToReferenceType_ReturnsNull()
        {
            var result = _manager.Convert(null, typeof(string));

            Assert.IsNull(result);
        }

        [Test]
        public void Convert_Null_ToValueType_ReturnsDefault()
        {
            var result = _manager.Convert(null, typeof(int));

            Assert.AreEqual(0, result);
        }

        [Test]
        public void Convert_Null_ToVector3_ReturnsDefault()
        {
            var result = _manager.Convert(null, typeof(Vector3));

            Assert.AreEqual(Vector3.zero, result);
        }

        #endregion

        #region TryConvert Tests

        [Test]
        public void TryConvert_ValidConversion_ReturnsTrue()
        {
            var success = _manager.TryConvert(3.14, typeof(float), out var result);

            Assert.IsTrue(success);
            Assert.AreEqual(3.14f, result);
        }

        [Test]
        public void TryConvert_InvalidConversion_ReturnsFalse()
        {
            // 非対応の複雑な型への変換を試みる
            var success = _manager.TryConvert("invalid", typeof(LayerMask), out var result);

            Assert.IsFalse(success);
        }

        #endregion

        #region Unsupported Type Tests

        [Test]
        public void Convert_UnsupportedUnityStruct_ReturnsNull()
        {
            // LayerMaskはサポートされていない
            var result = _manager.Convert(123, typeof(LayerMask));

            Assert.IsNull(result);
        }

        #endregion

        #region Custom Converter Tests

        [Test]
        public void RegisterConverter_CustomConverter_IsUsed()
        {
            // カスタムコンバーターを作成
            var customConverter = new TestCustomConverter();
            _manager.RegisterConverter(customConverter);

            // カスタムコンバーターが処理する型を変換
            var result = _manager.Convert("custom_input", typeof(TestCustomType));

            Assert.IsNotNull(result);
            Assert.IsInstanceOf<TestCustomType>(result);
            Assert.AreEqual("converted", ((TestCustomType)result).Value);
        }

        // テスト用のカスタム型
        private class TestCustomType
        {
            public string Value { get; set; }
        }

        // テスト用のカスタムコンバーター
        private class TestCustomConverter : IValueConverter
        {
            public int Priority => 1000; // 最高優先度

            public bool CanConvert(Type targetType)
            {
                return targetType == typeof(TestCustomType);
            }

            public object Convert(object value, Type targetType)
            {
                return new TestCustomType { Value = "converted" };
            }
        }

        #endregion

        #region Priority Tests

        [Test]
        public void Convert_HigherPriorityConverterIsUsedFirst()
        {
            // UnityObjectReferenceConverter (300) > UnityStructConverter (200) > EnumConverter (150) > PrimitiveConverter (100)
            // Vector3はUnityStructConverterが処理
            var dict = new Dictionary<string, object>
            {
                ["x"] = 1.0,
                ["y"] = 2.0,
                ["z"] = 3.0
            };

            var result = _manager.Convert(dict, typeof(Vector3));

            Assert.IsInstanceOf<Vector3>(result);
        }

        #endregion
    }
}
