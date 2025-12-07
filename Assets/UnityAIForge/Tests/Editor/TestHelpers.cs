using UnityEngine;
using UnityEditor;
using UnityAIForge.GameKit;
using System.Collections.Generic;
using MCP.Editor;
using MCP.Editor.Base;
using NUnit.Framework;

namespace UnityAIForge.Tests.Editor
{
    /// <summary>
    /// Helper methods for creating test objects and assertions.
    /// Provides convenient methods to reduce boilerplate code in tests.
    /// </summary>
    public static class TestHelpers
    {
        #region GameKit Object Creation
        
        /// <summary>
        /// Creates a test GameKitActor with default or custom parameters
        /// </summary>
        /// <param name="actorId">Unique actor identifier</param>
        /// <param name="behavior">Behavior profile</param>
        /// <param name="control">Control mode</param>
        /// <returns>Tuple of (GameObject, GameKitActor)</returns>
        public static (GameObject go, GameKitActor actor) CreateTestActor(
            string actorId = "test_actor",
            GameKitActor.BehaviorProfile behavior = GameKitActor.BehaviorProfile.TwoDLinear,
            GameKitActor.ControlMode control = GameKitActor.ControlMode.DirectController)
        {
            var go = new GameObject($"TestActor_{actorId}");
            var actor = go.AddComponent<GameKitActor>();
            actor.Initialize(actorId, behavior, control);
            return (go, actor);
        }
        
        /// <summary>
        /// Creates a test GameKitManager with default or custom parameters
        /// </summary>
        /// <param name="managerId">Unique manager identifier</param>
        /// <param name="type">Manager type</param>
        /// <param name="persistent">DontDestroyOnLoad flag</param>
        /// <returns>Tuple of (GameObject, GameKitManager)</returns>
        public static (GameObject go, GameKitManager manager) CreateTestManager(
            string managerId = "test_manager",
            GameKitManager.ManagerType type = GameKitManager.ManagerType.ResourcePool,
            bool persistent = false)
        {
            var go = new GameObject($"TestManager_{managerId}");
            var manager = go.AddComponent<GameKitManager>();
            manager.Initialize(managerId, type, persistent);
            return (go, manager);
        }
        
        /// <summary>
        /// Creates a test GameKitInteraction with default or custom parameters
        /// </summary>
        /// <param name="interactionId">Unique interaction identifier</param>
        /// <param name="triggerType">Trigger detection type</param>
        /// <returns>Tuple of (GameObject, GameKitInteraction)</returns>
        public static (GameObject go, GameKitInteraction interaction) CreateTestInteraction(
            string interactionId = "test_interaction",
            GameKitInteraction.TriggerType triggerType = GameKitInteraction.TriggerType.Trigger)
        {
            var go = new GameObject($"TestInteraction_{interactionId}");
            var interaction = go.AddComponent<GameKitInteraction>();
            interaction.Initialize(interactionId, triggerType);
            return (go, interaction);
        }
        
        #endregion
        
        #region Component Helpers
        
        /// <summary>
        /// Adds a component using Undo system (Editor-friendly)
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <param name="go">Target GameObject</param>
        /// <returns>Added component</returns>
        public static T AddTestComponent<T>(GameObject go) where T : Component
        {
            return Undo.AddComponent<T>(go);
        }
        
        /// <summary>
        /// Adds a component by type name (useful for types in other assemblies)
        /// </summary>
        /// <param name="go">Target GameObject</param>
        /// <param name="typeName">Full type name (e.g., "TMPro.TextMeshPro")</param>
        /// <returns>Added component or null if type not found</returns>
        public static Component AddTestComponent(GameObject go, string typeName)
        {
            var type = System.Type.GetType(typeName);
            if (type == null)
            {
                // Try to find in all assemblies
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(typeName);
                    if (type != null) break;
                }
            }
            
            if (type != null)
            {
                return Undo.AddComponent(go, type);
            }
            
            Debug.LogWarning($"Type '{typeName}' not found");
            return null;
        }
        
        #endregion
        
        #region MCP Command Helpers
        
