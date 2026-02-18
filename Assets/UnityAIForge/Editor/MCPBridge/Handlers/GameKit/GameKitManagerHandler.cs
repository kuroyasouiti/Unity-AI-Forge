using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Manager handler: create and manage game manager hubs.
    /// Uses code generation to produce standalone Manager scripts with zero package dependency.
    /// </summary>
    public class GameKitManagerHandler : BaseCommandHandler
    {
        private static readonly string[] Operations = { "create", "update", "inspect", "delete" };

        public override string Category => "gamekitManager";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateManager(payload),
                "update" => UpdateManager(payload),
                "inspect" => InspectManager(payload),
                "delete" => DeleteManager(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit Manager operation: {operation}"),
            };
        }

        #region Create Manager

        private object CreateManager(Dictionary<string, object> payload)
        {
            var managerId = GetString(payload, "managerId") ?? $"Manager_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var parentPath = GetString(payload, "parentPath");
            var typeStr = GetString(payload, "managerType") ?? "realtime";
            var persistent = GetBool(payload, "persistent");

            // Create GameObject
            var managerGo = new GameObject(managerId);
            Undo.RegisterCreatedObjectUndo(managerGo, "Create GameKit Manager");

            if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = ResolveGameObject(parentPath);
                managerGo.transform.SetParent(parent.transform, false);
            }

            var managerType = ParseManagerType(typeStr);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(managerId, "Manager");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "MANAGER_ID", managerId },
                { "MANAGER_TYPE", managerType },
                { "PERSISTENT", persistent.ToString().ToLowerInvariant() }
            };

            var outputDir = GetString(payload, "outputPath");

            // Build properties to set after component is added
            var propertiesToSet = new Dictionary<string, object>();

            // Turn phases
            if (payload.TryGetValue("turnPhases", out var phasesObj) && phasesObj is List<object> phasesList)
            {
                // Will set phases after component is added via SerializedObject
            }

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                managerGo, "Manager", managerId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate Manager script.");
            }

            // If component was added, set turn phases and resources
            if (result.TryGetValue("componentAdded", out var added) && (bool)added)
            {
                var component = CodeGenHelper.FindComponentByField(managerGo, "managerId", managerId);
                if (component != null)
                {
                    var so = new SerializedObject(component);

                    // Set turn phases
                    if (payload.TryGetValue("turnPhases", out var phasesObj2) && phasesObj2 is List<object> phasesList2)
                    {
                        var phasesProp = so.FindProperty("turnPhases");
                        if (phasesProp != null)
                        {
                            phasesProp.ClearArray();
                            for (int i = 0; i < phasesList2.Count; i++)
                            {
                                phasesProp.InsertArrayElementAtIndex(i);
                                phasesProp.GetArrayElementAtIndex(i).stringValue = phasesList2[i].ToString();
                            }
                        }
                    }

                    // Set initial resources
                    if (payload.TryGetValue("initialResources", out var resourcesObj) && resourcesObj is Dictionary<string, object> resourcesDict)
                    {
                        var resourcesProp = so.FindProperty("resources");
                        if (resourcesProp != null)
                        {
                            resourcesProp.ClearArray();
                            int idx = 0;
                            foreach (var kvp in resourcesDict)
                            {
                                resourcesProp.InsertArrayElementAtIndex(idx);
                                var entry = resourcesProp.GetArrayElementAtIndex(idx);
                                entry.FindPropertyRelative("name").stringValue = kvp.Key;
                                entry.FindPropertyRelative("amount").floatValue = Convert.ToSingle(kvp.Value);
                                idx++;
                            }
                        }
                    }

                    so.ApplyModifiedProperties();
                }
            }

            EditorSceneManager.MarkSceneDirty(managerGo.scene);

            result["managerId"] = managerId;
            result["path"] = BuildGameObjectPath(managerGo);
            result["managerType"] = managerType;

            return result;
        }

        #endregion

        #region Update Manager

        private object UpdateManager(Dictionary<string, object> payload)
        {
            var component = ResolveManagerComponent(payload);

            Undo.RecordObject(component, "Update Manager");

            var so = new SerializedObject(component);

            // Update turn phases
            if (payload.TryGetValue("turnPhases", out var phasesObj) && phasesObj is List<object> phasesList)
            {
                var phasesProp = so.FindProperty("turnPhases");
                if (phasesProp != null)
                {
                    // Append new phases that don't already exist
                    foreach (var phase in phasesList)
                    {
                        var phaseStr = phase.ToString();
                        bool exists = false;
                        for (int i = 0; i < phasesProp.arraySize; i++)
                        {
                            if (phasesProp.GetArrayElementAtIndex(i).stringValue == phaseStr)
                            {
                                exists = true;
                                break;
                            }
                        }
                        if (!exists)
                        {
                            phasesProp.InsertArrayElementAtIndex(phasesProp.arraySize);
                            phasesProp.GetArrayElementAtIndex(phasesProp.arraySize - 1).stringValue = phaseStr;
                        }
                    }
                }
            }

            // Update resources
            if (payload.TryGetValue("initialResources", out var resourcesObj) && resourcesObj is Dictionary<string, object> resourcesDict)
            {
                var resourcesProp = so.FindProperty("resources");
                if (resourcesProp != null)
                {
                    foreach (var kvp in resourcesDict)
                    {
                        // Find existing resource entry or create new
                        bool found = false;
                        for (int i = 0; i < resourcesProp.arraySize; i++)
                        {
                            var entry = resourcesProp.GetArrayElementAtIndex(i);
                            if (entry.FindPropertyRelative("name").stringValue == kvp.Key)
                            {
                                entry.FindPropertyRelative("amount").floatValue = Convert.ToSingle(kvp.Value);
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            resourcesProp.InsertArrayElementAtIndex(resourcesProp.arraySize);
                            var newEntry = resourcesProp.GetArrayElementAtIndex(resourcesProp.arraySize - 1);
                            newEntry.FindPropertyRelative("name").stringValue = kvp.Key;
                            newEntry.FindPropertyRelative("amount").floatValue = Convert.ToSingle(kvp.Value);
                        }
                    }
                }
            }

            if (payload.TryGetValue("persistent", out var persistObj))
                so.FindProperty("persistent").boolValue = Convert.ToBoolean(persistObj);

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var managerId = new SerializedObject(component).FindProperty("managerId").stringValue;

            return CreateSuccessResponse(
                ("managerId", managerId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect Manager

        private object InspectManager(Dictionary<string, object> payload)
        {
            var component = ResolveManagerComponent(payload);
            var so = new SerializedObject(component);

            var typeProp = so.FindProperty("managerType");

            // Read turn phases
            var turnPhases = new List<string>();
            var phasesProp = so.FindProperty("turnPhases");
            if (phasesProp != null)
            {
                for (int i = 0; i < phasesProp.arraySize; i++)
                    turnPhases.Add(phasesProp.GetArrayElementAtIndex(i).stringValue);
            }

            // Read resources
            var resources = new Dictionary<string, float>();
            var resourcesProp = so.FindProperty("resources");
            if (resourcesProp != null)
            {
                for (int i = 0; i < resourcesProp.arraySize; i++)
                {
                    var entry = resourcesProp.GetArrayElementAtIndex(i);
                    var name = entry.FindPropertyRelative("name").stringValue;
                    var amount = entry.FindPropertyRelative("amount").floatValue;
                    if (!string.IsNullOrEmpty(name))
                        resources[name] = amount;
                }
            }

            var info = new Dictionary<string, object>
            {
                { "found", true },
                { "managerId", so.FindProperty("managerId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "managerType", typeProp.enumValueIndex < typeProp.enumDisplayNames.Length
                    ? typeProp.enumDisplayNames[typeProp.enumValueIndex] : "Realtime" },
                { "persistent", so.FindProperty("persistent").boolValue },
                { "turnPhases", turnPhases },
                { "resources", resources }
            };

            return CreateSuccessResponse(("manager", info));
        }

        #endregion

        #region Delete Manager

        private object DeleteManager(Dictionary<string, object> payload)
        {
            var component = ResolveManagerComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var managerId = new SerializedObject(component).FindProperty("managerId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component.gameObject);

            // Clean up generated script from tracker
            ScriptGenerator.Delete(managerId);

            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("managerId", managerId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Helpers

        private Component ResolveManagerComponent(Dictionary<string, object> payload)
        {
            // Try by managerId first
            var managerId = GetString(payload, "managerId");
            if (!string.IsNullOrEmpty(managerId))
            {
                var managerById = CodeGenHelper.FindComponentInSceneByField("managerId", managerId);
                if (managerById != null)
                    return managerById;
            }

            // Try by targetPath
            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var managerByPath = CodeGenHelper.FindComponentByField(targetGo, "managerId", null);
                    if (managerByPath != null)
                        return managerByPath;

                    throw new InvalidOperationException($"No Manager component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either managerId or targetPath is required.");
        }

        private string ParseManagerType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "turnbased" => "TurnBased",
                "realtime" => "Realtime",
                "resourcepool" => "ResourcePool",
                "eventhub" => "EventHub",
                "statemanager" => "StateManager",
                _ => "Realtime"
            };
        }

        #endregion
    }
}
