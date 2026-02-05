using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace MCP.Editor.Utilities.GraphAnalysis
{
    /// <summary>
    /// Analyzes object references within a Unity scene.
    /// </summary>
    public class SceneReferenceAnalyzer
    {
        private readonly Dictionary<int, SceneObjectNode> _nodeCache = new Dictionary<int, SceneObjectNode>();
        private readonly HashSet<string> _processedReferences = new HashSet<string>();

        /// <summary>
        /// Analyze all references in the current scene or a specific scene.
        /// </summary>
        public SceneReferenceResult AnalyzeScene(string scenePath = null, bool includeHierarchy = true, bool includeEvents = true)
        {
            var result = new SceneReferenceResult();

            // Get root GameObjects
            GameObject[] rootObjects;
            if (string.IsNullOrEmpty(scenePath))
            {
                var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                result.ScenePath = activeScene.path;
                rootObjects = activeScene.GetRootGameObjects();
            }
            else
            {
                result.ScenePath = scenePath;
                // Try to find the scene if loaded
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                {
                    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                    if (scene.path == scenePath)
                    {
                        rootObjects = scene.GetRootGameObjects();
                        goto processScene;
                    }
                }
                throw new InvalidOperationException($"Scene not loaded: {scenePath}");
            }

            processScene:
            // Build node cache
            foreach (var root in rootObjects)
            {
                ProcessGameObjectRecursive(root, result, includeHierarchy, includeEvents);
            }

            // Find orphans (nodes with no incoming edges)
            var referencedIds = new HashSet<string>(result.Edges.Select(e => e.Target));
            var orphans = result.Nodes
                .Where(n => !referencedIds.Contains(n.Id) && !IsRootObject(n))
                .Select(n => n.Id)
                .ToList();
            result.Orphans = orphans;

            return result;
        }

        /// <summary>
        /// Analyze references for a specific GameObject.
        /// </summary>
        public SceneReferenceResult AnalyzeObject(string objectPath, bool includeChildren = true, bool includeEvents = true)
        {
            var result = new SceneReferenceResult();
            var targetGo = FindGameObjectByPath(objectPath);
            if (targetGo == null)
            {
                throw new InvalidOperationException($"GameObject not found: {objectPath}");
            }

            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            result.ScenePath = activeScene.path;

            // First pass: build node cache for the entire scene to find references
            var rootObjects = activeScene.GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                CacheGameObjectRecursive(root);
            }

            // Process the target object
            ProcessGameObject(targetGo, result, includeChildren, includeEvents);

            if (includeChildren)
            {
                foreach (Transform child in targetGo.transform)
                {
                    ProcessGameObjectRecursive(child.gameObject, result, true, includeEvents);
                }
            }

            return result;
        }

        /// <summary>
        /// Find all objects that reference the specified object.
        /// </summary>
        public SceneReferenceResult FindReferencesTo(string objectPath)
        {
            var result = new SceneReferenceResult();
            var targetGo = FindGameObjectByPath(objectPath);
            if (targetGo == null)
            {
                throw new InvalidOperationException($"GameObject not found: {objectPath}");
            }

            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            result.ScenePath = activeScene.path;

            // Build full scene graph
            var fullResult = AnalyzeScene(null, true, true);

            // Filter edges that point to the target
            var targetId = GetGameObjectPath(targetGo);
            var targetInstanceId = targetGo.GetInstanceID();

            foreach (var edge in fullResult.Edges)
            {
                if (edge.Target == targetId || edge.Target == targetGo.name)
                {
                    result.AddEdge(edge);
                    var sourceNode = fullResult.GetNode(edge.Source);
                    if (sourceNode != null)
                    {
                        result.AddNode(sourceNode);
                    }
                }
            }

            // Add the target node
            var targetNode = fullResult.GetNode(targetId);
            if (targetNode != null)
            {
                result.AddNode(targetNode);
            }

            return result;
        }

        /// <summary>
        /// Find all objects that the specified object references.
        /// </summary>
        public SceneReferenceResult FindReferencesFrom(string objectPath)
        {
            var result = new SceneReferenceResult();
            var targetGo = FindGameObjectByPath(objectPath);
            if (targetGo == null)
            {
                throw new InvalidOperationException($"GameObject not found: {objectPath}");
            }

            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            result.ScenePath = activeScene.path;

            // Build full scene graph
            var fullResult = AnalyzeScene(null, true, true);

            // Filter edges that originate from the target
            var targetId = GetGameObjectPath(targetGo);

            foreach (var edge in fullResult.Edges)
            {
                if (edge.Source == targetId)
                {
                    result.AddEdge(edge);
                    var targetNode = fullResult.GetNode(edge.Target);
                    if (targetNode != null)
                    {
                        result.AddNode(targetNode);
                    }
                }
            }

            // Add the source node
            var sourceNode = fullResult.GetNode(targetId);
            if (sourceNode != null)
            {
                result.AddNode(sourceNode);
            }

            return result;
        }

        /// <summary>
        /// Find orphan objects (not referenced by anything).
        /// </summary>
        public SceneReferenceResult FindOrphans()
        {
            var fullResult = AnalyzeScene(null, false, true);
            var result = new SceneReferenceResult
            {
                ScenePath = fullResult.ScenePath
            };

            // Find nodes with no incoming edges (except root objects)
            var referencedIds = new HashSet<string>(fullResult.Edges.Select(e => e.Target));

            foreach (var node in fullResult.Nodes)
            {
                if (!referencedIds.Contains(node.Id) && !IsRootObject(node))
                {
                    result.AddNode(node);
                }
            }

            result.Orphans = result.Nodes.Select(n => n.Id).ToList();
            return result;
        }

        #region Private Methods

        private void ProcessGameObjectRecursive(GameObject go, SceneReferenceResult result, bool includeHierarchy, bool includeEvents)
        {
            ProcessGameObject(go, result, includeHierarchy, includeEvents);

            foreach (Transform child in go.transform)
            {
                ProcessGameObjectRecursive(child.gameObject, result, includeHierarchy, includeEvents);
            }
        }

        private void ProcessGameObject(GameObject go, SceneReferenceResult result, bool includeHierarchy, bool includeEvents)
        {
            var node = CreateNodeForGameObject(go);
            result.AddNode(node);

            // Add hierarchy edge (parent-child)
            if (includeHierarchy && go.transform.parent != null)
            {
                var parentPath = GetGameObjectPath(go.transform.parent.gameObject);
                var childPath = node.Id;
                var edge = new SceneReferenceEdge(parentPath, childPath, "hierarchy_child");
                result.AddEdge(edge);
            }

            // Analyze component references
            var components = go.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component == null) continue;
                AnalyzeComponentReferences(component, node.Id, result, includeEvents);
            }
        }

        private void AnalyzeComponentReferences(Component component, string sourcePath, SceneReferenceResult result, bool includeEvents)
        {
            var componentType = component.GetType();
            var componentTypeName = componentType.Name;

            try
            {
                var serializedObject = new SerializedObject(component);
                var iterator = serializedObject.GetIterator();

                while (iterator.NextVisible(true))
                {
                    // Check for object references
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        var refObject = iterator.objectReferenceValue;
                        if (refObject != null)
                        {
                            ProcessObjectReference(refObject, sourcePath, componentTypeName, iterator.name, result);
                        }
                    }
                    // Check for UnityEvents
                    else if (includeEvents && iterator.propertyType == SerializedPropertyType.Generic)
                    {
                        // UnityEvents have m_PersistentCalls property
                        if (iterator.name == "m_PersistentCalls" || iterator.type.Contains("UnityEvent"))
                        {
                            AnalyzeUnityEvent(iterator, sourcePath, componentTypeName, result);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Some components might not be serializable, skip them
            }
        }

        private void ProcessObjectReference(UnityEngine.Object refObject, string sourcePath, string componentTypeName, string fieldName, SceneReferenceResult result)
        {
            string targetPath = null;
            string targetComponent = null;

            if (refObject is GameObject go)
            {
                targetPath = GetGameObjectPath(go);
                // Ensure target node exists
                var targetNode = CreateNodeForGameObject(go);
                result.AddNode(targetNode);
            }
            else if (refObject is Component comp)
            {
                targetPath = GetGameObjectPath(comp.gameObject);
                targetComponent = comp.GetType().Name;
                // Ensure target node exists
                var targetNode = CreateNodeForGameObject(comp.gameObject);
                result.AddNode(targetNode);
            }

            if (!string.IsNullOrEmpty(targetPath) && targetPath != sourcePath)
            {
                var edgeKey = $"{sourcePath}|{targetPath}|{componentTypeName}|{fieldName}";
                if (!_processedReferences.Contains(edgeKey))
                {
                    _processedReferences.Add(edgeKey);

                    var edge = new SceneReferenceEdge(sourcePath, targetPath, "component_reference")
                    {
                        SourceComponent = componentTypeName,
                        SourceField = fieldName
                    };
                    if (!string.IsNullOrEmpty(targetComponent))
                    {
                        edge.TargetComponent = targetComponent;
                    }
                    result.AddEdge(edge);
                }
            }
        }

        private void AnalyzeUnityEvent(SerializedProperty eventProp, string sourcePath, string componentTypeName, SceneReferenceResult result)
        {
            // Navigate to m_PersistentCalls.m_Calls array
            var callsProp = eventProp.FindPropertyRelative("m_Calls");
            if (callsProp == null || !callsProp.isArray) return;

            for (int i = 0; i < callsProp.arraySize; i++)
            {
                var callProp = callsProp.GetArrayElementAtIndex(i);
                var targetProp = callProp.FindPropertyRelative("m_Target");
                var methodProp = callProp.FindPropertyRelative("m_MethodName");

                if (targetProp != null && targetProp.objectReferenceValue != null)
                {
                    var targetObj = targetProp.objectReferenceValue;
                    var methodName = methodProp?.stringValue ?? "Unknown";
                    string targetPath = null;

                    if (targetObj is GameObject go)
                    {
                        targetPath = GetGameObjectPath(go);
                        var targetNode = CreateNodeForGameObject(go);
                        result.AddNode(targetNode);
                    }
                    else if (targetObj is Component comp)
                    {
                        targetPath = GetGameObjectPath(comp.gameObject);
                        var targetNode = CreateNodeForGameObject(comp.gameObject);
                        result.AddNode(targetNode);
                    }

                    if (!string.IsNullOrEmpty(targetPath) && targetPath != sourcePath)
                    {
                        var edge = new SceneReferenceEdge(sourcePath, targetPath, "unity_event")
                        {
                            SourceComponent = componentTypeName,
                            EventName = methodName
                        };
                        result.AddEdge(edge);
                    }
                }
            }
        }

        private void CacheGameObjectRecursive(GameObject go)
        {
            var node = CreateNodeForGameObject(go);
            _nodeCache[go.GetInstanceID()] = node as SceneObjectNode;

            foreach (Transform child in go.transform)
            {
                CacheGameObjectRecursive(child.gameObject);
            }
        }

        private SceneObjectNode CreateNodeForGameObject(GameObject go)
        {
            var instanceId = go.GetInstanceID();
            if (_nodeCache.TryGetValue(instanceId, out var cached))
            {
                return cached;
            }

            var path = GetGameObjectPath(go);
            var node = new SceneObjectNode(path, path)
            {
                InstanceId = instanceId,
                Type = "GameObject"
            };

            // Get components
            var components = go.GetComponents<Component>();
            node.Components = components
                .Where(c => c != null)
                .Select(c => c.GetType().Name)
                .ToList();

            // Check if prefab instance
            if (PrefabUtility.IsPartOfPrefabInstance(go))
            {
                node.IsPrefabInstance = true;
                node.Type = "PrefabInstance";
                var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(go);
                if (prefabAsset != null)
                {
                    node.PrefabAsset = AssetDatabase.GetAssetPath(prefabAsset);
                }
            }

            _nodeCache[instanceId] = node;
            return node;
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
            // Handle paths with or without leading slash
            var cleanPath = path.TrimStart('/');
            var parts = cleanPath.Split('/');

            // Find in all loaded scenes
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
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

        private bool IsRootObject(GraphNode node)
        {
            if (node is SceneObjectNode sceneNode)
            {
                var path = sceneNode.Path ?? sceneNode.Id;
                // Root objects have only one slash at the beginning
                return path.Count(c => c == '/') == 1;
            }
            return false;
        }

        #endregion
    }
}
