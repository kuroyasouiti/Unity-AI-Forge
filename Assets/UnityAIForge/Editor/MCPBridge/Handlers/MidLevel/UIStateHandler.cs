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
    /// Mid-level UI state management handler: manage UI states (active/visible),
    /// state groups, state transitions, and state persistence.
    /// </summary>
    public class UIStateHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "defineState",
            "applyState",
            "saveState",
            "loadState",
            "listStates",
            "deleteState",
            "createStateGroup",
            "transitionTo",
            "getActiveState",
        };

        public override string Category => "uiState";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "defineState" => DefineState(payload),
                "applyState" => ApplyState(payload),
                "saveState" => SaveCurrentState(payload),
                "loadState" => LoadState(payload),
                "listStates" => ListStates(payload),
                "deleteState" => DeleteState(payload),
                "createStateGroup" => CreateStateGroup(payload),
                "transitionTo" => TransitionTo(payload),
                "getActiveState" => GetActiveState(payload),
                _ => throw new InvalidOperationException($"Unsupported UI state operation: {operation}"),
            };
        }

        #region Define State

        private object DefineState(Dictionary<string, object> payload)
        {
            var stateName = GetString(payload, "stateName");
            if (string.IsNullOrEmpty(stateName))
            {
                throw new InvalidOperationException("stateName is required for defineState operation.");
            }

            var rootPath = GetString(payload, "rootPath");
            if (string.IsNullOrEmpty(rootPath))
            {
                throw new InvalidOperationException("rootPath is required for defineState operation.");
            }

            var rootGo = ResolveGameObject(rootPath);

            // Get elements configuration
            if (!payload.TryGetValue("elements", out var elementsObj) || elementsObj is not List<object> elementsList)
            {
                throw new InvalidOperationException("elements array is required for defineState operation.");
            }

            var stateData = new UIStateData
            {
                StateName = stateName,
                RootPath = rootPath,
                Elements = new List<UIElementState>()
            };

            foreach (var elemObj in elementsList)
            {
                if (elemObj is Dictionary<string, object> elemDict)
                {
                    var elementState = new UIElementState
                    {
                        Path = GetString(elemDict, "path") ?? "",
                        Active = GetBool(elemDict, "active", true),
                        Visible = GetBool(elemDict, "visible", true),
                        Interactable = GetBool(elemDict, "interactable", true),
                        Alpha = GetFloat(elemDict, "alpha", 1f),
                        BlocksRaycasts = GetBool(elemDict, "blocksRaycasts", true),
                        IgnoreParentGroups = GetBool(elemDict, "ignoreParentGroups", false)
                    };

                    // Optional position/size overrides
                    if (elemDict.TryGetValue("anchoredPosition", out var posObj) && posObj is Dictionary<string, object> posDict)
                    {
                        elementState.AnchoredPosition = new Vector2(
                            GetFloat(posDict, "x", 0),
                            GetFloat(posDict, "y", 0)
                        );
                        elementState.HasPositionOverride = true;
                    }

                    if (elemDict.TryGetValue("sizeDelta", out var sizeObj) && sizeObj is Dictionary<string, object> sizeDict)
                    {
                        elementState.SizeDelta = new Vector2(
                            GetFloat(sizeDict, "x", 0),
                            GetFloat(sizeDict, "y", 0)
                        );
                        elementState.HasSizeOverride = true;
                    }

                    stateData.Elements.Add(elementState);
                }
            }

            // Store state in EditorPrefs (JSON serialized)
            var stateKey = GetStateKey(rootPath, stateName);
            var json = JsonUtility.ToJson(stateData);
            EditorPrefs.SetString(stateKey, json);
            AddToRegistry(rootPath, stateName);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["stateName"] = stateName,
                ["rootPath"] = rootPath,
                ["elementCount"] = stateData.Elements.Count,
                ["message"] = $"State '{stateName}' defined with {stateData.Elements.Count} elements."
            };
        }

        #endregion

        #region Apply State

        private object ApplyState(Dictionary<string, object> payload)
        {
            var stateName = GetString(payload, "stateName");
            if (string.IsNullOrEmpty(stateName))
            {
                throw new InvalidOperationException("stateName is required for applyState operation.");
            }

            var rootPath = GetString(payload, "rootPath");
            if (string.IsNullOrEmpty(rootPath))
            {
                throw new InvalidOperationException("rootPath is required for applyState operation.");
            }

            var rootGo = ResolveGameObject(rootPath);
            var stateKey = GetStateKey(rootPath, stateName);

            if (!EditorPrefs.HasKey(stateKey))
            {
                throw new InvalidOperationException($"State '{stateName}' not found for root '{rootPath}'.");
            }

            var json = EditorPrefs.GetString(stateKey);
            var stateData = JsonUtility.FromJson<UIStateData>(json);

            var appliedCount = 0;
            var errors = new List<string>();

            foreach (var elemState in stateData.Elements)
            {
                try
                {
                    var targetPath = string.IsNullOrEmpty(elemState.Path)
                        ? rootPath
                        : $"{rootPath}/{elemState.Path}";

                    Transform targetTransform;
                    if (string.IsNullOrEmpty(elemState.Path))
                        targetTransform = rootGo.transform;
                    else
                        targetTransform = rootGo.transform.Find(elemState.Path);
                    var targetGo = targetTransform?.gameObject;
                    if (targetGo == null)
                    {
                        errors.Add($"Element not found: {targetPath}");
                        continue;
                    }

                    // Apply active state
                    Undo.RecordObject(targetGo, "Apply UI State");
                    targetGo.SetActive(elemState.Active);

                    // Apply CanvasGroup settings if visible/alpha/interactable are specified
                    var canvasGroup = targetGo.GetComponent<CanvasGroup>();
                    if (canvasGroup == null && (!elemState.Visible || elemState.Alpha < 1f || !elemState.Interactable || !elemState.BlocksRaycasts || elemState.IgnoreParentGroups))
                    {
                        canvasGroup = Undo.AddComponent<CanvasGroup>(targetGo);
                    }

                    if (canvasGroup != null)
                    {
                        Undo.RecordObject(canvasGroup, "Apply UI State");
                        canvasGroup.alpha = elemState.Visible ? elemState.Alpha : 0f;
                        canvasGroup.interactable = elemState.Interactable;
                        canvasGroup.blocksRaycasts = elemState.BlocksRaycasts;
                        canvasGroup.ignoreParentGroups = elemState.IgnoreParentGroups;
                    }

                    // Apply position/size overrides
                    var rectTransform = targetGo.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        Undo.RecordObject(rectTransform, "Apply UI State");
                        if (elemState.HasPositionOverride)
                        {
                            rectTransform.anchoredPosition = elemState.AnchoredPosition;
                        }
                        if (elemState.HasSizeOverride)
                        {
                            rectTransform.sizeDelta = elemState.SizeDelta;
                        }
                    }

                    appliedCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Error applying state to '{elemState.Path}': {ex.Message}");
                }
            }

            EditorSceneManager.MarkSceneDirty(rootGo.scene);

            // Store active state
            SetActiveStateName(rootPath, stateName);

            return new Dictionary<string, object>
            {
                ["success"] = errors.Count == 0,
                ["stateName"] = stateName,
                ["appliedCount"] = appliedCount,
                ["totalElements"] = stateData.Elements.Count,
                ["errors"] = errors
            };
        }

        #endregion

        #region Save Current State

        private object SaveCurrentState(Dictionary<string, object> payload)
        {
            var stateName = GetString(payload, "stateName");
            if (string.IsNullOrEmpty(stateName))
            {
                throw new InvalidOperationException("stateName is required for saveState operation.");
            }

            var rootPath = GetString(payload, "rootPath");
            if (string.IsNullOrEmpty(rootPath))
            {
                throw new InvalidOperationException("rootPath is required for saveState operation.");
            }

            var rootGo = ResolveGameObject(rootPath);
            var includeChildren = GetBool(payload, "includeChildren", true);
            var maxDepth = GetInt(payload, "maxDepth", 10);

            var stateData = new UIStateData
            {
                StateName = stateName,
                RootPath = rootPath,
                Elements = new List<UIElementState>()
            };

            // Capture current state
            CaptureElementState(rootGo, rootPath, "", stateData.Elements, includeChildren, maxDepth, 0);

            // Store state
            var stateKey = GetStateKey(rootPath, stateName);
            var json = JsonUtility.ToJson(stateData);
            EditorPrefs.SetString(stateKey, json);
            AddToRegistry(rootPath, stateName);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["stateName"] = stateName,
                ["rootPath"] = rootPath,
                ["capturedCount"] = stateData.Elements.Count,
                ["message"] = $"Current state saved as '{stateName}' with {stateData.Elements.Count} elements."
            };
        }

        private void CaptureElementState(GameObject go, string rootPath, string relativePath, List<UIElementState> elements, bool includeChildren, int maxDepth, int currentDepth)
        {
            var canvasGroup = go.GetComponent<CanvasGroup>();
            var rectTransform = go.GetComponent<RectTransform>();

            var elemState = new UIElementState
            {
                Path = relativePath,
                Active = go.activeSelf,
                Visible = canvasGroup == null || canvasGroup.alpha > 0,
                Interactable = canvasGroup == null || canvasGroup.interactable,
                Alpha = canvasGroup?.alpha ?? 1f,
                BlocksRaycasts = canvasGroup == null || canvasGroup.blocksRaycasts,
                IgnoreParentGroups = canvasGroup != null && canvasGroup.ignoreParentGroups
            };

            if (rectTransform != null)
            {
                elemState.AnchoredPosition = rectTransform.anchoredPosition;
                elemState.SizeDelta = rectTransform.sizeDelta;
                elemState.HasPositionOverride = true;
                elemState.HasSizeOverride = true;
            }

            elements.Add(elemState);

            // Capture children
            if (includeChildren && currentDepth < maxDepth)
            {
                foreach (Transform child in go.transform)
                {
                    var childPath = string.IsNullOrEmpty(relativePath) ? child.name : $"{relativePath}/{child.name}";
                    CaptureElementState(child.gameObject, rootPath, childPath, elements, includeChildren, maxDepth, currentDepth + 1);
                }
            }
        }

        #endregion

        #region Load State

        private object LoadState(Dictionary<string, object> payload)
        {
            var stateName = GetString(payload, "stateName");
            if (string.IsNullOrEmpty(stateName))
            {
                throw new InvalidOperationException("stateName is required for loadState operation.");
            }

            var rootPath = GetString(payload, "rootPath");
            if (string.IsNullOrEmpty(rootPath))
            {
                throw new InvalidOperationException("rootPath is required for loadState operation.");
            }

            var stateKey = GetStateKey(rootPath, stateName);

            if (!EditorPrefs.HasKey(stateKey))
            {
                throw new InvalidOperationException($"State '{stateName}' not found for root '{rootPath}'.");
            }

            var json = EditorPrefs.GetString(stateKey);
            var stateData = JsonUtility.FromJson<UIStateData>(json);

            var elementsInfo = stateData.Elements.Select(e => new Dictionary<string, object>
            {
                ["path"] = e.Path,
                ["active"] = e.Active,
                ["visible"] = e.Visible,
                ["interactable"] = e.Interactable,
                ["alpha"] = e.Alpha,
                ["blocksRaycasts"] = e.BlocksRaycasts,
                ["ignoreParentGroups"] = e.IgnoreParentGroups,
                ["hasPositionOverride"] = e.HasPositionOverride,
                ["hasSizeOverride"] = e.HasSizeOverride
            }).ToList();

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["stateName"] = stateData.StateName,
                ["rootPath"] = stateData.RootPath,
                ["elementCount"] = stateData.Elements.Count,
                ["elements"] = elementsInfo
            };
        }

        #endregion

        #region List States

        private object ListStates(Dictionary<string, object> payload)
        {
            var rootPath = GetString(payload, "rootPath");
            var prefix = string.IsNullOrEmpty(rootPath)
                ? "UIState_"
                : $"UIState_{rootPath.Replace("/", "_")}_";

            var states = new List<Dictionary<string, object>>();

            // EditorPrefs doesn't have a way to enumerate keys, so we use a registry key
            var registryKey = $"UIStateRegistry_{(string.IsNullOrEmpty(rootPath) ? "all" : rootPath.Replace("/", "_"))}";
            var registry = EditorPrefs.GetString(registryKey, "");
            var stateNames = string.IsNullOrEmpty(registry)
                ? new string[0]
                : registry.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var stateName in stateNames)
            {
                var stateKey = GetStateKey(rootPath, stateName);
                if (EditorPrefs.HasKey(stateKey))
                {
                    var json = EditorPrefs.GetString(stateKey);
                    var stateData = JsonUtility.FromJson<UIStateData>(json);
                    states.Add(new Dictionary<string, object>
                    {
                        ["stateName"] = stateData.StateName,
                        ["rootPath"] = stateData.RootPath,
                        ["elementCount"] = stateData.Elements.Count
                    });
                }
            }

            var activeState = GetActiveStateName(rootPath);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["rootPath"] = rootPath ?? "all",
                ["stateCount"] = states.Count,
                ["activeState"] = activeState,
                ["states"] = states
            };
        }

        #endregion

        #region Delete State

        private object DeleteState(Dictionary<string, object> payload)
        {
            var stateName = GetString(payload, "stateName");
            if (string.IsNullOrEmpty(stateName))
            {
                throw new InvalidOperationException("stateName is required for deleteState operation.");
            }

            var rootPath = GetString(payload, "rootPath");
            if (string.IsNullOrEmpty(rootPath))
            {
                throw new InvalidOperationException("rootPath is required for deleteState operation.");
            }

            var stateKey = GetStateKey(rootPath, stateName);

            if (!EditorPrefs.HasKey(stateKey))
            {
                throw new InvalidOperationException($"State '{stateName}' not found for root '{rootPath}'.");
            }

            EditorPrefs.DeleteKey(stateKey);
            RemoveFromRegistry(rootPath, stateName);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["stateName"] = stateName,
                ["rootPath"] = rootPath,
                ["message"] = $"State '{stateName}' deleted."
            };
        }

        #endregion

        #region State Group

        private object CreateStateGroup(Dictionary<string, object> payload)
        {
            var groupName = GetString(payload, "groupName");
            if (string.IsNullOrEmpty(groupName))
            {
                throw new InvalidOperationException("groupName is required for createStateGroup operation.");
            }

            var rootPath = GetString(payload, "rootPath");
            if (string.IsNullOrEmpty(rootPath))
            {
                throw new InvalidOperationException("rootPath is required for createStateGroup operation.");
            }

            if (!payload.TryGetValue("states", out var statesObj) || statesObj is not List<object> statesList)
            {
                throw new InvalidOperationException("states array is required for createStateGroup operation.");
            }

            var stateNames = statesList.Select(s => s.ToString()).ToList();
            var defaultState = GetString(payload, "defaultState") ?? (stateNames.Count > 0 ? stateNames[0] : "");

            var groupData = new UIStateGroupData
            {
                GroupName = groupName,
                RootPath = rootPath,
                StateNames = stateNames,
                DefaultState = defaultState
            };

            var groupKey = $"UIStateGroup_{rootPath.Replace("/", "_")}_{groupName}";
            var json = JsonUtility.ToJson(groupData);
            EditorPrefs.SetString(groupKey, json);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["groupName"] = groupName,
                ["rootPath"] = rootPath,
                ["stateCount"] = stateNames.Count,
                ["defaultState"] = defaultState,
                ["states"] = stateNames
            };
        }

        #endregion

        #region Transition To

        private object TransitionTo(Dictionary<string, object> payload)
        {
            var stateName = GetString(payload, "stateName");
            if (string.IsNullOrEmpty(stateName))
            {
                throw new InvalidOperationException("stateName is required for transitionTo operation.");
            }

            var rootPath = GetString(payload, "rootPath");
            if (string.IsNullOrEmpty(rootPath))
            {
                throw new InvalidOperationException("rootPath is required for transitionTo operation.");
            }

            // For now, transition is just apply (no animation in Editor)
            // In future, this could support tween/animation preview
            var result = ApplyState(new Dictionary<string, object>
            {
                ["stateName"] = stateName,
                ["rootPath"] = rootPath
            });

            if (result is Dictionary<string, object> resultDict)
            {
                resultDict["operation"] = "transitionTo";
                resultDict["message"] = $"Transitioned to state '{stateName}'.";
            }

            return result;
        }

        #endregion

        #region Get Active State

        private object GetActiveState(Dictionary<string, object> payload)
        {
            var rootPath = GetString(payload, "rootPath");
            if (string.IsNullOrEmpty(rootPath))
            {
                throw new InvalidOperationException("rootPath is required for getActiveState operation.");
            }

            var activeState = GetActiveStateName(rootPath);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["rootPath"] = rootPath,
                ["activeState"] = activeState ?? "none",
                ["hasActiveState"] = !string.IsNullOrEmpty(activeState)
            };
        }

        #endregion

        #region Helper Methods

        private string GetStateKey(string rootPath, string stateName)
        {
            var safePath = string.IsNullOrEmpty(rootPath) ? "root" : rootPath.Replace("/", "_");
            return $"UIState_{safePath}_{stateName}";
        }

        private void AddToRegistry(string rootPath, string stateName)
        {
            var registryKey = $"UIStateRegistry_{(string.IsNullOrEmpty(rootPath) ? "all" : rootPath.Replace("/", "_"))}";
            var registry = EditorPrefs.GetString(registryKey, "");
            var stateNames = string.IsNullOrEmpty(registry)
                ? new List<string>()
                : registry.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (!stateNames.Contains(stateName))
            {
                stateNames.Add(stateName);
                EditorPrefs.SetString(registryKey, string.Join(",", stateNames));
            }
        }

        private void RemoveFromRegistry(string rootPath, string stateName)
        {
            var registryKey = $"UIStateRegistry_{(string.IsNullOrEmpty(rootPath) ? "all" : rootPath.Replace("/", "_"))}";
            var registry = EditorPrefs.GetString(registryKey, "");
            var stateNames = string.IsNullOrEmpty(registry)
                ? new List<string>()
                : registry.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            stateNames.Remove(stateName);
            EditorPrefs.SetString(registryKey, string.Join(",", stateNames));
        }

        private void SetActiveStateName(string rootPath, string stateName)
        {
            var activeKey = $"UIStateActive_{(string.IsNullOrEmpty(rootPath) ? "root" : rootPath.Replace("/", "_"))}";
            EditorPrefs.SetString(activeKey, stateName);
        }

        private string GetActiveStateName(string rootPath)
        {
            var activeKey = $"UIStateActive_{(string.IsNullOrEmpty(rootPath) ? "root" : rootPath.Replace("/", "_"))}";
            return EditorPrefs.GetString(activeKey, "");
        }

        // BuildGameObjectPath is inherited from BaseCommandHandler
        // GetBool (GetBoolOrDefault) is inherited from BaseCommandHandler
        // GetFloat (GetFloatOrDefault) is inherited from BaseCommandHandler
        // GetInt (GetIntOrDefault) is inherited from BaseCommandHandler

        #endregion

        #region Data Classes

        [Serializable]
        private class UIStateData
        {
            public string StateName;
            public string RootPath;
            public List<UIElementState> Elements = new List<UIElementState>();
        }

        [Serializable]
        private class UIElementState
        {
            public string Path;
            public bool Active = true;
            public bool Visible = true;
            public bool Interactable = true;
            public float Alpha = 1f;
            public bool BlocksRaycasts = true;
            public bool IgnoreParentGroups = false;
            public Vector2 AnchoredPosition;
            public Vector2 SizeDelta;
            public bool HasPositionOverride;
            public bool HasSizeOverride;
        }

        [Serializable]
        private class UIStateGroupData
        {
            public string GroupName;
            public string RootPath;
            public List<string> StateNames = new List<string>();
            public string DefaultState;
        }

        #endregion
    }
}
