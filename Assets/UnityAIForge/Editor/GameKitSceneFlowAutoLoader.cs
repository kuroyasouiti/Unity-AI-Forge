using UnityEditor;
using UnityEngine;
using UnityAIForge.GameKit;

namespace MCP.Editor
{
    /// <summary>
    /// Automatically loads GameKitSceneFlow prefabs when entering Play Mode.
    /// Prefabs are loaded from Resources/GameKitSceneFlows/ and instantiated with DontDestroyOnLoad.
    /// </summary>
    [InitializeOnLoad]
    public static class GameKitSceneFlowAutoLoader
    {
        private const string RESOURCES_FOLDER = "GameKitSceneFlows";
        
        static GameKitSceneFlowAutoLoader()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                LoadAllSceneFlowPrefabs();
            }
        }
        
        /// <summary>
        /// Loads all GameKitSceneFlow prefabs from Resources/GameKitSceneFlows/.
        /// Called automatically when entering Play Mode.
        /// </summary>
        private static void LoadAllSceneFlowPrefabs()
        {
            // Load all prefabs from Resources/GameKitSceneFlows/
            var prefabs = Resources.LoadAll<GameObject>(RESOURCES_FOLDER);
            
            if (prefabs.Length == 0)
            {
                Debug.Log("[GameKitSceneFlow] No SceneFlow prefabs found in Resources/GameKitSceneFlows/");
                return;
            }
            
            int loadedCount = 0;
            foreach (var prefab in prefabs)
            {
                var sceneFlow = prefab.GetComponent<GameKitSceneFlow>();
                if (sceneFlow != null)
                {
                    // Check if already loaded (avoid duplicates)
                    if (GameObject.Find(prefab.name) == null)
                    {
                        var instance = Object.Instantiate(prefab);
                        instance.name = prefab.name; // Remove "(Clone)" suffix
                        Object.DontDestroyOnLoad(instance);
                        loadedCount++;
                        Debug.Log($"[GameKitSceneFlow] Auto-loaded: {prefab.name} (flowId: {sceneFlow.FlowId})");
                    }
                }
                else
                {
                    Debug.LogWarning($"[GameKitSceneFlow] Prefab '{prefab.name}' in Resources/{RESOURCES_FOLDER}/ does not have GameKitSceneFlow component");
                }
            }
            
            if (loadedCount > 0)
            {
                Debug.Log($"[GameKitSceneFlow] Successfully auto-loaded {loadedCount} SceneFlow prefab(s)");
            }
        }
        
        /// <summary>
        /// Creates the Resources/GameKitSceneFlows directory if it doesn't exist.
        /// </summary>
        [MenuItem("Tools/Unity-AI-Forge/GameKit/Create SceneFlows Directory")]
        public static void CreateSceneFlowsDirectory()
        {
            var resourcesPath = "Assets/Resources";
            var sceneFlowsPath = "Assets/Resources/GameKitSceneFlows";
            
            // Create Resources folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
                Debug.Log($"Created folder: {resourcesPath}");
            }
            
            // Create GameKitSceneFlows folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder(sceneFlowsPath))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "GameKitSceneFlows");
                Debug.Log($"Created folder: {sceneFlowsPath}");
            }
            else
            {
                Debug.Log($"Folder already exists: {sceneFlowsPath}");
            }
            
            AssetDatabase.Refresh();
        }
    }
}

