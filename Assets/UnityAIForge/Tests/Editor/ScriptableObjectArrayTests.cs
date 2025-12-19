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

        #region User-Defined Type Array Tests

        [Test]
        public void Create_WithCustomStructArray_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestCustomStructArray.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["customStructArray"] = new List<object>
                    {
                        new Dictionary<string, object> { ["id"] = 1, ["name"] = "First", ["value"] = 10.5 },
                        new Dictionary<string, object> { ["id"] = 2, ["name"] = "Second", ["value"] = 20.5 },
                        new Dictionary<string, object> { ["id"] = 3, ["name"] = "Third", ["value"] = 30.5 }
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestScriptableObjectWithArrays>(assetPath);
            Assert.IsNotNull(asset);
            Assert.IsNotNull(asset.customStructArray);
            Assert.AreEqual(3, asset.customStructArray.Length);
            Assert.AreEqual(1, asset.customStructArray[0].id);
            Assert.AreEqual("First", asset.customStructArray[0].name);
            Assert.AreEqual(10.5f, asset.customStructArray[0].value, 0.001f);
            Assert.AreEqual(3, asset.customStructArray[2].id);
            Assert.AreEqual("Third", asset.customStructArray[2].name);
        }

        [Test]
        public void Create_WithCustomStructList_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestCustomStructList.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["customStructList"] = new List<object>
                    {
                        new Dictionary<string, object> { ["id"] = 100, ["name"] = "ListItem1", ["value"] = 1.1 },
                        new Dictionary<string, object> { ["id"] = 200, ["name"] = "ListItem2", ["value"] = 2.2 }
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestScriptableObjectWithArrays>(assetPath);
            Assert.IsNotNull(asset);
            Assert.IsNotNull(asset.customStructList);
            Assert.AreEqual(2, asset.customStructList.Count);
            Assert.AreEqual(100, asset.customStructList[0].id);
            Assert.AreEqual("ListItem1", asset.customStructList[0].name);
            Assert.AreEqual(200, asset.customStructList[1].id);
        }

        [Test]
        public void Create_WithEnumArray_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestEnumArray.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["enumArray"] = new List<object> { "Attack", "Defend", "Heal", "Attack" }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestScriptableObjectWithArrays>(assetPath);
            Assert.IsNotNull(asset);
            Assert.IsNotNull(asset.enumArray);
            Assert.AreEqual(4, asset.enumArray.Length);
            Assert.AreEqual(TestActionType.Attack, asset.enumArray[0]);
            Assert.AreEqual(TestActionType.Defend, asset.enumArray[1]);
            Assert.AreEqual(TestActionType.Heal, asset.enumArray[2]);
            Assert.AreEqual(TestActionType.Attack, asset.enumArray[3]);
        }

        [Test]
        public void Create_WithEnumList_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestEnumList.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["enumList"] = new List<object> { "Idle", "Running", "Jumping" }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestScriptableObjectWithArrays>(assetPath);
            Assert.IsNotNull(asset);
            Assert.IsNotNull(asset.enumList);
            Assert.AreEqual(3, asset.enumList.Count);
            Assert.AreEqual(TestCharacterState.Idle, asset.enumList[0]);
            Assert.AreEqual(TestCharacterState.Running, asset.enumList[1]);
            Assert.AreEqual(TestCharacterState.Jumping, asset.enumList[2]);
        }

        [Test]
        public void Update_CustomStructArray_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestUpdateCustomStructArray.asset";
            _createdAssets.Add(assetPath);

            // まずアセットを作成
            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["customStructArray"] = new List<object>
                    {
                        new Dictionary<string, object> { ["id"] = 1, ["name"] = "Old", ["value"] = 1.0 }
                    }
                }
            };
            _handler.Execute(createPayload);

            // 配列を更新
            var updatePayload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["customStructArray"] = new List<object>
                    {
                        new Dictionary<string, object> { ["id"] = 10, ["name"] = "NewItem1", ["value"] = 100.0 },
                        new Dictionary<string, object> { ["id"] = 20, ["name"] = "NewItem2", ["value"] = 200.0 }
                    }
                }
            };

            var result = _handler.Execute(updatePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestScriptableObjectWithArrays>(assetPath);
            Assert.AreEqual(2, asset.customStructArray.Length);
            Assert.AreEqual(10, asset.customStructArray[0].id);
            Assert.AreEqual("NewItem1", asset.customStructArray[0].name);
            Assert.AreEqual(100.0f, asset.customStructArray[0].value, 0.001f);
            Assert.AreEqual(20, asset.customStructArray[1].id);
        }

        [Test]
        public void Update_EnumArray_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestUpdateEnumArray.asset";
            _createdAssets.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["enumArray"] = new List<object> { "Attack" }
                }
            };
            _handler.Execute(createPayload);

            var updatePayload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["enumArray"] = new List<object> { "Defend", "Heal", "Special" }
                }
            };

            var result = _handler.Execute(updatePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestScriptableObjectWithArrays>(assetPath);
            Assert.AreEqual(3, asset.enumArray.Length);
            Assert.AreEqual(TestActionType.Defend, asset.enumArray[0]);
            Assert.AreEqual(TestActionType.Heal, asset.enumArray[1]);
            Assert.AreEqual(TestActionType.Special, asset.enumArray[2]);
        }

        [Test]
        public void Inspect_WithCustomStructArray_ReturnsStructDictionaries()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestInspectCustomStructArray.asset";
            _createdAssets.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["customStructArray"] = new List<object>
                    {
                        new Dictionary<string, object> { ["id"] = 999, ["name"] = "InspectTest", ["value"] = 99.9 }
                    }
                }
            };
            _handler.Execute(createPayload);

            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["assetPath"] = assetPath,
                ["includeProperties"] = true,
                ["propertyFilter"] = new List<object> { "customStructArray" }
            };

            var result = _handler.Execute(inspectPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var properties = result["properties"] as Dictionary<string, object>;
            Assert.IsNotNull(properties);
            Assert.IsTrue(properties.ContainsKey("customStructArray"));

            var structArrayValue = properties["customStructArray"] as List<object>;
            Assert.IsNotNull(structArrayValue);
            Assert.AreEqual(1, structArrayValue.Count);

            var structDict = structArrayValue[0] as Dictionary<string, object>;
            Assert.IsNotNull(structDict);
            Assert.AreEqual(999, Convert.ToInt32(structDict["id"]));
            Assert.AreEqual("InspectTest", structDict["name"]);
            Assert.AreEqual(99.9f, Convert.ToSingle(structDict["value"]), 0.001f);
        }

        [Test]
        public void Inspect_WithEnumArray_ReturnsEnumStrings()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestInspectEnumArray.asset";
            _createdAssets.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["enumArray"] = new List<object> { "Attack", "Special" }
                }
            };
            _handler.Execute(createPayload);

            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["assetPath"] = assetPath,
                ["includeProperties"] = true,
                ["propertyFilter"] = new List<object> { "enumArray" }
            };

            var result = _handler.Execute(inspectPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            var properties = result["properties"] as Dictionary<string, object>;
            var enumArrayValue = properties["enumArray"] as List<object>;

            Assert.IsNotNull(enumArrayValue);
            Assert.AreEqual(2, enumArrayValue.Count);
            Assert.AreEqual("Attack", enumArrayValue[0].ToString());
            Assert.AreEqual("Special", enumArrayValue[1].ToString());
        }

        [Test]
        public void Create_WithNestedStructArray_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestNestedStructArray.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = "MCP.Editor.Tests.TestScriptableObjectWithArrays",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["nestedStructArray"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["label"] = "Player",
                            ["position"] = new Dictionary<string, object> { ["x"] = 1.0, ["y"] = 2.0, ["z"] = 3.0 },
                            ["color"] = new Dictionary<string, object> { ["r"] = 1.0, ["g"] = 0.0, ["b"] = 0.0, ["a"] = 1.0 }
                        },
                        new Dictionary<string, object>
                        {
                            ["label"] = "Enemy",
                            ["position"] = new Dictionary<string, object> { ["x"] = 10.0, ["y"] = 0.0, ["z"] = 5.0 },
                            ["color"] = new Dictionary<string, object> { ["r"] = 0.0, ["g"] = 0.0, ["b"] = 1.0, ["a"] = 1.0 }
                        }
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestScriptableObjectWithArrays>(assetPath);
            Assert.IsNotNull(asset);
            Assert.IsNotNull(asset.nestedStructArray);
            Assert.AreEqual(2, asset.nestedStructArray.Length);

            Assert.AreEqual("Player", asset.nestedStructArray[0].label);
            Assert.AreEqual(new Vector3(1, 2, 3), asset.nestedStructArray[0].position);
            Assert.AreEqual(Color.red, asset.nestedStructArray[0].color);

            Assert.AreEqual("Enemy", asset.nestedStructArray[1].label);
            Assert.AreEqual(new Vector3(10, 0, 5), asset.nestedStructArray[1].position);
            Assert.AreEqual(Color.blue, asset.nestedStructArray[1].color);
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

        // ユーザー定義構造体の配列とList
        public TestCustomStruct[] customStructArray;
        public List<TestCustomStruct> customStructList;

        // ネストした構造体の配列
        public TestNestedStruct[] nestedStructArray;

        // Enum配列とList
        public TestActionType[] enumArray;
        public List<TestCharacterState> enumList;
    }

    /// <summary>
    /// テスト用のカスタム構造体
    /// </summary>
    [Serializable]
    public struct TestCustomStruct
    {
        public int id;
        public string name;
        public float value;
    }

    /// <summary>
    /// テスト用のネストした構造体（Unity型を含む）
    /// </summary>
    [Serializable]
    public struct TestNestedStruct
    {
        public string label;
        public Vector3 position;
        public Color color;
    }

    /// <summary>
    /// テスト用のアクションタイプenum
    /// </summary>
    public enum TestActionType
    {
        Attack,
        Defend,
        Heal,
        Special
    }

    /// <summary>
    /// テスト用のキャラクター状態enum
    /// </summary>
    public enum TestCharacterState
    {
        Idle,
        Running,
        Jumping,
        Falling,
        Dead
    }
}
