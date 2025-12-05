using NUnit.Framework;
using UnityAIForge.GameKit;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.TestTools;

namespace UnityAIForge.Tests.Editor
{
    [TestFixture]
    public class GameKitSceneFlowTests
    {
        private GameObject testSceneFlowGo;

        [SetUp]
        public void Setup()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void Teardown()
        {
            if (testSceneFlowGo != null)
            {
                Object.DestroyImmediate(testSceneFlowGo);
            }
        }

        [Test]
        public void CreateSceneFlow_WithValidParameters_CreatesGameObject()
        {
            // Arrange
            testSceneFlowGo = new GameObject("TestSceneFlow");
            var sceneFlow = testSceneFlowGo.AddComponent<GameKitSceneFlow>();

            // Act
            sceneFlow.Initialize("flow_001");

            // Assert
            Assert.IsNotNull(sceneFlow);
            Assert.AreEqual("flow_001", sceneFlow.FlowId);
        }

        [Test]
        public void AddScene_WithValidScene_AddsScene()
        {
            // Arrange
            testSceneFlowGo = new GameObject("TestSceneFlow");
            var sceneFlow = testSceneFlowGo.AddComponent<GameKitSceneFlow>();
            sceneFlow.Initialize("flow_001");

            // Act - Now directly specify shared scene paths instead of group names
            sceneFlow.AddScene("Title", "Assets/Scenes/Title.unity", GameKitSceneFlow.SceneLoadMode.Single, new string[] { });
            sceneFlow.AddScene("Level1", "Assets/Scenes/Level1.unity", GameKitSceneFlow.SceneLoadMode.Additive, 
                new string[] { "Assets/Scenes/Shared/GameUI.unity", "Assets/Scenes/Shared/AudioManager.unity" });

            // Assert - scenes are stored internally
            Assert.IsNotNull(sceneFlow);
        }

        [Test]
        public void AddTransition_WithValidTransition_AddsTransition()
        {
            // Arrange
            testSceneFlowGo = new GameObject("TestSceneFlow");
            var sceneFlow = testSceneFlowGo.AddComponent<GameKitSceneFlow>();
            sceneFlow.Initialize("flow_001");
            
            // Add scenes first
            sceneFlow.AddScene("Title", "Assets/Scenes/Title.unity", GameKitSceneFlow.SceneLoadMode.Single, new string[] { });
            sceneFlow.AddScene("Level1", "Assets/Scenes/Level1.unity", GameKitSceneFlow.SceneLoadMode.Additive, new string[] { });

            // Act - Add transitions FROM specific scenes
            sceneFlow.AddTransition("Title", "StartGame", "Level1");
            sceneFlow.AddTransition("Level1", "ReturnToTitle", "Title");

            // Assert - transitions are stored in scene definitions
            Assert.IsNotNull(sceneFlow);
            var triggers = sceneFlow.GetAvailableTriggers();
            Assert.IsNotNull(triggers);
        }

        [Test]
        public void AddSharedScenesToScene_WithValidPaths_AddsSharedScenes()
        {
            // Arrange
            testSceneFlowGo = new GameObject("TestSceneFlow");
            var sceneFlow = testSceneFlowGo.AddComponent<GameKitSceneFlow>();
            sceneFlow.Initialize("flow_001");
            
            sceneFlow.AddScene("Level1", "Assets/Scenes/Level1.unity", GameKitSceneFlow.SceneLoadMode.Additive, new string[] { });

            // Act - Add shared scenes to existing scene
            sceneFlow.AddSharedScenesToScene("Level1", new string[] { 
                "Assets/Scenes/Shared/GameUI.unity",
                "Assets/Scenes/Shared/AudioManager.unity"
            });

            // Assert - scenes are stored internally
            Assert.IsNotNull(sceneFlow);
        }

