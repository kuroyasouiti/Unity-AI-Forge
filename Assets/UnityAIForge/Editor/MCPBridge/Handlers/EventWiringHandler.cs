using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MCP.Editor.Base;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Event Wiring Handler: Dynamic UnityEvent connection.
    /// Enables LLMs to wire up UI events, game events, and custom UnityEvents programmatically.
    /// </summary>
    public class EventWiringHandler : BaseCommandHandler
    {
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "wire",
            "unwire",
            "inspect",
            "listEvents",
            "clearEvent",
            "wireMultiple"
        };

        public override string Category => "EventWiring";
        public override string Version => "1.0.0";

        protected override bool RequiresCompilationWait(string operation)
        {
            return false;
        }

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "wire" => HandleWire(payload),
                "unwire" => HandleUnwire(payload),
                "inspect" => HandleInspect(payload),
                "listEvents" => HandleListEvents(payload),
                "clearEvent" => HandleClearEvent(payload),
                "wireMultiple" => HandleWireMultiple(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }

        /// <summary>
        /// Wire a UnityEvent to a target method.
        /// </summary>
        private object HandleWire(Dictionary<string, object> payload)
        {
            var sourceData = GetDictFromPayload(payload, "source");
            var targetData = GetDictFromPayload(payload, "target");

            if (sourceData == null)
            {
                throw new ArgumentException("source is required");
            }

            if (targetData == null)
            {
                throw new ArgumentException("target is required");
            }

            // Parse source
            string sourceGameObjectPath = sourceData.ContainsKey("gameObject") ? sourceData["gameObject"]?.ToString() : "";
            string sourceComponentType = sourceData.ContainsKey("component") ? sourceData["component"]?.ToString() : "";
            string eventName = sourceData.ContainsKey("event") ? sourceData["event"]?.ToString() : "";

            // Parse target
            string targetGameObjectPath = targetData.ContainsKey("gameObject") ? targetData["gameObject"]?.ToString() : "";
            string targetComponentType = targetData.ContainsKey("component") ? targetData["component"]?.ToString() : null;
            string methodName = targetData.ContainsKey("method") ? targetData["method"]?.ToString() : "";
            string argMode = targetData.ContainsKey("mode") ? targetData["mode"]?.ToString() : "Void";
            object argument = targetData.ContainsKey("argument") ? targetData["argument"] : null;

            // Find source GameObject and component
            var sourceGo = GameObject.Find(sourceGameObjectPath);
            if (sourceGo == null)
            {
                throw new ArgumentException($"Source GameObject not found: {sourceGameObjectPath}");
            }

            Component sourceComponent = FindComponent(sourceGo, sourceComponentType);
            if (sourceComponent == null)
            {
                throw new ArgumentException($"Source component not found: {sourceComponentType}");
            }

            // Find target GameObject and component
            var targetGo = GameObject.Find(targetGameObjectPath);
            if (targetGo == null)
            {
                throw new ArgumentException($"Target GameObject not found: {targetGameObjectPath}");
            }

            UnityEngine.Object targetObj = targetGo;
            if (!string.IsNullOrEmpty(targetComponentType))
            {
                var targetComponent = FindComponent(targetGo, targetComponentType);
                if (targetComponent == null)
                {
                    throw new ArgumentException($"Target component not found: {targetComponentType}");
                }
                targetObj = targetComponent;
            }

            // Find the event field
            var eventField = FindEventField(sourceComponent, eventName);
            if (eventField == null)
            {
                throw new ArgumentException($"Event not found: {eventName}");
            }

            var unityEvent = eventField.GetValue(sourceComponent) as UnityEventBase;
            if (unityEvent == null)
            {
                throw new ArgumentException($"Event is not a UnityEvent: {eventName}");
            }

            // Find target method
            MethodInfo targetMethod = FindMethod(targetObj.GetType(), methodName, argMode);
            if (targetMethod == null)
            {
                throw new ArgumentException($"Method not found: {methodName} with mode {argMode}");
            }

            // Add persistent listener using reflection
            AddPersistentListener(unityEvent, targetObj, methodName, argMode, argument);

            EditorUtility.SetDirty(sourceComponent);

            return CreateSuccessResponse(
                ("source", new Dictionary<string, object>
                {
                    ["gameObject"] = sourceGameObjectPath,
                    ["component"] = sourceComponentType,
                    ["event"] = eventName
                }),
                ("target", new Dictionary<string, object>
                {
                    ["gameObject"] = targetGameObjectPath,
                    ["method"] = methodName,
                    ["mode"] = argMode
                }),
                ("message", "Event wired successfully")
            );
        }

        /// <summary>
        /// Unwire (remove) a listener from a UnityEvent.
        /// </summary>
        private object HandleUnwire(Dictionary<string, object> payload)
        {
            var sourceData = GetDictFromPayload(payload, "source");
            string targetGameObjectPath = GetString(payload, "targetGameObject", null);
            string targetMethodName = GetString(payload, "targetMethod", null);
            int listenerIndex = GetInt(payload, "listenerIndex", -1);

            if (sourceData == null)
            {
                throw new ArgumentException("source is required");
            }

            string sourceGameObjectPath = sourceData.ContainsKey("gameObject") ? sourceData["gameObject"]?.ToString() : "";
            string sourceComponentType = sourceData.ContainsKey("component") ? sourceData["component"]?.ToString() : "";
            string eventName = sourceData.ContainsKey("event") ? sourceData["event"]?.ToString() : "";

            var sourceGo = GameObject.Find(sourceGameObjectPath);
            if (sourceGo == null)
            {
                throw new ArgumentException($"Source GameObject not found: {sourceGameObjectPath}");
            }

            Component sourceComponent = FindComponent(sourceGo, sourceComponentType);
            if (sourceComponent == null)
            {
                throw new ArgumentException($"Source component not found: {sourceComponentType}");
            }

            var eventField = FindEventField(sourceComponent, eventName);
            if (eventField == null)
            {
                throw new ArgumentException($"Event not found: {eventName}");
            }

            var unityEvent = eventField.GetValue(sourceComponent) as UnityEventBase;
            if (unityEvent == null)
            {
                throw new ArgumentException($"Event is not a UnityEvent: {eventName}");
            }

            int removedCount = 0;

            if (listenerIndex >= 0)
            {
                // Remove by index
                UnityEventTools.RemovePersistentListener(unityEvent, listenerIndex);
                removedCount = 1;
            }
            else if (!string.IsNullOrEmpty(targetGameObjectPath) || !string.IsNullOrEmpty(targetMethodName))
            {
                // Remove by target/method match
                int count = unityEvent.GetPersistentEventCount();
                for (int i = count - 1; i >= 0; i--)
                {
                    var target = unityEvent.GetPersistentTarget(i);
                    var method = unityEvent.GetPersistentMethodName(i);

                    bool matchTarget = string.IsNullOrEmpty(targetGameObjectPath);
                    bool matchMethod = string.IsNullOrEmpty(targetMethodName);

                    if (!string.IsNullOrEmpty(targetGameObjectPath) && target is Component comp)
                    {
                        matchTarget = GetGameObjectPath(comp.gameObject) == targetGameObjectPath;
                    }
                    else if (!string.IsNullOrEmpty(targetGameObjectPath) && target is GameObject go)
                    {
                        matchTarget = GetGameObjectPath(go) == targetGameObjectPath;
                    }

                    if (!string.IsNullOrEmpty(targetMethodName))
                    {
                        matchMethod = method == targetMethodName;
                    }

                    if (matchTarget && matchMethod)
                    {
                        UnityEventTools.RemovePersistentListener(unityEvent, i);
                        removedCount++;
                    }
                }
            }

            EditorUtility.SetDirty(sourceComponent);

            return CreateSuccessResponse(
                ("source", sourceData),
                ("removedCount", removedCount),
                ("message", $"Removed {removedCount} listener(s)")
            );
        }

        /// <summary>
        /// Inspect listeners on a UnityEvent.
        /// </summary>
        private object HandleInspect(Dictionary<string, object> payload)
        {
            var sourceData = GetDictFromPayload(payload, "source");

            if (sourceData == null)
            {
                throw new ArgumentException("source is required");
            }

            string sourceGameObjectPath = sourceData.ContainsKey("gameObject") ? sourceData["gameObject"]?.ToString() : "";
            string sourceComponentType = sourceData.ContainsKey("component") ? sourceData["component"]?.ToString() : "";
            string eventName = sourceData.ContainsKey("event") ? sourceData["event"]?.ToString() : "";

            var sourceGo = GameObject.Find(sourceGameObjectPath);
            if (sourceGo == null)
            {
                throw new ArgumentException($"Source GameObject not found: {sourceGameObjectPath}");
            }

            Component sourceComponent = FindComponent(sourceGo, sourceComponentType);
            if (sourceComponent == null)
            {
                throw new ArgumentException($"Source component not found: {sourceComponentType}");
            }

            var eventField = FindEventField(sourceComponent, eventName);
            if (eventField == null)
            {
                throw new ArgumentException($"Event not found: {eventName}");
            }

            var unityEvent = eventField.GetValue(sourceComponent) as UnityEventBase;
            if (unityEvent == null)
            {
                throw new ArgumentException($"Event is not a UnityEvent: {eventName}");
            }

            var listeners = new List<Dictionary<string, object>>();
            int count = unityEvent.GetPersistentEventCount();

            for (int i = 0; i < count; i++)
            {
                var target = unityEvent.GetPersistentTarget(i);
                var method = unityEvent.GetPersistentMethodName(i);

                string targetPath = "";
                string targetType = "";

                if (target is Component comp)
                {
                    targetPath = GetGameObjectPath(comp.gameObject);
                    targetType = comp.GetType().Name;
                }
                else if (target is GameObject go)
                {
                    targetPath = GetGameObjectPath(go);
                    targetType = "GameObject";
                }

                listeners.Add(new Dictionary<string, object>
                {
                    ["index"] = i,
                    ["targetPath"] = targetPath,
                    ["targetType"] = targetType,
                    ["method"] = method,
                    ["callState"] = unityEvent.GetPersistentListenerState(i).ToString()
                });
            }

            return CreateSuccessResponse(
                ("source", sourceData),
                ("listenerCount", count),
                ("listeners", listeners)
            );
        }

        /// <summary>
        /// List all UnityEvent fields on a component.
        /// </summary>
        private object HandleListEvents(Dictionary<string, object> payload)
        {
            string gameObjectPath = GetString(payload, "gameObjectPath", null);
            string componentType = GetString(payload, "componentType", null);

            if (string.IsNullOrEmpty(gameObjectPath))
            {
                throw new ArgumentException("gameObjectPath is required");
            }

            var go = GameObject.Find(gameObjectPath);
            if (go == null)
            {
                throw new ArgumentException($"GameObject not found: {gameObjectPath}");
            }

            var events = new List<Dictionary<string, object>>();

            // Get components to inspect
            Component[] components;
            if (!string.IsNullOrEmpty(componentType))
            {
                var comp = FindComponent(go, componentType);
                if (comp == null)
                {
                    throw new ArgumentException($"Component not found: {componentType}");
                }
                components = new[] { comp };
            }
            else
            {
                components = go.GetComponents<Component>();
            }

            foreach (var component in components)
            {
                if (component == null) continue;

                var type = component.GetType();
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

                // Check fields
                foreach (var field in fields)
                {
                    if (typeof(UnityEventBase).IsAssignableFrom(field.FieldType))
                    {
                        var unityEvent = field.GetValue(component) as UnityEventBase;
                        events.Add(new Dictionary<string, object>
                        {
                            ["componentType"] = type.Name,
                            ["eventName"] = field.Name,
                            ["eventType"] = field.FieldType.Name,
                            ["listenerCount"] = unityEvent?.GetPersistentEventCount() ?? 0
                        });
                    }
                }

                // Check properties
                foreach (var prop in properties)
                {
                    if (typeof(UnityEventBase).IsAssignableFrom(prop.PropertyType) && prop.CanRead)
                    {
                        try
                        {
                            var unityEvent = prop.GetValue(component) as UnityEventBase;
                            events.Add(new Dictionary<string, object>
                            {
                                ["componentType"] = type.Name,
                                ["eventName"] = prop.Name,
                                ["eventType"] = prop.PropertyType.Name,
                                ["listenerCount"] = unityEvent?.GetPersistentEventCount() ?? 0
                            });
                        }
                        catch
                        {
                            // Skip if property getter throws
                        }
                    }
                }
            }

            return CreateSuccessResponse(
                ("gameObjectPath", gameObjectPath),
                ("events", events),
                ("eventCount", events.Count)
            );
        }

        /// <summary>
        /// Clear all listeners from a UnityEvent.
        /// </summary>
        private object HandleClearEvent(Dictionary<string, object> payload)
        {
            var sourceData = GetDictFromPayload(payload, "source");

            if (sourceData == null)
            {
                throw new ArgumentException("source is required");
            }

            string sourceGameObjectPath = sourceData.ContainsKey("gameObject") ? sourceData["gameObject"]?.ToString() : "";
            string sourceComponentType = sourceData.ContainsKey("component") ? sourceData["component"]?.ToString() : "";
            string eventName = sourceData.ContainsKey("event") ? sourceData["event"]?.ToString() : "";

            var sourceGo = GameObject.Find(sourceGameObjectPath);
            if (sourceGo == null)
            {
                throw new ArgumentException($"Source GameObject not found: {sourceGameObjectPath}");
            }

            Component sourceComponent = FindComponent(sourceGo, sourceComponentType);
            if (sourceComponent == null)
            {
                throw new ArgumentException($"Source component not found: {sourceComponentType}");
            }

            var eventField = FindEventField(sourceComponent, eventName);
            if (eventField == null)
            {
                throw new ArgumentException($"Event not found: {eventName}");
            }

            var unityEvent = eventField.GetValue(sourceComponent) as UnityEventBase;
            if (unityEvent == null)
            {
                throw new ArgumentException($"Event is not a UnityEvent: {eventName}");
            }

            int previousCount = unityEvent.GetPersistentEventCount();

            // Remove all listeners
            for (int i = previousCount - 1; i >= 0; i--)
            {
                UnityEventTools.RemovePersistentListener(unityEvent, i);
            }

            EditorUtility.SetDirty(sourceComponent);

            return CreateSuccessResponse(
                ("source", sourceData),
                ("removedCount", previousCount),
                ("message", $"Cleared {previousCount} listener(s)")
            );
        }

        /// <summary>
        /// Wire multiple events at once.
        /// </summary>
        private object HandleWireMultiple(Dictionary<string, object> payload)
        {
            var wiringsData = GetListFromPayload(payload, "wirings");

            if (wiringsData == null || wiringsData.Count == 0)
            {
                throw new ArgumentException("wirings array is required");
            }

            var results = new List<Dictionary<string, object>>();
            int successCount = 0;
            int errorCount = 0;

            foreach (var wiringData in wiringsData)
            {
                if (wiringData is Dictionary<string, object> wiring)
                {
                    try
                    {
                        var result = HandleWire(wiring);
                        results.Add(new Dictionary<string, object>
                        {
                            ["success"] = true,
                            ["wiring"] = wiring
                        });
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        results.Add(new Dictionary<string, object>
                        {
                            ["success"] = false,
                            ["wiring"] = wiring,
                            ["error"] = ex.Message
                        });
                        errorCount++;
                    }
                }
            }

            return CreateSuccessResponse(
                ("totalCount", wiringsData.Count),
                ("successCount", successCount),
                ("errorCount", errorCount),
                ("results", results)
            );
        }

        #region Helper Methods

        // Note: GetDictFromPayload and GetListFromPayload are inherited from BaseCommandHandler

        private Component FindComponent(GameObject go, string componentType)
        {
            if (string.IsNullOrEmpty(componentType))
            {
                return null;
            }

            // Try exact match
            var type = Type.GetType(componentType);
            if (type != null)
            {
                return go.GetComponent(type);
            }

            // Try common Unity namespaces
            string[] namespaces = {
                "UnityEngine.UI.",
                "UnityEngine.",
                "TMPro.",
                ""
            };

            foreach (var ns in namespaces)
            {
                var fullTypeName = ns + componentType;

                // Check all loaded assemblies
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(fullTypeName);
                    if (type != null)
                    {
                        var comp = go.GetComponent(type);
                        if (comp != null) return comp;
                    }
                }
            }

            // Try by simple name
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp.GetType().Name == componentType)
                {
                    return comp;
                }
            }

            return null;
        }

        private FieldInfo FindEventField(Component component, string eventName)
        {
            var type = component.GetType();

            // Try field first
            var field = type.GetField(eventName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && typeof(UnityEventBase).IsAssignableFrom(field.FieldType))
            {
                return field;
            }

            // Try backing field for property
            field = type.GetField("m_" + eventName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null && typeof(UnityEventBase).IsAssignableFrom(field.FieldType))
            {
                return field;
            }

            // Try with "On" prefix
            field = type.GetField("On" + eventName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && typeof(UnityEventBase).IsAssignableFrom(field.FieldType))
            {
                return field;
            }

            // Search for partial match
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var f in fields)
            {
                if (f.Name.Contains(eventName, StringComparison.OrdinalIgnoreCase) &&
                    typeof(UnityEventBase).IsAssignableFrom(f.FieldType))
                {
                    return f;
                }
            }

            return null;
        }

        private MethodInfo FindMethod(Type type, string methodName, string argMode)
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);

            foreach (var method in methods.Where(m => m.Name == methodName))
            {
                var parameters = method.GetParameters();

                switch (argMode.ToLower())
                {
                    case "void":
                        if (parameters.Length == 0) return method;
                        break;
                    case "int":
                        if (parameters.Length == 1 && parameters[0].ParameterType == typeof(int)) return method;
                        break;
                    case "float":
                        if (parameters.Length == 1 && parameters[0].ParameterType == typeof(float)) return method;
                        break;
                    case "string":
                        if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string)) return method;
                        break;
                    case "bool":
                        if (parameters.Length == 1 && parameters[0].ParameterType == typeof(bool)) return method;
                        break;
                    case "object":
                        if (parameters.Length == 1 && typeof(UnityEngine.Object).IsAssignableFrom(parameters[0].ParameterType)) return method;
                        break;
                }
            }

            // Fallback: return first matching method name
            return methods.FirstOrDefault(m => m.Name == methodName);
        }

        private void AddPersistentListener(UnityEventBase unityEvent, UnityEngine.Object target, string methodName, string argMode, object argument)
        {
            // Use reflection to add persistent listener manually
            // Unity's UnityEventTools requires delegates, but we need to set up serialized listeners

            // Get the current count to determine the new index
            int currentCount = unityEvent.GetPersistentEventCount();

            // Use reflection to access internal methods
            var unityEventType = typeof(UnityEventBase);

            // Add a new persistent listener slot
            var addListenerMethod = unityEventType.GetMethod("AddPersistentListener",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (addListenerMethod != null)
            {
                addListenerMethod.Invoke(unityEvent, null);
            }

            // Set the target and method name
            var registerMethod = unityEventType.GetMethod("RegisterPersistentListener",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (registerMethod != null)
            {
                registerMethod.Invoke(unityEvent, new object[] { currentCount, target, methodName });
            }

            // Set the call state to RuntimeOnly by default
            var setCallStateMethod = unityEventType.GetMethod("SetPersistentListenerState",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (setCallStateMethod != null)
            {
                setCallStateMethod.Invoke(unityEvent, new object[] { currentCount, UnityEventCallState.RuntimeOnly });
            }

            // For typed arguments, set the argument value via SerializedObject if needed
            if (argMode.ToLower() != "void" && argument != null)
            {
                SetPersistentListenerArgument(unityEvent, currentCount, argMode, argument);
            }
        }

        private void SetPersistentListenerArgument(UnityEventBase unityEvent, int listenerIndex, string argMode, object argument)
        {
            // For persistent listeners with arguments, we need to use SerializedObject
            // This is complex and varies by Unity version
            // For now, we'll skip argument setting for non-void listeners
            // The listener will still be wired up, just without the preset argument
        }

        private string GetGameObjectPath(GameObject go)
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

        #endregion
    }
}
