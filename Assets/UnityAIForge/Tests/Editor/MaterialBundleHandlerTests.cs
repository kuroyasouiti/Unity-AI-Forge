using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class MaterialBundleHandlerTests
    {
        private MaterialBundleHandler _handler;
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _handler = new MaterialBundleHandler();
            _tempDir = TestUtilities.CreateTempAssetDirectory();
        }

        [TearDown]
        public void TearDown()
        {
            TestUtilities.CleanupTempAssetDirectory(_tempDir);
        }

        [Test]
        public void Category_ReturnsMaterialBundle()
        {
            Assert.AreEqual("materialBundle", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("create", ops);
            Assert.Contains("update", ops);
            Assert.Contains("setTexture", ops);
            Assert.Contains("setColor", ops);
            Assert.Contains("applyPreset", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("listPresets", ops);
        }

        [Test]
        public void Execute_NullPayload_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(null));
        }

        [Test]
        public void Execute_UnsupportedOperation_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(TestUtilities.CreatePayload("nonExistent")), "not supported");
        }

        [Test]
        public void Create_ValidPath_CreatesMaterialAsset()
        {
            var matPath = $"{_tempDir}/TestMat.mat";
            var result = _handler.Execute(TestUtilities.CreatePayload("create",
                ("savePath", matPath))) as Dictionary<string, object>;
            TestUtilities.AssertSuccess(result);

            Assert.IsTrue(result.ContainsKey("assetPath"));
            Assert.IsTrue(result.ContainsKey("shader"));
            var actualPath = result["assetPath"].ToString();
            var mat = AssetDatabase.LoadAssetAtPath<Material>(actualPath);
            Assert.IsNotNull(mat, $"Material asset should exist at '{actualPath}'");
        }

        [Test]
        public void Inspect_CreatedMaterial_ReturnsShaderInfo()
        {
            var matPath = $"{_tempDir}/InspectMat.mat";
            var createResult = _handler.Execute(TestUtilities.CreatePayload("create",
                ("savePath", matPath))) as Dictionary<string, object>;
            TestUtilities.AssertSuccess(createResult);
            var actualPath = createResult["assetPath"].ToString();

            var result = _handler.Execute(TestUtilities.CreatePayload("inspect",
                ("assetPath", actualPath))) as Dictionary<string, object>;
            TestUtilities.AssertSuccess(result);

            Assert.IsTrue(result.ContainsKey("shader"));
            Assert.IsTrue(result.ContainsKey("name"));
        }

        [Test]
        public void ListPresets_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("listPresets"));
            TestUtilities.AssertSuccess(result);
        }
    }
}
