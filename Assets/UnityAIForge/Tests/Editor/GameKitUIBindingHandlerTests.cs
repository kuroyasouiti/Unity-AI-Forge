using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitUIBindingHandlerTests : GameKitHandlerTestBase
    {
        private GameKitUIBindingHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitUIBindingHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitUIBinding", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete",
                "setRange", "refresh", "findByBindingId");
        }

        #endregion

        #region Create

        [Test]
        public void Create_ShouldGenerateScript()
        {
            CreateTestGameObject("UITarget");
            var result = Execute(_handler, "create",
                ("targetPath", "UITarget"),
                ("bindingId", "test_binding"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "bindingId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            CreateTestGameObject("UITarget");
            var result = Execute(_handler, "create",
                ("targetPath", "UITarget"),
                ("bindingId", "test_binding"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomUIBinding"));

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomUIBinding");
        }

        [Test]
        public void Create_WithElementName_ShouldGenerateScript()
        {
            CreateTestGameObject("UITarget");
            var result = Execute(_handler, "create",
                ("targetPath", "UITarget"),
                ("bindingId", "test_binding_elem"),
                ("elementName", "hp-bar"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptGenerated(result);
        }

        #endregion

        #region Error Handling

        [Test]
        public void Create_MissingTargetPath_ShouldReturnError()
        {
            var result = Execute(_handler, "create",
                ("bindingId", "test_binding"),
                ("outputPath", TestOutputDir));
            AssertError(result);
        }

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
