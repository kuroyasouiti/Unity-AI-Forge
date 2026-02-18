using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.CodeGen
{
    /// <summary>
    /// Result of a script generation operation.
    /// </summary>
    public class GenerationResult
    {
        /// <summary>Whether the generation succeeded.</summary>
        public bool Success;

        /// <summary>Path to the generated script file (e.g. "Assets/Scripts/Generated/PlayerHealth.cs").</summary>
        public string ScriptPath;

        /// <summary>Name of the generated C# class.</summary>
        public string ClassName;

        /// <summary>
        /// The resolved Type after compilation.
        /// Will be null immediately after generation — becomes available after Unity recompiles.
        /// </summary>
        public Type GeneratedType;

        /// <summary>Error message if Success is false.</summary>
        public string ErrorMessage;
    }

    /// <summary>
    /// Main entry point for code generation.
    /// Loads templates, renders them with variables, writes .cs files,
    /// and registers metadata in the GeneratedScriptTracker.
    /// </summary>
    public static class ScriptGenerator
    {
        /// <summary>Default output directory for generated scripts.</summary>
        public const string DefaultOutputDir = "Assets/Scripts/Generated";

        /// <summary>
        /// Path to the Templates folder inside the package.
        /// Template files are named {TemplateName}.cs.txt.
        /// </summary>
        private static readonly string TemplatesDir =
            "Assets/UnityAIForge/Editor/CodeGen/Templates";

        /// <summary>
        /// Generates a C# script from a template and registers it in the tracker.
        /// After Unity recompiles, the generated type can be added as a component.
        /// </summary>
        /// <param name="target">The GameObject that will receive the component (used for tracker metadata). Can be null for ScriptableObjects.</param>
        /// <param name="templateName">Template name without extension (e.g. "Health").</param>
        /// <param name="className">The C# class name to generate (e.g. "PlayerHealth").</param>
        /// <param name="componentId">Logical ID for the handler (e.g. "player_hp").</param>
        /// <param name="variables">Template variables. CLASS_NAME is set automatically if not provided.</param>
        /// <param name="outputDir">Output directory. Defaults to <see cref="DefaultOutputDir"/>.</param>
        /// <returns>Generation result with file path and class name.</returns>
        public static GenerationResult Generate(
            GameObject target,
            string templateName,
            string className,
            string componentId,
            Dictionary<string, object> variables,
            string outputDir = null)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrEmpty(templateName))
                    return Fail("templateName is required.");
                if (string.IsNullOrEmpty(className))
                    return Fail("className is required.");
                if (!IsValidClassName(className))
                    return Fail($"'{className}' is not a valid C# class name.");
                if (string.IsNullOrEmpty(componentId))
                    return Fail("componentId is required.");

                // Check for duplicate class name in tracker
                var tracker = GeneratedScriptTracker.Instance;
                var existingByClass = tracker.FindByClassName(className);
                if (existingByClass != null && existingByClass.componentId != componentId)
                {
                    return Fail($"Class name '{className}' is already used by component '{existingByClass.componentId}'.");
                }

                // Load template
                var templateContent = LoadTemplate(templateName);
                if (templateContent == null)
                    return Fail($"Template '{templateName}' not found at '{GetTemplatePath(templateName)}'.");

                // Prepare variables (ensure CLASS_NAME is always set)
                var vars = variables != null
                    ? new Dictionary<string, object>(variables)
                    : new Dictionary<string, object>();
                vars["CLASS_NAME"] = className;

                // Render template
                var script = TemplateRenderer.Render(templateContent, vars);

                // Determine output path
                var dir = string.IsNullOrEmpty(outputDir) ? DefaultOutputDir : outputDir;
                EnsureDirectoryExists(dir);
                var scriptPath = Path.Combine(dir, className + ".cs").Replace("\\", "/");

                // Write file
                File.WriteAllText(scriptPath, script, System.Text.Encoding.UTF8);

                // Import the new asset so Unity starts compiling
                AssetDatabase.ImportAsset(scriptPath, ImportAssetOptions.ForceUpdate);

                // Register in tracker
                var variablesJson = SerializeVariables(vars);
                tracker.Register(new GeneratedScriptTracker.Entry
                {
                    componentId = componentId,
                    className = className,
                    scriptPath = scriptPath,
                    templateName = templateName,
                    gameObjectPath = target != null ? GetGameObjectPath(target) : "",
                    variablesJson = variablesJson
                });

                Debug.Log($"[ScriptGenerator] Generated {scriptPath} (class: {className}, template: {templateName})");

                return new GenerationResult
                {
                    Success = true,
                    ScriptPath = scriptPath,
                    ClassName = className
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScriptGenerator] Generation failed: {ex.Message}");
                return Fail(ex.Message);
            }
        }

        /// <summary>
        /// Regenerates an existing script by re-rendering its template with new variables.
        /// The script path and class name remain the same.
        /// </summary>
        /// <param name="componentId">The logical component ID to regenerate.</param>
        /// <param name="variables">New template variables (merged with existing). CLASS_NAME is preserved.</param>
        /// <returns>Generation result.</returns>
        public static GenerationResult Regenerate(
            string componentId,
            Dictionary<string, object> variables)
        {
            try
            {
                var tracker = GeneratedScriptTracker.Instance;
                var entry = tracker.FindByComponentId(componentId);
                if (entry == null)
                    return Fail($"No generated script found for component '{componentId}'.");

                // Load template
                var templateContent = LoadTemplate(entry.templateName);
                if (templateContent == null)
                    return Fail($"Template '{entry.templateName}' not found.");

                // Merge existing variables with new ones
                var existingVars = DeserializeVariables(entry.variablesJson);
                var mergedVars = new Dictionary<string, object>(existingVars);
                if (variables != null)
                {
                    foreach (var kvp in variables)
                    {
                        mergedVars[kvp.Key] = kvp.Value;
                    }
                }
                mergedVars["CLASS_NAME"] = entry.className; // always preserve

                // Render
                var script = TemplateRenderer.Render(templateContent, mergedVars);

                // Write
                File.WriteAllText(entry.scriptPath, script, System.Text.Encoding.UTF8);
                AssetDatabase.ImportAsset(entry.scriptPath, ImportAssetOptions.ForceUpdate);

                // Update tracker entry
                entry.variablesJson = SerializeVariables(mergedVars);
                tracker.Register(entry); // re-register updates the existing entry

                Debug.Log($"[ScriptGenerator] Regenerated {entry.scriptPath}");

                return new GenerationResult
                {
                    Success = true,
                    ScriptPath = entry.scriptPath,
                    ClassName = entry.className
                };
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
        }

        /// <summary>
        /// Deletes a generated script file and removes it from the tracker.
        /// </summary>
        /// <param name="componentId">The logical component ID to delete.</param>
        /// <returns>True if the script was found and deleted.</returns>
        public static bool Delete(string componentId)
        {
            var tracker = GeneratedScriptTracker.Instance;
            var entry = tracker.FindByComponentId(componentId);
            if (entry == null)
                return false;

            // Delete the script file
            if (File.Exists(entry.scriptPath))
            {
                AssetDatabase.DeleteAsset(entry.scriptPath);
            }

            // Unregister from tracker
            tracker.Unregister(componentId);

            Debug.Log($"[ScriptGenerator] Deleted {entry.scriptPath} (component: {componentId})");
            return true;
        }

        /// <summary>
        /// Resolves a generated class name to its System.Type after compilation.
        /// Returns null if the type hasn't been compiled yet.
        /// </summary>
        /// <param name="className">The generated class name.</param>
        /// <returns>The resolved Type, or null.</returns>
        public static Type ResolveGeneratedType(string className)
        {
            if (string.IsNullOrEmpty(className))
                return null;

            // Generated scripts are in the global namespace, so search all assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(className);
                if (type != null)
                    return type;
            }

            return null;
        }

        /// <summary>
        /// Converts a component ID (e.g. "player_hp") to a PascalCase class name.
        /// </summary>
        /// <param name="id">The snake_case or kebab-case ID.</param>
        /// <param name="suffix">Optional suffix (e.g. "Health").</param>
        /// <returns>PascalCase class name (e.g. "PlayerHpHealth").</returns>
        public static string ToPascalCase(string id, string suffix = "")
        {
            if (string.IsNullOrEmpty(id))
                return suffix;

            var parts = Regex.Split(id, @"[-_\s]+");
            var result = string.Concat(parts.Select(p =>
                p.Length > 0
                    ? char.ToUpper(p[0], CultureInfo.InvariantCulture) + p.Substring(1)
                    : ""));

            return result + suffix;
        }

        #region Internal Helpers

        private static string GetTemplatePath(string templateName)
        {
            return Path.Combine(TemplatesDir, templateName + ".cs.txt").Replace("\\", "/");
        }

        internal static string LoadTemplate(string templateName)
        {
            var path = GetTemplatePath(templateName);
            if (!File.Exists(path))
                return null;
            return File.ReadAllText(path, System.Text.Encoding.UTF8);
        }

        private static void EnsureDirectoryExists(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                // Let Unity know about the new folder
                AssetDatabase.Refresh();
            }
        }

        private static bool IsValidClassName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            // Must start with letter or underscore, rest can be alphanumeric or underscore
            return Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_]*$");
        }

        private static string GetGameObjectPath(GameObject go)
        {
            if (go == null) return "";
            var path = go.name;
            var parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        private static GenerationResult Fail(string message)
        {
            return new GenerationResult
            {
                Success = false,
                ErrorMessage = message
            };
        }

        /// <summary>
        /// Simple JSON-like serialization for template variables.
        /// Only supports primitive types (string, int, float, bool).
        /// Complex types are serialized as their ToString().
        /// </summary>
        internal static string SerializeVariables(Dictionary<string, object> vars)
        {
            if (vars == null || vars.Count == 0)
                return "{}";

            var entries = new List<string>();
            foreach (var kvp in vars)
            {
                var value = SerializeValue(kvp.Value);
                entries.Add($"\"{EscapeJsonString(kvp.Key)}\":{value}");
            }
            return "{" + string.Join(",", entries) + "}";
        }

        private static string SerializeValue(object value)
        {
            if (value == null)
                return "null";
            if (value is bool b)
                return b ? "true" : "false";
            if (value is int i)
                return i.ToString(CultureInfo.InvariantCulture);
            if (value is long l)
                return l.ToString(CultureInfo.InvariantCulture);
            if (value is float f)
                return f.ToString(CultureInfo.InvariantCulture);
            if (value is double d)
                return d.ToString(CultureInfo.InvariantCulture);
            return $"\"{EscapeJsonString(value.ToString())}\"";
        }

        private static string EscapeJsonString(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }

        /// <summary>
        /// Simple JSON-like deserialization for template variables.
        /// Parses the output of SerializeVariables.
        /// </summary>
        internal static Dictionary<string, object> DeserializeVariables(string json)
        {
            var result = new Dictionary<string, object>();
            if (string.IsNullOrEmpty(json) || json == "{}")
                return result;

            // Use Unity's built-in JSON utility isn't suitable here since we need Dictionary.
            // Simple key-value parser for our controlled format.
            try
            {
                // Remove outer braces
                var content = json.Trim();
                if (content.StartsWith("{")) content = content.Substring(1);
                if (content.EndsWith("}")) content = content.Substring(0, content.Length - 1);

                // Parse key:value pairs
                int pos = 0;
                while (pos < content.Length)
                {
                    // Skip whitespace and commas
                    while (pos < content.Length && (content[pos] == ',' || content[pos] == ' '))
                        pos++;
                    if (pos >= content.Length) break;

                    // Parse key
                    if (content[pos] != '"') break;
                    var keyEnd = content.IndexOf('"', pos + 1);
                    if (keyEnd < 0) break;
                    var key = content.Substring(pos + 1, keyEnd - pos - 1);
                    pos = keyEnd + 1;

                    // Skip colon
                    while (pos < content.Length && content[pos] != ':') pos++;
                    pos++; // skip ':'

                    // Parse value
                    while (pos < content.Length && content[pos] == ' ') pos++;
                    if (pos >= content.Length) break;

                    object value;
                    if (content[pos] == '"')
                    {
                        // String value
                        var valueEnd = FindClosingQuote(content, pos + 1);
                        value = UnescapeJsonString(content.Substring(pos + 1, valueEnd - pos - 1));
                        pos = valueEnd + 1;
                    }
                    else if (content.Substring(pos).StartsWith("true"))
                    {
                        value = true;
                        pos += 4;
                    }
                    else if (content.Substring(pos).StartsWith("false"))
                    {
                        value = false;
                        pos += 5;
                    }
                    else if (content.Substring(pos).StartsWith("null"))
                    {
                        value = null;
                        pos += 4;
                    }
                    else
                    {
                        // Number
                        var numEnd = pos;
                        while (numEnd < content.Length && content[numEnd] != ',' && content[numEnd] != '}')
                            numEnd++;
                        var numStr = content.Substring(pos, numEnd - pos).Trim();
                        if (numStr.Contains("."))
                        {
                            float.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var fv);
                            value = fv;
                        }
                        else
                        {
                            int.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var iv);
                            value = iv;
                        }
                        pos = numEnd;
                    }

                    result[key] = value;
                }
            }
            catch
            {
                // Return whatever we parsed so far
            }

            return result;
        }

        private static int FindClosingQuote(string s, int startAfterQuote)
        {
            for (int i = startAfterQuote; i < s.Length; i++)
            {
                if (s[i] == '\\')
                {
                    i++; // skip escaped char
                    continue;
                }
                if (s[i] == '"')
                    return i;
            }
            return s.Length - 1;
        }

        private static string UnescapeJsonString(string s)
        {
            return s.Replace("\\\"", "\"").Replace("\\\\", "\\")
                    .Replace("\\n", "\n").Replace("\\r", "\r")
                    .Replace("\\t", "\t");
        }

        #endregion
    }
}
