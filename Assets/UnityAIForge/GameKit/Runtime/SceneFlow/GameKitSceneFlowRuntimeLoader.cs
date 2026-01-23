using UnityEngine;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// Runtime auto-loader for GameKitSceneFlow prefabs.
    /// Loads all SceneFlow prefabs from Resources/GameKitSceneFlows/ before the first scene loads.
    /// Works in both Editor Play Mode and built applications.
    /// </summary>
    public static class GameKitSceneFlowRuntimeLoader
    {
        private const string RESOURCES_FOLDER = "GameKitSceneFlows";
        private static bool isInitialized = false;
        
        /// <summary>
        /// Called automatically before the first scene loads.
        /// Loads all GameKitSceneFlow prefabs from Resources.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (isInitialized)
            {
                return;
            }
            
            isInitialized = true;
            LoadAllSceneFlowPrefabs();
        }
        
        /// <summary>
        /// Loads all GameKitSceneFlow prefabs from Resources/GameKitSceneFlows/.
        /// Each prefab is instantiated and marked with DontDestroyOnLoad.
        /// </summary>
        private static void LoadAllSceneFlowPrefabs()
        {
            // Load all prefabs from Resources/GameKitSceneFlows/
            var prefabs = Resources.LoadAll<GameObject>(RESOURCES_FOLDER);
            
            if (prefabs.Length == 0)
            {
                Debug.Log($"[GameKitSceneFlow] No SceneFlow prefabs found in Resources/{RESOURCES_FOLDER}/");
                return;
            }
            
            int loadedCount = 0;
            foreach (var prefab in prefabs)
            {
                var sceneFlow = prefab.GetComponent<GameKitSceneFlow>();
                if (sceneFlow != null)
                {
                    // Instantiate and persist across scenes
                    var instance = Object.Instantiate(prefab);
                    instance.name = prefab.name; // Remove "(Clone)" suffix
                    Object.DontDestroyOnLoad(instance);
                    loadedCount++;
                    
                    Debug.Log($"[GameKitSceneFlow] Runtime loaded: {prefab.name} (flowId: {sceneFlow.FlowId})");
                }
                else
                {
                    Debug.LogWarning($"[GameKitSceneFlow] Prefab '{prefab.name}' in Resources/{RESOURCES_FOLDER}/ does not have GameKitSceneFlow component");
                }
            }
            
            if (loadedCount > 0)
            {
                Debug.Log($"[GameKitSceneFlow] Successfully runtime loaded {loadedCount} SceneFlow prefab(s)");
            }
        }
        
        /// <summary>
        /// Manually load all SceneFlow prefabs (useful for hot-reloading).
        /// </summary>
        public static void Reload()
        {
            // Clear existing instances
            var existingFlows = Object.FindObjectsOfType<GameKitSceneFlow>();
            foreach (var flow in existingFlows)
            {
                Object.Destroy(flow.gameObject);
            }
            
            // Reload all prefabs
            isInitialized = false;
            Initialize();
        }
    }
}

