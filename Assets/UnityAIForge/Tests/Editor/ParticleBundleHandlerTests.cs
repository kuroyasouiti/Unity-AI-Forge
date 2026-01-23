using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MCP.Editor.Handlers;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// ParticleBundleHandler unit tests.
    /// Tests particle system creation, modification, and preset application.
    /// </summary>
    [TestFixture]
    public class ParticleBundleHandlerTests
    {
        private ParticleBundleHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new ParticleBundleHandler();
            _createdObjects = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    Undo.ClearUndo(obj);
                    Object.DestroyImmediate(obj);
                }
            }
            _createdObjects.Clear();
        }

        private GameObject CreateTestGameObject(string name)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go;
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnParticleBundle()
        {
            Assert.AreEqual("particleBundle", _handler.Category);
        }

        [Test]
        public void Version_ShouldReturn100()
        {
            Assert.AreEqual("1.0.0", _handler.Version);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("create", operations);
            Assert.Contains("update", operations);
            Assert.Contains("applyPreset", operations);
            Assert.Contains("play", operations);
            Assert.Contains("stop", operations);
            Assert.Contains("pause", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("duplicate", operations);
            Assert.Contains("listPresets", operations);
        }

        #endregion

        #region Create Operation Tests

        [Test]
        public void Execute_Create_ShouldCreateParticleSystem()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestParticle"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("gameObjectPath"));

            var path = result["gameObjectPath"] as string;
            var go = GameObject.Find(path);
            Assert.IsNotNull(go);
            _createdObjects.Add(go);

            var ps = go.GetComponent<ParticleSystem>();
            Assert.IsNotNull(ps);
        }

        [Test]
        public void Execute_Create_WithPreset_ShouldApplyPreset()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestExplosion",
                ["preset"] = "explosion"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("explosion", result["preset"]);

            var path = result["gameObjectPath"] as string;
            var go = GameObject.Find(path);
            _createdObjects.Add(go);

            var ps = go.GetComponent<ParticleSystem>();
            Assert.IsNotNull(ps);
            // Explosion preset should not loop
            Assert.IsFalse(ps.main.loop);
        }

        [Test]
        public void Execute_Create_WithPosition_ShouldSetPosition()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestPositionedParticle",
                ["position"] = new Dictionary<string, object>
                {
                    ["x"] = 5f,
                    ["y"] = 10f,
                    ["z"] = 15f
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var path = result["gameObjectPath"] as string;
            var go = GameObject.Find(path);
            _createdObjects.Add(go);

            Assert.AreEqual(5f, go.transform.position.x, 0.01f);
            Assert.AreEqual(10f, go.transform.position.y, 0.01f);
            Assert.AreEqual(15f, go.transform.position.z, 0.01f);
        }

        [Test]
        public void Execute_Create_FirePreset_ShouldBeLooping()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestFire",
                ["preset"] = "fire"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var path = result["gameObjectPath"] as string;
            var go = GameObject.Find(path);
            _createdObjects.Add(go);

            var ps = go.GetComponent<ParticleSystem>();
            Assert.IsTrue(ps.main.loop);
        }

        #endregion

        #region Update Operation Tests

        [Test]
        public void Execute_Update_ShouldModifyParticleSystem()
        {
            var go = CreateTestGameObject("TestParticle_Update");
            var ps = go.AddComponent<ParticleSystem>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = "TestParticle_Update",
                ["startLifetime"] = 5f,
                ["startSpeed"] = 10f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(5f, ps.main.startLifetime.constant, 0.01f);
            Assert.AreEqual(10f, ps.main.startSpeed.constant, 0.01f);
        }

        [Test]
        public void Execute_Update_NoParticleSystem_ShouldReturnError()
        {
            var go = CreateTestGameObject("TestNoParticle");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = "TestNoParticle",
                ["startLifetime"] = 5f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("error"));
        }

        #endregion

        #region ApplyPreset Operation Tests

        [Test]
        public void Execute_ApplyPreset_ShouldApplyPresetToExisting()
        {
            var go = CreateTestGameObject("TestParticle_ApplyPreset");
            go.AddComponent<ParticleSystem>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "applyPreset",
                ["gameObjectPath"] = "TestParticle_ApplyPreset",
                ["preset"] = "smoke"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        [Test]
        public void Execute_ApplyPreset_NoPreset_ShouldReturnError()
        {
            var go = CreateTestGameObject("TestParticle_NoPreset");
            go.AddComponent<ParticleSystem>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "applyPreset",
                ["gameObjectPath"] = "TestParticle_NoPreset"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion

        #region Play/Stop/Pause Operation Tests

        [Test]
        public void Execute_Play_ShouldStartParticleSystem()
        {
            var go = CreateTestGameObject("TestParticle_Play");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "play",
                ["gameObjectPath"] = "TestParticle_Play"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(ps.isPlaying);
        }

        [Test]
        public void Execute_Stop_ShouldStopParticleSystem()
        {
            var go = CreateTestGameObject("TestParticle_Stop");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Play();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "stop",
                ["gameObjectPath"] = "TestParticle_Stop"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(ps.isStopped);
        }

        [Test]
        public void Execute_Pause_ShouldPauseParticleSystem()
        {
            var go = CreateTestGameObject("TestParticle_Pause");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Play();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "pause",
                ["gameObjectPath"] = "TestParticle_Pause"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(ps.isPaused);
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnParticleProperties()
        {
            var go = CreateTestGameObject("TestParticle_Inspect");
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.maxParticles = 500;

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = "TestParticle_Inspect"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("main"));
            Assert.IsTrue(result.ContainsKey("emission"));
            Assert.IsTrue(result.ContainsKey("shape"));

            var mainDict = result["main"] as Dictionary<string, object>;
            Assert.IsNotNull(mainDict);
            Assert.AreEqual(500, mainDict["maxParticles"]);
        }

        #endregion

        #region Delete Operation Tests

        [Test]
        public void Execute_Delete_ShouldRemoveParticleSystem()
        {
            var go = CreateTestGameObject("TestParticle_Delete");
            go.AddComponent<ParticleSystem>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["gameObjectPath"] = "TestParticle_Delete"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            _createdObjects.Remove(go);
        }

        #endregion

        #region Duplicate Operation Tests

        [Test]
        public void Execute_Duplicate_ShouldCreateCopy()
        {
            var go = CreateTestGameObject("TestParticle_Source");
            go.AddComponent<ParticleSystem>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "duplicate",
                ["gameObjectPath"] = "TestParticle_Source",
                ["newName"] = "TestParticle_Copy"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var copyPath = result["newPath"] as string;
            var copy = GameObject.Find(copyPath);
            Assert.IsNotNull(copy);
            _createdObjects.Add(copy);

            Assert.IsNotNull(copy.GetComponent<ParticleSystem>());
        }

        #endregion

        #region ListPresets Operation Tests

        [Test]
        public void Execute_ListPresets_ShouldReturnPresetList()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "listPresets"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("presets"));

            var presets = result["presets"] as List<Dictionary<string, object>>;
            Assert.IsNotNull(presets);
            Assert.IsTrue(presets.Count >= 10); // Should have multiple presets
        }

        [Test]
        public void Execute_ListPresets_ShouldContainExpectedPresets()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "listPresets"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;
            var presets = result["presets"] as List<Dictionary<string, object>>;
            var presetNames = presets.Select(p => p["name"] as string).ToList();

            Assert.Contains("explosion", presetNames);
            Assert.Contains("fire", presetNames);
            Assert.Contains("smoke", presetNames);
            Assert.Contains("rain", presetNames);
            Assert.Contains("snow", presetNames);
        }

        #endregion
    }
}
