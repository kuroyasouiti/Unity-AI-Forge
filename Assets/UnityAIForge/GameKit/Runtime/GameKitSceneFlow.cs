using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit SceneFlow component: manages scene transitions with scene-centric state machine.
    /// Each scene defines its own transitions and shared scene groups.
    /// </summary>
    [AddComponentMenu("SkillForUnity/GameKit/SceneFlow")]
    public class GameKitSceneFlow : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string flowId;
        
        [Header("Scene Definitions")]
        [Tooltip("Scene definitions with transitions and shared scenes")]
        [SerializeField] private List<SceneDefinition> scenes = new List<SceneDefinition>();
        
        [Header("Current State")]
        [SerializeField] private string currentSceneName;
        [SerializeField] private List<string> loadedSharedScenes = new List<string>();

        private static GameKitSceneFlow instance;
        private Dictionary<string, SceneDefinition> sceneLookup;

        public string FlowId => flowId;
        public string CurrentScene => currentSceneName;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                BuildSceneLookup();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void BuildSceneLookup()
        {
            sceneLookup = new Dictionary<string, SceneDefinition>();
            foreach (var scene in scenes)
            {
                if (!string.IsNullOrEmpty(scene.name))
                {
                    sceneLookup[scene.name] = scene;
                }
            }
        }

        public void Initialize(string id)
        {
            flowId = id;
            BuildSceneLookup();
        }

        /// <summary>
        /// Add a scene definition with its path, load mode, and shared scene paths.
        /// </summary>
        public void AddScene(string name, string scenePath, SceneLoadMode loadMode, string[] sharedScenePaths = null)
        {
            var scene = new SceneDefinition
            {
                name = name,
                scenePath = scenePath,
                loadMode = loadMode,
                sharedScenePaths = new List<string>(sharedScenePaths ?? new string[0]),
                transitions = new List<SceneTransition>()
            };
            
            scenes.Add(scene);
            
            // Update lookup
            if (sceneLookup == null)
                sceneLookup = new Dictionary<string, SceneDefinition>();
            sceneLookup[name] = scene;
        }

        /// <summary>
        /// Remove a scene definition by name.
        /// </summary>
        public bool RemoveScene(string name)
        {
            var scene = scenes.Find(s => s.name == name);
            if (scene == null)
            {
                Debug.LogWarning($"[GameKitSceneFlow] Scene '{name}' not found.");
                return false;
            }

            scenes.Remove(scene);
            
            // Update lookup
            if (sceneLookup != null && sceneLookup.ContainsKey(name))
            {
                sceneLookup.Remove(name);
            }
            
            return true;
        }

        /// <summary>
        /// Update an existing scene definition's properties.
        /// </summary>
        public bool UpdateScene(string name, string scenePath = null, SceneLoadMode? loadMode = null, string[] sharedScenePaths = null)
        {
            if (sceneLookup == null)
                BuildSceneLookup();

            if (!sceneLookup.TryGetValue(name, out var scene))
            {
                Debug.LogWarning($"[GameKitSceneFlow] Scene '{name}' not found.");
                return false;
            }

            if (scenePath != null)
                scene.scenePath = scenePath;
            
            if (loadMode.HasValue)
                scene.loadMode = loadMode.Value;
            
            if (sharedScenePaths != null)
                scene.sharedScenePaths = new List<string>(sharedScenePaths);
            
            return true;
        }

        /// <summary>
        /// Add a transition from a specific scene. The same trigger can lead to different scenes
        /// depending on the current scene (e.g., "nextPage" from Page1 goes to Page2, from Page2 goes to Page3).
        /// </summary>
        public void AddTransition(string fromSceneName, string trigger, string toSceneName)
        {
            // Find the source scene
            var fromScene = scenes.Find(s => s.name == fromSceneName);
            if (fromScene == null)
            {
                Debug.LogWarning($"[GameKitSceneFlow] Source scene '{fromSceneName}' not found. Create scene first.");
                return;
            }

            // Add transition to that scene
            fromScene.transitions.Add(new SceneTransition
            {
                trigger = trigger,
                toScene = toSceneName
            });
        }

        /// <summary>
        /// Remove a transition from a specific scene by trigger name.
        /// </summary>
        public bool RemoveTransition(string fromSceneName, string trigger)
        {
            var fromScene = scenes.Find(s => s.name == fromSceneName);
            if (fromScene == null)
            {
                Debug.LogWarning($"[GameKitSceneFlow] Source scene '{fromSceneName}' not found.");
                return false;
            }

            var transition = fromScene.transitions.Find(t => t.trigger == trigger);
            if (transition == null)
            {
                Debug.LogWarning($"[GameKitSceneFlow] Transition with trigger '{trigger}' not found in scene '{fromSceneName}'.");
                return false;
            }

            fromScene.transitions.Remove(transition);
            return true;
        }

        /// <summary>
        /// Add shared scene paths to an existing scene definition.
        /// </summary>
        public void AddSharedScenesToScene(string sceneName, string[] sharedScenePaths)
        {
            if (sceneLookup == null)
                BuildSceneLookup();

            if (sceneLookup.TryGetValue(sceneName, out var scene))
            {
                foreach (var path in sharedScenePaths)
                {
                    if (!scene.sharedScenePaths.Contains(path))
                    {
                        scene.sharedScenePaths.Add(path);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[GameKitSceneFlow] Scene '{sceneName}' not found. Create scene first.");
            }
        }

        /// <summary>
        /// Remove shared scene paths from an existing scene definition.
        /// </summary>
        public bool RemoveSharedSceneFromScene(string sceneName, string sharedScenePath)
        {
            if (sceneLookup == null)
                BuildSceneLookup();

            if (sceneLookup.TryGetValue(sceneName, out var scene))
            {
                return scene.sharedScenePaths.Remove(sharedScenePath);
            }
            
            Debug.LogWarning($"[GameKitSceneFlow] Scene '{sceneName}' not found.");
            return false;
        }

        /// <summary>
        /// Trigger a scene transition based on trigger name and current scene.
        /// The same trigger can lead to different destinations depending on which scene is active.
        /// </summary>
        public void TriggerTransition(string triggerName)
        {
            if (string.IsNullOrEmpty(currentSceneName))
            {
                Debug.LogWarning($"[GameKitSceneFlow] No current scene set. Cannot trigger transition: {triggerName}");
                return;
            }

            // Get current scene definition
            if (sceneLookup == null)
                BuildSceneLookup();

            if (!sceneLookup.TryGetValue(currentSceneName, out var currentScene))
            {
                Debug.LogWarning($"[GameKitSceneFlow] Current scene '{currentSceneName}' not found in definitions");
                return;
            }

            // Find matching transition from current scene
            foreach (var transition in currentScene.transitions)
            {
                if (transition.trigger == triggerName)
                {
                    Debug.Log($"[GameKitSceneFlow] Trigger '{triggerName}' activated from '{currentSceneName}' → '{transition.toScene}'");
                    StartCoroutine(TransitionToScene(transition.toScene));
                    return;
                }
            }

            Debug.LogWarning($"[GameKitSceneFlow] No transition found for trigger '{triggerName}' from scene '{currentSceneName}'");
        }

        private IEnumerator TransitionToScene(string targetSceneName)
        {
            Debug.Log($"[GameKitSceneFlow] Transitioning from '{currentSceneName}' to '{targetSceneName}'");

            if (sceneLookup == null)
                BuildSceneLookup();

            // Find target scene definition
            if (!sceneLookup.TryGetValue(targetSceneName, out var targetScene))
            {
                Debug.LogError($"[GameKitSceneFlow] Target scene '{targetSceneName}' not found in flow");
                yield break;
            }

            // Get current scene definition
            SceneDefinition currentScene = null;
            if (!string.IsNullOrEmpty(currentSceneName))
            {
                sceneLookup.TryGetValue(currentSceneName, out currentScene);
            }

            // Unload current scene if exists and is additive
            if (currentScene != null && currentScene.loadMode == SceneLoadMode.Additive)
            {
                Debug.Log($"[GameKitSceneFlow] Unloading current scene: {currentScene.scenePath}");
                yield return SceneManager.UnloadSceneAsync(currentScene.scenePath);
            }

            // Determine which shared scenes need to be loaded/unloaded
            var targetSharedScenes = new HashSet<string>(targetScene.sharedScenePaths);

            // Unload shared scenes that are not needed in target
            var scenesToUnload = new List<string>();
            foreach (var loadedScene in loadedSharedScenes)
            {
                if (!targetSharedScenes.Contains(loadedScene))
                {
                    scenesToUnload.Add(loadedScene);
                }
            }

            foreach (var sceneToUnload in scenesToUnload)
            {
                Debug.Log($"[GameKitSceneFlow] Unloading shared scene: {sceneToUnload}");
                yield return SceneManager.UnloadSceneAsync(sceneToUnload);
                loadedSharedScenes.Remove(sceneToUnload);
            }

            // Load target scene
            if (targetScene.loadMode == SceneLoadMode.Single)
            {
                Debug.Log($"[GameKitSceneFlow] Loading scene (Single): {targetScene.scenePath}");
                yield return SceneManager.LoadSceneAsync(targetScene.scenePath, LoadSceneMode.Single);
            }
            else
            {
                Debug.Log($"[GameKitSceneFlow] Loading scene (Additive): {targetScene.scenePath}");
                yield return SceneManager.LoadSceneAsync(targetScene.scenePath, LoadSceneMode.Additive);
            }

            currentSceneName = targetSceneName;

            // Load new shared scenes that aren't already loaded
            foreach (var sharedScenePath in targetSharedScenes)
            {
                if (!loadedSharedScenes.Contains(sharedScenePath))
                {
                    Debug.Log($"[GameKitSceneFlow] Loading shared scene: {sharedScenePath}");
                    yield return SceneManager.LoadSceneAsync(sharedScenePath, LoadSceneMode.Additive);
                    loadedSharedScenes.Add(sharedScenePath);
                }
            }

            Debug.Log($"[GameKitSceneFlow] Transition complete. Current scene: {currentSceneName}");
        }

        public static void Transition(string triggerName)
        {
            if (instance != null)
            {
                instance.TriggerTransition(triggerName);
            }
            else
            {
                Debug.LogError("[GameKitSceneFlow] No SceneFlow instance found");
            }
        }

        /// <summary>
        /// Set the current scene without loading (useful for initialization).
        /// </summary>
        public void SetCurrentScene(string sceneName)
        {
            currentSceneName = sceneName;
            Debug.Log($"[GameKitSceneFlow] Current scene set to: {sceneName}");
        }

        /// <summary>
        /// Get all available trigger names from the current scene.
        /// </summary>
        public List<string> GetAvailableTriggers()
        {
            var triggers = new List<string>();
            
            if (string.IsNullOrEmpty(currentSceneName))
                return triggers;

            if (sceneLookup == null)
                BuildSceneLookup();

            if (sceneLookup.TryGetValue(currentSceneName, out var currentScene))
            {
                foreach (var transition in currentScene.transitions)
                {
                    triggers.Add(transition.trigger);
                }
            }

            return triggers;
        }

        /// <summary>
        /// Get all scene names.
        /// </summary>
        public List<string> GetSceneNames()
        {
            var names = new List<string>();
            foreach (var scene in scenes)
            {
                names.Add(scene.name);
            }
            return names;
        }

        [Serializable]
        public class SceneDefinition
        {
            [Tooltip("Unique name for this scene")]
            public string name;
            
            [Tooltip("Path to scene asset (e.g., Assets/Scenes/MainMenu.unity)")]
            public string scenePath;
            
            [Tooltip("How to load this scene (Single replaces all, Additive adds to existing)")]
            public SceneLoadMode loadMode;
            
            [Tooltip("Shared scene paths to load with this scene (e.g., UI scenes, Audio manager)")]
            public List<string> sharedScenePaths = new List<string>();
            
            [Tooltip("Transitions available from this scene")]
            public List<SceneTransition> transitions = new List<SceneTransition>();
        }

        [Serializable]
        public class SceneTransition
        {
            [Tooltip("Trigger name that activates this transition")]
            public string trigger;
            
            [Tooltip("Destination scene name")]
            public string toScene;
        }

        public enum SceneLoadMode
        {
            Single,
            Additive
        }
    }
}

