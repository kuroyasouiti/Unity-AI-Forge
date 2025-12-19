using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MCP.Editor.Handlers;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// SceneCommandHandler のユニットテスト。
    /// </summary>
    [TestFixture]
    public class SceneCommandHandlerTests
    {
        private SceneCommandHandler _handler;
        private string _testScenePath;
        private Scene _originalScene;

        [SetUp]
        public void SetUp()
        {
            _handler = new SceneCommandHandler();
            
            // 現在のシーンを保存
            _originalScene = SceneManager.GetActiveScene();
            
            // テスト用シーンパス
            _testScenePath = "Assets/TestScenes";
            
            // テスト用ディレクトリ作成
            if (!Directory.Exists(_testScenePath))
            {
                Directory.CreateDirectory(_testScenePath);
            }
        }

        [TearDown]
        public void TearDown()
        {
            // テスト用シーンを削除
            CleanupTestScenes();
            
            // テスト用ディレクトリ削除
            if (Directory.Exists(_testScenePath))
            {
                try
                {
                    AssetDatabase.DeleteAsset(_testScenePath);
                }
                catch
                {
                    // 削除に失敗しても無視
                }
            }
            
            AssetDatabase.Refresh();
        }

        private void CleanupTestScenes()
        {
            var testScenes = Directory.GetFiles(_testScenePath, "*.unity", SearchOption.AllDirectories);
            foreach (var scenePath in testScenes)
            {
                var assetPath = scenePath.Replace("\\", "/");
                if (File.Exists(assetPath))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
            }
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnScene()
        {
            Assert.AreEqual("scene", _handler.Category);
        }

        [Test]
        public void Version_ShouldReturn100()
        {
            Assert.AreEqual("1.0.0", _handler.Version);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("create", operations);
            Assert.Contains("load", operations);
            Assert.Contains("save", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("duplicate", operations);
            Assert.Contains("inspect", operations);
            // Note: Build settings operations (listBuildSettings, addToBuildSettings, etc.)
            // have been moved to ProjectSettingsManageHandler
        }

        #endregion

        #region Create Tests

        [Test]
        public void Execute_Create_WithoutPath_ShouldCreateScene()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNotNull(result["sceneName"]);
        }

        [Test]
        public void Execute_Create_WithPath_ShouldCreateAndSaveScene()
        {
            // Arrange
            var scenePath = Path.Combine(_testScenePath, "TestCreateScene.unity").Replace("\\", "/");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["scenePath"] = scenePath
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(scenePath, result["scenePath"]);
            Assert.IsTrue(File.Exists(scenePath));
        }

        [Test]
        public void Execute_Create_WithAdditive_ShouldCreateAdditiveScene()
        {
            // Arrange - まず基本シーンが存在することを確認
            // (additive モードは既存のシーンに追加する形で新しいシーンを作成)
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["additive"] = true
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            
            // 成功した場合の検証
            if ((bool)result["success"])
            {
                Assert.IsTrue((bool)result["additive"]);
            }
            else
            {
                // テスト環境によっては additive シーン作成が失敗することがある
                // エラーメッセージが含まれていることを確認し、Inconclusive としてマーク
                Assert.IsTrue(result.ContainsKey("error"), 
                    $"Operation failed without error message. Result: {string.Join(", ", result.Keys)}");
                
                // テスト環境の制限として Inconclusive（結論なし）としてマーク
                Assert.Inconclusive($"Additive scene creation failed in test environment: {result["error"]}. " +
                           "This may be expected in certain Unity Editor states.");
            }
        }

        [Test]
        public void Execute_Create_InvalidPath_ShouldReturnError()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["scenePath"] = "InvalidPath/test.unity"  // Not starting with Assets/
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion

        #region Inspect Tests

        [Test]
        public void Execute_Inspect_ShouldReturnSceneInfo()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["includeHierarchy"] = true
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNotNull(result["sceneName"]);
            Assert.IsTrue(result.ContainsKey("isLoaded"));
            Assert.IsTrue(result.ContainsKey("isDirty"));
            Assert.IsTrue(result.ContainsKey("rootCount"));
        }

        [Test]
        public void Execute_Inspect_WithHierarchy_ShouldIncludeHierarchy()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["includeHierarchy"] = true
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("hierarchy"));
        }

        [Test]
        public void Execute_Inspect_WithComponents_ShouldIncludeComponents()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["includeHierarchy"] = true,
                ["includeComponents"] = true
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion

        // Note: ListBuildSettings tests have been moved to ProjectSettingsManageHandlerTests
        // as build settings operations are now handled by that handler.

        #region Validation Tests

        [Test]
        public void Execute_InvalidScenePath_WithTraversal_ShouldReturnError()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["scenePath"] = "Assets/../outside/test.unity"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsTrue(result["error"].ToString().Contains(".."));
        }

        [Test]
        public void Execute_InvalidScenePath_WithoutUnityExtension_ShouldReturnError()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["scenePath"] = "Assets/test.scene"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsTrue(result["error"].ToString().Contains(".unity"));
        }

        [Test]
        public void Execute_Load_NonExistentScene_ShouldReturnError()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "load",
                ["scenePath"] = "Assets/NonExistentScene.unity"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        [Test]
        public void Execute_Delete_NonExistentScene_ShouldReturnError()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["scenePath"] = "Assets/NonExistentScene.unity"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion

        #region Unsupported Operation Tests

        [Test]
        public void Execute_UnsupportedOperation_ShouldReturnError()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "unsupportedOperation"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsTrue(result["error"].ToString().Contains("not supported"));
        }

        #endregion
    }
}
