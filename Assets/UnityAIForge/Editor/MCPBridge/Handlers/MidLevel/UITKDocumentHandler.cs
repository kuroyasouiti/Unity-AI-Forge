using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Mid-level UI Toolkit document handler: create/inspect/update/delete UIDocument GameObjects,
    /// and query the live VisualElement tree via UQuery-style selectors.
    /// </summary>
    public class UITKDocumentHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create",
            "inspect",
            "update",
            "delete",
            "query",
        };

        public override string Category => "uitkDocument";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateDocument(payload),
                "inspect" => InspectDocument(payload),
                "update" => UpdateDocument(payload),
                "delete" => DeleteDocument(payload),
                "query" => QueryElements(payload),
                _ => throw new InvalidOperationException($"Unsupported uitkDocument operation: {operation}"),
            };
        }

        #region Create

        private object CreateDocument(Dictionary<string, object> payload)
        {
            var name = GetString(payload, "name") ?? "UIDocument";

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create UIDocument");

            var uiDoc = Undo.AddComponent<UIDocument>(go);

            // Set parent if specified
            var parentPath = GetString(payload, "parentPath");
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = ResolveGameObject(parentPath);
                if (parent != null)
                {
                    Undo.SetTransformParent(go.transform, parent.transform, "Set UIDocument Parent");
                }
            }

            // Assign UXML source asset
            var uxmlPath = GetString(payload, "sourceAsset");
            if (!string.IsNullOrEmpty(uxmlPath))
            {
                var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
                if (asset != null)
                {
                    Undo.RecordObject(uiDoc, "Set UXML Source");
                    uiDoc.visualTreeAsset = asset;
                }
            }

            // Assign PanelSettings
            var panelSettingsPath = GetString(payload, "panelSettings");
            if (!string.IsNullOrEmpty(panelSettingsPath))
            {
                var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelSettingsPath);
                if (panelSettings != null)
                {
                    Undo.RecordObject(uiDoc, "Set PanelSettings");
                    uiDoc.panelSettings = panelSettings;
                }
            }

            // Sort order
            var sortOrder = GetFloat(payload, "sortingOrder", 0f);
            if (sortOrder != 0f)
            {
                Undo.RecordObject(uiDoc, "Set Sort Order");
                uiDoc.sortingOrder = sortOrder;
            }

            MarkSceneDirty(go);

            return CreateSuccessResponse(
                (KeyGameObjectPath, BuildGameObjectPath(go)),
                ("name", name),
                (KeyMessage, $"Created UIDocument '{name}'")
            );
        }

        #endregion

        #region Inspect

        private object InspectDocument(Dictionary<string, object> payload)
        {
            var gameObjectPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(gameObjectPath))
                throw new InvalidOperationException("'gameObjectPath' is required for inspect");

            var go = ResolveGameObject(gameObjectPath);
            var uiDoc = go.GetComponent<UIDocument>();
            if (uiDoc == null)
                return CreateFailureResponse($"No UIDocument found on '{gameObjectPath}'");

            var result = new Dictionary<string, object>
            {
                [KeySuccess] = true,
                [KeyGameObjectPath] = BuildGameObjectPath(go),
                ["sourceAsset"] = uiDoc.visualTreeAsset != null ? AssetDatabase.GetAssetPath(uiDoc.visualTreeAsset) : null,
                ["panelSettings"] = uiDoc.panelSettings != null ? AssetDatabase.GetAssetPath(uiDoc.panelSettings) : null,
                ["sortingOrder"] = uiDoc.sortingOrder,
            };

            // Try to inspect the live visual tree (only available in play mode or if panel is active)
            var root = uiDoc.rootVisualElement;
            if (root != null)
            {
                result["treeAvailable"] = true;
                var maxDepth = GetInt(payload, "maxDepth", 5);
                result["visualTree"] = SerializeVisualElement(root, 0, maxDepth);
            }
            else
            {
                result["treeAvailable"] = false;
            }

            return result;
        }

        private Dictionary<string, object> SerializeVisualElement(VisualElement element, int depth, int maxDepth)
        {
            var result = new Dictionary<string, object>
            {
                ["type"] = element.GetType().Name,
            };

            if (!string.IsNullOrEmpty(element.name))
                result["name"] = element.name;

            var classes = element.GetClasses().ToList();
            if (classes.Count > 0)
                result["classes"] = classes;

            // Basic layout info
            var layout = element.layout;
            if (!float.IsNaN(layout.width) && !float.IsNaN(layout.height))
            {
                result["layout"] = new Dictionary<string, object>
                {
                    ["x"] = layout.x,
                    ["y"] = layout.y,
                    ["width"] = layout.width,
                    ["height"] = layout.height,
                };
            }

            result["visible"] = element.visible;
            result["enabledSelf"] = element.enabledSelf;
            result["childCount"] = element.childCount;

            // Recurse children up to maxDepth
            if (depth < maxDepth && element.childCount > 0)
            {
                var children = new List<object>();
                foreach (var child in element.Children())
                {
                    children.Add(SerializeVisualElement(child, depth + 1, maxDepth));
                }
                result["children"] = children;
            }

            return result;
        }

        #endregion

        #region Update

        private object UpdateDocument(Dictionary<string, object> payload)
        {
            var gameObjectPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(gameObjectPath))
                throw new InvalidOperationException("'gameObjectPath' is required for update");

            var go = ResolveGameObject(gameObjectPath);
            var uiDoc = go.GetComponent<UIDocument>();
            if (uiDoc == null)
                return CreateFailureResponse($"No UIDocument found on '{gameObjectPath}'");

            Undo.RecordObject(uiDoc, "Update UIDocument");

            var uxmlPath = GetString(payload, "sourceAsset");
            if (uxmlPath != null)
            {
                if (string.IsNullOrEmpty(uxmlPath))
                {
                    uiDoc.visualTreeAsset = null;
                }
                else
                {
                    var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
                    if (asset != null) uiDoc.visualTreeAsset = asset;
                    else return CreateFailureResponse($"VisualTreeAsset not found at '{uxmlPath}'");
                }
            }

            var panelSettingsPath = GetString(payload, "panelSettings");
            if (panelSettingsPath != null)
            {
                if (string.IsNullOrEmpty(panelSettingsPath))
                {
                    uiDoc.panelSettings = null;
                }
                else
                {
                    var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelSettingsPath);
                    if (ps != null) uiDoc.panelSettings = ps;
                    else return CreateFailureResponse($"PanelSettings not found at '{panelSettingsPath}'");
                }
            }

            if (payload.ContainsKey("sortingOrder"))
            {
                uiDoc.sortingOrder = GetFloat(payload, "sortingOrder", 0f);
            }

            MarkSceneDirty(go);

            return CreateSuccessResponse(
                (KeyGameObjectPath, BuildGameObjectPath(go)),
                (KeyMessage, $"Updated UIDocument on '{gameObjectPath}'")
            );
        }

        #endregion

        #region Delete

        private object DeleteDocument(Dictionary<string, object> payload)
        {
            var gameObjectPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(gameObjectPath))
                throw new InvalidOperationException("'gameObjectPath' is required for delete");

            var go = ResolveGameObject(gameObjectPath);
            var deleteGameObject = GetBool(payload, "deleteGameObject", false);

            if (deleteGameObject)
            {
                Undo.DestroyObjectImmediate(go);
                return CreateSuccessResponse(
                    (KeyMessage, $"Deleted GameObject '{gameObjectPath}'")
                );
            }

            var uiDoc = go.GetComponent<UIDocument>();
            if (uiDoc == null)
                return CreateFailureResponse($"No UIDocument found on '{gameObjectPath}'");

            Undo.DestroyObjectImmediate(uiDoc);
            MarkSceneDirty(go);

            return CreateSuccessResponse(
                (KeyGameObjectPath, BuildGameObjectPath(go)),
                (KeyMessage, $"Removed UIDocument from '{gameObjectPath}'")
            );
        }

        #endregion

        #region Query

        private object QueryElements(Dictionary<string, object> payload)
        {
            var gameObjectPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(gameObjectPath))
                throw new InvalidOperationException("'gameObjectPath' is required for query");

            var go = ResolveGameObject(gameObjectPath);
            var uiDoc = go.GetComponent<UIDocument>();
            if (uiDoc == null)
                return CreateFailureResponse($"No UIDocument found on '{gameObjectPath}'");

            var root = uiDoc.rootVisualElement;
            if (root == null)
                return CreateFailureResponse("VisualElement tree not available (not in play mode or panel not active)");

            var queryName = GetString(payload, "queryName");
            var queryClass = GetString(payload, "queryClass");
            var queryType = GetString(payload, "queryType");
            var maxResults = GetInt(payload, "maxResults", 100);

            IEnumerable<VisualElement> results;

            if (!string.IsNullOrEmpty(queryName))
            {
                // Find by name
                var found = root.Q(queryName);
                results = found != null ? new[] { found } : Enumerable.Empty<VisualElement>();
            }
            else if (!string.IsNullOrEmpty(queryClass))
            {
                // Find all by class
                results = root.Query(className: queryClass).ToList();
            }
            else if (!string.IsNullOrEmpty(queryType))
            {
                // Find all elements and filter by type name
                results = root.Query().ToList()
                    .Where(e => e.GetType().Name.Equals(queryType, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                // Return all elements
                results = root.Query().ToList();
            }

            var resultList = results.Take(maxResults).Select(e => SerializeVisualElement(e, 0, 1)).ToList();

            return CreateSuccessResponse(
                (KeyGameObjectPath, BuildGameObjectPath(go)),
                ("results", resultList),
                ("count", resultList.Count)
            );
        }

        #endregion
    }
}
