using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Utilities
{
    /// <summary>
    /// Centralized utility methods for command handlers.
    /// Provides common functionality for GameObject path building, vector/color conversions,
    /// serialization, and scene management.
    /// </summary>
    public static class HandlerUtilities
    {
        #region GameObject Utilities

        /// <summary>
        /// Builds the full hierarchy path for a GameObject.
        /// </summary>
        /// <param name="go">The GameObject to get the path for.</param>
        /// <returns>The full path (e.g., "Parent/Child/Grandchild"), or empty string if null.</returns>
        public static string BuildGameObjectPath(GameObject go)
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

        #region Vector Conversions

        /// <summary>
        /// Extracts a Vector3 from a dictionary with x, y, z keys.
        /// </summary>
        /// <param name="dict">The dictionary containing vector components.</param>
        /// <param name="fallback">The fallback value if extraction fails.</param>
        /// <returns>The extracted Vector3 or fallback value.</returns>
        public static Vector3 GetVector3FromDict(Dictionary<string, object> dict, Vector3 fallback = default)
        {
            if (dict == null)
            {
                return fallback;
            }

            float x = dict.TryGetValue("x", out var xObj) ? Convert.ToSingle(xObj) : fallback.x;
            float y = dict.TryGetValue("y", out var yObj) ? Convert.ToSingle(yObj) : fallback.y;
            float z = dict.TryGetValue("z", out var zObj) ? Convert.ToSingle(zObj) : fallback.z;
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Extracts a Vector3 from a payload dictionary at a specific key.
        /// </summary>
        /// <param name="payload">The payload dictionary.</param>
        /// <param name="key">The key to look up.</param>
        /// <param name="fallback">The fallback value if extraction fails.</param>
        /// <returns>The extracted Vector3 or fallback value.</returns>
        public static Vector3 GetVector3FromPayload(Dictionary<string, object> payload, string key, Vector3 fallback = default)
        {
            if (payload == null || !payload.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            if (value is Dictionary<string, object> dict)
            {
                return GetVector3FromDict(dict, fallback);
            }

            return fallback;
        }

        /// <summary>
        /// Extracts a Vector2 from a dictionary with x, y keys.
        /// </summary>
        /// <param name="dict">The dictionary containing vector components.</param>
        /// <param name="fallback">The fallback value if extraction fails.</param>
        /// <returns>The extracted Vector2 or fallback value.</returns>
        public static Vector2 GetVector2FromDict(Dictionary<string, object> dict, Vector2 fallback = default)
        {
            if (dict == null)
            {
                return fallback;
            }

            float x = dict.TryGetValue("x", out var xObj) ? Convert.ToSingle(xObj) : fallback.x;
            float y = dict.TryGetValue("y", out var yObj) ? Convert.ToSingle(yObj) : fallback.y;
            return new Vector2(x, y);
        }

        /// <summary>
        /// Extracts a Vector2 from a payload dictionary at a specific key.
        /// </summary>
        /// <param name="payload">The payload dictionary.</param>
        /// <param name="key">The key to look up.</param>
        /// <param name="fallback">The fallback value if extraction fails.</param>
        /// <returns>The extracted Vector2 or fallback value.</returns>
        public static Vector2 GetVector2FromPayload(Dictionary<string, object> payload, string key, Vector2 fallback = default)
        {
            if (payload == null || !payload.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            if (value is Dictionary<string, object> dict)
            {
                return GetVector2FromDict(dict, fallback);
            }

            return fallback;
        }

        #endregion

        #region Color Conversions

        /// <summary>
        /// Extracts a Color from a dictionary with r, g, b, a keys.
        /// </summary>
        /// <param name="dict">The dictionary containing color components.</param>
        /// <param name="fallback">The fallback value if extraction fails.</param>
        /// <returns>The extracted Color or fallback value.</returns>
        public static Color GetColorFromDict(Dictionary<string, object> dict, Color fallback = default)
        {
            if (dict == null)
            {
                return fallback;
            }

            float r = dict.TryGetValue("r", out var rObj) ? Convert.ToSingle(rObj) : fallback.r;
            float g = dict.TryGetValue("g", out var gObj) ? Convert.ToSingle(gObj) : fallback.g;
            float b = dict.TryGetValue("b", out var bObj) ? Convert.ToSingle(bObj) : fallback.b;
            float a = dict.TryGetValue("a", out var aObj) ? Convert.ToSingle(aObj) : fallback.a;
            return new Color(r, g, b, a);
        }

        /// <summary>
        /// Extracts a Color from a payload dictionary at a specific key.
        /// </summary>
        /// <param name="payload">The payload dictionary.</param>
        /// <param name="key">The key to look up.</param>
        /// <param name="fallback">The fallback value if extraction fails.</param>
        /// <returns>The extracted Color or fallback value.</returns>
        public static Color GetColorFromPayload(Dictionary<string, object> payload, string key, Color fallback = default)
        {
            if (payload == null || !payload.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            if (value is Dictionary<string, object> dict)
            {
                return GetColorFromDict(dict, fallback);
            }

            return fallback;
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Serializes a Vector3 to a dictionary.
        /// </summary>
        /// <param name="vector">The Vector3 to serialize.</param>
        /// <returns>A dictionary with x, y, z keys.</returns>
        public static Dictionary<string, object> SerializeVector3(Vector3 vector)
        {
            return new Dictionary<string, object>
            {
                ["x"] = vector.x,
                ["y"] = vector.y,
                ["z"] = vector.z
            };
        }

        /// <summary>
        /// Serializes a Vector2 to a dictionary.
        /// </summary>
        /// <param name="vector">The Vector2 to serialize.</param>
        /// <returns>A dictionary with x, y keys.</returns>
        public static Dictionary<string, object> SerializeVector2(Vector2 vector)
        {
            return new Dictionary<string, object>
            {
                ["x"] = vector.x,
                ["y"] = vector.y
            };
        }

        /// <summary>
        /// Serializes a Color to a dictionary.
        /// </summary>
        /// <param name="color">The Color to serialize.</param>
        /// <returns>A dictionary with r, g, b, a keys.</returns>
        public static Dictionary<string, object> SerializeColor(Color color)
        {
            return new Dictionary<string, object>
            {
                ["r"] = color.r,
                ["g"] = color.g,
                ["b"] = color.b,
                ["a"] = color.a
            };
        }

        /// <summary>
        /// Serializes a Quaternion to a dictionary.
        /// </summary>
        /// <param name="quaternion">The Quaternion to serialize.</param>
        /// <returns>A dictionary with x, y, z, w keys.</returns>
        public static Dictionary<string, object> SerializeQuaternion(Quaternion quaternion)
        {
            return new Dictionary<string, object>
            {
                ["x"] = quaternion.x,
                ["y"] = quaternion.y,
                ["z"] = quaternion.z,
                ["w"] = quaternion.w
            };
        }

        /// <summary>
        /// Serializes Euler angles to a dictionary.
        /// </summary>
        /// <param name="eulerAngles">The Euler angles to serialize.</param>
        /// <returns>A dictionary with x, y, z keys.</returns>
        public static Dictionary<string, object> SerializeEulerAngles(Vector3 eulerAngles)
        {
            return SerializeVector3(eulerAngles);
        }

        #endregion

        #region Scene Management

        /// <summary>
        /// Marks the scenes of the specified GameObjects as dirty.
        /// </summary>
        /// <param name="gameObjects">The GameObjects whose scenes should be marked dirty.</param>
        public static void MarkScenesDirty(IEnumerable<GameObject> gameObjects)
        {
            if (gameObjects == null)
            {
                return;
            }

            foreach (var scene in gameObjects.Where(o => o != null).Select(o => o.scene).Distinct())
            {
                if (scene.IsValid())
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }
        }

        /// <summary>
        /// Marks the scene of the specified GameObject as dirty.
        /// </summary>
        /// <param name="gameObject">The GameObject whose scene should be marked dirty.</param>
        public static void MarkSceneDirty(GameObject gameObject)
        {
            if (gameObject != null && gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
        }

        #endregion

        #region Payload Helpers

        /// <summary>
        /// Gets a boolean value from a payload, with a default fallback.
        /// </summary>
        /// <param name="payload">The payload dictionary.</param>
        /// <param name="key">The key to look up.</param>
        /// <param name="defaultValue">The default value if key is missing or null.</param>
        /// <returns>The boolean value or default.</returns>
        public static bool GetBoolOrDefault(Dictionary<string, object> payload, string key, bool defaultValue = false)
        {
            if (payload == null || !payload.TryGetValue(key, out var value) || value == null)
            {
                return defaultValue;
            }

            if (value is bool boolValue)
            {
                return boolValue;
            }

            if (bool.TryParse(value.ToString(), out var parsedValue))
            {
                return parsedValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets an integer value from a payload, with a default fallback.
        /// </summary>
        /// <param name="payload">The payload dictionary.</param>
        /// <param name="key">The key to look up.</param>
        /// <param name="defaultValue">The default value if key is missing or null.</param>
        /// <returns>The integer value or default.</returns>
        public static int GetIntOrDefault(Dictionary<string, object> payload, string key, int defaultValue = 0)
        {
            if (payload == null || !payload.TryGetValue(key, out var value) || value == null)
            {
                return defaultValue;
            }

            return Convert.ToInt32(value);
        }

        /// <summary>
        /// Gets a float value from a payload, with a default fallback.
        /// </summary>
        /// <param name="payload">The payload dictionary.</param>
        /// <param name="key">The key to look up.</param>
        /// <param name="defaultValue">The default value if key is missing or null.</param>
        /// <returns>The float value or default.</returns>
        public static float GetFloatOrDefault(Dictionary<string, object> payload, string key, float defaultValue = 0f)
        {
            if (payload == null || !payload.TryGetValue(key, out var value) || value == null)
            {
                return defaultValue;
            }

            return Convert.ToSingle(value);
        }

        /// <summary>
        /// Gets a list of strings from a payload.
        /// </summary>
        /// <param name="payload">The payload dictionary.</param>
        /// <param name="key">The key to look up.</param>
        /// <returns>A list of strings, or empty list if not found.</returns>
        public static List<string> GetStringList(Dictionary<string, object> payload, string key)
        {
            if (payload == null || !payload.TryGetValue(key, out var value) || value == null)
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

        /// <summary>
        /// Gets a dictionary from a payload at a specific key.
        /// </summary>
        /// <param name="payload">The payload dictionary.</param>
        /// <param name="key">The key to look up.</param>
        /// <returns>The nested dictionary or null if not found.</returns>
        public static Dictionary<string, object> GetDictOrDefault(Dictionary<string, object> payload, string key)
        {
            if (payload != null && payload.TryGetValue(key, out var value) && value is Dictionary<string, object> dict)
            {
                return dict;
            }
            return null;
        }

        #endregion
    }
}
