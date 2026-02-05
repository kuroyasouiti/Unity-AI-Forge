using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using UnityAIForge.GameKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Manager handler: create and manage game manager hubs.
    /// Creates GameKitManager which acts as a hub and automatically adds mode-specific components:
    /// - TurnBased → GameKitTurnManager
    /// - ResourcePool → GameKitResourceManager
    /// - EventHub → GameKitEventManager
    /// - StateManager → GameKitStateManager
    /// - Realtime → GameKitRealtimeManager
    /// </summary>
    public class GameKitManagerHandler : BaseCommandHandler
    {
        private static readonly string[] Operations = { "create", "update", "inspect", "delete" };

        public override string Category => "gamekitManager";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

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

            // Add GameKitManager component (hub)
            var manager = Undo.AddComponent<GameKitManager>(managerGo);
            var type = ParseManagerType(typeStr);
            manager.Initialize(managerId, type, persistent);
            // Initialize automatically attaches mode-specific component

            // Configure mode-specific component based on type
            if (type == GameKitManager.ManagerType.TurnBased)
            {
                if (payload.TryGetValue("turnPhases", out var phasesObj) && phasesObj is List<object> phasesList)
                {
                    foreach (var phase in phasesList)
                    {
                        manager.AddTurnPhase(phase.ToString());
                    }
                }
            }

            if (type == GameKitManager.ManagerType.ResourcePool)
            {
                if (payload.TryGetValue("initialResources", out var resourcesObj) && resourcesObj is Dictionary<string, object> resourcesDict)
                {
                    foreach (var kvp in resourcesDict)
                    {
                        manager.SetResource(kvp.Key, Convert.ToSingle(kvp.Value));
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(managerGo.scene);
            return CreateSuccessResponse(("managerId", managerId), ("path", BuildGameObjectPath(managerGo)));
        }

        #endregion

        #region Update Manager

        private object UpdateManager(Dictionary<string, object> payload)
        {
            var managerId = GetString(payload, "managerId");
            if (string.IsNullOrEmpty(managerId))
            {
                throw new InvalidOperationException("managerId is required for update.");
            }

            var manager = FindManagerById(managerId);
            if (manager == null)
            {
                throw new InvalidOperationException($"Manager with ID '{managerId}' not found.");
            }

            Undo.RecordObject(manager, "Update GameKit Manager");

            // Update turn phases
            if (payload.TryGetValue("turnPhases", out var phasesObj) && phasesObj is List<object> phasesList)
            {
                foreach (var phase in phasesList)
                {
                    manager.AddTurnPhase(phase.ToString());
                }
            }

            // Update resources
            if (payload.TryGetValue("initialResources", out var resourcesObj) && resourcesObj is Dictionary<string, object> resourcesDict)
            {
                foreach (var kvp in resourcesDict)
                {
                    manager.SetResource(kvp.Key, Convert.ToSingle(kvp.Value));
                }
            }

            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            return CreateSuccessResponse(("managerId", managerId), ("path", BuildGameObjectPath(manager.gameObject)));
        }

        #endregion

        #region Inspect Manager

        private object InspectManager(Dictionary<string, object> payload)
        {
            var managerId = GetString(payload, "managerId");
            if (string.IsNullOrEmpty(managerId))
            {
                throw new InvalidOperationException("managerId is required for inspect.");
            }

            var manager = FindManagerById(managerId);
            if (manager == null)
            {
                return CreateSuccessResponse(("found", false), ("managerId", managerId));
            }

            var info = new Dictionary<string, object>
            {
                { "found", true },
                { "managerId", manager.ManagerId },
                { "path", BuildGameObjectPath(manager.gameObject) },
                { "managerType", manager.Type.ToString() },
                { "persistent", manager.IsPersistent },
                { "resources", manager.GetAllResources() }
            };

            return CreateSuccessResponse(("manager", info));
        }

        #endregion

        #region Delete Manager

        private object DeleteManager(Dictionary<string, object> payload)
        {
            var managerId = GetString(payload, "managerId");
            if (string.IsNullOrEmpty(managerId))
            {
                throw new InvalidOperationException("managerId is required for delete.");
            }

            var manager = FindManagerById(managerId);
            if (manager == null)
            {
                throw new InvalidOperationException($"Manager with ID '{managerId}' not found.");
            }

            var scene = manager.gameObject.scene;
            Undo.DestroyObjectImmediate(manager.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(("managerId", managerId), ("deleted", true));
        }

        #endregion

        #region Helpers

        private GameKitManager FindManagerById(string managerId)
        {
            var managers = UnityEngine.Object.FindObjectsByType<GameKitManager>(FindObjectsSortMode.None);
            foreach (var manager in managers)
            {
                if (manager.ManagerId == managerId)
                {
                    return manager;
                }
            }
            return null;
        }

        private GameKitManager.ManagerType ParseManagerType(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "turnbased" => GameKitManager.ManagerType.TurnBased,
                "realtime" => GameKitManager.ManagerType.Realtime,
                "resourcepool" => GameKitManager.ManagerType.ResourcePool,
                "eventhub" => GameKitManager.ManagerType.EventHub,
                "statemanager" => GameKitManager.ManagerType.StateManager,
                _ => GameKitManager.ManagerType.Realtime
            };
        }

        #endregion
    }
}

