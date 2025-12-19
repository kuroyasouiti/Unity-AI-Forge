using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using MCP.Editor.Base.ValueConverters;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests.ValueConverters
{
    /// <summary>
    /// ArrayValueConverterのテストクラス。
    /// 配列およびList型の変換をテストします。
    /// </summary>
    [TestFixture]
    public class ArrayValueConverterTests
    {
        private ArrayValueConverter _converter;

        [SetUp]
        public void SetUp()
        {
            _converter = new ArrayValueConverter();
            ValueConverterManager.ResetInstance();
        }

        [TearDown]
        public void TearDown()
        {
            ValueConverterManager.ResetInstance();
        }

        #region CanConvert Tests

        [Test]
        public void CanConvert_IntArray_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(int[])));
        }

        [Test]
        public void CanConvert_StringArray_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(string[])));
        }

        [Test]
        public void CanConvert_ListOfInt_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(List<int>)));
        }

        [Test]
        public void CanConvert_ListOfString_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(List<string>)));
        }

        [Test]
        public void CanConvert_Vector3Array_ReturnsTrue()
        {
            Assert.IsTrue(_converter.CanConvert(typeof(Vector3[])));
        }

        [Test]
        public void CanConvert_NonArrayType_ReturnsFalse()
        {
            Assert.IsFalse(_converter.CanConvert(typeof(int)));
            Assert.IsFalse(_converter.CanConvert(typeof(string)));
            Assert.IsFalse(_converter.CanConvert(typeof(Vector3)));
        }

        [Test]
        public void CanConvert_Null_ReturnsFalse()
        {
            Assert.IsFalse(_converter.CanConvert(null));
        }

        #endregion

        #region Convert Array Tests

        [Test]
        public void Convert_ListOfObjectToIntArray_Success()
        {
            var input = new List<object> { 1, 2, 3, 4, 5 };
            var result = _converter.Convert(input, typeof(int[]));

            Assert.IsInstanceOf<int[]>(result);
            var array = (int[])result;
            Assert.AreEqual(5, array.Length);
            Assert.AreEqual(1, array[0]);
            Assert.AreEqual(5, array[4]);
        }

        [Test]
        public void Convert_ListOfObjectToStringArray_Success()
        {
            var input = new List<object> { "a", "b", "c" };
            var result = _converter.Convert(input, typeof(string[]));

            Assert.IsInstanceOf<string[]>(result);
            var array = (string[])result;
            Assert.AreEqual(3, array.Length);
            Assert.AreEqual("a", array[0]);
            Assert.AreEqual("c", array[2]);
        }

        [Test]
        public void Convert_ListOfObjectToFloatArray_Success()
        {
            var input = new List<object> { 1.5, 2.5, 3.5 };
            var result = _converter.Convert(input, typeof(float[]));

            Assert.IsInstanceOf<float[]>(result);
            var array = (float[])result;
            Assert.AreEqual(3, array.Length);
            Assert.AreEqual(1.5f, array[0], 0.001f);
            Assert.AreEqual(3.5f, array[2], 0.001f);
        }

        [Test]
        public void Convert_ObjectArrayToIntArray_Success()
        {
            var input = new object[] { 10, 20, 30 };
            var result = _converter.Convert(input, typeof(int[]));

            Assert.IsInstanceOf<int[]>(result);
            var array = (int[])result;
            Assert.AreEqual(3, array.Length);
            Assert.AreEqual(10, array[0]);
            Assert.AreEqual(30, array[2]);
        }

        #endregion

        #region Convert List Tests

        [Test]
        public void Convert_ListOfObjectToListOfInt_Success()
        {
            var input = new List<object> { 1, 2, 3 };
            var result = _converter.Convert(input, typeof(List<int>));

            Assert.IsInstanceOf<List<int>>(result);
            var list = (List<int>)result;
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(3, list[2]);
        }

        [Test]
        public void Convert_ListOfObjectToListOfString_Success()
        {
            var input = new List<object> { "x", "y", "z" };
            var result = _converter.Convert(input, typeof(List<string>));

            Assert.IsInstanceOf<List<string>>(result);
            var list = (List<string>)result;
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("x", list[0]);
            Assert.AreEqual("z", list[2]);
        }

        [Test]
        public void Convert_ObjectArrayToListOfFloat_Success()
        {
            var input = new object[] { 1.1, 2.2, 3.3 };
            var result = _converter.Convert(input, typeof(List<float>));

            Assert.IsInstanceOf<List<float>>(result);
            var list = (List<float>)result;
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(1.1f, list[0], 0.001f);
            Assert.AreEqual(3.3f, list[2], 0.001f);
        }

        #endregion

        #region Convert Unity Types Tests

        [Test]
        public void Convert_ListOfDictionaryToVector3Array_Success()
        {
            var input = new List<object>
            {
                new Dictionary<string, object> { ["x"] = 1.0, ["y"] = 2.0, ["z"] = 3.0 },
                new Dictionary<string, object> { ["x"] = 4.0, ["y"] = 5.0, ["z"] = 6.0 }
            };
            var result = _converter.Convert(input, typeof(Vector3[]));

            Assert.IsInstanceOf<Vector3[]>(result);
            var array = (Vector3[])result;
            Assert.AreEqual(2, array.Length);
            Assert.AreEqual(new Vector3(1, 2, 3), array[0]);
            Assert.AreEqual(new Vector3(4, 5, 6), array[1]);
        }

        [Test]
        public void Convert_ListOfDictionaryToColorArray_Success()
        {
            var input = new List<object>
            {
                new Dictionary<string, object> { ["r"] = 1.0, ["g"] = 0.0, ["b"] = 0.0, ["a"] = 1.0 },
                new Dictionary<string, object> { ["r"] = 0.0, ["g"] = 1.0, ["b"] = 0.0, ["a"] = 1.0 }
            };
            var result = _converter.Convert(input, typeof(Color[]));

            Assert.IsInstanceOf<Color[]>(result);
            var array = (Color[])result;
            Assert.AreEqual(2, array.Length);
            Assert.AreEqual(Color.red, array[0]);
            Assert.AreEqual(Color.green, array[1]);
        }

        [Test]
        public void Convert_ListOfDictionaryToVector2List_Success()
        {
            var input = new List<object>
            {
                new Dictionary<string, object> { ["x"] = 1.0, ["y"] = 2.0 },
                new Dictionary<string, object> { ["x"] = 3.0, ["y"] = 4.0 }
            };
            var result = _converter.Convert(input, typeof(List<Vector2>));

            Assert.IsInstanceOf<List<Vector2>>(result);
            var list = (List<Vector2>)result;
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(new Vector2(1, 2), list[0]);
            Assert.AreEqual(new Vector2(3, 4), list[1]);
        }

        #endregion

        #region Null and Empty Tests

        [Test]
        public void Convert_Null_ReturnsEmptyArray()
        {
            var result = _converter.Convert(null, typeof(int[]));

            Assert.IsInstanceOf<int[]>(result);
            Assert.AreEqual(0, ((int[])result).Length);
        }

        [Test]
        public void Convert_Null_ReturnsEmptyList()
        {
            var result = _converter.Convert(null, typeof(List<string>));

            Assert.IsInstanceOf<List<string>>(result);
            Assert.AreEqual(0, ((List<string>)result).Count);
        }

        [Test]
        public void Convert_EmptyList_ReturnsEmptyArray()
        {
            var input = new List<object>();
            var result = _converter.Convert(input, typeof(int[]));

            Assert.IsInstanceOf<int[]>(result);
            Assert.AreEqual(0, ((int[])result).Length);
        }

        [Test]
        public void Convert_EmptyArray_ReturnsEmptyList()
        {
            var input = new object[0];
            var result = _converter.Convert(input, typeof(List<string>));

            Assert.IsInstanceOf<List<string>>(result);
            Assert.AreEqual(0, ((List<string>)result).Count);
        }

        #endregion

        #region Same Type Tests

        [Test]
        public void Convert_SameTypeArray_ReturnsSameValue()
        {
            var input = new int[] { 1, 2, 3 };
            var result = _converter.Convert(input, typeof(int[]));

            Assert.AreSame(input, result);
        }

        [Test]
        public void Convert_SameTypeList_ReturnsSameValue()
        {
            var input = new List<string> { "a", "b", "c" };
            var result = _converter.Convert(input, typeof(List<string>));

            Assert.AreSame(input, result);
        }

        #endregion

        #region Single Value Tests

        [Test]
        public void Convert_SingleValueToArray_WrapsInArray()
        {
            var result = _converter.Convert(42, typeof(int[]));

            Assert.IsInstanceOf<int[]>(result);
            var array = (int[])result;
            Assert.AreEqual(1, array.Length);
            Assert.AreEqual(42, array[0]);
        }

        [Test]
        public void Convert_SingleValueToList_WrapsInList()
        {
            var result = _converter.Convert("hello", typeof(List<string>));

            Assert.IsInstanceOf<List<string>>(result);
            var list = (List<string>)result;
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("hello", list[0]);
        }

        #endregion

        #region ValueConverterManager Integration Tests

        [Test]
        public void ValueConverterManager_Convert_ListOfObjectToIntArray_Success()
        {
            var manager = ValueConverterManager.Instance;
            var input = new List<object> { 1, 2, 3, 4, 5 };
            var result = manager.Convert(input, typeof(int[]));

            Assert.IsInstanceOf<int[]>(result);
            var array = (int[])result;
            Assert.AreEqual(5, array.Length);
        }

        [Test]
        public void ValueConverterManager_Convert_ListOfDictionaryToVector3Array_Success()
        {
            var manager = ValueConverterManager.Instance;
            var input = new List<object>
            {
                new Dictionary<string, object> { ["x"] = 1.0, ["y"] = 2.0, ["z"] = 3.0 },
                new Dictionary<string, object> { ["x"] = 4.0, ["y"] = 5.0, ["z"] = 6.0 }
            };
            var result = manager.Convert(input, typeof(Vector3[]));

            Assert.IsInstanceOf<Vector3[]>(result);
            var array = (Vector3[])result;
            Assert.AreEqual(2, array.Length);
            Assert.AreEqual(new Vector3(1, 2, 3), array[0]);
        }

        #endregion
    }
}
