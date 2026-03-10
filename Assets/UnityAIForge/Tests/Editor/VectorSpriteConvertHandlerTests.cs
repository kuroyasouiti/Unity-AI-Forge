using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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

        #region Metadata

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

        #endregion

        #region Error Cases

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
        public void PrimitiveToSprite_MissingOutputPath_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(TestUtilities.CreatePayload("primitiveToSprite",
                ("primitiveType", "circle"))), "outputPath");
        }

        [Test]
        public void CreateColorSprite_MissingOutputPath_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(TestUtilities.CreatePayload("createColorSprite")), "outputPath");
        }

        #endregion

        #region createColorSprite

        [Test]
        public void CreateColorSprite_ValidPayload_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("createColorSprite",
                ("outputPath", $"{_tempDir}/color_sprite.png"),
                ("width", 32),
                ("height", 32),
                ("color", new Dictionary<string, object> { ["r"] = 1, ["g"] = 0, ["b"] = 0, ["a"] = 1 })));
            TestUtilities.AssertSuccess(result);

            var dict = result as Dictionary<string, object>;
            Assert.IsTrue(dict.ContainsKey("spritePath"));
            Assert.IsTrue(dict.ContainsKey("spriteGuid"));
            Assert.IsTrue(File.Exists(dict["spritePath"].ToString()));
        }

        [Test]
        public void CreateColorSprite_WithOutline_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("createColorSprite",
                ("outputPath", $"{_tempDir}/outlined_color.png"),
                ("width", 32),
                ("height", 32),
                ("color", new Dictionary<string, object> { ["r"] = 1, ["g"] = 1, ["b"] = 1, ["a"] = 1 }),
                ("outlineColor", new Dictionary<string, object> { ["r"] = 0, ["g"] = 0, ["b"] = 0, ["a"] = 1 }),
                ("outlineWidth", 4)));
            TestUtilities.AssertSuccess(result);
            Assert.IsTrue(File.Exists($"{_tempDir}/outlined_color.png"));
        }

        #endregion

        #region primitiveToSprite - Circle

        [Test]
        public void PrimitiveToSprite_Circle_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("primitiveToSprite",
                ("primitiveType", "circle"),
                ("outputPath", $"{_tempDir}/circle.png"),
                ("width", 64),
                ("height", 64),
                ("color", new Dictionary<string, object> { ["r"] = 0, ["g"] = 1, ["b"] = 0, ["a"] = 1 })));
            TestUtilities.AssertSuccess(result);

            var dict = result as Dictionary<string, object>;
            Assert.AreEqual("circle", dict["primitiveType"]);
            Assert.IsTrue(File.Exists(dict["spritePath"].ToString()));
        }

        [Test]
        public void PrimitiveToSprite_Circle_WithOutline_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("primitiveToSprite",
                ("primitiveType", "circle"),
                ("outputPath", $"{_tempDir}/circle_outline.png"),
                ("width", 64),
                ("height", 64),
                ("color", new Dictionary<string, object> { ["r"] = 1, ["g"] = 0, ["b"] = 0, ["a"] = 1 }),
                ("outlineColor", new Dictionary<string, object> { ["r"] = 0, ["g"] = 0, ["b"] = 0, ["a"] = 1 }),
                ("outlineWidth", 3)));
            TestUtilities.AssertSuccess(result);
        }

        #endregion

        #region primitiveToSprite - Square

        [Test]
        public void PrimitiveToSprite_Square_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("primitiveToSprite",
                ("primitiveType", "square"),
                ("outputPath", $"{_tempDir}/square.png"),
                ("width", 64),
                ("height", 64)));
            TestUtilities.AssertSuccess(result);

            var dict = result as Dictionary<string, object>;
            Assert.AreEqual("square", dict["primitiveType"]);
        }

        [Test]
        public void PrimitiveToSprite_Square_WithOutline_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("primitiveToSprite",
                ("primitiveType", "square"),
                ("outputPath", $"{_tempDir}/square_outline.png"),
                ("width", 64),
                ("height", 64),
                ("outlineColor", new Dictionary<string, object> { ["r"] = 1, ["g"] = 0, ["b"] = 0, ["a"] = 1 }),
                ("outlineWidth", 5)));
            TestUtilities.AssertSuccess(result);
        }

        #endregion

        #region primitiveToSprite - Triangle

        [Test]
        public void PrimitiveToSprite_Triangle_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("primitiveToSprite",
                ("primitiveType", "triangle"),
                ("outputPath", $"{_tempDir}/triangle.png"),
                ("width", 64),
                ("height", 64)));
            TestUtilities.AssertSuccess(result);

            var dict = result as Dictionary<string, object>;
            Assert.AreEqual("triangle", dict["primitiveType"]);
        }

        [Test]
        public void PrimitiveToSprite_Triangle_WithOutline_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("primitiveToSprite",
                ("primitiveType", "triangle"),
                ("outputPath", $"{_tempDir}/triangle_outline.png"),
                ("width", 64),
                ("height", 64),
                ("outlineColor", new Dictionary<string, object> { ["r"] = 0, ["g"] = 0, ["b"] = 1, ["a"] = 1 }),
                ("outlineWidth", 3)));
            TestUtilities.AssertSuccess(result);
        }

        #endregion

        #region primitiveToSprite - Polygon

        [Test]
        public void PrimitiveToSprite_Polygon_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("primitiveToSprite",
                ("primitiveType", "polygon"),
                ("outputPath", $"{_tempDir}/polygon.png"),
                ("width", 64),
                ("height", 64),
                ("sides", 8)));
            TestUtilities.AssertSuccess(result);

            var dict = result as Dictionary<string, object>;
            Assert.AreEqual("polygon", dict["primitiveType"]);
        }

        [Test]
        public void PrimitiveToSprite_Polygon_WithOutline_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("primitiveToSprite",
                ("primitiveType", "polygon"),
                ("outputPath", $"{_tempDir}/polygon_outline.png"),
                ("width", 64),
                ("height", 64),
                ("sides", 6),
                ("outlineColor", new Dictionary<string, object> { ["r"] = 1, ["g"] = 1, ["b"] = 0, ["a"] = 1 }),
                ("outlineWidth", 4)));
            TestUtilities.AssertSuccess(result);
        }

        #endregion

        #region Outline Edge Cases

        [Test]
        public void PrimitiveToSprite_OutlineWidthZero_NoOutlineApplied()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("primitiveToSprite",
                ("primitiveType", "circle"),
                ("outputPath", $"{_tempDir}/no_outline.png"),
                ("width", 32),
                ("height", 32),
                ("outlineColor", new Dictionary<string, object> { ["r"] = 0, ["g"] = 0, ["b"] = 0, ["a"] = 1 }),
                ("outlineWidth", 0)));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void PrimitiveToSprite_OutlineColorTransparent_NoOutlineApplied()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("primitiveToSprite",
                ("primitiveType", "circle"),
                ("outputPath", $"{_tempDir}/transparent_outline.png"),
                ("width", 32),
                ("height", 32),
                ("outlineColor", new Dictionary<string, object> { ["r"] = 0, ["g"] = 0, ["b"] = 0, ["a"] = 0 }),
                ("outlineWidth", 3)));
            TestUtilities.AssertSuccess(result);
        }

        #endregion

        #region Sprite Import Settings

        [Test]
        public void PrimitiveToSprite_AutoAddsPngExtension()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("primitiveToSprite",
                ("primitiveType", "circle"),
                ("outputPath", $"{_tempDir}/no_ext"),
                ("width", 32),
                ("height", 32)));
            TestUtilities.AssertSuccess(result);

            var dict = result as Dictionary<string, object>;
            StringAssert.EndsWith(".png", dict["spritePath"].ToString());
        }

        [Test]
        public void PrimitiveToSprite_SpriteGuid_IsNotEmpty()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("primitiveToSprite",
                ("primitiveType", "square"),
                ("outputPath", $"{_tempDir}/guid_test.png"),
                ("width", 32),
                ("height", 32)));
            TestUtilities.AssertSuccess(result);

            var dict = result as Dictionary<string, object>;
            var guid = dict["spriteGuid"].ToString();
            Assert.IsFalse(string.IsNullOrEmpty(guid));
        }

        [Test]
        public void PrimitiveToSprite_ResponseContainsSize()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("primitiveToSprite",
                ("primitiveType", "circle"),
                ("outputPath", $"{_tempDir}/size_test.png"),
                ("width", 128),
                ("height", 64)));
            TestUtilities.AssertSuccess(result);

            var dict = result as Dictionary<string, object>;
            var size = dict["size"] as Dictionary<string, object>;
            Assert.IsNotNull(size);
            Assert.AreEqual(128, size["width"]);
            Assert.AreEqual(64, size["height"]);
        }

        #endregion
    }
}
