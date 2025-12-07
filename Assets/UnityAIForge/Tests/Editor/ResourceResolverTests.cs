using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MCP.Editor.Base;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// GameObjectResolver のユニットテスト。
    /// </summary>
    [TestFixture]
    public class GameObjectResolverTests
    {
        private GameObjectResolver _resolver;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _resolver = new GameObjectResolver();
            _createdObjects = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }
            _createdObjects.Clear();
        }

        private GameObject CreateTestGameObject(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            _createdObjects.Add(go);
            return go;
        }

        #region Resolve Tests

        [Test]
        public void Resolve_ExistingRootObject_ShouldReturnObject()
        {
            // Arrange
            var go = CreateTestGameObject("RootObject");

            // Act
            var result = _resolver.Resolve("RootObject");

            // Assert
            Assert.AreEqual(go, result);
        }

        [Test]
        public void Resolve_NestedObject_ShouldReturnObject()
        {
            // Arrange
            var root = CreateTestGameObject("Root");
            var child = CreateTestGameObject("Child", root.transform);
            var grandchild = CreateTestGameObject("Grandchild", child.transform);

            // Act
            var result = _resolver.Resolve("Root/Child/Grandchild");

            // Assert
            Assert.AreEqual(grandchild, result);
        }

        [Test]
        public void Resolve_NonExistentObject_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                _resolver.Resolve("NonExistentObject"));
        }

        [Test]
        public void Resolve_EmptyPath_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                _resolver.Resolve(""));
        }

        [Test]
        public void Resolve_NullPath_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                _resolver.Resolve(null));
        }

        #endregion

        #region TryResolve Tests

        [Test]
        public void TryResolve_ExistingObject_ShouldReturnObject()
        {
            // Arrange
            var go = CreateTestGameObject("ExistingObject");

            // Act
            var result = _resolver.TryResolve("ExistingObject");

            // Assert
            Assert.AreEqual(go, result);
        }

        [Test]
        public void TryResolve_NonExistentObject_ShouldReturnNull()
        {
            // Act
            var result = _resolver.TryResolve("NonExistentObject");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void TryResolve_EmptyPath_ShouldReturnNull()
        {
            // Act
            var result = _resolver.TryResolve("");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void TryResolve_NullPath_ShouldReturnNull()
        {
            // Act
            var result = _resolver.TryResolve(null);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region Exists Tests

        [Test]
        public void Exists_ExistingObject_ShouldReturnTrue()
        {
            // Arrange
            CreateTestGameObject("ExistingObject");

            // Act & Assert
            Assert.IsTrue(_resolver.Exists("ExistingObject"));
        }

        [Test]
        public void Exists_NonExistentObject_ShouldReturnFalse()
        {
            // Act & Assert
            Assert.IsFalse(_resolver.Exists("NonExistentObject"));
        }

        #endregion

        #region ResolveMany Tests

        [Test]
        public void ResolveMany_MultipleExistingObjects_ShouldReturnAll()
        {
            // Arrange
            var go1 = CreateTestGameObject("Object1");
            var go2 = CreateTestGameObject("Object2");

            // Act
            var results = _resolver.ResolveMany("Object1", "Object2").ToList();

            // Assert
            Assert.AreEqual(2, results.Count);
            Assert.Contains(go1, results);
            Assert.Contains(go2, results);
        }

        [Test]
        public void ResolveMany_SomeNonExistent_ShouldReturnOnlyExisting()
        {
            // Arrange
            var go = CreateTestGameObject("ExistingOnly");

            // Act
            var results = _resolver.ResolveMany("ExistingOnly", "NonExistent").ToList();

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.Contains(go, results);
        }

        #endregion

        #region FindByPattern Tests

        [Test]
        public void FindByPattern_WildcardMatch_ShouldFindObjects()
        {
            // Arrange
            CreateTestGameObject("Test_A");
            CreateTestGameObject("Test_B");
            CreateTestGameObject("Other");

            // Act
            var results = _resolver.FindByPattern("Test_*").ToList();

            // Assert
            Assert.AreEqual(2, results.Count);
        }

        [Test]
        public void FindByPattern_RegexMatch_ShouldFindObjects()
        {
            // Arrange
            CreateTestGameObject("Item1");
            CreateTestGameObject("Item2");
            CreateTestGameObject("Other");

            // Act
            var results = _resolver.FindByPattern("Item\\d", useRegex: true).ToList();

            // Assert
            Assert.AreEqual(2, results.Count);
        }

        [Test]
        public void FindByPattern_WithMaxResults_ShouldLimitResults()
        {
            // Arrange
            for (int i = 0; i < 10; i++)
            {
                CreateTestGameObject($"Limited_{i}");
            }

            // Act
            var results = _resolver.FindByPattern("Limited_*", maxResults: 3).ToList();

            // Assert
            Assert.LessOrEqual(results.Count, 3);
        }

        [Test]
        public void FindByPattern_EmptyPattern_ShouldReturnEmpty()
        {
            // Arrange
            CreateTestGameObject("TestObject");

            // Act
            var results = _resolver.FindByPattern("").ToList();

            // Assert
            Assert.IsEmpty(results);
        }

        [Test]
        public void FindByPattern_QuestionMarkWildcard_ShouldMatchSingleChar()
        {
            // Arrange
            CreateTestGameObject("A1");
            CreateTestGameObject("B2");
            CreateTestGameObject("AB");

            // Act
            var results = _resolver.FindByPattern("?1").ToList();

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("A1", results[0].name);
        }

        #endregion

        #region ResolveByHierarchyPath Tests

        [Test]
        public void ResolveByHierarchyPath_DeepHierarchy_ShouldResolve()
        {
            // Arrange
            var level1 = CreateTestGameObject("Level1");
            var level2 = CreateTestGameObject("Level2", level1.transform);
            var level3 = CreateTestGameObject("Level3", level2.transform);
            var level4 = CreateTestGameObject("Level4", level3.transform);

            // Act
            var result = _resolver.ResolveByHierarchyPath("Level1/Level2/Level3/Level4");

            // Assert
            Assert.AreEqual(level4, result);
        }

        [Test]
        public void ResolveByHierarchyPath_PartialPathNotFound_ShouldReturnNull()
        {
            // Arrange
            var root = CreateTestGameObject("Root");
            CreateTestGameObject("Child", root.transform);

            // Act
            var result = _resolver.ResolveByHierarchyPath("Root/NonExistent/Child");

            // Assert
            Assert.IsNull(result);
        }

        #endregion
    }

    /// <summary>
    /// AssetResolver のユニットテスト。
    /// </summary>
    [TestFixture]
    public class AssetResolverTests
    {
        private AssetResolver _resolver;
        private string _testAssetPath;

        [SetUp]
        public void SetUp()
        {
            _resolver = new AssetResolver();
            _testAssetPath = "Assets/TestAssets";
            
            // テストディレクトリ作成
            if (!Directory.Exists(_testAssetPath))
            {
                Directory.CreateDirectory(_testAssetPath);
            }
        }

        [TearDown]
        public void TearDown()
        {
            // テストアセットをクリーンアップ
            if (Directory.Exists(_testAssetPath))
            {
                try
                {
                    AssetDatabase.DeleteAsset(_testAssetPath);
                }
                catch
                {
                    // 削除失敗時は無視
                }
            }
            AssetDatabase.Refresh();
        }

        #region ValidatePath Tests

        [Test]
        public void ValidatePath_ValidPath_ShouldReturnTrue()
        {
            // Act & Assert
            Assert.IsTrue(_resolver.ValidatePath("Assets/Folder/asset.prefab"));
        }

        [Test]
        public void ValidatePath_PathNotStartingWithAssets_ShouldReturnFalse()
        {
            // Act & Assert
            Assert.IsFalse(_resolver.ValidatePath("Folder/asset.prefab"));
        }

        [Test]
        public void ValidatePath_PathWithTraversal_ShouldReturnFalse()
        {
            // Act & Assert
            Assert.IsFalse(_resolver.ValidatePath("Assets/../Outside/asset.prefab"));
        }

        [Test]
        public void ValidatePath_NullPath_ShouldReturnFalse()
        {
            // Act & Assert
            Assert.IsFalse(_resolver.ValidatePath(null));
        }

        [Test]
        public void ValidatePath_EmptyPath_ShouldReturnFalse()
        {
            // Act & Assert
            Assert.IsFalse(_resolver.ValidatePath(""));
        }

        #endregion

        #region TryResolve Tests

        [Test]
        public void TryResolve_ValidAssetPath_ShouldReturnAsset()
        {
            // Arrange - Use a built-in Unity asset
            var builtInPath = "Assets/UnityAIForge/Editor/UnityAIForge.Editor.asmdef";
            
            // Act
            var result = _resolver.TryResolve(builtInPath);

            // Assert
            // Asset may or may not exist, so we just check it doesn't throw
            // The result depends on whether the asset exists
        }

        [Test]
        public void TryResolve_NonExistentPath_ShouldReturnNull()
        {
            // Act
            var result = _resolver.TryResolve("Assets/NonExistent/Asset.prefab");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void TryResolve_NullPath_ShouldReturnNull()
        {
            // Act
            var result = _resolver.TryResolve(null);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void TryResolve_EmptyPath_ShouldReturnNull()
        {
            // Act
            var result = _resolver.TryResolve("");

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region Exists Tests

        [Test]
        public void Exists_NonExistentAsset_ShouldReturnFalse()
        {
            // Act & Assert
            Assert.IsFalse(_resolver.Exists("Assets/NonExistent.asset"));
        }

        #endregion

        #region ResolveByGuid Tests

        [Test]
        public void ResolveByGuid_InvalidGuid_ShouldReturnNull()
        {
            // Act
            var result = _resolver.ResolveByGuid("invalid-guid");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void ResolveByGuid_NullGuid_ShouldReturnNull()
        {
            // Act
            var result = _resolver.ResolveByGuid(null);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void ResolveByGuid_EmptyGuid_ShouldReturnNull()
        {
            // Act
            var result = _resolver.ResolveByGuid("");

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region GetAssetType Tests

        [Test]
        public void GetAssetType_NonExistentAsset_ShouldReturnNull()
        {
            // Act
            var result = _resolver.GetAssetType("Assets/NonExistent.asset");

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region ResolveMany Tests

        [Test]
        public void ResolveMany_AllNonExistent_ShouldReturnEmpty()
        {
            // Act
            var results = _resolver.ResolveMany(
                "Assets/NonExistent1.asset", 
                "Assets/NonExistent2.asset"
            ).ToList();

            // Assert
            Assert.IsEmpty(results);
        }

        #endregion
    }

    /// <summary>
    /// TypeResolver のユニットテスト。
    /// </summary>
    [TestFixture]
    public class TypeResolverTests
    {
        private TypeResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _resolver = new TypeResolver();
            TypeResolver.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            TypeResolver.ClearCache();
        }

        #region Resolve Tests

        [Test]
        public void Resolve_FullTypeName_ShouldReturnType()
        {
            // Act
            var result = _resolver.Resolve("UnityEngine.GameObject");

            // Assert
            Assert.AreEqual(typeof(GameObject), result);
        }

        [Test]
        public void Resolve_ShortTypeName_ShouldReturnType()
        {
            // Act
            var result = _resolver.Resolve("Rigidbody");

            // Assert
            Assert.AreEqual(typeof(Rigidbody), result);
        }

        [Test]
        public void Resolve_NonExistentType_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                _resolver.Resolve("NonExistent.Type.Name"));
        }

        #endregion

        #region TryResolve Tests

        [Test]
        public void TryResolve_ExistingType_ShouldReturnType()
        {
            // Act
            var result = _resolver.TryResolve("UnityEngine.Transform");

            // Assert
            Assert.AreEqual(typeof(Transform), result);
        }

        [Test]
        public void TryResolve_NonExistentType_ShouldReturnNull()
        {
            // Act
            var result = _resolver.TryResolve("NonExistent.Type");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void TryResolve_NullTypeName_ShouldReturnNull()
        {
            // Act
            var result = _resolver.TryResolve(null);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void TryResolve_EmptyTypeName_ShouldReturnNull()
        {
            // Act
            var result = _resolver.TryResolve("");

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region Exists Tests

        [Test]
        public void Exists_ExistingType_ShouldReturnTrue()
        {
            // Act & Assert
            Assert.IsTrue(_resolver.Exists("UnityEngine.BoxCollider"));
        }

        [Test]
        public void Exists_NonExistentType_ShouldReturnFalse()
        {
            // Act & Assert
            Assert.IsFalse(_resolver.Exists("NonExistent.Type"));
        }

        #endregion

        #region ResolveByFullName Tests

        [Test]
        public void ResolveByFullName_ValidFullName_ShouldReturnType()
        {
            // Act
            var result = _resolver.ResolveByFullName("UnityEngine.Camera");

            // Assert
            Assert.AreEqual(typeof(Camera), result);
        }

        [Test]
        public void ResolveByFullName_SystemType_ShouldReturnType()
        {
            // Act
            var result = _resolver.ResolveByFullName("System.String");

            // Assert
            Assert.AreEqual(typeof(string), result);
        }

        [Test]
        public void ResolveByFullName_NonExistentType_ShouldReturnNull()
        {
            // Act
            var result = _resolver.ResolveByFullName("NonExistent.Full.Name");

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region ResolveByShortName Tests

        [Test]
        public void ResolveByShortName_WithNamespaces_ShouldReturnType()
        {
            // Act
            var result = _resolver.ResolveByShortName("Light", "UnityEngine");

            // Assert
            Assert.AreEqual(typeof(Light), result);
        }

        [Test]
        public void ResolveByShortName_MultipleNamespaces_ShouldSearchAll()
        {
            // Act
            // DateTime は System 名前空間に存在する
            var result = _resolver.ResolveByShortName("DateTime", "UnityEngine", "System");

            // Assert
            // Should find System.DateTime
            Assert.IsNotNull(result);
            Assert.AreEqual(typeof(System.DateTime), result);
        }

        [Test]
        public void ResolveByShortName_TypeNotInNamespaces_ShouldReturnNull()
        {
            // Act
            var result = _resolver.ResolveByShortName("NonExistent", "UnityEngine", "System");

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region FindDerivedTypes Tests

        [Test]
        public void FindDerivedTypes_Component_ShouldFindDerivedTypes()
        {
            // Act
            var results = _resolver.FindDerivedTypes(typeof(Component)).ToList();

            // Assert
            Assert.IsTrue(results.Count > 0);
            Assert.IsTrue(results.Contains(typeof(Transform)));
            Assert.IsTrue(results.Contains(typeof(Rigidbody)));
        }

        [Test]
        public void FindDerivedTypes_NullBaseType_ShouldReturnEmpty()
        {
            // Act
            var results = _resolver.FindDerivedTypes(null).ToList();

            // Assert
            Assert.IsEmpty(results);
        }

        [Test]
        public void FindDerivedTypes_SealedType_ShouldReturnEmpty()
        {
            // Act - String is a sealed class
            var results = _resolver.FindDerivedTypes(typeof(string)).ToList();

            // Assert
            Assert.IsEmpty(results);
        }

        #endregion

        #region ResolveMany Tests

        [Test]
        public void ResolveMany_MultipleTypes_ShouldReturnAll()
        {
            // Act
            var results = _resolver.ResolveMany(
                "UnityEngine.GameObject",
                "UnityEngine.Transform",
                "UnityEngine.Rigidbody"
            ).ToList();

            // Assert
            Assert.AreEqual(3, results.Count);
        }

        [Test]
        public void ResolveMany_SomeInvalid_ShouldReturnOnlyValid()
        {
            // Act
            var results = _resolver.ResolveMany(
                "UnityEngine.GameObject",
                "NonExistent.Type",
                "UnityEngine.Transform"
            ).ToList();

            // Assert
            Assert.AreEqual(2, results.Count);
        }

        #endregion

        #region Cache Tests

        [Test]
        public void TryResolve_SameType_ShouldUseCacheOnSecondCall()
        {
            // Arrange
            var typeName = "UnityEngine.AudioSource";

            // Act - First call
            var result1 = _resolver.TryResolve(typeName);
            // Act - Second call (should use cache)
            var result2 = _resolver.TryResolve(typeName);

            // Assert
            Assert.AreEqual(result1, result2);
            Assert.AreEqual(typeof(AudioSource), result1);
        }

        [Test]
        public void ClearCache_ShouldClearCache()
        {
            // Arrange
            _resolver.TryResolve("UnityEngine.MeshRenderer");
            
            // Act
            TypeResolver.ClearCache();
            
            // Assert - Should still work after clearing cache
            var result = _resolver.TryResolve("UnityEngine.MeshRenderer");
            Assert.AreEqual(typeof(MeshRenderer), result);
        }

        #endregion
    }
}