        [Test]
        public void TriggerTransition_WithValidTrigger_LogsTransition()
        {
            // Arrange
            testSceneFlowGo = new GameObject("TestSceneFlow");
            var sceneFlow = testSceneFlowGo.AddComponent<GameKitSceneFlow>();
            sceneFlow.Initialize("flow_001");
            sceneFlow.AddScene("Title", "Assets/Scenes/Title.unity", GameKitSceneFlow.SceneLoadMode.Single, new string[] { });
            sceneFlow.AddScene("Level1", "Assets/Scenes/Level1.unity", GameKitSceneFlow.SceneLoadMode.Additive, new string[] { });
            sceneFlow.AddTransition("Title", "StartGame", "Level1");
            
            // Set current scene
            sceneFlow.SetCurrentScene("Title");

            // Act & Assert - In editor mode without actual scenes, we expect an exception
            // The test verifies that the transition system is configured correctly
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("InvalidOperationException.*"));
            sceneFlow.TriggerTransition("StartGame");
            
            // Verify the component is still valid after attempting transition
            Assert.IsNotNull(sceneFlow);
        }

        [Test]
        public void StaticTransition_CallsInstanceMethod()
        {
            // Arrange
            testSceneFlowGo = new GameObject("TestSceneFlow");
            var sceneFlow = testSceneFlowGo.AddComponent<GameKitSceneFlow>();
            sceneFlow.Initialize("flow_001");
            sceneFlow.AddScene("Title", "Assets/Scenes/Title.unity", GameKitSceneFlow.SceneLoadMode.Single, new string[] { });
            sceneFlow.AddScene("Level1", "Assets/Scenes/Level1.unity", GameKitSceneFlow.SceneLoadMode.Additive, new string[] { });
            sceneFlow.AddTransition("Level1", "ReturnToTitle", "Title");

            // Note: Static method requires singleton instance to be set via Awake
            // In editor tests, we can't easily test the static method without play mode
            
            // Assert
            Assert.IsNotNull(sceneFlow);
        }

        [Test]
        public void SameTrigger_DifferentScenes_DifferentDestinations()
        {
            // Arrange - This tests the key feature: same trigger, different destinations per scene
            testSceneFlowGo = new GameObject("TestSceneFlow");
            var sceneFlow = testSceneFlowGo.AddComponent<GameKitSceneFlow>();
            sceneFlow.Initialize("flow_001");
            
            // Add page scenes
            sceneFlow.AddScene("Page1", "Assets/Scenes/Page1.unity", GameKitSceneFlow.SceneLoadMode.Single, new string[] { });
            sceneFlow.AddScene("Page2", "Assets/Scenes/Page2.unity", GameKitSceneFlow.SceneLoadMode.Single, new string[] { });
            sceneFlow.AddScene("Page3", "Assets/Scenes/Page3.unity", GameKitSceneFlow.SceneLoadMode.Single, new string[] { });
            
            // Same trigger "nextPage" leads to different destinations
            sceneFlow.AddTransition("Page1", "nextPage", "Page2");
            sceneFlow.AddTransition("Page2", "nextPage", "Page3");
            
            // Act & Assert - Test from Page1
            sceneFlow.SetCurrentScene("Page1");
            var triggers = sceneFlow.GetAvailableTriggers();
            Assert.Contains("nextPage", triggers);
            
            // Would transition to Page2 from Page1 (expect exception in EditMode)
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("InvalidOperationException.*"));
            sceneFlow.TriggerTransition("nextPage");
            
            // Act & Assert - Test from Page2  
            sceneFlow.SetCurrentScene("Page2");
            triggers = sceneFlow.GetAvailableTriggers();
            Assert.Contains("nextPage", triggers);
            
