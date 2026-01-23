using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MCP.Editor.Handlers.GameKit;
using UnityAIForge.GameKit;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// GameKitAudioHandler unit tests (3-Pillar Architecture - Presentation).
    /// Tests audio manager creation, volume/pitch settings, and audio clip management.
    /// </summary>
    [TestFixture]
    public class GameKitAudioHandlerTests
    {
        private GameKitAudioHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameKitAudioHandler();
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

        private void SetSerializedField(Component component, string fieldName, object value)
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                if (value is string strValue)
                    prop.stringValue = strValue;
                else if (value is int intValue)
                    prop.intValue = intValue;
                else if (value is float floatValue)
                    prop.floatValue = floatValue;
                else if (value is bool boolValue)
                    prop.boolValue = boolValue;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnGamekitAudio()
        {
            Assert.AreEqual("gamekitAudio", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();

            Assert.Contains("create", operations);
            Assert.Contains("update", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("setVolume", operations);
            Assert.Contains("setPitch", operations);
            Assert.Contains("setLoop", operations);
            Assert.Contains("setClip", operations);
            Assert.Contains("findByAudioId", operations);
        }

        #endregion

        #region Create Operation Tests

        [Test]
        public void Execute_Create_ShouldAddAudioComponent()
        {
            var go = CreateTestGameObject("TestAudio");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestAudio",
                ["audioId"] = "test_audio"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var audio = go.GetComponent<GameKitAudio>();
            Assert.IsNotNull(audio);
            Assert.AreEqual("test_audio", audio.AudioId);
        }

        [Test]
        public void Execute_Create_WithoutTargetPath_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        [Test]
        public void Execute_Create_WithVolumeConfig_ShouldSetVolume()
        {
            var go = CreateTestGameObject("TestAudioVolume");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["targetPath"] = "TestAudioVolume",
                ["audioId"] = "volume_test",
                ["volume"] = 0.5f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var audio = go.GetComponent<GameKitAudio>();
            Assert.AreEqual(0.5f, audio.Volume, 0.01f);
        }

        #endregion

        #region Delete Operation Tests

        [Test]
        public void Execute_Delete_ShouldRemoveComponent()
        {
            var go = CreateTestGameObject("TestAudioDelete");
            go.AddComponent<GameKitAudio>();

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["targetPath"] = "TestAudioDelete"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(go.GetComponent<GameKitAudio>());
        }

        #endregion

        #region Inspect Operation Tests

        [Test]
        public void Execute_Inspect_ShouldReturnAudioInfo()
        {
            var go = CreateTestGameObject("TestAudioInspect");
            var audio = go.AddComponent<GameKitAudio>();
            SetSerializedField(audio, "audioId", "inspect_test");
            SetSerializedField(audio, "volume", 0.8f);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["targetPath"] = "TestAudioInspect"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            var audioInfo = result["audio"] as Dictionary<string, object>;
            Assert.IsNotNull(audioInfo);
            Assert.AreEqual("inspect_test", audioInfo["audioId"]);
        }

        #endregion

        #region SetVolume Operation Tests

        [Test]
        public void Execute_SetVolume_ShouldUpdateVolume()
        {
            var go = CreateTestGameObject("TestAudioSetVolume");
            var audio = go.AddComponent<GameKitAudio>();
            SetSerializedField(audio, "audioId", "setvolume_test");
            SetSerializedField(audio, "volume", 1.0f);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "setVolume",
                ["targetPath"] = "TestAudioSetVolume",
                ["volume"] = 0.3f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(0.3f, audio.Volume, 0.01f);
        }

        #endregion

        #region SetLoop Operation Tests

        [Test]
        public void Execute_SetLoop_ShouldUpdateLoopSetting()
        {
            var go = CreateTestGameObject("TestAudioLoop");
            var audio = go.AddComponent<GameKitAudio>();
            SetSerializedField(audio, "audioId", "loop_test");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "setLoop",
                ["targetPath"] = "TestAudioLoop",
                ["loop"] = true
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion
    }
}
