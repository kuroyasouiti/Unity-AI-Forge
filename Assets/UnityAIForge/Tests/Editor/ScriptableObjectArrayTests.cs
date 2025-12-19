using System;
using System.Collections.Generic;
using System.IO;
using MCP.Editor.Base;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// ScriptableObjectの配列変数操作に関するテストクラス。
    /// </summary>
    [TestFixture]
    public class ScriptableObjectArrayTests
    {
        private ScriptableObjectCommandHandler _handler;
        private List<string> _createdAssets;

        [SetUp]
        public void SetUp()
        {
            _handler = new ScriptableObjectCommandHandler();
            _createdAssets = new List<string>();
            ValueConverterManager.ResetInstance();
            TestUtilities.CreateTestDirectory();
        }

        [TearDown]
        public void TearDown()
        {
            // 作成したアセットをクリーンアップ
            foreach (var path in _createdAssets)
            {
                if (File.Exists(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
            _createdAssets.Clear();
            TestUtilities.CleanupTestDirectory();
            ValueConverterManager.ResetInstance();
        }

        #region Create with Array Properties Tests

        [Test]
        public void Create_WithIntArray_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestIntArray.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["intArray"] = new List<object> { 1, 2, 3, 4, 5 }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            // 作成されたアセットを検証
            var asset = AssetDatabase.LoadAssetAtPath<TestScriptableObjectWithArrays>(assetPath);
            Assert.IsNotNull(asset);
            Assert.IsNotNull(asset.intArray);
            Assert.AreEqual(5, asset.intArray.Length);
            Assert.AreEqual(1, asset.intArray[0]);
            Assert.AreEqual(5, asset.intArray[4]);
        }

        [Test]
        public void Create_WithStringList_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestStringList.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["stringList"] = new List<object> { "apple", "banana", "cherry" }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestScriptableObjectWithArrays>(assetPath);
            Assert.IsNotNull(asset);
            Assert.IsNotNull(asset.stringList);
            Assert.AreEqual(3, asset.stringList.Count);
            Assert.AreEqual("apple", asset.stringList[0]);
            Assert.AreEqual("cherry", asset.stringList[2]);
        }

        [Test]
        public void Create_WithFloatArray_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestFloatArray.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["floatArray"] = new List<object> { 1.5, 2.5, 3.5 }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestScriptableObjectWithArrays>(assetPath);
            Assert.IsNotNull(asset);
            Assert.IsNotNull(asset.floatArray);
            Assert.AreEqual(3, asset.floatArray.Length);
            Assert.AreEqual(1.5f, asset.floatArray[0], 0.001f);
            Assert.AreEqual(3.5f, asset.floatArray[2], 0.001f);
        }

        [Test]
        public void Create_WithVector3Array_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestVector3Array.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["vector3Array"] = new List<object>
                    {
                        new Dictionary<string, object> { ["x"] = 1.0, ["y"] = 2.0, ["z"] = 3.0 },
                        new Dictionary<string, object> { ["x"] = 4.0, ["y"] = 5.0, ["z"] = 6.0 }
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestScriptableObjectWithArrays>(assetPath);
            Assert.IsNotNull(asset);
            Assert.IsNotNull(asset.vector3Array);
            Assert.AreEqual(2, asset.vector3Array.Length);
            Assert.AreEqual(new Vector3(1, 2, 3), asset.vector3Array[0]);
            Assert.AreEqual(new Vector3(4, 5, 6), asset.vector3Array[1]);
        }

        [Test]
        public void Create_WithColorList_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestColorList.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["colorList"] = new List<object>
                    {
                        new Dictionary<string, object> { ["r"] = 1.0, ["g"] = 0.0, ["b"] = 0.0, ["a"] = 1.0 },
                        new Dictionary<string, object> { ["r"] = 0.0, ["g"] = 1.0, ["b"] = 0.0, ["a"] = 1.0 }
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestScriptableObjectWithArrays>(assetPath);
            Assert.IsNotNull(asset);
            Assert.IsNotNull(asset.colorList);
            Assert.AreEqual(2, asset.colorList.Count);
            Assert.AreEqual(Color.red, asset.colorList[0]);
            Assert.AreEqual(Color.green, asset.colorList[1]);
        }

        #endregion

        #region Update Array Properties Tests

        [Test]
        public void Update_IntArray_Success()
        {
            // まずアセットを作成
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestUpdateIntArray.asset";
            _createdAssets.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath
            };
            _handler.Execute(createPayload);

            // 配列を更新
            var updatePayload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["intArray"] = new List<object> { 10, 20, 30 }
                }
            };

            var result = _handler.Execute(updatePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            // 更新を検証
            var asset = AssetDatabase.LoadAssetAtPath<TestScriptableObjectWithArrays>(assetPath);
            Assert.IsNotNull(asset.intArray);
            Assert.AreEqual(3, asset.intArray.Length);
            Assert.AreEqual(10, asset.intArray[0]);
            Assert.AreEqual(30, asset.intArray[2]);
        }

        [Test]
        public void Update_StringList_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestUpdateStringList.asset";
            _createdAssets.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["stringList"] = new List<object> { "old1", "old2" }
                }
            };
            _handler.Execute(createPayload);

            var updatePayload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["stringList"] = new List<object> { "new1", "new2", "new3" }
                }
            };

            var result = _handler.Execute(updatePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestScriptableObjectWithArrays>(assetPath);
            Assert.AreEqual(3, asset.stringList.Count);
            Assert.AreEqual("new1", asset.stringList[0]);
            Assert.AreEqual("new3", asset.stringList[2]);
        }

        #endregion

        #region Inspect Array Properties Tests

        [Test]
        public void Inspect_WithIntArray_ReturnsArrayValues()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestInspectIntArray.asset";
            _createdAssets.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["intArray"] = new List<object> { 100, 200, 300 }
                }
            };
            _handler.Execute(createPayload);

            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["assetPath"] = assetPath,
                ["includeProperties"] = true,
                ["propertyFilter"] = new List<object> { "intArray" }
            };

            var result = _handler.Execute(inspectPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var properties = result["properties"] as Dictionary<string, object>;
            Assert.IsNotNull(properties);
            Assert.IsTrue(properties.ContainsKey("intArray"));

            var intArrayValue = properties["intArray"] as List<object>;
            Assert.IsNotNull(intArrayValue);
            Assert.AreEqual(3, intArrayValue.Count);
            Assert.AreEqual(100, Convert.ToInt32(intArrayValue[0]));
            Assert.AreEqual(300, Convert.ToInt32(intArrayValue[2]));
        }

        [Test]
        public void Inspect_WithStringList_ReturnsListValues()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestInspectStringList.asset";
            _createdAssets.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["stringList"] = new List<object> { "foo", "bar", "baz" }
                }
            };
            _handler.Execute(createPayload);

            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["assetPath"] = assetPath,
                ["includeProperties"] = true,
                ["propertyFilter"] = new List<object> { "stringList" }
            };

            var result = _handler.Execute(inspectPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            var properties = result["properties"] as Dictionary<string, object>;
            var stringListValue = properties["stringList"] as List<object>;

            Assert.IsNotNull(stringListValue);
            Assert.AreEqual(3, stringListValue.Count);
            Assert.AreEqual("foo", stringListValue[0]);
            Assert.AreEqual("baz", stringListValue[2]);
        }

        [Test]
        public void Inspect_WithVector3Array_ReturnsVectorDictionaries()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestInspectVector3Array.asset";
            _createdAssets.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["vector3Array"] = new List<object>
                    {
                        new Dictionary<string, object> { ["x"] = 1.0, ["y"] = 2.0, ["z"] = 3.0 }
                    }
                }
            };
            _handler.Execute(createPayload);

            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["assetPath"] = assetPath,
                ["includeProperties"] = true,
                ["propertyFilter"] = new List<object> { "vector3Array" }
            };

            var result = _handler.Execute(inspectPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            var properties = result["properties"] as Dictionary<string, object>;
            var vectorArrayValue = properties["vector3Array"] as List<object>;

            Assert.IsNotNull(vectorArrayValue);
            Assert.AreEqual(1, vectorArrayValue.Count);

            var vectorDict = vectorArrayValue[0] as Dictionary<string, object>;
            Assert.IsNotNull(vectorDict);
            Assert.AreEqual(1f, Convert.ToSingle(vectorDict["x"]), 0.001f);
            Assert.AreEqual(2f, Convert.ToSingle(vectorDict["y"]), 0.001f);
            Assert.AreEqual(3f, Convert.ToSingle(vectorDict["z"]), 0.001f);
        }

        #endregion

        #region Empty Array Tests

        [Test]
        public void Create_WithEmptyArray_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestEmptyArray.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["intArray"] = new List<object>()
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestScriptableObjectWithArrays>(assetPath);
            Assert.IsNotNull(asset.intArray);
            Assert.AreEqual(0, asset.intArray.Length);
        }

        [Test]
        public void Update_ToEmptyArray_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestUpdateToEmptyArray.asset";
            _createdAssets.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["intArray"] = new List<object> { 1, 2, 3 }
                }
            };
            _handler.Execute(createPayload);

            var updatePayload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["intArray"] = new List<object>()
                }
            };

            var result = _handler.Execute(updatePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestScriptableObjectWithArrays>(assetPath);
            Assert.AreEqual(0, asset.intArray.Length);
        }

        #endregion

        #region Mixed Properties Tests

        [Test]
        public void Create_WithMixedProperties_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestMixedProperties.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["singleInt"] = 42,
                    ["singleString"] = "hello",
                    ["intArray"] = new List<object> { 1, 2, 3 },
                    ["stringList"] = new List<object> { "a", "b", "c" }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestScriptableObjectWithArrays>(assetPath);
            Assert.AreEqual(42, asset.singleInt);
            Assert.AreEqual("hello", asset.singleString);
            Assert.AreEqual(3, asset.intArray.Length);
            Assert.AreEqual(3, asset.stringList.Count);
        }

        #endregion
    }

    /// <summary>
    /// テスト用のScriptableObject（配列とList型のプロパティを含む）
    /// </summary>
    public class TestScriptableObjectWithArrays : ScriptableObject
    {
        // 単一値フィールド
        public int singleInt;
        public string singleString;

        // 配列フィールド
        public int[] intArray;
        public float[] floatArray;
        public string[] stringArray;
        public Vector3[] vector3Array;

        // Listフィールド
        public List<int> intList;
        public List<string> stringList;
        public List<float> floatList;
        public List<Color> colorList;
        public List<Vector2> vector2List;
    }
}
