using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace MCP.Editor.Utilities.GraphAnalysis
{
    /// <summary>
    /// Entry for a discovered type in the project.
    /// </summary>
    public class TypeCatalogEntry
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Namespace { get; set; }
        public string TypeKind { get; set; }
        public string FilePath { get; set; }
        public string BaseClass { get; set; }
        public List<string> Interfaces { get; set; }
        public string Assembly { get; set; }

        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                ["name"] = Name,
                ["fullName"] = FullName,
                ["typeKind"] = TypeKind
            };
            if (!string.IsNullOrEmpty(Namespace)) dict["namespace"] = Namespace;
            if (!string.IsNullOrEmpty(FilePath)) dict["filePath"] = FilePath;
            if (!string.IsNullOrEmpty(BaseClass)) dict["baseClass"] = BaseClass;
            if (Interfaces != null && Interfaces.Count > 0) dict["interfaces"] = Interfaces;
            if (!string.IsNullOrEmpty(Assembly)) dict["assembly"] = Assembly;
            return dict;
        }
    }

    /// <summary>
    /// Detailed inspection result for a single type.
    /// </summary>
    public class TypeInspectionResult
    {
        public TypeCatalogEntry Entry { get; set; }
        public List<Dictionary<string, object>> Fields { get; set; }
        public List<Dictionary<string, object>> Methods { get; set; }
        public List<Dictionary<string, object>> Properties { get; set; }
        public List<string> Attributes { get; set; }

        public Dictionary<string, object> ToDictionary()
        {
            var dict = Entry.ToDictionary();
            if (Attributes != null && Attributes.Count > 0) dict["attributes"] = Attributes;
            if (Fields != null) dict["fields"] = Fields;
            if (Methods != null) dict["methods"] = Methods;
            if (Properties != null) dict["properties"] = Properties;
            return dict;
        }
    }

    /// <summary>
    /// Analyzes and catalogs types in the Unity project.
    /// Provides type enumeration and detailed inspection.
    /// </summary>
    public class TypeCatalogAnalyzer
    {
        /// <summary>
        /// List types matching the given filters.
        /// </summary>
        public List<TypeCatalogEntry> ListTypes(
            string searchPath = null,
            string typeKind = null,
            string namespaceName = null,
            string baseClass = null,
            string namePattern = null,
            int maxResults = 100)
        {
            maxResults = Math.Min(Math.Max(maxResults, 1), 1000);

            var results = new List<TypeCatalogEntry>();
            var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies);
            var monoScripts = MonoImporter.GetAllRuntimeMonoScripts();
            var scriptTypeMap = BuildScriptTypeMap(monoScripts);

            // Collect source files scoped by searchPath
            var sourceFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var asm in assemblies)
            {
                foreach (var src in asm.sourceFiles)
                {
                    if (searchPath == null || src.Replace('\\', '/').StartsWith(searchPath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
                    {
                        sourceFiles.Add(src);
                    }
                }
            }

            // Iterate loaded assemblies and match types
            foreach (var loadedAsm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = loadedAsm.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }

                foreach (var type in types)
                {
                    if (type.IsNested) continue;

                    // Check if the type has a source file in our scope
                    string filePath = null;
                    if (scriptTypeMap.TryGetValue(type, out var path))
                    {
                        filePath = path;
                    }

                    // If searchPath is specified, only include types with matching source files
                    if (searchPath != null)
                    {
                        if (filePath == null || !sourceFiles.Contains(filePath)) continue;
                    }
                    else
                    {
                        // Without searchPath, skip Unity/System types
                        if (filePath == null && IsUnityOrSystemType(type)) continue;
                    }

                    var kind = GetTypeKind(type);

                    // Apply typeKind filter
                    if (typeKind != null && !string.Equals(kind, typeKind, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Apply namespace filter (prefix match)
                    if (namespaceName != null)
                    {
                        var ns = type.Namespace ?? "";
                        if (!ns.Equals(namespaceName, StringComparison.OrdinalIgnoreCase) &&
                            !ns.StartsWith(namespaceName + ".", StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    // Apply baseClass filter
                    if (baseClass != null)
                    {
                        if (type.BaseType == null) continue;
                        var baseName = type.BaseType.Name;
                        var baseFullName = type.BaseType.FullName ?? baseName;
                        if (!string.Equals(baseName, baseClass, StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(baseFullName, baseClass, StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    // Apply namePattern filter (wildcard)
                    if (namePattern != null && !McpWildcardUtility.IsMatch(type.Name, namePattern))
                        continue;

                    var entry = CreateEntry(type, kind, filePath);
                    results.Add(entry);

                    if (results.Count >= maxResults) break;
                }

                if (results.Count >= maxResults) break;
            }

            return results;
        }

        /// <summary>
        /// Inspect a single type in detail.
        /// </summary>
        public TypeInspectionResult InspectType(
            string className,
            bool includeFields = true,
            bool includeMethods = false,
            bool includeProperties = false)
        {
            var type = FindType(className);
            if (type == null)
            {
                throw new InvalidOperationException($"Type not found: {className}");
            }

            var kind = GetTypeKind(type);
            string filePath = null;
            var monoScripts = MonoImporter.GetAllRuntimeMonoScripts();
            foreach (var script in monoScripts)
            {
                if (script != null && script.GetClass() == type)
                {
                    filePath = AssetDatabase.GetAssetPath(script);
                    break;
                }
            }

            var entry = CreateEntry(type, kind, filePath);
            var result = new TypeInspectionResult { Entry = entry };

            // Attributes
            var attrs = type.GetCustomAttributes(false)
                .Select(a => a.GetType().Name)
                .ToList();
            if (attrs.Count > 0) result.Attributes = attrs;

            // Fields
            if (includeFields)
            {
                result.Fields = GetFieldInfos(type);
            }

            // Methods
            if (includeMethods)
            {
                result.Methods = GetMethodInfos(type);
            }

            // Properties
            if (includeProperties)
            {
                result.Properties = GetPropertyInfos(type);
            }

            return result;
        }

        #region Private Helpers

        private static Dictionary<Type, string> BuildScriptTypeMap(MonoScript[] scripts)
        {
            var map = new Dictionary<Type, string>();
            foreach (var script in scripts)
            {
                if (script == null) continue;
                var cls = script.GetClass();
                if (cls != null)
                {
                    map[cls] = AssetDatabase.GetAssetPath(script);
                }
            }
            return map;
        }

        private static TypeCatalogEntry CreateEntry(Type type, string kind, string filePath)
        {
            var entry = new TypeCatalogEntry
            {
                Name = type.Name,
                FullName = type.FullName ?? type.Name,
                Namespace = type.Namespace,
                TypeKind = kind,
                FilePath = filePath,
                Assembly = type.Assembly.GetName().Name
            };

            if (type.BaseType != null && type.BaseType != typeof(object))
            {
                entry.BaseClass = type.BaseType.FullName ?? type.BaseType.Name;
            }

            var interfaces = type.GetInterfaces()
                .Where(i => type.BaseType == null || !type.BaseType.GetInterfaces().Contains(i))
                .Select(i => i.FullName ?? i.Name)
                .ToList();
            if (interfaces.Count > 0)
            {
                entry.Interfaces = interfaces;
            }

            return entry;
        }

        private static string GetTypeKind(Type type)
        {
            if (type.IsInterface) return "interface";
            if (type.IsEnum) return "enum";
            if (type.IsValueType) return "struct";
            if (typeof(MonoBehaviour).IsAssignableFrom(type)) return "MonoBehaviour";
            if (typeof(ScriptableObject).IsAssignableFrom(type)) return "ScriptableObject";
            return "class";
        }

        private static List<Dictionary<string, object>> GetFieldInfos(Type type)
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            var list = new List<Dictionary<string, object>>();

            foreach (var field in fields)
            {
                // Skip compiler-generated backing fields
                if (field.Name.StartsWith("<")) continue;

                var isPublic = field.IsPublic;
                var hasSerializeField = field.GetCustomAttribute<SerializeField>() != null;
                var hasNonSerialized = field.GetCustomAttribute<NonSerializedAttribute>() != null;
                var hasHideInInspector = field.GetCustomAttribute<HideInInspector>() != null;
                var isSerializable = (isPublic && !hasNonSerialized) || hasSerializeField;

                var info = new Dictionary<string, object>
                {
                    ["name"] = field.Name,
                    ["type"] = GetFriendlyTypeName(field.FieldType),
                    ["access"] = GetAccessModifier(field),
                    ["isSerializable"] = isSerializable
                };

                if (hasHideInInspector) info["hideInInspector"] = true;

                var fieldAttrs = field.GetCustomAttributes(false)
                    .Select(a => a.GetType().Name)
                    .Where(n => n != "SerializeField" && n != "CompilerGeneratedAttribute")
                    .ToList();
                if (fieldAttrs.Count > 0) info["attributes"] = fieldAttrs;

                list.Add(info);
            }

            return list;
        }

        private static List<Dictionary<string, object>> GetMethodInfos(Type type)
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            var list = new List<Dictionary<string, object>>();

            foreach (var method in methods)
            {
                // Skip property accessors and compiler-generated methods
                if (method.IsSpecialName) continue;
                if (method.Name.StartsWith("<")) continue;

                var info = new Dictionary<string, object>
                {
                    ["name"] = method.Name,
                    ["returnType"] = GetFriendlyTypeName(method.ReturnType),
                    ["access"] = method.IsPublic ? "public" : method.IsPrivate ? "private" : method.IsFamily ? "protected" : "internal",
                    ["isStatic"] = method.IsStatic
                };

                var parameters = method.GetParameters()
                    .Select(p => new Dictionary<string, object>
                    {
                        ["name"] = p.Name,
                        ["type"] = GetFriendlyTypeName(p.ParameterType)
                    })
                    .ToList();
                if (parameters.Count > 0) info["parameters"] = parameters;

                list.Add(info);
            }

            return list;
        }

        private static List<Dictionary<string, object>> GetPropertyInfos(Type type)
        {
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            var list = new List<Dictionary<string, object>>();

            foreach (var prop in properties)
            {
                var info = new Dictionary<string, object>
                {
                    ["name"] = prop.Name,
                    ["type"] = GetFriendlyTypeName(prop.PropertyType),
                    ["canRead"] = prop.CanRead,
                    ["canWrite"] = prop.CanWrite
                };

                list.Add(info);
            }

            return list;
        }

        private static Type FindType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(typeName);
                    if (type != null) return type;
                }
                catch { }
            }

            // Try by simple name
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

        private static bool IsUnityOrSystemType(Type type)
        {
            if (type == null) return true;
            var ns = type.Namespace;
            if (string.IsNullOrEmpty(ns)) return false;
            return ns.StartsWith("System") ||
                   ns.StartsWith("UnityEngine") ||
                   ns.StartsWith("UnityEditor") ||
                   ns.StartsWith("Unity.") ||
                   ns.StartsWith("Microsoft") ||
                   ns.StartsWith("Mono") ||
                   ns.StartsWith("MCP.Editor");
        }

        private static string GetAccessModifier(FieldInfo field)
        {
            if (field.IsPublic) return "public";
            if (field.IsPrivate) return "private";
            if (field.IsFamily) return "protected";
            if (field.IsAssembly) return "internal";
            return "";
        }

        private static string GetFriendlyTypeName(Type type)
        {
            if (type == typeof(void)) return "void";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            if (type == typeof(long)) return "long";

            if (type.IsArray)
            {
                return GetFriendlyTypeName(type.GetElementType()) + "[]";
            }

            if (type.IsGenericType)
            {
                var genericName = type.Name.Split('`')[0];
                var args = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));
                return $"{genericName}<{args}>";
            }

            return type.Name;
        }

        #endregion
    }
}
