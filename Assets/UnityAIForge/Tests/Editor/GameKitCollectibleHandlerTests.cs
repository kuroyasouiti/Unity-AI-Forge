using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitCollectibleHandlerTests : GameKitHandlerTestBase
    {
        private GameKitCollectibleHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitCollectibleHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitCollectible", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete",
                "collect", "respawn", "reset", "findByCollectibleId");
        }

        #endregion

        #region Create

        [Test]
        public void Create_ShouldGenerateScript()
        {
            var result = Execute(_handler, "create",
                ("collectibleId", "test_collectible"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("Collectible");

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "collectibleId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "create",
                ("collectibleId", "test_collectible"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomCollectible"));
            TrackCreatedGameObject("Collectible");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomCollectible");
        }

        [Test]
        public void Create_WithTargetPath_ShouldAttachToExistingGameObject()
        {
            CreateTestGameObject("PickupTarget");
            var result = Execute(_handler, "create",
                ("targetPath", "PickupTarget"),
                ("collectibleId", "target_collectible"),
                ("outputPath", TestOutputDir));

            AssertSuccess(result);
            AssertScriptGenerated(result);
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
