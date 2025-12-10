using System;
using MCP.Editor.Base.ValueConverters;
using NUnit.Framework;

namespace MCP.Editor.Tests.ValueConverters
{
    /// <summary>
    /// PrimitiveValueConverterのテストクラス。
    /// </summary>
    [TestFixture]
    public class PrimitiveValueConverterTests
    {
        private PrimitiveValueConverter _converter;

        [SetUp]
        public void SetUp()
        {
            _converter = new PrimitiveValueConverter();
        }

        #region CanConvert Tests

        [Test]
        public void CanConvert_Float_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(float)));
        }

        [Test]
        public void CanConvert_Int_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(int)));
        }

        [Test]
        public void CanConvert_Bool_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(bool)));
        }

        [Test]
        public void CanConvert_String_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(string)));
        }

        [Test]
        public void CanConvert_Double_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(double)));
        }

        [Test]
        public void CanConvert_Long_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(long)));
        }

        [Test]
        public void CanConvert_Short_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(short)));
        }

        [Test]
        public void CanConvert_Byte_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(byte)));
        }

        [Test]
        public void CanConvert_Vector3_ReturnsFalse()
        {
            Assert.IsFalse(_converter.CanConvert(typeof(UnityEngine.Vector3)));
        }

        #endregion

        #region Convert Float Tests

        [Test]
        public void Convert_DoubleToFloat_Success()
        {
            var result = _converter.Convert(3.14, typeof(float));
            Assert.AreEqual(3.14f, result);
        }

        [Test]
        public void Convert_IntToFloat_Success()
        {
            var result = _converter.Convert(42, typeof(float));
            Assert.AreEqual(42f, result);
        }

        [Test]
        public void Convert_LongToFloat_Success()
        {
            var result = _converter.Convert(100L, typeof(float));
            Assert.AreEqual(100f, result);
        }

        [Test]
        public void Convert_StringToFloat_Success()
        {
            var result = _converter.Convert("2.5", typeof(float));
            Assert.AreEqual(2.5f, result);
        }

        #endregion

        #region Convert Int Tests

        [Test]
        public void Convert_LongToInt_Success()
        {
            var result = _converter.Convert(100L, typeof(int));
            Assert.AreEqual(100, result);
        }

        [Test]
        public void Convert_DoubleToInt_Success()
        {
            var result = _converter.Convert(42.9, typeof(int));
            Assert.AreEqual(42, result);
        }

        [Test]
        public void Convert_FloatToInt_Success()
        {
            var result = _converter.Convert(10.5f, typeof(int));
            Assert.AreEqual(10, result);
        }

        [Test]
        public void Convert_StringToInt_Success()
        {
            var result = _converter.Convert("123", typeof(int));
            Assert.AreEqual(123, result);
        }

        #endregion

        #region Convert Bool Tests

        [Test]
        public void Convert_StringTrueTooBool_ReturnsTrue()
        {
            var result = _converter.Convert("true", typeof(bool));
            Assert.AreEqual(true, result);
        }

        [Test]
        public void Convert_StringFalseToBool_ReturnsFalse()
        {
            var result = _converter.Convert("false", typeof(bool));
            Assert.AreEqual(false, result);
        }

        [Test]
        public void Convert_IntOneToBool_ReturnsTrue()
        {
            var result = _converter.Convert(1, typeof(bool));
            Assert.AreEqual(true, result);
        }

        [Test]
        public void Convert_IntZeroToBool_ReturnsFalse()
        {
            var result = _converter.Convert(0, typeof(bool));
            Assert.AreEqual(false, result);
        }

        #endregion

        #region Convert String Tests

        [Test]
        public void Convert_IntToString_Success()
        {
            var result = _converter.Convert(42, typeof(string));
            Assert.AreEqual("42", result);
        }

        [Test]
        public void Convert_FloatToString_Success()
        {
            var result = _converter.Convert(3.14f, typeof(string));
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<string>(result);
        }

        [Test]
        public void Convert_NullToString_ReturnsNull()
        {
            var result = _converter.Convert(null, typeof(string));
            Assert.IsNull(result);
        }

        #endregion

        #region Convert Other Numeric Types Tests

        [Test]
        public void Convert_IntToDouble_Success()
        {
            var result = _converter.Convert(42, typeof(double));
            Assert.AreEqual(42.0, result);
        }

        [Test]
        public void Convert_IntToLong_Success()
        {
            var result = _converter.Convert(42, typeof(long));
            Assert.AreEqual(42L, result);
        }

        [Test]
        public void Convert_IntToShort_Success()
        {
            var result = _converter.Convert(42, typeof(short));
            Assert.AreEqual((short)42, result);
        }

        [Test]
        public void Convert_IntToByte_Success()
        {
            var result = _converter.Convert(42, typeof(byte));
            Assert.AreEqual((byte)42, result);
        }

        #endregion

        #region Same Type Tests

        [Test]
        public void Convert_FloatToFloat_ReturnsSameValue()
        {
            var result = _converter.Convert(3.14f, typeof(float));
            Assert.AreEqual(3.14f, result);
        }

        [Test]
        public void Convert_IntToInt_ReturnsSameValue()
        {
            var result = _converter.Convert(42, typeof(int));
            Assert.AreEqual(42, result);
        }

        #endregion

        #region Priority Tests

        [Test]
        public void Priority_Returns100()
        {
            Assert.AreEqual(100, _converter.Priority);
        }

        #endregion
    }
}
