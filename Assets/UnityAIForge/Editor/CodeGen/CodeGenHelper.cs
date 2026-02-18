using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.CodeGen
{
    /// <summary>
    /// Utility methods for handlers that use code generation.
    /// Provides component resolution, property access, and lifecycle helpers
    /// for working with generated MonoBehaviour types.
    /// </summary>
    public static class CodeGenHelper
    {
        /// <summary>
        /// Finds a generated component on a GameObject by its tracker component ID.
        /// Uses the tracker to resolve the class name, then searches the GameObject.
        /// </summary>
        /// <param name="go">The GameObject to search.</param>
        /// <param name="componentId">The logical component ID (e.g. "player_hp").</param>
        /// <returns>The component, or null if not found.</returns>
        public static Component FindGeneratedComponent(GameObject go, string componentId)
        {
            if (go == null || string.IsNullOrEmpty(componentId))
                return null;

            var tracker = GeneratedScriptTracker.Instance;
            var entry = tracker.FindByComponentId(componentId);
            if (entry == null)
                return null;

            var type = ScriptGenerator.ResolveGeneratedType(entry.className);
            if (type == null)
                return null;

            return go.GetComponent(type);
        }

        /// <summary>
        /// Finds a generated component on a GameObject by searching for a MonoBehaviour
        /// with a matching serialized field value. Useful when the componentId isn't known
        /// but the ID field value is (e.g. finding by healthId).
        /// </summary>
        /// <param name="go">The GameObject to search.</param>
        /// <param name="fieldName">The serialized field name to match (e.g. "healthId").</param>
        /// <param name="fieldValue">The expected field value.</param>
        /// <returns>The matching component, or null.</returns>
        public static Component FindComponentByField(GameObject go, string fieldName, string fieldValue)
        {
            if (go == null || string.IsNullOrEmpty(fieldName))
                return null;

            foreach (var comp in go.GetComponents<MonoBehaviour>())
            {
                if (comp == null) continue;
                var so = new SerializedObject(comp);
                var prop = so.FindProperty(fieldName);
                if (prop != null && prop.propertyType == SerializedPropertyType.String
                    && (fieldValue == null || prop.stringValue == fieldValue))
                {
                    return comp;
                }
            }
            return null;
        }

        /// <summary>
        /// Searches all GameObjects in the scene for a MonoBehaviour with a matching
        /// serialized field value. Equivalent to FindObjectsByType + filter.
        /// </summary>
        /// <param name="fieldName">The serialized field name to match.</param>
        /// <param name="fieldValue">The expected field value.</param>
        /// <returns>The matching component, or null.</returns>
        public static Component FindComponentInSceneByField(string fieldName, string fieldValue)
        {
            if (string.IsNullOrEmpty(fieldName) || string.IsNullOrEmpty(fieldValue))
                return null;

            var allMonoBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var comp in allMonoBehaviours)
            {
                if (comp == null) continue;
                try
                {
                    var so = new SerializedObject(comp);
                    var prop = so.FindProperty(fieldName);
                    if (prop != null && prop.propertyType == SerializedPropertyType.String
                        && prop.stringValue == fieldValue)
                    {
                        return comp;
                    }
                }
                catch
                {
                    // Skip components that can't be serialized
                }
            }
            return null;
        }

        /// <summary>
        /// Adds a generated component to a GameObject after compilation.
        /// Resolves the class name from the tracker and uses Undo.AddComponent.
        /// </summary>
        /// <param name="go">The target GameObject.</param>
        /// <param name="className">The generated class name.</param>
        /// <returns>The added component, or null if the type isn't compiled yet.</returns>
        public static Component AddGeneratedComponent(GameObject go, string className)
        {
            if (go == null || string.IsNullOrEmpty(className))
                return null;

            var type = ScriptGenerator.ResolveGeneratedType(className);
            if (type == null)
                return null;

            return Undo.AddComponent(go, type);
        }

        /// <summary>
        /// Configures serialized properties on a component using a dictionary of values.
        /// Works with any MonoBehaviour regardless of its concrete type.
        /// </summary>
        /// <param name="component">The component to configure.</param>
        /// <param name="properties">Dictionary of property name → value.</param>
        public static void SetSerializedProperties(Component component, Dictionary<string, object> properties)
        {
            if (component == null || properties == null || properties.Count == 0)
                return;

            var so = new SerializedObject(component);
            foreach (var kvp in properties)
            {
                var prop = so.FindProperty(kvp.Key);
                if (prop == null) continue;
                SetSerializedPropertyValue(prop, kvp.Value);
            }
            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// Reads serialized properties from a component into a dictionary.
        /// </summary>
        /// <param name="component">The component to read from.</param>
        /// <param name="propertyNames">Property names to read. If null, reads all visible properties.</param>
        /// <returns>Dictionary of property name → value.</returns>
        public static Dictionary<string, object> GetSerializedProperties(
            Component component, IEnumerable<string> propertyNames = null)
        {
            var result = new Dictionary<string, object>();
            if (component == null) return result;

            var so = new SerializedObject(component);

            if (propertyNames != null)
            {
                foreach (var name in propertyNames)
                {
                    var prop = so.FindProperty(name);
                    if (prop != null)
                        result[name] = GetSerializedPropertyValue(prop);
                }
            }
            else
            {
                // Iterate all visible serialized properties
                var iterator = so.GetIterator();
                if (iterator.NextVisible(true))
                {
                    do
                    {
                        if (iterator.name == "m_Script") continue; // skip script reference
                        result[iterator.name] = GetSerializedPropertyValue(iterator);
                    } while (iterator.NextVisible(false));
                }
            }

            return result;
        }

        /// <summary>
        /// Generates a script and optionally adds the component if the type is already compiled.
        /// Returns a response dictionary suitable for handler return values.
        /// </summary>
        /// <param name="go">Target GameObject.</param>
        /// <param name="templateName">Template name (e.g. "Health").</param>
        /// <param name="componentId">Logical ID (e.g. "player_hp").</param>
        /// <param name="className">Generated class name (e.g. "PlayerHpHealth").</param>
        /// <param name="variables">Template variables.</param>
        /// <param name="outputDir">Output directory (optional).</param>
        /// <param name="propertiesToSet">Properties to set on the component after adding (optional).</param>
        /// <returns>Result dictionary with success, scriptPath, className, compilationRequired.</returns>
        public static Dictionary<string, object> GenerateAndAttach(
            GameObject go,
            string templateName,
            string componentId,
            string className,
            Dictionary<string, object> variables,
            string outputDir = null,
            Dictionary<string, object> propertiesToSet = null)
        {
            // Generate the script
            var result = ScriptGenerator.Generate(go, templateName, className, componentId, variables, outputDir);
            if (!result.Success)
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = result.ErrorMessage
                };
            }

            var response = new Dictionary<string, object>
            {
                ["success"] = true,
                ["scriptPath"] = result.ScriptPath,
                ["className"] = result.ClassName,
                ["componentId"] = componentId
            };

            // Try to add the component if the type is already compiled
            var type = ScriptGenerator.ResolveGeneratedType(className);
            if (type != null && go != null)
            {
                // Check if already has this component
                var existing = go.GetComponent(type);
                if (existing == null)
                {
                    var comp = Undo.AddComponent(go, type);
                    if (comp != null && propertiesToSet != null)
                    {
                        SetSerializedProperties(comp, propertiesToSet);
                    }
                    response["componentAdded"] = true;
                }
                else
                {
                    // Update existing component properties
                    if (propertiesToSet != null)
                    {
                        SetSerializedProperties(existing, propertiesToSet);
                    }
                    response["componentAdded"] = true;
                    response["componentUpdated"] = true;
                }
                response["compilationRequired"] = false;
            }
            else
            {
                response["componentAdded"] = false;
                response["compilationRequired"] = true;
            }

            return response;
        }

        #region SerializedProperty Helpers

        private static void SetSerializedPropertyValue(SerializedProperty prop, object value)
        {
            if (value == null) return;

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    prop.intValue = Convert.ToInt32(value);
                    break;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = Convert.ToBoolean(value);
                    break;
                case SerializedPropertyType.Float:
                    prop.floatValue = Convert.ToSingle(value);
                    break;
                case SerializedPropertyType.String:
                    prop.stringValue = value.ToString();
                    break;
                case SerializedPropertyType.Enum:
                    if (value is int intVal)
                        prop.enumValueIndex = intVal;
                    else if (value is string strVal)
                    {
                        var names = prop.enumDisplayNames;
                        for (int i = 0; i < names.Length; i++)
                        {
                            if (string.Equals(names[i], strVal, StringComparison.OrdinalIgnoreCase))
                            {
                                prop.enumValueIndex = i;
                                break;
                            }
                        }
                    }
                    break;
                case SerializedPropertyType.Vector3:
                    if (value is Dictionary<string, object> v3Dict)
                    {
                        prop.vector3Value = new Vector3(
                            v3Dict.TryGetValue("x", out var x) ? Convert.ToSingle(x) : 0f,
                            v3Dict.TryGetValue("y", out var y) ? Convert.ToSingle(y) : 0f,
                            v3Dict.TryGetValue("z", out var z) ? Convert.ToSingle(z) : 0f
                        );
                    }
                    break;
                case SerializedPropertyType.Vector2:
                    if (value is Dictionary<string, object> v2Dict)
                    {
                        prop.vector2Value = new Vector2(
                            v2Dict.TryGetValue("x", out var vx) ? Convert.ToSingle(vx) : 0f,
                            v2Dict.TryGetValue("y", out var vy) ? Convert.ToSingle(vy) : 0f
                        );
                    }
                    break;
                case SerializedPropertyType.Color:
                    if (value is Dictionary<string, object> cDict)
                    {
                        prop.colorValue = new Color(
                            cDict.TryGetValue("r", out var r) ? Convert.ToSingle(r) : 0f,
                            cDict.TryGetValue("g", out var g) ? Convert.ToSingle(g) : 0f,
                            cDict.TryGetValue("b", out var b) ? Convert.ToSingle(b) : 0f,
                            cDict.TryGetValue("a", out var a) ? Convert.ToSingle(a) : 1f
                        );
                    }
                    break;
            }
        }

        private static object GetSerializedPropertyValue(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return prop.intValue;
                case SerializedPropertyType.Boolean:
                    return prop.boolValue;
                case SerializedPropertyType.Float:
                    return prop.floatValue;
                case SerializedPropertyType.String:
                    return prop.stringValue;
                case SerializedPropertyType.Enum:
                    return prop.enumValueIndex < prop.enumDisplayNames.Length
                        ? prop.enumDisplayNames[prop.enumValueIndex]
                        : prop.enumValueIndex.ToString();
                case SerializedPropertyType.Vector3:
                    var v3 = prop.vector3Value;
                    return new Dictionary<string, object>
                    {
                        ["x"] = v3.x, ["y"] = v3.y, ["z"] = v3.z
                    };
                case SerializedPropertyType.Vector2:
                    var v2 = prop.vector2Value;
                    return new Dictionary<string, object>
                    {
                        ["x"] = v2.x, ["y"] = v2.y
                    };
                case SerializedPropertyType.Color:
                    var c = prop.colorValue;
                    return new Dictionary<string, object>
                    {
                        ["r"] = c.r, ["g"] = c.g, ["b"] = c.b, ["a"] = c.a
                    };
                case SerializedPropertyType.ObjectReference:
                    var obj = prop.objectReferenceValue;
                    return obj != null ? obj.name : null;
                default:
                    return null;
            }
        }

        #endregion
    }
}
