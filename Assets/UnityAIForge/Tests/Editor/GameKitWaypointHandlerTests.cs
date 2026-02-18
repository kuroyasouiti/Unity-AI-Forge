using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitWaypointHandlerTests : GameKitHandlerTestBase
    {
        private GameKitWaypointHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitWaypointHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitWaypoint", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete",
                "addWaypoint", "removeWaypoint", "clearWaypoints",
                "startPath", "stopPath", "pausePath", "resumePath", "resetPath",
                "goToWaypoint", "findByWaypointId");
        }

        #endregion

        #region Create

        [Test]
        public void Create_ShouldGenerateScript()
        {
            var result = Execute(_handler, "create",
                ("waypointId", "test_waypoint"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("test_waypoint");

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "waypointId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "create",
                ("waypointId", "test_waypoint"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomWaypoint"));
            TrackCreatedGameObject("test_waypoint");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomWaypoint");
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
