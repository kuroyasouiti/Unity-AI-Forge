using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class TilemapBundleHandlerTests
    {
        private TilemapBundleHandler _handler;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _handler = new TilemapBundleHandler();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown() => _tracker.Dispose();

        [Test]
        public void Category_ReturnsTilemapBundle()
        {
            Assert.AreEqual("tilemapBundle", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("createTilemap", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("setTile", ops);
            Assert.Contains("clearAllTiles", ops);
        }

        [Test]
        public void Execute_NullPayload_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(null));
        }

        [Test]
        public void Execute_UnsupportedOperation_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(TestUtilities.CreatePayload("nonExistent")), "not supported");
        }

        [Test]
        public void CreateTilemap_Default_CreatesGridAndTilemap()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("createTilemap",
                ("name", "TestTilemap"))) as Dictionary<string, object>;
            TestUtilities.AssertSuccess(result);

            Assert.IsTrue(result.ContainsKey("path"));
            Assert.IsTrue(result.ContainsKey("gridPath"));

            // Find and track the created grid (parent of tilemap)
            var gridPath = result["gridPath"].ToString();
            var gridGo = GameObject.Find(gridPath);
            Assert.IsNotNull(gridGo, "Grid GameObject should exist");
            Assert.IsNotNull(gridGo.GetComponent<Grid>(), "Grid component should exist");
            _tracker.Track(gridGo);

            // Verify tilemap has correct components
            var tilemapPath = result["path"].ToString();
            var tilemapGo = GameObject.Find(tilemapPath);
            Assert.IsNotNull(tilemapGo, "Tilemap GameObject should exist");
            Assert.IsNotNull(tilemapGo.GetComponent<Tilemap>(), "Tilemap component should exist");
            Assert.IsNotNull(tilemapGo.GetComponent<TilemapRenderer>(), "TilemapRenderer should exist");
        }

        [Test]
        public void Inspect_CreatedTilemap_ReturnsDetails()
        {
            var createResult = _handler.Execute(TestUtilities.CreatePayload("createTilemap",
                ("name", "InspectMap"))) as Dictionary<string, object>;
            TestUtilities.AssertSuccess(createResult);

            var gridGo = GameObject.Find(createResult["gridPath"].ToString());
            if (gridGo != null) _tracker.Track(gridGo);

            var result = _handler.Execute(TestUtilities.CreatePayload("inspect",
                ("tilemapPath", createResult["path"].ToString()))) as Dictionary<string, object>;
            TestUtilities.AssertSuccess(result);
            Assert.IsTrue(result.ContainsKey("tilemap"));
        }
    }
}