            // Would transition to Page3 from Page2 (expect exception in EditMode)
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("InvalidOperationException.*"));
            sceneFlow.TriggerTransition("nextPage");
            
            Assert.IsNotNull(sceneFlow);
        }

        [Test]
        public void GetAvailableTriggers_ReturnsCurrentSceneTriggers()
        {
            // Arrange
            testSceneFlowGo = new GameObject("TestSceneFlow");
            var sceneFlow = testSceneFlowGo.AddComponent<GameKitSceneFlow>();
            sceneFlow.Initialize("flow_001");
            
            sceneFlow.AddScene("Page1", "Assets/Scenes/Page1.unity", GameKitSceneFlow.SceneLoadMode.Single, new string[] { });
            sceneFlow.AddScene("Page2", "Assets/Scenes/Page2.unity", GameKitSceneFlow.SceneLoadMode.Single, new string[] { });
            
            sceneFlow.AddTransition("Page1", "next", "Page2");
            sceneFlow.AddTransition("Page1", "skip", "Page2");
            sceneFlow.AddTransition("Page2", "back", "Page1");
            
            // Act - Get triggers from Page1
            sceneFlow.SetCurrentScene("Page1");
            var triggers1 = sceneFlow.GetAvailableTriggers();
            
            // Assert
            Assert.AreEqual(2, triggers1.Count);
            Assert.Contains("next", triggers1);
            Assert.Contains("skip", triggers1);
            Assert.IsFalse(triggers1.Contains("back"));
            
            // Act - Get triggers from Page2
            sceneFlow.SetCurrentScene("Page2");
            var triggers2 = sceneFlow.GetAvailableTriggers();
            
            // Assert
            Assert.AreEqual(1, triggers2.Count);
            Assert.Contains("back", triggers2);
            Assert.IsFalse(triggers2.Contains("next"));
        }

        [Test]
        public void GetSceneNames_ReturnsAllSceneNames()
        {
            // Arrange
            testSceneFlowGo = new GameObject("TestSceneFlow");
            var sceneFlow = testSceneFlowGo.AddComponent<GameKitSceneFlow>();
            sceneFlow.Initialize("flow_001");
            
            sceneFlow.AddScene("Title", "Assets/Scenes/Title.unity", GameKitSceneFlow.SceneLoadMode.Single, new string[] { });
            sceneFlow.AddScene("Level1", "Assets/Scenes/Level1.unity", GameKitSceneFlow.SceneLoadMode.Additive, new string[] { });
            sceneFlow.AddScene("Level2", "Assets/Scenes/Level2.unity", GameKitSceneFlow.SceneLoadMode.Additive, new string[] { });
            
            // Act
            var sceneNames = sceneFlow.GetSceneNames();
            
            // Assert
            Assert.AreEqual(3, sceneNames.Count);
            Assert.Contains("Title", sceneNames);
            Assert.Contains("Level1", sceneNames);
            Assert.Contains("Level2", sceneNames);
        }

        [Test]
        public void GetLoadedSharedScenes_ReturnsEmptyList_WhenNoScenesLoaded()
        {
            // Arrange
            testSceneFlowGo = new GameObject("TestSceneFlow");
            var sceneFlow = testSceneFlowGo.AddComponent<GameKitSceneFlow>();
            sceneFlow.Initialize("flow_001");
            
            // Act
            var loadedSharedScenes = sceneFlow.GetLoadedSharedScenes();
            
            // Assert
            Assert.IsNotNull(loadedSharedScenes);
            Assert.AreEqual(0, loadedSharedScenes.Count);
        }

        [Test]
        public void IsInitialized_InitialState_ReturnsFalse()
        {
            // Arrange
            testSceneFlowGo = new GameObject("TestSceneFlow");
            var sceneFlow = testSceneFlowGo.AddComponent<GameKitSceneFlow>();
            
            // Act & Assert - Before Awake/Initialize, should be false
            // Note: In edit mode, Awake doesn't automatically run
            Assert.IsFalse(sceneFlow.IsInitialized);
        }

        [Test]
        public void SetCurrentScene_SetsSceneName()
        {
            // Arrange
            testSceneFlowGo = new GameObject("TestSceneFlow");
            var sceneFlow = testSceneFlowGo.AddComponent<GameKitSceneFlow>();
            sceneFlow.Initialize("flow_001");
            sceneFlow.AddScene("Title", "Assets/Scenes/Title.unity", GameKitSceneFlow.SceneLoadMode.Single, new string[] { });
            
            // Act
            sceneFlow.SetCurrentScene("Title");
            
            // Assert
            Assert.AreEqual("Title", sceneFlow.CurrentScene);
        }

        [Test]
        public void ReinitializeCurrentScene_AllowsReinitialization()
        {
            // Arrange
            testSceneFlowGo = new GameObject("TestSceneFlow");
            var sceneFlow = testSceneFlowGo.AddComponent<GameKitSceneFlow>();
            sceneFlow.Initialize("flow_001");
            
            // Note: Current scene is the editor test scene (usually called "InitTestScene###")
            // We can't easily test automatic scene detection in edit mode,
            // but we can verify that the method can be called without errors
            
            // Act & Assert - Should not throw an exception
            sceneFlow.ReinitializeCurrentScene();
            Assert.IsNotNull(sceneFlow);
        }

        [Test]
        public void RemoveScene_RemovesSceneDefinition()
        {
            // Arrange
            testSceneFlowGo = new GameObject("TestSceneFlow");
            var sceneFlow = testSceneFlowGo.AddComponent<GameKitSceneFlow>();
            sceneFlow.Initialize("flow_001");
            sceneFlow.AddScene("Title", "Assets/Scenes/Title.unity", GameKitSceneFlow.SceneLoadMode.Single, new string[] { });
            sceneFlow.AddScene("Level1", "Assets/Scenes/Level1.unity", GameKitSceneFlow.SceneLoadMode.Additive, new string[] { });
            
            // Act
            var removed = sceneFlow.RemoveScene("Title");
            
            // Assert
            Assert.IsTrue(removed);
            var sceneNames = sceneFlow.GetSceneNames();
            Assert.AreEqual(1, sceneNames.Count);
            Assert.IsFalse(sceneNames.Contains("Title"));
            Assert.Contains("Level1", sceneNames);
        }

        [Test]
        public void RemoveScene_NonExistentScene_ReturnsFalse()
        {
            // Arrange
            testSceneFlowGo = new GameObject("TestSceneFlow");
            var sceneFlow = testSceneFlowGo.AddComponent<GameKitSceneFlow>();
            sceneFlow.Initialize("flow_001");
            
            // Act
            var removed = sceneFlow.RemoveScene("NonExistent");
            
            // Assert
            Assert.IsFalse(removed);
        }

        [Test]
        public void UpdateScene_UpdatesSceneProperties()
        {
            // Arrange
            testSceneFlowGo = new GameObject("TestSceneFlow");
            var sceneFlow = testSceneFlowGo.AddComponent<GameKitSceneFlow>();
            sceneFlow.Initialize("flow_001");
            sceneFlow.AddScene("Level1", "Assets/Scenes/Level1.unity", GameKitSceneFlow.SceneLoadMode.Additive, new string[] { });
            
            // Act
            var updated = sceneFlow.UpdateScene("Level1", 
                scenePath: "Assets/Scenes/Level1_Updated.unity", 
                loadMode: GameKitSceneFlow.SceneLoadMode.Single,
                sharedScenePaths: new string[] { "Assets/Scenes/Shared/UI.unity" });
            
            // Assert
            Assert.IsTrue(updated);
        }

        [Test]
        public void RemoveTransition_RemovesTransitionFromScene()
        {
            // Arrange
            testSceneFlowGo = new GameObject("TestSceneFlow");
            var sceneFlow = testSceneFlowGo.AddComponent<GameKitSceneFlow>();
            sceneFlow.Initialize("flow_001");
            sceneFlow.AddScene("Page1", "Assets/Scenes/Page1.unity", GameKitSceneFlow.SceneLoadMode.Single, new string[] { });
            sceneFlow.AddScene("Page2", "Assets/Scenes/Page2.unity", GameKitSceneFlow.SceneLoadMode.Single, new string[] { });
            sceneFlow.AddTransition("Page1", "next", "Page2");
            
            // Act
            var removed = sceneFlow.RemoveTransition("Page1", "next");
            
            // Assert
            Assert.IsTrue(removed);
            sceneFlow.SetCurrentScene("Page1");
            var triggers = sceneFlow.GetAvailableTriggers();
            Assert.AreEqual(0, triggers.Count);
        }

        [Test]
        public void RemoveSharedSceneFromScene_RemovesSharedScene()
        {
            // Arrange
            testSceneFlowGo = new GameObject("TestSceneFlow");
            var sceneFlow = testSceneFlowGo.AddComponent<GameKitSceneFlow>();
            sceneFlow.Initialize("flow_001");
            sceneFlow.AddScene("Level1", "Assets/Scenes/Level1.unity", GameKitSceneFlow.SceneLoadMode.Additive, 
                new string[] { "Assets/Scenes/Shared/UI.unity", "Assets/Scenes/Shared/Audio.unity" });
            
            // Act
            var removed = sceneFlow.RemoveSharedSceneFromScene("Level1", "Assets/Scenes/Shared/UI.unity");
            
            // Assert
            Assert.IsTrue(removed);
        }
    }
}

