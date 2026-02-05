using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace MCP.Editor.Utilities.GraphAnalysis
{
    /// <summary>
    /// Analyzes class dependencies in the Unity project.
    /// </summary>
    public class ClassDependencyAnalyzer
    {
        private readonly Dictionary<string, ClassGraphNode> _nodeCache = new Dictionary<string, ClassGraphNode>();
        private readonly HashSet<string> _processedTypes = new HashSet<string>();
        private bool _includeUnityTypes;
        private int _maxDepth = 1;
        private int _currentDepth = 0;

        /// <summary>
        /// Analyze a single class and its dependencies.
        /// </summary>
        public ClassDependencyResult AnalyzeClass(string typeName, int depth = 1, bool includeUnityTypes = false)
        {
            _maxDepth = depth;
            _includeUnityTypes = includeUnityTypes;
            _currentDepth = 0;

            var result = new ClassDependencyResult
            {
                AnalysisTarget = typeName,
                Depth = depth
            };

            var type = FindType(typeName);
            if (type == null)
            {
                throw new InvalidOperationException($"Type not found: {typeName}");
            }

            AnalyzeTypeRecursive(type, result, 0);

            return result;
        }

        /// <summary>
        /// Analyze all classes in an assembly.
        /// </summary>
        public ClassDependencyResult AnalyzeAssembly(string assemblyName, int depth = 1, bool includeUnityTypes = false)
        {
            _maxDepth = depth;
            _includeUnityTypes = includeUnityTypes;
            _currentDepth = 0;

            var result = new ClassDependencyResult
            {
                AnalysisTarget = assemblyName,
                Depth = depth
            };

            // Find the assembly
            var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies);
            var targetAssembly = assemblies.FirstOrDefault(a => a.name == assemblyName);

            if (targetAssembly == null)
            {
                // Try to find in loaded assemblies
                var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == assemblyName);

                if (loadedAssembly == null)
                {
                    throw new InvalidOperationException($"Assembly not found: {assemblyName}");
                }

                foreach (var type in loadedAssembly.GetTypes())
                {
                    if (!type.IsPublic || type.IsNested) continue;
                    AnalyzeTypeRecursive(type, result, 0);
                }
            }
            else
            {
                // Analyze from source files
                foreach (var sourceFile in targetAssembly.sourceFiles)
                {
                    AnalyzeSourceFile(sourceFile, result);
                }
            }

            return result;
        }

        /// <summary>
        /// Analyze all classes in a namespace.
        /// </summary>
        public ClassDependencyResult AnalyzeNamespace(string namespaceName, int depth = 1, bool includeUnityTypes = false)
        {
            _maxDepth = depth;
            _includeUnityTypes = includeUnityTypes;
            _currentDepth = 0;

            var result = new ClassDependencyResult
            {
                AnalysisTarget = namespaceName,
                Depth = depth
            };

            // Find all types in the namespace from all loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Namespace == namespaceName ||
                            (type.Namespace != null && type.Namespace.StartsWith(namespaceName + ".")))
                        {
                            AnalyzeTypeRecursive(type, result, 0);
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Some assemblies might not be loadable
                }
            }

            return result;
        }

        /// <summary>
        /// Find classes that depend on the specified class.
        /// </summary>
        public ClassDependencyResult FindDependents(string typeName, bool includeUnityTypes = false)
        {
            _includeUnityTypes = includeUnityTypes;

            var result = new ClassDependencyResult
            {
                AnalysisTarget = typeName
            };

            var targetType = FindType(typeName);
            if (targetType == null)
            {
                throw new InvalidOperationException($"Type not found: {typeName}");
            }

            // Add target node
            var targetNode = CreateNodeForType(targetType);
            result.AddNode(targetNode);

            // Search all project assemblies for types that reference the target
            var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies);
            foreach (var assembly in assemblies)
            {
                foreach (var sourceFile in assembly.sourceFiles)
                {
                    SearchFileForReferences(sourceFile, targetType, result);
                }
            }

            return result;
        }

        /// <summary>
        /// Find classes that the specified class depends on.
        /// </summary>
        public ClassDependencyResult FindDependencies(string typeName, int depth = 1, bool includeUnityTypes = false)
        {
            // This is essentially the same as AnalyzeClass
            return AnalyzeClass(typeName, depth, includeUnityTypes);
        }

        #region Private Methods

        private void AnalyzeTypeRecursive(Type type, ClassDependencyResult result, int currentDepth)
        {
            if (currentDepth > _maxDepth) return;

            var fullName = type.FullName ?? type.Name;
            if (_processedTypes.Contains(fullName)) return;
            _processedTypes.Add(fullName);

            // Skip Unity/System types if not requested
            if (!_includeUnityTypes && IsUnityOrSystemType(type)) return;

            var node = CreateNodeForType(type);
            result.AddNode(node);

            // Analyze base class
            if (type.BaseType != null && type.BaseType != typeof(object))
            {
                var baseType = type.BaseType;
                if (_includeUnityTypes || !IsUnityOrSystemType(baseType))
                {
                    var edge = new ClassDependencyEdge(fullName, baseType.FullName ?? baseType.Name, "inherits");
                    result.AddEdge(edge);

                    if (currentDepth < _maxDepth)
                    {
                        AnalyzeTypeRecursive(baseType, result, currentDepth + 1);
                    }
                }
            }

            // Analyze interfaces
            foreach (var iface in type.GetInterfaces())
            {
                // Skip interfaces from parent types
                if (type.BaseType != null && type.BaseType.GetInterfaces().Contains(iface)) continue;

                if (_includeUnityTypes || !IsUnityOrSystemType(iface))
                {
                    var edge = new ClassDependencyEdge(fullName, iface.FullName ?? iface.Name, "implements");
                    result.AddEdge(edge);

                    if (currentDepth < _maxDepth)
                    {
                        AnalyzeTypeRecursive(iface, result, currentDepth + 1);
                    }
                }
            }

            // Analyze fields
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                var fieldType = GetUnderlyingType(field.FieldType);
                if (fieldType != null && fieldType != type && (_includeUnityTypes || !IsUnityOrSystemType(fieldType)))
                {
                    var edge = new ClassDependencyEdge(fullName, fieldType.FullName ?? fieldType.Name, "field_reference")
                    {
                        MemberName = field.Name,
                        Declaration = $"{GetAccessModifier(field)} {fieldType.Name} {field.Name}"
                    };
                    result.AddEdge(edge);

                    if (currentDepth < _maxDepth)
                    {
                        AnalyzeTypeRecursive(fieldType, result, currentDepth + 1);
                    }
                }
            }

            // Check for RequireComponent attribute
            var requireComponentAttrs = type.GetCustomAttributes<RequireComponent>(true);
            foreach (var attr in requireComponentAttrs)
            {
                var requiredTypes = new[] { attr.m_Type0, attr.m_Type1, attr.m_Type2 }
                    .Where(t => t != null);

                foreach (var reqType in requiredTypes)
                {
                    var edge = new ClassDependencyEdge(fullName, reqType.FullName ?? reqType.Name, "requires_component");
                    result.AddEdge(edge);

                    if (currentDepth < _maxDepth)
                    {
                        AnalyzeTypeRecursive(reqType, result, currentDepth + 1);
                    }
                }
            }
        }

        private void AnalyzeSourceFile(string filePath, ClassDependencyResult result)
        {
            if (!File.Exists(filePath)) return;

            try
            {
                var content = File.ReadAllText(filePath);

                // Extract namespace
                var namespaceMatch = Regex.Match(content, @"namespace\s+([\w.]+)");
                var ns = namespaceMatch.Success ? namespaceMatch.Groups[1].Value : "";

                // Extract class/struct/interface declarations
                var typeMatches = Regex.Matches(content, @"(public|internal|private|protected)?\s*(sealed|abstract|static)?\s*(class|struct|interface|enum)\s+(\w+)(?:<[^>]+>)?(?:\s*:\s*([^{]+))?");

                foreach (Match match in typeMatches)
                {
                    var typeName = match.Groups[4].Value;
                    var fullName = string.IsNullOrEmpty(ns) ? typeName : $"{ns}.{typeName}";
                    var typeKind = match.Groups[3].Value;
                    var inheritance = match.Groups[5].Value;

                    var node = new ClassGraphNode(fullName, typeKind)
                    {
                        FilePath = filePath,
                        Namespace = ns
                    };

                    // Parse inheritance
                    if (!string.IsNullOrEmpty(inheritance))
                    {
                        var parts = inheritance.Split(',').Select(p => p.Trim()).ToList();
                        foreach (var part in parts)
                        {
                            var baseName = part.Split('<')[0].Trim();
                            if (string.IsNullOrEmpty(baseName)) continue;

                            // First one is likely base class for class type
                            if (typeKind == "class" && parts.IndexOf(part) == 0 && !baseName.StartsWith("I"))
                            {
                                node.BaseClass = baseName;
                                var edge = new ClassDependencyEdge(fullName, baseName, "inherits");
                                result.AddEdge(edge);
                            }
                            else
                            {
                                if (node.Interfaces == null) node.Interfaces = new List<string>();
                                node.Interfaces.Add(baseName);
                                var edge = new ClassDependencyEdge(fullName, baseName, "implements");
                                result.AddEdge(edge);
                            }
                        }
                    }

                    result.AddNode(node);
                }
            }
            catch (Exception)
            {
                // File read error, skip
            }
        }

        private void SearchFileForReferences(string filePath, Type targetType, ClassDependencyResult result)
        {
            if (!File.Exists(filePath)) return;

            try
            {
                var content = File.ReadAllText(filePath);
                var targetName = targetType.Name;
                var targetFullName = targetType.FullName ?? targetName;

                // Check if file references the target type
                if (!content.Contains(targetName)) return;

                // Extract the class name from this file
                var namespaceMatch = Regex.Match(content, @"namespace\s+([\w.]+)");
                var ns = namespaceMatch.Success ? namespaceMatch.Groups[1].Value : "";

                var classMatch = Regex.Match(content, @"(class|struct)\s+(\w+)");
                if (!classMatch.Success) return;

                var className = classMatch.Groups[2].Value;
                var fullClassName = string.IsNullOrEmpty(ns) ? className : $"{ns}.{className}";

                if (fullClassName == targetFullName) return; // Skip self-reference

                // Check for field declarations
                var fieldPattern = $@"(private|public|protected|internal)?\s*{Regex.Escape(targetName)}(?:<[^>]+>)?\s+\w+\s*[;=]";
                if (Regex.IsMatch(content, fieldPattern))
                {
                    var sourceNode = new ClassGraphNode(fullClassName, "class") { FilePath = filePath };
                    result.AddNode(sourceNode);

                    var edge = new ClassDependencyEdge(fullClassName, targetFullName, "field_reference");
                    result.AddEdge(edge);
                }

                // Check for inheritance
                var inheritPattern = $@"(class|struct)\s+\w+\s*:\s*[^{{]*{Regex.Escape(targetName)}";
                if (Regex.IsMatch(content, inheritPattern))
                {
                    var sourceNode = new ClassGraphNode(fullClassName, "class") { FilePath = filePath };
                    result.AddNode(sourceNode);

                    var relation = targetType.IsInterface ? "implements" : "inherits";
                    var edge = new ClassDependencyEdge(fullClassName, targetFullName, relation);
                    result.AddEdge(edge);
                }

                // Check for RequireComponent attribute
                var requirePattern = $@"\[RequireComponent\s*\([^)]*typeof\s*\(\s*{Regex.Escape(targetName)}\s*\)";
                if (Regex.IsMatch(content, requirePattern))
                {
                    var sourceNode = new ClassGraphNode(fullClassName, "class") { FilePath = filePath };
                    result.AddNode(sourceNode);

                    var edge = new ClassDependencyEdge(fullClassName, targetFullName, "requires_component");
                    result.AddEdge(edge);
                }
            }
            catch (Exception)
            {
                // File read error, skip
            }
        }

        private ClassGraphNode CreateNodeForType(Type type)
        {
            var fullName = type.FullName ?? type.Name;
            if (_nodeCache.TryGetValue(fullName, out var cached))
            {
                return cached;
            }

            var typeKind = type.IsInterface ? "interface" :
                           type.IsEnum ? "enum" :
                           type.IsValueType ? "struct" :
                           typeof(MonoBehaviour).IsAssignableFrom(type) ? "MonoBehaviour" :
                           typeof(ScriptableObject).IsAssignableFrom(type) ? "ScriptableObject" :
                           "class";

            var node = new ClassGraphNode(fullName, typeKind)
            {
                Namespace = type.Namespace,
                Assembly = type.Assembly.GetName().Name
            };

            // Get source file path if available
            var monoScripts = MonoImporter.GetAllRuntimeMonoScripts();
            foreach (var script in monoScripts)
            {
                if (script != null && script.GetClass() == type)
                {
                    node.FilePath = AssetDatabase.GetAssetPath(script);
                    break;
                }
            }

            // Get base class
            if (type.BaseType != null && type.BaseType != typeof(object))
            {
                node.BaseClass = type.BaseType.FullName ?? type.BaseType.Name;
            }

            // Get interfaces
            var interfaces = type.GetInterfaces()
                .Where(i => type.BaseType == null || !type.BaseType.GetInterfaces().Contains(i))
                .Select(i => i.FullName ?? i.Name)
                .ToList();
            if (interfaces.Any())
            {
                node.Interfaces = interfaces;
            }

            _nodeCache[fullName] = node;
            return node;
        }

        private Type FindType(string typeName)
        {
            // Try direct match first
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(typeName);
                    if (type != null) return type;
                }
                catch { }
            }

            // Try to find by simple name
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name == typeName || type.FullName == typeName)
                        {
                            return type;
                        }
                    }
                }
                catch (ReflectionTypeLoadException) { }
            }

            return null;
        }

        private Type GetUnderlyingType(Type type)
        {
            // Handle arrays
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            // Handle generics (List<T>, Dictionary<K,V>, etc.)
            if (type.IsGenericType)
            {
                var args = type.GetGenericArguments();
                // Return the first meaningful type argument
                return args.FirstOrDefault(t => !IsUnityOrSystemType(t) || _includeUnityTypes) ?? args.FirstOrDefault();
            }

            return type;
        }

        private bool IsUnityOrSystemType(Type type)
        {
            if (type == null) return true;

            var ns = type.Namespace;
            if (string.IsNullOrEmpty(ns)) return false;

            return ns.StartsWith("System") ||
                   ns.StartsWith("UnityEngine") ||
                   ns.StartsWith("UnityEditor") ||
                   ns.StartsWith("Unity.") ||
                   ns.StartsWith("Microsoft") ||
                   ns.StartsWith("Mono");
        }

        private string GetAccessModifier(FieldInfo field)
        {
            if (field.IsPublic) return "public";
            if (field.IsPrivate) return "private";
            if (field.IsFamily) return "protected";
            if (field.IsAssembly) return "internal";
            return "";
        }

        #endregion
    }
}
