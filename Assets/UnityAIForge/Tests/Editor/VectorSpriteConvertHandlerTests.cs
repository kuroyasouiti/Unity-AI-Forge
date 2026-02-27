using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class VectorSpriteConvertHandlerTests
    {
        private VectorSpriteConvertHandler _handler;
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _handler = new VectorSpriteConvertHandler();
            _tempDir = TestUtilities.CreateTempAssetDirectory();
        }

        [TearDown]
        public void TearDown()
        {
            TestUtilities.CleanupTempAssetDirectory(_tempDir);
        }

        [Test]
        public void Category_ReturnsSprite()
        {
            Assert.AreEqual("vectorSpriteConvert", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("primitiveToSprite", ops);
            Assert.Contains("svgToSprite", ops);
            Assert.Contains("textureToSprite", ops);
            Assert.Contains("createColorSprite", ops);
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
        public void CreateColorSprite_ValidPayload_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("createColorSprite",
                ("outputPath", $"{_tempDir}/color_sprite.png"),
                ("width", 32),
                ("height", 32),
                ("color", new Dictionary<string, object> { ["r"] = 1, ["g"] = 0, ["b"] = 0, ["a"] = 1 })));
            TestUtilities.AssertSuccess(result);
        }
    }
}
