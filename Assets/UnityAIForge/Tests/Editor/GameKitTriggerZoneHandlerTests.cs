using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitTriggerZoneHandlerTests : GameKitHandlerTestBase
    {
        private GameKitTriggerZoneHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitTriggerZoneHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitTriggerZone", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete",
                "activate", "deactivate", "reset",
                "setTeleportDestination", "findByZoneId");
        }

        #endregion

        #region Create

        [Test]
        public void Create_ShouldGenerateScript()
        {
            var result = Execute(_handler, "create",
                ("zoneId", "test_zone"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("test_zone");

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "zoneId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "create",
                ("zoneId", "test_zone"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomTriggerZone"));
            TrackCreatedGameObject("test_zone");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomTriggerZone");
        }

        [Test]
        public void Create_DefaultClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "create",
                ("zoneId", "test_zone"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("test_zone");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "TestZoneTriggerZone");
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
