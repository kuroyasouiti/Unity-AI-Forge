using MCP.Editor.Base;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// ResourceResolverのユニットテスト。
    /// </summary>
    public class ResourceResolverTests
    {
        private GameObjectResolver _gameObjectResolver;
        private AssetResolver _assetResolver;
        private TypeResolver _typeResolver;
        
        [SetUp]
        public void SetUp()
        {
            _gameObjectResolver = new GameObjectResolver();
            _assetResolver = new AssetResolver();
            _typeResolver = new TypeResolver();
        }
        
        [TearDown]
        public void TearDown()
        {
            // テスト用に作成したGameObjectをクリーンアップ
            var testObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (var obj in testObjects)
            {
                if (obj.name.StartsWith("Test_"))
                {
                    Object.DestroyImmediate(obj);
                }
            }
        }
        
        #region GameObjectResolver Tests
        
        [Test]
        public void TestGameObjectResolver_ExistingObject_Resolves()
        {
            var testObject = new GameObject("Test_Object");
            
            var resolved = _gameObjectResolver.TryResolve("Test_Object");
            
            Assert.IsNotNull(resolved);
            Assert.AreEqual("Test_Object", resolved.name);
        }
        
        [Test]
        public void TestGameObjectResolver_NonExistingObject_ReturnsNull()
        {
            var resolved = _gameObjectResolver.TryResolve("NonExistent");
            
            Assert.IsNull(resolved);
        }
        
        [Test]
        public void TestGameObjectResolver_HierarchyPath_Resolves()
        {
            var parent = new GameObject("Test_Parent");
            var child = new GameObject("Child");
            child.transform.SetParent(parent.transform);
            
            var resolved = _gameObjectResolver.ResolveByHierarchyPath("Test_Parent/Child");
            
            Assert.IsNotNull(resolved);
            Assert.AreEqual("Child", resolved.name);
        }
        
        [Test]
        public void TestGameObjectResolver_FindByPattern_WildcardMatches()
        {
            new GameObject("Test_Object1");
            new GameObject("Test_Object2");
            new GameObject("Other_Object");
            
            var results = _gameObjectResolver.FindByPattern("Test_*", useRegex: false);
            
            Assert.IsTrue(results.Count() >= 2);
        }
        
        [Test]
        public void TestGameObjectResolver_FindByPattern_RegexMatches()
        {
            new GameObject("Test_Object1");
            new GameObject("Test_Object2");
            new GameObject("Test_Other");
            
            var results = _gameObjectResolver.FindByPattern("^Test_Object\\d+$", useRegex: true);
            
            Assert.IsTrue(results.Count() >= 2);
        }
        
        #endregion
        
        #region AssetResolver Tests
        
        [Test]
        public void TestAssetResolver_ValidatePath_ValidPath_ReturnsTrue()
        {
            var result = _assetResolver.ValidatePath("Assets/Test/File.txt");
            
            Assert.IsTrue(result);
        }
        
        [Test]
        public void TestAssetResolver_ValidatePath_InvalidPath_ReturnsFalse()
        {
            var result = _assetResolver.ValidatePath("../../../etc/passwd");
            
            Assert.IsFalse(result);
        }
        
        [Test]
        public void TestAssetResolver_ValidatePath_PathTraversal_ReturnsFalse()
        {
            var result = _assetResolver.ValidatePath("Assets/../../../etc/passwd");
            
            Assert.IsFalse(result);
        }
        
        [Test]
        public void TestAssetResolver_ValidatePath_NoAssetsPrefix_ReturnsFalse()
        {
            var result = _assetResolver.ValidatePath("Test/File.txt");
            
            Assert.IsFalse(result);
        }
        
        #endregion
        
        #region TypeResolver Tests
        
        [Test]
        public void TestTypeResolver_ResolveByFullName_UnityType_Resolves()
        {
            var type = _typeResolver.ResolveByFullName("UnityEngine.GameObject");
            
            Assert.IsNotNull(type);
            Assert.AreEqual(typeof(GameObject), type);
        }
        
        [Test]
        public void TestTypeResolver_ResolveByShortName_WithNamespace_Resolves()
        {
            var type = _typeResolver.ResolveByShortName("GameObject", "UnityEngine");
            
            Assert.IsNotNull(type);
            Assert.AreEqual(typeof(GameObject), type);
        }
        
        [Test]
        public void TestTypeResolver_TryResolve_CachesResult()
        {
            var type1 = _typeResolver.TryResolve("UnityEngine.GameObject");
            var type2 = _typeResolver.TryResolve("UnityEngine.GameObject");
            
            Assert.AreSame(type1, type2); // 同じインスタンスが返される（キャッシュ）
        }
        
        [Test]
        public void TestTypeResolver_FindDerivedTypes_FindsSubclasses()
        {
            var derivedTypes = _typeResolver.FindDerivedTypes(typeof(MonoBehaviour));
            
            Assert.IsTrue(derivedTypes.Any());
        }
        
        [Test]
        public void TestTypeResolver_ResolveNonExistentType_ReturnsNull()
        {
            var type = _typeResolver.TryResolve("NonExistent.Type");
            
            Assert.IsNull(type);
        }
        
        #endregion
    }
}

