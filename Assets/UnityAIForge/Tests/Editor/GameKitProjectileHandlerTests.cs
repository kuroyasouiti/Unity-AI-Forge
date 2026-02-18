using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.Handlers.GameKit;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameKitProjectileHandlerTests : GameKitHandlerTestBase
    {
        private GameKitProjectileHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitProjectileHandler();
        }

        #region Metadata

        [Test]
        public void Category_ShouldReturnExpected()
        {
            Assert.AreEqual("gamekitProjectile", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            AssertOperationsContain(_handler.SupportedOperations,
                "create", "update", "inspect", "delete",
                "launch", "setHomingTarget", "destroy", "findByProjectileId");
        }

        #endregion

        #region Create

        [Test]
        public void Create_ShouldGenerateScript()
        {
            var result = Execute(_handler, "create",
                ("projectileId", "test_projectile"),
                ("outputPath", TestOutputDir));
            TrackCreatedGameObject("test_projectile");

            AssertSuccess(result);
            AssertScriptGenerated(result);
            AssertHasField(result, "projectileId");
        }

        [Test]
        public void Create_GeneratedScriptClassName_ShouldBeCorrect()
        {
            var result = Execute(_handler, "create",
                ("projectileId", "test_projectile"),
                ("outputPath", TestOutputDir),
                ("className", "MyCustomProjectile"));
            TrackCreatedGameObject("test_projectile");

            AssertSuccess(result);
            AssertScriptContainsClass(result, "MyCustomProjectile");
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
