using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Mid-level UI navigation handler: configure keyboard/gamepad navigation,
    /// manage navigation groups, and set up focus systems.
    /// </summary>
    public class UINavigationHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "configure",
            "setExplicit",
            "autoSetup",
            "createGroup",
            "setFirstSelected",
            "inspect",
            "reset",
            "disable",
        };

        public override string Category => "uiNavigation";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "configure" => ConfigureNavigation(payload),
                "setExplicit" => SetExplicitNavigation(payload),
                "autoSetup" => AutoSetupNavigation(payload),
                "createGroup" => CreateNavigationGroup(payload),
                "setFirstSelected" => SetFirstSelected(payload),
                "inspect" => InspectNavigation(payload),
                "reset" => ResetNavigation(payload),
                "disable" => DisableNavigation(payload),
                _ => throw new InvalidOperationException($"Unsupported UI navigation operation: {operation}"),
            };
        }

        #region Configure Navigation

        private object ConfigureNavigation(Dictionary<string, object> payload)
        {
            var gameObjectPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required for configure operation.");
            }

            var go = ResolveGameObject(gameObjectPath);
            var selectable = go.GetComponent<Selectable>();
            if (selectable == null)
            {
                throw new InvalidOperationException($"GameObject '{gameObjectPath}' does not have a Selectable component.");
            }

            var modeStr = GetString(payload, "mode") ?? "automatic";
            var navigation = selectable.navigation;

            navigation.mode = modeStr.ToLowerInvariant() switch
            {
                "none" => Navigation.Mode.None,
                "horizontal" => Navigation.Mode.Horizontal,
                "vertical" => Navigation.Mode.Vertical,
                "automatic" => Navigation.Mode.Automatic,
                "explicit" => Navigation.Mode.Explicit,
                _ => Navigation.Mode.Automatic
            };

            // Set wrap around if applicable
            if (GetBoolOrDefault(payload, "wrapAround", false))
            {
                navigation.wrapAround = true;
            }

            // Set explicit targets if provided
            if (payload.TryGetValue("selectOnUp", out var upPath) && upPath is string upStr && !string.IsNullOrEmpty(upStr))
            {
                var upGo = GameObject.Find(upStr);
                if (upGo != null)
                    navigation.selectOnUp = upGo.GetComponent<Selectable>();
            }

            if (payload.TryGetValue("selectOnDown", out var downPath) && downPath is string downStr && !string.IsNullOrEmpty(downStr))
            {
                var downGo = GameObject.Find(downStr);
                if (downGo != null)
                    navigation.selectOnDown = downGo.GetComponent<Selectable>();
            }

            if (payload.TryGetValue("selectOnLeft", out var leftPath) && leftPath is string leftStr && !string.IsNullOrEmpty(leftStr))
            {
                var leftGo = GameObject.Find(leftStr);
                if (leftGo != null)
                    navigation.selectOnLeft = leftGo.GetComponent<Selectable>();
            }

            if (payload.TryGetValue("selectOnRight", out var rightPath) && rightPath is string rightStr && !string.IsNullOrEmpty(rightStr))
            {
                var rightGo = GameObject.Find(rightStr);
                if (rightGo != null)
                    navigation.selectOnRight = rightGo.GetComponent<Selectable>();
            }

            selectable.navigation = navigation;
            EditorUtility.SetDirty(selectable);
            EditorSceneManager.MarkSceneDirty(go.scene);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["gameObjectPath"] = gameObjectPath,
                ["mode"] = navigation.mode.ToString(),
                ["wrapAround"] = navigation.wrapAround
            };
        }

        #endregion

        #region Set Explicit Navigation

        private object SetExplicitNavigation(Dictionary<string, object> payload)
        {
            var gameObjectPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required for setExplicit operation.");
            }

            var go = ResolveGameObject(gameObjectPath);
            var selectable = go.GetComponent<Selectable>();
            if (selectable == null)
            {
                throw new InvalidOperationException($"GameObject '{gameObjectPath}' does not have a Selectable component.");
            }

            var navigation = selectable.navigation;
            navigation.mode = Navigation.Mode.Explicit;

            var connections = new Dictionary<string, string>();

            // Set explicit navigation targets
            if (payload.TryGetValue("up", out var upPath) && upPath is string upStr && !string.IsNullOrEmpty(upStr))
            {
                var upGo = GameObject.Find(upStr);
                if (upGo != null)
                {
                    navigation.selectOnUp = upGo.GetComponent<Selectable>();
                    connections["up"] = upStr;
                }
            }

            if (payload.TryGetValue("down", out var downPath) && downPath is string downStr && !string.IsNullOrEmpty(downStr))
            {
                var downGo = GameObject.Find(downStr);
                if (downGo != null)
                {
                    navigation.selectOnDown = downGo.GetComponent<Selectable>();
                    connections["down"] = downStr;
                }
            }

            if (payload.TryGetValue("left", out var leftPath) && leftPath is string leftStr && !string.IsNullOrEmpty(leftStr))
            {
                var leftGo = GameObject.Find(leftStr);
                if (leftGo != null)
                {
                    navigation.selectOnLeft = leftGo.GetComponent<Selectable>();
                    connections["left"] = leftStr;
                }
            }

            if (payload.TryGetValue("right", out var rightPath) && rightPath is string rightStr && !string.IsNullOrEmpty(rightStr))
            {
                var rightGo = GameObject.Find(rightStr);
                if (rightGo != null)
                {
                    navigation.selectOnRight = rightGo.GetComponent<Selectable>();
                    connections["right"] = rightStr;
                }
            }

            selectable.navigation = navigation;
            EditorUtility.SetDirty(selectable);
            EditorSceneManager.MarkSceneDirty(go.scene);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["gameObjectPath"] = gameObjectPath,
                ["mode"] = "Explicit",
                ["connections"] = connections
            };
        }

        #endregion

        #region Auto Setup Navigation

        private object AutoSetupNavigation(Dictionary<string, object> payload)
        {
            var rootPath = GetString(payload, "rootPath");
            if (string.IsNullOrEmpty(rootPath))
            {
                throw new InvalidOperationException("rootPath is required for autoSetup operation.");
            }

            var rootGo = ResolveGameObject(rootPath);
            var direction = GetString(payload, "direction") ?? "vertical";
            var wrapAround = GetBoolOrDefault(payload, "wrapAround", false);
            var includeDisabled = GetBoolOrDefault(payload, "includeDisabled", false);

            // Find all Selectables under root
            var selectables = rootGo.GetComponentsInChildren<Selectable>(includeDisabled)
                .Where(s => s.interactable || includeDisabled)
                .ToList();

            if (selectables.Count == 0)
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["message"] = "No interactable Selectables found under root."
                };
            }

            // Sort by position based on direction
            if (direction == "vertical")
            {
                selectables = selectables.OrderByDescending(s => s.transform.position.y)
                    .ThenBy(s => s.transform.position.x)
                    .ToList();
            }
            else if (direction == "horizontal")
            {
                selectables = selectables.OrderBy(s => s.transform.position.x)
                    .ThenByDescending(s => s.transform.position.y)
                    .ToList();
            }
            else if (direction == "grid")
            {
                // Sort by row (y) then column (x)
                selectables = selectables.OrderByDescending(s => s.transform.position.y)
                    .ThenBy(s => s.transform.position.x)
                    .ToList();
            }

            var configuredCount = 0;
            var configuredPaths = new List<string>();

            for (int i = 0; i < selectables.Count; i++)
            {
                var selectable = selectables[i];
                var navigation = selectable.navigation;
                navigation.mode = Navigation.Mode.Explicit;

                if (direction == "vertical")
                {
                    // Up/Down navigation
                    navigation.selectOnUp = i > 0 ? selectables[i - 1] : (wrapAround ? selectables[selectables.Count - 1] : null);
                    navigation.selectOnDown = i < selectables.Count - 1 ? selectables[i + 1] : (wrapAround ? selectables[0] : null);
                }
                else if (direction == "horizontal")
                {
                    // Left/Right navigation
                    navigation.selectOnLeft = i > 0 ? selectables[i - 1] : (wrapAround ? selectables[selectables.Count - 1] : null);
                    navigation.selectOnRight = i < selectables.Count - 1 ? selectables[i + 1] : (wrapAround ? selectables[0] : null);
                }
                else if (direction == "grid")
                {
                    // Grid navigation - detect columns
                    var columnsPerRow = GetIntOrDefault(payload, "columns", DetectColumns(selectables));
                    var row = i / columnsPerRow;
                    var col = i % columnsPerRow;
                    var totalRows = (selectables.Count + columnsPerRow - 1) / columnsPerRow;

                    // Up
                    if (row > 0)
                        navigation.selectOnUp = selectables[i - columnsPerRow];
                    else if (wrapAround)
                    {
                        var lastRowStart = (totalRows - 1) * columnsPerRow;
                        var targetIdx = lastRowStart + col;
                        navigation.selectOnUp = targetIdx < selectables.Count ? selectables[targetIdx] : selectables[selectables.Count - 1];
                    }

                    // Down
                    if (i + columnsPerRow < selectables.Count)
                        navigation.selectOnDown = selectables[i + columnsPerRow];
                    else if (wrapAround)
                        navigation.selectOnDown = col < selectables.Count ? selectables[col] : selectables[0];

                    // Left
                    if (col > 0)
                        navigation.selectOnLeft = selectables[i - 1];
                    else if (wrapAround)
                    {
                        var rightMost = Math.Min(i + columnsPerRow - 1, selectables.Count - 1);
                        navigation.selectOnLeft = selectables[rightMost];
                    }

                    // Right
                    if (col < columnsPerRow - 1 && i + 1 < selectables.Count && (i + 1) / columnsPerRow == row)
                        navigation.selectOnRight = selectables[i + 1];
                    else if (wrapAround)
                        navigation.selectOnRight = selectables[row * columnsPerRow];
                }

                selectable.navigation = navigation;
                EditorUtility.SetDirty(selectable);
                configuredCount++;
                configuredPaths.Add(BuildGameObjectPath(selectable.gameObject));
            }

            EditorSceneManager.MarkSceneDirty(rootGo.scene);

            // Set first selected if EventSystem exists
            var eventSystem = UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem != null && selectables.Count > 0)
            {
                eventSystem.firstSelectedGameObject = selectables[0].gameObject;
                EditorUtility.SetDirty(eventSystem);
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["rootPath"] = rootPath,
                ["direction"] = direction,
                ["wrapAround"] = wrapAround,
                ["configuredCount"] = configuredCount,
                ["firstSelected"] = selectables.Count > 0 ? BuildGameObjectPath(selectables[0].gameObject) : null,
                ["configuredPaths"] = configuredPaths
            };
        }

        private int DetectColumns(List<Selectable> selectables)
        {
            if (selectables.Count <= 1) return 1;

            // Detect columns by counting items with same Y position
            var firstY = selectables[0].transform.position.y;
            var tolerance = 10f; // pixels
            var columnsInFirstRow = selectables.TakeWhile(s => Mathf.Abs(s.transform.position.y - firstY) < tolerance).Count();

            return Math.Max(1, columnsInFirstRow);
        }

        #endregion

        #region Create Navigation Group

        private object CreateNavigationGroup(Dictionary<string, object> payload)
        {
            var groupName = GetString(payload, "groupName");
            if (string.IsNullOrEmpty(groupName))
            {
                throw new InvalidOperationException("groupName is required for createGroup operation.");
            }

            if (!payload.TryGetValue("elements", out var elementsObj) || elementsObj is not List<object> elementsList)
            {
                throw new InvalidOperationException("elements array is required for createGroup operation.");
            }

            var elementPaths = elementsList.Select(e => e.ToString()).ToList();
            var direction = GetString(payload, "direction") ?? "vertical";
            var wrapAround = GetBoolOrDefault(payload, "wrapAround", false);
            var isolate = GetBoolOrDefault(payload, "isolate", true);

            var selectables = new List<Selectable>();
            foreach (var path in elementPaths)
            {
                var go = GameObject.Find(path);
                if (go != null)
                {
                    var selectable = go.GetComponent<Selectable>();
                    if (selectable != null)
                        selectables.Add(selectable);
                }
            }

            if (selectables.Count == 0)
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["message"] = "No valid Selectables found in the elements list."
                };
            }

            // Configure navigation within group
            for (int i = 0; i < selectables.Count; i++)
            {
                var selectable = selectables[i];
                var navigation = selectable.navigation;
                navigation.mode = Navigation.Mode.Explicit;

                if (isolate)
                {
                    // Clear all navigation
                    navigation.selectOnUp = null;
                    navigation.selectOnDown = null;
                    navigation.selectOnLeft = null;
                    navigation.selectOnRight = null;
                }

                if (direction == "vertical" || direction == "both")
                {
                    navigation.selectOnUp = i > 0 ? selectables[i - 1] : (wrapAround ? selectables[selectables.Count - 1] : navigation.selectOnUp);
                    navigation.selectOnDown = i < selectables.Count - 1 ? selectables[i + 1] : (wrapAround ? selectables[0] : navigation.selectOnDown);
                }

                if (direction == "horizontal" || direction == "both")
                {
                    navigation.selectOnLeft = i > 0 ? selectables[i - 1] : (wrapAround ? selectables[selectables.Count - 1] : navigation.selectOnLeft);
                    navigation.selectOnRight = i < selectables.Count - 1 ? selectables[i + 1] : (wrapAround ? selectables[0] : navigation.selectOnRight);
                }

                selectable.navigation = navigation;
                EditorUtility.SetDirty(selectable);
            }

            if (selectables.Count > 0)
            {
                EditorSceneManager.MarkSceneDirty(selectables[0].gameObject.scene);
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["groupName"] = groupName,
                ["direction"] = direction,
                ["wrapAround"] = wrapAround,
                ["isolate"] = isolate,
                ["elementCount"] = selectables.Count,
                ["elements"] = selectables.Select(s => BuildGameObjectPath(s.gameObject)).ToList()
            };
        }

        #endregion

        #region Set First Selected

        private object SetFirstSelected(Dictionary<string, object> payload)
        {
            var gameObjectPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required for setFirstSelected operation.");
            }

            var go = ResolveGameObject(gameObjectPath);
            var selectable = go.GetComponent<Selectable>();
            if (selectable == null)
            {
                throw new InvalidOperationException($"GameObject '{gameObjectPath}' does not have a Selectable component.");
            }

            var eventSystem = UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                throw new InvalidOperationException("No EventSystem found in the scene.");
            }

            eventSystem.firstSelectedGameObject = go;
            EditorUtility.SetDirty(eventSystem);
            EditorSceneManager.MarkSceneDirty(go.scene);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["gameObjectPath"] = gameObjectPath,
                ["message"] = $"Set '{gameObjectPath}' as first selected."
            };
        }

        #endregion

        #region Inspect Navigation

        private object InspectNavigation(Dictionary<string, object> payload)
        {
            var gameObjectPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required for inspect operation.");
            }

            var go = ResolveGameObject(gameObjectPath);
            var selectable = go.GetComponent<Selectable>();
            if (selectable == null)
            {
                throw new InvalidOperationException($"GameObject '{gameObjectPath}' does not have a Selectable component.");
            }

            var navigation = selectable.navigation;

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["gameObjectPath"] = gameObjectPath,
                ["mode"] = navigation.mode.ToString(),
                ["wrapAround"] = navigation.wrapAround,
                ["selectOnUp"] = navigation.selectOnUp != null ? BuildGameObjectPath(navigation.selectOnUp.gameObject) : null,
                ["selectOnDown"] = navigation.selectOnDown != null ? BuildGameObjectPath(navigation.selectOnDown.gameObject) : null,
                ["selectOnLeft"] = navigation.selectOnLeft != null ? BuildGameObjectPath(navigation.selectOnLeft.gameObject) : null,
                ["selectOnRight"] = navigation.selectOnRight != null ? BuildGameObjectPath(navigation.selectOnRight.gameObject) : null,
                ["interactable"] = selectable.interactable
            };
        }

        #endregion

        #region Reset Navigation

        private object ResetNavigation(Dictionary<string, object> payload)
        {
            var gameObjectPath = GetString(payload, "gameObjectPath");
            var recursive = GetBoolOrDefault(payload, "recursive", false);

            if (string.IsNullOrEmpty(gameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required for reset operation.");
            }

            var go = ResolveGameObject(gameObjectPath);
            var resetCount = 0;

            if (recursive)
            {
                var selectables = go.GetComponentsInChildren<Selectable>(true);
                foreach (var selectable in selectables)
                {
                    ResetSelectableNavigation(selectable);
                    resetCount++;
                }
            }
            else
            {
                var selectable = go.GetComponent<Selectable>();
                if (selectable != null)
                {
                    ResetSelectableNavigation(selectable);
                    resetCount = 1;
                }
            }

            EditorSceneManager.MarkSceneDirty(go.scene);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["gameObjectPath"] = gameObjectPath,
                ["recursive"] = recursive,
                ["resetCount"] = resetCount
            };
        }

        private void ResetSelectableNavigation(Selectable selectable)
        {
            var navigation = selectable.navigation;
            navigation.mode = Navigation.Mode.Automatic;
            navigation.wrapAround = false;
            navigation.selectOnUp = null;
            navigation.selectOnDown = null;
            navigation.selectOnLeft = null;
            navigation.selectOnRight = null;
            selectable.navigation = navigation;
            EditorUtility.SetDirty(selectable);
        }

        #endregion

        #region Disable Navigation

        private object DisableNavigation(Dictionary<string, object> payload)
        {
            var gameObjectPath = GetString(payload, "gameObjectPath");
            var recursive = GetBoolOrDefault(payload, "recursive", false);

            if (string.IsNullOrEmpty(gameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required for disable operation.");
            }

            var go = ResolveGameObject(gameObjectPath);
            var disabledCount = 0;

            if (recursive)
            {
                var selectables = go.GetComponentsInChildren<Selectable>(true);
                foreach (var selectable in selectables)
                {
                    var navigation = selectable.navigation;
                    navigation.mode = Navigation.Mode.None;
                    selectable.navigation = navigation;
                    EditorUtility.SetDirty(selectable);
                    disabledCount++;
                }
            }
            else
            {
                var selectable = go.GetComponent<Selectable>();
                if (selectable != null)
                {
                    var navigation = selectable.navigation;
                    navigation.mode = Navigation.Mode.None;
                    selectable.navigation = navigation;
                    EditorUtility.SetDirty(selectable);
                    disabledCount = 1;
                }
            }

            EditorSceneManager.MarkSceneDirty(go.scene);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["gameObjectPath"] = gameObjectPath,
                ["recursive"] = recursive,
                ["disabledCount"] = disabledCount
            };
        }

        #endregion

        #region Helper Methods

        private string BuildGameObjectPath(GameObject go)
        {
            var path = go.name;
            var parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        private bool GetBoolOrDefault(Dictionary<string, object> dict, string key, bool defaultValue)
        {
            if (dict.TryGetValue(key, out var value))
            {
                if (value is bool boolVal) return boolVal;
                if (value is string strVal && bool.TryParse(strVal, out var parsed)) return parsed;
            }
            return defaultValue;
        }

        private int GetIntOrDefault(Dictionary<string, object> dict, string key, int defaultValue)
        {
            if (dict.TryGetValue(key, out var value))
            {
                if (value is int intVal) return intVal;
                if (value is long longVal) return (int)longVal;
                if (value is float floatVal) return (int)floatVal;
                if (value is double doubleVal) return (int)doubleVal;
                if (value is string strVal && int.TryParse(strVal, out var parsed)) return parsed;
            }
            return defaultValue;
        }

        #endregion
    }
}
