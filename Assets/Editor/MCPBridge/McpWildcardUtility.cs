using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MCP.Editor
{
    /// <summary>
    /// Provides wildcard and multiple object resolution utilities for MCP bridge operations.
    /// Supports wildcard patterns (*, ?) and regular expressions for GameObject, Asset, and Component searches.
    /// </summary>
    internal static class McpWildcardUtility
    {
        /// <summary>
        /// Resolves GameObjects using wildcard or exact path matching.
        /// Supports patterns like "Player*", "Enemy?", or exact paths like "Player/Camera".
        /// </summary>
        /// <param name="pattern">Hierarchy path pattern. Can include wildcards (* and ?) or be an exact path.</param>
        /// <param name="useRegex">If true, treats pattern as a regular expression instead of wildcard pattern.</param>
        /// <returns>List of matching GameObjects.</returns>
        public static List<GameObject> ResolveGameObjects(string pattern, bool useRegex = false)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return new List<GameObject>();
            }

            // Check if pattern contains wildcards
            bool hasWildcard = pattern.Contains('*') || pattern.Contains('?');

            // If no wildcards and not regex, try exact match first
            if (!hasWildcard && !useRegex)
            {
                var exactMatch = GameObject.Find(pattern);
                if (exactMatch != null)
                {
                    return new List<GameObject> { exactMatch };
                }
                return new List<GameObject>();
            }

            // Convert wildcard pattern to regex if needed
            Regex regex;
            if (useRegex)
            {
                try
                {
                    regex = new Regex(pattern, RegexOptions.IgnoreCase);
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidOperationException($"Invalid regex pattern: {pattern}. Error: {ex.Message}");
                }
            }
            else
            {
                regex = WildcardToRegex(pattern);
            }

            // Search all GameObjects in all loaded scenes
            var results = new List<GameObject>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                var rootObjects = scene.GetRootGameObjects();
                foreach (var root in rootObjects)
                {
                    SearchGameObjectRecursive(root, regex, results);
                }
            }

            return results;
        }

        /// <summary>
        /// Resolves asset paths using wildcard or exact path matching.
        /// Supports patterns like "Assets/Scripts/*.cs" or "Assets/Prefabs/Player*.prefab".
        /// </summary>
        /// <param name="pattern">Asset path pattern. Can include wildcards (* and ?) or be an exact path.</param>
        /// <param name="useRegex">If true, treats pattern as a regular expression instead of wildcard pattern.</param>
        /// <returns>List of matching asset paths.</returns>
        public static List<string> ResolveAssetPaths(string pattern, bool useRegex = false)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return new List<string>();
            }

            // Check if pattern contains wildcards
            bool hasWildcard = pattern.Contains('*') || pattern.Contains('?');

            // If no wildcards and not regex, check if exact path exists
            if (!hasWildcard && !useRegex)
            {
                if (AssetDatabase.AssetPathExists(pattern))
                {
                    return new List<string> { pattern };
                }
                return new List<string>();
            }

            // Convert wildcard pattern to regex if needed
            Regex regex;
            if (useRegex)
            {
                try
                {
                    regex = new Regex(pattern, RegexOptions.IgnoreCase);
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidOperationException($"Invalid regex pattern: {pattern}. Error: {ex.Message}");
                }
            }
            else
            {
                regex = WildcardToRegex(pattern);
            }

            // Find all assets in the project
            var allAssetPaths = AssetDatabase.GetAllAssetPaths();
            var results = new List<string>();

            foreach (var assetPath in allAssetPaths)
            {
                if (regex.IsMatch(assetPath))
                {
                    results.Add(assetPath);
                }
            }

            return results;
        }

        /// <summary>
        /// Resolves Components on GameObjects using GameObject pattern and component type.
        /// </summary>
        /// <param name="gameObjectPattern">GameObject hierarchy path pattern.</param>
        /// <param name="componentType">Full type name of the component (e.g., "UnityEngine.Transform").</param>
        /// <param name="useRegex">If true, treats pattern as a regular expression.</param>
        /// <returns>List of matching Components.</returns>
        public static List<Component> ResolveComponents(string gameObjectPattern, string componentType, bool useRegex = false)
        {
            var gameObjects = ResolveGameObjects(gameObjectPattern, useRegex);
            var results = new List<Component>();

            Type type = ResolveComponentType(componentType);
            if (type == null)
            {
                throw new InvalidOperationException($"Component type not found: {componentType}");
            }

            foreach (var go in gameObjects)
            {
                var component = go.GetComponent(type);
                if (component != null)
                {
                    results.Add(component);
                }
            }

            return results;
        }

        /// <summary>
        /// Resolves a component type from a type name string.
        /// Searches through all loaded assemblies.
        /// </summary>
        /// <param name="typeName">Full type name (e.g., "UnityEngine.Transform").</param>
        /// <returns>Type if found, null otherwise.</returns>
        public static Type ResolveComponentType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            // Try direct Type.GetType first
            var type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            // Search through all loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        /// <summary>
        /// Converts a wildcard pattern (* and ?) to a regular expression.
        /// </summary>
        /// <param name="pattern">Wildcard pattern string.</param>
        /// <returns>Compiled Regex object.</returns>
        private static Regex WildcardToRegex(string pattern)
        {
            // Escape regex special characters except * and ?
            var regexPattern = Regex.Escape(pattern)
                .Replace("\\*", ".*")   // * matches any characters
                .Replace("\\?", ".");    // ? matches single character

            // Match entire string
            regexPattern = "^" + regexPattern + "$";

            return new Regex(regexPattern, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Recursively searches GameObjects in hierarchy and adds matches to results.
        /// </summary>
        /// <param name="gameObject">Current GameObject to check.</param>
        /// <param name="regex">Regex pattern to match against.</param>
        /// <param name="results">List to accumulate matching GameObjects.</param>
        private static void SearchGameObjectRecursive(GameObject gameObject, Regex regex, List<GameObject> results)
        {
            var hierarchyPath = GetHierarchyPath(gameObject);
            if (regex.IsMatch(hierarchyPath))
            {
                results.Add(gameObject);
            }

            // Search children
            var transform = gameObject.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                SearchGameObjectRecursive(transform.GetChild(i).gameObject, regex, results);
            }
        }

        /// <summary>
        /// Gets the full hierarchy path of a GameObject.
        /// </summary>
        /// <param name="go">GameObject to get path for.</param>
        /// <returns>Full hierarchy path (e.g., "Parent/Child/Target").</returns>
        private static string GetHierarchyPath(GameObject go)
        {
            var stack = new System.Collections.Generic.Stack<string>();
            var current = go.transform;
            while (current != null)
            {
                stack.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", stack);
        }

        /// <summary>
        /// Checks if a pattern contains wildcard characters.
        /// </summary>
        /// <param name="pattern">Pattern string to check.</param>
        /// <returns>True if pattern contains * or ? characters.</returns>
        public static bool IsWildcardPattern(string pattern)
        {
            return !string.IsNullOrEmpty(pattern) && (pattern.Contains('*') || pattern.Contains('?'));
        }

        /// <summary>
        /// Checks if a string matches a wildcard pattern.
        /// </summary>
        /// <param name="input">String to test.</param>
        /// <param name="pattern">Wildcard pattern (* and ?).</param>
        /// <param name="useRegex">If true, treats pattern as regex instead of wildcard.</param>
        /// <returns>True if input matches pattern.</returns>
        public static bool IsMatch(string input, string pattern, bool useRegex = false)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return true;
            }

            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            Regex regex;
            if (useRegex)
            {
                try
                {
                    regex = new Regex(pattern, RegexOptions.IgnoreCase);
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }
            else
            {
                regex = WildcardToRegex(pattern);
            }

            return regex.IsMatch(input);
        }

        /// <summary>
        /// Loads multiple assets by paths.
        /// </summary>
        /// <typeparam name="T">Asset type to load.</typeparam>
        /// <param name="assetPaths">List of asset paths.</param>
        /// <returns>List of loaded assets (null entries for failed loads).</returns>
        public static List<T> LoadAssets<T>(List<string> assetPaths) where T : UnityEngine.Object
        {
            var results = new List<T>();
            foreach (var path in assetPaths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                results.Add(asset);
            }
            return results;
        }
    }
}
