using System;
using MCP.Editor.Base.ValueConverters;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests.ValueConverters
{
    /// <summary>
    /// EnumValueConverterのテストクラス。
    /// </summary>
    [TestFixture]
    public class EnumValueConverterTests
    {
        private EnumValueConverter _converter;

        // テスト用の列挙型
        private enum TestEnum
        {
            None = 0,
            First = 1,
            Second = 2,
            Third = 3
        }

        [SetUp]
        public void SetUp()
        {
            _converter = new EnumValueConverter();
        }

        #region CanConvert Tests

        [Test]
        public void CanConvert_Enum_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(TestEnum)));
        }

        [Test]
        public void CanConvert_RigidbodyType2D_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(RigidbodyType2D)));
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

        #region Convert From String Tests

        [Test]
        public void Convert_StringToEnum_Success()
        {
            var result = _converter.Convert("First", typeof(TestEnum));

            Assert.IsInstanceOf<TestEnum>(result);
            Assert.AreEqual(TestEnum.First, result);
        }

        [Test]
        public void Convert_StringToEnum_CaseInsensitive()
        {
            var result = _converter.Convert("second", typeof(TestEnum));

            Assert.IsInstanceOf<TestEnum>(result);
            Assert.AreEqual(TestEnum.Second, result);
        }

        [Test]
        public void Convert_StringToEnum_MixedCase()
        {
            var result = _converter.Convert("THIRD", typeof(TestEnum));

            Assert.IsInstanceOf<TestEnum>(result);
            Assert.AreEqual(TestEnum.Third, result);
        }

        [Test]
        public void Convert_InvalidString_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                _converter.Convert("InvalidValue", typeof(TestEnum));
            });
        }

        #endregion

        #region Convert From Int Tests

        [Test]
        public void Convert_IntToEnum_Success()
        {
            var result = _converter.Convert(1, typeof(TestEnum));

            Assert.IsInstanceOf<TestEnum>(result);
            Assert.AreEqual(TestEnum.First, result);
        }

        [Test]
        public void Convert_ZeroToEnum_Success()
        {
            var result = _converter.Convert(0, typeof(TestEnum));

            Assert.IsInstanceOf<TestEnum>(result);
            Assert.AreEqual(TestEnum.None, result);
        }

        [Test]
        public void Convert_LongToEnum_Success()
        {
            var result = _converter.Convert(2L, typeof(TestEnum));

            Assert.IsInstanceOf<TestEnum>(result);
            Assert.AreEqual(TestEnum.Second, result);
        }

        #endregion

        #region Unity Enum Tests

        [Test]
        public void Convert_StringToRigidbodyType2D_Dynamic()
        {
            var result = _converter.Convert("Dynamic", typeof(RigidbodyType2D));

            Assert.IsInstanceOf<RigidbodyType2D>(result);
            Assert.AreEqual(RigidbodyType2D.Dynamic, result);
        }

        [Test]
        public void Convert_StringToRigidbodyType2D_Kinematic()
        {
            var result = _converter.Convert("Kinematic", typeof(RigidbodyType2D));

            Assert.IsInstanceOf<RigidbodyType2D>(result);
            Assert.AreEqual(RigidbodyType2D.Kinematic, result);
        }

        [Test]
        public void Convert_StringToRigidbodyType2D_Static()
        {
            var result = _converter.Convert("Static", typeof(RigidbodyType2D));

            Assert.IsInstanceOf<RigidbodyType2D>(result);
            Assert.AreEqual(RigidbodyType2D.Static, result);
        }

        [Test]
        public void Convert_IntToRigidbodyType2D()
        {
            var result = _converter.Convert(1, typeof(RigidbodyType2D));

            Assert.IsInstanceOf<RigidbodyType2D>(result);
            Assert.AreEqual(RigidbodyType2D.Kinematic, result);
        }

        #endregion

        #region Same Type Tests

        [Test]
        public void Convert_SameEnumType_ReturnsSameValue()
        {
            var result = _converter.Convert(TestEnum.Second, typeof(TestEnum));

            Assert.AreEqual(TestEnum.Second, result);
        }

        #endregion

        #region Null Tests

        [Test]
        public void Convert_Null_ReturnsDefault()
        {
            var result = _converter.Convert(null, typeof(TestEnum));

            Assert.IsInstanceOf<TestEnum>(result);
            Assert.AreEqual(TestEnum.None, result); // デフォルト値 (0)
        }

        #endregion

        #region Priority Tests

        [Test]
        public void Priority_Returns150()
        {
            Assert.AreEqual(150, _converter.Priority);
        }

        #endregion
    }
}
