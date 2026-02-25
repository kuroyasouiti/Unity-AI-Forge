using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;

namespace MCP.Editor.Utilities.GraphAnalysis
{
    /// <summary>
    /// Analyzes C# source code structure using regex-based parsing.
    /// Provides source-level analysis with line numbers — complementing
    /// ClassDependencyAnalyzer (reflection-based) and TypeCatalogAnalyzer.
    /// </summary>
    public class ScriptSyntaxAnalyzer
    {
        #region Regex Patterns

        // Strip comments and strings for accurate matching
        private static readonly Regex BlockCommentPattern = new Regex(@"/\*[\s\S]*?\*/", RegexOptions.Compiled);
        private static readonly Regex LineCommentPattern = new Regex(@"//.*$", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex StringLiteralPattern = new Regex(@"@""[^""]*""|""(?:\\.|[^""\\])*""", RegexOptions.Compiled);

        // Structural patterns
        private static readonly Regex UsingPattern = new Regex(
            @"^\s*using\s+(?:static\s+)?([\w.]+)\s*;",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex NamespacePattern = new Regex(
            @"^\s*namespace\s+([\w.]+)",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex TypeDeclPattern = new Regex(
            @"^\s*(?:(public|private|protected|internal)\s+)?(?:(static|abstract|sealed|partial)\s+)*(?:(partial)\s+)?(class|struct|interface|enum|record)\s+(\w+)(?:<([^>]+)>)?(?:\s*:\s*([^{]+))?\s*\{?",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex MethodPattern = new Regex(
            @"^\s*(?:\[[\w\s,()""=.]+\]\s*)*(?:(public|private|protected|internal)\s+)?(?:(static|virtual|override|abstract|async|new|sealed|extern)\s+)*(?:(async)\s+)?([\w<>\[\],?\s]+?)\s+(\w+)\s*(<[^>]+>)?\s*\(([^)]*)\)\s*(?:where\s+[^{;]+)?\s*[{;=]",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex FieldPattern = new Regex(
            @"^\s*(?:\[[\w\s,()""=.]+\]\s*)*(?:(public|private|protected|internal)\s+)?(?:(static|readonly|const|volatile|new)\s+)*([\w<>\[\],?\s.]+?)\s+(\w+)\s*(?:=\s*[^;]+)?\s*;",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex PropertyPattern = new Regex(
            @"^\s*(?:\[[\w\s,()""=.]+\]\s*)*(?:(public|private|protected|internal)\s+)?(?:(static|virtual|override|abstract|new|sealed)\s+)*([\w<>\[\],?\s.]+?)\s+(\w+)\s*\{",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex AttributePattern = new Regex(
            @"\[(\w+)(?:\([^)]*\))?\]",
            RegexOptions.Compiled);

        #endregion

        /// <summary>
        /// Analyze the structure of a single C# script file.
        /// Returns namespaces, types, methods, fields, properties, using directives, and line metrics.
        /// </summary>
        public Dictionary<string, object> AnalyzeScript(string scriptPath)
        {
            ValidateScriptPath(scriptPath);
            var fullPath = Path.GetFullPath(scriptPath);
            var content = File.ReadAllText(fullPath);
            var lines = content.Split('\n');
            var strippedContent = StripCommentsAndStrings(content);

            var result = new Dictionary<string, object>
            {
                ["scriptPath"] = scriptPath,
                ["fileName"] = Path.GetFileName(scriptPath),
                ["totalLines"] = lines.Length,
                ["blankLines"] = lines.Count(l => string.IsNullOrWhiteSpace(l)),
                ["commentLines"] = CountCommentLines(content),
            };

            // Using directives
            var usings = new List<string>();
            foreach (Match m in UsingPattern.Matches(strippedContent))
            {
                usings.Add(m.Groups[1].Value);
            }
            result["usings"] = usings;

            // Namespace
            var nsMatch = NamespacePattern.Match(strippedContent);
            result["namespace"] = nsMatch.Success ? nsMatch.Groups[1].Value : null;

            // Type declarations
            var types = ParseTypeDeclarations(content, strippedContent, lines);
            result["types"] = types;
            result["typeCount"] = types.Count;

            // Aggregate method/field/property counts
            var methodCount = 0;
            var fieldCount = 0;
            var propertyCount = 0;
            foreach (var t in types)
            {
                if (t.TryGetValue("methods", out var methods) && methods is List<Dictionary<string, object>> methodList)
                    methodCount += methodList.Count;
                if (t.TryGetValue("fields", out var fields) && fields is List<Dictionary<string, object>> fieldList)
                    fieldCount += fieldList.Count;
                if (t.TryGetValue("properties", out var props) && props is List<Dictionary<string, object>> propList)
                    propertyCount += propList.Count;
            }

            result["methodCount"] = methodCount;
            result["fieldCount"] = fieldCount;
            result["propertyCount"] = propertyCount;
            result["codeLines"] = lines.Length - (int)result["blankLines"] - (int)result["commentLines"];

            return result;
        }

        /// <summary>
        /// Find all references to a symbol (method, field, class, property) across project scripts.
        /// </summary>
        public Dictionary<string, object> FindReferences(string symbolName, string symbolType, string searchPath)
        {
            if (string.IsNullOrEmpty(symbolName))
                throw new ArgumentException("symbolName is required");

            var scriptPaths = FindAllScripts(searchPath);
            var references = new List<Dictionary<string, object>>();

            foreach (var scriptPath in scriptPaths)
            {
                try
                {
                    var fullPath = Path.GetFullPath(scriptPath);
                    var content = File.ReadAllText(fullPath);

                    // Quick check before expensive analysis
                    if (!content.Contains(symbolName)) continue;

                    var strippedContent = StripCommentsAndStrings(content);
                    if (!strippedContent.Contains(symbolName)) continue;

                    var strippedLines = strippedContent.Split('\n');
                    var originalLines = content.Split('\n');

                    for (int i = 0; i < strippedLines.Length; i++)
                    {
                        var line = strippedLines[i];
                        if (!line.Contains(symbolName)) continue;

                        var refType = ClassifyReference(line, symbolName, symbolType);
                        if (refType == null) continue;

                        references.Add(new Dictionary<string, object>
                        {
                            ["scriptPath"] = scriptPath,
                            ["line"] = i + 1,
                            ["referenceType"] = refType,
                            ["lineContent"] = originalLines[i].Trim(),
                        });
                    }
                }
                catch (Exception)
                {
                    // Skip files that can't be read
                }
            }

            return new Dictionary<string, object>
            {
                ["symbolName"] = symbolName,
                ["symbolType"] = symbolType ?? "any",
                ["totalReferences"] = references.Count,
                ["fileCount"] = references.Select(r => r["scriptPath"]).Distinct().Count(),
                ["references"] = references.OrderBy(r => (string)r["scriptPath"]).ThenBy(r => (int)r["line"]).ToList(),
            };
        }

        /// <summary>
        /// Find potentially unused methods and fields across the project.
        /// A symbol is considered unused if it is declared but never referenced in other files.
        /// </summary>
        public Dictionary<string, object> FindUnusedCode(string scriptPath, string searchPath, string targetType)
        {
            var scriptPaths = !string.IsNullOrEmpty(scriptPath)
                ? new[] { scriptPath }
                : FindAllScripts(searchPath);

            // Collect all method/field declarations
            var declarations = new List<Dictionary<string, object>>();
            foreach (var path in scriptPaths)
            {
                try
                {
                    var fullPath = Path.GetFullPath(path);
                    var content = File.ReadAllText(fullPath);
                    var strippedContent = StripCommentsAndStrings(content);
                    var strippedLines = strippedContent.Split('\n');

                    for (int i = 0; i < strippedLines.Length; i++)
                    {
                        var line = strippedLines[i];

                        if (targetType == null || targetType == "method")
                        {
                            var methodMatch = MethodPattern.Match(line);
                            if (methodMatch.Success)
                            {
                                var name = methodMatch.Groups[5].Value;
                                // Skip common entry points and Unity callbacks
                                if (!IsUnityCallback(name) && !IsCommonEntryPoint(name))
                                {
                                    declarations.Add(new Dictionary<string, object>
                                    {
                                        ["name"] = name,
                                        ["type"] = "method",
                                        ["scriptPath"] = path,
                                        ["line"] = i + 1,
                                        ["access"] = methodMatch.Groups[1].Value,
                                    });
                                }
                            }
                        }

                        if (targetType == null || targetType == "field")
                        {
                            var fieldMatch = FieldPattern.Match(line);
                            if (fieldMatch.Success)
                            {
                                var name = fieldMatch.Groups[4].Value;
                                // Skip compiler-generated or single-char names
                                if (name.Length > 1 && !name.StartsWith("<"))
                                {
                                    declarations.Add(new Dictionary<string, object>
                                    {
                                        ["name"] = name,
                                        ["type"] = "field",
                                        ["scriptPath"] = path,
                                        ["line"] = i + 1,
                                        ["access"] = fieldMatch.Groups[1].Value,
                                    });
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Skip unreadable files
                }
            }

            // Search all project scripts for references to each declaration
            var allScripts = FindAllScripts(null);
            var allContents = new Dictionary<string, string>();
            foreach (var path in allScripts)
            {
                try
                {
                    var content = File.ReadAllText(Path.GetFullPath(path));
                    allContents[path] = StripCommentsAndStrings(content);
                }
                catch { }
            }

            var unusedItems = new List<Dictionary<string, object>>();
            foreach (var decl in declarations)
            {
                var name = (string)decl["name"];
                var declPath = (string)decl["scriptPath"];
                var refCount = 0;

                foreach (var kvp in allContents)
                {
                    if (kvp.Key == declPath) continue; // Skip self
                    // Use word-boundary check for accuracy
                    if (Regex.IsMatch(kvp.Value, $@"\b{Regex.Escape(name)}\b"))
                    {
                        refCount++;
                    }
                }

                if (refCount == 0)
                {
                    unusedItems.Add(new Dictionary<string, object>
                    {
                        ["name"] = name,
                        ["type"] = decl["type"],
                        ["scriptPath"] = declPath,
                        ["line"] = decl["line"],
                        ["access"] = decl["access"],
                    });
                }
            }

            return new Dictionary<string, object>
            {
                ["totalDeclarations"] = declarations.Count,
                ["totalUnused"] = unusedItems.Count,
                ["unusedItems"] = unusedItems.OrderBy(i => (string)i["scriptPath"]).ThenBy(i => (int)i["line"]).ToList(),
            };
        }

        /// <summary>
        /// Compute code metrics for scripts.
        /// </summary>
        public Dictionary<string, object> AnalyzeMetrics(string scriptPath, string searchPath)
        {
            var scriptPaths = !string.IsNullOrEmpty(scriptPath)
                ? new[] { scriptPath }
                : FindAllScripts(searchPath);

            var fileMetrics = new List<Dictionary<string, object>>();
            int totalLines = 0, totalCode = 0, totalComments = 0, totalBlanks = 0;
            int totalMethods = 0, totalFields = 0, totalProperties = 0, totalTypes = 0;
            var methodLengths = new List<int>();

            foreach (var path in scriptPaths)
            {
                try
                {
                    var fullPath = Path.GetFullPath(path);
                    var content = File.ReadAllText(fullPath);
                    var lines = content.Split('\n');
                    var strippedContent = StripCommentsAndStrings(content);

                    var blanks = lines.Count(l => string.IsNullOrWhiteSpace(l));
                    var comments = CountCommentLines(content);
                    var code = lines.Length - blanks - comments;

                    // Count members
                    var methods = MethodPattern.Matches(strippedContent).Count;
                    var fields = FieldPattern.Matches(strippedContent).Count;
                    var properties = PropertyPattern.Matches(strippedContent).Count;
                    var types = TypeDeclPattern.Matches(strippedContent).Count;

                    // Method lengths (approximate via brace counting)
                    var fileLengths = EstimateMethodLengths(content, strippedContent);
                    methodLengths.AddRange(fileLengths);

                    // Max nesting depth
                    var maxNesting = CalculateMaxNesting(strippedContent);

                    var fm = new Dictionary<string, object>
                    {
                        ["scriptPath"] = path,
                        ["totalLines"] = lines.Length,
                        ["codeLines"] = code,
                        ["commentLines"] = comments,
                        ["blankLines"] = blanks,
                        ["typeCount"] = types,
                        ["methodCount"] = methods,
                        ["fieldCount"] = fields,
                        ["propertyCount"] = properties,
                        ["maxNestingDepth"] = maxNesting,
                    };

                    if (fileLengths.Count > 0)
                    {
                        fm["avgMethodLength"] = Math.Round(fileLengths.Average(), 1);
                        fm["maxMethodLength"] = fileLengths.Max();
                    }

                    fileMetrics.Add(fm);

                    totalLines += lines.Length;
                    totalCode += code;
                    totalComments += comments;
                    totalBlanks += blanks;
                    totalMethods += methods;
                    totalFields += fields;
                    totalProperties += properties;
                    totalTypes += types;
                }
                catch (Exception)
                {
                    // Skip unreadable files
                }
            }

            var result = new Dictionary<string, object>
            {
                ["fileCount"] = fileMetrics.Count,
                ["totalLines"] = totalLines,
                ["totalCodeLines"] = totalCode,
                ["totalCommentLines"] = totalComments,
                ["totalBlankLines"] = totalBlanks,
                ["totalTypes"] = totalTypes,
                ["totalMethods"] = totalMethods,
                ["totalFields"] = totalFields,
                ["totalProperties"] = totalProperties,
            };

            if (totalCode > 0)
            {
                result["commentRatio"] = Math.Round((double)totalComments / (totalCode + totalComments) * 100, 1);
            }

            if (methodLengths.Count > 0)
            {
                result["avgMethodLength"] = Math.Round(methodLengths.Average(), 1);
                result["maxMethodLength"] = methodLengths.Max();
                result["medianMethodLength"] = methodLengths.OrderBy(x => x).ElementAt(methodLengths.Count / 2);
            }

            // Only include per-file details for single-file analysis or small sets
            if (fileMetrics.Count <= 50)
            {
                result["files"] = fileMetrics;
            }
            else
            {
                // For large sets, show top files by size
                result["largestFiles"] = fileMetrics
                    .OrderByDescending(f => (int)f["totalLines"])
                    .Take(20)
                    .ToList();
                result["mostComplexFiles"] = fileMetrics
                    .Where(f => f.ContainsKey("maxMethodLength"))
                    .OrderByDescending(f => (int)f["maxMethodLength"])
                    .Take(20)
                    .ToList();
            }

            return result;
        }

        #region Private Helpers

        private static List<Dictionary<string, object>> ParseTypeDeclarations(
            string originalContent, string strippedContent, string[] originalLines)
        {
            var strippedLines = strippedContent.Split('\n');
            var types = new List<Dictionary<string, object>>();

            for (int i = 0; i < strippedLines.Length; i++)
            {
                var match = TypeDeclPattern.Match(strippedLines[i]);
                if (!match.Success) continue;

                var typeDict = new Dictionary<string, object>
                {
                    ["name"] = match.Groups[5].Value,
                    ["kind"] = match.Groups[4].Value,
                    ["line"] = i + 1,
                    ["access"] = match.Groups[1].Value,
                };

                var modifiers = new List<string>();
                if (!string.IsNullOrWhiteSpace(match.Groups[2].Value))
                    modifiers.Add(match.Groups[2].Value.Trim());
                if (!string.IsNullOrWhiteSpace(match.Groups[3].Value))
                    modifiers.Add(match.Groups[3].Value.Trim());
                if (modifiers.Count > 0)
                    typeDict["modifiers"] = modifiers;

                // Generic type parameters
                if (match.Groups[6].Success && !string.IsNullOrEmpty(match.Groups[6].Value))
                    typeDict["typeParameters"] = match.Groups[6].Value;

                // Inheritance
                if (match.Groups[7].Success && !string.IsNullOrEmpty(match.Groups[7].Value))
                {
                    var bases = match.Groups[7].Value.Split(',')
                        .Select(b => b.Trim().Split('<')[0].Trim())
                        .Where(b => !string.IsNullOrEmpty(b))
                        .ToList();
                    typeDict["baseTypes"] = bases;
                }

                // Find the end of this type (brace matching) to extract members
                var bodyStart = FindOpenBrace(strippedLines, i);
                var bodyEnd = FindMatchingCloseBrace(strippedLines, bodyStart);

                if (bodyStart >= 0 && bodyEnd >= 0)
                {
                    typeDict["endLine"] = bodyEnd + 1;
                    typeDict["methods"] = ExtractMethods(strippedLines, originalLines, bodyStart, bodyEnd);
                    typeDict["fields"] = ExtractFields(strippedLines, originalLines, bodyStart, bodyEnd);
                    typeDict["properties"] = ExtractProperties(strippedLines, originalLines, bodyStart, bodyEnd);
                }

                types.Add(typeDict);
            }

            return types;
        }

        private static List<Dictionary<string, object>> ExtractMethods(
            string[] strippedLines, string[] originalLines, int start, int end)
        {
            var methods = new List<Dictionary<string, object>>();
            for (int i = start + 1; i < end && i < strippedLines.Length; i++)
            {
                // Skip nested type declarations
                if (TypeDeclPattern.IsMatch(strippedLines[i])) break;

                var match = MethodPattern.Match(strippedLines[i]);
                if (!match.Success) continue;

                var returnType = match.Groups[4].Value.Trim();
                var name = match.Groups[5].Value;

                // Skip property accessors and constructor-like patterns
                if (name == "get" || name == "set" || name == "add" || name == "remove") continue;
                if (returnType.Contains("{") || returnType.Contains("}")) continue;

                var methodDict = new Dictionary<string, object>
                {
                    ["name"] = name,
                    ["returnType"] = returnType,
                    ["line"] = i + 1,
                    ["access"] = match.Groups[1].Value,
                };

                var modifiers = new List<string>();
                if (!string.IsNullOrWhiteSpace(match.Groups[2].Value))
                    modifiers.Add(match.Groups[2].Value.Trim());
                if (!string.IsNullOrWhiteSpace(match.Groups[3].Value))
                    modifiers.Add(match.Groups[3].Value.Trim());
                if (modifiers.Count > 0)
                    methodDict["modifiers"] = modifiers;

                if (match.Groups[6].Success && !string.IsNullOrEmpty(match.Groups[6].Value))
                    methodDict["typeParameters"] = match.Groups[6].Value;

                // Parameters
                var paramStr = match.Groups[7].Value.Trim();
                if (!string.IsNullOrEmpty(paramStr))
                {
                    var parameters = paramStr.Split(',')
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrEmpty(p))
                        .ToList();
                    methodDict["parameters"] = parameters;
                    methodDict["parameterCount"] = parameters.Count;
                }
                else
                {
                    methodDict["parameterCount"] = 0;
                }

                // Estimate method body length
                var methodBodyStart = FindOpenBrace(strippedLines, i);
                if (methodBodyStart >= 0)
                {
                    var methodBodyEnd = FindMatchingCloseBrace(strippedLines, methodBodyStart);
                    if (methodBodyEnd >= 0)
                    {
                        methodDict["endLine"] = methodBodyEnd + 1;
                        methodDict["bodyLines"] = methodBodyEnd - methodBodyStart - 1;
                    }
                }

                methods.Add(methodDict);
            }

            return methods;
        }

        private static List<Dictionary<string, object>> ExtractFields(
            string[] strippedLines, string[] originalLines, int start, int end)
        {
            var fields = new List<Dictionary<string, object>>();
            for (int i = start + 1; i < end && i < strippedLines.Length; i++)
            {
                // Skip nested type declarations
                if (TypeDeclPattern.IsMatch(strippedLines[i])) break;
                // Skip method bodies (very rough: skip lines after open brace until matching close)
                if (MethodPattern.IsMatch(strippedLines[i]) || PropertyPattern.IsMatch(strippedLines[i])) continue;

                var match = FieldPattern.Match(strippedLines[i]);
                if (!match.Success) continue;

                var name = match.Groups[4].Value;
                var fieldType = match.Groups[3].Value.Trim();

                // Filter out false positives
                if (name.Length <= 1 || name.StartsWith("<")) continue;
                if (fieldType.Contains("(") || fieldType.Contains("{")) continue;

                // Check for attributes on previous lines
                var attributes = new List<string>();
                for (int j = i - 1; j >= start + 1; j--)
                {
                    var attrLine = strippedLines[j].Trim();
                    if (attrLine.StartsWith("["))
                    {
                        foreach (Match am in AttributePattern.Matches(attrLine))
                        {
                            attributes.Add(am.Groups[1].Value);
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                var fieldDict = new Dictionary<string, object>
                {
                    ["name"] = name,
                    ["fieldType"] = fieldType,
                    ["line"] = i + 1,
                    ["access"] = match.Groups[1].Value,
                };

                var modifiers = match.Groups[2].Value.Trim();
                if (!string.IsNullOrEmpty(modifiers))
                    fieldDict["modifiers"] = modifiers;

                if (attributes.Count > 0)
                    fieldDict["attributes"] = attributes;

                fields.Add(fieldDict);
            }

            return fields;
        }

        private static List<Dictionary<string, object>> ExtractProperties(
            string[] strippedLines, string[] originalLines, int start, int end)
        {
            var properties = new List<Dictionary<string, object>>();
            for (int i = start + 1; i < end && i < strippedLines.Length; i++)
            {
                // Skip nested type declarations
                if (TypeDeclPattern.IsMatch(strippedLines[i])) break;

                var match = PropertyPattern.Match(strippedLines[i]);
                if (!match.Success) continue;

                var name = match.Groups[4].Value;
                var propType = match.Groups[3].Value.Trim();

                // Filter property-like false positives (methods, etc.)
                if (propType.Contains("(") || propType.Contains(")")) continue;
                if (name == "get" || name == "set") continue;

                // Check if it's actually a method (has parentheses after name)
                var lineAfterName = strippedLines[i].Substring(strippedLines[i].IndexOf(name) + name.Length).TrimStart();
                if (lineAfterName.StartsWith("(")) continue;

                var propDict = new Dictionary<string, object>
                {
                    ["name"] = name,
                    ["propertyType"] = propType,
                    ["line"] = i + 1,
                    ["access"] = match.Groups[1].Value,
                };

                var modifiers = match.Groups[2].Value.Trim();
                if (!string.IsNullOrEmpty(modifiers))
                    propDict["modifiers"] = modifiers;

                // Detect getter/setter
                var propBodyStart = FindOpenBrace(strippedLines, i);
                if (propBodyStart >= 0)
                {
                    var propBodyEnd = FindMatchingCloseBrace(strippedLines, propBodyStart);
                    if (propBodyEnd >= 0)
                    {
                        var propBody = string.Join("\n", strippedLines.Skip(propBodyStart).Take(propBodyEnd - propBodyStart + 1));
                        propDict["hasGetter"] = propBody.Contains("get");
                        propDict["hasSetter"] = propBody.Contains("set");
                    }
                }

                properties.Add(propDict);
            }

            return properties;
        }

        private static string ClassifyReference(string line, string symbolName, string symbolType)
        {
            var escaped = Regex.Escape(symbolName);

            // Check for word boundary match
            if (!Regex.IsMatch(line, $@"\b{escaped}\b")) return null;

            // Type declaration (skip — we want usages, not declarations)
            if (Regex.IsMatch(line, $@"(class|struct|interface|enum)\s+{escaped}\b")) return null;

            // Method declaration (skip)
            if (Regex.IsMatch(line, $@"\b\w+\s+{escaped}\s*(<[^>]*>)?\s*\(") &&
                !Regex.IsMatch(line, $@"[\w.]+\.{escaped}\s*\(") &&
                !Regex.IsMatch(line, $@"=\s*{escaped}\s*\("))
            {
                // Could be a declaration if it doesn't look like a call
                if (symbolType == "method") return null;
            }

            // Classify by context
            if (symbolType == "method" || symbolType == null)
            {
                if (Regex.IsMatch(line, $@"\b{escaped}\s*(<[^>]*>)?\s*\("))
                    return "method_call";
            }

            if (symbolType == "class" || symbolType == null)
            {
                if (Regex.IsMatch(line, $@"\bnew\s+{escaped}\b"))
                    return "instantiation";
                if (Regex.IsMatch(line, $@"\b{escaped}\s+\w+"))
                    return "type_usage";
                if (Regex.IsMatch(line, $@":\s*[^{{]*\b{escaped}\b"))
                    return "inheritance";
                if (Regex.IsMatch(line, $@"typeof\s*\(\s*{escaped}\s*\)"))
                    return "typeof";
                if (Regex.IsMatch(line, $@"<[^>]*\b{escaped}\b[^>]*>"))
                    return "generic_argument";
                if (Regex.IsMatch(line, $@"\b{escaped}\s*\."))
                    return "static_access";
            }

            if (symbolType == "field" || symbolType == "property" || symbolType == null)
            {
                if (Regex.IsMatch(line, $@"\.\s*{escaped}\b(?!\s*\()"))
                    return "member_access";
            }

            return "reference";
        }

        private static string StripCommentsAndStrings(string content)
        {
            // Order matters: remove strings first, then block comments, then line comments
            var result = StringLiteralPattern.Replace(content, m => new string(' ', m.Length));
            result = BlockCommentPattern.Replace(result, m =>
            {
                // Preserve line count
                var lineCount = m.Value.Count(c => c == '\n');
                return string.Join("\n", Enumerable.Repeat("", lineCount + 1));
            });
            result = LineCommentPattern.Replace(result, "");
            return result;
        }

        private static int CountCommentLines(string content)
        {
            var count = 0;
            var lines = content.Split('\n');
            var inBlockComment = false;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (inBlockComment)
                {
                    count++;
                    if (line.Contains("*/"))
                        inBlockComment = false;
                    continue;
                }

                if (line.StartsWith("//") || line.StartsWith("///"))
                {
                    count++;
                }
                else if (line.StartsWith("/*") || line.Contains("/*"))
                {
                    count++;
                    if (!line.Contains("*/"))
                        inBlockComment = true;
                }
            }

            return count;
        }

        private static int FindOpenBrace(string[] lines, int startLine)
        {
            for (int i = startLine; i < lines.Length; i++)
            {
                if (lines[i].Contains("{")) return i;
            }
            return -1;
        }

        private static int FindMatchingCloseBrace(string[] lines, int openBraceLine)
        {
            if (openBraceLine < 0) return -1;
            int depth = 0;
            for (int i = openBraceLine; i < lines.Length; i++)
            {
                foreach (char c in lines[i])
                {
                    if (c == '{') depth++;
                    else if (c == '}') depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }

        private static List<int> EstimateMethodLengths(string content, string strippedContent)
        {
            var lengths = new List<int>();
            var strippedLines = strippedContent.Split('\n');

            for (int i = 0; i < strippedLines.Length; i++)
            {
                if (!MethodPattern.IsMatch(strippedLines[i])) continue;
                var name = MethodPattern.Match(strippedLines[i]).Groups[5].Value;
                if (name == "get" || name == "set" || name == "add" || name == "remove") continue;

                var bodyStart = FindOpenBrace(strippedLines, i);
                if (bodyStart < 0) continue;
                var bodyEnd = FindMatchingCloseBrace(strippedLines, bodyStart);
                if (bodyEnd >= 0)
                {
                    lengths.Add(bodyEnd - bodyStart - 1);
                }
            }

            return lengths;
        }

        private static int CalculateMaxNesting(string strippedContent)
        {
            int maxDepth = 0, currentDepth = 0;
            foreach (char c in strippedContent)
            {
                if (c == '{') { currentDepth++; if (currentDepth > maxDepth) maxDepth = currentDepth; }
                else if (c == '}') { currentDepth--; }
            }
            return maxDepth;
        }

        private static bool IsUnityCallback(string name)
        {
            var callbacks = new HashSet<string>
            {
                "Awake", "Start", "Update", "FixedUpdate", "LateUpdate",
                "OnEnable", "OnDisable", "OnDestroy",
                "OnCollisionEnter", "OnCollisionExit", "OnCollisionStay",
                "OnCollisionEnter2D", "OnCollisionExit2D", "OnCollisionStay2D",
                "OnTriggerEnter", "OnTriggerExit", "OnTriggerStay",
                "OnTriggerEnter2D", "OnTriggerExit2D", "OnTriggerStay2D",
                "OnGUI", "OnDrawGizmos", "OnDrawGizmosSelected",
                "OnApplicationQuit", "OnApplicationPause", "OnApplicationFocus",
                "OnValidate", "Reset", "OnBecameVisible", "OnBecameInvisible",
                "OnMouseDown", "OnMouseUp", "OnMouseEnter", "OnMouseExit",
                "OnMouseOver", "OnMouseDrag",
                "OnAnimatorMove", "OnAnimatorIK",
            };
            return callbacks.Contains(name);
        }

        private static bool IsCommonEntryPoint(string name)
        {
            return name == "Main" || name == "Execute" || name == "Run" ||
                   name == "ToString" || name == "GetHashCode" || name == "Equals";
        }

        private static void ValidateScriptPath(string scriptPath)
        {
            if (string.IsNullOrEmpty(scriptPath))
                throw new ArgumentException("scriptPath is required");
            if (!scriptPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Not a C# file: {scriptPath}");

            var fullPath = Path.GetFullPath(scriptPath);
            if (!File.Exists(fullPath))
                throw new ArgumentException($"Script not found: {scriptPath}");
        }

        private static string[] FindAllScripts(string searchPath)
        {
            string[] searchFolders = string.IsNullOrEmpty(searchPath)
                ? new[] { "Assets" }
                : new[] { searchPath };

            var guids = AssetDatabase.FindAssets("t:Script", searchFolders);
            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p)
                .ToArray();
        }

        #endregion
    }
}
