using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class GameObjectCommandHandlerTests
    {
        private GameObjectCommandHandler _handler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameObjectCommandHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown()
        {
            _tracker.Dispose();
        }

        [Test]
        public void Category_ReturnsGameObject()
        {
            Assert.AreEqual("gameObjectManage", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("create", ops);
            Assert.Contains("delete", ops);
            Assert.Contains("move", ops);
            Assert.Contains("rename", ops);
            Assert.Contains("update", ops);
            Assert.Contains("duplicate", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("findMultiple", ops);
            Assert.Contains("deleteMultiple", ops);
            Assert.Contains("inspectMultiple", ops);
        }

        [Test]
        public void Execute_NullPayload_ReturnsError()
        {
            var result = _handler.Execute(null);
            TestUtilities.AssertError(result);
        }

        [Test]
        public void Execute_MissingOperation_ReturnsError()
        {
            var result = _handler.Execute(new Dictionary<string, object> { ["name"] = "Test" });
            TestUtilities.AssertError(result);
        }

        [Test]
        public void Execute_UnsupportedOperation_ReturnsError()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("nonExistent"));
            TestUtilities.AssertError(result, "not supported");
        }

        [Test]
        public void Create_Simple_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("create",
                ("name", "TestGO")));
            TestUtilities.AssertSuccess(result);

            var go = GameObject.Find("TestGO");
            Assert.IsNotNull(go);
            _tracker.Track(go);
        }

        [Test]
        public void Create_WithParent_ReturnsSuccess()
        {
            var parent = _tracker.Create("Parent");
            var result = _handler.Execute(TestUtilities.CreatePayload("create",
                ("name", "Child"),
                ("parentPath", "Parent")));
            TestUtilities.AssertSuccess(result);

            var child = parent.transform.Find("Child");
            Assert.IsNotNull(child);
        }

        [Test]
        public void Inspect_ValidObject_ReturnsDetails()
        {
            var go = _tracker.Create("InspectTarget");
            var result = _handler.Execute(TestUtilities.CreatePayload("inspect",
                ("gameObjectPath", "InspectTarget"))) as Dictionary<string, object>;
            TestUtilities.AssertSuccess(result);
            Assert.AreEqual("InspectTarget", result["name"]);
        }

        [Test]
        public void Rename_ValidObject_ChangesName()
        {
            var go = _tracker.Create("OldName");
            var result = _handler.Execute(TestUtilities.CreatePayload("rename",
                ("gameObjectPath", "OldName"),
                ("name", "NewName")));
            TestUtilities.AssertSuccess(result);
            Assert.AreEqual("NewName", go.name);
        }

        [Test]
        public void Delete_ValidObject_DestroysIt()
        {
            var go = new GameObject("ToDelete");
            var result = _handler.Execute(TestUtilities.CreatePayload("delete",
                ("gameObjectPath", "ToDelete")));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void Duplicate_ValidObject_CreatesClone()
        {
            _tracker.Create("Original");
            var result = _handler.Execute(TestUtilities.CreatePayload("duplicate",
                ("gameObjectPath", "Original")));
            TestUtilities.AssertSuccess(result);

            var clone = GameObject.Find("Original(Clone)") ?? GameObject.Find("Original (1)");
            if (clone != null) _tracker.Track(clone);
        }

        [Test]
        public void Update_Tag_Succeeds()
        {
            var go = _tracker.Create("TagTarget");
            var result = _handler.Execute(TestUtilities.CreatePayload("update",
                ("gameObjectPath", "TagTarget"),
                ("tag", "MainCamera")));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void FindMultiple_Pattern_ReturnsMatches()
        {
            _tracker.Create("FindMe_A");
            _tracker.Create("FindMe_B");
            _tracker.Create("Other");
            var result = _handler.Execute(TestUtilities.CreatePayload("findMultiple",
                ("pattern", "FindMe*"))) as Dictionary<string, object>;
            TestUtilities.AssertSuccess(result);
        }
    }
}
