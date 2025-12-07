using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace UnityAIForge.Tests.Editor
{
    /// <summary>
    /// Base class for Editor tests with common setup/teardown functionality.
    /// Provides consistent test environment management and resource cleanup.
    /// 
    /// Usage:
    /// [TestFixture]
    /// public class MyTests : EditorTestBase
    /// {
    ///     [Test]
    ///     public void MyTest()
    ///     {
    ///         var go = CreateTestGameObject("TestObject");
    ///         // Test logic...
    ///     }
    /// }
    /// </summary>
    public abstract class EditorTestBase
    {
        /// <summary>
        /// List of test GameObjects that will be automatically cleaned up in Teardown
        /// </summary>
        protected List<GameObject> testObjects = new List<GameObject>();
        
        /// <summary>
        /// Path to test scene file (if any) that will be cleaned up in Teardown
        /// </summary>
        protected string testScenePath;
        
        /// <summary>
        /// Setup method called before each test.
        /// Creates a new empty scene and clears test objects list.
        /// Override this method to add custom setup logic, but remember to call base.Setup().
        /// </summary>
        [SetUp]
        public virtual void Setup()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            testObjects.Clear();
        }
        
        /// <summary>
        /// Teardown method called after each test.
        /// Cleans up all test objects and test scene if it exists.
        /// Override this method to add custom cleanup logic, but remember to call base.Teardown().
        /// </summary>
        [TearDown]
        public virtual void Teardown()
        {
            CleanupTestObjects();
            CleanupTestScene();
            CleanupTestAssets();
        }
        
        /// <summary>
        /// Destroys all GameObjects in the testObjects list
        /// </summary>
        protected void CleanupTestObjects()
        {
            foreach (var obj in testObjects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
            testObjects.Clear();
        }
        
        /// <summary>
        /// Deletes the test scene file if it exists
        /// </summary>
        protected void CleanupTestScene()
        {
            if (!string.IsNullOrEmpty(testScenePath) && System.IO.File.Exists(testScenePath))
            {
                AssetDatabase.DeleteAsset(testScenePath);
                testScenePath = null;
            }
        }
        
        /// <summary>
        /// Cleans up any test assets created in Assets folder
        /// Override this to implement custom asset cleanup
        /// </summary>
        protected virtual void CleanupTestAssets()
        {
            // Subclasses can override to clean up specific test assets
        }
        
        /// <summary>
        /// Creates a test GameObject and automatically adds it to the cleanup list
        /// </summary>
        /// <param name="name">Name of the GameObject</param>
        /// <returns>Created GameObject</returns>
        protected GameObject CreateTestGameObject(string name = "TestObject")
        {
            var go = new GameObject(name);
            testObjects.Add(go);
            return go;
        }
        
        /// <summary>
        /// Creates a test GameObject with components and automatically adds it to the cleanup list
        /// </summary>
        /// <param name="name">Name of the GameObject</param>
        /// <param name="types">Component types to add</param>
        /// <returns>Created GameObject</returns>
        protected GameObject CreateTestGameObject(string name, params System.Type[] types)
        {
            var go = new GameObject(name, types);
            testObjects.Add(go);
            return go;
        }
        
        /// <summary>
        /// Creates a test GameObject with a specific component and returns both
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <param name="name">Name of the GameObject</param>
        /// <returns>Tuple of (GameObject, Component)</returns>
        protected (GameObject go, T component) CreateTestGameObjectWith<T>(string name = "TestObject") where T : Component
        {
            var go = CreateTestGameObject(name);
            var component = go.AddComponent<T>();
            return (go, component);
        }
        
        /// <summary>
        /// Adds a test GameObject to the cleanup list (for objects created elsewhere)
        /// </summary>
        /// <param name="go">GameObject to track</param>
        protected void TrackTestObject(GameObject go)
        {
            if (go != null && !testObjects.Contains(go))
            {
                testObjects.Add(go);
            }
        }
        
        /// <summary>
        /// Removes a GameObject from the cleanup list (useful when you want to handle cleanup manually)
        /// </summary>
        /// <param name="go">GameObject to untrack</param>
        protected void UntrackTestObject(GameObject go)
        {
            testObjects.Remove(go);
        }
        
        /// <summary>
        /// Creates a test asset and saves it to the Assets folder
        /// The asset path will be tracked for cleanup
        /// </summary>
        /// <typeparam name="T">Asset type (e.g., ScriptableObject)</typeparam>
        /// <param name="assetPath">Path where to save the asset (e.g., "Assets/TestAsset.asset")</param>
        /// <returns>Created asset</returns>
        protected T CreateTestAsset<T>(string assetPath) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return asset;
        }
        
        /// <summary>
        /// Asserts that a GameObject exists in the scene
        /// </summary>
        /// <param name="name">GameObject name</param>
        /// <param name="message">Optional custom assertion message</param>
        protected void AssertGameObjectExists(string name, string message = null)
        {
            var go = GameObject.Find(name);
            Assert.IsNotNull(go, message ?? $"GameObject '{name}' should exist in scene");
        }
        
        /// <summary>
        /// Asserts that a GameObject does not exist in the scene
        /// </summary>
        /// <param name="name">GameObject name</param>
        /// <param name="message">Optional custom assertion message</param>
        protected void AssertGameObjectNotExists(string name, string message = null)
        {
            var go = GameObject.Find(name);
            Assert.IsNull(go, message ?? $"GameObject '{name}' should not exist in scene");
        }
        
        /// <summary>
        /// Asserts that a GameObject has a specific component
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <param name="go">GameObject to check</param>
        /// <param name="message">Optional custom assertion message</param>
        protected void AssertHasComponent<T>(GameObject go, string message = null) where T : Component
        {
            var component = go.GetComponent<T>();
            Assert.IsNotNull(component, message ?? $"GameObject '{go.name}' should have {typeof(T).Name} component");
        }
        
        /// <summary>
        /// Asserts that a GameObject does not have a specific component
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <param name="go">GameObject to check</param>
        /// <param name="message">Optional custom assertion message</param>
        protected void AssertNotHasComponent<T>(GameObject go, string message = null) where T : Component
        {
            var component = go.GetComponent<T>();
            Assert.IsNull(component, message ?? $"GameObject '{go.name}' should not have {typeof(T).Name} component");
        }
        
        /// <summary>
        /// Waits for a condition to become true (useful for async operations in tests)
        /// Note: This is blocking and should be used sparingly in EditMode tests
        /// </summary>
        /// <param name="condition">Condition to wait for</param>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        /// <param name="message">Error message if timeout occurs</param>
        protected void WaitForCondition(System.Func<bool> condition, int timeoutMs = 5000, string message = "Condition timeout")
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (!condition())
            {
                if (stopwatch.ElapsedMilliseconds > timeoutMs)
                {
                    Assert.Fail($"{message} (waited {timeoutMs}ms)");
                }
                System.Threading.Thread.Sleep(10);
            }
        }
    }
}
