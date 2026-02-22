using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class AssetCommandHandlerTests
    {
        private AssetCommandHandler _handler;
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            // Asset operations trigger VS/Rider project sync which can cause
            // IOException sharing violations on .csproj files (Windows).
            LogAssert.ignoreFailingMessages = true;
            _handler = new AssetCommandHandler();
            _tempDir = TestUtilities.CreateTempAssetDirectory();
        }

        [TearDown]
        public void TearDown()
        {
            TestUtilities.CleanupTempAssetDirectory(_tempDir);
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void Category_ReturnsAsset()
        {
            Assert.AreEqual("asset", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("create", ops);
            Assert.Contains("update", ops);
            Assert.Contains("delete", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("findMultiple", ops);
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
        public void Create_TextAsset_ReturnsSuccess()
        {
            var assetPath = $"{_tempDir}/test.txt";
            var result = _handler.Execute(TestUtilities.CreatePayload("create",
                ("assetPath", assetPath),
                ("content", "test content")));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void Inspect_ExistingAsset_ReturnsDetails()
        {
            // Create an asset first
            var assetPath = $"{_tempDir}/inspect_test.txt";
            _handler.Execute(TestUtilities.CreatePayload("create",
                ("assetPath", assetPath),
                ("content", "inspect me")));

            var result = _handler.Execute(TestUtilities.CreatePayload("inspect",
                ("assetPath", assetPath)));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void Delete_ExistingAsset_ReturnsSuccess()
        {
            var assetPath = $"{_tempDir}/delete_test.txt";
            _handler.Execute(TestUtilities.CreatePayload("create",
                ("assetPath", assetPath),
                ("content", "delete me")));

            var result = _handler.Execute(TestUtilities.CreatePayload("delete",
                ("assetPath", assetPath)));
            TestUtilities.AssertSuccess(result);
        }
    }
}
