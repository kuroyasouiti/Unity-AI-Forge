using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitActorHandlerTests : GameKitHandlerTestBase
    {
        private GameKitActorHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitActorHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitActor", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete");
        }

        #endregion

        #region Create

        [Test]
        public void Create_ShouldGenerateScript()
        {
            var result = Execute(_handler, "create",
                ("actorId", "test_actor"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("test_actor");

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "actorId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "create",
                ("actorId", "test_actor"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomActor"));
            TrackCreatedGameObject("test_actor");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomActor");
        }

        [Test]
        public void Create_DefaultClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "create",
                ("actorId", "test_actor"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("test_actor");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "TestActorActor");
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