        /// <summary>
        /// Creates a payload for componentManage operations
        /// </summary>
        /// <param name="operation">Operation type (add, remove, update, inspect)</param>
        /// <param name="gameObjectPath">GameObject hierarchy path</param>
        /// <param name="componentType">Component type name</param>
        /// <param name="propertyChanges">Optional property changes for update operation</param>
        /// <returns>Command payload dictionary</returns>
        public static Dictionary<string, object> CreateComponentPayload(
            string operation,
            string gameObjectPath,
            string componentType,
            Dictionary<string, object> propertyChanges = null)
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = operation,
                ["gameObjectPath"] = gameObjectPath,
                ["componentType"] = componentType
            };
            
            if (propertyChanges != null)
            {
                payload["propertyChanges"] = propertyChanges;
            }
            
            return payload;
        }
        
        /// <summary>
        /// Creates an MCP command for component operations
        /// </summary>
        /// <param name="operation">Operation type</param>
        /// <param name="gameObjectPath">GameObject hierarchy path</param>
        /// <param name="componentType">Component type name</param>
        /// <param name="propertyChanges">Optional property changes</param>
        /// <returns>McpIncomingCommand</returns>
        public static McpIncomingCommand CreateComponentCommand(
            string operation,
            string gameObjectPath,
            string componentType,
            Dictionary<string, object> propertyChanges = null)
        {
            var payload = CreateComponentPayload(operation, gameObjectPath, componentType, propertyChanges);
            return new McpIncomingCommand { ToolName = "componentManage", Payload = payload };
        }
        
        /// <summary>
        /// Executes a component command and returns the result
        /// </summary>
        /// <param name="operation">Operation type</param>
        /// <param name="gameObjectPath">GameObject hierarchy path</param>
        /// <param name="componentType">Component type name</param>
        /// <param name="propertyChanges">Optional property changes</param>
        /// <returns>Command result as dictionary</returns>
        public static Dictionary<string, object> ExecuteComponentCommand(
            string operation,
            string gameObjectPath,
            string componentType,
            Dictionary<string, object> propertyChanges = null)
        {
            var command = CreateComponentCommand(operation, gameObjectPath, componentType, propertyChanges);
            return McpCommandProcessor.Execute(command) as Dictionary<string, object>;
        }
        
        #endregion
        
        #region Assertion Helpers
        
        /// <summary>
        /// Asserts that a GameObject has a specific component
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <param name="go">GameObject to check</param>
        /// <param name="message">Optional custom message</param>
        public static void AssertComponentExists<T>(GameObject go, string message = null) where T : Component
        {
            var component = go.GetComponent<T>();
            Assert.IsNotNull(component, message ?? $"{typeof(T).Name} component should exist on {go.name}");
        }
        
        /// <summary>
        /// Asserts that a GameObject has a component by type name
        /// </summary>
        /// <param name="go">GameObject to check</param>
        /// <param name="typeName">Type name (e.g., "TMPro.TextMeshPro")</param>
        /// <param name="message">Optional custom message</param>
        public static void AssertComponentExists(GameObject go, string typeName, string message = null)
        {
            var component = go.GetComponent(typeName);
            Assert.IsNotNull(component, message ?? $"{typeName} component should exist on {go.name}");
        }
        
        /// <summary>
        /// Asserts that a GameObject does not have a specific component
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <param name="go">GameObject to check</param>
        /// <param name="message">Optional custom message</param>
        public static void AssertComponentNotExists<T>(GameObject go, string message = null) where T : Component
        {
            var component = go.GetComponent<T>();
            Assert.IsNull(component, message ?? $"{typeof(T).Name} component should not exist on {go.name}");
        }
        
        /// <summary>
        /// Asserts that a component has a specific property value
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <param name="go">GameObject with component</param>
        /// <param name="propertyName">Property name</param>
        /// <param name="expectedValue">Expected value</param>
        /// <param name="message">Optional custom message</param>
        public static void AssertComponentProperty<T>(GameObject go, string propertyName, object expectedValue, string message = null) where T : Component
        {
            var component = go.GetComponent<T>();
            Assert.IsNotNull(component, $"{typeof(T).Name} component not found on {go.name}");
            
            var field = typeof(T).GetField(propertyName, 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field == null)
            {
                var property = typeof(T).GetProperty(propertyName, 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(property, $"Property/Field '{propertyName}' not found on {typeof(T).Name}");
                
                var actualValue = property.GetValue(component);
                Assert.AreEqual(expectedValue, actualValue, 
                    message ?? $"{typeof(T).Name}.{propertyName} should be {expectedValue} but was {actualValue}");
            }
            else
            {
                var actualValue = field.GetValue(component);
                Assert.AreEqual(expectedValue, actualValue, 
                    message ?? $"{typeof(T).Name}.{propertyName} should be {expectedValue} but was {actualValue}");
            }
        }
        
        /// <summary>
        /// Asserts that an MCP command result indicates success
        /// </summary>
        /// <param name="result">Command result</param>
        /// <param name="message">Optional custom message</param>
        public static void AssertCommandSuccess(Dictionary<string, object> result, string message = null)
        {
            Assert.IsNotNull(result, "Command result should not be null");
            Assert.IsTrue(result.ContainsKey("success"), "Command result should contain 'success' key");
            Assert.IsTrue((bool)result["success"], message ?? "Command should succeed");
        }
        
        /// <summary>
        /// Asserts that an MCP command result indicates failure
        /// </summary>
        /// <param name="result">Command result</param>
        /// <param name="message">Optional custom message</param>
        public static void AssertCommandFailure(Dictionary<string, object> result, string message = null)
        {
            Assert.IsNotNull(result, "Command result should not be null");
            Assert.IsTrue(result.ContainsKey("success"), "Command result should contain 'success' key");
            Assert.IsFalse((bool)result["success"], message ?? "Command should fail");
        }
        
        /// <summary>
        /// Asserts that an MCP command result contains specific updated properties
        /// </summary>
        /// <param name="result">Command result</param>
        /// <param name="propertyNames">Expected property names</param>
        public static void AssertUpdatedProperties(Dictionary<string, object> result, params string[] propertyNames)
        {
            Assert.IsTrue(result.ContainsKey("updatedProperties"), "Result should contain 'updatedProperties'");
            var updatedProps = result["updatedProperties"] as List<string>;
            Assert.IsNotNull(updatedProps, "'updatedProperties' should be a list");
            
            foreach (var propName in propertyNames)
            {
                Assert.Contains(propName, updatedProps, $"Property '{propName}' should be in updatedProperties");
            }
        }
        
        /// <summary>
        /// Asserts that an MCP command result contains specific failed properties
        /// </summary>
        /// <param name="result">Command result</param>
        /// <param name="propertyNames">Expected failed property names</param>
        public static void AssertFailedProperties(Dictionary<string, object> result, params string[] propertyNames)
        {
            Assert.IsTrue(result.ContainsKey("failedProperties"), "Result should contain 'failedProperties'");
            var failedProps = result["failedProperties"] as Dictionary<string, string>;
            Assert.IsNotNull(failedProps, "'failedProperties' should be a dictionary");
            
            foreach (var propName in propertyNames)
            {
                Assert.IsTrue(failedProps.ContainsKey(propName), $"Property '{propName}' should be in failedProperties");
            }
        }
        
        #endregion
        
        #region Value Comparison Helpers
        
        /// <summary>
        /// Compares two float values with tolerance
        /// </summary>
        /// <param name="expected">Expected value</param>
        /// <param name="actual">Actual value</param>
        /// <param name="tolerance">Tolerance (default: 0.01)</param>
        /// <param name="message">Optional custom message</param>
        public static void AssertFloatEquals(float expected, float actual, float tolerance = 0.01f, string message = null)
        {
            Assert.AreEqual(expected, actual, tolerance, message ?? $"Expected {expected} but got {actual}");
        }
        
        /// <summary>
        /// Compares two Vector3 values with tolerance
        /// </summary>
        /// <param name="expected">Expected vector</param>
        /// <param name="actual">Actual vector</param>
        /// <param name="tolerance">Tolerance (default: 0.01)</param>
        /// <param name="message">Optional custom message</param>
        public static void AssertVector3Equals(Vector3 expected, Vector3 actual, float tolerance = 0.01f, string message = null)
        {
            Assert.AreEqual(expected.x, actual.x, tolerance, message ?? $"X: Expected {expected.x} but got {actual.x}");
            Assert.AreEqual(expected.y, actual.y, tolerance, message ?? $"Y: Expected {expected.y} but got {actual.y}");
            Assert.AreEqual(expected.z, actual.z, tolerance, message ?? $"Z: Expected {expected.z} but got {actual.z}");
        }
        
        /// <summary>
        /// Compares two Color values with tolerance
        /// </summary>
        /// <param name="expected">Expected color</param>
        /// <param name="actual">Actual color</param>
        /// <param name="tolerance">Tolerance (default: 0.01)</param>
        /// <param name="message">Optional custom message</param>
        public static void AssertColorEquals(Color expected, Color actual, float tolerance = 0.01f, string message = null)
        {
            Assert.AreEqual(expected.r, actual.r, tolerance, message ?? $"R: Expected {expected.r} but got {actual.r}");
            Assert.AreEqual(expected.g, actual.g, tolerance, message ?? $"G: Expected {expected.g} but got {actual.g}");
            Assert.AreEqual(expected.b, actual.b, tolerance, message ?? $"B: Expected {expected.b} but got {actual.b}");
            Assert.AreEqual(expected.a, actual.a, tolerance, message ?? $"A: Expected {expected.a} but got {actual.a}");
        }
        
        #endregion
        
        #region Scene Helpers
        
        /// <summary>
        /// Finds a GameObject by name, asserting it exists
        /// </summary>
        /// <param name="name">GameObject name</param>
        /// <returns>Found GameObject</returns>
        public static GameObject FindGameObjectOrFail(string name)
        {
            var go = GameObject.Find(name);
            Assert.IsNotNull(go, $"GameObject '{name}' should exist in scene");
            return go;
        }
        
        /// <summary>
        /// Finds all GameObjects with a specific component type
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <returns>Array of GameObjects</returns>
        public static GameObject[] FindGameObjectsWithComponent<T>() where T : Component
        {
            var components = Object.FindObjectsOfType<T>();
            var gameObjects = new GameObject[components.Length];
            for (int i = 0; i < components.Length; i++)
            {
                gameObjects[i] = components[i].gameObject;
            }
            return gameObjects;
        }
        
        #endregion
    }
}
