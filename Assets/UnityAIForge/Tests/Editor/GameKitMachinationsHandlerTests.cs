using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitMachinationsHandlerTests : GameKitHandlerTestBase
    {
        private GameKitMachinationsHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitMachinationsHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitMachinations", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete", "apply", "export");
        }

        #endregion

        #region Create

        [Test]
        public void Create_ShouldGenerateScript()
        {
            var assetPath = "Assets/TestTemp/Generated/TestMachinations.asset";
            var result = Execute(_handler, "create",
                ("diagramId", "test_machinations"),
                ("assetPath", assetPath),
                ("outputPath", TestOutputDir));
            TrackAssetPath(assetPath);

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "diagramId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            var assetPath = "Assets/TestTemp/Generated/TestMachinations2.asset";
            var result = Execute(_handler, "create",
                ("diagramId", "test_machinations_custom"),
                ("assetPath", assetPath),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomMachinations"));
            TrackAssetPath(assetPath);

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomMachinations");
        }

        [Test]
        public void Create_DefaultDiagramId_ShouldAutoGenerate()
        {
            var assetPath = "Assets/TestTemp/Generated/TestMachinations3.asset";
            var result = Execute(_handler, "create",
                ("assetPath", assetPath),
                ("outputPath", TestOutputDir));
            TrackAssetPath(assetPath);

            AssertSuccess(result);
            AssertHasField(result, "diagramId");
        }

        #endregion

        #region Error Handling

        [Test]
        public void Execute_UnsupportedOperation_ShouldReturnError()
        {
            var result = Execute(_handler, "nonexistent_operation");
            AssertError(result);
        }

        [Test]
        public void Execute_NullPayload_ShouldReturnError()
        {
            var result = _handler.Execute(null) as Dictionary<string, object>;
            AssertError(result);
        }

        #endregion
    }
}
