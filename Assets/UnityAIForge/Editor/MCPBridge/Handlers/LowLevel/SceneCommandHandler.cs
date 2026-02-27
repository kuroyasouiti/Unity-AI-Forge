using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCP.Editor.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Scene management command handler.
    /// Handles scene creation, loading, saving, deletion, duplication, inspection, and build settings.
    /// </summary>
    public class SceneCommandHandler : BaseCommandHandler
    {
        #region ICommandHandler Implementation
        
        public override string Category => "sceneManage";
        
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "create",
            "load",
            "save",
            "delete",
            "duplicate",
            "inspect"
        };
        
        #endregion
        
        #region BaseCommandHandler Overrides
        
        protected override bool RequiresCompilationWait(string operation)
        {
            // inspect は読み取り専用
            return operation != "inspect";
        }
        
        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateScene(payload),
                "load" => LoadScene(payload),
                "save" => SaveScene(payload),
                "delete" => DeleteScene(payload),
                "duplicate" => DuplicateScene(payload),
                "inspect" => InspectScene(payload),
                _ => throw new InvalidOperationException($"Unknown scene operation: {operation}. Build settings operations have been moved to unity_projectSettings_crud tool.")
            };
        }
        
        #endregion
        
        #region Scene Operations
        
        /// <summary>
        /// Creates a new scene.
        /// </summary>
        private object CreateScene(Dictionary<string, object> payload)
        {
            var additive = GetBool(payload, "additive");
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects, 
                additive ? NewSceneMode.Additive : NewSceneMode.Single
            );
            
            var scenePath = GetString(payload, "scenePath");
            if (!string.IsNullOrEmpty(scenePath))
            {
                ValidateScenePath(scenePath);
                EnsureDirectoryExists(scenePath);
                
                if (!EditorSceneManager.SaveScene(scene, scenePath))
                {
                    throw new InvalidOperationException($"Failed to save scene to: {scenePath}");
                }
            }
            
            return CreateSuccessResponse(
                ("scenePath", scene.path),
                ("sceneName", scene.name),
                ("isDirty", scene.isDirty),
                ("additive", additive)
            );
        }
        
        /// <summary>
        /// Loads an existing scene.
        /// </summary>
        private object LoadScene(Dictionary<string, object> payload)
        {
            var scenePath = GetString(payload, "scenePath");
            if (string.IsNullOrEmpty(scenePath))
            {
                throw new InvalidOperationException("scenePath is required for load operation");
            }
            
            ValidateScenePath(scenePath);
            
            if (!File.Exists(scenePath))
            {
                throw new InvalidOperationException($"Scene file does not exist: {scenePath}");
            }
            
            var additive = GetBool(payload, "additive");
            var mode = additive ? OpenSceneMode.Additive : OpenSceneMode.Single;
            
            var scene = EditorSceneManager.OpenScene(scenePath, mode);
            
            return CreateSuccessResponse(
                ("scenePath", scene.path),
                ("sceneName", scene.name),
                ("loadMode", additive ? "additive" : "single"),
                ("isLoaded", scene.isLoaded),
                ("rootCount", scene.rootCount)
            );
        }
        
        /// <summary>
        /// Saves the current scene(s).
        /// </summary>
        private object SaveScene(Dictionary<string, object> payload)
        {
            var includeOpenScenes = GetBool(payload, "includeOpenScenes");
            var savedScenes = new List<string>();
            
            if (includeOpenScenes)
            {
                // Save all open scenes
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.isDirty || string.IsNullOrEmpty(scene.path))
                    {
                        if (string.IsNullOrEmpty(scene.path))
                        {
                            throw new InvalidOperationException(
                                $"Scene '{scene.name}' has not been saved yet. Please provide a scenePath."
                            );
                        }
                        
                        if (EditorSceneManager.SaveScene(scene))
                        {
                            savedScenes.Add(scene.path);
                        }
                    }
                }
            }
            else
            {
                // Save only the active scene
                var scene = SceneManager.GetActiveScene();
                if (string.IsNullOrEmpty(scene.path))
                {
                    throw new InvalidOperationException(
                        "Active scene has not been saved yet. Please provide a scenePath."
                    );
                }
                
                if (EditorSceneManager.SaveScene(scene))
                {
                    savedScenes.Add(scene.path);
                }
            }
            
            return CreateSuccessResponse(
                ("savedScenes", savedScenes),
                ("count", savedScenes.Count)
            );
        }
        
        /// <summary>
        /// Deletes a scene file.
        /// </summary>
        private object DeleteScene(Dictionary<string, object> payload)
        {
            var scenePath = GetString(payload, "scenePath");
            if (string.IsNullOrEmpty(scenePath))
            {
                throw new InvalidOperationException("scenePath is required for delete operation");
            }
            
            ValidateScenePath(scenePath);
            
            if (!File.Exists(scenePath))
            {
                throw new InvalidOperationException($"Scene file does not exist: {scenePath}");
            }
            
            // Check if scene is currently open
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.path == scenePath)
                {
                    throw new InvalidOperationException(
                        $"Cannot delete currently open scene: {scenePath}. Please close it first."
                    );
                }
            }
            
            if (!AssetDatabase.DeleteAsset(scenePath))
            {
                throw new InvalidOperationException($"Failed to delete scene: {scenePath}");
            }
            
            AssetDatabase.Refresh();
            
            return CreateSuccessResponse(
                ("scenePath", scenePath),
                ("message", "Scene deleted successfully")
            );
        }
        
        /// <summary>
        /// Duplicates a scene file.
        /// </summary>
        private object DuplicateScene(Dictionary<string, object> payload)
        {
            var scenePath = GetString(payload, "scenePath");
            if (string.IsNullOrEmpty(scenePath))
            {
                throw new InvalidOperationException("scenePath is required for duplicate operation");
            }
            
            ValidateScenePath(scenePath);
            
            if (!File.Exists(scenePath))
            {
                throw new InvalidOperationException($"Scene file does not exist: {scenePath}");
            }
            
            var newSceneName = GetString(payload, "newSceneName");
            if (string.IsNullOrEmpty(newSceneName))
            {
                newSceneName = Path.GetFileNameWithoutExtension(scenePath) + "_Copy";
            }
            
            var directory = Path.GetDirectoryName(scenePath);
            var newScenePath = Path.Combine(directory, newSceneName + ".unity");
            
            if (File.Exists(newScenePath))
            {
                throw new InvalidOperationException($"Scene already exists: {newScenePath}");
            }
            
            if (!AssetDatabase.CopyAsset(scenePath, newScenePath))
            {
                throw new InvalidOperationException($"Failed to duplicate scene from {scenePath} to {newScenePath}");
            }
            
            AssetDatabase.Refresh();
            
            return CreateSuccessResponse(
                ("sourceScenePath", scenePath),
                ("newScenePath", newScenePath),
                ("newSceneName", newSceneName)
            );
        }
        
        /// <summary>
        /// Inspects the current scene and returns hierarchy information.
        /// </summary>
        private object InspectScene(Dictionary<string, object> payload)
        {
            var scene = SceneManager.GetActiveScene();
            var includeHierarchy = GetBool(payload, "includeHierarchy", true);
            var includeComponents = GetBool(payload, "includeComponents", false);
            var filter = GetString(payload, "filter");
            
            var result = new Dictionary<string, object>
            {
                ["success"] = true,
                ["scenePath"] = scene.path,
                ["sceneName"] = scene.name,
                ["isLoaded"] = scene.isLoaded,
                ["isDirty"] = scene.isDirty,
                ["rootCount"] = scene.rootCount,
                ["buildIndex"] = scene.buildIndex
            };
            
            if (includeHierarchy)
            {
                var rootObjects = scene.GetRootGameObjects();
                var hierarchyList = new List<Dictionary<string, object>>();
                
                foreach (var rootGo in rootObjects)
                {
                    // Apply filter if provided
                    if (!string.IsNullOrEmpty(filter) && !MatchesFilter(rootGo.name, filter))
                    {
                        continue;
                    }
                    
                    var goInfo = new Dictionary<string, object>
                    {
                        ["name"] = rootGo.name,
                        ["active"] = rootGo.activeSelf,
                        ["tag"] = rootGo.tag,
                        ["layer"] = LayerMask.LayerToName(rootGo.layer),
                        ["childCount"] = rootGo.transform.childCount
                    };
                    
                    if (includeComponents)
                    {
                        var components = rootGo.GetComponents<Component>();
                        goInfo["components"] = components
                            .Where(c => c != null)
                            .Select(c => c.GetType().Name)
                            .ToList();
                    }
                    
                    // Include direct children names
                    var childNames = new List<string>();
                    for (int i = 0; i < rootGo.transform.childCount; i++)
                    {
                        childNames.Add(rootGo.transform.GetChild(i).name);
                    }
                    goInfo["children"] = childNames;
                    
                    hierarchyList.Add(goInfo);
                }
                
                result["hierarchy"] = hierarchyList;
            }
            
            return result;
        }
        
        #endregion

        #region Helper Methods
        
        /// <summary>
        /// Validates that a scene path is valid.
        /// </summary>
        private void ValidateScenePath(string path)
        {
            if (!path.StartsWith("Assets/"))
            {
                throw new InvalidOperationException("Scene path must start with 'Assets/'");
            }
            
            if (!path.EndsWith(".unity"))
            {
                throw new InvalidOperationException("Scene path must end with '.unity'");
            }
            
            if (path.Contains(".."))
            {
                throw new InvalidOperationException("Scene path cannot contain '..'");
            }
        }
        
        /// <summary>
        /// Ensures the directory for a file path exists.
        /// </summary>
        private void EnsureDirectoryExists(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        
        /// <summary>
        /// Checks if a name matches a wildcard filter.
        /// </summary>
        private bool MatchesFilter(string name, string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                return true;
            }
            
            // Simple wildcard matching (* and ?)
            var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(filter)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";
            
            return System.Text.RegularExpressions.Regex.IsMatch(name, regexPattern);
        }
        
        #endregion
    }
}

