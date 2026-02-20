using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;
using MCP.Editor.Interfaces;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class CommandHandlerFactoryTests
    {
        [SetUp]
        public void SetUp()
        {
            CommandHandlerFactory.Clear();
            CommandHandlerInitializer.ResetInitializationState();
        }

        [TearDown]
        public void TearDown()
        {
            // Re-initialize for other tests
            CommandHandlerFactory.Clear();
            CommandHandlerInitializer.ResetInitializationState();
        }

        private class StubHandler : BaseCommandHandler
        {
            private readonly string _category;
            public override string Category => _category;
            public override IEnumerable<string> SupportedOperations => new[] { "test" };

            public StubHandler(string category = "stub")
            {
                _category = category;
            }

            protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
            {
                return CreateSuccessResponse();
            }
        }

        [Test]
        public void Register_ValidHandler_CanBeRetrieved()
        {
            CommandHandlerFactory.Initialize();
            var handler = new StubHandler();
            CommandHandlerFactory.Register("testTool", handler);
            Assert.IsTrue(CommandHandlerFactory.IsRegistered("testTool"));
        }

        [Test]
        public void Register_NullToolName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CommandHandlerFactory.Register(null, new StubHandler()));
        }

        [Test]
        public void Register_EmptyToolName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CommandHandlerFactory.Register("", new StubHandler()));
        }

        [Test]
        public void Register_NullHandler_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CommandHandlerFactory.Register("testTool", null));
        }

        [Test]
        public void GetHandler_RegisteredTool_ReturnsHandler()
        {
            // Initialize first so GetHandler won't trigger re-init (which calls Clear)
            CommandHandlerFactory.Initialize();
            var handler = new StubHandler();
            CommandHandlerFactory.Register("testTool", handler);
            var retrieved = CommandHandlerFactory.GetHandler("testTool");
            Assert.AreSame(handler, retrieved);
        }

        [Test]
        public void GetHandler_UnregisteredTool_InitializesFirst()
        {
            // GetHandler calls Initialize() which calls CommandHandlerInitializer
            // After initialization, known handlers should be registered
            var handler = CommandHandlerFactory.GetHandler("gameObjectManage");
            Assert.IsNotNull(handler);
        }

        [Test]
        public void TryGetHandler_RegisteredTool_ReturnsTrue()
        {
            CommandHandlerFactory.Initialize();
            CommandHandlerFactory.Register("testTool", new StubHandler());
            Assert.IsTrue(CommandHandlerFactory.TryGetHandler("testTool", out var handler));
            Assert.IsNotNull(handler);
        }

        [Test]
        public void TryGetHandler_UnregisteredToolAfterInit_ReturnsFalse()
        {
            CommandHandlerFactory.Initialize();
            Assert.IsFalse(CommandHandlerFactory.TryGetHandler("nonExistentTool12345", out var handler));
            Assert.IsNull(handler);
        }

        [Test]
        public void Clear_RemovesAllHandlers()
        {
            CommandHandlerFactory.Register("testTool", new StubHandler());
            CommandHandlerFactory.Clear();
            Assert.IsFalse(CommandHandlerFactory.IsRegistered("testTool"));
        }

        [Test]
        public void GetRegisteredToolNames_ReturnsAllRegisteredNames()
        {
            CommandHandlerFactory.Initialize();
            CommandHandlerFactory.Register("tool1", new StubHandler("cat1"));
            CommandHandlerFactory.Register("tool2", new StubHandler("cat2"));
            var names = CommandHandlerFactory.GetRegisteredToolNames().ToList();
            Assert.Contains("tool1", names);
            Assert.Contains("tool2", names);
        }

        [Test]
        public void GetStatistics_ReturnsValidStats()
        {
            CommandHandlerFactory.Register("testTool", new StubHandler());
            var stats = CommandHandlerFactory.GetStatistics();
            Assert.IsNotNull(stats);
            Assert.AreEqual(1, stats["totalHandlers"]);
            Assert.IsInstanceOf<List<Dictionary<string, object>>>(stats["registeredHandlers"]);
        }

        [Test]
        public void Initialize_RegistersAllExpectedHandlers()
        {
            CommandHandlerFactory.Initialize();
            var names = CommandHandlerFactory.GetRegisteredToolNames().ToList();

            // Verify key handlers from all phases are registered
            Assert.Contains("pingUnityEditor", names, "Utility handler missing");
            Assert.Contains("sceneManage", names, "LowLevel handler missing");
            Assert.Contains("gameObjectManage", names, "LowLevel handler missing");
            Assert.Contains("componentManage", names, "LowLevel handler missing");
            Assert.Contains("transformBatch", names, "MidLevel handler missing");
            Assert.Contains("gamekitUICommand", names, "GameKit handler missing");
            Assert.Contains("classDependencyGraph", names, "HighLevel handler missing");
        }

        [Test]
        public void Initialize_Registers47Handlers()
        {
            CommandHandlerFactory.Initialize();
            var stats = CommandHandlerFactory.GetStatistics();
            Assert.AreEqual(47, stats["totalHandlers"],
                $"Expected 47 handlers but got {stats["totalHandlers"]}. " +
                $"Registered: {string.Join(", ", CommandHandlerFactory.GetRegisteredToolNames())}");
        }
    }
}
