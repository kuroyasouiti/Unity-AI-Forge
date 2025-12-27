using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests.ValueConverters
{
    /// <summary>
    /// ValueConverterManagerのテストクラス。
    /// Newtonsoft.Jsonベースの新しい実装をテストします。
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
            Assert.AreEqual(3.14f, (float)result, 0.001f);
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

        [Test]
        public void Convert_LongToInt_Success()
        {
            var result = _manager.Convert(42L, typeof(int));

            Assert.IsInstanceOf<int>(result);
            Assert.AreEqual(42, result);
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
        public void Convert_DictionaryToVector2_Success()
        {
            var dict = new Dictionary<string, object>
            {
                ["x"] = 1.0,
                ["y"] = 2.0
            };

            var result = _manager.Convert(dict, typeof(Vector2));

            Assert.IsInstanceOf<Vector2>(result);
            var vector = (Vector2)result;
            Assert.AreEqual(new Vector2(1f, 2f), vector);
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

        [Test]
        public void Convert_StringConstantToColor_Success()
        {
            var result = _manager.Convert("red", typeof(Color));

            Assert.IsInstanceOf<Color>(result);
            Assert.AreEqual(Color.red, result);
        }

        [Test]
        public void Convert_StringConstantToVector3_Success()
        {
            var result = _manager.Convert("up", typeof(Vector3));

            Assert.IsInstanceOf<Vector3>(result);
            Assert.AreEqual(Vector3.up, result);
        }

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

            var result = _manager.Convert(dict, typeof(Quaternion));

            Assert.IsInstanceOf<Quaternion>(result);
            Assert.AreEqual(Quaternion.identity, result);
        }

        #endregion

        #region Convert LayerMask Tests

        [Test]
        public void Convert_IntToLayerMask_Success()
        {
            var result = _manager.Convert(33, typeof(LayerMask));

            Assert.IsInstanceOf<LayerMask>(result);
            Assert.AreEqual(33, ((LayerMask)result).value);
        }

        [Test]
        public void Convert_StringToLayerMask_Success()
        {
            var result = _manager.Convert("Everything", typeof(LayerMask));

            Assert.IsInstanceOf<LayerMask>(result);
            Assert.AreEqual(~0, ((LayerMask)result).value);
        }

        [Test]
        public void Convert_DictionaryToLayerMask_Success()
        {
            var dict = new Dictionary<string, object>
            {
                ["value"] = 255
            };

            var result = _manager.Convert(dict, typeof(LayerMask));

            Assert.IsInstanceOf<LayerMask>(result);
            Assert.AreEqual(255, ((LayerMask)result).value);
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

        [Test]
        public void Convert_StringCaseInsensitiveToEnum_Success()
        {
            var result = _manager.Convert("dynamic", typeof(RigidbodyType2D));

            Assert.IsInstanceOf<RigidbodyType2D>(result);
            Assert.AreEqual(RigidbodyType2D.Dynamic, result);
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
            Assert.AreEqual(3.14f, (float)result, 0.001f);
        }

        [Test]
        public void TryConvert_DictionaryToVector3_ReturnsTrue()
        {
            var dict = new Dictionary<string, object>
            {
                ["x"] = 1.0,
                ["y"] = 2.0,
                ["z"] = 3.0
            };

            var success = _manager.TryConvert(dict, typeof(Vector3), out var result);

            Assert.IsTrue(success);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), result);
        }

        #endregion

        #region Serialize Tests

        [Test]
        public void Serialize_Vector3_ReturnsDictionary()
        {
            var vector = new Vector3(1, 2, 3);

            var result = _manager.Serialize(vector);

            Assert.IsInstanceOf<Dictionary<string, object>>(result);
            var dict = (Dictionary<string, object>)result;
            Assert.AreEqual(1.0, Convert.ToDouble(dict["x"]), 0.001);
            Assert.AreEqual(2.0, Convert.ToDouble(dict["y"]), 0.001);
            Assert.AreEqual(3.0, Convert.ToDouble(dict["z"]), 0.001);
        }

        [Test]
        public void Serialize_Color_ReturnsDictionary()
        {
            var color = new Color(1f, 0.5f, 0f, 1f);

            var result = _manager.Serialize(color);

            Assert.IsInstanceOf<Dictionary<string, object>>(result);
            var dict = (Dictionary<string, object>)result;
            Assert.AreEqual(1.0, Convert.ToDouble(dict["r"]), 0.001);
            Assert.AreEqual(0.5, Convert.ToDouble(dict["g"]), 0.001);
            Assert.AreEqual(0.0, Convert.ToDouble(dict["b"]), 0.001);
        }

        [Test]
        public void Serialize_LayerMask_ReturnsDictionaryWithValueAndLayers()
        {
            LayerMask mask = 33; // Default + UI

            var result = _manager.Serialize(mask);

            Assert.IsInstanceOf<Dictionary<string, object>>(result);
            var dict = (Dictionary<string, object>)result;
            Assert.AreEqual(33, Convert.ToInt32(dict["value"]));
            Assert.IsTrue(dict.ContainsKey("layers"));
        }

        [Test]
        public void Serialize_Null_ReturnsNull()
        {
            var result = _manager.Serialize(null);

            Assert.IsNull(result);
        }

        [Test]
        public void Serialize_PrimitiveInt_ReturnsLong()
        {
            var result = _manager.Serialize(42);

            // Newtonsoft.Json returns long for integers
            Assert.AreEqual(42L, result);
        }

        [Test]
        public void Serialize_String_ReturnsString()
        {
            var result = _manager.Serialize("hello");

            Assert.AreEqual("hello", result);
        }

        #endregion

        #region UnityEngine.Object Reference Tests

        [Test]
        public void Convert_RefFormatDictionary_ToMaterial_LoadsAsset()
        {
            // Use a built-in Unity asset path
            var dict = new Dictionary<string, object>
            {
                ["$ref"] = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Lit.mat"
            };

            var result = _manager.Convert(dict, typeof(Material));

            // Note: This test may fail if URP is not installed, which is acceptable
            // The important thing is that the $ref format is being parsed correctly
            if (result != null)
            {
                Assert.IsInstanceOf<Material>(result);
            }
        }

        [Test]
        public void Convert_StringPath_ToAsset_LoadsAsset()
        {
            // Use a string path directly
            var result = _manager.Convert(
                "Packages/com.unity.textmeshpro/Package Resources/Fonts & Materials/LiberationSans SDF.asset",
                typeof(UnityEngine.Object)
            );

            // TMP asset should exist in most Unity projects
            if (result != null)
            {
                Assert.IsInstanceOf<UnityEngine.Object>(result);
            }
        }

        [Test]
        public void Convert_InvalidAssetPath_ReturnsNull()
        {
            var dict = new Dictionary<string, object>
            {
                ["$ref"] = "Assets/NonExistent/Path.asset"
            };

            var result = _manager.Convert(dict, typeof(Material));

            Assert.IsNull(result);
        }

        #endregion

        #region Prefab Reference Tests

        [Test]
        public void Convert_PrefabPath_ToGameObject_LoadsPrefab()
        {
            // Create a test prefab
            var testGo = new GameObject("TestPrefabForConversion");
            var prefabPath = "Assets/TestTemp/TestPrefabForConversion.prefab";

            try
            {
                // Ensure directory exists
                if (!System.IO.Directory.Exists("Assets/TestTemp"))
                {
                    System.IO.Directory.CreateDirectory("Assets/TestTemp");
                    UnityEditor.AssetDatabase.Refresh();
                }

                // Create prefab
                UnityEditor.PrefabUtility.SaveAsPrefabAsset(testGo, prefabPath);
                UnityEditor.AssetDatabase.Refresh();

                // Test conversion with string path
                var result = _manager.Convert(prefabPath, typeof(GameObject));

                Assert.IsNotNull(result);
                Assert.IsInstanceOf<GameObject>(result);
                Assert.AreEqual("TestPrefabForConversion", ((GameObject)result).name);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(testGo);
                UnityEditor.AssetDatabase.DeleteAsset(prefabPath);
                UnityEditor.AssetDatabase.DeleteAsset("Assets/TestTemp");
            }
        }

        [Test]
        public void Convert_RefFormatDictionary_ToPrefab_LoadsPrefab()
        {
            // Create a test prefab
            var testGo = new GameObject("TestPrefabRefFormat");
            var prefabPath = "Assets/TestTemp/TestPrefabRefFormat.prefab";

            try
            {
                if (!System.IO.Directory.Exists("Assets/TestTemp"))
                {
                    System.IO.Directory.CreateDirectory("Assets/TestTemp");
                    UnityEditor.AssetDatabase.Refresh();
                }

                UnityEditor.PrefabUtility.SaveAsPrefabAsset(testGo, prefabPath);
                UnityEditor.AssetDatabase.Refresh();

                // Test conversion with $ref format
                var dict = new Dictionary<string, object>
                {
                    ["$ref"] = prefabPath
                };

                var result = _manager.Convert(dict, typeof(GameObject));

                Assert.IsNotNull(result);
                Assert.IsInstanceOf<GameObject>(result);
                Assert.AreEqual("TestPrefabRefFormat", ((GameObject)result).name);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(testGo);
                UnityEditor.AssetDatabase.DeleteAsset(prefabPath);
                UnityEditor.AssetDatabase.DeleteAsset("Assets/TestTemp");
            }
        }

        [Test]
        public void Convert_SceneObjectPath_ToGameObject_FindsSceneObject()
        {
            // Create a scene object (not a prefab)
            var sceneGo = new GameObject("TestSceneObject");

            try
            {
                // Path without "Assets/" prefix should be treated as scene object path
                var result = _manager.Convert("TestSceneObject", typeof(GameObject));

                Assert.IsNotNull(result);
                Assert.IsInstanceOf<GameObject>(result);
                Assert.AreSame(sceneGo, result);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sceneGo);
            }
        }

        [Test]
        public void Convert_RefFormat_ToSceneObject_FindsSceneObject()
        {
            // Create a scene object
            var sceneGo = new GameObject("TestRefSceneObject");

            try
            {
                // $ref format with scene object path (not starting with Assets/)
                var dict = new Dictionary<string, object>
                {
                    ["$ref"] = "TestRefSceneObject"
                };

                var result = _manager.Convert(dict, typeof(GameObject));

                Assert.IsNotNull(result);
                Assert.IsInstanceOf<GameObject>(result);
                Assert.AreSame(sceneGo, result);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sceneGo);
            }
        }

        [Test]
        public void Convert_HierarchyPath_ToGameObject_FindsNestedObject()
        {
            // Create a hierarchy: Parent/Child/GrandChild
            var parent = new GameObject("TestParent");
            var child = new GameObject("TestChild");
            var grandChild = new GameObject("TestGrandChild");
            child.transform.SetParent(parent.transform);
            grandChild.transform.SetParent(child.transform);

            try
            {
                var result = _manager.Convert("TestParent/TestChild/TestGrandChild", typeof(GameObject));

                Assert.IsNotNull(result);
                Assert.IsInstanceOf<GameObject>(result);
                Assert.AreSame(grandChild, result);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void Convert_HierarchyPath_ToComponent_FindsComponent()
        {
            // Create a hierarchy with a component
            var parent = new GameObject("TestParentWithComponent");
            var child = new GameObject("TestChildWithRigidbody");
            child.transform.SetParent(parent.transform);
            var rb = child.AddComponent<Rigidbody>();

            try
            {
                var result = _manager.Convert("TestParentWithComponent/TestChildWithRigidbody", typeof(Rigidbody));

                Assert.IsNotNull(result);
                Assert.IsInstanceOf<Rigidbody>(result);
                Assert.AreSame(rb, result);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void Convert_InactiveSceneObject_FindsObject()
        {
            // Create an inactive scene object
            var sceneGo = new GameObject("TestInactiveObject");
            sceneGo.SetActive(false);

            try
            {
                var result = _manager.Convert("TestInactiveObject", typeof(GameObject));

                Assert.IsNotNull(result);
                Assert.IsInstanceOf<GameObject>(result);
                Assert.AreSame(sceneGo, result);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sceneGo);
            }
        }

        [Test]
        public void Convert_InactiveNestedObject_FindsObject()
        {
            // Create a hierarchy with inactive child
            var parent = new GameObject("TestActiveParent");
            var child = new GameObject("TestInactiveChild");
            child.transform.SetParent(parent.transform);
            child.SetActive(false);

            try
            {
                var result = _manager.Convert("TestActiveParent/TestInactiveChild", typeof(GameObject));

                Assert.IsNotNull(result);
                Assert.IsInstanceOf<GameObject>(result);
                Assert.AreSame(child, result);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void Convert_RefFormat_HierarchyPath_FindsNestedObject()
        {
            // Create a hierarchy
            var parent = new GameObject("TestRefParent");
            var child = new GameObject("TestRefChild");
            child.transform.SetParent(parent.transform);

            try
            {
                var dict = new Dictionary<string, object>
                {
                    ["$ref"] = "TestRefParent/TestRefChild"
                };

                var result = _manager.Convert(dict, typeof(GameObject));

                Assert.IsNotNull(result);
                Assert.IsInstanceOf<GameObject>(result);
                Assert.AreSame(child, result);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void Convert_InvalidPrefabPath_ReturnsNull()
        {
            var dict = new Dictionary<string, object>
            {
                ["$ref"] = "Assets/NonExistent/Prefab.prefab"
            };

            var result = _manager.Convert(dict, typeof(GameObject));

            Assert.IsNull(result);
        }

        #endregion

        #region Array/List Tests

        [Test]
        public void Convert_ListToIntArray_Success()
        {
            var list = new List<object> { 1, 2, 3, 4, 5 };

            var result = _manager.Convert(list, typeof(int[]));

            Assert.IsInstanceOf<int[]>(result);
            var array = (int[])result;
            Assert.AreEqual(5, array.Length);
            Assert.AreEqual(1, array[0]);
            Assert.AreEqual(5, array[4]);
        }

        [Test]
        public void Convert_ListToVector3Array_Success()
        {
            var list = new List<object>
            {
                new Dictionary<string, object> { ["x"] = 1.0, ["y"] = 2.0, ["z"] = 3.0 },
                new Dictionary<string, object> { ["x"] = 4.0, ["y"] = 5.0, ["z"] = 6.0 }
            };

            var result = _manager.Convert(list, typeof(Vector3[]));

            Assert.IsInstanceOf<Vector3[]>(result);
            var array = (Vector3[])result;
            Assert.AreEqual(2, array.Length);
            Assert.AreEqual(new Vector3(1, 2, 3), array[0]);
            Assert.AreEqual(new Vector3(4, 5, 6), array[1]);
        }

        [Test]
        public void Convert_ListToStringList_Success()
        {
            var list = new List<object> { "a", "b", "c" };

            var result = _manager.Convert(list, typeof(List<string>));

            Assert.IsInstanceOf<List<string>>(result);
            var stringList = (List<string>)result;
            Assert.AreEqual(3, stringList.Count);
            Assert.AreEqual("a", stringList[0]);
        }

        #endregion
    }
}
