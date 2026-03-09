using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class UIConvertHandlerTests
    {
        private UIConvertHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new UIConvertHandler();
        }

        [Test]
        public void Category_ReturnsUiConvert()
        {
            Assert.AreEqual("uiConvert", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsAllOps()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("analyze", ops);
            Assert.Contains("toUITK", ops);
            Assert.Contains("toUGUI", ops);
            Assert.Contains("extractStyles", ops);
        }

        [Test]
        public void Execute_NullPayload_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(null));
        }

        [Test]
        public void Execute_UnsupportedOperation_ReturnsError()
        {
            TestUtilities.AssertError(
                _handler.Execute(TestUtilities.CreatePayload("nonExistent")),
                "not supported");
        }

        [Test]
        public void Analyze_MissingSourceType_ReturnsError()
        {
            TestUtilities.AssertError(
                _handler.Execute(TestUtilities.CreatePayload("analyze",
                    ("sourcePath", "Canvas"))),
                "sourceType");
        }

        [Test]
        public void Analyze_MissingSourcePath_ReturnsError()
        {
            TestUtilities.AssertError(
                _handler.Execute(TestUtilities.CreatePayload("analyze",
                    ("sourceType", "ugui"))),
                "sourcePath");
        }

        [Test]
        public void Analyze_InvalidSourceType_ReturnsError()
        {
            TestUtilities.AssertError(
                _handler.Execute(TestUtilities.CreatePayload("analyze",
                    ("sourceType", "invalid"),
                    ("sourcePath", "Canvas"))),
                "Unsupported sourceType");
        }

        [Test]
        public void Analyze_UITK_NonExistentFile_ReturnsError()
        {
            TestUtilities.AssertError(
                _handler.Execute(TestUtilities.CreatePayload("analyze",
                    ("sourceType", "uitk"),
                    ("sourcePath", "Assets/NonExistent.uxml"))),
                "not found");
        }

        [Test]
        public void ToUITK_MissingSourcePath_ReturnsError()
        {
            TestUtilities.AssertError(
                _handler.Execute(TestUtilities.CreatePayload("toUITK")),
                "sourcePath");
        }

        [Test]
        public void ToUGUI_MissingSourcePath_ReturnsError()
        {
            TestUtilities.AssertError(
                _handler.Execute(TestUtilities.CreatePayload("toUGUI")),
                "sourcePath");
        }

        [Test]
        public void ToUGUI_NonExistentFile_ReturnsError()
        {
            TestUtilities.AssertError(
                _handler.Execute(TestUtilities.CreatePayload("toUGUI",
                    ("sourcePath", "Assets/NonExistent.uxml"))),
                "not found");
        }

        [Test]
        public void ExtractStyles_MissingSourcePath_ReturnsError()
        {
            TestUtilities.AssertError(
                _handler.Execute(TestUtilities.CreatePayload("extractStyles",
                    ("outputPath", "Assets/test.uss"))),
                "sourcePath");
        }

        [Test]
        public void ExtractStyles_MissingOutputPath_ReturnsError()
        {
            TestUtilities.AssertError(
                _handler.Execute(TestUtilities.CreatePayload("extractStyles",
                    ("sourcePath", "Canvas"))),
                "outputPath");
        }
    }
}
