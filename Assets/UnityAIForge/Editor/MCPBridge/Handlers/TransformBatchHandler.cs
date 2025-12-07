using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Mid-level batch utilities for arranging transforms, renaming objects, and creating menu lists.
    /// </summary>
    public class TransformBatchHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "arrangeCircle",
            "arrangeLine",
            "renameSequential",
            "renameFromList",
            "createMenuList",
        };

        public override string Category => "transformBatch";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "arrangeCircle" => ArrangeCircle(payload),
                "arrangeLine" => ArrangeLine(payload),
                "renameSequential" => RenameSequential(payload),
                "renameFromList" => RenameFromList(payload),
                "createMenuList" => CreateMenuList(payload),
                _ => throw new InvalidOperationException($"Unsupported transform batch operation: {operation}"),
            };
        }

        #region Arrange Circle

        private object ArrangeCircle(Dictionary<string, object> payload)
        {
            var targets = GetTargetGameObjects(payload);
            if (targets.Count == 0)
            {
                throw new InvalidOperationException("gameObjectPaths is required and must contain at least one object.");
            }

            var center = GetVector3(payload, "center", Vector3.zero);
            var radius = GetFloatPayload(payload, "radius", 1f);
            var startAngleDeg = GetFloatPayload(payload, "startAngle", 0f);
            var clockwise = GetBool(payload, "clockwise");
            var plane = GetString(payload, "plane")?.ToUpperInvariant() ?? "XZ";
            var localSpace = GetBool(payload, "localSpace");

            var step = targets.Count == 1 ? 360f : 360f / targets.Count;
            if (clockwise)
            {
                step = -step;
            }

            var updated = new List<string>();
            for (var i = 0; i < targets.Count; i++)
            {
                var angleRad = Mathf.Deg2Rad * (startAngleDeg + step * i);
                Vector3 offset = plane switch
                {
                    "XY" => new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f),
                    "YZ" => new Vector3(0f, Mathf.Sin(angleRad), Mathf.Cos(angleRad)),
                    _ => new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad)),
                };
                var targetPosition = center + offset * radius;
                ApplyPosition(targets[i], targetPosition, localSpace);
                updated.Add(BuildGameObjectPath(targets[i]));
            }

            MarkScenesDirty(targets);
            return CreateSuccessResponse(("updated", updated), ("count", updated.Count));
        }

        #endregion

        #region Arrange Line

        private object ArrangeLine(Dictionary<string, object> payload)
        {
            var targets = GetTargetGameObjects(payload);
            if (targets.Count == 0)
            {
                throw new InvalidOperationException("gameObjectPaths is required and must contain at least one object.");
            }

            var start = GetVector3(payload, "startPosition", Vector3.zero);
            var end = GetVector3(payload, "endPosition", start);
            var localSpace = GetBool(payload, "localSpace");

            var updated = new List<string>();
            if (targets.Count == 1)
            {
                ApplyPosition(targets[0], start, localSpace);
                updated.Add(BuildGameObjectPath(targets[0]));
            }
            else
            {
                for (var i = 0; i < targets.Count; i++)
                {
                    var t = i / (float)(targets.Count - 1);
                    var position = Vector3.Lerp(start, end, t);
                    ApplyPosition(targets[i], position, localSpace);
                    updated.Add(BuildGameObjectPath(targets[i]));
                }
            }

            MarkScenesDirty(targets);
            return CreateSuccessResponse(("updated", updated), ("count", updated.Count));
        }

        #endregion

        #region Rename

        private object RenameSequential(Dictionary<string, object> payload)
        {
            var targets = GetTargetGameObjects(payload);
            if (targets.Count == 0)
            {
                throw new InvalidOperationException("gameObjectPaths is required and must contain at least one object.");
            }

            var baseName = GetString(payload, "baseName");
            if (string.IsNullOrEmpty(baseName))
            {
                throw new InvalidOperationException("baseName is required for renameSequential.");
            }

            var startIndex = GetInt(payload, "startIndex", 1);
            var padding = Mathf.Max(0, GetInt(payload, "padding", 0));
            var updated = new List<string>();

            for (var i = 0; i < targets.Count; i++)
            {
                var number = (startIndex + i).ToString().PadLeft(padding, '0');
                var newName = $"{baseName}{number}";
                Undo.RecordObject(targets[i], "Rename GameObject");
                targets[i].name = newName;
                updated.Add(newName);
            }

            MarkScenesDirty(targets);
            return CreateSuccessResponse(("names", updated), ("count", updated.Count));
        }

        private object RenameFromList(Dictionary<string, object> payload)
        {
            var targets = GetTargetGameObjects(payload);
            var names = GetStringList(payload, "names");
            if (targets.Count == 0 || names.Count == 0)
            {
                throw new InvalidOperationException("gameObjectPaths and names must both contain at least one entry.");
            }

            if (targets.Count != names.Count)
            {
                throw new InvalidOperationException("names count must match gameObjectPaths count.");
            }

            var updated = new List<string>();
            for (var i = 0; i < targets.Count; i++)
            {
                Undo.RecordObject(targets[i], "Rename GameObject");
                targets[i].name = names[i];
                updated.Add(names[i]);
            }

            MarkScenesDirty(targets);
            return CreateSuccessResponse(("names", updated), ("count", updated.Count));
        }

        #endregion

        #region Create Menu List

        private object CreateMenuList(Dictionary<string, object> payload)
        {
            var parentPath = GetString(payload, "parentPath");
            if (string.IsNullOrEmpty(parentPath))
            {
                throw new InvalidOperationException("parentPath is required for createMenuList.");
            }

            var names = GetStringList(payload, "names");
            if (names.Count == 0)
            {
                throw new InvalidOperationException("names is required and must contain at least one entry.");
            }

            var parent = ResolveGameObject(parentPath);
            var prefabPath = GetString(payload, "prefabPath");
            var axis = (GetString(payload, "axis") ?? "vertical").ToLowerInvariant();
            var spacing = GetFloatPayload(payload, "spacing", 40f);
            var offset = GetFloatPayload(payload, "offset", 0f);
            var created = new List<string>();

            bool parentIsUI = parent.GetComponent<RectTransform>() != null;
            Vector3 startPosition = parentIsUI ? Vector3.zero : new Vector3(0f, offset, 0f);

            for (var i = 0; i < names.Count; i++)
            {
                GameObject child;
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab == null)
                    {
                        throw new InvalidOperationException($"Failed to load prefab at {prefabPath}");
                    }
                    child = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    Undo.RegisterCreatedObjectUndo(child, "Create Menu Item");
                }
                else
                {
                    child = parentIsUI ? new GameObject(names[i], typeof(RectTransform)) : new GameObject(names[i]);
                    Undo.RegisterCreatedObjectUndo(child, "Create Menu Item");
                }

                child.name = names[i];
                child.transform.SetParent(parent.transform, false);

                if (parentIsUI)
                {
                    var rect = child.GetComponent<RectTransform>();
                    if (rect == null)
                    {
                        rect = child.AddComponent<RectTransform>();
                    }

                    var anchored = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y);
                    var delta = spacing * i;
                    if (axis == "horizontal")
                    {
                        anchored.x = offset + delta;
                    }
                    else
                    {
                        anchored.y = offset - delta;
                    }
                    rect.anchoredPosition = anchored;
                }
                else
                {
                    var delta = spacing * i;
                    Vector3 localPosition;
                    if (axis == "horizontal")
                    {
                        localPosition = startPosition + new Vector3(offset + delta, 0f, 0f);
                    }
                    else
                    {
                        localPosition = startPosition + new Vector3(0f, offset - delta, 0f);
                    }
                child.transform.localPosition = localPosition;
                }

            created.Add(BuildGameObjectPath(child));
            }

            MarkScenesDirty(new List<GameObject> { parent });
            return CreateSuccessResponse(("created", created), ("count", created.Count));
        }

        #endregion

        #region Helpers

        private List<GameObject> GetTargetGameObjects(Dictionary<string, object> payload)
        {
            var paths = GetStringList(payload, "gameObjectPaths");
            var result = new List<GameObject>();
            foreach (var path in paths)
            {
                var go = ResolveGameObject(path);
                result.Add(go);
            }
            return result;
        }

        private Vector3 GetVector3(Dictionary<string, object> payload, string key, Vector3 fallback)
        {
            if (!payload.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            if (value is Dictionary<string, object> dict)
            {
                float x = GetFloat(dict, "x", fallback.x);
                float y = GetFloat(dict, "y", fallback.y);
                float z = GetFloat(dict, "z", fallback.z);
                return new Vector3(x, y, z);
            }

            return fallback;
        }

        // Note: Using 'new' to explicitly hide the inherited methods for local use
        private new float GetFloat(Dictionary<string, object> dict, string key, float defaultValue)
        {
            if (!dict.TryGetValue(key, out var value) || value == null)
            {
                return defaultValue;
            }

            return Convert.ToSingle(value);
        }

        private float GetFloatPayload(Dictionary<string, object> payload, string key, float defaultValue)
        {
            if (!payload.TryGetValue(key, out var value) || value == null)
            {
                return defaultValue;
            }

            return Convert.ToSingle(value);
        }

        private new int GetInt(Dictionary<string, object> payload, string key, int defaultValue)
        {
            if (!payload.TryGetValue(key, out var value) || value == null)
            {
                return defaultValue;
            }

            return Convert.ToInt32(value);
        }

        private List<string> GetStringList(Dictionary<string, object> payload, string key)
        {
            if (!payload.TryGetValue(key, out var value) || value == null)
            {
                return new List<string>();
            }

            if (value is List<object> list)
            {
                return list.Select(v => v?.ToString()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            }

            if (value is string single)
            {
                return new List<string> { single };
            }

            return new List<string>();
        }

        private void ApplyPosition(GameObject go, Vector3 targetPosition, bool localSpace)
        {
            Undo.RecordObject(go.transform, "Arrange Transform");
            if (localSpace)
            {
                go.transform.localPosition = targetPosition;
            }
            else
            {
                go.transform.position = targetPosition;
            }
        }

        private void MarkScenesDirty(IEnumerable<GameObject> objects)
        {
            foreach (var scene in objects.Select(o => o.scene).Distinct())
            {
                if (scene.IsValid())
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }
        }

        private string BuildGameObjectPath(GameObject go)
        {
            if (go == null)
            {
                return string.Empty;
            }

            var path = go.name;
            var parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        #endregion
    }
}

