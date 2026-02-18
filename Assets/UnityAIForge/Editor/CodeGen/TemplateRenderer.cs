using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MCP.Editor.CodeGen
{
    /// <summary>
    /// Template variable expansion engine for code generation.
    /// Supports {{VAR}} substitution, {{#IF VAR}}...{{/IF}} conditionals,
    /// and {{#FOREACH VAR}}...{{/FOREACH}} list expansion.
    /// </summary>
    public static class TemplateRenderer
    {
        /// <summary>
        /// Renders a template string by expanding variables, conditionals, and loops.
        /// </summary>
        /// <param name="template">The template string with {{VAR}} placeholders.</param>
        /// <param name="variables">Dictionary of variable names to values.</param>
        /// <returns>The rendered string with all placeholders resolved.</returns>
        public static string Render(string template, Dictionary<string, object> variables)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            if (variables == null)
                variables = new Dictionary<string, object>();

            var result = template;

            // Process FOREACH blocks first (they may contain IF blocks and variables)
            result = ProcessForeachBlocks(result, variables);

            // Process IF blocks (may be nested)
            result = ProcessIfBlocks(result, variables);

            // Process simple variable substitution
            result = ProcessVariables(result, variables);

            return result;
        }

        /// <summary>
        /// Processes {{#FOREACH VAR}}...{{/FOREACH}} blocks.
        /// Each item in the list is expected to be a Dictionary&lt;string, object&gt;.
        /// </summary>
        private static string ProcessForeachBlocks(string template, Dictionary<string, object> variables)
        {
            // Match {{#FOREACH VAR}}...{{/FOREACH}} with balanced nesting
            var pattern = @"\{\{#FOREACH\s+(\w+)\}\}(.*?)\{\{/FOREACH\}\}";
            var regex = new Regex(pattern, RegexOptions.Singleline);

            // Process from innermost to outermost
            while (regex.IsMatch(template))
            {
                template = regex.Replace(template, match =>
                {
                    var varName = match.Groups[1].Value;
                    var body = match.Groups[2].Value;

                    if (!variables.TryGetValue(varName, out var listObj))
                        return string.Empty;

                    var list = CoerceToList(listObj);
                    if (list == null || list.Count == 0)
                        return string.Empty;

                    var sb = new StringBuilder();
                    for (int i = 0; i < list.Count; i++)
                    {
                        var item = list[i];
                        // Merge item variables with parent variables
                        var itemVars = new Dictionary<string, object>(variables);

                        // Add loop metadata
                        itemVars["_INDEX"] = i;
                        itemVars["_FIRST"] = i == 0;
                        itemVars["_LAST"] = i == list.Count - 1;

                        if (item is Dictionary<string, object> dict)
                        {
                            foreach (var kvp in dict)
                            {
                                itemVars[kvp.Key] = kvp.Value;
                            }
                        }
                        else
                        {
                            // Scalar item — accessible as {{_ITEM}}
                            itemVars["_ITEM"] = item;
                        }

                        // Recursively render the body with merged variables
                        sb.Append(Render(body, itemVars));
                    }

                    return sb.ToString();
                });
            }

            return template;
        }

        /// <summary>
        /// Processes {{#IF VAR}}...{{/IF}} and {{#IF VAR}}...{{#ELSE}}...{{/IF}} blocks.
        /// A variable is truthy if it exists and is not null, false, 0, or empty string.
        /// Also supports negation: {{#IF !VAR}}.
        /// </summary>
        private static string ProcessIfBlocks(string template, Dictionary<string, object> variables)
        {
            // Match {{#IF VAR}}...{{#ELSE}}...{{/IF}} or {{#IF VAR}}...{{/IF}}
            var pattern = @"\{\{#IF\s+(!?\w+)\}\}(.*?)(?:\{\{#ELSE\}\}(.*?))?\{\{/IF\}\}";
            var regex = new Regex(pattern, RegexOptions.Singleline);

            // Process from innermost to outermost
            while (regex.IsMatch(template))
            {
                template = regex.Replace(template, match =>
                {
                    var varExpr = match.Groups[1].Value;
                    var trueBlock = match.Groups[2].Value;
                    var elseBlock = match.Groups[3].Success ? match.Groups[3].Value : string.Empty;

                    bool negate = varExpr.StartsWith("!");
                    var varName = negate ? varExpr.Substring(1) : varExpr;

                    bool isTruthy = IsTruthy(variables, varName);
                    if (negate) isTruthy = !isTruthy;

                    return isTruthy ? trueBlock : elseBlock;
                });
            }

            return template;
        }

        /// <summary>
        /// Processes simple {{VAR}} variable substitutions.
        /// </summary>
        private static string ProcessVariables(string template, Dictionary<string, object> variables)
        {
            var pattern = @"\{\{(\w+)\}\}";
            return Regex.Replace(template, pattern, match =>
            {
                var varName = match.Groups[1].Value;
                if (variables.TryGetValue(varName, out var value) && value != null)
                {
                    return FormatValue(value);
                }
                // Leave unresolved variables as-is for debugging
                return match.Value;
            });
        }

        /// <summary>
        /// Determines if a variable is "truthy" for conditional blocks.
        /// </summary>
        private static bool IsTruthy(Dictionary<string, object> variables, string varName)
        {
            if (!variables.TryGetValue(varName, out var value))
                return false;

            if (value == null)
                return false;

            if (value is bool b)
                return b;

            if (value is int i)
                return i != 0;

            if (value is long l)
                return l != 0;

            if (value is float f)
                return f != 0f;

            if (value is double d)
                return d != 0.0;

            if (value is string s)
                return !string.IsNullOrEmpty(s);

            if (value is IList list)
                return list.Count > 0;

            return true;
        }

        /// <summary>
        /// Formats a value for insertion into generated code.
        /// Floats use "f" suffix, bools are lowercase, etc.
        /// </summary>
        private static string FormatValue(object value)
        {
            if (value is float f)
                return f.ToString(CultureInfo.InvariantCulture) + "f";

            if (value is double d)
                return d.ToString(CultureInfo.InvariantCulture);

            if (value is bool b)
                return b ? "true" : "false";

            if (value is int || value is long)
                return value.ToString();

            return value.ToString();
        }

        /// <summary>
        /// Attempts to coerce a value to a list of objects.
        /// </summary>
        private static IList CoerceToList(object value)
        {
            if (value is IList list)
                return list;

            return null;
        }
    }
}
