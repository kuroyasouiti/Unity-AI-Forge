using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MCP.Editor.Handlers;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// MaterialBundleHandler unit tests.
    /// Tests material creation, modification, and preset application.
    /// </summary>
    [TestFixture]
    public class MaterialBundleHandlerTests
    {
        private MaterialBundleHandler _handler;
        private List<string> _createdAssetPaths;
        private const string TestAssetFolder = "Assets/UnityAIForge/Tests/Editor/TestAssets";

        [SetUp]
        public void SetUp()
        {
            _handler = new MaterialBundleHandler();
            _createdAssetPaths = new List<string>();

            // Ensure test folder exists
            if (!AssetDatabase.IsValidFolder(TestAssetFolder))
            {
                string parentFolder = Path.GetDirectoryName(TestAssetFolder).Replace("\\", "/");
                string folderName = Path.GetFileName(TestAssetFolder);
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up created assets
            foreach (var path in _createdAssetPaths)
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
            _createdAssetPaths.Clear();
            AssetDatabase.Refresh();
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnMaterialBundle()
        {
            Assert.AreEqual("MaterialBundle", _handler.Category);
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
            Assert.Contains("update", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("duplicate", operations);
            Assert.Contains("setTexture", operations);
            Assert.Contains("listPresets", operations);
        }

        #endregion

        #region Create Operation Tests

        [Test]
        public void Execute_Create_ShouldCreateMaterialWithDefaultPreset()
        {
            string assetPath = $"{TestAssetFolder}/TestMaterial_Create.mat";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestMaterial_Create",
                ["savePath"] = assetPath
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(File.Exists(assetPath.Replace("Assets/", Application.dataPath + "/")));

            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            Assert.IsNotNull(material);
        }

        [Test]
        public void Execute_Create_WithPreset_ShouldApplyPresetSettings()
        {
            string assetPath = $"{TestAssetFolder}/TestMaterial_Preset.mat";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestMaterial_Preset",
                ["savePath"] = assetPath,
                ["preset"] = "transparent"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("transparent", result["preset"]);
        }

        [Test]
        public void Execute_Create_WithColor_ShouldApplyColor()
        {
            string assetPath = $"{TestAssetFolder}/TestMaterial_Color.mat";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestMaterial_Color",
                ["savePath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["color"] = new Dictionary<string, object>
                    {
                        ["r"] = 1f,
                        ["g"] = 0f,
                        ["b"] = 0f,
                        ["a"] = 1f
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            Assert.IsNotNull(material);
        }

        #endregion

        #region Update Operation Tests

        [Test]
        public void Execute_Update_ShouldModifyExistingMaterial()
        {
            // First create a material
            string assetPath = $"{TestAssetFolder}/TestMaterial_Update.mat";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestMaterial_Update",
                ["savePath"] = assetPath
            };
            _handler.Execute(createPayload);

            // Then update it
            var updatePayload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["color"] = new Dictionary<string, object>
                    {
                        ["r"] = 0f,
                        ["g"] = 1f,
                        ["b"] = 0f,
                        ["a"] = 1f
                    }
                }
            };

            var result = _handler.Execute(updatePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        [Test]
        public void Execute_Update_NonExistentMaterial_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["assetPath"] = "Assets/NonExistent.mat"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("error"));
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnMaterialProperties()
        {
            // First create a material
            string assetPath = $"{TestAssetFolder}/TestMaterial_Inspect.mat";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestMaterial_Inspect",
                ["savePath"] = assetPath
            };
            _handler.Execute(createPayload);

            // Then inspect it
            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["assetPath"] = assetPath
            };

            var result = _handler.Execute(inspectPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("shader"));
            Assert.IsTrue(result.ContainsKey("renderQueue"));
        }

        #endregion

        #region Delete Operation Tests

        [Test]
        public void Execute_Delete_ShouldRemoveMaterial()
        {
            // First create a material
            string assetPath = $"{TestAssetFolder}/TestMaterial_Delete.mat";

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestMaterial_Delete",
                ["savePath"] = assetPath
            };
            _handler.Execute(createPayload);

            // Then delete it
            var deletePayload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["assetPath"] = assetPath
            };

            var result = _handler.Execute(deletePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<Material>(assetPath));
        }

        #endregion

        #region Duplicate Operation Tests

        [Test]
        public void Execute_Duplicate_ShouldCreateCopy()
        {
            // First create a material
            string sourcePath = $"{TestAssetFolder}/TestMaterial_Source.mat";
            string destPath = $"{TestAssetFolder}/TestMaterial_Copy.mat";
            _createdAssetPaths.Add(sourcePath);
            _createdAssetPaths.Add(destPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestMaterial_Source",
                ["savePath"] = sourcePath
            };
            _handler.Execute(createPayload);

            // Then duplicate it
            var duplicatePayload = new Dictionary<string, object>
            {
                ["operation"] = "duplicate",
                ["assetPath"] = sourcePath,
                ["destinationPath"] = destPath
            };

            var result = _handler.Execute(duplicatePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Material>(destPath));
        }

        #endregion

        #region ListPresets Operation Tests

        [Test]
        public void Execute_ListPresets_ShouldReturnPresetList()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "listPresets"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("presets"));

            var presets = result["presets"] as List<Dictionary<string, object>>;
            Assert.IsNotNull(presets);
            Assert.IsTrue(presets.Count > 0);
        }

        #endregion
    }
}
