using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class UITKAssetHandlerTests
    {
        private UITKAssetHandler _handler;
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _handler = new UITKAssetHandler();
            _tempDir = TestUtilities.CreateTempAssetDirectory();
        }

        [TearDown]
        public void TearDown()
        {
            TestUtilities.CleanupTempAssetDirectory(_tempDir);
        }

        [Test]
        public void Category_ReturnsUitkAsset()
        {
            Assert.AreEqual("uitkAsset", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("createUXML", ops);
            Assert.Contains("createUSS", ops);
            Assert.Contains("inspectUXML", ops);
            Assert.Contains("inspectUSS", ops);
            Assert.Contains("updateUXML", ops);
            Assert.Contains("updateUSS", ops);
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
        public void CreateUXML_ValidPath_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("createUXML",
                ("assetPath", $"{_tempDir}/test.uxml"),
                ("elements", new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["type"] = "VisualElement",
                        ["name"] = "root"
                    }
                })));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void CreateUSS_ValidPath_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("createUSS",
                ("assetPath", $"{_tempDir}/test.uss"),
                ("rules", new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["selector"] = ".root",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["background-color"] = "red"
                        }
                    }
                })));
            TestUtilities.AssertSuccess(result);
        }
    }
}
