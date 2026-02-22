using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace MCP.Editor.Utilities.GraphAnalysis
{
    /// <summary>
    /// Analyzes scene integrity by detecting missing scripts, null references,
    /// broken UnityEvents, and broken prefab connections.
    /// </summary>
    public class SceneIntegrityAnalyzer
    {
        /// <summary>
        /// Represents a single integrity issue found in the scene.
        /// </summary>
        public class IntegrityIssue
        {
            public string Type { get; set; }
            public string Severity { get; set; }
            public string GameObjectPath { get; set; }
            public int ComponentIndex { get; set; } = -1;
            public string ComponentType { get; set; }
            public string FieldName { get; set; }
            public string Message { get; set; }

            public Dictionary<string, object> ToDictionary()
            {
                var dict = new Dictionary<string, object>
                {
                    ["type"] = Type,
                    ["severity"] = Severity,
                    ["gameObjectPath"] = GameObjectPath,
                    ["message"] = Message
                };
                if (ComponentIndex >= 0)
                    dict["componentIndex"] = ComponentIndex;
                if (!string.IsNullOrEmpty(ComponentType))
                    dict["componentType"] = ComponentType;
                if (!string.IsNullOrEmpty(FieldName))
                    dict["fieldName"] = FieldName;
                return dict;
            }
        }

        /// <summary>
        /// Find GameObjects with missing (null) MonoBehaviour scripts.
        /// Uses GameObjectUtility API for reliable detection (runtime GetComponents skips missing scripts).
        /// </summary>
        public List<IntegrityIssue> FindMissingScripts(string rootPath = null)
        {
            var issues = new List<IntegrityIssue>();
            var gameObjects = GetTargetGameObjects(rootPath);

            foreach (var go in gameObjects)
            {
                int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (missingCount > 0)
                {
                    issues.Add(new IntegrityIssue
                    {
                        Type = "missingScript",
                        Severity = "error",
                        GameObjectPath = GetGameObjectPath(go),
                        Message = $"{missingCount} missing MonoBehaviour script(s)"
                    });
                }
            }

            return issues;
        }

        /// <summary>
        /// Remove all missing MonoBehaviour scripts from GameObjects in the scene or subtree.
        /// Uses GameObjectUtility.RemoveMonoBehavioursWithMissingScript for reliable removal.
        /// Returns the list of GameObjects that were cleaned.
        /// </summary>
        public (List<IntegrityIssue> removed, int totalRemoved) RemoveMissingScripts(string rootPath = null)
        {
            var removed = new List<IntegrityIssue>();
            int totalRemoved = 0;
            var gameObjects = GetTargetGameObjects(rootPath);

            foreach (var go in gameObjects)
            {
                int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (missingCount > 0)
                {
                    Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    totalRemoved += missingCount;

                    removed.Add(new IntegrityIssue
                    {
                        Type = "missingScript",
                        Severity = "info",
                        GameObjectPath = GetGameObjectPath(go),
                        Message = $"Removed {missingCount} missing MonoBehaviour script(s)"
                    });
                }
            }

            return (removed, totalRemoved);
        }

        /// <summary>
        /// Find SerializedProperty object references that point to destroyed objects.
        /// Detects cases where objectReferenceInstanceIDValue != 0 but objectReferenceValue == null.
        /// </summary>
        public List<IntegrityIssue> FindNullReferences(string rootPath = null)
        {
            var issues = new List<IntegrityIssue>();
            var gameObjects = GetTargetGameObjects(rootPath);

            foreach (var go in gameObjects)
            {
                var components = go.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null) continue;

                    try
                    {
                        var so = new SerializedObject(component);
                        var iterator = so.GetIterator();

                        while (iterator.NextVisible(true))
                        {
                            if (iterator.propertyType != SerializedPropertyType.ObjectReference)
                                continue;

                            // Skip the m_Script field — it's covered by FindMissingScripts
                            if (iterator.name == "m_Script")
                                continue;

                            if (iterator.objectReferenceInstanceIDValue != 0 &&
                                iterator.objectReferenceValue == null)
                            {
                                issues.Add(new IntegrityIssue
                                {
                                    Type = "nullReference",
                                    Severity = "warning",
                                    GameObjectPath = GetGameObjectPath(go),
                                    ComponentType = component.GetType().Name,
                                    FieldName = iterator.propertyPath,
                                    Message = $"Null object reference in {component.GetType().Name}.{iterator.propertyPath}"
                                });
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Some components might not be serializable
                    }
                }
            }

            return issues;
        }

        /// <summary>
        /// Find UnityEvent persistent listeners with null targets or missing methods.
        /// </summary>
        public List<IntegrityIssue> FindBrokenEvents(string rootPath = null)
        {
            var issues = new List<IntegrityIssue>();
            var gameObjects = GetTargetGameObjects(rootPath);

            foreach (var go in gameObjects)
            {
                var components = go.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null) continue;

                    try
                    {
                        var so = new SerializedObject(component);
                        var iterator = so.GetIterator();

                        while (iterator.NextVisible(true))
                        {
                            if (iterator.propertyType != SerializedPropertyType.Generic)
                                continue;

                            // Look for UnityEvent persistent calls
                            var callsProp = iterator.FindPropertyRelative("m_PersistentCalls.m_Calls");
                            if (callsProp == null || !callsProp.isArray)
                                continue;

                            for (int i = 0; i < callsProp.arraySize; i++)
                            {
                                var call = callsProp.GetArrayElementAtIndex(i);
                                var targetProp = call.FindPropertyRelative("m_Target");
                                var methodProp = call.FindPropertyRelative("m_MethodName");

                                if (targetProp == null) continue;

                                var target = targetProp.objectReferenceValue;
                                var methodName = methodProp?.stringValue;

                                // Null target
                                if (target == null && targetProp.objectReferenceInstanceIDValue != 0)
                                {
                                    issues.Add(new IntegrityIssue
                                    {
                                        Type = "brokenEvent",
                                        Severity = "error",
                                        GameObjectPath = GetGameObjectPath(go),
                                        ComponentType = component.GetType().Name,
                                        FieldName = iterator.propertyPath,
                                        Message = $"UnityEvent listener {i} has null target in {component.GetType().Name}.{iterator.propertyPath}"
                                    });
                                    continue;
                                }

                                // Target exists but method is missing
                                if (target != null && !string.IsNullOrEmpty(methodName))
                                {
                                    if (!HasMethod(target, methodName))
                                    {
                                        issues.Add(new IntegrityIssue
                                        {
                                            Type = "brokenEvent",
                                            Severity = "error",
                                            GameObjectPath = GetGameObjectPath(go),
                                            ComponentType = component.GetType().Name,
                                            FieldName = iterator.propertyPath,
                                            Message = $"UnityEvent listener {i} method '{methodName}' not found on {target.GetType().Name} in {component.GetType().Name}.{iterator.propertyPath}"
                                        });
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Some components might not be serializable
                    }
                }
            }

            return issues;
        }

        /// <summary>
        /// Find prefab instances with missing assets or disconnected status.
        /// </summary>
        public List<IntegrityIssue> FindBrokenPrefabs(string rootPath = null)
        {
            var issues = new List<IntegrityIssue>();
            var gameObjects = GetTargetGameObjects(rootPath);

            foreach (var go in gameObjects)
            {
                if (!PrefabUtility.IsPartOfPrefabInstance(go))
                    continue;

                var status = PrefabUtility.GetPrefabInstanceStatus(go);

                if (status == PrefabInstanceStatus.MissingAsset)
                {
                    issues.Add(new IntegrityIssue
                    {
                        Type = "brokenPrefab",
                        Severity = "error",
                        GameObjectPath = GetGameObjectPath(go),
                        Message = "Prefab instance has missing asset"
                    });
                }
                else if (status == PrefabInstanceStatus.Disconnected)
                {
                    issues.Add(new IntegrityIssue
                    {
                        Type = "brokenPrefab",
                        Severity = "warning",
                        GameObjectPath = GetGameObjectPath(go),
                        Message = "Prefab instance is disconnected from source"
                    });
                }
            }

            return issues;
        }

        /// <summary>
        /// Run all integrity checks and return combined results with summary.
        /// </summary>
        public (List<IntegrityIssue> issues, Dictionary<string, int> summary) FindAllIssues(string rootPath = null)
        {
            var allIssues = new List<IntegrityIssue>();
            var summary = new Dictionary<string, int>
            {
                ["missingScripts"] = 0,
                ["nullReferences"] = 0,
                ["brokenEvents"] = 0,
                ["brokenPrefabs"] = 0
            };

            var missingScripts = FindMissingScripts(rootPath);
            summary["missingScripts"] = missingScripts.Count;
            allIssues.AddRange(missingScripts);

            var nullRefs = FindNullReferences(rootPath);
            summary["nullReferences"] = nullRefs.Count;
            allIssues.AddRange(nullRefs);

            var brokenEvents = FindBrokenEvents(rootPath);
            summary["brokenEvents"] = brokenEvents.Count;
            allIssues.AddRange(brokenEvents);

            var brokenPrefabs = FindBrokenPrefabs(rootPath);
            summary["brokenPrefabs"] = brokenPrefabs.Count;
            allIssues.AddRange(brokenPrefabs);

            return (allIssues, summary);
        }

        #region Private Helpers

        private List<GameObject> GetTargetGameObjects(string rootPath)
        {
            var result = new List<GameObject>();

            if (!string.IsNullOrEmpty(rootPath))
            {
                var root = FindGameObjectByPath(rootPath);
                if (root == null)
                    throw new InvalidOperationException($"GameObject not found: {rootPath}");
                CollectGameObjectsRecursive(root, result);
            }
            else
            {
                var activeScene = SceneManager.GetActiveScene();
                foreach (var root in activeScene.GetRootGameObjects())
                {
                    CollectGameObjectsRecursive(root, result);
                }
            }

            return result;
        }

        private void CollectGameObjectsRecursive(GameObject go, List<GameObject> list)
        {
            list.Add(go);
            foreach (Transform child in go.transform)
            {
                CollectGameObjectsRecursive(child.gameObject, list);
            }
        }

        private string GetGameObjectPath(GameObject go)
        {
            var path = "/" + go.name;
            var current = go.transform.parent;
            while (current != null)
            {
                path = "/" + current.name + path;
                current = current.parent;
            }
            return path;
        }

        private GameObject FindGameObjectByPath(string path)
        {
            var cleanPath = path.TrimStart('/');
            var parts = cleanPath.Split('/');

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.name == parts[0])
                    {
                        if (parts.Length == 1) return root;

                        var current = root.transform;
                        for (int j = 1; j < parts.Length; j++)
                        {
                            current = current.Find(parts[j]);
                            if (current == null) break;
                        }
                        if (current != null) return current.gameObject;
                    }
                }
            }

            return null;
        }

        private bool HasMethod(UnityEngine.Object target, string methodName)
        {
            if (target == null || string.IsNullOrEmpty(methodName))
                return false;

            var type = target.GetType();
            // Check public and non-public instance methods
            var method = type.GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null) return true;

            // Also check properties (UnityEvents can target property setters)
            var property = type.GetProperty(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.GetSetMethod(true) != null) return true;

            return false;
        }

        #endregion
    }
}
