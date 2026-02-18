using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitSpawnerHandlerTests : GameKitHandlerTestBase
    {
        private GameKitSpawnerHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitSpawnerHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitSpawner", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete",
                "start", "stop", "reset",
                "spawnOne", "spawnBurst", "despawnAll",
                "addSpawnPoint", "addWave", "findBySpawnerId");
        }

        #endregion

        #region Create

        [Test]
        public void Create_ShouldGenerateScript()
        {
            var result = Execute(_handler, "create",
                ("spawnerId", "test_spawner"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("test_spawner");

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "spawnerId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "create",
                ("spawnerId", "test_spawner"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomSpawner"));
            TrackCreatedGameObject("test_spawner");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomSpawner");
        }

        [Test]
        public void Create_DefaultClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "create",
                ("spawnerId", "test_spawner"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("test_spawner");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "TestSpawnerSpawner");
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
