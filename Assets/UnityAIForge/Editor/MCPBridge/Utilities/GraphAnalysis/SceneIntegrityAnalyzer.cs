using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
            public string Suggestion { get; set; }

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
                if (!string.IsNullOrEmpty(Suggestion))
                    dict["suggestion"] = Suggestion;
                return dict;
            }
        }

        /// <summary>
        /// Find GameObjects with missing (null) MonoBehaviour scripts.
        /// Uses GameObjectUtility API for reliable detection (runtime GetComponents skips missing scripts).
        /// </summary>
        public List<IntegrityIssue> FindMissingScripts(string rootPath = null)
        {
            return FindMissingScripts(GetTargetGameObjects(rootPath));
        }

        /// <summary>
        /// Find GameObjects with missing scripts from a provided list.
        /// </summary>
        public List<IntegrityIssue> FindMissingScripts(List<GameObject> gameObjects)
        {
            var issues = new List<IntegrityIssue>();

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
            return FindNullReferences(GetTargetGameObjects(rootPath));
        }

        /// <summary>
        /// Find null references from a provided list of GameObjects.
        /// </summary>
        public List<IntegrityIssue> FindNullReferences(List<GameObject> gameObjects)
        {
            var issues = new List<IntegrityIssue>();

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
            return FindBrokenEvents(GetTargetGameObjects(rootPath));
        }

        /// <summary>
        /// Find broken events from a provided list of GameObjects.
        /// </summary>
        public List<IntegrityIssue> FindBrokenEvents(List<GameObject> gameObjects)
        {
            var issues = new List<IntegrityIssue>();

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
        /// Find type mismatches in object reference fields.
        /// Detects where the assigned object's type doesn't match the field's declared type.
        /// </summary>
        public List<IntegrityIssue> FindTypeMismatches(string rootPath = null)
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
                        var componentType = component.GetType();
                        var so = new SerializedObject(component);
                        var iterator = so.GetIterator();

                        while (iterator.NextVisible(true))
                        {
                            if (iterator.propertyType != SerializedPropertyType.ObjectReference)
                                continue;
                            if (iterator.name == "m_Script")
                                continue;
                            if (iterator.objectReferenceValue == null)
                                continue;

                            // Skip nested paths inside UnityEvents (m_PersistentCalls) and
                            // collection elements (Array.data[]) — comparing the root field type
                            // against a nested element produces false positives.
                            var path = iterator.propertyPath;
                            if (path.Contains(".m_PersistentCalls.") || path.Contains(".Array.data["))
                                continue;

                            // Extract root field name from propertyPath
                            var rootFieldName = path.Split('.')[0];
                            var fieldInfo = componentType.GetField(rootFieldName,
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                            if (fieldInfo == null) continue;

                            if (!fieldInfo.FieldType.IsAssignableFrom(iterator.objectReferenceValue.GetType()))
                            {
                                issues.Add(new IntegrityIssue
                                {
                                    Type = "typeMismatch",
                                    Severity = "warning",
                                    GameObjectPath = GetGameObjectPath(go),
                                    ComponentType = componentType.Name,
                                    FieldName = iterator.propertyPath,
                                    Message = $"Type mismatch in {componentType.Name}.{iterator.propertyPath}: " +
                                              $"expected {fieldInfo.FieldType.Name}, got {iterator.objectReferenceValue.GetType().Name}"
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
        /// Generate fix suggestions for null reference issues by searching for compatible objects in the scene.
        /// </summary>
        public void GenerateSuggestions(List<IntegrityIssue> nullRefIssues)
        {
            foreach (var issue in nullRefIssues)
            {
                if (issue.Type != "nullReference" || string.IsNullOrEmpty(issue.ComponentType) ||
                    string.IsNullOrEmpty(issue.FieldName))
                    continue;

                try
                {
                    // Find the GameObject and component
                    var go = FindGameObjectByPath(issue.GameObjectPath);
                    if (go == null) continue;

                    var component = go.GetComponent(issue.ComponentType);
                    if (component == null) continue;

                    var rootFieldName = issue.FieldName.Split('.')[0];
                    var fieldInfo = component.GetType().GetField(rootFieldName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (fieldInfo == null) continue;

                    // Search scene for objects of the expected type
                    var expectedType = fieldInfo.FieldType;
                    if (typeof(Component).IsAssignableFrom(expectedType))
                    {
                        var candidates = UnityEngine.Object.FindObjectsOfType(expectedType);
                        if (candidates.Length > 0 && candidates.Length <= 5)
                        {
                            var paths = new List<string>();
                            foreach (var candidate in candidates)
                            {
                                if (candidate is Component comp)
                                {
                                    paths.Add(GetGameObjectPath(comp.gameObject));
                                }
                            }
                            issue.Suggestion = $"Possible candidates ({expectedType.Name}): {string.Join(", ", paths)}";
                        }
                        else if (candidates.Length > 5)
                        {
                            issue.Suggestion = $"Found {candidates.Length} {expectedType.Name} instances in scene";
                        }
                    }
                }
                catch (Exception)
                {
                    // Skip suggestion generation on error
                }
            }
        }

        /// <summary>
        /// Check a prefab asset for integrity issues (missing scripts, null refs, broken events).
        /// </summary>
        public (List<IntegrityIssue> issues, Dictionary<string, int> summary) CheckPrefabAsset(string prefabPath)
        {
            var summary = new Dictionary<string, int>
            {
                ["missingScripts"] = 0,
                ["nullReferences"] = 0,
                ["brokenEvents"] = 0
            };

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var gameObjects = new List<GameObject>();
                CollectGameObjectsRecursive(root, gameObjects);

                var missingScripts = FindMissingScripts(gameObjects);
                summary["missingScripts"] = missingScripts.Count;

                var nullRefs = FindNullReferences(gameObjects);
                summary["nullReferences"] = nullRefs.Count;

                var brokenEvents = FindBrokenEvents(gameObjects);
                summary["brokenEvents"] = brokenEvents.Count;

                var allIssues = new List<IntegrityIssue>();
                allIssues.AddRange(missingScripts);
                allIssues.AddRange(nullRefs);
                allIssues.AddRange(brokenEvents);

                return (allIssues, summary);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
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
                ["brokenPrefabs"] = 0,
                ["typeMismatches"] = 0,
                ["canvasGroupIssues"] = 0,
                ["semanticRefIssues"] = 0,
                ["requiredFieldIssues"] = 0,
                ["uiOverflowIssues"] = 0,
                ["uiOverlapIssues"] = 0,
                ["nullAssetIssues"] = 0
            };

            var missingScripts = FindMissingScripts(rootPath);
            summary["missingScripts"] = missingScripts.Count;
            allIssues.AddRange(missingScripts);

            var nullRefs = FindNullReferences(rootPath);
            summary["nullReferences"] = nullRefs.Count;
            GenerateSuggestions(nullRefs);
            allIssues.AddRange(nullRefs);

            var brokenEvents = FindBrokenEvents(rootPath);
            summary["brokenEvents"] = brokenEvents.Count;
            allIssues.AddRange(brokenEvents);

            var brokenPrefabs = FindBrokenPrefabs(rootPath);
            summary["brokenPrefabs"] = brokenPrefabs.Count;
            allIssues.AddRange(brokenPrefabs);

            var typeMismatches = FindTypeMismatches(rootPath);
            summary["typeMismatches"] = typeMismatches.Count;
            allIssues.AddRange(typeMismatches);

            var canvasGroupIssues = FindCanvasGroupIssues(rootPath);
            summary["canvasGroupIssues"] = canvasGroupIssues.Count;
            allIssues.AddRange(canvasGroupIssues);

            var semanticRefIssues = FindReferenceSemanticsIssues(rootPath);
            summary["semanticRefIssues"] = semanticRefIssues.Count;
            allIssues.AddRange(semanticRefIssues);

            var requiredFieldIssues = FindRequiredFieldIssues(rootPath);
            summary["requiredFieldIssues"] = requiredFieldIssues.Count;
            allIssues.AddRange(requiredFieldIssues);

            var uiOverflowIssues = FindUIOverflowIssues(rootPath);
            summary["uiOverflowIssues"] = uiOverflowIssues.Count;
            allIssues.AddRange(uiOverflowIssues);

            var uiOverlapIssues = FindUIOverlapIssues(rootPath);
            summary["uiOverlapIssues"] = uiOverlapIssues.Count;
            allIssues.AddRange(uiOverlapIssues);

            var nullAssetIssues = FindNullAssetFieldIssues();
            summary["nullAssetIssues"] = nullAssetIssues.Count;
            allIssues.AddRange(nullAssetIssues);

            var touchTargetIssues = FindTouchTargetIssues(rootPath);
            summary["touchTargetIssues"] = touchTargetIssues.Count;
            allIssues.AddRange(touchTargetIssues);

            var eventSystemIssues = FindEventSystemIssues(rootPath);
            summary["eventSystemIssues"] = eventSystemIssues.Count;
            allIssues.AddRange(eventSystemIssues);

            var textOverflowIssues = FindTextOverflowIssues(rootPath);
            summary["textOverflowIssues"] = textOverflowIssues.Count;
            allIssues.AddRange(textOverflowIssues);

            return (allIssues, summary);
        }

        /// <summary>
        /// Find CanvasGroup parent-child alpha conflicts and mismatched CanvasGroup references.
        /// Detects cases where a parent CanvasGroup with alpha=0 blocks a child CanvasGroup,
        /// and SerializedField references pointing to child CanvasGroups hidden by a parent.
        /// </summary>
        public List<IntegrityIssue> FindCanvasGroupIssues(string rootPath = null)
        {
            var issues = new List<IntegrityIssue>();
            var gameObjects = GetTargetGameObjects(rootPath);

            foreach (var go in gameObjects)
            {
                var cg = go.GetComponent<CanvasGroup>();
                if (cg == null) continue;

                // Check: parent CanvasGroup alpha=0 blocks this child CanvasGroup
                var parent = go.transform.parent;
                while (parent != null)
                {
                    var parentCg = parent.GetComponent<CanvasGroup>();
                    if (parentCg != null && parentCg.alpha < 0.01f && cg.alpha > 0.01f)
                    {
                        issues.Add(new IntegrityIssue
                        {
                            Type = "canvasGroupConflict",
                            Severity = "warning",
                            GameObjectPath = GetGameObjectPath(go),
                            ComponentType = "CanvasGroup",
                            Message = $"Parent '{parent.name}' CanvasGroup(alpha={parentCg.alpha:F2}) blocks child '{go.name}' CanvasGroup(alpha={cg.alpha:F2})",
                            Suggestion = $"If '{go.name}' should be visible, set parent '{parent.name}' CanvasGroup alpha > 0 or move visibility control to the parent."
                        });
                        break;
                    }
                    parent = parent.parent;
                }
            }

            // Check: SerializedField<CanvasGroup> references that point to child instead of parent
            foreach (var go in gameObjects)
            {
                var components = go.GetComponents<Component>();
                foreach (var comp in components)
                {
                    if (comp == null || comp is CanvasGroup) continue;
                    try
                    {
                        var so = new SerializedObject(comp);
                        var iter = so.GetIterator();
                        while (iter.NextVisible(true))
                        {
                            if (iter.propertyType != SerializedPropertyType.ObjectReference) continue;
                            if (iter.name == "m_Script") continue;
                            var refObj = iter.objectReferenceValue;
                            if (refObj == null || !(refObj is CanvasGroup refCg)) continue;

                            // Check: is there a parent CanvasGroup with alpha=0 above the referenced one?
                            var refGo = refCg.gameObject;
                            var ancestor = refGo.transform.parent;
                            while (ancestor != null)
                            {
                                var ancestorCg = ancestor.GetComponent<CanvasGroup>();
                                if (ancestorCg != null && ancestorCg.alpha < 0.01f)
                                {
                                    issues.Add(new IntegrityIssue
                                    {
                                        Type = "canvasGroupRefMismatch",
                                        Severity = "error",
                                        GameObjectPath = GetGameObjectPath(go),
                                        ComponentType = comp.GetType().Name,
                                        FieldName = iter.propertyPath,
                                        Message = $"{comp.GetType().Name}.{iter.propertyPath} references CanvasGroup on '{refGo.name}', but ancestor '{ancestor.name}' has CanvasGroup(alpha=0) that blocks rendering",
                                        Suggestion = $"Change reference to point to '{ancestor.name}' CanvasGroup instead"
                                    });
                                    break;
                                }
                                ancestor = ancestor.parent;
                            }
                        }
                    }
                    catch { }
                }
            }

            return issues;
        }

        /// <summary>
        /// Find semantic reference issues: references to inactive GameObjects, self-references.
        /// Detects logical inconsistencies that pass compilation but cause runtime problems.
        /// </summary>
        public List<IntegrityIssue> FindReferenceSemanticsIssues(string rootPath = null)
        {
            var issues = new List<IntegrityIssue>();
            var gameObjects = GetTargetGameObjects(rootPath);

            foreach (var go in gameObjects)
            {
                var components = go.GetComponents<Component>();
                foreach (var comp in components)
                {
                    if (comp == null) continue;
                    try
                    {
                        var so = new SerializedObject(comp);
                        var iter = so.GetIterator();
                        while (iter.NextVisible(true))
                        {
                            if (iter.propertyType != SerializedPropertyType.ObjectReference) continue;
                            if (iter.name == "m_Script") continue;
                            var refObj = iter.objectReferenceValue;
                            if (refObj == null) continue;

                            // Rule: disabledTarget — reference to inactive GameObject
                            if (refObj is Component refComp && !refComp.gameObject.activeInHierarchy)
                            {
                                issues.Add(new IntegrityIssue
                                {
                                    Type = "semanticRef_disabledTarget",
                                    Severity = "warning",
                                    GameObjectPath = GetGameObjectPath(go),
                                    ComponentType = comp.GetType().Name,
                                    FieldName = iter.propertyPath,
                                    Message = $"{comp.GetType().Name}.{iter.propertyPath} references inactive GameObject '{refComp.gameObject.name}'"
                                });
                            }
                            else if (refObj is GameObject refGo2 && !refGo2.activeInHierarchy)
                            {
                                issues.Add(new IntegrityIssue
                                {
                                    Type = "semanticRef_disabledTarget",
                                    Severity = "warning",
                                    GameObjectPath = GetGameObjectPath(go),
                                    ComponentType = comp.GetType().Name,
                                    FieldName = iter.propertyPath,
                                    Message = $"{comp.GetType().Name}.{iter.propertyPath} references inactive GameObject '{refGo2.name}'"
                                });
                            }

                            // Rule: selfReference — component references itself
                            if (refObj == comp)
                            {
                                issues.Add(new IntegrityIssue
                                {
                                    Type = "semanticRef_selfReference",
                                    Severity = "info",
                                    GameObjectPath = GetGameObjectPath(go),
                                    ComponentType = comp.GetType().Name,
                                    FieldName = iter.propertyPath,
                                    Message = $"{comp.GetType().Name}.{iter.propertyPath} references itself"
                                });
                            }
                        }
                    }
                    catch { }
                }
            }
            return issues;
        }

        /// <summary>
        /// Find SerializedFields that are null (unset) but used in code without null guards.
        /// Only checks user scripts (Assembly-CSharp) and skips Unity internal fields.
        /// </summary>
        public List<IntegrityIssue> FindRequiredFieldIssues(string rootPath = null)
        {
            var issues = new List<IntegrityIssue>();
            var gameObjects = GetTargetGameObjects(rootPath);
            var sourceCache = new Dictionary<Type, string>();

            foreach (var go in gameObjects)
            {
                var components = go.GetComponents<Component>();
                foreach (var comp in components)
                {
                    if (comp == null) continue;
                    var compType = comp.GetType();
                    // Skip Unity standard components
                    if (compType.Namespace != null && compType.Namespace.StartsWith("UnityEngine")) continue;
                    if (compType.Namespace != null && compType.Namespace.StartsWith("UnityEditor")) continue;
                    if (compType.Namespace != null && compType.Namespace.StartsWith("TMPro")) continue;

                    try
                    {
                        var so = new SerializedObject(comp);
                        var iter = so.GetIterator();
                        while (iter.NextVisible(true))
                        {
                            if (iter.propertyType != SerializedPropertyType.ObjectReference) continue;
                            if (iter.name == "m_Script") continue;
                            // Skip Unity internal fields (m_ prefix on Unity standard components)
                            if (iter.name.StartsWith("m_") && IsUnityInternalField(compType, iter.name)) continue;
                            if (iter.objectReferenceValue != null) continue; // Already set
                            if (iter.objectReferenceInstanceIDValue != 0) continue; // Broken ref, caught by nullReferences

                            var fieldName = iter.propertyPath.Split('.')[0];
                            var (isUsed, hasNullGuard) = CheckFieldUsageInSource(compType, fieldName, sourceCache);

                            if (isUsed)
                            {
                                var fieldTypeName = GetFieldTypeName(compType, fieldName);
                                issues.Add(new IntegrityIssue
                                {
                                    Type = "requiredField",
                                    Severity = hasNullGuard ? "info" : "warning",
                                    GameObjectPath = GetGameObjectPath(go),
                                    ComponentType = compType.Name,
                                    FieldName = fieldName,
                                    Message = hasNullGuard
                                        ? $"{compType.Name}.{fieldName} is null (used with null guard)"
                                        : $"{compType.Name}.{fieldName} is null but used without null guard",
                                    Suggestion = $"Set {fieldTypeName} reference in Inspector"
                                });
                            }
                        }
                    }
                    catch (Exception) { }
                }
            }
            return issues;
        }

        /// <summary>
        /// Find UI layout overflow issues.
        /// Rule A: LayoutGroup content exceeds parent bounds without ScrollRect.
        /// Rule B: RectTransform sizeDelta causes overflow on stretch axes.
        /// </summary>
        public List<IntegrityIssue> FindUIOverflowIssues(string rootPath = null)
        {
            var issues = new List<IntegrityIssue>();
            var gameObjects = GetTargetGameObjects(rootPath);

            foreach (var go in gameObjects)
            {
                // Rule A: GridLayoutGroup overflow
                var grid = go.GetComponent<GridLayoutGroup>();
                if (grid != null && grid.transform.childCount > 0)
                {
                    CheckGridOverflow(go, grid, issues);
                }

                // Rule A: VerticalLayoutGroup overflow
                var vLayout = go.GetComponent<VerticalLayoutGroup>();
                if (vLayout != null && vLayout.transform.childCount > 0)
                {
                    CheckVerticalLayoutOverflow(go, vLayout, issues);
                }

                // Rule A: HorizontalLayoutGroup overflow
                var hLayout = go.GetComponent<HorizontalLayoutGroup>();
                if (hLayout != null && hLayout.transform.childCount > 0)
                {
                    CheckHorizontalLayoutOverflow(go, hLayout, issues);
                }

                // Rule B: sizeDelta overflow on stretch anchors
                var rt = go.GetComponent<RectTransform>();
                if (rt != null && rt.parent is RectTransform parentRt)
                {
                    CheckSizeDeltaOverflow(go, rt, parentRt, issues);
                }
            }
            return issues;
        }

        /// <summary>
        /// Find UI overlap issues: sibling RectTransforms overlapping with interactive elements,
        /// multiple children at same position without LayoutGroup, and raycast-blocking overlaps.
        /// </summary>
        public List<IntegrityIssue> FindUIOverlapIssues(string rootPath = null)
        {
            var issues = new List<IntegrityIssue>();
            var gameObjects = GetTargetGameObjects(rootPath);

            // Collect parents that have 2+ RectTransform children
            var parentSet = new HashSet<Transform>();
            foreach (var go in gameObjects)
            {
                var rt = go.GetComponent<RectTransform>();
                if (rt == null || rt.parent == null) continue;
                var parent = rt.parent;
                if (parentSet.Contains(parent)) continue;

                int childRtCount = 0;
                for (int i = 0; i < parent.childCount; i++)
                {
                    if (parent.GetChild(i).GetComponent<RectTransform>() != null)
                        childRtCount++;
                }
                if (childRtCount >= 2)
                    parentSet.Add(parent);
            }

            foreach (var parent in parentSet)
            {
                var parentGo = parent.gameObject;
                var hasLayout = parentGo.GetComponent<LayoutGroup>() != null;
                var children = new List<(RectTransform rt, bool interactive, bool raycastTarget)>();

                for (int i = 0; i < parent.childCount; i++)
                {
                    var childRt = parent.GetChild(i).GetComponent<RectTransform>();
                    if (childRt == null || !childRt.gameObject.activeInHierarchy) continue;

                    bool interactive = childRt.GetComponentInChildren<Selectable>(false) != null;
                    bool hasRaycast = false;
                    var graphics = childRt.GetComponentsInChildren<Graphic>(false);
                    foreach (var g in graphics)
                    {
                        if (g.raycastTarget) { hasRaycast = true; break; }
                    }
                    children.Add((childRt, interactive, hasRaycast));
                }

                // Rule A: Same anchoredPosition without LayoutGroup
                if (!hasLayout && children.Count >= 2)
                {
                    var posGroups = new Dictionary<string, List<RectTransform>>();
                    foreach (var (rt, _, _) in children)
                    {
                        var key = $"{rt.anchoredPosition.x:F1},{rt.anchoredPosition.y:F1}";
                        if (!posGroups.ContainsKey(key))
                            posGroups[key] = new List<RectTransform>();
                        posGroups[key].Add(rt);
                    }

                    foreach (var kvp in posGroups)
                    {
                        if (kvp.Value.Count < 2) continue;
                        var names = new List<string>();
                        foreach (var rt in kvp.Value) names.Add(rt.name);
                        issues.Add(new IntegrityIssue
                        {
                            Type = "uiOverlap_samePosition",
                            Severity = "warning",
                            GameObjectPath = GetGameObjectPath(parentGo),
                            ComponentType = "RectTransform",
                            Message = $"{kvp.Value.Count} siblings at same position ({kvp.Key}) without LayoutGroup: {string.Join(", ", names)}",
                            Suggestion = "Add a LayoutGroup component to the parent, or assign different anchoredPositions to each child"
                        });
                    }
                }

                // Rule B & C: Sibling overlap with interactive elements
                if (!hasLayout)
                {
                    for (int i = 0; i < children.Count; i++)
                    {
                        for (int j = i + 1; j < children.Count; j++)
                        {
                            var (rtA, interactiveA, raycastA) = children[i];
                            var (rtB, interactiveB, raycastB) = children[j];

                            if (!interactiveA && !interactiveB) continue;

                            var rectA = GetWorldRect(rtA);
                            var rectB = GetWorldRect(rtB);
                            if (!rectA.Overlaps(rectB)) continue;

                            // Rule B: Two interactive siblings overlap
                            if (interactiveA && interactiveB)
                            {
                                issues.Add(new IntegrityIssue
                                {
                                    Type = "uiOverlap_interactiveOverlap",
                                    Severity = "warning",
                                    GameObjectPath = GetGameObjectPath(parentGo),
                                    ComponentType = "Selectable",
                                    Message = $"Interactive siblings '{rtA.name}' and '{rtB.name}' overlap — click target may be ambiguous",
                                    Suggestion = "Separate them with different positions or add a LayoutGroup to the parent"
                                });
                            }

                            // Rule C: Raycast-target non-interactive element blocks interactive one
                            if (interactiveA && !interactiveB && raycastB && rtB.GetSiblingIndex() > rtA.GetSiblingIndex())
                            {
                                issues.Add(new IntegrityIssue
                                {
                                    Type = "uiOverlap_raycastBlock",
                                    Severity = "error",
                                    GameObjectPath = GetGameObjectPath(rtB.gameObject),
                                    ComponentType = "Graphic",
                                    Message = $"'{rtB.name}' (raycastTarget=true) overlaps and blocks interactive '{rtA.name}'",
                                    Suggestion = $"Disable raycastTarget on '{rtB.name}' or move it behind '{rtA.name}' in hierarchy"
                                });
                            }
                            else if (interactiveB && !interactiveA && raycastA && rtA.GetSiblingIndex() > rtB.GetSiblingIndex())
                            {
                                issues.Add(new IntegrityIssue
                                {
                                    Type = "uiOverlap_raycastBlock",
                                    Severity = "error",
                                    GameObjectPath = GetGameObjectPath(rtA.gameObject),
                                    ComponentType = "Graphic",
                                    Message = $"'{rtA.name}' (raycastTarget=true) overlaps and blocks interactive '{rtB.name}'",
                                    Suggestion = $"Disable raycastTarget on '{rtA.name}' or move it behind '{rtB.name}' in hierarchy"
                                });
                            }
                        }
                    }
                }
            }

            return issues;
        }

        /// <summary>
        /// Find null asset reference fields in ScriptableObject assets.
        /// Checks Sprite, Texture2D, AudioClip, Material, GameObject, Mesh fields.
        /// Skips fields with "optional" in their name and Unity built-in SOs.
        /// </summary>
        public List<IntegrityIssue> FindNullAssetFieldIssues(string searchPath = null)
        {
            var issues = new List<IntegrityIssue>();
            var folders = new[] { searchPath ?? "Assets" };
            var guids = AssetDatabase.FindAssets("t:ScriptableObject", folders);
            var checkedTypes = new HashSet<Type>
            {
                typeof(Sprite), typeof(Texture2D), typeof(AudioClip),
                typeof(Material), typeof(GameObject), typeof(Mesh)
            };

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null) continue;
                var assetType = asset.GetType();
                if (assetType.Namespace != null &&
                    (assetType.Namespace.StartsWith("UnityEngine") || assetType.Namespace.StartsWith("UnityEditor")))
                    continue;

                try
                {
                    var so = new SerializedObject(asset);
                    var iter = so.GetIterator();
                    while (iter.NextVisible(true))
                    {
                        if (iter.propertyType != SerializedPropertyType.ObjectReference) continue;
                        if (iter.name == "m_Script") continue;
                        if (iter.objectReferenceValue != null) continue;
                        if (iter.objectReferenceInstanceIDValue != 0) continue; // Broken ref, different issue
                        // Skip fields with "optional" in the name
                        if (iter.name.IndexOf("optional", StringComparison.OrdinalIgnoreCase) >= 0) continue;

                        var fieldInfo = assetType.GetField(iter.name,
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (fieldInfo != null && checkedTypes.Contains(fieldInfo.FieldType))
                        {
                            issues.Add(new IntegrityIssue
                            {
                                Type = "nullAssetField",
                                Severity = "warning",
                                GameObjectPath = path,
                                ComponentType = assetType.Name,
                                FieldName = iter.name,
                                Message = $"{assetType.Name}.{iter.name} ({fieldInfo.FieldType.Name}) is null in '{Path.GetFileName(path)}'",
                                Suggestion = $"Set {fieldInfo.FieldType.Name} reference in '{path}'"
                            });
                        }
                    }
                }
                catch (Exception) { }
            }
            return issues;
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

        private static Rect GetWorldRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            var min = corners[0];
            var max = corners[0];
            for (int i = 1; i < 4; i++)
            {
                min = Vector3.Min(min, corners[i]);
                max = Vector3.Max(max, corners[i]);
            }
            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
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

        private bool IsUnityInternalField(Type compType, string fieldName)
        {
            // If the field is declared on a Unity base type, it's internal
            var field = compType.GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) return true; // Can't find it, treat as internal
            var declaringType = field.DeclaringType;
            return declaringType != null && declaringType.Namespace != null &&
                   (declaringType.Namespace.StartsWith("UnityEngine") || declaringType.Namespace.StartsWith("UnityEditor"));
        }

        private (bool isUsed, bool hasNullGuard) CheckFieldUsageInSource(
            Type compType, string fieldName, Dictionary<Type, string> sourceCache)
        {
            if (!sourceCache.TryGetValue(compType, out var source))
            {
                source = LoadSourceForType(compType);
                sourceCache[compType] = source;
            }

            if (string.IsNullOrEmpty(source))
                return (false, false);

            // Check if field is used in method bodies (not just declared)
            // Look for usage patterns: fieldName. fieldName) fieldName, fieldName; fieldName[
            var usagePattern = new Regex($@"(?<![\w.])({Regex.Escape(fieldName)})(?=\s*[.)\],;\[!=><!])");
            var matches = usagePattern.Matches(source);

            if (matches.Count == 0)
                return (false, false);

            // Check for null guard patterns
            var nullGuardPatterns = new[]
            {
                $@"{Regex.Escape(fieldName)}\s*!=\s*null",
                $@"{Regex.Escape(fieldName)}\s*==\s*null",
                $@"{Regex.Escape(fieldName)}\?\.",
                $@"{Regex.Escape(fieldName)}\?\[",
            };

            bool hasNullGuard = false;
            foreach (var pattern in nullGuardPatterns)
            {
                if (Regex.IsMatch(source, pattern))
                {
                    hasNullGuard = true;
                    break;
                }
            }

            return (true, hasNullGuard);
        }

        private string LoadSourceForType(Type compType)
        {
            // Find MonoScript for this type
            var scripts = AssetDatabase.FindAssets($"t:MonoScript {compType.Name}");
            foreach (var guid in scripts)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (monoScript != null && monoScript.GetClass() == compType)
                {
                    try
                    {
                        var fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), path);
                        return File.ReadAllText(fullPath);
                    }
                    catch { }
                }
            }
            return null;
        }

        private string GetFieldTypeName(Type compType, string fieldName)
        {
            var field = compType.GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field != null ? field.FieldType.Name : "Object";
        }

        private bool HasScrollRectAncestor(GameObject go)
        {
            var parent = go.transform.parent;
            while (parent != null)
            {
                if (parent.GetComponent<ScrollRect>() != null)
                    return true;
                parent = parent.parent;
            }
            return false;
        }

        private int CountActiveChildren(Transform parent)
        {
            int count = 0;
            foreach (Transform child in parent)
            {
                if (child.gameObject.activeSelf)
                    count++;
            }
            return count;
        }

        private void CheckGridOverflow(GameObject go, GridLayoutGroup grid, List<IntegrityIssue> issues)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return;
            if (HasScrollRectAncestor(go)) return;

            int activeChildCount = CountActiveChildren(grid.transform);
            if (activeChildCount == 0) return;

            var padding = grid.padding;
            var cellSize = grid.cellSize;
            var spacing = grid.spacing;

            float requiredWidth, requiredHeight;

            if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount && grid.constraintCount > 0)
            {
                int cols = grid.constraintCount;
                int rows = Mathf.CeilToInt((float)activeChildCount / cols);
                requiredWidth = cols * cellSize.x + (cols - 1) * spacing.x + padding.left + padding.right;
                requiredHeight = rows * cellSize.y + (rows - 1) * spacing.y + padding.top + padding.bottom;
            }
            else if (grid.constraint == GridLayoutGroup.Constraint.FixedRowCount && grid.constraintCount > 0)
            {
                int rows = grid.constraintCount;
                int cols = Mathf.CeilToInt((float)activeChildCount / rows);
                requiredWidth = cols * cellSize.x + (cols - 1) * spacing.x + padding.left + padding.right;
                requiredHeight = rows * cellSize.y + (rows - 1) * spacing.y + padding.top + padding.bottom;
            }
            else
            {
                // Flexible — can't reliably predict layout without forcing a rebuild
                return;
            }

            var rect = rt.rect;
            bool overflowW = requiredWidth > rect.width + 0.5f;
            bool overflowH = requiredHeight > rect.height + 0.5f;

            if (overflowW || overflowH)
            {
                var axis = overflowW && overflowH ? "width and height" : overflowW ? "width" : "height";
                issues.Add(new IntegrityIssue
                {
                    Type = "uiOverflow",
                    Severity = "warning",
                    GameObjectPath = GetGameObjectPath(go),
                    ComponentType = "GridLayoutGroup",
                    Message = $"GridLayoutGroup content overflows parent {axis} ({activeChildCount} children, " +
                              $"required: {requiredWidth:F0}x{requiredHeight:F0}, available: {rect.width:F0}x{rect.height:F0})",
                    Suggestion = "Add a ScrollRect parent or increase the container size"
                });
            }
        }

        private void CheckVerticalLayoutOverflow(GameObject go, VerticalLayoutGroup layout, List<IntegrityIssue> issues)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return;
            if (HasScrollRectAncestor(go)) return;

            int activeChildCount = CountActiveChildren(layout.transform);
            if (activeChildCount == 0) return;

            // Sum up preferred heights of children
            float totalHeight = layout.padding.top + layout.padding.bottom;
            totalHeight += (activeChildCount - 1) * layout.spacing;
            foreach (Transform child in layout.transform)
            {
                if (!child.gameObject.activeSelf) continue;
                var childRt = child.GetComponent<RectTransform>();
                if (childRt != null)
                {
                    var le = child.GetComponent<LayoutElement>();
                    if (le != null && le.preferredHeight > 0)
                        totalHeight += le.preferredHeight;
                    else
                        totalHeight += childRt.rect.height > 0 ? childRt.rect.height : childRt.sizeDelta.y;
                }
            }

            var rect = rt.rect;
            if (totalHeight > rect.height + 0.5f)
            {
                issues.Add(new IntegrityIssue
                {
                    Type = "uiOverflow",
                    Severity = "warning",
                    GameObjectPath = GetGameObjectPath(go),
                    ComponentType = "VerticalLayoutGroup",
                    Message = $"VerticalLayoutGroup content overflows parent height ({activeChildCount} children, " +
                              $"required: {totalHeight:F0}, available: {rect.height:F0})",
                    Suggestion = "Add a ScrollRect parent or increase the container height"
                });
            }
        }

        private void CheckHorizontalLayoutOverflow(GameObject go, HorizontalLayoutGroup layout, List<IntegrityIssue> issues)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return;
            if (HasScrollRectAncestor(go)) return;

            int activeChildCount = CountActiveChildren(layout.transform);
            if (activeChildCount == 0) return;

            float totalWidth = layout.padding.left + layout.padding.right;
            totalWidth += (activeChildCount - 1) * layout.spacing;
            foreach (Transform child in layout.transform)
            {
                if (!child.gameObject.activeSelf) continue;
                var childRt = child.GetComponent<RectTransform>();
                if (childRt != null)
                {
                    var le = child.GetComponent<LayoutElement>();
                    if (le != null && le.preferredWidth > 0)
                        totalWidth += le.preferredWidth;
                    else
                        totalWidth += childRt.rect.width > 0 ? childRt.rect.width : childRt.sizeDelta.x;
                }
            }

            var rect = rt.rect;
            if (totalWidth > rect.width + 0.5f)
            {
                issues.Add(new IntegrityIssue
                {
                    Type = "uiOverflow",
                    Severity = "warning",
                    GameObjectPath = GetGameObjectPath(go),
                    ComponentType = "HorizontalLayoutGroup",
                    Message = $"HorizontalLayoutGroup content overflows parent width ({activeChildCount} children, " +
                              $"required: {totalWidth:F0}, available: {rect.width:F0})",
                    Suggestion = "Add a ScrollRect parent or increase the container width"
                });
            }
        }

        private void CheckSizeDeltaOverflow(GameObject go, RectTransform rt, RectTransform parentRt, List<IntegrityIssue> issues)
        {
            // Only check stretch axes (anchorMin != anchorMax)
            bool stretchX = Mathf.Abs(rt.anchorMin.x - rt.anchorMax.x) > 0.001f;
            bool stretchY = Mathf.Abs(rt.anchorMin.y - rt.anchorMax.y) > 0.001f;

            if (!stretchX && !stretchY) return;

            var parentRect = parentRt.rect;
            bool overflowX = stretchX && rt.sizeDelta.x > 0.5f &&
                             (parentRect.width + rt.sizeDelta.x) > parentRect.width + 0.5f;
            bool overflowY = stretchY && rt.sizeDelta.y > 0.5f &&
                             (parentRect.height + rt.sizeDelta.y) > parentRect.height + 0.5f;

            if (overflowX || overflowY)
            {
                var axis = overflowX && overflowY ? "width and height" : overflowX ? "width" : "height";
                issues.Add(new IntegrityIssue
                {
                    Type = "uiOverflow_sizeDelta",
                    Severity = "warning",
                    GameObjectPath = GetGameObjectPath(go),
                    ComponentType = "RectTransform",
                    Message = $"RectTransform sizeDelta ({rt.sizeDelta.x:F0}, {rt.sizeDelta.y:F0}) causes overflow on stretch {axis}",
                    Suggestion = "Set sizeDelta to 0 or negative on stretch axes to fit within parent"
                });
            }
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

        /// <summary>
        /// Find interactive UI elements (Selectable) that are too small for touch input.
        /// Minimum recommended size is 44x44 units.
        /// </summary>
        public List<IntegrityIssue> FindTouchTargetIssues(string rootPath = null)
        {
            var issues = new List<IntegrityIssue>();
            var gameObjects = GetTargetGameObjects(rootPath);
            const float minSize = 44f;

            foreach (var go in gameObjects)
            {
                var selectable = go.GetComponent<UnityEngine.UI.Selectable>();
                if (selectable == null) continue;

                var rt = go.GetComponent<RectTransform>();
                if (rt == null) continue;

                var rect = rt.rect;
                if (rect.width < minSize || rect.height < minSize)
                {
                    issues.Add(new IntegrityIssue
                    {
                        Type = "touchTarget_tooSmall",
                        Severity = "warning",
                        GameObjectPath = GetGameObjectPath(go),
                        ComponentType = selectable.GetType().Name,
                        Message = $"'{go.name}' size ({rect.width:F0}x{rect.height:F0}) is below minimum touch target ({minSize}x{minSize})",
                        Suggestion = $"Increase RectTransform size to at least {minSize}x{minSize} for reliable touch input"
                    });
                }
            }
            return issues;
        }

        /// <summary>
        /// Find scenes with UI elements (Canvas or UIDocument) but no EventSystem.
        /// Also detects duplicate EventSystems.
        /// </summary>
        public List<IntegrityIssue> FindEventSystemIssues(string rootPath = null)
        {
            var issues = new List<IntegrityIssue>();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();

            bool hasCanvas = false;
            bool hasUIDocument = false;
            int eventSystemCount = 0;

            foreach (var root in rootObjects)
            {
                if (root.GetComponentInChildren<Canvas>(true) != null) hasCanvas = true;
                if (root.GetComponentInChildren<UnityEngine.UIElements.UIDocument>(true) != null) hasUIDocument = true;
                var esList = root.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true);
                eventSystemCount += esList.Length;
            }

            if ((hasCanvas || hasUIDocument) && eventSystemCount == 0)
            {
                string uiType = hasCanvas && hasUIDocument ? "Canvas and UIDocument" : hasCanvas ? "Canvas" : "UIDocument";
                issues.Add(new IntegrityIssue
                {
                    Type = "eventSystem_missing",
                    Severity = "error",
                    GameObjectPath = "/" + scene.name,
                    Message = $"Scene '{scene.name}' has {uiType} but no EventSystem",
                    Suggestion = "Add an EventSystem GameObject (GameObject > UI > Event System)"
                });
            }

            if (eventSystemCount > 1)
            {
                issues.Add(new IntegrityIssue
                {
                    Type = "eventSystem_duplicate",
                    Severity = "warning",
                    GameObjectPath = "/" + scene.name,
                    Message = $"Scene '{scene.name}' has {eventSystemCount} EventSystem components (expected 1)",
                    Suggestion = "Remove duplicate EventSystem GameObjects — only one should exist per scene"
                });
            }

            return issues;
        }

        /// <summary>
        /// Find Text/TextMeshPro elements where content may overflow the RectTransform bounds.
        /// Checks preferredWidth/Height against rect size when overflow mode allows it.
        /// </summary>
        public List<IntegrityIssue> FindTextOverflowIssues(string rootPath = null)
        {
            var issues = new List<IntegrityIssue>();
            var gameObjects = GetTargetGameObjects(rootPath);

            foreach (var go in gameObjects)
            {
                var rt = go.GetComponent<RectTransform>();
                if (rt == null) continue;
                var rect = rt.rect;
                if (rect.width <= 0 || rect.height <= 0) continue;

                // Check TMPro.TextMeshProUGUI via reflection (avoid hard dependency)
                var tmp = go.GetComponent("TextMeshProUGUI");
                if (tmp == null) tmp = go.GetComponent("TextMeshPro");
                if (tmp != null)
                {
                    var tmpType = tmp.GetType();
                    var overflowProp = tmpType.GetProperty("overflowMode");
                    var prefWProp = tmpType.GetProperty("preferredWidth");
                    var prefHProp = tmpType.GetProperty("preferredHeight");
                    var textProp = tmpType.GetProperty("text");

                    if (overflowProp != null && prefWProp != null && prefHProp != null && textProp != null)
                    {
                        string text = textProp.GetValue(tmp) as string;
                        if (string.IsNullOrEmpty(text)) continue;

                        int overflow = (int)overflowProp.GetValue(tmp);
                        // 0 = Overflow (not clipped)
                        if (overflow == 0)
                        {
                            float prefW = (float)prefWProp.GetValue(tmp);
                            float prefH = (float)prefHProp.GetValue(tmp);
                            if (prefW > rect.width * 1.1f || prefH > rect.height * 1.1f)
                            {
                                issues.Add(new IntegrityIssue
                                {
                                    Type = "textOverflow_exceeds",
                                    Severity = "warning",
                                    GameObjectPath = GetGameObjectPath(go),
                                    ComponentType = "TextMeshProUGUI",
                                    Message = $"Text preferred size ({prefW:F0}x{prefH:F0}) exceeds RectTransform ({rect.width:F0}x{rect.height:F0})",
                                    Suggestion = "Set overflowMode to Truncate/Ellipsis, increase RectTransform size, or reduce font size"
                                });
                            }
                        }
                    }
                    continue;
                }

                // Check legacy UnityEngine.UI.Text
                var text2 = go.GetComponent<UnityEngine.UI.Text>();
                if (text2 != null && !string.IsNullOrEmpty(text2.text))
                {
                    if (text2.horizontalOverflow == HorizontalWrapMode.Overflow || text2.verticalOverflow == VerticalWrapMode.Overflow)
                    {
                        float prefW = text2.preferredWidth;
                        float prefH = text2.preferredHeight;
                        if (prefW > rect.width * 1.1f || prefH > rect.height * 1.1f)
                        {
                            issues.Add(new IntegrityIssue
                            {
                                Type = "textOverflow_exceeds",
                                Severity = "warning",
                                GameObjectPath = GetGameObjectPath(go),
                                ComponentType = "Text",
                                Message = $"Text preferred size ({prefW:F0}x{prefH:F0}) exceeds RectTransform ({rect.width:F0}x{rect.height:F0})",
                                Suggestion = "Set overflow to Wrap/Truncate, increase RectTransform size, or reduce font size"
                            });
                        }
                    }
                }
            }
            return issues;
        }

        /// <summary>
        /// Find cross-element design consistency issues in the UI hierarchy.
        /// Inspired by Figma MCP's design system rules concept.
        /// </summary>
        public List<IntegrityIssue> FindStyleConsistencyIssues(string rootPath = null)
        {
            var gameObjects = GetTargetGameObjects(rootPath);
            var issues = new List<IntegrityIssue>();

            // Collect data for cross-element analysis
            var buttonNormalColors = new Dictionary<Color, List<string>>();
            var fontSizes = new Dictionary<int, List<string>>();
            var spacingValues = new HashSet<float>();
            var siblingAnchorGroups = new Dictionary<Transform, List<(GameObject go, string anchorPattern)>>();

            foreach (var go in gameObjects)
            {
                var path = GetGameObjectPath(go);

                // 1. Button color variation
                var button = go.GetComponent<Button>();
                if (button != null && button.transition == Selectable.Transition.ColorTint)
                {
                    var normalColor = button.colors.normalColor;
                    // Round to avoid floating point noise
                    var key = new Color(
                        Mathf.Round(normalColor.r * 100f) / 100f,
                        Mathf.Round(normalColor.g * 100f) / 100f,
                        Mathf.Round(normalColor.b * 100f) / 100f,
                        Mathf.Round(normalColor.a * 100f) / 100f);
                    if (!buttonNormalColors.ContainsKey(key))
                        buttonNormalColors[key] = new List<string>();
                    buttonNormalColors[key].Add(path);
                }

                // 2. Font size collection
                var text = go.GetComponent<Text>();
                if (text != null)
                {
                    if (!fontSizes.ContainsKey(text.fontSize))
                        fontSizes[text.fontSize] = new List<string>();
                    fontSizes[text.fontSize].Add(path);
                }
                else
                {
                    var tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
                    if (tmpType != null)
                    {
                        var tmp = go.GetComponent(tmpType);
                        if (tmp != null)
                        {
                            var fsProp = tmpType.GetProperty("fontSize");
                            if (fsProp != null)
                            {
                                int fs = (int)Math.Round(Convert.ToSingle(fsProp.GetValue(tmp)));
                                if (!fontSizes.ContainsKey(fs))
                                    fontSizes[fs] = new List<string>();
                                fontSizes[fs].Add(path);
                            }
                        }
                    }
                }

                // 3. Spacing values
                var hLayout = go.GetComponent<HorizontalLayoutGroup>();
                if (hLayout != null) spacingValues.Add(hLayout.spacing);
                var vLayout = go.GetComponent<VerticalLayoutGroup>();
                if (vLayout != null) spacingValues.Add(vLayout.spacing);
                var gridLayout = go.GetComponent<GridLayoutGroup>();
                if (gridLayout != null)
                {
                    spacingValues.Add(gridLayout.spacing.x);
                    spacingValues.Add(gridLayout.spacing.y);
                }

                // 4. No-op CanvasGroup
                var cg = go.GetComponent<CanvasGroup>();
                if (cg != null && Math.Abs(cg.alpha - 1f) < 0.001f &&
                    cg.interactable && cg.blocksRaycasts && !cg.ignoreParentGroups)
                {
                    issues.Add(new IntegrityIssue
                    {
                        Type = "noOpCanvasGroup",
                        Severity = "info",
                        GameObjectPath = path,
                        ComponentType = "CanvasGroup",
                        Message = "CanvasGroup has default values (alpha=1, interactable=true, blocksRaycasts=true) and has no effect",
                        Suggestion = "Remove the CanvasGroup if it's not needed, or adjust its properties"
                    });
                }

                // 5. Missing interaction feedback
                var selectable = go.GetComponent<Selectable>();
                if (selectable != null && selectable.transition == Selectable.Transition.None)
                {
                    issues.Add(new IntegrityIssue
                    {
                        Type = "missingInteractionFeedback",
                        Severity = "warning",
                        GameObjectPath = path,
                        ComponentType = selectable.GetType().Name,
                        Message = "Interactive element has Transition.None — no visual feedback on hover/press",
                        Suggestion = "Set transition to ColorTint, SpriteSwap, or Animation for better UX"
                    });
                }

                // 6. Unnecessary raycast target
                var image = go.GetComponent<Image>();
                if (image != null && image.raycastTarget && selectable == null)
                {
                    // Check if parent has ScrollRect (valid use case for raycast blocking)
                    bool hasScrollParent = false;
                    var parent = go.transform.parent;
                    while (parent != null)
                    {
                        if (parent.GetComponent<ScrollRect>() != null) { hasScrollParent = true; break; }
                        parent = parent.parent;
                    }
                    if (!hasScrollParent)
                    {
                        issues.Add(new IntegrityIssue
                        {
                            Type = "unnecessaryRaycastTarget",
                            Severity = "info",
                            GameObjectPath = path,
                            ComponentType = "Image",
                            Message = "Image has raycastTarget=true but no Selectable component — may waste raycast processing",
                            Suggestion = "Set raycastTarget to false if this Image doesn't need to receive input events"
                        });
                    }
                }

                // 7. Collect sibling anchor patterns (for inconsistency check below)
                var rect = go.GetComponent<RectTransform>();
                if (rect != null && go.transform.parent != null)
                {
                    var parentTransform = go.transform.parent;
                    // Only check if parent has no LayoutGroup (layout groups control positioning)
                    if (parentTransform.GetComponent<HorizontalLayoutGroup>() == null &&
                        parentTransform.GetComponent<VerticalLayoutGroup>() == null &&
                        parentTransform.GetComponent<GridLayoutGroup>() == null)
                    {
                        string anchorPattern = ClassifyAnchorPattern(rect);
                        if (!siblingAnchorGroups.ContainsKey(parentTransform))
                            siblingAnchorGroups[parentTransform] = new List<(GameObject, string)>();
                        siblingAnchorGroups[parentTransform].Add((go, anchorPattern));
                    }
                }
            }

            // Cross-element checks

            // 1. Excessive button color variation
            if (buttonNormalColors.Count > 3)
            {
                var colorList = new List<string>();
                foreach (var kvp in buttonNormalColors)
                    colorList.Add($"#{(int)(kvp.Key.r * 255):X2}{(int)(kvp.Key.g * 255):X2}{(int)(kvp.Key.b * 255):X2} ({kvp.Value.Count} buttons)");

                issues.Add(new IntegrityIssue
                {
                    Type = "excessiveButtonColorVariation",
                    Severity = "warning",
                    GameObjectPath = rootPath ?? "(scene-wide)",
                    Message = $"Found {buttonNormalColors.Count} distinct button normal colors: {string.Join(", ", colorList)}",
                    Suggestion = "Standardize button colors to 2-3 variants (primary, secondary, danger) for design consistency"
                });
            }

            // 2. Font size scale violation
            if (fontSizes.Count > 2)
            {
                var sortedSizes = new List<int>(fontSizes.Keys);
                sortedSizes.Sort();
                for (int i = 0; i < sortedSizes.Count - 2; i++)
                {
                    float ratio1 = (float)sortedSizes[i + 1] / sortedSizes[i];
                    float ratio2 = (float)sortedSizes[i + 2] / sortedSizes[i + 1];
                    if (ratio1 > 0 && ratio2 > 0 && Math.Abs(ratio1 - ratio2) / ratio1 > 0.5f)
                    {
                        issues.Add(new IntegrityIssue
                        {
                            Type = "fontSizeScaleViolation",
                            Severity = "info",
                            GameObjectPath = rootPath ?? "(scene-wide)",
                            Message = $"Font sizes [{sortedSizes[i]}, {sortedSizes[i + 1]}, {sortedSizes[i + 2]}] do not follow a consistent scale ratio",
                            Suggestion = "Consider using a modular type scale (e.g., 1.25x ratio: 12, 15, 19, 24, 30)"
                        });
                        break;
                    }
                }
            }

            // 3. Spacing inconsistency
            var nonZeroSpacings = new List<float>();
            foreach (var s in spacingValues)
                if (s > 0) nonZeroSpacings.Add(s);
            if (nonZeroSpacings.Count > 4)
            {
                nonZeroSpacings.Sort();
                issues.Add(new IntegrityIssue
                {
                    Type = "spacingInconsistency",
                    Severity = "warning",
                    GameObjectPath = rootPath ?? "(scene-wide)",
                    Message = $"Found {nonZeroSpacings.Count} distinct spacing values: {string.Join(", ", nonZeroSpacings)}",
                    Suggestion = "Standardize spacing to a consistent scale (e.g., 4, 8, 12, 16, 24)"
                });
            }

            // 7. Inconsistent anchor patterns among siblings
            foreach (var kvp in siblingAnchorGroups)
            {
                var siblings = kvp.Value;
                if (siblings.Count < 2) continue;

                var patterns = new HashSet<string>();
                foreach (var (go, pattern) in siblings)
                    patterns.Add(pattern);

                if (patterns.Count > 2)
                {
                    var parentPath = GetGameObjectPath(kvp.Key.gameObject);
                    issues.Add(new IntegrityIssue
                    {
                        Type = "inconsistentSiblingAnchors",
                        Severity = "info",
                        GameObjectPath = parentPath,
                        Message = $"Children use {patterns.Count} different anchor patterns: {string.Join(", ", patterns)}",
                        Suggestion = "Consider using consistent anchor patterns for siblings, or add a LayoutGroup for automatic arrangement"
                    });
                }
            }

            return issues;
        }

        private static string ClassifyAnchorPattern(RectTransform rect)
        {
            bool stretchH = Math.Abs(rect.anchorMin.x - rect.anchorMax.x) > 0.01f;
            bool stretchV = Math.Abs(rect.anchorMin.y - rect.anchorMax.y) > 0.01f;
            if (stretchH && stretchV) return "stretchAll";
            if (stretchH) return "stretchHorizontal";
            if (stretchV) return "stretchVertical";

            // Point anchor — classify by position
            float x = rect.anchorMin.x;
            float y = rect.anchorMin.y;
            string h = x < 0.33f ? "left" : x > 0.67f ? "right" : "center";
            string v = y < 0.33f ? "bottom" : y > 0.67f ? "top" : "middle";
            return $"{v}{char.ToUpper(h[0])}{h.Substring(1)}";
        }
    }
}
