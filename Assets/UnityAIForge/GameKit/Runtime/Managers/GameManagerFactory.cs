using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// Factory for creating game manager components.
    /// Replaces reflection-based manager creation with type-safe factory methods.
    /// Part of the Abstract Factory Pattern for manager creation.
    /// </summary>
    public static class GameManagerFactory
    {
        private static readonly Dictionary<GameKitManager.ManagerType, Type> _registeredTypes =
            new Dictionary<GameKitManager.ManagerType, Type>
            {
                { GameKitManager.ManagerType.TurnBased, typeof(GameKitTurnManager) },
                { GameKitManager.ManagerType.ResourcePool, typeof(GameKitResourceManager) },
                { GameKitManager.ManagerType.EventHub, typeof(GameKitEventManager) },
                { GameKitManager.ManagerType.StateManager, typeof(GameKitStateManager) },
                { GameKitManager.ManagerType.Realtime, typeof(GameKitRealtimeManager) }
            };

        /// <summary>
        /// Creates a manager component on the specified GameObject.
        /// </summary>
        /// <param name="target">The GameObject to attach the manager to.</param>
        /// <param name="type">The type of manager to create.</param>
        /// <param name="managerId">The unique ID for the manager instance.</param>
        /// <returns>The created IGameManager instance, or null if creation failed.</returns>
        public static IGameManager CreateManager(GameObject target, GameKitManager.ManagerType type, string managerId)
        {
            if (target == null)
            {
                Debug.LogError("[GameManagerFactory] Cannot create manager on null GameObject");
                return null;
            }

            if (!_registeredTypes.TryGetValue(type, out Type managerType))
            {
                Debug.LogError($"[GameManagerFactory] Unknown manager type: {type}");
                return null;
            }

            // Add component to GameObject
            var component = target.AddComponent(managerType);
            if (component == null)
            {
                Debug.LogError($"[GameManagerFactory] Failed to create component of type: {managerType}");
                return null;
            }

            // Initialize if it implements IGameManager
            if (component is IGameManager gameManager)
            {
                gameManager.Initialize(managerId);
                Debug.Log($"[GameManagerFactory] Created and initialized {type} manager with ID: {managerId}");
                return gameManager;
            }

            Debug.LogWarning($"[GameManagerFactory] Component {managerType} does not implement IGameManager");
            return null;
        }

        /// <summary>
        /// Creates a manager component of a specific type.
        /// </summary>
        /// <typeparam name="T">The specific manager type.</typeparam>
        /// <param name="target">The GameObject to attach the manager to.</param>
        /// <param name="managerId">The unique ID for the manager instance.</param>
        /// <returns>The created manager instance, or null if creation failed.</returns>
        public static T CreateManager<T>(GameObject target, string managerId) where T : MonoBehaviour, IGameManager
        {
            if (target == null)
            {
                Debug.LogError("[GameManagerFactory] Cannot create manager on null GameObject");
                return null;
            }

            var component = target.AddComponent<T>();
            if (component == null)
            {
                Debug.LogError($"[GameManagerFactory] Failed to create component of type: {typeof(T)}");
                return null;
            }

            component.Initialize(managerId);
            Debug.Log($"[GameManagerFactory] Created and initialized {typeof(T).Name} manager with ID: {managerId}");
            return component;
        }

        /// <summary>
        /// Registers a custom manager type for factory creation.
        /// </summary>
        /// <typeparam name="T">The manager implementation type (must implement IGameManager).</typeparam>
        /// <param name="type">The ManagerType enum value to associate with this type.</param>
        public static void RegisterManagerType<T>(GameKitManager.ManagerType type) where T : MonoBehaviour, IGameManager
        {
            _registeredTypes[type] = typeof(T);
            Debug.Log($"[GameManagerFactory] Registered custom manager type: {typeof(T).Name} for {type}");
        }

        /// <summary>
        /// Unregisters a manager type.
        /// </summary>
        /// <param name="type">The ManagerType to unregister.</param>
        public static void UnregisterManagerType(GameKitManager.ManagerType type)
        {
            if (_registeredTypes.Remove(type))
            {
                Debug.Log($"[GameManagerFactory] Unregistered manager type: {type}");
            }
        }

        /// <summary>
        /// Gets the registered type for a manager type.
        /// </summary>
        /// <param name="type">The ManagerType to look up.</param>
        /// <returns>The registered Type, or null if not found.</returns>
        public static Type GetRegisteredType(GameKitManager.ManagerType type)
        {
            return _registeredTypes.TryGetValue(type, out Type managerType) ? managerType : null;
        }

        /// <summary>
        /// Checks if a manager type is registered.
        /// </summary>
        /// <param name="type">The ManagerType to check.</param>
        /// <returns>True if the type is registered, false otherwise.</returns>
        public static bool IsTypeRegistered(GameKitManager.ManagerType type)
        {
            return _registeredTypes.ContainsKey(type);
        }

        /// <summary>
        /// Gets all registered manager types.
        /// </summary>
        /// <returns>A collection of registered ManagerTypes.</returns>
        public static IEnumerable<GameKitManager.ManagerType> GetRegisteredTypes()
        {
            return _registeredTypes.Keys;
        }

        /// <summary>
        /// Resets the factory to default registrations.
        /// </summary>
        public static void ResetToDefaults()
        {
            _registeredTypes.Clear();
            _registeredTypes[GameKitManager.ManagerType.TurnBased] = typeof(GameKitTurnManager);
            _registeredTypes[GameKitManager.ManagerType.ResourcePool] = typeof(GameKitResourceManager);
            _registeredTypes[GameKitManager.ManagerType.EventHub] = typeof(GameKitEventManager);
            _registeredTypes[GameKitManager.ManagerType.StateManager] = typeof(GameKitStateManager);
            _registeredTypes[GameKitManager.ManagerType.Realtime] = typeof(GameKitRealtimeManager);
            Debug.Log("[GameManagerFactory] Reset to default manager type registrations");
        }
    }
}
